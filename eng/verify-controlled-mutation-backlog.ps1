#requires -Version 7.2

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
$backlogPath = Join-Path $repositoryRoot 'eng\evidence\milestones\M6\controlled-mutation-backlog.md'
if (-not (Test-Path -LiteralPath $backlogPath -PathType Leaf)) {
    throw 'GATE-A-011 Controlled Mutation backlog is missing.'
}
$backlog = Get-Content -LiteralPath $backlogPath -Raw

$tickets = [regex]::Matches($backlog, '(?m)^## (?<id>CM-\d{3}) — (?<title>.+)$')
if ($tickets.Count -ne 16) {
    throw 'GATE-A-011 must define exactly CM-001 through CM-016.'
}

for ($number = 1; $number -le 16; $number++) {
    $id = 'CM-{0:D3}' -f $number
    $heading = "## $id —"
    $start = $backlog.IndexOf($heading, [StringComparison]::Ordinal)
    if ($start -lt 0) {
        throw "GATE-A-011 is missing $id."
    }
    $next = if ($number -lt 16) {
        $backlog.IndexOf(('## CM-{0:D3} —' -f ($number + 1)), $start, [StringComparison]::Ordinal)
    } else {
        $backlog.IndexOf('## Founder Gate B scope', $start, [StringComparison]::Ordinal)
    }
    if ($next -lt 0) {
        throw "GATE-A-011 cannot resolve the boundary after $id."
    }
    $section = $backlog.Substring($start, $next - $start)
    foreach ($field in @(
        '- Outcome:', '- Depends on:', '- ADR links:', '- Specification links:',
        '- Security review:', '- Recovery and compensation:', '- Acceptance:')) {
        if (-not $section.Contains($field, [StringComparison]::Ordinal)) {
            throw "$id is missing required field $field"
        }
    }
    if (-not $section.Contains('ADR-', [StringComparison]::Ordinal) -or
        -not $section.Contains('SPEC-', [StringComparison]::Ordinal)) {
        throw "$id does not link applicable ADR and specification authority."
    }
}

foreach ($required in @(
    'exactly one UTF-8 text file',
    'unified-diff subset',
    'exact preview',
    'approval binds',
    'same volume',
    'rechecks project, Workspace generation, canonical path, file identity',
    'owner receipts',
    'curated read-only command templates',
    'Restricted Command Worker',
    'Timeout and cancellation',
    'bounded local buffers',
    'effect intent',
    'No AI-generated patch',
    'no arbitrary shell',
    'Founder Gate B scope',
    'Gate A amendment carry-over')) {
    if (-not $backlog.Contains($required, [StringComparison]::OrdinalIgnoreCase)) {
        throw "GATE-A-011 required scope or boundary is missing: $required"
    }
}

$criticalPath = 'CM-001 → CM-002 → CM-003 → CM-004 → CM-005 → CM-006 → CM-007 → CM-008 → CM-009 → CM-010 → CM-011 → CM-012 → CM-013 → CM-014 → CM-015 → CM-016'
if (-not $backlog.Contains($criticalPath, [StringComparison]::Ordinal)) {
    throw 'GATE-A-011 critical path is incomplete or reordered.'
}
if (-not $backlog.Contains('Command-worker implementation cannot begin before CM-011', [StringComparison]::Ordinal) -or
    -not $backlog.Contains('Phase 8 Local Intelligence', [StringComparison]::Ordinal)) {
    throw 'GATE-A-011 mutation-before-tools or Founder Gate B boundary is missing.'
}

Write-Host 'GATE-A-011 Controlled Mutation backlog verification passed.' -ForegroundColor Green
