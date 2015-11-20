using System;
using System.Collections.Generic;
using System.IO;

namespace SharpWave.Codecs {
	
	public interface ICodec {
		
		IEnumerable<AudioChunk> StreamData( Stream source );
		
		string Name { get; }
	}
	
	public sealed class AudioChunk {
		public int Frequency;
		public int Channels;
		public int BitsPerSample;
		public byte[] Data;
		public int BytesUsed; // can be 0
		
		public int Length {
			get { return BytesUsed > 0 ? BytesUsed : Data.Length; }
		}
	}
}
