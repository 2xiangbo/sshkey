$ErrorActionPreference = 'Stop'

function ConvertTo-WindowsCommandLineArgument {
    param([Parameter(Mandatory)][string]$Argument)

    if ($Argument -notmatch '[\s"]') {
        return $Argument
    }

    return '"' + ($Argument -replace '"', '\"') + '"'
}

function Stop-ProcessTree {
    param([System.Diagnostics.Process]$Process)

    if ($null -eq $Process) {
        return
    }

    try {
        if (-not $Process.HasExited) {
            & "$env:SystemRoot\System32\taskkill.exe" /PID $Process.Id /T /F 2>$null | Out-Null
        }
    }
    catch [System.InvalidOperationException] {
        # The process exited while checking its state.
    }
}

function Remove-TemporaryDirectory {
    param([Parameter(Mandatory)][string]$Path)

    for ($attempt = 0; $attempt -lt 10 -and (Test-Path -LiteralPath $Path); $attempt++) {
        try {
            Remove-Item -LiteralPath $Path -Recurse -Force -ErrorAction Stop
        }
        catch [System.IO.IOException] {
            Start-Sleep -Milliseconds 250
        }
        catch [System.UnauthorizedAccessException] {
            Start-Sleep -Milliseconds 250
        }
    }

    if (Test-Path -LiteralPath $Path) {
        throw "Could not remove temporary icon directory '$Path'."
    }
}

function Test-IconFile {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][int[]]$Sizes
    )

    $bytes = [System.IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -lt 6) {
        throw 'Generated ICO is shorter than its header.'
    }
    if ([BitConverter]::ToUInt16($bytes, 0) -ne 0 -or [BitConverter]::ToUInt16($bytes, 2) -ne 1) {
        throw 'Generated ICO has an invalid header.'
    }
    if ([BitConverter]::ToUInt16($bytes, 4) -ne $Sizes.Count) {
        throw 'Generated ICO does not contain every requested layer.'
    }

    $expectedOffset = 6 + (16 * $Sizes.Count)
    for ($index = 0; $index -lt $Sizes.Count; $index++) {
        $entryOffset = 6 + (16 * $index)
        if ($bytes.Length -lt $entryOffset + 16) {
            throw 'Generated ICO contains a truncated directory entry.'
        }

        $width = if ($bytes[$entryOffset] -eq 0) { 256 } else { [int]$bytes[$entryOffset] }
        $height = if ($bytes[$entryOffset + 1] -eq 0) { 256 } else { [int]$bytes[$entryOffset + 1] }
        $planes = [BitConverter]::ToUInt16($bytes, $entryOffset + 4)
        $bitsPerPixel = [BitConverter]::ToUInt16($bytes, $entryOffset + 6)
        $payloadLength = [BitConverter]::ToUInt32($bytes, $entryOffset + 8)
        $payloadOffset = [BitConverter]::ToUInt32($bytes, $entryOffset + 12)

        if ($width -ne $Sizes[$index] -or $height -ne $Sizes[$index]) {
            throw "Generated ICO layer $index has an unexpected size."
        }
        if ($planes -ne 1 -or $bitsPerPixel -ne 32) {
            throw "Generated ICO layer $index has an unexpected pixel format."
        }
        if ($payloadOffset -ne $expectedOffset -or $payloadLength -le 8 -or $payloadOffset + $payloadLength -gt $bytes.Length) {
            throw "Generated ICO layer $index has an invalid payload range."
        }
        if (-not [System.Linq.Enumerable]::SequenceEqual(
            [byte[]]$bytes[$payloadOffset..($payloadOffset + 7)],
            [byte[]](137, 80, 78, 71, 13, 10, 26, 10))) {
            throw "Generated ICO layer $index is not PNG encoded."
        }

        $payload = [System.IO.MemoryStream]::new($bytes, [int]$payloadOffset, [int]$payloadLength, $false)
        try {
            $layer = [System.Drawing.Image]::FromStream($payload, $false, $true)
            try {
                if ($layer.Width -ne $Sizes[$index] -or $layer.Height -ne $Sizes[$index]) {
                    throw "Generated ICO layer $index did not decode at its declared size."
                }
            }
            finally {
                $layer.Dispose()
            }
        }
        finally {
            $payload.Dispose()
        }

        $expectedOffset += [int]$payloadLength
    }

    if ($expectedOffset -ne $bytes.Length) {
        throw 'Generated ICO payloads are not contiguous.'
    }
}

function Promote-IconAssets {
    param(
        [Parameter(Mandatory)][string]$GeneratedPngPath,
        [Parameter(Mandatory)][string]$GeneratedIcoPath,
        [Parameter(Mandatory)][string]$PngPath,
        [Parameter(Mandatory)][string]$IcoPath
    )

    $hasPng = Test-Path -LiteralPath $PngPath
    $hasIco = Test-Path -LiteralPath $IcoPath
    if ($hasPng -ne $hasIco) {
        throw 'Existing icon assets must either both exist or both be absent before promotion.'
    }

    $token = [Guid]::NewGuid().ToString('N')
    $pngStagingPath = "$PngPath.$token.staged"
    $icoStagingPath = "$IcoPath.$token.staged"
    $pngBackupPath = "$PngPath.$token.backup"
    $icoBackupPath = "$IcoPath.$token.backup"
    $pngPromoted = $false
    $icoPromoted = $false

    try {
        [System.IO.File]::Copy($GeneratedPngPath, $pngStagingPath, $true)
        [System.IO.File]::Copy($GeneratedIcoPath, $icoStagingPath, $true)

        if ($hasPng) {
            [System.IO.File]::Replace($pngStagingPath, $PngPath, $pngBackupPath, $true)
            $pngPromoted = $true
            [System.IO.File]::Replace($icoStagingPath, $IcoPath, $icoBackupPath, $true)
            $icoPromoted = $true
        }
        else {
            [System.IO.File]::Move($pngStagingPath, $PngPath)
            $pngPromoted = $true
            [System.IO.File]::Move($icoStagingPath, $IcoPath)
            $icoPromoted = $true
        }
    }
    catch {
        if ($hasIco -and $icoPromoted -and (Test-Path -LiteralPath $icoBackupPath)) {
            [System.IO.File]::Copy($icoBackupPath, $IcoPath, $true)
        }
        elseif (-not $hasIco -and $icoPromoted -and (Test-Path -LiteralPath $IcoPath)) {
            Remove-Item -LiteralPath $IcoPath -Force
        }
        if ($hasPng -and $pngPromoted -and (Test-Path -LiteralPath $pngBackupPath)) {
            [System.IO.File]::Copy($pngBackupPath, $PngPath, $true)
        }
        elseif (-not $hasPng -and $pngPromoted -and (Test-Path -LiteralPath $PngPath)) {
            Remove-Item -LiteralPath $PngPath -Force
        }
        throw
    }
    finally {
        foreach ($path in @($pngStagingPath, $icoStagingPath, $pngBackupPath, $icoBackupPath)) {
            if (Test-Path -LiteralPath $path) {
                Remove-Item -LiteralPath $path -Force
            }
        }
    }
}

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$assetDirectory = Join-Path $repositoryRoot 'src\SshKeySetupTool\Assets'
$svgPath = Join-Path $assetDirectory 'ssh-key-tool-icon.svg'
$pngPath = Join-Path $assetDirectory 'ssh-key-tool-icon-1024.png'
$icoPath = Join-Path $assetDirectory 'ssh-key-tool-icon.ico'
$edgeCandidates = @(
    'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe',
    'C:\Program Files\Microsoft\Edge\Application\msedge.exe'
)
$edgePath = $edgeCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $edgePath) {
    throw 'Microsoft Edge was not found in a standard installation path.'
}

New-Item -ItemType Directory -Force -Path $assetDirectory | Out-Null
$temporaryDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ('ssh-key-tool-icon-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temporaryDirectory | Out-Null
$renderedPngPath = Join-Path $temporaryDirectory 'ssh-key-tool-icon-1024.png'
$generatedIcoPath = Join-Path $temporaryDirectory 'ssh-key-tool-icon.ico'
$browserProfileDirectory = Join-Path $temporaryDirectory 'edge-user-data'
$edgeProcess = $null

try {
    $edgeArguments = @(
        '--headless=new',
        '--disable-gpu',
        '--hide-scrollbars',
        '--default-background-color=00000000',
        '--window-size=1024,1024',
        "--user-data-dir=$browserProfileDirectory",
        "--screenshot=$renderedPngPath",
        ([Uri]$svgPath).AbsoluteUri
    )
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $edgePath
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.Arguments = (($edgeArguments | ForEach-Object {
        ConvertTo-WindowsCommandLineArgument $_
    }) -join ' ')
    $edgeProcess = [System.Diagnostics.Process]::Start($startInfo)
    if ($null -eq $edgeProcess) {
        throw 'Microsoft Edge did not start.'
    }

    $deadline = [DateTime]::UtcNow.AddSeconds(15)
    $renderComplete = $false
    do {
        if (Test-Path -LiteralPath $renderedPngPath) {
            try {
                $renderStream = [System.IO.File]::Open(
                    $renderedPngPath,
                    [System.IO.FileMode]::Open,
                    [System.IO.FileAccess]::Read,
                    [System.IO.FileShare]::None
                )
                try {
                    $renderComplete = $renderStream.Length -gt 0
                }
                finally {
                    $renderStream.Dispose()
                }
            }
            catch [System.IO.IOException] {
                $renderComplete = $false
            }
        }
        if (-not $renderComplete) {
            Start-Sleep -Milliseconds 250
        }
    } while (-not $renderComplete -and [DateTime]::UtcNow -lt $deadline)
    if (-not $renderComplete) {
        throw 'Edge did not produce the PNG within 15 seconds.'
    }
    if (-not $edgeProcess.HasExited) {
        $edgeProcess.WaitForExit(5000) | Out-Null
    }

    Add-Type -AssemblyName System.Drawing
    $source = [System.Drawing.Image]::FromFile($renderedPngPath)
    try {
        if ($source.Width -ne 1024 -or $source.Height -ne 1024) {
            throw 'Edge did not render a 1024 x 1024 PNG.'
        }

        $sizes = @(16, 24, 32, 48, 64, 128, 256)
        foreach ($size in $sizes) {
            $scaled = [System.Drawing.Bitmap]::new($size, $size)
            $graphics = [System.Drawing.Graphics]::FromImage($scaled)
            try {
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.DrawImage($source, 0, 0, $size, $size)
                $scaled.Save((Join-Path $temporaryDirectory "$size.png"), [System.Drawing.Imaging.ImageFormat]::Png)
            }
            finally {
                $graphics.Dispose()
                $scaled.Dispose()
            }
        }
    }
    finally {
        $source.Dispose()
    }

    $payloads = [System.Collections.Generic.List[byte[]]]::new()
    foreach ($size in $sizes) {
        $payloads.Add([System.IO.File]::ReadAllBytes((Join-Path $temporaryDirectory "$size.png")))
    }

    $stream = [System.IO.File]::Create($generatedIcoPath)
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

    Test-IconFile -Path $generatedIcoPath -Sizes $sizes
    Promote-IconAssets -GeneratedPngPath $renderedPngPath -GeneratedIcoPath $generatedIcoPath -PngPath $pngPath -IcoPath $icoPath
}
catch {
    Stop-ProcessTree $edgeProcess
    throw
}
finally {
    if ($null -ne $edgeProcess) {
        Stop-ProcessTree $edgeProcess
        $edgeProcess.WaitForExit(5000) | Out-Null
        $edgeProcess.Dispose()
    }
    if (Test-Path -LiteralPath $temporaryDirectory) {
        Remove-TemporaryDirectory $temporaryDirectory
    }
}
