namespace ENTcapture2.Core.Models;

public sealed class HotkeySettings
{
    public bool Control { get; set; } = true;

    public bool Shift { get; set; } = true;

    public int KeyCode { get; set; }

    public HotkeySettings Clone()
    {
        return new HotkeySettings
        {
            Control = Control,
            Shift = Shift,
            KeyCode = KeyCode
        };
    }
}
