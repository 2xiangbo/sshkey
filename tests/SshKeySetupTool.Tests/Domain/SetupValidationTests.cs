using SshKeySetupTool.Domain;

namespace SshKeySetupTool.Tests.Domain;

public sealed class SetupValidationTests
{
    [Fact]
    public void Validate_ReportsRequiredFieldsAndInvalidPort()
    {
        var request = new SetupRequest(" ", 0, " ", "", "");
        var errors = SetupValidation.Validate(request);
        Assert.Contains("Server IP address is required.", errors);
        Assert.Contains("SSH port must be between 1 and 65535.", errors);
        Assert.Contains("SSH account name is required.", errors);
        Assert.Contains("Password is required.", errors);
        Assert.Contains("Private-key save path is required.", errors);
    }
}
