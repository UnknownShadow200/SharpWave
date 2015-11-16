using System;

namespace SharpWave.Codecs.Flac {
	
	public class FlacBitReader : BitReader {
		public FlacBitReader( PrimitiveReader reader )
			: base( reader ) {
		}
		
		public int ReadUnary() {
			int value = 0;
			int bit = ReadBit();
			while( bit == 0 ) {
				value++;
				bit = ReadBit();
			}
			return value;
		}
		
		public int ReadRice( int parameter ) {
			int msbs = ReadUnary();
			int lsbs = ReadBits( parameter );

			uint value = ( (uint)msbs << parameter ) | (uint)lsbs;			
			return ( value & 0x01 ) != 0 ? (int)~( value >> 1 ) : (int)( value >> 1 );
		}		
	}
}
