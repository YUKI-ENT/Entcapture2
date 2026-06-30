using System.Diagnostics;
using ENTcapture2.Core.Models;
using ENTcapture2.WinForms.Capture;

namespace ENTcapture2.WinForms.Integration;

internal sealed class RsBaseFilingService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public async Task<IReadOnlyList<string>> FileRecordingAsync(
        IReadOnlyList<string> sourceFiles,
        ApplicationSettings settings,
        bool fileVideo,
        bool notifyRsBase,
        string patientId,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var destinationFiles = new List<string>();
        if (sourceFiles.Count == 0)
        {
            ENTcapture2.Core.Services.DebugLogger.Info(
                "RsBaseFilingService.FileRecordingAsync: No source files. " +
                "Skipping RSBase reload and patient page.");
            return destinationFiles;
        }

        if (fileVideo)
        {
            Directory.CreateDirectory(settings.SnapshotDirectory);
            for (int index = 0; index < sourceFiles.Count; index++)
            {
                string sourceFile = sourceFiles[index];
                if (!File.Exists(sourceFile))
                {
                    continue;
                }

                long thresholdBytes =
                    settings.ReencodeThresholdMegabytes <= 0
                        ? long.MaxValue
                        : settings.ReencodeThresholdMegabytes * 1024L * 1024L;
                if (new FileInfo(sourceFile).Length > thresholdBytes)
                {
                    string destinationFile = GetAvailableDestinationPath(
                        settings.SnapshotDirectory,
                        Path.ChangeExtension(
                            Path.GetFileName(sourceFile),
                            ".mp4"));
                    progress?.Report(
                        $"動画を圧縮中 {index + 1}/{sourceFiles.Count}: " +
                        Path.GetFileName(destinationFile));
                    await ReencodeAsync(
                        sourceFile,
                        destinationFile,
                        settings.FinalVideoCodec,
                        settings.FinalVideoQuality,
                        cancellationToken);
                    destinationFiles.Add(destinationFile);
                }
                else
                {
                    string destinationFile = GetAvailableDestinationPath(
                        settings.SnapshotDirectory,
                        Path.GetFileName(sourceFile));
                    progress?.Report(
                        $"動画をコピー中 {index + 1}/{sourceFiles.Count}: " +
                        Path.GetFileName(destinationFile));
                    File.Copy(sourceFile, destinationFile);
                    destinationFiles.Add(destinationFile);
                }
            }
        }

        if (notifyRsBase && (!fileVideo || destinationFiles.Count > 0))
        {
            progress?.Report("RSBaseへ取込通知中...");
            await CallReloadUrlAsync(settings, cancellationToken);
            OpenPatientPageIfNeeded(settings, patientId);
        }
        else if (notifyRsBase)
        {
            ENTcapture2.Core.Services.DebugLogger.Info(
                "RsBaseFilingService.FileRecordingAsync: No destination files. " +
                "Skipping RSBase reload and patient page.");
        }

        return destinationFiles;
    }

    public async Task NotifyRsBaseAsync(
        ApplicationSettings settings,
        string patientId,
        CancellationToken cancellationToken = default)
    {
        ENTcapture2.Core.Services.DebugLogger.Info(
            "RsBaseFilingService.NotifyRsBaseAsync: Starting RSBase notification" +
            Environment.NewLine +
            $"  reloadUrl={settings.RsBaseReloadUrl}" +
            Environment.NewLine +
            $"  openPatientPage={settings.OpenRsBasePatientPageAfterFiling}" +
            Environment.NewLine +
            $"  patientId={patientId}");
        if (!string.IsNullOrWhiteSpace(settings.RsBaseReloadUrl))
        {
            await CallReloadUrlAsync(settings, cancellationToken);
            OpenPatientPageIfNeeded(settings, patientId);
            ENTcapture2.Core.Services.DebugLogger.Info(
                "RsBaseFilingService.NotifyRsBaseAsync: RSBase notification completed.");
        }
        else
        {
            ENTcapture2.Core.Services.DebugLogger.Info(
                "RsBaseFilingService.NotifyRsBaseAsync: Reload URL is empty. Skipping notification.");
        }
    }

    private static async Task ReencodeAsync(
        string sourceFile,
        string destinationFile,
        string codec,
        string quality,
        CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = FfmpegRuntime.CreateStartInfo()
        };
        string encoder = string.IsNullOrWhiteSpace(codec)
            ? "AUTO"
            : codec.Trim();
        if (string.Equals(encoder, "libx264", StringComparison.OrdinalIgnoreCase))
        {
            encoder = "AUTO";
        }

        if (string.Equals(encoder, "AUTO", StringComparison.OrdinalIgnoreCase) ||
            encoder is "NVENC" or "AMF" or "QSV")
        {
            encoder = FfmpegRuntime.SelectH264Encoder(encoder).EncoderName;
        }

        FfmpegRuntime.Add(
            process.StartInfo.ArgumentList,
            "-hide_banner",
            "-y",
            "-i",
            sourceFile,
            "-map",
            "0",
            "-c:v",
            encoder);
        AddFinalVideoQualityArguments(
            process.StartInfo.ArgumentList,
            encoder,
            quality);
        FfmpegRuntime.Add(
            process.StartInfo.ArgumentList,
            "-c:a",
            "aac",
            "-b:a",
            "128k",
            "-movflags",
            "+faststart",
            destinationFile);
        await RunAsync(process, cancellationToken);
    }

    private static void AddFinalVideoQualityArguments(
        System.Collections.ObjectModel.Collection<string> arguments,
        string encoder,
        string quality)
    {
        (int crf, int maxrateKbps) = NormalizeFinalVideoQuality(quality);
        string maxrate = $"{maxrateKbps}k";
        string bufferSize = $"{maxrateKbps * 2}k";

        switch (encoder)
        {
            case "libopenh264":
                FfmpegRuntime.Add(
                    arguments,
                    "-b:v",
                    maxrate,
                    "-maxrate",
                    maxrate,
                    "-bufsize",
                    bufferSize);
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
                    "-crf",
                    crf.ToString(),
                    "-maxrate",
                    maxrate,
                    "-bufsize",
                    bufferSize);
                break;
        }
    }

    private static (int Crf, int MaxrateKbps) NormalizeFinalVideoQuality(
        string quality)
    {
        return quality?.Trim() switch
        {
            "Size" => (26, 4000),
            "High" => (20, 12000),
            "Highest" => (18, 20000),
            _ => (23, 8000)
        };
    }

    private static async Task RunAsync(
        Process process,
        CancellationToken cancellationToken)
    {
        process.Start();
        string error = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                FfmpegRuntime.SummarizeError(
                    error,
                    $"ffmpeg が終了コード {process.ExitCode} を返しました。"));
        }
    }

    private static async Task CallReloadUrlAsync(
        ApplicationSettings settings,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.RsBaseReloadUrl))
        {
            return;
        }

        ENTcapture2.Core.Services.DebugLogger.Info(
            $"RsBaseFilingService.CallReloadUrlAsync: GET {settings.RsBaseReloadUrl}");
        using HttpResponseMessage response = await HttpClient.GetAsync(
            settings.RsBaseReloadUrl,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        ENTcapture2.Core.Services.DebugLogger.Info(
            "RsBaseFilingService.CallReloadUrlAsync: Reload URL completed. " +
            $"StatusCode={(int)response.StatusCode}");
    }

    private static void OpenPatientPageIfNeeded(
        ApplicationSettings settings,
        string patientId)
    {
        if (!settings.OpenRsBasePatientPageAfterFiling ||
            string.IsNullOrWhiteSpace(patientId))
        {
            ENTcapture2.Core.Services.DebugLogger.Info(
                "RsBaseFilingService.OpenPatientPageIfNeeded: Skipping patient page." +
                Environment.NewLine +
                $"  openPatientPage={settings.OpenRsBasePatientPageAfterFiling}" +
                Environment.NewLine +
                $"  patientIdIsEmpty={string.IsNullOrWhiteSpace(patientId)}");
            return;
        }

        string url = BuildPatientPageUrl(settings, patientId.Trim());
        ENTcapture2.Core.Services.DebugLogger.Info(
            $"RsBaseFilingService.OpenPatientPageIfNeeded: Opening patient page {url}");
        Process.Start(new ProcessStartInfo(url)
        {
            UseShellExecute = true
        });
    }

    private static string BuildPatientPageUrl(
        ApplicationSettings settings,
        string patientId)
    {
        string escapedPatientId = Uri.EscapeDataString(patientId);
        if (Uri.TryCreate(
                settings.RsBaseReloadUrl,
                UriKind.Absolute,
                out Uri? reloadUri))
        {
            string path = reloadUri.AbsolutePath;
            int lastSlash = path.LastIndexOf('/');
            string directory = lastSlash >= 0 ? path[..(lastSlash + 1)] : "/";
            var builder = new UriBuilder(reloadUri)
            {
                Path = directory + "N2017.cgi",
                Query = $"id_change={escapedPatientId}=="
            };
            return builder.Uri.AbsoluteUri;
        }

        return $"http://127.0.0.1/~rsn/N2017.cgi?id_change={escapedPatientId}==";
    }

    private static string GetAvailableDestinationPath(
        string directory,
        string fileName)
    {
        string candidate = Path.Combine(directory, fileName);
        if (!File.Exists(candidate))
        {
            return candidate;
        }

        string stem = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName);
        string[] parts = stem.Split('~');
        if (parts.Length >= 5 &&
            int.TryParse(parts[1], out int serial))
        {
            for (int index = serial + 1; index <= 9999; index++)
            {
                parts[1] = index.ToString("D4");
                candidate = Path.Combine(
                    directory,
                    string.Join("~", parts) + extension);
                if (!File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        int suffix = 1;
        do
        {
            candidate = Path.Combine(
                directory,
                $"{stem}_{suffix++:D2}{extension}");
        }
        while (File.Exists(candidate));

        return candidate;
    }
}
