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

		[DllImport(AL.Lib, EntryPoint = "alEnable", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		public static extern void Enable(ALCapability capability);
		
		[DllImport(AL.Lib, EntryPoint = "alDisable", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		public static extern void Disable(ALCapability capability);
		
		[DllImport(AL.Lib, EntryPoint = "alIsEnabled", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		public static extern bool IsEnabled(ALCapability capability);

		[DllImport(AL.Lib, EntryPoint = "alGetString", ExactSpelling = true, CallingConvention = AL.Style, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity]
		private static extern IntPtr GetStringPrivate(ALGetString param);
		public static string Get(ALGetString param) {
			return Marshal.PtrToStringAnsi(GetStringPrivate(param));
		}

		public static string GetErrorString(ALError param) {
			return Marshal.PtrToStringAnsi(GetStringPrivate((ALGetString)param));
		}

		[DllImport(AL.Lib, EntryPoint = "alGetInteger", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		public static extern int Get(ALGetInteger param);
		
		[DllImport(AL.Lib, EntryPoint = "alGetFloat", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		public static extern float Get(ALGetFloat param);

		[DllImport(AL.Lib, EntryPoint = "alGetError", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		public static extern ALError GetError();
		

		[DllImport(AL.Lib, EntryPoint = "alIsExtensionPresent", ExactSpelling = true, CallingConvention = AL.Style, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity]
		public static extern bool IsExtensionPresent([In] string extname);
		
		[DllImport(AL.Lib, EntryPoint = "alGetProcAddress", ExactSpelling = true, CallingConvention = AL.Style, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr GetProcAddress([In] string fname);
		
		[DllImport(AL.Lib, EntryPoint = "alGetEnumValue", ExactSpelling = true, CallingConvention = AL.Style, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity]
		public static extern int GetEnumValue([In] string ename);

		[DllImport(AL.Lib, EntryPoint = "alGenSources", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		private static extern void GenSourcesPrivate(int n, [Out] uint* sources);
		
		public static void GenSources(int n, out uint sources) {
			fixed (uint* sources_ptr = &sources)
				GenSourcesPrivate(n, sources_ptr);
		}

		[DllImport(AL.Lib, EntryPoint = "alDeleteSources", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		public static extern void DeleteSources(int n, [In] uint* sources);
		
		[DllImport(AL.Lib, EntryPoint = "alDeleteSources", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		public static extern void DeleteSources(int n, ref uint sources);

		[DllImport(AL.Lib, EntryPoint = "alIsSource", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		public static extern bool IsSource(uint sid);

		[DllImport(AL.Lib, EntryPoint = "alSourcef", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		public static extern void Source(uint sid, ALSourcef param, float value);

		[DllImport(AL.Lib, EntryPoint = "alSourcei", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		public static extern void Source(uint sid, ALSourcei param, uint value);

		public static void BindBufferToSource(uint source, uint buffer) {
			Source(source, ALSourcei.Buffer, buffer);
		}
		
		[DllImport(AL.Lib, EntryPoint = "alGetSourcef", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		public static extern void GetSource(uint sid, ALSourcef param, [Out] out float value);

		[DllImport(AL.Lib, EntryPoint = "alGetSourcei", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		public static extern void GetSource(uint sid, ALGetSourcei param, [Out] out int value);

		public static void GetSource(uint sid, ALSourceb param, out bool value) {
			int result;
			GetSource(sid, (ALGetSourcei)param, out result);
			value = result != 0;
		}

		[DllImport(AL.Lib, EntryPoint = "alSourcePlayv"), SuppressUnmanagedCodeSecurity]
		public static extern void SourcePlay(int ns, [In] uint* sids);
		
		public static void SourcePlay(int ns, uint[] sids) {
			fixed (uint* ptr = sids)
				SourcePlay(ns, ptr);
		}

		public static void SourcePlay(int ns, ref uint sids) {
			fixed (uint* ptr = &sids)
				SourcePlay(ns, ptr);
		}

		[DllImport(AL.Lib, EntryPoint = "alSourceStopv"), SuppressUnmanagedCodeSecurity]
		public static extern void SourceStop(int ns, [In] uint* sids);
		
		public static void SourceStop(int ns, uint[] sids) {
			fixed (uint* ptr = sids)
				SourceStop(ns, ptr);
		}

		public static void SourceStop(int ns, ref uint sids) {
			fixed (uint* ptr = &sids)
				SourceStop(ns, ptr);
		}

		[DllImport(AL.Lib, EntryPoint = "alSourceRewindv"), SuppressUnmanagedCodeSecurity]
		public static extern void SourceRewind(int ns, [In] uint* sids);
		
		public static void SourceRewind(int ns, uint[] sids) {
			fixed (uint* ptr = sids)
				SourceRewind(ns, ptr);
		}

		public static void SourceRewind(int ns, ref uint sids) {
			fixed (uint* ptr = &sids)
				SourceRewind(ns, ptr);
		}

		[DllImport(AL.Lib, EntryPoint = "alSourcePausev"), SuppressUnmanagedCodeSecurity]
		public static extern void SourcePause(int ns, [In] uint* sids);
		
		public static void SourcePause(int ns, uint[] sids) {
			fixed (uint* ptr = sids)
				SourcePause(ns, ptr);
		}

		public static void SourcePause(int ns, ref uint sids) {
			fixed (uint* ptr = &sids)
				SourcePause(ns, ptr);
		}

		[DllImport(AL.Lib, EntryPoint = "alSourcePlay", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		public static extern void SourcePlay(uint sid);

		[DllImport(AL.Lib, EntryPoint = "alSourceStop", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		public static extern void SourceStop(uint sid);

		[DllImport(AL.Lib, EntryPoint = "alSourceRewind", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		public static extern void SourceRewind(uint sid);

		[DllImport(AL.Lib, EntryPoint = "alSourcePause", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		public static extern void SourcePause(uint sid);

		[DllImport(AL.Lib, EntryPoint = "alSourceQueueBuffers"), SuppressUnmanagedCodeSecurity]
		public static extern void SourceQueueBuffers(uint sid, int numEntries, [In] uint* bids);
		
		public static void SourceQueueBuffers(uint sid, int numEntries, uint[] bids) {
			fixed (uint* ptr = bids)
				SourceQueueBuffers(sid, numEntries, ptr);
		}

		public static void SourceQueueBuffers(uint sid, int numEntries, ref uint bids) {
			fixed (uint* ptr = &bids)
				SourceQueueBuffers(sid, numEntries, ptr);
		}

		[DllImport(AL.Lib, EntryPoint = "alSourceUnqueueBuffers"), SuppressUnmanagedCodeSecurity]
		public static extern void SourceUnqueueBuffers(uint sid, int numEntries, [In] uint* bids);
		
		[DllImport(AL.Lib, EntryPoint = "alSourceUnqueueBuffers"), SuppressUnmanagedCodeSecurity]
		public static extern void SourceUnqueueBuffers(uint sid, int numEntries, [Out] uint[] bids);

		[DllImport(AL.Lib, EntryPoint = "alSourceUnqueueBuffers"), SuppressUnmanagedCodeSecurity]
		public static extern void SourceUnqueueBuffers(uint sid, int numEntries, ref uint bids);
		

		[DllImport(AL.Lib, EntryPoint = "alGenBuffers", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		public static extern void GenBuffers(int n, [Out] uint* buffers);
		
		[DllImport(AL.Lib, EntryPoint = "alGenBuffers", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		public static extern void GenBuffers(int n, out uint buffers);

		[DllImport(AL.Lib, EntryPoint = "alDeleteBuffers", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		public static extern void DeleteBuffers(int n, [In] uint* buffers);
		
		[DllImport(AL.Lib, EntryPoint = "alDeleteBuffers", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		public static extern void DeleteBuffers(int n, [In] ref uint buffers);

		public static void DeleteBuffers(uint[] buffers) {
			if (buffers == null) throw new ArgumentNullException();
			if (buffers.Length == 0) throw new ArgumentOutOfRangeException();
			DeleteBuffers(buffers.Length, ref buffers[0]);
		}

		[DllImport(AL.Lib, EntryPoint = "alIsBuffer", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		public static extern bool IsBuffer(uint bid);

		[DllImport(AL.Lib, EntryPoint = "alBufferData", ExactSpelling = true, CallingConvention = AL.Style), SuppressUnmanagedCodeSecurity]
		public static extern void BufferData(uint bid, ALFormat format, IntPtr buffer, int size, int freq);
	}
}
