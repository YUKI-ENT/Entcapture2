using System.Collections.ObjectModel;
using System.Diagnostics;

namespace ENTcapture2.WinForms.Capture;

internal static class FfmpegRuntime
{
    private static readonly object SyncRoot = new();
    private static FfmpegEncoderSelection? _cachedSelection;

    public static string ExecutablePath
    {
        get
        {
            string path = Path.Combine(
                AppContext.BaseDirectory,
                "ffmpeg",
                "ffmpeg.exe");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    "FFmpegが見つかりません。アプリを再インストールしてください。",
                    path);
            }

            return path;
        }
    }

    public static FfmpegEncoderSelection SelectH264Encoder(
        string preference)
    {
        string normalized = string.IsNullOrWhiteSpace(preference)
            ? "AUTO"
            : preference.Trim().ToUpperInvariant();
        lock (SyncRoot)
        {
            if (normalized == "AUTO")
            {
                return _cachedSelection ??= DetectH264Encoder();
            }

            (string encoder, string displayName) = normalized switch
            {
                "NVENC" => ("h264_nvenc", "NVIDIA NVENC"),
                "AMF" => ("h264_amf", "AMD AMF"),
                "QSV" => ("h264_qsv", "Intel Quick Sync"),
                _ => throw new ArgumentException(
                    $"未対応のH.264エンコーダー指定です: {preference}")
            };
            FfmpegProbeResult requestedResult = ProbeEncoder(encoder);
            if (requestedResult.Success)
            {
                return new FfmpegEncoderSelection(
                    encoder,
                    displayName,
                    true,
                    [],
                    false,
                    null);
            }

            FfmpegEncoderSelection fallback =
                _cachedSelection ??= DetectH264Encoder();
            return fallback with
            {
                UsedFallback = true,
                FallbackMessage =
                    $"{displayName}を使用できないため、" +
                    $"{fallback.DisplayName}へ切り替えました。" +
                    Environment.NewLine +
                    requestedResult.Message
            };
        }
    }

    public static FfmpegEncoderSelection SelectMjpegEncoder()
    {
        _ = ExecutablePath;
        return new FfmpegEncoderSelection(
            "mjpeg",
            "FFmpeg MJPEG",
            false,
            [],
            false,
            null);
    }

    private static FfmpegEncoderSelection DetectH264Encoder()
    {
        List<string> failures = [];
        foreach ((string encoder, string displayName) in new[]
                 {
                     ("h264_nvenc", "NVIDIA NVENC"),
                     ("h264_amf", "AMD AMF"),
                     ("h264_qsv", "Intel Quick Sync"),
                     ("h264_mf", "Media Foundation"),
                     ("libopenh264", "OpenH264 (CPU)")
                 })
        {
            FfmpegProbeResult result = ProbeEncoder(encoder);
            if (result.Success)
            {
                return new FfmpegEncoderSelection(
                    encoder,
                    displayName,
                    encoder != "libopenh264",
                    failures,
                    false,
                    null);
            }

            failures.Add($"{displayName}: {result.Message}");
        }

        throw new InvalidOperationException(
            "使用可能なH.264エンコーダーがありません。" +
            Environment.NewLine +
            string.Join(Environment.NewLine, failures));
    }

    private static FfmpegProbeResult ProbeEncoder(string encoder)
    {
        using var process = new Process
        {
            StartInfo = CreateStartInfo()
        };
        Add(process.StartInfo.ArgumentList,
            "-hide_banner",
            "-loglevel", "error",
            "-f", "lavfi",
            "-i", "color=size=640x360:rate=1",
            "-frames:v", "1",
            "-an",
            "-c:v", encoder);
        AddEncoderArguments(process.StartInfo.ArgumentList, encoder);
        Add(process.StartInfo.ArgumentList, "-f", "null", "NUL");

        try
        {
            process.Start();
            string error = process.StandardError.ReadToEnd();
            if (!process.WaitForExit(8000))
            {
                process.Kill(true);
                return new FfmpegProbeResult(false, "初期化がタイムアウトしました");
            }

            return process.ExitCode == 0
                ? new FfmpegProbeResult(true, string.Empty)
                : new FfmpegProbeResult(
                    false,
                    SummarizeError(error, $"終了コード {process.ExitCode}"));
        }
        catch (Exception exception)
        {
            return new FfmpegProbeResult(false, exception.Message);
        }
    }

    public static ProcessStartInfo CreateStartInfo()
    {
        return new ProcessStartInfo
        {
            FileName = ExecutablePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true
        };
    }

    public static void AddEncoderArguments(
        Collection<string> arguments,
        string encoder,
        bool bitrateControlled = false)
    {
        switch (encoder)
        {
            case "mjpeg":
                if (!bitrateControlled)
                {
                    Add(arguments, "-q:v", "2");
                }

                break;
            case "h264_nvenc":
                Add(arguments, "-preset", "p1", "-tune", "ll");
                Add(arguments, bitrateControlled
                    ? ["-rc", "vbr"]
                    : ["-rc", "constqp", "-qp", "18"]);
                break;
            case "h264_amf":
                Add(arguments, "-quality", "speed");
                Add(arguments, bitrateControlled
                    ? ["-usage", "transcoding", "-rc", "cbr"]
                    : ["-usage", "lowlatency", "-rc", "cqp", "-qp_i", "18", "-qp_p", "18"]);
                break;
            case "h264_qsv":
                Add(arguments, "-preset", "veryfast");
                if (!bitrateControlled)
                {
                    Add(arguments, "-global_quality", "18");
                }

                break;
            case "h264_mf":
                if (!bitrateControlled)
                {
                    Add(arguments, "-quality", "75");
                }

                break;
            default:
                if (!bitrateControlled)
                {
                    Add(arguments, "-b:v", "12M", "-maxrate", "16M",
                        "-bufsize", "24M");
                }

                break;
        }
    }

    public static string SummarizeError(string error, string fallback)
    {
        string[] lines = error
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        return lines.LastOrDefault()?.Trim() ?? fallback;
    }

    public static void Add(
        Collection<string> arguments,
        params string[] values)
    {
        foreach (string value in values)
        {
            arguments.Add(value);
        }
    }

    private sealed record FfmpegProbeResult(bool Success, string Message);
}

internal sealed record FfmpegEncoderSelection(
    string EncoderName,
    string DisplayName,
    bool IsHardwareAccelerated,
    IReadOnlyList<string> EarlierFailures,
    bool UsedFallback,
    string? FallbackMessage);
