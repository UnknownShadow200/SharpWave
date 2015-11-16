/*using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.DirectX.DirectSound;
using Microsoft.DirectX;
using System.Threading;

namespace SharpWave {
	
	public sealed class StreamAudioPlayerDX : IStreamAudioPlayer {
		int source = -1;
		int[] bufferIDs;
		Device device;
		SecondaryBuffer buffer;
		
		public StreamAudioPlayerDX() {
			device = new Device();
			IntPtr handle = IntPtr.Zero;
			using( Process process = Process.GetCurrentProcess() ) {
				handle = process.Handle;
			}
			device.SetCooperativeLevel( handle, CooperativeLevel.Priority );
		}
		
		public void StreamData( Stream sourceStream, ICodec codec ) {
			if( sourceStream == null ) throw new ArgumentNullException( "sourceStream" );
			if( codec == null ) throw new ArgumentNullException( "codec" );
			IEnumerable<AudioInfo> chunks = codec.StreamData( sourceStream );
			
			buffer = new SecondaryBuffer( 0, 0, device );
			BufferDescription description = new BufferDescription();
			WaveFormat format = new WaveFormat();
			
		}
		
		
		public void Dispose() {
		}
	}
}*/
