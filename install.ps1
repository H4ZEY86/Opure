#requires -Version 5.1

param(
    [Parameter()]
    [ValidateSet('All', 'Cert', 'Package')]
    [string] $Stage = 'All'
)

$ErrorActionPreference = 'Stop'
$msixName = 'Opure.Preview-1.1.0.10000-win-x64.msix'
$certPath = Join-Path $PSScriptRoot 'OpureTestCert.pfx'
$msixPath = Join-Path $PSScriptRoot $msixName

function Test-IsAdministrator {
    return ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Install-OpureTestCertificate {
    if (-not (Test-Path -LiteralPath $certPath -PathType Leaf)) {
        throw "Certificate not found: $certPath"
    }

    Write-Host "Installing Opure Test Certificate..." -ForegroundColor Cyan
    $password = ConvertTo-SecureString -String 'password' -Force -AsPlainText
    Import-PfxCertificate -FilePath $certPath -CertStoreLocation Cert:\LocalMachine\Root -Password $password | Out-Null
    Write-Host "Certificate trusted successfully." -ForegroundColor Green
}

function Install-OpurePackage {
    if (-not (Test-Path -LiteralPath $msixPath -PathType Leaf)) {
        throw "MSIX not found: $msixPath"
    }

    Write-Host "Installing Opure Preview for the current user..." -ForegroundColor Cyan
    Add-AppxPackage -Path $msixPath
    Write-Host "Opure installed successfully! Search Start for 'Opure'." -ForegroundColor Green
}

switch ($Stage) {
    'Cert' {
        if (-not (Test-IsAdministrator)) {
            throw 'Certificate install requires an elevated PowerShell window.'
        }

        Install-OpureTestCertificate
        return
    }

    'Package' {
        Install-OpurePackage
        return
    }

    'All' {
        # Cert needs admin; package must install for THIS user (not the elevated admin identity).
        if (-not (Test-IsAdministrator)) {
            Write-Host "Requesting Administrator approval for the test certificate..." -ForegroundColor Cyan
            $certProcess = Start-Process -FilePath PowerShell `
                -Verb RunAs `
                -Wait `
                -PassThru `
                -ArgumentList @(
                    '-NoProfile',
                    '-ExecutionPolicy', 'Bypass',
                    '-File', "`"$PSCommandPath`"",
                    '-Stage', 'Cert'
                )

            if ($certProcess.ExitCode -ne 0) {
                throw "Certificate install failed with exit code $($certProcess.ExitCode)."
            }
        }
        else {
            Install-OpureTestCertificate
        }

        Install-OpurePackage
    }
}

if ($Host.Name -eq 'ConsoleHost') {
    Write-Host "`nPress any key to exit..."
    $null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
}
