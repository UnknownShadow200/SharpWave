using System;
using System.IO;
using SharpWave.Codecs;
using SharpWave.Codecs.Flac;

namespace SharpWave.Containers.Flac {
	
	public sealed class FlacContainer : IMediaContainer {
		
		enum MetadataBlockType {
			StreamInfo = 0,
			Padding = 1,
			Application = 2,
			SeekTable = 3,
			VorbisComment = 4,
			Cuesheet = 5,
			Picture = 6,
			// 7..126 are reserved.
			Invalid = 127,
		}
		
		public FlacContainer( Stream s ) : base( s ) {
		}
		
		int metaSampleRate;
		int metaBitsPerSample;
		int channelCount;
		
		public override ICodec GetAudioCodec() {
			return new FlacCodec( metaSampleRate, metaBitsPerSample );
		}
		
		public override void ReadMetadata() {
			PrimitiveReader reader = new PrimitiveReader( this );
			string signature = reader.ReadASCIIString( 4 );
			
			if( signature != "fLaC" ) {
				throw new InvalidDataException( "Invalid signature." );
			}
			reader.BigEndian = true;			
			while( !ReadMetadataBlock( reader ) );
		}
		
		bool ReadMetadataBlock( PrimitiveReader reader ) {
			byte flags = reader.ReadByte();
			bool lastBlock = ( flags & 0x80 ) != 0;
			flags &= 0x7F;
			MetadataBlockType blockType = (MetadataBlockType)flags;
			int metaLength = reader.ReadUInt24();
			
			switch( blockType ) {
				case MetadataBlockType.StreamInfo:
					ProcessMetadataStreamInfo( metaLength, reader );
					break;
					
				case MetadataBlockType.Padding:
					ProcessMetadataPadding( metaLength, reader );
					break;
					
				case MetadataBlockType.Application:
					ProcessMetadataApplication( metaLength, reader );
					break;
					
				case MetadataBlockType.SeekTable:
					ProcessMetadataSeekTable( metaLength, reader );
					break;
					
				case MetadataBlockType.VorbisComment:
					ProcessMetadataVorbisComment( metaLength, reader );
					break;
					
				case MetadataBlockType.Cuesheet:
					ProcessMetadataCuesheet( metaLength, reader );
					break;
					
				case MetadataBlockType.Picture:
					ProcessMetadataPicture( metaLength, reader );
					break;
					
				default:
					throw new InvalidDataException( "Invalid metadata block type." );
			}
			return lastBlock;
		}
		
		void ProcessMetadataStreamInfo( int len, PrimitiveReader reader ) {
			short minBlockSize = reader.ReadInt16();
			short maxBlockSize = reader.ReadInt16();
			int minFrameSize = reader.ReadUInt24();
			int maxFrameSize = reader.ReadUInt24();
			
			FlacBitReader bitReader = new FlacBitReader( reader );
			bitReader.BigEndian = true;
			int sampleRate = bitReader.ReadBits( 20 );
			Metadata[MetadataKeys.SampleRate] = sampleRate.ToString();
			int channels = bitReader.ReadBits( 3 ) + 1;
			Metadata[MetadataKeys.Channels] = channels.ToString();
			int bitsPerSample = bitReader.ReadBits( 5 ) + 1;
			Metadata[MetadataKeys.BitsPerSample] = bitsPerSample.ToString();
			long totalSamples = bitReader.ReadBits64( 36 );
			Metadata["Total samples"] = totalSamples.ToString();
			
			channelCount = channels;
			metaSampleRate = sampleRate;
			metaBitsPerSample = bitsPerSample;		
			byte[] md5hash = reader.ReadBytes( 16 );
		}
		
		void ProcessMetadataPadding( int len, PrimitiveReader reader ) {
			reader.ReadBytes( len );
		}
		
		void ProcessMetadataApplication( int len, PrimitiveReader reader ) {
			string appID = reader.ReadASCIIString( 4 );
			int dataSize = len - 4;
		}
		
		void ProcessMetadataSeekTable( int len, PrimitiveReader reader ) {
			int pointCount = len / 18;
			for( int i = 0; i < pointCount; i++ ) {
				ulong firstSampleNumber = reader.ReadUInt64();
				ulong targetFrameOffset = reader.ReadUInt64();
				ushort numberSamples = reader.ReadUInt16();
				bool isPlaceholder = firstSampleNumber == ulong.MaxValue;
			}
		}
		
		void ProcessMetadataVorbisComment( int len, PrimitiveReader reader ) {
			int bytesRead = 0;
			reader.BigEndian = false;
			while( bytesRead < len ) {
				int vendorLength = reader.ReadInt32();
				string vendor = reader.ReadUTF8String( vendorLength );
				bytesRead += 4 + vendorLength;
				uint commentsCount = reader.ReadUInt32();
				bytesRead += 4;
				for( uint i = 0; i < commentsCount; i++ ) {
					int commentLength = reader.ReadInt32();
					string comment = reader.ReadUTF8String( commentLength );
					bytesRead += 4 + commentLength;
				}
			}
			reader.BigEndian = true;
		}
		
		void ProcessMetadataCuesheet( int len, PrimitiveReader reader ) {
			string catalogueNumber = reader.ReadASCIIString( 128 );
			ulong leadInSamplesCount = reader.ReadUInt64();
			byte format = reader.ReadByte(); // All bits other than 1 are reserved.
			bool compactDisc = ( format & 0x01 ) != 0;
			byte[] reserved = reader.ReadBytes( 258 );
			byte trackCount = reader.ReadByte();
			bool cdda = leadInSamplesCount != 0;
			
			for( int i = 0; i < trackCount; i++ ) {
				ulong trackOffset = reader.ReadUInt64();
				byte trackNumber = reader.ReadByte();
				bool leadOut = cdda ? trackNumber == 170 : trackNumber == 255;
				string isrc = reader.ReadASCIIString( 12 );
				byte trackFlags = reader.ReadByte(); // All bits other than 1 and 2 are reserved.
				bool audio = ( trackFlags & 0x01 ) == 0;
				bool preEmphasis = ( trackFlags & 0x02 ) != 0;
				byte[] trackReserved = reader.ReadBytes( 13 );
				byte indexCount = reader.ReadByte();
				for( int j = 0; j < indexCount; j++ ) {
					ulong indexOffset = reader.ReadUInt64();
					byte indexPointNumber = reader.ReadByte();
					byte[] indexReserved = reader.ReadBytes( 3 );
				}
			}
		}
		
		void ProcessMetadataPicture( int len, PrimitiveReader reader ) {
			uint type = reader.ReadUInt32();
			PictureType pictureType = (PictureType)type;
			int mimeTypeLength = reader.ReadInt32();
			string mimeType = reader.ReadASCIIString( mimeTypeLength );
			int descriptionLength = reader.ReadInt32();
			string description = reader.ReadUTF8String( descriptionLength );
			int width = reader.ReadInt32();
			int height = reader.ReadInt32();
			int colourDepth = reader.ReadInt32(); // Bits per pixel.
			int indexedColoursUsed = reader.ReadInt32();
			int picturelength = reader.ReadInt32();
			byte[] data = reader.ReadBytes( picturelength );
		}
		
		enum PictureType : uint {
			Other = 0,
			FileIcon = 1, // 32 x 32 pixels
			OtherFileIcon = 2,
			FrontCover = 3,
			BackCover = 4,
			LeafletPage = 5,
			Media = 6, // e.g. label side of CD
			LeadArtist = 7,
			Artist = 8,
			Conductor = 9,
			Band = 10,
			Composer = 11,
			Lyricist = 12,
			RecordingLocation = 13,
			DuringRecording = 14,
			DuringPerformance = 15,
			VideoScreenCapture = 16,
			BrightColouredFish = 17,
			Illustration = 18,
			BandLogotype = 19,
			StudioLogotype = 20,
		}
	}
}
