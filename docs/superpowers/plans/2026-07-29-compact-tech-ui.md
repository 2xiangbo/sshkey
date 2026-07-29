# Compact Tech UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the version 6 WinForms presentation with a compact 680 x 520 space-black and cyan interface while preserving all SSH setup behavior.

**Architecture:** Keep the existing `Form1` and control names so the current workflow remains intact. `Form1.Designer.cs` owns the fixed visual layout and custom title-bar controls; `Form1.cs` owns window interactions and status-tone changes. Presentation tests instantiate the real form on an STA thread and verify the visual contract.

**Tech Stack:** .NET 8, Windows Forms, xUnit, Windows x64 self-contained single-file publishing.

## Global Constraints

- Fixed client size: 680 x 520 pixels.
- Borderless window with a custom 42-pixel draggable title bar.
- Main background: `#0B1118`; no gradients or default Windows button styling.
- Cyan accent: `#38D7FF`; success: `#45E6A1`; error: `#FF6B7A`.
- Preserve IP/port, username/password, path, status, and connection-details order.
- Preserve every existing control name used by form logic and tests.
- Do not change SSH key generation, server installation, or clipboard output behavior.
- Never place the password in status or connection-details output.

---

## File Structure

- Modify `tests/SshKeySetupTool.Tests/Presentation/FormLayoutTests.cs`: lock the compact dimensions, custom chrome, required field relationships, and color contract.
- Modify `src/SshKeySetupTool/Form1.Designer.cs`: define the borderless 680 x 520 layout, title bar, flat controls, and space-black/cyan palette.
- Modify `src/SshKeySetupTool/Form1.cs`: implement title-bar drag/minimize/close actions and status-tone updates.
- Create `outputs/SshKeySetupTool-v7.exe`: publish the verified single-file build.

### Task 1: Compact Layout And Theme

**Files:**
- Modify: `tests/SshKeySetupTool.Tests/Presentation/FormLayoutTests.cs`
- Modify: `src/SshKeySetupTool/Form1.Designer.cs`

**Interfaces:**
- Consumes: existing `Form1` control names.
- Produces: `titleBarPanel`, `minimizeButton`, and `closeButton` controls plus the unchanged input/output controls.

- [ ] **Step 1: Extend the layout test before changing production code**

Add these assertions inside the existing STA-thread test after resolving the
current controls:

```csharp
var titleBar = Find<Panel>(form, "titleBarPanel");
var minimize = Find<Button>(form, "minimizeButton");
var close = Find<Button>(form, "closeButton");

Assert.Equal(new Size(680, 520), form.ClientSize);
Assert.Equal(FormBorderStyle.None, form.FormBorderStyle);
Assert.Equal(42, titleBar.Height);
Assert.Equal(Color.FromArgb(11, 17, 24), form.BackColor);
Assert.Equal(Color.FromArgb(56, 215, 255), generate.BackColor);
Assert.Equal(FlatStyle.Flat, minimize.FlatStyle);
Assert.Equal(FlatStyle.Flat, close.FlatStyle);
Assert.True(form.Height < 560);
Assert.False(status.Multiline);
Assert.Equal(Color.FromArgb(14, 24, 34), host.BackColor);
Assert.Equal(Color.FromArgb(14, 24, 34), connectionDetails.BackColor);
```

Keep the existing relationship assertions for the five required sections.

- [ ] **Step 2: Run the focused test and verify the expected failure**

Run:

```powershell
dotnet test tests\SshKeySetupTool.Tests\SshKeySetupTool.Tests.csproj -c Release --filter FullyQualifiedName~FormLayoutTests
```

Expected: FAIL because the current form is 780 x 636, has a fixed-dialog frame,
has no `titleBarPanel`, and uses the version 6 green/white palette.

- [ ] **Step 3: Implement the compact designer layout**

Add these namespace imports above the file-scoped namespace:

```csharp
using System.Drawing;
using System.Windows.Forms;
```

Update `InitializeComponent()` with these exact presentation values:

```csharp
BackColor = Color.FromArgb(11, 17, 24);
ClientSize = new Size(680, 520);
FormBorderStyle = FormBorderStyle.None;
Padding = new Padding(1);

titleBarPanel.BackColor = Color.FromArgb(16, 27, 38);
titleBarPanel.Location = new Point(1, 1);
titleBarPanel.Size = new Size(678, 42);

headerTitleLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
headerTitleLabel.ForeColor = Color.FromArgb(233, 245, 250);
headerTitleLabel.Location = new Point(14, 11);
headerTitleLabel.Text = "CODEX  //  SSH \u5bc6\u94a5\u8bbe\u7f6e";

minimizeButton.FlatStyle = FlatStyle.Flat;
minimizeButton.FlatAppearance.BorderSize = 0;
minimizeButton.Location = new Point(606, 0);
minimizeButton.Size = new Size(36, 42);
minimizeButton.Text = "\uE921";

closeButton.FlatStyle = FlatStyle.Flat;
closeButton.FlatAppearance.BorderSize = 0;
closeButton.Location = new Point(642, 0);
closeButton.Size = new Size(36, 42);
closeButton.Text = "\uE8BB";

headerRulePanel.BackColor = Color.FromArgb(56, 215, 255);
headerRulePanel.Location = new Point(1, 43);
headerRulePanel.Size = new Size(678, 1);
```

Use these exact field bounds:

```text
host label/input:       (20, 58) / (20, 76, 456, 28)
port label/input:       (490, 58) / (490, 76, 170, 28)
username label/input:   (20, 114) / (20, 132, 313, 28)
password label/input:   (347, 114) / (347, 132, 313, 28)
path label/input:       (20, 170) / (20, 188, 640, 28)
status label/input:     (20, 226) / (20, 244, 640, 30)
details label/input:    (20, 288) / (20, 306, 640, 132)
primary button:         (440, 458, 220, 40)
```

Apply these exact shared styles:

```csharp
var inputBackColor = Color.FromArgb(14, 24, 34);
var primaryTextColor = Color.FromArgb(233, 245, 250);
var secondaryTextColor = Color.FromArgb(127, 149, 163);
var neutralBorderColor = Color.FromArgb(38, 55, 71);
var cyanColor = Color.FromArgb(56, 215, 255);

// All labels
label.ForeColor = secondaryTextColor;
label.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);

// All inputs and outputs
textBox.BackColor = inputBackColor;
textBox.ForeColor = primaryTextColor;
textBox.BorderStyle = BorderStyle.FixedSingle;

generateButton.BackColor = cyanColor;
generateButton.ForeColor = Color.FromArgb(4, 25, 34);
generateButton.FlatStyle = FlatStyle.Flat;
generateButton.FlatAppearance.BorderSize = 0;
```

Set `statusTextBox.Multiline = false`. Keep `connectionDetailsTextBox`
multiline, read-only, vertically scrollable, and set to Consolas 9.5.

Add every new title-bar control to the designer field declarations and control
tree. Do not wire interaction events in this task; Task 2 creates and binds the
handlers so this task remains independently compilable.

- [ ] **Step 4: Run the focused test and verify it passes**

Run the focused test command from Step 2.

Expected: PASS, 1 test passed and 0 failed.

- [ ] **Step 5: Commit the visual contract**

```powershell
git add src\SshKeySetupTool\Form1.Designer.cs tests\SshKeySetupTool.Tests\Presentation\FormLayoutTests.cs
git commit -m "feat: add compact cyan SSH setup layout"
```

### Task 2: Custom Chrome And Status Tones

**Files:**
- Modify: `tests/SshKeySetupTool.Tests/Presentation/FormLayoutTests.cs`
- Modify: `src/SshKeySetupTool/Form1.cs`

**Interfaces:**
- Consumes: `titleBarPanel`, `headerTitleLabel`, `minimizeButton`, `closeButton`, and `statusTextBox`.
- Produces: `WireWindowChrome`, `titleBar_MouseDown`, `minimizeButton_Click`, `closeButton_Click`, and `SetStatus`.

- [ ] **Step 1: Add a failing interaction and status test**

Add a second STA test:

```csharp
[Fact]
public void Form1_CustomChromeMinimizesAndStatusPresentationCanChangeTone()
{
    RunInSta(() =>
    {
        using var form = new Form1();
        var minimize = Find<Button>(form, "minimizeButton");
        var status = Find<TextBox>(form, "statusTextBox");
        var setStatus = typeof(Form1).GetMethod(
            "SetStatus",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(setStatus);
        setStatus.Invoke(form, new object[] { "failed", Color.FromArgb(255, 107, 122) });
        Assert.Equal("failed", status.Text);
        Assert.Equal(Color.FromArgb(255, 107, 122), status.ForeColor);

        minimize.PerformClick();
        Assert.Equal(FormWindowState.Minimized, form.WindowState);
    });
}
```

Extract the existing STA setup into:

```csharp
private static void RunInSta(Action action)
{
    Exception? exception = null;
    var thread = new Thread(() =>
    {
        try
        {
            action();
        }
        catch (Exception caught)
        {
            exception = caught;
        }
    });

    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join();

    if (exception is not null)
    {
        ExceptionDispatchInfo.Capture(exception).Throw();
    }
}
```

Add `using System.Reflection;`.

- [ ] **Step 2: Run the focused tests and verify the expected failure**

Run:

```powershell
dotnet test tests\SshKeySetupTool.Tests\SshKeySetupTool.Tests.csproj -c Release --filter FullyQualifiedName~FormLayoutTests
```

Expected: FAIL because `SetStatus` and the custom chrome handlers do not yet
exist.

- [ ] **Step 3: Implement window actions and status tones**

Add these members to `Form1`:

```csharp
private const int WmNcLButtonDown = 0x00A1;
private const int HtCaption = 0x0002;
private static readonly Color WorkingColor = Color.FromArgb(56, 215, 255);
private static readonly Color SuccessColor = Color.FromArgb(69, 230, 161);
private static readonly Color ErrorColor = Color.FromArgb(255, 107, 122);

[DllImport("user32.dll")]
private static extern bool ReleaseCapture();

[DllImport("user32.dll")]
private static extern IntPtr SendMessage(
    IntPtr windowHandle,
    int message,
    int wordParameter,
    int longParameter);

private void WireWindowChrome()
{
    titleBarPanel.MouseDown += titleBar_MouseDown;
    headerTitleLabel.MouseDown += titleBar_MouseDown;
    minimizeButton.Click += minimizeButton_Click;
    closeButton.Click += closeButton_Click;
}

private void titleBar_MouseDown(object? sender, MouseEventArgs e)
{
    if (e.Button != MouseButtons.Left)
    {
        return;
    }

    ReleaseCapture();
    SendMessage(Handle, WmNcLButtonDown, HtCaption, 0);
}

private void minimizeButton_Click(object? sender, EventArgs e) =>
    WindowState = FormWindowState.Minimized;

private void closeButton_Click(object? sender, EventArgs e) => Close();

private void SetStatus(string text, Color color)
{
    statusTextBox.Text = text;
    statusTextBox.ForeColor = color;
}
```

Call `WireWindowChrome()` immediately after `InitializeComponent()` in the
constructor.

Replace direct status assignments in `generateButton_Click` with `SetStatus`.
Use `WorkingColor` while running, `SuccessColor` after successful setup,
`ErrorColor` for failed results and exceptions, and the secondary label color
for cancellation. Preserve the existing status message text and never include
the password.

- [ ] **Step 4: Run focused and full tests**

Run:

```powershell
dotnet test tests\SshKeySetupTool.Tests\SshKeySetupTool.Tests.csproj -c Release --filter FullyQualifiedName~FormLayoutTests
dotnet test SshKeySetupTool.sln -c Release
```

Expected: focused presentation tests pass; the complete suite has 0 failures.

- [ ] **Step 5: Commit the window interactions**

```powershell
git add src\SshKeySetupTool\Form1.cs tests\SshKeySetupTool.Tests\Presentation\FormLayoutTests.cs
git commit -m "feat: add custom window chrome and status tones"
```

### Task 3: Publish And Verify Version 7

**Files:**
- Create: `outputs/SshKeySetupTool-v7.exe`

**Interfaces:**
- Consumes: verified `src/SshKeySetupTool/SshKeySetupTool.csproj`.
- Produces: a self-contained Windows x64 single-file executable.

- [ ] **Step 1: Check source cleanliness and run final tests**

```powershell
git diff --check
git status --short
dotnet test SshKeySetupTool.sln -c Release
```

Expected: no whitespace errors, no unexpected changes, and 0 failed tests.

- [ ] **Step 2: Publish to a unique temporary directory**

```powershell
$v7PublishDir = Join-Path $env:TEMP ("SshKeySetupTool-v7-" + [guid]::NewGuid())
dotnet publish src\SshKeySetupTool\SshKeySetupTool.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:DebugType=None -o $v7PublishDir
Copy-Item (Join-Path $v7PublishDir "SshKeySetupTool.exe") ..\..\outputs\SshKeySetupTool-v7.exe -Force
```

Expected: publish exits with code 0 and creates
`outputs\SshKeySetupTool-v7.exe`.

- [ ] **Step 3: Verify binary integrity and launch responsiveness**

```powershell
$v7SourceExe = Join-Path $v7PublishDir "SshKeySetupTool.exe"
$v7TargetExe = Resolve-Path ..\..\outputs\SshKeySetupTool-v7.exe
Get-FileHash -Algorithm SHA256 $v7SourceExe, $v7TargetExe
$v7Process = Start-Process $v7TargetExe -PassThru -WindowStyle Hidden
Start-Sleep -Seconds 2
Get-Process -Id $v7Process.Id | Select-Object Id, Responding, Path
Stop-Process -Id $v7Process.Id
```

Expected: both SHA-256 hashes match and the packaged process reports
`Responding = True`.

- [ ] **Step 4: Record final repository state**

```powershell
git status --short
git log -3 --oneline
```

Expected: clean tracked source state and the two implementation commits visible.
