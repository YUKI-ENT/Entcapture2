using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenCvSharp;
using CvSize = OpenCvSharp.Size;

namespace ENTcapture2.WinForms.Capture;

internal sealed class FfmpegVideoWriter : IDisposable
{
    private readonly Process _process;
    private readonly Stream _input;
    private readonly Task<string> _errorTask;
    private readonly byte[] _frameBuffer;
    private readonly TimestampedRawVideoStream _transport;
    private long _lastTimestamp = -1;
    private readonly double _framesPerSecond;
    private bool _disposed;

    public FfmpegVideoWriter(
        string path,
        CvSize frameSize,
        FfmpegEncoderSelection encoder,
        double framesPerSecond,
        int h264Quality)
    {
        Encoder = encoder;
        _framesPerSecond = double.IsFinite(framesPerSecond) && framesPerSecond > 0
            ? Math.Clamp(framesPerSecond, 1, 240) : 30;
        _frameBuffer = new byte[checked(
            frameSize.Width * frameSize.Height * 3)];

        ProcessStartInfo startInfo = FfmpegRuntime.CreateStartInfo();
        startInfo.RedirectStandardInput = true;
        AddArguments(
            startInfo.ArgumentList,
            path,
            encoder,
            h264Quality);

        _process = new Process { StartInfo = startInfo };
        try
        {
            _process.Start();
            _input = _process.StandardInput.BaseStream;
            _errorTask = _process.StandardError.ReadToEndAsync();
            _transport = new TimestampedRawVideoStream(_input, frameSize.Width, frameSize.Height, _framesPerSecond);
        }
        catch
        {
            _process.Dispose();
            throw;
        }
    }

    public FfmpegEncoderSelection Encoder { get; }

    public void Write(Mat frame, TimeSpan capturedAt)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (frame.Type() != MatType.CV_8UC3)
        {
            throw new InvalidOperationException(
                $"FFmpeg入力はBGR24が必要です。実際: {frame.Type()}");
        }

        int frameWidth = frame.Width;
        int frameHeight = frame.Height;
        int rowBytes = checked(frameWidth * 3);
        if (checked(rowBytes * frameHeight) != _frameBuffer.Length)
            throw new ArgumentException("録画中にフレームサイズが変わりました。", nameof(frame));
        if (frame.IsContinuous() && frame.Step() == rowBytes)
        {
            Marshal.Copy(frame.Data, _frameBuffer, 0, _frameBuffer.Length);
        }
        else
        {
            for (int row = 0; row < frameHeight; row++)
            {
                Marshal.Copy(
                    frame.Ptr(row),
                    _frameBuffer,
                    row * rowBytes,
                    rowBytes);
            }
        }

        try
        {
            long timestamp = Math.Max(_lastTimestamp + 1, (long)Math.Round(capturedAt.TotalMilliseconds));
            // Cover startup delay with the first available image, so the file
            // begins at recording start rather than trimming its leading gap.
            if (_lastTimestamp < 0 && timestamp > 0) _transport.WriteFrame(_frameBuffer, 0);
            _transport.WriteFrame(_frameBuffer, timestamp);
            _lastTimestamp = timestamp;
        }
        catch (IOException exception)
        {
            throw CreateWriteFailure(exception);
        }
    }

    // Hold the last image through a dropped-frame tail without filling every gap
    // with duplicate frames. Only an endpoint frame is added if necessary.
    public void Complete(TimeSpan endedAt)
    {
        long endpoint = (long)Math.Round(endedAt.TotalMilliseconds - 1000 / _framesPerSecond);
        if (_lastTimestamp >= 0 && endpoint > _lastTimestamp)
        {
            _transport.WriteFrame(_frameBuffer, endpoint);
            _lastTimestamp = endpoint;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Exception? closeError = null;
        try
        {
            _input.Flush();
            _input.Dispose();
        }
        catch (Exception exception)
        {
            closeError = exception;
        }

        if (!_process.WaitForExit(15000))
        {
            _process.Kill(true);
            _process.WaitForExit();
        }

        string error = _errorTask.GetAwaiter().GetResult();
        int exitCode = _process.ExitCode;
        _process.Dispose();

        if (closeError is not null)
        {
            throw new InvalidOperationException(
                "FFmpegへのフレーム送信を完了できませんでした。",
                closeError);
        }

        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                "FFmpeg録画を正常に終了できませんでした: " +
                FfmpegRuntime.SummarizeError(
                    error,
                    $"終了コード {exitCode}"));
        }
    }

    private static void AddArguments(
        Collection<string> arguments,
        string path,
        FfmpegEncoderSelection encoder,
        int h264Quality)
    {
        Add(arguments,
            "-hide_banner",
            "-loglevel", "warning",
            "-y",
            "-copyts",
            "-f", "matroska",
            "-analyzeduration", "1",
            "-probesize", "32",
            "-fpsprobesize", "0",
            "-i", "pipe:0",
            "-an",
            "-c:v", encoder.EncoderName);
        if (encoder.EncoderName != "rawvideo") FfmpegRuntime.AddEncoderArguments(
            arguments,
            encoder.EncoderName,
            h264Quality: h264Quality);
        string pixelFormat =
            encoder.EncoderName == "rawvideo" ? "bgr24" : FfmpegRuntime.GetOutputPixelFormat(encoder.EncoderName);
        Add(arguments,
            "-pix_fmt", pixelFormat,
            "-bf", "0",
            "-fps_mode", "vfr",
            "-enc_time_base", "1:1000",
            "-video_track_timescale", "1000",
            "-avoid_negative_ts", "disabled",
            "-movflags", "+faststart");
        if (encoder.EncoderName == "rawvideo") Add(arguments, "-allow_raw_vfw", "1");
        Add(arguments, path);
    }

    private static void Add(
        Collection<string> arguments,
        params string[] values)
    {
        foreach (string value in values)
        {
            arguments.Add(value);
        }
    }

    private InvalidOperationException CreateWriteFailure(IOException exception)
    {
        string error = string.Empty;
        if (_process.HasExited || _errorTask.IsCompleted)
        {
            try
            {
                error = _errorTask.GetAwaiter().GetResult();
            }
            catch
            {
                error = string.Empty;
            }
        }

        string message = "FFmpeg録画プロセスへの書き込みに失敗しました。";
        if (_process.HasExited)
        {
            message += $" ExitCode={_process.ExitCode}.";
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            message += " " +
                FfmpegRuntime.SummarizeError(
                    error,
                    "FFmpegの詳細エラーを取得できませんでした。");
        }

        return new InvalidOperationException(message, exception);
    }
}
