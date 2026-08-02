#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot
$projectTests = Join-Path $repositoryRoot 'tests\Project\Opure.Project.Sqlite.Tests\Opure.Project.Sqlite.Tests.csproj'
$desktopTests = Join-Path $repositoryRoot 'tests\Desktop\Opure.Desktop.Tests\Opure.Desktop.Tests.csproj'
$ipcTests = Join-Path $repositoryRoot 'tests\Ipc\Opure.Ipc.NamedPipes.Windows.Tests\Opure.Ipc.NamedPipes.Windows.Tests.csproj'
$architectureTests = Join-Path $repositoryRoot 'tests\Architecture\Opure.ArchitectureTests\Opure.ArchitectureTests.csproj'
$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M4'
$evidencePaths = @(
    (Join-Path $evidenceRoot 'project-list-contract.json'),
    (Join-Path $evidenceRoot 'project-list-accessibility.json'),
    (Join-Path $evidenceRoot 'project-list-performance.json'),
    (Join-Path $evidenceRoot 'project-list-ui-verification.md'))

Write-Host ''
Write-Host '==> Verify FND-032 Project List UI' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') -Configuration Release -BuildChannel Development

& dotnet test $projectTests --configuration Release --no-build --no-restore `
    --filter-class 'Opure.Project.Sqlite.Tests.ProjectListProjectionServiceTests' --timeout 60s
if ($LASTEXITCODE -ne 0) { throw 'FND-032 Project Service acceptance tests failed.' }

& dotnet test $desktopTests --configuration Release --no-build --no-restore `
    --filter-class 'Opure.Desktop.Tests.DesktopProjectListViewModelTests' `
    --filter-class 'Opure.Desktop.Tests.ProjectListHeadlessTests' --timeout 60s
if ($LASTEXITCODE -ne 0) { throw 'FND-032 Desktop acceptance tests failed.' }

& dotnet test $ipcTests --configuration Release --no-build --no-restore `
    --filter-class 'Opure.Ipc.NamedPipes.Windows.Tests.NamedPipeProjectListTransportTests' --timeout 60s
if ($LASTEXITCODE -ne 0) { throw 'FND-032 authenticated transport test failed.' }

& dotnet test $architectureTests --configuration Release --no-build --no-restore `
    --filter-class 'Opure.ArchitectureTests.ProjectServiceBoundaryTests' --timeout 60s
if ($LASTEXITCODE -ne 0) { throw 'FND-032 architecture tests failed.' }

foreach ($path in $evidencePaths) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "FND-032 evidence is missing: $path" }
    $content = [System.IO.File]::ReadAllText($path)
    foreach ($token in @('C:\Users\', 'ghp_', 'github_pat_', 'Authorization:', 'Bearer ', 'Password=')) {
        if ($content.Contains($token, [StringComparison]::OrdinalIgnoreCase)) {
            throw "FND-032 evidence contains prohibited material: $token"
        }
    }
    if ($content -match '[A-Za-z]:[\\/]' -or $content -match '\\\\[^\\/\r\n]+[\\/]') {
        throw "FND-032 evidence contains an absolute or UNC path: $path"
    }
}

$contract = [System.IO.File]::ReadAllText($evidencePaths[0]) | ConvertFrom-Json
$accessibility = [System.IO.File]::ReadAllText($evidencePaths[1]) | ConvertFrom-Json
$performance = [System.IO.File]::ReadAllText($evidencePaths[2]) | ConvertFrom-Json
if ($contract.result -ne 'Passed' -or $contract.desktopDatabaseAuthority -ne $false -or `
    $contract.removeDeletesFiles -ne $false -or $contract.staleProjectionRetained -ne $true -or `
    $accessibility.result -ne 'Passed' -or $accessibility.keyboardOpen -ne $true -or `
    $accessibility.availabilityNarrated -ne $true -or $accessibility.colourOnlyState -ne $false -or `
    $performance.result -ne 'Passed' -or $performance.projectCount -ne 10000 -or `
    $performance.virtualisedList -ne $true) {
    throw 'FND-032 evidence is incomplete.'
}

Write-Host ''
Write-Host 'FND-032 Project List UI verification passed.' -ForegroundColor Green
