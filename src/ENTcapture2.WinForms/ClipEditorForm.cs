using System.ComponentModel;
using ENTcapture2.WinForms.Capture;
using ENTcapture2.WinForms.Ui;

namespace ENTcapture2.WinForms;

internal sealed class ClipEditorForm : Form
{
    private readonly IReadOnlyList<string> _sourceFiles;
    private readonly Func<PlaybackPosition?> _getPlaybackPosition;
    private readonly Action<TimeSpan> _seekPlayback;
    private readonly Func<Bitmap?> _capturePreview;
    private readonly Func<string, DateTime, string, string> _buildFileName;
    private readonly Func<string, DateTime, string, int, string[]> _buildFileNames;
    private readonly List<ClipExportRange> _ranges = [];
    private PlaybackPosition? _lastPosition;
    private TimeSpan? _startTime;
    private TimeSpan? _endTime;
    private bool _isUpdatingPlaybackSlider;
    private bool _isUserSeeking;
    private TimeSpan? _pendingSeekTime;
    private TimeSpan _lastPresentationTotal = TimeSpan.MinValue;
    private int _frameRenderPending;

    private readonly Label _sourceLabel = new();
    private readonly PictureBox _previewBox = new();
    private readonly TrackBar _playbackTrackBar = new();
    private readonly Label _positionLabel = new();
    private readonly ClipRangeBar _rangeBar = new();
    private readonly Label _rangeLabel = new();
    private readonly ListBox _rangeListBox = new();
    private readonly ComboBox _encoderComboBox = new();
    private readonly NumericUpDown _bitrateInput = new();
    private readonly RadioButton _joinedRadioButton = new();
    private readonly RadioButton _separateRadioButton = new();
    private readonly TextBox _outputDirectoryTextBox = new();
    private readonly TextBox _commandPreviewTextBox = new();
    private readonly ModernButton _setStartButton = new();
    private readonly ModernButton _setEndButton = new();
    private readonly ModernButton _addRangeButton = new();
    private readonly ModernButton _removeRangeButton = new();
    private readonly ModernButton _clearButton = new();
    private readonly ModernButton _browseButton = new();
    private readonly ModernButton _exportButton = new();
    private readonly ModernButton _cancelButton = new();
    private readonly System.Windows.Forms.Timer _refreshTimer = new();
    private readonly System.Windows.Forms.Timer _seekTimer = new();

    public ClipEditorForm(
        IReadOnlyList<string> sourceFiles,
        string outputDirectory,
        Func<PlaybackPosition?> getPlaybackPosition,
        Action<TimeSpan> seekPlayback,
        Func<Bitmap?> capturePreview,
        Func<string, DateTime, string, string> buildFileName,
        Func<string, DateTime, string, int, string[]> buildFileNames)
    {
        _sourceFiles = sourceFiles;
        _getPlaybackPosition = getPlaybackPosition;
        _seekPlayback = seekPlayback;
        _capturePreview = capturePreview;
        _buildFileName = buildFileName;
        _buildFileNames = buildFileNames;

        Text = "動画編集・エンコード";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(820, 620);
        Size = new Size(920, 700);
        BackColor = Theme.Window;
        ForeColor = Theme.Text;
        Font = Theme.BodyFont();

        BuildLayout(outputDirectory);
        UpdatePresentation();
    }

    private void BuildLayout(string outputDirectory)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7,
            Padding = new Padding(16),
            BackColor = Theme.Window
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 250F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 86F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        _sourceLabel.AutoSize = true;
        _sourceLabel.ForeColor = Theme.Muted;
        _sourceLabel.Text = _sourceFiles.Count == 1
            ? $"元動画: {Path.GetFileName(_sourceFiles[0])}"
            : $"元動画: {_sourceFiles.Count} ファイル";
        root.Controls.Add(_sourceLabel, 0, 0);

        var previewPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.Surface,
            Padding = new Padding(10),
            Margin = new Padding(0, 10, 0, 8)
        };
        previewPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        previewPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));
        previewPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        _previewBox.Dock = DockStyle.Fill;
        _previewBox.BackColor = Color.Black;
        _previewBox.SizeMode = PictureBoxSizeMode.Zoom;
        _rangeBar.Dock = DockStyle.Fill;
        _rangeBar.Margin = new Padding(3, 0, 3, 0);
        _playbackTrackBar.Dock = DockStyle.Fill;
        _playbackTrackBar.Maximum = 1000;
        _playbackTrackBar.TickStyle = TickStyle.None;
        _positionLabel.Dock = DockStyle.Right;
        _positionLabel.Width = 160;
        _positionLabel.ForeColor = Theme.Muted;
        _positionLabel.TextAlign = ContentAlignment.MiddleRight;
        var sliderPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        var rangeBarPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        sliderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        sliderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
        rangeBarPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rangeBarPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
        sliderPanel.Controls.Add(_playbackTrackBar, 0, 0);
        sliderPanel.Controls.Add(_positionLabel, 1, 0);
        rangeBarPanel.Controls.Add(_rangeBar, 0, 0);
        previewPanel.Controls.Add(_previewBox, 0, 0);
        previewPanel.Controls.Add(rangeBarPanel, 0, 1);
        previewPanel.Controls.Add(sliderPanel, 0, 2);
        root.Controls.Add(previewPanel, 0, 1);

        var rangeButtons = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            Margin = new Padding(0, 12, 0, 8)
        };
        ConfigureButton(_setStartButton, "現在位置を始点", Theme.SurfaceRaised);
        ConfigureButton(_setEndButton, "現在位置を終点", Theme.SurfaceRaised);
        ConfigureButton(_addRangeButton, "区間を追加", Theme.Accent);
        ConfigureButton(_removeRangeButton, "選択区間を削除", Theme.SurfaceRaised);
        ConfigureButton(_clearButton, "クリア", Theme.SurfaceRaised);
        rangeButtons.Controls.AddRange(
            [
                _setStartButton,
                _setEndButton,
                _addRangeButton,
                _removeRangeButton,
                _clearButton
            ]);
        root.Controls.Add(rangeButtons, 0, 2);

        var rangePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.Surface,
            Padding = new Padding(12)
        };
        rangePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rangePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _rangeLabel.AutoSize = true;
        _rangeLabel.ForeColor = Theme.Muted;
        _rangeListBox.Dock = DockStyle.Fill;
        _rangeListBox.BackColor = Theme.SurfaceRaised;
        _rangeListBox.ForeColor = Theme.Text;
        _rangeListBox.BorderStyle = BorderStyle.FixedSingle;
        rangePanel.Controls.Add(_rangeLabel, 0, 0);
        rangePanel.Controls.Add(_rangeListBox, 0, 1);
        root.Controls.Add(rangePanel, 0, 3);

        var optionGrid = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            ColumnCount = 4,
            RowCount = 3,
            Margin = new Padding(0, 12, 0, 8)
        };
        optionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        optionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210F));
        optionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        optionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.Controls.Add(optionGrid, 0, 4);

        AddLabel(optionGrid, "出力形式", 0, 0);
        _joinedRadioButton.Text = "連結して1本";
        _joinedRadioButton.Checked = true;
        _joinedRadioButton.ForeColor = Theme.Text;
        _separateRadioButton.Text = "個別クリップ";
        _separateRadioButton.ForeColor = Theme.Text;
        var modePanel = new FlowLayoutPanel { AutoSize = true };
        modePanel.Controls.Add(_joinedRadioButton);
        modePanel.Controls.Add(_separateRadioButton);
        optionGrid.Controls.Add(modePanel, 1, 0);
        optionGrid.SetColumnSpan(modePanel, 3);

        AddLabel(optionGrid, "Codec", 0, 1);
        _encoderComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _encoderComboBox.BackColor = Theme.SurfaceRaised;
        _encoderComboBox.ForeColor = Theme.Text;
        _encoderComboBox.FlatStyle = FlatStyle.Flat;
        _encoderComboBox.Items.AddRange(
            [
                new EncoderChoice("コピー（無劣化・高速）", string.Empty, false),
                new EncoderChoice("H.264 自動", "AUTO", true),
                new EncoderChoice("H.264 NVENC", "NVENC", true),
                new EncoderChoice("H.264 AMF", "AMF", true),
                new EncoderChoice("H.264 QSV", "QSV", true),
                new EncoderChoice("MJPEG", "mjpeg", true)
            ]);
        _encoderComboBox.SelectedIndex = 1;
        optionGrid.Controls.Add(_encoderComboBox, 1, 1);

        AddLabel(optionGrid, "ビットレート", 2, 1);
        _bitrateInput.Minimum = 500;
        _bitrateInput.Maximum = 100000;
        _bitrateInput.Increment = 500;
        _bitrateInput.Value = 5000;
        _bitrateInput.BackColor = Theme.SurfaceRaised;
        _bitrateInput.ForeColor = Theme.Text;
        optionGrid.Controls.Add(_bitrateInput, 3, 1);

        AddLabel(optionGrid, "出力先", 0, 2);
        _outputDirectoryTextBox.Text = outputDirectory;
        _outputDirectoryTextBox.BackColor = Theme.SurfaceRaised;
        _outputDirectoryTextBox.ForeColor = Theme.Text;
        _outputDirectoryTextBox.BorderStyle = BorderStyle.FixedSingle;
        _outputDirectoryTextBox.Dock = DockStyle.Fill;
        optionGrid.Controls.Add(_outputDirectoryTextBox, 1, 2);
        optionGrid.SetColumnSpan(_outputDirectoryTextBox, 2);
        ConfigureButton(_browseButton, "参照...", Theme.SurfaceRaised);
        optionGrid.Controls.Add(_browseButton, 3, 2);

        _commandPreviewTextBox.Dock = DockStyle.Fill;
        _commandPreviewTextBox.Multiline = true;
        _commandPreviewTextBox.ReadOnly = true;
        _commandPreviewTextBox.BackColor = Theme.SurfaceRaised;
        _commandPreviewTextBox.ForeColor = Theme.Muted;
        _commandPreviewTextBox.BorderStyle = BorderStyle.FixedSingle;
        root.Controls.Add(_commandPreviewTextBox, 0, 5);

        var footer = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.LeftToRight
        };
        ConfigureButton(_cancelButton, "閉じる", Theme.SurfaceRaised);
        ConfigureButton(_exportButton, "出力開始", Theme.Accent);
        footer.Controls.Add(_cancelButton);
        footer.Controls.Add(_exportButton);
        root.Controls.Add(footer, 0, 6);

        _setStartButton.Click += (_, _) =>
        {
            _startTime = GetCurrentEditorTime();
            if (_endTime is not null && _endTime <= _startTime)
            {
                _endTime = null;
            }

            UpdatePresentation();
        };
        _setEndButton.Click += (_, _) =>
        {
            _endTime = GetCurrentEditorTime();
            if (_startTime is not null && _endTime <= _startTime)
            {
                (_startTime, _endTime) = (_endTime, _startTime);
            }

            UpdatePresentation();
        };
        _addRangeButton.Click += (_, _) => AddRange();
        _removeRangeButton.Click += (_, _) => RemoveSelectedRange();
        _clearButton.Click += (_, _) =>
        {
            _ranges.Clear();
            _startTime = null;
            _endTime = null;
            UpdatePresentation();
        };
        _browseButton.Click += (_, _) => BrowseOutputDirectory();
        _exportButton.Click += async (_, _) => await ExportAsync();
        _cancelButton.Click += (_, _) => Close();
        _playbackTrackBar.MouseDown += (_, _) => _isUserSeeking = true;
        _playbackTrackBar.MouseUp += (_, _) =>
        {
            _isUserSeeking = false;
            TimeSpan? releasedTime = GetTrackBarTime();
            QueuePlaybackSeek(immediate: true);
            SetEndFromSliderRelease(releasedTime);
        };
        _playbackTrackBar.Scroll += PlaybackTrackBar_Scroll;
        _encoderComboBox.SelectedIndexChanged += (_, _) => UpdatePresentation();
        _bitrateInput.ValueChanged += (_, _) => UpdatePresentation();
        _joinedRadioButton.CheckedChanged += (_, _) => UpdatePresentation();
        _separateRadioButton.CheckedChanged += (_, _) => UpdatePresentation();

        _refreshTimer.Interval = 50;
        _refreshTimer.Tick += (_, _) => RefreshPlaybackPreview();
        _refreshTimer.Start();
        _seekTimer.Interval = 90;
        _seekTimer.Tick += (_, _) =>
        {
            _seekTimer.Stop();
            ApplyPendingSeek();
        };
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _refreshTimer.Stop();
        _seekTimer.Stop();
        _previewBox.Image?.Dispose();
        _previewBox.Image = null;
        base.OnFormClosed(e);
    }

    private void PlaybackTrackBar_Scroll(object? sender, EventArgs e)
    {
        if (_isUpdatingPlaybackSlider)
        {
            return;
        }

        QueuePlaybackSeek(immediate: true);
    }

    private void QueuePlaybackSeek(bool immediate)
    {
        PlaybackPosition? position = _lastPosition;
        if (position is null || position.TotalTime <= TimeSpan.Zero)
        {
            return;
        }

        _pendingSeekTime = GetTrackBarTime();
        if (_pendingSeekTime is null)
        {
            return;
        }

        _positionLabel.Text =
            $"{FormatTime(_pendingSeekTime)} / {FormatTime(position.TotalTime)}";
        _rangeBar.CurrentTime = _pendingSeekTime.Value;
        _rangeBar.Invalidate();

        if (immediate)
        {
            _seekTimer.Stop();
            ApplyPendingSeek();
            return;
        }

        _seekTimer.Stop();
        _seekTimer.Start();
    }

    private void ApplyPendingSeek()
    {
        if (_pendingSeekTime is null)
        {
            return;
        }

        TimeSpan seekTime = _pendingSeekTime.Value;
        _pendingSeekTime = null;
        _seekPlayback(seekTime);
    }

    private void RefreshPlaybackPreview()
    {
        RefreshPlaybackPresentation();
    }

    private void RefreshPlaybackPresentation()
    {
        PlaybackPosition? position = _getPlaybackPosition();
        if (position is not null)
        {
            _lastPosition = position;
        }

        TimeSpan current =
            _isUserSeeking && _pendingSeekTime is not null
                ? _pendingSeekTime.Value
                : position?.CurrentTime ?? TimeSpan.Zero;
        TimeSpan total = position?.TotalTime ?? TimeSpan.Zero;
        if (total != _lastPresentationTotal)
        {
            _lastPresentationTotal = total;
            UpdatePresentation();
        }

        _positionLabel.Text =
            $"{FormatTime(current)} / {FormatTime(total)}";

        _rangeBar.TotalTime = total;
        _rangeBar.CurrentTime = current;
        _rangeBar.Ranges = GetDisplayRanges(total).ToArray();
        _rangeBar.PendingStart = _startTime;
        _rangeBar.PendingEnd =
            _endTime ??
            (_startTime is not null ? current : null);
        _rangeBar.Invalidate();

        if (!_isUserSeeking)
        {
            _isUpdatingPlaybackSlider = true;
            try
            {
                _playbackTrackBar.Enabled = total > TimeSpan.Zero;
                _playbackTrackBar.Value = total <= TimeSpan.Zero
                    ? 0
                    : Math.Clamp(
                        (int)Math.Round(
                            current.Ticks /
                            (double)Math.Max(1, total.Ticks) *
                            _playbackTrackBar.Maximum),
                        _playbackTrackBar.Minimum,
                        _playbackTrackBar.Maximum);
            }
            finally
            {
                _isUpdatingPlaybackSlider = false;
            }
        }

        if (Interlocked.Exchange(ref _frameRenderPending, 1) == 1)
        {
            return;
        }

        try
        {
            Bitmap? preview = _capturePreview();
            if (preview is null)
            {
                return;
            }

            Image? previous = _previewBox.Image;
            _previewBox.Image = preview;
            previous?.Dispose();
        }
        finally
        {
            Interlocked.Exchange(ref _frameRenderPending, 0);
        }
    }

    private static void ConfigureButton(
        ModernButton button,
        string text,
        Color fillColor)
    {
        button.Text = text;
        button.FillColor = fillColor;
        button.BackColor = fillColor;
        button.HoverColor = fillColor == Theme.Accent
            ? Theme.AccentHover
            : Theme.ButtonHover;
        button.ForeColor = Theme.Text;
        button.Width = 124;
        button.Height = 38;
        button.Margin = new Padding(0, 0, 8, 0);
    }

    private static void AddLabel(
        TableLayoutPanel grid,
        string text,
        int column,
        int row)
    {
        grid.Controls.Add(
            new Label
            {
                Text = text,
                ForeColor = Theme.Muted,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            },
            column,
            row);
    }

    private void AddRange()
    {
        if (_startTime is null || _endTime is null || _endTime <= _startTime)
        {
            MessageBox.Show(
                this,
                "始点と終点を指定してください。",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        _ranges.Add(new ClipExportRange(_startTime.Value, _endTime.Value));
        _ranges.Sort((left, right) => left.Start.CompareTo(right.Start));
        _startTime = null;
        _endTime = null;
        UpdatePresentation();
    }

    private void RemoveSelectedRange()
    {
        if (_rangeListBox.SelectedIndex < 0 ||
            _rangeListBox.SelectedIndex >= _ranges.Count)
        {
            return;
        }

        _ranges.RemoveAt(_rangeListBox.SelectedIndex);
        UpdatePresentation();
    }

    private void BrowseOutputDirectory()
    {
        using var dialog = new FolderBrowserDialog
        {
            InitialDirectory = Directory.Exists(_outputDirectoryTextBox.Text)
                ? _outputDirectoryTextBox.Text
                : Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            UseDescriptionForTitle = true,
            Description = "出力先フォルダーを選択してください"
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _outputDirectoryTextBox.Text = dialog.SelectedPath;
            UpdatePresentation();
        }
    }

    private async Task ExportAsync()
    {
        ClipExportRange[] exportRanges = GetExportRanges().ToArray();
        if (exportRanges.Length == 0)
        {
            MessageBox.Show(
                this,
                "出力する区間がありません。",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        string outputDirectory = _outputDirectoryTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            MessageBox.Show(
                this,
                "出力先を指定してください。",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        Directory.CreateDirectory(outputDirectory);
        ClipExportOptions options = CreateOptions();
        string extension = options.EncoderName.Equals(
            "mjpeg",
            StringComparison.OrdinalIgnoreCase)
                ? ".avi"
                : ".mp4";
        DateTime now = DateTime.Now;
        var exporter = new ClipExportService();
        var progress = new Progress<string>(
            message =>
            {
                _commandPreviewTextBox.AppendText(
                    Environment.NewLine +
                    Environment.NewLine +
                    message);
            });

        _exportButton.Enabled = false;
        try
        {
            if (_joinedRadioButton.Checked)
            {
                string outputFile = Path.Combine(
                    outputDirectory,
                    _buildFileName(outputDirectory, now, extension));
                await exporter.ExportJoinedAsync(
                    _sourceFiles,
                    exportRanges,
                    outputFile,
                    options,
                    progress);
            }
            else
            {
                string[] outputFiles = _buildFileNames(
                    outputDirectory,
                    now,
                    extension,
                    exportRanges.Length)
                    .Select(fileName => Path.Combine(outputDirectory, fileName))
                    .ToArray();
                await exporter.ExportSeparateAsync(
                    _sourceFiles,
                    exportRanges,
                    outputFiles,
                    options,
                    progress);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                exception.Message,
                "出力に失敗しました",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _exportButton.Enabled = true;
        }
    }

    private ClipExportOptions CreateOptions()
    {
        var choice = (EncoderChoice)_encoderComboBox.SelectedItem!;
        if (!choice.Reencode)
        {
            return ClipExportOptions.Copy;
        }

        string encoder = choice.Value;
        if (encoder == "AUTO" || encoder is "NVENC" or "AMF" or "QSV")
        {
            encoder = FfmpegRuntime.SelectH264Encoder(encoder).EncoderName;
        }

        return new ClipExportOptions(
            ClipExportMode.Reencode,
            encoder,
            decimal.ToInt32(_bitrateInput.Value));
    }

    private TimeSpan? GetTrackBarTime()
    {
        PlaybackPosition? position = _lastPosition;
        if (position is null || position.TotalTime <= TimeSpan.Zero)
        {
            return null;
        }

        long ticks = (long)Math.Round(
            _playbackTrackBar.Value /
            (double)_playbackTrackBar.Maximum *
            position.TotalTime.Ticks);
        return TimeSpan.FromTicks(
            Math.Clamp(ticks, 0, position.TotalTime.Ticks));
    }

    private void SetEndFromSliderRelease(TimeSpan? releasedTime)
    {
        if (_startTime is null || releasedTime is null)
        {
            return;
        }

        TimeSpan current = releasedTime.Value;
        if (current == _startTime)
        {
            return;
        }

        _endTime = current;
        if (_endTime <= _startTime)
        {
            (_startTime, _endTime) = (_endTime, _startTime);
        }

        UpdatePresentation();
    }

    private void UpdatePresentation()
    {
        TimeSpan total = _lastPosition?.TotalTime ?? TimeSpan.Zero;
        ClipExportRange[] exportRanges = GetExportRanges().ToArray();
        _rangeLabel.Text =
            $"始点 {FormatTime(_startTime)} / 終点 {FormatTime(_endTime)}";
        _rangeLabel.Text = _ranges.Count == 0
            ? $"全選択 / 始点 {FormatTime(_startTime)} / 終点 {FormatTime(_endTime)}"
            : $"始点 {FormatTime(_startTime)} / 終点 {FormatTime(_endTime)}";
        _rangeListBox.BeginUpdate();
        try
        {
            _rangeListBox.Items.Clear();
            if (_ranges.Count == 0 && total > TimeSpan.Zero)
            {
                _rangeListBox.Items.Add(
                    $"全体: 00:00:00.000 - {FormatTime(total)}");
            }

            for (int index = 0; index < _ranges.Count; index++)
            {
                ClipExportRange range = _ranges[index];
                _rangeListBox.Items.Add(
                    $"{index + 1:D2}: {FormatTime(range.Start)} - " +
                    $"{FormatTime(range.End)}  " +
                    $"({FormatTime(range.End - range.Start)})");
            }
        }
        finally
        {
            _rangeListBox.EndUpdate();
        }

        var choice = (EncoderChoice?)_encoderComboBox.SelectedItem;
        _bitrateInput.Enabled = choice?.Reencode == true;
        TimeSpan selectedDuration = TimeSpan.FromTicks(
            exportRanges.Sum(range => (range.End - range.Start).Ticks));
        long sourceSizeBytes = GetSourceSizeBytes();
        string sourceSizeText = FormatFileSize(sourceSizeBytes);
        string sourceBitrateText = EstimateBitrateText(total, sourceSizeBytes);
        string estimatedSize = choice?.Reencode == true
            ? EstimateFileSizeText(
                selectedDuration,
                decimal.ToInt32(_bitrateInput.Value))
            : "コピー出力のためサイズ推定なし";
        _commandPreviewTextBox.Text =
            $"元動画: {sourceSizeText} / 平均 {sourceBitrateText}" +
            Environment.NewLine +
            "↓" +
            Environment.NewLine +
            $"区間数: {exportRanges.Length} / " +
            (_joinedRadioButton.Checked ? "連結出力" : "個別出力") +
            //Environment.NewLine +
            $"選択時間: {FormatTime(selectedDuration)} / 推定サイズ: {estimatedSize} / " +
            //Environment.NewLine +
            $"Codec: {choice?.Text} / " +
            $"Bitrate: {_bitrateInput.Value:0} kbps" +
            Environment.NewLine +
            Environment.NewLine +
            BuildCommandPreview(exportRanges);
        _rangeBar.Ranges = GetDisplayRanges(total).ToArray();
        _rangeBar.PendingStart = _startTime;
        _rangeBar.PendingEnd =
            _endTime ??
            (_startTime is not null ? GetCurrentEditorTime() : null);
        _rangeBar.Invalidate();
    }

    private TimeSpan GetCurrentEditorTime()
    {
        return _pendingSeekTime ?? _lastPosition?.CurrentTime ?? TimeSpan.Zero;
    }

    private string BuildCommandPreview(IReadOnlyList<ClipExportRange> ranges)
    {
        if (ranges.Count == 0)
        {
            return "ffmpeg: 出力区間が未確定です。";
        }

        string outputDirectory = string.IsNullOrWhiteSpace(
            _outputDirectoryTextBox.Text)
                ? Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)
                : _outputDirectoryTextBox.Text.Trim();
        ClipExportOptions options = CreateOptions();
        string extension = options.EncoderName.Equals(
            "mjpeg",
            StringComparison.OrdinalIgnoreCase)
                ? ".avi"
                : ".mp4";
        DateTime now = DateTime.Now;
        if (_joinedRadioButton.Checked)
        {
            string outputFile = Path.Combine(
                outputDirectory,
                _buildFileName(outputDirectory, now, extension));
            string firstCommand = ClipExportService.BuildExportCommandPreview(
                _sourceFiles,
                ranges[0],
                ranges.Count == 1 ? outputFile : "<segment_001" + extension + ">",
                options);
            if (ranges.Count == 1)
            {
                return "ffmpeg:" + Environment.NewLine + firstCommand;
            }

            string concatCommand = ClipExportService.BuildConcatCommandPreview(
                [],
                outputFile);
            return
                "ffmpeg（先頭区間の例）:" +
                Environment.NewLine +
                firstCommand +
                Environment.NewLine +
                "ffmpeg（連結）:" +
                Environment.NewLine +
                concatCommand;
        }

        string outputName = _buildFileNames(
            outputDirectory,
            now,
            extension,
            Math.Max(1, ranges.Count))[0];
        string command = ClipExportService.BuildExportCommandPreview(
            _sourceFiles,
            ranges[0],
            Path.Combine(outputDirectory, outputName),
            options);
        return "ffmpeg（先頭クリップの例）:" + Environment.NewLine + command;
    }

    private IEnumerable<ClipExportRange> GetExportRanges()
    {
        if (_ranges.Count > 0)
        {
            return _ranges;
        }

        TimeSpan total = _lastPosition?.TotalTime ?? TimeSpan.Zero;
        return total > TimeSpan.Zero
            ? [new ClipExportRange(TimeSpan.Zero, total)]
            : [];
    }

    private IEnumerable<ClipExportRange> GetDisplayRanges(TimeSpan total)
    {
        if (_ranges.Count > 0 ||
            _startTime is not null ||
            _endTime is not null)
        {
            return _ranges;
        }

        return total > TimeSpan.Zero
            ? [new ClipExportRange(TimeSpan.Zero, total)]
            : [];
    }

    private static string EstimateFileSizeText(
        TimeSpan duration,
        int bitrateKbps)
    {
        if (duration <= TimeSpan.Zero || bitrateKbps <= 0)
        {
            return "-- MB";
        }

        double megabytes =
            bitrateKbps * 1000.0 / 8.0 * duration.TotalSeconds /
            (1024.0 * 1024.0);
        return $"約 {megabytes:0.0} MB";
    }

    private long GetSourceSizeBytes()
    {
        long total = 0;
        foreach (string sourceFile in _sourceFiles)
        {
            try
            {
                var info = new FileInfo(sourceFile);
                if (info.Exists)
                {
                    total += info.Length;
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        return total;
    }

    private static string EstimateBitrateText(
        TimeSpan duration,
        long sizeBytes)
    {
        if (duration <= TimeSpan.Zero || sizeBytes <= 0)
        {
            return "-- kbps";
        }

        double kbps = sizeBytes * 8.0 / duration.TotalSeconds / 1000.0;
        return $"{kbps:0} kbps";
    }

    private static string EstimateSelectedSourceSizeText(
        TimeSpan totalDuration,
        TimeSpan selectedDuration,
        long sourceSizeBytes)
    {
        if (totalDuration <= TimeSpan.Zero ||
            selectedDuration <= TimeSpan.Zero ||
            sourceSizeBytes <= 0)
        {
            return "-- MB";
        }

        double ratio = Math.Clamp(
            selectedDuration.TotalSeconds / totalDuration.TotalSeconds,
            0.0,
            1.0);
        return FormatFileSize((long)Math.Round(sourceSizeBytes * ratio));
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes <= 0)
        {
            return "-- MB";
        }

        double megabytes = bytes / (1024.0 * 1024.0);
        if (megabytes < 1024.0)
        {
            return $"{megabytes:0.0} MB";
        }

        return $"{megabytes / 1024.0:0.00} GB";
    }

    private static string FormatTime(TimeSpan? value)
    {
        return value is null
            ? "--:--"
            : value.Value.ToString(@"hh\:mm\:ss\.fff");
    }

    private sealed record EncoderChoice(
        string Text,
        string Value,
        bool Reencode)
    {
        public override string ToString() => Text;
    }

    private sealed class ClipRangeBar : Control
    {
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IReadOnlyList<ClipExportRange> Ranges { get; set; } = [];

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TimeSpan? PendingStart { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TimeSpan? PendingEnd { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TimeSpan CurrentTime { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TimeSpan TotalTime { get; set; }

        public ClipRangeBar()
        {
            DoubleBuffered = true;
            Height = 18;
            MinimumSize = new Size(0, 18);
            BackColor = Theme.SurfaceRaised;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Rectangle bar = new(0, 5, Width, Math.Max(6, Height - 10));
            using var backgroundBrush = new SolidBrush(Theme.SurfaceRaised);
            e.Graphics.FillRectangle(backgroundBrush, ClientRectangle);
            using var railBrush = new SolidBrush(Color.FromArgb(55, 75, 96));
            e.Graphics.FillRectangle(railBrush, bar);

            foreach (ClipExportRange range in Ranges)
            {
                PaintRange(e.Graphics, bar, range.Start, range.End, Theme.Accent);
            }

            if (PendingStart is not null && PendingEnd is not null)
            {
                TimeSpan start = PendingStart.Value <= PendingEnd.Value
                    ? PendingStart.Value
                    : PendingEnd.Value;
                TimeSpan end = PendingStart.Value <= PendingEnd.Value
                    ? PendingEnd.Value
                    : PendingStart.Value;
                PaintRange(e.Graphics, bar, start, end, Theme.AccentBright);
            }

            if (TotalTime > TimeSpan.Zero)
            {
                int x = TimeToX(CurrentTime, bar);
                using var pen = new Pen(Color.White, 2F);
                e.Graphics.DrawLine(pen, x, 1, x, Height - 2);
            }
        }

        private void PaintRange(
            Graphics graphics,
            Rectangle bar,
            TimeSpan start,
            TimeSpan end,
            Color color)
        {
            if (TotalTime <= TimeSpan.Zero || end <= start)
            {
                return;
            }

            int x1 = TimeToX(start, bar);
            int x2 = TimeToX(end, bar);
            Rectangle rangeRectangle = new(
                Math.Min(x1, x2),
                bar.Top - 2,
                Math.Max(3, Math.Abs(x2 - x1)),
                bar.Height + 4);
            using var brush = new SolidBrush(Color.FromArgb(210, color));
            graphics.FillRectangle(brush, rangeRectangle);
        }

        private int TimeToX(TimeSpan time, Rectangle bar)
        {
            if (TotalTime <= TimeSpan.Zero)
            {
                return bar.Left;
            }

            double ratio = Math.Clamp(
                time.Ticks / (double)Math.Max(1, TotalTime.Ticks),
                0,
                1);
            return bar.Left + (int)Math.Round(ratio * bar.Width);
        }
    }
}
