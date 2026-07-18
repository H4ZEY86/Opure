# SPEC-002 — Runtime Kernel

## Opure Platform Runtime Foundation

**Document:** SPEC-002  
**Status:** Founder Draft  
**Version:** 0.1  
**Language:** British English  
**Last updated:** 18 July 2026  
**Depends on:** CHARTER-001, SPEC-001  

---

## 1. Purpose

This specification defines the Runtime Kernel of the Opure Platform.

The Runtime Kernel is the local foundation responsible for starting, coordinating, supervising and stopping Opure's major services.

It is not the artificial-intelligence engine, user interface, workflow engine, plugin manager or project-memory system.

It is the controlled environment in which those systems operate.

The Runtime Kernel MUST provide:

- deterministic startup and shutdown;
- service registration and discovery;
- service lifecycle management;
- health monitoring;
- dependency validation;
- failure isolation;
- configuration loading;
- permission and policy enforcement entry points;
- local message and event transport;
- task scheduling coordination;
- resource-awareness;
- crash recovery;
- runtime state inspection;
- and reliable audit integration.

The Runtime Kernel MUST remain useful even when no AI model is available.

---

## 2. Position in the Platform

The Runtime Kernel sits beneath the desktop interface and above operating-system-specific facilities.

A simplified logical view is:

```text
Desktop Interface
        │
        ▼
Runtime Kernel
        │
        ├── Service Registry
        ├── Lifecycle Manager
        ├── Scheduler
        ├── Event and Message Transport
        ├── Health Supervisor
        ├── Configuration Manager
        ├── Policy Enforcement Hooks
        ├── Resource Monitor
        ├── Recovery Manager
        └── Runtime Diagnostics
                │
                ▼
Major Platform Services
        ├── AI Router
        ├── Workflow Engine
        ├── Knowledge Engine
        ├── Plugin Manager
        ├── MCP Gateway
        ├── Network Gateway
        ├── Workspace and Patch Engine
        ├── Secrets Vault
        ├── Trust Centre
        └── Storage Services
```

The Runtime Kernel MUST coordinate these services without depending on their internal implementation.

---

## 3. Design Goals

The Runtime Kernel MUST be:

- local-first;
- deterministic;
- observable;
- resilient;
- modular;
- loosely coupled;
- secure by default;
- efficient;
- testable;
- and independent from any specific AI provider.

The Runtime Kernel SHOULD make platform state easy to understand.

It MUST NOT create hidden background behaviour that cannot be inspected.

It MUST favour clear contracts over implicit behaviour.

---

## 4. Non-Goals

The Runtime Kernel is not responsible for:

- generating code;
- reasoning about project architecture;
- selecting source files for an AI prompt;
- storing semantic project knowledge;
- rendering the desktop interface;
- implementing provider-specific AI protocols;
- interpreting plugin business logic;
- directly editing project files;
- directly storing secrets;
- or replacing the operating system's process manager.

Those responsibilities belong to other Opure services.

The Runtime Kernel MAY expose infrastructure used by those services.

---

## 5. Normative Language

The terms **MUST**, **MUST NOT**, **SHOULD**, **SHOULD NOT**, **MAY** and **DEFERRED** have the meanings defined in SPEC-001.

A Runtime Kernel design that intentionally violates a **SHOULD** requirement MUST be justified in an Architecture Decision Record.

---

# 6. Core Responsibilities

## 6.1 Runtime Bootstrap

The Runtime Kernel MUST provide a single, controlled bootstrap process for starting Opure.

Bootstrap MUST:

1. establish the runtime identity;
2. determine the runtime mode;
3. load trusted base configuration;
4. initialise secure logging;
5. inspect the previous shutdown state;
6. validate required directories and storage;
7. initialise operating-system integrations;
8. initialise the service registry;
9. register core services;
10. validate service dependencies;
11. start services in dependency order;
12. verify platform health;
13. expose runtime readiness;
14. and record the result in the Trust Centre or temporary audit buffer.

The desktop interface MUST NOT report Opure as ready until the minimum required services are healthy.

---

## 6.2 Runtime Identity

Every Runtime Kernel instance MUST have a unique runtime instance identifier.

The runtime identity SHOULD include:

- an instance identifier;
- application version;
- runtime protocol version;
- start timestamp;
- operating-system information;
- process identifier;
- active user context;
- runtime mode;
- and safe-mode status.

The runtime identity MUST NOT include secret values.

The identity SHOULD be included in diagnostic and Trust Centre records where useful.

---

## 6.3 Runtime Modes

The Runtime Kernel SHOULD support the following modes:

- **Normal** — standard operation.
- **Safe Mode** — optional services and third-party integrations are restricted.
- **Recovery Mode** — startup focuses on diagnosis and repair after a failed or incomplete shutdown.
- **Maintenance Mode** — used for migrations, repair or controlled updates.
- **Test Mode** — deterministic behaviour for automated testing.
- **Headless Mode** — runtime services operate without the desktop interface where supported.

The exact user interface for selecting a mode is DEFERRED to SPEC-010.

Runtime modes MUST be visible in diagnostics and the Trust Centre.

---

## 6.4 Service Coordination

The Runtime Kernel MUST coordinate major platform services through explicit service contracts.

It MUST provide:

- service registration;
- dependency declaration;
- startup ordering;
- shutdown ordering;
- health reporting;
- capability discovery;
- version compatibility checks;
- and failure notification.

A service MUST NOT require knowledge of another service's concrete implementation.

---

## 6.5 Health Supervision

The Runtime Kernel MUST monitor the operational state of registered services.

Health information SHOULD include:

- lifecycle state;
- readiness;
- liveness;
- dependency state;
- last successful operation;
- recent failure summary;
- restart count;
- degraded capability;
- and resource use where relevant.

The Health Supervisor MUST distinguish between:

- unavailable;
- starting;
- ready;
- busy;
- degraded;
- unhealthy;
- stopping;
- stopped;
- and failed.

A degraded optional service MUST NOT automatically make the entire platform unavailable.

---

## 6.6 Scheduling Coordination

The Runtime Kernel MUST provide or host a Scheduler capable of coordinating:

- interactive tasks;
- background tasks;
- model inference;
- indexing;
- builds;
- tests;
- plugin work;
- maintenance;
- and recovery operations.

The Scheduler MUST account for:

- task priority;
- developer interaction;
- CPU pressure;
- memory pressure;
- GPU and VRAM pressure;
- storage pressure;
- network policy;
- provider availability;
- cancellation;
- and performance mode.

Detailed workflow orchestration belongs to SPEC-006.

The Runtime Scheduler coordinates execution capacity; it does not define engineering workflow meaning.

---

## 6.7 Runtime Messaging

The Runtime Kernel MUST provide local communication primitives for major services.

Supported communication concepts SHOULD include:

- commands;
- queries;
- responses;
- events;
- notifications;
- streams;
- and cancellation signals.

Message transport MAY be in-process or inter-process.

The logical contract MUST remain independent from the chosen transport.

---

## 6.8 Configuration Management

The Runtime Kernel MUST load, validate and distribute runtime configuration.

Configuration scopes SHOULD include:

- application defaults;
- machine settings;
- user settings;
- project settings;
- session overrides;
- and temporary workflow overrides.

More specific scopes SHOULD override less specific scopes only where the schema permits.

Configuration changes MUST be validated before becoming active.

Security-critical settings MUST NOT be silently downgraded.

---

## 6.9 Failure Isolation

The Runtime Kernel MUST prevent failure in one optional service from unnecessarily terminating unrelated services.

It SHOULD isolate:

- third-party plugins;
- MCP servers;
- external provider adapters;
- heavy model processes;
- command execution;
- indexing workers;
- and other high-risk or unstable workloads.

Isolation MAY use:

- process boundaries;
- worker threads;
- restricted execution hosts;
- job objects;
- operating-system permissions;
- containers;
- or equivalent mechanisms.

The exact mechanism is DEFERRED to later specifications and ADRs.

---

## 6.10 Recovery

The Runtime Kernel MUST detect incomplete or abnormal previous shutdowns.

After an abnormal shutdown it SHOULD:

1. preserve diagnostic evidence;
2. inspect runtime journals;
3. detect incomplete file or database operations;
4. verify core storage integrity;
5. identify services involved in the failure;
6. avoid automatically repeating destructive work;
7. enter Recovery Mode when required;
8. and provide understandable recovery actions.

Recovery MUST prioritise project integrity over automatic continuation.

---

# 7. Runtime Boundaries

## 7.1 Trusted Core Boundary

The Runtime Kernel and a minimal set of security-critical services form the trusted core.

The trusted core SHOULD be kept small.

Likely trusted-core components include:

- Runtime Bootstrap;
- Service Registry;
- Lifecycle Manager;
- Policy Engine entry point;
- Secrets Vault interface;
- secure configuration loader;
- audit buffer;
- and Workspace transaction coordinator.

The final trusted-core composition is DEFERRED to SPEC-008.

---

## 7.2 Untrusted and Semi-Trusted Components

The following MUST NOT automatically be treated as trusted core:

- third-party plugins;
- MCP servers;
- external AI providers;
- downloaded models;
- command-line tools;
- project build scripts;
- package-manager scripts;
- browser automation;
- and remote services.

These components MUST operate through declared capabilities and policy checks.

---

## 7.3 Project Boundary

Each project MUST have an independent runtime context.

A project runtime context SHOULD include:

- project identifier;
- workspace root;
- active configuration;
- cloud policy;
- approved providers;
- plugin permissions;
- project-memory namespace;
- Trust Centre namespace;
- build environment;
- and active workflows.

Project contexts MUST NOT leak data into one another by default.

---

## 7.4 User Boundary

The Runtime Kernel MUST operate under the authenticated operating-system user unless a deliberately configured service account or elevated process is required.

Privilege elevation MUST be:

- exceptional;
- visible;
- narrowly scoped;
- justified;
- and recorded.

Opure MUST NOT run the entire platform with administrator privileges by default.

---

# 8. Service Model

## 8.1 Service Definition

A Runtime Service is a component managed by the Runtime Kernel that exposes a versioned contract and participates in the runtime lifecycle.

A service definition MUST include:

- service identifier;
- display name;
- service version;
- contract version;
- service classification;
- required dependencies;
- optional dependencies;
- capabilities provided;
- permissions required;
- startup policy;
- restart policy;
- shutdown timeout;
- health-check policy;
- and isolation preference.

---

## 8.2 Service Classifications

Services SHOULD be classified as:

- **Critical Core** — required for safe platform operation.
- **Required Platform** — required for normal operation but not necessarily for recovery.
- **Optional Platform** — built-in capability that may be unavailable without blocking core use.
- **Provider Adapter** — connects to an AI or external provider.
- **Plugin Host** — hosts one or more plugins.
- **Project Worker** — performs project-specific background work.
- **Ephemeral Worker** — exists for one bounded operation.
- **External Bridge** — represents an MCP server, CLI tool, API or local service.

Classification influences startup, failure and restart policy.

---

## 8.3 Service States

Every managed service MUST have exactly one primary lifecycle state:

```text
Registered
Configured
Starting
Ready
Degraded
Stopping
Stopped
Failed
Disabled
```

A service MAY expose secondary operational state such as:

- idle;
- busy;
- waiting;
- throttled;
- blocked by policy;
- blocked by dependency;
- or awaiting approval.

State transitions MUST be recorded and inspectable.

---

## 8.4 Service Registration

Services MUST register through the Service Registry.

Registration MUST validate:

- unique identity;
- contract compatibility;
- dependency declarations;
- requested capabilities;
- configuration schema;
- health-check availability;
- and lifecycle hooks.

Duplicate service identities MUST be rejected unless versioned side-by-side operation is explicitly supported.

---

## 8.5 Dependency Declarations

Dependencies MUST be declared explicitly.

A dependency declaration SHOULD specify:

- service or capability required;
- minimum compatible contract version;
- whether the dependency is required or optional;
- startup relationship;
- health relationship;
- and fallback behaviour.

Circular required dependencies MUST be rejected during bootstrap.

Optional cycles SHOULD be avoided and MUST be documented if allowed.

---

## 8.6 Capability-Based Discovery

Services SHOULD discover required functionality through capabilities rather than concrete service names where practical.

Examples include:

- `ai.inference.chat`;
- `workspace.patch.apply`;
- `storage.keyvalue`;
- `trust.record.write`;
- `network.request`;
- `secrets.read`;
- `build.execute`;
- and `project.memory.query`.

Capability names and schema are DEFERRED to SPEC-003.

---

## 8.7 Service Startup

Service startup MUST be:

- dependency-aware;
- cancellable where safe;
- time-bounded;
- observable;
- and idempotent where practical.

A service MUST NOT report itself as ready until it can fulfil its declared minimum contract.

If startup partially succeeds, the service MUST either:

- cleanly roll back;
- enter a documented degraded state;
- or fail with actionable diagnostics.

---

## 8.8 Service Shutdown

Shutdown MUST occur in reverse dependency order unless an explicit contract requires otherwise.

Services MUST receive a graceful shutdown request before forced termination.

A service shutdown SHOULD:

1. stop accepting new work;
2. finish or safely pause active work;
3. flush durable state;
4. release resources;
5. report final state;
6. and acknowledge completion.

The Runtime Kernel MUST impose configurable shutdown deadlines.

A service that fails to stop within its deadline MAY be forcefully terminated after diagnostic evidence is preserved.

---

## 8.9 Restart Policies

Supported restart policies SHOULD include:

- **Never**;
- **On Failure**;
- **Always While Enabled**;
- **Limited Retry**;
- **Manual Approval Required**.

Restart policy MUST account for repeated failure.

The Runtime Kernel MUST prevent uncontrolled restart loops.

A circuit breaker SHOULD disable or pause a repeatedly failing service and surface the issue to the developer.

---

## 8.10 Service Versioning

Service contracts MUST be versioned independently from implementation versions.

Compatibility MUST be evaluated using explicit rules.

A newer service MUST NOT assume that every client has upgraded simultaneously.

Breaking contract changes require:

- a new major contract version;
- migration guidance;
- compatibility tests;
- and an ADR where they affect major architecture.

---

# 9. Bootstrap Sequence

## 9.1 Phase 0 — Process Entry

At process entry, Opure MUST:

- establish a minimal crash handler;
- determine executable and data locations;
- establish the active user;
- parse safe startup arguments;
- and avoid loading optional code.

No third-party plugin code may run during Phase 0.

---

## 9.2 Phase 1 — Secure Foundation

The Runtime Kernel MUST initialise:

- secure logging;
- runtime identity;
- base directories;
- environment checks;
- configuration integrity;
- lock files or single-instance coordination;
- and the temporary audit buffer.

Secrets MUST NOT be logged.

---

## 9.3 Phase 2 — Previous-State Inspection

The Runtime Kernel MUST inspect:

- prior shutdown marker;
- incomplete transaction journals;
- pending migrations;
- crash reports;
- previous safe-mode request;
- and required recovery actions.

No destructive recovery action may occur without policy and visibility.

---

## 9.4 Phase 3 — Core Infrastructure

The Runtime Kernel SHOULD start:

- Configuration Manager;
- Service Registry;
- Lifecycle Manager;
- internal messaging;
- Storage Services;
- Policy Engine;
- Secrets Vault interface;
- Trust Centre writer;
- and Health Supervisor.

Required ordering MUST be explicit.

---

## 9.5 Phase 4 — Required Platform Services

The Runtime Kernel SHOULD start required services such as:

- Project Manager;
- Workspace and Patch Engine;
- Scheduler;
- Workflow Engine;
- AI Router;
- Knowledge Engine;
- Plugin Manager;
- MCP Gateway;
- and Network Gateway.

A service may start in degraded mode if its optional dependencies are unavailable.

---

## 9.6 Phase 5 — Optional Services and Integrations

Optional services MAY start after the platform reaches minimum safe readiness.

Third-party plugins SHOULD start only after:

- policy is available;
- Trust Centre recording is available;
- secrets controls are available;
- and project context is established where required.

---

## 9.7 Phase 6 — Readiness

Opure may report itself ready when:

- the trusted core is healthy;
- storage is usable;
- project files can be opened safely;
- permissions can be evaluated;
- significant actions can be audited;
- and the desktop interface can query runtime state.

AI availability MUST NOT be required for minimum runtime readiness.

---

# 10. Shutdown Sequence

## 10.1 Shutdown Triggers

Shutdown may be triggered by:

- developer request;
- operating-system shutdown;
- application update;
- unrecoverable critical failure;
- test completion;
- or maintenance operation.

The trigger MUST be recorded.

---

## 10.2 Shutdown Phases

The Runtime Kernel SHOULD perform:

1. **Quiesce** — stop accepting new non-essential work.
2. **Cancel or Pause** — signal active tasks.
3. **Workflow Stabilisation** — checkpoint resumable workflows.
4. **Project Protection** — complete or roll back workspace transactions.
5. **Service Shutdown** — stop services in dependency-safe order.
6. **State Flush** — flush logs, Trust Centre records and durable queues.
7. **Integrity Marking** — write clean-shutdown state.
8. **Process Exit** — release final operating-system resources.

---

## 10.3 Forced Shutdown

Forced shutdown MUST be a last resort.

Before forced termination, Opure SHOULD:

- preserve diagnostic state;
- mark the shutdown as incomplete;
- record active transactions;
- and identify services that failed to stop.

On next launch, Recovery Mode SHOULD inspect the incomplete shutdown.

---

# 11. Configuration System

## 11.1 Configuration Sources

Configuration MAY come from:

- built-in defaults;
- signed or trusted installation configuration;
- machine configuration;
- user configuration;
- project configuration;
- environment variables;
- command-line arguments;
- and temporary session overrides.

Configuration precedence MUST be documented and deterministic.

---

## 11.2 Configuration Schema

Every managed service MUST declare a versioned configuration schema.

The schema SHOULD define:

- type;
- default;
- allowed values;
- scope;
- sensitivity;
- restart requirement;
- validation rules;
- and migration behaviour.

Unknown security-sensitive settings MUST be rejected.

Unknown non-critical settings MAY be preserved for forward compatibility but MUST be reported.

---

## 11.3 Configuration Validation

Configuration MUST be validated before use.

Invalid configuration MUST result in:

- a clear explanation;
- safe fallback where permitted;
- preservation of the invalid value for diagnosis;
- and no silent weakening of security.

---

## 11.4 Configuration Changes

Runtime configuration changes SHOULD be applied transactionally.

A change SHOULD follow:

1. validate;
2. preview impact;
3. request approval if required;
4. apply;
5. confirm service acceptance;
6. roll back on failure;
7. and record the outcome.

Changes requiring restart MUST be clearly identified.

---

## 11.5 Sensitive Configuration

Secret values MUST NOT be stored in normal configuration files.

Configuration MUST reference secrets by vault identifier or controlled alias.

Diagnostic output MUST redact sensitive configuration.

---

# 12. Messaging and Events

## 12.1 Message Envelope

Every runtime message SHOULD include:

- message identifier;
- message type;
- contract version;
- source service;
- destination or topic;
- runtime instance identifier;
- project identifier where applicable;
- correlation identifier;
- causation identifier;
- timestamp;
- priority;
- cancellation context;
- permission context reference;
- and payload.

Secret values MUST NOT be included unless the receiving contract explicitly requires them and policy permits it.

---

## 12.2 Commands

A command requests a state-changing action.

Commands MUST:

- have one responsible handler;
- be idempotent where practical;
- identify required permissions;
- produce a clear result;
- and be auditable when significant.

Examples include:

- apply patch;
- start build;
- install plugin;
- stop workflow;
- connect provider;
- and update project policy.

---

## 12.3 Queries

A query requests information without intentionally changing state.

Queries SHOULD be side-effect free.

Examples include:

- get service health;
- list available models;
- read project policy;
- retrieve workflow status;
- and inspect runtime resources.

---

## 12.4 Events

An event reports something that has already occurred.

Events MAY have multiple subscribers.

Events MUST NOT be used when a direct command response is required for correctness.

Examples include:

- service started;
- project opened;
- patch generated;
- test completed;
- provider disconnected;
- and policy changed.

---

## 12.5 Delivery Semantics

The default event transport SHOULD provide at-least-once delivery for durable significant events where practical.

Consumers MUST tolerate duplicate delivery where at-least-once semantics are used.

Ephemeral user-interface updates MAY use best-effort delivery.

Exactly-once delivery MUST NOT be claimed unless it is actually guaranteed by the complete transaction design.

---

## 12.6 Ordering

Ordering MUST be guaranteed only where explicitly required.

Messages requiring order SHOULD use:

- a partition key;
- sequence number;
- stream identifier;
- or transaction boundary.

Global ordering SHOULD be avoided because it harms scalability and resilience.

---

## 12.7 Backpressure

The messaging system MUST support backpressure.

A slow consumer MUST NOT cause unbounded memory growth.

Backpressure strategies MAY include:

- bounded queues;
- prioritisation;
- coalescing;
- throttling;
- dropping replaceable status updates;
- or pausing producers.

Significant audit, permission or transaction messages MUST NOT be silently dropped.

---

# 13. Scheduler

## 13.1 Task Definition

A scheduled task SHOULD declare:

- task identifier;
- task type;
- project context;
- owner service;
- priority;
- expected resource class;
- cancellation policy;
- timeout;
- retry policy;
- required capabilities;
- network requirement;
- provider requirement;
- and whether it is interactive or background.

---

## 13.2 Priority Classes

Recommended priority classes are:

- **Critical** — security, integrity or shutdown work.
- **Interactive** — directly blocking developer interaction.
- **Foreground** — visible user-requested work.
- **Background** — indexing, maintenance or preparation.
- **Idle** — work that should run only when resources are available.

Background work MUST yield to critical and interactive work where practical.

---

## 13.3 Resource Classes

Tasks SHOULD declare expected resource use:

- lightweight CPU;
- heavy CPU;
- memory intensive;
- GPU inference;
- GPU compute;
- disk intensive;
- network intensive;
- external process;
- or mixed.

The Scheduler MAY revise estimates using measured historical data.

---

## 13.4 Performance Modes

The Scheduler SHOULD support:

### Eco

- minimise background activity;
- prefer smaller or already-loaded models;
- constrain CPU and GPU use;
- and reduce power consumption.

### Balanced

- maintain responsiveness;
- allow measured background work;
- use the fast model by default;
- and load heavier models only when justified.

### Performance

- prioritise task completion;
- permit higher resource use;
- and increase concurrency where safe.

### Turbo

- maximise throughput within user-approved limits;
- allow aggressive model and worker use;
- and clearly disclose resource impact.

Balanced SHOULD be the initial default.

---

## 13.5 Model Scheduling

The Scheduler and AI Router SHOULD cooperate so that:

- one suitable fast model may remain loaded where resources permit;
- larger reasoning models load only when needed;
- model unloading considers likely near-term reuse;
- VRAM pressure is monitored;
- and multiple simultaneous models are avoided unless beneficial.

Model selection logic belongs to SPEC-004.

---

## 13.6 Cancellation

Every task that can run longer than a trivial duration SHOULD support cancellation.

Cancellation MUST be cooperative where possible.

The Scheduler MUST distinguish:

- cancellation requested;
- cancellation acknowledged;
- cancellation completed;
- and cancellation failed.

Cancellation MUST NOT leave file or database transactions in an unknown state.

---

## 13.7 Timeouts

Tasks SHOULD have explicit timeouts or deadline policies.

Timeout behaviour MUST be appropriate to the operation.

A timeout MUST NOT automatically imply that an external process has stopped.

The Runtime Kernel MUST verify termination or isolate the process before releasing associated resources.

---

## 13.8 Retry

Retries MUST be bounded.

Retry policy SHOULD consider:

- whether the operation is idempotent;
- whether failure is transient;
- resource cost;
- provider cost;
- network policy;
- and risk of duplicate side effects.

Destructive or externally visible actions MUST NOT be retried automatically unless safe idempotency is established.

---

# 14. Health and Diagnostics

## 14.1 Health Checks

Services SHOULD expose:

- liveness check;
- readiness check;
- dependency health;
- degraded capability detail;
- and optional deep diagnostic check.

Deep diagnostics SHOULD NOT run continuously if they are expensive.

---

## 14.2 Health Aggregation

The Runtime Kernel MUST calculate overall platform health without hiding partial failure.

Recommended platform states are:

- **Healthy** — required capabilities available.
- **Degraded** — core usable, one or more optional or reduced capabilities.
- **Recovery Required** — project integrity or core storage requires attention.
- **Unavailable** — minimum safe capabilities are not available.

---

## 14.3 Diagnostic Bundle

Opure SHOULD be able to produce a local diagnostic bundle containing:

- runtime identity;
- version information;
- service states;
- safe configuration summary;
- recent errors;
- crash metadata;
- resource summary;
- and relevant Trust Centre references.

The bundle MUST exclude secrets by default.

The developer MUST be able to inspect the bundle before sharing it.

---

## 14.4 Logs

Runtime logs SHOULD be structured.

A log entry SHOULD include:

- timestamp;
- severity;
- runtime instance;
- service;
- operation;
- project identifier where appropriate;
- correlation identifier;
- safe message;
- and structured metadata.

Logs MUST NOT contain secret values.

Logs SHOULD use bounded retention.

---

## 14.5 Log Levels

Recommended levels are:

- Trace;
- Debug;
- Information;
- Warning;
- Error;
- Critical.

Production defaults SHOULD avoid excessive Trace and Debug logging.

Temporary detailed logging MAY be enabled visibly for diagnosis.

---

## 14.6 Crash Reporting

Crash reports MUST be stored locally by default.

Automatic external crash upload MUST be disabled by default.

Any future upload capability MUST:

- require visible approval;
- show the proposed data;
- redact sensitive content;
- and respect project cloud policy.

---

# 15. Failure Handling

## 15.1 Failure Categories

Failures SHOULD be classified as:

- configuration failure;
- dependency failure;
- permission failure;
- policy denial;
- resource exhaustion;
- provider failure;
- external-tool failure;
- plugin failure;
- data-integrity failure;
- contract incompatibility;
- timeout;
- cancellation;
- and unexpected internal error.

Classification MUST NOT conceal the original evidence.

---

## 15.2 Critical Failure

A critical failure is one that threatens:

- project integrity;
- secrets;
- permission enforcement;
- audit integrity;
- or safe operation.

After a critical failure, the Runtime Kernel MUST prefer stopping affected operations over continuing unsafely.

---

## 15.3 Degraded Operation

Opure SHOULD continue in degraded mode when:

- an optional provider is unavailable;
- a plugin fails;
- an MCP server disconnects;
- a non-essential index is rebuilding;
- or an optional integration is disabled.

The developer MUST be told which capabilities are unavailable.

---

## 15.4 Circuit Breakers

Repeated failure of an external or optional component SHOULD trigger a circuit breaker.

The circuit breaker SHOULD:

- stop repeated failing calls;
- preserve the last error;
- set a retry time or require manual action;
- and make the disabled capability visible.

---

## 15.5 Bulkhead Isolation

High-risk workloads SHOULD be isolated so resource exhaustion or failure does not spread.

Examples include:

- model inference;
- plugin execution;
- builds;
- package installation;
- browser automation;
- and untrusted project scripts.

---

# 16. Resource Management

## 16.1 Resource Monitoring

The Runtime Kernel SHOULD monitor:

- CPU usage;
- memory usage;
- GPU utilisation;
- VRAM usage;
- storage usage;
- disk activity;
- network activity;
- process count;
- queue depth;
- and task latency.

Monitoring frequency SHOULD balance visibility with overhead.

---

## 16.2 Resource Budgets

Services and tasks MAY be assigned resource budgets.

Budgets SHOULD support:

- soft warning threshold;
- throttling threshold;
- and hard safety limit.

A hard limit MUST NOT be used where abrupt termination would risk project corruption without a recovery mechanism.

---

## 16.3 Memory Pressure

Under memory pressure, Opure SHOULD:

1. pause or reduce idle work;
2. release caches;
3. unload unused models;
4. reduce concurrency;
5. warn the developer where necessary;
6. and preserve interactive responsiveness.

---

## 16.4 GPU and VRAM Pressure

Under VRAM pressure, Opure SHOULD coordinate model unloading and task delay rather than allow uncontrolled failure.

It MUST NOT assume all detected GPU memory is available exclusively to Opure.

---

## 16.5 Storage Pressure

Opure SHOULD monitor storage used by:

- models;
- indexes;
- project memory;
- logs;
- backups;
- snapshots;
- build artefacts;
- and downloads.

The developer SHOULD be able to inspect and safely clean managed storage.

Destructive cleanup MUST identify what will be removed.

---

# 17. Persistence and Runtime State

## 17.1 Runtime State Categories

Runtime state SHOULD be classified as:

- ephemeral;
- session;
- recoverable;
- durable;
- and security-sensitive.

Each category MUST have clear storage and cleanup rules.

---

## 17.2 Durable State

Durable state MAY include:

- service configuration;
- project registration;
- workflow checkpoints;
- pending transactions;
- health history;
- migration state;
- and audit references.

Durable state MUST use transaction-safe storage.

---

## 17.3 Journalling

Operations that may leave partial state SHOULD use a journal or transaction record.

Examples include:

- patch application;
- database migration;
- plugin installation;
- model download;
- project import;
- and configuration update.

The journal MUST support detection of incomplete operations after restart.

---

## 17.4 Temporary Files

Temporary files MUST be created in managed locations.

They SHOULD:

- have traceable ownership;
- avoid predictable insecure names;
- be cleaned after normal completion;
- and be recoverable or safely removable after a crash.

Temporary files containing sensitive information MUST receive stronger protection.

---

## 17.5 Single-Instance Behaviour

The desktop Opure application SHOULD prevent accidental conflicting access by multiple runtime instances using the same user data.

Multiple instances MAY be supported in the future if they use explicit isolated profiles.

The Runtime Kernel MUST detect stale locks after crashes safely.

---

# 18. Security Enforcement Hooks

## 18.1 Permission Context

Every significant command MUST carry or resolve a permission context.

The permission context SHOULD include:

- initiating user;
- project;
- workflow;
- plugin or service;
- granted capabilities;
- approval reference;
- and expiry where applicable.

---

## 18.2 Policy Evaluation

Before a protected action, the Runtime Kernel MUST invoke the Policy Engine.

Protected actions include:

- filesystem write;
- command execution;
- network access;
- secret access;
- external data sharing;
- plugin installation;
- permission escalation;
- and irreversible operations.

The AI model's recommendation MUST NOT substitute for deterministic policy evaluation.

---

## 18.3 Approval Tokens

A developer approval SHOULD create a bounded approval token or record.

An approval token SHOULD specify:

- action;
- scope;
- project;
- target;
- data category;
- expiry;
- whether it is single-use;
- and revocation state.

Broad or indefinite approval MUST NOT be the default.

---

## 18.4 Secret Access

Services MUST request secrets through the Secrets Vault interface.

The Runtime Kernel MUST ensure that secret access is:

- permission checked;
- scoped;
- auditable without recording the value;
- and revocable.

---

## 18.5 Network Access

Network requests MUST pass through the Network Gateway or approved equivalent.

The Runtime Kernel MUST NOT permit plugins or models to bypass network policy through undeclared direct sockets where enforceable.

Detailed network security belongs to SPEC-008.

---

# 19. Operating-System Integration

## 19.1 Windows-First Behaviour

The initial Runtime Kernel SHOULD use Windows-native capabilities where they provide clear security or reliability benefits.

Potential examples include:

- Windows credential protection;
- job objects;
- named pipes;
- access-control lists;
- file-change notifications;
- process isolation;
- and event logging integrations.

Use of a Windows capability MUST be isolated behind an interface where practical.

---

## 19.2 File Paths

The Runtime Kernel MUST handle Windows path behaviour correctly, including:

- drive letters;
- UNC paths where supported;
- case-insensitive comparisons;
- reserved names;
- long paths;
- junctions;
- symbolic links;
- and path traversal prevention.

Path normalisation MUST NOT cause Opure to access a different file than the developer intended.

---

## 19.3 Process Execution

External process execution MUST:

- use explicit executable resolution;
- avoid unsafe shell interpolation;
- separate arguments from command text;
- capture output safely;
- support cancellation;
- enforce working-directory boundaries;
- and apply policy before execution.

Elevated execution requires explicit approval.

---

## 19.4 Environment Variables

The Runtime Kernel SHOULD construct a controlled environment for external processes.

Sensitive environment variables MUST NOT be inherited unnecessarily.

Project-specific environment data SHOULD be explicit and inspectable without revealing secret values.

---

# 20. Runtime API Surface

## 20.1 Runtime Control API

The Runtime Kernel SHOULD expose a versioned local control API for the desktop interface and authorised tools.

Capabilities MAY include:

- query runtime status;
- list services;
- read health;
- start or stop optional services;
- inspect task queues;
- cancel tasks;
- inspect resource use;
- enter safe mode;
- request shutdown;
- and create diagnostic bundles.

---

## 20.2 Authentication

Local API access MUST be authenticated.

Binding only to localhost is not sufficient authentication by itself.

Authentication SHOULD use operating-system identity, scoped local tokens or secure IPC credentials.

---

## 20.3 Authorisation

Authenticated callers MUST still be authorised for each capability.

A desktop process MUST NOT automatically receive unrestricted access merely because it is part of the same installation.

---

## 20.4 Transport

The initial local transport MAY use:

- named pipes;
- local sockets;
- loopback HTTP;
- in-process calls;
- or another documented local mechanism.

The logical API contract MUST remain transport independent.

Transport selection is DEFERRED to an ADR.

---

# 21. Trust Centre Integration

## 21.1 Audit Buffer

The Runtime Kernel MUST provide a temporary audit buffer before the Trust Centre service becomes available.

Buffered significant events MUST be flushed once durable recording is healthy.

If durable audit recording is unavailable, protected high-risk actions SHOULD be blocked or require explicit degraded-mode approval.

---

## 21.2 Required Runtime Records

The Runtime Kernel SHOULD record:

- runtime start;
- selected runtime mode;
- previous abnormal shutdown;
- service start and failure;
- policy denial;
- approval creation and use;
- protected command execution;
- forced service termination;
- recovery action;
- configuration change;
- and runtime shutdown.

---

## 21.3 Sensitive Data

Audit records MUST NOT include:

- secret values;
- private keys;
- passwords;
- unredacted tokens;
- or unnecessary project content.

A hash, identifier or data-category description SHOULD be used where sufficient.

---

# 22. Testing Requirements

## 22.1 Unit Testing

The Runtime Kernel MUST have unit tests for:

- dependency resolution;
- lifecycle transitions;
- restart policies;
- configuration precedence;
- policy hook invocation;
- message validation;
- cancellation;
- and health aggregation.

---

## 22.2 Integration Testing

Integration tests SHOULD cover:

- clean startup;
- startup with optional service failure;
- startup with critical service failure;
- graceful shutdown;
- forced shutdown;
- abnormal previous shutdown;
- recovery mode;
- configuration migration;
- plugin crash isolation;
- external-process timeout;
- and Trust Centre unavailability.

---

## 22.3 Fault Injection

The Runtime Kernel SHOULD support fault-injection testing.

Fault scenarios SHOULD include:

- service hangs;
- service crashes;
- disk full;
- corrupted configuration;
- unavailable database;
- network denial;
- GPU exhaustion;
- memory pressure;
- invalid contract version;
- and incomplete transaction journal.

---

## 22.4 Deterministic Test Mode

Test Mode SHOULD provide:

- deterministic identifiers where appropriate;
- controllable clock;
- fake resource readings;
- in-memory or temporary storage;
- simulated services;
- predictable scheduler behaviour;
- and suppressed external network access.

---

## 22.5 Recovery Testing

Recovery paths MUST be tested as first-class product behaviour.

A feature is incomplete if its normal path is tested but its interrupted path is not.

---

# 23. Performance Requirements

## 23.1 Startup

The Runtime Kernel SHOULD minimise time to minimum safe readiness.

Optional services SHOULD NOT unnecessarily delay the desktop from opening a local project.

Exact startup targets are DEFERRED until a measurable prototype exists.

---

## 23.2 Idle Cost

When no project work is active, the Runtime Kernel SHOULD consume minimal CPU and GPU resources.

Background indexing and maintenance SHOULD respect performance mode and idle policy.

---

## 23.3 Scheduling Latency

Interactive tasks SHOULD enter execution promptly unless blocked by a visible resource, policy or dependency reason.

---

## 23.4 Diagnostic Overhead

Health monitoring, logging and auditing MUST remain efficient enough that visibility does not materially degrade ordinary use.

---

# 24. Initial Implementation Milestone

The first Runtime Kernel milestone is successful when it can:

1. start on Windows 11;
2. create a unique runtime identity;
3. load validated configuration;
4. initialise secure structured logging;
5. register mock core services;
6. validate service dependencies;
7. start services in dependency order;
8. report service health;
9. schedule foreground and background tasks;
10. cancel a running task;
11. isolate a failing optional worker;
12. record significant activity;
13. perform graceful shutdown;
14. detect an abnormal previous shutdown;
15. enter Recovery Mode;
16. and complete all tests without requiring an AI provider.

The milestone SHOULD use mock or placeholder services where later specifications are not yet implemented.

---

# 25. Acceptance Criteria

SPEC-002 is implemented when all of the following are true:

- [ ] Runtime startup is deterministic and dependency-aware.
- [ ] Runtime shutdown is ordered and recoverable.
- [ ] AI availability is not required for core readiness.
- [ ] Services expose versioned contracts and health.
- [ ] Required and optional service failures are handled differently.
- [ ] Repeated crashes do not create infinite restart loops.
- [ ] Protected actions invoke deterministic policy evaluation.
- [ ] Significant runtime actions are auditable.
- [ ] Secret values do not appear in logs or audit records.
- [ ] External processes can be cancelled and isolated.
- [ ] The Scheduler responds to resource pressure.
- [ ] Project contexts remain isolated.
- [ ] Recovery detects incomplete prior operations.
- [ ] Safe Mode can start without third-party integrations.
- [ ] The runtime API is authenticated and authorised.
- [ ] Windows-specific behaviour is isolated behind interfaces where practical.
- [ ] Unit, integration, fault-injection and recovery tests pass.

---

# 26. Deferred Decisions

The following decisions are intentionally deferred:

- implementation language;
- desktop-to-runtime transport;
- event-bus library;
- process-hosting model;
- container support;
- Windows service integration;
- exact storage engine;
- exact logging library;
- exact dependency-injection framework;
- exact scheduler implementation;
- crash-dump format;
- update mechanism;
- Linux and macOS runtime ports;
- and public headless-runtime support.

These decisions MUST be resolved through later specifications or Architecture Decision Records.

---

# 27. Required Architecture Decision Records

The implementation of SPEC-002 SHOULD produce ADRs for:

- Runtime implementation language;
- In-process versus multi-process service boundaries;
- Local IPC transport;
- Service contract format;
- Event and message transport;
- Scheduler technology;
- Windows process-isolation strategy;
- Runtime storage and journalling;
- Structured logging technology;
- and crash-recovery strategy.

---

# 28. Relationship to Other Specifications

This specification provides the runtime foundation for:

- **SPEC-003 — Service Architecture**
- **SPEC-004 — AI Router**
- **SPEC-005 — Memory Engine**
- **SPEC-006 — Workflow and Agent System**
- **SPEC-007 — Plugin SDK**
- **SPEC-008 — Trust Centre and Security**
- **SPEC-009 — Workspace and File Patch Engine**
- **SPEC-010 — Desktop User Interface**
- **SPEC-011 — Project and Build Management**

SPEC-003 will define service contracts and boundaries in greater detail.

SPEC-008 will define security, policy, permissions, audit and secrets behaviour in greater detail.

Where later specifications introduce stricter safety requirements, the stricter requirement takes precedence unless it conflicts with CHARTER-001.

---

# 29. Founder Approval

This document remains a founder draft until explicitly approved.

Approval establishes the Runtime Kernel as:

- deterministic infrastructure;
- independent from AI-provider availability;
- responsible for service lifecycle and health;
- responsible for safe scheduling and recovery;
- the enforcement entry point for permissions and policy;
- and the observable foundation of the Opure Platform.

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**