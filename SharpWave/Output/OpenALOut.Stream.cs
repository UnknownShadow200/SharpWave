using System;
using System.Collections.Generic;
using System.Threading;
using OpenTK.Audio.OpenAL;
using SharpWave.Codecs;
using SharpWave.Containers;

namespace SharpWave {
	
	/// <summary> Outputs audio to the default sound playback device using the
	/// native OpenAL library. Cross platform. </summary>
	public unsafe sealed partial class OpenALOut : IAudioOutput {
		
		public void PlayStreaming( IMediaContainer container ) {
			if( container == null ) throw new ArgumentNullException( "container" );
			container.ReadMetadata();
			ICodec codec = container.GetAudioCodec();
			IEnumerator<AudioChunk> chunks =
				codec.StreamData( container ).GetEnumerator();
			
			int usedCount = 0;
			for( int i = 0; i < bufferIDs.Length; i++ ) {
				if( !chunks.MoveNext() ) break;
				
				AudioChunk chunk = chunks.Current;
				if( i == 0 ) Initalise( chunk );
				
				UpdateBuffer( bufferIDs[i], chunk );
				CheckError( "PlayStreaming.UpdateBuffer (first)" );
				usedCount++;
			}
			Console.WriteLine( "used: " + usedCount );
			if( usedCount == 0 ) return;
			
			lock( globalLock ) {
				context.MakeCurrent();
				AL.SourceQueueBuffers( source, usedCount, bufferIDs );
			}
			CheckError( "PlayStreaming.SourceQueueBuffers (first)" );
			
			lock( globalLock ) {
				context.MakeCurrent();
				AL.SourcePlay( source );
			}
			CheckError( "PlayStreaming.SourcePlay" );
			
			while( !pendingStop ) {
				int buffersProcessed = 0;
				lock( globalLock ) {
					context.MakeCurrent();
					AL.GetSource( source, ALGetSourcei.BuffersProcessed, out buffersProcessed );
				}
				CheckError( "PlayStreaming.GetSources" );
				
				if( buffersProcessed > 0 ) {
					uint bufferId = 0;
					lock( globalLock ) {
						context.MakeCurrent();
						AL.SourceUnqueueBuffers( source, 1, ref bufferId );
					}
					if( !chunks.MoveNext() ) break;
					
					AudioChunk chunk = chunks.Current;
					UpdateBuffer( bufferId, chunk );
					CheckError( "PlayStreaming.BufferData" );
					           
					lock( globalLock ) {
						context.MakeCurrent();
						AL.SourceQueueBuffers( source, 1, ref bufferId );
					}
					CheckError( "PlayStreaming.SourceQueueBuffers" );
				}
				Thread.Sleep( 1 );
			}
			Console.WriteLine( "Ran out of chunks!" );
			
			while( !pendingStop ) {
				int buffersProcessed = 0;
				lock( globalLock ) {
					context.MakeCurrent();
					AL.GetSource( source, ALGetSourcei.BuffersProcessed, out buffersProcessed );
				}
				CheckError( "PlayStreaming.GetSource" );
				
				if( buffersProcessed > 0 ) {
					for( int i = 0; i < buffersProcessed; i++ ) {
						uint bufferId = 0;
						lock( globalLock ) {
							context.MakeCurrent();
							AL.SourceUnqueueBuffers( source, 1, ref bufferId );
						}
					}
				}
				
				int state;
				lock( globalLock ) {
					context.MakeCurrent();
					AL.GetSource( source, ALGetSourcei.SourceState, out state );
				}
				if( (ALSourceState)state != ALSourceState.Playing )
					break;
				Thread.Sleep( 1 );
			}
		}
	}
}
