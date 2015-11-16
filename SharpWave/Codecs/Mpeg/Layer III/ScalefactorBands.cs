using System;
using SharpWave.Utils;

namespace SharpWave.Codecs.Mpeg {
	
	public sealed partial class LayerIIIDecoder {
		
		static int[] sbEnds_long_32khz = new int[] {
			0, 3, 7, 11, 15, 
			19, 23, 29, 35, 
			43, 53, 65, 81, 
			101, 125, 155, 193, 
			239, 295, 363, 447, 
			549, 575, 575,
		};
		
		static int[] sbEnds_short_32khz = new int[] {
			0, 3, 7, 11, 15, 
			21, 29, 41, 57, 
			77, 103, 137, 179, 575,
		};
		
		static int[] sbEnds_long_441hz = new int[] {
			0, 3, 7, 11, 15,
			19, 23, 29, 35,
			43, 51, 61, 73,
			89, 109, 133, 161,
			195, 237, 287, 341,
			417, 575, 575,
		};
		
		static int[] sbEnds_short_441hz = new int[] {
			0, 3, 7, 11, 15,
			21, 29, 39, 51,
			65, 83, 105, 135,
		};
		
		static int[] sbEnds_long_48khz = new int[] {
			0, 3, 7, 11, 15,
			19, 23, 29, 35,
			41, 49, 59, 71,
			87, 105, 127, 155,
			189, 229, 275, 329,
			383, 575, 575,
		};
		
		static int[] sbEnds_short_48khz = new int[] {
			0, 3, 7, 11, 15,
			21, 27, 37, 49,
			63, 79, 99, 125,
		};
		
		
		static int GetGroup( int sb ) {
			if( sb <= 5 ) return 0;
			if( sb <= 10 ) return 1;
			if( sb <= 15 ) return 2;
			if( sb <= 20 ) return 3;
			return -1;
		}
		
		static int[] slen1Codes = new int[] { 0, 0, 0, 0, 3, 1, 1, 1,
			2, 2, 2, 3, 3, 3, 4, 4 };
		static int[] slen2Codes = new int[] { 0, 1, 2, 3, 0, 1, 2, 3,
			1, 2, 3, 1, 2, 3, 2, 3 };
		
		int ReadScalefactors( int ch, int gr, int[,,,] scalefac ) {
			int scfsiBandsCh = scfsiBands[ch];
			SideInfoGranule granule = granules[ch, gr];
			SideInfoBlocksplitFlag blocksplitInfo = granule.BlocksplitInfo;
			SideInfoNoBlocksplitFlag noBlocksplitInfo = granule.NoBlocksplitInfo;
			
			int compression = granule.ScalefacCompress;
			int slen1 = slen1Codes[compression];
			int slen2 = slen2Codes[compression];
			int bitsRead = 0;
			
			if( granule.BlocksplitFlag && blocksplitInfo.BlockType == 2 ) { // short window
				for( int sb = 0; sb < blocksplitInfo.SwitchPoint; sb++ ) {
					bool bandNotDuplicated = ( scfsiBandsCh & ( 1 << GetGroup( sb ) ) ) == 0;
					if( gr == 0 || bandNotDuplicated ) {
						int bits = sb <= 6 ? slen1 : slen2;
						if( bits == 0 ) continue;
						scalefac[sb, 0, gr, ch] = reader.ReadBits( bits );
						if( !bandNotDuplicated ) scalefac[sb, 0, 1, ch] = scalefac[sb, 0, 0, ch];
						bitsRead += bits;
					}
				}
				for( int sb = blocksplitInfo.SwitchPoint; sb < numSubbandsShort; sb++ ) {
					bool bandNotDuplicated = ( scfsiBandsCh & ( 1 << GetGroup( sb ) ) ) == 0;
					if( gr == 0 || bandNotDuplicated ) {
						int bits = sb <= 5 ? slen1 : slen2; // TODO: or is it 6??
						if( bits == 0 ) continue;
						for( int window = 0; window < 3; window++ ) {
							scalefac[sb, window, gr, ch] = reader.ReadBits( bits );
							if( !bandNotDuplicated ) scalefac[sb, window, 1, ch] = scalefac[sb, window, 0, ch];
							bitsRead += bits;
						}
					}
				}
			} else { // long window
				for( int sb = 0; sb < numSubbands; sb++ ) {
					bool bandNotDuplicated = ( scfsiBandsCh & ( 1 << GetGroup( sb ) ) ) == 0;
					if( gr == 0 || bandNotDuplicated ) {
						int bits = sb <= 10 ? slen1 : slen2;
						if( bits == 0 ) continue;
						scalefac[sb, 0, gr, ch] = reader.ReadBits( bits );
						if( !bandNotDuplicated ) scalefac[sb, 0, 1, ch] = scalefac[sb, 0, 0, ch];
						bitsRead += bits;
					}
				}
			}
			return bitsRead;
		}
	}
}