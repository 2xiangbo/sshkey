namespace SshKeySetupTool.Ssh;

public sealed record WindowsOpenSshExecutables(string SshPath);

public static class WindowsOpenSshExecutableResolver
{
    public static WindowsOpenSshExecutables Resolve() =>
        Resolve(Environment.GetFolderPath(Environment.SpecialFolder.Windows));

    internal static WindowsOpenSshExecutables Resolve(string windowsDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(windowsDirectory);

        var openSshDirectory = Path.GetFullPath(
            Path.Combine(windowsDirectory, "System32", "OpenSSH"));
        var sshPath = ValidateExecutable(Path.Combine(openSshDirectory, "ssh.exe"));
        return new WindowsOpenSshExecutables(sshPath);
    }

    private static string ValidateExecutable(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                "Windows OpenSSH Client is required. Enable it in Windows Optional Features, " +
                $"then retry. Missing executable: {fullPath}",
                fullPath);
        }

        return fullPath;
    }
}
