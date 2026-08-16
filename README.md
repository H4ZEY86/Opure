# Opure

> Developer Respect. Local Intelligence. Complete Control.

Opure is a local-first, developer-controlled software engineering platform for
Windows 11. It helps developers design, understand, modify, build, test and
operate software without surrendering authority to autonomous behaviour.

The repository contains the trusted Runtime, Avalonia Desktop, Bootstrap,
`opure` CLI, service contracts, persistence libraries, verification tooling and
the specifications that govern their behaviour.

## Current status — v0.2.0-gate-b (Phase 7: Controlled Mutation)

Founder Gate B has been formally accepted. Phase 7 (Controlled Mutation, tickets
CM-001 through CM-016) is complete and verified at **950 passing tests with zero
warnings and zero errors**.

### Phase 7 summary

| Ticket | Description |
|---|---|
| CM-001 | Versioned `ExactUtf8PatchProposal` contract (BOM-free, 4 MB ceiling, immutable) |
| CM-002 | Patch State Store and Transition Machine |
| CM-003 | Patch Precondition Verifier (source hash / size guards) |
| CM-004 | Staged Write with Atomic Swap |
| CM-005 | Postcondition Verifier (result hash confirmation) |
| CM-006 | Patch Approval Identity |
| CM-007 | Patch Execution Pipeline |
| CM-008 | Patch Trust Receipt Emission |
| CM-009 | Last-Known-Good Patch Rollback |
| CM-010 | Patch Recovery Orchestrator |
| CM-011 | Unified Diff Parser |
| CM-012 | Typed Read-Only Tool Templates and Effect Intent Validator |
| CM-013 | Restricted Command Worker with Windows Job Objects |
| CM-014 | Bounded Stream Drainer and FND-020 Redaction Pipeline |
| CM-015 | Compound Cryptographic Approvals and Authoritative Exit Receipts |
| CM-016 | Controlled Mutation Adversarial Suite and Founder Gate B |

### Production baseline (v0.2.0-gate-b)

- **950 passing tests**, zero warnings, zero errors in Release configuration
- **Windows Job Object OS containment**: `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE`
  and memory tier limits enforced at the kernel level via `SetInformationJobObject`
- **FND-020 secret redaction**: ANSI-scrubbing and canary-pattern detection
  applied at the stream boundary; zero raw bytes cross the IPC surface
- **Zero implicit shell invocation**: `cmd`, `pwsh`, `bash` and all shell
  identifiers are rejected by the `ToolTemplateValidator` at proposal time;
  verified by architecture tests
- **Compound cryptographic approvals**: each command execution approval is a
  deterministic SHA-256 of `[TemplateHash + CanonicalArguments + WorkspaceSnapshotId]`;
  stale or tampered approvals are denied
- **Ephemeral staging**: STDOUT/STDERR flushed as content-hashed blobs to
  `.opure-staging`; only metadata and content hashes are persisted in SQLite
- **Zero AI inference**, zero network listeners, zero arbitrary shell authority
  — proven by the `FounderGateBSecurityTests` architecture test suite

### Gate B performance baseline (RTX-HAZE, 32 processors, Windows 11)

| Measurement | Value |
|---|---|
| Bootstrap to IPC session readiness | 3 161 ms |

These measurements are recorded in
[`eng/evidence/milestones/M6/GATE-B-metrics.md`](eng/evidence/milestones/M6/GATE-B-metrics.md).

## Build and verify

The repository requires the .NET SDK pinned by `global.json`.

```powershell
pwsh ./build.ps1 verify -Configuration Release
```

Useful bounded launch commands:

```powershell
pwsh ./build.ps1 runtime -RuntimeDurationMilliseconds 500
pwsh ./build.ps1 desktop -DesktopDurationMilliseconds 1500
pwsh ./build.ps1 bootstrap -Configuration Release -BootstrapDurationMilliseconds 3000
```

Run the Local Recovery Point acceptance verifier with:

```powershell
pwsh ./build.ps1 local-recovery-point-policy
```

## Documentation

- [Product documentation](docs/README.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Foundation roadmap](specs/ROADMAP-001-foundation-implementation-sequence.md)
- [Foundation backlog](specs/BACKLOG-001-foundation-first-12-weeks.md)
- [Architecture decisions](adr/)
- [Engineering commands](eng/README.md)

The public website has deliberately been removed from this repository. It will
be rebuilt as a complete web application closer to public launch.

## Maintainers and attribution

Opure is maintained and attributed exclusively to:

- **H4ZEY86**
- **DevMediaDesign**

Automated tools are not recognised as contributors or authors. See
[CONTRIBUTORS.md](CONTRIBUTORS.md) for the repository attribution policy.

## Repository

[github.com/H4ZEY86/Opure](https://github.com/H4ZEY86/Opure)
