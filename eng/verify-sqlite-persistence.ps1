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
$dependencyManifestPath = Join-Path `
    $evidenceRoot `
    'sqlite-dependency-manifest.json'
$transactionReportPath = Join-Path `
    $evidenceRoot `
    'sqlite-transaction-report.json'
$designPath = Join-Path `
    $evidenceRoot `
    'persistence-library-design.md'

New-Item -ItemType Directory -Force -Path $evidenceRoot | Out-Null

Write-Host ''
Write-Host '==> Verify FND-014 build and tests' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration Release `
    -BuildChannel Development

Write-Host ''
Write-Host '==> Exercise service-owned SQLite boundary' -ForegroundColor Cyan

$previousDependencyEvidence =
    $env:OPURE_SQLITE_DEPENDENCY_EVIDENCE_PATH
$previousTransactionEvidence =
    $env:OPURE_SQLITE_TRANSACTION_EVIDENCE_PATH
$testExitCode = 1

try {
    $env:OPURE_SQLITE_DEPENDENCY_EVIDENCE_PATH =
        $dependencyManifestPath
    $env:OPURE_SQLITE_TRANSACTION_EVIDENCE_PATH =
        $transactionReportPath

    & dotnet test $persistenceTests `
        --configuration Release `
        --no-build `
        --no-restore `
        --filter-class `
        'Opure.Persistence.Sqlite.Tests.SqliteServiceDatabaseTests' `
        --timeout 60s
    $testExitCode = $LASTEXITCODE
}
finally {
    if ($null -eq $previousDependencyEvidence) {
        Remove-Item Env:OPURE_SQLITE_DEPENDENCY_EVIDENCE_PATH `
            -ErrorAction SilentlyContinue
    }
    else {
        $env:OPURE_SQLITE_DEPENDENCY_EVIDENCE_PATH =
            $previousDependencyEvidence
    }

    if ($null -eq $previousTransactionEvidence) {
        Remove-Item Env:OPURE_SQLITE_TRANSACTION_EVIDENCE_PATH `
            -ErrorAction SilentlyContinue
    }
    else {
        $env:OPURE_SQLITE_TRANSACTION_EVIDENCE_PATH =
            $previousTransactionEvidence
    }
}

if ($testExitCode -ne 0) {
    throw 'FND-014 SQLite persistence tests failed.'
}

& dotnet test $architectureTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter-class 'Opure.ArchitectureTests.PersistenceBoundaryTests' `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-014 persistence architecture tests failed.'
}

foreach ($evidencePath in @(
    $dependencyManifestPath,
    $transactionReportPath
)) {
    if (-not (Test-Path -LiteralPath $evidencePath -PathType Leaf)) {
        throw "FND-014 evidence was not produced: $evidencePath"
    }
}

$dependencyText = [System.IO.File]::ReadAllText($dependencyManifestPath)
$dependency = $dependencyText | ConvertFrom-Json

if ($dependency.schema -ne 'opure.sqlite-dependency-manifest/1' -or
    $dependency.result -ne 'Passed' -or
    $dependency.managedProviderPackageVersion -ne '10.0.10' -or
    $dependency.nativeBundlePackageVersion -ne '2.1.12' -or
    $dependency.nativeSqliteVersion -ne '3.53.3' -or
    $dependency.arbitraryExtensionLoadingEnabled -ne $false) {
    throw 'FND-014 SQLite dependency manifest is not the reviewed graph.'
}

$transactionText = [System.IO.File]::ReadAllText($transactionReportPath)
$transaction = $transactionText | ConvertFrom-Json

if ($transaction.schema -ne 'opure.sqlite-transaction-report/1' -or
    $transaction.result -ne 'Passed' -or
    $transaction.concurrentWriteRequests -ne 8 -or
    $transaction.maximumConcurrentWriters -ne 1 -or
    $transaction.rolledBackTransactionRows -ne 0 -or
    $transaction.transactionMode -ne 'Immediate' -or
    $transaction.busyTimeoutMilliseconds -ne 2000) {
    throw 'FND-014 transaction report is not the reviewed scenario.'
}

$design = @'
# FND-014 SQLite Persistence Library Design

## Ownership boundary

`ServiceDatabaseAuthority` represents exactly one owning service under one absolute channel data root. It derives descriptors at `services/<owner>/databases/<name>.db`; callers cannot supply a database path or append connection-string options. `SqliteServiceDatabaseConnectionFactory` rejects descriptors from another authority before creating directories or opening a file.

The library is infrastructure only. It owns no domain database and the Runtime, Bootstrap and Desktop projects do not reference it. A future service-specific persistence assembly will hold that service's schema and SQL. Desktop continues to receive projections through the Desktop Gateway.

## Reviewed connection profile

- `Microsoft.Data.Sqlite` 10.0.10.
- `SQLitePCLRaw.bundle_e_sqlite3` 2.1.12 with loaded native SQLite 3.53.3.
- Absolute canonical data source, read/write/create mode, private cache and pooling disabled.
- Foreign keys enabled and verified on every connection.
- `trusted_schema` disabled and verified.
- WAL requested and verified; no silent journal fallback.
- `synchronous=FULL`, 2,000 ms busy timeout and memory mapping disabled.
- Positive SQLite `application_id` established for an empty owned file and verified on reopen.
- `quick_check` must return `ok` before the database is reported open.

## Transactions and writer policy

Every state-changing operation enters one bounded in-process writer gate and an immediate SQLite transaction. A process-wide canonical-path lease prevents a second factory from opening another owning writer for the same database. Domain exceptions unwind the transaction and SQLite errors become stable persistence error codes.

## Health and recovery

An opened database reports bounded Open health with the verified profile and loaded SQLite version. Disposal reports Closed health and releases the writer lease. A malformed file, wrong application identifier, unsafe reparse path or configuration failure is not deleted or replaced. The owner receives a stable error and can enter Failed or Recovery Required.

## Deliberately deferred

FND-014 does not create a Runtime database. Ordered migrations, migration history and recovery points belong to FND-015; transactional outbox publication belongs to FND-016; backup, restore, integrity scheduling, ACL hardening and service health publication remain later persistence work. ADR-0005 remains Proposed until its broader acceptance suite passes.
'@
[System.IO.File]::WriteAllText(
    $designPath,
    $design.Replace("`r`n", "`n"),
    [System.Text.UTF8Encoding]::new($false))

foreach ($prohibitedToken in @(
    'ghp_',
    'github_pat_',
    'sessionSecret',
    'accessToken',
    'C:\Users\',
    'Password='
)) {
    foreach ($evidencePath in @(
        $dependencyManifestPath,
        $transactionReportPath,
        $designPath
    )) {
        $evidence = [System.IO.File]::ReadAllText($evidencePath)

        if ($evidence.Contains(
                $prohibitedToken,
                [StringComparison]::OrdinalIgnoreCase)) {
            throw "FND-014 evidence contains prohibited material: $prohibitedToken"
        }
    }
}

Write-Host ''
Write-Host 'FND-014 SQLite persistence verification passed.' -ForegroundColor Green
