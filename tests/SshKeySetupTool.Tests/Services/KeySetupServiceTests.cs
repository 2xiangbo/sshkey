using SshKeySetupTool.Domain;
using SshKeySetupTool.Security;
using SshKeySetupTool.Services;
using SshKeySetupTool.Ssh;

namespace SshKeySetupTool.Tests.Services;

public sealed class KeySetupServiceTests
{
    [Fact]
    public async Task RunAsync_DisabledRootWithConsent_RepairsBeforeInstallAndCommits()
    {
        var sshClient = new FakeSshSetupClient
        {
            Probe = new(
                SshPublicKeyAuthenticationState.Disabled,
                "pubkeyauthentication no\n")
        };
        var phases = new List<SetupPhase>();
        var progress = new InlineProgress<SetupProgress>(
            value => phases.Add(value.Phase));
        var service = new KeySetupService(new FakeKeyMaterialFactory(), sshClient);
        var request = CreateRequest("root");

        var result = await service.RunAsync(
            request,
            (_, probe) => probe.State == SshPublicKeyAuthenticationState.Disabled,
            progress,
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(
            ["host", "inspect", "apply", "install", "verify", "commit"],
            sshClient.Calls);
        Assert.Contains(SetupPhase.WaitingForServerConfigurationConsent, phases);
        Assert.Contains(SetupPhase.EnablingServerConfiguration, phases);
    }

    [Fact]
    public async Task RunAsync_EnabledProbeSkipsConsentAndMutation()
    {
        var sshClient = new FakeSshSetupClient();
        var consentCalls = 0;
        var service = new KeySetupService(new FakeKeyMaterialFactory(), sshClient);

        var result = await service.RunAsync(
            CreateRequest("deploy"),
            (_, _) =>
            {
                consentCalls++;
                return true;
            },
            null,
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(["host", "inspect", "install", "verify"], sshClient.Calls);
        Assert.Equal(0, consentCalls);
    }

    [Fact]
    public async Task RunAsync_DisabledRootDeclined_ReturnsDeclinedWithoutMutation()
    {
        var sshClient = new FakeSshSetupClient
        {
            Probe = new(SshPublicKeyAuthenticationState.Disabled, "pubkeyauthentication no\n")
        };
        var service = new KeySetupService(new FakeKeyMaterialFactory(), sshClient);

        var result = await service.RunAsync(
            CreateRequest("root"),
            (_, _) => false,
            null,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(SetupFailureKind.ServerConfigurationDeclined, result.FailureKind);
        Assert.Equal(["host", "inspect"], sshClient.Calls);
    }

    [Fact]
    public async Task RunAsync_DisabledNonRoot_ReturnsRootRequiredWithoutConsent()
    {
        var sshClient = new FakeSshSetupClient
        {
            Probe = new(SshPublicKeyAuthenticationState.Disabled, "pubkeyauthentication no\n")
        };
        var consentCalls = 0;
        var service = new KeySetupService(new FakeKeyMaterialFactory(), sshClient);

        var result = await service.RunAsync(
            CreateRequest("Root"),
            (_, _) =>
            {
                consentCalls++;
                return true;
            },
            null,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(SetupFailureKind.ServerConfigurationRootRequired, result.FailureKind);
        Assert.Equal(["host", "inspect"], sshClient.Calls);
        Assert.Equal(0, consentCalls);
    }

    [Fact]
    public async Task RunAsync_UnavailableProbe_ReturnsInspectionFailureWithoutConsent()
    {
        var sshClient = new FakeSshSetupClient
        {
            Probe = new(SshPublicKeyAuthenticationState.Unavailable, "malformed\n")
        };
        var consentCalls = 0;
        var service = new KeySetupService(new FakeKeyMaterialFactory(), sshClient);

        var result = await service.RunAsync(
            CreateRequest("root"),
            (_, _) =>
            {
                consentCalls++;
                return true;
            },
            null,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(SetupFailureKind.ServerConfigurationInspection, result.FailureKind);
        Assert.Equal(["host", "inspect"], sshClient.Calls);
        Assert.Equal(0, consentCalls);
    }

    [Fact]
    public async Task RunAsync_VerificationFailure_RollsBackAndDoesNotCommit()
    {
        var sshClient = new FakeSshSetupClient
        {
            Probe = new(SshPublicKeyAuthenticationState.Disabled, "pubkeyauthentication no\n"),
            VerifyException = new SshSetupOperationException(
                SetupFailureKind.PrivateKeyVerification,
                "private key failed")
        };
        var service = new KeySetupService(new FakeKeyMaterialFactory(), sshClient);

        var result = await service.RunAsync(
            CreateRequest("root"),
            (_, _) => true,
            null,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(SetupFailureKind.PrivateKeyVerification, result.FailureKind);
        Assert.Equal(
            ["host", "inspect", "apply", "install", "verify", "rollback"],
            sshClient.Calls);
    }

    [Fact]
    public async Task RunAsync_CancellationAfterApply_RollsBackWithLiveTokenAndRethrows()
    {
        using var cancellation = new CancellationTokenSource();
        var sshClient = new FakeSshSetupClient
        {
            Probe = new(SshPublicKeyAuthenticationState.Disabled, "pubkeyauthentication no\n"),
            BeforeVerify = cancellation.Cancel,
            VerifyException = new OperationCanceledException("cancelled")
        };
        var service = new KeySetupService(new FakeKeyMaterialFactory(), sshClient);

        await Assert.ThrowsAsync<OperationCanceledException>(() => service.RunAsync(
            CreateRequest("root"),
            (_, _) => true,
            null,
            cancellation.Token));

        Assert.Equal(
            ["host", "inspect", "apply", "install", "verify", "rollback"],
            sshClient.Calls);
        Assert.NotNull(sshClient.RollbackToken);
        Assert.False(sshClient.RollbackToken!.Value.IsCancellationRequested);
    }

    [Fact]
    public async Task RunAsync_RollbackFailure_ReturnsRollbackKindAndBothErrors()
    {
        var sshClient = new FakeSshSetupClient
        {
            Probe = new(SshPublicKeyAuthenticationState.Disabled, "pubkeyauthentication no\n"),
            VerifyException = new SshSetupOperationException(
                SetupFailureKind.PrivateKeyVerification,
                "private key failed"),
            RollbackException = new SshSetupOperationException(
                SetupFailureKind.Rollback,
                "rollback failed")
        };
        var service = new KeySetupService(new FakeKeyMaterialFactory(), sshClient);

        var result = await service.RunAsync(
            CreateRequest("root"),
            (_, _) => true,
            null,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(SetupFailureKind.Rollback, result.FailureKind);
        Assert.Contains("/etc/ssh/sshd_config.d/00-sshkey-setup-tool.conf", result.Message);
        Assert.Contains("private key failed", result.Message);
        Assert.Contains("rollback failed", result.Message);
    }

    [Fact]
    public async Task RunAsync_ValidationFailureDoesNotCreateKeysOrConnect()
    {
        var keyFactory = new FakeKeyMaterialFactory();
        var sshClient = new FakeSshSetupClient();
        var service = new KeySetupService(keyFactory, sshClient);

        var result = await service.RunAsync(
            new SetupRequest("", 22, "root", "pw", @"C:\keys\id_ed25519"),
            (_, _) => true,
            null,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(SetupFailureKind.Validation, result.FailureKind);
        Assert.Contains("Server IP address is required.", result.Message);
        Assert.False(keyFactory.WasCalled);
        Assert.Empty(sshClient.Calls);
    }

    private static SetupRequest CreateRequest(string username) =>
        new("203.0.113.10", 22, username, "pw", @"C:\keys\id_ed25519");

    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }

    private sealed class FakeKeyMaterialFactory : IKeyMaterialFactory
    {
        internal const string PublicKeyLine =
            "ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIFakePublicKey ssh-key-setup-tool";

        public bool WasCalled { get; private set; }

        public KeyMaterial Create(string privateKeyPath)
        {
            WasCalled = true;
            return new KeyMaterial(privateKeyPath, privateKeyPath + ".pub", PublicKeyLine);
        }
    }

    private sealed class FakeSshSetupClient : ISshSetupClient
    {
        public List<string> Calls { get; } = [];
        public SshServerConfigurationProbe Probe { get; init; } = new(
            SshPublicKeyAuthenticationState.Enabled,
            "pubkeyauthentication yes\n");
        public Exception? VerifyException { get; init; }
        public Exception? RollbackException { get; init; }
        public Action? BeforeVerify { get; init; }
        public CancellationToken? RollbackToken { get; private set; }

        public Task<OpenSshHostKey> ApproveHostKeyAsync(
            SetupRequest request,
            CancellationToken cancellationToken)
        {
            Calls.Add("host");
            return Task.FromResult(OpenSshHostKey.ParseKnownHostsOutput(
                "203.0.113.10 ssh-ed25519 ZmFrZS1ob3N0LWtleQ==\n"));
        }

        public Task<SshServerConfigurationProbe> InspectServerConfigurationAsync(
            SetupRequest request,
            OpenSshHostKey approvedHostKey,
            CancellationToken cancellationToken)
        {
            Calls.Add("inspect");
            return Task.FromResult(Probe);
        }

        public Task<SshServerConfigurationChange> EnablePublicKeyAuthenticationAsync(
            SetupRequest request,
            OpenSshHostKey approvedHostKey,
            CancellationToken cancellationToken)
        {
            Calls.Add("apply");
            return Task.FromResult(new SshServerConfigurationChange(
                "0123456789abcdef0123456789abcdef",
                SshServerConfigurationStrategy.ManagedDropIn,
                false));
        }

        public Task InstallPublicKeyAsync(
            SetupRequest request,
            string command,
            OpenSshHostKey approvedHostKey,
            CancellationToken cancellationToken)
        {
            Calls.Add("install");
            return Task.CompletedTask;
        }

        public Task VerifyPrivateKeyAsync(
            SetupRequest request,
            string privateKeyPath,
            OpenSshHostKey approvedHostKey,
            CancellationToken cancellationToken)
        {
            Calls.Add("verify");
            BeforeVerify?.Invoke();
            return VerifyException is null
                ? Task.CompletedTask
                : Task.FromException(VerifyException);
        }

        public Task CommitServerConfigurationAsync(
            SetupRequest request,
            OpenSshHostKey approvedHostKey,
            SshServerConfigurationChange change,
            CancellationToken cancellationToken)
        {
            Calls.Add("commit");
            return Task.CompletedTask;
        }

        public Task RollbackServerConfigurationAsync(
            SetupRequest request,
            OpenSshHostKey approvedHostKey,
            SshServerConfigurationChange change,
            CancellationToken cancellationToken)
        {
            Calls.Add("rollback");
            RollbackToken = cancellationToken;
            return RollbackException is null
                ? Task.CompletedTask
                : Task.FromException(RollbackException);
        }
    }
}
