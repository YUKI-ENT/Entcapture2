namespace ENTcapture2.Core.Models;

public sealed class DeviceControlPreset
{
    public bool ApplyOnPresetSelect { get; set; }

    public List<DeviceControlValue> VideoProcAmp { get; set; } = [];

    public List<DeviceControlValue> CameraControl { get; set; } = [];

    public DeviceControlPreset Clone()
    {
        return new DeviceControlPreset
        {
            ApplyOnPresetSelect = ApplyOnPresetSelect,
            VideoProcAmp = VideoProcAmp.Select(item => item.Clone()).ToList(),
            CameraControl = CameraControl.Select(item => item.Clone()).ToList()
        };
    }
}

public sealed class DeviceControlValue
{
    public string Property { get; set; } = string.Empty;

    public int Value { get; set; }

    public int Flags { get; set; }

    public DeviceControlValue Clone()
    {
        return new DeviceControlValue
        {
            Property = Property,
            Value = Value,
            Flags = Flags
        };
    }
}
