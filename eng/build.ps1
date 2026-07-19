#requires -Version 7.2

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',

    [Parameter()]
    [ValidateSet('Development', 'Preview', 'Stable')]
    [string] $BuildChannel = 'Development',

    [Parameter()]
    [switch] $ContinuousIntegration
)

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

$dirtyEntries = @(& git -C $repositoryRoot status --porcelain=v1)
if ($LASTEXITCODE -ne 0) {
    throw 'Git working-tree inspection failed.'
}

$sourceDirty = if ($dirtyEntries.Count -gt 0) { 'true' } else { 'false' }

if ($BuildChannel -ne 'Development' -and $sourceDirty -eq 'true') {
    throw "$BuildChannel builds require a clean Git working tree."
}

$buildSource = if ($env:GITHUB_ACTIONS -eq 'true') {
    'GitHubActions'
}
elseif ($env:CI -eq 'true') {
    'CI'
}
else {
    'Local'
}

$arguments = @(
    'build',
    (Join-Path $repositoryRoot 'Opure.slnx'),
    '--configuration',
    $Configuration,
    '--no-restore',
    "-p:OpureBuildChannel=$BuildChannel",
    "-p:OpureSourceDirty=$sourceDirty",
    "-p:OpureBuildSource=$buildSource"
)

if ($ContinuousIntegration) {
    $arguments += '-p:ContinuousIntegrationBuild=true'
}

Push-Location $repositoryRoot
try {
    Write-Host "Build identity: channel=$BuildChannel source=$buildSource dirty=$sourceDirty" -ForegroundColor DarkGray
    Invoke-OpureNativeCommand -FilePath 'dotnet' -Arguments $arguments
}
finally {
    Pop-Location
}
