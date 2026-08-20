using XIVLauncherProxy.Domain.Accounts;

namespace XIVLauncherProxy.Application.Abstractions;

public interface IXivLauncherAccountSource
{
    IReadOnlyList<XivLauncherAccount> LoadAccounts();
}
