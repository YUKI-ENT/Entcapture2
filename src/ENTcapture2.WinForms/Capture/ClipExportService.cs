using System.Diagnostics;
using System.Collections.ObjectModel;

namespace ENTcapture2.WinForms.Capture;

internal sealed class ClipExportService
{
    public async Task ExportSeparateAsync(
        IReadOnlyList<string> sourceFiles,
        IReadOnlyList<ClipExportRange> ranges,
        IReadOnlyList<string> outputFiles,
        ClipExportOptions? options = null,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Validate(sourceFiles, ranges);
        if (outputFiles.Count != ranges.Count)
        {
            throw new ArgumentException("出力ファイル数が区間数と一致しません。");
        }

        for (int index = 0; index < ranges.Count; index++)
        {
            progress?.Report($"クリップ {index + 1}/{ranges.Count} を出力中...");
            progress?.Report(BuildExportCommandPreview(
                sourceFiles,
                ranges[index],
                outputFiles[index],
                options ?? ClipExportOptions.Copy));
            await ExportOneAsync(
                sourceFiles,
                ranges[index],
                outputFiles[index],
                options ?? ClipExportOptions.Copy,
                cancellationToken);
        }
    }

    public async Task ExportJoinedAsync(
        IReadOnlyList<string> sourceFiles,
        IReadOnlyList<ClipExportRange> ranges,
        string outputFile,
        ClipExportOptions? options = null,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Validate(sourceFiles, ranges);
        string workDirectory = Path.Combine(
            Path.GetTempPath(),
            "ENTcapture2_clip_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDirectory);
        try
        {
            if (ranges.Count == 1)
            {
                progress?.Report("クリップを出力中...");
                progress?.Report(BuildExportCommandPreview(
                    sourceFiles,
                    ranges[0],
                    outputFile,
                    options ?? ClipExportOptions.Copy));
                await ExportOneAsync(
                    sourceFiles,
                    ranges[0],
                    outputFile,
                    options ?? ClipExportOptions.Copy,
                    cancellationToken);
                return;
            }

            var segmentFiles = new List<string>();
            string extension = Path.GetExtension(outputFile);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".mp4";
            }

            for (int index = 0; index < ranges.Count; index++)
            {
                string segmentFile = Path.Combine(
                    workDirectory,
                    $"segment_{index + 1:D3}{extension}");
                progress?.Report($"連結用クリップ {index + 1}/{ranges.Count} を作成中...");
                progress?.Report(BuildExportCommandPreview(
                    sourceFiles,
                    ranges[index],
                    segmentFile,
                    options ?? ClipExportOptions.Copy));
                await ExportOneAsync(
                    sourceFiles,
                    ranges[index],
                    segmentFile,
                    options ?? ClipExportOptions.Copy,
                    cancellationToken);
                segmentFiles.Add(segmentFile);
            }

            progress?.Report("クリップを連結中...");
            progress?.Report(BuildConcatCommandPreview(segmentFiles, outputFile));
            await ConcatAsync(segmentFiles, outputFile, cancellationToken);
        }
        finally
        {
            try
            {
                Directory.Delete(workDirectory, true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private static void Validate(
        IReadOnlyList<string> sourceFiles,
        IReadOnlyList<ClipExportRange> ranges)
    {
        if (sourceFiles.Count == 0)
        {
            throw new ArgumentException("入力動画がありません。");
        }

        if (ranges.Count == 0)
        {
            throw new ArgumentException("出力する区間がありません。");
        }
    }

    private static async Task ExportOneAsync(
        IReadOnlyList<string> sourceFiles,
        ClipExportRange range,
        string outputFile,
        ClipExportOptions options,
        CancellationToken cancellationToken)
    {
        string inputFile = sourceFiles.Count == 1
            ? sourceFiles[0]
            : CreateConcatList(sourceFiles);
        bool deleteInputList = sourceFiles.Count > 1;
        try
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(outputFile) ?? ".");
            using var process = new Process
            {
                StartInfo = FfmpegRuntime.CreateStartInfo()
            };
            FfmpegRuntime.Add(
                process.StartInfo.ArgumentList,
                "-hide_banner",
                "-nostdin",
                "-y");
            if (deleteInputList)
            {
                FfmpegRuntime.Add(
                    process.StartInfo.ArgumentList,
                    "-f",
                    "concat",
                    "-safe",
                    "0");
            }

            FfmpegRuntime.Add(
                process.StartInfo.ArgumentList,
                "-i",
                inputFile,
                "-ss",
                FormatTimestamp(range.Start),
                "-t",
                FormatTimestamp(range.End - range.Start),
                "-map",
                "0:v:0");
            AddVideoOutputArguments(
                process.StartInfo.ArgumentList,
                options);
            FfmpegRuntime.Add(
                process.StartInfo.ArgumentList,
                "-avoid_negative_ts",
                "make_zero",
                outputFile);
            await RunAsync(process, cancellationToken);
        }
        finally
        {
            if (deleteInputList)
            {
                TryDelete(inputFile);
            }
        }
    }

    public static string BuildExportCommandPreview(
        IReadOnlyList<string> sourceFiles,
        ClipExportRange range,
        string outputFile,
        ClipExportOptions options)
    {
        var arguments = new Collection<string>();
        FfmpegRuntime.Add(
            arguments,
            "-hide_banner",
            "-nostdin",
            "-y");
        if (sourceFiles.Count > 1)
        {
            FfmpegRuntime.Add(arguments, "-f", "concat", "-safe", "0");
        }

        FfmpegRuntime.Add(
            arguments,
            "-i",
            sourceFiles.Count == 1
                ? sourceFiles[0]
                : "<concat-list.txt>",
            "-ss",
            FormatTimestamp(range.Start),
            "-t",
            FormatTimestamp(range.End - range.Start),
            "-map",
            "0:v:0");
        AddVideoOutputArguments(arguments, options);
        FfmpegRuntime.Add(
            arguments,
            "-avoid_negative_ts",
            "make_zero",
            outputFile);
        return FormatCommand(FfmpegRuntime.ExecutablePath, arguments);
    }

    public static string BuildConcatCommandPreview(
        IReadOnlyList<string> segmentFiles,
        string outputFile)
    {
        var arguments = new Collection<string>();
        FfmpegRuntime.Add(
            arguments,
            "-hide_banner",
            "-nostdin",
            "-y",
            "-f",
            "concat",
            "-safe",
            "0",
            "-i",
            "<segment-list.txt>",
            "-map",
            "0:v:0",
            "-c:v",
            "copy",
            "-movflags",
            "+faststart",
            outputFile);
        return FormatCommand(FfmpegRuntime.ExecutablePath, arguments);
    }

    private static void AddVideoOutputArguments(
        Collection<string> arguments,
        ClipExportOptions options)
    {
        if (options.Mode == ClipExportMode.Copy)
        {
            FfmpegRuntime.Add(arguments, "-c:v", "copy");
            return;
        }

        string encoder = options.EncoderName;
        if (string.IsNullOrWhiteSpace(encoder))
        {
            encoder = "libopenh264";
        }

        FfmpegRuntime.Add(arguments, "-an", "-c:v", encoder);
        if (options.RateControl == ClipExportRateControl.Bitrate &&
            options.BitrateKbps > 0)
        {
            int bitrateKbps = Math.Clamp(options.BitrateKbps, 500, 200000);
            FfmpegRuntime.Add(
                arguments,
                "-b:v",
                $"{bitrateKbps}k",
                "-maxrate",
                $"{bitrateKbps}k",
                "-bufsize",
                $"{Math.Max(1000, bitrateKbps * 2)}k");
        }
        else if (options.RateControl == ClipExportRateControl.Quality)
        {
            AddQualityArguments(arguments, encoder, options.Quality);
        }

        string pixelFormat = encoder.Equals(
            "mjpeg",
            StringComparison.OrdinalIgnoreCase)
                ? "yuvj420p"
                : "yuv420p";
        FfmpegRuntime.Add(arguments, "-pix_fmt", pixelFormat);
        FfmpegRuntime.AddEncoderArguments(
            arguments,
            encoder,
            bitrateControlled:
                options.RateControl is
                    ClipExportRateControl.Bitrate or
                    ClipExportRateControl.Quality);
    }

    private static void AddQualityArguments(
        Collection<string> arguments,
        string encoder,
        string quality)
    {
        (int crf, int maxrateKbps) = NormalizeQuality(quality);
        string maxrate = $"{maxrateKbps}k";
        string bufferSize = $"{maxrateKbps * 2}k";

        switch (encoder)
        {
            case "mjpeg":
                FfmpegRuntime.Add(arguments, "-q:v", ToMjpegQuality(crf).ToString());
                break;
            case "h264_nvenc":
                FfmpegRuntime.Add(
                    arguments,
                    "-preset",
                    "p4",
                    "-rc",
                    "vbr",
                    "-cq",
                    crf.ToString(),
                    "-b:v",
                    maxrate,
                    "-maxrate",
                    maxrate,
                    "-bufsize",
                    bufferSize);
                break;
            case "h264_qsv":
                FfmpegRuntime.Add(
                    arguments,
                    "-preset",
                    "veryfast",
                    "-global_quality",
                    crf.ToString(),
                    "-maxrate",
                    maxrate,
                    "-bufsize",
                    bufferSize);
                break;
            case "h264_amf":
                FfmpegRuntime.Add(
                    arguments,
                    "-quality",
                    "balanced",
                    "-usage",
                    "transcoding",
                    "-rc",
                    "vbr_peak",
                    "-b:v",
                    maxrate,
                    "-maxrate",
                    maxrate,
                    "-bufsize",
                    bufferSize);
                break;
            case "h264_mf":
                FfmpegRuntime.Add(
                    arguments,
                    "-quality",
                    Math.Clamp(100 - (crf * 2), 40, 90).ToString(),
                    "-b:v",
                    maxrate,
                    "-maxrate",
                    maxrate,
                    "-bufsize",
                    bufferSize);
                break;
            default:
                FfmpegRuntime.Add(
                    arguments,
                    "-preset",
                    "veryfast",
                    "-crf",
                    crf.ToString(),
                    "-maxrate",
                    maxrate,
                    "-bufsize",
                    bufferSize);
                break;
        }
    }

    private static (int Crf, int MaxrateKbps) NormalizeQuality(string quality)
    {
        return quality?.Trim() switch
        {
            "Size" => (26, 4000),
            "High" => (20, 12000),
            "Highest" => (18, 20000),
            _ => (23, 8000)
        };
    }

    private static int ToMjpegQuality(int crf)
    {
        return crf switch
        {
            <= 18 => 2,
            <= 20 => 3,
            <= 23 => 5,
            _ => 8
        };
    }

    private static async Task ConcatAsync(
        IReadOnlyList<string> segmentFiles,
        string outputFile,
        CancellationToken cancellationToken)
    {
        string listFile = CreateConcatList(segmentFiles);
        try
        {
            using var process = new Process
            {
                StartInfo = FfmpegRuntime.CreateStartInfo()
            };
            FfmpegRuntime.Add(
                process.StartInfo.ArgumentList,
                "-hide_banner",
                "-nostdin",
                "-y",
                "-f",
                "concat",
                "-safe",
                "0",
                "-i",
                listFile,
                "-map",
                "0:v:0",
                "-c:v",
                "copy",
                "-movflags",
                "+faststart",
                outputFile);
            await RunAsync(process, cancellationToken);
        }
        finally
        {
            TryDelete(listFile);
        }
    }

    private static async Task RunAsync(
        Process process,
        CancellationToken cancellationToken)
    {
        process.Start();
        Task<string> errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        using CancellationTokenSource timeout = CancellationTokenSource
            .CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(10));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                process.Kill(true);
            }
            catch (InvalidOperationException)
            {
            }

            throw new TimeoutException(
                "ffmpeg の処理が10分を超えたため中断しました。");
        }

        string error = await errorTask;
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                FfmpegRuntime.SummarizeError(
                    error,
                    $"ffmpeg が終了コード {process.ExitCode} を返しました。"));
        }
    }

    private static string CreateConcatList(IReadOnlyList<string> files)
    {
        string listFile = Path.Combine(
            Path.GetTempPath(),
            "ENTcapture2_concat_" + Guid.NewGuid().ToString("N") + ".txt");
        File.WriteAllLines(
            listFile,
            files.Select(file => $"file '{EscapeConcatPath(file)}'"));
        return listFile;
    }

    private static string EscapeConcatPath(string path)
    {
        return Path.GetFullPath(path)
            .Replace("\\", "/", StringComparison.Ordinal)
            .Replace("'", "'\\''", StringComparison.Ordinal);
    }

    private static string FormatCommand(
        string executablePath,
        IEnumerable<string> arguments)
    {
        return string.Join(
            " ",
            new[] { QuoteArgument(executablePath) }
                .Concat(arguments.Select(QuoteArgument)));
    }

    private static string QuoteArgument(string value)
    {
        if (value.Length == 0)
        {
            return "\"\"";
        }

        return value.Any(char.IsWhiteSpace) || value.Contains('"')
            ? "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\""
            : value;
    }

    private static string FormatTimestamp(TimeSpan value)
    {
        return value.ToString(@"hh\:mm\:ss\.fff");
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}

internal sealed record ClipExportRange(TimeSpan Start, TimeSpan End);

internal enum ClipExportMode
{
    Copy,
    Reencode
}

internal enum ClipExportRateControl
{
    Quality,
    Bitrate
}

internal sealed record ClipExportOptions(
    ClipExportMode Mode,
    string EncoderName,
    ClipExportRateControl RateControl,
    int BitrateKbps,
    string Quality)
{
    public static ClipExportOptions Copy { get; } =
        new(
            ClipExportMode.Copy,
            string.Empty,
            ClipExportRateControl.Bitrate,
            0,
            string.Empty);
}
