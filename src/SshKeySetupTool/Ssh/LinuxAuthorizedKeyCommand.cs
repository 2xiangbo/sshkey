namespace SshKeySetupTool.Ssh;

public static class LinuxAuthorizedKeyCommand
{
    public static string Build(string publicKeyLine)
    {
        ArgumentNullException.ThrowIfNull(publicKeyLine);
        if (publicKeyLine.Contains('\r') || publicKeyLine.Contains('\n'))
        {
            throw new ArgumentException(
                "The public key line must not contain carriage return or line feed characters.",
                nameof(publicKeyLine));
        }

        var quotedKey = ShellQuote(publicKeyLine);
        return "mkdir -p ~/.ssh && chmod 700 ~/.ssh && " +
               "touch ~/.ssh/authorized_keys && chmod 600 ~/.ssh/authorized_keys && " +
               $"{{ grep -qxF -- {quotedKey} ~/.ssh/authorized_keys || {{ " +
               "{ [ ! -s ~/.ssh/authorized_keys ] || " +
               "[ \"$(tail -c 1 ~/.ssh/authorized_keys | wc -l)\" -eq 1 ] || " +
               "printf '\\n' >> ~/.ssh/authorized_keys; } && " +
               $"printf '%s\\n' {quotedKey} >> ~/.ssh/authorized_keys; }}; }} && " +
               $"grep -qxF -- {quotedKey} ~/.ssh/authorized_keys";
    }

    private static string ShellQuote(string value) =>
        "'" + value.Replace("'", "'\"'\"'", StringComparison.Ordinal) + "'";
}
