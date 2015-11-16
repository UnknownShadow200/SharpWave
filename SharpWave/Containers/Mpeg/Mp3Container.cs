using System;
using System.IO;
using System.Text;
using SharpWave.Codecs;
using SharpWave.Codecs.Mpeg;

namespace SharpWave.Containers.Mpeg {
	
	public class Mp3Container : IMediaContainer {

		public Mp3Container( Stream s ) : base( s ) {
		}
		
		public override void ReadMetadata() {
			PrimitiveReader reader = new PrimitiveReader( this );
			while( true ) {
				string header = reader.ReadASCIIString( 4 );
				if( header == "TAG+" ) { // ID3 1.1 Extended tag
					ReadID31Extended( reader );
				} else if( header.StartsWith( "TAG" ) ) { // ID3 1
					reader.Seek( -1, SeekOrigin.Current );
					ReadID31( reader );
				} else if( header.StartsWith( "ID3" ) ) { // ID3 2
					ID3v2Frame tag = new ID3v2Frame( this, reader, (byte)header[3] );
				} else {
					// Unrecognised header, it's probably the start of the actual MPEG audio stream.
					reader.Seek( -4, SeekOrigin.Current );
					break;
				}
			}
		}
		
		void ReadID31( PrimitiveReader reader ) {
			MusicInfo[MusicInfoKeys.Title] = ReadFixedASCIIString( reader, 30 );
			MusicInfo[MusicInfoKeys.Artist] = ReadFixedASCIIString( reader, 30 );
			MusicInfo[MusicInfoKeys.Album] = ReadFixedASCIIString( reader, 30 );
			MusicInfo[MusicInfoKeys.YearReleased] = ReadFixedASCIIString( reader, 4 );
			// TODO: check for track number in comment field
			string comment = ReadFixedASCIIString( reader, 30 );
			byte genre = reader.ReadByte();
		}
		
		void ReadID31Extended( PrimitiveReader reader ) {
			// TODO: Concat these 3 strings with their ID31.1 strings
			string title = ReadFixedASCIIString( reader, 60 );
			string artist = ReadFixedASCIIString( reader, 60 );
			string album = ReadFixedASCIIString( reader, 60 );
			byte speed = reader.ReadByte();
			string genre = ReadFixedASCIIString( reader, 30 );
			string startTime = ReadFixedASCIIString( reader, 6 );
			string endTime = ReadFixedASCIIString( reader, 6 );
		}
		
		static string ReadFixedASCIIString( PrimitiveReader reader, int count ) {
			byte[] data = reader.ReadBytes( count );
			int index = Array.IndexOf( data, (byte)0 );
			return Encoding.ASCII.GetString( data, 0, index == -1 ? count : index );
		}
		
		public override ICodec GetAudioCodec() {
			return new Mp3Codec();
		}	
	}
}
