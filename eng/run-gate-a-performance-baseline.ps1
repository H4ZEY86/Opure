#requires -Version 7.2

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Release')]
    [string] $Configuration = 'Release'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

if (-not $IsWindows) {
    throw 'GATE-A-007 performance evidence requires the Windows 11 reference environment.'
}

$rawEvidenceRoot = Join-Path $repositoryRoot 'artifacts\evidence\founder-gate-a\gate-a-007'
$committedEvidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M6'
$runtimeAssembly = Join-Path $repositoryRoot 'artifacts\bin\Opure.Runtime\release\Opure.Runtime.dll'
$desktopExecutable = Join-Path $repositoryRoot 'artifacts\bin\Opure.Desktop\release\Opure.Desktop.exe'
$solution = Join-Path $repositoryRoot 'Opure.slnx'

New-Item -ItemType Directory -Force -Path $rawEvidenceRoot | Out-Null
New-Item -ItemType Directory -Force -Path $committedEvidenceRoot | Out-Null

function Invoke-CheckedDotNet {
    param(
        [Parameter(Mandatory)]
        [string[]] $Arguments
    )

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet failed with exit code $LASTEXITCODE."
    }
}

function Invoke-TimedDotNet {
    param(
        [Parameter(Mandatory)]
        [string[]] $Arguments
    )

    $timer = [System.Diagnostics.Stopwatch]::StartNew()
    Invoke-CheckedDotNet -Arguments $Arguments | Out-Host
    $timer.Stop()
    return [math]::Round($timer.Elapsed.TotalMilliseconds, 3)
}

function Invoke-PerformanceTest {
    param(
        [Parameter(Mandatory)]
        [string] $Project,

        [Parameter(Mandatory)]
        [string] $ClassName,

        [Parameter(Mandatory)]
        [string] $EvidenceVariable,

        [Parameter(Mandatory)]
        [string] $EvidencePath
    )

    $prior = [Environment]::GetEnvironmentVariable($EvidenceVariable, 'Process')
    [Environment]::SetEnvironmentVariable(
        $EvidenceVariable,
        $EvidencePath,
        'Process')
    try {
        Invoke-CheckedDotNet -Arguments @(
            'test'
            $Project
            '--configuration'
            $Configuration
            '--no-restore'
            '--'
            '--filter-class'
            $ClassName
        )
    }
    finally {
        [Environment]::SetEnvironmentVariable(
            $EvidenceVariable,
            $prior,
            'Process')
    }

    if (-not (Test-Path -LiteralPath $EvidencePath -PathType Leaf)) {
        throw "Performance test did not write its evidence: $EvidencePath"
    }
}

function Get-DirectorySizeBytes {
    param([Parameter(Mandatory)][string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        return 0L
    }

    return [long](@(
        Get-ChildItem -LiteralPath $Path -File -Recurse -Force -ErrorAction Stop |
            Measure-Object -Property Length -Sum
    )[0].Sum ?? 0L)
}

function Get-OwnedNetworkEndpointCount {
    param([Parameter(Mandatory)][int] $ProcessId)

    $tcp = @(Get-NetTCPConnection -OwningProcess $ProcessId -ErrorAction SilentlyContinue)
    $udp = @(Get-NetUDPEndpoint -OwningProcess $ProcessId -ErrorAction SilentlyContinue)
    return [pscustomobject]@{
        Tcp = $tcp.Count
        Udp = $udp.Count
    }
}

Write-Host ''
Write-Host '==> GATE-A-007 clean and incremental build measurements' -ForegroundColor Cyan
Invoke-CheckedDotNet -Arguments @('clean', $solution, '--configuration', $Configuration)
Invoke-CheckedDotNet -Arguments @('restore', $solution, '--locked-mode')
$cleanBuildMilliseconds = Invoke-TimedDotNet -Arguments @(
    'build', $solution, '--configuration', $Configuration, '--no-restore')
$incrementalBuildMilliseconds = Invoke-TimedDotNet -Arguments @(
    'build', $solution, '--configuration', $Configuration, '--no-restore')

if (-not (Test-Path -LiteralPath $runtimeAssembly -PathType Leaf)) {
    throw "Runtime assembly was not produced: $runtimeAssembly"
}
if (-not (Test-Path -LiteralPath $desktopExecutable -PathType Leaf)) {
    throw "Desktop executable was not produced: $desktopExecutable"
}

Write-Host ''
Write-Host '==> GATE-A-007 Runtime readiness and resource measurements' -ForegroundColor Cyan
$runtimeDataRoot = Join-Path $rawEvidenceRoot 'runtime-data'
$runtimeStart = [System.Diagnostics.ProcessStartInfo]::new()
$runtimeStart.FileName = 'dotnet'
$runtimeStart.UseShellExecute = $false
$runtimeStart.RedirectStandardOutput = $true
$runtimeStart.RedirectStandardError = $true
$runtimeStart.CreateNoWindow = $true
$runtimeStart.WorkingDirectory = $repositoryRoot
$runtimeStart.Environment['OPURE_RUNTIME_TEST_MODE'] = '1'
[void]$runtimeStart.ArgumentList.Add($runtimeAssembly)
[void]$runtimeStart.ArgumentList.Add('--shutdown-after-ms')
[void]$runtimeStart.ArgumentList.Add('2500')
[void]$runtimeStart.ArgumentList.Add('--data-root')
[void]$runtimeStart.ArgumentList.Add($runtimeDataRoot)
$runtime = [System.Diagnostics.Process]::new()
$runtime.StartInfo = $runtimeStart
$runtimeTimer = [System.Diagnostics.Stopwatch]::StartNew()
$runtimeReadyMilliseconds = $null
$runtimeLines = [System.Collections.Generic.List[string]]::new()

try {
    if (-not $runtime.Start()) {
        throw 'Runtime performance process did not start.'
    }

    $runtimeErrorTask = $runtime.StandardError.ReadToEndAsync()
    $readyDeadline = [DateTimeOffset]::UtcNow.AddSeconds(5)
    $lineTask = $runtime.StandardOutput.ReadLineAsync()
    while ([DateTimeOffset]::UtcNow -lt $readyDeadline -and -not $runtime.HasExited) {
        if (-not $lineTask.Wait(250)) {
            continue
        }
        $line = $lineTask.GetAwaiter().GetResult()
        if ($null -eq $line) {
            break
        }
        $runtimeLines.Add($line)
        if ($line -match '"event":"runtime.lifecycle"' -and $line -match '"state":"ready"') {
            $runtimeReadyMilliseconds = [math]::Round($runtimeTimer.Elapsed.TotalMilliseconds, 3)
            break
        }
        $lineTask = $runtime.StandardOutput.ReadLineAsync()
    }

    if ($null -eq $runtimeReadyMilliseconds) {
        throw 'Runtime did not report ready within five seconds.'
    }

    $runtime.Refresh()
    $cpuBefore = $runtime.TotalProcessorTime
    Start-Sleep -Milliseconds 1000
    $runtime.Refresh()
    $cpuAfter = $runtime.TotalProcessorTime
    $runtimeWorkingSetBytes = [long]$runtime.WorkingSet64
    $runtimeIdleCpuPercent = [math]::Round(
        (($cpuAfter - $cpuBefore).TotalMilliseconds / 1000) *
            100 / [Environment]::ProcessorCount,
        3)
    $runtimeNetwork = Get-OwnedNetworkEndpointCount -ProcessId $runtime.Id

    if (-not $runtime.WaitForExit(10000)) {
        $runtime.Kill($true)
        throw 'Runtime performance process exceeded its shutdown deadline.'
    }
    $runtimeError = $runtimeErrorTask.GetAwaiter().GetResult()
    if ($runtime.ExitCode -ne 0) {
        throw "Runtime performance process exited with code $($runtime.ExitCode)."
    }
    if (-not [string]::IsNullOrWhiteSpace($runtimeError)) {
        throw "Runtime performance process wrote unexpected stderr: $runtimeError"
    }
}
finally {
    if (-not $runtime.HasExited) {
        try { $runtime.Kill($true) } catch { }
    }
    $runtime.Dispose()
}

$runtimeDiskGrowthBytes = Get-DirectorySizeBytes -Path $runtimeDataRoot

Write-Host ''
Write-Host '==> GATE-A-007 Desktop visibility and resource measurements' -ForegroundColor Cyan
$desktopStart = [System.Diagnostics.ProcessStartInfo]::new()
$desktopStart.FileName = $desktopExecutable
$desktopStart.UseShellExecute = $false
$desktopStart.WorkingDirectory = $repositoryRoot
$desktopStart.Environment['OPURE_DESKTOP_TEST_MODE'] = '1'
[void]$desktopStart.ArgumentList.Add('--close-after-ms')
[void]$desktopStart.ArgumentList.Add('2500')
$desktop = [System.Diagnostics.Process]::new()
$desktop.StartInfo = $desktopStart
$desktopTimer = [System.Diagnostics.Stopwatch]::StartNew()

try {
    if (-not $desktop.Start()) {
        throw 'Desktop performance process did not start.'
    }
    $desktopVisibleMilliseconds = $null
    $desktopDeadline = [DateTimeOffset]::UtcNow.AddSeconds(15)
    while ([DateTimeOffset]::UtcNow -lt $desktopDeadline -and -not $desktop.HasExited) {
        $desktop.Refresh()
        if ($desktop.MainWindowHandle -ne [IntPtr]::Zero) {
            $desktopVisibleMilliseconds = [math]::Round(
                $desktopTimer.Elapsed.TotalMilliseconds,
                3)
            break
        }
        Start-Sleep -Milliseconds 10
    }
    if ($null -eq $desktopVisibleMilliseconds) {
        if ($desktop.HasExited) {
            throw "Desktop exited before exposing a real main window; exit code $($desktop.ExitCode)."
        }
        throw 'Desktop did not expose a real main window within fifteen seconds.'
    }

    $desktop.Refresh()
    $desktopWorkingSetBytes = [long]$desktop.WorkingSet64
    $desktopNetwork = Get-OwnedNetworkEndpointCount -ProcessId $desktop.Id
    if (-not $desktop.WaitForExit(10000)) {
        $desktop.Kill($true)
        throw 'Desktop performance process exceeded its shutdown deadline.'
    }
    if ($desktop.ExitCode -ne 0) {
        throw "Desktop performance process exited with code $($desktop.ExitCode)."
    }
}
finally {
    if (-not $desktop.HasExited) {
        try { $desktop.Kill($true) } catch { }
    }
    $desktop.Dispose()
}

if (($runtimeNetwork.Tcp + $runtimeNetwork.Udp + $desktopNetwork.Tcp + $desktopNetwork.Udp) -ne 0) {
    throw 'A performance probe owned an unexpected TCP or UDP endpoint.'
}

Write-Host ''
Write-Host '==> GATE-A-007 service-level measurements' -ForegroundColor Cyan
$ipcPath = Join-Path $rawEvidenceRoot 'ipc.json'
$workspacePath = Join-Path $rawEvidenceRoot 'workspace.json'
$configurationPath = Join-Path $rawEvidenceRoot 'configuration.json'
$persistencePath = Join-Path $rawEvidenceRoot 'persistence.json'
$trustPath = Join-Path $rawEvidenceRoot 'trust.json'
$projectPath = Join-Path $rawEvidenceRoot 'project.json'
$recoveryPath = Join-Path $rawEvidenceRoot 'recovery.json'

Invoke-PerformanceTest `
    -Project 'tests\Ipc\Opure.Ipc.NamedPipes.Windows.Tests\Opure.Ipc.NamedPipes.Windows.Tests.csproj' `
    -ClassName 'Opure.Ipc.NamedPipes.Windows.Tests.GateAPerformanceBaselineTests' `
    -EvidenceVariable 'OPURE_GATE_A_PERFORMANCE_IPC_PATH' `
    -EvidencePath $ipcPath
Invoke-PerformanceTest `
    -Project 'tests\Workspace\Opure.Workspace.Windows.Tests\Opure.Workspace.Windows.Tests.csproj' `
    -ClassName 'Opure.Workspace.Windows.Tests.GateAWorkspacePerformanceTests' `
    -EvidenceVariable 'OPURE_GATE_A_PERFORMANCE_WORKSPACE_PATH' `
    -EvidencePath $workspacePath
Invoke-PerformanceTest `
    -Project 'tests\Configuration\Opure.Configuration.Tests\Opure.Configuration.Tests.csproj' `
    -ClassName 'Opure.Configuration.Tests.GateAConfigurationPerformanceTests' `
    -EvidenceVariable 'OPURE_GATE_A_PERFORMANCE_CONFIGURATION_PATH' `
    -EvidencePath $configurationPath
Invoke-PerformanceTest `
    -Project 'tests\Persistence\Opure.Persistence.Sqlite.Tests\Opure.Persistence.Sqlite.Tests.csproj' `
    -ClassName 'Opure.Persistence.Sqlite.Tests.GateAPersistencePerformanceTests' `
    -EvidenceVariable 'OPURE_GATE_A_PERFORMANCE_PERSISTENCE_PATH' `
    -EvidencePath $persistencePath
Invoke-PerformanceTest `
    -Project 'tests\Trust\Opure.TrustEvidence.Sqlite.Tests\Opure.TrustEvidence.Sqlite.Tests.csproj' `
    -ClassName 'Opure.TrustEvidence.Sqlite.Tests.GateATrustPerformanceTests' `
    -EvidenceVariable 'OPURE_GATE_A_PERFORMANCE_TRUST_PATH' `
    -EvidencePath $trustPath
Invoke-PerformanceTest `
    -Project 'tests\Project\Opure.Project.Sqlite.Tests\Opure.Project.Sqlite.Tests.csproj' `
    -ClassName 'Opure.Project.Sqlite.Tests.GateAProjectOpenPerformanceTests' `
    -EvidenceVariable 'OPURE_GATE_A_PERFORMANCE_PROJECT_PATH' `
    -EvidencePath $projectPath
Invoke-PerformanceTest `
    -Project 'tests\Recovery\Opure.Recovery.Service.Tests\Opure.Recovery.Service.Tests.csproj' `
    -ClassName 'Opure.Recovery.Service.Tests.GateARecoveryPerformanceTests' `
    -EvidenceVariable 'OPURE_GATE_A_PERFORMANCE_RECOVERY_PATH' `
    -EvidencePath $recoveryPath

$ipc = Get-Content -LiteralPath $ipcPath -Raw | ConvertFrom-Json
$workspace = Get-Content -LiteralPath $workspacePath -Raw | ConvertFrom-Json
$configurationEvidence = Get-Content -LiteralPath $configurationPath -Raw | ConvertFrom-Json
$persistence = Get-Content -LiteralPath $persistencePath -Raw | ConvertFrom-Json
$trust = Get-Content -LiteralPath $trustPath -Raw | ConvertFrom-Json
$project = Get-Content -LiteralPath $projectPath -Raw | ConvertFrom-Json
$recovery = Get-Content -LiteralPath $recoveryPath -Raw | ConvertFrom-Json

$commit = (& git -C $repositoryRoot rev-parse HEAD).Trim()
$sourceStatus = @(& git -C $repositoryRoot status --short)
$os = Get-CimInstance Win32_OperatingSystem
$cpu = Get-CimInstance Win32_Processor | Select-Object -First 1
$gpu = Get-CimInstance Win32_VideoController |
    Where-Object { $_.Name -match 'NVIDIA|AMD|Intel' } |
    Select-Object -First 1
$powerPlan = (powercfg /getactivescheme) -join ' '
$hardwareMaterial = @(
    $os.Caption
    $os.Version
    $os.BuildNumber
    $cpu.Name
    $cpu.NumberOfCores
    $cpu.NumberOfLogicalProcessors
    $os.TotalVisibleMemorySize
    $gpu.Name
) -join '|'
$hardwareIdentity = [Convert]::ToHexStringLower(
    [Security.Cryptography.SHA256]::HashData(
        [Text.Encoding]::UTF8.GetBytes($hardwareMaterial)))
$buildIdentity = "$commit-release-development"

function New-Measurement {
    param(
        [Parameter(Mandatory)][string] $Name,
        [Parameter(Mandatory)][string] $Unit,
        [Parameter(Mandatory)][double] $Value,
        [Parameter(Mandatory)][string] $Fixture,
        [Parameter()][Nullable[double]] $P50,
        [Parameter()][Nullable[double]] $P95,
        [Parameter()][Nullable[double]] $P99,
        [Parameter()][Nullable[double]] $RoadmapTarget,
        [Parameter(Mandatory)][double] $RegressionThreshold,
        [Parameter(Mandatory)][bool] $LowerIsBetter
    )

    $targetDecision = if ($null -eq $RoadmapTarget) {
        'NotApplicable'
    }
    elseif (($LowerIsBetter -and $Value -lt $RoadmapTarget) -or
            (-not $LowerIsBetter -and $Value -gt $RoadmapTarget)) {
        'Met'
    }
    else {
        'MissedDocumented'
    }

    return [ordered]@{
        name = $Name
        unit = $Unit
        value = [math]::Round($Value, 3)
        p50 = if ($null -ne $P50) { [math]::Round($P50, 3) } else { $null }
        p95 = if ($null -ne $P95) { [math]::Round($P95, 3) } else { $null }
        p99 = if ($null -ne $P99) { [math]::Round($P99, 3) } else { $null }
        roadmapTarget = if ($null -ne $RoadmapTarget) { $RoadmapTarget } else { $null }
        targetDecision = $targetDecision
        regressionThreshold = $RegressionThreshold
        thresholdDecision = if (($LowerIsBetter -and $Value -le $RegressionThreshold) -or
            (-not $LowerIsBetter -and $Value -ge $RegressionThreshold)) { 'Passed' } else { 'Failed' }
        buildIdentity = $buildIdentity
        hardwareIdentity = $hardwareIdentity
        fixtureIdentity = $Fixture
        securityControlsEnabled = $true
    }
}

$measurements = @(
    New-Measurement -Name 'cleanBuild' -Unit 'milliseconds' -Value $cleanBuildMilliseconds -Fixture 'Opure.slnx;locked-restore;Release' -RegressionThreshold 180000 -LowerIsBetter $true
    New-Measurement -Name 'incrementalBuild' -Unit 'milliseconds' -Value $incrementalBuildMilliseconds -Fixture 'Opure.slnx;no-source-change;Release' -RegressionThreshold 30000 -LowerIsBetter $true
    New-Measurement -Name 'desktopShellVisible' -Unit 'milliseconds' -Value $desktopVisibleMilliseconds -Fixture 'real-Avalonia-window;Development' -RoadmapTarget 2000 -RegressionThreshold 15000 -LowerIsBetter $true
    New-Measurement -Name 'runtimeReady' -Unit 'milliseconds' -Value $runtimeReadyMilliseconds -Fixture 'clean-isolated-runtime-root;Development' -RoadmapTarget 3000 -RegressionThreshold 5000 -LowerIsBetter $true
    New-Measurement -Name 'desktopReconnect' -Unit 'milliseconds' -Value $ipc.measurements.desktopReconnect.p95Milliseconds -Fixture '21-authenticated-session-rotations' -P50 $ipc.measurements.desktopReconnect.p50Milliseconds -P95 $ipc.measurements.desktopReconnect.p95Milliseconds -P99 $ipc.measurements.desktopReconnect.p99Milliseconds -RoadmapTarget 500 -RegressionThreshold 1000 -LowerIsBetter $true
    New-Measurement -Name 'ipcHealth' -Unit 'milliseconds' -Value $ipc.measurements.ipcHealth.p95Milliseconds -Fixture '201-authenticated-health-calls' -P50 $ipc.measurements.ipcHealth.p50Milliseconds -P95 $ipc.measurements.ipcHealth.p95Milliseconds -P99 $ipc.measurements.ipcHealth.p99Milliseconds -RoadmapTarget 10 -RegressionThreshold 10 -LowerIsBetter $true
    New-Measurement -Name 'serviceRegistryQuery' -Unit 'milliseconds' -Value $ipc.measurements.serviceRegistryQueryMilliseconds -Fixture 'authenticated-registry-query' -RegressionThreshold 100 -LowerIsBetter $true
    New-Measurement -Name 'sqliteTransaction' -Unit 'milliseconds' -Value $persistence.measurements.sqliteTransactionP95Milliseconds -Fixture '201-immediate-transactions' -P50 $persistence.measurements.sqliteTransactionP50Milliseconds -P95 $persistence.measurements.sqliteTransactionP95Milliseconds -P99 $persistence.measurements.sqliteTransactionP99Milliseconds -RegressionThreshold 100 -LowerIsBetter $true
    New-Measurement -Name 'outboxCommit' -Unit 'milliseconds' -Value $persistence.measurements.outboxCommitP95Milliseconds -Fixture '201-immutable-outbox-commits' -P50 $persistence.measurements.outboxCommitP50Milliseconds -P95 $persistence.measurements.outboxCommitP95Milliseconds -P99 $persistence.measurements.outboxCommitP99Milliseconds -RegressionThreshold 100 -LowerIsBetter $true
    New-Measurement -Name 'evidenceIngestion' -Unit 'milliseconds' -Value $trust.measurements.evidenceIngestionP95Milliseconds -Fixture '10000-authenticated-records' -P50 $trust.measurements.evidenceIngestionP50Milliseconds -P95 $trust.measurements.evidenceIngestionP95Milliseconds -P99 $trust.measurements.evidenceIngestionP99Milliseconds -RoadmapTarget 20 -RegressionThreshold 20 -LowerIsBetter $true
    New-Measurement -Name 'smallProjectOpen' -Unit 'milliseconds' -Value $project.measurements.smallProjectOpenP95Milliseconds -Fixture '11-cold-roots;50-files-each' -P50 $project.measurements.smallProjectOpenP50Milliseconds -P95 $project.measurements.smallProjectOpenP95Milliseconds -P99 $project.measurements.smallProjectOpenP99Milliseconds -RoadmapTarget 1000 -RegressionThreshold 1000 -LowerIsBetter $true
    New-Measurement -Name 'mediumProjectMetadataOpen' -Unit 'milliseconds' -Value $project.measurements.mediumProjectMetadataOpenP95Milliseconds -Fixture '11-cold-roots;2000-files-each' -P50 $project.measurements.mediumProjectMetadataOpenP50Milliseconds -P95 $project.measurements.mediumProjectMetadataOpenP95Milliseconds -P99 $project.measurements.mediumProjectMetadataOpenP99Milliseconds -RoadmapTarget 3000 -RegressionThreshold 3000 -LowerIsBetter $true
    New-Measurement -Name 'workspaceInventory' -Unit 'milliseconds' -Value $workspace.measurements.workspaceInventoryMilliseconds -Fixture '1001-files;verified-root' -RegressionThreshold 20000 -LowerIsBetter $true
    New-Measurement -Name 'fileHashingThroughput' -Unit 'MiB/s' -Value $workspace.measurements.fileHashMiBPerSecond -Fixture '16-MiB;SHA-256;verified-handle' -RegressionThreshold 10 -LowerIsBetter $false
    New-Measurement -Name 'effectiveConfigurationBuild' -Unit 'milliseconds' -Value $configurationEvidence.measurements.p95Milliseconds -Fixture '201-balanced-policy-builds' -P50 $configurationEvidence.measurements.p50Milliseconds -P95 $configurationEvidence.measurements.p95Milliseconds -P99 $configurationEvidence.measurements.p99Milliseconds -RoadmapTarget 100 -RegressionThreshold 100 -LowerIsBetter $true
    New-Measurement -Name 'trustQuery10000' -Unit 'milliseconds' -Value $trust.measurements.trustQueryP95Milliseconds -Fixture '10000-record-authorised-project' -P50 $trust.measurements.trustQueryP50Milliseconds -P95 $trust.measurements.trustQueryP95Milliseconds -P99 $trust.measurements.trustQueryP99Milliseconds -RoadmapTarget 100 -RegressionThreshold 100 -LowerIsBetter $true
    New-Measurement -Name 'localRecoveryPointConsistencyBarrier' -Unit 'milliseconds' -Value $recovery.measurements.p95Milliseconds -Fixture '21-points;1-owner;1-MiB-checkpoint' -P50 $recovery.measurements.p50Milliseconds -P95 $recovery.measurements.p95Milliseconds -P99 $recovery.measurements.p99Milliseconds -RoadmapTarget 2000 -RegressionThreshold 2000 -LowerIsBetter $true
    New-Measurement -Name 'sqliteBackupThroughput' -Unit 'MiB/s' -Value $persistence.measurements.sqliteBackupMiBPerSecond -Fixture '32-MiB-payload;online-backup' -RegressionThreshold 10 -LowerIsBetter $false
    New-Measurement -Name 'disposableRestoreValidation' -Unit 'milliseconds' -Value $persistence.measurements.disposableRestoreValidationMilliseconds -Fixture 'read-only-backup;quick-check;201-rows' -RegressionThreshold 5000 -LowerIsBetter $true
    New-Measurement -Name 'idleCpu' -Unit 'percent' -Value $runtimeIdleCpuPercent -Fixture 'runtime-ready;1-second-window' -RegressionThreshold 5 -LowerIsBetter $true
    New-Measurement -Name 'workingSetMemory' -Unit 'bytes' -Value ($runtimeWorkingSetBytes + $desktopWorkingSetBytes) -Fixture 'runtime-plus-real-desktop' -RegressionThreshold 1073741824 -LowerIsBetter $true
    New-Measurement -Name 'diskGrowth' -Unit 'bytes' -Value $runtimeDiskGrowthBytes -Fixture 'isolated-runtime-root;2.5-second-run' -RegressionThreshold 104857600 -LowerIsBetter $true
)

$failedThresholds = @($measurements | Where-Object thresholdDecision -ne 'Passed')
if ($failedThresholds.Count -gt 0) {
    throw "Performance regression thresholds failed: $($failedThresholds.name -join ', ')"
}

$report = [ordered]@{
    schema = 'opure.gate-a.performance-baseline/1'
    result = 'Passed'
    ticket = 'GATE-A-007'
    measuredAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
    build = [ordered]@{
        identity = $buildIdentity
        commit = $commit
        configuration = $Configuration
        channel = 'Development'
        sourceStatusEntryCount = $sourceStatus.Count
    }
    hardware = [ordered]@{
        identity = $hardwareIdentity
        os = $os.Caption
        osVersion = $os.Version
        osBuild = $os.BuildNumber
        processor = $cpu.Name.Trim()
        physicalCores = [int]$cpu.NumberOfCores
        logicalProcessors = [int]$cpu.NumberOfLogicalProcessors
        memoryBytes = [long]$os.TotalVisibleMemorySize * 1024
        graphics = $gpu.Name
        windowsPowerPlan = $powerPlan.Trim()
    }
    performanceMode = [ordered]@{
        opureMode = $configurationEvidence.performanceMode
        acceptanceDecision = if ($configurationEvidence.performanceMode -eq 'Balanced') { 'Passed' } else { 'Failed' }
        windowsPowerPlanObservedSeparately = $powerPlan.Trim()
    }
    security = [ordered]@{
        securityControlsDisabledByBenchmarks = $false
        runtimeTcpEndpoints = $runtimeNetwork.Tcp
        runtimeUdpEndpoints = $runtimeNetwork.Udp
        desktopTcpEndpoints = $desktopNetwork.Tcp
        desktopUdpEndpoints = $desktopNetwork.Udp
        aiLoaded = $false
        pluginsLoaded = $false
        mcpLoaded = $false
        externalConnectorsLoaded = $false
    }
    measurements = $measurements
    cancellationMeasurements = @(
        [ordered]@{
            operation = 'authenticatedIpcCall'
            latencyMilliseconds = $ipc.measurements.cancellationLatencyMilliseconds
            thresholdMilliseconds = $ipc.measurements.cancellationThresholdMilliseconds
            result = 'Passed'
        }
        [ordered]@{
            operation = 'workspaceFileHash'
            latencyMilliseconds = $workspace.measurements.cancellationLatencyMilliseconds
            thresholdMilliseconds = $workspace.measurements.cancellationThresholdMilliseconds
            result = 'Passed'
        }
    )
    roadmapComparison = [ordered]@{
        provisionalTargetsAreReleaseContracts = $false
        missedTargets = @($measurements |
            Where-Object targetDecision -eq 'MissedDocumented' |
            ForEach-Object name)
        rule = 'A missed provisional target is retained and documented; it is never hidden.'
    }
    lowResourceFollowUp = [ordered]@{
        status = 'Identified'
        os = 'Windows 11'
        processor = '4 physical cores'
        memory = '8 GB'
        graphics = 'Integrated graphics'
        channel = 'Development'
        purpose = 'Repeat GATE-A-007 before Stable public-launch readiness.'
    }
}

$reportPath = Join-Path $committedEvidenceRoot 'gate-a-007-performance-baseline.json'
[System.IO.File]::WriteAllText(
    $reportPath,
    ($report | ConvertTo-Json -Depth 12).Replace("`r`n", "`n") + "`n",
    [System.Text.UTF8Encoding]::new($false))

& (Join-Path $PSScriptRoot 'verify-gate-a-performance-baseline.ps1')
if ($LASTEXITCODE -ne 0) {
    throw 'GATE-A-007 verifier failed.'
}

Write-Host ''
Write-Host "GATE-A-007 performance baseline passed: $reportPath" -ForegroundColor Green
