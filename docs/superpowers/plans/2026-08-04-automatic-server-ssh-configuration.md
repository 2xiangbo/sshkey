# Automatic Server SSH Configuration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Detect disabled server-side SSH public-key authentication, obtain explicit consent for a root-only repair, apply a reversible SSH daemon configuration change, and complete private-key verification automatically.

**Architecture:** Pure domain records and a Linux command builder define the server configuration protocol. `WindowsOpenSshSetupClient` executes those commands through the existing password askpass and pinned-host path, while `KeySetupService` owns consent, sequencing, commit, and rollback. `Form1` maps setup phases and known failure kinds to localized UI copy.

**Tech Stack:** .NET 8, C# 12, WinForms, Windows OpenSSH, POSIX shell on the remote Linux host, xUnit, optional Docker shell verification.

## Global Constraints

- Automatic server mutation is permitted only when `SetupRequest.Username` is exactly `root`.
- Non-root users continue normally when public-key authentication is already enabled.
- Change only `PubkeyAuthentication yes`. Do not install server packages, change `PermitRootLogin`, disable password login, add `sudo` support, or alter SELinux policy.
- Keep host-fingerprint confirmation and host-key pinning on every remote operation.
- Never persist the password, place it in process arguments, or expose it in errors.
- Prefer `/etc/ssh/sshd_config.d/00-sshkey-setup-tool.conf`, then use a backed-up main-config prepend only when the drop-in is unavailable or ineffective.
- Run `sshd -t`, verify `sshd -T` reports `pubkeyauthentication yes`, and reload SSH before reporting an applied change.
- Roll back with a fresh bounded cleanup token after cancellation or any post-apply failure.
- Do not overwrite an unrecognized managed-drop-in file.
- Preserve the existing untracked `artifacts/` directory.

## File Structure

- Modify `src/SshKeySetupTool/Domain/SetupRequest.cs`: phase and failure enums used across service and UI.
- Create `src/SshKeySetupTool/Ssh/SshServerConfiguration.cs`: probe, strategy, transaction, and typed SSH operation exception.
- Create `src/SshKeySetupTool/Ssh/LinuxSshServerConfigurationCommand.cs`: probe/apply/commit/rollback command protocol.
- Modify `src/SshKeySetupTool/Ssh/ISshSetupClient.cs`: server configuration operations.
- Modify `src/SshKeySetupTool/Ssh/WindowsOpenSshSetupClient.cs`: pinned password-command execution and structured parsing.
- Modify `src/SshKeySetupTool/Services/KeySetupService.cs`: consent, progress, root-only policy, commit, and rollback.
- Modify `src/SshKeySetupTool/Form1.cs` and `src/SshKeySetupTool/Presentation/UiLanguage.cs`: localized confirmation, phases, and failures.
- Create `tests/SshKeySetupTool.Tests/Ssh/LinuxSshServerConfigurationCommandTests.cs`.
- Modify `tests/SshKeySetupTool.Tests/Ssh/WindowsOpenSshSetupClientTests.cs`.
- Modify `tests/SshKeySetupTool.Tests/Services/KeySetupServiceTests.cs`.
- Modify `tests/SshKeySetupTool.Tests/Presentation/FormLifecycleTests.cs` and `tests/SshKeySetupTool.Tests/Presentation/UiLanguageTests.cs`.
- Modify `README.md`: document automatic repair and its root-only safety boundary.

---

### Task 1: Define the configuration protocol and command builder

**Files:**
- Modify: `src/SshKeySetupTool/Domain/SetupRequest.cs`
- Create: `src/SshKeySetupTool/Ssh/SshServerConfiguration.cs`
- Create: `src/SshKeySetupTool/Ssh/LinuxSshServerConfigurationCommand.cs`
- Create: `tests/SshKeySetupTool.Tests/Ssh/LinuxSshServerConfigurationCommandTests.cs`

**Interfaces:**
- Produces: `SetupPhase`, `SetupFailureKind`, and `SetupProgress(SetupPhase Phase)`.
- Produces: `SshPublicKeyAuthenticationState`, `SshServerConfigurationProbe.Parse(string)`, `SshServerConfigurationStrategy`, `SshServerConfigurationChange`, and `SshSetupOperationException`.
- Produces: `LinuxSshServerConfigurationCommand.BuildProbe()`, `BuildApply(string)`, `ParseApplyResult(string, string)`, `BuildCommit(SshServerConfigurationChange)`, and `BuildRollback(SshServerConfigurationChange)`.

- [ ] **Step 1: Write failing protocol and command tests**

Create the test file with these core cases:

```csharp
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
}
```

Add focused assertions that `BuildCommit` deletes only the backup derived from the transaction id, `BuildRollback` removes a newly created drop-in or restores the recorded backup, and malformed apply output throws `SshSetupOperationException` with `SetupFailureKind.ServerConfigurationApply`.

- [ ] **Step 2: Run the focused test and confirm RED**

Run:

```powershell
dotnet test .\tests\SshKeySetupTool.Tests\SshKeySetupTool.Tests.csproj -c Release --filter FullyQualifiedName~LinuxSshServerConfigurationCommandTests
```

Expected: FAIL because the configuration types and command builder do not exist.

- [ ] **Step 3: Add the domain and SSH configuration records**

Update `SetupRequest.cs` without changing the existing request constructor:

```csharp
public enum SetupPhase
{
    GeneratingKey,
    DiscoveringServer,
    CheckingServerConfiguration,
    WaitingForServerConfigurationConsent,
    EnablingServerConfiguration,
    InstallingPublicKey,
    VerifyingPrivateKey,
    RollingBackServerConfiguration
}

public enum SetupFailureKind
{
    None,
    Validation,
    ServerConfigurationInspection,
    ServerConfigurationRootRequired,
    ServerConfigurationDeclined,
    ServerConfigurationApply,
    PublicKeyInstallation,
    PrivateKeyVerification,
    Rollback
}

public sealed record SetupProgress(SetupPhase Phase);

public sealed record SetupResult(
    bool Succeeded,
    string Message,
    string? PrivateKeyPath = null,
    SetupFailureKind FailureKind = SetupFailureKind.None);
```

Create `SshServerConfiguration.cs` with this public surface:

```csharp
using SshKeySetupTool.Domain;

namespace SshKeySetupTool.Ssh;

public enum SshPublicKeyAuthenticationState
{
    Enabled,
    Disabled,
    Unavailable
}

public sealed record SshServerConfigurationProbe(
    SshPublicKeyAuthenticationState State,
    string RawOutput)
{
    public static SshServerConfigurationProbe Parse(string output);
}

public enum SshServerConfigurationStrategy
{
    ManagedDropIn,
    MainConfiguration
}

public sealed record SshServerConfigurationChange(
    string OperationId,
    SshServerConfigurationStrategy Strategy,
    bool HadExistingManagedDropIn);

public sealed class SshSetupOperationException : InvalidOperationException
{
    public SshSetupOperationException(
        SetupFailureKind failureKind,
        string message,
        Exception? innerException = null);

    public SetupFailureKind FailureKind { get; }
}
```

`Parse` must use ordinal, whitespace-tolerant line matching for exactly one `pubkeyauthentication yes` or `pubkeyauthentication no` directive. Conflicting, absent, or malformed lines return `Unavailable` and preserve the raw output.

- [ ] **Step 4: Implement the complete remote command protocol**

Create `LinuxSshServerConfigurationCommand.cs` with constants for:

```csharp
internal const string MainConfigurationPath = "/etc/ssh/sshd_config";
internal const string ManagedDropInPath =
    "/etc/ssh/sshd_config.d/00-sshkey-setup-tool.conf";
internal const string ManagedMarker =
    "# Managed by SSHKEY. Do not edit while setup is running.";
```

Every public builder must validate that `operationId` is exactly 32 lowercase hexadecimal characters. `BuildProbe` resolves `sshd` with `command -v sshd` and then `/usr/sbin/sshd`, runs `"$sshd" -T`, and prints only the matching `pubkeyauthentication` line.

`BuildApply` must emit a POSIX `sh` script with these exact behaviors:

```sh
set -eu
main_config='/etc/ssh/sshd_config'
managed_config='/etc/ssh/sshd_config.d/00-sshkey-setup-tool.conf'
marker='# Managed by SSHKEY. Do not edit while setup is running.'
backup="/etc/ssh/sshd_config.sshkey-setup-$operation_id.bak"
sshd="$(command -v sshd 2>/dev/null || true)"
[ -n "$sshd" ] || [ ! -x /usr/sbin/sshd ] || sshd=/usr/sbin/sshd
[ -n "$sshd" ] || { printf '%s\n' 'SSHKEY_ERROR sshd-not-found'; exit 41; }
is_enabled() {
  "$sshd" -T 2>/dev/null |
    awk 'tolower($1) == "pubkeyauthentication" { print tolower($2); exit }' |
    grep -qx yes
}
reload_sshd() {
  if command -v systemctl >/dev/null 2>&1; then
    systemctl reload sshd 2>/dev/null || systemctl reload ssh
  elif command -v service >/dev/null 2>&1; then
    service sshd reload 2>/dev/null || service ssh reload
  else
    return 1
  fi
}
```

The builder puts `operation_id='__OPERATION_ID__'` immediately after the
variable declarations and replaces the literal token `__OPERATION_ID__` with
the validated lowercase id before returning the command. No path is read from
remote command output.

The remainder of the same script must execute this fixed sequence:

1. Try the managed path only when its directory exists and the file is absent or its first line equals `$marker`.
2. Back up a recognized existing managed file with `cp -a`; create the replacement through `mktemp`, `cat`, `chown root:root`, `chmod 600`, and an atomic `mv`.
3. Accept the drop-in only when `"$sshd" -t` and `is_enabled` both succeed. Restore/remove it before main-file fallback when either check fails.
4. Back up the main configuration with `cp -a`, build the prepended content in
   a same-directory temporary file, then write that content through the active
   file's existing inode. This preserves its owner, mode, ACLs, and extended
   attributes; restore the `cp -a` backup if the write is incomplete.
5. On validation or reload failure, restore the changed file before exiting nonzero.
6. Print exactly one success sentinel: `SSHKEY_APPLIED drop-in-new`, `SSHKEY_APPLIED drop-in-existing`, or `SSHKEY_APPLIED main`.

`BuildCommit` removes only the transaction backup. `BuildRollback` restores the backed-up existing drop-in or main file, removes a newly created drop-in, runs `sshd -t`, and reloads SSH. Neither command may accept a remote path from process output; derive all paths from the validated local operation id and strategy.

- [ ] **Step 5: Run focused tests and confirm GREEN**

Run:

```powershell
dotnet test .\tests\SshKeySetupTool.Tests\SshKeySetupTool.Tests.csproj -c Release --filter FullyQualifiedName~LinuxSshServerConfigurationCommandTests
```

Expected: PASS.

- [ ] **Step 6: Commit the protocol layer**

```powershell
git add src/SshKeySetupTool/Domain/SetupRequest.cs src/SshKeySetupTool/Ssh/SshServerConfiguration.cs src/SshKeySetupTool/Ssh/LinuxSshServerConfigurationCommand.cs tests/SshKeySetupTool.Tests/Ssh/LinuxSshServerConfigurationCommandTests.cs
git commit -m "feat: define reversible SSH server configuration protocol"
```

### Task 2: Execute server configuration through pinned Windows OpenSSH

**Files:**
- Modify: `src/SshKeySetupTool/Ssh/ISshSetupClient.cs`
- Modify: `src/SshKeySetupTool/Ssh/WindowsOpenSshSetupClient.cs`
- Modify: `tests/SshKeySetupTool.Tests/Ssh/WindowsOpenSshSetupClientTests.cs`

**Interfaces:**
- Consumes: all types and command builders from Task 1.
- Produces: four new `ISshSetupClient` methods for inspect, enable, commit, and rollback.
- Produces: password-redacted `SshSetupOperationException` failures tagged with `SetupFailureKind`.

- [ ] **Step 1: Add failing client tests**

Add these tests beside the current pinned-host workflow tests:

```csharp
[Fact]
public async Task InspectServerConfigurationAsync_UsesPinnedPasswordSshAndParsesDisabled()
{
    var runner = new RecordingProcessRunner(startInfo =>
    {
        AssertPinnedPasswordCommand(startInfo);
        Assert.Contains(
            LinuxSshServerConfigurationCommand.BuildProbe(),
            startInfo.ArgumentList);
        return new ProcessResult(0, "pubkeyauthentication no\n", "");
    });
    var client = CreateClient(runner);
    var request = CreateRequest("root");
    var hostKey = CreateApprovedHostKey();

    var probe = await client.InspectServerConfigurationAsync(
        request, hostKey, CancellationToken.None);

    Assert.Equal(SshPublicKeyAuthenticationState.Disabled, probe.State);
}

[Fact]
public async Task EnablePublicKeyAuthenticationAsync_ParsesOnlyOwnedSentinel()
{
    var runner = new RecordingProcessRunner(_ =>
        new ProcessResult(0, "SSHKEY_APPLIED drop-in-new\n", ""));
    var client = CreateClient(runner);
    var request = CreateRequest("root");

    var change = await client.EnablePublicKeyAuthenticationAsync(
        request, CreateApprovedHostKey(), CancellationToken.None);

    Assert.Equal(SshServerConfigurationStrategy.ManagedDropIn, change.Strategy);
    Assert.False(change.HadExistingManagedDropIn);
    Assert.Matches("^[0-9a-f]{32}$", change.OperationId);
}

[Fact]
public async Task RollbackServerConfigurationAsync_UsesPasswordAfterCancellation()
{
    var observedToken = default(CancellationToken);
    var runner = new RecordingProcessRunner(
        _ => new ProcessResult(0, "", ""),
        token => observedToken = token);
    var client = CreateClient(runner);
    var change = new SshServerConfigurationChange(
        "0123456789abcdef0123456789abcdef",
        SshServerConfigurationStrategy.MainConfiguration,
        false);

    await client.RollbackServerConfigurationAsync(
        CreateRequest("root"),
        CreateApprovedHostKey(),
        change,
        CancellationToken.None);

    Assert.False(observedToken.IsCancellationRequested);
}
```

Add cases for commit command selection, malformed apply output, nonzero inspect/apply/commit/rollback results, host-key pinning, password environment cleanup, and password redaction. Each failure test asserts the exact `SetupFailureKind`.

- [ ] **Step 2: Run client tests and confirm RED**

Run:

```powershell
dotnet test .\tests\SshKeySetupTool.Tests\SshKeySetupTool.Tests.csproj -c Release --filter FullyQualifiedName~WindowsOpenSshSetupClientTests
```

Expected: FAIL because `ISshSetupClient` lacks the new methods.

- [ ] **Step 3: Extend the SSH client contract**

Add these signatures to `ISshSetupClient`:

```csharp
Task<SshServerConfigurationProbe> InspectServerConfigurationAsync(
    SetupRequest request,
    OpenSshHostKey approvedHostKey,
    CancellationToken cancellationToken);

Task<SshServerConfigurationChange> EnablePublicKeyAuthenticationAsync(
    SetupRequest request,
    OpenSshHostKey approvedHostKey,
    CancellationToken cancellationToken);

Task CommitServerConfigurationAsync(
    SetupRequest request,
    OpenSshHostKey approvedHostKey,
    SshServerConfigurationChange change,
    CancellationToken cancellationToken);

Task RollbackServerConfigurationAsync(
    SetupRequest request,
    OpenSshHostKey approvedHostKey,
    SshServerConfigurationChange change,
    CancellationToken cancellationToken);
```

- [ ] **Step 4: Refactor pinned password execution and implement the methods**

Extract the common password path from `InstallPublicKeyAsync` into:

```csharp
private async Task<ProcessResult> RunPinnedPasswordCommandAsync(
    SetupRequest request,
    OpenSshHostKey approvedHostKey,
    string command,
    SetupFailureKind failureKind,
    string failureMessage,
    CancellationToken cancellationToken);
```

It must create and delete the temporary known-hosts file, invoke `CreatePasswordStartInfo`, return stdout on exit code `0`, and call a typed `CreateFailure` on nonzero exit. Keep `CreatePrivateKeyStartInfo` password-free.

Implement the four methods as follows:

```csharp
public async Task<SshServerConfigurationProbe> InspectServerConfigurationAsync(
    SetupRequest request,
    OpenSshHostKey approvedHostKey,
    CancellationToken cancellationToken)
{
    var result = await RunPinnedPasswordCommandAsync(
        request,
        approvedHostKey,
        LinuxSshServerConfigurationCommand.BuildProbe(),
        SetupFailureKind.ServerConfigurationInspection,
        "SSH server configuration inspection failed",
        cancellationToken);
    return SshServerConfigurationProbe.Parse(result.StandardOutput);
}

public async Task<SshServerConfigurationChange> EnablePublicKeyAuthenticationAsync(
    SetupRequest request,
    OpenSshHostKey approvedHostKey,
    CancellationToken cancellationToken)
{
    var operationId = Guid.NewGuid().ToString("N");
    var result = await RunPinnedPasswordCommandAsync(
        request,
        approvedHostKey,
        LinuxSshServerConfigurationCommand.BuildApply(operationId),
        SetupFailureKind.ServerConfigurationApply,
        "SSH public-key authentication repair failed",
        cancellationToken);
    return LinuxSshServerConfigurationCommand.ParseApplyResult(
        operationId,
        result.StandardOutput);
}
```

`CommitServerConfigurationAsync` and `RollbackServerConfigurationAsync` call the matching builder with `SetupFailureKind.ServerConfigurationApply` and `SetupFailureKind.Rollback` respectively. Update public-key installation and private-key verification failures to use `PublicKeyInstallation` and `PrivateKeyVerification`.

Update `CreateFailure` to return `SshSetupOperationException` and redact `request.Password` from combined stderr/stdout before constructing the exception. The password environment entry must still be removed by `ProcessRunner` immediately after process start.

- [ ] **Step 5: Run client tests and confirm GREEN**

Run:

```powershell
dotnet test .\tests\SshKeySetupTool.Tests\SshKeySetupTool.Tests.csproj -c Release --filter FullyQualifiedName~WindowsOpenSshSetupClientTests
```

Expected: PASS, including all pre-existing host discovery and password-redaction tests.

- [ ] **Step 6: Commit the client adapter**

```powershell
git add src/SshKeySetupTool/Ssh/ISshSetupClient.cs src/SshKeySetupTool/Ssh/WindowsOpenSshSetupClient.cs tests/SshKeySetupTool.Tests/Ssh/WindowsOpenSshSetupClientTests.cs
git commit -m "feat: execute SSH server configuration transactions"
```

### Task 3: Orchestrate consent, commit, and rollback

**Files:**
- Modify: `src/SshKeySetupTool/Services/KeySetupService.cs`
- Modify: `tests/SshKeySetupTool.Tests/Services/KeySetupServiceTests.cs`
- Modify: `tests/SshKeySetupTool.Tests/Presentation/FormLifecycleTests.cs` for the changed service interface fakes

**Interfaces:**
- Consumes: the extended `ISshSetupClient` and Task 1 domain types.
- Produces: `IKeySetupService.RunAsync(SetupRequest, Func<SetupRequest, SshServerConfigurationProbe, bool>, IProgress<SetupProgress>?, CancellationToken)`.
- Produces: a bounded 45-second rollback cleanup path independent of the user cancellation token.

- [ ] **Step 1: Write failing service transaction tests**

Replace the happy-path fake with an `ISshSetupClient` fake that records every operation. Add this primary case:

```csharp
[Fact]
public async Task RunAsync_DisabledRootWithConsent_RepairsBeforeInstallAndCommits()
{
    var sshClient = new FakeSshSetupClient
    {
        Probe = new(
            SshPublicKeyAuthenticationState.Disabled,
            "pubkeyauthentication no\n")
    };
    var phases = new List<SetupPhase>();
    var progress = new InlineProgress<SetupProgress>(
        value => phases.Add(value.Phase));
    var service = new KeySetupService(new FakeKeyMaterialFactory(), sshClient);
    var request = new SetupRequest(
        "203.0.113.10", 22, "root", "pw", @"C:\keys\id_ed25519");

    var result = await service.RunAsync(
        request,
        (_, probe) => probe.State == SshPublicKeyAuthenticationState.Disabled,
        progress,
        CancellationToken.None);

    Assert.True(result.Succeeded);
    Assert.Equal(
        ["host", "inspect", "apply", "install", "verify", "commit"],
        sshClient.Calls);
    Assert.Contains(SetupPhase.WaitingForServerConfigurationConsent, phases);
    Assert.Contains(SetupPhase.EnablingServerConfiguration, phases);
}
```

Add these separate tests with exact call assertions:

- Enabled probe: `["host", "inspect", "install", "verify"]`; consent delegate must not run.
- Disabled root declined: `["host", "inspect"]` and result kind `ServerConfigurationDeclined`.
- Disabled non-root: `["host", "inspect"]` and result kind `ServerConfigurationRootRequired`.
- Unavailable probe: no mutation and result kind `ServerConfigurationInspection`.
- Verification failure after apply: `rollback` occurs and `commit` does not.
- Cancellation after apply: rollback receives a token that is not canceled, then the original `OperationCanceledException` propagates.
- Rollback failure: result kind `Rollback`, message contains the derived backup path and both the original and rollback failure messages.
- Success: commit runs only after private-key verification.
- Validation failure: no key creation, no SSH call, and result kind `Validation`.

Define `InlineProgress<T> : IProgress<T>` in the test file so phase assertions run synchronously.

- [ ] **Step 2: Run service tests and confirm RED**

Run:

```powershell
dotnet test .\tests\SshKeySetupTool.Tests\SshKeySetupTool.Tests.csproj -c Release --filter FullyQualifiedName~KeySetupServiceTests
```

Expected: FAIL because the service has no inspection, consent, progress, or transaction behavior.

- [ ] **Step 3: Change the service interface and update test fakes**

Change `IKeySetupService` to:

```csharp
Task<SetupResult> RunAsync(
    SetupRequest request,
    Func<SetupRequest, SshServerConfigurationProbe, bool>
        confirmServerConfiguration,
    IProgress<SetupProgress>? progress,
    CancellationToken cancellationToken);
```

Update `ImmediateSetupService` and `BlockingSetupService` in `FormLifecycleTests.cs` to accept the two new parameters while preserving their current cancellation behavior. Do not add UI behavior in this step.

- [ ] **Step 4: Implement the transactional workflow**

Implement this sequence in `KeySetupService.RunAsync`:

```csharp
var errors = SetupValidation.Validate(request);
if (errors.Count > 0)
{
    return new SetupResult(
        false,
        string.Join(Environment.NewLine, errors),
        FailureKind: SetupFailureKind.Validation);
}

progress?.Report(new(SetupPhase.GeneratingKey));
var keyMaterial = _keyMaterialFactory.Create(request.PrivateKeyPath);

progress?.Report(new(SetupPhase.DiscoveringServer));
var approvedHostKey = await _sshClient.ApproveHostKeyAsync(
    request,
    cancellationToken);

progress?.Report(new(SetupPhase.CheckingServerConfiguration));
var probe = await _sshClient.InspectServerConfigurationAsync(
    request,
    approvedHostKey,
    cancellationToken);
```

For `Disabled`, require ordinal username equality with `root`, report `WaitingForServerConfigurationConsent`, call the delegate once, and return the typed declined result when it returns false. For `Unavailable` return the inspection failure without calling consent.

After consent, report `EnablingServerConfiguration` and store the returned transaction. Report `InstallingPublicKey` before the existing authorized-key command and `VerifyingPrivateKey` before key verification. Commit only after successful verification.

Wrap all work after apply in `try/catch`. On `SshSetupOperationException`, roll back and return its failure kind. On `OperationCanceledException`, roll back and rethrow. Create the cleanup token exactly as:

```csharp
using var rollbackTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
```

If rollback throws, return `SetupFailureKind.Rollback` with a message containing the transaction's derived remote backup path and both failure messages. Do not call commit after rollback.

- [ ] **Step 5: Run service and form lifecycle tests and confirm GREEN**

Run:

```powershell
dotnet test .\tests\SshKeySetupTool.Tests\SshKeySetupTool.Tests.csproj -c Release --filter "FullyQualifiedName~KeySetupServiceTests|FullyQualifiedName~FormLifecycleTests"
```

Expected: PASS.

- [ ] **Step 6: Commit orchestration**

```powershell
git add src/SshKeySetupTool/Services/KeySetupService.cs tests/SshKeySetupTool.Tests/Services/KeySetupServiceTests.cs tests/SshKeySetupTool.Tests/Presentation/FormLifecycleTests.cs
git commit -m "feat: orchestrate SSH configuration repair and rollback"
```

### Task 4: Add localized consent, phase status, and failure presentation

**Files:**
- Modify: `src/SshKeySetupTool/Form1.cs`
- Modify: `src/SshKeySetupTool/Presentation/UiLanguage.cs`
- Modify: `tests/SshKeySetupTool.Tests/Presentation/FormLifecycleTests.cs`
- Modify: `tests/SshKeySetupTool.Tests/Presentation/UiLanguageTests.cs`

**Interfaces:**
- Consumes: `SetupProgress`, `SetupFailureKind`, and the expanded `IKeySetupService.RunAsync`.
- Produces: `Form1.ConfirmServerConfiguration`, `HandleSetupProgress`, and localized failure formatting.
- No `Form1.Designer.cs` layout change is required; confirmation uses a modal `MessageBox`.

- [ ] **Step 1: Write failing localization and progress tests**

Extend `UiLanguageTests`:

```csharp
[Fact]
public void Catalogs_ContainServerRepairConsentAndPhaseCopy()
{
    var english = UiTextCatalog.For(UiLanguage.English);
    var chinese = UiTextCatalog.For(UiLanguage.Chinese);

    Assert.Equal(
        "Enable SSH public-key authentication",
        english.ConfirmServerConfigurationTitle);
    Assert.Contains(
        "PubkeyAuthentication no",
        english.ConfirmServerConfigurationMessageFormat);
    Assert.Contains(
        "PubkeyAuthentication no",
        chinese.ConfirmServerConfigurationMessageFormat);
    Assert.NotEmpty(english.CheckingServerConfiguration);
    Assert.NotEmpty(chinese.RollingBackServerConfiguration);
    Assert.NotEqual(
        english.CheckingServerConfiguration,
        english.EnablingServerConfiguration);
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
```

Add a `ProgressReportingSetupService` to `FormLifecycleTests` that reports `CheckingServerConfiguration` and returns only after the form has processed the update. Assert the status text changes in Chinese, switch the language combo to `EN`, and assert the same active phase is re-rendered in English.

- [ ] **Step 2: Run presentation tests and confirm RED**

Run:

```powershell
dotnet test .\tests\SshKeySetupTool.Tests\SshKeySetupTool.Tests.csproj -c Release --filter "FullyQualifiedName~UiLanguageTests|FullyQualifiedName~FormLifecycleTests"
```

Expected: FAIL because the catalog, service call, and form progress handler do not exist.

- [ ] **Step 3: Extend the localized text catalog**

Add these `UiText` fields and fill both catalogs:

```csharp
string ConfirmServerConfigurationTitle,
string ConfirmServerConfigurationMessageFormat,
string CheckingServerConfiguration,
string WaitingForServerConfigurationConsent,
string EnablingServerConfiguration,
string InstallingPublicKey,
string VerifyingPrivateKey,
string RollingBackServerConfiguration
```

The consent message format must include placeholders `{0}` for host and `{1}` for the detected setting, state that SSHKEY will validate and reload SSH, state that password login is not disabled, and state that a failed setup is rolled back. Add:

```csharp
public string FailureLabel(SetupFailureKind kind) => kind switch
{
    SetupFailureKind.ServerConfigurationInspection =>
        "Server SSH configuration check failed",
    SetupFailureKind.ServerConfigurationRootRequired =>
        "Automatic repair requires the root account",
    SetupFailureKind.ServerConfigurationDeclined =>
        "Server SSH repair was cancelled",
    SetupFailureKind.ServerConfigurationApply =>
        "Server SSH configuration repair failed",
    SetupFailureKind.PublicKeyInstallation =>
        "Public-key installation failed",
    SetupFailureKind.PrivateKeyVerification =>
        "Private-key verification failed",
    SetupFailureKind.Rollback =>
        "SSH configuration rollback failed; manual recovery may be required",
    _ => FailedPrefix
};
```

Use these exact English labels: `Server SSH configuration check failed`, `Automatic repair requires the root account`, `Server SSH repair was cancelled`, `Server SSH configuration repair failed`, `Public-key installation failed`, `Private-key verification failed`, and `SSH configuration rollback failed; manual recovery may be required`. Use these exact Chinese labels: `服务器 SSH 配置检测失败`, `自动修复需要 root 账号`, `服务器 SSH 修复已取消`, `服务器 SSH 配置修复失败`, `公钥写入失败`, `私钥验证失败`, and `SSH 配置回滚失败，可能需要手动恢复`. Keep all current text unchanged.

- [ ] **Step 4: Wire consent and progress into Form1**

Change the service call in `RunSetupAsync` to:

```csharp
var progress = new Progress<SetupProgress>(HandleSetupProgress);
var result = await _keySetupService.RunAsync(
    request,
    ConfirmServerConfiguration,
    progress,
    cancellationToken);
```

Implement `ConfirmServerConfiguration` with the same `InvokeRequired` guard as `ConfirmHostKey` and show:

```csharp
MessageBox.Show(
    this,
    string.Format(
        CurrentText.ConfirmServerConfigurationMessageFormat,
        request.Host,
        probe.RawOutput.Trim()),
    CurrentText.ConfirmServerConfigurationTitle,
    MessageBoxButtons.OKCancel,
    MessageBoxIcon.Warning,
    MessageBoxDefaultButton.Button2) == DialogResult.OK
```

Store the latest `SetupPhase?` so language switching can re-render an in-progress phase. `HandleSetupProgress` maps each phase to the corresponding `UiText` field and uses `WorkingColor` except rollback, which uses `ErrorColor`.

When `SetupResult.Succeeded` is false, format the status as `CurrentText.FailureLabel(result.FailureKind)` followed by `result.Message` on the second line when details are nonempty. Keep clipboard behavior and password clearing unchanged.

- [ ] **Step 5: Run presentation and complete tests**

Run:

```powershell
dotnet test .\tests\SshKeySetupTool.Tests\SshKeySetupTool.Tests.csproj -c Release --filter "FullyQualifiedName~UiLanguageTests|FullyQualifiedName~FormLifecycleTests|FullyQualifiedName~FormLayoutTests"
dotnet test .\SshKeySetupTool.sln -c Release
```

Expected: all tests PASS and the form layout assertions remain unchanged.

- [ ] **Step 6: Commit the UI behavior**

```powershell
git add src/SshKeySetupTool/Form1.cs src/SshKeySetupTool/Presentation/UiLanguage.cs tests/SshKeySetupTool.Tests/Presentation/FormLifecycleTests.cs tests/SshKeySetupTool.Tests/Presentation/UiLanguageTests.cs
git commit -m "feat: confirm and report automatic SSH repair"
```

### Task 5: Document and verify the complete workflow

**Files:**
- Modify: `README.md`
- Test: `SshKeySetupTool.sln`
- Verify: `scripts/publish.ps1` and the generated standalone executable

**Interfaces:**
- Consumes: the complete workflow from Tasks 1-4.
- Produces: bilingual user documentation and release evidence.

- [ ] **Step 1: Update README behavior and safety notes**

In both English and Chinese sections, add:

- Servers with `PubkeyAuthentication no` trigger an explicit repair prompt only for `root`.
- SSHKEY enables only public-key authentication, validates `sshd` configuration, reloads the service, and rolls back on a failed setup.
- Password authentication and `PermitRootLogin` are not changed.
- Non-root users receive an administrator-required message instead of an automatic mutation.

Update the usage list so the repair confirmation appears after host-fingerprint confirmation and before successful connection details.

- [ ] **Step 2: Run the complete automated suite**

Run:

```powershell
dotnet test .\SshKeySetupTool.sln -c Release
```

Expected: all tests PASS with zero failed tests. Docker-dependent POSIX tests may report skipped only when their already-required local image is unavailable.

- [ ] **Step 3: Publish the standalone executable**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish.ps1
Get-Item .\outputs\SshKeySetupTool.exe | Select-Object FullName,Length,LastWriteTime
```

Expected: publish exits `0` and `outputs\SshKeySetupTool.exe` exists with nonzero length.

- [ ] **Step 4: Perform authorized disposable-host acceptance when available**

Use only a disposable Linux SSH host explicitly authorized for mutation. Record these four outcomes in the implementation handoff:

1. With `PubkeyAuthentication yes`, no repair prompt appears and setup succeeds.
2. With `PubkeyAuthentication no` and root consent, the prompt appears once, the effective setting becomes `yes`, and setup succeeds.
3. Declining the prompt leaves both effective configuration and configuration file checksums unchanged.
4. Forcing private-key verification to fail after repair restores the original effective setting and file checksum.

If no authorized disposable host is available, state `Disposable Linux SSH acceptance not run` in the handoff. Do not substitute the user's production server without explicit authorization.

- [ ] **Step 5: Review the final diff and commit documentation**

Run:

```powershell
git diff --check
git status --short
```

Expected: no whitespace errors; only intended source, test, and documentation
changes are tracked. Generated `artifacts/` and `outputs/` remain untracked or
ignored and must not be staged.

```powershell
git add README.md
git commit -m "docs: explain automatic SSH server repair"
```

- [ ] **Step 6: Capture final verification evidence**

Run:

```powershell
git log -5 --oneline
git status --short
```

Expected: the five feature commits are present; `artifacts/` may remain untracked and no implementation file is unexpectedly modified.
