using System.Runtime.InteropServices;
using DirectShowLib;

namespace ENTcapture2.WinForms.Capture;

public static class DirectShowDeviceDiagnostics
{
    public static string CreateReport(CameraDeviceInfo device)
    {
        if (device.IsMissing || string.IsNullOrWhiteSpace(device.MonikerString))
        {
            throw new InvalidOperationException(
                "選択中のDirectShowデバイスが見つかりません。");
        }

        object? sourceObject = null;
        IBaseFilter? sourceFilter = null;
        try
        {
            using (DsDevice dsDevice = FindDevice(device.MonikerString)
                ?? throw new InvalidOperationException(
                    "選択中のDirectShowデバイスが接続されていません。"))
            {
                Guid filterId = typeof(IBaseFilter).GUID;
                dsDevice.Mon.BindToObject(
                    null!,
                    null!,
                    ref filterId,
                    out sourceObject);
            }

            if (sourceObject is not IBaseFilter filter)
            {
                throw new InvalidOperationException(
                    "DirectShowフィルタを作成できませんでした。");
            }

            sourceFilter = filter;
            return BuildReport(device, sourceFilter);
        }
        finally
        {
            ReleaseComObject(sourceFilter);
            if (sourceObject is not null &&
                !ReferenceEquals(sourceObject, sourceFilter))
            {
                ReleaseComObject(sourceObject);
            }
        }
    }

    private static string BuildReport(CameraDeviceInfo device, IBaseFilter filter)
    {
        var lines = new List<string>
        {
            device.Name,
            string.Empty,
            "VideoProcAmp"
        };
        AddVideoProcAmpReport(lines, filter);
        lines.Add(string.Empty);
        lines.Add("CameraControl");
        AddCameraControlReport(lines, filter);
        lines.Add(string.Empty);
        lines.Add("Crossbar");
        AddCrossbarReport(lines, filter);
        return string.Join(Environment.NewLine, lines);
    }

    private static void AddVideoProcAmpReport(
        List<string> lines,
        IBaseFilter filter)
    {
        if (filter is not IAMVideoProcAmp videoProcAmp)
        {
            lines.Add("  非対応");
            return;
        }

        int count = 0;
        foreach (VideoProcAmpProperty property in Enum.GetValues<VideoProcAmpProperty>())
        {
            int result = videoProcAmp.GetRange(
                property,
                out int minimum,
                out int maximum,
                out int step,
                out int defaultValue,
                out VideoProcAmpFlags caps);
            if (result < 0)
            {
                continue;
            }

            int currentResult = videoProcAmp.Get(
                property,
                out int currentValue,
                out VideoProcAmpFlags currentFlags);
            string currentText = currentResult < 0
                ? string.Empty
                : $", current={currentValue} ({currentFlags})";
            lines.Add(
                $"  {property}: {minimum}..{maximum}, step={step}, default={defaultValue}, caps={caps}{currentText}");
            count++;
        }

        if (count == 0)
        {
            lines.Add("  標準APIで取得できる項目なし");
        }
    }

    private static void AddCameraControlReport(
        List<string> lines,
        IBaseFilter filter)
    {
        if (filter is not IAMCameraControl cameraControl)
        {
            lines.Add("  非対応");
            return;
        }

        int count = 0;
        foreach (CameraControlProperty property in Enum.GetValues<CameraControlProperty>())
        {
            int result = cameraControl.GetRange(
                property,
                out int minimum,
                out int maximum,
                out int step,
                out int defaultValue,
                out CameraControlFlags caps);
            if (result < 0)
            {
                continue;
            }

            int currentResult = cameraControl.Get(
                property,
                out int currentValue,
                out CameraControlFlags currentFlags);
            string currentText = currentResult < 0
                ? string.Empty
                : $", current={currentValue} ({currentFlags})";
            lines.Add(
                $"  {property}: {minimum}..{maximum}, step={step}, default={defaultValue}, caps={caps}{currentText}");
            count++;
        }

        if (count == 0)
        {
            lines.Add("  標準APIで取得できる項目なし");
        }
    }

    private static void AddCrossbarReport(List<string> lines, IBaseFilter filter)
    {
        if (filter is not IAMCrossbar crossbar)
        {
            lines.Add("  非対応");
            return;
        }

        int result = crossbar.get_PinCounts(
            out int outputPinCount,
            out int inputPinCount);
        if (result < 0)
        {
            lines.Add($"  ピン情報を取得できません: 0x{result:X8}");
            return;
        }

        lines.Add($"  inputs={inputPinCount}, outputs={outputPinCount}");
        for (int outputIndex = 0; outputIndex < outputPinCount; outputIndex++)
        {
            _ = crossbar.get_CrossbarPinInfo(
                false,
                outputIndex,
                out _,
                out PhysicalConnectorType outputType);
            _ = crossbar.get_IsRoutedTo(outputIndex, out int routedInputIndex);
            lines.Add(
                $"  output {outputIndex}: {outputType}, routedInput={routedInputIndex}");

            for (int inputIndex = 0; inputIndex < inputPinCount; inputIndex++)
            {
                if (crossbar.CanRoute(outputIndex, inputIndex) < 0)
                {
                    continue;
                }

                _ = crossbar.get_CrossbarPinInfo(
                    true,
                    inputIndex,
                    out _,
                    out PhysicalConnectorType inputType);
                lines.Add($"    input {inputIndex}: {inputType}");
            }
        }
    }

    private static DsDevice? FindDevice(string monikerString)
    {
        DsDevice[] devices =
            DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
        DsDevice? selected = null;
        foreach (DsDevice device in devices)
        {
            if (selected is null &&
                string.Equals(
                    device.DevicePath,
                    monikerString,
                    StringComparison.OrdinalIgnoreCase))
            {
                selected = device;
                continue;
            }

            device.Dispose();
        }

        return selected;
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.ReleaseComObject(value);
        }
    }
}
