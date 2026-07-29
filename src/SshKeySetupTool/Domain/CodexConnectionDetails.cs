namespace SshKeySetupTool.Domain;

public static class CodexConnectionDetails
{
    public static string Format(SetupRequest request, string privateKeyPath)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(privateKeyPath);

        return string.Join(
            Environment.NewLine,
            $"\u670d\u52a1\u5668\u5730\u5740\uff1a{request.Host}",
            $"\u7aef\u53e3\uff1a{request.Port}",
            $"\u7528\u6237\u540d\uff1a{request.Username}",
            "\u8ba4\u8bc1\u65b9\u5f0f\uff1aSSH \u79c1\u94a5",
            $"\u79c1\u94a5\u8def\u5f84\uff1a{privateKeyPath}");
    }
}
