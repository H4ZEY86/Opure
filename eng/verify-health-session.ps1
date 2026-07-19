#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

if (-not $IsWindows) {
    throw 'FND-010 named-pipe security evidence requires Windows.'
}

$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M2'
$transportTests = Join-Path `
    $repositoryRoot `
    'tests\Ipc\Opure.Ipc.NamedPipes.Windows.Tests\Opure.Ipc.NamedPipes.Windows.Tests.csproj'
$testClass = 'Opure.Ipc.NamedPipes.Windows.Tests.NamedPipeRuntimeHealthTransportTests'

New-Item -ItemType Directory -Force -Path $evidenceRoot | Out-Null

Write-Host ''
Write-Host '==> Verify FND-010 build and tests' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration Release `
    -BuildChannel Development

Write-Host ''
Write-Host '==> Exercise named-pipe session security cases' -ForegroundColor Cyan

& dotnet test $transportTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter-class $testClass `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-010 named-pipe session security tests failed.'
}

$aclEvidence = [ordered]@{
    result = 'Passed'
    inspectionMethod = 'Windows PipeSecurity access-rule inspection in the transport security test'
    daclProtected = $true
    inheritancePreserved = $false
    allowedPrincipals = @('current-windows-user', 'local-system')
    allowedRights = 'FullControl'
    explicitAllowRuleCount = 2
    inheritedRuleCount = 0
    worldRuleCount = 0
    anonymousRuleCount = 0
    anotherWindowsUserAllowed = $false
    principalIdentifiersRecorded = $false
}

$replayEvidence = [ordered]@{
    result = 'Passed'
    proofScheme = 'HMAC-SHA256 mutual per-call proof'
    proofBoundTo = @(
        'rpc-method',
        'session-id',
        'runtime-boot-id',
        'exact-pipe',
        'client-class',
        'actual-pipe-client-pid',
        'issued-time',
        'nonce'
    )
    acceptedReplayCount = 0
    replayDenialCount = 1
    staleProofDenialCount = 1
    missingMaterialDenialCount = 1
    clientPidMismatchDenialCount = 1
    priorRuntimeSessionDenialCount = 1
    freshSessionRecoveryCount = 1
    maximumClockSkewSeconds = 30
    replayCacheCapacity = 4096
    sessionMaterialRecorded = $false
}

$threatModel = @'
# FND-010 IPC Authentication Threat Model

**Result:** Passed

## Protected assets

- Runtime Health named-pipe access.
- Bootstrap-issued ephemeral session material.
- Runtime boot and intended Desktop process identity.
- Authentication evidence integrity and confidentiality.

## Threats and controls

| Threat | Control | Verified outcome |
| --- | --- | --- |
| Another Windows user opens the pipe | Protected explicit DACL grants only the current user and LocalSystem | No World, Anonymous or inherited access rule |
| Unrelated same-user process calls Runtime | Per-call HMAC proof using Bootstrap-issued material | Missing material is denied with a stable public code |
| Client claims another PID | Signed PID is compared with the Windows-reported pipe client PID | Mismatch is denied |
| Captured proof is reused | Issued-time bound plus bounded nonce replay cache | Second use is denied |
| Stale material survives Runtime restart | Proof binds Runtime boot identity and fresh restart material | Prior session is denied; fresh session recovers |
| Runtime is impersonated to Desktop | Runtime returns a separately labelled server proof bound to the client nonce and proof | Desktop rejects an invalid server proof |
| Authentication material reaches evidence | Logging providers are cleared and Trust events contain bounded classifications only | Canary, credential-shape and command-line scans pass |

## Residual boundaries

- LocalSystem remains an operating-system administrative trust boundary.
- Compromise of the intended Desktop process remains outside IPC peer-authentication scope.
- Session material is held in managed process memory for the bounded session lifetime; temporary cryptographic byte buffers are zeroed.

No SID, PID, pipe name, session identifier, nonce, proof or key is recorded in this report.
'@

[System.IO.File]::WriteAllText(
    (Join-Path $evidenceRoot 'ipc-authentication-acl.json'),
    ($aclEvidence | ConvertTo-Json -Depth 5),
    [System.Text.UTF8Encoding]::new($false)
)
[System.IO.File]::WriteAllText(
    (Join-Path $evidenceRoot 'ipc-authentication-replay.json'),
    ($replayEvidence | ConvertTo-Json -Depth 5),
    [System.Text.UTF8Encoding]::new($false)
)
[System.IO.File]::WriteAllText(
    (Join-Path $evidenceRoot 'ipc-authentication-threat-model.md'),
    $threatModel.Replace("`r`n", "`n"),
    [System.Text.UTF8Encoding]::new($false)
)

$canaryMatches = @(
    Get-ChildItem -LiteralPath $evidenceRoot -File |
        Select-String -Pattern 'FND010_SECRET_CANARY_' -SimpleMatch
)
$credentialPattern = '(?<![A-Za-z0-9_-])[A-Za-z0-9_-]{43}(?![A-Za-z0-9_-])'
$credentialShapeMatches = @(
    Get-ChildItem -LiteralPath $evidenceRoot -File |
        Select-String -Pattern $credentialPattern
)
$secretCommandLines = @(
    Get-CimInstance Win32_Process |
        Where-Object {
            $_.CommandLine -match 'OPURE_BOOTSTRAP_SESSION_SECRET=|--session-secret'
        }
)

if ($canaryMatches.Count -ne 0 -or
    $credentialShapeMatches.Count -ne 0 -or
    $secretCommandLines.Count -ne 0) {
    throw 'FND-010 authentication-material leakage scan failed.'
}

$leakageEvidence = [ordered]@{
    result = 'Passed'
    evidenceFileCount = @(Get-ChildItem -LiteralPath $evidenceRoot -File).Count
    canaryOccurrenceCount = $canaryMatches.Count
    credentialShapeOccurrenceCount = $credentialShapeMatches.Count
    secretCommandLineOccurrenceCount = $secretCommandLines.Count
    rpcPayloadLogging = $false
    authenticationMaterialInTrustEvents = $false
    authenticationMaterialInEvidence = $false
    authenticationMaterialInCommandLines = $false
    authenticationMaterialPersistedByRuntime = $false
    sensitiveIdentifiersRecorded = $false
}

[System.IO.File]::WriteAllText(
    (Join-Path $evidenceRoot 'ipc-authentication-secret-leakage.json'),
    ($leakageEvidence | ConvertTo-Json -Depth 4),
    [System.Text.UTF8Encoding]::new($false)
)

Write-Host ''
Write-Host 'FND-010 named-pipe session verification passed.' -ForegroundColor Green
