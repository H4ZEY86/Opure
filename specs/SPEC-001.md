# SPEC-001 — Vision and Design Principles

## Opure Platform Product Direction

**Document:** SPEC-001  
**Status:** Founder Draft  
**Version:** 0.1  
**Language:** British English  
**Last updated:** 18 July 2026  
**Depends on:** CHARTER-001  

---

## 1. Purpose

This specification translates the founding commitments in **CHARTER-001** into practical product and engineering direction for the Opure Platform.

The Charter defines what Opure believes.

This specification defines how those beliefs must shape:

- product decisions;
- user experience;
- software architecture;
- artificial-intelligence behaviour;
- automation;
- integrations;
- privacy and security;
- performance;
- and future technical specifications.

Every later Opure specification must remain consistent with this document and with CHARTER-001.

Where a conflict exists, CHARTER-001 takes precedence.

---

## 2. Normative Language

The following words express requirement strength throughout the Opure specifications:

- **MUST** — required for compliance.
- **MUST NOT** — prohibited.
- **SHOULD** — expected unless a documented reason justifies an alternative.
- **SHOULD NOT** — discouraged unless a documented reason justifies it.
- **MAY** — optional.
- **DEFERRED** — intentionally postponed to a later specification or project phase.

A decision that intentionally violates a **SHOULD** or **SHOULD NOT** requirement MUST be recorded in an Architecture Decision Record.

A decision that violates a **MUST** or **MUST NOT** requirement requires an approved amendment to this specification or the Charter.

---

## 3. Product Definition

Opure is a local, developer-first software engineering platform.

It helps developers:

- understand existing projects;
- design software architecture;
- plan engineering work;
- write and modify code;
- review proposed changes;
- run builds and tests;
- diagnose errors;
- manage project knowledge;
- document decisions;
- automate controlled workflows;
- integrate external engineering tools;
- and improve software over time.

Opure uses artificial intelligence as a replaceable engineering capability.

Artificial intelligence is not the product's authority, identity or permanent centre.

> **Opure is a software engineering platform that uses AI — not an AI platform that happens to write code.**

---

## 4. Product Statement

> **Opure is a software engineering platform built for developers that uses artificial intelligence to help them build, understand and maintain software without taking away their control.**

The product statement MUST remain understandable without requiring knowledge of:

- a particular AI model;
- a particular AI provider;
- a particular database;
- a particular desktop framework;
- a particular plugin;
- or a particular cloud service.

Implementation choices may change without changing Opure's identity.

---

## 5. Motto and Guiding Statement

### 5.1 Platform Motto

> **Developer Respect. Local Intelligence. Complete Control.**

### 5.2 Golden Rule

> **Build software with developers, not instead of them.**

When product convenience conflicts with meaningful developer control, the developer's control MUST take precedence unless a safety requirement demands otherwise.

---

## 6. Initial Product Scope

The first production target for Opure is a local desktop engineering platform for Windows 11.

The initial product SHOULD provide:

- project creation and import;
- workspace inspection;
- local project memory;
- local AI-provider integration;
- an AI Router;
- reviewable file patches;
- build and test execution;
- Git integration;
- a Trust Centre;
- permission and policy controls;
- plugin infrastructure;
- MCP integration;
- and controlled workflow automation.

The initial product MUST be designed for excellent performance on modern developer workstations.

The founder's reference development system is:

- Windows 11;
- AMD Ryzen 9 5950X;
- 32 GB system memory;
- NVIDIA RTX 5070 Ti with 16 GB VRAM.

This machine is a performance target and development reference, not a minimum requirement.

Exact minimum and recommended system requirements are DEFERRED to a later specification.

---

## 7. Windows-First, Not Windows-Locked

Opure is Windows-first for its initial implementation.

This means:

- Windows 11 receives first-class support;
- Windows-native security and credential capabilities SHOULD be used where appropriate;
- the initial desktop application MAY use Windows-specific integrations;
- and testing SHOULD prioritise Windows reliability.

Windows-first MUST NOT mean permanently Windows-locked.

Core services, contracts, data formats and plugin interfaces SHOULD avoid unnecessary operating-system dependence.

Operating-system-specific behaviour MUST be isolated behind clear interfaces wherever practical.

Future Linux and macOS support MAY be added without redefining the platform's core architecture.

Cross-platform release support is not required for the first implementation.

---

## 8. Target Users

Opure is intended for developers who want intelligent engineering assistance without surrendering control.

Initial user groups include:

### 8.1 Independent Developers

Developers building personal projects, utilities, bots, applications, games, services or experiments.

They need:

- simple setup;
- understandable automation;
- local privacy;
- practical project assistance;
- and efficient use of personal hardware.

### 8.2 Maintainers

Developers responsible for understanding, repairing and evolving existing software.

They need:

- project memory;
- architecture discovery;
- safe patch generation;
- build and test integration;
- and recorded engineering decisions.

### 8.3 Small Engineering Teams

Teams that want shared engineering workflows without making cloud dependency mandatory.

They need:

- reproducibility;
- policy controls;
- consistent reviews;
- plugin integrations;
- and exportable project knowledge.

### 8.4 Learners and Less-Experienced Developers

Developers who benefit from explanations, guidance and visible engineering reasoning.

Opure MUST support learning without treating the user as incapable.

It SHOULD explain decisions without hiding complexity or replacing understanding.

### 8.5 Advanced Developers

Developers who require deep control, inspectable prompts, custom providers, command-line tools, MCP servers and automation.

Opure MUST NOT simplify the interface by removing access to important technical detail.

Progressive disclosure SHOULD make advanced detail available without overwhelming new users.

---

## 9. Product Goals

### 9.1 Developer Authority

The developer MUST remain the final authority over important actions.

### 9.2 Local Capability

Core engineering workflows MUST operate locally wherever technically practical.

### 9.3 Safe Automation

Automation MUST be visible, limited by policy and reversible wherever technically practical.

### 9.4 Project Understanding

Opure SHOULD develop durable, structured understanding of each project.

### 9.5 Better Engineering Outcomes

Opure SHOULD improve correctness, maintainability, documentation, testing and project understanding rather than merely increase code output.

### 9.6 Replaceable Intelligence

AI models and providers MUST remain replaceable through an AI Router or equivalent abstraction.

### 9.7 Open Integration

Plugins, MCP, APIs and command-line tools SHOULD extend the platform without bypassing permissions or security controls.

### 9.8 Trustworthy Operation

Important actions MUST be inspectable through the Trust Centre.

### 9.9 Efficient Performance

Opure SHOULD use hardware deliberately and avoid unnecessary model loading, parallelism and resource consumption.

### 9.10 Long-Term Maintainability

Major components SHOULD remain replaceable and independently testable through loose coupling and versioned contracts.

---

## 10. Product Non-Goals

Opure is not intended to become:

- a general-purpose social chatbot;
- a replacement for developer judgement;
- a fully uncontrolled autonomous coding agent;
- an AI-provider-specific desktop client;
- a cloud-only development environment;
- a hidden telemetry product;
- a low-code platform that prevents access to source code;
- a system that silently rewrites entire projects;
- a proprietary replacement for every engineering tool;
- or an IDE that must own every part of the developer's workflow.

Opure MAY provide editing, terminal, project-browser and debugging interfaces.

However, it MUST NOT require developers to abandon their preferred external editors or development tools.

Opure SHOULD integrate with existing tools rather than replacing them without a clear benefit.

---

# 11. Foundational Product Principles

## 11.1 Developer Respect

Every product decision MUST be tested against its impact on:

- developer time;
- developer attention;
- developer privacy;
- developer ownership;
- developer understanding;
- developer hardware;
- developer workflow;
- and developer choice.

Opure SHOULD avoid:

- unnecessary prompts;
- repeated questions whose answers are already known;
- noisy logs;
- unexplained failures;
- excessive confirmation for harmless actions;
- and hidden automation for significant actions.

The platform SHOULD distinguish between low-risk convenience and high-impact authority.

Developer Respect does not require interrupting the developer for every harmless operation.

It requires using permissions, risk classification and clear defaults intelligently.

---

## 11.2 Local by Design. Cloud Optional. Always.

Local operation MUST be treated as a primary architecture path.

Local providers, local project memory, local builds, local tests and local tools SHOULD remain fully useful without a cloud account.

Cloud features MAY provide:

- remote AI models;
- remote repositories;
- hosted collaboration;
- external documentation;
- package registries;
- online search;
- and third-party integrations.

Cloud features MUST NOT silently become prerequisites for unrelated local features.

When a workflow requires online access by its nature, Opure MUST clearly identify that dependency.

---

## 11.3 Visible by Design

The user interface MUST make important actions discoverable and understandable.

Visibility SHOULD include:

- what Opure intends to do;
- what it is currently doing;
- what data it is using;
- what external service it is contacting;
- what changed;
- what succeeded;
- what failed;
- and how to recover.

Visibility MUST NOT mean exposing raw noise without interpretation.

Opure SHOULD provide layered detail:

1. a concise summary;
2. an expanded technical explanation;
3. complete inspectable records where available.

---

## 11.4 Human in Control

Opure MUST support multiple levels of automation without confusing automation with authority.

Recommended automation levels include:

- **Manual** — Opure analyses and proposes only.
- **Assisted** — Opure may prepare actions but requests approval before applying them.
- **Controlled Automation** — Opure may act within explicit project permissions.
- **Workflow Automation** — approved multi-step workflows may run within defined limits.

Names and exact interface treatment are DEFERRED to SPEC-010.

No automation level may bypass:

- project policy;
- secret protection;
- irreversible-action warnings;
- provider restrictions;
- or Trust Centre recording.

---

## 11.5 Engineering Before Generation

The platform SHOULD ask, when relevant:

- Is a code change actually needed?
- Can the existing design be reused?
- Is there a proven project pattern?
- What tests are required?
- What documentation must change?
- What risks does the change introduce?
- Can the change be smaller?
- Can the result be verified?

The platform MUST NOT measure success by lines of code generated.

---

# 12. User Experience Principles

## 12.1 Clear Before Clever

The interface SHOULD prefer clarity over novelty.

Animations, agent personas and visual effects MUST NOT obscure:

- system state;
- permissions;
- risk;
- progress;
- errors;
- or developer choices.

### 12.2 Progressive Disclosure

Common tasks SHOULD remain approachable.

Advanced technical detail MUST remain available.

The interface SHOULD allow the developer to move from:

- summary;
- to explanation;
- to raw technical record.

### 12.3 Honest State

Opure MUST clearly distinguish between:

- planned;
- proposed;
- running;
- completed;
- failed;
- partially completed;
- unverified;
- and cancelled work.

The platform MUST NOT present unverified output as completed engineering.

### 12.4 Actionable Errors

Errors SHOULD explain:

- what failed;
- where it failed;
- likely causes;
- what evidence is available;
- and sensible recovery actions.

Opure MUST NOT invent a cause when evidence is insufficient.

### 12.5 Minimal Interruption

Harmless, reversible and previously authorised operations SHOULD require minimal interruption.

Potentially destructive, external, secret-bearing or irreversible operations MUST receive appropriate visibility and approval.

### 12.6 No Dark Patterns

Opure MUST NOT use interface design to pressure the developer into:

- enabling cloud access;
- selecting a paid provider;
- granting broad permissions;
- sharing more data than necessary;
- or accepting automation they did not request.

---

# 13. Project Model

A project is the primary unit of engineering context in Opure.

Each project SHOULD have independent:

- settings;
- permissions;
- cloud policy;
- provider preferences;
- project memory;
- workflow configuration;
- plugin configuration;
- Trust Centre records;
- build configuration;
- and knowledge boundaries.

Project data MUST NOT leak into another project unless the developer deliberately enables a shared knowledge or pattern mechanism.

Global settings MAY provide defaults.

Project-specific settings MUST override global defaults where the developer has deliberately configured them.

---

## 13.1 Project Creation

Project creation SHOULD support:

- creating a new project;
- importing an existing directory;
- cloning a repository;
- opening an existing repository;
- and registering a project without moving its files.

During creation, Opure SHOULD identify:

- programming languages;
- build tools;
- package managers;
- repository status;
- existing tests;
- likely entry points;
- project documentation;
- and potential sensitive files.

Detected information MUST be presented as an initial assessment rather than unquestionable fact.

---

## 13.2 Project Onboarding

Initial project analysis SHOULD be staged and controllable.

Opure SHOULD avoid scanning every file immediately when:

- the project is extremely large;
- directories are excluded;
- generated content dominates the repository;
- or the operation would cause substantial delay or resource use.

The developer SHOULD be able to inspect and change exclusions before deep analysis.

Default exclusions SHOULD include common generated, dependency, cache and secret-bearing directories.

---

# 14. AI and Automation Principles

## 14.1 AI Router

All AI-provider access MUST pass through an AI Router or equivalent provider-neutral service.

The AI Router SHOULD manage:

- provider selection;
- model selection;
- capability matching;
- local versus cloud policy;
- context size;
- task complexity;
- performance mode;
- availability;
- cost information where applicable;
- and failure recovery.

No major Opure component SHOULD depend directly on an individual AI-provider SDK.

---

## 14.2 Replaceable Providers

The initial implementation MAY support Ollama first.

The architecture MUST allow future support for providers such as:

- LM Studio;
- llama.cpp;
- OpenAI-compatible services;
- hosted commercial providers;
- and future provider types.

Provider support does not imply automatic permission to share project data.

Every provider remains subject to project policy.

---

## 14.3 Agents as Workflows

Planner, Architect, Coder, Reviewer, Tester, Documentation and Git roles SHOULD be implemented as workflows or task roles rather than permanently loaded independent model processes.

Multiple roles MAY use the same model and inference engine.

The platform SHOULD avoid loading many large models simultaneously unless a measured engineering benefit justifies it.

Agent names MUST NOT create the false impression that independently conscious entities are operating.

---

## 14.4 Model Honesty

Opure MUST clearly identify when output is:

- generated;
- inferred;
- retrieved from project knowledge;
- retrieved from a proven pattern;
- produced by a tool;
- or verified by a test.

AI confidence MUST NOT be presented as proof.

The platform SHOULD separate:

- model suggestion;
- platform validation;
- tool evidence;
- and developer approval.

---

## 14.5 Prompt and Context Visibility

The developer MUST be able to inspect the effective request sent to an external or local model, subject to reasonable redaction of secrets.

The interface SHOULD show:

- system instructions relevant to the task;
- user intent;
- selected project context;
- retrieved knowledge;
- tool results;
- provider and model;
- and context removed because of policy or limits.

Internal implementation metadata MAY be hidden by default but MUST remain inspectable where it affects behaviour.

---

## 14.6 AI Failure Handling

When a model is unavailable, fails, times out or returns invalid output, Opure SHOULD:

1. preserve the current project state;
2. explain the failure honestly;
3. record the event;
4. offer a safe retry or alternate provider where permitted;
5. avoid repeating expensive work unnecessarily;
6. and retain validated intermediate results where useful.

The platform MUST NOT silently switch from a local provider to a cloud provider.

---

# 15. Knowledge Engine Principles

## 15.1 Purpose

The Knowledge Engine exists to help Opure understand a project over time.

It SHOULD maintain structured knowledge rather than relying exclusively on chat history or vector similarity.

### 15.2 Knowledge Categories

The Knowledge Engine MAY store:

- projects;
- files;
- modules;
- classes;
- functions;
- services;
- dependencies;
- interfaces;
- architecture;
- tasks;
- errors;
- fixes;
- tests;
- build results;
- decisions;
- documentation;
- conversations;
- workflows;
- and reusable patterns.

### 15.3 Structured Authority

Structured records and direct project evidence SHOULD remain authoritative.

Vector search MAY assist semantic retrieval.

A vector similarity score MUST NOT be treated as proof that code is correct, compatible or safe.

### 15.4 Proven Pattern Library

Reusable engineering solutions SHOULD be stored with:

- source project or origin;
- language and framework;
- dependencies;
- assumptions;
- validation evidence;
- test evidence;
- review history;
- reuse history;
- known limitations;
- and confidence level.

A pattern MUST NOT be promoted to **Trusted** solely because it was generated by a highly capable model.

### 15.5 Project Isolation

Project memory MUST remain isolated by default.

Cross-project reuse MUST be deliberate and visible.

Sensitive project content MUST NOT be copied into a global pattern library without explicit approval.

### 15.6 Forgetting and Correction

The developer MUST be able to:

- inspect stored project knowledge;
- correct inaccurate records;
- remove obsolete records;
- exclude files or areas;
- rebuild indexes;
- and delete project memory.

The Knowledge Engine MUST NOT treat historical records as permanently correct.

---

# 16. File and Patch Principles

## 16.1 Patches by Default

Generated code changes MUST be represented as patches by default.

A patch SHOULD include:

- the original state;
- the proposed state;
- a human-readable explanation;
- affected files;
- validation status;
- and recovery information.

### 16.2 Smallest Safe Change

Opure SHOULD prefer the smallest safe patch that satisfies the requested outcome.

Large rewrites require stronger justification and visibility.

### 16.3 Direct File Modification

Direct file modification MAY occur only when:

- the developer explicitly applies an approved patch;
- an authorised controlled workflow permits it;
- or the operation is harmless, expected and reversible.

All significant modifications MUST be recorded.

### 16.4 Conflict Handling

When the project changes after a patch is generated, Opure MUST detect likely conflicts before applying it.

The platform SHOULD regenerate or rebase the patch rather than overwrite newer work.

### 16.5 Validation

Where suitable tools exist, proposed changes SHOULD be validated through:

- parsing;
- formatting;
- linting;
- type checking;
- compilation;
- unit tests;
- integration tests;
- and project-specific checks.

A patch MUST clearly state which checks were and were not performed.

---

# 17. Security and Privacy Principles

## 17.1 Least Privilege

Every service, plugin, MCP server, workflow and model tool SHOULD receive only the permissions required for its current task.

Broad permanent access MUST NOT be the default.

### 17.2 Explicit External Sharing

Project data MUST NOT be sent to an external service without a policy allowing it and appropriate developer approval.

### 17.3 Visible Data Sharing

Before external transmission, Opure MUST show:

- what data will be shared;
- the destination;
- the purpose;
- detected sensitive content;
- and available local alternatives.

### 17.4 Secret Protection

Secrets MUST be excluded from:

- normal project databases;
- vector indexes;
- model prompts;
- logs;
- conversation history;
- generated patches;
- and Trust Centre detail.

Exceptions require deliberate, controlled handling through the encrypted secrets vault.

### 17.5 Encrypted Secrets Vault

Passwords, tokens, API keys, private keys and other credentials MUST be stored through a dedicated encrypted secrets vault.

The vault SHOULD use Windows operating-system protection for the initial platform where suitable.

The exact cryptographic design is DEFERRED to SPEC-008.

### 17.6 No Secret Telemetry

Opure MUST NOT collect or transmit secrets for analytics, diagnostics or telemetry.

### 17.7 Telemetry

Telemetry MUST be disabled by default unless a later founder-approved policy explicitly changes this requirement.

Any future telemetry MUST be:

- opt-in;
- visible;
- inspectable;
- minimal;
- and free from project content and secrets.

---

# 18. Cloud Policy Principles

Each project MUST have a visible cloud policy.

Initial modes are:

- **Local Only**;
- **Ask Every Time**;
- **Approved Providers Only**;
- **Custom Policy**.

New projects MUST default to **Ask Every Time**.

During project creation, **Local Only** MUST be offered as an equally visible choice.

No plugin, MCP server, workflow, provider or service may bypass the active project policy.

Permissions MUST be revocable.

A less restrictive policy MUST require deliberate developer action.

---

# 19. Integration Principles

## 19.1 Integration Types

Opure SHOULD support:

- native plugins;
- MCP servers;
- command-line adapters;
- REST or other API connectors;
- and local services.

### 19.2 Mediation

AI models MUST NOT communicate directly with tools that can modify the system.

Tool requests MUST pass through Opure's:

- policy checks;
- permission checks;
- security controls;
- network controls;
- and Trust Centre recording.

### 19.3 Plugin Independence

Plugins SHOULD remain independently installable, removable, versioned and testable.

A plugin failure MUST NOT crash the entire platform.

### 19.4 Capability Declaration

Plugins and MCP servers MUST declare requested capabilities, such as:

- filesystem read;
- filesystem write;
- command execution;
- network access;
- secret access;
- repository access;
- and project-memory access.

Declared capability does not automatically grant permission.

### 19.5 Trust Boundaries

Third-party plugins and MCP servers MUST be treated as external trust boundaries.

Their identity, publisher, permissions and activity SHOULD be visible.

---

# 20. Architecture Principles

## 20.1 Service-Oriented Internal Design

Major capabilities SHOULD be implemented as independently testable services or modules.

Expected major subsystems include:

- Runtime Kernel;
- Scheduler;
- Workflow Engine;
- AI Router;
- Knowledge Engine;
- Plugin Manager;
- MCP Gateway;
- Policy Engine;
- Network Gateway;
- Workspace and Patch Engine;
- Secrets Vault;
- Trust Centre;
- Storage Services;
- and Desktop Interface.

Exact boundaries are DEFERRED to SPEC-002 and SPEC-003.

### 20.2 Contract-Driven Communication

Subsystems SHOULD communicate through:

- interfaces;
- typed messages;
- events;
- commands;
- queries;
- and versioned contracts.

### 20.3 No Unnecessary Distribution

Loose coupling does not require every service to become a separate process.

The initial implementation SHOULD prefer a modular local architecture over unnecessary networked microservices.

Process separation MAY be used for:

- security boundaries;
- fault isolation;
- resource-heavy workloads;
- provider runtimes;
- and plugin isolation.

### 20.4 Deterministic Core

Security policy, permission evaluation, patch application, secret handling and audit recording MUST be deterministic platform responsibilities.

These responsibilities MUST NOT be delegated to probabilistic model judgement.

### 20.5 Failure Isolation

Failure in one optional component SHOULD NOT make unrelated core functions unavailable.

### 20.6 Versioned Data

Persistent data formats and service contracts SHOULD support versioning and migration.

### 20.7 Replaceable Storage

The Knowledge Engine and Trust Centre MUST NOT expose storage-engine-specific behaviour to unrelated components.

SQLite MAY be used initially.

Storage choices are DEFERRED to later specifications and ADRs.

---

# 21. Workflow Principles

## 21.1 Workflows Over Hard-Coded Agents

Engineering automation SHOULD be represented as inspectable workflows.

A workflow MAY include stages such as:

1. understand intent;
2. retrieve project knowledge;
3. plan;
4. design;
5. generate a patch;
6. validate;
7. review;
8. request approval;
9. apply;
10. test;
11. document;
12. commit.

### 21.2 Stage Visibility

The current stage, completed stages and failed stages SHOULD remain visible.

### 21.3 Resumability

Long workflows SHOULD be resumable where practical.

### 21.4 Cancellation

Developers MUST be able to cancel a running workflow.

Cancellation SHOULD preserve project integrity and validated intermediate results.

### 21.5 Workflow Permissions

A workflow MUST NOT gain more authority merely because it contains multiple approved stages.

Every action remains subject to policy.

### 21.6 Workflow Templates

Opure MAY provide built-in workflow templates.

Developers SHOULD be able to inspect, copy and customise them.

---

# 22. Trust Centre Principles

The Trust Centre is the primary interface for understanding significant platform activity.

It SHOULD record:

- developer intent;
- workflow;
- provider and model;
- files and data categories accessed;
- external context shared;
- tools invoked;
- commands executed;
- network destinations;
- permissions;
- approvals;
- patches;
- tests;
- warnings;
- overrides;
- outcomes;
- timing;
- and resource usage where useful.

The Trust Centre MUST NOT record secret values.

Records SHOULD be:

- searchable;
- filterable;
- exportable;
- understandable;
- and linked to the relevant project action.

The Trust Centre exists for accountability, diagnosis and recovery.

It MUST NOT be designed as surveillance.

---

# 23. Performance Principles

## 23.1 Scheduler

Resource-heavy activity SHOULD pass through a scheduler.

The scheduler SHOULD consider:

- CPU usage;
- memory usage;
- GPU and VRAM availability;
- active model state;
- task priority;
- user interaction;
- and performance mode.

### 23.2 Model Loading

The platform SHOULD keep a suitable fast model available where hardware allows.

Larger models SHOULD load only when their expected benefit justifies the resource cost.

### 23.3 Performance Modes

Opure SHOULD support modes such as:

- **Eco**;
- **Balanced**;
- **Performance**;
- **Turbo**.

**Balanced** SHOULD be the initial default.

Exact behaviour is DEFERRED to a later runtime or desktop specification.

### 23.4 Responsiveness

The desktop interface MUST remain responsive during long-running analysis, model inference, builds and tests.

### 23.5 Resource Visibility

Substantial:

- model downloads;
- memory use;
- VRAM use;
- storage use;
- network use;
- and background computation

SHOULD be visible to the developer.

### 23.6 No Wasteful Parallelism

Multiple simultaneous agents or models MUST NOT be used merely to create the appearance of sophistication.

Parallelism SHOULD be justified by measured improvement in latency, quality or reliability.

---

# 24. Data Ownership and Portability

Project data belongs to the developer.

Opure SHOULD store project knowledge in documented, exportable formats or provide reliable export mechanisms.

The developer MUST be able to:

- export project memory;
- export Trust Centre records;
- remove a project from Opure;
- delete stored project knowledge;
- and continue using the underlying source project without Opure.

Opure MUST NOT make a source project unusable without its own database or application.

Where proprietary internal formats are necessary, documented migration and export paths SHOULD exist.

---

# 25. Quality Attributes

The Opure Platform SHOULD be evaluated against the following quality attributes.

## 25.1 Trustworthiness

The platform behaves as described and does not hide significant actions.

## 25.2 Correctness

Deterministic platform operations produce predictable, tested results.

## 25.3 Maintainability

Components are understandable, testable and replaceable.

## 25.4 Security

Access, secrets, permissions and external communication are controlled.

## 25.5 Privacy

Local data remains local unless deliberate policy permits otherwise.

## 25.6 Performance

The platform uses available hardware efficiently while remaining responsive.

## 25.7 Reliability

Failures are isolated, visible and recoverable.

## 25.8 Usability

Common workflows are understandable without hiding advanced control.

## 25.9 Extensibility

New providers, plugins, MCP servers, tools and workflows can be added without rewriting the core.

## 25.10 Portability

Core design avoids unnecessary dependence on one provider, framework or operating system.

---

# 26. Initial Success Criteria

The first useful Opure milestone SHOULD demonstrate all of the following:

1. A developer can create or import a local project.
2. Opure can inspect the project without modifying it.
3. A local AI provider can be configured through the AI Router.
4. The developer can request a code change.
5. Opure can produce a reviewable patch.
6. The patch can be accepted or rejected.
7. Relevant validation can run locally.
8. The action appears in the Trust Centre.
9. No project data leaves the machine without visible approval.
10. Secrets are blocked from ordinary storage and external sharing.
11. A project can operate under **Local Only** policy.
12. The desktop interface remains responsive during the workflow.
13. The source project remains usable outside Opure.
14. A failed workflow does not silently corrupt project files.
15. The applied change can be reversed where technically practical.

A milestone that generates code but fails these criteria is not considered a successful Opure foundation.

---

# 27. Design Review Checklist

Every major feature proposal SHOULD answer:

### Developer Respect

- What developer problem does this solve?
- Does it save time without removing understanding?
- Can the developer decline or disable it?

### Local Operation

- Can it work locally?
- What genuinely requires the internet?
- Is cloud use visible and optional?

### Control

- What actions can it perform?
- What permissions are required?
- Can permissions be revoked?

### Visibility

- What will the developer see before, during and after the action?
- What will be recorded in the Trust Centre?

### Safety

- Can it modify files?
- Can it execute commands?
- Can it access secrets?
- Can it contact external services?
- Is it reversible?

### Engineering Quality

- How is the result validated?
- What evidence supports its confidence?
- Does it improve maintainability?

### Architecture

- Which subsystem owns the capability?
- What contracts does it use?
- Can it be replaced or removed?
- Does it introduce unnecessary coupling?

### Performance

- What resources does it consume?
- Does it remain responsive?
- Is expensive work scheduled deliberately?

A feature proposal is incomplete until these questions have meaningful answers.

---

# 28. Deferred Decisions

The following details are intentionally deferred:

- desktop framework;
- programming languages used for each subsystem;
- process boundaries;
- database schema;
- vector-index implementation;
- graph-storage implementation;
- event-bus technology;
- plugin packaging format;
- MCP transport details;
- cryptographic implementation of the secrets vault;
- exact model recommendations;
- installer and update mechanism;
- monetisation;
- licensing;
- public telemetry policy beyond the current default prohibition;
- collaboration and team synchronisation;
- and non-Windows release dates.

Deferred decisions MUST be resolved through later specifications or Architecture Decision Records.

They MUST NOT be assumed merely because a prototype uses a particular technology.

---

# 29. Relationship to Later Specifications

Later specifications should expand this document as follows:

- **SPEC-002 — Runtime Kernel**
- **SPEC-003 — Service Architecture**
- **SPEC-004 — AI Router**
- **SPEC-005 — Memory Engine**
- **SPEC-006 — Workflow and Agent System**
- **SPEC-007 — Plugin SDK**
- **SPEC-008 — Trust Centre and Security**
- **SPEC-009 — Workspace and File Patch Engine**
- **SPEC-010 — Desktop User Interface**
- **SPEC-011 — Project and Build Management**
- **SPEC-012 — Roadmap**

Later specifications MAY refine terminology where necessary.

They MUST NOT weaken the founding requirements without a visible, approved change to this specification.

---

# 30. Founder Approval

This document is a founder draft until explicitly approved.

Approval means that this specification becomes the product and engineering direction for the Opure Platform.

Future implementation choices must be evaluated against:

- CHARTER-001;
- this specification;
- relevant later specifications;
- and approved Architecture Decision Records.

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**