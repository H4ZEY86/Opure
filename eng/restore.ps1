#requires -Version 7.2

[CmdletBinding()]
param(
    [Parameter()]
    [switch] $Locked
)

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

Push-Location $repositoryRoot
try {
    Invoke-OpureNativeCommand -FilePath 'dotnet' -Arguments @(
        'tool',
        'restore'
    )

    $arguments = @(
        'restore',
        (Join-Path $repositoryRoot 'Opure.slnx')
    )

    if ($Locked) {
        $arguments += '--locked-mode'
    }

    Invoke-OpureNativeCommand -FilePath 'dotnet' -Arguments $arguments
}
finally {
    Pop-Location
}
