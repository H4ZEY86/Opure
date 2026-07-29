#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

$desktopTests = Join-Path $repositoryRoot `
    'tests\Desktop\Opure.Desktop.Tests\Opure.Desktop.Tests.csproj'
$architectureTests = Join-Path $repositoryRoot `
    'tests\Architecture\Opure.ArchitectureTests\Opure.ArchitectureTests.csproj'
$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M4'
$flowPath = Join-Path $evidenceRoot 'folder-picker-flow.json'
$transferPath = Join-Path $evidenceRoot 'folder-picker-capability-transfer.md'
$accessibilityPath = Join-Path $evidenceRoot 'folder-picker-accessibility.json'

Write-Host ''
Write-Host '==> Verify FND-027 Trusted Folder Picker Adapter' `
    -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration Release `
    -BuildChannel Development

& dotnet test $desktopTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter-class `
    'Opure.Desktop.Tests.TrustedFolderPickerAdapterTests' `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-027 folder-picker coordinator tests failed.'
}

& dotnet test $desktopTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter-method `
    'Opure.Desktop.Tests.DesktopHeadlessTests.Project_folder_picker_is_keyboard_focusable_and_labelled' `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-027 folder-picker accessibility test failed.'
}

& dotnet test $architectureTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter-class `
    'Opure.ArchitectureTests.DesktopExecutableBoundaryTests' `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-027 Desktop architecture tests failed.'
}

foreach ($path in @($flowPath, $transferPath, $accessibilityPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "FND-027 evidence is missing: $path"
    }
}

$flow = [System.IO.File]::ReadAllText($flowPath) | ConvertFrom-Json
$accessibility = [System.IO.File]::ReadAllText($accessibilityPath) |
    ConvertFrom-Json

if ($flow.schema -ne 'opure.folder-picker-flow/1' -or
    $flow.result -ne 'Passed' -or
    $flow.pathAcquisitions -ne 1 -or
    $flow.cancelledStateChange -ne $false -or
    $flow.desktopRetainsCapability -ne $false -or
    $flow.localTransfer -ne 'VerifiedReferenceOnly' -or
    $flow.networkPolicy -ne 'RejectedBeforeAccess' -or
    $flow.reparsePolicy -ne 'Rejected' -or
    $flow.deletedSelection -ne 'Rejected') {
    throw 'FND-027 folder-picker flow evidence is incomplete.'
}

if ($accessibility.schema -ne 'opure.folder-picker-accessibility/1' -or
    $accessibility.result -ne 'Passed' -or
    $accessibility.keyboardFocusable -ne $true -or
    $accessibility.tabIndex -ne 5 -or
    $accessibility.automationId -ne 'SelectProjectFolder' -or
    $accessibility.accessKey -ne 'Alt+S' -or
    $accessibility.colourOnlyStatus -ne $false) {
    throw 'FND-027 accessibility evidence is incomplete.'
}

foreach ($path in @($flowPath, $transferPath, $accessibilityPath)) {
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
            throw "FND-027 evidence contains prohibited material: $token"
        }
    }

    if ($content -match '[A-Za-z]:[\\/]' -or
        $content -match '\\\\[^\\/\r\n]+[\\/]') {
        throw "FND-027 evidence contains an absolute or UNC path: $path"
    }
}

Write-Host ''
Write-Host 'FND-027 Trusted Folder Picker verification passed.' `
    -ForegroundColor Green
