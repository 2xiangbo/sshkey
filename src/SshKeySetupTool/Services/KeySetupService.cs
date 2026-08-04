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
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception error)
        {
            return Failure(error, SetupFailureKind.ServerConfigurationInspection);
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
            var operationId = Guid.NewGuid().ToString("N");
            try
            {
                change = await _sshClient.EnablePublicKeyAuthenticationAsync(
                    request,
                    approvedHostKey,
                    operationId,
                    cancellationToken);
            }
            catch (OperationCanceledException cancellation)
            {
                var recoveryFailure = await TryRecoverApplyAsync(
                    request,
                    approvedHostKey,
                    operationId,
                    progress,
                    cancellation.Message);
                if (recoveryFailure is not null)
                {
                    return recoveryFailure;
                }

                throw;
            }
            catch (Exception error)
            {
                var recoveryFailure = await TryRecoverApplyAsync(
                    request,
                    approvedHostKey,
                    operationId,
                    progress,
                    error.Message);
                return recoveryFailure ?? Failure(
                    error,
                    SetupFailureKind.ServerConfigurationApply);
            }
        }

        var currentFailureKind = SetupFailureKind.PublicKeyInstallation;
        try
        {
            progress?.Report(new(SetupPhase.InstallingPublicKey));
            await _sshClient.InstallPublicKeyAsync(
                request,
                LinuxAuthorizedKeyCommand.Build(keyMaterial.PublicKeyLine),
                approvedHostKey,
                cancellationToken);
            currentFailureKind = SetupFailureKind.PrivateKeyVerification;
            progress?.Report(new(SetupPhase.VerifyingPrivateKey));
            await _sshClient.VerifyPrivateKeyAsync(
                request,
                keyMaterial.PrivateKeyPath,
                approvedHostKey,
                cancellationToken);

            if (change is not null)
            {
                currentFailureKind = SetupFailureKind.ServerConfigurationApply;
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
                return rollbackFailure;
            }

            throw;
        }
        catch (Exception error)
        {
            if (change is null)
            {
                return Failure(error, currentFailureKind);
            }

            var rollbackFailure = await TryRollbackAsync(
                request,
                approvedHostKey,
                change,
                progress,
                error.Message);
            return rollbackFailure ?? Failure(error, currentFailureKind);
        }
    }

    private async Task<SetupResult?> TryRecoverApplyAsync(
        SetupRequest request,
        OpenSshHostKey approvedHostKey,
        string operationId,
        IProgress<SetupProgress>? progress,
        string originalErrorMessage)
    {
        progress?.Report(new(SetupPhase.RollingBackServerConfiguration));
        using var rollbackTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        try
        {
            await _sshClient.RecoverServerConfigurationAsync(
                request,
                approvedHostKey,
                operationId,
                rollbackTimeout.Token);
            return null;
        }
        catch (Exception recoveryError)
        {
            return RollbackFailure(
                GetRemoteRecoveryPaths(operationId),
                originalErrorMessage,
                recoveryError.Message);
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
            return RollbackFailure(
                GetRemoteBackupPath(change),
                originalErrorMessage,
                rollbackError.Message);
        }
    }

    private static SetupResult Failure(
        Exception error,
        SetupFailureKind fallbackKind = SetupFailureKind.None) =>
        new(
            false,
            error.Message,
            FailureKind: error is SshSetupOperationException setupError
                ? setupError.FailureKind
                : fallbackKind);

    private static SetupResult RollbackFailure(
        string backupPaths,
        string originalErrorMessage,
        string rollbackErrorMessage) =>
        new(
            false,
            $"SSH configuration rollback failed; manual recovery may be required. " +
            $"Remote backup: {backupPaths}. Original error: {originalErrorMessage}. " +
            $"Rollback error: {rollbackErrorMessage}",
            FailureKind: SetupFailureKind.Rollback);

    private static string GetRemoteBackupPath(SshServerConfigurationChange change)
    {
        var basePath = change.Strategy == SshServerConfigurationStrategy.ManagedDropIn
            ? "/etc/ssh/sshd_config.d/00-sshkey-setup-tool.conf"
            : "/etc/ssh/sshd_config";
        return $"{basePath}.sshkey-setup-{change.OperationId}.bak";
    }

    private static string GetRemoteRecoveryPaths(string operationId) =>
        $"/etc/ssh/sshd_config.sshkey-setup-{operationId}.bak or " +
        $"/etc/ssh/sshd_config.d/00-sshkey-setup-tool.conf.sshkey-setup-{operationId}.bak";
}
