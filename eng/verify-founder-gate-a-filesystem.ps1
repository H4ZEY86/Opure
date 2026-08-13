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
if (-not $IsWindows) { throw 'GATE-A-004 filesystem evidence requires Windows 11.' }

$matrixPath = Join-Path $repositoryRoot `
    'eng\evidence\milestones\M6\gate-a-004-filesystem-adversarial-matrix.json'
$matrix = [IO.File]::ReadAllText($matrixPath) | ConvertFrom-Json
if ($matrix.ticket -ne 'GATE-A-004' -or
    $matrix.status -ne 'Ready' -or
    $matrix.result -ne 'Passed' -or
    $matrix.scenarioCount -ne 20 -or
    $matrix.scenarios.Count -ne 20) {
    throw 'GATE-A-004 filesystem matrix is incomplete.'
}

$proofRoots = @(
    'tests\Filesystem',
    'tests\Workspace',
    'tests\EndToEnd\Opure.EndToEnd.Tests\AdversarialFileSystemSuiteTests.cs')
$proofContent = foreach ($proofRoot in $proofRoots) {
    $path = Join-Path $repositoryRoot $proofRoot
    if (Test-Path -LiteralPath $path -PathType Container) {
        Get-ChildItem -LiteralPath $path -Recurse -Filter *.cs -File |
            ForEach-Object { [IO.File]::ReadAllText($_.FullName) }
    }
    else {
        [IO.File]::ReadAllText($path)
    }
}
$allProof = $proofContent -join "`n"
for ($index = 0; $index -lt $matrix.scenarios.Count; $index++) {
    $scenario = $matrix.scenarios[$index]
    if ($scenario.id -ne ($index + 1) -or
        -not $allProof.Contains([string]$scenario.proofMember, [StringComparison]::Ordinal)) {
        throw "GATE-A-004 scenario proof is missing: $($scenario.scenario)"
    }
}

foreach ($relativePath in $matrix.evidence) {
    $path = Join-Path $repositoryRoot ([string]$relativePath).Replace('/', '\')
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "GATE-A-004 evidence is missing: $path"
    }
}

$inventorySource = [IO.File]::ReadAllText((Join-Path $repositoryRoot `
    'src\Workspace\Opure.Workspace.Windows\WindowsWorkspaceInventoryGenerator.cs'))
foreach ($required in @(
    'NormalizationForm.FormC',
    'StringComparer.OrdinalIgnoreCase',
    'LOGICAL_PATH_COLLISION',
    'HashText(entry.LogicalPath)')) {
    if (-not $inventorySource.Contains($required, [StringComparison]::Ordinal)) {
        throw "GATE-A-004 collision boundary is missing: $required"
    }
}

Write-Host ''
Write-Host '==> Verify GATE-A-004 Release baseline' -ForegroundColor Cyan
& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration $Configuration `
    -BuildChannel Development

$suites = @(
    @('tests\Filesystem\Opure.Filesystem.Windows.Tests\Opure.Filesystem.Windows.Tests.csproj'),
    @('tests\Workspace\Opure.Workspace.Windows.Tests\Opure.Workspace.Windows.Tests.csproj'),
    @(
        'tests\Workspace\Opure.Workspace.Service.Tests\Opure.Workspace.Service.Tests.csproj',
        'Opure.Workspace.Service.Tests.WorkspaceReconciliationServiceTests'),
    @(
        'tests\EndToEnd\Opure.EndToEnd.Tests\Opure.EndToEnd.Tests.csproj',
        'Opure.EndToEnd.Tests.AdversarialFileSystemSuiteTests'))
foreach ($suite in $suites) {
    $arguments = @(
        'test', (Join-Path $repositoryRoot $suite[0]),
        '--configuration', $Configuration, '--no-build', '--no-restore',
        '--timeout', '180s')
    if ($suite.Count -gt 1) { $arguments += @('--filter-class', $suite[1]) }
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "GATE-A-004 filesystem tests failed: $($suite[0])"
    }
}

$receiptRoot = Join-Path $repositoryRoot 'artifacts\evidence\founder-gate-a'
New-Item -ItemType Directory -Force -Path $receiptRoot | Out-Null
$payload = [ordered]@{
    schema = 'opure.gate-a-004-filesystem/1'
    ticket = 'GATE-A-004'
    result = 'Passed'
    scenarioCount = 20
    unicodeCollisionDetection = $true
    projectFilesModified = $false
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
$receiptPath = Join-Path $receiptRoot 'gate-a-004-filesystem-receipt.json'
[IO.File]::WriteAllText(
    $receiptPath,
    ($receipt | ConvertTo-Json -Depth 5),
    [Text.UTF8Encoding]::new($false))
$receiptContent = [IO.File]::ReadAllText($receiptPath)
foreach ($prohibited in @(
    'C:\Users\', 'ghp_', 'github_pat_', 'Authorization:',
    'Bearer ', 'Password=', 'sessionSecret')) {
    if ($receiptContent.Contains($prohibited, [StringComparison]::OrdinalIgnoreCase)) {
        throw "GATE-A-004 receipt contains prohibited material: $prohibited"
    }
}

Write-Host "GATE-A-004 filesystem adversarial suite passed: $receiptPath" `
    -ForegroundColor Green
