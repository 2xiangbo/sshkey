using SshKeySetupTool.Domain;

namespace SshKeySetupTool;

public static class SetupFormInput
{
    public static string GetSuggestedPrivateKeyPath(string userProfilePath)
    {
        var sshDirectory = Path.Combine(userProfilePath, ".ssh");
        return GetSuggestedPrivateKeyPathInDirectory(sshDirectory);
    }

    public static string GetSuggestedPrivateKeyPathInDirectory(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        for (var suffix = 1; ; suffix++)
        {
            var fileName = suffix == 1 ? "id_ed25519_codex" : $"id_ed25519_codex_{suffix}";
            var privateKeyPath = Path.Combine(directory, fileName);
            if (!IsOccupied(privateKeyPath) && !IsOccupied(privateKeyPath + ".pub"))
            {
                return privateKeyPath;
            }
        }
    }

    public static SetupRequest BuildRequest(
        string host,
        string portText,
        string username,
        string password,
        string privateKeyPath)
    {
        _ = int.TryParse(portText, out var port);
        return new SetupRequest(host.Trim(), port, username.Trim(), password, privateKeyPath.Trim());
    }

    private static bool IsOccupied(string path) => File.Exists(path) || Directory.Exists(path);
}
