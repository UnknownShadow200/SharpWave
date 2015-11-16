using System;

namespace SharpWave.Transformers {

	//Based on http://wiki.multimedia.cx/index.php?title=IMA_ADPCM
	// TODO: Refactor this and DialogicAdpcmTransformer into a subclass of AdpcmTransformer
	public class ImaAdpcmTransformer : Transformer {
		
		public override string TransformerName {
			get { return "Interactive Multimedia Association ADPCM"; }
		}
		
		public override byte[] Transform( byte[] samples, int bitsPerSample ) {
			byte[] transformedSamples = new byte[samples.Length * 4];
			int transformedIndex = 0;
			for( int i = 0; i < samples.Length; i++ ) {
				byte sample = samples[i];
				ushort sample1 = (ushort)DecodeAdpcm( sample >> 4 );
				ushort sample2 = (ushort)DecodeAdpcm( sample & 0x0F );
				transformedSamples[transformedIndex] = (byte)sample1;
				transformedSamples[transformedIndex + 1] = (byte)( sample1 >> 8 );
				transformedSamples[transformedIndex + 2] = (byte)sample2;
				transformedSamples[transformedIndex + 3] = (byte)( sample2 >> 8 );
				transformedIndex += 4;
			}
			return transformedSamples;
		}
		
		int stepIndex;
		int lastSample;

		short DecodeAdpcm( int code ) {
			int step = stepTable[stepIndex];
			// TODO: Just replace this with a simpler multiplier?
			int diff = step >> 3; // / 8
			if( ( code & 0x01 ) != 0 )
				diff += step >> 2; // / 4
			if( ( code & 0x02 ) != 0 )
				diff += step >> 1; // / 2
			if( ( code & 0x04 ) != 0 )
				diff += step;
			// Sign bit.
			if( ( code & 0x08 ) != 0 )
				diff = -diff;
			
			int sample = lastSample + diff;			
			stepIndex += indexTable[code];
			
			// Range clipping to avoid IndexOutOfRange exceptions.
			if( stepIndex < 0 )
				stepIndex = 0;
			if( stepIndex > 88 )
				stepIndex = 88;
			if( sample < -32768 )
				sample = -32768; // -(2^16)
			if( sample > 32767 )
				sample = 32767; // 2^16 - 1
			
			lastSample = sample;
			return (short)sample;
		}
		
		static readonly short[] stepTable = new short[] {
			7, 8, 9, 10, 11, 12, 13, 14, 16, 17,
			19, 21, 23, 25, 28, 31, 34, 37, 41, 45,
			50, 55, 60, 66, 73, 80, 88, 97, 107, 118,
			130, 143, 157, 173, 190, 209, 230, 253, 279, 307,
			337, 371, 408, 449, 494, 544, 598, 658, 724, 796,
			876, 963, 1060, 1166, 1282, 1411, 1552, 1707, 1878, 2066,
			2272, 2499, 2749, 3024, 3327, 3660, 4026, 4428, 4871, 5358,
			5894, 6484, 7132, 7845, 8630, 9493, 10442, 11487, 12635, 13899,
			15289, 16818, 18500, 20350, 22385, 24623, 27086, 29794, 32767
		};
		
		static readonly short[] indexTable = new short[] {
			-1, -1, -1, -1, 2, 4, 6, 8,
			-1, -1, -1, -1, 2, 4, 6, 8
		};
		
		public static Transformer Instance {
			get { return new ImaAdpcmTransformer(); }
		}
	}
}