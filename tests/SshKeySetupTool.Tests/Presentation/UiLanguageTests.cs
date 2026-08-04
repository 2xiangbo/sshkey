using SshKeySetupTool.Presentation;
using SshKeySetupTool.Domain;

namespace SshKeySetupTool.Tests.Presentation;

public sealed class UiLanguageTests
{
    [Fact]
    public void ChineseCatalog_UsesTheRequestedDefaultCopy()
    {
        var text = UiTextCatalog.For(UiLanguage.Chinese);

        Assert.Equal("SSHKEY   //   SSH密钥设置", text.Title);
        Assert.Equal("生成并写入服务器", text.GenerateAndInstall);
        Assert.Equal("中文", text.LanguageChoice);
    }

    [Fact]
    public void EnglishCatalog_UsesTheRequestedActionAndTitle()
    {
        var text = UiTextCatalog.For(UiLanguage.English);

        Assert.Equal("SSHKEY   //   SSH Key Setup", text.Title);
        Assert.Equal("Generate and Install", text.GenerateAndInstall);
        Assert.Equal("EN", text.LanguageChoice);
    }

    [Fact]
    public void Catalogs_ContainServerRepairConsentAndPhaseCopy()
    {
        var english = UiTextCatalog.For(UiLanguage.English);
        var chinese = UiTextCatalog.For(UiLanguage.Chinese);

        Assert.Equal("Enable SSH public-key authentication", english.ConfirmServerConfigurationTitle);
        Assert.Contains("PubkeyAuthentication no", english.ConfirmServerConfigurationMessageFormat);
        Assert.Contains("PubkeyAuthentication no", chinese.ConfirmServerConfigurationMessageFormat);
        Assert.NotEmpty(english.CheckingServerConfiguration);
        Assert.NotEmpty(chinese.RollingBackServerConfiguration);
        Assert.NotEqual(english.CheckingServerConfiguration, english.EnablingServerConfiguration);
    }

    [Theory]
    [InlineData(SetupFailureKind.ServerConfigurationInspection)]
    [InlineData(SetupFailureKind.ServerConfigurationRootRequired)]
    [InlineData(SetupFailureKind.ServerConfigurationDeclined)]
    [InlineData(SetupFailureKind.ServerConfigurationApply)]
    [InlineData(SetupFailureKind.PublicKeyInstallation)]
    [InlineData(SetupFailureKind.PrivateKeyVerification)]
    [InlineData(SetupFailureKind.Rollback)]
    public void Catalogs_HaveDistinctKnownFailureLabels(SetupFailureKind kind)
    {
        Assert.False(string.IsNullOrWhiteSpace(
            UiTextCatalog.For(UiLanguage.English).FailureLabel(kind)));
        Assert.False(string.IsNullOrWhiteSpace(
            UiTextCatalog.For(UiLanguage.Chinese).FailureLabel(kind)));
    }
}
