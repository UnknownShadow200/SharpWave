using System;
using System.IO;

namespace SharpWave.Codecs.Vorbis {
	
	public class Mapping : IVorbisComponent {
		
		public int submaps, couplingSteps;
		public byte[] mux;
		public byte[] submapFloor;
		public byte[] submapResidue;
		public byte[] magnitude, angle;
		
		public override void ReadSetupData( VorbisCodec codec, BitReader reader ) {
			submaps = reader.ReadBit() == 0 ? 1 : reader.ReadBits( 4 ) + 1;
			couplingSteps = reader.ReadBit() == 0 ? 0 : reader.ReadBits( 8 ) + 1;
			int channels = codec.channels;
			
			if( couplingSteps > 0 && channels > 1 ) {
				magnitude = new byte[couplingSteps];
				angle = new byte[couplingSteps];
				int bits = VorbisUtils.iLog( channels - 1 );
				
				for( int i = 0; i < couplingSteps; i++ ) {
					magnitude[i] = (byte)reader.ReadBits( bits );
					angle[i] = (byte)reader.ReadBits( bits );
				}
			}
			
			if( reader.ReadBits( 2 ) != 0 )
				throw new InvalidDataException( "Reserved field not 0!" );			
			if( submaps > 1 ) {
				mux = new byte[channels];
				for( int i = 0; i < mux.Length; i++ )
					mux[i] = (byte)reader.ReadBits( 4 );
			} else {
				mux = new byte[channels];
			}
			
			submapFloor = new byte[submaps];
			submapResidue = new byte[submaps];
			for( int i = 0; i < submaps; i++ ) {
				reader.ReadBits( 8 ); // unused time configuration
				submapFloor[i] = (byte)reader.ReadBits( 8 );
				submapResidue[i] = (byte)reader.ReadBits( 8 );
			}
		}	
	}
}
