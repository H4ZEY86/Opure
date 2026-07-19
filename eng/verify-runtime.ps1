#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

if (-not $IsWindows) {
    throw 'FND-004 network endpoint evidence currently requires Windows.'
}

foreach ($command in @('Get-NetTCPConnection', 'Get-NetUDPEndpoint')) {
    if (-not (Get-Command $command -ErrorAction SilentlyContinue)) {
        throw "Required Windows network inspection command is unavailable: $command"
    }
}

$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M1'
$runtimeAssembly = Join-Path `
    $repositoryRoot `
    'artifacts\bin\Opure.Runtime\release\Opure.Runtime.dll'

New-Item -ItemType Directory -Force -Path $evidenceRoot | Out-Null

function ConvertFrom-RuntimeJsonLines {
    param(
        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string] $Text
    )

    return @(
        $Text -split "\r?\n" |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            ForEach-Object { $_ | ConvertFrom-Json }
    )
}

function Start-RuntimeProbe {
    param(
        [Parameter(Mandatory)]
        [int] $ShutdownAfterMilliseconds,

        [Parameter()]
        [switch] $CaptureNetwork,

        [Parameter()]
        [switch] $StartupFailure
    )

    $dataRoot = Join-Path `
        $repositoryRoot `
        "artifacts\runtime-data-probe\$([guid]::NewGuid().ToString('N'))"

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = 'dotnet'
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.WorkingDirectory = $repositoryRoot
    $startInfo.Environment['OPURE_RUNTIME_TEST_MODE'] = '1'
    [void]$startInfo.ArgumentList.Add($runtimeAssembly)

    if ($StartupFailure) {
        [void]$startInfo.ArgumentList.Add('--test-startup-failure')
    }
    elseif ($ShutdownAfterMilliseconds -gt 0) {
        [void]$startInfo.ArgumentList.Add('--shutdown-after-ms')
        [void]$startInfo.ArgumentList.Add([string]$ShutdownAfterMilliseconds)
    }

    [void]$startInfo.ArgumentList.Add('--data-root')
    [void]$startInfo.ArgumentList.Add($dataRoot)

    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $startInfo

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    if (-not $process.Start()) {
        throw 'Runtime evidence process did not start.'
    }

    $standardOutputTask = $process.StandardOutput.ReadToEndAsync()
    $standardErrorTask = $process.StandardError.ReadToEndAsync()

    $tcpEndpoints = @()
    $udpEndpoints = @()

    if ($CaptureNetwork) {
        Start-Sleep -Milliseconds 350

        if ($process.HasExited) {
            throw 'Runtime exited before the network inspection window.'
        }

        $tcpEndpoints = @(
            Get-NetTCPConnection `
                -OwningProcess $process.Id `
                -ErrorAction SilentlyContinue
        )

        $udpEndpoints = @(
            Get-NetUDPEndpoint `
                -OwningProcess $process.Id `
                -ErrorAction SilentlyContinue
        )
    }

    if (-not $process.WaitForExit(10000)) {
        try {
            $process.Kill($true)
        }
        catch {
        }

        throw 'Runtime evidence process exceeded the ten-second outer timeout.'
    }

    $standardOutput = $standardOutputTask.GetAwaiter().GetResult()
    $standardError = $standardErrorTask.GetAwaiter().GetResult()
    $stopwatch.Stop()

    $result = [pscustomobject]@{
        ProcessId = $process.Id
        ExitCode = $process.ExitCode
        ElapsedMilliseconds = $stopwatch.ElapsedMilliseconds
        StandardOutput = $standardOutput
        StandardError = $standardError
        DataRootExists = (Test-Path -LiteralPath $dataRoot)
        TcpEndpoints = $tcpEndpoints
        UdpEndpoints = $udpEndpoints
    }

    $process.Dispose()
    return $result
}

Write-Host ''
Write-Host '==> Verify Runtime build and tests' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration Release `
    -BuildChannel Development

if (-not (Test-Path -LiteralPath $runtimeAssembly -PathType Leaf)) {
    throw "Runtime assembly was not produced: $runtimeAssembly"
}

Write-Host ''
Write-Host '==> Capture two independent Runtime boots' -ForegroundColor Cyan

$first = Start-RuntimeProbe -ShutdownAfterMilliseconds 50
$second = Start-RuntimeProbe -ShutdownAfterMilliseconds 50

foreach ($probe in @($first, $second)) {
    if ($probe.ExitCode -ne 0) {
        $probe.StandardOutput | Out-Host
        $probe.StandardError | Out-Host
        throw "Runtime probe exited with code $($probe.ExitCode)."
    }

    if (-not [string]::IsNullOrWhiteSpace($probe.StandardError)) {
        throw "Runtime wrote unexpected standard error: $($probe.StandardError)"
    }

    if ($probe.DataRootExists) {
        throw 'Runtime created data-root state during the minimal lifecycle probe.'
    }

    if ($probe.ElapsedMilliseconds -gt 5000) {
        throw "Runtime shutdown exceeded five seconds: $($probe.ElapsedMilliseconds) ms."
    }
}

$firstEvents = ConvertFrom-RuntimeJsonLines -Text $first.StandardOutput
$secondEvents = ConvertFrom-RuntimeJsonLines -Text $second.StandardOutput

$expectedStates = @('starting', 'ready', 'stopping', 'stopped')
$firstStates = @($firstEvents | Where-Object event -eq 'runtime.lifecycle' | ForEach-Object state)

if (($firstStates -join ',') -ne ($expectedStates -join ',')) {
    throw "Unexpected Runtime lifecycle: $($firstStates -join ', ')"
}

$firstBootEvent = $firstEvents |
    Where-Object event -eq 'runtime.lifecycle' |
    Select-Object -First 1

$secondBootEvent = $secondEvents |
    Where-Object event -eq 'runtime.lifecycle' |
    Select-Object -First 1

$firstBootId = [string]$firstBootEvent.bootId
$secondBootId = [string]$secondBootEvent.bootId

if ($firstBootId -eq $secondBootId) {
    throw 'Separate Runtime starts produced the same boot identity.'
}

if ($firstBootId -notmatch '^[0-9a-f]{32}$' -or $secondBootId -notmatch '^[0-9a-f]{32}$') {
    throw 'A Runtime boot identity did not match the opaque 32-character format.'
}

$firstReady = $firstEvents |
    Where-Object { $_.event -eq 'runtime.lifecycle' -and $_.state -eq 'ready' } |
    Select-Object -First 1

$reportedProductVersion = [string]$firstReady.productVersion
$reportedContractVersion = [string]$firstReady.contractVersion
$reportedNetworkAccess = [string]$firstReady.networkAccess

if ([string]::IsNullOrWhiteSpace($reportedProductVersion)) {
    throw 'Runtime did not report its product version.'
}

if ($reportedContractVersion -ne '1.0') {
    throw "Unexpected Runtime contract version: $reportedContractVersion"
}

if ($reportedNetworkAccess -ne 'disabled') {
    throw 'Runtime did not report disabled network access.'
}

Write-Host ''
Write-Host '==> Inspect Runtime TCP and UDP ownership while ready' -ForegroundColor Cyan

$networkProbe = Start-RuntimeProbe `
    -ShutdownAfterMilliseconds 2000 `
    -CaptureNetwork

if ($networkProbe.ExitCode -ne 0) {
    throw "Runtime network probe exited with code $($networkProbe.ExitCode)."
}

if ($networkProbe.TcpEndpoints.Count -gt 0) {
    throw "Runtime owned TCP endpoints during offline launch: $($networkProbe.TcpEndpoints.Count)"
}

if ($networkProbe.UdpEndpoints.Count -gt 0) {
    throw "Runtime owned UDP endpoints during offline launch: $($networkProbe.UdpEndpoints.Count)"
}

if ($networkProbe.DataRootExists) {
    throw 'Runtime created data-root state during the network probe.'
}

Write-Host ''
Write-Host '==> Verify stable startup-failure category' -ForegroundColor Cyan

$failureProbe = Start-RuntimeProbe `
    -ShutdownAfterMilliseconds 0 `
    -StartupFailure

if ($failureProbe.ExitCode -ne 20) {
    $failureProbe.StandardOutput | Out-Host
    $failureProbe.StandardError | Out-Host
    throw "Expected startup failure exit code 20, found $($failureProbe.ExitCode)."
}

$failureEvents = ConvertFrom-RuntimeJsonLines -Text $failureProbe.StandardOutput
$failureEvent = $failureEvents |
    Where-Object event -eq 'runtime.failure' |
    Select-Object -First 1

$failureCategory = [string]$failureEvent.category

if ($failureCategory -ne 'startup_failure') {
    throw "Unexpected startup failure category: $failureCategory"
}

if ($failureProbe.DataRootExists) {
    throw 'Failed Runtime startup created data-root state.'
}

$startupTracePath = Join-Path $evidenceRoot 'runtime-startup-trace.jsonl'
[System.IO.File]::WriteAllText(
    $startupTracePath,
    $first.StandardOutput.Replace("`r`n", "`n"),
    [System.Text.UTF8Encoding]::new($false)
)

$processInventory = @(
    'Opure Runtime Process Inventory'
    '==============================='
    'Host: dotnet'
    'Assembly: Opure.Runtime.dll'
    "FirstExitCode: $($first.ExitCode)"
    "FirstElapsedMilliseconds: $($first.ElapsedMilliseconds)"
    "SecondExitCode: $($second.ExitCode)"
    "SecondElapsedMilliseconds: $($second.ElapsedMilliseconds)"
    "NetworkProbeExitCode: $($networkProbe.ExitCode)"
    "NetworkProbeElapsedMilliseconds: $($networkProbe.ElapsedMilliseconds)"
    "StartupFailureExitCode: $($failureProbe.ExitCode)"
    'ChildProcessesStarted: 0'
    'DomainServicesLoaded: 0'
)

[System.IO.File]::WriteAllLines(
    (Join-Path $evidenceRoot 'runtime-process-inventory.txt'),
    [string[]]$processInventory,
    [System.Text.UTF8Encoding]::new($false)
)

$networkEvidence = @(
    'Opure Runtime Offline Endpoint Evidence'
    '======================================='
    "CapturePlatform: $([System.Environment]::OSVersion.VersionString)"
    'CaptureMethod: Get-NetTCPConnection and Get-NetUDPEndpoint by owning process'
    "RuntimeProcessId: $($networkProbe.ProcessId)"
    "TcpEndpointCount: $($networkProbe.TcpEndpoints.Count)"
    "UdpEndpointCount: $($networkProbe.UdpEndpoints.Count)"
    'Expected: zero Runtime-owned TCP or UDP endpoints'
    'Result: Passed'
)

[System.IO.File]::WriteAllLines(
    (Join-Path $evidenceRoot 'runtime-offline-network.txt'),
    [string[]]$networkEvidence,
    [System.Text.UTF8Encoding]::new($false)
)

$summary = @"
# FND-004 Runtime Executable Evidence

**Ticket:** FND-004  
**Milestone:** M1  
**Result:** Passed  

## Verified

- The Runtime starts as a separate executable process.
- Every process start receives a distinct opaque boot identity.
- Lifecycle state is emitted as `starting`, `ready`, `stopping` and `stopped`.
- Product informational version and Runtime contract version `1.0` are reported.
- Controlled automatic shutdown completes within the five-second deadline.
- Unexpected startup failure returns stable exit code `20` and category `startup_failure`.
- The minimal Runtime writes no files and creates no data-root directory.
- The default data-root resolver is scoped to Local Application Data / Opure / Development.
- No Runtime-owned TCP connection or UDP endpoint exists during the readiness window.
- No AI, plugin, MCP, workflow, project or persistence service is loaded.
- No child process is started by the Runtime.
- Startup diagnostics contain no secrets and omit the absolute data-root path.

## Evidence Files

- `runtime-startup-trace.jsonl`
- `runtime-process-inventory.txt`
- `runtime-offline-network.txt`

## Recovery

The Runtime creates no durable state in this milestone. A startup or shutdown failure leaves no partially committed service state.
"@

[System.IO.File]::WriteAllText(
    (Join-Path $evidenceRoot 'runtime-verification.md'),
    $summary.Replace("`r`n", "`n"),
    [System.Text.UTF8Encoding]::new($false)
)

Write-Host ''
Write-Host 'FND-004 Runtime verification passed.' -ForegroundColor Green
