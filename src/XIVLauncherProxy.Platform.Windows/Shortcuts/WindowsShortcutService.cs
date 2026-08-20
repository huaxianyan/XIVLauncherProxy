using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using XIVLauncherProxy.Application.Abstractions;
using XIVLauncherProxy.Application.Errors;
using XIVLauncherProxy.Domain.Configuration;

namespace XIVLauncherProxy.Platform.Windows.Shortcuts;

[SupportedOSPlatform("windows")]
public sealed class WindowsShortcutService(string executablePath, string outputDirectory) : IShortcutService
{
    public string GetPath(LaunchProfile profile)
    {
        string safeName = string.Join("_", profile.Name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "XIVLauncher";
        return Path.Combine(outputDirectory, $"XIVLauncher - {safeName}.lnk");
    }

    public void Create(LaunchProfile profile)
    {
        Type shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new ProxyException("ShortcutUnsupported");
        object? shell = null;
        object? shortcut = null;
        try
        {
            shell = Activator.CreateInstance(shellType) ?? throw new ProxyException("ShortcutCreateFailed");
            dynamic dynamicShell = shell;
            shortcut = dynamicShell.CreateShortcut(GetPath(profile));
            dynamic value = shortcut;
            value.TargetPath = executablePath;
            value.Arguments = $"--launch --profile={profile.Id}";
            value.WorkingDirectory = Path.GetDirectoryName(executablePath) ?? AppContext.BaseDirectory;
            value.IconLocation = $"{executablePath},0";
            value.Description = profile.Name;
            value.Save();
        }
        catch (Exception exception) when (exception is not ProxyException)
        {
            throw new ProxyException("ShortcutCreateFailed", exception);
        }
        finally
        {
            if (shortcut is not null && Marshal.IsComObject(shortcut)) Marshal.FinalReleaseComObject(shortcut);
            if (shell is not null && Marshal.IsComObject(shell)) Marshal.FinalReleaseComObject(shell);
        }
    }

    public void Delete(LaunchProfile profile)
    {
        string path = GetPath(profile);
        if (File.Exists(path)) File.Delete(path);
    }
}
