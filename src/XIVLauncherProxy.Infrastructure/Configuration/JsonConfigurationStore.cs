using System.Text.Json;
using XIVLauncherProxy.Application.Abstractions;
using XIVLauncherProxy.Application.Errors;
using XIVLauncherProxy.Domain.Configuration;

namespace XIVLauncherProxy.Infrastructure.Configuration;

public sealed class JsonConfigurationStore(string path, string defaultLauncherPath) : IConfigurationStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public AppConfiguration Load()
    {
        if (!File.Exists(path))
            return CreateDefault();
        try
        {
            AppConfiguration configuration = JsonSerializer.Deserialize<AppConfiguration>(
                File.ReadAllText(path), Options) ?? CreateDefault();
            configuration.LaunchProfiles ??= [];
            if (string.IsNullOrWhiteSpace(configuration.LauncherPath))
                configuration.LauncherPath = defaultLauncherPath;
            return configuration;
        }
        catch (Exception exception) when (exception is JsonException or IOException)
        {
            throw new ProxyException("ConfigurationReadFailed", exception);
        }
    }

    public void Save(AppConfiguration configuration)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(configuration, Options));
            File.Move(temporaryPath, path, true);
        }
        catch (IOException exception)
        {
            throw new ProxyException("ConfigurationSaveFailed", exception);
        }
    }

    private AppConfiguration CreateDefault() => new() { LauncherPath = defaultLauncherPath };
}
