# Opure Specifications

## Canonical Document Index, Governance and Maintenance Guide

**Repository location:** `C:\Opure\specs\README.md`
**Status:** Proposed
**Version:** 0.1
**Language:** British English
**Last updated:** 18 July 2026
**Owner:** Christopher Dyer, Founder and Product Owner
**Applies to:** All documents in `C:\Opure\specs\`
**Related architecture records:** `C:\Opure\adr\`
**Related implementation programme:** `ROADMAP-001-foundation-implementation-sequence.md`
**Related first backlog:** `BACKLOG-001-foundation-first-12-weeks.md`

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**

---

# 1. Purpose

This file is the canonical index and maintenance guide for the Opure specification set.

It exists to make the specifications:

* easy to discover;
* easy to read in the correct order;
* easy to maintain;
* explicit about authority;
* explicit about dependencies;
* traceable to Architecture Decision Records;
* traceable to implementation roadmaps and backlogs;
* clear about status and approval;
* resistant to duplicate or ambiguous filenames;
* and understandable to a founder-led engineering team.

This file is not itself a product specification.

It does not replace:

* CHARTER-001;
* SPEC-001 through SPEC-012;
* ROADMAP-001;
* BACKLOG-001;
* or the ADR sequence.

It explains how those documents fit together.

---

# 2. Canonical Specifications Folder

The canonical folder is:

```text
C:\Opure\specs\
```

The initial canonical contents are:

```text
specs\
├── README.md
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
├── SPEC-012.md
├── ROADMAP-001-foundation-implementation-sequence.md
└── BACKLOG-001-foundation-first-12-weeks.md
```

Files such as:

```text
CHARTER-001(2).md
SPEC-001(2).md
ROADMAP-001-foundation-implementation-sequence(2).md
```

are download duplicates.

They are not canonical repository filenames.

They should not remain in the repository when the corresponding clean filename exists.

---

# 3. Governing Hierarchy

The Opure document hierarchy is:

```text
CHARTER-001
    ↓
SPEC-001
    ↓
SPEC-002 through SPEC-011
    ↓
SPEC-012
    ↓
ADR-0001 through ADR-0028
    ↓
ROADMAP-001
    ↓
BACKLOG-001
    ↓
Issues, branches, commits, tests and evidence
```

The hierarchy means:

1. **CHARTER-001** establishes the permanent founding principles.
2. **SPEC-001** translates those principles into normative product and engineering direction.
3. **SPEC-002 through SPEC-011** define the major platform capability areas.
4. **SPEC-012** defines high-level delivery phases and evidence gates.
5. **ADRs** select concrete architecture and implementation decisions.
6. **ROADMAP-001** orders the foundational implementation programme.
7. **BACKLOG-001** converts the first twelve weeks into executable tickets.
8. **Implementation evidence** proves whether proposed decisions should be accepted.

---

# 4. Conflict Resolution

When documents conflict, apply the following order.

## 4.1 Founding Principle Conflict

CHARTER-001 takes precedence.

---

## 4.2 Normative Product Conflict

SPEC-001 takes precedence over later specifications unless SPEC-001 is formally amended.

---

## 4.3 Domain Conflict

The specification that owns the relevant domain takes precedence over a general description in another specification.

Examples:

* Runtime lifecycle belongs to SPEC-002.
* Service ownership belongs to SPEC-003.
* AI routing belongs to SPEC-004.
* Memory and knowledge belong to SPEC-005.
* Workflows and agents belong to SPEC-006.
* Plugins belong to SPEC-007.
* Trust, permissions and security belong to SPEC-008.
* Workspace and patches belong to SPEC-009.
* Desktop behaviour belongs to SPEC-010.
* Projects, builds and repositories belong to SPEC-011.
* Delivery gates belong to SPEC-012.

---

## 4.4 Implementation Conflict

An Accepted ADR controls the implementation decision within the bounds of the Charter and specifications.

A Proposed ADR remains provisional until evidence is recorded.

---

## 4.5 Roadmap Conflict

SPEC-012 controls high-level evidence-gated delivery.

ROADMAP-001 controls the detailed foundational implementation order.

BACKLOG-001 controls the initial issue-level sequence.

---

## 4.6 Code Conflict

Code does not silently override a specification or ADR.

A meaningful conflict discovered in implementation requires:

* a defect;
* an architecture review;
* an ADR amendment or replacement;
* a specification amendment where required;
* and founder approval where product principles or scope are affected.

---

# 5. Reading Order

A new contributor should read the documents in this order.

## 5.1 Essential Orientation

1. `CHARTER-001.md`
2. `SPEC-001.md`
3. this `README.md`

---

## 5.2 Foundation Architecture

4. `SPEC-002.md`
5. `SPEC-003.md`
6. `SPEC-008.md`
7. `SPEC-009.md`
8. `SPEC-010.md`
9. `SPEC-011.md`

---

## 5.3 Intelligence and Extension Architecture

10. `SPEC-004.md`
11. `SPEC-005.md`
12. `SPEC-006.md`
13. `SPEC-007.md`

---

## 5.4 Delivery

14. `SPEC-012.md`
15. `ROADMAP-001-foundation-implementation-sequence.md`
16. `BACKLOG-001-foundation-first-12-weeks.md`

---

## 5.5 Implementation Decisions

Then read the relevant ADRs from:

```text
C:\Opure\adr\
```

Do not begin with every ADR unless the task requires broad architecture context.

Use the traceability tables in this file to identify the relevant records.

---

# 6. Document Catalogue

# 6.1 CHARTER-001

**Canonical filename:** `CHARTER-001.md`
**Title:** The Opure Charter
**Subtitle:** The Founding Principles of the Opure Platform
**Status:** Founder Draft
**Version:** 0.1
**Depends on:** None
**Authority:** Highest product and engineering principle authority

## Purpose

CHARTER-001 defines:

* what Opure is;
* why Opure exists;
* the platform mission;
* the platform vision;
* the developer-respect principle;
* the local-first commitment;
* the human-authority commitment;
* the privacy commitment;
* the explainability commitment;
* the reversibility commitment;
* and the test against which future product choices must be judged.

## Key Statements

```text
Developer Respect. Local Intelligence. Complete Control.
```

```text
Build software with developers, not instead of them.
```

```text
Opure is a software engineering platform that uses AI,
not an AI platform that happens to write code.
```

## Change Threshold

Changes require explicit founder approval.

A change that weakens developer authority, privacy, local operation or ownership should be treated as a constitutional amendment to the project.

---

# 6.2 SPEC-001

**Canonical filename:** `SPEC-001.md`
**Title:** Vision and Design Principles
**Subtitle:** Opure Platform Product Direction
**Status:** Founder Draft
**Version:** 0.1
**Depends on:** CHARTER-001
**Authority:** Normative product and engineering direction

## Purpose

SPEC-001 translates the Charter into requirements for:

* product decisions;
* user experience;
* architecture;
* AI behaviour;
* automation;
* integrations;
* privacy;
* security;
* performance;
* and future specifications.

## Important Rules

* `MUST` is required.
* `MUST NOT` is prohibited.
* `SHOULD` requires documented justification when not followed.
* `SHOULD NOT` requires documented justification when followed.
* `MAY` is optional.
* `DEFERRED` is intentionally postponed.

A violation of a `MUST` or `MUST NOT` requirement requires an approved amendment.

---

# 6.3 SPEC-002

**Canonical filename:** `SPEC-002.md`
**Title:** Runtime Kernel
**Subtitle:** Opure Platform Runtime Foundation
**Status:** Founder Draft
**Version:** 0.1
**Depends on:** CHARTER-001, SPEC-001
**Primary ADRs:** ADR-0003, ADR-0004, ADR-0005, ADR-0006, ADR-0007

## Purpose

SPEC-002 defines the Runtime Kernel responsible for:

* bootstrap;
* deterministic startup and shutdown;
* service lifecycle;
* dependency validation;
* health;
* scheduling coordination;
* local messaging;
* resource awareness;
* crash recovery;
* diagnostics;
* and policy-enforcement entry points.

## Non-Responsibilities

The Runtime Kernel does not own:

* AI reasoning;
* project files;
* project knowledge;
* workflows;
* plugins;
* MCP business logic;
* or secrets.

---

# 6.4 SPEC-003

**Canonical filename:** `SPEC-003.md`
**Title:** Service Architecture
**Subtitle:** Opure Platform Service Boundaries and Contracts
**Status:** Founder Draft
**Version:** 0.1
**Depends on:** CHARTER-001, SPEC-001, SPEC-002
**Primary ADRs:** ADR-0003, ADR-0004, ADR-0005, ADR-0010

## Purpose

SPEC-003 defines:

* major services;
* ownership;
* service contracts;
* dependency rules;
* data ownership;
* process-isolation reasons;
* failure isolation;
* and anti-coupling rules.

## Core Rule

```text
Every major service owns one clear responsibility
and communicates through explicit contracts.
```

## Important Constraint

Logical services do not all need separate processes.

Process boundaries exist for justified:

* trust;
* crash isolation;
* resource isolation;
* external runtime;
* or lifecycle reasons.

---

# 6.5 SPEC-004

**Canonical filename:** `SPEC-004.md`
**Title:** AI Router
**Subtitle:** Opure Platform Provider-Neutral Intelligence Routing
**Status:** Founder Draft
**Version:** 0.1
**Depends on:** CHARTER-001, SPEC-001, SPEC-002, SPEC-003
**Primary ADRs:** ADR-0019, ADR-0020, ADR-0021, ADR-0024

## Purpose

SPEC-004 defines the provider-neutral AI Router responsible for:

* provider registration;
* model discovery;
* capability description;
* routing;
* local and cloud policy;
* streaming;
* cancellation;
* provider health;
* usage;
* provenance;
* and bounded retry or fallback.

## Core Rule

```text
AI is a replaceable engineering capability,
not the authority or identity of Opure.
```

## Gate A Status

Not implemented during the first twelve-week foundation backlog.

Contracts and service boundaries may be reserved.

No AI process should start during Founder Gate A.

---

# 6.6 SPEC-005

**Canonical filename:** `SPEC-005.md`
**Title:** Memory Engine
**Subtitle:** Opure Platform Project Knowledge and Proven Engineering Memory
**Status:** Founder Draft
**Version:** 0.1
**Depends on:** CHARTER-001, SPEC-001, SPEC-002, SPEC-003, SPEC-004
**Primary ADRs:** ADR-0022, ADR-0023

## Purpose

SPEC-005 defines:

* Project Knowledge;
* Project Memory;
* provenance;
* confidence;
* relationship graphs;
* indexing;
* semantic retrieval;
* accepted decisions;
* error and fix memory;
* deletion;
* reindexing;
* and memory correction.

## Core Rule

```text
Project memory must strengthen developer understanding,
not replace it.
```

## Gate A Status

Not implemented during the first twelve weeks.

Workspace Snapshot foundations are built first.

---

# 6.7 SPEC-006

**Canonical filename:** `SPEC-006.md`
**Title:** Workflow and Agent System
**Subtitle:** Opure Platform Controlled Engineering Automation
**Status:** Founder Draft
**Version:** 0.1
**Depends on:** CHARTER-001, SPEC-001, SPEC-002, SPEC-003, SPEC-004, SPEC-005
**Primary ADR:** ADR-0025

## Purpose

SPEC-006 defines:

* durable workflows;
* planning;
* named engineering roles;
* checkpoints;
* approvals;
* cancellation;
* retries;
* compensation;
* progress;
* and final outcomes.

## Core Rule

```text
Agents are workflows, not authorities.
```

## Gate A Status

Not implemented during the first twelve weeks.

Workflow execution begins only after independent capabilities are proven.

---

# 6.8 SPEC-007

**Canonical filename:** `SPEC-007.md`
**Title:** Plugin SDK
**Subtitle:** Opure Platform Extensibility, Capability Contracts and Safe Plugin Execution
**Status:** Founder Draft
**Version:** 0.1
**Depends on:** CHARTER-001, SPEC-001, SPEC-002, SPEC-003, SPEC-004, SPEC-005, SPEC-006
**Primary ADRs:** ADR-0016, ADR-0017

## Purpose

SPEC-007 defines:

* plugin identity;
* packaging;
* manifests;
* compatibility;
* permissions;
* capabilities;
* lifecycle;
* isolation;
* workflow contributions;
* UI contributions;
* storage;
* secrets;
* network;
* project access;
* diagnostics;
* signing;
* installation;
* update;
* disablement;
* and removal.

## Core Rule

```text
A plugin may extend Opure,
but it may not bypass Opure.
```

## Gate A Status

Plugins remain disabled.

No plugin package or host is required for Founder Gate A.

---

# 6.9 SPEC-008

**Canonical filename:** `SPEC-008.md`
**Title:** Trust Centre and Security
**Subtitle:** Opure Platform Security, Permissions, Approvals and Verifiable Trust
**Status:** Founder Draft
**Version:** 0.1
**Depends on:** CHARTER-001, SPEC-001 through SPEC-007
**Primary ADRs:** ADR-0006, ADR-0007, ADR-0017, ADR-0018, ADR-0019, ADR-0026, ADR-0027, ADR-0028

## Purpose

SPEC-008 defines how Opure:

* protects developer authority;
* evaluates permissions;
* records approvals;
* classifies data;
* protects secrets;
* mediates network access;
* prevents unauthorised sharing;
* redacts sensitive data;
* records significant actions;
* quarantines unsafe components;
* supports incident investigation;
* retains and deletes security records;
* and recovers.

## Core Rule

```text
Trust must be inspectable,
permissions must be explicit,
and secrets must never become ordinary data.
```

## Gate A Importance

SPEC-008 governs:

* named-pipe authentication;
* path denials;
* configuration policy;
* Trust Evidence;
* redaction;
* recovery evidence;
* completeness;
* and adversarial testing.

---

# 6.10 SPEC-009

**Canonical filename:** `SPEC-009.md`
**Title:** Workspace and File Patch Engine
**Subtitle:** Opure Platform Safe Workspace Access, Reviewable Change and Transactional File Operations
**Status:** Founder Draft
**Version:** 0.1
**Depends on:** CHARTER-001, SPEC-001 through SPEC-008
**Primary ADRs:** ADR-0009, ADR-0021, ADR-0022, ADR-0025

## Purpose

SPEC-009 defines:

* workspace identity;
* path safety;
* reads;
* file observation;
* reparse handling;
* classification;
* snapshots;
* patch creation;
* patch review;
* conflict detection;
* transactional writes;
* reversal;
* and recovery.

## Core Rule

```text
Opure proposes changes through reviewable patches;
the developer's workspace remains authoritative.
```

## Gate A Scope

Founder Gate A implements:

* safe root acquisition;
* file inventory;
* file hashing;
* immutable Workspace generations;
* change reconciliation;
* and Trust Evidence.

Patch application is deferred until after Gate A.

---

# 6.11 SPEC-010

**Canonical filename:** `SPEC-010.md`
**Title:** Desktop User Interface
**Subtitle:** Opure Platform Desktop Experience, Review Surfaces and Developer Control
**Status:** Founder Draft
**Version:** 0.1
**Depends on:** CHARTER-001, SPEC-001 through SPEC-009
**Primary ADR:** ADR-0002

## Purpose

SPEC-010 defines:

* shell and navigation;
* projects;
* workflow views;
* patch review;
* approvals;
* Trust Centre;
* model and provider controls;
* memory;
* build and test views;
* plugin and MCP management;
* settings;
* notifications;
* and recovery.

## Core Rule

```text
The interface must show what Opure is doing,
why it is doing it,
what it will affect,
and how the developer can stop or reverse it.
```

## Gate A Scope

The first Desktop scope includes:

* Runtime health;
* Project list;
* Open Project;
* Workspace summary;
* Configuration;
* Trust Centre;
* and local Recovery Point.

---

# 6.12 SPEC-011

**Canonical filename:** `SPEC-011.md`
**Title:** Project and Build Management
**Subtitle:** Opure Platform Project Lifecycle, Build, Test, Repository and Dependency Operations
**Status:** Founder Draft
**Version:** 0.1
**Depends on:** CHARTER-001, SPEC-001 through SPEC-010
**Primary ADRs:** ADR-0009, ADR-0018, ADR-0025

## Purpose

SPEC-011 defines:

* project creation and import;
* stable identity;
* language and framework detection;
* build discovery;
* test discovery;
* package and dependency operations;
* repository operations;
* environments;
* artefacts;
* commit preparation;
* and release preparation.

## Core Rule

```text
Project automation must remain explicit,
inspectable and subordinate to the developer's intent.
```

## Gate A Scope

Founder Gate A includes:

* project registration;
* project open;
* repository observation;
* project list;
* and Workspace generation.

Build and test execution is deferred until controlled tool mediation exists.

---

# 6.13 SPEC-012

**Canonical filename:** `SPEC-012.md`
**Title:** Roadmap
**Subtitle:** Opure Platform Delivery Plan, Milestones and Evidence Gates
**Status:** Founder Draft
**Version:** 0.1
**Depends on:** CHARTER-001, SPEC-001 through SPEC-011
**Primary related artefacts:** ROADMAP-001 and BACKLOG-001

## Purpose

SPEC-012 defines:

* implementation phases;
* milestones;
* dependencies;
* evidence gates;
* prototypes;
* testing expectations;
* release criteria;
* risk controls;
* technical-debt rules;
* and deferred boundaries.

## Core Rule

```text
Opure must earn complexity through evidence.
```

## Evidence Gates

```text
G0 — Specification
G1 — Architecture
G2 — Prototype
G3 — Reliability
G4 — Security
G5 — Usability
G6 — Performance
G7 — Release
```

---

# 6.14 ROADMAP-001

**Canonical filename:** `ROADMAP-001-foundation-implementation-sequence.md`
**Title:** Foundation Implementation Sequence
**Subtitle:** Opure Implementation Programme
**Status:** Proposed
**Version:** 0.1
**Depends on:** CHARTER-001, SPEC-001 through SPEC-012, ADR-0001 through ADR-0028

## Purpose

ROADMAP-001 converts the architecture into a dependency-ordered implementation programme.

It defines:

* phases;
* milestones;
* work packages;
* first vertical slice;
* twelve-week foundation plan;
* founder gates;
* quality gates;
* implementation order;
* and Version 1 boundaries.

## Governing Principle

```text
Build the trustworthy control plane
before building AI capability on top of it.
```

---

# 6.15 BACKLOG-001

**Canonical filename:** `BACKLOG-001-foundation-first-12-weeks.md`
**Title:** Foundation First 12 Weeks
**Subtitle:** Repository-Ready Engineering Backlog
**Status:** Proposed
**Version:** 0.1
**Depends on:** ROADMAP-001 and the full governing specification set

## Purpose

BACKLOG-001 converts the first twelve weeks into:

* epics;
* sixty foundation tickets;
* weekly outcomes;
* dependencies;
* acceptance criteria;
* tests;
* Trust Evidence;
* recovery requirements;
* performance tasks;
* accessibility tasks;
* and Founder Gate A review tasks.

## Active Implementation Boundary

During BACKLOG-001:

* no AI runtime;
* no remote provider;
* no plugin;
* no MCP;
* no workflow engine;
* no patch application;
* no arbitrary shell;
* and no cloud backup

should enter the critical path.

---

# 7. Status Model

Use the following document statuses.

## 7.1 Founder Draft

The founder has authored or commissioned the document.

It is not yet formally approved for implementation as a fixed requirement.

---

## 7.2 Proposed

The document is ready for review and may guide prototypes.

Implementation evidence is still required.

---

## 7.3 Under Review

Named reviewers are assessing the document.

Unresolved review issues should be linked.

---

## 7.4 Approved

The product owner has approved the normative scope.

Approval does not automatically accept every implementation choice.

---

## 7.5 Accepted

Used primarily for ADRs after evidence.

For specifications, `Approved` is generally clearer.

---

## 7.6 Implementing

The specification or roadmap is actively being implemented.

This status does not mean all requirements are satisfied.

---

## 7.7 Partially Implemented

Some required capabilities exist.

The implemented scope and omissions must be listed.

---

## 7.8 Implemented

All selected scope is implemented and verified.

Deferred items remain clearly identified.

---

## 7.9 Superseded

A later document replaces this document.

The replacement must be named.

---

## 7.10 Rejected

The document or proposal was considered and not selected.

Reasons should remain available.

---

## 7.11 Archived

The document is historical and no longer active.

Archived documents must not be silently deleted when they explain past decisions.

---

# 8. Version Model

Specifications use document versions.

Initial form:

```text
0.1
```

Recommended progression:

```text
0.1 — initial founder draft
0.2 — substantial review revision
0.3 — prototype-informed revision
0.9 — approval candidate
1.0 — approved baseline
1.1 — backward-compatible clarification or addition
2.0 — materially changed normative scope
```

A minor wording correction that does not change meaning may update the change history without forcing a major version.

A change that alters:

* authority;
* safety;
* privacy;
* data ownership;
* service ownership;
* permissions;
* or developer control

should be treated as a substantive version change.

---

# 9. Date Format

Use:

```text
18 July 2026
```

inside human-readable document metadata.

Use ISO 8601 where machine-readable dates are required:

```text
2026-07-18
```

Do not use ambiguous numeric dates.

---

# 10. Language

Use British English.

Preferred forms include:

* authorised;
* behaviour;
* catalogue;
* centre;
* colour;
* customised;
* organisation;
* prioritise;
* programme;
* and synchronisation.

Use technical identifiers exactly as required by their technologies.

---

# 11. Normative Language

SPEC-001 defines the normative language.

Use:

* **MUST**
* **MUST NOT**
* **SHOULD**
* **SHOULD NOT**
* **MAY**
* **DEFERRED**

Avoid vague requirements when a decision is intended.

When uncertainty is real, state:

* what is provisional;
* what evidence is required;
* and what decision gate applies.

---

# 12. Document Naming

## 12.1 Charter

```text
CHARTER-001.md
```

---

## 12.2 Specifications

```text
SPEC-001.md
SPEC-002.md
...
SPEC-012.md
```

---

## 12.3 Roadmaps

```text
ROADMAP-001-foundation-implementation-sequence.md
ROADMAP-002-<scope>.md
```

---

## 12.4 Backlogs

```text
BACKLOG-001-foundation-first-12-weeks.md
BACKLOG-002-controlled-mutation.md
```

---

## 12.5 Supporting Guides

Examples:

```text
README.md
GLOSSARY.md
CONTRIBUTING-SPECS.md
```

Do not create unnumbered competing architecture specifications.

---

## 12.6 Duplicate Download Names

Do not commit:

```text
SPEC-001(1).md
SPEC-001(2).md
SPEC-001 - Copy.md
SPEC-001-final.md
SPEC-001-final-final.md
```

Use Git history and document versions instead.

---

# 13. Required Metadata

Every numbered specification should include:

```text
Document
Status
Version
Language
Last updated
Depends on
```

Recommended additional fields:

```text
Owner
Reviewers
Approved by
Approval date
Supersedes
Superseded by
Related ADRs
Related roadmap
```

---

# 14. Required Sections

A platform specification should normally include:

1. Purpose
2. Founding Rule
3. Relationship to Other Services
4. Design Goals
5. Non-Goals
6. Normative Language
7. Responsibilities
8. Service or Component Boundaries
9. Data and State
10. Security and Privacy
11. Failure and Recovery
12. Observability and Trust Evidence
13. Performance
14. Accessibility where user facing
15. Testing
16. Release Gates
17. Acceptance Criteria
18. Open Questions
19. Deferred Decisions
20. Alternatives Rejected
21. Review Record
22. Approval
23. Supersession
24. Change History
25. Final Decision or Requirement Statement

Not every document needs every heading.

Omissions should be deliberate.

---

# 15. Founding Rule Format

Each domain specification should contain one short founding rule.

The founding rule should be:

* memorable;
* testable;
* consistent with the Charter;
* and strong enough to reject unsafe shortcuts.

---

# 16. Non-Goals

Every specification should state what the component does not own.

Non-goals prevent:

* service sprawl;
* hidden coupling;
* duplicated authority;
* accidental platform identity changes;
* and implementation convenience from swallowing domain boundaries.

---

# 17. Service Ownership

Every domain capability should identify:

* owner service;
* durable state owner;
* contract owner;
* process placement;
* dependencies;
* Trust Evidence owner;
* backup adapter owner;
* and recovery validator owner.

No specification should assign one durable state to several writers.

---

# 18. Process Boundaries

Specifications describe logical boundaries.

ADRs decide process topology.

A service may begin:

* in the Runtime process;
* in a supervised worker;
* or in a dedicated host

depending on:

* trust;
* crash isolation;
* resource isolation;
* native runtime;
* lifecycle;
* and performance.

---

# 19. Data Ownership

Every persistent record should have:

* one owner;
* one schema;
* one migration path;
* one backup classification;
* one deletion path;
* and one Trust Evidence policy.

Derived projections should not be confused with owner authority.

---

# 20. Trust Evidence

Specifications should identify significant events that require Trust Evidence.

A Trust Evidence requirement should state:

* owner;
* Authority Class;
* action;
* outcome;
* subject;
* project or operation scope;
* payload classification;
* retention;
* and completeness expectations.

Operational logs are not substitutes for Trust Evidence.

---

# 21. Secrets

Specifications must not treat secrets as ordinary data.

Secret values must not enter:

* project settings;
* ordinary service databases;
* logs;
* metrics;
* traces;
* Trust Evidence;
* Context Plans;
* Project Memory;
* indexes;
* support bundles;
* backups;
* or portability exports

unless the relevant approved secret-specific design explicitly allows a protected form.

---

# 22. Filesystem Safety

Any specification that touches files should account for:

* path normalisation;
* handle identity;
* containment;
* reparse points;
* junctions;
* symlinks;
* alternate data streams;
* file replacement races;
* case behaviour;
* Unicode behaviour;
* network paths;
* removable media;
* and recovery after interruption.

Do not specify path-string prefix checks as a security boundary.

---

# 23. Configuration and Policy

Specifications should distinguish:

* Product Defaults;
* User Profile;
* Project Settings;
* Enterprise Policy;
* Product Policy;
* requested value;
* effective value;
* and source provenance.

Project files must not grant:

* capabilities;
* permissions;
* secret access;
* plugin trust;
* MCP trust;
* provider trust;
* or external side effects.

---

# 24. AI Behaviour

Specifications involving AI must preserve:

* provider neutrality;
* model neutrality;
* visible context;
* visible source provenance;
* developer approval;
* deterministic policy;
* no direct project write;
* no permission authority;
* no secret authority;
* and no claim that model output is fact.

AI may propose.

Deterministic services authorise and execute.

---

# 25. Workflow Behaviour

Workflow specifications must preserve:

* compiled plans;
* exact inputs;
* checkpointing;
* approvals;
* cancellation;
* idempotency;
* effect intents;
* receipts;
* Outcome Unknown;
* reconciliation;
* compensation;
* and Recovery Required after ambiguous restore.

Agents remain named workflow roles.

They are not independent permanent authorities.

---

# 26. Plugin Behaviour

Plugin specifications must preserve:

* package identity;
* quarantine;
* signature and source review;
* capability leases;
* process isolation;
* no direct filesystem;
* no direct network;
* no direct Vault;
* no direct provider;
* no direct MCP;
* and no self-granted permissions.

---

# 27. MCP Behaviour

MCP specifications must preserve:

* exact server identity;
* mediated transport;
* tool schema validation;
* bounded permissions;
* no automatic roots;
* no automatic sampling;
* no automatic tasks;
* current trust;
* and safe credential handling.

---

# 28. Network Behaviour

Any network-capable specification must state:

* destination;
* operator;
* purpose;
* data classes;
* credentials;
* region;
* retention posture;
* approval;
* retry;
* timeout;
* and receipt.

No network use should be hidden behind a generic feature label.

---

# 29. Recovery Behaviour

Every durable capability specification should define:

* process crash;
* service restart;
* transaction failure;
* schema migration;
* backup classification;
* restore validation;
* deletion;
* erasure;
* and degraded operation.

A capability is not complete when recovery is undefined.

---

# 30. Accessibility

User-facing specifications should require:

* keyboard operation;
* Narrator support;
* visible focus;
* high contrast;
* no colour-only meaning;
* actionable error text;
* accessible progress;
* accessible cancellation;
* and alternatives to purely visual graphs.

---

# 31. Performance

Specifications should define measurable:

* latency;
* throughput;
* memory;
* disk;
* concurrency;
* cancellation;
* startup;
* and degraded-mode targets

where the capability is performance sensitive.

Performance targets are provisional until measured.

---

# 32. Testing

Every specification should identify:

* unit tests;
* contract tests;
* schema tests;
* integration tests;
* crash tests;
* security tests;
* adversarial tests;
* accessibility tests;
* performance tests;
* and recovery tests

as applicable.

Tests are implementation deliverables.

They are not optional follow-up work.

---

# 33. Evidence

Implementation evidence should be stored under:

```text
C:\Opure\eng\evidence\
```

Recommended structure:

```text
eng\
└── evidence\
    ├── milestones\
    ├── architecture\
    ├── security\
    ├── privacy\
    ├── performance\
    ├── recovery\
    ├── accessibility\
    ├── dependencies\
    ├── signing\
    └── releases\
```

Do not commit:

* secrets;
* personal data;
* project source;
* recovery keys;
* Vault Capsules;
* raw diagnostic dumps;
* or support bundles containing sensitive content.

Commit safe summaries, hashes and reports.

---

# 34. ADR Relationship

Specifications state required product and system behaviour.

ADRs record concrete decisions such as:

* language;
* framework;
* process topology;
* IPC;
* persistence;
* logging;
* secrets;
* testing;
* filesystem handling;
* repository structure;
* CI;
* versioning;
* packaging;
* signing;
* updating;
* plugin packaging;
* plugin permissions;
* MCP trust;
* provider trust;
* local model runtime;
* context;
* knowledge;
* memory;
* routing;
* workflows;
* configuration;
* Trust Evidence;
* and backup.

An ADR should not quietly redefine the product identity.

A specification should not pretend an implementation choice is permanent when the architecture intentionally keeps it replaceable.

---

# 35. Foundational ADR Catalogue

The foundational ADR sequence currently includes:

```text
ADR-0001 — Primary Implementation Language
ADR-0002 — Desktop Framework
ADR-0003 — Runtime Process Topology
ADR-0004 — Local IPC
ADR-0005 — Persistence
ADR-0006 — Logging and Observability
ADR-0007 — Secrets Vault
ADR-0008 — Testing Strategy
ADR-0009 — Windows Path and Filesystem Handling
ADR-0010 — Repository and Solution Structure
ADR-0011 — Build and Continuous Integration
ADR-0012 — Versioning and Release Management
ADR-0013 — Packaging and Installer
ADR-0014 — Code Signing and Release Trust
ADR-0015 — Updater and Update Policy
ADR-0016 — Plugin Packaging and Distribution
ADR-0017 — Plugin Permissions and Capability Model
ADR-0018 — MCP Server Trust and Permission Model
ADR-0019 — AI Provider Trust and Data Sharing
ADR-0020 — Local Model Runtime and Model Management
ADR-0021 — Context Assembly and Token Budgeting
ADR-0022 — Project Knowledge Indexing and Retrieval
ADR-0023 — Project Memory and Provenance
ADR-0024 — AI Evaluation, Qualification and Routing
ADR-0025 — Workflow Execution, Checkpointing and Recovery
ADR-0026 — Configuration and Policy
ADR-0027 — Trust Evidence, Support Bundles and Operational Records
ADR-0028 — Backup, Restore, Data Portability and Disaster Recovery
```

Use the exact canonical ADR filenames in the `adr` folder.

---

# 36. Specification-to-ADR Traceability

| Specification | Principal ADRs                                                                 |
| ------------- | ------------------------------------------------------------------------------ |
| CHARTER-001   | All ADRs must comply                                                           |
| SPEC-001      | All ADRs must comply                                                           |
| SPEC-002      | ADR-0003, ADR-0004, ADR-0005, ADR-0006, ADR-0007                               |
| SPEC-003      | ADR-0003, ADR-0004, ADR-0005, ADR-0010                                         |
| SPEC-004      | ADR-0019, ADR-0020, ADR-0021, ADR-0024                                         |
| SPEC-005      | ADR-0022, ADR-0023                                                             |
| SPEC-006      | ADR-0025                                                                       |
| SPEC-007      | ADR-0016, ADR-0017                                                             |
| SPEC-008      | ADR-0006, ADR-0007, ADR-0017, ADR-0018, ADR-0019, ADR-0026, ADR-0027, ADR-0028 |
| SPEC-009      | ADR-0009, ADR-0021, ADR-0022, ADR-0025                                         |
| SPEC-010      | ADR-0002, ADR-0003, ADR-0004, ADR-0026, ADR-0027                               |
| SPEC-011      | ADR-0009, ADR-0018, ADR-0025                                                   |
| SPEC-012      | ROADMAP-001 and BACKLOG-001                                                    |
| ROADMAP-001   | ADR-0001 through ADR-0028                                                      |
| BACKLOG-001   | Gate A subset of the foundational ADR sequence                                 |

This table identifies principal relationships.

It is not an exclusive list.

---

# 37. Foundation Implementation Status

The active implementation programme begins with:

```text
M0 — Repository Builds
M1 — Runtime Lives
M2 — Services Communicate
M3 — State Persists
M4 — Project Opens Safely
M5 — Configuration Is Explainable
M6 — Foundation Is Visible
Founder Gate A
```

The first vertical slice is:

```text
Desktop
    ↓
Runtime supervision
    ↓
authenticated named-pipe IPC
    ↓
Project Service
    ↓
Workspace Snapshot
    ↓
Effective Configuration
    ↓
Trust Evidence
    ↓
Trust Centre
    ↓
local Recovery Point
```

---

# 38. Founder Gates

The current roadmap defines:

```text
Founder Gate A — Foundation Proof
Founder Gate B — Controlled Mutation
Founder Gate C — Local Intelligence
Founder Gate D — External Trust Boundaries
Founder Gate E — Workflow Control
```

A founder gate records:

* reviewed build;
* demonstration;
* evidence passed;
* evidence failed;
* accepted limitations;
* required amendments;
* ADR decisions;
* and permission to continue.

---

# 39. Gate A Exclusions

Before Founder Gate A, the active implementation should not include:

* AI inference;
* remote providers;
* provider credentials;
* plugins;
* Plugin Host;
* MCP servers;
* MCP Gateway;
* workflows;
* agents;
* patch application;
* project writes;
* arbitrary shell;
* dependency installation;
* Git write operations;
* support upload;
* telemetry export;
* cloud backup;
* or automatic external side effects.

These boundaries protect the critical path.

---

# 40. Adding a New Specification

A new specification requires:

1. a clear domain gap;
2. an owner;
3. a unique identifier;
4. a canonical filename;
5. dependencies;
6. a founding rule;
7. responsibilities;
8. non-goals;
9. service ownership;
10. state ownership;
11. security and privacy;
12. failure and recovery;
13. Trust Evidence;
14. tests;
15. acceptance criteria;
16. open questions;
17. deferred decisions;
18. review record;
19. approval;
20. and README catalogue entry.

Do not create a new specification merely because a feature is large.

Create one when the capability needs a stable normative boundary.

---

# 41. Allocating a Specification Number

Use the next unused number.

Example:

```text
SPEC-013.md
```

Before allocating:

* search the repository;
* check Git history;
* check superseded documents;
* and update this README.

Do not reuse a retired number.

---

# 42. Amending a Specification

An amendment should:

1. identify the requirement being changed;
2. explain why;
3. identify affected services;
4. identify affected ADRs;
5. identify data migration;
6. identify security and privacy impact;
7. identify recovery impact;
8. identify roadmap and backlog impact;
9. update acceptance criteria;
10. update change history;
11. update version;
12. and record approval.

Small editorial fixes should still be clear in Git history.

---

# 43. Superseding a Specification

A superseding document must:

* name the old document;
* explain why replacement is required;
* map old requirements to new requirements;
* identify removed requirements;
* identify migration;
* identify ADR impact;
* identify roadmap impact;
* and update the `Superseded by` field.

Do not delete the superseded document.

---

# 44. Splitting a Specification

Split a specification when:

* it has several independent owners;
* it has several incompatible lifecycles;
* review is blocked by excessive scope;
* separate security boundaries exist;
* or independent roadmaps require stable documents.

Do not split solely by implementation project.

---

# 45. Merging Specifications

Merge only when:

* authority is truly one domain;
* separation creates duplicated requirements;
* owners agree;
* traceability is preserved;
* and supersession is explicit.

Do not merge Runtime, Trust, Workspace, Desktop or Project domains merely to reduce file count.

---

# 46. Open Questions

Open Questions are not hidden defects.

They should state:

* the unresolved issue;
* why it matters;
* what evidence is required;
* who owns the answer;
* and the latest decision point.

An Open Question should become:

* an ADR;
* a spike;
* a backlog ticket;
* a deferred decision;
* or a resolved answer.

---

# 47. Deferred Decisions

A deferred decision should state:

* what is deferred;
* why it is not required now;
* what evidence would justify it;
* and which milestone may reconsider it.

Deferred does not mean forgotten.

---

# 48. Alternatives Rejected

Keep rejected alternatives when they explain:

* why the selected design exists;
* which risk was avoided;
* which evidence changed the decision;
* or why a fashionable approach was not appropriate.

Examples include:

* cloud-first architecture;
* autonomous agents;
* raw file-copy backup;
* shared service databases;
* arbitrary shell;
* logs as audit authority;
* or plugin code inside Desktop.

---

# 49. Approval

A specification approval should include:

```text
Name
Role
Decision
Date
Notes
```

Founder approval is required when the specification materially changes:

* product identity;
* developer authority;
* privacy;
* local-first operation;
* scope;
* or release commitment.

Specialist review is required where relevant:

* security;
* privacy;
* recovery;
* accessibility;
* cryptography;
* persistence;
* filesystem;
* packaging;
* or enterprise policy.

---

# 50. Review Records

Review records should be concise and factual.

Example:

| Date         | Reviewer           | Decision | Notes                                |
| ------------ | ------------------ | -------- | ------------------------------------ |
| 18 July 2026 | Architecture draft | Proposed | Initial service boundary recommended |
| Pending      | Founder            | Pending  | Product approval required            |
| Pending      | Security           | Pending  | Threat model and tests required      |

---

# 51. Change History

Each document should preserve a change history.

Example:

| Version | Date         | Author        | Summary                     |
| ------- | ------------ | ------------- | --------------------------- |
| 0.1     | 18 July 2026 | Founder Draft | Initial specification       |
| 0.2     | Pending      | Pending       | Prototype-informed revision |

Do not use the change history as a substitute for Git history.

Use it to explain meaningful document evolution.

---

# 52. Links

Use relative repository links where practical.

Example:

```markdown
[CHARTER-001](./CHARTER-001.md)
```

Example ADR link:

```markdown
[ADR-0004](../adr/ADR-0004-local-ipc.md)
```

Avoid machine-specific absolute links inside committed Markdown.

The path `C:\Opure` may appear as the canonical local repository root in explanatory text.

---

# 53. Document IDs

Document IDs are stable.

Use:

```text
CHARTER-001
SPEC-001
ROADMAP-001
BACKLOG-001
ADR-0001
```

Do not refer to a document only by its title when precision matters.

---

# 54. Heading Style

Use:

```markdown
# Document title
## Subtitle
# 1. Major section
## 1.1 Subsection
```

Existing documents use detailed numbered sections.

New documents should remain consistent unless a shorter guide genuinely benefits from lighter structure.

---

# 55. Tables

Use tables for:

* catalogues;
* ownership;
* status;
* dependencies;
* traceability;
* milestones;
* and approval records.

Avoid very wide tables containing full requirement prose.

Use headings and lists for detailed normative text.

---

# 56. Diagrams

Use text diagrams when they improve understanding.

Example:

```text
Desktop
    ↓
Runtime
    ↓
Service
    ↓
Persistence
```

A diagram should not replace explicit ownership prose.

---

# 57. Code Blocks

Use code blocks for:

* paths;
* identifiers;
* command examples;
* schemas;
* directory structures;
* and state-machine sketches.

Do not assign arbitrary metadata IDs to Markdown code fences in committed files.

---

# 58. Repository Paths

Use Windows paths where the implementation target is Windows-specific:

```text
C:\Opure
%LOCALAPPDATA%\Opure\<Channel>\
```

Use repository-relative paths for source structure:

```text
src/Runtime/Opure.Runtime/
```

Use URI or POSIX-style path syntax only where the referenced technology requires it.

---

# 59. Privacy in Specifications

Specifications may contain conceptual examples.

They should not contain:

* real credentials;
* real private project paths;
* personal access tokens;
* real customer information;
* private source code;
* production incident details;
* or private recovery material.

Use opaque examples.

---

# 60. Security Claims

Avoid absolute claims such as:

* secure;
* tamper-proof;
* impossible to bypass;
* ransomware-proof;
* anonymous;
* private by default

without a precise scope and evidence.

Prefer:

* deny by default;
* protected by;
* verified within;
* detected by;
* bounded by;
* or not claimed.

---

# 61. Legal and Compliance Claims

Specifications may record design intent.

They should not claim legal compliance without current review.

Examples requiring specialist review include:

* UK GDPR;
* data portability;
* erasure;
* records retention;
* export controls;
* licence compliance;
* accessibility law;
* and cryptographic regulation.

---

# 62. External Standards and Documentation

When a specification relies on external technical facts:

* use primary sources;
* record the retrieval date where change risk matters;
* avoid long copied passages;
* identify provisional assumptions;
* and revalidate before acceptance.

Official references may appear in the specification's references section.

---

# 63. Specification Quality Checklist

Before review:

* [ ] Document ID is correct.
* [ ] Canonical filename is correct.
* [ ] Title is clear.
* [ ] Status is correct.
* [ ] Version is correct.
* [ ] Date is exact.
* [ ] Dependencies are complete.
* [ ] Purpose is clear.
* [ ] Founding rule is clear.
* [ ] Responsibilities are owned.
* [ ] Non-goals are explicit.
* [ ] State ownership is explicit.
* [ ] Security is addressed.
* [ ] Privacy is addressed.
* [ ] Failure is addressed.
* [ ] Recovery is addressed.
* [ ] Trust Evidence is addressed.
* [ ] Performance is addressed.
* [ ] Accessibility is addressed where relevant.
* [ ] Testing is addressed.
* [ ] Acceptance criteria exist.
* [ ] Open questions are explicit.
* [ ] Deferred decisions are explicit.
* [ ] Alternatives are represented fairly.
* [ ] Review record exists.
* [ ] Approval exists.
* [ ] Supersession exists.
* [ ] Change history exists.
* [ ] README catalogue is updated.

---

# 64. Roadmap Quality Checklist

Before review:

* [ ] Outcomes are user visible.
* [ ] Dependencies are explicit.
* [ ] Milestones are evidence gated.
* [ ] Security is part of delivery.
* [ ] Recovery is part of delivery.
* [ ] Accessibility is part of delivery.
* [ ] Performance is measured.
* [ ] Founder gates are explicit.
* [ ] Deferred capability is protected from scope creep.
* [ ] ADR evidence is planned.
* [ ] First vertical slice is complete end to end.
* [ ] Calendar estimates are presented honestly.

---

# 65. Backlog Quality Checklist

Before activation:

* [ ] Tickets have stable IDs.
* [ ] Tickets have outcomes.
* [ ] Tickets have owners.
* [ ] Tickets have dependencies.
* [ ] Tickets link specifications.
* [ ] Tickets link ADRs.
* [ ] Tickets identify state ownership.
* [ ] Tickets identify Trust Evidence.
* [ ] Tickets identify recovery.
* [ ] Tickets include tests.
* [ ] Tickets include acceptance criteria.
* [ ] Tickets include evidence outputs.
* [ ] Tickets fit within a milestone.
* [ ] Oversized tickets are split or time boxed.
* [ ] Founder review work is included.
* [ ] No prohibited scope entered the active milestone.

---

# 66. Current Active Documents

| ID          | Filename                                            | Status        | Active role               |
| ----------- | --------------------------------------------------- | ------------- | ------------------------- |
| CHARTER-001 | `CHARTER-001.md`                                    | Founder Draft | Founding authority        |
| SPEC-001    | `SPEC-001.md`                                       | Founder Draft | Product direction         |
| SPEC-002    | `SPEC-002.md`                                       | Founder Draft | Runtime                   |
| SPEC-003    | `SPEC-003.md`                                       | Founder Draft | Services                  |
| SPEC-004    | `SPEC-004.md`                                       | Founder Draft | AI routing                |
| SPEC-005    | `SPEC-005.md`                                       | Founder Draft | Memory and knowledge      |
| SPEC-006    | `SPEC-006.md`                                       | Founder Draft | Workflows and agents      |
| SPEC-007    | `SPEC-007.md`                                       | Founder Draft | Plugins                   |
| SPEC-008    | `SPEC-008.md`                                       | Founder Draft | Trust and security        |
| SPEC-009    | `SPEC-009.md`                                       | Founder Draft | Workspace and patches     |
| SPEC-010    | `SPEC-010.md`                                       | Founder Draft | Desktop                   |
| SPEC-011    | `SPEC-011.md`                                       | Founder Draft | Projects and builds       |
| SPEC-012    | `SPEC-012.md`                                       | Founder Draft | Delivery roadmap          |
| ROADMAP-001 | `ROADMAP-001-foundation-implementation-sequence.md` | Proposed      | Detailed programme        |
| BACKLOG-001 | `BACKLOG-001-foundation-first-12-weeks.md`          | Proposed      | Initial execution backlog |

---

# 67. Current Approval Position

The current documents are founder drafts or proposed implementation documents.

The recommended approval sequence is:

1. approve CHARTER-001;
2. approve SPEC-001;
3. review and approve SPEC-002 through SPEC-011 by domain;
4. approve SPEC-012;
5. retain ROADMAP-001 as Proposed until Gate A evidence;
6. activate BACKLOG-001;
7. update relevant ADRs with implementation evidence;
8. record Founder Gate A;
9. approve or amend the next backlog.

Implementation may begin under the proposed roadmap because the documents are sufficiently detailed.

Provisional choices remain replaceable.

---

# 68. Current Canonical Implementation Entry Point

The current implementation entry point is:

```text
BACKLOG-001
    ↓
FND-001 — Create Solution Baseline
```

The initial active milestone is:

```text
M0 — Repository Builds
```

The first ready ticket is:

```text
FND-001 — Create Solution Baseline
```

---

# 69. First Repository Validation

Before implementation begins, confirm:

```powershell
Set-Location C:\Opure

Get-ChildItem .\specs -File |
    Sort-Object Name |
    Select-Object Name
```

Check for duplicate suffixes:

```powershell
Get-ChildItem .\specs -File |
    Where-Object Name -Match '\(\d+\)| - Copy|final'
```

Check expected documents:

```powershell
$expected = @(
    'README.md',
    'CHARTER-001.md',
    'SPEC-001.md',
    'SPEC-002.md',
    'SPEC-003.md',
    'SPEC-004.md',
    'SPEC-005.md',
    'SPEC-006.md',
    'SPEC-007.md',
    'SPEC-008.md',
    'SPEC-009.md',
    'SPEC-010.md',
    'SPEC-011.md',
    'SPEC-012.md',
    'ROADMAP-001-foundation-implementation-sequence.md',
    'BACKLOG-001-foundation-first-12-weeks.md'
)

$actual = Get-ChildItem .\specs -File |
    Select-Object -ExpandProperty Name

Compare-Object $expected $actual
```

A blank comparison means the expected set matches.

---

# 70. Duplicate Content Check

Filename checks do not detect renamed duplicate content.

A simple hash inventory can help:

```powershell
Get-ChildItem .\specs -File -Filter *.md |
    Get-FileHash -Algorithm SHA256 |
    Sort-Object Hash, Path |
    Format-Table Hash, Path -AutoSize
```

Files with identical hashes should be reviewed.

Do not delete merely because content is similar.

ROADMAP-001 and SPEC-012 intentionally overlap in topic but serve different levels.

---

# 71. Markdown Validation

Recommended checks:

* valid UTF-8;
* no accidental binary content;
* no duplicate canonical filename;
* no broken relative links;
* no raw secret canaries;
* no trailing merge-conflict markers;
* and consistent heading order.

Search conflict markers:

```powershell
Get-ChildItem .\specs -File -Filter *.md |
    Select-String -Pattern '^(<<<<<<<|=======|>>>>>>>)'
```

---

# 72. Git Workflow for Specification Changes

Recommended:

```powershell
git switch -c docs/spec-<scope>
```

Edit the document.

Review:

```powershell
git diff -- specs/
```

Validate:

```powershell
.\build.ps1 verify-docs
```

Commit:

```powershell
git add specs/
git commit -m "Update <document> <scope>"
git status
```

The `verify-docs` command is a recommended future build target.

---

# 73. Recommended Documentation Build Checks

Add later:

```text
verify-docs
    ├── canonical filename check
    ├── duplicate filename check
    ├── UTF-8 check
    ├── metadata check
    ├── internal link check
    ├── document ID check
    ├── dependency check
    ├── duplicate hash report
    ├── conflict marker check
    ├── prohibited-secret pattern check
    └── catalogue synchronisation check
```

These checks should not replace human architecture review.

---

# 74. Catalogue Synchronisation

A future documentation verifier should compare:

* files in `specs`;
* entries in this README;
* internal document IDs;
* dependency declarations;
* and active status.

A missing README entry should fail documentation verification for numbered documents.

---

# 75. Glossary

## AI Router

The provider-neutral service that selects and invokes approved AI routes.

---

## Authority

The recognised right of a service, policy or human decision to establish a specific kind of state.

---

## Backup Adapter

An owner-service contract that describes how its state is snapshotted, validated and restored.

---

## Capability

A bounded permission to perform a specific operation against a specific resource.

---

## Context Plan

An immutable record of the exact information supplied to a model request.

---

## Effective Configuration

The final policy-evaluated configuration used by a service or project scope.

---

## Evidence Gate

A delivery checkpoint requiring defined proof before progression.

---

## Founder Gate

A product and architecture decision point owned by Christopher Dyer.

---

## Local First

Core value and operation remain available without a required cloud control plane.

---

## MCP

Model Context Protocol.

MCP does not imply automatic trust.

---

## Owner Service

The one service responsible for authoritative state in a domain.

---

## Project Knowledge

Rebuildable project indexes and relationships derived from project evidence.

---

## Project Memory

Accepted durable project knowledge with provenance, correction and deletion.

---

## Recovery Point

A consistent local snapshot used for update, migration or local corruption recovery.

---

## Trust Centre

The developer-facing projection of authoritative receipts, policy decisions, evidence completeness, security state and recovery state.

---

## Trust Evidence

Typed, authority-labelled records of significant actions, decisions, outcomes and integrity state.

---

## Workspace Snapshot

An immutable generation describing a verified project file and repository state.

---

# 76. Document Maintenance Ownership

| Area                 | Owner                                     |
| -------------------- | ----------------------------------------- |
| Charter              | Founder                                   |
| Product principles   | Founder and Product                       |
| Runtime              | Runtime Architecture                      |
| Services             | Foundation Architecture                   |
| AI Router            | AI Platform                               |
| Knowledge and Memory | Knowledge Architecture                    |
| Workflows            | Workflow Architecture                     |
| Plugins              | Extension Architecture                    |
| Trust and Security   | Security Architecture                     |
| Workspace and Patch  | Workspace Architecture                    |
| Desktop              | Desktop and Accessibility                 |
| Projects and Builds  | Project and Build Architecture            |
| Roadmap              | Founder and Programme                     |
| Backlog              | Product and Engineering Programme         |
| README catalogue     | Foundation Architecture and Documentation |

For a founder-led project, Christopher may hold several roles.

The role names preserve future ownership clarity.

---

# 77. Review Cadence

Review this README when:

* a specification is added;
* a specification is renamed;
* a status changes;
* a document is superseded;
* a roadmap is added;
* a backlog is added;
* the ADR range changes;
* a founder gate changes;
* or the repository documentation structure changes.

Minimum review:

* before Founder Gate A;
* before each later Founder Gate;
* and before Version 1 release.

---

# 78. Near-Term Documentation Sequence

After this README, the next documentation work should be implementation-supporting rather than another broad architecture specification.

Recommended sequence:

1. Start FND-001.
2. Create repository structure.
3. Add build and documentation verification foundations.
4. Create `eng\evidence\` structure.
5. Create issue milestones and labels from BACKLOG-001.
6. Begin implementation.
7. Update ADR evidence as prototypes complete.
8. Create `BACKLOG-002-controlled-mutation.md` only after Founder Gate A.

---

# 79. Documents Not Yet Required

The following may be useful later but are not required before implementation:

```text
GLOSSARY.md
CONTRIBUTING-SPECS.md
SECURITY-REVIEW-TEMPLATE.md
EVIDENCE-README.md
SCHEMA-CATALOGUE.md
SERVICE-CATALOGUE.md
TRUST-EVIDENCE-CATALOGUE.md
CONFIGURATION-CATALOGUE.md
```

Avoid creating all of them before real implementation needs exist.

Some can be generated from schemas or source catalogues later.

---

# 80. Implementation Should Now Begin

The specification set is sufficiently complete to begin the foundation.

The next active action is not another foundational ADR.

It is:

```text
FND-001 — Create Solution Baseline
```

The first milestone is:

```text
M0 — Repository Builds
```

The first evidence objective is:

```text
A clean clone can restore, build and test.
```

---

# 81. Review Record

| Date         | Reviewer           | Decision | Notes                                                                       |
| ------------ | ------------------ | -------- | --------------------------------------------------------------------------- |
| 18 July 2026 | Architecture draft | Proposed | Canonical specifications catalogue, hierarchy and maintenance rules defined |

---

# 82. Approval

## Founder and Product Approval

* **Name:** Christopher Dyer
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Canonical hierarchy, filenames and implementation entry point require confirmation

## Architecture Approval

* **Name or role:** Foundation Architecture Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Traceability and maintenance rules require review

## Documentation Approval

* **Name or role:** Documentation Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Naming, metadata and validation rules require review

---

# 83. Supersession

This README may be replaced by a later version at the same canonical path.

Git history preserves prior versions.

A replacement should:

* retain the canonical document catalogue;
* explain hierarchy changes;
* identify added and removed documents;
* preserve supersession history;
* and record founder approval where authority changes.

---

# 84. Change History

| Version | Date         | Author        | Summary                                                        |
| ------- | ------------ | ------------- | -------------------------------------------------------------- |
| 0.1     | 18 July 2026 | Founder Draft | Initial specifications index, governance and maintenance guide |

---

# 85. Final Statement

> **The Opure specification set is one connected system of authority and evidence: CHARTER-001 defines the principles that must not be traded away, SPEC-001 converts those principles into normative product direction, SPEC-002 through SPEC-011 define the platform domains and their ownership boundaries, SPEC-012 defines evidence-gated delivery, the foundational ADR sequence selects replaceable implementation decisions, ROADMAP-001 orders the work, BACKLOG-001 defines the first executable twelve weeks, and this README keeps those artefacts discoverable, canonical and traceable so that implementation can move quickly without allowing code, convenience, duplicate files or undocumented decisions to become a hidden source of truth.**

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**