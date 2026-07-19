#requires -Version 7.2

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',

    [Parameter()]
    [ValidateSet('Development', 'Preview', 'Stable')]
    [string] $Channel = 'Development',

    [Parameter()]
    [ValidateRange(0, 60000)]
    [int] $DesktopCloseAfterMilliseconds = 0
)

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

if (-not $IsWindows) {
    throw 'FND-006 Bootstrap currently targets Windows 11.'
}

& (Join-Path $PSScriptRoot 'restore.ps1') -Locked
& (Join-Path $PSScriptRoot 'build.ps1') `
    -Configuration $Configuration `
    -BuildChannel $Channel

$bootstrapExecutable = Join-Path `
    $repositoryRoot `
    "artifacts\bin\Opure.Bootstrap.Windows\$($Configuration.ToLowerInvariant())\Opure.Bootstrap.Windows.exe"

if (-not (Test-Path -LiteralPath $bootstrapExecutable -PathType Leaf)) {
    throw "Bootstrap executable was not produced: $bootstrapExecutable"
}

$startInfo = [System.Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = $bootstrapExecutable
$startInfo.UseShellExecute = $false
$startInfo.WorkingDirectory = $repositoryRoot

[void]$startInfo.ArgumentList.Add('--layout')
[void]$startInfo.ArgumentList.Add('Development')
[void]$startInfo.ArgumentList.Add('--configuration')
[void]$startInfo.ArgumentList.Add($Configuration)
[void]$startInfo.ArgumentList.Add('--channel')
[void]$startInfo.ArgumentList.Add($Channel)

if ($DesktopCloseAfterMilliseconds -gt 0) {
    $startInfo.Environment['OPURE_BOOTSTRAP_TEST_MODE'] = '1'
    [void]$startInfo.ArgumentList.Add('--desktop-close-after-ms')
    [void]$startInfo.ArgumentList.Add([string]$DesktopCloseAfterMilliseconds)
}

$process = [System.Diagnostics.Process]::new()
$process.StartInfo = $startInfo

try {
    if (-not $process.Start()) {
        throw 'Bootstrap process did not start.'
    }

    $process.WaitForExit()

    if ($process.ExitCode -ne 0) {
        throw "Bootstrap exited with code $($process.ExitCode)."
    }
}
finally {
    $process.Dispose()
}
