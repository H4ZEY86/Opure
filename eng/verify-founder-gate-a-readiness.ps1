#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot
$fixtureRoot = Join-Path $repositoryRoot 'eng\fixtures\founder-gate-a'
$evidencePath = Join-Path $repositoryRoot 'eng\evidence\milestones\M6\founder-gate-a-001-readiness.json'
$checklistPath = Join-Path $repositoryRoot 'eng\evidence\milestones\M6\founder-gate-a-001-checklist.json'
$backlogPath = Join-Path $repositoryRoot 'specs\BACKLOG-001-foundation-first-12-weeks.md'
$recoveryTestPath = Join-Path $repositoryRoot 'tests\EndToEnd\Opure.EndToEnd.Tests\RecoveryPointCliPipelineTests.cs'

Write-Host ''
Write-Host '==> Verify GATE-A-001 demonstration readiness' -ForegroundColor Cyan

foreach ($requiredPath in @(
    $fixtureRoot,
    $evidencePath,
    $checklistPath,
    $backlogPath,
    $recoveryTestPath,
    (Join-Path $repositoryRoot 'eng\run-bootstrap.ps1'),
    (Join-Path $repositoryRoot 'eng\run-founder-gate-a.ps1'),
    (Join-Path $repositoryRoot 'src\Desktop\Opure.Desktop\RecoveryPointView.axaml'))) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "GATE-A-001 readiness input is missing: $requiredPath"
    }
}

$checklistContent = [IO.File]::ReadAllText($checklistPath)
$checklist = $checklistContent | ConvertFrom-Json
if ($checklist.ticket -ne 'GATE-A-001' -or
    $checklist.status -ne 'InProgress' -or
    $checklist.fullDemonstrationComplete -ne $false -or
    $checklist.steps.Count -ne 32) {
    throw 'GATE-A-001 checklist does not retain its bounded in-progress state.'
}

for ($index = 0; $index -lt 32; $index++) {
    if ($checklist.steps[$index].id -ne ($index + 1)) {
        throw 'GATE-A-001 checklist step identifiers must be sequential from 1 through 32.'
    }
}

$readyStepIds = @($checklist.steps |
    Where-Object { $_.status -eq 'Ready' } |
    ForEach-Object { [int]$_.id })
if (($readyStepIds -join ',') -ne '1,2,3,4,5,6,7,8' -or
    $checklist.steps[8].automation -ne 'Partial' -or
    $checklist.steps[8].status -ne 'Pending' -or
    [string]::IsNullOrWhiteSpace($checklist.steps[8].blocker)) {
    throw 'GATE-A-001 checklist readiness must remain bounded to proven steps 1 through 8.'
}

foreach ($requiredAssertion in @(
    'No network endpoint is owned by a Gate A child process.',
    'No AI runtime process is spawned by the Gate A process tree.',
    'No plugin process is spawned by the Gate A process tree.',
    'No MCP process is spawned by the Gate A process tree.',
    'No agent or skill host is spawned by the Gate A process tree.',
    'No Linux-style output or data path is used.',
    'The checked-in fixture is unchanged.')) {
    if ($requiredAssertion -notin $checklist.negativeAssertions) {
        throw "GATE-A-001 negative assertion is missing: $requiredAssertion"
    }
}

$runnerContent = [IO.File]::ReadAllText(
    (Join-Path $repositoryRoot 'eng\run-founder-gate-a.ps1'))
foreach ($requiredProbe in @(
    "'gate-a', 'probe'",
    'serverProofVerified',
    'invalidSessionDenied',
    'rootIdentityVerified',
    'repositoryClass')) {
    if (-not $runnerContent.Contains($requiredProbe, [StringComparison]::Ordinal)) {
        throw "GATE-A-001 live probe assertion is missing: $requiredProbe"
    }
}
foreach ($prohibitedPersistence in @(
    'bootstrap.stdout.jsonl',
    'bootstrap.stderr.txt')) {
    if ($runnerContent.Contains($prohibitedPersistence, [StringComparison]::Ordinal)) {
        throw "GATE-A-001 runner must not persist session-bearing process output: $prohibitedPersistence"
    }
}

$cliContent = [IO.File]::ReadAllText(
    (Join-Path $repositoryRoot 'src\Cli\Opure.Cli\Program.cs'))
foreach ($requiredBoundary in @(
    'OPURE_GATE_A_TEST_MODE',
    'The Gate A probe is restricted to the bounded engineering harness.',
    'WindowsPathReferenceResolver.AcquireRoot',
    'Invalid session: Denied')) {
    if (-not $cliContent.Contains($requiredBoundary, [StringComparison]::Ordinal)) {
        throw "GATE-A-001 CLI probe boundary is missing: $requiredBoundary"
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
