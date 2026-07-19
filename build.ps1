#requires -Version 7.2

[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('restore', 'build', 'test', 'verify', 'policy', 'version', 'version-policy', 'runtime', 'runtime-policy', 'desktop', 'desktop-policy')]
    [string] $Target = 'verify',

    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',

    [Parameter()]
    [ValidateSet('Development', 'Preview', 'Stable')]
    [string] $BuildChannel = 'Development',

    [Parameter()]
    [ValidateRange(0, 60000)]
    [int] $RuntimeDurationMilliseconds = 0,

    [Parameter()]
    [ValidateRange(0, 60000)]
    [int] $DesktopDurationMilliseconds = 0
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

    'version' {
        & (Join-Path $PSScriptRoot 'eng\version.ps1')
    }

    'version-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-versioning.ps1')
    }

    'runtime' {
        & (Join-Path $PSScriptRoot 'eng\run-runtime.ps1') `
            -ShutdownAfterMilliseconds $RuntimeDurationMilliseconds
    }

    'runtime-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-runtime.ps1')
    }

    'desktop' {
        & (Join-Path $PSScriptRoot 'eng\run-desktop.ps1') `
            -CloseAfterMilliseconds $DesktopDurationMilliseconds
    }

    'desktop-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-desktop.ps1')
    }
}
