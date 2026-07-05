using System.Runtime.InteropServices;
using DirectShowLib;
using ENTcapture2.Core.Models;

namespace ENTcapture2.WinForms.Capture;

public static class DirectShowDeviceControlService
{
    public static DeviceControlPreset CaptureCurrent(CameraDeviceInfo device)
    {
        using BoundDevice bound = BoundDevice.Open(device);
        var preset = new DeviceControlPreset();
        CaptureVideoProcAmp(bound.Filter, preset);
        CaptureCameraControl(bound.Filter, preset);
        return preset;
    }

    public static void Apply(CameraDeviceInfo device, DeviceControlPreset controls)
    {
        if (!controls.ApplyOnPresetSelect)
        {
            return;
        }

        using BoundDevice bound = BoundDevice.Open(device);
        ApplyVideoProcAmp(bound.Filter, controls.VideoProcAmp);
        ApplyCameraControl(bound.Filter, controls.CameraControl);
    }

    public static string Describe(DeviceControlPreset controls)
    {
        int count = controls.VideoProcAmp.Count + controls.CameraControl.Count;
        if (count == 0)
        {
            return "記録なし";
        }

        return $"{count}項目を記録済み";
    }

    private static void CaptureVideoProcAmp(
        IBaseFilter filter,
        DeviceControlPreset preset)
    {
        if (filter is not IAMVideoProcAmp videoProcAmp)
        {
            return;
        }

        foreach (VideoProcAmpProperty property in Enum.GetValues<VideoProcAmpProperty>())
        {
            if (videoProcAmp.GetRange(
                    property,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _) < 0 ||
                videoProcAmp.Get(
                    property,
                    out int value,
                    out VideoProcAmpFlags flags) < 0)
            {
                continue;
            }

            preset.VideoProcAmp.Add(
                new DeviceControlValue
                {
                    Property = property.ToString(),
                    Value = value,
                    Flags = (int)flags
                });
        }
    }

    private static void CaptureCameraControl(
        IBaseFilter filter,
        DeviceControlPreset preset)
    {
        if (filter is not IAMCameraControl cameraControl)
        {
            return;
        }

        foreach (CameraControlProperty property in Enum.GetValues<CameraControlProperty>())
        {
            if (cameraControl.GetRange(
                    property,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _) < 0 ||
                cameraControl.Get(
                    property,
                    out int value,
                    out CameraControlFlags flags) < 0)
            {
                continue;
            }

            preset.CameraControl.Add(
                new DeviceControlValue
                {
                    Property = property.ToString(),
                    Value = value,
                    Flags = (int)flags
                });
        }
    }

    private static void ApplyVideoProcAmp(
        IBaseFilter filter,
        IEnumerable<DeviceControlValue> settings)
    {
        if (filter is not IAMVideoProcAmp videoProcAmp)
        {
            return;
        }

        foreach (DeviceControlValue setting in settings)
        {
            if (!Enum.TryParse(
                    setting.Property,
                    ignoreCase: true,
                    out VideoProcAmpProperty property))
            {
                continue;
            }

            int result = videoProcAmp.Set(
                property,
                setting.Value,
                (VideoProcAmpFlags)setting.Flags);
            DsError.ThrowExceptionForHR(result);
        }
    }

    private static void ApplyCameraControl(
        IBaseFilter filter,
        IEnumerable<DeviceControlValue> settings)
    {
        if (filter is not IAMCameraControl cameraControl)
        {
            return;
        }

        foreach (DeviceControlValue setting in settings)
        {
            if (!Enum.TryParse(
                    setting.Property,
                    ignoreCase: true,
                    out CameraControlProperty property))
            {
                continue;
            }

            int result = cameraControl.Set(
                property,
                setting.Value,
                (CameraControlFlags)setting.Flags);
            DsError.ThrowExceptionForHR(result);
        }
    }

    private sealed class BoundDevice : IDisposable
    {
        private object? _sourceObject;
        private IBaseFilter? _sourceFilter;

        private BoundDevice(object sourceObject, IBaseFilter sourceFilter)
        {
            _sourceObject = sourceObject;
            _sourceFilter = sourceFilter;
        }

        public IBaseFilter Filter => _sourceFilter
            ?? throw new ObjectDisposedException(nameof(BoundDevice));

        public static BoundDevice Open(CameraDeviceInfo device)
        {
            if (device.IsMissing || string.IsNullOrWhiteSpace(device.MonikerString))
            {
                throw new InvalidOperationException(
                    "選択中のDirectShowデバイスが見つかりません。");
            }

            object? sourceObject = null;
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

            if (sourceObject is not IBaseFilter sourceFilter)
            {
                ReleaseComObject(sourceObject);
                throw new InvalidOperationException(
                    "DirectShowフィルタを作成できませんでした。");
            }

            return new BoundDevice(sourceObject, sourceFilter);
        }

        public void Dispose()
        {
            IBaseFilter? sourceFilter = _sourceFilter;
            object? sourceObject = _sourceObject;
            _sourceFilter = null;
            _sourceObject = null;
            ReleaseComObject(sourceFilter);
            if (sourceObject is not null &&
                !ReferenceEquals(sourceObject, sourceFilter))
            {
                ReleaseComObject(sourceObject);
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
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.ReleaseComObject(value);
        }
    }
}
