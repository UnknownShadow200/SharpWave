using System;
using System.Collections.Generic;

namespace SharpWave.Logging {

	public abstract class Logger {
		
		protected readonly object logLock = new object();
		public void LogMessage( LoggingType messageType, string message ) {
			lock( logLock ) {
				LogMessageInternal( messageType, message );
			}
		}
		
		protected abstract void LogMessageInternal( LoggingType messageType, string message );
		
		public abstract void Dispose();
		
		
		static readonly List<Logger> loggers = new List<Logger>();
		static readonly object loggersLock = new object();
		
		public static void RegisterLogger( Logger logger ) {
			if( logger == null ) throw new ArgumentNullException( "logger" );
			lock( loggersLock ) {
				loggers.Add( logger );
			}
		}
		
		public static void Log( LoggingType messageType, string message ) {
			lock( loggersLock ) {
				loggers.ForEach( l => l.LogMessage( messageType, message ) );
			}
		}
		
		public static void LogFormat( LoggingType messageType, string format, params object[] args ) {
			string message = String.Format( format, args );
			Log( messageType, message );
		}
		
		public static void Log( LoggingType messageType, params string[] lines ) {
			// TODO: Is there a better way of doing this than String.Join() ?
			string message = String.Join( Environment.NewLine, lines );
			Log( messageType, message );
		}
		
		public static void StopLogging() {
			lock( loggersLock ) {
				loggers.ForEach( l => l.Dispose() );
				loggers.Clear();
			}
		}
	}
	
	/// <summary> Enumeration of logging types available. </summary>
	/// <remarks> The enumeration is divided
	/// into general, audio output, transformer, and codec logging groups.
	/// Is it recommended that classes use their respective logging groups.
	/// (i.e. codecs use codec logging group) </remarks>
	public enum LoggingType {
		Normal =  1,
		Warning = 2,
		Error =   3,
		Debug =   4,

		AudioOutputNormal =  1 << 8,
		AudioOutputWarning = 2 << 8,
		AudioOutputError =   3 << 8,
		AudioOutputDebug =   4 << 8,

		CodecNormal =  1 << 16,
		CodecWarning = 2 << 16,
		CodecError =   3 << 16,
		CodecDebug =   4 << 16,

		TransformerNormal =  1 << 24,
		TransformerWarning = 2 << 24,
		TransformerError =   3 << 24,
		TransformerDebug =   4 << 24,
	}
}
