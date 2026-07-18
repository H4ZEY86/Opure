# ADR-0003 — Runtime Process Topology

## Architecture Decision Record

**Status:** Proposed  
**Date:** 18 July 2026  
**Decision owners:** Founder and Product Owner  
**Technical owners:** Runtime Architecture Owner  
**Reviewers:** Security Owner, Desktop Owner, Plugin Owner, Performance Owner, Recovery Owner  
**Supersedes:** None  
**Superseded by:** None  
**Related ADRs:** ADR-0001 Primary Implementation Language, ADR-0002 Desktop Framework, ADR-0004 Local IPC, ADR-0005 Persistence  
**Related specifications:** CHARTER-001, SPEC-001, SPEC-002, SPEC-003, SPEC-004, SPEC-006, SPEC-007, SPEC-008, SPEC-009, SPEC-010, SPEC-011, SPEC-012  
**Target milestone:** Phase 0 — Founding Baseline and Phase 1 — Architecture Skeleton  

---

## 1. Decision Summary

Opure should use a **hybrid supervised process topology**.

The first implementation should contain:

1. a separate **Opure Desktop** process;
2. one primary **Opure Runtime** process hosting trusted first-party services as strongly separated in-process modules;
3. one or more supervised **trusted worker processes** for CPU-heavy, failure-prone or separately bounded operations;
4. isolated **Plugin Host processes** for third-party plugin execution;
5. mediated external **MCP server processes or endpoints**;
6. external **AI provider processes or endpoints**, including Ollama;
7. controlled child processes for builds, tests, Git, package managers and other command-line tools;
8. and a minimal **bootstrap or recovery helper** only where installation, update or damaged-Runtime recovery requires it.

Opure should **not** begin with one operating-system process per logical service.

Service boundaries remain real and enforceable through contracts, ownership rules, architecture tests and persistence isolation even when trusted services share the Runtime process.

A service may move into its own process later when evidence demonstrates a security, reliability, resource or deployment need.

---

## 2. Status

Current status:

> **Proposed**

This ADR becomes **Accepted** only after a topology prototype demonstrates:

- safe Runtime startup and shutdown;
- Desktop independence;
- worker supervision;
- crash containment;
- process-tree cancellation;
- Plugin Host isolation;
- Runtime restart and desktop reconnection;
- durable operation recovery;
- bounded resource use;
- authenticated local communication;
- and acceptable startup and idle performance.

---

## 3. Context

Opure is one local desktop product composed of many logical services.

The architecture defines services such as:

- Runtime Kernel;
- Project Manager;
- Workspace Service;
- Patch Service;
- Build Manager;
- Repository Service;
- AI Router;
- Knowledge Engine;
- Workflow Engine;
- Policy Engine;
- Secrets Vault;
- Trust Centre;
- Plugin Manager;
- MCP Gateway;
- and Desktop Gateway.

Logical service separation does not automatically require one operating-system process per service.

A small team must balance:

- security isolation;
- fault containment;
- performance;
- startup time;
- memory use;
- deployment simplicity;
- debugging;
- contract discipline;
- plugin risk;
- external process control;
- and future replacement.

A single monolithic process would be simple, but it would place desktop rendering, third-party plugins, heavy work and trusted services inside one failure boundary.

A process-per-service design would maximise isolation, but it would add substantial IPC, deployment, diagnostics, startup, compatibility and recovery complexity before Opure has proven its first vertical slice.

The architecture therefore requires a topology that is modular without becoming prematurely distributed.

---

## 4. Problem Statement

Opure requires a process topology that keeps the desktop responsive, protects the trusted core from third-party and external failures, supports supervised long-running work and future service extraction, while remaining practical for a small Windows-first implementation.

---

## 5. Decision Drivers

The topology is evaluated against:

- alignment with the Opure Charter;
- developer control;
- transparent operation;
- Windows 11 behaviour;
- startup time;
- idle resource use;
- crash containment;
- third-party plugin isolation;
- process-tree control;
- cancellation;
- recovery;
- project integrity;
- secret isolation;
- local API security;
- AI provider independence;
- MCP mediation;
- build and test process control;
- service ownership;
- contract enforceability;
- debugging;
- testability;
- packaging;
- updates;
- cross-platform viability;
- small-team delivery;
- operational simplicity;
- future scalability;
- and replacement cost.

---

## 6. Governing Principles

This decision must preserve:

- **Developer Respect**
- **Developer First**
- **Software Engineering First**
- **Local by Design**
- **Cloud Optional**
- **Human in Control**
- **Visible by Design**
- **Inspectable Decisions**
- **Reviewable Changes**
- **Reversible Wherever Technically Practical**
- **Open by Architecture**
- **Loose Coupling**
- **Least Privilege**
- **Project Isolation**
- **Performance Respect**
- **No Hidden Authority**
- **No Silent Recovery**
- **Honest State**

Specific architecture requirements include:

- The Runtime must operate without AI.
- The desktop must not own authoritative domain state.
- Third-party plugin code must run outside the trusted core.
- AI providers remain external adapters or endpoints.
- MCP access remains mediated.
- Build and repository tools remain controlled external processes.
- Protected file changes pass through the Patch Service.
- Secrets remain owned by the Secrets Vault.
- State-changing operations require durable recovery.
- Optional component failure must not crash the entire product.
- Service boundaries must remain replaceable even when in process.

---

## 7. Scope

This ADR decides:

- which primary Opure components run in separate processes;
- which trusted services may initially share a process;
- which workloads require worker isolation;
- how processes are supervised;
- how failure boundaries are defined;
- how process identity is represented;
- how process shutdown and restart are coordinated;
- how child-process trees are controlled;
- how Safe Mode changes topology;
- and how services may be extracted later.

This ADR does not decide:

- the exact IPC transport;
- the exact serialisation format;
- the storage engine;
- the desktop framework;
- the installer;
- the update mechanism;
- the plugin wire protocol;
- the MCP protocol itself;
- the AI provider protocol;
- container support;
- remote workers;
- or distributed team services.

---

## 8. Constraints

Known constraints include:

- Windows 11 is the first supported platform.
- C#/.NET is the proposed primary implementation platform.
- Avalonia is the proposed desktop framework.
- The desktop and Runtime must be independently restartable.
- The platform must work offline.
- The platform must remain usable if AI is unavailable.
- The first implementation must be practical for a small team.
- Plugins must not run inside the trusted Runtime.
- Build and package scripts are untrusted executable content.
- Localhost does not equal trusted access.
- State-changing operations must survive desktop failure.
- Process cancellation must include child processes.
- Resource use must respect local models and developer workloads.
- Cross-platform portability should be preserved.
- The topology must not require a permanent cloud control plane.
- Logical service boundaries must remain explicit even when sharing a process.

---

## 9. Assumptions

This decision assumes:

- The first usable product runs on one local machine.
- One developer is the primary interactive user.
- The Runtime owns authoritative platform state.
- The desktop may be closed while selected Runtime work continues only when explicitly allowed.
- Most first-party services are trusted and maintained together.
- Third-party plugins are not trusted.
- MCP servers are external trust boundaries.
- AI providers usually run outside Opure.
- Builds, tests and package managers run as external processes.
- CPU-heavy indexing or parsing may benefit from worker isolation.
- Process extraction should be driven by evidence rather than fashion.
- The Desktop Gateway and service contracts will permit process separation later.
- Windows Job Objects or equivalent platform process-group mechanisms can be used for child-process control.
- Durable state, not process memory, determines recovery.

---

## 10. Options Considered

The principal options are:

1. **Option A — Single Process**
2. **Option B — Process per Logical Service**
3. **Option C — Hybrid Supervised Runtime**
4. **Option D — Desktop-Hosted Runtime**
5. **Option E — Containerised Local Services**
6. **Option F — Distributed Local Daemons**

---

# 11. Option A — Single Process

## 11.1 Description

Run:

- desktop;
- Runtime;
- trusted services;
- plugins;
- workflow execution;
- and most adapters

inside one operating-system process.

External provider and build tools remain separate only when unavoidable.

---

## 11.2 Advantages

- Simplest deployment.
- Minimal IPC.
- Minimal serialisation overhead.
- Easy shared debugging.
- Low process count.
- Fast initial startup.
- Simple in-memory event routing.
- Easy access to shared models.
- Fewer version-compatibility problems.

---

## 11.3 Disadvantages

- Desktop crash can terminate authoritative state.
- Runtime failure terminates the UI.
- Plugin crash can terminate the whole product.
- Memory corruption or native library failure has broad impact.
- Heavy work can affect UI responsiveness.
- Service boundaries may become informal.
- Restarting one subsystem requires restarting everything.
- Memory cannot be reclaimed through worker termination.
- Security boundaries depend only on code discipline.
- Safe Mode becomes more difficult.
- Long-running work cannot continue if the desktop closes.
- Process privileges are shared.
- Third-party dependencies expand the trusted computing base.

---

## 11.4 Risks

- The platform becomes a large desktop monolith.
- Direct service calls replace explicit contracts.
- Plugins gain accidental authority.
- UI state becomes authoritative.
- Failures create project-integrity risk.
- Native provider or parser crashes take down the product.
- Recovery cannot distinguish desktop failure from Runtime failure cleanly.

---

## 11.5 Estimated Adoption Cost

- **Initial implementation:** Low
- **Operational complexity:** Low
- **Migration difficulty:** High
- **Replacement difficulty:** High

---

# 12. Option B — Process per Logical Service

## 12.1 Description

Run each major service in its own process.

Examples:

- Project Manager process;
- Workspace process;
- Patch process;
- AI Router process;
- Knowledge process;
- Workflow process;
- Trust process;
- and so on.

---

## 12.2 Advantages

- Strong failure isolation.
- Strong process-level ownership.
- Independent restart.
- Independent resource accounting.
- Explicit IPC contracts.
- Easier future service replacement.
- Stronger privilege separation.
- Smaller per-process trusted surface.
- Potential independent deployment or scaling.

---

## 12.3 Disadvantages

- High process count.
- High IPC volume.
- High serialisation complexity.
- More startup coordination.
- More shutdown complexity.
- More packaging complexity.
- More compatibility management.
- More debugging complexity.
- More logs and traces.
- More partial-failure states.
- Greater memory overhead.
- More authentication relationships.
- More version-skew risk.
- More recovery coordination.
- Disproportionate for one local user and a small team.

---

## 12.4 Risks

- Opure becomes a distributed system on one machine.
- Delivery slows before product value exists.
- Engineers spend time on transport rather than engineering features.
- Failure combinations multiply.
- Shared transactions become difficult.
- Local performance degrades.
- Upgrades require complex compatibility choreography.

---

## 12.5 Estimated Adoption Cost

- **Initial implementation:** Very High
- **Operational complexity:** Very High
- **Migration difficulty:** Moderate
- **Replacement difficulty:** Moderate

---

# 13. Option C — Hybrid Supervised Runtime

## 13.1 Description

Use:

- a separate desktop process;
- one primary trusted Runtime process;
- trusted services as in-process modules;
- supervised trusted workers for selected heavy or failure-prone work;
- isolated Plugin Host processes;
- external MCP and AI processes or endpoints;
- controlled command processes;
- and a minimal recovery or update helper where required.

---

## 13.2 Advantages

- Separates UI failure from authoritative Runtime state.
- Keeps trusted service calls efficient.
- Avoids process-per-service complexity.
- Provides real isolation for third-party plugins.
- Allows heavy tasks to be terminated and memory reclaimed.
- Supports process-tree cancellation.
- Supports Safe Mode.
- Supports Runtime restart and desktop reconnect.
- Preserves future service extraction through contracts.
- Keeps startup and deployment manageable.
- Enables resource accounting by process class.
- Maintains one coherent local product.
- Supports external provider independence.
- Fits a small-team delivery model.
- Provides bounded native and parser failure domains.
- Allows trusted services to share transactions where appropriate through owned abstractions.
- Supports plugin-host quarantine.
- Supports multiple isolated workers only when needed.

---

## 13.3 Disadvantages

- Requires both in-process and IPC communication models.
- Requires supervision logic.
- Requires process identity and authentication.
- Worker protocol must be designed.
- Some failures remain shared inside the trusted Runtime.
- Service boundaries need architecture tests because process separation does not enforce them automatically.
- Worker lifecycle adds operational states.
- Packaging includes several executables.
- Debugging spans multiple processes.
- Desktop and Runtime version compatibility must be managed.
- Privilege separation remains limited inside the trusted Runtime.
- Later extraction of a service may still require adaptation.

---

## 13.4 Risks

- Too many operations may be moved to workers prematurely.
- Too few operations may be isolated.
- Shared Runtime modules may begin making direct calls.
- Generic workers may accumulate excessive capabilities.
- Plugin hosts may be grouped too broadly.
- Runtime restart may be mistaken for operation recovery.
- Desktop may present stale state after reconnect.
- Worker protocol may become a hidden public API.
- Process privileges may be broader than required.
- Native components may still crash the trusted Runtime if loaded there.

---

## 13.5 Mitigations

- Define explicit extraction criteria.
- Maintain contract-first service boundaries.
- Use architecture tests.
- Keep workers capability-scoped.
- Use one Plugin Host process per plugin instance initially where practical.
- Keep native third-party code outside the Runtime.
- Use durable operation journals.
- Reconcile state after reconnect.
- Version all worker contracts.
- Separate health from readiness.
- Use platform job-control primitives.
- Record process activity in the Trust Centre.
- Review process placement through ADRs.

---

## 13.6 Estimated Adoption Cost

- **Initial implementation:** Moderate
- **Operational complexity:** Moderate
- **Migration difficulty:** Low to Moderate
- **Replacement difficulty:** Low to Moderate

---

# 14. Option D — Desktop-Hosted Runtime

## 14.1 Description

Run the Runtime as part of the desktop process but isolate plugins and external tools.

---

## 14.2 Advantages

- Simpler than separate Desktop and Runtime.
- Lower IPC overhead.
- Easier startup.
- Fewer executables.
- Easier local debugging.
- Some external isolation remains.

---

## 14.3 Disadvantages

- Desktop crash terminates workflows.
- UI restart terminates authoritative services.
- Long-running work depends on the window process.
- Safe Mode and recovery are coupled to UI startup.
- UI framework failure affects domain services.
- Desktop updates require Runtime shutdown.
- Headless diagnostics and repair are harder.
- Future alternate clients are harder.
- Desktop may become too authoritative.

---

## 14.4 Risks

- Violates the intended command-and-projection boundary.
- Encourages domain-state ownership in the UI.
- Makes recovery dependent on the main shell.
- Limits future CLI or alternate-client use.

---

## 14.5 Estimated Adoption Cost

- **Initial implementation:** Low to Moderate
- **Operational complexity:** Low
- **Migration difficulty:** High
- **Replacement difficulty:** High

---

# 15. Option E — Containerised Local Services

## 15.1 Description

Run Runtime services in local containers.

The desktop communicates with containerised service endpoints.

---

## 15.2 Advantages

- Strong process and filesystem isolation.
- Reproducible service environments.
- Independent service packaging.
- Clear network boundaries.
- Potential Linux-service reuse on Windows.
- Easier dependency containment.

---

## 15.3 Disadvantages

- Requires container runtime.
- Adds substantial installation complexity.
- Conflicts with a lightweight local desktop product.
- File access across container boundaries is complex.
- GPU access can be complex.
- Windows filesystem semantics may be altered.
- Startup time increases.
- Resource overhead increases.
- Diagnostics become harder.
- Offline installation becomes larger.
- Container runtime becomes a hidden dependency.
- User projects may need broad mounts.
- Not appropriate for the first local desktop milestone.

---

## 15.4 Risks

- Opure becomes dependent on Docker or equivalent.
- Local-only no longer means simple local installation.
- Workspace security becomes mount-security complexity.
- Support burden increases.
- Container networking obscures destination visibility.

---

## 15.5 Estimated Adoption Cost

- **Initial implementation:** Very High
- **Operational complexity:** Very High
- **Migration difficulty:** High
- **Replacement difficulty:** High

---

# 16. Option F — Distributed Local Daemons

## 16.1 Description

Install several persistent operating-system daemons or services that communicate independently and may continue without an interactive user session.

---

## 16.2 Advantages

- Services can run continuously.
- Strong separation from desktop.
- Independent lifecycle.
- Suitable for future background or team functionality.
- Can serve multiple local clients.

---

## 16.3 Disadvantages

- Installation and privilege complexity.
- User-session security complexity.
- Background resource use.
- Harder uninstall.
- Harder update choreography.
- Greater attack surface.
- Multiple-user project isolation becomes necessary.
- Hidden background work becomes easier.
- Not required for the first product.
- Conflicts with explicit user-controlled execution.

---

## 16.4 Risks

- Opure appears to perform work when the user is not aware.
- Services run with excessive privileges.
- Projects from different users are mixed.
- Support and recovery become difficult.
- A local desktop product becomes a machine-wide platform prematurely.

---

## 16.5 Estimated Adoption Cost

- **Initial implementation:** High
- **Operational complexity:** High
- **Migration difficulty:** Moderate
- **Replacement difficulty:** High

---

# 17. Comparison Matrix

Scores:

- **1** — poor
- **2** — weak
- **3** — acceptable
- **4** — strong
- **5** — excellent

| Criterion | Weight | Single Process | Process per Service | Hybrid | Desktop Hosted | Containers | Local Daemons |
|---|---:|---:|---:|---:|---:|---:|---:|
| Charter alignment | 5 | 2 | 4 | 5 | 2 | 3 | 2 |
| Small-team delivery | 5 | 5 | 1 | 4 | 5 | 1 | 2 |
| Desktop independence | 5 | 1 | 5 | 5 | 1 | 5 | 5 |
| Plugin isolation | 5 | 1 | 5 | 5 | 4 | 5 | 5 |
| Trusted-service efficiency | 4 | 5 | 2 | 5 | 5 | 2 | 2 |
| Crash containment | 5 | 1 | 5 | 4 | 2 | 5 | 5 |
| Startup performance | 4 | 5 | 2 | 4 | 5 | 1 | 3 |
| Idle resource use | 4 | 5 | 2 | 4 | 5 | 1 | 2 |
| Recovery | 5 | 2 | 4 | 5 | 2 | 4 | 4 |
| Debuggability | 4 | 5 | 2 | 4 | 5 | 2 | 2 |
| Packaging | 4 | 5 | 2 | 4 | 5 | 1 | 2 |
| Contract enforcement | 5 | 2 | 5 | 4 | 2 | 5 | 5 |
| Future extraction | 4 | 2 | 5 | 5 | 2 | 5 | 5 |
| Process-tree control | 4 | 3 | 4 | 5 | 3 | 3 | 4 |
| Windows-first fit | 4 | 4 | 3 | 5 | 4 | 2 | 3 |
| Cross-platform viability | 3 | 4 | 4 | 5 | 4 | 4 | 4 |
| Security reviewability | 5 | 2 | 4 | 5 | 2 | 4 | 3 |
| Operational simplicity | 5 | 5 | 1 | 4 | 5 | 1 | 2 |
| Replacement cost | 3 | 2 | 4 | 5 | 2 | 3 | 3 |
| **Indicative weighted total** |  | **290** | **304** | **432** | **294** | **276** | **302** |

The hybrid topology provides the strongest overall balance.

---

# 18. Decision

Opure will use a **hybrid supervised Runtime topology**.

The first implementation will contain these process classes:

```text
Opure.Desktop
Opure.Runtime
Opure.Worker
Opure.PluginHost
External AI Provider
External MCP Server
Controlled Command Process
Opure.Bootstrap or Recovery Helper
```

The initial process boundary is:

```text
┌───────────────────────────────────────────────────────────────┐
│ Opure.Desktop                                                 │
│                                                               │
│ Avalonia shell, views, view models, Gateway client            │
└───────────────────────────────┬───────────────────────────────┘
                                │ Authenticated local IPC
                                ▼
┌───────────────────────────────────────────────────────────────┐
│ Opure.Runtime                                                 │
│                                                               │
│ Runtime Kernel                                                │
│ Desktop Gateway                                               │
│ Project Manager                                               │
│ Workspace Service                                             │
│ Patch Service                                                 │
│ Build Manager coordination                                    │
│ Repository Service coordination                               │
│ Dependency Manager                                            │
│ Environment Manager                                           │
│ Artefact Manager                                              │
│ AI Router                                                     │
│ Context Engine                                                │
│ Knowledge Engine coordination                                 │
│ Workflow Engine                                               │
│ Policy Engine                                                 │
│ Approval Manager                                              │
│ Secrets Vault                                                 │
│ Network Gateway                                               │
│ Trust Centre                                                  │
│ Plugin Manager                                                │
│ MCP Gateway                                                   │
└───────────────┬───────────────────────┬───────────────────────┘
                │                       │
                ▼                       ▼
┌───────────────────────────┐  ┌───────────────────────────────┐
│ Trusted Worker Processes  │  │ Plugin Host Processes         │
│                           │  │                               │
│ parsing, indexing,        │  │ third-party plugin code       │
│ diff, optional native,    │  │ scoped capability protocol    │
│ heavy deterministic work │  │ per-plugin isolation          │
└───────────────────────────┘  └───────────────────────────────┘
                │                       │
                └──────────────┬────────┘
                               ▼
┌───────────────────────────────────────────────────────────────┐
│ External Processes and Endpoints                              │
│                                                               │
│ Ollama / AI providers                                         │
│ MCP servers                                                   │
│ Git                                                           │
│ build tools                                                   │
│ test tools                                                    │
│ package managers                                              │
│ external APIs                                                 │
└───────────────────────────────────────────────────────────────┘
```

This decision is:

- [ ] Permanent until superseded
- [x] Provisional pending prototype evidence
- [ ] Experimental only
- [ ] Limited to Phase 1
- [ ] Limited to Windows

---

# 19. Rationale

The hybrid topology is preferred because it provides:

- a separate desktop failure boundary;
- one coherent trusted Runtime;
- low-overhead trusted-service collaboration;
- real third-party isolation;
- bounded worker failure;
- process-tree cancellation;
- future service extraction;
- and manageable deployment.

It avoids two extremes:

- a desktop monolith with weak isolation;
- and a process-per-service architecture with premature distributed-system complexity.

The trusted Runtime remains modular.

Sharing a process does not grant services permission to bypass contracts, ownership or policy.

---

# 20. Process Inventory

## 20.1 Opure.Desktop

Responsibilities:

- render the user interface;
- maintain view state;
- send commands;
- receive projections and events;
- present approvals;
- display Trust Centre activity;
- and reconnect after Runtime restart.

It must not own authoritative domain state.

---

## 20.2 Opure.Runtime

Responsibilities:

- establish Runtime identity;
- load configuration;
- authenticate local clients;
- register services;
- supervise services and workers;
- own authoritative application state;
- schedule work;
- enforce policy;
- coordinate recovery;
- and expose the Desktop Gateway.

---

## 20.3 Opure.Worker

Responsibilities may include:

- parsing;
- indexing;
- embedding preparation;
- diff computation;
- syntax analysis;
- static analysis;
- archive processing;
- content transformation;
- or other bounded work.

Workers should not own authoritative long-lived state.

---

## 20.4 Opure.PluginHost

Responsibilities:

- load one approved plugin instance or a deliberately approved isolation group;
- expose only granted capabilities;
- enforce timeouts;
- enforce output limits;
- report health;
- and terminate safely.

Plugin hosts must not contain trusted secrets or unrestricted project access.

---

## 20.5 External AI Provider

Examples:

- Ollama;
- LM Studio;
- llama.cpp server;
- or remote provider endpoint.

The provider remains outside Opure's trusted core.

---

## 20.6 External MCP Server

An MCP server may be:

- local executable;
- local network endpoint;
- remote endpoint;
- or plugin-managed external capability.

It remains an external trust boundary.

---

## 20.7 Controlled Command Process

Examples:

- Git;
- compiler;
- test runner;
- package manager;
- formatter;
- linter;
- or build script.

Command processes run through controlled plans and job supervision.

---

## 20.8 Bootstrap or Recovery Helper

A minimal helper may support:

- initial Runtime launch;
- update replacement;
- rollback;
- damaged-install repair;
- process cleanup;
- or Recovery Mode startup.

It must remain small and independently reviewable.

---

# 21. Trusted Runtime Services

The following services may initially run in the primary Runtime process:

- Configuration Manager;
- Service Registry;
- Lifecycle Manager;
- Scheduler;
- Health Supervisor;
- Desktop Gateway;
- Project Manager;
- Workspace Service;
- Patch Service;
- Build Manager coordination;
- Repository Service coordination;
- Dependency Manager;
- Environment Manager;
- Artefact Manager;
- AI Router;
- Context Engine;
- Knowledge Engine coordination;
- Workflow Engine;
- Policy Engine;
- Approval Manager;
- Secrets Vault;
- Network Gateway;
- Trust Centre;
- Plugin Manager;
- MCP Gateway;
- and Recovery Coordinator.

This is not permission for direct internal implementation access.

Each service retains:

- contract boundary;
- owned state;
- owned persistence;
- stable errors;
- and test boundary.

---

# 22. In-Process Communication

Trusted Runtime services may use efficient in-process command, query and event dispatch.

The dispatcher must still enforce:

- service identity;
- command ownership;
- cancellation;
- correlation;
- policy hooks;
- error translation;
- and observability.

A service must not call another service's repository or database directly.

---

# 23. Worker Admission Criteria

A workload should move to a trusted worker when one or more conditions apply:

- it may crash due to native code;
- it may consume large temporary memory;
- it may require hard termination;
- it performs substantial CPU work;
- it parses untrusted complex input;
- it has a different privilege requirement;
- it uses an unstable dependency;
- it should release all memory on completion;
- it requires architecture-specific binaries;
- or independent scaling is useful.

A workload should remain in process when:

- it is low risk;
- it is IO coordination;
- it owns authoritative state;
- it requires many low-latency service interactions;
- or worker overhead exceeds benefit.

---

# 24. Worker Types

The initial implementation may use a generic worker executable with explicit modes.

Possible modes include:

- `parser`;
- `indexer`;
- `diff`;
- `scanner`;
- `archive`;
- `analysis`;
- and `native-adapter`.

A worker mode must declare:

- capabilities;
- input schema;
- output schema;
- resource class;
- cancellation behaviour;
- and security profile.

---

# 25. Generic Worker Limits

A generic worker must not become an unrestricted execution host.

It must not receive:

- arbitrary service credentials;
- unrestricted filesystem roots;
- unrestricted network access;
- raw Vault access;
- broad project authority;
- or arbitrary executable paths

unless a separate protected command plan explicitly authorises them.

---

# 26. Plugin Host Strategy

The initial safety preference is one Plugin Host process per active third-party plugin instance where practical.

A plugin instance is scoped to:

- plugin package;
- version;
- project where applicable;
- granted capabilities;
- and session.

Grouping multiple plugins into one process requires evidence that:

- trust level is equivalent;
- failure sharing is acceptable;
- privilege sets do not broaden each other;
- and resource accounting remains meaningful.

---

# 27. Plugin Host Restrictions

A Plugin Host must not:

- read project files directly;
- write project files directly;
- contact AI providers directly;
- contact MCP servers directly;
- access the Secrets Vault directly;
- access service databases;
- or create unsupervised child processes.

It requests capabilities through Runtime-mediated contracts.

---

# 28. AI Provider Topology

AI providers should remain separate processes or remote endpoints.

The Runtime should not load large model runtimes into the trusted core initially.

Benefits include:

- provider replacement;
- GPU-runtime isolation;
- model-memory independence;
- provider crash containment;
- and simpler licensing boundaries.

The AI Router owns provider coordination.

---

# 29. MCP Topology

MCP servers remain external.

The Runtime owns:

- server registration;
- session mediation;
- permission evaluation;
- schema validation;
- and activity recording.

A local MCP executable should be supervised where Opure launches it.

An independently launched MCP server remains externally managed.

---

# 30. Build and Test Topology

Build and test commands run as child processes under controlled execution.

The Runtime should:

- construct the command plan;
- validate policy;
- create the process;
- attach process-group control;
- stream output;
- monitor resource use;
- cancel the process tree;
- verify termination;
- and record results.

Build tools do not become trusted services merely because they are installed locally.

---

# 31. Git Topology

Git may initially run as a controlled external command-line process.

A future managed Git library may be evaluated separately.

The Repository Service owns:

- command planning;
- repository state interpretation;
- result validation;
- and recovery presentation.

Git hooks remain external executable risk.

---

# 32. Desktop Lifecycle

The desktop may:

- start the Runtime;
- connect to an existing compatible Runtime;
- request graceful Runtime shutdown;
- disconnect without stopping explicitly permitted Runtime work;
- and restart independently.

Closing the desktop must present clear choices when work is active.

Possible choices include:

- keep Runtime running;
- pause safe workflows;
- cancel selected tasks;
- or stop Runtime after safe shutdown.

The default behaviour is decided by product policy.

---

# 33. Runtime Lifecycle

Runtime states should include:

- Not Running;
- Bootstrapping;
- Starting;
- Ready;
- Ready with Degraded Capabilities;
- Safe Mode;
- Recovery Required;
- Stopping;
- Stopped;
- and Failed.

The Runtime should publish exact readiness.

A running process does not necessarily mean a ready Runtime.

---

# 34. Runtime Instance Identity

Every Runtime launch must have:

- stable installation identity;
- user identity;
- machine identity where appropriate;
- unique Runtime instance identifier;
- process identifier;
- start time;
- version;
- protocol range;
- and security session material.

The instance identifier must appear in:

- logs;
- Trust Centre records where useful;
- diagnostics;
- worker launches;
- and local client sessions.

---

# 35. Process Identity

Every supervised process should have a structured identity containing:

- process class;
- instance identifier;
- parent Runtime;
- project scope where applicable;
- plugin or worker identity;
- executable hash where practical;
- version;
- launch time;
- capability set;
- and security token identifier.

---

# 36. Process Authentication

Child and local client processes must authenticate.

Authentication details are decided by ADR-0004.

The topology requires:

- no trust based only on process name;
- no trust based only on localhost;
- no reusable unlimited token;
- bounded session credentials;
- parent-child binding where practical;
- and revocation on Runtime shutdown.

---

# 37. Process Supervision

The Runtime supervises processes it launches.

Supervision should track:

- expected executable;
- process identifier;
- process tree;
- start time;
- heartbeat;
- readiness;
- resource use;
- current task;
- cancellation state;
- exit code;
- crash status;
- and restart eligibility.

---

# 38. Health and Readiness

Health and readiness are distinct.

A process may be:

- alive but not ready;
- ready;
- degraded;
- unresponsive;
- stopping;
- crashed;
- quarantined;
- or incompatible.

Heartbeat alone does not prove functional readiness.

---

# 39. Restart Policy

Restart policy should depend on process class.

## 39.1 Trusted Worker

May restart for a new task.

A failed task is not automatically replayed.

---

## 39.2 Plugin Host

May restart within bounded limits.

Repeated crash should quarantine the plugin.

---

## 39.3 Runtime

May be restarted by the bootstrap helper or desktop.

State-changing operations require recovery validation before continuation.

---

## 39.4 External Provider

Provider reconnection may be attempted.

Requests are retried only according to explicit idempotency and policy.

---

## 39.5 Command Process

Commands are not automatically restarted after failure.

A new execution requires a new decision or safe retry rule.

---

# 40. Restart Limits

Restart loops must be prevented.

A restart policy should define:

- maximum attempts;
- time window;
- backoff;
- reset condition;
- escalation;
- and quarantine threshold.

Repeated failure should become visible to the developer.

---

# 41. Crash Containment

Expected containment includes:

- desktop crash does not corrupt Runtime state;
- plugin crash does not crash Runtime;
- worker crash does not crash Runtime;
- AI provider crash does not crash Runtime;
- MCP server crash does not crash Runtime;
- build process crash does not crash Runtime;
- and optional-service failure does not block unrelated services.

A trusted Runtime crash remains a major failure and must trigger recovery.

---

# 42. Native Code Placement

Third-party native code should not be loaded into the trusted Runtime unless explicitly approved.

Preferred placement order:

1. external provider process;
2. plugin process;
3. trusted worker;
4. dedicated first-party process;
5. trusted Runtime only when unavoidable and reviewed.

Native libraries loaded into the Runtime increase the trusted computing base.

---

# 43. Process Privileges

Processes should run with the least privilege required.

The first implementation should avoid administrator rights for normal operation.

Potential privilege classes include:

- normal user;
- restricted worker;
- plugin host;
- network-disabled worker;
- recovery helper;
- and elevated helper for narrowly approved operations.

An elevated process must not host ordinary Runtime services.

---

# 44. Elevation

Elevation is a separate protected action.

When required, Opure should:

- explain why;
- identify the exact operation;
- minimise duration;
- minimise capability;
- use a separate helper;
- avoid passing broad secrets;
- validate the result;
- and terminate the elevated helper.

The Runtime should not elevate itself permanently.

---

# 45. Windows Job Objects

On Windows, Opure should evaluate Job Objects for:

- child-process grouping;
- kill-on-job-close;
- CPU limits;
- memory limits;
- process count limits;
- and termination control.

The design must account for tools that create nested process trees.

The exact Windows implementation requires prototype evidence.

---

# 46. Cross-Platform Process Groups

Platform process control should be abstracted.

Potential future equivalents include:

- Unix process groups;
- sessions;
- cgroups;
- sandbox profiles;
- and operating-system-specific resource controls.

Domain services must not depend directly on Windows Job Object types.

---

# 47. Resource Governance

Each process class should have resource policy.

Possible controls include:

- CPU priority;
- memory limit;
- process count;
- GPU use declaration;
- disk throughput class;
- network permission;
- and background priority.

The Scheduler coordinates work admission.

The operating system enforces available hard limits.

---

# 48. Worker Resource Profiles

Suggested worker profiles:

### Light

- small memory;
- short duration;
- no network;
- no GPU.

### Standard

- moderate CPU and memory;
- bounded duration;
- no direct network.

### Heavy

- high CPU;
- substantial temporary memory;
- explicit scheduling;
- cancellable.

### Native Risk

- unstable or native dependency;
- strong isolation;
- low privilege;
- strict timeout.

### GPU

- declared VRAM;
- explicit scheduler admission;
- provider or specialised workload only.

---

# 49. Process Communication Classes

Communication classes include:

- Desktop to Runtime;
- Runtime to trusted worker;
- Runtime to Plugin Host;
- Runtime to launched MCP server;
- Runtime to external AI provider;
- Runtime to controlled command process;
- Runtime to recovery helper.

ADR-0004 decides the transport and security model.

---

# 50. In-Process Versus Out-of-Process Contracts

The same conceptual command or event may have:

- an in-process implementation;
- and an out-of-process transport representation.

Contract semantics must remain consistent.

Process extraction should not change user-visible meaning.

---

# 51. Version Compatibility

Desktop and Runtime may update independently only within an explicitly supported compatibility window.

Every connection should exchange:

- component version;
- protocol version range;
- contract capabilities;
- feature flags;
- and required migration state.

Incompatible components must fail clearly.

---

# 52. Worker Compatibility

A Runtime should launch only compatible workers.

Worker compatibility should include:

- executable version;
- protocol version;
- capability version;
- package hash;
- and platform architecture.

---

# 53. Plugin Host Compatibility

Plugin Host compatibility should include:

- host protocol;
- plugin SDK version;
- manifest version;
- package integrity;
- and granted capability schema.

---

# 54. Process Startup Order

Recommended order:

1. bootstrap establishes installation and user context;
2. Runtime process starts;
3. Runtime identity is created;
4. configuration is loaded;
5. audit recovery buffer is opened;
6. critical storage is validated;
7. Service Registry is built;
8. critical services start;
9. Desktop Gateway becomes available;
10. Runtime reaches Ready or Degraded;
11. desktop connects;
12. optional workers and providers start on demand;
13. plugin hosts start only for enabled plugins;
14. MCP servers start only as configured.

---

# 55. Process Shutdown Order

Recommended order:

1. stop accepting new protected commands;
2. notify desktop;
3. pause or cancel workflows according to policy;
4. stop new worker tasks;
5. cancel or complete active command processes;
6. stop plugin hosts;
7. close MCP sessions;
8. close provider sessions;
9. flush Trust Centre;
10. flush service state;
11. stop services in reverse dependency order;
12. revoke local credentials;
13. terminate remaining supervised children;
14. mark clean shutdown;
15. exit Runtime.

---

# 56. Forced Shutdown

If graceful shutdown exceeds the deadline:

- identify unfinished operations;
- persist recovery state;
- terminate child trees;
- mark abnormal shutdown;
- avoid claiming successful completion;
- and enter Recovery Required on next start where appropriate.

---

# 57. Desktop Crash

After desktop crash:

- Runtime continues only according to active-work policy;
- approvals cannot be granted without an authenticated client;
- interactive workflows may pause;
- background low-risk tasks may continue if pre-authorised;
- Trust Centre records continue;
- and reconnect restores projections.

---

# 58. Runtime Crash

After Runtime crash:

- desktop shows disconnected state;
- child processes are terminated or adopted only through an explicit recovery strategy;
- transaction journals remain;
- no operation is assumed complete;
- bootstrap may restart Runtime;
- Runtime enters recovery validation;
- and desktop reconciles after readiness.

---

# 59. Worker Crash

After worker crash:

- owning task becomes Failed or Recovery Required;
- Runtime validates partial outputs;
- temporary files are quarantined or cleaned;
- no automatic replay occurs unless idempotent and configured;
- and failure evidence is recorded.

---

# 60. Plugin Host Crash

After Plugin Host crash:

- plugin capabilities become unavailable;
- dependent workflow stages pause or fail safely;
- plugin storage integrity is checked;
- bounded restart may occur;
- repeated failure triggers quarantine;
- and unrelated plugins remain available.

---

# 61. External Provider Crash

After provider failure:

- active request ends with exact status;
- partial response is labelled partial;
- cloud fallback does not occur silently;
- local provider health becomes degraded;
- and retry follows request policy.

---

# 62. Command Process Crash

After build, test, Git or package-process failure:

- exit or crash state is captured;
- output remains available;
- process tree is checked;
- partial external effects are inspected;
- recovery guidance is shown;
- and no successful state is inferred.

---

# 63. Orphan Prevention

Processes launched by Opure should not remain orphaned unintentionally.

Controls should include:

- parent Runtime identity;
- job or process-group membership;
- launch token;
- heartbeat;
- shutdown signal;
- kill-on-parent-loss where appropriate;
- and orphan cleanup on next start.

Some external providers may intentionally remain independently managed.

Those must be marked as external rather than supervised.

---

# 64. Runtime Single-Instance Policy

The initial user-level installation should normally run one Runtime instance per operating-system user.

A second launch should:

- discover the compatible Runtime;
- authenticate;
- connect;
- or explain incompatibility.

Multiple Runtime instances may be allowed for:

- development;
- testing;
- Safe Mode;
- recovery;
- or explicit isolated profiles.

They must use separate state roots and endpoints.

---

# 65. Multiple Desktop Clients

The initial product may support one primary desktop connection.

The architecture should not make multiple read-only clients impossible.

Multiple command-capable clients require later rules for:

- approval ownership;
- notification routing;
- view-state independence;
- and conflicting commands.

---

# 66. Project Concurrency

Multiple projects may be registered.

The first product may keep one active interactive project while allowing bounded background work for others.

Process topology must not assume one project per Runtime.

Worker and plugin instances should carry project scope explicitly.

---

# 67. Safe Mode Topology

Safe Mode should run:

- Desktop;
- Runtime core;
- Configuration Manager;
- Project Manager;
- Workspace Service in restricted mode;
- Patch recovery;
- Repository recovery inspection;
- Policy Engine;
- Trust Centre;
- and diagnostics.

Safe Mode should not start by default:

- third-party Plugin Hosts;
- non-essential MCP servers;
- custom provider adapters;
- background indexing workers;
- or automatic workflows.

---

# 68. Recovery Mode Topology

Recovery Mode may use:

- bootstrap helper;
- minimal Runtime;
- minimal Desktop;
- recovery services;
- storage migration tools;
- patch journal inspection;
- and repository recovery inspection.

It should minimise:

- third-party packages;
- graphics complexity;
- external network use;
- and background tasks.

---

# 69. Update Topology

Application updates may require a separate helper because a running process cannot replace itself safely.

A future update flow may be:

```text
Desktop requests update
    ↓
Runtime validates safe point
    ↓
Update helper starts
    ↓
Desktop and Runtime stop
    ↓
Package replaced
    ↓
Migration or validation runs
    ↓
Runtime restarts
    ↓
Desktop reconnects
    ↓
Rollback if required
```

The updater requires its own ADR.

---

# 70. Installation Topology

Normal installation should avoid machine-wide services unless justified.

Preferred initial installation:

- per-user application;
- user-level Runtime;
- user-level state;
- no administrator requirement for ordinary use;
- and explicit elevation only for protected operations.

---

# 71. Headless Operation

The Runtime may eventually support limited headless operation for:

- diagnostics;
- test automation;
- CLI;
- controlled workflows;
- or recovery.

Headless operation must not create hidden background authority.

It requires explicit command, policy and audit behaviour.

---

# 72. CLI Client

A future `Opure.Cli` may act as another Desktop Gateway client.

It should not bypass the Runtime.

CLI commands must use the same:

- authentication;
- policy;
- approvals;
- service ownership;
- and Trust Centre records.

---

# 73. Service Extraction Criteria

A trusted service should move into its own process only when evidence demonstrates one or more:

- independent privilege requirement;
- native crash risk;
- memory isolation need;
- restart independence;
- scaling need;
- deployment independence;
- security boundary;
- external licensing boundary;
- or measurable performance benefit.

Extraction requires an ADR.

---

# 74. Service Extraction Requirements

Before extraction:

- contract must be stable;
- state ownership must be clear;
- persistence must be isolated;
- idempotency must be defined;
- failure behaviour must be tested;
- latency impact must be measured;
- compatibility must be defined;
- and migration must be documented.

---

# 75. Services Not Initially Extracted

The following should not begin as independent processes unless prototypes contradict this ADR:

- Project Manager;
- Workspace Service;
- Patch Service;
- Policy Engine;
- Approval Manager;
- Trust Centre;
- Workflow Engine;
- AI Router coordination;
- Knowledge Engine coordination;
- Repository Service coordination;
- and Build Manager coordination.

Their external tools or workers may still be separate.

---

# 76. Secrets Vault Placement

The initial Secrets Vault service may run in the trusted Runtime.

Secret material should use operating-system credential protection.

A future dedicated secrets broker process may be considered if:

- privilege separation provides material benefit;
- raw secret access can be reduced;
- and additional process complexity is justified.

---

# 77. Network Gateway Placement

The Network Gateway may initially run in the trusted Runtime.

External network requests may occur through:

- Runtime-owned HTTP clients;
- provider adapters;
- MCP sessions;
- or supervised external processes.

The Network Gateway must still mediate policy and destination visibility.

A separate network broker may be considered later.

---

# 78. Knowledge Engine Placement

Knowledge coordination and authoritative metadata may run in the Runtime.

Parsing, indexing and embedding preparation may use trusted workers.

Vector database or embedding provider processes may remain external or separately hosted depending on the storage ADR.

---

# 79. Patch Service Placement

Patch authority should remain inside the trusted Runtime initially.

Risky parsing or diff computation may use workers.

Final validation, approval binding, journalling and application remain Runtime-owned.

A worker must never apply a patch directly.

---

# 80. Build Manager Placement

Build Manager coordination remains inside the Runtime.

Actual commands run as external child processes.

Output parsing may occur:

- in Runtime for trusted simple parsers;
- or in a worker for complex or native parsers.

---

# 81. Repository Service Placement

Repository coordination remains inside the Runtime.

Git may run externally.

A native Git library should be isolated if its crash or dependency risk is material.

---

# 82. Process Logs

Every process should produce structured logs with:

- timestamp;
- process class;
- process instance;
- Runtime instance;
- version;
- project where relevant;
- correlation identifier;
- severity;
- safe message;
- and stable error code.

Logs must not include secret values.

---

# 83. Trust Centre Process Records

Significant process events may include:

- process launch;
- process identity;
- capability scope;
- external executable;
- command purpose;
- network expectation;
- exit;
- crash;
- forced termination;
- restart;
- quarantine;
- and resource-limit violation.

Routine heartbeat noise should not flood the Trust Centre.

---

# 84. Diagnostics

A diagnostic bundle should include safe metadata for:

- process inventory;
- executable versions;
- process relationships;
- health;
- readiness;
- restart history;
- resource use;
- crash reports;
- endpoint state;
- and recent stable error codes.

Project content and secrets should be excluded by default.

---

# 85. Crash Dumps

Crash dumps may contain sensitive memory.

They must be:

- disabled or local by default;
- stored with restricted permissions;
- clearly disclosed;
- retained for a bounded period;
- excluded from automatic upload;
- and deleted through privacy controls.

Full-memory dumps require explicit developer approval.

---

# 86. Security Impact

## 86.1 Trust Boundaries

The topology creates boundaries between:

- Desktop and Runtime;
- Runtime and trusted workers;
- Runtime and Plugin Hosts;
- Runtime and AI providers;
- Runtime and MCP servers;
- Runtime and command processes;
- Runtime and recovery helper;
- and user session and elevated helper.

---

## 86.2 Threats

Relevant threats include:

- local client impersonation;
- worker impersonation;
- plugin escape;
- privilege inheritance;
- token theft;
- executable replacement;
- process injection;
- malicious child processes;
- orphaned processes;
- command-line secret leakage;
- environment leakage;
- DLL search-path abuse;
- insecure temporary files;
- and IPC endpoint hijacking.

---

## 86.3 Mitigations

- authenticated IPC;
- executable-path validation;
- executable hash or package validation where practical;
- restricted process tokens;
- controlled environment;
- safe working directory;
- explicit child-process handles;
- process-group control;
- short-lived credentials;
- restricted temporary directories;
- no raw secrets on command lines where avoidable;
- architecture tests;
- code signing before public release;
- and Trust Centre records.

---

# 87. Command-Line Secret Handling

Secrets should not be placed in command-line arguments where avoidable because operating-system process inspection may expose them.

Preferred mechanisms include:

- protected environment injection;
- standard input;
- secure temporary descriptor or pipe;
- provider-specific credential helper;
- or OS credential broker.

The destination and purpose remain visible without revealing the value.

---

# 88. Environment Construction

Each child process should receive a deliberately constructed environment.

The Runtime should avoid blindly inheriting:

- unrelated secrets;
- development-only variables;
- unsafe PATH entries;
- debugging variables;
- proxy credentials;
- and unrelated provider credentials.

---

# 89. Executable Resolution

Command plans should resolve executables explicitly.

The Runtime should record:

- resolved path;
- version;
- source;
- hash where useful;
- and trust classification.

PATH lookup alone should not be treated as sufficient evidence for high-risk commands.

---

# 90. Working Directories

Every launched process must have an explicit working directory.

The directory must be:

- canonicalised;
- authorised;
- and visible in the command plan.

---

# 91. Temporary Directories

Each process or task should receive a scoped temporary directory where practical.

Temporary directories should have:

- restricted permissions;
- task identity;
- bounded retention;
- cleanup;
- and recovery inspection.

---

# 92. Process Output

Output channels should be:

- standard output;
- standard error;
- structured event channel where supported;
- and optional artefact channel.

Output must be:

- bounded;
- streamed;
- cancellable;
- and treated as untrusted.

---

# 93. Backpressure

The Runtime must prevent a process from overwhelming memory through output.

Backpressure may include:

- bounded pipes;
- incremental persistence;
- output truncation with evidence;
- rate-limited desktop updates;
- and process termination for abusive output.

The full outcome must state when output was truncated.

---

# 94. Process Input

Input should be structured and bounded.

Workers and Plugin Hosts should receive:

- task identifier;
- schema version;
- capability token;
- allowed resources;
- deadline;
- cancellation channel;
- and input payload reference.

Large payloads should use safe transfer mechanisms rather than unbounded command messages.

---

# 95. Deadlines

Every external or worker operation should have:

- explicit timeout;
- cancellation grace period;
- forced-termination deadline;
- and final verification.

No task should wait forever by default.

---

# 96. Cancellation Model

Cancellation states may include:

- Not Requested;
- Requested;
- Acknowledged;
- Graceful Stop;
- Forced Stop;
- Terminated;
- Termination Failed;
- and Recovery Required.

The desktop must not show Cancelled until the Runtime confirms the final state.

---

# 97. Process Tree Termination

Termination must address descendants.

The implementation should verify:

- direct process exit;
- child process exit;
- pipe closure;
- file-handle release where practical;
- and final exit status.

Tools that escape the supervised process group require explicit handling or warning.

---

# 98. Long-Lived Providers

Independently installed providers such as Ollama may remain running after Opure exits.

Opure must distinguish:

- Opure-launched provider;
- externally managed provider;
- and remote provider.

The Runtime may stop only processes it owns unless the developer explicitly requests otherwise.

---

# 99. Long-Lived MCP Servers

MCP servers may be:

- per-invocation;
- per-session;
- project scoped;
- Runtime scoped;
- or externally managed.

The server registration must declare the lifecycle model.

---

# 100. Long-Lived Plugin Hosts

Plugin Hosts should normally exist only while:

- the plugin is enabled;
- required projects are open;
- or plugin work is active.

Idle host shutdown may be supported.

Restart must revalidate plugin package and permissions.

---

# 101. Worker Pooling

Trusted workers may be pooled only when:

- task isolation remains adequate;
- memory is reset or bounded;
- capability scope does not leak;
- temporary state is cleared;
- and performance benefit is measured.

Native-risk workers should prefer one task per process.

---

# 102. Worker Warm Pools

Warm pools are deferred until evidence shows startup overhead is material.

Balanced mode should not keep unnecessary workers resident.

---

# 103. Performance Modes and Topology

## Eco

- minimal resident workers;
- stop idle Plugin Hosts where possible;
- no warm heavy workers;
- conservative concurrency.

## Balanced

- Runtime and desktop resident;
- start workers on demand;
- keep only justified providers or workers warm.

## Performance

- maintain selected warm workers;
- allow greater concurrency;
- preserve UI priority.

## Turbo

- use developer-approved high concurrency;
- maintain bounded safety controls;
- never weaken process isolation or policy.

---

# 104. Desktop Startup Behaviour

Desktop startup should:

1. inspect whether a compatible Runtime is available;
2. authenticate to it;
3. or launch the Runtime;
4. display boot progress;
5. receive readiness;
6. and render the shell before optional providers are ready.

A model provider must not block shell readiness.

---

# 105. Runtime Startup Failure

When Runtime startup fails, the desktop should provide:

- stable error code;
- log location;
- recovery option;
- Safe Mode option;
- configuration repair;
- and diagnostic export.

The desktop must not repeatedly restart the Runtime without a limit.

---

# 106. Development Topology

Development may support:

- desktop launched from IDE;
- Runtime launched from IDE;
- workers launched under debugger;
- fake providers;
- simulated crashes;
- and isolated test state roots.

Development shortcuts must not weaken production authentication by default.

---

# 107. Test Topology

Tests should be able to run:

- services in process;
- Runtime as a child process;
- desktop against a fake Gateway;
- workers as real processes;
- plugin hosts with malicious test plugins;
- and full end-to-end process trees.

---

# 108. Architecture Enforcement

Automated tests should prohibit:

- desktop references to service persistence;
- plugin code loaded into Runtime;
- worker direct access to service databases;
- service direct construction of Plugin Hosts outside supervisor;
- unsupervised child processes;
- provider SDK use outside adapters;
- and protected file application outside Patch Service.

---

# 109. Reliability and Recovery

## 109.1 Durable Authority

No critical operation should depend only on process memory.

Durable state should include:

- workflow checkpoints;
- patch journals;
- approval records;
- build records;
- repository operation state;
- and migrations.

---

## 109.2 Recovery Validation

After process restart, Opure must validate:

- whether the operation started;
- which steps completed;
- current external state;
- whether replay is safe;
- whether compensation is possible;
- and whether developer action is required.

---

## 109.3 No Blind Replay

A crashed process does not justify automatic command replay.

State-changing actions require explicit idempotency evidence.

---

# 110. Observability Requirements

The prototype must expose:

- process map;
- process classes;
- parent-child relations;
- service placement;
- worker task;
- plugin identity;
- current health;
- restart count;
- resource use;
- communication status;
- and recent failure.

---

# 111. Performance Impact

## 111.1 Expected Resource Use

The hybrid topology adds:

- one Runtime process;
- one desktop process;
- and on-demand child processes.

This creates more baseline memory than a single process.

It should create substantially less overhead than process-per-service.

---

## 111.2 Reference Hardware Expectations

On the reference machine:

- Windows 11;
- Ryzen 9 5950X;
- 32 GB RAM;
- RTX 5070 Ti 16 GB;

the combined idle desktop and Runtime should remain modest compared with local model workloads.

Exact targets require measurement.

---

## 111.3 Required Measurements

Measure:

- cold launch;
- Runtime readiness;
- desktop shell readiness;
- idle desktop memory;
- idle Runtime memory;
- idle CPU;
- worker startup;
- Plugin Host startup;
- IPC round trip;
- process restart;
- desktop reconnect;
- process-tree cancellation;
- and shutdown.

---

# 112. Cross-Platform Impact

The topology should map to Linux and macOS through platform abstractions for:

- process creation;
- process groups;
- restricted tokens;
- signals;
- executable identity;
- user-scoped endpoints;
- and resource limits.

The logical process classes remain the same.

---

# 113. Packaging Impact

The Windows package will likely include:

- desktop executable;
- Runtime executable;
- worker executable;
- Plugin Host executable;
- bootstrap or recovery helper;
- first-party assemblies;
- contracts;
- and platform-native dependencies.

All executables should be versioned and signed before public release.

---

# 114. Update Compatibility

Updates must prevent unsupported combinations such as:

- new desktop with incompatible Runtime;
- new Runtime with old workers;
- new Plugin Host with incompatible plugin protocol;
- or mismatched recovery helper.

Compatibility checks occur before commands are accepted.

---

# 115. Implementation Plan

## 115.1 Initial Tasks

1. Record founder review.
2. Define process-class contracts.
3. Define Runtime instance identity.
4. Implement Runtime bootstrap.
5. Implement one trusted in-process service.
6. Implement Desktop-to-Runtime connection.
7. Implement one trusted worker.
8. Implement worker supervision.
9. Implement worker crash test.
10. Implement process-tree cancellation.
11. Implement one Plugin Host with a test plugin.
12. Implement plugin crash-loop quarantine.
13. Implement external provider simulation.
14. Implement Runtime crash and recovery.
15. Implement desktop reconnect.
16. Measure startup and idle resources.
17. Review security.
18. Accept, amend or reject the ADR.

---

## 115.2 Owners

| Area | Owner |
|---|---|
| Product decision | Founder |
| Runtime topology | Runtime Architecture Owner |
| Desktop lifecycle | Desktop Owner |
| Worker supervision | Runtime Owner |
| Plugin isolation | Plugin Owner |
| Security | Security Owner |
| Recovery | Recovery Owner |
| Performance | Performance Owner |
| Packaging | Release Engineering Owner |
| Testing | Test Architecture Owner |

---

# 116. Prototype Scenarios

## 116.1 Scenario A — Normal Startup

- Desktop launches.
- Runtime starts.
- services register;
- Gateway authenticates;
- shell becomes ready;
- optional worker remains stopped.

---

## 116.2 Scenario B — Optional Service Failure

- optional service throws during startup;
- Runtime becomes Ready with Degraded Capabilities;
- desktop remains usable;
- failure is visible.

---

## 116.3 Scenario C — Worker Crash

- worker performs test task;
- worker crashes;
- Runtime survives;
- task fails exactly;
- temporary output is inspected;
- new task can launch a clean worker.

---

## 116.4 Scenario D — Plugin Crash Loop

- malicious test plugin crashes repeatedly;
- Plugin Host restarts within limit;
- plugin becomes quarantined;
- Runtime and other plugins remain available.

---

## 116.5 Scenario E — Desktop Crash

- workflow is running;
- desktop terminates;
- Runtime follows active-work policy;
- workflow state remains durable;
- desktop restarts and reconciles.

---

## 116.6 Scenario F — Runtime Crash

- patch transaction reaches controlled interruption;
- Runtime terminates;
- child processes are cleaned;
- Runtime restarts;
- recovery state is shown;
- no blind replay occurs.

---

## 116.7 Scenario G — Process-Tree Cancellation

- build tool launches child processes;
- cancellation is requested;
- all supervised descendants terminate;
- final state is verified.

---

## 116.8 Scenario H — Incompatible Worker

- Runtime detects protocol mismatch;
- worker is not used;
- capability becomes unavailable safely;
- error is clear.

---

# 117. Testing Strategy

## 117.1 Unit Tests

Test:

- process-state transitions;
- restart policy;
- compatibility negotiation;
- capability scopes;
- worker admission;
- shutdown ordering;
- and recovery decisions.

---

## 117.2 Contract Tests

Test:

- Desktop Gateway compatibility;
- worker protocol;
- Plugin Host protocol;
- process identity;
- cancellation;
- heartbeat;
- and error translation.

---

## 117.3 Integration Tests

Test:

- desktop launch;
- Runtime launch;
- worker launch;
- Plugin Host launch;
- provider connection;
- command execution;
- shutdown;
- restart;
- and reconnect.

---

## 117.4 Architecture Tests

Enforce:

- no plugin assembly reference from Runtime;
- no UI framework reference from Runtime services;
- no unsupervised process creation;
- no worker direct persistence ownership;
- no provider SDK outside adapters;
- and no service extraction without contract.

---

## 117.5 Security Tests

Test:

- local process impersonation;
- token reuse;
- executable replacement;
- unsafe PATH;
- environment secret leakage;
- command-line secret leakage;
- plugin escape;
- worker capability escalation;
- DLL search path;
- and endpoint hijacking.

---

## 117.6 Fault-Injection Tests

Inject:

- desktop crash;
- Runtime crash;
- worker crash;
- Plugin Host crash;
- provider crash;
- MCP crash;
- build crash;
- pipe break;
- heartbeat loss;
- disk full;
- and forced shutdown.

---

## 117.7 Recovery Tests

Test:

- interrupted patch;
- incomplete workflow;
- incomplete build;
- orphan cleanup;
- plugin quarantine;
- incompatible process;
- and failed update helper.

---

## 117.8 Performance Tests

Measure:

- startup;
- idle;
- worker launch;
- Plugin Host launch;
- IPC latency;
- event throughput;
- output streaming;
- reconnect;
- and shutdown.

---

# 118. Acceptance Criteria

This ADR may move to **Accepted** when:

- [ ] Desktop and Runtime are separate processes.
- [ ] Desktop can restart without losing authoritative Runtime state.
- [ ] Runtime can start without the desktop.
- [ ] Runtime can start without AI providers.
- [ ] Trusted first-party services run as in-process modules.
- [ ] Architecture tests enforce service ownership.
- [ ] One trusted worker can be launched and supervised.
- [ ] Worker crash does not crash Runtime.
- [ ] Worker memory is reclaimed after exit.
- [ ] Process-tree cancellation is verified on Windows.
- [ ] One Plugin Host runs outside Runtime.
- [ ] Plugin crash does not crash Runtime.
- [ ] Repeated plugin crash triggers quarantine.
- [ ] External provider failure does not crash Runtime.
- [ ] Command-process failure is reported exactly.
- [ ] Runtime restart enters recovery validation.
- [ ] No state-changing action is blindly replayed.
- [ ] Desktop reconnect reconciles authoritative state.
- [ ] Safe Mode starts without third-party Plugin Hosts.
- [ ] Recovery Mode uses a minimal process set.
- [ ] Local processes authenticate.
- [ ] Executable identity is recorded.
- [ ] Restart loops are bounded.
- [ ] Idle CPU and memory are measured.
- [ ] Startup time is measured.
- [ ] Shutdown deadlines are tested.
- [ ] Orphan cleanup is tested.
- [ ] Security review is complete.
- [ ] Founder approval is recorded.

---

# 119. Evidence Required Before Acceptance

- [ ] topology prototype;
- [ ] Desktop-to-Runtime prototype;
- [ ] worker supervision prototype;
- [ ] Plugin Host prototype;
- [ ] process-tree cancellation test;
- [ ] Runtime crash recovery test;
- [ ] desktop reconnect test;
- [ ] Safe Mode test;
- [ ] startup benchmark;
- [ ] idle-resource benchmark;
- [ ] compatibility test;
- [ ] executable identity test;
- [ ] security review;
- [ ] founder approval.

---

# 120. Known Limitations

- The local IPC transport is not yet selected.
- The serialisation format is not yet selected.
- Windows process-token restrictions are not yet prototyped.
- Job Object behaviour is not yet proven.
- Plugin Host protocol is not yet defined.
- Worker pooling is not approved.
- Multi-user Runtime behaviour is deferred.
- Multiple active desktop clients are deferred.
- Linux process-group implementation is deferred.
- macOS process-control implementation is deferred.
- Container execution is deferred.
- Remote workers are deferred.
- Separate Secrets Vault process is deferred.
- Separate Network Gateway process is deferred.
- Updater topology is deferred.

---

# 121. Open Questions

- Should the Runtime continue running when the last desktop closes?
- Which tasks may continue without an interactive client?
- Should one Plugin Host process be used per plugin or per project-plugin pair?
- Which worker tasks require one-process-per-task isolation?
- Should trusted workers be pre-warmed in Performance mode?
- How should independently managed Ollama instances be discovered?
- How should externally managed MCP servers be represented?
- Which process privileges can be reduced on Windows?
- Should the Runtime use a named user profile for development isolation?
- How should crash dumps be encrypted or protected?
- Which helper owns update rollback?
- How should a damaged Runtime launch Recovery Mode?
- When should a service be extracted into a dedicated process?
- Should a future CLI be allowed to start the Runtime?
- What compatibility window should exist between desktop and Runtime?

---

# 122. Deferred Decisions

This ADR intentionally defers:

- local IPC to ADR-0004;
- persistence to ADR-0005;
- contract serialisation to a later ADR;
- process sandbox technology to a security ADR;
- plugin-host wire protocol to a Plugin Host ADR;
- updater topology to an update ADR;
- crash-dump policy to a diagnostics ADR;
- elevated-helper design to a security ADR;
- Linux process control to a platform ADR;
- macOS process control to a platform ADR;
- remote workers to a future distributed-execution specification;
- and multi-user Runtime operation to a future collaboration specification.

---

# 123. Alternatives Rejected

## 123.1 Single-Process Desktop Monolith

Rejected because it couples desktop failure, third-party plugin failure and authoritative Runtime state inside one process.

---

## 123.2 Process per Service

Rejected for the first implementation because it introduces distributed-system complexity, memory overhead, version skew and excessive IPC before evidence justifies it.

---

## 123.3 Desktop-Hosted Runtime

Rejected because the desktop must remain a replaceable command-and-projection client rather than the owner of platform state.

---

## 123.4 Containerised Local Core

Rejected because a container runtime would become a disproportionate installation and operational dependency for the first local desktop product.

---

## 123.5 Machine-Wide Persistent Daemons

Rejected because the first product does not require continuous machine-wide execution, and this model increases privilege, privacy and support complexity.

---

# 124. Review Record

| Date | Reviewer | Decision | Notes |
|---|---|---|---|
| 18 July 2026 | Architecture draft | Proposed | Hybrid supervised Runtime recommended |

---

# 125. Approval

## Founder or Product Approval

- **Name:** Christopher Dyer
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Pending founder review

## Architecture Approval

- **Name or role:** Runtime Architecture Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Process supervision and recovery prototype required

## Security Approval

- **Name or role:** Security Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Process authentication, plugin isolation and privilege review required

## Performance Approval

- **Name or role:** Performance Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Startup, idle and worker-overhead measurements required

---

# 126. Supersession

This ADR is superseded only when a later ADR:

- names ADR-0003 explicitly;
- explains why the topology changed;
- identifies affected process classes;
- describes contract and compatibility impact;
- describes migration and update impact;
- and updates the `Superseded by` field.

Historical ADRs remain in version control.

---

# 127. Change History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | 18 July 2026 | Founder Draft | Initial hybrid supervised Runtime topology recommendation |

---

# 128. Final Decision Statement

> **Opure will provisionally use a hybrid supervised topology with a separate desktop process, one trusted modular Runtime process, isolated trusted workers, per-plugin Plugin Host processes and mediated external providers and tools because this provides the strongest balance of developer control, fault containment, performance, recoverability and small-team delivery without turning the first local product into a process-per-service distributed system.**

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**