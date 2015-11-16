using System;

namespace SharpWave.Transformers {

	// Example A-law encoded wave files were downloaded from http://www-mmsp.ece.mcgill.ca/Documents/AudioFormats/WAVE/Samples.html
	public class ALawTransformer : Transformer {
		
		public override string TransformerName {
			get { return "G.711 A-law"; }
		}
		
		public override byte[] Transform( byte[] samples, int bitsPerSample ) {
			//if( bitsPerSample != 8 ) throw new ArgumentException( "Input bits per sample must be 8." );
			byte[] transformedSamples = new byte[samples.Length * 2];
			int transformedIndex = 0;
			for( int i = 0; i < samples.Length; i++ ) {
				ushort sample = (ushort)ALawDecompressTable[samples[i]];
				transformedSamples[transformedIndex] = (byte)sample;
				transformedSamples[transformedIndex + 1] = (byte)( sample >> 8 );
				transformedIndex += 2;
			}
			return transformedSamples;
		}	
		
		// Below is the original C# code port of the algorithm to generate the
		// decompression table. See more information about the code in 'license.txt'.	
		/*int TransformSample( byte sample ) {
			int ix = sample ^ 0x55;	// re-toggle toggled bits

			ix &= 0x7F;	// remove sign bit
			int exponent = ix >> 4;	// extract exponent
			int mantissa = ix & 0x0F; // now get mantissa
			if( exponent > 0 ) {
				mantissa = mantissa + 16; // add leading '1', if exponent > 0
			}

			mantissa = ( mantissa << 4 ) + 0x08; // now mantissa left justified and
			// 1/2 quantization step added
			if( exponent > 1 ) { // now left shift according exponent
				mantissa = mantissa << ( exponent - 1 );
			}

			return sample > 127	// invert, if negative sample
				? mantissa
				: -mantissa;
		}*/		
		
		public static readonly Transformer Instance = new ALawTransformer();
		
		static readonly short[] ALawDecompressTable = new short[] {
			-5504, -5248, -6016, -5760, -4480, -4224, -4992, -4736,
			-7552, -7296, -8064, -7808, -6528, -6272, -7040, -6784,
			-2752, -2624, -3008, -2880, -2240, -2112, -2496, -2368,
			-3776, -3648, -4032, -3904, -3264, -3136, -3520, -3392,
			-22016, -20992, -24064, -23040, -17920, -16896, -19968, -18944,
			-30208, -29184, -32256, -31232, -26112, -25088, -28160, -27136,
			-11008, -10496, -12032, -11520, -8960, -8448, -9984, -9472,
			-15104, -14592, -16128, -15616, -13056, -12544, -14080, -13568,
			-344, -328, -376, -360, -280, -264, -312, -296,
			-472, -456, -504, -488, -408, -392, -440, -424,
			-88, -72, -120, -104, -24, -8, -56, -40,
			-216, -200, -248, -232, -152, -136, -184, -168,
			-1376, -1312, -1504, -1440, -1120, -1056, -1248, -1184,
			-1888, -1824, -2016, -1952, -1632, -1568, -1760, -1696,
			-688, -656, -752, -720, -560, -528, -624, -592,
			-944, -912, -1008, -976, -816, -784, -880, -848,
			5504, 5248, 6016, 5760, 4480, 4224, 4992, 4736,
			7552, 7296, 8064, 7808, 6528, 6272, 7040, 6784,
			2752, 2624, 3008, 2880, 2240, 2112, 2496, 2368,
			3776, 3648, 4032, 3904, 3264, 3136, 3520, 3392,
			22016, 20992, 24064, 23040, 17920, 16896, 19968, 18944,
			30208, 29184, 32256, 31232, 26112, 25088, 28160, 27136,
			11008, 10496, 12032, 11520, 8960, 8448, 9984, 9472,
			15104, 14592, 16128, 15616, 13056, 12544, 14080, 13568,
			344, 328, 376, 360, 280, 264, 312, 296,
			472, 456, 504, 488, 408, 392, 440, 424,
			88, 72, 120, 104, 24, 8, 56, 40,
			216, 200, 248, 232, 152, 136, 184, 168,
			1376, 1312, 1504, 1440, 1120, 1056, 1248, 1184,
			1888, 1824, 2016, 1952, 1632, 1568, 1760, 1696,
			688, 656, 752, 720, 560, 528, 624, 592,
			944, 912, 1008, 976, 816, 784, 880, 848,
		};
	}
}
