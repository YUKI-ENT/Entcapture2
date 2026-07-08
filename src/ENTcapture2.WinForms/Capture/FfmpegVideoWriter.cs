using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
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
        FfmpegEncoderSelection encoder,
        double framesPerSecond,
        int h264Quality)
    {
        Encoder = encoder;
        _frameBuffer = new byte[checked(
            frameSize.Width * frameSize.Height * 3)];

        ProcessStartInfo startInfo = FfmpegRuntime.CreateStartInfo();
        startInfo.RedirectStandardInput = true;
        AddArguments(
            startInfo.ArgumentList,
            path,
            frameSize,
            encoder,
            framesPerSecond,
            h264Quality);

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

        int frameWidth = frame.Width;
        int frameHeight = frame.Height;
        int rowBytes = checked(frameWidth * 3);
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
            _input.Write(_frameBuffer);
        }
        catch (IOException exception)
        {
            throw CreateWriteFailure(exception);
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
        CvSize frameSize,
        FfmpegEncoderSelection encoder,
        double framesPerSecond,
        int h264Quality)
    {
        string frameRate = NormalizeFrameRate(framesPerSecond);
        Add(arguments,
            "-hide_banner",
            "-loglevel", "warning",
            "-y",
            "-fflags", "+genpts",
            "-use_wallclock_as_timestamps", "1",
            "-f", "rawvideo",
            "-pixel_format", "bgr24",
            "-video_size", $"{frameSize.Width}x{frameSize.Height}",
            "-framerate", frameRate,
            "-i", "pipe:0",
            "-an",
            "-c:v", encoder.EncoderName);
        FfmpegRuntime.AddEncoderArguments(
            arguments,
            encoder.EncoderName,
            h264Quality: h264Quality);
        string pixelFormat =
            FfmpegRuntime.GetOutputPixelFormat(encoder.EncoderName);
        Add(arguments,
            "-pix_fmt", pixelFormat,
            "-fps_mode", "vfr",
            "-enc_time_base", "1:1000",
            "-video_track_timescale", "1000",
            "-avoid_negative_ts", "make_zero",
            "-movflags", "+faststart",
            path);
    }

    private static string NormalizeFrameRate(double framesPerSecond)
    {
        double fps = double.IsFinite(framesPerSecond) && framesPerSecond > 0
            ? framesPerSecond
            : 30;
        fps = Math.Clamp(fps, 1, 240);
        return fps.ToString("0.###", CultureInfo.InvariantCulture);
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
