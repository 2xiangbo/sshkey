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

    Task<SshServerConfigurationProbe> InspectServerConfigurationAsync(
        SetupRequest request,
        OpenSshHostKey approvedHostKey,
        CancellationToken cancellationToken);

    Task<SshServerConfigurationChange> EnablePublicKeyAuthenticationAsync(
        SetupRequest request,
        OpenSshHostKey approvedHostKey,
        string operationId,
        CancellationToken cancellationToken);

    Task RecoverServerConfigurationAsync(
        SetupRequest request,
        OpenSshHostKey approvedHostKey,
        string operationId,
        CancellationToken cancellationToken);

    Task CommitServerConfigurationAsync(
        SetupRequest request,
        OpenSshHostKey approvedHostKey,
        SshServerConfigurationChange change,
        CancellationToken cancellationToken);

    Task RollbackServerConfigurationAsync(
        SetupRequest request,
        OpenSshHostKey approvedHostKey,
        SshServerConfigurationChange change,
        CancellationToken cancellationToken);

    Task VerifyPrivateKeyAsync(
        SetupRequest request,
        string privateKeyPath,
        OpenSshHostKey approvedHostKey,
        CancellationToken cancellationToken);
}
