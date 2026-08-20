using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Runtime.Versioning;
using XIVLauncherProxy.Application.Abstractions;
using XIVLauncherProxy.Application.Errors;

namespace XIVLauncherProxy.Platform.Windows.Security;

[SupportedOSPlatform("windows")]
public sealed class DpapiTotpCredentialStore(string path) : ITotpCredentialStore
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("XIVLauncherProxy.Totp.v1");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public bool Contains(string credentialKey) =>
        !string.IsNullOrWhiteSpace(credentialKey) && Load().ContainsKey(NormalizeKey(credentialKey));

    public void Save(string credentialKey, string normalizedSecret)
    {
        byte[] plaintext = Encoding.UTF8.GetBytes(normalizedSecret);
        try
        {
            byte[] protectedSecret = ProtectedData.Protect(plaintext, Entropy, DataProtectionScope.CurrentUser);
            Dictionary<string, string> values = Load();
            values[NormalizeKey(credentialKey)] = Convert.ToBase64String(protectedSecret);
            Write(values);
        }
        catch (CryptographicException exception)
        {
            throw new ProxyException("CredentialSaveFailed", exception);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public string Get(string credentialKey)
    {
        if (!Load().TryGetValue(NormalizeKey(credentialKey), out string? encoded))
            throw new ProxyException("ProfileCredentialMissing");
        try
        {
            byte[] plaintext = ProtectedData.Unprotect(
                Convert.FromBase64String(encoded), Entropy, DataProtectionScope.CurrentUser);
            try { return Encoding.UTF8.GetString(plaintext); }
            finally { CryptographicOperations.ZeroMemory(plaintext); }
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException)
        {
            throw new ProxyException("CredentialReadFailed", exception);
        }
    }

    public void Remove(string credentialKey)
    {
        if (string.IsNullOrWhiteSpace(credentialKey))
            return;
        Dictionary<string, string> values = Load();
        if (values.Remove(NormalizeKey(credentialKey)))
            Write(values);
    }

    private Dictionary<string, string> Load()
    {
        if (!File.Exists(path))
            return new(StringComparer.OrdinalIgnoreCase);
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path), Options)
                   ?? new(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is JsonException or IOException)
        {
            throw new ProxyException("CredentialReadFailed", exception);
        }
    }

    private void Write(Dictionary<string, string> values)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(values, Options));
        File.Move(temporaryPath, path, true);
    }

    private static string NormalizeKey(string value) => value.Trim().ToLowerInvariant();
}
