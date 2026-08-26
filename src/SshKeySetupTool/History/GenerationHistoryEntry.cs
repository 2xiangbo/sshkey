namespace SshKeySetupTool.History;

public sealed record GenerationHistoryEntry(
    DateTimeOffset CompletedAtUtc,
    string ConnectionDetails,
    string? Host = null);
