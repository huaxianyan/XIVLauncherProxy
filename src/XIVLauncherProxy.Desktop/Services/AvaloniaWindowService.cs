using Avalonia.Controls;
using Avalonia.Platform.Storage;
using XIVLauncherProxy.Desktop.Views;

namespace XIVLauncherProxy.Desktop.Services;

public sealed class AvaloniaWindowService(Func<Window?> owner) : IWindowService
{
    public async Task<string?> PickLauncherAsync(string currentPath)
    {
        Window? window = owner();
        if (window?.StorageProvider is null)
            return null;
        IReadOnlyList<IStorageFile> files = await window.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                AllowMultiple = false,
                Title = "XIVLauncher.exe",
                FileTypeFilter = [new FilePickerFileType("XIVLauncher") { Patterns = ["XIVLauncher.exe"] }],
                SuggestedStartLocation = await GetStartLocationAsync(window, currentPath),
            });
        return files.Count == 1 ? files[0].TryGetLocalPath() : null;
    }

    public async Task<bool> ConfirmAsync(string message)
    {
        Window? window = owner();
        if (window is null) return false;
        var dialog = new ConfirmationWindow(message);
        return await dialog.ShowDialog<bool>(window);
    }

    private static async Task<IStorageFolder?> GetStartLocationAsync(Window window, string path)
    {
        string? directory = Path.GetDirectoryName(path);
        return Directory.Exists(directory)
            ? await window.StorageProvider.TryGetFolderFromPathAsync(directory)
            : null;
    }
}
