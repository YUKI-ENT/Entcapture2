namespace ENTcapture2.Core.Models;

public sealed record CaptureResolution(
    int Width,
    int Height,
    int FramesPerSecond,
    string PixelFormat = "")
{
    public static CaptureResolution Default { get; } = new(0, 0, 0, "");

    public override string ToString()
    {
        if (Width <= 0 || Height <= 0)
        {
            return "自動";
        }

        string resolutionText = FramesPerSecond > 0
            ? $"{Width} x {Height} / {FramesPerSecond} fps"
            : $"{Width} x {Height}";

        return string.IsNullOrWhiteSpace(PixelFormat)
            ? resolutionText
            : $"{resolutionText} / {PixelFormat}";
    }
}
