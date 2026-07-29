#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

$observabilityTests = Join-Path `
    $repositoryRoot `
    'tests\Observability\Opure.Observability.Tests\Opure.Observability.Tests.csproj'
$architectureTests = Join-Path `
    $repositoryRoot `
    'tests\Architecture\Opure.ArchitectureTests\Opure.ArchitectureTests.csproj'
$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M3'
$profilePath = Join-Path $evidenceRoot 'redaction-profile.txt'
$canaryPath = Join-Path $evidenceRoot 'redaction-canary-coverage.txt'
$scanPath = Join-Path $evidenceRoot 'redaction-persisted-scan.txt'
$verificationPath = Join-Path $evidenceRoot 'redaction-verification.md'

Write-Host ''
Write-Host '==> Verify FND-020 build and tests' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration Release `
    -BuildChannel Development

$env:OPURE_REDACTION_PROFILE_EVIDENCE_PATH = $profilePath
$env:OPURE_REDACTION_CANARY_EVIDENCE_PATH = $canaryPath
$env:OPURE_REDACTION_SCAN_EVIDENCE_PATH = $scanPath

try {
    & dotnet test $observabilityTests `
        --configuration Release `
        --no-build `
        --no-restore `
        --filter-class `
        'Opure.Observability.Tests.OperationalRedactionTests' `
        --timeout 60s

    if ($LASTEXITCODE -ne 0) {
        throw 'FND-020 redaction and canary tests failed.'
    }
}
finally {
    Remove-Item Env:OPURE_REDACTION_PROFILE_EVIDENCE_PATH `
        -ErrorAction SilentlyContinue
    Remove-Item Env:OPURE_REDACTION_CANARY_EVIDENCE_PATH `
        -ErrorAction SilentlyContinue
    Remove-Item Env:OPURE_REDACTION_SCAN_EVIDENCE_PATH `
        -ErrorAction SilentlyContinue
}

& dotnet test $architectureTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter-class 'Opure.ArchitectureTests.ObservabilityBoundaryTests' `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-020 observability architecture tests failed.'
}

foreach ($evidencePath in @(
    $profilePath,
    $canaryPath,
    $scanPath,
    $verificationPath)) {
    if (-not (Test-Path -LiteralPath $evidencePath -PathType Leaf)) {
        throw "FND-020 evidence is missing: $evidencePath"
    }
}

$profile = [System.IO.File]::ReadAllText($profilePath)
$canary = [System.IO.File]::ReadAllText($canaryPath)
$scan = [System.IO.File]::ReadAllText($scanPath)

foreach ($requiredLine in @(
    'schema=opure.redaction-profile/1',
    'result=Passed',
    'profileId=opure.local-diagnostics-redaction/1',
    'fieldAdmission=AllowlistFirst',
    'classifiedUnsafeFields=Rejected',
    'absolutePathOutcome=path.absolute',
    'percentEncodingInspection=Enabled',
    'base64EncodingInspection=Enabled',
    'maximumDecodedValueBytes=4096',
    'failureAction=DropUnsafeFieldsAndEmitWarning',
    'findingValuesIncluded=False',
    'authoritative=False')) {
    if (-not $profile.Contains($requiredLine, [StringComparison]::Ordinal)) {
        throw "FND-020 redaction profile evidence is incomplete: $requiredLine"
    }
}

foreach ($requiredLine in @(
    'schema=opure.redaction-canary-coverage/1',
    'result=Passed',
    'exactCredentialCanary=Passed',
    'patternCredentialCanary=Passed',
    'headerFieldCanary=Passed',
    'projectTextCanary=Passed',
    'windowsPathCanary=Passed',
    'uncPathCanary=Passed',
    'unixPathCanary=Passed',
    'exceptionMetadataCanary=Passed',
    'percentEncodedCanary=Passed',
    'base64EncodedCanary=Passed',
    'traceTagCanary=Passed',
    'processorFailureInjection=Passed',
    'findingCodesStable=Passed',
    'findingValuesIncluded=False',
    'authoritative=False')) {
    if (-not $canary.Contains($requiredLine, [StringComparison]::Ordinal)) {
        throw "FND-020 canary evidence is incomplete: $requiredLine"
    }
}

foreach ($requiredLine in @(
    'schema=opure.persisted-diagnostics-scan/1',
    'result=Passed',
    'operationalLogFilesScanned=1',
    'traceAdmissionScanned=Passed',
    'rawCanaryOccurrences=0',
    'encodedCanaryOccurrences=0',
    'absolutePathOccurrences=0',
    'safePathCategoryOccurrences=1',
    'findingValuesIncluded=False',
    'authoritative=False')) {
    if (-not $scan.Contains($requiredLine, [StringComparison]::Ordinal)) {
        throw "FND-020 persisted scan evidence is incomplete: $requiredLine"
    }
}

$scanTargets = @(
    $profilePath,
    $canaryPath,
    $scanPath,
    $verificationPath,
    (Join-Path $evidenceRoot 'trace-example.json'),
    (Join-Path $evidenceRoot 'trace-payload-leakage.txt'),
    (Join-Path $evidenceRoot 'trace-cross-process.txt')
)

foreach ($scanTarget in $scanTargets) {
    if (-not (Test-Path -LiteralPath $scanTarget -PathType Leaf)) {
        throw "FND-020 diagnostic scan target is missing: $scanTarget"
    }

    $content = [System.IO.File]::ReadAllText($scanTarget)

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
            throw "FND-020 evidence contains prohibited material: $prohibitedToken"
        }
    }

    if ($content -match '[A-Za-z]:[\\/]' -or
        $content -match '\\\\[^\\/\r\n]+[\\/]' -or
        $content -match '(?:^|[^A-Z0-9])AKIA[0-9A-Z]{16}(?:$|[^A-Z0-9])' -or
        $content -match '(?:^|[^A-Za-z0-9_-])eyJ[A-Za-z0-9_-]{2,}\.[A-Za-z0-9_-]{2,}\.[A-Za-z0-9_-]{2,}(?:$|[^A-Za-z0-9_-])') {
        throw "FND-020 evidence contains a prohibited path or credential pattern: $scanTarget"
    }
}

Write-Host ''
Write-Host 'FND-020 redaction and canary verification passed.' `
    -ForegroundColor Green
