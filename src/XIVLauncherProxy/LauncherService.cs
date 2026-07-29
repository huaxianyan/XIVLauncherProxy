using System.Diagnostics;

namespace XIVLauncherProxy;

internal static class LauncherService
{
    public static void Start(AppConfig config)
    {
        string proxy = config.ProxyUrl.Trim();
        var startInfo = new ProcessStartInfo
        {
            FileName = config.LauncherPath,
            WorkingDirectory = Path.GetDirectoryName(config.LauncherPath) ?? AppContext.BaseDirectory,
            UseShellExecute = false
        };

        startInfo.Environment["HTTP_PROXY"] = proxy;
        startInfo.Environment["HTTPS_PROXY"] = proxy;

        if (config.SetAllProxy)
            startInfo.Environment["ALL_PROXY"] = proxy;
        else
            startInfo.Environment.Remove("ALL_PROXY");

        if (config.BypassLocalAddresses)
            startInfo.Environment["NO_PROXY"] = "localhost,127.0.0.1,::1";
        else
            startInfo.Environment.Remove("NO_PROXY");

        Process.Start(startInfo);
    }
}
