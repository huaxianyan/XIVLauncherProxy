using System.Diagnostics;
using XIVLauncherProxy.Application.Abstractions;
using XIVLauncherProxy.Application.Errors;
using XIVLauncherProxy.Domain.Configuration;

namespace XIVLauncherProxy.Platform.Windows.Launching;

public sealed class WindowsLauncherProcess : ILauncherProcess
{
    public void Start(AppConfiguration configuration, LaunchProfile profile)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = configuration.LauncherPath,
            WorkingDirectory = Path.GetDirectoryName(configuration.LauncherPath) ?? AppContext.BaseDirectory,
            UseShellExecute = false,
        };
        if (profile.UseQuickLogin)
            startInfo.ArgumentList.Add($"--account={profile.XivLauncherAccountId}");
        else
            startInfo.ArgumentList.Add("--noautologin");
        if (profile.UseProxy)
            ApplyProxy(startInfo, configuration);
        try
        {
            _ = Process.Start(startInfo) ?? throw new ProxyException("LauncherStartFailed");
        }
        catch (Exception exception) when (exception is not ProxyException)
        {
            throw new ProxyException("LauncherStartFailed", exception);
        }
    }

    private static void ApplyProxy(ProcessStartInfo startInfo, AppConfiguration configuration)
    {
        string proxy = configuration.ProxyUrl.Trim();
        startInfo.Environment["HTTP_PROXY"] = proxy;
        startInfo.Environment["HTTPS_PROXY"] = proxy;
        if (configuration.SetAllProxy)
            startInfo.Environment["ALL_PROXY"] = proxy;
        else
            startInfo.Environment.Remove("ALL_PROXY");
        if (configuration.BypassLocalAddresses)
            startInfo.Environment["NO_PROXY"] = "localhost,127.0.0.1,::1";
        else
            startInfo.Environment.Remove("NO_PROXY");
    }
}
