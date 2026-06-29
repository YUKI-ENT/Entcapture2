using ENTcapture2.Core.Models;

namespace ENTcapture2.WinForms.Capture;

public sealed record CameraDeviceInfo(
    int Index,
    string Name,
    string MonikerString,
    IReadOnlyList<CaptureResolution> Resolutions,
    bool IsMissing = false)
{
    public string DeviceId => MonikerString;

    public string DisplayName => IsMissing ? $"{Name} (デバイスなし)" : Name;

    public override string ToString() =>
        DisplayName;
}
