using System.Drawing.Drawing2D;

namespace XIVLauncherProxy;

internal static class Theme
{
    public static readonly Color Background = Color.FromArgb(244, 246, 250);
    public static readonly Color Surface = Color.White;
    public static readonly Color Header = Color.FromArgb(28, 35, 55);
    public static readonly Color Accent = Color.FromArgb(102, 91, 214);
    public static readonly Color AccentHover = Color.FromArgb(86, 75, 197);
    public static readonly Color Text = Color.FromArgb(37, 43, 58);
    public static readonly Color MutedText = Color.FromArgb(111, 119, 138);
    public static readonly Color Border = Color.FromArgb(218, 223, 234);
    public static readonly Color Success = Color.FromArgb(28, 153, 102);
    public static readonly Color Warning = Color.FromArgb(210, 133, 35);
}

internal static class RoundedShape
{
    public static GraphicsPath Create(Rectangle bounds, int radius)
    {
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

internal sealed class CardPanel : Panel
{
    public CardPanel()
    {
        BackColor = Theme.Surface;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        if (Width > 1 && Height > 1)
        {
            using GraphicsPath path = RoundedShape.Create(new Rectangle(0, 0, Width, Height), 10);
            Region = new Region(path);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using GraphicsPath path = RoundedShape.Create(new Rectangle(0, 0, Width - 1, Height - 1), 10);
        using var pen = new Pen(Theme.Border);
        e.Graphics.DrawPath(pen, path);
    }
}

internal sealed class ModernTextBox : UserControl
{
    private readonly TextBox editor = new();
    private bool focused;

    public ModernTextBox()
    {
        BackColor = Theme.Surface;
        Height = 34;
        Padding = new Padding(11, 7, 11, 5);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        editor.BorderStyle = BorderStyle.None;
        editor.BackColor = Theme.Surface;
        editor.ForeColor = Theme.Text;
        editor.Font = new Font("Microsoft YaHei UI", 9.5F);
        editor.Dock = DockStyle.Fill;
        editor.Enter += (_, _) => { focused = true; Invalidate(); };
        editor.Leave += (_, _) => { focused = false; Invalidate(); };
        editor.TextChanged += (_, _) => OnTextChanged(EventArgs.Empty);
        Controls.Add(editor);
    }

    public override string Text
    {
        get => editor.Text;
        set => editor.Text = value;
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        editor.Focus();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using GraphicsPath path = RoundedShape.Create(new Rectangle(0, 0, Width - 1, Height - 1), 6);
        using var pen = new Pen(focused ? Theme.Accent : Theme.Border, focused ? 2F : 1F);
        e.Graphics.DrawPath(pen, path);
    }
}

internal enum ModernButtonStyle
{
    Primary,
    Secondary,
    Dark
}

internal sealed class ModernButton : Button
{
    private bool hovered;
    private bool pressed;
    private ModernButtonStyle buttonStyle = ModernButtonStyle.Primary;

    public ModernButtonStyle ButtonStyle
    {
        get => buttonStyle;
        set
        {
            if (buttonStyle == value)
                return;

            buttonStyle = value;
            Invalidate();
        }
    }

    public ModernButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        UseVisualStyleBackColor = false;
        Cursor = Cursors.Hand;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        hovered = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        hovered = false;
        pressed = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        base.OnMouseDown(mevent);
        if (mevent.Button == MouseButtons.Left)
        {
            pressed = true;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        base.OnMouseUp(mevent);
        if (pressed)
        {
            pressed = false;
            Invalidate();
        }
    }

    protected override void OnKeyDown(KeyEventArgs kevent)
    {
        base.OnKeyDown(kevent);
        if (kevent.KeyCode is Keys.Space or Keys.Enter)
        {
            pressed = true;
            Invalidate();
        }
    }

    protected override void OnKeyUp(KeyEventArgs kevent)
    {
        base.OnKeyUp(kevent);
        if (pressed)
        {
            pressed = false;
            Invalidate();
        }
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        pressed = false;
        Invalidate();
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        if (!Enabled)
        {
            hovered = false;
            pressed = false;
        }
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        // Paint the transparent area first so the pixels outside the rounded path
        // show the card/form behind the button instead of the native button color.
        base.OnPaintBackground(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        Color background;
        Color foreground;
        Color border;

        switch (buttonStyle)
        {
            case ModernButtonStyle.Secondary:
                background = hovered ? Color.FromArgb(246, 247, 251) : Theme.Surface;
                foreground = hovered ? Theme.AccentHover : Theme.Text;
                border = hovered ? Color.FromArgb(174, 168, 229) : Theme.Border;
                break;
            case ModernButtonStyle.Dark:
                background = hovered ? Color.FromArgb(43, 52, 77) : Theme.Header;
                foreground = Color.White;
                border = background;
                break;
            default:
                background = hovered ? Theme.AccentHover : Theme.Accent;
                foreground = Color.White;
                border = background;
                break;
        }

        if (pressed)
            background = ControlPaint.Dark(background, 0.06F);
        if (!Enabled)
        {
            background = Color.FromArgb(224, 227, 234);
            foreground = Theme.MutedText;
            border = Color.FromArgb(214, 218, 227);
        }

        Rectangle buttonBounds = ClientRectangle;
        buttonBounds.Width--;
        buttonBounds.Height--;
        using GraphicsPath path = RoundedShape.Create(buttonBounds, 7);
        using var brush = new SolidBrush(background);
        using var pen = new Pen(border);
        e.Graphics.FillPath(brush, path);
        e.Graphics.DrawPath(pen, path);

        Rectangle textBounds = ClientRectangle;
        if (pressed)
            textBounds.Offset(0, 1);
        TextRenderer.DrawText(e.Graphics, Text, Font, textBounds, foreground,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

        if (Focused && ShowFocusCues && Enabled)
        {
            Rectangle focusBounds = Rectangle.Inflate(ClientRectangle, -4, -4);
            Color focusColor = buttonStyle == ModernButtonStyle.Secondary
                ? Theme.Accent
                : Color.FromArgb(210, Color.White);
            using GraphicsPath focusPath = RoundedShape.Create(focusBounds, 4);
            using var focusPen = new Pen(focusColor) { DashStyle = DashStyle.Dot };
            e.Graphics.DrawPath(focusPen, focusPath);
        }
    }
}
