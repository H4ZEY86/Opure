# FND-029 Open Project sequence

This evidence describes the implemented owner and recovery boundaries. Project
Service is authoritative. Desktop and Desktop Gateway retain no database object
or verified-root capability after the bounded command returns.

```mermaid
sequenceDiagram
    actor Developer
    participant Desktop
    participant Gateway as Desktop Gateway
    participant Runtime
    participant Project as Project Service
    participant Workspace as Workspace Snapshot boundary
    participant Database as projects.db

    Developer->>Desktop: Select one folder
    Desktop->>Desktop: Acquire opaque verified-root reference
    Desktop->>Gateway: Transfer reference once
    Gateway->>Runtime: Authenticated OpenProject(path display + identity claim)
    Runtime->>Project: Dispatch bounded command
    Project->>Project: Reopen root and compare filesystem identity
    Project->>Database: Commit registration + Opening atomically
    Project->>Workspace: Request initial Workspace Snapshot
    Workspace-->>Project: Requested or Ready
    Project->>Database: Commit Open
    Project-->>Gateway: Stable project summary
    Gateway-->>Desktop: Safe Open receipt

    alt failure after Opening commit
        Project->>Database: Commit RecoveryRequired
    else Runtime restart while Opening
        Project->>Project: Revalidate root and reconcile
        Project->>Database: Commit Open or RecoveryRequired
    end
```

The initial Workspace Snapshot requester is a future-stable boundary. In this
foundation slice it records `Requested`; later Workspace Service work may
replace the adapter without changing the Open Project wire contract.
