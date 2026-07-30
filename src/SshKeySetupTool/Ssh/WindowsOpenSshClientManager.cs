using System.ComponentModel;
using System.Diagnostics;

namespace SshKeySetupTool.Ssh;

public enum OpenSshClientStatus
{
    Installed,
    Missing,
    CheckFailed,
    InstallFailed,
    InstallCancelled
}

public interface IOpenSshClientManager
{
    Task<OpenSshClientStatus> CheckAsync(CancellationToken cancellationToken);

    Task<OpenSshClientStatus> InstallAsync(CancellationToken cancellationToken);
}

public sealed class WindowsOpenSshClientManager : IOpenSshClientManager
{
    private readonly Func<WindowsOpenSshExecutables> _resolveExecutables;
    private readonly Func<ProcessStartInfo, CancellationToken, Task<int>> _runElevatedInstaller;

    public WindowsOpenSshClientManager()
        : this(WindowsOpenSshExecutableResolver.Resolve, RunElevatedInstallerAsync)
    {
    }

    internal WindowsOpenSshClientManager(
        Func<WindowsOpenSshExecutables> resolveExecutables,
        Func<ProcessStartInfo, CancellationToken, Task<int>> runElevatedInstaller)
    {
        _resolveExecutables = resolveExecutables ?? throw new ArgumentNullException(nameof(resolveExecutables));
        _runElevatedInstaller = runElevatedInstaller ?? throw new ArgumentNullException(nameof(runElevatedInstaller));
    }

    public Task<OpenSshClientStatus> CheckAsync(CancellationToken cancellationToken) =>
        Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                _ = _resolveExecutables();
                return OpenSshClientStatus.Installed;
            }
            catch (FileNotFoundException)
            {
                return OpenSshClientStatus.Missing;
            }
        }, cancellationToken);

    public async Task<OpenSshClientStatus> InstallAsync(CancellationToken cancellationToken)
    {
        try
        {
            var exitCode = await _runElevatedInstaller(CreateInstallStartInfo(), cancellationToken);
            if (exitCode != 0)
            {
                return OpenSshClientStatus.InstallFailed;
            }

            return await CheckAsync(cancellationToken) == OpenSshClientStatus.Installed
                ? OpenSshClientStatus.Installed
                : OpenSshClientStatus.InstallFailed;
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode == 1223)
        {
            return OpenSshClientStatus.InstallCancelled;
        }
    }

    internal static ProcessStartInfo CreateInstallStartInfo() => new(
        "powershell.exe",
        "-NoProfile -NonInteractive -Command \"Add-WindowsCapability -Online -Name OpenSSH.Client~~~~0.0.1.0\"")
    {
        UseShellExecute = true,
        Verb = "runas"
    };

    private static async Task<int> RunElevatedInstallerAsync(
        ProcessStartInfo startInfo,
        CancellationToken cancellationToken)
    {
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("The OpenSSH installer process could not be started.");
        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode;
    }
}
