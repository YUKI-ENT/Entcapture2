namespace ENTcapture2.Core.Models;

public enum ExternalPreviewMode
{
    Zoom,
    Fit
}

public enum ExternalPreviewAlignment
{
    Left,
    Center,
    Right
}

public sealed class ExternalPreviewSettings
{
    public bool Enabled { get; set; } = true;
    // Empty selects a monitor other than the main form's monitor.
    public string MonitorDeviceName { get; set; } = string.Empty;
    // Coordinates are relative to the selected monitor, in screen pixels.
    public int Left { get; set; }
    public int Top { get; set; }
    public int Width { get; set; } = 1280;
    public int Height { get; set; } = 1024;
    public ExternalPreviewMode Mode { get; set; } = ExternalPreviewMode.Zoom;
    public double Magnification { get; set; } = 1.0;
    public ExternalPreviewAlignment Alignment { get; set; } = ExternalPreviewAlignment.Left;
    public int OffsetX { get; set; }
    public int OffsetY { get; set; }

    public ExternalPreviewSettings Clone() => (ExternalPreviewSettings)MemberwiseClone();
}
