using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using SharpWave.Codecs;
using SharpWave.Containers;

namespace SharpWave {
	
	public sealed class AudioOutputWinMm : IAudioOutput {
		
		IntPtr handle;
		readonly int waveHeaderSize;
		WaveOutCallback callback;
		public AudioOutputWinMm( int bufferSize ) {			
			waveHeaderSize = Marshal.SizeOf( default( WaveHeader ) );
			buffers = new WaveHeader[bufferSize];
			buffersHandle = GCHandle.Alloc( buffers, GCHandleType.Pinned );
			this.bufferSize = bufferSize;
		}
		IEnumerator<AudioChunk> chunks;
		bool reachedEnd = false;
		int bufferSize;
		
		public void StreamData( IMediaContainer container ) {
			
			container.ReadMetadata();
			ICodec codec = container.GetAudioCodec();		
			chunks = codec.StreamData( container ).GetEnumerator();

			for( int i = 0; i < bufferSize; i++ ) {
				if( chunks.MoveNext() ) {
					// We have to delay initialisation until we read the first chunk,
					// as we cannot change wave format after initialisation.
					if( i == 0 ) {
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
			callback = ProcessWaveOutCallback;
			uint result = Open( out handle, new UIntPtr( (uint)0xFFFF ), ref format, callback, UIntPtr.Zero, flags );
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
		
		void SendNextBuffer( AudioChunk chunk ) {
			Console.WriteLine( "send buffer " + buffersIndex );
			WaveHeader header = new WaveHeader();
			byte[] data = chunk.Data;
			GCHandle bufferHandle = GCHandle.Alloc( data, GCHandleType.Pinned );
			header.DataBuffer = bufferHandle.AddrOfPinnedObject();
			header.BufferLength = data.Length;
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
		
		void ProcessWaveOutCallback( IntPtr handle, WaveOutMessage message, UIntPtr user, ref WaveHeader header, UIntPtr reserved ) {
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
		
		static string GetErrorDescription( uint error ) {
			StringBuilder message = new StringBuilder( 512 );
			uint result = GetErrorText( error, message, (uint)message.Capacity );
			if( result == 0 ) {
				return message.ToString();
			}
			return "waveOutGetErrorText failed.";
		}
		
		#region Native
		
		public enum MmResult : uint {
			NoError = 0,
			Error = 1,
			BadDeviceId = 2,
			NotEnabled = 3,
			Allocated = 4,
			InvalidHandle = 5,
			NoDriver = 6,
			OutOfMemory = 7,
			NotSupported = 8,
			BadErrorNumber = 9,
			InvalidFlag = 10,
			InvalidParameter = 11,
			HandleBusy = 12,
			InvalidAlias = 13,
			BadRegistryDatabase = 14,
			RegistryKeyNotFound = 15,
			RegistryReadError = 16,
			RegistryWriteError = 17,
			RegistryDeleteError = 18,
			RegistryValueNotFound = 19,
			NoDriverCallback = 20,
			
			WaveBadFormat = 32,
			WaveStillPlaying = 33,
			WaveHeaderUnprepared = 34,
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
		
		[DllImport( "winmm.dll", EntryPoint = "waveOutGetPosition", SetLastError = true, CharSet = CharSet.Auto )]
		static extern uint GetPosition( IntPtr handle, ref MmTime time, uint timeSize );
		
		[DllImport( "winmm.dll", EntryPoint = "waveOutGetNumDevs", SetLastError = true, CharSet = CharSet.Auto )]
		static extern uint GetNumberOfDevices();
		
		[DllImport( "winmm.dll", EntryPoint = "waveOutGetDevCaps", SetLastError = true, CharSet = CharSet.Auto )]
		static extern uint GetDeviceCaps( IntPtr handle, ref WaveOutDevCaps devCaps, uint devCapsSize );
		
		#endregion
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
	
	[StructLayout(LayoutKind.Explicit)]
	public struct MmTime {
		[FieldOffset( 0 )]
		public uint wType;
		[FieldOffset( 4 )]
		public uint MillisecondCount;
		[FieldOffset( 4 )]
		public uint SampleCount;
		[FieldOffset( 4 )]
		public uint ByteCount;
		[FieldOffset( 4 )]
		public uint TickCount;
		[FieldOffset( 4 )]
		public byte smpteHour;
		[FieldOffset( 5 )]
		public byte smpteMin;
		[FieldOffset( 6 )]
		public byte smpteSec;
		[FieldOffset( 7 )]
		public byte smpteFrame;
		[FieldOffset( 8 )]
		public byte smpteFps;
		[FieldOffset( 9 )]
		public byte smpteDummy;
		[FieldOffset( 10 )]
		public byte smptePad0;
		[FieldOffset( 11 )]
		public byte smptePad1;
		[FieldOffset( 4 )]
		public uint midiSongPtrPos;
	}
	
	public enum WaveFormatTag : ushort {
		Invalid = 0x00,
		Pcm = 0x01,
		Adpcm = 0x02,
		IeeeFloat = 0x03,
		ALaw = 0x04,
		MuLaw = 0x05,
	}
	
	[StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Auto)]
	public struct WaveOutDevCaps {
		public ushort wMid;
		public ushort wPid;
		public uint vDriverVersion;
		[MarshalAs( UnmanagedType.ByValTStr, SizeConst = 32 )]
		public string szPname;
		public uint dwFormats;
		public ushort wChannels;
		public ushort wReserved1;
		public uint dwSupport;
	}
	
	[Flags]
	public enum WaveHeaderFlags : uint {
		Done = 0x01,
		Prepared = 0x02,
		BeginLoop = 0x04,
		EndLoop = 0x08,
		InQueue = 0x10,
	}
}
