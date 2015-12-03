using System;
using System.IO;
using SharpWave;
using SharpWave.Codecs;
namespace WaveTest {
	
	class Program {
		
		public static void Main( string[] args ) {

			using( var player = new WinMmOut() ) {
				player.Create( 4 );
				byte[] data = File.ReadAllBytes( "dig.bin" );
				
				var container = new BinContainer( 44100, 5 );
				BinCodec codec = container.GetAudioCodec() as BinCodec;
				codec.AddSound( data, 0, data.Length / 2, 1 );
				codec.AddSound( data, data.Length / 4, data.Length / 2, 1 );
				codec.AddSound( data, data.Length / 2 - 1, data.Length / 2, 1 );
				//codec.AddSound( data, data.Length / 2 - 1, data.Length / 2, 1 );
				codec.AddSound( data, 0, data.Length, 1 );
				//codec.AddSound( data, 1497192, 121708, 2 );
				player.PlayStreaming( container );
			}
			Console.WriteLine( "done playing" );
			Console.ReadKey( true );
		}
	}
}