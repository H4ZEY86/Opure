#requires -Version 7.2

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [Parameter()]
    [ValidateSet('Development', 'Preview', 'Stable')]
    [string] $BuildChannel = 'Development'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))

$versionFile = Join-Path $RepositoryRoot 'version.json'
$versionDocument = Get-Content -LiteralPath $versionFile -Raw | ConvertFrom-Json
$declaredVersion = [string]$versionDocument.version

# Parse version and compute MSIX version
$msixMajor = 1
$msixMinor = 0
$msixBuild = 0
$msixRevision = 0

if ($declaredVersion -match '^(\d+)\.(\d+)\.(\d+)(?:-(.+))?$') {
    $msixMajor = [int]$matches[1] + 1
    $msixMinor = [int]$matches[2]
    $msixBuild = [int]$matches[3]
    
    $prerelease = $matches[4]
    if ([string]::IsNullOrEmpty($prerelease)) {
        $msixRevision = 60000
    }
    elseif ($prerelease -match 'preview\.(\d+)') {
        $msixRevision = 10000 + [int]$matches[1]
    }
    elseif ($prerelease -match 'beta\.(\d+)') {
        $msixRevision = 20000 + [int]$matches[1]
    }
    elseif ($prerelease -match 'rc\.(\d+)') {
        $msixRevision = 30000 + [int]$matches[1]
    }
    else {
        $msixRevision = 1
    }
}
$msixVersion = "$msixMajor.$msixMinor.$msixBuild.$msixRevision"

Write-Host "Packaging Opure v$declaredVersion (MSIX: $msixVersion) for channel $BuildChannel"

$certPath = Join-Path $RepositoryRoot "artifacts\OpureTestCert.pfx"
$certPassword = "password"

if (-not (Test-Path $certPath)) {
    Write-Host "Creating self-signed test certificate..."
    $cert = New-SelfSignedCertificate -Type Custom -Subject "CN=Opure Development" -KeyUsage DigitalSignature -FriendlyName "Opure Test Cert" -CertStoreLocation "Cert:\CurrentUser\My" -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
    $securePwd = ConvertTo-SecureString -String $certPassword -Force -AsPlainText
    Export-PfxCertificate -Cert $cert -FilePath $certPath -Password $securePwd | Out-Null
}

$publishDir = Join-Path $RepositoryRoot "artifacts\publish"
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

$outputDir = Join-Path $RepositoryRoot "artifacts\packages"
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
}

$projectsToPublish = @(
    "src\Bootstrap\Opure.Bootstrap.Windows\Opure.Bootstrap.Windows.csproj",
    "src\Runtime\Opure.Runtime\Opure.Runtime.csproj",
    "src\Desktop\Opure.Desktop\Opure.Desktop.csproj",
    "src\Cli\Opure.Cli\Opure.Cli.csproj"
)

foreach ($project in $projectsToPublish) {
    $projectPath = Join-Path $RepositoryRoot $project
    Write-Host "Publishing $projectPath..."
    dotnet publish $projectPath -c $Configuration -r win-x64 --self-contained true -o $publishDir /p:OpureBuildChannel=$BuildChannel
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to publish $projectPath"
    }
}

$packagingProject = Join-Path $RepositoryRoot "src\Packaging\Opure.Packaging.Windows\Opure.Packaging.Windows.csproj"
Write-Host "Running Packaging Tool..."
dotnet run --project $packagingProject -c $Configuration -- $outputDir $publishDir $msixVersion $BuildChannel $certPath $certPassword
if ($LASTEXITCODE -ne 0) {
    throw "Packaging failed."
}
