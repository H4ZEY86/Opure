# GATE-A-009 ADR Evidence Matrix

Review date: 13 August 2026

Review scope: Founder Gate A, through GATE-A-008

Integration baseline: `917cbcb`
ADR source status at review: all reviewed ADRs remain `Proposed`; no decision is promoted merely because code exists.

This matrix separates the implemented Foundation subset from each ADR's wider
aspiration. `Retain` means the reviewed subset remains the recommended direction;
it does not accept the complete ADR. `Amend` requires an explicit ADR revision or
superseding record before acceptance.

## Reviewed decisions

### ADR-0001 — Primary Implementation Language

- Review scope: .NET 10 and C# foundation implementation.
- Current ADR status: Proposed.
- Gate A disposition: **Retain**.
- Implementation commits: `1457916`, `917cbcb`.
- Executable tests: `tests/Architecture/Opure.ArchitectureTests/Opure.ArchitectureTests.csproj`; complete locked Release verification (765 tests).
- Performance evidence: GATE-A-007 clean and incremental .NET build measurements.
- Remaining limitations: Windows ARM64 and non-Windows runtime validation remain outside Gate A.
- Proposed status change: Keep Proposed until Founder Gate A records approval.
- Supersession: None required for the reviewed subset.

### ADR-0002 — Desktop Framework

- Review scope: Avalonia shell, accessibility, packaging boundary and WinUI 3 fallback.
- Current ADR status: Proposed.
- Gate A disposition: **Retain** Avalonia for Gate A.
- Implementation commits: `5cc8c53`, `1e2a030`, `917cbcb`.
- Executable tests: `tests/Desktop/Opure.Desktop.Tests/Opure.Desktop.Tests.csproj` and `tests/Desktop/Opure.Desktop.GatewayClient.Tests/Opure.Desktop.GatewayClient.Tests.csproj`.
- Performance evidence: GATE-A-007 records Desktop visibility at 5.834 seconds as a documented miss; GATE-A-008 records the accessibility baseline.
- Remaining limitations: packaged audible Narrator review remains a release-candidate confirmation; Desktop visibility misses the provisional two-second target.
- Proposed status change: Keep Proposed pending the Founder Gate A framework decision.
- Supersession: Replace with a WinUI 3 ADR only if packaged UI Automation, high contrast, performance or framework support fails the recorded trigger.

### ADR-0003 — Runtime Process Topology

- Review scope: Bootstrap supervising one trusted Runtime and an independent Desktop; trusted services grouped as in-process Runtime modules.
- Current ADR status: Proposed.
- Gate A disposition: **Retain** the current service grouping.
- Implementation commits: `4dc3df4`, `3b092ac`, `f29aee3`.
- Executable tests: `tests/Bootstrap/Opure.Bootstrap.Windows.Tests/Opure.Bootstrap.Windows.Tests.csproj`, `tests/Runtime/Opure.Runtime.Tests/Opure.Runtime.Tests.csproj`, and `tests/EndToEnd/Opure.EndToEnd.Tests/Opure.EndToEnd.Tests.csproj`.
- Performance evidence: GATE-A-007 records Runtime readiness at 3.673 seconds as a documented miss plus bounded working-set and idle-CPU evidence.
- Remaining limitations: trusted workers, plugin hosts, MCP, AI providers and mediated tool processes are deliberately absent and unvalidated.
- Proposed status change: Keep Proposed until Founder Gate A explicitly retains or amends the topology.
- Supersession: A later worker or sandbox topology must amend or supersede this foundation subset rather than silently changing service grouping.

### ADR-0004 — Local IPC

- Review scope: authenticated gRPC over Windows named pipes for local first-party contracts.
- Current ADR status: Proposed.
- Gate A disposition: **Retain** named-pipe gRPC.
- Implementation commits: `d9360dc`, `3b51243`, `e97b9d2`.
- Executable tests: `tests/Ipc/Opure.Ipc.NamedPipes.Windows.Tests/Opure.Ipc.NamedPipes.Windows.Tests.csproj` and the authenticated IPC cases in `tests/EndToEnd/Opure.EndToEnd.Tests/Opure.EndToEnd.Tests.csproj`.
- Performance evidence: GATE-A-007 IPC health and service-registry latency; GATE-A-003 admission, authentication and zero-listener evidence.
- Remaining limitations: Unix-domain-socket portability, high-volume streaming and separately sandboxed third-party endpoints remain unimplemented.
- Proposed status change: Keep Proposed until Founder Gate A records the transport decision.
- Supersession: None required; future transports must reuse contracts and receive their own ADR evidence.

### ADR-0005 — Persistence

- Review scope: service-owned SQLite, migrations, transactional outbox/inbox and online backup.
- Current ADR status: Proposed.
- Gate A disposition: **Retain**.
- Implementation commits: `b96ff34`, `1bd9e9d`, `2758f45`, `c26f921`, `9b70845`.
- Executable tests: `tests/Persistence/Opure.Persistence.Sqlite.Tests/Opure.Persistence.Sqlite.Tests.csproj` and owner-database tests under Project, Workspace, Trust Evidence and Recovery.
- Performance evidence: GATE-A-007 SQLite transaction, outbox commit, evidence ingestion, disk growth and backup throughput measurements.
- Remaining limitations: large-payload content-addressed storage, long-duration endurance and non-local-filesystem behaviour remain outside the Foundation subset.
- Proposed status change: Keep Proposed pending Founder Gate A and later endurance evidence.
- Supersession: None required for the reviewed subset.

### ADR-0006 — Logging and Observability

- Review scope: structured local logs, trace propagation, redaction and canary protection.
- Current ADR status: Proposed.
- Gate A disposition: **Retain**.
- Implementation commits: `c9ed13b`, `25ec7c2`, `a3d6b83`, `3359ec2`.
- Executable tests: `tests/Observability/Opure.Observability.Tests/Opure.Observability.Tests.csproj` and operational-trace transport cases in `tests/Ipc/Opure.Ipc.NamedPipes.Windows.Tests/Opure.Ipc.NamedPipes.Windows.Tests.csproj`.
- Performance evidence: Not separately thresholded; GATE-A-007 ran with security and observability controls enabled.
- Remaining limitations: local long-duration telemetry analysis, diagnostic sessions, dumps and export bundles remain deferred.
- Proposed status change: Keep Proposed; evidence is complete only for the Foundation logging subset.
- Supersession: None required for the reviewed subset.

### ADR-0008 — Testing Strategy

- Review scope: deterministic unit, architecture, integration, adversarial, headless UI and Windows E2E verification.
- Current ADR status: Proposed.
- Gate A disposition: **Retain**.
- Implementation commits: `8090fc9`, `b63d284`, `917cbcb`.
- Executable tests: `Opure.slnx` through `pwsh ./build.ps1 verify -Configuration Release`.
- Performance evidence: GATE-A-007 is the repeatable baseline; A001–A008 evidence is committed under `eng/evidence/milestones/M6`.
- Remaining limitations: installer upgrade/uninstall, ARM64, low-resource reference hardware and long-duration endurance suites remain pending.
- Proposed status change: Keep Proposed until those later release dimensions are covered.
- Supersession: None required.

### ADR-0009 — Windows Path and Filesystem Handling

- Review scope: path references, trusted folder selection, inventory, hashing and adversarial Windows filesystem behaviour.
- Current ADR status: Proposed.
- Gate A disposition: **Retain**.
- Implementation commits: `8395a44`, `bb17831`, `1b8db10`.
- Executable tests: `tests/Filesystem/Opure.Filesystem.Windows.Tests/Opure.Filesystem.Windows.Tests.csproj` and Workspace Windows tests.
- Performance evidence: GATE-A-007 hashing throughput and project-open measurements; GATE-A-004 adversarial matrix.
- Remaining limitations: ReFS-specific endurance, removable media and future non-Windows path adapters remain unvalidated.
- Proposed status change: Keep Proposed pending broader platform evidence.
- Supersession: None required.

### ADR-0010 — Repository and Solution Structure

- Review scope: modular monorepo, `Opure.slnx`, central policy and architecture boundaries.
- Current ADR status: Proposed.
- Gate A disposition: **Retain**.
- Implementation commits: `1457916`, `8090fc9`, `917cbcb`.
- Executable tests: `tests/Architecture/Opure.ArchitectureTests/Opure.ArchitectureTests.csproj` and repository policy targets in `build.ps1`.
- Performance evidence: GATE-A-007 clean and incremental solution build measurements.
- Remaining limitations: Phase 7 mutation/tool projects and later plugin/provider projects have not tested future solution scale.
- Proposed status change: Keep Proposed pending Founder Gate A and future scale review.
- Supersession: None required.

### ADR-0011 — Build and Continuous Integration

- Review scope: deterministic local build policy and the repository's current absence of hosted workflow definitions.
- Current ADR status: Proposed.
- Gate A disposition: **Amend**.
- Implementation commits: `8090fc9`, `df8ece5`, `917cbcb`.
- Executable tests: `pwsh ./build.ps1 verify -Configuration Release` plus build-policy and version-policy targets.
- Performance evidence: GATE-A-007 clean and incremental build measurements.
- Remaining limitations: no hosted continuous-integration workflow currently enforces the local verifier on pushes or pull requests.
- Proposed status change: Keep Proposed and amend the ADR to separate authoritative local verification from optional hosted automation.
- Supersession: The ADR's hosted-workflow assumptions must be amended or superseded; deleted workflows must not be described as active evidence.

### ADR-0012 — Versioning and Release Management

- Review scope: authoritative version source, deterministic build identity and channel isolation.
- Current ADR status: Proposed.
- Gate A disposition: **Retain**.
- Implementation commits: `ef2bd25`, `96753ff`, `917cbcb`.
- Executable tests: version policy in `build.ps1`, Bootstrap channel tests, and `tests/Architecture/Opure.ArchitectureTests/Opure.ArchitectureTests.csproj`.
- Performance evidence: Not directly applicable; GATE-A-007 binds every measurement to build identity.
- Remaining limitations: signed Stable release, update feed, rollback and installer upgrade identity remain pending.
- Proposed status change: Keep Proposed until signed packaging and update evidence exists.
- Supersession: None required for the reviewed subset.

### ADR-0026 — Configuration, Profile and Policy Management

- Review scope: Foundation defaults, strict project settings, merge, policy evaluation, effective snapshot, provenance, transactions and last-known-good recovery.
- Current ADR status: Proposed.
- Gate A disposition: **Retain** the Foundation subset.
- Implementation commits: `0cf0c98`, `8185155`, `c28d80b`, `5d1b66f`.
- Executable tests: `tests/Configuration/Opure.Configuration.Tests/Opure.Configuration.Tests.csproj` and `tests/Configuration/Opure.Configuration.Contracts.Tests/Opure.Configuration.Contracts.Tests.csproj`.
- Performance evidence: GATE-A-007 effective-configuration build; GATE-A-005 adversarial configuration matrix.
- Remaining limitations: enterprise policy distribution, multi-user profile sharding and later mutation UI remain outside the reviewed subset.
- Proposed status change: Keep Proposed; do not infer acceptance of the complete ADR from Foundation code.
- Supersession: None required for the reviewed subset.

### ADR-0027 — Trust Centre, Evidence Retention and Support Bundles

- Review scope: Foundation evidence types/records, owner database, ingestion, queries, completeness, reconciliation and accessible Desktop projections.
- Current ADR status: Proposed.
- Gate A disposition: **Retain** the Foundation subset.
- Implementation commits: `6114042`, `8c8df96`, `12cabc7`, `917cbcb`.
- Executable tests: Trust Evidence contract/SQLite tests, `tests/EndToEnd/Opure.EndToEnd.Tests/Opure.EndToEnd.Tests.csproj`, and Desktop accessibility tests.
- Performance evidence: GATE-A-007 10,000-record Trust query; GATE-A-006 forgery/reconciliation suite; GATE-A-008 accessible projection evidence.
- Remaining limitations: retention execution, preservation holds, diagnostics collection, support-bundle export and later plugin/provider evidence remain deferred.
- Proposed status change: Keep Proposed; the complete ADR has substantial unimplemented scope.
- Supersession: None required for the reviewed subset.

### ADR-0028 — Backup, Restore, Data Portability and Disaster Recovery

- Review scope: local same-device Recovery Points, consistency barrier, SQLite Online Backup, manifest, disposable verification and Desktop projection.
- Current ADR status: Proposed.
- Gate A disposition: **Retain** SQLite Online Backup and the local Recovery Point subset.
- Implementation commits: `ec9795d`, `9b70845`, `85da9d5`, `917cbcb`.
- Executable tests: `tests/Recovery/Opure.Recovery.Service.Tests/Opure.Recovery.Service.Tests.csproj`, Recovery contracts tests and Recovery Point E2E cases.
- Performance evidence: GATE-A-007 consistency-barrier, backup-throughput and disposable-restore measurements.
- Remaining limitations: device-loss protection, encrypted repositories, portable archives, selective activation, disaster-recovery plans and recovery exercises remain unimplemented.
- Proposed status change: Keep Proposed; same-device recovery must never be represented as a complete backup strategy.
- Supersession: None required for the reviewed subset.

## Explicit architecture decisions

- Avalonia: **Retain for Gate A**, with WinUI 3 retained as the evidence-triggered fallback.
- Local IPC: **Retain authenticated gRPC over Windows named pipes**; loopback TCP remains disabled.
- Persistence backup primitive: **Retain SQLite Online Backup** behind service-owned adapter contracts.
- Service grouping: **Retain one trusted Runtime process containing strongly bounded first-party modules**; do not introduce one process per logical service without evidence.
- Hosted workflows: **Amend ADR-0011** because the repository intentionally contains no active GitHub Actions or Git workflow directory.

## Review notes and proposed status changes

No reviewed ADR is promoted to Accepted by GATE-A-009. Founder Gate A owns the
explicit decision. ADR-0011 requires amendment or supersession. The remaining
reviewed decisions retain their stated Foundation subset while staying Proposed;
ADR-0026, ADR-0027 and ADR-0028 explicitly do not claim their wider roadmaps are
implemented.

## Architecture follow-up backlog

1. Founder Gate A: record Retain/Amend/Replace decisions and Phase 7 authority.
2. ADR-0011: reconcile local authoritative verification with the deliberate absence of hosted workflows.
3. ADR-0002: perform packaged Windows Narrator listening review and remeasure visible-shell startup.
4. ADR-0003: write separate evidence before adding worker, plugin, MCP, provider or tool-host processes.
5. ADR-0004: define Unix-domain-socket portability only when a non-Windows target enters scope.
6. ADR-0005/0008: run low-resource and endurance baselines on the recorded Windows 11 reference class.
7. ADR-0012/0028: bind signed installer, upgrade, rollback and recovery-exercise evidence before Stable release.
8. ADR-0026/0027/0028: retain explicit subset labels until the deferred capabilities have their own tickets and evidence.
