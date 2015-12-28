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
		public AudioContext context, shareContext;
		ALFormat format;
		
		static readonly object globalLock = new object();
		
		public void Create( int numBuffers ) {
			Create( numBuffers, null );
		}
		
		public void Create( int numBuffers, IAudioOutput share ) {
			bufferIDs = new uint[numBuffers];
			OpenALOut alOut = share as OpenALOut;
			
			if( alOut == null ) {
				context = new AudioContext();
			} else {
				context = alOut.context;
				shareContext = context;
			}
			
			lock( globalLock ) {
				context.MakeCurrent();
				AL.DistanceModel( ALDistanceModel.None );
			}
			Console.WriteLine( "al context:" + context );
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
			lock( globalLock ) {
				context.MakeCurrent();
				AL.GetSource( source, ALGetSourcei.SourceState, out state );
			}
			if( (ALSourceState)state == ALSourceState.Playing )
				return false;
			
			playingAsync = false;
			uint bufferId = 0;
			lock( globalLock ) {
				context.MakeCurrent();
				AL.SourceUnqueueBuffers( source, 1, ref bufferId );
			}
			return true;
		}
		
		void SetupRaw( AudioChunk chunk ) {
			Initalise( chunk );
			UpdateBuffer( bufferIDs[0], chunk );
			CheckError();
			// TODO: Use AL.Source(source, ALSourcei.Buffer, buffer);
			lock( globalLock ) {
				context.MakeCurrent();
				AL.SourceQueueBuffers( source, 1, bufferIDs );
			}
			CheckError();
			lock( globalLock ) {
				context.MakeCurrent();
				AL.SourcePlay( source );
			}
			CheckError();
		}
		
		unsafe void UpdateBuffer( uint bufferId, AudioChunk chunk ) {
			fixed( byte* src = chunk.Data ) {
				byte* chunkPtr = src + chunk.BytesOffset;
				lock( globalLock ) {
					context.MakeCurrent();
					AL.BufferData( bufferId, format, (IntPtr)chunkPtr,
					              chunk.Length, chunk.Frequency );
				}
			}
		}
		
		void CheckError() {
			ALError error;
			lock( globalLock ) {
				context.MakeCurrent();
				error = AL.GetError();
			}
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
			DisposeSource();
			if( shareContext == null && context != null )
				context.Dispose();
		}
		
		void DisposeSource() {
			if( source == uint.MaxValue )
				return;
			lock( globalLock ) {
				context.MakeCurrent();
				AL.DeleteSources( 1, ref source );
			}
			lock( globalLock ) {
				context.MakeCurrent();
				AL.DeleteBuffers( bufferIDs );
			}
			CheckError();
		}
		
		AudioChunk last;
		void Initalise( AudioChunk first ) {
			format = GetALFormat( first.Channels, first.BitsPerSample );
			
			// Don't need to recreate device if it's the same.
			if( last != null && last.BitsPerSample == first.BitsPerSample &&
			   last.Channels == first.Channels && last.Frequency == first.Frequency )
				return;
			
			last = first;
			DisposeSource();
			uint sourceU = 0;
			lock( globalLock ) {
				context.MakeCurrent();
				AL.GenSources( 1, out sourceU );
			}
			source = sourceU;
			CheckError();
			
			fixed( uint* bufferPtr = bufferIDs ) {
				lock( globalLock ) {
					context.MakeCurrent();
					AL.GenBuffers( bufferIDs.Length, bufferPtr );
				}
			}
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
