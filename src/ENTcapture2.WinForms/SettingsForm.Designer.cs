#nullable disable

namespace ENTcapture2.WinForms;

public partial class SettingsForm
{
    private TableLayoutPanel rootLayout = null!;
    private FlowLayoutPanel navigationPanel = null!;
    private Button _generalPageButton = null!;
    private Button _presetsPageButton = null!;
    private Panel contentPanel = null!;
    private TabControl settingsTabControl = null!;
    private TabPage generalTabPage = null!;
    private TabPage presetsTabPage = null!;
    private Panel generalPagePanel = null!;
    private TableLayoutPanel generalLayout = null!;
    private Panel snapshotCardPanel = null!;
    private TableLayoutPanel snapshotCardLayout = null!;
    private Label snapshotCardTitleLabel = null!;
    private Label snapshotDirectoryLabel = null!;
    private TextBox _snapshotDirectoryTextBox = null!;
    private Button _browseDirectoryButton = null!;
    private Label jpegQualityLabel = null!;
    private NumericUpDown _jpegQualityInput = null!;
    private Label snapshotDebounceLabel = null!;
    private NumericUpDown _snapshotDebounceInput = null!;
    private Label captureModeLabel = null!;
    private FlowLayoutPanel captureModePanel = null!;
    private RadioButton _continuousRecordingRadioButton = null!;
    private RadioButton _previewOnlyRadioButton = null!;
    private CheckBox _topMostDuringPreviewCheckBox = null!;
    private Panel temporaryCardPanel = null!;
    private TableLayoutPanel temporaryCardLayout = null!;
    private Label temporaryCardTitleLabel = null!;
    private Label temporaryDirectoryLabel = null!;
    private TextBox _temporaryDirectoryTextBox = null!;
    private Button _browseTemporaryDirectoryButton = null!;
    private Label temporaryCodecLabel = null!;
    private ComboBox _temporaryCodecComboBox = null!;
    private Label h264EncoderLabel = null!;
    private ComboBox _h264EncoderComboBox = null!;
    private Label segmentMinutesLabel = null!;
    private NumericUpDown _segmentMinutesInput = null!;
    private Label retentionDaysLabel = null!;
    private NumericUpDown _retentionDaysInput = null!;
    private Label maximumFpsLabel = null!;
    private NumericUpDown _maximumFpsInput = null!;
    private Label examinationTypesLabel = null!;
    private TextBox _examinationTypesTextBox = null!;
    private Panel finalVideoCardPanel = null!;
    private TableLayoutPanel finalVideoCardLayout = null!;
    private Label finalVideoCardTitleLabel = null!;
    private Label reencodeThresholdLabel = null!;
    private NumericUpDown _reencodeThresholdInput = null!;
    private Label finalCodecLabel = null!;
    private ComboBox _finalCodecComboBox = null!;
    private Label finalQualityLabel = null!;
    private ComboBox _finalQualityComboBox = null!;
    private Panel hotkeyCardPanel = null!;
    private TableLayoutPanel hotkeyCardLayout = null!;
    private Label hotkeyCardTitleLabel = null!;
    private Panel integrationCardPanel = null!;
    private TableLayoutPanel integrationLayout = null!;
    private Label integrationTitleLabel = null!;
    private Label snapshotHotkeyLabel = null!;
    private TextBox _snapshotHotkeyTextBox = null!;
    private Label captureHotkeyLabel = null!;
    private TextBox _captureHotkeyTextBox = null!;
    private Label rsBaseDirectoryLabel = null!;
    private TextBox _rsBaseDirectoryTextBox = null!;
    private Button _browseRsBaseDirectoryButton = null!;
    private Label rsBaseReloadUrlLabel = null!;
    private TextBox _rsBaseReloadUrlTextBox = null!;
    private CheckBox _autoFileCheckBox = null!;
    private Panel importCardPanel = null!;
    private TableLayoutPanel importLayout = null!;
    private Label importTitleLabel = null!;
    private Label importDescriptionLabel = null!;
    private FlowLayoutPanel _settingsTransferPanel = null!;
    private Button _exportSettingsButton = null!;
    private Button _importSettingsButton = null!;
    private Button _importLegacyButton = null!;
    private Panel presetsPagePanel = null!;
    private TableLayoutPanel presetLayout = null!;
    private TableLayoutPanel presetListLayout = null!;
    private ListBox _presetList = null!;
    private FlowLayoutPanel presetButtonsPanel = null!;
    private Button _addPresetButton = null!;
    private Button _duplicatePresetButton = null!;
    private Button _deletePresetButton = null!;
    private Button _moveUpButton = null!;
    private Button _moveDownButton = null!;
    private Panel presetEditorPanel = null!;
    private TableLayoutPanel editorLayout = null!;
    private Label presetNameLabel = null!;
    private TextBox _nameTextBox = null!;
    private Label examinationLabel = null!;
    private TextBox _examinationTextBox = null!;
    private Label deviceLabel = null!;
    private ComboBox _deviceComboBox = null!;
    private Label resolutionLabel = null!;
    private ComboBox _resolutionComboBox = null!;
    private Label fpsLabel = null!;
    private NumericUpDown _fpsInput = null!;
    private FlowLayoutPanel presetOptionsPanel = null!;
    private CheckBox _videoCheckBox = null!;
    private CheckBox _presetPreviewOnlyCheckBox = null!;
    private Panel roiCardPanel = null!;
    private TableLayoutPanel roiLayout = null!;
    private Label roiTitleLabel = null!;
    private FlowLayoutPanel roiFlowPanel = null!;
    private Label roiX1Label = null!;
    private NumericUpDown _roiX1Input = null!;
    private Label roiY1Label = null!;
    private NumericUpDown _roiY1Input = null!;
    private Label roiX2Label = null!;
    private NumericUpDown _roiX2Input = null!;
    private Label roiY2Label = null!;
    private NumericUpDown _roiY2Input = null!;
    private Label roiHintLabel = null!;
    private Label fontLabel = null!;
    private ComboBox _fontComboBox = null!;
    private Label overlayLabel = null!;
    private DataGridView _overlayGrid = null!;
    private DataGridViewTextBoxColumn overlayXColumn = null!;
    private DataGridViewTextBoxColumn overlayYColumn = null!;
    private DataGridViewTextBoxColumn overlaySizeColumn = null!;
    private DataGridViewTextBoxColumn overlayColorColumn = null!;
    private DataGridViewTextBoxColumn overlayTextColumn = null!;
    private Label overlayRawLabel = null!;
    private TextBox _overlayRawTextBox = null!;
    private Label overlayHintLabel = null!;
    private FlowLayoutPanel footerPanel = null!;
    private Button _okButton = null!;
    private Button _cancelButton = null!;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
        rootLayout = new TableLayoutPanel();
        navigationPanel = new FlowLayoutPanel();
        _generalPageButton = new Button();
        _presetsPageButton = new Button();
        contentPanel = new Panel();
        settingsTabControl = new TabControl();
        generalTabPage = new TabPage();
        generalPagePanel = new Panel();
        generalLayout = new TableLayoutPanel();
        snapshotCardPanel = new Panel();
        snapshotCardLayout = new TableLayoutPanel();
        snapshotCardTitleLabel = new Label();
        snapshotDirectoryLabel = new Label();
        _snapshotDirectoryTextBox = new TextBox();
        _browseDirectoryButton = new Button();
        jpegQualityLabel = new Label();
        _jpegQualityInput = new NumericUpDown();
        captureModeLabel = new Label();
        captureModePanel = new FlowLayoutPanel();
        _topMostDuringPreviewCheckBox = new CheckBox();
        examinationTypesLabel = new Label();
        _examinationTypesTextBox = new TextBox();
        finalVideoCardPanel = new Panel();
        finalVideoCardLayout = new TableLayoutPanel();
        finalVideoCardTitleLabel = new Label();
        reencodeThresholdLabel = new Label();
        _reencodeThresholdInput = new NumericUpDown();
        finalCodecLabel = new Label();
        _finalCodecComboBox = new ComboBox();
        finalQualityLabel = new Label();
        _finalQualityComboBox = new ComboBox();
        temporaryCardPanel = new Panel();
        temporaryCardLayout = new TableLayoutPanel();
        temporaryCardTitleLabel = new Label();
        temporaryDirectoryLabel = new Label();
        _temporaryDirectoryTextBox = new TextBox();
        _browseTemporaryDirectoryButton = new Button();
        temporaryCodecLabel = new Label();
        _temporaryCodecComboBox = new ComboBox();
        h264EncoderLabel = new Label();
        _h264EncoderComboBox = new ComboBox();
        segmentMinutesLabel = new Label();
        retentionDaysLabel = new Label();
        _retentionDaysInput = new NumericUpDown();
        _maximumFpsInput = new NumericUpDown();
        maximumFpsLabel = new Label();
        _segmentMinutesInput = new NumericUpDown();
        label2 = new Label();
        label3 = new Label();
        label4 = new Label();
        hotkeyCardPanel = new Panel();
        hotkeyCardLayout = new TableLayoutPanel();
        hotkeyCardTitleLabel = new Label();
        snapshotDebounceLabel = new Label();
        _snapshotDebounceInput = new NumericUpDown();
        snapshotHotkeyLabel = new Label();
        captureHotkeyLabel = new Label();
        _captureHotkeyTextBox = new TextBox();
        _snapshotHotkeyTextBox = new TextBox();
        label1 = new Label();
        integrationCardPanel = new Panel();
        integrationLayout = new TableLayoutPanel();
        integrationTitleLabel = new Label();
        rsBaseDirectoryLabel = new Label();
        _rsBaseDirectoryTextBox = new TextBox();
        _browseRsBaseDirectoryButton = new Button();
        rsBaseReloadUrlLabel = new Label();
        _rsBaseReloadUrlTextBox = new TextBox();
        _autoFileCheckBox = new CheckBox();
        importCardPanel = new Panel();
        importLayout = new TableLayoutPanel();
        importTitleLabel = new Label();
        importDescriptionLabel = new Label();
        _settingsTransferPanel = new FlowLayoutPanel();
        _exportSettingsButton = new Button();
        _importSettingsButton = new Button();
        _importLegacyButton = new Button();
        presetsTabPage = new TabPage();
        presetsPagePanel = new Panel();
        presetLayout = new TableLayoutPanel();
        presetListLayout = new TableLayoutPanel();
        _presetList = new ListBox();
        presetButtonsPanel = new FlowLayoutPanel();
        _addPresetButton = new Button();
        _duplicatePresetButton = new Button();
        _deletePresetButton = new Button();
        _moveUpButton = new Button();
        _moveDownButton = new Button();
        presetEditorPanel = new Panel();
        editorLayout = new TableLayoutPanel();
        presetNameLabel = new Label();
        _nameTextBox = new TextBox();
        examinationLabel = new Label();
        _examinationTextBox = new TextBox();
        deviceLabel = new Label();
        _deviceComboBox = new ComboBox();
        resolutionLabel = new Label();
        _resolutionComboBox = new ComboBox();
        fpsLabel = new Label();
        _fpsInput = new NumericUpDown();
        presetOptionsPanel = new FlowLayoutPanel();
        _videoCheckBox = new CheckBox();
        _presetPreviewOnlyCheckBox = new CheckBox();
        roiCardPanel = new Panel();
        roiLayout = new TableLayoutPanel();
        roiFlowPanel = new FlowLayoutPanel();
        roiX1Label = new Label();
        _roiX1Input = new NumericUpDown();
        roiY1Label = new Label();
        _roiY1Input = new NumericUpDown();
        roiX2Label = new Label();
        _roiX2Input = new NumericUpDown();
        roiY2Label = new Label();
        _roiY2Input = new NumericUpDown();
        roiHintLabel = new Label();
        roiTitleLabel = new Label();
        fontLabel = new Label();
        _fontComboBox = new ComboBox();
        overlayLabel = new Label();
        _overlayGrid = new DataGridView();
        overlayXColumn = new DataGridViewTextBoxColumn();
        overlayYColumn = new DataGridViewTextBoxColumn();
        overlaySizeColumn = new DataGridViewTextBoxColumn();
        overlayColorColumn = new DataGridViewTextBoxColumn();
        overlayTextColumn = new DataGridViewTextBoxColumn();
        overlayRawLabel = new Label();
        _overlayRawTextBox = new TextBox();
        overlayHintLabel = new Label();
        footerPanel = new FlowLayoutPanel();
        _okButton = new Button();
        _cancelButton = new Button();
        _continuousRecordingRadioButton = new RadioButton();
        _previewOnlyRadioButton = new RadioButton();
        toolTip1 = new ToolTip(components);
        rootLayout.SuspendLayout();
        navigationPanel.SuspendLayout();
        contentPanel.SuspendLayout();
        settingsTabControl.SuspendLayout();
        generalTabPage.SuspendLayout();
        generalPagePanel.SuspendLayout();
        generalLayout.SuspendLayout();
        snapshotCardPanel.SuspendLayout();
        snapshotCardLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_jpegQualityInput).BeginInit();
        captureModePanel.SuspendLayout();
        finalVideoCardPanel.SuspendLayout();
        finalVideoCardLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_reencodeThresholdInput).BeginInit();
        temporaryCardPanel.SuspendLayout();
        temporaryCardLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_retentionDaysInput).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_maximumFpsInput).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_segmentMinutesInput).BeginInit();
        hotkeyCardPanel.SuspendLayout();
        hotkeyCardLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_snapshotDebounceInput).BeginInit();
        integrationCardPanel.SuspendLayout();
        integrationLayout.SuspendLayout();
        importCardPanel.SuspendLayout();
        importLayout.SuspendLayout();
        _settingsTransferPanel.SuspendLayout();
        presetsTabPage.SuspendLayout();
        presetsPagePanel.SuspendLayout();
        presetLayout.SuspendLayout();
        presetListLayout.SuspendLayout();
        presetButtonsPanel.SuspendLayout();
        presetEditorPanel.SuspendLayout();
        editorLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_fpsInput).BeginInit();
        presetOptionsPanel.SuspendLayout();
        roiCardPanel.SuspendLayout();
        roiLayout.SuspendLayout();
        roiFlowPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_roiX1Input).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_roiY1Input).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_roiX2Input).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_roiY2Input).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_overlayGrid).BeginInit();
        footerPanel.SuspendLayout();
        SuspendLayout();
        //
        // rootLayout
        //
        rootLayout.ColumnCount = 1;
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.Controls.Add(navigationPanel, 0, 0);
        rootLayout.Controls.Add(contentPanel, 0, 1);
        rootLayout.Controls.Add(footerPanel, 0, 2);
        rootLayout.Dock = DockStyle.Fill;
        rootLayout.Location = new Point(0, 0);
        rootLayout.Name = "rootLayout";
        rootLayout.Padding = new Padding(18);
        rootLayout.RowCount = 3;
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
        rootLayout.Size = new Size(1100, 881);
        rootLayout.TabIndex = 0;
        rootLayout.Tag = "window";
        //
        // navigationPanel
        //
        navigationPanel.Controls.Add(_generalPageButton);
        navigationPanel.Controls.Add(_presetsPageButton);
        navigationPanel.Dock = DockStyle.Fill;
        navigationPanel.Location = new Point(18, 18);
        navigationPanel.Margin = new Padding(0);
        navigationPanel.Name = "navigationPanel";
        navigationPanel.Size = new Size(1064, 1);
        navigationPanel.TabIndex = 0;
        navigationPanel.Tag = "window";
        navigationPanel.Visible = false;
        navigationPanel.WrapContents = false;
        //
        // _generalPageButton
        //
        _generalPageButton.Location = new Point(0, 0);
        _generalPageButton.Margin = new Padding(0, 0, 8, 8);
        _generalPageButton.Name = "_generalPageButton";
        _generalPageButton.Size = new Size(150, 38);
        _generalPageButton.TabIndex = 0;
        _generalPageButton.Text = "全般・移行";
        _generalPageButton.Click += NavigationButton_Click;
        //
        // _presetsPageButton
        //
        _presetsPageButton.Location = new Point(158, 0);
        _presetsPageButton.Margin = new Padding(0, 0, 8, 8);
        _presetsPageButton.Name = "_presetsPageButton";
        _presetsPageButton.Size = new Size(180, 38);
        _presetsPageButton.TabIndex = 1;
        _presetsPageButton.Text = "プリセット・画像加工";
        _presetsPageButton.Click += NavigationButton_Click;
        //
        // contentPanel
        //
        contentPanel.Controls.Add(settingsTabControl);
        contentPanel.Dock = DockStyle.Fill;
        contentPanel.Location = new Point(21, 21);
        contentPanel.Name = "contentPanel";
        contentPanel.Size = new Size(1058, 785);
        contentPanel.TabIndex = 1;
        //
        // settingsTabControl
        //
        settingsTabControl.Controls.Add(generalTabPage);
        settingsTabControl.Controls.Add(presetsTabPage);
        settingsTabControl.Dock = DockStyle.Fill;
        settingsTabControl.Location = new Point(0, 0);
        settingsTabControl.Name = "settingsTabControl";
        settingsTabControl.SelectedIndex = 0;
        settingsTabControl.Size = new Size(1058, 785);
        settingsTabControl.TabIndex = 0;
        settingsTabControl.Tag = "window";
        //
        // generalTabPage
        //
        generalTabPage.Controls.Add(generalPagePanel);
        generalTabPage.Location = new Point(4, 24);
        generalTabPage.Name = "generalTabPage";
        generalTabPage.Padding = new Padding(14);
        generalTabPage.Size = new Size(1050, 757);
        generalTabPage.TabIndex = 0;
        generalTabPage.Text = "全体設定";
        //
        // generalPagePanel
        //
        generalPagePanel.AutoScroll = true;
        generalPagePanel.Controls.Add(generalLayout);
        generalPagePanel.Dock = DockStyle.Fill;
        generalPagePanel.Location = new Point(14, 14);
        generalPagePanel.Name = "generalPagePanel";
        generalPagePanel.Size = new Size(1022, 729);
        generalPagePanel.TabIndex = 1;
        //
        // generalLayout
        //
        generalLayout.AutoSize = true;
        generalLayout.ColumnCount = 2;
        generalLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        generalLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        generalLayout.Controls.Add(snapshotCardPanel, 0, 1);
        generalLayout.Controls.Add(finalVideoCardPanel, 1, 1);
        generalLayout.Controls.Add(temporaryCardPanel, 0, 2);
        generalLayout.Controls.Add(hotkeyCardPanel, 1, 2);
        generalLayout.Controls.Add(integrationCardPanel, 0, 3);
        generalLayout.Controls.Add(importCardPanel, 1, 3);
        generalLayout.Dock = DockStyle.Top;
        generalLayout.Location = new Point(0, 0);
        generalLayout.Name = "generalLayout";
        generalLayout.RowCount = 4;
        generalLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 17F));
        generalLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 237F));
        generalLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 240F));
        generalLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 246F));
        generalLayout.Size = new Size(1005, 740);
        generalLayout.TabIndex = 0;
        //
        // snapshotCardPanel
        //
        snapshotCardPanel.Controls.Add(snapshotCardLayout);
        snapshotCardPanel.Dock = DockStyle.Fill;
        snapshotCardPanel.Location = new Point(0, 17);
        snapshotCardPanel.Margin = new Padding(0, 0, 10, 10);
        snapshotCardPanel.Name = "snapshotCardPanel";
        snapshotCardPanel.Padding = new Padding(14);
        snapshotCardPanel.Size = new Size(492, 227);
        snapshotCardPanel.TabIndex = 1;
        snapshotCardPanel.Tag = "raised";
        //
        // snapshotCardLayout
        //
        snapshotCardLayout.ColumnCount = 3;
        snapshotCardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        snapshotCardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        snapshotCardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86F));
        snapshotCardLayout.Controls.Add(snapshotCardTitleLabel, 0, 0);
        snapshotCardLayout.Controls.Add(snapshotDirectoryLabel, 0, 1);
        snapshotCardLayout.Controls.Add(_snapshotDirectoryTextBox, 1, 1);
        snapshotCardLayout.Controls.Add(_browseDirectoryButton, 2, 1);
        snapshotCardLayout.Controls.Add(jpegQualityLabel, 0, 2);
        snapshotCardLayout.Controls.Add(_jpegQualityInput, 1, 2);
        snapshotCardLayout.Controls.Add(captureModeLabel, 0, 3);
        snapshotCardLayout.Controls.Add(captureModePanel, 1, 3);
        snapshotCardLayout.Controls.Add(examinationTypesLabel, 0, 4);
        snapshotCardLayout.Controls.Add(_examinationTypesTextBox, 1, 4);
        snapshotCardLayout.Dock = DockStyle.Fill;
        snapshotCardLayout.Location = new Point(14, 14);
        snapshotCardLayout.Name = "snapshotCardLayout";
        snapshotCardLayout.RowCount = 5;
        snapshotCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        snapshotCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        snapshotCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 31F));
        snapshotCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 41F));
        snapshotCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
        snapshotCardLayout.Size = new Size(464, 199);
        snapshotCardLayout.TabIndex = 0;
        //
        // snapshotCardTitleLabel
        //
        snapshotCardTitleLabel.AutoSize = true;
        snapshotCardLayout.SetColumnSpan(snapshotCardTitleLabel, 3);
        snapshotCardTitleLabel.Font = new Font("Yu Gothic UI", 10F, FontStyle.Bold);
        snapshotCardTitleLabel.Location = new Point(3, 0);
        snapshotCardTitleLabel.Name = "snapshotCardTitleLabel";
        snapshotCardTitleLabel.Size = new Size(96, 19);
        snapshotCardTitleLabel.TabIndex = 0;
        snapshotCardTitleLabel.Tag = "accent";
        snapshotCardTitleLabel.Text = "保存・キャプチャ";
        //
        // snapshotDirectoryLabel
        //
        snapshotDirectoryLabel.Anchor = AnchorStyles.Left;
        snapshotDirectoryLabel.AutoSize = true;
        snapshotDirectoryLabel.Location = new Point(3, 34);
        snapshotDirectoryLabel.Name = "snapshotDirectoryLabel";
        snapshotDirectoryLabel.Size = new Size(86, 15);
        snapshotDirectoryLabel.TabIndex = 1;
        snapshotDirectoryLabel.Text = "保存先フォルダー";
        //
        // _snapshotDirectoryTextBox
        //
        _snapshotDirectoryTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _snapshotDirectoryTextBox.Location = new Point(130, 32);
        _snapshotDirectoryTextBox.Margin = new Padding(0, 8, 12, 8);
        _snapshotDirectoryTextBox.Name = "_snapshotDirectoryTextBox";
        _snapshotDirectoryTextBox.Size = new Size(236, 23);
        _snapshotDirectoryTextBox.TabIndex = 2;
        toolTip1.SetToolTip(_snapshotDirectoryTextBox, "RSBaseのgazouフォルダやRSAutoのフォルダを指定します");
        //
        // _browseDirectoryButton
        //
        _browseDirectoryButton.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _browseDirectoryButton.Location = new Point(378, 30);
        _browseDirectoryButton.Margin = new Padding(0, 5, 0, 5);
        _browseDirectoryButton.Name = "_browseDirectoryButton";
        _browseDirectoryButton.Size = new Size(86, 23);
        _browseDirectoryButton.TabIndex = 3;
        _browseDirectoryButton.Text = "参照...";
        _browseDirectoryButton.Click += BrowseDirectoryButton_Click;
        //
        // jpegQualityLabel
        //
        jpegQualityLabel.Anchor = AnchorStyles.Left;
        jpegQualityLabel.AutoSize = true;
        jpegQualityLabel.Location = new Point(3, 68);
        jpegQualityLabel.Name = "jpegQualityLabel";
        jpegQualityLabel.Size = new Size(56, 15);
        jpegQualityLabel.TabIndex = 4;
        jpegQualityLabel.Text = "JPEG品質";
        //
        // _jpegQualityInput
        //
        _jpegQualityInput.Anchor = AnchorStyles.Left;
        _jpegQualityInput.Location = new Point(133, 64);
        _jpegQualityInput.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        _jpegQualityInput.Name = "_jpegQualityInput";
        _jpegQualityInput.Size = new Size(120, 23);
        _jpegQualityInput.TabIndex = 5;
        _jpegQualityInput.Value = new decimal(new int[] { 95, 0, 0, 0 });
        //
        // captureModeLabel
        //
        captureModeLabel.Anchor = AnchorStyles.Left;
        captureModeLabel.AutoSize = true;
        captureModeLabel.Location = new Point(3, 104);
        captureModeLabel.Name = "captureModeLabel";
        captureModeLabel.Size = new Size(72, 15);
        captureModeLabel.TabIndex = 6;
        captureModeLabel.Text = "プレビュー動作";
        //
        // captureModePanel
        //
        snapshotCardLayout.SetColumnSpan(captureModePanel, 2);
        captureModePanel.Controls.Add(_topMostDuringPreviewCheckBox);
        captureModePanel.Dock = DockStyle.Fill;
        captureModePanel.Location = new Point(133, 94);
        captureModePanel.Name = "captureModePanel";
        captureModePanel.Size = new Size(328, 35);
        captureModePanel.TabIndex = 7;
        //
        // _topMostDuringPreviewCheckBox
        //
        _topMostDuringPreviewCheckBox.AutoSize = true;
        _topMostDuringPreviewCheckBox.Location = new Point(0, 10);
        _topMostDuringPreviewCheckBox.Margin = new Padding(0, 10, 0, 0);
        _topMostDuringPreviewCheckBox.Name = "_topMostDuringPreviewCheckBox";
        _topMostDuringPreviewCheckBox.Size = new Size(148, 19);
        _topMostDuringPreviewCheckBox.TabIndex = 2;
        _topMostDuringPreviewCheckBox.Text = "プレビュー時最前面に表示";
        //
        // examinationTypesLabel
        //
        examinationTypesLabel.Anchor = AnchorStyles.Left;
        examinationTypesLabel.AutoSize = true;
        examinationTypesLabel.Location = new Point(3, 150);
        examinationTypesLabel.Name = "examinationTypesLabel";
        examinationTypesLabel.Size = new Size(74, 30);
        examinationTypesLabel.TabIndex = 21;
        examinationTypesLabel.Text = "検査名一覧\r\n(カンマ区切り)";
        //
        // _examinationTypesTextBox
        //
        _examinationTypesTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        snapshotCardLayout.SetColumnSpan(_examinationTypesTextBox, 2);
        _examinationTypesTextBox.Location = new Point(130, 154);
        _examinationTypesTextBox.Margin = new Padding(0, 8, 12, 8);
        _examinationTypesTextBox.Name = "_examinationTypesTextBox";
        _examinationTypesTextBox.Size = new Size(322, 23);
        _examinationTypesTextBox.TabIndex = 22;
        //
        // finalVideoCardPanel
        //
        finalVideoCardPanel.Controls.Add(finalVideoCardLayout);
        finalVideoCardPanel.Dock = DockStyle.Fill;
        finalVideoCardPanel.Location = new Point(512, 17);
        finalVideoCardPanel.Margin = new Padding(10, 0, 0, 10);
        finalVideoCardPanel.Name = "finalVideoCardPanel";
        finalVideoCardPanel.Padding = new Padding(14);
        finalVideoCardPanel.Size = new Size(493, 227);
        finalVideoCardPanel.TabIndex = 3;
        finalVideoCardPanel.Tag = "raised";
        //
        // finalVideoCardLayout
        //
        finalVideoCardLayout.ColumnCount = 2;
        finalVideoCardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
        finalVideoCardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        finalVideoCardLayout.Controls.Add(finalVideoCardTitleLabel, 0, 0);
        finalVideoCardLayout.Controls.Add(reencodeThresholdLabel, 0, 1);
        finalVideoCardLayout.Controls.Add(_reencodeThresholdInput, 1, 1);
        finalVideoCardLayout.Controls.Add(finalCodecLabel, 0, 2);
        finalVideoCardLayout.Controls.Add(_finalCodecComboBox, 1, 2);
        finalVideoCardLayout.Controls.Add(finalQualityLabel, 0, 3);
        finalVideoCardLayout.Controls.Add(_finalQualityComboBox, 1, 3);
        finalVideoCardLayout.Dock = DockStyle.Fill;
        finalVideoCardLayout.Location = new Point(14, 14);
        finalVideoCardLayout.Name = "finalVideoCardLayout";
        finalVideoCardLayout.RowCount = 5;
        finalVideoCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        finalVideoCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        finalVideoCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        finalVideoCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        finalVideoCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        finalVideoCardLayout.Size = new Size(465, 199);
        finalVideoCardLayout.TabIndex = 0;
        //
        // finalVideoCardTitleLabel
        //
        finalVideoCardTitleLabel.AutoSize = true;
        finalVideoCardLayout.SetColumnSpan(finalVideoCardTitleLabel, 2);
        finalVideoCardTitleLabel.Font = new Font("Yu Gothic UI", 10F, FontStyle.Bold);
        finalVideoCardTitleLabel.Location = new Point(3, 0);
        finalVideoCardTitleLabel.Name = "finalVideoCardTitleLabel";
        finalVideoCardTitleLabel.Size = new Size(65, 19);
        finalVideoCardTitleLabel.TabIndex = 0;
        finalVideoCardTitleLabel.Tag = "accent";
        finalVideoCardTitleLabel.Text = "出力動画";
        //
        // reencodeThresholdLabel
        //
        reencodeThresholdLabel.Anchor = AnchorStyles.Left;
        reencodeThresholdLabel.AutoSize = true;
        reencodeThresholdLabel.Location = new Point(3, 41);
        reencodeThresholdLabel.Name = "reencodeThresholdLabel";
        reencodeThresholdLabel.Size = new Size(100, 15);
        reencodeThresholdLabel.TabIndex = 23;
        reencodeThresholdLabel.Text = "再圧縮しきい値MB";
        //
        // _reencodeThresholdInput
        //
        _reencodeThresholdInput.Location = new Point(153, 33);
        _reencodeThresholdInput.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
        _reencodeThresholdInput.Name = "_reencodeThresholdInput";
        _reencodeThresholdInput.Size = new Size(120, 23);
        _reencodeThresholdInput.TabIndex = 24;
        //
        // finalCodecLabel
        //
        finalCodecLabel.Anchor = AnchorStyles.Left;
        finalCodecLabel.AutoSize = true;
        finalCodecLabel.Location = new Point(3, 79);
        finalCodecLabel.Name = "finalCodecLabel";
        finalCodecLabel.Size = new Size(88, 15);
        finalCodecLabel.TabIndex = 25;
        finalCodecLabel.Text = "出力動画Codec";
        //
        // _finalCodecComboBox
        //
        _finalCodecComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _finalCodecComboBox.Items.AddRange(new object[] { "AUTO", "libopenh264", "h264_mf", "h264_nvenc", "h264_qsv", "h264_amf" });
        _finalCodecComboBox.Location = new Point(153, 71);
        _finalCodecComboBox.Name = "_finalCodecComboBox";
        _finalCodecComboBox.Size = new Size(180, 23);
        _finalCodecComboBox.TabIndex = 26;
        //
        // finalQualityLabel
        //
        finalQualityLabel.Anchor = AnchorStyles.Left;
        finalQualityLabel.AutoSize = true;
        finalQualityLabel.Location = new Point(3, 117);
        finalQualityLabel.Name = "finalQualityLabel";
        finalQualityLabel.Size = new Size(79, 15);
        finalQualityLabel.TabIndex = 27;
        finalQualityLabel.Text = "出力動画品質";
        //
        // _finalQualityComboBox
        //
        _finalQualityComboBox.Anchor = AnchorStyles.Left;
        _finalQualityComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _finalQualityComboBox.Items.AddRange(new object[] { "サイズ優先", "標準", "高品質", "最高品質" });
        _finalQualityComboBox.Location = new Point(153, 113);
        _finalQualityComboBox.Name = "_finalQualityComboBox";
        _finalQualityComboBox.Size = new Size(180, 23);
        _finalQualityComboBox.TabIndex = 28;
        //
        // temporaryCardPanel
        //
        temporaryCardPanel.Controls.Add(temporaryCardLayout);
        temporaryCardPanel.Dock = DockStyle.Fill;
        temporaryCardPanel.Location = new Point(0, 254);
        temporaryCardPanel.Margin = new Padding(0, 0, 10, 10);
        temporaryCardPanel.Name = "temporaryCardPanel";
        temporaryCardPanel.Padding = new Padding(14);
        temporaryCardPanel.Size = new Size(492, 230);
        temporaryCardPanel.TabIndex = 2;
        temporaryCardPanel.Tag = "raised";
        //
        // temporaryCardLayout
        //
        temporaryCardLayout.ColumnCount = 3;
        temporaryCardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        temporaryCardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        temporaryCardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86F));
        temporaryCardLayout.Controls.Add(temporaryCardTitleLabel, 0, 0);
        temporaryCardLayout.Controls.Add(temporaryDirectoryLabel, 0, 1);
        temporaryCardLayout.Controls.Add(_temporaryDirectoryTextBox, 1, 1);
        temporaryCardLayout.Controls.Add(_browseTemporaryDirectoryButton, 2, 1);
        temporaryCardLayout.Controls.Add(temporaryCodecLabel, 0, 2);
        temporaryCardLayout.Controls.Add(_temporaryCodecComboBox, 1, 2);
        temporaryCardLayout.Controls.Add(h264EncoderLabel, 0, 3);
        temporaryCardLayout.Controls.Add(_h264EncoderComboBox, 1, 3);
        temporaryCardLayout.Controls.Add(segmentMinutesLabel, 0, 4);
        temporaryCardLayout.Controls.Add(retentionDaysLabel, 0, 5);
        temporaryCardLayout.Controls.Add(_retentionDaysInput, 1, 5);
        temporaryCardLayout.Controls.Add(_maximumFpsInput, 1, 6);
        temporaryCardLayout.Controls.Add(maximumFpsLabel, 0, 6);
        temporaryCardLayout.Controls.Add(_segmentMinutesInput, 1, 4);
        temporaryCardLayout.Controls.Add(label2, 2, 4);
        temporaryCardLayout.Controls.Add(label3, 2, 5);
        temporaryCardLayout.Controls.Add(label4, 2, 6);
        temporaryCardLayout.Dock = DockStyle.Fill;
        temporaryCardLayout.Location = new Point(14, 14);
        temporaryCardLayout.Name = "temporaryCardLayout";
        temporaryCardLayout.RowCount = 8;
        temporaryCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 23F));
        temporaryCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        temporaryCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 27F));
        temporaryCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 27F));
        temporaryCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 27F));
        temporaryCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        temporaryCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        temporaryCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));
        temporaryCardLayout.Size = new Size(464, 202);
        temporaryCardLayout.TabIndex = 0;
        //
        // temporaryCardTitleLabel
        //
        temporaryCardTitleLabel.AutoSize = true;
        temporaryCardLayout.SetColumnSpan(temporaryCardTitleLabel, 3);
        temporaryCardTitleLabel.Font = new Font("Yu Gothic UI", 10F, FontStyle.Bold);
        temporaryCardTitleLabel.Location = new Point(3, 0);
        temporaryCardTitleLabel.Name = "temporaryCardTitleLabel";
        temporaryCardTitleLabel.Size = new Size(65, 19);
        temporaryCardTitleLabel.TabIndex = 0;
        temporaryCardTitleLabel.Tag = "accent";
        temporaryCardTitleLabel.Text = "一時録画";
        //
        // temporaryDirectoryLabel
        //
        temporaryDirectoryLabel.Anchor = AnchorStyles.Left;
        temporaryDirectoryLabel.AutoSize = true;
        temporaryDirectoryLabel.Location = new Point(3, 33);
        temporaryDirectoryLabel.Name = "temporaryDirectoryLabel";
        temporaryDirectoryLabel.Size = new Size(91, 15);
        temporaryDirectoryLabel.TabIndex = 8;
        temporaryDirectoryLabel.Text = "一時動画保存先";
        //
        // _temporaryDirectoryTextBox
        //
        _temporaryDirectoryTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _temporaryDirectoryTextBox.Location = new Point(130, 31);
        _temporaryDirectoryTextBox.Margin = new Padding(0, 8, 12, 8);
        _temporaryDirectoryTextBox.Name = "_temporaryDirectoryTextBox";
        _temporaryDirectoryTextBox.Size = new Size(236, 23);
        _temporaryDirectoryTextBox.TabIndex = 9;
        //
        // _browseTemporaryDirectoryButton
        //
        _browseTemporaryDirectoryButton.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _browseTemporaryDirectoryButton.Location = new Point(378, 29);
        _browseTemporaryDirectoryButton.Margin = new Padding(0, 5, 0, 5);
        _browseTemporaryDirectoryButton.Name = "_browseTemporaryDirectoryButton";
        _browseTemporaryDirectoryButton.Size = new Size(86, 23);
        _browseTemporaryDirectoryButton.TabIndex = 10;
        _browseTemporaryDirectoryButton.Text = "参照...";
        _browseTemporaryDirectoryButton.Click += BrowseTemporaryDirectoryButton_Click;
        //
        // temporaryCodecLabel
        //
        temporaryCodecLabel.Anchor = AnchorStyles.Left;
        temporaryCodecLabel.AutoSize = true;
        temporaryCodecLabel.Location = new Point(3, 65);
        temporaryCodecLabel.Name = "temporaryCodecLabel";
        temporaryCodecLabel.Size = new Size(88, 15);
        temporaryCodecLabel.TabIndex = 11;
        temporaryCodecLabel.Text = "一時動画Codec";
        //
        // _temporaryCodecComboBox
        //
        _temporaryCodecComboBox.Anchor = AnchorStyles.Left;
        _temporaryCodecComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _temporaryCodecComboBox.Items.AddRange(new object[] { "MJPG", "H264", "RAW" });
        _temporaryCodecComboBox.Location = new Point(133, 62);
        _temporaryCodecComboBox.Name = "_temporaryCodecComboBox";
        _temporaryCodecComboBox.Size = new Size(180, 23);
        _temporaryCodecComboBox.TabIndex = 12;
        //
        // h264EncoderLabel
        //
        h264EncoderLabel.Anchor = AnchorStyles.Left;
        h264EncoderLabel.AutoSize = true;
        h264EncoderLabel.Location = new Point(3, 92);
        h264EncoderLabel.Name = "h264EncoderLabel";
        h264EncoderLabel.Size = new Size(88, 15);
        h264EncoderLabel.TabIndex = 13;
        h264EncoderLabel.Text = "H.264エンコーダー";
        //
        // _h264EncoderComboBox
        //
        _h264EncoderComboBox.Anchor = AnchorStyles.Left;
        _h264EncoderComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _h264EncoderComboBox.Items.AddRange(new object[] { "自動", "NVENC", "AMF", "QSV" });
        _h264EncoderComboBox.Location = new Point(133, 89);
        _h264EncoderComboBox.Name = "_h264EncoderComboBox";
        _h264EncoderComboBox.Size = new Size(180, 23);
        _h264EncoderComboBox.TabIndex = 14;
        //
        // segmentMinutesLabel
        //
        segmentMinutesLabel.Anchor = AnchorStyles.Left;
        segmentMinutesLabel.AutoSize = true;
        segmentMinutesLabel.Location = new Point(3, 119);
        segmentMinutesLabel.Name = "segmentMinutesLabel";
        segmentMinutesLabel.Size = new Size(91, 15);
        segmentMinutesLabel.TabIndex = 15;
        segmentMinutesLabel.Text = "分割時間（分）";
        //
        // retentionDaysLabel
        //
        retentionDaysLabel.Anchor = AnchorStyles.Left;
        retentionDaysLabel.AutoSize = true;
        retentionDaysLabel.Location = new Point(3, 146);
        retentionDaysLabel.Name = "retentionDaysLabel";
        retentionDaysLabel.Size = new Size(103, 15);
        retentionDaysLabel.TabIndex = 17;
        retentionDaysLabel.Text = "一次動画保持日数";
        //
        // _retentionDaysInput
        //
        _retentionDaysInput.Location = new Point(133, 143);
        _retentionDaysInput.Maximum = new decimal(new int[] { 3650, 0, 0, 0 });
        _retentionDaysInput.Name = "_retentionDaysInput";
        _retentionDaysInput.Size = new Size(120, 23);
        _retentionDaysInput.TabIndex = 18;
        //
        // _maximumFpsInput
        //
        _maximumFpsInput.Location = new Point(133, 171);
        _maximumFpsInput.Maximum = new decimal(new int[] { 240, 0, 0, 0 });
        _maximumFpsInput.Name = "_maximumFpsInput";
        _maximumFpsInput.Size = new Size(120, 23);
        _maximumFpsInput.TabIndex = 20;
        //
        // maximumFpsLabel
        //
        maximumFpsLabel.Anchor = AnchorStyles.Left;
        maximumFpsLabel.AutoSize = true;
        maximumFpsLabel.Location = new Point(3, 174);
        maximumFpsLabel.Name = "maximumFpsLabel";
        maximumFpsLabel.Size = new Size(50, 15);
        maximumFpsLabel.TabIndex = 19;
        maximumFpsLabel.Text = "上限FPS";
        //
        // _segmentMinutesInput
        //
        _segmentMinutesInput.Location = new Point(133, 116);
        _segmentMinutesInput.Maximum = new decimal(new int[] { 240, 0, 0, 0 });
        _segmentMinutesInput.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        _segmentMinutesInput.Name = "_segmentMinutesInput";
        _segmentMinutesInput.Size = new Size(120, 23);
        _segmentMinutesInput.TabIndex = 16;
        _segmentMinutesInput.Value = new decimal(new int[] { 10, 0, 0, 0 });
        //
        // label2
        //
        label2.Anchor = AnchorStyles.Left;
        label2.AutoSize = true;
        label2.Location = new Point(381, 119);
        label2.Name = "label2";
        label2.Size = new Size(0, 15);
        label2.TabIndex = 21;
        //
        // label3
        //
        label3.Anchor = AnchorStyles.Left;
        label3.AutoSize = true;
        label3.Location = new Point(381, 146);
        label3.Name = "label3";
        label3.Size = new Size(67, 15);
        label3.TabIndex = 22;
        label3.Text = "0で削除無し";
        //
        // label4
        //
        label4.Anchor = AnchorStyles.Left;
        label4.AutoSize = true;
        label4.Location = new Point(381, 174);
        label4.Name = "label4";
        label4.Size = new Size(59, 15);
        label4.TabIndex = 23;
        label4.Text = "0で無制限";
        //
        // hotkeyCardPanel
        //
        hotkeyCardPanel.Controls.Add(hotkeyCardLayout);
        hotkeyCardPanel.Dock = DockStyle.Fill;
        hotkeyCardPanel.Location = new Point(512, 254);
        hotkeyCardPanel.Margin = new Padding(10, 0, 0, 10);
        hotkeyCardPanel.Name = "hotkeyCardPanel";
        hotkeyCardPanel.Padding = new Padding(14);
        hotkeyCardPanel.Size = new Size(493, 230);
        hotkeyCardPanel.TabIndex = 4;
        hotkeyCardPanel.Tag = "raised";
        //
        // hotkeyCardLayout
        //
        hotkeyCardLayout.ColumnCount = 2;
        hotkeyCardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
        hotkeyCardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        hotkeyCardLayout.Controls.Add(hotkeyCardTitleLabel, 0, 0);
        hotkeyCardLayout.Controls.Add(snapshotDebounceLabel, 0, 1);
        hotkeyCardLayout.Controls.Add(_snapshotDebounceInput, 1, 1);
        hotkeyCardLayout.Controls.Add(snapshotHotkeyLabel, 0, 2);
        hotkeyCardLayout.Controls.Add(captureHotkeyLabel, 0, 3);
        hotkeyCardLayout.Controls.Add(_captureHotkeyTextBox, 1, 3);
        hotkeyCardLayout.Controls.Add(_snapshotHotkeyTextBox, 1, 2);
        hotkeyCardLayout.Controls.Add(label1, 1, 4);
        hotkeyCardLayout.Dock = DockStyle.Fill;
        hotkeyCardLayout.Location = new Point(14, 14);
        hotkeyCardLayout.Name = "hotkeyCardLayout";
        hotkeyCardLayout.RowCount = 5;
        hotkeyCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        hotkeyCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        hotkeyCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        hotkeyCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        hotkeyCardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        hotkeyCardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 51F));
        hotkeyCardLayout.Size = new Size(465, 202);
        hotkeyCardLayout.TabIndex = 0;
        //
        // hotkeyCardTitleLabel
        //
        hotkeyCardTitleLabel.AutoSize = true;
        hotkeyCardLayout.SetColumnSpan(hotkeyCardTitleLabel, 2);
        hotkeyCardTitleLabel.Font = new Font("Yu Gothic UI", 10F, FontStyle.Bold);
        hotkeyCardTitleLabel.Location = new Point(3, 0);
        hotkeyCardTitleLabel.Name = "hotkeyCardTitleLabel";
        hotkeyCardTitleLabel.Size = new Size(140, 19);
        hotkeyCardTitleLabel.TabIndex = 0;
        hotkeyCardTitleLabel.Tag = "accent";
        hotkeyCardTitleLabel.Text = "ホットキー・フットスイッチ";
        //
        // snapshotDebounceLabel
        //
        snapshotDebounceLabel.Anchor = AnchorStyles.Left;
        snapshotDebounceLabel.AutoSize = true;
        snapshotDebounceLabel.Location = new Point(3, 41);
        snapshotDebounceLabel.Name = "snapshotDebounceLabel";
        snapshotDebounceLabel.Size = new Size(114, 15);
        snapshotDebounceLabel.TabIndex = 29;
        snapshotDebounceLabel.Text = "静止画連写抑制(ms)";
        //
        // _snapshotDebounceInput
        //
        _snapshotDebounceInput.Anchor = AnchorStyles.Left;
        _snapshotDebounceInput.Increment = new decimal(new int[] { 100, 0, 0, 0 });
        _snapshotDebounceInput.Location = new Point(153, 37);
        _snapshotDebounceInput.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
        _snapshotDebounceInput.Name = "_snapshotDebounceInput";
        _snapshotDebounceInput.Size = new Size(120, 23);
        _snapshotDebounceInput.TabIndex = 30;
        _snapshotDebounceInput.Value = new decimal(new int[] { 800, 0, 0, 0 });
        //
        // snapshotHotkeyLabel
        //
        snapshotHotkeyLabel.Anchor = AnchorStyles.Left;
        snapshotHotkeyLabel.AutoSize = true;
        snapshotHotkeyLabel.Location = new Point(3, 79);
        snapshotHotkeyLabel.Name = "snapshotHotkeyLabel";
        snapshotHotkeyLabel.Size = new Size(86, 15);
        snapshotHotkeyLabel.TabIndex = 31;
        snapshotHotkeyLabel.Text = "静止画ホットキー";
        //
        // captureHotkeyLabel
        //
        captureHotkeyLabel.Anchor = AnchorStyles.Left;
        captureHotkeyLabel.AutoSize = true;
        captureHotkeyLabel.Location = new Point(3, 117);
        captureHotkeyLabel.Name = "captureHotkeyLabel";
        captureHotkeyLabel.Size = new Size(101, 15);
        captureHotkeyLabel.TabIndex = 33;
        captureHotkeyLabel.Text = "取込開始/終了キー";
        //
        // _captureHotkeyTextBox
        //
        _captureHotkeyTextBox.Anchor = AnchorStyles.Left;
        _captureHotkeyTextBox.Location = new Point(150, 114);
        _captureHotkeyTextBox.Margin = new Padding(0, 8, 12, 8);
        _captureHotkeyTextBox.Name = "_captureHotkeyTextBox";
        _captureHotkeyTextBox.ReadOnly = true;
        _captureHotkeyTextBox.Size = new Size(183, 23);
        _captureHotkeyTextBox.TabIndex = 34;
        _captureHotkeyTextBox.KeyDown += HotkeyTextBox_KeyDown;
        //
        // _snapshotHotkeyTextBox
        //
        _snapshotHotkeyTextBox.Anchor = AnchorStyles.Left;
        _snapshotHotkeyTextBox.Location = new Point(150, 76);
        _snapshotHotkeyTextBox.Margin = new Padding(0, 8, 12, 8);
        _snapshotHotkeyTextBox.Name = "_snapshotHotkeyTextBox";
        _snapshotHotkeyTextBox.ReadOnly = true;
        _snapshotHotkeyTextBox.Size = new Size(183, 23);
        _snapshotHotkeyTextBox.TabIndex = 32;
        _snapshotHotkeyTextBox.KeyDown += HotkeyTextBox_KeyDown;
        //
        // label1
        //
        label1.AutoSize = true;
        label1.Location = new Point(153, 144);
        label1.Name = "label1";
        label1.Size = new Size(204, 15);
        label1.TabIndex = 37;
        label1.Text = "上のボックスを選択してキーを推してください";
        //
        // integrationCardPanel
        //
        integrationCardPanel.Controls.Add(integrationLayout);
        integrationCardPanel.Dock = DockStyle.Fill;
        integrationCardPanel.Location = new Point(0, 494);
        integrationCardPanel.Margin = new Padding(0, 0, 10, 0);
        integrationCardPanel.Name = "integrationCardPanel";
        integrationCardPanel.Padding = new Padding(18);
        integrationCardPanel.Size = new Size(492, 246);
        integrationCardPanel.TabIndex = 36;
        integrationCardPanel.Tag = "raised";
        //
        // integrationLayout
        //
        integrationLayout.ColumnCount = 3;
        integrationLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        integrationLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        integrationLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 81F));
        integrationLayout.Controls.Add(integrationTitleLabel, 0, 0);
        integrationLayout.Controls.Add(rsBaseDirectoryLabel, 0, 1);
        integrationLayout.Controls.Add(_rsBaseDirectoryTextBox, 1, 1);
        integrationLayout.Controls.Add(_browseRsBaseDirectoryButton, 2, 1);
        integrationLayout.Controls.Add(rsBaseReloadUrlLabel, 0, 2);
        integrationLayout.Controls.Add(_rsBaseReloadUrlTextBox, 1, 2);
        integrationLayout.Controls.Add(_autoFileCheckBox, 1, 3);
        integrationLayout.Dock = DockStyle.Fill;
        integrationLayout.Location = new Point(18, 18);
        integrationLayout.Name = "integrationLayout";
        integrationLayout.RowCount = 4;
        integrationLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 19F));
        integrationLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        integrationLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        integrationLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 59F));
        integrationLayout.Size = new Size(456, 210);
        integrationLayout.TabIndex = 0;
        //
        // integrationTitleLabel
        //
        integrationTitleLabel.AutoSize = true;
        integrationLayout.SetColumnSpan(integrationTitleLabel, 3);
        integrationTitleLabel.Font = new Font("Yu Gothic UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 128);
        integrationTitleLabel.Location = new Point(3, 0);
        integrationTitleLabel.Name = "integrationTitleLabel";
        integrationTitleLabel.Size = new Size(77, 17);
        integrationTitleLabel.TabIndex = 0;
        integrationTitleLabel.Tag = "accent";
        integrationTitleLabel.Text = "RSBase連携";
        //
        // rsBaseDirectoryLabel
        //
        rsBaseDirectoryLabel.Anchor = AnchorStyles.Left;
        rsBaseDirectoryLabel.AutoSize = true;
        rsBaseDirectoryLabel.Location = new Point(3, 28);
        rsBaseDirectoryLabel.Name = "rsBaseDirectoryLabel";
        rsBaseDirectoryLabel.Size = new Size(95, 15);
        rsBaseDirectoryLabel.TabIndex = 1;
        rsBaseDirectoryLabel.Text = "thept.txtフォルダー";
        //
        // _rsBaseDirectoryTextBox
        //
        _rsBaseDirectoryTextBox.Dock = DockStyle.Fill;
        _rsBaseDirectoryTextBox.Location = new Point(133, 22);
        _rsBaseDirectoryTextBox.Name = "_rsBaseDirectoryTextBox";
        _rsBaseDirectoryTextBox.Size = new Size(239, 23);
        _rsBaseDirectoryTextBox.TabIndex = 2;
        //
        // _browseRsBaseDirectoryButton
        //
        _browseRsBaseDirectoryButton.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _browseRsBaseDirectoryButton.Location = new Point(375, 25);
        _browseRsBaseDirectoryButton.Margin = new Padding(0, 6, 0, 6);
        _browseRsBaseDirectoryButton.Name = "_browseRsBaseDirectoryButton";
        _browseRsBaseDirectoryButton.Size = new Size(81, 22);
        _browseRsBaseDirectoryButton.TabIndex = 3;
        _browseRsBaseDirectoryButton.Text = "参照...";
        _browseRsBaseDirectoryButton.Click += BrowseRsBaseDirectoryButton_Click;
        //
        // rsBaseReloadUrlLabel
        //
        rsBaseReloadUrlLabel.Anchor = AnchorStyles.Left;
        rsBaseReloadUrlLabel.AutoSize = true;
        rsBaseReloadUrlLabel.Location = new Point(3, 61);
        rsBaseReloadUrlLabel.Name = "rsBaseReloadUrlLabel";
        rsBaseReloadUrlLabel.Size = new Size(89, 15);
        rsBaseReloadUrlLabel.TabIndex = 4;
        rsBaseReloadUrlLabel.Text = "RSBase取込URL";
        //
        // _rsBaseReloadUrlTextBox
        //
        _rsBaseReloadUrlTextBox.Dock = DockStyle.Fill;
        _rsBaseReloadUrlTextBox.Location = new Point(133, 56);
        _rsBaseReloadUrlTextBox.Name = "_rsBaseReloadUrlTextBox";
        _rsBaseReloadUrlTextBox.Size = new Size(239, 23);
        _rsBaseReloadUrlTextBox.TabIndex = 5;
        //
        // _autoFileCheckBox
        //
        _autoFileCheckBox.AutoSize = true;
        _autoFileCheckBox.Location = new Point(133, 88);
        _autoFileCheckBox.Name = "_autoFileCheckBox";
        _autoFileCheckBox.Size = new Size(160, 19);
        _autoFileCheckBox.TabIndex = 6;
        _autoFileCheckBox.Text = "録画終了後自動ファイリング";
        //
        // importCardPanel
        //
        importCardPanel.Controls.Add(importLayout);
        importCardPanel.Dock = DockStyle.Fill;
        importCardPanel.Location = new Point(512, 494);
        importCardPanel.Margin = new Padding(10, 0, 0, 0);
        importCardPanel.Name = "importCardPanel";
        importCardPanel.Padding = new Padding(18);
        importCardPanel.Size = new Size(493, 246);
        importCardPanel.TabIndex = 37;
        importCardPanel.Tag = "raised";
        //
        // importLayout
        //
        importLayout.ColumnCount = 1;
        importLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        importLayout.Controls.Add(importTitleLabel, 0, 0);
        importLayout.Controls.Add(importDescriptionLabel, 0, 1);
        importLayout.Controls.Add(_settingsTransferPanel, 0, 2);
        importLayout.Dock = DockStyle.Fill;
        importLayout.Location = new Point(18, 18);
        importLayout.Name = "importLayout";
        importLayout.RowCount = 3;
        importLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        importLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        importLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        importLayout.Size = new Size(457, 210);
        importLayout.TabIndex = 0;
        importLayout.Tag = "raised";
        //
        // importTitleLabel
        //
        importTitleLabel.AutoSize = true;
        importTitleLabel.Font = new Font("Yu Gothic UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 128);
        importTitleLabel.Location = new Point(3, 0);
        importTitleLabel.Name = "importTitleLabel";
        importTitleLabel.Size = new Size(120, 17);
        importTitleLabel.TabIndex = 0;
        importTitleLabel.Tag = "accent";
        importTitleLabel.Text = "ENTcapture V1設定";
        //
        // importDescriptionLabel
        //
        importDescriptionLabel.AutoSize = true;
        importDescriptionLabel.Location = new Point(3, 30);
        importDescriptionLabel.Name = "importDescriptionLabel";
        importDescriptionLabel.Size = new Size(448, 30);
        importDescriptionLabel.TabIndex = 1;
        importDescriptionLabel.Text = ".configから保存先、JPEG品質、6プリセット、カメラ、解像度、ROI、文字埋込み、画像補正を読み込みます。";
        //
        // _settingsTransferPanel
        //
        _settingsTransferPanel.Controls.Add(_exportSettingsButton);
        _settingsTransferPanel.Controls.Add(_importSettingsButton);
        _settingsTransferPanel.Controls.Add(_importLegacyButton);
        _settingsTransferPanel.Dock = DockStyle.Fill;
        _settingsTransferPanel.Location = new Point(0, 70);
        _settingsTransferPanel.Margin = new Padding(0);
        _settingsTransferPanel.Name = "_settingsTransferPanel";
        _settingsTransferPanel.Size = new Size(457, 140);
        _settingsTransferPanel.TabIndex = 2;
        _settingsTransferPanel.Tag = "raised";
        _settingsTransferPanel.WrapContents = false;
        //
        // _exportSettingsButton
        //
        _exportSettingsButton.Location = new Point(3, 3);
        _exportSettingsButton.Name = "_exportSettingsButton";
        _exportSettingsButton.Size = new Size(135, 38);
        _exportSettingsButton.TabIndex = 0;
        _exportSettingsButton.Tag = "accent";
        _exportSettingsButton.Text = "設定を書き出し";
        _exportSettingsButton.Click += ExportSettingsButton_Click;
        //
        // _importSettingsButton
        //
        _importSettingsButton.Location = new Point(144, 3);
        _importSettingsButton.Name = "_importSettingsButton";
        _importSettingsButton.Size = new Size(135, 38);
        _importSettingsButton.TabIndex = 1;
        _importSettingsButton.Text = "設定を読み込み";
        _importSettingsButton.Click += ImportSettingsButton_Click;
        //
        // _importLegacyButton
        //
        _importLegacyButton.Location = new Point(285, 3);
        _importLegacyButton.Name = "_importLegacyButton";
        _importLegacyButton.Size = new Size(147, 38);
        _importLegacyButton.TabIndex = 2;
        _importLegacyButton.Tag = "accent";
        _importLegacyButton.Text = "V1設定をインポート";
        _importLegacyButton.Click += ImportLegacyButton_Click;
        //
        // presetsTabPage
        //
        presetsTabPage.Controls.Add(presetsPagePanel);
        presetsTabPage.Location = new Point(4, 24);
        presetsTabPage.Name = "presetsTabPage";
        presetsTabPage.Padding = new Padding(14);
        presetsTabPage.Size = new Size(1050, 757);
        presetsTabPage.TabIndex = 1;
        presetsTabPage.Text = "プリセット・画像加工";
        //
        // presetsPagePanel
        //
        presetsPagePanel.Controls.Add(presetLayout);
        presetsPagePanel.Dock = DockStyle.Fill;
        presetsPagePanel.Location = new Point(14, 14);
        presetsPagePanel.Name = "presetsPagePanel";
        presetsPagePanel.Size = new Size(1022, 729);
        presetsPagePanel.TabIndex = 0;
        //
        // presetLayout
        //
        presetLayout.ColumnCount = 2;
        presetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 245F));
        presetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        presetLayout.Controls.Add(presetListLayout, 0, 0);
        presetLayout.Controls.Add(presetEditorPanel, 1, 0);
        presetLayout.Dock = DockStyle.Fill;
        presetLayout.Location = new Point(0, 0);
        presetLayout.Name = "presetLayout";
        presetLayout.RowCount = 1;
        presetLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        presetLayout.Size = new Size(1022, 729);
        presetLayout.TabIndex = 0;
        //
        // presetListLayout
        //
        presetListLayout.ColumnCount = 1;
        presetListLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        presetListLayout.Controls.Add(_presetList, 0, 0);
        presetListLayout.Controls.Add(presetButtonsPanel, 0, 1);
        presetListLayout.Dock = DockStyle.Fill;
        presetListLayout.Location = new Point(0, 0);
        presetListLayout.Margin = new Padding(0, 0, 18, 0);
        presetListLayout.Name = "presetListLayout";
        presetListLayout.RowCount = 2;
        presetListLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        presetListLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 128F));
        presetListLayout.Size = new Size(227, 729);
        presetListLayout.TabIndex = 0;
        //
        // _presetList
        //
        _presetList.Dock = DockStyle.Fill;
        _presetList.Location = new Point(3, 3);
        _presetList.Name = "_presetList";
        _presetList.Size = new Size(221, 595);
        _presetList.TabIndex = 0;
        _presetList.SelectedIndexChanged += PresetList_SelectedIndexChanged;
        //
        // presetButtonsPanel
        //
        presetButtonsPanel.Controls.Add(_addPresetButton);
        presetButtonsPanel.Controls.Add(_duplicatePresetButton);
        presetButtonsPanel.Controls.Add(_deletePresetButton);
        presetButtonsPanel.Controls.Add(_moveUpButton);
        presetButtonsPanel.Controls.Add(_moveDownButton);
        presetButtonsPanel.Dock = DockStyle.Fill;
        presetButtonsPanel.Location = new Point(0, 611);
        presetButtonsPanel.Margin = new Padding(0, 10, 0, 0);
        presetButtonsPanel.Name = "presetButtonsPanel";
        presetButtonsPanel.Size = new Size(227, 118);
        presetButtonsPanel.TabIndex = 1;
        //
        // _addPresetButton
        //
        _addPresetButton.Location = new Point(3, 3);
        _addPresetButton.Name = "_addPresetButton";
        _addPresetButton.Size = new Size(63, 28);
        _addPresetButton.TabIndex = 0;
        _addPresetButton.Text = "追加";
        _addPresetButton.Click += AddPresetButton_Click;
        //
        // _duplicatePresetButton
        //
        _duplicatePresetButton.Location = new Point(72, 3);
        _duplicatePresetButton.Name = "_duplicatePresetButton";
        _duplicatePresetButton.Size = new Size(63, 28);
        _duplicatePresetButton.TabIndex = 1;
        _duplicatePresetButton.Text = "複製";
        _duplicatePresetButton.Click += DuplicatePresetButton_Click;
        //
        // _deletePresetButton
        //
        _deletePresetButton.Location = new Point(141, 3);
        _deletePresetButton.Name = "_deletePresetButton";
        _deletePresetButton.Size = new Size(63, 28);
        _deletePresetButton.TabIndex = 2;
        _deletePresetButton.Text = "削除";
        _deletePresetButton.Click += DeletePresetButton_Click;
        //
        // _moveUpButton
        //
        _moveUpButton.Location = new Point(3, 37);
        _moveUpButton.Name = "_moveUpButton";
        _moveUpButton.Size = new Size(63, 28);
        _moveUpButton.TabIndex = 3;
        _moveUpButton.Text = "上へ";
        _moveUpButton.Click += MovePresetButton_Click;
        //
        // _moveDownButton
        //
        _moveDownButton.Location = new Point(72, 37);
        _moveDownButton.Name = "_moveDownButton";
        _moveDownButton.Size = new Size(63, 28);
        _moveDownButton.TabIndex = 4;
        _moveDownButton.Text = "下へ";
        _moveDownButton.Click += MovePresetButton_Click;
        //
        // presetEditorPanel
        //
        presetEditorPanel.AutoScroll = true;
        presetEditorPanel.Controls.Add(editorLayout);
        presetEditorPanel.Dock = DockStyle.Fill;
        presetEditorPanel.Location = new Point(248, 3);
        presetEditorPanel.Name = "presetEditorPanel";
        presetEditorPanel.Size = new Size(771, 723);
        presetEditorPanel.TabIndex = 1;
        //
        // editorLayout
        //
        editorLayout.AutoSize = true;
        editorLayout.ColumnCount = 2;
        editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125F));
        editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        editorLayout.Controls.Add(presetNameLabel, 0, 0);
        editorLayout.Controls.Add(_nameTextBox, 1, 0);
        editorLayout.Controls.Add(examinationLabel, 0, 1);
        editorLayout.Controls.Add(_examinationTextBox, 1, 1);
        editorLayout.Controls.Add(deviceLabel, 0, 2);
        editorLayout.Controls.Add(_deviceComboBox, 1, 2);
        editorLayout.Controls.Add(resolutionLabel, 0, 3);
        editorLayout.Controls.Add(_resolutionComboBox, 1, 3);
        editorLayout.Controls.Add(fpsLabel, 0, 4);
        editorLayout.Controls.Add(_fpsInput, 1, 4);
        editorLayout.Controls.Add(presetOptionsPanel, 1, 5);
        editorLayout.Controls.Add(roiCardPanel, 0, 6);
        editorLayout.Controls.Add(fontLabel, 0, 7);
        editorLayout.Controls.Add(_fontComboBox, 1, 7);
        editorLayout.Controls.Add(overlayLabel, 0, 8);
        editorLayout.Controls.Add(_overlayGrid, 1, 8);
        editorLayout.Controls.Add(overlayRawLabel, 0, 9);
        editorLayout.Controls.Add(_overlayRawTextBox, 1, 9);
        editorLayout.Controls.Add(overlayHintLabel, 1, 10);
        editorLayout.Dock = DockStyle.Top;
        editorLayout.Location = new Point(0, 0);
        editorLayout.Name = "editorLayout";
        editorLayout.RowCount = 11;
        editorLayout.RowStyles.Add(new RowStyle());
        editorLayout.RowStyles.Add(new RowStyle());
        editorLayout.RowStyles.Add(new RowStyle());
        editorLayout.RowStyles.Add(new RowStyle());
        editorLayout.RowStyles.Add(new RowStyle());
        editorLayout.RowStyles.Add(new RowStyle());
        editorLayout.RowStyles.Add(new RowStyle());
        editorLayout.RowStyles.Add(new RowStyle());
        editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 200F));
        editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));
        editorLayout.RowStyles.Add(new RowStyle());
        editorLayout.Size = new Size(771, 659);
        editorLayout.TabIndex = 0;
        //
        // presetNameLabel
        //
        presetNameLabel.Location = new Point(3, 0);
        presetNameLabel.Name = "presetNameLabel";
        presetNameLabel.Size = new Size(100, 23);
        presetNameLabel.TabIndex = 0;
        presetNameLabel.Text = "プリセット名";
        //
        // _nameTextBox
        //
        _nameTextBox.Location = new Point(128, 3);
        _nameTextBox.Name = "_nameTextBox";
        _nameTextBox.Size = new Size(342, 23);
        _nameTextBox.TabIndex = 1;
        //
        // examinationLabel
        //
        examinationLabel.Location = new Point(3, 29);
        examinationLabel.Name = "examinationLabel";
        examinationLabel.Size = new Size(100, 23);
        examinationLabel.TabIndex = 2;
        examinationLabel.Text = "検査名";
        //
        // _examinationTextBox
        //
        _examinationTextBox.Location = new Point(128, 32);
        _examinationTextBox.Name = "_examinationTextBox";
        _examinationTextBox.Size = new Size(342, 23);
        _examinationTextBox.TabIndex = 3;
        //
        // deviceLabel
        //
        deviceLabel.Location = new Point(3, 58);
        deviceLabel.Name = "deviceLabel";
        deviceLabel.Size = new Size(100, 23);
        deviceLabel.TabIndex = 4;
        deviceLabel.Text = "入力デバイス";
        //
        // _deviceComboBox
        //
        _deviceComboBox.Location = new Point(128, 61);
        _deviceComboBox.Name = "_deviceComboBox";
        _deviceComboBox.Size = new Size(342, 23);
        _deviceComboBox.TabIndex = 5;
        _deviceComboBox.SelectedIndexChanged += DeviceComboBox_SelectedIndexChanged;
        //
        // resolutionLabel
        //
        resolutionLabel.Location = new Point(3, 87);
        resolutionLabel.Name = "resolutionLabel";
        resolutionLabel.Size = new Size(100, 23);
        resolutionLabel.TabIndex = 6;
        resolutionLabel.Text = "解像度・フォーマット";
        //
        // _resolutionComboBox
        //
        _resolutionComboBox.Location = new Point(128, 90);
        _resolutionComboBox.Name = "_resolutionComboBox";
        _resolutionComboBox.Size = new Size(342, 23);
        _resolutionComboBox.TabIndex = 7;
        //
        // fpsLabel
        //
        fpsLabel.Location = new Point(3, 116);
        fpsLabel.Name = "fpsLabel";
        fpsLabel.Size = new Size(100, 23);
        fpsLabel.TabIndex = 8;
        fpsLabel.Text = "FPS(0で実測)";
        //
        // _fpsInput
        //
        _fpsInput.Location = new Point(125, 121);
        _fpsInput.Margin = new Padding(0, 5, 0, 8);
        _fpsInput.Maximum = new decimal(new int[] { 240, 0, 0, 0 });
        _fpsInput.Name = "_fpsInput";
        _fpsInput.Size = new Size(120, 23);
        _fpsInput.TabIndex = 9;
        //
        // presetOptionsPanel
        //
        presetOptionsPanel.AutoSize = true;
        presetOptionsPanel.Controls.Add(_videoCheckBox);
        presetOptionsPanel.Controls.Add(_presetPreviewOnlyCheckBox);
        presetOptionsPanel.Location = new Point(125, 157);
        presetOptionsPanel.Margin = new Padding(0, 5, 0, 12);
        presetOptionsPanel.Name = "presetOptionsPanel";
        presetOptionsPanel.Size = new Size(259, 29);
        presetOptionsPanel.TabIndex = 10;
        //
        // _videoCheckBox
        //
        _videoCheckBox.AutoSize = true;
        _videoCheckBox.Location = new Point(0, 5);
        _videoCheckBox.Margin = new Padding(0, 5, 16, 5);
        _videoCheckBox.Name = "_videoCheckBox";
        _videoCheckBox.Size = new Size(155, 19);
        _videoCheckBox.TabIndex = 10;
        _videoCheckBox.Text = "動画をRSBaseにファイリング";
        //
        // _presetPreviewOnlyCheckBox
        //
        _presetPreviewOnlyCheckBox.AutoSize = true;
        _presetPreviewOnlyCheckBox.Location = new Point(171, 5);
        _presetPreviewOnlyCheckBox.Margin = new Padding(0, 5, 0, 5);
        _presetPreviewOnlyCheckBox.Name = "_presetPreviewOnlyCheckBox";
        _presetPreviewOnlyCheckBox.Size = new Size(88, 19);
        _presetPreviewOnlyCheckBox.TabIndex = 11;
        _presetPreviewOnlyCheckBox.Text = "プレビューのみ";
        //
        // roiCardPanel
        //
        editorLayout.SetColumnSpan(roiCardPanel, 2);
        roiCardPanel.Controls.Add(roiLayout);
        roiCardPanel.Dock = DockStyle.Top;
        roiCardPanel.Location = new Point(0, 203);
        roiCardPanel.Margin = new Padding(0, 5, 0, 12);
        roiCardPanel.Name = "roiCardPanel";
        roiCardPanel.Padding = new Padding(14);
        roiCardPanel.Size = new Size(771, 118);
        roiCardPanel.TabIndex = 11;
        roiCardPanel.Tag = "raised";
        //
        // roiLayout
        //
        roiLayout.ColumnCount = 1;
        roiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        roiLayout.Controls.Add(roiFlowPanel, 0, 1);
        roiLayout.Controls.Add(roiTitleLabel, 0, 0);
        roiLayout.Dock = DockStyle.Fill;
        roiLayout.Location = new Point(14, 14);
        roiLayout.Name = "roiLayout";
        roiLayout.RowCount = 3;
        roiLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        roiLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
        roiLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 27F));
        roiLayout.Size = new Size(743, 90);
        roiLayout.TabIndex = 0;
        roiLayout.Tag = "raised";
        //
        // roiFlowPanel
        //
        roiFlowPanel.Controls.Add(roiX1Label);
        roiFlowPanel.Controls.Add(_roiX1Input);
        roiFlowPanel.Controls.Add(roiY1Label);
        roiFlowPanel.Controls.Add(_roiY1Input);
        roiFlowPanel.Controls.Add(roiX2Label);
        roiFlowPanel.Controls.Add(_roiX2Input);
        roiFlowPanel.Controls.Add(roiY2Label);
        roiFlowPanel.Controls.Add(_roiY2Input);
        roiFlowPanel.Controls.Add(roiHintLabel);
        roiFlowPanel.Dock = DockStyle.Fill;
        roiFlowPanel.Location = new Point(3, 31);
        roiFlowPanel.Name = "roiFlowPanel";
        roiFlowPanel.Size = new Size(737, 33);
        roiFlowPanel.TabIndex = 1;
        roiFlowPanel.Tag = "raised";
        //
        // roiX1Label
        //
        roiX1Label.Location = new Point(3, 0);
        roiX1Label.Name = "roiX1Label";
        roiX1Label.Size = new Size(40, 23);
        roiX1Label.TabIndex = 0;
        roiX1Label.Text = "左上X";
        //
        // _roiX1Input
        //
        _roiX1Input.Location = new Point(49, 3);
        _roiX1Input.Maximum = new decimal(new int[] { 99999, 0, 0, 0 });
        _roiX1Input.Name = "_roiX1Input";
        _roiX1Input.Size = new Size(61, 23);
        _roiX1Input.TabIndex = 1;
        //
        // roiY1Label
        //
        roiY1Label.Location = new Point(116, 0);
        roiY1Label.Name = "roiY1Label";
        roiY1Label.Size = new Size(40, 23);
        roiY1Label.TabIndex = 2;
        roiY1Label.Text = "左上Y";
        //
        // _roiY1Input
        //
        _roiY1Input.Location = new Point(162, 3);
        _roiY1Input.Maximum = new decimal(new int[] { 99999, 0, 0, 0 });
        _roiY1Input.Name = "_roiY1Input";
        _roiY1Input.Size = new Size(61, 23);
        _roiY1Input.TabIndex = 3;
        //
        // roiX2Label
        //
        roiX2Label.Location = new Point(229, 0);
        roiX2Label.Name = "roiX2Label";
        roiX2Label.Size = new Size(40, 23);
        roiX2Label.TabIndex = 4;
        roiX2Label.Text = "右下X";
        //
        // _roiX2Input
        //
        _roiX2Input.Location = new Point(275, 3);
        _roiX2Input.Maximum = new decimal(new int[] { 99999, 0, 0, 0 });
        _roiX2Input.Name = "_roiX2Input";
        _roiX2Input.Size = new Size(61, 23);
        _roiX2Input.TabIndex = 5;
        //
        // roiY2Label
        //
        roiY2Label.Location = new Point(342, 0);
        roiY2Label.Name = "roiY2Label";
        roiY2Label.Size = new Size(40, 23);
        roiY2Label.TabIndex = 6;
        roiY2Label.Text = "右下Y";
        //
        // _roiY2Input
        //
        _roiY2Input.Location = new Point(388, 3);
        _roiY2Input.Maximum = new decimal(new int[] { 99999, 0, 0, 0 });
        _roiY2Input.Name = "_roiY2Input";
        _roiY2Input.Size = new Size(61, 23);
        _roiY2Input.TabIndex = 7;
        //
        // roiHintLabel
        //
        roiHintLabel.AutoSize = true;
        roiHintLabel.Location = new Point(455, 0);
        roiHintLabel.Name = "roiHintLabel";
        roiHintLabel.Size = new Size(253, 15);
        roiHintLabel.TabIndex = 2;
        roiHintLabel.Text = "すべて0なら無効。指定範囲外は白背景になります。";
        //
        // roiTitleLabel
        //
        roiTitleLabel.AutoSize = true;
        roiTitleLabel.Location = new Point(3, 0);
        roiTitleLabel.Name = "roiTitleLabel";
        roiTitleLabel.Size = new Size(114, 15);
        roiTitleLabel.TabIndex = 0;
        roiTitleLabel.Tag = "text";
        roiTitleLabel.Text = "ROI（切り出し領域）";
        //
        // fontLabel
        //
        fontLabel.Location = new Point(3, 333);
        fontLabel.Name = "fontLabel";
        fontLabel.Size = new Size(100, 23);
        fontLabel.TabIndex = 12;
        fontLabel.Text = "文字フォント";
        //
        // _fontComboBox
        //
        _fontComboBox.Location = new Point(128, 336);
        _fontComboBox.Name = "_fontComboBox";
        _fontComboBox.Size = new Size(215, 23);
        _fontComboBox.TabIndex = 13;
        //
        // overlayLabel
        //
        overlayLabel.Location = new Point(3, 362);
        overlayLabel.Name = "overlayLabel";
        overlayLabel.Size = new Size(100, 96);
        overlayLabel.TabIndex = 14;
        overlayLabel.Text = "埋込み文字列\r\n（左上x,y座標とサイズ、色を指定）";
        //
        // _overlayGrid
        //
        _overlayGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _overlayGrid.Columns.AddRange(new DataGridViewColumn[] { overlayXColumn, overlayYColumn, overlaySizeColumn, overlayColorColumn, overlayTextColumn });
        _overlayGrid.Dock = DockStyle.Fill;
        _overlayGrid.Location = new Point(128, 365);
        _overlayGrid.Name = "_overlayGrid";
        _overlayGrid.Size = new Size(640, 194);
        _overlayGrid.TabIndex = 15;
        //
        // overlayXColumn
        //
        overlayXColumn.FillWeight = 35F;
        overlayXColumn.HeaderText = "X";
        overlayXColumn.Name = "overlayXColumn";
        //
        // overlayYColumn
        //
        overlayYColumn.FillWeight = 35F;
        overlayYColumn.HeaderText = "Y";
        overlayYColumn.Name = "overlayYColumn";
        //
        // overlaySizeColumn
        //
        overlaySizeColumn.FillWeight = 45F;
        overlaySizeColumn.HeaderText = "サイズ";
        overlaySizeColumn.Name = "overlaySizeColumn";
        //
        // overlayColorColumn
        //
        overlayColorColumn.FillWeight = 65F;
        overlayColorColumn.HeaderText = "色";
        overlayColorColumn.Name = "overlayColorColumn";
        //
        // overlayTextColumn
        //
        overlayTextColumn.FillWeight = 170F;
        overlayTextColumn.HeaderText = "文字列";
        overlayTextColumn.Name = "overlayTextColumn";
        //
        // overlayRawLabel
        //
        overlayRawLabel.Location = new Point(3, 562);
        overlayRawLabel.Name = "overlayRawLabel";
        overlayRawLabel.Size = new Size(100, 23);
        overlayRawLabel.TabIndex = 16;
        //
        // _overlayRawTextBox
        //
        _overlayRawTextBox.AcceptsReturn = true;
        _overlayRawTextBox.Dock = DockStyle.Fill;
        _overlayRawTextBox.Location = new Point(128, 565);
        _overlayRawTextBox.Multiline = true;
        _overlayRawTextBox.Name = "_overlayRawTextBox";
        _overlayRawTextBox.ScrollBars = ScrollBars.Vertical;
        _overlayRawTextBox.Size = new Size(640, 70);
        _overlayRawTextBox.TabIndex = 17;
        _overlayRawTextBox.TextChanged += OverlayRawTextBox_TextChanged;
        //
        // overlayHintLabel
        //
        overlayHintLabel.AutoSize = true;
        overlayHintLabel.Location = new Point(125, 644);
        overlayHintLabel.Margin = new Padding(0, 6, 0, 0);
        overlayHintLabel.Name = "overlayHintLabel";
        overlayHintLabel.Size = new Size(317, 15);
        overlayHintLabel.TabIndex = 18;
        overlayHintLabel.Text = "色はFFFFFF形式。$d=日付、$t=時刻、$i=患者ID、$n=患者名";
        //
        // footerPanel
        //
        footerPanel.Controls.Add(_okButton);
        footerPanel.Controls.Add(_cancelButton);
        footerPanel.Dock = DockStyle.Fill;
        footerPanel.FlowDirection = FlowDirection.RightToLeft;
        footerPanel.Location = new Point(21, 812);
        footerPanel.Name = "footerPanel";
        footerPanel.Padding = new Padding(0, 8, 0, 0);
        footerPanel.Size = new Size(1058, 48);
        footerPanel.TabIndex = 2;
        footerPanel.Tag = "window";
        //
        // _okButton
        //
        _okButton.Location = new Point(955, 11);
        _okButton.Name = "_okButton";
        _okButton.Size = new Size(100, 38);
        _okButton.TabIndex = 0;
        _okButton.Tag = "accent";
        _okButton.Text = "保存";
        _okButton.Click += OkButton_Click;
        //
        // _cancelButton
        //
        _cancelButton.DialogResult = DialogResult.Cancel;
        _cancelButton.Location = new Point(839, 11);
        _cancelButton.Name = "_cancelButton";
        _cancelButton.Size = new Size(110, 38);
        _cancelButton.TabIndex = 1;
        _cancelButton.Text = "キャンセル";
        //
        // _continuousRecordingRadioButton
        //
        _continuousRecordingRadioButton.AutoSize = true;
        _continuousRecordingRadioButton.Checked = true;
        _continuousRecordingRadioButton.Location = new Point(0, 10);
        _continuousRecordingRadioButton.Margin = new Padding(0, 10, 24, 0);
        _continuousRecordingRadioButton.Name = "_continuousRecordingRadioButton";
        _continuousRecordingRadioButton.Size = new Size(150, 19);
        _continuousRecordingRadioButton.TabIndex = 0;
        _continuousRecordingRadioButton.TabStop = true;
        _continuousRecordingRadioButton.Text = "一時動画し停止後に再生";
        //
        // _previewOnlyRadioButton
        //
        _previewOnlyRadioButton.AutoSize = true;
        _previewOnlyRadioButton.Location = new Point(174, 10);
        _previewOnlyRadioButton.Margin = new Padding(0, 10, 0, 0);
        _previewOnlyRadioButton.Name = "_previewOnlyRadioButton";
        _previewOnlyRadioButton.Size = new Size(87, 19);
        _previewOnlyRadioButton.TabIndex = 1;
        _previewOnlyRadioButton.Text = "プレビューのみ";
        //
        // SettingsForm
        //
        AcceptButton = _okButton;
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        CancelButton = _cancelButton;
        ClientSize = new Size(1100, 881);
        Controls.Add(rootLayout);
        Icon = (Icon)resources.GetObject("$this.Icon");
        MinimumSize = new Size(900, 650);
        Name = "SettingsForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "ENTcapture2 設定";
        rootLayout.ResumeLayout(false);
        navigationPanel.ResumeLayout(false);
        contentPanel.ResumeLayout(false);
        settingsTabControl.ResumeLayout(false);
        generalTabPage.ResumeLayout(false);
        generalPagePanel.ResumeLayout(false);
        generalPagePanel.PerformLayout();
        generalLayout.ResumeLayout(false);
        snapshotCardPanel.ResumeLayout(false);
        snapshotCardLayout.ResumeLayout(false);
        snapshotCardLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_jpegQualityInput).EndInit();
        captureModePanel.ResumeLayout(false);
        captureModePanel.PerformLayout();
        finalVideoCardPanel.ResumeLayout(false);
        finalVideoCardLayout.ResumeLayout(false);
        finalVideoCardLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_reencodeThresholdInput).EndInit();
        temporaryCardPanel.ResumeLayout(false);
        temporaryCardLayout.ResumeLayout(false);
        temporaryCardLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_retentionDaysInput).EndInit();
        ((System.ComponentModel.ISupportInitialize)_maximumFpsInput).EndInit();
        ((System.ComponentModel.ISupportInitialize)_segmentMinutesInput).EndInit();
        hotkeyCardPanel.ResumeLayout(false);
        hotkeyCardLayout.ResumeLayout(false);
        hotkeyCardLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_snapshotDebounceInput).EndInit();
        integrationCardPanel.ResumeLayout(false);
        integrationLayout.ResumeLayout(false);
        integrationLayout.PerformLayout();
        importCardPanel.ResumeLayout(false);
        importLayout.ResumeLayout(false);
        importLayout.PerformLayout();
        _settingsTransferPanel.ResumeLayout(false);
        presetsTabPage.ResumeLayout(false);
        presetsPagePanel.ResumeLayout(false);
        presetLayout.ResumeLayout(false);
        presetListLayout.ResumeLayout(false);
        presetButtonsPanel.ResumeLayout(false);
        presetEditorPanel.ResumeLayout(false);
        presetEditorPanel.PerformLayout();
        editorLayout.ResumeLayout(false);
        editorLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_fpsInput).EndInit();
        presetOptionsPanel.ResumeLayout(false);
        presetOptionsPanel.PerformLayout();
        roiCardPanel.ResumeLayout(false);
        roiLayout.ResumeLayout(false);
        roiLayout.PerformLayout();
        roiFlowPanel.ResumeLayout(false);
        roiFlowPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_roiX1Input).EndInit();
        ((System.ComponentModel.ISupportInitialize)_roiY1Input).EndInit();
        ((System.ComponentModel.ISupportInitialize)_roiX2Input).EndInit();
        ((System.ComponentModel.ISupportInitialize)_roiY2Input).EndInit();
        ((System.ComponentModel.ISupportInitialize)_overlayGrid).EndInit();
        footerPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    private ToolTip toolTip1 = null!;
    private System.ComponentModel.IContainer components = null!;
    private Label label1 = null!;
    private Label label2 = null!;
    private Label label3 = null!;
    private Label label4 = null!;
}
