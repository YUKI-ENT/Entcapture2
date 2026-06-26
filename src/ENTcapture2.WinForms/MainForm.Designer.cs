#nullable enable
#pragma warning disable CS8600

using ENTcapture2.WinForms.Ui;

namespace ENTcapture2.WinForms;

public partial class MainForm
{
    private TableLayoutPanel rootLayout = null!;
    private TableLayoutPanel headerLayout = null!;
    private FlowLayoutPanel titlePanel = null!;
    private Label titleLabel = null!;
    private Label versionLabel = null!;
    private Label subtitleLabel = null!;
    private FlowLayoutPanel patientPanel = null!;
    private Label patientIdLabel = null!;
    private TextBox _patientIdTextBox = null!;
    private Label patientNameLabel = null!;
    private TextBox _patientNameTextBox = null!;
    private Label examinationTypeLabel = null!;
    private ComboBox _examinationTypeComboBox = null!;
    private CheckBox _previewOnlyCheckBox = null!;
    private CheckBox _fileVideoCheckBox = null!;
    private TableLayoutPanel primaryActionPanel = null!;
    private ModernButton _openPlaybackButton = null!;
    private FlowLayoutPanel _quickPresetPanel = null!;
    private ModernRadioButton _presetButton1 = null!;
    private ModernRadioButton _presetButton2 = null!;
    private ModernRadioButton _presetButton3 = null!;
    private ModernRadioButton _presetButton4 = null!;
    private ModernRadioButton _presetButton5 = null!;
    private FlowLayoutPanel presetActionPanel = null!;
    private Label otherPresetsLabel = null!;
    private ModernButton _morePresetsDropDownButton = null!;
    private ModernButton _managePresetsButton = null!;
    private TableLayoutPanel workspaceLayout = null!;
    private Panel previewSurfacePanel = null!;
    private PictureBox _previewBox = null!;
    private Label _previewMessage = null!;
    private Label liveBadgeLabel = null!;
    private FlowLayoutPanel _thumbnailStripPanel = null!;
    private Label _thumbnailEmptyLabel = null!;
    private TableLayoutPanel playbackPanel = null!;
    private ModernButton _playPauseButton = null!;
    private TrackBar _playbackTrackBar = null!;
    private Label _playbackPositionLabel = null!;
    private ModernButton _openClipEditorButton = null!;
    private ModernButton _closePlaybackButton = null!;
    private FlowLayoutPanel _clipEditorPanel = null!;
    private ModernButton _markClipStartButton = null!;
    private ModernButton _markClipEndButton = null!;
    private ModernButton _addClipButton = null!;
    private ModernButton _clearClipsButton = null!;
    private ModernButton _exportJoinedClipsButton = null!;
    private ModernButton _exportSeparateClipsButton = null!;
    private Label _clipRangeLabel = null!;
    private ListBox _clipListBox = null!;
    private FlowLayoutPanel sidePanel = null!;
    private TableLayoutPanel sourceCard = null!;
    private Label sourceTitleLabel = null!;
    private Label cameraLabel = null!;
    private ComboBox _deviceComboBox = null!;
    private Label resolutionLabel = null!;
    private ComboBox _resolutionComboBox = null!;
    private FlowLayoutPanel sourceButtonPanel = null!;
    private ModernButton _startButton = null!;
    private ModernButton _refreshDevicesButton = null!;
    private ModernButton _toggleImageControlButton = null!;
    private ModernButton _togglePresetToolsButton = null!;
    private TableLayoutPanel filterCard = null!;
    private Label filterTitleLabel = null!;
    private FlowLayoutPanel whiteBalancePanel = null!;
    private CheckBox _whiteBalanceSelectionCheckBox = null!;
    private Label whiteBalanceHintLabel = null!;
    private TableLayoutPanel redRow = null!;
    private Label redCaptionLabel = null!;
    private TrackBar _redTrack = null!;
    private Label _redValue = null!;
    private TableLayoutPanel greenRow = null!;
    private Label greenCaptionLabel = null!;
    private TrackBar _greenTrack = null!;
    private Label _greenValue = null!;
    private TableLayoutPanel blueRow = null!;
    private Label blueCaptionLabel = null!;
    private TrackBar _blueTrack = null!;
    private Label _blueValue = null!;
    private TableLayoutPanel gammaRow = null!;
    private Label gammaCaptionLabel = null!;
    private TrackBar _gammaTrack = null!;
    private Label _gammaValue = null!;
    private FlowLayoutPanel flipPanel = null!;
    private CheckBox _flipHorizontalCheckBox = null!;
    private CheckBox _flipVerticalCheckBox = null!;
    private ModernButton _resetFiltersButton = null!;
    private TableLayoutPanel captureCard = null!;
    private Label captureTitleLabel = null!;
    private ModernButton _snapshotButton = null!;
    private ModernButton _savePresetButton = null!;
    private TableLayoutPanel footerLayout = null!;
    private Label _statusLabel = null!;
    private Label _presetLabel = null!;
    private Label _fpsLabel = null!;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
        rootLayout = new TableLayoutPanel();
        headerLayout = new TableLayoutPanel();
        patientPanel = new FlowLayoutPanel();
        patientIdLabel = new Label();
        _patientIdTextBox = new TextBox();
        patientNameLabel = new Label();
        _patientNameTextBox = new TextBox();
        examinationTypeLabel = new Label();
        _examinationTypeComboBox = new ComboBox();
        _previewOnlyCheckBox = new CheckBox();
        _fileVideoCheckBox = new CheckBox();
        primaryActionPanel = new TableLayoutPanel();
        _startButton = new ModernButton();
        _managePresetsButton = new ModernButton();
        _snapshotButton = new ModernButton();
        _openPlaybackButton = new ModernButton();
        _quickPresetPanel = new FlowLayoutPanel();
        _presetButton1 = new ModernRadioButton();
        _presetButton2 = new ModernRadioButton();
        _presetButton3 = new ModernRadioButton();
        _presetButton4 = new ModernRadioButton();
        _presetButton5 = new ModernRadioButton();
        _morePresetsDropDownButton = new ModernButton();
        presetActionPanel = new FlowLayoutPanel();
        workspaceLayout = new TableLayoutPanel();
        previewSurfacePanel = new Panel();
        _previewBox = new PictureBox();
        _previewMessage = new Label();
        liveBadgeLabel = new Label();
        _thumbnailStripPanel = new FlowLayoutPanel();
        _thumbnailEmptyLabel = new Label();
        playbackPanel = new TableLayoutPanel();
        _playPauseButton = new ModernButton();
        _playbackTrackBar = new TrackBar();
        _playbackPositionLabel = new Label();
        _openClipEditorButton = new ModernButton();
        _closePlaybackButton = new ModernButton();
        sidePanel = new FlowLayoutPanel();
        sourceCard = new TableLayoutPanel();
        sourceTitleLabel = new Label();
        cameraLabel = new Label();
        _deviceComboBox = new ComboBox();
        resolutionLabel = new Label();
        _resolutionComboBox = new ComboBox();
        sourceButtonPanel = new FlowLayoutPanel();
        _refreshDevicesButton = new ModernButton();
        filterCard = new TableLayoutPanel();
        filterTitleLabel = new Label();
        whiteBalancePanel = new FlowLayoutPanel();
        _whiteBalanceSelectionCheckBox = new CheckBox();
        whiteBalanceHintLabel = new Label();
        redRow = new TableLayoutPanel();
        redCaptionLabel = new Label();
        _redTrack = new TrackBar();
        _redValue = new Label();
        greenRow = new TableLayoutPanel();
        greenCaptionLabel = new Label();
        _greenTrack = new TrackBar();
        _greenValue = new Label();
        blueRow = new TableLayoutPanel();
        blueCaptionLabel = new Label();
        _blueTrack = new TrackBar();
        _blueValue = new Label();
        gammaRow = new TableLayoutPanel();
        gammaCaptionLabel = new Label();
        _gammaTrack = new TrackBar();
        _gammaValue = new Label();
        flipPanel = new FlowLayoutPanel();
        _flipHorizontalCheckBox = new CheckBox();
        _flipVerticalCheckBox = new CheckBox();
        _resetFiltersButton = new ModernButton();
        captureCard = new TableLayoutPanel();
        captureTitleLabel = new Label();
        _savePresetButton = new ModernButton();
        footerLayout = new TableLayoutPanel();
        _statusLabel = new Label();
        _presetLabel = new Label();
        _fpsLabel = new Label();
        _clipEditorPanel = new FlowLayoutPanel();
        _markClipStartButton = new ModernButton();
        _markClipEndButton = new ModernButton();
        _addClipButton = new ModernButton();
        _clearClipsButton = new ModernButton();
        _exportJoinedClipsButton = new ModernButton();
        _exportSeparateClipsButton = new ModernButton();
        _clipRangeLabel = new Label();
        _clipListBox = new ListBox();
        otherPresetsLabel = new Label();
        _toggleImageControlButton = new ModernButton();
        _togglePresetToolsButton = new ModernButton();
        titlePanel = new FlowLayoutPanel();
        titleLabel = new Label();
        versionLabel = new Label();
        subtitleLabel = new Label();
        rootLayout.SuspendLayout();
        headerLayout.SuspendLayout();
        patientPanel.SuspendLayout();
        primaryActionPanel.SuspendLayout();
        _quickPresetPanel.SuspendLayout();
        workspaceLayout.SuspendLayout();
        previewSurfacePanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_previewBox).BeginInit();
        _thumbnailStripPanel.SuspendLayout();
        playbackPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_playbackTrackBar).BeginInit();
        sidePanel.SuspendLayout();
        sourceCard.SuspendLayout();
        sourceButtonPanel.SuspendLayout();
        filterCard.SuspendLayout();
        whiteBalancePanel.SuspendLayout();
        redRow.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_redTrack).BeginInit();
        greenRow.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_greenTrack).BeginInit();
        blueRow.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_blueTrack).BeginInit();
        gammaRow.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_gammaTrack).BeginInit();
        flipPanel.SuspendLayout();
        captureCard.SuspendLayout();
        footerLayout.SuspendLayout();
        _clipEditorPanel.SuspendLayout();
        titlePanel.SuspendLayout();
        SuspendLayout();
        // 
        // rootLayout
        // 
        rootLayout.BackColor = Color.FromArgb(12, 17, 26);
        rootLayout.ColumnCount = 1;
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.Controls.Add(headerLayout, 0, 0);
        rootLayout.Controls.Add(workspaceLayout, 0, 1);
        rootLayout.Controls.Add(footerLayout, 0, 2);
        rootLayout.Dock = DockStyle.Fill;
        rootLayout.Location = new Point(0, 0);
        rootLayout.Name = "rootLayout";
        rootLayout.Padding = new Padding(18);
        rootLayout.RowCount = 3;
        rootLayout.RowStyles.Add(new RowStyle());
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayout.RowStyles.Add(new RowStyle());
        rootLayout.Size = new Size(1380, 860);
        rootLayout.TabIndex = 0;
        // 
        // headerLayout
        // 
        headerLayout.AutoSize = true;
        headerLayout.ColumnCount = 2;
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        headerLayout.ColumnStyles.Add(new ColumnStyle());
        headerLayout.Controls.Add(patientPanel, 0, 0);
        headerLayout.Controls.Add(primaryActionPanel, 1, 0);
        headerLayout.Controls.Add(_quickPresetPanel, 0, 1);
        headerLayout.Controls.Add(presetActionPanel, 1, 1);
        headerLayout.Dock = DockStyle.Top;
        headerLayout.Location = new Point(18, 18);
        headerLayout.Margin = new Padding(0, 0, 0, 14);
        headerLayout.Name = "headerLayout";
        headerLayout.RowCount = 2;
        headerLayout.RowStyles.Add(new RowStyle());
        headerLayout.RowStyles.Add(new RowStyle());
        headerLayout.Size = new Size(1344, 132);
        headerLayout.TabIndex = 0;
        // 
        // patientPanel
        // 
        patientPanel.AutoSize = true;
        patientPanel.Controls.Add(patientIdLabel);
        patientPanel.Controls.Add(_patientIdTextBox);
        patientPanel.Controls.Add(patientNameLabel);
        patientPanel.Controls.Add(_patientNameTextBox);
        patientPanel.Controls.Add(examinationTypeLabel);
        patientPanel.Controls.Add(_examinationTypeComboBox);
        patientPanel.Controls.Add(_previewOnlyCheckBox);
        patientPanel.Controls.Add(_fileVideoCheckBox);
        patientPanel.Location = new Point(0, 0);
        patientPanel.Margin = new Padding(0);
        patientPanel.Name = "patientPanel";
        patientPanel.Size = new Size(811, 29);
        patientPanel.TabIndex = 1;
        patientPanel.WrapContents = false;
        // 
        // patientIdLabel
        // 
        patientIdLabel.AutoSize = true;
        patientIdLabel.ForeColor = Color.FromArgb(148, 163, 184);
        patientIdLabel.Location = new Point(0, 6);
        patientIdLabel.Margin = new Padding(0, 6, 2, 0);
        patientIdLabel.Name = "patientIdLabel";
        patientIdLabel.Size = new Size(42, 15);
        patientIdLabel.TabIndex = 0;
        patientIdLabel.Text = "患者ID";
        // 
        // _patientIdTextBox
        // 
        _patientIdTextBox.BackColor = Color.FromArgb(28, 37, 52);
        _patientIdTextBox.BorderStyle = BorderStyle.FixedSingle;
        _patientIdTextBox.ForeColor = Color.FromArgb(235, 241, 248);
        _patientIdTextBox.Location = new Point(47, 3);
        _patientIdTextBox.Name = "_patientIdTextBox";
        _patientIdTextBox.Size = new Size(100, 23);
        _patientIdTextBox.TabIndex = 1;
        // 
        // patientNameLabel
        // 
        patientNameLabel.AutoSize = true;
        patientNameLabel.ForeColor = Color.FromArgb(148, 163, 184);
        patientNameLabel.Location = new Point(150, 6);
        patientNameLabel.Margin = new Padding(0, 6, 2, 0);
        patientNameLabel.Name = "patientNameLabel";
        patientNameLabel.Size = new Size(43, 15);
        patientNameLabel.TabIndex = 2;
        patientNameLabel.Text = "患者名";
        // 
        // _patientNameTextBox
        // 
        _patientNameTextBox.BackColor = Color.FromArgb(28, 37, 52);
        _patientNameTextBox.BorderStyle = BorderStyle.FixedSingle;
        _patientNameTextBox.ForeColor = Color.FromArgb(235, 241, 248);
        _patientNameTextBox.Location = new Point(198, 3);
        _patientNameTextBox.Name = "_patientNameTextBox";
        _patientNameTextBox.Size = new Size(150, 23);
        _patientNameTextBox.TabIndex = 3;
        // 
        // examinationTypeLabel
        // 
        examinationTypeLabel.AutoSize = true;
        examinationTypeLabel.ForeColor = Color.FromArgb(148, 163, 184);
        examinationTypeLabel.Location = new Point(355, 6);
        examinationTypeLabel.Margin = new Padding(4, 6, 2, 0);
        examinationTypeLabel.Name = "examinationTypeLabel";
        examinationTypeLabel.Size = new Size(43, 15);
        examinationTypeLabel.TabIndex = 4;
        examinationTypeLabel.Text = "検査名";
        // 
        // _examinationTypeComboBox
        // 
        _examinationTypeComboBox.BackColor = Color.FromArgb(28, 37, 52);
        _examinationTypeComboBox.FlatStyle = FlatStyle.Flat;
        _examinationTypeComboBox.ForeColor = Color.FromArgb(235, 241, 248);
        _examinationTypeComboBox.FormattingEnabled = true;
        _examinationTypeComboBox.Location = new Point(403, 3);
        _examinationTypeComboBox.Name = "_examinationTypeComboBox";
        _examinationTypeComboBox.Size = new Size(190, 23);
        _examinationTypeComboBox.TabIndex = 5;
        // 
        // _previewOnlyCheckBox
        // 
        _previewOnlyCheckBox.AutoSize = true;
        _previewOnlyCheckBox.Location = new Point(603, 5);
        _previewOnlyCheckBox.Margin = new Padding(7, 5, 8, 0);
        _previewOnlyCheckBox.Name = "_previewOnlyCheckBox";
        _previewOnlyCheckBox.Size = new Size(88, 19);
        _previewOnlyCheckBox.TabIndex = 6;
        _previewOnlyCheckBox.Text = "プレビューのみ";
        // 
        // _fileVideoCheckBox
        // 
        _fileVideoCheckBox.AutoSize = true;
        _fileVideoCheckBox.Location = new Point(702, 5);
        _fileVideoCheckBox.Margin = new Padding(3, 5, 0, 0);
        _fileVideoCheckBox.Name = "_fileVideoCheckBox";
        _fileVideoCheckBox.Size = new Size(109, 19);
        _fileVideoCheckBox.TabIndex = 7;
        _fileVideoCheckBox.Text = "動画をファイリング";
        // 
        // primaryActionPanel
        // 
        primaryActionPanel.AutoSize = true;
        primaryActionPanel.ColumnCount = 2;
        primaryActionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132F));
        primaryActionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132F));
        primaryActionPanel.Controls.Add(_startButton, 0, 0);
        primaryActionPanel.Controls.Add(_managePresetsButton, 1, 0);
        primaryActionPanel.Controls.Add(_snapshotButton, 0, 1);
        primaryActionPanel.Controls.Add(_openPlaybackButton, 1, 1);
        primaryActionPanel.Location = new Point(1080, 0);
        primaryActionPanel.Margin = new Padding(0);
        primaryActionPanel.Name = "primaryActionPanel";
        primaryActionPanel.RowCount = 2;
        primaryActionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        primaryActionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        primaryActionPanel.Size = new Size(264, 88);
        primaryActionPanel.TabIndex = 1;
        // 
        // _startButton
        // 
        _startButton.BackColor = Color.FromArgb(28, 37, 52);
        _startButton.Dock = DockStyle.Fill;
        _startButton.FlatStyle = FlatStyle.Flat;
        _startButton.Font = new Font("Yu Gothic UI", 10F);
        _startButton.ForeColor = Color.FromArgb(235, 241, 248);
        _startButton.Location = new Point(4, 4);
        _startButton.Margin = new Padding(4);
        _startButton.Name = "_startButton";
        _startButton.Padding = new Padding(12, 0, 12, 0);
        _startButton.Size = new Size(124, 36);
        _startButton.TabIndex = 0;
        _startButton.Text = "プレビュー開始";
        _startButton.UseVisualStyleBackColor = false;
        // 
        // _managePresetsButton
        // 
        _managePresetsButton.BackColor = Color.FromArgb(28, 37, 52);
        _managePresetsButton.Dock = DockStyle.Fill;
        _managePresetsButton.FlatStyle = FlatStyle.Flat;
        _managePresetsButton.Font = new Font("Yu Gothic UI", 10F);
        _managePresetsButton.ForeColor = Color.FromArgb(235, 241, 248);
        _managePresetsButton.Location = new Point(136, 4);
        _managePresetsButton.Margin = new Padding(4);
        _managePresetsButton.Name = "_managePresetsButton";
        _managePresetsButton.Padding = new Padding(12, 0, 12, 0);
        _managePresetsButton.Size = new Size(124, 36);
        _managePresetsButton.TabIndex = 1;
        _managePresetsButton.Text = "設定";
        _managePresetsButton.UseVisualStyleBackColor = false;
        // 
        // _snapshotButton
        // 
        _snapshotButton.BackColor = Color.FromArgb(28, 37, 52);
        _snapshotButton.Dock = DockStyle.Fill;
        _snapshotButton.Enabled = false;
        _snapshotButton.FlatStyle = FlatStyle.Flat;
        _snapshotButton.Font = new Font("Yu Gothic UI", 10F);
        _snapshotButton.ForeColor = Color.FromArgb(235, 241, 248);
        _snapshotButton.Location = new Point(4, 48);
        _snapshotButton.Margin = new Padding(4);
        _snapshotButton.Name = "_snapshotButton";
        _snapshotButton.Padding = new Padding(12, 0, 12, 0);
        _snapshotButton.Size = new Size(124, 36);
        _snapshotButton.TabIndex = 1;
        _snapshotButton.Text = "静止画";
        _snapshotButton.UseVisualStyleBackColor = false;
        // 
        // _openPlaybackButton
        // 
        _openPlaybackButton.BackColor = Color.FromArgb(28, 37, 52);
        _openPlaybackButton.Dock = DockStyle.Fill;
        _openPlaybackButton.FlatStyle = FlatStyle.Flat;
        _openPlaybackButton.Font = new Font("Yu Gothic UI", 10F);
        _openPlaybackButton.ForeColor = Color.FromArgb(235, 241, 248);
        _openPlaybackButton.Location = new Point(136, 48);
        _openPlaybackButton.Margin = new Padding(4);
        _openPlaybackButton.Name = "_openPlaybackButton";
        _openPlaybackButton.Padding = new Padding(12, 0, 12, 0);
        _openPlaybackButton.Size = new Size(124, 36);
        _openPlaybackButton.TabIndex = 3;
        _openPlaybackButton.Text = PlaybackCaptions.OpenPlayback;
        _openPlaybackButton.UseVisualStyleBackColor = false;
        // 
        // _quickPresetPanel
        // 
        _quickPresetPanel.AutoSize = true;
        _quickPresetPanel.Controls.Add(_presetButton1);
        _quickPresetPanel.Controls.Add(_presetButton2);
        _quickPresetPanel.Controls.Add(_presetButton3);
        _quickPresetPanel.Controls.Add(_presetButton4);
        _quickPresetPanel.Controls.Add(_presetButton5);
        _quickPresetPanel.Controls.Add(_morePresetsDropDownButton);
        _quickPresetPanel.Location = new Point(0, 94);
        _quickPresetPanel.Margin = new Padding(0, 6, 0, 0);
        _quickPresetPanel.Name = "_quickPresetPanel";
        _quickPresetPanel.Size = new Size(803, 38);
        _quickPresetPanel.TabIndex = 2;
        _quickPresetPanel.WrapContents = false;
        // 
        // _presetButton1
        // 
        _presetButton1.Appearance = Appearance.Button;
        _presetButton1.AutoEllipsis = true;
        _presetButton1.BackColor = Color.FromArgb(28, 37, 52);
        _presetButton1.FlatStyle = FlatStyle.Flat;
        _presetButton1.Font = new Font("Yu Gothic UI", 10F);
        _presetButton1.ForeColor = Color.FromArgb(148, 163, 184);
        _presetButton1.Location = new Point(0, 0);
        _presetButton1.Margin = new Padding(0, 0, 7, 0);
        _presetButton1.Name = "_presetButton1";
        _presetButton1.Size = new Size(128, 38);
        _presetButton1.TabIndex = 0;
        _presetButton1.Text = "プリセット1";
        _presetButton1.TextAlign = ContentAlignment.MiddleCenter;
        _presetButton1.UseVisualStyleBackColor = false;
        // 
        // _presetButton2
        // 
        _presetButton2.Appearance = Appearance.Button;
        _presetButton2.AutoEllipsis = true;
        _presetButton2.BackColor = Color.FromArgb(28, 37, 52);
        _presetButton2.FlatStyle = FlatStyle.Flat;
        _presetButton2.Font = new Font("Yu Gothic UI", 10F);
        _presetButton2.ForeColor = Color.FromArgb(148, 163, 184);
        _presetButton2.Location = new Point(135, 0);
        _presetButton2.Margin = new Padding(0, 0, 7, 0);
        _presetButton2.Name = "_presetButton2";
        _presetButton2.Size = new Size(128, 38);
        _presetButton2.TabIndex = 1;
        _presetButton2.Text = "プリセット2";
        _presetButton2.TextAlign = ContentAlignment.MiddleCenter;
        _presetButton2.UseVisualStyleBackColor = false;
        // 
        // _presetButton3
        // 
        _presetButton3.Appearance = Appearance.Button;
        _presetButton3.AutoEllipsis = true;
        _presetButton3.BackColor = Color.FromArgb(28, 37, 52);
        _presetButton3.FlatStyle = FlatStyle.Flat;
        _presetButton3.Font = new Font("Yu Gothic UI", 10F);
        _presetButton3.ForeColor = Color.FromArgb(148, 163, 184);
        _presetButton3.Location = new Point(270, 0);
        _presetButton3.Margin = new Padding(0, 0, 7, 0);
        _presetButton3.Name = "_presetButton3";
        _presetButton3.Size = new Size(128, 38);
        _presetButton3.TabIndex = 2;
        _presetButton3.Text = "プリセット3";
        _presetButton3.TextAlign = ContentAlignment.MiddleCenter;
        _presetButton3.UseVisualStyleBackColor = false;
        // 
        // _presetButton4
        // 
        _presetButton4.Appearance = Appearance.Button;
        _presetButton4.AutoEllipsis = true;
        _presetButton4.BackColor = Color.FromArgb(28, 37, 52);
        _presetButton4.FlatStyle = FlatStyle.Flat;
        _presetButton4.Font = new Font("Yu Gothic UI", 10F);
        _presetButton4.ForeColor = Color.FromArgb(148, 163, 184);
        _presetButton4.Location = new Point(405, 0);
        _presetButton4.Margin = new Padding(0, 0, 7, 0);
        _presetButton4.Name = "_presetButton4";
        _presetButton4.Size = new Size(128, 38);
        _presetButton4.TabIndex = 3;
        _presetButton4.Text = "プリセット4";
        _presetButton4.TextAlign = ContentAlignment.MiddleCenter;
        _presetButton4.UseVisualStyleBackColor = false;
        // 
        // _presetButton5
        // 
        _presetButton5.Appearance = Appearance.Button;
        _presetButton5.AutoEllipsis = true;
        _presetButton5.BackColor = Color.FromArgb(28, 37, 52);
        _presetButton5.FlatStyle = FlatStyle.Flat;
        _presetButton5.Font = new Font("Yu Gothic UI", 10F);
        _presetButton5.ForeColor = Color.FromArgb(148, 163, 184);
        _presetButton5.Location = new Point(540, 0);
        _presetButton5.Margin = new Padding(0, 0, 7, 0);
        _presetButton5.Name = "_presetButton5";
        _presetButton5.Size = new Size(128, 38);
        _presetButton5.TabIndex = 4;
        _presetButton5.Text = "プリセット5";
        _presetButton5.TextAlign = ContentAlignment.MiddleCenter;
        _presetButton5.UseVisualStyleBackColor = false;
        // 
        // _morePresetsDropDownButton
        // 
        _morePresetsDropDownButton.BackColor = Color.FromArgb(28, 37, 52);
        _morePresetsDropDownButton.FlatStyle = FlatStyle.Flat;
        _morePresetsDropDownButton.Font = new Font("Yu Gothic UI", 10F);
        _morePresetsDropDownButton.ForeColor = Color.FromArgb(235, 241, 248);
        _morePresetsDropDownButton.Location = new Point(675, 0);
        _morePresetsDropDownButton.Margin = new Padding(0);
        _morePresetsDropDownButton.Name = "_morePresetsDropDownButton";
        _morePresetsDropDownButton.Padding = new Padding(12, 0, 12, 0);
        _morePresetsDropDownButton.Size = new Size(128, 38);
        _morePresetsDropDownButton.TabIndex = 5;
        _morePresetsDropDownButton.Text = "その他 ▾";
        _morePresetsDropDownButton.UseVisualStyleBackColor = false;
        // 
        // presetActionPanel
        // 
        presetActionPanel.AutoSize = true;
        presetActionPanel.Location = new Point(1088, 100);
        presetActionPanel.Margin = new Padding(8, 12, 0, 0);
        presetActionPanel.Name = "presetActionPanel";
        presetActionPanel.Size = new Size(0, 0);
        presetActionPanel.TabIndex = 3;
        presetActionPanel.WrapContents = false;
        // 
        // workspaceLayout
        // 
        workspaceLayout.ColumnCount = 2;
        workspaceLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        workspaceLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 310F));
        workspaceLayout.Controls.Add(previewSurfacePanel, 0, 0);
        workspaceLayout.Controls.Add(sidePanel, 1, 0);
        workspaceLayout.Dock = DockStyle.Fill;
        workspaceLayout.Location = new Point(18, 164);
        workspaceLayout.Margin = new Padding(0);
        workspaceLayout.Name = "workspaceLayout";
        workspaceLayout.RowCount = 1;
        workspaceLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        workspaceLayout.Size = new Size(1344, 646);
        workspaceLayout.TabIndex = 1;
        // 
        // previewSurfacePanel
        // 
        previewSurfacePanel.BackColor = Color.Black;
        previewSurfacePanel.Controls.Add(_previewBox);
        previewSurfacePanel.Controls.Add(_previewMessage);
        previewSurfacePanel.Controls.Add(liveBadgeLabel);
        previewSurfacePanel.Controls.Add(_thumbnailStripPanel);
        previewSurfacePanel.Controls.Add(playbackPanel);
        previewSurfacePanel.Dock = DockStyle.Fill;
        previewSurfacePanel.Location = new Point(0, 0);
        previewSurfacePanel.Margin = new Padding(0, 0, 14, 0);
        previewSurfacePanel.Name = "previewSurfacePanel";
        previewSurfacePanel.Padding = new Padding(1);
        previewSurfacePanel.Size = new Size(1020, 646);
        previewSurfacePanel.TabIndex = 0;
        previewSurfacePanel.Resize += PreviewSurfacePanel_Resize;
        // 
        // _previewBox
        // 
        _previewBox.BackColor = Color.FromArgb(4, 7, 12);
        _previewBox.Dock = DockStyle.Fill;
        _previewBox.Location = new Point(1, 1);
        _previewBox.Name = "_previewBox";
        _previewBox.Size = new Size(1018, 504);
        _previewBox.SizeMode = PictureBoxSizeMode.Zoom;
        _previewBox.TabIndex = 0;
        _previewBox.TabStop = false;
        // 
        // _previewMessage
        // 
        _previewMessage.AutoSize = true;
        _previewMessage.BackColor = Color.Transparent;
        _previewMessage.Font = new Font("Yu Gothic UI", 12F);
        _previewMessage.ForeColor = Color.FromArgb(148, 163, 184);
        _previewMessage.Location = new Point(0, 0);
        _previewMessage.Name = "_previewMessage";
        _previewMessage.Size = new Size(309, 21);
        _previewMessage.TabIndex = 1;
        _previewMessage.Text = "カメラを選択して「プレビュー開始」を押してください";
        // 
        // liveBadgeLabel
        // 
        liveBadgeLabel.AutoSize = true;
        liveBadgeLabel.BackColor = Color.FromArgb(34, 20, 184, 166);
        liveBadgeLabel.Font = new Font("Yu Gothic UI", 9F);
        liveBadgeLabel.ForeColor = Color.FromArgb(45, 212, 191);
        liveBadgeLabel.Location = new Point(14, 14);
        liveBadgeLabel.Name = "liveBadgeLabel";
        liveBadgeLabel.Padding = new Padding(7, 5, 7, 5);
        liveBadgeLabel.Size = new Size(99, 25);
        liveBadgeLabel.TabIndex = 2;
        liveBadgeLabel.Text = " LIVE PREVIEW ";
        // 
        // _thumbnailStripPanel
        // 
        _thumbnailStripPanel.AutoScroll = true;
        _thumbnailStripPanel.BackColor = Color.FromArgb(12, 17, 26);
        _thumbnailStripPanel.Controls.Add(_thumbnailEmptyLabel);
        _thumbnailStripPanel.Dock = DockStyle.Bottom;
        _thumbnailStripPanel.Location = new Point(1, 505);
        _thumbnailStripPanel.Name = "_thumbnailStripPanel";
        _thumbnailStripPanel.Padding = new Padding(8, 7, 8, 7);
        _thumbnailStripPanel.Size = new Size(1018, 84);
        _thumbnailStripPanel.TabIndex = 4;
        _thumbnailStripPanel.WrapContents = false;
        // 
        // _thumbnailEmptyLabel
        // 
        _thumbnailEmptyLabel.ForeColor = Color.FromArgb(148, 163, 184);
        _thumbnailEmptyLabel.Location = new Point(11, 7);
        _thumbnailEmptyLabel.Name = "_thumbnailEmptyLabel";
        _thumbnailEmptyLabel.Size = new Size(240, 66);
        _thumbnailEmptyLabel.TabIndex = 0;
        _thumbnailEmptyLabel.Text = "静止画を保存すると、ここに直近のサムネイルを表示します";
        _thumbnailEmptyLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // playbackPanel
        // 
        playbackPanel.BackColor = Color.FromArgb(20, 27, 39);
        playbackPanel.ColumnCount = 5;
        playbackPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
        playbackPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        playbackPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        playbackPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        playbackPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        playbackPanel.Controls.Add(_playPauseButton, 0, 0);
        playbackPanel.Controls.Add(_playbackTrackBar, 1, 0);
        playbackPanel.Controls.Add(_playbackPositionLabel, 2, 0);
        playbackPanel.Controls.Add(_openClipEditorButton, 3, 0);
        playbackPanel.Controls.Add(_closePlaybackButton, 4, 0);
        playbackPanel.Dock = DockStyle.Bottom;
        playbackPanel.Location = new Point(1, 589);
        playbackPanel.Name = "playbackPanel";
        playbackPanel.Padding = new Padding(10, 8, 10, 8);
        playbackPanel.RowCount = 1;
        playbackPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        playbackPanel.Size = new Size(1018, 56);
        playbackPanel.TabIndex = 3;
        playbackPanel.Tag = "raised";
        playbackPanel.Visible = false;
        // 
        // _playPauseButton
        // 
        _playPauseButton.BackColor = Color.FromArgb(28, 37, 52);
        _playPauseButton.Dock = DockStyle.Fill;
        _playPauseButton.FlatStyle = FlatStyle.Flat;
        _playPauseButton.Font = new Font("Yu Gothic UI", 10F);
        _playPauseButton.ForeColor = Color.FromArgb(235, 241, 248);
        _playPauseButton.Location = new Point(13, 11);
        _playPauseButton.Name = "_playPauseButton";
        _playPauseButton.Padding = new Padding(12, 0, 12, 0);
        _playPauseButton.Size = new Size(94, 34);
        _playPauseButton.TabIndex = 0;
        _playPauseButton.Text = PlaybackCaptions.Pause;
        _playPauseButton.UseVisualStyleBackColor = false;
        // 
        // _playbackTrackBar
        // 
        _playbackTrackBar.Dock = DockStyle.Fill;
        _playbackTrackBar.Location = new Point(113, 11);
        _playbackTrackBar.Maximum = 1000;
        _playbackTrackBar.Name = "_playbackTrackBar";
        _playbackTrackBar.Size = new Size(642, 34);
        _playbackTrackBar.TabIndex = 1;
        _playbackTrackBar.TickStyle = TickStyle.None;
        // 
        // _playbackPositionLabel
        // 
        _playbackPositionLabel.Dock = DockStyle.Fill;
        _playbackPositionLabel.Location = new Point(761, 8);
        _playbackPositionLabel.Name = "_playbackPositionLabel";
        _playbackPositionLabel.Size = new Size(124, 40);
        _playbackPositionLabel.TabIndex = 2;
        _playbackPositionLabel.Text = "00:00 / 00:00";
        _playbackPositionLabel.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // _openClipEditorButton
        // 
        _openClipEditorButton.BackColor = Color.FromArgb(28, 37, 52);
        _openClipEditorButton.Dock = DockStyle.Fill;
        _openClipEditorButton.FlatStyle = FlatStyle.Flat;
        _openClipEditorButton.Font = new Font("Yu Gothic UI", 10F);
        _openClipEditorButton.ForeColor = Color.FromArgb(235, 241, 248);
        _openClipEditorButton.Location = new Point(891, 11);
        _openClipEditorButton.Name = "_openClipEditorButton";
        _openClipEditorButton.Padding = new Padding(12, 0, 12, 0);
        _openClipEditorButton.Size = new Size(114, 34);
        _openClipEditorButton.TabIndex = 3;
        _openClipEditorButton.Text = "動画編集";
        _openClipEditorButton.UseVisualStyleBackColor = false;
        // 
        // _closePlaybackButton
        // 
        _closePlaybackButton.BackColor = Color.FromArgb(28, 37, 52);
        _closePlaybackButton.Dock = DockStyle.Fill;
        _closePlaybackButton.FlatStyle = FlatStyle.Flat;
        _closePlaybackButton.Font = new Font("Yu Gothic UI", 10F);
        _closePlaybackButton.ForeColor = Color.FromArgb(235, 241, 248);
        _closePlaybackButton.Location = new Point(1011, 11);
        _closePlaybackButton.Name = "_closePlaybackButton";
        _closePlaybackButton.Padding = new Padding(12, 0, 12, 0);
        _closePlaybackButton.Size = new Size(114, 34);
        _closePlaybackButton.TabIndex = 4;
        _closePlaybackButton.Text = PlaybackCaptions.ClosePlayback;
        _closePlaybackButton.UseVisualStyleBackColor = false;
        // 
        // sidePanel
        // 
        sidePanel.AutoScroll = true;
        sidePanel.BackColor = Color.FromArgb(12, 17, 26);
        sidePanel.Controls.Add(sourceCard);
        sidePanel.Controls.Add(filterCard);
        sidePanel.Controls.Add(captureCard);
        sidePanel.Dock = DockStyle.Fill;
        sidePanel.FlowDirection = FlowDirection.TopDown;
        sidePanel.Location = new Point(1034, 0);
        sidePanel.Margin = new Padding(0);
        sidePanel.Name = "sidePanel";
        sidePanel.Size = new Size(310, 646);
        sidePanel.TabIndex = 1;
        sidePanel.WrapContents = false;
        // 
        // sourceCard
        // 
        sourceCard.AutoSize = true;
        sourceCard.BackColor = Color.FromArgb(20, 27, 39);
        sourceCard.ColumnCount = 1;
        sourceCard.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
        sourceCard.Controls.Add(sourceTitleLabel);
        sourceCard.Controls.Add(cameraLabel);
        sourceCard.Controls.Add(_deviceComboBox);
        sourceCard.Controls.Add(resolutionLabel);
        sourceCard.Controls.Add(_resolutionComboBox);
        sourceCard.Controls.Add(sourceButtonPanel);
        sourceCard.Location = new Point(3, 3);
        sourceCard.Name = "sourceCard";
        sourceCard.Padding = new Padding(16);
        sourceCard.RowStyles.Add(new RowStyle());
        sourceCard.RowStyles.Add(new RowStyle());
        sourceCard.RowStyles.Add(new RowStyle());
        sourceCard.RowStyles.Add(new RowStyle());
        sourceCard.RowStyles.Add(new RowStyle());
        sourceCard.RowStyles.Add(new RowStyle());
        sourceCard.Size = new Size(294, 185);
        sourceCard.TabIndex = 0;
        // 
        // sourceTitleLabel
        // 
        sourceTitleLabel.AutoSize = true;
        sourceTitleLabel.Font = new Font("Yu Gothic UI", 9F);
        sourceTitleLabel.ForeColor = Color.FromArgb(45, 212, 191);
        sourceTitleLabel.Location = new Point(16, 16);
        sourceTitleLabel.Margin = new Padding(0, 0, 0, 12);
        sourceTitleLabel.Name = "sourceTitleLabel";
        sourceTitleLabel.Size = new Size(86, 15);
        sourceTitleLabel.TabIndex = 0;
        sourceTitleLabel.Text = "VIDEO SOURCE";
        // 
        // cameraLabel
        // 
        cameraLabel.AutoSize = true;
        cameraLabel.ForeColor = Color.FromArgb(148, 163, 184);
        cameraLabel.Location = new Point(19, 43);
        cameraLabel.Name = "cameraLabel";
        cameraLabel.Size = new Size(58, 15);
        cameraLabel.TabIndex = 1;
        cameraLabel.Text = "ビデオソース";
        // 
        // _deviceComboBox
        // 
        _deviceComboBox.BackColor = Color.FromArgb(28, 37, 52);
        _deviceComboBox.Dock = DockStyle.Fill;
        _deviceComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _deviceComboBox.FlatStyle = FlatStyle.Flat;
        _deviceComboBox.ForeColor = Color.FromArgb(235, 241, 248);
        _deviceComboBox.Location = new Point(19, 61);
        _deviceComboBox.Name = "_deviceComboBox";
        _deviceComboBox.Size = new Size(256, 23);
        _deviceComboBox.TabIndex = 2;
        // 
        // resolutionLabel
        // 
        resolutionLabel.AutoSize = true;
        resolutionLabel.ForeColor = Color.FromArgb(148, 163, 184);
        resolutionLabel.Location = new Point(19, 87);
        resolutionLabel.Name = "resolutionLabel";
        resolutionLabel.Size = new Size(43, 15);
        resolutionLabel.TabIndex = 3;
        resolutionLabel.Text = "解像度";
        // 
        // _resolutionComboBox
        // 
        _resolutionComboBox.BackColor = Color.FromArgb(28, 37, 52);
        _resolutionComboBox.Dock = DockStyle.Fill;
        _resolutionComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _resolutionComboBox.FlatStyle = FlatStyle.Flat;
        _resolutionComboBox.ForeColor = Color.FromArgb(235, 241, 248);
        _resolutionComboBox.Location = new Point(19, 105);
        _resolutionComboBox.Name = "_resolutionComboBox";
        _resolutionComboBox.Size = new Size(256, 23);
        _resolutionComboBox.TabIndex = 4;
        // 
        // sourceButtonPanel
        // 
        sourceButtonPanel.AutoSize = true;
        sourceButtonPanel.Controls.Add(_refreshDevicesButton);
        sourceButtonPanel.Dock = DockStyle.Fill;
        sourceButtonPanel.Location = new Point(16, 139);
        sourceButtonPanel.Margin = new Padding(0, 8, 0, 0);
        sourceButtonPanel.Name = "sourceButtonPanel";
        sourceButtonPanel.Size = new Size(262, 30);
        sourceButtonPanel.TabIndex = 5;
        sourceButtonPanel.WrapContents = false;
        // 
        // _refreshDevicesButton
        // 
        _refreshDevicesButton.BackColor = Color.FromArgb(28, 37, 52);
        _refreshDevicesButton.FlatStyle = FlatStyle.Flat;
        _refreshDevicesButton.Font = new Font("Yu Gothic UI", 10F);
        _refreshDevicesButton.ForeColor = Color.FromArgb(235, 241, 248);
        _refreshDevicesButton.Location = new Point(3, 3);
        _refreshDevicesButton.Name = "_refreshDevicesButton";
        _refreshDevicesButton.Padding = new Padding(12, 0, 12, 0);
        _refreshDevicesButton.Size = new Size(82, 24);
        _refreshDevicesButton.TabIndex = 1;
        _refreshDevicesButton.Text = "再検索";
        _refreshDevicesButton.UseVisualStyleBackColor = false;
        // 
        // filterCard
        // 
        filterCard.AutoSize = true;
        filterCard.BackColor = Color.FromArgb(20, 27, 39);
        filterCard.ColumnCount = 1;
        filterCard.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
        filterCard.Controls.Add(filterTitleLabel);
        filterCard.Controls.Add(whiteBalancePanel);
        filterCard.Controls.Add(redRow);
        filterCard.Controls.Add(greenRow);
        filterCard.Controls.Add(blueRow);
        filterCard.Controls.Add(gammaRow);
        filterCard.Controls.Add(flipPanel);
        filterCard.Controls.Add(_resetFiltersButton);
        filterCard.Location = new Point(3, 194);
        filterCard.Name = "filterCard";
        filterCard.Padding = new Padding(16);
        filterCard.RowStyles.Add(new RowStyle());
        filterCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        filterCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        filterCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        filterCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        filterCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        filterCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        filterCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        filterCard.Size = new Size(294, 345);
        filterCard.TabIndex = 1;
        // 
        // filterTitleLabel
        // 
        filterTitleLabel.AutoSize = true;
        filterTitleLabel.Font = new Font("Yu Gothic UI", 9F);
        filterTitleLabel.ForeColor = Color.FromArgb(45, 212, 191);
        filterTitleLabel.Location = new Point(16, 16);
        filterTitleLabel.Margin = new Padding(0, 0, 0, 12);
        filterTitleLabel.Name = "filterTitleLabel";
        filterTitleLabel.Size = new Size(99, 15);
        filterTitleLabel.TabIndex = 0;
        filterTitleLabel.Text = "IMAGE CONTROL";
        // 
        // whiteBalancePanel
        // 
        whiteBalancePanel.Controls.Add(_whiteBalanceSelectionCheckBox);
        whiteBalancePanel.Controls.Add(whiteBalanceHintLabel);
        whiteBalancePanel.Dock = DockStyle.Fill;
        whiteBalancePanel.Location = new Point(19, 46);
        whiteBalancePanel.Name = "whiteBalancePanel";
        whiteBalancePanel.Size = new Size(256, 36);
        whiteBalancePanel.TabIndex = 1;
        whiteBalancePanel.WrapContents = false;
        // 
        // _whiteBalanceSelectionCheckBox
        // 
        _whiteBalanceSelectionCheckBox.AutoSize = true;
        _whiteBalanceSelectionCheckBox.Location = new Point(3, 8);
        _whiteBalanceSelectionCheckBox.Margin = new Padding(3, 8, 8, 3);
        _whiteBalanceSelectionCheckBox.Name = "_whiteBalanceSelectionCheckBox";
        _whiteBalanceSelectionCheckBox.Size = new Size(89, 19);
        _whiteBalanceSelectionCheckBox.TabIndex = 0;
        _whiteBalanceSelectionCheckBox.Text = "WB選択 ON";
        // 
        // whiteBalanceHintLabel
        // 
        whiteBalanceHintLabel.AutoSize = true;
        whiteBalanceHintLabel.Location = new Point(103, 8);
        whiteBalanceHintLabel.Margin = new Padding(3, 8, 3, 0);
        whiteBalanceHintLabel.Name = "whiteBalanceHintLabel";
        whiteBalanceHintLabel.Size = new Size(95, 15);
        whiteBalanceHintLabel.TabIndex = 1;
        whiteBalanceHintLabel.Text = "白い部分をドラッグ";
        // 
        // redRow
        // 
        redRow.ColumnCount = 3;
        redRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22F));
        redRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        redRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46F));
        redRow.Controls.Add(redCaptionLabel, 0, 0);
        redRow.Controls.Add(_redTrack, 1, 0);
        redRow.Controls.Add(_redValue, 2, 0);
        redRow.Dock = DockStyle.Fill;
        redRow.Location = new Point(19, 88);
        redRow.Name = "redRow";
        redRow.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        redRow.Size = new Size(256, 36);
        redRow.TabIndex = 1;
        // 
        // redCaptionLabel
        // 
        redCaptionLabel.Dock = DockStyle.Fill;
        redCaptionLabel.ForeColor = Color.FromArgb(148, 163, 184);
        redCaptionLabel.Location = new Point(3, 0);
        redCaptionLabel.Name = "redCaptionLabel";
        redCaptionLabel.Size = new Size(16, 42);
        redCaptionLabel.TabIndex = 0;
        redCaptionLabel.Text = "R";
        redCaptionLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _redTrack
        // 
        _redTrack.BackColor = Color.FromArgb(20, 27, 39);
        _redTrack.Dock = DockStyle.Fill;
        _redTrack.Location = new Point(25, 3);
        _redTrack.Maximum = 510;
        _redTrack.Name = "_redTrack";
        _redTrack.Size = new Size(182, 36);
        _redTrack.TabIndex = 0;
        _redTrack.TickStyle = TickStyle.None;
        _redTrack.Value = 255;
        // 
        // _redValue
        // 
        _redValue.ForeColor = Color.FromArgb(235, 241, 248);
        _redValue.Location = new Point(213, 0);
        _redValue.Name = "_redValue";
        _redValue.Size = new Size(40, 20);
        _redValue.TabIndex = 0;
        _redValue.Text = "255";
        _redValue.TextAlign = ContentAlignment.MiddleRight;
        // 
        // greenRow
        // 
        greenRow.ColumnCount = 3;
        greenRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22F));
        greenRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        greenRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46F));
        greenRow.Controls.Add(greenCaptionLabel, 0, 0);
        greenRow.Controls.Add(_greenTrack, 1, 0);
        greenRow.Controls.Add(_greenValue, 2, 0);
        greenRow.Dock = DockStyle.Fill;
        greenRow.Location = new Point(19, 130);
        greenRow.Name = "greenRow";
        greenRow.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        greenRow.Size = new Size(256, 36);
        greenRow.TabIndex = 2;
        // 
        // greenCaptionLabel
        // 
        greenCaptionLabel.Dock = DockStyle.Fill;
        greenCaptionLabel.ForeColor = Color.FromArgb(148, 163, 184);
        greenCaptionLabel.Location = new Point(3, 0);
        greenCaptionLabel.Name = "greenCaptionLabel";
        greenCaptionLabel.Size = new Size(16, 42);
        greenCaptionLabel.TabIndex = 0;
        greenCaptionLabel.Text = "G";
        greenCaptionLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _greenTrack
        // 
        _greenTrack.BackColor = Color.FromArgb(20, 27, 39);
        _greenTrack.Dock = DockStyle.Fill;
        _greenTrack.Location = new Point(25, 3);
        _greenTrack.Maximum = 510;
        _greenTrack.Name = "_greenTrack";
        _greenTrack.Size = new Size(182, 36);
        _greenTrack.TabIndex = 0;
        _greenTrack.TickStyle = TickStyle.None;
        _greenTrack.Value = 255;
        // 
        // _greenValue
        // 
        _greenValue.ForeColor = Color.FromArgb(235, 241, 248);
        _greenValue.Location = new Point(213, 0);
        _greenValue.Name = "_greenValue";
        _greenValue.Size = new Size(40, 20);
        _greenValue.TabIndex = 0;
        _greenValue.Text = "255";
        _greenValue.TextAlign = ContentAlignment.MiddleRight;
        // 
        // blueRow
        // 
        blueRow.ColumnCount = 3;
        blueRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22F));
        blueRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        blueRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46F));
        blueRow.Controls.Add(blueCaptionLabel, 0, 0);
        blueRow.Controls.Add(_blueTrack, 1, 0);
        blueRow.Controls.Add(_blueValue, 2, 0);
        blueRow.Dock = DockStyle.Fill;
        blueRow.Location = new Point(19, 172);
        blueRow.Name = "blueRow";
        blueRow.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        blueRow.Size = new Size(256, 36);
        blueRow.TabIndex = 3;
        // 
        // blueCaptionLabel
        // 
        blueCaptionLabel.Dock = DockStyle.Fill;
        blueCaptionLabel.ForeColor = Color.FromArgb(148, 163, 184);
        blueCaptionLabel.Location = new Point(3, 0);
        blueCaptionLabel.Name = "blueCaptionLabel";
        blueCaptionLabel.Size = new Size(16, 42);
        blueCaptionLabel.TabIndex = 0;
        blueCaptionLabel.Text = "B";
        blueCaptionLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _blueTrack
        // 
        _blueTrack.BackColor = Color.FromArgb(20, 27, 39);
        _blueTrack.Dock = DockStyle.Fill;
        _blueTrack.Location = new Point(25, 3);
        _blueTrack.Maximum = 510;
        _blueTrack.Name = "_blueTrack";
        _blueTrack.Size = new Size(182, 36);
        _blueTrack.TabIndex = 0;
        _blueTrack.TickStyle = TickStyle.None;
        _blueTrack.Value = 255;
        // 
        // _blueValue
        // 
        _blueValue.ForeColor = Color.FromArgb(235, 241, 248);
        _blueValue.Location = new Point(213, 0);
        _blueValue.Name = "_blueValue";
        _blueValue.Size = new Size(40, 20);
        _blueValue.TabIndex = 0;
        _blueValue.Text = "255";
        _blueValue.TextAlign = ContentAlignment.MiddleRight;
        // 
        // gammaRow
        // 
        gammaRow.ColumnCount = 3;
        gammaRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22F));
        gammaRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        gammaRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46F));
        gammaRow.Controls.Add(gammaCaptionLabel, 0, 0);
        gammaRow.Controls.Add(_gammaTrack, 1, 0);
        gammaRow.Controls.Add(_gammaValue, 2, 0);
        gammaRow.Dock = DockStyle.Fill;
        gammaRow.Location = new Point(19, 214);
        gammaRow.Name = "gammaRow";
        gammaRow.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        gammaRow.Size = new Size(256, 36);
        gammaRow.TabIndex = 4;
        // 
        // gammaCaptionLabel
        // 
        gammaCaptionLabel.Dock = DockStyle.Fill;
        gammaCaptionLabel.ForeColor = Color.FromArgb(148, 163, 184);
        gammaCaptionLabel.Location = new Point(3, 0);
        gammaCaptionLabel.Name = "gammaCaptionLabel";
        gammaCaptionLabel.Size = new Size(16, 42);
        gammaCaptionLabel.TabIndex = 0;
        gammaCaptionLabel.Text = "γ";
        gammaCaptionLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _gammaTrack
        // 
        _gammaTrack.BackColor = Color.FromArgb(20, 27, 39);
        _gammaTrack.Dock = DockStyle.Fill;
        _gammaTrack.Location = new Point(25, 3);
        _gammaTrack.Maximum = 30;
        _gammaTrack.Minimum = 1;
        _gammaTrack.Name = "_gammaTrack";
        _gammaTrack.Size = new Size(182, 36);
        _gammaTrack.TabIndex = 0;
        _gammaTrack.TickStyle = TickStyle.None;
        _gammaTrack.Value = 10;
        // 
        // _gammaValue
        // 
        _gammaValue.ForeColor = Color.FromArgb(235, 241, 248);
        _gammaValue.Location = new Point(213, 0);
        _gammaValue.Name = "_gammaValue";
        _gammaValue.Size = new Size(40, 20);
        _gammaValue.TabIndex = 0;
        _gammaValue.Text = "1.0";
        _gammaValue.TextAlign = ContentAlignment.MiddleRight;
        // 
        // flipPanel
        // 
        flipPanel.Controls.Add(_flipHorizontalCheckBox);
        flipPanel.Controls.Add(_flipVerticalCheckBox);
        flipPanel.Dock = DockStyle.Fill;
        flipPanel.Location = new Point(19, 256);
        flipPanel.Name = "flipPanel";
        flipPanel.Size = new Size(256, 32);
        flipPanel.TabIndex = 5;
        flipPanel.WrapContents = false;
        // 
        // _flipHorizontalCheckBox
        // 
        _flipHorizontalCheckBox.AutoSize = true;
        _flipHorizontalCheckBox.Location = new Point(3, 6);
        _flipHorizontalCheckBox.Margin = new Padding(3, 6, 18, 3);
        _flipHorizontalCheckBox.Name = "_flipHorizontalCheckBox";
        _flipHorizontalCheckBox.Size = new Size(74, 19);
        _flipHorizontalCheckBox.TabIndex = 0;
        _flipHorizontalCheckBox.Text = "左右反転";
        // 
        // _flipVerticalCheckBox
        // 
        _flipVerticalCheckBox.AutoSize = true;
        _flipVerticalCheckBox.Location = new Point(98, 6);
        _flipVerticalCheckBox.Margin = new Padding(3, 6, 3, 3);
        _flipVerticalCheckBox.Name = "_flipVerticalCheckBox";
        _flipVerticalCheckBox.Size = new Size(74, 19);
        _flipVerticalCheckBox.TabIndex = 1;
        _flipVerticalCheckBox.Text = "上下反転";
        // 
        // _resetFiltersButton
        // 
        _resetFiltersButton.BackColor = Color.FromArgb(28, 37, 52);
        _resetFiltersButton.Dock = DockStyle.Fill;
        _resetFiltersButton.FlatStyle = FlatStyle.Flat;
        _resetFiltersButton.Font = new Font("Yu Gothic UI", 10F);
        _resetFiltersButton.ForeColor = Color.FromArgb(235, 241, 248);
        _resetFiltersButton.Location = new Point(19, 294);
        _resetFiltersButton.Name = "_resetFiltersButton";
        _resetFiltersButton.Padding = new Padding(12, 0, 12, 0);
        _resetFiltersButton.Size = new Size(256, 32);
        _resetFiltersButton.TabIndex = 6;
        _resetFiltersButton.Text = "補正をリセット";
        _resetFiltersButton.UseVisualStyleBackColor = false;
        // 
        // captureCard
        // 
        captureCard.AutoSize = true;
        captureCard.BackColor = Color.FromArgb(20, 27, 39);
        captureCard.ColumnCount = 1;
        captureCard.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
        captureCard.Controls.Add(captureTitleLabel);
        captureCard.Controls.Add(_savePresetButton);
        captureCard.Location = new Point(3, 545);
        captureCard.Name = "captureCard";
        captureCard.Padding = new Padding(16);
        captureCard.RowStyles.Add(new RowStyle());
        captureCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        captureCard.Size = new Size(294, 97);
        captureCard.TabIndex = 2;
        // 
        // captureTitleLabel
        // 
        captureTitleLabel.AutoSize = true;
        captureTitleLabel.Font = new Font("Yu Gothic UI", 9F);
        captureTitleLabel.ForeColor = Color.FromArgb(45, 212, 191);
        captureTitleLabel.Location = new Point(16, 16);
        captureTitleLabel.Margin = new Padding(0, 0, 0, 12);
        captureTitleLabel.Name = "captureTitleLabel";
        captureTitleLabel.Size = new Size(56, 15);
        captureTitleLabel.TabIndex = 0;
        captureTitleLabel.Text = "CAPTURE";
        // 
        // _savePresetButton
        // 
        _savePresetButton.BackColor = Color.FromArgb(28, 37, 52);
        _savePresetButton.Dock = DockStyle.Fill;
        _savePresetButton.FlatStyle = FlatStyle.Flat;
        _savePresetButton.Font = new Font("Yu Gothic UI", 10F);
        _savePresetButton.ForeColor = Color.FromArgb(235, 241, 248);
        _savePresetButton.Location = new Point(19, 46);
        _savePresetButton.Name = "_savePresetButton";
        _savePresetButton.Padding = new Padding(12, 0, 12, 0);
        _savePresetButton.Size = new Size(256, 32);
        _savePresetButton.TabIndex = 1;
        _savePresetButton.Text = "現在値をプリセットへ保存";
        _savePresetButton.UseVisualStyleBackColor = false;
        // 
        // footerLayout
        // 
        footerLayout.AutoSize = true;
        footerLayout.ColumnCount = 3;
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        footerLayout.ColumnStyles.Add(new ColumnStyle());
        footerLayout.ColumnStyles.Add(new ColumnStyle());
        footerLayout.Controls.Add(_statusLabel, 0, 0);
        footerLayout.Controls.Add(_presetLabel, 1, 0);
        footerLayout.Controls.Add(_fpsLabel, 2, 0);
        footerLayout.Dock = DockStyle.Fill;
        footerLayout.Location = new Point(18, 822);
        footerLayout.Margin = new Padding(0, 12, 0, 0);
        footerLayout.Name = "footerLayout";
        footerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        footerLayout.Size = new Size(1344, 20);
        footerLayout.TabIndex = 2;
        // 
        // _statusLabel
        // 
        _statusLabel.Anchor = AnchorStyles.Left;
        _statusLabel.AutoSize = true;
        _statusLabel.ForeColor = Color.FromArgb(148, 163, 184);
        _statusLabel.Location = new Point(3, 2);
        _statusLabel.Name = "_statusLabel";
        _statusLabel.Size = new Size(71, 15);
        _statusLabel.TabIndex = 0;
        _statusLabel.Text = "●  スタンバイ";
        // 
        // _presetLabel
        // 
        _presetLabel.AutoSize = true;
        _presetLabel.ForeColor = Color.FromArgb(148, 163, 184);
        _presetLabel.Location = new Point(1054, 0);
        _presetLabel.Margin = new Padding(0, 0, 24, 0);
        _presetLabel.Name = "_presetLabel";
        _presetLabel.Size = new Size(92, 15);
        _presetLabel.TabIndex = 1;
        _presetLabel.Text = "プリセット: 未選択";
        // 
        // _fpsLabel
        // 
        _fpsLabel.AutoSize = true;
        _fpsLabel.ForeColor = Color.FromArgb(148, 163, 184);
        _fpsLabel.Location = new Point(1173, 0);
        _fpsLabel.Name = "_fpsLabel";
        _fpsLabel.Size = new Size(168, 15);
        _fpsLabel.TabIndex = 2;
        _fpsLabel.Text = "INPUT -- fps  /  PREVIEW -- fps";
        // 
        // _clipEditorPanel
        // 
        _clipEditorPanel.AutoScroll = true;
        _clipEditorPanel.Controls.Add(_markClipStartButton);
        _clipEditorPanel.Controls.Add(_markClipEndButton);
        _clipEditorPanel.Controls.Add(_addClipButton);
        _clipEditorPanel.Controls.Add(_clearClipsButton);
        _clipEditorPanel.Controls.Add(_exportJoinedClipsButton);
        _clipEditorPanel.Controls.Add(_exportSeparateClipsButton);
        _clipEditorPanel.Controls.Add(_clipRangeLabel);
        _clipEditorPanel.Controls.Add(_clipListBox);
        _clipEditorPanel.Dock = DockStyle.Fill;
        _clipEditorPanel.Location = new Point(13, 51);
        _clipEditorPanel.Name = "_clipEditorPanel";
        _clipEditorPanel.Size = new Size(992, 80);
        _clipEditorPanel.TabIndex = 4;
        _clipEditorPanel.WrapContents = false;
        // 
        // _markClipStartButton
        // 
        _markClipStartButton.BackColor = Color.FromArgb(28, 37, 52);
        _markClipStartButton.FlatStyle = FlatStyle.Flat;
        _markClipStartButton.Font = new Font("Yu Gothic UI", 10F);
        _markClipStartButton.ForeColor = Color.FromArgb(235, 241, 248);
        _markClipStartButton.Location = new Point(3, 3);
        _markClipStartButton.Name = "_markClipStartButton";
        _markClipStartButton.Padding = new Padding(12, 0, 12, 0);
        _markClipStartButton.Size = new Size(72, 34);
        _markClipStartButton.TabIndex = 0;
        _markClipStartButton.Text = "始点";
        _markClipStartButton.UseVisualStyleBackColor = false;
        // 
        // _markClipEndButton
        // 
        _markClipEndButton.BackColor = Color.FromArgb(28, 37, 52);
        _markClipEndButton.FlatStyle = FlatStyle.Flat;
        _markClipEndButton.Font = new Font("Yu Gothic UI", 10F);
        _markClipEndButton.ForeColor = Color.FromArgb(235, 241, 248);
        _markClipEndButton.Location = new Point(81, 3);
        _markClipEndButton.Name = "_markClipEndButton";
        _markClipEndButton.Padding = new Padding(12, 0, 12, 0);
        _markClipEndButton.Size = new Size(72, 34);
        _markClipEndButton.TabIndex = 1;
        _markClipEndButton.Text = "終点";
        _markClipEndButton.UseVisualStyleBackColor = false;
        // 
        // _addClipButton
        // 
        _addClipButton.BackColor = Color.FromArgb(20, 184, 166);
        _addClipButton.FlatStyle = FlatStyle.Flat;
        _addClipButton.Font = new Font("Yu Gothic UI", 10F);
        _addClipButton.ForeColor = Color.White;
        _addClipButton.Location = new Point(159, 3);
        _addClipButton.Name = "_addClipButton";
        _addClipButton.Padding = new Padding(12, 0, 12, 0);
        _addClipButton.Size = new Size(88, 34);
        _addClipButton.TabIndex = 2;
        _addClipButton.Text = "区間追加";
        _addClipButton.UseVisualStyleBackColor = false;
        // 
        // _clearClipsButton
        // 
        _clearClipsButton.BackColor = Color.FromArgb(28, 37, 52);
        _clearClipsButton.FlatStyle = FlatStyle.Flat;
        _clearClipsButton.Font = new Font("Yu Gothic UI", 10F);
        _clearClipsButton.ForeColor = Color.FromArgb(235, 241, 248);
        _clearClipsButton.Location = new Point(253, 3);
        _clearClipsButton.Name = "_clearClipsButton";
        _clearClipsButton.Padding = new Padding(12, 0, 12, 0);
        _clearClipsButton.Size = new Size(72, 34);
        _clearClipsButton.TabIndex = 3;
        _clearClipsButton.Text = "クリア";
        _clearClipsButton.UseVisualStyleBackColor = false;
        // 
        // _exportJoinedClipsButton
        // 
        _exportJoinedClipsButton.BackColor = Color.FromArgb(20, 184, 166);
        _exportJoinedClipsButton.FlatStyle = FlatStyle.Flat;
        _exportJoinedClipsButton.Font = new Font("Yu Gothic UI", 10F);
        _exportJoinedClipsButton.ForeColor = Color.White;
        _exportJoinedClipsButton.Location = new Point(331, 3);
        _exportJoinedClipsButton.Name = "_exportJoinedClipsButton";
        _exportJoinedClipsButton.Padding = new Padding(12, 0, 12, 0);
        _exportJoinedClipsButton.Size = new Size(96, 34);
        _exportJoinedClipsButton.TabIndex = 4;
        _exportJoinedClipsButton.Text = "連結出力";
        _exportJoinedClipsButton.UseVisualStyleBackColor = false;
        // 
        // _exportSeparateClipsButton
        // 
        _exportSeparateClipsButton.BackColor = Color.FromArgb(28, 37, 52);
        _exportSeparateClipsButton.FlatStyle = FlatStyle.Flat;
        _exportSeparateClipsButton.Font = new Font("Yu Gothic UI", 10F);
        _exportSeparateClipsButton.ForeColor = Color.FromArgb(235, 241, 248);
        _exportSeparateClipsButton.Location = new Point(433, 3);
        _exportSeparateClipsButton.Name = "_exportSeparateClipsButton";
        _exportSeparateClipsButton.Padding = new Padding(12, 0, 12, 0);
        _exportSeparateClipsButton.Size = new Size(96, 34);
        _exportSeparateClipsButton.TabIndex = 5;
        _exportSeparateClipsButton.Text = "個別出力";
        _exportSeparateClipsButton.UseVisualStyleBackColor = false;
        // 
        // _clipRangeLabel
        // 
        _clipRangeLabel.ForeColor = Color.FromArgb(148, 163, 184);
        _clipRangeLabel.Location = new Point(535, 0);
        _clipRangeLabel.Name = "_clipRangeLabel";
        _clipRangeLabel.Size = new Size(160, 40);
        _clipRangeLabel.TabIndex = 6;
        _clipRangeLabel.Text = "始点 --:-- / 終点 --:--";
        _clipRangeLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _clipListBox
        // 
        _clipListBox.BackColor = Color.FromArgb(15, 23, 36);
        _clipListBox.BorderStyle = BorderStyle.FixedSingle;
        _clipListBox.ForeColor = Color.FromArgb(235, 241, 248);
        _clipListBox.Location = new Point(701, 3);
        _clipListBox.Name = "_clipListBox";
        _clipListBox.Size = new Size(260, 62);
        _clipListBox.TabIndex = 7;
        // 
        // otherPresetsLabel
        // 
        otherPresetsLabel.AutoSize = true;
        otherPresetsLabel.ForeColor = Color.FromArgb(148, 163, 184);
        otherPresetsLabel.Location = new Point(3, 8);
        otherPresetsLabel.Margin = new Padding(3, 8, 4, 0);
        otherPresetsLabel.Name = "otherPresetsLabel";
        otherPresetsLabel.Size = new Size(38, 15);
        otherPresetsLabel.TabIndex = 0;
        otherPresetsLabel.Text = "その他";
        // 
        // _toggleImageControlButton
        // 
        _toggleImageControlButton.BackColor = Color.FromArgb(28, 37, 52);
        _toggleImageControlButton.FlatStyle = FlatStyle.Flat;
        _toggleImageControlButton.Font = new Font("Yu Gothic UI", 10F);
        _toggleImageControlButton.ForeColor = Color.FromArgb(235, 241, 248);
        _toggleImageControlButton.Location = new Point(3, 208);
        _toggleImageControlButton.Name = "_toggleImageControlButton";
        _toggleImageControlButton.Padding = new Padding(12, 0, 12, 0);
        _toggleImageControlButton.Size = new Size(294, 34);
        _toggleImageControlButton.TabIndex = 1;
        _toggleImageControlButton.Text = "画像調整を表示 ▼";
        _toggleImageControlButton.UseVisualStyleBackColor = false;
        // 
        // _togglePresetToolsButton
        // 
        _togglePresetToolsButton.BackColor = Color.FromArgb(28, 37, 52);
        _togglePresetToolsButton.FlatStyle = FlatStyle.Flat;
        _togglePresetToolsButton.Font = new Font("Yu Gothic UI", 10F);
        _togglePresetToolsButton.ForeColor = Color.FromArgb(235, 241, 248);
        _togglePresetToolsButton.Location = new Point(3, 599);
        _togglePresetToolsButton.Name = "_togglePresetToolsButton";
        _togglePresetToolsButton.Padding = new Padding(12, 0, 12, 0);
        _togglePresetToolsButton.Size = new Size(294, 34);
        _togglePresetToolsButton.TabIndex = 3;
        _togglePresetToolsButton.Text = "プリセット操作を表示 ▼";
        _togglePresetToolsButton.UseVisualStyleBackColor = false;
        // 
        // titlePanel
        // 
        titlePanel.AutoSize = true;
        titlePanel.Controls.Add(titleLabel);
        titlePanel.Controls.Add(versionLabel);
        titlePanel.Controls.Add(subtitleLabel);
        titlePanel.Location = new Point(0, 0);
        titlePanel.Margin = new Padding(0);
        titlePanel.Name = "titlePanel";
        titlePanel.Size = new Size(364, 37);
        titlePanel.TabIndex = 0;
        titlePanel.WrapContents = false;
        // 
        // titleLabel
        // 
        titleLabel.AutoSize = true;
        titleLabel.Font = new Font("Yu Gothic UI", 20F, FontStyle.Bold);
        titleLabel.ForeColor = Color.FromArgb(235, 241, 248);
        titleLabel.Location = new Point(0, 0);
        titleLabel.Margin = new Padding(0, 0, 6, 0);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(157, 37);
        titleLabel.TabIndex = 0;
        titleLabel.Text = "ENTcapture";
        // 
        // versionLabel
        // 
        versionLabel.AutoSize = true;
        versionLabel.Font = new Font("Yu Gothic UI", 20F, FontStyle.Bold);
        versionLabel.ForeColor = Color.FromArgb(45, 212, 191);
        versionLabel.Location = new Point(163, 0);
        versionLabel.Margin = new Padding(0);
        versionLabel.Name = "versionLabel";
        versionLabel.Size = new Size(32, 37);
        versionLabel.TabIndex = 1;
        versionLabel.Text = "2";
        // 
        // subtitleLabel
        // 
        subtitleLabel.AutoSize = true;
        subtitleLabel.Font = new Font("Yu Gothic UI", 9F);
        subtitleLabel.ForeColor = Color.FromArgb(148, 163, 184);
        subtitleLabel.Location = new Point(203, 9);
        subtitleLabel.Margin = new Padding(8, 9, 0, 0);
        subtitleLabel.Name = "subtitleLabel";
        subtitleLabel.Size = new Size(161, 15);
        subtitleLabel.TabIndex = 2;
        subtitleLabel.Text = "NEXT GENERATION CAPTURE";
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(12, 17, 26);
        ClientSize = new Size(1380, 860);
        Controls.Add(rootLayout);
        DoubleBuffered = true;
        ForeColor = Color.FromArgb(235, 241, 248);
        Icon = (Icon)resources.GetObject("$this.Icon");
        MinimumSize = new Size(1080, 700);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "ENTcapture2";
        rootLayout.ResumeLayout(false);
        rootLayout.PerformLayout();
        headerLayout.ResumeLayout(false);
        headerLayout.PerformLayout();
        patientPanel.ResumeLayout(false);
        patientPanel.PerformLayout();
        primaryActionPanel.ResumeLayout(false);
        _quickPresetPanel.ResumeLayout(false);
        workspaceLayout.ResumeLayout(false);
        previewSurfacePanel.ResumeLayout(false);
        previewSurfacePanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_previewBox).EndInit();
        _thumbnailStripPanel.ResumeLayout(false);
        playbackPanel.ResumeLayout(false);
        playbackPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_playbackTrackBar).EndInit();
        sidePanel.ResumeLayout(false);
        sidePanel.PerformLayout();
        sourceCard.ResumeLayout(false);
        sourceCard.PerformLayout();
        sourceButtonPanel.ResumeLayout(false);
        filterCard.ResumeLayout(false);
        filterCard.PerformLayout();
        whiteBalancePanel.ResumeLayout(false);
        whiteBalancePanel.PerformLayout();
        redRow.ResumeLayout(false);
        redRow.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_redTrack).EndInit();
        greenRow.ResumeLayout(false);
        greenRow.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_greenTrack).EndInit();
        blueRow.ResumeLayout(false);
        blueRow.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_blueTrack).EndInit();
        gammaRow.ResumeLayout(false);
        gammaRow.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_gammaTrack).EndInit();
        flipPanel.ResumeLayout(false);
        flipPanel.PerformLayout();
        captureCard.ResumeLayout(false);
        captureCard.PerformLayout();
        footerLayout.ResumeLayout(false);
        footerLayout.PerformLayout();
        _clipEditorPanel.ResumeLayout(false);
        titlePanel.ResumeLayout(false);
        titlePanel.PerformLayout();
        ResumeLayout(false);
    }

}

#pragma warning restore CS8600
