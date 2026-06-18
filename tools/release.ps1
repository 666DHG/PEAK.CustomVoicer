[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

$ErrorActionPreference = 'Stop'

function Fail([string]$Message) {
    throw "[release] $Message"
}

function Require-Command([string]$Name) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        Fail "Required command '$Name' was not found on PATH."
    }
}

function Require-File([string]$Path, [string]$Description) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        Fail "$Description was not found."
    }
}

if ($Version -notmatch '^\d+\.\d+\.\d+([-.][0-9A-Za-z.-]+)?$') {
    Fail "Version must look like 1.2.3 or 1.2.3-preview.1."
}

Require-Command dotnet
Require-Command git

if ([string]::IsNullOrWhiteSpace($env:PEAK_GAME_ROOT)) {
    Fail 'PEAK_GAME_ROOT is not set. Example: $env:PEAK_GAME_ROOT="C:\Path\To\PEAK"'
}

$peakManagedReference = Join-Path $env:PEAK_GAME_ROOT 'PEAK_Data\Managed\Assembly-CSharp.dll'
$harmonyReference = Join-Path $env:PEAK_GAME_ROOT 'BepInEx\core\0Harmony.dll'
Require-File $peakManagedReference 'Required PEAK managed reference'
Require-File $harmonyReference 'Required BepInEx core reference'

Require-Command gh

$repoRoot = (& git rev-parse --show-toplevel).Trim()
if ([string]::IsNullOrWhiteSpace($repoRoot)) {
    Fail 'Could not determine the git repository root.'
}

Set-Location $repoRoot

$status = & git status --porcelain
if ($LASTEXITCODE -ne 0) {
    Fail 'Could not inspect git status.'
}

if ($status) {
    Fail 'Working tree is not clean. Commit or stash changes before creating a release.'
}

& gh auth status | Out-Null
if ($LASTEXITCODE -ne 0) {
    Fail 'GitHub CLI is not authenticated. Run gh auth login first.'
}

$tagName = "v$Version"
$targetCommit = (& git rev-parse HEAD).Trim()
if ([string]::IsNullOrWhiteSpace($targetCommit)) {
    Fail 'Could not determine the current commit.'
}

& git ls-remote --exit-code --tags origin "refs/tags/$tagName" *> $null
if ($LASTEXITCODE -eq 0) {
    Fail "Remote tag '$tagName' already exists."
}
elseif ($LASTEXITCODE -ne 2) {
    Fail "Could not check whether remote tag '$tagName' exists."
}

& gh release view $tagName *> $null
if ($LASTEXITCODE -eq 0) {
    Fail "GitHub release '$tagName' already exists."
}

Write-Host "Building PEAK.CustomVoicer $Version..."
& dotnet build PEAK.CustomVoicer.sln `
    --configuration Release `
    /p:Version=$Version `
    /p:PeakGameRootDir="$env:PEAK_GAME_ROOT" `
    /p:DeployPluginOnBuild=false

if ($LASTEXITCODE -ne 0) {
    Fail 'Build failed.'
}

$pluginDll = Join-Path $repoRoot 'src\PEAK.CustomVoicer\bin\Release\netstandard2.1\PEAK.CustomVoicer.dll'
$toolOutDir = Join-Path $repoRoot 'tools\PEAK.CustomVoicer.VoicePackTool\bin\Release\net8.0'
$toolExe = Join-Path $toolOutDir 'PEAK.CustomVoicer.VoicePackTool.exe'
$toolDll = Join-Path $toolOutDir 'PEAK.CustomVoicer.VoicePackTool.dll'
$toolDeps = Join-Path $toolOutDir 'PEAK.CustomVoicer.VoicePackTool.deps.json'
$toolRuntimeConfig = Join-Path $toolOutDir 'PEAK.CustomVoicer.VoicePackTool.runtimeconfig.json'

Require-File $pluginDll 'Plugin DLL'
Require-File $toolExe 'VoicePackTool executable'
Require-File $toolDll 'VoicePackTool DLL'
Require-File $toolDeps 'VoicePackTool deps file'
Require-File $toolRuntimeConfig 'VoicePackTool runtime config'

$artifactsDir = Join-Path $repoRoot 'artifacts'
$packageRoot = Join-Path $artifactsDir 'PEAK.CustomVoicer'
$zipPath = Join-Path $artifactsDir "PEAK.CustomVoicer-v$Version.zip"

if (Test-Path -LiteralPath $packageRoot) {
    Remove-Item -LiteralPath $packageRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $packageRoot | Out-Null

Copy-Item -LiteralPath $pluginDll -Destination $packageRoot
Copy-Item -LiteralPath $toolExe -Destination $packageRoot
Copy-Item -LiteralPath $toolDll -Destination $packageRoot
Copy-Item -LiteralPath $toolDeps -Destination $packageRoot
Copy-Item -LiteralPath $toolRuntimeConfig -Destination $packageRoot

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $packageRoot '*') -DestinationPath $zipPath
Require-File $zipPath 'Release zip'

Write-Host "Creating GitHub release $tagName..."
& gh release create $tagName `
    $zipPath `
    $pluginDll `
    $toolExe `
    --title "PEAK CustomVoicer $tagName" `
    --notes "Release $tagName" `
    --target $targetCommit

if ($LASTEXITCODE -ne 0) {
    Fail 'GitHub release creation failed.'
}

Write-Host "Release $tagName created successfully."
