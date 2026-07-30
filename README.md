<p align="center">
  <img src="src/SshKeySetupTool/Assets/ssh-key-tool-icon-1024.png" width="112" alt="SSHKEY icon">
</p>

<h1 align="center">SSHKEY</h1>

<p align="center">
  A lightweight Windows GUI for configuring SSH keys on remote servers.
</p>

<p align="center">
  <a href="#english">English</a> |
  <a href="#中文">中文</a>
</p>

## English

SSHKEY simplifies SSH key setup for remote development. Enter the server IP,
port, username, password, and private-key path; SSHKEY handles the rest.

It verifies the server fingerprint, creates an Ed25519 key pair, installs the
public key, validates key-based authentication, and generates ready-to-copy SSH
connection details.

### Features

- Compact Windows desktop interface with Chinese and English support
- Built-in OpenSSH detection and one-click installation — no manual PowerShell
  command is required
- Ed25519 private and public key generation
- Secure private-key file permissions
- SSH server fingerprint confirmation
- Idempotent public-key installation in `authorized_keys`
- Key-based login verification
- Ready-to-copy connection details for AI coding tools
- Standalone Windows executable; no Python installation required

### Works With

SSHKEY is designed for remote development workflows with Codex, Claude Code,
Cursor, Windsurf, TRAE, GitHub Copilot, Antigravity/Gemini CLI, Cline, Roo Code,
Aider, OpenCode, Qwen Code, Kiro, Zed, and other SSH-capable AI coding tools.

### Requirements

- Windows 10 or Windows 11 (64-bit)
- A Linux server that currently allows password authentication

### Usage

1. Start SSHKEY.
2. If OpenSSH is missing, select **Install OpenSSH** and approve the Windows
   administrator prompt.
3. Enter the server IP, SSH port, username, password, and private-key path.
4. Confirm the server fingerprint.
5. Select **Generate and Install**.
6. Copy the generated connection details into your AI coding tool.

The password is used only for the initial SSH connection and is cleared from
the window after the operation. It is not saved to disk.

### Build From Source

Install the .NET 8 SDK, clone the repository, and run:

```powershell
dotnet test .\SshKeySetupTool.sln -c Release
powershell -ExecutionPolicy Bypass -File .\scripts\publish.ps1
```

The standalone executable is created in `outputs\`.

## 中文

SSHKEY 是一款轻量、易用的 Windows SSH 密钥配置工具，可帮助用户快速为远程服务器生成并安装 SSH 密钥。

只需输入服务器 IP、端口、账号、密码和私钥保存路径，SSHKEY 即可完成服务器指纹确认、Ed25519 密钥生成、公钥写入、密钥登录验证，并自动生成可直接复制使用的 SSH 连接信息。

### 功能

- 紧凑的 Windows 桌面界面，支持中文和英文
- 内置 OpenSSH 检测与一键安装，无需手动输入 PowerShell 命令
- 生成 Ed25519 私钥和公钥
- 自动保护私钥文件权限
- 确认 SSH 服务器指纹
- 幂等写入服务器的 `authorized_keys`
- 自动验证密钥登录
- 生成适用于 AI 编程工具的连接信息
- 独立 Windows EXE，无需安装 Python

### 适用工具

适用于 Codex、Claude Code、Cursor、Windsurf、TRAE、GitHub Copilot、Antigravity/Gemini CLI、Cline、Roo Code、Aider、OpenCode、Qwen Code、Kiro、Zed 等 AI 编程工具及远程开发场景。

### 系统要求

- 64 位 Windows 10 或 Windows 11
- 当前允许密码登录的 Linux 服务器

### 使用方法

1. 启动 SSHKEY。
2. 如果缺少 OpenSSH，点击**一键安装 OpenSSH**并同意 Windows 管理员授权。
3. 输入服务器 IP、SSH 端口、账号、密码和私钥保存路径。
4. 核对并确认服务器指纹。
5. 点击**生成并写入服务器**。
6. 将生成的连接信息复制到 AI 编程工具中。

密码仅用于首次 SSH 连接，操作完成后会从窗口中清空，不会保存到磁盘。

### 从源码构建

安装 .NET 8 SDK，克隆仓库后运行：

```powershell
dotnet test .\SshKeySetupTool.sln -c Release
powershell -ExecutionPolicy Bypass -File .\scripts\publish.ps1
```

生成的独立 EXE 位于 `outputs\` 目录。
