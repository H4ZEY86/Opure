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

$restoreScript = Join-Path $PSScriptRoot 'restore.ps1'
$buildScript = Join-Path $PSScriptRoot 'build.ps1'
$testScript = Join-Path $PSScriptRoot 'test.ps1'

& $restoreScript -Locked
& $buildScript `
    -Configuration $Configuration `
    -BuildChannel $BuildChannel
& $testScript `
    -Configuration $Configuration `
    -BuildChannel $BuildChannel
