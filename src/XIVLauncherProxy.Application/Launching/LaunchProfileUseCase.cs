using XIVLauncherProxy.Application.Abstractions;
using XIVLauncherProxy.Application.Configuration;
using XIVLauncherProxy.Domain.Configuration;

namespace XIVLauncherProxy.Application.Launching;

public sealed class LaunchProfileUseCase(
    ConfigurationValidator validator,
    ILauncherProcess launcher,
    IAutologinSessionFactory autologin,
    IOtpSubmissionService otpSubmission)
{
    public async Task ExecuteAsync(
        AppConfiguration configuration,
        LaunchProfile profile,
        CancellationToken cancellationToken = default)
    {
        validator.ValidateProfile(configuration, profile);
        if (!profile.UseQuickLogin)
        {
            launcher.Start(configuration, profile);
            return;
        }

        using IAutologinSession session = autologin.EnableTemporarily();
        launcher.Start(configuration, profile);
        await otpSubmission.SubmitWhenRequestedAsync(profile.CredentialKey, cancellationToken);
    }
}
