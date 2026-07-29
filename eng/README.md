# Opure Engineering Commands

Repository-owned scripts are the authoritative local build interface.

## Commands

```powershell
pwsh ./build.ps1 restore
pwsh ./build.ps1 build
pwsh ./build.ps1 test
pwsh ./build.ps1 verify
pwsh ./build.ps1 policy
```

`restore`, `build`, `test` and `verify` use the committed package lock files.

`policy` performs the heavier FND-002 evidence checks:

- warning-as-error negative probe;
- stale lock-file negative probe;
- deterministic Release assembly comparison;
- dependency inventory;
- and M0 evidence generation.

## Build channels

```text
Development
Preview
Stable
```

Select one with:

```powershell
pwsh ./build.ps1 verify -BuildChannel Development
```

The channels define compile-time identity only. They do not weaken security, auditing, warning, package or test policy.

## Generated output

All generated build and intermediate output belongs under:

```text
artifacts/
```

The directory is ignored by Git.

## Package policy

Package versions belong only in:

```text
Directory.Packages.props
```

Project files contain versionless `PackageReference` items.

Package lock files are committed beside each project and validation uses locked restore.

## Local tools

Restore exact repository tools with:

```powershell
dotnet tool restore
```

The manifest is security-sensitive because local tools execute with developer privileges.

## Version identity

The authoritative product version is:

```text
version.json
```

The repository pins matching releases of:

```text
Nerdbank.GitVersioning 3.10.70
nbgv 3.10.70
```

Show the current resolved identity with:

```powershell
pwsh ./build.ps1 version
```

Run the FND-003 evidence probes with:

```powershell
pwsh ./build.ps1 version-policy
```

Every first-party project receives the generated internal `ThisAssembly` class. Hosts should use it for diagnostic identity rather than declaring version literals.

Useful generated members include:

```text
ThisAssembly.AssemblyVersion
ThisAssembly.AssemblyFileVersion
ThisAssembly.AssemblyInformationalVersion
ThisAssembly.GitCommitId
ThisAssembly.IsPublicRelease
ThisAssembly.NuGetPackageVersion
```

A development build may be clean or dirty. `eng/version.ps1` reports that state explicitly.

Preview and Stable build-channel commands require a clean working tree.

Public release classification is tag-only. The normal `main` branch is not a public release ref.

The trusted `PublicRelease=true` override is reserved for an exact validated release candidate or tagged commit. It must never be used merely to remove commit identity from ordinary builds.

## Runtime executable

Run the Development Runtime until Ctrl+C:

```powershell
pwsh ./build.ps1 runtime
```

Run it for a bounded smoke-test interval:

```powershell
pwsh ./build.ps1 runtime -RuntimeDurationMilliseconds 1000
```

The minimal FND-004 Runtime:

- owns one random boot identity per process start;
- reports `starting`, `ready`, `stopping` and `stopped`;
- reports product and Runtime contract versions;
- uses the Development channel data-root resolver;
- writes newline-delimited safe JSON to standard output;
- creates no durable state;
- opens no TCP or UDP endpoint;
- starts no child process;
- and loads no project, AI, plugin, MCP or workflow code.

Run the full Runtime evidence gate with:

```powershell
pwsh ./build.ps1 runtime-policy
```

Ctrl+C is intercepted through the controlled shutdown signal so the Runtime can report `stopping` and `stopped` before exit. The current shutdown deadline is five seconds.

## Desktop executable

Run the Development Desktop until its main window is closed:

```powershell
pwsh ./build.ps1 desktop
```

Run a bounded real-window smoke launch:

```powershell
pwsh ./build.ps1 desktop -DesktopDurationMilliseconds 1500
```

Run the complete FND-005 evidence gate:

```powershell
pwsh ./build.ps1 desktop-policy
```

The initial Desktop uses Avalonia 12.1.0 through a framework-specific adapter project. `Opure.Desktop.Contracts` remains framework neutral so the documented WinUI 3 fallback can reuse its shell state and view model.

The shell reports `Runtime unavailable` honestly until authenticated local IPC exists. It does not read project files, open service databases or own authoritative domain state.

## Bootstrap executable

Run the Development Bootstrap until the Desktop is closed:

```powershell
pwsh ./build.ps1 bootstrap
```

Run a bounded process-tree smoke launch:

```powershell
pwsh ./build.ps1 bootstrap -Configuration Release -BootstrapDurationMilliseconds 3000
```

Run the complete FND-006 evidence gate:

```powershell
pwsh ./build.ps1 bootstrap-policy
```

Run the complete FND-007 process-supervisor evidence gate:

```powershell
pwsh ./build.ps1 supervisor-policy
```

Run the complete FND-008 Runtime Health contract evidence gate:

```powershell
pwsh ./build.ps1 health-contract-policy
```

Run the complete FND-009 named-pipe transport evidence gate:

```powershell
pwsh ./build.ps1 health-transport-policy
```

Run the complete FND-010 named-pipe session-authentication evidence gate:

```powershell
pwsh ./build.ps1 health-session-policy
```

Run the complete FND-011 Runtime Service Registry contract evidence gate:

```powershell
pwsh ./build.ps1 service-registry-policy
```

Run the complete FND-012 Service Lifecycle evidence gate:

```powershell
pwsh ./build.ps1 service-lifecycle-policy
```

Run the complete FND-013 Runtime Health UI evidence gate:

```powershell
pwsh ./build.ps1 runtime-health-ui-policy
```

Run the complete FND-014 SQLite persistence evidence gate:

```powershell
pwsh ./build.ps1 persistence-policy
```

Run the complete FND-015 SQLite migration evidence gate:

```powershell
pwsh ./build.ps1 migration-policy
```

Run the complete FND-016 transactional outbox evidence gate:

```powershell
pwsh ./build.ps1 outbox-policy
```

Run the complete FND-017 transactional inbox evidence gate:

```powershell
pwsh ./build.ps1 inbox-policy
```

Run the complete FND-018 structured operational logging evidence gate:

```powershell
pwsh ./build.ps1 structured-logging-policy
```

Run the complete FND-019 trace propagation evidence gate:

```powershell
pwsh ./build.ps1 trace-propagation-policy
```

Run the complete FND-020 redaction and canary evidence gate:

```powershell
pwsh ./build.ps1 redaction-policy
```

Run the complete FND-021 Evidence Type schema evidence gate:

```powershell
pwsh ./build.ps1 evidence-type-policy
```

Run the complete FND-022 Evidence Record schema evidence gate:

```powershell
pwsh ./build.ps1 evidence-record-policy
```

Run the complete FND-023 Trust Evidence database evidence gate:

```powershell
pwsh ./build.ps1 trust-database-policy
```

Run the complete FND-024 Trust Evidence ingestion evidence gate:

```powershell
pwsh ./build.ps1 trust-ingestion-policy
```

Run the complete FND-025 Trust Evidence query evidence gate:

```powershell
pwsh ./build.ps1 trust-query-policy
```

Run the complete FND-026 Windows Path-Reference evidence gate:

```powershell
pwsh ./build.ps1 path-reference-policy
```

Run the complete FND-027 Trusted Folder Picker evidence gate:

```powershell
pwsh ./build.ps1 folder-picker-policy
```

Bootstrap verifies absolute Runtime and Desktop executable paths and companion assembly identities before launch. It starts Runtime first, waits for explicit Runtime readiness, starts Desktop second, and shuts down Desktop before Runtime.

Supervisor verification injects a bounded Runtime crash, a rapid crash loop and an abrupt Bootstrap termination. It verifies restart identity, exponential backoff, visible Safe Mode and Windows Job Object orphan cleanup without recording child environment values.

Runtime Health contract verification compiles the protobuf client and server surfaces, exercises compatibility and semantic validation, enforces message and service-summary bounds, and emits the authoritative schema, compatibility matrix and golden messages under `eng/evidence/milestones/M2`.

Named-pipe transport verification exercises the Desktop gateway round trip, deadline, cancellation, message-size and restart/reconnect paths. It records a bounded unary latency baseline and inspects the live Runtime process for TCP and UDP listeners without logging RPC payloads.

Named-pipe session verification inspects the protected DACL, exercises expected and denied same-user sessions, process binding, replay and expiry paths, and confirms Runtime and Desktop restart rotation. It emits only bounded policy results and scans the evidence and running process command lines for authentication material.

Service Registry verification compiles the protobuf query surface, exercises transactional registration, duplicate and dependency rejection, deterministic cursor ordering, serialization and the authenticated named-pipe endpoint. It emits the authoritative schema and safe initial catalogue under `eng/evidence/milestones/M1`.

Service Lifecycle verification exercises the exhaustive transition policy, dependency-aware start and reverse-order shutdown, required and optional failure propagation, startup and shutdown deadlines, restart transitions, deterministic events and the registry-backed lifecycle projection. It emits the reviewed state-machine diagram and transition report under `eng/evidence/milestones/M1`.

Runtime Health UI verification exercises the live registry-backed projection, authenticated refresh and reconnect path, stale-snapshot recovery, all six visible Runtime states, safe boot-identity copy, keyboard and UI Automation semantics, theme-owned high-contrast colours and a 64-row performance baseline. It observes a native Windows window and emits the UI test artefact, accessibility report and reconnect recording under `eng/evidence/milestones/M1`.

SQLite persistence verification exercises canonical channel-isolated ownership paths, fixed connection strings, WAL/FULL/foreign-key/trusted-schema configuration, application identity, commit and rollback, one process-wide writer, malformed database preservation and architecture isolation. It records the loaded native SQLite dependency manifest, transaction report and reviewed library design under `eng/evidence/milestones/M3`.

SQLite migration verification exercises fresh and incremental forward migration, deterministic checksum history, per-migration rollback, interruption recovery, unsupported-newer-schema refusal, readiness blocking, verified Recovery Point hooks and staged-restore-copy invocation. It records the reviewed migration catalogue, failure/rollback report and schema-validation report under `eng/evidence/milestones/M3`.

Transactional outbox verification exercises atomic domain/envelope commit and rollback, immutable payload identity, monotonic owner sequences, ordered leases, bounded retry, backlog health, restart recovery and expected duplicate delivery after an expired in-flight lease. It records the atomic transaction report, crash matrix and owner-sequence report under `eng/evidence/milestones/M3` without claiming exactly-once delivery.

Transactional inbox verification exercises atomic receipt/domain-effect commit and rollback, source-scoped identities, matching duplicate acknowledgement, conflicting duplicate quarantine, unsupported revision refusal, restart-safe replay and conflict health. It records the idempotency and conflicting-duplicate reports under `eng/evidence/milestones/M3`; conflicting payload bytes are not copied into the conflict ledger.

Structured operational logging verification exercises fixed reviewed event definitions, typed per-event attribute allowlists, pre-queue sanitisation, the bounded severity-aware queue, JSON Lines parsing, size rotation, age and count retention, partial-write recovery, Windows path pinning, validated-handle mutation, sink failure isolation and safe Runtime Health degradation. It records the schema, rotation, queue and injection reports under `eng/evidence/milestones/M3`; operational logs remain ordinary local diagnostics and never substitute for authoritative Trust Evidence.

Trace propagation verification exercises a connected Desktop Gateway, Runtime
IPC and owner-service trace, W3C metadata propagation, asynchronous parentage,
cancellation, stable failure classes, payload-canary exclusion,
high-cardinality rejection, disabled sampling and bounded latency overhead. A
bounded Bootstrap launch verifies the trace identity in Runtime logs. Traces
remain local, non-authoritative diagnostics; baggage and external export are
disabled.

Redaction verification exercises the versioned local-diagnostics profile,
allowlist-first field admission, exact and pattern canaries, percent and base64
encoding, safe absolute-path categorisation, exception-metadata exclusion,
trace-tag admission and fail-closed processor behaviour. It scans the generated
reports and retained trace evidence without reproducing rejected values.

Evidence Type verification exercises the framework-neutral
`opure.trust-evidence-type/1` contract, immutable revision and canonical-hash
rules, owner and Authority Class binding, safe payload indexes, explicit
retention, support-export and redaction metadata, unknown-type refusal,
historical revision lookup and the reviewed nine-type foundation catalogue.
Record persistence and ingestion remain deferred to their dependency-ordered
tickets.

Evidence Record verification exercises the framework-neutral
`opure.trust-evidence-record/1` envelope, opaque identities, exact type, owner
and authority binding, project-scope requirements, distinct source and
observation times, bounded correlation and sequence fields, 64 KiB canonical
inline JSON, bounded owner and content-addressed references, prohibited fields,
payload SHA-256 and a framed canonical record-hash vector. Persistence,
deduplication and quarantine are implemented by the dependency-ordered database
and ingestion gates below.

Trust Evidence database verification exercises isolated `trust.db` creation,
forward migration, the single-writer WAL and foreign-key profile, duplicate and
parent constraints, owner-sequence, project and operation query plans,
payload-free projections, projection reset and bounded corruption health. The
store is a non-authoritative Trust projection; missing projection data is
reported as incomplete and never as proof that no activity occurred. Ingestion,
duplicate acknowledgement and conflict quarantine are verified separately by
the FND-024 gate below.

Trust Evidence ingestion verification exercises transport-authenticated owner
binding, exact Evidence Type and hash validation, matching retry
acknowledgement, retained conflicting-duplicate quarantine, unknown-type
quarantine, owner-gap recording, previous-stream validation and injected
database rollback. Inbox, record, payload, sequence, projection, retention,
receipt and gap writes share one SQLite transaction. The projection is a
Verified Service Receipt and does not replace owner-domain authority.

Trust Evidence query verification exercises the typed `opure.trust-query/1`
contract, transport-authenticated project and release-channel scope, operation,
Evidence Type, Authority Class, outcome and bounded time filters, stable keyset
pagination during concurrent ingestion, malformed and mismatched cursors,
projection-generation refresh, owner-gap completeness, payload omission and the
reviewed project/channel/time query index. Query snapshots expose freshness,
owner availability, completeness and redaction metadata; raw SQL, regex and
arbitrary expressions are not contract fields.

Windows Path-Reference verification exercises bounded logical paths, ordinary
local-root registration, no-follow component walking, reparse denial,
handle-derived final paths, 128-bit file identity, volume binding, alternate
stream detection and replacement-race revalidation. The library is
inspection-only: developer approval, mutation, journalling and recovery remain
owned by later tickets.

Trusted Folder Picker verification exercises one-shot Avalonia folder
selection, cancellation without transfer, local-root handle verification,
network and reparse refusal, deletion-after-selection recovery, an opaque
capability receiver port and keyboard/automation metadata. Desktop retains
only display classification; the Project Service receiver remains honestly
unavailable until its dependency-ordered implementation.

Channel-specific data-root and one-time session material are passed through bounded environment variables. The session secret is not placed on command lines, written to disk or included in diagnostics.
