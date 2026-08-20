namespace XIVLauncherProxy.Application.Abstractions;

public interface IOtpSubmissionService
{
    Task SubmitWhenRequestedAsync(string credentialKey, CancellationToken cancellationToken);
}
