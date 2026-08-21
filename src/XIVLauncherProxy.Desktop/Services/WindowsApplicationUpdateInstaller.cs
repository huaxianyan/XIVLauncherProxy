using Avalonia.Controls.ApplicationLifetimes;
using XIVLauncherProxy.Application.Updates;
using XIVLauncherProxy.Platform.Windows.Updates;

namespace XIVLauncherProxy.Desktop.Services;

public sealed class WindowsApplicationUpdateInstaller(
    IClassicDesktopStyleApplicationLifetime desktop) : IApplicationUpdateInstaller
{
    public bool IsSupported => WindowsSelfUpdate.IsSupportedInstallation;

    public void Launch(PreparedApplicationUpdate preparedUpdate)
    {
        WindowsSelfUpdate.Launch(preparedUpdate);
        desktop.Shutdown();
    }
}
