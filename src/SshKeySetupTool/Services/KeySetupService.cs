using SshKeySetupTool.Domain;
using SshKeySetupTool.Security;
using SshKeySetupTool.Ssh;

namespace SshKeySetupTool.Services;

public interface IKeySetupService
{
    Task<SetupResult> RunAsync(
        SetupRequest request,
        Func<SetupRequest, SshServerConfigurationProbe, bool> confirmServerConfiguration,
        IProgress<SetupProgress>? progress,
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
        Func<SetupRequest, SshServerConfigurationProbe, bool> confirmServerConfiguration,
        IProgress<SetupProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(confirmServerConfiguration);

        var validationErrors = SetupValidation.Validate(request);
        if (validationErrors.Count > 0)
        {
            return new SetupResult(
                false,
                string.Join(Environment.NewLine, validationErrors),
                FailureKind: SetupFailureKind.Validation);
        }

        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new(SetupPhase.GeneratingKey));
        var keyMaterial = _keyMaterialFactory.Create(request.PrivateKeyPath);

        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new(SetupPhase.DiscoveringServer));
        var approvedHostKey = await _sshClient.ApproveHostKeyAsync(request, cancellationToken);
        progress?.Report(new(SetupPhase.CheckingServerConfiguration));

        SshServerConfigurationProbe probe;
        try
        {
            probe = await _sshClient.InspectServerConfigurationAsync(
                request,
                approvedHostKey,
                cancellationToken);
        }
        catch (SshSetupOperationException error)
        {
            return Failure(error);
        }

        SshServerConfigurationChange? change = null;
        if (probe.State == SshPublicKeyAuthenticationState.Unavailable)
        {
            return new SetupResult(
                false,
                "Server SSH configuration inspection failed.",
                FailureKind: SetupFailureKind.ServerConfigurationInspection);
        }

        if (probe.State == SshPublicKeyAuthenticationState.Disabled)
        {
            if (!string.Equals(request.Username, "root", StringComparison.Ordinal))
            {
                return new SetupResult(
                    false,
                    "Automatic repair requires the root account.",
                    FailureKind: SetupFailureKind.ServerConfigurationRootRequired);
            }

            progress?.Report(new(SetupPhase.WaitingForServerConfigurationConsent));
            if (!confirmServerConfiguration(request, probe))
            {
                return new SetupResult(
                    false,
                    "Server SSH repair was declined.",
                    FailureKind: SetupFailureKind.ServerConfigurationDeclined);
            }

            progress?.Report(new(SetupPhase.EnablingServerConfiguration));
            try
            {
                change = await _sshClient.EnablePublicKeyAuthenticationAsync(
                    request,
                    approvedHostKey,
                    cancellationToken);
            }
            catch (SshSetupOperationException error)
            {
                return Failure(error);
            }
        }

        try
        {
            progress?.Report(new(SetupPhase.InstallingPublicKey));
            await _sshClient.InstallPublicKeyAsync(
                request,
                LinuxAuthorizedKeyCommand.Build(keyMaterial.PublicKeyLine),
                approvedHostKey,
                cancellationToken);
            progress?.Report(new(SetupPhase.VerifyingPrivateKey));
            await _sshClient.VerifyPrivateKeyAsync(
                request,
                keyMaterial.PrivateKeyPath,
                approvedHostKey,
                cancellationToken);

            if (change is not null)
            {
                await _sshClient.CommitServerConfigurationAsync(
                    request,
                    approvedHostKey,
                    change,
                    cancellationToken);
            }

            return new SetupResult(true, "Ready for Codex.", keyMaterial.PrivateKeyPath);
        }
        catch (OperationCanceledException cancellation)
        {
            if (change is null)
            {
                throw;
            }

            var rollbackFailure = await TryRollbackAsync(
                request,
                approvedHostKey,
                change,
                progress,
                cancellation.Message);
            if (rollbackFailure is not null)
            {
                return rollbackFailure with
                {
                    Message = $"{rollbackFailure.Message} Original error: {cancellation.Message}"
                };
            }

            throw;
        }
        catch (SshSetupOperationException error)
        {
            if (change is null)
            {
                return Failure(error);
            }

            var rollbackFailure = await TryRollbackAsync(
                request,
                approvedHostKey,
                change,
                progress,
                error.Message);
            return rollbackFailure ?? Failure(error);
        }
    }

    private async Task<SetupResult?> TryRollbackAsync(
        SetupRequest request,
        OpenSshHostKey approvedHostKey,
        SshServerConfigurationChange change,
        IProgress<SetupProgress>? progress,
        string originalErrorMessage)
    {
        progress?.Report(new(SetupPhase.RollingBackServerConfiguration));
        using var rollbackTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        try
        {
            await _sshClient.RollbackServerConfigurationAsync(
                request,
                approvedHostKey,
                change,
                rollbackTimeout.Token);
            return null;
        }
        catch (Exception rollbackError)
        {
            var backupPath = GetRemoteBackupPath(change);
            return new SetupResult(
                false,
                $"SSH configuration rollback failed; manual recovery may be required. " +
                $"Remote backup: {backupPath}. Original error: {originalErrorMessage}. " +
                $"Rollback error: {rollbackError.Message}",
                FailureKind: SetupFailureKind.Rollback);
        }
    }

    private static SetupResult Failure(SshSetupOperationException error) =>
        new(false, error.Message, FailureKind: error.FailureKind);

    private static string GetRemoteBackupPath(SshServerConfigurationChange change)
    {
        var basePath = change.Strategy == SshServerConfigurationStrategy.ManagedDropIn
            ? "/etc/ssh/sshd_config.d/00-sshkey-setup-tool.conf"
            : "/etc/ssh/sshd_config";
        return $"{basePath}.sshkey-setup-{change.OperationId}.bak";
    }
}
