using System.Text.Json;
using XIVLauncherProxy.Application.Abstractions;
using XIVLauncherProxy.Application.Errors;
using XIVLauncherProxy.Domain.Accounts;

namespace XIVLauncherProxy.Infrastructure.Accounts;

public sealed class JsonXivLauncherAccountSource(string path) : IXivLauncherAccountSource
{
    public IReadOnlyList<XivLauncherAccount> LoadAccounts()
    {
        if (!File.Exists(path))
            return [];
        try
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                throw new JsonException();
            var accounts = new List<XivLauncherAccount>();
            foreach (JsonElement item in document.RootElement.EnumerateArray())
            {
                if (!item.TryGetProperty("UserName", out JsonElement userNameProperty)
                    || userNameProperty.ValueKind != JsonValueKind.String
                    || string.IsNullOrWhiteSpace(userNameProperty.GetString()))
                    continue;
                accounts.Add(new XivLauncherAccount(
                    userNameProperty.GetString()!.Trim(),
                    ReadBoolean(item, "UseOtp"),
                    ReadBoolean(item, "UseSteamServiceAccount")));
            }
            return accounts.OrderBy(x => x.UserName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.UseSteamServiceAccount).ToArray();
        }
        catch (Exception exception) when (exception is JsonException or IOException)
        {
            throw new ProxyException("AccountsReadFailed", exception);
        }
    }

    private static bool ReadBoolean(JsonElement item, string name) =>
        item.TryGetProperty(name, out JsonElement property)
        && property.ValueKind is JsonValueKind.True or JsonValueKind.False
        && property.GetBoolean();
}
