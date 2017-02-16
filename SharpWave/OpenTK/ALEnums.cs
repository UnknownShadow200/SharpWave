﻿#region --- OpenTK.OpenAL License ---
/* AlTokens.cs
 * C header: \OpenAL 1.1 SDK\include\Al.h
 * Spec: http://www.openal.org/openal_webstf/specs/OpenAL11Specification.pdf
 * Copyright (c) 2008 Christoph Brandtner and Stefanos Apostolopoulos
 * See license.txt for license details
 * http://www.OpenTK.net */
#endregion

using System;

namespace OpenTK.Audio.OpenAL
{
	///<summary>A list of valid 8-bit boolean Source/GetSource parameters</summary>
	public enum ALSourceb : int
	{
		///<summary>Indicate that the Source has relative coordinates. Type: bool Range: [True, False]</summary>
		SourceRelative = 0x202,

		///<summary>Indicate whether the Source is looping. Type: bool Range: [True, False] Default: False.</summary>
		Looping = 0x1007,
	}

	///<summary>A list of valid Int32 Source parameters</summary>
	public enum ALSourcei : int
	{

		///<summary>Indicate the Buffer to provide sound samples. Type: uint Range: any valid Buffer Handle.</summary>
		Buffer = 0x1009,

		///<summary>Source type (Static, Streaming or undetermined). Use enum AlSourceType for comparison</summary>
		SourceType = 0x1027,
	}

	///<summary>A list of valid Int32 GetSource parameters</summary>
	public enum ALGetSourcei : int
	{

		///<summary>Indicate the Buffer to provide sound samples. Type: uint Range: any valid Buffer Handle.</summary>
		Buffer = 0x1009,

		/// <summary>The state of the source (Stopped, Playing, etc.) Use the enum AlSourceState for comparison.</summary>
		SourceState = 0x1010,

		/// <summary>The number of buffers queued on this source.</summary>
		BuffersQueued = 0x1015,

		/// <summary>The number of buffers in the queue that have been processed.</summary>
		BuffersProcessed = 0x1016,

		///<summary>Source type (Static, Streaming or undetermined). Use enum AlSourceType for comparison.</summary>
		SourceType = 0x1027,
	}
	
	    ///<summary>A list of valid 32-bit Float Source/GetSource parameters</summary>
    public enum ALSourcef : int
    {
        ///<summary>Specify the pitch to be applied, either at Source, or on mixer results, at Listener. Range: [0.5f - 2.0f] Default: 1.0f</summary>
        Pitch = 0x1003,

        ///<summary>Indicate the gain (volume amplification) applied. Type: float. Range: [0.0f - ? ] A value of 1.0 means un-attenuated/unchanged. Each division by 2 equals an attenuation of -6dB. Each multiplicaton with 2 equals an amplification of +6dB. A value of 0.0f is meaningless with respect to a logarithmic scale; it is interpreted as zero volume - the channel is effectively disabled.</summary>
        Gain = 0x100A,

    }

	///<summary>Source state information, can be retrieved by AL.Source() with ALSourcei.SourceState.</summary>
	public enum ALSourceState : int
	{
		///<summary>Default State when loaded, can be manually set with AL.SourceRewind().</summary>
		Initial = 0x1011,

		///<summary>The source is currently playing.</summary>
		Playing = 0x1012,

		///<summary>The source has paused playback.</summary>
		Paused = 0x1013,

		///<summary>The source is not playing.</summary>
		Stopped = 0x1014,
	}

	///<summary>Source type information,  can be retrieved by AL.Source() with ALSourcei.SourceType.</summary>
	public enum ALSourceType : int
	{
		///<summary>Source is Static if a Buffer has been attached using AL.Source with the parameter Sourcei.Buffer.</summary>
		Static = 0x1028,

		///<summary>Source is Streaming if one or more Buffers have been attached using AL.SourceQueueBuffers</summary>
		Streaming = 0x1029,

		///<summary>Source is undetermined when it has a null Buffer attached</summary>
		Undetermined = 0x1030,
	}

	///<summary>Sound samples: Format specifier.</summary>
	public enum ALFormat : int
	{
		///<summary>1 Channel, 8 bits per sample.</summary>
		Mono8 = 0x1100,

		///<summary>1 Channel, 16 bits per sample.</summary>
		Mono16 = 0x1101,

		///<summary>2 Channels, 8 bits per sample each.</summary>
		Stereo8 = 0x1102,

		///<summary>2 Channels, 16 bits per sample each.</summary>
		Stereo16 = 0x1103,
	}

	/// <summary>Returned by AL.GetError</summary>
	public enum ALError : int
	{
		///<summary>No OpenAL Error.</summary>
		NoError = 0,

		///<summary>Invalid Name paramater passed to OpenAL call.</summary>
		InvalidName = 0xA001,

		///<summary>Invalid parameter passed to OpenAL call.</summary>
		IllegalEnum = 0xA002,
		///<summary>Invalid parameter passed to OpenAL call.</summary>
		InvalidEnum = 0xA002,

		///<summary>Invalid OpenAL enum parameter value.</summary>
		InvalidValue = 0xA003,

		///<summary>Illegal OpenAL call.</summary>
		IllegalCommand = 0xA004,
		///<summary>Illegal OpenAL call.</summary>
		InvalidOperation = 0xA004,

		///<summary>No OpenAL memory left.</summary>
		OutOfMemory = 0xA005,
	}

	///<summary>A list of valid string AL.Get() parameters</summary>
	public enum ALGetString : int
	{
		/// <summary>Gets the Vendor name.</summary>
		Vendor = 0xB001,

		/// <summary>Gets the driver version.</summary>
		Version = 0xB002,

		/// <summary>Gets the renderer mode.</summary>
		Renderer = 0xB003,

		/// <summary>Gets a list of all available Extensions, separated with spaces.</summary>
		Extensions = 0xB004,
	}

	/// <summary>Used by AL.DistanceModel(), the distance model can be retrieved by AL.Get() with ALGetInteger.DistanceModel</summary>
	public enum ALDistanceModel : int
	{
		///<summary>Bypasses all distance attenuation calculation for all Sources.</summary>
		None = 0,
	}

}
