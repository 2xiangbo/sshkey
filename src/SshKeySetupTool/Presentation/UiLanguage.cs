namespace SshKeySetupTool.Presentation;

public enum UiLanguage
{
    Chinese,
    English
}

public sealed record UiText(
    string LanguageChoice,
    string Title,
    string Host,
    string Port,
    string Username,
    string Password,
    string PrivateKeyPath,
    string Status,
    string ConnectionDetails,
    string GenerateAndInstall,
    string Ready,
    string Working,
    string CompletedCopied,
    string CompletedNotCopied,
    string Cancelled,
    string FailedPrefix,
    string ConfirmServerTitle,
    string ConfirmServerMessageFormat,
    string CheckingOpenSsh,
    string OpenSshInstalled,
    string InstallOpenSsh,
    string OpenSshCheckFailed,
    string OpenSshInstallFailed,
    string OpenSshInstallCancelled);

public static class UiTextCatalog
{
    public static UiText For(UiLanguage language) => language == UiLanguage.English
        ? new UiText(
            "EN",
            "SSHKEY   //   SSH Key Setup",
            "Server IP",
            "Port",
            "Username",
            "Password",
            "Private key path",
            "Status",
            "Codex connection details",
            "Generate and Install",
            "Ready.",
            "Generating the local key and connecting to the server...",
            "Complete. Codex connection details are shown below and copied to the clipboard.",
            "Complete. Codex connection details are shown below; please copy them manually.",
            "Operation cancelled.",
            "Failed: ",
            "Confirm server",
            "First connection to server {0}.\r\n\r\nServer SHA-256 key fingerprint:\r\n{1}\r\n\r\nTrust this server for this operation only?",
            "Checking OpenSSH…",
            "✓ OpenSSH installed",
            "Install OpenSSH",
            "OpenSSH check failed",
            "OpenSSH installation failed — retry",
            "OpenSSH installation was cancelled — retry" )
        : new UiText(
            "中文",
            "SSHKEY   //   SSH密钥设置",
            "服务器 IP",
            "端口",
            "账号",
            "密码",
            "私钥路径",
            "状态",
            "Codex 连接信息",
            "生成并写入服务器",
            "准备就绪。",
            "正在生成本地密钥并连接服务器...",
            "完成，Codex 连接信息已显示在下方并复制到剪贴板。",
            "完成，Codex 连接信息已显示在下方，请手动复制。",
            "操作已取消。",
            "失败：",
            "确认服务器",
            "首次连接服务器 {0}。\r\n\r\n服务器 SHA-256 密钥指纹：\r\n{1}\r\n\r\n是否仅在本次操作中信任此服务器？",
            "检测 OpenSSH…",
            "✓ OpenSSH 已安装",
            "一键安装 OpenSSH",
            "OpenSSH 检测失败",
            "OpenSSH 安装失败，请重试",
            "OpenSSH 安装已取消，请重试");
}
