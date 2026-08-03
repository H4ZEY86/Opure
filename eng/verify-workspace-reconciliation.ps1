#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot
$workspaceTests = Join-Path $repositoryRoot 'tests\Workspace\Opure.Workspace.Service.Tests\Opure.Workspace.Service.Tests.csproj'
$architectureTests = Join-Path $repositoryRoot 'tests\Architecture\Opure.ArchitectureTests\Opure.ArchitectureTests.csproj'
$servicePath = Join-Path $repositoryRoot 'src\Workspace\Opure.Workspace.Service\WorkspaceReconciliationService.cs'
$queuePath = Join-Path $repositoryRoot 'src\Workspace\Opure.Workspace.Service\WorkspaceReconciliationQueue.cs'
$watcherPath = Join-Path $repositoryRoot 'src\Workspace\Opure.Workspace.Windows\WindowsWorkspaceChangeWatcher.cs'
$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M4'
$evidencePaths = @(
    (Join-Path $evidenceRoot 'workspace-reconciliation-state-machine.json'),
    (Join-Path $evidenceRoot 'workspace-watcher-loss-report.json'),
    (Join-Path $evidenceRoot 'workspace-edit-storm-benchmark.json'))

Write-Host ''
Write-Host '==> Verify FND-037 Workspace change reconciliation' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') -Configuration Release -BuildChannel Development

& dotnet test $workspaceTests --configuration Release --no-build --no-restore --timeout 60s
if ($LASTEXITCODE -ne 0) { throw 'FND-037 Workspace reconciliation acceptance tests failed.' }

& dotnet test $architectureTests --configuration Release --no-build --no-restore `
    --filter-class 'Opure.ArchitectureTests.WorkspaceServiceBoundaryTests' --timeout 60s
if ($LASTEXITCODE -ne 0) { throw 'FND-037 Workspace architecture tests failed.' }

$service = [System.IO.File]::ReadAllText($servicePath)
$queue = [System.IO.File]::ReadAllText($queuePath)
$watcher = [System.IO.File]::ReadAllText($watcherPath)
foreach ($required in @('inventoryGenerator.Generate', 'fileHasher.HashAsync', 'ComputeCanonicalHash', 'generationStore.Commit', 'Compare')) {
    if (-not $service.Contains($required, [StringComparison]::Ordinal)) {
        throw "FND-037 reconciliation service is missing required behaviour: $required"
    }
}
foreach ($required in @('MaximumPendingHints', 'ForceFullScan', 'WatcherOverflow', 'WatcherUncertain')) {
    if (-not $queue.Contains($required, [StringComparison]::Ordinal)) {
        throw "FND-037 bounded queue is missing required behaviour: $required"
    }
}
foreach ($required in @('FileSystemWatcher', 'InternalBufferOverflowException', 'WatcherOverflow', 'WatcherUncertain')) {
    if (-not $watcher.Contains($required, [StringComparison]::Ordinal)) {
        throw "FND-037 Windows watcher is missing required advisory behaviour: $required"
    }
}

foreach ($path in $evidencePaths) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "FND-037 evidence is missing: $path" }
    $content = [System.IO.File]::ReadAllText($path)
    foreach ($token in @('C:\Users\', 'ghp_', 'github_pat_', 'Authorization:', 'Bearer ', 'Password=')) {
        if ($content.Contains($token, [StringComparison]::OrdinalIgnoreCase)) {
            throw "FND-037 evidence contains prohibited material: $token"
        }
    }
    if ($content -match '[A-Za-z]:[\\/]' -or $content -match '\\\\[^\\/\r\n]+[\\/]') {
        throw "FND-037 evidence contains an absolute or UNC path: $path"
    }
}

$state = [System.IO.File]::ReadAllText($evidencePaths[0]) | ConvertFrom-Json
$loss = [System.IO.File]::ReadAllText($evidencePaths[1]) | ConvertFrom-Json
$storm = [System.IO.File]::ReadAllText($evidencePaths[2]) | ConvertFrom-Json
if ($state.result -ne 'Passed' -or $state.watcherAuthority -ne 'HintOnly' -or `
    $state.partialScanPromotesGeneration -ne $false -or $state.currentGenerationRemainsComplete -ne $true -or `
    $state.concurrentHintPreventsFreshnessClaim -ne $true -or `
    $state.failedAttemptRearmsFullScan -ne $true -or `
    $loss.result -ne 'Passed' -or $loss.overflowForcesFullScan -ne $true -or `
    $loss.missedEventRepaired -ne $true -or $loss.restartRequiresFreshScan -ne $true -or `
    $storm.result -ne 'Passed' -or $storm.inputHintCount -ne 10000 -or `
    $storm.maximumPendingHintCount -ne 8 -or $storm.observedPeakPendingHintCount -gt 8 -or `
    $storm.overflowCollapsesToFullScan -ne $true) {
    throw 'FND-037 evidence is incomplete.'
}

Write-Host ''
Write-Host 'FND-037 Workspace change reconciliation verification passed.' -ForegroundColor Green
