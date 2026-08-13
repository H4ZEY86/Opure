#requires -Version 7.2

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [Parameter()]
    [ValidateRange(10000, 60000)]
    [int] $DesktopCloseAfterMilliseconds = 20000,

    [Parameter()]
    [switch] $SkipBuild
)

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
Assert-OpureBuildEnvironment -RepositoryRoot $repositoryRoot

if (-not $IsWindows) {
    throw 'GATE-A-001 currently requires a supported Windows environment.'
}

foreach ($requiredCommand in @('git', 'Get-CimInstance', 'Get-NetTCPConnection', 'Get-NetUDPEndpoint')) {
    if (-not (Get-Command $requiredCommand -ErrorAction SilentlyContinue)) {
        throw "GATE-A-001 requires $requiredCommand for bounded launch evidence."
    }
}

function Get-FixtureHash {
    param([Parameter(Mandatory)][string] $Root)

    $files = Get-ChildItem -LiteralPath $Root -Recurse -Force -File |
        Where-Object {
            $relativePath = [IO.Path]::GetRelativePath($Root, $_.FullName)
            -not ($relativePath -eq '.git' -or $relativePath.StartsWith(".git$([IO.Path]::DirectorySeparatorChar)", [StringComparison]::Ordinal))
        } |
        Sort-Object { [IO.Path]::GetRelativePath($Root, $_.FullName) }

    $canonicalLines = foreach ($file in $files) {
        $relativePath = [IO.Path]::GetRelativePath($Root, $file.FullName).Replace('\', '/')
        $fileHash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        "$relativePath|$($file.Length)|$fileHash"
    }

    $bytes = [Text.Encoding]::UTF8.GetBytes(($canonicalLines -join "`n"))
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
}

function Get-DescendantProcesses {
    param([Parameter(Mandatory)][int] $RootProcessId)

    $allProcesses = @(Get-CimInstance Win32_Process -ErrorAction Stop |
        Select-Object ProcessId, ParentProcessId, Name)
    $knownParents = [Collections.Generic.HashSet[uint32]]::new()
    [void]$knownParents.Add([uint32]$RootProcessId)
    $descendants = [Collections.Generic.List[object]]::new()

    do {
        $added = $false
        foreach ($candidate in $allProcesses) {
            if ($knownParents.Contains([uint32]$candidate.ParentProcessId) -and
                -not $knownParents.Contains([uint32]$candidate.ProcessId)) {
                [void]$knownParents.Add([uint32]$candidate.ProcessId)
                $descendants.Add($candidate)
                $added = $true
            }
        }
    } while ($added)

    return $descendants
}

function Invoke-CapturedProcess {
    param(
        [Parameter(Mandatory)][string] $Executable,
        [Parameter(Mandatory)][string[]] $Arguments,
        [Parameter(Mandatory)][Collections.Generic.Dictionary[string, string]] $Environment,
        [Parameter()][AllowNull()][string] $StandardInput
    )

    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $Executable
    $startInfo.WorkingDirectory = $repositoryRoot
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.RedirectStandardInput = $null -ne $StandardInput
    foreach ($argument in $Arguments) {
        [void]$startInfo.ArgumentList.Add($argument)
    }
    foreach ($entry in $Environment.GetEnumerator()) {
        $startInfo.Environment[$entry.Key] = $entry.Value
    }

    $child = [Diagnostics.Process]::new()
    $child.StartInfo = $startInfo
    try {
        if (-not $child.Start()) {
            throw "Gate A probe did not start: $Executable"
        }
        $outputTask = $child.StandardOutput.ReadToEndAsync()
        $errorTask = $child.StandardError.ReadToEndAsync()
        if ($null -ne $StandardInput) {
            $child.StandardInput.WriteLine($StandardInput)
            $child.StandardInput.Close()
        }
        $child.WaitForExit()
        return [pscustomobject]@{
            ExitCode = $child.ExitCode
            StandardOutput = $outputTask.GetAwaiter().GetResult()
            StandardError = $errorTask.GetAwaiter().GetResult()
        }
    }
    finally {
        $child.Dispose()
    }
}

function Get-ConfigurationStageEvidence {
    param(
        [Parameter(Mandatory)][string] $Output,
        [Parameter(Mandatory)][string] $Stage
    )

    $escapedStage = [regex]::Escape($Stage)
    $block = [regex]::Match(
        $Output,
        "(?ms)^Configuration stage:\s*$escapedStage\s*`r?`n(?<body>.*?)(?=^Configuration stage:|^Invalid session:|\z)").Groups['body'].Value
    if ([string]::IsNullOrWhiteSpace($block)) {
        throw "The Gate A configuration stage '$Stage' was not reported."
    }

    return [ordered]@{
        productDefaultsRevision = [int][regex]::Match(
            $block,
            'Product Defaults:\s*revision\s*(?<value>\d+)').Groups['value'].Value
        productDefaultsSha256 = [regex]::Match(
            $block,
            'Product Defaults:.*?SHA-256\s*(?<value>[0-9a-f]{64})').Groups['value'].Value
        userProfileId = [regex]::Match(
            $block,
            'User Base Profile:\s*(?<value>\S+)').Groups['value'].Value
        userProfileRevision = [int][regex]::Match(
            $block,
            'User Base Profile:.*?revision\s*(?<value>\d+)').Groups['value'].Value
        projectContentSha256 = [regex]::Match(
            $block,
            'Project settings content SHA-256:\s*(?<value>[0-9a-f]{64})').Groups['value'].Value
        snapshotId = [regex]::Match(
            $block,
            'Effective Configuration:\s*(?<value>[0-9a-f]{32})').Groups['value'].Value
        configurationGeneration = [long][regex]::Match(
            $block,
            'Effective Configuration:.*?generation\s*(?<value>\d+)').Groups['value'].Value
        workspaceGeneration = [long][regex]::Match(
            $block,
            'Configuration Workspace generation:\s*(?<value>\d+)').Groups['value'].Value
        latestObservedWorkspaceGeneration = [long][regex]::Match(
            $block,
            'Latest observed Workspace generation:\s*(?<value>\d+)').Groups['value'].Value
        latestValidWorkspaceGeneration = [long][regex]::Match(
            $block,
            'Latest valid Workspace generation:\s*(?<value>\d+)').Groups['value'].Value
        sourceError = [regex]::Match(
            $block,
            'Configuration source error:\s*(?<value>\S+)').Groups['value'].Value
        provenanceEntryCount = @([regex]::Matches(
            $block,
            '(?m)^\s+Configuration key\s+')).Count
    }
}

function Invoke-ProjectEvidenceProbe {
    param(
        [Parameter(Mandatory)][string] $CliExecutable,
        [Parameter(Mandatory)][string] $FixtureRoot,
        [Parameter(Mandatory)][pscustomobject] $Session
    )

    $environment = [Collections.Generic.Dictionary[string, string]]::new(
        [StringComparer]::Ordinal)
    $environment['OPURE_RUNTIME_PIPE_NAME'] = [string]$Session.OPURE_IPC_PIPE
    $environment['OPURE_RUNTIME_BOOT_ID'] = [string]$Session.OPURE_RUNTIME_BOOT_ID
    $environment['OPURE_BOOTSTRAP_SESSION_ID'] = [string]$Session.OPURE_BOOTSTRAP_SESSION_ID
    $environment['OPURE_BOOTSTRAP_SESSION_SECRET'] = [string]$Session.OPURE_BOOTSTRAP_SESSION_SECRET
    $environment['OPURE_GATE_A_TEST_MODE'] = '1'

    $opened = Invoke-CapturedProcess `
        -Executable $CliExecutable `
        -Arguments @('gate-a', 'probe', '--channel', 'Development', '--path-stdin') `
        -Environment $environment `
        -StandardInput $FixtureRoot
    if ($opened.ExitCode -ne 0 -or
        -not [string]::IsNullOrWhiteSpace($opened.StandardError)) {
        throw "The authenticated Project-open CLI probe failed: $($opened.StandardError)"
    }

    $status = [regex]::Match(
        $opened.StandardOutput,
        'Runtime Status:\s*(?<value>\S+)').Groups['value'].Value
    $readiness = [regex]::Match(
        $opened.StandardOutput,
        'Readiness:\s*(?<value>\S+)').Groups['value'].Value
    $mode = [regex]::Match(
        $opened.StandardOutput,
        'Mode:\s*(?<value>\S+)').Groups['value'].Value
    $bootId = [regex]::Match(
        $opened.StandardOutput,
        'Boot ID:\s*(?<value>[0-9a-f]{32})').Groups['value'].Value
    $serviceCount = [int][regex]::Match(
        $opened.StandardOutput,
        'Services:\s*(?<value>\d+)').Groups['value'].Value
    $services = @([regex]::Matches(
        $opened.StandardOutput,
        '(?m)^\s+-\s+(?<id>[a-z0-9.-]+):\s*(?<state>\S+)\s*$') |
        ForEach-Object {
            [ordered]@{
                serviceId = $_.Groups['id'].Value
                state = $_.Groups['state'].Value
            }
        })
    $invalidDenied = $opened.StandardOutput -match '(?m)^Invalid session:\s*Denied\s*$'
    if ($bootId -ne [string]$Session.OPURE_RUNTIME_BOOT_ID -or
        $serviceCount -lt 1 -or $services.Count -ne $serviceCount -or
        -not $invalidDenied) {
        throw 'The combined Gate A probe did not prove health, service projection and invalid-session denial.'
    }

    $projectId = [regex]::Match(
        $opened.StandardOutput,
        'Project ID:\s*(?<value>[0-9a-f]{32})').Groups['value'].Value
    $disposition = [regex]::Match(
        $opened.StandardOutput,
        'Disposition:\s*(?<value>\S+)').Groups['value'].Value
    $lifecycle = [regex]::Match(
        $opened.StandardOutput,
        'Lifecycle:\s*(?<value>\S+)').Groups['value'].Value
    $volumeClass = [regex]::Match(
        $opened.StandardOutput,
        'Root volume class:\s*(?<value>\S+)').Groups['value'].Value
    $snapshotState = [regex]::Match(
        $opened.StandardOutput,
        'Initial Workspace Snapshot:\s*(?<value>\S+)').Groups['value'].Value
    $workspaceGeneration = [long][regex]::Match(
        $opened.StandardOutput,
        'Workspace generation:\s*(?<value>\d+)').Groups['value'].Value
    $workspaceGenerationSha256 = [regex]::Match(
        $opened.StandardOutput,
        'Workspace generation SHA-256:\s*(?<value>[0-9a-f]{64})').Groups['value'].Value
    if ([string]::IsNullOrWhiteSpace($projectId) -or
        $lifecycle -ne 'Open' -or
        $snapshotState -ne 'Ready' -or
        $workspaceGeneration -lt 1 -or
        [string]::IsNullOrWhiteSpace($workspaceGenerationSha256)) {
        throw 'The Project-open CLI response did not contain a safe open Project projection.'
    }

    $escapedProjectId = [regex]::Escape($projectId)
    $projectListMatch = [regex]::Match(
        $opened.StandardOutput,
        "(?m)^$escapedProjectId\s+Repository:\s*(?<repository>.+?)\s+Availability:\s*(?<availability>\S+)\s*$")
    $repositoryClass = $projectListMatch.Groups['repository'].Value
    $availability = $projectListMatch.Groups['availability'].Value
    if (-not [string]::Equals(
            $repositoryClass,
            'git repository',
            [StringComparison]::OrdinalIgnoreCase) -or
        [string]::IsNullOrWhiteSpace($availability)) {
        throw 'The opened fixture was not projected as an observed Git repository.'
    }

    $configuration = [ordered]@{
        initial = Get-ConfigurationStageEvidence -Output $opened.StandardOutput -Stage 'Initial'
        validChange = Get-ConfigurationStageEvidence -Output $opened.StandardOutput -Stage 'ValidChange'
        invalidSource = Get-ConfigurationStageEvidence -Output $opened.StandardOutput -Stage 'InvalidSource'
        repaired = Get-ConfigurationStageEvidence -Output $opened.StandardOutput -Stage 'Repaired'
    }
    if ($configuration.initial.productDefaultsRevision -lt 1 -or
        [string]::IsNullOrWhiteSpace($configuration.initial.productDefaultsSha256) -or
        $configuration.initial.userProfileId -ne 'user.base' -or
        $configuration.initial.userProfileRevision -lt 1 -or
        $configuration.initial.configurationGeneration -lt 1 -or
        $configuration.initial.workspaceGeneration -ne $workspaceGeneration -or
        $configuration.initial.provenanceEntryCount -lt 1 -or
        $configuration.initial.sourceError -ne 'None' -or
        $configuration.validChange.configurationGeneration -le $configuration.initial.configurationGeneration -or
        $configuration.validChange.workspaceGeneration -le $configuration.initial.workspaceGeneration -or
        [string]::IsNullOrWhiteSpace($configuration.validChange.projectContentSha256) -or
        $configuration.invalidSource.configurationGeneration -ne $configuration.validChange.configurationGeneration -or
        $configuration.invalidSource.latestObservedWorkspaceGeneration -le $configuration.validChange.workspaceGeneration -or
        $configuration.invalidSource.latestValidWorkspaceGeneration -ne $configuration.validChange.workspaceGeneration -or
        $configuration.invalidSource.sourceError -ne 'Present' -or
        $configuration.repaired.configurationGeneration -le $configuration.validChange.configurationGeneration -or
        $configuration.repaired.latestValidWorkspaceGeneration -le $configuration.validChange.workspaceGeneration -or
        $configuration.repaired.sourceError -ne 'None') {
        throw 'The Gate A configuration evidence did not prove valid, invalid, last-known-good and repaired states.'
    }

    return [ordered]@{
        health = [ordered]@{
            authenticated = $true
            serverProofVerified = $true
            invalidSessionDenied = $true
            overallHealth = $status
            readiness = $readiness
            runtimeMode = $mode
            serviceCount = $serviceCount
            services = $services
        }
        project = [ordered]@{
            authenticated = $true
            projectId = $projectId
            disposition = $disposition
            lifecycleState = $lifecycle
            rootIdentityVerified = $true
            rootVolumeClass = $volumeClass
            repositoryClass = $repositoryClass
            availability = $availability
            initialWorkspaceSnapshotState = $snapshotState
            workspaceGeneration = $workspaceGeneration
            workspaceGenerationSha256 = $workspaceGenerationSha256
        }
        configuration = $configuration
    }
}

& (Join-Path $PSScriptRoot 'verify-founder-gate-a-readiness.ps1')

if (-not $SkipBuild) {
    & (Join-Path $PSScriptRoot 'restore.ps1') -Locked
    & (Join-Path $PSScriptRoot 'build.ps1') `
        -Configuration $Configuration `
        -BuildChannel Development
}

$configurationFolder = $Configuration.ToLowerInvariant()
$bootstrapExecutable = Join-Path $repositoryRoot "artifacts\bin\Opure.Bootstrap.Windows\$configurationFolder\Opure.Bootstrap.Windows.exe"
$cliExecutable = Join-Path $repositoryRoot "artifacts\bin\Opure.Cli\$configurationFolder\Opure.Cli.exe"
if (-not (Test-Path -LiteralPath $bootstrapExecutable -PathType Leaf)) {
    throw "Bootstrap executable was not produced: $bootstrapExecutable"
}
if (-not (Test-Path -LiteralPath $cliExecutable -PathType Leaf)) {
    throw "CLI executable was not produced: $cliExecutable"
}

$fixtureSource = Join-Path $repositoryRoot 'eng\fixtures\founder-gate-a'
$runIdentity = [Guid]::NewGuid().ToString('N')
$temporaryBase = [IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetTempPath()) 'Opure\FounderGateA'))
$workRoot = [IO.Path]::GetFullPath((Join-Path $temporaryBase $runIdentity))
$isolatedLocalApplicationData = Join-Path $workRoot 'LocalApplicationData'
$fixtureRoot = Join-Path $workRoot 'fixture'
$receiptDirectory = Join-Path $repositoryRoot 'artifacts\evidence\founder-gate-a'
$receiptPath = Join-Path $receiptDirectory 'launch-receipt.json'
$process = $null

try {
    [IO.Directory]::CreateDirectory($isolatedLocalApplicationData) | Out-Null
    Copy-Item -LiteralPath $fixtureSource -Destination $fixtureRoot -Recurse

    $fixtureHashBefore = Get-FixtureHash -Root $fixtureRoot
    & git -C $fixtureRoot -c core.autocrlf=false init --quiet
    if ($LASTEXITCODE -ne 0) {
        throw 'The disposable Gate A fixture could not be initialised as a Git repository.'
    }

    & git -C $fixtureRoot -c core.autocrlf=false add --all
    if ($LASTEXITCODE -ne 0) {
        throw 'The disposable Gate A fixture could not be staged.'
    }

    & git -C $fixtureRoot -c core.autocrlf=false -c user.name=H4ZEY86 -c user.email=development@opure.local commit --quiet -m 'Gate A fixture baseline'
    if ($LASTEXITCODE -ne 0) {
        throw 'The disposable Gate A fixture baseline could not be committed.'
    }

    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $bootstrapExecutable
    $startInfo.WorkingDirectory = $repositoryRoot
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.Environment['OPURE_BOOTSTRAP_TEST_MODE'] = '1'

    foreach ($argument in @(
        '--layout', 'Development',
        '--configuration', $Configuration,
        '--channel', 'Development',
        '--desktop-close-after-ms', [string]$DesktopCloseAfterMilliseconds,
        '--test-local-app-data-root', $isolatedLocalApplicationData)) {
        [void]$startInfo.ArgumentList.Add($argument)
    }

    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    if (-not $process.Start()) {
        throw 'GATE-A-001 Bootstrap did not start.'
    }

    $standardErrorTask = $process.StandardError.ReadToEndAsync()
    $safeOutputLines = [Collections.Generic.List[string]]::new()
    $session = $null
    $sessionDeadline = [DateTimeOffset]::UtcNow.AddSeconds(15)
    while ($null -eq $session -and [DateTimeOffset]::UtcNow -lt $sessionDeadline) {
        $line = $process.StandardOutput.ReadLineAsync().WaitAsync(
            [TimeSpan]::FromSeconds(15)).GetAwaiter().GetResult()
        if ($null -eq $line) {
            break
        }
        $value = $line | ConvertFrom-Json -ErrorAction Stop
        if ($value.PSObject.Properties['kind']?.Value -eq 'ipc.session') {
            $session = $value
        }
        else {
            $safeOutputLines.Add($line)
        }
    }
    if ($null -eq $session) {
        $safeEventNames = @($safeOutputLines | ForEach-Object {
            ($_ | ConvertFrom-Json -ErrorAction Stop).PSObject.Properties['event']?.Value
        } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        $safeFailure = @($safeOutputLines | ForEach-Object {
            $_ | ConvertFrom-Json -ErrorAction Stop
        } | Where-Object { $_.event -eq 'bootstrap.failure' } | Select-Object -Last 1)
        $failureSummary = if ($safeFailure.Count -eq 1) {
            "$($safeFailure[0].category): $($safeFailure[0].message) ($($safeFailure[0].exceptionType))"
        }
        else {
            'none'
        }
        throw "Bootstrap did not emit the bounded in-memory Gate A session hand-off. Safe events: $($safeEventNames -join ', '). Failure: $failureSummary."
    }

    Write-Host 'Gate A session hand-off acquired in memory.' -ForegroundColor DarkGray
    $controlPlaneEvidence = Invoke-ProjectEvidenceProbe `
        -CliExecutable $cliExecutable `
        -FixtureRoot $fixtureRoot `
        -Session $session
    $healthEvidence = $controlPlaneEvidence.health
    $projectEvidence = $controlPlaneEvidence.project
    Write-Host 'Gate A authenticated health, Project and denial probes passed.' -ForegroundColor DarkGray
    $standardOutputTask = $process.StandardOutput.ReadToEndAsync()
    $observedProcesses = @{}
    $networkEndpointCount = 0

    while (-not $process.HasExited) {
        $descendants = @(Get-DescendantProcesses -RootProcessId $process.Id)
        $descendantProcessIds = @($descendants | ForEach-Object { [uint32]$_.ProcessId })
        foreach ($child in $descendants) {
            $observedProcesses[[int]$child.ProcessId] = [string]$child.Name
        }

        $networkEndpointCount += @(
            Get-NetTCPConnection -ErrorAction Stop |
                Where-Object { [uint32]$_.OwningProcess -in $descendantProcessIds }
        ).Count
        $networkEndpointCount += @(
            Get-NetUDPEndpoint -ErrorAction Stop |
                Where-Object { [uint32]$_.OwningProcess -in $descendantProcessIds }
        ).Count

        Start-Sleep -Milliseconds 200
    }

    $process.WaitForExit()
    $remainingOutput = $standardOutputTask.GetAwaiter().GetResult()
    if (-not [string]::IsNullOrWhiteSpace($remainingOutput)) {
        foreach ($line in $remainingOutput -split "`r?`n") {
            if (-not [string]::IsNullOrWhiteSpace($line)) {
                $safeOutputLines.Add($line)
            }
        }
    }
    $standardOutput = $safeOutputLines -join "`n"
    $standardError = $standardErrorTask.GetAwaiter().GetResult()

    if ($process.ExitCode -ne 0) {
        throw "GATE-A-001 Bootstrap exited with code $($process.ExitCode)."
    }

    if (-not [string]::IsNullOrWhiteSpace($standardError)) {
        throw 'GATE-A-001 Bootstrap or a child process wrote to stderr.'
    }

    $events = @($standardOutput -split "`r?`n" |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { $_ | ConvertFrom-Json -ErrorAction Stop })
    $childStarts = @($events | Where-Object {
        $_.PSObject.Properties['event']?.Value -eq 'bootstrap.child.started'
    })
    $runtimeStarts = @($childStarts | Where-Object {
        $_.PSObject.Properties['processClass']?.Value -eq 'runtime'
    })
    $desktopStarts = @($childStarts | Where-Object {
        $_.PSObject.Properties['processClass']?.Value -eq 'desktop'
    })
    $runtimeReady = @($events | Where-Object {
        $_.PSObject.Properties['event']?.Value -eq 'bootstrap.child.ready' -and
        $_.PSObject.Properties['processClass']?.Value -eq 'runtime'
    })
    $failures = @($events | Where-Object {
        $_.PSObject.Properties['event']?.Value -eq 'bootstrap.failure'
    })

    if ($runtimeStarts.Count -ne 1 -or $desktopStarts.Count -ne 1 -or
        $runtimeReady.Count -ne 1 -or $failures.Count -ne 0) {
        throw 'GATE-A-001 did not observe one verified Runtime, one verified Desktop and one Runtime readiness signal.'
    }

    $unexpectedEventProcesses = @($childStarts | Where-Object {
        $_.executableName -notin @('Opure.Runtime.exe', 'Opure.Desktop.exe')
    })
    $unexpectedObservedProcesses = @($observedProcesses.GetEnumerator() | Where-Object {
        $_.Value -notin @('Opure.Runtime.exe', 'Opure.Desktop.exe', 'conhost.exe')
    })
    $prohibitedNamePattern = '(?i)(ollama|nemotron|plugin|mcp|agent|skill)'
    $prohibitedProcesses = @($observedProcesses.GetEnumerator() | Where-Object {
        $_.Value -match $prohibitedNamePattern
    })

    if ($unexpectedEventProcesses.Count -ne 0 -or
        $unexpectedObservedProcesses.Count -ne 0 -or
        $prohibitedProcesses.Count -ne 0) {
        $unexpectedNames = @(
            $unexpectedEventProcesses | ForEach-Object { [string]$_.executableName }
            $unexpectedObservedProcesses | ForEach-Object { [string]$_.Value }
        ) | Sort-Object -Unique
        throw "GATE-A-001 observed a process outside the trusted Runtime and Desktop launch boundary: $($unexpectedNames -join ', ')"
    }

    if ($networkEndpointCount -ne 0) {
        throw 'GATE-A-001 observed a TCP or UDP endpoint owned by a child process.'
    }

    $fixtureHashAfter = Get-FixtureHash -Root $fixtureRoot
    if ($fixtureHashAfter -ne $fixtureHashBefore) {
        throw 'GATE-A-001 modified the disposable fixture repository.'
    }

    $payload = [ordered]@{
        schemaVersion = 1
        ticket = 'GATE-A-001'
        scope = 'bounded-development-channel-launch-prerequisite'
        result = 'Passed'
        fullDemonstrationComplete = $false
        channel = 'Development'
        configuration = $Configuration
        isolatedDataRoot = $true
        fixtureSha256 = $fixtureHashAfter
        bootstrapSha256 = (Get-FileHash -LiteralPath $bootstrapExecutable -Algorithm SHA256).Hash.ToLowerInvariant()
        runtime = [ordered]@{
            processId = [int]$runtimeStarts[0].processId
            instanceId = [string]$runtimeStarts[0].instanceId
            bootId = [string]$runtimeReady[0].bootId
            executableSha256 = [string]$runtimeStarts[0].executableSha256
        }
        desktop = [ordered]@{
            processId = [int]$desktopStarts[0].processId
            instanceId = [string]$desktopStarts[0].instanceId
            executableSha256 = [string]$desktopStarts[0].executableSha256
        }
        ipcAndHealth = $healthEvidence
        project = $projectEvidence
        negativeAssertions = [ordered]@{
            aiRuntimeSpawned = $false
            pluginProcessSpawned = $false
            mcpProcessSpawned = $false
            agentOrSkillHostSpawned = $false
            networkEndpointOwned = $false
            linuxStylePathUsed = $false
            fixtureModified = $false
            standardErrorObserved = $false
            bootstrapFailureObserved = $false
        }
        allowedPlatformInfrastructure = @('conhost.exe')
        checklist = [ordered]@{
            ready = @(1..19)
            partial = @()
            pending = @(20..32)
        }
    }

    $payloadJson = $payload | ConvertTo-Json -Depth 8 -Compress
    $payloadBytes = [Text.Encoding]::UTF8.GetBytes($payloadJson)
    $payloadHash = [Convert]::ToHexString(
        [Security.Cryptography.SHA256]::HashData($payloadBytes)).ToLowerInvariant()
    $receipt = [ordered]@{
        algorithm = 'SHA-256'
        payloadSha256 = $payloadHash
        payload = $payload
    }

    [IO.Directory]::CreateDirectory($receiptDirectory) | Out-Null
    [IO.File]::WriteAllText(
        $receiptPath,
        (($receipt | ConvertTo-Json -Depth 10) + "`n"),
        [Text.UTF8Encoding]::new($false))

    Write-Host "GATE-A-001 bounded launch prerequisite passed: $receiptPath" -ForegroundColor Green
    Write-Host 'Checklist steps 1-19 are ready; steps 20-32 remain pending.' -ForegroundColor Yellow
}
finally {
    if ($null -ne $process) {
        if (-not $process.HasExited) {
            $process.Kill($true)
            $process.WaitForExit()
        }
        $process.Dispose()
    }

    $resolvedWorkRoot = [IO.Path]::GetFullPath($workRoot)
    $requiredPrefix = $temporaryBase.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if ($resolvedWorkRoot.StartsWith($requiredPrefix, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedWorkRoot)) {
        Remove-Item -LiteralPath $resolvedWorkRoot -Recurse -Force
    }
}
