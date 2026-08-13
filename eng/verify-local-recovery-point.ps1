#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot
$recoveryTests = Join-Path $repositoryRoot 'tests\Recovery\Opure.Recovery.Service.Tests\Opure.Recovery.Service.Tests.csproj'
$desktopTests = Join-Path $repositoryRoot 'tests\Desktop\Opure.Desktop.Tests\Opure.Desktop.Tests.csproj'
$endToEndTests = Join-Path $repositoryRoot 'tests\EndToEnd\Opure.EndToEnd.Tests\Opure.EndToEnd.Tests.csproj'
$runtimePath = Join-Path $repositoryRoot 'src\Runtime\Opure.Runtime\RuntimeApplication.cs'
$viewPath = Join-Path $repositoryRoot 'src\Desktop\Opure.Desktop\RecoveryPointView.axaml'
$servicePath = Join-Path $repositoryRoot 'src\Recovery\Opure.Recovery.Service\LocalRecoveryPointService.cs'
$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M6'
$evidencePaths = @(
    (Join-Path $evidenceRoot 'recovery-point-manifest.json'),
    (Join-Path $evidenceRoot 'recovery-point-structural-verification.json'),
    (Join-Path $evidenceRoot 'recovery-point-disposable-restore.json'),
    (Join-Path $evidenceRoot 'backup-health-ui.json'))

Write-Host ''
Write-Host '==> Verify FND-060 Local Recovery Point view' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') -Configuration Release -BuildChannel Development

& dotnet test $recoveryTests --configuration Release --no-build --no-restore `
    --filter-class 'Opure.Recovery.Service.Tests.LocalRecoveryPointServiceTests' `
    --filter-class 'Opure.Recovery.Service.Tests.RecoveryPointVerifierTests' --timeout 60s
if ($LASTEXITCODE -ne 0) { throw 'FND-060 recovery service acceptance tests failed.' }

& dotnet test $desktopTests --configuration Release --no-build --no-restore `
    --filter-class 'Opure.Desktop.Tests.RecoveryPointViewHeadlessTests' --timeout 60s
if ($LASTEXITCODE -ne 0) { throw 'FND-060 Desktop accessibility test failed.' }

& dotnet test $endToEndTests --configuration Release --no-build --no-restore `
    --filter-class 'Opure.EndToEnd.Tests.RecoveryPointCliPipelineTests' --timeout 60s
if ($LASTEXITCODE -ne 0) { throw 'FND-060 authenticated CLI pipeline test failed.' }

$runtime = [System.IO.File]::ReadAllText($runtimePath)
$view = [System.IO.File]::ReadAllText($viewPath)
$service = [System.IO.File]::ReadAllText($servicePath)
foreach ($required in @('trustEvidenceService.BackupAdapter', 'projectService.BackupAdapter')) {
    if (-not $runtime.Contains($required, [StringComparison]::Ordinal)) {
        throw "FND-060 Runtime is missing required owner composition: $required"
    }
}
foreach ($required in @('Same-device recovery only', 'Structural verification', 'Creation and verification receipts', 'AutomationProperties.Name')) {
    if (-not $view.Contains($required, [StringComparison]::OrdinalIgnoreCase)) {
        throw "FND-060 Desktop view is missing required projection text: $required"
    }
}
foreach ($required in @('backup.recovery-point-created', 'backup.verification-completed', 'FileMode.CreateNew', 'CommitMarkerFileName')) {
    if (-not $service.Contains($required, [StringComparison]::Ordinal)) {
        throw "FND-060 service is missing required behaviour: $required"
    }
}

foreach ($path in $evidencePaths) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "FND-060 evidence is missing: $path" }
    $content = [System.IO.File]::ReadAllText($path)
    foreach ($token in @('C:\Users\', 'ghp_', 'github_pat_', 'Authorization:', 'Bearer ', 'Password=')) {
        if ($content.Contains($token, [StringComparison]::OrdinalIgnoreCase)) {
            throw "FND-060 evidence contains prohibited material: $token"
        }
    }
    if ($content -match '[A-Za-z]:[\\/]' -or $content -match '\\\\[^\\/\r\n]+[\\/]') {
        throw "FND-060 evidence contains an absolute or UNC path: $path"
    }
}

$manifest = [System.IO.File]::ReadAllText($evidencePaths[0]) | ConvertFrom-Json
$structural = [System.IO.File]::ReadAllText($evidencePaths[1]) | ConvertFrom-Json
$restore = [System.IO.File]::ReadAllText($evidencePaths[2]) | ConvertFrom-Json
$ui = [System.IO.File]::ReadAllText($evidencePaths[3]) | ConvertFrom-Json
if ($manifest.result -ne 'Passed' -or $manifest.scopeClass -ne 'same-device' -or `
    $manifest.requiredRuntimeOwnersComplete -ne $true -or $manifest.commitMarkerWrittenLast -ne $true -or `
    $manifest.receipts.Count -ne 2 -or $structural.result -ne 'Passed' -or `
    $structural.manifestTamperRejected -ne $true -or $structural.missingCommitRejected -ne $true -or `
    $restore.result -ne 'Passed' -or $restore.activeRootUnchanged -ne $true -or `
    $restore.disposableRootDeleted -ne $true -or $ui.result -ne 'Passed' -or `
    $ui.sameDeviceWarningProminent -ne $true -or $ui.keyboardReachable -ne $true -or `
    $ui.receiptsVisible -ne $true) {
    throw 'FND-060 evidence is incomplete.'
}

Write-Host ''
Write-Host 'FND-060 Local Recovery Point verification passed.' -ForegroundColor Green
