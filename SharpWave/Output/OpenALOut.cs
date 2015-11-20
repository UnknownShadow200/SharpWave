using System;
using System.Threading;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using SharpWave.Codecs;

namespace SharpWave {
	
	public unsafe sealed partial class OpenALOut : IAudioOutput {
		uint source = uint.MaxValue;
		uint[] bufferIDs;
		AudioContext context;
		
		public OpenALOut() {
			try {
				context = new AudioContext();
			} catch( Exception e ) {
				Console.WriteLine( e );
			}
		}
		
		public void PlayRaw( AudioChunk chunk ) {
			bufferIDs = new uint[1];
			fixed( uint* bufferPtr = bufferIDs )
				AL.GenBuffers( 1, bufferPtr );
			CheckError();
			
			uint sourceU = 0;
			AL.GenSources( 1, out sourceU );
			source = sourceU;
			CheckError();			
			
			ALFormat format = GetALFormat( chunk.Channels, chunk.BitsPerSample );
			AL.BufferData( bufferIDs[0], format, chunk.Data, chunk.Data.Length, chunk.Frequency );
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
			
			AL.DeleteBuffers( bufferIDs );
			AL.DeleteSources( 1, ref source );
			source = uint.MaxValue;
			bufferIDs = null;
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
