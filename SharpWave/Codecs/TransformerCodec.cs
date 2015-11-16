using System;
using System.Collections.Generic;
using System.IO;
using SharpWave.Transformers;

namespace SharpWave.Codecs.Wave {
	
	public class TransformerCodec : ICodec {
		
		public string Name {
			get { return "Simple transformer for WAVE/AU files"; }
		}
		
		int bufferSize;
		AudioChunk info;
		Transformer transformer;
		int codecBitsPerSample;
		uint dataSize;
		PrimitiveReader reader;
		
		public TransformerCodec( uint dataSize, PrimitiveReader reader, int frequency, int channels,
		                 int bitsPerSample, Transformer transformer, int codecBitsPerSample ) {
			info = new AudioChunk();
			this.dataSize = dataSize;
			this.reader = reader;
			info.Frequency = frequency;
			info.Channels = channels;
			info.BitsPerSample = bitsPerSample;
			this.transformer = transformer;
			this.codecBitsPerSample = codecBitsPerSample;
			bufferSize = (int)( frequency * channels * codecBitsPerSample / 8 );
		}
		
		public IEnumerable<AudioChunk> StreamData( Stream source ) {
			long length = dataSize;
			int bufferLength = bufferSize;
			while( length > 0 ) {
				int currentBufferSize = (int)Math.Min( bufferLength, length );
				byte[] data = reader.ReadBytes( currentBufferSize );
				info.Data = transformer.Transform( data, codecBitsPerSample );
				length -= bufferLength;
				yield return info;
			}
		}
	}
}