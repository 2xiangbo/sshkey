# SSH Key Tool Icon Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create the approved A-direction SSH key icon, apply it consistently to the Windows executable and form, and publish `SshKeySetupTool-v10.exe`.

**Architecture:** Keep a maintainable SVG as the visual source of truth. A build-time PowerShell tool renders a 1024px PNG with Microsoft Edge, creates high-quality scaled PNG layers, and packs those layers into a Windows ICO without adding runtime dependencies. The ICO is both the MSBuild `ApplicationIcon` and an embedded resource loaded by the WinForms window.

**Tech Stack:** .NET 8 WinForms, SVG, PowerShell 5.1, Microsoft Edge headless rendering, System.Drawing, xUnit.

## Global Constraints

- Preserve the approved dark rounded background, cyan/blue double-X structure, and lime key symbol.
- Generate 16, 24, 32, 48, 64, 128, and 256 pixel ICO layers.
- Keep SVG, 1024 x 1024 PNG, and ICO assets in the repository.
- Do not add Python or any runtime package dependency.
- Do not change layout, SSH behavior, authentication, or user data.
- Publish a self-contained, single-file `win-x64` executable named `SshKeySetupTool-v10.exe`.

---

### Task 1: Create Reproducible Icon Assets

**Files:**
- Create: `src/SshKeySetupTool/Assets/ssh-key-tool-icon.svg`
- Create: `src/SshKeySetupTool/Assets/ssh-key-tool-icon-1024.png`
- Create: `src/SshKeySetupTool/Assets/ssh-key-tool-icon.ico`
- Create: `tools/Generate-SshKeyToolIcon.ps1`

**Interfaces:**
- Consumes: Microsoft Edge at one of the standard Windows installation paths.
- Produces: `ssh-key-tool-icon.svg`, `ssh-key-tool-icon-1024.png`, and a seven-layer `ssh-key-tool-icon.ico`.

- [ ] **Step 1: Add the approved SVG source**

Create `src/SshKeySetupTool/Assets/ssh-key-tool-icon.svg` with the approved dark background, paired X geometry, central dark diamond, and lime key:

```xml
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" role="img" aria-labelledby="title desc">
  <title id="title">XXcodex SSH key tool icon</title>
  <desc id="desc">A dark app icon with cyan and blue double X shapes surrounding a lime SSH key.</desc>
  <defs>
    <linearGradient id="bg" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="#101820"/>
      <stop offset="1" stop-color="#050c14"/>
    </linearGradient>
    <linearGradient id="cyan" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="#58f0ff"/>
      <stop offset="1" stop-color="#1678ff"/>
    </linearGradient>
    <linearGradient id="blue" x1="0" y1="1" x2="1" y2="0">
      <stop offset="0" stop-color="#1765ff"/>
      <stop offset="1" stop-color="#7af8ff"/>
    </linearGradient>
    <filter id="glow" x="-40%" y="-40%" width="180%" height="180%">
      <feGaussianBlur stdDeviation="7" result="blur"/>
      <feMerge><feMergeNode in="blur"/><feMergeNode in="SourceGraphic"/></feMerge>
    </filter>
    <filter id="limeGlow" x="-80%" y="-80%" width="260%" height="260%">
      <feGaussianBlur stdDeviation="5" result="blur"/>
      <feMerge><feMergeNode in="blur"/><feMergeNode in="SourceGraphic"/></feMerge>
    </filter>
  </defs>
  <rect width="512" height="512" rx="108" fill="url(#bg)"/>
  <rect x="34" y="34" width="444" height="444" rx="84" fill="none" stroke="#193447" stroke-width="2"/>
  <g filter="url(#glow)" fill="none" stroke-linecap="round" stroke-linejoin="round">
    <path d="M116 145 214 256 116 367" stroke="url(#cyan)" stroke-width="44"/>
    <path d="M214 145 116 256 214 367" stroke="url(#blue)" stroke-width="44" opacity=".92"/>
    <path d="M298 145 396 256 298 367" stroke="url(#cyan)" stroke-width="44"/>
    <path d="M396 145 298 256 396 367" stroke="url(#blue)" stroke-width="44" opacity=".92"/>
  </g>
  <path d="M204 211h104l44 45-44 45H204l-44-45z" fill="#08111b" stroke="#18364a" stroke-width="3"/>
  <g filter="url(#limeGlow)" fill="none" stroke="#9dff42" stroke-width="15" stroke-linecap="round" stroke-linejoin="round">
    <circle cx="232" cy="256" r="23"/>
    <path d="M255 256h78m-24 0v22m-24-22v17"/>
  </g>
</svg>
```

- [ ] **Step 2: Add the deterministic asset generator**

Create `tools/Generate-SshKeyToolIcon.ps1`. It must:

1. Resolve the repository root from `$PSScriptRoot`.
2. Find `msedge.exe` under `C:\Program Files (x86)\Microsoft\Edge\Application` or `C:\Program Files\Microsoft\Edge\Application`.
3. Render the SVG at 1024 x 1024 with:

```powershell
$edgeArguments = @(
    '--headless=new',
    '--disable-gpu',
    '--hide-scrollbars',
    '--default-background-color=00000000',
    '--window-size=1024,1024',
    "--screenshot=$pngPath",
    ([Uri]$svgPath).AbsoluteUri
)
```

4. Poll for the PNG for up to 15 seconds because Edge may finish the screenshot in a child process after the launcher exits.
5. Load the 1024px PNG with `System.Drawing.Image`, scale it with `HighQualityBicubic` to `16, 24, 32, 48, 64, 128, 256`, and save each layer as PNG in a temporary directory.
6. Write a valid ICO header and one 16-byte directory entry per PNG layer. Use width/height byte `0` for 256px, planes `1`, bit depth `32`, and append each PNG payload at its recorded offset.
7. Delete only the unique temporary directory in a `finally` block.

The ICO packing loop must use this exact structure:

```powershell
$sizes = @(16, 24, 32, 48, 64, 128, 256)
$payloads = foreach ($size in $sizes) {
    [System.IO.File]::ReadAllBytes((Join-Path $temporaryDirectory "$size.png"))
}

$stream = [System.IO.File]::Create($icoPath)
$writer = [System.IO.BinaryWriter]::new($stream)
try {
    $writer.Write([uint16]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]$sizes.Count)
    $offset = 6 + (16 * $sizes.Count)

    for ($index = 0; $index -lt $sizes.Count; $index++) {
        $sizeByte = if ($sizes[$index] -eq 256) { 0 } else { $sizes[$index] }
        $writer.Write([byte]$sizeByte)
        $writer.Write([byte]$sizeByte)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]32)
        $writer.Write([uint32]$payloads[$index].Length)
        $writer.Write([uint32]$offset)
        $offset += $payloads[$index].Length
    }

    foreach ($payload in $payloads) {
        $writer.Write($payload)
    }
}
finally {
    $writer.Dispose()
    $stream.Dispose()
}
```

- [ ] **Step 3: Generate the PNG and ICO**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Generate-SshKeyToolIcon.ps1
```

Expected: exit code `0` and all three files exist under `src\SshKeySetupTool\Assets`.

- [ ] **Step 4: Verify the generated ICO directory**

Run a PowerShell check that reads the first six bytes and the seven 16-byte entries:

```powershell
$bytes = [System.IO.File]::ReadAllBytes('src\SshKeySetupTool\Assets\ssh-key-tool-icon.ico')
$count = [BitConverter]::ToUInt16($bytes, 4)
if ($count -ne 7) { throw "Expected 7 ICO layers, found $count." }
$sizes = for ($index = 0; $index -lt $count; $index++) {
    $value = $bytes[6 + (16 * $index)]
    if ($value -eq 0) { 256 } else { [int]$value }
}
if (Compare-Object $sizes @(16, 24, 32, 48, 64, 128, 256)) {
    throw "Unexpected ICO dimensions: $($sizes -join ', ')."
}
```

Expected: exit code `0`.

- [ ] **Step 5: Visually inspect the 1024px PNG**

Open `src/SshKeySetupTool/Assets/ssh-key-tool-icon-1024.png` and confirm:

- transparent pixels exist outside the rounded square;
- the cyan/blue paired X is centered;
- the lime key is fully inside the central diamond;
- no white browser background or clipping appears.

- [ ] **Step 6: Commit the generated assets**

```powershell
git add tools/Generate-SshKeyToolIcon.ps1 src/SshKeySetupTool/Assets
git commit -m "feat: add ssh key tool icon assets"
```

### Task 2: Apply the Icon to the Executable and Window

**Files:**
- Create: `src/SshKeySetupTool/AppIcon.cs`
- Modify: `src/SshKeySetupTool/SshKeySetupTool.csproj`
- Modify: `src/SshKeySetupTool/Form1.cs`
- Create: `tests/SshKeySetupTool.Tests/Presentation/AppIconTests.cs`

**Interfaces:**
- Consumes: embedded resource `SshKeySetupTool.Assets.ssh-key-tool-icon.ico`.
- Produces: `internal static Icon AppIcon.Load()` and a `Form1.Icon` initialized from that resource.

- [ ] **Step 1: Write the failing icon-loading test**

Create `tests/SshKeySetupTool.Tests/Presentation/AppIconTests.cs`:

```csharp
namespace SshKeySetupTool.Tests.Presentation;

public sealed class AppIconTests
{
    [Fact]
    public void Load_ReturnsTheEmbeddedApplicationIcon()
    {
        using var icon = AppIcon.Load();
        using var bitmap = icon.ToBitmap();

        Assert.InRange(icon.Width, 16, 256);
        Assert.Equal(icon.Width, icon.Height);
        Assert.Equal(icon.Size, bitmap.Size);
    }
}
```

- [ ] **Step 2: Run the focused test to verify RED**

Run:

```powershell
dotnet test SshKeySetupTool.sln -c Release --filter FullyQualifiedName~AppIconTests
```

Expected: compilation failure because `AppIcon` does not exist.

- [ ] **Step 3: Embed and apply the icon**

Update `src/SshKeySetupTool/SshKeySetupTool.csproj`:

```xml
<PropertyGroup>
  <ApplicationIcon>Assets\ssh-key-tool-icon.ico</ApplicationIcon>
</PropertyGroup>
<ItemGroup>
  <EmbeddedResource Include="Assets\ssh-key-tool-icon.ico"
                    LogicalName="SshKeySetupTool.Assets.ssh-key-tool-icon.ico" />
</ItemGroup>
```

Create `src/SshKeySetupTool/AppIcon.cs`:

```csharp
namespace SshKeySetupTool;

internal static class AppIcon
{
    private const string ResourceName =
        "SshKeySetupTool.Assets.ssh-key-tool-icon.ico";

    public static Icon Load()
    {
        using var stream = typeof(AppIcon).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded application icon '{ResourceName}' was not found.");
        using var source = new Icon(stream);
        return (Icon)source.Clone();
    }
}
```

In the private `Form1` constructor, immediately after `InitializeComponent();`, add:

```csharp
Icon = AppIcon.Load();
```

- [ ] **Step 4: Run the focused test to verify GREEN**

Run:

```powershell
dotnet test SshKeySetupTool.sln -c Release --filter FullyQualifiedName~AppIconTests
```

Expected: `1` test passed and `0` failed.

- [ ] **Step 5: Run presentation tests**

Run:

```powershell
dotnet test SshKeySetupTool.sln -c Release --filter FullyQualifiedName~Presentation
```

Expected: all presentation tests pass.

- [ ] **Step 6: Commit application integration**

```powershell
git add src/SshKeySetupTool/SshKeySetupTool.csproj src/SshKeySetupTool/AppIcon.cs src/SshKeySetupTool/Form1.cs tests/SshKeySetupTool.Tests/Presentation/AppIconTests.cs
git commit -m "feat: apply ssh tool application icon"
```

### Task 3: Verify and Publish v10

**Files:**
- Create ignored artifact: `outputs/SshKeySetupTool-v10.exe`

**Interfaces:**
- Consumes: the fully tested `SshKeySetupTool` project.
- Produces: a self-contained single-file Windows executable with the new PE icon.

- [ ] **Step 1: Run the complete test suite**

Run:

```powershell
dotnet test SshKeySetupTool.sln -c Release
```

Expected: every test passes with `0` failures.

- [ ] **Step 2: Publish to a unique temporary directory**

Run:

```powershell
dotnet publish src\SshKeySetupTool\SshKeySetupTool.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:DebugType=None `
  -p:DebugSymbols=false `
  -o .artifacts\publish-v10
```

Expected: `.artifacts\publish-v10\SshKeySetupTool.exe` exists.

- [ ] **Step 3: Copy and hash the release executable**

Run:

```powershell
Copy-Item .artifacts\publish-v10\SshKeySetupTool.exe outputs\SshKeySetupTool-v10.exe
Get-FileHash -Algorithm SHA256 outputs\SshKeySetupTool-v10.exe
```

Expected: a non-empty SHA256 value.

- [ ] **Step 4: Verify the published PE icon**

Run:

```powershell
Add-Type -AssemblyName System.Drawing
$icon = [System.Drawing.Icon]::ExtractAssociatedIcon(
    (Resolve-Path 'outputs\SshKeySetupTool-v10.exe'))
if ($null -eq $icon) { throw 'Published executable has no associated icon.' }
try {
    if ($icon.Width -lt 16 -or $icon.Height -lt 16) {
        throw "Unexpected executable icon size: $($icon.Size)."
    }
}
finally {
    $icon.Dispose()
}
```

Expected: exit code `0`.

- [ ] **Step 5: Launch-smoke the release**

Start `outputs\SshKeySetupTool-v10.exe` hidden, wait three seconds, verify `HasExited=False` and `Responding=True`, then stop only that process ID.

Expected: the application remains alive and responds.

- [ ] **Step 6: Verify repository state**

Run:

```powershell
git diff --check
git status --short
```

Expected: no whitespace errors and no uncommitted tracked changes. Only ignored release artifacts may remain.
