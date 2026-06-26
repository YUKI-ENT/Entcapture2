using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using ENTcapture2.Core.Models;

namespace ENTcapture2.WinForms.Imaging;

internal static class SnapshotProcessor
{
    public static Bitmap Process(
        Bitmap source,
        CapturePreset? preset,
        string patientId,
        string patientName,
        DateTime capturedAt)
    {
        Rectangle? cropArea = ParseCropArea(preset?.Roi, source.Size);
        Bitmap result = CreateLegacyRoiImage(source, cropArea);

        if (!string.IsNullOrWhiteSpace(preset?.OverlayText))
        {
            DrawLegacyOverlays(
                result,
                preset,
                patientId,
                patientName,
                capturedAt,
                Point.Empty);
        }
        else
        {
            DrawDefaultPatientOverlay(result, patientId, patientName);
        }

        return result;
    }

    private static Rectangle? ParseCropArea(string? roiText, Size imageSize)
    {
        if (string.IsNullOrWhiteSpace(roiText))
        {
            return null;
        }

        string[] parts = roiText.Split(
            ',',
            StringSplitOptions.TrimEntries);
        if (parts.Length != 4 ||
            !int.TryParse(parts[0], out int x1) ||
            !int.TryParse(parts[1], out int y1) ||
            !int.TryParse(parts[2], out int x2) ||
            !int.TryParse(parts[3], out int y2))
        {
            return null;
        }

        int left = Math.Clamp(Math.Min(x1, x2), 0, imageSize.Width - 1);
        int top = Math.Clamp(Math.Min(y1, y2), 0, imageSize.Height - 1);
        int right = Math.Clamp(Math.Max(x1, x2), left + 1, imageSize.Width);
        int bottom = Math.Clamp(Math.Max(y1, y2), top + 1, imageSize.Height);
        return Rectangle.FromLTRB(left, top, right, bottom);
    }

    private static Bitmap CreateLegacyRoiImage(
        Bitmap source,
        Rectangle? cropArea)
    {
        if (cropArea is null)
        {
            return source.Clone(
                new Rectangle(Point.Empty, source.Size),
                PixelFormat.Format24bppRgb);
        }

        var result = new Bitmap(
            source.Width,
            source.Height,
            PixelFormat.Format24bppRgb);
        using Graphics graphics = Graphics.FromImage(result);
        graphics.Clear(Color.White);
        graphics.DrawImage(
            source,
            cropArea.Value,
            cropArea.Value,
            GraphicsUnit.Pixel);
        return result;
    }

    private static void DrawLegacyOverlays(
        Bitmap image,
        CapturePreset preset,
        string patientId,
        string patientName,
        DateTime capturedAt,
        Point cropOffset)
    {
        string[] parts = preset.OverlayText.Split(',');
        if (parts.Length < 5)
        {
            return;
        }

        using Graphics graphics = Graphics.FromImage(image);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint =
            System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        for (int index = 0; index + 4 < parts.Length; index += 5)
        {
            if (!float.TryParse(
                    parts[index],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float x) ||
                !float.TryParse(
                    parts[index + 1],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float y) ||
                !float.TryParse(
                    parts[index + 2],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float fontSize))
            {
                continue;
            }

            string text = ExpandTokens(
                parts[index + 4],
                patientId,
                patientName,
                capturedAt);
            if (text.Length == 0)
            {
                continue;
            }

            using Font font = CreateFont(preset.FontName, fontSize);
            using var brush = new SolidBrush(ParseColor(parts[index + 3]));
            graphics.DrawString(
                text,
                font,
                brush,
                x - cropOffset.X,
                y - cropOffset.Y);
        }
    }

    private static Font CreateFont(string fontName, float fontSize)
    {
        string family = string.IsNullOrWhiteSpace(fontName)
            ? "Yu Gothic UI"
            : fontName.Trim();
        float size = Math.Clamp(fontSize, 6F, 256F);

        try
        {
            return new Font(
                family,
                size,
                FontStyle.Regular,
                GraphicsUnit.Pixel);
        }
        catch (ArgumentException)
        {
            return new Font(
                "Yu Gothic UI",
                size,
                FontStyle.Regular,
                GraphicsUnit.Pixel);
        }
    }

    private static Color ParseColor(string value)
    {
        string normalized = value.Trim();
        if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[2..];
        }

        if (!normalized.StartsWith('#'))
        {
            normalized = $"#{normalized}";
        }

        try
        {
            return ColorTranslator.FromHtml(normalized);
        }
        catch (Exception) when (
            normalized.Length > 0)
        {
            return Color.White;
        }
    }

    private static string ExpandTokens(
        string text,
        string patientId,
        string patientName,
        DateTime capturedAt)
    {
        return text
            .Replace("$d", capturedAt.ToString("yyyy/MM/dd"), StringComparison.Ordinal)
            .Replace("$t", capturedAt.ToString("HH:mm:ss"), StringComparison.Ordinal)
            .Replace("$i", patientId, StringComparison.Ordinal)
            .Replace("$n", patientName, StringComparison.Ordinal);
    }

    private static void DrawDefaultPatientOverlay(
        Bitmap image,
        string patientId,
        string patientName)
    {
        string text = string.Join(
            "  ",
            new[] { patientId.Trim(), patientName.Trim() }
                .Where(value => value.Length > 0));
        if (text.Length == 0)
        {
            return;
        }

        using Graphics graphics = Graphics.FromImage(image);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using Font font = new(
            "Yu Gothic UI",
            Math.Max(14, image.Width / 55F),
            FontStyle.Bold,
            GraphicsUnit.Pixel);
        SizeF textSize = graphics.MeasureString(text, font);
        float padding = Math.Max(10, image.Width / 100F);
        var background = new RectangleF(
            padding,
            image.Height - textSize.Height - padding * 2,
            textSize.Width + padding * 2,
            textSize.Height + padding);
        using var backgroundBrush = new SolidBrush(
            Color.FromArgb(155, 0, 0, 0));
        using var textBrush = new SolidBrush(Color.White);
        graphics.FillRectangle(backgroundBrush, background);
        graphics.DrawString(
            text,
            font,
            textBrush,
            background.Left + padding,
            background.Top + padding / 2);
    }
}
