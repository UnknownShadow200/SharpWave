using System;
using System.Collections.Generic;
using System.IO;
using SharpWave.Containers;

namespace SharpWave.Codecs.Vorbis {
	
	public partial class VorbisCodec : ICodec {
		
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

		void ReadIdentificationHeader() {
			uint version = reader.ReadUInt32();
			if( version != 0 )
				throw new InvalidDataException( "Unsupported vorbis version " + version );
			
			channels = reader.ReadByte();
			noResidue = new bool[channels];
			doNotDecode = new bool[channels];
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
		
		internal Codebook[] codebookConfigs;
		internal Floor[] floorConfigs;
		internal Residue[] residueConfigs;
		internal Mapping[] mappingConfigs;
		internal Mode[] modeConfigs;
		
		void ReadSetupHeader() {
			ReadCodebooksSetup( bitReader.ReadBits( 8 ) + 1 );
			ReadTimesSetup( bitReader.ReadBits( 6 ) + 1 );
			ReadFloorsSetup( bitReader.ReadBits( 6 ) + 1 );
			ReadResiduesSetup( bitReader.ReadBits( 6 ) + 1 );
			ReadMappingsSetup( bitReader.ReadBits( 6 ) + 1 );
			ReadModesSetup( bitReader.ReadBits( 6 ) + 1 );
			modeNumberBits = VorbisUtils.iLog( modeConfigs.Length - 1 );
		}
		
		void ReadCodebooksSetup( int count ) {
			codebookConfigs = new Codebook[count];
			for( int i = 0; i < codebookConfigs.Length; i++ ) {
				Codebook codebook = new Codebook();
				codebook.ReadSetupData( this, bitReader );
				codebookConfigs[i] = codebook;
			}
		}
		
		void ReadTimesSetup( int count ) {
			for( int i = 0; i < count; i++ ) {
				int value = bitReader.ReadBits( 16 );
				if( value != 0 ) {
					throw new InvalidDataException( "Time count values must be 0" );
				}
			}
		}
		
		void ReadFloorsSetup( int count ) {
			floorConfigs = new Floor[count];
			for( int i = 0; i < floorConfigs.Length; i++ ) {
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
				floorConfigs[i] = floor;
			}
		}
		
		void ReadResiduesSetup( int count ) {
			residueConfigs = new Residue[count];
			for( int i = 0; i < residueConfigs.Length; i++ ) {
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
				residueConfigs[i] = residue;
			}
			Console.WriteLine();
		}
		
		void ReadMappingsSetup( int count ) {
			mappingConfigs = new Mapping[count];
			for( int i = 0; i < mappingConfigs.Length; i++ ) {
				int mappingType = bitReader.ReadBits( 16 );
				Mapping mapping = null;
				if( mappingType == 0 ) {
					mapping = new Mapping();
				} else {
					throw new InvalidDataException( "Invalid mapping type: " + mappingType );
				}
				mapping.ReadSetupData( this, bitReader );
				mappingConfigs[i] = mapping;
			}
		}
		
		void ReadModesSetup( int count ) {
			modeConfigs = new Mode[count];
			for( int i = 0; i < modeConfigs.Length; i++ ) {
				Mode mode = new Mode();
				mode.ReadSetupData( this, bitReader );
				modeConfigs[i] = mode;
			}
		}
	}
}