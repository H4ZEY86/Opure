$ErrorActionPreference = 'Stop'

Write-Host "Uninstalling Opure..."

# 1. Terminate the daemon process if it is running
$ProcessName = "Opure.Runtime"
$RunningProcesses = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue
if ($RunningProcesses) {
    Write-Host "Terminating $ProcessName..."
    $RunningProcesses | Stop-Process -Force
}

$CliProcessName = "Opure.Cli"
$RunningCli = Get-Process -Name $CliProcessName -ErrorAction SilentlyContinue
if ($RunningCli) {
    Write-Host "Terminating $CliProcessName..."
    $RunningCli | Stop-Process -Force
}

# 2. Remove the MSIX app
# Note: The PackageName must match what's in AppxManifest.template.xml
# Let's try to remove anything starting with 'Opure' from Publisher 'H4ZEY86'
$Packages = Get-AppxPackage -Name "*Opure*" -Publisher "*H4ZEY86*"
if ($Packages) {
    foreach ($Pkg in $Packages) {
        Write-Host "Removing MSIX package $($Pkg.PackageFullName)..."
        Remove-AppxPackage -Package $Pkg.PackageFullName
    }
}

# 3. Remove the binaries from %LOCALAPPDATA%\Opure\bin
$BinPath = Join-Path $env:LOCALAPPDATA "Opure\bin"
if (Test-Path $BinPath) {
    Write-Host "Removing CLI binaries from $BinPath..."
    Remove-Item -Path $BinPath -Recurse -Force
}

Write-Host "Uninstallation complete." -ForegroundColor Green
