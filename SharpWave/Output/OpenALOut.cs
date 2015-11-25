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
		AudioContext context;
		
		public OpenALOut() {
			context = new AudioContext();
		}
		
		public void PlayRaw( AudioChunk chunk ) {
			Initalise( chunk );
			ALFormat format = GetALFormat( chunk.Channels, chunk.BitsPerSample );
			AL.BufferData( bufferIDs[0], format, chunk.Data, chunk.Length, chunk.Frequency );
			CheckError();
			AL.SourceQueueBuffers( source, bufferIDs.Length, bufferIDs );
			CheckError();
			AL.SourcePlay( source );
			CheckError();
			
			int state;
			// Query the source to find out when the last buffer stops playing.
			for( ; ; ) {
				AL.GetSource( source, ALGetSourcei.SourceState, out state );
				if( (ALSourceState)state != ALSourceState.Playing )
					break;
				Thread.Sleep( 1 );
			}
		}
		
		void CheckError() {
			ALError error = AL.GetError();
			if( error != ALError.NoError ) {
				Console.WriteLine( "OpenAL error:" + error );
				throw new Exception();
			}
		}
		
		public void Dispose() {
			if( source != uint.MaxValue ) {
				AL.DeleteSources( 1, ref source );
				AL.DeleteBuffers( bufferIDs );
			}
		}
		
		// A buffer size of 2 is not enough for some codecs.
		const int bufferSize = 4;
		AudioChunk last;
		void Initalise( AudioChunk first ) {
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
			
			bufferIDs = new uint[bufferSize];
			fixed( uint* bufferPtr = bufferIDs )
				AL.GenBuffers( bufferSize, bufferPtr );
			CheckError();
		}
		
		static ALFormat GetFormatFor( ALFormat baseFormat, int bitsPerSample ) {
			if( bitsPerSample == 8 ) return baseFormat;
			if( bitsPerSample == 16 ) return (ALFormat)( baseFormat + 1 );
			throw new NotSupportedException( "Unsupported bits per sample: " + bitsPerSample );
		}
		
		static ALFormat GetALFormat( int channels, int bitsPerSample ) {
			switch( channels ) {
					case 1: return GetFormatFor( ALFormat.Mono8, bitsPerSample );
					case 2: return GetFormatFor( ALFormat.Stereo8, bitsPerSample );
					case 4: return GetFormatFor( ALFormat.MultiQuad8Ext, bitsPerSample );
					case 5: return GetFormatFor( ALFormat.Multi51Chn8Ext, bitsPerSample );
					case 6: return GetFormatFor( ALFormat.Multi61Chn8Ext, bitsPerSample );
					case 7: return GetFormatFor( ALFormat.Multi71Chn8Ext, bitsPerSample );
					default: throw new NotSupportedException( "Unsupported number of channels: " + channels );
			}
		}
	}
}
