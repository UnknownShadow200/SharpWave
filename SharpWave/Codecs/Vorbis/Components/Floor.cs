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
		int floor1Values;
		
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
			CalcFloor1Values();
			
			xList = new int[floor1Values];
			int xListIndex = 2;
			xList[1] = 1 << rangeBits;
			for( int i = 0; i < partitions; i++ ) {
				int classNumber = partitionClassList[i];
				for( int j = 0; j < classDimensions[classNumber]; j++ ) {
					xList[xListIndex++] = reader.ReadBits( rangeBits );
				}
			}
			
			int range = rangeElements[multiplier - 1];
			elems01Bits = VorbisUtils.iLog( range - 1 );
			yList = new int[floor1Values];
			step2Flag = new bool[floor1Values];
			finalY = new int[floor1Values];
			floor = new int[floor1Values];
		}
		
		void CalcFloor1Values() {
			int len = 2;
			for( int i = 0; i < partitions; i++ ) {
				int classNumber = partitionClassList[i];
				len += classDimensions[classNumber];
			}
			floor1Values = len;
		}
		
		static int[] rangeElements = new int[] { 256, 128, 84, 64 };
		int[] yList, finalY, floor;
		bool[] step2Flag;
		int elems01Bits;
		
		public override object ReadPerPacketData( VorbisCodec codec, BitReader reader, object args ) {
			bool emptyThisFrame = reader.ReadBit() == 0;
			if( emptyThisFrame ) return true;
			
			int offset = 0;
			yList[offset++] = reader.ReadBits( elems01Bits );
			yList[offset++] = reader.ReadBits( elems01Bits );
			
			for( int i = 0; i < partitions; i++ ) {
				int classNum = partitionClassList[i];
				int cDim = classDimensions[classNum];
				int cBits = classSubclasses[classNum];
				int cSub = (1 << cBits) - 1;
				int cVal = 0;
				if( cBits > 0 ) {
					int bookNum = classMasterbooks[classNum];
					Codebook codebook = codec.codebookConfigs[bookNum];
					cVal = codebook.GetScalarContext( reader );
				}
				
				for( int j = 0; j < cDim; j++ ) {
					int book = subclassBooks[classNum][cVal & cSub];
					cVal = cVal >> cBits;
					if( book >= 0 ) {
						Codebook codebook = codec.codebookConfigs[book];
						yList[offset++] = codebook.GetScalarContext( reader );
					} else {
						yList[offset++] = 0;
					}
				}
			}
			return false;
		}
		
		public override object ApplyToFrame( VorbisCodec codec, BitReader reader, object args ) {
			step2Flag[0] = true; step2Flag[1] = true;
			finalY[0] = yList[0]; finalY[1] = yList[1];
			SynthesiseAmplitudeValues();
			return SynthesiseCurve( (int)args );
		}
		
		void SynthesiseAmplitudeValues() {
			int range = rangeElements[multiplier - 1];
			for( int i = 2; i < xList.Length; i++ ) {
				int lo = lowNeighbour( xList, i );
				int hi = highNeighbour( xList, i );
				int predicted = renderPoint( xList[lo], finalY[lo],
				                            xList[hi], finalY[hi], xList[i] );
				int val = yList[i];
				
				if( val == 0 ) {
					step2Flag[i] = false;
					finalY[i] = predicted;
					continue;
				}

				step2Flag[lo] = true;
				step2Flag[i] = true;
				step2Flag[hi] = true;
				int hiRoom = range - predicted, loRoom = predicted;
				int room = (hiRoom < loRoom) ? hiRoom * 2 : loRoom * 2;
				if( val >= room ) {
					if( hiRoom > loRoom ) {
						finalY[i] = val - loRoom + predicted;
					} else {
						finalY[i] = predicted - val + hiRoom - 1;
					}
				} else {
					if( (val & 1) == 1 ) {
						finalY[i] = predicted - ((val + 1) / 2);
					} else {
						finalY[i] = predicted + (val / 2);
					}
				}
			}
		}
		
		double[] SynthesiseCurve( int n ) {
			int hx = 0, lx = 0;
			int ly = finalY[0] * multiplier, hy = 0;
			for( int i = 0; i < floor1Values; i++ )
				floor[i] = 0; // TODO: unncessary or not?
			
			for( int i = 0; i < floor1Values; i++ ) {
				if( step2Flag[i] ) {
					hx = xList[i]; hy = finalY[i] * multiplier;
					renderLine( lx, ly, hx, hy, floor );
					lx = hx; ly = hy;
				}
			}
			
			if( hx < n )
				renderLine( hx, hy, n, hy, floor );
			double[] result = new double[n]; // TODO: avoid reallocating all the time
			for( int i = 0; i < result.Length; i++ ) {
				result[i] = lookupTable[floor[i]];
			}
			return result;
		}
		
		static int lowNeighbour( int[] v, int x ) {
			int pos = 0, lo = -65536, vX = v[x];
			for( int n = 0; n < x; n++ ) {
				if( v[n] < vX && v[n] > lo ) {
					pos = n; lo = v[n];
				}
			}
			return pos;
		}
		
		static int highNeighbour( int[] v, int x ) {
			int pos = 0, hi = 65536, vX = v[x];
			for( int n = 0; n < x; n++ ) {
				if( v[n] > v[x] && v[n] > hi ) {
					pos = n; hi = v[n];
				}
			}
			return pos;
		}
		
		static int renderPoint( int x0, int y0, int x1, int y1, int X ) {
			int dy = y1 - y0, ady = Math.Abs( dy );
			int err = ady * (X - x0);
			int off = err / (x1 - x0);
			return dy < 0 ? (y0 - off) : (y0 + off);
		}
		
		static void renderLine( int x0, int y0, int x1, int y1, int[] V ) {
			int dy = y1 - y0, ady = Math.Abs( dy );
			int adx = x1 - x0, base_ = dy / adx;
			int err = 0;
			int sy = dy < 0 ? (base_ - 1) : (base_ + 1);
			ady -= Math.Abs( base_) * adx;
			
			int y = y0; V[x0] = y;
			for( int x = x0 + 1; x < x1; x++ ) {
				err += ady;
				if( err >= adx ) {
					err -= adx;
					y += sy;
				} else {
					y += base_;
				}
				V[x] = y;
			}
		}
		
		static double[] lookupTable = new double[] {
			1.0649863e-07, 1.1341951e-07, 1.2079015e-07, 1.2863978e-07, 1.3699951e-07, 1.4590251e-07, 1.5538408e-07, 1.6548181e-07,
			1.7623575e-07, 1.8768855e-07, 1.9988561e-07, 2.1287530e-07, 2.2670913e-07, 2.4144197e-07, 2.5713223e-07, 2.7384213e-07,
			2.9163793e-07, 3.1059021e-07, 3.3077411e-07, 3.5226968e-07, 3.7516214e-07, 3.9954229e-07, 4.2550680e-07, 4.5315863e-07,
			4.8260743e-07, 5.1396998e-07, 5.4737065e-07, 5.8294187e-07, 6.2082472e-07, 6.6116941e-07, 7.0413592e-07, 7.4989464e-07,
			7.9862701e-07, 8.5052630e-07, 9.0579828e-07, 9.6466216e-07, 1.0273513e-06, 1.0941144e-06, 1.1652161e-06, 1.2409384e-06,
			1.3215816e-06, 1.4074654e-06, 1.4989305e-06, 1.5963394e-06, 1.7000785e-06, 1.8105592e-06, 1.9282195e-06, 2.0535261e-06,
			2.1869758e-06, 2.3290978e-06, 2.4804557e-06, 2.6416497e-06, 2.8133190e-06, 2.9961443e-06, 3.1908506e-06, 3.3982101e-06,
			3.6190449e-06, 3.8542308e-06, 4.1047004e-06, 4.3714470e-06, 4.6555282e-06, 4.9580707e-06, 5.2802740e-06, 5.6234160e-06,
			5.9888572e-06, 6.3780469e-06, 6.7925283e-06, 7.2339451e-06, 7.7040476e-06, 8.2047000e-06, 8.7378876e-06, 9.3057248e-06,
			9.9104632e-06, 1.0554501e-05, 1.1240392e-05, 1.1970856e-05, 1.2748789e-05, 1.3577278e-05, 1.4459606e-05, 1.5399272e-05,
			1.6400004e-05, 1.7465768e-05, 1.8600792e-05, 1.9809576e-05, 2.1096914e-05, 2.2467911e-05, 2.3928002e-05, 2.5482978e-05,
			2.7139006e-05, 2.8902651e-05, 3.0780908e-05, 3.2781225e-05, 3.4911534e-05, 3.7180282e-05, 3.9596466e-05, 4.2169667e-05,
			4.4910090e-05, 4.7828601e-05, 5.0936773e-05, 5.4246931e-05, 5.7772202e-05, 6.1526565e-05, 6.5524908e-05, 6.9783085e-05,
			7.4317983e-05, 7.9147585e-05, 8.4291040e-05, 8.9768747e-05, 9.5602426e-05, 0.00010181521, 0.00010843174, 0.00011547824,
			0.00012298267, 0.00013097477, 0.00013948625, 0.00014855085, 0.00015820453, 0.00016848555, 0.00017943469, 0.00019109536,
			0.00020351382, 0.00021673929, 0.00023082423, 0.00024582449, 0.00026179955, 0.00027881276, 0.00029693158, 0.00031622787,
			0.00033677814, 0.00035866388, 0.00038197188, 0.00040679456, 0.00043323036, 0.00046138411, 0.00049136745, 0.00052329927,
			0.00055730621, 0.00059352311, 0.00063209358, 0.00067317058, 0.00071691700, 0.00076350630, 0.00081312324, 0.00086596457,
			0.00092223983, 0.00098217216, 0.0010459992,  0.0011139742,  0.0011863665,  0.0012634633,  0.0013455702,  0.0014330129,
			0.0015261382,  0.0016253153,  0.0017309374,  0.0018434235,  0.0019632195,  0.0020908006,  0.0022266726,  0.0023713743,
			0.0025254795,  0.0026895994,  0.0028643847,  0.0030505286,  0.0032487691,  0.0034598925,  0.0036847358,  0.0039241906,
			0.0041792066,  0.0044507950,  0.0047400328,  0.0050480668,  0.0053761186,  0.0057254891,  0.0060975636,  0.0064938176,
			0.0069158225,  0.0073652516,  0.0078438871,  0.0083536271,  0.0088964928,  0.009474637,   0.010090352,   0.010746080,
			0.011444421,   0.012188144,   0.012980198,   0.013823725,   0.014722068,   0.015678791,   0.016697687,   0.017782797,
			0.018938423,   0.020169149,   0.021479854,   0.022875735,   0.024362330,   0.025945531,   0.027631618,   0.029427276,
			0.031339626,   0.033376252,   0.035545228,   0.037855157,   0.040315199,   0.042935108,   0.045725273,   0.048696758,
			0.051861348,   0.055231591,   0.058820850,   0.062643361,   0.066714279,   0.071049749,   0.075666962,   0.080584227,
			0.085821044,   0.091398179,   0.097337747,   0.10366330,    0.11039993,    0.11757434,    0.12521498,    0.13335215,
			0.14201813,    0.15124727,    0.16107617,    0.17154380,    0.18269168,    0.19456402,    0.20720788,    0.22067342,
			0.23501402,    0.25028656,    0.26655159,    0.28387361,    0.30232132,    0.32196786,    0.34289114,    0.36517414,
			0.38890521,    0.41417847,    0.44109412,    0.46975890,    0.50028648,    0.53279791,    0.56742212,    0.60429640,
			0.64356699,    0.68538959,    0.72993007,    0.77736504,    0.82788260,    0.88168307,    0.9389798,     1.00000000,
		};
	}
}
