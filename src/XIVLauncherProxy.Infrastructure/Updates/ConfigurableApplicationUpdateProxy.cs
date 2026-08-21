using System.Net;
using XIVLauncherProxy.Application.Updates;

namespace XIVLauncherProxy.Infrastructure.Updates;

public sealed class ConfigurableApplicationUpdateProxy : IWebProxy, IApplicationUpdateProxy
{
    private Uri? proxyUri;

    public ICredentials? Credentials
    {
        get => null;
        set
        {
            if (value is not null)
                throw new NotSupportedException("Authenticated update proxies are not supported.");
        }
    }

    public void Configure(Uri? value) => Volatile.Write(ref proxyUri, value);

    public Uri GetProxy(Uri destination) => Volatile.Read(ref proxyUri) ?? destination;

    public bool IsBypassed(Uri host) => Volatile.Read(ref proxyUri) is null;
}
