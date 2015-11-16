using System;
using System.IO;
using SharpWave.Codecs;
using SharpWave.Codecs.Vorbis;

namespace SharpWave.Containers.Ogg {
	
	public class OggContainer : IMediaContainer {

		public OggContainer( Stream s ) : base( s ) {
		}
		
		VorbisCodec vorbis;
		PrimitiveReader reader;
		public override void ReadMetadata() {
			reader = new PrimitiveReader( stream );
			// TODO: Ogg can encapsulate far more than just vorbis.
			// We need to implement codec detection!
			vorbis = new VorbisCodec( this );
			vorbis.ReadSetupData();
		}
		
		public override ICodec GetAudioCodec() {
			return vorbis;
		}
		
		public override void Flush() {
			stream.Flush();
		}
		
		public override long Length {
			get { throw new NotSupportedException( "Seeking in ogg streams not supported." ); }
		}
		
		public override long Position {
			get { throw new NotSupportedException( "Seeking in ogg streams not supported." ); }
			set { throw new NotSupportedException( "Seeking in ogg streams not supported." ); }
		}

		public override long Seek( long offset, SeekOrigin origin ) {
			throw new NotSupportedException( "Seeking in ogg streams not supported." );
		}
		
		class OggPage {
			public string CapturePattern;
			public byte Version;
			public byte HeaderFlags;
			public long GranulePosition;
			public int StreamNumber;
			public int PageSequence;
			public int Checksum;
			public int NumberOfSegments;
			public byte[] SegmentLengths;
			
			public int curSegmentLength;
			public int curSegmentIndex;
			public byte[] curSegment;
			public int curSegmentOffset;
		}
		
		OggPage page = new OggPage();
		public override int Read( byte[] buffer, int offset, int count ) {
			if( count == 0 ) return 0;
			int read = 0;
			
			// TODO: don't throw EndOfStreamException
			while( count > 0 ) {
				// Find next packet with data
				while( page.NumberOfSegments <= 0 ) {
					ReadOggPage();
				}
				
				CopyPageSegment( ref offset, ref count, ref read, buffer );
				if( page.curSegmentOffset >= page.curSegmentLength ) {
					page.NumberOfSegments--;
					if( page.NumberOfSegments > 0 ) {
						page.curSegmentLength = page.SegmentLengths[page.curSegmentIndex++];
						page.curSegmentOffset = 0;
						page.curSegment = reader.ReadBytes( page.curSegmentLength );
					}
				}
			}
			return read;
		}
		
		public override int ReadByte() {
			while( page.NumberOfSegments <= 0 ) {
				ReadOggPage();
			}
			
			byte value = page.curSegment[page.curSegmentOffset++];
			if( page.curSegmentOffset >= page.curSegmentLength ) {
				page.NumberOfSegments--;
				if( page.NumberOfSegments > 0 ) {
					page.curSegmentLength = page.SegmentLengths[page.curSegmentIndex++];
					page.curSegmentOffset = 0;
					page.curSegment = reader.ReadBytes( page.curSegmentLength );
				}
			}
			return value;
		}
		
		void CopyPageSegment( ref int offset, ref int count, ref int read, byte[] buffer ) {
			int bytesToCopy = Math.Min( count, page.curSegmentLength - page.curSegmentOffset );
			Buffer.BlockCopy( page.curSegment, page.curSegmentOffset, buffer, offset, bytesToCopy );
			
			offset += bytesToCopy;
			count -= bytesToCopy;
			read += bytesToCopy;
			page.curSegmentOffset += bytesToCopy;
		}
		
		void ReadOggPage() {
			page.CapturePattern = reader.ReadASCIIString( 4 );
			if( page.CapturePattern != "OggS" ) {
				throw new InvalidDataException( "Expected 'OggS' capture pattern, got " + page.CapturePattern );
			}
			page.Version = reader.ReadByte();
			page.HeaderFlags = reader.ReadByte();
			page.GranulePosition = reader.ReadInt64();
			page.StreamNumber = reader.ReadInt32();
			page.PageSequence = reader.ReadInt32();
			page.Checksum = reader.ReadInt32();
			page.NumberOfSegments = reader.ReadByte();
			page.SegmentLengths = reader.ReadBytes( page.NumberOfSegments );
			page.curSegmentIndex = 0;
			
			if( page.NumberOfSegments > 0 ) {
				page.curSegmentLength = page.SegmentLengths[page.curSegmentIndex++];
				page.curSegmentOffset = 0;
				page.curSegment = reader.ReadBytes( page.curSegmentLength );
			}
		}
	}
}
