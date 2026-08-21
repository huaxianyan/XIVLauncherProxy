using System.Security.Cryptography;
using XIVLauncherProxy.Platform.Windows.Updates;

namespace XIVLauncherProxy.Platform.Windows.Tests;

public sealed class WindowsUpdateApplierTests : IDisposable
{
    private readonly string root = Path.Combine(
        Path.GetTempPath(), $"XIVLauncherProxy-update-applier-{Guid.NewGuid():N}");

    [Fact]
    public async Task VerifiedUpdateReplacesTheInstalledExecutable()
    {
        WindowsUpdatePlan plan = await CreatePlanAsync();

        await new WindowsUpdateApplier().ApplyAsync(plan, restartApplication: false);

        Assert.Equal("new version", await File.ReadAllTextAsync(plan.TargetExecutablePath));
        Assert.False(File.Exists($"{plan.TargetExecutablePath}.update-backup"));
    }

    [Fact]
    public async Task ReplacementFailureRestoresTheInstalledExecutable()
    {
        WindowsUpdatePlan plan = await CreatePlanAsync();
        var applier = new WindowsUpdateApplier(new FailAfterBackup());

        await Assert.ThrowsAsync<IOException>(() =>
            applier.ApplyAsync(plan, restartApplication: false));

        Assert.Equal("old version", await File.ReadAllTextAsync(plan.TargetExecutablePath));
        Assert.False(File.Exists($"{plan.TargetExecutablePath}.update-backup"));
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }

    private async Task<WindowsUpdatePlan> CreatePlanAsync()
    {
        string installDirectory = Path.Combine(root, "install");
        string workingDirectory = Path.Combine(root, "updates", Guid.NewGuid().ToString("N"));
        string packageDirectory = Path.Combine(workingDirectory, "package");
        Directory.CreateDirectory(installDirectory);
        Directory.CreateDirectory(packageDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(workingDirectory, ".xivlauncherproxy-update"), "v3.2.0");
        string targetPath = Path.Combine(installDirectory, "XIVLauncherProxy.exe");
        string packagePath = Path.Combine(packageDirectory, "XIVLauncherProxy.exe");
        await File.WriteAllTextAsync(targetPath, "old version");
        await File.WriteAllTextAsync(packagePath, "new version");
        string hash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(packagePath)));
        return new WindowsUpdatePlan(
            int.MaxValue, targetPath, packagePath, workingDirectory, hash);
    }

    private sealed class FailAfterBackup : IWindowsUpdateFaultInjector
    {
        public void BeforeCommit() { }
        public void AfterBackupCreated() => throw new IOException("Simulated replacement failure.");
    }
}
