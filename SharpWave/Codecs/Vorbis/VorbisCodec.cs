using System;
using System.Collections.Generic;
using System.IO;
using SharpWave.Containers;
using SharpWave.Transformers;

namespace SharpWave.Codecs.Vorbis {
	
	public class VorbisCodec : ICodec {
		
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
		
		public void ReadSetupData() {
			// According to spec, we should always first read three packets
			// in the order: identification header, comments header, setup header
			ReadSetupPacket();
			ReadSetupPacket();
			ReadSetupPacket();
		}
		
		void ReadSetupPacket() {
			byte packetType = reader.ReadByte();
			string identifier = reader.ReadASCIIString( 6 );
			if( identifier != "vorbis" )
				throw new InvalidDataException( "Expected 'vorbis' identifier, got: " + identifier );
			
			if( packetType == 1 ) {
				ReadIdentificationHeader();
			} else if( packetType == 3 ) {
				ReadCommentHeader();
			} else if( packetType == 5 ) {
				ReadSetupHeader();
			} else {
				throw new NotSupportedException( "Packet " + packetType + " is not a valid setup packet!" );
			}
			bitReader.SkipRemainingBits();
		}
		
		internal int channels;
		int sampleRate;
		int blockSize0, blockSize1;
		void ReadIdentificationHeader() {
			uint version = reader.ReadUInt32();
			if( version != 0 )
				throw new InvalidDataException( "Unsupported vorbis version " + version );
			
			channels = reader.ReadByte();
			sampleRate = reader.ReadInt32();
			container.Metadata[MetadataKeys.Channels] = channels.ToString();
			container.Metadata[MetadataKeys.SampleRate] = sampleRate.ToString();
			
			int bitrateMax = reader.ReadInt32();
			int bitrateNominal = reader.ReadInt32();
			int bitrateMin = reader.ReadInt32();
			blockSize0 = 1 << bitReader.ReadBits( 4 );
			blockSize1 = 1 << bitReader.ReadBits( 4 );
			if( bitReader.ReadBit() == 0 )
				throw new InvalidDataException( "Framing bit expected to not be 0." );
		}
		
		void ReadCommentHeader() {
			int vendorLength = reader.ReadInt32();
			string vendorString = reader.ReadUTF8String( vendorLength );
			int commentsCount = reader.ReadInt32();
			for( int i = 0; i < commentsCount; i++ ) {
				int commentLength = reader.ReadInt32();
				string comment = reader.ReadUTF8String( commentLength );
			}
			
			if( bitReader.ReadBit() == 0 )
				throw new InvalidDataException( "Framing bit expected to not be 0." );
		}
		
		internal Codebook[] codebookConfigurations;
		internal Floor[] floorConfigurations;
		internal Residue[] residueConfigurations;
		internal Mapping[] mappingConfigurations;
		internal Mode[] modeConfigurations;
		
		void ReadSetupHeader() {
			int codebooksCount = bitReader.ReadBits( 8 ) + 1;
			codebookConfigurations = new Codebook[codebooksCount];
			for( int i = 0; i < codebookConfigurations.Length; i++ ) {
				Codebook codebook = new Codebook();
				codebook.ReadSetupData( this, bitReader );
				codebookConfigurations[i] = codebook;
			}
			
			int timeCount = bitReader.ReadBits( 6 ) + 1;
			for( int i = 0; i < timeCount; i++ ) {
				int value = bitReader.ReadBits( 16 );
				if( value != 0 ) {
					throw new InvalidDataException( "Time count values must be 0" );
				}
			}
			
			int floorCount = bitReader.ReadBits( 6 ) + 1;
			floorConfigurations = new Floor[floorCount];
			for( int i = 0; i < floorConfigurations.Length; i++ ) {
				int floorType = bitReader.ReadBits( 16 );
				Console.Write( " F: " + floorType );
				Floor floor = null;
				if( floorType == 0 ) {
					floor = new Floor0();
				} else if( floorType == 1 ) {
					floor = new Floor1();
				} else {
					throw new InvalidDataException( "Invalid floor type: " + floorType );
				}
				floor.ReadSetupData( this, bitReader );
				floorConfigurations[i] = floor;
			}
			
			int residueCount = bitReader.ReadBits( 6 ) + 1;
			residueConfigurations = new Residue[residueCount];
			for( int i = 0; i < residueConfigurations.Length; i++ ) {
				int residueType = bitReader.ReadBits( 16 );
				Residue residue = null;
				Console.Write( " R: " + residueType );
				if( residueType == 0 ) {
					residue = new Residue0();
				} else if( residueType == 1 ) {
					residue = new Residue1();
				} else if( residueType == 2 ) {
					residue = new Residue2();
				} else {
					throw new InvalidDataException( "Invalid residue type: " + residueType );
				}
				residue.ReadSetupData( this, bitReader );
				residueConfigurations[i] = residue;
			}
			Console.WriteLine();
			
			int mappingsCount = bitReader.ReadBits( 6 ) + 1;
			mappingConfigurations = new Mapping[mappingsCount];
			for( int i = 0; i < mappingConfigurations.Length; i++ ) {
				int mappingType = bitReader.ReadBits( 16 );
				Mapping mapping = null;
				if( mappingType == 0 ) {
					mapping = new Mapping();
				} else {
					throw new InvalidDataException( "Invalid mapping type: " + mappingType );
				}
				mapping.ReadSetupData( this, bitReader );
				mappingConfigurations[i] = mapping;
			}
			
			int modeCount = bitReader.ReadBits( 6 ) + 1;
			modeConfigurations = new Mode[modeCount];
			for( int i = 0; i < modeConfigurations.Length; i++ ) {
				Mode mode = new Mode();
				mode.ReadSetupData( this, bitReader );
				modeConfigurations[i] = mode;
			}
		}
		
		public IEnumerable<AudioChunk> StreamData( Stream source ) {
			while( true ) {
				if( bitReader.ReadBit() != 0 )
					throw new InvalidDataException( "packet type should be audio." );
				
				int modeNumber = bitReader.ReadBits( VorbisUtils.iLog( modeConfigurations.Length - 1 ) );
				Mode mode = modeConfigurations[modeNumber];
				int blockSize = mode.blockFlag == 1 ? blockSize1 : blockSize0;
				
				int blockFlag = mode.blockFlag, prevWindowFlag = 0, nextWindowFlag = 0;
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
				
				bitReader.SkipRemainingBits();
			}
		}
		
		static double Window( int i, int start, int n ) {
			double value = ( i - start + 0.5f ) / n * Math.PI / 2;
			double sinValue = Math.Sin( value );
			return Math.Sin( Math.PI / 2 * sinValue * sinValue );
		}
	}
}