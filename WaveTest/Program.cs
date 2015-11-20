using System;
using SharpWave;
using SharpWave.Codecs;
using SharpWave.Codecs.Wave;
using SharpWave.Codecs.Flac;
using SharpWave.Codecs.Vorbis;
using SharpWave.Containers;
using SharpWave.Containers.Wave;
using SharpWave.Containers.Flac;
using System.IO;
using SharpWave.Logging;

namespace WaveTest {
	
	class Program {
		
		public static void Main( string[] args ) {

			using( var player = new OpenALOut() ) {
				using( FileStream fs = File.OpenRead( "hal1.ogg" ) ) {
					var container = new OggContainer( fs );
					player.PlayStreaming( container );
				}
				//System.Diagnostics.Debugger.Break();
				/*foreach( string file in Directory.GetFiles( "resources" ) ) {
					try {
					using( FileStream fs = File.OpenRead( file ) ) {
						var container = new OggContainer( fs );
						using( var player2 = new AudioOutputAL() ) {
						player2.StreamData( container );
						}
					}
					} catch { }
				}*/
			}
			
			
			using( var player = new OpenALOut() ) {				
				foreach( string file in Directory.GetFiles( "waV" ) ) {
					using( FileStream fs = File.OpenRead( file ) ) {
						var container = new WaveContainer( fs );
						player.PlayStreaming( container );
					}
				}
				
				/*using( FileStream fs = File.OpenRead( "he_44khz.mp3" ) ) {
					var container = new Mp3Container( fs );
					player.StreamData( container );
				}*/
				/*using( FileStream fs = File.OpenRead( "dk.ogg" ) ) {
					var container = new OggContainer( fs );
					player.StreamData( container );
				}
				using( FileStream fs = File.OpenRead( "temp.wav" ) ) {
					var container = new WaveContainer( fs );
					player.StreamData( container );
				}
				using( FileStream fs = File.OpenRead( "test3.mpeg1" ) ) {
					var container = new MpegContainer( fs );
					player.StreamData( container );
				}
				using( FileStream fs = File.OpenRead( "old/01.wav" ) ) {
					var container = new WaveContainer( fs );
					player.StreamData( container );
				}*/
			}
			Logger.RegisterLogger( new ConsoleLogger() );
			Console.WriteLine( "done playing" );
			Console.ReadKey( true );
		}
	}
}