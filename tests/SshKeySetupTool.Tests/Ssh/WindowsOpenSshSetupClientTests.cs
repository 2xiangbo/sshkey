using SshKeySetupTool.Domain;
using SshKeySetupTool.Ssh;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace SshKeySetupTool.Tests.Ssh;

public sealed class WindowsOpenSshSetupClientTests
{
    [Fact]
    public async Task ApproveHostKeyAsync_DefersExecutableResolutionUntilSetupBegins()
    {
        var expectedError = new FileNotFoundException(
            "Windows OpenSSH Client is required. Enable it in Windows Optional Features, then retry.");
        var resolutionCalls = 0;
        var client = new WindowsOpenSshSetupClient(
            (_, _) => true,
            () =>
            {
                resolutionCalls++;
                throw expectedError;
            },
            new RecordingProcessRunner(
                _ => throw new Xunit.Sdk.XunitException(
                    "A process must not start when executable resolution fails.")),
            @"C:\tool\SshKeySetupTool.exe");
        var request = new SetupRequest(
            "203.0.113.10",
            22,
            "root",
            "secret",
            @"C:\keys\id_ed25519");

        Assert.Equal(0, resolutionCalls);

        var actualError = await Assert.ThrowsAsync<FileNotFoundException>(
            () => client.ApproveHostKeyAsync(request, CancellationToken.None));

        Assert.Same(expectedError, actualError);
        Assert.Equal(1, resolutionCalls);
    }

    [Fact]
    public async Task ApproveHostKeyAsync_UsesCredentialFreeSshProbeInsteadOfKeyScan()
    {
        var hostKeyBytes = Encoding.ASCII.GetBytes("fake-ed25519-host-key-blob");
        var knownHostsLine =
            $"[203.0.113.10]:31122 ssh-ed25519 {Convert.ToBase64String(hostKeyBytes)}";
        string? temporaryKnownHostsPath = null;
        var runner = new RecordingProcessRunner(startInfo =>
        {
            Assert.EndsWith("ssh.exe", startInfo.FileName, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(
                WindowsOpenSshPasswordFallback.PasswordEnvironmentVariable,
                startInfo.Environment.Keys);
            Assert.Contains("StrictHostKeyChecking=accept-new", startInfo.ArgumentList);
            Assert.Contains("BatchMode=yes", startInfo.ArgumentList);
            Assert.Contains("NumberOfPasswordPrompts=0", startInfo.ArgumentList);
            Assert.Contains("PasswordAuthentication=no", startInfo.ArgumentList);
            Assert.Contains("KbdInteractiveAuthentication=no", startInfo.ArgumentList);
            Assert.Contains("PubkeyAuthentication=no", startInfo.ArgumentList);
            var knownHostsOption = Assert.Single(
                startInfo.ArgumentList,
                argument => argument.StartsWith("UserKnownHostsFile=", StringComparison.Ordinal));
            temporaryKnownHostsPath = knownHostsOption["UserKnownHostsFile=".Length..];
            File.WriteAllText(temporaryKnownHostsPath, knownHostsLine + "\n");
            return new ProcessResult(255, "", "Permission denied (publickey,password).");
        });
        var client = new WindowsOpenSshSetupClient(
            (_, _) => true,
            new WindowsOpenSshExecutables(
                @"C:\Windows\System32\OpenSSH\ssh.exe"),
            runner,
            @"C:\tool\SshKeySetupTool.exe");
        var request = new SetupRequest(
            "203.0.113.10",
            31122,
            "root",
            "secret",
            @"C:\keys\id_ed25519");

        var hostKey = await client.ApproveHostKeyAsync(request, CancellationToken.None);

        Assert.Equal("ssh-ed25519", hostKey.KeyType);
        Assert.Single(runner.StartInfos);
        Assert.NotNull(temporaryKnownHostsPath);
        Assert.False(File.Exists(temporaryKnownHostsPath));
    }

    [Fact]
    public async Task Workflow_ConfirmsSha256BeforePasswordAndPinsTheApprovedHostKey()
    {
        var hostKeyBytes = Encoding.ASCII.GetBytes("fake-ed25519-host-key-blob");
        var fingerprint = "SHA256:" + Convert.ToBase64String(SHA256.HashData(hostKeyBytes)).TrimEnd('=');
        var request = new SetupRequest("203.0.113.10", 22, "root", "secret", @"C:\keys\id_ed25519");
        var calls = new List<string>();
        var runner = new RecordingProcessRunner(startInfo =>
        {
            if (startInfo.ArgumentList.Contains("StrictHostKeyChecking=accept-new"))
            {
                calls.Add("discover");
                Assert.False(startInfo.Environment.ContainsKey(
                    WindowsOpenSshPasswordFallback.PasswordEnvironmentVariable));
                Assert.Equal(
                    ["--", "root@203.0.113.10", "true"],
                    startInfo.ArgumentList.TakeLast(3));
                return WriteDiscoveredHostKey(
                    startInfo,
                    $"203.0.113.10 ssh-ed25519 {Convert.ToBase64String(hostKeyBytes)}");
            }

            calls.Add(startInfo.Environment.ContainsKey(
                WindowsOpenSshPasswordFallback.PasswordEnvironmentVariable)
                ? "password"
                : "private-key");
            AssertPinnedToApprovedKey(startInfo, hostKeyBytes);
            return new ProcessResult(0, "", "");
        });
        var client = new WindowsOpenSshSetupClient(
            (_, actualFingerprint) =>
            {
                calls.Add("confirm");
                Assert.Equal(fingerprint, actualFingerprint);
                return true;
            },
            new WindowsOpenSshExecutables(
                @"C:\Windows\System32\OpenSSH\ssh.exe"),
            runner,
            @"C:\tool\SshKeySetupTool.exe");

        var approvedHostKey = await client.ApproveHostKeyAsync(request, CancellationToken.None);
        await client.InstallPublicKeyAsync(request, "echo installed", approvedHostKey, CancellationToken.None);
        await client.VerifyPrivateKeyAsync(request, request.PrivateKeyPath, approvedHostKey, CancellationToken.None);

        Assert.Equal(["discover", "confirm", "password", "private-key"], calls);
        var discoveryStartInfo = runner.StartInfos[0];
        var connectTimeoutOption = Assert.Single(
            discoveryStartInfo.ArgumentList,
            argument => argument.StartsWith("ConnectTimeout=", StringComparison.Ordinal));
        var connectTimeout = TimeSpan.FromSeconds(
            int.Parse(connectTimeoutOption["ConnectTimeout=".Length..]));
        Assert.True(
            runner.Timeouts[0] >= connectTimeout + TimeSpan.FromSeconds(5),
            "The process timeout must leave the SSH discovery probe enough time to exit.");
        var passwordStartInfo = runner.StartInfos[1];
        Assert.Equal("secret", passwordStartInfo.Environment[
            WindowsOpenSshPasswordFallback.PasswordEnvironmentVariable]);
        Assert.Contains("echo installed", passwordStartInfo.ArgumentList);
        Assert.Contains("BatchMode=no", passwordStartInfo.ArgumentList);
        var keyStartInfo = runner.StartInfos[2];
        Assert.False(keyStartInfo.Environment.ContainsKey(
            WindowsOpenSshPasswordFallback.PasswordEnvironmentVariable));
        Assert.Contains(@"C:\keys\id_ed25519", keyStartInfo.ArgumentList);
        Assert.Contains("BatchMode=yes", keyStartInfo.ArgumentList);
    }

    [Fact]
    public async Task ApproveHostKeyAsync_DeclineStopsBeforePasswordOrMutation()
    {
        var runner = new RecordingProcessRunner(startInfo => WriteDiscoveredHostKey(
            startInfo,
            "203.0.113.10 ssh-ed25519 ZmFrZS1ob3N0LWtleQ=="));
        var request = new SetupRequest("203.0.113.10", 22, "root", "secret", @"C:\keys\id_ed25519");
        var client = new WindowsOpenSshSetupClient(
            (_, _) => false,
            new WindowsOpenSshExecutables(
                @"C:\Windows\System32\OpenSSH\ssh.exe"),
            runner,
            @"C:\tool\SshKeySetupTool.exe");

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => client.ApproveHostKeyAsync(request, CancellationToken.None));

        var discovery = Assert.Single(runner.StartInfos);
        Assert.EndsWith("ssh.exe", discovery.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            WindowsOpenSshPasswordFallback.PasswordEnvironmentVariable,
            discovery.Environment.Keys);
    }

    [Fact]
    public async Task ApproveHostKeyAsync_CancellationAfterScanDoesNotPrompt()
    {
        using var cancellation = new CancellationTokenSource();
        var runner = new RecordingProcessRunner(startInfo =>
        {
            var result = WriteDiscoveredHostKey(
                startInfo,
                "203.0.113.10 ssh-ed25519 ZmFrZS1ob3N0LWtleQ==");
            cancellation.Cancel();
            return result;
        });
        var client = new WindowsOpenSshSetupClient(
            (_, _) => throw new Xunit.Sdk.XunitException(
                "Confirmation must not start after cancellation."),
            new WindowsOpenSshExecutables(
                @"C:\Windows\System32\OpenSSH\ssh.exe"),
            runner,
            @"C:\tool\SshKeySetupTool.exe");
        var request = new SetupRequest(
            "203.0.113.10",
            22,
            "root",
            "secret",
            @"C:\keys\id_ed25519");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.ApproveHostKeyAsync(request, cancellation.Token));
    }

    [Fact]
    public async Task InstallPublicKeyAsync_RedactsPasswordFromOpenSshFailure()
    {
        var runner = new RecordingProcessRunner(_ => new ProcessResult(255, "", "secret was rejected"));
        var client = new WindowsOpenSshSetupClient(
            (_, _) => true,
            new WindowsOpenSshExecutables(
                @"C:\Windows\System32\OpenSSH\ssh.exe"),
            runner,
            @"C:\tool\SshKeySetupTool.exe");
        var request = new SetupRequest("203.0.113.10", 22, "root", "secret", @"C:\keys\id_ed25519");
        var approvedHostKey = OpenSshHostKey.ParseKnownHostsOutput(
            "203.0.113.10 ssh-ed25519 ZmFrZS1ob3N0LWtleQ==\n");

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.InstallPublicKeyAsync(
                request,
                "echo installed",
                approvedHostKey,
                CancellationToken.None));

        Assert.DoesNotContain("secret", error.Message, StringComparison.Ordinal);
        Assert.Contains("[redacted]", error.Message, StringComparison.Ordinal);
    }

    private static void AssertPinnedToApprovedKey(ProcessStartInfo startInfo, byte[] hostKeyBytes)
    {
        Assert.Equal(@"C:\Windows\System32\OpenSSH\ssh.exe", startInfo.FileName);
        var configFileOption = startInfo.ArgumentList.IndexOf("-F");
        Assert.True(configFileOption >= 0);
        Assert.Equal("none", startInfo.ArgumentList[configFileOption + 1]);
        Assert.Contains("StrictHostKeyChecking=yes", startInfo.ArgumentList);
        Assert.Contains("GlobalKnownHostsFile=NUL", startInfo.ArgumentList);
        Assert.Contains("HostKeyAlgorithms=ssh-ed25519", startInfo.ArgumentList);
        var optionBoundary = startInfo.ArgumentList.IndexOf("--");
        Assert.True(optionBoundary >= 0);
        Assert.Equal("root@203.0.113.10", startInfo.ArgumentList[optionBoundary + 1]);
        var knownHostsOption = Assert.Single(
            startInfo.ArgumentList,
            argument => argument.StartsWith("UserKnownHostsFile=", StringComparison.Ordinal));
        var knownHostsPath = knownHostsOption["UserKnownHostsFile=".Length..];
        var knownHostsLine = File.ReadAllText(knownHostsPath);
        Assert.Contains("ssh-ed25519", knownHostsLine);
        Assert.Contains(Convert.ToBase64String(hostKeyBytes), knownHostsLine);
    }

    private static ProcessResult WriteDiscoveredHostKey(
        ProcessStartInfo startInfo,
        string knownHostsLine)
    {
        var knownHostsOption = Assert.Single(
            startInfo.ArgumentList,
            argument => argument.StartsWith("UserKnownHostsFile=", StringComparison.Ordinal));
        var knownHostsPath = knownHostsOption["UserKnownHostsFile=".Length..];
        File.WriteAllText(knownHostsPath, knownHostsLine + "\n");
        return new ProcessResult(255, "", "Permission denied (publickey,password).");
    }

    private sealed class RecordingProcessRunner : IProcessRunner
    {
        private readonly Func<ProcessStartInfo, ProcessResult> _resultFactory;

        public RecordingProcessRunner(Func<ProcessStartInfo, ProcessResult> resultFactory)
        {
            _resultFactory = resultFactory;
        }

        public List<ProcessStartInfo> StartInfos { get; } = [];
        public List<TimeSpan> Timeouts { get; } = [];

        public Task<ProcessResult> RunAsync(
            ProcessStartInfo startInfo,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            Assert.True(timeout > TimeSpan.Zero);
            Assert.NotEqual(Timeout.InfiniteTimeSpan, timeout);
            StartInfos.Add(startInfo);
            Timeouts.Add(timeout);
            return Task.FromResult(_resultFactory(startInfo));
        }
    }
}
