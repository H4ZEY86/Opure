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

$arguments = @(
    'build',
    (Join-Path $repositoryRoot 'Opure.slnx'),
    '--configuration',
    $Configuration,
    '--no-restore',
    "-p:OpureBuildChannel=$BuildChannel"
)

if ($ContinuousIntegration) {
    $arguments += '-p:ContinuousIntegrationBuild=true'
}

Push-Location $repositoryRoot
try {
    Invoke-OpureNativeCommand -FilePath 'dotnet' -Arguments $arguments
}
finally {
    Pop-Location
}
