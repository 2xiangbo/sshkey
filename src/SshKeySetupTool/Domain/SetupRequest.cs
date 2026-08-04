namespace SshKeySetupTool.Domain;

public sealed record SetupRequest(string Host, int Port, string Username, string Password, string PrivateKeyPath);

public enum SetupPhase
{
    GeneratingKey,
    DiscoveringServer,
    CheckingServerConfiguration,
    WaitingForServerConfigurationConsent,
    EnablingServerConfiguration,
    InstallingPublicKey,
    VerifyingPrivateKey,
    RollingBackServerConfiguration
}

public enum SetupFailureKind
{
    None,
    Validation,
    ServerConfigurationInspection,
    ServerConfigurationRootRequired,
    ServerConfigurationDeclined,
    ServerConfigurationApply,
    PublicKeyInstallation,
    PrivateKeyVerification,
    Rollback
}

public sealed record SetupProgress(SetupPhase Phase);

public sealed record SetupResult(
    bool Succeeded,
    string Message,
    string? PrivateKeyPath = null,
    SetupFailureKind FailureKind = SetupFailureKind.None);
