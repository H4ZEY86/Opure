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
$transactionPath = Join-Path `
    $evidenceRoot `
    'sqlite-transactional-outbox-report.json'
$crashPath = Join-Path $evidenceRoot 'sqlite-outbox-crash-matrix.json'
$sequencePath = Join-Path `
    $evidenceRoot `
    'sqlite-outbox-owner-sequence-report.json'

New-Item -ItemType Directory -Force -Path $evidenceRoot | Out-Null

Write-Host ''
Write-Host '==> Verify FND-016 build and tests' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration Release `
    -BuildChannel Development

Write-Host ''
Write-Host '==> Exercise transactional outbox recovery' -ForegroundColor Cyan

$previousTransaction =
    $env:OPURE_SQLITE_OUTBOX_TRANSACTION_EVIDENCE_PATH
$previousCrash =
    $env:OPURE_SQLITE_OUTBOX_CRASH_EVIDENCE_PATH
$previousSequence =
    $env:OPURE_SQLITE_OUTBOX_SEQUENCE_EVIDENCE_PATH
$testExitCode = 1

try {
    $env:OPURE_SQLITE_OUTBOX_TRANSACTION_EVIDENCE_PATH =
        $transactionPath
    $env:OPURE_SQLITE_OUTBOX_CRASH_EVIDENCE_PATH =
        $crashPath
    $env:OPURE_SQLITE_OUTBOX_SEQUENCE_EVIDENCE_PATH =
        $sequencePath

    & dotnet test $persistenceTests `
        --configuration Release `
        --no-build `
        --no-restore `
        --filter-class `
        'Opure.Persistence.Sqlite.Tests.SqliteOutboxTests' `
        --timeout 60s
    $testExitCode = $LASTEXITCODE
}
finally {
    foreach ($item in @(
        @(
            'OPURE_SQLITE_OUTBOX_TRANSACTION_EVIDENCE_PATH',
            $previousTransaction),
        @(
            'OPURE_SQLITE_OUTBOX_CRASH_EVIDENCE_PATH',
            $previousCrash),
        @(
            'OPURE_SQLITE_OUTBOX_SEQUENCE_EVIDENCE_PATH',
            $previousSequence)
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
    throw 'FND-016 transactional outbox tests failed.'
}

& dotnet test $architectureTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter-class 'Opure.ArchitectureTests.PersistenceBoundaryTests' `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-016 persistence architecture tests failed.'
}

foreach ($evidencePath in @(
    $transactionPath,
    $crashPath,
    $sequencePath
)) {
    if (-not (Test-Path -LiteralPath $evidencePath -PathType Leaf)) {
        throw "FND-016 evidence was not produced: $evidencePath"
    }
}

$transaction = [System.IO.File]::ReadAllText($transactionPath) |
    ConvertFrom-Json

if ($transaction.schema -ne 'opure.sqlite-outbox-transaction/1' -or
    $transaction.result -ne 'Passed' -or
    $transaction.domainAndOutboxCommitAtomically -ne $true -or
    $transaction.domainAndOutboxRollbackAtomically -ne $true -or
    $transaction.rolledBackDomainRows -ne 0 -or
    $transaction.rolledBackOutboxRows -ne 0 -or
    $transaction.rolledBackSequenceRows -ne 0 -or
    $transaction.firstSequenceAfterRollback -ne 1 -or
    $transaction.payloadSha256 -notmatch '^[0-9a-f]{64}$' -or
    $transaction.idempotencySha256 -notmatch '^[0-9a-f]{64}$' -or
    $transaction.payloadImmutable -ne $true -or
    $transaction.deliveredIdentityRetained -ne $true) {
    throw 'FND-016 transaction evidence does not prove atomic immutable enqueue.'
}

$crash = [System.IO.File]::ReadAllText($crashPath) |
    ConvertFrom-Json

if ($crash.schema -ne 'opure.sqlite-outbox-crash-matrix/1' -or
    $crash.result -ne 'Passed' -or
    $crash.crashAfterCommitBeforeDeliveryRecovered -ne $true -or
    $crash.crashDuringDeliveryRecovered -ne $true -or
    $crash.beforeDeliveryPublishCount -ne 1 -or
    $crash.duringDeliveryPublishCount -ne 2 -or
    $crash.lostMessages -ne 0 -or
    $crash.duplicateDeliveryExpected -ne $true -or
    $crash.deliverySemantics -ne 'AtLeastOnce' -or
    $crash.expiredLeaseAttempt -ne 2) {
    throw 'FND-016 crash evidence does not prove at-least-once recovery.'
}

$sequence = [System.IO.File]::ReadAllText($sequencePath) |
    ConvertFrom-Json
$alphaSequence = @($sequence.alphaSequences) -join ','
$betaSequence = @($sequence.betaSequences) -join ','
$alphaDelivery = @($sequence.alphaDeliveryOrder) -join ','

if ($sequence.schema -ne 'opure.sqlite-outbox-owner-sequence/1' -or
    $sequence.result -ne 'Passed' -or
    $sequence.ownerServiceId -ne 'outbox.persistence' -or
    $alphaSequence -ne '1,2,3' -or
    $betaSequence -ne '1' -or
    $alphaDelivery -ne 'alpha-001,alpha-002,alpha-003' -or
    $sequence.perStreamOrdering -ne $true -or
    $sequence.idempotencyMetadataBound -ne $true -or
    $sequence.payloadHashesBound -ne $true) {
    throw 'FND-016 owner-sequence evidence is not monotonic and ordered.'
}

foreach ($prohibitedToken in @(
    'ghp_',
    'github_pat_',
    'sessionSecret',
    'accessToken',
    'C:\Users\',
    'Password=',
    'privateKey',
    'recoveryCode'
)) {
    foreach ($evidencePath in @(
        $transactionPath,
        $crashPath,
        $sequencePath
    )) {
        $evidence = [System.IO.File]::ReadAllText($evidencePath)

        if ($evidence.Contains(
                $prohibitedToken,
                [StringComparison]::OrdinalIgnoreCase)) {
            throw "FND-016 evidence contains prohibited material: $prohibitedToken"
        }
    }
}

Write-Host ''
Write-Host 'FND-016 transactional outbox verification passed.' `
    -ForegroundColor Green
