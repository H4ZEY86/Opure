#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

if (-not $IsWindows) {
    throw 'FND-009 named-pipe process evidence requires Windows.'
}

foreach ($command in @('Get-NetTCPConnection', 'Get-NetUDPEndpoint')) {
    if (-not (Get-Command $command -ErrorAction SilentlyContinue)) {
        throw "Required Windows network inspection command is unavailable: $command"
    }
}

$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M2'
$runtimeAssembly = Join-Path `
    $repositoryRoot `
    'artifacts\bin\Opure.Runtime\release\Opure.Runtime.dll'
$transportTests = Join-Path `
    $repositoryRoot `
    'tests\Ipc\Opure.Ipc.NamedPipes.Windows.Tests\Opure.Ipc.NamedPipes.Windows.Tests.csproj'
$latencyPath = Join-Path $evidenceRoot 'runtime-health-transport-latency.json'

New-Item -ItemType Directory -Force -Path $evidenceRoot | Out-Null

Write-Host ''
Write-Host '==> Verify FND-009 build and tests' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration Release `
    -BuildChannel Development

Write-Host ''
Write-Host '==> Capture named-pipe latency baseline' -ForegroundColor Cyan

$previousLatencyPath = $env:OPURE_IPC_LATENCY_EVIDENCE_PATH

try {
    $env:OPURE_IPC_LATENCY_EVIDENCE_PATH = $latencyPath
    & dotnet test $transportTests `
        --configuration Release `
        --no-build `
        --no-restore `
        --filter-method `
        'Opure.Ipc.NamedPipes.Windows.Tests.NamedPipeRuntimeHealthTransportTests.Unary_latency_baseline_remains_bounded'
}
finally {
    if ($null -eq $previousLatencyPath) {
        Remove-Item Env:OPURE_IPC_LATENCY_EVIDENCE_PATH -ErrorAction SilentlyContinue
    }
    else {
        $env:OPURE_IPC_LATENCY_EVIDENCE_PATH = $previousLatencyPath
    }
}

if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $latencyPath)) {
    throw 'FND-009 latency evidence was not produced.'
}

Write-Host ''
Write-Host '==> Inspect live Runtime network listeners' -ForegroundColor Cyan

$startInfo = [System.Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = 'dotnet'
$startInfo.UseShellExecute = $false
$startInfo.RedirectStandardOutput = $true
$startInfo.RedirectStandardError = $true
$startInfo.WorkingDirectory = $repositoryRoot
$startInfo.Environment['OPURE_RUNTIME_TEST_MODE'] = '1'
[void]$startInfo.ArgumentList.Add($runtimeAssembly)
[void]$startInfo.ArgumentList.Add('--shutdown-after-ms')
[void]$startInfo.ArgumentList.Add('2500')

$process = [System.Diagnostics.Process]::new()
$process.StartInfo = $startInfo

if (-not $process.Start()) {
    throw 'Runtime transport evidence process did not start.'
}

$standardOutputTask = $process.StandardOutput.ReadToEndAsync()
$standardErrorTask = $process.StandardError.ReadToEndAsync()

try {
    Start-Sleep -Milliseconds 750

    if ($process.HasExited) {
        throw 'Runtime exited before the named-pipe listener inspection window.'
    }

    $tcpEndpoints = @(
        Get-NetTCPConnection -OwningProcess $process.Id -ErrorAction SilentlyContinue
    )
    $udpEndpoints = @(
        Get-NetUDPEndpoint -OwningProcess $process.Id -ErrorAction SilentlyContinue
    )

    if ($tcpEndpoints.Count -ne 0 -or $udpEndpoints.Count -ne 0) {
        throw "Runtime owned network endpoints: TCP=$($tcpEndpoints.Count), UDP=$($udpEndpoints.Count)."
    }

    if (-not $process.WaitForExit(10000)) {
        $process.Kill($true)
        throw 'Runtime transport evidence process exceeded its outer timeout.'
    }

    $standardOutput = $standardOutputTask.GetAwaiter().GetResult()
    $standardError = $standardErrorTask.GetAwaiter().GetResult()

    if ($process.ExitCode -ne 0 -or -not [string]::IsNullOrWhiteSpace($standardError)) {
        throw "Runtime transport probe failed with exit code $($process.ExitCode)."
    }

    $events = @(
        $standardOutput -split "\r?\n" |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            ForEach-Object { $_ | ConvertFrom-Json }
    )
    $ready = $events |
        Where-Object { $_.event -eq 'runtime.lifecycle' -and $_.state -eq 'ready' } |
        Select-Object -First 1

    if ($null -eq $ready -or
        [string]$ready.bootId -notmatch '^[0-9a-f]{32}$' -or
        [string]$ready.runtimeHealthPipe -notmatch '^opure-development-[0-9a-f]{32}$') {
        throw 'Runtime did not publish a valid boot-scoped named-pipe descriptor.'
    }

    $networkEvidence = [ordered]@{
        result = 'Passed'
        captureMethod = 'Get-NetTCPConnection and Get-NetUDPEndpoint by owning process'
        runtimeProcessId = $process.Id
        tcpListenerCount = $tcpEndpoints.Count
        udpEndpointCount = $udpEndpoints.Count
        namedPipePublished = $true
        channel = 'development'
        endpointIdentityFormat = 'opure-development-<32 lowercase hexadecimal random characters>'
        endpointContainsCredential = $false
        payloadLogging = $false
    }

    [System.IO.File]::WriteAllText(
        (Join-Path $evidenceRoot 'runtime-health-network-listeners.json'),
        ($networkEvidence | ConvertTo-Json -Depth 4),
        [System.Text.UTF8Encoding]::new($false)
    )
}
finally {
    if (-not $process.HasExited) {
        $process.Kill($true)
        $process.WaitForExit()
    }

    $process.Dispose()
}

$report = @'
# FND-009 Named-Pipe Transport Prototype

**Result:** Passed

## Verified

- Desktop Gateway receives the versioned Runtime Health projection through gRPC over the exact Windows named pipe.
- Runtime binds HTTP/2 only through Kestrel's named-pipe transport and owns no TCP or UDP listener.
- Endpoint names are random, channel-qualified and paired with one Runtime boot identity; they are identifiers, not credentials.
- Missing endpoints return `HEALTH_TRANSPORT_UNAVAILABLE` under a bounded connection timeout.
- Live-call deadline expiry returns `HEALTH_TRANSPORT_DEADLINE_EXCEEDED`.
- Caller cancellation closes the call within the one-second test bound.
- Oversized requests are rejected before transport and response limits are configured on client and server.
- Runtime restart rotates both boot and endpoint identity; a new Desktop client reconnects through the latest descriptor.
- Transport implementation remains behind `Opure.Ipc.Abstractions` and the Windows adapter.
- Connection evidence contains no request or response payloads.

## Deliberately deferred

Named-pipe ACLs, mutual session proof, replay protection and stale-session authentication belong to FND-010.

## Evidence files

- `runtime-health-transport-latency.json`
- `runtime-health-network-listeners.json`
- `runtime-health-transport-report.md`
'@

[System.IO.File]::WriteAllText(
    (Join-Path $evidenceRoot 'runtime-health-transport-report.md'),
    $report.Replace("`r`n", "`n"),
    [System.Text.UTF8Encoding]::new($false)
)

Write-Host ''
Write-Host 'FND-009 named-pipe transport verification passed.' -ForegroundColor Green
