using ENTcapture2.WinForms.Ui;
using OpenCvSharp.Extensions;
using Cv2 = OpenCvSharp.Cv2;
using CvInterpolationFlags = OpenCvSharp.InterpolationFlags;
using CvMat = OpenCvSharp.Mat;
using CvSize = OpenCvSharp.Size;

namespace ENTcapture2.WinForms;

public sealed class PreviewZoomForm : Form
{
    private readonly Panel _scrollPanel = new();
    private readonly PictureBox _pictureBox = new();
    private readonly Label _zoomLabel = new();
    private readonly TableLayoutPanel _playbackPanel = new();
    private readonly Button _playPauseButton = new();
    private readonly TrackBar _playbackTrackBar = new();
    private Bitmap? _sourceImage;
    private double _zoom = 1.0;
    private bool _isManualZoom;
    private bool _useManualZoomAfterResize;
    private bool _isUserResizing;
    private TimeSpan _playbackTotalTime = TimeSpan.Zero;
    private bool _isUpdatingPlaybackTrackBar;

    public event Action<TimeSpan>? PlaybackSeekRequested;
    public event Action? PlaybackToggleRequested;

    public PreviewZoomForm()
    {
        UpdateTitle();
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(320, 240);
        Size = new Size(960, 720);
        BackColor = Theme.Window;
        ForeColor = Theme.Text;
        KeyPreview = true;

        _scrollPanel.Dock = DockStyle.Fill;
        _scrollPanel.AutoScroll = false;
        _scrollPanel.BackColor = Color.Black;

        _pictureBox.Location = Point.Empty;
        _pictureBox.BackColor = Color.Black;
        _pictureBox.SizeMode = PictureBoxSizeMode.Normal;
        _pictureBox.MouseWheel += PreviewZoomForm_MouseWheel;
        _pictureBox.MouseEnter += (_, _) => _pictureBox.Focus();
        _pictureBox.DoubleClick += (_, _) =>
        {
            _isManualZoom = false;
            FitImageToWindow();
        };

        _zoomLabel.AutoSize = true;
        _zoomLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _zoomLabel.BackColor = Color.FromArgb(180, 4, 7, 12);
        _zoomLabel.ForeColor = Theme.Text;
        _zoomLabel.Padding = new Padding(8, 4, 8, 4);
        _zoomLabel.Location = new Point(12, 12);

        _playbackPanel.Dock = DockStyle.Bottom;
        _playbackPanel.Height = 42;
        _playbackPanel.BackColor = Theme.Window;
        _playbackPanel.ColumnCount = 2;
        _playbackPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104F));
        _playbackPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _playbackPanel.RowCount = 1;
        _playbackPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _playbackPanel.Padding = new Padding(6, 4, 6, 4);

        _playPauseButton.Dock = DockStyle.Fill;
        _playPauseButton.Enabled = false;
        _playPauseButton.Text = PlaybackCaptions.Play;
        _playPauseButton.Click += (_, _) => PlaybackToggleRequested?.Invoke();

        _playbackTrackBar.Dock = DockStyle.Fill;
        _playbackTrackBar.Minimum = 0;
        _playbackTrackBar.Maximum = 1000;
        _playbackTrackBar.Value = 0;
        _playbackTrackBar.TickStyle = TickStyle.None;
        _playbackTrackBar.BackColor = Theme.Window;
        _playbackTrackBar.Enabled = false;
        _playbackTrackBar.Scroll += PlaybackTrackBar_Scroll;
        _playbackTrackBar.MouseWheel += PlaybackTrackBar_MouseWheel;

        _playbackPanel.Controls.Add(_playPauseButton, 0, 0);
        _playbackPanel.Controls.Add(_playbackTrackBar, 1, 0);
        _scrollPanel.Controls.Add(_pictureBox);
        Controls.Add(_scrollPanel);
        Controls.Add(_playbackPanel);
        Controls.Add(_zoomLabel);
        MouseWheel += PreviewZoomForm_MouseWheel;
        ResizeBegin += (_, _) => _isUserResizing = true;
        ResizeEnd += (_, _) =>
        {
            _isUserResizing = false;
            if (_sourceImage is not null)
            {
                _isManualZoom = false;
                FitImageToWindow();
            }
        };
        Resize += (_, _) =>
        {
            if (_sourceImage is not null)
            {
                if (_useManualZoomAfterResize)
                {
                    _useManualZoomAfterResize = false;
                    _isManualZoom = true;
                    ApplyZoom(resizeWindow: false);
                }
                else if (!_isManualZoom || _isUserResizing)
                {
                    _isManualZoom = false;
                    FitImageToWindow();
                }
                else
                {
                    PositionImageBox(_pictureBox.Width, _pictureBox.Height);
                }
            }

            PositionZoomLabel();
        };
        Shown += (_, _) => _pictureBox.Focus();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Icon = Owner?.Icon ?? Application.OpenForms
            .Cast<Form>()
            .FirstOrDefault(form => form.Icon is not null)
            ?.Icon;
        FitImageToWindow();
    }

    public void SetImage(Bitmap source)
    {
        Point? scrollPosition = _isManualZoom
            ? GetScrollPosition()
            : null;
        ReplaceSourceImage(source);
        if (_isManualZoom)
        {
            ApplyZoom(resizeWindow: false);
            if (scrollPosition is { } position)
            {
                RestoreScrollPosition(position);
                if (!IsDisposed && IsHandleCreated)
                {
                    BeginInvoke((Action)(() =>
                    {
                        if (!IsDisposed)
                        {
                            RestoreScrollPosition(position);
                        }
                    }));
                }
            }
        }
        else
        {
            FitImageToWindow();
        }
    }

    public void SetImage(Bitmap source, double zoom)
    {
        Bitmap sourceImage = ReplaceSourceImage(source);
        _isManualZoom = false;
        _zoom = Math.Clamp(Math.Round(zoom, 1), 0.1, 3.0);
        ResizeWindowToImage(
            Math.Max(1, (int)Math.Round(sourceImage.Width * _zoom)),
            Math.Max(1, (int)Math.Round(sourceImage.Height * _zoom)));
        FitImageToWindow();
    }

    public void ClearImage()
    {
        ClearDisplayImage();
        _sourceImage?.Dispose();
        _sourceImage = null;
        _zoomLabel.Text = "No image";
        SetPlaybackPosition(TimeSpan.Zero, TimeSpan.Zero);
        SetPlaybackState(false, true);
        UpdateTitle();
    }

    public void SetPlaybackState(bool isOpen, bool isPaused)
    {
        _playPauseButton.Enabled = isOpen && _playbackTotalTime > TimeSpan.Zero;
        _playPauseButton.Text = isPaused
            ? PlaybackCaptions.Play
            : PlaybackCaptions.Pause;
    }

    public void SetPlaybackPosition(
        TimeSpan currentTime,
        TimeSpan totalTime)
    {
        _playbackTotalTime = totalTime > TimeSpan.Zero
            ? totalTime
            : TimeSpan.Zero;
        _isUpdatingPlaybackTrackBar = true;
        try
        {
            _playbackTrackBar.Enabled = _playbackTotalTime > TimeSpan.Zero;
            _playbackTrackBar.Value = _playbackTotalTime <= TimeSpan.Zero
                ? 0
                : Math.Clamp(
                    (int)Math.Round(
                        currentTime.Ticks /
                        (double)Math.Max(1, _playbackTotalTime.Ticks) *
                        _playbackTrackBar.Maximum),
                    _playbackTrackBar.Minimum,
                    _playbackTrackBar.Maximum);
        }
        finally
        {
            _isUpdatingPlaybackTrackBar = false;
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        ClearDisplayImage();
        _sourceImage?.Dispose();
        _sourceImage = null;
        base.OnFormClosed(e);
    }

    private void PreviewZoomForm_MouseWheel(object? sender, MouseEventArgs e)
    {
        double step = e.Delta < 0 ? 0.1 : -0.1;
        ResizeWindowForZoom(_zoom + step);
        if (e is HandledMouseEventArgs handled)
        {
            handled.Handled = true;
        }
    }

    private void PlaybackTrackBar_Scroll(object? sender, EventArgs e)
    {
        if (_isUpdatingPlaybackTrackBar ||
            _playbackTotalTime <= TimeSpan.Zero)
        {
            return;
        }

        long ticks = (long)Math.Round(
            _playbackTrackBar.Value /
            (double)_playbackTrackBar.Maximum *
            _playbackTotalTime.Ticks);
        PlaybackSeekRequested?.Invoke(TimeSpan.FromTicks(
            Math.Clamp(ticks, 0, _playbackTotalTime.Ticks)));
    }

    private void PlaybackTrackBar_MouseWheel(object? sender, MouseEventArgs e)
    {
        PreviewZoomForm_MouseWheel(sender, e);
        if (e is HandledMouseEventArgs handled)
        {
            handled.Handled = true;
        }
    }

    //private void SetZoom(double zoom, bool resizeWindow = false)
    //{
    //    _zoom = Math.Clamp(Math.Round(zoom, 1), 0.1, 3.0);
    //    ApplyZoom(resizeWindow);
    //}

    private void ResizeWindowForZoom(double zoom)
    {
        if (_sourceImage is null)
        {
            return;
        }

        _isManualZoom = false;
        _zoom = Math.Clamp(Math.Round(zoom, 1), 0.1, 2.0);
        var previousSize = Size;
        int imageWidth =
            Math.Max(1, (int)Math.Round(_sourceImage.Width * _zoom));
        int imageHeight =
            Math.Max(1, (int)Math.Round(_sourceImage.Height * _zoom));
        _useManualZoomAfterResize =
            CalculateWindowSizeForImage(imageWidth, imageHeight).Clamped;
        ResizeWindowToImage(imageWidth, imageHeight);
        if (Size == previousSize)
        {
            if (_useManualZoomAfterResize)
            {
                _useManualZoomAfterResize = false;
                _isManualZoom = true;
                ApplyZoom(resizeWindow: false);
            }
            else
            {
                FitImageToWindow();
            }
        }
    }

    private void ApplyZoom(bool resizeWindow)
    {
        if (_sourceImage is null)
        {
            _zoomLabel.Text = "100%";
            UpdateTitle();
            return;
        }

        int width = Math.Max(1, (int)Math.Round(_sourceImage.Width * _zoom));
        int height = Math.Max(1, (int)Math.Round(_sourceImage.Height * _zoom));
        _scrollPanel.AutoScroll = true;
        _pictureBox.Size = new Size(width, height);
        ResizeDisplayImage(width, height);
        PositionImageBox(width, height);
        if (resizeWindow)
        {
            ResizeWindowToImage(width, height);
        }

        _zoomLabel.Text = $"{_zoom * 100:0}%";
        UpdateTitle();
        PositionZoomLabel();
        _pictureBox.Invalidate();
    }

    private void FitImageToWindow()
    {
        if (_sourceImage is null ||
            _scrollPanel.ClientSize.Width <= 0 ||
            _scrollPanel.ClientSize.Height <= 0)
        {
            PositionZoomLabel();
            return;
        }

        Size imageSize = _sourceImage.Size;
        _scrollPanel.AutoScroll = false;
        double scale = Math.Min(
            _scrollPanel.ClientSize.Width / (double)imageSize.Width,
            _scrollPanel.ClientSize.Height / (double)imageSize.Height);
        int width = Math.Max(1, (int)Math.Round(imageSize.Width * scale));
        int height = Math.Max(1, (int)Math.Round(imageSize.Height * scale));
        _zoom = Math.Clamp(Math.Round(scale, 1), 0.1, 3.0);
        _pictureBox.Size = new Size(width, height);
        ResizeDisplayImage(width, height);
        PositionImageBox(width, height);
        _zoomLabel.Text = $"{_zoom * 100:0}%";
        UpdateTitle();
        PositionZoomLabel();
        _pictureBox.Invalidate();
    }

    private void PositionImageBox(int width, int height)
    {
        if (width > _scrollPanel.ClientSize.Width ||
            height > _scrollPanel.ClientSize.Height)
        {
            _pictureBox.Location = Point.Empty;
            return;
        }

        _pictureBox.Location = new Point(
            Math.Max(0, (_scrollPanel.ClientSize.Width - width) / 2),
            Math.Max(0, (_scrollPanel.ClientSize.Height - height) / 2));
    }

    private Point GetScrollPosition()
    {
        Point position = _scrollPanel.AutoScrollPosition;
        return new Point(-position.X, -position.Y);
    }

    private void RestoreScrollPosition(Point position)
    {
        int maxX = Math.Max(0, _pictureBox.Width - _scrollPanel.ClientSize.Width);
        int maxY = Math.Max(0, _pictureBox.Height - _scrollPanel.ClientSize.Height);
        _scrollPanel.AutoScrollPosition = new Point(
            Math.Clamp(position.X, 0, maxX),
            Math.Clamp(position.Y, 0, maxY));
    }

    private Bitmap ReplaceSourceImage(Bitmap source)
    {
        Bitmap clone = (Bitmap)source.Clone();
        _sourceImage?.Dispose();
        _sourceImage = clone;
        return clone;
    }

    private void ResizeDisplayImage(int width, int height)
    {
        if (_sourceImage is null)
        {
            ClearDisplayImage();
            return;
        }

        Image? previous = _pictureBox.Image;
        _pictureBox.Image = ResizeWithLanczos(_sourceImage, width, height);
        previous?.Dispose();
    }

    private static Bitmap ResizeWithLanczos(Bitmap source, int width, int height)
    {
        using CvMat srcmat = source.ToMat();
        using CvMat dstmat = new();
        Cv2.Resize(
            srcmat,
            dstmat,
            new CvSize(width, height),
            0,
            0,
            CvInterpolationFlags.Lanczos4);
        return dstmat.ToBitmap();
    }

    private void ClearDisplayImage()
    {
        Image? previous = _pictureBox.Image;
        _pictureBox.Image = null;
        previous?.Dispose();
    }

    private void UpdateTitle()
    {
        Text = $"ENTcapture2 Preview (x{_zoom:0.0})（マウスホイールで拡大縮小）";
    }

    private void ResizeWindowToImage(int imageWidth, int imageHeight)
    {
        if (WindowState != FormWindowState.Normal)
        {
            return;
        }

        Size targetSize = CalculateWindowSizeForImage(
            imageWidth,
            imageHeight).TargetSize;
        if (Size != targetSize)
        {
            Size = targetSize;
        }

        Rectangle area = Screen.FromControl(this).WorkingArea;
        Left = Math.Clamp(Left, area.Left, Math.Max(area.Left, area.Right - Width));
        Top = Math.Clamp(Top, area.Top, Math.Max(area.Top, area.Bottom - Height));
    }

    private (Size TargetSize, bool Clamped) CalculateWindowSizeForImage(
        int imageWidth,
        int imageHeight)
    {
        Rectangle area = Screen.FromControl(this).WorkingArea;
        int borderWidth = Width - ClientSize.Width;
        int borderHeight = Height - ClientSize.Height;
        int requestedWidth = imageWidth + borderWidth;
        int requestedHeight = imageHeight + _playbackPanel.Height + borderHeight;
        int targetWidth = Math.Clamp(
            requestedWidth,
            MinimumSize.Width,
            Math.Max(MinimumSize.Width, area.Width));
        int targetHeight = Math.Clamp(
            requestedHeight,
            MinimumSize.Height,
            Math.Max(MinimumSize.Height, area.Height));

        return (
            new Size(targetWidth, targetHeight),
            targetWidth < requestedWidth || targetHeight < requestedHeight);
    }

    private void InitializeComponent()
    {

    }

    private void PositionZoomLabel()
    {
        _zoomLabel.Location = new Point(
            Math.Max(12, ClientSize.Width - _zoomLabel.Width - 18),
            12);
    }
}
