using System.Text;

namespace SshKeySetupTool.Ssh;

internal static class WindowsOpenSshPasswordFallback
{
    internal const string AskPassModeEnvironmentVariable = "CODEX_SSH_KEY_SETUP_ASKPASS_MODE";
    internal const string AskPassModeValue = "password";
    internal const string PasswordEnvironmentVariable = "CODEX_SSH_KEY_SETUP_PASSWORD";

    internal static bool TryGetPasswordResponse(
        string[] arguments,
        string? askPassMode,
        string? password,
        out string? response)
    {
        response = null;
        if (!string.Equals(askPassMode, AskPassModeValue, StringComparison.Ordinal)
            || arguments.Length != 1
            || password is null
            || !arguments[0].Contains("password", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        response = password;
        return true;
    }

    internal static void WriteAskPassPassword(Stream output, string password)
    {
        using var writer = new StreamWriter(output, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 1024, leaveOpen: true);
        writer.Write(password);
    }
}
