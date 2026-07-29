using SshKeySetupTool.Domain;
using SshKeySetupTool.Security;
using SshKeySetupTool.Ssh;

namespace SshKeySetupTool.Services;

public interface IKeySetupService
{
    Task<SetupResult> RunAsync(
        SetupRequest request,
        CancellationToken cancellationToken);
}

public sealed class KeySetupService : IKeySetupService
{
    private readonly IKeyMaterialFactory _keyMaterialFactory;
    private readonly ISshSetupClient _sshClient;

    public KeySetupService(IKeyMaterialFactory keyMaterialFactory, ISshSetupClient sshClient)
    {
        _keyMaterialFactory = keyMaterialFactory ?? throw new ArgumentNullException(nameof(keyMaterialFactory));
        _sshClient = sshClient ?? throw new ArgumentNullException(nameof(sshClient));
    }

    public async Task<SetupResult> RunAsync(
        SetupRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = SetupValidation.Validate(request);
        if (validationErrors.Count > 0)
        {
            return new SetupResult(false, string.Join(Environment.NewLine, validationErrors));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var keyMaterial = _keyMaterialFactory.Create(request.PrivateKeyPath);

        cancellationToken.ThrowIfCancellationRequested();
        var approvedHostKey = await _sshClient.ApproveHostKeyAsync(request, cancellationToken);
        await _sshClient.InstallPublicKeyAsync(
            request,
            LinuxAuthorizedKeyCommand.Build(keyMaterial.PublicKeyLine),
            approvedHostKey,
            cancellationToken);
        await _sshClient.VerifyPrivateKeyAsync(
            request,
            keyMaterial.PrivateKeyPath,
            approvedHostKey,
            cancellationToken);

        return new SetupResult(true, "Ready for Codex.", keyMaterial.PrivateKeyPath);
    }
}
