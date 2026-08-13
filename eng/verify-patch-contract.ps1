#requires -Version 7.2

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
$contractRoot = Join-Path $repositoryRoot 'src\Patch\Opure.Patch.Contracts'
$testProject = Join-Path $repositoryRoot 'tests\Patch\Opure.Patch.Contracts.Tests\Opure.Patch.Contracts.Tests.csproj'
$evidencePath = Join-Path $repositoryRoot 'eng\evidence\milestones\M7\cm-001-exact-utf8-patch-contract.md'

foreach ($path in @(
    (Join-Path $contractRoot 'Opure.Patch.Contracts.csproj'),
    (Join-Path $contractRoot 'ExactUtf8PatchContract.cs'),
    $testProject,
    $evidencePath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "CM-001 required artefact is missing: $path"
    }
}

$source = Get-Content -LiteralPath (Join-Path $contractRoot 'ExactUtf8PatchContract.cs') -Raw
foreach ($required in @(
    'opure.patch.exact-utf8/1',
    'ExactUtf8PatchOperationKind',
    'BaseWorkspaceGenerationSha256',
    'TargetPathReferenceId',
    'ExpectedSourceSha256',
    'ExpectedSourceSizeBytes',
    'PatchLineEndingIntent',
    'PatchCreatorKind',
    'ResultingContentSha256',
    'throwOnInvalidBytes: true')) {
    if (-not $source.Contains($required, [StringComparison]::Ordinal)) {
        throw "CM-001 contract binding is missing: $required"
    }
}
foreach ($forbidden in @(
    'System.IO', 'System.Net', 'System.Diagnostics', 'Microsoft.Data.Sqlite',
    'Opure.Desktop', 'ApplyAsync', 'WriteAll', 'AbsolutePath', 'DisplayPath')) {
    if ($source.Contains($forbidden, [StringComparison]::Ordinal)) {
        throw "CM-001 contract contains forbidden authority: $forbidden"
    }
}

& dotnet test $testProject --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    throw "CM-001 contract tests failed with exit code $LASTEXITCODE."
}
& dotnet test (Join-Path $repositoryRoot 'tests\Architecture\Opure.ArchitectureTests\Opure.ArchitectureTests.csproj') `
    --configuration Release --no-restore -- --filter-class Opure.ArchitectureTests.PatchServiceBoundaryTests
if ($LASTEXITCODE -ne 0) {
    throw "CM-001 architecture test failed with exit code $LASTEXITCODE."
}

Write-Host 'CM-001 exact UTF-8 Patch contract verification passed.' -ForegroundColor Green
