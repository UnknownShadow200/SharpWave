using System;

namespace SharpWave.Codecs.Vorbis {
	
	public abstract class Floor : IVorbisComponent {		
	}
	
	public class Floor0 : Floor {
		
		public override void ReadSetupData( VorbisCodec codec, BitReader reader ) {
			throw new NotSupportedException( "Floor0 not supported." );
		}
		
		public override object ReadPerPacketData( VorbisCodec codec, BitReader reader, object data ) {
			throw new NotSupportedException( "Floor0 not supported." );
		}
	}
	
	public class Floor1 : Floor {
		
		int partitions;
		int multiplier;
		byte[] partitionClassList;
		byte[] classDimensions;
		byte[] classSubclasses;
		byte[] classMasterbooks;
		byte[][] subclassBooks;
		int[] xList;
		
		public override void ReadSetupData( VorbisCodec codec, BitReader reader ) {
			partitions = reader.ReadBits( 5 );
			int maximumClass = -1;
			partitionClassList = new byte[partitions];
			for( int i = 0; i < partitionClassList.Length; i++ ) {
				int element = reader.ReadBits( 4 );
				if( element > maximumClass ) {
					maximumClass = element;
				}
				partitionClassList[i] = (byte)element;
			}
			
			classDimensions = new byte[maximumClass + 1];
			classSubclasses = new byte[maximumClass + 1];
			classMasterbooks = new byte[maximumClass + 1];
			subclassBooks = new byte[maximumClass + 1][];
			
			for( int i = 0; i <= maximumClass; i++ ) {
				classDimensions[i] = (byte)( reader.ReadBits( 3 ) + 1 );
				int subclass = reader.ReadBits( 2 );
				classSubclasses[i] = (byte)subclass;
				if( subclass > 0 ) {
					classMasterbooks[i] = (byte)reader.ReadBits( 8 );
				}
				
				int subclassMax = 1 << subclass;
				byte[] books = new byte[subclassMax];
				for( int j = 0; j < books.Length; j++ ) {
					books[j] = (byte)( reader.ReadBits( 8 ) - 1 );
				}
				subclassBooks[i] = books;
			}
			
			multiplier = reader.ReadBits( 2 ) + 1;
			int rangeBits = reader.ReadBits( 4 );
			int floor1Length = 2;
			for( int i = 0; i < partitions; i++ ) {
				int classNumber = partitionClassList[i];
				floor1Length += classDimensions[classNumber];
			}
			
			xList = new int[floor1Length];
			int xListIndex = 2;
			xList[1] = 1 << rangeBits;
			for( int i = 0; i < partitions; i++ ) {
				int classNumber = partitionClassList[i];
				for( int j = 0; j < classDimensions[classNumber]; j++ ) {
					xList[xListIndex++] = reader.ReadBits( rangeBits );
				}
			}
		}
		
		static int[] rangeElements = new int[] { 256, 128, 84, 64 };
		bool emptyThisFrame;
		int[] yList;
		
		public override object ReadPerPacketData( VorbisCodec codec, BitReader reader, object data ) {
			emptyThisFrame = reader.ReadBit() == 0;
			if( emptyThisFrame ) return true;
			
			int range = rangeElements[multiplier - 1];
			int length = 2;
			for( int i = 0; i < partitions; i++ ) {
				int classNum = partitionClassList[i];
				length += classDimensions[classNum];
			}
			yList = new int[length];
			yList[0] = reader.ReadBits( VorbisUtils.iLog( range - 1 ) );
			yList[1] = reader.ReadBits( VorbisUtils.iLog( range - 1 ) );
			int offset = 2;
			
			for( int i = 0; i < partitions; i++ ) {
				int classNum = partitionClassList[i];
				int cDim = classDimensions[classNum];
				int cBits = classSubclasses[classNum];
				int cSub = ( 1 << cBits ) - 1;
				int cVal = 0;
				if( cBits > 0 ) {
					int bookNum = classMasterbooks[classNum];
					Codebook codebook = codec.codebookConfigs[bookNum];
					cVal = codebook.GetScalarContext( reader );
				}
				
				for( int j = 0; j < cDim; j++ ) {
					int book = subclassBooks[classNum][cVal & cSub];
					cVal <<= cBits;
					if( book > 0 ) {
						Codebook codebook = codec.codebookConfigs[book];
						yList[offset + j] = codebook.GetScalarContext( reader );
					}
				}
				offset += cDim;
			}
			return false;
		}
		
		public override void ApplyToFrame( VorbisCodec codec, BitReader reader ) {
			int range = rangeElements[multiplier - 1];
			bool[] step2Flag = new bool[xList.Length];
			step2Flag[0] = true;
			step2Flag[1] = true;
			int[] finalY = new int[xList.Length];
			finalY[0] = yList[0];
			finalY[1] = yList[1];
			
			for( int i = 2; i < xList.Length; i++ ) {
				
			}
		}
		
		int lowNeighbour( int[] v, int x ) {
			int pos = 0;
			for( int n = 0; n < x; n++ ) {
				if( v[n] < v[x] && v[n] >= v[pos] ) pos = n;
			}
			return pos;
		}
		
		int highNeighbour( int[] v, int x ) {
			int pos = 0;
			for( int n = 0; n < x; n++ ) {
				if( v[n] > v[x] && v[n] < v[pos] ) pos = n;
			}
			return pos;
		}
	}
}
