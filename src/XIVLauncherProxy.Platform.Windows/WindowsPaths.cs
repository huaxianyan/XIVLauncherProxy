namespace XIVLauncherProxy.Platform.Windows;

public static class WindowsPaths
{
    public static string AppDataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "XIVLauncherProxy");
    public static string ConfigurationPath => Path.Combine(AppDataDirectory, "config.json");
    public static string CredentialPath => Path.Combine(AppDataDirectory, "totp-credentials.json");
    public static string DefaultLauncherPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "XIVLauncher", "XIVLauncher.exe");
    public static string XivLauncherDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XIVLauncher");
    public static string AccountListPath => Path.Combine(XivLauncherDirectory, "accountsList.json");
    public static string LauncherSettingsPath => Path.Combine(XivLauncherDirectory, "launcherConfigV3.json");
}
