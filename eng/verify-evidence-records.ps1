#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

$contractTests = Join-Path `
    $repositoryRoot `
    'tests\Trust\Opure.TrustEvidence.Contracts.Tests\Opure.TrustEvidence.Contracts.Tests.csproj'
$architectureTests = Join-Path `
    $repositoryRoot `
    'tests\Architecture\Opure.ArchitectureTests\Opure.ArchitectureTests.csproj'
$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M3'
$schemaPath = Join-Path $evidenceRoot 'evidence-record-schema.json'
$vectorPath = Join-Path `
    $evidenceRoot `
    'evidence-record-canonicalisation.json'
$examplesPath = Join-Path $evidenceRoot 'evidence-record-examples.json'
$verificationPath = Join-Path `
    $evidenceRoot `
    'evidence-record-verification.md'

Write-Host ''
Write-Host '==> Verify FND-022 build and tests' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration Release `
    -BuildChannel Development

$env:OPURE_EVIDENCE_RECORD_SCHEMA_PATH = $schemaPath
$env:OPURE_EVIDENCE_RECORD_VECTOR_PATH = $vectorPath
$env:OPURE_EVIDENCE_RECORD_EXAMPLES_PATH = $examplesPath

try {
    & dotnet test $contractTests `
        --configuration Release `
        --no-build `
        --no-restore `
        --filter-class `
        'Opure.TrustEvidence.Contracts.Tests.EvidenceRecordContractTests' `
        --timeout 60s

    if ($LASTEXITCODE -ne 0) {
        throw 'FND-022 Evidence Record contract tests failed.'
    }
}
finally {
    Remove-Item Env:OPURE_EVIDENCE_RECORD_SCHEMA_PATH `
        -ErrorAction SilentlyContinue
    Remove-Item Env:OPURE_EVIDENCE_RECORD_VECTOR_PATH `
        -ErrorAction SilentlyContinue
    Remove-Item Env:OPURE_EVIDENCE_RECORD_EXAMPLES_PATH `
        -ErrorAction SilentlyContinue
}

& dotnet test $architectureTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter-class 'Opure.ArchitectureTests.TrustEvidenceBoundaryTests' `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-022 Trust Evidence architecture tests failed.'
}

foreach ($evidencePath in @(
    $schemaPath,
    $vectorPath,
    $examplesPath,
    $verificationPath)) {
    if (-not (Test-Path -LiteralPath $evidencePath -PathType Leaf)) {
        throw "FND-022 evidence is missing: $evidencePath"
    }
}

$schema = [System.IO.File]::ReadAllText($schemaPath) | ConvertFrom-Json
$vector = [System.IO.File]::ReadAllText($vectorPath) | ConvertFrom-Json
$examples = [System.IO.File]::ReadAllText($examplesPath) | ConvertFrom-Json

if ($schema.schema -ne 'opure.trust-evidence-record/1' -or
    $schema.result -ne 'Passed' -or
    $schema.requiredProperties.Count -ne 21 -or
    $schema.conditionalProperties.Count -ne 8 -or
    $schema.maximumInlinePayloadBytes -ne 65536 -or
    $schema.maximumReferencedPayloadBytes -ne 268435456 -or
    $schema.projectScopeRequiresProjectId -ne $true -or
    $schema.secretAndProhibitedPayloadFieldsAllowed -ne $false -or
    $schema.payloadClassificationCoversFields -ne $true -or
    $schema.occurredAndObservedTimeDistinct -ne $true -or
    $schema.canonicalRecordHashAlgorithm -ne 'SHA-256' -or
    $schema.authoritative -ne $false) {
    throw 'FND-022 Evidence Record schema does not match the implemented contract.'
}

foreach ($requiredProperty in @(
    'evidenceId',
    'evidenceTypeId',
    'evidenceTypeRevision',
    'evidenceTypeDefinitionSha256',
    'ownerServiceId',
    'ownerRecordId',
    'ownerRecordRevision',
    'authorityClass',
    'releaseChannel',
    'scope',
    'subjectKind',
    'subjectId',
    'action',
    'outcome',
    'occurredAtUtc',
    'observedAtUtc',
    'ownerSequence',
    'retentionClass',
    'preservationState',
    'payload',
    'recordSha256')) {
    if ($requiredProperty -notin $schema.requiredProperties) {
        throw "FND-022 schema omits required property: $requiredProperty"
    }
}

foreach ($conditionalProperty in @(
    'projectId',
    'operationId',
    'workflowInstanceId',
    'traceId',
    'spanId',
    'runtimeBootId',
    'previousStreamSha256',
    'payloadReference')) {
    if ($conditionalProperty -notin $schema.conditionalProperties) {
        throw "FND-022 schema omits conditional property: $conditionalProperty"
    }
}

if ($vector.schema -ne 'opure.evidence-record-canonicalisation/1' -or
    $vector.result -ne 'Passed' -or
    $vector.vectorId -ne 'project-operation-inline/1' -or
    $vector.canonicalPayloadSha256 -ne
        '76b96b1e34925aa47eb462f19421a68628510ed34705b42fa481ebc5f2dad5f7' -or
    $vector.recordSha256 -ne
        '606beff62aa17ce3526d881320c180f5d1a44bada1deede20bd38ce1523bd408' -or
    $vector.reorderedPayloadRecordSha256 -ne $vector.recordSha256 -or
    $vector.semanticChangeRecordSha256 -eq $vector.recordSha256 -or
    $vector.propertyOrderInvariant -ne $true -or
    $vector.semanticChangeDetected -ne $true -or
    $vector.authoritative -ne $false) {
    throw 'FND-022 canonicalisation vectors do not match the reviewed fixture.'
}

if ($examples.schema -ne 'opure.evidence-record-examples/1' -or
    $examples.result -ne 'Passed' -or
    $examples.records.Count -lt 1 -or
    $examples.secretValuesIncluded -ne $false -or
    $examples.projectNamesIncluded -ne $false -or
    $examples.pathsIncluded -ne $false -or
    $examples.authoritative -ne $false) {
    throw 'FND-022 record examples are incomplete or unsafe.'
}

foreach ($record in $examples.records) {
    if ($record.schema -ne 'opure.trust-evidence-record/1' -or
        $record.evidenceId -notmatch '^[0-9a-f]{32}$' -or
        [string]::IsNullOrWhiteSpace($record.evidenceTypeId) -or
        $record.evidenceTypeRevision -lt 1 -or
        $record.evidenceTypeDefinitionSha256 -notmatch '^[0-9a-f]{64}$' -or
        [string]::IsNullOrWhiteSpace($record.ownerServiceId) -or
        [string]::IsNullOrWhiteSpace($record.ownerRecordId) -or
        $record.ownerRecordRevision -lt 1 -or
        [string]::IsNullOrWhiteSpace($record.authorityClass) -or
        [string]::IsNullOrWhiteSpace($record.subjectId) -or
        [string]::IsNullOrWhiteSpace($record.action) -or
        [string]::IsNullOrWhiteSpace($record.outcome) -or
        [DateTimeOffset]$record.occurredAtUtc -eq
            [DateTimeOffset]$record.observedAtUtc -or
        $record.ownerSequence -lt 1 -or
        $record.payload.payloadSizeBytes -lt 1 -or
        $record.payload.payloadSha256 -notmatch '^[0-9a-f]{64}$' -or
        $record.recordSha256 -notmatch '^[0-9a-f]{64}$') {
        throw 'FND-022 record example does not match the implemented envelope.'
    }

    if ($record.scope -eq 'Project' -and
        [string]::IsNullOrWhiteSpace($record.projectId)) {
        throw 'FND-022 project-scoped example omits project identity.'
    }
}

foreach ($evidencePath in @(
    $schemaPath,
    $vectorPath,
    $examplesPath,
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
            throw "FND-022 evidence contains prohibited material: $prohibitedToken"
        }
    }

    if ($content -match '[A-Za-z]:[\\/]' -or
        $content -match '\\\\[^\\/\r\n]+[\\/]') {
        throw "FND-022 evidence contains an absolute or UNC path: $evidencePath"
    }
}

Write-Host ''
Write-Host 'FND-022 Evidence Record verification passed.' `
    -ForegroundColor Green
