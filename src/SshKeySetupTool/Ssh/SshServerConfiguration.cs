using SshKeySetupTool.Domain;

namespace SshKeySetupTool.Ssh;

public enum SshPublicKeyAuthenticationState
{
    Enabled,
    Disabled,
    Unavailable
}

public sealed record SshServerConfigurationProbe(
    SshPublicKeyAuthenticationState State,
    string RawOutput)
{
    public static SshServerConfigurationProbe Parse(string output)
    {
        ArgumentNullException.ThrowIfNull(output);

        var matches = 0;
        var state = SshPublicKeyAuthenticationState.Unavailable;
        foreach (var rawLine in output.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var fields = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (!string.Equals(fields[0], "pubkeyauthentication", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (fields.Length != 2)
            {
                return new SshServerConfigurationProbe(
                    SshPublicKeyAuthenticationState.Unavailable,
                    output);
            }

            var lineState = fields[1] switch
            {
                _ when string.Equals(fields[1], "yes", StringComparison.OrdinalIgnoreCase) =>
                    SshPublicKeyAuthenticationState.Enabled,
                _ when string.Equals(fields[1], "no", StringComparison.OrdinalIgnoreCase) =>
                    SshPublicKeyAuthenticationState.Disabled,
                _ => SshPublicKeyAuthenticationState.Unavailable
            };
            if (lineState == SshPublicKeyAuthenticationState.Unavailable)
            {
                return new SshServerConfigurationProbe(lineState, output);
            }

            matches++;
            if (matches > 1)
            {
                return new SshServerConfigurationProbe(
                    SshPublicKeyAuthenticationState.Unavailable,
                    output);
            }

            state = lineState;
        }

        return new SshServerConfigurationProbe(
            matches == 1 ? state : SshPublicKeyAuthenticationState.Unavailable,
            output);
    }
}

public enum SshServerConfigurationStrategy
{
    ManagedDropIn,
    MainConfiguration
}

public sealed record SshServerConfigurationChange(
    string OperationId,
    SshServerConfigurationStrategy Strategy,
    bool HadExistingManagedDropIn);

public sealed class SshSetupOperationException : InvalidOperationException
{
    public SshSetupOperationException(
        SetupFailureKind failureKind,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        FailureKind = failureKind;
    }

    public SetupFailureKind FailureKind { get; }
}
