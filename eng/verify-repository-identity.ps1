#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot
$projectTests = Join-Path $repositoryRoot `
    'tests\Project\Opure.Project.Sqlite.Tests\Opure.Project.Sqlite.Tests.csproj'
$architectureTests = Join-Path $repositoryRoot `
    'tests\Architecture\Opure.ArchitectureTests\Opure.ArchitectureTests.csproj'
$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M4'
$evidencePaths = @(
    (Join-Path $evidenceRoot 'repository-identity-design.json'),
    (Join-Path $evidenceRoot 'repository-git-detection.json'),
    (Join-Path $evidenceRoot 'repository-remote-privacy.json'),
    (Join-Path $evidenceRoot 'repository-identity-verification.md'))

Write-Host ''
Write-Host '==> Verify FND-031 Repository Identity Detection' `
    -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration Release `
    -BuildChannel Development

& dotnet test $projectTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter-class `
    'Opure.Project.Sqlite.Tests.RepositoryIdentityDetectorTests' `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-031 repository detection acceptance tests failed.'
}

& dotnet test $architectureTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter-class 'Opure.ArchitectureTests.RepositoryBoundaryTests' `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-031 repository architecture tests failed.'
}

$detectorPath = Join-Path $repositoryRoot `
    'src\Repository\Opure.Repository.Git\GitRepositoryIdentityDetector.cs'
$detector = [System.IO.File]::ReadAllText($detectorPath)

foreach ($token in @(
    'Process.Start',
    'System.Diagnostics',
    'System.Net',
    'CredentialsProvider',
    '.Fetch(',
    '.Push(')) {
    if ($detector.Contains($token, [StringComparison]::Ordinal)) {
        throw "FND-031 detector contains forbidden authority: $token"
    }
}

foreach ($path in $evidencePaths) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "FND-031 evidence is missing: $path"
    }

    $content = [System.IO.File]::ReadAllText($path)

    foreach ($token in @(
        'C:\Users\',
        'ghp_',
        'github_pat_',
        'Authorization:',
        'Bearer ',
        'Password=',
        'never-persist-this-token')) {
        if ($content.Contains(
                $token,
                [StringComparison]::OrdinalIgnoreCase)) {
            throw "FND-031 evidence contains prohibited material: $token"
        }
    }

    if ($content -match '[A-Za-z]:[\\/]' -or
        $content -match '\\\\[^\\/\r\n]+[\\/]') {
        throw "FND-031 evidence contains an absolute or UNC path: $path"
    }
}

$design = [System.IO.File]::ReadAllText($evidencePaths[0]) |
    ConvertFrom-Json
$detection = [System.IO.File]::ReadAllText($evidencePaths[1]) |
    ConvertFrom-Json
$privacy = [System.IO.File]::ReadAllText($evidencePaths[2]) |
    ConvertFrom-Json

if ($design.result -ne 'Passed' -or
    $design.repositoryWriteAuthority -ne $false -or
    $design.networkAuthority -ne $false -or
    $design.externalProcessUsed -ne $false -or
    $design.outsideProjectMetadataAllowed -ne $false -or
    $detection.result -ne 'Passed' -or
    $detection.schemaVersion -ne 5 -or
    $detection.corruptMetadataDegraded -ne $true -or
    $privacy.result -ne 'Passed' -or
    $privacy.rawRemotePersisted -ne $false -or
    $privacy.credentialProviderInvoked -ne $false -or
    $privacy.credentialCanaryAbsent -ne $true) {
    throw 'FND-031 repository evidence is incomplete.'
}

Write-Host ''
Write-Host 'FND-031 Repository Identity verification passed.' `
    -ForegroundColor Green
