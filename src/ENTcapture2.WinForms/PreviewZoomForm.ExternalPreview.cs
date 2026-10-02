using System.Drawing.Drawing2D;
using ENTcapture2.Core.Models;

namespace ENTcapture2.WinForms;

public sealed partial class PreviewZoomForm
{
    private ExternalPreviewSettings? _externalSettings;
    private ExternalPreviewSurface? _externalSurface;
    private Rectangle _normalBounds;
    private bool _normalShowInTaskbar;

    public bool IsExternalDisplay => _externalSettings is not null;

    protected override bool ShowWithoutActivation => IsExternalDisplay;

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams parameters = base.CreateParams;
            if (IsExternalDisplay)
            {
                // Keep the display-only window out of taskbar thumbnails and Alt+Tab.
                parameters.ExStyle |= 0x08000000 | 0x00000080; // WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW
                parameters.ExStyle &= ~0x00040000; // WS_EX_APPWINDOW
            }
            return parameters;
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (IsExternalDisplay && m.Msg == 0x0021) // WM_MOUSEACTIVATE
        {
            m.Result = (IntPtr)3; // MA_NOACTIVATE
            return;
        }
        base.WndProc(ref m);
    }

    public void ConfigureExternalDisplay(ExternalPreviewSettings settings, Rectangle bounds)
    {
        bool enteringExternalDisplay = !IsExternalDisplay;
        if (enteringExternalDisplay)
        {
            _normalBounds = Bounds;
            _normalShowInTaskbar = ShowInTaskbar;
        }
        _externalSettings = settings.Clone();
        if (enteringExternalDisplay) ShowInTaskbar = false;
        if (enteringExternalDisplay && IsHandleCreated) UpdateStyles();
        SuspendLayout();
        try
        {
            // Setting even the same WindowState can call ShowWindow and activate
            // the form. This method is called by the monitor timer every second.
            if (WindowState != FormWindowState.Normal) WindowState = FormWindowState.Normal;
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
            if (enteringExternalDisplay) _externalSurface.BringToFront();
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
        ShowInTaskbar = _normalShowInTaskbar;
        if (IsHandleCreated) UpdateStyles();
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
