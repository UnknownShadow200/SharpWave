using System;
using System.Collections.Generic;
using System.IO;
using SharpWave.Codecs;
using SharpWave.Containers;

namespace SharpWave {
	public delegate void Action();
	
	/// <summary> Outputs raw audio to the given stream in the constructor. </summary>
	public unsafe sealed partial class RawOut : IAudioOutput {
		
		public int Channels, BitsPerSample, Frequency;
		public readonly Stream OutStream;
		public readonly bool LeaveOpen;
		public Action OnGotMetadata;
		
		public RawOut( FileStream outStream, bool leaveOpen ) {
			OutStream = outStream;
			LeaveOpen = leaveOpen;
		}
		
		public void Create( int numBuffers ) { }
		
		public void Create( int numBuffers, IAudioOutput share ) { }
		
		public void Stop() { }
		
		public void PlayRaw( AudioChunk chunk ) {
			Channels = chunk.Channels;
			BitsPerSample = chunk.BitsPerSample;
			Frequency = chunk.Frequency;
			if( OnGotMetadata != null )
				OnGotMetadata();
			
			OutStream.Write( chunk.Data, chunk.BytesOffset, chunk.Length );
		}
		
		public void PlayStreaming( IMediaContainer container ) {
			container.ReadMetadata();
			ICodec codec = container.GetAudioCodec();
			IEnumerator<AudioChunk> chunks = 
				codec.StreamData( container ).GetEnumerator();
			
			if( !chunks.MoveNext() ) return;
			PlayRaw( chunks.Current );				
			while( chunks.MoveNext() ) {
				AudioChunk chunk = chunks.Current;
				OutStream.Write( chunk.Data, chunk.BytesOffset, chunk.Length );
			}
		}
		
		public void PlayRawAsync( AudioChunk chunk ) {
			throw new NotImplementedException();
		}
		
		public bool DoneRawAsync() {
			throw new NotImplementedException();
		}
		
		public void Dispose() {
			if( LeaveOpen ) return;
			OutStream.Close();
		}
		
		public void SetVolume(float volume) { }
		
		public void SetPitch(float pitch) { }
	}
}
