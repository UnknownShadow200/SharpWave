using System;
using System.IO;
using SharpWave.Utils;

namespace SharpWave.Codecs.Vorbis {
	
	public class Codebook : IVorbisComponent {
		
		byte[] codewordLengths;
		ushort[] multiplicands;
		float[][] VQ;
		public override void ReadSetupData( VorbisCodec codec, BitReader reader ) {
			int syncPattern = reader.ReadBits( 24 );
			if( syncPattern != 0x564342 ) {
				throw new InvalidDataException( "Invalid codebook sync pattern: " + syncPattern );
			}
			
			int dimensions = reader.ReadBits( 16 );
			int entries = reader.ReadBits( 24 );
			int ordered = reader.ReadBit();
			codewordLengths = new byte[entries];
			if( ordered == 0 ) {
				int sparse = reader.ReadBit();
				for( int i = 0; i < entries; i++ ) {
					if( sparse == 0 || ( reader.ReadBit() == 1 ) ) {
						codewordLengths[i] = (byte)( reader.ReadBits( 5 ) + 1 );
					}
				}
			} else {
				int curEntry = 0;
				int curLength = reader.ReadBits( 5 ) + 1;
				while( curEntry < entries ) {
					int number = reader.ReadBits( VorbisUtils.iLog( entries - curEntry ) );
					for( int i = curEntry; i < curEntry + number; i++ ) {
						codewordLengths[i] = (byte)curLength;
					}
					curEntry += number;
					curLength++;
				}
			}
			
			BuildHuffmanTree();
			
			int lookupType = reader.ReadBits( 4 );
			if( lookupType == 1 || lookupType == 2 ) {
				float minValue = VorbisUtils.Unpack( reader.ReadBitsU( 32 ) );
				float deltaValue = VorbisUtils.Unpack( reader.ReadBitsU( 32 ) );
				int valueBits = reader.ReadBits( 4 ) + 1;
				int sequenceP = reader.ReadBit();
				int lookupValues = 0;
				if( lookupType == 1 ) {
					lookupValues = VorbisUtils.lookup1_values( entries, dimensions );
				} else {
					lookupValues = entries * dimensions;
				}
				multiplicands = new ushort[lookupValues];
				for( int i = 0; i < multiplicands.Length; i++ ) {
					multiplicands[i] = (ushort)reader.ReadBits( valueBits );
				}
				
				VQ = new float[entries][];
				for( int i = 0; i < entries; i++ ) {
					float[] vector = new float[dimensions];
					if( lookupType == 1 ) {
						float last = 0;
						int indexDivisor = 1;
						for( int j = 0; j < dimensions; j++ ) {
							int multiplicandOffset = ( i / indexDivisor ) % lookupValues;
							vector[j] = multiplicands[multiplicandOffset] * deltaValue + minValue + last;
							if( sequenceP != 0 ) last = vector[j];
							
							indexDivisor *= lookupValues;
						}
					} else {
						float last = 0;
						int mulitiplicandOffset = i * dimensions;
						for( int j = 0; j < dimensions; j++ ) {
							vector[j] = multiplicands[mulitiplicandOffset] * deltaValue + minValue + last;
							if( sequenceP != 0 ) last = vector[j];
							
							mulitiplicandOffset++;
						}
					}
					VQ[i] = vector;
				}
			}
		}
		
		public int GetScalarContext( BitReader reader ) {
			uint huffCode = 0;
			int bits = 1;
			int value = 0;
			
			while( bits <= 32 ) {
				huffCode <<= 1;
				huffCode |= (uint)reader.ReadBit();
				
				if( tree.GetValue( huffCode, bits, out value ) ) 
					break;
				bits++;
			}
			return value;
		}
		
		public float[] GetVQContext( BitReader reader ) {
			int offset = GetScalarContext( reader );
			return VQ[offset];
		}
		
		HuffmanTree<int> tree;
		void BuildHuffmanTree() {
			tree = new HuffmanTree<int>();
			for( int i = 0; i < codewordLengths.Length; i++ ) {
				byte length = codewordLengths[i];
				if( length == 0 ) continue;
				
				HuffmanNode<int> node = tree.RootNode;
				nodeFound = false;
				InsertNode( i, node, length );
				if( !nodeFound )
					throw new InvalidOperationException( "Could not find empty branch in huffman tree!" );
			}
		}
		
		bool nodeFound = false;
		void InsertNode( int value, HuffmanNode<int> node, int depth ) {
			if( nodeFound ) return;
			if( node.HasValue ) return;
			
			if( depth == 0 ) {
				// we cannot add a node if there are further children down the tree
				if( node.Left != null || node.Right != null ) return;
				
				node.HasValue = true;
				node.Value = value;
				nodeFound = true;
			} else {
				// Keep going down the tree
				if( node.Left == null )
					node.Left = new HuffmanNode<int>();
				InsertNode( value, node.Left, depth - 1 );			
				if( nodeFound ) return;
				
				if( node.Right == null )
					node.Right = new HuffmanNode<int>();
				InsertNode( value, node.Right, depth - 1 );
			}
		}
	}
}
