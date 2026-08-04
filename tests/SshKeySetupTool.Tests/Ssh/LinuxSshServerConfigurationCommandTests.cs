using SshKeySetupTool.Domain;
using SshKeySetupTool.Ssh;

namespace SshKeySetupTool.Tests.Ssh;

public sealed class LinuxSshServerConfigurationCommandTests
{
    [Theory]
    [InlineData("pubkeyauthentication yes\n", SshPublicKeyAuthenticationState.Enabled)]
    [InlineData("pubkeyauthentication no\n", SshPublicKeyAuthenticationState.Disabled)]
    [InlineData("unexpected output\n", SshPublicKeyAuthenticationState.Unavailable)]
    public void ProbeParse_ReturnsStructuredState(
        string output,
        SshPublicKeyAuthenticationState expected)
    {
        Assert.Equal(expected, SshServerConfigurationProbe.Parse(output).State);
    }

    [Fact]
    public void ProbeParse_RejectsConflictingDirectivesAndPreservesRawOutput()
    {
        const string output = " PubkeyAuthentication yes\nPubkeyAuthentication no\n";

        var probe = SshServerConfigurationProbe.Parse(output);

        Assert.Equal(SshPublicKeyAuthenticationState.Unavailable, probe.State);
        Assert.Equal(output, probe.RawOutput);
    }

    [Fact]
    public void BuildApply_ContainsReversibleDropInFallbackValidationAndReload()
    {
        var command = LinuxSshServerConfigurationCommand.BuildApply(
            "0123456789abcdef0123456789abcdef");

        Assert.Contains("/etc/ssh/sshd_config.d/00-sshkey-setup-tool.conf", command);
        Assert.Contains("/etc/ssh/sshd_config", command);
        Assert.Contains("PubkeyAuthentication yes", command);
        Assert.Contains("cp -a", command);
        Assert.Contains("\"$sshd\" -t", command);
        Assert.Contains("\"$sshd\" -T", command);
        Assert.Contains("systemctl reload sshd", command);
        Assert.Contains("systemctl reload ssh", command);
        Assert.Contains("service sshd reload", command);
        Assert.Contains("service ssh reload", command);
        Assert.Contains("# SSHKEY operation: 0123456789abcdef0123456789abcdef", command);
        Assert.Contains("restore_drop_in_and_reload", command);
        Assert.Contains("restore_main_and_reload", command);
        Assert.Contains("systemctl reload sshd >/dev/null 2>&1", command);
        Assert.Contains("service ssh reload >/dev/null 2>&1", command);
        Assert.Contains("SSHKEY_APPLIED", command);
    }

    [Theory]
    [InlineData("")]
    [InlineData("../escape")]
    [InlineData("0123456789ABCDEF0123456789ABCDEF")]
    public void BuildApply_RejectsAnUnsafeOperationId(string operationId)
    {
        Assert.Throws<ArgumentException>(
            () => LinuxSshServerConfigurationCommand.BuildApply(operationId));
    }

    [Theory]
    [InlineData("SSHKEY_APPLIED drop-in-new\n", SshServerConfigurationStrategy.ManagedDropIn, false)]
    [InlineData("SSHKEY_APPLIED drop-in-existing\n", SshServerConfigurationStrategy.ManagedDropIn, true)]
    [InlineData("SSHKEY_APPLIED main\n", SshServerConfigurationStrategy.MainConfiguration, false)]
    public void ParseApplyResult_ReturnsTheOwnedTransaction(
        string output,
        SshServerConfigurationStrategy strategy,
        bool hadExistingDropIn)
    {
        var change = LinuxSshServerConfigurationCommand.ParseApplyResult(
            "0123456789abcdef0123456789abcdef",
            output);

        Assert.Equal(strategy, change.Strategy);
        Assert.Equal(hadExistingDropIn, change.HadExistingManagedDropIn);
    }

    [Fact]
    public void ParseApplyResult_RejectsMalformedOutputWithApplyFailure()
    {
        var error = Assert.Throws<SshSetupOperationException>(() =>
            LinuxSshServerConfigurationCommand.ParseApplyResult(
                "0123456789abcdef0123456789abcdef",
                "SSHKEY_ERROR validation-failed\n"));

        Assert.Equal(SetupFailureKind.ServerConfigurationApply, error.FailureKind);
    }

    [Fact]
    public void BuildCommit_DeletesOnlyTransactionBackup()
    {
        var command = LinuxSshServerConfigurationCommand.BuildCommit(
            new SshServerConfigurationChange(
                "0123456789abcdef0123456789abcdef",
                SshServerConfigurationStrategy.MainConfiguration,
                false));

        Assert.Contains("rm -f -- '/etc/ssh/sshd_config.sshkey-setup-0123456789abcdef0123456789abcdef.bak'", command);
        Assert.DoesNotContain("$(", command);
        Assert.DoesNotContain("`", command);
    }

    [Fact]
    public void BuildRollback_RemovesNewDropInWithoutUsingOutputPaths()
    {
        var command = LinuxSshServerConfigurationCommand.BuildRollback(
            new SshServerConfigurationChange(
                "0123456789abcdef0123456789abcdef",
                SshServerConfigurationStrategy.ManagedDropIn,
                false));

        Assert.Contains("rm -f -- '/etc/ssh/sshd_config.d/00-sshkey-setup-tool.conf'", command);
        Assert.Contains("\"$sshd\" -t", command);
        Assert.DoesNotContain("$(cat", command);
    }

    [Fact]
    public void BuildRollback_RestoresExistingDropInFromTransactionBackup()
    {
        var command = LinuxSshServerConfigurationCommand.BuildRollback(
            new SshServerConfigurationChange(
                "0123456789abcdef0123456789abcdef",
                SshServerConfigurationStrategy.ManagedDropIn,
                true));

        Assert.Contains("cp -a -- '/etc/ssh/sshd_config.d/00-sshkey-setup-tool.conf.sshkey-setup-0123456789abcdef0123456789abcdef.bak' '/etc/ssh/sshd_config.d/00-sshkey-setup-tool.conf'", command);
    }

    [Fact]
    public void BuildRecovery_RestoresOnlyArtifactsOwnedByTheOperation()
    {
        var command = LinuxSshServerConfigurationCommand.BuildRecovery(
            "0123456789abcdef0123456789abcdef");

        Assert.Contains(
            "'/etc/ssh/sshd_config.sshkey-setup-0123456789abcdef0123456789abcdef.bak'",
            command);
        Assert.Contains(
            "'/etc/ssh/sshd_config.d/00-sshkey-setup-tool.conf.sshkey-setup-0123456789abcdef0123456789abcdef.bak'",
            command);
        Assert.Contains(
            "# SSHKEY operation: 0123456789abcdef0123456789abcdef",
            command);
        Assert.Contains(
            "# SSHKEY new drop-in: 0123456789abcdef0123456789abcdef",
            command);
        Assert.Contains("\"$sshd\" -t", command);
        Assert.Contains("reload_sshd", command);
        Assert.DoesNotContain("../", command);
    }
}
