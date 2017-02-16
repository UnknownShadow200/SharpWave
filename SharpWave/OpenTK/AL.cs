/* AlFunctions.cs
 * C header: \OpenAL 1.1 SDK\include\Al.h
 * Spec: http://www.openal.org/openal_webstf/specs/OpenAL11Specification.pdf
 * Copyright (c) 2008 Christoph Brandtner and Stefanos Apostolopoulos
 * See license.txt for license details
 * http://www.OpenTK.net */

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace OpenTK.Audio.OpenAL {
	
	public unsafe static partial class AL {
		
		internal const string Lib = "openal32.dll";
		private const CallingConvention Style = CallingConvention.Cdecl;

		[DllImport(Lib, EntryPoint = "alGetString", ExactSpelling = true, CallingConvention = Style, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity]
		private static extern IntPtr GetStringPrivate(ALGetString param);
		public static string Get(ALGetString param) {
			return Marshal.PtrToStringAnsi(GetStringPrivate(param));
		}

		public static string GetErrorString(ALError param) {
			return Marshal.PtrToStringAnsi(GetStringPrivate((ALGetString)param));
		}
		
		[DllImport(Lib, EntryPoint = "alDistanceModel", ExactSpelling = true, CallingConvention = Style), SuppressUnmanagedCodeSecurity]
		public static extern void DistanceModel(ALDistanceModel param);

		[DllImport(Lib, EntryPoint = "alGetError", ExactSpelling = true, CallingConvention = Style), SuppressUnmanagedCodeSecurity]
		public static extern ALError GetError();		

		[DllImport(Lib, EntryPoint = "alIsExtensionPresent", ExactSpelling = true, CallingConvention = Style, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity]
		public static extern bool IsExtensionPresent([In] string extname);
		
		[DllImport(Lib, EntryPoint = "alGetProcAddress", ExactSpelling = true, CallingConvention = Style, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr GetProcAddress([In] string fname);
		
		[DllImport(Lib, EntryPoint = "alGetEnumValue", ExactSpelling = true, CallingConvention = Style, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity]
		public static extern int GetEnumValue([In] string ename);

		[DllImport(Lib, EntryPoint = "alGenSources", ExactSpelling = true, CallingConvention = Style), SuppressUnmanagedCodeSecurity]
		public static extern void GenSources(int n, [Out] uint* sources);
		
		public static void GenSources(int n, out uint sources) {
			fixed (uint* sources_ptr = &sources)
				GenSources(n, sources_ptr);
		}

		[DllImport(Lib, EntryPoint = "alDeleteSources", ExactSpelling = true, CallingConvention = Style), SuppressUnmanagedCodeSecurity]
		public static extern void DeleteSources(int n, [In] uint* sources);
		
		[DllImport(Lib, EntryPoint = "alDeleteSources", ExactSpelling = true, CallingConvention = Style), SuppressUnmanagedCodeSecurity]
		public static extern void DeleteSources(int n, ref uint sources);

		[DllImport(Lib, EntryPoint = "alIsSource", ExactSpelling = true, CallingConvention = Style), SuppressUnmanagedCodeSecurity]
		public static extern bool IsSource(uint sid);

		[DllImport(Lib, EntryPoint = "alSourcei", ExactSpelling = true, CallingConvention = Style), SuppressUnmanagedCodeSecurity]
		public static extern void Source(uint sid, ALSourcei param, uint value);

		[DllImport(Lib, EntryPoint = "alSourcef", ExactSpelling = true, CallingConvention = Style), SuppressUnmanagedCodeSecurity]		
		public static extern void Source(uint sid, ALSourcef param, float value);

		[DllImport(Lib, EntryPoint = "alGetSourcei", ExactSpelling = true, CallingConvention = Style), SuppressUnmanagedCodeSecurity]
		public static extern void GetSource(uint sid, ALGetSourcei param, [Out] out int value);

		[DllImport(Lib, EntryPoint = "alSourcePlayv"), SuppressUnmanagedCodeSecurity]
		public static extern void SourcePlay(int ns, [In] uint* sids);

		public static void SourcePlay(int ns, ref uint sids) {
			fixed (uint* ptr = &sids)
				SourcePlay(ns, ptr);
		}

		[DllImport(Lib, EntryPoint = "alSourceStopv"), SuppressUnmanagedCodeSecurity]
		public static extern void SourceStop(int ns, [In] uint* sids);

		public static void SourceStop(int ns, ref uint sids) {
			fixed (uint* ptr = &sids)
				SourceStop(ns, ptr);
		}

		[DllImport(Lib, EntryPoint = "alSourceRewindv"), SuppressUnmanagedCodeSecurity]
		public static extern void SourceRewind(int ns, [In] uint* sids);

		public static void SourceRewind(int ns, ref uint sids) {
			fixed (uint* ptr = &sids)
				SourceRewind(ns, ptr);
		}

		[DllImport(Lib, EntryPoint = "alSourcePausev"), SuppressUnmanagedCodeSecurity]
		public static extern void SourcePause(int ns, [In] uint* sids);

		public static void SourcePause(int ns, ref uint sids) {
			fixed (uint* ptr = &sids)
				SourcePause(ns, ptr);
		}

		[DllImport(Lib, EntryPoint = "alSourcePlay", ExactSpelling = true, CallingConvention = Style), SuppressUnmanagedCodeSecurity]
		public static extern void SourcePlay(uint sid);

		[DllImport(Lib, EntryPoint = "alSourceStop", ExactSpelling = true, CallingConvention = Style), SuppressUnmanagedCodeSecurity]
		public static extern void SourceStop(uint sid);

		[DllImport(Lib, EntryPoint = "alSourceRewind", ExactSpelling = true, CallingConvention = Style), SuppressUnmanagedCodeSecurity]
		public static extern void SourceRewind(uint sid);

		[DllImport(Lib, EntryPoint = "alSourcePause", ExactSpelling = true, CallingConvention = Style), SuppressUnmanagedCodeSecurity]
		public static extern void SourcePause(uint sid);

		[DllImport(Lib, EntryPoint = "alSourceQueueBuffers"), SuppressUnmanagedCodeSecurity]
		public static extern void SourceQueueBuffers(uint sid, int numEntries, [In] uint* bids);
		
		public static void SourceQueueBuffers(uint sid, int numEntries, uint[] bids) {
			fixed (uint* ptr = bids)
				SourceQueueBuffers(sid, numEntries, ptr);
		}

		public static void SourceQueueBuffers(uint sid, int numEntries, ref uint bids) {
			fixed (uint* ptr = &bids)
				SourceQueueBuffers(sid, numEntries, ptr);
		}

		[DllImport(Lib, EntryPoint = "alSourceUnqueueBuffers"), SuppressUnmanagedCodeSecurity]
		public static extern void SourceUnqueueBuffers(uint sid, int numEntries, [In] uint* bids);
		
		[DllImport(Lib, EntryPoint = "alSourceUnqueueBuffers"), SuppressUnmanagedCodeSecurity]
		public static extern void SourceUnqueueBuffers(uint sid, int numEntries, [Out] uint[] bids);

		[DllImport(Lib, EntryPoint = "alSourceUnqueueBuffers"), SuppressUnmanagedCodeSecurity]
		public static extern void SourceUnqueueBuffers(uint sid, int numEntries, ref uint bids);
		

		[DllImport(Lib, EntryPoint = "alGenBuffers", ExactSpelling = true, CallingConvention = Style), SuppressUnmanagedCodeSecurity]
		public static extern void GenBuffers(int n, [Out] uint* buffers);
		
		[DllImport(Lib, EntryPoint = "alGenBuffers", ExactSpelling = true, CallingConvention = Style), SuppressUnmanagedCodeSecurity]
		public static extern void GenBuffers(int n, out uint buffers);

		[DllImport(Lib, EntryPoint = "alDeleteBuffers", ExactSpelling = true, CallingConvention = Style), SuppressUnmanagedCodeSecurity]
		public static extern void DeleteBuffers(int n, [In] uint* buffers);
		
		[DllImport(Lib, EntryPoint = "alDeleteBuffers", ExactSpelling = true, CallingConvention = Style), SuppressUnmanagedCodeSecurity]
		public static extern void DeleteBuffers(int n, [In] ref uint buffers);

		public static void DeleteBuffers(uint[] buffers) {
			fixed(uint* ptr = buffers)
				DeleteBuffers(buffers.Length, ptr);
		}

		[DllImport(Lib, EntryPoint = "alIsBuffer", ExactSpelling = true, CallingConvention = Style), SuppressUnmanagedCodeSecurity]
		public static extern bool IsBuffer(uint bid);

		[DllImport(Lib, EntryPoint = "alBufferData", ExactSpelling = true, CallingConvention = Style), SuppressUnmanagedCodeSecurity]
		public static extern void BufferData(uint bid, ALFormat format, IntPtr buffer, int size, int freq);
	}
}
