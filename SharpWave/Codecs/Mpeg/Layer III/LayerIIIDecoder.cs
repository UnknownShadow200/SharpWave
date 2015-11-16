using System;
using System.IO;
using SharpWave.Utils;

namespace SharpWave.Codecs.Mpeg {

	public sealed partial class LayerIIIDecoder : LayerDecoder {
		
		int channels;
		BitReader reader;
		const int numGranules = 2;
		const int numSubbands = 21;
		const int numSubbandsShort = 12;
		int sampleRate;
		BitReservoir reservoir = new BitReservoir( 2048 );
		
		public override byte[] Decode( MpegFrame frame, BitReader reader ) {
			channels = frame.Channels;
			this.reader = reader;
			this.sampleRate = frame.SampleRate;
			//int samples = frame.Version == MpegVersion.Version10 ? 1152 : 756;
			const int samples = 1152;
			int frameSize = ( samples / 8 ) * frame.Bitrate / frame.SampleRate + ( frame.Padding ? 1 : 0 ) - 4; // - 4 to account for header
			
			if( frame.ChannelMode == ChannelMode.SingleChannel || frame.ChannelMode == ChannelMode.Stereo
			   || frame.ChannelMode == ChannelMode.DualChannel ) {
				ReadSideData();
				int sideInfoSize = channels == 1 ? 17 : 32;
				reservoir.AddBytesFromStream( frameSize - sideInfoSize, reader.bytereader );
				Stream original = reader.bytereader.stream;
				reader.bytereader.stream = reservoir;
				ReadMainData();
				reader.bytereader.stream = original;
				return new byte[2 * 10000];
			} else {
				throw new NotImplementedException( "joint stereo implementation not done");
			}
		}
		
		
		class SideInfoBlocksplitFlag {
			public byte BlockType;
			public byte SwitchPoint;
			public byte[] TableSelect = new byte[2];
			public byte[] SubblockGain = new byte[3];
		}
		
		class SideInfoNoBlocksplitFlag {
			public byte[] TableSelect = new byte[3];
			public byte RegionAddress1;
			public byte RegionAddress2;
		}
		
		class SideInfoGranule {
			public int Part2_3Length;
			public int BigValues;
			public int GlobalGain;
			public byte ScalefacCompress;
			
			public bool BlocksplitFlag;
			public SideInfoBlocksplitFlag BlocksplitInfo;
			public SideInfoNoBlocksplitFlag NoBlocksplitInfo;
			
			public byte Preflag;
			public byte ScalefacScale;
			public byte Count1TableSelect;
		}
		
		SideInfoGranule[,] granules = new SideInfoGranule[2, numGranules];
		int[] scfsiBands = new int[2];
		void ReadSideData() {
			int mainDataEnd = reader.ReadBits( 9 );
			reservoir.SetReservoirOffset( mainDataEnd );
			reader.ReadBits( channels == 1 ? 5 : 3 ); // private bits
			
			for( int ch = 0; ch < channels; ch++ ) {
				int scfsi = 0;
				for( int band = 0; band < 4; band++ ) {
					scfsi |= reader.ReadBit() << band;
				}
				scfsiBands[ch] = scfsi;
			}
			
			for( int gr = 0; gr < numGranules; gr++ ) {
				for( int ch = 0; ch < channels; ch++ ) {
					SideInfoGranule granule = new SideInfoGranule();
					granule.Part2_3Length = reader.ReadBits( 12 );
					granule.BigValues = reader.ReadBits( 9 );
					granule.GlobalGain = reader.ReadBits( 8 );
					granule.ScalefacCompress = (byte)reader.ReadBits( 4 );
					granule.BlocksplitFlag = reader.ReadBit() != 0;
					if( granule.BlocksplitFlag ) {
						SideInfoBlocksplitFlag info = new SideInfoBlocksplitFlag();
						info.BlockType = (byte)reader.ReadBits( 2 );
						info.SwitchPoint = (byte)reader.ReadBit();
						for( int region = 0; region < 2; region++ ) {
							info.TableSelect[region] = (byte)reader.ReadBits( 5 );
						}
						for( int window = 0; window < 3; window++ ) {
							info.SubblockGain[window] = (byte)reader.ReadBits( 3 );
						}
						granule.BlocksplitInfo = info;
					} else {
						SideInfoNoBlocksplitFlag info = new SideInfoNoBlocksplitFlag();
						for( int region = 0; region < 3; region++ ) {
							info.TableSelect[region] = (byte)reader.ReadBits( 5 );
						}
						info.RegionAddress1 = (byte)reader.ReadBits( 4 );
						info.RegionAddress2 = (byte)reader.ReadBits( 3 );
						granule.NoBlocksplitInfo = info;
					}
					granule.Preflag = (byte)reader.ReadBit();
					granule.ScalefacScale = (byte)reader.ReadBit();
					granule.Count1TableSelect = (byte)reader.ReadBit();
					granules[ch, gr] = granule;
				}
			}
		}
		
		void ReadMainData() {
			int[,,,] scalefac = new int[numSubbands, 3, numGranules, channels];
			for ( int gr = 0; gr < numGranules; gr++ ) {
				for( int ch = 0; ch < channels; ch++ ) {
					// Part 2 data
					int part2Length = ReadScalefactors( ch, gr, scalefac );
					// Part 3 data
					
					SideInfoGranule granule = granules[ch, gr];
					int part3Length = granule.Part2_3Length - part2Length;
					if( granule.BlocksplitFlag ) System.Diagnostics.Debugger.Break();
					
					int[] samples = new int[576];
					int sampleIndex = 0;
					int reg0Subband = granule.NoBlocksplitInfo.RegionAddress1;
					int reg1Subband = granule.NoBlocksplitInfo.RegionAddress2;
					
					int[] sbEnds = sbEnds_long_32khz;
					if( sampleRate == 44100 ) sbEnds = sbEnds_long_441hz;
					else if( sampleRate == 48000 ) sbEnds = sbEnds_long_48khz;
					int reg1Start = sbEnds[reg0Subband + 1] + 1;
					int reg2Start = sbEnds[reg0Subband + 1 + reg1Subband + 1] + 1;
					//Console.WriteLine( "HUFFMAN START: " + reader.bytereader.Position );
					
					int bigValuesSamples = granule.BigValues * 2;
					for( ; sampleIndex < bigValuesSamples; ) {
						int table = 0;
						if( sampleIndex < reg1Start )
							table = granule.NoBlocksplitInfo.TableSelect[0];
						else if( sampleIndex < reg2Start )
							table = granule.NoBlocksplitInfo.TableSelect[1];
						else
							table = granule.NoBlocksplitInfo.TableSelect[2];
						
						Xy value = default( Xy );
						if( table != 0 ) {
							HuffmanTree<Xy> huffTree = GetBigValueTree( table );
							ReadHuffCode( huffTree, reader, table, out value, ref part3Length );
						}
						GetXySamples( value, samples, table, ref sampleIndex, ref part3Length );
					}
					
					HuffmanTree<int> countTree = GetQuadTree( granule.Count1TableSelect );
					while( part3Length > 0 ) {
						int value = 0;
						ReadHuffCode( countTree, reader, granule.Count1TableSelect, out value, ref part3Length );
						GetQuadSamples( value, samples, ref sampleIndex, ref part3Length );
					}
					System.Diagnostics.Debugger.Break();
				}
			}
			System.Diagnostics.Debugger.Break();
		}
		
		static void ReadHuffCode<T>( HuffmanTree<T> tree, BitReader reader, int table, out T value, ref int part3Len ) {
			uint huffCode = 0;
			int bits = 1;
			value = default( T );
			
			while( bits <= 20 ) {
				huffCode <<= 1;
				huffCode |= (uint)reader.ReadBit();
				part3Len--;
				if( tree.GetValue( huffCode, bits, out value ) ) {
					Console.WriteLine( "READING BITS: " + bits + "," + table + "," + value );
					return;
				}
				bits++;
			}
			throw new InvalidOperationException( "Tried to read more than 20 bits!" );
		}
		
		const int escCode = 15;
		void GetXySamples( Xy xy, int[] samples, int tableIndex, ref int index, ref int part3Len ) {
			for( int i = 0; i < 2; i++ ) {
				int s = i == 0 ? xy.X : xy.Y;
				if( s == escCode ) {
					int bits = linBits[tableIndex];
					if( bits != 0 ) {
						part3Len -= bits;
						s += reader.ReadBits( bits );
					}
				}
				if( s != 0 ) {
					part3Len--;
					if( reader.ReadBit() == 1 ) {
						s = -s;
					}
				}
				samples[index++] = s;
			}
		}
		
		void GetQuadSamples( int quad, int[] samples, ref int index, ref int part3Len ) {
			for( int i = 3; i >= 0; i-- ) {
				int s = quad & ( 1 << i );
				if( s != 0 ) {
					s = 1; // don't bother also doing >> i
					part3Len--;
					if( reader.ReadBit() == 1 ) {
						s = -s;
					}
				}
				samples[index++] = s;
			}
		}
	}
}