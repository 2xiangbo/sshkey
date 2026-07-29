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
