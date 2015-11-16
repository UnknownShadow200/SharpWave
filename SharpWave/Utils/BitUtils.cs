using System;

namespace SharpWave {
	
	/// <summary> Class with common utilities relating to bits. </summary>
	public unsafe static class BitUtils {
		
		public static byte RotateLeft( byte value, int bits ) {
			return (byte)( ( value << bits ) | ( value >> ( 8 - bits ) ) );
		}
		
		public static ushort RotateLeft( ushort value, int bits ) {
			return (ushort)( ( value << bits ) | ( value >> ( 16 - bits ) ) );
		}
		
		public static uint RotateLeft( uint value, int bits ) {
			return ( value << bits ) | ( value >> ( 32 - bits ) );
		}
		
		public static ulong RotateLeft( ulong value, int bits ) {
			return (ulong)( ( value << bits ) | ( value >> ( 64 - bits ) ) );
		}
		
		public static byte RotateRight( byte value, int bits ) {
			return (byte)( ( value >> bits ) | ( value << ( 8 - bits ) ) );
		}
		
		public static ushort RotateRight( ushort value, int bits ) {
			return (ushort)( ( value >> bits ) | ( value << ( 16 - bits ) ) );
		}
		
		public static uint RotateRight( uint value, int bits ) {
			return ( value >> bits ) | ( value << ( 32 - bits ) );
		}
		
		public static ulong RotateRight( ulong value, int bits ) {
			return (ulong)( ( value >> bits ) | ( value << ( 64 - bits ) ) );
		}
		
		public static bool IsBitSet( uint value, byte bit ) {
			return ( value & ( 1 << bit ) ) != 0;
		}
		
		public static bool IsBitSet( ushort value, byte bit ) {
			return ( value & ( 1 << bit ) ) != 0;
		}
		
		public static bool IsBitSet( ulong value, byte bit ) {
			return ( value & ( 1u << bit ) ) != 0;
		}
		
		public static ushort Reverse( ushort value ) {
			return (ushort)
				(
					( ( value & 0x00FF ) << 8 ) |
					
					( ( value & 0xFF00 ) >> 8 )
				);
		}
		
		public static uint Reverse( uint value ) {
			return (uint)
				(
					( ( value & 0x000000FF ) << 24 ) |
					( ( value & 0x0000FF00 ) << 8 ) |
					
					( ( value & 0x00FF0000 ) >> 8 ) |
					( ( value & 0xFF000000 ) >> 24 )
				);
		}
		
		public static uint Reverse24( uint value ) {
			return (uint)
				(
					( ( value & 0x0000FF ) << 16 ) |
					( ( value & 0xFF0000 ) >> 16 )
				);
		}
		
		public static ulong Reverse( ulong value ) {
			return (ulong)
				(
					( ( value & 0x00000000000000FF ) << 56 ) |
					( ( value & 0x000000000000FF00 ) << 40 ) |
					( ( value & 0x0000000000FF0000 ) << 24 ) |
					( ( value & 0x00000000FF000000 ) << 8 ) |
					
					( ( value & 0x000000FF00000000 ) >> 8 ) |
					( ( value & 0x0000FF0000000000 ) >> 24 ) |
					( ( value & 0x00FF000000000000 ) >> 40 ) |
					( ( value & 0xFF00000000000000 ) >> 56 )
				);
		}
		
		public static ulong ToUInt64( byte[] value, int offset ) {
			fixed( byte* ptr = value ) {
				return *(ulong*)&ptr[offset];
			}
		}
		
		public static uint ToUInt32( byte[] value, int offset ) {
			fixed( byte* ptr = value ) {
				return *(uint*)&ptr[offset];
			}
		}
		
		public static ushort ToUInt16( byte[] value, int offset ) {
			fixed( byte* ptr = value ) {
				return *(ushort*)&ptr[offset];
			}
		}
		
		public static byte[] GetBytes( ulong value ) {
			byte[] bytes = new byte[8];
			fixed( byte* ptr = bytes ) {
				*(ulong*)ptr = value;
			}
			return bytes;
		}
		
		public static byte[] GetBytes( uint value ) {
			byte[] bytes = new byte[4];
			fixed( byte* ptr = bytes ) {
				*(uint*)ptr = value;
			}
			return bytes;
		}
		
		public static byte[] GetBytes( ushort value ) {
			byte[] bytes = new byte[2];
			fixed( byte* ptr = bytes ) {
				*(ushort*)ptr = value;
			}
			return bytes;
		}
		
		public static string ToHexString( byte[] array ) {
			char[] hexadecimal = new char[array.Length * 2];
			for( int i = 0; i < array.Length; i++ ) {
				int value = array[i];
				int upperNibble = value >> 4;
				int lowerNibble = value & 0x0F;
				hexadecimal[i << 1] = upperNibble < 10 ? (char)( upperNibble + 48 ) : (char)( upperNibble + 55 ); // 48 = index of 0, 55 = index of (A - 10).
				hexadecimal[( i << 1 ) + 1] = lowerNibble < 10 ? (char)( lowerNibble + 48 ) : (char)( lowerNibble + 55 );
			}
			return new String( hexadecimal );
		}
		
		public static string ToBinaryString( byte[] array ) {
			char[] binary = new char[array.Length * 8];
			for( int i = 0; i < array.Length; i++ ) {
				int value = array[i];
				for( int j = 0; j < 8; j++ ) {
					binary[( i << 3 ) + j] = ( value & ( 1 << ( 7 - j ) ) ) != 0 ? '1' : '0';
				}
			}
			return new String( binary );
		}
		
		public static string ToBinaryString( byte value ) {
			char[] binary = new char[8];
			for( int i = 0; i < binary.Length; i++ ) {
				binary[i] = ( value & ( 1 << ( 7 - i ) ) ) != 0 ? '1' : '0';
			}
			return new String( binary );
		}
		
		public static string ToBinaryString( ushort value ) {
			char[] binary = new char[16];
			for( int i = 0; i < binary.Length; i++ ) {
				binary[i] = ( value & ( 1 << ( 15 - i ) ) ) != 0 ? '1' : '0';
			}
			return new String( binary );
		}
		
		public static string ToBinaryString( uint value ) {
			char[] binary = new char[32];
			for( int i = 0; i < binary.Length; i++ ) {
				binary[i] = ( value & ( 1 << ( 31 - i ) ) ) != 0 ? '1' : '0';
			}
			return new String( binary );
		}
	}
}