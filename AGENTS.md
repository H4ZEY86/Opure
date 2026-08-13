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

Locally implemented, verified and committed; pending reviewed pushes:

* None.

The next planned ticket is:

```text
GATE-A-001 — Run the End-to-End Foundation Demonstration
```

FND-060 passed its ticket verifier and was reviewed, committed and pushed.
Do not assume GATE-A-001 is complete until its repeatable clean-root demonstration,
evidence, Release verification, review, commit and push are complete.

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
