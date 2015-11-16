using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Audio.OpenAL;
using System.Text;

namespace SharpWave.Codecs.Mpeg {
	
	public enum MpegVersion : byte {
		Version25 = 0x00,
		Reserved = 0x01,
		Version20 = 0x02,
		Version10 = 0x03,
	}
	
	public enum ChannelMode : byte {
		Stereo = 0, // stereo
		JointStereo = 1, // stereo
		DualChannel = 2, // two mono channels
		SingleChannel = 3, // mono
	}
	
	public class MpegFrame {
		public MpegVersion Version;
		public int SampleRate;
		public int Bitrate;
		public int Channels;
		public ChannelMode ChannelMode;
		public int ModeExtension;
		public int Emphasis;
		public bool Padding, CrcProtected;
	}
	
	public class Mp3Codec : ICodec {
		
		public string Name {
			get { return "MPEG 1/2 audio"; }
		}

		AudioChunk info;		
		void MidSideToLeftRight( float[] mid, float[] side, out float[] left, out float[] right ) {
			int len = Math.Min( mid.Length, side.Length );
			left = new float[len];
			right = new float[len];
			for( int i = 0; i < len; i++ ) {
				float midSample = mid[i], sideSample = side[i];
				left[i] = 0.70710678118f * ( midSample + sideSample ); // 1 / sqrt(2)
				right[i] = 0.70710678118f * ( midSample - sideSample );
			}
		}
		
		enum LayerIndex : byte {
			Reserved = 0,
			Layer3 = 1,
			Layer2 = 2,
			Layer1 = 3,
		}
		
		// Indexed by [mpegVersion][samplingIndex]
		static readonly int[][] samplingRates = {
			new[] { 11025, 12000, 8000, -1 },  // MPEG 2.5
			new[] { -1, -1, -1, -1 },          // Reserved
			new[] { 22050, 24000, 16000, -1 }, // MPEG 2
			new[] { 44100, 48000, 32000, -1 }, // MPEG 1
		};
		
		// Indexed by [layerIndex][bitrateIndex]
		static readonly int[][] mpeg10bitrates = {
			new[] { 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },            // Reserved
			new[] { 0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, -1 },     // Layer 3
			new[] { 0, 32, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 384, -1 },    // Layer 2
			new[] { 0, 32, 64, 96, 128, 160, 192, 224, 256, 288, 320, 352, 384, 416, 448, -1 }, // Layer 1
		};
		
		// Indexed by [layerIndex][bitrateIndex]
		static readonly int[][] mpeg2025bitrates = {
			new[] { 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },         // Reserved
			new[] { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, -1 },      // Layer 3
			new[] { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, -1 },      // Layer 2
			new[] { 0, 32, 48, 56, 64, 80, 96, 112, 128, 144, 160, 176, 192, 224, 256, -1 }, // Layer 1
		};
		
		public IEnumerable<AudioChunk> StreamData( Stream source ) {
			info = new AudioChunk();
			PrimitiveReader reader = new PrimitiveReader( source );
			while( true ) {
				// Read frame header
				BitReader bitReader = new BitReader( reader );
				bitReader.BigEndian = true;			
				//Console.WriteLine( "start pos" + reader.Position );
				
				// Skip any padding('00') bytes before the start of a frame
				byte data = 0;
				while( ( data = reader.ReadByte() ) == 0 );
				
				// Make sure that the 'data' byte is the first 8 bits of the sync word.
				if( data != 0xFF ) {
					throw new InvalidDataException( "Invalid frame sync value." );
				}
				int frameSync = bitReader.ReadBits( 3/*11*/ );
				if( frameSync != 0x7/*FF*/ ) {
					throw new InvalidDataException( "Invalid frame sync value." );
				}
				int versionId = bitReader.ReadBits( 2 );
				int layerIndex = bitReader.ReadBits( 2 );
				bool crcProtection = bitReader.ReadBit() == 0;
				int bitrateIndex = bitReader.ReadBits( 4 );
				int samplingRateIndex = bitReader.ReadBits( 2 );
				
				bool padded = bitReader.ReadBit() != 0;
				int privateBit = bitReader.ReadBit();
				int channelMode = bitReader.ReadBits( 2 );
				int modeExtension = bitReader.ReadBits( 2 );
				int copyrightBit = bitReader.ReadBit();
				int originalBit = bitReader.ReadBit();
				int emphasis = bitReader.ReadBits( 2 );
				
				int bitrate = GetBitRate( (MpegVersion)versionId, layerIndex, bitrateIndex );
				info.Frequency = samplingRates[versionId][samplingRateIndex];
				
				ushort crc = 0;
				if( crcProtection ) {
					crc = (ushort)bitReader.ReadBits( 16 );
				}
				MpegFrame frame = new MpegFrame();
				frame.Bitrate = bitrate;
				frame.ChannelMode = (ChannelMode)channelMode;
				frame.Channels = Common.GetNumberOfChannels( frame.ChannelMode );
				frame.CrcProtected = crcProtection;
				frame.Padding = padded;
				frame.ModeExtension = modeExtension;
				frame.SampleRate = info.Frequency;
				frame.Emphasis = emphasis;
				frame.Version = (MpegVersion)versionId;
				
				LayerIndex index2 = (LayerIndex)layerIndex;
				info.Data = null;
				//Console.WriteLine( "padding: {0}, type: {1}, sr: {2}, br: {3}",
				//frame.Padding, index2, frame.SampleRate, frame.Bitrate );
				if( layerIndex == (int)LayerIndex.Layer1 ) {
					info.Data = decoder1.Decode( frame, bitReader );
				} else if( layerIndex == (int)LayerIndex.Layer2 ) {
					info.Data = decoder2.Decode( frame, bitReader );
				} else if( layerIndex == (int)LayerIndex.Layer3 ) {
					throw new NotSupportedException( "Layer III not supported" );
				} else {
					throw new InvalidDataException( "Invalid layer" );
				}
				info.Channels = frame.Channels;
				info.BitsPerSample = 16;
				//if( bitReader.offset == 8 ) {
				//reader.ReadByte();
				//}
				yield return info;
			}
		}
		LayerDecoder decoder1 = new LayerIDecoder();
		LayerDecoder decoder2 = new LayerIIDecoder();
		
		static int GetBitRate( MpegVersion version, int layer, int index ) {
			if( version == MpegVersion.Version10 ) {
				return mpeg10bitrates[layer][index] * 1000;
			} else if( version == MpegVersion.Version20 || version == MpegVersion.Version25 ) {
				return mpeg2025bitrates[layer][index] * 1000;
			}
			throw new ArgumentException( "Unsupported version" + version );
		}
	}
}
