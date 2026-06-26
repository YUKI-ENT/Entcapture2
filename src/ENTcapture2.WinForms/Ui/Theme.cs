using System.Drawing.Drawing2D;

namespace ENTcapture2.WinForms.Ui;

internal static class Theme
{
    public static readonly Color Window = Color.FromArgb(12, 17, 26);
    public static readonly Color Surface = Color.FromArgb(20, 27, 39);
    public static readonly Color SurfaceRaised = Color.FromArgb(28, 37, 52);
    public static readonly Color Border = Color.FromArgb(51, 65, 85);
    public static readonly Color Text = Color.FromArgb(235, 241, 248);
    public static readonly Color Muted = Color.FromArgb(148, 163, 184);
    public static readonly Color Accent = Color.FromArgb(15, 118, 110);
    public static readonly Color AccentHover = Color.FromArgb(13, 148, 136);
    public static readonly Color Selected = Color.FromArgb(22, 78, 99);
    public static readonly Color ButtonHover = Color.FromArgb(40, 52, 70);
    public static readonly Color AccentBright = Color.FromArgb(45, 212, 191);
    public static readonly Color Danger = Color.FromArgb(244, 63, 94);

    public static Font HeadingFont(float size) =>
        new("Yu Gothic UI", size, FontStyle.Bold, GraphicsUnit.Point);

    public static Font BodyFont(float size = 10) =>
        new("Yu Gothic UI", size, FontStyle.Regular, GraphicsUnit.Point);

    public static void Apply(Control root)
    {
        ApplyControl(root);
        foreach (Control child in root.Controls)
        {
            Apply(child);
        }
    }

    public static void ApplyButton(Button button, bool accent = false)
    {
        button.BackColor = accent ? Accent : SurfaceRaised;
        button.ForeColor = Text;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = accent ? AccentHover : Border;
        button.FlatAppearance.BorderSize = 0;
        button.UseVisualStyleBackColor = false;
        button.Cursor = Cursors.Hand;
    }

    private static void ApplyControl(Control control)
    {
        string role = control.Tag as string ?? string.Empty;

        switch (control)
        {
            case Form form:
                form.BackColor = Window;
                form.ForeColor = Text;
                form.Font = BodyFont();
                break;
            case DataGridView grid:
                grid.BackgroundColor = Window;
                grid.BorderStyle = BorderStyle.None;
                grid.EnableHeadersVisualStyles = false;
                grid.GridColor = Border;
                grid.ColumnHeadersDefaultCellStyle.BackColor = SurfaceRaised;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = Text;
                grid.RowHeadersDefaultCellStyle.BackColor = SurfaceRaised;
                grid.RowHeadersDefaultCellStyle.ForeColor = Text;
                grid.DefaultCellStyle.BackColor = Surface;
                grid.DefaultCellStyle.ForeColor = Text;
                grid.DefaultCellStyle.SelectionBackColor = Accent;
                grid.DefaultCellStyle.SelectionForeColor = Color.White;
                break;
            case TextBox textBox:
                textBox.BackColor = SurfaceRaised;
                textBox.ForeColor = Text;
                textBox.BorderStyle = BorderStyle.FixedSingle;
                break;
            case ComboBox comboBox:
                comboBox.BackColor = SurfaceRaised;
                comboBox.ForeColor = Text;
                comboBox.FlatStyle = FlatStyle.Flat;
                break;
            case NumericUpDown numeric:
                numeric.BackColor = SurfaceRaised;
                numeric.ForeColor = Text;
                numeric.BorderStyle = BorderStyle.FixedSingle;
                break;
            case ListBox listBox:
                listBox.BackColor = Window;
                listBox.ForeColor = Text;
                listBox.BorderStyle = BorderStyle.None;
                break;
            case TabControl tabControl:
                tabControl.BackColor = Window;
                tabControl.ForeColor = Text;
                break;
            case TabPage tabPage:
                tabPage.BackColor = Window;
                tabPage.ForeColor = Text;
                tabPage.UseVisualStyleBackColor = false;
                break;
            case Button button:
                ApplyButton(button, role == "accent");
                break;
            case Label label:
                label.BackColor = Color.Transparent;
                label.ForeColor = role switch
                {
                    "accent" => AccentBright,
                    "text" => Text,
                    _ => Muted
                };
                break;
            case CheckBox checkBox:
                checkBox.BackColor = Color.Transparent;
                checkBox.ForeColor = Text;
                break;
            case Panel or TableLayoutPanel or FlowLayoutPanel:
                control.BackColor = role switch
                {
                    "window" => Window,
                    "raised" => SurfaceRaised,
                    _ => Surface
                };
                control.ForeColor = Text;
                break;
        }
    }

    public static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        if (radius <= 0)
        {
            var rectanglePath = new GraphicsPath();
            rectanglePath.AddRectangle(bounds);
            return rectanglePath;
        }

        int diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
