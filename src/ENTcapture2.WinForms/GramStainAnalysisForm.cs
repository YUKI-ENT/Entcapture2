using ENTcapture2.WinForms.Analysis;
using ENTcapture2.WinForms.Ui;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Text.Encodings.Web;
using System.Text.Json;
using DrawingPoint = System.Drawing.Point;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingSize = System.Drawing.Size;

namespace ENTcapture2.WinForms;

public sealed class GramStainAnalysisForm : Form
{
    private readonly string _defaultDirectory;
    private readonly Panel _imageScrollPanel = new();
    private readonly PictureBox _imageBox = new();
    private readonly Button _openImageButton = new();
    private readonly Button _openModelButton = new();
    private readonly Button _aiAnalyzeButton = new();
    private readonly Button _clearBoxesButton = new();
    private readonly Button _showAllDetectionsButton = new();
    private readonly CheckBox _selectedDetectionsOnlyCheck = new();
    private readonly CheckBox _annotationModeCheck = new();
    private readonly ComboBox _annotationClassCombo = new();
    private readonly ComboBox _datasetSplitCombo = new();
    private readonly ComboBox _cropSizeCombo = new();
    private readonly ComboBox _cropAnchorCombo = new();
    private readonly Button _clearCropButton = new();
    private readonly Button _startTileButton = new();
    private readonly Button _editTileButton = new();
    private readonly Button _saveTileButton = new();
    private readonly Button _saveEmptyTileButton = new();
    private readonly Button _skipTileButton = new();
    private readonly Button _previousTileButton = new();
    private readonly Button _nextTileButton = new();
    private readonly ComboBox _zoomCombo = new();
    private readonly TextBox _datasetDirectoryBox = new();
    private readonly Button _browseDatasetButton = new();
    private readonly Button _saveDatasetButton = new();
    private readonly NumericUpDown _confidenceInput = new();
    private readonly NumericUpDown _iouInput = new();
    private readonly Label _fileLabel = new();
    private readonly Label _modelLabel = new();
    private readonly Label _statusLabel = new();
    private readonly DataGridView _countGrid = new();
    private readonly DataGridView _objectGrid = new();
    private readonly ListBox _candidateList = new();
    private Mat? _originalImage;
    private Mat? _sourceImage;
    private Bitmap? _displayImage;
    private GramStainAnalysisResult? _lastResult;
    private YoloBacteriaDetector? _yoloDetector;
    private bool _isDraggingAnnotation;
    private DrawingPoint _dragStart;
    private DrawingPoint _dragEnd;
    private double _zoomScale = 1.0;
    private bool _fitToView = true;
    private DrawingPoint? _cropPreviewPoint;
    private string? _sourceName;
    private string? _sourcePath;
    private DrawingRectangle? _activeCropBoundsOnOriginal;
    private TileCandidate? _activeTile;
    private readonly List<TileCandidate> _tileCandidates = [];
    private readonly Dictionary<int, List<YoloAnnotationBox>> _tileAnnotationDrafts = [];
    private readonly Dictionary<int, SavedDatasetFiles> _savedTileFiles = [];
    private readonly HashSet<int> _savedTileIndexes = [];
    private readonly HashSet<int> _skippedTileIndexes = [];
    private int _currentTileIndex = -1;
    private readonly HashSet<int> _selectedDetectionIndexes = [];
    private string? _currentAnalysisLabel;
    private int _nextAnnotationId = 1;
    private int? _selectedAnnotationId;
    private bool _annotationsDirty;
    private readonly List<YoloAnnotationBox> _annotations = [];
    private readonly ContextMenuStrip _annotationMenu = new();

    public GramStainAnalysisForm(string defaultDirectory, Bitmap? initialImage = null)
    {
        _defaultDirectory = defaultDirectory;
        InitializeLayout();
        LoadSavedYoloSettings();
        if (initialImage is not null)
        {
            LoadBitmap(initialImage, "現在の表示画像");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _originalImage?.Dispose();
            _sourceImage?.Dispose();
            _displayImage?.Dispose();
            _lastResult?.Dispose();
            _yoloDetector?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeLayout()
    {
        Text = "AI解析";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new DrawingSize(1020, 700);
        ClientSize = new DrawingSize(1220, 820);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(12),
            BackColor = Theme.Window
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 315F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 168F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));

        Control toolbar = BuildToolbar();
        var imagePanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Black,
            Padding = new Padding(6),
            Margin = new Padding(0, 0, 10, 0)
        };
        _imageScrollPanel.Dock = DockStyle.Fill;
        _imageScrollPanel.AutoScroll = true;
        _imageScrollPanel.BackColor = Color.Black;
        _imageScrollPanel.Resize += (_, _) =>
        {
            if (_fitToView)
            {
                ApplyZoomLayout();
            }
        };

        _imageBox.Location = DrawingPoint.Empty;
        _imageBox.Margin = Padding.Empty;
        _imageBox.SizeMode = PictureBoxSizeMode.StretchImage;
        _imageBox.BackColor = Color.Black;
        _imageBox.MouseDown += ImageBox_MouseDown;
        _imageBox.MouseMove += ImageBox_MouseMove;
        _imageBox.MouseUp += ImageBox_MouseUp;
        _imageBox.MouseLeave += ImageBox_MouseLeave;
        _imageBox.Paint += ImageBox_Paint;
        _imageScrollPanel.Controls.Add(_imageBox);
        imagePanel.Controls.Add(_imageScrollPanel);
        BuildAnnotationMenu();

        Control sidePanel = BuildSidePanel();
        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.Tag = "text";
        _statusLabel.Text = "準備完了";

        root.Controls.Add(toolbar, 0, 0);
        root.SetColumnSpan(toolbar, 2);
        root.Controls.Add(imagePanel, 0, 1);
        root.Controls.Add(sidePanel, 1, 1);
        root.Controls.Add(_statusLabel, 0, 2);
        root.SetColumnSpan(_statusLabel, 2);

        Controls.Add(root);
        Theme.Apply(this);
        Theme.ApplyButton(_openImageButton);
        Theme.ApplyButton(_openModelButton);
        Theme.ApplyButton(_aiAnalyzeButton, true);
        Theme.ApplyButton(_clearBoxesButton);
        Theme.ApplyButton(_showAllDetectionsButton);
        Theme.ApplyButton(_clearCropButton);
        Theme.ApplyButton(_startTileButton, true);
        Theme.ApplyButton(_editTileButton);
        Theme.ApplyButton(_saveTileButton, true);
        Theme.ApplyButton(_saveEmptyTileButton);
        Theme.ApplyButton(_skipTileButton);
        Theme.ApplyButton(_previousTileButton);
        Theme.ApplyButton(_nextTileButton);
        Theme.ApplyButton(_browseDatasetButton);
        Theme.ApplyButton(_saveDatasetButton, true);
        ConfigureGrid(_countGrid);
        ConfigureGrid(_objectGrid);
    }

    private Control BuildToolbar()
    {
        var toolbar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            BackColor = Theme.Window
        };
        toolbar.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        toolbar.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        toolbar.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));

        var analysisToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            BackColor = Theme.Window
        };
        var annotationToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            BackColor = Theme.Window
        };
        var tileToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            BackColor = Theme.Window
        };

        ConfigureToolbarButton(_openImageButton, "静止画を開く", 112);
        _openImageButton.Click += OpenImageButton_Click;
        ConfigureToolbarButton(_openModelButton, "ONNXモデル", 104);
        _openModelButton.Click += OpenModelButton_Click;
        ConfigureToolbarButton(_aiAnalyzeButton, "AI解析", 76);
        _aiAnalyzeButton.Enabled = false;
        _aiAnalyzeButton.Click += AiAnalyzeButton_Click;
        ConfigureToolbarButton(_clearBoxesButton, "boxクリア", 82);
        _clearBoxesButton.Enabled = false;
        _clearBoxesButton.Click += ClearBoxesButton_Click;
        ConfigureToolbarButton(_showAllDetectionsButton, "全表示", 74);
        _showAllDetectionsButton.Enabled = false;
        _showAllDetectionsButton.Click += ShowAllDetectionsButton_Click;
        ConfigureToolbarButton(_clearCropButton, "領域クリア", 86);
        _clearCropButton.Enabled = false;
        _clearCropButton.Click += ClearCropButton_Click;
        ConfigureToolbarButton(_startTileButton, "tile開始", 82);
        _startTileButton.Enabled = false;
        _startTileButton.Click += StartTileButton_Click;
        ConfigureToolbarButton(_editTileButton, "編集", 58);
        _editTileButton.Enabled = false;
        _editTileButton.Click += EditTileButton_Click;
        ConfigureToolbarButton(_saveTileButton, "YOLO保存", 86);
        _saveTileButton.Enabled = false;
        _saveTileButton.Click += SaveTileButton_Click;
        ConfigureToolbarButton(_saveEmptyTileButton, "空保存", 74);
        _saveEmptyTileButton.Enabled = false;
        _saveEmptyTileButton.Click += SaveEmptyTileButton_Click;
        ConfigureToolbarButton(_skipTileButton, "スキップ", 78);
        _skipTileButton.Enabled = false;
        _skipTileButton.Click += SkipTileButton_Click;
        ConfigureToolbarButton(_previousTileButton, "前へ", 58);
        _previousTileButton.Enabled = false;
        _previousTileButton.Click += PreviousTileButton_Click;
        ConfigureToolbarButton(_nextTileButton, "次へ", 58);
        _nextTileButton.Enabled = false;
        _nextTileButton.Click += NextTileButton_Click;
        ConfigureToolbarButton(_browseDatasetButton, "保存先", 70);
        _browseDatasetButton.Click += BrowseDatasetButton_Click;
        ConfigureToolbarButton(_saveDatasetButton, "YOLO保存", 86);
        _saveDatasetButton.Enabled = false;
        _saveDatasetButton.Click += SaveDatasetButton_Click;

        ConfigurePercentInput(_confidenceInput, 25);
        ConfigurePercentInput(_iouInput, 45);
        ConfigureCombo(_zoomCombo, 76);
        _zoomCombo.Items.AddRange(["Fit", "50%", "100%", "200%", "400%"]);
        _zoomCombo.SelectedItem = "Fit";
        _zoomCombo.SelectedIndexChanged += ZoomCombo_SelectedIndexChanged;

        _annotationModeCheck.Text = "教師データ作成";
        _annotationModeCheck.Width = 116;
        _annotationModeCheck.Height = 32;
        _annotationModeCheck.Margin = new Padding(8, 0, 6, 4);
        _annotationModeCheck.TextAlign = ContentAlignment.MiddleLeft;
        _annotationModeCheck.CheckedChanged += AnnotationModeCheck_CheckedChanged;
        _selectedDetectionsOnlyCheck.Text = "選択のみ";
        _selectedDetectionsOnlyCheck.Width = 82;
        _selectedDetectionsOnlyCheck.Height = 32;
        _selectedDetectionsOnlyCheck.Margin = new Padding(0, 0, 6, 4);
        _selectedDetectionsOnlyCheck.TextAlign = ContentAlignment.MiddleLeft;
        _selectedDetectionsOnlyCheck.Enabled = false;
        _selectedDetectionsOnlyCheck.CheckedChanged += (_, _) => RefreshDetectionOverlay();

        ConfigureCombo(_annotationClassCombo, 112);
        foreach (YoloAnnotationClass annotationClass in YoloAnnotationClass.All)
        {
            _annotationClassCombo.Items.Add(annotationClass);
        }

        _annotationClassCombo.SelectedIndex = 1;
        ConfigureCombo(_datasetSplitCombo, 62);
        _datasetSplitCombo.Items.AddRange(["train", "val"]);
        _datasetSplitCombo.SelectedIndex = 0;
        ConfigureCombo(_cropSizeCombo, 70);
        _cropSizeCombo.Items.Add("640");
        _cropSizeCombo.SelectedIndex = 0;
        ConfigureCombo(_cropAnchorCombo, 78);
        _cropAnchorCombo.Items.AddRange(["中心", "左上"]);
        _cropAnchorCombo.SelectedIndex = 0;

        _datasetDirectoryBox.Width = 265;
        _datasetDirectoryBox.Height = 26;
        _datasetDirectoryBox.Margin = new Padding(0, 2, 6, 4);
        _datasetDirectoryBox.Text = GetDefaultDatasetDirectory();
        _datasetDirectoryBox.Leave += (_, _) => SaveYoloSettings();

        _fileLabel.AutoSize = false;
        _fileLabel.Width = 330;
        _fileLabel.Height = 26;
        _fileLabel.TextAlign = ContentAlignment.MiddleLeft;
        _fileLabel.Text = "画像を選択してください";
        _fileLabel.Tag = "text";
        _modelLabel.AutoSize = false;
        _modelLabel.Width = 180;
        _modelLabel.Height = 26;
        _modelLabel.TextAlign = ContentAlignment.MiddleLeft;
        _modelLabel.Text = "AIモデル未選択";
        _modelLabel.Tag = "text";
        analysisToolbar.Controls.Add(_openImageButton);
        analysisToolbar.Controls.Add(_openModelButton);
        analysisToolbar.Controls.Add(_aiAnalyzeButton);
        analysisToolbar.Controls.Add(_clearBoxesButton);
        analysisToolbar.Controls.Add(_selectedDetectionsOnlyCheck);
        analysisToolbar.Controls.Add(_showAllDetectionsButton);
        analysisToolbar.Controls.Add(CreateToolbarLabel("Conf%", 48));
        analysisToolbar.Controls.Add(_confidenceInput);
        analysisToolbar.Controls.Add(CreateToolbarLabel("IoU%", 40));
        analysisToolbar.Controls.Add(_iouInput);
        analysisToolbar.Controls.Add(CreateToolbarLabel("倍率", 36));
        analysisToolbar.Controls.Add(_zoomCombo);
        analysisToolbar.Controls.Add(_fileLabel);
        analysisToolbar.Controls.Add(_modelLabel);

        annotationToolbar.Controls.Add(_annotationModeCheck);
        annotationToolbar.Controls.Add(CreateToolbarLabel("crop", 36));
        annotationToolbar.Controls.Add(_cropSizeCombo);
        annotationToolbar.Controls.Add(_cropAnchorCombo);
        annotationToolbar.Controls.Add(_clearCropButton);
        annotationToolbar.Controls.Add(CreateToolbarLabel("ラベル", 44));
        annotationToolbar.Controls.Add(_annotationClassCombo);
        annotationToolbar.Controls.Add(CreateToolbarLabel("保存", 36));
        annotationToolbar.Controls.Add(_datasetSplitCombo);
        annotationToolbar.Controls.Add(_datasetDirectoryBox);
        annotationToolbar.Controls.Add(_browseDatasetButton);
        annotationToolbar.Controls.Add(_saveDatasetButton);

        tileToolbar.Controls.Add(CreateToolbarLabel("tile", 36));
        tileToolbar.Controls.Add(_startTileButton);
        tileToolbar.Controls.Add(_skipTileButton);
        tileToolbar.Controls.Add(_saveEmptyTileButton);
        tileToolbar.Controls.Add(_saveTileButton);
        tileToolbar.Controls.Add(_previousTileButton);
        tileToolbar.Controls.Add(_nextTileButton);
        tileToolbar.Controls.Add(_editTileButton);
        tileToolbar.Controls.Add(CreateToolbarLabel("設定", 36));
        tileToolbar.Controls.Add(CreateToolbarLabel("640 / stride 512 / overlap 128", 210));

        toolbar.Controls.Add(analysisToolbar, 0, 0);
        toolbar.Controls.Add(annotationToolbar, 0, 1);
        toolbar.Controls.Add(tileToolbar, 0, 2);
        return toolbar;
    }

    private static void ConfigureToolbarButton(
        Button button,
        string text,
        int width)
    {
        button.Text = text;
        button.Width = width;
        button.Height = 32;
        button.Margin = new Padding(0, 0, 6, 4);
    }

    private static void ConfigurePercentInput(NumericUpDown input, int value)
    {
        input.Minimum = 1;
        input.Maximum = 99;
        input.Value = value;
        input.Width = 52;
        input.Height = 32;
        input.Margin = new Padding(0, 2, 6, 4);
    }

    private static void ConfigureCombo(ComboBox combo, int width)
    {
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.Width = width;
        combo.Height = 32;
        combo.Margin = new Padding(0, 2, 6, 4);
    }

    private static Label CreateToolbarLabel(string text, int width)
    {
        return new Label
        {
            Text = text,
            Width = width,
            Height = 32,
            Margin = new Padding(0, 0, 2, 4),
            TextAlign = ContentAlignment.MiddleRight,
            Tag = "text"
        };
    }

    private Control BuildSidePanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            BackColor = Theme.Surface,
            Padding = new Padding(10)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 160F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 112F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        Label countTitle = CreateSectionLabel("カウント");
        Label candidateTitle = CreateSectionLabel("菌名候補メモ");
        Label objectTitle = CreateSectionLabel("検出領域");

        _countGrid.Dock = DockStyle.Fill;
        _countGrid.RowHeadersVisible = false;
        _countGrid.AllowUserToAddRows = false;
        _countGrid.AllowUserToDeleteRows = false;
        _countGrid.ReadOnly = true;
        _countGrid.Columns.Add("class", "分類");
        _countGrid.Columns.Add("count", "数");
        _countGrid.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _countGrid.Columns[1].Width = 58;
        _countGrid.SelectionChanged += CountGrid_SelectionChanged;

        _candidateList.Dock = DockStyle.Fill;
        _candidateList.HorizontalScrollbar = true;

        _objectGrid.Dock = DockStyle.Fill;
        _objectGrid.RowHeadersVisible = false;
        _objectGrid.AllowUserToAddRows = false;
        _objectGrid.AllowUserToDeleteRows = false;
        _objectGrid.ReadOnly = true;
        _objectGrid.Columns.Add("class", "分類");
        _objectGrid.Columns.Add("conf", "信頼");
        _objectGrid.Columns.Add("shape", "形状");
        _objectGrid.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _objectGrid.Columns[1].Width = 50;
        _objectGrid.Columns[2].Width = 58;
        _objectGrid.SelectionChanged += ObjectGrid_SelectionChanged;

        panel.Controls.Add(countTitle, 0, 0);
        panel.Controls.Add(_countGrid, 0, 1);
        panel.Controls.Add(candidateTitle, 0, 2);
        panel.Controls.Add(_candidateList, 0, 3);
        panel.Controls.Add(objectTitle, 0, 4);
        panel.Controls.Add(_objectGrid, 0, 5);
        return panel;
    }

    private static Label CreateSectionLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = Theme.HeadingFont(10F),
            Tag = "accent"
        };
    }

    private static void ConfigureGrid(DataGridView grid)
    {
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = true;
        grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        grid.ColumnHeadersHeightSizeMode =
            DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersHeight = 26;
        grid.RowTemplate.Height = 24;
        grid.BorderStyle = BorderStyle.None;
    }

    private void OpenImageButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "グラム染色画像を選択",
            Filter = "画像ファイル|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff|すべてのファイル|*.*",
            InitialDirectory = Directory.Exists(_defaultDirectory)
                ? _defaultDirectory
                : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            LoadImage(dialog.FileName);
        }
    }

    private void OpenModelButton_Click(object? sender, EventArgs e)
    {
        string savedModelPath = Properties.Settings.Default.YoloOnnxModelPath;
        using var dialog = new OpenFileDialog
        {
            Title = "YOLO ONNXモデルを選択",
            Filter = "ONNX model|*.onnx|すべてのファイル|*.*",
            InitialDirectory = File.Exists(savedModelPath)
                ? Path.GetDirectoryName(savedModelPath)
                : Directory.Exists(_defaultDirectory)
                ? _defaultDirectory
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            LoadYoloModel(dialog.FileName);
            Properties.Settings.Default.YoloOnnxModelPath = dialog.FileName;
            SaveYoloSettings();
            _statusLabel.Text =
                "AIモデルを読み込みました。クラス順は G-cocci, G+cocci, G-bacilli, G+bacilli を想定します。";
        }
        catch (Exception exception)
        {
            _yoloDetector?.Dispose();
            _yoloDetector = null;
            _modelLabel.Text = "AIモデル未選択";
            _aiAnalyzeButton.Enabled = false;
            MessageBox.Show(
                this,
                $"ONNXモデルを読み込めませんでした。\r\n{exception.Message}",
                "AI解析",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void LoadImage(string path)
    {
        Mat image = Cv2.ImRead(path, ImreadModes.Color);
        if (image.Empty())
        {
            MessageBox.Show(
                this,
                "画像を読み込めませんでした。",
                "AI解析",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            image.Dispose();
            return;
        }

        SetSourceImage(image, path);
    }

    private void LoadBitmap(Bitmap bitmap, string sourceName)
    {
        using Bitmap clone = (Bitmap)bitmap.Clone();
        Mat image = BitmapConverter.ToMat(clone);
        SetSourceImage(image, sourceName);
    }

    private void SetSourceImage(Mat image, string sourceName)
    {
        _originalImage?.Dispose();
        _originalImage = image.Clone();
        _sourceImage?.Dispose();
        _sourceImage = image;
        _sourceName = sourceName;
        _sourcePath = File.Exists(sourceName) ? sourceName : null;
        _activeCropBoundsOnOriginal = null;
        _activeTile = null;
        _tileCandidates.Clear();
        _tileAnnotationDrafts.Clear();
        _savedTileFiles.Clear();
        _savedTileIndexes.Clear();
        _skippedTileIndexes.Clear();
        _currentTileIndex = -1;
        _selectedDetectionIndexes.Clear();
        _currentAnalysisLabel = null;
        _lastResult?.Dispose();
        _lastResult = null;
        _annotations.Clear();
        _selectedAnnotationId = null;
        _annotationsDirty = false;
        _nextAnnotationId = 1;
        _cropPreviewPoint = null;
        SetDisplayImage(image);
        _fileLabel.Text = sourceName;
        _statusLabel.Text = "画像を読み込みました。AI解析は640px tile分割で実行します。";
        _aiAnalyzeButton.Enabled = _yoloDetector is not null;
        _saveDatasetButton.Enabled = true;
        _clearBoxesButton.Enabled = true;
        _clearCropButton.Enabled = false;
        _startTileButton.Enabled = true;
        UpdateTileButtons();
        UpdateDetectionControls();
        SetZoomFit();
        ClearResults();
        UpdateAnnotationStatus();
    }

    private void AiAnalyzeButton_Click(object? sender, EventArgs e)
    {
        if (_sourceImage is null || _yoloDetector is null)
        {
            return;
        }

        float confidence = decimal.ToSingle(_confidenceInput.Value) / 100F;
        float iou = decimal.ToSingle(_iouInput.Value) / 100F;
        RunAnalysis(
            "AI解析中... 640px tileに分割して推論しています",
            () => _yoloDetector.AnalyzeTiled(_sourceImage, confidence, iou),
            "AI解析");
    }

    private void RunAnalysis(
        string runningText,
        Func<GramStainAnalysisResult> analyze,
        string label)
    {
        try
        {
            UseWaitCursor = true;
            _statusLabel.Text = runningText;
            _lastResult?.Dispose();
            Mat sourceImage = _sourceImage ??
                throw new InvalidOperationException("画像が読み込まれていません。");
            _lastResult = analyze();
            _currentAnalysisLabel = label;
            _selectedDetectionIndexes.Clear();
            using Mat overlay = CreateDetectionOverlay(sourceImage, _lastResult.Detections);
            SetDisplayImage(overlay);
            PopulateResults(_lastResult);
            UpdateDetectionControls();
            _clearBoxesButton.Enabled = true;
            _statusLabel.Text =
                $"{label}完了: 検出候補 {_lastResult.Counts.Total} 個。結果は参考値です。";
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                $"{label}に失敗しました。\r\n{exception.Message}",
                "AI解析",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            _statusLabel.Text = $"{label}に失敗しました";
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void RefreshDetectionOverlay()
    {
        if (_sourceImage is null || _lastResult is null)
        {
            return;
        }

        using Mat overlay = CreateDetectionOverlay(_sourceImage, _lastResult.Detections);
        SetDisplayImage(overlay);
    }

    private Mat CreateDetectionOverlay(
        Mat source,
        IReadOnlyList<GramStainDetection> detections)
    {
        using Bitmap bitmap = BitmapConverter.ToBitmap(source);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using Font labelFont = CreateJapaneseOverlayFont();
        for (int index = 0; index < detections.Count; index++)
        {
            bool selected = _selectedDetectionIndexes.Count == 0 ||
                _selectedDetectionIndexes.Contains(index);
            if (_selectedDetectionsOnlyCheck.Checked && !selected)
            {
                continue;
            }

            DrawDetectionOverlay(graphics, detections[index], index, selected, labelFont);
        }

        return BitmapConverter.ToMat(bitmap);
    }

    private static Font CreateJapaneseOverlayFont()
    {
        string[] candidates =
        [
            "Yu Gothic UI",
            "Meiryo",
            "MS Gothic",
            SystemFonts.DefaultFont.FontFamily.Name
        ];
        foreach (string family in candidates)
        {
            try
            {
                return new Font(family, 24F, FontStyle.Bold, GraphicsUnit.Pixel);
            }
            catch (ArgumentException)
            {
            }
        }

        return (Font)SystemFonts.DefaultFont.Clone();
    }

    private void DrawDetectionOverlay(
        Graphics graphics,
        GramStainDetection detection,
        int detectionIndex,
        bool selected,
        Font labelFont)
    {
        DrawingRectangle bounds = new(
            detection.Bounds.Left,
            detection.Bounds.Top,
            detection.Bounds.Width,
            detection.Bounds.Height);
        Color color = GetDetectionColor(detection);
        int alpha = selected ? 255 : 90;
        using Pen pen = new(Color.FromArgb(alpha, color), selected ? 3 : 2);
        graphics.DrawRectangle(pen, bounds);

        string label = $"{detection.ShortLabel} {detection.Confidence:0.00}";
        DrawingSize textSize = TextRenderer.MeasureText(label, labelFont);
        DrawingRectangle labelRect = new(
            bounds.Left,
            Math.Max(0, bounds.Top - textSize.Height - 2),
            textSize.Width + 12,
            textSize.Height + 4);
        using SolidBrush background = new(Color.FromArgb(selected ? 220 : 120, 0, 0, 0));
        using SolidBrush textBrush = new(Color.FromArgb(alpha, color));
        graphics.FillRectangle(background, labelRect);
        graphics.DrawString(label, labelFont, textBrush, labelRect.Left + 6, labelRect.Top + 2);

        if (selected && _selectedDetectionIndexes.Contains(detectionIndex))
        {
            using Pen haloPen = new(Color.White, 1);
            haloPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
            graphics.DrawEllipse(haloPen, Inflate(bounds, 6));
        }
    }

    private static DrawingRectangle Inflate(DrawingRectangle bounds, int amount)
    {
        DrawingRectangle inflated = bounds;
        inflated.Inflate(amount, amount);
        return inflated;
    }

    private static Color GetDetectionColor(GramStainDetection detection)
    {
        return (detection.Gram, detection.Shape) switch
        {
            (GramStainPolarity.Positive, BacteriumShape.Coccus) => Color.Magenta,
            (GramStainPolarity.Negative, BacteriumShape.Coccus) => Color.Cyan,
            (GramStainPolarity.Positive, BacteriumShape.Bacillus) => Color.Orange,
            (GramStainPolarity.Negative, BacteriumShape.Bacillus) => Color.Lime,
            _ => Color.Yellow
        };
    }

    private void SetDisplayImage(Mat image)
    {
        Bitmap next = BitmapConverter.ToBitmap(image);
        _imageBox.Image = next;
        _displayImage?.Dispose();
        _displayImage = next;
        ApplyZoomLayout();
        _imageBox.Invalidate();
    }

    private void ZoomCombo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_zoomCombo.SelectedItem is not string value)
        {
            return;
        }

        if (value == "Fit")
        {
            SetZoomFit();
            return;
        }

        if (value.EndsWith("%", StringComparison.Ordinal) &&
            double.TryParse(value[..^1], out double percent))
        {
            _fitToView = false;
            _zoomScale = Math.Clamp(percent / 100.0, 0.1, 8.0);
            ApplyZoomLayout();
        }
    }

    private void LoadSavedYoloSettings()
    {
        string datasetDirectory = Properties.Settings.Default.YoloDatasetDirectory;
        if (!string.IsNullOrWhiteSpace(datasetDirectory))
        {
            _datasetDirectoryBox.Text = datasetDirectory;
        }

        string modelPath = Properties.Settings.Default.YoloOnnxModelPath;
        if (File.Exists(modelPath))
        {
            try
            {
                LoadYoloModel(modelPath);
                _statusLabel.Text = "前回のAIモデルを読み込みました。";
            }
            catch
            {
                _modelLabel.Text = "AIモデル未選択";
                _aiAnalyzeButton.Enabled = false;
            }
        }
    }

    private void LoadYoloModel(string modelPath)
    {
        _yoloDetector?.Dispose();
        _yoloDetector = new YoloBacteriaDetector(modelPath);
        _modelLabel.Text = Path.GetFileName(modelPath);
        _aiAnalyzeButton.Enabled = _sourceImage is not null;
    }

    private void SaveYoloSettings()
    {
        Properties.Settings.Default.YoloDatasetDirectory = _datasetDirectoryBox.Text.Trim();
        Properties.Settings.Default.Save();
    }

    private string GetDefaultDatasetDirectory()
    {
        string saved = Properties.Settings.Default.YoloDatasetDirectory;
        return string.IsNullOrWhiteSpace(saved)
            ? Path.Combine(_defaultDirectory, "ENTcapture2_YOLO_Dataset")
            : saved;
    }

    private void SetZoomFit()
    {
        _fitToView = true;
        _zoomCombo.SelectedItem = "Fit";
        ApplyZoomLayout();
    }

    private void ApplyZoomLayout()
    {
        if (_displayImage is null)
        {
            _imageBox.Size = DrawingSize.Empty;
            return;
        }

        if (_fitToView)
        {
            DrawingSize viewport = _imageScrollPanel.ClientSize;
            if (viewport.Width > 0 && viewport.Height > 0)
            {
                double fitScale = Math.Min(
                    viewport.Width / (double)_displayImage.Width,
                    viewport.Height / (double)_displayImage.Height);
                _zoomScale = Math.Clamp(fitScale, 0.05, 8.0);
            }
        }

        _imageBox.Size = new DrawingSize(
            Math.Max(1, (int)Math.Round(_displayImage.Width * _zoomScale)),
            Math.Max(1, (int)Math.Round(_displayImage.Height * _zoomScale)));
        _imageBox.Invalidate();
    }

    private void AnnotationModeCheck_CheckedChanged(object? sender, EventArgs e)
    {
        _statusLabel.Text = _annotationModeCheck.Checked
            ? "教師データ作成: 手動cropまたはtile上でboxをドラッグしてください。"
            : "教師データ作成を解除しました。";
        _cropPreviewPoint = null;
        _imageBox.Invalidate();
    }

    private void BrowseDatasetButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "YOLO教師データの保存先フォルダを選択",
            SelectedPath = Directory.Exists(_datasetDirectoryBox.Text)
                ? _datasetDirectoryBox.Text
                : _defaultDirectory
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _datasetDirectoryBox.Text = dialog.SelectedPath;
            SaveYoloSettings();
        }
    }

    private void ClearBoxesButton_Click(object? sender, EventArgs e)
    {
        _annotations.Clear();
        _selectedAnnotationId = null;
        _annotationsDirty = false;
        _isDraggingAnnotation = false;
        _selectedDetectionIndexes.Clear();
        _currentAnalysisLabel = null;

        _lastResult?.Dispose();
        _lastResult = null;
        ClearResults();
        UpdateDetectionControls();
        if (_sourceImage is not null)
        {
            ResetToOriginalImage();
        }

        _statusLabel.Text = "boxをクリアしました。";
    }

    private void ShowAllDetectionsButton_Click(object? sender, EventArgs e)
    {
        _selectedDetectionIndexes.Clear();
        _selectedDetectionsOnlyCheck.Checked = false;
        _countGrid.ClearSelection();
        _objectGrid.ClearSelection();
        RefreshDetectionOverlay();
        if (_lastResult is not null)
        {
            _statusLabel.Text =
                $"{_currentAnalysisLabel ?? "解析"}結果を全表示しました。";
        }
    }

    private void ClearCropButton_Click(object? sender, EventArgs e)
    {
        ClearCropSelection();
        _statusLabel.Text = "crop領域をクリアしました。";
    }

    private void ClearCropSelection()
    {
        SaveCurrentTileDraft();
        _annotations.Clear();
        _selectedAnnotationId = null;
        _annotationsDirty = false;
        _isDraggingAnnotation = false;
        _cropPreviewPoint = null;
        _activeTile = null;
        _currentTileIndex = -1;
        ResetToOriginalImage();
        _clearCropButton.Enabled = false;
        UpdateTileButtons();
        _imageBox.Invalidate();
    }

    private void StartTileButton_Click(object? sender, EventArgs e)
    {
        if (_originalImage is null)
        {
            return;
        }

        _tileCandidates.Clear();
        _tileAnnotationDrafts.Clear();
        _savedTileIndexes.Clear();
        _skippedTileIndexes.Clear();
        _tileCandidates.AddRange(CreateTileCandidates(_originalImage.Width, _originalImage.Height));
        if (_tileCandidates.Count == 0)
        {
            _statusLabel.Text = "tile候補を作成できませんでした。";
            return;
        }

        _currentTileIndex = 0;
        ShowCurrentTile();
    }

    private void EditTileButton_Click(object? sender, EventArgs e)
    {
        if (_activeTile is null)
        {
            return;
        }

        _annotationModeCheck.Checked = true;
        _statusLabel.Text =
            $"tile {_currentTileIndex + 1}/{_tileCandidates.Count}: ラベル編集できます。";
    }

    private void SaveEmptyTileButton_Click(object? sender, EventArgs e)
    {
        if (_activeTile is null)
        {
            return;
        }

        DialogResult result = MessageBox.Show(
            this,
            "現在のtileを空ラベルとして保存しますか？",
            "YOLO教師データ",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (result != DialogResult.Yes)
        {
            return;
        }

        _annotations.Clear();
        _selectedAnnotationId = null;
        _annotationsDirty = false;
        SaveCurrentTileAndAdvance("空ラベルtileを保存して次へ進みました。");
    }

    private void SaveTileButton_Click(object? sender, EventArgs e)
    {
        if (_activeTile is null)
        {
            return;
        }

        SaveCurrentTileAndAdvance("tileをYOLO保存して次へ進みました。");
    }

    private void SkipTileButton_Click(object? sender, EventArgs e)
    {
        if (_activeTile is not null)
        {
            SaveCurrentTileDraft();
            _skippedTileIndexes.Add(_activeTile.Index);
        }

        MoveTile(1, "tileをスキップして次へ進みました。");
    }

    private void PreviousTileButton_Click(object? sender, EventArgs e)
    {
        MoveTile(-1, "前のtileへ戻りました。");
    }

    private void NextTileButton_Click(object? sender, EventArgs e)
    {
        MoveTile(1, "次のtileへ進みました。");
    }

    private void MoveTile(int offset, string statusText)
    {
        if (_tileCandidates.Count == 0)
        {
            return;
        }

        SaveCurrentTileDraft();

        int nextIndex = Math.Clamp(_currentTileIndex + offset, 0, _tileCandidates.Count - 1);
        if (nextIndex == _currentTileIndex)
        {
            _statusLabel.Text = _currentTileIndex <= 0
                ? "最初のtileです。"
                : "最後のtileです。";
            return;
        }

        _currentTileIndex = nextIndex;
        ShowCurrentTile(statusText);
    }

    private void SaveCurrentTileAndAdvance(string statusText)
    {
        string datasetRoot = _datasetDirectoryBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(datasetRoot))
        {
            MessageBox.Show(
                this,
                "保存先フォルダを指定してください。",
                "YOLO教師データ",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        try
        {
            SaveYoloSettings();
            SaveCurrentAnnotation(datasetRoot, overwriteActiveTile: true);
            _annotationsDirty = false;
            SaveCurrentTileDraft();
            if (_activeTile is not null)
            {
                _savedTileIndexes.Add(_activeTile.Index);
                _skippedTileIndexes.Remove(_activeTile.Index);
            }

            if (_currentTileIndex < _tileCandidates.Count - 1)
            {
                _currentTileIndex++;
                ShowCurrentTile(statusText);
            }
            else
            {
                _statusLabel.Text =
                    $"{statusText} 最後のtileです。{CreateCurrentTileStatusText()}";
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                $"YOLO教師データの保存に失敗しました。\r\n{exception.Message}",
                "YOLO教師データ",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void ShowCurrentTile(string? prefix = null)
    {
        if (_originalImage is null ||
            _currentTileIndex < 0 ||
            _currentTileIndex >= _tileCandidates.Count)
        {
            return;
        }

        TileCandidate tile = _tileCandidates[_currentTileIndex];
        using Mat roi = new(_originalImage, new Rect(
            tile.Bounds.Left,
            tile.Bounds.Top,
            tile.Bounds.Width,
            tile.Bounds.Height));
        Mat tileImage = roi.Clone();
        _sourceImage?.Dispose();
        _sourceImage = tileImage;
        _activeCropBoundsOnOriginal = tile.Bounds;
        _activeTile = tile;
        RestoreCurrentTileDraft();
        _selectedDetectionIndexes.Clear();
        _lastResult?.Dispose();
        _lastResult = null;
        _currentAnalysisLabel = null;
        ClearResults();
        SetDisplayImage(tileImage);
        SetZoomFit();
        _clearCropButton.Enabled = true;
        _saveDatasetButton.Enabled = true;
        UpdateTileButtons();
        string tileText = CreateCurrentTileStatusText();
        _statusLabel.Text = string.IsNullOrWhiteSpace(prefix)
            ? tileText
            : $"{prefix} {tileText}";
    }

    private void SaveCurrentTileDraft()
    {
        if (_activeTile is null)
        {
            return;
        }

        _tileAnnotationDrafts[_activeTile.Index] = _annotations.ToList();
    }

    private void RestoreCurrentTileDraft()
    {
        _annotations.Clear();
        _selectedAnnotationId = null;
        _annotationsDirty = false;
        if (_activeTile is not null &&
            _tileAnnotationDrafts.TryGetValue(_activeTile.Index, out List<YoloAnnotationBox>? draft))
        {
            _annotations.AddRange(draft);
        }

        _nextAnnotationId = _annotations.Count == 0
            ? 1
            : _annotations.Max(item => item.Id) + 1;
    }

    private string CreateCurrentTileStatusText()
    {
        if (_activeTile is null)
        {
            return string.Empty;
        }

        string state = _savedTileIndexes.Contains(_activeTile.Index)
            ? "保存済み(再保存で上書き)"
            : _annotations.Count > 0 || _annotationsDirty
            ? "編集中"
            : _skippedTileIndexes.Contains(_activeTile.Index)
            ? "スキップ"
            : "未保存";
        return
            $"tile {_currentTileIndex + 1}/{_tileCandidates.Count}: {state}, box {_annotations.Count} 件, x={_activeTile.Bounds.Left}, y={_activeTile.Bounds.Top}, {_activeTile.Bounds.Width}x{_activeTile.Bounds.Height}";
    }

    private static IReadOnlyList<TileCandidate> CreateTileCandidates(int imageWidth, int imageHeight)
    {
        const int tileSize = 640;
        const int stride = 512;
        const int overlap = 128;
        int width = Math.Min(tileSize, imageWidth);
        int height = Math.Min(tileSize, imageHeight);
        var tiles = new List<TileCandidate>();
        int index = 0;
        foreach (int y in CreateTilePositions(imageHeight, height, stride))
        {
            foreach (int x in CreateTilePositions(imageWidth, width, stride))
            {
                tiles.Add(new TileCandidate(
                    index++,
                    new DrawingRectangle(x, y, width, height),
                    tileSize,
                    stride,
                    overlap));
            }
        }

        return tiles;
    }

    private static IEnumerable<int> CreateTilePositions(int fullSize, int tileSize, int stride)
    {
        if (fullSize <= tileSize)
        {
            yield return 0;
            yield break;
        }

        var positions = new List<int>();
        for (int position = 0; position <= fullSize - tileSize; position += stride)
        {
            positions.Add(position);
        }

        int last = fullSize - tileSize;
        if (positions.Count == 0 || positions[^1] != last)
        {
            positions.Add(last);
        }

        foreach (int position in positions)
        {
            yield return position;
        }
    }

    private void UpdateTileButtons()
    {
        bool hasTiles = _tileCandidates.Count > 0 && _activeTile is not null;
        _editTileButton.Enabled = hasTiles;
        _saveTileButton.Enabled = hasTiles;
        _saveEmptyTileButton.Enabled = hasTiles;
        _skipTileButton.Enabled = hasTiles;
        _previousTileButton.Enabled = hasTiles && _currentTileIndex > 0;
        _nextTileButton.Enabled = hasTiles && _currentTileIndex < _tileCandidates.Count - 1;
    }

    private void ImageBox_MouseDown(object? sender, MouseEventArgs e)
    {
        if (_sourceImage is null)
        {
            return;
        }

        if (e.Button == MouseButtons.Right)
        {
            ShowAnnotationMenu(e.Location);
            return;
        }

        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        if (_annotationModeCheck.Checked)
        {
            if (_activeCropBoundsOnOriginal is null && _originalImage is not null)
            {
                CreateCropFromPoint(e.Location);
                _imageBox.Invalidate();
                return;
            }

            _isDraggingAnnotation = true;
            _dragStart = e.Location;
            _dragEnd = e.Location;
            SelectAnnotationAt(e.Location);
            _imageBox.Invalidate();
            return;
        }
    }

    private void ImageBox_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_annotationModeCheck.Checked &&
            _activeCropBoundsOnOriginal is null &&
            _originalImage is not null)
        {
            _cropPreviewPoint = e.Location;
            _imageBox.Invalidate();
            return;
        }

        if (!_isDraggingAnnotation)
        {
            return;
        }

        _dragEnd = e.Location;
        _imageBox.Invalidate();
    }

    private void ImageBox_MouseLeave(object? sender, EventArgs e)
    {
        if (_cropPreviewPoint is not null)
        {
            _cropPreviewPoint = null;
            _imageBox.Invalidate();
        }
    }

    private void ImageBox_MouseUp(object? sender, MouseEventArgs e)
    {
        if (_sourceImage is null)
        {
            return;
        }

        if (_isDraggingAnnotation)
        {
            _isDraggingAnnotation = false;
            _dragEnd = e.Location;
            AddAnnotationFromDrag();
            _imageBox.Invalidate();
            return;
        }

    }

    private void ImageBox_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        PaintAnnotations(e.Graphics);
        PaintCropPreview(e.Graphics);

        if (!_isDraggingAnnotation)
        {
            return;
        }

        DrawingRectangle rect = NormalizeRectangle(_dragStart, _dragEnd);
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        Color color = _isDraggingAnnotation
            ? _activeCropBoundsOnOriginal is null ? Color.Yellow : GetAnnotationColor(GetSelectedAnnotationClass())
            : Color.FromArgb(255, 45, 212, 191);
        using var pen = new Pen(color, 2);
        e.Graphics.DrawRectangle(pen, rect);
    }

    private bool TryMapClientPointToImage(
        DrawingPoint clientPoint,
        out DrawingPoint imagePoint)
    {
        imagePoint = DrawingPoint.Empty;
        if (_sourceImage is null || _zoomScale <= 0)
        {
            return false;
        }

        if (clientPoint.X < 0 || clientPoint.Y < 0 ||
            clientPoint.X > _imageBox.Width || clientPoint.Y > _imageBox.Height)
        {
            return false;
        }

        int x = (int)Math.Round(clientPoint.X / _zoomScale);
        int y = (int)Math.Round(clientPoint.Y / _zoomScale);
        imagePoint = new DrawingPoint(
            Math.Clamp(x, 0, _sourceImage.Width - 1),
            Math.Clamp(y, 0, _sourceImage.Height - 1));
        return true;
    }

    private DrawingRectangle MapImageRectangleToView(DrawingRectangle imageRect)
    {
        return new DrawingRectangle(
            (int)Math.Round(imageRect.Left * _zoomScale),
            (int)Math.Round(imageRect.Top * _zoomScale),
            Math.Max(1, (int)Math.Round(imageRect.Width * _zoomScale)),
            Math.Max(1, (int)Math.Round(imageRect.Height * _zoomScale)));
    }

    private void PaintCropPreview(Graphics graphics)
    {
        if (!_annotationModeCheck.Checked ||
            _activeCropBoundsOnOriginal is not null ||
            _originalImage is null ||
            _cropPreviewPoint is not DrawingPoint previewPoint ||
            !TryMapClientPointToImage(previewPoint, out DrawingPoint imagePoint))
        {
            return;
        }

        DrawingRectangle cropPreview = MapImageRectangleToView(
            CreateCropBounds(imagePoint, GetSelectedCropSize()));
        using var cropPen = new Pen(Color.Yellow, 2);
        graphics.DrawRectangle(cropPen, cropPreview);
    }

    private void CreateCropFromPoint(DrawingPoint clientPoint)
    {
        if (_originalImage is null ||
            !TryMapClientPointToImage(clientPoint, out DrawingPoint imagePoint))
        {
            return;
        }

        int cropSize = GetSelectedCropSize();
        DrawingRectangle cropBounds = CreateCropBounds(imagePoint, cropSize);
        using Mat roi = new(_originalImage, new Rect(
            cropBounds.Left,
            cropBounds.Top,
            cropBounds.Width,
            cropBounds.Height));
        Mat crop = roi.Clone();

        _sourceImage?.Dispose();
        _sourceImage = crop;
        _activeCropBoundsOnOriginal = cropBounds;
        _annotations.Clear();
        _selectedAnnotationId = null;
        _annotationsDirty = false;
        _nextAnnotationId = 1;
        _lastResult?.Dispose();
        _lastResult = null;
        _cropPreviewPoint = null;
        ClearResults();
        SetDisplayImage(crop);
        SetZoomFit();
        _clearCropButton.Enabled = true;
        _statusLabel.Text =
            $"crop作成: {cropBounds.Width}x{cropBounds.Height}px ({cropBounds.Left}, {cropBounds.Top})";
    }

    private DrawingRectangle CreateCropBounds(DrawingPoint selectedPoint, int cropSize)
    {
        if (_originalImage is null)
        {
            return DrawingRectangle.Empty;
        }

        int width = Math.Min(cropSize, _originalImage.Width);
        int height = Math.Min(cropSize, _originalImage.Height);
        bool anchorIsTopLeft = _cropAnchorCombo.SelectedItem?.ToString() == "左上";
        int left = anchorIsTopLeft
            ? selectedPoint.X
            : selectedPoint.X - width / 2;
        int top = anchorIsTopLeft
            ? selectedPoint.Y
            : selectedPoint.Y - height / 2;

        left = Math.Clamp(left, 0, Math.Max(0, _originalImage.Width - width));
        top = Math.Clamp(top, 0, Math.Max(0, _originalImage.Height - height));
        return new DrawingRectangle(left, top, width, height);
    }

    private int GetSelectedCropSize()
    {
        return int.TryParse(_cropSizeCombo.SelectedItem?.ToString(), out int cropSize)
            ? Math.Clamp(cropSize, 1, 10000)
            : 640;
    }

    private void ResetToOriginalImage()
    {
        if (_originalImage is null)
        {
            return;
        }

        _sourceImage?.Dispose();
        _sourceImage = _originalImage.Clone();
        _activeCropBoundsOnOriginal = null;
        _activeTile = null;
        SetDisplayImage(_sourceImage);
        SetZoomFit();
        UpdateTileButtons();
    }

    private static DrawingRectangle GetZoomedImageRectangle(
        DrawingSize container,
        DrawingSize image)
    {
        if (image.Width <= 0 || image.Height <= 0 ||
            container.Width <= 0 || container.Height <= 0)
        {
            return DrawingRectangle.Empty;
        }

        double scale = Math.Min(
            container.Width / (double)image.Width,
            container.Height / (double)image.Height);
        int width = Math.Max(1, (int)Math.Round(image.Width * scale));
        int height = Math.Max(1, (int)Math.Round(image.Height * scale));
        return new DrawingRectangle(
            (container.Width - width) / 2,
            (container.Height - height) / 2,
            width,
            height);
    }

    private static DrawingRectangle NormalizeRectangle(
        DrawingPoint a,
        DrawingPoint b)
    {
        int left = Math.Min(a.X, b.X);
        int top = Math.Min(a.Y, b.Y);
        return new DrawingRectangle(
            left,
            top,
            Math.Abs(a.X - b.X),
            Math.Abs(a.Y - b.Y));
    }

    private void BuildAnnotationMenu()
    {
        _annotationMenu.Items.Clear();
        foreach (YoloAnnotationClass annotationClass in YoloAnnotationClass.All)
        {
            ToolStripMenuItem item = new(annotationClass.DisplayName)
            {
                Tag = annotationClass
            };
            item.Click += (_, _) => AssignSelectedAnnotationClass(annotationClass);
            _annotationMenu.Items.Add(item);
        }

        _annotationMenu.Items.Add(new ToolStripSeparator());
        ToolStripMenuItem deleteItem = new("削除");
        deleteItem.Click += (_, _) => DeleteSelectedAnnotation();
        _annotationMenu.Items.Add(deleteItem);
    }

    private void AddAnnotationFromDrag()
    {
        if (_sourceImage is null ||
            !TryMapClientPointToImage(_dragStart, out DrawingPoint imageStart) ||
            !TryMapClientPointToImage(_dragEnd, out DrawingPoint imageEnd))
        {
            return;
        }

        DrawingRectangle bounds = NormalizeRectangle(imageStart, imageEnd);
        if (bounds.Width < 2 || bounds.Height < 2)
        {
            return;
        }

        bounds = ClampImageRectangle(bounds);
        YoloAnnotationBox annotation = new(
            _nextAnnotationId++,
            bounds,
            GetSelectedAnnotationClass(),
            DateTimeOffset.Now);
        _annotations.Add(annotation);
        _selectedAnnotationId = annotation.Id;
        _annotationsDirty = true;
        MarkActiveTileDirty();
        UpdateAnnotationStatus();
    }

    private DrawingRectangle ClampImageRectangle(DrawingRectangle bounds)
    {
        if (_sourceImage is null)
        {
            return bounds;
        }

        int left = Math.Clamp(bounds.Left, 0, _sourceImage.Width - 1);
        int top = Math.Clamp(bounds.Top, 0, _sourceImage.Height - 1);
        int right = Math.Clamp(bounds.Right, left + 1, _sourceImage.Width);
        int bottom = Math.Clamp(bounds.Bottom, top + 1, _sourceImage.Height);
        return DrawingRectangle.FromLTRB(left, top, right, bottom);
    }

    private void PaintAnnotations(Graphics graphics)
    {
        foreach (YoloAnnotationBox annotation in _annotations)
        {
            DrawingRectangle viewRect = MapImageRectangleToView(annotation.Bounds);
            Color color = GetAnnotationColor(annotation.Class);
            bool selected = annotation.Id == _selectedAnnotationId;
            using Pen pen = new(color, selected ? 3 : 2);
            graphics.DrawRectangle(pen, viewRect);
            string label = annotation.Class.ShortName;
            DrawingSize labelSize = TextRenderer.MeasureText(label, Font);
            DrawingRectangle labelRect = new(
                viewRect.Left,
                Math.Max(0, viewRect.Top - labelSize.Height - 2),
                labelSize.Width + 8,
                labelSize.Height + 2);
            using SolidBrush background = new(Color.FromArgb(210, 0, 0, 0));
            graphics.FillRectangle(background, labelRect);
            TextRenderer.DrawText(
                graphics,
                label,
                Font,
                labelRect,
                color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    private void ShowAnnotationMenu(DrawingPoint location)
    {
        if (!_annotationModeCheck.Checked || _activeCropBoundsOnOriginal is null)
        {
            return;
        }

        SelectAnnotationAt(location);
        _annotationMenu.Show(_imageBox, location);
    }

    private void SelectAnnotationAt(DrawingPoint location)
    {
        if (!TryMapClientPointToImage(location, out DrawingPoint imagePoint))
        {
            _selectedAnnotationId = null;
            return;
        }

        YoloAnnotationBox? hit = _annotations
            .LastOrDefault(item => item.Bounds.Contains(imagePoint));
        _selectedAnnotationId = hit?.Id;
        _imageBox.Invalidate();
    }

    private void AssignSelectedAnnotationClass(YoloAnnotationClass annotationClass)
    {
        int index = _annotations.FindIndex(item => item.Id == _selectedAnnotationId);
        if (index < 0)
        {
            _annotationClassCombo.SelectedItem = annotationClass;
            return;
        }

        _annotations[index] = _annotations[index] with { Class = annotationClass };
        _annotationClassCombo.SelectedItem = annotationClass;
        _annotationsDirty = true;
        MarkActiveTileDirty();
        UpdateAnnotationStatus();
        _imageBox.Invalidate();
    }

    private void DeleteSelectedAnnotation()
    {
        int removed = _annotations.RemoveAll(item => item.Id == _selectedAnnotationId);
        if (removed > 0)
        {
            _selectedAnnotationId = null;
            _annotationsDirty = true;
            MarkActiveTileDirty();
            UpdateAnnotationStatus();
            _imageBox.Invalidate();
        }
    }

    private void MarkActiveTileDirty()
    {
        if (_activeTile is null)
        {
            return;
        }

        _savedTileIndexes.Remove(_activeTile.Index);
        _skippedTileIndexes.Remove(_activeTile.Index);
    }

    private YoloAnnotationClass GetSelectedAnnotationClass()
    {
        return _annotationClassCombo.SelectedItem as YoloAnnotationClass
            ?? YoloAnnotationClass.All[0];
    }

    private static Color GetAnnotationColor(YoloAnnotationClass annotationClass)
    {
        return annotationClass.Id switch
        {
            0 => Color.Cyan,
            1 => Color.Magenta,
            2 => Color.Lime,
            3 => Color.Orange,
            _ => Color.White
        };
    }

    private void UpdateAnnotationStatus()
    {
        if (_activeTile is not null)
        {
            SaveCurrentTileDraft();
            _statusLabel.Text = CreateCurrentTileStatusText();
            return;
        }

        string dirty = _annotationsDirty ? " / 未保存" : "";
        _statusLabel.Text = $"教師データ: box {_annotations.Count} 件{dirty}";
    }

    private void SaveDatasetButton_Click(object? sender, EventArgs e)
    {
        if (_sourceImage is null)
        {
            return;
        }

        if (_activeTile is not null)
        {
            SaveCurrentTileAndAdvance("tileをYOLO保存して次へ進みました。");
            return;
        }

        string datasetRoot = _datasetDirectoryBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(datasetRoot))
        {
            MessageBox.Show(
                this,
                "保存先フォルダを指定してください。",
                "YOLO教師データ",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        try
        {
            SaveYoloSettings();
            SaveCurrentAnnotation(datasetRoot, overwriteActiveTile: false);
            _annotationsDirty = false;
            UpdateAnnotationStatus();
            MessageBox.Show(
                this,
                "YOLO教師データを保存しました。",
                "YOLO教師データ",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                $"YOLO教師データの保存に失敗しました。\r\n{exception.Message}",
                "YOLO教師データ",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void SaveCurrentAnnotation(string datasetRoot, bool overwriteActiveTile)
    {
        if (_sourceImage is null)
        {
            return;
        }

        string split = _datasetSplitCombo.SelectedItem?.ToString() == "val"
            ? "val"
            : "train";
        string imageDirectory = Path.Combine(datasetRoot, "images", split);
        string labelDirectory = Path.Combine(datasetRoot, "labels", split);
        string metaDirectory = Path.Combine(datasetRoot, "meta", split);
        Directory.CreateDirectory(imageDirectory);
        Directory.CreateDirectory(labelDirectory);
        Directory.CreateDirectory(metaDirectory);

        SavedDatasetFiles files = GetDatasetFiles(
            imageDirectory,
            labelDirectory,
            metaDirectory,
            overwriteActiveTile);
        string imagePath = files.ImagePath;
        string labelPath = files.LabelPath;
        string metaPath = files.MetaPath;

        Cv2.ImWrite(imagePath, _sourceImage);
        File.WriteAllLines(labelPath, CreateYoloLabelLines(_sourceImage.Width, _sourceImage.Height));
        File.WriteAllText(metaPath, CreateMetaJson(split, imagePath, labelPath));
        File.WriteAllText(Path.Combine(datasetRoot, "data.yaml"), CreateDataYaml());
    }

    private SavedDatasetFiles GetDatasetFiles(
        string imageDirectory,
        string labelDirectory,
        string metaDirectory,
        bool overwriteActiveTile)
    {
        if (overwriteActiveTile &&
            _activeTile is not null &&
            _savedTileFiles.TryGetValue(_activeTile.Index, out var saved))
        {
            return saved;
        }

        string baseName = CreateDatasetBaseName(imageDirectory);
        SavedDatasetFiles files = new(
            Path.Combine(imageDirectory, baseName + ".jpg"),
            Path.Combine(labelDirectory, baseName + ".txt"),
            Path.Combine(metaDirectory, baseName + ".json"));
        if (overwriteActiveTile && _activeTile is not null)
        {
            _savedTileFiles[_activeTile.Index] = files;
        }

        return files;
    }

    private string CreateDatasetBaseName(string imageDirectory)
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        for (int index = 1; index < 10000; index++)
        {
            string candidate = $"{timestamp}_{index:000}";
            if (!File.Exists(Path.Combine(imageDirectory, candidate + ".jpg")))
            {
                return candidate;
            }
        }

        return $"{timestamp}_{Guid.NewGuid():N}";
    }

    private IEnumerable<string> CreateYoloLabelLines(int imageWidth, int imageHeight)
    {
        foreach (YoloAnnotationBox annotation in _annotations)
        {
            double centerX = (annotation.Bounds.Left + annotation.Bounds.Width / 2.0) / imageWidth;
            double centerY = (annotation.Bounds.Top + annotation.Bounds.Height / 2.0) / imageHeight;
            double width = annotation.Bounds.Width / (double)imageWidth;
            double height = annotation.Bounds.Height / (double)imageHeight;
            yield return string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"{annotation.Class.Id} {centerX:0.######} {centerY:0.######} {width:0.######} {height:0.######}");
        }
    }

    private string CreateMetaJson(string split, string imagePath, string labelPath)
    {
        var meta = new
        {
            app = "ENTcapture2",
            task = "manual_yolo_annotation",
            created_at = DateTimeOffset.Now,
            split,
            source_name = _sourceName,
            source_path = _sourcePath,
            image_path = imagePath,
            label_path = labelPath,
            image_width = _sourceImage?.Width ?? 0,
            image_height = _sourceImage?.Height ?? 0,
            original_source_name = _sourceName,
            original_source_path = _sourcePath,
            tile = _activeTile is null
                ? null
                : new
                {
                    index = _activeTile.Index,
                    x = _activeTile.Bounds.Left,
                    y = _activeTile.Bounds.Top,
                    width = _activeTile.Bounds.Width,
                    height = _activeTile.Bounds.Height,
                    tile_size = _activeTile.TileSize,
                    stride = _activeTile.Stride,
                    overlap = _activeTile.Overlap
                },
            box_count = _annotations.Count,
            classes = YoloAnnotationClass.All.Select(item => new
            {
                id = item.Id,
                name = item.YoloName,
                display_name = item.DisplayName
            }),
            boxes = _annotations.Select(item => new
            {
                id = item.Id,
                class_id = item.Class.Id,
                class_name = item.Class.YoloName,
                x = item.Bounds.Left,
                y = item.Bounds.Top,
                width = item.Bounds.Width,
                height = item.Bounds.Height,
                source = item.Source,
                confidence = item.Confidence,
                original_class_id = item.OriginalClassId,
                created_at = item.CreatedAt
            })
        };
        return JsonSerializer.Serialize(
            meta,
            new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            });
    }

    private static string CreateDataYaml()
    {
        return string.Join(
            Environment.NewLine,
            [
                "path: .",
                "train: images/train",
                "val: images/val",
                "nc: 4",
                "names:",
                "  0: G-cocci",
                "  1: G+cocci",
                "  2: G-bacilli",
                "  3: G+bacilli",
                ""
            ]);
    }

    private void ClearResults()
    {
        _countGrid.Rows.Clear();
        _objectGrid.Rows.Clear();
        _candidateList.Items.Clear();
        _selectedDetectionIndexes.Clear();
        UpdateDetectionControls();
    }

    private void PopulateResults(GramStainAnalysisResult result)
    {
        ClearResults();
        AddCountRow("G+ 球菌", result.Counts.GramPositiveCocci, GramStainPolarity.Positive, BacteriumShape.Coccus);
        AddCountRow("G- 球菌", result.Counts.GramNegativeCocci, GramStainPolarity.Negative, BacteriumShape.Coccus);
        AddCountRow("G+ 桿菌", result.Counts.GramPositiveBacilli, GramStainPolarity.Positive, BacteriumShape.Bacillus);
        AddCountRow("G- 桿菌", result.Counts.GramNegativeBacilli, GramStainPolarity.Negative, BacteriumShape.Bacillus);
        AddCountRow("不明/要確認", result.Counts.Uncertain, GramStainPolarity.Uncertain, BacteriumShape.Uncertain);
        AddCountRow("合計", result.Counts.Total, null, null);

        foreach (string candidate in result.CandidateSummary)
        {
            _candidateList.Items.Add(candidate);
        }

        foreach ((GramStainDetection detection, int index) in result.Detections
                     .Select((item, index) => (item, index))
                     .OrderByDescending(item => item.item.Confidence)
                     .Take(500))
        {
            int rowIndex = _objectGrid.Rows.Add(
                detection.DisplayClass,
                detection.Confidence.ToString("0.00"),
                $"AR {detection.AspectRatio:0.0}");
            _objectGrid.Rows[rowIndex].Tag = index;
        }
    }

    private void AddCountRow(
        string label,
        int count,
        GramStainPolarity? gram,
        BacteriumShape? shape)
    {
        int rowIndex = _countGrid.Rows.Add(label, count.ToString());
        _countGrid.Rows[rowIndex].Tag = new DetectionClassFilter(gram, shape);
    }

    private void ObjectGrid_SelectionChanged(object? sender, EventArgs e)
    {
        if (_lastResult is null || _objectGrid.Focused is false)
        {
            return;
        }

        _selectedDetectionIndexes.Clear();
        foreach (DataGridViewRow row in _objectGrid.SelectedRows)
        {
            if (row.Tag is int detectionIndex)
            {
                _selectedDetectionIndexes.Add(detectionIndex);
            }
        }

        RefreshDetectionOverlay();
    }

    private void CountGrid_SelectionChanged(object? sender, EventArgs e)
    {
        if (_lastResult is null || _countGrid.Focused is false)
        {
            return;
        }

        _selectedDetectionIndexes.Clear();
        foreach (DataGridViewRow row in _countGrid.SelectedRows)
        {
            if (row.Tag is not DetectionClassFilter filter)
            {
                continue;
            }

            for (int index = 0; index < _lastResult.Detections.Count; index++)
            {
                GramStainDetection detection = _lastResult.Detections[index];
                if (filter.Matches(detection))
                {
                    _selectedDetectionIndexes.Add(index);
                }
            }
        }

        RefreshDetectionOverlay();
    }

    private void UpdateDetectionControls()
    {
        bool hasDetections = _lastResult?.Detections.Count > 0;
        _selectedDetectionsOnlyCheck.Enabled = hasDetections;
        _showAllDetectionsButton.Enabled = hasDetections;
        if (!hasDetections)
        {
            _selectedDetectionsOnlyCheck.Checked = false;
        }
    }

    private sealed record YoloAnnotationBox(
        int Id,
        DrawingRectangle Bounds,
        YoloAnnotationClass Class,
        DateTimeOffset CreatedAt,
        string Source = "manual",
        double? Confidence = null,
        int? OriginalClassId = null);

    private sealed record DetectionClassFilter(
        GramStainPolarity? Gram,
        BacteriumShape? Shape)
    {
        public bool Matches(GramStainDetection detection)
        {
            if (Gram is null || Shape is null)
            {
                return true;
            }

            if (Gram == GramStainPolarity.Uncertain ||
                Shape == BacteriumShape.Uncertain)
            {
                return detection.Gram == GramStainPolarity.Uncertain ||
                    detection.Shape == BacteriumShape.Uncertain;
            }

            return detection.Gram == Gram && detection.Shape == Shape;
        }
    }

    private sealed record TileCandidate(
        int Index,
        DrawingRectangle Bounds,
        int TileSize,
        int Stride,
        int Overlap);

    private sealed record SavedDatasetFiles(
        string ImagePath,
        string LabelPath,
        string MetaPath);

    private sealed record YoloAnnotationClass(
        int Id,
        string YoloName,
        string DisplayName,
        string ShortName)
    {
        public static IReadOnlyList<YoloAnnotationClass> All { get; } =
        [
            new(0, "G-cocci", "G- 球菌", "G-球"),
            new(1, "G+cocci", "G+ 球菌", "G+球"),
            new(2, "G-bacilli", "G- 桿菌", "G-桿"),
            new(3, "G+bacilli", "G+ 桿菌", "G+桿")
        ];

        public override string ToString() => DisplayName;
    }
}
