using XIVLauncherProxy.Domain.Configuration;

namespace XIVLauncherProxy.Application.Abstractions;

/// <summary>
/// Records launch problems that happen after XIVLauncher is already running, so they can be
/// reviewed later without opening the configuration window.
/// </summary>
public interface IDiagnosticLog
{
    void Report(string code, LaunchProfile profile, Exception? exception = null);
}
