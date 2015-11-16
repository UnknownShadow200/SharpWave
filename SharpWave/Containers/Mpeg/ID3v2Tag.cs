using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SharpWave.Containers.Mpeg {
	
	public abstract class ID3v2Tag {
		
		public string Identifier;
		public int DataSize;
		
		public void Read( Mp3Container container, PrimitiveReader reader ) {
			ReadData( container, reader );
			if( OnRead != null ) {
				OnRead( container, this );
			}
		}
		
		protected abstract void ReadData( Mp3Container container, PrimitiveReader reader );
		
		protected string ReadNullTerminatedEncodedString( PrimitiveReader reader, ref int bytesLeft ) {
			byte encoding = reader.ReadByte(); bytesLeft--;
			byte[] stringData = null;
			Encoding textEncoding = null;
			int index = 0;
			
			if( encoding == 0 ) {
				textEncoding = Encoding.GetEncoding( 28591 );
				stringData = new byte[bytesLeft];
				
				while( bytesLeft > 0 ) {
					byte part = reader.ReadByte();
					if( part == 0x00 ) break;
					
					stringData[index++] = part; bytesLeft--;
				}
			} else if( encoding == 1 ) {
				ushort bom = reader.ReadUInt16(); bytesLeft -= 2;
				stringData = new byte[bytesLeft];
				
				if( bom == 0xFEFF ) {
					textEncoding = Encoding.BigEndianUnicode;
				} else if( bom == 0xFFFE ) {
					textEncoding = Encoding.Unicode;
				} else {
					throw new InvalidDataException( "Invalid bom: " + bom );
				}
				
				while( bytesLeft > 0 ) {
					byte part1 = reader.ReadByte();
					byte part2 = reader.ReadByte();
					if( part1 == 0x00 && part2 == 0x00 ) break;
					
					stringData[index++] = part1; bytesLeft--;
					stringData[index++] = part2; bytesLeft--;
				}
			} else {
				throw new NotSupportedException( "Unsupported encoding " + encoding );
			}
			return textEncoding.GetString( stringData, 0, index );
		}
		
		protected string ReadNullTerminatedString( PrimitiveReader reader, ref int bytesLeft ) {
			int index = 0;
			Encoding textEncoding = Encoding.GetEncoding( 28591 );
			byte[] stringData = new byte[bytesLeft];
			
			while( bytesLeft > 0 ) {
				byte part = reader.ReadByte();
				if( part == 0x00 ) break;
				
				stringData[index++] = part; bytesLeft--;
			}
			return textEncoding.GetString( stringData, 0, index );
		}
		
		public Action<Mp3Container, ID3v2Tag> OnRead;
		
		#region Frame lookups
		
		public static Dictionary<string, Func<ID3v2Tag>> Makev3TagConstructors() {
			return new Dictionary<string, Func<ID3v2Tag>>() {
				// Text information frames
				{ "TALB", () => new TextInformationID3v2Tag() {
						OnRead = (c, tag) => c.MusicInfo[MusicInfoKeys.Title] = ((TextInformationID3v2Tag)tag).value }
				},
				{ "TBPM", () => new TextInformationID3v2Tag() },
				{ "TCOM", () => new TextInformationID3v2Tag() {
						OnRead = (c, tag) => c.MusicInfo[MusicInfoKeys.Composers] = ((TextInformationID3v2Tag)tag).value }
				},
				{ "TCON", () => new TextInformationID3v2Tag() },
				{ "TCOP", () => new TextInformationID3v2Tag() },
				{ "TDAT", () => new TextInformationID3v2Tag() },
				{ "TDLY", () => new TextInformationID3v2Tag() },
				{ "TENC", () => new TextInformationID3v2Tag() },
				{ "TEXT", () => new TextInformationID3v2Tag() },
				{ "TFLT", () => new TextInformationID3v2Tag() },
				{ "TIME", () => new TextInformationID3v2Tag() },
				{ "TIT1", () => new TextInformationID3v2Tag() },
				{ "TIT2", () => new TextInformationID3v2Tag() },
				{ "TIT3", () => new TextInformationID3v2Tag() },
				{ "TKEY", () => new TextInformationID3v2Tag() },
				{ "TLAN", () => new TextInformationID3v2Tag() },
				{ "TLEN", () => new TextInformationID3v2Tag() },
				{ "TMED", () => new TextInformationID3v2Tag() },
				{ "TOAL", () => new TextInformationID3v2Tag() },
				{ "TOFN", () => new TextInformationID3v2Tag() },
				{ "TOLY", () => new TextInformationID3v2Tag() },
				{ "TOPE", () => new TextInformationID3v2Tag() },
				{ "TORY", () => new TextInformationID3v2Tag() },
				{ "TOWN", () => new TextInformationID3v2Tag() },
				{ "TPE1", () => new TextInformationID3v2Tag() },
				{ "TPE2", () => new TextInformationID3v2Tag() },
				{ "TPE3", () => new TextInformationID3v2Tag() },
				{ "TPE4", () => new TextInformationID3v2Tag() },
				{ "TPOW", () => new TextInformationID3v2Tag() },
				{ "TPUN", () => new TextInformationID3v2Tag() },
				{ "TRCK", () => new TextInformationID3v2Tag() },
				{ "TRDA", () => new TextInformationID3v2Tag() },
				{ "TRSN", () => new TextInformationID3v2Tag() },
				{ "TRSO", () => new TextInformationID3v2Tag() },
				{ "TSIZ", () => new TextInformationID3v2Tag() },
				{ "TSRC", () => new TextInformationID3v2Tag() },
				{ "TSSE", () => new TextInformationID3v2Tag() },
				{ "TYER", () => new TextInformationID3v2Tag() {
						OnRead = (c, tag) => c.MusicInfo[MusicInfoKeys.YearReleased] = ((TextInformationID3v2Tag)tag).value }
				},
				// Url link frames				
				{ "WCOM", () => new UrlLinkID3v2Tag() },
				{ "WCOP", () => new UrlLinkID3v2Tag() },
				{ "WOAF", () => new UrlLinkID3v2Tag() },
				{ "WOAR", () => new UrlLinkID3v2Tag() },
				{ "WOAS", () => new UrlLinkID3v2Tag() },
				{ "WORS", () => new UrlLinkID3v2Tag() },
				{ "WPAY", () => new UrlLinkID3v2Tag() },
				{ "WPUB", () => new UrlLinkID3v2Tag() },
			};
		}
		
		#endregion
	}
	
	public class TextInformationID3v2Tag : ID3v2Tag {
		
		public string value;
		protected override void ReadData( Mp3Container container, PrimitiveReader reader ) {
			value = ReadNullTerminatedEncodedString( reader, ref DataSize );
			if( DataSize > 0 ) {
				reader.SkipData( DataSize );
			}
			Console.WriteLine( value );
		}
	}
	
	public class UrlLinkID3v2Tag : ID3v2Tag {
		
		public string value;
		protected override void ReadData( Mp3Container container, PrimitiveReader reader ) {
			value = ReadNullTerminatedString( reader, ref DataSize );
			if( DataSize > 0 ) {
				reader.SkipData( DataSize );
			}
			Console.WriteLine( value );
		}
	}
}
