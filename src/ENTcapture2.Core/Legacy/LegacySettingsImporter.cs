using System.Globalization;
using System.Xml.Linq;
using ENTcapture2.Core.Models;

namespace ENTcapture2.Core.Legacy;

public static class LegacySettingsImporter
{
    public static LegacySettingsImportResult Import(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        XDocument document = XDocument.Load(filePath, LoadOptions.None);
        Dictionary<string, string> values = document
            .Descendants()
            .Where(element => element.Name.LocalName == "setting")
            .Select(
                setting => new
                {
                    Name = (string?)setting.Attribute("name"),
                    Value = setting.Elements()
                        .FirstOrDefault(element => element.Name.LocalName == "value")
                        ?.Value
                        ?? string.Empty
                })
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .GroupBy(item => item.Name!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Last().Value,
                StringComparer.OrdinalIgnoreCase);

        if (values.Count == 0)
        {
            throw new InvalidDataException(
                "旧ENTcaptureのsetting要素が見つかりませんでした。");
        }

        var settings = ApplicationSettings.CreateDefault();
        settings.Presets.Clear();
        settings.SnapshotDirectory = Get(values, "outdir");
        if (string.IsNullOrWhiteSpace(settings.SnapshotDirectory))
        {
            settings.SnapshotDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                "ENTcapture2");
        }

        settings.JpegQuality = GetInt(values, "jpegquality", 95);
        settings.TemporaryRecordingDirectory = Get(values, "tmpdir");
        if (string.IsNullOrWhiteSpace(settings.TemporaryRecordingDirectory))
        {
            settings.TemporaryRecordingDirectory =
                Path.Combine(Path.GetTempPath(), "ENTcapture2");
        }

        settings.TemporaryRecordingCodec = Get(values, "codec");
        if (string.IsNullOrWhiteSpace(settings.TemporaryRecordingCodec))
        {
            settings.TemporaryRecordingCodec = "MJPG";
        }
        settings.TemporaryRecordingCodec =
            NormalizeLegacyCodec(settings.TemporaryRecordingCodec);

        bool previewOnly = GetBool(values, "norec");
        settings.CaptureMode = CaptureOperationMode.ContinuousTemporaryRecording;
        settings.RecordingSegmentMinutes =
            Math.Max(1, GetInt(values, "timeout", 10));
        settings.TemporaryFileRetentionDays =
            Math.Max(0, GetInt(values, "autodelete", 0));
        settings.MaximumPreviewFramesPerSecond =
            Math.Max(0, GetInt(values, "maxfps", 0));
        settings.ReencodeThresholdMegabytes =
            Math.Max(0, GetInt(values, "encodesize", 0));
        settings.FinalVideoCodec = Get(values, "recodec");
        if (string.IsNullOrWhiteSpace(settings.FinalVideoCodec))
        {
            settings.FinalVideoCodec = "AUTO";
        }
        settings.FinalVideoBitrateKbps =
            Math.Max(500, GetInt(values, "rebitrate", 4000));
        settings.FinalVideoQuality =
            settings.FinalVideoBitrateKbps switch
            {
                <= 3000 => "Size",
                <= 8000 => "Standard",
                <= 14000 => "High",
                _ => "Highest"
            };
        settings.ExaminationTypes = Get(values, "tests");
        settings.RsBaseTheptDirectory = Get(values, "thept");
        settings.RsBaseReloadUrl =
            FirstNonEmpty(
                Get(values, "reloadurl"),
                Get(values, "reload"),
                Get(values, "rsbaseurl"));
        settings.AutoFileToRsBase = GetBool(values, "autofiling");
        DecodeLegacyHotkeys(
            GetInt(values, "snapkey", 0),
            settings.SnapshotHotkey,
            settings.CaptureHotkey);
        var result = new LegacySettingsImportResult { Settings = settings };
        Dictionary<string, LegacyFilter> filters = ParseFilters(
            Get(values, "filter"),
            result.Warnings);

        for (int index = 1; index <= 6; index++)
        {
            string name = Get(values, $"name{index}");
            string deviceName = Get(values, $"device{index}");
            var preset = new CapturePreset
            {
                Name = string.IsNullOrWhiteSpace(name)
                    ? $"プリセット{index}"
                    : name.Trim(),
                DeviceName = deviceName.Trim(),
                DeviceIndex = Math.Max(0, index - 1),
                LegacyResolutionIndex = GetInt(values, $"reso{index}", -1),
                IsVideo = GetBool(values, $"video{index}"),
                PreviewOnly = previewOnly,
                ExaminationType = Get(values, $"test{index}"),
                Roi = Get(values, $"roi{index}"),
                OverlayText = Get(values, $"string{index}"),
                FontName = Get(values, $"font{index}"),
                FramesPerSecond = GetInt(values, $"FPS{index}", 0)
            };
            if (preset.IsVideo)
            {
                preset.PreviewOnly = false;
            }

            ApplyFilter(preset, filters);
            ValidateLegacyDrawingValues(preset, index, result.Warnings);
            settings.Presets.Add(preset);
        }

        settings.SelectedPresetId = settings.Presets.FirstOrDefault()?.Id;
        return result;
    }

    private static Dictionary<string, LegacyFilter> ParseFilters(
        string filterText,
        ICollection<string> warnings)
    {
        var filters = new Dictionary<string, LegacyFilter>(
            StringComparer.OrdinalIgnoreCase);

        foreach (string entry in filterText.Split(
                     ';',
                     StringSplitOptions.RemoveEmptyEntries |
                     StringSplitOptions.TrimEntries))
        {
            string[] parts = entry.Split(',');
            if (parts.Length < 5 ||
                !int.TryParse(parts[1], out int red) ||
                !int.TryParse(parts[2], out int green) ||
                !int.TryParse(parts[3], out int blue) ||
                !int.TryParse(parts[4], out int gammaTimesTen))
            {
                warnings.Add($"フィルター設定を解釈できませんでした: {entry}");
                continue;
            }

            filters[parts[0]] = new LegacyFilter(
                red,
                green,
                blue,
                gammaTimesTen / 10.0);
        }

        return filters;
    }

    private static void ApplyFilter(
        CapturePreset preset,
        IReadOnlyDictionary<string, LegacyFilter> filters)
    {
        string normalizedDeviceName = Normalize(preset.DeviceName);
        if (normalizedDeviceName.Length == 0)
        {
            return;
        }

        KeyValuePair<string, LegacyFilter>? match = filters
            .FirstOrDefault(
                pair =>
                {
                    string normalizedFilterName = Normalize(pair.Key);
                    return normalizedFilterName.Length > 0 &&
                        (normalizedDeviceName.Contains(
                            normalizedFilterName,
                            StringComparison.OrdinalIgnoreCase) ||
                         normalizedFilterName.Contains(
                            normalizedDeviceName,
                            StringComparison.OrdinalIgnoreCase));
                });

        if (match is not { } selected ||
            string.IsNullOrWhiteSpace(selected.Key))
        {
            return;
        }

        preset.WhiteBalanceRed = selected.Value.Red;
        preset.WhiteBalanceGreen = selected.Value.Green;
        preset.WhiteBalanceBlue = selected.Value.Blue;
        preset.Gamma = selected.Value.Gamma;
    }

    private static void ValidateLegacyDrawingValues(
        CapturePreset preset,
        int index,
        ICollection<string> warnings)
    {
        if (!string.IsNullOrWhiteSpace(preset.Roi) &&
            preset.Roi.Split(',').Length != 4)
        {
            warnings.Add($"プリセット{index}: ROIの形式が不正です。");
        }

        if (!string.IsNullOrWhiteSpace(preset.OverlayText) &&
            preset.OverlayText.Split(',').Length % 5 != 0)
        {
            warnings.Add($"プリセット{index}: 文字列設定の形式が不正です。");
        }
    }

    private static string Get(
        IReadOnlyDictionary<string, string> values,
        string name)
    {
        return values.TryGetValue(name, out string? value)
            ? value
            : string.Empty;
    }

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(
            value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }

    private static int GetInt(
        IReadOnlyDictionary<string, string> values,
        string name,
        int fallback)
    {
        return int.TryParse(
            Get(values, name),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int value)
            ? value
            : fallback;
    }

    private static bool GetBool(
        IReadOnlyDictionary<string, string> values,
        string name)
    {
        string value = Get(values, name).Trim();
        if (bool.TryParse(value, out bool boolValue))
        {
            return boolValue;
        }

        return value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("on", StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string value)
    {
        return value.Replace(",", string.Empty)
            .Replace(";", string.Empty)
            .Trim();
    }

    private static string NormalizeLegacyCodec(string value)
    {
        return value.Trim().ToUpperInvariant() switch
        {
            "RAW" => "RAW",
            "H264" or "AVC1" => "H264",
            _ => "MJPG"
        };
    }

    private static void DecodeLegacyHotkeys(
        int packed,
        HotkeySettings snapshot,
        HotkeySettings capture)
    {
        int snapshotValue = packed % 100000;
        snapshot.Control = snapshotValue / 10000 > 0;
        snapshot.Shift = snapshotValue % 10000 / 1000 > 0;
        snapshot.KeyCode = snapshotValue % 1000;

        int captureValue = packed / 100000;
        capture.Control = captureValue / 10000 > 0;
        capture.Shift = captureValue % 10000 / 1000 > 0;
        capture.KeyCode = captureValue % 1000;
    }

    private sealed record LegacyFilter(
        int Red,
        int Green,
        int Blue,
        double Gamma);
}
