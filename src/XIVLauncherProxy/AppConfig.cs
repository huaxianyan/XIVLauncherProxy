namespace XIVLauncherProxy;

internal sealed class AppConfig
{
    public string LauncherPath { get; set; } = ConfigService.DefaultLauncherPath;

    public string ProxyUrl { get; set; } = string.Empty;

    public bool SetAllProxy { get; set; }

    public bool BypassLocalAddresses { get; set; } = true;
}
