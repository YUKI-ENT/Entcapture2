using System.Runtime.InteropServices;
using DirectShowLib;

namespace ENTcapture2.WinForms.Capture;

public static class DirectShowDevicePropertyDialog
{
    public static void Show(IWin32Window owner, CameraDeviceInfo device)
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
            ShowPropertyPages(owner.Handle, device.Name, sourceFilter);
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

    private static void ShowPropertyPages(
        IntPtr ownerHandle,
        string caption,
        object target)
    {
        if (target is not ISpecifyPropertyPages propertyPages)
        {
            throw new InvalidOperationException(
                "このDirectShowデバイスはプロパティページを公開していません。");
        }

        var pages = new DsCAUUID();
        int result = propertyPages.GetPages(out pages);
        DsError.ThrowExceptionForHR(result);
        try
        {
            if (pages.cElems <= 0 || pages.pElems == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    "このDirectShowデバイスには表示できるプロパティページがありません。");
            }

            object targetObject = target;
            result = OleCreatePropertyFrame(
                ownerHandle,
                0,
                0,
                caption,
                1,
                ref targetObject,
                pages.cElems,
                pages.pElems,
                0,
                0,
                IntPtr.Zero);
            DsError.ThrowExceptionForHR(result);
        }
        finally
        {
            if (pages.pElems != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(pages.pElems);
            }
        }
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.ReleaseComObject(value);
        }
    }

    [DllImport("oleaut32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int OleCreatePropertyFrame(
        IntPtr hwndOwner,
        int x,
        int y,
        string lpszCaption,
        int cObjects,
        [MarshalAs(UnmanagedType.Interface)] ref object ppUnk,
        int cPages,
        IntPtr pPageClsID,
        int lcid,
        int dwReserved,
        IntPtr pvReserved);
}
