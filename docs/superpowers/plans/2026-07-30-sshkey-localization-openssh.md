# SSHKEY Bilingual UI and OpenSSH Readiness Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a Chinese-default, runtime-switchable SSHKEY interface with footer links and a first-row OpenSSH Client readiness/install action.

**Architecture:** Keep app-owned UI strings in a small presentation catalog and let `Form1` apply one catalog instance to every control. Add a scoped OpenSSH manager that detects `ssh.exe`, creates the elevated Windows optional-feature installation command, and returns a state the form can render.

**Tech Stack:** .NET 8, C# 12, Windows Forms, xUnit.

## Global Constraints

- Default language is `中文`; choices are exactly `中文` and `EN`.
- Chinese title is `SSHKEY   //   SSH密钥设置`; English title is `SSHKEY   //   SSH Key Setup`.
- The Chinese primary action is `生成并写入服务器`; English primary action is `Generate and Install`.
- Links target exactly `https://github.com/2xiangbo/sshkey` and `https://xxcodex.com`.
- OpenSSH installation uses elevated Windows optional feature `OpenSSH.Client~~~~0.0.1.0`.
- Do not rename solution, namespaces, or executable file.

---

### Task 1: Add centralized bilingual text catalog

**Files:**
- Create: `src/SshKeySetupTool/Presentation/UiLanguage.cs`
- Create: `tests/SshKeySetupTool.Tests/Presentation/UiLanguageTests.cs`
- Modify: `src/SshKeySetupTool/SshKeySetupTool.csproj`

**Interfaces:** Produces `UiLanguage { Chinese, English }`, `UiText`, and `UiTextCatalog.For(UiLanguage)`. The catalog owns all strings used by `Form1`: titles, labels, primary action, OpenSSH states, statuses, and host-key confirmation copy.

- [ ] **Step 1: Write the failing catalog tests**

```csharp
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
```

- [ ] **Step 2: Run the tests and verify they fail**

Run: `dotnet test tests/SshKeySetupTool.Tests/SshKeySetupTool.Tests.csproj --filter FullyQualifiedName~UiLanguageTests`

Expected: compilation failure naming missing `UiTextCatalog` and `UiLanguage`.

- [ ] **Step 3: Implement the catalog and metadata**

```csharp
public enum UiLanguage { Chinese, English }

public sealed record UiText(string LanguageChoice, string Title, string Host, string Port,
    string Username, string Password, string PrivateKeyPath, string Status,
    string ConnectionDetails, string GenerateAndInstall, string CheckingOpenSsh,
    string OpenSshInstalled, string InstallOpenSsh, string OpenSshInstallFailed);

public static class UiTextCatalog
{
    public static UiText For(UiLanguage language) => language == UiLanguage.English
        ? new("EN", "SSHKEY   //   SSH Key Setup", "Server IP", "Port", "Username", "Password", "Private key path", "Status", "Codex connection details", "Generate and Install", "Checking OpenSSH…", "✓ OpenSSH installed", "Install OpenSSH", "OpenSSH installation failed — retry")
        : new("中文", "SSHKEY   //   SSH密钥设置", "服务器 IP", "端口", "账号", "密码", "私钥路径", "状态", "Codex 连接信息", "生成并写入服务器", "检测 OpenSSH…", "✓ OpenSSH 已安装", "一键安装 OpenSSH", "OpenSSH 安装失败，请重试");
}
```

Set `<AssemblyTitle>SSHKEY</AssemblyTitle>` and `<Product>SSHKEY</Product>` in the app project. Add all remaining form-owned operation and confirmation strings to `UiText` rather than branching at individual call sites.

- [ ] **Step 4: Run the catalog tests**

Run: `dotnet test tests/SshKeySetupTool.Tests/SshKeySetupTool.Tests.csproj --filter FullyQualifiedName~UiLanguageTests`

Expected: 2 passing tests.

- [ ] **Step 5: Commit**

Run: `git add src/SshKeySetupTool/Presentation/UiLanguage.cs src/SshKeySetupTool/SshKeySetupTool.csproj tests/SshKeySetupTool.Tests/Presentation/UiLanguageTests.cs; git commit -m "feat: add SSHKEY bilingual UI text"`

### Task 2: Add testable OpenSSH readiness and elevated installation

**Files:**
- Create: `src/SshKeySetupTool/Ssh/WindowsOpenSshClientManager.cs`
- Create: `tests/SshKeySetupTool.Tests/Ssh/WindowsOpenSshClientManagerTests.cs`

**Interfaces:** Produces `OpenSshClientStatus { Installed, Missing, InstallFailed, InstallCancelled }` and `IOpenSshClientManager` with `CheckAsync(CancellationToken)` and `InstallAsync(CancellationToken)`.

- [ ] **Step 1: Write failing manager tests**

```csharp
[Fact]
public async Task CheckAsync_ReturnsMissingWhenResolverCannotFindSsh()
{
    var manager = new WindowsOpenSshClientManager(
        () => throw new FileNotFoundException(), (_, _) => Task.FromResult(0));
    Assert.Equal(OpenSshClientStatus.Missing, await manager.CheckAsync(CancellationToken.None));
}

[Fact]
public void CreateInstallStartInfo_RequestsElevationForTheOpenSshCapability()
{
    var startInfo = WindowsOpenSshClientManager.CreateInstallStartInfo();
    Assert.Equal("powershell.exe", startInfo.FileName);
    Assert.True(startInfo.UseShellExecute);
    Assert.Equal("runas", startInfo.Verb);
    Assert.Contains("OpenSSH.Client~~~~0.0.1.0", startInfo.Arguments);
}
```

- [ ] **Step 2: Run the tests and verify they fail**

Run: `dotnet test tests/SshKeySetupTool.Tests/SshKeySetupTool.Tests.csproj --filter FullyQualifiedName~WindowsOpenSshClientManagerTests`

Expected: compilation failure naming missing `WindowsOpenSshClientManager`.

- [ ] **Step 3: Implement manager and process boundary**

```csharp
public enum OpenSshClientStatus { Installed, Missing, InstallFailed, InstallCancelled }
public interface IOpenSshClientManager
{
    Task<OpenSshClientStatus> CheckAsync(CancellationToken cancellationToken);
    Task<OpenSshClientStatus> InstallAsync(CancellationToken cancellationToken);
}
```

Use `WindowsOpenSshExecutableResolver.Resolve` in the public constructor. The internal constructor receives `Func<WindowsOpenSshExecutables>` and `Func<ProcessStartInfo, CancellationToken, Task<int>>` for tests. `CreateInstallStartInfo` returns `powershell.exe` with `UseShellExecute = true`, `Verb = "runas"`, and an `Add-WindowsCapability -Online -Name OpenSSH.Client~~~~0.0.1.0` command. Map `FileNotFoundException` to `Missing`, Win32 error 1223 to `InstallCancelled`, nonzero exit to `InstallFailed`, and zero exit to a fresh check.

- [ ] **Step 4: Run the manager tests**

Run: `dotnet test tests/SshKeySetupTool.Tests/SshKeySetupTool.Tests.csproj --filter FullyQualifiedName~WindowsOpenSshClientManagerTests`

Expected: missing, installed, cancelled, nonzero exit, and command-construction tests pass.

- [ ] **Step 5: Commit**

Run: `git add src/SshKeySetupTool/Ssh/WindowsOpenSshClientManager.cs tests/SshKeySetupTool.Tests/Ssh/WindowsOpenSshClientManagerTests.cs; git commit -m "feat: add OpenSSH readiness manager"`

### Task 3: Bind localization, links, and OpenSSH state to the form

**Files:**
- Modify: `src/SshKeySetupTool/Form1.Designer.cs`
- Modify: `src/SshKeySetupTool/Form1.cs`
- Modify: `tests/SshKeySetupTool.Tests/Presentation/FormLayoutTests.cs`
- Modify: `tests/SshKeySetupTool.Tests/Presentation/FormLifecycleTests.cs`

**Interfaces:** Consumes `UiTextCatalog` and `IOpenSshClientManager`; adds an internal `Form1(IKeySetupService, IOpenSshClientManager)` constructor while preserving `Form1(IKeySetupService)`.

- [ ] **Step 1: Extend failing layout/lifecycle tests**

```csharp
var language = Find<ComboBox>(form, "languageComboBox");
var openSsh = Find<Button>(form, "openSshButton");
var project = Find<LinkLabel>(form, "projectLinkLabel");
var xxCodex = Find<LinkLabel>(form, "xxCodexLinkLabel");
Assert.Equal("SSHKEY   //   SSH密钥设置", form.Text);
Assert.Equal("中文", language.SelectedItem);
Assert.Equal("https://github.com/2xiangbo/sshkey", project.Tag);
Assert.Equal("https://xxcode.com", xxCodex.Tag);
Assert.True(host.Right < port.Left);
Assert.True(port.Right < openSsh.Left);
language.SelectedItem = "EN";
Assert.Equal("SSHKEY   //   SSH Key Setup", form.Text);
Assert.Equal("Generate and Install", generate.Text);
```

Use a fake installed `IOpenSshClientManager` so tests never trigger an optional-feature install. Preserve the existing key-setup test for a missing OpenSSH executable.

- [ ] **Step 2: Run and verify failure**

Run: `dotnet test tests/SshKeySetupTool.Tests/SshKeySetupTool.Tests.csproj --filter "FullyQualifiedName~FormLayoutTests|FullyQualifiedName~FormLifecycleTests"`

Expected: failures because controls, constructor, and language behavior are absent.

- [ ] **Step 3: Implement compact form behavior**

Add `languageComboBox`, `openSshButton`, `projectLinkLabel`, and `xxCodexLinkLabel`. Preserve the 680-pixel form width. Use first-row bounds host `20,76,244,28`, port `278,76,72,28`, OpenSSH button `364,76,296,28`; place links on the left footer, a 70-pixel combo immediately left of the existing primary button, then the primary button.

Initialize `中文`, populate `中文`/`EN`, and implement `ApplyLanguage(UiLanguage)` to update the window, custom header, labels, main button, OpenSSH state, form-owned statuses, and confirmation dialog. Check OpenSSH asynchronously in `Shown`; on missing state make the right button install, then refresh after installation. Disable it during check/install. Attach the exact URLs in `LinkLabel.Tag`; open them using `Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })` from the click handler. Tests assert tags and do not launch a browser.

- [ ] **Step 4: Run form and full tests**

Run: `dotnet test SshKeySetupTool.sln`

Expected: all tests pass.

- [ ] **Step 5: Commit**

Run: `git add src/SshKeySetupTool/Form1.cs src/SshKeySetupTool/Form1.Designer.cs tests/SshKeySetupTool.Tests/Presentation/FormLayoutTests.cs tests/SshKeySetupTool.Tests/Presentation/FormLifecycleTests.cs; git commit -m "feat: localize SSHKEY form and add OpenSSH action"`

### Task 4: Release smoke verification

**Files:** No source changes expected.

- [ ] **Step 1: Build release**

Run: `dotnet build SshKeySetupTool.sln -c Release --no-restore`

Expected: zero errors.

- [ ] **Step 2: Run publish workflow**

Run: `powershell -ExecutionPolicy Bypass -File scripts/publish.ps1`

Expected: a publish artifact is produced; no source file changes are introduced.

- [ ] **Step 3: Verify working tree**

Run: `git status --short`

Expected: only intentionally generated/ignored artifacts, if any.

## Plan self-review

- Spec coverage: Tasks 1–3 cover every visible identity, localization, layout, link, readiness, UAC, and test requirement. Task 4 verifies release output.
- Completeness scan: no unfinished markers or deferred implementation instructions.
- Type consistency: `UiLanguage` and `UiTextCatalog` originate in Task 1; `IOpenSshClientManager` originates in Task 2; both are consumed by Task 3.
