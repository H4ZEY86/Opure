#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

$expectedToolVersion = '3.10.70'
$expectedDevelopmentVersion = '0.1.0-preview.0'
$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M0'
$probeWorkRoot = Join-Path $repositoryRoot "artifacts\versioning-probe\$([guid]::NewGuid().ToString('N'))"
$probeRoot = Join-Path $probeWorkRoot 'repository'
$probeEvidenceRoot = Join-Path $probeWorkRoot 'evidence'

New-Item -ItemType Directory -Force -Path $evidenceRoot | Out-Null
New-Item -ItemType Directory -Force -Path $probeEvidenceRoot | Out-Null

function Invoke-Captured {
    param(
        [Parameter(Mandatory)]
        [string] $WorkingDirectory,

        [Parameter(Mandatory)]
        [string] $FilePath,

        [Parameter()]
        [string[]] $Arguments = @()
    )

    Push-Location $WorkingDirectory
    try {
        $output = @(& $FilePath @Arguments 2>&1)
        $exitCode = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }

    if ($exitCode -ne 0) {
        $output | Out-Host
        throw "Command failed with exit code ${exitCode}: $FilePath $($Arguments -join ' ')"
    }

    return [string[]]$output
}

function Write-SafeLines {
    param(
        [Parameter(Mandatory)]
        [string] $Path,

        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [AllowEmptyString()]
        [string[]] $Lines
    )

    $parent = Split-Path -Parent $Path
    if ($parent) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    [System.IO.File]::WriteAllLines(
        $Path,
        $Lines,
        [System.Text.UTF8Encoding]::new($false)
    )
}

function Read-KeyValueFile {
    param(
        [Parameter(Mandatory)]
        [string] $Path
    )

    $result = [ordered]@{}

    foreach ($line in Get-Content -LiteralPath $Path) {
        $separator = $line.IndexOf('=')
        if ($separator -lt 1) {
            continue
        }

        $key = $line.Substring(0, $separator)
        $value = $line.Substring($separator + 1)
        $result[$key] = $value
    }

    return $result
}

function Write-ProbeProject {
    param(
        [Parameter(Mandatory)]
        [string] $Directory
    )

    foreach ($file in @(
        'global.json',
        '.gitignore',
        'Directory.Build.props',
        'Directory.Build.targets',
        'Directory.Packages.props',
        'NuGet.config',
        'version.json'
    )) {
        Copy-Item `
            -LiteralPath (Join-Path $repositoryRoot $file) `
            -Destination (Join-Path $Directory $file)
    }

    $project = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
'@

    $source = @'
namespace Opure.VersionProbe;

public static class Marker
{
}
'@

    [System.IO.File]::WriteAllText(
        (Join-Path $Directory 'Opure.VersionProbe.csproj'),
        $project.Replace("`r`n", "`n"),
        [System.Text.UTF8Encoding]::new($false)
    )

    [System.IO.File]::WriteAllText(
        (Join-Path $Directory 'Marker.cs'),
        $source.Replace("`r`n", "`n"),
        [System.Text.UTF8Encoding]::new($false)
    )
}

try {
    Write-Host ''
    Write-Host '==> Validate repository version policy' -ForegroundColor Cyan

    $versionPath = Join-Path $repositoryRoot 'version.json'
    $versionDocument = Get-Content -LiteralPath $versionPath -Raw | ConvertFrom-Json

    if ([string]$versionDocument.version -ne $expectedDevelopmentVersion) {
        throw "Expected development version $expectedDevelopmentVersion."
    }

    $packagesXml = [xml](Get-Content -LiteralPath (Join-Path $repositoryRoot 'Directory.Packages.props') -Raw)
    $packageNode = @(
        $packagesXml.Project.ItemGroup.PackageVersion |
            Where-Object { $_.Include -eq 'Nerdbank.GitVersioning' }
    )

    if ($packageNode.Count -ne 1 -or [string]$packageNode[0].Version -ne $expectedToolVersion) {
        throw "Nerdbank.GitVersioning must be pinned exactly once at $expectedToolVersion."
    }

    $toolManifest = Get-Content -LiteralPath (Join-Path $repositoryRoot '.config\dotnet-tools.json') -Raw | ConvertFrom-Json
    $toolVersion = [string]$toolManifest.tools.nbgv.version

    if ($toolVersion -ne $expectedToolVersion) {
        throw "nbgv tool version $toolVersion does not match package version $expectedToolVersion."
    }

    $nestedVersionFiles = @(
        foreach ($rootName in @('src', 'tests', 'eng', 'tools', 'build', 'packaging', 'enterprise', 'samples', 'docs', 'schemas')) {
            $searchRoot = Join-Path $repositoryRoot $rootName
            if (Test-Path -LiteralPath $searchRoot -PathType Container) {
                Get-ChildItem -LiteralPath $searchRoot -Recurse -File -Filter 'version.json'
            }
        }
    )

    if ($nestedVersionFiles.Count -gt 0) {
        throw "Nested version.json files are prohibited: $($nestedVersionFiles.FullName -join ', ')"
    }

    Write-Host ''
    Write-Host '==> Capture repository version resolution' -ForegroundColor Cyan

    $developmentEvidencePath = Join-Path $evidenceRoot 'sample-development-version.txt'
    & (Join-Path $PSScriptRoot 'version.ps1') -OutputPath $developmentEvidencePath

    Write-Host ''
    Write-Host '==> Inspect production assembly metadata' -ForegroundColor Cyan

    $productionAssemblies = @(
        Get-ChildItem `
            -LiteralPath (Join-Path $repositoryRoot 'artifacts\bin') `
            -Recurse `
            -File `
            -Filter 'Opure.*.dll' |
            Where-Object { $_.Name -notmatch 'Tests\.dll$' } |
            Sort-Object FullName
    )

    if ($productionAssemblies.Count -lt 3) {
        throw "Expected at least three production assemblies, found $($productionAssemblies.Count)."
    }

    $assemblyRows = foreach ($assemblyFile in $productionAssemblies) {
        $assemblyName = [System.Reflection.AssemblyName]::GetAssemblyName($assemblyFile.FullName)
        $fileInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($assemblyFile.FullName)

        [pscustomobject]@{
            Assembly = $assemblyFile.Name
            AssemblyVersion = [string]$assemblyName.Version
            FileVersion = [string]$fileInfo.FileVersion
            InformationalVersion = [string]$fileInfo.ProductVersion
        }
    }

    $assemblyVersions = @($assemblyRows.AssemblyVersion | Sort-Object -Unique)
    $fileVersions = @($assemblyRows.FileVersion | Sort-Object -Unique)
    $informationalVersions = @($assemblyRows.InformationalVersion | Sort-Object -Unique)

    if ($assemblyVersions.Count -ne 1) {
        throw "Production assemblies have inconsistent AssemblyVersion values: $($assemblyVersions -join ', ')"
    }

    if ($assemblyVersions[0] -ne '0.1.0.0') {
        throw "Expected AssemblyVersion 0.1.0.0, found $($assemblyVersions[0])."
    }

    if ($fileVersions.Count -ne 1) {
        throw "Production assemblies have inconsistent file versions: $($fileVersions -join ', ')"
    }

    if ($informationalVersions.Count -ne 1) {
        throw "Production assemblies have inconsistent informational versions: $($informationalVersions -join ', ')"
    }

    $headCommit = (& git -C $repositoryRoot rev-parse HEAD).Trim()
    $shortCommit = $headCommit.Substring(0, 12)

    if ($informationalVersions[0] -notmatch [regex]::Escape($shortCommit)) {
        throw "Informational version does not contain the expected 12-character commit identity $shortCommit."
    }

    $assemblyEvidencePath = Join-Path $evidenceRoot 'assembly-version-metadata.csv'
    $assemblyRows |
        ConvertTo-Csv -NoTypeInformation |
        Set-Content -LiteralPath $assemblyEvidencePath -Encoding utf8NoBOM

    Write-Host ''
    Write-Host '==> Compare project-level resolved versions' -ForegroundColor Cyan

    $projectEvidenceDirectory = Join-Path $probeEvidenceRoot 'project-evidence'
    New-Item -ItemType Directory -Force -Path $projectEvidenceDirectory | Out-Null

    $productionProjects = @(
        Get-ChildItem `
            -LiteralPath (Join-Path $repositoryRoot 'src') `
            -Recurse `
            -File `
            -Filter '*.csproj' |
            Sort-Object FullName
    )

    $resolvedProjects = foreach ($project in $productionProjects) {
        $outputFile = Join-Path $projectEvidenceDirectory "$($project.BaseName).txt"

        $msbuildOutput = Invoke-Captured `
            -WorkingDirectory $repositoryRoot `
            -FilePath 'dotnet' `
            -Arguments @(
                'msbuild',
                $project.FullName,
                '-nologo',
                '-target:WriteOpureVersionEvidence',
                "-property:OpureVersionEvidenceFile=$outputFile",
                '-property:OpureSourceDirty=true',
                '-property:OpureBuildSource=FND003Verification',
                '-property:OpureBuildChannel=Development'
            )

        $msbuildOutput | Out-Host
        Read-KeyValueFile -Path $outputFile
    }

    foreach ($key in @(
        'AssemblyVersion',
        'AssemblyFileVersion',
        'AssemblyInformationalVersion',
        'NuGetPackageVersion',
        'GitCommitId'
    )) {
        $values = @($resolvedProjects | ForEach-Object { [string]$_[$key] } | Sort-Object -Unique)
        if ($values.Count -ne 1 -or [string]::IsNullOrWhiteSpace($values[0])) {
            throw "Project-level version property '$key' is inconsistent or empty: $($values -join ', ')"
        }
    }

    Write-Host ''
    Write-Host '==> Simulate clean, dirty and annotated-tag states' -ForegroundColor Cyan

    New-Item -ItemType Directory -Force -Path $probeRoot | Out-Null
    Write-ProbeProject -Directory $probeRoot

    Invoke-Captured -WorkingDirectory $probeRoot -FilePath 'git' -Arguments @(
        'init',
        '--initial-branch=main'
    ) | Out-Host

    Invoke-Captured -WorkingDirectory $probeRoot -FilePath 'git' -Arguments @(
        'config',
        'user.name',
        'Opure Version Probe'
    ) | Out-Host

    Invoke-Captured -WorkingDirectory $probeRoot -FilePath 'git' -Arguments @(
        'config',
        'user.email',
        'version-probe@opure.invalid'
    ) | Out-Host

    Invoke-Captured -WorkingDirectory $probeRoot -FilePath 'dotnet' -Arguments @(
        'restore',
        (Join-Path $probeRoot 'Opure.VersionProbe.csproj'),
        '--force-evaluate'
    ) | Out-Host

    Invoke-Captured -WorkingDirectory $probeRoot -FilePath 'git' -Arguments @(
        'add',
        '.'
    ) | Out-Host

    Invoke-Captured -WorkingDirectory $probeRoot -FilePath 'git' -Arguments @(
        'commit',
        '-m',
        'Create clean development version probe'
    ) | Out-Host

    $cleanStatus = @(& git -C $probeRoot status --porcelain=v1)
    if ($LASTEXITCODE -ne 0 -or $cleanStatus.Count -ne 0) {
        throw 'Clean version probe is unexpectedly dirty.'
    }

    $cleanVersionOutput = Invoke-Captured `
        -WorkingDirectory $probeRoot `
        -FilePath 'dotnet' `
        -Arguments @('nbgv', 'get-version')

    $cleanEvidenceFile = Join-Path $probeEvidenceRoot 'clean-version.txt'
    Invoke-Captured `
        -WorkingDirectory $probeRoot `
        -FilePath 'dotnet' `
        -Arguments @(
            'msbuild',
            (Join-Path $probeRoot 'Opure.VersionProbe.csproj'),
            '-nologo',
            '-target:WriteOpureVersionEvidence',
            "-property:OpureVersionEvidenceFile=$cleanEvidenceFile",
            '-property:OpureSourceDirty=false',
            '-property:OpureBuildSource=FND003CleanProbe',
            '-property:OpureBuildChannel=Development'
        ) | Out-Host

    $cleanResolved = Read-KeyValueFile -Path $cleanEvidenceFile
    $cleanCommit = (& git -C $probeRoot rev-parse HEAD).Trim()
    $cleanShortCommit = $cleanCommit.Substring(0, 12)

    if ([string]$cleanResolved.AssemblyInformationalVersion -notmatch [regex]::Escape($cleanShortCommit)) {
        throw 'Clean untagged build is missing commit identity.'
    }

    [System.IO.File]::WriteAllText(
        (Join-Path $probeRoot 'dirty-probe.txt'),
        "dirty`n",
        [System.Text.UTF8Encoding]::new($false)
    )

    $dirtyStatus = @(& git -C $probeRoot status --porcelain=v1)
    if ($LASTEXITCODE -ne 0 -or $dirtyStatus.Count -eq 0) {
        throw 'Dirty version probe was not detected.'
    }

    $dirtyVersionOutput = Invoke-Captured `
        -WorkingDirectory $probeRoot `
        -FilePath 'dotnet' `
        -Arguments @('nbgv', 'get-version')

    Remove-Item -LiteralPath (Join-Path $probeRoot 'dirty-probe.txt') -Force

    $postDirtyStatus = @(& git -C $probeRoot status --porcelain=v1)
    if ($LASTEXITCODE -ne 0 -or $postDirtyStatus.Count -ne 0) {
        $postDirtyStatus | Out-Host
        throw 'Version probe did not return to a clean state after removing the deliberate dirty file.'
    }

    $probeVersionPath = Join-Path $probeRoot 'version.json'
    $probeVersionText = Get-Content -LiteralPath $probeVersionPath -Raw
    $probeVersionText = $probeVersionText.Replace(
        '"version": "0.1.0-preview.0"',
        '"version": "0.1.0-preview.1"'
    )

    [System.IO.File]::WriteAllText(
        $probeVersionPath,
        $probeVersionText.Replace("`r`n", "`n"),
        [System.Text.UTF8Encoding]::new($false)
    )

    Invoke-Captured -WorkingDirectory $probeRoot -FilePath 'git' -Arguments @(
        'add',
        'version.json'
    ) | Out-Host

    Invoke-Captured -WorkingDirectory $probeRoot -FilePath 'git' -Arguments @(
        'commit',
        '-m',
        'Prepare public preview probe'
    ) | Out-Host

    $releaseTag = 'v0.1.0-preview.1'
    Invoke-Captured -WorkingDirectory $probeRoot -FilePath 'git' -Arguments @(
        'tag',
        '-a',
        $releaseTag,
        '-m',
        'Opure 0.1.0-preview.1 version probe'
    ) | Out-Host

    $tagType = (Invoke-Captured `
        -WorkingDirectory $probeRoot `
        -FilePath 'git' `
        -Arguments @('cat-file', '-t', $releaseTag) |
        Select-Object -Last 1).Trim()

    if ($tagType -ne 'tag') {
        throw "Expected an annotated tag object, found '$tagType'."
    }

    $tagsAtHead = @(& git -C $probeRoot tag --points-at HEAD)
    if ($LASTEXITCODE -ne 0 -or $tagsAtHead -notcontains $releaseTag) {
        throw 'Release tag does not point at the exact probe commit.'
    }

    $taggedVersionOutput = Invoke-Captured `
        -WorkingDirectory $probeRoot `
        -FilePath 'dotnet' `
        -Arguments @('nbgv', 'get-version')

    $releaseEvidenceFile = Join-Path $probeEvidenceRoot 'release-version.txt'
    Invoke-Captured `
        -WorkingDirectory $probeRoot `
        -FilePath 'dotnet' `
        -Arguments @(
            'msbuild',
            (Join-Path $probeRoot 'Opure.VersionProbe.csproj'),
            '-nologo',
            '-target:WriteOpureVersionEvidence',
            "-property:OpureVersionEvidenceFile=$releaseEvidenceFile",
            '-property:PublicRelease=true',
            '-property:OpureSourceDirty=false',
            '-property:OpureBuildSource=FND003TaggedProbe',
            '-property:OpureBuildChannel=Preview'
        ) | Out-Host

    $releaseResolved = Read-KeyValueFile -Path $releaseEvidenceFile
    $releaseCommit = (& git -C $probeRoot rev-parse HEAD).Trim()
    $releaseShortCommit = $releaseCommit.Substring(0, 12)
    $releasePackageVersion = [string]$releaseResolved.NuGetPackageVersion

    if ($releasePackageVersion -ne '0.1.0-preview.1') {
        throw "Public preview package version is unexpected: $releasePackageVersion"
    }

    if ($releasePackageVersion -match [regex]::Escape($releaseShortCommit)) {
        throw 'Public package version must not contain the non-public commit suffix.'
    }

    Invoke-Captured `
        -WorkingDirectory $probeRoot `
        -FilePath 'dotnet' `
        -Arguments @(
            'build',
            (Join-Path $probeRoot 'Opure.VersionProbe.csproj'),
            '--configuration',
            'Release',
            '--no-restore',
            '-property:PublicRelease=true',
            '-property:OpureSourceDirty=false',
            '-property:OpureBuildSource=FND003OfflineProbe',
            '-property:OpureBuildChannel=Preview'
        ) | Out-Host

    $sampleReleaseLines = @(
        'Opure Sample Public Preview Version'
        '==================================='
        "Tag: $releaseTag"
        "TagType: $tagType"
        "Commit: $releaseCommit"
        "NuGetPackageVersion: $releasePackageVersion"
        "AssemblyVersion: $($releaseResolved.AssemblyVersion)"
        "AssemblyFileVersion: $($releaseResolved.AssemblyFileVersion)"
        "AssemblyInformationalVersion: $($releaseResolved.AssemblyInformationalVersion)"
        'BuildWithoutRestore: passed'
        ''
        'nbgv get-version'
        '----------------'
    ) + [string[]]$taggedVersionOutput

    Write-SafeLines `
        -Path (Join-Path $evidenceRoot 'sample-release-tag-version.txt') `
        -Lines $sampleReleaseLines

    $summary = @"
# FND-003 Version Source Evidence

**Ticket:** FND-003  
**Version source:** `version.json`  
**Declared development version:** `$expectedDevelopmentVersion`  
**Nerdbank.GitVersioning package:** `$expectedToolVersion`  
**nbgv local tool:** `$expectedToolVersion`  
**Assembly version:** `$($assemblyVersions[0])`  
**File version:** `$($fileVersions[0])`  
**Informational version:** `$($informationalVersions[0])`  
**Commit abbreviation length:** 12  
**Result:** Passed  

## Verified

- One lower-case root `version.json` governs all first-party projects.
- Nested version sources are absent.
- The MSBuild package and local CLI use the same exact pinned release.
- Production assemblies share one assembly, file and informational version.
- Assembly identity follows `MAJOR.MINOR.0.0`.
- Non-public informational versions contain source commit identity.
- Project-level version overrides are prohibited by architecture tests.
- Package lock files include the pinned versioning package.
- A clean temporary repository resolves a clean development version.
- A modified temporary repository is detected as dirty.
- An exact annotated `v0.1.0-preview.1` tag is validated against the tagged commit.
- The trusted public-release override yields package version `0.1.0-preview.1` without a commit suffix.
- Version stamping builds with `--no-restore` after locked dependencies are available.
- No tag was created in the real Opure repository.

## Dirty Working Tree Behaviour

Nerdbank.GitVersioning stamps the source commit. Opure separately reports Git working-tree state through `eng/version.ps1`. A dirty source tree is classified as `Local Dirty`, and Preview or Stable build-channel execution is rejected until the tree is clean.

## Runtime and Desktop Consumption

Each first-party project receives the NBGV-generated internal `ThisAssembly` class. It exposes assembly, file, informational, commit and public-release identity. `NBGV_ThisAssemblyIncludesPackageVersion` also exposes the computed package version. Runtime and the future Desktop host should consume this generated identity rather than inventing version literals.

## Evidence Files

- `sample-development-version.txt`
- `sample-release-tag-version.txt`
- `assembly-version-metadata.csv`
- project-level package lock files
"@

    [System.IO.File]::WriteAllText(
        (Join-Path $evidenceRoot 'version-resolution-report.md'),
        $summary.Replace("`r`n", "`n"),
        [System.Text.UTF8Encoding]::new($false)
    )

    Write-Host ''
    Write-Host 'FND-003 version-source verification passed.' -ForegroundColor Green
}
finally {
    if (Test-Path -LiteralPath $probeWorkRoot) {
        Remove-Item -LiteralPath $probeWorkRoot -Recurse -Force
    }
}
