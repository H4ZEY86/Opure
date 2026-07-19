#requires -Version 7.2

[CmdletBinding()]
param(
    [Parameter()]
    [string] $RepositoryRoot,

    [Parameter()]
    [string] $OutputPath,

    [Parameter()]
    [switch] $FailIfDirty
)

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = Get-OpureRepositoryRoot
}
else {
    $RepositoryRoot = [System.IO.Path]::GetFullPath($RepositoryRoot)
}

Assert-OpureBuildEnvironment -RepositoryRoot $RepositoryRoot

$versionFile = Join-Path $RepositoryRoot 'version.json'
if (-not (Test-Path -LiteralPath $versionFile -PathType Leaf)) {
    throw "Version source not found: $versionFile"
}

$versionDocument = Get-Content -LiteralPath $versionFile -Raw | ConvertFrom-Json
$declaredVersion = [string]$versionDocument.version

$commit = (& git -C $RepositoryRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($commit)) {
    throw 'Unable to resolve the current Git commit.'
}

$branch = (& git -C $RepositoryRoot branch --show-current).Trim()
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to resolve the current Git branch.'
}
if ([string]::IsNullOrWhiteSpace($branch)) {
    $branch = '<detached>'
}

$dirtyEntries = @(& git -C $RepositoryRoot status --porcelain=v1)
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to inspect Git working-tree state.'
}

$isDirty = $dirtyEntries.Count -gt 0
if ($FailIfDirty -and $isDirty) {
    throw 'The Git working tree is dirty. Release-class version resolution is prohibited.'
}

$tagsAtHead = @(
    & git -C $RepositoryRoot tag --points-at HEAD |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
)
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to inspect tags at HEAD.'
}

$expectedTag = "v$declaredVersion"
$matchingReleaseTag = $tagsAtHead |
    Where-Object {
        $_ -eq $expectedTag -and
        $_ -match '^v\d+\.\d+\.\d+(?:-(?:preview|beta|rc)\.[1-9]\d*)?$'
    } |
    Select-Object -First 1

$releaseChannel = if (-not $matchingReleaseTag) {
    'Development'
}
elseif ($declaredVersion -match '-preview\.') {
    'Preview'
}
elseif ($declaredVersion -match '-beta\.') {
    'Beta'
}
elseif ($declaredVersion -match '-rc\.') {
    'Release Candidate'
}
else {
    'Stable'
}

$buildClass = if ($env:GITHUB_ACTIONS -eq 'true') {
    if ($matchingReleaseTag) { "Public $releaseChannel" } else { 'GitHub Integration' }
}
elseif ($isDirty) {
    'Local Dirty'
}
elseif ($matchingReleaseTag) {
    "Public $releaseChannel"
}
else {
    'Local Clean'
}

Push-Location $RepositoryRoot
try {
    $nbgvOutput = @(& dotnet nbgv get-version 2>&1)
    $nbgvExitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

if ($nbgvExitCode -ne 0) {
    $nbgvOutput | Out-Host
    throw "nbgv get-version failed with exit code $nbgvExitCode."
}

$lines = @(
    'Opure Version Identity'
    '======================'
    "DeclaredVersion: $declaredVersion"
    "ReleaseChannel: $releaseChannel"
    "BuildClass: $buildClass"
    "Commit: $commit"
    "Branch: $branch"
    "SourceDirty: $($isDirty.ToString().ToLowerInvariant())"
    "MatchingReleaseTag: $(if ($matchingReleaseTag) { $matchingReleaseTag } else { '<none>' })"
    ''
    'Nerdbank.GitVersioning'
    '----------------------'
) + [string[]]$nbgvOutput

$lines | Out-Host

if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    $resolvedOutputPath = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
        $OutputPath
    }
    else {
        Join-Path $RepositoryRoot $OutputPath
    }

    $parent = Split-Path -Parent $resolvedOutputPath
    if ($parent) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    [System.IO.File]::WriteAllLines(
        $resolvedOutputPath,
        [string[]]$lines,
        [System.Text.UTF8Encoding]::new($false)
    )
}
