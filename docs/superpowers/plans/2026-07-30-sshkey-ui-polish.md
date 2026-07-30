# SSHKEY Form Visual Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reflow the SSHKEY form to the approved three-part first row, equal credential row, and inline connection-status header without changing application behavior.

**Architecture:** Change only `Form1.Designer.cs` control bounds and add small layout-only panels where WinForms needs grouping. Leave `Form1.cs`, all services, events, status state, and existing control names untouched.

**Tech Stack:** .NET 8 Windows Forms, xUnit.

## Global Constraints

- Keep the custom title bar, 680-pixel form width, footer order, URLs, and current color semantics.
- Preserve all existing control names, event handlers, and business logic.
- First row is server IP, port, then OpenSSH; second row is equal-width username and password.
- Put the existing status text on the connection-details header row.

---

### Task 1: Reflow the form and lock it with layout tests

**Files:**
- Modify: `src/SshKeySetupTool/Form1.Designer.cs`
- Modify: `tests/SshKeySetupTool.Tests/Presentation/FormLayoutTests.cs`

**Interfaces:** The existing controls `hostTextBox`, `portTextBox`, `openSshButton`, `usernameTextBox`, `passwordTextBox`, `statusTextBox`, and `connectionDetailsTextBox` retain their names and behavior. New layout-only panels do not expose application behavior.

- [ ] **Step 1: Add failing layout assertions**

```csharp
Assert.Equal(host.Top, port.Top);
Assert.Equal(port.Top, openSsh.Top);
Assert.True(host.Right < port.Left);
Assert.True(port.Right < openSsh.Left);
Assert.Equal(username.Top, password.Top);
Assert.Equal(username.Width, password.Width);
Assert.Equal(connectionDetailsLabel.Top, status.Top);
Assert.True(status.Left > connectionDetailsLabel.Right);
Assert.True(connectionDetails.Top > connectionDetailsLabel.Bottom);
```

- [ ] **Step 2: Run the layout test and verify failure**

Run: `dotnet test tests/SshKeySetupTool.Tests/SshKeySetupTool.Tests.csproj --filter FullyQualifiedName~FormLayoutTests`

Expected: failure because OpenSSH is not in the first row and status is above the connection-details label.

- [ ] **Step 3: Make the minimal Designer-only layout changes**

Set first-row bounds to host `20,76,302,28`, port `336,76,76,28`, and OpenSSH `426,76,234,28`. Set username and password to equal widths at `20,132,313,28` and `347,132,313,28`. Move the status label/text box onto the connection-details header row, with the details text box below that row; preserve the existing footer coordinates and all `Click`/`LinkClicked` hookups.

- [ ] **Step 4: Run layout and full suite tests**

Run: `dotnet test SshKeySetupTool.sln`

Expected: all tests pass with no behavior-test changes.

- [ ] **Step 5: Commit**

Run: `git add src/SshKeySetupTool/Form1.Designer.cs tests/SshKeySetupTool.Tests/Presentation/FormLayoutTests.cs && git commit -m "style: polish SSHKEY form layout"`

## Plan self-review

- Scope coverage: Task 1 covers all approved visual changes and explicitly retains behavior.
- Completeness: the plan has no unfinished markers or deferred work.
- Type consistency: no new production types or behavior interfaces are introduced.
