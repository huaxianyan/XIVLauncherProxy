namespace XIVLauncherProxy.Domain.Configuration;

public sealed class LaunchProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public bool UseProxy { get; set; }
    public bool UseQuickLogin { get; set; }
    public string XivLauncherAccountId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;

    public string CredentialKey => UserName.Trim().ToLowerInvariant();
}
