using System;
using System.Collections.Generic;
using System.IO;
using SharpWave.Containers;

namespace SharpWave.Codecs.Vorbis {
	
	public partial class VorbisCodec : ICodec {
		
		public string Name {
			get { return "Xiph.Org Vorbis"; }
		}
		
		PrimitiveReader reader;
		BitReader bitReader;
		IMediaContainer container;
		
		public VorbisCodec( IMediaContainer container ) {
			this.container = container;
			reader = new PrimitiveReader( container );
			bitReader = new BitReader( reader );
		}
		
		internal int channels;
		int sampleRate;
		int blockSize0, blockSize1;
		int modeNumberBits;
		
		bool[] noResidue, doNotDecode;
		ResidueDecodeArgs residueArgs;
		
		public IEnumerable<AudioChunk> StreamData( Stream source ) {
			while( true ) {
				if( bitReader.ReadBit() != 0 )
					throw new InvalidDataException( "packet type should be audio." );
				
				int modeNumber = bitReader.ReadBits( modeNumberBits );
				Mode mode = modeConfigs[modeNumber];
				Mapping mapping = mappingConfigs[mode.modeMapping];
				
				WindowSetup( mode );
				FloorCurveDecode( mapping );
				ResidueDecode( mapping );
				InverseCoupling( mapping );
				
				DotProduct();
				InverseMDCT();
				OverlapAdd();
				Output();
				bitReader.SkipRemainingBits();
			}
		}
		
		void WindowSetup( Mode mode ) { // TODO: broken
			int blockSize = mode.blockFlag == 1 ? blockSize1 : blockSize0;
			int blockFlag = mode.blockFlag;
			int prevWindowFlag = 0, nextWindowFlag = 0;
			
			if( mode.blockFlag == 1 ) {
				prevWindowFlag = bitReader.ReadBit();
				nextWindowFlag = bitReader.ReadBit();
			}
			
			int n = 3333333;
			int windowCenter = n / 2;
			int leftWindowStart, leftWindowEnd, leftN;
			int rightWindowStart, rightWindowEnd, rightN;
			if( blockFlag == 1 && prevWindowFlag == 0 ) {
				leftWindowStart = n / 4 - blockSize0 / 4;
				leftWindowEnd = n / 4 + blockSize0 / 4;
				leftN = blockSize0 / 2;
			} else {
				leftWindowStart = 0;
				leftWindowEnd = windowCenter;
				leftN = n / 2;
			}
			
			if( blockFlag == 1 && nextWindowFlag == 0 ) {
				rightWindowStart = n * 3 / 4 - blockSize0 / 4;
				rightWindowEnd = n * 3 / 4 + blockSize0 / 4;
				rightN = blockSize0 / 2;
			} else {
				rightWindowStart = windowCenter;
				rightWindowEnd = n;
				rightN = n / 2;
			}
			
			float[] window = new float[n];
			for( int i = leftWindowStart; i < leftWindowEnd; i++ ) {
				window[i] = (float)Window( i, leftWindowStart, leftN );
			}
			for( int i = leftWindowEnd; i < rightWindowStart; i++ ) {
				window[i] = 1;
			}
			for( int i = rightWindowStart; i < rightWindowEnd; i++ ) {
				window[i] = (float)Window( i, rightWindowStart, rightN );
			}
		}
		
		void FloorCurveDecode( Mapping mapping ) {
			for( int i = 0; i < channels; i++ ) {
				int submapNum = mapping.mux[i];
				int floorNum = mapping.submapFloor[submapNum];
				
				Floor floor = floorConfigs[floorNum];
				noResidue[i] = (bool)floor.ReadPerPacketData( this, bitReader, null );
			}
			
			// nonezero vector propagate
			for( int i = 0; i < mapping.couplingSteps; i++ ) {
				if( !noResidue[mapping.magnitude[i]] || !noResidue[mapping.angle[i]] ) {
					noResidue[mapping.magnitude[i]] = false;
					noResidue[mapping.angle[i]] = false;
				}
			}
		}
		
		void ResidueDecode( Mapping mapping ) {
			for( int i = 0; i < mapping.submaps; i++ ) {
				int ch = 0;
				for( int j = 0; j < channels; j++ ) {
					if( mapping.mux[j] == i ) {
						doNotDecode[ch] = noResidue[j];
						ch++;
					}
				}
				
				int residueNum = mapping.submapResidue[i];
				residueArgs.ch = ch;
				residueArgs.doNotDecode = doNotDecode;
				residueConfigs[residueNum].ReadPerPacketData( this, bitReader, residueArgs );
				ch = 0;
				for( int j = 0; j < channels; j++ ) {
					if( mapping.mux[j] == i ) {
						// TODO: residue_vector[j] = decoded_residue_vector[ch];
						ch++;
					}
				}
			}
		}
		
		void InverseCoupling( Mapping mapping ) {
			// TODO: inverse coupling
			for( int i = mapping.couplingSteps - 1; i >= 0; i-- ) {
			}
		}
		
		void DotProduct() {
			// TODO: dot product
		}
		
		void InverseMDCT() {
			// TODO: inverse mdct
		}
		
		void OverlapAdd() {
			// TODO: overlapp add
		}
		
		void Output() {
			// TODO: output
		}
		
		static double Window( int i, int start, int n ) {
			double value = ( i - start + 0.5f ) / n * Math.PI / 2;
			double sinValue = Math.Sin( value );
			return Math.Sin( Math.PI / 2 * sinValue * sinValue );
		}
	}
}