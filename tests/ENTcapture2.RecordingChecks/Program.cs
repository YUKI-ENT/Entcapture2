using ENTcapture2.WinForms.Capture;
using OpenCvSharp;
using System.Diagnostics;
using System.Text.Json;
using System.Globalization;
foreach (string codec in new[] { "mjpeg", "libopenh264", "rawvideo" }) {
    var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + (codec == "rawvideo" ? ".mkv" : ".mp4"));
    var timestamps = Enumerable.Range(0, 300).Select(i => 200 + i * 1000.0 / 30).Where(t => t < 4000 || t >= 4700).Where(t => t < 9900).ToArray();
    using (var writer = new FfmpegVideoWriter(path, new OpenCvSharp.Size(322, 180), new FfmpegEncoderSelection(codec, codec, false, [], false, null), 60, 18)) {
        using var image = new Mat(180, 322, MatType.CV_8UC3, new Scalar(255, 0, 0));
        using (var bottom = new Mat(image, new Rect(0, 90, 322, 90))) bottom.SetTo(new Scalar(0, 0, 255));
        foreach (double ms in timestamps) writer.Write(image, TimeSpan.FromMilliseconds(ms));
        writer.Complete(TimeSpan.FromSeconds(10));
    }
    var info = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "ffmpeg", "ffprobe.exe")) { RedirectStandardOutput=true, RedirectStandardError=true, UseShellExecute=false, CreateNoWindow=true };
    foreach(var arg in new[] {"-v","error","-select_streams","v:0","-show_frames","-show_format","-show_entries","frame=best_effort_timestamp_time:format=duration","-of","json",path}) info.ArgumentList.Add(arg);
    using var probe = Process.Start(info)!;
    var output = probe.StandardOutput.ReadToEnd(); var error = probe.StandardError.ReadToEnd(); probe.WaitForExit();
    if (probe.ExitCode != 0) throw new Exception(error);
    using var json = JsonDocument.Parse(output);
    var actual = json.RootElement.GetProperty("frames").EnumerateArray().Select(f => double.Parse(f.GetProperty("best_effort_timestamp_time").GetString()!, CultureInfo.InvariantCulture)).ToArray();
    timestamps = new[] { 0.0 }.Concat(timestamps).ToArray();
    for (int i=0;i<timestamps.Length;i++) if(Math.Abs(actual[i] - timestamps[i]/1000) > .003) throw new Exception($"{codec}: PTS mismatch {i}: {actual[i]} expected {timestamps[i]/1000}");
    double duration = double.Parse(json.RootElement.GetProperty("format").GetProperty("duration").GetString()!, CultureInfo.InvariantCulture);
    if (Math.Abs(duration - 10) > .04) throw new Exception($"{codec}: duration {duration}");
    using var capture = new VideoCapture(path); using var decoded = new Mat(); capture.Read(decoded);
    Console.WriteLine($"OpenCV {codec}: fps={capture.Get(VideoCaptureProperties.Fps)}, count={capture.Get(VideoCaptureProperties.FrameCount)}");
    var top = decoded.At<Vec3b>(10, 10); var bottomPixel=decoded.At<Vec3b>(170, 10);
    if(top.Item0 < 200 || bottomPixel.Item2 < 200) throw new Exception($"{codec}: image orientation/color mismatch {top} / {bottomPixel}");
    Console.WriteLine($"PASS {codec}: {actual.Length} frames, duration {duration:F3}s, capture timestamps and colors preserved despite burst writes and 700ms gap");
    capture.Dispose();
    await using (var player = new VideoPlaybackService()) {
        await player.OpenAsync(path);
        player.Pause();
        await Task.Delay(100);
        var sought = new TaskCompletionSource<PlaybackPosition>(TaskCreationOptions.RunContinuationsAsynchronously);
        player.FrameReady += (bitmap, position) => { bitmap.Dispose(); sought.TrySetResult(position); };
        player.Seek(TimeSpan.FromSeconds(5));
        var position = await sought.Task.WaitAsync(TimeSpan.FromSeconds(5));
        if (Math.Abs(position.CurrentTime.TotalSeconds - 5) > .15) throw new Exception($"{codec}: seek landed at {position.CurrentTime}");
        Console.WriteLine($"PASS {codec}: playback seek to 5s -> {position.CurrentTime.TotalSeconds:F3}s");
        await player.CloseAsync();
    }
    File.Delete(path);
}
