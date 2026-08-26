namespace SshKeySetupTool.Domain;

public static class CodexConnectionDetails
{
    public static string Format(SetupRequest request, string privateKeyPath)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(privateKeyPath);

        return $"ssh -p {request.Port} -i \"{privateKeyPath}\" {request.Username}@{request.Host}";
    }
}
