#requires -Version 7.2

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [Parameter()]
    [ValidateRange(3000, 60000)]
    [int] $DesktopCloseAfterMilliseconds = 6000,

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

& (Join-Path $PSScriptRoot 'verify-founder-gate-a-readiness.ps1')

if (-not $SkipBuild) {
    & (Join-Path $PSScriptRoot 'restore.ps1') -Locked
    & (Join-Path $PSScriptRoot 'build.ps1') `
        -Configuration $Configuration `
        -BuildChannel Development
}

$configurationFolder = $Configuration.ToLowerInvariant()
$bootstrapExecutable = Join-Path $repositoryRoot "artifacts\bin\Opure.Bootstrap.Windows\$configurationFolder\Opure.Bootstrap.Windows.exe"
if (-not (Test-Path -LiteralPath $bootstrapExecutable -PathType Leaf)) {
    throw "Bootstrap executable was not produced: $bootstrapExecutable"
}

$fixtureSource = Join-Path $repositoryRoot 'eng\fixtures\founder-gate-a'
$runIdentity = [Guid]::NewGuid().ToString('N')
$temporaryBase = [IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetTempPath()) 'Opure\FounderGateA'))
$workRoot = [IO.Path]::GetFullPath((Join-Path $temporaryBase $runIdentity))
$isolatedLocalApplicationData = Join-Path $workRoot 'LocalApplicationData'
$fixtureRoot = Join-Path $workRoot 'fixture'
$standardOutputPath = Join-Path $workRoot 'bootstrap.stdout.jsonl'
$standardErrorPath = Join-Path $workRoot 'bootstrap.stderr.txt'
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

    $standardOutputTask = $process.StandardOutput.ReadToEndAsync()
    $standardErrorTask = $process.StandardError.ReadToEndAsync()
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
    $standardOutput = $standardOutputTask.GetAwaiter().GetResult()
    $standardError = $standardErrorTask.GetAwaiter().GetResult()
    [IO.File]::WriteAllText($standardOutputPath, $standardOutput, [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText($standardErrorPath, $standardError, [Text.UTF8Encoding]::new($false))

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
            ready = @(1, 2, 3)
            partial = @(4, 5)
            pending = @(6..32)
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
    Write-Host 'Checklist steps 1-3 are ready; steps 4-5 remain partial and steps 6-32 remain pending.' -ForegroundColor Yellow
}
finally {
    if ($null -ne $process) {
        $process.Dispose()
    }

    $resolvedWorkRoot = [IO.Path]::GetFullPath($workRoot)
    $requiredPrefix = $temporaryBase.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if ($resolvedWorkRoot.StartsWith($requiredPrefix, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedWorkRoot)) {
        Remove-Item -LiteralPath $resolvedWorkRoot -Recurse -Force
    }
}
