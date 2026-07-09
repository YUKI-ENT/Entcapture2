using ENTcapture2.WinForms.Analysis;
using ENTcapture2.WinForms.Ui;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using DrawingPoint = System.Drawing.Point;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingSize = System.Drawing.Size;

namespace ENTcapture2.WinForms;

public sealed class GramStainAnalysisForm : Form
{
    private readonly string _defaultDirectory;
    private readonly GramStainAnalysisService _analysisService = new();
    private readonly PictureBox _imageBox = new();
    private readonly Button _openImageButton = new();
    private readonly Button _ruleAnalyzeButton = new();
    private readonly Button _openModelButton = new();
    private readonly Button _aiAnalyzeButton = new();
    private readonly NumericUpDown _minTargetSizeInput = new();
    private readonly NumericUpDown _maxTargetSizeInput = new();
    private readonly NumericUpDown _confidenceInput = new();
    private readonly NumericUpDown _iouInput = new();
    private readonly Label _fileLabel = new();
    private readonly Label _modelLabel = new();
    private readonly Label _measureLabel = new();
    private readonly Label _statusLabel = new();
    private readonly DataGridView _countGrid = new();
    private readonly DataGridView _objectGrid = new();
    private readonly ListBox _candidateList = new();
    private Mat? _sourceImage;
    private Bitmap? _displayImage;
    private GramStainAnalysisResult? _lastResult;
    private YoloBacteriaDetector? _yoloDetector;
    private bool _isDraggingMeasure;
    private DrawingPoint _dragStart;
    private DrawingPoint _dragEnd;

    public GramStainAnalysisForm(string defaultDirectory, Bitmap? initialImage = null)
    {
        _defaultDirectory = defaultDirectory;
        InitializeLayout();
        if (initialImage is not null)
        {
            LoadBitmap(initialImage, "現在の表示画像");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _sourceImage?.Dispose();
            _displayImage?.Dispose();
            _lastResult?.Dispose();
            _yoloDetector?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeLayout()
    {
        Text = "細菌解析";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new DrawingSize(980, 640);
        ClientSize = new DrawingSize(1180, 740);

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
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));

        FlowLayoutPanel toolbar = BuildToolbar();
        var imagePanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Black,
            Padding = new Padding(6),
            Margin = new Padding(0, 0, 10, 0)
        };
        _imageBox.Dock = DockStyle.Fill;
        _imageBox.SizeMode = PictureBoxSizeMode.Zoom;
        _imageBox.BackColor = Color.Black;
        _imageBox.MouseDown += ImageBox_MouseDown;
        _imageBox.MouseMove += ImageBox_MouseMove;
        _imageBox.MouseUp += ImageBox_MouseUp;
        _imageBox.Paint += ImageBox_Paint;
        imagePanel.Controls.Add(_imageBox);

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
        Theme.ApplyButton(_ruleAnalyzeButton);
        Theme.ApplyButton(_openModelButton);
        Theme.ApplyButton(_aiAnalyzeButton, true);
        ConfigureGrid(_countGrid);
        ConfigureGrid(_objectGrid);
    }

    private FlowLayoutPanel BuildToolbar()
    {
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = Padding.Empty,
            BackColor = Theme.Window
        };

        ConfigureToolbarButton(_openImageButton, "静止画を開く", 112);
        _openImageButton.Click += OpenImageButton_Click;
        ConfigureToolbarButton(_ruleAnalyzeButton, "ルール解析", 88);
        _ruleAnalyzeButton.Enabled = false;
        _ruleAnalyzeButton.Click += RuleAnalyzeButton_Click;
        ConfigureToolbarButton(_openModelButton, "ONNXモデル", 104);
        _openModelButton.Click += OpenModelButton_Click;
        ConfigureToolbarButton(_aiAnalyzeButton, "AI解析", 76);
        _aiAnalyzeButton.Enabled = false;
        _aiAnalyzeButton.Click += AiAnalyzeButton_Click;

        ConfigurePixelInput(_minTargetSizeInput, 10);
        ConfigurePixelInput(_maxTargetSizeInput, 100);
        ConfigurePercentInput(_confidenceInput, 25);
        ConfigurePercentInput(_iouInput, 45);

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
        _measureLabel.AutoSize = false;
        _measureLabel.Width = 360;
        _measureLabel.Height = 26;
        _measureLabel.TextAlign = ContentAlignment.MiddleLeft;
        _measureLabel.Text = "画像上をドラッグするとpx測定できます";
        _measureLabel.Tag = "text";

        toolbar.Controls.Add(_openImageButton);
        toolbar.Controls.Add(_ruleAnalyzeButton);
        toolbar.Controls.Add(CreateToolbarLabel("対象px", 54));
        toolbar.Controls.Add(_minTargetSizeInput);
        toolbar.Controls.Add(CreateToolbarLabel("-", 12));
        toolbar.Controls.Add(_maxTargetSizeInput);
        toolbar.Controls.Add(_openModelButton);
        toolbar.Controls.Add(_aiAnalyzeButton);
        toolbar.Controls.Add(CreateToolbarLabel("Conf%", 48));
        toolbar.Controls.Add(_confidenceInput);
        toolbar.Controls.Add(CreateToolbarLabel("IoU%", 40));
        toolbar.Controls.Add(_iouInput);
        toolbar.Controls.Add(_fileLabel);
        toolbar.Controls.Add(_modelLabel);
        toolbar.Controls.Add(_measureLabel);
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

    private static void ConfigurePixelInput(NumericUpDown input, int value)
    {
        input.Minimum = 1;
        input.Maximum = 10000;
        input.Value = value;
        input.Width = 62;
        input.Height = 32;
        input.Margin = new Padding(0, 2, 6, 4);
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
        grid.MultiSelect = false;
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
        using var dialog = new OpenFileDialog
        {
            Title = "YOLO ONNXモデルを選択",
            Filter = "ONNX model|*.onnx|すべてのファイル|*.*",
            InitialDirectory = Directory.Exists(_defaultDirectory)
                ? _defaultDirectory
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            _yoloDetector?.Dispose();
            _yoloDetector = new YoloBacteriaDetector(dialog.FileName);
            _modelLabel.Text = Path.GetFileName(dialog.FileName);
            _aiAnalyzeButton.Enabled = _sourceImage is not null;
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
                "細菌解析",
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
                "細菌解析",
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
        _sourceImage?.Dispose();
        _sourceImage = image;
        _lastResult?.Dispose();
        _lastResult = null;
        SetDisplayImage(image);
        _fileLabel.Text = sourceName;
        _statusLabel.Text = "画像を読み込みました。対象pxを確認してから解析してください。";
        _ruleAnalyzeButton.Enabled = true;
        _aiAnalyzeButton.Enabled = _yoloDetector is not null;
        ClearResults();
    }

    private void RuleAnalyzeButton_Click(object? sender, EventArgs e)
    {
        if (_sourceImage is null)
        {
            return;
        }

        GramStainRuleAnalysisOptions options = CreateRuleOptions();
        RunAnalysis(
            "ルール解析中...",
            () => _analysisService.Analyze(_sourceImage, options),
            "ルール解析");
    }

    private GramStainRuleAnalysisOptions CreateRuleOptions()
    {
        int min = decimal.ToInt32(_minTargetSizeInput.Value);
        int max = decimal.ToInt32(_maxTargetSizeInput.Value);
        if (max < min)
        {
            max = min;
            _maxTargetSizeInput.Value = max;
        }

        return new GramStainRuleAnalysisOptions(min, max);
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
            "AI解析中...",
            () => _yoloDetector.Analyze(_sourceImage, confidence, iou),
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
            _lastResult = analyze();
            SetDisplayImage(_lastResult.OverlayImage);
            PopulateResults(_lastResult);
            _statusLabel.Text =
                $"{label}完了: 検出候補 {_lastResult.Counts.Total} 個。結果は参考値です。";
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                $"{label}に失敗しました。\r\n{exception.Message}",
                "細菌解析",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            _statusLabel.Text = $"{label}に失敗しました";
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void SetDisplayImage(Mat image)
    {
        Bitmap next = BitmapConverter.ToBitmap(image);
        _imageBox.Image = next;
        _displayImage?.Dispose();
        _displayImage = next;
        _imageBox.Invalidate();
    }

    private void ImageBox_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _sourceImage is null)
        {
            return;
        }

        _isDraggingMeasure = true;
        _dragStart = e.Location;
        _dragEnd = e.Location;
        _imageBox.Invalidate();
    }

    private void ImageBox_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_isDraggingMeasure)
        {
            return;
        }

        _dragEnd = e.Location;
        _imageBox.Invalidate();
    }

    private void ImageBox_MouseUp(object? sender, MouseEventArgs e)
    {
        if (!_isDraggingMeasure || _sourceImage is null)
        {
            return;
        }

        _isDraggingMeasure = false;
        _dragEnd = e.Location;
        if (!TryMapClientPointToImage(_dragStart, out DrawingPoint imageStart) ||
            !TryMapClientPointToImage(_dragEnd, out DrawingPoint imageEnd))
        {
            _imageBox.Invalidate();
            return;
        }

        int width = Math.Abs(imageEnd.X - imageStart.X);
        int height = Math.Abs(imageEnd.Y - imageStart.Y);
        int longSide = Math.Max(width, height);
        if (longSide <= 0)
        {
            _imageBox.Invalidate();
            return;
        }

        int min = Math.Max(1, (int)Math.Round(longSide * 0.6));
        int max = Math.Max(min, (int)Math.Round(longSide * 1.6));
        _minTargetSizeInput.Value = Math.Clamp(min, 1, 10000);
        _maxTargetSizeInput.Value = Math.Clamp(max, 1, 10000);
        _measureLabel.Text =
            $"測定: {width}x{height}px 長辺{longSide}px -> 対象px {min}-{max}";
        _imageBox.Invalidate();
    }

    private void ImageBox_Paint(object? sender, PaintEventArgs e)
    {
        if (!_isDraggingMeasure)
        {
            return;
        }

        DrawingRectangle rect = NormalizeRectangle(_dragStart, _dragEnd);
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        using var pen = new Pen(Color.FromArgb(255, 45, 212, 191), 2);
        e.Graphics.DrawRectangle(pen, rect);
    }

    private bool TryMapClientPointToImage(
        DrawingPoint clientPoint,
        out DrawingPoint imagePoint)
    {
        imagePoint = DrawingPoint.Empty;
        if (_sourceImage is null || _imageBox.ClientSize.Width <= 0 ||
            _imageBox.ClientSize.Height <= 0)
        {
            return false;
        }

        DrawingRectangle imageRect = GetZoomedImageRectangle(
            _imageBox.ClientSize,
            new DrawingSize(_sourceImage.Width, _sourceImage.Height));
        if (!imageRect.Contains(clientPoint))
        {
            return false;
        }

        double scaleX = _sourceImage.Width / (double)imageRect.Width;
        double scaleY = _sourceImage.Height / (double)imageRect.Height;
        int x = (int)Math.Round((clientPoint.X - imageRect.Left) * scaleX);
        int y = (int)Math.Round((clientPoint.Y - imageRect.Top) * scaleY);
        imagePoint = new DrawingPoint(
            Math.Clamp(x, 0, _sourceImage.Width - 1),
            Math.Clamp(y, 0, _sourceImage.Height - 1));
        return true;
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

    private void ClearResults()
    {
        _countGrid.Rows.Clear();
        _objectGrid.Rows.Clear();
        _candidateList.Items.Clear();
    }

    private void PopulateResults(GramStainAnalysisResult result)
    {
        ClearResults();
        AddCountRow("G+ 球菌", result.Counts.GramPositiveCocci);
        AddCountRow("G- 球菌", result.Counts.GramNegativeCocci);
        AddCountRow("G+ 桿菌", result.Counts.GramPositiveBacilli);
        AddCountRow("G- 桿菌", result.Counts.GramNegativeBacilli);
        AddCountRow("不明/要確認", result.Counts.Uncertain);
        AddCountRow("合計", result.Counts.Total);

        foreach (string candidate in result.CandidateSummary)
        {
            _candidateList.Items.Add(candidate);
        }

        foreach (GramStainDetection detection in result.Detections
                     .OrderByDescending(item => item.Confidence)
                     .Take(500))
        {
            _objectGrid.Rows.Add(
                detection.DisplayClass,
                detection.Confidence.ToString("0.00"),
                $"AR {detection.AspectRatio:0.0}");
        }
    }

    private void AddCountRow(string label, int count)
    {
        _countGrid.Rows.Add(label, count.ToString());
    }
}
