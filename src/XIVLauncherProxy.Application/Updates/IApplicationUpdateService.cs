namespace XIVLauncherProxy.Application.Updates;

public sealed record ApplicationRelease(
    Version Version,
    string TagName,
    Uri PackageUri,
    Uri ChecksumUri);

public sealed record ApplicationUpdateStatus(
    Version CurrentVersion,
    ApplicationRelease LatestRelease)
{
    public bool IsUpdateAvailable => LatestRelease.Version > CurrentVersion;
}

public sealed record PreparedApplicationUpdate(
    ApplicationRelease Release,
    string PackageExecutablePath,
    string UpdaterExecutablePath,
    string WorkingDirectory,
    string PackageSha256);

public sealed record ApplicationUpdateProgress(long BytesReceived, long? TotalBytes);

public interface IApplicationUpdateService
{
    Task<ApplicationUpdateStatus> CheckAsync(CancellationToken cancellationToken = default);

    Task<PreparedApplicationUpdate> PrepareAsync(
        ApplicationRelease release,
        IProgress<ApplicationUpdateProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
