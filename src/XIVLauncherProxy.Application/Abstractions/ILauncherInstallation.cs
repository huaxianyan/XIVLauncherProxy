namespace XIVLauncherProxy.Application.Abstractions;

public interface ILauncherInstallation
{
    bool Exists(string launcherPath);
    bool HasExpectedFileName(string launcherPath);
}
