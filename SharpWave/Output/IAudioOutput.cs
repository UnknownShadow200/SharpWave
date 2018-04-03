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
		
		void Initalise( AudioChunk chunk );
		
		/// <summary> Plays an entire single chunk of PCM audio. </summary>
		void PlayRaw( AudioChunk chunk );
		
		/// <summary> Plays an entire single chunk of PCM audio asynchronously. </summary>
		void PlayRawAsync( AudioChunk chunk );
		
		/// <summary> Whether the last single chunk PCM audio chunk played asynchronously has finished playing. </summary>
		bool DoneRawAsync();
		
		void SetVolume(float volume);
		
		/// <summary> Details about the last audio chunk this player played. </summary>
		/// <remarks> Playing sounds of same channels, bits per sample and sample rate avoid the costly device recreating operation.</remarks>
		LastChunk Last { get; }
		
		void SetListenerPos(float x, float y, float z);
		void SetListenerDir(float yaw);
		void SetSoundPos(float x, float y, float z);
		void SetSoundGain(float gain);
	}
	
	public struct LastChunk { public int Channels, BitsPerSample, SampleRate; }
}