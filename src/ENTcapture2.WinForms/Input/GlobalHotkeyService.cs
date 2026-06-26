using System.Diagnostics;
using System.Runtime.InteropServices;
using ENTcapture2.Core.Models;

namespace ENTcapture2.WinForms.Input;

public sealed class GlobalHotkeyService : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;

    private readonly HookProcedure _hookProcedure;
    private readonly HashSet<int> _pressedKeys = [];
    private nint _hook;
    private HotkeySettings _snapshot = new();
    private HotkeySettings _capture = new();

    public GlobalHotkeyService()
    {
        _hookProcedure = HookCallback;
    }

    public event Action? SnapshotPressed;

    public event Action? CapturePressed;

    public void Start(
        HotkeySettings snapshot,
        HotkeySettings capture)
    {
        Update(snapshot, capture);
        if (_hook != 0)
        {
            return;
        }

        using Process process = Process.GetCurrentProcess();
        using ProcessModule? module = process.MainModule;
        nint moduleHandle = module is null
            ? 0
            : GetModuleHandle(module.ModuleName);
        _hook = SetWindowsHookEx(
            WhKeyboardLl,
            _hookProcedure,
            moduleHandle,
            0);
        if (_hook == 0)
        {
            throw new InvalidOperationException(
                "グローバルキーボードフックを開始できませんでした。");
        }
    }

    public void Update(
        HotkeySettings snapshot,
        HotkeySettings capture)
    {
        _snapshot = snapshot.Clone();
        _capture = capture.Clone();
    }

    public void Dispose()
    {
        if (_hook != 0)
        {
            UnhookWindowsHookEx(_hook);
            _hook = 0;
        }
    }

    private nint HookCallback(
        int code,
        nint wParam,
        nint lParam)
    {
        if (code >= 0)
        {
            int message = unchecked((int)wParam);
            int keyCode = Marshal.ReadInt32(lParam);
            if (message is WmKeyUp or WmSysKeyUp)
            {
                _pressedKeys.Remove(keyCode);
            }
            else if (message is WmKeyDown or WmSysKeyDown &&
                     _pressedKeys.Add(keyCode))
            {
                bool control =
                    (GetAsyncKeyState((int)Keys.ControlKey) & 0x8000) != 0;
                bool shift =
                    (GetAsyncKeyState((int)Keys.ShiftKey) & 0x8000) != 0;

                if (Matches(_snapshot, keyCode, control, shift))
                {
                    SnapshotPressed?.Invoke();
                }
                else if (Matches(_capture, keyCode, control, shift))
                {
                    CapturePressed?.Invoke();
                }
            }
        }

        return CallNextHookEx(_hook, code, wParam, lParam);
    }

    private static bool Matches(
        HotkeySettings settings,
        int keyCode,
        bool control,
        bool shift)
    {
        return settings.KeyCode > 0 &&
            settings.KeyCode == keyCode &&
            settings.Control == control &&
            settings.Shift == shift;
    }

    private delegate nint HookProcedure(
        int code,
        nint wParam,
        nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(
        int hookId,
        HookProcedure procedure,
        nint moduleHandle,
        uint threadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(nint hook);

    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(
        nint hook,
        int code,
        nint wParam,
        nint lParam);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandle(string? moduleName);
}
