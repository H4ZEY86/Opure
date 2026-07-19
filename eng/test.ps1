#requires -Version 7.2

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',

    [Parameter()]
    [ValidateSet('Development', 'Preview', 'Stable')]
    [string] $BuildChannel = 'Development'
)

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

Push-Location $repositoryRoot
try {
    Invoke-OpureNativeCommand -FilePath 'dotnet' -Arguments @(
        'test',
        (Join-Path $repositoryRoot 'Opure.slnx'),
        '--configuration',
        $Configuration,
        '--no-build',
        "-p:OpureBuildChannel=$BuildChannel"
    )
}
finally {
    Pop-Location
}
