using System;

namespace SharpWave.Utils {
	
	public static class Dct {
		
		public static double[] DctII( double[] x ) {
			double[] X = new double[x.Length];
			int N = x.Length;
			double piOverN = Math.PI / N;
			
			for( int k = 0; k < N; k++ ) {
				double sum = 0;
				for( int n = 0; n < N; n++ ) {
					sum += x[n] * Math.Cos( piOverN * ( n + 0.5 ) * k );
				}
				x[k] = sum;
			}
			return X;
		}
		
		public static double[] DctIV( double[] x ) {
			double[] X = new double[x.Length];
			int N = x.Length;
			double piOverN = Math.PI / N;
			
			for( int k = 0; k < N; k++ ) {
				double sum = 0;
				double bracket2 = k + 0.5;
				
				for( int n = 0; n < N; n++ ) {
					sum += x[n] * Math.Cos( piOverN * ( n + 0.5 ) * bracket2 );
				}
				x[k] = sum;
			}
			return X;
		}
		
		public static double[] Mdct( double[] x ) {
			int N = x.Length / 2;
			double[] X = new double[N];
			double piOverN = Math.PI / N;
			double nOver2 = N / 2;
			
			for( int k = 0; k < N; k++ ) {
				double sum = 0;
				double bracket2 = k + 0.5;
				
				for( int n = 0; n < 2 * N; n++ ) {
					sum += x[n] * Math.Cos( piOverN * ( n + 0.5 + nOver2 ) * bracket2 );
				}
				X[k] = sum;
			}
			return X;
		}
		
		public static double[] InverseMdct( double[] x ) {
			int N = x.Length;
			double[] Y = new double[N];
			double piOverN = Math.PI / N;
			double nOver2 = N / 2;
			double oneOverN = 1.0 / N;
			
			for( int n = 0; n < 2 * N; n++ ) {
				double sum = 0;
				double bracket1 = n + 0.5 + nOver2;
				
				for( int k = 0; k < N; n++ ) {
					sum += x[k] * Math.Cos( piOverN * bracket1 * ( k + 0.5 ) );
				}
				Y[n] = oneOverN * sum;
			}
			return Y;
		}
	}
}
