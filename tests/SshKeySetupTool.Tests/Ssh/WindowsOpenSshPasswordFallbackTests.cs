using SshKeySetupTool.Ssh;
using System.Text;

namespace SshKeySetupTool.Tests.Ssh;

public sealed class WindowsOpenSshPasswordFallbackTests
{
    [Theory]
    [InlineData("root@server's password: ")]
    [InlineData("Password:")]
    public void TryGetPasswordResponse_ReturnsPasswordOnlyForPasswordPrompts(string prompt)
    {
        var recognized = WindowsOpenSshPasswordFallback.TryGetPasswordResponse(
            [prompt],
            WindowsOpenSshPasswordFallback.AskPassModeValue,
            "secret",
            out var response);

        Assert.True(recognized);
        Assert.Equal("secret", response);
    }

    [Theory]
    [InlineData("The authenticity of host cannot be established. Are you sure you want to continue connecting (yes/no/[fingerprint])?")]
    [InlineData("Host key fingerprint is SHA256:abc. Continue connecting (yes/no)?")]
    [InlineData("Verification code:")]
    public void TryGetPasswordResponse_NeverReturnsPasswordForHostOrUnknownPrompts(string prompt)
    {
        var recognized = WindowsOpenSshPasswordFallback.TryGetPasswordResponse(
            [prompt],
            WindowsOpenSshPasswordFallback.AskPassModeValue,
            "secret",
            out var response);

        Assert.False(recognized);
        Assert.Null(response);
    }

    [Fact]
    public void TryGetPasswordResponse_RequiresTheDedicatedAskPassMode()
    {
        Assert.False(WindowsOpenSshPasswordFallback.TryGetPasswordResponse(
            ["Password:"],
            null,
            "secret",
            out _));
    }

    [Fact]
    public void WriteAskPassPassword_WritesOnlyThePassword()
    {
        using var output = new MemoryStream();

        WindowsOpenSshPasswordFallback.WriteAskPassPassword(output, "secret");

        Assert.Equal("secret", Encoding.UTF8.GetString(output.ToArray()));
    }
}
