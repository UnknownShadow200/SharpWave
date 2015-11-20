using System;

namespace SharpWave.Transformers {
	
	public sealed class BigEndian16BitTo16BitTransformer : Transformer {
		
		public override string TransformerName {
			get { return "16 bit BE -> 16 bit LE"; }
		}
		
		public override byte[] Transform( byte[] samples, int bitsPerSample ) {
			for( int i = 0; i < samples.Length; i += 2 ) {
				byte value1 = samples[i];
				byte value2 = samples[i + 1];
				samples[i + 1] = value1;
				samples[i] = value2;
			}
			return samples;
		}
		
		public static readonly Transformer Instance = new BigEndian16BitTo16BitTransformer();
	}
}