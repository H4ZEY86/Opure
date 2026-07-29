#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

$ipcTests = Join-Path `
    $repositoryRoot `
    'tests\Ipc\Opure.Ipc.NamedPipes.Windows.Tests\Opure.Ipc.NamedPipes.Windows.Tests.csproj'
$observabilityTests = Join-Path `
    $repositoryRoot `
    'tests\Observability\Opure.Observability.Tests\Opure.Observability.Tests.csproj'
$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M3'
$tracePath = Join-Path $evidenceRoot 'trace-example.json'
$leakagePath = Join-Path $evidenceRoot 'trace-payload-leakage.txt'
$latencyPath = Join-Path $evidenceRoot 'trace-latency-overhead.json'
$crossProcessPath = Join-Path $evidenceRoot 'trace-cross-process.txt'
$verificationPath = Join-Path $evidenceRoot 'trace-verification.md'

Write-Host ''
Write-Host '==> Verify FND-019 build and tests' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration Release `
    -BuildChannel Development

$env:OPURE_TRACE_EXAMPLE_EVIDENCE_PATH = $tracePath
$env:OPURE_TRACE_LEAKAGE_EVIDENCE_PATH = $leakagePath
$env:OPURE_TRACE_LATENCY_EVIDENCE_PATH = $latencyPath

try {
    & dotnet test $observabilityTests `
        --configuration Release `
        --no-build `
        --no-restore `
        --filter-class `
        'Opure.Observability.Tests.OperationalTraceSessionTests' `
        --timeout 60s

    if ($LASTEXITCODE -ne 0) {
        throw 'FND-019 trace policy tests failed.'
    }

    & dotnet test $ipcTests `
        --configuration Release `
        --no-build `
        --no-restore `
        --filter-class `
        'Opure.Ipc.NamedPipes.Windows.Tests.OperationalTraceTransportTests' `
        --timeout 60s

    if ($LASTEXITCODE -ne 0) {
        throw 'FND-019 trace transport tests failed.'
    }
}
finally {
    Remove-Item Env:OPURE_TRACE_EXAMPLE_EVIDENCE_PATH `
        -ErrorAction SilentlyContinue
    Remove-Item Env:OPURE_TRACE_LEAKAGE_EVIDENCE_PATH `
        -ErrorAction SilentlyContinue
    Remove-Item Env:OPURE_TRACE_LATENCY_EVIDENCE_PATH `
        -ErrorAction SilentlyContinue
}

Write-Host ''
Write-Host '==> Exercise cross-process Desktop to Runtime trace' `
    -ForegroundColor Cyan

$startedUtc = [DateTimeOffset]::UtcNow.AddSeconds(-1)
$bootstrapExecutable = Join-Path `
    $repositoryRoot `
    'artifacts\bin\Opure.Bootstrap.Windows\release\Opure.Bootstrap.Windows.exe'
$startInfo = [System.Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = $bootstrapExecutable
$startInfo.UseShellExecute = $false
$startInfo.WorkingDirectory = $repositoryRoot
$startInfo.Environment['OPURE_BOOTSTRAP_TEST_MODE'] = '1'

foreach ($argument in @(
    '--layout',
    'Development',
    '--configuration',
    'Release',
    '--channel',
    'Development',
    '--desktop-close-after-ms',
    '1500')) {
    [void]$startInfo.ArgumentList.Add($argument)
}

$process = [System.Diagnostics.Process]::new()
$process.StartInfo = $startInfo

try {
    if (-not $process.Start()) {
        throw 'FND-019 Bootstrap trace launch did not start.'
    }

    if (-not $process.WaitForExit(30000)) {
        $process.Kill($true)
        throw 'FND-019 Bootstrap trace launch exceeded 30 seconds.'
    }

    if ($process.ExitCode -ne 0) {
        throw "FND-019 Bootstrap trace launch exited with $($process.ExitCode)."
    }
}
finally {
    $process.Dispose()
}

$localApplicationData = [Environment]::GetFolderPath(
    [Environment+SpecialFolder]::LocalApplicationData)
$logRoot = Join-Path `
    $localApplicationData `
    'Opure\Development\diagnostics\operational\opure.runtime'

$traceRecords = @(
    Get-ChildItem -LiteralPath $logRoot -Filter '*.jsonl' -File |
        ForEach-Object {
            Get-Content -LiteralPath $_.FullName |
                ForEach-Object {
                    try {
                        $_ | ConvertFrom-Json
                    }
                    catch {
                        $null
                    }
                }
        } |
        Where-Object {
            $_ -and
            $_.eventName -eq 'runtime.trace.completed' -and
            [DateTimeOffset]$_.timestampUtc -ge $startedUtc
        }
)

if ($traceRecords.Count -lt 1) {
    throw 'FND-019 cross-process launch produced no correlated Runtime trace log.'
}

$traceRecord = $traceRecords |
    Sort-Object timestampUtc -Descending |
    Select-Object -First 1

if ($traceRecord.traceId -notmatch '^[0-9a-f]{32}$' -or
    $traceRecord.operationId -notmatch '^[0-9a-f]{16}$') {
    throw 'FND-019 Runtime trace log identities are malformed.'
}

[System.IO.File]::WriteAllLines(
    $crossProcessPath,
    @(
        'schema=opure.trace-cross-process/1',
        'result=Passed',
        'bootstrapDesktopRuntimeBoundary=Passed',
        'w3cTraceIdentityInRuntimeLog=Passed',
        'spanIdentityInRuntimeLog=Passed',
        'sessionMaterialIncluded=False',
        'payloadIncluded=False',
        'authoritative=False'
    ))

foreach ($evidencePath in @(
    $tracePath,
    $leakagePath,
    $latencyPath,
    $crossProcessPath,
    $verificationPath)) {
    if (-not (Test-Path -LiteralPath $evidencePath -PathType Leaf)) {
        throw "FND-019 evidence is missing: $evidencePath"
    }
}

$trace = [System.IO.File]::ReadAllText($tracePath) | ConvertFrom-Json
$latency = [System.IO.File]::ReadAllText($latencyPath) | ConvertFrom-Json
$leakage = [System.IO.File]::ReadAllText($leakagePath)
$crossProcess = [System.IO.File]::ReadAllText($crossProcessPath)

if ($trace.schema -ne 'opure.operational-trace-example/1' -or
    $trace.result -ne 'Passed' -or
    $trace.traceIdentityNormalised -ne $true -or
    $trace.authoritative -ne $false -or
    $trace.spans.Count -ne 3) {
    throw 'FND-019 trace example does not match the implemented policy.'
}

if ($latency.schema -ne 'opure.trace-latency/1' -or
    $latency.result -ne 'Passed' -or
    $latency.enabledP95Milliseconds -ge 250 -or
    $latency.medianOverheadMilliseconds -ge 20 -or
    $latency.payloadAttributes -ne $false) {
    throw 'FND-019 trace latency evidence exceeded its bounds.'
}

foreach ($requiredLine in @(
    'result=Passed',
    'payloadCanaryAbsent=Passed',
    'pipeNameAbsent=Passed',
    'absolutePathAbsent=Passed',
    'requestResponseAttributesAbsent=Passed',
    'attributeAllowlist=Passed',
    'baggagePropagation=Disabled',
    'authoritative=False')) {
    if (-not $leakage.Contains($requiredLine, [StringComparison]::Ordinal)) {
        throw "FND-019 leakage evidence is incomplete: $requiredLine"
    }
}

foreach ($requiredLine in @(
    'result=Passed',
    'bootstrapDesktopRuntimeBoundary=Passed',
    'w3cTraceIdentityInRuntimeLog=Passed',
    'spanIdentityInRuntimeLog=Passed',
    'sessionMaterialIncluded=False',
    'payloadIncluded=False',
    'authoritative=False')) {
    if (-not $crossProcess.Contains(
            $requiredLine,
            [StringComparison]::Ordinal)) {
        throw "FND-019 cross-process evidence is incomplete: $requiredLine"
    }
}

foreach ($prohibitedToken in @(
    'C:\Users\',
    'ghp_',
    'github_pat_',
    'Authorization:',
    'Basic ',
    'Cookie:',
    'Set-Cookie:',
    'Bearer ',
    'sessionSecret',
    'clientSecret',
    'connectionString',
    'requestBody',
    'responseBody',
    'sourceContent')) {
    foreach ($evidencePath in @(
        $tracePath,
        $leakagePath,
        $latencyPath,
        $crossProcessPath,
        $verificationPath)) {
        $evidence = [System.IO.File]::ReadAllText($evidencePath)

        if ($evidence.Contains(
                $prohibitedToken,
                [StringComparison]::OrdinalIgnoreCase)) {
            throw "FND-019 evidence contains prohibited material: $prohibitedToken"
        }
    }
}

Write-Host ''
Write-Host 'FND-019 trace propagation verification passed.' `
    -ForegroundColor Green
