namespace XIVLauncherProxy.Application.Abstractions;

public interface IOtpSubmissionService
{
    /// <summary>
    /// Submits the current code once XIVLauncher requests it.
    /// Returns <see langword="false"/> when no request was observed before the wait expires.
    /// </summary>
    Task<bool> SubmitWhenRequestedAsync(string credentialKey, CancellationToken cancellationToken);
}
