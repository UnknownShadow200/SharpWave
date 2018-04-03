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
		float volume = 1;
		
		LastChunk last;
		public LastChunk Last { get { return last; } }
		static readonly object globalLock = new object();
		
		public void SetVolume(float volume) {
			this.volume = volume;
			if (source == uint.MaxValue) return;
			
			lock( globalLock ) {
				context.MakeCurrent();
				AL.Source(source, ALSourcef.Gain, volume);
			}
			CheckError( "SetVolume" );
		}
		
		
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
			Initalise( chunk );
			SetData( chunk );
			
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
			Initalise( chunk );
			SetData( chunk );
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
		
		void SetData( AudioChunk chunk ) {
			UpdateBuffer( bufferIDs[0], chunk );
			CheckError( "SetupRaw.BufferData" );
			// TODO: Use AL.Source(source, ALSourcei.Buffer, buffer);
			lock( globalLock ) {
				context.MakeCurrent();
				AL.SourceQueueBuffers( source, 1, bufferIDs );
			}
			CheckError( "SetupRaw.QueueBuffers" );
			lock( globalLock ) {
				context.MakeCurrent();
				AL.SourcePlay( source );
			}
			CheckError( "SetupRaw.SourcePlay" );
		}
		
		unsafe void UpdateBuffer( uint bufferId, AudioChunk chunk ) {
			fixed( byte* src = chunk.Data ) {
				byte* chunkPtr = src + chunk.BytesOffset;
				lock( globalLock ) {
					context.MakeCurrent();
					AL.BufferData( bufferId, format, (IntPtr)chunkPtr,
					              chunk.Length, chunk.SampleRate );
				}
			}
		}
		
		void CheckError(string location) {
			ALError error;
			lock( globalLock ) {
				context.MakeCurrent();
				error = AL.GetError();
			}
			
			if( error != ALError.NoError ) {
				throw new InvalidOperationException( "OpenAL error: " + error + " at " + location);
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
			if( source == uint.MaxValue ) return;
			
			lock( globalLock ) {
				context.MakeCurrent();
				AL.DeleteSources( 1, ref source );
				source = uint.MaxValue;
			}
			lock( globalLock ) {
				context.MakeCurrent();
				AL.DeleteBuffers( bufferIDs );
			}
			CheckError( "Initalise.DeleteBuffers" );
		}
		
		public void Initalise( AudioChunk first ) {
			format = GetALFormat( first.Channels, first.BitsPerSample );
			// Don't need to recreate device if it's the same.
			if( last.BitsPerSample == first.BitsPerSample
			   && last.Channels    == first.Channels
			   && last.SampleRate  == first.SampleRate ) return;
			
			last.SampleRate    = first.SampleRate;
			last.BitsPerSample = first.BitsPerSample;
			last.Channels      = first.Channels;
			
			DisposeSource();
			uint sourceU = 0;
			lock( globalLock ) {
				context.MakeCurrent();
				AL.GenSources( 1, out sourceU );
			}
			source = sourceU;
			CheckError( "Initalise.GenSources" );
			if (volume != 1) SetVolume(volume);
			
			fixed( uint* bufferPtr = bufferIDs ) {
				lock( globalLock ) {
					context.MakeCurrent();
					AL.GenBuffers( bufferIDs.Length, bufferPtr );
				}
			}
			CheckError( "Initalise.GenBuffers" );
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
		
		public void SetListenerPos(float x, float y, float z) {
			lock( globalLock ) {
				context.MakeCurrent();
				AL.Listener(ALListener3f.Position, x, y, z);
			}
		}
		
		public unsafe void SetListenerDir(float yaw) {
			lock( globalLock ) {
				context.MakeCurrent();
				float* values = stackalloc float[6];
				values[0] = (float)Math.Sin(yaw);
				values[1] = 0.0f;
				values[2] = (float)Math.Cos(yaw);
				values[3] = 0.0f;
				values[4] = 1.0f;
				values[5] = 0.0f;
				AL.Listener(ALListenerfv.Orientation, values);
			}
		}
		
		public void SetSoundPos(float x, float y, float z) {
			lock( globalLock ) {
				context.MakeCurrent();
				AL.Source(source, ALSource3f.Position, x, y, z);
			}
		}

		public void SetSoundGain(float gain) {
			lock( globalLock ) {
				context.MakeCurrent();
				// I have no idea ??
				AL.DistanceModel(ALDistanceModel.InverseDistance);
				AL.Source(source, ALSourcef.ReferenceDistance, 0.0f);
				AL.Source(source, ALSourcef.MaxDistance, 1.0f);
			}
		}
	}
}
