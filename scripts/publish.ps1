$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot '..\src\SshKeySetupTool\SshKeySetupTool.csproj'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output = [IO.Path]::GetFullPath((Join-Path $workspace 'outputs'))
$workspacePrefix = $workspace.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar

if (-not $output.StartsWith($workspacePrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "The publish output path must be inside the workspace: $output"
}

$minimalStaging = Join-Path $output 'publish-minimal'
$net8Staging = Join-Path $output 'publish-net8'
$minimalPackage = Join-Path $output 'SSHKEY-minimal-win-x64.exe'
$net8Package = Join-Path $output 'SSHKEY-net8-win-x64.exe'

function Reset-StagingDirectory([string]$path) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Recurse -Force
    }

    New-Item -ItemType Directory -Path $path -Force | Out-Null
}

New-Item -ItemType Directory -Path $output -Force | Out-Null
Reset-StagingDirectory $minimalStaging
Reset-StagingDirectory $net8Staging

dotnet publish $project -c Release -r win-x64 --self-contained false `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $minimalStaging

Copy-Item -LiteralPath (Join-Path $minimalStaging 'SshKeySetupTool.exe') `
    -Destination $minimalPackage -Force

dotnet publish $project -c Release -r win-x64 --self-contained true `
    -p:RuntimeFrameworkVersion=8.0.28 `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $net8Staging

Copy-Item -LiteralPath (Join-Path $net8Staging 'SshKeySetupTool.exe') `
    -Destination $net8Package -Force

Write-Output "Published $minimalPackage"
Write-Output "Published $net8Package"
