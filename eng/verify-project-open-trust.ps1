#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

$projectTests = Join-Path $repositoryRoot `
    'tests\Project\Opure.Project.Sqlite.Tests\Opure.Project.Sqlite.Tests.csproj'
$persistenceTests = Join-Path $repositoryRoot `
    'tests\Persistence\Opure.Persistence.Sqlite.Tests\Opure.Persistence.Sqlite.Tests.csproj'
$contractTests = Join-Path $repositoryRoot `
    'tests\Trust\Opure.TrustEvidence.Contracts.Tests\Opure.TrustEvidence.Contracts.Tests.csproj'
$architectureTests = Join-Path $repositoryRoot `
    'tests\Architecture\Opure.ArchitectureTests\Opure.ArchitectureTests.csproj'
$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M4'
$typesPath = Join-Path $evidenceRoot 'project-open-evidence-types.json'
$samplePath = Join-Path $evidenceRoot 'project-open-trust-sample.json'
$transactionPath = Join-Path $evidenceRoot `
    'project-open-trust-transaction.json'
$recoveryPath = Join-Path $evidenceRoot 'project-open-trust-recovery.json'
$verificationPath = Join-Path $evidenceRoot `
    'project-open-trust-verification.md'

Write-Host ''
Write-Host '==> Verify FND-030 Project Open Trust receipt' `
    -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration Release `
    -BuildChannel Development

$env:OPURE_PROJECT_TRUST_TYPES_PATH = $typesPath
$env:OPURE_PROJECT_TRUST_SAMPLE_PATH = $samplePath
$env:OPURE_PROJECT_TRUST_TRANSACTION_PATH = $transactionPath
$env:OPURE_PROJECT_TRUST_RECOVERY_PATH = $recoveryPath

try {
    & dotnet test $projectTests `
        --configuration Release `
        --no-build `
        --no-restore `
        --filter-class `
        'Opure.Project.Sqlite.Tests.ProjectOpenTrustReceiptTests' `
        --timeout 60s

    if ($LASTEXITCODE -ne 0) {
        throw 'FND-030 Project Open Trust receipt tests failed.'
    }
}
finally {
    Remove-Item Env:OPURE_PROJECT_TRUST_TYPES_PATH `
        -ErrorAction SilentlyContinue
    Remove-Item Env:OPURE_PROJECT_TRUST_SAMPLE_PATH `
        -ErrorAction SilentlyContinue
    Remove-Item Env:OPURE_PROJECT_TRUST_TRANSACTION_PATH `
        -ErrorAction SilentlyContinue
    Remove-Item Env:OPURE_PROJECT_TRUST_RECOVERY_PATH `
        -ErrorAction SilentlyContinue
}

& dotnet test $persistenceTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter-method `
    'Opure.Persistence.Sqlite.Tests.SqliteOutboxTests.Type_filtered_dispatch_does_not_consume_or_block_other_events' `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-030 type-filtered outbox regression test failed.'
}

& dotnet test $contractTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter-method `
    'Opure.TrustEvidence.Contracts.Tests.EvidenceTypeContractTests.Project_open_types_bind_authority_and_minimised_payload' `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-030 Project Evidence Type contract test failed.'
}

& dotnet test $architectureTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-030 architecture tests failed.'
}

foreach ($path in @(
    $typesPath,
    $samplePath,
    $transactionPath,
    $recoveryPath,
    $verificationPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "FND-030 evidence is missing: $path"
    }
}

$types = [System.IO.File]::ReadAllText($typesPath) | ConvertFrom-Json
$sample = [System.IO.File]::ReadAllText($samplePath) | ConvertFrom-Json
$transaction = [System.IO.File]::ReadAllText($transactionPath) |
    ConvertFrom-Json
$recovery = [System.IO.File]::ReadAllText($recoveryPath) |
    ConvertFrom-Json

if ($types.schema -ne 'opure.project-open-evidence-types/1' -or
    $types.result -ne 'Passed' -or
    $types.ownerServiceId -ne 'opure.project' -or
    $types.authorityClass -ne 'AuthoritativeDomainStateTransition' -or
    $types.types.Count -ne 2 -or
    'project.registered' -notin $types.types.evidenceTypeId -or
    'project.opened' -notin $types.types.evidenceTypeId -or
    $types.rawPathFieldAllowed -ne $false -or
    $types.secretFieldAllowed -ne $false) {
    throw 'FND-030 Project Evidence Type evidence is incomplete.'
}

foreach ($type in $types.types) {
    if ($type.revision -ne 1 -or
        $type.definitionSha256 -notmatch '^[0-9a-f]{64}$' -or
        $type.payloadFields.Count -ne 5 -or
        'project_id' -notin $type.payloadFields -or
        'operation_id' -notin $type.payloadFields -or
        'root_class' -notin $type.payloadFields -or
        'repository_state' -notin $type.payloadFields -or
        'lifecycle_state' -notin $type.payloadFields) {
        throw "FND-030 Evidence Type is incomplete: $($type.evidenceTypeId)"
    }

    if ($type.payloadFields.Where({
            $_ -match 'path|content|secret|token'
        }).Count -ne 0) {
        throw "FND-030 Evidence Type contains a prohibited field: $($type.evidenceTypeId)"
    }
}

if ($sample.schema -ne 'opure.project-open-trust-sample/1' -or
    $sample.result -ne 'Passed' -or
    $sample.evidenceTypeId -ne 'project.opened' -or
    $sample.ownerServiceId -ne 'opure.project' -or
    $sample.authorityClass -ne 'AuthoritativeDomainStateTransition' -or
    $sample.scope -ne 'Project' -or
    $sample.action -ne 'project.open' -or
    $sample.outcome -ne 'succeeded' -or
    $sample.payload.root_class -ne 'fixed-local' -or
    $sample.payload.lifecycle_state -ne 'open' -or
    $sample.identifiersPseudonymisedForEvidence -ne $true -or
    $sample.payloadHashValidated -ne $true -or
    $sample.recordHashValidated -ne $true -or
    $sample.rawRootPathPersisted -ne $false) {
    throw 'FND-030 sample Project Open receipt evidence is incomplete.'
}

if ($transaction.schema -ne
        'opure.project-open-trust-transaction/1' -or
    $transaction.result -ne 'Passed' -or
    $transaction.projectStateAndReceiptCommitTogether -ne $true -or
    $transaction.receiptInsertFailureRolledBackProject -ne $true -or
    $transaction.successfulReceiptForFailedOpen -ne $false -or
    $transaction.ownerDatabase -ne 'projects.db' -or
    $transaction.ownerServiceId -ne 'opure.project' -or
    $transaction.crossServiceTransactionUsed -ne $false -or
    $transaction.delivery -ne 'transactional-outbox-at-least-once') {
    throw 'FND-030 owner transaction evidence is incomplete.'
}

if ($recovery.schema -ne 'opure.project-open-trust-recovery/1' -or
    $recovery.result -ne 'Passed' -or
    $recovery.ownerCommitSurvivedTrustUnavailable -ne $true -or
    $recovery.pendingReceiptPersisted -ne $true -or
    $recovery.projectRestartResumedDelivery -ne $true -or
    $recovery.trustRestartAcceptedDelivery -ne $true -or
    $recovery.duplicateDeliveryIdempotent -ne $true -or
    $recovery.boundedDispatchMaximum -ne 4096 -or
    $recovery.retryMaximumAttempts -ne 100 -or
    $recovery.finalUndeliveredCount -ne 0) {
    throw 'FND-030 recovery evidence is incomplete.'
}

foreach ($path in @(
    $typesPath,
    $samplePath,
    $transactionPath,
    $recoveryPath,
    $verificationPath)) {
    $content = [System.IO.File]::ReadAllText($path)

    foreach ($token in @(
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
                $token,
                [StringComparison]::OrdinalIgnoreCase)) {
            throw "FND-030 evidence contains prohibited material: $token"
        }
    }

    if ($content -match '[A-Za-z]:[\\/]' -or
        $content -match '\\\\[^\\/\r\n]+[\\/]') {
        throw "FND-030 evidence contains an absolute or UNC path: $path"
    }
}

Write-Host ''
Write-Host 'FND-030 Project Open Trust receipt verification passed.' `
    -ForegroundColor Green
