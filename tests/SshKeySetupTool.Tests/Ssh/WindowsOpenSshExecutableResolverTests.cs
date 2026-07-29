using SshKeySetupTool.Ssh;

namespace SshKeySetupTool.Tests.Ssh;

public sealed class WindowsOpenSshExecutableResolverTests : IDisposable
{
    private readonly string _windowsDirectory =
        Path.Combine(Path.GetTempPath(), $"fake-windows-{Guid.NewGuid():N}");

    [Fact]
    public void Resolve_UsesValidatedSystemOpenSshExecutables()
    {
        var openSshDirectory = Path.Combine(_windowsDirectory, "System32", "OpenSSH");
        Directory.CreateDirectory(openSshDirectory);
        var sshPath = Path.Combine(openSshDirectory, "ssh.exe");
        File.WriteAllBytes(sshPath, []);

        var executables = WindowsOpenSshExecutableResolver.Resolve(_windowsDirectory);

        Assert.Equal(Path.GetFullPath(sshPath), executables.SshPath);
    }

    [Fact]
    public void Resolve_RejectsAMissingSystemSshExecutable()
    {
        Directory.CreateDirectory(Path.Combine(_windowsDirectory, "System32", "OpenSSH"));

        var error = Assert.Throws<FileNotFoundException>(
            () => WindowsOpenSshExecutableResolver.Resolve(_windowsDirectory));

        Assert.Contains("Windows OpenSSH Client is required", error.Message);
        Assert.Contains("Optional Features", error.Message);
        Assert.Contains(Path.Combine("System32", "OpenSSH", "ssh.exe"), error.Message);
    }

    public void Dispose()
    {
        if (Directory.Exists(_windowsDirectory))
        {
            Directory.Delete(_windowsDirectory, recursive: true);
        }
    }
}
