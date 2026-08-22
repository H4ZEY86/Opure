$ErrorActionPreference = 'Stop'

Write-Host "--- Stage 1: Publishing Desktop (Single-File) ---" -ForegroundColor Cyan
dotnet publish src\Desktop\Opure.Desktop\Opure.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts\Staging\Desktop

Write-Host "--- Stage 2: Publishing Runtime (Single-File) ---" -ForegroundColor Cyan
dotnet publish src\Runtime\Opure.Runtime\Opure.Runtime.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts\Staging\Runtime

Write-Host "--- Stage 3: Building WiX Installer ---" -ForegroundColor Cyan
dotnet build src\Setup\Opure.Setup\Opure.Setup.wixproj -c Release -o artifacts\GateD

Write-Host "--- Build Complete ---" -ForegroundColor Green
Write-Host "Installer is available at: artifacts\GateD\Opure.GateD.msi" -ForegroundColor Green
