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
        ConfigureRuntimePresentation();
        LoadSettings();
    }

    public ApplicationSettings Settings { get; private set; }

    private CapturePreset? SelectedPreset =>
        _presetList.SelectedItem as CapturePreset;

    private void ConfigureRuntimePresentation()
    {
        Theme.Apply(this);
        ShowSettingsPage(false);

        _videoCheckBox.Text = "動画ファイリング";
        importTitleLabel.Text = "設定のインポート・エクスポート";
        importDescriptionLabel.Text =
            "ドキュメントフォルダーの entcapture2.config を既定にして読み書きします。";
        _importLegacyButton.Text = "V1設定をインポート";
        _presetList.DisplayMember = nameof(CapturePreset.Name);
        _deviceComboBox.DisplayMember = nameof(CameraDeviceInfo.Name);
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
            _continuousRecordingRadioButton.Checked =
                Settings.CaptureMode ==
                CaptureOperationMode.ContinuousTemporaryRecording;
            _previewOnlyRadioButton.Checked =
                Settings.CaptureMode == CaptureOperationMode.PreviewOnly;
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
            _fpsInput.Value = Math.Clamp(preset.FramesPerSecond, 0, 240);

            CameraDeviceInfo? device = FindDevice(preset);
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
        preset.FramesPerSecond = decimal.ToInt32(_fpsInput.Value);
        preset.FontName = _fontComboBox.Text.Trim();

        if (_deviceComboBox.SelectedItem is CameraDeviceInfo device)
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
            if (device is null)
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
                item => string.Equals(
                    item.Name,
                    preset.DeviceName,
                    StringComparison.OrdinalIgnoreCase))
            ?? _devices.FirstOrDefault(item => item.Index == preset.DeviceIndex);
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
        Settings.CaptureMode = _previewOnlyRadioButton.Checked
            ? CaptureOperationMode.PreviewOnly
            : CaptureOperationMode.ContinuousTemporaryRecording;
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
                ? "libx264"
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
        Settings.CaptureMode = _previewOnlyRadioButton.Checked
            ? CaptureOperationMode.PreviewOnly
            : CaptureOperationMode.ContinuousTemporaryRecording;
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
                ? "libx264"
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
            CaptureMode = source.CaptureMode,
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
            FlipVertical = source.FlipVertical
        };
    }
}
