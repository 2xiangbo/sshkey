$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$publishScript = Join-Path $repositoryRoot 'scripts\publish.ps1'
$scriptText = [System.IO.File]::ReadAllText($publishScript)

foreach ($requiredText in @(
    'SSHKEY-minimal-win-x64.exe',
    'SSHKEY-net8-win-x64.exe',
    '--self-contained false',
    '--self-contained true')) {
    if ($scriptText.IndexOf($requiredText, [System.StringComparison]::Ordinal) -lt 0) {
        throw "Expected publish script to contain: $requiredText"
    }
}

Write-Output 'PASS: publish script defines both minimal and bundled .NET 8 packages.'
