# SPEC-007 — Plugin SDK

## Opure Platform Extensibility, Capability Contracts and Safe Plugin Execution

**Document:** SPEC-007  
**Status:** Founder Draft  
**Version:** 0.1  
**Language:** British English  
**Last updated:** 18 July 2026  
**Depends on:** CHARTER-001, SPEC-001, SPEC-002, SPEC-003, SPEC-004, SPEC-005, SPEC-006  

---

## 1. Purpose

This specification defines the Plugin Software Development Kit for the Opure Platform.

The Plugin SDK allows third-party and first-party extensions to add capabilities without weakening:

- developer control;
- project isolation;
- local-first operation;
- security policy;
- service boundaries;
- provider neutrality;
- auditability;
- recoverability;
- and performance discipline.

The Plugin SDK must define:

- plugin identity;
- package structure;
- manifests;
- versioning;
- compatibility;
- permissions;
- capabilities;
- lifecycle;
- isolation;
- service contracts;
- workflow contributions;
- user-interface contributions;
- data storage;
- secrets access;
- network access;
- project access;
- diagnostics;
- testing;
- signing;
- installation;
- update;
- disablement;
- and removal.

The SDK must support meaningful extension while preventing plugins from becoming hidden privileged code.

---

## 2. Founding Rule

> **A plugin may extend Opure, but it may not bypass Opure.**

A plugin must not:

- read arbitrary project files directly;
- write arbitrary project files directly;
- access secret values without approval;
- create unrestricted network connections;
- bypass the Policy Engine;
- bypass the Network Gateway;
- bypass the Trust Centre;
- invoke AI providers directly;
- call MCP servers directly;
- change runtime policy;
- grant itself permissions;
- or treat installation as permanent trust.

Every significant plugin action must remain mediated through Opure capability contracts.

---

## 3. Relationship to Other Services

A simplified logical view is:

```text
Plugin Developer
        │
        ▼
    Plugin SDK
        │
        ├── Manifest Schema
        ├── Contract Libraries
        ├── Capability APIs
        ├── Workflow APIs
        ├── UI Contribution APIs
        ├── Test Harness
        ├── Packaging Tools
        ├── Signing Tools
        └── Validation Tools
                │
                ▼
       Plugin Manager
                │
                ▼
          Plugin Host
                │
                ├── Policy Engine
                ├── Network Gateway
                ├── Secrets Vault
                ├── Trust Centre
                ├── Scheduler
                ├── Workspace Service
                ├── Patch Service
                ├── AI Router
                ├── Knowledge Engine
                ├── Workflow Engine
                ├── MCP Gateway
                └── Desktop Gateway
```

The Plugin Manager owns plugin installation state.

The Plugin Host owns plugin runtime execution.

The Plugin SDK defines the development and contract surface used by plugin authors.

---

## 4. Design Goals

The Plugin SDK must be:

- secure by default;
- capability-based;
- contract-driven;
- versioned;
- language-flexible where practical;
- testable;
- diagnosable;
- local-first;
- replaceable;
- and understandable.

It should make safe plugins easy to build.

It must make hidden privilege escalation difficult.

---

## 5. Non-Goals

The Plugin SDK is not responsible for:

- granting unrestricted operating-system access;
- replacing the Runtime Kernel;
- replacing MCP;
- providing a general application container;
- guaranteeing trust based only on a signature;
- allowing arbitrary provider SDK use;
- bypassing Opure service ownership;
- or hiding third-party behaviour behind a friendly interface.

A plugin may integrate external technology.

It must do so through declared and mediated capabilities.

---

## 6. Normative Language

The terms **MUST**, **MUST NOT**, **SHOULD**, **SHOULD NOT**, **MAY** and **DEFERRED** have the meanings defined in SPEC-001.

Any intentional violation of a **SHOULD** requirement must be documented in an Architecture Decision Record.

---

# 7. Core Concepts

## 7.1 Plugin

A Plugin is a versioned extension package installed through the Plugin Manager and executed through the Plugin Host.

A plugin may contribute:

- capabilities;
- commands;
- queries;
- workflows;
- workflow stages;
- views;
- panels;
- settings;
- parsers;
- adapters;
- validators;
- exporters;
- importers;
- notifications;
- and developer tools.

---

## 7.2 Plugin Package

A Plugin Package is the distributable artefact containing:

- manifest;
- executable code;
- contract metadata;
- assets;
- schemas;
- documentation;
- licence information;
- integrity metadata;
- and optional signature.

---

## 7.3 Plugin Manifest

The Plugin Manifest is the declarative description of a plugin.

It must describe:

- identity;
- version;
- compatibility;
- entry points;
- requested capabilities;
- provided capabilities;
- settings;
- data access;
- network access;
- secrets access;
- workflow contributions;
- UI contributions;
- lifecycle hooks;
- and package integrity information.

---

## 7.4 Capability

A Capability is a versioned operation or contract that a plugin may consume or provide.

Capabilities must be explicit.

They must not be inferred from implementation details.

---

## 7.5 Permission

A Permission is a policy-controlled grant allowing a plugin to perform a protected action.

Installation does not grant all requested permissions.

---

## 7.6 Plugin Host

The Plugin Host is the controlled runtime boundary in which plugin code executes.

---

## 7.7 Plugin Manager

The Plugin Manager installs, validates, enables, disables, updates and removes plugins.

---

## 7.8 Contribution

A Contribution is a declarative extension to Opure behaviour, such as:

- a command;
- a workflow;
- a parser;
- a view;
- or a validation provider.

---

# 8. Plugin Categories

## 8.1 Capability Plugin

Provides one or more reusable capabilities.

Examples:

- package metadata lookup;
- code metric analysis;
- project export;
- diagram generation;
- static analysis;
- and custom validation.

---

## 8.2 Workflow Plugin

Provides workflow definitions, workflow stages or role templates.

---

## 8.3 Provider Adapter Plugin

Provides an AI provider adapter.

Provider adapter plugins must still operate through the AI Router.

---

## 8.4 Parser Plugin

Provides language or format parsing for the Memory Engine.

---

## 8.5 Build Adapter Plugin

Provides build-system or test-runner integration through the Build Manager.

---

## 8.6 Repository Adapter Plugin

Provides version-control integration through the Repository Service.

---

## 8.7 UI Extension Plugin

Provides desktop panels, views, commands or contextual actions.

UI extensions must not own authoritative domain state.

---

## 8.8 Connector Plugin

Provides integration with an external API or service through the API Connector Host and Network Gateway.

---

## 8.9 MCP Bridge Plugin

Adds Opure-specific translation or convenience around MCP capabilities.

It must not bypass the MCP Gateway.

---

## 8.10 Knowledge Plugin

Provides import, export, enrichment or specialist retrieval for the Knowledge Engine.

---

# 9. Plugin Trust Classes

## 9.1 Built-In

Distributed as part of the trusted Opure installation.

Built-In status does not remove the requirement for clear capabilities and contracts.

---

## 9.2 First-Party Optional

Published by the Opure project but installed separately.

---

## 9.3 Verified Third-Party

Published by a third party and verified through whatever future verification process Opure adopts.

Verification does not imply unlimited trust.

---

## 9.4 Signed Third-Party

Cryptographically signed by a known publisher.

A signature verifies package origin and integrity.

It does not prove safety or quality.

---

## 9.5 Unsigned Local

Loaded from a developer-controlled local package.

Unsigned Local plugins require stronger warnings and may default to Safe Mode restrictions.

---

## 9.6 Development Plugin

Loaded in development mode.

Development plugins should be clearly marked and should not be confused with installed production plugins.

---

# 10. Plugin Identity

## 10.1 Identifier

Every plugin must have a globally unique identifier.

Recommended form:

```text
publisher.plugin-name
```

Example:

```text
opure.git-tools
```

---

## 10.2 Display Name

The display name may be localised.

It must not replace the stable identifier.

---

## 10.3 Publisher Identity

The manifest should include:

- publisher identifier;
- publisher display name;
- website where available;
- support information;
- and signing identity where applicable.

---

## 10.4 Version

Plugin versions must follow semantic versioning or another documented compatible scheme.

---

## 10.5 Package Digest

Every installed package must have a cryptographic digest.

---

# 11. Manifest Requirements

## 11.1 Required Manifest Fields

The manifest must include:

- schema version;
- plugin identifier;
- plugin version;
- display name;
- description;
- publisher;
- licence;
- minimum Opure version;
- supported contract versions;
- plugin categories;
- entry points;
- requested capabilities;
- provided capabilities;
- runtime requirements;
- and package digest.

---

## 11.2 Optional Manifest Fields

Optional fields may include:

- homepage;
- source repository;
- issue tracker;
- documentation;
- icons;
- screenshots;
- localisation;
- release notes;
- migration metadata;
- and support policy.

---

## 11.3 Manifest Validation

The Plugin Manager must validate the manifest before installation.

Validation must include:

- schema validity;
- identifier validity;
- version validity;
- compatibility;
- capability names;
- permission declarations;
- entry-point existence;
- integrity metadata;
- and prohibited fields.

---

## 11.4 Unknown Fields

Unknown optional fields may be preserved for forward compatibility.

Unknown security-sensitive fields must not be ignored silently.

---

# 12. Example Manifest

An illustrative manifest may resemble:

```yaml
schemaVersion: 1
id: example.code-metrics
version: 1.2.0
name: Code Metrics
publisher:
  id: example
  name: Example Tools
licence: MIT
opure:
  minimumVersion: 0.1.0
  contracts:
    plugin: 1
categories:
  - capability
  - ui-extension
entryPoints:
  runtime: dist/plugin
capabilities:
  consumes:
    - workspace.file.read
    - knowledge.query
    - trust.record.write
  provides:
    - metrics.project.calculate
permissions:
  projectRead:
    scope: selected-files
  projectWrite:
    requested: false
  network:
    requested: false
  secrets:
    requested: false
contributions:
  commands:
    - id: example.code-metrics.run
      title: Calculate Project Metrics
  views:
    - id: example.code-metrics.panel
      location: project-tools
```

This example is informative.

The final schema is DEFERRED to an ADR.

---

# 13. Package Structure

## 13.1 Required Files

A plugin package should contain:

```text
plugin-package/
├── opure-plugin.yaml
├── dist/
├── schemas/
├── assets/
├── docs/
├── LICENSE
└── integrity.json
```

---

## 13.2 Executable Content

Executable content must remain inside declared package paths.

---

## 13.3 Native Libraries

Native libraries require declaration.

They may require stronger isolation and platform compatibility checks.

---

## 13.4 Scripts

Installation scripts are prohibited by default.

If supported later, they must be:

- declared;
- visible;
- sandboxed;
- policy-controlled;
- and auditable.

---

## 13.5 Package Size

The package manifest should declare expected installed size.

Large optional assets should be downloadable separately where practical.

---

# 14. Capability Model

## 14.1 Capability Naming

Capabilities should use stable domain names.

Examples:

- `workspace.file.read`
- `patch.create`
- `build.execute`
- `knowledge.query`
- `ai.inference.generate`
- `network.request`
- `plugin.storage.read`
- `plugin.storage.write`

---

## 14.2 Consumed Capabilities

A plugin must declare capabilities it consumes.

---

## 14.3 Provided Capabilities

A plugin must declare capabilities it provides.

---

## 14.4 Capability Versions

Capability contracts must be versioned independently from plugin versions.

---

## 14.5 Capability Discovery

Plugins may discover optional capabilities through the Service Registry.

They must handle absence or incompatibility.

---

## 14.6 No Undeclared Use

A plugin must not invoke undeclared protected capabilities.

---

# 15. Permission Model

## 15.1 Installation Versus Permission

Installing a plugin does not grant all requested permissions.

The developer may:

- approve;
- deny;
- narrow;
- defer;
- or revoke permissions.

---

## 15.2 Permission Scope

Permissions should support scope by:

- project;
- workspace;
- file pattern;
- capability;
- network destination;
- data classification;
- secret identifier;
- workflow;
- session;
- and time.

---

## 15.3 Read Permissions

Project read permissions may include:

- selected file;
- selected directory;
- workspace metadata;
- project summary;
- repository status;
- and knowledge query.

---

## 15.4 Write Permissions

Project write permission must occur through the Patch Service.

A plugin should not receive raw unrestricted filesystem write access.

---

## 15.5 Command Permissions

Command execution must occur through:

- Build Manager;
- CLI Adapter Host;
- Repository Service;
- or another controlled service.

---

## 15.6 Network Permissions

Network access must specify:

- destination;
- protocol;
- purpose;
- data category;
- and whether project data may be transmitted.

---

## 15.7 Secret Permissions

Secret access must specify:

- secret reference type;
- purpose;
- project;
- destination;
- and duration.

---

## 15.8 Permission Revocation

Permissions must be revocable while the plugin remains installed.

---

# 16. Least Privilege

## 16.1 Default Deny

Undeclared or ungranted capabilities must be denied.

---

## 16.2 Dynamic Grants

A plugin may request an additional permission at runtime.

The request must explain:

- action;
- scope;
- reason;
- affected data;
- duration;
- and consequence of denial.

---

## 16.3 No Permission Bundling

Unrelated permissions should not be bundled unnecessarily.

---

## 16.4 Narrow Defaults

A plugin requesting project access should default to the narrowest practical scope.

---

# 17. Plugin Lifecycle

## 17.1 Lifecycle States

A plugin should use:

- discovered;
- validating;
- installed;
- disabled;
- enabling;
- enabled;
- degraded;
- updating;
- migration required;
- failed;
- quarantined;
- removing;
- and removed.

---

## 17.2 Installation

Installation must include:

1. package acquisition;
2. integrity verification;
3. signature inspection;
4. manifest validation;
5. compatibility check;
6. dependency check;
7. permission review;
8. safe extraction;
9. registration;
10. optional migration;
11. disabled initial state where required;
12. and Trust Centre recording.

---

## 17.3 Enablement

Enablement must:

- validate current compatibility;
- validate permissions;
- create a Plugin Host instance;
- register contributions;
- perform health checks;
- and expose runtime state.

---

## 17.4 Disablement

Disablement must:

- stop new plugin work;
- cancel or finish active tasks safely;
- unregister contributions;
- release resources;
- and preserve plugin data unless removal is requested.

---

## 17.5 Update

Update must:

- validate the new package;
- compare permissions;
- show new or broadened permissions;
- stop the old version safely;
- migrate data where required;
- start the new version;
- verify health;
- and support rollback where technically practical.

---

## 17.6 Removal

Removal must identify:

- plugin data;
- workflow definitions;
- settings;
- cached artefacts;
- secrets references;
- dependent workflows;
- and retained Trust Centre records.

---

# 18. Safe Update Rules

## 18.1 Permission Changes

An update that requests new permissions must require review.

---

## 18.2 Contract Changes

Breaking provided-capability changes require a major version and migration guidance.

---

## 18.3 Failed Update

A failed update should restore the previous working version where possible.

---

## 18.4 Rollback

Rollback must not silently reuse incompatible migrated data.

---

# 19. Plugin Host Isolation

## 19.1 Process Boundary

Third-party plugins should run outside the trusted core process.

---

## 19.2 Isolation Goals

Isolation should limit:

- filesystem access;
- network access;
- process creation;
- environment access;
- secret access;
- memory impact;
- CPU impact;
- and crash propagation.

---

## 19.3 Runtime Options

Plugin runtimes may include:

- managed process host;
- language-specific worker;
- WebAssembly runtime;
- restricted native process;
- or another isolated host.

Exact technologies are DEFERRED.

---

## 19.4 Crash Isolation

A plugin crash must not crash the Runtime Kernel.

---

## 19.5 Resource Limits

Plugin hosts should support:

- memory limits;
- CPU limits;
- task concurrency;
- output size limits;
- execution timeouts;
- and process count limits.

---

## 19.6 Host Reuse

Multiple plugins may share a host only when:

- trust class;
- runtime;
- permissions;
- and isolation requirements are compatible.

---

# 20. Plugin Runtime Contract

## 20.1 Startup

At startup, the Plugin Host should provide:

- plugin identity;
- plugin version;
- runtime context;
- granted capabilities;
- configuration;
- project context where applicable;
- cancellation;
- and logging interface.

---

## 20.2 Readiness

A plugin must not be marked ready until:

- entry point started;
- contract negotiation succeeded;
- required capabilities resolved;
- and health check passed.

---

## 20.3 Health

A plugin should expose:

- liveness;
- readiness;
- degraded features;
- dependency state;
- and recent error.

---

## 20.4 Shutdown

A plugin must support graceful shutdown.

---

## 20.5 Heartbeat

Long-running plugin hosts may use heartbeats.

Missed heartbeats must not immediately imply safe termination without verification.

---

# 21. Data Storage

## 21.1 Private Plugin Storage

Each plugin should receive isolated storage.

---

## 21.2 Storage Types

The SDK may expose:

- key-value storage;
- document storage;
- structured records;
- temporary storage;
- and cache storage.

---

## 21.3 No Shared Private Tables

A plugin must not access another plugin's private storage.

---

## 21.4 Project-Scoped Storage

Project-specific plugin data must be namespaced by project.

---

## 21.5 Sensitive Data

Secret values must not be stored in plugin storage.

---

## 21.6 Retention

The plugin manifest should declare retention behaviour.

---

## 21.7 Removal

Removal should offer:

- keep plugin data;
- export plugin data;
- or delete plugin data.

---

# 22. Configuration and Settings

## 22.1 Settings Schema

Plugins must declare a versioned settings schema.

---

## 22.2 Settings Scope

Settings may be:

- global;
- user;
- project;
- workspace;
- or session.

---

## 22.3 Sensitive Settings

Sensitive values must use Secrets Vault references.

---

## 22.4 Validation

Settings must be validated before activation.

---

## 22.5 Restart Requirement

Settings should identify whether a plugin restart is required.

---

# 23. Secrets Access

## 23.1 Secret References

Plugins must request secret references, not raw configuration values.

---

## 23.2 Access Mediation

Secret access must pass through:

- Policy Engine;
- Secrets Vault;
- and Trust Centre.

---

## 23.3 Destination Binding

Where a secret is used for an external destination, access should be bound to that destination.

---

## 23.4 Secret Non-Disclosure

Plugins must not:

- log secret values;
- store them;
- return them to the desktop;
- include them in Trust Centre records;
- or include them in AI prompts.

---

# 24. Network Access

## 24.1 Gateway Requirement

Plugin network traffic must pass through the Network Gateway where enforceable.

---

## 24.2 Declared Destinations

The manifest should declare expected destinations.

---

## 24.3 Dynamic Destinations

Dynamic destinations require stronger permission and inspection.

---

## 24.4 Data-Sharing Preview

Before external project data is transmitted, Opure must show:

- plugin;
- destination;
- purpose;
- data categories;
- approximate size;
- and project policy result.

---

## 24.5 Direct Sockets

Direct unrestricted sockets are prohibited by default.

---

# 25. Project and Workspace Access

## 25.1 Workspace Reads

Plugins read files through the Workspace Service.

---

## 25.2 File Selection

The permission may be limited to:

- current file;
- selected files;
- selected directory;
- matching patterns;
- or whole project.

---

## 25.3 Workspace Writes

Plugins propose changes through the Patch Service.

---

## 25.4 File Watch

Plugins may subscribe to file changes through Workspace Service contracts.

---

## 25.5 Path Safety

Plugins must not receive unsafe raw path authority.

---

# 26. Patch Contributions

## 26.1 Patch Creation

A plugin may create a patch proposal.

---

## 26.2 Patch Validation

Plugin patches must pass the same validation as AI-generated or built-in patches.

---

## 26.3 Secret Scanning

Plugin patches must be secret scanned.

---

## 26.4 Approval

Applying a plugin patch remains subject to project policy and approval.

---

## 26.5 Reversal

Patch reversal metadata must remain available where technically practical.

---

# 27. AI Access

## 27.1 Router Requirement

Plugins must access AI through the AI Router.

---

## 27.2 Provider Neutrality

Plugins should request capabilities rather than hard-code a provider.

---

## 27.3 Provider-Specific Plugins

A provider-adapter plugin may contain provider-specific code only inside its adapter boundary.

---

## 27.4 Prompt Visibility

Plugin AI requests must be inspectable.

---

## 27.5 Tool Execution

Model-generated tool proposals must not execute directly.

---

# 28. Knowledge Access

## 28.1 Query

Plugins may query the Knowledge Engine through granted capability scope.

---

## 28.2 Write

Plugins may propose knowledge records.

---

## 28.3 Provenance

Plugin-created knowledge must identify:

- plugin;
- version;
- source;
- method;
- and confidence.

---

## 28.4 Cross-Project Access

Cross-project access is denied by default.

---

## 28.5 Pattern Contributions

Plugins may propose patterns.

They may not mark them Trusted without evidence and required review.

---

# 29. Workflow Contributions

## 29.1 Workflow Definitions

Plugins may contribute workflow definitions.

---

## 29.2 Validation

Plugin workflows must pass Workflow Engine validation.

---

## 29.3 Capability Declaration

Each contributed workflow must declare all capabilities and permissions.

---

## 29.4 Role Templates

Plugins may provide role templates.

Role templates remain bounded workflow roles, not autonomous authorities.

---

## 29.5 Hidden Stages

Plugin workflows must not contain hidden executable stages.

---

## 29.6 Disablement Impact

Disabling a plugin must mark dependent workflows unavailable or degraded.

---

# 30. Custom Workflow Stages

## 30.1 Stage Contract

A plugin may provide a custom workflow-stage type.

---

## 30.2 Stage Metadata

The stage type must define:

- identifier;
- version;
- input schema;
- output schema;
- permission requirements;
- idempotency;
- cancellation;
- timeout;
- compensation support;
- and visibility.

---

## 30.3 Execution

Custom stages execute through the Plugin Host.

---

## 30.4 Safety

A custom stage must not bypass the Workflow Engine or Policy Engine.

---

# 31. UI Contributions

## 31.1 Contribution Types

Plugins may contribute:

- commands;
- toolbar actions;
- context-menu actions;
- panels;
- project views;
- settings pages;
- status items;
- notifications;
- and workflow visualisations.

---

## 31.2 Declarative Preference

UI contributions should be declarative where practical.

---

## 31.3 Sandboxed Views

Rich plugin views should run in a sandboxed view host.

---

## 31.4 Domain State

Plugin UI must not become authoritative for domain state.

---

## 31.5 Permission Visibility

The UI must clearly identify when an action is provided by a plugin.

---

## 31.6 Design Consistency

Plugins should use Opure design tokens and accessibility standards.

---

## 31.7 Accessibility

Plugin UI must support:

- keyboard navigation;
- readable labels;
- scaling;
- contrast requirements;
- and assistive technologies.

---

# 32. Command Contributions

## 32.1 Command Identity

Commands must have stable identifiers.

---

## 32.2 Command Metadata

A command should declare:

- title;
- description;
- category;
- icon;
- availability condition;
- required capability;
- risk level;
- and execution target.

---

## 32.3 Context Conditions

Commands may be available based on:

- active project;
- selected file;
- repository state;
- workflow state;
- and plugin health.

---

## 32.4 Execution Boundary

Commands route through the Desktop Gateway and Plugin Host.

---

# 33. Parser Contributions

## 33.1 Parser Contract

Parser plugins must implement the parser contract defined by SPEC-005.

---

## 33.2 Language Identity

A parser must declare:

- language;
- file extensions;
- MIME types where relevant;
- parser version;
- and feature support.

---

## 33.3 Parse Safety

Parsers must treat project content as untrusted.

---

## 33.4 Incremental Support

Parsers should declare whether incremental parsing is supported.

---

## 33.5 Output Provenance

Parser output must identify the parser plugin and version.

---

# 34. Build and Test Contributions

## 34.1 Build Adapters

Plugins may contribute build-system adapters.

---

## 34.2 Structured Command Plans

Build adapters must produce structured command plans.

---

## 34.3 Tool Detection

Detection must not execute arbitrary project scripts without approval.

---

## 34.4 Output Parsing

Adapters may parse:

- compiler output;
- test output;
- coverage;
- warnings;
- and artefacts.

---

## 34.5 Authority

The Build Manager remains authoritative for build execution records.

---

# 35. Repository Contributions

## 35.1 Repository Adapter

Plugins may provide repository adapters.

---

## 35.2 Contract Boundary

Provider-specific repository behaviour must remain inside the adapter.

---

## 35.3 Credentials

Credentials must use the Secrets Vault.

---

## 35.4 Network

Remote repository traffic must use the Network Gateway.

---

# 36. MCP Integration

## 36.1 Complementary Roles

Plugins and MCP serve different purposes.

A plugin is an Opure-native extension package.

An MCP server is an external capability provider reached through the MCP Gateway.

---

## 36.2 Plugin-Managed MCP

A plugin may help configure or interpret MCP capabilities.

It must not communicate with an MCP server outside the gateway.

---

## 36.3 Capability Translation

A plugin may translate MCP results into Opure domain contracts.

---

## 36.4 No Automatic Trust

Installing a plugin does not grant trust to an MCP server it references.

---

# 37. Logging

## 37.1 Structured Logging

Plugins must use the provided logging interface.

---

## 37.2 Required Metadata

Plugin logs should include:

- plugin identifier;
- plugin version;
- host instance;
- project where applicable;
- correlation identifier;
- severity;
- and safe message.

---

## 37.3 Secret Safety

Plugin logs must not contain secret values.

---

## 37.4 Retention

Plugin logs use bounded retention.

---

# 38. Trust Centre Integration

## 38.1 Required Records

The Plugin Manager and Plugin Host should record:

- installation;
- signature result;
- permission request;
- permission grant or denial;
- enablement;
- disablement;
- update;
- migration;
- crash;
- quarantine;
- network access;
- secret access;
- patch proposal;
- workflow contribution;
- and removal.

---

## 38.2 Noise Control

Routine low-risk plugin queries may be aggregated.

---

## 38.3 Visibility

The developer should be able to inspect:

- what the plugin can do;
- what it has done;
- what data it accessed;
- which network destinations it used;
- and which workflows depend on it.

---

# 39. Diagnostics

## 39.1 Diagnostic Interface

Plugins should expose safe diagnostic information.

---

## 39.2 Diagnostic Bundle

A plugin diagnostic bundle may include:

- manifest;
- version;
- compatibility;
- granted capabilities;
- health;
- recent safe errors;
- resource use;
- and migration state.

---

## 39.3 No Secrets

Diagnostic bundles must exclude secret values.

---

# 40. Failure Handling

## 40.1 Plugin Crash

On crash, the Plugin Host should:

1. preserve diagnostics;
2. terminate the failed host if required;
3. release resources;
4. mark plugin unhealthy;
5. apply bounded restart policy;
6. and notify dependent services.

---

## 40.2 Restart Loops

Repeated crashes must trigger a circuit breaker or quarantine.

---

## 40.3 Degraded Operation

Optional plugin failure must not make Opure unavailable.

---

## 40.4 Partial Failure

A plugin may expose capability-specific degradation.

---

## 40.5 Quarantine

A plugin may be quarantined when:

- integrity fails;
- repeated crashes occur;
- malicious behaviour is detected;
- permission abuse is attempted;
- or compatibility becomes unsafe.

---

# 41. Compatibility

## 41.1 Opure Version

Plugins must declare minimum and optional maximum compatible Opure versions.

---

## 41.2 Contract Compatibility

Capability compatibility must be checked independently from application version.

---

## 41.3 Runtime Compatibility

Plugins must declare:

- operating systems;
- CPU architectures;
- runtime versions;
- native dependencies;
- and optional GPU requirements.

---

## 41.4 Forward Compatibility

Plugins should tolerate unknown optional fields where safe.

---

## 41.5 Breaking Changes

Breaking SDK changes require:

- major contract version;
- migration guidance;
- deprecation period where practical;
- and compatibility tooling.

---

# 42. Dependency Model

## 42.1 Plugin Dependencies

A plugin may depend on:

- Opure capabilities;
- runtime packages;
- or other plugins.

---

## 42.2 Plugin-to-Plugin Dependencies

Plugin-to-plugin dependencies should be avoided where a stable capability contract can be used.

---

## 42.3 Dependency Cycles

Required cycles are prohibited.

---

## 42.4 Dependency Resolution

The Plugin Manager must validate dependency versions before enablement.

---

## 42.5 Missing Optional Dependencies

Optional dependency absence should produce degraded capability, not hidden failure.

---

# 43. Signing and Integrity

## 43.1 Integrity

Every package must be verified against its digest.

---

## 43.2 Signatures

Signed packages should include:

- publisher identity;
- signing certificate or key identifier;
- signature;
- and signing timestamp where available.

---

## 43.3 Signature Meaning

A valid signature proves that the package matches what the signer produced.

It does not prove:

- safety;
- correctness;
- quality;
- compatibility;
- or ethical behaviour.

---

## 43.4 Revocation

The system should support publisher-key revocation and compromised-package warnings in the future.

---

## 43.5 Offline Verification

Package integrity and signature checks should work offline where the required trust material is available.

---

# 44. Distribution

## 44.1 Local Installation

The developer may install a local package.

---

## 44.2 Registry Installation

A future registry may provide discovery and package delivery.

---

## 44.3 Marketplace

A public marketplace is DEFERRED.

---

## 44.4 Download Transparency

Before download, Opure should show:

- publisher;
- version;
- size;
- permissions;
- licence;
- signature status;
- and compatibility.

---

## 44.5 Automatic Updates

Automatic plugin updates should be disabled by default or limited to clearly approved policy.

Updates requesting new permissions must never apply silently.

---

# 45. Development Mode

## 45.1 Local Development

The SDK should support loading a plugin from a local development directory.

---

## 45.2 Clear Labelling

Development plugins must be visually marked.

---

## 45.3 Hot Reload

Hot reload may be supported for safe contribution types.

---

## 45.4 Permission Simulation

The test harness should allow developers to simulate granted and denied permissions.

---

## 45.5 Safe Defaults

Development mode must not silently disable security boundaries.

---

# 46. SDK Components

The SDK should include:

- manifest schema;
- contract definitions;
- client libraries;
- test harness;
- mock Opure services;
- permission simulator;
- package builder;
- package validator;
- signature tools;
- migration tools;
- diagnostics tools;
- example plugins;
- and documentation.

---

# 47. Language Support

## 47.1 Initial Language

The first SDK may support one implementation language.

---

## 47.2 Contract Neutrality

Contracts should remain language-neutral.

---

## 47.3 Future Languages

Future language support may be provided through:

- generated bindings;
- JSON or binary contracts;
- WebAssembly;
- local RPC;
- or language-specific hosts.

---

## 47.4 Native Code

Native plugins require stronger review and isolation.

---

# 48. Testing Requirements

## 48.1 Unit Tests

Plugin authors should unit test plugin business logic independently.

---

## 48.2 Contract Tests

Every plugin must pass contract tests for:

- manifest;
- startup;
- readiness;
- health;
- shutdown;
- capability invocation;
- errors;
- cancellation;
- and permissions.

---

## 48.3 Permission Tests

Tests must cover:

- allowed access;
- denied access;
- narrowed scope;
- revoked permission;
- expired permission;
- and approval invalidation.

---

## 48.4 Isolation Tests

Tests should simulate:

- crash;
- memory pressure;
- infinite loop;
- excessive output;
- forbidden filesystem access;
- forbidden network access;
- and process-spawn attempts.

---

## 48.5 Update Tests

Tests must cover:

- successful update;
- failed migration;
- permission expansion;
- rollback;
- and compatibility rejection.

---

## 48.6 Removal Tests

Tests must verify:

- host shutdown;
- contribution removal;
- workflow dependency handling;
- optional data deletion;
- and no stale permissions.

---

## 48.7 Security Tests

Tests must cover:

- path traversal;
- secret leakage;
- prompt injection;
- malformed messages;
- oversized payloads;
- malicious package contents;
- signature failure;
- and capability spoofing.

---

# 49. SDK Validation Levels

## 49.1 Basic Validation

Checks:

- manifest schema;
- package structure;
- entry point;
- and identity.

---

## 49.2 Contract Validation

Checks:

- capability schemas;
- version compatibility;
- lifecycle;
- and error contracts.

---

## 49.3 Security Validation

Checks:

- permission declarations;
- secret handling;
- network declarations;
- file access;
- and unsafe APIs.

---

## 49.4 Behavioural Validation

Runs:

- startup;
- health;
- representative commands;
- cancellation;
- and shutdown.

---

## 49.5 Publication Validation

A future registry may require all previous validation plus publisher verification.

---

# 50. Error Model

Recommended stable errors include:

- `PLUGIN_PACKAGE_INVALID`
- `PLUGIN_MANIFEST_INVALID`
- `PLUGIN_SIGNATURE_INVALID`
- `PLUGIN_INTEGRITY_FAILED`
- `PLUGIN_INCOMPATIBLE`
- `PLUGIN_DEPENDENCY_MISSING`
- `PLUGIN_PERMISSION_DENIED`
- `PLUGIN_CAPABILITY_UNDECLARED`
- `PLUGIN_CAPABILITY_UNAVAILABLE`
- `PLUGIN_START_FAILED`
- `PLUGIN_HEALTH_FAILED`
- `PLUGIN_CRASHED`
- `PLUGIN_QUARANTINED`
- `PLUGIN_TIMEOUT`
- `PLUGIN_CANCELLED`
- `PLUGIN_RESOURCE_LIMIT`
- `PLUGIN_NETWORK_BLOCKED`
- `PLUGIN_SECRET_ACCESS_DENIED`
- `PLUGIN_MIGRATION_FAILED`
- `PLUGIN_UPDATE_FAILED`
- `PLUGIN_REMOVE_FAILED`
- and `PLUGIN_INTERNAL_ERROR`

Errors must include safe recovery guidance where possible.

---

# 51. Plugin API

## 51.1 Plugin Manager Commands

The Plugin Manager should provide:

- `InstallPlugin`
- `EnablePlugin`
- `DisablePlugin`
- `UpdatePlugin`
- `RemovePlugin`
- `GrantPluginPermission`
- `RevokePluginPermission`
- `QuarantinePlugin`
- `RestorePlugin`
- and `ValidatePluginPackage`

---

## 51.2 Plugin Manager Queries

The Plugin Manager should provide:

- `ListPlugins`
- `GetPlugin`
- `GetPluginManifest`
- `GetPluginPermissions`
- `GetPluginDependencies`
- `GetPluginContributions`
- `GetPluginCompatibility`
- `GetPluginStorageUsage`
- and `GetPluginUpdateStatus`

---

## 51.3 Plugin Host Commands

The Plugin Host should provide:

- `StartPluginRuntime`
- `StopPluginRuntime`
- `InvokePluginCapability`
- `CancelPluginTask`
- `RestartPluginRuntime`
- and `RunPluginDiagnostic`

---

## 51.4 Events

The system should publish:

- `PluginDiscovered`
- `PluginInstalled`
- `PluginEnabled`
- `PluginDisabled`
- `PluginUpdated`
- `PluginRemoved`
- `PluginPermissionRequested`
- `PluginPermissionGranted`
- `PluginPermissionDenied`
- `PluginHealthChanged`
- `PluginCrashed`
- `PluginQuarantined`
- `PluginContributionRegistered`
- and `PluginContributionRemoved`

---

# 52. Performance Requirements

## 52.1 Startup Cost

Plugins should not unnecessarily delay minimum Opure readiness.

---

## 52.2 Lazy Activation

Plugins should activate lazily where practical.

---

## 52.3 Resource Visibility

The developer should be able to inspect plugin resource use.

---

## 52.4 Background Work

Plugin background work must use the Scheduler.

---

## 52.5 Output Limits

Plugin messages and streams must have bounded size.

---

# 53. Example Capability Plugin Flow

A safe project-metrics plugin flow may be:

```text
1. Developer invokes plugin command.
2. Desktop Gateway sends command to Plugin Manager.
3. Plugin Manager verifies plugin state.
4. Policy Engine evaluates project-read permission.
5. Plugin Host invokes plugin capability.
6. Plugin requests selected file metadata through Workspace Service.
7. Plugin calculates metrics.
8. Plugin returns structured result.
9. Desktop displays result.
10. Trust Centre records significant access where required.
```

The plugin never receives unrestricted raw operating-system authority.

---

# 54. Example Patch-Producing Plugin Flow

```text
1. Plugin analyses approved project data.
2. Plugin creates a patch proposal.
3. Patch Service validates format and paths.
4. Secret scan runs.
5. Developer previews changes.
6. Policy Engine evaluates application.
7. Patch Service applies transactionally.
8. Build Manager validates.
9. Trust Centre records outcome.
```

---

# 55. Example External Connector Flow

```text
1. Plugin proposes external request.
2. Network Gateway classifies destination.
3. Policy Engine evaluates project cloud policy.
4. Data-sharing preview is shown.
5. Developer approves if required.
6. Secrets Vault releases bounded credential access.
7. Network Gateway sends request.
8. Plugin receives normalised response.
9. Trust Centre records destination and data categories.
```

---

# 56. Initial Implementation Milestone

The first Plugin SDK milestone is successful when it can:

1. define a versioned plugin manifest;
2. package a plugin;
3. validate package structure;
4. verify package digest;
5. install a local unsigned development plugin;
6. display requested permissions;
7. enable the plugin in an isolated host;
8. register one command contribution;
9. invoke one plugin capability;
10. read approved project data through Workspace Service;
11. create a patch proposal through Patch Service;
12. deny undeclared network access;
13. deny undeclared secret access;
14. persist plugin settings;
15. record plugin activity;
16. disable the plugin safely;
17. update the plugin;
18. roll back a failed update;
19. remove the plugin and optionally delete its data;
20. and pass lifecycle, contract, permission and isolation tests.

The milestone should include at least:

- one simple capability plugin;
- one workflow-contribution plugin;
- and one parser or build-adapter example.

---

# 57. Acceptance Criteria

SPEC-007 is implemented when:

- [ ] Every plugin has a stable identifier and version.
- [ ] Every package has a validated manifest.
- [ ] Every installed package has a verified digest.
- [ ] Signatures are interpreted as origin and integrity evidence only.
- [ ] Installation does not automatically grant permissions.
- [ ] Plugin permissions are explicit, scoped and revocable.
- [ ] Undeclared protected capability use is denied.
- [ ] Third-party plugin code runs outside the trusted core.
- [ ] Plugin crashes do not crash the Runtime Kernel.
- [ ] Repeated plugin crashes trigger circuit breaking or quarantine.
- [ ] Plugins read project files only through Workspace Service.
- [ ] Plugins write project files only through Patch Service.
- [ ] Plugin command execution uses controlled service contracts.
- [ ] Plugin network traffic uses Network Gateway controls.
- [ ] Plugin secret access uses Secrets Vault controls.
- [ ] Plugins access AI only through the AI Router.
- [ ] Plugins access MCP only through the MCP Gateway.
- [ ] Plugin-created knowledge retains provenance.
- [ ] Plugin workflow definitions pass Workflow Engine validation.
- [ ] Plugin workflows cannot contain hidden executable stages.
- [ ] UI contributions are visibly identified as plugin-provided.
- [ ] Plugin UI does not own authoritative domain state.
- [ ] Plugin data is isolated.
- [ ] Secret values do not enter plugin storage or logs.
- [ ] Updates requesting broader permissions require review.
- [ ] Failed updates can recover or roll back safely.
- [ ] Disabling a plugin removes active contributions.
- [ ] Removal explains data and workflow impact.
- [ ] Development plugins are clearly labelled.
- [ ] The SDK includes mocks, validation and permission simulation.
- [ ] Architecture tests detect service-boundary bypass.
- [ ] Security tests cover path, network, secret and package attacks.
- [ ] Plugin resource use is visible and bounded.
- [ ] Safe Mode can start with third-party plugins disabled.
- [ ] The platform remains usable with all optional plugins disabled.

---

# 58. Deferred Decisions

The following are intentionally deferred:

- primary plugin implementation language;
- plugin-host runtime technology;
- WebAssembly support;
- native plugin support;
- manifest serialisation format;
- package archive format;
- signature algorithm;
- publisher trust infrastructure;
- public registry;
- marketplace;
- revenue model;
- review process;
- automatic update defaults;
- organisation-managed plugins;
- remote plugin execution;
- and cross-device plugin synchronisation.

These decisions must not weaken:

- capability mediation;
- least privilege;
- project isolation;
- developer visibility;
- safe disablement;
- or recoverability.

---

# 59. Required Architecture Decision Records

Implementation should produce ADRs for:

- plugin manifest format;
- plugin package format;
- Plugin Host technology;
- process isolation;
- SDK implementation language;
- contract serialisation;
- plugin storage model;
- UI sandboxing;
- package signing;
- update rollback;
- development-mode loading;
- and registry architecture.

---

# 60. Relationship to Later Specifications

This specification provides extensibility foundations for:

- **SPEC-008 — Trust Centre and Security**
- **SPEC-009 — Workspace and File Patch Engine**
- **SPEC-010 — Desktop User Interface**
- **SPEC-011 — Project and Build Management**
- **SPEC-012 — Roadmap**

SPEC-008 will define detailed permission, secret, network, trust and quarantine requirements.

SPEC-009 will define file and patch contracts used by plugins.

SPEC-010 will define desktop contribution locations and user experience.

SPEC-011 will define build, test and repository adapter details.

Later specifications may add stricter requirements.

They must not allow plugins to bypass Opure service ownership.

---

# 61. Founder Approval

This document remains a founder draft until explicitly approved.

Approval establishes the following rules:

- A plugin may extend Opure, but it may not bypass Opure.
- Installation does not equal trust.
- Permissions remain explicit, scoped and revocable.
- Third-party plugin code runs outside the trusted core.
- Plugins use service capabilities rather than private implementation details.
- Project reads use Workspace Service.
- Project writes use Patch Service.
- AI access uses AI Router.
- MCP access uses MCP Gateway.
- Network access uses Network Gateway.
- Secret access uses Secrets Vault.
- Workflow contributions remain inspectable and policy-bound.
- Updates cannot silently broaden permissions.
- Plugin failure cannot take down the core platform.
- Developers can disable, inspect, update and remove plugins safely.
- Safe extensibility is part of Opure's architecture, not an exception to it.

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**