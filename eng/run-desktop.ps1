#requires -Version 7.2

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateRange(0, 60000)]
    [int] $CloseAfterMilliseconds = 0
)

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

if (-not $IsWindows) {
    throw 'The FND-005 real Desktop shell currently targets Windows 11.'
}

& (Join-Path $PSScriptRoot 'restore.ps1') -Locked
& (Join-Path $PSScriptRoot 'build.ps1') `
    -Configuration Debug `
    -BuildChannel Development

$desktopExecutable = Join-Path `
    $repositoryRoot `
    'artifacts\bin\Opure.Desktop\debug\Opure.Desktop.exe'

if (-not (Test-Path -LiteralPath $desktopExecutable -PathType Leaf)) {
    throw "Desktop executable was not produced: $desktopExecutable"
}

$startInfo = [System.Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = $desktopExecutable
$startInfo.UseShellExecute = $false
$startInfo.WorkingDirectory = $repositoryRoot

if ($CloseAfterMilliseconds -gt 0) {
    $startInfo.Environment['OPURE_DESKTOP_TEST_MODE'] = '1'
    [void]$startInfo.ArgumentList.Add('--close-after-ms')
    [void]$startInfo.ArgumentList.Add([string]$CloseAfterMilliseconds)
}

$process = [System.Diagnostics.Process]::new()
$process.StartInfo = $startInfo

try {
    if (-not $process.Start()) {
        throw 'Desktop process did not start.'
    }

    $process.WaitForExit()

    if ($process.ExitCode -ne 0) {
        throw "Desktop exited with code $($process.ExitCode)."
    }
}
finally {
    $process.Dispose()
}
