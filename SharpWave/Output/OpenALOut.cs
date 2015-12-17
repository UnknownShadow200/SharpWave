using System;
using System.Threading;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using SharpWave.Codecs;

namespace SharpWave {
	
	/// <summary> Outputs audio to the default sound playback device using the 
	/// native OpenAL library. Cross platform. </summary>
	public unsafe sealed partial class OpenALOut : IAudioOutput {
		uint source = uint.MaxValue;
		uint[] bufferIDs;
		AudioContext context, shareContext;
		ALFormat format;
		
		public void Create( int numBuffers ) {			
			Create( numBuffers, null );
		}
		
		public void Create( int numBuffers, IAudioOutput share ) {
			AL.DistanceModel( ALDistanceModel.None );
			bufferIDs = new uint[numBuffers];
			OpenALOut alOut = share as OpenALOut;
			
			if( alOut == null ) {
				context = new AudioContext();
			} else {
				context = alOut.context;
				shareContext = context;
			}
		}
		
		public void PlayRaw( AudioChunk chunk ) {
			SetupRaw( chunk );		
			int state;
			while( !pendingStop ) {
				AL.GetSource( source, ALGetSourcei.SourceState, out state );
				if( (ALSourceState)state != ALSourceState.Playing )
					break;
				Thread.Sleep( 1 );
			}
			uint bufferId = 0;
			AL.SourceUnqueueBuffers( source, 1, ref bufferId );
		}
		
		bool playingAsync;
		public void PlayRawAsync( AudioChunk chunk ) { 
			SetupRaw( chunk ); 
			playingAsync = true;
		}
		
		public bool DoneRawAsync() {
			if( !playingAsync ) return true;
			int state;
			AL.GetSource( source, ALGetSourcei.SourceState, out state );
			if( (ALSourceState)state == ALSourceState.Playing )
				return false;
			
			playingAsync = false;
			uint bufferId = 0;
			AL.SourceUnqueueBuffers( source, 1, ref bufferId );
			return true;
		}
		
		void SetupRaw( AudioChunk chunk ) {
			Initalise( chunk );
			UpdateBuffer( bufferIDs[0], chunk );
			CheckError();
			// TODO: Use AL.Source(source, ALSourcei.Buffer, buffer);
			AL.SourceQueueBuffers( source, 1, bufferIDs );
			CheckError();
			AL.SourcePlay( source );
			CheckError();
		}
		
		unsafe void UpdateBuffer( uint bufferId, AudioChunk chunk ) {
			fixed( byte* src = chunk.Data ) {
				byte* chunkPtr = src + chunk.BytesOffset;
				AL.BufferData( bufferId, format, (IntPtr)chunkPtr, 
				              chunk.Length, chunk.Frequency );
			}
		}
		void CheckError() {
			ALError error = AL.GetError();
			if( error != ALError.NoError ) {
				Console.WriteLine( "OpenAL error:" + error );
				throw new Exception();
			}
		}
		
		bool pendingStop;
		public void Stop() {
			pendingStop = true;
		}
		
		public void Dispose() {
			if( source != uint.MaxValue ) {
				AL.DeleteSources( 1, ref source );
				AL.DeleteBuffers( bufferIDs );
				CheckError();
			}
			if( shareContext != null )
				context.Dispose();
		}
		
		AudioChunk last;
		void Initalise( AudioChunk first ) {
			format = GetALFormat( first.Channels, first.BitsPerSample );
			
			// Don't need to recreate device if it's the same.
			if( last != null && last.BitsPerSample == first.BitsPerSample &&
			   last.Channels == first.Channels && last.Frequency == first.Frequency )
				return;
			
			last = first;
			Dispose();
			uint sourceU = 0;
			AL.GenSources( 1, out sourceU );
			source = sourceU;
			CheckError();
			
			fixed( uint* bufferPtr = bufferIDs )
				AL.GenBuffers( bufferIDs.Length, bufferPtr );
			CheckError();
		}
		
		static ALFormat GetFormatFor( ALFormat baseFormat, int bitsPerSample ) {
			if( bitsPerSample == 8 ) return baseFormat;
			if( bitsPerSample == 16 ) return (ALFormat)(baseFormat + 1);
			throw new NotSupportedException( "Unsupported bits per sample: " + bitsPerSample );
		}
		
		static ALFormat GetALFormat( int channels, int bitsPerSample ) {
			switch( channels ) {
					case 1: return GetFormatFor( ALFormat.Mono8, bitsPerSample );
					case 2: return GetFormatFor( ALFormat.Stereo8, bitsPerSample );
					default: throw new NotSupportedException( "Unsupported number of channels: " + channels );
			}
		}
	}
}
