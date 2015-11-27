using System;
using SharpWave;
using SharpWave.Codecs;
using SharpWave.Codecs.Vorbis;
using SharpWave.Containers;
using System.IO;

namespace WaveTest {
	
	class Program {
		
		public static void Main( string[] args ) {

			using( var player = new WinMmOut() ) {
				player.Create( 4 );
				using( FileStream fs = File.OpenRead( "hal1.ogg" ) ) {
					var container = new OggContainer( fs );
					player.PlayStreaming( container );
				}
			}
			Console.WriteLine( "done playing" );
			Console.ReadKey( true );
		}
	}
}