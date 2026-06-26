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
    private bool _disposed;

    public FfmpegVideoWriter(
        string path,
        CvSize frameSize,
        FfmpegEncoderSelection encoder)
    {
        Encoder = encoder;
        _frameBuffer = new byte[checked(
            frameSize.Width * frameSize.Height * 3)];

        ProcessStartInfo startInfo = FfmpegRuntime.CreateStartInfo();
        startInfo.RedirectStandardInput = true;
        AddArguments(startInfo.ArgumentList, path, frameSize, encoder);

        _process = new Process { StartInfo = startInfo };
        try
        {
            _process.Start();
            _input = _process.StandardInput.BaseStream;
            _errorTask = _process.StandardError.ReadToEndAsync();
        }
        catch
        {
            _process.Dispose();
            throw;
        }
    }

    public FfmpegEncoderSelection Encoder { get; }

    public void Write(Mat frame)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (frame.Type() != MatType.CV_8UC3)
        {
            throw new InvalidOperationException(
                $"FFmpeg入力はBGR24が必要です。実際: {frame.Type()}");
        }

        int rowBytes = checked(frame.Width * 3);
        if (frame.IsContinuous() && frame.Step() == rowBytes)
        {
            Marshal.Copy(frame.Data, _frameBuffer, 0, _frameBuffer.Length);
        }
        else
        {
            for (int row = 0; row < frame.Height; row++)
            {
                Marshal.Copy(
                    frame.Ptr(row),
                    _frameBuffer,
                    row * rowBytes,
                    rowBytes);
            }
        }

        _input.Write(_frameBuffer);
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
        CvSize frameSize,
        FfmpegEncoderSelection encoder)
    {
        Add(arguments,
            "-hide_banner",
            "-loglevel", "warning",
            "-y",
            "-fflags", "+genpts",
            "-use_wallclock_as_timestamps", "1",
            "-f", "rawvideo",
            "-pixel_format", "bgr24",
            "-video_size", $"{frameSize.Width}x{frameSize.Height}",
            "-framerate", "1000",
            "-i", "pipe:0",
            "-an",
            "-c:v", encoder.EncoderName);
        FfmpegRuntime.AddEncoderArguments(arguments, encoder.EncoderName);
        string pixelFormat =
            encoder.EncoderName == "mjpeg" ? "yuvj420p" : "yuv420p";
        Add(arguments,
            "-pix_fmt", pixelFormat,
            "-fps_mode", "vfr",
            "-enc_time_base", "1:1000",
            "-video_track_timescale", "1000",
            "-avoid_negative_ts", "make_zero",
            "-movflags", "+faststart",
            path);
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
}
