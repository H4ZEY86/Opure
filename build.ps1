#requires -Version 7.2
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('restore', 'build', 'test', 'verify')]
    [string] $Target = 'verify',

    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$solution = Join-Path $PSScriptRoot 'Opure.slnx'
if (-not (Test-Path -LiteralPath $solution)) {
    throw "Solution not found: $solution"
}

function Invoke-DotNet {
    param([Parameter(Mandatory)][string[]] $Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed with exit code ${LASTEXITCODE}: dotnet $($Arguments -join ' ')"
    }
}

switch ($Target) {
    'restore' {
        Invoke-DotNet @('restore', $solution)
    }

    'build' {
        Invoke-DotNet @('restore', $solution)
        Invoke-DotNet @('build', $solution, '--no-restore', '--configuration', $Configuration)
    }

    'test' {
        Invoke-DotNet @('restore', $solution)
        Invoke-DotNet @('build', $solution, '--no-restore', '--configuration', $Configuration)
        Invoke-DotNet @('test', $solution, '--no-build', '--configuration', $Configuration)
    }

    'verify' {
        Invoke-DotNet @('restore', $solution)
        Invoke-DotNet @('build', $solution, '--no-restore', '--configuration', $Configuration)
        Invoke-DotNet @('test', $solution, '--no-build', '--configuration', $Configuration)
    }
}