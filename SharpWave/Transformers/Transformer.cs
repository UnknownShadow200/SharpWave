using System;

namespace SharpWave.Transformers {
	
	public abstract class Transformer {
		
		public abstract string TransformerName { get; }
		
		public abstract byte[] Transform( byte[] samples, int bitsPerSample );
	}
	
	public sealed class EmptyTransformer : Transformer {
		
		public override string TransformerName {
			get { return "No transformation"; }
		}
		
		public override byte[] Transform( byte[] samples, int bitsPerSample ) {
			return samples;
		}
		
		public static readonly Transformer Instance = new EmptyTransformer();
	}
}
