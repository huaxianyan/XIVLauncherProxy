using System.Net.Sockets;
using XIVLauncherProxy.Application.Abstractions;
using XIVLauncherProxy.Application.Errors;

namespace XIVLauncherProxy.Infrastructure.Networking;

public sealed class TcpProxyConnectionTester : IProxyConnectionTester
{
    public async Task TestAsync(Uri proxyAddress, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(3));
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(proxyAddress.Host, proxyAddress.Port, timeout.Token);
        }
        catch (Exception exception) when (exception is SocketException or OperationCanceledException)
        {
            throw new ProxyException("ProxyConnectionFailed", exception);
        }
    }
}
