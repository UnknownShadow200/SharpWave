
using System;

namespace SharpWave.Codecs.Vorbis {
	
	public static class VorbisUtils {
		
		public static int iLog( int value ) {
			int bits = 0;
			while( value > 0 ) {
				bits++;
				value >>= 1;
			}
			return bits;
		}
		
		public static float Unpack( uint value ) {
			int mantissa = (int)( value & 0x1fffff );
			uint sign = value &0x80000000;
			uint exponent = ( value & 0x7fe00000 ) >> 21;
			if( sign != 0 ) {
				mantissa = -mantissa;
			}
			return mantissa * (float)Math.Pow( 2, exponent - 788 );
		}
		
		public static int lookup1_values( int entries, int dimensions ) {
			int value = 0;
			while( Pow( value + 1, dimensions ) <= entries ) {
				value++;
			}
			return value;
		}
		
		static long Pow( int baseNum, int exp ) {
			if( baseNum == 0 ) return 0;
			if( baseNum == 1 ) return 1;
			
			// TODO: exponentiation by squaring
			long value = 1;
			while( exp > 0 ) {
				value *= baseNum;
				exp--;
			}
			return value;
		}
	}
}
