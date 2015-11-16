using System;

namespace SharpWave.Codecs.Mpeg {

	public abstract class LayerDecoder {
		
		public abstract byte[] Decode( MpegFrame frame, BitReader reader );
	}
}
