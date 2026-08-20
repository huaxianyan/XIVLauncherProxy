using XIVLauncherProxy.Application.Abstractions;
using XIVLauncherProxy.Domain.Authentication;
using XIVLauncherProxy.Domain.Configuration;

namespace XIVLauncherProxy.Application.Configuration;

public sealed class ProfileService(
    IConfigurationStore settings,
    ITotpCredentialStore credentials,
    IShortcutService shortcuts,
    ConfigurationValidator validator)
{
    public void Save(AppConfiguration configuration, LaunchProfile profile, string? pendingSecret)
    {
        validator.ValidateProfileInputs(configuration, profile);
        if (!string.IsNullOrWhiteSpace(pendingSecret))
            credentials.Save(profile.CredentialKey, Base32Secret.Normalize(pendingSecret));
        validator.ValidateProfile(configuration, profile);
        settings.Save(configuration);
    }

    public string CreateShortcut(AppConfiguration configuration, LaunchProfile profile, string? pendingSecret)
    {
        Save(configuration, profile, pendingSecret);
        shortcuts.Create(profile);
        return shortcuts.GetPath(profile);
    }

    public void Delete(AppConfiguration configuration, LaunchProfile profile)
    {
        configuration.LaunchProfiles.Remove(profile);
        settings.Save(configuration);
        shortcuts.Delete(profile);
    }

    public bool HasCredential(LaunchProfile profile) =>
        !string.IsNullOrWhiteSpace(profile.CredentialKey) && credentials.Contains(profile.CredentialKey);

    public void RemoveCredential(LaunchProfile profile) => credentials.Remove(profile.CredentialKey);
}
