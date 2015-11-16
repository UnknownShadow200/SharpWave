using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace SharpWave.Containers.Mpeg {
	
	public class ID3v2Frame {
		
		byte majorVersion;
		byte revision;
		byte headerFlags;
		int dataSize;
		Mp3Container container;
		
		public override string ToString() {
			return "ID3 2." + majorVersion + "." + revision;
		}

		public ID3v2Frame( Mp3Container container, PrimitiveReader reader, byte majorVersion ) {
			revision = reader.ReadByte();
			headerFlags = reader.ReadByte();
			dataSize = ReadSyncSafeInt32( reader );
			reader.BigEndian = true;
			this.container = container;
			this.majorVersion = majorVersion;
			
			bool useUnsynch = ( headerFlags & 0x80 ) != 0;
			if( useUnsynch ) {
				reader.stream = new UnsynchronisationStream( reader.stream );
			}
			
			if( majorVersion == 3 ) {
				ReadV3( reader );
			} else {
				throw new NotImplementedException( "Support for ID3 version " + majorVersion + " not yet done." );
			}
			
			if( useUnsynch ) {
				reader.stream = ( (UnsynchronisationStream)reader.stream ).UnderlyingStream;
			}
		}
		
		#region V2
		
		void ReadV2( PrimitiveReader reader ) {
			int len = dataSize;		
			
			while( len >= 4 ) {
				if( ReadV2Frame( reader, ref len ) ) break;
			}
			reader.SkipData( len );
		}
		
		bool ReadV2Frame( PrimitiveReader reader, ref int len ) {
			string frameId = reader.ReadASCIIString( 4 ); len -= 4;
			if( frameId == "\0\0\0\0" ) return true;
			
			int frameDataSize = reader.ReadUInt24(); len -= ( frameDataSize + 3 );
			Console.WriteLine( "reading frame: " + frameId );
			Console.WriteLine( "   skipping frame." );
			reader.SkipData( frameDataSize );
			return false;
		}		
		
		#endregion
		
		#region V3
		
		void ReadV3( PrimitiveReader reader ) {
			int len = dataSize;		
			bool extendedHeader = ( headerFlags & 0x40 ) != 0;
			if( extendedHeader ) {
				ReadV3ExtendedHeader( reader, ref len );
			}
			
			while( len >= 4 ) {
				if( ReadV3Frame( reader, ref len ) ) break;
			}
			reader.SkipData( len );
		}
		
		void ReadV3ExtendedHeader( PrimitiveReader reader, ref int len ) {
			int extendedDataSize = reader.ReadInt32(); len -= ( 4 + extendedDataSize );
			ushort extendedFlags = reader.ReadUInt16();
			int paddingSize = reader.ReadInt32();
			extendedDataSize -= 6;
			
			if( ( extendedFlags & 0x8000 ) != 0 ) {
				uint crc32 = reader.ReadUInt32();
				extendedDataSize -= 4;
			}			
			reader.SkipData( extendedDataSize );
		}
		
		bool ReadV3Frame( PrimitiveReader reader, ref int len ) {
			string frameId = reader.ReadASCIIString( 4 ); len -= 4;
			if( frameId == "\0\0\0\0" ) return true;
			
			int frameDataSize = reader.ReadInt32(); 
			ushort frameFlags = reader.ReadUInt16(); len -= ( frameDataSize + 4 + 2 );
			
			bool encryption = ( frameFlags & 0x80 ) != 0;
			bool compression = ( frameFlags & 0x40 ) != 0;
			bool groupingInfo = ( frameFlags & 0x20 ) != 0;
			if( groupingInfo ) {
				byte groupIdentifier = reader.ReadByte();
			}
			if( encryption || compression ) throw new NotImplementedException( "encryption and/or compression support is not yet implemented." );
			Console.WriteLine( "reading frame: " + frameId );
			if( v3TagConstructors == null ) {
				v3TagConstructors = ID3v2Tag.Makev3TagConstructors();
			}
			
			Func<ID3v2Tag> constructor;
			if( v3TagConstructors.TryGetValue( frameId, out constructor ) ) {
				ID3v2Tag tag = constructor();
				tag.Identifier = frameId;
				tag.DataSize = frameDataSize;
				tag.Read( container, reader );
			} else {
				Console.WriteLine( "   skipping frame." );
				reader.SkipData( frameDataSize );
			}
			return false;
		}
		
		Dictionary<string, Func<ID3v2Tag>> v3TagConstructors;
		#endregion
		
		int ReadSyncSafeInt32( PrimitiveReader reader ) {
			byte[] buffer = reader.ReadBytes( 4 );
			return ( buffer[0] & 0x7F ) << 21 | ( buffer[1] & 0x7F ) << 14 |
				( buffer[2] & 0x7F ) << 7 | ( buffer[3] & 0x7F );
		}
		
		class UnsynchronisationStream : Stream {
			
			public Stream UnderlyingStream;
			
			public UnsynchronisationStream( Stream stream ) {
				UnderlyingStream = stream;
			}
			public bool CloseUnderlyingStream = false;
			
			public override bool CanRead {
				get { return UnderlyingStream.CanRead; }
			}
			
			public override bool CanSeek {
				get { return false; }
			}
			
			public override bool CanWrite {
				get { return UnderlyingStream.CanWrite; }
			}
			
			public override long Length {
				get { throw new NotImplementedException( "unsync streams do not support seeking." ); }
			}
			
			public override long Position {
				get { throw new NotImplementedException( "unsync streams do not support seeking." ); }
				set { throw new NotImplementedException( "unsync streams do not support seeking." ); }
			}
			
			public override long Seek( long offset, SeekOrigin origin ) {
				throw new NotImplementedException( "unsync streams do not support seeking." );
			}
			
			public override void SetLength( long value ) {
				throw new NotImplementedException( "unsync streams do not support seeking." );
			}
			
			public override void Close() {
				if( CloseUnderlyingStream ) {
					UnderlyingStream.Close();
				}
			}
			
			public override void Flush() {
				UnderlyingStream.Flush();
			}
			
			public override int Read( byte[] buffer, int offset, int count ) {
				int read = 0;
				for( int i = offset; i < offset + count; i++ ) {
					int value = UnderlyingStream.ReadByte();
					if( value == -1 ) {
						return read; // end of stream
					}
					if( value == 0xFF ) {
						UnderlyingStream.ReadByte(); // skip following 0x00
					}
					buffer[i] = (byte)value;
					read++;
				}
				return read;
			}
			
			public override void Write( byte[] buffer, int offset, int count ) {
				for( int i = offset; i < offset + count; i++ ) {
					byte value = buffer[i];
					UnderlyingStream.WriteByte( value );
					if( value == 0xFF ) {
						UnderlyingStream.WriteByte( 0x00 );
					}
				}
			}
		}
	}
}
