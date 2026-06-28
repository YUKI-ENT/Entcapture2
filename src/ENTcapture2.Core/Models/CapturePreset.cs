namespace ENTcapture2.Core.Models;

public sealed class CapturePreset
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "新しいプリセット";

    public string DeviceId { get; set; } = string.Empty;

    public string DeviceName { get; set; } = string.Empty;

    public int DeviceIndex { get; set; }

    public CaptureResolution Resolution { get; set; } = CaptureResolution.Default;

    public int LegacyResolutionIndex { get; set; } = -1;

    public bool IsVideo { get; set; }

    public bool PreviewOnly { get; set; }

    public string ExaminationType { get; set; } = string.Empty;

    public string Roi { get; set; } = string.Empty;

    public string OverlayText { get; set; } = string.Empty;

    public string FontName { get; set; } = string.Empty;

    public int FramesPerSecond { get; set; }

    public int WhiteBalanceRed { get; set; } = 255;

    public int WhiteBalanceGreen { get; set; } = 255;

    public int WhiteBalanceBlue { get; set; } = 255;

    public double Gamma { get; set; } = 1.0;

    public bool FlipHorizontal { get; set; }

    public bool FlipVertical { get; set; }

    public bool SimpleNbi { get; set; }

    public CapturePreset Clone()
    {
        return new CapturePreset
        {
            Id = Guid.NewGuid(),
            Name = $"{Name} のコピー",
            DeviceId = DeviceId,
            DeviceName = DeviceName,
            DeviceIndex = DeviceIndex,
            Resolution = Resolution with { },
            LegacyResolutionIndex = LegacyResolutionIndex,
            IsVideo = IsVideo,
            PreviewOnly = PreviewOnly,
            ExaminationType = ExaminationType,
            Roi = Roi,
            OverlayText = OverlayText,
            FontName = FontName,
            FramesPerSecond = FramesPerSecond,
            WhiteBalanceRed = WhiteBalanceRed,
            WhiteBalanceGreen = WhiteBalanceGreen,
            WhiteBalanceBlue = WhiteBalanceBlue,
            Gamma = Gamma,
            FlipHorizontal = FlipHorizontal,
            FlipVertical = FlipVertical,
            SimpleNbi = SimpleNbi
        };
    }

    public override string ToString() => Name;
}
