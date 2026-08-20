using XIVLauncherProxy.Application.Abstractions;
using XIVLauncherProxy.Application.Errors;
using XIVLauncherProxy.Domain.Configuration;

namespace XIVLauncherProxy.Application.Configuration;

public sealed class ConfigurationValidator(
    ITotpCredentialStore credentials,
    ILauncherInstallation launcherInstallation)
{
    public void ValidateLauncherPath(string launcherPath)
    {
        if (string.IsNullOrWhiteSpace(launcherPath))
            throw new ProxyException("LauncherPathMissing");
        if (!launcherInstallation.Exists(launcherPath))
            throw new ProxyException("LauncherNotFound");
        if (!launcherInstallation.HasExpectedFileName(launcherPath))
            throw new ProxyException("LauncherFileInvalid");
    }

    public void Validate(AppConfiguration configuration)
    {
        ValidateLauncherPath(configuration.LauncherPath);
        if (configuration.LaunchProfiles.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1))
            throw new ProxyException("ProfilesConflict");
    }

    public void ValidateProfileInputs(AppConfiguration configuration, LaunchProfile profile)
    {
        Validate(configuration);
        if (string.IsNullOrWhiteSpace(profile.Name))
            throw new ProxyException("ProfileNameMissing");
        if (!profile.UseProxy && !profile.UseQuickLogin)
            throw new ProxyException("ProfileFeatureMissing");
        if (profile.UseProxy && !ProxyAddress.TryParse(configuration.ProxyUrl, out _))
            throw new ProxyException("ProxyAddressInvalid");
        if (profile.UseQuickLogin
            && (string.IsNullOrWhiteSpace(profile.UserName)
                || string.IsNullOrWhiteSpace(profile.XivLauncherAccountId)))
            throw new ProxyException("ProfileAccountMissing");
    }

    public void ValidateProfile(AppConfiguration configuration, LaunchProfile profile)
    {
        ValidateProfileInputs(configuration, profile);
        if (profile.UseQuickLogin && !credentials.Contains(profile.CredentialKey))
            throw new ProxyException("ProfileCredentialMissing");
    }
}

public static class ProxyAddress
{
    public static bool TryParse(string value, out Uri? uri)
    {
        uri = null;
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out Uri? parsed)
            || parsed.Scheme is not ("http" or "https" or "socks4" or "socks5")
            || string.IsNullOrWhiteSpace(parsed.Host)
            || parsed.Port is < 1 or > 65535)
            return false;
        uri = parsed;
        return true;
    }
}
