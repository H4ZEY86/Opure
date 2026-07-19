#requires -Version 7.2

[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('restore', 'build', 'test', 'verify', 'policy')]
    [string] $Target = 'verify',

    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',

    [Parameter()]
    [ValidateSet('Development', 'Preview', 'Stable')]
    [string] $BuildChannel = 'Development'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

switch ($Target) {
    'restore' {
        & (Join-Path $PSScriptRoot 'eng\restore.ps1') -Locked
    }

    'build' {
        & (Join-Path $PSScriptRoot 'eng\restore.ps1') -Locked
        & (Join-Path $PSScriptRoot 'eng\build.ps1') `
            -Configuration $Configuration `
            -BuildChannel $BuildChannel
    }

    'test' {
        & (Join-Path $PSScriptRoot 'eng\verify.ps1') `
            -Configuration $Configuration `
            -BuildChannel $BuildChannel
    }

    'verify' {
        & (Join-Path $PSScriptRoot 'eng\verify.ps1') `
            -Configuration $Configuration `
            -BuildChannel $BuildChannel
    }

    'policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-build-policy.ps1')
    }
}