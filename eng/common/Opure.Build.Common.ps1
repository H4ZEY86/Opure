Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

function Get-OpureRepositoryRoot {
    $root = [System.IO.Path]::GetFullPath(
        (Join-Path $PSScriptRoot '..\..')
    )

    if (-not (Test-Path -LiteralPath (Join-Path $root 'Opure.slnx') -PathType Leaf)) {
        throw "Opure.slnx was not found at the expected repository root: $root"
    }

    return $root
}

function Invoke-OpureNativeCommand {
    param(
        [Parameter(Mandatory)]
        [string] $FilePath,

        [Parameter()]
        [string[]] $Arguments = @()
    )

    & $FilePath @Arguments | Out-Host

    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $FilePath $($Arguments -join ' ')"
    }
}

function Assert-OpureBuildEnvironment {
    param(
        [Parameter(Mandatory)]
        [string] $RepositoryRoot
    )

    if ($PSVersionTable.PSVersion -lt [version]'7.2') {
        throw "PowerShell 7.2 or later is required. Current version: $($PSVersionTable.PSVersion)"
    }

    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw '.NET SDK was not found on PATH.'
    }

    if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
        throw 'Git was not found on PATH.'
    }

    $expectedSdk = [string](Get-Content -LiteralPath (Join-Path $RepositoryRoot 'global.json') -Raw | ConvertFrom-Json).sdk.version
    $actualSdk = (& dotnet --version).Trim()

    if ($actualSdk -ne $expectedSdk) {
        throw "SDK mismatch. global.json requires $expectedSdk, but dotnet resolved $actualSdk."
    }

    $gitRoot = (& git -C $RepositoryRoot rev-parse --show-toplevel).Trim()
    if ($LASTEXITCODE -ne 0) {
        throw "Git could not resolve the repository root: $RepositoryRoot"
    }

    $gitRoot = [System.IO.Path]::GetFullPath($gitRoot)
    if (-not $gitRoot.Equals($RepositoryRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Expected Git root $RepositoryRoot, but Git reported $gitRoot."
    }
}
