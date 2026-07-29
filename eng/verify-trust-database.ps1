#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

$databaseTests = Join-Path `
    $repositoryRoot `
    'tests\Trust\Opure.TrustEvidence.Sqlite.Tests\Opure.TrustEvidence.Sqlite.Tests.csproj'
$architectureTests = Join-Path `
    $repositoryRoot `
    'tests\Architecture\Opure.ArchitectureTests\Opure.ArchitectureTests.csproj'
$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M3'
$schemaPath = Join-Path $evidenceRoot 'trust-database-schema.json'
$migrationPath = Join-Path $evidenceRoot 'trust-database-migration-report.json'
$queryPlanPath = Join-Path $evidenceRoot 'trust-database-query-plan.json'
$verificationPath = Join-Path $evidenceRoot 'trust-database-verification.md'

Write-Host ''
Write-Host '==> Verify FND-023 build and tests' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration Release `
    -BuildChannel Development

$env:OPURE_TRUST_DATABASE_SCHEMA_PATH = $schemaPath
$env:OPURE_TRUST_DATABASE_MIGRATION_PATH = $migrationPath
$env:OPURE_TRUST_DATABASE_QUERY_PLAN_PATH = $queryPlanPath

try {
    & dotnet test $databaseTests `
        --configuration Release `
        --no-build `
        --no-restore `
        --filter-class `
        'Opure.TrustEvidence.Sqlite.Tests.TrustEvidenceDatabaseTests' `
        --timeout 60s

    if ($LASTEXITCODE -ne 0) {
        throw 'FND-023 Trust Evidence database tests failed.'
    }
}
finally {
    Remove-Item Env:OPURE_TRUST_DATABASE_SCHEMA_PATH `
        -ErrorAction SilentlyContinue
    Remove-Item Env:OPURE_TRUST_DATABASE_MIGRATION_PATH `
        -ErrorAction SilentlyContinue
    Remove-Item Env:OPURE_TRUST_DATABASE_QUERY_PLAN_PATH `
        -ErrorAction SilentlyContinue
}

& dotnet test $architectureTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter-class 'Opure.ArchitectureTests.TrustEvidenceBoundaryTests' `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-023 Trust Evidence architecture tests failed.'
}

foreach ($evidencePath in @(
    $schemaPath,
    $migrationPath,
    $queryPlanPath,
    $verificationPath)) {
    if (-not (Test-Path -LiteralPath $evidencePath -PathType Leaf)) {
        throw "FND-023 evidence is missing: $evidencePath"
    }
}

$schema = [System.IO.File]::ReadAllText($schemaPath) | ConvertFrom-Json
$migration = [System.IO.File]::ReadAllText($migrationPath) | ConvertFrom-Json
$queryPlan = [System.IO.File]::ReadAllText($queryPlanPath) | ConvertFrom-Json

if ($schema.schema -ne 'opure.trust-database-schema/1' -or
    $schema.result -ne 'Passed' -or
    $schema.databaseName -ne 'trust' -or
    $schema.ownerServiceId -ne 'opure.trust-evidence' -or
    $schema.schemaVersion -ne 5 -or
    $schema.journalMode -ne 'WAL' -or
    $schema.foreignKeysEnabled -ne $true -or
    $schema.oneWriter -ne $true -or
    $schema.separateFromOperationalLogs -ne $true -or
    $schema.fullTextTables.Count -ne 0 -or
    $schema.payloadCopiedToProjection -ne $false -or
    $schema.authoritativeForOwnerDomain -ne $false) {
    throw 'FND-023 schema evidence does not match the implemented store.'
}

foreach ($table in @(
    'evidence_type_definitions',
    'evidence_type_revisions',
    'evidence_records',
    'evidence_payload_references',
    'evidence_relationships',
    'evidence_owner_sequences',
    '__opure_inbox_receipts',
    '__opure_inbox_conflicts',
    'trust_projection_checkpoints',
    'trust_projection_records',
    'evidence_retention_decisions',
    'trust_projection_state')) {
    if ($table -notin $schema.tables) {
        throw "FND-023 schema evidence omits table: $table"
    }
}

foreach ($index in @(
    'ix_evidence_records_owner_sequence',
    'ix_trust_projection_project_query',
    'ix_trust_projection_operation_query',
    'ix_evidence_records_project_channel_query')) {
    if ($index -notin $schema.indexes) {
        throw "FND-023 schema evidence omits reviewed index: $index"
    }
}

if ($migration.schema -ne 'opure.trust-database-migration-report/1' -or
    $migration.result -ne 'Passed' -or
    $migration.StartingVersion -ne 0 -or
    $migration.CurrentVersion -ne 5 -or
    $migration.migrations.Count -ne 5 -or
    $migration.validations.Count -lt 15 -or
    @($migration.validations | Where-Object { -not $_.Passed }).Count -ne 0 -or
    $migration.recoveryMeaning -notmatch 'not proof') {
    throw 'FND-023 migration evidence is incomplete or failed.'
}

if ($queryPlan.schema -ne 'opure.trust-database-query-plan/1' -or
    $queryPlan.result -ne 'Passed' -or
    $queryPlan.boundedPageSize -ne 50 -or
    $queryPlan.ownerSequence.plan -notmatch
        [regex]::Escape($queryPlan.ownerSequence.index) -or
    $queryPlan.project.plan -notmatch
        [regex]::Escape($queryPlan.project.index) -or
    $queryPlan.operation.plan -notmatch
        [regex]::Escape($queryPlan.operation.index) -or
    $queryPlan.payloadIndexed -ne $false) {
    throw 'FND-023 query-plan evidence does not use every reviewed index.'
}

foreach ($evidencePath in @(
    $schemaPath,
    $migrationPath,
    $queryPlanPath,
    $verificationPath)) {
    $content = [System.IO.File]::ReadAllText($evidencePath)

    foreach ($prohibitedToken in @(
        'C:\Users\',
        'ghp_',
        'github_pat_',
        'Authorization:',
        'Basic ',
        'Cookie:',
        'Set-Cookie:',
        'Bearer ',
        'Password=',
        'privateKey',
        'sessionSecret',
        'clientSecret',
        'connectionString',
        'requestBody',
        'responseBody',
        'sourceContent')) {
        if ($content.Contains(
                $prohibitedToken,
                [StringComparison]::OrdinalIgnoreCase)) {
            throw "FND-023 evidence contains prohibited material: $prohibitedToken"
        }
    }

    if ($content -match '[A-Za-z]:[\\/]' -or
        $content -match '\\\\[^\\/\r\n]+[\\/]') {
        throw "FND-023 evidence contains an absolute or UNC path: $evidencePath"
    }
}

Write-Host ''
Write-Host 'FND-023 Trust Evidence database verification passed.' `
    -ForegroundColor Green
