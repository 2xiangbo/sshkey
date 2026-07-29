# SSH Key Setup Tool Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a self-contained Windows executable that creates an Ed25519 SSH key, installs its public key on a password-authenticated Linux account, and verifies key authentication for Codex.

**Architecture:** A .NET 8 WinForms shell delegates to testable application services. BouncyCastle produces OpenSSH-compatible key files; SSH.NET authenticates with a password then the generated key. A JSON known-host store records approved SSH host fingerprints in Local AppData.

**Tech Stack:** .NET 8 SDK, C# 12, WinForms, SSH.NET 2025.1.0, BouncyCastle.Cryptography 2.6.2, xUnit, `dotnet publish`.

## Global Constraints

- Target `net8.0-windows`, `win-x64`, self-contained and single-file.
- The user must not need Python or a separately installed .NET runtime.
- Support Linux SSH servers only and do not change `sshd` configuration.
- Passwords stay only in memory. Never write, log, or put them on a command line.
- Default key path: `%USERPROFILE%\\.ssh\\id_ed25519`; never overwrite a private key or its `.pub` file.
- Default trust store: `%LOCALAPPDATA%\\SshKeySetupTool\\known-hosts.json`.
- Git is initialized solely to support isolated worktrees, progress tracking, and diff-based reviews. Keep commits focused on the current task.

---

## Files

```text
SshKeySetupTool.sln
src/SshKeySetupTool/
  SshKeySetupTool.csproj
  Program.cs
  Domain/{SetupRequest,SetupValidation}.cs
  Security/{OpenSshKeyMaterialFactory,KnownHostStore,HostTrustService}.cs
  Ssh/{SshNetSessionFactory,LinuxAuthorizedKeyCommand}.cs
  Application/{KeySetupService,SetupController}.cs
  UI/SetupForm.cs
  Properties/PublishProfile.pubxml
tests/SshKeySetupTool.Tests/
  SshKeySetupTool.Tests.csproj
  Domain/SetupValidationTests.cs
  Security/{OpenSshKeyMaterialFactoryTests,HostTrustServiceTests}.cs
  Ssh/LinuxAuthorizedKeyCommandTests.cs
  Application/{KeySetupServiceTests,SetupControllerTests}.cs
scripts/publish.ps1
README.md
```

### Task 1: Create the buildable Windows solution

**Files:** Create `SshKeySetupTool.sln`, the two `.csproj` files, `Program.cs`, `Properties/PublishProfile.pubxml`, and `scripts/publish.ps1`.

**Produces:** a WinForms project with a test project and a reproducible publish command.

- [ ] **Step 1: Install the missing SDK.**

Run:

```powershell
winget install --id Microsoft.DotNet.SDK.8 --exact --source winget
dotnet --list-sdks
```

Expected: an `8.0.x` SDK is listed. This machine currently has the .NET 8 runtime only. If `winget` is unavailable, install the .NET 8 SDK from `https://dotnet.microsoft.com/download/dotnet/8.0`, open a new terminal, then rerun the check.

- [ ] **Step 2: Create solution and projects.**

Run:

```powershell
dotnet new sln --name SshKeySetupTool
dotnet new winforms --framework net8.0 --name SshKeySetupTool --output src/SshKeySetupTool
dotnet new xunit --framework net8.0 --name SshKeySetupTool.Tests --output tests/SshKeySetupTool.Tests
dotnet sln SshKeySetupTool.sln add src/SshKeySetupTool/SshKeySetupTool.csproj tests/SshKeySetupTool.Tests/SshKeySetupTool.Tests.csproj
dotnet add tests/SshKeySetupTool.Tests/SshKeySetupTool.Tests.csproj reference src/SshKeySetupTool/SshKeySetupTool.csproj
dotnet add src/SshKeySetupTool/SshKeySetupTool.csproj package SSH.NET --version 2025.1.0
dotnet add src/SshKeySetupTool/SshKeySetupTool.csproj package BouncyCastle.Cryptography --version 2.6.2
```

- [ ] **Step 3: Set application and publish properties.**

Write `src/SshKeySetupTool/SshKeySetupTool.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="SSH.NET" Version="2025.1.0" />
    <PackageReference Include="BouncyCastle.Cryptography" Version="2.6.2" />
  </ItemGroup>
</Project>
```

Write `src/SshKeySetupTool/Properties/PublishProfile.pubxml`:

```xml
<Project><PropertyGroup>
  <Configuration>Release</Configuration><RuntimeIdentifier>win-x64</RuntimeIdentifier>
  <SelfContained>true</SelfContained><PublishSingleFile>true</PublishSingleFile>
  <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract><PublishTrimmed>false</PublishTrimmed>
</PropertyGroup></Project>
```

Write `scripts/publish.ps1`:

```powershell
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '..\src\SshKeySetupTool\SshKeySetupTool.csproj'
dotnet publish $project -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o (Join-Path $PSScriptRoot '..\outputs')
```

- [ ] **Step 4: Verify the project infrastructure.**

Run:

```powershell
dotnet build SshKeySetupTool.sln -c Release
dotnet test SshKeySetupTool.sln -c Release
```

Expected: both commands exit `0`.

### Task 2: Implement input records and validation with tests first

**Files:** Create `Domain/SetupRequest.cs`, `Domain/SetupValidation.cs`; test `tests/SshKeySetupTool.Tests/Domain/SetupValidationTests.cs`.

**Produces:** `SetupRequest`, `SetupProgress`, `SetupResult`, and `SetupValidation.Validate(SetupRequest)` returning `IReadOnlyList<string>`.

- [ ] **Step 1: Write the failing validation test.**

```csharp
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
```

- [ ] **Step 2: Confirm the test is red.**

Run: `dotnet test tests/SshKeySetupTool.Tests/SshKeySetupTool.Tests.csproj --filter FullyQualifiedName~SetupValidationTests`

Expected: FAIL because the types do not exist.

- [ ] **Step 3: Implement the smallest passing domain API.**

Write `Domain/SetupRequest.cs`:

```csharp
namespace SshKeySetupTool.Domain;

public sealed record SetupRequest(string Host, int Port, string Username, string Password, string PrivateKeyPath);
public sealed record SetupProgress(string Message, bool IsError = false);
public sealed record SetupResult(bool Succeeded, string Message, string? PrivateKeyPath = null);
```

Write `Domain/SetupValidation.cs`:

```csharp
namespace SshKeySetupTool.Domain;

public static class SetupValidation
{
    public static IReadOnlyList<string> Validate(SetupRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Host)) errors.Add("Server IP address is required.");
        if (request.Port is < 1 or > 65535) errors.Add("SSH port must be between 1 and 65535.");
        if (string.IsNullOrWhiteSpace(request.Username)) errors.Add("SSH account name is required.");
        if (string.IsNullOrEmpty(request.Password)) errors.Add("Password is required.");
        if (string.IsNullOrWhiteSpace(request.PrivateKeyPath)) errors.Add("Private-key save path is required.");
        return errors;
    }
}
```

- [ ] **Step 4: Confirm green.**

Run: `dotnet test tests/SshKeySetupTool.Tests/SshKeySetupTool.Tests.csproj --filter FullyQualifiedName~SetupValidationTests`

Expected: PASS.

### Task 3: Generate and protect OpenSSH Ed25519 key files with tests first

**Files:** Create `Security/OpenSshKeyMaterialFactory.cs`; test `tests/SshKeySetupTool.Tests/Security/OpenSshKeyMaterialFactoryTests.cs`.

**Produces:** `IKeyMaterialFactory.Create(string privateKeyPath)` returning `KeyMaterial(string PrivateKeyPath, string PublicKeyPath, string PublicKeyLine)`.

- [ ] **Step 1: Write failing behavior tests.**

```csharp
using SshKeySetupTool.Security;

namespace SshKeySetupTool.Tests.Security;

public sealed class OpenSshKeyMaterialFactoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    [Fact]
    public void Create_WritesOpenSshPrivateAndPublicFiles()
    {
        var key = new OpenSshKeyMaterialFactory().Create(Path.Combine(_directory, "id_ed25519"));
        Assert.StartsWith("-----BEGIN OPENSSH PRIVATE KEY-----", File.ReadAllText(key.PrivateKeyPath));
        Assert.StartsWith("ssh-ed25519 ", File.ReadAllText(key.PublicKeyPath));
        Assert.Equal(File.ReadAllText(key.PublicKeyPath).Trim(), key.PublicKeyLine);
    }

    [Fact]
    public void Create_RefusesExistingPrivateKey()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "id_ed25519");
        File.WriteAllText(path, "existing");
        Assert.Throws<IOException>(() => new OpenSshKeyMaterialFactory().Create(path));
    }

    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
}
```

- [ ] **Step 2: Confirm the test is red.**

Run: `dotnet test tests/SshKeySetupTool.Tests/SshKeySetupTool.Tests.csproj --filter FullyQualifiedName~OpenSshKeyMaterialFactoryTests`

Expected: FAIL because `OpenSshKeyMaterialFactory` is missing.

- [ ] **Step 3: Implement OpenSSH encoding and user-only private-key ACL.**

Create the public surface below. Use `OpenSshPrivateKeyUtilities.EncodePrivateKey` and `OpenSshPublicKeyUtilities.EncodePublicKey` from BouncyCastle; write PEM-style OpenSSH headers around the Base64 private bytes.

```csharp
public sealed record KeyMaterial(string PrivateKeyPath, string PublicKeyPath, string PublicKeyLine);
public interface IKeyMaterialFactory { KeyMaterial Create(string privateKeyPath); }

public sealed class OpenSshKeyMaterialFactory : IKeyMaterialFactory
{
    public KeyMaterial Create(string privateKeyPath)
    {
        var publicKeyPath = privateKeyPath + ".pub";
        if (File.Exists(privateKeyPath) || File.Exists(publicKeyPath))
            throw new IOException("The selected private-key path already exists. Choose a new path.");
        Directory.CreateDirectory(Path.GetDirectoryName(privateKeyPath)!);
        var privateKey = new Org.BouncyCastle.Crypto.Parameters.Ed25519PrivateKeyParameters(
            new Org.BouncyCastle.Security.SecureRandom());
        var privateText = "-----BEGIN OPENSSH PRIVATE KEY-----\n" +
            Convert.ToBase64String(Org.BouncyCastle.Crypto.Utilities.OpenSshPrivateKeyUtilities.EncodePrivateKey(privateKey), Base64FormattingOptions.InsertLineBreaks) +
            "\n-----END OPENSSH PRIVATE KEY-----\n";
        var publicLine = "ssh-ed25519 " + Convert.ToBase64String(
            Org.BouncyCastle.Crypto.Utilities.OpenSshPublicKeyUtilities.EncodePublicKey(privateKey.GeneratePublicKey())) + " ssh-key-setup-tool";
        // CreateNew atomically rejects a collision that appears after preflight.
        using (var privateKeyFile = new FileStream(privateKeyPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            using var privateKeyWriter = new StreamWriter(privateKeyFile, new UTF8Encoding(false), 1024, leaveOpen: true);
            privateKeyWriter.Write(privateText);
        }
        ProtectPrivateKey(privateKeyPath);
        using var publicKeyFile = new FileStream(publicKeyPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var publicKeyWriter = new StreamWriter(publicKeyFile, new UTF8Encoding(false), 1024, leaveOpen: true);
        publicKeyWriter.Write(publicLine + Environment.NewLine);
        return new KeyMaterial(privateKeyPath, publicKeyPath, publicLine);
    }
}
```

Implement `ProtectPrivateKey` using `FileSecurity.SetAccessRuleProtection(true, false)` and one `FileSystemAccessRule` granting `WindowsIdentity.GetCurrent().User` full control. Do not alter public-key ACLs. If a later local public-key operation fails after the private key has been securely written, do not perform a path-based best-effort deletion: report the failure and retain the protected private key, because deleting a path that another process could replace is less safe than leaving a user-only key for inspection.

- [ ] **Step 4: Confirm green.**

Run: `dotnet test tests/SshKeySetupTool.Tests/SshKeySetupTool.Tests.csproj --filter FullyQualifiedName~OpenSshKeyMaterialFactoryTests`

Expected: PASS.

### Task 4: Trust host fingerprints and build the idempotent Linux install command

**Files:** Create `Security/KnownHostStore.cs`, `Security/HostTrustService.cs`, `Ssh/LinuxAuthorizedKeyCommand.cs`; test `Security/HostTrustServiceTests.cs`, `Ssh/LinuxAuthorizedKeyCommandTests.cs`.

**Produces:** a SHA-256 fingerprint decision and a safe remote command that creates `~/.ssh`, preserves existing keys, and appends only a missing exact public-key line.

- [ ] **Step 1: Write failing tests.**

```csharp
[Fact]
public void Check_PersistsFirstApprovedKeyAndRejectsChangedKey()
{
    var store = new InMemoryKnownHostStore();
    var trust = new HostTrustService(store);
    Assert.True(trust.Check("203.0.113.7:22", [1, 2, 3], _ => true));
    Assert.True(trust.Check("203.0.113.7:22", [1, 2, 3], _ => throw new Xunit.Sdk.XunitException()));
    var error = Assert.Throws<InvalidOperationException>(() => trust.Check("203.0.113.7:22", [4, 5, 6], _ => true));
    Assert.Equal("The SSH server fingerprint changed. Connection was blocked.", error.Message);
}

[Fact]
public void Build_DeduplicatesTheExactPublicKeyAndSetsPermissions()
{
    var command = LinuxAuthorizedKeyCommand.Build("ssh-ed25519 AAA test");
    Assert.Contains("mkdir -p ~/.ssh", command);
    Assert.Contains("chmod 700 ~/.ssh", command);
    Assert.Contains("grep -qxF --", command);
    Assert.Contains("chmod 600 ~/.ssh/authorized_keys", command);
}
```

- [ ] **Step 2: Confirm red.**

Run: `dotnet test tests/SshKeySetupTool.Tests/SshKeySetupTool.Tests.csproj --filter "FullyQualifiedName~HostTrustServiceTests|FullyQualifiedName~LinuxAuthorizedKeyCommandTests"`

Expected: FAIL because the trust and command classes are absent.

- [ ] **Step 3: Implement host trust.**

Define `IKnownHostStore` with `Get(string endpoint)` and `Set(string endpoint, string fingerprint)`. `HostTrustService.Check(endpoint, hostKey, confirmFirstUse)` must calculate `Convert.ToBase64String(SHA256.HashData(hostKey))`; approve an existing equal fingerprint; call `confirmFirstUse` and persist only for a first-use approval; throw the exact changed-fingerprint error for a mismatch. Implement `JsonKnownHostStore` by serializing `Dictionary<string,string>` atomically to `%LOCALAPPDATA%\SshKeySetupTool\known-hosts.json` using a `.tmp` replacement file.

- [ ] **Step 4: Implement the command builder.**

Add a `ShellQuote(string value)` helper that wraps the value in `'` and replaces every single quote with `'\"'\"'`. `Build` must return:

```text
mkdir -p ~/.ssh && chmod 700 ~/.ssh && touch ~/.ssh/authorized_keys && chmod 600 ~/.ssh/authorized_keys && { grep -qxF -- '<public-key>' ~/.ssh/authorized_keys || printf '%s\n' '<public-key>' >> ~/.ssh/authorized_keys; }
```

- [ ] **Step 5: Confirm green.**

Run: `dotnet test tests/SshKeySetupTool.Tests/SshKeySetupTool.Tests.csproj --filter "FullyQualifiedName~HostTrustServiceTests|FullyQualifiedName~LinuxAuthorizedKeyCommandTests"`

Expected: PASS.

### Task 5: Implement SSH.NET adapter and password-to-key workflow with tests first

**Files:** Create `Ssh/SshNetSessionFactory.cs`, `Application/KeySetupService.cs`; test `Application/KeySetupServiceTests.cs`.

**Produces:** `Task<SetupResult> IKeySetupRunner.RunAsync(SetupRequest, IProgress<SetupProgress>, CancellationToken)`.

- [ ] **Step 1: Write a failing happy-path workflow test.**

```csharp
[Fact]
public async Task RunAsync_InstallsThenVerifiesWithTheGeneratedKey()
{
    var keys = new FakeKeyMaterialFactory(new KeyMaterial(@"C:\keys\id_ed25519", @"C:\keys\id_ed25519.pub", "ssh-ed25519 AAA test"));
    var sessions = new FakeSshSessionFactory();
    var result = await new KeySetupService(keys, sessions).RunAsync(
        new SetupRequest("203.0.113.7", 22, "deploy", "password", @"C:\keys\id_ed25519"), new Progress<SetupProgress>(), CancellationToken.None);
    Assert.True(result.Succeeded);
    Assert.Equal(@"C:\keys\id_ed25519", result.PrivateKeyPath);
    Assert.Equal(["password", "key"], sessions.ConnectionModes);
    Assert.Contains("grep -qxF", sessions.PasswordSession.Commands.Single());
    Assert.Equal("true", sessions.KeySession.Commands.Single());
}
```

The test file must contain small `FakeKeyMaterialFactory`, `FakeSshSessionFactory`, and `FakeSshSession` implementations that record calls without mocking SSH.NET.

- [ ] **Step 2: Confirm red.**

Run: `dotnet test tests/SshKeySetupTool.Tests/SshKeySetupTool.Tests.csproj --filter FullyQualifiedName~KeySetupServiceTests`

Expected: FAIL because the service and session interfaces are absent.

- [ ] **Step 3: Implement interfaces and the minimal workflow.**

Create these interfaces:

```csharp
public interface ISshSession : IDisposable { void Execute(string command); }
public interface ISshSessionFactory
{
    ISshSession ConnectWithPassword(SetupRequest request);
    ISshSession ConnectWithKey(SetupRequest request, string privateKeyPath);
}
public interface IKeySetupRunner
{
    Task<SetupResult> RunAsync(SetupRequest request, IProgress<SetupProgress> progress, CancellationToken cancellationToken);
}
```

`KeySetupService.RunAsync` must: report password connection; open a password session; generate keys; execute `LinuxAuthorizedKeyCommand.Build(key.PublicKeyLine)`; dispose password session; report verification; open a key session; execute `true`; dispose it; return `new SetupResult(true, "SSH key setup succeeded.", key.PrivateKeyPath)`. Use `using` so either connection is closed on failures.

- [ ] **Step 4: Implement the production adapter.**

Add `IHostKeyConfirmation` with `bool ConfirmFirstUse(string endpoint, string sha256Fingerprint)`. `SshNetSessionFactory` accepts both `HostTrustService` and an `IHostKeyConfirmation`. It must create `ConnectionInfo` with `PasswordAuthenticationMethod` or `PrivateKeyAuthenticationMethod(new PrivateKeyFile(path))`, then create an `SshClient`. Handle `HostKeyReceived` by invoking `HostTrustService.Check($"{request.Host}:{request.Port}", e.HostKey, fingerprint => confirmation.ConfirmFirstUse($"{request.Host}:{request.Port}", fingerprint))` and assigning `e.CanTrust`. `ISshSession.Execute` runs `SshClient.RunCommand`, and throws a sanitized `InvalidOperationException` for nonzero `ExitStatus`; remove `request.Password` from any wrapped error text.

- [ ] **Step 5: Add a failing verification-failure test and make it pass.**

```csharp
[Fact]
public async Task RunAsync_ReturnsSafeFailureWhenKeyVerificationFails()
{
    var sessions = new FakeSshSessionFactory { ThrowOnKeyConnect = new InvalidOperationException("denied") };
    var result = await new KeySetupService(new FakeKeyMaterialFactory(new KeyMaterial("a", "b", "ssh-ed25519 AAA test")), sessions)
        .RunAsync(new SetupRequest("203.0.113.7", 22, "deploy", "password", "a"), new Progress<SetupProgress>(), CancellationToken.None);
    Assert.False(result.Succeeded);
    Assert.Contains("Key authentication verification failed", result.Message);
    Assert.DoesNotContain("password", result.Message, StringComparison.OrdinalIgnoreCase);
}
```

Catch expected SSH, socket, I/O, and invalid-operation errors in `KeySetupService`; return user-safe failure results. Let `OperationCanceledException` propagate.

- [ ] **Step 6: Confirm green.**

Run: `dotnet test tests/SshKeySetupTool.Tests/SshKeySetupTool.Tests.csproj --filter FullyQualifiedName~KeySetupServiceTests`

Expected: PASS.

### Task 6: Build the WinForms UI and validation controller

**Files:** Create `Application/SetupController.cs`, `UI/SetupForm.cs`; modify `Program.cs`; test `Application/SetupControllerTests.cs`.

**Produces:** the only UI: a masked password form, first-use fingerprint prompt, progress status, and Codex identity-file result.

- [ ] **Step 1: Write a failing controller test.**

```csharp
[Fact]
public async Task RunAsync_ReturnsValidationErrorsWithoutCallingRunner()
{
    var runner = new FakeRunner();
    var result = await new SetupController(runner).RunAsync(new SetupRequest("", 22, "", "", ""), new Progress<SetupProgress>(), CancellationToken.None);
    Assert.False(result.Succeeded);
    Assert.Equal(0, runner.CallCount);
    Assert.Contains("Server IP address is required.", result.Message);
}
```

- [ ] **Step 2: Confirm red.**

Run: `dotnet test tests/SshKeySetupTool.Tests/SshKeySetupTool.Tests.csproj --filter FullyQualifiedName~SetupControllerTests`

Expected: FAIL because `SetupController` is absent.

- [ ] **Step 3: Implement the controller.**

`SetupController.RunAsync` must call `SetupValidation.Validate`; on errors return `new SetupResult(false, string.Join(Environment.NewLine, errors))` and do not invoke the runner; otherwise delegate to `IKeySetupRunner`.

- [ ] **Step 4: Implement the form.**

Create labeled `TextBox` fields for IP address, port, account, password, and private-key path; a `Browse...` `SaveFileDialog`; a `Generate and deploy` button; and a read-only multiline status box. Defaults are port `22` and `Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh", "id_ed25519")`. Set `UseSystemPasswordChar = true`. `SetupForm` implements `IHostKeyConfirmation`: `ConfirmFirstUse` displays the endpoint and SHA-256 fingerprint in a Yes/No dialog. Add `SetController(SetupController controller)` so Program can finish composition after the form exists. During the asynchronous run disable the main button; append each `SetupProgress.Message`; in `finally`, clear the password field and re-enable the button. A changed fingerprint only displays its blocked error. Success shows the exact Codex identity-file path.

- [ ] **Step 5: Compose services in Program.cs.**

```csharp
using SshKeySetupTool.Application;
using SshKeySetupTool.Security;
using SshKeySetupTool.Ssh;
using SshKeySetupTool.UI;

ApplicationConfiguration.Initialize();
var form = new SetupForm();
var trust = new HostTrustService(new JsonKnownHostStore());
var sessions = new SshNetSessionFactory(trust, form);
var service = new KeySetupService(new OpenSshKeyMaterialFactory(), sessions);
form.SetController(new SetupController(service));
Application.Run(form);
```

- [ ] **Step 6: Confirm green.**

Run:

```powershell
dotnet test SshKeySetupTool.sln -c Release
dotnet build SshKeySetupTool.sln -c Release
```

Expected: all tests pass and build has no warnings.

### Task 7: Publish and verify the standalone executable

**Files:** Create `README.md`; modify `scripts/publish.ps1` only if packaging verification exposes an error.

**Produces:** `outputs/SshKeySetupTool.exe` and usage instructions.

- [ ] **Step 1: Write README usage.**

```markdown
1. Run `SshKeySetupTool.exe` on Windows.
2. Enter the Linux server IP address, port, account, and password.
3. Confirm the fingerprint only after comparing it with a trusted source.
4. After success, use the printed private-key path as Codex's **Identity File**.

The password is not saved. Keep the generated private key private.
```

- [ ] **Step 2: Publish.**

Run:

```powershell
.\scripts\publish.ps1
Get-Item .\outputs\SshKeySetupTool.exe | Select-Object FullName,Length
```

Expected: the executable exists and has nonzero length.

- [ ] **Step 3: Run the published executable.**

Run: `Start-Process -FilePath .\outputs\SshKeySetupTool.exe`

Expected: a window with masked password, port `22`, and the default `id_ed25519` path. Close it without submitting credentials.

- [ ] **Step 4: Final verification.**

Run:

```powershell
dotnet test SshKeySetupTool.sln -c Release
Get-ChildItem .\outputs | Select-Object Name,Length
```

Expected: all tests pass and `SshKeySetupTool.exe` exists. Do not test against a real server without the user's explicit credentials and authorization.
