/* AlcFunctions.cs
 * C header: \OpenAL 1.1 SDK\include\Alc.h
 * Spec: http://www.openal.org/openal_webstf/specs/OpenAL11Specification.pdf
 * Copyright (c) 2008 Christoph Brandtner and Stefanos Apostolopoulos
 * See license.txt for license details
 * http://www.OpenTK.net */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;

namespace OpenTK.Audio.OpenAL {

	public static class Alc {
		
		private const string Lib = AL.Lib;
		private const CallingConvention Style = CallingConvention.Cdecl;

		[DllImport(Lib, EntryPoint = "alcCreateContext", ExactSpelling = true, CallingConvention = Style), SuppressUnmanagedCodeSecurity]
		public unsafe static extern IntPtr CreateContext([In] IntPtr device, [In] int* attrlist);
		
		public unsafe static IntPtr CreateContext(IntPtr device, int[] attriblist) {
			fixed (int* attriblist_ptr = attriblist)
				return CreateContext(device, attriblist_ptr);
		}

		[DllImport(Lib, EntryPoint = "alcMakeContextCurrent", ExactSpelling = true, CallingConvention = Style), SuppressUnmanagedCodeSecurity]
		public static extern bool MakeContextCurrent(IntPtr context);
		
		[DllImport(Lib, EntryPoint = "alcProcessContext", ExactSpelling = true, CallingConvention = Style), SuppressUnmanagedCodeSecurity]
		public static extern void ProcessContext(IntPtr context);
		
		[DllImport(Lib, EntryPoint = "alcSuspendContext", ExactSpelling = true, CallingConvention = Style), SuppressUnmanagedCodeSecurity]
		public static extern void SuspendContext(IntPtr context);
		
		[DllImport(Lib, EntryPoint = "alcDestroyContext", ExactSpelling = true, CallingConvention = Style), SuppressUnmanagedCodeSecurity]
		public static extern void DestroyContext(IntPtr context);
		
		[DllImport(Lib, EntryPoint = "alcGetCurrentContext", ExactSpelling = true, CallingConvention = Style), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr GetCurrentContext();
		
		[DllImport(Lib, EntryPoint = "alcGetContextsDevice", ExactSpelling = true, CallingConvention = Style), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr GetContextsDevice(IntPtr context);

		[DllImport(Lib, EntryPoint = "alcOpenDevice", ExactSpelling = true, CallingConvention = Style, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr OpenDevice([In] string devicename);
		
		[DllImport(Lib, EntryPoint = "alcCloseDevice", ExactSpelling = true, CallingConvention = Style), SuppressUnmanagedCodeSecurity]
		public static extern bool CloseDevice([In] IntPtr device);

		[DllImport(Lib, EntryPoint = "alcGetError", ExactSpelling = true, CallingConvention = Style), SuppressUnmanagedCodeSecurity]
		public static extern AlcError GetError([In] IntPtr device);

		[DllImport(Lib, EntryPoint = "alcIsExtensionPresent", ExactSpelling = true, CallingConvention = Style, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity]
		public static extern bool IsExtensionPresent([In] IntPtr device, [In] string extname);
		
		[DllImport(Lib, EntryPoint = "alcGetProcAddress", ExactSpelling = true, CallingConvention = Style, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity]
		public static extern IntPtr GetProcAddress([In] IntPtr device, [In] string funcname);
		
		[DllImport(Lib, EntryPoint = "alcGetEnumValue", ExactSpelling = true, CallingConvention = Style, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity]
		public static extern int GetEnumValue([In] IntPtr device, [In] string enumname);

		[DllImport(Lib, EntryPoint = "alcGetString", ExactSpelling = true, CallingConvention = Style, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity]
		private static extern IntPtr GetStringPrivate([In] IntPtr device, AlcGetString param);
		
		public static string GetString(IntPtr device, AlcGetString param) {
			return Marshal.PtrToStringAnsi(GetStringPrivate(device, param));
		}

		public static IList<string> GetString(IntPtr device, AlcGetStringList param) {
			List<string> result = new List<string>();
			IntPtr t = GetStringPrivate(IntPtr.Zero, (AlcGetString)param);
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			int offset = 0;
			do {
				byte b = Marshal.ReadByte(t, offset++);
				if (b != 0)
					sb.Append((char)b);
				if (b == 0)
				{
					result.Add(sb.ToString());
					if (Marshal.ReadByte(t, offset) == 0) // offset already properly increased through ++
						break; // 2x null
					else
						sb.Remove(0, sb.Length); // 1x null
				}
			} while (true);

			return result;
		}

		[DllImport(Lib, EntryPoint = "alcGetIntegerv", ExactSpelling = true, CallingConvention = Style, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity]
		public unsafe static extern void GetInteger(IntPtr device, AlcGetInteger param, int size, int* data);
		
		public unsafe static void GetInteger(IntPtr device, AlcGetInteger param, int size, out int data) {
			fixed (int* data_ptr = &data)
				GetInteger(device, param, size, data_ptr);
		}

		public unsafe static void GetInteger(IntPtr device, AlcGetInteger param, int size, int[] data) {
			fixed (int* data_ptr = data)
				GetInteger(device, param, size, data_ptr);
		}
	}
}