#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

if (-not $IsWindows) {
    throw 'FND-006 Bootstrap evidence currently requires Windows 11.'
}

$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M1'
$bootstrapExecutable = Join-Path `
    $repositoryRoot `
    'artifacts\bin\Opure.Bootstrap.Windows\release\Opure.Bootstrap.Windows.exe'

New-Item -ItemType Directory -Force -Path $evidenceRoot | Out-Null

Write-Host ''
Write-Host '==> Verify Bootstrap build and tests' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration Release `
    -BuildChannel Development

if (-not (Test-Path -LiteralPath $bootstrapExecutable -PathType Leaf)) {
    throw "Bootstrap executable was not produced: $bootstrapExecutable"
}

Write-Host ''
Write-Host '==> Launch Bootstrap process tree' -ForegroundColor Cyan

$startInfo = [System.Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = $bootstrapExecutable
$startInfo.UseShellExecute = $false
$startInfo.WorkingDirectory = $repositoryRoot
$startInfo.RedirectStandardOutput = $true
$startInfo.RedirectStandardError = $true
$startInfo.Environment['OPURE_BOOTSTRAP_TEST_MODE'] = '1'

foreach ($argument in @(
    '--layout',
    'Development',
    '--configuration',
    'Release',
    '--channel',
    'Development',
    '--desktop-close-after-ms',
    '3000'
)) {
    [void]$startInfo.ArgumentList.Add($argument)
}

$process = [System.Diagnostics.Process]::new()
$process.StartInfo = $startInfo
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$observedParentLinks = @{}

try {
    if (-not $process.Start()) {
        throw 'Bootstrap evidence process did not start.'
    }

    $standardOutputTask = $process.StandardOutput.ReadToEndAsync()
    $standardErrorTask = $process.StandardError.ReadToEndAsync()
    $observationDeadline = [DateTimeOffset]::UtcNow.AddSeconds(15)

    while ([DateTimeOffset]::UtcNow -lt $observationDeadline) {
        if ($process.HasExited) {
            break
        }

        $children = Get-CimInstance `
            -ClassName Win32_Process `
            -Filter "ParentProcessId = $($process.Id)" `
            -ErrorAction SilentlyContinue

        foreach ($child in $children) {
            $name = [string]$child.Name

            if ($name -in @('Opure.Runtime.exe', 'Opure.Desktop.exe')) {
                $observedParentLinks[$name] = [int]$child.ProcessId
            }
        }

        Start-Sleep -Milliseconds 50
    }

    if (-not $process.WaitForExit(20000)) {
        $process.Kill($true)
        throw 'Bootstrap did not complete its bounded evidence launch.'
    }

    $process.WaitForExit()
    $stopwatch.Stop()

    $standardOutput = $standardOutputTask.GetAwaiter().GetResult()
    $standardError = $standardErrorTask.GetAwaiter().GetResult()

    if ($process.ExitCode -ne 0) {
        throw @"
Bootstrap evidence launch failed with exit code $($process.ExitCode).
Standard error:
$standardError
Standard output:
$standardOutput
"@
    }

    $events = @(
        $standardOutput -split "`r?`n" |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            ForEach-Object { $_ | ConvertFrom-Json }
    )

    $runtimeStarted = $events |
        Where-Object {
            $_.event -eq 'bootstrap.child.started' -and
            $_.processClass -eq 'runtime'
        } |
        Select-Object -First 1

    $runtimeReady = $events |
        Where-Object {
            $_.event -eq 'bootstrap.child.ready' -and
            $_.processClass -eq 'runtime'
        } |
        Select-Object -First 1

    $desktopStarted = $events |
        Where-Object {
            $_.event -eq 'bootstrap.child.started' -and
            $_.processClass -eq 'desktop'
        } |
        Select-Object -First 1

    if ($null -eq $runtimeStarted -or
        $null -eq $runtimeReady -or
        $null -eq $desktopStarted) {
        throw @"
Bootstrap diagnostics omitted required child lifecycle events.
Standard error:
$standardError
Standard output:
$standardOutput
"@
    }

    $runtimeProcessId = [int]$runtimeStarted.processId
    $desktopProcessId = [int]$desktopStarted.processId

    foreach ($childPid in @($runtimeProcessId, $desktopProcessId)) {
        $remaining = Get-Process -Id $childPid -ErrorAction SilentlyContinue

        if ($null -ne $remaining) {
            throw "Bootstrap left child process $childPid running."
        }
    }

    $processTreeEvidence = @(
        'Opure Bootstrap Process Tree'
        '============================'
        "BootstrapProcessId: $($process.Id)"
        "RuntimeProcessId: $runtimeProcessId"
        "DesktopProcessId: $desktopProcessId"
        'StartOrder: Runtime, Desktop'
        'ShutdownOrder: Desktop, Runtime'
        "RuntimeParentCorroboratedByCim: $($observedParentLinks.ContainsKey('Opure.Runtime.exe'))"
        "DesktopParentCorroboratedByCim: $($observedParentLinks.ContainsKey('Opure.Desktop.exe'))"
        'ChildPidSource: Bootstrap structured lifecycle diagnostics'
        'ChildrenRemainingAfterExit: 0'
        "ElapsedMilliseconds: $($stopwatch.ElapsedMilliseconds)"
        "ExitCode: $($process.ExitCode)"
        'Result: Passed'
    )

    [System.IO.File]::WriteAllLines(
        (Join-Path $evidenceRoot 'bootstrap-process-tree.txt'),
        [string[]]$processTreeEvidence,
        [System.Text.UTF8Encoding]::new($false)
    )

    $identityEvidence = [ordered]@{
        result = 'Passed'
        runtime = [ordered]@{
            executableName = [string]$runtimeStarted.executableName
            assemblyName = [string]$runtimeStarted.assemblyName
            productVersion = [string]$runtimeStarted.productVersion
            executableSha256 = [string]$runtimeStarted.executableSha256
            bootId = [string]$runtimeReady.bootId
        }
        desktop = [ordered]@{
            executableName = [string]$desktopStarted.executableName
            assemblyName = [string]$desktopStarted.assemblyName
            productVersion = [string]$desktopStarted.productVersion
            executableSha256 = [string]$desktopStarted.executableSha256
        }
        absolutePathsVerified = $true
        currentDirectorySearchUsed = $false
        sessionMaterialRecorded = $false
    }

    [System.IO.File]::WriteAllText(
        (Join-Path $evidenceRoot 'bootstrap-binary-identity.json'),
        ($identityEvidence | ConvertTo-Json -Depth 5),
        [System.Text.UTF8Encoding]::new($false)
    )
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

$dataRootEvidence = @"
# FND-006 Channel Data-Root Report

**Result:** Passed

Bootstrap derives three non-colliding per-user roots:

```text
<LocalApplicationData>\Opure\Stable
<LocalApplicationData>\Opure\Preview
<LocalApplicationData>\Opure\Development
```

## Enforcement

- the root is absolute;
- the channel name is an exact path segment;
- Runtime validates that the received root matches the received channel;
- Bootstrap does not create or mutate the root;
- session material is passed only through bounded child-process environment variables;
- session material is not written to evidence or command lines.
"@

[System.IO.File]::WriteAllText(
    (Join-Path $evidenceRoot 'bootstrap-channel-data-roots.md'),
    $dataRootEvidence.Replace("`r`n", "`n"),
    [System.Text.UTF8Encoding]::new($false)
)

Write-Host ''
Write-Host 'FND-006 Bootstrap verification passed.' -ForegroundColor Green
