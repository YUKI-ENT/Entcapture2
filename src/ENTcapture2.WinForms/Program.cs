using ENTcapture2.WinForms.Capture;
using ENTcapture2.Core.Services;

namespace ENTcapture2.WinForms;

internal static class Program
{
    private const string SingleInstanceMutexName =
        @"Local\ENTcapture2.WinForms.SingleInstance";

    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length == 2 &&
            string.Equals(
                args[0],
                "--list-devices",
                StringComparison.OrdinalIgnoreCase))
        {
            IReadOnlyList<CameraDeviceInfo> devices =
                DirectShowDeviceCatalog
                    .DiscoverAsync()
                    .GetAwaiter()
                    .GetResult();
            File.WriteAllText(
                args[1],
                DirectShowDeviceCatalog.Describe(devices));
            return;
        }

        using var mutex = new Mutex(
            false,
            SingleInstanceMutexName,
            out bool createdNew);
        if (!createdNew)
        {
            return;
        }

        try
        {
            ApplicationConfiguration.Initialize();
            Application.SetColorMode(SystemColorMode.Dark);

            ISettingsStore settingsStore = new JsonSettingsStore();
            Application.Run(new MainForm(settingsStore));
        }
        finally
        {
            DebugLogger.Shutdown();
        }
    }
}
