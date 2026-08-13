#requires -Version 7.2

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
$evidencePath = Join-Path $repositoryRoot 'eng\evidence\milestones\M6\gate-a-008-accessibility-baseline.json'
$receiptPath = Join-Path $repositoryRoot 'eng\evidence\milestones\M6\gate-a-008-accessibility-baseline.sha256'
if (-not (Test-Path -LiteralPath $evidencePath -PathType Leaf) -or
    -not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) {
    throw 'GATE-A-008 evidence or SHA-256 receipt is missing.'
}

$report = Get-Content -LiteralPath $evidencePath -Raw | ConvertFrom-Json
if ($report.schema -ne 'opure.gate-a.accessibility-baseline/1' -or
    $report.ticket -ne 'GATE-A-008' -or
    $report.result -ne 'Passed') {
    throw 'GATE-A-008 evidence identity or result is invalid.'
}

$requiredFlows = @(
    'launch', 'Runtime health', 'open project', 'project list',
    'configuration review', 'Trust Centre Overview', 'Project evidence',
    'Configuration evidence', 'invalid-source warning',
    'Recovery Point creation', 'Recovery Point verification', 'error handling'
)
if (@($report.flows).Count -ne $requiredFlows.Count) {
    throw 'GATE-A-008 does not contain exactly the required flow matrix.'
}
foreach ($flow in $requiredFlows) {
    $entry = @($report.flows | Where-Object flow -eq $flow)
    if ($entry.Count -ne 1 -or $entry[0].result -ne 'Passed' -or
        [string]::IsNullOrWhiteSpace($entry[0].automatedProof)) {
        throw "GATE-A-008 flow proof is missing or failed: $flow"
    }
}

if (@($report.acceptanceCriteria).Count -ne 12 -or
    @($report.acceptanceCriteria | Where-Object result -ne 'Passed').Count -ne 0) {
    throw 'GATE-A-008 acceptance criteria are incomplete or failed.'
}
if ($report.keyboardAutomation.result -ne 'Passed' -or
    $report.narratorReview.result -ne 'Passed' -or
    $report.highContrast.result -ne 'Passed' -or
    $report.progressAndCancellation.result -ne 'Passed' -or
    $report.frameworkDecision.decision -ne 'Retain Avalonia for Gate A' -or
    @($report.frameworkDecision.limitations).Count -lt 2) {
    throw 'GATE-A-008 accessibility review evidence is incomplete.'
}
if ($report.security.desktopReadsTrustDatabase -or
    $report.security.mutationActionsAdded -or
    $report.security.networkCapabilityAdded -or
    $report.security.aiLoaded -or
    $report.security.pluginsLoaded -or
    $report.security.mcpLoaded -or
    $report.security.connectorsLoaded) {
    throw 'GATE-A-008 security boundary assertions failed.'
}

$expectedHash = (Get-Content -LiteralPath $receiptPath -Raw).Trim()
$actualHash = (Get-FileHash -LiteralPath $evidencePath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($expectedHash -notmatch '^[a-f0-9]{64}$' -or $expectedHash -ne $actualHash) {
    throw 'GATE-A-008 SHA-256 receipt does not match the accessibility evidence.'
}

Write-Host 'GATE-A-008 accessibility baseline verification passed.' -ForegroundColor Green
