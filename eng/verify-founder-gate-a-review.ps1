#requires -Version 7.2

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
$reviewPath = Join-Path $repositoryRoot 'eng\evidence\milestones\M6\founder-gate-a.md'
if (-not (Test-Path -LiteralPath $reviewPath -PathType Leaf)) {
    throw 'GATE-A-010 founder review is missing.'
}
$review = Get-Content -LiteralPath $reviewPath -Raw

foreach ($required in @(
    'Build identity: `0a25b3425abe325c78ee8e9deaaf37984448a07e`',
    'Decision authority:',
    '“With Gate A (Foundation/Stability) cleared',
    '**Accept with Amendments.**',
    'Complete Release verification: **765 tests passed, zero warnings, zero errors**',
    'Evidence failures and accepted limitations',
    'Required amendments',
    'ADR status decisions',
    'Phase 7 entry: **Approved**',
    'No ADR is marked Accepted solely because code exists')) {
    if (-not $review.Contains($required, [StringComparison]::Ordinal)) {
        throw "GATE-A-010 required review evidence is missing: $required"
    }
}

for ($index = 1; $index -le 10; $index++) {
    if (-not $review.Contains("$index.", [StringComparison]::Ordinal)) {
        throw "GATE-A-010 review question $index is unanswered."
    }
}

$amendmentRows = [regex]::Matches(
    $review,
    '(?m)^\| (?!---)(?<amendment>.+?) \| (?<owner>.+?) \| (?<date>\d{1,2} [A-Za-z]+ 2026) \| (?<impact>.+?) \|$')
if ($amendmentRows.Count -lt 5) {
    throw 'GATE-A-010 amendments do not have sufficient explicit owners and dates.'
}

& git -C $repositoryRoot cat-file -e '0a25b3425abe325c78ee8e9deaaf37984448a07e^{commit}'
if ($LASTEXITCODE -ne 0) {
    throw 'GATE-A-010 build identity does not resolve to a commit.'
}

foreach ($evidence in @(
    'founder-gate-a-001-checklist.json',
    'gate-a-002-crash-recovery-matrix.json',
    'gate-a-003-ipc-security-matrix.json',
    'gate-a-004-filesystem-adversarial-matrix.json',
    'gate-a-005-configuration-adversarial-matrix.json',
    'gate-a-006-trust-reconciliation-matrix.json',
    'gate-a-007-performance-baseline.json',
    'gate-a-008-accessibility-baseline.json',
    'adr-evidence-matrix.md')) {
    $path = Join-Path $repositoryRoot "eng\evidence\milestones\M6\$evidence"
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "GATE-A-010 prerequisite evidence is missing: $evidence"
    }
}

Write-Host 'GATE-A-010 Founder Gate A review verification passed.' -ForegroundColor Green
