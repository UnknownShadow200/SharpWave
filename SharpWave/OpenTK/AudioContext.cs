//
// The Open Toolkit Library License
//
// Copyright (c) 2006 - 2009 the Open Toolkit library.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using OpenTK.Audio.OpenAL;

namespace OpenTK.Audio {
	
	public sealed class AudioContext : IDisposable {
		
		bool disposed;
		bool is_processing;
		IntPtr device_handle;
		IntPtr context_handle;
		bool context_exists;

		string device_name;
		static object audio_context_lock = new object();
		static Dictionary<IntPtr, AudioContext> available_contexts = new Dictionary<IntPtr, AudioContext>();
		
		static AudioContext() {
			if (AudioDeviceEnumerator.IsOpenALSupported) // forces enumeration
			{ }
		}
		
		public AudioContext() :
			this(null, 0, 0, false, true, MaxAuxiliarySends.UseDriverDefault) { }
		
		public AudioContext(string device) :
			this(device, 0, 0, false, true, MaxAuxiliarySends.UseDriverDefault) { }
		
		public AudioContext(string device, int freq) :
			this(device, freq, 0, false, true, MaxAuxiliarySends.UseDriverDefault) { }
		
		public AudioContext(string device, int freq, int refresh)
			: this(device, freq, refresh, false, true, MaxAuxiliarySends.UseDriverDefault) { }
		
		public AudioContext(string device, int freq, int refresh, bool sync)
			: this(AudioDeviceEnumerator.AvailablePlaybackDevices[0], freq, refresh, sync, true) { }
		
		public AudioContext(string device, int freq, int refresh, bool sync, bool enableEfx)
		{
			CreateContext(device, freq, refresh, sync, enableEfx, MaxAuxiliarySends.UseDriverDefault);
		}

		public AudioContext(string device, int freq, int refresh, bool sync, bool enableEfx, MaxAuxiliarySends efxMaxAuxSends)
		{
			CreateContext(device, freq, refresh, sync, enableEfx, efxMaxAuxSends);
		}
		
		public enum MaxAuxiliarySends:int
		{
			UseDriverDefault = 0,
			One = 1,
			Two = 2,
			Three = 3,
			Four = 4,
		}

		void CreateContext(string device, int freq, int refresh, bool sync, bool enableEfx, MaxAuxiliarySends efxAuxiliarySends)
		{
			if (!AudioDeviceEnumerator.IsOpenALSupported)
				throw new DllNotFoundException("openal32.dll");

			if (AudioDeviceEnumerator.Version == AudioDeviceEnumerator.AlcVersion.Alc1_1 && AudioDeviceEnumerator.AvailablePlaybackDevices.Count == 0)    // Alc 1.0 does not support device enumeration.
				throw new NotSupportedException("No audio hardware is available.");
			if (context_exists) throw new NotSupportedException("Multiple AudioContexts are not supported.");
			if (freq < 0) throw new ArgumentOutOfRangeException("freq", freq, "Should be greater than zero.");
			if (refresh < 0) throw new ArgumentOutOfRangeException("refresh", refresh, "Should be greater than zero.");


			if (!String.IsNullOrEmpty(device))
			{
				device_name = device;
				device_handle = Alc.OpenDevice(device); // try to open device by name
			}
			if (device_handle == IntPtr.Zero)
			{
				device_name = "IntPtr.Zero (null string)";
				device_handle = Alc.OpenDevice(null); // try to open unnamed default device
			}
			if (device_handle == IntPtr.Zero)
			{
				device_name = AudioContext.DefaultDevice;
				device_handle = Alc.OpenDevice(AudioContext.DefaultDevice); // try to open named default device
			}
			if (device_handle == IntPtr.Zero)
			{
				device_name = "None";
				throw new AudioDeviceException(String.Format("Audio device '{0}' does not exist or is tied up by another application.",
				                                             String.IsNullOrEmpty(device) ? "default" : device));
			}

			CheckErrors();

			// Build the attribute list
			List<int> attributes = new List<int>();

			if (freq != 0)
			{
				attributes.Add((int)AlcContextAttributes.Frequency);
				attributes.Add(freq);
			}

			if (refresh != 0)
			{
				attributes.Add((int)AlcContextAttributes.Refresh);
				attributes.Add(refresh);
			}

			attributes.Add((int)AlcContextAttributes.Sync);
			attributes.Add(sync ? 1 : 0);

			if (enableEfx && Alc.IsExtensionPresent(device_handle, "ALC_EXT_EFX"))
			{
				int num_slots;
				switch (efxAuxiliarySends)
				{
					case MaxAuxiliarySends.One:
					case MaxAuxiliarySends.Two:
					case MaxAuxiliarySends.Three:
					case MaxAuxiliarySends.Four:
						num_slots = (int)efxAuxiliarySends;
						break;
					default:
					case MaxAuxiliarySends.UseDriverDefault:
						Alc.GetInteger(device_handle, AlcGetInteger.EfxMaxAuxiliarySends, 1, out num_slots);
						break;
				}
				
				attributes.Add((int)AlcContextAttributes.EfxMaxAuxiliarySends);
				attributes.Add(num_slots);
			}
			attributes.Add(0);

			context_handle = Alc.CreateContext(device_handle, attributes.ToArray());

			if (context_handle == IntPtr.Zero)
			{
				Alc.CloseDevice(device_handle);
				throw new AudioContextException("The audio context could not be created with the specified parameters.");
			}

			CheckErrors();

			// HACK: OpenAL SI on Linux/ALSA crashes on MakeCurrent. This hack avoids calling MakeCurrent when
			// an old OpenAL version is detect - it may affect outdated OpenAL versions different than OpenAL SI,
			// but it looks like a good compromise for now.
			if (AudioDeviceEnumerator.AvailablePlaybackDevices.Count > 0)
				MakeCurrent();

			CheckErrors();

			device_name = Alc.GetString(device_handle, AlcGetString.DeviceSpecifier);
			

			lock (audio_context_lock)
			{
				available_contexts.Add(this.context_handle, this);
				context_exists = true;
			}
		}

		static void MakeCurrent(AudioContext context)
		{
			lock (audio_context_lock)
			{
				if (!Alc.MakeContextCurrent(context != null ? context.context_handle : IntPtr.Zero))
					throw new AudioContextException(String.Format("ALC {0} error detected at {1}.",
					                                              Alc.GetError(context != null ? (IntPtr)context.context_handle : IntPtr.Zero).ToString(),
					                                              context != null ? context.ToString() : "null"));
			}
		}
		
		internal bool IsCurrent
		{
			get
			{
				lock (audio_context_lock)
				{
					if (available_contexts.Count == 0)
						return false;
					else
					{
						return AudioContext.CurrentContext == this;
					}
				}
			}
			set
			{
				if (value) AudioContext.MakeCurrent(this);
				else AudioContext.MakeCurrent(null);
			}
		}
		
		public void CheckErrors()
		{
			if (disposed)
				throw new ObjectDisposedException(this.GetType().FullName);

			new AudioDeviceErrorChecker(device_handle).Dispose();
		}
		
		public AlcError CurrentError
		{
			get
			{
				if (disposed)
					throw new ObjectDisposedException(this.GetType().FullName);

				return Alc.GetError(device_handle);
			}
		}
		
		public void MakeCurrent()
		{
			if (disposed)
				throw new ObjectDisposedException(this.GetType().FullName);

			AudioContext.MakeCurrent(this);
		}

		public bool IsProcessing
		{
			get
			{
				CheckDisposed();
				return is_processing;
			}
			private set { is_processing = value; }
		}
		
		public void Process()
		{
			CheckDisposed();
			Alc.ProcessContext(this.context_handle);
			IsProcessing = true;
		}
		
		public void Suspend()
		{
			CheckDisposed();
			Alc.SuspendContext(this.context_handle);
			IsProcessing = false;
		}
		
		public bool SupportsExtension(string extension)
		{
			CheckDisposed();
			return Alc.IsExtensionPresent(device_handle, extension);
		}
		
		public string CurrentDevice
		{
			get
			{
				CheckDisposed();
				return device_name;
			}
		}
		
		public static AudioContext CurrentContext
		{
			get
			{
				lock (audio_context_lock)
				{
					if (available_contexts.Count == 0)
						return null;
					else
					{
						AudioContext context;
						AudioContext.available_contexts.TryGetValue(
							(IntPtr)Alc.GetCurrentContext(),
							out context);
						return context;
					}
				}
			}
		}
		
		public static IList<string> AvailableDevices {
			get { return AudioDeviceEnumerator.AvailablePlaybackDevices; }
		}
		
		public static string DefaultDevice {
			get { return AudioDeviceEnumerator.DefaultPlaybackDevice; }
		}
		
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		
		void CheckDisposed() {
			if (disposed)
					throw new ObjectDisposedException(this.GetType().FullName);
		}

		void Dispose(bool manual)
		{
			if (!disposed)
			{
				if (this.IsCurrent)
					this.IsCurrent = false;

				if (context_handle != IntPtr.Zero)
				{
					available_contexts.Remove(context_handle);
					Alc.DestroyContext(context_handle);
				}

				if (device_handle != IntPtr.Zero)
					Alc.CloseDevice(device_handle);
				
				disposed = true;
			}
		}

		~AudioContext() {
			this.Dispose(false);
		}

		public override string ToString() {
			return String.Format("{0} (handle: {1}, device: {2})",
			                     this.device_name, this.context_handle, this.device_handle);
		}
	}
}
