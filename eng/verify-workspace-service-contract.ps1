#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot
$workspaceTests = Join-Path $repositoryRoot 'tests\Workspace\Opure.Workspace.Protocol.Tests\Opure.Workspace.Protocol.Tests.csproj'
$projectTests = Join-Path $repositoryRoot 'tests\Project\Opure.Project.Sqlite.Tests\Opure.Project.Sqlite.Tests.csproj'
$architectureTests = Join-Path $repositoryRoot 'tests\Architecture\Opure.ArchitectureTests\Opure.ArchitectureTests.csproj'
$schemaPath = Join-Path $repositoryRoot 'src\Workspace\Opure.Workspace.Protocol\Protos\snapshot\workspace_snapshot.proto'
$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M4'
$evidencePaths = @(
    (Join-Path $evidenceRoot 'workspace-snapshot-contract.json'),
    (Join-Path $evidenceRoot 'workspace-snapshot-fixtures.json'),
    (Join-Path $evidenceRoot 'workspace-snapshot-limit-rationale.md'),
    (Join-Path $evidenceRoot 'workspace-service-contract-verification.md'))

Write-Host ''
Write-Host '==> Verify FND-033 Workspace Service contract' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') -Configuration Release -BuildChannel Development

& dotnet test $workspaceTests --configuration Release --no-build --no-restore `
    --filter-class 'Opure.Workspace.Protocol.Tests.WorkspaceSnapshotContractTests' --timeout 60s
if ($LASTEXITCODE -ne 0) { throw 'FND-033 Workspace contract acceptance tests failed.' }

& dotnet test $projectTests --configuration Release --no-build --no-restore `
    --filter-class 'Opure.Project.Sqlite.Tests.ProjectOpenServiceTests' --timeout 60s
if ($LASTEXITCODE -ne 0) { throw 'FND-033 Project-to-Workspace boundary tests failed.' }

& dotnet test $architectureTests --configuration Release --no-build --no-restore `
    --filter-class 'Opure.ArchitectureTests.WorkspaceServiceBoundaryTests' --timeout 60s
if ($LASTEXITCODE -ne 0) { throw 'FND-033 Workspace architecture tests failed.' }

$schema = [System.IO.File]::ReadAllText($schemaPath)
foreach ($prohibited in @('absolute_path', 'display_path', 'raw_content', 'bytes content')) {
    if ($schema.Contains($prohibited, [StringComparison]::OrdinalIgnoreCase)) {
        throw "FND-033 schema contains prohibited authority or content: $prohibited"
    }
}

foreach ($path in $evidencePaths) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "FND-033 evidence is missing: $path" }
    $content = [System.IO.File]::ReadAllText($path)
    foreach ($token in @('C:\Users\', 'ghp_', 'github_pat_', 'Authorization:', 'Bearer ', 'Password=')) {
        if ($content.Contains($token, [StringComparison]::OrdinalIgnoreCase)) {
            throw "FND-033 evidence contains prohibited material: $token"
        }
    }
    if ($content -match '[A-Za-z]:[\\/]' -or $content -match '\\\\[^\\/\r\n]+[\\/]') {
        throw "FND-033 evidence contains an absolute or UNC path: $path"
    }
}

$contract = [System.IO.File]::ReadAllText($evidencePaths[0]) | ConvertFrom-Json
$fixtures = [System.IO.File]::ReadAllText($evidencePaths[1]) | ConvertFrom-Json
if ($contract.result -ne 'Passed' -or $contract.contractRevision -ne 1 -or `
    $contract.maximumFileCount -ne 100000 -or $contract.maximumObservedBytes -ne 4294967296 -or `
    $contract.maximumDurationMilliseconds -ne 30000 -or $contract.absolutePathsAllowed -ne $false -or `
    $contract.rawFileContentAllowed -ne $false -or $contract.projectRootBindingRequired -ne $true -or `
    $fixtures.result -ne 'Passed' -or $fixtures.schemaRoundTrip -ne $true -or `
    $fixtures.crossProjectDenied -ne $true -or $fixtures.misleadingCompletionDenied -ne $true -or `
    $fixtures.unsupportedFileClassSafe -ne $true) {
    throw 'FND-033 evidence is incomplete.'
}

Write-Host ''
Write-Host 'FND-033 Workspace Service contract verification passed.' -ForegroundColor Green
