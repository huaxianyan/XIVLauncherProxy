using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XIVLauncherProxy.Application.Abstractions;
using XIVLauncherProxy.Application.Configuration;
using XIVLauncherProxy.Application.Authentication;
using XIVLauncherProxy.Application.Errors;
using XIVLauncherProxy.Application.Updates;
using XIVLauncherProxy.Desktop.Localization;
using XIVLauncherProxy.Desktop.Services;
using XIVLauncherProxy.Domain.Accounts;
using XIVLauncherProxy.Domain.Authentication;
using XIVLauncherProxy.Domain.Configuration;

namespace XIVLauncherProxy.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IConfigurationStore settings;
    private readonly IXivLauncherAccountSource accountSource;
    private readonly IProxyConnectionTester proxyTester;
    private readonly ProfileService profileService;
    private readonly ConfigurationValidator validator;
    private readonly ImportOtpFromImagesUseCase otpImporter;
    private readonly IApplicationUpdateService updateService;
    private readonly IApplicationUpdateProxy updateProxy;
    private readonly IApplicationUpdateInstaller updateInstaller;
    private readonly IWindowService windows;
    private readonly ITextLocalizer text;
    private readonly AppConfiguration configuration;

    public ObservableCollection<LaunchProfile> Profiles { get; } = [];
    public ObservableCollection<AccountOption> Accounts { get; } = [];
    public IReadOnlyList<string> ProxySchemes => ProxyAddress.SupportedSchemes;

    [ObservableProperty] private bool isGeneralPage = true;
    [ObservableProperty] private bool isProfilesPage;
    [ObservableProperty] private string launcherPath = string.Empty;
    [ObservableProperty] private string selectedProxyScheme = ProxyAddress.SupportedSchemes[0];
    [ObservableProperty] private string proxyHost = string.Empty;
    [ObservableProperty] private string proxyPort = string.Empty;
    [ObservableProperty] private bool setAllProxy;
    [ObservableProperty] private bool useProxyForUpdates;
    [ObservableProperty] private bool bypassLocalAddresses;
    [ObservableProperty] private LaunchProfile? selectedProfile;
    [ObservableProperty] private AccountOption? selectedAccount;
    [ObservableProperty] private string profileName = string.Empty;
    private bool useProxy;
    private bool useQuickLogin;
    [ObservableProperty] private string totpSecret = string.Empty;
    [ObservableProperty] private string credentialStatus = string.Empty;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CheckForUpdatesCommand))]
    [NotifyCanExecuteChangedFor(nameof(InstallUpdateCommand))]
    private bool isUpdateBusy;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallUpdateCommand))]
    private bool isUpdateAvailable;
    [ObservableProperty] private bool isUpdateProgressIndeterminate = true;
    [ObservableProperty] private double updateProgressValue;
    [ObservableProperty] private string updateStatus = string.Empty;
    private ApplicationRelease? availableRelease;
    private bool loadingProfile;

    public bool UseProxy
    {
        get => useProxy;
        set
        {
            if (value && !CanEnableFeature(requireProxy: true))
            {
                SetProperty(ref useProxy, false);
                return;
            }
            SetProperty(ref useProxy, value);
        }
    }

    public bool UseQuickLogin
    {
        get => useQuickLogin;
        set
        {
            if (value && !CanEnableFeature(requireProxy: false))
            {
                SetProperty(ref useQuickLogin, false);
                return;
            }
            SetProperty(ref useQuickLogin, value);
        }
    }

    public string CurrentVersion => text.Format(
        "CurrentVersion", typeof(MainViewModel).Assembly.GetName().Version?.ToString(3) ?? "—");
    public bool IsAutomaticUpdateSupported => updateInstaller.IsSupported;
    public string PageTitle => text[IsGeneralPage ? "GeneralTitle" : "ProfilesTitle"];
    public string PageSubtitle => text[IsGeneralPage ? "GeneralSubtitle" : "ProfilesSubtitle"];
    public bool HasSelectedProfile => SelectedProfile is not null;

    public MainViewModel(
        IConfigurationStore settings,
        IXivLauncherAccountSource accountSource,
        IProxyConnectionTester proxyTester,
        ProfileService profileService,
        ConfigurationValidator validator,
        ImportOtpFromImagesUseCase otpImporter,
        IApplicationUpdateService updateService,
        IApplicationUpdateProxy updateProxy,
        IApplicationUpdateInstaller updateInstaller,
        IWindowService windows,
        ITextLocalizer text,
        string? initialErrorCode = null)
    {
        this.settings = settings;
        this.accountSource = accountSource;
        this.proxyTester = proxyTester;
        this.profileService = profileService;
        this.validator = validator;
        this.otpImporter = otpImporter;
        this.updateService = updateService;
        this.updateProxy = updateProxy;
        this.updateInstaller = updateInstaller;
        this.windows = windows;
        this.text = text;
        configuration = settings.Load();
        LauncherPath = configuration.LauncherPath;
        if (ProxyAddress.TrySplit(configuration.ProxyUrl, out string scheme, out string host, out string port))
        {
            SelectedProxyScheme = scheme;
            ProxyHost = host;
            ProxyPort = port;
        }
        SetAllProxy = configuration.SetAllProxy;
        UseProxyForUpdates = configuration.UseProxyForUpdates;
        BypassLocalAddresses = configuration.BypassLocalAddresses;
        updateProxy.Configure(UseProxyForUpdates && TryGetProxy(out Uri? updateProxyUri)
            ? updateProxyUri
            : null);
        UpdateStatus = text["UpdateNotChecked"];
        EnsureDefaultProfile();
        RefreshProfileCollection();
        RefreshAccounts(showStatus: false);
        StatusMessage = initialErrorCode is null ? BuildSummary() : text[initialErrorCode];
    }

    [RelayCommand]
    private void ShowGeneral()
    {
        IsGeneralPage = true;
        IsProfilesPage = false;
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(PageSubtitle));
    }

    [RelayCommand]
    private void ShowProfiles()
    {
        IsGeneralPage = false;
        IsProfilesPage = true;
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(PageSubtitle));
    }

    [RelayCommand]
    private async Task BrowseLauncherAsync()
    {
        string? selected = await windows.PickLauncherAsync(LauncherPath);
        if (selected is not null) LauncherPath = selected;
    }

    [RelayCommand]
    private async Task ImportQrAsync()
    {
        IReadOnlyList<string> paths = await windows.PickQrImagesAsync();
        if (paths.Count == 0)
            return;
        IsBusy = true;
        try
        {
            IReadOnlyList<OtpImportEntry> entries = otpImporter.Execute(paths);
            OtpImportEntry? selected = entries.Count == 1
                ? entries[0]
                : await windows.SelectOtpEntryAsync(entries);
            if (selected is null)
                return;
            TotpSecret = selected.Secret;
            StatusMessage = text.Format("OtpImported", selected.DisplayName);
        }
        catch (Exception exception) { ShowError(exception); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void SaveGeneral()
    {
        Run(() =>
        {
            ApplyGeneralSettings();
            validator.Validate(configuration);
            settings.Save(configuration);
            StatusMessage = text["SettingsSaved"];
        });
    }

    [RelayCommand]
    private async Task TestProxyAsync()
    {
        if (!TryGetProxy(out Uri? uri))
        {
            StatusMessage = text["ProxyAddressInvalid"];
            return;
        }
        IsBusy = true;
        try
        {
            await proxyTester.TestAsync(uri!);
            StatusMessage = text.Format("ProxyConnected", uri!.Host, uri.Port);
        }
        catch (Exception exception) { ShowError(exception); }
        finally { IsBusy = false; }
    }

    [RelayCommand(CanExecute = nameof(CanCheckForUpdates))]
    public async Task CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        IsUpdateBusy = true;
        IsUpdateProgressIndeterminate = true;
        UpdateProgressValue = 0;
        UpdateStatus = text["CheckingForUpdates"];
        try
        {
            ApplicationUpdateStatus status = await updateService.CheckAsync(cancellationToken);
            availableRelease = status.IsUpdateAvailable ? status.LatestRelease : null;
            IsUpdateAvailable = availableRelease is not null;
            UpdateStatus = availableRelease is null
                ? text["AlreadyLatestVersion"]
                : text.Format("UpdateAvailable", availableRelease.Version.ToString(3));
        }
        catch (OperationCanceledException)
        {
            UpdateStatus = text["UpdateCheckCanceled"];
        }
        catch (Exception)
        {
            availableRelease = null;
            IsUpdateAvailable = false;
            UpdateStatus = text["UpdateCheckFailed"];
        }
        finally
        {
            IsUpdateBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanInstallUpdate))]
    private async Task InstallUpdateAsync(CancellationToken cancellationToken)
    {
        if (availableRelease is null)
            return;
        IsUpdateBusy = true;
        IsUpdateProgressIndeterminate = true;
        UpdateProgressValue = 0;
        string version = availableRelease.Version.ToString(3);
        UpdateStatus = text.Format("DownloadingUpdate", version);
        var progress = new Progress<ApplicationUpdateProgress>(value =>
            ReportUpdateProgress(value, version));
        try
        {
            PreparedApplicationUpdate prepared = await updateService.PrepareAsync(
                availableRelease, progress, cancellationToken);
            UpdateStatus = text["ApplyingUpdate"];
            updateInstaller.Launch(prepared);
        }
        catch (OperationCanceledException)
        {
            UpdateStatus = text["UpdateCanceled"];
        }
        catch (Exception)
        {
            UpdateStatus = text["UpdateFailed"];
        }
        finally
        {
            IsUpdateBusy = false;
        }
    }

    [RelayCommand]
    private void AddProfile()
    {
        var profile = new LaunchProfile
        {
            Name = text.Format("NewProfileName", configuration.LaunchProfiles.Count + 1),
            UseProxy = true,
        };
        configuration.LaunchProfiles.Add(profile);
        Profiles.Add(profile);
        SelectedProfile = profile;
    }

    [RelayCommand]
    private async Task DeleteProfileAsync()
    {
        LaunchProfile? profile = SelectedProfile;
        if (profile is null || !await windows.ConfirmAsync(text.Format("ConfirmDeleteProfile", profile.Name)))
            return;
        Run(() =>
        {
            profileService.Delete(configuration, profile);
            Profiles.Remove(profile);
            SelectedProfile = Profiles.FirstOrDefault();
            StatusMessage = BuildSummary();
        });
    }

    [RelayCommand]
    private void ReloadAccounts() => RefreshAccounts(showStatus: true);

    [RelayCommand]
    private void SaveProfile()
    {
        Run(() =>
        {
            ApplyProfileEditor();
            ApplyGeneralSettings();
            profileService.Save(configuration, SelectedProfile!, TotpSecret);
            TotpSecret = string.Empty;
            CredentialStatus = text["CredentialSaved"];
            RefreshProfileCollection(SelectedProfile!.Id);
            StatusMessage = text["ProfileSaved"];
        });
    }

    [RelayCommand]
    private void CreateShortcut()
    {
        Run(() =>
        {
            ApplyProfileEditor();
            ApplyGeneralSettings();
            string path = profileService.CreateShortcut(configuration, SelectedProfile!, TotpSecret);
            TotpSecret = string.Empty;
            CredentialStatus = text["CredentialSaved"];
            RefreshProfileCollection(SelectedProfile!.Id);
            StatusMessage = text.Format("ShortcutCreated", Path.GetFileName(path));
        });
    }

    [RelayCommand]
    private async Task ClearSecretAsync()
    {
        LaunchProfile? profile = SelectedProfile;
        if (profile is null || !profileService.HasCredential(profile)
            || !await windows.ConfirmAsync(text.Format("ConfirmClearSecret", profile.UserName)))
            return;
        Run(() =>
        {
            profileService.RemoveCredential(profile);
            CredentialStatus = text["CredentialMissing"];
        });
    }

    partial void OnLauncherPathChanged(string value)
    {
        try
        {
            validator.ValidateLauncherPath(value.Trim());
        }
        catch (Exception)
        {
            if (useProxy) { useProxy = false; OnPropertyChanged(nameof(UseProxy)); }
            if (useQuickLogin) { useQuickLogin = false; OnPropertyChanged(nameof(UseQuickLogin)); }
        }
    }

    partial void OnSelectedProxySchemeChanged(string value) => DisableProxyWhenInvalid();
    partial void OnProxyHostChanged(string value) => DisableProxyWhenInvalid();
    partial void OnProxyPortChanged(string value) => DisableProxyWhenInvalid();

    partial void OnSelectedProfileChanged(LaunchProfile? value)
    {
        OnPropertyChanged(nameof(HasSelectedProfile));
        if (value is null) return;
        loadingProfile = true;
        ProfileName = value.Name;
        UseProxy = value.UseProxy;
        UseQuickLogin = value.UseQuickLogin;
        loadingProfile = false;
        SelectedAccount = Accounts.FirstOrDefault(x => x.Account.AccountId == value.XivLauncherAccountId);
        TotpSecret = string.Empty;
        CredentialStatus = profileService.HasCredential(value)
            ? text["CredentialSaved"] : text["CredentialMissing"];
    }

    private void EnsureDefaultProfile()
    {
        if (configuration.LaunchProfiles.Count == 0)
            configuration.LaunchProfiles.Add(new LaunchProfile { Name = text["DefaultProfileName"], UseProxy = true });
    }

    private void RefreshProfileCollection(string? selectedId = null)
    {
        selectedId ??= SelectedProfile?.Id;
        Profiles.Clear();
        foreach (LaunchProfile profile in configuration.LaunchProfiles) Profiles.Add(profile);
        SelectedProfile = Profiles.FirstOrDefault(x => x.Id == selectedId) ?? Profiles.FirstOrDefault();
        StatusMessage = BuildSummary();
    }

    private void RefreshAccounts(bool showStatus)
    {
        Run(() =>
        {
            string? selectedId = SelectedProfile?.XivLauncherAccountId;
            Accounts.Clear();
            foreach (XivLauncherAccount account in accountSource.LoadAccounts().Where(x => x.UseOtp))
                Accounts.Add(new AccountOption(account));
            SelectedAccount = Accounts.FirstOrDefault(x => x.Account.AccountId == selectedId);
            if (showStatus)
                StatusMessage = Accounts.Count == 0 ? text["NoAccounts"] : text.Format("AccountsLoaded", Accounts.Count);
        });
    }

    private void ApplyGeneralSettings()
    {
        configuration.LauncherPath = LauncherPath.Trim();
        configuration.ProxyUrl = BuildProxyUrl();
        configuration.SetAllProxy = SetAllProxy;
        configuration.UseProxyForUpdates = UseProxyForUpdates;
        configuration.BypassLocalAddresses = BypassLocalAddresses;
        Uri? updateUri = null;
        if (UseProxyForUpdates && !TryGetProxy(out updateUri))
            throw new ProxyException("ProxyAddressInvalid");
        updateProxy.Configure(updateUri);
    }

    private void ApplyProfileEditor()
    {
        if (SelectedProfile is null) throw new ProxyException("ProfileFeatureMissing");
        SelectedProfile.Name = ProfileName.Trim();
        SelectedProfile.UseProxy = UseProxy;
        SelectedProfile.UseQuickLogin = UseQuickLogin;
        if (SelectedAccount is not null)
        {
            SelectedProfile.UserName = SelectedAccount.Account.UserName;
            SelectedProfile.XivLauncherAccountId = SelectedAccount.Account.AccountId;
        }
    }

    private bool CanEnableFeature(bool requireProxy)
    {
        try
        {
            validator.ValidateLauncherPath(LauncherPath.Trim());
        }
        catch (Exception)
        {
            if (!loadingProfile)
                StatusMessage = text["ConfigureLauncherFirst"];
            return false;
        }

        if (requireProxy && !TryGetProxy(out _))
        {
            if (!loadingProfile)
                StatusMessage = text["ConfigureProxyFirst"];
            return false;
        }
        return true;
    }

    private void ReportUpdateProgress(ApplicationUpdateProgress progress, string version)
    {
        if (progress.BytesReceived == 0)
        {
            IsUpdateProgressIndeterminate = true;
            UpdateStatus = text.Format("WaitingForUpdate", version);
            return;
        }
        string received = FormatDownloadSize(progress.BytesReceived);
        if (progress.TotalBytes is not > 0)
        {
            IsUpdateProgressIndeterminate = true;
            UpdateStatus = text.Format("DownloadingUpdateUnknownSize", version, received);
            return;
        }
        double percentage = Math.Clamp(
            progress.BytesReceived * 100d / progress.TotalBytes.Value, 0, 100);
        IsUpdateProgressIndeterminate = false;
        UpdateProgressValue = percentage;
        UpdateStatus = text.Format(
            "DownloadingUpdateProgress",
            version,
            received,
            FormatDownloadSize(progress.TotalBytes.Value),
            Math.Round(percentage));
    }

    private string FormatDownloadSize(long bytes) => bytes < 1_000_000
        ? text.Format("DownloadKilobytes", bytes / 1_000d)
        : text.Format("DownloadMegabytes", bytes / 1_000_000d);

    private bool CanCheckForUpdates() => !IsUpdateBusy;

    private bool CanInstallUpdate() =>
        !IsUpdateBusy && IsUpdateAvailable && updateInstaller.IsSupported;

    private bool TryGetProxy(out Uri? uri) =>
        ProxyAddress.TryCreate(SelectedProxyScheme, ProxyHost, ProxyPort, out uri);

    private string BuildProxyUrl()
    {
        if (string.IsNullOrWhiteSpace(ProxyHost) && string.IsNullOrWhiteSpace(ProxyPort))
            return string.Empty;
        if (!TryGetProxy(out Uri? uri))
            throw new ProxyException("ProxyAddressInvalid");
        return uri!.GetLeftPart(UriPartial.Authority);
    }

    private void DisableProxyWhenInvalid()
    {
        if (TryGetProxy(out _))
            return;
        if (useProxy)
        {
            useProxy = false;
            OnPropertyChanged(nameof(UseProxy));
        }
        if (UseProxyForUpdates)
            UseProxyForUpdates = false;
    }

    private string BuildSummary() => text.Format("StatusSummary",
        configuration.LaunchProfiles.Count,
        configuration.LaunchProfiles.Count(x => x.UseProxy),
        configuration.LaunchProfiles.Count(x => x.UseQuickLogin));

    private void Run(Action action)
    {
        try { action(); }
        catch (Exception exception) { ShowError(exception); }
    }

    private void ShowError(Exception exception)
    {
        string key = exception switch
        {
            ProxyException proxy => proxy.Code,
            FormatException format => format.Message,
            _ => "UnexpectedError",
        };
        StatusMessage = text[key];
    }
}

public sealed record AccountOption(XivLauncherAccount Account)
{
    public string DisplayName => Account.UseSteamServiceAccount ? $"{Account.UserName} (Steam)" : Account.UserName;
}
