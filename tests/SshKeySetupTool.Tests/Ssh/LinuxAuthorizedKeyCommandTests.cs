using SshKeySetupTool.Ssh;
using System.Diagnostics;
using System.Text;
using Xunit.Sdk;

namespace SshKeySetupTool.Tests.Ssh;

public sealed class LinuxAuthorizedKeyCommandTests
{
    [Fact]
    public void Build_CreatesSecureAuthorizedKeysFileAndAppendsOnlyAMissingExactKey()
    {
        var command = LinuxAuthorizedKeyCommand.Build("ssh-ed25519 AAA test");

        Assert.Equal(
            "mkdir -p ~/.ssh && chmod 700 ~/.ssh && touch ~/.ssh/authorized_keys && chmod 600 ~/.ssh/authorized_keys && { grep -qxF -- 'ssh-ed25519 AAA test' ~/.ssh/authorized_keys || { { [ ! -s ~/.ssh/authorized_keys ] || [ \"$(tail -c 1 ~/.ssh/authorized_keys | wc -l)\" -eq 1 ] || printf '\\n' >> ~/.ssh/authorized_keys; } && printf '%s\\n' 'ssh-ed25519 AAA test' >> ~/.ssh/authorized_keys; }; } && grep -qxF -- 'ssh-ed25519 AAA test' ~/.ssh/authorized_keys",
            command);
    }

    [Fact]
    public void Build_ShellQuotesSingleQuotesInThePublicKeyLine()
    {
        var command = LinuxAuthorizedKeyCommand.Build("ssh-ed25519 AAA owner's-key");

        Assert.Contains("grep -qxF -- 'ssh-ed25519 AAA owner'\"'\"'s-key'", command);
        Assert.Contains("printf '%s\\n' 'ssh-ed25519 AAA owner'\"'\"'s-key'", command);
    }

    [Theory]
    [InlineData("ssh-ed25519 AAA comment\rmalicious-command")]
    [InlineData("ssh-ed25519 AAA comment\nmalicious-command")]
    public void Build_RejectsPublicKeyLineContainingCarriageReturnOrLineFeed(string publicKeyLine)
    {
        var error = Assert.Throws<ArgumentException>(() => LinuxAuthorizedKeyCommand.Build(publicKeyLine));

        Assert.Equal("publicKeyLine", error.ParamName);
        Assert.Contains("carriage return or line feed", error.Message);
    }

    [Fact]
    public async Task Build_IsNewlineSafeAndIdempotentAcrossAuthorizedKeysFileStates()
    {
        var dockerPath = FindDocker();
        if (dockerPath is null || !await HasShellImageAsync(dockerPath))
        {
            throw SkipException.ForSkip(
                "A local python:3.12-slim Docker image is required for the POSIX shell matrix.");
        }

        const string existingKey = "ssh-ed25519 OLD existing";
        const string newKey = "ssh-ed25519 NEW generated";
        var cases = new[]
        {
            new FileState("absent", null, newKey + "\n"),
            new FileState("empty", "", newKey + "\n"),
            new FileState(
                "newline-terminated",
                existingKey + "\n",
                existingKey + "\n" + newKey + "\n"),
            new FileState(
                "missing-final-newline",
                existingKey,
                existingKey + "\n" + newKey + "\n"),
            new FileState("already-present", newKey, newKey)
        };
        var command = LinuxAuthorizedKeyCommand.Build(newKey);

        foreach (var fileState in cases)
        {
            await VerifyFileStateAsync(dockerPath, command, fileState);
        }
    }

    private static async Task VerifyFileStateAsync(
        string dockerPath,
        string command,
        FileState fileState)
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"authorized-keys-{fileState.Name}-{Guid.NewGuid():N}");
        var sshDirectory = Path.Combine(directory, "home", ".ssh");
        var authorizedKeysPath = Path.Combine(sshDirectory, "authorized_keys");
        Directory.CreateDirectory(directory);
        try
        {
            if (fileState.InitialContent is not null)
            {
                Directory.CreateDirectory(sshDirectory);
                await File.WriteAllTextAsync(
                    authorizedKeysPath,
                    fileState.InitialContent,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }

            await RunInPosixShellAsync(dockerPath, directory, command);
            await RunInPosixShellAsync(dockerPath, directory, command);

            Assert.True(File.Exists(authorizedKeysPath));
            Assert.Equal(
                fileState.ExpectedContent,
                await File.ReadAllTextAsync(authorizedKeysPath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task RunInPosixShellAsync(
        string dockerPath,
        string directory,
        string command)
    {
        var startInfo = new ProcessStartInfo(dockerPath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--rm");
        startInfo.ArgumentList.Add("-e");
        startInfo.ArgumentList.Add("HOME=/work/home");
        startInfo.ArgumentList.Add("-v");
        startInfo.ArgumentList.Add($"{directory}:/work");
        startInfo.ArgumentList.Add("python:3.12-slim");
        startInfo.ArgumentList.Add("sh");
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add(command);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Docker could not be started.");
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        await Task.WhenAll(standardOutput, standardError);

        Assert.True(
            process.ExitCode == 0,
            $"POSIX command failed with exit code {process.ExitCode}: {standardError.Result}{standardOutput.Result}");
    }

    private static async Task<bool> HasShellImageAsync(string dockerPath)
    {
        var startInfo = new ProcessStartInfo(dockerPath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("image");
        startInfo.ArgumentList.Add("inspect");
        startInfo.ArgumentList.Add("python:3.12-slim");

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return false;
        }

        await process.WaitForExitAsync();
        return process.ExitCode == 0;
    }

    private static string? FindDocker()
    {
        var executableName = OperatingSystem.IsWindows() ? "docker.exe" : "docker";
        return (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(directory => Path.Combine(directory, executableName))
            .FirstOrDefault(File.Exists);
    }

    private sealed record FileState(
        string Name,
        string? InitialContent,
        string ExpectedContent);
}
