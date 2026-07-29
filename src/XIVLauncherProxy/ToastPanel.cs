namespace XIVLauncherProxy;

internal enum ToastKind
{
    Info,
    Success,
    Warning
}

internal sealed class ToastPanel : Panel
{
    private readonly Label iconLabel = new();
    private readonly Label messageLabel = new();
    private readonly System.Windows.Forms.Timer hideTimer = new();

    public ToastPanel()
    {
        Visible = false;
        Size = new Size(340, 46);
        BackColor = Theme.Header;

        iconLabel.Font = new Font("Segoe UI Symbol", 11F, FontStyle.Bold);
        iconLabel.ForeColor = Color.White;
        iconLabel.TextAlign = ContentAlignment.MiddleCenter;
        iconLabel.Location = new Point(10, 8);
        iconLabel.Size = new Size(30, 30);

        messageLabel.Font = new Font("Microsoft YaHei UI", 9F);
        messageLabel.ForeColor = Color.White;
        messageLabel.TextAlign = ContentAlignment.MiddleLeft;
        messageLabel.AutoEllipsis = true;
        messageLabel.Location = new Point(43, 7);
        messageLabel.Size = new Size(284, 32);

        Controls.AddRange(new Control[] { iconLabel, messageLabel });
        hideTimer.Tick += (_, _) =>
        {
            hideTimer.Stop();
            Visible = false;
        };
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        if (Width > 1 && Height > 1)
        {
            using var path = RoundedShape.Create(new Rectangle(0, 0, Width, Height), 8);
            Region = new Region(path);
        }
    }

    public void ShowMessage(string message, ToastKind kind, int durationMilliseconds = 3200)
    {
        hideTimer.Stop();
        messageLabel.Text = message.Replace('\n', ' ');

        switch (kind)
        {
            case ToastKind.Success:
                BackColor = Theme.Success;
                iconLabel.Text = "✓";
                break;
            case ToastKind.Warning:
                BackColor = Theme.Warning;
                iconLabel.Text = "!";
                break;
            default:
                BackColor = Theme.Header;
                iconLabel.Text = "i";
                break;
        }

        Visible = true;
        BringToFront();

        if (durationMilliseconds > 0)
        {
            hideTimer.Interval = durationMilliseconds;
            hideTimer.Start();
        }
    }
}
