using System.Text.Json;
using System.Text.Json.Nodes;
using XIVLauncherProxy.Application.Abstractions;
using XIVLauncherProxy.Application.Errors;

namespace XIVLauncherProxy.Platform.Windows.Launching;

public sealed class WindowsAutologinSessionFactory(string settingsPath) : IAutologinSessionFactory
{
    public IAutologinSession EnableTemporarily()
    {
        bool previous = SetEnabled(true);
        return new Session(() => SetEnabled(previous));
    }

    private bool SetEnabled(bool enabled)
    {
        if (!File.Exists(settingsPath))
            throw new ProxyException("LauncherSettingsMissing");
        try
        {
            JsonObject settings = JsonNode.Parse(File.ReadAllText(settingsPath))?.AsObject()
                ?? throw new JsonException();
            const string property = "AutologinEnabled";
            bool previous = ReadBoolean(settings[property]);
            settings[property] = enabled ? "true" : "false";
            string temporaryPath = settingsPath + ".xivlauncherproxy.tmp";
            File.WriteAllText(temporaryPath, settings.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporaryPath, settingsPath, true);
            return previous;
        }
        catch (Exception exception) when (exception is JsonException or IOException or InvalidOperationException)
        {
            throw new ProxyException("LauncherSettingsUpdateFailed", exception);
        }
    }

    private static bool ReadBoolean(JsonNode? node)
    {
        if (node is not JsonValue value)
            return false;
        if (value.TryGetValue<string>(out string? text) && bool.TryParse(text, out bool parsed))
            return parsed;
        return value.TryGetValue<bool>(out bool boolean) && boolean;
    }

    private sealed class Session(Action restore) : IAutologinSession
    {
        private bool disposed;
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            restore();
        }
    }
}
