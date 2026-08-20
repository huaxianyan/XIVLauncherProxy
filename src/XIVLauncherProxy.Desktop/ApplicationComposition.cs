using XIVLauncherProxy.Application.Abstractions;
using XIVLauncherProxy.Application.Authentication;
using XIVLauncherProxy.Application.Configuration;
using XIVLauncherProxy.Application.Launching;
using XIVLauncherProxy.Application.Errors;
using XIVLauncherProxy.Desktop.Localization;
using XIVLauncherProxy.Desktop.Services;
using XIVLauncherProxy.Desktop.ViewModels;
using XIVLauncherProxy.Infrastructure.Accounts;
using XIVLauncherProxy.Infrastructure.Authentication;
using XIVLauncherProxy.Infrastructure.Configuration;
using XIVLauncherProxy.Infrastructure.Networking;
using XIVLauncherProxy.Platform.Windows;
using XIVLauncherProxy.Platform.Windows.Launching;
using XIVLauncherProxy.Platform.Windows.Security;
using XIVLauncherProxy.Platform.Windows.Shortcuts;

namespace XIVLauncherProxy.Desktop;

public sealed class ApplicationComposition
{
    public IConfigurationStore Settings { get; }
    public LaunchProfileUseCase LaunchProfile { get; }

    private readonly IXivLauncherAccountSource accounts;
    private readonly IProxyConnectionTester proxyTester;
    private readonly ProfileService profiles;
    private readonly ConfigurationValidator validator;
    private readonly ImportOtpFromImagesUseCase otpImporter;

    private ApplicationComposition(
        IConfigurationStore settings,
        IXivLauncherAccountSource accounts,
        IProxyConnectionTester proxyTester,
        ProfileService profiles,
        ConfigurationValidator validator,
        ImportOtpFromImagesUseCase otpImporter,
        LaunchProfileUseCase launchProfile)
    {
        Settings = settings;
        this.accounts = accounts;
        this.proxyTester = proxyTester;
        this.profiles = profiles;
        this.validator = validator;
        this.otpImporter = otpImporter;
        LaunchProfile = launchProfile;
    }

    public static ApplicationComposition Create()
    {
        if (!OperatingSystem.IsWindows())
            throw new ProxyException("WindowsOnly");
        string executable = Environment.ProcessPath
            ?? throw new ProxyException("ShortcutCreateFailed");
        IConfigurationStore settings = new JsonConfigurationStore(
            WindowsPaths.ConfigurationPath, WindowsPaths.DefaultLauncherPath);
        ITotpCredentialStore credentials = new DpapiTotpCredentialStore(WindowsPaths.CredentialPath);
        var validator = new ConfigurationValidator(credentials, new WindowsLauncherInstallation());
        IShortcutService shortcuts = new WindowsShortcutService(
            executable, Path.GetDirectoryName(executable) ?? AppContext.BaseDirectory);
        var profiles = new ProfileService(settings, credentials, shortcuts, validator);
        var otp = new HttpOtpSubmissionService(credentials, TimeProvider.System);
        var launch = new LaunchProfileUseCase(
            validator,
            new WindowsLauncherProcess(),
            new WindowsAutologinSessionFactory(WindowsPaths.LauncherSettingsPath),
            otp);
        return new ApplicationComposition(
            settings,
            new JsonXivLauncherAccountSource(WindowsPaths.AccountListPath),
            new TcpProxyConnectionTester(),
            profiles,
            validator,
            new ImportOtpFromImagesUseCase(new ImageSharpQrCodeDecoder()),
            launch);
    }

    public MainViewModel CreateMainViewModel(IWindowService windows, string? initialErrorCode) =>
        new(Settings, accounts, proxyTester, profiles, validator, otpImporter, windows,
            ResourceTextLocalizer.Instance, initialErrorCode);
}
