using ENTcapture2.Core.Models;
using ENTcapture2.WinForms.Ui;

namespace ENTcapture2.WinForms;

public sealed class ExternalPreviewSettingsForm : Form
{
    private readonly CheckBox _enabled = new() { Text = "このプリセットで拡張表示する", AutoSize = true };
    private readonly ComboBox _monitor = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _mode = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _alignment = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _left = Number(0, 16384);
    private readonly NumericUpDown _top = Number(0, 16384);
    private readonly NumericUpDown _width = Number(1, 16384);
    private readonly NumericUpDown _height = Number(1, 16384);
    private readonly NumericUpDown _magnification = Number(10, 400);
    private readonly NumericUpDown _offsetX = Number(-16384, 16384);
    private readonly NumericUpDown _offsetY = Number(-16384, 16384);

    private sealed record MonitorChoice(string DeviceName, string Label)
    {
        public override string ToString() => Label;
    }

    public ExternalPreviewSettings Settings { get; private set; }

    public ExternalPreviewSettingsForm(ExternalPreviewSettings settings)
    {
        Settings = settings.Clone();
        Text = "プリセットの拡張モニター表示";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(580, 660);
        MinimumSize = new Size(520, 480);
        Font = Theme.BodyFont();
        BackColor = Theme.Window;
        ForeColor = Theme.Text;

        _monitor.Items.Add(new MonitorChoice(string.Empty, "自動（メインフォームとは別のモニター）"));
        foreach (Screen screen in Screen.AllScreens)
        {
            _monitor.Items.Add(new MonitorChoice(screen.DeviceName,
                $"{screen.DeviceName}  {screen.Bounds.Width}×{screen.Bounds.Height}"));
        }
        int monitorIndex = _monitor.Items.Cast<MonitorChoice>().ToList()
            .FindIndex(item => item.DeviceName == settings.MonitorDeviceName);
        if (monitorIndex < 0)
        {
            _monitor.Items.Add(new MonitorChoice(settings.MonitorDeviceName,
                $"{settings.MonitorDeviceName}（未接続）"));
            monitorIndex = _monitor.Items.Count - 1;
        }
        _monitor.SelectedIndex = monitorIndex;
        _mode.Items.AddRange(["Zoom（領域を埋める・はみ出しを切り取り）", "Fit（全体を収める）"]);
        _mode.SelectedIndex = settings.Mode == ExternalPreviewMode.Fit ? 1 : 0;
        _alignment.Items.AddRange(["左寄せ", "中央", "右寄せ"]);
        _alignment.SelectedIndex = Math.Clamp((int)settings.Alignment, 0, 2);
        _enabled.Checked = settings.Enabled;
        SetValue(_left, settings.Left);
        SetValue(_top, settings.Top);
        SetValue(_width, settings.Width);
        SetValue(_height, settings.Height);
        SetValue(_magnification, double.IsFinite(settings.Magnification)
            ? Math.Clamp(settings.Magnification * 100, 10, 400) : 100);
        SetValue(_offsetX, settings.OffsetX);
        SetValue(_offsetY, settings.OffsetY);
        // Subscribe after loading saved values so opening the dialog preserves custom regions.
        _monitor.SelectedIndexChanged += (_, _) => SetSelectedMonitorRegion();

        var editor = new TableLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2,
            Padding = new Padding(18)
        };
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddRow(editor, "拡張表示", _enabled);
        AddRow(editor, "表示モニター", _monitor);
        AddRow(editor, "領域のX（px）", _left);
        AddRow(editor, "領域のY（px）", _top);
        AddRow(editor, "領域の幅（px）", _width);
        AddRow(editor, "領域の高さ（px）", _height);
        AddRow(editor, "表示方法", _mode);
        AddRow(editor, "追加倍率（%）", _magnification);
        AddRow(editor, "横方向の基準", _alignment);
        AddRow(editor, "映像の横移動（px）", _offsetX);
        AddRow(editor, "映像の縦移動（px）", _offsetY);
        var help = new Label
        {
            AutoSize = true, MaximumSize = new Size(500, 0), Margin = new Padding(0, 12, 0, 0),
            Text = "モニターを選ぶと領域を画面全体に設定します。必要に応じて変更できます。\r\n領域のX・Yは選択モニターの左上が原点です。\r\n領域はモニター内に収めます。映像の移動は右・下が正です。\r\n1280×1024に1920×1080を表示する例：Zoom、100%、左寄せ。\r\n拡張表示中は枠・スクロールバー・再生操作部を表示しません。"
        };
        int helpRow = editor.RowCount;
        editor.RowCount++;
        editor.Controls.Add(help, 0, helpRow);
        editor.SetColumnSpan(help, 2);
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        scroll.Controls.Add(editor);
        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(12),
            FlowDirection = FlowDirection.RightToLeft
        };
        var ok = new Button { Text = "OK", AutoSize = true };
        ok.Click += (_, _) =>
        {
            Settings = new ExternalPreviewSettings
            {
                Enabled = _enabled.Checked,
                MonitorDeviceName = ((MonitorChoice)_monitor.SelectedItem!).DeviceName,
                Left = (int)_left.Value, Top = (int)_top.Value,
                Width = (int)_width.Value, Height = (int)_height.Value,
                Mode = _mode.SelectedIndex == 1 ? ExternalPreviewMode.Fit : ExternalPreviewMode.Zoom,
                Magnification = (double)_magnification.Value / 100,
                Alignment = (ExternalPreviewAlignment)_alignment.SelectedIndex,
                OffsetX = (int)_offsetX.Value, OffsetY = (int)_offsetY.Value
            };
            DialogResult = DialogResult.OK;
        };
        var cancel = new Button { Text = "キャンセル", AutoSize = true, DialogResult = DialogResult.Cancel };
        footer.Controls.Add(ok);
        footer.Controls.Add(cancel);
        Controls.Add(scroll);
        Controls.Add(footer);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private void SetSelectedMonitorRegion()
    {
        if (_monitor.SelectedItem is not MonitorChoice choice) return;
        Screen mainScreen = Screen.FromControl(Owner ?? this);
        Screen? screen = string.IsNullOrEmpty(choice.DeviceName)
            ? Screen.AllScreens.FirstOrDefault(item => item.DeviceName != mainScreen.DeviceName)
            : Screen.AllScreens.FirstOrDefault(item => item.DeviceName == choice.DeviceName);
        if (screen is null) return;
        SetValue(_left, 0);
        SetValue(_top, 0);
        SetValue(_width, screen.Bounds.Width);
        SetValue(_height, screen.Bounds.Height);
    }

    private static NumericUpDown Number(int minimum, int maximum) => new()
    {
        Minimum = minimum, Maximum = maximum, BackColor = Theme.SurfaceRaised,
        ForeColor = Theme.Text, Width = 140
    };

    private static void SetValue(NumericUpDown control, double value) =>
        control.Value = Math.Clamp((decimal)value, control.Minimum, control.Maximum);

    private static void AddRow(TableLayoutPanel editor, string text, Control control)
    {
        int row = editor.RowCount++;
        editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editor.Controls.Add(new Label
        {
            Text = text, AutoSize = true, Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 8, 8, 8), ForeColor = Theme.Muted
        }, 0, row);
        control.Margin = new Padding(0, 4, 0, 4);
        if (control is ComboBox) control.Dock = DockStyle.Fill;
        editor.Controls.Add(control, 1, row);
    }
}
