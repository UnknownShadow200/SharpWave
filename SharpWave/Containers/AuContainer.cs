using System;
using System.IO;
using SharpWave.Codecs;
using SharpWave.Codecs.Wave;
using SharpWave.Transformers;

namespace SharpWave.Containers {
	
	public sealed class AuContainer : IMediaContainer {
			
		public AuContainer( Stream s ) : base( s ) {
		}
		
		enum AuEncoding : uint {
			Int8G711uLaw = 1,
			Int8LinearPcm = 2,
			Int16LinearPcm = 3,
			Int24LinearPcm = 4,
			Int32LinearPcm = 5,
			Float32LinearPcm = 6,
			Float64LinearPcm = 7,
			
			Int8G711ALaw = 27,
		}
		
		int freq, channels, bitsPerSample;
		Transformer transformer;
		int codecBitsPerSample;
		PrimitiveReader reader;
		uint dataLength;	
		public override void ReadMetadata() {
			reader = new PrimitiveReader( stream );
			string signature = reader.ReadASCIIString( 4 );
			if( signature != ".snd" ) {
				throw new InvalidDataException( "Invalid initial signature." );
			}
			
			reader.BigEndian = true;
			uint dataOffset = reader.ReadUInt32();
			dataLength = reader.ReadUInt32();
			if( dataLength == 0xFFFFFFFF ) {
				dataLength = (uint)( reader.Length - dataOffset );
			}
			
			AuEncoding encoding = (AuEncoding)reader.ReadUInt32();
			Metadata["AU encoding"] = encoding.ToString();
			freq = reader.ReadInt32();
			Metadata[MetadataKeys.SampleRate] = freq.ToString();
			channels = reader.ReadInt32();
			Metadata[MetadataKeys.Channels] = channels.ToString();	
			
			if( dataOffset > 24 ) {
				int infoLength = (int)( dataOffset - 24 );
				string info = reader.ReadASCIIString( infoLength );
				Metadata["File comment"] = info;
			}						
			
			transformer = EmptyTransformer.Instance;
			switch( encoding ) {
				case AuEncoding.Int8G711uLaw:
					transformer = MuLawTransformer.Instance;
					bitsPerSample = 16; codecBitsPerSample = 8;
					break;
					
				case AuEncoding.Int8LinearPcm:
					bitsPerSample = codecBitsPerSample = 8;
					break;
					
				case AuEncoding.Int16LinearPcm:
					bitsPerSample = codecBitsPerSample = 16;
					transformer = BigEndian16BitTo16BitTransformer.Instance;
					break;
					
				case AuEncoding.Int24LinearPcm:
					bitsPerSample = 16; codecBitsPerSample = 24;
					transformer = BigEndian24BitTo16BitTransformer.Instance;
					break;
					
				case AuEncoding.Int32LinearPcm:
					bitsPerSample = 16; codecBitsPerSample = 32;
					transformer = BigEndian32BitTo16BitTransformer.Instance;
					break;
					
				case AuEncoding.Float32LinearPcm:
					bitsPerSample = 16; codecBitsPerSample = 32;
					transformer = BigEndianFloat32To16BitTransformer.Instance;
					break;
					
				case AuEncoding.Float64LinearPcm:
					bitsPerSample = 16; codecBitsPerSample = 64;
					transformer = BigEndianFloat64To16BitTransformer.Instance;
					break;
					
				case AuEncoding.Int8G711ALaw:
					transformer = ALawTransformer.Instance;
					bitsPerSample = 16; codecBitsPerSample = 8;
					break;
					
				default:
					throw new NotSupportedException( "Unsupported audio format: " + encoding );
			}
			Metadata[MetadataKeys.BitsPerSample] = bitsPerSample.ToString();
		}
		
		public override ICodec GetAudioCodec() {
			return new TransformerCodec( dataLength, reader, freq, channels, bitsPerSample,
			                            transformer, codecBitsPerSample );
		}		
	}
}