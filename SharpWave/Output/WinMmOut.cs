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
		
		LastChunk last;
		public LastChunk Last { get { return last; } }
		int volumePercent = 100;

		public void SetVolume(float volume) { volumePercent = (int)(volume * 100); }

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
		
		public void Initalise( AudioChunk first ) {
			// Don't need to recreate device if it's the same.
			if( last.BitsPerSample == first.BitsPerSample
			   && last.Channels    == first.Channels
			   && last.SampleRate  == first.SampleRate ) return;
			
			last.SampleRate    = first.SampleRate;
			last.BitsPerSample = first.BitsPerSample;
			last.Channels      = first.Channels;
			
			Console.WriteLine( "init" );
			DisposeDevice();
			WaveFormatEx format = new WaveFormatEx();
			
			format.Channels = (ushort)first.Channels;
			format.ExtraSize = 0;
			format.FormatTag = WaveFormatTag.Pcm;
			format.BitsPerSample = (ushort)first.BitsPerSample;
			format.BlockAlign = (ushort)(format.Channels * format.BitsPerSample / 8);
			format.SampleRate = (uint)first.SampleRate;
			format.AverageBytesPerSecond = (int)format.SampleRate * format.BlockAlign;
			
			WaveOpenFlags flags = WaveOpenFlags.CallbackNull;
			uint devices = WinMmNative.waveOutGetNumDevs();
			if( devices == 0 )
				throw new InvalidOperationException( "No audio devices found" );

			uint result = WinMmNative.waveOutOpen( out devHandle, (IntPtr)(-1), ref format,
			                                      IntPtr.Zero, UIntPtr.Zero, flags );
			CheckError( result, "Open" );
		}
		
		unsafe void UpdateBuffer( int index, AudioChunk chunk ) {
			WaveHeader header = new WaveHeader();
			byte[] data = chunk.Data;
			CheckBufferSize( index, chunk.Length );
			IntPtr handle = dataHandles[index];
			fixed( byte* src = data ) {
				byte* chunkPtr = src + chunk.BytesOffset;
				MemUtils.memcpy( (IntPtr)chunkPtr, handle, chunk.Length );
				ApplyVolume( handle, chunk );
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
		
		unsafe void ApplyVolume( IntPtr handle, AudioChunk chunk ) {
			if (volumePercent == 100) return;
			
			if (chunk.BitsPerSample == 16) {
				VolumeMixer.Mix16((short*)handle, chunk.Length / sizeof(short), volumePercent);
			} else if (chunk.BitsPerSample == 8) {
				VolumeMixer.Mix8((byte*)handle, chunk.Length, volumePercent);
			}
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
