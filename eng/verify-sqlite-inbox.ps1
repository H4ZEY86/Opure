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
$idempotencyPath = Join-Path `
    $evidenceRoot `
    'sqlite-inbox-idempotency-report.json'
$conflictPath = Join-Path `
    $evidenceRoot `
    'sqlite-inbox-conflicting-duplicate-report.json'

New-Item -ItemType Directory -Force -Path $evidenceRoot | Out-Null

Write-Host ''
Write-Host '==> Verify FND-017 build and tests' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration Release `
    -BuildChannel Development

Write-Host ''
Write-Host '==> Exercise transactional inbox recovery' -ForegroundColor Cyan

$previousIdempotency =
    $env:OPURE_SQLITE_INBOX_IDEMPOTENCY_EVIDENCE_PATH
$previousConflict =
    $env:OPURE_SQLITE_INBOX_CONFLICT_EVIDENCE_PATH
$testExitCode = 1

try {
    $env:OPURE_SQLITE_INBOX_IDEMPOTENCY_EVIDENCE_PATH =
        $idempotencyPath
    $env:OPURE_SQLITE_INBOX_CONFLICT_EVIDENCE_PATH =
        $conflictPath

    & dotnet test $persistenceTests `
        --configuration Release `
        --no-build `
        --no-restore `
        --filter-class `
        'Opure.Persistence.Sqlite.Tests.SqliteInboxTests' `
        --timeout 60s
    $testExitCode = $LASTEXITCODE
}
finally {
    foreach ($item in @(
        @(
            'OPURE_SQLITE_INBOX_IDEMPOTENCY_EVIDENCE_PATH',
            $previousIdempotency),
        @(
            'OPURE_SQLITE_INBOX_CONFLICT_EVIDENCE_PATH',
            $previousConflict)
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
    throw 'FND-017 transactional inbox tests failed.'
}

& dotnet test $architectureTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter-class 'Opure.ArchitectureTests.PersistenceBoundaryTests' `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-017 persistence architecture tests failed.'
}

foreach ($evidencePath in @($idempotencyPath, $conflictPath)) {
    if (-not (Test-Path -LiteralPath $evidencePath -PathType Leaf)) {
        throw "FND-017 evidence was not produced: $evidencePath"
    }
}

$idempotency = [System.IO.File]::ReadAllText($idempotencyPath) |
    ConvertFrom-Json

if ($idempotency.schema -ne 'opure.sqlite-inbox-idempotency/1' -or
    $idempotency.result -ne 'Passed' -or
    $idempotency.receiverServiceId -ne 'inbox.persistence' -or
    $idempotency.sourceServiceId -ne 'source.service' -or
    $idempotency.firstDeliveryState -ne 'Applied' -or
    $idempotency.matchingDuplicateState -ne 'Duplicate' -or
    $idempotency.domainEffectCount -ne 1 -or
    $idempotency.receiptCount -ne 1 -or
    $idempotency.payloadSha256 -notmatch '^[0-9a-f]{64}$' -or
    $idempotency.duplicatePayloadSha256 -ne
        $idempotency.payloadSha256 -or
    $idempotency.receiptAndDomainEffectAtomic -ne $true -or
    $idempotency.matchingDuplicateAcknowledged -ne $true -or
    $idempotency.duplicateDomainEffectApplied -ne $false -or
    $idempotency.crashBeforeCommitReplaySafe -ne $true -or
    $idempotency.crashAfterCommitReplaySafe -ne $true -or
    $idempotency.receiptSurvivesRestart -ne $true) {
    throw 'FND-017 idempotency evidence does not prove atomic replay safety.'
}

$conflict = [System.IO.File]::ReadAllText($conflictPath) |
    ConvertFrom-Json

if ($conflict.schema -ne
        'opure.sqlite-inbox-conflicting-duplicate/1' -or
    $conflict.result -ne 'Passed' -or
    $conflict.conflictState -ne 'ConflictingDuplicate' -or
    $conflict.stableConflictReason -ne 'INBOX_PAYLOAD_CONFLICT' -or
    $conflict.acceptedPayloadSha256 -notmatch '^[0-9a-f]{64}$' -or
    $conflict.conflictingPayloadSha256 -notmatch '^[0-9a-f]{64}$' -or
    $conflict.conflictingPayloadSha256 -eq
        $conflict.acceptedPayloadSha256 -or
    $conflict.domainEffectCount -ne 1 -or
    $conflict.receiptCount -ne 1 -or
    $conflict.distinctConflictedMessages -ne 1 -or
    $conflict.conflictVariants -ne 1 -or
    $conflict.conflictObservations -ne 2 -or
    $conflict.healthState -ne 'ConflictDetected' -or
    $conflict.conflictingPayloadPersisted -ne $false -or
    $conflict.conflictIdentityRetained -ne $true -or
    $conflict.materialIntegrityEvidenceRecorded -ne $true -or
    $conflict.typedTrustEvidenceEmissionDeferred -ne $true) {
    throw 'FND-017 conflict evidence does not prove safe quarantine.'
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
    foreach ($evidencePath in @($idempotencyPath, $conflictPath)) {
        $evidence = [System.IO.File]::ReadAllText($evidencePath)

        if ($evidence.Contains(
                $prohibitedToken,
                [StringComparison]::OrdinalIgnoreCase)) {
            throw "FND-017 evidence contains prohibited material: $prohibitedToken"
        }
    }
}

Write-Host ''
Write-Host 'FND-017 transactional inbox verification passed.' `
    -ForegroundColor Green
