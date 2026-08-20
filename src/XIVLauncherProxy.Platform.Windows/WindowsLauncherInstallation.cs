using XIVLauncherProxy.Application.Abstractions;

namespace XIVLauncherProxy.Platform.Windows;

public sealed class WindowsLauncherInstallation : ILauncherInstallation
{
    public bool Exists(string launcherPath) => File.Exists(launcherPath);
    public bool HasExpectedFileName(string launcherPath) =>
        Path.GetFileName(launcherPath).Equals("XIVLauncher.exe", StringComparison.OrdinalIgnoreCase);
}
