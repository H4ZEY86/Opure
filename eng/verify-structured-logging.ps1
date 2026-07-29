#requires -Version 7.2

[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

$observabilityTests = Join-Path `
    $repositoryRoot `
    'tests\Observability\Opure.Observability.Tests\Opure.Observability.Tests.csproj'
$architectureTests = Join-Path `
    $repositoryRoot `
    'tests\Architecture\Opure.ArchitectureTests\Opure.ArchitectureTests.csproj'
$runtimeTests = Join-Path `
    $repositoryRoot `
    'tests\Runtime\Opure.Runtime.Tests\Opure.Runtime.Tests.csproj'
$evidenceRoot = Join-Path $repositoryRoot 'eng\evidence\milestones\M3'
$schemaPath = Join-Path $evidenceRoot 'structured-log-schema.json'
$rotationPath = Join-Path $evidenceRoot 'structured-log-rotation.txt'
$queuePath = Join-Path $evidenceRoot 'structured-log-queue.txt'
$injectionPath = Join-Path `
    $evidenceRoot `
    'structured-log-injection-report.txt'
$verificationPath = Join-Path `
    $evidenceRoot `
    'structured-log-verification.md'

Write-Host ''
Write-Host '==> Verify FND-018 build and tests' -ForegroundColor Cyan

& (Join-Path $PSScriptRoot 'verify.ps1') `
    -Configuration Release `
    -BuildChannel Development

Write-Host ''
Write-Host '==> Exercise structured logging policy' -ForegroundColor Cyan

& dotnet test $observabilityTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-018 structured logging tests failed.'
}

& dotnet test $architectureTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter-class 'Opure.ArchitectureTests.ObservabilityBoundaryTests' `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-018 observability architecture tests failed.'
}

& dotnet test $runtimeTests `
    --configuration Release `
    --no-build `
    --no-restore `
    --filter-method `
    'Opure.Runtime.Tests.RuntimeHealthRequestHandlerTests.Operational_log_failure_is_visible_as_safe_degraded_health' `
    --timeout 60s

if ($LASTEXITCODE -ne 0) {
    throw 'FND-018 operational diagnostics health projection test failed.'
}

foreach ($evidencePath in @(
    $schemaPath,
    $rotationPath,
    $queuePath,
    $injectionPath,
    $verificationPath)) {
    if (-not (Test-Path -LiteralPath $evidencePath -PathType Leaf)) {
        throw "FND-018 evidence is missing: $evidencePath"
    }
}

$schema = [System.IO.File]::ReadAllText($schemaPath) |
    ConvertFrom-Json

if ($schema.schema -ne 'opure.structured-operational-log/1' -or
    $schema.format -ne 'JSON Lines' -or
    $schema.encoding -ne 'UTF-8 without BOM' -or
    $schema.authoritative -ne $false -or
    $schema.requiredFields.Count -ne 9 -or
    $schema.attributeTypes.Count -ne 4 -or
    $schema.eventDefinitionPolicy.callerSuppliedMessagesAllowed -ne $false -or
    $schema.eventDefinitionPolicy.maximumDefinitionMessageCharacters -ne 256 -or
    $schema.eventDefinitionPolicy.perEventAttributeAllowlistRequired -ne $true -or
    $schema.eventDefinitionPolicy.attributeKindMatchRequired -ne $true -or
    $schema.eventDefinitionPolicy.secretAndProhibitedDefinitionsAllowed -ne $false -or
    $schema.eventDefinitionPolicy.sanitisationBeforeQueue -ne $true -or
    $schema.defaultQueuePolicy.capacity -ne 1024 -or
    $schema.defaultQueuePolicy.completionTimeoutMilliseconds -ne 5000 -or
    $schema.defaultQueuePolicy.sinkDisposalTimeoutMilliseconds -ne 5000 -or
    [string]::Join(',', $schema.defaultQueuePolicy.severityPriority) -ne
        'critical,error,warning,information,debug,trace' -or
    $schema.defaultQueuePolicy.droppedSummaryEvent -ne
        'observability.queue.records-dropped' -or
    [string]::Join(',', $schema.defaultQueuePolicy.droppedSummaryAttributes) -ne
        'drop.count,queue.capacity' -or
    $schema.defaultPolicy.maximumActiveFileBytes -ne 8388608 -or
    $schema.defaultPolicy.maximumRetainedFileCount -ne 16 -or
    $schema.defaultPolicy.maximumRetainedAgeDays -ne 14 -or
    $schema.defaultPolicy.maximumMessageCharacters -ne 2048 -or
    $schema.defaultPolicy.maximumAttributeCount -ne 24 -or
    $schema.defaultPolicy.maximumAttributeNameCharacters -ne 64 -or
    $schema.defaultPolicy.maximumAttributeValueCharacters -ne 512 -or
    $schema.defaultPolicy.maximumEventBytes -ne 16384 -or
    $schema.defaultPolicy.maximumCleanupFileCount -ne 256) {
    throw 'FND-018 schema evidence does not match the implemented policy.'
}

$rotation = [System.IO.File]::ReadAllText($rotationPath)
$queue = [System.IO.File]::ReadAllText($queuePath)
$injection = [System.IO.File]::ReadAllText($injectionPath)

foreach ($requiredLine in @(
    'result=Passed',
    'rotationBoundary=Passed',
    'retainedFileCleanup=Passed',
    'retainedAgeCleanup=Passed',
    'partialFinalLineRecovery=Passed',
    'midWriteFailureQuarantineAndRecovery=Passed',
    'midWriteCancellationQuarantineAndRecovery=Passed',
    'transientSinkRecovery=Passed',
    'ownedDirectoryHandlePinning=Passed',
    'directoryReplacementRedirectDenied=Passed',
    'activeReparseTargetNotFollowed=Passed',
    'rotationValidatedHandleMutation=Passed',
    'retentionValidatedHandleDeletion=Passed',
    'externalHardLinkTargetUnchanged=Passed')) {
    if (-not $rotation.Contains($requiredLine, [StringComparison]::Ordinal)) {
        throw "FND-018 rotation evidence is incomplete: $requiredLine"
    }
}

foreach ($requiredLine in @(
    'schema=opure.structured-log-queue/1',
    'result=Passed',
    'defaultCapacity=1024',
    'nonBlockingProducer=Passed',
    'priorityOrder=critical,error,warning,information,debug,trace',
    'severityDropCounters=Passed',
    'dropSummaryAfterCapacityRecovery=Passed',
    'dropSummaryEvent=observability.queue.records-dropped',
    'dropSummaryAttributes=drop.count,queue.capacity',
    'dropSummaryPayloadAbsent=Passed',
    'publicDegradedHealthSnapshot=Passed',
    'runtimeHealthProjection=Passed',
    'defaultCompletionTimeoutMilliseconds=5000',
    'defaultSinkDisposalTimeoutMilliseconds=5000',
    'runtimeCompletionTimeoutMilliseconds=2000',
    'runtimeSinkDisposalTimeoutMilliseconds=2000',
    'boundedCompletion=Passed',
    'boundedSinkDisposal=Passed',
    'remainingEventsAccountedAsDropped=Passed')) {
    if (-not $queue.Contains($requiredLine, [StringComparison]::Ordinal)) {
        throw "FND-018 queue evidence is incomplete: $requiredLine"
    }
}

foreach ($requiredLine in @(
    'result=Passed',
    'singlePhysicalLine=Passed',
    'credentialCanaryAbsent=Passed',
    'headerClassInputAbsent=Passed',
    'absoluteProjectPathAbsent=Passed',
    'exceptionDataAbsent=Passed',
    'fixedDefinitionMessageOnly=Passed',
    'callerSuppliedMessageApiAbsent=Passed',
    'perEventAttributeAllowlist=Passed',
    'attributeKindEnforcement=Passed',
    'secretAndProhibitedSchemaRejected=Passed',
    'dangerousAllowlistedValueDropped=Passed',
    'preQueueSanitisation=Passed')) {
    if (-not $injection.Contains($requiredLine, [StringComparison]::Ordinal)) {
        throw "FND-018 injection evidence is incomplete: $requiredLine"
    }
}

foreach ($prohibitedToken in @(
    'C:\Users\',
    'ghp_',
    'github_pat_',
    'Authorization:',
    'Basic ',
    'Cookie:',
    'Set-Cookie:',
    'Bearer ',
    'sessionSecret',
    'clientSecret',
    'connectionString',
    'accessToken',
    'requestBody',
    'responseBody',
    'sourceContent',
    'raw-credential-canary-5831',
    'Namespace Leaked',
    '\\build-server\',
    'Password=',
    'privateKey')) {
    foreach ($evidencePath in @(
        $schemaPath,
        $rotationPath,
        $queuePath,
        $injectionPath,
        $verificationPath)) {
        $evidence = [System.IO.File]::ReadAllText($evidencePath)

        if ($evidence.Contains(
                $prohibitedToken,
                [StringComparison]::OrdinalIgnoreCase)) {
            throw "FND-018 evidence contains prohibited material: $prohibitedToken"
        }
    }
}

foreach ($evidencePath in @(
    $schemaPath,
    $rotationPath,
    $queuePath,
    $injectionPath,
    $verificationPath)) {
    $evidence = [System.IO.File]::ReadAllText($evidencePath)

    if ($evidence -match '[A-Za-z]:[\\/]' -or
        $evidence -match '\\\\[^\\/\r\n]+[\\/]') {
        throw "FND-018 evidence contains an absolute or UNC path: $evidencePath"
    }
}

Write-Host ''
Write-Host 'FND-018 structured logging verification passed.' `
    -ForegroundColor Green
