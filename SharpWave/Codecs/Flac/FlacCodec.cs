using System;
using System.Collections.Generic;
using System.IO;
using SharpWave.Codecs.Flac;

namespace SharpWave.Codecs {
	
	public partial class FlacCodec : ICodec {
		
		public string Name {
			get { return "Free Loseless Audio Codec"; }
		}
		
		static readonly int[] bitSampleSizes = new [] {
			0, 8, 12, 0, 16, 20, 24, 0
		};
		
		static readonly int[] sampleRates =new [] {
			0, 88200, 176400, 192000, 8000, 16000,
			22050, 24000, 32000, 44100, 48000, 96000,
			0, 0, 0, 0 };
		
		enum ChannelAssignment {
			LeftSide = 0x08,
			RightSide = 0x09,
			MidSide = 0x0A,
		}
		
		int metaSampleRate, metaBitsPerSample;
		public FlacCodec( int metaSampleRate, int metaBitsPerSample ) {
			this.metaSampleRate = metaSampleRate;
			this.metaBitsPerSample = metaBitsPerSample;
		}
		
		public IEnumerable<AudioChunk> StreamData( Stream source ) {
			PrimitiveReader reader = new PrimitiveReader( source );
			reader.BigEndian = true;
			while( true ) {
				
				#region Frame header
				
				FlacBitReader bitReader = new FlacBitReader( reader );
				bitReader.BigEndian = true;
				int syncCode = bitReader.ReadBits( 14 );
				if( syncCode != 0x3FFE ) {
					throw new InvalidDataException( "Invalid synchronisation code." );
				}
				int reserved = bitReader.ReadBit();
				bool variableBlockSize = bitReader.ReadBit() != 0;
				int blockSizeFlags = bitReader.ReadBits( 4 );
				int sampleRateFlags = bitReader.ReadBits( 4 );
				int channelAssignment = bitReader.ReadBits( 4 );
				int sampleSizeFlags = bitReader.ReadBits( 3 );
				if( bitReader.ReadBit() != 0 )
					throw new InvalidDataException( "Reserved bit is not 0." );
				
				byte[] numberData = ReadRawUTF8Char( reader );
				/*int frameNumber = numberData[0];				
				for( int i = 1; i < numberData.Length; i++ ) {
					frameNumber <<= 6;
					frameNumber |= numberData[i];
				}
				Console.WriteLine( frameNumber );*/
				
				int blockSize = 0;
				if( blockSizeFlags == 0x0 ) {
					throw new InvalidDataException( "0 is reserved for block sizes." );
				} else if( blockSizeFlags == 0x1 ) {
					blockSize = 192;
				} else if( blockSizeFlags >= 0x2 && blockSizeFlags <= 0x5 ) {
					blockSize = 576 * ( 1 << ( blockSizeFlags - 2 ) ); // 2^x.
				} else if( blockSizeFlags == 0x6 ) {
					blockSize = reader.ReadByte() + 1;
				} else if( blockSizeFlags == 0x7 ) {
					blockSize = reader.ReadUInt16() + 1;
				} else {
					blockSize = 256 * ( 1 << ( blockSizeFlags - 8 ) ); // 2^x.
				}
				
				int sampleRate = 0;
				if( sampleRateFlags == 0x0 ) {
					sampleRate = metaSampleRate;
				} else if( sampleRateFlags >= 0x01 && sampleRateFlags <= 0xB ) {
					sampleRate = sampleRates[sampleRateFlags];
				} else if( sampleRateFlags == 0xC ) {
					sampleRate = reader.ReadByte();
				} else if( sampleRateFlags == 0xD ) {
					sampleRate = reader.ReadUInt16();
				} else if( sampleRateFlags == 0xE ) {
					sampleRate = reader.ReadUInt16() * 10;
				} else {
					throw new InvalidDataException( "Invalid sample rate flag." );
				}
				
				int bitsPerSample;
				if( sampleSizeFlags == 0 ) {
					bitsPerSample = metaBitsPerSample;
				} else if( sampleSizeFlags == 0x3 || sampleRateFlags == 0x7 ) {
					throw new InvalidDataException( "Sample size is reserved." );
				} else {
					bitsPerSample = bitSampleSizes[sampleSizeFlags];
				}
				
				int channelsCount;
				ChannelAssignment soundAssignment = (ChannelAssignment)channelAssignment;
				if( channelAssignment < 0x08 ) {
					channelsCount = channelAssignment + 1;
				} else if( channelAssignment < 0x0B ) {
					channelsCount = 2;
				} else {
					throw new InvalidDataException( "Channel assignment values > 1010 are reserved." );
				}
				byte crc8 = reader.ReadByte();
				
				#endregion
				
				#region Subframe
				
				int[][] channelsData = new int[channelsCount][];
				for( int i = 0; i < channelsCount; i++ ) {
					if( bitReader.ReadBit() != 0 ) {
						throw new InvalidDataException( "Padding bit should be 0." );
					}
					int[] channelData = null;
					int subframeType = bitReader.ReadBits( 6 );
					bool wastedBitsPerSampleFlag = bitReader.ReadBit() != 0;
					
					int adjustedBitsPerSample = bitsPerSample;
					switch( soundAssignment ) {
						case ChannelAssignment.LeftSide:
							if( i == 1 ) adjustedBitsPerSample++;
							break;
							
						case ChannelAssignment.RightSide:
							if( i == 0 ) adjustedBitsPerSample++;
							break;
							
						case ChannelAssignment.MidSide:
							if( i == 1 ) adjustedBitsPerSample++;
							break;
					}
					
					int wastedBitsPerSample = 0;
					if( wastedBitsPerSampleFlag ) {
						wastedBitsPerSample = 1 + bitReader.ReadUnary();
					}
					if( subframeType == 0x00 ) {
						channelData = ProcessConstantSubframe( bitReader, adjustedBitsPerSample, blockSize );
					} else if( subframeType == 0x01 ) {
						channelData = ProcessVerbatimSubframe( bitReader, adjustedBitsPerSample, blockSize );
					} else {
						if( ( subframeType & 0x20 ) != 0 ) {
							int order = ( subframeType & 0x1F ) + 1;
							channelData = ProcessLpcSubframe( bitReader, adjustedBitsPerSample, order, blockSize );
						} else if( ( subframeType & 0x08 ) != 0 ) {
							int order = subframeType & 0x07;
							channelData = ProcessFixedSubframe( bitReader, adjustedBitsPerSample, order, blockSize );
						}
					}
					channelsData[i] = channelData;
				}
				bitReader.SkipRemainingBits();
				
				#endregion
				
				// Transform the samples into left right
				switch( soundAssignment ) {
					case ChannelAssignment.LeftSide:
						TransformSamplesLS( channelsData[0], channelsData[1] );
						break;
						
					case ChannelAssignment.RightSide:
						TransformSamplesSR( channelsData[0], channelsData[1] );
						break;
						
					case ChannelAssignment.MidSide:
						TransformSamplesMS( channelsData[0], channelsData[1] );
						break;
				}
				
				int bytesPerSample = (int)Math.Ceiling( bitsPerSample / 8.0 );
				byte[] data = new byte[channelsCount * bytesPerSample * blockSize];
				bool use16Bits = bitsPerSample <= 16;
				bool use8Bits = bitsPerSample <= 8;
				int offset = 0;
				for( int i = 0; i < blockSize; i++ ) {
					for( int ch = 0; ch < channelsCount; ch++ ) {
						int[] channelData = channelsData[ch];
						if( use16Bits ) {
							ushort sample = (ushort)channelData[i];
							data[offset++] = (byte)( sample );
							data[offset++] = (byte)( sample >> 8 );
						} else if( use8Bits ) {
							data[offset++] = (byte)channelData[i];
						}
					}
				}
				
				// Read frame footer
				ushort crc16 = reader.ReadUInt16();
				
				AudioChunk chunk = new AudioChunk();
				chunk.Frequency = sampleRate;
				chunk.Channels = channelsCount;
				chunk.BitsPerSample = bitsPerSample;
				chunk.Data = data;
				yield return chunk;
			}
		}
		
		int[] ProcessConstantSubframe( FlacBitReader bitReader, int bitsPerSample, int blockSize ) {
			int constantValue = bitReader.ReadSignedBits( bitsPerSample );
			int[] data = new int[blockSize];
			for( int i = 0; i < data.Length; i++ ) {
				data[i] = constantValue;
			}
			return data;
		}
		
		
		int[] ProcessVerbatimSubframe( FlacBitReader bitReader, int bitsPerSample, int blockSize ) {
			int[] data = new int[blockSize];
			for( int i = 0; i < data.Length; i++ ) {
				data[i] = bitReader.ReadSignedBits( bitsPerSample );
			}
			return data;
		}
		
		int[] ProcessFixedSubframe( FlacBitReader bitReader, int bitsPerSample, int predictorOrder, int blockSize ) {
			int[] warmUpSamples = new int[predictorOrder];
			for( int i = 0; i < predictorOrder; i++ ) {
				warmUpSamples[i] = bitReader.ReadSignedBits( bitsPerSample );
			}
			int[] residual = ReadResidual( bitReader, predictorOrder, blockSize );
			int[] data = new int[blockSize];
			Buffer.BlockCopy( warmUpSamples, 0, data, 0, predictorOrder * sizeof( int ) );
			FixedSignalRestore( residual, blockSize - predictorOrder, predictorOrder, data );
			return data;
		}
		
		int[] ProcessLpcSubframe( FlacBitReader bitReader, int bitsPerSample, int lpcOrder, int blockSize ) {
			int[] warmUpSamples = new int[lpcOrder];
			for( int i = 0; i < warmUpSamples.Length; i++ ) {
				warmUpSamples[i] = bitReader.ReadSignedBits( bitsPerSample );
			}
			
			int lpcPrecision = bitReader.ReadBits( 4 ) + 1;
			if( lpcPrecision == 16 ) {// 1111 + 1
				throw new InvalidDataException( "Invalid lpc precision." );
			}

			int lpcShift = bitReader.ReadSignedBits( 5 );
			int[] coefficients = new int[lpcOrder];
			for( int i = 0; i < coefficients.Length; i++ ) {
				coefficients[i] = bitReader.ReadSignedBits( lpcPrecision );
			}
			
			int[] residual = ReadResidual( bitReader, lpcOrder, blockSize );
			int[] data = new int[lpcOrder + blockSize];
			Buffer.BlockCopy( warmUpSamples, 0, data, 0, lpcOrder * sizeof( int ) );
			
			if( bitsPerSample + lpcPrecision + ilog2( lpcOrder ) <= 32 ) {
				LpcRestoreSignal( residual, blockSize - lpcOrder, coefficients, lpcOrder, lpcShift, data );
			} else {
				LpcRestoreSignal64( residual, blockSize - lpcOrder, coefficients, lpcOrder, lpcShift, data );
			}
			return data;
		}
		
		public static void LpcRestoreSignal( int[] residual, int length, int[] coefficients, int order, int shift, int[] data ) {
			int offset = order;
			for( int i = 0; i < length; i++ ) {
				int sum = 0;
				for( int j = 0; j < order; j++ ) {
					sum += (int)coefficients[j] * (int)data[offset + i - j - 1];
				}
				data[i + offset] = residual[i] + (int)( sum >> shift );
			}
		}

		public static void LpcRestoreSignal64( int[] residual, int length, int[] coefficients, int order, int shift, int[] data ) {
			int offset = order;
			for( int i = 0; i < length; i++ ) {
				long sum = 0;
				for( int j = 0; j < order; j++ ) {
					sum += coefficients[j] * (long)data[offset + i - j - 1];
				}
				data[i + offset] = residual[i] + (int)( sum >> shift );
			}
		}
		
		int ilog2( int value ) {
			int bits = 0;
			while( ( value >>= 1 ) != 0 )
				bits++;
			return bits;
		}
		
		const int rice2ParamLength = 5;
		const int riceParamLength = 4;
		const int rice2EscapeParam = 0x1F; // 11111
		const int riceEscapeParam = 0x0F;  // 01111
		const int riceEscapeParamLength = 5;

		int[] ReadResidual( FlacBitReader bitReader, int predictorOrder, int blockSize ) {
			int encodingMethod = bitReader.ReadBits( 2 );
			if( !( encodingMethod == 0 || encodingMethod == 1 ) ) {
				throw new InvalidDataException( "Invalid rice encoding method." );
			}
			bool extended = encodingMethod == 1;
			int partitionOrder = bitReader.ReadBits( 4 );
			int partitionsCount = 1 << partitionOrder; // 2^x.
			int partitionSamples = partitionOrder > 0 ? blockSize >> partitionOrder : blockSize - predictorOrder;
			int paramLength = extended ? rice2ParamLength : riceParamLength;
			int escapeParam = extended ? rice2EscapeParam : riceEscapeParam;
			
			int sample = 0;
			int[] residual = new int[blockSize - predictorOrder];
			for( int partition = 0; partition < partitionsCount; partition++ ) {
				int riceParam = bitReader.ReadBits( paramLength );
				if( riceParam < escapeParam ) {
					int n = ( partitionOrder == 0 || partition > 0 ) ? partitionSamples : partitionSamples - predictorOrder;
					for( int i = 0; i < n; i++ ) {
						residual[sample + i] = bitReader.ReadRice( riceParam );
					}
					sample += n;
				} else {
					int rawParamLength = bitReader.ReadBits( riceEscapeParamLength );
					int n = ( partitionOrder == 0 || partition > 0 ) ? partitionSamples : partitionSamples - predictorOrder;
					for( int i = 0; i < n; i++ ) {
						residual[sample + i] = bitReader.ReadBits( rawParamLength );
					}
					sample += n;
				}
			}
			return residual;
		}
		
		
		void FixedSignalRestore( int[] residual, int length, int order, int[] data ) {
			int offset = order;
			switch( order ) {
				case 0:
					Buffer.BlockCopy( residual, 0, data, offset * sizeof( int ), length * sizeof( int ) );
					break;
				case 1:
					for( int i = 0; i < length; i++ )
						data[offset + i] = residual[i] + data[offset + i - 1];
					break;
				case 2:
					for( int i = 0; i < length; i++ )
						data[offset + i] = residual[i] + 2 * data[offset + i - 1] - data[offset + i - 2];
					break;
				case 3:
					for( int i = 0; i < length; i++ )
						data[offset + i] = residual[i] + 3 * data[offset + i - 1] - 3 * data[offset + i - 2] + data[offset + i - 3];
					break;
				case 4:
					for( int i = 0; i < length; i++ )
						data[offset + i] = residual[i] + 4 * data[offset + i - 1] - 6 * data[offset + i - 2] + 4 * data[offset + i - 3] - data[offset + i - 4];
					break;
				default:
					throw new NotSupportedException( "Unsupported order." );
			}
		}
		
		byte[] ReadRawUTF8Char( PrimitiveReader reader ) {
			int byteCount = 0;
			byte header = reader.ReadByte();
			for( int bit = 7; bit >= 0; bit-- ) {
				if( ( header & ( 1 << bit ) ) != 0 ) {
					byteCount++;
					header &= (byte)~( 1 << bit );
				} else {
					break;
				}
			}
			if( byteCount == 0 ) return new byte[] { header };
			
			byte[] data = new byte[byteCount];
			data[0] = header;
			reader.FillBuffer( data, 1, byteCount - 1 );
			for( int i = 1; i < data.Length; i++ ) {
				data[i] &= 0x3F; // Clear highest two bits.
			}
			return data;
		}
		
		static void TransformSamplesSR( int[] side, int[] right ) {
			int len = Math.Min( right.Length, side.Length );
			for( int i = 0; i < len; i++ ) {
				side[i] += right[i];
			}
		}
		
		static void TransformSamplesLS( int[] left, int[] side ) {
			int len = Math.Min( left.Length, side.Length );
			for( int i = 0; i < len; i++ ) {
				side[i] = left[i] - side[i];
			}
		}

		static void TransformSamplesMS( int[] mid, int[] side ) {
			int len = Math.Min( mid.Length, side.Length );
			for( int i = 0; i < len; i++ ) {
				int sideValue = side[i];
				int midValue = mid[i] << 1;
				midValue |= ( sideValue & 1 ); // i.e. if 'side' is odd...
				mid[i] = ( midValue + sideValue ) >> 1;
				side[i] = ( midValue - sideValue ) >> 1;
			}
		}
	}
}
