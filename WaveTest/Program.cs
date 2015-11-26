using System;
using SharpWave;
using SharpWave.Codecs;
using SharpWave.Codecs.Vorbis;
using SharpWave.Containers;
using System.IO;

namespace WaveTest {
	
	class Program {
		
		public static void Main( string[] args ) {

			using( var player = new OpenALOut() ) {
				foreach( string file in Directory.GetFiles( "resources/sound3/dig" ) ) {
					using( FileStream fs = File.OpenRead( file ) ) {
						Console.WriteLine( "PLAYING " + file );
						var container = new OggContainer( fs );
						player.PlayStreaming( container );
					}
				}
			}
			
			using( var player = new RawOut( File.Create( "sounds.bin" ), false ) ) {
				foreach( string file in Directory.GetFiles( "resources" ) ) {
					using( FileStream fs = File.OpenRead( file ) ) {
						var container = new OggContainer( fs );
						player.PlayStreaming( container );
					}
				}
			}
			Console.WriteLine( "done playing" );
			Console.ReadKey( true );
		}
	}
}