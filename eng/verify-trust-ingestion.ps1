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
$contractPath = Join-Path $evidenceRoot 'trust-ingestion-contract.json'
$authenticationPath = Join-Path `
    $evidenceRoot `
    'trust-ingestion-owner-authentication.json'
$conflictPath = Join-Path `
    $evidenceRoot `
    'trust-ingestion-duplicate-conflict.json'
$verificationPath = Join-Path `
    $evidenceRoot `
    'trust-ingestion-verification.md'

Write-Host ''
Write-Host '==> Verify FND-024 Trust Evidence ingestion' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration Release `
    -BuildChannel Development

$env:OPURE_TRUST_INGESTION_CONTRACT_PATH = $contractPath
$env:OPURE_TRUST_INGESTION_AUTHENTICATION_PATH = $authenticationPath
$env:OPURE_TRUST_INGESTION_CONFLICT_PATH = $conflictPath

try {
    & dotnet test $databaseTests `
        --configuration Release `
        --no-build `
        --no-restore `
        --filter-class `
        'Opure.TrustEvidence.Sqlite.Tests.TrustEvidenceIngestionPipelineTests' `
        --timeout 60s

    if ($LASTEXITCODE -ne 0) {
        throw 'FND-024 Trust Evidence ingestion tests failed.'
    }
}
finally {
    Remove-Item Env:OPURE_TRUST_INGESTION_CONTRACT_PATH `
        -ErrorAction SilentlyContinue
    Remove-Item Env:OPURE_TRUST_INGESTION_AUTHENTICATION_PATH `
        -ErrorAction SilentlyContinue
    Remove-Item Env:OPURE_TRUST_INGESTION_CONFLICT_PATH `
        -ErrorAction SilentlyContinue
}

& dotnet test $architectureTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter-class 'Opure.ArchitectureTests.TrustEvidenceBoundaryTests' `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-024 Trust Evidence architecture tests failed.'
}

foreach ($evidencePath in @(
    $contractPath,
    $authenticationPath,
    $conflictPath,
    $verificationPath)) {
    if (-not (Test-Path -LiteralPath $evidencePath -PathType Leaf)) {
        throw "FND-024 evidence is missing: $evidencePath"
    }
}

$contract = [System.IO.File]::ReadAllText($contractPath) | ConvertFrom-Json
$authentication = [System.IO.File]::ReadAllText(
    $authenticationPath) | ConvertFrom-Json
$conflict = [System.IO.File]::ReadAllText($conflictPath) | ConvertFrom-Json

if ($contract.schema -ne 'opure.trust-evidence-ingestion/1' -or
    $contract.result -ne 'Passed' -or
    $contract.contractRevision -ne 1 -or
    $contract.maximumRelationships -ne 64 -or
    $contract.ownerIdentitySource -ne 'AuthenticatedLocalTransport' -or
    $contract.validates.Count -ne 7 -or
    $contract.transactionMembers.Count -ne 9 -or
    $contract.stableReceipt -ne $true -or
    $contract.ownerDomainAuthorityPreserved -ne $true) {
    throw 'FND-024 ingestion contract evidence is incomplete.'
}

foreach ($binding in @(
    'OwnerIdentity',
    'EvidenceTypeRevision',
    'PayloadSha256',
    'RecordSha256',
    'OwnerSequence',
    'PreviousStreamSha256',
    'RelationshipEligibility')) {
    if ($binding -notin $contract.validates) {
        throw "FND-024 contract omits validation: $binding"
    }
}

foreach ($transactionMember in @(
    'InboxReceipt',
    'EvidenceRecord',
    'PayloadReference',
    'OwnerSequence',
    'Relationships',
    'VerifiedServiceReceiptProjection',
    'RetentionDecision',
    'IngestionReceipt',
    'OwnerGap')) {
    if ($transactionMember -notin $contract.transactionMembers) {
        throw "FND-024 transaction omits member: $transactionMember"
    }
}

if ($authentication.schema -ne
        'opure.trust-ingestion-owner-authentication/1' -or
    $authentication.result -ne 'Passed' -or
    $authentication.applied -ne 'Applied' -or
    $authentication.denied -ne 'Denied' -or
    $authentication.deniedCode -ne
        'TRUST_INGESTION_OWNER_MISMATCH' -or
    $authentication.recordOwnerTrustedDirectly -ne $false -or
    $authentication.authenticationMaterialPersisted -ne $false -or
    $authentication.sessionIdPersisted -ne $false -or
    $authentication.wrongOwnerWrites -ne 0) {
    throw 'FND-024 owner-authentication evidence is incomplete.'
}

if ($conflict.schema -ne
        'opure.trust-ingestion-duplicate-conflict/1' -or
    $conflict.result -ne 'Passed' -or
    $conflict.acceptedDisposition -ne 'Applied' -or
    $conflict.conflictDisposition -ne 'Quarantined' -or
    $conflict.conflictCode -ne
        'TRUST_INGESTION_CONFLICTING_DUPLICATE' -or
    $conflict.retainedConflictVariants -ne 1 -or
    $conflict.secondDomainEffectApplied -ne $false -or
    $conflict.conflictingPayloadPersisted -ne $false -or
    $conflict.acceptedRecordReplaced -ne $false) {
    throw 'FND-024 duplicate-conflict evidence is incomplete.'
}

foreach ($evidencePath in @(
    $contractPath,
    $authenticationPath,
    $conflictPath,
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
            throw "FND-024 evidence contains prohibited material: $prohibitedToken"
        }
    }

    if ($content -match '[A-Za-z]:[\\/]' -or
        $content -match '\\\\[^\\/\r\n]+[\\/]') {
        throw "FND-024 evidence contains an absolute or UNC path: $evidencePath"
    }
}

Write-Host ''
Write-Host 'FND-024 Trust Evidence ingestion verification passed.' `
    -ForegroundColor Green
