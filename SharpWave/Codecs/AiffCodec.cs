using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SharpWave.Transformers;

namespace SharpWave.Codecs {
	
	public class AiffCodec : ICodec {
		
		public string Name {
			get { return "Apple Audio Interchange File Format"; }
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
		int actualBitsPerSample;
		AudioChunk info;
		Transformer transformer;
		bool aifcFormat;
		
		public IEnumerable<AudioChunk> StreamData( Stream source ) {
			infoBuffer.Length = 0;
			info = new AudioChunk();
			PrimitiveReader reader = new PrimitiveReader( source );
			string signature = reader.ReadASCIIString( 4 );
			if( signature != "FORM" )
				throw new InvalidDataException( "Invalid initial signature." );
			
			reader.BigEndian = true;
			int formChunkSize = reader.ReadInt32();
			AppendInfoLine( 0, "-- Begin info --" );
			AppendInfoLine( 0, "{0} (Chunk size: {1} bytes, {2} KB, {3} MB)", signature,
			               formChunkSize, formChunkSize / 1024, formChunkSize / 1024 / 1024 );

			string format = reader.ReadASCIIString( 4 );
			switch( format ) {
				case "AIFF":
					break;
				
					//http://www-mmsp.ece.mcgill.ca/Documents/AudioFormats/AIFF/Docs/AIFF-C.9.26.91.pdf
				case "AIFC":
					aifcFormat = true;
					break;
					
				default:
					throw new InvalidDataException( "Invalid initial signature." );
			}
			
			AiffChunkHeader chunk;
			while( true ) {
				chunk = ReadChunkHeader( reader );
				AppendInfoLine( 1, "{0} (Chunk size: {1})", chunk.Signature, chunk.DataSize );
				if( chunk.Signature == "COMM" ) {
					ProcessCommonChunk( chunk, reader );
				} else if( chunk.Signature == "SSND" ) {
					break;
				} else {					
					SkipChunkData( reader, chunk.DataSize );
				}
			}
			AppendInfoLine( 0, "-- End info --" );
			fileinfo = infoBuffer.ToString();
			
			return StreamDataCore( chunk, reader );
		}
		
		IEnumerable<AudioChunk> StreamDataCore( AiffChunkHeader chunk, PrimitiveReader reader ) {
			int offset = reader.ReadInt32();
			int blockSize = reader.ReadInt32();
			
			long length = (uint)chunk.DataSize - 8; // Signed number to unsigned number.
			int bufferLength = bufferSize; // Approximately one second each.
			while( length > 0 ) {
				int currentBufferSize = (int)Math.Min( bufferLength, length );
				byte[] data = reader.ReadBytes( currentBufferSize );
				info.Data = transformer.Transform( data, actualBitsPerSample );
				length -= bufferLength;
				yield return info;
			}
		}
		
		void ProcessFormatVersionChunk( AiffChunkHeader chunk, PrimitiveReader reader ) {
			uint timeStamp = reader.ReadUInt32();
			AppendInfoLine( 2, "Format timestamp: {0} (hex {1})", timeStamp, timeStamp.ToString( "X8" ) );
		}
		
		void ProcessCommonChunk( AiffChunkHeader chunk, PrimitiveReader reader ) {
			byte[] chunkData = reader.ReadBytes( chunk.DataSize );
			Stream source = reader.stream;
			reader.stream = new MemoryStream( chunkData );
			int channelsCount = reader.ReadInt16();
			uint frameCount = reader.ReadUInt32();
			int bitsPerSample = reader.ReadInt16();
			byte[] sampleRateBytes = reader.ReadBytes( 10 );
			double sampleRate = ConvertFromIeeeExtended( sampleRateBytes );
			Console.WriteLine( sampleRate );			
			
			AppendInfoLine( 2, "Channels Count: {0}", channelsCount );
			AppendInfoLine( 2, "Sample rate (frames/sec): {0}", sampleRate );
			AppendInfoLine( 2, "Bits per sample: {0}", bitsPerSample );
			AppendInfoLine( 2, "Frame count: {0}", frameCount );
			
			int byteRate = (int)Math.Ceiling( sampleRate ) * channelsCount * bitsPerSample / 8;
			info.Frequency = (int)sampleRate;
			Console.WriteLine( "BPS:" + bitsPerSample + ", SR:" + info.Frequency );
			
			transformer = EmptyTransformer.Instance;
			if( bitsPerSample > 8 && bitsPerSample <= 16 ) {
				transformer = BigEndian16BitTo16BitTransformer.Instance;
			}
			if( bitsPerSample > 16 && bitsPerSample <= 24 ) {
				transformer = BigEndian24BitTo16BitTransformer.Instance;
			}
			// Number of bytes that make up a second's worth of audio data.
			bufferSize = byteRate;
			info.Channels = channelsCount;
			actualBitsPerSample = bitsPerSample;
			// TODO: Remove this hackery.
			if( bitsPerSample > 16 )
				bitsPerSample = 16;
			info.BitsPerSample = bitsPerSample;
			if( aifcFormat ) {
				string compressionType = reader.ReadASCIIString( 4 );
				string compressionName = reader.ReadASCIIString( reader.ReadByte() );
				AppendInfoLine( 2, "Compression type: {0}", compressionType );
				AppendInfoLine( 2, "Compression name: {0}", compressionName );
				switch( compressionType ) {
					case "NONE":
					case "sowt":
						break;
						
					case "alaw":
					case "ALAW":
						info.BitsPerSample = 16;
						transformer = ALawTransformer.Instance;
						break;
						
					case "ulaw":
					case "ULAW":
						info.BitsPerSample = 16;
						transformer = MuLawTransformer.Instance;
						break;
				}
			}
			reader.stream = source;
		}
		
		static AiffChunkHeader ReadChunkHeader( PrimitiveReader reader ) {
			AiffChunkHeader chunk = new AiffChunkHeader();
			chunk.Signature = reader.ReadASCIIString( 4 );
			chunk.DataSize = reader.ReadInt32();
			return chunk;
		}		

		static double UnsignedToFloat( uint u ) {
			return ( (int)( u - 2147483647L - 1 ) ) + 2147483648.0;
		}
		
		static double ldexp( double x, int exp ) {
			return x * Math.Pow( 2, exp ); //( 1 << exp ); // x * 2^exp
		}

		static double ConvertFromIeeeExtended( byte[] bytes ) {
			int exponent = ( bytes[0] & 0x7F ) << 8 | ( bytes[1] & 0xFF );
			uint hiMantissa = (uint)( bytes[2] << 24 | bytes[3] << 16 | bytes[4] << 8 | bytes[5] );
			uint loMantissa = (uint)( bytes[6] << 24 | bytes[7] << 16 | bytes[8] << 8 | bytes[9] );

			if( exponent == 0 && hiMantissa == 0 && loMantissa == 0 ) {
				return 0;
			} else {
				if( exponent == 0x7FFF ) { // Infinity or NaN
					return double.NaN;
				} else {
					double f = 0;
					exponent -= 16383;
					f  = ldexp( UnsignedToFloat( hiMantissa ), exponent -= 31 );
					f += ldexp( UnsignedToFloat( loMantissa ), exponent -= 32 );
					return ( bytes[0] & 0x80 ) != 0 ? -f : f;
				}
			}
		}
		
		struct AiffChunkHeader {
			public string Signature;
			public int DataSize;
		}
		
		void SkipChunkData( PrimitiveReader reader, long length ) {
			while( length > 0 ) {
				int skipSize = (int)Math.Min( 4096, length );
				reader.ReadBytes( skipSize );
				length -= 4096;
			}
		}
		
		string fileinfo = "";
		
		public string Info {
			get { return fileinfo; }
		}
	}
}