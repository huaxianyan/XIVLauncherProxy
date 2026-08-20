using Avalonia.Controls;
using Avalonia.Platform.Storage;
using XIVLauncherProxy.Desktop.Views;
using XIVLauncherProxy.Domain.Authentication;

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

    public async Task<IReadOnlyList<string>> PickQrImagesAsync()
    {
        Window? window = owner();
        if (window?.StorageProvider is null)
            return [];
        IReadOnlyList<IStorageFile> files = await window.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                AllowMultiple = true,
                Title = Localization.ResourceTextLocalizer.Instance["SelectQrImages"],
                FileTypeFilter =
                [
                    new FilePickerFileType(Localization.ResourceTextLocalizer.Instance["ImageFiles"])
                    {
                        Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif", "*.webp"],
                    },
                ],
            });
        return files.Select(x => x.TryGetLocalPath()).Where(x => x is not null).Cast<string>().ToArray();
    }

    public async Task<OtpImportEntry?> SelectOtpEntryAsync(IReadOnlyList<OtpImportEntry> entries)
    {
        Window? window = owner();
        if (window is null)
            return null;
        var dialog = new OtpImportWindow(entries);
        return await dialog.ShowDialog<OtpImportEntry?>(window);
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
