using System.Drawing.Drawing2D;
using ENTcapture2.WinForms.Ui;

namespace ENTcapture2.WinForms;

public sealed class PreviewZoomForm : Form
{
    private readonly Panel _scrollPanel = new();
    private readonly PictureBox _pictureBox = new();
    private readonly Label _zoomLabel = new();
    private readonly TableLayoutPanel _playbackPanel = new();
    private readonly Button _playPauseButton = new();
    private readonly TrackBar _playbackTrackBar = new();
    private double _zoom = 1.0;
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
        _scrollPanel.AutoScroll = true;
        _scrollPanel.BackColor = Color.Black;

        _pictureBox.Location = Point.Empty;
        _pictureBox.BackColor = Color.Black;
        _pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
        _pictureBox.MouseWheel += PreviewZoomForm_MouseWheel;
        _pictureBox.MouseEnter += (_, _) => _pictureBox.Focus();
        _pictureBox.DoubleClick += (_, _) => SetZoom(1.0, resizeWindow: true);

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
        Resize += (_, _) => PositionZoomLabel();
        Shown += (_, _) => _pictureBox.Focus();
    }

    public void SetImage(Bitmap source)
    {
        Bitmap clone = (Bitmap)source.Clone();
        Image? previous = _pictureBox.Image;
        _pictureBox.Image = clone;
        previous?.Dispose();
        ApplyZoom(resizeWindow: false);
    }

    public void SetImage(Bitmap source, double zoom)
    {
        Bitmap clone = (Bitmap)source.Clone();
        Image? previous = _pictureBox.Image;
        _pictureBox.Image = clone;
        previous?.Dispose();
        SetZoom(zoom, resizeWindow: true);
    }

    public void ClearImage()
    {
        Image? previous = _pictureBox.Image;
        _pictureBox.Image = null;
        previous?.Dispose();
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
        Image? previous = _pictureBox.Image;
        _pictureBox.Image = null;
        previous?.Dispose();
        base.OnFormClosed(e);
    }

    private void PreviewZoomForm_MouseWheel(object? sender, MouseEventArgs e)
    {
        double step = e.Delta < 0 ? 0.1 : -0.1;
        SetZoom(_zoom + step, resizeWindow: true);
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

    private void SetZoom(double zoom, bool resizeWindow = false)
    {
        _zoom = Math.Clamp(Math.Round(zoom, 1), 0.1, 2.0);
        ApplyZoom(resizeWindow);
    }

    private void ApplyZoom(bool resizeWindow)
    {
        if (_pictureBox.Image is null)
        {
            _zoomLabel.Text = "100%";
            UpdateTitle();
            return;
        }

        int width = Math.Max(1, (int)Math.Round(_pictureBox.Image.Width * _zoom));
        int height = Math.Max(1, (int)Math.Round(_pictureBox.Image.Height * _zoom));
        _pictureBox.Size = new Size(width, height);
        if (resizeWindow)
        {
            ResizeWindowToImage(width, height);
        }

        _zoomLabel.Text = $"{_zoom * 100:0}%";
        UpdateTitle();
        PositionZoomLabel();
        _pictureBox.Invalidate();
    }

    private void UpdateTitle()
    {
        Text = $"ENTcapture2 Preview (x{_zoom:0.0})";
    }

    private void ResizeWindowToImage(int imageWidth, int imageHeight)
    {
        if (WindowState != FormWindowState.Normal)
        {
            return;
        }

        Rectangle area = Screen.FromControl(this).WorkingArea;
        int borderWidth = Width - ClientSize.Width;
        int borderHeight = Height - ClientSize.Height;
        int targetWidth = Math.Clamp(
            imageWidth + borderWidth + SystemInformation.VerticalScrollBarWidth,
            MinimumSize.Width,
            Math.Max(MinimumSize.Width, area.Width));
        int targetHeight = Math.Clamp(
            imageHeight +
            _playbackPanel.Height +
            borderHeight +
            SystemInformation.HorizontalScrollBarHeight,
            MinimumSize.Height,
            Math.Max(MinimumSize.Height, area.Height));

        Size = new Size(targetWidth, targetHeight);
        Left = Math.Clamp(Left, area.Left, Math.Max(area.Left, area.Right - Width));
        Top = Math.Clamp(Top, area.Top, Math.Max(area.Top, area.Bottom - Height));
    }

    private void PositionZoomLabel()
    {
        _zoomLabel.Location = new Point(
            Math.Max(12, ClientSize.Width - _zoomLabel.Width - 18),
            12);
    }
}
