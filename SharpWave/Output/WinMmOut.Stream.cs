using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using SharpWave.Codecs;
using SharpWave.Containers;

namespace SharpWave {
	
	public sealed partial class AudioOutputWinMm : IAudioOutput {
		
		IEnumerator<AudioChunk> chunks;
		public void PlayStreaming( IMediaContainer container ) {
			
			container.ReadMetadata();
			ICodec codec = container.GetAudioCodec();
			chunks = codec.StreamData( container ).GetEnumerator();

			for( int i = 0; i < bufferSize; i++ ) {
				if( chunks.MoveNext() ) {
					// We have to delay initialisation until we read the first chunk,
					// as we cannot change wave format after initialisation.
					if( i == 0 ) {
						callback = ProcessStreamingWaveOutCallback;
						InitWinMm( chunks.Current );
					}
					SendNextBuffer( chunks.Current );
					buffersIndex++;
				}
			}
			while( true ) {
				Thread.Sleep( 1 );
				// TODO: fix playing slightly after disposed.
				if( reachedEnd ) break;
			}
		}
		
		void InitWinMm( AudioChunk chunk ) {
			handle = IntPtr.Zero;
			WaveFormatEx format = new WaveFormatEx();
			format.Channels = (ushort)chunk.Channels;
			format.ExtraSize = 0;
			format.FormatTag = WaveFormatTag.Pcm;
			format.BitsPerSample = (ushort)chunk.BitsPerSample;
			format.BlockAlign = (ushort)( format.Channels * format.BitsPerSample / 8 );
			format.SampleRate = (uint)chunk.Frequency;
			format.AverageBytesPerSecond = chunk.Frequency * format.BlockAlign;
			
			WaveOpenFlags flags = WaveOpenFlags.CallbackFunction;			
			uint result = Open( out handle, new UIntPtr( (uint)0xFFFF ), ref format, callback, UIntPtr.Zero, flags );
			CheckError( result );
		}
		
		void ProcessStreamingWaveOutCallback( IntPtr handle, WaveOutMessage message, UIntPtr user, ref WaveHeader header, UIntPtr reserved ) {
			Console.WriteLine( "callback:" + message + "," + buffersIndex );
			if( message == WaveOutMessage.Done ) {
				Free( ref header );
				// TODO: This probably needs to be rewritten.
				// Due to the asynchronous nature of WinMm, this function can be invoked on the thread that plays the music (I think?)
				// It would be much better regardless of this was invoked on the thread that called the player to begin with.
				if( chunks.MoveNext() ) {
					SendNextBuffer( chunks.Current );
					buffersIndex++;
				} else {
					if( buffersIndex <= 0 ) {
						reachedEnd = true;
					}
				}
			}
		}

		delegate void WaveOutCallback( IntPtr handle, WaveOutMessage message, UIntPtr user, ref WaveHeader waveHeader, UIntPtr reserved );

		[DllImport( "winmm.dll", EntryPoint = "waveOutGetErrorText", SetLastError = true, CharSet = CharSet.Auto )]
		static extern uint GetErrorText( uint error, StringBuilder buffer, uint buffferLength );

		[DllImport( "winmm.dll", EntryPoint = "waveOutOpen", SetLastError = true, CharSet = CharSet.Auto )]
		static extern uint Open( out IntPtr handle, UIntPtr deviceID, ref WaveFormatEx format,
		                        WaveOutCallback callback, UIntPtr callbackInstance, WaveOpenFlags flags );
		
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
			if( result == 0 ) {
				return message.ToString();
			}
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
		
		public enum WaveOutMessage : uint {
			Open = 0x3BB,
			Close = 0x3BC,
			Done = 0x3BD,
		}

		[Flags]
		public enum WaveOpenFlags : uint {
			WaveFormatQuery = 0x01,
			WaveAllowSync = 0x02,
			WaveMapped = 0x04,
			WaveFormatDirect = 0x08,
			
			CallbackNull = 0x00000,
			CallbackWindow = 0x10000,
			CallbackThread = 0x20000,
			CallbackFunction = 0x30000,
		}
	}
}
