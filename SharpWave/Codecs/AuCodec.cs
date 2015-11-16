using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Audio.OpenAL;
using System.Text;

namespace SharpWave.Codecs {
	
	public class AuCodec : ICodec {
		
		public string Name {
			get { return "Sun Audio"; }
		}
		
		StringBuilder infoBuffer = new StringBuilder();
		
		void AppendInfoLine( int indention, string format, params object[] args ) {
			const int spacesPerIndention = 2;
			string line = String.Format( format, args );
			if( indention > 0 ) {
				line = new String( ' ', indention * spacesPerIndention ) + line;
			}
			infoBuffer.AppendLine( line );
		}
		int bufferSize;
		AudioChunk chunk;
		
		enum AuEncoding : uint {
			Int8G711uLaw = 1,
			Int8LinearPcm = 2,
			Int16LinearPcm = 3,
			Int24LinearPcm = 4,
			Int32LinearPcm = 5,
			Float32LinearPcm = 6,
			Float64LinearPcm = 7,
			
			Int4G721AdPcm = 23,
			Int1G722AdPcm = 24,
			Int3G723AdPcm = 25,
			Int5G723AdPcm = 26,
			Int8G711ALaw = 27,
		}
		
		static readonly int[] bitsPerSampleEncoding =
			new [] { -1, 8, 8, 16, 23, 32, 32, 64,
			-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
			-1, -1, -1, -1, -1, 4, 1, 3, 5, 8 };
		
		static readonly int[] paddedBitsPerSampleEncoding =
		new [] { -1, 8, 8, 16, 23, 32, 32, 64,
			-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
			-1, -1, -1, -1, -1, 8, 8, 8, 8, 8 };
		
		long dataLength;
		
		public IEnumerable<AudioChunk> StreamData( Stream source ) {
			infoBuffer.Length = 0;
			chunk = new AudioChunk();
			PrimitiveReader reader = new PrimitiveReader( source );
			string signature = reader.ReadASCIIString( 4 );
			if( signature != ".snd" ) {
				throw new InvalidDataException( "Invalid initial signature." );
			}
			
			AppendInfoLine( 0, "-- Begin info --" );
			reader.BigEndian = true;
			uint dataOffset = reader.ReadUInt32();
			AppendInfoLine( 0, "Data offset: {0}", dataOffset );
			uint dataSize = reader.ReadUInt32();
			if( dataSize == 0xFFFFFFFF ) {
				dataLength = source.Length - dataOffset;
			} else {
				dataLength = dataSize;
			}
			AppendInfoLine( 0, "Data length: {0}", dataLength );
			AuEncoding encoding = (AuEncoding)reader.ReadUInt32();
			AppendInfoLine( 0, "Encoding: {0}", encoding );
			uint sampleRate = reader.ReadUInt32();
			AppendInfoLine( 0, "Sample rate: {0}", sampleRate );
			uint channels = reader.ReadUInt32();
			AppendInfoLine( 0, "Channels: {0}", channels );
			if( dataOffset > 24 ) {
				int infoLength = (int)( dataOffset - 24 );
				string info = reader.ReadASCIIString( infoLength );
				AppendInfoLine( 0, "Info: {0}", info );
			}
			int bitsPerSample = bitsPerSampleEncoding[(int)encoding];
			int adjustedBitsPerSample = paddedBitsPerSampleEncoding[(int)encoding];
			bufferSize = (int)( sampleRate * channels * adjustedBitsPerSample / 8 );

			AppendInfoLine( 0, "-- End info --" );
			fileinfo = infoBuffer.ToString();
			
			return StreamDataCore( reader );
		}
		
		#if DEBUG_POS
		long counter = 0;
		#endif
		IEnumerable<AudioChunk> StreamDataCore( PrimitiveReader reader ) {
			long length = dataLength;
			int bufferLength = bufferSize; // Approximately one second each.
			while( length > 0 ) {
				int currentBufferSize = (int)Math.Min( bufferLength, length );
				chunk.Data = reader.ReadBytes( currentBufferSize );
				length -= bufferLength;
				#if DEBUG_POS
				counter++;
				Console.WriteLine( "Returned {0} chunks. (Still {1} bytes left)", counter, length );
				#endif
				yield return chunk;
			}
		}
		
		string fileinfo = "";
		
		public string Info {
			get { return fileinfo; }
		}
	}
}