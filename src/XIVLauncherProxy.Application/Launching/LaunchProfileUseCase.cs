using XIVLauncherProxy.Application.Abstractions;
using XIVLauncherProxy.Application.Configuration;
using XIVLauncherProxy.Application.Errors;
using XIVLauncherProxy.Domain.Configuration;

namespace XIVLauncherProxy.Application.Launching;

public sealed class LaunchProfileUseCase(
    ConfigurationValidator validator,
    ILauncherProcess launcher,
    IAutologinSessionFactory autologin,
    IOtpSubmissionService otpSubmission,
    IDiagnosticLog diagnostics)
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

        IAutologinSession session = autologin.EnableTemporarily();
        try
        {
            launcher.Start(configuration, profile);
        }
        catch
        {
            session.Dispose();
            throw;
        }

        // XIVLauncher is on screen and owns the login result from here on. A failure in this phase can
        // come from XIVLauncher itself, so it is recorded instead of opening our own configuration window.
        try
        {
            if (!await otpSubmission.SubmitWhenRequestedAsync(profile.CredentialKey, cancellationToken))
                diagnostics.Report("OtpNotRequested", profile);
        }
        catch (Exception exception)
        {
            diagnostics.Report(ErrorCodes.From(exception), profile, exception);
        }
        finally
        {
            try
            {
                session.Dispose();
            }
            catch (Exception exception)
            {
                diagnostics.Report(ErrorCodes.From(exception), profile, exception);
            }
        }
    }
}
