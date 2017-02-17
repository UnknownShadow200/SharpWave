using System;
using SharpWave.Codecs;
using SharpWave.Containers;

namespace SharpWave {
	
	public interface IAudioOutput : IDisposable {
		
		void Create( int numBuffers );
		
		void Create( int numBuffers, IAudioOutput shared );
		
		void Stop();
		
		/// <summary> Progressively streams and plays data from the given container. </summary>
		void PlayStreaming( IMediaContainer container );
		
		/// <summary> Plays an entire single chunk of PCM audio. </summary>
		void PlayRaw( AudioChunk chunk );
		
		/// <summary> Plays an entire single chunk of PCM audio asynchronously. </summary>
		void PlayRawAsync( AudioChunk chunk );
		
		/// <summary> Whether the last single chunk PCM audio chunk played asynchronously has finished playing. </summary>
		bool DoneRawAsync();
		
		void SetVolume(float volume);
		
		void SetPitch(float pitch);
		
		/// <summary> Details about the last audio chunk this player played. </summary>
		/// <remarks> Playing sounds of same channels, bits per sample and sample rate avoid the costly device recreating operation.</remarks>
		LastChunk Last { get; }
	}
	
	public struct LastChunk { public int Channels, BitsPerSample, SampleRate; }
}