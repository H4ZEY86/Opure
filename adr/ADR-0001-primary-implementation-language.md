# ADR-0001 — Primary Implementation Language

## Architecture Decision Record

**Status:** Proposed  
**Date:** 18 July 2026  
**Decision owners:** Founder and Product Owner  
**Technical owners:** Runtime Architecture Owner  
**Reviewers:** Security Owner, Desktop Owner, Service Architecture Owner  
**Supersedes:** None  
**Superseded by:** None  
**Related ADRs:** ADR-0002 Desktop Framework, ADR-0003 Runtime Process Topology, ADR-0004 Local IPC, ADR-0005 Persistence  
**Related specifications:** CHARTER-001, SPEC-001, SPEC-002, SPEC-003, SPEC-008, SPEC-009, SPEC-010, SPEC-011, SPEC-012  
**Target milestone:** Phase 0 — Founding Baseline  

---

## 1. Decision Summary

Opure should use **C# on a supported long-term-support .NET release** as the primary implementation language and managed runtime for the trusted core, Runtime Kernel, Desktop Gateway, engineering services, intelligence coordination services and first-party adapters.

The desktop framework remains a separate decision.

Rust may be introduced later for narrowly bounded components where measured evidence demonstrates a material security, isolation or performance benefit.

TypeScript may be used for tooling, generated clients, web-based extension surfaces or a future desktop presentation layer only when approved by a separate ADR.

---

## 2. Status

Current status:

> **Proposed**

This ADR becomes **Accepted** only after:

- a small Runtime prototype has been built;
- local IPC and process supervision have been demonstrated;
- Windows path and process-control tests have passed;
- performance has been measured on the reference machine;
- and founder approval has been recorded.

---

## 3. Context

Opure requires a primary implementation language before the source tree, build system, service contracts, testing framework and Runtime architecture can be implemented consistently.

The language must support a platform containing:

- a supervised local Runtime;
- modular services;
- multiple processes;
- desktop integration;
- project and filesystem operations;
- process execution;
- Git integration;
- local AI-provider adapters;
- streaming;
- durable storage;
- security controls;
- plugin isolation;
- MCP mediation;
- structured logging;
- recovery;
- and future cross-platform support.

Windows 11 is the first supported platform.

The first implementation must remain practical for a small team and must not require avoidable systems-programming complexity throughout the entire codebase.

The language must support long-lived engineering software rather than only rapid prototyping.

---

## 4. Problem Statement

Opure requires one primary implementation language that can support its trusted core, service architecture, Windows-first desktop integration, local process supervision, provider-neutral adapters, security controls, testing and future cross-platform expansion without imposing disproportionate implementation risk on a small team.

---

## 5. Decision Drivers

The decision is evaluated against:

- alignment with the Opure Charter;
- strong Windows 11 support;
- future Linux and macOS viability;
- local-first operation;
- service-oriented architecture;
- process supervision;
- reliable asynchronous programming;
- safe memory management;
- practical filesystem and process APIs;
- mature cryptography and authentication support;
- strong testing tools;
- structured logging and observability;
- maintainability;
- developer productivity;
- ecosystem maturity;
- package and dependency stability;
- native interoperability;
- startup and steady-state performance;
- distribution and packaging;
- plugin-host implementation;
- generated contract clients;
- implementation complexity;
- hiring and contributor accessibility;
- licensing;
- and long-term replacement cost.

---

## 6. Governing Principles

This decision must remain consistent with:

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

Specific requirements include:

- The Runtime must operate without AI.
- Provider-specific code must remain inside adapters.
- Protected file writes must pass through the Patch Service.
- Services must communicate through explicit contracts.
- The desktop must not own authoritative domain state.
- Third-party plugins must remain outside the trusted core.
- Secrets must remain in the Secrets Vault.
- Windows 11 is the first target without becoming a permanent lock-in.
- Recovery and observability must be designed from the beginning.

---

## 7. Scope

This ADR decides the primary language for:

- Runtime Kernel;
- Configuration Manager;
- Service Registry;
- Lifecycle Manager;
- Scheduler;
- Health Supervisor;
- Runtime Messaging;
- Desktop Gateway;
- Project Manager;
- Workspace Service;
- Patch Service;
- Build Manager;
- Repository Service;
- Dependency Manager;
- Environment Manager;
- Artefact Manager;
- AI Router;
- Context Engine;
- Knowledge Engine coordination;
- Workflow Engine;
- Policy Engine;
- Approval Manager;
- Secrets Vault integration;
- Network Gateway;
- Trust Centre;
- Plugin Manager;
- MCP Gateway;
- first-party provider adapters;
- first-party build and repository adapters;
- command-line tools;
- and test infrastructure.

This ADR does not decide:

- the desktop framework;
- the desktop rendering technology;
- the final local IPC protocol;
- the storage engine;
- the vector database;
- the plugin implementation languages;
- the language used by third-party MCP servers;
- whether performance-critical native modules will exist;
- or the public extension SDK language surface.

---

## 8. Constraints

Known constraints include:

- Windows 11 is the first supported operating system.
- The platform must work without a cloud account.
- The Runtime must supervise multiple services and isolated processes.
- Long-running operations must be cancellable.
- Protected file writes must pass through the Patch Service.
- Provider adapters must remain replaceable.
- Local AI models may consume substantial memory and GPU resources.
- The desktop must remain responsive during inference and builds.
- Secret values must not enter normal storage.
- Third-party plugin code must not run with trusted-core authority.
- The implementation must remain practical for a small initial team.
- The architecture should preserve future Linux and macOS support.
- The project should avoid unnecessary language fragmentation.

---

## 9. Assumptions

This decision assumes:

- Opure will initially be developed by a small team.
- Most platform complexity will be orchestration, state management, policy, IO and process control rather than hard real-time computation.
- Local model inference will usually occur in external provider runtimes such as Ollama rather than inside the Opure process.
- Performance-sensitive text parsing, hashing and diff work can be optimised within managed code before native code is considered.
- The desktop may use the same language or a different presentation technology.
- Service contracts will be designed to permit future language diversity.
- A supported LTS .NET release will remain available for the product lifecycle.
- Native libraries can be called through bounded interoperability layers where justified.
- Strong architecture tests will prevent framework convenience from weakening service boundaries.

---

## 10. Options Considered

The primary options considered are:

1. **Option A — C# and .NET**
2. **Option B — Rust**
3. **Option C — TypeScript and Node.js**
4. **Option D — C++**
5. **Option E — Mixed-Language Core from the Beginning**

---

# 11. Option A — C# and .NET

## 11.1 Description

Use C# and a supported LTS .NET release as the primary implementation platform.

The Runtime, services, adapters, local API and test infrastructure would be implemented in managed .NET code.

Native interoperability would be isolated behind interfaces.

---

## 11.2 Advantages

- Strong Windows integration.
- Mature asynchronous programming.
- Mature process and filesystem APIs.
- Safe managed memory for most application code.
- Strong static typing.
- Good support for records, pattern matching and immutable models.
- Mature dependency injection and hosting patterns.
- Mature structured logging ecosystem.
- Mature test frameworks.
- Strong HTTP, JSON, streaming and cryptography support.
- Good SQLite and database support.
- Good gRPC, named-pipe and local-socket support.
- Good cancellation-token model.
- Good background-service and worker-process patterns.
- Good cross-platform support.
- Productive for a small team.
- Familiar deployment options.
- Good debugging and profiling.
- Strong source generators and code generation.
- Native ahead-of-time compilation is available for selected components if justified.
- Easy creation of command-line tools.
- Good support for Windows Services, processes, job objects through platform APIs and interoperability.
- Suitable for long-lived desktop and service applications.

---

## 11.3 Disadvantages

- Managed-runtime distribution adds deployment size.
- Garbage collection can introduce latency variability.
- Some Windows security and process-control features require native interoperability.
- Native memory safety is not guaranteed when using unsafe code or external libraries.
- Single-file or trimmed publishing can require careful testing.
- Cross-platform UI choices are less straightforward than service implementation.
- .NET architectural conventions can encourage excessive framework coupling if used without discipline.
- Some high-performance parsing or sandbox components may eventually benefit from Rust.
- Plugin isolation cannot rely on application-domain mechanisms alone and still requires process boundaries.

---

## 11.4 Risks

- Overuse of dependency-injection frameworks could obscure ownership.
- Excessive reflection could weaken trimming and deployment predictability.
- Platform-specific APIs could leak into domain services.
- NuGet dependency growth could increase supply-chain exposure.
- Developers may confuse managed memory safety with complete security.
- Native interoperability may become scattered if not isolated.
- Desktop framework selection could accidentally force architecture coupling.

---

## 11.5 Evidence to Obtain

- Runtime boot and service-supervision prototype.
- Local authenticated IPC prototype.
- Windows process-tree cancellation prototype.
- Canonical path and junction-handling prototype.
- Transactional multi-file patch prototype.
- SQLite durability prototype.
- Structured event and command contract prototype.
- Provider streaming and cancellation prototype.
- Memory and startup measurement.
- Trimmed or self-contained packaging experiment.
- Cross-platform build experiment on Linux.

---

## 11.6 Estimated Adoption Cost

- **Initial implementation:** Low to Moderate
- **Operational complexity:** Low to Moderate
- **Migration difficulty:** Moderate
- **Replacement difficulty:** Moderate

---

# 12. Option B — Rust

## 12.1 Description

Use Rust as the primary language for the Runtime, services, adapters and command-line tools.

The desktop would use Rust directly or communicate with a Rust Runtime.

---

## 12.2 Advantages

- Strong compile-time memory safety.
- Predictable runtime behaviour.
- No garbage collector.
- Strong performance.
- Good control over process, filesystem and network behaviour.
- Good cross-platform capability.
- Small native binaries are possible.
- Strong type system.
- Good fit for security-sensitive parsers and isolated hosts.
- Good support for FFI and native libraries.
- Explicit error handling.
- Strong suitability for long-running system processes.
- Good potential for efficient indexing and diff operations.

---

## 12.3 Disadvantages

- Higher implementation complexity for a small team.
- Longer development time for broad application functionality.
- More difficult asynchronous and ownership-heavy code for ordinary orchestration.
- Desktop framework ecosystem is less settled.
- Enterprise application libraries are less mature in some areas.
- Database, migration and plugin patterns require more assembly.
- Windows-specific integration can require substantial systems work.
- Reflection-heavy contract and plugin systems are less straightforward.
- Dynamic extension scenarios need careful ABI or protocol design.
- Contributor accessibility may be lower.
- UI and service development may fragment across technologies.
- Compile times may become significant.
- Business-logic iteration may be slower than C#.

---

## 12.4 Risks

- Architecture progress could be slowed by language complexity.
- Too much effort could be spent on low-level correctness before product value exists.
- Unsafe code may still enter through native integrations.
- Plugin and UI requirements could force language fragmentation early.
- Small-team throughput may be insufficient.
- Delivery milestones may slip while infrastructure is perfected.
- Contributors may avoid the project because of the learning curve.

---

## 12.5 Evidence to Obtain

- Equivalent Runtime prototype.
- Windows process supervision.
- Local IPC implementation.
- SQLite and migration support.
- Structured logging and diagnostics.
- Plugin-process protocol.
- Packaging experiment.
- Developer throughput comparison.
- Cross-platform build.
- Benchmark comparison with C#.

---

## 12.6 Estimated Adoption Cost

- **Initial implementation:** High
- **Operational complexity:** Moderate
- **Migration difficulty:** High
- **Replacement difficulty:** High

---

# 13. Option C — TypeScript and Node.js

## 13.1 Description

Use TypeScript on Node.js as the primary platform for the Runtime and services.

The desktop could share the same language through Electron or another web-based shell.

---

## 13.2 Advantages

- High development speed.
- Large ecosystem.
- Strong JSON and web-protocol support.
- Easy desktop integration with Electron.
- Good contributor accessibility.
- Convenient dynamic plugin development.
- Mature package tooling.
- Strong support for streaming and network operations.
- One language could cover desktop and backend services.
- Good generated-client tooling.
- Straightforward UI-to-gateway model sharing.

---

## 13.3 Disadvantages

- Weaker operating-system and process-control ergonomics.
- Node's single event loop requires discipline for CPU-heavy work.
- Package ecosystem presents substantial supply-chain risk.
- Runtime and dependency footprint can be large.
- Type safety does not exist at runtime without validation.
- Filesystem and path security require careful handling.
- Native Windows integration often requires add-ons.
- Long-running multi-service supervision is less natural.
- Memory use may be high for desktop plus Runtime.
- Package-manager scripts introduce additional trust concerns.
- Native extension compatibility can complicate distribution.
- Strong isolation still requires separate processes.
- Cross-package version drift can become significant.
- Large monorepos may develop weak boundary enforcement without additional tooling.

---

## 13.4 Risks

- The platform could become too closely tied to Electron or Node.
- Supply-chain exposure could become difficult to control.
- CPU-heavy indexing, diff and parsing tasks could block or require worker complexity.
- Runtime contract validation may be inconsistent.
- Security-sensitive path and process operations may rely heavily on native modules.
- Desktop convenience could cause the UI and domain state to become coupled.
- Dependency volume could undermine maintainability.

---

## 13.5 Evidence to Obtain

- Service-supervision prototype.
- Worker and child-process cancellation.
- Windows job-object support.
- Secure path handling.
- Transactional patching.
- Authenticated local IPC.
- Supply-chain dependency analysis.
- Memory comparison against C#.
- Packaging and update experiment.

---

## 13.6 Estimated Adoption Cost

- **Initial implementation:** Low
- **Operational complexity:** Moderate to High
- **Migration difficulty:** Moderate
- **Replacement difficulty:** Moderate to High

---

# 14. Option D — C++

## 14.1 Description

Use modern C++ for the Runtime, services and native desktop integration.

---

## 14.2 Advantages

- Native performance.
- Direct access to Windows APIs.
- Mature systems ecosystem.
- Strong control over memory and process behaviour.
- Broad library availability.
- Straightforward native deployment.
- Suitable for high-performance parsing and indexing.
- Strong interoperability with native tools.

---

## 14.3 Disadvantages

- Manual memory and lifetime complexity.
- Higher security risk.
- Slower application-development throughput.
- Complex build and dependency management.
- More difficult cross-platform consistency.
- Greater test burden.
- ABI stability challenges.
- More difficult safe plugin boundaries.
- Higher contributor barrier.
- Significant complexity for ordinary service and workflow logic.
- Increased risk of undefined behaviour.
- Less productive for structured application-state management.

---

## 14.4 Risks

- Memory-safety defects in trusted-core code.
- Development effort consumed by systems concerns.
- Platform-specific code spreading throughout the architecture.
- Complex packaging and dependency management.
- Increased difficulty maintaining a small-team codebase.
- More expensive recovery and fault diagnosis.

---

## 14.5 Evidence to Obtain

C++ is not recommended for full primary-language prototyping unless the preferred options fail.

It may remain relevant for isolated native integrations.

---

## 14.6 Estimated Adoption Cost

- **Initial implementation:** High
- **Operational complexity:** High
- **Migration difficulty:** High
- **Replacement difficulty:** High

---

# 15. Option E — Mixed-Language Core from the Beginning

## 15.1 Description

Use different primary languages from the outset, such as:

- Rust for Runtime and security;
- C# for services;
- TypeScript for desktop;
- Python for AI-related processing.

---

## 15.2 Advantages

- Each subsystem can use a language suited to its immediate task.
- Native performance can be applied selectively.
- Desktop ecosystem choice remains broad.
- AI experimentation may be convenient in Python.
- Service contracts are forced to become explicit early.

---

## 15.3 Disadvantages

- Multiple build systems.
- Multiple dependency systems.
- Multiple test frameworks.
- Multiple packaging pipelines.
- More complex debugging.
- More complex contributor onboarding.
- More complex release engineering.
- Higher integration cost.
- More IPC boundaries.
- More duplicated domain models.
- Greater risk of contract drift.
- More difficult security review.
- Slower early delivery.
- Higher operational complexity.

---

## 15.4 Risks

- The architecture could become distributed before product value is proven.
- A small team could spend most of its time on integration.
- Shared concepts could drift across languages.
- Release and migration complexity could become disproportionate.
- Fault diagnosis could become difficult.
- Native boundaries could be introduced without measured need.

---

## 15.5 Evidence to Obtain

A mixed-language architecture should be considered only after:

- the C# prototype exists;
- a specific bottleneck is measured;
- replacement boundaries are stable;
- and the benefit exceeds integration cost.

---

## 15.6 Estimated Adoption Cost

- **Initial implementation:** Very High
- **Operational complexity:** Very High
- **Migration difficulty:** High
- **Replacement difficulty:** High

---

# 16. Comparison Matrix

Scoring:

- **1** — poor
- **2** — weak
- **3** — acceptable
- **4** — strong
- **5** — excellent

Scores represent architectural judgement and must be verified through prototypes.

| Criterion | Weight | C#/.NET | Rust | TypeScript/Node | C++ | Mixed Core |
|---|---:|---:|---:|---:|---:|---:|
| Charter alignment | 5 | 5 | 5 | 4 | 4 | 3 |
| Windows 11 support | 5 | 5 | 4 | 4 | 5 | 4 |
| Cross-platform viability | 4 | 4 | 5 | 5 | 4 | 4 |
| Memory safety | 5 | 4 | 5 | 4 | 2 | 4 |
| Process supervision | 5 | 5 | 5 | 3 | 5 | 4 |
| Filesystem control | 5 | 5 | 5 | 3 | 5 | 4 |
| Async and streaming | 4 | 5 | 4 | 5 | 3 | 4 |
| Security ecosystem | 5 | 5 | 5 | 3 | 3 | 4 |
| Recovery implementation | 5 | 5 | 4 | 3 | 3 | 3 |
| Performance | 4 | 4 | 5 | 3 | 5 | 5 |
| Developer productivity | 5 | 5 | 3 | 5 | 2 | 2 |
| Small-team suitability | 5 | 5 | 3 | 4 | 2 | 1 |
| Testability | 5 | 5 | 4 | 4 | 3 | 3 |
| Observability | 4 | 5 | 4 | 4 | 3 | 3 |
| Desktop options | 3 | 4 | 3 | 5 | 4 | 5 |
| Ecosystem maturity | 4 | 5 | 4 | 5 | 5 | 4 |
| Supply-chain manageability | 4 | 4 | 4 | 2 | 3 | 2 |
| Native interoperability | 3 | 4 | 5 | 3 | 5 | 5 |
| Packaging | 4 | 4 | 4 | 3 | 4 | 2 |
| Replacement cost | 4 | 4 | 3 | 3 | 2 | 2 |
| **Weighted indicative total** |  | **445** | **403** | **360** | **342** | **323** |

The matrix supports C#/.NET as the best overall primary platform for Opure's first implementation.

It does not prove that C# is best for every future component.

---

# 17. Decision

Opure will adopt **C# and a supported LTS .NET release** as the primary implementation language and managed runtime for the trusted platform core and first-party services.

The decision applies to:

- Runtime Kernel;
- service contracts;
- Desktop Gateway;
- engineering services;
- intelligence coordination;
- trust and policy services;
- first-party adapters;
- command-line tools;
- and the default automated test suite.

The decision does not require:

- every desktop view to use C#;
- third-party plugins to use C#;
- MCP servers to use C#;
- AI providers to run in .NET;
- model inference to occur in process;
- or all performance-sensitive components to remain managed forever.

This decision is:

- [x] Permanent until superseded
- [ ] Provisional
- [ ] Experimental
- [ ] Limited to one milestone
- [ ] Limited to one subsystem

Acceptance remains conditional upon the Phase 0 and Phase 1 evidence gates.

---

# 18. Rationale

C#/.NET is preferred because it provides the strongest combined fit for:

- Windows-first platform development;
- managed memory safety;
- service orchestration;
- asynchronous IO;
- process supervision;
- filesystem operations;
- cryptography;
- structured logging;
- database access;
- testing;
- cross-platform portability;
- and small-team productivity.

Rust offers stronger low-level guarantees and performance, but using it throughout the complete platform would impose higher implementation cost before evidence shows that cost is necessary.

TypeScript offers excellent development speed and desktop ecosystem options, but it is less suitable as the trusted primary Runtime for security-sensitive process, filesystem and package operations.

C++ provides native control but imposes unnecessary memory-safety and maintenance risk for the majority of Opure's application logic.

A mixed-language core would create disproportionate integration complexity before the architecture and product are proven.

---

# 19. Consequences

## 19.1 Positive Consequences

- One primary language covers most trusted services.
- The team can share domain models and contract libraries.
- Runtime and service implementation can proceed quickly.
- Windows integration is strong.
- Cross-platform domain logic remains practical.
- Process, filesystem and network operations are well supported.
- Mature testing and diagnostics are available.
- Managed memory reduces common memory-safety defects.
- Provider, plugin and MCP protocols can remain language-neutral.
- Native components can still be introduced behind boundaries.
- Tooling and IDE support are mature.
- Refactoring across the trusted core is practical.
- Service contracts can use generated clients and schemas.
- Long-lived background services are a normal .NET use case.

---

## 19.2 Negative Consequences

- The application will depend on the .NET runtime or self-contained deployment.
- Distribution size may be larger than a minimal native application.
- Garbage collection must be profiled.
- Some Windows process and security features require interoperability.
- Native plugin isolation still requires separate processes.
- Careless framework use could obscure architectural ownership.
- The desktop framework may still introduce a second language.
- Very high-performance components may later require native optimisation.
- Trimming and ahead-of-time compilation may constrain reflection-heavy designs.

---

## 19.3 Neutral Consequences

- Plugin and MCP contracts remain language-neutral.
- AI model providers remain external.
- Build tools remain external processes.
- The storage engine remains undecided.
- Desktop rendering remains undecided.
- Cross-platform packaging remains future work.
- A later Rust component remains possible.

---

## 19.4 New Responsibilities

This decision creates responsibility for:

- establishing C# coding standards;
- selecting the supported .NET LTS release;
- defining package-management policy;
- controlling reflection and dynamic loading;
- isolating native interoperability;
- measuring garbage-collection behaviour;
- testing self-contained deployment;
- maintaining architecture tests;
- and preventing framework coupling from replacing explicit service contracts.

---

# 20. Language Boundary Rules

## 20.1 Trusted Core

Trusted-core code should use C# unless a later ADR approves an exception.

---

## 20.2 Native Interoperability

Native interoperability must be:

- isolated;
- documented;
- tested;
- bounded;
- and owned.

Platform invocation code should live in dedicated platform-adapter assemblies.

---

## 20.3 Unsafe Code

Unsafe C# is denied by default.

It requires:

- an explicit reason;
- code review;
- focused tests;
- security review;
- and an ADR or documented exception for material use.

---

## 20.4 Dynamic Loading

Dynamic assembly loading must not become the security boundary for third-party plugins.

Third-party plugins should run in separate processes.

---

## 20.5 Reflection

Reflection should be limited where it affects:

- trimming;
- ahead-of-time compilation;
- security review;
- or contract discoverability.

Generated registration may be preferred for trusted core services.

---

## 20.6 Scripting

The trusted core must not depend on runtime scripting for normal operation.

Developer-approved scripts may run through controlled process adapters.

---

# 21. Project Structure Implications

A likely initial source layout is:

```text
src/
├── Opure.Runtime/
├── Opure.Runtime.Abstractions/
├── Opure.Contracts/
├── Opure.Desktop.Gateway/
├── Opure.ProjectManagement/
├── Opure.Workspace/
├── Opure.Patching/
├── Opure.Build/
├── Opure.Repository/
├── Opure.Dependencies/
├── Opure.Environments/
├── Opure.Artefacts/
├── Opure.AI/
├── Opure.AI.Abstractions/
├── Opure.AI.Ollama/
├── Opure.Context/
├── Opure.Knowledge/
├── Opure.Workflows/
├── Opure.Policy/
├── Opure.Approvals/
├── Opure.Secrets/
├── Opure.Network/
├── Opure.Trust/
├── Opure.Plugins/
├── Opure.Mcp/
├── Opure.Diagnostics/
├── Opure.Platform.Windows/
└── Opure.Cli/
```

A likely test layout is:

```text
tests/
├── Opure.UnitTests/
├── Opure.ContractTests/
├── Opure.ArchitectureTests/
├── Opure.IntegrationTests/
├── Opure.SecurityTests/
├── Opure.RecoveryTests/
├── Opure.PerformanceTests/
└── Opure.EndToEndTests/
```

The exact layout requires a source-structure ADR or implementation plan.

---

# 22. Coding Model

The codebase should favour:

- explicit interfaces;
- immutable command and event records;
- nullable-reference analysis;
- asynchronous APIs for IO;
- cancellation tokens;
- structured result types where useful;
- stable error codes;
- dependency direction enforcement;
- and small service-owned modules.

It should avoid:

- global service locators;
- static mutable state;
- unbounded background tasks;
- hidden reflection registration;
- direct database sharing;
- direct filesystem mutation;
- and exceptions as ordinary control flow across service boundaries.

---

# 23. Asynchronous Programming

Long-running and IO-bound operations should be asynchronous.

All cancellable operations should accept a cancellation token or equivalent service-contract field.

The implementation must distinguish:

- caller cancellation;
- timeout;
- service shutdown;
- process termination;
- and external provider failure.

Fire-and-forget tasks are prohibited unless supervised by the Scheduler or Lifecycle Manager.

---

# 24. Error Handling

Internal errors should use:

- stable codes;
- safe messages;
- technical detail;
- correlation identifiers;
- recoverability;
- and causal chains.

Exceptions may be used internally.

They must be translated at service boundaries.

Secret values and sensitive payloads must not appear in exception messages.

---

# 25. Serialization

The final contract format is deferred.

C# models must not become the wire contract by accident.

Wire contracts should remain:

- versioned;
- explicit;
- compatible;
- and language-neutral.

Generated C# types may represent those contracts.

---

# 26. Dependency Policy

NuGet packages must be:

- deliberately selected;
- version pinned or centrally managed;
- licence reviewed;
- vulnerability reviewed;
- transitively inspected;
- and minimised.

The platform should prefer standard-library capabilities where they are sufficient.

Large framework dependencies require explicit justification.

---

# 27. Security Impact

## 27.1 Trust Boundaries

Affected trust boundaries include:

- Desktop to Desktop Gateway;
- Runtime to worker processes;
- Runtime to plugins;
- Runtime to MCP servers;
- Runtime to AI providers;
- Runtime to build tools;
- Runtime to Git;
- Runtime to filesystem;
- Runtime to network;
- and managed code to native libraries.

---

## 27.2 Permissions

The language decision does not grant permissions.

Permissions remain owned by:

- Policy Engine;
- Approval Manager;
- Workspace Service;
- Patch Service;
- Secrets Vault;
- Network Gateway;
- Plugin Manager;
- and MCP Gateway.

---

## 27.3 Secrets

C# string immutability means secret handling must avoid unnecessary ordinary strings where practical.

The Vault implementation should:

- minimise secret lifetime;
- use operating-system-backed protection;
- avoid logging;
- avoid exception interpolation;
- avoid normal serialisation;
- clear mutable buffers where possible;
- and expose secret-use operations rather than broad retrieval.

---

## 27.4 Network

The language ecosystem provides mature network APIs.

All protected network calls must still pass through the Network Gateway or approved adapter boundary.

Direct HTTP clients outside authorised network components should be prevented by architecture tests where practical.

---

## 27.5 Threats

Relevant threats include:

- malicious NuGet packages;
- unsafe native interoperability;
- assembly-load abuse;
- reflection-based capability discovery;
- insecure deserialisation;
- local API impersonation;
- path canonicalisation errors;
- command injection;
- secret leakage through logs;
- and excessive Runtime privileges.

---

## 27.6 Mitigations

- central package management;
- dependency review;
- locked restore;
- signed release pipeline;
- no BinaryFormatter-style unsafe deserialisation;
- explicit JSON or schema validation;
- isolated plugin processes;
- restricted native-interop assemblies;
- architecture tests;
- local API authentication;
- secret-redaction tests;
- and least-privilege process execution.

---

# 28. Privacy Impact

The language choice does not require external telemetry or cloud use.

Privacy controls remain architecture-level requirements.

The implementation must ensure:

- no automatic remote diagnostics;
- no hidden package-time or runtime telemetry;
- no project data in crash reports by default;
- no secret values in logs;
- and preview before diagnostic export.

Any third-party library telemetry must be disabled or documented.

---

# 29. Developer-Control Impact

C#/.NET supports the required developer-control model because:

- command plans can be structured;
- service states can be modelled explicitly;
- cancellation is first class;
- event histories can be recorded;
- desktop projections can remain separate from authoritative services;
- and provider adapters can remain replaceable.

The choice does not remove manual alternatives.

Developers may continue to use:

- editors;
- terminals;
- Git;
- build tools;
- package managers;
- and provider runtimes

outside Opure.

---

# 30. Performance Impact

## 30.1 Expected Resource Use

- **CPU:** Appropriate for orchestration, parsing and local services.
- **Memory:** Moderate managed-runtime overhead.
- **GPU:** No direct requirement for the trusted core.
- **VRAM:** Controlled by external model providers.
- **Disk:** Self-contained deployment and symbols may be substantial.
- **Network:** No language-imposed cloud requirement.
- **Startup:** Expected to be suitable, subject to measurement.
- **Idle use:** Expected to be low with correct background-service design.

---

## 30.2 Reference Hardware

On the reference machine:

- Windows 11;
- AMD Ryzen 9 5950X;
- 32 GB RAM;
- NVIDIA RTX 5070 Ti with 16 GB VRAM;

the Runtime and desktop should remain a small fraction of total resource use compared with local model inference.

This is an expectation, not yet measured evidence.

---

## 30.3 Required Measurements

Measure:

- cold Runtime startup;
- warm Runtime startup;
- desktop-to-gateway latency;
- idle CPU;
- idle memory;
- service registration time;
- project-open time;
- large-directory enumeration;
- hashing throughput;
- diff throughput;
- patch apply throughput;
- structured logging overhead;
- event throughput;
- AI streaming overhead;
- and build-output streaming.

---

# 31. Reliability and Recovery

## 31.1 Failure Modes

Potential language-platform failure modes include:

- unhandled exception;
- deadlock;
- thread-pool starvation;
- runaway task;
- memory pressure;
- garbage-collection pause;
- corrupted persistent state;
- native interoperability crash;
- and process termination during state change.

---

## 31.2 Cancellation

Cancellation tokens must be propagated through:

- service commands;
- workflows;
- AI requests;
- file operations;
- build operations;
- plugin calls;
- MCP calls;
- and shutdown.

External process cancellation must verify process-tree termination.

---

## 31.3 Retry

Retry policies must be:

- bounded;
- explicit;
- idempotency-aware;
- and observable.

General-purpose transparent retries are prohibited for state-changing commands.

---

## 31.4 Recovery

Recovery must rely on durable domain state rather than in-memory task continuation.

The implementation should use:

- journals;
- checkpoints;
- stable operation identifiers;
- idempotency keys;
- snapshots;
- and recovery coordinators.

---

## 31.5 Data Integrity

Data-integrity protections include:

- transactional storage;
- schema versioning;
- atomic replacement where supported;
- fsync or equivalent where required;
- hashes;
- migration backups;
- and corruption detection.

---

# 32. Observability and Trust Centre

The .NET implementation must expose:

- service health;
- Runtime status;
- structured logs;
- metrics;
- traces or correlation;
- task state;
- process state;
- and Trust Centre records.

Logging and Trust Centre APIs must make redaction easy by default.

Secret-bearing types should not provide unsafe automatic string formatting.

---

# 33. Compatibility

## 33.1 Contract Compatibility

Service contracts remain language-neutral.

C# record types are implementation representations, not permanent protocol definitions.

---

## 33.2 Data Compatibility

Persistent data must use versioned schemas independent from object-memory layout.

---

## 33.3 Platform Compatibility

- **Windows 11:** Primary supported platform.
- **Linux future:** Expected to be viable for most services.
- **macOS future:** Expected to be viable for most services.
- **Platform-specific features:** Isolated behind adapters.

---

## 33.4 Plugin and MCP Compatibility

Third-party plugins and MCP servers do not need to use C#.

They communicate through documented protocols and capability contracts.

---

# 34. Migration

## 34.1 Existing State

There is no production implementation to migrate.

The repository currently contains specifications and architecture documentation.

---

## 34.2 Initial Adoption Steps

1. Install and pin the selected .NET SDK.
2. Create central build configuration.
3. Create source and test solution structure.
4. Enable nullable references.
5. Enable warnings as errors for trusted-core projects.
6. Configure formatting.
7. Configure static analysis.
8. Configure package locking or central package management.
9. Create architecture-test project.
10. Implement Runtime boot-to-health prototype.
11. Implement authenticated local IPC prototype.
12. Measure startup and idle resource use.

---

## 34.3 Migration Validation

- clean restore;
- reproducible build;
- test execution;
- self-contained publish;
- package inventory;
- Windows installation;
- and Linux compilation experiment.

---

## 34.4 Migration Failure

If the prototype fails evidence gates, this ADR returns to **Under Review**.

The repository should retain contracts and test scenarios to support comparison with Rust or another alternative.

---

# 35. Rollback or Replacement

C#/.NET can be replaced or narrowed through:

- language-neutral service contracts;
- isolated process boundaries;
- stable storage schemas;
- provider adapters;
- platform adapters;
- and generated client libraries.

A future Rust component may replace:

- a parser;
- a diff engine;
- a plugin sandbox;
- a hashing service;
- a process supervisor;
- or another bounded subsystem.

A replacement should not require rewriting:

- desktop views;
- workflow definitions;
- policy rules;
- Trust Centre records;
- or unrelated services.

---

# 36. Native Component Admission Rule

A native component should be introduced only when:

1. a real bottleneck or safety need is measured;
2. managed optimisation has been attempted;
3. the boundary can be narrow;
4. failure isolation is designed;
5. packaging impact is understood;
6. security review is complete;
7. tests cover interoperability;
8. and an ADR approves the addition.

Preference order:

1. improve algorithm;
2. improve allocation and IO behaviour;
3. use optimised managed libraries;
4. isolate work in a managed worker;
5. use an existing trusted native library;
6. write a new native component only when justified.

---

# 37. Desktop Relationship

This ADR does not choose the desktop framework.

Possible outcomes include:

- a C# desktop framework;
- a web-rendered desktop using a C# Runtime;
- or another presentation technology using the Desktop Gateway.

Regardless of framework:

- domain state remains in services;
- desktop commands pass through the Gateway;
- and the Runtime remains independently testable.

---

# 38. Plugin Relationship

The Plugin SDK should be protocol-first.

First-party plugins may use C#.

Third-party plugins may use any supported language if they comply with:

- manifest;
- process protocol;
- capability contracts;
- permissions;
- health;
- cancellation;
- and packaging requirements.

No public in-process .NET plugin ABI is implied by this ADR.

---

# 39. MCP Relationship

MCP remains an external protocol boundary.

MCP servers may use any language.

The C# Runtime implements the MCP Gateway client and mediation logic.

---

# 40. AI Relationship

AI providers remain external adapters.

The primary language choice does not require model inference in .NET.

Initial provider integration may communicate with Ollama through its local API.

Provider-specific code remains in adapter assemblies.

---

# 41. Build and Tooling

The initial repository should use standard .NET build tooling.

Required capabilities include:

- deterministic build settings where practical;
- central package versions;
- code formatting;
- static analysis;
- unit tests;
- coverage;
- architecture tests;
- integration tests;
- performance tests;
- and packaging.

Exact tools require an implementation-tooling ADR or project setup decision.

---

# 42. Coding Standards Required

Before Phase 1 implementation, define:

- naming;
- nullable references;
- async naming and usage;
- cancellation;
- error handling;
- logging;
- security-sensitive types;
- immutable contracts;
- package dependencies;
- native interoperability;
- test naming;
- and documentation.

---

# 43. Architecture Enforcement

Automated tests should detect:

- forbidden project references;
- provider SDK references outside adapters;
- filesystem writes outside Patch Service;
- direct secret-store implementation access;
- direct MCP access outside MCP Gateway;
- direct network access outside approved boundaries;
- desktop references to persistence internals;
- and circular service dependencies.

---

# 44. Prototype Plan

## 44.1 Prototype A — Runtime Boot

Build:

- Runtime identity;
- service registry;
- lifecycle;
- health;
- graceful shutdown;
- and one failing optional service.

---

## 44.2 Prototype B — Local Gateway

Build:

- authenticated local IPC;
- command;
- query;
- event subscription;
- reconnection;
- and cancellation.

---

## 44.3 Prototype C — Process Control

Build:

- worker launch;
- child-process tree;
- output streaming;
- cancellation;
- timeout;
- and verified termination.

---

## 44.4 Prototype D — Workspace Safety

Build:

- path canonicalisation;
- junction handling;
- traversal rejection;
- file hashing;
- encoding detection;
- and file watching.

---

## 44.5 Prototype E — Patch Transaction

Build:

- multi-file patch;
- base hashes;
- staging;
- journal;
- interruption;
- recovery;
- and reversal.

---

## 44.6 Prototype F — Provider Streaming

Build:

- Ollama adapter;
- model discovery;
- streaming;
- cancellation;
- structured output;
- and simulated second adapter.

---

# 45. Implementation Plan

## 45.1 Initial Tasks

1. Record founder approval or requested amendments.
2. Select the supported LTS .NET release.
3. Create `global.json`.
4. Create central build property files.
5. Create solution structure.
6. Create contract and Runtime abstractions.
7. Create architecture tests.
8. Create Runtime boot prototype.
9. Create local IPC prototype.
10. Create process-control prototype.
11. Create workspace path prototype.
12. Record benchmark results.
13. Review the ADR.
14. Accept, amend or reject the decision.

---

## 45.2 Owners

| Area | Owner |
|---|---|
| Product decision | Founder |
| Architecture | Runtime Architecture Owner |
| Runtime prototype | Runtime Owner |
| Security review | Security Owner |
| Performance measurement | Runtime and Performance Owner |
| Testing | Test Architecture Owner |
| Documentation | Architecture Owner |

---

## 45.3 Milestones

- Phase 0 language proof;
- Phase 1 Runtime skeleton;
- Phase 2 workspace proof;
- Phase 3 patch vertical slice;
- Phase 4 AI Router proof.

---

# 46. Testing Strategy

## 46.1 Unit Tests

- state transitions;
- contract validation;
- cancellation logic;
- path handling;
- hashing;
- error translation;
- policy models;
- and secret redaction.

---

## 46.2 Contract Tests

- command compatibility;
- query compatibility;
- event compatibility;
- serialization round trips;
- unknown-field handling;
- and version mismatch.

---

## 46.3 Integration Tests

- Runtime startup;
- Desktop Gateway connection;
- service restart;
- project open;
- file watching;
- patch apply;
- process execution;
- provider streaming;
- and shutdown.

---

## 46.4 Architecture Tests

- service-reference direction;
- adapter boundaries;
- persistence ownership;
- no direct provider access;
- no direct protected file write;
- no direct secret access;
- no direct MCP access;
- and no desktop domain ownership.

---

## 46.5 Security Tests

- malicious path;
- assembly loading;
- unsafe deserialization;
- command injection;
- local API impersonation;
- secret logging;
- package tampering;
- and native interoperability boundaries.

---

## 46.6 Fault-Injection Tests

- Runtime process crash;
- worker crash;
- disk full;
- cancellation race;
- IPC disconnect;
- thread-pool starvation;
- native library failure;
- and corrupt state.

---

## 46.7 Recovery Tests

- interrupted patch;
- interrupted migration;
- worker restart;
- Runtime restart;
- incomplete workflow;
- and repository recovery.

---

## 46.8 Performance Tests

- startup;
- idle CPU;
- idle memory;
- event throughput;
- path enumeration;
- hashing;
- diff;
- patch apply;
- output streaming;
- and provider streaming.

---

## 46.9 Accessibility or Usability Tests

The language does not determine accessibility directly.

The Desktop Framework ADR must ensure:

- keyboard access;
- screen-reader support;
- scaling;
- contrast;
- and reduced motion.

---

# 47. Acceptance Criteria

This ADR may move to **Accepted** when:

- [ ] A C# Runtime prototype starts and stops safely.
- [ ] Service dependencies are validated.
- [ ] An optional service can fail without crashing the Runtime.
- [ ] Local IPC is authenticated.
- [ ] Commands, queries and events are versioned.
- [ ] Cancellation propagates through a worker process.
- [ ] Child-process termination is verified on Windows.
- [ ] Workspace canonicalisation tests pass.
- [ ] Junction and traversal escape tests pass.
- [ ] A multi-file patch interruption can be recovered.
- [ ] Ollama streaming and cancellation work through an adapter.
- [ ] A simulated second adapter proves provider neutrality.
- [ ] Idle resource use is measured.
- [ ] Startup performance is measured.
- [ ] A self-contained Windows publish succeeds.
- [ ] A Linux build experiment succeeds for platform-neutral projects.
- [ ] Package inventory and licensing are reviewed.
- [ ] Architecture tests enforce primary boundaries.
- [ ] Security review is complete.
- [ ] Known limitations are documented.
- [ ] Founder approval is recorded.

---

# 48. Evidence Required Before Acceptance

- [ ] Runtime boot prototype;
- [ ] authenticated IPC prototype;
- [ ] Windows process-control prototype;
- [ ] workspace path-safety prototype;
- [ ] patch transaction prototype;
- [ ] AI adapter prototype;
- [ ] startup benchmark;
- [ ] idle-resource benchmark;
- [ ] security review;
- [ ] package and licence review;
- [ ] cross-platform compilation experiment;
- [ ] founder approval.

---

# 49. Known Limitations

- The final desktop framework is undecided.
- The final IPC protocol is undecided.
- The final storage engine is undecided.
- Plugin process isolation remains to be designed.
- OS-backed Vault implementation remains to be designed.
- Native Windows process-control details require prototypes.
- Garbage-collection behaviour is not yet measured.
- Native ahead-of-time compilation suitability is not yet measured.
- Linux and macOS packaging remain deferred.
- The public plugin SDK language surface remains undecided.

---

# 50. Open Questions

- Which supported LTS .NET release should be pinned?
- Should the Runtime use a generic host pattern or a smaller custom bootstrap?
- Which contract serialisation format should be used?
- Which local IPC transport best fits Windows and future platforms?
- Should the desktop use the same language?
- Which static-analysis tools should be mandatory?
- How should native Windows API calls be isolated?
- Which libraries are acceptable for Git and diff handling?
- When should native ahead-of-time publication be evaluated?
- Which package-locking approach should be mandatory?

---

# 51. Deferred Decisions

This ADR intentionally defers:

- desktop framework to ADR-0002;
- Runtime process topology to ADR-0003;
- local IPC to ADR-0004;
- persistence to ADR-0005;
- contract serialisation to a dedicated ADR;
- logging framework to a dedicated ADR;
- test framework to a tooling ADR;
- Windows packaging to a packaging ADR;
- plugin process protocol to a Plugin Host ADR;
- and native component selection to evidence-driven ADRs.

---

# 52. Alternatives Rejected

## 52.1 Rust as the Entire Initial Platform

Rejected as the primary recommendation because the expected increase in implementation complexity and reduction in small-team throughput are not justified by current evidence.

Rust remains a valid future option for bounded components.

---

## 52.2 TypeScript and Node.js as the Trusted Runtime

Rejected as the primary recommendation because process control, filesystem security, native Windows integration and dependency-supply-chain concerns make it a weaker fit for the trusted engineering core.

TypeScript remains possible for UI or tooling.

---

## 52.3 C++ as the Primary Platform

Rejected because its memory-safety and maintenance risks are disproportionate for Opure's primarily application-level orchestration and engineering logic.

---

## 52.4 Mixed-Language Core Immediately

Rejected because the integration, build, packaging and debugging burden would slow delivery before evidence identifies a need for multiple core languages.

---

## 52.5 Python as the Primary Platform

Python was not selected as a principal finalist because:

- Runtime distribution is more complex;
- static guarantees are weaker;
- process and threading behaviour may require more care;
- desktop options would likely add another language;
- and the platform does not need to host model inference directly.

Python remains suitable for external tools, scripts or MCP servers when controlled by contracts.

---

# 53. Review Record

| Date | Reviewer | Decision | Notes |
|---|---|---|---|
| 18 July 2026 | Architecture draft | Proposed | Initial recommendation for founder review |

---

# 54. Approval

## Founder or Product Approval

- **Name:** Christopher Dyer
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Pending founder review

## Architecture Approval

- **Name or role:** Runtime Architecture Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Prototype evidence required

## Security Approval

- **Name or role:** Security Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Security prototype and package review required

---

# 55. Supersession

This ADR is superseded only when a later ADR:

- names ADR-0001 explicitly;
- explains why the primary-language decision changed;
- identifies affected services;
- describes contract and data migration;
- describes packaging impact;
- and updates this document's `Superseded by` field.

Historical ADRs remain in version control.

---

# 56. Change History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | 18 July 2026 | Founder Draft | Initial language comparison and C#/.NET recommendation |

---

# 57. Final Decision Statement

> **Opure will use C# on a supported LTS .NET release as its primary implementation platform because it provides the strongest overall balance of Windows integration, managed safety, service architecture, maintainability, performance and small-team delivery, while preserving language-neutral boundaries for future replacement or specialised native components.**

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**