#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

$contractTests = Join-Path `
    $repositoryRoot `
    'tests\Trust\Opure.TrustEvidence.Contracts.Tests\Opure.TrustEvidence.Contracts.Tests.csproj'
$databaseTests = Join-Path `
    $repositoryRoot `
    'tests\Trust\Opure.TrustEvidence.Sqlite.Tests\Opure.TrustEvidence.Sqlite.Tests.csproj'
$architectureTests = Join-Path `
    $repositoryRoot `
    'tests\Architecture\Opure.ArchitectureTests\Opure.ArchitectureTests.csproj'
$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M3'
$schemaPath = Join-Path $evidenceRoot 'trust-query-schema.json'
$crossProjectPath = Join-Path $evidenceRoot 'trust-query-cross-project.json'
$planPath = Join-Path $evidenceRoot 'trust-query-plan-latency.json'
$verificationPath = Join-Path $evidenceRoot 'trust-query-verification.md'

Write-Host ''
Write-Host '==> Verify FND-025 Trust Evidence query contract' `
    -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration Release `
    -BuildChannel Development

& dotnet test $contractTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter-class `
    'Opure.TrustEvidence.Contracts.Tests.TrustEvidenceQueryContractTests' `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-025 Trust Evidence query contract tests failed.'
}

$env:OPURE_TRUST_QUERY_SCHEMA_PATH = $schemaPath
$env:OPURE_TRUST_QUERY_CROSS_PROJECT_PATH = $crossProjectPath
$env:OPURE_TRUST_QUERY_PLAN_PATH = $planPath

try {
    & dotnet test $databaseTests `
        --configuration Release `
        --no-build `
        --no-restore `
        --filter-class `
        'Opure.TrustEvidence.Sqlite.Tests.TrustEvidenceQueryServiceTests' `
        --timeout 60s

    if ($LASTEXITCODE -ne 0) {
        throw 'FND-025 Trust Evidence query service tests failed.'
    }
}
finally {
    Remove-Item Env:OPURE_TRUST_QUERY_SCHEMA_PATH `
        -ErrorAction SilentlyContinue
    Remove-Item Env:OPURE_TRUST_QUERY_CROSS_PROJECT_PATH `
        -ErrorAction SilentlyContinue
    Remove-Item Env:OPURE_TRUST_QUERY_PLAN_PATH `
        -ErrorAction SilentlyContinue
}

& dotnet test $architectureTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter-class 'Opure.ArchitectureTests.TrustEvidenceBoundaryTests' `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-025 Trust Evidence architecture tests failed.'
}

foreach ($evidencePath in @(
    $schemaPath,
    $crossProjectPath,
    $planPath,
    $verificationPath)) {
    if (-not (Test-Path -LiteralPath $evidencePath -PathType Leaf)) {
        throw "FND-025 evidence is missing: $evidencePath"
    }
}

$schema = [System.IO.File]::ReadAllText($schemaPath) | ConvertFrom-Json
$crossProject = [System.IO.File]::ReadAllText(
    $crossProjectPath) | ConvertFrom-Json
$plan = [System.IO.File]::ReadAllText($planPath) | ConvertFrom-Json

if ($schema.schema -ne 'opure.trust-query/1' -or
    $schema.result -ne 'Passed' -or
    $schema.contractRevision -ne 1 -or
    $schema.maximumPageSize -ne 100 -or
    $schema.maximumTimeRangeDays -ne 31 -or
    $schema.maximumCursorLength -ne 2048 -or
    $schema.scope.Count -ne 2 -or
    $schema.filters.Count -ne 5 -or
    $schema.rawSqlAccepted -ne $false -or
    $schema.arbitraryExpressionAccepted -ne $false -or
    $schema.payloadReturned -ne $false -or
    $schema.snapshotMetadata.Count -ne 6) {
    throw 'FND-025 query schema evidence is incomplete.'
}

foreach ($scope in @('ReleaseChannel', 'Project')) {
    if ($scope -notin $schema.scope) {
        throw "FND-025 query scope omits: $scope"
    }
}

foreach ($filter in @(
    'Operation',
    'EvidenceType',
    'Authority',
    'Outcome',
    'TimeRange')) {
    if ($filter -notin $schema.filters) {
        throw "FND-025 query filter evidence omits: $filter"
    }
}

if ($crossProject.schema -ne 'opure.trust-query-cross-project/1' -or
    $crossProject.result -ne 'Passed' -or
    $crossProject.allowedDisposition -ne 'Succeeded' -or
    $crossProject.allowedRecordCount -ne 1 -or
    $crossProject.deniedDisposition -ne 'Denied' -or
    $crossProject.deniedCode -ne 'TRUST_QUERY_PROJECT_DENIED' -or
    $crossProject.unauthorisedProjectRowsReturned -ne 0 -or
    $crossProject.authorisationBeforeDatabaseAccess -ne $true -or
    $crossProject.channelBound -ne $true) {
    throw 'FND-025 cross-project evidence is incomplete.'
}

if ($plan.schema -ne 'opure.trust-query-plan-latency/1' -or
    $plan.result -ne 'Passed' -or
    $plan.index -ne 'ix_evidence_records_project_channel_query' -or
    $plan.queryPlan -notmatch [regex]::Escape($plan.index) -or
    $plan.elapsedMilliseconds -gt $plan.budgetMilliseconds -or
    $plan.cursorPagination -ne 'Keyset' -or
    $plan.snapshotMaximumRowBound -ne $true -or
    $plan.projectionGenerationBound -ne $true -or
    $plan.concurrentRecordExcluded -ne $true -or
    $plan.payloadColumnsSelected -ne $false) {
    throw 'FND-025 query-plan or latency evidence is incomplete.'
}

foreach ($evidencePath in @(
    $schemaPath,
    $crossProjectPath,
    $planPath,
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
            throw "FND-025 evidence contains prohibited material: $prohibitedToken"
        }
    }

    if ($content -match '[A-Za-z]:[\\/]' -or
        $content -match '\\\\[^\\/\r\n]+[\\/]') {
        throw "FND-025 evidence contains an absolute or UNC path: $evidencePath"
    }
}

Write-Host ''
Write-Host 'FND-025 Trust Evidence query verification passed.' `
    -ForegroundColor Green
