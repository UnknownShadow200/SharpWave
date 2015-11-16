using System;
using System.IO;
using SharpWave.Codecs;
using SharpWave.Codecs.Mpeg;

namespace SharpWave.Containers.Ogg {
	
	public class MpegContainer : IMediaContainer {

		public MpegContainer( Stream s ) : base( s ) {
		}
		
		PrimitiveReader reader;
		BitReader bitReader;
		public override void ReadMetadata() {
			reader = new PrimitiveReader( stream );
			bitReader = new BitReader( reader );
			bitReader.BigEndian = true;
		}
		
		public override ICodec GetAudioCodec() {
			return new Mp3Codec();
		}
		
		public override void Flush() {
			stream.Flush();
		}
		
		public override long Length {
			get { throw new NotSupportedException( "Seeking in mpeg streams not supported." ); }
		}
		
		public override long Position {
			get { throw new NotSupportedException( "Seeking in mpeg streams not supported." ); }
			set { throw new NotSupportedException( "Seeking in mpeg streams not supported." ); }
		}

		public override long Seek( long offset, SeekOrigin origin ) {
			throw new NotSupportedException( "Seeking in mpeg streams not supported." );
		}
		
		int packetLength;
		int packetOffset;
		byte[] packetData;
		
		public override int Read( byte[] buffer, int offset, int count ) {
			if( count == 0 ) return 0;
			int read = 0;
			
			while( count > 0 ) {
				while( packetLength <= 0 ) {
					if( !ReadMpegData() ) return read;
				}
				
				CopyPacketSegment( ref offset, ref count, ref read, buffer );
				if( packetOffset >= packetLength )
					packetLength = 0;
			}
			return read;
		}
		
		public override int ReadByte() {
			while( packetLength <= 0 ) {
				if( !ReadMpegData() ) return -1;
			}
			
			byte value = packetData[packetOffset++];
			if( packetOffset >= packetLength )
				packetLength = 0;
			return value;
		}
		
		void CopyPacketSegment( ref int bufOffset, ref int count, ref int read, byte[] buffer ) {
			int bytesToCopy = Math.Min( count, packetLength - packetOffset );
			Buffer.BlockCopy( packetData, packetOffset, buffer, bufOffset, bytesToCopy );
			
			bufOffset += bytesToCopy;
			read += bytesToCopy;
			packetOffset += bytesToCopy;
			count -= bytesToCopy;
		}
		
		uint code = uint.MaxValue;
		bool ReadMpegData() {
			code = bitReader.ReadBitsU( 32 );
			if( ( code >> 8 ) != 0x000001 ) {
				throw new InvalidOperationException( "Invalid code: " + code );
			}
			uint dataType = code & 0xFF;
			
			if( dataType == 0xB9 ) {
				return false; // end of stream
			} else if( dataType == 0xBA ) {
				ReadPack();
			} else if( dataType == 0xBB ) {
				ReadSystemHeader();
			} else {
				ReadPacket( (int)dataType );
			}
			return true;
		}
		
		void ReadPack() {
			int version = bitReader.ReadBits( 2 );
			if( version == 0x00 ) { // MPEG 1
				bitReader.ReadBits( 2 );
				long scr = Read33BitsWithMarkers();
				bitReader.ReadBit();
				int muxRate = bitReader.ReadBits( 22 );
				bitReader.ReadBit();
			} else {
				long scr = Read33BitsWithMarkers();
				int scrExtension = bitReader.ReadBits( 9 );
				bitReader.ReadBit();
				int muxRate = bitReader.ReadBits( 22 );
				bitReader.ReadBits( 7 );
				int stuffingLength = bitReader.ReadBits( 3 );
				for( int i = 0; i < stuffingLength; i++ ) {
					bitReader.ReadBits( 8 ); // TODO: ReadByte
				}
			}
		}
		
		void ReadPacket( int streamId ) {
			int dataLengthBytes = bitReader.ReadBits( 16 );
			if( streamId != 0xBF ) {
				int type = 0;
				type = bitReader.ReadBits( 2 );
				while( type == 0x03 ) { // Stuffing
					bitReader.ReadBits( 6 );
					dataLengthBytes--;
				}
				
				if( type == 0x0 ) {
					int subType = bitReader.ReadBits( 2 );
					if( subType == 0x00 ) {
						bitReader.ReadBits( 4 );
						dataLengthBytes--;
					} else if( subType == 0x2 ) {
						long pts = Read33BitsWithMarkers();
						dataLengthBytes -= 5;
					} else if( subType == 0x3 ) {
						long pts = Read33BitsWithMarkers();
						bitReader.ReadBits( 4 );
						long dts = Read33BitsWithMarkers();
						dataLengthBytes -= 10;
					}
				} else if( type == 0x02 ) {
					int bufferScale = bitReader.ReadBit();
					int bufferSizeBound = bitReader.ReadBits( 13 );
					dataLengthBytes -= 2;
				}
			}
			if( ( streamId & 0xE0 ) == 0xC0 ) {
				packetLength = dataLengthBytes;
				packetOffset = 0;
				packetData = reader.ReadBytes( packetLength );
			} else {
				reader.SkipData( dataLengthBytes );
			}
		}
		
		long Read33BitsWithMarkers() {
			int bits30_32 = bitReader.ReadBits( 3 );
			bitReader.ReadBit();
			int bits15_29 = bitReader.ReadBits( 15 );
			bitReader.ReadBit();
			int bits0_14 = bitReader.ReadBits( 15 );
			bitReader.ReadBit();
			return ( (long)bits30_32 << 30 ) | ( bits15_29 << 15 ) | bits0_14;
		}
		
		void ReadSystemHeader() {
			int dataLengthBytes = bitReader.ReadBits( 16 );
			bitReader.ReadBit();
			int rateBound = bitReader.ReadBits( 22 );
			bitReader.ReadBit();
			
			int audioBound = bitReader.ReadBits( 6 );
			int fixedFlag = bitReader.ReadBit();
			int cspsFlag = bitReader.ReadBit();
			
			int audioLockFlag = bitReader.ReadBit();
			int videoLockFlag = bitReader.ReadBit();
			bitReader.ReadBit();
			int videoBound = bitReader.ReadBits( 5 );
			bitReader.ReadBits( 8 );
			
			dataLengthBytes -= 3 + 1 + 2;
			while( dataLengthBytes > 0 ) {
				int streamId = bitReader.ReadBits( 8 );
				bitReader.ReadBits( 2 );
				
				int bufferScale = bitReader.ReadBit();
				int bufferSizeBound = bitReader.ReadBits( 13 );
				dataLengthBytes -= 3;
			}
			if( dataLengthBytes < 0 ) {
				throw new InvalidOperationException( "Read past end of MPEG system header." );
			}
		}
	}
}
