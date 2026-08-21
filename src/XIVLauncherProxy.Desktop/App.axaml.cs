using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using XIVLauncherProxy.Desktop.Services;
using XIVLauncherProxy.Desktop.Views;

namespace XIVLauncherProxy.Desktop;

public partial class App : Avalonia.Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            MainWindow? window = null;
            ApplicationComposition composition = StartupState.Composition
                ?? ApplicationComposition.Create();
            var windowService = new AvaloniaWindowService(() => window);
            var updateInstaller = new WindowsApplicationUpdateInstaller(desktop);
            var viewModel = composition.CreateMainViewModel(
                windowService, updateInstaller, StartupState.ErrorCode);
            window = new MainWindow { DataContext = viewModel };
            desktop.MainWindow = window;
            _ = viewModel.CheckForUpdatesAsync();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
