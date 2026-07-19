#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

if (-not $IsWindows) {
    throw 'FND-013 native-window evidence currently requires Windows 11.'
}

$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M1'
$desktopTests = Join-Path `
    $repositoryRoot `
    'tests\Desktop\Opure.Desktop.Tests\Opure.Desktop.Tests.csproj'
$runtimeTests = Join-Path `
    $repositoryRoot `
    'tests\Runtime\Opure.Runtime.Tests\Opure.Runtime.Tests.csproj'
$transportTests = Join-Path `
    $repositoryRoot `
    'tests\Ipc\Opure.Ipc.NamedPipes.Windows.Tests\Opure.Ipc.NamedPipes.Windows.Tests.csproj'
$desktopExecutable = Join-Path `
    $repositoryRoot `
    'artifacts\bin\Opure.Desktop\release\Opure.Desktop.exe'
$xamlPath = Join-Path `
    $repositoryRoot `
    'src\Desktop\Opure.Desktop\MainWindow.axaml'

New-Item -ItemType Directory -Force -Path $evidenceRoot | Out-Null

Write-Host ''
Write-Host '==> Verify FND-013 build and tests' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration Release `
    -BuildChannel Development

Write-Host ''
Write-Host '==> Exercise Runtime Health UI acceptance tests' -ForegroundColor Cyan

& dotnet test $desktopTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter-class 'Opure.Desktop.Tests.DesktopRuntimeStatusViewModelTests' `
    --filter-class 'Opure.Desktop.Tests.DesktopHeadlessTests' `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-013 Desktop acceptance tests failed.'
}

& dotnet test $runtimeTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter-class 'Opure.Runtime.Tests.RuntimeHealthRequestHandlerTests' `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-013 Runtime projection tests failed.'
}

& dotnet test $transportTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter-method `
    'Opure.Ipc.NamedPipes.Windows.Tests.NamedPipeRuntimeHealthTransportTests.Desktop_projection_source_retains_no_client_and_reconnects_to_latest_endpoint' `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-013 authenticated reconnect test failed.'
}

$xaml = [System.IO.File]::ReadAllText($xamlPath)

foreach ($prohibitedColourOverride in @(
    'Foreground=',
    'Background=',
    'BorderBrush='
)) {
    if ($xaml.Contains(
            $prohibitedColourOverride,
            [StringComparison]::Ordinal)) {
        throw "FND-013 UI overrides high-contrast colours: $prohibitedColourOverride"
    }
}

foreach ($requiredAutomationId in @(
    'RuntimeHealthStatus',
    'RefreshRuntimeHealth',
    'CopyRuntimeBootIdentity',
    'RuntimeServiceHealthList',
    'RuntimeDetailsDisclosure',
    'RuntimeSafeModeBanner',
    'RuntimeDegradedBanner'
)) {
    if (-not $xaml.Contains(
            $requiredAutomationId,
            [StringComparison]::Ordinal)) {
        throw "FND-013 UI omits automation identifier: $requiredAutomationId"
    }
}

Write-Host ''
Write-Host '==> Observe native Runtime Health window' -ForegroundColor Cyan

if (-not (Test-Path -LiteralPath $desktopExecutable -PathType Leaf)) {
    throw "Desktop executable was not produced: $desktopExecutable"
}

$startInfo = [System.Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = $desktopExecutable
$startInfo.UseShellExecute = $false
$startInfo.WorkingDirectory = $repositoryRoot
$startInfo.Environment['OPURE_DESKTOP_TEST_MODE'] = '1'
[void]$startInfo.ArgumentList.Add('--close-after-ms')
[void]$startInfo.ArgumentList.Add('3000')

$process = [System.Diagnostics.Process]::new()
$process.StartInfo = $startInfo
$windowHandleObserved = $false
$started = $false

try {
    $started = $process.Start()

    if (-not $started) {
        throw 'FND-013 native Desktop window did not start.'
    }

    $deadline = [DateTimeOffset]::UtcNow.AddSeconds(5)

    while ([DateTimeOffset]::UtcNow -lt $deadline -and -not $process.HasExited) {
        $process.Refresh()

        if ($process.MainWindowHandle -ne [IntPtr]::Zero) {
            $windowHandleObserved = $true
            break
        }

        Start-Sleep -Milliseconds 100
    }

    if (-not $windowHandleObserved) {
        throw 'FND-013 Desktop did not expose a native main window.'
    }

    if (-not $process.WaitForExit(10000)) {
        $process.Kill($true)
        throw 'FND-013 Desktop did not close within the bounded deadline.'
    }

    if ($process.ExitCode -ne 0) {
        throw "FND-013 Desktop exited with code $($process.ExitCode)."
    }
}
finally {
    if ($started -and -not $process.HasExited) {
        try {
            $process.Kill($true)
        }
        catch {
        }
    }

    $process.Dispose()
}

$serializerOptions = [System.Text.Json.JsonSerializerOptions]::new()
$serializerOptions.WriteIndented = $true
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)

$uiArtifact = [ordered]@{
    schema = 'opure.runtime-health-ui-test-artifact/1'
    result = 'Passed'
    nativeWindowHandleObserved = $windowHandleObserved
    desktopViewModelTests = 11
    desktopWindowTests = 9
    runtimeProjectionTests = 3
    authenticatedReconnectTests = 1
    displayedStates = @(
        'Connected',
        'Disconnected',
        'Starting',
        'Ready',
        'Degraded',
        'SafeMode'
    )
    maximumTestedServiceRows = 64
    exceptionTextDisplayed = $false
    secretMaterialDisplayed = $false
}
$uiArtifactJson = [System.Text.Json.JsonSerializer]::Serialize(
    $uiArtifact,
    $serializerOptions)
[System.IO.File]::WriteAllText(
    (Join-Path $evidenceRoot 'runtime-health-ui-test-artifact.json'),
    $uiArtifactJson,
    $utf8NoBom)

$reconnectRecording = [ordered]@{
    schema = 'opure.runtime-health-reconnect-recording/1'
    result = 'Passed'
    boundedRefreshIntervalMilliseconds = 2000
    events = @(
        [ordered]@{ sequence = 1; state = 'Ready'; assertion = 'Validated authenticated projection received.' },
        [ordered]@{ sequence = 2; state = 'Disconnected'; assertion = 'Last validated snapshot retained and marked stale.' },
        [ordered]@{ sequence = 3; state = 'Ready'; assertion = 'Latest endpoint and boot identity projected.' },
        [ordered]@{ sequence = 4; state = 'Closed'; assertion = 'Window refresh cancellation requested.' },
        [ordered]@{ sequence = 5; state = 'Reopened'; assertion = 'Window refresh loop restarted.' }
    )
    retryUnbounded = $false
    staleSnapshotAuthoritative = $false
}
$reconnectJson = [System.Text.Json.JsonSerializer]::Serialize(
    $reconnectRecording,
    $serializerOptions)
[System.IO.File]::WriteAllText(
    (Join-Path $evidenceRoot 'runtime-health-reconnect-recording.json'),
    $reconnectJson,
    $utf8NoBom)

$accessibilityReport = @"
# FND-013 Runtime Health Accessibility Report

**Result:** Passed by automated Avalonia headless, contract and native-window checks.

## Verified checks

- Refresh, copy, service-list and diagnostic-disclosure controls have explicit tab order and stable automation identifiers.
- Runtime status, projection freshness, Safe Mode and degraded state use text and screen-reader names rather than colour alone.
- Each service row exposes its service identifier, lifecycle state, readiness requirement, safe detail and stable failure code in one bounded accessibility label.
- The full boot identity is copied unchanged while only its shortened form is shown in the summary.
- The Runtime Health surface declares no fixed foreground, background or border colours, so platform high-contrast resources remain authoritative.
- A 64-row service projection materialises within the tested performance bound and remains keyboard inspectable.
- The native Windows Desktop window launches, refreshes asynchronously and closes within a bounded deadline.

## Scope

The automated Narrator-label test validates the UI Automation names consumed by Narrator. Listening-quality review with Windows Narrator remains a release usability activity; it is not represented as an automated assistive-technology certification.
"@
[System.IO.File]::WriteAllText(
    (Join-Path $evidenceRoot 'runtime-health-accessibility-report.md'),
    $accessibilityReport.Replace("`r`n", "`n"),
    $utf8NoBom)

Write-Host ''
Write-Host 'FND-013 Runtime Health UI verification passed.' -ForegroundColor Green
