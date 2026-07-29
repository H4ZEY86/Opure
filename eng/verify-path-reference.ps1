#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

$filesystemTests = Join-Path $repositoryRoot `
    'tests\Filesystem\Opure.Filesystem.Windows.Tests\Opure.Filesystem.Windows.Tests.csproj'
$architectureTests = Join-Path $repositoryRoot `
    'tests\Architecture\Opure.ArchitectureTests\Opure.ArchitectureTests.csproj'
$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M4'
$threatModel = Join-Path $evidenceRoot 'filesystem-threat-model.md'
$adversarialReport = Join-Path $evidenceRoot 'path-adversarial-report.json'
$apiReview = Join-Path $evidenceRoot 'typed-path-reference-api-review.md'

Write-Host ''
Write-Host '==> Verify FND-026 Windows Path-Reference Library' `
    -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration Release `
    -BuildChannel Development

& dotnet test $filesystemTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-026 Windows path-reference tests failed.'
}

& dotnet test $architectureTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter-class 'Opure.ArchitectureTests.FilesystemBoundaryTests' `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-026 filesystem architecture tests failed.'
}

foreach ($path in @($threatModel, $adversarialReport, $apiReview)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "FND-026 evidence is missing: $path"
    }
}

$report = [System.IO.File]::ReadAllText($adversarialReport) |
    ConvertFrom-Json

if ($report.schema -ne 'opure.windows-path-reference-adversarial/1' -or
    $report.result -ne 'Passed' -or
    $report.scenarios.Count -lt 10 -or
    $report.handleHeld -ne $true -or
    $report.fileId128 -ne $true -or
    $report.volumeBound -ne $true -or
    $report.reparseTraversalAllowed -ne $false -or
    $report.rawConcatenationContract -ne $false) {
    throw 'FND-026 adversarial evidence is incomplete.'
}

foreach ($path in @($threatModel, $adversarialReport, $apiReview)) {
    $content = [System.IO.File]::ReadAllText($path)

    foreach ($token in @(
        'C:\Users\',
        'ghp_',
        'github_pat_',
        'Authorization:',
        'Bearer ',
        'Password=',
        'sessionSecret',
        'clientSecret')) {
        if ($content.Contains(
                $token,
                [StringComparison]::OrdinalIgnoreCase)) {
            throw "FND-026 evidence contains prohibited material: $token"
        }
    }

    if ($content -match '[A-Za-z]:[\\/]' -or
        $content -match '\\\\[^\\/\r\n]+[\\/]') {
        throw "FND-026 evidence contains an absolute or UNC path: $path"
    }
}

Write-Host ''
Write-Host 'FND-026 Windows Path-Reference verification passed.' `
    -ForegroundColor Green
