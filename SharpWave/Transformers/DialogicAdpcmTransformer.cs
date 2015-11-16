using System;

namespace SharpWave.Transformers {

	//Based on http://people.cis.ksu.edu/~tim/vox/
	public class DialogicAdpcmTransformer : Transformer {
		
		public override string TransformerName {
			get { return "Dialogic ADPCM"; }
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
		
		int lastSample;
		int stepIndex;

		short DecodeAdpcm( int code ) {
			int step = stepSizes[stepIndex];
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
			stepIndex += stepAdjust[code & 0x07];
			// Clip the values to +(2^11)-1 to -2^11. (12 bits 2's complement)
			if( sample > 2047 )
				sample = 2047;
			if( sample < -2048 )
				sample = -2048;
			if( stepIndex < 0 )
				stepIndex = 0;
			if( stepIndex > 48 )
				stepIndex = 48;
			
			lastSample = sample;
			return (short)sample;
		}
		
		static readonly short[] stepSizes = new short[] {
			16, 17, 19, 21, 23, 25, 28, 31, 34, 37, 41,
			45, 50, 55, 60, 66, 73, 80, 88, 97, 107, 118, 130, 143, 157, 173,
			190, 209, 230, 253, 279, 307, 337, 371, 408, 449, 494, 544, 598, 658,
			724, 796, 876, 963, 1060, 1166, 1282, 1411, 1552 };
		
		static readonly short[] stepAdjust = new short[] {
			-1, -1, -1, -1, 2, 4, 6, 8 };
		
		// Note that this is not just a single instance,
		// because transforming samples for this transformer depend on previous samples.
		public static Transformer Instance {
			get { return new DialogicAdpcmTransformer(); }
		}
	}
}