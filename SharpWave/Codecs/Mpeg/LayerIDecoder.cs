//http://scratchpad.wikia.com/wiki/MPEG-1_Audio_Layers_I_and_II
//http://keyj.emphy.de/kjmp2/
//http://www.jonolick.com/code.html
using System;

namespace SharpWave.Codecs.Mpeg {

	public sealed class LayerIDecoder : LayerDecoder {
		
		const int subbands = 32;
		const int samplesPerSubband = 12;
		const int totalSamples = subbands * samplesPerSubband;
		
		int channels;
		BitReader reader;
		
		void ReadBitAllocation( byte[,] allocation ) {
			for( int sb = 0; sb < subbands; sb++ ) {
				for( int ch = 0; ch < channels; ch++ ) {
					allocation[sb, ch] = (byte)reader.ReadBits( 4 );
				}
			}
		}
		
		void ReadScaleFactors( byte[,] allocation, byte[,] scaleIndices ) {
			for( int sb = 0; sb < subbands; sb++ ) {
				for( int ch = 0; ch < channels; ch++ ) {
					if( allocation[sb, ch] != 0 ) {
						scaleIndices[sb, ch] = (byte)reader.ReadBits( 6 );
					}
				}
			}
		}
		
		void ReadSamples( byte[,] allocation, int[,,] samples ) {
			for( int s = 0; s < samplesPerSubband; s++ ) {
				for( int sb = 0; sb < subbands; sb++ ) {
					for( int ch = 0; ch < channels; ch++ ) {
						int bits = allocation[sb, ch];
						if( bits != 0 ) {
							samples[s, sb, ch] = reader.ReadBits( bits + 1 );
						}
					}
				}
			}
		}
		
		void RequantiseSamples( int s, double[,] bandTbl, byte[,] allocation, int[,,] samples, byte[,] scaleIndices ) {
			for( int sb = 0; sb < subbands; sb++ ) {
				for( int ch = 0; ch < channels; ch++ ) {
					int bits = allocation[sb, ch];
					if( bits != 0 ) {
						// Requantise the sample
						int sample = samples[s, sb, ch];
						double adder = ( sample >> bits ) != 0 ? 0 : -1; // test if sign bit is unset (negative)
						sample &= ( 1 << bits ) - 1; // zero the sign bit
						double fractionalisedNum = sample / (double)( 1 << bits ) + adder;
						
						bits++;
						double value = fractionalisedNum + Math.Pow( 2, 1 - bits ); // TODO: ( 1 / ( 1 << ( bits - 1 )
						value *= ( 1 << bits ) / ( ( 1 << bits ) - 1.0 ); // 2^bits / 2^bits - 1
						value *= Common.LayerI_II_ScaleFactors[scaleIndices[sb, ch]]; // rescale
						bandTbl[ch, sb] = value;
					}
				}
			}
		}
		
		double[] V0 = new double[1024], V1 = new double[1024];	
		public override byte[] Decode( MpegFrame frame, BitReader reader ) {
			byte[,] allocation = new byte[subbands, 2];
			byte[,] scaleIndices = new byte[subbands, 2];
			channels = frame.Channels;
			int[,,] samples = new int[samplesPerSubband, subbands, channels];
			byte[] output = new byte[totalSamples * channels * 2];
			this.reader = reader;
			int index = 0;
			
			if( frame.ChannelMode == ChannelMode.SingleChannel || frame.ChannelMode == ChannelMode.Stereo
			   || frame.ChannelMode == ChannelMode.DualChannel ) {
				
				ReadBitAllocation( allocation );
				ReadScaleFactors( allocation, scaleIndices );
				ReadSamples( allocation, samples );
				
				for( int s = 0; s < samplesPerSubband; s++ ) {
					double[,] bandTbl = new double[channels, subbands];
					RequantiseSamples( s, bandTbl, allocation, samples, scaleIndices );
					
					if( channels == 1 ) {
						double[] samples0 = Common.SynthesisSubbandFilter( 0, bandTbl, V0, subbands );						
						for( int sb = 0; sb < subbands; sb++ ) {
							Common.OutputSample( samples0[sb], output, ref index );
						}
					} else {
						double[] samples0 = Common.SynthesisSubbandFilter( 0, bandTbl, V0, subbands );
						double[] samples1 = Common.SynthesisSubbandFilter( 1, bandTbl, V1, subbands );						
						for( int sb = 0; sb < subbands; sb++ ) {
							Common.OutputSample( samples0[sb], output, ref index );
							Common.OutputSample( samples1[sb], output, ref index );
						}
					}
				}
				return output;
			} else {
				throw new NotImplementedException( "joint stereo implementation not done");
			}
		}
	}
}