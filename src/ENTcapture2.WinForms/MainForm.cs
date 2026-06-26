using System.Drawing.Imaging;
using System.Diagnostics;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using ENTcapture2.Core.Models;
using ENTcapture2.Core.Services;
using ENTcapture2.WinForms.Capture;
using ENTcapture2.WinForms.Imaging;
using ENTcapture2.WinForms.Input;
using ENTcapture2.WinForms.Integration;
using ENTcapture2.WinForms.Ui;

namespace ENTcapture2.WinForms;

public partial class MainForm : Form
{
    private const int QuickPresetCount = 5;
    private const int MaxSnapshotThumbnails = 8;
    private const int WindowSnapDistance = 18;
    private const int WmWindowPosChanging = 0x0046;
    private const int SwpNoSize = 0x0001;
    private const int SwpNoMove = 0x0002;
    private const int DwmwaExtendedFrameBounds = 9;

    private readonly ISettingsStore _settingsStore;
    private readonly CameraCaptureService _cameraService = new();
    private readonly VideoPlaybackService _playbackService = new();
    private readonly GlobalHotkeyService _hotkeyService = new();
    private readonly RsBaseTheptWatcher _theptWatcher = new();
    private readonly RsBaseFilingService _rsBaseFilingService = new();
    private readonly PatientMetadataStore _metadataStore = new();
    private readonly FlowLayoutPanel _sourceHeaderPanel = new();
    private readonly ModernButton _toggleSidePanelButton = new();
    private readonly ModernButton _openSnapshotFolderButton = new();
    private readonly ModernButton _rsBaseImportButton = new();
    private readonly ToolTip _thumbnailToolTip = new();
    private readonly ContextMenuStrip _morePresetsMenu = new();

    private ApplicationSettings _settings = ApplicationSettings.CreateDefault();
    private CapturePreset? _selectedPreset;
    private Bitmap? _displayedImage;
    private Bitmap? _playbackSourceImage;
    private PreviewZoomForm? _previewZoomForm;
    private ImageFilterState _captureFilterState = ImageFilterState.Default;
    private ImageFilterState _playbackFilterState = ImageFilterState.Default;
    private readonly List<ClipExportRange> _clipRanges = [];
    private IReadOnlyList<string> _playbackFilePaths = [];
    private CapturePreset? _playbackCapturePreset;
    private DateTime? _playbackCapturedAt;
    private PlaybackPosition? _lastPlaybackPosition;
    private TimeSpan? _clipStartTime;
    private TimeSpan? _clipEndTime;
    private bool _isPlaybackFilterMode;
    private bool _isUpdatingPresetControls;
    private bool _isApplyingPreset;
    private bool _isClosing;
    private int _frameRenderPending;
    private bool _isSeekingPlayback;
    private readonly Stopwatch _recordingClock = new();
    private bool _isRefreshingDevices;
    private DateTime _lastSnapshotAt = DateTime.MinValue;
    private Point _whiteBalanceDragStart;
    private Rectangle _whiteBalanceSelection;
    private bool _isSelectingWhiteBalance;

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowPos
    {
        public IntPtr hwnd;
        public IntPtr hwndInsertAfter;
        public int x;
        public int y;
        public int cx;
        public int cy;
        public uint flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public readonly Rectangle ToRectangle()
        {
            return Rectangle.FromLTRB(Left, Top, Right, Bottom);
        }
    }

    private readonly record struct FrameInsets(
        int Left,
        int Top,
        int Right,
        int Bottom)
    {
        public static FrameInsets Empty { get; } = new(0, 0, 0, 0);

        public Rectangle Apply(Rectangle bounds)
        {
            return Rectangle.FromLTRB(
                bounds.Left + Left,
                bounds.Top + Top,
                bounds.Right - Right,
                bounds.Bottom - Bottom);
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        out NativeRect pvAttribute,
        int cbAttribute);

    public MainForm()
        : this(new JsonSettingsStore())
    {
    }

    public MainForm(ISettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
        InitializeComponent();
        ApplyRuntimePresentation();
        InitializeRuntimeChoices();
        WireEvents();
    }

    private void ApplyRuntimePresentation()
    {
        Theme.Apply(this);
        RebuildRuntimeLayout();
        string applicationVersion = GetApplicationVersionText();
        Text = $"ENTcapture2 v{applicationVersion}";
        BackColor = Theme.Window;
        ForeColor = Theme.Text;
        Font = Theme.BodyFont();

        ConfigureLabel(titleLabel, "ENTcapture", Theme.Text, 20F, true);
        ConfigureLabel(versionLabel, "2", Theme.AccentBright, 20F, true);
        ConfigureLabel(
            subtitleLabel,
            $"  NEXT GENERATION CAPTURE  v{applicationVersion}",
            Theme.Muted,
            9F,
            false);
        ConfigureLabel(patientIdLabel, "患者ID", Theme.Muted, 10F, false);
        ConfigureLabel(patientNameLabel, "患者名", Theme.Muted, 10F, false);
        ConfigureTextBox(_patientIdTextBox, 80);
        ConfigureTextBox(_patientNameTextBox, 150);
        ConfigureLabel(
            examinationTypeLabel,
            "検査名",
            Theme.Muted,
            10F,
            false);
        ConfigureComboBox(_examinationTypeComboBox);
        _examinationTypeComboBox.Width = 160;

        ConfigureModernButton(
            _morePresetsDropDownButton,
            "その他 ▾",
            Theme.SurfaceRaised);
        _morePresetsDropDownButton.Width = 128;
        _morePresetsDropDownButton.Height = 38;
        _morePresetsDropDownButton.Margin = new Padding(0);
        SetMorePresetsDropDownSelected(false);
        ConfigureMorePresetsMenu();
        _previewOnlyCheckBox.ForeColor = Theme.Text;
        _fileVideoCheckBox.ForeColor = Theme.Text;
        _fileVideoCheckBox.Text = "動画ファイリング";
        ConfigureModernButton(
            _managePresetsButton,
            "⚙ 設定",
            Theme.SurfaceRaised);
        _managePresetsButton.Width = 96;
        ConfigureModernButton(
            _openPlaybackButton,
            PlaybackCaptions.OpenPlayback,
            Theme.SurfaceRaised);

        ConfigureLabel(
            _previewMessage,
            "カメラを選択して「プレビュー開始」を押してください",
            Theme.Muted,
            12F,
            false);
        _previewMessage.BackColor = Color.Transparent;
        _previewBox.Dock = DockStyle.None;
        _previewBox.SizeMode = PictureBoxSizeMode.Zoom;
        ConfigureThumbnailStrip();
        ConfigureLabel(
            liveBadgeLabel,
            " LIVE PREVIEW ",
            Theme.AccentBright,
            9F,
            false);
        liveBadgeLabel.BackColor = Color.FromArgb(34, Theme.Accent);

        ConfigureCard(sourceCard);
        ConfigureLabel(
            sourceTitleLabel,
            "VIDEO SOURCE",
            Theme.AccentBright,
            9F,
            false);
        ConfigureLabel(cameraLabel, "ビデオソース", Theme.Muted, 10F, false);
        ConfigureComboBox(_deviceComboBox);
        _deviceComboBox.Dock = DockStyle.None;
        _deviceComboBox.Width = 200;
        _deviceComboBox.Margin = new Padding(4, 2, 12, 0);
        ConfigureLabel(resolutionLabel, "解像度", Theme.Muted, 10F, false);
        ConfigureComboBox(_resolutionComboBox);
        _resolutionComboBox.Dock = DockStyle.None;
        _resolutionComboBox.Width = 210;
        _resolutionComboBox.Margin = new Padding(4, 2, 8, 0);
        cameraLabel.Margin = new Padding(0, 5, 0, 0);
        resolutionLabel.Margin = new Padding(0, 5, 0, 0);
        ConfigureModernButton(_startButton, "プレビュー開始", Theme.Accent);
        ConfigureModernButton(
            _refreshDevicesButton,
            "↻ 再検索",
            Theme.SurfaceRaised);
        _refreshDevicesButton.MinimumSize = new Size(70, 24);
        _refreshDevicesButton.Size = new Size(82, 24);
        _refreshDevicesButton.Margin = new Padding(0, 1, 0, 0);
        _toggleSidePanelButton.Text = "＜ 補正";
        _toggleSidePanelButton.FillColor = Theme.SurfaceRaised;
        _toggleSidePanelButton.BackColor = Theme.SurfaceRaised;
        _toggleSidePanelButton.HoverColor = Theme.ButtonHover;
        _toggleSidePanelButton.ForeColor = Theme.Text;
        _toggleSidePanelButton.CornerRadius = 0;
        _toggleSidePanelButton.StackTextVertically = true;
        _toggleSidePanelButton.Padding = Padding.Empty;
        _toggleSidePanelButton.Font = new Font(
            "Yu Gothic UI",
            10F,
            FontStyle.Bold);
        _toggleSidePanelButton.TextAlign = ContentAlignment.MiddleCenter;
        _toggleSidePanelButton.Dock = DockStyle.None;
        _toggleSidePanelButton.Anchor = AnchorStyles.None;
        _toggleSidePanelButton.Size = new Size(38, 112);
        _toggleSidePanelButton.Margin = new Padding(1);

        ConfigureCard(filterCard);
        ConfigureLabel(
            filterTitleLabel,
            "IMAGE CONTROL",
            Theme.AccentBright,
            9F,
            false);
        _whiteBalanceSelectionCheckBox.ForeColor = Theme.Text;
        whiteBalanceHintLabel.ForeColor = Theme.Muted;
        _flipHorizontalCheckBox.ForeColor = Theme.Text;
        _flipVerticalCheckBox.ForeColor = Theme.Text;
        ConfigureSliderRow(redRow, _redTrack, _redValue, "R", 0, 510, 255);
        ConfigureSliderRow(greenRow, _greenTrack, _greenValue, "G", 0, 510, 255);
        ConfigureSliderRow(blueRow, _blueTrack, _blueValue, "B", 0, 510, 255);
        ConfigureSliderRow(gammaRow, _gammaTrack, _gammaValue, "γ", 1, 30, 10);
        ConfigureModernButton(
            _resetFiltersButton,
            "補正をリセット",
            Theme.SurfaceRaised);

        ConfigureCard(captureCard);
        ConfigureLabel(
            captureTitleLabel,
            "PRESET",
            Theme.AccentBright,
            9F,
            false);
        captureTitleLabel.Visible = false;
        ConfigureModernButton(
            _snapshotButton,
            "静止画",
            Theme.Accent);
        ConfigureModernButton(
            _savePresetButton,
            "現在値をプリセットへ保存",
            Theme.SurfaceRaised);
        ConfigureModernButton(
            _playPauseButton,
            PlaybackCaptions.Pause,
            Theme.SurfaceRaised);
        ConfigureModernButton(
            _openClipEditorButton,
            "動画編集",
            Theme.SurfaceRaised);
        ConfigureModernButton(
            _closePlaybackButton,
            PlaybackCaptions.ClosePlayback,
            Theme.SurfaceRaised);

        ConfigureLabel(_statusLabel, "●  スタンバイ", Theme.Muted, 10F, false);
        ConfigureLabel(
            _presetLabel,
            "プリセット: 未選択",
            Theme.Muted,
            10F,
            false);
        ConfigureLabel(
            _fpsLabel,
            "INPUT -- fps  /  PREVIEW -- fps",
            Theme.Muted,
            10F,
            false);

        sidePanel.Width = 310;
        sourceCard.Width = 294;
        filterCard.Width = 294;
        captureCard.Width = 294;
        filterCard.Visible = true;
        captureCard.Visible = true;
        captureCard.MinimumSize = new Size(294, 0);
        filterCard.MinimumSize = new Size(294, 0);
        _savePresetButton.Padding = new Padding(4, 0, 4, 0);
        ApplyPrimaryActionButtonLayout();
        sourceCard.Visible = false;
        sidePanel.Visible = false;
        workspaceLayout.ColumnStyles[2].Width = 0;
        PreviewSurfacePanel_Resize(this, EventArgs.Empty);
    }

    private void RebuildRuntimeLayout()
    {
        sourceCard.Controls.Remove(cameraLabel);
        sourceCard.Controls.Remove(_deviceComboBox);
        sourceCard.Controls.Remove(resolutionLabel);
        sourceCard.Controls.Remove(_resolutionComboBox);
        sourceButtonPanel.Controls.Remove(_refreshDevicesButton);
        sourceCard.Visible = false;
        sidePanel.Controls.Remove(_toggleImageControlButton);
        sidePanel.Controls.Remove(_togglePresetToolsButton);
        if (!sidePanel.Controls.Contains(filterCard))
        {
            sidePanel.Controls.Add(filterCard);
        }
        if (!sidePanel.Controls.Contains(captureCard))
        {
            sidePanel.Controls.Add(captureCard);
        }
        captureCard.Visible = true;

        _sourceHeaderPanel.AutoSize = true;
        _sourceHeaderPanel.WrapContents = false;
        _sourceHeaderPanel.Margin = new Padding(0, 0, 12, 0);
        _sourceHeaderPanel.Controls.Add(cameraLabel);
        _sourceHeaderPanel.Controls.Add(_deviceComboBox);
        _sourceHeaderPanel.Controls.Add(resolutionLabel);
        _sourceHeaderPanel.Controls.Add(_resolutionComboBox);
        _sourceHeaderPanel.Controls.Add(_refreshDevicesButton);

        headerLayout.Controls.Remove(patientPanel);
        headerLayout.Controls.Remove(primaryActionPanel);
        headerLayout.Controls.Remove(_quickPresetPanel);
        headerLayout.Controls.Remove(_sourceHeaderPanel);
        headerLayout.Controls.Remove(presetActionPanel);
        headerLayout.RowCount = 3;
        headerLayout.RowStyles.Clear();
        headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        patientPanel.Margin = new Padding(0, 6, 0, 0);
        _quickPresetPanel.Margin = new Padding(0, 6, 0, 0);
        headerLayout.Controls.Add(_sourceHeaderPanel, 0, 0);
        headerLayout.Controls.Add(primaryActionPanel, 1, 0);
        headerLayout.SetRowSpan(primaryActionPanel, 3);
        headerLayout.Controls.Add(patientPanel, 0, 1);
        headerLayout.Controls.Add(_quickPresetPanel, 0, 2);
        if (!_quickPresetPanel.Controls.Contains(_morePresetsDropDownButton))
        {
            _quickPresetPanel.Controls.Add(_morePresetsDropDownButton);
        }

        workspaceLayout.Controls.Remove(previewSurfacePanel);
        workspaceLayout.Controls.Remove(sidePanel);
        workspaceLayout.ColumnCount = 3;
        workspaceLayout.ColumnStyles.Clear();
        workspaceLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100F));
        workspaceLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 40F));
        workspaceLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 0F));
        workspaceLayout.Controls.Add(previewSurfacePanel, 0, 0);
        workspaceLayout.Controls.Add(_toggleSidePanelButton, 1, 0);
        workspaceLayout.Controls.Add(sidePanel, 2, 0);
        sidePanel.Visible = false;
    }

    private void InitializeRuntimeChoices()
    {
        _resolutionComboBox.Items.AddRange(
            new object[]
            {
                CaptureResolution.Default
            });
        _resolutionComboBox.SelectedIndex = 0;
    }

    private void PreviewSurfacePanel_Resize(object? sender, EventArgs e)
    {
        LayoutPreviewBox();
        PositionThumbnailActionButtons();
    }

    private void LayoutPreviewBox()
    {
        Padding padding = previewSurfacePanel.Padding;
        int reservedBottom = 0;
        if (_thumbnailStripPanel.Visible)
        {
            reservedBottom += _thumbnailStripPanel.Height;
        }

        if (playbackPanel.Visible)
        {
            reservedBottom += playbackPanel.Height;
        }

        int width = Math.Max(
            0,
            previewSurfacePanel.ClientSize.Width - padding.Left - padding.Right);
        int height = Math.Max(
            0,
            previewSurfacePanel.ClientSize.Height -
            padding.Top -
            padding.Bottom -
            reservedBottom);
        _previewBox.Bounds = new Rectangle(
            padding.Left,
            padding.Top,
            width,
            height);
        _previewMessage.Location = new Point(
            Math.Max(8, _previewBox.Left + (_previewBox.Width - _previewMessage.Width) / 2),
            Math.Max(8, _previewBox.Top + (_previewBox.Height - _previewMessage.Height) / 2));
    }

    private static void ConfigureCard(TableLayoutPanel card)
    {
        card.SuspendLayout();
        card.RowStyles.Clear();
        card.AutoSize = true;
        card.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        card.BackColor = Theme.Surface;
        card.ColumnCount = 1;
        card.ColumnStyles.Clear();
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        card.RowCount = card.Controls.Count;
        for (int index = 0; index < card.RowCount; index++)
        {
            card.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        card.Margin = new Padding(0, 0, 0, 12);
        card.Padding = new Padding(16);
        card.Width = 294;
        card.MinimumSize = new Size(294, 0);
        card.ResumeLayout();
    }

    private static void ConfigureLabel(
        Label label,
        string text,
        Color color,
        float size,
        bool bold)
    {
        label.AutoSize = true;
        label.Font = bold ? Theme.HeadingFont(size) : Theme.BodyFont(size);
        label.ForeColor = color;
        label.Text = text;
    }

    private static void ConfigureComboBox(ComboBox comboBox)
    {
        comboBox.BackColor = Theme.SurfaceRaised;
        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox.FlatStyle = FlatStyle.Flat;
        comboBox.Font = Theme.BodyFont();
        comboBox.ForeColor = Theme.Text;
        comboBox.Margin = new Padding(0, 2, 8, 8);
    }

    private static void ConfigureTextBox(TextBox textBox, int width)
    {
        textBox.BackColor = Theme.SurfaceRaised;
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.Font = Theme.BodyFont();
        textBox.ForeColor = Theme.Text;
        textBox.Margin = new Padding(4, 1, 10, 0);
        textBox.Width = width;
    }

    private static void ConfigureModernButton(
        ModernButton button,
        string text,
        Color fillColor)
    {
        button.Text = text;
        button.FillColor = fillColor;
        button.BackColor = fillColor;
        button.HoverColor =
            fillColor == Theme.Accent
                ? Theme.AccentHover
                : Theme.ButtonHover;
        button.AutoSize = false;
        button.Height = 38;
        button.MinimumSize = new Size(76, 38);
        button.Margin = new Padding(0, 0, 8, 0);
    }

    private void ConfigureMorePresetsMenu()
    {
        _morePresetsMenu.BackColor = Theme.SurfaceRaised;
        _morePresetsMenu.ForeColor = Theme.Text;
        _morePresetsMenu.ShowImageMargin = false;
        _morePresetsMenu.Font = Theme.BodyFont();
        _morePresetsMenu.RenderMode = ToolStripRenderMode.System;
    }

    private void SetMorePresetsDropDownSelected(bool selected)
    {
        Color fillColor = selected
            ? Theme.Selected
            : Theme.SurfaceRaised;
        _morePresetsDropDownButton.FillColor = fillColor;
        _morePresetsDropDownButton.BackColor = fillColor;
        _morePresetsDropDownButton.HoverColor = selected
            ? Theme.Selected
            : Theme.ButtonHover;
        _morePresetsDropDownButton.ForeColor = selected
            ? Theme.Text
            : Theme.Muted;
    }

    private void ApplyPrimaryActionButtonLayout()
    {
        primaryActionPanel.AutoSize = true;
        primaryActionPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        primaryActionPanel.ColumnStyles.Clear();
        primaryActionPanel.RowStyles.Clear();
        primaryActionPanel.ColumnCount = 2;
        primaryActionPanel.RowCount = 2;
        primaryActionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132F));
        primaryActionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132F));
        primaryActionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        primaryActionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));

        ConfigurePrimaryActionButton(_startButton, Theme.Accent);
        ConfigurePrimaryActionButton(_managePresetsButton, Theme.SurfaceRaised);
        ConfigurePrimaryActionButton(_snapshotButton, Color.FromArgb(30, 64, 175));
        ConfigurePrimaryActionButton(_openPlaybackButton, Theme.SurfaceRaised);
    }

    private static void ConfigurePrimaryActionButton(ModernButton button, Color fillColor)
    {
        button.Dock = DockStyle.Fill;
        button.Margin = new Padding(4);
        button.Padding = new Padding(8, 0, 8, 0);
        button.MinimumSize = new Size(116, 36);
        button.FillColor = fillColor;
        button.BackColor = fillColor;
        button.HoverColor =
            fillColor == Theme.Accent
                ? Theme.AccentHover
                : Theme.ButtonHover;
    }

    private static void ConfigureSliderRow(
        TableLayoutPanel row,
        TrackBar trackBar,
        Label valueLabel,
        string caption,
        int minimum,
        int maximum,
        int value)
    {
        row.SuspendLayout();
        row.Controls.Clear();
        row.ColumnStyles.Clear();
        row.RowStyles.Clear();
        row.AutoSize = false;
        row.Height = 42;
        row.MinimumSize = new Size(0, 42);
        row.ColumnCount = 3;
        row.RowCount = 1;
        row.Dock = DockStyle.Fill;
        row.Margin = new Padding(0, 0, 0, 4);
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46F));
        row.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));

        var label = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Theme.Muted,
            Text = caption,
            TextAlign = ContentAlignment.MiddleLeft
        };
        trackBar.BackColor = Theme.Surface;
        trackBar.Dock = DockStyle.Fill;
        trackBar.Maximum = maximum;
        trackBar.Minimum = minimum;
        trackBar.TickStyle = TickStyle.None;
        trackBar.Value = value;
        valueLabel.AutoSize = false;
        valueLabel.Font = Theme.BodyFont(9F);
        valueLabel.ForeColor = Theme.Text;
        valueLabel.Text = caption == "γ" ? "1.0" : value.ToString();
        valueLabel.TextAlign = ContentAlignment.MiddleRight;
        valueLabel.Width = 42;
        row.Controls.Add(label, 0, 0);
        row.Controls.Add(trackBar, 1, 0);
        row.Controls.Add(valueLabel, 2, 0);
        row.ResumeLayout();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(18),
            BackColor = Theme.Window
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildWorkspace(), 0, 1);
        root.Controls.Add(BuildFooter(), 0, 2);
        Controls.Add(root);
    }

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(0, 0, 0, 14)
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var titleArea = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty
        };
        titleArea.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "ENTcapture",
            Font = Theme.HeadingFont(20),
            ForeColor = Theme.Text,
            Margin = new Padding(0, 0, 6, 0)
        });
        titleArea.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "2",
            Font = Theme.HeadingFont(20),
            ForeColor = Theme.AccentBright,
            Margin = Padding.Empty
        });
        titleArea.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "  NEXT GENERATION CAPTURE",
            Font = Theme.BodyFont(9),
            ForeColor = Theme.Muted,
            Margin = new Padding(8, 9, 0, 0)
        });

        var patientArea = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty
        };
        patientArea.Controls.Add(CreateCompactLabel("患者ID"));
        _patientIdTextBox.Width = 120;
        patientArea.Controls.Add(_patientIdTextBox);
        patientArea.Controls.Add(CreateCompactLabel("患者名"));
        _patientNameTextBox.Width = 180;
        patientArea.Controls.Add(_patientNameTextBox);

        _quickPresetPanel.AutoSize = true;
        _quickPresetPanel.WrapContents = false;
        _quickPresetPanel.Margin = new Padding(0, 12, 0, 0);

        var presetActions = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(8, 12, 0, 0)
        };
        _morePresetsDropDownButton.Width = 128;
        presetActions.Controls.Add(_morePresetsDropDownButton);

        ConfigureButton(_managePresetsButton, "プリセット管理", Theme.SurfaceRaised);
        presetActions.Controls.Add(_managePresetsButton);

        header.Controls.Add(titleArea, 0, 0);
        header.Controls.Add(patientArea, 1, 0);
        header.Controls.Add(_quickPresetPanel, 0, 1);
        header.Controls.Add(presetActions, 1, 1);
        return header;
    }

    private Control BuildWorkspace()
    {
        var workspace = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty
        };
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 310));

        workspace.Controls.Add(BuildPreviewSurface(), 0, 0);
        workspace.Controls.Add(BuildControlPanel(), 1, 0);
        return workspace;
    }

    private Control BuildPreviewSurface()
    {
        var previewSurface = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Black,
            Margin = new Padding(0, 0, 14, 0),
            Padding = new Padding(1)
        };

        _previewBox.Dock = DockStyle.None;
        _previewBox.BackColor = Color.FromArgb(4, 7, 12);
        _previewBox.SizeMode = PictureBoxSizeMode.Zoom;

        _previewMessage.AutoSize = true;
        _previewMessage.Text = "カメラを選択して「プレビュー開始」を押してください";
        _previewMessage.Font = Theme.BodyFont(12);
        _previewMessage.ForeColor = Theme.Muted;
        _previewMessage.BackColor = Color.Transparent;

        var badge = new Label
        {
            AutoSize = true,
            Text = " LIVE PREVIEW ",
            Font = Theme.BodyFont(9),
            ForeColor = Theme.AccentBright,
            BackColor = Color.FromArgb(34, Theme.Accent),
            Padding = new Padding(7, 5, 7, 5),
            Location = new Point(14, 14)
        };

        previewSurface.Controls.Add(_previewBox);
        previewSurface.Controls.Add(_previewMessage);
        previewSurface.Controls.Add(badge);
        badge.BringToFront();
        _previewMessage.BringToFront();

        previewSurface.Resize += (_, _) =>
        {
            _previewMessage.Location = new Point(
                Math.Max(8, (previewSurface.ClientSize.Width - _previewMessage.Width) / 2),
                Math.Max(8, (previewSurface.ClientSize.Height - _previewMessage.Height) / 2));
        };

        return previewSurface;
    }

    private Control BuildControlPanel()
    {
        var side = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Theme.Window,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        side.Controls.Add(BuildSourceCard());
        side.Controls.Add(BuildFilterCard());
        side.Controls.Add(BuildActionCard());
        return side;
    }

    private Control BuildSourceCard()
    {
        var card = CreateCard("VIDEO SOURCE");
        var content = (TableLayoutPanel)card.Controls[1];

        _deviceComboBox.Dock = DockStyle.Fill;
        _resolutionComboBox.Dock = DockStyle.Fill;
        _resolutionComboBox.Items.AddRange(
        [
            new CaptureResolution(0, 0, 0),
            new CaptureResolution(640, 480, 30),
            new CaptureResolution(720, 480, 30),
            new CaptureResolution(1280, 720, 30),
            new CaptureResolution(1920, 1080, 30),
            new CaptureResolution(3840, 2160, 30)
        ]);
        _resolutionComboBox.SelectedIndex = 0;

        AddCardRow(content, "カメラ", _deviceComboBox);
        AddCardRow(content, "解像度", _resolutionComboBox);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            WrapContents = false,
            Margin = new Padding(0, 8, 0, 0)
        };
        ConfigureButton(_startButton, "プレビュー開始", Theme.Accent);
        _startButton.Width = 170;
        ConfigureButton(_refreshDevicesButton, "再検索", Theme.SurfaceRaised);
        _refreshDevicesButton.Width = 82;
        buttons.Controls.Add(_startButton);
        buttons.Controls.Add(_refreshDevicesButton);
        content.Controls.Add(buttons);
        return card;
    }

    private Control BuildFilterCard()
    {
        var card = CreateCard("IMAGE CONTROL");
        var content = (TableLayoutPanel)card.Controls[1];

        ConfigureColorTrack(_redTrack, _redValue, "R");
        ConfigureColorTrack(_greenTrack, _greenValue, "G");
        ConfigureColorTrack(_blueTrack, _blueValue, "B");

        _gammaTrack.Minimum = 1;
        _gammaTrack.Maximum = 30;
        _gammaTrack.Value = 10;
        _gammaTrack.TickStyle = TickStyle.None;
        _gammaTrack.Dock = DockStyle.Fill;
        _gammaTrack.BackColor = Theme.Surface;
        _gammaValue.Text = "1.0";

        AddSliderRow(content, "R", _redTrack, _redValue);
        AddSliderRow(content, "G", _greenTrack, _greenValue);
        AddSliderRow(content, "B", _blueTrack, _blueValue);
        AddSliderRow(content, "γ", _gammaTrack, _gammaValue);

        ConfigureButton(_resetFiltersButton, "補正をリセット", Theme.SurfaceRaised);
        _resetFiltersButton.Dock = DockStyle.Fill;
        content.Controls.Add(_resetFiltersButton);
        return card;
    }

    private Control BuildActionCard()
    {
        var card = CreateCard("CAPTURE");
        var content = (TableLayoutPanel)card.Controls[1];

        ConfigureButton(_snapshotButton, "●  静止画を保存", Theme.Accent);
        _snapshotButton.Dock = DockStyle.Fill;
        _snapshotButton.Enabled = false;

        ConfigureButton(_savePresetButton, "現在値をプリセットへ保存", Theme.SurfaceRaised);
        _savePresetButton.Dock = DockStyle.Fill;

        content.Controls.Add(_snapshotButton);
        content.Controls.Add(_savePresetButton);
        return card;
    }

    private Control BuildFooter()
    {
        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0, 12, 0, 0)
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _statusLabel.AutoSize = true;
        _statusLabel.Text = "●  スタンバイ";
        _statusLabel.ForeColor = Theme.Muted;
        _statusLabel.Anchor = AnchorStyles.Left;

        _presetLabel.AutoSize = true;
        _presetLabel.Text = "プリセット: 未選択";
        _presetLabel.ForeColor = Theme.Muted;
        _presetLabel.Margin = new Padding(0, 0, 24, 0);

        _fpsLabel.AutoSize = true;
        _fpsLabel.Text = "INPUT -- fps  /  PREVIEW -- fps";
        _fpsLabel.ForeColor = Theme.Muted;

        footer.Controls.Add(_statusLabel, 0, 0);
        footer.Controls.Add(_presetLabel, 1, 0);
        footer.Controls.Add(_fpsLabel, 2, 0);
        return footer;
    }

    private void WireEvents()
    {
        Shown += MainForm_Shown;
        FormClosing += MainForm_FormClosing;
        _cameraService.FrameReady += CameraService_FrameReady;
        _cameraService.StatisticsUpdated += CameraService_StatisticsUpdated;
        _playbackService.FrameReady += PlaybackService_FrameReady;
        _hotkeyService.SnapshotPressed += HotkeyService_SnapshotPressed;
        _hotkeyService.CapturePressed += HotkeyService_CapturePressed;
        _theptWatcher.PatientChanged += TheptWatcher_PatientChanged;
        _startButton.Click += StartButton_Click;
        _snapshotButton.Click += SnapshotButton_Click;
        _refreshDevicesButton.Click += async (_, _) => await RefreshDevicesAsync();
        _managePresetsButton.Click += ManagePresetsButton_Click;
        _openPlaybackButton.Click += OpenPlaybackButton_Click;
        _toggleSidePanelButton.Click +=
            async (_, _) => await ToggleSidePanelAsync();
        _savePresetButton.Click += SavePresetButton_Click;
        _morePresetsDropDownButton.Click += MorePresetsDropDownButton_Click;
        _openSnapshotFolderButton.Click += OpenSnapshotFolderButton_Click;
        _rsBaseImportButton.Click += RsBaseImportButton_Click;
        _deviceComboBox.SelectedIndexChanged += DeviceComboBox_SelectedIndexChanged;
        _examinationTypeComboBox.TextChanged +=
            ExaminationTypeComboBox_TextChanged;
        _previewOnlyCheckBox.CheckedChanged +=
            HeaderOptionCheckBox_CheckedChanged;
        _fileVideoCheckBox.CheckedChanged +=
            HeaderOptionCheckBox_CheckedChanged;
        foreach (ModernRadioButton button in QuickPresetButtons)
        {
            button.CheckedChanged += QuickPresetButton_CheckedChanged;
        }
        _resetFiltersButton.Click += (_, _) => ResetFilters();
        _redTrack.ValueChanged += (_, _) => ProcessingControlChanged();
        _greenTrack.ValueChanged += (_, _) => ProcessingControlChanged();
        _blueTrack.ValueChanged += (_, _) => ProcessingControlChanged();
        _gammaTrack.ValueChanged += (_, _) => ProcessingControlChanged();
        _flipHorizontalCheckBox.CheckedChanged +=
            (_, _) => ProcessingControlChanged();
        _flipVerticalCheckBox.CheckedChanged +=
            (_, _) => ProcessingControlChanged();
        _whiteBalanceSelectionCheckBox.CheckedChanged +=
            WhiteBalanceSelectionCheckBox_CheckedChanged;
        _previewBox.MouseDown += PreviewBox_MouseDown;
        _previewBox.MouseMove += PreviewBox_MouseMove;
        _previewBox.MouseUp += PreviewBox_MouseUp;
        _previewBox.Paint += PreviewBox_Paint;
        _previewBox.MouseDoubleClick += PreviewBox_MouseDoubleClick;
        _playPauseButton.Click += PlayPauseButton_Click;
        _openClipEditorButton.Click += OpenClipEditorButton_Click;
        _closePlaybackButton.Click += async (_, _) => await ClosePlaybackAsync();
        _playbackTrackBar.MouseDown += (_, _) => _isSeekingPlayback = true;
        _playbackTrackBar.Scroll += PlaybackTrackBar_Scroll;
        _playbackTrackBar.MouseUp += PlaybackTrackBar_MouseUp;
        _markClipStartButton.Click += MarkClipStartButton_Click;
        _markClipEndButton.Click += MarkClipEndButton_Click;
        _addClipButton.Click += AddClipButton_Click;
        _clearClipsButton.Click += ClearClipsButton_Click;
        _exportJoinedClipsButton.Click += ExportJoinedClipsButton_Click;
        _exportSeparateClipsButton.Click += ExportSeparateClipsButton_Click;
    }

    private async void MainForm_Shown(object? sender, EventArgs e)
    {
        try
        {
            _settings = await _settingsStore.LoadAsync();
            RestoreMainWindowBounds();
            RefreshExaminationTypes();
            UpdateHeaderOptions();
            UpdateCaptureModePresentation();
            UpdateRsBaseImportButtonVisibility();
            StartExternalIntegrations();
            CleanupTemporaryRecordings();
            RefreshPresetControls();
            await RefreshDevicesAsync();

            CapturePreset? selectedPreset =
                _settings.Presets.FirstOrDefault(
                    preset => preset.Id == _settings.SelectedPresetId)
                ?? _settings.Presets.FirstOrDefault();

            if (selectedPreset is not null)
            {
                SelectPreset(selectedPreset);
            }

            UpdateInteractionGuards();
        }
        catch (Exception exception)
        {
            ShowError("初期化に失敗しました。", exception);
        }
    }

    private async Task RefreshDevicesAsync()
    {
        _isRefreshingDevices = true;
        UpdateInteractionGuards();
        _statusLabel.Text = "●  カメラを検索中...";
        _statusLabel.ForeColor = Theme.AccentBright;

        try
        {
            IReadOnlyList<CameraDeviceInfo> devices =
                await DirectShowDeviceCatalog.DiscoverAsync();

            _deviceComboBox.BeginUpdate();
            _deviceComboBox.Items.Clear();
            foreach (CameraDeviceInfo device in devices)
            {
                _deviceComboBox.Items.Add(device);
            }
            _deviceComboBox.EndUpdate();

            if (devices.Count > 0)
            {
                CameraDeviceInfo? preferred = devices.FirstOrDefault(
                    device =>
                        !string.IsNullOrWhiteSpace(_selectedPreset?.DeviceId) &&
                        string.Equals(
                            device.MonikerString,
                            _selectedPreset.DeviceId,
                            StringComparison.OrdinalIgnoreCase))
                    ?? devices.FirstOrDefault(
                        device => device.Index == _selectedPreset?.DeviceIndex);
                _deviceComboBox.SelectedItem = preferred ?? devices[0];
                _statusLabel.Text = $"●  {devices.Count}台のカメラを検出";
                _statusLabel.ForeColor = Theme.AccentBright;
            }
            else
            {
                _statusLabel.Text = "●  カメラが見つかりません";
                _statusLabel.ForeColor = Theme.Danger;
            }
        }
        finally
        {
            _isRefreshingDevices = false;
            UpdateInteractionGuards();
        }
    }

    private void DeviceComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_deviceComboBox.SelectedItem is not CameraDeviceInfo device)
        {
            return;
        }

        CaptureResolution? preferredResolution =
            _selectedPreset?.Resolution;

        _resolutionComboBox.BeginUpdate();
        try
        {
            _resolutionComboBox.Items.Clear();
            _resolutionComboBox.Items.Add(CaptureResolution.Default);
            foreach (CaptureResolution resolution in device.Resolutions)
            {
                _resolutionComboBox.Items.Add(resolution);
            }

            int preferredIndex =
                GetPreferredResolutionIndex(preferredResolution);
            _resolutionComboBox.SelectedIndex =
                preferredIndex >= 0
                    ? preferredIndex
                    : (_resolutionComboBox.Items.Count > 1 ? 1 : 0);
        }
        finally
        {
            _resolutionComboBox.EndUpdate();
        }
    }

    private int GetPreferredResolutionIndex(CaptureResolution? preferredResolution)
    {
        if (preferredResolution is null ||
            preferredResolution.Width <= 0 ||
            preferredResolution.Height <= 0)
        {
            return GetBestResolutionIndex(_resolutionComboBox);
        }

        int exactIndex = _resolutionComboBox.Items.IndexOf(preferredResolution);
        if (exactIndex >= 0)
        {
            return exactIndex;
        }

        return _resolutionComboBox.Items
            .Cast<object>()
            .Select((item, index) => new { item, index })
            .Where(entry =>
                entry.item is CaptureResolution resolution &&
                resolution.Width == preferredResolution.Width &&
                resolution.Height == preferredResolution.Height &&
                (
                    preferredResolution.FramesPerSecond <= 0 ||
                    resolution.FramesPerSecond == preferredResolution.FramesPerSecond))
            .OrderBy(entry =>
                GetCapturePixelFormatPriority(
                    ((CaptureResolution)entry.item).PixelFormat))
            .Select(entry => entry.index)
            .FirstOrDefault(-1);
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

    private async void StartButton_Click(object? sender, EventArgs e)
    {
        if (_cameraService.IsRunning)
        {
            await StopPreviewAsync();
            return;
        }

        if (_deviceComboBox.SelectedItem is not CameraDeviceInfo device)
        {
            MessageBox.Show(
                this,
                "カメラを選択してください。",
                "ENTcapture2",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        CaptureResolution resolution =
            _resolutionComboBox.SelectedItem as CaptureResolution
            ?? CaptureResolution.Default;

        try
        {
            if (_playbackService.IsOpen)
            {
                await ClosePlaybackAsync();
            }

            _startButton.Enabled = false;
            _statusLabel.Text = "●  カメラを起動中...";
            _statusLabel.ForeColor = Theme.AccentBright;
            RecordingOptions? recordingOptions =
                GetEffectiveCaptureMode() ==
                CaptureOperationMode.ContinuousTemporaryRecording
                    ? new RecordingOptions(
                        _settings.TemporaryRecordingDirectory,
                        _settings.TemporaryRecordingCodec,
                        GetRecordingFrameRate(resolution),
                        BuildTemporaryRecordingPrefix(),
                        _settings.RecordingSegmentMinutes,
                        _settings.H264EncoderPreference)
                    : null;
            await _cameraService.StartAsync(
                device.Index,
                resolution.Width,
                resolution.Height,
                resolution.FramesPerSecond,
                resolution.PixelFormat,
                recordingOptions,
                _settings.MaximumPreviewFramesPerSecond);
            if (recordingOptions is not null)
            {
                _recordingClock.Restart();
            }
            else
            {
                _recordingClock.Reset();
            }

            _startButton.Text = recordingOptions is null
                ? "プレビュー停止"
                : "取り込み終了";
            _startButton.FillColor = Theme.Danger;
            _startButton.BackColor = Theme.Danger;
            _startButton.HoverColor = Color.FromArgb(225, 29, 72);
            _previewMessage.Visible = false;
            _snapshotButton.Enabled = true;
            _deviceComboBox.Enabled = false;
            _resolutionComboBox.Enabled = false;
            _statusLabel.Text = recordingOptions is null
                ? "●  プレビュー中"
                : "●  一次動画を録画中";
            _statusLabel.ForeColor = Theme.AccentBright;
            UpdatePreviewTopMostState();
            UpdateInteractionGuards();
        }
        catch (Exception exception)
        {
            ShowError("カメラを開始できませんでした。", exception);
        }
        finally
        {
            _startButton.Enabled = true;
            UpdateInteractionGuards();
        }
    }

    private async Task StopPreviewAsync()
    {
        _startButton.Enabled = false;
        try
        {
            bool shouldPlay =
                !_isClosing &&
                GetEffectiveCaptureMode() ==
                CaptureOperationMode.ContinuousTemporaryRecording;
            await _cameraService.StopAsync();
            _recordingClock.Stop();
            UpdateCaptureModePresentation();
            _startButton.FillColor = Theme.Accent;
            _startButton.BackColor = Theme.Accent;
            _startButton.HoverColor = Theme.AccentHover;
            _snapshotButton.Enabled = false;
            _deviceComboBox.Enabled = true;
            _resolutionComboBox.Enabled = true;
            _previewMessage.Visible = true;
            _statusLabel.Text = "●  スタンバイ";
            _statusLabel.ForeColor = Theme.Muted;
            _fpsLabel.Text = "INPUT -- fps  /  PREVIEW -- fps";
            UpdatePreviewTopMostState();
            UpdateInteractionGuards();

            if (_cameraService.LastRecordingError is not null)
            {
                ShowError(
                    "一次動画の録画に失敗しました。",
                    _cameraService.LastRecordingError);
            }
            else
            {
                if (_cameraService.LastRecordingWarning is not null)
                {
                    MessageBox.Show(
                        this,
                        _cameraService.LastRecordingWarning,
                        "ENTcapture2",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                if (shouldPlay &&
                    _cameraService.LastRecordingPaths.Count > 0)
                {
                    await RecordRecordingFilesAsync(
                        _cameraService.LastRecordingPaths);
                    await StartPlaybackAsync(
                        _cameraService.LastRecordingPaths);
                }

                await RunRsBaseAutoFilingAsync(
                    _cameraService.LastRecordingPaths);
            }
        }
        finally
        {
            _startButton.Enabled = true;
            UpdateInteractionGuards();
        }
    }

    private double GetRecordingFrameRate(CaptureResolution resolution)
    {
        if (_selectedPreset?.FramesPerSecond > 0)
        {
            return _selectedPreset.FramesPerSecond;
        }

        return resolution.FramesPerSecond > 0
            ? resolution.FramesPerSecond
            : 30;
    }

    private async Task RunRsBaseAutoFilingAsync(
        IReadOnlyList<string> recordingPaths)
    {
        bool fileVideo = _fileVideoCheckBox.Checked;
        bool notifyRsBase = _settings.AutoFileToRsBase;
        if (!fileVideo && !notifyRsBase)
        {
            return;
        }

        try
        {
            var progress = new Progress<string>(message =>
            {
                _statusLabel.Text = "●  " + message;
                _statusLabel.ForeColor = Theme.AccentBright;
            });
            IReadOnlyList<string> filedPaths =
                await _rsBaseFilingService.FileRecordingAsync(
                    recordingPaths,
                    _settings,
                    fileVideo,
                    notifyRsBase,
                    progress);
            await RecordRecordingFilesAsync(filedPaths);
            _statusLabel.Text = "●  RSBase自動ファイリングを完了しました";
            _statusLabel.ForeColor = Theme.AccentBright;
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                $"RSBase自動ファイリングに失敗しました。\r\n{exception.Message}",
                "ENTcapture2",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            _statusLabel.Text = "●  RSBase自動ファイリング失敗";
            _statusLabel.ForeColor = Theme.Danger;
        }
    }

    private async Task NotifyRsBaseAfterSnapshotAsync()
    {
        if (!_settings.AutoFileToRsBase)
        {
            return;
        }

        try
        {
            await _rsBaseFilingService.NotifyRsBaseAsync(_settings);
        }
        catch (Exception exception)
        {
            try
            {
                BeginInvoke(() =>
                {
                    _statusLabel.Text =
                        "●  RSBase取込通知に失敗: " + exception.Message;
                    _statusLabel.ForeColor = Theme.Danger;
                });
            }
            catch (InvalidOperationException)
            {
            }
        }
    }

    private string BuildTemporaryRecordingPrefix()
    {
        return BuildRsBaseFileStem(
            _settings.TemporaryRecordingDirectory,
            DateTime.Now);
    }

    private string BuildRsBaseFileName(
        string directory,
        DateTime capturedAt,
        string extension)
    {
        return BuildRsBaseFileStem(directory, capturedAt) + extension;
    }

    private string[] BuildSequentialRsBaseFileNames(
        string directory,
        DateTime capturedAt,
        string extension,
        int count)
    {
        string firstStem = BuildRsBaseFileStem(directory, capturedAt);
        string[] parts = firstStem.Split('~');
        if (parts.Length < 5 ||
            !int.TryParse(parts[1], out int serial))
        {
            return Enumerable.Range(0, count)
                .Select(index => $"{firstStem}_{index + 1:D2}{extension}")
                .ToArray();
        }

        var result = new string[count];
        for (int index = 0; index < count; index++)
        {
            parts[1] = Math.Clamp(serial + index, 1, 9999)
                .ToString("D4");
            result[index] = string.Join("~", parts) + extension;
        }

        return result;
    }

    private string BuildRsBaseFileStem(string directory, DateTime capturedAt)
    {
        string patientId = SanitizeFileName(_patientIdTextBox.Text);
        if (string.IsNullOrWhiteSpace(patientId))
        {
            patientId = "capture";
        }

        string examination = SanitizeFileName(_examinationTypeComboBox.Text);
        if (string.IsNullOrWhiteSpace(examination))
        {
            examination = "camera";
        }

        int serial = GetNextRsBaseSerial(directory, patientId);
        return string.Join(
            "~",
            patientId,
            serial.ToString("D4"),
            capturedAt.ToString("yyyy_MM_dd"),
            examination,
            "RSB");
    }

    private static int GetNextRsBaseSerial(string directory, string patientId)
    {
        if (!Directory.Exists(directory))
        {
            return 1;
        }

        string prefix = patientId + "~";
        int max = 0;
        foreach (string file in Directory.EnumerateFiles(
                     directory,
                     prefix + "*~RSB.*"))
        {
            string name = Path.GetFileNameWithoutExtension(file);
            string[] parts = name.Split('~');
            if (parts.Length >= 2 &&
                int.TryParse(parts[1], out int serial))
            {
                max = Math.Max(max, serial);
            }
        }

        return Math.Clamp(max + 1, 1, 9999);
    }

    private void UpdateCaptureModePresentation()
    {
        _startButton.Text = "プレビュー開始";
    }

    private IReadOnlyList<ModernRadioButton> QuickPresetButtons =>
    [
        _presetButton1,
        _presetButton2,
        _presetButton3,
        _presetButton4,
        _presetButton5
    ];

    private void UpdateInteractionGuards()
    {
        bool isPreviewing = _cameraService.IsRunning;
        bool canChangeCaptureSource =
            !isPreviewing &&
            !_isRefreshingDevices;
        bool canChangePreset =
            !isPreviewing;
        bool canOpenDialogs =
            !isPreviewing;

        foreach (ModernRadioButton button in QuickPresetButtons)
        {
            button.Enabled = canChangePreset && button.Tag is CapturePreset;
        }

        _morePresetsDropDownButton.Enabled =
            canChangePreset && _morePresetsDropDownButton.Visible;
        foreach (ToolStripItem item in _morePresetsMenu.Items)
        {
            item.Enabled = canChangePreset;
        }

        _deviceComboBox.Enabled = canChangeCaptureSource;
        _resolutionComboBox.Enabled = canChangeCaptureSource;
        _refreshDevicesButton.Enabled = canChangeCaptureSource;
        _managePresetsButton.Enabled = canOpenDialogs;
        _openPlaybackButton.Enabled = canOpenDialogs;
        _previewOnlyCheckBox.Enabled = canChangeCaptureSource;
        _fileVideoCheckBox.Enabled = canChangeCaptureSource;
    }

    private void UpdateHeaderOptions()
    {
        bool wasUpdating = _isUpdatingPresetControls;
        _isUpdatingPresetControls = true;
        try
        {
            _previewOnlyCheckBox.Checked =
                _settings.CaptureMode == CaptureOperationMode.PreviewOnly;
            ApplyHeaderOptionRules(null);
        }
        finally
        {
            _isUpdatingPresetControls = wasUpdating;
        }
    }

    private async Task RecordRecordingFilesAsync(
        IReadOnlyList<string> recordingPaths)
    {
        foreach (string recordingPath in recordingPaths)
        {
            await _metadataStore.RecordFileAsync(
                recordingPath,
                _patientIdTextBox.Text,
                _patientNameTextBox.Text,
                _examinationTypeComboBox.Text,
                File.Exists(recordingPath)
                    ? File.GetCreationTime(recordingPath)
                    : DateTime.Now,
                _selectedPreset);
        }
    }

    private void MainForm_ResizeEnd(object? sender, EventArgs e)
    {
        SnapMainWindowToScreenEdges();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmWindowPosChanging &&
            WindowState == FormWindowState.Normal &&
            m.LParam != IntPtr.Zero)
        {
            SnapChangingWindowPosition(m.LParam);
        }

        base.WndProc(ref m);
    }

    private void SnapMainWindowToScreenEdges()
    {
        if (WindowState != FormWindowState.Normal)
        {
            return;
        }

        FrameInsets insets = GetVisibleFrameInsets();
        Rectangle snappedBounds = SnapMoveBoundsToScreenEdges(
            Bounds,
            insets);
        if (snappedBounds != Bounds)
        {
            Bounds = snappedBounds;
        }
    }

    private void SnapChangingWindowPosition(IntPtr windowPosPointer)
    {
        WindowPos windowPos = Marshal.PtrToStructure<WindowPos>(
            windowPosPointer);
        bool canMove = (windowPos.flags & SwpNoMove) == 0;
        bool canSize = (windowPos.flags & SwpNoSize) == 0;
        if (!canMove && !canSize)
        {
            return;
        }

        Rectangle proposedBounds = new(
            canMove ? windowPos.x : Left,
            canMove ? windowPos.y : Top,
            canSize ? windowPos.cx : Width,
            canSize ? windowPos.cy : Height);
        bool isResizing =
            proposedBounds.Width != Width ||
            proposedBounds.Height != Height;
        Rectangle snappedBounds = isResizing
            ? SnapResizeBoundsToScreenEdges(
                proposedBounds,
                GetVisibleFrameInsets())
            : SnapMoveBoundsToScreenEdges(
                proposedBounds,
                GetVisibleFrameInsets());

        if (canMove)
        {
            windowPos.x = snappedBounds.Left;
            windowPos.y = snappedBounds.Top;
        }

        if (canSize)
        {
            windowPos.cx = snappedBounds.Width;
            windowPos.cy = snappedBounds.Height;
        }

        Marshal.StructureToPtr(windowPos, windowPosPointer, false);
    }

    private Rectangle SnapMoveBoundsToScreenEdges(
        Rectangle bounds,
        FrameInsets insets)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return bounds;
        }

        Rectangle visibleBounds = insets.Apply(bounds);
        Rectangle workingArea = Screen.FromRectangle(visibleBounds).WorkingArea;
        int x = bounds.Left;
        int y = bounds.Top;
        int width = bounds.Width;
        int height = bounds.Height;

        if (Math.Abs(visibleBounds.Left - workingArea.Left) <=
            WindowSnapDistance)
        {
            x = workingArea.Left - insets.Left;
        }
        else if (Math.Abs(visibleBounds.Right - workingArea.Right) <=
                 WindowSnapDistance)
        {
            int snappedRight = workingArea.Right + insets.Right;
            x = snappedRight - width;
        }

        if (Math.Abs(visibleBounds.Top - workingArea.Top) <=
            WindowSnapDistance)
        {
            y = workingArea.Top - insets.Top;
        }
        else if (Math.Abs(visibleBounds.Bottom - workingArea.Bottom) <=
                 WindowSnapDistance)
        {
            int snappedBottom = workingArea.Bottom + insets.Bottom;
            y = snappedBottom - height;
        }

        return new Rectangle(x, y, width, height);
    }

    private Rectangle SnapResizeBoundsToScreenEdges(
        Rectangle bounds,
        FrameInsets insets)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return bounds;
        }

        Rectangle visibleBounds = insets.Apply(bounds);
        Rectangle workingArea = Screen.FromRectangle(visibleBounds).WorkingArea;
        int x = bounds.Left;
        int y = bounds.Top;
        int right = bounds.Right;
        int bottom = bounds.Bottom;

        bool leftEdgeChanged = bounds.Left != Left;
        bool rightEdgeChanged = bounds.Right != Right;
        bool topEdgeChanged = bounds.Top != Top;
        bool bottomEdgeChanged = bounds.Bottom != Bottom;

        if (leftEdgeChanged &&
            Math.Abs(visibleBounds.Left - workingArea.Left) <=
            WindowSnapDistance)
        {
            x = workingArea.Left - insets.Left;
        }
        else if (rightEdgeChanged &&
                 Math.Abs(visibleBounds.Right - workingArea.Right) <=
                 WindowSnapDistance)
        {
            right = workingArea.Right + insets.Right;
        }

        if (topEdgeChanged &&
            Math.Abs(visibleBounds.Top - workingArea.Top) <=
            WindowSnapDistance)
        {
            y = workingArea.Top - insets.Top;
        }
        else if (bottomEdgeChanged &&
                 Math.Abs(visibleBounds.Bottom - workingArea.Bottom) <=
                 WindowSnapDistance)
        {
            bottom = workingArea.Bottom + insets.Bottom;
        }

        int width = Math.Max(MinimumSize.Width, right - x);
        int height = Math.Max(MinimumSize.Height, bottom - y);
        return new Rectangle(x, y, width, height);
    }

    private FrameInsets GetVisibleFrameInsets()
    {
        if (IsHandleCreated &&
            DwmGetWindowAttribute(
                Handle,
                DwmwaExtendedFrameBounds,
                out NativeRect nativeRect,
                Marshal.SizeOf<NativeRect>()) == 0)
        {
            Rectangle visibleBounds = nativeRect.ToRectangle();
            if (!visibleBounds.IsEmpty)
            {
                return new FrameInsets(
                    visibleBounds.Left - Left,
                    visibleBounds.Top - Top,
                    Right - visibleBounds.Right,
                    Bottom - visibleBounds.Bottom);
            }
        }

        return FrameInsets.Empty;
    }

    private void RestoreMainWindowBounds()
    {
        if (_settings.MainWindowWidth <= 0 ||
            _settings.MainWindowHeight <= 0 ||
            _settings.MainWindowLeft == int.MinValue ||
            _settings.MainWindowTop == int.MinValue)
        {
            return;
        }

        var bounds = new Rectangle(
            _settings.MainWindowLeft,
            _settings.MainWindowTop,
            _settings.MainWindowWidth,
            _settings.MainWindowHeight);
        Rectangle visibleArea = Rectangle.Inflate(bounds, -40, -40);
        bool isVisibleOnAnyScreen = Screen.AllScreens.Any(
            screen => screen.WorkingArea.IntersectsWith(visibleArea));
        if (!isVisibleOnAnyScreen)
        {
            return;
        }

        StartPosition = FormStartPosition.Manual;
        Bounds = bounds;
    }

    private CaptureOperationMode GetEffectiveCaptureMode()
    {
        return _previewOnlyCheckBox.Checked
            ? CaptureOperationMode.PreviewOnly
            : CaptureOperationMode.ContinuousTemporaryRecording;
    }

    private bool ShouldUsePreviewTopMost()
    {
        return _settings.TopMostDuringPreview &&
            _cameraService.IsRunning;
    }

    private void UpdatePreviewTopMostState()
    {
        bool topMost = ShouldUsePreviewTopMost();
        TopMost = topMost;
        if (_previewZoomForm is { IsDisposed: false })
        {
            _previewZoomForm.TopMost = topMost;
        }
    }

    private void ApplyHeaderOptionRules(object? changedControl)
    {
        if (ReferenceEquals(changedControl, _fileVideoCheckBox) &&
            _fileVideoCheckBox.Checked)
        {
            _previewOnlyCheckBox.Checked = false;
        }
        else if (ReferenceEquals(changedControl, _previewOnlyCheckBox) &&
                 _previewOnlyCheckBox.Checked)
        {
            _fileVideoCheckBox.Checked = false;
        }
        else if (_previewOnlyCheckBox.Checked &&
                 _fileVideoCheckBox.Checked)
        {
            _previewOnlyCheckBox.Checked = false;
        }
    }

    private void HeaderOptionCheckBox_CheckedChanged(
        object? sender,
        EventArgs e)
    {
        if (_isUpdatingPresetControls)
        {
            return;
        }

        bool wasUpdating = _isUpdatingPresetControls;
        _isUpdatingPresetControls = true;
        try
        {
            ApplyHeaderOptionRules(sender);
        }
        finally
        {
            _isUpdatingPresetControls = wasUpdating;
        }

        UpdateCaptureModePresentation();
    }

    private void StartExternalIntegrations()
    {
        _hotkeyService.Start(
            _settings.SnapshotHotkey,
            _settings.CaptureHotkey);
        _theptWatcher.Start(_settings.RsBaseTheptDirectory);
    }

    private void HotkeyService_SnapshotPressed()
    {
        try
        {
            BeginInvoke(() =>
            {
                if (_snapshotButton.Enabled)
                {
                    SnapshotButton_Click(this, EventArgs.Empty);
                }
            });
        }
        catch (InvalidOperationException)
        {
        }
    }

    private void HotkeyService_CapturePressed()
    {
        try
        {
            BeginInvoke(() => StartButton_Click(this, EventArgs.Empty));
        }
        catch (InvalidOperationException)
        {
        }
    }

    private void TheptWatcher_PatientChanged(string id, string name)
    {
        _ = _metadataStore.UpsertPatientAsync(id, name);
        try
        {
            BeginInvoke(() =>
            {
                ClearPreviewAndThumbnails();
                _patientIdTextBox.Text = id;
                _patientNameTextBox.Text = name;
                _statusLabel.Text = "●  RSBase患者情報を読み込みました";
                _statusLabel.ForeColor = Theme.AccentBright;
            });
        }
        catch (InvalidOperationException)
        {
        }
    }

    private async Task RecordCapturedFileAsync(
        string filePath,
        DateTime capturedAt)
    {
        try
        {
            await _metadataStore.RecordFileAsync(
                filePath,
                _patientIdTextBox.Text,
                _patientNameTextBox.Text,
                _examinationTypeComboBox.Text,
                capturedAt,
                GetActiveSnapshotPreset());
        }
        catch (Exception exception)
        {
            try
            {
                BeginInvoke(() =>
                {
                    _statusLabel.Text =
                        "● メタデータ保存に失敗: " + exception.Message;
                    _statusLabel.ForeColor = Theme.Danger;
                });
            }
            catch (InvalidOperationException)
            {
            }
        }
    }

    private void CleanupTemporaryRecordings()
    {
        if (_settings.TemporaryFileRetentionDays <= 0 ||
            !Directory.Exists(_settings.TemporaryRecordingDirectory))
        {
            return;
        }

        DateTime threshold = DateTime.Now.AddDays(
            -_settings.TemporaryFileRetentionDays);
        foreach (string extension in new[] { "*.avi", "*.mp4" })
        {
            foreach (string file in Directory.EnumerateFiles(
                         _settings.TemporaryRecordingDirectory,
                         extension))
            {
                try
                {
                    if (File.GetLastWriteTime(file) < threshold)
                    {
                        File.Delete(file);
                    }
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
    }

    private async Task StartPlaybackAsync(
        IReadOnlyList<string> filePaths)
    {
        EnterPlaybackFilterMode();
        await ApplyPlaybackMetadataAsync(filePaths);
        await _playbackService.OpenAsync(filePaths);
        _playbackFilePaths = filePaths.ToArray();
        ClearClipSelection();
        playbackPanel.Visible = true;
        playbackPanel.BringToFront();
        LayoutPreviewBox();
        PositionThumbnailActionButtons();
        liveBadgeLabel.Text = " PLAYBACK ";
        UpdatePlaybackControlCaptions();
        _previewMessage.Visible = false;
        _snapshotButton.Enabled = true;
        _openClipEditorButton.Enabled = true;
        _statusLabel.Text =
            $"●  一次動画を再生中: {filePaths.Count}ファイル";
        _statusLabel.ForeColor = Theme.AccentBright;
        UpdatePreviewTopMostState();
        UpdateInteractionGuards();
    }

    private async Task ApplyPlaybackMetadataAsync(
        IReadOnlyList<string> filePaths)
    {
        _playbackCapturePreset = CreatePlaybackFallbackPreset(string.Empty);
        _playbackCapturedAt = null;
        string? firstFile = filePaths.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(firstFile))
        {
            return;
        }

        try
        {
            CapturedFileMetadata? metadata =
                await _metadataStore.FindFileAsync(firstFile);
            if (metadata is not null)
            {
                ApplyPlaybackMetadata(metadata);
                return;
            }

            ParsedCaptureFileName? parsed =
                PatientMetadataStore.ParseCaptureFileName(firstFile);
            if (parsed is null)
            {
                return;
            }

            string patientName =
                await _metadataStore.FindPatientNameAsync(parsed.PatientId)
                ?? string.Empty;
            _patientIdTextBox.Text = parsed.PatientId;
            _patientNameTextBox.Text = patientName;
            _examinationTypeComboBox.Text = parsed.ExaminationName;
            _playbackCapturePreset =
                CreatePlaybackFallbackPreset(parsed.ExaminationName);
            _playbackCapturedAt = parsed.CapturedAt;
            _presetLabel.Text = "プリセット: なし（再生ファイル）";
        }
        catch (Exception exception)
        {
            _statusLabel.Text =
                "● 再生メタデータ復元に失敗: " + exception.Message;
            _statusLabel.ForeColor = Theme.Danger;
        }
    }

    private void ApplyPlaybackMetadata(CapturedFileMetadata metadata)
    {
        CapturePreset? preset =
            metadata.PresetId is { } presetId
                ? _settings.Presets.FirstOrDefault(item => item.Id == presetId)
                : null;
        _playbackCapturePreset = metadata.Preset ?? preset;
        _playbackCapturedAt = metadata.CapturedAt;
        if (_playbackCapturePreset is null)
        {
            _playbackCapturePreset =
                CreatePlaybackFallbackPreset(metadata.ExaminationName);
        }

        if (preset is not null)
        {
            _presetLabel.Text = $"プリセット: {preset.Name}（再生ファイル）";
        }
        else if (_playbackCapturePreset is not null)
        {
            _presetLabel.Text =
                $"プリセット: {_playbackCapturePreset.Name}（再生ファイル）";
        }

        if (!string.IsNullOrWhiteSpace(metadata.PatientId))
        {
            _patientIdTextBox.Text = metadata.PatientId;
        }

        if (!string.IsNullOrWhiteSpace(metadata.PatientName))
        {
            _patientNameTextBox.Text = metadata.PatientName;
        }

        if (!string.IsNullOrWhiteSpace(metadata.ExaminationName))
        {
            _examinationTypeComboBox.Text = metadata.ExaminationName;
        }
    }

    private static CapturePreset CreatePlaybackFallbackPreset(
        string examinationName)
    {
        return new CapturePreset
        {
            Name = "再生ファイル",
            ExaminationType = examinationName,
            Roi = string.Empty,
            OverlayText = string.Empty,
            FontName = string.Empty,
            WhiteBalanceRed = 255,
            WhiteBalanceGreen = 255,
            WhiteBalanceBlue = 255,
            Gamma = 1.0
        };
    }

    private async void OpenPlaybackButton_Click(
        object? sender,
        EventArgs e)
    {
        if (_cameraService.IsRunning)
        {
            MessageBox.Show(
                this,
                "プレビューまたは録画を停止してから再生してください。",
                "ENTcapture2",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        using var dialog = new OpenFileDialog
        {
            Title = "一時録画ファイルを選択",
            Filter =
                "動画ファイル (*.mp4;*.avi;*.mkv)|*.mp4;*.avi;*.mkv|" +
                "すべてのファイル (*.*)|*.*",
            Multiselect = true,
            InitialDirectory =
                Directory.Exists(_settings.TemporaryRecordingDirectory)
                    ? _settings.TemporaryRecordingDirectory
                    : string.Empty
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            _openPlaybackButton.Enabled = false;
            await StartPlaybackAsync(
                dialog.FileNames
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray());
        }
        catch (Exception exception)
        {
            ShowError("動画を再生できませんでした。", exception);
        }
        finally
        {
            _openPlaybackButton.Enabled = true;
            UpdateInteractionGuards();
        }
    }

    private async Task ToggleSidePanelAsync()
    {
        bool show = !sidePanel.Visible;
        workspaceLayout.SuspendLayout();
        try
        {
            sidePanel.Visible = show;
            workspaceLayout.ColumnStyles[2].Width = show ? 310 : 0;
            _toggleSidePanelButton.Text = show ? "＞" : "＜ 補正";
            _toggleSidePanelButton.AccessibleName = show
                ? "詳細パネルを閉じる"
                : "詳細パネルを開く";
        }
        finally
        {
            workspaceLayout.ResumeLayout(true);
        }

        if (!show)
        {
            if (_whiteBalanceSelectionCheckBox.Checked)
            {
                _whiteBalanceSelectionCheckBox.Checked = false;
            }

            await SaveSelectedPresetAsync();
        }
    }

    private void RefreshExaminationTypes()
    {
        string selected = _examinationTypeComboBox.Text;
        string[] examinationTypes = _settings.ExaminationTypes
            .Split(
                [',', '、', ';', '\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        _examinationTypeComboBox.BeginUpdate();
        try
        {
            _examinationTypeComboBox.Items.Clear();
            _examinationTypeComboBox.Items.AddRange(
                examinationTypes.Cast<object>().ToArray());
        }
        finally
        {
            _examinationTypeComboBox.EndUpdate();
        }

        _examinationTypeComboBox.Text = selected;
    }

    private void ExaminationTypeComboBox_TextChanged(
        object? sender,
        EventArgs e)
    {
        if (_isUpdatingPresetControls || _selectedPreset is null)
        {
            return;
        }

        _selectedPreset.ExaminationType =
            _examinationTypeComboBox.Text.Trim();
    }

    private async Task ClosePlaybackAsync()
    {
        LeavePlaybackFilterMode();
        await _playbackService.CloseAsync();
        _playbackSourceImage?.Dispose();
        _playbackSourceImage = null;
        _playbackCapturePreset = null;
        _playbackCapturedAt = null;
        ClearPreviewImage();
        _playbackFilePaths = [];
        _lastPlaybackPosition = null;
        ClearClipSelection();
        UpdatePlaybackControlCaptions();
        playbackPanel.Visible = false;
        LayoutPreviewBox();
        PositionThumbnailActionButtons();
        liveBadgeLabel.Text = " LIVE PREVIEW ";
        _snapshotButton.Enabled = false;
        _openClipEditorButton.Enabled = false;
        _previewMessage.Visible = true;
        _playbackTrackBar.Value = 0;
        _playbackPositionLabel.Text = "00:00 / 00:00";
        _fpsLabel.Text = "INPUT -- fps  /  PREVIEW -- fps";
        _statusLabel.Text = "●  スタンバイ";
        _statusLabel.ForeColor = Theme.Muted;
        UpdatePreviewTopMostState();
        UpdateInteractionGuards();
    }

    private void PlayPauseButton_Click(object? sender, EventArgs e)
    {
        TogglePlaybackPause();
    }

    private void TogglePlaybackPause()
    {
        if (!_playbackService.IsOpen)
        {
            return;
        }

        _playbackService.TogglePause();
        UpdatePlaybackControlCaptions();
    }

    private void UpdatePlaybackControlCaptions()
    {
        bool isOpen = _playbackService.IsOpen;
        bool isPaused = !isOpen || _playbackService.IsPaused;
        _playPauseButton.Text = isPaused
            ? PlaybackCaptions.Play
            : PlaybackCaptions.Pause;
        _previewZoomForm?.SetPlaybackState(isOpen, isPaused);
    }

    private void OpenClipEditorButton_Click(object? sender, EventArgs e)
    {
        if (_playbackFilePaths.Count == 0)
        {
            _statusLabel.Text = "● 編集する一時録画がありません";
            _statusLabel.ForeColor = Theme.Danger;
            return;
        }

        string outputDirectory = Directory.Exists(_settings.SnapshotDirectory)
            ? _settings.SnapshotDirectory
            : Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        if (!_playbackService.IsPaused)
        {
            _playbackService.Pause();
            UpdatePlaybackControlCaptions();
        }

        UpdatePreviewTopMostState();
        using var dialog = new ClipEditorForm(
            _playbackFilePaths,
            outputDirectory,
            _settings.FinalVideoCodec,
            _settings.FinalVideoQuality,
            () => _lastPlaybackPosition,
            time => _playbackService.Seek(time),
            CaptureDisplayedImage,
            BuildRsBaseFileName,
            BuildSequentialRsBaseFileNames);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _statusLabel.Text = "● 動画クリップを出力しました";
            _statusLabel.ForeColor = Theme.AccentBright;
        }
    }

    private void PlaybackTrackBar_MouseUp(object? sender, MouseEventArgs e)
    {
        _isSeekingPlayback = false;
        SeekPlaybackFromTrackBar();
    }

    private void PlaybackTrackBar_Scroll(object? sender, EventArgs e)
    {
        _isSeekingPlayback = true;
        SeekPlaybackFromTrackBar();
    }

    private void SeekPlaybackFromTrackBar()
    {
        if (_playbackTrackBar.Tag is TimeSpan totalTime &&
            totalTime > TimeSpan.Zero)
        {
            long ticks = (long)Math.Round(
                _playbackTrackBar.Value /
                (double)_playbackTrackBar.Maximum *
                totalTime.Ticks);
            _playbackService.Seek(TimeSpan.FromTicks(
                Math.Clamp(ticks, 0, totalTime.Ticks)));
            UpdatePlaybackControlCaptions();
        }
    }

    private void MarkClipStartButton_Click(object? sender, EventArgs e)
    {
        _clipStartTime = GetCurrentPlaybackTime();
        if (_clipEndTime is not null && _clipEndTime <= _clipStartTime)
        {
            _clipEndTime = null;
        }

        UpdateClipEditorPresentation();
    }

    private void MarkClipEndButton_Click(object? sender, EventArgs e)
    {
        _clipEndTime = GetCurrentPlaybackTime();
        if (_clipStartTime is not null && _clipEndTime <= _clipStartTime)
        {
            (_clipStartTime, _clipEndTime) = (_clipEndTime, _clipStartTime);
        }

        UpdateClipEditorPresentation();
    }

    private void AddClipButton_Click(object? sender, EventArgs e)
    {
        if (_clipStartTime is null || _clipEndTime is null)
        {
            _statusLabel.Text = "●  始点と終点を指定してください";
            _statusLabel.ForeColor = Theme.Danger;
            return;
        }

        TimeSpan start = _clipStartTime.Value;
        TimeSpan end = _clipEndTime.Value;
        if (end <= start)
        {
            _statusLabel.Text = "●  終点は始点より後にしてください";
            _statusLabel.ForeColor = Theme.Danger;
            return;
        }

        _clipRanges.Add(new ClipExportRange(start, end));
        _clipStartTime = null;
        _clipEndTime = null;
        UpdateClipEditorPresentation();
    }

    private void ClearClipsButton_Click(object? sender, EventArgs e)
    {
        ClearClipSelection();
    }

    private async void ExportJoinedClipsButton_Click(
        object? sender,
        EventArgs e)
    {
        await ExportClipsAsync(join: true);
    }

    private async void ExportSeparateClipsButton_Click(
        object? sender,
        EventArgs e)
    {
        await ExportClipsAsync(join: false);
    }

    private async Task ExportClipsAsync(bool join)
    {
        if (_playbackFilePaths.Count == 0 || _clipRanges.Count == 0)
        {
            _statusLabel.Text = "●  出力する区間がありません";
            _statusLabel.ForeColor = Theme.Danger;
            return;
        }

        using var dialog = new FolderBrowserDialog
        {
            Description = "クリップの出力先フォルダーを選択してください",
            InitialDirectory = Directory.Exists(_settings.SnapshotDirectory)
                ? _settings.SnapshotDirectory
                : Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            UseDescriptionForTitle = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            SetClipExportButtonsEnabled(false);
            var exporter = new ClipExportService();
            var progress = new Progress<string>(message =>
            {
                _statusLabel.Text = "●  " + message;
                _statusLabel.ForeColor = Theme.AccentBright;
            });
            string extension = GetPlaybackOutputExtension();
            if (join)
            {
                string outputFile = Path.Combine(
                    dialog.SelectedPath,
                    BuildRsBaseFileName(
                        dialog.SelectedPath,
                        DateTime.Now,
                        extension));
                await exporter.ExportJoinedAsync(
                    _playbackFilePaths,
                    _clipRanges,
                    outputFile,
                    progress: progress);
            }
            else
            {
                string[] outputFiles = BuildSequentialRsBaseFileNames(
                    dialog.SelectedPath,
                    DateTime.Now,
                    extension,
                    _clipRanges.Count)
                    .Select(fileName => Path.Combine(dialog.SelectedPath, fileName))
                    .ToArray();
                await exporter.ExportSeparateAsync(
                    _playbackFilePaths,
                    _clipRanges,
                    outputFiles,
                    progress: progress);
            }

            _statusLabel.Text = join
                ? "●  連結クリップを出力しました"
                : "●  個別クリップを出力しました";
            _statusLabel.ForeColor = Theme.AccentBright;
        }
        catch (Exception exception)
        {
            ShowError("クリップを出力できませんでした。", exception);
        }
        finally
        {
            SetClipExportButtonsEnabled(true);
        }
    }

    private TimeSpan GetCurrentPlaybackTime()
    {
        if (_lastPlaybackPosition is not null)
        {
            return _lastPlaybackPosition.CurrentTime;
        }

        return TimeSpan.Zero;
    }

    private Bitmap? CaptureDisplayedImage()
    {
        if (_displayedImage is null)
        {
            return null;
        }

        try
        {
            return (Bitmap)_displayedImage.Clone();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private void ClearClipSelection()
    {
        _clipStartTime = null;
        _clipEndTime = null;
        _clipRanges.Clear();
        UpdateClipEditorPresentation();
    }

    private void UpdateClipEditorPresentation()
    {
        _clipRangeLabel.Text =
            $"始点 {FormatClipTime(_clipStartTime)} / " +
            $"終点 {FormatClipTime(_clipEndTime)}";
        _clipListBox.BeginUpdate();
        try
        {
            _clipListBox.Items.Clear();
            for (int index = 0; index < _clipRanges.Count; index++)
            {
                ClipExportRange range = _clipRanges[index];
                _clipListBox.Items.Add(
                    $"{index + 1:D2}: " +
                    $"{FormatElapsed(range.Start)} - " +
                    $"{FormatElapsed(range.End)}");
            }
        }
        finally
        {
            _clipListBox.EndUpdate();
        }
    }

    private void SetClipExportButtonsEnabled(bool enabled)
    {
        _markClipStartButton.Enabled = enabled;
        _markClipEndButton.Enabled = enabled;
        _addClipButton.Enabled = enabled;
        _clearClipsButton.Enabled = enabled;
        _exportJoinedClipsButton.Enabled = enabled;
        _exportSeparateClipsButton.Enabled = enabled;
    }

    private string GetPlaybackOutputExtension()
    {
        string extension = _playbackFilePaths.Count > 0
            ? Path.GetExtension(_playbackFilePaths[0])
            : ".mp4";
        return string.IsNullOrWhiteSpace(extension)
            ? ".mp4"
            : extension;
    }

    private static string FormatClipTime(TimeSpan? value)
    {
        return value is null ? "--:--" : FormatElapsed(value.Value);
    }

    private void PlaybackService_FrameReady(
        Bitmap bitmap,
        PlaybackPosition position)
    {
        if (_isClosing)
        {
            bitmap.Dispose();
            return;
        }

        Bitmap rawBitmap = bitmap;
        Bitmap displayBitmap =
            ApplyDisplayFilter((Bitmap)rawBitmap.Clone(), _playbackFilterState);

        try
        {
            BeginInvoke(() =>
            {
                _lastPlaybackPosition = position;
                Bitmap? previousSource = _playbackSourceImage;
                Bitmap? previous = _displayedImage;
                _playbackSourceImage = rawBitmap;
                _displayedImage = displayBitmap;
                _previewBox.Image = displayBitmap;
                UpdatePreviewZoomForm(displayBitmap);
                previousSource?.Dispose();
                previous?.Dispose();

                if (!_isSeekingPlayback)
                {
                    _playbackTrackBar.Tag = position.TotalTime;
                    _playbackTrackBar.Value = Math.Clamp(
                        (int)Math.Round(
                            position.CurrentTime.Ticks /
                            (double)Math.Max(1, position.TotalTime.Ticks) *
                            _playbackTrackBar.Maximum),
                        _playbackTrackBar.Minimum,
                        _playbackTrackBar.Maximum);
                }

                _playbackPositionLabel.Text =
                    $"{FormatElapsed(position.CurrentTime)} / " +
                    $"{FormatElapsed(position.TotalTime)}";
                _fpsLabel.Text =
                    $"PLAYBACK {position.PlaybackFramesPerSecond:0.0} fps";
                UpdatePlaybackControlCaptions();
            });
        }
        catch (InvalidOperationException)
        {
            rawBitmap.Dispose();
            displayBitmap.Dispose();
        }
    }

    private void CameraService_FrameReady(Bitmap bitmap)
    {
        if (_isClosing)
        {
            bitmap.Dispose();
            return;
        }

        if (Interlocked.Exchange(ref _frameRenderPending, 1) == 1)
        {
            bitmap.Dispose();
            return;
        }

        try
        {
            BeginInvoke(() =>
            {
                try
                {
                    if (_isClosing)
                    {
                        bitmap.Dispose();
                        return;
                    }

                    Bitmap? previous = _displayedImage;
                    _displayedImage = bitmap;
                    _previewBox.Image = bitmap;
                    UpdatePreviewZoomForm(bitmap);
                    previous?.Dispose();
                }
                finally
                {
                    Interlocked.Exchange(ref _frameRenderPending, 0);
                }
            });
        }
        catch (InvalidOperationException)
        {
            bitmap.Dispose();
            Interlocked.Exchange(ref _frameRenderPending, 0);
        }
    }

    private void CameraService_StatisticsUpdated(CaptureStatistics statistics)
    {
        if (_isClosing)
        {
            return;
        }

        try
        {
            BeginInvoke(() =>
            {
                _fpsLabel.Text =
                    $"INPUT {statistics.InputFramesPerSecond:0.0} fps  /  " +
                    $"PREVIEW {statistics.PreviewFramesPerSecond:0.0} fps  /  " +
                    $"DROP {statistics.DroppedFrames}" +
                    (_recordingClock.IsRunning
                        ? $"  /  REC {FormatElapsed(_recordingClock.Elapsed)}" +
                          (!string.IsNullOrWhiteSpace(
                              _cameraService.LastRecordingEncoder)
                              ? $"  /  ENC {_cameraService.LastRecordingEncoder}"
                              : string.Empty)
                        : string.Empty);
            });
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static string FormatElapsed(TimeSpan value)
    {
        return value.TotalHours >= 1
            ? value.ToString(@"hh\:mm\:ss")
            : value.ToString(@"mm\:ss");
    }

    private void WhiteBalanceSelectionCheckBox_CheckedChanged(
        object? sender,
        EventArgs e)
    {
        bool enabled = _whiteBalanceSelectionCheckBox.Checked;
        _cameraService.SetUnprocessedSnapshotEnabled(enabled);
        _previewBox.Cursor = enabled ? Cursors.Cross : Cursors.Default;
        if (!enabled)
        {
            _isSelectingWhiteBalance = false;
            _whiteBalanceSelection = Rectangle.Empty;
            _previewBox.Invalidate();
        }

        _statusLabel.Text = enabled
            ? "●  プレビュー上の白またはグレーの部分をドラッグしてください"
            : "●  ホワイトバランス範囲選択 OFF";
        _statusLabel.ForeColor = enabled
            ? Theme.AccentBright
            : Theme.Muted;
    }

    private void PreviewBox_MouseDown(object? sender, MouseEventArgs e)
    {
        if (!_whiteBalanceSelectionCheckBox.Checked ||
            e.Button != MouseButtons.Left ||
            _previewBox.Image is null)
        {
            return;
        }

        Rectangle imageRectangle = GetPreviewImageRectangle();
        if (!imageRectangle.Contains(e.Location))
        {
            return;
        }

        _isSelectingWhiteBalance = true;
        _whiteBalanceDragStart = e.Location;
        _whiteBalanceSelection =
            new Rectangle(e.Location, Size.Empty);
        _previewBox.Capture = true;
    }

    private void PreviewBox_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_isSelectingWhiteBalance)
        {
            return;
        }

        Rectangle imageRectangle = GetPreviewImageRectangle();
        Point current = new(
            Math.Clamp(e.X, imageRectangle.Left, imageRectangle.Right - 1),
            Math.Clamp(e.Y, imageRectangle.Top, imageRectangle.Bottom - 1));
        _whiteBalanceSelection = Rectangle.FromLTRB(
            Math.Min(_whiteBalanceDragStart.X, current.X),
            Math.Min(_whiteBalanceDragStart.Y, current.Y),
            Math.Max(_whiteBalanceDragStart.X, current.X),
            Math.Max(_whiteBalanceDragStart.Y, current.Y));
        _previewBox.Invalidate();
    }

    private void PreviewBox_MouseUp(object? sender, MouseEventArgs e)
    {
        if (!_isSelectingWhiteBalance)
        {
            return;
        }

        _isSelectingWhiteBalance = false;
        _previewBox.Capture = false;
        if (_whiteBalanceSelection.Width < 4 ||
            _whiteBalanceSelection.Height < 4)
        {
            _whiteBalanceSelection = Rectangle.Empty;
            _previewBox.Invalidate();
            return;
        }

        ApplyWhiteBalanceFromSelection();
    }

    private void PreviewBox_Paint(object? sender, PaintEventArgs e)
    {
        if (_whiteBalanceSelection.IsEmpty)
        {
            return;
        }

        using var fill = new SolidBrush(Color.FromArgb(40, Theme.AccentBright));
        using var outline = new Pen(Theme.AccentBright, 2F)
        {
            DashStyle = System.Drawing.Drawing2D.DashStyle.Dash
        };
        e.Graphics.FillRectangle(fill, _whiteBalanceSelection);
        e.Graphics.DrawRectangle(outline, _whiteBalanceSelection);
    }

    private void ApplyWhiteBalanceFromSelection()
    {
        using Bitmap? source = GetWhiteBalanceSourceImage();
        if (source is null)
        {
            _statusLabel.Text = "●  ホワイトバランスを取得できる画像がありません";
            _statusLabel.ForeColor = Theme.Danger;
            return;
        }

        Rectangle imageRectangle = GetPreviewImageRectangle();
        Rectangle roi = MapPreviewSelectionToImage(
            _whiteBalanceSelection,
            imageRectangle,
            source.Size);
        if (_flipHorizontalCheckBox.Checked)
        {
            roi.X = source.Width - roi.Right;
        }

        if (_flipVerticalCheckBox.Checked)
        {
            roi.Y = source.Height - roi.Bottom;
        }

        (double red, double green, double blue) =
            CalculateAverageColor(source, roi);
        if (red < 1 || green < 1 || blue < 1)
        {
            _statusLabel.Text =
                "●  選択範囲が暗すぎるためホワイトバランスを計算できません";
            _statusLabel.ForeColor = Theme.Danger;
            return;
        }

        double target = Math.Max(red, Math.Max(green, blue));
        _isApplyingPreset = true;
        try
        {
            SetTrackBarValue(
                _redTrack,
                (int)Math.Round(255 * target / red));
            SetTrackBarValue(
                _greenTrack,
                (int)Math.Round(255 * target / green));
            SetTrackBarValue(
                _blueTrack,
                (int)Math.Round(255 * target / blue));
        }
        finally
        {
            _isApplyingPreset = false;
        }

        ProcessingControlChanged();
        _statusLabel.Text =
            $"●  WB適用 R {_redTrack.Value} / " +
            $"G {_greenTrack.Value} / B {_blueTrack.Value}";
        _statusLabel.ForeColor = Theme.AccentBright;
    }

    private Bitmap? GetWhiteBalanceSourceImage()
    {
        if (_playbackService.IsOpen)
        {
            if (_playbackSourceImage is not null)
            {
                return (Bitmap)_playbackSourceImage.Clone();
            }

            return _displayedImage is null
                ? null
                : (Bitmap)_displayedImage.Clone();
        }

        return _cameraService.GetUnprocessedSnapshot();
    }

    private Rectangle GetPreviewImageRectangle()
    {
        if (_previewBox.Image is null ||
            _previewBox.ClientSize.Width <= 0 ||
            _previewBox.ClientSize.Height <= 0)
        {
            return Rectangle.Empty;
        }

        double scale = Math.Min(
            _previewBox.ClientSize.Width /
            (double)_previewBox.Image.Width,
            _previewBox.ClientSize.Height /
            (double)_previewBox.Image.Height);
        int width = Math.Max(
            1,
            (int)Math.Round(_previewBox.Image.Width * scale));
        int height = Math.Max(
            1,
            (int)Math.Round(_previewBox.Image.Height * scale));
        return new Rectangle(
            (_previewBox.ClientSize.Width - width) / 2,
            (_previewBox.ClientSize.Height - height) / 2,
            width,
            height);
    }

    private static Rectangle MapPreviewSelectionToImage(
        Rectangle selection,
        Rectangle displayedImage,
        Size imageSize)
    {
        int left = (int)Math.Floor(
            (selection.Left - displayedImage.Left) /
            (double)displayedImage.Width *
            imageSize.Width);
        int top = (int)Math.Floor(
            (selection.Top - displayedImage.Top) /
            (double)displayedImage.Height *
            imageSize.Height);
        int right = (int)Math.Ceiling(
            (selection.Right - displayedImage.Left) /
            (double)displayedImage.Width *
            imageSize.Width);
        int bottom = (int)Math.Ceiling(
            (selection.Bottom - displayedImage.Top) /
            (double)displayedImage.Height *
            imageSize.Height);
        left = Math.Clamp(left, 0, imageSize.Width - 1);
        top = Math.Clamp(top, 0, imageSize.Height - 1);
        right = Math.Clamp(right, left + 1, imageSize.Width);
        bottom = Math.Clamp(bottom, top + 1, imageSize.Height);
        return Rectangle.FromLTRB(left, top, right, bottom);
    }

    private static (double Red, double Green, double Blue)
        CalculateAverageColor(Bitmap source, Rectangle roi)
    {
        using var bitmap = new Bitmap(
            source.Width,
            source.Height,
            PixelFormat.Format24bppRgb);
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.DrawImageUnscaled(source, 0, 0);
        }

        BitmapData data = bitmap.LockBits(
            roi,
            ImageLockMode.ReadOnly,
            PixelFormat.Format24bppRgb);
        try
        {
            int stride = Math.Abs(data.Stride);
            byte[] pixels = new byte[stride * roi.Height];
            Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
            int sampleStep = Math.Max(
                1,
                (int)Math.Sqrt(roi.Width * (double)roi.Height / 100000));
            long samples = 0;
            long red = 0;
            long green = 0;
            long blue = 0;
            for (int y = 0; y < roi.Height; y += sampleStep)
            {
                int row = y * stride;
                for (int x = 0; x < roi.Width; x += sampleStep)
                {
                    int offset = row + x * 3;
                    blue += pixels[offset];
                    green += pixels[offset + 1];
                    red += pixels[offset + 2];
                    samples++;
                }
            }

            return samples == 0
                ? (0, 0, 0)
                : (
                    red / (double)samples,
                    green / (double)samples,
                    blue / (double)samples);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    private void SnapshotButton_Click(object? sender, EventArgs e)
    {
        DateTime requestedAt = DateTime.Now;
        int debounceMilliseconds =
            Math.Clamp(_settings.SnapshotDebounceMilliseconds, 0, 10000);
        if (debounceMilliseconds > 0 &&
            (requestedAt - _lastSnapshotAt).TotalMilliseconds <
            debounceMilliseconds)
        {
            return;
        }

        using Bitmap? snapshot = GetCurrentSnapshot();
        if (snapshot is null)
        {
            return;
        }

        try
        {
            DateTime capturedAt = GetSnapshotCapturedAt(requestedAt);
            using Bitmap processed = SnapshotProcessor.Process(
                snapshot,
                GetActiveSnapshotPreset(),
                _patientIdTextBox.Text.Trim(),
                _patientNameTextBox.Text.Trim(),
                capturedAt);
            Directory.CreateDirectory(_settings.SnapshotDirectory);
            string fileName = BuildRsBaseFileName(
                _settings.SnapshotDirectory,
                capturedAt,
                ".jpg");
            string path = Path.Combine(_settings.SnapshotDirectory, fileName);

            using var encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] =
                new EncoderParameter(
                    Encoder.Quality,
                    (long)Math.Clamp(_settings.JpegQuality, 1, 100));
            ImageCodecInfo jpegEncoder = ImageCodecInfo
                .GetImageEncoders()
                .First(encoder => encoder.MimeType == "image/jpeg");
            processed.Save(path, jpegEncoder, encoderParameters);
            _ = RecordCapturedFileAsync(path, capturedAt);
            AddSnapshotThumbnail(processed, path, capturedAt);
            PlaySnapshotSound();
            _lastSnapshotAt = capturedAt;

            _statusLabel.Text = $"●  保存しました: {fileName}";
            _statusLabel.ForeColor = Theme.AccentBright;
        }
        catch (Exception exception)
        {
            ShowError("静止画を保存できませんでした。", exception);
        }
    }

    private void ConfigureThumbnailStrip()
    {
        _thumbnailStripPanel.BackColor = Theme.Window;
        _thumbnailStripPanel.Height = 84;
        _thumbnailStripPanel.Dock = DockStyle.Bottom;
        _thumbnailStripPanel.Padding = new Padding(8, 7, 8, 7);
        _thumbnailStripPanel.Margin = Padding.Empty;
        _thumbnailStripPanel.WrapContents = false;
        _thumbnailStripPanel.AutoScroll = true;

        _thumbnailEmptyLabel.ForeColor = Theme.Muted;
        _thumbnailEmptyLabel.Font = Theme.BodyFont(9F);
        _thumbnailEmptyLabel.Text =
            "静止画を保存すると、ここに直近のサムネイルを表示します";
        _thumbnailEmptyLabel.TextAlign = ContentAlignment.MiddleLeft;
        _thumbnailEmptyLabel.Width = 280;
        _thumbnailEmptyLabel.Height = 66;

        ConfigureModernButton(
            _openSnapshotFolderButton,
            "保存フォルダ",
            Theme.SurfaceRaised);
        _openSnapshotFolderButton.Width = 112;
        _openSnapshotFolderButton.Height = 54;
        _openSnapshotFolderButton.Anchor =
            AnchorStyles.Right | AnchorStyles.Bottom;

        ConfigureModernButton(
            _rsBaseImportButton,
            "RSBase取込",
            Theme.SurfaceRaised);
        _rsBaseImportButton.Width = 104;
        _rsBaseImportButton.Height = 54;
        _rsBaseImportButton.Visible = false;
        _rsBaseImportButton.Anchor =
            AnchorStyles.Right | AnchorStyles.Bottom;

        EnsureThumbnailFixedControls();
        UpdateRsBaseImportButtonVisibility();
        LayoutPreviewBox();
    }

    private void EnsureThumbnailFixedControls()
    {
        if (!_thumbnailStripPanel.Controls.Contains(_thumbnailEmptyLabel))
        {
            _thumbnailStripPanel.Controls.Add(_thumbnailEmptyLabel);
        }

        if (_openSnapshotFolderButton.Parent != previewSurfacePanel)
        {
            _openSnapshotFolderButton.Parent?.Controls.Remove(
                _openSnapshotFolderButton);
            previewSurfacePanel.Controls.Add(_openSnapshotFolderButton);
        }

        if (_rsBaseImportButton.Parent != previewSurfacePanel)
        {
            _rsBaseImportButton.Parent?.Controls.Remove(_rsBaseImportButton);
            previewSurfacePanel.Controls.Add(_rsBaseImportButton);
        }

        _openSnapshotFolderButton.BringToFront();
        _rsBaseImportButton.BringToFront();
        PositionThumbnailActionButtons();
    }

    private void PositionThumbnailActionButtons()
    {
        if (_openSnapshotFolderButton.Parent != previewSurfacePanel)
        {
            return;
        }

        int y = Math.Max(
            8,
            previewSurfacePanel.ClientSize.Height -
            _thumbnailStripPanel.Height +
            (_thumbnailStripPanel.Height - _openSnapshotFolderButton.Height) / 2);
        int right = previewSurfacePanel.ClientSize.Width - 14;

        if (_rsBaseImportButton.Parent == previewSurfacePanel &&
            _rsBaseImportButton.Visible)
        {
            int rsBaseX = Math.Max(8, right - _rsBaseImportButton.Width);
            _rsBaseImportButton.Location = new Point(rsBaseX, y);
            right = rsBaseX - 8;
        }

        int folderX = Math.Max(
            8,
            right - _openSnapshotFolderButton.Width);
        _openSnapshotFolderButton.Location = new Point(folderX, y);
    }

    private void UpdateRsBaseImportButtonVisibility()
    {
        _rsBaseImportButton.Visible =
            !string.IsNullOrWhiteSpace(_settings.RsBaseReloadUrl);
        EnsureThumbnailFixedControls();
        PositionThumbnailActionButtons();
    }

    private void OpenSnapshotFolderButton_Click(object? sender, EventArgs e)
    {
        string directory = _settings.SnapshotDirectory;
        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = ApplicationSettings.CreateDefault().SnapshotDirectory;
        }

        try
        {
            Directory.CreateDirectory(directory);
            Process.Start(
                new ProcessStartInfo(directory)
                {
                    UseShellExecute = true
                });
        }
        catch (Exception exception)
        {
            ShowError("保存フォルダを開けませんでした。", exception);
        }
    }

    private async void RsBaseImportButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_settings.RsBaseReloadUrl))
        {
            UpdateRsBaseImportButtonVisibility();
            return;
        }

        _rsBaseImportButton.Enabled = false;
        _statusLabel.Text = "● RSBase取込を通知中...";
        _statusLabel.ForeColor = Theme.AccentBright;
        try
        {
            await _rsBaseFilingService.NotifyRsBaseAsync(_settings);
            _statusLabel.Text = "● RSBase取込を通知しました";
            _statusLabel.ForeColor = Theme.AccentBright;
        }
        catch (Exception exception)
        {
            ShowError("RSBase取込通知に失敗しました。", exception);
        }
        finally
        {
            _rsBaseImportButton.Enabled = true;
        }
    }

    private void ClearPreviewAndThumbnails()
    {
        ClearPreviewImage();

        Bitmap? previousPlaybackSource = _playbackSourceImage;
        _playbackSourceImage = null;
        previousPlaybackSource?.Dispose();

        foreach (Panel item in _thumbnailStripPanel.Controls
                     .OfType<Panel>()
                     .ToArray())
        {
            _thumbnailStripPanel.Controls.Remove(item);
            DisposeThumbnailItem(item);
        }

        _thumbnailEmptyLabel.Visible = true;
        EnsureThumbnailFixedControls();
    }

    private void ClearPreviewImage()
    {
        _previewBox.Image = null;
        Bitmap? previous = _displayedImage;
        _displayedImage = null;
        previous?.Dispose();
        if (_previewZoomForm is { IsDisposed: false, Visible: true })
        {
            _previewZoomForm.ClearImage();
        }
    }

    private void AddSnapshotThumbnail(
        Bitmap source,
        string path,
        DateTime capturedAt)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AddSnapshotThumbnail(source, path, capturedAt));
            return;
        }

        _thumbnailEmptyLabel.Visible = false;

        Panel item = CreateSnapshotThumbnailItem(source, path, capturedAt);
        _thumbnailStripPanel.Controls.Add(item);
        _thumbnailStripPanel.Controls.SetChildIndex(item, 0);
        EnsureThumbnailFixedControls();

        while (_thumbnailStripPanel.Controls
                   .OfType<Panel>()
                   .Count() > MaxSnapshotThumbnails)
        {
            Panel oldest = _thumbnailStripPanel.Controls
                .OfType<Panel>()
                .Last();
            _thumbnailStripPanel.Controls.Remove(oldest);
            DisposeThumbnailItem(oldest);
        }
        EnsureThumbnailFixedControls();
    }

    private Panel CreateSnapshotThumbnailItem(
        Bitmap source,
        string path,
        DateTime capturedAt)
    {
        Bitmap thumbnail = CreateThumbnailBitmap(source, 96, 54);
        var item = new Panel
        {
            Width = 106,
            Height = 68,
            BackColor = Theme.Surface,
            Margin = new Padding(0, 0, 8, 0),
            Padding = new Padding(4),
            Tag = path,
            Cursor = Cursors.Hand
        };

        var pictureBox = new PictureBox
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = Color.Black,
            Image = thumbnail,
            SizeMode = PictureBoxSizeMode.Zoom,
            Tag = path,
            Cursor = Cursors.Hand
        };

        var timeLabel = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 14,
            ForeColor = Theme.Muted,
            Font = Theme.BodyFont(8F),
            Text = capturedAt.ToString("HH:mm:ss"),
            TextAlign = ContentAlignment.MiddleCenter,
            Tag = path,
            Cursor = Cursors.Hand
        };

        item.Controls.Add(pictureBox);
        item.Controls.Add(timeLabel);

        string tip = Path.GetFileName(path);
        _thumbnailToolTip.SetToolTip(item, tip);
        _thumbnailToolTip.SetToolTip(pictureBox, tip);
        _thumbnailToolTip.SetToolTip(timeLabel, tip);

        item.Click += SnapshotThumbnail_Click;
        pictureBox.Click += SnapshotThumbnail_Click;
        timeLabel.Click += SnapshotThumbnail_Click;

        return item;
    }

    private static Bitmap CreateThumbnailBitmap(
        Bitmap source,
        int width,
        int height)
    {
        var thumbnail = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        using Graphics graphics = Graphics.FromImage(thumbnail);
        graphics.Clear(Color.Black);
        graphics.InterpolationMode =
            System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode =
            System.Drawing.Drawing2D.SmoothingMode.HighQuality;
        graphics.PixelOffsetMode =
            System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

        double scale = Math.Min(
            width / (double)source.Width,
            height / (double)source.Height);
        int drawWidth = Math.Max(1, (int)Math.Round(source.Width * scale));
        int drawHeight = Math.Max(1, (int)Math.Round(source.Height * scale));
        int x = (width - drawWidth) / 2;
        int y = (height - drawHeight) / 2;
        graphics.DrawImage(
            source,
            new Rectangle(x, y, drawWidth, drawHeight));
        return thumbnail;
    }

    private void SnapshotThumbnail_Click(object? sender, EventArgs e)
    {
        if (sender is not Control { Tag: string path } ||
            !File.Exists(path))
        {
            return;
        }

        try
        {
            Process.Start(
                new ProcessStartInfo(path)
                {
                    UseShellExecute = true
                });
        }
        catch (Exception exception)
        {
            ShowError("静止画を開けませんでした。", exception);
        }
    }

    private static void DisposeThumbnailItem(Control item)
    {
        foreach (Control child in item.Controls)
        {
            if (child is PictureBox pictureBox &&
                pictureBox.Image is { } image)
            {
                pictureBox.Image = null;
                image.Dispose();
            }
        }

        item.Dispose();
    }

    private Bitmap? GetCurrentSnapshot()
    {
        if (_playbackService.IsOpen)
        {
            return _displayedImage is null
                ? null
                : (Bitmap)_displayedImage.Clone();
        }

        return _cameraService.GetSnapshot();
    }

    private DateTime GetSnapshotCapturedAt(DateTime fallback)
    {
        if (!_playbackService.IsOpen || _playbackCapturedAt is not { } capturedAt)
        {
            return fallback;
        }

        return _lastPlaybackPosition is null
            ? capturedAt
            : capturedAt + _lastPlaybackPosition.CurrentTime;
    }

    private void PreviewBox_MouseDoubleClick(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _displayedImage is null)
        {
            return;
        }

        if (_previewZoomForm is null || _previewZoomForm.IsDisposed)
        {
            _previewZoomForm = new PreviewZoomForm();
            RestoreZoomWindowBounds(_previewZoomForm);
            _previewZoomForm.TopMost = ShouldUsePreviewTopMost();
            _previewZoomForm.PlaybackSeekRequested +=
                PreviewZoomForm_PlaybackSeekRequested;
            _previewZoomForm.PlaybackToggleRequested +=
                PreviewZoomForm_PlaybackToggleRequested;
            _previewZoomForm.FormClosed += async (_, _) =>
            {
                if (_previewZoomForm is not null)
                {
                    SaveZoomWindowBounds(_previewZoomForm);
                    if (!_isClosing)
                    {
                        await _settingsStore.SaveAsync(_settings);
                    }
                }

                _previewZoomForm = null;
            };
        }

        using Bitmap clone = (Bitmap)_displayedImage.Clone();
        double initialZoom = CalculateInitialZoom(clone.Size);
        _previewZoomForm.SetImage(clone, initialZoom);
        RestoreZoomWindowBounds(_previewZoomForm);
        if (_lastPlaybackPosition is not null)
        {
            _previewZoomForm.SetPlaybackPosition(
                _lastPlaybackPosition.CurrentTime,
                _lastPlaybackPosition.TotalTime);
            UpdatePlaybackControlCaptions();
        }
        else
        {
            _previewZoomForm.SetPlaybackPosition(
                TimeSpan.Zero,
                TimeSpan.Zero);
            _previewZoomForm.SetPlaybackState(false, true);
        }

        if (!_previewZoomForm.Visible)
        {
            _previewZoomForm.Show(this);
        }

        _previewZoomForm.Activate();
    }

    private void PreviewZoomForm_PlaybackSeekRequested(TimeSpan time)
    {
        if (!_playbackService.IsOpen)
        {
            return;
        }

        _playbackService.Seek(time);
        UpdatePlaybackControlCaptions();
    }

    private CapturePreset? GetActiveSnapshotPreset()
    {
        return _playbackService.IsOpen && _playbackCapturePreset is not null
            ? _playbackCapturePreset
            : _selectedPreset;
    }

    private void PreviewZoomForm_PlaybackToggleRequested()
    {
        TogglePlaybackPause();
    }

    private double CalculateInitialZoom(Size imageSize)
    {
        if (imageSize.Width <= 0 || imageSize.Height <= 0)
        {
            return 1.0;
        }

        Rectangle area = Screen.FromControl(this).WorkingArea;
        double scale = Math.Min(
            area.Width * 0.9 / imageSize.Width,
            area.Height * 0.85 / imageSize.Height);
        return Math.Clamp(Math.Min(1.0, scale), 0.1, 2.0);
    }

    private void UpdatePreviewZoomForm(Bitmap bitmap)
    {
        if (_previewZoomForm is null ||
            _previewZoomForm.IsDisposed ||
            !_previewZoomForm.Visible)
        {
            return;
        }

        _previewZoomForm.SetImage(bitmap);
        if (_lastPlaybackPosition is not null)
        {
            _previewZoomForm.SetPlaybackPosition(
                _lastPlaybackPosition.CurrentTime,
                _lastPlaybackPosition.TotalTime);
            UpdatePlaybackControlCaptions();
        }
        else
        {
            _previewZoomForm.SetPlaybackPosition(
                TimeSpan.Zero,
                TimeSpan.Zero);
            _previewZoomForm.SetPlaybackState(false, true);
        }
    }

    private static void PlaySnapshotSound()
    {
        try
        {
            SystemSounds.Asterisk.Play();
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static Bitmap ApplyDisplayFilter(
        Bitmap source,
        ImageFilterState state)
    {
        if (state.IsDefault)
        {
            return source;
        }

        var filtered = new Bitmap(
            source.Width,
            source.Height,
            PixelFormat.Format24bppRgb);
        using (Graphics graphics = Graphics.FromImage(filtered))
        {
            graphics.DrawImageUnscaled(source, 0, 0);
        }

        if (state.FlipHorizontal || state.FlipVertical)
        {
            filtered.RotateFlip(
                state.FlipHorizontal && state.FlipVertical
                    ? RotateFlipType.RotateNoneFlipXY
                    : state.FlipHorizontal
                        ? RotateFlipType.RotateNoneFlipX
                        : RotateFlipType.RotateNoneFlipY);
        }

        byte[] redLut = BuildCorrectionLookup(state.Red, state.Gamma);
        byte[] greenLut = BuildCorrectionLookup(state.Green, state.Gamma);
        byte[] blueLut = BuildCorrectionLookup(state.Blue, state.Gamma);
        Rectangle bounds = new(0, 0, filtered.Width, filtered.Height);
        BitmapData data = filtered.LockBits(
            bounds,
            ImageLockMode.ReadWrite,
            PixelFormat.Format24bppRgb);
        try
        {
            int byteCount = Math.Abs(data.Stride) * filtered.Height;
            byte[] pixels = new byte[byteCount];
            Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
            for (int y = 0; y < filtered.Height; y++)
            {
                int rowOffset = y * Math.Abs(data.Stride);
                for (int x = 0; x < filtered.Width; x++)
                {
                    int offset = rowOffset + x * 3;
                    pixels[offset] = blueLut[pixels[offset]];
                    pixels[offset + 1] = greenLut[pixels[offset + 1]];
                    pixels[offset + 2] = redLut[pixels[offset + 2]];
                }
            }

            Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
        }
        finally
        {
            filtered.UnlockBits(data);
        }

        return filtered;
    }

    private static byte[] BuildCorrectionLookup(int balance, double gamma)
    {
        var lookup = new byte[256];
        double multiplier = Math.Clamp(balance, 0, 510) / 255.0;
        double safeGamma = Math.Clamp(gamma, 0.1, 5.0);
        double exponent = 1.0 / safeGamma;
        for (int i = 0; i < lookup.Length; i++)
        {
            double balanced = Math.Clamp(i * multiplier, 0, 255);
            lookup[i] = (byte)Math.Clamp(
                Math.Round(Math.Pow(balanced / 255.0, exponent) * 255.0),
                0,
                255);
        }

        return lookup;
    }

    private void ProcessingControlChanged()
    {
        if (_isApplyingPreset)
        {
            return;
        }

        ImageFilterState state = ReadFilterStateFromControls();
        UpdateFilterValueLabels(state);
        if (_isPlaybackFilterMode)
        {
            _playbackFilterState = state;
            RedrawCurrentPlaybackFrame();
            return;
        }

        _captureFilterState = state;
        _cameraService.UpdateProcessingOptions(
            state.Red,
            state.Green,
            state.Blue,
            state.Gamma,
            state.FlipHorizontal,
            state.FlipVertical);
    }

    private void RedrawCurrentPlaybackFrame()
    {
        if (_playbackSourceImage is null)
        {
            return;
        }

        Bitmap displayBitmap = ApplyDisplayFilter(
            (Bitmap)_playbackSourceImage.Clone(),
            _playbackFilterState);
        Bitmap? previous = _displayedImage;
        _displayedImage = displayBitmap;
        _previewBox.Image = displayBitmap;
        UpdatePreviewZoomForm(displayBitmap);
        previous?.Dispose();
    }

    private ImageFilterState ReadFilterStateFromControls()
    {
        return new ImageFilterState(
            _redTrack.Value,
            _greenTrack.Value,
            _blueTrack.Value,
            _gammaTrack.Value / 10.0,
            _flipHorizontalCheckBox.Checked,
            _flipVerticalCheckBox.Checked);
    }

    private void ApplyFilterStateToControls(ImageFilterState state)
    {
        bool wasApplying = _isApplyingPreset;
        _isApplyingPreset = true;
        try
        {
            SetTrackBarValue(_redTrack, state.Red);
            SetTrackBarValue(_greenTrack, state.Green);
            SetTrackBarValue(_blueTrack, state.Blue);
            SetTrackBarValue(
                _gammaTrack,
                (int)Math.Round(state.Gamma * 10));
            _flipHorizontalCheckBox.Checked = state.FlipHorizontal;
            _flipVerticalCheckBox.Checked = state.FlipVertical;
            UpdateFilterValueLabels(state);
        }
        finally
        {
            _isApplyingPreset = wasApplying;
        }
    }

    private void UpdateFilterValueLabels(ImageFilterState state)
    {
        _redValue.Text = state.Red.ToString();
        _greenValue.Text = state.Green.ToString();
        _blueValue.Text = state.Blue.ToString();
        _gammaValue.Text = state.Gamma.ToString("0.0");
    }

    private void EnterPlaybackFilterMode()
    {
        if (_isPlaybackFilterMode)
        {
            return;
        }

        _captureFilterState = ReadFilterStateFromControls();
        _playbackFilterState = ImageFilterState.Default;
        _isPlaybackFilterMode = true;
        ApplyFilterStateToControls(_playbackFilterState);
    }

    private void LeavePlaybackFilterMode()
    {
        if (!_isPlaybackFilterMode)
        {
            return;
        }

        _playbackFilterState = ReadFilterStateFromControls();
        _isPlaybackFilterMode = false;
        ApplyFilterStateToControls(_captureFilterState);
        _cameraService.UpdateProcessingOptions(
            _captureFilterState.Red,
            _captureFilterState.Green,
            _captureFilterState.Blue,
            _captureFilterState.Gamma,
            _captureFilterState.FlipHorizontal,
            _captureFilterState.FlipVertical);
    }

    private void ResetFilters()
    {
        _redTrack.Value = 255;
        _greenTrack.Value = 255;
        _blueTrack.Value = 255;
        _gammaTrack.Value = 10;
        _flipHorizontalCheckBox.Checked = false;
        _flipVerticalCheckBox.Checked = false;
    }

    private async void SavePresetButton_Click(object? sender, EventArgs e)
    {
        await SaveSelectedPresetAsync();
    }

    private async Task SaveSelectedPresetAsync()
    {
        if (_selectedPreset is null)
        {
            return;
        }

        if (_deviceComboBox.SelectedItem is CameraDeviceInfo device)
        {
            _selectedPreset.DeviceIndex = device.Index;
            _selectedPreset.DeviceId = device.DeviceId;
            _selectedPreset.DeviceName = device.Name;
        }

        CaptureResolution resolution =
            _resolutionComboBox.SelectedItem as CaptureResolution
            ?? CaptureResolution.Default;
        _selectedPreset.Resolution = resolution;
        _selectedPreset.FramesPerSecond = resolution.FramesPerSecond;
        _selectedPreset.WhiteBalanceRed = _redTrack.Value;
        _selectedPreset.WhiteBalanceGreen = _greenTrack.Value;
        _selectedPreset.WhiteBalanceBlue = _blueTrack.Value;
        _selectedPreset.Gamma = _gammaTrack.Value / 10.0;
        _selectedPreset.ExaminationType =
            _examinationTypeComboBox.Text.Trim();
        _selectedPreset.FlipHorizontal =
            _flipHorizontalCheckBox.Checked;
        _selectedPreset.FlipVertical =
            _flipVerticalCheckBox.Checked;

        _savePresetButton.Enabled = false;
        try
        {
            await _settingsStore.SaveAsync(_settings);
            _statusLabel.Text =
                $"●  {_selectedPreset.Name} を更新しました";
            _statusLabel.ForeColor = Theme.AccentBright;
        }
        catch (Exception exception)
        {
            ShowError("プリセット設定を保存できませんでした。", exception);
        }
        finally
        {
            _savePresetButton.Enabled = true;
        }
    }

    private void RefreshPresetControls()
    {
        _isUpdatingPresetControls = true;
        try
        {
            _morePresetsMenu.Items.Clear();

            for (int index = 0; index < QuickPresetButtons.Count; index++)
            {
                ModernRadioButton button = QuickPresetButtons[index];
                if (index < _settings.Presets.Count)
                {
                    CapturePreset preset = _settings.Presets[index];
                    button.Text = preset.Name;
                    button.Tag = preset;
                    button.Visible = true;
                }
                else
                {
                    button.Tag = null;
                    button.Visible = false;
                }
            }

            foreach (CapturePreset preset in _settings.Presets.Skip(QuickPresetCount))
            {
                bool selected = _selectedPreset?.Id == preset.Id;
                var item = new ToolStripMenuItem(preset.Name)
                {
                    Tag = preset,
                    BackColor = selected
                        ? Theme.Selected
                        : Theme.SurfaceRaised,
                    ForeColor = Theme.Text
                };
                item.Click += MorePresetMenuItem_Click;
                _morePresetsMenu.Items.Add(item);
            }

            _morePresetsDropDownButton.Visible =
                _settings.Presets.Count > QuickPresetCount;
            bool selectedMorePreset =
                _selectedPreset is not null &&
                _settings.Presets
                    .Skip(QuickPresetCount)
                    .Any(item => item.Id == _selectedPreset.Id);
            _morePresetsDropDownButton.Text = selectedMorePreset
                ? $"{_selectedPreset!.Name} ▾"
                : "その他 ▾";
            SetMorePresetsDropDownSelected(selectedMorePreset);
        }
        finally
        {
            _isUpdatingPresetControls = false;
            UpdateInteractionGuards();
        }
    }

    private void QuickPresetButton_CheckedChanged(object? sender, EventArgs e)
    {
        if (!_isUpdatingPresetControls &&
            sender is RadioButton { Checked: true, Tag: CapturePreset preset })
        {
            SelectPreset(preset);
        }
    }

    private void MorePresetsDropDownButton_Click(object? sender, EventArgs e)
    {
        if (_morePresetsMenu.Items.Count == 0)
        {
            return;
        }

        _morePresetsMenu.Show(
            _morePresetsDropDownButton,
            new Point(0, _morePresetsDropDownButton.Height));
    }

    private void MorePresetMenuItem_Click(object? sender, EventArgs e)
    {
        if (!_isUpdatingPresetControls &&
            sender is ToolStripMenuItem { Tag: CapturePreset preset })
        {
            SelectPreset(preset);
        }
    }

    private void SelectPreset(CapturePreset preset)
    {
        _selectedPreset = preset;
        if (_playbackService.IsOpen || _isPlaybackFilterMode)
        {
            _playbackCapturePreset = preset;
        }

        _settings.SelectedPresetId = preset.Id;
        _presetLabel.Text = $"プリセット: {preset.Name}";

        _isUpdatingPresetControls = true;
        _isApplyingPreset = true;
        try
        {
            foreach (RadioButton button in _quickPresetPanel.Controls.OfType<RadioButton>())
            {
                button.Checked =
                    button.Tag is CapturePreset item &&
                    item.Id == preset.Id;
            }

            bool selectedMorePreset =
                _settings.Presets
                    .Skip(QuickPresetCount)
                    .Any(item => item.Id == preset.Id);
            _morePresetsDropDownButton.Text = selectedMorePreset
                ? $"{preset.Name} ▾"
                : "その他 ▾";
            SetMorePresetsDropDownSelected(selectedMorePreset);
            _examinationTypeComboBox.Text =
                preset.ExaminationType;
            _fileVideoCheckBox.Checked = preset.IsVideo;
            ApplyHeaderOptionRules(null);

            CameraDeviceInfo? device = _deviceComboBox.Items
                .Cast<CameraDeviceInfo>()
                .FirstOrDefault(
                    item =>
                        !string.IsNullOrWhiteSpace(preset.DeviceId) &&
                        string.Equals(
                            item.MonikerString,
                            preset.DeviceId,
                            StringComparison.OrdinalIgnoreCase))
                ?? _deviceComboBox.Items
                    .Cast<CameraDeviceInfo>()
                    .FirstOrDefault(item => item.Index == preset.DeviceIndex);
            if (device is not null)
            {
                _deviceComboBox.SelectedItem = device;
            }

            int resolutionIndex = _resolutionComboBox.Items.IndexOf(preset.Resolution);
            _resolutionComboBox.SelectedIndex =
                resolutionIndex >= 0 ? resolutionIndex : 0;

            SetTrackBarValue(_redTrack, preset.WhiteBalanceRed);
            SetTrackBarValue(_greenTrack, preset.WhiteBalanceGreen);
            SetTrackBarValue(_blueTrack, preset.WhiteBalanceBlue);
            SetTrackBarValue(
                _gammaTrack,
                (int)Math.Round(preset.Gamma * 10));
            _flipHorizontalCheckBox.Checked =
                preset.FlipHorizontal;
            _flipVerticalCheckBox.Checked =
                preset.FlipVertical;
        }
        finally
        {
            _isApplyingPreset = false;
            _isUpdatingPresetControls = false;
            ProcessingControlChanged();
            UpdateInteractionGuards();
        }
    }

    private async void ManagePresetsButton_Click(object? sender, EventArgs e)
    {
        CameraDeviceInfo[] devices = _deviceComboBox.Items
            .Cast<CameraDeviceInfo>()
            .ToArray();
        _hotkeyService.Dispose();
        try
        {
            using var dialog = new SettingsForm(_settings, devices);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            _settings = dialog.Settings;
            await _settingsStore.SaveAsync(_settings);
            RefreshExaminationTypes();
            RefreshPresetControls();
            CapturePreset? selectedPreset =
                _settings.Presets.FirstOrDefault(
                    preset => preset.Id == _settings.SelectedPresetId)
                ?? _settings.Presets.FirstOrDefault();

            if (selectedPreset is not null)
            {
                SelectPreset(selectedPreset);
            }
            else
            {
                _selectedPreset = null;
                _fileVideoCheckBox.Checked = false;
            }

            UpdateHeaderOptions();
            UpdateCaptureModePresentation();
            UpdateRsBaseImportButtonVisibility();
            UpdatePreviewTopMostState();
            UpdateInteractionGuards();
            _theptWatcher.Start(_settings.RsBaseTheptDirectory);
        }
        finally
        {
            if (!_isClosing)
            {
                _hotkeyService.Start(
                    _settings.SnapshotHotkey,
                    _settings.CaptureHotkey);
            }
        }
    }

    private async void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_isClosing)
        {
            return;
        }

        e.Cancel = true;
        _isClosing = true;
        Enabled = false;

        try
        {
            SaveMainWindowBounds();
            if (_previewZoomForm is { IsDisposed: false })
            {
                SaveZoomWindowBounds(_previewZoomForm);
            }

            await _cameraService.DisposeAsync();
            await _playbackService.DisposeAsync();
            _hotkeyService.Dispose();
            _theptWatcher.Dispose();
            await _settingsStore.SaveAsync(_settings);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                $"終了処理中にエラーが発生しました。\r\n{exception.Message}",
                "ENTcapture2",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        finally
        {
            ClearPreviewAndThumbnails();
            _thumbnailToolTip.Dispose();
            e.Cancel = false;
            FormClosing -= MainForm_FormClosing;
            Close();
        }
    }

    private static ComboBox CreateComboBox()
    {
        return new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Theme.SurfaceRaised,
            ForeColor = Theme.Text,
            FlatStyle = FlatStyle.Flat,
            Font = Theme.BodyFont(),
            Height = 34,
            Margin = new Padding(0, 2, 8, 2)
        };
    }

    private static TextBox CreateTextBox()
    {
        return new TextBox
        {
            BackColor = Theme.SurfaceRaised,
            ForeColor = Theme.Text,
            BorderStyle = BorderStyle.FixedSingle,
            Font = Theme.BodyFont(),
            Margin = new Padding(4, 1, 10, 0)
        };
    }

    private static TrackBar CreateColorTrack()
    {
        return new TrackBar
        {
            Minimum = 0,
            Maximum = 510,
            Value = 255,
            TickStyle = TickStyle.None,
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface
        };
    }

    private static Label CreateValueLabel()
    {
        return new Label
        {
            AutoSize = false,
            Width = 42,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Theme.Text,
            Font = Theme.BodyFont(9)
        };
    }

    private static Label CreateCompactLabel(string text)
    {
        return new Label
        {
            AutoSize = true,
            Text = text,
            ForeColor = Theme.Muted,
            Margin = new Padding(0, 6, 2, 0)
        };
    }

    private static TableLayoutPanel CreateCard(string title)
    {
        var card = new TableLayoutPanel
        {
            Width = 294,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.Surface,
            Padding = new Padding(16),
            Margin = new Padding(0, 0, 0, 12)
        };
        card.Controls.Add(new Label
        {
            AutoSize = true,
            Text = title,
            ForeColor = Theme.AccentBright,
            Font = Theme.BodyFont(9),
            Margin = new Padding(0, 0, 0, 12)
        });

        card.Controls.Add(new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 0,
            Margin = Padding.Empty
        });
        return card;
    }

    private static void AddCardRow(
        TableLayoutPanel content,
        string labelText,
        Control control)
    {
        content.Controls.Add(new Label
        {
            AutoSize = true,
            Text = labelText,
            ForeColor = Theme.Muted,
            Margin = new Padding(0, 4, 0, 4)
        });
        control.Margin = new Padding(0, 0, 0, 8);
        content.Controls.Add(control);
    }

    private static void AddSliderRow(
        TableLayoutPanel content,
        string labelText,
        TrackBar trackBar,
        Label valueLabel)
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 3,
            Margin = new Padding(0, 0, 0, 4)
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));
        row.Controls.Add(new Label
        {
            Text = labelText,
            ForeColor = Theme.Muted,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        });
        row.Controls.Add(trackBar);
        row.Controls.Add(valueLabel);
        content.Controls.Add(row);
    }

    private static void ConfigureColorTrack(
        TrackBar trackBar,
        Label valueLabel,
        string channel)
    {
        valueLabel.Text = trackBar.Value.ToString();
        valueLabel.AccessibleName = channel;
    }

    private static void ConfigureButton(
        ModernButton button,
        string text,
        Color fillColor)
    {
        button.Text = text;
        button.FillColor = fillColor;
        button.BackColor = fillColor;
        button.HoverColor =
            fillColor == Theme.Accent
                ? Theme.AccentHover
                : Theme.ButtonHover;
        button.Margin = new Padding(0, 0, 8, 0);
    }

    private static string SanitizeFileName(string value)
    {
        string result = value.Trim();
        foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
        {
            result = result.Replace(invalidCharacter, '_');
        }

        return result;
    }

    private static string GetApplicationVersionText()
    {
        Assembly assembly = typeof(MainForm).Assembly;
        string? informationalVersion =
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            return informationalVersion;
        }

        return assembly.GetName().Version?.ToString(4) ?? "0.0.0";
    }

    private void SaveMainWindowBounds()
    {
        Rectangle bounds =
            WindowState == FormWindowState.Normal
                ? Bounds
                : RestoreBounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        _settings.MainWindowLeft = bounds.Left;
        _settings.MainWindowTop = bounds.Top;
        _settings.MainWindowWidth = bounds.Width;
        _settings.MainWindowHeight = bounds.Height;
    }

    private void RestoreZoomWindowBounds(Form zoomForm)
    {
        if (_settings.ZoomWindowWidth <= 0 ||
            _settings.ZoomWindowHeight <= 0 ||
            _settings.ZoomWindowLeft == int.MinValue ||
            _settings.ZoomWindowTop == int.MinValue)
        {
            return;
        }

        var bounds = new Rectangle(
            _settings.ZoomWindowLeft,
            _settings.ZoomWindowTop,
            _settings.ZoomWindowWidth,
            _settings.ZoomWindowHeight);
        if (!Screen.AllScreens.Any(screen =>
                screen.WorkingArea.IntersectsWith(bounds)))
        {
            return;
        }

        zoomForm.StartPosition = FormStartPosition.Manual;
        zoomForm.Bounds = bounds;
    }

    private void SaveZoomWindowBounds(Form zoomForm)
    {
        Rectangle bounds =
            zoomForm.WindowState == FormWindowState.Normal
                ? zoomForm.Bounds
                : zoomForm.RestoreBounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        _settings.ZoomWindowLeft = bounds.Left;
        _settings.ZoomWindowTop = bounds.Top;
        _settings.ZoomWindowWidth = bounds.Width;
        _settings.ZoomWindowHeight = bounds.Height;
    }

    private static void SetTrackBarValue(TrackBar trackBar, int value)
    {
        trackBar.Value = Math.Clamp(value, trackBar.Minimum, trackBar.Maximum);
    }

    private void ShowError(string message, Exception exception)
    {
        _statusLabel.Text = "●  エラー";
        _statusLabel.ForeColor = Theme.Danger;
        MessageBox.Show(
            this,
            $"{message}\r\n{exception.Message}",
            "ENTcapture2",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private sealed record ImageFilterState(
        int Red,
        int Green,
        int Blue,
        double Gamma,
        bool FlipHorizontal,
        bool FlipVertical)
    {
        public static ImageFilterState Default { get; } =
            new(255, 255, 255, 1.0, false, false);

        public bool IsDefault =>
            Red == 255 &&
            Green == 255 &&
            Blue == 255 &&
            Math.Abs(Gamma - 1.0) < 0.001 &&
            !FlipHorizontal &&
            !FlipVertical;
    }
}
