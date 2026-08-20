namespace XIVLauncherProxy.Domain.Configuration;

public sealed class AppConfiguration
{
    public string LauncherPath { get; set; } = string.Empty;
    public string ProxyUrl { get; set; } = string.Empty;
    public bool SetAllProxy { get; set; }
    public bool BypassLocalAddresses { get; set; } = true;
    public List<LaunchProfile> LaunchProfiles { get; set; } = [];
}
