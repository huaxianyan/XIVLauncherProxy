using Avalonia;
using XIVLauncherProxy.Application.Errors;
using XIVLauncherProxy.Domain.Configuration;
using XIVLauncherProxy.Platform.Windows.Updates;

namespace XIVLauncherProxy.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (OperatingSystem.IsWindows()
            && WindowsSelfUpdate.TryRunUpdater(args, out int updaterExitCode))
        {
            Environment.ExitCode = updaterExitCode;
            return;
        }
        if (OperatingSystem.IsWindows())
            args = WindowsSelfUpdate.ScheduleCleanupAndRemoveArguments(args);

        try
        {
            StartupState.Composition = ApplicationComposition.Create();
            if (args.Any(x => x.Equals("--launch", StringComparison.OrdinalIgnoreCase)))
            {
                RunLaunchProfile(args);
                return;
            }
        }
        catch (Exception exception)
        {
            StartupState.ErrorCode = ErrorCodes.From(exception);
        }
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static void RunLaunchProfile(string[] args)
    {
        string? profileId = args.FirstOrDefault(
            x => x.StartsWith("--profile=", StringComparison.OrdinalIgnoreCase))?[10..];
        AppConfiguration configuration = StartupState.Composition!.Settings.Load();
        LaunchProfile? profile = configuration.LaunchProfiles.FirstOrDefault(
            x => x.Id.Equals(profileId, StringComparison.OrdinalIgnoreCase));
        if (profile is null)
            throw new ProxyException("ProfileNotFound");
        StartupState.Composition.LaunchProfile.ExecuteAsync(configuration, profile)
            .GetAwaiter().GetResult();
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
}
