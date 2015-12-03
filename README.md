# SharpWave
Not very fast audio library.

I made this quite a while ago, so most of the code probably isn't that great.

- Supports FLAC, Mpeg layer I and II, WAVE audio codecs.

- Supports outputting to OpenAL and WaveOut (Windows only).


### Licensing
Although SharpWave itself is licensed under BSD-3, it includes a modified version of csvorbis,
which is licensed under LGPL. Thus, this means that as long as csvorbis is included with the source code of SharpWave,
the entirety of SharpWave must also be treated as if it was licensed under LGPL.

If you wish to use SharpWave under a different license, you must remove csvorbis from the project.
