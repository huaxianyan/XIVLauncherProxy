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
            window = new MainWindow
            {
                DataContext = composition.CreateMainViewModel(windowService, StartupState.ErrorCode),
            };
            desktop.MainWindow = window;
        }
        base.OnFrameworkInitializationCompleted();
    }
}
