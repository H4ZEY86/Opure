# Opure — Project Context

## Product

Opure is a local-first, developer-controlled software engineering platform for Windows 11.

Its purpose is to help developers design, understand, modify, build, test and operate software without surrendering authority to autonomous AI behaviour.

The product motto is:

> Developer Respect. Local Intelligence. Complete Control.

The governing rule is:

> Build software with developers, not instead of them.

Opure is a software engineering platform that uses AI. It is not an AI platform that happens to write code.

## Repository

Repository root:

```text
C:\Opure
```

Specifications:

```text
C:\Opure\specs
```

Architecture Decision Records:

```text
C:\Opure\adr
```

Primary solution:

```text
C:\Opure\Opure.slnx
```

## Authoritative guidance and working memory

Repository work must use these sources together:

* `AGENTS.md` for standing engineering and safety instructions;
* `specs/README.md` for the specification catalogue, scope and Founder Gate boundaries;
* `specs/BACKLOG-001-foundation-first-12-weeks.md` for acceptance criteria and ticket order;
* `specs/ROADMAP-001-foundation-implementation-sequence.md` for milestone sequencing;
* the ticket's named specifications and ADRs for its detailed authority.

There is no canonical `roadmap/backlog/agents.md` file. References to that phrase
mean the sources above unless a future reviewed document explicitly supersedes
them. Do not infer completion from an idea, roadmap entry or draft specification;
the repository implementation, verifier evidence, reviewed commit and pushed
state remain decisive.

Opure-related working notes may be consulted read-only in:

```text
C:\Users\Ctdde\Documents\Obsidian Vault
```

Those notes are non-authoritative design memory. Use them to recover context and
identify future questions, but do not let them override repository specifications,
ADRs, backlog gates or current implementation evidence. Do not copy unrelated
personal material, secrets or private paths from the vault into source, logs or
evidence. In particular, model routing, token budgets, project memory, plugins,
MCP and agent ideas remain post-Gate-A work unless the approved roadmap changes.

Engineering commands are exposed through:

```text
C:\Opure\build.ps1
```

Use British English in documentation, messages and user-facing text.

## Core principles

All implementation work must preserve these principles:

* developer authority remains explicit;
* AI proposes, deterministic services authorise and execute;
* local operation is the default;
* cloud use is optional and policy-controlled;
* decisions and effects must be visible and inspectable;
* patches are reviewable by default;
* actions should be reversible where technically practical;
* services have explicit ownership boundaries;
* no hidden authority or hidden side effects;
* no silent fallback across trust boundaries;
* secrets must not appear in ordinary databases, project memory, embeddings, logs, command lines, checkpoints or Trust Centre evidence;
* Desktop is a command and projection layer, not an authority over domain state;
* agents are controlled workflows, not autonomous minds;
* provider and model implementations must remain replaceable;
* Windows is the first target, not a permanent architectural lock-in.

## Architecture

The initial process topology is:

```text
Opure.Bootstrap.Windows
    ├── Opure.Runtime
    └── Opure.Desktop
```

### Bootstrap

Bootstrap owns controlled product launch.

It must:

* resolve exact absolute executable paths;
* verify expected binary identities before launch;
* never search the current directory for executables;
* create channel-specific process environments;
* start Runtime before Desktop;
* wait for explicit Runtime readiness;
* pass only bounded, random session material;
* keep session secrets out of command lines and diagnostics;
* record safe child process identities;
* stop Desktop before Runtime;
* clean up partial launches;
* avoid persisting bootstrap session material.

Windows Job Objects, restart budgets, crash-loop policy and Safe Mode belong to the Process Supervisor work, not the basic Bootstrap ticket.

### Runtime

Runtime owns authoritative platform state and trusted first-party services.

Runtime must:

* operate offline;
* start without AI, plugins, MCP servers or cloud providers;
* expose explicit lifecycle states;
* produce a unique boot identity;
* expose product and contract versions;
* use time-bounded shutdown;
* keep service ownership explicit;
* avoid creating unowned databases or persistence;
* reject malformed bootstrap environment information;
* support direct engineering and test launches without Bootstrap;
* accept controlled Bootstrap shutdown without blocking startup.

### Desktop

Desktop uses Avalonia behind framework-neutral contracts and view models.

Desktop must:

* remain a command and projection layer;
* show honest disconnected Runtime state;
* never read service databases directly;
* never read project files directly;
* remain independently closable;
* preserve a future WinUI 3 fallback;
* provide keyboard navigation and stable accessibility names;
* keep authoritative state outside the UI.

## Release channels

Supported channels are:

```text
Stable
Preview
Development
```

Their mutable roots must not collide:

```text
%LOCALAPPDATA%\Opure\Stable
%LOCALAPPDATA%\Opure\Preview
%LOCALAPPDATA%\Opure\Development
```

Preview, Stable and Development must remain isolated in data, identity and future IPC namespaces.

## Current implementation state

Completed and committed:

* FND-001 — solution baseline;
* FND-002 — central build policy;
* FND-003 — authoritative version source;
* FND-004 — minimal Runtime executable and lifecycle;
* FND-005 — disconnected Avalonia Desktop shell;
* FND-006 — controlled Bootstrap executable;
* FND-007 — Windows process supervision and Safe Mode.
* FND-008 — versioned Runtime Health protobuf contract.
* FND-009 — named-pipe transport prototype.
* FND-010 — named-pipe session authentication.
* FND-011 — Runtime Service Registry contract.
* FND-012 — Service Lifecycle State Machine.
* FND-013 — Runtime Health UI.
* FND-014 — SQLite Persistence Library.
* FND-015 — Add Migration Runner.
* FND-016 — Add Transactional Outbox.
* FND-017 — Add Transactional Inbox.
* FND-018 — Add Structured Logging.
* FND-019 — Add Trace Propagation.
* FND-020 — Add Redaction and Canary Tests.
* FND-021 — Add Evidence Type Schema.
* FND-022 — Add Evidence Record Schema.
* FND-023 — Add Trust Evidence Database.
* FND-024 — Add Trust Evidence Ingestion.
* FND-025 — Add Trust Query Contract.
* FND-026 — Add Windows Path-Reference Library.
* FND-027 — Add Trusted Folder Picker Adapter.
* FND-028 — Add Project Service Database.
* FND-029 — Add Open Project Flow.
* FND-030 — Add Project Open Trust Receipt.
* FND-031 — Add Repository Identity Detection.
* FND-032 — Add Project List UI.
* FND-033 — Define Workspace Service Contract.
* FND-034 — Add File Inventory Generation.
* FND-035 — Add Safe File Hashing.
* FND-036 — Add Workspace Generation.
* FND-037 — Add Change Reconciliation.
* FND-038 — Add Workspace Snapshot Receipt.
* FND-039 — Add Setting Definition Schema.
* FND-040 — Add Policy Definition Schema.
* FND-043 — Strict JSON Parser.
* FND-044 — Duplicate Key Detector.
* FND-045 — Schema Registry.
* FND-046 — Project Settings Acquisition.
* FND-047 — Add Setting Merge.
* FND-048 — Product Policy Evaluator.
* FND-049 — Effective Configuration Snapshot.
* FND-050 — Add Per-Key Provenance.
* FND-051 — Add Configuration Change Transaction.
* FND-052 — Add Last-Known-Good Configuration.
* FND-053 — Trust Overview.
* FND-054 — Packaging.
* FND-055 — Add Trust Centre Project View.
* FND-056 — Add Trust Centre Configuration View.
* FND-057 — Add Trust Centre IPC Gateway.
* FND-058 — Add Backup Adapter Contract.
* FND-059 — Add SQLite Online Backup.
* FND-060 — Add Local Recovery Point View.
* GATE-A-001 — Run the End-to-End Foundation Demonstration.
* GATE-A-002 — Run Crash and Restart Recovery Suite.
* GATE-A-003 — Run IPC Security Suite.
* GATE-A-004 — Run Filesystem Adversarial Suite.
* GATE-A-005 — Run Configuration Adversarial Suite.
* GATE-A-006 — Run Trust Evidence Forgery and Reconciliation Suite.
* GATE-A-007 — Establish Performance Baseline.
* GATE-A-008 — Establish Accessibility Baseline.
* GATE-A-009 — Update ADR Evidence Matrix.
* GATE-A-010 — Founder Gate A Review.
* GATE-A-011 — Prepare Controlled Mutation Backlog.

Locally implemented, verified and committed; pending reviewed pushes:

* None.

The next planned ticket is:

```text
CM-001 — Version Patch Contracts and Exact UTF-8 Operation
```

GATE-A-001 passed its repeatable 32-step Development-channel demonstration and
the complete Release verification at commit
`b63d284f22dc1c5ec123c00779bcdbfd25a12110`, which is pushed to `origin/main`.
That verification executed 730 tests with zero warnings and zero errors. The
run-specific ignored launch receipt had payload SHA-256
`f55fc9d4a80b5f3522b17a50b5dd5ad8af52e0a3011b761a62df26c080d92200`.

GATE-A-002 passed its 12-scenario crash and restart matrix and the complete
Release verification with 735 tests, zero warnings and zero errors. Its
run-specific ignored receipt had payload SHA-256
`3b13fd9874fc85d6feb751211b4cd319e15f0c321a744c890f4006be36ca0714`.

GATE-A-003 passed its 12-scenario IPC security matrix and the complete Release
verification with 735 tests, zero warnings and zero errors. It proved a
32-connection admission ceiling and zero Runtime-owned TCP or UDP endpoints.
Its run-specific ignored receipt had payload SHA-256
`42d5cc3ce5a27e408d2991aab5427959595f05340a233af906ff0e8fcd6ad4de`.

GATE-A-004 passed its 20-scenario filesystem adversarial matrix and the complete
Release verification with 736 tests, zero warnings and zero errors. Case-only
and Unicode-normalisation collisions now prevent a false complete inventory
claim without exposing entry names. Its run-specific ignored receipt had
payload SHA-256
`c4d596d1e9137f37e12828cbd211950ae6d0d4ed6970e896e66936b5d4cf39f9`.

GATE-A-005 passed its 19-scenario configuration adversarial matrix and the
complete Release verification with 744 tests, zero warnings and zero errors.
Configuration approvals now bind the exact proposal, base profile revision and
optional Workspace generation and content hash. Invalid UTF-8, evaluator faults,
stale approvals and last-known-good/Trust projection behaviour have executable
proof. Its run-specific ignored receipt had payload SHA-256
`d02c9acaab502d689f3b84f0ea0eb59fb5000815f2f925a9eb4c49af80dcf60c`.

GATE-A-006 passed its 15-scenario Trust Evidence forgery and reconciliation
matrix and the complete Release verification with 755 tests, zero warnings and
zero errors. Exact owner-range repair, unavailable and deleted owner states,
hash-conflict quarantine, project/global capability separation, restart resume
and projection/database rebuild now have executable proof. Its run-specific
ignored receipt had payload SHA-256
`1188163b02681b160ad31e3cb1eda6202bea81fe7f19ecd566d13ed5725bcbd0`.

GATE-A-007 passed its 22-measurement Windows 11 performance baseline and the
complete Release verification with 762 tests, zero warnings and zero errors.
Every result is bound to build, hardware and fixture identity; security controls
remain enabled; Runtime and Desktop own no TCP or UDP endpoints; Opure uses its
Balanced performance mode; and cancellation latency is measured for authenticated
IPC and Workspace hashing. Desktop shell visibility at 5.834 seconds and Runtime
readiness at 3.673 seconds are retained as documented misses against provisional
ROADMAP targets rather than hidden. All regression thresholds pass, and a Windows
11, four-core, 8 GB low-resource follow-up is identified.

GATE-A-008 passed its 12-flow Windows accessibility baseline and the complete
Release verification with 765 tests, zero warnings and zero errors. Launch,
Runtime health, project open/list, configuration review, Trust Centre Overview,
selected-project evidence, invalid-source warning, Recovery Point creation and
verification, and error recovery have executable keyboard and UI Automation
proof. The Desktop now projects Trust Overview and selected-project causal
timeline data through authenticated named-pipe IPC without reading owner
databases or adding mutation authority. Warning, health, progress and
cancellation meaning is textual; evidence rows use semantic keyboard lists; no
fixed colours override high contrast; and the causal graph has an accessible
table alternative. Avalonia is retained for Gate A with packaged Narrator
listening quality recorded as a release-candidate confirmation limitation. The
committed evidence is bound to SHA-256
`a922199b8d8030da8b9138d8e009de982a70b3f1974c8c8f162a0a1fe04232b9`.

GATE-A-009 reviewed the 14 required ADRs against named implementation commits,
executable tests, applicable performance evidence, remaining limitations and
explicit proposed status changes. The machine verifier resolves every commit and
test path and rejects missing review fields. Avalonia, authenticated named-pipe
gRPC, SQLite Online Backup and the current trusted-service grouping are retained
for the Foundation subset. ADR-0011 is marked Amend because hosted workflow
assumptions no longer match the repository's deliberate absence of GitHub Actions.
No ADR is promoted from Proposed solely because implementation exists. The
complete Release verification passed 765 tests with zero warnings and zero errors.

GATE-A-010 records the founder directive that Gate A is cleared as an explicit
Accept with Amendments decision. Phase 7 Controlled Mutation entry is approved
without authorising AI, agents, plugins, MCP, connectors or network listeners.
All ten review questions, measured failures, accepted limitations, ADR decisions
and five owned/date-bound amendments are recorded against build
`0a25b3425abe325c78ee8e9deaaf37984448a07e`. The complete Release verification
passed 765 tests with zero warnings and zero errors.

GATE-A-011 defines and machine-verifies the dependency-ready Phase 7 backlog as
CM-001 through CM-016. Deterministic exact UTF-8 mutation, unified patch safety,
approval, staged/atomic writes, identity revalidation, recovery, Trust receipts
and accessible review precede typed read-only tool templates and the restricted
command worker. Every story links ADR/specification authority and records security,
recovery/compensation and acceptance evidence. Gate A amendments are carried
forward, no arbitrary shell exists, and no AI-generated patch is authorised before
Founder Gate B. The complete Release verification passed 765 tests with zero
warnings and zero errors.

This completes GATE-A-001 through GATE-A-011 and records Founder Gate A acceptance
with amendments. Phase 7 implementation begins at CM-001 under the verified
Controlled Mutation backlog.
The next programme phase is Controlled Mutation: deterministic reviewable file
patches first, followed by capability-bound curated commands. Local intelligence
follows Founder Gate B. Remote providers, plugins and MCP remain Phase 9 work
after local intelligence and Founder Gate C; do not pull them into the Gate B
critical path.

## Build policy

The repository uses:

* .NET SDK `10.0.302`;
* central package management;
* exact package versions;
* package lock files;
* locked restore;
* nullable reference types;
* warnings and analysers as errors;
* deterministic build output;
* Nerdbank.GitVersioning;
* xUnit v3;
* architecture tests.

Do not suppress analysers merely to make a build pass.

Correct the underlying source unless a suppression is explicitly justified by an existing policy or ADR.

Known strict analyser categories include:

* nullable correctness;
* concrete local types where appropriate;
* xUnit cancellation-token rules;
* xUnit assertion rules;
* allocation and constant-array rules;
* argument exception parameter validation.

## Engineering commands

Run the complete Release verification:

```powershell
pwsh ./build.ps1 verify -Configuration Release
```

Run Runtime:

```powershell
pwsh ./build.ps1 runtime `
    -RuntimeDurationMilliseconds 500
```

Run Runtime policy verification:

```powershell
pwsh ./build.ps1 runtime-policy
```

Run Desktop:

```powershell
pwsh ./build.ps1 desktop `
    -DesktopDurationMilliseconds 1500
```

Run Desktop policy verification:

```powershell
pwsh ./build.ps1 desktop-policy
```

Run a bounded Bootstrap launch:

```powershell
pwsh ./build.ps1 bootstrap `
    -Configuration Release `
    -BootstrapDurationMilliseconds 3000
```

Run Bootstrap evidence verification:

```powershell
pwsh ./build.ps1 bootstrap-policy
```

Generated build output under `artifacts` must not be committed.

## Required working style

Before editing:

1. Read the relevant specification, ADR, ticket and nearby implementation.
2. Inspect the current Git status.
3. Distinguish existing user changes from changes required for the task.
4. Do not discard or rewrite unrelated work.
5. Do not assume an interrupted installer means its earlier changes should be reverted.

While editing:

* make the smallest complete architectural change;
* preserve existing public contracts unless the ticket requires a change;
* keep implementation boundaries explicit;
* prefer deterministic code over hidden convention;
* avoid adding dependencies without strong justification;
* do not introduce AI, network, plugin, MCP or persistence capability outside the relevant ticket;
* keep secrets out of logs, errors, evidence and command lines;
* use absolute verified executable paths;
* use asynchronous process output safely;
* do not invoke PowerShell script blocks from .NET worker-thread callbacks;
* add regression tests for discovered failures;
* update architecture tests when a new boundary needs enforcement.

After editing:

1. Run formatting and static checks.
2. Run locked restore where required.
3. Build Release with zero warnings and zero errors.
4. Run all tests.
5. Run the ticket-specific verifier.
6. Inspect generated evidence.
7. Run `git diff --check`.
8. Show `git diff --stat` and `git status --short`.
9. Do not commit unless explicitly instructed.
10. Never add `artifacts`.

## Failure handling

When a command fails:

* diagnose the first real source failure;
* distinguish product failure from verifier or installer failure;
* preserve already reviewed partial work;
* avoid broad rewrites;
* correct likely follow-on analyser failures in the same narrow area;
* make reruns idempotent and exact-state guarded where scripts are involved;
* do not claim success until the actual Windows build, tests and evidence pass.

Expected negative tests may intentionally emit errors. Judge them by their final asserted result, not by alarming intermediate output.

## Security boundaries

Never:

* persist bootstrap session secrets;
* print environment secrets;
* place secrets in arguments;
* allow current-directory executable resolution;
* trust a child process solely because its filename looks correct;
* silently fall back to another executable, provider or channel;
* let Desktop become authoritative;
* introduce direct Desktop access to service persistence;
* introduce network clients or listeners during foundation tickets unless explicitly scoped;
* create service databases without a declared owner;
* load plugins or external providers into the trusted Runtime;
* weaken tests to accommodate incorrect behaviour.

## Documentation standard

Documentation must state:

* what owns the behaviour;
* what is authoritative;
* what is provisional;
* what is deliberately deferred;
* how the developer can inspect or stop the behaviour;
* what evidence proves the implementation;
* how failure and recovery work.

Do not describe aspirational behaviour as already implemented.

## Definition of done

A foundation ticket is complete only when:

* its acceptance criteria are implemented;
* Release builds with zero warnings and errors;
* all tests pass;
* architecture boundaries pass;
* Windows smoke or integration evidence passes where required;
* required evidence files are generated;
* no secrets or private paths leak into evidence;
* `git diff --check` passes;
* generated artefacts are excluded;
* the implementation is reviewed, committed and pushed;
* the working tree is clean and up to date with `origin/main`.
