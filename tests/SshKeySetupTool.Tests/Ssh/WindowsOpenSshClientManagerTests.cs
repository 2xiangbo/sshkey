using SshKeySetupTool.Ssh;
using System.ComponentModel;
using System.Diagnostics;

namespace SshKeySetupTool.Tests.Ssh;

public sealed class WindowsOpenSshClientManagerTests
{
    [Fact]
    public async Task CheckAsync_ReturnsMissingWhenResolverCannotFindSsh()
    {
        var manager = new WindowsOpenSshClientManager(
            () => throw new FileNotFoundException(),
            (_, _) => Task.FromResult(0));

        var result = await manager.CheckAsync(CancellationToken.None);

        Assert.Equal(OpenSshClientStatus.Missing, result);
    }

    [Fact]
    public async Task CheckAsync_ReturnsInstalledWhenResolverSucceeds()
    {
        var manager = new WindowsOpenSshClientManager(
            () => new WindowsOpenSshExecutables(@"C:\Windows\System32\OpenSSH\ssh.exe"),
            (_, _) => Task.FromResult(0));

        var result = await manager.CheckAsync(CancellationToken.None);

        Assert.Equal(OpenSshClientStatus.Installed, result);
    }

    [Fact]
    public async Task CheckAsync_DoesNotBlockBeforeTheResolverCompletes()
    {
        var resolverStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var releaseResolver = new ManualResetEventSlim();
        var manager = new WindowsOpenSshClientManager(
            () =>
            {
                resolverStarted.SetResult();
                releaseResolver.Wait();
                return new WindowsOpenSshExecutables(@"C:\Windows\System32\OpenSSH\ssh.exe");
            },
            (_, _) => Task.FromResult(0));

        var invocation = Task.Factory.StartNew(
            () => manager.CheckAsync(CancellationToken.None),
            CancellationToken.None,
            TaskCreationOptions.DenyChildAttach,
            TaskScheduler.Default);
        await resolverStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Task<OpenSshClientStatus> checkTask;
        try
        {
            Assert.True(invocation.IsCompleted);
            checkTask = await invocation;
            Assert.False(checkTask.IsCompleted);
        }
        finally
        {
            releaseResolver.Set();
        }

        Assert.Equal(OpenSshClientStatus.Installed, await checkTask);
    }

    [Fact]
    public async Task InstallAsync_ReturnsInstallFailedForANonzeroInstallerExitCode()
    {
        var manager = new WindowsOpenSshClientManager(
            () => throw new FileNotFoundException(),
            (_, _) => Task.FromResult(1));

        var result = await manager.InstallAsync(CancellationToken.None);

        Assert.Equal(OpenSshClientStatus.InstallFailed, result);
    }

    [Fact]
    public async Task InstallAsync_ReturnsInstallCancelledWhenUacIsDeclined()
    {
        var manager = new WindowsOpenSshClientManager(
            () => throw new FileNotFoundException(),
            (_, _) => Task.FromException<int>(new Win32Exception(1223)));

        var result = await manager.InstallAsync(CancellationToken.None);

        Assert.Equal(OpenSshClientStatus.InstallCancelled, result);
    }

    [Fact]
    public void CreateInstallStartInfo_RequestsElevationForTheOpenSshCapability()
    {
        var startInfo = WindowsOpenSshClientManager.CreateInstallStartInfo();

        Assert.Equal("powershell.exe", startInfo.FileName);
        Assert.True(startInfo.UseShellExecute);
        Assert.Equal("runas", startInfo.Verb);
        Assert.Contains("OpenSSH.Client~~~~0.0.1.0", startInfo.Arguments);
    }
}
