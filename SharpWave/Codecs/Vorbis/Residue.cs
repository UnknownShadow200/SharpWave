using System;

namespace SharpWave.Codecs.Vorbis {
	
	public abstract class Residue : IVorbisComponent {
		
		public override void ReadSetupData( VorbisCodec codec, BitReader reader ) {
			int residueBegin = reader.ReadBits( 24 );
			int residueEnd = reader.ReadBits( 24 );
			int partitionSize = reader.ReadBits( 24 ) + 1;
			int classifications = reader.ReadBits( 6 ) + 1;
			int classbook = reader.ReadBits( 8 );
			
			byte[] residueCascade = new byte[classifications];
			for( int i = 0; i < residueCascade.Length; i++ ) {
				int highBits = 0;
				int lowBits = reader.ReadBits( 3 );
				if( reader.ReadBit() == 1 )
					highBits = reader.ReadBits( 5 );
				residueCascade[i] = (byte)( lowBits | ( highBits << 3 ) );
			}
			
			byte[][] bookNumbers = new byte[classifications][];
			for( int i = 0; i < bookNumbers.Length; i++ ) {
				byte[] nums = new byte[8];
				int cascade = residueCascade[i];
				for( int j = 0; j < nums.Length; j++ ) {
					if( ( cascade & ( 1 << j ) ) != 0 ) {
						nums[j] = (byte)reader.ReadBits( 8 );
					}
				}
				bookNumbers[i] = nums;
			}
		}	
	}
	
	public class Residue0 : Residue {
		
	}
	
	public class Residue1 : Residue {

	}
	
	public class Residue2 : Residue {
		
	}
}
