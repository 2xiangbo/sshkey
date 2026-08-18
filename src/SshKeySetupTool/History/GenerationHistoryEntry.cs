namespace SshKeySetupTool.History;

public enum GenerationHistoryOutcome
{
    Succeeded,
    Failed,
    Cancelled
}

public sealed record GenerationHistoryEntry(
    DateTimeOffset CompletedAtUtc,
    string Host,
    int Port,
    string Username,
    string PrivateKeyPath,
    GenerationHistoryOutcome Outcome,
    string Message);
