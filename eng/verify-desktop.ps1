#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

if (-not $IsWindows) {
    throw 'FND-005 real-window evidence currently requires Windows 11.'
}

$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M1'
$desktopExecutable = Join-Path `
    $repositoryRoot `
    'artifacts\bin\Opure.Desktop\release\Opure.Desktop.exe'

New-Item -ItemType Directory -Force -Path $evidenceRoot | Out-Null

Write-Host ''
Write-Host '==> Verify Desktop build and tests' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration Release `
    -BuildChannel Development

if (-not (Test-Path -LiteralPath $desktopExecutable -PathType Leaf)) {
    throw "Desktop executable was not produced: $desktopExecutable"
}

Write-Host ''
Write-Host '==> Launch real Avalonia window' -ForegroundColor Cyan

$startInfo = [System.Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = $desktopExecutable
$startInfo.UseShellExecute = $false
$startInfo.WorkingDirectory = $repositoryRoot
$startInfo.Environment['OPURE_DESKTOP_TEST_MODE'] = '1'
[void]$startInfo.ArgumentList.Add('--close-after-ms')
[void]$startInfo.ArgumentList.Add('2500')

$process = [System.Diagnostics.Process]::new()
$process.StartInfo = $startInfo
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

try {
    if (-not $process.Start()) {
        throw 'Desktop real-window process did not start.'
    }

    $windowHandle = [IntPtr]::Zero
    $windowTitle = ''
    $windowDeadline = [DateTimeOffset]::UtcNow.AddSeconds(5)

    while ([DateTimeOffset]::UtcNow -lt $windowDeadline) {
        if ($process.HasExited) {
            break
        }

        $process.Refresh()
        $windowHandle = $process.MainWindowHandle
        $windowTitle = $process.MainWindowTitle

        if ($windowHandle -ne [IntPtr]::Zero) {
            break
        }

        Start-Sleep -Milliseconds 100
    }

    if ($windowHandle -eq [IntPtr]::Zero) {
        if (-not $process.HasExited) {
            $process.Kill($true)
        }

        throw 'Desktop did not expose a real main window within five seconds.'
    }

    if (-not $process.WaitForExit(10000)) {
        $process.Kill($true)
        throw 'Desktop did not close within the bounded smoke-test deadline.'
    }

    $stopwatch.Stop()

    if ($process.ExitCode -ne 0) {
        throw "Desktop real-window process exited with code $($process.ExitCode)."
    }

    if (-not [string]::Equals(
            $windowTitle,
            'Opure',
            [System.StringComparison]::Ordinal)) {
        throw "Unexpected Desktop window title: $windowTitle"
    }

    $launchEvidence = @(
        'Opure Desktop Launch Evidence'
        '============================='
        'Framework: Avalonia'
        'FrameworkVersion: 12.1.0'
        "Executable: $($desktopExecutable | Split-Path -Leaf)"
        "ProcessId: $($process.Id)"
        "MainWindowHandleObserved: $($windowHandle -ne [IntPtr]::Zero)"
        "MainWindowTitle: $windowTitle"
        "ElapsedMilliseconds: $($stopwatch.ElapsedMilliseconds)"
        "ExitCode: $($process.ExitCode)"
        'InitialRuntimeState: Unavailable'
        'Result: Passed'
    )

    [System.IO.File]::WriteAllLines(
        (Join-Path $evidenceRoot 'desktop-launch-evidence.txt'),
        [string[]]$launchEvidence,
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

$accessibilityEvidence = @"
# FND-005 Accessibility Smoke

**Result:** Passed by automated headless checks; manual Narrator confirmation remains part of the broader ADR-0002 prototype.

## Automated checks

- Home, Projects, Workflows and Trust Centre controls are tab stops.
- Tab order is explicit and stable from 1 through 4.
- Each primary navigation control exposes a stable automation name.
- Each primary navigation control exposes a stable automation identifier.
- Runtime unavailable status exposes explicit text and automation metadata.
- Disconnected state is conveyed by words, not colour alone.
- The real Windows window launches and closes cleanly.

## Manual follow-up retained by ADR-0002

- Narrator announcement quality.
- High-contrast usability.
- 100%, 150% and 200% scaling.
- Mixed-DPI and multi-monitor movement.
"@

[System.IO.File]::WriteAllText(
    (Join-Path $evidenceRoot 'desktop-accessibility-smoke.md'),
    $accessibilityEvidence.Replace("`r`n", "`n"),
    [System.Text.UTF8Encoding]::new($false)
)

$adapterDiagram = @"
# FND-005 Desktop Framework Adapter

```text
Opure.Desktop.Contracts
    DesktopShellSnapshot
    IDesktopShellStateSource
    DesktopShellViewModel
             |
             | framework-neutral state
             v
Opure.Desktop
    DesktopShellComposition
    MainWindow.axaml
    Avalonia application lifetime
             |
             | presentation only
             v
Windows 11 window
```

## Boundary

- `Opure.Desktop.Contracts` has no Avalonia package or type dependency.
- `Opure.Desktop` is the Avalonia adapter.
- Runtime and domain projects do not reference Avalonia.
- The Desktop does not reference Runtime implementation projects.
- A WinUI 3 fallback can reuse the contracts and view model while replacing the adapter and views.
"@

[System.IO.File]::WriteAllText(
    (Join-Path $evidenceRoot 'desktop-framework-adapter.md'),
    $adapterDiagram.Replace("`r`n", "`n"),
    [System.Text.UTF8Encoding]::new($false)
)

$templateEvidence = @"
# FND-005 Avalonia Template Baseline

- Template package: `Avalonia.Templates`
- Template version: `12.1.0`
- Template short name: `avalonia.app`
- Materialisation: reviewed repository-owned snapshot
- Target framework: `net10.0`
- Runtime packages: centrally pinned at `12.1.0`

The installer does not mutate the developer's global `dotnet new` template cache.
"@

[System.IO.File]::WriteAllText(
    (Join-Path $evidenceRoot 'desktop-template-baseline.md'),
    $templateEvidence.Replace("`r`n", "`n"),
    [System.Text.UTF8Encoding]::new($false)
)

Write-Host ''
Write-Host 'FND-005 Desktop verification passed.' -ForegroundColor Green
