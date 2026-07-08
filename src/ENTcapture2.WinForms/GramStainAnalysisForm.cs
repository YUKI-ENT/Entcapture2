using ENTcapture2.WinForms.Analysis;
using ENTcapture2.WinForms.Ui;
using OpenCvSharp;
using OpenCvSharp.Extensions;
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
    private readonly NumericUpDown _confidenceInput = new();
    private readonly NumericUpDown _iouInput = new();
    private readonly Label _fileLabel = new();
    private readonly Label _modelLabel = new();
    private readonly Label _statusLabel = new();
    private readonly DataGridView _countGrid = new();
    private readonly DataGridView _objectGrid = new();
    private readonly ListBox _candidateList = new();
    private Mat? _sourceImage;
    private Bitmap? _displayImage;
    private GramStainAnalysisResult? _lastResult;
    private YoloBacteriaDetector? _yoloDetector;

    public GramStainAnalysisForm(string defaultDirectory)
    {
        _defaultDirectory = defaultDirectory;
        InitializeLayout();
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
        MinimumSize = new DrawingSize(1180, 720);
        ClientSize = new DrawingSize(1380, 820);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(16),
            BackColor = Theme.Window
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));

        FlowLayoutPanel toolbar = BuildToolbar();
        var imagePanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Black,
            Padding = new Padding(8),
            Margin = new Padding(0, 0, 12, 0)
        };
        _imageBox.Dock = DockStyle.Fill;
        _imageBox.SizeMode = PictureBoxSizeMode.Zoom;
        _imageBox.BackColor = Color.Black;
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
            WrapContents = false,
            Margin = Padding.Empty,
            BackColor = Theme.Window
        };

        ConfigureToolbarButton(_openImageButton, "静止画を開く", 120);
        _openImageButton.Click += OpenImageButton_Click;
        ConfigureToolbarButton(_ruleAnalyzeButton, "ルール解析", 90);
        _ruleAnalyzeButton.Enabled = false;
        _ruleAnalyzeButton.Click += RuleAnalyzeButton_Click;
        ConfigureToolbarButton(_openModelButton, "ONNXモデル", 110);
        _openModelButton.Click += OpenModelButton_Click;
        ConfigureToolbarButton(_aiAnalyzeButton, "AI解析", 90);
        _aiAnalyzeButton.Enabled = false;
        _aiAnalyzeButton.Click += AiAnalyzeButton_Click;

        ConfigurePercentInput(_confidenceInput, 25);
        ConfigurePercentInput(_iouInput, 45);

        _fileLabel.AutoSize = false;
        _fileLabel.Width = 350;
        _fileLabel.Height = 34;
        _fileLabel.TextAlign = ContentAlignment.MiddleLeft;
        _fileLabel.Text = "画像を選択してください";
        _fileLabel.Tag = "text";
        _modelLabel.AutoSize = false;
        _modelLabel.Width = 220;
        _modelLabel.Height = 34;
        _modelLabel.TextAlign = ContentAlignment.MiddleLeft;
        _modelLabel.Text = "AIモデル未選択";
        _modelLabel.Tag = "text";

        toolbar.Controls.Add(_openImageButton);
        toolbar.Controls.Add(_ruleAnalyzeButton);
        toolbar.Controls.Add(_openModelButton);
        toolbar.Controls.Add(_aiAnalyzeButton);
        toolbar.Controls.Add(CreateToolbarLabel("Conf%", 50));
        toolbar.Controls.Add(_confidenceInput);
        toolbar.Controls.Add(CreateToolbarLabel("IoU%", 42));
        toolbar.Controls.Add(_iouInput);
        toolbar.Controls.Add(_fileLabel);
        toolbar.Controls.Add(_modelLabel);
        return toolbar;
    }

    private static void ConfigureToolbarButton(
        Button button,
        string text,
        int width)
    {
        button.Text = text;
        button.Width = width;
        button.Height = 34;
        button.Margin = new Padding(0, 0, 8, 0);
    }

    private static void ConfigurePercentInput(
        NumericUpDown input,
        int value)
    {
        input.Minimum = 1;
        input.Maximum = 99;
        input.Value = value;
        input.Width = 58;
        input.Height = 34;
        input.Margin = new Padding(0, 3, 8, 0);
    }

    private static Label CreateToolbarLabel(string text, int width)
    {
        return new Label
        {
            Text = text,
            Width = width,
            Height = 34,
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
            Padding = new Padding(12)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 170F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F));
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
        _countGrid.Columns[1].Width = 70;

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
        _objectGrid.Columns[1].Width = 58;
        _objectGrid.Columns[2].Width = 64;

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
        grid.ColumnHeadersHeight = 28;
        grid.RowTemplate.Height = 26;
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

        _sourceImage?.Dispose();
        _sourceImage = image;
        _lastResult?.Dispose();
        _lastResult = null;
        SetDisplayImage(image);
        _fileLabel.Text = path;
        _statusLabel.Text = "画像を読み込みました。解析ボタンを押してください。";
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

        RunAnalysis(
            "ルール解析中...",
            () => _analysisService.Analyze(_sourceImage),
            "ルール解析");
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
