using System;
using System.Runtime.InteropServices;
using System.Threading;
using SharpWave.Codecs;

namespace SharpWave {
	
	/// <summary> Outputs audio to the default sound playback device using the
	/// native WinMm library. Windows only. </summary>
	public sealed partial class WinMmOut : IAudioOutput {
		
		IntPtr devHandle;
		readonly int waveHeaderSize;
		public WinMmOut() {
			waveHeaderSize = Marshal.SizeOf( default( WaveHeader ) );
			
		}
		IntPtr headers;
		IntPtr[] dataHandles;
		int[] dataSizes;
		float volume = 1, pitch = 1;
		
		// TODO: need to check device support
		public void SetVolume(float volume) {
			this.volume = volume;
			if (devHandle == IntPtr.Zero) return;
			
			uint packed = (uint)(volume * 0xFFFF);
			packed = (packed << 16) | packed; // left and right same
			uint result = WinMmNative.waveOutSetVolume(devHandle, packed);
			CheckError( result, "SetVolume" );
		}
		
		public void SetPitch(float pitch) {
			this.pitch = pitch;
			if (devHandle == IntPtr.Zero) return;
			
			uint packed = (uint)(pitch * 0x1000);
			uint result = WinMmNative.waveOutSetPitch(devHandle, packed);
			CheckError( result, "SetPitch" );
		}
		

		public void Create( int numBuffers ) {
			headers = Marshal.AllocHGlobal( waveHeaderSize * numBuffers );
			dataHandles = new IntPtr[numBuffers];
			dataSizes = new int[numBuffers];
		}
		
		public void Create( int numBuffers, IAudioOutput share ) {
			Create( numBuffers );
		}
		
		public unsafe void PlayRaw( AudioChunk chunk ) {
			Initalise( chunk );
			UpdateBuffer( 0, chunk );
			
			while( true ) {
				WaveHeader header = *((WaveHeader*)headers);
				if( (header.Flags & WaveHeaderFlags.Done) != 0 ) {
					Free( 0 );
					break;
				}
				Thread.Sleep( 1 );
			}
		}
		
		bool playingAsync;
		public void PlayRawAsync( AudioChunk chunk ) {
			Initalise( chunk );
			UpdateBuffer( 0, chunk );
			playingAsync = true;
		}
		
		public unsafe bool DoneRawAsync() {
			if( !playingAsync ) return true;
			WaveHeader header = *((WaveHeader*)headers);
			if( (header.Flags & WaveHeaderFlags.Done) != 0 ) {
				Free( 0 );
				playingAsync = false;
				return true;
			}
			return false;
		}
		
		
		bool pendingStop;
		public void Stop() { pendingStop = true; }
		
		int lastFreq = -1, lastBits = -1, lastChannels = -1;
		public void Initalise( AudioChunk first ) {
			// Don't need to recreate device if it's the same.
			if( lastBits == first.BitsPerSample && lastChannels == first.Channels && lastFreq == first.Frequency )
				return;
			
			lastFreq = first.Frequency;
			lastBits = first.BitsPerSample;
			lastChannels = first.Channels;
			
			Console.WriteLine( "init" );
			DisposeDevice();
			WaveFormatEx format = new WaveFormatEx();
			
			format.Channels = (ushort)first.Channels;
			format.ExtraSize = 0;
			format.FormatTag = WaveFormatTag.Pcm;
			format.BitsPerSample = (ushort)first.BitsPerSample;
			format.BlockAlign = (ushort)(format.Channels * format.BitsPerSample / 8);
			format.SampleRate = (uint)first.Frequency;
			format.AverageBytesPerSecond = first.Frequency * format.BlockAlign;
			
			WaveOpenFlags flags = WaveOpenFlags.CallbackNull;
			uint devices = WinMmNative.waveOutGetNumDevs();
			if( devices == 0 )
				throw new InvalidOperationException( "No audio devices found" );
			
			uint result = WinMmNative.waveOutOpen( out devHandle, new UIntPtr( 0xFFFF ), ref format,
			                                      IntPtr.Zero, UIntPtr.Zero, flags );
			CheckError( result, "Open" );
			
			if (volume != 1) SetVolume(volume);
			if (pitch != 1)  SetPitch(pitch);
		}
		
		unsafe void UpdateBuffer( int index, AudioChunk chunk ) {
			WaveHeader header = new WaveHeader();
			byte[] data = chunk.Data;
			CheckBufferSize( index, chunk.Length );
			IntPtr handle = dataHandles[index];
			fixed( byte* src = data ) {
				byte* chunkPtr = src + chunk.BytesOffset;
				MemUtils.memcpy( (IntPtr)chunkPtr, handle, chunk.Length );
			}
			
			header.DataBuffer = handle;
			header.BufferLength = chunk.Length;
			header.Loops = 1;
			IntPtr address = (IntPtr)((byte*)headers + index * waveHeaderSize );
			*((WaveHeader*)address) = header;
			
			uint result = WinMmNative.waveOutPrepareHeader( devHandle, address, (uint)waveHeaderSize );
			CheckError( result, "PrepareHeader" );
			result = WinMmNative.waveOutWrite( devHandle, address, (uint)waveHeaderSize );
			CheckError( result, "Write" );
		}
		
		void CheckBufferSize( int index, int chunkDataSize ) {
			if( chunkDataSize <= dataSizes[index] ) return;
			
			IntPtr ptr = dataHandles[index];
			if( ptr != IntPtr.Zero )
				Marshal.FreeHGlobal( ptr );
			dataHandles[index] = Marshal.AllocHGlobal( chunkDataSize );
		}
		
		unsafe void Free( int index ) {
			IntPtr address = (IntPtr)((byte*)headers + index * waveHeaderSize );
			uint result = WinMmNative.waveOutUnprepareHeader( devHandle, address, (uint)waveHeaderSize );
			CheckError( result, "UnprepareHeader" );
		}
		
		void CheckError( uint result, string func ) {
			if( result == 0 ) return;
			
			string description = WinMmNative.GetErrorDescription( result );
			const string format = "{0} at {1} ({2})";
			string text = String.Format( format, result, func, description );
			throw new InvalidOperationException( text );
		}
		
		public void Dispose() {
			Console.WriteLine( "dispose" );
			DisposeDevice();
			for( int i = 0; i < dataHandles.Length; i++ )
				Marshal.FreeHGlobal( dataHandles[i] );
		}
		
		void DisposeDevice() {
			if( devHandle == IntPtr.Zero) return;
			
			Console.WriteLine( "disposing device" );
			uint result = WinMmNative.waveOutClose( devHandle );
			CheckError( result, "Close" );
			devHandle = IntPtr.Zero;
		}
	}
}
