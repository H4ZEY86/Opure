#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

$projectTests = Join-Path $repositoryRoot `
    'tests\Project\Opure.Project.Sqlite.Tests\Opure.Project.Sqlite.Tests.csproj'
$ipcTests = Join-Path $repositoryRoot `
    'tests\Ipc\Opure.Ipc.NamedPipes.Windows.Tests\Opure.Ipc.NamedPipes.Windows.Tests.csproj'
$architectureTests = Join-Path $repositoryRoot `
    'tests\Architecture\Opure.ArchitectureTests\Opure.ArchitectureTests.csproj'
$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M4'
$sequencePath = Join-Path $evidenceRoot 'open-project-sequence.md'
$racePath = Join-Path $evidenceRoot 'open-project-race-report.json'
$fixturesPath = Join-Path $evidenceRoot `
    'open-project-contract-fixtures.json'

Write-Host ''
Write-Host '==> Verify FND-029 Open Project flow' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration Release `
    -BuildChannel Development

& dotnet test $projectTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-029 Project Service and contract tests failed.'
}

& dotnet test $ipcTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter-class `
    'Opure.Ipc.NamedPipes.Windows.Tests.NamedPipeProjectOpenTransportTests' `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-029 named-pipe and Desktop Gateway tests failed.'
}

& dotnet test $architectureTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter-class `
    'Opure.ArchitectureTests.ProjectServiceBoundaryTests' `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-029 Project Service architecture tests failed.'
}

foreach ($path in @($sequencePath, $racePath, $fixturesPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "FND-029 evidence is missing: $path"
    }
}

$race = [System.IO.File]::ReadAllText($racePath) | ConvertFrom-Json
$fixtures = [System.IO.File]::ReadAllText($fixturesPath) |
    ConvertFrom-Json
$sequence = [System.IO.File]::ReadAllText($sequencePath)

if ($race.schema -ne 'opure.open-project-races/1' -or
    $race.result -ne 'Passed' -or
    $race.preCommitCancellation -ne 'NoProject' -or
    $race.rootDeletion -ne 'RejectedNoProject' -or
    $race.identitySubstitution -ne 'ReviewRequired' -or
    $race.samePathChangedIdentity -ne 'ReviewRequired' -or
    $race.postCommitCancellation -ne 'RecoveryRequired' -or
    $race.runtimeRestart -ne 'OpeningReconciledToOpen' -or
    $race.policyDenial -ne 'RejectedNoProject') {
    throw 'FND-029 race evidence is incomplete.'
}

if ($fixtures.schema -ne 'opure.open-project-contract-fixtures/1' -or
    $fixtures.result -ne 'Passed' -or
    $fixtures.contractRevision -ne 1 -or
    $fixtures.transport -ne 'grpc-over-windows-named-pipe' -or
    $fixtures.sessionAuthenticated -ne $true -or
    $fixtures.requestIdentityClaim -ne $true -or
    $fixtures.runtimeIdentityRevalidation -ne $true -or
    $fixtures.responseDatabaseObject -ne $false -or
    $fixtures.maximumRequestBytes -ne 8192 -or
    $fixtures.maximumResponseBytes -ne 8192) {
    throw 'FND-029 contract fixture evidence is incomplete.'
}

foreach ($required in @(
    'Desktop',
    'Desktop Gateway',
    'Project Service',
    'Opening',
    'Workspace Snapshot',
    'Open',
    'RecoveryRequired')) {
    if (-not $sequence.Contains($required, [StringComparison]::Ordinal)) {
        throw "FND-029 sequence evidence omits: $required"
    }
}

foreach ($path in @($sequencePath, $racePath, $fixturesPath)) {
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
            throw "FND-029 evidence contains prohibited material: $token"
        }
    }

    if ($content -match '[A-Za-z]:[\\/]' -or
        $content -match '\\\\[^\\/\r\n]+[\\/]') {
        throw "FND-029 evidence contains an absolute or UNC path: $path"
    }
}

Write-Host ''
Write-Host 'FND-029 Open Project verification passed.' `
    -ForegroundColor Green
