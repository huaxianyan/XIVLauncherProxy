using XIVLauncherProxy.Application.Abstractions;
using XIVLauncherProxy.Application.Configuration;
using XIVLauncherProxy.Application.Launching;
using XIVLauncherProxy.Domain.Configuration;

namespace XIVLauncherProxy.Application.Tests;

public sealed class LaunchProfileUseCaseTests
{
    [Fact]
    public async Task QuickLoginFailureAfterLaunchIsRecordedInsteadOfReported()
    {
        var log = new RecordingDiagnosticLog();
        var harness = Create(log, new ThrowingOtpSubmission());

        await harness.UseCase.ExecuteAsync(harness.Configuration, harness.Profile);

        Assert.Equal("OtpSubmitFailed", Assert.Single(log.Entries).Code);
        Assert.True(harness.Session.Restored);
    }

    [Fact]
    public async Task QuickLoginWithoutObservedRequestIsRecorded()
    {
        var log = new RecordingDiagnosticLog();
        var harness = Create(log, new SilentOtpSubmission());

        await harness.UseCase.ExecuteAsync(harness.Configuration, harness.Profile);

        Assert.Equal("OtpNotRequested", Assert.Single(log.Entries).Code);
        Assert.True(harness.Session.Restored);
    }

    [Fact]
    public async Task LauncherStartFailureIsReportedAndStillRestoresAutologin()
    {
        var log = new RecordingDiagnosticLog();
        var harness = Create(log, new SilentOtpSubmission(), failToStart: true);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            harness.UseCase.ExecuteAsync(harness.Configuration, harness.Profile));

        Assert.Empty(log.Entries);
        Assert.True(harness.Session.Restored);
    }

    private static Harness Create(
        RecordingDiagnosticLog log,
        IOtpSubmissionService otpSubmission,
        bool failToStart = false)
    {
        var configuration = new AppConfiguration
        {
            LauncherPath = "XIVLauncher.exe",
            LaunchProfiles = [],
        };
        var profile = new LaunchProfile
        {
            Name = "测试",
            UseQuickLogin = true,
            UserName = "tester",
            XivLauncherAccountId = "account-id",
        };
        configuration.LaunchProfiles.Add(profile);
        var validator = new ConfigurationValidator(
            new AlwaysPresentCredentials(),
            new AcceptingLauncherInstallation());
        var session = new RecordingAutologinSession();
        return new Harness(
            new LaunchProfileUseCase(
                validator,
                new FakeLauncherProcess(failToStart),
                new FakeAutologinSessionFactory(session),
                otpSubmission,
                log),
            configuration,
            profile,
            session);
    }

    private sealed record Harness(
        LaunchProfileUseCase UseCase,
        AppConfiguration Configuration,
        LaunchProfile Profile,
        RecordingAutologinSession Session);

    private sealed class RecordingDiagnosticLog : IDiagnosticLog
    {
        public List<(string Code, string ProfileId)> Entries { get; } = [];

        public void Report(string code, LaunchProfile profile, Exception? exception = null) =>
            Entries.Add((code, profile.Id));
    }

    private sealed class ThrowingOtpSubmission : IOtpSubmissionService
    {
        public Task<bool> SubmitWhenRequestedAsync(string credentialKey, CancellationToken cancellationToken) =>
            throw new Application.Errors.ProxyException("OtpSubmitFailed");
    }

    private sealed class SilentOtpSubmission : IOtpSubmissionService
    {
        public Task<bool> SubmitWhenRequestedAsync(string credentialKey, CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }

    private sealed class FakeAutologinSessionFactory(IAutologinSession session) : IAutologinSessionFactory
    {
        public IAutologinSession EnableTemporarily() => session;
    }

    private sealed class RecordingAutologinSession : IAutologinSession
    {
        public bool Restored { get; private set; }

        public void Dispose() => Restored = true;
    }

    private sealed class FakeLauncherProcess(bool failToStart) : ILauncherProcess
    {
        public void Start(AppConfiguration configuration, LaunchProfile profile)
        {
            if (failToStart)
                throw new Application.Errors.ProxyException("LauncherStartFailed");
        }
    }

    private sealed class AlwaysPresentCredentials : ITotpCredentialStore
    {
        public bool Contains(string credentialKey) => true;
        public void Save(string credentialKey, string normalizedSecret) { }
        public string Get(string credentialKey) => "JBSWY3DPEHPK3PXP";
        public void Remove(string credentialKey) { }
    }

    private sealed class AcceptingLauncherInstallation : ILauncherInstallation
    {
        public bool Exists(string launcherPath) => true;
        public bool HasExpectedFileName(string launcherPath) => true;
    }
}
