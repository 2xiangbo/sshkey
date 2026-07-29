$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$testRoot = Join-Path ([System.IO.Path]::GetTempPath()) (
    'ssh icon promotion rollback ' + [Guid]::NewGuid().ToString('N'))
$testTools = Join-Path $testRoot 'tools'
$testAssets = Join-Path $testRoot 'src\SshKeySetupTool\Assets'
$testGenerator = Join-Path $testTools 'Generate-SshKeyToolIcon.ps1'
$testPng = Join-Path $testAssets 'ssh-key-tool-icon-1024.png'
$testIco = Join-Path $testAssets 'ssh-key-tool-icon.ico'

try {
    New-Item -ItemType Directory -Force -Path $testTools | Out-Null
    New-Item -ItemType Directory -Force -Path $testAssets | Out-Null
    Copy-Item -LiteralPath (
        Join-Path $repositoryRoot 'tools\Generate-SshKeyToolIcon.ps1'
    ) -Destination $testGenerator
    Copy-Item -LiteralPath (
        Join-Path $repositoryRoot 'src\SshKeySetupTool\Assets\ssh-key-tool-icon.svg'
    ) -Destination (Join-Path $testAssets 'ssh-key-tool-icon.svg')

    $generatorSource = [System.IO.File]::ReadAllText($testGenerator)
    $moveStatement = '[System.IO.File]::Move($icoStagingPath, $IcoPath)'
    $injectedFailure = "throw 'Injected ICO promotion failure after PNG promotion.'"
    $firstMatch = $generatorSource.IndexOf($moveStatement, [System.StringComparison]::Ordinal)
    $lastMatch = $generatorSource.LastIndexOf($moveStatement, [System.StringComparison]::Ordinal)
    if ($firstMatch -lt 0 -or $firstMatch -ne $lastMatch) {
        throw 'Expected exactly one initial-generation ICO move statement.'
    }
    $generatorSource = $generatorSource.Replace($moveStatement, $injectedFailure)
    [System.IO.File]::WriteAllText($testGenerator, $generatorSource)

    $originalErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $generatorOutput = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $testGenerator 2>&1 |
            Out-String
        $generatorExitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $originalErrorActionPreference
    }

    if ($generatorExitCode -eq 0) {
        throw 'Expected the fault-injected generator to fail.'
    }
    if ($generatorOutput -notmatch 'Injected ICO promotion failure after PNG promotion') {
        throw "Unexpected generator failure:`n$generatorOutput"
    }
    if (Test-Path -LiteralPath $testPng) {
        throw 'PNG remained after the initial-generation ICO promotion failed.'
    }
    if (Test-Path -LiteralPath $testIco) {
        throw 'ICO remained after the initial-generation ICO promotion failed.'
    }

    Write-Output 'PASS: initial-generation promotion failure restored the absent asset pair.'
}
finally {
    if (Test-Path -LiteralPath $testRoot) {
        Remove-Item -LiteralPath $testRoot -Recurse -Force
    }
}
