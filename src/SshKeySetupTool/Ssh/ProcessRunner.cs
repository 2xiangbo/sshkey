using System.Diagnostics;

namespace SshKeySetupTool.Ssh;

internal sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);

internal interface IProcessRunner
{
    Task<ProcessResult> RunAsync(
        ProcessStartInfo startInfo,
        TimeSpan timeout,
        CancellationToken cancellationToken);
}

internal sealed class ProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(
        ProcessStartInfo startInfo,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        if (timeout <= TimeSpan.Zero || timeout == Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        using var process = new Process { StartInfo = startInfo };
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!process.Start())
            {
                throw new InvalidOperationException($"Process could not be started: {startInfo.FileName}");
            }
        }
        finally
        {
            startInfo.Environment.Remove(
                WindowsOpenSshPasswordFallback.PasswordEnvironmentVariable);
        }

        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        try
        {
            await process.WaitForExitAsync(timeoutSource.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await TerminateProcessTreeAsync(process);
            await Task.WhenAll(standardOutput, standardError);
            throw new OperationCanceledException(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await TerminateProcessTreeAsync(process);
            await Task.WhenAll(standardOutput, standardError);
            throw new TimeoutException(
                $"Process exceeded its {timeout.TotalSeconds:0.#}-second timeout: {startInfo.FileName}");
        }

        await Task.WhenAll(standardOutput, standardError);
        return new ProcessResult(process.ExitCode, standardOutput.Result, standardError.Result);
    }

    private static async Task TerminateProcessTreeAsync(Process process)
    {
        if (!process.HasExited)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException) when (process.HasExited)
            {
            }
        }

        using var terminationTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await process.WaitForExitAsync(terminationTimeout.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException(
                $"Process tree did not terminate after being killed: {process.StartInfo.FileName}");
        }
    }
}
