#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot
$workspaceTests = Join-Path $repositoryRoot 'tests\Workspace\Opure.Workspace.Windows.Tests\Opure.Workspace.Windows.Tests.csproj'
$filesystemTests = Join-Path $repositoryRoot 'tests\Filesystem\Opure.Filesystem.Windows.Tests\Opure.Filesystem.Windows.Tests.csproj'
$architectureTests = Join-Path $repositoryRoot 'tests\Architecture\Opure.ArchitectureTests\Opure.ArchitectureTests.csproj'
$hasherPath = Join-Path $repositoryRoot 'src\Workspace\Opure.Workspace.Windows\WindowsWorkspaceFileHasher.cs'
$modelsPath = Join-Path $repositoryRoot 'src\Workspace\Opure.Workspace.Contracts\WorkspaceFileHashModels.cs'
$testsPath = Join-Path $repositoryRoot 'tests\Workspace\Opure.Workspace.Windows.Tests\WindowsWorkspaceFileHasherTests.cs'
$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M4'
$evidencePaths = @(
    (Join-Path $evidenceRoot 'workspace-hashing-correctness-report.json'),
    (Join-Path $evidenceRoot 'workspace-hashing-race-condition-report.json'),
    (Join-Path $evidenceRoot 'workspace-hashing-throughput-benchmark.json'),
    (Join-Path $evidenceRoot 'workspace-file-hashing-verification.md'))

Write-Host ''
Write-Host '==> Verify FND-035 safe Workspace file hashing' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') -Configuration Release -BuildChannel Development

& dotnet test $workspaceTests --configuration Release --no-build --no-restore `
    --filter-class 'Opure.Workspace.Windows.Tests.WindowsWorkspaceFileHasherTests' --timeout 60s
if ($LASTEXITCODE -ne 0) { throw 'FND-035 Workspace hashing acceptance tests failed.' }

& dotnet test $filesystemTests --configuration Release --no-build --no-restore `
    --filter-class 'Opure.Filesystem.Windows.Tests.WindowsPathReferenceResolverTests' --timeout 60s
if ($LASTEXITCODE -ne 0) { throw 'FND-035 verified-handle compatibility tests failed.' }

& dotnet test $architectureTests --configuration Release --no-build --no-restore `
    --filter-class 'Opure.ArchitectureTests.WorkspaceServiceBoundaryTests' --timeout 60s
if ($LASTEXITCODE -ne 0) { throw 'FND-035 Workspace architecture tests failed.' }

$hasher = [System.IO.File]::ReadAllText($hasherPath)
$models = [System.IO.File]::ReadAllText($modelsPath)
$tests = [System.IO.File]::ReadAllText($testsPath)
foreach ($prohibited in @('File.ReadAll', 'File.OpenRead', 'FileStream', 'ILogger', 'System.Net', 'Process.Start')) {
    if ($hasher.Contains($prohibited, [StringComparison]::Ordinal)) {
        throw "FND-035 hasher contains prohibited capability: $prohibited"
    }
}
foreach ($required in @('ResolveFileForRead', 'RefreshMetadata', 'Revalidate', 'IncrementalHash', 'SHA256', 'ZeroMemory', 'MaximumAttempts', 'MaximumFileSizeBytes', 'CancellationToken')) {
    if (-not $hasher.Contains($required, [StringComparison]::Ordinal)) {
        throw "FND-035 hasher is missing required policy: $required"
    }
}
foreach ($required in @('SHA-256', 'AlgorithmVersion', 'WorkspaceFileHashDisposition')) {
    if (-not ($models.Contains($required, [StringComparison]::Ordinal) -or `
            $hasher.Contains($required, [StringComparison]::Ordinal))) {
        throw "FND-035 hashing contract is missing: $required"
    }
}
if (-not $tests.Contains('OPURE-FND035-CONTENT-CANARY-4f2d', [StringComparison]::Ordinal) -or `
    $hasher.Contains('OPURE-FND035-CONTENT-CANARY-4f2d', [StringComparison]::Ordinal)) {
    throw 'FND-035 content-canary coverage is incomplete.'
}

foreach ($path in $evidencePaths) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "FND-035 evidence is missing: $path" }
    $content = [System.IO.File]::ReadAllText($path)
    foreach ($token in @('C:\Users\', 'ghp_', 'github_pat_', 'Authorization:', 'Bearer ', 'Password=', 'OPURE-FND035-CONTENT-CANARY-4f2d')) {
        if ($content.Contains($token, [StringComparison]::OrdinalIgnoreCase)) {
            throw "FND-035 evidence contains prohibited material: $token"
        }
    }
    if ($content -match '[A-Za-z]:[\\/]' -or $content -match '\\\\[^\\/\r\n]+[\\/]') {
        throw "FND-035 evidence contains an absolute or UNC path: $path"
    }
}

$correctness = [System.IO.File]::ReadAllText($evidencePaths[0]) | ConvertFrom-Json
$races = [System.IO.File]::ReadAllText($evidencePaths[1]) | ConvertFrom-Json
$benchmark = [System.IO.File]::ReadAllText($evidencePaths[2]) | ConvertFrom-Json
if ($correctness.result -ne 'Passed' -or $correctness.algorithm -ne 'SHA-256' -or `
    $correctness.algorithmVersion -ne 1 -or $correctness.knownAnswer -ne $true -or `
    $correctness.oversizedExcluded -ne $true -or $correctness.lockedFileExplicit -ne $true -or `
    $correctness.cancellation -ne $true -or $correctness.contentCanaryExcluded -ne $true -or `
    $races.result -ne 'Passed' -or $races.concurrentModificationDetected -ne $true -or `
    $races.replacementIdentityDetected -ne $true -or $races.reparseSubstitutionDenied -ne $true -or `
    $benchmark.result -ne 'Passed' -or $benchmark.fixtureBytes -ne 8388608 -or `
    $benchmark.maximumAllowedMilliseconds -ne 20000 -or $benchmark.bufferBytes -ne 65536) {
    throw 'FND-035 evidence is incomplete.'
}

Write-Host ''
Write-Host 'FND-035 safe Workspace file hashing verification passed.' -ForegroundColor Green
