using System;
using System.Runtime.InteropServices;
using System.Threading;
using SharpWave.Codecs;

namespace SharpWave {
	
	public sealed partial class WinMmOut : IAudioOutput {
		
		IntPtr handle;
		readonly int waveHeaderSize;
		public WinMmOut() {
			waveHeaderSize = Marshal.SizeOf( default( WaveHeader ) );
			headers = new WaveHeader[bufferSize];
			dataSizes = new int[bufferSize];
			dataHandles = new IntPtr[bufferSize];
		}
		const int bufferSize = 3;
		IntPtr[] dataHandles;
		int[] dataSizes;
		
		public void PlayRaw( AudioChunk chunk ) {
			Initalise( chunk );
			UpdateBuffer( 0, chunk );
			
			while( true ) {
				if( (headers[0].Flags & WaveHeaderFlags.Done) != 0 ) {
					Free( ref headers[0] );
					break;
				}
				Thread.Sleep( 1 );
			}
		}
		
		AudioChunk last;
		public void Initalise( AudioChunk first ) {
			// Don't need to recreate device if it's the same.
			if( last != null && last.BitsPerSample == first.BitsPerSample &&
			   last.Channels == first.Channels && last.Frequency == first.Frequency )
				return;
			
			Console.WriteLine( "init" );
			last = first;
			Dispose();
			WaveFormatEx format = new WaveFormatEx();
			
			format.Channels = (ushort)first.Channels;
			format.ExtraSize = 0;
			format.FormatTag = WaveFormatTag.Pcm;
			format.BitsPerSample = (ushort)first.BitsPerSample;
			format.BlockAlign = (ushort)(format.Channels * format.BitsPerSample / 8);
			format.SampleRate = (uint)first.Frequency;
			format.AverageBytesPerSecond = first.Frequency * format.BlockAlign;
			
			WaveOpenFlags flags = WaveOpenFlags.CallbackNull;
			uint result = Open( out handle, new UIntPtr( (uint)0xFFFF ), ref format, 
			                   IntPtr.Zero, UIntPtr.Zero, flags );
			CheckError( result );
		}
		
		unsafe void UpdateBuffer( int index, AudioChunk chunk ) {
			WaveHeader header = new WaveHeader();
			byte[] data = chunk.Data;
			CheckBufferSize( index, chunk.Length );
			IntPtr handle = dataHandles[index];			
			fixed( byte* src = data )
				MemUtils.memcpy( (IntPtr)src, handle, chunk.Length );
			
			header.DataBuffer = handle;
			header.BufferLength = chunk.Length;
			header.Loops = 1;
			headers[index] = header;
			
			uint result = PrepareHeader( handle, ref headers[index], (uint)waveHeaderSize );
			CheckError( result );
			result = Write( handle, ref headers[index], (uint)waveHeaderSize );
			CheckError( result );
		}
		
		void CheckBufferSize( int index, int chunkDataSize ) {		
			if( chunkDataSize <= dataSizes[index] ) return;
			
			IntPtr ptr = dataHandles[index];
			if( ptr != IntPtr.Zero )
				Marshal.FreeHGlobal( ptr );
			ptr = Marshal.AllocHGlobal( chunkDataSize );
			dataHandles[index] = ptr;
		}
		
		void Free( ref WaveHeader header ) {
			uint result = UnprepareHeader( handle, ref header, (uint)waveHeaderSize );
			CheckError( result );
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
				handle = IntPtr.Zero;
			}
		}
		
		WaveHeader[] headers;
	}
}
