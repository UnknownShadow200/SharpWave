using System;

namespace SharpWave.Codecs.Vorbis {
	
	public abstract class IVorbisComponent {
		
		public abstract void ReadSetupData( VorbisCodec codec, BitReader reader );
		
		public virtual void ReadPerPacketData( VorbisCodec codec, BitReader reader ) {
			
		}
		
		public virtual void ApplyToFrame( VorbisCodec codec, BitReader reader ) {
			
		}
	}
}
