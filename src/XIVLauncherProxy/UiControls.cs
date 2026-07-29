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

internal sealed class ModernComboBox : Control
{
    private sealed class DropDownColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Theme.Surface;
        public override Color MenuBorder => Theme.Border;
        public override Color MenuItemBorder => Color.Transparent;
        public override Color MenuItemSelected => Color.FromArgb(239, 237, 252);
        public override Color MenuItemSelectedGradientBegin => MenuItemSelected;
        public override Color MenuItemSelectedGradientEnd => MenuItemSelected;
        public override Color ImageMarginGradientBegin => Theme.Surface;
        public override Color ImageMarginGradientMiddle => Theme.Surface;
        public override Color ImageMarginGradientEnd => Theme.Surface;
    }

    private readonly ContextMenuStrip dropDown = new();
    private readonly List<string> items = new();
    private int selectedIndex = -1;
    private bool hovered;

    public ModernComboBox()
    {
        BackColor = Color.Transparent;
        Height = 34;
        TabStop = true;
        Cursor = Cursors.Hand;
        Font = new Font("Microsoft YaHei UI", 9F);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor, true);

        dropDown.AutoSize = false;
        dropDown.ShowImageMargin = false;
        dropDown.ShowCheckMargin = false;
        dropDown.BackColor = Theme.Surface;
        dropDown.ForeColor = Theme.Text;
        dropDown.Font = Font;
        dropDown.Padding = new Padding(2);
        dropDown.Renderer = new ToolStripProfessionalRenderer(new DropDownColors())
        {
            RoundedEdges = true
        };
        dropDown.Closed += (_, _) => Invalidate();
    }

    public List<string> Items => items;

    public int SelectedIndex
    {
        get => selectedIndex;
        set
        {
            int newValue = value >= 0 && value < items.Count ? value : -1;
            if (selectedIndex == newValue)
                return;

            selectedIndex = newValue;
            Invalidate();
        }
    }

    public string? SelectedItem
    {
        get => selectedIndex >= 0 && selectedIndex < items.Count ? items[selectedIndex] : null;
        set => SelectedIndex = value is null
            ? -1
            : items.FindIndex(item => string.Equals(item, value, StringComparison.Ordinal));
    }

    private void ShowDropDown()
    {
        if (dropDown.Visible || items.Count == 0)
            return;

        dropDown.Items.Clear();
        foreach (string item in items)
        {
            var menuItem = new ToolStripMenuItem(item)
            {
                AutoSize = false,
                Size = new Size(Math.Max(Width - 4, 80), 30),
                Padding = new Padding(10, 0, 8, 0),
                ForeColor = Theme.Text,
                BackColor = Theme.Surface
            };
            menuItem.Click += (_, _) =>
            {
                SelectedItem = item;
                Focus();
            };
            dropDown.Items.Add(menuItem);
        }

        dropDown.Size = new Size(Width, items.Count * 30 + dropDown.Padding.Vertical + 2);
        dropDown.Show(this, new Point(0, Height + 2));
        Invalidate();
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
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
            return;

        Focus();
        ShowDropDown();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode is Keys.Enter or Keys.Space || (e.KeyCode == Keys.Down && e.Alt))
        {
            ShowDropDown();
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Keys.Down && selectedIndex < items.Count - 1)
        {
            SelectedIndex++;
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Up && selectedIndex > 0)
        {
            SelectedIndex--;
            e.Handled = true;
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
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        bool highlighted = Focused || dropDown.Visible;
        Color background = hovered || highlighted
            ? Color.FromArgb(249, 249, 253)
            : Theme.Surface;
        Color border = highlighted
            ? Theme.Accent
            : hovered ? Color.FromArgb(190, 185, 228) : Theme.Border;

        Rectangle bounds = ClientRectangle;
        bounds.Width--;
        bounds.Height--;
        using GraphicsPath path = RoundedShape.Create(bounds, 6);
        using var brush = new SolidBrush(background);
        using var pen = new Pen(border, highlighted ? 2F : 1F);
        e.Graphics.FillPath(brush, path);
        e.Graphics.DrawPath(pen, path);

        Rectangle textBounds = new(11, 0, Math.Max(0, Width - 36), Height);
        TextRenderer.DrawText(e.Graphics, SelectedItem ?? string.Empty, Font, textBounds, Theme.Text,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

        int centerX = Width - 17;
        int centerY = Height / 2;
        using var arrowPen = new Pen(highlighted ? Theme.Accent : Theme.MutedText, 1.7F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        e.Graphics.DrawLines(arrowPen, new[]
        {
            new Point(centerX - 4, centerY - 2),
            new Point(centerX, centerY + 2),
            new Point(centerX + 4, centerY - 2)
        });
    }
}

internal sealed class ModernCheckBox : CheckBox
{
    private bool hovered;

    public ModernCheckBox()
    {
        AutoSize = true;
        BackColor = Theme.Surface;
        Cursor = Cursors.Hand;
        Font = new Font("Microsoft YaHei UI", 9F);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        Size textSize = TextRenderer.MeasureText(Text, Font, Size.Empty,
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        return new Size(27 + textSize.Width, Math.Max(24, textSize.Height + 6));
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
        Invalidate();
    }

    protected override void OnCheckedChanged(EventArgs e)
    {
        base.OnCheckedChanged(e);
        Invalidate();
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaintBackground(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        Rectangle box = new(1, (Height - 18) / 2, 17, 17);
        Color border = Checked || Focused
            ? Theme.Accent
            : hovered ? Color.FromArgb(158, 150, 220) : Theme.Border;
        Color fill = Checked
            ? (hovered ? Theme.AccentHover : Theme.Accent)
            : hovered ? Color.FromArgb(247, 246, 253) : Theme.Surface;

        using GraphicsPath boxPath = RoundedShape.Create(box, 4);
        using var fillBrush = new SolidBrush(Enabled ? fill : Color.FromArgb(235, 237, 242));
        using var borderPen = new Pen(Enabled ? border : Color.FromArgb(208, 212, 222));
        e.Graphics.FillPath(fillBrush, boxPath);
        e.Graphics.DrawPath(borderPen, boxPath);

        if (Checked)
        {
            using var checkPen = new Pen(Enabled ? Color.White : Theme.MutedText, 2F)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            e.Graphics.DrawLines(checkPen, new[]
            {
                new Point(box.Left + 4, box.Top + 9),
                new Point(box.Left + 7, box.Top + 12),
                new Point(box.Left + 13, box.Top + 5)
            });
        }

        Rectangle textBounds = new(27, 0, Math.Max(0, Width - 27), Height);
        TextRenderer.DrawText(e.Graphics, Text, Font, textBounds,
            Enabled ? ForeColor : Theme.MutedText,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
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
