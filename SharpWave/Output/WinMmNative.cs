using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace SharpWave {
	
	/// <summary> Outputs audio to the default sound playback device using the
	/// native WinMm library. Windows only. </summary>
	internal static class WinMmNative {
		
		[DllImport( "winmm.dll", SetLastError = true, CharSet = CharSet.Auto ), SuppressUnmanagedCodeSecurity]
		internal static extern uint waveOutGetErrorText( uint error, StringBuilder buffer, uint bufferLen );

		[DllImport( "winmm.dll", SetLastError = true, CharSet = CharSet.Auto ), SuppressUnmanagedCodeSecurity]
		internal static extern uint waveOutOpen( out IntPtr handle, UIntPtr deviceID, ref WaveFormatEx format,
		                                        IntPtr callback, UIntPtr callbackInstance, WaveOpenFlags flags );
		
		[DllImport( "winmm.dll", SetLastError = true ), SuppressUnmanagedCodeSecurity]
		internal static extern uint waveOutClose( IntPtr handle );
		
		[DllImport( "winmm.dll", SetLastError = true ), SuppressUnmanagedCodeSecurity]
		internal static extern uint waveOutPrepareHeader( IntPtr handle, IntPtr header, uint hdrSize );
		
		[DllImport( "winmm.dll", SetLastError = true ), SuppressUnmanagedCodeSecurity]
		internal static extern uint waveOutUnprepareHeader( IntPtr handle, IntPtr header, uint hdrSize );
		
		[DllImport( "winmm.dll", SetLastError = true ), SuppressUnmanagedCodeSecurity]
		internal static extern uint waveOutWrite( IntPtr handle, IntPtr header, uint hdrSize );
		
		[DllImport( "winmm.dll", SetLastError = true ), SuppressUnmanagedCodeSecurity]
		internal static extern uint waveOutGetNumDevs();
		
		[DllImport( "winmm.dll", SetLastError = true ), SuppressUnmanagedCodeSecurity]
		public static extern uint waveOutSetVolume( IntPtr handle, uint volume );
		
		[DllImport( "winmm.dll", SetLastError = true ), SuppressUnmanagedCodeSecurity]
		public static extern uint waveOutSetPitch( IntPtr handle, uint pitch );
		
		internal static string GetErrorDescription( uint error ) {
			StringBuilder message = new StringBuilder( 1024 );
			uint result = waveOutGetErrorText( error, message, (uint)message.Capacity );
			if( result == 0 )
				return message.ToString();
			return "waveOutGetErrorText failed.";
		}
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
		public IntPtr Reserved;
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
