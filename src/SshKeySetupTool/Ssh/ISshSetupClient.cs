using SshKeySetupTool.Domain;

namespace SshKeySetupTool.Ssh;

public interface ISshSetupClient
{
    Task<OpenSshHostKey> ApproveHostKeyAsync(
        SetupRequest request,
        CancellationToken cancellationToken);

    Task InstallPublicKeyAsync(
        SetupRequest request,
        string command,
        OpenSshHostKey approvedHostKey,
        CancellationToken cancellationToken);

    Task VerifyPrivateKeyAsync(
        SetupRequest request,
        string privateKeyPath,
        OpenSshHostKey approvedHostKey,
        CancellationToken cancellationToken);
}
