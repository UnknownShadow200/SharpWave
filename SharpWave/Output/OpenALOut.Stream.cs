using System;
using System.Collections.Generic;
using System.Threading;
using OpenTK.Audio.OpenAL;
using SharpWave.Codecs;
using SharpWave.Containers;

namespace SharpWave {
	
	public unsafe sealed partial class OpenALOut : IAudioOutput {
		
		public void PlayStreaming( IMediaContainer container ) {
			if( container == null ) throw new ArgumentNullException( "container" );
			
			container.ReadMetadata();
			ICodec codec = container.GetAudioCodec();
			IEnumerable<AudioChunk> chunks = codec.StreamData( container );
			
			// A buffer size of 2 is not enough for some codecs.
			const int bufferSize = 3;
			bufferIDs = new uint[bufferSize];
			fixed( uint* bufferPtr = bufferIDs )
				AL.GenBuffers( bufferSize, bufferPtr );
			CheckError();
			
			uint sourceU = 0;
			AL.GenSources( 1, out sourceU );
			source = sourceU;
			CheckError();

			// TODO: Handle the case where the file is less than 2 seconds long.
			IEnumerator<AudioChunk> enumerator = chunks.GetEnumerator();
			for( int i = 0; i < bufferIDs.Length; i++ ) {
				enumerator.MoveNext();
				AudioChunk chunk = enumerator.Current;
				if( chunk == null || chunk.Data == null )
					throw new InvalidOperationException( "chunk or chunk audio data is null." );
				ALFormat format = GetALFormat( chunk.Channels, chunk.BitsPerSample );
				AL.BufferData( bufferIDs[i], format, chunk.Data, chunk.Length, chunk.Frequency );
				CheckError();
			}
			
			AL.SourceQueueBuffers( source, bufferIDs.Length, bufferIDs );
			CheckError();
			AL.SourcePlay( source );
			CheckError();
			
			for( ; ; ) {
				int buffersProcessed = 0;
				AL.GetSource( source, ALGetSourcei.BuffersProcessed, out buffersProcessed );
				CheckError();
				
				if( buffersProcessed > 0 ) {
					uint bufferId = 0;
					AL.SourceUnqueueBuffers( source, 1, ref bufferId );
					
					if( enumerator.MoveNext() ) {
						AudioChunk chunk = enumerator.Current;
						ALFormat format = GetALFormat( chunk.Channels, chunk.BitsPerSample );
						AL.BufferData( bufferId, format, chunk.Data, chunk.Length, chunk.Frequency );
						CheckError();
						AL.SourceQueueBuffers( source, 1, ref bufferId );
						CheckError();
					} else {
						break;
					}
				}
				Thread.Sleep( 1 );
			}
			Console.WriteLine( "Ran out of chunks!" );
			
			int state;
			// Query the source to find out when the last buffer stops playing.
			for( ; ; ) {
				AL.GetSource( source, ALGetSourcei.SourceState, out state );
				if( (ALSourceState)state != ALSourceState.Playing ) {
					break;
				}
				Thread.Sleep( 1 );
			}
			
			AL.DeleteBuffers( bufferIDs );
			AL.DeleteSources( 1, ref source );
			source = uint.MaxValue;
			bufferIDs = null;
		}
	}
}
