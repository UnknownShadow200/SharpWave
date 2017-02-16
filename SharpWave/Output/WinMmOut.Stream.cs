using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using SharpWave.Codecs;
using SharpWave.Containers;

namespace SharpWave {
	
	/// <summary> Outputs audio to the default sound playback device using the 
	/// native WinMm library. Windows only. </summary>
	public sealed partial class WinMmOut : IAudioOutput {
		
		public unsafe void PlayStreaming( IMediaContainer container ) {
			container.ReadMetadata();
			ICodec codec = container.GetAudioCodec();
			IEnumerator<AudioChunk> chunks = 
				codec.StreamData( container ).GetEnumerator();

			int usedBuffers = 0;
			for( int i = 0; i < dataHandles.Length; i++ ) {
				if( chunks.MoveNext() ) {
					if( i == 0 )
						Initalise( chunks.Current );
					UpdateBuffer( i, chunks.Current );
					usedBuffers++;
				}
			}			
			Console.WriteLine( "used: " + usedBuffers );
			if( usedBuffers == 0 ) return;
			
			bool ranOutOfChunks = false;
			while( !AllDone( ranOutOfChunks, usedBuffers ) ) {
				for( int i = 0; i < usedBuffers; i++ ) {
					IntPtr address = (IntPtr)((byte*)headers + i * waveHeaderSize );
					WaveHeader header = *((WaveHeader*)address);
					if( (header.Flags & WaveHeaderFlags.Done) == 0 )
						continue;
					
					Free( i );
					if( pendingStop || !chunks.MoveNext() )
						ranOutOfChunks = true;
					else
						UpdateBuffer( i, chunks.Current );
				}
				Thread.Sleep( 1 );
			}
		}
		
		unsafe bool AllDone( bool ranOutOfChunks, int usedBuffers ) {
			if( !ranOutOfChunks ) return false;
			for( int i = 0; i < usedBuffers; i++ ) {
				IntPtr address = (IntPtr)((byte*)headers + i * waveHeaderSize );
				WaveHeader header = *((WaveHeader*)address);
				if( (header.Flags & WaveHeaderFlags.Done) == 0 )
					return false;
			}
			return true;
		}
	}
}
