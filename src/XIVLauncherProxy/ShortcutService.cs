using System.Runtime.InteropServices;

namespace XIVLauncherProxy;

internal static class ShortcutService
{
    public static string ShortcutPath => Path.Combine(
        AppContext.BaseDirectory,
        "XIVLauncherProxy.lnk");

    public static void CreateShortcut()
    {
        string executablePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("无法确定启动器程序路径。");

        Type shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("系统不支持创建 Windows 快捷方式。");

        object? shell = null;
        object? shortcut = null;

        try
        {
            shell = Activator.CreateInstance(shellType)
                ?? throw new InvalidOperationException("无法创建 Windows 快捷方式组件。");

            dynamic dynamicShell = shell;
            shortcut = dynamicShell.CreateShortcut(ShortcutPath);
            dynamic dynamicShortcut = shortcut;
            dynamicShortcut.TargetPath = executablePath;
            dynamicShortcut.Arguments = "--launch";
            dynamicShortcut.WorkingDirectory = Path.GetDirectoryName(executablePath) ?? AppContext.BaseDirectory;
            dynamicShortcut.IconLocation = $"{executablePath},0";
            dynamicShortcut.Description = "通过本地代理启动 XIVLauncher";
            dynamicShortcut.Save();
        }
        finally
        {
            if (shortcut is not null && Marshal.IsComObject(shortcut))
                Marshal.FinalReleaseComObject(shortcut);
            if (shell is not null && Marshal.IsComObject(shell))
                Marshal.FinalReleaseComObject(shell);
        }
    }
}
