# SPEC-012 — Roadmap

## Opure Platform Delivery Plan, Milestones and Evidence Gates

**Document:** SPEC-012  
**Status:** Founder Draft  
**Version:** 0.1  
**Language:** British English  
**Last updated:** 18 July 2026  
**Depends on:** CHARTER-001, SPEC-001, SPEC-002, SPEC-003, SPEC-004, SPEC-005, SPEC-006, SPEC-007, SPEC-008, SPEC-009, SPEC-010, SPEC-011  

---

## 1. Purpose

This specification defines the delivery roadmap for the Opure Platform.

It translates the approved Charter and product specifications into:

- implementation phases;
- milestone boundaries;
- dependency order;
- evidence gates;
- prototype goals;
- testing requirements;
- release criteria;
- risk controls;
- technical-debt rules;
- and deferred capability boundaries.

The roadmap must prevent Opure from attempting to build every planned capability at once.

It must preserve the founding architecture while allowing practical delivery by a small team.

---

## 2. Founding Rule

> **Opure must earn complexity through evidence.**

Capabilities must be introduced when they:

- support a validated user need;
- have a clear service owner;
- fit the approved architecture;
- can be tested;
- can be observed;
- and do not weaken developer control.

A feature must not enter a milestone merely because it is impressive, fashionable or associated with artificial intelligence.

---

## 3. Roadmap Principles

The roadmap must follow these principles:

- Build the safe local foundation first.
- Prove service boundaries before adding breadth.
- Deliver one complete engineering path before many partial paths.
- Keep AI replaceable from the first working prototype.
- Treat security, recovery and observability as product features.
- Prefer vertical slices over disconnected subsystem demos.
- Use Windows 11 as the first supported platform.
- Avoid permanent Windows lock-in where practical.
- Delay cloud requirements until local value is proven.
- Delay marketplace, collaboration and distributed systems until justified.
- Maintain a clean repository and reviewable specifications.
- Require measurable evidence before advancing milestones.

---

## 4. Scope of This Roadmap

This roadmap covers the journey from specifications to:

1. architecture proof;
2. internal developer prototype;
3. first usable local product;
4. private alpha;
5. public technical preview;
6. beta;
7. version 1.0;
8. and post-1.0 expansion.

Exact calendar dates are not fixed by this document.

The roadmap is evidence-gated rather than date-driven.

---

## 5. Delivery Model

Opure should be delivered through staged vertical slices.

Each phase must contain:

- one or more user-visible outcomes;
- required infrastructure;
- tests;
- diagnostics;
- Trust Centre coverage;
- documentation;
- and explicit exit criteria.

A phase is not complete because code exists.

It is complete only when its exit criteria are satisfied.

---

## 6. Milestone Statuses

Milestones should use these statuses:

- **Not Started**
- **Discovery**
- **Designing**
- **Building**
- **Validating**
- **Blocked**
- **Ready for Gate Review**
- **Approved**
- **Complete**
- **Superseded**
- **Cancelled**

---

## 7. Evidence Gates

Every milestone should pass relevant evidence gates.

Recommended gates are:

### G0 — Specification Gate

Required behaviour, ownership and non-goals are documented.

### G1 — Architecture Gate

Service boundaries, contracts and ADRs are defined.

### G2 — Prototype Gate

The capability works in a controlled prototype.

### G3 — Reliability Gate

Failure, cancellation, restart and recovery paths are tested.

### G4 — Security Gate

Permissions, secrets, policy and trust boundaries are tested.

### G5 — Usability Gate

A developer can understand and use the capability without hidden knowledge.

### G6 — Performance Gate

The capability performs acceptably on the reference machine.

### G7 — Release Gate

Documentation, migration, diagnostics and support expectations are ready.

---

## 8. Reference Development Environment

The initial reference environment is:

- Windows 11;
- AMD Ryzen 9 5950X;
- 32 GB system memory;
- NVIDIA RTX 5070 Ti with 16 GB VRAM;
- local Git repository;
- local provider support beginning with Ollama;
- and no required cloud dependency.

This reference environment guides optimisation.

It must not become the only future supported hardware profile.

---

## 9. Repository Structure Target

The project repository should evolve towards:

```text
C:\Opure
├── specs
├── docs
├── adr
├── src
├── tests
├── tools
├── scripts
├── samples
├── packaging
├── assets
├── benchmarks
├── security
└── .github
```

Exact source layout depends on implementation-language ADRs.

---

## 10. Specification Baseline

The founding specification set is:

- `CHARTER-001` — Founding Principles
- `SPEC-001` — Vision and Design Principles
- `SPEC-002` — Runtime Kernel
- `SPEC-003` — Service Architecture
- `SPEC-004` — AI Router
- `SPEC-005` — Memory Engine
- `SPEC-006` — Workflow and Agent System
- `SPEC-007` — Plugin SDK
- `SPEC-008` — Trust Centre and Security
- `SPEC-009` — Workspace and File Patch Engine
- `SPEC-010` — Desktop User Interface
- `SPEC-011` — Project and Build Management
- `SPEC-012` — Roadmap

These documents define product intent.

Implementation ADRs must resolve deferred technical decisions.

---

# 11. Phase 0 — Founding Baseline

## 11.1 Goal

Create a stable architectural and governance baseline before implementation begins.

---

## 11.2 Required Work

Phase 0 includes:

- approve Charter;
- approve product specifications;
- establish repository conventions;
- define ADR process;
- define issue and milestone conventions;
- define contribution rules;
- define coding standards after language selection;
- define security review process;
- define release versioning;
- and define acceptance-test ownership.

---

## 11.3 Required ADRs

Before major implementation, resolve:

- primary implementation language;
- desktop framework;
- Runtime process topology;
- local IPC transport;
- contract serialisation;
- storage strategy;
- testing framework;
- logging framework;
- and Windows packaging approach.

---

## 11.4 Deliverables

- approved specification set;
- ADR template;
- contribution guide;
- coding-standard skeleton;
- development-environment guide;
- issue templates;
- and project milestone board.

---

## 11.5 Exit Criteria

Phase 0 is complete when:

- [ ] All founding specifications exist.
- [ ] Founding principles are internally consistent.
- [ ] Major deferred implementation choices have ADR owners.
- [ ] Repository conventions are documented.
- [ ] The first implementation milestone has a defined vertical slice.
- [ ] No core service lacks an owner.
- [ ] Security and recovery are represented in milestone planning.

---

# 12. Phase 1 — Architecture Skeleton

## 12.1 Goal

Prove that Opure can start, supervise modular services and expose observable state without requiring AI.

---

## 12.2 User-Visible Outcome

A minimal desktop shell starts on Windows 11 and displays:

- Runtime status;
- registered services;
- health;
- safe shutdown;
- and diagnostic information.

---

## 12.3 Required Components

- Runtime Bootstrap;
- Runtime identity;
- Configuration Manager;
- Service Registry;
- Lifecycle Manager;
- Runtime Messaging;
- Scheduler skeleton;
- Health Supervisor;
- Trust Centre audit buffer;
- Desktop Gateway skeleton;
- desktop shell;
- and structured logging.

---

## 12.4 Service Proof

The phase must include at least:

- one in-process service;
- one isolated worker process;
- one simulated failing optional service;
- and one simulated critical-service failure.

---

## 12.5 Tests

Tests must cover:

- startup order;
- dependency rejection;
- graceful shutdown;
- forced shutdown;
- service crash;
- restart-loop prevention;
- health aggregation;
- and Runtime reconnection.

---

## 12.6 Exit Criteria

Phase 1 is complete when:

- [ ] Opure starts without AI.
- [ ] Desktop shows accurate Runtime health.
- [ ] Services start in dependency order.
- [ ] Optional-service failure causes degradation, not full failure.
- [ ] Critical-service failure blocks unsafe readiness.
- [ ] Shutdown completes in dependency-safe order.
- [ ] Abnormal shutdown is detected.
- [ ] A diagnostic bundle can be created without secret content.
- [ ] Architecture tests prevent prohibited service coupling.

---

# 13. Phase 2 — Safe Project Foundation

## 13.1 Goal

Open a real project safely and observe its workspace without modifying files.

---

## 13.2 User-Visible Outcome

A developer can:

- register a project;
- open it;
- inspect files;
- view repository status;
- see project health;
- and close it safely.

---

## 13.3 Required Components

- Project Manager;
- Workspace Service;
- canonical path handling;
- file identity;
- file metadata;
- exclusions;
- file watcher;
- selected-file snapshots;
- Repository Service read-only Git support;
- Project Dashboard;
- and Trust Centre project records.

---

## 13.4 Supported Scope

Initial scope should support:

- one local Windows workspace root;
- ordinary NTFS project folders;
- Git repositories;
- text files;
- and external-editor launch.

UNC shares and complex multi-root projects may remain deferred.

---

## 13.5 Security Requirements

- workspace boundary enforcement;
- traversal rejection;
- symbolic-link and junction inspection;
- sensitive-file defaults;
- local API authentication;
- and project isolation.

---

## 13.6 Exit Criteria

Phase 2 is complete when:

- [ ] A project has stable identity independent from path.
- [ ] Workspace reads cannot escape the approved root.
- [ ] File metadata includes hash, encoding and line endings.
- [ ] External edits are detected.
- [ ] Git status is shown without altering repository state.
- [ ] Sensitive files are classified.
- [ ] Project activity appears in the Trust Centre.
- [ ] Closing and reopening preserves safe project state.
- [ ] A moved project can be re-associated safely.

---

# 14. Phase 3 — Reviewable Patch Vertical Slice

## 14.1 Goal

Deliver the first complete Opure engineering path:

```text
Developer Request
    ↓
Proposed Patch
    ↓
Review
    ↓
Approval
    ↓
Transactional Apply
    ↓
Verification
    ↓
Trust Record
```

---

## 14.2 User-Visible Outcome

A developer can create or import a patch, inspect every change, approve it, apply it safely and reverse it where possible.

---

## 14.3 Required Components

- Patch Service;
- patch format;
- Diff Engine;
- patch preview;
- path validation;
- base-revision validation;
- secret scanning;
- approval binding;
- transaction journal;
- staging area;
- file replacement;
- resulting-hash verification;
- reverse plan;
- Patch Centre;
- conflict view;
- and recovery interface.

---

## 14.4 Initial Patch Scope

The first vertical slice should support:

- create text file;
- modify text file;
- delete text file;
- rename text file;
- multi-file patch;
- UTF-8 text;
- common Windows line endings;
- and basic conflict detection.

Binary changes and advanced three-way merge may remain deferred.

---

## 14.5 Exit Criteria

Phase 3 is complete when:

- [ ] Every patch records exact base revisions.
- [ ] Preview shows all affected files.
- [ ] Secret scanning runs before apply.
- [ ] Approval binds to the exact patch version.
- [ ] Existing unrelated developer changes are preserved.
- [ ] External overlapping edits create a conflict.
- [ ] Apply uses durable journalling.
- [ ] Resulting files are verified.
- [ ] Interrupted apply enters recoverable state.
- [ ] Reverse validates current state.
- [ ] Patch application does not stage or commit in Git.
- [ ] Full patch activity is correlated in the Trust Centre.

---

# 15. Phase 4 — Local AI Router

## 15.1 Goal

Add replaceable local intelligence without weakening the working non-AI platform.

---

## 15.2 User-Visible Outcome

A developer can:

- configure a local provider;
- inspect installed models;
- choose a model;
- submit a local request;
- stream output;
- cancel;
- and inspect routing evidence.

---

## 15.3 Required Components

- AI Router;
- provider registry;
- model registry;
- capability model;
- provider health;
- routing engine;
- streaming;
- cancellation;
- structured output;
- usage metadata;
- local model runtime coordination;
- Ollama adapter;
- and simulated second adapter.

---

## 15.4 Important Constraint

No workflow or service may call Ollama directly.

All AI traffic must use provider-neutral contracts.

---

## 15.5 Initial Request Types

- conversational text;
- code explanation;
- code-generation proposal;
- structured output;
- and embeddings.

---

## 15.6 Exit Criteria

Phase 4 is complete when:

- [ ] Ollama models are discovered through an adapter.
- [ ] A simulated second adapter proves provider neutrality.
- [ ] Model capabilities are explicit.
- [ ] Routing is inspectable.
- [ ] Streaming and cancellation work.
- [ ] Structured output is validated.
- [ ] Provider failure does not crash the Runtime.
- [ ] Local AI requests require no cloud connectivity.
- [ ] Provider credentials, where relevant, use Vault references.
- [ ] Model output cannot modify files directly.
- [ ] Balanced mode coordinates one fast resident model where practical.

---

# 16. Phase 5 — Context and Project Memory

## 16.1 Goal

Give Opure structured project understanding with provenance and isolation.

---

## 16.2 User-Visible Outcome

A developer can inspect:

- indexed files;
- symbols;
- relationships;
- project summaries;
- provenance;
- confidence;
- freshness;
- and memory storage.

---

## 16.3 Required Components

- Knowledge Engine;
- project memory namespace;
- parser framework;
- file and symbol records;
- relationship storage;
- full-text search;
- local embeddings;
- vector retrieval;
- hybrid retrieval;
- provenance registry;
- stale-state handling;
- correction;
- deletion;
- and Memory Explorer.

---

## 16.4 Initial Language Support

The first implementation should support a deliberately small number of languages aligned with the implementation stack and sample projects.

Exact languages are decided by ADR.

---

## 16.5 Exit Criteria

Phase 5 is complete when:

- [ ] Project memory is isolated.
- [ ] Every durable record has provenance.
- [ ] File changes mark dependent records stale.
- [ ] Structured search works without AI.
- [ ] Local embeddings work through the AI Router.
- [ ] Incompatible vector spaces are separated.
- [ ] Secrets do not enter embeddings.
- [ ] Developers can correct and delete records.
- [ ] Retrieval explains why results were selected.
- [ ] Removing memory does not damage the project.
- [ ] Reindexing is resumable and cancellable.

---

# 17. Phase 6 — First Assisted Engineering Workflow

## 17.1 Goal

Deliver the first complete AI-assisted software-engineering workflow.

---

## 17.2 Recommended First Workflow

The first workflow should be:

```text
Fix a Small Bug
```

It should:

1. capture developer intent;
2. retrieve relevant context;
3. produce a plan;
4. produce a patch;
5. validate the patch;
6. run available checks;
7. review the result;
8. request approval;
9. apply the patch;
10. rerun validation;
11. and record the outcome.

---

## 17.3 Required Components

- Workflow Engine;
- workflow definitions;
- workflow instances;
- stage state machine;
- role templates;
- context packages;
- checkpoints;
- approvals;
- cancellation;
- retry;
- compensation;
- Workflow Centre;
- and complete Trust Centre correlation.

---

## 17.4 Initial Roles

- Planner;
- Coder;
- Reviewer;
- Tester;
- and Git preparation role where available.

---

## 17.5 Exit Criteria

Phase 6 is complete when:

- [ ] The workflow is fully inspectable.
- [ ] Roles are workflows, not separate authorities.
- [ ] AI requests pass through the AI Router.
- [ ] File changes pass through the Patch Service.
- [ ] Protected actions pass through policy.
- [ ] The workflow can pause for approval.
- [ ] The workflow can be cancelled.
- [ ] Checkpoints survive Runtime restart.
- [ ] Stale project state invalidates unsafe continuation.
- [ ] Validation clearly states what was not run.
- [ ] Failed attempts remain visible.
- [ ] The developer can complete the same project work manually outside the workflow.

---

# 18. Phase 7 — Build, Test and Git Integration

## 18.1 Goal

Connect generated changes to real engineering evidence.

---

## 18.2 User-Visible Outcome

A developer can:

- detect a supported build system;
- run builds;
- run tests;
- inspect diagnostics;
- stage exact changes;
- prepare a commit;
- and create a commit with approval.

---

## 18.3 Required Components

- Build Manager;
- Build Profiles;
- Test Profiles;
- CLI Adapter Host;
- command plans;
- process control;
- output parsing;
- artefact records;
- Repository Service write operations;
- staging;
- commit preparation;
- secret scanning before commit;
- and build/test desktop views.

---

## 18.4 Initial Ecosystem

The initial ecosystem should match the primary implementation stack and one representative external sample ecosystem.

Exact selection is decided by ADR.

---

## 18.5 Exit Criteria

Phase 7 is complete when:

- [ ] Detected commands remain untrusted until approved.
- [ ] Executable and arguments remain separate.
- [ ] Builds and tests are cancellable.
- [ ] Child processes are controlled.
- [ ] Diagnostics link to files where possible.
- [ ] Test retries expose flakiness.
- [ ] Build results retain tool and source identity.
- [ ] Patch application, staging and commit remain separate.
- [ ] Staging preserves unrelated developer changes.
- [ ] Secret scanning runs before commit.
- [ ] Repository hooks are visible.
- [ ] Commit creation is recorded in the Trust Centre.

---

# 19. Phase 8 — Trust Centre and Security Completion

## 19.1 Goal

Move from foundational security hooks to complete, user-visible trust controls.

---

## 19.2 User-Visible Outcome

A developer can:

- inspect permissions;
- review approvals;
- manage cloud policy;
- inspect external data sharing;
- manage secrets;
- inspect security events;
- quarantine a component;
- and export a redacted diagnostic bundle.

---

## 19.3 Required Components

- full Policy Engine;
- Approval Manager;
- permission records;
- data classification;
- Secrets Vault;
- secret detection;
- Redaction Service;
- Network Gateway;
- Trust Centre timeline;
- audit integrity;
- quarantine;
- security incidents;
- Safe Mode;
- and Recovery Mode.

---

## 19.4 Exit Criteria

Phase 8 is complete when:

- [ ] Protected actions are deterministically evaluated.
- [ ] Default deny works.
- [ ] Approval is bounded and revocable.
- [ ] Material changes invalidate approval.
- [ ] Local Only blocks remote project-data transmission.
- [ ] Secrets remain outside normal storage.
- [ ] Network destinations are classified.
- [ ] Redirects trigger re-evaluation where required.
- [ ] Trust records are correlated and privacy-preserving.
- [ ] Audit integrity can be verified.
- [ ] High-risk actions are constrained when audit storage fails.
- [ ] Unsafe plugins can be quarantined.
- [ ] Safe Mode starts without third-party components.
- [ ] Diagnostic exports are redacted and previewed.
- [ ] Telemetry remains disabled by default.

---

# 20. Phase 9 — Plugin SDK Foundation

## 20.1 Goal

Prove safe extensibility without weakening the core.

---

## 20.2 User-Visible Outcome

A developer can:

- install a local development plugin;
- inspect its permissions;
- enable it;
- use its capability;
- disable it;
- update it;
- and remove it safely.

---

## 20.3 Required Components

- plugin manifest;
- package validation;
- package digest;
- Plugin Manager;
- Plugin Host;
- capability contracts;
- isolated plugin storage;
- settings;
- permission simulation;
- update rollback;
- and SDK examples.

---

## 20.4 Required Example Plugins

At least:

- one simple project-analysis capability;
- one workflow-contribution plugin;
- and one parser or build adapter.

---

## 20.5 Exit Criteria

Phase 9 is complete when:

- [ ] Installation does not grant permissions automatically.
- [ ] Third-party plugin code runs outside the trusted core.
- [ ] Plugin crashes do not crash Opure.
- [ ] Undeclared capability use is denied.
- [ ] Project writes use Patch Service.
- [ ] Network access uses Network Gateway.
- [ ] Secrets use Vault controls.
- [ ] Plugin updates cannot silently broaden permissions.
- [ ] Failed updates can roll back.
- [ ] Disabling removes active contributions.
- [ ] Removal explains data and workflow impact.
- [ ] Development plugins are visibly labelled.

---

# 21. Phase 10 — MCP Gateway Foundation

## 21.1 Goal

Support external MCP capabilities through the same trust model as native integrations.

---

## 21.2 User-Visible Outcome

A developer can:

- register an MCP server;
- inspect its endpoint or executable;
- inspect discovered capabilities;
- grant bounded permissions;
- invoke a capability;
- cancel it;
- and inspect activity.

---

## 21.3 Required Components

- MCP server registry;
- MCP session management;
- capability discovery;
- protocol validation;
- policy binding;
- secret references;
- Network Gateway integration;
- Trust Centre recording;
- and MCP management desktop view.

---

## 21.4 Exit Criteria

Phase 10 is complete when:

- [ ] Discovery does not equal permission.
- [ ] MCP servers remain external trust boundaries.
- [ ] AI cannot invoke MCP directly.
- [ ] Plugin code cannot bypass the gateway.
- [ ] Requests and responses are schema validated.
- [ ] MCP output is treated as untrusted.
- [ ] Invocation is cancellable.
- [ ] Secret and network use is scoped.
- [ ] Significant invocations are auditable.
- [ ] MCP failure does not degrade unrelated platform capabilities.

---

# 22. Phase 11 — Proven Pattern Library

## 22.1 Goal

Turn successful engineering evidence into reusable, inspectable patterns.

---

## 22.2 User-Visible Outcome

A developer can:

- inspect pattern candidates;
- understand evidence;
- promote or reject patterns;
- search patterns;
- and apply a pattern through a new patch or workflow.

---

## 22.3 Required Components

- pattern records;
- lifecycle states;
- promotion rules;
- compatibility metadata;
- known limitations;
- reuse history;
- private-source safeguards;
- Pattern Library view;
- and pattern revalidation.

---

## 22.4 Exit Criteria

Phase 11 is complete when:

- [ ] Automatic extraction creates Draft only.
- [ ] Compiled requires build evidence where applicable.
- [ ] Tested requires test evidence.
- [ ] Reviewed requires developer review.
- [ ] Proven requires successful real use.
- [ ] Trusted requires repeated successful reuse.
- [ ] No pattern is labelled bug-free.
- [ ] Private project code cannot become shared without approval.
- [ ] Regressions reduce confidence.
- [ ] Pattern application creates a reviewable patch or workflow.

---

# 23. Phase 12 — First Usable Product

## 23.1 Goal

Combine the validated vertical slices into a coherent local product suitable for daily founder use.

---

## 23.2 Required User Journeys

The product must support:

### Journey A — Understand a Project

- open project;
- inspect profile;
- index files;
- query project memory;
- inspect provenance.

### Journey B — Fix a Small Bug

- describe issue;
- review plan;
- review patch;
- run checks;
- approve apply;
- inspect result;
- prepare commit.

### Journey C — Review an Existing Change

- inspect Git diff;
- invoke reviewer;
- inspect findings;
- run tests;
- record outcome.

### Journey D — Work Offline

- use project;
- use local model;
- apply patch;
- build;
- test;
- inspect Trust Centre;
- with no cloud dependency.

### Journey E — Recover

- reopen after interrupted operation;
- inspect recovery state;
- complete or reverse safely.

---

## 23.3 Exit Criteria

Phase 12 is complete when:

- [ ] All required journeys work end to end.
- [ ] No required journey depends on cloud services.
- [ ] Major actions are visible and cancellable.
- [ ] Patch, build, Git and Trust Centre state are correlated.
- [ ] Safe Mode and Recovery Mode work.
- [ ] The reference machine remains responsive.
- [ ] Crash and recovery tests pass.
- [ ] Founder can use Opure on a real project for repeated sessions.
- [ ] Known limitations are documented.
- [ ] The product does not overclaim correctness or privacy.

---

# 24. Phase 13 — Private Alpha

## 24.1 Goal

Validate the product with a small invited group of technically capable users.

---

## 24.2 Alpha Scope

Private Alpha should focus on:

- Windows 11;
- local projects;
- Git;
- one or two build ecosystems;
- Ollama;
- one local embedding model;
- controlled workflows;
- patch review;
- and Trust Centre.

---

## 24.3 Alpha Exclusions

Private Alpha should not require:

- team collaboration;
- cloud account;
- plugin marketplace;
- remote execution;
- multi-platform support;
- mobile support;
- or automatic release publication.

---

## 24.4 Evidence Collection

Collect evidence on:

- installation;
- first project open;
- provider setup;
- workflow understanding;
- patch review;
- approval clarity;
- Runtime stability;
- model performance;
- recovery;
- and trust in external-sharing controls.

Telemetry must remain opt-in.

---

## 24.5 Exit Criteria

Private Alpha is complete when:

- [ ] Installation succeeds on representative Windows 11 systems.
- [ ] Users complete core journeys without developer intervention.
- [ ] Critical data-loss defects are absent.
- [ ] Recovery behaviour is understood.
- [ ] Approval screens are understandable.
- [ ] Local/cloud distinction is clear.
- [ ] Performance remains acceptable on supported hardware.
- [ ] Plugin and MCP features do not compromise core stability.
- [ ] Alpha feedback is converted into tracked decisions.

---

# 25. Phase 14 — Public Technical Preview

## 25.1 Goal

Release an explicitly limited preview for broader technical feedback.

---

## 25.2 Preview Requirements

Before public preview:

- installer and uninstaller must be reliable;
- data locations must be documented;
- logs and diagnostics must be accessible;
- known limitations must be prominent;
- migration policy must exist;
- update process must be testable;
- security-reporting contact must exist;
- and rollback to previous release must be possible where practical.

---

## 25.3 Preview Positioning

The preview must be described as:

- local-first;
- Windows-first;
- developer-controlled;
- early;
- and not a guarantee of production correctness.

---

## 25.4 Exit Criteria

The public preview may advance when:

- [ ] Installation and upgrade paths are tested.
- [ ] Uninstall does not delete projects.
- [ ] Project metadata backup and restore are tested.
- [ ] Security reporting process exists.
- [ ] Crash reports remain local by default.
- [ ] Public documentation matches actual behaviour.
- [ ] Major specification commitments are traceable to implementation.
- [ ] No known Critical security issue remains open.

---

# 26. Phase 15 — Beta

## 26.1 Goal

Stabilise supported workflows and expand ecosystem coverage carefully.

---

## 26.2 Beta Priorities

- reliability;
- performance;
- memory accuracy;
- patch conflict handling;
- build ecosystem support;
- plugin SDK quality;
- MCP compatibility;
- accessibility;
- upgrade stability;
- and documentation.

---

## 26.3 Possible Beta Expansion

Subject to evidence:

- second local provider;
- selected cloud providers;
- more languages;
- more build systems;
- improved syntax-aware diffs;
- richer pattern reuse;
- and more plugin examples.

---

## 26.4 Beta Exit Criteria

- [ ] Supported feature matrix is explicit.
- [ ] Upgrade migrations are reliable.
- [ ] Recovery tests pass across supported versions.
- [ ] Supported workflows have measurable success criteria.
- [ ] Security review covers all trusted-core components.
- [ ] Accessibility audit is complete.
- [ ] Performance baselines exist.
- [ ] Data deletion and export are tested.
- [ ] Public extension contracts are documented.
- [ ] Known breaking changes are resolved or scheduled before 1.0.

---

# 27. Phase 16 — Version 1.0

## 27.1 Goal

Deliver a dependable Windows-first local software-engineering platform with stable core contracts.

---

## 27.2 Version 1.0 Core Promise

Version 1.0 should allow a developer to:

- open a local project;
- understand its structure;
- use local AI;
- request a bounded engineering workflow;
- review every proposed file change;
- apply changes safely;
- build and test;
- prepare Git commits;
- inspect permissions and external sharing;
- recover interrupted work;
- and remain in control.

---

## 27.3 Required Stability

Version 1.0 requires stable major contracts for:

- Runtime services;
- Desktop Gateway;
- Workspace;
- Patch;
- AI Router;
- Workflow;
- Knowledge;
- Policy;
- Trust Centre;
- Plugin SDK;
- MCP Gateway;
- Build Manager;
- and Repository Service.

---

## 27.4 Version 1.0 Exclusions

Version 1.0 does not need:

- distributed agents;
- cloud control plane;
- team collaboration;
- hosted project storage;
- public plugin marketplace;
- mobile interface;
- autonomous release publication;
- or support for every programming language.

---

## 27.5 Version 1.0 Release Gate

- [ ] Core user journeys pass on supported systems.
- [ ] No known Critical defect remains open.
- [ ] Security threat models are current.
- [ ] Secret-handling tests pass.
- [ ] Data-loss recovery tests pass.
- [ ] Upgrade and rollback are tested.
- [ ] Installer and uninstaller are signed where appropriate.
- [ ] Accessibility requirements are met.
- [ ] Documentation is complete for supported scope.
- [ ] Plugin and MCP contracts have compatibility policy.
- [ ] Performance on reference hardware meets published expectations.
- [ ] Privacy and telemetry defaults match documentation.
- [ ] Support boundaries are explicit.

---

# 28. Post-1.0 Expansion

Post-1.0 work may include:

- Linux support;
- macOS support;
- additional desktop clients;
- remote developer-controlled inference;
- private team collaboration;
- organisation policy;
- remote build workers;
- private plugin registry;
- public plugin marketplace;
- knowledge-pack sharing;
- richer cloud-provider support;
- advanced release automation;
- and selected enterprise controls.

Each expansion requires new or amended specifications.

---

# 29. Platform Expansion Order

Recommended post-1.0 order:

1. strengthen Windows reliability;
2. broaden local provider support;
3. broaden build ecosystems;
4. stabilise plugin and MCP contracts;
5. prototype Linux Runtime;
6. prototype macOS Runtime;
7. add optional team features;
8. add remote execution where justified;
9. consider marketplace only after extension security is mature.

---

# 30. Cloud Provider Roadmap

## 30.1 Initial Position

Cloud providers are optional.

They are not required for architecture proof or first usable product.

---

## 30.2 First Cloud Integration Gate

A cloud provider may be added only when:

- Local Only is fully enforced;
- external-sharing preview is complete;
- provider credentials use the Vault;
- Network Gateway is operational;
- Trust Centre records are complete;
- retention behaviour can be described honestly;
- and no silent local-to-cloud fallback exists.

---

## 30.3 Provider Selection

Cloud-provider priority should be determined by:

- user demand;
- contract stability;
- privacy controls;
- structured-output support;
- model quality;
- cost transparency;
- and adapter maintainability.

---

# 31. Language and Build Ecosystem Roadmap

Ecosystem support should be added through evidence.

Selection criteria include:

- founder use;
- target users;
- parser availability;
- build-tool stability;
- test-tool support;
- package-manager risk;
- and adapter maintenance cost.

A new ecosystem is not complete until it supports:

- profile detection;
- file parsing;
- build;
- test;
- diagnostics;
- dependencies;
- and documentation.

---

# 32. Desktop Roadmap

Recommended desktop progression:

### Stage A

- Runtime status;
- project open;
- file tree;
- workflow list;
- basic settings.

### Stage B

- patch review;
- approvals;
- Trust Centre;
- model controls;
- build output.

### Stage C

- Memory Explorer;
- Pattern Library;
- plugin management;
- MCP management;
- recovery dashboards.

### Stage D

- richer workflow editing;
- advanced layout;
- deeper code navigation;
- and optional embedded editing.

---

# 33. Security Roadmap

Security must evolve alongside capability.

Recommended sequence:

1. workspace boundary;
2. local API authentication;
3. policy skeleton;
4. approval binding;
5. secret detection;
6. Vault;
7. Network Gateway;
8. audit integrity;
9. quarantine;
10. incident workflow;
11. signing and package trust;
12. organisation policy after 1.0.

Security must not be deferred until feature completion.

---

# 34. Recovery Roadmap

Every state-changing capability must ship with recovery.

Required recovery areas include:

- Runtime startup;
- configuration migration;
- patch apply;
- patch reverse;
- build interruption;
- dependency installation;
- repository operations;
- plugin update;
- memory migration;
- and application update.

No milestone may mark a state-changing feature complete without interruption testing.

---

# 35. Testing Roadmap

## 35.1 Foundational Testing

- unit tests;
- contract tests;
- architecture tests;
- and deterministic simulations.

---

## 35.2 Integration Testing

- project open;
- workflow;
- patch;
- build;
- Git;
- Trust Centre;
- plugin;
- MCP;
- and recovery chains.

---

## 35.3 Fault Injection

- process crash;
- disk full;
- network failure;
- model failure;
- corrupted state;
- timeout;
- denied permission;
- and cancellation.

---

## 35.4 Security Testing

- path traversal;
- secret leakage;
- prompt injection;
- plugin abuse;
- MCP abuse;
- command injection;
- approval replay;
- and endpoint change.

---

## 35.5 Performance Testing

- startup;
- idle use;
- model loading;
- streaming;
- indexing;
- large diff;
- large logs;
- and build concurrency.

---

## 35.6 Accessibility Testing

- keyboard navigation;
- screen readers;
- scaling;
- contrast;
- reduced motion;
- and diff semantics.

---

# 36. Benchmark Roadmap

Benchmarks should be introduced only after behaviour exists.

Initial benchmarks should measure:

- Runtime startup;
- project-open time;
- indexing throughput;
- memory query latency;
- patch validation time;
- patch apply time;
- model first-token latency;
- model throughput;
- desktop streaming responsiveness;
- and build-output handling.

Benchmark results must record hardware and configuration.

---

# 37. Performance Targets

Exact numerical targets should be established through measured prototypes.

Until then, targets should be expressed as:

- desktop remains responsive;
- optional services do not block readiness;
- interactive tasks pre-empt background work;
- one fast local model remains practical on the reference machine;
- large lists use virtualisation;
- and idle resource use remains low.

False precision must be avoided.

---

# 38. Technical Debt Policy

Technical debt may be accepted when:

- it is documented;
- it does not violate the Charter;
- it does not weaken security silently;
- it does not destroy service boundaries;
- it has an owner;
- and it has a review trigger.

Technical debt must not be hidden as “temporary” without a tracking record.

---

# 39. Prototype Exceptions

A prototype may use simplified implementations.

It must not:

- bypass the Policy Engine in production paths;
- store secrets in plain text;
- allow direct project writes;
- create permanent provider coupling;
- or misrepresent recovery guarantees.

Any prototype exception must be documented and removed before the relevant release gate.

---

# 40. ADR Roadmap

The ADR sequence should broadly follow:

1. implementation language;
2. desktop framework;
3. process topology;
4. IPC;
5. contract format;
6. storage;
7. logging;
8. testing;
9. path handling;
10. patch format;
11. AI adapter model;
12. workflow format;
13. Vault;
14. audit integrity;
15. plugin host;
16. MCP transport;
17. packaging and updates.

---

# 41. Documentation Roadmap

Documentation should include:

- developer setup;
- architecture overview;
- service contracts;
- ADRs;
- security model;
- plugin SDK;
- MCP integration;
- provider setup;
- project workflows;
- recovery;
- privacy;
- and supported feature matrix.

Documentation must be updated in the same milestone as behaviour changes.

---

# 42. Sample Project Roadmap

Opure should maintain sample projects for:

- basic text patching;
- Git integration;
- supported build ecosystem;
- test failures;
- dependency changes;
- plugin examples;
- MCP examples;
- recovery scenarios;
- and security tests.

Samples must not contain real secrets.

---

# 43. Internal Dogfooding

The Opure repository itself should become an Opure-managed project when the product is capable enough.

Dogfooding should validate:

- project opening;
- specification navigation;
- patch review;
- build;
- tests;
- memory;
- workflows;
- and Trust Centre.

Opure must not rely only on its own repository for validation.

---

# 44. Release Channels

Recommended channels are:

- Development;
- Internal;
- Private Alpha;
- Technical Preview;
- Beta;
- Stable.

Each channel must have:

- update policy;
- migration policy;
- rollback expectations;
- diagnostics level;
- and support boundaries.

---

# 45. Versioning

Opure should use semantic versioning once public contracts stabilise.

Before 1.0:

- breaking changes are expected;
- migrations must still be documented;
- and user data must not be abandoned silently.

---

# 46. Update Roadmap

The application-update system must eventually support:

- update discovery;
- signed packages;
- release notes;
- compatibility checks;
- migration preview;
- safe shutdown;
- update apply;
- rollback where practical;
- and Recovery Mode.

Automatic updates should not override explicit developer policy.

---

# 47. Packaging Roadmap

Initial packaging should target Windows 11.

The installer should:

- install per user where practical;
- avoid unnecessary administrator rights;
- identify data locations;
- preserve projects on uninstall;
- and support clean removal of application components.

---

# 48. Data Migration Roadmap

Every persistent store must have:

- schema version;
- migration plan;
- backup or checkpoint;
- failure recovery;
- and compatibility tests.

Migration must not silently discard data.

---

# 49. Observability Roadmap

Observability should mature through:

1. structured logs;
2. service health;
3. task state;
4. Trust Centre correlation;
5. metrics;
6. diagnostic bundles;
7. performance profiles;
8. and optional opt-in telemetry after stable local diagnostics.

---

# 50. Risk Register

The roadmap must track risks such as:

- overbuilding before first usable workflow;
- hidden service coupling;
- provider lock-in;
- poor local-model performance;
- excessive VRAM use;
- patch corruption;
- weak recovery;
- secret leakage;
- plugin sandbox limitations;
- MCP trust confusion;
- desktop complexity;
- slow indexing;
- unsupported build diversity;
- installation friction;
- and documentation drift.

---

# 51. Primary Delivery Risks

## 51.1 Scope Explosion

Mitigation:

- milestone gates;
- vertical slices;
- explicit exclusions;
- and founder approval for scope changes.

---

## 51.2 Architecture Without Product

Mitigation:

- every infrastructure phase includes a user-visible outcome;
- first patch vertical slice arrives early;
- first assisted workflow remains narrow.

---

## 51.3 AI Provider Coupling

Mitigation:

- provider adapter contract;
- simulated second adapter;
- no direct SDK use outside adapters;
- architecture tests.

---

## 51.4 Unsafe File Modification

Mitigation:

- Patch Service only;
- snapshots;
- base hashes;
- conflict detection;
- journals;
- recovery tests.

---

## 51.5 Security Deferred Too Late

Mitigation:

- policy skeleton from early phases;
- secret scanning before AI cloud support;
- Trust Centre from architecture skeleton;
- security gate for every milestone.

---

## 51.6 Desktop Overload

Mitigation:

- progressive disclosure;
- structured views;
- limited first shell;
- usability testing;
- and no attempt to clone a full IDE initially.

---

## 51.7 Weak Local Performance

Mitigation:

- Balanced default;
- one fast resident model;
- controlled model loading;
- resource metrics;
- reference-hardware benchmarks.

---

# 52. Scope Change Process

A proposed roadmap change should identify:

- problem;
- user value;
- affected specifications;
- affected milestones;
- architecture impact;
- security impact;
- performance impact;
- migration impact;
- and what existing work is displaced.

Material scope changes require founder approval.

---

# 53. Definition of Ready

A feature is ready for implementation when:

- responsibility owner is clear;
- specification exists;
- dependencies are identified;
- security implications are understood;
- success criteria are measurable;
- tests are planned;
- and user experience is described.

---

# 54. Definition of Done

A feature is done when:

- implementation exists;
- contracts are versioned;
- tests pass;
- failure paths are tested;
- recovery is tested where applicable;
- security requirements pass;
- Trust Centre behaviour exists;
- desktop state is accurate;
- documentation is updated;
- and known limitations are recorded.

---

# 55. Release Blocking Defects

The following should block a release:

- known project data loss;
- secret leakage;
- permission bypass;
- silent cloud transmission;
- patch preview mismatch;
- unrecoverable migration failure;
- plugin escape into trusted core;
- audit claiming activity that did not occur;
- or installer behaviour that deletes user projects.

---

# 56. Non-Blocking Early Limitations

Early releases may accept:

- limited language support;
- limited build-system support;
- limited binary patch support;
- basic visual design;
- incomplete plugin marketplace;
- no team collaboration;
- no Linux or macOS support;
- and limited cloud-provider coverage.

These limitations must be documented clearly.

---

# 57. Product Metrics

Useful product metrics may include:

- successful project opens;
- patch validation success;
- patch conflicts prevented;
- recovery success;
- workflow cancellation success;
- build completion;
- test evidence captured;
- secret findings blocked;
- local versus cloud routing;
- and user-reversed changes.

Metrics must be local by default unless telemetry is enabled.

---

# 58. Quality Metrics

Quality evidence may include:

- crash-free sessions;
- recovery-test pass rate;
- contract-test pass rate;
- architecture-test pass rate;
- accessibility findings;
- startup latency;
- memory use;
- VRAM use;
- and defect severity distribution.

---

# 59. Security Metrics

Security evidence may include:

- denied unauthorised capability calls;
- secret-scan coverage;
- audit-integrity verification;
- quarantine events;
- policy test coverage;
- and time to resolve security incidents.

These metrics must not expose secret or project content.

---

# 60. Governance

The founder remains Product Owner and final authority for roadmap priority.

Architecture changes should be reviewed against:

- Charter;
- specifications;
- ADRs;
- security model;
- and current milestone scope.

A small architecture review may include:

- founder;
- Runtime owner;
- security owner;
- desktop owner;
- and relevant service owner.

---

# 61. Specification Change Rules

A specification change should:

- preserve history;
- update version;
- explain rationale;
- identify affected implementation;
- identify migration impact;
- and update roadmap if scope changes.

---

# 62. Compatibility Policy

Before 1.0, internal contracts may evolve.

After public SDK and plugin contracts are released:

- compatibility rules must be documented;
- deprecation periods should be provided;
- breaking changes require major versions;
- and migration tooling should be supplied where practical.

---

# 63. Open-Source and Licensing Roadmap

Licensing decisions are DEFERRED.

Before public release, resolve:

- core platform licence;
- plugin SDK licence;
- sample licence;
- third-party dependency policy;
- model licence display;
- and contribution terms.

No licence claim should be made without legal review where required.

---

# 64. Supply-Chain Roadmap

Before public preview, establish:

- dependency locking;
- package integrity checks;
- reproducible build evidence where practical;
- signed releases;
- dependency update review;
- security scanning;
- and third-party licence inventory.

---

# 65. Privacy Roadmap

Privacy milestones include:

1. local storage map;
2. data classification;
3. conversation-retention controls;
4. raw AI-request retention controls;
5. deletion tools;
6. export tools;
7. cloud sharing history;
8. provider-retention disclosures;
9. and optional telemetry consent.

---

# 66. Accessibility Roadmap

Accessibility must be implemented incrementally.

Minimum sequence:

1. keyboard-first shell;
2. focus management;
3. screen-reader names;
4. contrast;
5. scaling;
6. reduced motion;
7. accessible diff;
8. accessible workflow timeline;
9. accessibility audit before Beta.

---

# 67. Internationalisation Roadmap

The first release may be English only.

Architecture should still avoid:

- hard-coded layout assumptions;
- concatenated user-facing strings;
- locale-insensitive dates;
- and untranslatable plugin surfaces.

---

# 68. Cross-Platform Roadmap

## 68.1 Windows First

Windows 11 is the first supported platform.

---

## 68.2 Portability Requirements

From the beginning:

- isolate path handling;
- isolate process control;
- isolate credential storage;
- isolate notification APIs;
- isolate desktop packaging;
- and avoid unnecessary Windows-only service contracts.

---

## 68.3 Linux Gate

Linux work should begin after:

- Runtime contracts stabilise;
- core local workflows are usable;
- filesystem abstractions are proven;
- and packaging effort is justified.

---

## 68.4 macOS Gate

macOS work should follow similar evidence and account for:

- sandboxing;
- signing;
- notarisation;
- keychain;
- and filesystem semantics.

---

# 69. Collaboration Roadmap

Team collaboration is post-1.0 unless evidence changes priority.

Future collaboration may require:

- identity;
- roles;
- project sharing;
- approval chains;
- shared Trust Centre;
- shared memory;
- conflict resolution;
- and remote policy.

These require separate specifications.

---

# 70. Remote Execution Roadmap

Remote build or inference execution must not be added merely as a transport change.

It requires:

- explicit trust boundary;
- authentication;
- encryption;
- data-sharing preview;
- remote secrets model;
- audit correlation;
- cancellation;
- and local fallback.

---

# 71. Marketplace Roadmap

A plugin marketplace should be considered only after:

- plugin contracts stabilise;
- signing works;
- quarantine works;
- permission UX is proven;
- update rollback is reliable;
- and security review processes exist.

---

# 72. AI Capability Expansion

After core text and embeddings, future AI capabilities may include:

- vision;
- audio;
- multimodal project artefacts;
- reranking;
- and specialised local models.

Each capability must pass:

- provider capability verification;
- privacy review;
- resource review;
- context controls;
- and desktop visibility.

---

# 73. Advanced Workflow Roadmap

Possible future workflows include:

- architecture migration;
- dependency upgrade;
- performance optimisation;
- security review;
- release preparation;
- incident diagnosis;
- project onboarding;
- and repository archaeology.

Each should begin as a bounded template.

---

# 74. Autonomy Roadmap

Opure should advance autonomy cautiously.

Recommended sequence:

1. propose only;
2. auto-run low-risk reads;
3. auto-run approved validation;
4. auto-apply bounded low-risk changes;
5. run full approved workflow;
6. consider scheduled controlled workflows;
7. avoid open-ended unsupervised autonomy.

No autonomy level may bypass policy or visibility.

---

# 75. Defer List

The following are explicitly deferred beyond the first usable product unless evidence demands otherwise:

- public plugin marketplace;
- distributed agent swarms;
- permanent cloud control plane;
- hosted source repositories;
- mobile application;
- team collaboration;
- enterprise SSO;
- organisation-wide policy;
- remote build farms;
- remote desktop;
- automatic release publication;
- advanced binary editing;
- multi-user live editing;
- and universal language support.

---

# 76. Roadmap Review Cadence

The roadmap should be reviewed:

- after each milestone gate;
- after a material architecture discovery;
- after a Critical security issue;
- after major user research;
- and before expanding platform scope.

Review should update priorities without rewriting historical evidence.

---

# 77. Roadmap Artefacts

Each milestone should produce:

- milestone brief;
- issue set;
- ADRs;
- test plan;
- threat-model update;
- demo checklist;
- exit report;
- known-limitations report;
- and next-milestone recommendation.

---

# 78. Milestone Exit Report

An exit report should include:

- what was delivered;
- what was not delivered;
- evidence;
- test results;
- security results;
- performance results;
- recovery results;
- known defects;
- technical debt;
- and approval decision.

---

# 79. Stop Conditions

A milestone should stop or narrow when:

- architecture no longer fits the Charter;
- security cannot be enforced;
- project integrity is at risk;
- performance is unacceptable;
- recovery cannot be made reliable;
- or scope prevents completion of a coherent user journey.

Stopping or narrowing is preferable to shipping hidden risk.

---

# 80. Success Definition

Opure succeeds when developers can use local intelligence to improve real software while retaining:

- ownership;
- understanding;
- review;
- permission;
- reversibility;
- provider choice;
- privacy;
- and engineering evidence.

Success is not measured by how autonomous Opure appears.

It is measured by how effectively it helps developers build and maintain software without taking control away from them.

---

# 81. Acceptance Criteria

SPEC-012 is implemented as an active roadmap when:

- [ ] Every milestone has a user-visible outcome.
- [ ] Every milestone has explicit exit criteria.
- [ ] Core Runtime work precedes high-level automation.
- [ ] Safe workspace and patching precede generated file changes.
- [ ] Local AI works before cloud dependency is introduced.
- [ ] Provider neutrality is proven by more than one adapter.
- [ ] Project memory retains provenance and isolation.
- [ ] The first workflow is narrow and complete.
- [ ] Build, test and Git evidence follow patch generation.
- [ ] Security and recovery evolve alongside features.
- [ ] Plugin and MCP capability arrive only through mediated boundaries.
- [ ] The first usable product supports complete local user journeys.
- [ ] Alpha, preview, beta and 1.0 gates are evidence-based.
- [ ] Version 1.0 has explicit exclusions.
- [ ] Cross-platform work remains possible without delaying Windows delivery.
- [ ] Technical debt is visible and owned.
- [ ] Release-blocking defects are defined.
- [ ] Documentation and accessibility are milestone requirements.
- [ ] No phase requires hidden cloud behaviour.
- [ ] Founder approval controls material scope change.
- [ ] Roadmap progress is measured by engineering evidence, not feature count.

---

# 82. Required Immediate Next Actions

After approval of this roadmap, the immediate next actions are:

1. review all founding specifications for contradictions;
2. approve or amend each Founder Draft;
3. create the first implementation ADRs;
4. select the primary implementation language;
5. select the desktop framework;
6. define Runtime process topology;
7. define the first repository source layout;
8. create the test harness;
9. create the architecture skeleton milestone;
10. and implement the first boot-to-health vertical slice.

---

# 83. Founder Approval

This document remains a founder draft until explicitly approved.

Approval establishes the following roadmap commitments:

- Opure will build foundations before breadth.
- Complexity must be earned through evidence.
- Windows 11 is the first target, not a permanent lock.
- Local value must exist before cloud value is required.
- Safe patching must exist before generated changes are trusted.
- AI remains replaceable.
- Agents remain workflows.
- Security, Trust Centre and recovery are product features.
- The first product will deliver complete local engineering journeys.
- Public releases will state limitations honestly.
- Marketplace, collaboration and distributed systems remain deferred until justified.
- Version 1.0 will prioritise developer control, reliability and evidence over feature count.
- The roadmap may evolve, but the Charter remains the governing foundation.

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**