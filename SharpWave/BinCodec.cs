using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SharpWave.Codecs;
using SharpWave.Containers;

namespace SharpWave {
	
	public sealed class BinContainer : IMediaContainer {
		
		BinCodec codec;
		public BinContainer( int freq, int maxSounds )  : base( null ) {
			codec = new BinCodec( freq, maxSounds );
		}
		
		public override void ReadMetadata() { }
		
		public override ICodec GetAudioCodec() { return codec; }
	}
	
	public unsafe sealed class BinCodec : ICodec, IEnumerable<AudioChunk>, IEnumerator<AudioChunk> {
		
		public string Name {
			get { return "ClassicalSharp .bin audio"; }
		}
		
		volatile int SoundsCount = 0;
		readonly object Locker;
		byte[] finalData;
		int[] workBuffer;
		int blockSamples;
		BinSound[] sounds;
		
		public BinCodec( int freq, int maxSounds ) {
			chunk = new AudioChunk();
			chunk.Frequency = freq;
			chunk.BitsPerSample = 16;
			chunk.Channels = 2;
			blockSamples = (freq / 2) * chunk.Channels; // stereo num samples
			sounds = new BinSound[maxSounds];
			
			chunk.BytesUsed = blockSamples * sizeof(short);
			finalData = new byte[blockSamples * sizeof(short)];
			workBuffer = new int[blockSamples];
			chunk.Data = finalData;
			Locker = new object();
		}
		
		public void AddSound( byte[] data, int offset, int length, int channels ) {
			lock( Locker ) {
				BinSound snd;
				snd.Offset = offset; snd.Length = length;
				snd.BytesUsed = 0; snd.Channels = channels;
				snd.Data = data;
				
				if( SoundsCount == sounds.Length ) {
					int i = 0;
					RemoveSound( ref i );
				}
				sounds[SoundsCount++] = snd;
			}
		}
		
		AudioChunk chunk;
		public AudioChunk Current {
			get { return chunk; }
		}
		
		public bool MoveNext() {
			lock( Locker ) {
				return ProcessSounds();
			}
		}
		
		bool ProcessSounds() {
			// adjust offset and length from previous sound processing call
			for( int i = 0; i < SoundsCount; i++ ) {
				sounds[i].Offset += sounds[i].BytesUsed;
				sounds[i].Length -= sounds[i].BytesUsed;
				if( sounds[i].Length <= 0 )
					RemoveSound( ref i );
			}

			if( SoundsCount == 0 ) return false;
			UpdateSounds();
			return true;
		}
		
		void RemoveSound( ref int i ) {
			int j = i;
			for( ; j < SoundsCount - 1; j++ )
				sounds[j] = sounds[j + 1];
			sounds[j] = default( BinSound );
			SoundsCount--;
			i--;
		}
		
		void UpdateSounds() {
			fixed( int* ptr = workBuffer ) {
				byte* bytePtr = (byte*)ptr;
				MemUtils.memset( (IntPtr)bytePtr, 0, 0, blockSamples * sizeof(int) );
			}
			for( int i = 0; i < SoundsCount; i++ ) {
				int div = sounds[i].Channels == 1 ? 2 : 1;
				sounds[i].BytesUsed = Math.Min( chunk.BytesUsed / div, sounds[i].Length );
			}
			
			fixed( byte* rawDst = finalData ) {
				short* dst = (short*)rawDst;
				CombineSounds( dst );
			}
		}
		
		void CombineSounds( short* dst ) {
			// accumulate samples from all sources
			for( int i = 0; i < SoundsCount; i++ ) {
				fixed( byte* rawSrc = sounds[i].Data ) {
					short* src = (short*)rawSrc;
					AddSoundPortion( src, sounds[i] );
				}
			}
			
			// now clip the samples between [-32767, 32767]
			for( int i = 0; i < blockSamples; i++ ) {
				int rawSample = workBuffer[i];
				if( rawSample < -32767 ) rawSample = -32767;
				if( rawSample > 32767 ) rawSample = 32767;
				dst[i] = (short)rawSample;
			}
		}
		
		void AddSoundPortion( short* src, BinSound snd ) {
			int sampleOffset = snd.Offset / 2, samples = snd.BytesUsed / 2;
			if( snd.Channels == 1 ) {
				for( int j = 0; j < samples; j++ ) {
					short srcSample = src[sampleOffset + j];
					workBuffer[j * 2 + 0] += srcSample;
					workBuffer[j * 2 + 1] += srcSample;
				}
			} else {
				for( int j = 0; j < samples; j++ )
					workBuffer[j] += src[sampleOffset + j];
			}
		}
		
		struct BinSound {
			public int Offset, Length;
			public int BytesUsed, Channels;
			public byte[] Data;
		}

		public void Dispose() { }
		
		public void Reset() { }
		
		object IEnumerator.Current { get { return null; } }
		
		IEnumerator IEnumerable.GetEnumerator() { return this; }
		
		public IEnumerator<AudioChunk> GetEnumerator() { return this; }
		
		public IEnumerable<AudioChunk> StreamData( Stream source ) { return this; }
	}
}
