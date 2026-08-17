# Opure v0.3.0-gate-c Release Notes

Gate C (v0.3.0) marks the formal completion of the Local Intelligence and Interactive Agent UX milestones (Phases 8-10). It builds upon the secure, offline-first foundation established in Gate B and introduces a comprehensive, deterministic agent execution pipeline with explicit developer authority.

## Milestone Delta from v0.2.0-gate-b

### Phase 8: Local Model Runtime (WP-018)
- Added Win32 Job Object kernel isolation (`JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE`) to tightly manage model process lifecycle.
- Introduced strict SHA-256 hash manifest verification for local models to prevent tampering.
- Implemented zero-allocation JSONL stream routing to handle model inference I/O efficiently.

### Phase 9: Workspace Toolchain & Remote Providers (WP-019)
- Introduced sandboxed inspection tools (`read_file_range`, `list_directory`, `inspect_diff`) guarded by strict `TrustedRoot` path containment.
- Added a deterministic 10-call recursion circuit breaker to prevent runaway agent execution loops.
- Defined explicit cryptographic mutation provenance via `ApproverIdentity.Agent`.
- Implemented zero-allocation `SseParser` for seamless remote provider failover.

### Phase 10: Interactive Desktop UX & Review Surface (WP-020)
- Built an Avalonia streaming telemetry pipeline using `ObservableCollection<ToolActivityItem>` for real-time visibility.
- Introduced a UTF-8 side-by-side patch review modal for inspecting AI-proposed diffs.
- Implemented one-click cryptographic user sign-off (`ApproverIdentity.User`) for explicit patch authorization.
- Added an immutable Trust Centre ledger feed mapping all agent lifecycle events and state transitions.

### Quality & Verification Matrix
- **979 passed tests** with **0 warnings** and **0 errors**.
- Strict offline-first airgap security assertion enforcement (restricting `HttpClient` strictly to `RemoteModelClient.cs`).

## Release Artifacts

- **File**: \Opure.Dev-1.3.0.60000-win-x64.msix\
- **Size**: 93,692,595 bytes
- **SHA-256 Checksum**: \E7D064E24236C1B120F7E4390CDD31DBBCBDD0FD55E2F590E20067563B52D541\

