using XIVLauncherProxy.Domain.Configuration;

namespace XIVLauncherProxy.Application.Abstractions;

public interface IConfigurationStore
{
    AppConfiguration Load();
    void Save(AppConfiguration configuration);
}
