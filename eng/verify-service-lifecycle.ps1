#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M1'
$runtimeTests = Join-Path `
    $repositoryRoot `
    'tests\Runtime\Opure.Runtime.Tests\Opure.Runtime.Tests.csproj'
$reportPath = Join-Path `
    $evidenceRoot `
    'service-lifecycle-transition-report.json'
$diagramPath = Join-Path `
    $evidenceRoot `
    'service-lifecycle-state-machine.md'

New-Item -ItemType Directory -Force -Path $evidenceRoot | Out-Null

Write-Host ''
Write-Host '==> Verify FND-012 build and tests' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration Release `
    -BuildChannel Development

Write-Host ''
Write-Host '==> Exercise Service Lifecycle state machine' -ForegroundColor Cyan

$previousEvidencePath = $env:OPURE_SERVICE_LIFECYCLE_EVIDENCE_PATH
$testExitCode = 1

try {
    $env:OPURE_SERVICE_LIFECYCLE_EVIDENCE_PATH = $reportPath
    & dotnet test $runtimeTests `
        --configuration Release `
        --no-build `
        --no-restore `
        --filter-class `
        'Opure.Runtime.Tests.RuntimeServiceLifecycleCoordinatorTests' `
        --timeout 60s
    $testExitCode = $LASTEXITCODE
}
finally {
    if ($null -eq $previousEvidencePath) {
        Remove-Item Env:OPURE_SERVICE_LIFECYCLE_EVIDENCE_PATH `
            -ErrorAction SilentlyContinue
    }
    else {
        $env:OPURE_SERVICE_LIFECYCLE_EVIDENCE_PATH = $previousEvidencePath
    }
}

if ($testExitCode -ne 0 -or
    -not (Test-Path -LiteralPath $reportPath -PathType Leaf)) {
    throw 'FND-012 transition test evidence was not produced.'
}

if (-not (Test-Path -LiteralPath $diagramPath -PathType Leaf)) {
    throw 'FND-012 state-machine diagram is missing.'
}

$reportText = [System.IO.File]::ReadAllText($reportPath)
$report = $reportText | ConvertFrom-Json

if ($report.schema -ne 'opure.service-lifecycle-transition-report/1' -or
    $report.result -ne 'Passed' -or
    $report.transitionCount -ne 8 -or
    @($report.transitions).Count -ne 8) {
    throw 'FND-012 transition report does not match the deterministic scenario.'
}

$expectedSequence = 1..8
$actualSequence = @($report.transitions | ForEach-Object { [int]$_.sequence })

if (($actualSequence -join ',') -ne ($expectedSequence -join ',')) {
    throw 'FND-012 lifecycle event sequence is not stable and contiguous.'
}

$expectedStates = @(
    'Starting',
    'Ready',
    'Starting',
    'Ready',
    'Stopping',
    'Stopped',
    'Stopping',
    'Stopped'
)
$actualStates = @($report.transitions | ForEach-Object state)

if (($actualStates -join ',') -ne ($expectedStates -join ',')) {
    throw 'FND-012 lifecycle event order is not the reviewed projection.'
}

foreach ($prohibitedToken in @(
    'exception',
    'stackTrace',
    'sessionSecret',
    'accessToken',
    'C:\\',
    '\\Users\\'
)) {
    if ($reportText.Contains(
            $prohibitedToken,
            [StringComparison]::OrdinalIgnoreCase)) {
        throw "FND-012 evidence contains prohibited diagnostic material: $prohibitedToken"
    }
}

$diagram = [System.IO.File]::ReadAllText($diagramPath)

foreach ($requiredState in @(
    'Registered',
    'Starting',
    'Ready',
    'Degraded',
    'Stopping',
    'Stopped',
    'Failed',
    'Restarting',
    'Quarantined',
    'Disabled'
)) {
    if (-not $diagram.Contains($requiredState, [StringComparison]::Ordinal)) {
        throw "FND-012 diagram omits required state: $requiredState"
    }
}

Write-Host ''
Write-Host 'FND-012 Service Lifecycle verification passed.' -ForegroundColor Green
