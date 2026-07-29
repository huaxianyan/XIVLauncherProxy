using System.Net.Sockets;

namespace XIVLauncherProxy;

internal sealed class MainForm : Form
{
    private readonly ModernTextBox launcherPathTextBox = new();
    private readonly ModernTextBox proxyUrlTextBox = new();
    private readonly CheckBox setAllProxyCheckBox = new();
    private readonly CheckBox bypassLocalCheckBox = new();
    private readonly ModernButton testProxyButton = new();
    private readonly ToastPanel toast = new();
    private readonly string? initialMessage;

    public MainForm(string? initialMessage = null)
    {
        this.initialMessage = initialMessage;
        InitializeWindow();
        LoadConfig();
    }

    private void InitializeWindow()
    {
        Text = "XIVLauncherProxy";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        ClientSize = new Size(760, 482);
        MinimumSize = Size;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = Theme.Background;

        var header = new Panel
        {
            BackColor = Theme.Header,
            Location = Point.Empty,
            Size = new Size(760, 104),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        var accentBar = new Panel
        {
            BackColor = Theme.Accent,
            Location = new Point(0, 100),
            Size = new Size(760, 4),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
        };

        var logo = new PictureBox
        {
            Image = Icon?.ToBitmap(),
            SizeMode = PictureBoxSizeMode.Zoom,
            Location = new Point(28, 24),
            Size = new Size(52, 52)
        };

        var titleLabel = new Label
        {
            Text = "XIVLauncherProxy",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 17F, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(96, 20)
        };

        var subtitleLabel = new Label
        {
            Text = "仅为 XIVLauncher 及其子进程应用本地代理",
            ForeColor = Color.FromArgb(184, 193, 214),
            Font = new Font("Microsoft YaHei UI", 9F),
            AutoSize = true,
            Location = new Point(99, 59)
        };

        header.Controls.AddRange(new Control[]
        {
            logo, titleLabel, subtitleLabel, accentBar
        });

        var card = new CardPanel
        {
            Location = new Point(28, 124),
            Size = new Size(704, 292)
        };

        var sectionTitle = new Label
        {
            Text = "连接设置",
            ForeColor = Theme.Text,
            Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(22, 18)
        };

        var launcherLabel = CreateFieldLabel("XIVLauncher 程序路径", 22, 55);
        launcherPathTextBox.Location = new Point(22, 77);
        launcherPathTextBox.Size = new Size(542, 34);

        var browseButton = new ModernButton
        {
            Text = "浏览",
            ButtonStyle = ModernButtonStyle.Secondary,
            Location = new Point(576, 77),
            Size = new Size(104, 34)
        };
        browseButton.Click += BrowseButton_Click;

        var proxyLabel = CreateFieldLabel("本地代理地址", 22, 126);
        proxyUrlTextBox.Location = new Point(22, 148);
        proxyUrlTextBox.Size = new Size(542, 34);

        testProxyButton.Text = "测试连接";
        testProxyButton.ButtonStyle = ModernButtonStyle.Secondary;
        testProxyButton.Location = new Point(576, 148);
        testProxyButton.Size = new Size(104, 34);
        testProxyButton.Click += TestProxyButton_Click;

        ConfigureCheckBox(setAllProxyCheckBox,
            "同时设置 ALL_PROXY", new Point(22, 203));
        ConfigureCheckBox(bypassLocalCheckBox,
            "本地地址不经过代理（NO_PROXY）", new Point(220, 203));

        var inheritanceHint = new Label
        {
            Text = "仅影响本次启动的进程树，不修改系统代理。",
            ForeColor = Theme.MutedText,
            Font = new Font("Microsoft YaHei UI", 8.5F),
            AutoSize = true,
            Location = new Point(22, 251)
        };

        card.Controls.AddRange(new Control[]
        {
            sectionTitle,
            launcherLabel, launcherPathTextBox, browseButton,
            proxyLabel, proxyUrlTextBox, testProxyButton,
            setAllProxyCheckBox, bypassLocalCheckBox, inheritanceHint
        });

        toast.Location = new Point(392, 29);

        var saveButton = new ModernButton
        {
            Text = "保存设置",
            ButtonStyle = ModernButtonStyle.Secondary,
            Location = new Point(308, 430),
            Size = new Size(104, 36)
        };
        saveButton.Click += (_, _) => SaveConfig(showSuccess: true);

        var saveAndLaunchButton = new ModernButton
        {
            Text = "保存并启动",
            ButtonStyle = ModernButtonStyle.Primary,
            Location = new Point(422, 430),
            Size = new Size(128, 36)
        };
        saveAndLaunchButton.Click += SaveAndLaunchButton_Click;

        var shortcutButton = new ModernButton
        {
            Text = "生成快捷方式",
            ButtonStyle = ModernButtonStyle.Dark,
            Location = new Point(560, 430),
            Size = new Size(172, 36)
        };
        shortcutButton.Click += ShortcutButton_Click;

        Controls.AddRange(new Control[]
        {
            header, card, saveButton, saveAndLaunchButton, shortcutButton, toast
        });

        Shown += (_, _) =>
        {
            ActiveControl = saveAndLaunchButton;

            if (!string.IsNullOrWhiteSpace(this.initialMessage))
            {
                toast.ShowMessage(this.initialMessage, ToastKind.Warning, 5000);
            }
        };
    }

    private static Label CreateFieldLabel(string text, int x, int y) => new()
    {
        Text = text,
        ForeColor = Theme.Text,
        Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
        AutoSize = true,
        Location = new Point(x, y)
    };

    private static void ConfigureCheckBox(CheckBox checkBox, string text, Point location)
    {
        checkBox.Text = text;
        checkBox.ForeColor = Theme.Text;
        checkBox.BackColor = Theme.Surface;
        checkBox.FlatStyle = FlatStyle.Flat;
        checkBox.Cursor = Cursors.Hand;
        checkBox.AutoSize = true;
        checkBox.Location = location;
    }

    private void LoadConfig()
    {
        AppConfig config = ConfigService.Load(out string? warning);
        launcherPathTextBox.Text = config.LauncherPath;
        proxyUrlTextBox.Text = config.ProxyUrl;
        setAllProxyCheckBox.Checked = config.SetAllProxy;
        bypassLocalCheckBox.Checked = config.BypassLocalAddresses;
        if (warning is not null)
            toast.ShowMessage(warning, ToastKind.Warning, 5000);
    }

    private AppConfig GetConfigFromControls() => new()
    {
        LauncherPath = launcherPathTextBox.Text.Trim(),
        ProxyUrl = proxyUrlTextBox.Text.Trim(),
        SetAllProxy = setAllProxyCheckBox.Checked,
        BypassLocalAddresses = bypassLocalCheckBox.Checked
    };

    private bool SaveConfig(bool showSuccess)
    {
        AppConfig config = GetConfigFromControls();
        if (!ConfigService.TryValidate(config, out string error))
        {
            toast.ShowMessage(error, ToastKind.Warning);
            return false;
        }

        try
        {
            ConfigService.Save(config);
            if (showSuccess)
                toast.ShowMessage("配置已保存", ToastKind.Success);
            return true;
        }
        catch (Exception ex)
        {
            toast.ShowMessage($"保存配置失败：{ex.Message}", ToastKind.Warning, 5000);
            return false;
        }
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "选择 XIVLauncher.exe",
            Filter = "XIVLauncher|XIVLauncher.exe|可执行文件|*.exe|所有文件|*.*",
            FileName = "XIVLauncher.exe",
            CheckFileExists = true
        };

        string currentPath = launcherPathTextBox.Text.Trim();
        string? currentDirectory = Path.GetDirectoryName(currentPath);
        if (!string.IsNullOrWhiteSpace(currentDirectory) && Directory.Exists(currentDirectory))
            dialog.InitialDirectory = currentDirectory;

        if (dialog.ShowDialog(this) == DialogResult.OK)
            launcherPathTextBox.Text = dialog.FileName;
    }

    private async void TestProxyButton_Click(object? sender, EventArgs e)
    {
        if (!ConfigService.TryParseProxyUri(proxyUrlTextBox.Text, out Uri? uri, out string error))
        {
            toast.ShowMessage(error, ToastKind.Warning);
            return;
        }

        testProxyButton.Enabled = false;
        toast.ShowMessage($"正在连接 {uri!.Host}:{uri.Port}...", ToastKind.Info, 0);

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(uri.Host, uri.Port).WaitAsync(TimeSpan.FromSeconds(3));
            toast.ShowMessage($"连接成功：{uri.Host}:{uri.Port}", ToastKind.Success);
        }
        catch (Exception ex)
        {
            toast.ShowMessage($"连接失败：{uri.Host}:{uri.Port}（{ex.Message}）",
                ToastKind.Warning, 5000);
        }
        finally
        {
            testProxyButton.Enabled = true;
        }
    }

    private void SaveAndLaunchButton_Click(object? sender, EventArgs e)
    {
        if (!SaveConfig(showSuccess: false))
            return;

        try
        {
            LauncherService.Start(GetConfigFromControls());
            Close();
        }
        catch (Exception ex)
        {
            toast.ShowMessage($"启动失败：{ex.Message}", ToastKind.Warning, 5000);
        }
    }

    private void ShortcutButton_Click(object? sender, EventArgs e)
    {
        if (!SaveConfig(showSuccess: false))
            return;

        if (File.Exists(ShortcutService.ShortcutPath)
            && MessageBox.Show(this, "程序目录中的快捷方式已经存在，是否覆盖？", "生成快捷方式",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            ShortcutService.CreateShortcut();
            toast.ShowMessage("XIVLauncherProxy.lnk 已生成到程序目录", ToastKind.Success);
        }
        catch (Exception ex)
        {
            toast.ShowMessage($"生成快捷方式失败：{ex.Message}", ToastKind.Warning, 5000);
        }
    }
}
