using System;
using System.Runtime.InteropServices;
using System.Text;
using SharpWave.Codecs;

namespace SharpWave {
	
	public sealed partial class AudioOutputWinMm : IAudioOutput {
		
		IntPtr handle;
		readonly int waveHeaderSize;
		WaveOutCallback callback;
		public AudioOutputWinMm( int bufferSize ) {
			waveHeaderSize = Marshal.SizeOf( default( WaveHeader ) );
			buffers = new WaveHeader[bufferSize];
			buffersHandle = GCHandle.Alloc( buffers, GCHandleType.Pinned );
			this.bufferSize = bufferSize;
		}
		
		bool reachedEnd = false;
		int bufferSize;
		
		public void PlayRaw( AudioChunk chunk ) {
			callback = ProcessRawWaveOutCallback;
			InitWinMm( chunk );
		}
		
		void ProcessRawWaveOutCallback( IntPtr handle, WaveOutMessage message, UIntPtr user, ref WaveHeader header, UIntPtr reserved ) {
			Console.WriteLine( "callback:" + message + "," + buffersIndex );
			if( message == WaveOutMessage.Done ) {
				Free( ref header );
				reachedEnd = true;
			}
		}
		
		void SendNextBuffer( AudioChunk chunk ) {
			Console.WriteLine( "send buffer " + buffersIndex );
			WaveHeader header = new WaveHeader();
			byte[] data = chunk.Data;
			GCHandle bufferHandle = GCHandle.Alloc( data, GCHandleType.Pinned );
			header.DataBuffer = bufferHandle.AddrOfPinnedObject();
			header.BufferLength = chunk.Length;
			header.Loops = 1;
			
			UserData userData = new UserData();
			userData.BufferHandle = bufferHandle;
			userData.Index = buffersIndex;
			GCHandle userDataHandle = GCHandle.Alloc( userData, GCHandleType.Pinned );
			header.UserData = GCHandle.ToIntPtr( userDataHandle );
			buffers[buffersIndex] = header;
			
			uint result = PrepareHeader( handle, ref buffers[buffersIndex], (uint)waveHeaderSize );
			CheckError( result );
			result = Write( handle, ref buffers[buffersIndex], (uint)waveHeaderSize );
			CheckError( result );
		}
		
		void Free( ref WaveHeader header ) {
			uint result = UnprepareHeader( handle, ref header, (uint)waveHeaderSize );
			CheckError( result );
			GCHandle userDataHandle = GCHandle.FromIntPtr( header.UserData );
			UserData userData = (UserData)userDataHandle.Target;
			GCHandle bufferHandle = userData.BufferHandle;
			bufferHandle.Free();
			buffersIndex = userData.Index;
		}
		
		void CheckError( uint result ) {
			if( result != 0 ) {
				string description = GetErrorDescription( result );
				const string format = "{0} (Description: {1})";
				throw new InvalidOperationException( String.Format( format, result, description ) );
			}
		}
		
		public void Dispose() {
			if( handle != IntPtr.Zero ) {
				Console.WriteLine( "disposing" );
				Close( handle );
			}
			buffersHandle.Free();
		}
		
		WaveHeader[] buffers;
		GCHandle buffersHandle;
		int buffersIndex;
		
		struct UserData {
			public GCHandle BufferHandle;
			public int Index;
		}
	}
}
