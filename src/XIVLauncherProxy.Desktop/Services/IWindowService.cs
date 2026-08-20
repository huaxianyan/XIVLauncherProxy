namespace XIVLauncherProxy.Desktop.Services;

public interface IWindowService
{
    Task<string?> PickLauncherAsync(string currentPath);
    Task<bool> ConfirmAsync(string message);
}
