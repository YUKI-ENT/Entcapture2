using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace ENTcapture2.WinForms.Ui;

public sealed class ModernButton : Button
{
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color FillColor { get; set; } = Theme.SurfaceRaised;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color HoverColor { get; set; } = Theme.ButtonHover;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CornerRadius { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool RotateTextClockwise { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool StackTextVertically { get; set; }

    public ModernButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = FillColor;
        ForeColor = Theme.Text;
        Font = Theme.BodyFont();
        Cursor = Cursors.Hand;
        Height = 38;
        Padding = new Padding(12, 0, 12, 0);
        UseVisualStyleBackColor = false;
        MouseEnter += (_, _) => BackColor = HoverColor;
        MouseLeave += (_, _) => BackColor = FillColor;
        Resize += (_, _) => UpdateRegion();
    }

    protected override void OnPaint(PaintEventArgs paintEvent)
    {
        paintEvent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using GraphicsPath path = Theme.RoundedRectangle(ClientRectangle, CornerRadius);
        using var brush = new SolidBrush(BackColor);
        paintEvent.Graphics.FillPath(brush, path);
        if (StackTextVertically)
        {
            DrawStackedText(paintEvent.Graphics);
            return;
        }

        if (RotateTextClockwise)
        {
            DrawRotatedText(paintEvent.Graphics);
            return;
        }

        TextRenderer.DrawText(
            paintEvent.Graphics,
            Text,
            Font,
            ClientRectangle,
            ForeColor,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis);
    }

    private void DrawRotatedText(Graphics graphics)
    {
        GraphicsState state = graphics.Save();
        try
        {
            graphics.TranslateTransform(Width, 0);
            graphics.RotateTransform(90F);
            TextRenderer.DrawText(
                graphics,
                Text,
                Font,
                new Rectangle(0, 0, Height, Width),
                ForeColor,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis);
        }
        finally
        {
            graphics.Restore(state);
        }
    }

    private void DrawStackedText(Graphics graphics)
    {
        string text = Text.Replace(" ", string.Empty);
        if (text.Length == 0)
        {
            return;
        }

        Size measured = TextRenderer.MeasureText(
            graphics,
            text,
            Font,
            Size.Empty,
            TextFormatFlags.NoPadding);
        int lineHeight = Math.Max(1, measured.Height + 1);
        int totalHeight = lineHeight * text.Length;
        int y = Math.Max(0, (Height - totalHeight) / 2);
        foreach (char character in text)
        {
            TextRenderer.DrawText(
                graphics,
                character.ToString(),
                Font,
                new Rectangle(0, y, Width, lineHeight),
                ForeColor,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPadding |
                TextFormatFlags.NoClipping);
            y += lineHeight;
        }
    }

    private void UpdateRegion()
    {
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        using GraphicsPath path = Theme.RoundedRectangle(ClientRectangle, CornerRadius);
        Region = new Region(path);
    }
}

public sealed class ModernRadioButton : RadioButton
{
    private bool _hovered;

    public ModernRadioButton()
    {
        Appearance = Appearance.Button;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Theme.SurfaceRaised;
        ForeColor = Theme.Muted;
        Font = Theme.BodyFont();
        TextAlign = ContentAlignment.MiddleCenter;
        Cursor = Cursors.Hand;
        Height = 38;
        Width = 128;
        AutoEllipsis = true;
        UseVisualStyleBackColor = false;
        MouseEnter += (_, _) =>
        {
            _hovered = true;
            Invalidate();
        };
        MouseLeave += (_, _) =>
        {
            _hovered = false;
            Invalidate();
        };
        Resize += (_, _) => UpdateRegion();
        CheckedChanged += (_, _) =>
        {
            BackColor = Checked ? Theme.Selected : Theme.SurfaceRaised;
            ForeColor = Checked ? Theme.Text : Theme.Muted;
            Invalidate();
        };
    }

    protected override void OnPaint(PaintEventArgs paintEvent)
    {
        paintEvent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Color fill = Checked
            ? Theme.Selected
            : _hovered
                ? Theme.ButtonHover
                : Theme.SurfaceRaised;
        using GraphicsPath path = Theme.RoundedRectangle(
            ClientRectangle,
            0);
        using var brush = new SolidBrush(fill);
        paintEvent.Graphics.FillPath(brush, path);
        TextRenderer.DrawText(
            paintEvent.Graphics,
            Text,
            Font,
            ClientRectangle,
            Checked ? Theme.Text : Theme.Muted,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis);
    }

    private void UpdateRegion()
    {
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        using GraphicsPath path = Theme.RoundedRectangle(
            ClientRectangle,
            0);
        Region = new Region(path);
    }
}

public sealed class SurfacePanel : Panel
{
    public SurfacePanel()
    {
        BackColor = Theme.Surface;
        Padding = new Padding(16);
    }
}
