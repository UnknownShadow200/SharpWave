using System;
using System.IO;

namespace SharpWave.Codecs.Vorbis {
	
	public class Mapping : IVorbisComponent {
		
		public override void ReadSetupData( VorbisCodec codec, BitReader reader ) {
			int submaps = reader.ReadBit() == 0 ? 1 : reader.ReadBits( 4 ) + 1;
			int couplingSteps = reader.ReadBit() == 0 ? 0 : reader.ReadBits( 8 ) + 1;
			int channels = codec.channels;
			
			if( couplingSteps > 0 && channels > 1 ) {
				byte[] magnitude = new byte[couplingSteps];
				byte[] angle = new byte[couplingSteps];
				int bits = VorbisUtils.iLog( channels - 1 );
				
				for( int i = 0; i < couplingSteps; i++ ) {
					magnitude[i] = (byte)reader.ReadBits( bits );
					angle[i] = (byte)reader.ReadBits( bits );
				}
			}
			
			if( reader.ReadBits( 2 ) != 0 )
				throw new InvalidDataException( "Reserved file not 0!" );
			
			if( submaps > 1 ) {
				byte[] mux = new byte[channels];
				for( int i = 0; i < mux.Length; i++ ) {
					mux[i] = (byte)reader.ReadBits( 4 );
				}
			}
			byte[] submapFloor = new byte[submaps];
			byte[] submapResidue = new byte[submaps];
			for( int i = 0; i < submaps; i++ ) {
				reader.ReadBits( 8 ); // unused time configuration
				submapFloor[i] = (byte)reader.ReadBits( 8 );
				submapResidue[i] = (byte)reader.ReadBits( 8 );
			}
		}	
	}
}
