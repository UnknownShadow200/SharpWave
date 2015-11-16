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
	
	public sealed class BigEndian24BitTo16BitTransformer : Transformer {
		
		public override string TransformerName {
			get { return "24 bit BE -> 16 bit LE"; }
		}
		
		const float rescaleValue = (float)( ( 1 << 24 ) - 1 );
		public override byte[] Transform( byte[] samples, int bitsPerSample ) {
			byte[] transformedSamples = new byte[samples.Length * 2 / 3];
			int j = 0;
			for( int i = 0; i < samples.Length; i += 3 ) {
				int raw = ( samples[i] << 16 ) | ( samples[i + 1] << 8 ) | samples[i + 2];
				int rescaled = (int)( raw / rescaleValue * ushort.MaxValue );
				transformedSamples[j] = (byte)rescaled;
				transformedSamples[j + 1] = (byte)( rescaled >> 8 );
				j += 2;
			}
			return transformedSamples;
		}
		
		public static readonly Transformer Instance = new BigEndian24BitTo16BitTransformer();
	}
	
	public sealed class BigEndian32BitTo16BitTransformer : Transformer {
		
		public override string TransformerName {
			get { return "32 bit BE -> 16 bit LE"; }
		}
		
		const float rescaleValue = (float)uint.MaxValue;
		public override byte[] Transform( byte[] samples, int bitsPerSample ) {
			byte[] transformedSamples = new byte[samples.Length * 2 / 4];
			int j = 0;
			for( int i = 0; i < samples.Length; i += 4 ) {
				int raw = ( samples[i] << 24 ) | ( samples[i + 1] << 16 ) | ( samples[i + 2] << 8 ) | samples[i + 3];
				int rescaled = (int)( (uint)raw / rescaleValue * ushort.MaxValue );
				transformedSamples[j] = (byte)rescaled;
				transformedSamples[j + 1] = (byte)( rescaled >> 8 );
				j += 2;
			}
			return transformedSamples;
		}
		
		public static readonly Transformer Instance = new BigEndian32BitTo16BitTransformer();
	}
	
	public unsafe sealed class BigEndianFloat32To16BitTransformer : Transformer {
		
		public override string TransformerName {
			get { return "32 bit float BE -> 16 bit LE"; }
		}
		
		public override byte[] Transform( byte[] samples, int bitsPerSample ) {
			byte[] transformedSamples = new byte[samples.Length * 2 / 4];
			int j = 0;
			for( int i = 0; i < samples.Length; i += 4 ) {
				int raw = ( samples[i] << 24 ) | ( samples[i + 1] << 16 ) | ( samples[i + 2] << 8 ) | samples[i + 3];
				float rawFloat = *(float*)&raw;
				
				int rescaled = (ushort)( rawFloat * 32767f );
				transformedSamples[j] = (byte)rescaled;
				transformedSamples[j + 1] = (byte)( rescaled >> 8 );
				j += 2;
			}
			return transformedSamples;
		}
		
		public static readonly Transformer Instance = new BigEndianFloat32To16BitTransformer();
	}
	
	public unsafe sealed class BigEndianFloat64To16BitTransformer : Transformer {
		
		public override string TransformerName {
			get { return "64 bit float BE -> 16 bit LE"; }
		}
		
		public override byte[] Transform( byte[] samples, int bitsPerSample ) {
			byte[] transformedSamples = new byte[samples.Length * 2 / 8];
			int j = 0;
			for( int i = 0; i < samples.Length; i += 8 ) {
				int raw1 = ( samples[i] << 24 ) | (samples[i + 1] << 16 ) | (samples[i + 2] << 8 ) | samples[i + 3];
				int raw2 = ( samples[i + 4] << 24 ) | (samples[i + 5] << 16 ) | ( samples[i + 6] << 8 ) | samples[i + 7];
				long raw = ( (long)raw1 << 32 ) | raw2;
				double rawFloat = *(double*)&raw;
				
				int rescaled = (ushort)( rawFloat * 32767f );
				transformedSamples[j] = (byte)rescaled;
				transformedSamples[j + 1] = (byte)( rescaled >> 8 );
				j += 2;
			}
			return transformedSamples;
		}
		
		public static readonly Transformer Instance = new BigEndianFloat64To16BitTransformer();
	}
}