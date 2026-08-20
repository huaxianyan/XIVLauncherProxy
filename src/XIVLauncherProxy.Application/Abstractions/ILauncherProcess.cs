using XIVLauncherProxy.Domain.Configuration;

namespace XIVLauncherProxy.Application.Abstractions;

public interface ILauncherProcess
{
    void Start(AppConfiguration configuration, LaunchProfile profile);
}
