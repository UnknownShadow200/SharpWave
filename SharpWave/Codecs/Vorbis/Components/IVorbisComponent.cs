using System;

namespace SharpWave.Codecs.Vorbis {
	
	public abstract class IVorbisComponent {
		
		public abstract void ReadSetupData( VorbisCodec codec, BitReader reader );
		
		public virtual object ReadPerPacketData( VorbisCodec codec, BitReader reader, object args ) {
			return null;
		}
		
		public virtual object ApplyToFrame( VorbisCodec codec, BitReader reader, object args ) {
			return null;
		}
	}
}
