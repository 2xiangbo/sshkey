namespace SshKeySetupTool.Domain;

public static class SetupValidation
{
    public static IReadOnlyList<string> Validate(SetupRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Host)) errors.Add("Server IP address is required.");
        if (request.Port is < 1 or > 65535) errors.Add("SSH port must be between 1 and 65535.");
        if (string.IsNullOrWhiteSpace(request.Username)) errors.Add("SSH account name is required.");
        if (string.IsNullOrEmpty(request.Password)) errors.Add("Password is required.");
        if (string.IsNullOrWhiteSpace(request.PrivateKeyPath)) errors.Add("Private-key save path is required.");
        return errors;
    }
}
