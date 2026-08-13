#requires -Version 7.2

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release'
)

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')
$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot
if (-not $IsWindows) { throw 'GATE-A-006 Trust reconciliation evidence requires Windows 11.' }

$matrixPath = Join-Path $repositoryRoot `
    'eng\evidence\milestones\M6\gate-a-006-trust-reconciliation-matrix.json'
$matrix = [IO.File]::ReadAllText($matrixPath) | ConvertFrom-Json
if ($matrix.ticket -ne 'GATE-A-006' -or
    $matrix.status -ne 'Ready' -or
    $matrix.result -ne 'Passed' -or
    $matrix.scenarioCount -ne 15 -or
    $matrix.scenarios.Count -ne 15) {
    throw 'GATE-A-006 Trust reconciliation matrix is incomplete.'
}

$proofContent = Get-ChildItem -LiteralPath (Join-Path $repositoryRoot 'tests\Trust') `
    -Recurse -Filter *.cs -File |
    ForEach-Object { [IO.File]::ReadAllText($_.FullName) }
$allProof = $proofContent -join "`n"
for ($index = 0; $index -lt $matrix.scenarios.Count; $index++) {
    $scenario = $matrix.scenarios[$index]
    if ($scenario.id -ne ($index + 1) -or
        -not $allProof.Contains([string]$scenario.proofMember, [StringComparison]::Ordinal)) {
        throw "GATE-A-006 scenario proof is missing: $($scenario.scenario)"
    }
}
foreach ($proofMember in $matrix.acceptanceProofMembers) {
    if (-not $allProof.Contains([string]$proofMember, [StringComparison]::Ordinal)) {
        throw "GATE-A-006 acceptance proof is missing: $proofMember"
    }
}
foreach ($relativePath in $matrix.evidence) {
    $path = Join-Path $repositoryRoot ([string]$relativePath).Replace('/', '\')
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "GATE-A-006 evidence is missing: $path"
    }
    $content = [IO.File]::ReadAllText($path)
    foreach ($prohibitedClaim in @('tamper-proof', 'tamper proof', 'guaranteed integrity')) {
        if ($content.Contains($prohibitedClaim, [StringComparison]::OrdinalIgnoreCase)) {
            throw "GATE-A-006 evidence contains an absolute integrity claim: $prohibitedClaim"
        }
    }
}

$reconciliationSource = [IO.File]::ReadAllText((Join-Path $repositoryRoot `
    'src\Trust\Opure.TrustEvidence.Sqlite\TrustEvidenceOwnerReconciliationService.cs'))
foreach ($required in @(
    'AuthorisedProjectIds.Contains',
    'AllowGlobalScope',
    'DeclaredPayloadSha256',
    'DeclaredRecordSha256',
    'ConflictQuarantined',
    'OwnerRecordDeleted')) {
    if (-not $reconciliationSource.Contains($required, [StringComparison]::Ordinal)) {
        throw "GATE-A-006 reconciliation boundary is missing: $required"
    }
}

Write-Host ''
Write-Host '==> Verify GATE-A-006 Release baseline' -ForegroundColor Cyan
& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration $Configuration `
    -BuildChannel Development

$suites = @(
    'tests\Trust\Opure.TrustEvidence.Contracts.Tests\Opure.TrustEvidence.Contracts.Tests.csproj',
    'tests\Trust\Opure.TrustEvidence.Sqlite.Tests\Opure.TrustEvidence.Sqlite.Tests.csproj')
foreach ($suite in $suites) {
    & dotnet test (Join-Path $repositoryRoot $suite) `
        --configuration $Configuration --no-build --no-restore --timeout 180s
    if ($LASTEXITCODE -ne 0) {
        throw "GATE-A-006 Trust Evidence tests failed: $suite"
    }
}

$receiptRoot = Join-Path $repositoryRoot 'artifacts\evidence\founder-gate-a'
New-Item -ItemType Directory -Force -Path $receiptRoot | Out-Null
$payload = [ordered]@{
    schema = 'opure.gate-a-006-trust-reconciliation/1'
    ticket = 'GATE-A-006'
    result = 'Passed'
    scenarioCount = 15
    exactOwnerRangeRepair = $true
    conflictQuarantine = $true
    projectionRebuild = $true
    fullReleaseVerificationPassed = $true
    matrixSha256 = (Get-FileHash -LiteralPath $matrixPath -Algorithm SHA256).Hash.ToLowerInvariant()
}
$payloadJson = $payload | ConvertTo-Json -Compress
$receipt = [ordered]@{
    algorithm = 'SHA-256'
    payloadSha256 = [Convert]::ToHexString(
        [Security.Cryptography.SHA256]::HashData(
            [Text.Encoding]::UTF8.GetBytes($payloadJson))).ToLowerInvariant()
    payload = $payload
}
$receiptPath = Join-Path $receiptRoot 'gate-a-006-trust-reconciliation-receipt.json'
[IO.File]::WriteAllText(
    $receiptPath,
    ($receipt | ConvertTo-Json -Depth 5),
    [Text.UTF8Encoding]::new($false))
$receiptContent = [IO.File]::ReadAllText($receiptPath)
foreach ($prohibited in @(
    'C:\Users\', 'ghp_', 'github_pat_', 'Authorization:',
    'Bearer ', 'Password=', 'sessionSecret')) {
    if ($receiptContent.Contains($prohibited, [StringComparison]::OrdinalIgnoreCase)) {
        throw "GATE-A-006 receipt contains prohibited material: $prohibited"
    }
}

Write-Host "GATE-A-006 Trust Evidence reconciliation suite passed: $receiptPath" `
    -ForegroundColor Green
