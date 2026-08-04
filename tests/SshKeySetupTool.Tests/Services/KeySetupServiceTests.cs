using SshKeySetupTool.Domain;
using SshKeySetupTool.Security;
using SshKeySetupTool.Services;
using SshKeySetupTool.Ssh;

namespace SshKeySetupTool.Tests.Services;

public sealed class KeySetupServiceTests
{
    [Fact]
    public async Task RunAsync_CreatesKeysInstallsPublicKeyThenVerifiesPrivateKey()
    {
        var sshClient = new FakeSshSetupClient();
        var service = new KeySetupService(new FakeKeyMaterialFactory(), sshClient);
        var request = new SetupRequest("203.0.113.10", 22, "root", "pw", @"C:\keys\id_ed25519");

        var result = await service.RunAsync(request, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(@"C:\keys\id_ed25519", result.PrivateKeyPath);
        Assert.Equal(new[] { "host", "password", "private-key" }, sshClient.Calls);
        Assert.Equal(LinuxAuthorizedKeyCommand.Build(FakeKeyMaterialFactory.PublicKeyLine), sshClient.Command);
    }

    [Fact]
    public async Task RunAsync_ReturnsValidationErrorsWithoutCreatingKeysOrConnecting()
    {
        var keyFactory = new FakeKeyMaterialFactory();
        var sshClient = new FakeSshSetupClient();
        var service = new KeySetupService(keyFactory, sshClient);

        var result = await service.RunAsync(new SetupRequest("", 22, "root", "pw", @"C:\keys\id_ed25519"), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("Server IP address is required.", result.Message);
        Assert.False(keyFactory.WasCalled);
        Assert.Empty(sshClient.Calls);
    }

    private sealed class FakeKeyMaterialFactory : IKeyMaterialFactory
    {
        internal const string PublicKeyLine = "ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIFakePublicKey ssh-key-setup-tool";

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
        public string Command { get; private set; } = string.Empty;

        public Task<OpenSshHostKey> ApproveHostKeyAsync(
            SetupRequest request,
            CancellationToken cancellationToken)
        {
            Calls.Add("host");
            return Task.FromResult(OpenSshHostKey.ParseKnownHostsOutput(
                "203.0.113.10 ssh-ed25519 ZmFrZS1ob3N0LWtleQ==\n"));
        }

        public Task InstallPublicKeyAsync(
            SetupRequest request,
            string command,
            OpenSshHostKey approvedHostKey,
            CancellationToken cancellationToken)
        {
            Calls.Add("password");
            Command = command;
            return Task.CompletedTask;
        }

        public Task<SshServerConfigurationProbe> InspectServerConfigurationAsync(
            SetupRequest request,
            OpenSshHostKey approvedHostKey,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new SshServerConfigurationProbe(
                SshPublicKeyAuthenticationState.Enabled,
                "pubkeyauthentication yes\n"));
        }

        public Task<SshServerConfigurationChange> EnablePublicKeyAuthenticationAsync(
            SetupRequest request,
            OpenSshHostKey approvedHostKey,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new SshServerConfigurationChange(
                "0123456789abcdef0123456789abcdef",
                SshServerConfigurationStrategy.ManagedDropIn,
                false));
        }

        public Task CommitServerConfigurationAsync(
            SetupRequest request,
            OpenSshHostKey approvedHostKey,
            SshServerConfigurationChange change,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task RollbackServerConfigurationAsync(
            SetupRequest request,
            OpenSshHostKey approvedHostKey,
            SshServerConfigurationChange change,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task VerifyPrivateKeyAsync(
            SetupRequest request,
            string privateKeyPath,
            OpenSshHostKey approvedHostKey,
            CancellationToken cancellationToken)
        {
            Calls.Add("private-key");
            return Task.CompletedTask;
        }
    }
}
