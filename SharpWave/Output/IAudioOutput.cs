using System;
using SharpWave.Codecs;
using SharpWave.Containers;

namespace SharpWave {
	
	public interface IAudioOutput : IDisposable {
		
		void Create( int numBuffers );
		
		void Create( int numBuffers, IAudioOutput shared );
		
		/// <summary> Progressively streams and plays data from the given container. </summary>
		void PlayStreaming( IMediaContainer container );
		
		/// <summary> Plays an entire single chunk of PCM audio. </summary>
		void PlayRaw( AudioChunk chunk );
	}
}