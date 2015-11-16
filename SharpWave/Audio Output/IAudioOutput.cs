using System;
using System.IO;
using SharpWave.Codecs;
using SharpWave.Containers;

namespace SharpWave {
	
	public interface IAudioOutput : IDisposable {
		
		void StreamData( IMediaContainer container );
	}
}