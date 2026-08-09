# Relaunch as Administrator if not already elevated
if (!([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Start-Process PowerShell -Verb RunAs "-NoProfile -ExecutionPolicy Bypass -Command `"cd '$PSScriptRoot'; & '.\uninstall.ps1'`""
    exit
}

$ErrorActionPreference = 'Continue'
$certThumbprint = 'B9638BC21D5CBEDE8FF72D9A334AB827F1483E0B'
$certSubject = 'CN=Opure Development'

Write-Host "Uninstalling Opure Preview..." -ForegroundColor Cyan
$packages = @(Get-AppxPackage -Name '*Opure*' -ErrorAction SilentlyContinue)
if ($packages.Count -eq 0) {
    Write-Host "No Opure AppX package found (already uninstalled)." -ForegroundColor Yellow
}
else {
    foreach ($package in $packages) {
        try {
            Write-Host "Removing $($package.PackageFullName)..."
            Remove-AppxPackage -Package $package.PackageFullName
            Write-Host "Removed $($package.Name)." -ForegroundColor Green
        }
        catch {
            Write-Host "Failed to remove $($package.PackageFullName): $_" -ForegroundColor Red
        }
    }
}

Write-Host "`nRemoving Opure Test Certificate..." -ForegroundColor Cyan
$certs = @(
    Get-ChildItem Cert:\LocalMachine\Root -ErrorAction SilentlyContinue |
        Where-Object {
            $_.Thumbprint -eq $certThumbprint -or
            $_.Subject -eq $certSubject
        }
)

if ($certs.Count -eq 0) {
    Write-Host "No Opure test certificate found in LocalMachine\Root." -ForegroundColor Yellow
}
else {
    foreach ($cert in $certs) {
        try {
            $store = New-Object System.Security.Cryptography.X509Certificates.X509Store(
                [System.Security.Cryptography.X509Certificates.StoreName]::Root,
                [System.Security.Cryptography.X509Certificates.StoreLocation]::LocalMachine)
            $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
            $store.Remove($cert)
            $store.Close()
            Write-Host "Removed certificate $($cert.Thumbprint)." -ForegroundColor Green
        }
        catch {
            Write-Host "Failed to remove certificate $($cert.Thumbprint): $_" -ForegroundColor Red
        }
    }
}

$dataRoot = Join-Path $env:LOCALAPPDATA 'Opure'
if (Test-Path -LiteralPath $dataRoot) {
    Write-Host "`nRemoving local Opure data ($dataRoot)..." -ForegroundColor Cyan
    try {
        Remove-Item -LiteralPath $dataRoot -Recurse -Force
        Write-Host "Local data removed." -ForegroundColor Green
    }
    catch {
        Write-Host "Failed to remove local data: $_" -ForegroundColor Red
    }
}
else {
    Write-Host "`nNo local Opure data folder found." -ForegroundColor Yellow
}

Write-Host "`nUninstall finished." -ForegroundColor Green
Write-Host "`nPress any key to exit..."
$Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") | Out-Null
