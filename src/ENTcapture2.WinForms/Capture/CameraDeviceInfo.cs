using ENTcapture2.Core.Models;

namespace ENTcapture2.WinForms.Capture;

public sealed record CameraDeviceInfo(
    int Index,
    string Name,
    string MonikerString,
    IReadOnlyList<CaptureResolution> Resolutions)
{
    public string DeviceId => MonikerString;

    public override string ToString() => Name;
}
