#requires -Version 7.2

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
$path = Join-Path $repositoryRoot 'eng\evidence\milestones\M6\gate-a-007-performance-baseline.json'
if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
    throw "GATE-A-007 evidence is missing: $path"
}

$report = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
if ($report.schema -ne 'opure.gate-a.performance-baseline/1' -or
    $report.ticket -ne 'GATE-A-007' -or
    $report.result -ne 'Passed') {
    throw 'GATE-A-007 evidence identity or result is invalid.'
}

$required = @(
    'cleanBuild',
    'incrementalBuild',
    'desktopShellVisible',
    'runtimeReady',
    'desktopReconnect',
    'ipcHealth',
    'serviceRegistryQuery',
    'sqliteTransaction',
    'outboxCommit',
    'evidenceIngestion',
    'smallProjectOpen',
    'mediumProjectMetadataOpen',
    'workspaceInventory',
    'fileHashingThroughput',
    'effectiveConfigurationBuild',
    'trustQuery10000',
    'localRecoveryPointConsistencyBarrier',
    'sqliteBackupThroughput',
    'disposableRestoreValidation',
    'idleCpu',
    'workingSetMemory',
    'diskGrowth'
)
$actual = @($report.measurements | ForEach-Object name)
if (($actual | Sort-Object -Unique).Count -ne $required.Count) {
    throw 'GATE-A-007 does not contain exactly the required measurement set.'
}
foreach ($name in $required) {
    $measurement = @($report.measurements | Where-Object name -eq $name)
    if ($measurement.Count -ne 1) {
        throw "GATE-A-007 measurement is absent or duplicated: $name"
    }
    $item = $measurement[0]
    if ([string]::IsNullOrWhiteSpace($item.buildIdentity) -or
        [string]::IsNullOrWhiteSpace($item.hardwareIdentity) -or
        [string]::IsNullOrWhiteSpace($item.fixtureIdentity) -or
        -not $item.securityControlsEnabled -or
        $item.thresholdDecision -ne 'Passed' -or
        $item.targetDecision -notin @('Met', 'MissedDocumented', 'NotApplicable')) {
        throw "GATE-A-007 measurement context or decision is invalid: $name"
    }
}

foreach ($name in @(
    'desktopReconnect',
    'ipcHealth',
    'sqliteTransaction',
    'outboxCommit',
    'evidenceIngestion',
    'smallProjectOpen',
    'mediumProjectMetadataOpen',
    'effectiveConfigurationBuild',
    'trustQuery10000',
    'localRecoveryPointConsistencyBarrier')) {
    $item = $report.measurements | Where-Object name -eq $name
    if ($null -eq $item.p95) {
        throw "GATE-A-007 p95 is missing where relevant: $name"
    }
}

if ($report.performanceMode.opureMode -ne 'Balanced' -or
    $report.performanceMode.acceptanceDecision -ne 'Passed') {
    throw 'GATE-A-007 did not use the Opure Balanced performance mode.'
}
if ($report.security.securityControlsDisabledByBenchmarks -or
    $report.security.runtimeTcpEndpoints -ne 0 -or
    $report.security.runtimeUdpEndpoints -ne 0 -or
    $report.security.desktopTcpEndpoints -ne 0 -or
    $report.security.desktopUdpEndpoints -ne 0 -or
    $report.security.aiLoaded -or
    $report.security.pluginsLoaded -or
    $report.security.mcpLoaded -or
    $report.security.externalConnectorsLoaded) {
    throw 'GATE-A-007 security or isolation assertions failed.'
}
if (@($report.cancellationMeasurements).Count -lt 2 -or
    @($report.cancellationMeasurements | Where-Object result -ne 'Passed').Count -ne 0) {
    throw 'GATE-A-007 cancellation evidence is incomplete or failed.'
}
if ($report.lowResourceFollowUp.status -ne 'Identified') {
    throw 'GATE-A-007 low-resource follow-up was not identified.'
}

Write-Host 'GATE-A-007 performance baseline verification passed.' -ForegroundColor Green
