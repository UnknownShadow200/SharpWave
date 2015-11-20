using System;
using System.IO;
using System.Text;
using SharpWave.Codecs;
using SharpWave.Codecs.Wave;
using SharpWave.Logging;
using SharpWave.Transformers;

namespace SharpWave.Containers.Wave {
	
	public class WaveContainer : IMediaContainer {
		
		public WaveContainer( Stream s ) : base( s ) {
		}
		
		Transformer transformer;
		int codecBitsPerSample;
		int chunkDataSize;
		PrimitiveReader reader;
		
		public override void ReadMetadata() {
			reader = new PrimitiveReader( stream );
			string signature = reader.ReadASCIIString( 4 );
			bool bigendian = false, reverseHeaders = false;
			
			if( signature == "RIFF" ) {
			} else if( signature == "RIFX" ) {
				bigendian = true;
			} else if( signature == "FFIR" ) {
				bigendian = true;
				reverseHeaders = true;
			} else {
				throw new InvalidDataException( "Invalid initial signature" );
			}
			
			reader.BigEndian = bigendian;
			int riffChunkSize = reader.ReadInt32();
			string format = reader.ReadASCIIString( 4 );
			if( !( format == "WAVE" || format == "EVAW" ) )
				throw new InvalidDataException( "Invalid format." );
			
			// Although technically a wave chunk is supposed to consist of
			// a 'fmt ' chunk followed by a 'data' chunk, this is not always the case.
			RiffChunkHeader chunk;
			while( true ) {
				chunk = ReadChunkHeader( reader, reverseHeaders );
				if( chunk.Signature == "fmt " ) {
					ProcessFormatChunk( chunk, reader );
					foundAudioInfo = true;
				} else if( chunk.Signature == "data" ) {
					if( !foundAudioInfo ) {
						Logger.Log( LoggingType.CodecError, "Data chunk found before format chunk.",
						           "This usually indicates an improper encoder or a corrupted file." );
						throw new InvalidOperationException( "Stream must be seekable when the data chunk is before the format chunk." );
					}
					break;
				} else {
					reader.SkipData( chunk.DataSize );
				}
			}
			chunkDataSize = chunk.DataSize;
		}
		
		public override ICodec GetAudioCodec() {
			return new TransformerCodec( (uint)chunkDataSize, reader, freq, channels, bitsPerSample,
			                            transformer, codecBitsPerSample );
		}
		
		bool foundAudioInfo;
		int freq, channels, bitsPerSample;
		
		void ProcessFormatChunk( RiffChunkHeader chunk, PrimitiveReader reader ) {
			byte[] chunkData = reader.ReadBytes( chunk.DataSize );
			Stream source = reader.stream;
			reader.stream = new MemoryStream( chunkData );
			int audioFormat = reader.ReadUInt16();
			Metadata["WAVE audio format"] = ((AudioFormat)audioFormat).ToString();
			int channels = reader.ReadInt16();
			Metadata[MetadataKeys.Channels] = channels.ToString();
			int sampleRate = reader.ReadInt32();
			Metadata[MetadataKeys.SampleRate] = sampleRate.ToString();
			int byteRate = reader.ReadInt32();
			int blockAlign = reader.ReadInt16();
			int bitsPerSample = reader.ReadInt16();
			Metadata[MetadataKeys.BitsPerSample] = bitsPerSample.ToString();
			int extraInfoSize = 0;
			#pragma warning disable 0618
			// Usually, only very old wave files don't have this
			// field included. (WaveFormat structure, not WaveFormatEx structure)
			// Supress the warning because it's a MemoryStream.
			if( reader.Position != reader.Length ) {
				extraInfoSize = reader.ReadUInt16();
			}
			#pragma warning restore 0618
			
			this.freq = sampleRate;
			transformer = EmptyTransformer.Instance;
			this.channels = channels;
			this.bitsPerSample = bitsPerSample;
			codecBitsPerSample = bitsPerSample;
			
			switch( (AudioFormat)audioFormat ) {
				case AudioFormat.Pcm: // No compression.
					if( bitsPerSample == 16 && reader.BigEndian ) {
						transformer = BigEndian16BitTo16BitTransformer.Instance;
					}
					break;
					
				case AudioFormat.ALaw:
					transformer = ALawTransformer.Instance;
					this.bitsPerSample = 16;
					break;
					
				case AudioFormat.MuLaw:
					transformer = MuLawTransformer.Instance;
					this.bitsPerSample = 16;
					break;
					
				case AudioFormat.Extensible:
					ushort validBitsPerSample = reader.ReadUInt16();
					uint channelMask = reader.ReadUInt32();
					Guid subFormat = reader.ReadGuid();
					Metadata["extensible guid"] = subFormat.ToString();
					if( subFormat == PcmGuid ) {
					} else if( subFormat == AlawGuid ) {
						transformer = ALawTransformer.Instance;
						this.bitsPerSample = 16;
					} else if( subFormat == MulawGuid ) {
						transformer = MuLawTransformer.Instance;
						this.bitsPerSample = 16;
					} else {
						throw new NotSupportedException( "Unsupported sub format: " + subFormat );
					}
					break;
					
				default:
					throw new NotSupportedException( "Unsupported audio format: " + (AudioFormat)audioFormat );
			}
			reader.stream = source;
		}

		static readonly Guid PcmGuid = new Guid( "00000001-0000-0010-8000-00aa00389b71" );
		static readonly Guid AlawGuid = new Guid( "00000006-0000-0010-8000-00aa00389b71" );
		static readonly Guid MulawGuid = new Guid( "00000007-0000-0010-8000-00aa00389b71" );
		
		static RiffChunkHeader ReadChunkHeader( PrimitiveReader reader, bool reverseHeaders ) {
			byte[] sig = reader.ReadBytes( 4 );
			if( reverseHeaders ) {
				sig = new byte[] { sig[3], sig[2], sig[1], sig[0] };
			}
			RiffChunkHeader chunk = new RiffChunkHeader();
			chunk.Signature = Encoding.ASCII.GetString( sig );
			chunk.DataSize = reader.ReadInt32();
			return chunk;
		}
		
		struct RiffChunkHeader {
			public string Signature;
			public int DataSize;
		}
	}
	
	public enum AudioFormat : ushort {
		Pcm = 0x0001,
		ALaw = 0x0006,
		MuLaw = 0x0007,	
		Extensible = 0xFFFE,
	}
}