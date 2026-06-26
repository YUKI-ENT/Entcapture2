using System.Runtime.InteropServices;
using DirectShowLib;
using ENTcapture2.Core.Models;

namespace ENTcapture2.WinForms.Capture;

public static class DirectShowDeviceCatalog
{
    private const long ReferenceTimeUnitsPerSecond = 10_000_000;

    public static Task<IReadOnlyList<CameraDeviceInfo>> DiscoverAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<CameraDeviceInfo>>(
            () => Discover(cancellationToken),
            cancellationToken);
    }

    public static string Describe(IReadOnlyList<CameraDeviceInfo> devices)
    {
        return string.Join(
            Environment.NewLine,
            devices.SelectMany(
                device =>
                    new[]
                    {
                        $"[{device.Index}] {device.Name}",
                        $"  Moniker: {device.MonikerString}"
                    }.Concat(
                        device.Resolutions.Select(
                            resolution => $"  {resolution}"))));
    }

    private static IReadOnlyList<CameraDeviceInfo> Discover(
        CancellationToken cancellationToken)
    {
        DsDevice[] devices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
        var result = new List<CameraDeviceInfo>(devices.Length);

        try
        {
            for (int index = 0; index < devices.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                DsDevice device = devices[index];
                IReadOnlyList<CaptureResolution> resolutions =
                    GetResolutions(device);

                result.Add(
                    new CameraDeviceInfo(
                        index,
                        device.Name,
                        device.DevicePath ?? string.Empty,
                        resolutions));
            }
        }
        finally
        {
            foreach (DsDevice device in devices)
            {
                device.Dispose();
            }
        }

        return result;
    }

    private static IReadOnlyList<CaptureResolution> GetResolutions(DsDevice device)
    {
        object? sourceObject = null;
        IBaseFilter? sourceFilter = null;
        IPin? capturePin = null;
        IntPtr capabilitiesPointer = IntPtr.Zero;
        var resolutions = new List<CaptureResolution>();

        try
        {
            Guid filterId = typeof(IBaseFilter).GUID;
            device.Mon.BindToObject(
                null!,
                null!,
                ref filterId,
                out sourceObject);

            if (sourceObject is not IBaseFilter filter)
            {
                return resolutions;
            }

            sourceFilter = filter;
            capturePin =
                DsFindPin.ByCategory(sourceFilter, PinCategory.Capture, 0)
                ?? DsFindPin.ByDirection(sourceFilter, PinDirection.Output, 0);

            if (capturePin is not IAMStreamConfig streamConfig)
            {
                return resolutions;
            }

            int result = streamConfig.GetNumberOfCapabilities(
                out int capabilityCount,
                out int capabilitySize);
            DsError.ThrowExceptionForHR(result);

            int expectedSize = Marshal.SizeOf<VideoStreamConfigCaps>();
            if (capabilitySize != expectedSize)
            {
                return resolutions;
            }

            capabilitiesPointer = Marshal.AllocCoTaskMem(capabilitySize);
            for (int capabilityIndex = 0;
                 capabilityIndex < capabilityCount;
                 capabilityIndex++)
            {
                AMMediaType? mediaType = null;
                try
                {
                    result = streamConfig.GetStreamCaps(
                        capabilityIndex,
                        out mediaType,
                        capabilitiesPointer);
                    if (result < 0 || mediaType?.formatPtr == IntPtr.Zero)
                    {
                        continue;
                    }

                    CaptureResolution? resolution = ReadResolution(mediaType!);
                    if (resolution is not null &&
                        !resolutions.Contains(resolution))
                    {
                        resolutions.Add(resolution);
                    }
                }
                finally
                {
                    if (mediaType is not null)
                    {
                        DsUtils.FreeAMMediaType(mediaType);
                    }
                }
            }
        }
        catch (COMException)
        {
            // Some capture filters expose no IAMStreamConfig capability list.
        }
        catch (InvalidCastException)
        {
            // Some video input monikers do not expose IBaseFilter even though
            // they are returned by the DirectShow video input category.
        }
        finally
        {
            if (capabilitiesPointer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(capabilitiesPointer);
            }

            ReleaseComObject(capturePin);
            ReleaseComObject(sourceFilter);
            if (sourceObject is not null &&
                !ReferenceEquals(sourceObject, sourceFilter))
            {
                ReleaseComObject(sourceObject);
            }
        }

        return resolutions
            .OrderBy(item => item.Width * item.Height)
            .ThenByDescending(item => item.FramesPerSecond)
            .ThenBy(item => GetPixelFormatPriority(item.PixelFormat))
            .ToArray();
    }

    private static int GetPixelFormatPriority(string pixelFormat)
    {
        string normalized = pixelFormat.Trim().ToUpperInvariant();
        return normalized switch
        {
            "MJPG" or "MJPEG" => 0,
            "H264" or "H265" or "HEVC" => 1,
            "YUY2" or "YUYV" or "UYVY" => 2,
            "RGB24" or "RGB32" or "BGR3" or "BGR4" => 3,
            _ => 4
        };
    }

    private static CaptureResolution? ReadResolution(AMMediaType mediaType)
    {
        int width;
        int height;
        long averageTimePerFrame;

        if (mediaType.formatType == FormatType.VideoInfo)
        {
            var videoInfo =
                Marshal.PtrToStructure<VideoInfoHeader>(mediaType.formatPtr)!;
            width = videoInfo.BmiHeader.Width;
            height = Math.Abs(videoInfo.BmiHeader.Height);
            averageTimePerFrame = videoInfo.AvgTimePerFrame;
        }
        else if (mediaType.formatType == FormatType.VideoInfo2)
        {
            var videoInfo =
                Marshal.PtrToStructure<VideoInfoHeader2>(mediaType.formatPtr)!;
            width = videoInfo.BmiHeader.Width;
            height = Math.Abs(videoInfo.BmiHeader.Height);
            averageTimePerFrame = videoInfo.AvgTimePerFrame;
        }
        else
        {
            return null;
        }

        if (width <= 0 || height <= 0)
        {
            return null;
        }

        int framesPerSecond = averageTimePerFrame > 0
            ? Math.Max(
                1,
                (int)Math.Round(
                    ReferenceTimeUnitsPerSecond /
                    (double)averageTimePerFrame))
            : 0;

        string pixelFormat = GetPixelFormat(mediaType.subType);
        return new CaptureResolution(
            width,
            height,
            framesPerSecond,
            pixelFormat);
    }

    private static string GetPixelFormat(Guid mediaSubType)
    {
        string name = DsToString.MediaSubTypeToString(mediaSubType);
        if (!string.IsNullOrWhiteSpace(name) &&
            !name.StartsWith("Unknown", StringComparison.OrdinalIgnoreCase))
        {
            const string prefix = "MEDIASUBTYPE_";
            return name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? name[prefix.Length..]
                : name;
        }

        byte[] bytes = mediaSubType.ToByteArray();
        string fourCc = new(
            bytes.Take(4)
                .Select(value => (char)value)
                .ToArray());

        return fourCc.All(character => character is >= ' ' and <= '~')
            ? fourCc
            : mediaSubType.ToString("D");
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.ReleaseComObject(value);
        }
    }
}
