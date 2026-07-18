# SPEC-003 — Service Architecture

## Opure Platform Service Boundaries and Contracts

**Document:** SPEC-003  
**Status:** Founder Draft  
**Version:** 0.1  
**Language:** British English  
**Last updated:** 18 July 2026  
**Depends on:** CHARTER-001, SPEC-001, SPEC-002  

---

## 1. Purpose

This specification defines the service architecture of the Opure Platform.

It establishes:

- the major services that make up Opure;
- the responsibility and ownership boundary of each service;
- which services may depend upon which capabilities;
- how services communicate;
- how contracts are versioned;
- how data ownership is divided;
- how failures are isolated;
- and how the platform avoids hidden coupling.

This document does not select a programming language, dependency-injection framework, process model, database engine or message-bus technology.

Those implementation decisions must follow the boundaries defined here and must be recorded through Architecture Decision Records where required.

---

## 2. Relationship to the Runtime Kernel

SPEC-002 defines the Runtime Kernel as the foundation that starts, supervises and coordinates Opure services.

This specification defines the services that operate within that runtime.

The Runtime Kernel owns:

- bootstrap;
- lifecycle;
- health supervision;
- task scheduling infrastructure;
- runtime messaging infrastructure;
- recovery coordination;
- and enforcement entry points.

Platform services own domain responsibilities such as:

- projects;
- workspaces;
- knowledge;
- AI providers;
- workflows;
- plugins;
- MCP;
- network access;
- policy;
- secrets;
- trust records;
- builds;
- repositories;
- and desktop-facing application operations.

The Runtime Kernel must not absorb domain responsibilities merely because it hosts the services that perform them.

---

## 3. Core Architectural Rule

> **Every major service owns one clear responsibility and communicates through explicit contracts.**

A service must not:

- read another service's private database tables;
- call another service's internal classes;
- depend on another service's private file layout;
- bypass another service's policy boundary;
- or duplicate another service's authoritative state.

Shared implementation convenience must not override architectural ownership.

---

## 4. Design Goals

The Opure service architecture must support:

- loose coupling;
- independent testing;
- clear ownership;
- deterministic security;
- controlled local execution;
- process isolation where useful;
- future provider replacement;
- plugin extensibility;
- MCP interoperability;
- reliable recovery;
- observable operation;
- and gradual evolution from an initial local implementation.

The architecture should remain understandable to a small engineering team.

It must not become a distributed microservice system merely to appear sophisticated.

---

## 5. Non-Goals

This specification does not require:

- every service to run in a separate process;
- network communication between local services;
- a container for every component;
- an enterprise service mesh;
- globally distributed data;
- cloud-hosted control planes;
- or event sourcing for every state change.

The first implementation should prefer a modular local architecture.

Process boundaries must be introduced for justified reasons such as:

- security;
- fault isolation;
- resource isolation;
- independent lifecycle;
- or external-runtime integration.

---

## 6. Normative Language

The terms **MUST**, **MUST NOT**, **SHOULD**, **SHOULD NOT**, **MAY** and **DEFERRED** have the meanings defined in SPEC-001.

Any intentional violation of a **SHOULD** requirement must be documented in an Architecture Decision Record.

---

# 7. Architectural Layers

Opure is organised into logical layers.

These layers describe responsibility, not necessarily process boundaries.

```text
┌────────────────────────────────────────────┐
│                Desktop Layer               │
│ Desktop Application / Local API Clients    │
└──────────────────────┬─────────────────────┘
                       │
┌──────────────────────▼─────────────────────┐
│         Application Coordination Layer     │
│ Desktop Gateway / Project Manager          │
│ Workflow Engine / Build Manager            │
└──────────────────────┬─────────────────────┘
                       │
┌──────────────────────▼─────────────────────┐
│              Domain Service Layer          │
│ Workspace / Knowledge / AI Router / Git    │
│ Plugin Manager / MCP Gateway               │
└──────────────────────┬─────────────────────┘
                       │
┌──────────────────────▼─────────────────────┐
│       Security and Infrastructure Layer    │
│ Policy / Network / Secrets / Trust         │
│ Storage / Scheduler / Runtime Messaging    │
└──────────────────────┬─────────────────────┘
                       │
┌──────────────────────▼─────────────────────┐
│          Operating-System Boundary         │
│ Files / Processes / GPU / Credentials      │
└────────────────────────────────────────────┘
```

Higher layers may request capabilities from lower layers.

Lower layers must not depend on desktop presentation concerns.

---

# 8. Service Categories

Services are grouped into the following categories.

## 8.1 Core Runtime Services

These provide the minimum safe platform environment:

- Runtime Kernel;
- Service Registry;
- Lifecycle Manager;
- Scheduler;
- Configuration Manager;
- Runtime Messaging;
- Health Supervisor;
- Recovery Manager.

These are defined primarily in SPEC-002.

---

## 8.2 Security and Trust Services

These enforce developer authority and platform safety:

- Policy Engine;
- Secrets Vault;
- Network Gateway;
- Trust Centre.

These services are security-critical.

Their rules must be deterministic.

They must not rely on AI judgement for enforcement.

---

## 8.3 Project and Workspace Services

These manage projects and source files:

- Project Manager;
- Workspace Service;
- Patch Service;
- Repository Service;
- Build Manager.

These services own the developer's active engineering workspace.

---

## 8.4 Intelligence and Knowledge Services

These provide project-aware assistance:

- AI Router;
- Knowledge Engine;
- Context Engine;
- Pattern Library;
- Workflow Engine.

These services must remain replaceable and policy-controlled.

---

## 8.5 Integration Services

These connect Opure to external tools and ecosystems:

- Plugin Manager;
- Plugin Host;
- MCP Gateway;
- CLI Adapter Host;
- API Connector Host.

External integrations must not bypass Opure's security services.

---

## 8.6 Presentation Services

These expose runtime capabilities to the desktop application:

- Desktop Gateway;
- Notification Service;
- View Projection Service where required.

Presentation services must not become the authority for domain state.

---

# 9. Service Catalogue

The following services form the planned Opure platform architecture.

---

## 9.1 Runtime Kernel

### Responsibility

The Runtime Kernel starts, supervises and stops the platform.

### Owns

- runtime identity;
- lifecycle coordination;
- service registration;
- health aggregation;
- recovery entry;
- task scheduling infrastructure;
- and runtime mode.

### Does Not Own

- project files;
- AI provider logic;
- workflow definitions;
- secrets;
- trust history;
- build logic;
- or plugin business behaviour.

### Required Dependencies

- Configuration Manager;
- Runtime Messaging;
- minimal audit buffer.

### Provided Capabilities

- `runtime.status.read`
- `runtime.shutdown.request`
- `runtime.mode.read`
- `runtime.service.list`
- `runtime.health.read`
- `runtime.task.cancel`

---

## 9.2 Configuration Manager

### Responsibility

Loads, validates, merges and distributes configuration.

### Owns

- configuration schemas;
- configuration precedence;
- configuration migration;
- runtime configuration snapshots;
- and change validation.

### Does Not Own

- secret values;
- project files;
- business policy decisions;
- or provider credentials.

### Required Dependencies

- Storage Service;
- Secrets Vault reference interface for secret aliases.

### Provided Capabilities

- `configuration.read`
- `configuration.validate`
- `configuration.preview`
- `configuration.apply`
- `configuration.schema.read`

### Rules

Secrets must be referenced, not stored directly.

Security settings must not be silently weakened.

---

## 9.3 Storage Service

### Responsibility

Provides durable, transaction-safe storage abstractions.

### Owns

- storage connections;
- migrations;
- transaction primitives;
- backup coordination;
- recovery metadata;
- and storage-health reporting.

### Does Not Own

- service-specific domain meaning;
- cross-service query models;
- or unrestricted access to all service data.

### Required Dependencies

- Runtime Kernel;
- Configuration Manager.

### Provided Capabilities

- `storage.transaction`
- `storage.keyvalue`
- `storage.document`
- `storage.backup`
- `storage.migrate`
- `storage.health`

### Rules

Each service must have an isolated logical storage namespace.

A service must not query another service's private storage directly.

SQLite may be used initially, but storage contracts must not expose SQLite-specific behaviour to unrelated services.

---

## 9.4 Project Manager

### Responsibility

Registers, opens, closes and manages Opure projects.

### Owns

- project identity;
- project registration;
- project metadata;
- project lifecycle;
- active project sessions;
- project settings references;
- and project capability availability.

### Does Not Own

- file contents;
- patches;
- build execution;
- project knowledge;
- or repository operations.

### Required Dependencies

- Storage Service;
- Configuration Manager;
- Policy Engine;
- Workspace Service;
- Trust Centre.

### Provided Capabilities

- `project.create`
- `project.import`
- `project.open`
- `project.close`
- `project.list`
- `project.metadata.read`
- `project.settings.read`
- `project.settings.update`

### Rules

Every project must have a stable internal identifier independent from its folder path.

A project may move on disk without losing its Opure identity.

---

## 9.5 Workspace Service

### Responsibility

Provides safe, policy-controlled access to project files and directories.

### Owns

- workspace root validation;
- file identity;
- file reads;
- file metadata;
- directory enumeration;
- path normalisation;
- file-change observation;
- exclusion rules;
- workspace snapshots;
- and transaction coordination for file operations.

### Does Not Own

- semantic knowledge;
- AI context selection;
- Git history;
- build semantics;
- or patch approval decisions.

### Required Dependencies

- Project Manager;
- Policy Engine;
- Trust Centre;
- Runtime Kernel.

### Provided Capabilities

- `workspace.file.read`
- `workspace.file.metadata`
- `workspace.directory.list`
- `workspace.snapshot.create`
- `workspace.watch`
- `workspace.exclusion.read`
- `workspace.exclusion.update`

### Rules

All file access must remain inside an approved workspace boundary unless explicit permission allows otherwise.

Symbolic links, junctions and path traversal must be resolved safely.

---

## 9.6 Patch Service

### Responsibility

Creates, validates, previews, applies and reverses file patches.

### Owns

- patch format;
- patch identity;
- patch lifecycle;
- conflict detection;
- patch preview;
- patch application transaction;
- patch reversal metadata;
- and patch validation records.

### Does Not Own

- AI generation;
- project intent;
- build execution;
- Git commit creation;
- or secret storage.

### Required Dependencies

- Workspace Service;
- Policy Engine;
- Trust Centre;
- Secrets-scanning capability;
- Repository Service where available.

### Provided Capabilities

- `patch.create`
- `patch.preview`
- `patch.validate`
- `patch.apply`
- `patch.reject`
- `patch.reverse`
- `patch.status.read`

### Rules

Patches are the default mechanism for generated file changes.

Patch application must be transactional where technically practical.

Conflicts must be detected before overwrite.

---

## 9.7 Repository Service

### Responsibility

Provides version-control operations through an abstract repository interface.

### Owns

- repository discovery;
- repository state;
- branch information;
- diff operations;
- staging;
- commit preparation;
- commit execution;
- restore operations;
- and repository-specific diagnostics.

### Does Not Own

- general workspace file access;
- cloud-hosted repository accounts;
- build execution;
- or project memory.

### Required Dependencies

- Workspace Service;
- Policy Engine;
- Trust Centre;
- CLI Adapter Host or native Git implementation;
- Secrets Vault for credentials where required.

### Provided Capabilities

- `repository.status`
- `repository.diff`
- `repository.stage`
- `repository.commit`
- `repository.branch.create`
- `repository.restore`
- `repository.history.read`

### Rules

The first implementation may support Git only.

The service contract must remain capable of supporting other version-control systems later.

Generated commits must remain reviewable and must not bypass secret scanning.

---

## 9.8 Build Manager

### Responsibility

Discovers, configures and executes project build and test operations.

### Owns

- build profiles;
- test profiles;
- command plans;
- build environment;
- build execution records;
- test execution records;
- output parsing;
- and build artefact metadata.

### Does Not Own

- arbitrary unrestricted command execution;
- package-manager credentials;
- project source edits;
- or AI review.

### Required Dependencies

- Project Manager;
- Workspace Service;
- Policy Engine;
- Scheduler;
- CLI Adapter Host;
- Trust Centre;
- Secrets Vault where required.

### Provided Capabilities

- `build.profile.detect`
- `build.execute`
- `build.cancel`
- `build.status`
- `test.execute`
- `test.cancel`
- `test.results.read`
- `build.output.stream`

### Rules

Build scripts and package scripts are external code and must be treated as a trust boundary.

Command plans must be visible before execution when risk requires approval.

---

## 9.9 Policy Engine

### Responsibility

Evaluates deterministic permissions and project policies.

### Owns

- policy schemas;
- policy evaluation;
- permission decisions;
- approval requirements;
- approval scopes;
- revocation state;
- policy explanations;
- and policy-denial reasons.

### Does Not Own

- user-interface prompts;
- secret values;
- network transport;
- file operations;
- or AI reasoning.

### Required Dependencies

- Project Manager;
- Configuration Manager;
- Storage Service;
- Trust Centre or audit buffer.

### Provided Capabilities

- `policy.evaluate`
- `policy.explain`
- `policy.read`
- `policy.update`
- `approval.create`
- `approval.validate`
- `approval.revoke`

### Rules

Policy evaluation must be deterministic.

AI output must never grant permission.

The Policy Engine must be callable by every protected service.

---

## 9.10 Secrets Vault

### Responsibility

Stores and releases secrets through a controlled, encrypted interface.

### Owns

- encrypted secret values;
- secret identity;
- secret metadata;
- access control;
- secret lifecycle;
- secret revocation;
- and secure operating-system integration.

### Does Not Own

- provider configuration;
- normal project settings;
- project files;
- or logs containing secret values.

### Required Dependencies

- operating-system credential protection;
- Policy Engine;
- Trust Centre;
- Configuration Manager.

### Provided Capabilities

- `secrets.create`
- `secrets.read`
- `secrets.update`
- `secrets.delete`
- `secrets.list.metadata`
- `secrets.access.validate`

### Rules

Secret values must not enter ordinary service storage.

Trust records may identify access but must never contain the secret value.

---

## 9.11 Network Gateway

### Responsibility

Mediates approved network communication.

### Owns

- network policy enforcement;
- outbound request routing;
- domain and endpoint allow-lists;
- request metadata;
- data-sharing summaries;
- network activity records;
- connection cancellation;
- and network diagnostics.

### Does Not Own

- provider business logic;
- browser presentation;
- project policy definition;
- or secret storage.

### Required Dependencies

- Policy Engine;
- Secrets Vault;
- Trust Centre;
- Scheduler;
- Project Manager.

### Provided Capabilities

- `network.request`
- `network.stream`
- `network.cancel`
- `network.preview`
- `network.activity.read`

### Rules

Services must not bypass the Network Gateway for protected external communication where enforceable.

Before external project data is sent, the Network Gateway must receive a valid policy decision and data-sharing description.

---

## 9.12 Trust Centre

### Responsibility

Stores and exposes an understandable record of significant Opure activity.

### Owns

- trust records;
- audit correlation;
- action summaries;
- approval history;
- tool activity;
- network activity references;
- patch references;
- validation evidence;
- warnings;
- overrides;
- and outcome history.

### Does Not Own

- secret values;
- provider internal reasoning;
- project file contents unless explicitly captured as safe references;
- or service operational logs.

### Required Dependencies

- Storage Service;
- Runtime identity;
- secure clock;
- Configuration Manager.

### Provided Capabilities

- `trust.record.write`
- `trust.record.read`
- `trust.search`
- `trust.export`
- `trust.link`
- `trust.redaction.validate`

### Rules

The Trust Centre must be readable and useful.

It must not become an indiscriminate dump of technical noise.

---

## 9.13 AI Router

### Responsibility

Selects and invokes AI providers through a provider-neutral interface.

### Owns

- provider registry;
- provider capabilities;
- model registry;
- model selection;
- request preparation envelope;
- context limits;
- inference routing;
- provider health;
- provider fallback logic;
- and inference usage metadata.

### Does Not Own

- project file selection;
- project knowledge;
- workflow state;
- patch application;
- permissions;
- or provider secrets.

### Required Dependencies

- Policy Engine;
- Network Gateway;
- Secrets Vault;
- Scheduler;
- Trust Centre;
- provider adapters.

### Provided Capabilities

- `ai.model.list`
- `ai.model.select`
- `ai.inference.chat`
- `ai.inference.generate`
- `ai.inference.embed`
- `ai.inference.cancel`
- `ai.provider.health`

### Rules

No major service may depend directly on a provider-specific SDK.

The AI Router must never silently move a request from a local provider to a cloud provider.

Detailed behaviour belongs to SPEC-004.

---

## 9.14 Knowledge Engine

### Responsibility

Maintains structured, project-specific engineering knowledge.

### Owns

- project-memory records;
- code-structure metadata;
- engineering decision references;
- error and fix records;
- relationship data;
- retrieval indexes;
- knowledge confidence;
- and knowledge correction history.

### Does Not Own

- source files;
- secret values;
- patch application;
- model inference;
- or workflow authority.

### Required Dependencies

- Project Manager;
- Workspace Service;
- Storage Service;
- Scheduler;
- Trust Centre;
- AI Router for optional embeddings or summarisation;
- Policy Engine.

### Provided Capabilities

- `knowledge.query`
- `knowledge.record`
- `knowledge.correct`
- `knowledge.delete`
- `knowledge.reindex`
- `knowledge.relationship.read`
- `knowledge.project.summary`

### Rules

Structured evidence remains authoritative.

Vector similarity is retrieval assistance, not proof.

Project knowledge is isolated by default.

Detailed behaviour belongs to SPEC-005.

---

## 9.15 Context Engine

### Responsibility

Builds bounded, inspectable context packages for AI and workflow tasks.

### Owns

- context selection;
- context prioritisation;
- token or size budgets;
- context provenance;
- exclusion handling;
- redaction requests;
- and context package summaries.

### Does Not Own

- raw project files;
- model selection;
- provider transmission;
- project policy;
- or secret values.

### Required Dependencies

- Knowledge Engine;
- Workspace Service;
- Policy Engine;
- secret-detection capability;
- AI Router capability metadata;
- Trust Centre.

### Provided Capabilities

- `context.build`
- `context.preview`
- `context.explain`
- `context.redact`
- `context.size.estimate`

### Rules

Every context item must have a source and reason for inclusion.

The developer must be able to inspect context proposed for external transmission.

---

## 9.16 Pattern Library

### Responsibility

Stores and retrieves reusable, evidence-backed engineering patterns.

### Owns

- pattern identity;
- pattern content;
- compatibility metadata;
- validation evidence;
- confidence status;
- reuse history;
- known limitations;
- and promotion rules.

### Does Not Own

- project source files;
- direct patch application;
- global permission to copy private code;
- or claims of bug-free software.

### Required Dependencies

- Knowledge Engine;
- Storage Service;
- Policy Engine;
- Trust Centre;
- Build Manager for validation evidence.

### Provided Capabilities

- `pattern.search`
- `pattern.read`
- `pattern.propose`
- `pattern.promote`
- `pattern.deprecate`
- `pattern.reuse.record`

### Rules

Project-specific code must not enter a global pattern library without explicit approval.

Confidence status must be evidence-based.

---

## 9.17 Workflow Engine

### Responsibility

Coordinates inspectable multi-stage engineering workflows.

### Owns

- workflow definitions;
- workflow instances;
- stage state;
- stage dependencies;
- checkpoints;
- workflow cancellation;
- workflow permissions references;
- and workflow outcomes.

### Does Not Own

- task execution infrastructure;
- model inference;
- file writes;
- build execution;
- project policy;
- or secrets.

### Required Dependencies

- Scheduler;
- Policy Engine;
- Trust Centre;
- AI Router;
- Context Engine;
- Patch Service;
- Build Manager;
- Knowledge Engine.

### Provided Capabilities

- `workflow.definition.list`
- `workflow.start`
- `workflow.pause`
- `workflow.resume`
- `workflow.cancel`
- `workflow.status`
- `workflow.stage.retry`
- `workflow.definition.clone`

### Rules

Workflow stages must call domain services through contracts.

A workflow must not gain permission merely because an earlier stage was approved.

Detailed behaviour belongs to SPEC-006.

---

## 9.18 Plugin Manager

### Responsibility

Discovers, installs, validates, enables, disables, updates and removes Opure plugins.

### Owns

- plugin registry;
- plugin manifests;
- plugin versions;
- plugin signatures and publisher metadata;
- requested capabilities;
- enabled state;
- compatibility checks;
- and plugin lifecycle.

### Does Not Own

- plugin runtime execution;
- arbitrary network access;
- project permissions;
- or plugin secrets.

### Required Dependencies

- Policy Engine;
- Network Gateway;
- Secrets Vault;
- Trust Centre;
- Storage Service;
- Plugin Host.

### Provided Capabilities

- `plugin.list`
- `plugin.install`
- `plugin.enable`
- `plugin.disable`
- `plugin.update`
- `plugin.remove`
- `plugin.permissions.read`

### Rules

Installation does not grant runtime permission.

A plugin must declare capabilities before activation.

Detailed behaviour belongs to SPEC-007.

---

## 9.19 Plugin Host

### Responsibility

Runs plugin code within an appropriate isolation boundary.

### Owns

- plugin process or worker lifecycle;
- plugin contract bridge;
- resource limits;
- crash isolation;
- plugin message validation;
- and plugin runtime diagnostics.

### Does Not Own

- plugin installation metadata;
- project policy;
- network transport;
- secret storage;
- or core service data.

### Required Dependencies

- Runtime Kernel;
- Plugin Manager;
- Policy Engine;
- Trust Centre;
- Scheduler.

### Provided Capabilities

- `plugin.runtime.start`
- `plugin.runtime.stop`
- `plugin.runtime.invoke`
- `plugin.runtime.health`
- `plugin.runtime.cancel`

### Rules

A plugin failure must not crash the core platform.

Plugin messages must be validated at the boundary.

---

## 9.20 MCP Gateway

### Responsibility

Connects Opure to Model Context Protocol servers through a controlled gateway.

### Owns

- MCP server registration;
- MCP capability discovery;
- session state;
- protocol translation;
- MCP request validation;
- MCP response validation;
- and MCP server health.

### Does Not Own

- permission decisions;
- network policy;
- secret values;
- workflow authority;
- or trust policy.

### Required Dependencies

- Policy Engine;
- Network Gateway;
- Secrets Vault;
- Trust Centre;
- Scheduler;
- Runtime Kernel.

### Provided Capabilities

- `mcp.server.list`
- `mcp.server.connect`
- `mcp.server.disconnect`
- `mcp.capability.list`
- `mcp.invoke`
- `mcp.health`

### Rules

MCP servers are external trust boundaries.

MCP capability discovery does not grant permission.

The AI must never communicate directly with an MCP server outside this gateway.

---

## 9.21 CLI Adapter Host

### Responsibility

Executes approved command-line tools through safe, structured adapters.

### Owns

- executable resolution;
- argument construction;
- process execution;
- output capture;
- cancellation;
- timeout handling;
- exit-code interpretation;
- and CLI adapter diagnostics.

### Does Not Own

- business meaning of build, Git or package commands;
- project policy;
- secrets;
- or broad shell access.

### Required Dependencies

- Policy Engine;
- Scheduler;
- Trust Centre;
- Secrets Vault where required;
- Workspace Service.

### Provided Capabilities

- `cli.adapter.list`
- `cli.execute`
- `cli.cancel`
- `cli.output.stream`
- `cli.health`

### Rules

Adapters must separate executable paths and arguments.

Unsafe shell interpolation must be avoided.

Generic unrestricted shell execution must require stronger permission than a known structured adapter.

---

## 9.22 API Connector Host

### Responsibility

Hosts structured integrations for external APIs that are not provided through MCP.

### Owns

- connector manifests;
- request and response schemas;
- connector runtime;
- connector health;
- and connector-specific translation.

### Does Not Own

- network permission;
- secret storage;
- project policy;
- or trust recording.

### Required Dependencies

- Network Gateway;
- Policy Engine;
- Secrets Vault;
- Trust Centre;
- Scheduler.

### Provided Capabilities

- `connector.list`
- `connector.invoke`
- `connector.cancel`
- `connector.health`

### Rules

Connectors must not open direct unrestricted network channels outside the Network Gateway.

---

## 9.23 Desktop Gateway

### Responsibility

Provides a stable application-facing API for the Opure desktop interface.

### Owns

- desktop session state;
- view-oriented aggregation;
- desktop command routing;
- notification subscriptions;
- and user-interface compatibility.

### Does Not Own

- domain truth;
- project files;
- workflow execution;
- security policy;
- or service databases.

### Required Dependencies

- Runtime Kernel;
- Project Manager;
- Workflow Engine;
- Trust Centre;
- other service query capabilities.

### Provided Capabilities

- `desktop.session.open`
- `desktop.project.view`
- `desktop.workflow.view`
- `desktop.trust.view`
- `desktop.notification.subscribe`
- `desktop.command.submit`

### Rules

The Desktop Gateway may aggregate data for usability.

It must not duplicate authoritative domain state.

The desktop interface should not call every internal service directly.

---

## 9.24 Notification Service

### Responsibility

Delivers local, prioritised platform notifications.

### Owns

- notification identity;
- severity;
- acknowledgement state;
- delivery channel;
- grouping;
- expiry;
- and notification history where required.

### Does Not Own

- original domain event;
- policy approval;
- workflow state;
- or operating-system notification permissions.

### Required Dependencies

- Runtime Messaging;
- Desktop Gateway;
- Trust Centre where significant;
- operating-system notification adapter.

### Provided Capabilities

- `notification.publish`
- `notification.acknowledge`
- `notification.list`
- `notification.subscribe`

### Rules

Notifications must not become the only record of significant activity.

Important events must remain available in their authoritative service or the Trust Centre.

---

# 10. Ownership Rules

## 10.1 Single Authority

Each domain concept must have one authoritative owner.

Examples:

| Domain Concept | Authoritative Service |
|---|---|
| Project identity | Project Manager |
| File contents and workspace access | Workspace Service |
| Patch state | Patch Service |
| Repository state | Repository Service |
| Build result | Build Manager |
| Policy decision | Policy Engine |
| Secret value | Secrets Vault |
| Network request record | Network Gateway |
| Trust record | Trust Centre |
| AI provider and model state | AI Router |
| Project knowledge | Knowledge Engine |
| Context package | Context Engine |
| Workflow state | Workflow Engine |
| Plugin installation state | Plugin Manager |
| MCP session state | MCP Gateway |

No second service may silently become a competing authority.

---

## 10.2 References, Not Copies

Services should exchange identifiers and projections rather than copying authoritative objects into independent stores.

A copied value must be treated as:

- a cache;
- a historical snapshot;
- a projection;
- or an explicitly versioned replica.

Its status must be clear.

---

## 10.3 No Shared Private Tables

Multiple services may use the same physical database engine initially.

They must still use separate schemas, tables or logical namespaces.

A service must not access another service's private tables.

Cross-service information must be obtained through a contract.

---

## 10.4 Shared Files

Shared files must have a declared owner.

Examples include:

- project files owned through Workspace Service;
- runtime logs owned through logging infrastructure;
- model files owned through AI-provider or model-management services;
- plugin packages owned through Plugin Manager.

Direct filesystem access to another service's managed directory is prohibited unless explicitly allowed by contract.

---

# 11. Dependency Rules

## 11.1 Dependency Direction

Dependencies must point towards stable capabilities.

High-level services may depend on lower-level abstractions.

Security services must not depend on higher-level workflow or AI reasoning.

Examples:

```text
Workflow Engine
    ├── AI Router
    ├── Context Engine
    ├── Patch Service
    ├── Build Manager
    └── Policy Engine

AI Router
    ├── Network Gateway
    ├── Secrets Vault
    ├── Policy Engine
    └── Scheduler
```

The Policy Engine must not depend on the Workflow Engine or AI Router.

---

## 11.2 Required and Optional Dependencies

Every dependency must be declared as required or optional.

A required dependency prevents readiness when unavailable.

An optional dependency causes reduced capability or degraded state.

Services must expose fallback behaviour for optional dependencies.

---

## 11.3 Dependency Cycles

Required dependency cycles are prohibited.

Optional cycles should also be avoided.

When bidirectional communication is required, services should use:

- events;
- callbacks through an interface;
- mediator services;
- or a shared lower-level contract.

---

## 11.4 Anti-Corruption Layers

Provider-specific, tool-specific and protocol-specific models must be translated at the boundary.

Examples include:

- provider responses translated into Opure AI response contracts;
- Git output translated into repository contracts;
- MCP messages translated into Opure capability contracts;
- build-tool output translated into build-result contracts.

External models must not leak through the platform.

---

# 12. Communication Model

## 12.1 Commands

Commands request a state change.

A command must have one responsible service.

Examples:

- apply a patch;
- start a workflow;
- install a plugin;
- run tests;
- update cloud policy.

Commands must produce a clear result.

---

## 12.2 Queries

Queries request information without intentionally changing domain state.

Examples:

- read project metadata;
- list available models;
- inspect patch status;
- read workflow state;
- search trust records.

Queries should be side-effect free.

---

## 12.3 Events

Events report completed or observed facts.

Examples:

- project opened;
- patch applied;
- build failed;
- plugin disabled;
- provider became unavailable.

Events may have multiple consumers.

---

## 12.4 Streams

Streams are used for ordered incremental data such as:

- model output;
- build output;
- process output;
- file-watch changes;
- workflow progress;
- and resource metrics.

Streams must support cancellation and backpressure.

---

## 12.5 Request Context

Every significant service request should carry a request context containing:

- runtime instance;
- request identifier;
- correlation identifier;
- causation identifier;
- project identifier;
- initiating user;
- initiating service;
- workflow identifier where applicable;
- approval reference;
- permission context;
- deadline;
- cancellation token;
- and trust-record correlation.

---

## 12.6 Contract Envelopes

A service contract envelope should contain:

- contract name;
- contract version;
- operation;
- request context;
- payload;
- validation metadata;
- and optional tracing metadata.

Payloads must be schema validated at service boundaries.

---

# 13. Synchronous and Asynchronous Interaction

## 13.1 Synchronous Requests

Synchronous communication is appropriate when:

- the caller needs an immediate result;
- the operation is short;
- and failure must be returned directly.

Examples include:

- reading project metadata;
- validating a policy;
- reading service health.

---

## 13.2 Asynchronous Tasks

Asynchronous execution is appropriate when:

- work is long-running;
- work can be cancelled;
- output is streamed;
- progress must be observed;
- or work should survive a temporary desktop disconnection.

Examples include:

- model inference;
- builds;
- tests;
- indexing;
- plugin installation;
- and project analysis.

---

## 13.3 Task Handles

Long-running operations must return a task handle.

A task handle should allow:

- status query;
- progress subscription;
- cancellation;
- result retrieval;
- error retrieval;
- and Trust Centre navigation.

---

## 13.4 Timeouts

Every cross-service request should have an explicit timeout or deadline policy.

A timeout must not imply that the operation definitely stopped.

The caller must be able to query final state where required.

---

# 14. Reliability Rules

## 14.1 Idempotency

State-changing operations should be idempotent where practical.

An idempotency key should be used for operations at risk of duplicate delivery.

Examples include:

- plugin installation;
- patch application;
- workflow start;
- and external API mutation.

---

## 14.2 Retries

Retries must be bounded and appropriate to the operation.

A service must not retry a non-idempotent external action unless duplicate effects are prevented.

Retry policy should include:

- retry count;
- delay;
- backoff;
- jitter where useful;
- retryable error categories;
- and final failure behaviour.

---

## 14.3 Circuit Breakers

External dependencies should use circuit breakers.

A circuit breaker should expose:

- current state;
- last failure;
- failure count;
- next retry time;
- and manual reset where appropriate.

---

## 14.4 Bulkheads

Resource-heavy or unreliable workloads should be isolated into separate task pools or processes.

Examples include:

- AI inference;
- builds;
- tests;
- plugin execution;
- MCP servers;
- browser automation;
- and package-manager operations.

---

## 14.5 Graceful Degradation

A service must declare how it behaves when optional dependencies are unavailable.

Examples:

- AI Router unavailable: project browsing and builds remain available.
- Knowledge Engine rebuilding: direct file operations remain available.
- Git unavailable: patches still function without repository operations.
- MCP server unavailable: unrelated integrations remain available.
- cloud provider unavailable: local providers remain selectable.

---

# 15. Transactions and Consistency

## 15.1 Local Transactions

A service may use local storage transactions within its owned boundary.

Cross-service database transactions are prohibited.

---

## 15.2 Cross-Service Workflows

Cross-service changes must use a coordinated workflow, saga or compensation pattern.

Example patch-and-commit flow:

1. Patch Service validates patch.
2. Policy Engine approves application.
3. Workspace Service creates snapshot.
4. Patch Service applies patch.
5. Build Manager validates.
6. Repository Service stages and commits if requested.
7. Trust Centre records outcome.

If a later step fails, compensating actions may:

- restore the snapshot;
- unstage changes;
- leave the patch applied but uncommitted;
- or request developer intervention.

The outcome must be explicit.

---

## 15.3 Eventual Consistency

Read models, indexes and projections may be eventually consistent.

The interface must not present a projection as fully current when it is known to lag.

---

## 15.4 Optimistic Concurrency

Services should use version identifiers or hashes when updating shared domain concepts through contracts.

A stale update must be rejected or reconciled explicitly.

---

# 16. Security Propagation

## 16.1 No Ambient Authority

A service must not receive broad authority merely because it is running inside Opure.

Every protected action must resolve an explicit permission context.

---

## 16.2 Capability Grants

Capabilities should be granted by:

- user;
- project;
- service;
- plugin;
- workflow;
- action;
- destination;
- data type;
- and time limit.

Capabilities must be revocable.

---

## 16.3 Approval Binding

An approval must bind to the action that was shown to the developer.

If the action changes materially, approval must be requested again.

Examples of material change include:

- new files;
- new network destination;
- more data shared;
- different command;
- different provider;
- broader secret access;
- or irreversible behaviour.

---

## 16.4 Secret References

Service contracts should pass secret identifiers rather than secret values.

The receiving boundary may resolve the value only when authorised and necessary.

---

## 16.5 Data Classification

Data passed between services should carry classification where relevant:

- Public;
- Project Internal;
- Sensitive;
- Secret;
- Restricted External Sharing.

The exact classification system is DEFERRED to SPEC-008.

---

# 17. Observability

## 17.1 Correlation

All significant cross-service activity must be traceable through correlation identifiers.

A developer should be able to move from:

- a workflow;
- to its AI request;
- to its tool calls;
- to its patch;
- to its build;
- to its commit;
- and to the final Trust Centre record.

---

## 17.2 Logs, Metrics and Trust Records

These concepts must remain distinct:

- **Logs** describe operational details for diagnosis.
- **Metrics** describe measured system behaviour.
- **Trust records** describe meaningful actions affecting the developer or project.

Not every log entry belongs in the Trust Centre.

Not every Trust Centre record requires verbose logs.

---

## 17.3 Health

Every service must expose a health contract.

Health must distinguish:

- liveness;
- readiness;
- dependency state;
- degradation;
- and recent failure.

---

## 17.4 Resource Metrics

Resource-heavy services should expose:

- queue length;
- active tasks;
- CPU use;
- memory use;
- GPU or VRAM use where relevant;
- process state;
- and task latency.

---

# 18. Deployment Topology

## 18.1 Initial Topology

The initial Opure implementation should use a modular local topology.

A likely first topology is:

```text
Desktop Process
        │
        ▼
Core Runtime Process
        ├── Runtime Kernel
        ├── Project Manager
        ├── Policy Engine
        ├── Trust Centre
        ├── Workflow Engine
        └── Other trusted services

Isolated Worker Processes
        ├── AI provider runtimes
        ├── Plugin hosts
        ├── MCP servers
        ├── Builds and tests
        └── External CLI tools
```

This topology is illustrative, not mandatory.

---

## 18.2 Modular Monolith Principle

The trusted core may initially be implemented as a modular monolith.

This is acceptable only if:

- service boundaries remain explicit;
- private data remains isolated;
- contracts are used internally;
- tests can run per service;
- and future process separation remains possible.

A modular monolith must not become an excuse for unrestricted internal calls.

---

## 18.3 Process Separation Criteria

A service or worker should move into a separate process when one or more of the following apply:

- it runs untrusted third-party code;
- it may crash independently;
- it consumes substantial memory or GPU resources;
- it requires a different runtime;
- it needs stronger operating-system restrictions;
- it benefits from independent restart;
- or it executes external code.

---

## 18.4 Remote Services

Remote service deployment is not required for the initial product.

If remote services are introduced later, they must preserve:

- developer visibility;
- explicit cloud policy;
- authentication;
- encryption;
- contract compatibility;
- and local fallback where required by the Charter.

---

# 19. Desktop Integration

## 19.1 Desktop Gateway Requirement

The desktop application should communicate primarily through the Desktop Gateway.

This provides:

- stable desktop-facing contracts;
- reduced coupling;
- aggregated views;
- consistent error handling;
- and easier future desktop replacement.

---

## 19.2 No Domain Logic in Views

Desktop components must not become authoritative owners of:

- project state;
- workflow state;
- permissions;
- patches;
- provider state;
- or Trust Centre history.

Views may cache display data temporarily.

The underlying service remains authoritative.

---

## 19.3 Disconnection Handling

If the desktop process restarts or disconnects, long-running runtime tasks should continue where safe.

The desktop should reconnect using task and workflow identifiers.

---

# 20. Service Contract Standards

## 20.1 Contract Naming

Contracts should use stable domain-oriented names.

Examples:

- `ProjectOpened`
- `ApplyPatchCommand`
- `EvaluatePolicyQuery`
- `BuildCompleted`
- `ProviderHealthChanged`

Names should not include implementation technology.

---

## 20.2 Schema Requirements

Contracts must define:

- required fields;
- optional fields;
- default behaviour;
- validation;
- maximum sizes where relevant;
- sensitivity;
- and compatibility rules.

---

## 20.3 Backwards Compatibility

Adding an optional field may be backwards compatible.

Removing or changing the meaning of a required field is breaking.

Breaking changes require a new major contract version.

---

## 20.4 Unknown Fields

Receivers should ignore unknown optional fields where safe.

Unknown security-sensitive fields must not be ignored blindly.

---

## 20.5 Error Contracts

Service errors should include:

- stable error code;
- safe human-readable message;
- category;
- retryability;
- correlation identifier;
- affected resource;
- and optional recovery guidance.

Errors must not expose secrets.

---

## 20.6 Pagination

List and search contracts should support pagination.

Pagination must use stable cursors where result sets may change.

---

## 20.7 Streaming Contracts

Streams should define:

- sequence number;
- stream identifier;
- completion state;
- cancellation behaviour;
- error behaviour;
- and backpressure.

---

# 21. Service Discovery

## 21.1 Registry

The Runtime Service Registry is authoritative for available services and capabilities.

A service must not assume another service exists merely because its package is installed.

---

## 21.2 Capability Resolution

A service should request a capability and compatible contract version.

The registry may resolve that capability to:

- a built-in service;
- a plugin;
- an adapter;
- or another approved provider.

Resolution must respect policy.

---

## 21.3 Dynamic Availability

Services may appear or disappear during runtime.

Clients must handle:

- provider disconnection;
- plugin disablement;
- MCP server loss;
- and worker restart.

---

# 22. Caching

## 22.1 Cache Ownership

A cache is owned by the service that creates it.

A cache must not become an undocumented source of truth.

---

## 22.2 Invalidation

Every cache must define:

- invalidation trigger;
- maximum age;
- consistency expectation;
- and fallback behaviour.

---

## 22.3 Sensitive Data

Secret values must not be placed in ordinary caches.

Sensitive project content should use bounded, protected caches.

---

# 23. Data Retention

Each service must define retention for its owned data.

Retention rules should cover:

- active records;
- historical records;
- logs;
- temporary files;
- projections;
- caches;
- backups;
- and deleted-project cleanup.

The developer must be able to remove project-owned data.

Retention must not silently conflict with cloud or privacy policy.

---

# 24. Testing Strategy

## 24.1 Contract Tests

Every service contract must have provider and consumer contract tests.

Contract tests should validate:

- schema;
- compatibility;
- error behaviour;
- timeouts;
- cancellation;
- and security metadata.

---

## 24.2 Service Unit Tests

Each service must test its domain rules independently from transport and storage.

---

## 24.3 Integration Tests

Integration tests should cover key service chains.

Examples:

- project open;
- context build;
- AI request;
- patch generation;
- patch validation;
- build;
- approval;
- patch apply;
- and Trust Centre recording.

---

## 24.4 Boundary Tests

Security boundaries must be tested for:

- permission denial;
- secret redaction;
- network blocking;
- path traversal;
- malformed plugin messages;
- invalid MCP responses;
- and stale approvals.

---

## 24.5 Failure Tests

Tests should simulate:

- service crash;
- timeout;
- duplicate message;
- delayed event;
- unavailable optional dependency;
- corrupted response;
- and desktop disconnection.

---

## 24.6 Architecture Tests

Automated architecture tests should enforce:

- prohibited dependencies;
- service namespace boundaries;
- contract-only cross-service access;
- no private database access;
- and no direct provider SDK use outside adapters.

---

# 25. Initial Service Dependency Map

The following map defines intended high-level dependencies.

```text
Desktop Gateway
    ├── Runtime Kernel
    ├── Project Manager
    ├── Workflow Engine
    ├── Trust Centre
    └── Notification Service

Project Manager
    ├── Workspace Service
    ├── Policy Engine
    ├── Storage Service
    └── Trust Centre

Workflow Engine
    ├── Scheduler
    ├── Policy Engine
    ├── Context Engine
    ├── AI Router
    ├── Patch Service
    ├── Build Manager
    ├── Knowledge Engine
    └── Trust Centre

Context Engine
    ├── Knowledge Engine
    ├── Workspace Service
    ├── Policy Engine
    └── Trust Centre

AI Router
    ├── Policy Engine
    ├── Network Gateway
    ├── Secrets Vault
    ├── Scheduler
    └── Trust Centre

Patch Service
    ├── Workspace Service
    ├── Policy Engine
    ├── Repository Service
    └── Trust Centre

Build Manager
    ├── Workspace Service
    ├── CLI Adapter Host
    ├── Policy Engine
    ├── Scheduler
    └── Trust Centre

Plugin Manager
    ├── Plugin Host
    ├── Policy Engine
    ├── Network Gateway
    ├── Secrets Vault
    └── Trust Centre

MCP Gateway
    ├── Policy Engine
    ├── Network Gateway
    ├── Secrets Vault
    ├── Scheduler
    └── Trust Centre
```

This map is normative at the responsibility level, not at the implementation-import level.

---

# 26. Prohibited Coupling

The following are prohibited:

- Workflow Engine writing project files directly.
- AI Router reading arbitrary project files directly.
- Desktop UI applying patches directly.
- Plugins reading the Secrets Vault storage directly.
- MCP servers opening unrestricted network connections through Opure.
- Knowledge Engine editing source files.
- Trust Centre becoming the authority for workflow or patch state.
- Project Manager owning Git operations.
- Build Manager changing source files outside approved build artefact rules.
- Provider adapters bypassing the AI Router.
- Services bypassing the Policy Engine for protected actions.
- Services bypassing the Network Gateway for protected external data sharing.
- Any service storing secret values in normal storage.
- Cross-service database joins against private tables.
- Hidden desktop-only state that changes platform behaviour without service acknowledgement.

---

# 27. Cross-Cutting Services

Some capabilities affect every service.

These must remain cross-cutting without becoming uncontrolled dependencies.

## 27.1 Policy

Protected operations call the Policy Engine through a stable interface.

## 27.2 Trust

Significant actions write Trust Centre records or use the runtime audit buffer.

## 27.3 Configuration

Services receive validated configuration through Configuration Manager.

## 27.4 Scheduling

Long-running or resource-heavy work uses Scheduler infrastructure.

## 27.5 Health

Every service reports health through the Runtime Kernel contract.

## 27.6 Correlation

Every significant operation carries correlation identifiers.

## 27.7 Cancellation

Long-running operations support cancellation signals.

---

# 28. Initial Implementation Phases

## Phase 1 — Contract Skeleton

Implement:

- service identifiers;
- service registry;
- contract envelopes;
- request context;
- health contracts;
- error contracts;
- mock services;
- and architecture tests.

## Phase 2 — Core Project Services

Implement:

- Project Manager;
- Workspace Service;
- Storage Service;
- Policy Engine;
- Trust Centre;
- and Desktop Gateway skeleton.

## Phase 3 — Engineering Operations

Implement:

- Patch Service;
- Repository Service;
- Build Manager;
- CLI Adapter Host;
- and Scheduler integration.

## Phase 4 — Intelligence Services

Implement:

- AI Router;
- Knowledge Engine;
- Context Engine;
- Workflow Engine;
- and Pattern Library foundations.

## Phase 5 — Integration Services

Implement:

- Plugin Manager;
- Plugin Host;
- MCP Gateway;
- API Connector Host;
- and Network Gateway expansion.

Each phase must retain clear service ownership.

---

# 29. Acceptance Criteria

SPEC-003 is implemented when:

- [ ] Every major service has a documented responsibility.
- [ ] Every domain concept has one authoritative owner.
- [ ] Cross-service communication uses versioned contracts.
- [ ] Required dependencies are acyclic.
- [ ] Optional dependency failure causes visible degradation rather than hidden failure.
- [ ] No service reads another service's private storage.
- [ ] Protected operations invoke Policy Engine evaluation.
- [ ] External communication passes through Network Gateway controls.
- [ ] Secret access uses secret references and the Secrets Vault.
- [ ] AI providers are accessible only through the AI Router.
- [ ] MCP servers are accessible only through the MCP Gateway.
- [ ] Plugins execute through the Plugin Host.
- [ ] Long-running operations return task handles and support cancellation.
- [ ] Significant operations can be correlated across services.
- [ ] Service health is visible through the Runtime Kernel.
- [ ] Desktop presentation does not own domain truth.
- [ ] Architecture tests detect prohibited dependencies.
- [ ] Contract compatibility tests pass.
- [ ] Failure-isolation tests pass.
- [ ] The platform can run as a modular local system without requiring cloud services.

---

# 30. Deferred Decisions

The following are intentionally deferred:

- implementation language;
- code package layout;
- dependency-injection framework;
- serialisation format;
- event-bus implementation;
- local IPC technology;
- database engine;
- process boundaries;
- plugin sandbox technology;
- MCP transport implementation;
- workflow-definition language;
- schema registry technology;
- metrics backend;
- and distributed deployment support.

These decisions must not weaken the boundaries defined in this document.

---

# 31. Required Architecture Decision Records

Implementation should produce ADRs for:

- modular monolith versus multi-process core;
- contract serialisation format;
- local message transport;
- service discovery mechanism;
- service package and namespace structure;
- storage namespace isolation;
- API error format;
- task-handle design;
- plugin-host isolation;
- MCP process and transport model;
- and desktop gateway transport.

---

# 32. Relationship to Later Specifications

This specification provides service boundaries for:

- **SPEC-004 — AI Router**
- **SPEC-005 — Memory Engine**
- **SPEC-006 — Workflow and Agent System**
- **SPEC-007 — Plugin SDK**
- **SPEC-008 — Trust Centre and Security**
- **SPEC-009 — Workspace and File Patch Engine**
- **SPEC-010 — Desktop User Interface**
- **SPEC-011 — Project and Build Management**

Later specifications may add internal components within a service boundary.

They must not move authoritative ownership between services without an approved change to this specification.

---

# 33. Founder Approval

This document remains a founder draft until explicitly approved.

Approval establishes that Opure will be built as a collection of clearly bounded, contract-driven services that remain replaceable, testable and accountable.

The service architecture must preserve the founding commitments:

- Developer Respect;
- Software Engineering First;
- Local by Design;
- Human in Control;
- Visible by Design;
- Open by Architecture;
- and Loose Coupling by Design.

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**