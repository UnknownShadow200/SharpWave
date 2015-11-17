using System;

namespace SharpWave.Utils {
	
	public class HuffmanTree<T> {
		
		public HuffmanNode<T> RootNode = new HuffmanNode<T>();
		
		public static HuffmanTree<T> MakeTree( Func<string, Tuple<string, T>> lineParser, params string[] lines ) {
			HuffmanTree<T> tree = new HuffmanTree<T>();
			for( int i = 0; i < lines.Length; i++ ) {
				Tuple<string, T> parsedLine = lineParser( lines[i] );
				string bits = parsedLine.Key;
				T value = parsedLine.Value;
				
				HuffmanNode<T> node = tree.RootNode;
				for( int j = 0; j < bits.Length; j++ ) {
					int bit = bits[j] - '0';
					if( bit == 0 ) {
						if( node.Left == null ) {
							node.Left = new HuffmanNode<T>();						
						}
						node = node.Left;
					} else if( bit == 1 ) {
						if( node.Right == null ) {
							node.Right = new HuffmanNode<T>();						
						}
						node = node.Right;
					}
				}
				node.HasValue = true;
				node.Value = value;
			}
			return tree;
		}
		
		public bool GetValue( string bits, out T value ) {
			HuffmanNode<T> node = RootNode;
			for( int j = 0; j < bits.Length; j++ ) {
				int bit = bits[j] - '0';
				node = bit == 0 ? node.Left : node.Right;
				if( node == null ) {
					value = default( T );
					return false;
				}
			}
			value = node.Value;
			return node.HasValue;
		}
		
		public bool GetValue( uint bits, int bitsNumber, out T value ) {
			HuffmanNode<T> node = RootNode;
			uint shift = 1u << (bitsNumber - 1);
			for( int j = 0; j < bitsNumber; j++ ) {
				node = (bits & shift) == 0 ? node.Left : node.Right;
				if( node == null ) {
					value = default( T );
					return false;
				}
				shift >>= 1;
			}
			value = node.Value;
			return node.HasValue;
		}
	}
	
	public class HuffmanNode<T> {
		
		public HuffmanNode<T> Left; // 0
		public HuffmanNode<T> Right; // 1
		
		public T Value;
		public bool HasValue = false;
	}
}
