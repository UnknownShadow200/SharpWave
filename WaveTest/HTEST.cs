using System;
using System.IO;
using SharpWave.Utils;

namespace WaveTest {
	
	public class HTEST {
		
		byte[] codewordLengths = {
			2, 4, 4,
			4, 4, 2,
			3, 3
		};
		
		public void DoThingy() {
			BuildHuffmanTree();
			PrintNode( tree.RootNode, "" );
		}
		
		void PrintNode( HuffmanNode<int> node, string bits ) {
			if( node == null ) return;
			
			if( node.HasValue ) {
				Console.WriteLine( node.Value + " : " + bits );
			} else {
				PrintNode( node.Left, "0" + bits );
				PrintNode( node.Right, "1" + bits );
			}
		}
		
		HuffmanTree<int> tree;
		void BuildHuffmanTree() {
			tree = new HuffmanTree<int>();
			for( int i = 0; i < codewordLengths.Length; i++ ) {
				byte length = codewordLengths[i];
				
				HuffmanNode<int> node = tree.RootNode;
				nodeFound = false;
				InsertNode( i, node, length, "" );
				if( !nodeFound )
					throw new InvalidOperationException( "Could not find empty branch in huffman tree!" );
			}
		}
		
		bool nodeFound = false;
		void InsertNode( int value, HuffmanNode<int> node, int bitsLeft, string bits ) {
			if( nodeFound ) return;
			if( node.HasValue ) return;
			
			if( bitsLeft == 0 ) {
				// we cannot add a node if there are further children down the tree
				if( node.Left != null || node.Right != null ) return;
				Console.WriteLine( "ADDED" + value + " : " + bits );
				node.HasValue = true;
				node.Value = value;
				nodeFound = true;
			} else {
				// Keep going down the tree
				if( node.Left == null )
					node.Left = new HuffmanNode<int>();
				InsertNode( value, node.Left, bitsLeft - 1, bits + "0" );
				if( nodeFound ) return;
				
				if( node.Right == null )
					node.Right = new HuffmanNode<int>();
				InsertNode( value, node.Right, bitsLeft - 1, bits + "1" );
			}
		}
	}
}
