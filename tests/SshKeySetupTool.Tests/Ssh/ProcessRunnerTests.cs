using SshKeySetupTool.Ssh;
using System.Diagnostics;

namespace SshKeySetupTool.Tests.Ssh;

public sealed class ProcessRunnerTests : IDisposable
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), $"process-runner-{Guid.NewGuid():N}");

    [Fact]
    public async Task RunAsync_AlreadyCanceledTokenNeverAttemptsToLaunchProcess()
    {
        var executablePath = Path.Combine(_directory, "must-not-launch.exe");
        Assert.False(File.Exists(executablePath));
        var startInfo = new ProcessStartInfo(executablePath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new ProcessRunner().RunAsync(
                startInfo,
                TimeSpan.FromSeconds(30),
                cancellation.Token));
    }

    [Fact]
    public async Task RunAsync_CancellationKillsAndAwaitsTheEntireProcessTree()
    {
        Directory.CreateDirectory(_directory);
        var processIdsPath = Path.Combine(_directory, "process-ids.txt");
        using var cancellation = new CancellationTokenSource();
        var runner = new ProcessRunner();
        var runTask = runner.RunAsync(
            CreateParentWithChildStartInfo(processIdsPath),
            TimeSpan.FromSeconds(30),
            cancellation.Token);
        var processIds = await ReadProcessIdsAsync(processIdsPath);

        try
        {
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
            Assert.All(processIds, processId => Assert.False(IsRunning(processId)));
        }
        finally
        {
            KillForTestCleanup(processIds);
        }
    }

    [Fact]
    public async Task RunAsync_TimeoutKillsAndAwaitsTheProcess()
    {
        Directory.CreateDirectory(_directory);
        var processIdsPath = Path.Combine(_directory, "timeout-process-id.txt");
        var runner = new ProcessRunner();
        var runTask = runner.RunAsync(
            CreateWaitingParentStartInfo(processIdsPath),
            TimeSpan.FromSeconds(2),
            CancellationToken.None);
        var processIds = await ReadProcessIdsAsync(processIdsPath);

        try
        {
            await Assert.ThrowsAsync<TimeoutException>(() => runTask);
            Assert.All(processIds, processId => Assert.False(IsRunning(processId)));
        }
        finally
        {
            KillForTestCleanup(processIds);
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private static ProcessStartInfo CreateParentWithChildStartInfo(string processIdsPath)
    {
        var script = string.Join(
            "; ",
            "$child = Start-Process -FilePath \"$env:WINDIR\\System32\\PING.EXE\" -ArgumentList '-t','127.0.0.1' -PassThru -WindowStyle Hidden",
            $"Set-Content -LiteralPath '{EscapePowerShellLiteral(processIdsPath)}' -Value \"$PID,$($child.Id)\" -NoNewline",
            "Wait-Process -Id $child.Id");
        return CreatePowerShellStartInfo(script);
    }

    private static ProcessStartInfo CreateWaitingParentStartInfo(string processIdsPath)
    {
        var script = string.Join(
            "; ",
            $"Set-Content -LiteralPath '{EscapePowerShellLiteral(processIdsPath)}' -Value $PID -NoNewline",
            "Start-Sleep -Seconds 60");
        return CreatePowerShellStartInfo(script);
    }

    private static ProcessStartInfo CreatePowerShellStartInfo(string script)
    {
        var startInfo = new ProcessStartInfo(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "System32",
                "WindowsPowerShell",
                "v1.0",
                "powershell.exe"))
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("-NoLogo");
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add(script);
        return startInfo;
    }

    private static async Task<int[]> ReadProcessIdsAsync(string path)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (!File.Exists(path) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25);
        }

        Assert.True(File.Exists(path), "The process did not publish its process IDs.");
        var text = await File.ReadAllTextAsync(path);
        return text.Split(',').Select(int.Parse).ToArray();
    }

    private static bool IsRunning(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static void KillForTestCleanup(IEnumerable<int> processIds)
    {
        foreach (var processId in processIds)
        {
            try
            {
                using var process = Process.GetProcessById(processId);
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }
            catch (ArgumentException)
            {
            }
        }
    }

    private static string EscapePowerShellLiteral(string value) =>
        value.Replace("'", "''", StringComparison.Ordinal);
}
