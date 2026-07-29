using SshKeySetupTool.Domain;

namespace SshKeySetupTool.Tests.Domain;

public sealed class CodexConnectionDetailsTests
{
    [Fact]
    public void Format_ReturnsReadyToPasteCodexSshDetails()
    {
        var request = new SetupRequest(
            "203.0.113.10",
            31122,
            "root",
            "password-is-not-in-the-output",
            @"C:\Users\lin\.ssh\id_ed25519_codex_5");

        var details = CodexConnectionDetails.Format(request, request.PrivateKeyPath);

        var expected = string.Join(
            Environment.NewLine,
            "\u670d\u52a1\u5668\u5730\u5740\uff1a203.0.113.10",
            "\u7aef\u53e3\uff1a31122",
            "\u7528\u6237\u540d\uff1aroot",
            "\u8ba4\u8bc1\u65b9\u5f0f\uff1aSSH \u79c1\u94a5",
            "\u79c1\u94a5\u8def\u5f84\uff1aC:\\Users\\lin\\.ssh\\id_ed25519_codex_5");

        Assert.Equal(expected, details);
        Assert.DoesNotContain(request.Password, details);
    }
}
