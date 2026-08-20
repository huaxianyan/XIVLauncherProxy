namespace XIVLauncherProxy.Domain.Accounts;

public sealed record XivLauncherAccount(
    string UserName,
    bool UseOtp,
    bool UseSteamServiceAccount)
{
    public string AccountId => $"{UserName}-{UseOtp}-{UseSteamServiceAccount}";
}
