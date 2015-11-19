using System;

namespace SharpWave.Codecs.Vorbis {
	
	public struct ResidueDecodeArgs {
		public bool[] doNotDecode;
		public int ch;
		public int blockSize;
	}
	
	public abstract class Residue : IVorbisComponent {
		
		protected int residueBegin, residueEnd;
		protected int partitionSize, classifications, classbook;
		protected byte[][] bookNumbers;
		
		public override void ReadSetupData( VorbisCodec codec, BitReader reader ) {
			residueBegin = reader.ReadBits( 24 );
			residueEnd = reader.ReadBits( 24 );
			partitionSize = reader.ReadBits( 24 ) + 1;
			classifications = reader.ReadBits( 6 ) + 1;
			classbook = reader.ReadBits( 8 );
			
			byte[] residueCascade = new byte[classifications];
			for( int i = 0; i < residueCascade.Length; i++ ) {
				int highBits = 0;
				int lowBits = reader.ReadBits( 3 );
				if( reader.ReadBit() == 1 )
					highBits = reader.ReadBits( 5 );
				residueCascade[i] = (byte)(lowBits | (highBits << 3));
			}
			
			bookNumbers = new byte[classifications][];
			for( int i = 0; i < bookNumbers.Length; i++ ) {
				byte[] nums = new byte[8];
				int cascade = residueCascade[i];
				for( int j = 0; j < nums.Length; j++ ) {
					if( (cascade & (1 << j)) != 0 ) {
						nums[j] = (byte)reader.ReadBits( 8 );
					}
				}
				bookNumbers[i] = nums;
			}
		}

		public override object ReadPerPacketData( VorbisCodec codec, BitReader reader, object args ) {
			ResidueDecodeArgs rArgs = (ResidueDecodeArgs)args;
			int actualSize = rArgs.blockSize / 2;
			if( this is Residue2 )
				actualSize *= rArgs.ch;
			
			int limBegin = Math.Max( residueBegin, actualSize );
			int limEnd = Math.Max( residueEnd, actualSize );
			int numToRead = limEnd - limBegin;
			if( numToRead == 0 ) return null;
			
			int partitionsToRead = numToRead / partitionSize;
			Codebook book = codec.codebookConfigs[classbook];
			int classwordsPerCodeword = book.dimensions;			
			byte[][] classificationsArr = new byte[rArgs.ch][];
			
			for( int pass = 0; pass < 8; pass++ ) {
				int partitionCount = 0;
				while( partitionCount < partitionsToRead ) {
					
					if( pass == 0 ) {
						for( int j = 0; j < rArgs.ch; j++ ) {
							if( rArgs.doNotDecode[j] ) continue;
							
							byte[] tempArray = new byte[classwordsPerCodeword];
							int temp = book.GetScalarContext( reader );
							for( int i = classwordsPerCodeword - 1; i >= 0; i-- ) {
								tempArray[i + partitionCount] = (byte)(temp % classifications);
								temp /= classifications;
							}
							classificationsArr[j] = tempArray;
						}
					}
					
					for( int i = 0; i < classwordsPerCodeword && partitionCount < partitionsToRead; i++ ) {
						for( int j = 0; j < rArgs.ch; j++ ) {
							int vqClass = classificationsArr[j][partitionCount];
							int vqBook = bookNumbers[vqClass][pass];
						}
						partitionCount++;
					}
				}
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
