namespace SshKeySetupTool.Domain;

public sealed record SetupRequest(string Host, int Port, string Username, string Password, string PrivateKeyPath);
public sealed record SetupProgress(string Message, bool IsError = false);
public sealed record SetupResult(bool Succeeded, string Message, string? PrivateKeyPath = null);
