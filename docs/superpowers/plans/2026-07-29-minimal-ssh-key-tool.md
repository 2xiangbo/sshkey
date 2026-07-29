# Minimal SSH Key Tool Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (\`- [ ]\`) syntax for tracking.

**Goal:** Build a small Windows executable that creates an SSH key pair, writes its public key to a Linux server using a supplied password, verifies key login, and displays the private-key path for Codex.

**Architecture:** Keep the existing validation, key generator, and Linux \`authorized_keys\` command builder. Add one orchestration service behind a small SSH client interface, so the password-to-key sequence is testable without a real server. The WinForms window only collects the five required values and invokes that service.

**Tech Stack:** .NET 8 WinForms, SSH.NET, BouncyCastle, xUnit, PowerShell \`dotnet publish\`.

## Global Constraints

- Passwords are held only in memory for the current click and are never persisted.
- Private and public key paths must not be overwritten.
- The server target is a Linux account with \`~/.ssh/authorized_keys\`.
- Do not add profiles, a server list, advanced settings, a persistent host-trust database, concurrent setup support, or SSH daemon configuration.
- Publish a self-contained, single-file \`win-x64\` executable.

---

## File Structure

- \`src/SshKeySetupTool/Ssh/ISshSetupClient.cs\`: testable SSH connection and command interface.
- \`src/SshKeySetupTool/Ssh/SshNetSetupClient.cs\`: SSH.NET password and private-key connection implementation.
- \`src/SshKeySetupTool/Services/KeySetupService.cs\`: validation, key creation, installation, and verification sequence.
- \`src/SshKeySetupTool/Form1.cs\`: minimal form controls and asynchronous button handler.
- \`src/SshKeySetupTool/Program.cs\`: composition root for the form and service.
- \`tests/SshKeySetupTool.Tests/Services/KeySetupServiceTests.cs\`: fake-client workflow tests.
- \`README.md\`: exact Codex \`IdentityFile\` usage.
- \`scripts/publish.ps1\`: self-contained release command.

### Task 1: SSH Setup Service

**Files:**
- Create: \`src/SshKeySetupTool/Ssh/ISshSetupClient.cs\`
- Create: \`src/SshKeySetupTool/Ssh/SshNetSetupClient.cs\`
- Create: \`src/SshKeySetupTool/Services/KeySetupService.cs\`
- Create: \`tests/SshKeySetupTool.Tests/Services/KeySetupServiceTests.cs\`

**Interfaces:**
- Consumes: \`SetupRequest\`, \`SetupValidation.Validate\`, \`IKeyMaterialFactory.Create\`, and \`LinuxAuthorizedKeyCommand.Build\`.
- Produces: \`Task<SetupResult> KeySetupService.RunAsync(SetupRequest, CancellationToken)\`; \`ISshSetupClient.ConnectWithPassword\`, \`ConnectWithPrivateKey\`, and \`Execute\`.

- [ ] **Step 1: Write failing tests for the required call order**

\`\`\`csharp
[Fact]
public async Task RunAsync_creates_keys_installs_public_key_then_verifies_private_key()
{
    var client = new FakeSshSetupClient();
    var service = new KeySetupService(new FakeKeyMaterialFactory(), client);

    var result = await service.RunAsync(
        new SetupRequest("203.0.113.10", 22, "root", "pw", "C:\\keys\\id_ed25519"),
        CancellationToken.None);

    Assert.True(result.Succeeded);
    Assert.Equal(new[] { "password", "command", "private-key" }, client.Calls);
    Assert.Contains("authorized_keys", client.Command);
    Assert.Equal("C:\\keys\\id_ed25519", result.PrivateKeyPath);
}
\`\`\`

- [ ] **Step 2: Run the targeted test and confirm red**

Run: \`dotnet test SshKeySetupTool.sln -c Release --filter FullyQualifiedName~KeySetupServiceTests\`

Expected: FAIL because \`KeySetupService\` and \`ISshSetupClient\` do not exist.

- [ ] **Step 3: Implement the smallest complete service**

\`\`\`csharp
public interface ISshSetupClient : IDisposable
{
    void ConnectWithPassword(SetupRequest request);
    void ConnectWithPrivateKey(SetupRequest request, string privateKeyPath);
    void Execute(string command);
}

public Task<SetupResult> RunAsync(SetupRequest request, CancellationToken cancellationToken)
{
    var errors = SetupValidation.Validate(request);
    if (errors.Count != 0) return Task.FromResult(new SetupResult(false, string.Join(Environment.NewLine, errors)));

    var key = _keyMaterialFactory.Create(request.PrivateKeyPath);
    _sshClient.ConnectWithPassword(request);
    _sshClient.Execute(LinuxAuthorizedKeyCommand.Build(key.PublicKeyLine));
    _sshClient.ConnectWithPrivateKey(request, key.PrivateKeyPath);
    return Task.FromResult(new SetupResult(true, "Ready for Codex.", key.PrivateKeyPath));
}
\`\`\`

The SSH.NET adapter opens a password connection, executes the command, disconnects it, then opens a separate private-key connection to verify authentication. It lets SSH.NET exceptions reach the UI as normal failure messages.

- [ ] **Step 4: Run focused and full tests**

Run: \`dotnet test SshKeySetupTool.sln -c Release\`

Expected: PASS, including validation, collision, command escaping, and service-sequence tests.

- [ ] **Step 5: Commit the service layer**

\`\`\`powershell
git add src/SshKeySetupTool/Ssh src/SshKeySetupTool/Services tests/SshKeySetupTool.Tests/Services
git commit -m "feat: add minimal SSH setup workflow"
\`\`\`

### Task 2: One-Button WinForms Window

**Files:**
- Modify: \`src/SshKeySetupTool/Form1.cs\`
- Modify: \`src/SshKeySetupTool/Form1.Designer.cs\`
- Modify: \`src/SshKeySetupTool/Program.cs\`
- Create: \`src/SshKeySetupTool/SetupFormInput.cs\`
- Create: \`tests/SshKeySetupTool.Tests/SetupFormInputTests.cs\`

**Interfaces:**
- Consumes: \`KeySetupService.RunAsync(SetupRequest, CancellationToken)\`.
- Produces: one form with IP, port, account, password, key path, status, and a \`Generate and write to server\` button.

- [ ] **Step 1: Write the failing request mapping test**

\`\`\`csharp
[Fact]
public void BuildRequest_uses_text_field_values()
{
    var request = SetupFormInput.BuildRequest("198.51.100.7", "2222", "admin", "pw", "C:\\keys\\id_ed25519");

    Assert.Equal(2222, request.Port);
    Assert.Equal("admin", request.Username);
}
\`\`\`

- [ ] **Step 2: Run the targeted test and confirm red**

Run: \`dotnet test SshKeySetupTool.sln -c Release --filter FullyQualifiedName~SetupFormInputTests\`

Expected: FAIL because \`SetupFormInput\` does not exist.

- [ ] **Step 3: Implement the narrow UI and handler**

\`\`\`csharp
private async void generateButton_Click(object? sender, EventArgs e)
{
    generateButton.Enabled = false;
    try
    {
        var result = await _keySetupService.RunAsync(BuildRequest(), CancellationToken.None);
        statusLabel.Text = result.Succeeded
            ? $"Ready for Codex. IdentityFile: {result.PrivateKeyPath}"
            : result.Message;
    }
    catch (Exception exception)
    {
        statusLabel.Text = $"Failed: {exception.Message}";
    }
    finally
    {
        passwordTextBox.Clear();
        generateButton.Enabled = true;
    }
}
\`\`\`

Set port to \`22\`, set the key path to \`Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh", "id_ed25519")\`, and set \`UseSystemPasswordChar = true\`. Do not save form values or password.

- [ ] **Step 4: Run tests and build**

Run: \`dotnet test SshKeySetupTool.sln -c Release; dotnet build SshKeySetupTool.sln -c Release\`

Expected: tests PASS and \`Build succeeded\`.

- [ ] **Step 5: Commit the window**

\`\`\`powershell
git add src/SshKeySetupTool/Form1.cs src/SshKeySetupTool/Form1.Designer.cs src/SshKeySetupTool/Program.cs src/SshKeySetupTool/SetupFormInput.cs tests/SshKeySetupTool.Tests/SetupFormInputTests.cs
git commit -m "feat: add one-button SSH key setup window"
\`\`\`

### Task 3: Publish and Verify the Executable

**Files:**
- Create: \`README.md\`
- Modify: \`scripts/publish.ps1\` only if the release output is not \`outputs/SshKeySetupTool.exe\`.

**Interfaces:**
- Consumes: the compiled WinForms project and \`scripts/publish.ps1\`.
- Produces: \`outputs/SshKeySetupTool.exe\` and concise Codex SSH config guidance.

- [ ] **Step 1: Add concise instructions**

\`\`\`markdown
Run \`powershell -ExecutionPolicy Bypass -File .\\scripts\\publish.ps1\`.
After a successful setup, use the displayed path in Codex SSH configuration:
\`IdentityFile C:\\Users\\your-name\\.ssh\\id_ed25519\`
\`\`\`

- [ ] **Step 2: Publish and launch the single-file executable**

Run: \`powershell -ExecutionPolicy Bypass -File .\\scripts\\publish.ps1\`

Expected: \`outputs\\SshKeySetupTool.exe\` exists.

Run: \`Start-Process .\\outputs\\SshKeySetupTool.exe\`

Expected: a desktop window opens with the five fields and one action button.

- [ ] **Step 3: Run final automated verification**

Run: \`dotnet test SshKeySetupTool.sln -c Release\`

Expected: PASS.

- [ ] **Step 4: Commit release support**

\`\`\`powershell
git add README.md scripts/publish.ps1
git commit -m "docs: add minimal SSH key tool release instructions"
\`\`\`

## Self-Review

- Spec coverage: Task 1 covers validation, no-overwrite key creation, password SSH, idempotent Linux key installation, and private-key verification. Task 2 covers the exact minimal UI, defaults, password clearing, and Codex path display. Task 3 covers publishing and launch verification.
- Placeholder scan: no \`TODO\`, \`TBD\`, or deferred implementation markers are present.
- Type consistency: \`SetupRequest\`, \`SetupResult\`, \`IKeyMaterialFactory\`, \`ISshSetupClient\`, and \`KeySetupService.RunAsync\` use the same names and signatures throughout.
