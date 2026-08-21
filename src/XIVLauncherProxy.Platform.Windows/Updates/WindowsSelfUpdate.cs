using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using XIVLauncherProxy.Application.Updates;

namespace XIVLauncherProxy.Platform.Windows.Updates;

public sealed record WindowsUpdatePlan(
    int ParentProcessId,
    string TargetExecutablePath,
    string PackageExecutablePath,
    string WorkingDirectory,
    string PackageSha256);

public interface IWindowsUpdateFaultInjector
{
    void BeforeCommit();
    void AfterBackupCreated();
}

public sealed class NoWindowsUpdateFaultInjector : IWindowsUpdateFaultInjector
{
    public void BeforeCommit() { }
    public void AfterBackupCreated() { }
}

public static class WindowsSelfUpdate
{
    public const string ApplyArgument = "--apply-update";
    public const string CleanupArgument = "--cleanup-update";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public static bool IsSupportedInstallation =>
        OperatingSystem.IsWindows()
        && Environment.ProcessPath is { } processPath
        && string.Equals(Path.GetFileName(processPath), "XIVLauncherProxy.exe", StringComparison.OrdinalIgnoreCase)
        && !File.Exists(Path.Combine(AppContext.BaseDirectory, "XIVLauncherProxy.dll"));

    public static bool TryRunUpdater(string[] args, out int exitCode)
    {
        exitCode = 0;
        if (args.Length != 2 || args[0] != ApplyArgument)
            return false;
        WindowsUpdatePlan? plan = null;
        try
        {
            string planPath = Path.GetFullPath(args[1]);
            plan = JsonSerializer.Deserialize<WindowsUpdatePlan>(
                    File.ReadAllText(planPath), SerializerOptions)
                ?? throw new InvalidDataException("The update plan is empty.");
            string logPath = Path.Combine(plan.WorkingDirectory, "update.log");
            WriteLog(logPath, "Updater started.");
            new WindowsUpdateApplier().ApplyAsync(plan, restartApplication: true)
                .GetAwaiter().GetResult();
            WriteLog(logPath, "Update completed.");
        }
        catch (Exception exception)
        {
            exitCode = 1;
            try
            {
                WriteLog(Path.Combine(Path.GetTempPath(), "XIVLauncherProxy-update-error.log"), exception.ToString());
            }
            catch { }
            if (plan is not null && File.Exists(plan.TargetExecutablePath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = plan.TargetExecutablePath,
                        WorkingDirectory = Path.GetDirectoryName(plan.TargetExecutablePath)!,
                        UseShellExecute = false,
                    });
                }
                catch { }
            }
        }
        return true;
    }

    public static void Launch(PreparedApplicationUpdate preparedUpdate)
    {
        if (!IsSupportedInstallation)
            throw new NotSupportedException("Automatic update requires the Windows single-file release.");
        string targetPath = Path.GetFullPath(Environment.ProcessPath!);
        string targetDirectory = Path.GetDirectoryName(targetPath)!;
        string writeProbePath = Path.Combine(targetDirectory, $".xivlauncherproxy-update-{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllBytes(writeProbePath, []);
        }
        finally
        {
            if (File.Exists(writeProbePath)) File.Delete(writeProbePath);
        }
        var plan = new WindowsUpdatePlan(
            Environment.ProcessId,
            targetPath,
            Path.GetFullPath(preparedUpdate.PackageExecutablePath),
            Path.GetFullPath(preparedUpdate.WorkingDirectory),
            preparedUpdate.PackageSha256);
        string planPath = Path.Combine(preparedUpdate.WorkingDirectory, "update-plan.json");
        File.WriteAllText(planPath, JsonSerializer.Serialize(plan, SerializerOptions));
        var startInfo = new ProcessStartInfo
        {
            FileName = Path.GetFullPath(preparedUpdate.UpdaterExecutablePath),
            WorkingDirectory = preparedUpdate.WorkingDirectory,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add(ApplyArgument);
        startInfo.ArgumentList.Add(planPath);
        _ = Process.Start(startInfo)
            ?? throw new InvalidOperationException("The update process could not be started.");
    }

    public static string[] ScheduleCleanupAndRemoveArguments(string[] args)
    {
        if (args.Length != 3 || args[0] != CleanupArgument || !int.TryParse(args[2], out int updaterProcessId))
            return args;
        string workingDirectory = Path.GetFullPath(args[1]);
        _ = Task.Run(async () =>
        {
            try
            {
                using Process updater = Process.GetProcessById(updaterProcessId);
                await updater.WaitForExitAsync();
            }
            catch (ArgumentException) { }
            for (int attempt = 0; attempt < 10; attempt++)
            {
                try
                {
                    if (File.Exists(Path.Combine(workingDirectory, ".xivlauncherproxy-update")))
                        Directory.Delete(workingDirectory, recursive: true);
                    return;
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    await Task.Delay(500);
                }
            }
        });
        return [];
    }

    private static void WriteLog(string path, string message)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.AppendAllText(path, $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
    }
}

public sealed class WindowsUpdateApplier(IWindowsUpdateFaultInjector? faultInjector = null)
{
    private readonly IWindowsUpdateFaultInjector faultInjector =
        faultInjector ?? new NoWindowsUpdateFaultInjector();

    public async Task ApplyAsync(
        WindowsUpdatePlan plan,
        bool restartApplication,
        CancellationToken cancellationToken = default)
    {
        ValidatePlan(plan);
        await WaitForParentExitAsync(plan.ParentProcessId, cancellationToken);
        string targetPath = Path.GetFullPath(plan.TargetExecutablePath);
        string targetDirectory = Path.GetDirectoryName(targetPath)!;
        string temporaryPath = Path.Combine(
            targetDirectory, $".{Path.GetFileName(targetPath)}.{Guid.NewGuid():N}.update");
        string backupPath = $"{targetPath}.update-backup";
        bool backupCreated = false;
        try
        {
            string sourceHash = await ComputeSha256Async(plan.PackageExecutablePath, cancellationToken);
            if (!string.Equals(sourceHash, plan.PackageSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The staged update checksum is invalid.");
            File.Copy(plan.PackageExecutablePath, temporaryPath, overwrite: false);
            string temporaryHash = await ComputeSha256Async(temporaryPath, cancellationToken);
            if (!string.Equals(temporaryHash, plan.PackageSha256, StringComparison.OrdinalIgnoreCase))
                throw new IOException("The update written to the installation directory is invalid.");
            faultInjector.BeforeCommit();
            File.Move(targetPath, backupPath, overwrite: true);
            backupCreated = true;
            faultInjector.AfterBackupCreated();
            File.Move(temporaryPath, targetPath, overwrite: false);
            string installedHash = await ComputeSha256Async(targetPath, cancellationToken);
            if (!string.Equals(installedHash, plan.PackageSha256, StringComparison.OrdinalIgnoreCase))
                throw new IOException("The installed update is invalid.");
            if (restartApplication)
                await StartAndVerifyAsync(targetPath, plan.WorkingDirectory, cancellationToken);
            File.Delete(backupPath);
            backupCreated = false;
        }
        catch
        {
            TryDeleteFile(temporaryPath);
            if (backupCreated || File.Exists(backupPath))
            {
                TryDeleteFile(targetPath);
                File.Move(backupPath, targetPath, overwrite: true);
            }
            throw;
        }
        finally
        {
            TryDeleteFile(temporaryPath);
        }
    }

    private static void ValidatePlan(WindowsUpdatePlan plan)
    {
        string targetPath = Path.GetFullPath(plan.TargetExecutablePath);
        string packagePath = Path.GetFullPath(plan.PackageExecutablePath);
        string workingDirectory = Path.GetFullPath(plan.WorkingDirectory);
        if (plan.ParentProcessId <= 0
            || !File.Exists(targetPath)
            || !File.Exists(packagePath)
            || !File.Exists(Path.Combine(workingDirectory, ".xivlauncherproxy-update"))
            || !IsWithinDirectory(packagePath, workingDirectory)
            || !string.Equals(Path.GetFileName(targetPath), "XIVLauncherProxy.exe", StringComparison.OrdinalIgnoreCase)
            || plan.PackageSha256.Length != 64
            || !plan.PackageSha256.All(Uri.IsHexDigit))
            throw new InvalidDataException("The update plan is invalid.");
    }

    private static bool IsWithinDirectory(string path, string directory)
    {
        string relative = Path.GetRelativePath(directory, path);
        return relative != ".."
            && !relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            && !Path.IsPathRooted(relative);
    }

    private static async Task WaitForParentExitAsync(int processId, CancellationToken cancellationToken)
    {
        try
        {
            using Process process = Process.GetProcessById(processId);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(60));
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (ArgumentException) { }
    }

    private static async Task StartAndVerifyAsync(
        string targetPath,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = targetPath,
            WorkingDirectory = Path.GetDirectoryName(targetPath)!,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add(WindowsSelfUpdate.CleanupArgument);
        startInfo.ArgumentList.Add(workingDirectory);
        startInfo.ArgumentList.Add(Environment.ProcessId.ToString());
        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("The updated application could not be started.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
            throw new InvalidOperationException($"The updated application exited with code {process.ExitCode}.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { }
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read,
            128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        byte[] digest = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(digest);
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
    }
}
