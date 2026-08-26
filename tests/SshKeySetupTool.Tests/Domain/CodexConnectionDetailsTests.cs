using SshKeySetupTool.Domain;

namespace SshKeySetupTool.Tests.Domain;

public sealed class CodexConnectionDetailsTests
{
    [Fact]
    public void Format_ReturnsReadyToPasteSshCommand()
    {
        var request = new SetupRequest(
            "203.0.113.10",
            31122,
            "root",
            "password-is-not-in-the-output",
            @"C:\Users\lin\.ssh\id_ed25519_codex_5");

        var details = CodexConnectionDetails.Format(request, request.PrivateKeyPath);

        const string expected = "ssh -p 31122 -i \"C:\\Users\\lin\\.ssh\\id_ed25519_codex_5\" root@203.0.113.10";

        Assert.Equal(expected, details);
        Assert.DoesNotContain(request.Password, details);
    }
}
