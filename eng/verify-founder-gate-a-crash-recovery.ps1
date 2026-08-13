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
if (-not $IsWindows) {
    throw 'GATE-A-002 crash and restart evidence requires Windows 11.'
}

$matrixPath = Join-Path $repositoryRoot `
    'eng\evidence\milestones\M6\gate-a-002-crash-recovery-matrix.json'
$matrixContent = [IO.File]::ReadAllText($matrixPath)
$matrix = $matrixContent | ConvertFrom-Json
if ($matrix.ticket -ne 'GATE-A-002' -or
    $matrix.status -ne 'Ready' -or
    $matrix.result -ne 'Passed' -or
    $matrix.scenarioCount -ne 12 -or
    $matrix.scenarios.Count -ne 12) {
    throw 'GATE-A-002 does not contain the complete 12-scenario recovery matrix.'
}

for ($index = 0; $index -lt $matrix.scenarios.Count; $index++) {
    $scenario = $matrix.scenarios[$index]
    if ($scenario.id -ne ($index + 1)) {
        throw 'GATE-A-002 scenario identifiers are not sequential.'
    }

    $proofPath = Join-Path $repositoryRoot `
        ([string]$scenario.proofFile).Replace('/', '\')
    if (-not (Test-Path -LiteralPath $proofPath -PathType Leaf)) {
        throw "GATE-A-002 proof is missing: $proofPath"
    }

    $proof = [IO.File]::ReadAllText($proofPath)
    if (-not $proof.Contains(
            [string]$scenario.proofMember,
            [StringComparison]::Ordinal)) {
        throw "GATE-A-002 proof member is missing: $($scenario.proofMember)"
    }
}

foreach ($relativeEvidencePath in $matrix.durabilityProofs) {
    $evidencePath = Join-Path $repositoryRoot `
        ([string]$relativeEvidencePath).Replace('/', '\')
    if (-not (Test-Path -LiteralPath $evidencePath -PathType Leaf)) {
        throw "GATE-A-002 durability evidence is missing: $evidencePath"
    }
}

foreach ($sourcePath in @(
    'src\Project\Opure.Project.Sqlite\ProjectOpenService.cs',
    'src\Workspace\Opure.Workspace.Service\WorkspaceReconciliationService.cs',
    'src\Configuration\Opure.Configuration\ConfigurationService.cs',
    'src\Trust\Opure.TrustEvidence.Service\TrustEvidenceServiceHost.cs')) {
    $source = [IO.File]::ReadAllText((Join-Path $repositoryRoot $sourcePath))
    if (-not $source.Contains('OPURE_BOOTSTRAP_TEST_MODE', [StringComparison]::Ordinal) -or
        -not $source.Contains('OPURE_TEST_CRASH_POINT', [StringComparison]::Ordinal)) {
        throw "GATE-A-002 crash injection is not test-mode bounded: $sourcePath"
    }
}

Write-Host ''
Write-Host '==> Verify GATE-A-002 Release baseline' -ForegroundColor Cyan
& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration $Configuration `
    -BuildChannel Development

$testSuites = @(
    @(
        'tests\Bootstrap\Opure.Bootstrap.Windows.Tests\Opure.Bootstrap.Windows.Tests.csproj',
        'Opure.Bootstrap.Windows.Tests.BootstrapCoordinatorTests',
        'Opure.Bootstrap.Windows.Tests.BootstrapSupervisorTests'),
    @(
        'tests\EndToEnd\Opure.EndToEnd.Tests\Opure.EndToEnd.Tests.csproj',
        'Opure.EndToEnd.Tests.RuntimeCrashRecoveryTests',
        'Opure.EndToEnd.Tests.ServiceCrashRecoveryTests'),
    @(
        'tests\Recovery\Opure.Recovery.Service.Tests\Opure.Recovery.Service.Tests.csproj',
        'Opure.Recovery.Service.Tests.LocalRecoveryPointServiceTests')
)

foreach ($suite in $testSuites) {
    $arguments = @(
        'test',
        (Join-Path $repositoryRoot $suite[0]),
        '--configuration', $Configuration,
        '--no-build',
        '--no-restore',
        '--timeout', '240s')
    foreach ($className in $suite[1..($suite.Count - 1)]) {
        $arguments += @('--filter-class', $className)
    }

    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "GATE-A-002 crash-recovery tests failed: $($suite[0])"
    }
}

$receiptRoot = Join-Path $repositoryRoot `
    'artifacts\evidence\founder-gate-a'
New-Item -ItemType Directory -Force -Path $receiptRoot | Out-Null
$matrixSha256 = (Get-FileHash -LiteralPath $matrixPath -Algorithm SHA256).Hash.ToLowerInvariant()
$proofHashes = [ordered]@{}
foreach ($relativeEvidencePath in $matrix.durabilityProofs) {
    $evidencePath = Join-Path $repositoryRoot `
        ([string]$relativeEvidencePath).Replace('/', '\')
    $proofHashes[[string]$relativeEvidencePath] =
        (Get-FileHash -LiteralPath $evidencePath -Algorithm SHA256).Hash.ToLowerInvariant()
}

$payload = [ordered]@{
    schema = 'opure.gate-a-002-crash-recovery/1'
    ticket = 'GATE-A-002'
    result = 'Passed'
    configuration = $Configuration
    scenarioCount = 12
    matrixSha256 = $matrixSha256
    durabilityProofSha256 = $proofHashes
    completeReleaseVerificationPassed = $true
    crashInjectionTestModeBounded = $true
    sessionMaterialPersisted = $false
}
$payloadJson = $payload | ConvertTo-Json -Depth 6 -Compress
$receipt = [ordered]@{
    algorithm = 'SHA-256'
    payloadSha256 = [Convert]::ToHexString(
        [Security.Cryptography.SHA256]::HashData(
            [Text.Encoding]::UTF8.GetBytes($payloadJson))).ToLowerInvariant()
    payload = $payload
}
$receiptPath = Join-Path $receiptRoot 'gate-a-002-crash-recovery-receipt.json'
[IO.File]::WriteAllText(
    $receiptPath,
    ($receipt | ConvertTo-Json -Depth 8),
    [Text.UTF8Encoding]::new($false))

$receiptContent = [IO.File]::ReadAllText($receiptPath)
foreach ($prohibited in @(
    'C:\Users\',
    'OPURE_BOOTSTRAP_SESSION_SECRET',
    'ghp_',
    'github_pat_',
    'Authorization:',
    'Bearer ',
    'Password=')) {
    if ($receiptContent.Contains($prohibited, [StringComparison]::OrdinalIgnoreCase)) {
        throw "GATE-A-002 receipt contains prohibited material: $prohibited"
    }
}

Write-Host "GATE-A-002 crash and restart recovery passed: $receiptPath" `
    -ForegroundColor Green
