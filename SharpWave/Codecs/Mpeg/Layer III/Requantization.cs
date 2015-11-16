using System;
using SharpWave.Utils;

namespace SharpWave.Codecs.Mpeg {
	
	public sealed partial class LayerIIIDecoder {
		
		static double[] Imdct( double[] X, int N ) {
			double[] Y = new double[N * 2];
			double invN = 1.0 / N;
			double invPiN = Math.PI / N;
			
			for( int n = 0; n < N * 2; n++ ) {
				double bracket1 = n + 0.5 + N * 0.5;
				double sum = 0;
				
				for( int k = 0; k < N; k++ ) {
					double bracket2 = k + 0.5;
					sum += X[k] * Math.Cos( invPiN * bracket1 * bracket2 );
				}
				Y[n] = invN * sum;
			}
			return Y;
		}
		
		// Windowing		
		static double[] Window_BlockType0( double[] X ) {
			const double piOver36 = Math.PI / 36;
			double[] Z = new double[36];
			
			for( int i = 0; i < Z.Length; i++ ) {
				Z[i] = X[i] * Math.Sin( piOver36 * ( i + 0.5 ) );
			}
			return Z;
		}
		
		static double[] Window_BlockType1( double[] X ) {
			const double piOver36 = Math.PI / 36;
			const double piOver12 = Math.PI / 12;
			double[] Z = new double[36];
			
			for( int i = 0; i <= 17; i++ ) {
				Z[i] = X[i] * Math.Sin( piOver36 * ( i + 0.5 ) );
			}
			for( int i = 18; i <= 23; i++ ) {
				Z[i] = X[i];
			}
			for( int i = 24; i <= 29; i++ ) {
				Z[i] = X[i] * Math.Sin( piOver12 * ( i - 17.5 ) );
			}
			return Z;
		}
		
		static double[] Window_BlockType3( double[] X ) {
			const double piOver36 = Math.PI / 36;
			const double piOver12 = Math.PI / 12;
			double[] Z = new double[36];
			
			for( int i = 6; i <= 11; i++ ) {
				Z[i] = X[i] * Math.Sin( piOver12 * ( i - 5.5 ) );
			}
			for( int i = 12; i <= 17; i++ ) {
				Z[i] = X[i];
			}
			for( int i = 18; i <= 35; i++ ) {
				Z[i] = X[i] * Math.Sin( piOver36 * ( i + 0.5 ) );
			}
			return Z;
		}
		
		// Aliasing reduction		
		static double[] cs = new double[] {
			0.857492925712544000, 0.881741997317705000,
			0.949628649102733000, 0.983314592491790000,
			0.995517816067586000, 0.999160558178148000,
			0.999899195244447000, 0.999993155070280000,
		};
		
		static double[] ca = new double[] {
			-0.514495755427527000, -0.471731968564972000,
			-0.313377454203902000, -0.181913199610981000,
			-0.094574192526420700, -0.040965582885304100,
			-0.014198568572471200, -0.003699974673760040,
		};
		
		static double[] AliasingButterfly( double[] xr ) {
			double[] xar = new double[xr.Length];
			for( int i = 0; i < xr.Length; i++ ) {
				xar[i] = xr[i];
			}
			
			for( int sb = 1; sb < 32; sb++ ) {
				int index = sb * 18;
				for( int i = 0; i < 8; i++ ) {
					double sample1 = xr[index - 1 - i];
					double sample2 = xr[index + i];
					
					xar[index - 1 - i] = sample1 * cs[i] - sample2 * ca[i];
					xar[index + i] = sample2 * cs[i] + sample1 * ca[i];
				}
			}
			return xar;
		}
	}
}