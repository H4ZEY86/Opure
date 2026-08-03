#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot
$tests = Join-Path $repositoryRoot 'tests\Configuration\Opure.Configuration.Contracts.Tests\Opure.Configuration.Contracts.Tests.csproj'
$architectureTests = Join-Path $repositoryRoot 'tests\Architecture\Opure.ArchitectureTests\Opure.ArchitectureTests.csproj'
$contractRoot = Join-Path $repositoryRoot 'src\Configuration\Opure.Configuration.Contracts'
$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M5'
$schemaPath = Join-Path $evidenceRoot 'setting-definition-schema.json'
$cataloguePath = Join-Path $evidenceRoot 'foundation-setting-definition-catalogue.json'
$reviewPath = Join-Path $evidenceRoot 'setting-definition-review.md'
$documentationPath = Join-Path $repositoryRoot 'docs\SETTING-DEFINITIONS.md'

Write-Host ''
Write-Host '==> Verify FND-039 Setting Definition schema' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') -Configuration Release -BuildChannel Development

$env:OPURE_SETTING_DEFINITION_CATALOGUE_PATH = $cataloguePath
$env:OPURE_SETTING_DEFINITION_DOCUMENTATION_PATH = $documentationPath
try {
    & dotnet test $tests --configuration Release --no-build --no-restore --timeout 60s
    if ($LASTEXITCODE -ne 0) { throw 'FND-039 Setting Definition acceptance tests failed.' }
}
finally {
    Remove-Item Env:OPURE_SETTING_DEFINITION_CATALOGUE_PATH -ErrorAction SilentlyContinue
    Remove-Item Env:OPURE_SETTING_DEFINITION_DOCUMENTATION_PATH -ErrorAction SilentlyContinue
}

& dotnet test $architectureTests --configuration Release --no-build --no-restore `
    --filter-class 'Opure.ArchitectureTests.ConfigurationBoundaryTests' --timeout 60s
if ($LASTEXITCODE -ne 0) { throw 'FND-039 Configuration architecture tests failed.' }

$source = [System.IO.File]::ReadAllText((Join-Path $contractRoot 'SettingDefinition.cs'))
$catalogueSource = [System.IO.File]::ReadAllText((Join-Path $contractRoot 'FoundationSettingDefinitionCatalogue.cs'))
foreach ($required in @('setting_id', 'DefinitionSha256', 'AllowedScopes', 'AllowedSources', 'MergeStrategy', 'Sensitivity', 'SecretPolicy', 'RestartImpact')) {
    if (-not $source.Contains($required, [StringComparison]::Ordinal)) {
        throw "FND-039 contract is missing required definition behaviour: $required"
    }
}
foreach ($required in @('security.integrity-validation.enabled', 'runtime.performance.default-mode', 'logging.level.default', 'desktop.appearance.theme', 'provider.credential.vault-reference')) {
    if (-not $catalogueSource.Contains($required, [StringComparison]::Ordinal)) {
        throw "FND-039 foundation catalogue is missing reviewed definition: $required"
    }
}

foreach ($path in @($schemaPath, $cataloguePath, $reviewPath, $documentationPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "FND-039 evidence is missing: $path" }
    $content = [System.IO.File]::ReadAllText($path)
    foreach ($token in @('C:\Users\', 'ghp_', 'github_pat_', 'Authorization:', 'Bearer ', 'Password=')) {
        if ($content.Contains($token, [StringComparison]::OrdinalIgnoreCase)) {
            throw "FND-039 evidence contains prohibited material: $token"
        }
    }
    if ($content -match '[A-Za-z]:[\\/]' -or $content -match '\\\\[^\\/\r\n]+[\\/]') {
        throw "FND-039 evidence contains an absolute or UNC path: $path"
    }
}

$schema = [System.IO.File]::ReadAllText($schemaPath) | ConvertFrom-Json
$catalogue = [System.IO.File]::ReadAllText($cataloguePath) | ConvertFrom-Json
if ($schema.result -ne 'Passed' -or $schema.schema -ne 'opure.setting-definition/1' -or `
    $schema.ordinarySecretValuesAllowed -ne $false -or $schema.projectSourcesGrantMachineAuthority -ne $false -or `
    $catalogue.schema -ne 'opure.setting-definition-catalogue/1' -or $catalogue.catalogue_revision -ne 1 -or `
    $catalogue.catalogue_sha256 -notmatch '^[a-f0-9]{64}$' -or `
    $catalogue.definitions.Count -ne 5 -or $catalogue.product_invariant_revision -ne 'opure.setting-definition-invariants/1') {
    throw 'FND-039 evidence is incomplete.'
}

Write-Host ''
Write-Host 'FND-039 Setting Definition verification passed.' -ForegroundColor Green
