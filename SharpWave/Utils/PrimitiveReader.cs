using System;
using System.IO;
using System.Text;

namespace SharpWave {
	
	/// <summary> Class that reads primitives types,
	/// in either little endian or big endian format. </summary>
	/// <remarks> The endianness of this class operates on byte levels, not bit levels.
	/// For reading bits that are encoded in big endian format, use
	/// the BitReader class. </remarks>
	public class PrimitiveReader : IDisposable {
		public Stream stream;
		protected byte[] pbuffer; // Buffer for primitives such as bytes, ints, long, etc.
		public bool BigEndian;
		public bool CloseUnderlyingStream = true;
		protected bool disposed = false;

		public PrimitiveReader( Stream input ) {
			stream = input;
			pbuffer = new byte[8]; // Double, ulong and long types are 8 bytes long.
		}

		protected void Dispose( bool disposing ) {
			if( disposing ) {
				if( stream != null && CloseUnderlyingStream ) {
					stream.Close();
				}
			}
			stream = null;
			pbuffer = null;
			disposed = true;
		}
		
		public void Dispose() {
			Dispose( true );
		}

		/// <summary> Reads an unsigned integer 8 bits long. </summary>
		/// <returns> True if the read value is not equal to 0. </returns>
		public virtual bool ReadBoolean() {
			return ReadByte() != 0;
		}

		/// <summary> Reads an unsigned integer 8 bits long. </summary>
		/// <returns> An unsigned 8 bit integer read from the stream. </returns>
		public virtual byte ReadByte() {
			int byteValue = stream.ReadByte();
			if( byteValue == -1 ) {
				throw new EndOfStreamException();
			}
			return (byte)byteValue;
		}

		/// <summary> Reads an signed integer 8 bits long. </summary>
		/// <returns> An signed 8 bit integer read from the stream. </returns>
		public virtual sbyte ReadSByte() {
			return (sbyte)ReadByte();
		}

		/// <summary> Reads an signed integer 16 bits long. </summary>
		/// <returns> An signed 16 bit integer read from the stream. </returns>
		public virtual short ReadInt16() {
			FillPrimitiveBuffer( 2 );
			if( BigEndian )
				return (short)( pbuffer[0] << 8 | pbuffer[1] );
			return (short)( pbuffer[0] | pbuffer[1] << 8 );
		}

		/// <summary> Reads an unsigned integer 16 bits long. </summary>
		/// <returns> An unsigned 16 bit integer read from the stream. </returns>
		public virtual ushort ReadUInt16() {
			FillPrimitiveBuffer( 2 );
			if( BigEndian )
				return (ushort)( pbuffer[0] << 8 | pbuffer[1] );
			return (ushort)( pbuffer[0] | pbuffer[1] << 8 );
		}
		
		/// <summary> Reads an unsigned integer 24 bits long. </summary>
		/// <returns> An integer with a maximum value of 0x00FFFFFF.
		/// Thus, the value returned will always be positive. </returns>
		public virtual int ReadUInt24() {
			FillPrimitiveBuffer( 3 );
			if( BigEndian )
				return pbuffer[0] << 16 | pbuffer[1] << 8 | pbuffer[2];
			return pbuffer[0] | pbuffer[1] << 8 | pbuffer[2] << 16;
		}

		/// <summary> Reads an signed integer 32 bits long. </summary>
		/// <returns> An signed 32 bit integer read from the stream. </returns>
		public virtual int ReadInt32() {
			FillPrimitiveBuffer( 4 );
			if( BigEndian )
				return pbuffer[0] << 24 | pbuffer[1] << 16 | pbuffer[2] << 8 | pbuffer[3];
			return pbuffer[0] | pbuffer[1] << 8 | pbuffer[2] << 16 | pbuffer[3] << 24;
		}

		/// <summary> Reads an unsigned integer 32 bits long. </summary>
		/// <returns> An unsigned 32 bit integer read from the stream. </returns>
		public virtual uint ReadUInt32() {
			FillPrimitiveBuffer( 4 );
			if( BigEndian )
				return (uint)( pbuffer[0] << 24 | pbuffer[1] << 16 | pbuffer[2] << 8 | pbuffer[3] );
			return (uint)( pbuffer[0] | pbuffer[1] << 8 | pbuffer[2] << 16 | pbuffer[3] << 24 );
		}
		
		/// <summary> Reads an signed integer 64 bits long. </summary>
		/// <returns> An signed 64 bit integer read from the stream. </returns>
		public virtual long ReadInt64() {
			FillPrimitiveBuffer( 8 );
			if( BigEndian )
				return (long)( (ulong)pbuffer[0] << 56 | (ulong)pbuffer[1] << 48 | (ulong)pbuffer[2] << 40 | (ulong)pbuffer[3] << 32 |
				              (ulong)pbuffer[4] << 24 | (ulong)pbuffer[5] << 16 | (ulong)pbuffer[6] << 8 | (ulong)pbuffer[7] );
			return (long)( (ulong)pbuffer[0] | (ulong)pbuffer[1] << 8 | (ulong)pbuffer[2] << 16 | (ulong)pbuffer[3] << 24 |
			              (ulong)pbuffer[4] << 32 | (ulong)pbuffer[5] << 40 | (ulong)pbuffer[6] << 48 | (ulong)pbuffer[7] << 56 );
		}

		/// <summary> Reads an unsigned integer 64 bits long. </summary>
		/// <returns> An unsigned 64 bit integer read from the stream. </returns>
		public virtual ulong ReadUInt64() {
			FillPrimitiveBuffer( 8 );
			if( BigEndian )
				return (ulong)( (ulong)pbuffer[0] << 56 | (ulong)pbuffer[1] << 48 | (ulong)pbuffer[2] << 40 | (ulong)pbuffer[3] << 32 |
				               (ulong)pbuffer[4] << 24 | (ulong)pbuffer[5] << 16 | (ulong)pbuffer[6] << 8 | (ulong)pbuffer[7] );
			return (ulong)( (ulong)pbuffer[0] | (ulong)pbuffer[1] << 8 | (ulong)pbuffer[2] << 16 | (ulong)pbuffer[3] << 24 |
			               (ulong)pbuffer[4] << 32 | (ulong)pbuffer[5] << 40 | (ulong)pbuffer[6] << 48 | (ulong)pbuffer[7] << 56 );
		}

		/// <summary> Reads a floating point value 32 bits long. </summary>
		/// <returns> An 32 bit floating point value read from the stream. </returns>
		public virtual unsafe float ReadSingle() {
			uint value = ReadUInt32();
			return *(float*)&value;
		}

		/// <summary> Reads a floating point value 64 bits long. </summary>
		/// <returns> An 64 bit floating point value read from the stream. </returns>
		public virtual unsafe double ReadDouble() {
			ulong value = ReadUInt64();
			return *(double*)&value;
		}
		
		/// <summary> Attempts to fully read the requested number of bytes,
		/// throwing an exception if the given number of bytes could not read. </summary>
		/// <param name="count"> The number bytes to read. </param>
		/// <exception cref="System.ArgumentOutOfRangeException"> count is less than or equal to 0. </exception>
		/// <exception cref="System.IO.EndOfStreamException"> The end of the stream was reached. </exception>
		/// <returns> A byte array containing data read from the underlying stream. </returns>
		public virtual byte[] ReadBytes( int count ) {
			if( disposed ) {
				throw new ObjectDisposedException( "this" );
			}
			// Exit early if the count is 0, in case the underlying stream has issues with reading 0 bytes.
			if( count == 0 ) {
				return new byte[0];
			}
			byte[] buffer = new byte[count];
			int totalRead = 0;
			do {
				int read = stream.Read( buffer, totalRead, count );
				if( read == 0 ) { // End of stream reached.
					break;
				}
				totalRead += read;
				count -= read;
			} while( count > 0 );
			
			// The end of stream was reached and not enough bytes were read.
			if( totalRead != buffer.Length ) {
				throw new EndOfStreamException();
			}
			return buffer;
		}
		
		public virtual void FillBuffer( byte[] buffer, int offset, int count ) {
			if( disposed ) {
				throw new ObjectDisposedException( "this" );
			}			
			if( count == 0 ) return;
			if( count + offset > buffer.Length )
				throw new ArgumentOutOfRangeException( "count" );
			
			int totalRead = 0;
			do {
				int read = stream.Read( buffer, totalRead + offset, count );
				if( read == 0 ) break;
				totalRead += read;
				count -= read;
			} while( count > 0 );
			
			// The end of stream was reached and not enough bytes were read.
			if( count > 0 ) {
				throw new EndOfStreamException();
			}
		}
		
		public virtual string ReadASCIIString( int bytes ) {
			return Encoding.ASCII.GetString( ReadBytes( bytes ) );
		}
		
		public virtual string ReadUTF8String( int bytes ) {
			return Encoding.UTF8.GetString( ReadBytes( bytes ) );
		}
		
		public virtual string ReadUTF16LEString( int bytes ) {
			return Encoding.Unicode.GetString( ReadBytes( bytes ) );
		}
		
		public virtual string ReadUTF16BEString( int bytes ) {
			return Encoding.BigEndianUnicode.GetString( ReadBytes( bytes ) );
		}
		
		public virtual string ReadString( int bytes, Encoding encoding ) {
			if( encoding == null ) throw new ArgumentNullException( "encoding" );
			return encoding.GetString( ReadBytes( bytes ) );
		}
		
		public virtual Guid ReadGuid() {
			return new Guid( ReadBytes( 16 ) );
		}
		
		[Obsolete( "Avoid retrieving length, as this won't work on some streams." )]
		public long Length {
			get { return stream.Length; }
		}
		
		[Obsolete( "Avoid retrieving position, as this won't work on some streams." )]
		public long Position {
			get { return stream.Position; }
		}
		
		[Obsolete( "Avoid seeking, as this won't work on some streams." )]
		public void Seek( long offset, SeekOrigin origin ) {
			stream.Seek( offset, origin );
		}
		
		protected void FillPrimitiveBuffer( int count ) {
			if( disposed ) {
				throw new ObjectDisposedException( "PrimitiveReader" );
			}
			
			int totalRead = 0;
			do {
				int read = stream.Read( pbuffer, totalRead, count );
				if( read == 0 ) break;
				totalRead += read;
				count -= read;
			} while( count > 0 );
			
			// The end of stream was reached and not enough bytes were read.
			if( count > 0 ) {
				throw new EndOfStreamException();
			}
		}
		
		/// <summary> Skips the specified number of bytes. </summary>
		/// <remarks> This is not the same as calling ReadBytes(), as
		/// this method reuses a small buffer of 4096 bytes to
		/// use as little memory as possible. </remarks>
		/// <param name="length"> Number of bytes to skip. </param>
		public void SkipData( long length ) {
			byte[] buffer = null;
			while( length > 0 ) {
				int skipSize = (int)Math.Min( 4096, length );
				if( buffer == null ) {
					buffer = new byte[skipSize]; // Not just 4096, in case length is initially < 4096.
				}
				FillBuffer( buffer, 0, skipSize );
				length -= 4096;
			}
		}
	}
}
