using System.Drawing.Drawing2D;
using ENTcapture2.Core.Models;

namespace ENTcapture2.WinForms;

public sealed partial class PreviewZoomForm
{
    private ExternalPreviewSettings? _externalSettings;
    private ExternalPreviewSurface? _externalSurface;
    private Rectangle _normalBounds;

    public bool IsExternalDisplay => _externalSettings is not null;

    public void ConfigureExternalDisplay(ExternalPreviewSettings settings, Rectangle bounds)
    {
        if (!IsExternalDisplay) _normalBounds = Bounds;
        _externalSettings = settings.Clone();
        SuspendLayout();
        try
        {
            WindowState = FormWindowState.Normal;
            MinimumSize = Size.Empty;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            _playbackPanel.Visible = false;
            _zoomLabel.Visible = false;
            _pictureBox.Visible = false;
            _scrollPanel.AutoScroll = false;
            _scrollPanel.AutoScrollPosition = Point.Empty;
            if (_externalSurface is null)
            {
                _externalSurface = new ExternalPreviewSurface(this) { Dock = DockStyle.Fill };
                _scrollPanel.Controls.Add(_externalSurface);
            }
            _externalSurface.Visible = true;
            _externalSurface.BringToFront();
            if (Bounds != bounds) Bounds = bounds;
        }
        finally
        {
            ResumeLayout(true);
        }
        _externalSurface.Invalidate();
    }

    public void ConfigureNormalDisplay()
    {
        if (!IsExternalDisplay) return;
        _externalSettings = null;
        SuspendLayout();
        try
        {
            if (_externalSurface is not null) _externalSurface.Visible = false;
            WindowState = FormWindowState.Normal;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(320, 240);
            _playbackPanel.Visible = true;
            _zoomLabel.Visible = true;
            _pictureBox.Visible = true;
            _scrollPanel.AutoScroll = false;
            _scrollPanel.AutoScrollPosition = Point.Empty;
            if (!_normalBounds.IsEmpty) Bounds = _normalBounds;
            _isManualZoom = false;
        }
        finally
        {
            ResumeLayout(true);
        }
        FitImageToWindow();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        if (IsExternalDisplay) e.Cancel = true;
        base.OnDpiChanged(e);
    }

    private sealed class ExternalPreviewSurface : Panel
    {
        private readonly PreviewZoomForm _form;

        public ExternalPreviewSurface(PreviewZoomForm form)
        {
            _form = form;
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.Black;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_form._sourceImage is not { } image || _form._externalSettings is not { } settings)
                return;
            Rectangle destination = ExternalPreviewLayout.GetImageBounds(image.Size, ClientSize, settings);
            if (destination.IsEmpty) return;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.DrawImage(image, destination);
        }
    }
}
