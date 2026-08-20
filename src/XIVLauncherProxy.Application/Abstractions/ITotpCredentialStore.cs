namespace XIVLauncherProxy.Application.Abstractions;

public interface ITotpCredentialStore
{
    bool Contains(string credentialKey);
    void Save(string credentialKey, string normalizedSecret);
    string Get(string credentialKey);
    void Remove(string credentialKey);
}
