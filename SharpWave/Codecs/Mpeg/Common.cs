﻿using System;

namespace SharpWave.Codecs.Mpeg {
	
	public static class Common {
		
		public static int GetNumberOfChannels( ChannelMode mode ) {
			return mode == ChannelMode.SingleChannel ? 1 : 2;
		}
		
		#region Layer I & II
		
		public static double[] SynthesisSubbandFilter( int ch, double[,] bandTbl, double[] V, int subbands ) {
			double[] output = new double[32];
			// Shift V up
			for( int i = 1023; i >= 64; i-- ) {
				V[i] = V[i - 64];
			}
			
			// Matrixing
			for( int i = 0; i < 64; i++ ) {
				double sum = 0;
				for( int k = 0; k < 32; k++ ) {
					sum += bandTbl[ch, k] * filter( i, k );
				}
				V[i] = sum;
			}
			// Build 512 values vector U
			double[] U = new double[512];
			for( int i = 0; i < 8; i++ ) {
				for( int j = 0; j < 32; j++ ) {
					U[i * 64 + j] = V[i * 128 + j];
					U[i * 64 + j + 32] = V[i * 128 + 96 + j];
				}
			}
			// Window by 512 coefficients
			for( int i = 0; i < 512; i++ ) {
				U[i] *= LayerI_II_deWindow[i];
			}
			// And finally output the normalised samples in [-1, 1] range.
			for( int sb = 0; sb < subbands; sb++ ) {
				double sample = 0;
				for( int i = 0; i < 16; i++ ) {
					sample += U[sb + i * 32];
				}
				output[sb] = sample;
			}
			return output;
		}
		
		static double filter( int i, int k ) {
			return Math.Cos( ( 16 + i ) * ( 2 * k + 1 ) * Math.PI / 64 );
		}
		
		// 2^(1-index/3)
		public static double[] LayerI_II_ScaleFactors = new double[] {
			2.00000000000000, 1.58740105196820, 1.25992104989487, 1.00000000000000,
			0.79370052598410, 0.62996052494744, 0.50000000000000, 0.39685026299205,
			0.31498026247372, 0.25000000000000, 0.19842513149602, 0.15749013123686,
			0.12500000000000, 0.09921256574801, 0.07874506561843, 0.06250000000000,
			0.04960628287401, 0.03937253280921, 0.03125000000000, 0.02480314143700,
			0.01968626640461, 0.01562500000000, 0.01240157071850, 0.00984313320230,
			0.00781250000000, 0.00620078535925, 0.00492156660115, 0.00390625000000,
			0.00310039267963, 0.00246078330058, 0.00195312500000, 0.00155019633981,
			0.00123039165029, 0.00097656250000, 0.00077509816991, 0.00061519582514,
			0.00048828125000, 0.00038754908495, 0.00030759791257, 0.00024414062500,
			0.00019377454248, 0.00015379895629, 0.00012207031250, 0.00009688727124,
			0.00007689947814, 0.00006103515625, 0.00004844363562, 0.00003844973907,
			0.00003051757813, 0.00002422181781, 0.00001922486954, 0.00001525878906,
			0.00001211090890, 0.00000961243477, 0.00000762939453, 0.00000605545445,
			0.00000480621738, 0.00000381469727, 0.00000302772723, 0.00000240310869,
			0.00000190734863, 0.00000151386361, 0.00000120155435, 1E-20,
		};
		
		public static double[] LayerI_II_deWindow = new double[] {
			0.000000000, -0.000015259, -0.000015259, -0.000015259,
			-0.000015259, -0.000015259, -0.000015259, -0.000030518,
			-0.000030518, -0.000030518, -0.000030518, -0.000045776,
			-0.000045776, -0.000061035, -0.000061035, -0.000076294,
			-0.000076294, -0.000091553, -0.000106812, -0.000106812,
			-0.000122070, -0.000137329, -0.000152588, -0.000167847,
			-0.000198364, -0.000213623, -0.000244141, -0.000259399,
			-0.000289917, -0.000320435, -0.000366211, -0.000396729,
			-0.000442505, -0.000473022, -0.000534058, -0.000579834,
			-0.000625610, -0.000686646, -0.000747681, -0.000808716,
			-0.000885010, -0.000961304, -0.001037598, -0.001113892,
			-0.001205444, -0.001296997, -0.001388550, -0.001480103,
			-0.001586914, -0.001693726, -0.001785278, -0.001907349,
			-0.002014160, -0.002120972, -0.002243042, -0.002349854,
			-0.002456665, -0.002578735, -0.002685547, -0.002792358,
			-0.002899170, -0.002990723, -0.003082275, -0.003173828,
			0.003250122, 0.003326416, 0.003387451, 0.003433228,
			0.003463745, 0.003479004, 0.003479004, 0.003463745,
			0.003417969, 0.003372192, 0.003280640, 0.003173828,
			0.003051758, 0.002883911, 0.002700806, 0.002487183,
			0.002227783, 0.001937866, 0.001617432, 0.001266479,
			0.000869751, 0.000442505, -0.000030518, -0.000549316,
			-0.001098633, -0.001693726, -0.002334595, -0.003005981,
			-0.003723145, -0.004486084, -0.005294800, -0.006118774,
			-0.007003784, -0.007919312, -0.008865356, -0.009841919,
			-0.010848999, -0.011886597, -0.012939453, -0.014022827,
			-0.015121460, -0.016235352, -0.017349243, -0.018463135,
			-0.019577026, -0.020690918, -0.021789551, -0.022857666,
			-0.023910522, -0.024932861, -0.025909424, -0.026840210,
			-0.027725220, -0.028533936, -0.029281616, -0.029937744,
			-0.030532837, -0.031005859, -0.031387329, -0.031661987,
			-0.031814575, -0.031845093, -0.031738281, -0.031478882,
			0.031082153, 0.030517578, 0.029785156, 0.028884888,
			0.027801514, 0.026535034, 0.025085449, 0.023422241,
			0.021575928, 0.019531250, 0.017257690, 0.014801025,
			0.012115479, 0.009231567, 0.006134033, 0.002822876,
			-0.000686646, -0.004394531, -0.008316040, -0.012420654,
			-0.016708374, -0.021179199, -0.025817871, -0.030609131,
			-0.035552979, -0.040634155, -0.045837402, -0.051132202,
			-0.056533813, -0.061996460, -0.067520142, -0.073059082,
			-0.078628540, -0.084182739, -0.089706421, -0.095169067,
			-0.100540161, -0.105819702, -0.110946655, -0.115921021,
			-0.120697021, -0.125259399, -0.129562378, -0.133590698,
			-0.137298584, -0.140670776, -0.143676758, -0.146255493,
			-0.148422241, -0.150115967, -0.151306152, -0.151962280,
			-0.152069092, -0.151596069, -0.150497437, -0.148773193,
			-0.146362305, -0.143264771, -0.139450073, -0.134887695,
			-0.129577637, -0.123474121, -0.116577148, -0.108856201,
			0.100311279, 0.090927124, 0.080688477, 0.069595337,
			0.057617187, 0.044784546, 0.031082153, 0.016510010,
			0.001068115, -0.015228271, -0.032379150, -0.050354004,
			-0.069168091, -0.088775635, -0.109161377, -0.130310059,
			-0.152206421, -0.174789429, -0.198059082, -0.221984863,
			-0.246505737, -0.271591187, -0.297210693, -0.323318481,
			-0.349868774, -0.376800537, -0.404083252, -0.431655884,
			-0.459472656, -0.487472534, -0.515609741, -0.543823242,
			-0.572036743, -0.600219727, -0.628295898, -0.656219482,
			-0.683914185, -0.711318970, -0.738372803, -0.765029907,
			-0.791213989, -0.816864014, -0.841949463, -0.866363525,
			-0.890090942, -0.913055420, -0.935195923, -0.956481934,
			-0.976852417, -0.996246338, -1.014617920, -1.031936646,
			-1.048156738, -1.063217163, -1.077117920, -1.089782715,
			-1.101211548, -1.111373901, -1.120223999, -1.127746582,
			-1.133926392, -1.138763428, -1.142211914, -1.144287109,
			1.144989014, 1.144287109, 1.142211914, 1.138763428,
			1.133926392, 1.127746582, 1.120223999, 1.111373901,
			1.101211548, 1.089782715, 1.077117920, 1.063217163,
			1.048156738, 1.031936646, 1.014617920, 0.996246338,
			0.976852417, 0.956481934, 0.935195923, 0.913055420,
			0.890090942, 0.866363525, 0.841949463, 0.816864014,
			0.791213989, 0.765029907, 0.738372803, 0.711318970,
			0.683914185, 0.656219482, 0.628295898, 0.600219727,
			0.572036743, 0.543823242, 0.515609741, 0.487472534,
			0.459472656, 0.431655884, 0.404083252, 0.376800537,
			0.349868774, 0.323318481, 0.297210693, 0.271591187,
			0.246505737, 0.221984863, 0.198059082, 0.174789429,
			0.152206421, 0.130310059, 0.109161377, 0.088775635,
			0.069168091, 0.050354004, 0.032379150, 0.015228271,
			-0.001068115, -0.016510010, -0.031082153, -0.044784546,
			-0.057617187, -0.069595337, -0.080688477, -0.090927124,
			0.100311279, 0.108856201, 0.116577148, 0.123474121,
			0.129577637, 0.134887695, 0.139450073, 0.143264771,
			0.146362305, 0.148773193, 0.150497437, 0.151596069,
			0.152069092, 0.151962280, 0.151306152, 0.150115967,
			0.148422241, 0.146255493, 0.143676758, 0.140670776,
			0.137298584, 0.133590698, 0.129562378, 0.125259399,
			0.120697021, 0.115921021, 0.110946655, 0.105819702,
			0.100540161, 0.095169067, 0.089706421, 0.084182739,
			0.078628540, 0.073059082, 0.067520142, 0.061996460,
			0.056533813, 0.051132202, 0.045837402, 0.040634155,
			0.035552979, 0.030609131, 0.025817871, 0.021179199,
			0.016708374, 0.012420654, 0.008316040, 0.004394531,
			0.000686646, -0.002822876, -0.006134033, -0.009231567,
			-0.012115479, -0.014801025, -0.017257690, -0.019531250,
			-0.021575928, -0.023422241, -0.025085449, -0.026535034,
			-0.027801514, -0.028884888, -0.029785156, -0.030517578,
			0.031082153, 0.031478882, 0.031738281, 0.031845093,
			0.031814575, 0.031661987, 0.031387329, 0.031005859,
			0.030532837, 0.029937744, 0.029281616, 0.028533936,
			0.027725220, 0.026840210, 0.025909424, 0.024932861,
			0.023910522, 0.022857666, 0.021789551, 0.020690918,
			0.019577026, 0.018463135, 0.017349243, 0.016235352,
			0.015121460, 0.014022827, 0.012939453, 0.011886597,
			0.010848999, 0.009841919, 0.008865356, 0.007919312,
			0.007003784, 0.006118774, 0.005294800, 0.004486084,
			0.003723145, 0.003005981, 0.002334595, 0.001693726,
			0.001098633, 0.000549316, 0.000030518, -0.000442505,
			-0.000869751, -0.001266479, -0.001617432, -0.001937866,
			-0.002227783, -0.002487183, -0.002700806, -0.002883911,
			-0.003051758, -0.003173828, -0.003280640, -0.003372192,
			-0.003417969, -0.003463745, -0.003479004, -0.003479004,
			-0.003463745, -0.003433228, -0.003387451, -0.003326416,
			0.003250122, 0.003173828, 0.003082275, 0.002990723,
			0.002899170, 0.002792358, 0.002685547, 0.002578735,
			0.002456665, 0.002349854, 0.002243042, 0.002120972,
			0.002014160, 0.001907349, 0.001785278, 0.001693726,
			0.001586914, 0.001480103, 0.001388550, 0.001296997,
			0.001205444, 0.001113892, 0.001037598, 0.000961304,
			0.000885010, 0.000808716, 0.000747681, 0.000686646,
			0.000625610, 0.000579834, 0.000534058, 0.000473022,
			0.000442505, 0.000396729, 0.000366211, 0.000320435,
			0.000289917, 0.000259399, 0.000244141, 0.000213623,
			0.000198364, 0.000167847, 0.000152588, 0.000137329,
			0.000122070, 0.000106812, 0.000106812, 0.000091553,
			0.000076294, 0.000076294, 0.000061035, 0.000061035,
			0.000045776, 0.000045776, 0.000030518, 0.000030518,
			0.000030518, 0.000030518, 0.000015259, 0.000015259,
			0.000015259, 0.000015259, 0.000015259, 0.000015259,
		};
		
		#endregion
		
		public static void OutputSample( double sample, byte[] pcm, ref int index ) {
			// TODO: do we really need to clamp?
			if( sample < -1 ) sample = -1;
			if( sample > 1 ) sample = 1;
			ushort pcmSample = (ushort)( sample * 32767.0 );
			pcm[index++] = (byte)( pcmSample );
			pcm[index++] = (byte)( pcmSample >> 8 );
		}
	}
}