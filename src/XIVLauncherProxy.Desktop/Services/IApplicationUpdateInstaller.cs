using XIVLauncherProxy.Application.Updates;

namespace XIVLauncherProxy.Desktop.Services;

public interface IApplicationUpdateInstaller
{
    bool IsSupported { get; }
    void Launch(PreparedApplicationUpdate preparedUpdate);
}
