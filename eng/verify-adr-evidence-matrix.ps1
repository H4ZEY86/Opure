#requires -Version 7.2

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'common\Opure.Build.Common.ps1')

$repositoryRoot = Get-OpureRepositoryRoot
$matrixPath = Join-Path $repositoryRoot 'eng\evidence\milestones\M6\adr-evidence-matrix.md'
if (-not (Test-Path -LiteralPath $matrixPath -PathType Leaf)) {
    throw 'GATE-A-009 ADR evidence matrix is missing.'
}
$matrix = Get-Content -LiteralPath $matrixPath -Raw

$rows = @(
    @{ Id = 'ADR-0001'; Decision = 'Retain'; Commits = @('1457916', '917cbcb'); Tests = @('tests\Architecture\Opure.ArchitectureTests\Opure.ArchitectureTests.csproj') },
    @{ Id = 'ADR-0002'; Decision = 'Retain'; Commits = @('5cc8c53', '1e2a030', '917cbcb'); Tests = @('tests\Desktop\Opure.Desktop.Tests\Opure.Desktop.Tests.csproj') },
    @{ Id = 'ADR-0003'; Decision = 'Retain'; Commits = @('4dc3df4', '3b092ac', 'f29aee3'); Tests = @('tests\Bootstrap\Opure.Bootstrap.Windows.Tests\Opure.Bootstrap.Windows.Tests.csproj', 'tests\Runtime\Opure.Runtime.Tests\Opure.Runtime.Tests.csproj') },
    @{ Id = 'ADR-0004'; Decision = 'Retain'; Commits = @('d9360dc', '3b51243', 'e97b9d2'); Tests = @('tests\Ipc\Opure.Ipc.NamedPipes.Windows.Tests\Opure.Ipc.NamedPipes.Windows.Tests.csproj') },
    @{ Id = 'ADR-0005'; Decision = 'Retain'; Commits = @('b96ff34', '1bd9e9d', '9b70845'); Tests = @('tests\Persistence\Opure.Persistence.Sqlite.Tests\Opure.Persistence.Sqlite.Tests.csproj') },
    @{ Id = 'ADR-0006'; Decision = 'Retain'; Commits = @('c9ed13b', 'a3d6b83', '3359ec2'); Tests = @('tests\Observability\Opure.Observability.Tests\Opure.Observability.Tests.csproj') },
    @{ Id = 'ADR-0008'; Decision = 'Retain'; Commits = @('8090fc9', 'b63d284', '917cbcb'); Tests = @('Opure.slnx') },
    @{ Id = 'ADR-0009'; Decision = 'Retain'; Commits = @('8395a44', 'bb17831', '1b8db10'); Tests = @('tests\Filesystem\Opure.Filesystem.Windows.Tests\Opure.Filesystem.Windows.Tests.csproj') },
    @{ Id = 'ADR-0010'; Decision = 'Retain'; Commits = @('1457916', '8090fc9', '917cbcb'); Tests = @('tests\Architecture\Opure.ArchitectureTests\Opure.ArchitectureTests.csproj') },
    @{ Id = 'ADR-0011'; Decision = 'Amend'; Commits = @('8090fc9', 'df8ece5', '917cbcb'); Tests = @('build.ps1') },
    @{ Id = 'ADR-0012'; Decision = 'Retain'; Commits = @('ef2bd25', '96753ff', '917cbcb'); Tests = @('tests\Architecture\Opure.ArchitectureTests\Opure.ArchitectureTests.csproj') },
    @{ Id = 'ADR-0026'; Decision = 'Retain'; Commits = @('0cf0c98', '8185155', 'c28d80b', '5d1b66f'); Tests = @('tests\Configuration\Opure.Configuration.Tests\Opure.Configuration.Tests.csproj') },
    @{ Id = 'ADR-0027'; Decision = 'Retain'; Commits = @('6114042', '8c8df96', '12cabc7', '917cbcb'); Tests = @('tests\Trust\Opure.TrustEvidence.Sqlite.Tests\Opure.TrustEvidence.Sqlite.Tests.csproj') },
    @{ Id = 'ADR-0028'; Decision = 'Retain'; Commits = @('ec9795d', '9b70845', '85da9d5', '917cbcb'); Tests = @('tests\Recovery\Opure.Recovery.Service.Tests\Opure.Recovery.Service.Tests.csproj') }
)

if ($rows.Count -ne 14) {
    throw 'GATE-A-009 must review exactly the required minimum ADR set.'
}

foreach ($row in $rows) {
    $heading = "### $($row.Id)"
    $start = $matrix.IndexOf($heading, [StringComparison]::Ordinal)
    if ($start -lt 0) {
        throw "GATE-A-009 is missing $($row.Id)."
    }
    $next = $matrix.IndexOf('### ADR-', $start + $heading.Length, [StringComparison]::Ordinal)
    $section = if ($next -lt 0) { $matrix.Substring($start) } else { $matrix.Substring($start, $next - $start) }

    foreach ($label in @(
        'Current ADR status: Proposed',
        "Gate A disposition: **$($row.Decision)",
        'Implementation commits:',
        'Executable tests:',
        'Performance evidence:',
        'Remaining limitations:',
        'Proposed status change:',
        'Supersession:')) {
        if (-not $section.Contains($label, [StringComparison]::Ordinal)) {
            throw "$($row.Id) is missing required review field: $label"
        }
    }

    foreach ($commit in $row.Commits) {
        if (-not $section.Contains("``$commit``", [StringComparison]::Ordinal)) {
            throw "$($row.Id) does not link implementation commit $commit."
        }
        & git -C $repositoryRoot cat-file -e "$commit^{commit}"
        if ($LASTEXITCODE -ne 0) {
            throw "$($row.Id) links an unresolved implementation commit: $commit"
        }
    }
    foreach ($test in $row.Tests) {
        if (-not (Test-Path -LiteralPath (Join-Path $repositoryRoot $test))) {
            throw "$($row.Id) links a missing test or verifier path: $test"
        }
    }
}

foreach ($explicit in @(
    'Avalonia: **Retain for Gate A**',
    'Local IPC: **Retain authenticated gRPC over Windows named pipes**',
    'Persistence backup primitive: **Retain SQLite Online Backup**',
    'Service grouping: **Retain one trusted Runtime process',
    'Hosted workflows: **Amend ADR-0011**')) {
    if (-not $matrix.Contains($explicit, [StringComparison]::Ordinal)) {
        throw "GATE-A-009 explicit architecture decision is missing: $explicit"
    }
}

if (-not $matrix.Contains('No reviewed ADR is promoted to Accepted', [StringComparison]::Ordinal) -or
    -not $matrix.Contains('Architecture follow-up backlog', [StringComparison]::Ordinal) -or
    -not $matrix.Contains('amended or superseded', [StringComparison]::Ordinal)) {
    throw 'GATE-A-009 status, follow-up or supersession review is incomplete.'
}

Write-Host 'GATE-A-009 ADR evidence matrix verification passed.' -ForegroundColor Green
