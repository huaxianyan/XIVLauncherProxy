using System.Text.Json;

namespace XIVLauncherProxy;

internal static class ConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string DefaultLauncherPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "XIVLauncher",
        "XIVLauncher.exe");

    public static string ConfigDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "XIVLauncherProxy");

    public static string ConfigPath => Path.Combine(ConfigDirectory, "config.json");

    public static bool ConfigExists => File.Exists(ConfigPath);

    public static AppConfig Load(out string? warning)
    {
        warning = null;

        if (!File.Exists(ConfigPath))
            return new AppConfig();

        try
        {
            AppConfig? config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath), JsonOptions);
            if (config is null)
                throw new InvalidDataException("配置内容为空。");

            return config;
        }
        catch (Exception ex)
        {
            warning = $"无法读取配置文件，将使用默认配置。\n{ex.Message}";
            return new AppConfig();
        }
    }

    public static void Save(AppConfig config)
    {
        Directory.CreateDirectory(ConfigDirectory);
        string temporaryPath = ConfigPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(config, JsonOptions));
        File.Move(temporaryPath, ConfigPath, true);
    }

    public static bool TryValidate(AppConfig config, out string error)
    {
        if (string.IsNullOrWhiteSpace(config.LauncherPath))
        {
            error = "请选择 XIVLauncher.exe。";
            return false;
        }

        if (!File.Exists(config.LauncherPath))
        {
            error = $"找不到 XIVLauncher：\n{config.LauncherPath}";
            return false;
        }

        if (!string.Equals(Path.GetFileName(config.LauncherPath), "XIVLauncher.exe", StringComparison.OrdinalIgnoreCase))
        {
            error = "所选文件不是 XIVLauncher.exe。";
            return false;
        }

        if (!TryParseProxyUri(config.ProxyUrl, out _, out error))
            return false;

        error = string.Empty;
        return true;
    }

    public static bool TryParseProxyUri(string value, out Uri? uri, out string error)
    {
        uri = null;

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out Uri? parsed)
            || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps)
            || string.IsNullOrWhiteSpace(parsed.Host)
            || parsed.Port is < 1 or > 65535)
        {
            error = "代理地址格式无效，请使用类似 http://127.0.0.1:37777 的地址。";
            return false;
        }

        uri = parsed;
        error = string.Empty;
        return true;
    }
}
