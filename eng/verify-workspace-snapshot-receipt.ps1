#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot
$sqliteTests = Join-Path $repositoryRoot 'tests\Workspace\Opure.Workspace.Sqlite.Tests\Opure.Workspace.Sqlite.Tests.csproj'
$serviceTests = Join-Path $repositoryRoot 'tests\Workspace\Opure.Workspace.Service.Tests\Opure.Workspace.Service.Tests.csproj'
$projectTests = Join-Path $repositoryRoot 'tests\Project\Opure.Project.Sqlite.Tests\Opure.Project.Sqlite.Tests.csproj'
$architectureTests = Join-Path $repositoryRoot 'tests\Architecture\Opure.ArchitectureTests\Opure.ArchitectureTests.csproj'
$cataloguePath = Join-Path $repositoryRoot 'src\Trust\Opure.TrustEvidence.Contracts\FoundationEvidenceTypeCatalogue.cs'
$storePath = Join-Path $repositoryRoot 'src\Workspace\Opure.Workspace.Sqlite\WorkspaceGenerationStore.cs'
$outboxPath = Join-Path $repositoryRoot 'src\Workspace\Opure.Workspace.Sqlite\WorkspaceTrustEvidenceOutbox.cs'
$deliveryPath = Join-Path $repositoryRoot 'src\Workspace\Opure.Workspace.Service\WorkspaceTrustEvidenceDelivery.cs'
$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M4'
$evidencePaths = @(
    (Join-Path $evidenceRoot 'workspace-snapshot-evidence-type.json'),
    (Join-Path $evidenceRoot 'workspace-snapshot-receipt-sample.json'),
    (Join-Path $evidenceRoot 'workspace-snapshot-transaction-report.json'),
    (Join-Path $evidenceRoot 'workspace-snapshot-recovery-report.json'))

Write-Host ''
Write-Host '==> Verify FND-038 Workspace Snapshot receipt' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') -Configuration Release -BuildChannel Development

foreach ($project in @($sqliteTests, $serviceTests, $projectTests)) {
    & dotnet test $project --configuration Release --no-build --no-restore --timeout 60s
    if ($LASTEXITCODE -ne 0) { throw "FND-038 acceptance tests failed: $project" }
}

& dotnet test $architectureTests --configuration Release --no-build --no-restore `
    --filter-class 'Opure.ArchitectureTests.WorkspaceServiceBoundaryTests' --timeout 60s
if ($LASTEXITCODE -ne 0) { throw 'FND-038 Workspace architecture tests failed.' }

$catalogue = [System.IO.File]::ReadAllText($cataloguePath)
$store = [System.IO.File]::ReadAllText($storePath)
$outbox = [System.IO.File]::ReadAllText($outboxPath)
$delivery = [System.IO.File]::ReadAllText($deliveryPath)
foreach ($required in @('workspace.snapshot-created', 'AuthoritativeDomainStateTransition', 'generation_sha256', 'repository_summary_sha256')) {
    if (-not $catalogue.Contains($required, [StringComparison]::Ordinal)) {
        throw "FND-038 Evidence Type is missing required authority: $required"
    }
}
foreach ($required in @('ActivateCurrent', 'WorkspaceTrustEvidenceOutbox.Enqueue', 'ExecuteTransaction')) {
    if (-not $store.Contains($required, [StringComparison]::Ordinal)) {
        throw "FND-038 commit path is missing transactional receipt behaviour: $required"
    }
}
foreach ($required in @('ProjectOpenEvidenceId', 'EvidenceRelationshipKind.CausedBy', 'WorkspaceDatabase.OwnerServiceId', 'CreateIngestionRequest')) {
    if (-not $outbox.Contains($required, [StringComparison]::Ordinal)) {
        throw "FND-038 outbox receipt is missing required binding: $required"
    }
}
foreach ($required in @('SqliteOutboxDispatcher', 'EvidenceIngestionDisposition.Duplicate', 'ReadBacklog')) {
    if (-not $delivery.Contains($required, [StringComparison]::Ordinal)) {
        throw "FND-038 delivery path is missing required recovery behaviour: $required"
    }
}

foreach ($path in $evidencePaths) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "FND-038 evidence is missing: $path" }
    $content = [System.IO.File]::ReadAllText($path)
    foreach ($token in @('C:\Users\', 'ghp_', 'github_pat_', 'Authorization:', 'Bearer ', 'Password=')) {
        if ($content.Contains($token, [StringComparison]::OrdinalIgnoreCase)) {
            throw "FND-038 evidence contains prohibited material: $token"
        }
    }
    if ($content -match '[A-Za-z]:[\\/]' -or $content -match '\\\\[^\\/\r\n]+[\\/]') {
        throw "FND-038 evidence contains an absolute or UNC path: $path"
    }
}

$type = [System.IO.File]::ReadAllText($evidencePaths[0]) | ConvertFrom-Json
$sample = [System.IO.File]::ReadAllText($evidencePaths[1]) | ConvertFrom-Json
$transaction = [System.IO.File]::ReadAllText($evidencePaths[2]) | ConvertFrom-Json
$recovery = [System.IO.File]::ReadAllText($evidencePaths[3]) | ConvertFrom-Json
$expectedSampleFields = @('ticket', 'result', 'evidenceTypeId', 'ownerServiceId', 'authorityClass', 'evidenceId', 'projectId', 'operationId', 'generation', 'generationSha256', 'entryCount', 'exclusionCount', 'repositorySummarySha256', 'relationshipKind', 'projectOpenEvidenceId')
$actualSampleFields = @($sample.PSObject.Properties.Name)
if (@(Compare-Object $expectedSampleFields $actualSampleFields).Count -ne 0) {
    throw 'FND-038 sample receipt contains an unexpected or missing field.'
}
if ($type.result -ne 'Passed' -or $type.ownerServiceId -ne 'opure.workspace' -or `
    $type.authorityClass -ne 'AuthoritativeDomainStateTransition' -or `
    $type.relationshipEligibility -notcontains 'CausedBy' -or `
    $sample.result -ne 'Passed' -or $sample.evidenceTypeId -ne 'workspace.snapshot-created' -or `
    $sample.generationSha256 -ne '1bda5987ab3295b47490c16dba1e3b0bb71f18de427f3658ca6ae487fc61aee5' -or `
    $sample.relationshipKind -ne 'CausedBy' -or `
    $transaction.result -ne 'Passed' -or $transaction.receiptCommitsWithCurrentPointer -ne $true -or `
    $transaction.receiptInsertFailureRollsBackGeneration -ne $true -or `
    $recovery.result -ne 'Passed' -or $recovery.pendingDeliveryResumesAfterRestart -ne $true -or `
    $recovery.missingProjectOpenTargetRetries -ne $true -or `
    $recovery.identicalDuplicateAppliesSecondDomainEffect -ne $false) {
    throw 'FND-038 evidence is incomplete.'
}

Write-Host ''
Write-Host 'FND-038 Workspace Snapshot receipt verification passed.' -ForegroundColor Green
