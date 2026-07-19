#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M3'
$persistenceTests = Join-Path `
    $repositoryRoot `
    'tests\Persistence\Opure.Persistence.Sqlite.Tests\Opure.Persistence.Sqlite.Tests.csproj'
$architectureTests = Join-Path `
    $repositoryRoot `
    'tests\Architecture\Opure.ArchitectureTests\Opure.ArchitectureTests.csproj'
$cataloguePath = Join-Path $evidenceRoot 'sqlite-migration-catalogue.json'
$failurePath = Join-Path $evidenceRoot 'sqlite-migration-failure-report.json'
$validationPath = Join-Path $evidenceRoot 'sqlite-schema-validation-report.json'

New-Item -ItemType Directory -Force -Path $evidenceRoot | Out-Null

Write-Host ''
Write-Host '==> Verify FND-015 build and tests' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration Release `
    -BuildChannel Development

Write-Host ''
Write-Host '==> Exercise ordered SQLite migrations' -ForegroundColor Cyan

$previousCatalogue =
    $env:OPURE_SQLITE_MIGRATION_CATALOGUE_EVIDENCE_PATH
$previousFailure =
    $env:OPURE_SQLITE_MIGRATION_FAILURE_EVIDENCE_PATH
$previousValidation =
    $env:OPURE_SQLITE_SCHEMA_VALIDATION_EVIDENCE_PATH
$testExitCode = 1

try {
    $env:OPURE_SQLITE_MIGRATION_CATALOGUE_EVIDENCE_PATH =
        $cataloguePath
    $env:OPURE_SQLITE_MIGRATION_FAILURE_EVIDENCE_PATH =
        $failurePath
    $env:OPURE_SQLITE_SCHEMA_VALIDATION_EVIDENCE_PATH =
        $validationPath

    & dotnet test $persistenceTests `
        --configuration Release `
        --no-build `
        --no-restore `
        --filter-class `
        'Opure.Persistence.Sqlite.Tests.SqliteMigrationRunnerTests' `
        --timeout 60s
    $testExitCode = $LASTEXITCODE
}
finally {
    foreach ($item in @(
        @(
            'OPURE_SQLITE_MIGRATION_CATALOGUE_EVIDENCE_PATH',
            $previousCatalogue),
        @(
            'OPURE_SQLITE_MIGRATION_FAILURE_EVIDENCE_PATH',
            $previousFailure),
        @(
            'OPURE_SQLITE_SCHEMA_VALIDATION_EVIDENCE_PATH',
            $previousValidation)
    )) {
        if ($null -eq $item[1]) {
            Remove-Item "Env:$($item[0])" -ErrorAction SilentlyContinue
        }
        else {
            Set-Item "Env:$($item[0])" $item[1]
        }
    }
}

if ($testExitCode -ne 0) {
    throw 'FND-015 SQLite migration tests failed.'
}

& dotnet test $architectureTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter-class 'Opure.ArchitectureTests.PersistenceBoundaryTests' `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-015 persistence architecture tests failed.'
}

foreach ($evidencePath in @(
    $cataloguePath,
    $failurePath,
    $validationPath
)) {
    if (-not (Test-Path -LiteralPath $evidencePath -PathType Leaf)) {
        throw "FND-015 evidence was not produced: $evidencePath"
    }
}

$catalogue = [System.IO.File]::ReadAllText($cataloguePath) |
    ConvertFrom-Json

if ($catalogue.schema -ne 'opure.sqlite-migration-catalogue/1' -or
    $catalogue.result -ne 'Passed' -or
    $catalogue.forwardOnly -ne $true -or
    $catalogue.targetSchemaVersion -ne 2 -or
    $catalogue.historyTable -ne '__opure_migration_history' -or
    $catalogue.migrations.Count -ne 2 -or
    $catalogue.migrations[0].id -ne 'create-projects' -or
    $catalogue.migrations[1].id -ne 'add-project-status' -or
    $catalogue.migrations[0].checksum -notmatch '^[0-9a-f]{64}$' -or
    $catalogue.migrations[1].checksum -notmatch '^[0-9a-f]{64}$') {
    throw 'FND-015 migration catalogue is not the reviewed forward sequence.'
}

$failure = [System.IO.File]::ReadAllText($failurePath) |
    ConvertFrom-Json

if ($failure.schema -ne 'opure.sqlite-migration-failure/1' -or
    $failure.result -ne 'Passed' -or
    $failure.errorCode -ne 'PERSISTENCE_MIGRATION_FAILED' -or
    $failure.startingSchemaVersion -ne 1 -or
    $failure.schemaVersionAfterFailure -ne 1 -or
    $failure.transientTableCount -ne 0 -or
    $failure.retainedRows -ne 1 -or
    $failure.migrationHistoryRows -ne 1 -or
    $failure.transactionRolledBack -ne $true -or
    $failure.normalWritesBlockedUntilRecovery -ne $true -or
    $failure.automaticDestructiveReset -ne $false) {
    throw 'FND-015 migration failure evidence does not prove safe rollback.'
}

$validation = [System.IO.File]::ReadAllText($validationPath) |
    ConvertFrom-Json

if ($validation.schema -ne 'opure.sqlite-schema-validation/1' -or
    $validation.result -ne 'Passed' -or
    $validation.currentVersion -ne 2 -or
    $validation.quickCheck -ne 'ok' -or
    $validation.foreignKeyCheckRows -ne 0 -or
    $validation.targetedChecks.Count -ne 3 -or
    @($validation.targetedChecks | Where-Object passed -ne $true).Count -ne 0 -or
    $validation.migrationState -ne 'Current' -or
    $validation.stagedRestoreCopySupported -ne $true) {
    throw 'FND-015 schema validation evidence is incomplete.'
}

foreach ($prohibitedToken in @(
    'ghp_',
    'github_pat_',
    'sessionSecret',
    'accessToken',
    'C:\Users\',
    'Password='
)) {
    foreach ($evidencePath in @(
        $cataloguePath,
        $failurePath,
        $validationPath
    )) {
        $evidence = [System.IO.File]::ReadAllText($evidencePath)

        if ($evidence.Contains(
                $prohibitedToken,
                [StringComparison]::OrdinalIgnoreCase)) {
            throw "FND-015 evidence contains prohibited material: $prohibitedToken"
        }
    }
}

Write-Host ''
Write-Host 'FND-015 SQLite migration verification passed.' -ForegroundColor Green
