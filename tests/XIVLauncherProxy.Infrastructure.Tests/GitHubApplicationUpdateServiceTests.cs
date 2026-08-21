using System.Net;
using System.Security.Cryptography;
using System.Text;
using XIVLauncherProxy.Infrastructure.Updates;

namespace XIVLauncherProxy.Infrastructure.Tests;

public sealed class GitHubApplicationUpdateServiceTests : IDisposable
{
    private static readonly Uri LatestUri = new("https://updates.test/latest");
    private static readonly Uri PackageUri = new("https://updates.test/XIVLauncherProxy.exe");
    private static readonly Uri ChecksumUri = new("https://updates.test/XIVLauncherProxy.exe.sha256");
    private readonly string root = Path.Combine(
        Path.GetTempPath(), $"XIVLauncherProxy-update-{Guid.NewGuid():N}");

    [Fact]
    public void UpdateProxySwitchChangesOnlyTheUpdateRequestRoute()
    {
        var proxy = new ConfigurableApplicationUpdateProxy();
        var destination = new Uri("https://api.github.com/repos/huaxianyan/XIVLauncherProxy/releases/latest");
        var localProxy = new Uri("socks5://127.0.0.1:37777");

        proxy.Configure(localProxy);
        Assert.False(proxy.IsBypassed(destination));
        Assert.Equal(localProxy, proxy.GetProxy(destination));

        proxy.Configure(null);
        Assert.True(proxy.IsBypassed(destination));
        Assert.Equal(destination, proxy.GetProxy(destination));
    }

    [Fact]
    public async Task NewReleaseDownloadsTheVerifiedExecutable()
    {
        byte[] executable = Encoding.UTF8.GetBytes("new executable");
        using HttpClient client = CreateClient(executable, checksumOverride: null);
        var service = new GitHubApplicationUpdateService(
            client, new Version(3, 1, 1), root, LatestUri);

        var release = (await service.CheckAsync()).LatestRelease;
        var prepared = await service.PrepareAsync(release);

        Assert.Equal(new Version(3, 2, 0), release.Version);
        Assert.Equal(executable, await File.ReadAllBytesAsync(prepared.PackageExecutablePath));
        Assert.Equal(
            Convert.ToHexString(SHA256.HashData(executable)),
            prepared.PackageSha256);
    }

    [Fact]
    public async Task DownloadWithWrongReleaseChecksumIsDiscarded()
    {
        byte[] executable = Encoding.UTF8.GetBytes("new executable");
        using HttpClient client = CreateClient(executable, new string('A', 64));
        var service = new GitHubApplicationUpdateService(
            client, new Version(3, 1, 1), root, LatestUri);
        var release = (await service.CheckAsync()).LatestRelease;

        await Assert.ThrowsAsync<InvalidDataException>(() => service.PrepareAsync(release));

        Assert.Empty(Directory.EnumerateFileSystemEntries(root));
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }

    private static HttpClient CreateClient(byte[] executable, string? checksumOverride)
    {
        string hash = checksumOverride ?? Convert.ToHexString(SHA256.HashData(executable));
        string releaseJson = $$"""
            {
              "tag_name": "v3.2.0",
              "assets": [
                { "name": "XIVLauncherProxy.exe", "browser_download_url": "{{PackageUri}}" },
                { "name": "XIVLauncherProxy.exe.sha256", "browser_download_url": "{{ChecksumUri}}" }
              ]
            }
            """;
        return new HttpClient(new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri == LatestUri)
                return Response(new StringContent(releaseJson, Encoding.UTF8, "application/json"));
            if (request.RequestUri == PackageUri)
                return Response(new ByteArrayContent(executable));
            if (request.RequestUri == ChecksumUri)
                return Response(new StringContent($"{hash}  XIVLauncherProxy.exe\n"));
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }));
    }

    private static HttpResponseMessage Response(HttpContent content) =>
        new(HttpStatusCode.OK) { Content = content };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
