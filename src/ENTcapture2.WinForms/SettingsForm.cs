using System.Drawing.Text;
using System.Text.Json;
using ENTcapture2.Core.Legacy;
using ENTcapture2.Core.Models;
using ENTcapture2.Core.Services;
using ENTcapture2.WinForms.Capture;
using ENTcapture2.WinForms.Ui;

namespace ENTcapture2.WinForms;

public partial class SettingsForm : Form
{
    private static readonly JsonSerializerOptions ExportSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly IReadOnlyList<CameraDeviceInfo> _devices;
    private bool _isLoading;
    private bool _overlayRawEdited;
    private CapturePreset? _editingPreset;
    private readonly PictureBox _sampleCoordinatePictureBox = new();
    private readonly Button _sampleLoadButton = new();
    private readonly TextBox _sampleXTextBox = new();
    private readonly TextBox _sampleYTextBox = new();
    private readonly Label _sampleImageInfoLabel = new();
    private readonly CheckBox _openRsBasePatientPageCheckBox = new();
    private readonly Label _rsBasePatientPageUrlLabel = new();
    private readonly TextBox _rsBasePatientPageUrlTextBox = new();
    private readonly Label _rsBasePatientPageDelayLabel = new();
    private readonly NumericUpDown _rsBasePatientPageDelayInput = new();
    private readonly Label _snapshotBestFrameWindowLabel = new();
    private readonly NumericUpDown _snapshotBestFrameWindowInput = new();
    private readonly Label _snapshotBestFrameWindowHintLabel = new();
    private Bitmap? _sampleCoordinateImage;

    public SettingsForm()
        : this(ApplicationSettings.CreateDefault(), [])
    {
    }

    public SettingsForm(
        ApplicationSettings settings,
        IReadOnlyList<CameraDeviceInfo> devices)
    {
        Settings = CloneSettings(settings);
        _devices = devices;
        InitializeComponent();
        _overlayGrid.CellValueChanged += OverlayGrid_Changed;
        _overlayGrid.RowsRemoved += OverlayGrid_Changed;
        _overlayGrid.UserAddedRow += OverlayGrid_Changed;
        _overlayGrid.UserDeletedRow += OverlayGrid_Changed;
        _videoCheckBox.CheckedChanged += PresetOptionCheckBox_CheckedChanged;
        _presetPreviewOnlyCheckBox.CheckedChanged +=
            PresetOptionCheckBox_CheckedChanged;
        ConfigureRuntimePresentation();
        FormClosed += (_, _) => DisposeSampleCoordinateImage();
        LoadSettings();
    }

    public ApplicationSettings Settings { get; private set; }

    private CapturePreset? SelectedPreset =>
        _presetList.SelectedItem as CapturePreset;

    private void ConfigureRuntimePresentation()
    {
        ClientSize = new Size(
            Math.Max(ClientSize.Width, 1240),
            Math.Max(ClientSize.Height, 940));
        MinimumSize = new Size(
            Math.Max(MinimumSize.Width, 1000),
            Math.Max(MinimumSize.Height, 720));

        Theme.Apply(this);
        ShowSettingsPage(false);
        ConfigureSampleCoordinatePicker();
        ConfigureRsBasePatientPageOption();
        ConfigureSnapshotBestFrameOption();

        _videoCheckBox.Text = "動画ファイリング";
        _presetPreviewOnlyCheckBox.Text = "プレビューのみ";
        importTitleLabel.Text = "設定のインポート・エクスポート";
        importDescriptionLabel.Text =
            "ドキュメントフォルダーの entcapture2.config を既定にして読み書きします。";
        _importLegacyButton.Text = "V1設定をインポート";
        _presetList.DisplayMember = nameof(CapturePreset.Name);
        _deviceComboBox.DisplayMember = nameof(CameraDeviceInfo.DisplayName);
        _deviceComboBox.Items.AddRange(_devices.Cast<object>().ToArray());
        _temporaryCodecComboBox.Items.Clear();
        _temporaryCodecComboBox.Items.AddRange(
            new object[]
            {
                "MJPG",
                "H264",
                "RAW"
            });
        _temporaryCodecComboBox.SelectedIndexChanged +=
            (_, _) => UpdateEncoderSelectionState();

        using var fonts = new InstalledFontCollection();
        _fontComboBox.Items.Add("Yu Gothic UI");
        foreach (FontFamily family in fonts.Families.OrderBy(item => item.Name))
        {
            if (!_fontComboBox.Items.Contains(family.Name))
            {
                _fontComboBox.Items.Add(family.Name);
            }
        }

    }

    private void NavigationButton_Click(object? sender, EventArgs e)
    {
        ShowSettingsPage(ReferenceEquals(sender, _presetsPageButton));
    }

    private void ShowSettingsPage(bool presetsPage)
    {
        if (settingsTabControl.TabPages.Count >= 2)
        {
            settingsTabControl.SelectedTab = presetsPage
                ? presetsTabPage
                : generalTabPage;
        }

        if (_generalPageButton is not null && _presetsPageButton is not null)
        {
            Theme.ApplyButton(_generalPageButton, !presetsPage);
            Theme.ApplyButton(_presetsPageButton, presetsPage);
        }
    }

    private void ConfigureSampleCoordinatePicker()
    {
        if (_sampleCoordinatePictureBox.Parent is not null)
        {
            return;
        }

        roiCardPanel.Height = Math.Max(roiCardPanel.Height, 360);
        roiCardPanel.MinimumSize = new Size(0, 360);
        if (roiLayout.RowStyles.Count >= 3)
        {
            roiLayout.RowStyles[2] = new RowStyle(SizeType.Absolute, 236F);
        }

        var sampleLayout = new TableLayoutPanel
        {
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 0),
            RowCount = 1,
            Tag = "raised"
        };
        sampleLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100F));
        sampleLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 190F));
        sampleLayout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100F));

        _sampleCoordinatePictureBox.BackColor = Color.Black;
        _sampleCoordinatePictureBox.BorderStyle = BorderStyle.FixedSingle;
        _sampleCoordinatePictureBox.Cursor = Cursors.Cross;
        _sampleCoordinatePictureBox.Dock = DockStyle.Fill;
        _sampleCoordinatePictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        _sampleCoordinatePictureBox.MouseClick +=
            SampleCoordinatePictureBox_MouseClick;
        sampleLayout.Controls.Add(_sampleCoordinatePictureBox, 0, 0);

        var sidePanel = new TableLayoutPanel
        {
            ColumnCount = 2,
            Dock = DockStyle.Top,
            Margin = new Padding(12, 0, 0, 0),
            RowCount = 5,
            Tag = "raised"
        };
        sidePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32F));
        sidePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));

        _sampleLoadButton.Dock = DockStyle.Fill;
        _sampleLoadButton.Text = "サンプル読込";
        _sampleLoadButton.Click += SampleLoadButton_Click;
        sidePanel.SetColumnSpan(_sampleLoadButton, 2);
        sidePanel.Controls.Add(_sampleLoadButton, 0, 0);

        var xLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "X",
            TextAlign = ContentAlignment.MiddleLeft,
            Tag = "text"
        };
        _sampleXTextBox.Dock = DockStyle.Fill;
        _sampleXTextBox.ReadOnly = true;
        sidePanel.Controls.Add(xLabel, 0, 1);
        sidePanel.Controls.Add(_sampleXTextBox, 1, 1);

        var yLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Y",
            TextAlign = ContentAlignment.MiddleLeft,
            Tag = "text"
        };
        _sampleYTextBox.Dock = DockStyle.Fill;
        _sampleYTextBox.ReadOnly = true;
        sidePanel.Controls.Add(yLabel, 0, 2);
        sidePanel.Controls.Add(_sampleYTextBox, 1, 2);

        _sampleImageInfoLabel.Dock = DockStyle.Fill;
        _sampleImageInfoLabel.Text = "画像未読込";
        _sampleImageInfoLabel.TextAlign = ContentAlignment.MiddleLeft;
        sidePanel.SetColumnSpan(_sampleImageInfoLabel, 2);
        sidePanel.Controls.Add(_sampleImageInfoLabel, 0, 3);

        var hintLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "クリックした画像上の座標を表示します。",
            Tag = "text"
        };
        sidePanel.SetColumnSpan(hintLabel, 2);
        sidePanel.Controls.Add(hintLabel, 0, 4);
        sampleLayout.Controls.Add(sidePanel, 1, 0);

        roiLayout.Controls.Add(sampleLayout, 0, 2);
        Theme.Apply(sampleLayout);
    }

    private void ConfigureRsBasePatientPageOption()
    {
        if (_openRsBasePatientPageCheckBox.Parent is not null)
        {
            return;
        }

        if (generalLayout.RowStyles.Count > 3)
        {
            generalLayout.RowStyles[3] = new RowStyle(SizeType.Absolute, 320F);
        }

        integrationCardPanel.MinimumSize = new Size(0, 320);
        importCardPanel.MinimumSize = new Size(0, 320);
        if (integrationLayout.RowCount < 7)
        {
            integrationLayout.RowCount = 7;
            integrationLayout.RowStyles.Clear();
            integrationLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            integrationLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            integrationLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            integrationLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            integrationLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            integrationLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            integrationLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
        }

        integrationLayout.SetRow(_autoFileCheckBox, 2);
        integrationLayout.SetColumn(_autoFileCheckBox, 0);
        integrationLayout.SetColumnSpan(_autoFileCheckBox, 3);
        _autoFileCheckBox.Margin = new Padding(3, 6, 0, 0);

        integrationLayout.SetRow(rsBaseReloadUrlLabel, 3);
        integrationLayout.SetColumn(_rsBaseReloadUrlTextBox, 1);
        integrationLayout.SetRow(_rsBaseReloadUrlTextBox, 3);
        integrationLayout.SetColumnSpan(_rsBaseReloadUrlTextBox, 2);
        _rsBaseReloadUrlTextBox.Margin = new Padding(3, 4, 3, 4);

        _openRsBasePatientPageCheckBox.AutoSize = true;
        _openRsBasePatientPageCheckBox.Margin = new Padding(3, 6, 0, 0);
        _openRsBasePatientPageCheckBox.Text =
            "取込後に患者ページをブラウザで開く";

        integrationLayout.SetColumn(_openRsBasePatientPageCheckBox, 0);
        integrationLayout.SetColumnSpan(_openRsBasePatientPageCheckBox, 3);
        integrationLayout.Controls.Add(_openRsBasePatientPageCheckBox, 0, 4);

        _rsBasePatientPageUrlLabel.Anchor = AnchorStyles.Left;
        _rsBasePatientPageUrlLabel.AutoSize = false;
        _rsBasePatientPageUrlLabel.Dock = DockStyle.Fill;
        _rsBasePatientPageUrlLabel.Text = "患者表示URL" +
            Environment.NewLine +
            "($iは患者ID)";
        _rsBasePatientPageUrlLabel.TextAlign = ContentAlignment.MiddleLeft;
        integrationLayout.SetRow(_rsBasePatientPageUrlLabel, 6);
        integrationLayout.SetColumn(_rsBasePatientPageUrlLabel, 0);
        integrationLayout.Controls.Add(_rsBasePatientPageUrlLabel, 0, 6);

        _rsBasePatientPageUrlTextBox.Anchor =
            AnchorStyles.Left | AnchorStyles.Right;
        _rsBasePatientPageUrlTextBox.Dock = DockStyle.None;
        _rsBasePatientPageUrlTextBox.Margin = new Padding(3, 15, 3, 4);
        _rsBasePatientPageUrlTextBox.PlaceholderText =
            "http://localhost/~rsn/N2017.cgi?id_change=$i==";
        integrationLayout.SetColumn(_rsBasePatientPageUrlTextBox, 1);
        integrationLayout.SetRow(_rsBasePatientPageUrlTextBox, 6);
        integrationLayout.SetColumnSpan(_rsBasePatientPageUrlTextBox, 2);
        integrationLayout.Controls.Add(_rsBasePatientPageUrlTextBox, 1, 6);

        _rsBasePatientPageDelayLabel.Anchor = AnchorStyles.Left;
        _rsBasePatientPageDelayLabel.AutoSize = true;
        _rsBasePatientPageDelayLabel.Text = "表示前待機(ms)";
        integrationLayout.Controls.Add(_rsBasePatientPageDelayLabel, 0, 5);

        _rsBasePatientPageDelayInput.Anchor = AnchorStyles.Left;
        _rsBasePatientPageDelayInput.Increment = 500;
        _rsBasePatientPageDelayInput.Maximum = 60000;
        _rsBasePatientPageDelayInput.Minimum = 0;
        _rsBasePatientPageDelayInput.Width = 100;
        integrationLayout.Controls.Add(_rsBasePatientPageDelayInput, 1, 5);

        Theme.Apply(integrationLayout);
    }

    private void ConfigureSnapshotBestFrameOption()
    {
        if (_snapshotBestFrameWindowInput.Parent is not null)
        {
            return;
        }

        if (hotkeyCardLayout.RowCount < 6)
        {
            hotkeyCardLayout.RowCount = 6;
            hotkeyCardLayout.RowStyles.Clear();
            hotkeyCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            hotkeyCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            hotkeyCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            hotkeyCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            hotkeyCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            hotkeyCardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        }

        hotkeyCardLayout.SetRow(snapshotHotkeyLabel, 3);
        hotkeyCardLayout.SetRow(_snapshotHotkeyTextBox, 3);
        hotkeyCardLayout.SetRow(captureHotkeyLabel, 4);
        hotkeyCardLayout.SetRow(_captureHotkeyTextBox, 4);
        hotkeyCardLayout.SetRow(label1, 5);

        _snapshotBestFrameWindowLabel.Anchor = AnchorStyles.Left;
        _snapshotBestFrameWindowLabel.AutoSize = true;
        _snapshotBestFrameWindowLabel.Text = "ブレ防止時間(ms)";

        _snapshotBestFrameWindowInput.Anchor = AnchorStyles.Left;
        _snapshotBestFrameWindowInput.Increment = 100;
        _snapshotBestFrameWindowInput.Maximum = 1000;
        _snapshotBestFrameWindowInput.Minimum = 0;
        _snapshotBestFrameWindowInput.Size = new Size(120, 23);
        _snapshotBestFrameWindowInput.Value = 300;

        _snapshotBestFrameWindowHintLabel.Anchor = AnchorStyles.Left;
        _snapshotBestFrameWindowHintLabel.AutoSize = true;
        _snapshotBestFrameWindowHintLabel.Margin = new Padding(8, 4, 0, 0);
        _snapshotBestFrameWindowHintLabel.Text = "0で無効";
        _snapshotBestFrameWindowHintLabel.Tag = "muted";

        var bestFramePanel = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = Padding.Empty,
            WrapContents = false
        };
        bestFramePanel.Controls.Add(_snapshotBestFrameWindowInput);
        bestFramePanel.Controls.Add(_snapshotBestFrameWindowHintLabel);

        hotkeyCardLayout.Controls.Add(_snapshotBestFrameWindowLabel, 0, 2);
        hotkeyCardLayout.Controls.Add(bestFramePanel, 1, 2);
        Theme.Apply(_snapshotBestFrameWindowLabel);
        Theme.Apply(_snapshotBestFrameWindowInput);
        Theme.Apply(_snapshotBestFrameWindowHintLabel);
    }

    private void LoadSettings()
    {
        _isLoading = true;
        try
        {
            _snapshotDirectoryTextBox.Text = Settings.SnapshotDirectory;
            _jpegQualityInput.Value = Math.Clamp(Settings.JpegQuality, 1, 100);
            _snapshotDebounceInput.Value = Math.Clamp(
                Settings.SnapshotDebounceMilliseconds,
                0,
                10000);
            _snapshotBestFrameWindowInput.Value = Math.Clamp(
                Settings.SnapshotBestFrameWindowMilliseconds,
                0,
                1000);
            _topMostDuringPreviewCheckBox.Checked =
                Settings.TopMostDuringPreview;
            _temporaryDirectoryTextBox.Text =
                Settings.TemporaryRecordingDirectory;
            _temporaryCodecComboBox.Text =
                Settings.TemporaryRecordingCodec;
            _h264EncoderComboBox.Text =
                Settings.H264EncoderPreference switch
                {
                    "NVENC" => "NVENC",
                    "AMF" => "AMF",
                    "QSV" => "QSV",
                    _ => "自動"
                };
            _segmentMinutesInput.Value =
                Math.Clamp(Settings.RecordingSegmentMinutes, 1, 240);
            _retentionDaysInput.Value =
                Math.Clamp(Settings.TemporaryFileRetentionDays, 0, 3650);
            _maximumFpsInput.Value =
                Math.Clamp(Settings.MaximumPreviewFramesPerSecond, 0, 240);
            _reencodeThresholdInput.Value =
                Math.Clamp(Settings.ReencodeThresholdMegabytes, 0, 1000000);
            _finalCodecComboBox.Text = Settings.FinalVideoCodec;
            _finalQualityComboBox.Text =
                ToFinalVideoQualityDisplayName(Settings.FinalVideoQuality);
            _examinationTypesTextBox.Text = Settings.ExaminationTypes;
            LoadHotkey(_snapshotHotkeyTextBox, Settings.SnapshotHotkey);
            LoadHotkey(_captureHotkeyTextBox, Settings.CaptureHotkey);
            _rsBaseDirectoryTextBox.Text = Settings.RsBaseTheptDirectory;
            _rsBaseReloadUrlTextBox.Text = Settings.RsBaseReloadUrl;
            _autoFileCheckBox.Checked = Settings.AutoFileToRsBase;
            _openRsBasePatientPageCheckBox.Checked =
                Settings.OpenRsBasePatientPageAfterFiling;
            _rsBasePatientPageUrlTextBox.Text =
                Settings.RsBasePatientPageUrlTemplate;
            _rsBasePatientPageDelayInput.Value = Math.Clamp(
                Settings.RsBasePatientPageDelayMilliseconds,
                0,
                60000);
            RefreshPresetList(Settings.SelectedPresetId);
        }
        finally
        {
            _isLoading = false;
        }

        LoadPresetEditor(SelectedPreset);
        UpdateEncoderSelectionState();
    }

    private void UpdateEncoderSelectionState()
    {
        _h264EncoderComboBox.Enabled = string.Equals(
            _temporaryCodecComboBox.Text,
            "H264",
            StringComparison.OrdinalIgnoreCase);
    }

    private static string ToFinalVideoQualityDisplayName(string? value)
    {
        return value?.Trim() switch
        {
            "Size" => "サイズ優先",
            "High" => "高品質",
            "Highest" => "最高品質",
            _ => "標準"
        };
    }

    private static string FromFinalVideoQualityDisplayName(string? value)
    {
        return value?.Trim() switch
        {
            "サイズ優先" => "Size",
            "高品質" => "High",
            "最高品質" => "Highest",
            _ => "Standard"
        };
    }

    private void RefreshPresetList(Guid? selectedId)
    {
        _presetList.BeginUpdate();
        try
        {
            _presetList.Items.Clear();
            _presetList.Items.AddRange(Settings.Presets.Cast<object>().ToArray());
        }
        finally
        {
            _presetList.EndUpdate();
        }

        if (_presetList.Items.Count == 0)
        {
            return;
        }

        int index = selectedId.HasValue
            ? Settings.Presets.FindIndex(item => item.Id == selectedId.Value)
            : 0;
        _presetList.SelectedIndex = index >= 0 ? index : 0;
    }

    private void PresetList_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        SavePresetEditor();
        LoadPresetEditor(SelectedPreset);
    }

    private void LoadPresetEditor(CapturePreset? preset)
    {
        _isLoading = true;
        _editingPreset = preset;
        try
        {
            bool enabled = preset is not null;
            presetEditorPanel.Enabled = enabled;
            if (preset is null)
            {
                ClearPresetEditor();
                return;
            }

            _nameTextBox.Text = preset.Name;
            _examinationTextBox.Text = preset.ExaminationType;
            _videoCheckBox.Checked = preset.IsVideo;
            _presetPreviewOnlyCheckBox.Checked = preset.PreviewOnly;
            ApplyPresetOptionRules(null);
            _fpsInput.Value = Math.Clamp(preset.FramesPerSecond, 0, 240);

            CameraDeviceInfo? device =
                FindDevice(preset) ?? CreateMissingDeviceInfo(preset);
            EnsureDeviceItem(device);
            _deviceComboBox.SelectedItem = device;
            LoadResolutions(device, preset.Resolution);

            int[] roi = ParseRoi(preset.Roi);
            _roiX1Input.Value = roi[0];
            _roiY1Input.Value = roi[1];
            _roiX2Input.Value = roi[2];
            _roiY2Input.Value = roi[3];

            string fontName = string.IsNullOrWhiteSpace(preset.FontName)
                ? "Yu Gothic UI"
                : preset.FontName;
            _fontComboBox.Text = fontName;
            _overlayRawEdited = false;
            _overlayRawTextBox.Text = preset.OverlayText;
            LoadOverlayRows(preset.OverlayText);
            _overlayRawEdited = true;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void ClearPresetEditor()
    {
        _nameTextBox.Clear();
        _examinationTextBox.Clear();
        _deviceComboBox.SelectedIndex = -1;
        _resolutionComboBox.Items.Clear();
        _fpsInput.Value = 0;
        _videoCheckBox.Checked = false;
        _presetPreviewOnlyCheckBox.Checked = false;
        _roiX1Input.Value = 0;
        _roiY1Input.Value = 0;
        _roiX2Input.Value = 0;
        _roiY2Input.Value = 0;
        _fontComboBox.Text = "Yu Gothic UI";
        _overlayGrid.Rows.Clear();
        _overlayRawTextBox.Clear();
        _overlayRawEdited = false;
    }

    private void SavePresetEditor()
    {
        if (_isLoading || _editingPreset is null)
        {
            return;
        }

        CapturePreset preset = _editingPreset;
        preset.Name = string.IsNullOrWhiteSpace(_nameTextBox.Text)
            ? "新しいプリセット"
            : _nameTextBox.Text.Trim();
        preset.ExaminationType = _examinationTextBox.Text.Trim();
        preset.IsVideo = _videoCheckBox.Checked;
        preset.PreviewOnly = _presetPreviewOnlyCheckBox.Checked &&
            !preset.IsVideo;
        preset.FramesPerSecond = decimal.ToInt32(_fpsInput.Value);
        preset.FontName = _fontComboBox.Text.Trim();

        if (_deviceComboBox.SelectedItem is CameraDeviceInfo { IsMissing: false } device)
        {
            preset.DeviceId = device.MonikerString;
            preset.DeviceName = device.Name;
            preset.DeviceIndex = device.Index;
        }

        if (_resolutionComboBox.SelectedItem is CaptureResolution resolution)
        {
            preset.Resolution = resolution;
        }

        int x1 = decimal.ToInt32(_roiX1Input.Value);
        int y1 = decimal.ToInt32(_roiY1Input.Value);
        int x2 = decimal.ToInt32(_roiX2Input.Value);
        int y2 = decimal.ToInt32(_roiY2Input.Value);
        preset.Roi = x1 == 0 && y1 == 0 && x2 == 0 && y2 == 0
            ? string.Empty
            : $"{x1},{y1},{x2},{y2}";
        preset.OverlayText = _overlayRawEdited
            ? _overlayRawTextBox.Text.Trim()
            : SerializeOverlayRows();
        _presetList.Refresh();
    }

    private void ApplyPresetOptionRules(object? changedControl)
    {
        if (ReferenceEquals(changedControl, _videoCheckBox) &&
            _videoCheckBox.Checked)
        {
            _presetPreviewOnlyCheckBox.Checked = false;
        }
        else if (ReferenceEquals(changedControl, _presetPreviewOnlyCheckBox) &&
                 _presetPreviewOnlyCheckBox.Checked)
        {
            _videoCheckBox.Checked = false;
        }
        else if (_videoCheckBox.Checked &&
                 _presetPreviewOnlyCheckBox.Checked)
        {
            _presetPreviewOnlyCheckBox.Checked = false;
        }
    }

    private void PresetOptionCheckBox_CheckedChanged(
        object? sender,
        EventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        try
        {
            ApplyPresetOptionRules(sender);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void DeviceComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        LoadResolutions(
            _deviceComboBox.SelectedItem as CameraDeviceInfo,
            null);
    }

    private void LoadResolutions(
        CameraDeviceInfo? device,
        CaptureResolution? selectedResolution)
    {
        _resolutionComboBox.BeginUpdate();
        try
        {
            _resolutionComboBox.Items.Clear();
            if (device is null || device.IsMissing)
            {
                if (selectedResolution is not null)
                {
                    _resolutionComboBox.Items.Add(selectedResolution);
                }
            }
            else
            {
                _resolutionComboBox.Items.AddRange(
                    device.Resolutions.Cast<object>().ToArray());
            }
        }
        finally
        {
            _resolutionComboBox.EndUpdate();
        }

        if (_resolutionComboBox.Items.Count == 0)
        {
            return;
        }

        int index =
            selectedResolution is null ||
            selectedResolution.Width <= 0 ||
            selectedResolution.Height <= 0
                ? GetBestResolutionIndex(_resolutionComboBox)
                : _resolutionComboBox.Items.IndexOf(selectedResolution);
        _resolutionComboBox.SelectedIndex = index >= 0 ? index : 0;
    }

    private static int GetBestResolutionIndex(ComboBox comboBox)
    {
        return comboBox.Items
            .Cast<object>()
            .Select((item, index) => new { item, index })
            .Where(entry =>
                entry.item is CaptureResolution resolution &&
                resolution.Width > 0 &&
                resolution.Height > 0)
            .OrderByDescending(entry =>
            {
                var resolution = (CaptureResolution)entry.item;
                return resolution.Width * resolution.Height;
            })
            .ThenByDescending(entry =>
                ((CaptureResolution)entry.item).FramesPerSecond)
            .ThenBy(entry =>
                GetCapturePixelFormatPriority(
                    ((CaptureResolution)entry.item).PixelFormat))
            .Select(entry => entry.index)
            .FirstOrDefault(-1);
    }

    private static int GetCapturePixelFormatPriority(string pixelFormat)
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

    private CameraDeviceInfo? FindDevice(CapturePreset preset)
    {
        return _devices.FirstOrDefault(
                item =>
                    !string.IsNullOrWhiteSpace(preset.DeviceId) &&
                    string.Equals(
                        item.MonikerString,
                        preset.DeviceId,
                        StringComparison.OrdinalIgnoreCase))
            ?? _devices.FirstOrDefault(
                item =>
                    string.IsNullOrWhiteSpace(preset.DeviceId) &&
                    string.Equals(
                    item.Name,
                    preset.DeviceName,
                    StringComparison.OrdinalIgnoreCase));
    }

    private void EnsureDeviceItem(CameraDeviceInfo? device)
    {
        if (device is null || _deviceComboBox.Items.Contains(device))
        {
            return;
        }

        _deviceComboBox.Items.Add(device);
    }

    private static CameraDeviceInfo? CreateMissingDeviceInfo(CapturePreset preset)
    {
        if (string.IsNullOrWhiteSpace(preset.DeviceId) &&
            string.IsNullOrWhiteSpace(preset.DeviceName))
        {
            return null;
        }

        string name = string.IsNullOrWhiteSpace(preset.DeviceName)
            ? preset.DeviceId
            : preset.DeviceName;
        return new CameraDeviceInfo(
            preset.DeviceIndex,
            name,
            preset.DeviceId,
            [],
            true);
    }

    private void BrowseDirectoryButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "静止画・動画の保存先を選択してください",
            SelectedPath = _snapshotDirectoryTextBox.Text,
            ShowNewFolderButton = true
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _snapshotDirectoryTextBox.Text = dialog.SelectedPath;
        }
    }

    private void SampleLoadButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter =
                "画像ファイル (*.bmp;*.jpg;*.jpeg;*.png;*.gif)|*.bmp;*.jpg;*.jpeg;*.png;*.gif|すべてのファイル (*.*)|*.*",
            Title = "座標確認用のサンプル画像を選択"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            using Image loaded = Image.FromFile(dialog.FileName);
            var bitmap = new Bitmap(loaded);
            DisposeSampleCoordinateImage();
            _sampleCoordinateImage = bitmap;
            _sampleCoordinatePictureBox.Image = _sampleCoordinateImage;
            _sampleXTextBox.Clear();
            _sampleYTextBox.Clear();
            _sampleImageInfoLabel.Text =
                $"{_sampleCoordinateImage.Width} x {_sampleCoordinateImage.Height}";
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                $"画像を読み込めませんでした。\r\n{exception.Message}",
                "サンプル読込",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void SampleCoordinatePictureBox_MouseClick(
        object? sender,
        MouseEventArgs e)
    {
        if (_sampleCoordinateImage is null ||
            _sampleCoordinatePictureBox.Image is null)
        {
            return;
        }

        Rectangle imageBounds = GetZoomedImageBounds(
            _sampleCoordinatePictureBox,
            _sampleCoordinateImage.Size);
        if (!imageBounds.Contains(e.Location) ||
            imageBounds.Width <= 0 ||
            imageBounds.Height <= 0)
        {
            return;
        }

        int x = (int)Math.Floor(
            (e.X - imageBounds.Left) *
            _sampleCoordinateImage.Width /
            (double)imageBounds.Width);
        int y = (int)Math.Floor(
            (e.Y - imageBounds.Top) *
            _sampleCoordinateImage.Height /
            (double)imageBounds.Height);
        x = Math.Clamp(x, 0, _sampleCoordinateImage.Width - 1);
        y = Math.Clamp(y, 0, _sampleCoordinateImage.Height - 1);

        _sampleXTextBox.Text = x.ToString();
        _sampleYTextBox.Text = y.ToString();
    }

    private static Rectangle GetZoomedImageBounds(
        PictureBox pictureBox,
        Size imageSize)
    {
        if (imageSize.Width <= 0 ||
            imageSize.Height <= 0 ||
            pictureBox.ClientSize.Width <= 0 ||
            pictureBox.ClientSize.Height <= 0)
        {
            return Rectangle.Empty;
        }

        double imageRatio = imageSize.Width / (double)imageSize.Height;
        double boxRatio =
            pictureBox.ClientSize.Width / (double)pictureBox.ClientSize.Height;

        int width;
        int height;
        if (boxRatio > imageRatio)
        {
            height = pictureBox.ClientSize.Height;
            width = (int)Math.Round(height * imageRatio);
        }
        else
        {
            width = pictureBox.ClientSize.Width;
            height = (int)Math.Round(width / imageRatio);
        }

        int left = (pictureBox.ClientSize.Width - width) / 2;
        int top = (pictureBox.ClientSize.Height - height) / 2;
        return new Rectangle(left, top, width, height);
    }

    private void DisposeSampleCoordinateImage()
    {
        _sampleCoordinatePictureBox.Image = null;
        _sampleCoordinateImage?.Dispose();
        _sampleCoordinateImage = null;
    }

    private void BrowseTemporaryDirectoryButton_Click(
        object? sender,
        EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "一次録画ファイルの保存先を選択してください",
            SelectedPath = _temporaryDirectoryTextBox.Text,
            ShowNewFolderButton = true
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _temporaryDirectoryTextBox.Text = dialog.SelectedPath;
        }
    }

    private void BrowseRsBaseDirectoryButton_Click(
        object? sender,
        EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "RSBaseのthept.txtがあるフォルダーを選択してください",
            SelectedPath = _rsBaseDirectoryTextBox.Text,
            ShowNewFolderButton = false
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _rsBaseDirectoryTextBox.Text = dialog.SelectedPath;
        }
    }

    private void HotkeyTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox ||
            e.KeyCode is Keys.ControlKey or Keys.ShiftKey or Keys.Menu)
        {
            return;
        }

        textBox.Tag = new HotkeySettings
        {
            Control = e.Control,
            Shift = e.Shift,
            KeyCode = e.KeyValue
        };
        textBox.Text = FormatHotkey((HotkeySettings)textBox.Tag);
        e.SuppressKeyPress = true;
        e.Handled = true;
    }

    private static void LoadHotkey(
        TextBox textBox,
        HotkeySettings settings)
    {
        textBox.Tag = settings.Clone();
        textBox.Text = FormatHotkey(settings);
    }

    private static string FormatHotkey(HotkeySettings settings)
    {
        if (settings.KeyCode <= 0)
        {
            return "未設定";
        }

        var parts = new List<string>();
        if (settings.Control)
        {
            parts.Add("Ctrl");
        }

        if (settings.Shift)
        {
            parts.Add("Shift");
        }

        parts.Add(((Keys)settings.KeyCode).ToString());
        return string.Join(" + ", parts);
    }

    private void ImportLegacyButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "ENTcapture V1設定ファイルを選択",
            Filter = "ENTcapture設定 (*.config)|*.config|すべてのファイル (*.*)|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (MessageBox.Show(
                this,
                "編集中の設定をV1設定で置き換えます。よろしいですか？",
                "V1設定のインポート",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            LegacySettingsImportResult result =
                LegacySettingsImporter.Import(dialog.FileName);
            ResolveLegacyDevices(result.Settings.Presets, result.Warnings);
            Settings = result.Settings;
            LoadSettings();

            string details = result.Warnings.Count == 0
                ? string.Empty
                : $"\r\n\r\n確認事項:\r\n・{string.Join("\r\n・", result.Warnings)}";
            MessageBox.Show(
                this,
                $"V1のプリセットを{Settings.Presets.Count}件読み込みました。{details}",
                "インポート完了",
                MessageBoxButtons.OK,
                result.Warnings.Count == 0
                    ? MessageBoxIcon.Information
                    : MessageBoxIcon.Warning);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                $"V1設定を読み込めませんでした。\r\n{exception.Message}",
                "インポートエラー",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void ExportSettingsButton_Click(object? sender, EventArgs e)
    {
        ApplyEditorValuesToSettings();

        using var dialog = new SaveFileDialog
        {
            Title = "設定を書き出し",
            Filter = "ENTcapture設定 (*.config)|*.config|JSON (*.json)|*.json|すべてのファイル (*.*)|*.*",
            InitialDirectory = GetDocumentsDirectory(),
            FileName = "entcapture2.config",
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            using FileStream stream = File.Create(dialog.FileName);
            JsonSerializer.Serialize(
                stream,
                Settings,
                ExportSerializerOptions);
            MessageBox.Show(
                this,
                "設定を書き出しました。",
                "ENTcapture2",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                $"設定を書き出せませんでした。\r\n{exception.Message}",
                "ENTcapture2",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void ImportSettingsButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "設定を読み込み",
            Filter = "ENTcapture設定 (*.config;*.json)|*.config;*.json|すべてのファイル (*.*)|*.*",
            InitialDirectory = GetDocumentsDirectory(),
            FileName = "entcapture2.config",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (MessageBox.Show(
                this,
                "現在編集中の設定を、読み込んだ設定で置き換えます。よろしいですか？",
                "設定の読み込み",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            using FileStream stream = File.OpenRead(dialog.FileName);
            ApplicationSettings? imported =
                JsonSerializer.Deserialize<ApplicationSettings>(
                    stream,
                    ExportSerializerOptions);
            if (imported is null)
            {
                throw new InvalidDataException("設定ファイルを読み込めませんでした。");
            }

            JsonSettingsStore.Validate(imported);
            Settings = CloneSettings(imported);
            LoadSettings();
            MessageBox.Show(
                this,
                "設定を読み込みました。",
                "ENTcapture2",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                $"設定を読み込めませんでした。\r\n{exception.Message}",
                "ENTcapture2",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static string GetDocumentsDirectory()
    {
        return Environment.GetFolderPath(
            Environment.SpecialFolder.MyDocuments);
    }

    private void ResolveLegacyDevices(
        IEnumerable<CapturePreset> presets,
        ICollection<string> warnings)
    {
        foreach (CapturePreset preset in presets)
        {
            if (string.IsNullOrWhiteSpace(preset.DeviceName))
            {
                continue;
            }

            CameraDeviceInfo? device = _devices.FirstOrDefault(
                    item => string.Equals(
                        item.Name,
                        preset.DeviceName,
                        StringComparison.OrdinalIgnoreCase))
                ?? _devices.FirstOrDefault(
                    item =>
                        item.Name.Contains(
                            preset.DeviceName,
                            StringComparison.OrdinalIgnoreCase) ||
                        preset.DeviceName.Contains(
                            item.Name,
                            StringComparison.OrdinalIgnoreCase));
            if (device is null)
            {
                warnings.Add(
                    $"{preset.Name}: カメラ「{preset.DeviceName}」が見つかりません。");
                continue;
            }

            preset.DeviceId = device.MonikerString;
            preset.DeviceName = device.Name;
            preset.DeviceIndex = device.Index;
            if (preset.LegacyResolutionIndex >= 0 &&
                preset.LegacyResolutionIndex < device.Resolutions.Count)
            {
                preset.Resolution =
                    device.Resolutions[preset.LegacyResolutionIndex];
            }
            else if (preset.LegacyResolutionIndex >= 0)
            {
                warnings.Add(
                    $"{preset.Name}: 旧解像度番号{preset.LegacyResolutionIndex}を変換できません。");
            }
        }
    }

    private void AddPresetButton_Click(object? sender, EventArgs e)
    {
        SavePresetEditor();
        var preset = new CapturePreset();
        Settings.Presets.Add(preset);
        RefreshPresetList(preset.Id);
    }

    private void DuplicatePresetButton_Click(object? sender, EventArgs e)
    {
        SavePresetEditor();
        if (SelectedPreset is not CapturePreset selected)
        {
            return;
        }

        CapturePreset clone = selected.Clone();
        Settings.Presets.Insert(_presetList.SelectedIndex + 1, clone);
        RefreshPresetList(clone.Id);
    }

    private void DeletePresetButton_Click(object? sender, EventArgs e)
    {
        if (_presetList.SelectedIndex < 0)
        {
            return;
        }

        int index = _presetList.SelectedIndex;
        Settings.Presets.RemoveAt(index);
        _editingPreset = null;
        RefreshPresetList(null);
        if (_presetList.Items.Count > 0)
        {
            _presetList.SelectedIndex =
                Math.Min(index, _presetList.Items.Count - 1);
        }
        else
        {
            LoadPresetEditor(null);
        }
    }

    private void MovePresetButton_Click(object? sender, EventArgs e)
    {
        SavePresetEditor();
        int offset = ReferenceEquals(sender, _moveUpButton) ? -1 : 1;
        int oldIndex = _presetList.SelectedIndex;
        int newIndex = oldIndex + offset;
        if (oldIndex < 0 || newIndex < 0 || newIndex >= Settings.Presets.Count)
        {
            return;
        }

        CapturePreset preset = Settings.Presets[oldIndex];
        Settings.Presets.RemoveAt(oldIndex);
        Settings.Presets.Insert(newIndex, preset);
        RefreshPresetList(preset.Id);
    }

    private void ApplyEditorValuesToSettings()
    {
        SavePresetEditor();
        Settings.SnapshotDirectory = string.IsNullOrWhiteSpace(
            _snapshotDirectoryTextBox.Text)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                "ENTcapture2")
            : _snapshotDirectoryTextBox.Text.Trim();
        Settings.JpegQuality = decimal.ToInt32(_jpegQualityInput.Value);
        Settings.SnapshotDebounceMilliseconds =
            decimal.ToInt32(_snapshotDebounceInput.Value);
        Settings.SnapshotBestFrameWindowMilliseconds =
            decimal.ToInt32(_snapshotBestFrameWindowInput.Value);
        Settings.TopMostDuringPreview = _topMostDuringPreviewCheckBox.Checked;
        Settings.TemporaryRecordingDirectory =
            string.IsNullOrWhiteSpace(_temporaryDirectoryTextBox.Text)
                ? Path.Combine(Path.GetTempPath(), "ENTcapture2")
                : _temporaryDirectoryTextBox.Text.Trim();
        Settings.TemporaryRecordingCodec =
            string.IsNullOrWhiteSpace(_temporaryCodecComboBox.Text)
                ? "MJPG"
                : _temporaryCodecComboBox.Text.Trim().ToUpperInvariant();
        Settings.H264EncoderPreference =
            _h264EncoderComboBox.Text switch
            {
                "NVENC" => "NVENC",
                "AMF" => "AMF",
                "QSV" => "QSV",
                _ => "AUTO"
            };
        Settings.RecordingSegmentMinutes =
            decimal.ToInt32(_segmentMinutesInput.Value);
        Settings.TemporaryFileRetentionDays =
            decimal.ToInt32(_retentionDaysInput.Value);
        Settings.MaximumPreviewFramesPerSecond =
            decimal.ToInt32(_maximumFpsInput.Value);
        Settings.ReencodeThresholdMegabytes =
            decimal.ToInt32(_reencodeThresholdInput.Value);
        Settings.FinalVideoCodec =
            string.IsNullOrWhiteSpace(_finalCodecComboBox.Text)
                ? "AUTO"
                : _finalCodecComboBox.Text.Trim();
        Settings.FinalVideoQuality =
            FromFinalVideoQualityDisplayName(_finalQualityComboBox.Text);
        Settings.ExaminationTypes = _examinationTypesTextBox.Text.Trim();
        Settings.SnapshotHotkey =
            (_snapshotHotkeyTextBox.Tag as HotkeySettings)?.Clone()
            ?? new HotkeySettings();
        Settings.CaptureHotkey =
            (_captureHotkeyTextBox.Tag as HotkeySettings)?.Clone()
            ?? new HotkeySettings();
        Settings.RsBaseTheptDirectory =
            _rsBaseDirectoryTextBox.Text.Trim();
        Settings.RsBaseReloadUrl =
            _rsBaseReloadUrlTextBox.Text.Trim();
        Settings.AutoFileToRsBase = _autoFileCheckBox.Checked;
        Settings.OpenRsBasePatientPageAfterFiling =
            _openRsBasePatientPageCheckBox.Checked;
        Settings.RsBasePatientPageUrlTemplate =
            _rsBasePatientPageUrlTextBox.Text.Trim();
        Settings.RsBasePatientPageDelayMilliseconds =
            decimal.ToInt32(_rsBasePatientPageDelayInput.Value);
        if (Settings.SelectedPresetId is null ||
            Settings.Presets.All(item => item.Id != Settings.SelectedPresetId))
        {
            Settings.SelectedPresetId = Settings.Presets.FirstOrDefault()?.Id;
        }
    }

    private void OkButton_Click(object? sender, EventArgs e)
    {
        SavePresetEditor();
        if (string.Equals(
                _temporaryCodecComboBox.Text,
                "RAW",
                StringComparison.OrdinalIgnoreCase) &&
            MessageBox.Show(
                this,
                "RAW録画は非常に大きなファイルになります。設定を保存しますか？",
                "RAW録画",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
        {
            return;
        }

        if (!ValidateImageSettings())
        {
            ShowSettingsPage(true);
            return;
        }

        if (Settings.Presets.Count == 0)
        {
            MessageBox.Show(
                this,
                "プリセットを1件以上作成してください。",
                "設定",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        Settings.SnapshotDirectory = string.IsNullOrWhiteSpace(
            _snapshotDirectoryTextBox.Text)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                "ENTcapture2")
            : _snapshotDirectoryTextBox.Text.Trim();
        Settings.JpegQuality = decimal.ToInt32(_jpegQualityInput.Value);
        Settings.SnapshotDebounceMilliseconds =
            decimal.ToInt32(_snapshotDebounceInput.Value);
        Settings.SnapshotBestFrameWindowMilliseconds =
            decimal.ToInt32(_snapshotBestFrameWindowInput.Value);
        Settings.TopMostDuringPreview = _topMostDuringPreviewCheckBox.Checked;
        Settings.TemporaryRecordingDirectory =
            string.IsNullOrWhiteSpace(_temporaryDirectoryTextBox.Text)
                ? Path.Combine(Path.GetTempPath(), "ENTcapture2")
                : _temporaryDirectoryTextBox.Text.Trim();
        Settings.TemporaryRecordingCodec =
            string.IsNullOrWhiteSpace(_temporaryCodecComboBox.Text)
                ? "MJPG"
                : _temporaryCodecComboBox.Text.Trim().ToUpperInvariant();
        Settings.H264EncoderPreference =
            _h264EncoderComboBox.Text switch
            {
                "NVENC" => "NVENC",
                "AMF" => "AMF",
                "QSV" => "QSV",
                _ => "AUTO"
            };
        Settings.RecordingSegmentMinutes =
            decimal.ToInt32(_segmentMinutesInput.Value);
        Settings.TemporaryFileRetentionDays =
            decimal.ToInt32(_retentionDaysInput.Value);
        Settings.MaximumPreviewFramesPerSecond =
            decimal.ToInt32(_maximumFpsInput.Value);
        Settings.ReencodeThresholdMegabytes =
            decimal.ToInt32(_reencodeThresholdInput.Value);
        Settings.FinalVideoCodec =
            string.IsNullOrWhiteSpace(_finalCodecComboBox.Text)
                ? "AUTO"
                : _finalCodecComboBox.Text.Trim();
        Settings.FinalVideoQuality =
            FromFinalVideoQualityDisplayName(_finalQualityComboBox.Text);
        Settings.ExaminationTypes = _examinationTypesTextBox.Text.Trim();
        Settings.SnapshotHotkey =
            (_snapshotHotkeyTextBox.Tag as HotkeySettings)?.Clone()
            ?? new HotkeySettings();
        Settings.CaptureHotkey =
            (_captureHotkeyTextBox.Tag as HotkeySettings)?.Clone()
            ?? new HotkeySettings();
        Settings.RsBaseTheptDirectory =
            _rsBaseDirectoryTextBox.Text.Trim();
        Settings.RsBaseReloadUrl =
            _rsBaseReloadUrlTextBox.Text.Trim();
        Settings.AutoFileToRsBase = _autoFileCheckBox.Checked;
        Settings.OpenRsBasePatientPageAfterFiling =
            _openRsBasePatientPageCheckBox.Checked;
        Settings.RsBasePatientPageUrlTemplate =
            _rsBasePatientPageUrlTextBox.Text.Trim();
        Settings.RsBasePatientPageDelayMilliseconds =
            decimal.ToInt32(_rsBasePatientPageDelayInput.Value);
        if (Settings.SelectedPresetId is null ||
            Settings.Presets.All(item => item.Id != Settings.SelectedPresetId))
        {
            Settings.SelectedPresetId = Settings.Presets[0].Id;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private bool ValidateImageSettings()
    {
        foreach (CapturePreset preset in Settings.Presets)
        {
            int[] roi = ParseRoi(preset.Roi);
            bool roiEnabled = roi.Any(value => value != 0);
            if (roiEnabled &&
                (roi[2] <= roi[0] || roi[3] <= roi[1]))
            {
                _presetList.SelectedItem = preset;
                MessageBox.Show(
                    this,
                    $"{preset.Name}のROIは、X2をX1より大きく、Y2をY1より大きくしてください。",
                    "ROI設定",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }
        }

        return true;
    }

    private void LoadOverlayRows(string value)
    {
        _overlayGrid.Rows.Clear();
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        string[] parts = value.Split(',');
        for (int index = 0; index + 4 < parts.Length; index += 5)
        {
            _overlayGrid.Rows.Add(
                parts[index],
                parts[index + 1],
                parts[index + 2],
                parts[index + 3],
                parts[index + 4]);
        }
    }

    private void OverlayRawTextBox_TextChanged(object? sender, EventArgs e)
    {
        if (!_isLoading)
        {
            _overlayRawEdited = true;
        }
    }

    private void OverlayGrid_Changed(object? sender, EventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        _overlayRawEdited = false;
        string serialized = SerializeOverlayRows();
        bool wasLoading = _isLoading;
        _isLoading = true;
        try
        {
            _overlayRawTextBox.Text = serialized;
        }
        finally
        {
            _isLoading = wasLoading;
        }
    }

    private string SerializeOverlayRows()
    {
        var values = new List<string>();
        foreach (DataGridViewRow row in _overlayGrid.Rows)
        {
            if (row.IsNewRow)
            {
                continue;
            }

            string text = Convert.ToString(row.Cells[4].Value)?.Trim()
                ?? string.Empty;
            if (text.Length == 0)
            {
                continue;
            }

            values.Add(CellText(row, 0, "0"));
            values.Add(CellText(row, 1, "0"));
            values.Add(CellText(row, 2, "24"));
            values.Add(CellText(row, 3, "FFFFFF"));
            values.Add(text.Replace(",", "，"));
        }

        return string.Join(",", values);
    }

    private static string CellText(
        DataGridViewRow row,
        int index,
        string fallback)
    {
        string value = Convert.ToString(row.Cells[index].Value)?.Trim()
            ?? string.Empty;
        return value.Length == 0 ? fallback : value;
    }

    private static int[] ParseRoi(string value)
    {
        string[] parts = value.Split(',');
        if (parts.Length != 4)
        {
            return [0, 0, 0, 0];
        }

        return parts
            .Select(item => int.TryParse(item, out int number) ? number : 0)
            .ToArray();
    }

    private static ApplicationSettings CloneSettings(
        ApplicationSettings source)
    {
        return new ApplicationSettings
        {
            SchemaVersion = source.SchemaVersion,
            SelectedPresetId = source.SelectedPresetId,
            SnapshotDirectory = source.SnapshotDirectory,
            JpegQuality = source.JpegQuality,
            SnapshotDebounceMilliseconds =
                source.SnapshotDebounceMilliseconds,
            SnapshotBestFrameWindowMilliseconds =
                source.SnapshotBestFrameWindowMilliseconds,
            CaptureMode = CaptureOperationMode.ContinuousTemporaryRecording,
            TopMostDuringPreview = source.TopMostDuringPreview,
            TemporaryRecordingDirectory = source.TemporaryRecordingDirectory,
            TemporaryRecordingCodec = source.TemporaryRecordingCodec,
            H264EncoderPreference = source.H264EncoderPreference,
            RecordingSegmentMinutes = source.RecordingSegmentMinutes,
            TemporaryFileRetentionDays = source.TemporaryFileRetentionDays,
            MaximumPreviewFramesPerSecond =
                source.MaximumPreviewFramesPerSecond,
            ReencodeThresholdMegabytes =
                source.ReencodeThresholdMegabytes,
            FinalVideoCodec = source.FinalVideoCodec,
            FinalVideoQuality = source.FinalVideoQuality,
            FinalVideoBitrateKbps = source.FinalVideoBitrateKbps,
            ExaminationTypes = source.ExaminationTypes,
            SnapshotHotkey = source.SnapshotHotkey.Clone(),
            CaptureHotkey = source.CaptureHotkey.Clone(),
            RsBaseTheptDirectory = source.RsBaseTheptDirectory,
            RsBaseReloadUrl = source.RsBaseReloadUrl,
            AutoFileToRsBase = source.AutoFileToRsBase,
            OpenRsBasePatientPageAfterFiling =
                source.OpenRsBasePatientPageAfterFiling,
            RsBasePatientPageUrlTemplate =
                source.RsBasePatientPageUrlTemplate,
            RsBasePatientPageDelayMilliseconds =
                source.RsBasePatientPageDelayMilliseconds,
            MainWindowLeft = source.MainWindowLeft,
            MainWindowTop = source.MainWindowTop,
            MainWindowWidth = source.MainWindowWidth,
            MainWindowHeight = source.MainWindowHeight,
            ZoomWindowLeft = source.ZoomWindowLeft,
            ZoomWindowTop = source.ZoomWindowTop,
            ZoomWindowWidth = source.ZoomWindowWidth,
            ZoomWindowHeight = source.ZoomWindowHeight,
            Presets = source.Presets.Select(ClonePreset).ToList()
        };
    }

    private static CapturePreset ClonePreset(CapturePreset source)
    {
        return new CapturePreset
        {
            Id = source.Id,
            Name = source.Name,
            DeviceId = source.DeviceId,
            DeviceName = source.DeviceName,
            DeviceIndex = source.DeviceIndex,
            Resolution = source.Resolution with { },
            LegacyResolutionIndex = source.LegacyResolutionIndex,
            IsVideo = source.IsVideo,
            PreviewOnly = source.PreviewOnly,
            ExaminationType = source.ExaminationType,
            Roi = source.Roi,
            OverlayText = source.OverlayText,
            FontName = source.FontName,
            FramesPerSecond = source.FramesPerSecond,
            WhiteBalanceRed = source.WhiteBalanceRed,
            WhiteBalanceGreen = source.WhiteBalanceGreen,
            WhiteBalanceBlue = source.WhiteBalanceBlue,
            Gamma = source.Gamma,
            FlipHorizontal = source.FlipHorizontal,
            FlipVertical = source.FlipVertical,
            SimpleNbi = source.SimpleNbi
        };
    }
}
