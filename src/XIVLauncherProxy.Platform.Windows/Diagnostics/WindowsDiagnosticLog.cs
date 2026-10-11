using System.Runtime.Versioning;
using XIVLauncherProxy.Application.Abstractions;
using XIVLauncherProxy.Domain.Configuration;

namespace XIVLauncherProxy.Platform.Windows.Diagnostics;

/// <summary>
/// Appends launch diagnostics to a local file. Records codes and exception text only; never secrets,
/// generated codes, or XIVLauncher account names.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsDiagnosticLog(string path) : IDiagnosticLog
{
    public void Report(string code, LaunchProfile profile, Exception? exception = null)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.AppendAllText(path, BuildEntry(code, profile, exception));
        }
        catch (Exception writeFailure) when (writeFailure is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static string BuildEntry(string code, LaunchProfile profile, Exception? exception)
    {
        string line = $"{DateTimeOffset.Now:O} {code} profile={profile.Id}";
        return exception is null
            ? line + Environment.NewLine
            : $"{line}{Environment.NewLine}{exception}{Environment.NewLine}";
    }
}
