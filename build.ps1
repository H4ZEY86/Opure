#requires -Version 7.2

[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('restore', 'build', 'test', 'verify', 'package', 'policy', 'version', 'version-policy', 'runtime', 'runtime-policy', 'desktop', 'desktop-policy', 'bootstrap', 'bootstrap-policy', 'supervisor-policy', 'health-contract-policy', 'health-transport-policy', 'health-session-policy', 'service-registry-policy', 'service-lifecycle-policy', 'runtime-health-ui-policy', 'persistence-policy', 'migration-policy', 'outbox-policy', 'inbox-policy', 'structured-logging-policy', 'trace-propagation-policy', 'redaction-policy', 'evidence-type-policy', 'evidence-record-policy', 'trust-database-policy', 'trust-ingestion-policy', 'trust-query-policy', 'path-reference-policy', 'folder-picker-policy', 'project-database-policy', 'open-project-policy', 'project-trust-policy', 'repository-policy', 'project-list-ui-policy', 'workspace-contract-policy', 'workspace-inventory-policy', 'workspace-hashing-policy', 'workspace-generation-policy', 'workspace-reconciliation-policy', 'workspace-snapshot-receipt-policy', 'setting-definition-policy')]
    [string] $Target = 'verify',

    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',

    [Parameter()]
    [ValidateSet('Development', 'Preview', 'Stable')]
    [string] $BuildChannel = 'Development',

    [Parameter()]
    [ValidateRange(0, 60000)]
    [int] $RuntimeDurationMilliseconds = 0,

    [Parameter()]
    [ValidateRange(0, 60000)]
    [int] $DesktopDurationMilliseconds = 0,

    [Parameter()]
    [ValidateRange(0, 60000)]
    [int] $BootstrapDurationMilliseconds = 0
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

switch ($Target) {
    'restore' {
        & (Join-Path $PSScriptRoot 'eng\restore.ps1') -Locked
    }

    'build' {
        & (Join-Path $PSScriptRoot 'eng\restore.ps1') -Locked
        & (Join-Path $PSScriptRoot 'eng\build.ps1') `
            -Configuration $Configuration `
            -BuildChannel $BuildChannel
    }

    'test' {
        & (Join-Path $PSScriptRoot 'eng\verify.ps1') `
            -Configuration $Configuration `
            -BuildChannel $BuildChannel
    }

    'verify' {
        & (Join-Path $PSScriptRoot 'eng\verify.ps1') `
            -Configuration $Configuration `
            -BuildChannel $BuildChannel
    }

    'package' {
        & (Join-Path $PSScriptRoot 'eng\package.ps1') `
            -Configuration $Configuration `
            -BuildChannel $BuildChannel
    }

    'policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-build-policy.ps1')
    }

    'version' {
        & (Join-Path $PSScriptRoot 'eng\version.ps1')
    }

    'version-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-versioning.ps1')
    }

    'runtime' {
        & (Join-Path $PSScriptRoot 'eng\run-runtime.ps1') `
            -ShutdownAfterMilliseconds $RuntimeDurationMilliseconds
    }

    'runtime-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-runtime.ps1')
    }

    'desktop' {
        & (Join-Path $PSScriptRoot 'eng\run-desktop.ps1') `
            -CloseAfterMilliseconds $DesktopDurationMilliseconds
    }

    'desktop-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-desktop.ps1')
    }

    'bootstrap' {
        & (Join-Path $PSScriptRoot 'eng\run-bootstrap.ps1') `
            -Configuration $Configuration `
            -Channel $BuildChannel `
            -DesktopCloseAfterMilliseconds $BootstrapDurationMilliseconds
    }

    'bootstrap-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-bootstrap.ps1')
    }

    'supervisor-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-supervisor.ps1')
    }

    'health-contract-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-health-contract.ps1')
    }

    'health-transport-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-health-transport.ps1')
    }

    'health-session-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-health-session.ps1')
    }

    'service-registry-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-service-registry.ps1')
    }

    'service-lifecycle-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-service-lifecycle.ps1')
    }

    'runtime-health-ui-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-runtime-health-ui.ps1')
    }

    'persistence-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-sqlite-persistence.ps1')
    }

    'migration-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-sqlite-migrations.ps1')
    }

    'outbox-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-sqlite-outbox.ps1')
    }

    'inbox-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-sqlite-inbox.ps1')
    }

    'structured-logging-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-structured-logging.ps1')
    }

    'trace-propagation-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-trace-propagation.ps1')
    }

    'redaction-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-redaction.ps1')
    }

    'evidence-type-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-evidence-types.ps1')
    }

    'evidence-record-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-evidence-records.ps1')
    }

    'trust-database-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-trust-database.ps1')
    }

    'trust-ingestion-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-trust-ingestion.ps1')
    }

    'trust-query-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-trust-query.ps1')
    }

    'path-reference-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-path-reference.ps1')
    }

    'folder-picker-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-folder-picker.ps1')
    }

    'project-database-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-project-database.ps1')
    }

    'open-project-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-open-project.ps1')
    }

    'project-trust-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-project-open-trust.ps1')
    }

    'repository-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-repository-identity.ps1')
    }

    'project-list-ui-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-project-list-ui.ps1')
    }

    'workspace-contract-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-workspace-service-contract.ps1')
    }

    'workspace-inventory-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-workspace-file-inventory.ps1')
    }

    'workspace-hashing-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-workspace-file-hashing.ps1')
    }

    'workspace-generation-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-workspace-generation.ps1')
    }

    'workspace-reconciliation-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-workspace-reconciliation.ps1')
    }

    'workspace-snapshot-receipt-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-workspace-snapshot-receipt.ps1')
    }

    'setting-definition-policy' {
        & (Join-Path $PSScriptRoot 'eng\verify-setting-definitions.ps1')
    }
}
