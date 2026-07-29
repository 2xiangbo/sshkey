using SshKeySetupTool.Presentation;

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
}
