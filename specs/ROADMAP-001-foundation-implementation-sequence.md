# ROADMAP-001 — Foundation Implementation Sequence

## Opure Implementation Programme

**Status:** Proposed
**Date:** 18 July 2026
**Programme owner:** Christopher Dyer, Founder and Product Owner
**Architecture owner:** Opure Foundation Architecture
**Applies to:** ADR-0001 through ADR-0028
**Repository root:** `C:\Opure`
**Target platform:** Windows 11 first
**Primary implementation language:** C# on supported LTS .NET
**Desktop framework:** Avalonia provisional
**Delivery principle:** Build the smallest trustworthy vertical slice first, then expand capability behind accepted evidence gates

---

# 1. Purpose

This roadmap converts the complete foundational Architecture Decision Record sequence into an executable implementation programme.

It defines:

* the order in which platform capabilities should be built;
* the dependency relationships among ADRs;
* the prototypes required before production commitment;
* the first end-to-end vertical slice;
* milestone entry and exit criteria;
* founder decision points;
* engineering workstreams;
* quality gates;
* security gates;
* performance gates;
* recovery gates;
* and the first twelve weeks of implementation.

This document is not a release marketing plan.

It is the technical execution plan for turning Opure from an architectural specification into a working developer-controlled local software engineering platform.

---

# 2. Governing Principle

> **Build the trustworthy control plane before building AI capability on top of it.**

Opure must prove:

* process supervision;
* authenticated local IPC;
* explicit service ownership;
* typed configuration;
* safe filesystem boundaries;
* durable persistence;
* structured observability;
* immutable evidence;
* and recoverable state

before adding:

* remote AI providers;
* local model runtimes;
* plugins;
* MCP servers;
* complex workflows;
* or broad automation.

The first implementation objective is not “AI that writes code”.

The first implementation objective is:

> **A visible, inspectable and recoverable local platform that can open a project, read it safely, apply configuration, record exactly what happened and show the result to the developer.**

---

# 3. Foundation Status

The foundational ADR sequence is complete through ADR-0028.

The sequence establishes:

* language and framework;
* desktop architecture;
* supervised process topology;
* local IPC;
* persistence;
* logging and observability;
* secrets;
* testing;
* filesystem boundaries;
* repository structure;
* build and CI;
* versioning and releases;
* packaging;
* code signing;
* updating;
* plugin packaging;
* plugin permissions;
* MCP trust;
* provider trust;
* local model runtime;
* context assembly;
* project knowledge;
* project memory;
* AI evaluation and routing;
* workflow execution;
* configuration and policy;
* Trust Centre evidence;
* backup, restore and disaster recovery.

The architecture is sufficiently defined to begin implementation.

Most ADRs remain **Proposed**.

Implementation must therefore gather the acceptance evidence required to move provisional decisions to **Accepted**.

---

# 4. Programme Outcome

The foundation programme is complete when Opure can:

1. install as a signed per-user MSIX;
2. launch an Avalonia desktop application;
3. supervise a local Runtime process;
4. authenticate over named-pipe gRPC;
5. open and identify a project safely;
6. create a verified Workspace Snapshot;
7. load typed Effective Configuration;
8. read project files through handle-verified boundaries;
9. record authoritative Trust Evidence;
10. display that evidence in the Trust Centre;
11. recover from Runtime and Desktop crashes;
12. create and restore a verified local Recovery Point;
13. pass the required security and adversarial suites;
14. build reproducibly in CI;
15. package, sign and install a release candidate;
16. and demonstrate that every side effect remains developer controlled.

AI capability may be introduced progressively after these controls are working.

---

# 5. Delivery Strategy

The implementation strategy has five layers.

## 5.1 Layer 1 — Bootstrap and Engineering System

Create:

* repository structure;
* build scripts;
* CI;
* versioning;
* packaging skeleton;
* coding standards;
* test harness;
* and dependency governance.

---

## 5.2 Layer 2 — Trusted Local Control Plane

Create:

* Runtime supervisor;
* service registry;
* named-pipe IPC;
* service authentication;
* service health;
* persistence primitives;
* observability;
* and Trust Evidence primitives.

---

## 5.3 Layer 3 — Project and Configuration Plane

Create:

* Project Service;
* Workspace Service;
* repository identity;
* path security;
* project snapshots;
* configuration and policy;
* and developer-facing Trust Centre.

---

## 5.4 Layer 4 — Controlled Execution and Intelligence

Create:

* Patch Service;
* Tool Mediation;
* local model runtime;
* provider router;
* Context Assembly;
* Project Knowledge;
* Project Memory;
* workflows;
* plugins;
* and MCP.

---

## 5.5 Layer 5 — Productisation and Resilience

Create:

* installer;
* code signing;
* updater;
* support bundles;
* backup and restore;
* disaster recovery;
* performance optimisation;
* and release readiness.

---

# 6. Implementation Rules

The following rules apply across all phases.

## 6.1 One Authority Per State Boundary

Each durable state domain has one owner service.

No other service writes its database directly.

---

## 6.2 No Hidden Side Effects

Every command, file write, provider call, plugin call, MCP call and workflow effect must have:

* an explicit plan;
* policy evaluation;
* capability;
* execution receipt;
* and Trust Evidence.

---

## 6.3 No AI in the Control Plane

AI may propose.

Deterministic services authorise and execute.

---

## 6.4 Build Vertically

Every milestone should produce a running slice across:

* Desktop;
* Runtime;
* service;
* persistence;
* observability;
* Trust Evidence;
* UI;
* and tests.

Avoid building large disconnected infrastructure layers without a user-visible proof.

---

## 6.5 Keep Provisional Decisions Replaceable

Implementation behind:

* Avalonia;
* llama.cpp;
* JSON Schema library;
* SQLite adapter;
* package distribution;
* and cryptographic format

must use narrow adapters.

---

## 6.6 Evidence Before Expansion

A phase cannot expand to higher-risk capability until its evidence gate passes.

---

## 6.7 Windows First, Not Windows Locked

Windows implementation may use:

* named pipes;
* DPAPI;
* Job Objects;
* AppContainer;
* MSIX;
* and Windows filesystem handles.

Contracts should remain portable where practical.

---

## 6.8 Local First

A fresh installation should perform core project work without a network connection.

---

## 6.9 No Silent Fallback Across Trust Boundaries

No:

* local-to-cloud;
* provider-to-provider;
* plugin-to-MCP;
* trusted-to-untrusted;
* or machine-to-network

fallback without explicit policy and visibility.

---

## 6.10 Tests Are Product Work

Security, recovery, accessibility and performance tests are deliverables, not deferred quality work.

---

# 7. First Vertical Slice

The first implementable vertical slice is:

```text
Avalonia Desktop
    ↓
Runtime Supervisor
    ↓
Authenticated Named-Pipe gRPC
    ↓
Project Service
    ↓
Workspace Snapshot
    ↓
Configuration Snapshot
    ↓
Trust Evidence Receipt
    ↓
Trust Centre Display
```

The user journey is:

1. install or run Opure Development;
2. launch Desktop;
3. Desktop starts or connects to Runtime;
4. Runtime starts Project Service, Workspace Service, Configuration Service and Trust Evidence Service;
5. developer chooses a project directory;
6. Project Service validates and registers the project;
7. Workspace Service creates a safe immutable snapshot of selected metadata and files;
8. Configuration Service resolves Product, User and Project configuration;
9. Project Open Receipt and Effective Configuration Receipt are published;
10. Trust Centre displays the causal timeline;
11. developer closes and reopens Opure;
12. project state and evidence recover correctly.

No AI is required.

No provider is required.

No plugin is required.

No MCP server is required.

No command execution is required.

No project write is required.

---

# 8. First Vertical Slice Acceptance Criteria

The first vertical slice is complete when:

* [ ] `Opure.Desktop` launches.
* [ ] `Opure.Runtime` launches under supervision.
* [ ] Runtime publishes a stable boot identity.
* [ ] Desktop authenticates over named-pipe gRPC.
* [ ] Session keys are not persisted.
* [ ] Service registry shows required services.
* [ ] Health checks are visible.
* [ ] Project picker uses trusted filesystem APIs.
* [ ] Project root is represented by a typed handle-verified reference.
* [ ] Reparse and traversal checks exist.
* [ ] Project Service creates a stable opaque project ID.
* [ ] Workspace Service reads only through its capability.
* [ ] Workspace Snapshot has exact file hashes.
* [ ] Configuration Service produces an immutable Effective Snapshot.
* [ ] Product defaults are loaded from packaged definitions.
* [ ] User Profile values are loaded from service-owned SQLite.
* [ ] Project settings are optional.
* [ ] Invalid project settings remain visible without partial application.
* [ ] Project Open Receipt is committed transactionally.
* [ ] Effective Configuration Receipt is committed transactionally.
* [ ] Trust Evidence Service ingests both receipts.
* [ ] Trust Centre shows authority and provenance.
* [ ] Desktop crash does not stop Runtime.
* [ ] Runtime crash is detected and recovered.
* [ ] SQLite migrations run.
* [ ] Logs and traces contain no project content.
* [ ] Unit, integration and adversarial tests run in CI.
* [ ] A local Recovery Point can be created and structurally verified.
* [ ] A second launch restores the project registration safely.
* [ ] All errors are actionable.
* [ ] UI supports keyboard navigation and Narrator for this slice.

---

# 9. Vertical Slice Demonstration Script

The milestone demonstration should follow this exact sequence.

1. Start from a clean Windows user profile.
2. Install or run the Development package.
3. Launch Opure Desktop.
4. Show Runtime supervision and service health.
5. Open a small Git repository.
6. Show path identity and project registration.
7. Show Workspace Snapshot contents and hashes.
8. Show Effective Configuration values and provenance.
9. Show Trust Centre project-open timeline.
10. Edit `.opure\project.settings.json` externally.
11. Show reconciliation and a new Configuration Snapshot.
12. Introduce invalid JSON.
13. Show last-known-good configuration retained.
14. Restore valid JSON.
15. Close Desktop while Runtime remains active.
16. Reopen Desktop and reconnect.
17. Terminate Runtime unexpectedly.
18. Show supervisor recovery.
19. Create a local Recovery Point.
20. Verify the Recovery Point.
21. Close and reopen the project.
22. Show identical project identity, configuration revision and evidence chain.

---

# 10. Programme Phases

The programme is divided into twelve phases.

| Phase | Name                                 | Primary outcome                                 |
| ----- | ------------------------------------ | ----------------------------------------------- |
| 0     | Repository and Engineering Bootstrap | Buildable trusted repository                    |
| 1     | Runtime Skeleton                     | Supervised local control plane                  |
| 2     | IPC and Service Contracts            | Authenticated typed communication               |
| 3     | Persistence and Observability        | Durable service state and diagnostics           |
| 4     | Project and Filesystem Boundary      | Safe project registration and reads             |
| 5     | Configuration and Trust Centre       | Effective configuration and visible evidence    |
| 6     | First Vertical Slice Hardening       | Founder-approved foundation proof               |
| 7     | Controlled Mutation                  | Patches, tools and reversible writes            |
| 8     | Local Intelligence                   | Local models, context, knowledge and memory     |
| 9     | Remote and Extension Boundaries      | Providers, plugins and MCP                      |
| 10    | Durable Workflows                    | Checkpointed orchestration and recovery         |
| 11    | Productisation and Resilience        | Packaging, updates, support, backup and release |

---

# 11. Phase 0 — Repository and Engineering Bootstrap

## 11.1 Objective

Create a reproducible repository and engineering system before feature work begins.

---

## 11.2 ADR Coverage

* ADR-0001
* ADR-0008
* ADR-0010
* ADR-0011
* ADR-0012

---

## 11.3 Deliverables

* `Opure.slnx`;
* root `Directory.Build.props`;
* root `Directory.Build.targets`;
* root `Directory.Packages.props`;
* root `global.json`;
* root `version.json`;
* locked NuGet restore;
* repository-local tools;
* PowerShell build entry points;
* CI pipeline;
* test projects;
* code-analysis rules;
* editor settings;
* source-link configuration;
* SBOM skeleton;
* architecture-test project;
* and contribution documentation.

---

## 11.4 Suggested Repository Root

```text
C:\Opure\
├── adr\
├── specs\
├── schemas\
├── src\
├── tests\
├── tools\
├── build\
├── eng\
├── packaging\
├── enterprise\
├── samples\
├── docs\
├── Directory.Build.props
├── Directory.Build.targets
├── Directory.Packages.props
├── global.json
├── version.json
├── Opure.slnx
└── README.md
```

---

## 11.5 Initial Projects

```text
src/
├── Bootstrap/
│   ├── Opure.Bootstrap.Contracts/
│   └── Opure.Bootstrap.Windows/
├── Runtime/
│   ├── Opure.Runtime.Contracts/
│   ├── Opure.Runtime/
│   └── Opure.Runtime.Windows/
├── Desktop/
│   ├── Opure.Desktop.Contracts/
│   └── Opure.Desktop/
├── Platform/
│   ├── Opure.Platform.Contracts/
│   ├── Opure.Platform.Windows/
│   └── Opure.Platform.Security/
├── Persistence/
│   ├── Opure.Persistence.Contracts/
│   └── Opure.Persistence.Sqlite/
├── Observability/
│   ├── Opure.Observability.Contracts/
│   └── Opure.Observability/
└── Trust/
    ├── Opure.Trust.Contracts/
    └── Opure.Trust.Evidence.Service/
```

---

## 11.6 Build Commands

Required initial commands:

```powershell
.\build.ps1 restore
.\build.ps1 build
.\build.ps1 test
.\build.ps1 verify
.\build.ps1 package
```

---

## 11.7 Quality Rules

* warnings as errors for first-party code;
* nullable enabled;
* implicit usings reviewed;
* deterministic builds;
* lock files;
* no floating package versions;
* no package downgrade warnings;
* no unauthorised transitive dependencies;
* and architecture tests for project references.

---

## 11.8 Exit Criteria

* [ ] Clean clone builds on Windows hosted CI.
* [ ] Clean clone tests on Windows and Ubuntu where applicable.
* [ ] Locked restore succeeds.
* [ ] Version metadata is generated from one source.
* [ ] Package graph is documented.
* [ ] No production project references a test project.
* [ ] Core contracts do not reference Windows implementation assemblies.
* [ ] Build output is deterministic within defined limits.
* [ ] SBOM can be generated.
* [ ] CI permissions are minimal.
* [ ] Founder can run one build command locally.

---

# 12. Phase 1 — Runtime Skeleton

## 12.1 Objective

Create the trusted local process topology and supervision model.

---

## 12.2 ADR Coverage

* ADR-0002
* ADR-0003
* ADR-0013

---

## 12.3 Deliverables

* Bootstrap executable;
* Runtime executable;
* Desktop executable;
* service process contract;
* service registry;
* service lifecycle state machine;
* Runtime boot identity;
* Process Supervisor;
* Windows Job Object integration;
* crash detection;
* restart policy;
* health reporting;
* and Development channel isolation.

---

## 12.4 Initial Process Topology

```text
Opure.Bootstrap
    ├── Opure.Runtime
    │   ├── in-process trusted foundational services
    │   └── supervised worker processes
    └── Opure.Desktop
```

---

## 12.5 Runtime Service States

* Registered;
* Starting;
* Ready;
* Degraded;
* Stopping;
* Stopped;
* Failed;
* Restarting;
* Quarantined;
* and Disabled.

---

## 12.6 Crash Policy

Foundational services:

* limited automatic restart;
* exponential backoff;
* restart budget;
* visible failure;
* and Safe Mode after repeated failure.

---

## 12.7 Desktop Independence

Desktop should reconnect to a running Runtime.

Desktop closure should not destroy durable Runtime state.

---

## 12.8 Exit Criteria

* [ ] Bootstrap starts Runtime.
* [ ] Bootstrap starts Desktop.
* [ ] Runtime boot identity is unique.
* [ ] Service registry is queryable.
* [ ] Job Object terminates orphaned child workers.
* [ ] Desktop can close independently.
* [ ] Runtime crash is detected.
* [ ] Restart budget works.
* [ ] Repeated crash enters Safe Mode.
* [ ] Health state is visible.
* [ ] Development channel data is isolated.
* [ ] No plugin or external process is loaded.

---

# 13. Phase 2 — IPC and Service Contracts

## 13.1 Objective

Create authenticated, typed and versioned local communication.

---

## 13.2 ADR Coverage

* ADR-0004
* ADR-0003
* ADR-0007

---

## 13.3 Deliverables

* protobuf contracts;
* gRPC over named pipes;
* named-pipe ACLs;
* service session handshake;
* mutual session authentication;
* message-size limits;
* deadline propagation;
* cancellation;
* trace context;
* error model;
* compatibility policy;
* and IPC test harness.

---

## 13.4 Handshake

Conceptual:

```text
Client connects to exact named pipe
    ↓
Windows identity and pipe ACL checked
    ↓
Runtime issues session challenge
    ↓
Client proves bootstrap-issued session material
    ↓
Service and contract revisions negotiated
    ↓
Authenticated session established
```

---

## 13.5 Contract Rules

Every operation declares:

* request schema;
* response schema;
* service revision;
* deadline;
* cancellation behaviour;
* idempotency;
* maximum payload;
* authority requirement;
* and error categories.

---

## 13.6 No Generic Command Bus

Use typed contracts.

---

## 13.7 Exit Criteria

* [ ] Named-pipe gRPC works between Desktop and Runtime.
* [ ] Pipe ACL is explicit.
* [ ] Another Windows user is denied.
* [ ] Unauthenticated local process is denied.
* [ ] Session replay is denied.
* [ ] Contract revision mismatch is actionable.
* [ ] Cancellation propagates.
* [ ] Deadlines work.
* [ ] Message limits work.
* [ ] Trace context propagates.
* [ ] Error categories are stable.
* [ ] Fuzzed malformed messages do not crash Runtime.

---

# 14. Phase 3 — Persistence and Observability

## 14.1 Objective

Create reusable durable-service and diagnostics primitives.

---

## 14.2 ADR Coverage

* ADR-0005
* ADR-0006
* ADR-0007
* ADR-0027

---

## 14.3 Deliverables

* service-owned SQLite database helper;
* migration runner;
* WAL setup;
* foreign-key enforcement;
* transaction helper;
* outbox;
* inbox;
* content-addressed store;
* backup adapter hooks;
* structured logging;
* ActivitySource;
* Meter;
* local log rotation;
* redaction library;
* Trust Evidence envelope;
* Trust Evidence ingestion;
* and Trust Centre query contract.

---

## 14.4 Persistence Baseline

Every service database must:

* enable foreign keys;
* use explicit migrations;
* use durable transactions;
* own one writer;
* expose health;
* expose backup checkpoint;
* and support integrity checks.

---

## 14.5 Transactional Outbox

Required before any domain service can claim durable evidence publication.

---

## 14.6 Trust Evidence Bootstrap Types

Initial types:

* runtime.started;
* runtime.stopped;
* service.state-changed;
* configuration.snapshot-committed;
* project.opened;
* project.closed;
* workspace.snapshot-created;
* backup.recovery-point-created;
* and security.policy-denied.

---

## 14.7 Exit Criteria

* [ ] Service database can create and migrate.
* [ ] WAL is enabled where selected.
* [ ] Foreign keys are enforced.
* [ ] Transaction rollback works.
* [ ] Outbox survives crash.
* [ ] Inbox deduplicates.
* [ ] CAS stores immutable object.
* [ ] CAS verifies hash.
* [ ] Logs rotate.
* [ ] Logs exclude seeded secrets.
* [ ] Traces correlate IPC.
* [ ] Metrics reject high-cardinality labels.
* [ ] Trust Evidence Record validates.
* [ ] Trust Evidence ingestion is idempotent.
* [ ] Trust Centre query returns typed records.
* [ ] Database backup adapter prototype works.

---

# 15. Phase 4 — Project and Filesystem Boundary

## 15.1 Objective

Open projects safely and represent filesystem access through trusted capabilities.

---

## 15.2 ADR Coverage

* ADR-0009
* ADR-0010
* ADR-0022

---

## 15.3 Deliverables

* Project Service;
* Workspace Service;
* typed path references;
* project root selection;
* handle-based identity;
* reparse-point controls;
* alternate-stream controls;
* file-ID verification;
* repository discovery;
* project registration;
* immutable Workspace Snapshots;
* file hashing;
* change reconciliation;
* and project lifecycle evidence.

---

## 15.4 Project Identity

Project identity should not be a path string.

It should bind:

* opaque project ID;
* filesystem identity;
* root handle identity;
* repository identity where available;
* and channel.

---

## 15.5 Workspace Snapshot

Initial snapshot should include:

* project metadata;
* repository state;
* selected file inventory;
* file hashes;
* file sizes;
* logical paths;
* and generation.

It should not index full project contents yet.

---

## 15.6 Read Policy

Workspace reads should require:

* project capability;
* logical path;
* operation purpose;
* size limit;
* and data classification.

---

## 15.7 Exit Criteria

* [ ] Project picker returns verified root.
* [ ] Drive-relative paths are denied.
* [ ] UNC policy is explicit.
* [ ] Device paths are denied.
* [ ] Traversal is denied.
* [ ] Reparse escape is denied.
* [ ] Alternate streams are denied.
* [ ] Project ID survives restart.
* [ ] Same path with different filesystem identity is detected.
* [ ] Repository identity is recorded.
* [ ] Workspace Snapshot is immutable.
* [ ] File hashes are reproducible.
* [ ] Changed files create a new generation.
* [ ] Deleted files are represented.
* [ ] Project open and snapshot receipts appear in Trust Centre.
* [ ] Project content does not appear in logs.

---

# 16. Phase 5 — Configuration and Trust Centre

## 16.1 Objective

Resolve typed configuration and show authoritative project evidence.

---

## 16.2 ADR Coverage

* ADR-0026
* ADR-0027
* ADR-0006

---

## 16.3 Deliverables

* Setting Definition catalogue;
* Policy Definition catalogue;
* Product defaults;
* User Base Profile;
* project settings document;
* strict JSON parser;
* duplicate-key detection;
* local schema registry;
* Effective Configuration Snapshot;
* per-key provenance;
* Configuration Change Transaction;
* Trust Centre project view;
* Trust Centre operation view;
* and backup health shell.

---

## 16.4 Initial Settings

Limit initial catalogue to settings required by the first slice.

Suggested:

```text
desktop.theme
desktop.diagnostics.show-advanced
runtime.startup.safe-mode
workspace.maximum-file-size
workspace.follow-reparse-points
workspace.include-hidden-files
logging.minimum-level
logging.retention-days
trust.retention-days
backup.local-recovery.enabled
project.cloud.policy
```

---

## 16.5 Initial Policies

Suggested:

```text
policy.product.no-secret-in-config
policy.product.no-project-capability-grants
policy.product.cloud-default-local
policy.product.no-remote-schema
policy.product.no-plugin-execution
policy.product.no-mcp-execution
policy.product.no-provider-call
```

---

## 16.6 Trust Centre First Views

* Runtime;
* Services;
* Projects;
* Configuration;
* Workspace;
* Security;
* and Backup.

---

## 16.7 Exit Criteria

* [ ] Setting Definitions validate.
* [ ] Policy Definitions validate.
* [ ] Product defaults load.
* [ ] User Profile persists.
* [ ] Project settings load from `.opure\project.settings.json`.
* [ ] Duplicate JSON keys fail.
* [ ] Comments fail.
* [ ] Trailing commas fail.
* [ ] Remote `$ref` is denied.
* [ ] Invalid project file does not replace last valid state.
* [ ] Effective Snapshot is immutable.
* [ ] Per-key provenance is visible.
* [ ] Product Policy cannot be weakened.
* [ ] Configuration changes are transactional.
* [ ] Trust Centre shows project and configuration receipts.
* [ ] Evidence completeness is visible.
* [ ] Search and filtering work.
* [ ] Accessibility tests pass for initial views.

---

# 17. Phase 6 — First Vertical Slice Hardening

## 17.1 Objective

Turn the initial slice into a founder-approved architectural proof.

---

## 17.2 Deliverables

* end-to-end automated test;
* crash-recovery test;
* IPC attack test;
* filesystem adversarial test;
* configuration adversarial test;
* evidence-reconciliation test;
* performance benchmark;
* local Recovery Point;
* installation development package;
* founder demonstration;
* and ADR evidence updates.

---

## 17.3 No New Capability Rule

No AI, plugins, MCP or command execution may enter this phase.

---

## 17.4 Exit Criteria

* [ ] Demonstration script passes.
* [ ] Runtime restart loses no committed project state.
* [ ] Desktop restart reconnects.
* [ ] Project identity remains correct.
* [ ] Invalid settings do not partially apply.
* [ ] Trust Evidence gap reconciliation works.
* [ ] Seeded secrets do not appear in logs or evidence.
* [ ] First project opens under target latency.
* [ ] Recovery Point restores into staged root.
* [ ] Founder reviews architectural proof.
* [ ] ADR-0003, ADR-0004, ADR-0005, ADR-0006, ADR-0009, ADR-0026 and ADR-0027 have prototype evidence.
* [ ] Decision made whether Avalonia remains selected.
* [ ] Decision made whether service grouping remains appropriate.

---

# 18. Founder Gate A — Foundation Proof

The founder should answer:

1. Does the Desktop and Runtime separation feel understandable?
2. Does project opening feel fast enough?
3. Is the Trust Centre useful rather than noisy?
4. Is configuration provenance understandable?
5. Are errors actionable?
6. Does recovery behaviour inspire confidence?
7. Is Avalonia retained?
8. Is the service topology retained?
9. Is the first slice acceptable as the base for controlled mutation?

The gate outcome should be:

* Accept;
* Accept with Amendments;
* Repeat Phase 6;
* or Replace a provisional architectural choice.

---

# 19. Phase 7 — Controlled Mutation

## 19.1 Objective

Introduce deterministic, reviewable and reversible project mutations.

---

## 19.2 ADR Coverage

* ADR-0009
* ADR-0018
* ADR-0025
* ADR-0027

---

## 19.3 Deliverables

* Patch Service;
* patch schema;
* diff renderer;
* patch review;
* Workspace staged write;
* atomic replacement;
* source precondition hashes;
* Tool Mediation Service;
* command template catalogue;
* command approval;
* restricted execution worker;
* stdout and stderr policy;
* cancellation;
* execution receipts;
* verification step;
* and rollback or compensation.

---

## 19.4 Initial Mutation Scope

The first mutation capability should be deliberately narrow:

```text
Create or replace one UTF-8 text file
inside one open project
after exact preview and developer approval.
```

The next capability should be:

```text
Apply one unified patch
against exact source hashes
through Workspace staged writes.
```

Command execution should follow only after file mutation is safe.

---

## 19.5 Patch State Machine

* Draft;
* Validating;
* Preview Ready;
* Approval Required;
* Approved;
* Applying;
* Applied;
* Verifying;
* Verified;
* Failed;
* Rolled Back;
* Compensated;
* and Cancelled.

---

## 19.6 Tool Execution Initial Scope

Allow only curated read-only commands first.

Suggested:

* `git status`;
* `git diff --stat`;
* `dotnet --info`;
* `dotnet build` against selected project;
* and test commands defined by trusted templates.

No arbitrary shell string.

---

## 19.7 Command Model

A command should be represented by:

* executable identity;
* argument array;
* working-directory capability;
* environment allowlist;
* timeout;
* input policy;
* output policy;
* resource budget;
* and effect class.

---

## 19.8 Exit Criteria

* [ ] Patch schema is versioned.
* [ ] Patch binds source hashes.
* [ ] Patch preview is exact.
* [ ] Changed source invalidates approval.
* [ ] Workspace writes through staged same-volume replacement.
* [ ] File identity is revalidated immediately before replacement.
* [ ] Reparse substitution is detected.
* [ ] Partial write is impossible.
* [ ] Patch receipt is authoritative.
* [ ] Tool templates are typed.
* [ ] Shell interpolation is absent.
* [ ] Working directory is capability bound.
* [ ] Environment is minimal.
* [ ] Timeout and cancellation work.
* [ ] Command output is bounded.
* [ ] Command output is excluded from logs by default.
* [ ] Exit receipt is authoritative.
* [ ] Verification step runs.
* [ ] Developer can inspect every effect in Trust Centre.
* [ ] Rollback or compensation is available where practical.

---

# 20. Founder Gate B — Controlled Mutation

The founder should review:

* patch clarity;
* approval friction;
* file safety;
* command visibility;
* error recovery;
* Trust Centre usefulness;
* and whether the product still feels developer controlled.

No AI-generated patch should be introduced before this gate passes.

---

# 21. Phase 8 — Local Intelligence

## 21.1 Objective

Add local AI capability behind the proven control plane.

---

## 21.2 ADR Coverage

* ADR-0020
* ADR-0021
* ADR-0022
* ADR-0023
* ADR-0024

---

## 21.3 Deliverables

* Local Model Service;
* Runtime Package management;
* model catalogue;
* model import;
* model verification;
* model execution profiles;
* GPU and CPU resource scheduling;
* Context Assembly Service;
* Context Plan;
* Project Knowledge indexing;
* Project Memory;
* local AI request receipts;
* evaluation harness;
* and AI Routing Service restricted to local routes.

---

## 21.4 Initial Local AI Use Case

The first local AI feature should be:

> **Explain selected code using an explicit developer-selected file and line range.**

The flow should be:

```text
Developer selects code
    ↓
Workspace produces exact source snapshot
    ↓
Context Assembly creates Context Plan
    ↓
Local-only Routing Decision
    ↓
Local Model Service produces response
    ↓
Response shown as proposal
    ↓
Trust Centre records source, model and execution profile
```

No automatic file selection.

No project-wide context.

No memory write.

No tool call.

No patch application.

---

## 21.5 Second Local AI Use Case

> **Propose a patch for an explicitly selected issue and context set.**

The response should enter the Patch Service as a proposal.

It should not write directly.

---

## 21.6 Local Model Initial Scope

* text generation;
* streaming;
* cancellation;
* bounded context;
* structured output;
* deterministic seed where supported;
* local embeddings;
* and one active model by default.

Multimodal, LoRA, speculative decoding and model training remain deferred.

---

## 21.7 Project Knowledge Initial Scope

Index:

* filenames;
* language;
* symbols;
* references;
* selected documentation;
* and local embeddings.

Do not index:

* secrets;
* ignored files by default;
* binary files;
* Vault content;
* support bundles;
* backup staging;
* or diagnostic dumps.

---

## 21.8 Project Memory Initial Scope

Support:

* developer-created notes;
* accepted decisions;
* project conventions;
* and reminders.

AI-generated memory remains a proposal requiring acceptance.

---

## 21.9 Exit Criteria

* [ ] Local model package is hash pinned.
* [ ] Native runtime package is isolated.
* [ ] Model source and licence are visible.
* [ ] Model loads in dedicated process.
* [ ] No network occurs during local inference.
* [ ] GPU and CPU budgets work.
* [ ] OOM handling is safe.
* [ ] Cancellation is prompt.
* [ ] Context Plan lists exact sources.
* [ ] No ambient workspace context exists.
* [ ] Project Knowledge honours Workspace policy.
* [ ] Secret canaries are excluded.
* [ ] Memory proposals require approval.
* [ ] Memory provenance is visible.
* [ ] Local Routing Decision is immutable.
* [ ] Model response is labelled untrusted proposal.
* [ ] AI cannot call Patch Service directly.
* [ ] Developer approval remains required for mutation.
* [ ] Performance is acceptable on reference hardware.
* [ ] Trust Centre shows local model identity and context provenance.

---

# 22. Founder Gate C — Local Intelligence

Review:

* model usefulness;
* response latency;
* resource use;
* Context Plan transparency;
* Knowledge quality;
* Memory usefulness;
* proposal versus authority distinction;
* and whether local AI improves work without obscuring control.

---

# 23. Phase 9 — Remote and Extension Boundaries

## 23.1 Objective

Add remote AI providers, plugins and MCP only after local control and evidence are proven.

---

## 23.2 ADR Coverage

* ADR-0016
* ADR-0017
* ADR-0018
* ADR-0019
* ADR-0024
* ADR-0027

---

## 23.3 Deliverables

* AI Router remote route;
* Provider Profiles;
* Network Gateway;
* Data Sharing Plan;
* provider credentials through Vault;
* exact provider adapters;
* plugin package installer;
* Plugin Host;
* plugin permission model;
* MCP Gateway;
* MCP server profiles;
* OAuth and token handling;
* capability leases;
* and Trust Centre views.

---

## 23.4 Remote AI Initial Use Case

The first remote use should mirror the proven local use case:

> **Explain explicitly selected code through an approved remote Provider Profile.**

The developer should see:

* provider;
* model;
* operator;
* region;
* selected source;
* byte and token estimate;
* data classifications;
* retention posture;
* cost estimate;
* and approval.

---

## 23.5 Provider Rollout Order

1. One direct commercial API.
2. One managed cloud model service.
3. One enterprise gateway.
4. Custom compatible endpoint.
5. Additional providers.

Do not implement many providers in parallel before the evidence model is proven.

---

## 23.6 Plugin Initial Use Case

A non-mutating plugin that contributes:

* a declarative panel;
* a project summary;
* or a bounded analyser result.

No project write.

No network.

No command.

No secret.

---

## 23.7 MCP Initial Use Case

One local stdio MCP server with one read-only tool.

It should have:

* exact executable identity;
* exact working directory;
* no network;
* no roots;
* no sampling;
* no tasks;
* and one bounded permission.

---

## 23.8 Exit Criteria

* [ ] Project default remains Local Only.
* [ ] Remote use requires approved policy.
* [ ] Provider Profile is immutable and revisioned.
* [ ] Data Handling Record is reviewed.
* [ ] Data Sharing Plan lists exact bytes and classes.
* [ ] Changed plan invalidates approval.
* [ ] Secrets enter only at Network Gateway.
* [ ] Provider call has exact receipt.
* [ ] No provider-side state is created initially.
* [ ] No remote tool use is enabled.
* [ ] No automatic cloud fallback exists.
* [ ] Plugin packages are quarantined before installation.
* [ ] Plugin code never runs during install.
* [ ] Plugin Host is isolated.
* [ ] Plugin permissions are deny by default.
* [ ] Plugin cannot access project filesystem directly.
* [ ] MCP Gateway owns MCP interactions.
* [ ] MCP server identity is exact.
* [ ] MCP credentials remain in Vault.
* [ ] MCP server cannot call Trust Evidence directly.
* [ ] Plugin and MCP evidence is authority labelled.
* [ ] Remote, plugin and MCP adversarial suites pass.

---

# 24. Founder Gate D — External Trust Boundaries

Review:

* remote data-sharing clarity;
* provider trust posture;
* approval experience;
* cost visibility;
* plugin installation trust;
* permission visibility;
* MCP identity and tool clarity;
* and whether external integrations remain understandable.

---

# 25. Phase 10 — Durable Workflows

## 25.1 Objective

Compose proven capabilities into durable, checkpointed and recoverable workflows.

---

## 25.2 ADR Coverage

* ADR-0025
* ADR-0018
* ADR-0019
* ADR-0024
* ADR-0026
* ADR-0027
* ADR-0028

---

## 25.3 Deliverables

* Workflow Definition schema;
* Compiled Plan;
* workflow event store;
* projections;
* checkpoints;
* timers;
* signals;
* approvals;
* workers;
* leases;
* side-effect intents;
* idempotency;
* reconciliation;
* compensation;
* migration;
* recovery;
* and workflow Trust Centre.

---

## 25.4 First Workflow

Suggested:

```text
Analyse selected project
    ↓
Create Context Plan
    ↓
Run local model explanation
    ↓
Propose patch
    ↓
Developer reviews
    ↓
Apply patch
    ↓
Run build
    ↓
Run tests
    ↓
Display result
```

Every step should already exist independently before orchestration.

---

## 25.5 Second Workflow

Suggested:

```text
Review pull-request branch locally
    ↓
Inspect changes
    ↓
Run static checks
    ↓
Run tests
    ↓
Generate review summary
    ↓
Developer decides what to publish
```

No automatic remote publication initially.

---

## 25.6 Workflow Safety Rules

* compiled deterministic plan;
* exact input revisions;
* exact capability per effect;
* approval hashes;
* idempotency keys;
* no automatic retry of ambiguous external effect;
* no plan mutation by model;
* and Recovery Required after uncertain restore.

---

## 25.7 Exit Criteria

* [ ] Workflow Definition validates.
* [ ] Compiled Plan is immutable.
* [ ] Event store is durable.
* [ ] Projection rebuild works.
* [ ] Checkpoints work.
* [ ] Worker leases expire safely.
* [ ] Duplicate delivery is idempotent.
* [ ] Approval binds exact plan.
* [ ] Tool and provider effects have intent before execution.
* [ ] Outcome Unknown is first class.
* [ ] Reconciliation works.
* [ ] Compensation is explicit.
* [ ] Cancellation works.
* [ ] Runtime crash recovery works.
* [ ] Backup restores workflows as Recovery Required.
* [ ] Configuration revisions are pinned.
* [ ] Plugin, MCP and provider revisions are pinned.
* [ ] Workflow Trust Centre timeline is complete.
* [ ] No workflow effect bypasses owner service.

---

# 26. Founder Gate E — Workflow Control

Review:

* workflow timeline;
* approval placement;
* cancellation;
* failure recovery;
* ambiguity handling;
* compensation;
* and whether workflows feel like visible developer procedures rather than autonomous agents.

---

# 27. Phase 11 — Productisation and Resilience

## 27.1 Objective

Turn the architecture proof into a distributable, supportable and recoverable product.

---

## 27.2 ADR Coverage

* ADR-0012
* ADR-0013
* ADR-0014
* ADR-0015
* ADR-0027
* ADR-0028
* all acceptance evidence.

---

## 27.3 Deliverables

* release train;
* MSIX packaging;
* signed binaries;
* signed package;
* update metadata;
* updater hand-off;
* migration recovery;
* Trust Centre support bundles;
* managed backup repositories;
* portable backups;
* restore tests;
* accessibility completion;
* performance optimisation;
* threat-model closure;
* privacy review;
* release candidate;
* and disaster-recovery exercise.

---

## 27.4 Version 1 Release Scope

Version 1 should include only capabilities that have:

* accepted architecture evidence;
* complete Trust Evidence;
* recovery behaviour;
* security tests;
* accessibility tests;
* and founder approval.

---

## 27.5 Release Candidate Gates

* no critical security finding;
* no critical data-loss finding;
* no unresolved secret leakage;
* no untested schema migration;
* no automatic external data sharing;
* no automatic workflow side effect;
* verified installation and uninstall;
* verified update and rollback;
* verified backup and restore;
* verified support bundle;
* signed release artefacts;
* and complete release evidence.

---

## 27.6 Exit Criteria

* [ ] MSIX installs per user.
* [ ] Package identity is stable.
* [ ] Stable and Preview are isolated.
* [ ] All executable content is signed.
* [ ] Signing keys are not exported.
* [ ] Version metadata is consistent.
* [ ] Update check is app controlled.
* [ ] Update hand-off is explicit.
* [ ] Update failure enters recovery.
* [ ] Support bundle is local and reviewed.
* [ ] No automatic support upload exists.
* [ ] Managed backup repository works.
* [ ] New-machine restore works.
* [ ] Disaster-recovery exercise passes.
* [ ] Accessibility review passes.
* [ ] Performance targets pass or are documented.
* [ ] Privacy review passes.
* [ ] Security review passes.
* [ ] Founder approves release candidate.

---

# 28. ADR Dependency Graph

The critical dependency chain is:

```text
ADR-0001 Language
    ↓
ADR-0010 Repository Structure
    ↓
ADR-0011 Build and CI
    ↓
ADR-0003 Process Topology
    ↓
ADR-0004 Local IPC
    ↓
ADR-0005 Persistence
    ↓
ADR-0006 Observability
    ↓
ADR-0009 Filesystem Boundary
    ↓
ADR-0026 Configuration and Policy
    ↓
ADR-0027 Trust Evidence
    ↓
ADR-0028 Backup and Recovery
```

The controlled mutation chain is:

```text
Filesystem Boundary
    ↓
Workspace Service
    ↓
Patch Service
    ↓
Tool Mediation
    ↓
Workflow Effects
```

The local intelligence chain is:

```text
Local Model Runtime
    ↓
Context Assembly
    ↓
Project Knowledge
    ↓
Project Memory
    ↓
AI Evaluation and Routing
```

The external integration chain is:

```text
Vault
    ↓
Network Gateway
    ↓
Provider Trust
    ↓
AI Router Remote Route
```

The extension chain is:

```text
Plugin Packaging
    ↓
Plugin Permissions
    ↓
Plugin Host
```

and:

```text
MCP Trust
    ↓
MCP Gateway
    ↓
MCP Tool Mediation
```

The workflow chain is:

```text
Configuration Snapshots
    ↓
Capability Services
    ↓
Trust Evidence
    ↓
Workflow Execution
    ↓
Backup and Recovery
```

---

# 29. ADR Implementation Order

Recommended implementation order:

| Order | ADR      | Implementation focus                     |
| ----: | -------- | ---------------------------------------- |
|     1 | ADR-0001 | C# and .NET baseline                     |
|     2 | ADR-0010 | repository and solution structure        |
|     3 | ADR-0011 | build and CI                             |
|     4 | ADR-0012 | versioning metadata                      |
|     5 | ADR-0008 | test platform                            |
|     6 | ADR-0003 | process topology                         |
|     7 | ADR-0004 | authenticated IPC                        |
|     8 | ADR-0005 | persistence primitives                   |
|     9 | ADR-0006 | observability                            |
|    10 | ADR-0007 | Vault skeleton                           |
|    11 | ADR-0009 | Windows filesystem boundary              |
|    12 | ADR-0026 | configuration and policy                 |
|    13 | ADR-0027 | Trust Evidence and Trust Centre          |
|    14 | ADR-0028 | local recovery-point skeleton            |
|    15 | ADR-0002 | Desktop framework evidence gate          |
|    16 | ADR-0013 | development MSIX                         |
|    17 | ADR-0014 | development signing path                 |
|    18 | ADR-0025 | workflow primitives required by mutation |
|    19 | ADR-0018 | tool and MCP capability foundations      |
|    20 | ADR-0020 | local model runtime                      |
|    21 | ADR-0021 | context assembly                         |
|    22 | ADR-0022 | project knowledge                        |
|    23 | ADR-0023 | project memory                           |
|    24 | ADR-0024 | AI evaluation and routing                |
|    25 | ADR-0019 | remote provider trust                    |
|    26 | ADR-0016 | plugin packaging                         |
|    27 | ADR-0017 | plugin permissions                       |
|    28 | ADR-0018 | full MCP gateway                         |
|    29 | ADR-0015 | updater                                  |
|    30 | ADR-0027 | support bundles                          |
|    31 | ADR-0028 | full backup and disaster recovery        |

Some ADRs appear more than once because a small foundation should be built early and the complete feature later.

---

# 30. Critical Path

The shortest path to a trustworthy product proof is:

```text
Repository
→ Build
→ Runtime
→ IPC
→ Persistence
→ Observability
→ Filesystem
→ Project
→ Configuration
→ Trust Centre
→ Recovery Point
→ First Vertical Slice Gate
```

The shortest path to useful AI is:

```text
First Vertical Slice Gate
→ Controlled Mutation
→ Local Model Runtime
→ Context Assembly
→ Local AI Explanation
→ AI Patch Proposal
→ Patch Review
```

The shortest path to remote AI is:

```text
Local AI Proof
→ Vault Credentials
→ Network Gateway
→ Provider Profile
→ Data Sharing Plan
→ Remote Route
```

The shortest path to durable workflows is:

```text
Independent Proven Capabilities
→ Workflow Event Store
→ Compiled Plan
→ Approval and Effect Protocol
→ Recovery
```

---

# 31. Parallel Workstreams

After Phase 2, work may proceed in controlled parallel streams.

## 31.1 Workstream A — Platform

* Runtime;
* IPC;
* persistence;
* observability;
* service health;
* and process supervision.

---

## 31.2 Workstream B — Project

* Project Service;
* Workspace;
* filesystem;
* repository;
* patches;
* and command mediation.

---

## 31.3 Workstream C — Governance

* configuration;
* policy;
* Vault;
* Trust Evidence;
* approvals;
* and audit views.

---

## 31.4 Workstream D — Intelligence

Begins after Gate A:

* local models;
* Context;
* Knowledge;
* Memory;
* evaluation;
* and routing.

---

## 31.5 Workstream E — Extensions

Begins after Gate C:

* providers;
* plugins;
* MCP;
* and extension trust.

---

## 31.6 Workstream F — Resilience

Starts early with skeletons and deepens throughout:

* backup adapters;
* recovery points;
* migration recovery;
* support bundles;
* restore;
* and exercises.

---

# 32. Parallelisation Rules

Parallel work is allowed only when:

* contracts are stable enough;
* one stream does not invent another stream's authority;
* integration tests exist;
* schema versions are pinned;
* and founder gates are not bypassed.

Do not parallelise:

* several provider adapters before one is complete;
* several local model backends before one is qualified;
* several plugin host designs;
* several workflow engines;
* or cloud backup and local backup simultaneously.

---

# 33. Twelve-Week Foundation Plan

The first twelve weeks should target Founder Gate A.

The schedule assumes a small founder-led engineering effort.

It is outcome based rather than an estimate guarantee.

---

# 34. Week 1 — Repository and Build

## Goals

* create repository structure;
* create `Opure.slnx`;
* pin .NET SDK;
* create central package management;
* create build scripts;
* create CI;
* create first tests.

---

## Deliverables

* buildable empty applications;
* deterministic version metadata;
* CI restore, build and test;
* architecture reference tests;
* local developer setup guide.

---

## Founder Review

Confirm:

* repository naming;
* project grouping;
* build command;
* and source layout.

---

# 35. Week 2 — Bootstrap, Runtime and Desktop Skeleton

## Goals

* start Bootstrap;
* start Runtime;
* start Desktop;
* establish process identities;
* create service registry;
* create basic health UI.

---

## Deliverables

* running three-process prototype;
* Job Object supervision;
* Runtime restart;
* Desktop reconnect shell;
* Development channel data root.

---

## Evidence

* process lifecycle trace;
* crash test;
* orphan cleanup test;
* service-state screenshot or automated UI evidence.

---

# 36. Week 3 — Named-Pipe gRPC

## Goals

* define contracts;
* establish named-pipe transport;
* implement authentication handshake;
* implement deadlines and cancellation;
* propagate traces.

---

## Deliverables

* Desktop-to-Runtime health call;
* Runtime-to-service call;
* malformed-message tests;
* another-user denial test.

---

## Founder Review

Demonstrate local communication and failure messaging.

---

# 37. Week 4 — Persistence, Outbox and Observability

## Goals

* SQLite helper;
* migrations;
* WAL;
* outbox and inbox;
* structured logging;
* ActivitySource;
* Meter;
* redaction.

---

## Deliverables

* one sample service database;
* crash-safe outbox;
* local log rotation;
* trace viewer or Trust debug screen;
* secret-canary tests.

---

# 38. Week 5 — Project Service and Filesystem Identity

## Goals

* trusted folder picker;
* typed project root;
* handle verification;
* project registration;
* project database;
* project-open receipt.

---

## Deliverables

* safe open-project flow;
* stable project ID;
* reparse and traversal tests;
* project list UI.

---

## Founder Review

Open several representative repositories and non-repository folders.

---

# 39. Week 6 — Workspace Snapshot

## Goals

* file inventory;
* logical paths;
* file hashes;
* generation;
* change detection;
* repository metadata;
* bounded reads.

---

## Deliverables

* immutable Workspace Snapshot;
* change reconciliation;
* Trust receipt;
* project summary UI.

---

# 40. Week 7 — Configuration Definitions

## Goals

* Setting Definitions;
* Policy Definitions;
* product defaults;
* User Base Profile;
* Configuration Service database;
* strict JSON parser.

---

## Deliverables

* initial definitions;
* user preference editor;
* duplicate-key rejection;
* schema registry;
* policy enforcement tests.

---

# 41. Week 8 — Project Configuration and Effective Snapshot

## Goals

* `.opure\project.settings.json`;
* Workspace acquisition;
* validation;
* merge;
* Product Policy;
* Effective Snapshot;
* provenance.

---

## Deliverables

* requested versus effective view;
* invalid edit handling;
* last-known-good state;
* configuration receipts.

---

## Founder Review

Assess configuration transparency.

---

# 42. Week 9 — Trust Evidence Service

## Goals

* Evidence Type catalogue;
* owner outboxes;
* Trust inbox;
* relationships;
* completeness;
* query API;
* retention skeleton.

---

## Deliverables

* project-open evidence;
* workspace evidence;
* configuration evidence;
* Runtime and service evidence;
* gap reconciliation prototype.

---

# 43. Week 10 — Trust Centre UI

## Goals

* Overview;
* Project;
* Configuration;
* Runtime;
* evidence detail;
* authority labels;
* completeness;
* search;
* accessibility.

---

## Deliverables

* causal project-open timeline;
* per-key configuration provenance;
* service health;
* retention view shell.

---

## Founder Review

Assess usefulness and information density.

---

# 44. Week 11 — Recovery Point and Hardening

## Goals

* Backup Adapter skeleton;
* SQLite Online Backup;
* local Recovery Point;
* structural verification;
* staged restore test;
* crash and power-failure tests.

---

## Deliverables

* create Recovery Point;
* verify Recovery Point;
* restore into disposable root;
* Backup Health view.

---

# 45. Week 12 — Founder Gate A

## Goals

* run full demonstration;
* run adversarial suite;
* benchmark;
* record architecture evidence;
* close critical defects;
* decide provisional choices.

---

## Deliverables

* end-to-end demonstration;
* Gate A report;
* ADR evidence matrix;
* performance baseline;
* security baseline;
* recovery baseline;
* and Phase 7 plan.

---

# 46. Twelve-Week Success Criteria

At the end of Week 12:

* Opure can be built from a clean clone;
* Desktop and Runtime are supervised;
* named-pipe IPC is authenticated;
* project paths are verified;
* Workspace Snapshot is immutable;
* Effective Configuration is explainable;
* Trust Evidence is authoritative and visible;
* invalid external edits fail safely;
* crashes recover;
* local Recovery Points work;
* and Founder Gate A has a recorded decision.

---

# 47. Post-Foundation Indicative Sequence

After Gate A:

| Block       | Indicative focus                           |
| ----------- | ------------------------------------------ |
| Weeks 13–16 | Patch Service and controlled writes        |
| Weeks 17–20 | Tool Mediation and build/test commands     |
| Weeks 21–26 | Local Model Service and first local AI     |
| Weeks 27–32 | Context, Knowledge and Memory              |
| Weeks 33–36 | AI Evaluation and Routing                  |
| Weeks 37–40 | First remote provider                      |
| Weeks 41–44 | Plugin Host and one read-only plugin       |
| Weeks 45–48 | MCP Gateway and one local server           |
| Weeks 49–56 | Durable workflows                          |
| Weeks 57–64 | Installer, signing, updater and support    |
| Weeks 65–72 | Full backup, restore and release hardening |

This is a sequencing aid, not a fixed promise.

---

# 48. Milestone Catalogue

## M0 — Repository Builds

Outcome:

* reproducible engineering baseline.

---

## M1 — Runtime Lives

Outcome:

* supervised Desktop and Runtime.

---

## M2 — Services Communicate

Outcome:

* authenticated local IPC.

---

## M3 — State Persists

Outcome:

* migrations, outbox, logs and Trust receipt.

---

## M4 — Project Opens Safely

Outcome:

* verified project identity and Workspace Snapshot.

---

## M5 — Configuration Is Explainable

Outcome:

* immutable Effective Snapshot and provenance.

---

## M6 — Foundation Is Visible

Outcome:

* Trust Centre and recovery proof.

---

## M7 — Mutation Is Controlled

Outcome:

* reviewed patch and command effects.

---

## M8 — Local AI Is Useful

Outcome:

* local explanation and patch proposal.

---

## M9 — External Boundaries Are Governed

Outcome:

* one provider, one plugin and one MCP integration.

---

## M10 — Workflows Recover

Outcome:

* durable orchestration without hidden autonomy.

---

## M11 — Release Candidate Is Recoverable

Outcome:

* signed packaged supportable product with tested restore.

---

# 49. Milestone Evidence Package

Every milestone should produce:

* release build;
* test report;
* security report;
* performance measurements;
* accessibility evidence;
* recovery evidence;
* Trust Centre evidence;
* known limitations;
* ADR acceptance evidence;
* founder decision;
* and next-milestone plan.

---

# 50. Architecture Evidence Matrix

| ADR      | Earliest milestone | Required evidence focus                    |
| -------- | ------------------ | ------------------------------------------ |
| ADR-0001 | M0                 | language and SDK build proof               |
| ADR-0002 | M6                 | Avalonia prototype and fallback comparison |
| ADR-0003 | M1–M6              | process topology and crash recovery        |
| ADR-0004 | M2                 | named-pipe security and compatibility      |
| ADR-0005 | M3–M6              | SQLite, migrations, outbox and backup      |
| ADR-0006 | M3–M6              | local telemetry and redaction              |
| ADR-0007 | M3, M9, M11        | Vault boundaries and recovery              |
| ADR-0008 | M0 onward          | test strategy evidence                     |
| ADR-0009 | M4, M7             | path and write adversarial tests           |
| ADR-0010 | M0                 | repository architecture tests              |
| ADR-0011 | M0                 | CI and supply-chain controls               |
| ADR-0012 | M0, M11            | version and release train                  |
| ADR-0013 | M6, M11            | MSIX install and data layout               |
| ADR-0014 | M11                | signing and trust                          |
| ADR-0015 | M11                | update and recovery                        |
| ADR-0016 | M9                 | plugin package validation                  |
| ADR-0017 | M9                 | capability leases and isolation            |
| ADR-0018 | M7, M9             | tool and MCP mediation                     |
| ADR-0019 | M9                 | provider data sharing                      |
| ADR-0020 | M8                 | local runtime and resource evidence        |
| ADR-0021 | M8                 | Context Plan transparency                  |
| ADR-0022 | M8                 | indexing and retrieval quality             |
| ADR-0023 | M8                 | memory lifecycle and provenance            |
| ADR-0024 | M8–M9              | evaluation and routing                     |
| ADR-0025 | M7, M10            | workflow effects and recovery              |
| ADR-0026 | M5–M6              | configuration and policy                   |
| ADR-0027 | M3–M11             | evidence, retention and support            |
| ADR-0028 | M6–M11             | recovery points, restore and exercises     |

---

# 51. Provisional ADR Acceptance Policy

An ADR may move from Proposed to Accepted when:

* selected prototype exists;
* required tests pass;
* known limitations are recorded;
* performance evidence exists;
* security review exists;
* integration evidence exists;
* alternatives remain fairly represented;
* founder decision is recorded;
* and no unresolved release-gate condition remains.

Acceptance does not require every deferred feature.

It requires evidence for the selected boundary.

---

# 52. Decision Reversal Policy

A provisional choice should be replaced when:

* prototype fails an acceptance gate;
* implementation complexity is materially higher than expected;
* security boundary cannot be enforced;
* performance is unacceptable;
* user experience conflicts with the Charter;
* or a simpler solution provides equivalent control.

A reversal should produce:

* comparison evidence;
* migration impact;
* updated ADR;
* and roadmap change.

---

# 53. Definition of Ready

A work item is Ready when:

* purpose is clear;
* owner service is known;
* applicable ADRs are linked;
* state ownership is known;
* inputs and outputs are typed;
* security classification is known;
* side effects are identified;
* Trust Evidence is defined;
* failure modes are defined;
* tests are identified;
* recovery is defined;
* and dependencies are available.

---

# 54. Definition of Done

A production capability is Done when:

* code is reviewed;
* contracts are versioned;
* persistence is migrated;
* configuration is typed;
* policy is enforced;
* errors are actionable;
* logs are redacted;
* traces are bounded;
* Trust Evidence exists;
* cancellation works;
* crash recovery works;
* backup behaviour is defined;
* accessibility is tested;
* security tests pass;
* performance is measured;
* documentation is updated;
* and founder-visible behaviour is demonstrated.

---

# 55. Service Definition of Done

Every trusted service must have:

* stable service ID;
* owner;
* process placement;
* IPC contract;
* authentication;
* health endpoint;
* database ownership;
* migration path;
* outbox;
* inbox where required;
* configuration section;
* policy dependencies;
* Trust Evidence types;
* metrics;
* logs;
* trace source;
* backup adapter;
* restore validator;
* Safe Mode behaviour;
* shutdown behaviour;
* and test suite.

---

# 56. UI Definition of Done

Every user-facing flow must have:

* keyboard path;
* Narrator labels;
* high-contrast support;
* progress;
* cancellation where possible;
* actionable errors;
* provenance;
* destructive-action preview;
* no colour-only meaning;
* no hidden network;
* no hidden side effect;
* and Trust Centre linkage where relevant.

---

# 57. Security Definition of Done

Every boundary must have:

* threat model;
* identity;
* authentication;
* authorisation;
* input bounds;
* path checks;
* secret policy;
* data classification;
* logging policy;
* failure policy;
* revocation;
* adversarial tests;
* and evidence.

---

# 58. Recovery Definition of Done

Every durable capability must define:

* crash recovery;
* process restart;
* database restore;
* backup inclusion;
* rebuildable state;
* migration;
* deletion;
* erasure;
* and Trust Evidence.

---

# 59. Performance Definition of Done

Every significant operation must define:

* expected latency;
* throughput where relevant;
* memory budget;
* disk budget;
* concurrency;
* cancellation latency;
* benchmark;
* reference hardware result;
* and degraded-mode behaviour.

---

# 60. Testing Pyramid

Recommended balance:

```text
Unit and property tests
    ↓
Contract and schema tests
    ↓
Service integration tests
    ↓
Multi-process integration tests
    ↓
Filesystem and Windows boundary tests
    ↓
Adversarial security tests
    ↓
Real-window UI tests
    ↓
Recovery and endurance tests
```

---

# 61. Required Continuous Integration Suites

## Pull Request

* restore;
* build;
* unit;
* contract;
* schema;
* architecture;
* formatting;
* analyzers;
* and fast security tests.

---

## Main Branch

Add:

* service integration;
* named-pipe integration;
* SQLite integration;
* filesystem tests;
* headless UI;
* package build;
* SBOM;
* and dependency audit.

---

## Nightly

Add:

* adversarial filesystem;
* fuzzing;
* secret canaries;
* process crash;
* backup verification;
* performance smoke;
* and real-window UI tests.

---

## Weekly

Add:

* endurance;
* full restore test;
* malicious archive;
* plugin and MCP isolation;
* provider contract tests where credentials are available;
* and dependency update evaluation.

---

## Release Candidate

Add:

* signed MSIX installation;
* update;
* rollback;
* uninstall;
* full recovery exercise;
* support bundle;
* accessibility;
* performance;
* privacy;
* and release attestation.

---

# 62. Test Environment Matrix

| Environment                   | Purpose                               |
| ----------------------------- | ------------------------------------- |
| Windows 11 reference hardware | primary product evidence              |
| Windows 11 low-memory VM      | degraded behaviour                    |
| Windows 11 clean VM           | install and new-machine recovery      |
| Windows 11 standard user      | permission boundary                   |
| Windows 11 second user        | named-pipe and data isolation         |
| Windows 11 offline            | local-first behaviour                 |
| Windows 11 network restricted | Network Gateway evidence              |
| Ubuntu CI                     | portable contracts and pure libraries |
| Development package           | fast iteration                        |
| Preview package               | upgrade and isolation                 |
| Stable package                | release evidence                      |

---

# 63. Reference Hardware Baseline

Primary:

```text
CPU: AMD Ryzen 9 5950X
Memory: 32 GB
GPU: NVIDIA RTX 5070 Ti 16 GB
OS: Windows 11
```

Also test:

* 16 GB RAM;
* integrated graphics;
* no supported GPU;
* slow SSD;
* low free disk;
* battery device;
* and offline environment.

---

# 64. Performance Mode Implementation Order

1. Balanced
2. Eco
3. Performance
4. Turbo

Balanced is the initial default.

Do not implement all tuning options before representative workloads exist.

---

# 65. Founder Review Cadence

Founder review should occur at:

* end of Week 1;
* end of Week 3;
* end of Week 5;
* end of Week 8;
* end of Week 10;
* Founder Gate A;
* and every later founder gate.

Reviews should focus on working software, not architecture slides alone.

---

# 66. Weekly Engineering Review

Review:

* completed vertical behaviour;
* failed tests;
* security findings;
* recovery findings;
* performance;
* ADR evidence;
* blockers;
* dependency changes;
* and next week's exact outcome.

---

# 67. Architecture Review Cadence

Formal architecture review occurs when:

* a new service is added;
* a process boundary changes;
* a persistence owner changes;
* a new external destination is introduced;
* a new executable format is accepted;
* a new secret path is introduced;
* a new automatic side effect is proposed;
* or an ADR release gate is challenged.

---

# 68. Risk Register

## Risk 1 — Excessive Architecture Before Product Proof

Mitigation:

* first vertical slice by Week 12;
* limited initial settings;
* limited initial Trust views;
* no AI before Gate A;
* and working demonstrations.

---

## Risk 2 — Too Many Services

Mitigation:

* logical service boundaries may share a process initially;
* maintain ownership contracts;
* split processes only for crash, trust or resource reasons.

---

## Risk 3 — Avalonia Limitations

Mitigation:

* focused prototype;
* accessibility evidence;
* native window and input tests;
* maintain Desktop contracts;
* retain WinUI 3 fallback.

---

## Risk 4 — Named-Pipe gRPC Complexity

Mitigation:

* narrow prototype in Week 3;
* explicit transport adapter;
* contract tests;
* fallback to another local transport only through ADR amendment.

---

## Risk 5 — Filesystem Security Complexity

Mitigation:

* Windows-specific boundary library;
* adversarial fixtures;
* typed handles;
* no scattered path logic.

---

## Risk 6 — Configuration Overengineering

Mitigation:

* begin with approximately ten settings;
* implement only required merge strategies;
* expand catalogue from real needs.

---

## Risk 7 — Trust Centre Noise

Mitigation:

* authority classes;
* useful default views;
* evidence grouping;
* founder review;
* and no raw log dump as the primary experience.

---

## Risk 8 — Local Model Performance

Mitigation:

* one backend;
* one model family;
* reference hardware benchmarks;
* bounded context;
* no multimodal initially.

---

## Risk 9 — Remote Provider Scope Explosion

Mitigation:

* one provider first;
* common Router contract;
* no stateful provider features;
* no provider tools.

---

## Risk 10 — Plugin and MCP Security

Mitigation:

* delay until core mediation works;
* one read-only example;
* no direct filesystem, network or Vault;
* strict host isolation.

---

## Risk 11 — Workflow Engine Complexity

Mitigation:

* orchestrate existing proven capabilities;
* event model before designer;
* no arbitrary code nodes;
* no model-authored runtime mutation.

---

## Risk 12 — Backup Cryptography Risk

Mitigation:

* independent security review;
* known-answer tests;
* versioned format;
* no cloud service;
* limited first release scope.

---

## Risk 13 — Founder Capacity

Mitigation:

* explicit gates;
* narrow milestones;
* working slices;
* ADRs as decision support;
* avoid parallel speculative systems.

---

# 69. Programme Anti-Patterns

Do not:

* start with a chatbot UI;
* implement many agents;
* build provider adapters before local control;
* let AI read the whole repository automatically;
* let plugins run in Desktop;
* use logs as authoritative history;
* put all state in one database;
* expose arbitrary shell execution;
* store secrets in settings;
* use a universal last-writer-wins configuration stack;
* implement workflow designer before workflow recovery;
* ship update before rollback;
* call a copied folder a backup;
* or call encrypted bytes a tested recovery.

---

# 70. Scope Control

Every proposed feature should be classified:

* Foundation Required;
* Vertical Slice Required;
* Gate Required;
* Version 1 Candidate;
* Post-Version 1;
* Research;
* or Rejected.

A Version 1 feature must identify:

* user outcome;
* owner;
* dependencies;
* side effects;
* evidence;
* recovery;
* and cost.

---

# 71. Technical Debt Policy

Technical debt may be accepted when:

* boundary remains correct;
* debt is visible;
* security is not weakened;
* data format remains migratable;
* recovery is not compromised;
* and a removal condition exists.

Do not accept debt that:

* bypasses owner services;
* introduces shared database writes;
* leaks secrets;
* disables evidence;
* weakens filesystem checks;
* or creates hidden external effects.

---

# 72. Schema Evolution Policy

Every persisted schema requires:

* stable schema ID;
* revision;
* migration;
* validation;
* backward-read policy;
* forward-version error;
* fixture;
* and backup restore test.

---

# 73. Dependency Policy

A new dependency requires:

* purpose;
* licence;
* maintainer;
* release cadence;
* security posture;
* size;
* transitive graph;
* native code review;
* offline behaviour;
* and removal path.

High-risk dependencies include:

* native model runtimes;
* JSON Schema validators;
* archive libraries;
* cryptographic libraries;
* browser engines;
* and shell parsers.

---

# 74. Native Code Policy

Native code requires:

* exact version or commit;
* source;
* build flags;
* target architectures;
* hash;
* licence;
* SBOM;
* signature;
* isolation;
* crash tests;
* and update policy.

---

# 75. Security Review Gates

Security review is mandatory before:

* first project write;
* first command execution;
* first provider call;
* first plugin execution;
* first MCP execution;
* first secret portability;
* first updater release;
* and first backup repository release.

---

# 76. Privacy Review Gates

Privacy review is mandatory before:

* Project Memory;
* remote provider use;
* support bundles;
* portability exports;
* telemetry export;
* Vault Capsules;
* and enterprise retention.

---

# 77. Recovery Review Gates

Recovery review is mandatory before:

* schema migration;
* product update;
* workflow external effects;
* package trust changes;
* backup release;
* and Version 1 release.

---

# 78. Documentation Deliverables

Each milestone should update:

* architecture overview;
* repository README;
* developer setup;
* service catalogue;
* schema catalogue;
* Trust Evidence catalogue;
* configuration catalogue;
* test catalogue;
* security model;
* recovery runbook;
* and release notes.

---

# 79. Service Catalogue Format

Every service record should include:

```text
Service ID
Owner
Purpose
Process
Trust Level
Dependencies
IPC
Database
CAS
Configuration
Policies
Evidence Types
Backup Adapter
Recovery Mode
Health
Current Version
```

---

# 80. Initial Service Catalogue

| Service           | Initial process                       | Milestone |
| ----------------- | ------------------------------------- | --------- |
| Runtime Registry  | Runtime                               | M1        |
| Configuration     | Runtime                               | M5        |
| Project           | Runtime                               | M4        |
| Workspace         | Runtime                               | M4        |
| Trust Evidence    | Runtime                               | M3–M6     |
| Backup            | Runtime                               | M6        |
| Patch             | Runtime                               | M7        |
| Tool Mediation    | Runtime                               | M7        |
| Local Model       | dedicated process                     | M8        |
| Context Assembly  | Runtime                               | M8        |
| Project Knowledge | dedicated worker as needed            | M8        |
| Project Memory    | Runtime                               | M8        |
| AI Router         | Runtime                               | M8–M9     |
| Network Gateway   | dedicated boundary or Runtime service | M9        |
| Plugin Platform   | Runtime plus Plugin Host              | M9        |
| MCP Gateway       | Runtime                               | M9        |
| Workflow          | Runtime plus workers                  | M10       |
| Update            | Runtime                               | M11       |

Logical services may share Runtime while maintaining strict ownership.

---

# 81. Initial Database Catalogue

| Database         | Owner             | Earliest milestone |
| ---------------- | ----------------- | ------------------ |
| runtime.db       | Runtime Registry  | M1                 |
| configuration.db | Configuration     | M5                 |
| projects.db      | Project           | M4                 |
| workspace.db     | Workspace         | M4                 |
| trust.db         | Trust Evidence    | M3                 |
| backup.db        | Backup            | M6                 |
| patch.db         | Patch             | M7                 |
| tools.db         | Tool Mediation    | M7                 |
| models.db        | Local Model       | M8                 |
| knowledge.db     | Project Knowledge | M8                 |
| memory.db        | Project Memory    | M8                 |
| routing.db       | AI Router         | M8                 |
| providers.db     | Provider Trust    | M9                 |
| plugins.db       | Plugin Platform   | M9                 |
| mcp.db           | MCP Gateway       | M9                 |
| workflow.db      | Workflow          | M10                |
| update.db        | Update            | M11                |

Consolidation may be used initially only when logical ownership and migrations remain separate.

---

# 82. Initial IPC Contract Catalogue

| Contract                  | Caller                | Owner                  | Milestone |
| ------------------------- | --------------------- | ---------------------- | --------- |
| RuntimeHealth             | Desktop               | Runtime                | M2        |
| ServiceRegistry           | Desktop               | Runtime                | M2        |
| OpenProject               | Desktop               | Project                | M4        |
| ListProjects              | Desktop               | Project                | M4        |
| CreateWorkspaceSnapshot   | Project               | Workspace              | M4        |
| GetWorkspaceSnapshot      | Desktop               | Workspace              | M4        |
| GetEffectiveConfiguration | Desktop/Services      | Configuration          | M5        |
| SubmitConfigurationChange | Desktop               | Configuration          | M5        |
| QueryTrustEvidence        | Desktop               | Trust                  | M6        |
| CreateRecoveryPoint       | Desktop/Update        | Backup                 | M6        |
| ValidatePatch             | Desktop/AI            | Patch                  | M7        |
| ApplyPatch                | Desktop               | Patch                  | M7        |
| ExecuteTool               | Desktop/Workflow      | Tool Mediation         | M7        |
| RunLocalInference         | AI Router             | Local Model            | M8        |
| BuildContextPlan          | Desktop/AI Router     | Context                | M8        |
| QueryKnowledge            | Context               | Knowledge              | M8        |
| QueryMemory               | Context               | Memory                 | M8        |
| RouteAI                   | Desktop/Workflow      | AI Router              | M8–M9     |
| CallProvider              | AI Router             | Provider Trust/Gateway | M9        |
| InvokePlugin              | Desktop/Workflow      | Plugin Platform        | M9        |
| InvokeMcp                 | Desktop/Workflow      | MCP Gateway            | M9        |
| StartWorkflow             | Desktop               | Workflow               | M10       |
| QueryWorkflow             | Desktop               | Workflow               | M10       |
| CreateSupportBundle       | Desktop               | Trust                  | M11       |
| PlanRestore               | Desktop/Recovery Host | Backup                 | M11       |

---

# 83. Initial Trust Evidence Catalogue

Foundation types:

```text
runtime.started
runtime.stopped
runtime.recovered
service.started
service.ready
service.failed
service.restarted
ipc.session-established
ipc.session-denied
project.registered
project.opened
project.closed
workspace.snapshot-created
workspace.snapshot-invalidated
configuration.snapshot-committed
configuration.source-invalid
configuration.policy-denied
backup.recovery-point-created
backup.verification-completed
security.path-denied
security.secret-redacted
```

Later types are added only with owner-service implementation.

---

# 84. Initial Configuration Catalogue

Foundation settings:

```text
desktop.appearance
desktop.show-advanced
runtime.safe-mode
runtime.restart-budget
workspace.maximum-file-size
workspace.include-hidden-files
workspace.network-path-policy
logging.minimum-level
logging.retention
trust.retention
backup.local-recovery.enabled
backup.local-recovery.retention
project.cloud.policy
```

Foundation policies:

```text
policy.product.no-secrets-in-configuration
policy.product.no-project-capability-grants
policy.product.no-remote-schema-references
policy.product.local-cloud-default
policy.product.plugins-disabled
policy.product.mcp-disabled
policy.product.remote-provider-disabled
policy.product.no-automatic-external-side-effects
```

---

# 85. Initial ADR Acceptance Targets by Gate A

Aim to Accept or materially validate:

* ADR-0001;
* ADR-0003;
* ADR-0004;
* ADR-0005;
* ADR-0006;
* ADR-0008;
* ADR-0009;
* ADR-0010;
* ADR-0011;
* ADR-0012;
* ADR-0026 foundation subset;
* ADR-0027 foundation subset;
* and ADR-0028 local Recovery Point subset.

ADR-0002 should reach a retain-or-replace decision.

---

# 86. Founder Gate Report Template

```text
Gate
Date
Build
Demonstration
User Outcomes
Passed Evidence
Failed Evidence
Security Findings
Recovery Findings
Performance
Accessibility
ADR Decisions
Known Limitations
Required Amendments
Founder Decision
Next Phase
```

---

# 87. First Implementation Backlog

## Epic 1 — Engineering Bootstrap

* Create solution.
* Pin SDK.
* Add central packages.
* Add build script.
* Add CI.
* Add test harness.
* Add architecture tests.
* Add version generation.
* Add SBOM generation.

---

## Epic 2 — Process Topology

* Create Bootstrap.
* Create Runtime.
* Create Desktop.
* Add boot identity.
* Add service registry.
* Add Job Object.
* Add restart budget.
* Add Safe Mode.

---

## Epic 3 — IPC

* Define protobuf conventions.
* Implement named-pipe channel.
* Add ACL.
* Add handshake.
* Add session authentication.
* Add deadlines.
* Add cancellation.
* Add trace propagation.
* Add malformed-message suite.

---

## Epic 4 — Persistence

* Create SQLite helper.
* Create migration runner.
* Add WAL configuration.
* Add foreign-key checks.
* Add outbox.
* Add inbox.
* Add CAS.
* Add integrity checks.
* Add backup hook.

---

## Epic 5 — Observability and Trust

* Create structured log envelope.
* Add ActivitySource.
* Add Meter.
* Add redaction.
* Add Evidence Type schema.
* Add Evidence Record schema.
* Add Trust ingestion.
* Add query API.
* Add initial Trust UI.

---

## Epic 6 — Project Boundary

* Create Project Service.
* Add folder picker adapter.
* Add typed project root.
* Add handle identity.
* Add reparse checks.
* Add project database.
* Add project registration.
* Add project receipts.

---

## Epic 7 — Workspace Snapshot

* Add file inventory.
* Add logical paths.
* Add hashes.
* Add generation.
* Add repository identity.
* Add change reconciliation.
* Add snapshot UI.
* Add adversarial paths.

---

## Epic 8 — Configuration

* Define initial settings.
* Define initial policies.
* Add strict JSON parser.
* Add duplicate detection.
* Add User Base Profile.
* Add project settings.
* Add Effective Snapshot.
* Add provenance UI.
* Add invalid edit recovery.

---

## Epic 9 — Foundation Recovery

* Define Backup Adapter.
* Add SQLite Online Backup.
* Add local Recovery Point.
* Add structural verification.
* Add disposable restore.
* Add Backup Health view.
* Add crash tests.

---

## Epic 10 — Gate A Hardening

* End-to-end test.
* Performance benchmark.
* Accessibility test.
* Security review.
* Recovery review.
* Founder demonstration.
* ADR evidence update.

---

# 88. Issue Sizing Policy

Prefer work items that complete within:

* one to three engineering days for implementation tasks;
* one week for a vertical capability;
* and two weeks maximum for a prototype with an explicit gate.

Break larger work by user-visible or evidence-visible outcome.

---

# 89. Branching and Integration

Use short-lived branches.

Every branch should:

* remain buildable;
* include tests;
* avoid long schema divergence;
* and merge behind feature flags only when inactive behaviour is safe.

Do not keep an alternative architecture alive indefinitely through feature flags.

---

# 90. Feature Flags

Feature flags are configuration, not policy bypass.

A feature flag must declare:

* owner;
* scope;
* default;
* channel;
* policy;
* expiry;
* removal release;
* and Trust visibility.

Stable should not expose unsafe Development flags.

---

# 91. Prototype Disposal

A prototype should be:

* promoted with evidence;
* rewritten behind the same contract;
* or deleted.

Do not let prototype shortcuts become hidden production authority.

---

# 92. Release Channels During Development

## Development

* unsigned or development-signed;
* verbose diagnostics;
* experimental features;
* local test providers;
* and short retention.

---

## Preview

* signed;
* migration testing;
* feature-complete candidates;
* and isolated data root.

---

## Stable

* accepted capabilities only;
* production signing;
* conservative defaults;
* and no hidden Development endpoints.

---

# 93. Data Migration Sequence

For every schema change:

1. create backup adapter compatibility;
2. create pre-migration Recovery Point;
3. validate Recovery Point;
4. stage migration;
5. run migration;
6. validate;
7. record Trust Evidence;
8. preserve rollback root or recovery set;
9. activate;
10. confirm;
11. clean up according to retention.

---

# 94. Security Incident During Development

On a credible secret, execution or data-loss incident:

1. stop affected feature;
2. preserve evidence;
3. revoke credentials;
4. quarantine artefacts;
5. reproduce safely;
6. identify architectural boundary failure;
7. amend tests and ADR if required;
8. restore trusted state;
9. record decision;
10. and only re-enable after evidence.

---

# 95. Performance Baseline by Gate A

Measure:

* cold Desktop launch;
* Runtime launch;
* reconnect;
* IPC call;
* project registration;
* Workspace Snapshot for small and medium repository;
* configuration resolution;
* Trust query;
* log write;
* evidence ingestion;
* Recovery Point creation;
* and staged restore validation.

---

# 96. Initial Performance Targets

Provisional targets:

| Operation                     |          Target |
| ----------------------------- | --------------: |
| Desktop shell visible         | under 2 seconds |
| Runtime ready                 | under 3 seconds |
| Desktop reconnect             |    under 500 ms |
| IPC health p95                |     under 10 ms |
| Open small project            |  under 1 second |
| Open medium project metadata  | under 3 seconds |
| Effective Configuration build |    under 100 ms |
| Trust query 10,000 records    |    under 100 ms |
| Evidence ingestion p95        |     under 20 ms |
| Recovery Point barrier        | under 2 seconds |

Targets require evidence and may be amended.

---

# 97. Security Baseline by Gate A

Must include:

* named-pipe ACL report;
* session replay test;
* malformed IPC fuzzing;
* path traversal suite;
* reparse-point suite;
* alternate-stream suite;
* SQLite corruption suite;
* secret-canary suite;
* log-injection suite;
* configuration bypass suite;
* evidence forgery suite;
* and backup raw-copy denial suite.

---

# 98. Recovery Baseline by Gate A

Must include:

* Desktop crash;
* Runtime crash;
* service crash;
* SQLite transaction crash;
* outbox replay;
* invalid configuration;
* Trust projection rebuild;
* local Recovery Point;
* staged restore test;
* and root remains untouched after failed restore.

---

# 99. Accessibility Baseline by Gate A

Must include:

* keyboard project open;
* keyboard settings edit;
* keyboard Trust Centre navigation;
* Narrator labels;
* focus order;
* high contrast;
* error announcements;
* and no colour-only health states.

---

# 100. Founder Product Principles Checklist

At every gate verify:

* [ ] Developer remains in control.
* [ ] Local operation remains useful.
* [ ] Cloud use is optional.
* [ ] Decisions are visible.
* [ ] Side effects are reviewable.
* [ ] Proposals are distinct from authority.
* [ ] Errors are understandable.
* [ ] Recovery is credible.
* [ ] Resource use is bounded.
* [ ] The product feels like software engineering tooling, not agent theatre.

---

# 101. Work Package Catalogue

The programme should use stable work-package IDs.

Work packages are larger than tasks and smaller than milestones.

---

# 102. WP-001 — Repository Baseline

## Outcome

A clean clone can restore, build and test.

## Inputs

* ADR-0001;
* ADR-0010;
* ADR-0011;
* ADR-0012.

## Deliverables

* solution;
* SDK pin;
* packages;
* build scripts;
* CI;
* versioning;
* analyzers;
* architecture tests.

## Evidence

* clean-clone build log;
* dependency graph;
* deterministic build comparison;
* CI permission review.

## Gate

M0.

---

# 103. WP-002 — Desktop and Runtime Bootstrap

## Outcome

Desktop and Runtime start under one bootstrap authority.

## Deliverables

* bootstrap process;
* Runtime process;
* Desktop process;
* boot identity;
* process supervision;
* health UI.

## Evidence

* startup trace;
* crash and restart report;
* orphan process report.

## Gate

M1.

---

# 104. WP-003 — Authenticated Named-Pipe IPC

## Outcome

Trusted local services communicate through typed authenticated contracts.

## Deliverables

* transport adapter;
* ACL;
* handshake;
* session authentication;
* contract negotiation;
* deadlines;
* cancellation;
* trace propagation.

## Evidence

* another-user denial;
* replay denial;
* malformed-message report;
* latency benchmark.

## Gate

M2.

---

# 105. WP-004 — Durable Service Foundation

## Outcome

A service can own SQLite state, migrate, emit an outbox and recover after crash.

## Deliverables

* persistence library;
* migration runner;
* WAL configuration;
* outbox;
* inbox;
* CAS;
* integrity health.

## Evidence

* power-failure test;
* transaction rollback;
* duplicate inbox;
* database backup prototype.

## Gate

M3.

---

# 106. WP-005 — Structured Observability

## Outcome

Logs, traces and metrics are useful without leaking project content or secrets.

## Deliverables

* structured log record;
* ActivitySource conventions;
* Meter conventions;
* local rotation;
* redaction;
* cardinality guards.

## Evidence

* secret-canary report;
* log-injection report;
* trace propagation report;
* cardinality report.

## Gate

M3.

---

# 107. WP-006 — Trust Evidence Foundation

## Outcome

Owner services can publish typed authoritative receipts and the Trust Centre can query them.

## Deliverables

* Evidence Type schema;
* Evidence Record schema;
* owner outbox contract;
* Trust inbox;
* projection;
* query;
* completeness;
* reconciliation.

## Evidence

* duplicate conflict test;
* owner gap test;
* authority-label test;
* projection rebuild.

## Gate

M3–M6.

---

# 108. WP-007 — Windows Filesystem Boundary

## Outcome

Opure can acquire and validate project and file capabilities without string-prefix trust.

## Deliverables

* typed path;
* handle identity;
* final-path resolution;
* file ID;
* reparse checks;
* alternate-stream checks;
* staged-write primitives.

## Evidence

* traversal suite;
* junction suite;
* symlink suite;
* file-replacement race suite;
* case and normalisation suite.

## Gate

M4.

---

# 109. WP-008 — Project Service

## Outcome

Projects have stable identity, lifecycle and authoritative records.

## Deliverables

* project database;
* registration;
* open;
* close;
* list;
* lifecycle;
* repository identity;
* evidence.

## Evidence

* same path different identity;
* moved repository;
* deleted project;
* channel isolation.

## Gate

M4.

---

# 110. WP-009 — Workspace Snapshot

## Outcome

A project can be represented by an immutable, bounded, hash-verified snapshot.

## Deliverables

* inventory;
* logical paths;
* hashes;
* repository status;
* generation;
* reconciliation.

## Evidence

* changed file;
* deleted file;
* rename;
* watcher loss;
* large file denial;
* secret-content exclusion from telemetry.

## Gate

M4.

---

# 111. WP-010 — Configuration Definitions and Profiles

## Outcome

Typed settings and policy definitions produce valid immutable Profiles.

## Deliverables

* Setting Definitions;
* Policy Definitions;
* Product defaults;
* User Base Profile;
* Profile revisions;
* strict JSON.

## Evidence

* definition tests;
* source-scope tests;
* duplicate-key tests;
* Product Policy bypass tests.

## Gate

M5.

---

# 112. WP-011 — Effective Configuration

## Outcome

Every active setting has an explainable effective value and policy result.

## Deliverables

* source registry;
* merge;
* policy intersection;
* Effective Snapshot;
* provenance;
* change transactions;
* last known good.

## Evidence

* invalid project file;
* conflicting policies;
* stale approval;
* atomic commit;
* activation receipt.

## Gate

M5–M6.

---

# 113. WP-012 — Trust Centre Foundation UI

## Outcome

The developer can inspect Runtime, project, Workspace and configuration evidence.

## Deliverables

* Overview;
* Services;
* Projects;
* Configuration;
* Evidence detail;
* completeness;
* search.

## Evidence

* keyboard;
* Narrator;
* high contrast;
* authority comprehension;
* query performance.

## Gate

M6.

---

# 114. WP-013 — Local Recovery Point

## Outcome

Foundation state can be snapshotted and restored into a disposable staged root.

## Deliverables

* Backup Adapter;
* Backup Epoch skeleton;
* SQLite Online Backup;
* manifest;
* structural verification;
* staged restore.

## Evidence

* live WAL database;
* raw-copy failure comparison;
* restore validation;
* unchanged live root.

## Gate

M6.

---

# 115. WP-014 — Foundation Hardening

## Outcome

The first vertical slice passes its full evidence gate.

## Deliverables

* end-to-end test;
* adversarial suite;
* performance report;
* recovery report;
* founder demonstration;
* ADR evidence updates.

## Gate

Founder Gate A.

---

# 116. WP-015 — Patch Service

## Outcome

An exact reviewed patch can be applied safely.

## Deliverables

* patch schema;
* validation;
* preview;
* approval;
* staged write;
* verification;
* receipt.

## Gate

M7.

---

# 117. WP-016 — Tool Mediation

## Outcome

Curated commands run through typed capabilities and produce bounded receipts.

## Deliverables

* Tool Definition;
* command plan;
* worker;
* output bounds;
* cancellation;
* result receipt.

## Gate

M7.

---

# 118. WP-017 — Workflow Effect Primitives

## Outcome

Patch and tool effects have durable intents, idempotency and recovery.

## Deliverables

* effect intent;
* attempt;
* receipt;
* outcome unknown;
* reconciliation;
* compensation.

## Gate

M7.

---

# 119. WP-018 — Local Model Runtime

## Outcome

One verified local text model runs under resource and trust controls.

## Deliverables

* Model Host;
* Runtime Package;
* model import;
* model profile;
* execution profile;
* streaming;
* cancellation;
* resource scheduler.

## Gate

M8.

---

# 120. WP-019 — Context Assembly

## Outcome

AI receives only an exact visible Context Plan.

## Deliverables

* Context Policy;
* source selection;
* budget;
* Context Plan;
* redaction;
* receipt.

## Gate

M8.

---

# 121. WP-020 — Project Knowledge

## Outcome

Local indexes improve context without becoming authority.

## Deliverables

* source manifest;
* index generations;
* symbols;
* local embeddings;
* query;
* provenance.

## Gate

M8.

---

# 122. WP-021 — Project Memory

## Outcome

Accepted durable project memory remains human governed.

## Deliverables

* Memory Proposal;
* acceptance;
* provenance;
* supersession;
* deletion;
* context use.

## Gate

M8.

---

# 123. WP-022 — AI Evaluation and Local Routing

## Outcome

Local model selection is evidence based and deterministic.

## Deliverables

* Evaluation Profile;
* benchmark set;
* qualification;
* Routing Policy;
* Routing Decision;
* local-only route.

## Gate

M8.

---

# 124. WP-023 — Provider Trust and Network Gateway

## Outcome

One remote provider can be used through an exact reviewed data-sharing boundary.

## Deliverables

* Provider Profile;
* Data Handling Record;
* Data Sharing Plan;
* Network Gateway;
* credential injection;
* provider receipt.

## Gate

M9.

---

# 125. WP-024 — Plugin Package and Host

## Outcome

One read-only plugin runs outside Desktop under bounded permissions.

## Deliverables

* package validation;
* quarantine;
* install;
* Plugin Host;
* capability lease;
* declarative UI;
* receipts.

## Gate

M9.

---

# 126. WP-025 — MCP Gateway

## Outcome

One exact local MCP server exposes one bounded read-only capability.

## Deliverables

* server profile;
* fingerprint;
* stdio transport;
* tool schema;
* permission;
* result receipt.

## Gate

M9.

---

# 127. WP-026 — Workflow Engine

## Outcome

Proven capabilities compose into durable recoverable workflows.

## Deliverables

* definitions;
* compiled plans;
* events;
* checkpoints;
* workers;
* approvals;
* effects;
* recovery.

## Gate

M10.

---

# 128. WP-027 — Packaging and Signing

## Outcome

Opure installs as a trusted per-user package.

## Deliverables

* MSIX;
* package identity;
* channel isolation;
* signing;
* release attestation;
* install tests.

## Gate

M11.

---

# 129. WP-028 — Updating

## Outcome

The application checks, reviews and hands off updates safely.

## Deliverables

* update metadata;
* check;
* preview;
* package verification;
* installer hand-off;
* post-launch confirmation;
* recovery.

## Gate

M11.

---

# 130. WP-029 — Support Bundles

## Outcome

A local reviewed diagnostic bundle can be exported safely.

## Deliverables

* Minimal bundle;
* Standard bundle;
* redaction;
* secret scan;
* preview;
* ZIP verification;
* export receipt.

## Gate

M11.

---

# 131. WP-030 — Managed Backup and Restore

## Outcome

Encrypted off-device backups restore on a clean machine.

## Deliverables

* repository;
* encryption;
* key slots;
* schedules;
* verification;
* Restore Host;
* staged activation;
* rollback.

## Gate

M11.

---

# 132. WP-031 — Disaster-Recovery Exercise

## Outcome

The release candidate is restored in a clean environment within measured objectives.

## Deliverables

* plan;
* exercise;
* recovery report;
* RPO;
* RTO;
* lessons;
* corrective actions.

## Gate

Release Candidate.

---

# 133. Work Package Dependency Table

| Work package | Depends on                                             |
| ------------ | ------------------------------------------------------ |
| WP-001       | None                                                   |
| WP-002       | WP-001                                                 |
| WP-003       | WP-002                                                 |
| WP-004       | WP-001, WP-003                                         |
| WP-005       | WP-001, WP-003                                         |
| WP-006       | WP-004, WP-005                                         |
| WP-007       | WP-001, WP-003                                         |
| WP-008       | WP-004, WP-006, WP-007                                 |
| WP-009       | WP-007, WP-008                                         |
| WP-010       | WP-004, WP-007                                         |
| WP-011       | WP-006, WP-009, WP-010                                 |
| WP-012       | WP-003, WP-006, WP-008, WP-011                         |
| WP-013       | WP-004, WP-006, WP-008, WP-011                         |
| WP-014       | WP-002 through WP-013                                  |
| WP-015       | WP-007, WP-009, WP-011, WP-012                         |
| WP-016       | WP-003, WP-006, WP-007, WP-011                         |
| WP-017       | WP-006, WP-015, WP-016                                 |
| WP-018       | WP-003, WP-004, WP-005, WP-006, WP-011                 |
| WP-019       | WP-009, WP-011, WP-018                                 |
| WP-020       | WP-009, WP-011, WP-018                                 |
| WP-021       | WP-004, WP-006, WP-011, WP-019                         |
| WP-022       | WP-018, WP-019, WP-020, WP-021                         |
| WP-023       | WP-006, WP-007, WP-011, WP-022                         |
| WP-024       | WP-003, WP-006, WP-007, WP-011                         |
| WP-025       | WP-003, WP-006, WP-007, WP-011, WP-016                 |
| WP-026       | WP-006, WP-011, WP-017, WP-022, WP-023, WP-024, WP-025 |
| WP-027       | WP-001, WP-002, WP-014                                 |
| WP-028       | WP-013, WP-027                                         |
| WP-029       | WP-005, WP-006, WP-012                                 |
| WP-030       | WP-004, WP-006, WP-013, WP-027                         |
| WP-031       | WP-026 through WP-030                                  |

---

# 134. Gate Dependency Rules

## Gate A

Requires:

* WP-001 through WP-014.

---

## Gate B

Requires:

* Gate A;
* WP-015;
* WP-016;
* WP-017.

---

## Gate C

Requires:

* Gate B;
* WP-018 through WP-022.

---

## Gate D

Requires:

* Gate C;
* WP-023 through WP-025.

---

## Gate E

Requires:

* Gate D;
* WP-026.

---

## Release Candidate

Requires:

* Gate E;
* WP-027 through WP-031.

---

# 135. Single-Founder Execution Mode

When one primary engineer is implementing the platform, work should remain deliberately serial on the critical path.

Recommended rules:

1. Keep one active milestone.
2. Keep no more than two active work packages.
3. Finish integration before starting the next service.
4. Automate regression tests immediately.
5. Record architectural uncertainty as a spike with a fixed decision date.
6. Prefer one backend, provider, plugin and MCP server.
7. Defer visual polish until the interaction contract is proven.
8. Keep Founder Gates short and evidence based.
9. Delete failed prototypes.
10. Protect time for documentation and recovery tests.

---

# 136. Small-Team Execution Mode

With additional engineers, assign ownership by workstream but keep:

* one architecture owner;
* one schema catalogue;
* one service catalogue;
* one release branch;
* one threat-model process;
* and one founder gate.

Avoid each workstream inventing its own:

* persistence;
* IPC;
* configuration;
* evidence;
* or backup format.

---

# 137. Initial Daily Start Sequence

The first implementation day should:

1. create the root directories;
2. add `global.json`;
3. create `Opure.slnx`;
4. create `Directory.Build.props`;
5. create `Directory.Packages.props`;
6. add the first contracts project;
7. add the first test project;
8. create `build.ps1`;
9. run restore, build and test;
10. commit the baseline.

---

# 138. First Five Commits

Recommended:

```text
1. Create Opure solution and build foundation
2. Add repository-wide package and analysis policy
3. Add Runtime, Desktop and Bootstrap skeletons
4. Add test and architecture validation projects
5. Add initial CI workflow and version generation
```

Each commit should build.

---

# 139. First Ten Engineering Tickets

## TICKET-001 — Create Solution Baseline

Acceptance:

* solution opens;
* build works;
* tests work.

---

## TICKET-002 — Add Central Build Policy

Acceptance:

* nullable;
* warnings as errors;
* deterministic;
* analyzers.

---

## TICKET-003 — Add Version Source

Acceptance:

* one version source;
* assemblies receive version;
* CI displays version.

---

## TICKET-004 — Add Runtime Executable

Acceptance:

* starts;
* records boot ID;
* exits cleanly.

---

## TICKET-005 — Add Desktop Executable

Acceptance:

* Avalonia shell appears;
* can show static Runtime unavailable state.

---

## TICKET-006 — Add Bootstrap Executable

Acceptance:

* starts Runtime and Desktop;
* records child process identity.

---

## TICKET-007 — Add Process Supervisor

Acceptance:

* detects Runtime exit;
* applies restart budget;
* cleans orphan.

---

## TICKET-008 — Define Runtime Health Contract

Acceptance:

* protobuf schema;
* service revision;
* test vectors.

---

## TICKET-009 — Implement Named-Pipe Transport Prototype

Acceptance:

* Desktop calls Runtime;
* timeout and cancellation work.

---

## TICKET-010 — Add Named-Pipe Session Authentication

Acceptance:

* authorised Desktop succeeds;
* unrelated process fails.

---

# 140. First Thirty Engineering Tickets

After the first ten:

11. Add Service Registry contract.
12. Add service lifecycle state machine.
13. Add Runtime health UI.
14. Add SQLite persistence library.
15. Add migration runner.
16. Add transactional outbox.
17. Add transactional inbox.
18. Add structured logging.
19. Add trace propagation.
20. Add redaction and canary tests.
21. Add Evidence Type schema.
22. Add Evidence Record schema.
23. Add Trust Evidence database.
24. Add Trust ingestion.
25. Add Trust query contract.
26. Add Windows path-reference library.
27. Add folder picker adapter.
28. Add Project Service database.
29. Add Open Project flow.
30. Add Project Open Trust receipt.

---

# 141. First Sixty Engineering Tickets

31. Add repository identity detection.
32. Add project-list UI.
33. Add Workspace Service contract.
34. Add file-inventory generation.
35. Add safe file hashing.
36. Add Workspace generation.
37. Add change reconciliation.
38. Add Workspace Snapshot receipt.
39. Add initial Setting Definition schema.
40. Add initial Policy Definition schema.
41. Add Product Defaults catalogue.
42. Add User Base Profile.
43. Add strict project JSON parser.
44. Add duplicate-key detector.
45. Add local schema registry.
46. Add project-settings acquisition.
47. Add setting merge.
48. Add Product Policy evaluation.
49. Add Effective Snapshot.
50. Add per-key provenance.
51. Add Configuration Change Transaction.
52. Add last-known-good state.
53. Add Trust Centre Overview.
54. Add Trust Centre Project view.
55. Add Trust Centre Configuration view.
56. Add evidence completeness.
57. Add evidence owner reconciliation.
58. Add Backup Adapter contract.
59. Add SQLite Online Backup prototype.
60. Add local Recovery Point view.

---

# 142. Ticket Template

```text
Title
Outcome
User-visible behaviour
Owner service
Applicable ADRs
Inputs
Outputs
State changes
Side effects
Policy
Trust Evidence
Failure modes
Recovery
Tests
Performance
Accessibility
Definition of Done
```

---

# 143. Architecture Spike Template

```text
Question
Why it blocks implementation
Options
Prototype
Measurement
Security impact
Recovery impact
Decision date
Exit decision
Code disposal or promotion
ADR update
```

---

# 144. Required Early Spikes

## SPIKE-001 — Avalonia Accessibility and Windowing

Decide by:

* Week 6.

Evidence:

* keyboard;
* Narrator;
* high contrast;
* native file picker;
* multiple windows;
* rendering performance.

---

## SPIKE-002 — Named-Pipe gRPC Transport

Decide by:

* Week 3.

Evidence:

* security;
* cancellation;
* streaming;
* latency;
* deployment.

---

## SPIKE-003 — SQLite Online Backup Interop

Decide by:

* Week 10.

Evidence:

* managed API sufficiency;
* live WAL database;
* cancellation;
* validation;
* performance.

---

## SPIKE-004 — JSON Schema 2020-12 Validator

Decide by:

* Week 7.

Evidence:

* supported subset;
* no remote references;
* duplicate-key integration;
* performance;
* licence.

---

## SPIKE-005 — Root-Switch Restore Mechanism

Decide after Gate A and before updater release.

Evidence:

* Windows atomicity;
* process locks;
* power loss;
* rollback;
* MSIX data layout.

---

## SPIKE-006 — llama.cpp Host Shape

Decide before WP-018.

Evidence:

* subprocess versus native adapter;
* CUDA support;
* streaming;
* cancellation;
* crash isolation;
* resource control.

---

# 145. Prototype Decision Log

For each spike record:

* hypothesis;
* code commit;
* benchmark;
* test report;
* selected option;
* rejected option;
* removal plan;
* and ADR reference.

---

# 146. Change Control

A roadmap change requires:

* reason;
* affected milestone;
* affected ADR;
* dependency impact;
* security impact;
* recovery impact;
* founder decision;
* and updated target sequence.

A task-level reorder inside one milestone does not require formal change control when dependencies remain intact.

---

# 147. Programme Status Categories

* Not Started;
* Ready;
* In Progress;
* Evidence Pending;
* Founder Review;
* Accepted;
* Blocked;
* Rework;
* Deferred;
* and Cancelled.

---

# 148. Milestone Status Report

```text
Milestone
Current status
Completed work packages
Active work packages
Evidence passed
Evidence failed
Critical defects
Security findings
Recovery findings
Performance
Founder decisions needed
Next seven-day outcome
```

---

# 149. Weekly Progress Metrics

Use limited useful metrics:

* work packages completed;
* vertical behaviours demonstrated;
* critical tests passing;
* open critical defects;
* ADR evidence completed;
* performance regressions;
* recovery failures;
* and founder decisions pending.

Do not optimise for:

* lines of code;
* ticket count;
* number of services;
* number of agents;
* or number of provider integrations.

---

# 150. Quality Dashboard

Show:

* build;
* unit;
* integration;
* adversarial;
* UI;
* accessibility;
* recovery;
* performance;
* dependency;
* signing;
* and package status.

---

# 151. Architecture Conformance Tests

Tests should prevent:

* Desktop referencing service persistence;
* plugins referencing trusted core internals;
* services referencing another service's database assembly;
* model code invoking Workspace writes;
* provider adapters invoking Vault directly;
* project files granting capabilities;
* and Trust Centre mutating domain state.

---

# 152. Source-Dependency Rules

Suggested dependency direction:

```text
Contracts
    ↑
Domain
    ↑
Application Service
    ↑
Platform Adapter
    ↑
Executable
```

UI depends on contracts and view models.

Persistence depends on domain contracts, not UI.

---

# 153. Schema Registry

By Gate A, create one repository catalogue covering:

* IPC schemas;
* database migrations;
* Evidence Types;
* configuration schemas;
* backup schemas;
* and project documents.

Every schema should have:

* owner;
* revision;
* status;
* compatibility;
* fixtures;
* and generated documentation.

---

# 154. Capability Registry

By Gate B, create one registry covering:

* Workspace read;
* Workspace write;
* Tool execute;
* Network request;
* Vault use;
* Plugin invoke;
* MCP invoke;
* Provider call;
* Model inference;
* and Backup export.

Each capability should define:

* issuer;
* subject;
* scope;
* operations;
* expiry;
* revocation;
* and evidence.

---

# 155. Error Catalogue

By Gate A, create stable user-facing error categories for:

* startup;
* IPC;
* database;
* project;
* path;
* configuration;
* policy;
* Trust Evidence;
* and backup.

By later gates, extend for:

* tools;
* AI;
* providers;
* plugins;
* MCP;
* workflow;
* update;
* and restore.

---

# 156. Supportability from the Start

Every milestone should support:

* local health view;
* copyable stable error code;
* bounded diagnostics;
* Trust Evidence link;
* and safe state summary.

Do not wait until M11 to make failures understandable.

---

# 157. Backup from the Start

Every service added after M6 must provide its Backup Adapter in the same milestone.

A service is not complete when:

* its database cannot be snapshotted;
* its CAS dependencies are unknown;
* its restore validator is absent;
* or its rebuildable state is undocumented.

---

# 158. Threat Modelling from the Start

Every new boundary adds:

* data flow;
* trust boundary;
* attacker goals;
* abuse cases;
* mitigations;
* residual risk;
* and adversarial tests.

---

# 159. Initial Threat Model Boundaries

Gate A:

* Desktop to Runtime;
* Runtime to service;
* service to SQLite;
* Workspace to filesystem;
* Configuration to project JSON;
* Trust Evidence ingestion;
* and Backup snapshot.

Gate B:

* Patch Service;
* command worker.

Gate C:

* native model host;
* model files;
* indexes.

Gate D:

* provider network;
* plugin host;
* MCP server.

Gate E:

* workflow effects and recovery.

---

# 160. Privacy Inventory from the Start

Catalogue:

* project metadata;
* paths;
* user preferences;
* logs;
* Trust Evidence;
* memory;
* prompts;
* provider data;
* plugin data;
* support bundles;
* and backups.

For each record:

* purpose;
* classification;
* owner;
* retention;
* export;
* deletion;
* and backup treatment.

---

# 161. Release Evidence Repository

Suggested:

```text
eng/
└── evidence/
    ├── milestones/
    ├── security/
    ├── performance/
    ├── recovery/
    ├── accessibility/
    ├── dependencies/
    ├── signing/
    └── releases/
```

Do not commit secrets or sensitive support artefacts.

Store:

* summaries;
* hashes;
* test outputs;
* and safe reports.

---

# 162. ADR Evidence Update Pattern

Add an implementation evidence section or linked evidence record stating:

* prototype version;
* commit;
* test environment;
* results;
* limitations;
* decision;
* and date.

Do not rewrite the historical decision rationale.

---

# 163. Founder Approval Records

Founder approvals should identify:

* gate;
* build;
* demonstration;
* accepted limitations;
* required follow-up;
* and decision date.

---

# 164. Milestone M0 Checklist

* [ ] Repository directories exist.
* [ ] Solution builds.
* [ ] SDK is pinned.
* [ ] Packages are centrally managed.
* [ ] Restore is locked.
* [ ] Build script works.
* [ ] Unit test runs.
* [ ] Architecture test runs.
* [ ] CI runs.
* [ ] Version is generated.
* [ ] SBOM skeleton exists.
* [ ] README explains setup.

---

# 165. Milestone M1 Checklist

* [ ] Bootstrap launches.
* [ ] Runtime launches.
* [ ] Desktop launches.
* [ ] Runtime boot ID exists.
* [ ] Service registry exists.
* [ ] Health states exist.
* [ ] Job Object works.
* [ ] Runtime crash restarts.
* [ ] Restart budget works.
* [ ] Safe Mode exists.
* [ ] Desktop can close independently.
* [ ] Development data root is isolated.

---

# 166. Milestone M2 Checklist

* [ ] Named-pipe transport works.
* [ ] ACL is explicit.
* [ ] Session handshake works.
* [ ] Mutual session authentication works.
* [ ] Contract revision works.
* [ ] Deadline works.
* [ ] Cancellation works.
* [ ] Message bounds work.
* [ ] Error model works.
* [ ] Trace propagation works.
* [ ] Another user is denied.
* [ ] Replay is denied.
* [ ] Fuzz tests pass.

---

# 167. Milestone M3 Checklist

* [ ] SQLite library works.
* [ ] Migration runner works.
* [ ] WAL policy works.
* [ ] Foreign keys work.
* [ ] Outbox works.
* [ ] Inbox works.
* [ ] CAS works.
* [ ] Structured logs work.
* [ ] Redaction works.
* [ ] Traces work.
* [ ] Metrics work.
* [ ] Evidence schemas work.
* [ ] Trust ingestion works.
* [ ] Query works.
* [ ] Backup adapter hook exists.

---

# 168. Milestone M4 Checklist

* [ ] Project Service works.
* [ ] Workspace Service works.
* [ ] Project picker is trusted.
* [ ] Project ID is stable.
* [ ] Filesystem identity is verified.
* [ ] Reparse escape is denied.
* [ ] Alternate streams are denied.
* [ ] Repository identity is captured.
* [ ] Workspace Snapshot is immutable.
* [ ] Hashes are correct.
* [ ] Generation changes correctly.
* [ ] Project Trust receipts exist.
* [ ] Path adversarial suite passes.

---

# 169. Milestone M5 Checklist

* [ ] Setting Definitions exist.
* [ ] Policy Definitions exist.
* [ ] Product defaults exist.
* [ ] User Base Profile exists.
* [ ] Strict JSON works.
* [ ] Duplicate keys fail.
* [ ] Project settings work.
* [ ] Merge works.
* [ ] Policy intersection works.
* [ ] Effective Snapshot exists.
* [ ] Provenance exists.
* [ ] Change Transaction works.
* [ ] Last-known-good works.
* [ ] Policy bypass suite passes.

---

# 170. Milestone M6 Checklist

* [ ] Trust Centre Overview works.
* [ ] Project view works.
* [ ] Configuration view works.
* [ ] Authority labels work.
* [ ] Completeness works.
* [ ] Reconciliation works.
* [ ] Local Recovery Point works.
* [ ] Structural verification works.
* [ ] Disposable restore works.
* [ ] End-to-end test passes.
* [ ] Crash suite passes.
* [ ] Accessibility baseline passes.
* [ ] Performance baseline exists.
* [ ] Founder Gate A decision recorded.

---

# 171. Milestone M7 Checklist

* [ ] Patch schema works.
* [ ] Exact preview works.
* [ ] Approval binding works.
* [ ] Staged write works.
* [ ] File race checks pass.
* [ ] Verification works.
* [ ] Tool templates work.
* [ ] Restricted worker works.
* [ ] Output bounds work.
* [ ] Effect intent exists.
* [ ] Idempotency works.
* [ ] Outcome Unknown works.
* [ ] Founder Gate B decision recorded.

---

# 172. Milestone M8 Checklist

* [ ] Local Model Host works.
* [ ] Model packages verify.
* [ ] Resource scheduling works.
* [ ] Context Plan works.
* [ ] Knowledge index works.
* [ ] Memory proposal works.
* [ ] Local evaluation works.
* [ ] Local Routing Decision works.
* [ ] Explain-code flow works.
* [ ] AI patch proposal enters Patch Service.
* [ ] No network occurs.
* [ ] Founder Gate C decision recorded.

---

# 173. Milestone M9 Checklist

* [ ] Provider Profile works.
* [ ] Data Sharing Plan works.
* [ ] Network Gateway works.
* [ ] One remote provider works.
* [ ] No automatic fallback exists.
* [ ] Plugin package validates.
* [ ] Plugin Host works.
* [ ] Plugin permission works.
* [ ] One read-only plugin works.
* [ ] MCP server profile works.
* [ ] MCP Gateway works.
* [ ] One read-only MCP tool works.
* [ ] Founder Gate D decision recorded.

---

# 174. Milestone M10 Checklist

* [ ] Workflow Definition works.
* [ ] Compiled Plan works.
* [ ] Event store works.
* [ ] Projection rebuild works.
* [ ] Checkpoints work.
* [ ] Workers and leases work.
* [ ] Approvals work.
* [ ] Effects work.
* [ ] Reconciliation works.
* [ ] Compensation works.
* [ ] Crash recovery works.
* [ ] Backup restore produces Recovery Required.
* [ ] Founder Gate E decision recorded.

---

# 175. Milestone M11 Checklist

* [ ] Version train works.
* [ ] MSIX works.
* [ ] Signing works.
* [ ] Stable and Preview isolate.
* [ ] Updater works.
* [ ] Update rollback works.
* [ ] Support bundle works.
* [ ] Secret scans work.
* [ ] Managed backup works.
* [ ] New-machine restore works.
* [ ] Disaster exercise works.
* [ ] Accessibility review passes.
* [ ] Security review passes.
* [ ] Privacy review passes.
* [ ] Founder release decision recorded.

---

# 176. Release Candidate Definition

A Release Candidate is a signed build that:

* installs on a clean supported Windows 11 machine;
* opens a real project safely;
* performs the selected Version 1 workflows;
* exposes complete Trust Evidence;
* operates locally without network;
* uses remote services only through explicit approval;
* survives process failures;
* updates safely;
* creates a reviewed support bundle;
* creates an encrypted portable backup;
* restores on a clean machine;
* and has no unresolved critical release gate.

---

# 177. Version 1 Scope Recommendation

Recommended Version 1 capabilities:

* safe project open;
* project and configuration Trust Centre;
* local model explanation;
* local AI patch proposal;
* reviewed Patch Service;
* curated build and test commands;
* local Project Knowledge;
* accepted Project Memory;
* one remote provider;
* one read-only plugin example;
* one read-only MCP example;
* one durable developer workflow;
* local support bundle;
* encrypted backup;
* and staged restore.

---

# 178. Version 1 Exclusions

Recommended exclusions:

* autonomous multi-agent operation;
* unattended repository writes;
* unattended pushes;
* unattended deployments;
* provider-side agents;
* provider-side files and vector stores;
* remote browser automation;
* remote computer use;
* arbitrary shell;
* arbitrary plugin network;
* arbitrary MCP network;
* plugin marketplace;
* cloud backup service;
* cloud Profile synchronisation;
* workflow designer with arbitrary nodes;
* model training;
* fine-tuning;
* speculative decoding;
* multimodal editing;
* cross-platform desktop;
* and enterprise central administration beyond policy templates.

---

# 179. Success Measures

Product success should be measured by:

* project-open correctness;
* crash recovery;
* developer approval clarity;
* percentage of effects with complete receipts;
* local completion rate;
* time to diagnose a failure;
* successful restore rate;
* context relevance;
* patch acceptance;
* build and test success;
* and developer trust.

Do not use token volume or agent count as primary success measures.

---

# 180. Foundation Completion Criteria

The foundation is complete when:

* the control plane exists;
* every service boundary is typed;
* project access is capability based;
* configuration is explainable;
* evidence is visible;
* persistence is recoverable;
* local Recovery Points work;
* build and CI are trusted;
* and Gate A is accepted.

---

# 181. Programme Completion Criteria

The implementation programme reaches Version 1 readiness when:

* Gate A through Gate E are accepted;
* M11 is complete;
* all selected ADRs have sufficient evidence;
* release signing is operational;
* update recovery is proven;
* backup restore is exercised;
* support exports are safe;
* accessibility passes;
* security and privacy reviews pass;
* and founder release approval is recorded.

---

# 182. Immediate Next Action

Create the repository implementation baseline.

The first command sequence should begin with:

```powershell
Set-Location C:\Opure

New-Item -ItemType Directory -Force `
  src, tests, tools, build, eng, packaging, enterprise, samples, docs, schemas

dotnet new sln -n Opure
```

Because ADR-0010 selected `Opure.slnx`, the exact stable .NET CLI command for creating or converting to `.slnx` should be verified against the pinned SDK before committing.

Create the first projects:

```powershell
dotnet new classlib -n Opure.Runtime.Contracts -o src/Runtime/Opure.Runtime.Contracts
dotnet new console  -n Opure.Runtime -o src/Runtime/Opure.Runtime
dotnet new console  -n Opure.Bootstrap.Windows -o src/Bootstrap/Opure.Bootstrap.Windows
dotnet new xunit    -n Opure.ArchitectureTests -o tests/Architecture/Opure.ArchitectureTests
```

Add the Avalonia Desktop project using the selected pinned template only after the SDK and package graph are fixed.

---

# 183. First Day Definition of Done

* [ ] Repository structure exists.
* [ ] `global.json` pins the SDK.
* [ ] `Opure.slnx` or verified conversion path exists.
* [ ] Central package management exists.
* [ ] Runtime Contracts project exists.
* [ ] Runtime executable exists.
* [ ] Bootstrap executable exists.
* [ ] Architecture test project exists.
* [ ] Build script restores, builds and tests.
* [ ] First CI workflow exists.
* [ ] First commit is clean.
* [ ] `git status` is clean.

---

# 184. First Week Definition of Done

* [ ] Clean clone builds.
* [ ] CI passes.
* [ ] Runtime starts.
* [ ] Bootstrap starts Runtime.
* [ ] Desktop project shell exists.
* [ ] Version is generated.
* [ ] Architecture tests enforce initial dependencies.
* [ ] Founder reviews repository and process shape.
* [ ] Week 2 tickets are ready.

---

# 185. Programme Review Record

| Date         | Reviewer           | Decision | Notes                                                             |
| ------------ | ------------------ | -------- | ----------------------------------------------------------------- |
| 18 July 2026 | Architecture draft | Proposed | Dependency-ordered control-plane-first implementation recommended |

---

# 186. Approval

## Founder and Product Approval

* **Name:** Christopher Dyer
* **Decision:** Pending
* **Date:** Pending
* **Notes:** First vertical slice, twelve-week sequence, founder gates and Version 1 scope require review

## Architecture Approval

* **Name or role:** Foundation Architecture Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Dependency graph, service sequence and ADR evidence gates require review

## Engineering-System Approval

* **Name or role:** Build, Test and Release Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** repository baseline, CI, versioning and evidence storage require review

## Security and Privacy Approval

* **Name or role:** Security, Privacy and Secrets Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** gate sequence and first external boundary timing require review

## Recovery Approval

* **Name or role:** Recovery Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** local Recovery Point by Gate A and full restore before release require review

## User Experience Approval

* **Name or role:** Desktop and Accessibility Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Avalonia evidence, Trust Centre and review interaction require evidence

---

# 187. Supersession

This roadmap is superseded only when a later roadmap:

* names ROADMAP-001 explicitly;
* explains why milestone order or founder gates changed;
* identifies affected ADR evidence;
* identifies repository, schema, service and migration impact;
* updates immediate implementation work;
* and records founder approval.

---

# 188. Change History

| Version | Date         | Author        | Summary                                                       |
| ------- | ------------ | ------------- | ------------------------------------------------------------- |
| 0.1     | 18 July 2026 | Founder Draft | Initial dependency-ordered foundation implementation sequence |

---

# 189. Final Programme Statement

> **Opure will begin implementation by proving its local trusted control plane rather than beginning with a chatbot, provider integration or autonomous workflow: the first twelve weeks will establish a reproducible repository, supervised Desktop and Runtime, authenticated named-pipe IPC, service-owned SQLite persistence, structured redacted observability, a handle-verified Project and Workspace boundary, typed configuration and non-bypassable policy, authoritative Trust Evidence, a developer-facing Trust Centre and a structurally verified local Recovery Point; only after Founder Gate A confirms that this foundation is understandable, secure, responsive and recoverable will the programme add reviewed patches and commands, then local models and explicit Context Plans, then remote providers, plugins and MCP under exact data-sharing and capability controls, then durable workflows composed from already proven services, and finally signed packaging, updates, support bundles, encrypted backup and exercised disaster recovery, because Opure's first implementation responsibility is not to maximise automated output but to establish the visible, testable and reversible system of control that makes every later intelligence feature worthy of developer trust.**

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**