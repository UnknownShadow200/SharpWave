using System;

namespace SharpWave.Logging {

	public sealed class ConsoleLogger : Logger {
		
		protected override void LogMessageInternal( LoggingType messageType, string message ) {
			switch( messageType ) {
				case LoggingType.Debug:
				case LoggingType.AudioOutputDebug:
				case LoggingType.CodecDebug:
				case LoggingType.TransformerDebug:
					#if DEBUG
					Console.ForegroundColor = ConsoleColor.Gray;
					Console.WriteLine( message );
					Console.ResetColor();
					#endif
					break;
					
				case LoggingType.Error:
				case LoggingType.AudioOutputError:
				case LoggingType.CodecError:
				case LoggingType.TransformerError:
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine( "ERRROR: " + message );
					Console.ResetColor();
					break;
					
				case LoggingType.Normal:
				case LoggingType.AudioOutputNormal:
				case LoggingType.CodecNormal:
				case LoggingType.TransformerNormal:
					Console.WriteLine( message );
					break;
					
				case LoggingType.Warning:
				case LoggingType.AudioOutputWarning:
				case LoggingType.CodecWarning:
				case LoggingType.TransformerWarning:
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.WriteLine( "Warning: " + message );
					Console.ResetColor();
					break;
			}
		}
		
		public override void Dispose() {
		}
	}
}
