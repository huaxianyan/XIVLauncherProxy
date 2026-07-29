namespace XIVLauncherProxy;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        bool quickLaunch = args.Any(arg =>
            string.Equals(arg, "--launch", StringComparison.OrdinalIgnoreCase));

        if (!quickLaunch)
        {
            Application.Run(new MainForm());
            return;
        }

        bool configExists = ConfigService.ConfigExists;
        AppConfig config = ConfigService.Load(out string? warning);
        string? problem = warning;

        if (!configExists)
            problem = "尚未保存配置，请先完成设置。";
        else if (!ConfigService.TryValidate(config, out string validationError))
            problem = validationError;

        if (problem is null)
        {
            try
            {
                LauncherService.Start(config);
                return;
            }
            catch (Exception ex)
            {
                problem = $"启动 XIVLauncher 失败：\n{ex.Message}";
            }
        }

        Application.Run(new MainForm(problem));
    }
}
