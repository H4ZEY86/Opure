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
$schemaPath = Join-Path $evidenceRoot 'project-database-schema.json'
$identityPath = Join-Path $evidenceRoot 'project-identity-report.json'
$ownershipPath = Join-Path $evidenceRoot 'project-ownership-conformance.json'

Write-Host ''
Write-Host '==> Verify FND-028 Project Service database' `
    -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration Release `
    -BuildChannel Development

& dotnet test $projectTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-028 Project database tests failed.'
}

& dotnet test $architectureTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter-class `
    'Opure.ArchitectureTests.ProjectServiceBoundaryTests' `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-028 Project Service architecture tests failed.'
}

foreach ($path in @($schemaPath, $identityPath, $ownershipPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "FND-028 evidence is missing: $path"
    }
}

$schema = [System.IO.File]::ReadAllText($schemaPath) | ConvertFrom-Json
$identity = [System.IO.File]::ReadAllText($identityPath) | ConvertFrom-Json
$ownership = [System.IO.File]::ReadAllText($ownershipPath) |
    ConvertFrom-Json

if ($schema.schema -ne 'opure.project-database-schema/1' -or
    $schema.result -ne 'Passed' -or
    $schema.databaseName -ne 'projects.db' -or
    $schema.schemaVersion -ne 4 -or
    $schema.tables.Count -ne 7 -or
    $schema.authoritative -ne $true -or
    $schema.wal -ne $true -or
    $schema.foreignKeys -ne $true -or
    $schema.outboxSameTransaction -ne $true) {
    throw 'FND-028 schema evidence is incomplete.'
}

if ($identity.schema -ne 'opure.project-identity-report/1' -or
    $identity.result -ne 'Passed' -or
    $identity.projectIdBytes -ne 16 -or
    $identity.exactDuplicate -ne 'Existing' -or
    $identity.pathIdentityConflict -ne 'Rejected' -or
    $identity.channelIsolation -ne $true -or
    $identity.restartSafe -ne $true -or
    $identity.lifecyclePersistent -ne $true) {
    throw 'FND-028 identity evidence is incomplete.'
}

if ($ownership.schema -ne 'opure.project-ownership/1' -or
    $ownership.result -ne 'Passed' -or
    $ownership.ownerServiceId -ne 'opure.project' -or
    $ownership.otherServiceWriters -ne 0 -or
    $ownership.desktopDatabaseAuthority -ne $false -or
    $ownership.verifiedRootRequired -ne $true -or
    $ownership.rootRevalidatedAtCommit -ne $true) {
    throw 'FND-028 ownership evidence is incomplete.'
}

foreach ($path in @($schemaPath, $identityPath, $ownershipPath)) {
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
            throw "FND-028 evidence contains prohibited material: $token"
        }
    }

    if ($content -match '[A-Za-z]:[\\/]' -or
        $content -match '\\\\[^\\/\r\n]+[\\/]') {
        throw "FND-028 evidence contains an absolute or UNC path: $path"
    }
}

Write-Host ''
Write-Host 'FND-028 Project Service database verification passed.' `
    -ForegroundColor Green
