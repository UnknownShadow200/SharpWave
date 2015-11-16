#define CHECK_FOR_READING_PAST_END
using System;
using System.IO;

namespace SharpWave.Codecs.Mpeg {
	
	public class BitReservoir : Stream {
		
		public readonly int Capacity;
		
		public BitReservoir( int capacity ) {
			Capacity = capacity;
			buffer = new byte[capacity];
		}
		
		int position;
		byte[] buffer;
		int bufferPosition;
		
		public void SetReservoirOffset( int offset ) {
			position = bufferPosition - offset;
		}
		
		public void AddBytes( byte[] bytes ) {
			if( bytes.Length + bufferPosition > Capacity ) {
				int toRemove = bytes.Length - ( Capacity - bufferPosition );
				bufferPosition = Capacity - bytes.Length;
				Buffer.BlockCopy( buffer, toRemove, buffer, 0, Capacity - toRemove );
			}
			Buffer.BlockCopy( bytes, 0, buffer, bufferPosition, bytes.Length );
			bufferPosition += bytes.Length;
		}
		
		public void AddBytesFromStream( int count, PrimitiveReader reader ) {
			if( count + bufferPosition > Capacity ) {
				int toRemove = count - ( Capacity - bufferPosition );
				bufferPosition = Capacity - count;
				Buffer.BlockCopy( buffer, toRemove, buffer, 0, Capacity - toRemove );
			}
			reader.FillBuffer( buffer, bufferPosition, count );
			bufferPosition += count;
		}
		
		public override bool CanRead {
			get { return true; }
		}
		
		public override bool CanSeek {
			get { return false; }
		}
		
		public override bool CanWrite {
			get { return false; }
		}
		
		public override void Flush() {
		}
		
		public override long Length {
			get { throw new NotSupportedException( "Cannot directly get bit reservoir size." ); }
		}
		
		public override long Position {
			get { throw new NotSupportedException( "Cannot directly get bit reservoir position." ); }
			set { throw new NotSupportedException( "Cannot directly seek in bit reservoir." ); }
		}
		
		public override int Read( byte[] buffer, int offset, int count ) {
			if( count == 0 ) return 0;
			#if CHECK_FOR_READING_PAST_END
			if( position + count >= bufferPosition ) throw new InvalidOperationException();
			#endif
			Buffer.BlockCopy( this.buffer, position, buffer, offset, count );
			position += count;
			return count;
		}
		
		public override int ReadByte() {
			#if CHECK_FOR_READING_PAST_END
			if( position + 1 >= bufferPosition ) throw new InvalidOperationException();
			#endif
			return buffer[position++];
		}
		
		public override long Seek( long offset, SeekOrigin origin ) {
			throw new NotSupportedException( "Cannot directly seek in bit reservoir." );
		}
		
		public override void SetLength( long value ) {
			throw new NotSupportedException( "Cannot directly set bit reservoir size." );
		}
		
		public override void Write( byte[] buffer, int offset, int count ) {
			throw new NotSupportedException( "Cannot directly write to bit reservoir." );
		}
	}
}
