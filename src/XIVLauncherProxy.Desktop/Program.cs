using Avalonia;
using XIVLauncherProxy.Application.Errors;
using XIVLauncherProxy.Domain.Configuration;

namespace XIVLauncherProxy.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            StartupState.Composition = ApplicationComposition.Create();
            if (args.Any(x => x.Equals("--launch", StringComparison.OrdinalIgnoreCase)))
            {
                string? profileId = args.FirstOrDefault(x => x.StartsWith("--profile=", StringComparison.OrdinalIgnoreCase))?[10..];
                AppConfiguration configuration = StartupState.Composition.Settings.Load();
                LaunchProfile? profile = configuration.LaunchProfiles.FirstOrDefault(
                    x => x.Id.Equals(profileId, StringComparison.OrdinalIgnoreCase));
                if (profile is null)
                    throw new ProxyException("ProfileNotFound");
                StartupState.Composition.LaunchProfile.ExecuteAsync(configuration, profile)
                    .GetAwaiter().GetResult();
                return;
            }
        }
        catch (Exception exception)
        {
            StartupState.ErrorCode = exception switch
            {
                ProxyException proxy => proxy.Code,
                FormatException format => format.Message,
                _ => "UnexpectedError",
            };
        }
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
}
