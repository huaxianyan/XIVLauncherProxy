using XIVLauncherProxy.Domain.Configuration;

namespace XIVLauncherProxy.Application.Abstractions;

public interface IShortcutService
{
    string GetPath(LaunchProfile profile);
    void Create(LaunchProfile profile);
    void Delete(LaunchProfile profile);
}
