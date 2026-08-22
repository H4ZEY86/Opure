$ErrorActionPreference = "Stop"

Write-Host "Stage 1: Building Runtime CLI to Staging..."
dotnet publish src\Runtime\Opure.Runtime\Opure.Runtime.csproj -c Release -r win-x64 --self-contained false -o artifacts\CLI_Staging

Write-Host "Stage 2: Building Desktop and Copying CLI..."
$publishDir = "artifacts/publish/Desktop"
$msixDir = "artifacts/GateD"
dotnet publish src/Desktop/Opure.Desktop/Opure.Desktop.csproj -c Release --self-contained false -o $publishDir

Write-Host "Stage 3: Packaging MSIX..."
$runtimeDest = "$publishDir/Runtime"
if (-not (Test-Path $runtimeDest)) {
    New-Item -ItemType Directory -Force -Path $runtimeDest | Out-Null
}
Copy-Item "artifacts/CLI_Staging/*" -Destination $runtimeDest -Recurse -Force

Copy-Item "src/Desktop/Opure.Desktop.Package/Package.appxmanifest" -Destination "$publishDir/AppxManifest.xml" -Force
Copy-Item "src/Desktop/Opure.Desktop.Package/Images" -Destination "$publishDir/Images" -Recurse -Force

$makeappx = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\makeappx.exe"
if (-not (Test-Path $makeappx)) {
    # Try finding any makeappx
    $makeappx = Get-ChildItem "C:\Program Files (x86)\Windows Kits\10\bin" -Recurse -Filter "makeappx.exe" | Where-Object { $_.FullName -match "x64" } | Select-Object -First 1 -ExpandProperty FullName
}
& $makeappx pack -d $publishDir -p "$msixDir/Opure.GateD.Sandbox.msix" -o

Write-Host "Stage 4: Signing MSIX..."
$signtool = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe"
if (-not (Test-Path $signtool)) {
    $signtool = Get-ChildItem "C:\Program Files (x86)\Windows Kits\10\bin" -Recurse -Filter "signtool.exe" | Where-Object { $_.FullName -match "x64" } | Select-Object -First 1 -ExpandProperty FullName
}
& $signtool sign /fd SHA256 /a /f eng/certs/OpureDev.pfx /p OpureDev123 "$msixDir/Opure.GateD.Sandbox.msix"

Write-Host "MSIX Build Complete!"
