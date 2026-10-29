# VFR recording regression check

On Windows with .NET 10 and the bundled `third_party/ffmpeg/bin` present:

```powershell
dotnet run --project tests/ENTcapture2.RecordingChecks/ENTcapture2.RecordingChecks.csproj -p:Platform=x64
```

Runs the actual timestamped recording writer with MJPEG, CPU H.264 and RAW.
Frames have a nominal input rate of 60 fps, approximately 30 fps capture
timestamps, a 200 ms startup delay, a 700 ms gap and a dropped-frame tail.
They are delivered in a burst, deliberately unrelated to wall-clock time.

Checks frame PTS with ffprobe, a 10-second output duration, decoded image
orientation/colors, and the application's time-based playback seek to 5 seconds.
RAW uses MKV to preserve variable frame timing; H.264/MJPEG use MP4.
Output videos are written to the system temporary directory.
