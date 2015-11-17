﻿//http://www.sno.phy.queensu.ca/~phil/exiftool/TagNames/RIFF.html
using System;

namespace SharpWave.Codecs.Wave {
	
	public enum AudioFormat : ushort {
		Unknown = 0x0000, // Microsoft Corporation
		Pcm = 0x0001, // Microsoft Corporation
		Adpcm = 0x0002, // Microsoft Corporation
		IeeeFloat = 0x0003, // Microsoft Corporation
		Vselp = 0x0004, // Compaq Computer Corp.
		IbmCvsd = 0x0005, // IBM Corporation
		ALaw = 0x0006, // Microsoft Corporation
		MuLaw = 0x0007, // Microsoft Corporation
		Dts = 0x0008, // Microsoft Corporation
		OkiAdpcm = 0x0010, // OKI
		DviAdpcm = 0x0011, // Intel Corporation
		ImaAdpcm = DviAdpcm, //  Intel Corporation
		MediaspaceAdpcm = 0x0012, // Videologic
		SierraAdpcm = 0x0013, // Sierra Semiconductor Corp
		G723Adpcm = 0x0014, // Antex Electronics Corporation
		DigiStd = 0x0015, // DSP Solutions, Inc.
		DigiFix = 0x0016, // DSP Solutions, Inc.
		DialogicOkiAdpcm = 0x0017, // Dialogic Corporation
		MediavisionAdpcm = 0x0018, // Media Vision, Inc.
		CuCodec = 0x0019, // Hewlett-Packard Company
		YamahaAdpcm = 0x0020, // Yamaha Corporation of America
		Sonarc = 0x0021, // Speech Compression
		DspgroupTruespeech = 0x0022, // DSP Group, Inc
		EchoSc1 = 0x0023, // Echo Speech Corporation
		AudiofileAf36 = 0x0024, // Virtual Music, Inc.
		Aptx = 0x0025, // Audio Processing Technology
		AudiofileAf10 = 0x0026, // Virtual Music, Inc.
		Prosody1612 = 0x0027, // Aculab plc
		Lrc = 0x0028, // Merging Technologies S.A.
		DolbyAc2 = 0x0030, // Dolby Laboratories
		Gsm610 = 0x0031, // Microsoft Corporation
		MsnAudio = 0x0032, // Microsoft Corporation
		AntexAdpcme = 0x0033, // Antex Electronics Corporation
		ControlResVqlpc = 0x0034, // Control Resources Limited
		DigiReal = 0x0035, // DSP Solutions, Inc.
		DigiAdpcm = 0x0036, // DSP Solutions,dolb Inc.
		ControlResCr10 = 0x0037, // Control Resources Limited
		NmsVbxAdpcm = 0x0038, // Natural MicroSystems
		CsImaAdpcm = 0x0039, // Crystal Semiconductor IMA ADPCM
		EchoSc3 = 0x003A, // Echo Speech Corporation
		RockwellAdpcm = 0x003B, // Rockwell International
		RockwellDigitalk = 0x003C, // Rockwell International
		Xebec = 0x003D, // Xebec Multimedia Solutions Limited
		G721Adpcm = 0x0040, // Antex Electronics Corporation
		G728Celp = 0x0041, // Antex Electronics Corporation
		Msg723 = 0x0042, // Microsoft Corporation
		Mpeg = 0x0050, // Microsoft Corporation
		Rt24 = 0x0052, // InSoft, Inc.
		Pac = 0x0053, // InSoft, Inc.
		MpegLayer3 = 0x0055, // ISO/MPEG Layer3 Format Tag
		LucentG723 = 0x0059, // Lucent Technologies
		Cirrus = 0x0060, // Cirrus Logic
		Espcm = 0x0061, // ESS Technology
		Voxware = 0x0062, // Voxware Inc
		CanopusAtrac = 0x0063, // Canopus, co., Ltd.
		G726Adpcm = 0x0064, // APICOM
		G722Adpcm = 0x0065, // APICOM
		DsatDisplay = 0x0067, // Microsoft Corporation
		VoxwareByteAligned = 0x0069, // Voxware Inc
		VoxwareAc8 = 0x0070, // Voxware Inc
		VoxwareAc10 = 0x0071, // Voxware Inc
		VoxwareAc16 = 0x0072, // Voxware Inc
		VoxwareAc20 = 0x0073, // Voxware Inc
		VoxwareRt24 = 0x0074, // Voxware Inc
		VoxwareRt29 = 0x0075, // Voxware Inc
		VoxwareRt29Hw = 0x0076, // Voxware Inc
		VoxwareVr12 = 0x0077, // Voxware Inc
		VoxwareVr18 = 0x0078, // Voxware Inc
		VoxwareTq40 = 0x0079, // Voxware Inc
		Softsound = 0x0080, // Softsound, Ltd.
		VoxwareTq60 = 0x0081, // Voxware Inc
		MsRt24 = 0x0082, // Microsoft Corporation
		G729a = 0x0083, // AT&T Labs, Inc.
		MviMvi2 = 0x0084, // Motion Pixels
		DfG726 = 0x0085, // DataFusion Systems (Pty) (Ltd)
		DfGsm610 = 0x0086, // DataFusion Systems (Pty) (Ltd)
		IsiAudio = 0x0088, // Iterated Systems, Inc.
		OnLive = 0x0089, // OnLive! Technologies, Inc.
		Sbc24 = 0x0091, // Siemens Business Communications Sys
		DolbyAc3Spdif = 0x0092, // Sonic Foundry
		MediasonicG723 = 0x0093, // MediaSonic
		Prosody8kbps = 0x0094, // Aculab plc
		ZyxelAdpcm = 0x0097, // ZyXEL Communications, Inc.
		PhilipsLpcbb = 0x0098, // Philips Speech Processing
		Packed = 0x0099, // Studer Professional Audio AG
		MaldenPhonytalk = 0x00A0, // Malden Electronics Ltd.
		RhetorexAdpcm = 0x0100, // Rhetorex Inc.
		Irat = 0x0101, // BeCubed Software Inc.
		VivoG723 = 0x0111, // Vivo Software
		VivoSiren = 0x0112, // Vivo Software
		DigitalG723 = 0x0123, // Digital Equipment Corporation
		SanyoLdAdpcm = 0x0125, // Sanyo Electric Co., Ltd.
		SiproLabAcePlnet = 0x0130, // Sipro Lab Telecom Inc.
		SiproLabAceLp4800 = 0x0131, // Sipro Lab Telecom Inc.
		SiproLabAceLp8v3 = 0x0132, // Sipro Lab Telecom Inc.
		SiproLabG729 = 0x0133, // Sipro Lab Telecom Inc.
		SiproLabG729a = 0x0134, // Sipro Lab Telecom Inc.
		SiproLabKelvin = 0x0135, // Sipro Lab Telecom Inc.
		DictaphoneG726Adpcm = 0x0140, // Dictaphone Corporation
		QualcommPureVoice = 0x0150, // Qualcomm, Inc.
		QualcommHalfRate = 0x0151, // Qualcomm, Inc.
		Tubgsm = 0x0155, // Ring Zero Systems, Inc.
		MsAudio1 = 0x0160, // Microsoft Corporation
		CreativeAdpcm = 0x0200, // Creative Labs, Inc
		CreativeFastSpeech8 = 0x0202, // Creative Labs, Inc
		CreativeFastSpeech10 = 0x0203, // Creative Labs, Inc
		UherAdpcm = 0x0210, // UHER informatic GmbH
		Quarterdeck = 0x0220, // Quarterdeck Corporation
		IlinkVc = 0x0230, // I-link Worldwide
		RawSport = 0x0240, // Aureal Semiconductor
		IpiHsx = 0x0250, // Interactive Products, Inc.
		IpiRpelp = 0x0251, // Interactive Products, Inc.
		Cs2 = 0x0260, // Consistent Software
		SonyScx = 0x0270, // Sony Corp.
		FmTownsSnd = 0x0300, // Fujitsu Corp.
		BtvDigital = 0x0400, // Brooktree Corporation
		QdesignMusic = 0x0450, // QDesign Corporation
		VmeVmpcm = 0x0680, // AT&T Labs, Inc.
		Tpc = 0x0681, // AT&T Labs, Inc.
		OliGsm = 0x1000, // Ing C. Olivetti & C., S.p.A.
		OliAdpcm = 0x1001, // Ing C. Olivetti & C., S.p.A.
		OliCelp = 0x1002, // Ing C. Olivetti & C., S.p.A.
		OliSbc = 0x1003, // Ing C. Olivetti & C., S.p.A.
		OliOpr = 0x1004, // Ing C. Olivetti & C., S.p.A.
		LhCodec = 0x1100, // Lernout & Hauspie
		Norris = 0x1400, // Norris Communications, Inc.
		SoundspaceMusicompress = 0x1500, // AT&T Labs, Inc.
		Dvm = 0x2000, // FAST Multimedia AG
		
		Extensible = 0xFFFE,
		Development = 0xFFFF,
	}
}