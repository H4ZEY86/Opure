# Opure

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**

Opure is a local-first software engineering platform designed to help developers understand, build, test, review, maintain and evolve software while keeping them in control of every important decision.

Opure uses artificial intelligence as one replaceable engineering capability.

It is not an AI platform that happens to write code.

It is a software engineering platform that uses AI.

---

## Status

**Current phase:** Founding architecture and specification baseline  
**Implementation status:** Pre-implementation  
**Primary target:** Windows 11  
**Cloud dependency:** None required  
**Initial local provider:** Ollama  
**Default philosophy:** Local by design, cloud optional  

The founding specification set is complete:

- `CHARTER-001.md`
- `SPEC-001.md` through `SPEC-012.md`
- `ARCHITECTURE.md`

The next phase is to approve the founding drafts, create the first Architecture Decision Records and implement the Runtime architecture skeleton.

---

## Why Opure Exists

AI-assisted development can be useful, but many products ask developers to trade away:

- privacy;
- control;
- understanding;
- provider choice;
- project ownership;
- or visibility.

Opure begins from a different belief:

> Technology should strengthen developers rather than replace them.

Opure is intended to help with the complete engineering process:

- understanding requirements;
- analysing existing projects;
- planning changes;
- designing architecture;
- generating code;
- reviewing patches;
- running builds and tests;
- diagnosing failures;
- managing dependencies;
- preparing Git changes;
- maintaining documentation;
- recording decisions;
- and learning from proven engineering outcomes.

The developer remains the final authority.

---

## Core Commitments

Opure is built around the following commitments.

### Developer Respect

The platform must respect the developer's:

- time;
- code;
- privacy;
- hardware;
- intelligence;
- workflow;
- decisions;
- and ownership.

### Local by Design

Core functionality should work locally whenever technically practical.

A cloud account, subscription or remote provider must not be required to open, understand or maintain a local project.

### Human in Control

Opure may analyse, plan, generate, test, review and recommend.

The developer remains responsible for high-impact decisions.

### Visible by Design

Important actions must remain visible and inspectable, including:

- model selection;
- context supplied;
- files accessed;
- commands executed;
- tools invoked;
- network requests;
- permissions;
- approvals;
- patches;
- builds;
- tests;
- and final outcomes.

### Reviewable Changes

Generated file changes are represented as reviewable patches.

AI models, plugins and MCP servers do not write protected project files directly.

### Replaceable Intelligence

AI providers are accessed through a provider-neutral AI Router.

The architecture must not depend permanently on Ollama or any other single provider.

### Proven Engineering

Reusable knowledge and patterns advance through evidence-based states:

- Draft;
- Compiled;
- Tested;
- Reviewed;
- Proven;
- Trusted.

Opure never labels code or patterns as bug-free.

---

## What Opure Is

Opure is intended to become a platform that combines:

- local AI;
- optional remote AI;
- project memory;
- structured context retrieval;
- engineering workflows;
- reviewable patches;
- builds and tests;
- repository operations;
- plugins;
- MCP integrations;
- policy enforcement;
- secrets protection;
- and an inspectable Trust Centre.

---

## What Opure Is Not

Opure is not:

- merely a chatbot;
- merely a code generator;
- a black-box autonomous agent;
- a hidden cloud dependency;
- a telemetry platform;
- a replacement for developer judgement;
- a system that silently uploads private projects;
- a system that silently modifies source files;
- or a platform controlled by one AI vendor.

---

## Architectural Overview

```text
┌───────────────────────────────────────────────────────────────┐
│                       Opure Desktop                           │
│                                                               │
│ Projects  Workflows  Patches  Build  Memory  Trust  Plugins  │
└──────────────────────────────┬────────────────────────────────┘
                               │
                               ▼
┌───────────────────────────────────────────────────────────────┐
│                     Desktop Gateway                           │
└──────────────────────────────┬────────────────────────────────┘
                               │
                               ▼
┌───────────────────────────────────────────────────────────────┐
│                      Runtime Kernel                           │
│                                                               │
│ Bootstrap  Registry  Lifecycle  Messaging  Scheduler  Health │
└──────────────┬────────────────────┬────────────────────┬───────┘
               │                    │                    │
               ▼                    ▼                    ▼
┌──────────────────────┐  ┌──────────────────────┐  ┌──────────────────────┐
│ Engineering Services │  │ Intelligence Services│  │ Trust and Integration│
│                      │  │                      │  │                      │
│ Project Manager      │  │ AI Router            │  │ Policy Engine        │
│ Workspace Service    │  │ Context Engine       │  │ Approval Manager     │
│ Patch Service        │  │ Knowledge Engine     │  │ Secrets Vault        │
│ Build Manager        │  │ Workflow Engine      │  │ Network Gateway      │
│ Repository Service   │  │ Pattern Library      │  │ Trust Centre         │
│ Dependency Manager   │  │                      │  │ Plugin Manager       │
│ Environment Manager  │  │                      │  │ Plugin Host          │
│ Artefact Manager     │  │                      │  │ MCP Gateway          │
└──────────────────────┘  └──────────────────────┘  └──────────────────────┘
```

For the full architecture guide, see [`ARCHITECTURE.md`](ARCHITECTURE.md).

---

## Architectural Invariants

The following rules are intended to remain true throughout implementation:

1. AI providers are accessed only through the AI Router.
2. Protected project writes are performed only through the Patch Service.
3. Project reads are mediated by the Workspace Service.
4. Secrets are stored only in the Secrets Vault.
5. Protected network activity is mediated by the Network Gateway.
6. MCP access is mediated by the MCP Gateway.
7. Third-party plugins execute outside the trusted core.
8. Security decisions are deterministic.
9. AI cannot grant permissions.
10. Approval binds to exact or bounded scope.
11. Material changes invalidate approval.
12. Project memory is isolated by project.
13. Durable knowledge retains provenance.
14. Vector similarity is not treated as proof.
15. Workflows cannot escalate their own authority.
16. Desktop views do not own authoritative domain state.
17. Patch application, Git staging and commit remain separate.
18. Cloud fallback is never silent.
19. Secret values never enter Trust Centre records.
20. Every state-changing capability includes recovery design.

---

## Platform Components

### Runtime Kernel

Starts and supervises Opure services.

Responsibilities include:

- Runtime identity;
- configuration;
- service discovery;
- dependency validation;
- lifecycle;
- messaging;
- scheduling;
- health;
- shutdown;
- and recovery.

### Project Manager

Owns:

- project identity;
- project lifecycle;
- project profiles;
- project settings;
- project policy association;
- and project health.

### Workspace Service

Owns safe file access:

- workspace boundaries;
- canonical paths;
- file identity;
- file revisions;
- exclusions;
- file reading;
- file watching;
- and snapshots.

### Patch Service

Owns reviewable file changes:

- patch format;
- patch versions;
- diffs;
- validation;
- conflicts;
- secret scans;
- staging;
- transactional application;
- reversal;
- and recovery journals.

### AI Router

Provides replaceable AI access:

- provider registry;
- model registry;
- capability discovery;
- request routing;
- streaming;
- cancellation;
- structured output;
- local model state;
- and usage evidence.

### Knowledge Engine

Provides project memory:

- files;
- symbols;
- relationships;
- decisions;
- errors;
- fixes;
- builds;
- tests;
- workflows;
- conversations;
- and proven patterns.

### Workflow Engine

Coordinates engineering workflows.

Agents are implemented as bounded workflow roles such as:

- Planner;
- Architect;
- Coder;
- Reviewer;
- Tester;
- Documentation;
- and Git.

They are not separate authorities.

### Build Manager

Owns:

- Build Profiles;
- Test Profiles;
- command plans;
- process execution;
- diagnostics;
- build results;
- test results;
- and artefact evidence.

### Repository Service

Owns Git and future version-control operations:

- status;
- branches;
- staging;
- commits;
- history;
- remotes;
- merges;
- and recovery state.

### Plugin System

Provides native Opure extensions through:

- manifests;
- capability contracts;
- scoped permissions;
- isolated execution;
- safe updates;
- and removal.

A plugin may extend Opure.

It may not bypass Opure.

### MCP Gateway

Mediates Model Context Protocol servers through:

- capability discovery;
- schema validation;
- permissions;
- secrets;
- network controls;
- cancellation;
- and Trust Centre recording.

### Trust Centre

Provides an inspectable view of significant activity:

- workflows;
- models;
- context;
- tools;
- commands;
- network requests;
- permissions;
- approvals;
- patches;
- tests;
- Git operations;
- plugins;
- MCP;
- and outcomes.

Secret values are never recorded.

---

## Local and Cloud Behaviour

Every project has an independent cloud policy.

Supported modes are:

- **Local Only**
- **Ask Every Time**
- **Approved Providers Only**
- **Custom**

New projects default to **Ask Every Time**, with **Local Only** offered equally and clearly.

Under Local Only:

- project data is not sent to remote AI providers;
- remote embeddings are blocked;
- remote reranking is blocked;
- and background project-data telemetry is prohibited.

A local model failure never silently triggers remote inference.

---

## Reviewable Patch Model

The intended change flow is:

```text
Developer Request
    ↓
Plan
    ↓
Patch Proposal
    ↓
Validation
    ↓
Secret Scan
    ↓
Developer Review
    ↓
Approval
    ↓
Transactional Apply
    ↓
Verification
    ↓
Build and Test
    ↓
Trust Centre Outcome
```

Patch application does not automatically:

- stage files;
- create a Git commit;
- push changes;
- or publish anything externally.

Those remain separate actions.

---

## Workflow Model

Opure workflows are:

- versioned;
- inspectable;
- cancellable;
- resumable;
- checkpointed;
- permission-bound;
- and recoverable.

A workflow may include:

- planning;
- architecture;
- coding;
- review;
- validation;
- repair;
- documentation;
- Git preparation;
- and release preparation.

AI may propose a plan.

It cannot grant itself permission to execute it.

---

## Project Memory

Project memory must remain:

- isolated;
- source-aware;
- version-aware;
- correctable;
- deletable;
- and evidence-based.

Every durable record identifies provenance.

The platform distinguishes:

- source evidence;
- model inference;
- tool inference;
- deterministic validation;
- and developer confirmation.

Vector similarity may help retrieval.

It never proves correctness.

---

## Secrets

Secrets include:

- passwords;
- API keys;
- tokens;
- private keys;
- connection strings;
- signing credentials;
- authentication cookies;
- and equivalent sensitive material.

Secrets must not be stored in:

- project memory;
- vector indexes;
- ordinary configuration;
- workflow checkpoints;
- plugin storage;
- logs;
- conversation history;
- or Trust Centre records.

They belong in the encrypted Secrets Vault.

---

## Performance Modes

Opure defines four performance modes.

### Eco

Reduces resource use and background work.

### Balanced

Preserves responsiveness while allowing normal engineering work.

This is the proposed default.

### Performance

Increases throughput and parallelism where justified.

### Turbo

Uses aggressive resource limits explicitly selected by the developer.

Performance mode never changes security policy.

---

## Reference Hardware

The initial reference environment is:

- Windows 11;
- AMD Ryzen 9 5950X;
- 32 GB RAM;
- NVIDIA RTX 5070 Ti with 16 GB VRAM;
- local Git;
- and Ollama.

The platform should:

- remain responsive during local inference;
- keep one fast model loaded where practical;
- load heavier models only when required;
- avoid wasteful multi-model parallelism;
- and expose CPU, memory, GPU and VRAM use.

---

## Current Repository Structure

```text
Opure/
├── README.md
├── ARCHITECTURE.md
├── specs/
│   ├── CHARTER-001.md
│   ├── SPEC-001.md
│   ├── SPEC-002.md
│   ├── SPEC-003.md
│   ├── SPEC-004.md
│   ├── SPEC-005.md
│   ├── SPEC-006.md
│   ├── SPEC-007.md
│   ├── SPEC-008.md
│   ├── SPEC-009.md
│   ├── SPEC-010.md
│   ├── SPEC-011.md
│   └── SPEC-012.md
└── ...
```

The repository is expected to evolve towards:

```text
Opure/
├── README.md
├── ARCHITECTURE.md
├── specs/
├── docs/
├── adr/
├── src/
├── tests/
├── tools/
├── scripts/
├── samples/
├── packaging/
├── assets/
├── benchmarks/
├── security/
└── .github/
```

---

## Specification Index

### Founding Charter

[`specs/CHARTER-001.md`](specs/CHARTER-001.md)  
Defines Opure's permanent founding commitments.

### Vision and Design Principles

[`specs/SPEC-001.md`](specs/SPEC-001.md)  
Defines the platform vision, scope, design principles and product boundaries.

### Runtime Kernel

[`specs/SPEC-002.md`](specs/SPEC-002.md)  
Defines Runtime startup, service supervision, scheduling, messaging, health and recovery.

### Service Architecture

[`specs/SPEC-003.md`](specs/SPEC-003.md)  
Defines service boundaries, ownership, contracts, events, persistence and integration.

### AI Router

[`specs/SPEC-004.md`](specs/SPEC-004.md)  
Defines provider-neutral model discovery, routing, streaming, cancellation and local/cloud controls.

### Memory Engine

[`specs/SPEC-005.md`](specs/SPEC-005.md)  
Defines project memory, provenance, indexing, embeddings, retrieval and the Proven Pattern Library.

### Workflow and Agent System

[`specs/SPEC-006.md`](specs/SPEC-006.md)  
Defines workflows, stages, roles, permissions, checkpoints, cancellation and recovery.

### Plugin SDK

[`specs/SPEC-007.md`](specs/SPEC-007.md)  
Defines plugin packages, manifests, permissions, isolation, capabilities and lifecycle.

### Trust Centre and Security

[`specs/SPEC-008.md`](specs/SPEC-008.md)  
Defines policy, approvals, secrets, data classification, network controls, audit and quarantine.

### Workspace and File Patch Engine

[`specs/SPEC-009.md`](specs/SPEC-009.md)  
Defines workspace boundaries, file identity, patch validation, transactional application and recovery.

### Desktop User Interface

[`specs/SPEC-010.md`](specs/SPEC-010.md)  
Defines the desktop shell, project views, workflows, patch review, approvals, Trust Centre and accessibility.

### Project and Build Management

[`specs/SPEC-011.md`](specs/SPEC-011.md)  
Defines project lifecycle, builds, tests, dependencies, Git, environments, artefacts and releases.

### Roadmap

[`specs/SPEC-012.md`](specs/SPEC-012.md)  
Defines delivery phases, evidence gates, release criteria and deferred scope.

---

## Delivery Roadmap

The implementation roadmap is evidence-gated.

### Phase 0 — Founding Baseline

- approve specifications;
- create ADR process;
- choose implementation stack;
- define repository conventions.

### Phase 1 — Architecture Skeleton

- Runtime;
- service registry;
- lifecycle;
- messaging;
- scheduler;
- health;
- desktop shell.

### Phase 2 — Safe Project Foundation

- Project Manager;
- Workspace Service;
- read-only project inspection;
- Git status;
- project health.

### Phase 3 — Reviewable Patch Vertical Slice

- Patch Service;
- diff review;
- secret scanning;
- approval;
- transactional apply;
- reversal;
- recovery.

### Phase 4 — Local AI Router

- AI Router;
- Ollama adapter;
- simulated second adapter;
- model discovery;
- routing;
- streaming;
- cancellation.

### Phase 5 — Context and Project Memory

- parsing;
- project knowledge;
- local embeddings;
- retrieval;
- provenance;
- Memory Explorer.

### Phase 6 — First Assisted Workflow

- small bug-fix workflow;
- Planner;
- Coder;
- Reviewer;
- Tester;
- patch and approval integration.

### Phase 7 — Build, Test and Git

- Build Profiles;
- Test Profiles;
- command plans;
- diagnostics;
- staging;
- commit preparation.

### Phase 8 — Security Completion

- full Policy Engine;
- Approval Manager;
- Vault;
- Network Gateway;
- audit integrity;
- quarantine;
- Safe Mode.

### Phase 9 — Plugin SDK

- package format;
- Plugin Host;
- capabilities;
- permissions;
- examples;
- update rollback.

### Phase 10 — MCP Gateway

- server registry;
- discovery;
- schema validation;
- permissions;
- invocation;
- audit.

### Phase 11 — Proven Pattern Library

- evidence states;
- promotion;
- reuse;
- regression handling.

### Phase 12 — First Usable Product

- complete local developer journeys;
- offline operation;
- recovery;
- founder dogfooding.

Later phases cover Private Alpha, Technical Preview, Beta and Version 1.0.

---

## Version 1.0 Goal

Version 1.0 should allow a developer to:

- open a local project;
- understand its structure;
- use local AI;
- request a bounded engineering workflow;
- review every proposed change;
- apply changes safely;
- build and test;
- prepare Git commits;
- inspect permissions and external sharing;
- recover interrupted work;
- and remain in control.

---

## Version 1.0 Exclusions

Version 1.0 does not require:

- distributed agent swarms;
- hosted project storage;
- a permanent cloud control plane;
- team collaboration;
- a public plugin marketplace;
- mobile applications;
- remote build farms;
- autonomous release publication;
- universal language support;
- or multi-user live editing.

---

## Immediate Next Steps

The next implementation actions are:

1. review and approve the Founder Draft specifications;
2. create the `adr/` directory;
3. create an ADR template;
4. choose the primary implementation language;
5. choose the desktop framework;
6. define the Runtime process topology;
7. define local IPC and contract serialisation;
8. define the first source-tree layout;
9. create the testing skeleton;
10. implement the Runtime boot-to-health vertical slice.

No implementation stack is assumed in this README until the relevant ADRs are approved.

---

## Architecture Decision Records

Major implementation choices must be recorded as ADRs.

The first ADRs should cover:

- implementation language;
- desktop framework;
- Runtime process topology;
- local IPC;
- contract serialisation;
- persistence;
- logging;
- testing;
- Windows path handling;
- and packaging.

ADRs belong in:

```text
adr/
```

---

## Development Principles

Implementation should follow these rules:

- one authoritative owner per domain;
- no direct cross-service database access;
- no direct provider SDK use outside adapters;
- no direct project writes outside Patch Service;
- no direct secret access outside Vault contracts;
- no direct MCP access outside MCP Gateway;
- no hidden network activity;
- no state-changing capability without recovery;
- no feature complete without tests;
- and no specification silently weakened by implementation convenience.

---

## Testing Expectations

Opure requires:

- unit tests;
- contract tests;
- integration tests;
- architecture tests;
- fault injection;
- security tests;
- recovery tests;
- performance tests;
- accessibility tests;
- and usability tests.

State-changing capabilities must be tested at multiple interruption points.

---

## Release-Blocking Defects

The following should block a release:

- project data loss;
- secret leakage;
- permission bypass;
- silent cloud transmission;
- patch preview mismatch;
- unrecoverable migration failure;
- plugin escape into the trusted core;
- false Trust Centre records;
- or uninstall behaviour that deletes developer projects.

---

## Contributing

The contribution process will be defined after the implementation language and toolchain ADRs are approved.

Until then:

- preserve specification intent;
- use British English in project documentation;
- keep changes reviewable;
- record major decisions;
- avoid provider-specific coupling;
- avoid hidden cloud dependencies;
- and do not commit secrets.

---

## Security

Security reporting instructions will be added before any public technical preview.

Do not include real credentials, tokens, private keys or customer data in:

- issues;
- examples;
- tests;
- fixtures;
- screenshots;
- logs;
- or sample projects.

---

## Licence

The project licence is not yet defined.

No licence assumption should be made until the relevant founder and legal decision is recorded.

---

## Project Motto

> **Developer Respect. Local Intelligence. Complete Control.**

---

## Golden Rule

> **Build software with developers, not instead of them.**