$ErrorActionPreference = "Stop"

Write-Host "Restoring solution..."
dotnet restore Opure.slnx

$publishDir = "artifacts/publish/Desktop"
$msixDir = "artifacts/GateD"

Write-Host "Publishing Desktop..."
# Note: we use standard publish for Avalonia apps on .NET
# It will build in Release mode and output to $publishDir
dotnet publish src/Desktop/Opure.Desktop/Opure.Desktop.csproj -c Release --self-contained false -o $publishDir

Write-Host "Staging Package assets..."
Copy-Item "src/Desktop/Opure.Desktop.Package/Package.appxmanifest" -Destination "$publishDir/AppxManifest.xml"
Copy-Item "src/Desktop/Opure.Desktop.Package/Images" -Destination "$publishDir/Images" -Recurse -Force

Write-Host "Building MSIX Package..."
New-Item -ItemType Directory -Force -Path $msixDir | Out-Null
$makeappx = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\makeappx.exe"
# The -o flag overwrites if exists.
& $makeappx pack -d $publishDir -p "$msixDir/Opure.GateD.Sandbox.msix" -o

Write-Host "Signing MSIX Package..."
$signtool = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe"
& $signtool sign /fd SHA256 /a /f eng/certs/OpureDev.pfx /p OpureDev123 "$msixDir/Opure.GateD.Sandbox.msix"

Write-Host "MSIX Build Complete!"
