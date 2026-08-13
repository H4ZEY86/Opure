#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot
$fixtureRoot = Join-Path $repositoryRoot 'eng\fixtures\founder-gate-a'
$evidencePath = Join-Path $repositoryRoot 'eng\evidence\milestones\M6\founder-gate-a-001-readiness.json'
$backlogPath = Join-Path $repositoryRoot 'specs\BACKLOG-001-foundation-first-12-weeks.md'
$recoveryTestPath = Join-Path $repositoryRoot 'tests\EndToEnd\Opure.EndToEnd.Tests\RecoveryPointCliPipelineTests.cs'

Write-Host ''
Write-Host '==> Verify GATE-A-001 demonstration readiness' -ForegroundColor Cyan

foreach ($requiredPath in @(
    $fixtureRoot,
    $evidencePath,
    $backlogPath,
    $recoveryTestPath,
    (Join-Path $repositoryRoot 'eng\run-bootstrap.ps1'),
    (Join-Path $repositoryRoot 'src\Desktop\Opure.Desktop\RecoveryPointView.axaml'))) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "GATE-A-001 readiness input is missing: $requiredPath"
    }
}

$fixtureFiles = Get-ChildItem -LiteralPath $fixtureRoot -Recurse -Force -File |
    Sort-Object { [IO.Path]::GetRelativePath($fixtureRoot, $_.FullName) }
$canonicalLines = foreach ($file in $fixtureFiles) {
    $relativePath = [IO.Path]::GetRelativePath($fixtureRoot, $file.FullName).Replace('\', '/')
    $fileHash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    "$relativePath|$($file.Length)|$fileHash"
}
$canonicalBytes = [Text.Encoding]::UTF8.GetBytes(($canonicalLines -join "`n"))
$fixtureHash = [Convert]::ToHexString(
    [Security.Cryptography.SHA256]::HashData($canonicalBytes)).ToLowerInvariant()

$evidenceContent = [IO.File]::ReadAllText($evidencePath)
foreach ($prohibited in @('C:\Users\', 'ghp_', 'github_pat_', 'Authorization:', 'Bearer ', 'Password=')) {
    if ($evidenceContent.Contains($prohibited, [StringComparison]::OrdinalIgnoreCase)) {
        throw "GATE-A-001 readiness evidence contains prohibited material: $prohibited"
    }
}
$evidence = $evidenceContent | ConvertFrom-Json
if ($evidence.ticket -ne 'GATE-A-001' -or
    $evidence.status -ne 'InProgress' -or
    $evidence.result -ne 'Passed' -or
    $evidence.fullDemonstrationComplete -ne $false -or
    $evidence.activeDataRootModified -ne $false -or
    $evidence.networkAuthorityAdded -ne $false -or
    $evidence.fixtureRevision -ne 1 -or
    $evidence.fixtureSha256 -ne $fixtureHash) {
    throw 'GATE-A-001 readiness evidence does not match the deterministic fixture or bounded status.'
}

$backlog = [IO.File]::ReadAllText($backlogPath)
foreach ($requiredStep in @(
    'Start from a clean Development-channel data root.',
    'Create a local Recovery Point.',
    'Show active data root was not modified.',
    'Show final Trust Centre timeline and evidence completeness.')) {
    if (-not $backlog.Contains($requiredStep, [StringComparison]::Ordinal)) {
        throw "GATE-A-001 backlog step is missing: $requiredStep"
    }
}

$recoveryTest = [IO.File]::ReadAllText($recoveryTestPath)
foreach ($requiredBehaviour in @(
    'NamedPipeGatewayServer.StartAsync',
    'recovery create --channel Development',
    'backup.recovery-point-created',
    'backup.verification-completed')) {
    if (-not $recoveryTest.Contains($requiredBehaviour, [StringComparison]::Ordinal)) {
        throw "GATE-A-001 recovery proof is missing: $requiredBehaviour"
    }
}

Write-Host "Fixture SHA-256: $fixtureHash"
Write-Host 'GATE-A-001 readiness verification passed; the complete demonstration remains pending.' -ForegroundColor Green
