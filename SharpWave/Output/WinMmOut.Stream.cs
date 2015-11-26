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
		
		public void PlayStreaming( IMediaContainer container ) {
			container.ReadMetadata();
			ICodec codec = container.GetAudioCodec();
			IEnumerator<AudioChunk> chunks = 
				codec.StreamData( container ).GetEnumerator();

			int usedBuffers = 0;
			for( int i = 0; i < headers.Length; i++ ) {
				if( chunks.MoveNext() ) {
					if( i == 0 )
						Initalise( chunks.Current );
					UpdateBuffer( i, chunks.Current );
					usedBuffers++;
				}
			}
			Console.WriteLine( "used: " + usedBuffers );
			
			bool ranOutOfChunks = false;
			while( !AllDone( ranOutOfChunks, usedBuffers ) ) {
				for( int i = 0; i < usedBuffers; i++ ) {
					if( (headers[i].Flags & WaveHeaderFlags.Done) == 0 )
						continue;
					
					Free( ref headers[i] );
					if( !chunks.MoveNext() )
						ranOutOfChunks = true;
					else
						UpdateBuffer( i, chunks.Current );
				}
				Thread.Sleep( 1 );
			}
		}
		
		bool AllDone( bool ranOutOfChunks, int usedBuffers ) {
			if( !ranOutOfChunks ) return false;
			for( int i = 0; i < usedBuffers; i++ ) {
				if( (headers[i].Flags & WaveHeaderFlags.Done) == 0 )
					return false;
			}
			return true;
		}

		[DllImport( "winmm.dll", EntryPoint = "waveOutGetErrorText", SetLastError = true, CharSet = CharSet.Auto )]
		static extern uint GetErrorText( uint error, StringBuilder buffer, uint buffferLength );

		[DllImport( "winmm.dll", EntryPoint = "waveOutOpen", SetLastError = true, CharSet = CharSet.Auto )]
		static extern uint Open( out IntPtr handle, UIntPtr deviceID, ref WaveFormatEx format,
		                        IntPtr callback, UIntPtr callbackInstance, WaveOpenFlags flags );
		
		[DllImport( "winmm.dll", EntryPoint = "waveOutClose", SetLastError = true, CharSet = CharSet.Auto )]
		static extern uint Close( IntPtr handle );
		
		[DllImport( "winmm.dll", EntryPoint = "waveOutPrepareHeader", SetLastError = true, CharSet = CharSet.Auto )]
		static extern uint PrepareHeader( IntPtr handle, ref WaveHeader header, uint headerByteSize );
		
		[DllImport( "winmm.dll", EntryPoint = "waveOutUnprepareHeader", SetLastError = true, CharSet = CharSet.Auto )]
		static extern uint UnprepareHeader( IntPtr handle, ref WaveHeader header, uint headerByteSize );
		
		[DllImport( "winmm.dll", EntryPoint = "waveOutWrite", SetLastError = true, CharSet = CharSet.Auto )]
		static extern uint Write( IntPtr handle, ref WaveHeader header, uint headerByteSize );
		
		static string GetErrorDescription( uint error ) {
			StringBuilder message = new StringBuilder( 512 );
			uint result = GetErrorText( error, message, (uint)message.Capacity );
			if( result == 0 )
				return message.ToString();
			return "waveOutGetErrorText failed.";
		}
		
		[StructLayout( LayoutKind.Sequential, Pack = 2 )]
		public struct WaveFormatEx {
			public WaveFormatTag FormatTag;
			public ushort Channels;
			public uint SampleRate;
			public int AverageBytesPerSecond;
			public ushort BlockAlign;
			public ushort BitsPerSample;
			public ushort ExtraSize;
		}
		
		[StructLayout( LayoutKind.Sequential, Pack = 2 )]
		public struct WaveHeader {
			public IntPtr DataBuffer;
			public int BufferLength;
			public int BytesRecorded;
			public IntPtr UserData;
			public WaveHeaderFlags Flags;
			public int Loops;
			public IntPtr Next;
			public int Reserved;
		}
		
		public enum WaveFormatTag : ushort {
			Invalid = 0x00,
			Pcm = 0x01,
			ALaw = 0x04,
			MuLaw = 0x05,
		}
		
		[Flags]
		public enum WaveHeaderFlags : uint {
			Done = 0x01,
			Prepared = 0x02,
			BeginLoop = 0x04,
			EndLoop = 0x08,
			InQueue = 0x10,
		}

		[Flags]
		public enum WaveOpenFlags : uint {
			WaveFormatQuery = 0x01,
			WaveAllowSync = 0x02,
			WaveMapped = 0x04,
			WaveFormatDirect = 0x08,		
			CallbackNull = 0x00000,
		}
	}
}
