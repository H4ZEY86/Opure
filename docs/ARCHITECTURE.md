# Opure Architecture

## System Architecture and Engineering Guide

**Document:** ARCHITECTURE.md  
**Status:** Founder Draft  
**Version:** 0.1  
**Language:** British English  
**Last updated:** 18 July 2026  
**Governing documents:** CHARTER-001 and SPEC-001 through SPEC-012  

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**

---

## 1. Purpose

This document provides a single architectural view of the Opure Platform.

It consolidates the founding specifications into an implementation-oriented system map covering:

- architecture principles;
- subsystem boundaries;
- service ownership;
- runtime topology;
- project isolation;
- data flow;
- AI routing;
- project memory;
- workflows;
- plugins;
- MCP;
- security;
- file patching;
- builds;
- repositories;
- desktop integration;
- recovery;
- observability;
- and delivery phases.

This document is a guide.

The detailed specifications remain authoritative for their respective domains.

---

## 2. What Opure Is

Opure is a local-first software engineering platform for developers.

It combines:

- project understanding;
- local and optional remote AI;
- reviewable engineering workflows;
- project memory;
- controlled tool use;
- safe file patching;
- builds and tests;
- repository operations;
- plugins;
- MCP integrations;
- policy enforcement;
- and an inspectable Trust Centre.

Opure is not primarily a chatbot.

It is not an autonomous agent swarm.

It is not a cloud service disguised as a desktop tool.

It is a software engineering platform that uses AI as one replaceable capability.

---

## 3. Governing Principles

The architecture must preserve these principles:

1. **Developer Respect**
2. **Developer First**
3. **Software Engineering First**
4. **Local by Design**
5. **Cloud Optional**
6. **Human in Control**
7. **Visible by Design**
8. **Inspectable Decisions**
9. **Reviewable Changes**
10. **Reversible Where Technically Practical**
11. **Proven Engineering**
12. **Open by Architecture**
13. **Loose Coupling**
14. **Least Privilege**
15. **Project Isolation**
16. **No Hidden Authority**
17. **No Silent Cloud Fallback**
18. **No Direct Protected File Writes**
19. **No Secret Values in Ordinary Storage**
20. **Performance Respect**

---

## 4. Architectural Style

Opure uses a modular service architecture.

The architecture favours:

- explicit service ownership;
- versioned contracts;
- local process communication;
- event-driven coordination;
- bounded state;
- replaceable adapters;
- capability-based access;
- and deterministic safety controls.

It avoids:

- unnecessary microservices;
- shared mutable global state;
- direct cross-service database access;
- provider-specific coupling;
- hidden process execution;
- and AI-controlled permissions.

The platform may contain multiple processes.

It is still one local product.

---

## 5. High-Level System Map

```text
┌───────────────────────────────────────────────────────────────────────┐
│                          Opure Desktop                                │
│                                                                       │
│  Projects  Workflows  Patches  Build  Memory  Trust  Plugins  MCP   │
└────────────────────────────────┬──────────────────────────────────────┘
                                 │
                                 ▼
┌───────────────────────────────────────────────────────────────────────┐
│                         Desktop Gateway                               │
│                                                                       │
│  View projections, commands, authentication, subscriptions           │
└────────────────────────────────┬──────────────────────────────────────┘
                                 │
                                 ▼
┌───────────────────────────────────────────────────────────────────────┐
│                          Runtime Kernel                               │
│                                                                       │
│  Bootstrap  Registry  Lifecycle  Messaging  Scheduler  Health       │
└───────────────┬─────────────────────┬─────────────────────┬───────────┘
                │                     │                     │
                ▼                     ▼                     ▼
┌───────────────────────┐  ┌───────────────────────┐  ┌───────────────────────┐
│ Engineering Services  │  │ Intelligence Services │  │ Trust and Integration │
│                       │  │                       │  │                       │
│ Project Manager       │  │ AI Router             │  │ Policy Engine         │
│ Workspace Service     │  │ Context Engine        │  │ Approval Manager      │
│ Patch Service         │  │ Knowledge Engine      │  │ Secrets Vault         │
│ Build Manager         │  │ Workflow Engine       │  │ Network Gateway       │
│ Repository Service    │  │ Pattern Library       │  │ Trust Centre          │
│ Dependency Manager    │  │                       │  │ Plugin Manager        │
│ Artefact Manager      │  │                       │  │ Plugin Host           │
│ Environment Manager   │  │                       │  │ MCP Gateway           │
└───────────────────────┘  └───────────────────────┘  └───────────────────────┘
                │                     │                     │
                └─────────────────────┴─────────────────────┘
                                 │
                                 ▼
┌───────────────────────────────────────────────────────────────────────┐
│                       External Boundaries                             │
│                                                                       │
│  Filesystem  Git  Compilers  Test Tools  Ollama  Cloud AI  MCP      │
│  Plugins  Package Registries  Remote Repositories  External APIs     │
└───────────────────────────────────────────────────────────────────────┘
```

---

## 6. Architectural Layers

Opure can be understood through six logical layers.

### 6.1 Experience Layer

Contains:

- desktop shell;
- project views;
- workflow views;
- patch review;
- approval screens;
- Trust Centre;
- settings;
- diagnostics;
- and recovery experiences.

The experience layer presents state.

It does not own authoritative engineering state.

---

### 6.2 Gateway Layer

Contains the Desktop Gateway.

It provides:

- authenticated local API access;
- view-oriented projections;
- command routing;
- subscriptions;
- reconnection;
- and stale-state reconciliation.

The desktop should not communicate directly with every internal service.

---

### 6.3 Runtime Layer

Contains:

- Runtime Bootstrap;
- Runtime Identity;
- Configuration Manager;
- Service Registry;
- Lifecycle Manager;
- Runtime Messaging;
- Scheduler;
- Health Supervisor;
- and Recovery Coordinator.

This layer starts and supervises the platform.

---

### 6.4 Engineering Layer

Contains:

- Project Manager;
- Workspace Service;
- Patch Service;
- Build Manager;
- Repository Service;
- Dependency Manager;
- Environment Manager;
- Artefact Manager;
- and Release Coordinator.

This layer owns project and engineering operations.

---

### 6.5 Intelligence Layer

Contains:

- AI Router;
- Context Engine;
- Knowledge Engine;
- Workflow Engine;
- Role Templates;
- and Proven Pattern Library.

This layer supplies intelligence without owning permissions or project files.

---

### 6.6 Trust and Integration Layer

Contains:

- Policy Engine;
- Approval Manager;
- Secrets Vault;
- Secret Detection;
- Redaction Service;
- Network Gateway;
- Trust Centre;
- Plugin Manager;
- Plugin Host;
- MCP Gateway;
- and CLI or API adapter hosts.

This layer mediates protected actions and external boundaries.

---

## 7. Runtime Kernel

The Runtime Kernel is the foundation of the local platform.

Its responsibilities include:

- creating a unique Runtime instance;
- loading configuration;
- discovering services;
- validating service dependencies;
- starting services in dependency order;
- supervising health;
- scheduling work;
- routing internal messages;
- coordinating shutdown;
- detecting abnormal termination;
- and entering Safe or Recovery Mode.

The Runtime Kernel must remain useful without AI.

---

## 8. Runtime Process Topology

The exact process model is resolved by ADR.

A likely initial topology is:

```text
Opure.Desktop.exe
        │
        ▼
Opure.Runtime.exe
        │
        ├── Trusted Core Services
        ├── Engineering Services
        ├── Intelligence Services
        ├── Adapter Hosts
        ├── Plugin Host Processes
        └── Controlled External Processes
```

Third-party plugins should run outside the trusted Runtime process.

AI providers, MCP servers and build tools remain external processes or endpoints.

---

## 9. Runtime Readiness

Runtime readiness should distinguish:

- **Starting**
- **Ready**
- **Ready with Degraded Capabilities**
- **Safe Mode**
- **Recovery Required**
- **Stopping**
- **Failed**

The desktop may become available before all optional services finish starting.

Critical services must be ready before protected work is permitted.

### 9.1 Implemented M1 Service Lifecycle

The Runtime Kernel owns the authoritative lifecycle of registered logical
services. It validates a single transition policy, starts dependencies before
dependants, stops dependants before dependencies, and applies bounded startup
and shutdown deadlines. Required dependency failure prevents readiness;
optional dependency failure produces a visible degraded state.

The Service Registry exposes a bounded projection of current lifecycle state,
the deterministic per-boot transition sequence, and stable failure category and
code. It does not expose implementation types, exception text, persistence paths
or session material.

M1 lifecycle state is in memory. After Runtime restart, state is reconstructed
from trusted service definitions and current startup checks; a stale Ready state
is never persisted or trusted. Trust Evidence publication of
`service.state-changed` remains deferred until that service exists, while
process restart budgets and crash-loop quarantine remain owned by the Process
Supervisor.

### 9.2 Implemented M1 Runtime Health UI

The Runtime Kernel remains authoritative for boot identity, readiness, overall
health and registered service lifecycle. Its authenticated Runtime Health
handler builds a bounded projection from the live Service Registry. The Desktop
Gateway validates that projection and translates it into view-oriented state;
the Desktop does not infer authority from displayed rows or stable error codes.

The Desktop distinguishes Connected, Disconnected, Starting, Ready, Degraded
and Safe Mode in text. Degraded is explicitly not healthy, Safe Mode has a
prominent restricted-operation banner, and diagnostic detail is progressively
disclosed. Service rows expose stable UI Automation labels, and the full opaque
boot identity can be copied without displaying it in the summary.

Refresh is asynchronous, serialised and bounded. When a validated connection is
lost, the last service and boot snapshot remains visible but is marked stale and
cannot be mistaken for current authority. Closing the view cancels its refresh
loop; reopening starts a new refresh and the Gateway resolves endpoint and
session material for each query. Subscription-based updates remain deferred
until the Desktop Gateway subscription contract exists.

### 9.3 Provisional M3 SQLite Persistence Boundary

`Opure.Persistence.Sqlite` is the shared trusted infrastructure boundary for
service-owned SQLite databases. It owns no domain state itself. A service creates
one authority from the Runtime-supplied absolute channel data root and its stable
service identifier; the authority derives the only permitted path beneath
`services/<owner>/databases`. A connection factory rejects foreign descriptors,
UNC roots and reparse paths before opening a database.

The fixed authoritative profile uses private non-pooled connections, verified
foreign keys, disabled trusted schema, WAL, FULL synchronous durability, a
bounded busy timeout, disabled memory mapping, an application identifier and a
quick integrity check. Callers cannot append connection-string options. Each
open database has one immediate-transaction writer gate, while a process-wide
canonical-path lease prevents a second owning writer from being opened.

The owning service remains authoritative for schema, SQL, health consequences
and recovery. Malformed or wrongly identified files are preserved and reported
with stable codes; they are never silently replaced. FND-014 deliberately does
not create a Runtime database. Migrations, transactional outboxes, backup,
restore, integrity scheduling and persistence-health publication remain later
tickets, so ADR-0005 remains Proposed.

---

## 10. Service Ownership

Every authoritative resource has one owning service.

| Resource | Owning Service |
|---|---|
| Runtime lifecycle | Runtime Kernel |
| Service registration | Service Registry |
| Scheduled tasks | Scheduler |
| Project identity | Project Manager |
| Workspace access | Workspace Service |
| Patch lifecycle | Patch Service |
| Build and test records | Build Manager |
| Repository state and operations | Repository Service |
| Dependencies | Dependency Manager |
| Environments | Environment Manager |
| Build artefacts | Artefact Manager |
| AI requests and routing | AI Router |
| Context packages | Context Engine |
| Project knowledge | Knowledge Engine |
| Workflow state | Workflow Engine |
| Plugin installation | Plugin Manager |
| Plugin execution | Plugin Host |
| MCP sessions | MCP Gateway |
| Policy decisions | Policy Engine |
| Approvals | Approval Manager |
| Secret values | Secrets Vault |
| Protected network activity | Network Gateway |
| Trust records | Trust Centre |

A service must not mutate another service's authoritative state directly.

---

## 11. Contract Rules

Service communication must use:

- versioned commands;
- versioned queries;
- versioned events;
- structured errors;
- correlation identifiers;
- cancellation;
- and declared compatibility.

Contracts should remain language-neutral where practical.

A contract should define:

- input schema;
- output schema;
- validation;
- permission requirements;
- idempotency;
- timeout;
- error model;
- and version.

---

## 12. Command, Query and Event Model

### 12.1 Commands

Commands request a state-changing action.

Examples:

- `ApplyPatch`
- `StartWorkflow`
- `RunBuild`
- `CreateCommit`
- `EnablePlugin`

Commands may be rejected.

---

### 12.2 Queries

Queries request authoritative state without changing it.

Examples:

- `GetProject`
- `GetPatch`
- `GetWorkflow`
- `GetRepositoryStatus`
- `GetTrustRecord`

---

### 12.3 Events

Events report completed or meaningful state transitions.

Examples:

- `PatchApplied`
- `WorkflowFailed`
- `BuildCompleted`
- `PluginQuarantined`
- `ProjectHealthChanged`

Events must not be used as hidden commands.

---

## 13. Correlation

Every significant engineering operation should use correlation identifiers.

A single bug-fix workflow may correlate:

```text
Developer Request
    ↓
Workflow Instance
    ↓
Context Package
    ↓
AI Request
    ↓
Patch
    ↓
Approval
    ↓
Patch Apply
    ↓
Build
    ↓
Test
    ↓
Commit
    ↓
Trust Centre Outcome
```

Correlation makes the platform inspectable.

---

## 14. Scheduler

The Scheduler coordinates local resources.

It should understand:

- task priority;
- project;
- service;
- CPU class;
- memory class;
- GPU and VRAM requirements;
- disk and network use;
- cancellation;
- and deadlines.

Recommended priorities include:

- Interactive;
- Foreground;
- Validation;
- Background;
- Maintenance.

Interactive work should pre-empt low-priority background work.

---

## 15. Performance Modes

Opure defines:

### Eco

Minimise resource use.

### Balanced

Preserve responsiveness while allowing normal engineering work.

This is the proposed default.

### Performance

Increase parallelism and throughput.

### Turbo

Use aggressive local resource limits chosen by the developer.

Performance mode never bypasses security or approvals.

---

## 16. Project Manager

The Project Manager owns:

- project identity;
- registration;
- opening;
- closing;
- removal;
- project metadata;
- project settings;
- project policy association;
- project profile;
- project health;
- and workspace association.

A project identity must remain stable when a folder moves.

A cloned repository normally becomes a separate project.

---

## 17. Project Isolation

Every project must have independent:

- identity;
- workspace roots;
- policy;
- cloud settings;
- memory namespace;
- workflow history;
- plugin permissions;
- MCP permissions;
- build profiles;
- repository state;
- and Trust Centre scope.

Cross-project access is denied by default.

---

## 18. Workspace Service

The Workspace Service is the only normal path for reading project files.

It owns:

- canonical path resolution;
- workspace boundaries;
- file identity;
- file revisions;
- file metadata;
- exclusions;
- file reading;
- file watching;
- and snapshots.

It must correctly handle Windows path semantics.

---

## 19. Workspace Boundary Rules

All path decisions must use canonical paths.

The service must defend against:

- `..` traversal;
- symbolic-link escape;
- junction escape;
- path aliasing;
- alternate data streams;
- reserved Windows names;
- unsafe UNC transitions;
- case collisions;
- and invalid path components.

A display path is not sufficient for a security decision.

---

## 20. File Identity

A file identity should include:

- project identifier;
- workspace-root identifier;
- canonical relative path;
- filesystem identity where available;
- resource type;
- and identity version.

A file revision should include:

- content hash;
- size;
- modification metadata;
- encoding;
- line endings;
- and observation time.

---

## 21. Patch Service

The Patch Service is the only normal path for protected project-file changes.

AI, plugins, MCP servers, workflows and desktop views may propose changes.

They may not write project files directly.

The Patch Service owns:

- patch format;
- patch versions;
- parsing;
- diff generation;
- validation;
- conflict detection;
- secret scanning coordination;
- approvals;
- staging;
- application;
- reversal;
- journals;
- and recovery.

---

## 22. Patch Lifecycle

Recommended states include:

- Draft;
- Generated;
- Parsed;
- Validating;
- Invalid;
- Ready for Review;
- Approval Required;
- Approved;
- Applying;
- Applied;
- Applied with Warnings;
- Conflicted;
- Rejected;
- Reversing;
- Reversed;
- Failed;
- Recovery Required;
- Superseded;
- Expired.

Material edits create a new patch version.

---

## 23. Patch Apply Flow

```text
Patch Proposal
    ↓
Schema Validation
    ↓
Path Validation
    ↓
Base Revision Validation
    ↓
Conflict Detection
    ↓
Secret Scan
    ↓
Policy Evaluation
    ↓
Developer Approval
    ↓
Pre-Apply Snapshot
    ↓
Staging
    ↓
Ordered File Operations
    ↓
Result Verification
    ↓
Post-Apply Validation
    ↓
Trust Centre Record
```

---

## 24. Patch Transaction Model

A multi-file filesystem operation is not always truly atomic.

Opure should provide an **all-or-known** transaction model using:

- durable journals;
- pre-apply snapshots;
- managed staging;
- ordered operations;
- per-operation verification;
- compensation;
- and explicit recovery state.

The platform must not overstate filesystem atomicity.

---

## 25. Conflict Handling

A patch conflicts when the workspace no longer matches its validated base.

Conflict types include:

- content hash mismatch;
- missing file;
- unexpected destination;
- rename collision;
- encoding change;
- line-ending change;
- permission change;
- link target change;
- and overlapping developer edits.

AI may propose a merge.

The merge must become a new reviewable patch.

---

## 26. Repository Service

The Repository Service owns version-control state and actions.

The initial implementation may support Git only.

It owns:

- status;
- branches;
- staging;
- unstaging;
- commits;
- history;
- diffs;
- remotes;
- fetch;
- pull;
- push;
- merge;
- rebase recovery;
- and repository diagnostics.

Patch application, staging and commit remain separate actions.

---

## 27. Git Safety Rules

Opure must:

- preserve unrelated staged work;
- scan staged content for secrets;
- show hooks as executable risk;
- avoid silent stash;
- avoid silent branch change;
- avoid silent commit;
- avoid silent push;
- treat force push as Critical risk;
- and preserve native Git recovery state.

---

## 28. Build Manager

The Build Manager owns build and test execution records.

It uses versioned:

- Build Profiles;
- Test Profiles;
- Validation Profiles;
- Command Plans;
- Environment Profiles;
- and Toolchain Records.

Detected commands are not automatically trusted.

---

## 29. Command Plan

A Command Plan should include:

- executable;
- arguments;
- working directory;
- environment summary;
- secret references;
- input files;
- expected outputs;
- timeout;
- cancellation method;
- shell use;
- privilege requirement;
- network expectation;
- and risk.

Executable and arguments remain separate.

---

## 30. Build Flow

```text
Build Request
    ↓
Profile Resolution
    ↓
Toolchain Validation
    ↓
Policy Evaluation
    ↓
Approval if Required
    ↓
Scheduler Admission
    ↓
Environment Construction
    ↓
Process Start
    ↓
Output Streaming
    ↓
Diagnostic Parsing
    ↓
Artefact Capture
    ↓
Exit Verification
    ↓
Trust and Memory Updates
```

---

## 31. Test Flow

A Test Run records:

- profile;
- test scope;
- source revision;
- environment;
- cases;
- pass;
- fail;
- skip;
- flaky status;
- duration;
- retries;
- output;
- coverage;
- and related patch or workflow.

Retries must not conceal flakiness.

---

## 32. Dependency Manager

The Dependency Manager coordinates package-manager work.

It does not replace package managers.

It owns:

- dependency discovery;
- dependency plans;
- package-manager adapters;
- manifest and lock-file analysis;
- update proposals;
- install previews;
- and dependency-operation state.

Manifest and lock-file changes should be patches.

Package scripts are executable risk.

---

## 33. Environment Manager

The Environment Manager owns non-secret execution metadata.

An Environment Profile may define:

- runtime version;
- compiler or SDK;
- virtual environment;
- non-secret variables;
- secret references;
- architecture;
- platform;
- isolation;
- and tool paths.

Secret values remain in the Secrets Vault.

---

## 34. Artefact Manager

The Artefact Manager owns metadata for:

- executables;
- libraries;
- packages;
- installers;
- archives;
- reports;
- coverage;
- documentation;
- and generated outputs.

Artefacts should retain:

- source build;
- source revision;
- hash;
- size;
- path;
- retention;
- and publication status.

Publication is a separate protected action.

---

## 35. AI Router

The AI Router is the only normal route to AI providers.

No workflow, plugin, service or UI should call an AI provider directly.

The AI Router owns:

- provider registry;
- model registry;
- capability registry;
- request routing;
- streaming;
- cancellation;
- fallback proposals;
- structured output validation;
- usage metadata;
- cost metadata;
- provider health;
- and model runtime state.

---

## 36. Provider Adapters

Provider-specific code remains inside adapters.

Initial support may include:

- Ollama;
- a simulated second provider;
- later LM Studio;
- llama.cpp;
- and approved cloud providers.

The simulated second provider is important because it proves that the architecture is not merely Ollama-specific.

---

## 37. AI Request Contract

An AI request should declare:

- task type;
- role;
- required capabilities;
- preferred locality;
- provider restrictions;
- model restrictions;
- context package;
- output schema;
- data classification;
- timeout;
- cancellation;
- retry;
- and fallback policy.

---

## 38. Local and Cloud Routing

Cloud use is optional.

Per-project modes are:

- Local Only;
- Ask Every Time;
- Approved Providers Only;
- Custom.

Local model failure must never silently trigger cloud use.

A cloud fallback is a new protected proposal.

---

## 39. Context Engine

The Context Engine assembles bounded context packages for workflows and AI requests.

It selects:

- files;
- symbols;
- project memory;
- history;
- decisions;
- errors;
- build evidence;
- test evidence;
- and pattern evidence.

It should minimise context and retain provenance.

---

## 40. Context Package

A Context Package should contain:

- project;
- task;
- requested scope;
- selected sources;
- source revisions;
- classifications;
- redactions;
- token or size estimate;
- freshness;
- and provenance.

A context package sent remotely must pass:

- project cloud policy;
- secret detection;
- redaction;
- preview;
- and approval.

---

## 41. Knowledge Engine

The Knowledge Engine owns structured project memory.

It stores records for:

- projects;
- files;
- symbols;
- relationships;
- decisions;
- errors;
- fixes;
- builds;
- tests;
- workflows;
- patches;
- conversations;
- dependencies;
- repository activity;
- and patterns.

Project memory is isolated by default.

---

## 42. Knowledge Provenance

Every durable record must identify:

- source;
- source revision;
- creation method;
- model or tool where relevant;
- confidence;
- freshness;
- validation;
- developer confirmation;
- and related evidence.

The system must distinguish:

- source evidence;
- model inference;
- tool inference;
- developer confirmation;
- and deterministic validation.

---

## 43. Search and Retrieval

The Knowledge Engine may support:

- structured queries;
- full-text search;
- graph traversal;
- semantic vector search;
- and hybrid retrieval.

Vector similarity helps find relevant information.

It does not prove correctness.

---

## 44. Embeddings

Embedding records must retain:

- provider;
- model;
- model version;
- dimensions;
- normalisation;
- source hash;
- and vector-space identity.

Incompatible embedding spaces must not be mixed.

Secrets must not be embedded.

---

## 45. Memory Freshness

File changes should:

- update file revision;
- mark dependent symbols stale;
- mark summaries stale;
- invalidate relevant embeddings;
- update relationships;
- and schedule reindexing.

The system must avoid treating stale memory as current truth.

---

## 46. Workflow Engine

The Workflow Engine coordinates multi-stage engineering work.

Agents are workflows.

They are not independent authorities.

The Workflow Engine owns:

- workflow definitions;
- versions;
- instances;
- stages;
- state transitions;
- checkpoints;
- approvals;
- retries;
- compensation;
- pause;
- resume;
- cancellation;
- and outcomes.

---

## 47. Workflow Roles

Built-in roles may include:

- Planner;
- Architect;
- Coder;
- Reviewer;
- Tester;
- Documentation;
- Git;
- Security Reviewer;
- Performance Reviewer;
- and Release Coordinator.

A role is defined by:

- task;
- prompt template;
- context;
- allowed capabilities;
- expected output;
- and validation.

Multiple roles may use the same model.

---

## 48. Workflow Stage Types

Supported stage types may include:

- service query;
- service command;
- AI request;
- context retrieval;
- deterministic transform;
- validation;
- approval;
- event wait;
- manual input;
- sub-workflow;
- compensation;
- and delay.

Unbounded loops are prohibited.

---

## 49. Workflow State

A workflow should support:

- Created;
- Validating;
- Ready;
- Running;
- Paused;
- Waiting for Approval;
- Waiting for Input;
- Cancelling;
- Cancelled;
- Completed;
- Completed with Warnings;
- Failed;
- Compensating;
- Rolled Back;
- Recovery Required;
- Superseded.

---

## 50. Workflow Safety

A workflow cannot:

- grant itself permission;
- change project policy;
- bypass service ownership;
- write files directly;
- access secrets directly;
- access providers directly;
- access MCP directly;
- or hide a material plan change.

Material plan changes require renewed approval.

---

## 51. Workflow Checkpoints

Checkpoints should occur:

- before state-changing work;
- after expensive stages;
- before approval waits;
- before external publication;
- and during long workflows.

Resume must validate:

- project state;
- workflow version;
- permissions;
- approvals;
- file revisions;
- provider state;
- and checkpoint integrity.

---

## 52. Proven Pattern Library

The Pattern Library turns successful engineering evidence into reusable patterns.

Lifecycle states include:

- Draft;
- Compiled;
- Tested;
- Reviewed;
- Proven;
- Trusted;
- Deprecated;
- Rejected.

Automatic extraction creates Draft only.

No pattern may be described as bug-free.

---

## 53. Pattern Reuse

A pattern should retain:

- language;
- framework;
- purpose;
- dependencies;
- evidence;
- known limitations;
- compatibility;
- reuse count;
- failures;
- and confidence.

Applying a pattern creates a new workflow or patch.

It never silently copies code.

---

## 54. Plugin System

Plugins extend Opure through native capability contracts.

A plugin may provide:

- commands;
- workflows;
- workflow stages;
- parsers;
- build adapters;
- provider adapters;
- repository adapters;
- views;
- validators;
- and external connectors.

A plugin may extend Opure.

It may not bypass Opure.

---

## 55. Plugin Package

A plugin package contains:

- manifest;
- executable code;
- assets;
- schemas;
- documentation;
- licence;
- integrity metadata;
- and optional signature.

Installation does not grant permissions automatically.

---

## 56. Plugin Runtime

Third-party plugins should run outside the trusted core.

The Plugin Host should enforce:

- process isolation;
- capability mediation;
- memory limits;
- CPU limits;
- timeouts;
- output limits;
- cancellation;
- and crash containment.

Repeated crashes may quarantine a plugin.

---

## 57. Plugin Permissions

Plugin permissions may be scoped by:

- project;
- file;
- directory;
- workflow;
- network destination;
- secret reference;
- session;
- and time.

Plugins use:

- Workspace Service for reads;
- Patch Service for writes;
- AI Router for AI;
- Network Gateway for network;
- Secrets Vault for secrets;
- MCP Gateway for MCP.

---

## 58. MCP Gateway

MCP provides external capabilities through a mediated gateway.

An MCP server is an external trust boundary.

Capability discovery does not equal permission.

The MCP Gateway owns:

- server registry;
- session state;
- capability discovery;
- schema validation;
- invocation;
- cancellation;
- permission binding;
- secret references;
- and Trust Centre records.

---

## 59. MCP Flow

```text
Workflow or User Requests Capability
    ↓
MCP Gateway Resolves Server
    ↓
Capability and Schema Validation
    ↓
Policy Evaluation
    ↓
Approval if Required
    ↓
Secret and Network Controls
    ↓
Invocation
    ↓
Response Validation
    ↓
Untrusted Result Returned
    ↓
Trust Centre Record
```

AI and plugins must not bypass the MCP Gateway.

---

## 60. Policy Engine

The Policy Engine makes deterministic security decisions.

It evaluates:

- subject;
- project;
- workflow;
- stage;
- capability;
- target;
- data classification;
- destination;
- provider;
- model;
- risk;
- reversibility;
- active approvals;
- Runtime mode;
- and policy version.

Results include:

- Allow;
- Allow with Conditions;
- Approval Required;
- Deny;
- Unavailable Safely;
- Recovery Required.

AI may explain policy.

AI may not decide policy.

---

## 61. Approval Manager

An approval is a bounded authorisation.

It should bind to:

- subject;
- action;
- project;
- target;
- exact or bounded scope;
- destination;
- data class;
- expiry;
- policy version;
- and single-use state.

Material change invalidates approval.

---

## 62. Data Classification

Recommended classes are:

- Public;
- Project Internal;
- Sensitive;
- Secret;
- Restricted External Sharing.

Derived content inherits the strongest relevant classification unless verified redaction changes the result.

---

## 63. Secrets Vault

Secret values belong only in the Secrets Vault.

They must not be stored in:

- project memory;
- vector indexes;
- logs;
- Trust Centre records;
- workflow checkpoints;
- plugin storage;
- ordinary configuration;
- or normal databases.

Where practical, services should use a secret without receiving its raw value.

---

## 64. Secret Detection

Secret detection should run before:

- external AI requests;
- external API calls;
- network uploads;
- patch application;
- Git staging;
- Git commit;
- memory ingestion;
- embedding generation;
- diagnostics export;
- and plugin-data export.

Detection is not perfect.

High-confidence findings should block by default.

---

## 65. Network Gateway

Protected network activity passes through the Network Gateway where technically enforceable.

A network request should identify:

- initiating subject;
- project;
- destination;
- protocol;
- method;
- purpose;
- data classification;
- payload size;
- secret reference;
- approval;
- and timeout.

Localhost is not automatically trusted.

Redirects may require renewed evaluation.

---

## 66. Trust Centre

The Trust Centre is the platform's inspectable record of significant activity.

It should correlate:

- developer intent;
- workflow;
- stage;
- model;
- provider;
- context;
- files;
- tools;
- commands;
- network;
- approvals;
- patches;
- builds;
- tests;
- Git;
- plugins;
- MCP;
- incidents;
- and outcomes.

Secret values must never be recorded.

---

## 67. Trust Record Detail

Trust Centre records should support three levels:

1. concise summary;
2. structured action detail;
3. technical evidence.

The Trust Centre exists for:

- understanding;
- accountability;
- diagnosis;
- recovery;
- and security investigation.

It must not become surveillance or meaningless log noise.

---

## 68. Audit Integrity

Significant records should be tamper-evident where practical.

Possible techniques include:

- append-oriented storage;
- sequence numbers;
- chained hashes;
- signed checkpoints;
- and immutable segments.

Corrections create new records.

They do not silently rewrite history.

---

## 69. Safe Mode

Safe Mode should disable or restrict:

- third-party plugins;
- non-essential MCP servers;
- custom provider adapters;
- automatic workflows;
- background network activity;
- and untrusted imported packages.

Core project inspection and recovery should remain available.

---

## 70. Recovery Mode

Recovery Mode should activate after:

- abnormal shutdown;
- incomplete patch transaction;
- failed migration;
- repository operation in progress;
- corrupted state;
- unsafe plugin behaviour;
- or audit integrity failure.

Recovery should prioritise integrity over automatic completion.

---

## 71. Desktop Gateway

The Desktop Gateway presents stable view-oriented contracts.

It should provide projections for:

- Home;
- Project;
- Workflows;
- Patches;
- Approvals;
- Trust Centre;
- Providers;
- Models;
- Memory;
- Build;
- Tests;
- Repository;
- Plugins;
- MCP;
- Settings;
- Notifications;
- and Recovery.

---

## 72. Desktop UI

The desktop is a command and projection surface.

It should not own authoritative domain state.

Primary areas include:

- Home;
- Project;
- Workflows;
- Patches;
- Build and Test;
- Memory;
- Trust Centre;
- Plugins;
- MCP;
- Providers;
- Settings;
- and Recovery.

Chat is one surface.

It is not the whole product.

---

## 73. Desktop State Honesty

The UI must distinguish:

- planned;
- proposed;
- queued;
- running;
- waiting;
- approved;
- applied;
- validated;
- failed;
- cancelled;
- reversed;
- and recovered.

A patch must not appear applied before the Patch Service confirms it.

A test must not appear run when it was only proposed.

---

## 74. Desktop Accessibility

The desktop should support:

- full keyboard navigation;
- visible focus;
- screen-reader semantics;
- sufficient contrast;
- scaling;
- reduced motion;
- colour-independent state;
- and accessible diff representation.

Accessibility is a product requirement.

---

## 75. Desktop Performance

Long work must not block the UI thread.

Large:

- diffs;
- logs;
- project trees;
- workflow timelines;
- Trust Centre lists;
- and test results

should use virtualisation or incremental rendering.

---

## 76. External Boundaries

Opure treats the following as trust boundaries:

- project files;
- build scripts;
- package scripts;
- Git hooks;
- plugins;
- MCP servers;
- AI providers;
- AI output;
- cloud APIs;
- package registries;
- remote repositories;
- imported patches;
- imported workflows;
- imported knowledge;
- and external process output.

External data is never automatically authoritative.

---

## 77. Primary Data Flows

### 77.1 Open Project

```text
Desktop
    ↓
Desktop Gateway
    ↓
Project Manager
    ↓
Workspace Service
    ↓
Repository Service
    ↓
Knowledge Engine
    ↓
Project Health Projection
```

---

### 77.2 Local AI Question

```text
Developer Request
    ↓
Workflow or Conversation Service
    ↓
Context Engine
    ↓
Policy Check
    ↓
AI Router
    ↓
Local Provider Adapter
    ↓
Model
    ↓
Structured Response
    ↓
Trust Centre
    ↓
Desktop
```

---

### 77.3 AI-Assisted Patch

```text
Developer Intent
    ↓
Workflow Engine
    ↓
Context Engine
    ↓
AI Router
    ↓
Coder Role Output
    ↓
Patch Service
    ↓
Validation and Secret Scan
    ↓
Developer Review
    ↓
Approval
    ↓
Patch Apply
    ↓
Build and Test
    ↓
Reviewer
    ↓
Trust Centre Outcome
```

---

### 77.4 Cloud AI Request

```text
Developer Request
    ↓
Context Package
    ↓
Data Classification
    ↓
Secret Detection
    ↓
Redaction
    ↓
Project Cloud Policy
    ↓
External Sharing Preview
    ↓
Developer Approval
    ↓
Network Gateway
    ↓
Remote Provider
    ↓
Response Validation
    ↓
Trust Centre
```

---

### 77.5 Plugin Capability

```text
Developer or Workflow
    ↓
Plugin Manager
    ↓
Permission Check
    ↓
Plugin Host
    ↓
Mediated Service Capabilities
    ↓
Structured Result
    ↓
Trust Centre
```

---

### 77.6 MCP Capability

```text
Developer or Workflow
    ↓
MCP Gateway
    ↓
Capability Validation
    ↓
Policy and Approval
    ↓
Network and Secret Controls
    ↓
MCP Server
    ↓
Untrusted Result
    ↓
Validation
    ↓
Trust Centre
```

---

### 77.7 Build and Commit

```text
Applied Patch
    ↓
Build Manager
    ↓
Build
    ↓
Tests
    ↓
Repository Service
    ↓
Stage Exact Changes
    ↓
Secret Scan
    ↓
Commit Approval
    ↓
Commit
    ↓
Trust Centre
```

---

## 78. Storage Domains

Opure should separate storage by responsibility.

Likely storage domains include:

- Runtime state;
- project registry;
- project settings;
- workflow definitions and instances;
- Knowledge Engine structured store;
- vector indexes;
- Trust Centre records;
- patch metadata;
- transaction journals;
- snapshots;
- plugin metadata;
- plugin private storage;
- build and test records;
- artefact metadata;
- and encrypted secrets.

No service should read another service's private tables directly.

---

## 79. Storage Safety

Every persistent store should have:

- schema version;
- migration path;
- backup or checkpoint strategy;
- corruption detection;
- retention policy;
- deletion policy;
- and recovery tests.

Migrations must not silently discard user data.

---

## 80. Local API Security

Localhost is not sufficient authentication.

The Desktop Gateway and local APIs should use:

- authenticated sessions;
- scoped tokens;
- short lifetimes;
- revocation;
- and authorised commands.

Third-party local processes must not gain access merely by running on the same machine.

---

## 81. Failure Model

Failures should remain local to their owning service where practical.

Examples:

- AI provider failure should not block project browsing.
- plugin failure should not stop the Runtime.
- Memory Engine rebuild should not block patch review.
- Git failure should not invalidate applied files.
- desktop disconnection should not corrupt workflow state.
- optional MCP failure should not block local engineering work.

---

## 82. Cancellation Model

Cancellation propagates through:

- Workflow Engine;
- Scheduler;
- AI Router;
- Build Manager;
- Plugin Host;
- MCP Gateway;
- CLI processes;
- and Patch Service safe points.

Cancellation must report exact final state.

A cancellation request does not prove the underlying external process stopped.

---

## 83. Retry Model

Retries must be:

- bounded;
- visible;
- idempotency-aware;
- deadline-aware;
- and policy-aware.

Non-idempotent operations require conservative retry behaviour.

Repeated failure should trigger developer review or recovery.

---

## 84. Recovery Model

State-changing operations must include recovery design.

Required recovery areas include:

- Runtime startup;
- service restart;
- configuration migration;
- patch application;
- patch reversal;
- build interruption;
- dependency installation;
- repository operations;
- plugin update;
- memory migration;
- and application update.

No state-changing feature is complete without interruption testing.

---

## 85. Observability

Opure observability includes:

- structured logs;
- service health;
- task state;
- workflow timelines;
- Trust Centre records;
- resource metrics;
- diagnostics;
- and recovery journals.

Logs are not the same as Trust Centre records.

Logs serve technical diagnosis.

Trust records serve developer understanding and accountability.

---

## 86. Logging Rules

Logs should include:

- timestamp;
- severity;
- service;
- Runtime instance;
- project where relevant;
- correlation identifier;
- safe message;
- and stable error code.

Logs must not include secret values.

---

## 87. Error Model

Errors should include:

- stable code;
- safe human message;
- technical detail;
- current state;
- recoverability;
- retry guidance;
- and correlation identifier.

Generic “Something went wrong” messages are insufficient.

---

## 88. Architecture Tests

Architecture tests should enforce:

- no direct provider SDK use outside adapters;
- no direct protected file writes outside Patch Service;
- no direct secret access outside Vault contracts;
- no direct MCP access outside MCP Gateway;
- no direct plugin process trust;
- no direct cross-service database access;
- no policy bypass;
- and no desktop ownership of domain state.

---

## 89. Security Tests

Security testing should cover:

- path traversal;
- junction and symlink escape;
- secret leakage;
- approval replay;
- command injection;
- prompt injection;
- plugin abuse;
- MCP abuse;
- network redirect;
- insecure transport;
- malicious patch;
- package script risk;
- Git hook execution;
- and privilege escalation.

---

## 90. Fault Injection

Fault-injection tests should simulate:

- Runtime crash;
- service crash;
- disk full;
- provider failure;
- network failure;
- corrupted store;
- transaction interruption;
- model timeout;
- build timeout;
- child-process leak;
- plugin crash loop;
- MCP failure;
- and audit unavailability.

---

## 91. Reference Hardware Behaviour

On the reference machine, Opure should aim to:

- remain responsive during local inference;
- keep one fast model loaded where practical;
- load heavier models only when needed;
- avoid unnecessary concurrent models;
- bound indexing work;
- stream build output smoothly;
- avoid idle CPU waste;
- and expose resource use.

Exact targets should be set from measured prototypes.

---

## 92. Initial Implementation Sequence

The recommended sequence is:

1. approve the specification baseline;
2. create architecture ADRs;
3. build Runtime skeleton;
4. build Desktop Gateway and shell;
5. open projects read-only;
6. implement Workspace Service;
7. implement Patch Service;
8. prove transactional patch application;
9. implement AI Router;
10. add Ollama adapter;
11. add simulated second provider;
12. implement Knowledge Engine;
13. implement Workflow Engine;
14. deliver first bug-fix workflow;
15. add Build Manager;
16. add Repository Service writes;
17. complete Trust Centre and security;
18. implement Plugin SDK;
19. implement MCP Gateway;
20. implement Proven Pattern Library;
21. deliver first usable product;
22. begin Private Alpha.

---

## 93. First Vertical Slice

The first meaningful engineering vertical slice should be:

```text
Open Project
    ↓
Create or Import Patch
    ↓
Review Diff
    ↓
Approve
    ↓
Apply Transactionally
    ↓
Verify
    ↓
Reverse
    ↓
Inspect Trust Record
```

This proves core value before AI is required.

---

## 94. First AI Vertical Slice

The first AI-assisted vertical slice should be:

```text
Describe Small Bug
    ↓
Retrieve Local Context
    ↓
Local Model Produces Plan
    ↓
Local Model Produces Patch
    ↓
Patch Validation
    ↓
Developer Review
    ↓
Apply
    ↓
Build or Test
    ↓
Review Outcome
```

This workflow should remain narrow and complete.

---

## 95. Version 1.0 Architecture Scope

Version 1.0 should provide stable core contracts for:

- Runtime;
- Desktop Gateway;
- Project Manager;
- Workspace Service;
- Patch Service;
- AI Router;
- Context Engine;
- Knowledge Engine;
- Workflow Engine;
- Policy Engine;
- Trust Centre;
- Secrets Vault;
- Network Gateway;
- Plugin SDK;
- MCP Gateway;
- Build Manager;
- Repository Service;
- and core desktop views.

---

## 96. Version 1.0 Exclusions

Version 1.0 does not require:

- distributed agent swarms;
- hosted project storage;
- permanent cloud control plane;
- team collaboration;
- public plugin marketplace;
- mobile application;
- remote build farms;
- autonomous release publication;
- universal language support;
- or multi-user live editing.

---

## 97. Cross-Platform Strategy

Windows 11 is the first target.

Portability should be preserved by isolating:

- path handling;
- process control;
- credential storage;
- notifications;
- packaging;
- filesystem watching;
- and desktop platform services.

Linux and macOS should be future platform adapters, not rewrites of domain logic.

---

## 98. Deferred Architectural Decisions

Major decisions requiring ADRs include:

- primary implementation language;
- desktop framework;
- local IPC;
- process topology;
- contract serialisation;
- storage engine;
- vector store;
- patch format;
- diff engine;
- Runtime messaging;
- workflow definition format;
- Vault encryption;
- audit integrity;
- plugin host technology;
- MCP transport implementation;
- process isolation;
- and Windows packaging.

---

## 99. Repository Navigation

The founding documents are:

```text
specs/
├── CHARTER-001.md
├── SPEC-001.md
├── SPEC-002.md
├── SPEC-003.md
├── SPEC-004.md
├── SPEC-005.md
├── SPEC-006.md
├── SPEC-007.md
├── SPEC-008.md
├── SPEC-009.md
├── SPEC-010.md
├── SPEC-011.md
└── SPEC-012.md
```

This architecture document belongs at repository root:

```text
C:\Opure\ARCHITECTURE.md
```

---

## 100. Specification Map

| Document | Subject |
|---|---|
| CHARTER-001 | Founding principles |
| SPEC-001 | Vision and design principles |
| SPEC-002 | Runtime Kernel |
| SPEC-003 | Service architecture |
| SPEC-004 | AI Router |
| SPEC-005 | Memory Engine |
| SPEC-006 | Workflow and Agent System |
| SPEC-007 | Plugin SDK |
| SPEC-008 | Trust Centre and Security |
| SPEC-009 | Workspace and File Patch Engine |
| SPEC-010 | Desktop User Interface |
| SPEC-011 | Project and Build Management |
| SPEC-012 | Delivery roadmap |

---

## 101. Architectural Invariants

The following rules should be enforced as architectural invariants:

1. AI providers are accessed only through the AI Router.
2. Protected project writes are performed only through the Patch Service.
3. Project reads are mediated by the Workspace Service.
4. Secrets are stored only in the Secrets Vault.
5. Protected network activity is mediated by the Network Gateway.
6. MCP access is mediated by the MCP Gateway.
7. Third-party plugin execution is isolated from the trusted core.
8. Policy decisions are deterministic.
9. AI cannot grant permissions.
10. Approval binds to exact or bounded scope.
11. Material change invalidates approval.
12. Project memory is isolated by project.
13. Durable knowledge retains provenance.
14. Vector similarity is not treated as proof.
15. Workflows cannot escalate their own authority.
16. Desktop views do not own authoritative state.
17. Patch application, Git staging and commit remain separate.
18. Cloud fallback is never silent.
19. Secret values never enter Trust Centre records.
20. Recovery is required for every state-changing capability.

---

## 102. Architecture Review Questions

Every material design should answer:

1. Which service owns the state?
2. Which contract exposes it?
3. What permissions are required?
4. What is the trust boundary?
5. What happens offline?
6. What happens when the service crashes?
7. Can the operation be cancelled?
8. Can the operation be reversed?
9. What evidence is recorded?
10. Can secret values appear?
11. Can project data leave the machine?
12. What invalidates approval?
13. What happens after Runtime restart?
14. Can another provider or adapter replace this implementation?
15. Does the design respect the developer?

---

## 103. Definition of Architectural Completion

An architectural capability is not complete until:

- ownership is explicit;
- contracts are versioned;
- security policy exists;
- cancellation is designed;
- recovery is tested;
- Trust Centre behaviour exists;
- desktop state is honest;
- performance is measured;
- documentation is updated;
- and known limitations are recorded.

---

## 104. Final Architecture Statement

Opure is designed as a local, modular and developer-controlled engineering platform.

Its intelligence is useful because it is bounded.

Its automation is valuable because it is visible.

Its integrations are powerful because they are mediated.

Its memory is trustworthy because it retains evidence.

Its changes are safe because they are reviewable.

Its architecture is open because providers and tools are replaceable.

Its platform exists to strengthen developers, not to obscure or replace them.

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**
