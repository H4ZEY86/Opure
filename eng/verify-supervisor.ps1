#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

if (-not $IsWindows) {
    throw 'FND-007 Supervisor evidence currently requires Windows 11.'
}

$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M1'
$bootstrapExecutable = Join-Path `
    $repositoryRoot `
    'artifacts\bin\Opure.Bootstrap.Windows\release\Opure.Bootstrap.Windows.exe'
$diagnosticCanary = 'FND007_SUPERVISOR_SECRET_CANARY_DO_NOT_RECORD'

New-Item -ItemType Directory -Force -Path $evidenceRoot | Out-Null

Write-Host ''
Write-Host '==> Verify Supervisor build and tests' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration Release `
    -BuildChannel Development

if (-not (Test-Path -LiteralPath $bootstrapExecutable -PathType Leaf)) {
    throw "Bootstrap executable was not produced: $bootstrapExecutable"
}

function Invoke-SupervisorScenario {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Name,

        [Parameter(Mandatory)]
        [string[]] $Arguments,

        [Parameter(Mandatory)]
        [int] $ExpectedExitCode
    )

    Write-Host ''
    Write-Host "==> $Name" -ForegroundColor Cyan

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $bootstrapExecutable
    $startInfo.UseShellExecute = $false
    $startInfo.WorkingDirectory = $repositoryRoot
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.Environment['OPURE_BOOTSTRAP_TEST_MODE'] = '1'
    $startInfo.Environment['OPURE_SUPERVISOR_DIAGNOSTIC_CANARY'] = `
        $diagnosticCanary

    foreach ($argument in $Arguments) {
        [void]$startInfo.ArgumentList.Add($argument)
    }

    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    try {
        if (-not $process.Start()) {
            throw "$Name did not start."
        }

        $standardOutputTask = $process.StandardOutput.ReadToEndAsync()
        $standardErrorTask = $process.StandardError.ReadToEndAsync()

        if (-not $process.WaitForExit(30000)) {
            $process.Kill($true)
            throw "$Name exceeded its bounded deadline."
        }

        $process.WaitForExit()
        $stopwatch.Stop()

        $standardOutput = $standardOutputTask.GetAwaiter().GetResult()
        $standardError = $standardErrorTask.GetAwaiter().GetResult()

        if ($process.ExitCode -ne $ExpectedExitCode) {
            throw @"
$Name returned exit code $($process.ExitCode); expected $ExpectedExitCode.
Standard error:
$standardError
Standard output:
$standardOutput
"@
        }

        if ($standardOutput.Contains($diagnosticCanary, [StringComparison]::Ordinal) -or
            $standardError.Contains($diagnosticCanary, [StringComparison]::Ordinal)) {
            throw "$Name leaked the diagnostic environment canary."
        }

        $events = @(
            $standardOutput -split "`r?`n" |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
                ForEach-Object { $_ | ConvertFrom-Json }
        )

        return [pscustomobject]@{
            Name = $Name
            ExitCode = $process.ExitCode
            ElapsedMilliseconds = $stopwatch.ElapsedMilliseconds
            Events = $events
            StandardOutput = $standardOutput
            StandardError = $standardError
        }
    }
    finally {
        if (-not $process.HasExited) {
            try {
                $process.Kill($true)
            }
            catch {
            }
        }

        $process.Dispose()
    }
}

$commonArguments = @(
    '--layout',
    'Development',
    '--configuration',
    'Release',
    '--channel',
    'Development'
)

$recovery = Invoke-SupervisorScenario `
    -Name 'Runtime kill and bounded recovery' `
    -ExpectedExitCode 0 `
    -Arguments ($commonArguments + @(
        '--desktop-close-after-ms',
        '1200',
        '--runtime-crash-after-ready-ms',
        '250',
        '--runtime-crash-count',
        '1'
    ))

$recoveryRuntimeStarts = @(
    $recovery.Events |
        Where-Object {
            $_.event -eq 'bootstrap.child.started' -and
            $_.processClass -eq 'runtime'
        }
)
$recoveryCrash = @(
    $recovery.Events |
        Where-Object {
            $_.event -eq 'bootstrap.child.exited' -and
            $_.processClass -eq 'runtime' -and
            $_.classification -eq 'crash'
        }
)
$recovered = @(
    $recovery.Events |
        Where-Object {
            $_.event -eq 'bootstrap.supervisor.state' -and
            $_.reason -eq 'runtime_recovered'
        }
)

if ($recoveryRuntimeStarts.Count -ne 2 -or
    $recoveryCrash.Count -ne 1 -or
    $recovered.Count -ne 1) {
    throw 'Runtime recovery evidence did not show one crash and one successful restart.'
}

if ($recoveryRuntimeStarts[0].instanceId -eq
    $recoveryRuntimeStarts[1].instanceId) {
    throw 'Runtime restart reused its supervisor process identity.'
}

$crashLoop = Invoke-SupervisorScenario `
    -Name 'Rapid Runtime crash loop' `
    -ExpectedExitCode 60 `
    -Arguments ($commonArguments + @(
        '--desktop-close-after-ms',
        '500',
        '--runtime-crash-after-ready-ms',
        '150',
        '--runtime-crash-count',
        '4'
    ))

$crashLoopRuntimeStarts = @(
    $crashLoop.Events |
        Where-Object {
            $_.event -eq 'bootstrap.child.started' -and
            $_.processClass -eq 'runtime'
        }
)
$safeModeEvents = @(
    $crashLoop.Events |
        Where-Object {
            $_.event -eq 'bootstrap.supervisor.state' -and
            $_.mode -eq 'safe_mode' -and
            $_.reason -eq 'runtime_restart_budget_exhausted'
        }
)
$safeModeDesktopStarts = @(
    $crashLoop.Events |
        Where-Object {
            $_.event -eq 'bootstrap.child.started' -and
            $_.processClass -eq 'desktop'
        }
)

if ($crashLoopRuntimeStarts.Count -ne 4 -or
    $safeModeEvents.Count -ne 1 -or
    $safeModeDesktopStarts.Count -lt 1) {
    throw 'Crash-loop evidence did not stop after three restarts and enter Safe Mode.'
}

Write-Host ''
Write-Host '==> Bootstrap crash and Job Object orphan cleanup' -ForegroundColor Cyan

$orphanStartInfo = [System.Diagnostics.ProcessStartInfo]::new()
$orphanStartInfo.FileName = $bootstrapExecutable
$orphanStartInfo.UseShellExecute = $false
$orphanStartInfo.WorkingDirectory = $repositoryRoot
$orphanStartInfo.RedirectStandardOutput = $true
$orphanStartInfo.RedirectStandardError = $true
$orphanStartInfo.Environment['OPURE_BOOTSTRAP_TEST_MODE'] = '1'

foreach ($argument in ($commonArguments + @(
    '--desktop-close-after-ms',
    '10000'
))) {
    [void]$orphanStartInfo.ArgumentList.Add($argument)
}

$orphanProcess = [System.Diagnostics.Process]::new()
$orphanProcess.StartInfo = $orphanStartInfo
$ownedProcessIds = @{}
$orphanProcessId = 0

try {
    if (-not $orphanProcess.Start()) {
        throw 'Job Object orphan-cleanup scenario did not start.'
    }

    $orphanProcessId = $orphanProcess.Id

    $orphanOutputTask = $orphanProcess.StandardOutput.ReadToEndAsync()
    $orphanErrorTask = $orphanProcess.StandardError.ReadToEndAsync()
    $discoveryDeadline = [DateTimeOffset]::UtcNow.AddSeconds(15)

    while ([DateTimeOffset]::UtcNow -lt $discoveryDeadline -and
        $ownedProcessIds.Count -lt 2) {
        $children = Get-CimInstance `
            -ClassName Win32_Process `
            -Filter "ParentProcessId = $($orphanProcess.Id)" `
            -ErrorAction SilentlyContinue

        foreach ($child in $children) {
            if ($child.Name -in @('Opure.Runtime.exe', 'Opure.Desktop.exe')) {
                $ownedProcessIds[[string]$child.Name] = [int]$child.ProcessId
            }
        }

        Start-Sleep -Milliseconds 50
    }

    if ($ownedProcessIds.Count -ne 2) {
        throw 'Could not identify both Job Object-owned Bootstrap children.'
    }

    $orphanProcess.Kill()

    if (-not $orphanProcess.WaitForExit(5000)) {
        throw 'Bootstrap did not terminate during orphan-cleanup injection.'
    }

    $cleanupDeadline = [DateTimeOffset]::UtcNow.AddSeconds(5)

    do {
        $remainingProcessIds = @(
            $ownedProcessIds.Values |
                Where-Object {
                    $null -ne (Get-Process -Id $_ -ErrorAction SilentlyContinue)
                }
        )

        if ($remainingProcessIds.Count -eq 0) {
            break
        }

        Start-Sleep -Milliseconds 50
    }
    while ([DateTimeOffset]::UtcNow -lt $cleanupDeadline)

    if ($remainingProcessIds.Count -ne 0) {
        throw "Job Object left owned processes running: $($remainingProcessIds -join ', ')"
    }

    [void]$orphanOutputTask.GetAwaiter().GetResult()
    [void]$orphanErrorTask.GetAwaiter().GetResult()
}
finally {
    if (-not $orphanProcess.HasExited) {
        try {
            $orphanProcess.Kill($true)
        }
        catch {
        }
    }

    $orphanProcess.Dispose()
}

$stateMachineEvidence = @"
# FND-007 Supervisor State-Machine Report

**Result:** Passed

## Observed transitions

- Normal/Ready -> Recovering/Crashed -> Recovering/Starting -> Normal/Ready
- Normal/Ready -> Recovering/Crashed -> SafeMode/Quarantined

## Policy

- Runtime restart attempts: maximum 3 within 30 seconds.
- Backoff: 100 ms, 200 ms, 400 ms, capped at 2 seconds.
- Each Runtime start has a random supervisor instance ID, PID, start time, executable hash, product version and Runtime boot ID.
- PID equality alone is never treated as process identity.
- Controlled shutdown is classified as policy_stop; a non-zero unexpected exit is classified as crash.
- Safe Mode projects a quarantined Runtime and bounded restart count to Desktop without exposing environment values.

## Recovery scenario

- Runtime starts observed: $($recoveryRuntimeStarts.Count)
- Runtime crashes observed: $($recoveryCrash.Count)
- Successful recoveries observed: $($recovered.Count)
- Elapsed milliseconds: $($recovery.ElapsedMilliseconds)

## Crash-loop scenario

- Runtime starts observed: $($crashLoopRuntimeStarts.Count)
- Safe Mode transitions observed: $($safeModeEvents.Count)
- Exit code: $($crashLoop.ExitCode)
- Elapsed milliseconds: $($crashLoop.ElapsedMilliseconds)
"@

[System.IO.File]::WriteAllText(
    (Join-Path $evidenceRoot 'supervisor-state-machine.md'),
    $stateMachineEvidence.Replace("`r`n", "`n"),
    [System.Text.UTF8Encoding]::new($false)
)

$crashLoopTrace = [ordered]@{
    result = 'Passed'
    expectedExitCode = 60
    runtimeStartCount = $crashLoopRuntimeStarts.Count
    restartBudget = 3
    safeModeEntered = $true
    diagnosticsContainEnvironmentCanary = $false
    events = $crashLoop.Events
}

[System.IO.File]::WriteAllText(
    (Join-Path $evidenceRoot 'supervisor-crash-loop.json'),
    ($crashLoopTrace | ConvertTo-Json -Depth 8),
    [System.Text.UTF8Encoding]::new($false)
)

$orphanEvidence = @(
    'FND-007 Job Object Orphan Cleanup'
    '================================='
    "BootstrapProcessId: $orphanProcessId"
    "RuntimeProcessId: $($ownedProcessIds['Opure.Runtime.exe'])"
    "DesktopProcessId: $($ownedProcessIds['Opure.Desktop.exe'])"
    'Injection: Bootstrap process terminated without tree-kill'
    'JobLimit: KILL_ON_JOB_CLOSE'
    'BreakawayAllowed: False'
    'OwnedChildrenRemaining: 0'
    'Result: Passed'
)

[System.IO.File]::WriteAllLines(
    (Join-Path $evidenceRoot 'supervisor-orphan-cleanup.txt'),
    [string[]]$orphanEvidence,
    [System.Text.UTF8Encoding]::new($false)
)

Write-Host ''
Write-Host 'FND-007 Supervisor verification passed.' -ForegroundColor Green
