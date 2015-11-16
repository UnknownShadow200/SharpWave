//http://scratchpad.wikia.com/wiki/MPEG-1_Audio_Layers_I_and_II
//http://keyj.emphy.de/kjmp2/
//http://www.jonolick.com/code.html
using System;

namespace SharpWave.Codecs.Mpeg {
	
	public sealed class LayerIIDecoder : LayerDecoder {
		
		const int subbands = 32;
		const int samplesPerSubband = 36;
		const int totalSamples = subbands * samplesPerSubband;
		const int samplesPerGranule = 3;
		const int granules = samplesPerSubband / samplesPerGranule;
		
		int channels;
		BitReader reader;
		
		int FindIndex( MpegFrame frame ) {
			if( frame.Version == MpegVersion.Version20 ) {
				return -1;
			}
			int sampleRate = frame.SampleRate;
			int bitRate = frame.Bitrate / frame.Channels;

			if( sampleRate == 48000 ) {
				if( bitRate >= 56000 && bitRate <= 192000 ) {
					return 0;
				} else if( bitRate >= 32000 && bitRate <= 48000 ) {
					return 2;
				}
			} else if( sampleRate == 44100 ) {
				if( bitRate >= 56000 && bitRate <= 80000 ) {
					return 0;
				} else if( bitRate >= 96000 && bitRate <= 192000 ) {
					return 1;
				} else if( bitRate >= 32000 && bitRate <= 48000 ) {
					return 2;
				}
			} else if( sampleRate == 32000 ) {
				if( bitRate >= 56000 && bitRate <= 80000 ) {
					return 0;
				} else if( bitRate >= 96000 && bitRate <= 192000 ) {
					return 1;
				} else if( bitRate == 32000 || bitRate == 48000 ) {
					return 3;
				}
			}
			throw new InvalidOperationException( "couldn't find bit table" );
		}
		
		byte[][] GetTableInfoIndexer( MpegFrame frame ) {
			int index = FindIndex( frame );
			Console.WriteLine( "table " + (char)( 'A' + index ) );
			return index == -1 ? table_QuantIndicesMpeg2 : 
				table_QuantIndices[index];
		}
		
		byte[] GetBitAllocSizeTable( MpegFrame frame ) {
			int index = FindIndex( frame );
			return index == -1 ? table_BitsAllocSizeMpeg2 :
				table_BitsAllocSize[index];
		}
		
		void ReadBitAllocation( byte[,] allocation, byte[] allocSize ) {
			int sblimit = Math.Min( subbands, allocSize.Length );
			for( int sb = 0; sb < sblimit; sb++ ) {
				int bits = allocSize[sb];
				for( int ch = 0; ch < channels; ch++ ) {
					allocation[sb, ch] = (byte)reader.ReadBits( bits );
				}
			}
		}
		
		void ReadScaleFactorInfo( byte[,] allocation, byte[,] scalefactorInfo ) {
			for( int sb = 0; sb < subbands; sb++ ) {
				for( int ch = 0; ch < channels; ch++ ) {
					if( allocation[sb, ch] != 0 ) {
						scalefactorInfo[sb, ch] = (byte)reader.ReadBits( 2 );
					}
				}
			}
		}
		
		void ReadScaleFactors( byte[,] allocation, byte[,] scalefactorInfo, byte[,,] scaleIndices ) {
			for( int sb = 0; sb < subbands; sb++ ) {
				for( int ch = 0; ch < channels; ch++ ) {
					if( allocation[sb, ch] != 0 ) {
						int type = scalefactorInfo[sb, ch];
						if( type == 0 ) {
							scaleIndices[sb, ch, 0] = (byte)reader.ReadBits( 6 );
							scaleIndices[sb, ch, 1] = (byte)reader.ReadBits( 6 );
							scaleIndices[sb, ch, 2] = (byte)reader.ReadBits( 6 );
						} else if( type == 1 ) {
							byte index01 = (byte)reader.ReadBits( 6 );
							scaleIndices[sb, ch, 0] = index01;
							scaleIndices[sb, ch, 1] = index01;
							scaleIndices[sb, ch, 2] = (byte)reader.ReadBits( 6 );
						} else if( type == 2 ) {
							byte index012 = (byte)reader.ReadBits( 6 );
							scaleIndices[sb, ch, 0] = index012;
							scaleIndices[sb, ch, 1] = index012;
							scaleIndices[sb, ch, 2] = index012;
						} else if( type == 3 ) {
							scaleIndices[sb, ch, 0] = (byte)reader.ReadBits( 6 );
							byte index12 = (byte)reader.ReadBits( 6 );
							scaleIndices[sb, ch, 1] = index12;
							scaleIndices[sb, ch, 2] = index12;
						}
					}
				}
			}
		}
		
		void ReadSamples( byte[,] allocation, byte[][] indices, int[,,] samples ) {
			for( int gr = 0; gr < granules; gr++ ) {
				for( int sb = 0; sb < subbands; sb++ ) {
					for( int ch = 0; ch < channels; ch++ ) {
						if( allocation[sb, ch] != 0 ) {
							TableInfo info = tables[indices[sb][allocation[sb, ch]]];
							int bits = info.Bits;
							
							if( info.SamplesPerCodeword == 1 ) {
								for( int s = 0; s < 3; s++ ) {
									samples[gr * 3 + s, sb, ch] = reader.ReadBits( bits );
								}
							} else {
								int code = reader.ReadBits( bits );
								// Degroup the sample code into 3 samples
								for( int s = 0; s < 3; s++ ) {
									samples[gr * 3 + s, sb, ch] = code % info.Steps;
									code /= info.Steps;
								}
							}
						}
					}
				}
			}
		}
		
		void RequantiseSamples( int s, double[,] bandTbl, byte[,] allocation, int[,,] samples, byte[][] indices, byte[,,] scaleIndices ) {
			for( int sb = 0; sb < subbands; sb++ ) {
				for( int ch = 0; ch < channels; ch++ ) {
					if( allocation[sb, ch] != 0 ) {
						TableInfo info = tables[indices[sb][allocation[sb, ch]]];
						int sample = samples[s, sb, ch];
						int x = info.MostSignificantBit;

						double adder = ( sample >> x ) != 0 ? 0 : -1;
						sample &= ( 1 << x ) - 1;
						double fractionalisedNum = sample / (double)( 1 << x ) + adder;
						
						double value = info.C * ( fractionalisedNum + info.D );
						value *= Common.LayerI_II_ScaleFactors[scaleIndices[sb, ch, s / 12]];
						bandTbl[ch, sb] = value;
					}
				}
			}
		}
		
		double[] V0 = new double[1024], V1 = new double[1024];
		public override byte[] Decode( MpegFrame frame, BitReader reader ) {
			int channels = frame.Channels;
			byte[,] allocation = new byte[subbands, channels];
			byte[,,] scaleIndices = new byte[subbands, channels, 3];
			byte[,] scalefactorInfo = new byte[subbands, channels];
			int[,,] samples = new int[samplesPerSubband, subbands, channels];
			byte[] output = new byte[totalSamples * channels * 2];
			this.channels = channels;
			this.reader = reader;
			int index = 0;

			if( frame.ChannelMode == ChannelMode.SingleChannel || frame.ChannelMode == ChannelMode.Stereo
			   || frame.ChannelMode == ChannelMode.DualChannel ) {
				
				byte[][] tableInfo = GetTableInfoIndexer( frame );
				byte[] allocSizeTable = GetBitAllocSizeTable( frame );
				ReadBitAllocation( allocation, allocSizeTable );
				ReadScaleFactorInfo( allocation, scalefactorInfo );
				ReadScaleFactors( allocation, scalefactorInfo, scaleIndices );
				ReadSamples( allocation, tableInfo, samples );
				
				for( int s = 0; s < samplesPerSubband; s++ ) {
					double[,] bandTbl = new double[channels, subbands];
					RequantiseSamples( s, bandTbl, allocation, samples, tableInfo, scaleIndices );
					
					if( channels == 1 ) {
						double[] samples0 = Common.SynthesisSubbandFilter( 0, bandTbl, V0, subbands );
						for( int sb = 0; sb < subbands; sb++ ) {
							Common.OutputSample( samples0[sb], output, ref index );
						}
					} else {
						double[] samples0 = Common.SynthesisSubbandFilter( 0, bandTbl, V0, subbands );
						double[] samples1 = Common.SynthesisSubbandFilter( 1, bandTbl, V1, subbands );
						for( int sb = 0; sb < subbands; sb++ ) {
							Common.OutputSample( samples0[sb], output, ref index );
							Common.OutputSample( samples1[sb], output, ref index );
						}
					}
				}
				return output;
			} else {
				throw new NotImplementedException( "joint stereo implementation not done");
			}
		}
		
		struct TableInfo {
			public int Steps;
			public double C;
			public double D;
			public int SamplesPerCodeword;
			public int Bits;
			public int MostSignificantBit;
			
			public TableInfo( int steps, double c, double d, int group, int bits ) {
				Steps = steps;
				C = c;
				D = d;
				SamplesPerCodeword = group;
				Bits = bits;
				
				MostSignificantBit = 0;
				while( ( 1 << MostSignificantBit ) < Steps ) {
					MostSignificantBit++;
				}
				MostSignificantBit--;
			}
		}

		// Instead of storing each table entry as an individual TableInfo member,
		// I've chosen to instead store them as indices into the tables array.
		// This reduces the lines from ~945 to ~108, and also reduces memory usage.
		TableInfo[] tables = new TableInfo[] {
			new TableInfo( 3, 1.33333333333, 0.50000000000, 3, 5 ), // 0
			new TableInfo( 5, 1.60000000000, 0.50000000000, 3, 7 ), // 1
			new TableInfo( 7, 1.14285714286, 0.25000000000, 1, 3 ), // 2
			new TableInfo( 9, 1.77777777777, 0.50000000000, 3, 10 ), // 3
			new TableInfo( 15, 1.06666666666, 0.12500000000, 1, 4 ), // 4
			new TableInfo( 31, 1.03225806452, 0.06250000000, 1, 5 ), // 5
			new TableInfo( 63, 1.01587301587, 0.03125000000, 1, 6 ), // 6
			new TableInfo( 127, 1.00787401575, 0.01562500000, 1, 7 ), // 7
			new TableInfo( 255, 1.00392156863, 0.00781250000, 1, 8 ), // 8
			new TableInfo( 511, 1.00195694716, 0.00390625000, 1, 9 ), // 9
			new TableInfo( 1023, 1.00097751711, 0.00195312500, 1, 10 ), // 10
			new TableInfo( 2047, 1.00048851979, 0.00097656250, 1, 11 ), // 11
			new TableInfo( 4095, 1.00024420024, 0.00048828125, 1, 12 ), // 12
			new TableInfo( 8191, 1.00012208522, 0.00024414063, 1, 13 ), // 13
			new TableInfo( 16383, 1.00006103888, 0.00012207031, 1, 14 ), // 14
			new TableInfo( 32767, 1.00003051851, 0.00006103516, 1, 15 ), // 15
			new TableInfo( 65535, 1.00001525902, 0.00003051758, 1, 16 ), // 16
		};
		
		byte[][][] table_QuantIndices = new byte[][][] {
			new byte[][] { // table A, limit = 27
				tableA1, tableA1, tableA1,
				tableA2, tableA2, tableA2, tableA2,
				tableA2, tableA2, tableA2, tableA2,
				tableA3, tableA3, tableA3, tableA3,
				tableA3, tableA3, tableA3, tableA3,
				tableA3, tableA3, tableA3, tableA3,
				tableA4, tableA4, tableA4, tableA4,
			},
			new byte[][] { // table B, limit = 30
				tableA1, tableA1, tableA1,
				tableA2, tableA2, tableA2, tableA2,
				tableA2, tableA2, tableA2, tableA2,
				tableA3, tableA3, tableA3, tableA3,
				tableA3, tableA3, tableA3, tableA3,
				tableA3, tableA3, tableA3, tableA3,
				tableA4, tableA4, tableA4, tableA4,
				tableA4, tableA4, tableA4,
			},
			new byte[][] { // table C, limit = 8
				tableD1, tableD1, tableD2, tableD2, tableD2,
				tableD2, tableD2, tableD2, tableD2, tableD2,
			},
			new byte[][] { // table D, limit = 12
				tableD1, tableD1, tableD2, tableD2, tableD2, tableD2,
				tableD2, tableD2, tableD2, tableD2, tableD2, tableD2,
			},
		};
		
		byte[][] table_QuantIndicesMpeg2 = new byte[][] {
			table21, table21, table21, table21,
			table22, table22, table22, table22,
			table22, table22, table22, table23,
			table23, table23, table23, table23,
			table23, table23, table23, table23,
			table23, table23, table23, table23,
			table23, table23, table23, table23,
			table23, table23,
		};

		byte[][] table_BitsAllocSize = new byte[][] {
			new byte[] {
				4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 3, 3, 3, 3, 3,
				3, 3, 3, 3, 3, 3, 3, 2, 2, 2, 2,
			},
			new byte[] {
				4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 3, 3, 3, 3, 3,			
				3, 3, 3, 3, 3, 3, 3, 2, 2, 2, 2, 2, 2, 2,
			},
			new byte[] { 4, 4, 3, 3, 3, 3, 3, 3, },
			new byte[] { 4, 4, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, },
		};
		
		
		static byte[] table_BitsAllocSizeMpeg2 = new byte[] {
			4, 4, 4, 4, 3, 3, 3, 3, 3, 3, 3, 2, 2, 2, 2, 2,
			2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 
		};
		
		static byte[] tableA1 = new byte[] { 0xFF, 0, 2, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
		static byte[] tableA2 = new byte[] { 0xFF, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 16 };
		static byte[] tableA3 = new byte[] { 0xFF, 0, 1, 2, 3, 4, 5, 16 };
		static byte[] tableA4 = new byte[] { 0xFF, 0, 15, 14 };
		
		static byte[] tableD1 = new byte[] { 0xFF, 0, 1, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 };
		static byte[] tableD2 = new byte[] { 0xFF, 0, 1, 3, 4, 5, 6, 7 };
		
		static byte[] table21 = new byte[] { 0xFF, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };
		static byte[] table22 = new byte[] { 0xFF, 0, 1, 3, 4, 5, 6, 7 };
		static byte[] table23 = new byte[] { 0xFF, 0, 1, 3, 4 };
	}
}