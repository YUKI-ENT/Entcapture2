using ENTcapture2.Core.Models;

namespace ENTcapture2.WinForms;

internal static class ExternalPreviewLayout
{
    public static Rectangle GetWindowBounds(Rectangle monitor, ExternalPreviewSettings settings)
    {
        int width = Math.Clamp(settings.Width, 1, monitor.Width);
        int height = Math.Clamp(settings.Height, 1, monitor.Height);
        return new Rectangle(
            monitor.Left + Math.Clamp(settings.Left, 0, monitor.Width - width),
            monitor.Top + Math.Clamp(settings.Top, 0, monitor.Height - height),
            width, height);
    }

    public static Rectangle GetImageBounds(Size image, Size viewport, ExternalPreviewSettings settings)
    {
        if (image.Width <= 0 || image.Height <= 0 || viewport.Width <= 0 || viewport.Height <= 0)
        {
            return Rectangle.Empty;
        }

        double scaleX = viewport.Width / (double)image.Width;
        double scaleY = viewport.Height / (double)image.Height;
        double scale = settings.Mode == ExternalPreviewMode.Fit
            ? Math.Min(scaleX, scaleY) : Math.Max(scaleX, scaleY);
        double magnification = double.IsFinite(settings.Magnification)
            ? Math.Clamp(settings.Magnification, 0.1, 4.0) : 1.0;
        int width = Math.Max(1, (int)Math.Round(image.Width * scale * magnification));
        int height = Math.Max(1, (int)Math.Round(image.Height * scale * magnification));
        int left = settings.Alignment switch
        {
            ExternalPreviewAlignment.Center => (viewport.Width - width) / 2,
            ExternalPreviewAlignment.Right => viewport.Width - width,
            _ => 0
        };
        return new Rectangle(
            left + Math.Clamp(settings.OffsetX, -16384, 16384),
            (viewport.Height - height) / 2 + Math.Clamp(settings.OffsetY, -16384, 16384),
            width, height);
    }
}
