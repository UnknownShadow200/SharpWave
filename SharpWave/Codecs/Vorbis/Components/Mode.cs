using System;
using System.IO;

namespace SharpWave.Codecs.Vorbis {
	
	public class Mode : IVorbisComponent {
		
		public int blockFlag;
		public int modeMapping;
		
		public override void ReadSetupData( VorbisCodec codec, BitReader reader ) {
			blockFlag = reader.ReadBit();
			int windowType = reader.ReadBits( 16 );
			int transformType = reader.ReadBits( 16 );
			modeMapping = reader.ReadBits( 8 );
			if( windowType > 0 || transformType > 0 )
				throw new InvalidDataException( "windowType and/or transformType not 0" );
		}	
	}
}
