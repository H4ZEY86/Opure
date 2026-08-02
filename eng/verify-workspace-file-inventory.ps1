#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot
$workspaceTests = Join-Path $repositoryRoot 'tests\Workspace\Opure.Workspace.Windows.Tests\Opure.Workspace.Windows.Tests.csproj'
$filesystemTests = Join-Path $repositoryRoot 'tests\Filesystem\Opure.Filesystem.Windows.Tests\Opure.Filesystem.Windows.Tests.csproj'
$architectureTests = Join-Path $repositoryRoot 'tests\Architecture\Opure.ArchitectureTests\Opure.ArchitectureTests.csproj'
$generatorPath = Join-Path $repositoryRoot 'src\Workspace\Opure.Workspace.Windows\WindowsWorkspaceInventoryGenerator.cs'
$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M4'
$evidencePaths = @(
    (Join-Path $evidenceRoot 'workspace-inventory-algorithm.md'),
    (Join-Path $evidenceRoot 'workspace-inventory-adversarial-fixtures.json'),
    (Join-Path $evidenceRoot 'workspace-inventory-benchmark.json'),
    (Join-Path $evidenceRoot 'workspace-file-inventory-verification.md'))

Write-Host ''
Write-Host '==> Verify FND-034 Workspace file inventory' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') -Configuration Release -BuildChannel Development

& dotnet test $workspaceTests --configuration Release --no-build --no-restore `
    --filter-class 'Opure.Workspace.Windows.Tests.WindowsWorkspaceInventoryGeneratorTests' --timeout 60s
if ($LASTEXITCODE -ne 0) { throw 'FND-034 Workspace inventory acceptance tests failed.' }

& dotnet test $filesystemTests --configuration Release --no-build --no-restore `
    --filter-class 'Opure.Filesystem.Windows.Tests.WindowsPathReferenceResolverTests' --timeout 60s
if ($LASTEXITCODE -ne 0) { throw 'FND-034 verified-handle compatibility tests failed.' }

& dotnet test $architectureTests --configuration Release --no-build --no-restore `
    --filter-class 'Opure.ArchitectureTests.WorkspaceServiceBoundaryTests' --timeout 60s
if ($LASTEXITCODE -ne 0) { throw 'FND-034 Workspace architecture tests failed.' }

$generator = [System.IO.File]::ReadAllText($generatorPath)
foreach ($prohibited in @('File.ReadAll', 'File.OpenRead', 'FileStream', 'Process.Start', 'System.Net')) {
    if ($generator.Contains($prohibited, [StringComparison]::Ordinal)) {
        throw "FND-034 inventory generator contains prohibited capability: $prohibited"
    }
}
foreach ($required in @('InspectExisting', 'ResolveExisting', 'CancellationToken', 'MaximumEntryCount', 'REPARSE_TRAVERSAL_DENIED')) {
    if (-not $generator.Contains($required, [StringComparison]::Ordinal)) {
        throw "FND-034 inventory generator is missing required policy: $required"
    }
}

foreach ($path in $evidencePaths) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "FND-034 evidence is missing: $path" }
    $content = [System.IO.File]::ReadAllText($path)
    foreach ($token in @('C:\Users\', 'ghp_', 'github_pat_', 'Authorization:', 'Bearer ', 'Password=')) {
        if ($content.Contains($token, [StringComparison]::OrdinalIgnoreCase)) {
            throw "FND-034 evidence contains prohibited material: $token"
        }
    }
    if ($content -match '[A-Za-z]:[\\/]' -or $content -match '\\\\[^\\/\r\n]+[\\/]') {
        throw "FND-034 evidence contains an absolute or UNC path: $path"
    }
}

$fixtures = [System.IO.File]::ReadAllText($evidencePaths[1]) | ConvertFrom-Json
$benchmark = [System.IO.File]::ReadAllText($evidencePaths[2]) | ConvertFrom-Json
if ($fixtures.result -ne 'Passed' -or $fixtures.smallTree -ne $true -or `
    $fixtures.entryLimit -ne $true -or $fixtures.depthLimit -ne $true -or `
    $fixtures.symlinkDenied -ne $true -or $fixtures.junctionDenied -ne $true -or `
    $fixtures.hiddenIncludedAndLabelled -ne $true -or $fixtures.casePreserved -ne $true -or `
    $fixtures.cancellation -ne $true -or $fixtures.directoryMutationPartial -ne $true -or `
    $benchmark.result -ne 'Passed' -or $benchmark.entryCount -ne 250 -or `
    $benchmark.maximumAllowedMilliseconds -ne 20000 -or $benchmark.fileContentReads -ne $false) {
    throw 'FND-034 evidence is incomplete.'
}

Write-Host ''
Write-Host 'FND-034 Workspace file inventory verification passed.' -ForegroundColor Green
