using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using XIVLauncherProxy.Application.Updates;

namespace XIVLauncherProxy.Infrastructure.Updates;

public sealed class GitHubApplicationUpdateService : IApplicationUpdateService
{
    public const string PackageAssetName = "XIVLauncherProxy.exe";
    public const string ChecksumAssetName = "XIVLauncherProxy.exe.sha256";

    private const long MaximumDownloadSize = 256L * 1024 * 1024;
    private static readonly Uri DefaultLatestReleaseUri = new(
        "https://api.github.com/repos/huaxianyan/XIVLauncherProxy/releases/latest");

    private readonly HttpClient httpClient;
    private readonly Version currentVersion;
    private readonly string updatesRoot;
    private readonly Uri latestReleaseUri;

    public GitHubApplicationUpdateService(
        HttpClient httpClient,
        Version currentVersion,
        string updatesRoot,
        Uri? latestReleaseUri = null)
    {
        this.httpClient = httpClient;
        this.currentVersion = currentVersion;
        this.updatesRoot = Path.GetFullPath(updatesRoot);
        this.latestReleaseUri = latestReleaseUri ?? DefaultLatestReleaseUri;
        if (!httpClient.DefaultRequestHeaders.UserAgent.Any())
            httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("XIVLauncherProxy", currentVersion.ToString(3)));
        httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    public async Task<ApplicationUpdateStatus> CheckAsync(CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(latestReleaseUri, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        GitHubReleaseDocument document = await JsonSerializer.DeserializeAsync<GitHubReleaseDocument>(
                stream, cancellationToken: cancellationToken)
            ?? throw new InvalidDataException("The GitHub release response is empty.");
        if (!TryParseVersion(document.TagName, out Version? version))
            throw new InvalidDataException("The release version is invalid.");
        GitHubReleaseAsset? package = document.Assets.FirstOrDefault(x => x.Name == PackageAssetName);
        GitHubReleaseAsset? checksum = document.Assets.FirstOrDefault(x => x.Name == ChecksumAssetName);
        if (package is null || checksum is null
            || !Uri.TryCreate(package.DownloadUrl, UriKind.Absolute, out Uri? packageUri)
            || !Uri.TryCreate(checksum.DownloadUrl, UriKind.Absolute, out Uri? checksumUri))
            throw new InvalidDataException("The release does not contain valid Windows update assets.");
        return new ApplicationUpdateStatus(
            currentVersion,
            new ApplicationRelease(version, document.TagName, packageUri, checksumUri));
    }

    public async Task<PreparedApplicationUpdate> PrepareAsync(
        ApplicationRelease release,
        IProgress<ApplicationUpdateProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (release.Version <= currentVersion)
            throw new InvalidOperationException("The selected release is not newer.");
        Directory.CreateDirectory(updatesRoot);
        string workingDirectory = Path.Combine(updatesRoot, $"{release.TagName}-{Guid.NewGuid():N}");
        string packageDirectory = Path.Combine(workingDirectory, "package");
        string updaterDirectory = Path.Combine(workingDirectory, "updater");
        Directory.CreateDirectory(packageDirectory);
        Directory.CreateDirectory(updaterDirectory);
        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(workingDirectory, ".xivlauncherproxy-update"),
                release.TagName,
                cancellationToken);
            string expectedHash = await DownloadChecksumAsync(release.ChecksumUri, cancellationToken);
            string packagePath = Path.Combine(packageDirectory, PackageAssetName);
            string actualHash = await DownloadFileAsync(
                release.PackageUri, packagePath, progress, cancellationToken);
            if (!string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The update checksum does not match the release.");
            string updaterPath = Path.Combine(updaterDirectory, "XIVLauncherProxy.Updater.exe");
            File.Copy(packagePath, updaterPath, overwrite: false);
            return new PreparedApplicationUpdate(
                release, packagePath, updaterPath, workingDirectory, actualHash);
        }
        catch
        {
            TryDeleteDirectory(workingDirectory);
            throw;
        }
    }

    private async Task<string> DownloadChecksumAsync(Uri uri, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();
        string contents = await response.Content.ReadAsStringAsync(cancellationToken);
        if (contents.Length > 4096)
            throw new InvalidDataException("The checksum file is too large.");
        string? hash = contents.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (hash is null || hash.Length != 64 || !hash.All(Uri.IsHexDigit))
            throw new InvalidDataException("The checksum file is invalid.");
        return hash.ToUpperInvariant();
    }

    private async Task<string> DownloadFileAsync(
        Uri uri,
        string destinationPath,
        IProgress<ApplicationUpdateProgress>? progress,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(
            uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        long? totalBytes = response.Content.Headers.ContentLength;
        if (totalBytes > MaximumDownloadSize)
            throw new InvalidDataException("The update is too large.");
        progress?.Report(new ApplicationUpdateProgress(0, totalBytes));
        await using Stream source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var destination = new FileStream(
            destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
            128 * 1024, FileOptions.Asynchronous | FileOptions.WriteThrough);
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[128 * 1024];
        long received = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            received = checked(received + read);
            if (received > MaximumDownloadSize)
                throw new InvalidDataException("The update is too large.");
            hash.AppendData(buffer, 0, read);
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            progress?.Report(new ApplicationUpdateProgress(received, totalBytes));
        }
        await destination.FlushAsync(cancellationToken);
        if (received == 0)
            throw new InvalidDataException("The update is empty.");
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static bool TryParseVersion(string tagName, out Version version)
    {
        string value = tagName.StartsWith('v') ? tagName[1..] : tagName;
        return Version.TryParse(value, out version!)
            && version.Build >= 0
            && version.Revision < 0;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
    }

    private sealed record GitHubReleaseDocument(
        [property: JsonPropertyName("tag_name")] string TagName,
        [property: JsonPropertyName("assets")] IReadOnlyList<GitHubReleaseAsset> Assets);

    private sealed record GitHubReleaseAsset(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("browser_download_url")] string DownloadUrl);
}
