# v0.2.0-gate-b — Founder Gate B: Controlled Mutation & Zero-AI Security Baseline

**Date:** 2026-08-16  
**Test baseline:** 950 passing tests · zero warnings · zero errors (Release configuration)  
**Platform:** Windows 11, .NET 10.0.302

---

## What is Gate B?

Founder Gate B closes Phase 7 (Controlled Mutation). It proves that Opure can
safely propose, approve, preview, execute, bound, redact and receipt-emit
deterministic file patches and typed read-only tool invocations without any AI
inference, arbitrary shell access, or hidden network authority.

The gate is not a performance milestone. It is a security and determinism
milestone: every mutation path in the platform is now explicitly authorised,
cryptographically bound, OS-contained, and receipted.

---

## Phase 7 tickets (CM-001 — CM-016)

| Ticket | Assembly | Description |
|---|---|---|
| CM-001 | `Opure.Patch.Contracts` | `ExactUtf8PatchProposal` — immutable, BOM-free, 4 MB ceiling, deterministic SHA-256 |
| CM-002 | `Opure.Patch.Sqlite` | Patch State Store and Transition Machine |
| CM-003 | `Opure.Patch.Service` | Patch Precondition Verifier |
| CM-004 | `Opure.Patch.Service` | Staged Write with Atomic Swap (`*.opure-staging → target`) |
| CM-005 | `Opure.Patch.Service` | Postcondition Verifier |
| CM-006 | `Opure.Patch.Contracts` | Patch Approval Identity (`ExactUtf8PatchApproval`) |
| CM-007 | `Opure.Patch.Service` | Patch Execution Pipeline |
| CM-008 | `Opure.Patch.Service` | Patch Trust Receipt Emission |
| CM-009 | `Opure.Patch.Service` | Last-Known-Good Patch Rollback |
| CM-010 | `Opure.Patch.Service` | Recovery Orchestrator |
| CM-011 | `Opure.Patch.Service` | Unified Diff Parser (`UnifiedPatchProposal`, `UnifiedHunk`) |
| CM-012 | `Opure.Workspace.Contracts` | Typed Read-Only Tool Templates and `ToolTemplateValidator` |
| CM-013 | `Opure.Workspace.Execution` | `RestrictedCommandWorker` — Windows Job Objects, absolute-path resolution |
| CM-014 | `Opure.Workspace.Execution` | `BoundedStreamDrainer` — 1 MB per-stream ceiling, ANSI/canary scrubbing |
| CM-015 | `Opure.Workspace.Service` | `CommandExecutionPipeline` — compound cryptographic approvals and exit receipts |
| CM-016 | `Opure.EndToEnd.Tests` / `Opure.ArchitectureTests` | Controlled Mutation Adversarial Suite and Gate B verification |

---

## Security properties proven at Gate B

### Zero shell invocation
The `ToolTemplateValidator` explicitly rejects any template whose executable
identifier matches `cmd`, `cmd.exe`, `pwsh`, `powershell`, `bash`, `sh` or any
variant. Rejection happens at the contract boundary before the process table is
touched. The `FounderGateBSecurityTests` architecture tests prove this by
scanning every `.cs` source file under `src/` and asserting that no production
code contains a direct shell identifier as a string literal.

### OS-level containment (Windows Job Objects)
Every command executed through `RestrictedCommandWorker` is assigned to a Windows
Job Object before its streams are drained. The Job Object enforces:

- `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE` — the child process tree is terminated
  when the job handle is released, preventing orphan processes on crash or
  timeout.
- `JOB_OBJECT_LIMIT_JOB_MEMORY` — memory is capped by `ResourceClass` tier:
  - `Lightweight`: 256 MB
  - `Heavy`: 1 024 MB

These limits are applied via `SetInformationJobObject` (kernel API). There is no
user-space fallback.

### Bounded and redacted output
`BoundedStreamDrainer` imposes a hard 1 MB ceiling per stream (STDOUT and
STDERR). If the ceiling is exceeded, the buffer is truncated and a `Truncated`
flag is set in `CommandOutputMetadata`. Before any content crosses the IPC
surface, the `FND-020` redaction pipeline:

1. Strips all ANSI escape sequences (colour codes, cursor control, etc.).
2. Detects and replaces known secret canary patterns with `[REDACTED]`.

Zero raw, unscrubbed bytes are persisted to the Trust Evidence database or
emitted over the named-pipe IPC channel.

### Compound cryptographic approvals
Each command execution is gated by a `CommandApproval` whose identity is a
deterministic SHA-256 of:

```
HMAC-SHA256(TemplateHash || CanonicalArguments || WorkspaceSnapshotId)
```

A stale approval (mismatched template, argument drift, or Workspace generation
change) is rejected by the pipeline before the Job Object is created. This makes
approval-replay attacks structurally impossible.

### Ephemeral staging
STDOUT and STDERR are flushed as content-addressed blobs to `.opure-staging`.
Only `CommandOutputMetadata` (byte counts, truncation flags, blob hash) is
persisted to SQLite. The staging blobs are treated as disposable and are not
replicated over IPC.

### Zero AI inference, zero network listeners
The `FounderGateBSecurityTests` prove by static analysis that:

- No type in any `src/` assembly references AI inference APIs, agent loops, or
  ML runtime types.
- Runtime owns zero TCP or UDP endpoints (`TcpListener`, `UdpClient`, `Socket`
  bound to a port).
- Desktop reads no service database directly.

---

## Adversarial suite (CM-016)

`ControlledMutationAdversarialSuite` provides three executable proofs:

| Test | What it proves |
|---|---|
| `Patch_SourceDrift_CaptureAndRejection` | A proposal whose `ExpectedSourceSha256` does not match the live file is denied at the contract boundary. |
| `WorkerCrash_CompensationRollback_Proven` | A worker that throws `InvalidOperationException` causes the pipeline to propagate the exception without persisting a receipt or staging blob. |
| `OutputTruncation_TimeoutAudit_Proven` | A worker that throws `TimeoutException` produces a receipt with `WasTimeout = true`, `ExitCode = -2`, and a non-null staging blob hash for the partial output captured before the timeout. |

---

## Performance baseline

Captured on RTX-HAZE (32 processors, Windows NT 10.0.26300.0):

| Measurement | Value |
|---|---|
| Bootstrap to IPC session readiness | 3 161 ms |

Full evidence: [`eng/evidence/milestones/M6/GATE-B-metrics.md`](https://github.com/H4ZEY86/Opure/blob/main/eng/evidence/milestones/M6/GATE-B-metrics.md)

---

## Test matrix

| Test assembly | Result |
|---|---|
| `Opure.ArchitectureTests` | ✅ Passed |
| `Opure.Bootstrap.Windows.Tests` | ✅ Passed |
| `Opure.Configuration.Contracts.Tests` | ✅ Passed |
| `Opure.Configuration.Tests` | ✅ Passed |
| `Opure.Desktop.GatewayClient.Tests` | ✅ Passed |
| `Opure.Desktop.Tests` | ✅ Passed |
| `Opure.EndToEnd.Tests` | ✅ Passed |
| `Opure.Filesystem.Windows.Tests` | ✅ Passed |
| `Opure.Ipc.NamedPipes.Windows.Tests` | ✅ Passed |
| `Opure.Observability.Tests` | ✅ Passed |
| `Opure.Patch.Contracts.Tests` | ✅ Passed |
| `Opure.Patch.Service.Tests` | ✅ Passed |
| `Opure.Patch.Sqlite.Tests` | ✅ Passed |
| `Opure.Persistence.Sqlite.Tests` | ✅ Passed |
| `Opure.Project.Sqlite.Tests` | ✅ Passed |
| `Opure.Recovery.Contracts.Tests` | ✅ Passed |
| `Opure.Recovery.Service.Tests` | ✅ Passed |
| `Opure.Runtime.Tests` | ✅ Passed |
| `Opure.TrustEvidence.Contracts.Tests` | ✅ Passed |
| `Opure.TrustEvidence.Sqlite.Tests` | ✅ Passed |
| `Opure.Workspace.Boundaries.Tests` | ✅ Passed |
| `Opure.Workspace.Containment.Tests` | ✅ Passed |
| `Opure.Workspace.Execution.Tests` | ✅ Passed |
| `Opure.Workspace.Protocol.Tests` | ✅ Passed |
| `Opure.Workspace.Service.Tests` | ✅ Passed |
| `Opure.Workspace.Sqlite.Tests` | ✅ Passed |
| `Opure.Workspace.Windows.Tests` | ✅ Passed |

**Total: 950 · Failed: 0 · Skipped: 0 · Warnings: 0 · Errors: 0**

---

## What comes next — Phase 8

Phase 8 (Local Intelligence) begins after Gate B. It introduces a local inference
engine operating strictly within the offline-first, developer-authority model
established by Gate B. Remote providers, plugins, and MCP remain Phase 9 work.
No AI-generated patch is authorised before Founder Gate C.

---

*Opure is maintained and attributed exclusively to H4ZEY86 and DevMediaDesign.
Automated tools are not recognised as contributors or authors.*
