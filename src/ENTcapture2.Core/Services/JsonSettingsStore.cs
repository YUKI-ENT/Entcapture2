using System.Text.Json;
using ENTcapture2.Core.Models;

namespace ENTcapture2.Core.Services;

public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public JsonSettingsStore(string? settingsFilePath = null)
    {
        SettingsFilePath = settingsFilePath ?? AppDataPaths.SettingsFilePath;
        if (settingsFilePath is null)
        {
            CopyLegacySettingsIfNeeded();
        }
    }

    public string SettingsFilePath { get; }

    public async Task<ApplicationSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(SettingsFilePath))
        {
            return ApplicationSettings.CreateDefault();
        }

        await using FileStream stream = File.OpenRead(SettingsFilePath);
        ApplicationSettings? settings =
            await JsonSerializer.DeserializeAsync<ApplicationSettings>(
                stream,
                SerializerOptions,
                cancellationToken);

        if (settings is null)
        {
            throw new InvalidDataException("設定ファイルを読み込めませんでした。");
        }

        Validate(settings);
        return settings;
    }

    public async Task SaveAsync(
        ApplicationSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        await _saveLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Validate(settings);

            string? directory = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string temporaryPath = SettingsFilePath + ".tmp";
            await using (FileStream stream = File.Create(temporaryPath))
            {
                await JsonSerializer.SerializeAsync(
                        stream,
                        settings,
                        SerializerOptions,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            File.Move(temporaryPath, SettingsFilePath, true);
        }
        finally
        {
            _saveLock.Release();
        }
    }

    private static void CopyLegacySettingsIfNeeded()
    {
        if (File.Exists(AppDataPaths.SettingsFilePath) ||
            !File.Exists(AppDataPaths.LegacySettingsFilePath))
        {
            return;
        }

        string? directory = Path.GetDirectoryName(AppDataPaths.SettingsFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.Copy(
            AppDataPaths.LegacySettingsFilePath,
            AppDataPaths.SettingsFilePath);
    }

    public static void Validate(ApplicationSettings settings)
    {
        if (settings.SchemaVersion > ApplicationSettings.CurrentSchemaVersion)
        {
            throw new InvalidDataException(
                $"この設定ファイルは新しい形式です（version {settings.SchemaVersion}）。");
        }

        settings.Presets ??= [];
        settings.SnapshotDirectory =
            string.IsNullOrWhiteSpace(settings.SnapshotDirectory)
                ? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                    "ENTcapture2")
                : settings.SnapshotDirectory;
        settings.JpegQuality = Math.Clamp(settings.JpegQuality, 1, 100);
        settings.SnapshotDebounceMilliseconds =
            Math.Clamp(settings.SnapshotDebounceMilliseconds, 0, 10000);
        settings.SnapshotBestFrameWindowMilliseconds =
            Math.Clamp(settings.SnapshotBestFrameWindowMilliseconds, 0, 1000);
        settings.TemporaryRecordingDirectory =
            string.IsNullOrWhiteSpace(settings.TemporaryRecordingDirectory)
                ? Path.Combine(Path.GetTempPath(), "ENTcapture2")
                : settings.TemporaryRecordingDirectory;
        settings.TemporaryRecordingCodec =
            string.IsNullOrWhiteSpace(settings.TemporaryRecordingCodec)
                ? "MJPG"
                : settings.TemporaryRecordingCodec.Trim().ToUpperInvariant();
        if (settings.TemporaryRecordingCodec is not ("RAW" or "MJPG" or "H264"))
        {
            settings.TemporaryRecordingCodec = "MJPG";
        }

        settings.H264EncoderPreference =
            string.IsNullOrWhiteSpace(settings.H264EncoderPreference)
                ? "AUTO"
                : settings.H264EncoderPreference.Trim().ToUpperInvariant();
        if (settings.H264EncoderPreference is not
            ("AUTO" or "NVENC" or "AMF" or "QSV"))
        {
            settings.H264EncoderPreference = "AUTO";
        }

        settings.RecordingSegmentMinutes =
            Math.Clamp(settings.RecordingSegmentMinutes, 1, 240);
        settings.TemporaryFileRetentionDays =
            Math.Clamp(settings.TemporaryFileRetentionDays, 0, 3650);
        settings.MaximumPreviewFramesPerSecond =
            Math.Clamp(settings.MaximumPreviewFramesPerSecond, 0, 240);
        settings.ReencodeThresholdMegabytes =
            Math.Clamp(settings.ReencodeThresholdMegabytes, 0, 1000000);
        settings.FinalVideoCodec =
            string.IsNullOrWhiteSpace(settings.FinalVideoCodec)
                ? "AUTO"
                : settings.FinalVideoCodec.Trim();
        if (string.Equals(
                settings.FinalVideoCodec,
                "libx264",
                StringComparison.OrdinalIgnoreCase))
        {
            settings.FinalVideoCodec = "AUTO";
        }
        settings.FinalVideoQuality =
            NormalizeFinalVideoQuality(settings.FinalVideoQuality);
        settings.FinalVideoBitrateKbps =
            Math.Clamp(settings.FinalVideoBitrateKbps, 500, 200000);
        settings.ExaminationTypes ??= string.Empty;
        settings.SnapshotHotkey ??= new HotkeySettings();
        settings.CaptureHotkey ??= new HotkeySettings();
        settings.RsBaseTheptDirectory ??= string.Empty;
        settings.RsBaseReloadUrl =
            string.IsNullOrWhiteSpace(settings.RsBaseReloadUrl)
                ? "http://localhost/~rsn/2000"
                : settings.RsBaseReloadUrl.Trim();
        settings.RsBasePatientPageUrlTemplate =
            string.IsNullOrWhiteSpace(settings.RsBasePatientPageUrlTemplate)
                ? ApplicationSettings.CreateDefault().RsBasePatientPageUrlTemplate
                : settings.RsBasePatientPageUrlTemplate.Trim();
        settings.RsBasePatientPageDelayMilliseconds =
            Math.Clamp(settings.RsBasePatientPageDelayMilliseconds, 0, 60000);
        settings.MainWindowWidth =
            Math.Clamp(settings.MainWindowWidth, 0, 10000);
        settings.MainWindowHeight =
            Math.Clamp(settings.MainWindowHeight, 0, 10000);
        settings.ZoomWindowWidth =
            Math.Clamp(settings.ZoomWindowWidth, 0, 10000);
        settings.ZoomWindowHeight =
            Math.Clamp(settings.ZoomWindowHeight, 0, 10000);
        if (!Enum.IsDefined(settings.CaptureMode))
        {
            settings.CaptureMode =
                CaptureOperationMode.ContinuousTemporaryRecording;
        }

        foreach (CapturePreset preset in settings.Presets)
        {
            if (preset.Id == Guid.Empty)
            {
                preset.Id = Guid.NewGuid();
            }

            preset.Name = string.IsNullOrWhiteSpace(preset.Name)
                ? "名称未設定"
                : preset.Name.Trim();
            preset.DeviceId ??= string.Empty;
            preset.DeviceName ??= string.Empty;
            preset.Resolution ??= CaptureResolution.Default;
            preset.ExaminationType ??= string.Empty;
            preset.Roi ??= string.Empty;
            preset.OverlayText ??= string.Empty;
            preset.FontName ??= string.Empty;
            preset.WhiteBalanceRed = Math.Clamp(preset.WhiteBalanceRed, 0, 510);
            preset.WhiteBalanceGreen = Math.Clamp(preset.WhiteBalanceGreen, 0, 510);
            preset.WhiteBalanceBlue = Math.Clamp(preset.WhiteBalanceBlue, 0, 510);
            preset.Gamma = Math.Clamp(preset.Gamma, 0.1, 3.0);
            if (preset.IsVideo)
            {
                preset.PreviewOnly = false;
            }
        }

        if (settings.CaptureMode == CaptureOperationMode.PreviewOnly)
        {
            foreach (CapturePreset preset in settings.Presets)
            {
                preset.PreviewOnly = !preset.IsVideo;
            }

            settings.CaptureMode =
                CaptureOperationMode.ContinuousTemporaryRecording;
        }
    }

    private static string NormalizeFinalVideoQuality(string? value)
    {
        return value?.Trim() switch
        {
            "Size" or "Standard" or "High" or "Highest" => value.Trim(),
            _ => "Standard"
        };
    }
}
