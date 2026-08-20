namespace XIVLauncherProxy.Application.Abstractions;

public interface IProxyConnectionTester
{
    Task TestAsync(Uri proxyAddress, CancellationToken cancellationToken = default);
}
