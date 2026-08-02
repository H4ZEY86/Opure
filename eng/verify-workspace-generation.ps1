#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot
$workspaceTests = Join-Path $repositoryRoot 'tests\Workspace\Opure.Workspace.Sqlite.Tests\Opure.Workspace.Sqlite.Tests.csproj'
$architectureTests = Join-Path $repositoryRoot 'tests\Architecture\Opure.ArchitectureTests\Opure.ArchitectureTests.csproj'
$schemaPath = Join-Path $repositoryRoot 'src\Workspace\Opure.Workspace.Sqlite\WorkspaceDatabaseSchema.cs'
$storePath = Join-Path $repositoryRoot 'src\Workspace\Opure.Workspace.Sqlite\WorkspaceGenerationStore.cs'
$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M4'
$evidencePaths = @(
    (Join-Path $evidenceRoot 'workspace-database-schema.json'),
    (Join-Path $evidenceRoot 'workspace-atomic-generation-report.json'),
    (Join-Path $evidenceRoot 'workspace-canonical-hash-vectors.json'))

Write-Host ''
Write-Host '==> Verify FND-036 immutable Workspace generation' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') -Configuration Release -BuildChannel Development

& dotnet test $workspaceTests --configuration Release --no-build --no-restore --timeout 60s
if ($LASTEXITCODE -ne 0) { throw 'FND-036 Workspace generation acceptance tests failed.' }

& dotnet test $architectureTests --configuration Release --no-build --no-restore `
    --filter-class 'Opure.ArchitectureTests.WorkspaceServiceBoundaryTests' --timeout 60s
if ($LASTEXITCODE -ne 0) { throw 'FND-036 Workspace architecture tests failed.' }

$schema = [System.IO.File]::ReadAllText($schemaPath)
$store = [System.IO.File]::ReadAllText($storePath)
foreach ($required in @('workspace_generations', 'workspace_generation_entries', 'workspace_repository_summaries', 'workspace_current_generations', 'workspace_generation_staging', 'workspace_entry_staging', 'immutable')) {
    if (-not $schema.Contains($required, [StringComparison]::OrdinalIgnoreCase)) {
        throw "FND-036 schema is missing required ownership state: $required"
    }
}
foreach ($required in @('ExecuteTransaction', 'ComputeCanonicalHash', 'PromoteStaging', 'ActivateCurrent', 'GetCurrent', 'GetByGeneration')) {
    if (-not $store.Contains($required, [StringComparison]::Ordinal)) {
        throw "FND-036 generation store is missing required behaviour: $required"
    }
}

foreach ($path in $evidencePaths) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "FND-036 evidence is missing: $path" }
    $content = [System.IO.File]::ReadAllText($path)
    foreach ($token in @('C:\Users\', 'ghp_', 'github_pat_', 'Authorization:', 'Bearer ', 'Password=')) {
        if ($content.Contains($token, [StringComparison]::OrdinalIgnoreCase)) {
            throw "FND-036 evidence contains prohibited material: $token"
        }
    }
    if ($content -match '[A-Za-z]:[\\/]' -or $content -match '\\\\[^\\/\r\n]+[\\/]') {
        throw "FND-036 evidence contains an absolute or UNC path: $path"
    }
}

$database = [System.IO.File]::ReadAllText($evidencePaths[0]) | ConvertFrom-Json
$atomic = [System.IO.File]::ReadAllText($evidencePaths[1]) | ConvertFrom-Json
$canonical = [System.IO.File]::ReadAllText($evidencePaths[2]) | ConvertFrom-Json
if ($database.result -ne 'Passed' -or $database.ownerServiceId -ne 'opure.workspace' -or `
    $database.schemaVersion -ne 1 -or $database.committedRowsImmutable -ne $true -or `
    $atomic.result -ne 'Passed' -or $atomic.failedBeforePromotionPreservesCurrent -ne $true -or `
    $atomic.failedBeforePointerPreservesCurrent -ne $true -or $atomic.priorGenerationQueryable -ne $true -or `
    $atomic.concurrentRequestsSerialised -ne $true -or $atomic.incompleteStagingDiscarded -ne $true -or `
    $atomic.partialInventoryRejected -ne $true -or `
    $atomic.unstableHashInherited -ne $false -or $canonical.result -ne 'Passed' -or `
    $canonical.algorithm -ne 'SHA-256' -or $canonical.canonicalRevision -ne 1 -or `
    $canonical.fixtureGenerationSha256 -ne '1bda5987ab3295b47490c16dba1e3b0bb71f18de427f3658ca6ae487fc61aee5') {
    throw 'FND-036 evidence is incomplete.'
}

Write-Host ''
Write-Host 'FND-036 immutable Workspace generation verification passed.' -ForegroundColor Green
