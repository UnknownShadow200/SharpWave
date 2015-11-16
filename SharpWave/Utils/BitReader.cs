using System;
using System.IO;

namespace SharpWave {
	
	public class BitReader {
		public BitReader( PrimitiveReader reader ) {
			if( reader == null )
				throw new ArgumentNullException( "reader" );
			bytereader = reader;
		}
		
		public PrimitiveReader bytereader;
		int offset = 8;
		byte lastByte;
		public bool BigEndian = false;
		
		/// <summary> Skips the remaining bits in the current byte being cached by the reader. </summary>
		/// <remarks> The next time ReadBit() or ReadBits() is called, the bits
		/// will start from the next byte read from the underlying stream. </remarks>
		public void SkipRemainingBits() {
			offset = 8; // Don't advance straight to next byte,
			// in case the reader is at the end of the stream.
		}
		
		public int ReadBit() {
			if( offset == 8 ) {
				offset = 0;
				lastByte = bytereader.ReadByte();
			}
			int value;
			if( BigEndian ) {
				value = ( lastByte & ( 1 << ( 7  - offset ) ) ) != 0 ? 1 : 0;
			} else {
				value = ( lastByte & ( 1 << offset ) ) != 0 ? 1 : 0;
			}
			//int value = BigEndian ? ( lastByte & ( 1 << ( 7  - offset ) ) ) != 0 ? 1 : 0
			//	:
			//	( lastByte & ( 1 << offset ) ) != 0 ? 1 : 0;
			offset++;
			return value;
		}
		
		public int ReadBits( int bitCount ) {
			if( bitCount == 0 ) return 0;
			int value = 0;
			for( int i = 0; i < bitCount; i++ ) {
				if( !BigEndian ) {
					value |= ( ReadBit() << i );
				} else {
					value |= ( ReadBit() << ( bitCount - 1 - i ) );
				}
			}
			return value;
		}
		
		public uint ReadBitsU( int bitCount ) {
			uint value = 0;
			// if.. else purposely moved outside the loop.
			if( !BigEndian ) {
				for( int i = 0; i < bitCount; i++ ) {
					value |= ( (uint)ReadBit() << i );
				}
			} else {
				for( int i = 0; i < bitCount; i++ ) {
					value |= ( (uint)ReadBit() << ( bitCount - 1 - i ) );
				}
			}
			return value;
		}
		
		public long ReadBits64( int bitCount ) {
			long value = 0;
			for( int i = 0; i < bitCount; i++ ) {
				if( !BigEndian ) {
					value |= ( (long)ReadBit() << i );
				} else {
					value |= ( (long)ReadBit() << ( bitCount - 1 - i ) );
				}
			}
			return value;
		}
		
		public ulong ReadBits64U( int bitCount ) {
			ulong value = 0;
			for( int i = 0; i < bitCount; i++ ) {
				if( !BigEndian ) {
					value |= ( (ulong)ReadBit() << i );
				} else {
					value |= ( (ulong)ReadBit() << ( bitCount - 1 - i ) );
				}
			}
			return value;
		}
		
		public int ReadSignedBits( int bitCount ) {
			int value = ReadBits( bitCount );
			value <<= ( 32 - bitCount );
			value >>= ( 32 - bitCount );
			return value;
		}
	}
}
