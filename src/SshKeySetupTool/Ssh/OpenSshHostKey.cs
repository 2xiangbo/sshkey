using System.Security.Cryptography;

namespace SshKeySetupTool.Ssh;

public sealed record OpenSshHostKey(
    string KeyType,
    string KeyData,
    string KnownHostsLine,
    string Sha256Fingerprint)
{
    private static readonly string[] PreferredKeyTypes =
    [
        "ssh-ed25519",
        "ecdsa-sha2-nistp521",
        "ecdsa-sha2-nistp384",
        "ecdsa-sha2-nistp256",
        "ssh-rsa"
    ];

    public static OpenSshHostKey ParseKnownHostsOutput(string output)
    {
        ArgumentNullException.ThrowIfNull(output);

        var candidates = output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(ParseLine)
            .Where(candidate => candidate is not null)
            .Cast<OpenSshHostKey>()
            .ToArray();

        foreach (var preferredKeyType in PreferredKeyTypes)
        {
            var candidate = candidates.FirstOrDefault(
                item => string.Equals(item.KeyType, preferredKeyType, StringComparison.Ordinal));
            if (candidate is not null)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            "Windows OpenSSH did not return a supported SSH server host key.");
    }

    private static OpenSshHostKey? ParseLine(string line)
    {
        var fields = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length < 3 || fields[0].StartsWith('#'))
        {
            return null;
        }

        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(fields[2]);
        }
        catch (FormatException)
        {
            return null;
        }

        var fingerprint = Convert.ToBase64String(SHA256.HashData(keyBytes)).TrimEnd('=');
        return new OpenSshHostKey(
            fields[1],
            fields[2],
            string.Join(' ', fields.Take(3)),
            $"SHA256:{fingerprint}");
    }
}
