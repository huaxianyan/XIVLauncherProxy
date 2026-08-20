using XIVLauncherProxy.Domain.Authentication;

namespace XIVLauncherProxy.Desktop.Services;

public interface IWindowService
{
    Task<string?> PickLauncherAsync(string currentPath);
    Task<IReadOnlyList<string>> PickQrImagesAsync();
    Task<OtpImportEntry?> SelectOtpEntryAsync(IReadOnlyList<OtpImportEntry> entries);
    Task<bool> ConfirmAsync(string message);
}
