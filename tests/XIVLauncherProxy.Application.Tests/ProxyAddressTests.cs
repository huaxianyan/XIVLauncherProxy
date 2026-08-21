using XIVLauncherProxy.Application.Configuration;

namespace XIVLauncherProxy.Application.Tests;

public sealed class ProxyAddressTests
{
    [Fact]
    public void ExistingProxyUrlCanBeEditedAsSeparateFieldsWithoutChangingItsAddress()
    {
        Assert.True(ProxyAddress.TrySplit(
            "socks5://127.0.0.1:37777", out string scheme, out string host, out string port));

        Assert.Equal("socks5://", scheme);
        Assert.Equal("127.0.0.1", host);
        Assert.Equal("37777", port);
        Assert.True(ProxyAddress.TryCreate(scheme, host, port, out Uri? uri));
        Assert.Equal("socks5://127.0.0.1:37777", uri!.GetLeftPart(UriPartial.Authority));
    }
}
