using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XIVLauncherProxy.Application.Abstractions;
using XIVLauncherProxy.Application.Configuration;
using XIVLauncherProxy.Application.Authentication;
using XIVLauncherProxy.Application.Errors;
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
    private readonly IWindowService windows;
    private readonly ITextLocalizer text;
    private readonly AppConfiguration configuration;

    public ObservableCollection<LaunchProfile> Profiles { get; } = [];
    public ObservableCollection<AccountOption> Accounts { get; } = [];

    [ObservableProperty] private bool isGeneralPage = true;
    [ObservableProperty] private bool isProfilesPage;
    [ObservableProperty] private string launcherPath = string.Empty;
    [ObservableProperty] private string proxyAddress = string.Empty;
    [ObservableProperty] private bool setAllProxy;
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
        this.windows = windows;
        this.text = text;
        configuration = settings.Load();
        LauncherPath = configuration.LauncherPath;
        ProxyAddress = configuration.ProxyUrl;
        SetAllProxy = configuration.SetAllProxy;
        BypassLocalAddresses = configuration.BypassLocalAddresses;
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
        if (!XIVLauncherProxy.Application.Configuration.ProxyAddress.TryParse(ProxyAddress, out Uri? uri))
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

    partial void OnProxyAddressChanged(string value)
    {
        if (XIVLauncherProxy.Application.Configuration.ProxyAddress.TryParse(value, out _) || !useProxy)
            return;
        useProxy = false;
        OnPropertyChanged(nameof(UseProxy));
    }

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
        configuration.ProxyUrl = ProxyAddress.Trim();
        configuration.SetAllProxy = SetAllProxy;
        configuration.BypassLocalAddresses = BypassLocalAddresses;
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

        if (requireProxy
            && !XIVLauncherProxy.Application.Configuration.ProxyAddress.TryParse(ProxyAddress, out _))
        {
            if (!loadingProfile)
                StatusMessage = text["ConfigureProxyFirst"];
            return false;
        }
        return true;
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
