#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M1'
$schemaPath = Join-Path `
    $repositoryRoot `
    'src\Runtime\Opure.Runtime.Contracts\Protos\registry\runtime_service_registry.proto'
$runtimeTests = Join-Path `
    $repositoryRoot `
    'tests\Runtime\Opure.Runtime.Tests\Opure.Runtime.Tests.csproj'
$transportTests = Join-Path `
    $repositoryRoot `
    'tests\Ipc\Opure.Ipc.NamedPipes.Windows.Tests\Opure.Ipc.NamedPipes.Windows.Tests.csproj'
$cataloguePath = Join-Path $evidenceRoot 'runtime-service-catalogue.json'

New-Item -ItemType Directory -Force -Path $evidenceRoot | Out-Null

Write-Host ''
Write-Host '==> Verify FND-011 build and tests' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration Release `
    -BuildChannel Development

Write-Host ''
Write-Host '==> Exercise Service Registry contract cases' -ForegroundColor Cyan

$previousCataloguePath = $env:OPURE_SERVICE_CATALOGUE_EVIDENCE_PATH

try {
    $env:OPURE_SERVICE_CATALOGUE_EVIDENCE_PATH = $cataloguePath
    & dotnet test $runtimeTests `
        --configuration Release `
        --no-build `
        --no-restore `
        --filter-class 'Opure.Runtime.Tests.RuntimeServiceRegistryTests' `
        --timeout 60s
}
finally {
    if ($null -eq $previousCataloguePath) {
        Remove-Item Env:OPURE_SERVICE_CATALOGUE_EVIDENCE_PATH `
            -ErrorAction SilentlyContinue
    }
    else {
        $env:OPURE_SERVICE_CATALOGUE_EVIDENCE_PATH = $previousCataloguePath
    }
}

if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $cataloguePath)) {
    throw 'FND-011 initial Service Registry catalogue evidence was not produced.'
}

Write-Host ''
Write-Host '==> Exercise authenticated Service Registry transport' -ForegroundColor Cyan

& dotnet test $transportTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter-method `
    'Opure.Ipc.NamedPipes.Windows.Tests.NamedPipeRuntimeHealthTransportTests.Service_registry_query_uses_authenticated_named_pipe' `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-011 authenticated Service Registry transport test failed.'
}

if (-not (Test-Path -LiteralPath $schemaPath -PathType Leaf)) {
    throw 'FND-011 authoritative Service Registry schema is missing.'
}

$schema = [System.IO.File]::ReadAllText($schemaPath)

foreach ($prohibitedToken in @(
    'google.protobuf.Any',
    'google.protobuf.Struct',
    'map<',
    'bytes ',
    'class_name',
    'database_path',
    'connection_string',
    'exception'
)) {
    if ($schema.Contains($prohibitedToken, [StringComparison]::OrdinalIgnoreCase)) {
        throw "FND-011 schema contains prohibited implementation metadata: $prohibitedToken"
    }
}

[System.IO.File]::WriteAllText(
    (Join-Path $evidenceRoot 'runtime-service-registry.proto'),
    $schema.Replace("`r`n", "`n"),
    [System.Text.UTF8Encoding]::new($false)
)

$catalogue = [System.IO.File]::ReadAllText($cataloguePath)

foreach ($prohibitedToken in @(
    'RuntimeServiceRegistry',
    '.cs',
    '.dll',
    '.db',
    'databasePath',
    'connectionString',
    '\\'
)) {
    if ($catalogue.Contains($prohibitedToken, [StringComparison]::OrdinalIgnoreCase)) {
        throw "FND-011 catalogue contains prohibited implementation metadata: $prohibitedToken"
    }
}

$parsedCatalogue = $catalogue | ConvertFrom-Json

if ($parsedCatalogue.contractRevision -ne 1 -or
    @($parsedCatalogue.registry.services).Count -ne 1 -or
    $parsedCatalogue.registry.services[0].serviceId -ne 'runtime.health' -or
    $parsedCatalogue.registry.services[0].ownerId -ne 'runtime.kernel' -or
    $parsedCatalogue.registry.services[0].processPlacement -ne
        'RUNTIME_SERVICE_PROCESS_PLACEMENT_RUNTIME_PROCESS') {
    throw 'FND-011 initial Service Registry catalogue is not the reviewed deterministic projection.'
}

Write-Host ''
Write-Host 'FND-011 Service Registry contract verification passed.' -ForegroundColor Green
