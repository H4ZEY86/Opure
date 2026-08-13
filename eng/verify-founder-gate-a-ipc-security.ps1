#requires -Version 7.2

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release'
)

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot
if (-not $IsWindows) { throw 'GATE-A-003 IPC security evidence requires Windows 11.' }

$matrixPath = Join-Path $repositoryRoot `
    'eng\evidence\milestones\M6\gate-a-003-ipc-security-matrix.json'
$matrix = [IO.File]::ReadAllText($matrixPath) | ConvertFrom-Json
if ($matrix.ticket -ne 'GATE-A-003' -or
    $matrix.status -ne 'Ready' -or
    $matrix.result -ne 'Passed' -or
    $matrix.scenarioCount -ne 12 -or
    $matrix.scenarios.Count -ne 12 -or
    $matrix.admissionPolicy.maximumConcurrentConnections -ne 32) {
    throw 'GATE-A-003 IPC security matrix is incomplete.'
}

$securityTestPath = Join-Path $repositoryRoot `
    'tests\Ipc\Opure.Ipc.NamedPipes.Windows.Tests\IpcSecuritySuiteTests.cs'
$transportTestPath = Join-Path $repositoryRoot `
    'tests\Ipc\Opure.Ipc.NamedPipes.Windows.Tests\NamedPipeRuntimeHealthTransportTests.cs'
$testContent = [IO.File]::ReadAllText($securityTestPath) +
    [IO.File]::ReadAllText($transportTestPath)
for ($index = 0; $index -lt $matrix.scenarios.Count; $index++) {
    $scenario = $matrix.scenarios[$index]
    if ($scenario.id -ne ($index + 1) -or
        -not $testContent.Contains(
            [string]$scenario.proofMember,
            [StringComparison]::Ordinal)) {
        throw "GATE-A-003 scenario proof is missing: $($scenario.scenario)"
    }
}

foreach ($relativePath in $matrix.evidence) {
    $path = Join-Path $repositoryRoot ([string]$relativePath).Replace('/', '\')
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "GATE-A-003 evidence is missing: $path"
    }
}

$serverContent = [IO.File]::ReadAllText((Join-Path $repositoryRoot `
    'src\Ipc\Opure.Ipc.NamedPipes.Windows\NamedPipeGatewayServer.cs'))
$policyContent = [IO.File]::ReadAllText((Join-Path $repositoryRoot `
    'src\Ipc\Opure.Ipc.Abstractions\RuntimeHealthTransport.cs'))
foreach ($required in @(
    'MaxConcurrentConnections',
    'MaximumConcurrentConnections = 32',
    'MaxReceiveMessageSize',
    'ListenNamedPipe')) {
    if (-not ($serverContent + $policyContent).Contains(
            $required,
            [StringComparison]::Ordinal)) {
        throw "GATE-A-003 bounded server policy is missing: $required"
    }
}

Write-Host ''
Write-Host '==> Verify GATE-A-003 Release baseline' -ForegroundColor Cyan
& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration $Configuration `
    -BuildChannel Development

$testProject = Join-Path $repositoryRoot `
    'tests\Ipc\Opure.Ipc.NamedPipes.Windows.Tests\Opure.Ipc.NamedPipes.Windows.Tests.csproj'
& dotnet test $testProject `
    --configuration $Configuration `
    --no-build `
    --no-restore `
    --filter-class 'Opure.Ipc.NamedPipes.Windows.Tests.IpcSecuritySuiteTests' `
    --filter-class 'Opure.Ipc.NamedPipes.Windows.Tests.NamedPipeRuntimeHealthTransportTests' `
    --timeout 120s
if ($LASTEXITCODE -ne 0) { throw 'GATE-A-003 IPC security tests failed.' }

& (Join-Path $PSScriptRoot 'verify-health-transport.ps1')

$networkPath = Join-Path $repositoryRoot `
    'eng\evidence\milestones\M2\runtime-health-network-listeners.json'
$network = [IO.File]::ReadAllText($networkPath) | ConvertFrom-Json
if ($network.result -ne 'Passed' -or
    $network.tcpListenerCount -ne 0 -or
    $network.udpEndpointCount -ne 0) {
    throw 'GATE-A-003 live no-listener evidence failed.'
}

$receiptRoot = Join-Path $repositoryRoot 'artifacts\evidence\founder-gate-a'
New-Item -ItemType Directory -Force -Path $receiptRoot | Out-Null
$payload = [ordered]@{
    schema = 'opure.gate-a-003-ipc-security/1'
    ticket = 'GATE-A-003'
    result = 'Passed'
    scenarioCount = 12
    maximumConcurrentConnections = 32
    tcpListenerCount = 0
    udpEndpointCount = 0
    fullReleaseVerificationPassed = $true
    matrixSha256 = (Get-FileHash -LiteralPath $matrixPath -Algorithm SHA256).Hash.ToLowerInvariant()
    sessionMaterialPersisted = $false
}
$payloadJson = $payload | ConvertTo-Json -Compress
$receipt = [ordered]@{
    algorithm = 'SHA-256'
    payloadSha256 = [Convert]::ToHexString(
        [Security.Cryptography.SHA256]::HashData(
            [Text.Encoding]::UTF8.GetBytes($payloadJson))).ToLowerInvariant()
    payload = $payload
}
$receiptPath = Join-Path $receiptRoot 'gate-a-003-ipc-security-receipt.json'
[IO.File]::WriteAllText(
    $receiptPath,
    ($receipt | ConvertTo-Json -Depth 5),
    [Text.UTF8Encoding]::new($false))
$receiptContent = [IO.File]::ReadAllText($receiptPath)
foreach ($prohibited in @(
    'C:\Users\', 'SESSION_SECRET', 'ghp_', 'github_pat_',
    'Authorization:', 'Bearer ', 'Password=')) {
    if ($receiptContent.Contains($prohibited, [StringComparison]::OrdinalIgnoreCase)) {
        throw "GATE-A-003 receipt contains prohibited material: $prohibited"
    }
}

Write-Host "GATE-A-003 IPC security passed: $receiptPath" -ForegroundColor Green
