using System;

namespace SharpWave.Transformers {

	// Example Mu-law encoded wave files were downloaded from http://www-mmsp.ece.mcgill.ca/Documents/AudioFormats/WAVE/Samples.html
	public class MuLawTransformer : Transformer {
		
		public override string TransformerName {
			get { return "G.711 Mu-law"; }
		}
		
		public override byte[] Transform( byte[] samples, int bitsPerSample ) {
			//if( bitsPerSample != 8 ) throw new ArgumentException( "Input bits per sample must be 8." );
			byte[] transformedSamples = new byte[samples.Length * 2];
			int transformedIndex = 0;
			for( int i = 0; i < samples.Length; i++ ) {
				ushort sample = (ushort)MuLawDecompressTable[samples[i]];
				transformedSamples[transformedIndex] = (byte)sample;
				transformedSamples[transformedIndex + 1] = (byte)( sample >> 8 );
				transformedIndex += 2;
			}
			return transformedSamples;
		}
		
		// Below is the original C# code port of the algorithm to generate the
		// decompression table. See more information about the code in 'license.txt'.	
		/*int TransformSample( byte sample ) {
			int sign = sample < 0x80 ? -1 : 1; // sign-bit = 1 for positive values
			int mantissa = ~sample;	// 1's complement of input value
			int exponent = ( mantissa >> 4 ) & 0x07; // extract exponent
			int segment = exponent + 1;	// compute segment number
			mantissa = mantissa & 0x0F;	// extract mantissa

			// Compute Quantized Sample (14 bit left justified!)
			int step = 4 << segment;	// position of the LSB
			// = 1 quantization step)
			return sign * // sign
				(
					( 0x80 << exponent )	// '1', preceding the mantissa
					+ step * mantissa	// left shift of mantissa
					+ step / 2		// 1/2 quantization step
					- 4 * 33
				);
		}*/
		
		public static readonly Transformer Instance = new MuLawTransformer();		
		
		static readonly short[] MuLawDecompressTable = new short[] {
			-32124, -31100, -30076, -29052, -28028, -27004, -25980, -24956,
			-23932, -22908, -21884, -20860, -19836, -18812, -17788, -16764,
			-15996, -15484, -14972, -14460, -13948, -13436, -12924, -12412,
			-11900, -11388, -10876, -10364, -9852, -9340, -8828, -8316,
			-7932, -7676, -7420, -7164, -6908, -6652, -6396, -6140,
			-5884, -5628, -5372, -5116, -4860, -4604, -4348, -4092,
			-3900, -3772, -3644, -3516, -3388, -3260, -3132, -3004,
			-2876, -2748, -2620, -2492, -2364, -2236, -2108, -1980,
			-1884, -1820, -1756, -1692, -1628, -1564, -1500, -1436,
			-1372, -1308, -1244, -1180, -1116, -1052, -988, -924,
			-876, -844, -812, -780, -748, -716, -684, -652,
			-620, -588, -556, -524, -492, -460, -428, -396,
			-372, -356, -340, -324, -308, -292, -276, -260,
			-244, -228, -212, -196, -180, -164, -148, -132,
			-120, -112, -104, -96, -88, -80, -72, -64,
			-56, -48, -40, -32, -24, -16, -8, 0,
			32124, 31100, 30076, 29052, 28028, 27004, 25980, 24956,
			23932, 22908, 21884, 20860, 19836, 18812, 17788, 16764,
			15996, 15484, 14972, 14460, 13948, 13436, 12924, 12412,
			11900, 11388, 10876, 10364, 9852, 9340, 8828, 8316,
			7932, 7676, 7420, 7164, 6908, 6652, 6396, 6140,
			5884, 5628, 5372, 5116, 4860, 4604, 4348, 4092,
			3900, 3772, 3644, 3516, 3388, 3260, 3132, 3004,
			2876, 2748, 2620, 2492, 2364, 2236, 2108, 1980,
			1884, 1820, 1756, 1692, 1628, 1564, 1500, 1436,
			1372, 1308, 1244, 1180, 1116, 1052, 988, 924,
			876, 844, 812, 780, 748, 716, 684, 652,
			620, 588, 556, 524, 492, 460, 428, 396,
			372, 356, 340, 324, 308, 292, 276, 260,
			244, 228, 212, 196, 180, 164, 148, 132,
			120, 112, 104, 96, 88, 80, 72, 64,
			56, 48, 40, 32, 24, 16, 8, 0,
		};
	}
}
