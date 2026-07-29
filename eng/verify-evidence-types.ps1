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
$schemaPath = Join-Path $evidenceRoot 'evidence-type-schema.json'
$cataloguePath = Join-Path `
    $evidenceRoot `
    'foundation-evidence-type-catalogue.json'
$authorityPath = Join-Path `
    $evidenceRoot `
    'evidence-type-authority-review.txt'
$verificationPath = Join-Path `
    $evidenceRoot `
    'evidence-type-verification.md'

Write-Host ''
Write-Host '==> Verify FND-021 build and tests' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration Release `
    -BuildChannel Development

$env:OPURE_EVIDENCE_TYPE_SCHEMA_PATH = $schemaPath
$env:OPURE_EVIDENCE_TYPE_CATALOGUE_PATH = $cataloguePath
$env:OPURE_EVIDENCE_TYPE_AUTHORITY_PATH = $authorityPath

try {
    & dotnet test $contractTests `
        --configuration Release `
        --no-build `
        --no-restore `
        --timeout 60s

    if ($LASTEXITCODE -ne 0) {
        throw 'FND-021 Evidence Type contract tests failed.'
    }
}
finally {
    Remove-Item Env:OPURE_EVIDENCE_TYPE_SCHEMA_PATH `
        -ErrorAction SilentlyContinue
    Remove-Item Env:OPURE_EVIDENCE_TYPE_CATALOGUE_PATH `
        -ErrorAction SilentlyContinue
    Remove-Item Env:OPURE_EVIDENCE_TYPE_AUTHORITY_PATH `
        -ErrorAction SilentlyContinue
}

& dotnet test $architectureTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter-class 'Opure.ArchitectureTests.TrustEvidenceBoundaryTests' `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-021 Trust Evidence architecture tests failed.'
}

foreach ($evidencePath in @(
    $schemaPath,
    $cataloguePath,
    $authorityPath,
    $verificationPath)) {
    if (-not (Test-Path -LiteralPath $evidencePath -PathType Leaf)) {
        throw "FND-021 evidence is missing: $evidencePath"
    }
}

$schema = [System.IO.File]::ReadAllText($schemaPath) | ConvertFrom-Json
$catalogue = [System.IO.File]::ReadAllText($cataloguePath) | ConvertFrom-Json
$authority = [System.IO.File]::ReadAllText($authorityPath)

if ($schema.schema -ne 'opure.trust-evidence-type/1' -or
    $schema.result -ne 'Passed' -or
    $schema.requiredProperties.Count -ne 12 -or
    $schema.unknownTypeTrusted -ne $false -or
    $schema.revisionImmutable -ne $true -or
    $schema.ownerAndAuthorityStableAcrossRevisions -ne $true -or
    $schema.secretPayloadFieldsAllowed -ne $false -or
    $schema.authoritative -ne $false) {
    throw 'FND-021 Evidence Type schema does not match the implemented contract.'
}

$requiredProperties = @(
    'evidenceTypeId',
    'revision',
    'ownerServiceId',
    'authorityClass',
    'payloadLocation',
    'payloadFields',
    'safeIndexFields',
    'relationshipEligibility',
    'retention',
    'supportExportEligibility',
    'redactionProfileId',
    'canonicalSha256'
)

foreach ($requiredProperty in $requiredProperties) {
    if ($requiredProperty -notin $schema.requiredProperties) {
        throw "FND-021 schema omits required property: $requiredProperty"
    }
}

$expectedTypeIds = @(
    'backup.recovery-point-created',
    'configuration.snapshot-committed',
    'project.closed',
    'project.opened',
    'runtime.started',
    'runtime.stopped',
    'security.policy-denied',
    'service.state-changed',
    'workspace.snapshot-created'
)

if ($catalogue.schema -ne 'opure.foundation-evidence-type-catalogue/1' -or
    $catalogue.result -ne 'Passed' -or
    $catalogue.typeCount -ne 9 -or
    $catalogue.types.Count -ne 9 -or
    $catalogue.authoritative -ne $false -or
    [string]::Join(',', $catalogue.types.evidenceTypeId) -ne
        [string]::Join(',', $expectedTypeIds)) {
    throw 'FND-021 foundation catalogue does not match the reviewed fixture.'
}

foreach ($type in $catalogue.types) {
    if ($type.revision -ne 1 -or
        [string]::IsNullOrWhiteSpace($type.ownerServiceId) -or
        [string]::IsNullOrWhiteSpace($type.authorityClass) -or
        $type.authorityClass -eq 'UnknownOrUnverified' -or
        $type.payloadFields.Count -lt 1 -or
        [string]::IsNullOrWhiteSpace($type.retention.retentionClass) -or
        $type.retention.defaultRetentionDays -lt 1 -or
        [string]::IsNullOrWhiteSpace($type.supportExportEligibility) -or
        $type.redactionProfileId -ne 'opure.trust-evidence-redaction.1' -or
        $type.canonicalSha256 -notmatch '^[0-9a-f]{64}$') {
        throw "FND-021 catalogue type is incomplete: $($type.evidenceTypeId)"
    }

    foreach ($field in $type.payloadFields) {
        if ($field.classification -in @('Secret', 'Prohibited')) {
            throw "FND-021 catalogue contains an unsafe payload field: $($type.evidenceTypeId)"
        }
    }

    foreach ($index in $type.safeIndexFields) {
        $field = $type.payloadFields |
            Where-Object { $_.name -eq $index } |
            Select-Object -First 1

        if (-not $field -or
            $field.classification -notin @('Safe', 'Pseudonymous')) {
            throw "FND-021 catalogue contains an unsafe index: $($type.evidenceTypeId).$index"
        }
    }
}

foreach ($requiredLine in @(
    'schema=opure.evidence-type-authority-review/1',
    'result=Passed',
    'reviewedTypeCount=9',
    'missingOwnerCount=0',
    'unknownAuthorityCount=0',
    'authorityChangeWithoutNewTypeIdAllowed=False',
    'unknownTypeTrusted=False',
    'historicalRevisionReadable=Passed',
    'findingValuesIncluded=False',
    'authoritative=False')) {
    if (-not $authority.Contains(
            $requiredLine,
            [StringComparison]::Ordinal)) {
        throw "FND-021 authority review is incomplete: $requiredLine"
    }
}

foreach ($evidencePath in @(
    $schemaPath,
    $cataloguePath,
    $authorityPath,
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
            throw "FND-021 evidence contains prohibited material: $prohibitedToken"
        }
    }

    if ($content -match '[A-Za-z]:[\\/]' -or
        $content -match '\\\\[^\\/\r\n]+[\\/]') {
        throw "FND-021 evidence contains an absolute or UNC path: $evidencePath"
    }
}

Write-Host ''
Write-Host 'FND-021 Evidence Type verification passed.' `
    -ForegroundColor Green
