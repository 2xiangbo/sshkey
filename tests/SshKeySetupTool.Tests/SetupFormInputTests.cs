using SshKeySetupTool.Domain;

namespace SshKeySetupTool.Tests;

public sealed class SetupFormInputTests
{
    [Fact]
    public void BuildRequest_MapsFormTextValues()
    {
        var request = SetupFormInput.BuildRequest(
            "198.51.100.7",
            "2222",
            "admin",
            "password",
            @"C:\keys\id_ed25519");

        Assert.Equal("198.51.100.7", request.Host);
        Assert.Equal(2222, request.Port);
        Assert.Equal("admin", request.Username);
        Assert.Equal("password", request.Password);
        Assert.Equal(@"C:\keys\id_ed25519", request.PrivateKeyPath);
    }

    [Fact]
    public void BuildRequest_UsesZeroPortWhenPortTextIsNotANumber()
    {
        var request = SetupFormInput.BuildRequest("server", "not-a-port", "admin", "password", "key");

        Assert.Equal(0, request.Port);
    }

    [Fact]
    public void GetSuggestedPrivateKeyPath_UsesAnAvailableCodexSpecificName()
    {
        var userProfilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var sshDirectory = Path.Combine(userProfilePath, ".ssh");
        Directory.CreateDirectory(sshDirectory);
        File.WriteAllText(Path.Combine(sshDirectory, "id_ed25519_codex"), "existing key");

        try
        {
            var path = SetupFormInput.GetSuggestedPrivateKeyPath(userProfilePath);

            Assert.Equal(Path.Combine(sshDirectory, "id_ed25519_codex_2"), path);
        }
        finally
        {
            Directory.Delete(userProfilePath, recursive: true);
        }
    }
}
