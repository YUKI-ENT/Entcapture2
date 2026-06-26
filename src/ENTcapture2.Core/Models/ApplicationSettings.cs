namespace ENTcapture2.Core.Models;

public sealed class ApplicationSettings
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public Guid? SelectedPresetId { get; set; }

    public string SnapshotDirectory { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
        "ENTcapture2");

    public int JpegQuality { get; set; } = 95;

    public int SnapshotDebounceMilliseconds { get; set; } = 800;

    public CaptureOperationMode CaptureMode { get; set; } =
        CaptureOperationMode.ContinuousTemporaryRecording;

    public bool TopMostDuringPreview { get; set; }

    public string TemporaryRecordingDirectory { get; set; } = Path.Combine(
        Path.GetTempPath(),
        "ENTcapture2");

    public string TemporaryRecordingCodec { get; set; } = "MJPG";

    public string H264EncoderPreference { get; set; } = "AUTO";

    public int RecordingSegmentMinutes { get; set; } = 10;

    public int TemporaryFileRetentionDays { get; set; }

    public int MaximumPreviewFramesPerSecond { get; set; }

    public int ReencodeThresholdMegabytes { get; set; }

    public string FinalVideoCodec { get; set; } = "libx264";

    public string FinalVideoQuality { get; set; } = "Standard";

    public int FinalVideoBitrateKbps { get; set; } = 4000;

    public string ExaminationTypes { get; set; } =
        "内視鏡,鼓膜,眼振,顕微鏡";

    public HotkeySettings SnapshotHotkey { get; set; } = new()
    {
        Control = true,
        Shift = true,
        KeyCode = 83
    };

    public HotkeySettings CaptureHotkey { get; set; } = new()
    {
        Control = true,
        Shift = true,
        KeyCode = 82
    };

    public string RsBaseTheptDirectory { get; set; } = @"C:\common";

    public string RsBaseReloadUrl { get; set; } =
        "http://localhost/~rsn/2000";

    public bool AutoFileToRsBase { get; set; }

    public int MainWindowLeft { get; set; } = int.MinValue;

    public int MainWindowTop { get; set; } = int.MinValue;

    public int MainWindowWidth { get; set; }

    public int MainWindowHeight { get; set; }

    public int ZoomWindowLeft { get; set; } = int.MinValue;

    public int ZoomWindowTop { get; set; } = int.MinValue;

    public int ZoomWindowWidth { get; set; }

    public int ZoomWindowHeight { get; set; }

    public List<CapturePreset> Presets { get; set; } = CreateDefaultPresets();

    public static ApplicationSettings CreateDefault()
    {
        return new ApplicationSettings();
    }

    private static List<CapturePreset> CreateDefaultPresets()
    {
        return Enumerable.Range(1, 6)
            .Select(index => new CapturePreset { Name = $"プリセット{index}" })
            .ToList();
    }
}
