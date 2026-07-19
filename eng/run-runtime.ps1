#requires -Version 7.2

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateRange(0, 60000)]
    [int] $ShutdownAfterMilliseconds = 0
)

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

& (Join-Path $PSScriptRoot 'restore.ps1') -Locked
& (Join-Path $PSScriptRoot 'build.ps1') `
    -Configuration Debug `
    -BuildChannel Development

$runtimeAssembly = Join-Path `
    $repositoryRoot `
    'artifacts\bin\Opure.Runtime\debug\Opure.Runtime.dll'

if (-not (Test-Path -LiteralPath $runtimeAssembly -PathType Leaf)) {
    throw "Runtime assembly was not produced: $runtimeAssembly"
}

$arguments = @($runtimeAssembly)

if ($ShutdownAfterMilliseconds -gt 0) {
    $arguments += @(
        '--shutdown-after-ms',
        [string]$ShutdownAfterMilliseconds
    )
}

Push-Location $repositoryRoot
try {
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Runtime exited with code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}
