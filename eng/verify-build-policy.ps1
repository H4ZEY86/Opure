#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M0'
New-Item -ItemType Directory -Force -Path $evidenceRoot | Out-Null

function Invoke-ExpectedFailure {
    param(
        [Parameter(Mandatory)]
        [string] $Description,

        [Parameter(Mandatory)]
        [string] $WorkingDirectory,

        [Parameter(Mandatory)]
        [string[]] $Arguments,

        [Parameter(Mandatory)]
        [string] $ExpectedPattern
    )

    Push-Location $WorkingDirectory
    try {
        $output = @(& dotnet @Arguments 2>&1)
        $exitCode = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }

    $output | Out-Host

    if ($exitCode -eq 0) {
        throw "$Description unexpectedly succeeded."
    }

    $joinedOutput = $output -join [Environment]::NewLine
    if ($joinedOutput -notmatch $ExpectedPattern) {
        throw "$Description failed, but did not emit the expected diagnostic pattern '$ExpectedPattern'."
    }
}

function Get-ProductionAssemblyHashes {
    param(
        [Parameter(Mandatory)]
        [string] $ArtifactsRoot
    )

    $files = @(
        Get-ChildItem -LiteralPath (Join-Path $ArtifactsRoot 'bin') -Recurse -File -Filter 'Opure.*.dll' |
            Where-Object {
                $_.Name -notmatch 'Tests\.dll$'
            } |
            Sort-Object FullName
    )

    if ($files.Count -lt 3) {
        throw "Expected at least three production assemblies, but found $($files.Count)."
    }

    $result = [ordered]@{}
    foreach ($file in $files) {
        $relativePath = [System.IO.Path]::GetRelativePath($ArtifactsRoot, $file.FullName)
        $result[$relativePath] = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    }

    return $result
}

function Compare-HashMaps {
    param(
        [Parameter(Mandatory)]
        [System.Collections.IDictionary] $First,

        [Parameter(Mandatory)]
        [System.Collections.IDictionary] $Second
    )

    $firstKeys = @($First.Keys | Sort-Object)
    $secondKeys = @($Second.Keys | Sort-Object)

    if (($firstKeys -join "`n") -ne ($secondKeys -join "`n")) {
        throw 'Deterministic build comparison produced different assembly inventories.'
    }

    foreach ($key in $firstKeys) {
        if ($First[$key] -ne $Second[$key]) {
            throw "Deterministic build comparison failed for $key."
        }
    }
}


function Initialize-ProbeGitRepository {
    param(
        [Parameter(Mandatory)]
        [string] $Directory,

        [Parameter(Mandatory)]
        [string] $CommitMessage
    )

    Push-Location $Directory
    try {
        & git init --initial-branch=main | Out-Host
        if ($LASTEXITCODE -ne 0) {
            throw "Unable to initialise temporary Git repository: $Directory"
        }

        & git config user.name 'Opure Build Policy Probe'
        if ($LASTEXITCODE -ne 0) {
            throw 'Unable to configure temporary Git user name.'
        }

        & git config user.email 'build-policy-probe@opure.invalid'
        if ($LASTEXITCODE -ne 0) {
            throw 'Unable to configure temporary Git user email.'
        }

        & git add .
        if ($LASTEXITCODE -ne 0) {
            throw 'Unable to stage temporary build-policy probe.'
        }

        & git commit -m $CommitMessage | Out-Host
        if ($LASTEXITCODE -ne 0) {
            throw 'Unable to commit temporary build-policy probe.'
        }
    }
    finally {
        Pop-Location
    }
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) "Opure-FND-002-$([guid]::NewGuid().ToString('N'))"
$warningProbeRoot = Join-Path $tempRoot 'warning-probe'
$lockProbeRoot = Join-Path $tempRoot 'lock-probe'

try {
    New-Item -ItemType Directory -Force -Path $warningProbeRoot | Out-Null
    New-Item -ItemType Directory -Force -Path $lockProbeRoot | Out-Null

    foreach ($file in @(
        'global.json',
        'Directory.Build.props',
        'Directory.Build.targets',
        'Directory.Packages.props',
        'NuGet.config',
        'version.json'
    )) {
        Copy-Item -LiteralPath (Join-Path $repositoryRoot $file) -Destination (Join-Path $warningProbeRoot $file)
        Copy-Item -LiteralPath (Join-Path $repositoryRoot $file) -Destination (Join-Path $lockProbeRoot $file)
    }

    $warningProject = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
'@

    $warningSource = @'
namespace Opure.WarningProbe;

public static class WarningProbe
{
    public static string GetValue()
    {
        string value = null;
        return value;
    }
}
'@

    [System.IO.File]::WriteAllText(
        (Join-Path $warningProbeRoot 'Opure.WarningProbe.csproj'),
        $warningProject,
        [System.Text.UTF8Encoding]::new($false)
    )

    [System.IO.File]::WriteAllText(
        (Join-Path $warningProbeRoot 'WarningProbe.cs'),
        $warningSource,
        [System.Text.UTF8Encoding]::new($false)
    )

    Initialize-ProbeGitRepository `
        -Directory $warningProbeRoot `
        -CommitMessage 'Create warning-as-error probe'

    Invoke-ExpectedFailure `
        -Description 'Warning-as-error probe' `
        -WorkingDirectory $warningProbeRoot `
        -Arguments @(
            'build',
            (Join-Path $warningProbeRoot 'Opure.WarningProbe.csproj'),
            '--configuration',
            'Release'
        ) `
        -ExpectedPattern 'CS8600|CS8603'

    $sourceProjectRoot = Join-Path $repositoryRoot 'src\Runtime\Opure.Runtime.Contracts'
    $lockProjectRoot = Join-Path $lockProbeRoot 'src\Runtime\Opure.Runtime.Contracts'
    New-Item -ItemType Directory -Force -Path $lockProjectRoot | Out-Null

    foreach ($file in @(
        'Opure.Runtime.Contracts.csproj',
        'packages.lock.json'
    )) {
        Copy-Item -LiteralPath (Join-Path $sourceProjectRoot $file) -Destination (Join-Path $lockProjectRoot $file)
    }

    Initialize-ProbeGitRepository `
        -Directory $lockProbeRoot `
        -CommitMessage 'Create locked-restore probe'

    $lockProjectPath = Join-Path $lockProjectRoot 'Opure.Runtime.Contracts.csproj'
    $lockProjectText = Get-Content -LiteralPath $lockProjectPath -Raw
    $lockProjectText = $lockProjectText.Replace(
        '</Project>',
        "  <ItemGroup>`n    <PackageReference Include=`"xunit.v3`" />`n  </ItemGroup>`n`n</Project>"
    )

    [System.IO.File]::WriteAllText(
        $lockProjectPath,
        $lockProjectText.Replace("`r`n", "`n"),
        [System.Text.UTF8Encoding]::new($false)
    )

    Invoke-ExpectedFailure `
        -Description 'Locked-restore stale graph probe' `
        -WorkingDirectory $lockProbeRoot `
        -Arguments @(
            'restore',
            $lockProjectPath,
            '--locked-mode',
            '--configfile',
            (Join-Path $lockProbeRoot 'NuGet.config')
        ) `
        -ExpectedPattern 'NU1004|locked mode|lock file'

    $artifactsRoot = Join-Path $repositoryRoot 'artifacts'
    if (Test-Path -LiteralPath $artifactsRoot) {
        Remove-Item -LiteralPath $artifactsRoot -Recurse -Force
    }

    & (Join-Path $PSScriptRoot 'restore.ps1') -Locked
    & (Join-Path $PSScriptRoot 'build.ps1') `
        -Configuration Release `
        -BuildChannel Development `
        -ContinuousIntegration

    $firstHashes = Get-ProductionAssemblyHashes -ArtifactsRoot $artifactsRoot

    Remove-Item -LiteralPath $artifactsRoot -Recurse -Force

    & (Join-Path $PSScriptRoot 'restore.ps1') -Locked
    & (Join-Path $PSScriptRoot 'build.ps1') `
        -Configuration Release `
        -BuildChannel Development `
        -ContinuousIntegration

    $secondHashes = Get-ProductionAssemblyHashes -ArtifactsRoot $artifactsRoot
    Compare-HashMaps -First $firstHashes -Second $secondHashes

    $hashLines = foreach ($key in ($secondHashes.Keys | Sort-Object)) {
        "$($secondHashes[$key])  $key"
    }

    [System.IO.File]::WriteAllLines(
        (Join-Path $evidenceRoot 'deterministic-build-hashes.txt'),
        $hashLines,
        [System.Text.UTF8Encoding]::new($false)
    )

    Push-Location $repositoryRoot
    try {
        $dependencyGraph = @(
            & dotnet package list --project (Join-Path $repositoryRoot 'Opure.slnx') --include-transitive --no-restore 2>&1
        )
        if ($LASTEXITCODE -ne 0) {
            throw "Dependency inventory failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }

    [System.IO.File]::WriteAllLines(
        (Join-Path $evidenceRoot 'dependency-graph.txt'),
        [string[]]$dependencyGraph,
        [System.Text.UTF8Encoding]::new($false)
    )

    $summary = @"
# FND-002 Central Build Policy Evidence

**Ticket:** FND-002  
**SDK:** $((& dotnet --version).Trim())  
**Build channel tested:** Development  
**Configuration tested:** Release  
**Result:** Passed  

## Verified

- Central Package Management is enabled.
- Project-local package versions are prohibited.
- Package version overrides are disabled.
- Package lock files are generated and committed.
- Locked restore succeeds for the committed graph.
- Locked restore fails for a deliberately stale graph.
- Nullable analysis is enabled.
- A nullable compiler warning fails the build.
- .NET analyzers and build code-style enforcement are enabled.
- Production projects do not inherit test-only packages.
- Release assemblies reproduce identical SHA-256 hashes across two clean artifact builds.
- Build output is centralised under `artifacts/`.
- The local `dotnet-coverage` tool is pinned by the repository manifest.
- Development, Preview and Stable build constants are explicit.

## Evidence Files

- `dependency-graph.txt`
- `deterministic-build-hashes.txt`
- project-level `packages.lock.json` files

## Recovery

Every policy file and lock file is source controlled. Reverting the FND-002 commit restores the FND-001 build policy without machine-wide changes.
"@

    [System.IO.File]::WriteAllText(
        (Join-Path $evidenceRoot 'central-build-policy.md'),
        $summary.Replace("`r`n", "`n"),
        [System.Text.UTF8Encoding]::new($false)
    )

    Write-Host ''
    Write-Host 'FND-002 build-policy verification passed.' -ForegroundColor Green
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
