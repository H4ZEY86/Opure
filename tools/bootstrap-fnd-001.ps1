#requires -Version 7.2
<#
.SYNOPSIS
Creates the FND-001 Opure solution baseline.

.DESCRIPTION
Run this script from outside C:\Opure. It verifies that the repository is clean,
selects an installed stable .NET 10 SDK, pins it in global.json, creates Opure.slnx,
creates the initial projects, adds a minimal build contract, verifies the solution,
and writes safe M0 evidence.

The script is deliberately conservative:
- it does not overwrite existing canonical files;
- it refuses a dirty repository unless -AllowDirty is supplied;
- it refuses a legacy Opure.sln instead of silently choosing a solution format;
- it does not create the Desktop project yet;
- it does not add central package management yet (FND-002).

.PARAMETER RepositoryRoot
The Opure repository root. Defaults to C:\Opure.

.PARAMETER SdkVersion
An exact installed stable .NET 10 SDK version. When omitted, the highest installed
stable 10.0 SDK is selected.

.PARAMETER AllowDirty
Allows execution when the Git working tree is not clean. Intended only for a safe
rerun after an interrupted first attempt.

.EXAMPLE
.\FND-001-create-solution-baseline.ps1 -RepositoryRoot C:\Opure
#>

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $RepositoryRoot = 'C:\Opure',

    [Parameter()]
    [ValidatePattern('^10\.0\.\d+$')]
    [string] $SdkVersion,

    [Parameter()]
    [switch] $AllowDirty
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

function Write-Step {
    param([Parameter(Mandatory)][string] $Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Invoke-Checked {
    param(
        [Parameter(Mandatory)][string] $FilePath,
        [Parameter()][string[]] $Arguments = @()
    )

    & $FilePath @Arguments | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $FilePath $($Arguments -join ' ')"
    }
}

function Write-NewUtf8File {
    param(
        [Parameter(Mandatory)][string] $Path,
        [Parameter(Mandatory)][AllowEmptyString()][string] $Content
    )

    if (Test-Path -LiteralPath $Path) {
        Write-Host "Keeping existing file: $Path" -ForegroundColor DarkGray
        return
    }

    $parent = Split-Path -Parent $Path
    if ($parent) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    [System.IO.File]::WriteAllText(
        $Path,
        $Content.Replace("`r`n", "`n"),
        [System.Text.UTF8Encoding]::new($false)
    )

    Write-Host "Created: $Path" -ForegroundColor Green
}

function Ensure-EmptyDirectoryOrProject {
    param(
        [Parameter(Mandatory)][string] $Directory,
        [Parameter(Mandatory)][string] $ExpectedProject
    )

    if (-not (Test-Path -LiteralPath $Directory)) {
        return
    }

    if (Test-Path -LiteralPath $ExpectedProject) {
        return
    }

    $entries = @(Get-ChildItem -LiteralPath $Directory -Force)
    if ($entries.Count -gt 0) {
        throw "Refusing to create a project in non-empty directory without expected project file: $Directory"
    }
}

function Ensure-DotNetProject {
    param(
        [Parameter(Mandatory)][string] $Template,
        [Parameter(Mandatory)][string] $Name,
        [Parameter(Mandatory)][string] $Directory
    )

    $project = Join-Path $Directory "$Name.csproj"
    Ensure-EmptyDirectoryOrProject -Directory $Directory -ExpectedProject $project

    if (Test-Path -LiteralPath $project) {
        Write-Host "Keeping existing project: $project" -ForegroundColor DarkGray
        return $project
    }

    Invoke-Checked -FilePath 'dotnet' -Arguments @(
        'new', $Template,
        '--name', $Name,
        '--output', $Directory,
        '--framework', 'net10.0',
        '--language', 'C#',
        '--no-restore'
    )

    if (-not (Test-Path -LiteralPath $project)) {
        throw "Project template completed but expected project was not found: $project"
    }

    return $project
}

function Ensure-ProjectInSolution {
    param(
        [Parameter(Mandatory)][string] $Solution,
        [Parameter(Mandatory)][string] $Project
    )

    $solutionText = Get-Content -LiteralPath $Solution -Raw
    $projectFile = [System.IO.Path]::GetFileName($Project)

    if ($solutionText.Contains($projectFile, [System.StringComparison]::OrdinalIgnoreCase)) {
        Write-Host "Project already in solution: $projectFile" -ForegroundColor DarkGray
        return
    }

    Invoke-Checked -FilePath 'dotnet' -Arguments @('sln', $Solution, 'add', $Project)
}

Write-Step "Validate repository"

$resolvedRoot = [System.IO.Path]::GetFullPath($RepositoryRoot)
if (-not (Test-Path -LiteralPath $resolvedRoot -PathType Container)) {
    throw "Repository root does not exist: $resolvedRoot"
}

Push-Location $resolvedRoot
try {
    if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
        throw 'Git was not found on PATH.'
    }

    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw '.NET SDK was not found on PATH.'
    }

    $gitTop = (& git rev-parse --show-toplevel 2>$null)
    if ($LASTEXITCODE -ne 0 -or -not $gitTop) {
        throw "The target directory is not a Git repository: $resolvedRoot"
    }

    $gitTop = [System.IO.Path]::GetFullPath($gitTop.Trim())
    if (-not $gitTop.Equals($resolvedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "RepositoryRoot must be the Git top-level directory. Git reports: $gitTop"
    }

    $dirty = @(& git status --porcelain=v1)
    if (-not $AllowDirty -and $dirty.Count -gt 0) {
        throw @"
The Git working tree is not clean.

Run:
  git status

Resolve or commit the existing changes, then rerun this script.
Use -AllowDirty only for a reviewed rerun after an interrupted FND-001 attempt.
"@
    }

    Write-Step "Select and pin a stable .NET 10 SDK"

    $installedSdkStrings = @(
        & dotnet --list-sdks |
            ForEach-Object { ($_ -split '\s+')[0] } |
            Where-Object { $_ -match '^10\.0\.\d+$' }
    )

    if ($installedSdkStrings.Count -eq 0) {
        throw 'No stable .NET 10 SDK is installed. Install a stable .NET 10 SDK, then rerun.'
    }

    if ($SdkVersion) {
        if ($installedSdkStrings -notcontains $SdkVersion) {
            throw "Requested SDK $SdkVersion is not installed. Installed stable .NET 10 SDKs: $($installedSdkStrings -join ', ')"
        }
        $selectedSdk = $SdkVersion
    }
    else {
        $selectedSdk = (
            $installedSdkStrings |
                Sort-Object { [version]$_ } -Descending |
                Select-Object -First 1
        )
    }

    Write-Host "Selected SDK: $selectedSdk" -ForegroundColor Green

    $globalJsonPath = Join-Path $resolvedRoot 'global.json'
    $globalJson = @"
{
  "sdk": {
    "version": "$selectedSdk",
    "rollForward": "disable",
    "allowPrerelease": false
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
"@

    if (Test-Path -LiteralPath $globalJsonPath) {
        $existingGlobal = Get-Content -LiteralPath $globalJsonPath -Raw | ConvertFrom-Json
        $existingVersion = [string]$existingGlobal.sdk.version
        if ($existingVersion -ne $selectedSdk) {
            throw "Existing global.json pins SDK $existingVersion, but this run selected $selectedSdk. Review the file manually."
        }
        Write-Host "Keeping existing global.json: $existingVersion" -ForegroundColor DarkGray
    }
    else {
        Write-NewUtf8File -Path $globalJsonPath -Content $globalJson
    }

    $resolvedSdk = (& dotnet --version).Trim()
    if ($resolvedSdk -ne $selectedSdk) {
        throw "global.json did not resolve the selected SDK. Expected $selectedSdk, got $resolvedSdk."
    }

    Write-Step "Create canonical directories"

    $directories = @(
        'src',
        'src\Bootstrap',
        'src\Runtime',
        'tests',
        'tests\Architecture',
        'tools',
        'build',
        'eng',
        'eng\evidence',
        'eng\evidence\milestones',
        'eng\evidence\milestones\M0',
        'packaging',
        'enterprise',
        'samples',
        'docs',
        'schemas'
    )

    foreach ($relative in $directories) {
        New-Item -ItemType Directory -Force -Path (Join-Path $resolvedRoot $relative) | Out-Null
    }

    foreach ($relative in @('build', 'packaging', 'enterprise', 'samples', 'docs', 'schemas')) {
        $keep = Join-Path (Join-Path $resolvedRoot $relative) '.gitkeep'
        Write-NewUtf8File -Path $keep -Content ''
    }

    Write-Step "Create repository support files when absent"

    $editorConfig = @'
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true

[*.{cs,csproj,props,targets}]
indent_style = space
indent_size = 4

[*.{json,jsonc,yml,yaml,md}]
indent_style = space
indent_size = 2

[*.ps1]
indent_style = space
indent_size = 4
'@

    $gitAttributes = @'
* text=auto

*.cs text eol=lf
*.csproj text eol=lf
*.props text eol=lf
*.targets text eol=lf
*.json text eol=lf
*.md text eol=lf
*.ps1 text eol=lf
*.slnx text eol=lf
'@

    $gitIgnore = @'
# .NET build output
**/bin/
**/obj/
artifacts/
TestResults/

# IDE and user state
.vs/
.idea/
*.user
*.suo
*.userosscache
*.sln.docstates

# Coverage and test output
coverage/
coverage.*
*.trx

# Local tooling
.tools/
.dotnet/

# OS files
Thumbs.db
Desktop.ini
.DS_Store

# Temporary files
*.tmp
*.temp
*.bak
'@

    Write-NewUtf8File -Path (Join-Path $resolvedRoot '.editorconfig') -Content $editorConfig
    Write-NewUtf8File -Path (Join-Path $resolvedRoot '.gitattributes') -Content $gitAttributes
    Write-NewUtf8File -Path (Join-Path $resolvedRoot '.gitignore') -Content $gitIgnore

    Write-Step "Create Opure.slnx"

    $solution = Join-Path $resolvedRoot 'Opure.slnx'
    $legacySolution = Join-Path $resolvedRoot 'Opure.sln'

    if ((Test-Path -LiteralPath $legacySolution) -and -not (Test-Path -LiteralPath $solution)) {
        throw "Legacy Opure.sln exists. Review and migrate it explicitly with 'dotnet sln Opure.sln migrate'."
    }

    if (-not (Test-Path -LiteralPath $solution)) {
        Invoke-Checked -FilePath 'dotnet' -Arguments @('new', 'sln', '--name', 'Opure', '--output', $resolvedRoot)
    }

    if (-not (Test-Path -LiteralPath $solution)) {
        throw "Expected .NET 10 to create Opure.slnx, but the file was not found."
    }

    Write-Step "Create initial production projects"

    [string] $contractsProject = Ensure-DotNetProject `
        -Template 'classlib' `
        -Name 'Opure.Runtime.Contracts' `
        -Directory (Join-Path $resolvedRoot 'src\Runtime\Opure.Runtime.Contracts')

    [string] $runtimeProject = Ensure-DotNetProject `
        -Template 'console' `
        -Name 'Opure.Runtime' `
        -Directory (Join-Path $resolvedRoot 'src\Runtime\Opure.Runtime')

    [string] $bootstrapProject = Ensure-DotNetProject `
        -Template 'console' `
        -Name 'Opure.Bootstrap.Windows' `
        -Directory (Join-Path $resolvedRoot 'src\Bootstrap\Opure.Bootstrap.Windows')

    Write-Step "Create xUnit v3 architecture test project"

    $architectureDirectory = Join-Path $resolvedRoot 'tests\Architecture\Opure.ArchitectureTests'
    $architectureProject = Join-Path $architectureDirectory 'Opure.ArchitectureTests.csproj'
    Ensure-EmptyDirectoryOrProject -Directory $architectureDirectory -ExpectedProject $architectureProject
    New-Item -ItemType Directory -Force -Path $architectureDirectory | Out-Null

    $architectureCsproj = @'
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.8.1" />
    <PackageReference Include="xunit.v3" Version="3.2.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

</Project>
'@

    $architectureTest = @'
using Xunit;

namespace Opure.ArchitectureTests;

public sealed class SolutionBaselineTests
{
    [Fact]
    public void Baseline_test_runner_is_operational()
    {
        Assert.True(true);
    }
}
'@

    Write-NewUtf8File -Path $architectureProject -Content $architectureCsproj
    Write-NewUtf8File -Path (Join-Path $architectureDirectory 'SolutionBaselineTests.cs') -Content $architectureTest

    $templateUnitTest = Join-Path $architectureDirectory 'UnitTest1.cs'
    if (Test-Path -LiteralPath $templateUnitTest) {
        Remove-Item -LiteralPath $templateUnitTest -Force
    }

    Write-Step "Add project references"

    $runtimeProjectText = Get-Content -LiteralPath $runtimeProject -Raw
    if (-not $runtimeProjectText.Contains('Opure.Runtime.Contracts.csproj', [System.StringComparison]::OrdinalIgnoreCase)) {
        Invoke-Checked -FilePath 'dotnet' -Arguments @(
            'add', $runtimeProject,
            'reference', $contractsProject
        )
    }
    else {
        Write-Host "Runtime already references Runtime.Contracts." -ForegroundColor DarkGray
    }

    Write-Step "Add projects to Opure.slnx"

    foreach ($project in @($contractsProject, $runtimeProject, $bootstrapProject, $architectureProject)) {
        Ensure-ProjectInSolution -Solution $solution -Project $project
    }

    Write-Step "Create the minimal build contract"

    $buildScriptPath = Join-Path $resolvedRoot 'build.ps1'
    $buildScript = @'
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
'@

    Write-NewUtf8File -Path $buildScriptPath -Content $buildScript

    Write-Step "Copy this bootstrap script into tools"

    $canonicalBootstrap = Join-Path $resolvedRoot 'tools\bootstrap-fnd-001.ps1'
    if (-not (Test-Path -LiteralPath $canonicalBootstrap)) {
        Copy-Item -LiteralPath $PSCommandPath -Destination $canonicalBootstrap
        Write-Host "Created: $canonicalBootstrap" -ForegroundColor Green
    }
    else {
        Write-Host "Keeping existing file: $canonicalBootstrap" -ForegroundColor DarkGray
    }

    Write-Step "Restore, build and test"

    & $buildScriptPath verify
    if ($LASTEXITCODE -ne 0) {
        throw "The FND-001 verification build failed."
    }

    Write-Step "Write safe M0 evidence"

    $evidenceDirectory = Join-Path $resolvedRoot 'eng\evidence\milestones\M0'
    New-Item -ItemType Directory -Force -Path $evidenceDirectory | Out-Null

    $dotnetInfo = (& dotnet --info | Out-String).TrimEnd()
    Write-NewUtf8File -Path (Join-Path $evidenceDirectory 'dotnet-info.txt') -Content ($dotnetInfo + "`n")

    $solutionList = (& dotnet sln $solution list | Out-String).TrimEnd()
    Write-NewUtf8File -Path (Join-Path $evidenceDirectory 'solution-projects.txt') -Content ($solutionList + "`n")

    $evidenceDate = (Get-Date).ToString('yyyy-MM-ddTHH:mm:ssK')
    $evidenceMarkdown = @"
# M0 Solution Baseline Evidence

**Ticket:** FND-001  
**Generated:** $evidenceDate  
**SDK:** $selectedSdk  
**Target framework:** net10.0  
**Solution:** Opure.slnx  
**Verification:** restore, build and test succeeded  

## Initial Projects

- `src/Runtime/Opure.Runtime.Contracts/Opure.Runtime.Contracts.csproj`
- `src/Runtime/Opure.Runtime/Opure.Runtime.csproj`
- `src/Bootstrap/Opure.Bootstrap.Windows/Opure.Bootstrap.Windows.csproj`
- `tests/Architecture/Opure.ArchitectureTests/Opure.ArchitectureTests.csproj`

## Boundary Established

- Runtime depends on Runtime.Contracts.
- Bootstrap does not reference Runtime implementation.
- Architecture tests are separate from production projects.
- Desktop is deliberately deferred to FND-005.
- Central package policy is deliberately deferred to FND-002.
- Version generation is deliberately deferred to FND-003.

## Evidence Files

- `dotnet-info.txt`
- `solution-projects.txt`

## Result

FND-001 solution baseline verification passed.
"@

    $evidencePath = Join-Path $evidenceDirectory 'solution-baseline.md'
    if (Test-Path -LiteralPath $evidencePath) {
        [System.IO.File]::WriteAllText(
            $evidencePath,
            $evidenceMarkdown.Replace("`r`n", "`n"),
            [System.Text.UTF8Encoding]::new($false)
        )
        Write-Host "Updated: $evidencePath" -ForegroundColor Green
    }
    else {
        Write-NewUtf8File -Path $evidencePath -Content $evidenceMarkdown
    }

    Write-Step "FND-001 completed"

    Write-Host ""
    Write-Host "Solution: $solution" -ForegroundColor Green
    Write-Host "SDK:      $selectedSdk" -ForegroundColor Green
    Write-Host ""
    Invoke-Checked -FilePath 'dotnet' -Arguments @('sln', $solution, 'list')

    Write-Host ""
    Write-Host "Git changes:" -ForegroundColor Cyan
    & git status --short

    Write-Host ""
    Write-Host "Review the changes, then commit with:" -ForegroundColor Yellow
    Write-Host '  git add .editorconfig .gitattributes .gitignore global.json Opure.slnx build.ps1 src tests tools build eng packaging enterprise samples docs schemas'
    Write-Host '  git commit -m "Create Opure solution baseline"'
    Write-Host '  git status'
}
finally {
    Pop-Location
}
