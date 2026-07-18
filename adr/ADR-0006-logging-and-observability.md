# ADR-0006 — Logging and Observability

## Architecture Decision Record

**Status:** Proposed  
**Date:** 18 July 2026  
**Decision owners:** Founder and Product Owner  
**Technical owners:** Observability Architecture Owner  
**Reviewers:** Runtime Architecture Owner, Security Owner, Trust Centre Owner, Privacy Owner, Performance Owner, Recovery Owner  
**Supersedes:** None  
**Superseded by:** None  
**Related ADRs:** ADR-0001 Primary Implementation Language, ADR-0003 Runtime Process Topology, ADR-0004 Local IPC, ADR-0005 Persistence  
**Related specifications:** CHARTER-001, SPEC-001, SPEC-002, SPEC-003, SPEC-004, SPEC-005, SPEC-006, SPEC-007, SPEC-008, SPEC-009, SPEC-010, SPEC-011, SPEC-012  
**Target milestone:** Phase 0 — Founding Baseline through Phase 8 — Trust Centre and Security Completion  

---

## 1. Decision Summary

Opure should use the standard .NET observability APIs as its application instrumentation foundation:

- `Microsoft.Extensions.Logging.ILogger` for structured diagnostic logs;
- `System.Diagnostics.ActivitySource` and `Activity` for traces;
- `System.Diagnostics.Metrics.Meter` for metrics;
- and the upstream OpenTelemetry .NET SDK for collection, processing and optional standards-based export.

The default production configuration should be entirely local.

Opure should provide:

- structured append-only local diagnostic log segments;
- local trace and metric collection sufficient for the Diagnostics and Trust experiences;
- bounded retention and storage quotas;
- strict field allowlists and redaction;
- stable event identifiers;
- end-to-end correlation across Desktop, Runtime, workers, Plugin Hosts and external operations;
- health, readiness and resource metrics;
- explicit diagnostic export packages;
- and optional OTLP export only after deliberate developer configuration and consent.

Opure should not:

- require a cloud telemetry service;
- transmit telemetry by default;
- record source code, prompts, model responses, file contents or secret values by default;
- enable automatic crash upload;
- treat logs as the Trust Centre;
- treat traces as durable workflow history;
- treat metrics as audit evidence;
- or use observability as product analytics.

The Trust Centre remains a separate, purpose-built, service-owned record of developer-relevant actions and security decisions.

---

## 2. Status

Current status:

> **Proposed**

This ADR becomes **Accepted** only after prototypes demonstrate:

- structured local logging;
- trace propagation across named-pipe IPC;
- worker and Plugin Host correlation;
- metric collection;
- bounded queues;
- log rotation;
- retention;
- redaction;
- secret canary tests;
- Runtime crash recovery;
- diagnostic export preview;
- optional OTLP export controls;
- Trust Centre separation;
- and acceptable performance on the reference machine.

---

## 3. Context

Opure is a local, modular software engineering platform containing:

- a Desktop process;
- a trusted Runtime;
- trusted workers;
- isolated Plugin Hosts;
- AI provider adapters;
- MCP integrations;
- builds and tests;
- Git and package-manager commands;
- workflows;
- patch transactions;
- project memory;
- policy decisions;
- approvals;
- and recovery operations.

When a workflow fails, the developer and maintainer need to answer questions such as:

- Which process failed?
- Which service owned the operation?
- Which project and workflow were involved?
- Which command or provider request was active?
- Which permission or policy decision applied?
- How long did each stage take?
- Was a retry attempted?
- Was cancellation acknowledged?
- Did a child process terminate?
- Was a transaction committed?
- Was a record dropped because of pressure?
- Is the result safe, partial or recovery required?

Observability must make these questions answerable without creating:

- hidden cloud transmission;
- surveillance;
- uncontrolled data retention;
- secret leakage;
- or excessive performance overhead.

Opure also has a Trust Centre.

The Trust Centre and technical observability overlap in correlation, but they serve different purposes.

The architecture must prevent technical logging from becoming an unstructured substitute for the developer-facing Trust Centre.

---

## 4. Problem Statement

Opure requires a local-first observability architecture that provides structured logs, traces, metrics, health and diagnostic export across multiple local processes while protecting project data and secrets, bounding resource use and preserving a strict separation between technical diagnostics and developer-facing Trust Centre evidence.

---

## 5. Decision Drivers

The decision is evaluated against:

- alignment with the Opure Charter;
- local-first operation;
- no default telemetry export;
- C# and .NET integration;
- multi-process correlation;
- structured logging;
- trace propagation;
- metrics;
- health and readiness;
- crash diagnosis;
- secret protection;
- privacy;
- data minimisation;
- bounded retention;
- storage quotas;
- backpressure;
- performance;
- testability;
- fault injection;
- exportability;
- vendor neutrality;
- OpenTelemetry compatibility;
- Trust Centre separation;
- security reviewability;
- package maturity;
- small-team delivery;
- future support workflows;
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
- **No Hidden Telemetry**
- **No Secret Values in Logs**
- **Honest State**
- **Evidence-Based Confidence**

Relevant architecture requirements include:

- Logs and Trust Centre records are not the same.
- Secret values must never enter ordinary logs.
- Correlation identifiers should connect significant engineering operations.
- Optional component failure should remain diagnosable without blocking unrelated work.
- Observability must remain useful offline.
- External export requires explicit policy and visibility.
- Diagnostics export must be previewable and redactable.
- State-changing operations must retain authoritative recovery records outside volatile logs.
- The desktop must not infer success from a log message.
- Telemetry must not become behavioural advertising or product analytics.

---

## 7. Scope

This ADR decides:

- logging API;
- tracing API;
- metrics API;
- OpenTelemetry integration;
- local log storage;
- local trace storage policy;
- local metric storage policy;
- correlation;
- process and service identity;
- severity;
- event identifiers;
- field naming;
- redaction;
- retention;
- storage quotas;
- backpressure;
- sampling;
- health and readiness signals;
- crash diagnostics policy;
- diagnostic export;
- optional OTLP export;
- and the boundary with the Trust Centre.

This ADR does not decide:

- the final Trust Centre tamper-evidence mechanism;
- product analytics;
- customer usage analytics;
- remote support infrastructure;
- cloud telemetry tenancy;
- a specific external APM vendor;
- the final crash-dump format;
- the final update telemetry model;
- or organisation-wide enterprise observability policy.

---

## 8. Constraints

Known constraints include:

- Windows 11 is the first supported platform.
- C#/.NET is the proposed implementation stack.
- The platform must work offline.
- Multiple local processes require correlation.
- Local model and build workloads may consume substantial resources.
- Logging must not block critical engineering work.
- Logging must not consume unbounded memory or disk.
- Secret values must not appear in logs, traces, metrics, crash reports or exports.
- Project source content must not be captured by default.
- The Trust Centre uses separate persistence and semantics.
- The Desktop must be able to show diagnostics without reading raw internal files directly.
- Plugin Hosts and workers are untrusted or restricted processes.
- External provider responses are untrusted content.
- A same-user attacker may be able to read local diagnostic files under the same user account.
- Production observability must not require a collector daemon.
- External export must be disabled by default.
- Instrumentation must remain useful if the OpenTelemetry SDK is disabled.
- Hot-path instrumentation must have bounded overhead.
- Metric label cardinality must remain bounded.
- Diagnostic schemas must remain versioned.

---

## 9. Assumptions

This decision assumes:

- `Microsoft.Extensions.Logging` remains the standard .NET logging abstraction.
- `ActivitySource` and `Activity` remain the standard .NET tracing primitives.
- `Meter` remains the standard .NET metrics primitive.
- OpenTelemetry .NET supports stable logs, metrics and traces.
- OpenTelemetry exporters can be configured independently from instrumentation.
- Local file storage is appropriate for diagnostic logs.
- Detailed traces need not be retained indefinitely.
- Metrics can be retained as bounded rolling aggregates.
- Trust Centre records remain durable and service owned.
- Developers may explicitly opt into external observability tools.
- A diagnostic bundle can contain a filtered subset of local evidence.
- Source-generated logging or equivalent high-performance templates can be used on hot paths.
- All first-party processes can propagate W3C-compatible trace context through Opure IPC metadata.
- Third-party command output remains separate from Opure's own structured logs.
- Crash dumps may contain sensitive memory and therefore require stricter controls than normal logs.

---

## 10. Official Platform Evidence

Official Microsoft and OpenTelemetry documentation available on 18 July 2026 establishes that:

- .NET provides high-performance structured logging through `ILogger`.
- .NET uses `ActivitySource` and `Activity` for distributed tracing.
- .NET uses `Meter` for metrics.
- OpenTelemetry .NET collects logs, metrics and traces from the standard .NET APIs.
- OpenTelemetry .NET reports stable support for logs, metrics and traces.
- OpenTelemetry log records support trace and span correlation.
- OpenTelemetry export is separate from instrumentation and can target vendor-neutral OTLP-compatible systems.
- .NET guidance recommends structured message templates rather than eagerly formatted string interpolation for normal logging.

The implementation team must verify current package and framework status before moving this ADR to Accepted.

---

## 11. Observability Signals

Opure defines five distinct signal classes:

1. **Diagnostic Logs**
2. **Traces**
3. **Metrics**
4. **Health and Readiness**
5. **Trust Centre Records**

Crash artefacts and external command output are related evidence classes but are not ordinary Opure logs.

---

## 11.1 Diagnostic Logs

Diagnostic logs record discrete implementation events useful for:

- debugging;
- fault diagnosis;
- support;
- recovery analysis;
- and performance investigation.

Logs may describe:

- service startup;
- task admission;
- timeout;
- retry;
- failure;
- state transition;
- checkpoint;
- configuration problem;
- and recovery action.

Logs are not authoritative proof that a domain action completed.

---

## 11.2 Traces

Traces connect units of work across:

- Desktop;
- Runtime;
- service modules;
- workers;
- Plugin Hosts;
- provider adapters;
- MCP calls;
- and controlled processes.

A trace helps answer:

- where time was spent;
- which dependency failed;
- which process handled each stage;
- and how work propagated.

A trace is not durable workflow state.

---

## 11.3 Metrics

Metrics quantify bounded numerical behaviour such as:

- queue depth;
- latency;
- throughput;
- resource use;
- error rate;
- retry rate;
- model-token rate;
- WAL size;
- process count;
- and dropped diagnostic records.

Metrics are not audit evidence.

---

## 11.4 Health and Readiness

Health and readiness describe current capability state.

They answer:

- Is the process alive?
- Is the service ready?
- Is the service degraded?
- Is recovery required?
- Can this capability accept work?

A heartbeat alone does not prove readiness.

---

## 11.5 Trust Centre Records

Trust Centre records explain significant developer-facing and security-relevant actions.

Examples:

- cloud data sharing;
- approval;
- patch application;
- command execution;
- plugin capability use;
- MCP invocation;
- Git commit;
- secret detection;
- and policy denial.

Trust records are designed for developer understanding and accountability.

They are not generated by scraping logs.

---

# 12. Options Considered

The principal options are:

1. **Option A — .NET Standard APIs with OpenTelemetry Compatibility**
2. **Option B — Serilog as the Application Logging Foundation**
3. **Option C — OpenTelemetry APIs Directly Everywhere**
4. **Option D — Custom Opure Logging and Tracing Framework**
5. **Option E — Windows Event Log and ETW as the Primary Foundation**
6. **Option F — External Collector Required**
7. **Option G — Trust Centre as the Only Observability Store**
8. **Option H — Minimal Text Logs Only**

---

# 13. Option A — .NET Standard APIs with OpenTelemetry Compatibility

## 13.1 Description

Use:

- `ILogger`;
- `ActivitySource`;
- `Meter`;
- upstream OpenTelemetry SDK packages;
- an Opure local structured-log provider;
- and optional OTLP exporters.

---

## 13.2 Advantages

- Standard .NET abstractions.
- Strong C# integration.
- Structured logging.
- Stable process and service categories.
- Trace and log correlation.
- Vendor-neutral telemetry.
- Optional exporters.
- No required cloud backend.
- Supports multiple providers.
- Libraries can emit instrumentation without choosing a backend.
- Good test tooling.
- Good performance potential.
- Compatible with future enterprise observability.
- Avoids coupling application code to one logging vendor.
- Supports generated logging methods.
- Supports sampling.
- Supports metrics and traces through framework primitives.
- Allows local storage to remain Opure controlled.
- Allows OpenTelemetry to be disabled without removing basic logging.
- Supports standard trace context across processes.

---

## 13.3 Disadvantages

- Requires an Opure local file provider or selected sink.
- Requires multiple concepts and APIs.
- OpenTelemetry package configuration can be complex.
- Semantic conventions require governance.
- Exporter packages add supply-chain surface.
- Local trace persistence is not provided automatically.
- Diagnostic UI requires local indexing or projection.
- Automatic instrumentation may capture more than desired.
- OpenTelemetry versioning requires review.
- Metrics can create cardinality problems.
- Structured fields can still leak sensitive data if instrumentation is careless.

---

## 13.4 Risks

- Teams may add arbitrary attributes.
- Automatic instrumentation may capture URLs or commands.
- Logs may contain raw exceptions with sensitive messages.
- Export may be enabled without sufficient preview.
- Local storage provider may block under pressure.
- Trace and log schemas may drift.
- Metric labels may include project paths or IDs.
- Developers may treat correlation as authority.
- Third-party instrumentation may violate Opure minimisation rules.
- Multiple exporters may duplicate data.

---

## 13.5 Mitigations

- allowlisted fields;
- redaction processors;
- explicit instrumentation registry;
- package review;
- export disabled by default;
- bounded local sink;
- stable naming standards;
- cardinality tests;
- secret canary tests;
- architecture tests;
- and Trust Centre separation.

---

## 13.6 Estimated Adoption Cost

- **Initial implementation:** Moderate
- **Operational complexity:** Moderate
- **Migration difficulty:** Low
- **Replacement difficulty:** Low to Moderate

---

# 14. Option B — Serilog as the Application Logging Foundation

## 14.1 Description

Use Serilog APIs and sinks directly throughout Opure.

Use additional systems for tracing and metrics.

---

## 14.2 Advantages

- Mature structured logging.
- Rich sink ecosystem.
- Rolling file support.
- Strong enrichment.
- Strong filtering.
- Good developer experience.
- Widely used.
- Good JSON output support.
- Can integrate with OpenTelemetry.

---

## 14.3 Disadvantages

- Application code becomes coupled to a third-party logging API if used directly.
- Traces and metrics still require other APIs.
- Sink packages expand the dependency graph.
- Enrichers may capture sensitive data.
- Direct Serilog use weakens standard `ILogger` abstraction.
- A file sink does not solve Trust Centre semantics.
- Multiple configuration models may emerge.
- Replacement cost increases.
- Some high-performance .NET logging patterns target `ILogger`.

---

## 14.4 Decision Relevance

A Serilog provider may later be evaluated as an implementation of the `ILogger` sink.

Serilog is not selected as the application instrumentation API.

---

## 14.5 Estimated Adoption Cost

- **Initial implementation:** Low
- **Operational complexity:** Moderate
- **Migration difficulty:** Moderate
- **Replacement difficulty:** Moderate

---

# 15. Option C — OpenTelemetry APIs Directly Everywhere

## 15.1 Description

Use OpenTelemetry-specific APIs directly for logs, traces and metrics throughout all services.

---

## 15.2 Advantages

- Direct standards alignment.
- Uniform concepts.
- Exporter ecosystem.
- Cross-language terminology.
- Strong vendor neutrality.

---

## 15.3 Disadvantages

- .NET already provides native instrumentation APIs.
- Application code becomes unnecessarily coupled to SDK packages.
- Libraries may initialise collection accidentally.
- Logging ergonomics may be weaker than `ILogger`.
- Package upgrades affect more code.
- Disabling OpenTelemetry becomes harder.
- Standard .NET instrumentation integration is reduced.

---

## 15.4 Estimated Adoption Cost

- **Initial implementation:** Moderate
- **Operational complexity:** Moderate
- **Migration difficulty:** Moderate
- **Replacement difficulty:** Moderate

---

# 16. Option D — Custom Opure Logging and Tracing Framework

## 16.1 Description

Create proprietary APIs, record models, exporters and context propagation.

---

## 16.2 Advantages

- Complete control.
- Exact Opure semantics.
- Minimal external dependencies.
- Can optimise for local storage.
- Can enforce custom privacy rules.

---

## 16.3 Disadvantages

- Reinvents mature standards.
- High implementation burden.
- High security-review burden.
- Difficult interoperability.
- Custom trace propagation.
- Custom metrics.
- Custom tooling.
- Custom exporters.
- Increased maintenance.
- More difficult external support.
- Replacement becomes expensive.

---

## 16.4 Estimated Adoption Cost

- **Initial implementation:** Very High
- **Operational complexity:** High
- **Migration difficulty:** High
- **Replacement difficulty:** High

---

# 17. Option E — Windows Event Log and ETW as the Primary Foundation

## 17.1 Description

Use Windows Event Log, EventSource and ETW as the primary diagnostic system.

---

## 17.2 Advantages

- Strong Windows integration.
- Mature system tooling.
- Efficient tracing.
- Operating-system retention.
- Administrative diagnostics.
- Useful for crash and performance analysis.
- No custom file store required for some data.

---

## 17.3 Disadvantages

- Windows specific.
- Future Linux and macOS path is weak.
- User access and permissions can be complex.
- Event Log is not ideal for high-volume application detail.
- Structured application schemas are less convenient.
- Export and user preview are awkward.
- Project privacy boundaries are weak.
- ETW tooling is specialist.
- Does not provide the complete logs, metrics and traces architecture.
- Can require installation registration.

---

## 17.4 Decision Relevance

EventSource or ETW may be used as optional platform diagnostics.

They are not selected as the primary cross-platform observability model.

---

## 17.5 Estimated Adoption Cost

- **Initial implementation:** Moderate
- **Operational complexity:** Moderate to High
- **Migration difficulty:** High
- **Replacement difficulty:** High

---

# 18. Option F — External Collector Required

## 18.1 Description

Require an OpenTelemetry Collector or external APM service for all useful observability.

---

## 18.2 Advantages

- Mature processing pipeline.
- Strong exporters.
- Advanced storage and queries.
- Tail sampling.
- Enterprise integration.
- Centralised dashboards.

---

## 18.3 Disadvantages

- Additional process or remote dependency.
- More installation complexity.
- Conflicts with simple local operation.
- Can transmit project-related data.
- Requires configuration.
- Adds support burden.
- External tool failure reduces diagnostics.
- May become a hidden cloud requirement.
- Inappropriate as the default for one local developer.

---

## 18.4 Decision Relevance

External collectors may be supported optionally.

They are not required.

---

## 18.5 Estimated Adoption Cost

- **Initial implementation:** High
- **Operational complexity:** High
- **Migration difficulty:** Low
- **Replacement difficulty:** Low

---

# 19. Option G — Trust Centre as the Only Observability Store

## 19.1 Description

Record all technical diagnostics as Trust Centre records.

---

## 19.2 Advantages

- One user-visible history.
- Simple conceptual storage.
- Strong correlation.
- Fewer systems.

---

## 19.3 Disadvantages

- Trust Centre becomes noisy.
- Technical details overwhelm developer actions.
- Retention needs conflict.
- High-volume logs become expensive.
- Security records mix with debug output.
- Debug traces are not stable audit events.
- Failures before Trust Centre startup become invisible.
- Performance metrics do not fit.
- Log level configuration could alter developer accountability.

---

## 19.4 Decision

Rejected.

The Trust Centre and diagnostics must remain separate.

---

# 20. Option H — Minimal Text Logs Only

## 20.1 Description

Write human-readable text files with timestamps and severity.

---

## 20.2 Advantages

- Simple.
- Easy to inspect.
- Low implementation cost.
- Minimal dependencies.
- Works offline.

---

## 20.3 Disadvantages

- Weak structure.
- Weak cross-process correlation.
- Hard to query.
- Hard to redact reliably.
- No metrics.
- No traces.
- No schema.
- Difficult automated diagnosis.
- Difficult versioning.
- Easy accidental sensitive data leakage.
- Poor support for performance analysis.

---

## 20.4 Estimated Adoption Cost

- **Initial implementation:** Low
- **Operational complexity:** Moderate
- **Migration difficulty:** High
- **Replacement difficulty:** High

---

# 21. Comparison Matrix

Scores:

- **1** — poor
- **2** — weak
- **3** — acceptable
- **4** — strong
- **5** — excellent

| Criterion | Weight | .NET + OTel | Serilog Direct | OTel Direct | Custom | ETW/Event Log | Collector Required | Trust Only | Text Only |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Charter alignment | 5 | 5 | 4 | 4 | 4 | 3 | 2 | 2 | 3 |
| Local-first | 5 | 5 | 5 | 5 | 5 | 5 | 2 | 5 | 5 |
| No default export | 5 | 5 | 5 | 5 | 5 | 5 | 1 | 5 | 5 |
| Structured logs | 5 | 5 | 5 | 4 | 5 | 3 | 5 | 4 | 1 |
| Traces | 5 | 5 | 2 | 5 | 4 | 4 | 5 | 2 | 1 |
| Metrics | 5 | 5 | 2 | 5 | 4 | 3 | 5 | 2 | 1 |
| .NET integration | 5 | 5 | 4 | 4 | 3 | 4 | 4 | 4 | 4 |
| Vendor neutrality | 4 | 5 | 3 | 5 | 5 | 2 | 5 | 5 | 5 |
| Privacy control | 5 | 5 | 4 | 4 | 5 | 3 | 2 | 4 | 3 |
| Trust separation | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 1 | 4 |
| Cross-platform | 4 | 5 | 5 | 5 | 4 | 1 | 5 | 5 | 5 |
| Small-team fit | 5 | 4 | 4 | 3 | 1 | 3 | 2 | 3 | 5 |
| External tooling | 3 | 5 | 4 | 5 | 1 | 3 | 5 | 2 | 2 |
| Testability | 4 | 5 | 4 | 4 | 3 | 3 | 3 | 3 | 2 |
| Performance control | 4 | 5 | 4 | 4 | 5 | 5 | 4 | 2 | 4 |
| Replacement cost | 3 | 5 | 3 | 3 | 1 | 2 | 4 | 2 | 2 |
| **Indicative weighted total** |  | **423** | **336** | **372** | **328** | **298** | **303** | **254** | **255** |

The standard .NET APIs with OpenTelemetry compatibility provide the strongest overall fit.

---

# 22. Decision

Opure will provisionally adopt:

> **Microsoft.Extensions.Logging for structured logs, ActivitySource for traces, Meter for metrics and the upstream OpenTelemetry .NET SDK for collection and optional standards-based export.**

The default production configuration will:

- write structured local diagnostic logs;
- collect bounded local metrics;
- collect sampled local traces;
- expose health and diagnostics through Runtime services;
- keep external exporters disabled;
- keep automatic instrumentation allowlisted;
- and require explicit developer action for diagnostic export.

This decision does not approve:

- mandatory external telemetry;
- product analytics;
- automatic crash upload;
- unreviewed automatic instrumentation;
- payload logging;
- source-code logging;
- secret logging;
- high-cardinality metrics;
- or generating Trust Centre history from logs.

This decision is:

- [ ] Permanent until superseded
- [x] Provisional pending prototype evidence
- [ ] Experimental only
- [ ] Limited to Runtime
- [ ] Limited to Windows permanently

---

# 23. Rationale

The .NET platform already provides the appropriate instrumentation APIs.

Using those APIs keeps application code:

- idiomatic;
- testable;
- backend independent;
- and compatible with OpenTelemetry.

OpenTelemetry provides a vendor-neutral path for:

- collection;
- processing;
- correlation;
- sampling;
- and optional export.

A custom framework would duplicate mature work.

Direct dependence on one logging vendor would increase replacement cost.

A mandatory collector would conflict with Opure's local-first design.

The selected architecture allows useful local diagnostics with no external service while preserving optional enterprise integration later.

---

# 24. Signal Separation

The following table is normative.

| Signal | Primary purpose | Authoritative? | Default retention | External export default |
|---|---|---:|---|---|
| Diagnostic log | Technical diagnosis | No | Bounded | Disabled |
| Trace | Causal and latency analysis | No | Short and sampled | Disabled |
| Metric | Health and performance trends | No | Rolling aggregate | Disabled |
| Health state | Current readiness | Authoritative only for current health | Current plus short history | Disabled |
| Trust record | Developer-relevant action and accountability | Yes for recorded action history | Policy controlled | Disabled |
| Workflow record | Durable workflow state | Yes | Service policy | Not an observability export |
| Patch journal | Recovery | Yes | Recovery policy | Never automatic |
| Command output | External tool evidence | Tool result evidence | Build or task policy | Never automatic |
| Crash artefact | Post-failure diagnosis | No | Explicit and short | Never automatic |

---

# 25. Instrumentation API Rules

## 25.1 Logging

First-party code should depend on:

```text
Microsoft.Extensions.Logging.ILogger<TCategory>
```

or approved generated logging abstractions.

---

## 25.2 Tracing

First-party modules should declare stable `ActivitySource` instances.

Example conceptual names:

```text
Opure.Runtime
Opure.Gateway
Opure.Workflows
Opure.Patching
Opure.Build
Opure.Repository
Opure.AI
Opure.Knowledge
Opure.Plugins
Opure.Mcp
Opure.Persistence
```

---

## 25.3 Metrics

First-party modules should declare stable `Meter` instances using the same or similarly governed scope names.

---

## 25.4 SDK Initialisation

Only process composition roots should configure:

- logging providers;
- OpenTelemetry SDK;
- processors;
- samplers;
- readers;
- and exporters.

Libraries and service modules emit instrumentation.

They do not choose its destination.

---

# 26. Package Strategy

The initial package set may include:

- `Microsoft.Extensions.Logging`;
- `Microsoft.Extensions.Logging.Abstractions`;
- `OpenTelemetry`;
- `OpenTelemetry.Extensions.Hosting`;
- `OpenTelemetry.Exporter.OpenTelemetryProtocol` only for optional export;
- and reviewed instrumentation packages for selected framework components.

The exact versions require package pinning.

---

## 26.1 Upstream OpenTelemetry

Opure should prefer the upstream OpenTelemetry .NET packages rather than a cloud-vendor-specific distribution.

A vendor-specific distribution may be supported later only as an optional integration.

---

## 26.2 Automatic Instrumentation

Automatic instrumentation is disabled by default unless explicitly allowlisted.

Reasons include:

- sensitive URL capture;
- excessive spans;
- third-party semantic drift;
- high cardinality;
- and unexpected dependency behaviour.

---

## 26.3 Reviewed Instrumentation

Each instrumentation package requires:

- owner;
- version;
- data inventory;
- attributes emitted;
- redaction review;
- cardinality review;
- performance review;
- and disable switch.

---

# 27. Structured Logging

Every production log event should have a stable structure.

A log event should include where relevant:

- timestamp;
- observed timestamp;
- severity;
- event identifier;
- event name;
- category;
- safe message template;
- process class;
- process instance;
- Runtime instance;
- service;
- project pseudonymous identity where allowed;
- correlation identifier;
- trace identifier;
- span identifier;
- operation identifier;
- stable error code;
- and allowlisted attributes.

---

## 27.1 Stable Event Identifier

Every recurring first-party log pattern should have a stable numeric or namespaced identifier.

Example conceptual identifiers:

```text
Runtime.Starting
Runtime.Ready
Runtime.Degraded
Service.StartFailed
Ipc.AuthenticationRejected
Workflow.StageFailed
Patch.RecoveryRequired
Persistence.MigrationFailed
Plugin.Quarantined
Provider.Unavailable
```

---

## 27.2 Event Identifier Registry

A central registry should define:

- owner;
- event identifier;
- event name;
- severity;
- message template;
- allowed attributes;
- data classification;
- retention relevance;
- and Trust Centre relationship.

Identifiers must not be reused for a different meaning.

---

## 27.3 Message Templates

Logging should use stable message templates.

Avoid:

- string interpolation;
- concatenation;
- arbitrary object serialisation;
- and user-controlled format strings.

Example:

```text
"Worker {WorkerId} exited with code {ExitCode}"
```

not:

```text
$"Worker {workerId} exited with code {exitCode}"
```

for routine structured production logging.

---

## 27.4 Source-Generated Logging

Hot-path or high-volume logging should use source-generated logging methods or equivalent compile-time templates where practical.

Benefits include:

- reduced allocations;
- stable event definitions;
- and easier review.

---

## 27.5 Object Logging

Arbitrary complex objects must not be destructured into logs by default.

Only approved fields should be emitted.

Calling `ToString()` on domain or secret-bearing objects for logging is prohibited unless the type guarantees a safe representation.

---

# 28. Log Severity

Opure uses standard levels:

- Trace
- Debug
- Information
- Warning
- Error
- Critical

---

## 28.1 Trace

Detailed diagnostic detail for short-term troubleshooting.

Disabled by default in normal production operation.

---

## 28.2 Debug

Developer-oriented internal state useful during investigation.

Normally disabled or sampled in production.

---

## 28.3 Information

Meaningful normal lifecycle events.

Information should not include every loop iteration or message frame.

---

## 28.4 Warning

Unexpected or degraded behaviour where the operation may continue safely.

Warnings must be actionable or diagnostically meaningful.

---

## 28.5 Error

An operation failed, but the process or Runtime may continue.

---

## 28.6 Critical

A process, security boundary, data-integrity capability or essential Runtime function is unusable or unsafe.

Critical must not be used merely to attract attention.

---

## 28.7 Severity Is Not Risk

Log severity and action risk are separate.

A successful Critical-risk approval may produce an Information diagnostic log and a Critical-risk Trust Centre record.

---

# 29. Log Categories

Categories should map to the emitting type or stable subsystem.

Category names must not include:

- project path;
- user name;
- provider prompt;
- plugin-provided arbitrary text;
- or high-cardinality identifiers.

---

# 30. Correlation Model

Every meaningful operation should carry:

- Runtime instance;
- trace identifier;
- span identifier;
- correlation identifier;
- operation identifier;
- and causation identifier where applicable.

---

## 30.1 Trace Identifier

Trace identifier links causal work across process boundaries.

It may be sampled for detailed span retention.

---

## 30.2 Correlation Identifier

Correlation identifier is an Opure stable grouping identifier for a developer-visible operation.

It may exist even when no trace is sampled.

---

## 30.3 Operation Identifier

Operation identifier identifies a durable command or workflow operation.

It belongs to the owning service.

---

## 30.4 Causation Identifier

Causation links an event or command to the prior action that caused it.

---

## 30.5 No Identity Overloading

A project identifier must not be used as a trace identifier.

A trace identifier must not be used as an approval identifier.

Identifiers retain distinct semantics.

---

# 31. Trace Context Propagation

Trace context should propagate through:

- Desktop-to-Runtime IPC;
- Runtime service dispatch;
- Runtime-to-worker IPC;
- Runtime-to-Plugin-Host IPC;
- provider adapter calls;
- MCP calls;
- and controlled command-process metadata where possible.

---

## 31.1 IPC

ADR-0004 metadata should carry W3C-compatible trace context and Opure correlation fields.

Authentication and authorisation remain separate.

---

## 31.2 External HTTP

Approved HTTP instrumentation may propagate trace context only when:

- destination policy allows it;
- no sensitive internal identifier is exposed improperly;
- and the external destination is expected to understand it.

Remote trace propagation may be disabled for privacy.

---

## 31.3 Command Processes

Command-line tools generally do not receive trace context unless an approved adapter supports it.

The Runtime should correlate:

- process launch;
- output;
- exit;
- and result

through its own operation identity.

---

# 32. Trace Design

A span should represent a meaningful bounded unit of work.

Good examples:

- `gateway.command`
- `workflow.stage`
- `patch.validate`
- `patch.apply`
- `build.execute`
- `repository.status`
- `ai.request`
- `knowledge.query`
- `plugin.invoke`
- `mcp.invoke`
- `persistence.transaction`

---

## 32.1 Span Names

Span names must be stable and low cardinality.

Do not put:

- file path;
- project name;
- plugin-provided command;
- model prompt;
- URL with identifiers;
- or user text

inside span names.

---

## 32.2 Span Attributes

Attributes must be allowlisted.

Examples of acceptable bounded attributes:

- service;
- operation kind;
- provider type;
- model locality;
- result class;
- retry count;
- risk class;
- process class;
- cache result;
- and data-class category.

---

## 32.3 Sensitive Span Attributes

Do not record:

- prompt;
- model response;
- source code;
- file contents;
- command environment;
- secret reference value;
- access token;
- full absolute path;
- or raw external response.

---

## 32.4 Span Events

Span events may record important diagnostic milestones.

They must remain bounded.

A token or log line must not become one span event each.

---

## 32.5 Span Status

Span status should represent technical success or failure of the unit.

It does not replace domain result fields.

---

# 33. Trace Sampling

Tracing should use sampling to control cost.

---

## 33.1 Default Sampling

A proposed initial local production policy is:

- retain all Error and Critical traces where technically possible;
- retain all recovery and security-boundary traces;
- retain a bounded sample of normal interactive traces;
- retain a lower sample of background maintenance traces;
- and allow temporary targeted tracing by service or operation.

Exact percentages require benchmarks.

---

## 33.2 Head Sampling

Head sampling may reduce overhead early.

It cannot know final outcome.

---

## 33.3 Tail-Informed Local Retention

Opure may buffer bounded trace summaries long enough to retain failed operations even when ordinary successful traces are sampled.

A full tail-sampling collector is not required initially.

---

## 33.4 No Sampling for Trust Records

Trust Centre records are not sampled.

---

# 34. Metrics

Metrics should use stable names, units and low-cardinality attributes.

---

## 34.1 Metric Types

Use:

- counters;
- up-down counters;
- histograms;
- observable gauges;
- and observable counters

according to the meaning of the measurement.

---

## 34.2 Required Runtime Metrics

Initial Runtime metrics may include:

- process CPU;
- working set;
- managed heap;
- thread-pool queue;
- garbage collection;
- process count;
- worker count;
- Plugin Host count;
- service readiness;
- scheduler queue depth;
- scheduler wait;
- and Runtime restart count.

---

## 34.3 Required IPC Metrics

- active connections;
- authentication failures;
- calls;
- latency;
- active streams;
- stream backlog;
- message size;
- cancellations;
- deadlines;
- reconnects;
- and rejected clients.

---

## 34.4 Required Persistence Metrics

- write queue;
- transaction latency;
- busy count;
- retry count;
- WAL size;
- checkpoint duration;
- database size;
- backup age;
- migration state;
- integrity-check age;
- and content-store size.

---

## 34.5 Required Workflow Metrics

- active workflows;
- waiting approvals;
- stage duration;
- retries;
- cancellations;
- failures;
- recoveries;
- and queue wait.

---

## 34.6 Required AI Metrics

- provider availability;
- model load state;
- request latency;
- first-token latency;
- token rate where available;
- cancellation;
- structured-output failures;
- retries;
- local versus remote class;
- and estimated cost where applicable.

Metrics must not contain prompt or response content.

---

## 34.7 Required Build Metrics

- active builds;
- queue wait;
- duration;
- output rate;
- process-tree count;
- cancellation time;
- pass or fail;
- test counts;
- and artefact size.

---

## 34.8 Metric Cardinality Rules

Metric attributes must not contain:

- project ID by default;
- workflow ID;
- trace ID;
- file path;
- repository URL;
- provider request ID;
- arbitrary plugin ID when unbounded;
- model prompt;
- error message;
- or user-provided text.

---

## 34.9 Project Metrics

Per-project operational detail should be available through service queries and diagnostics.

It should not create unbounded metric label cardinality.

---

# 35. Health and Readiness

Each process and service should expose:

- liveness;
- readiness;
- degraded reason;
- last successful check;
- last failure;
- dependencies;
- and recovery requirement.

---

## 35.1 Liveness

Liveness means the component is responsive enough to report.

It does not mean it can accept work.

---

## 35.2 Readiness

Readiness means the component can perform its declared capability safely.

---

## 35.3 Degraded

Degraded means some capability is unavailable or impaired while safe partial use remains.

---

## 35.4 Recovery Required

Recovery Required means ordinary work must not continue for the affected capability.

---

# 36. Trust Centre Boundary

The Trust Centre must not ingest arbitrary application logs.

A Trust record is emitted explicitly by the owning service when a qualifying developer-relevant action occurs.

---

## 36.1 Shared Correlation

A Trust record may include:

- correlation identifier;
- operation identifier;
- trace identifier where safe;
- process;
- service;
- and technical-evidence reference.

---

## 36.2 Technical Evidence Reference

A Trust record may link to a bounded local diagnostic view.

The link should contain:

- diagnostic query;
- time range;
- process;
- and correlation.

It must not copy entire raw logs into the Trust Centre.

---

## 36.3 Trust Record Failure

If Trust Centre persistence is unavailable, protected actions follow SPEC-008 policy.

A successful diagnostic log write does not make the protected action auditable.

---

# 37. Local Diagnostic Log Store

Opure should implement a local structured log provider that writes append-only segments.

Proposed physical format:

- newline-delimited JSON;
- UTF-8;
- one structured event per line;
- versioned envelope;
- and checksum or segment integrity metadata where practical.

---

## 37.1 Why JSON Lines

Benefits include:

- local inspectability;
- streaming write;
- easy segmentation;
- partial-file recovery;
- standard tooling;
- and simple export.

The provider may use a more compact binary format later if evidence justifies it.

---

## 37.2 Segment Layout

Conceptual layout:

```text
diagnostics/
├── runtime/
│   ├── current/
│   └── archive/
├── desktop/
├── workers/
├── plugins/
└── crashes/
```

Actual filenames should use:

- process class;
- process instance;
- start time;
- segment sequence;
- and format version.

---

## 37.3 Process-Local Writing

Each process should write its own local log segments.

The Runtime should not become the synchronous sink for every process log event.

Benefits include:

- failure isolation;
- reduced IPC traffic;
- and logs surviving Runtime communication failure.

---

## 37.4 Aggregated Diagnostics

The Runtime may maintain a bounded index of process log manifests and safe summaries.

The Desktop queries diagnostics through the Desktop Gateway.

It does not scan arbitrary files directly.

---

## 37.5 Segment Rotation

Rotate by:

- size;
- time;
- process restart;
- and format change.

Exact thresholds require benchmark evidence.

---

## 37.6 Flush Policy

Critical events and process shutdown should request bounded flush.

Normal events may use buffered writes.

Logs must not force a disk flush for every Information event.

---

## 37.7 Crash Tolerance

The log format should tolerate:

- incomplete final line;
- abrupt process termination;
- and missing final manifest update.

Readers must ignore incomplete records safely.

---

# 38. Diagnostic Queue

Each process should use a bounded asynchronous diagnostic queue.

---

## 38.1 No Blocking Hot Path

Ordinary Information, Debug and Trace logging should not block critical operations on disk IO.

---

## 38.2 Queue Full Policy

Suggested priority:

1. preserve Critical;
2. preserve Error;
3. preserve Warning;
4. preserve selected Information lifecycle events;
5. drop or sample Debug and Trace first.

The system must record a dropped-record summary when capacity returns.

---

## 38.3 Never Drop Silently

Dropped diagnostics should increment:

- counter;
- severity-specific counter;
- and process-specific health indicator.

A summary log should state the count without reproducing lost payloads.

---

## 38.4 Critical Fallback

If the normal sink is unavailable, Critical and Error events may use a bounded emergency file or operating-system fallback.

This fallback must remain redacted.

---

# 39. Retention

Retention must be bounded by:

- age;
- total size;
- per-process size;
- and free-disk pressure.

---

## 39.1 Proposed Initial Defaults

Subject to measurement, proposed defaults are:

- Information and above: retain up to 14 days;
- Debug: retain up to 3 days when enabled;
- Trace: retain up to 24 hours when enabled;
- total ordinary diagnostic quota: 1 GiB per profile;
- emergency diagnostics: 128 MiB;
- local sampled trace detail: 7 days or quota;
- rolling metric history: 30 days at reduced resolution.

These values are provisional.

---

## 39.2 Security Events

Security-relevant developer actions remain in the Trust Centre, not retained merely through log policy.

---

## 39.3 Pinned Diagnostic Session

A developer may pin a diagnostic session temporarily.

Pinning should display:

- size;
- content classes;
- expiry;
- and deletion control.

---

## 39.4 Disk Pressure

Under disk pressure, Opure should:

- delete expired Trace and Debug first;
- compact metric history;
- stop verbose sessions;
- preserve Error and Critical within quota;
- preserve Trust Centre and recovery data according to their own policies;
- and notify the developer.

---

# 40. Local Trace Retention

Full raw traces can be voluminous.

The initial implementation should retain:

- sampled successful traces;
- failed traces;
- recovery traces;
- and bounded span summaries.

---

## 40.1 Trace Store

Trace summaries may use:

- service-owned SQLite metadata;
- content references for larger serialised batches;
- or bounded OpenTelemetry-compatible local segments.

The exact local trace store is an implementation decision under this ADR.

---

## 40.2 Trace Expiry

Expired traces may be deleted without altering:

- workflow history;
- patch history;
- Trust Centre;
- or authoritative operation outcomes.

---

# 41. Metric Retention

Metrics should be aggregated into rolling intervals.

Suggested resolutions:

- recent: high resolution;
- medium term: one-minute or five-minute aggregates;
- older retained period: hourly aggregates.

Exact retention requires measurement.

---

## 41.1 Metrics Are Local Diagnostics

Default metric storage remains local.

No metrics are exported automatically.

---

# 42. Field Classification

Every diagnostic field should be classified as:

- Safe;
- Pseudonymous;
- Sensitive;
- Secret;
- Prohibited.

---

## 42.1 Safe

Examples:

- stable service name;
- process class;
- operation kind;
- error code;
- duration;
- result class;
- retry count;
- queue depth.

---

## 42.2 Pseudonymous

Examples:

- random project identifier;
- workflow identifier;
- plugin identifier;
- provider instance identifier.

Pseudonymous does not mean anonymous.

---

## 42.3 Sensitive

Examples:

- relative file path;
- repository host;
- package name;
- command executable path;
- model name;
- project language;
- exception detail.

Sensitive fields require purpose and retention review.

---

## 42.4 Secret

Examples:

- token;
- password;
- private key;
- connection string;
- authentication cookie;
- secret environment value.

Secret fields are prohibited.

---

## 42.5 Prohibited

Examples:

- source file content;
- full AI prompt;
- full model response;
- raw clipboard;
- complete environment;
- complete command line with secrets;
- Vault payload;
- or arbitrary object dump.

---

# 43. Redaction

Redaction must happen before a record enters:

- queue;
- file;
- OpenTelemetry processor;
- exporter;
- diagnostic UI;
- or crash metadata.

Redaction after persistence is insufficient.

---

## 43.1 Allowlist First

Preferred instrumentation design:

- emit only approved fields;
- then apply defensive redaction.

A denylist alone is insufficient.

---

## 43.2 Redaction Outcomes

A field may be:

- retained;
- normalised;
- truncated;
- hashed;
- replaced with classification marker;
- or removed.

---

## 43.3 Secret Reference

A secret reference identifier may be logged only if:

- it is not itself sensitive;
- it cannot retrieve the secret;
- and it serves a diagnostic purpose.

The raw value is never logged.

---

## 43.4 Paths

Absolute paths can reveal:

- user name;
- organisation;
- repository;
- and project structure.

Logs should prefer:

- workspace-relative path;
- path classification;
- file identity;
- or one-way scoped token.

---

## 43.5 URLs

URLs should be normalised.

Remove or redact:

- query strings;
- fragments;
- user information;
- access signatures;
- and secret path segments.

---

## 43.6 Commands

Log:

- command plan identifier;
- executable identity;
- argument count;
- working-directory classification;
- and risk.

Do not log the full command line by default.

---

## 43.7 Exceptions

Exception type and stable error details may be logged.

Exception messages and stack traces require redaction because they may contain:

- paths;
- SQL;
- provider content;
- or secrets.

---

# 44. Redaction Architecture

The observability pipeline should include:

1. typed instrumentation;
2. field classification;
3. allowlist validation;
4. secret detection;
5. normalisation;
6. truncation;
7. sink-specific policy;
8. and final serialisation.

---

## 44.1 Sink Policy

Local diagnostic sink may permit more Sensitive fields than an external export.

External export uses a stricter policy.

---

## 44.2 Redaction Failure

If redaction fails for a record containing unknown complex data:

- drop the unsafe field;
- retain safe event metadata;
- increment a redaction-failure metric;
- and never fail open.

---

# 45. Secret Canary Testing

Automated tests should inject unique canary secrets through:

- configuration;
- environment;
- provider error;
- command output;
- exception;
- plugin message;
- MCP response;
- and database failure.

Tests must verify the canary does not appear in:

- logs;
- traces;
- metrics;
- Trust Centre;
- diagnostics;
- crash metadata;
- or export bundle.

---

# 46. External Command Output

Build, test, Git and package-manager output is not automatically an Opure diagnostic log.

It belongs to the owning operation record.

---

## 46.1 Output Handling

Command output should be:

- streamed;
- bounded;
- classified;
- secret scanned;
- stored according to build or task policy;
- and linked through correlation.

---

## 46.2 Logging Command Output

Opure logs may state:

- output stream opened;
- line count;
- bytes;
- truncation;
- parser failure;
- and exit status.

They should not duplicate every output line.

---

# 47. AI Observability

AI diagnostics may record:

- provider class;
- model identifier;
- locality;
- request purpose;
- context-size class;
- output-size class;
- first-token latency;
- duration;
- cancellation;
- schema-validation result;
- and safe provider error code.

---

## 47.1 AI Data Prohibited by Default

Do not record:

- prompt;
- context content;
- response;
- chain of thought;
- source snippets;
- or secret-bearing provider payload.

---

## 47.2 Debug Capture

A developer may explicitly create a bounded AI diagnostic capture.

It must show:

- exact data classes;
- retention;
- destination;
- and redaction.

Raw chain of thought must never be requested or stored.

---

# 48. Plugin Observability

Plugin Host logs should include:

- plugin identifier;
- package version;
- host instance;
- capability name;
- duration;
- result;
- crash;
- restart;
- and quarantine.

Plugin-provided log fields are untrusted.

---

## 48.1 Plugin Log Ingestion

Plugin logs must pass:

- size limits;
- rate limits;
- schema validation;
- field allowlist;
- redaction;
- and classification.

---

## 48.2 Plugin Namespace

Plugin event names must remain in a plugin namespace and cannot impersonate Opure core event identifiers.

---

# 49. MCP Observability

MCP diagnostics may record:

- server identity;
- capability name;
- destination class;
- duration;
- result;
- cancellation;
- schema-validation result;
- and safe error.

Do not record full request or response by default.

---

# 50. IPC Observability

ADR-0004 IPC metrics and traces should include:

- method;
- client class;
- result;
- duration;
- request-size class;
- response-size class;
- stream type;
- and authentication result.

Do not include:

- authentication proof;
- session token;
- payload;
- endpoint secret;
- or raw project data.

---

# 51. Persistence Observability

ADR-0005 persistence telemetry should include:

- domain;
- operation kind;
- duration;
- rows class;
- transaction result;
- busy;
- retry;
- WAL size;
- checkpoint;
- migration;
- backup;
- and integrity state.

Do not log SQL parameter values by default.

---

# 52. File and Patch Observability

File and patch diagnostics may include:

- file identity;
- workspace-relative path under Sensitive policy;
- operation type;
- size class;
- revision match;
- conflict class;
- validation result;
- and transaction state.

Do not log file content.

---

# 53. Repository Observability

Repository diagnostics may include:

- operation kind;
- repository identity;
- branch classification;
- duration;
- exit;
- conflict state;
- and remote host classification.

Do not log credentials or full remote URLs containing sensitive information.

---

# 54. Crash Diagnostics

Crash diagnostics require stricter handling because memory can contain secrets and source content.

---

## 54.1 Default Crash Behaviour

By default, Opure should create:

- local crash metadata;
- process identity;
- version;
- exception type;
- safe stack representation;
- recent safe Error and Critical summaries;
- and Runtime state classification.

Automatic remote upload is disabled.

---

## 54.2 Memory Dump

Full or mini memory dumps are disabled by default unless the dump type is proven sufficiently bounded and useful.

Creating a dump should require:

- explicit developer action or policy;
- storage warning;
- sensitivity warning;
- short retention;
- restrictive permissions;
- and export preview.

---

## 54.3 Crash Upload

Opure must never upload a crash artefact automatically.

A future support upload requires:

- preview;
- redaction;
- destination;
- consent;
- and Trust Centre record.

---

# 55. Diagnostics UI

The Desktop should provide a Diagnostics area showing:

- Runtime health;
- service health;
- process inventory;
- current log level;
- dropped records;
- storage use;
- recent errors;
- active traces;
- queue depth;
- exporter state;
- and diagnostic-session controls.

---

## 55.1 Raw Log View

Raw local logs may be displayed through a bounded service query.

The Desktop must not read diagnostic directories directly.

---

## 55.2 Filtering

Support filters for:

- time;
- severity;
- process;
- service;
- project;
- correlation;
- error code;
- event identifier;
- and text over safe rendered fields.

---

## 55.3 Sensitive Fields

Sensitive fields should be hidden by default and revealed deliberately.

---

# 56. Diagnostic Sessions

A developer may start a temporary diagnostic session.

A session may enable:

- Debug or Trace for selected services;
- increased trace sampling;
- selected instrumentation;
- and longer local retention.

---

## 56.1 Session Scope

A session must declare:

- services;
- process classes;
- project scope;
- duration;
- expected size;
- data classes;
- and external export state.

---

## 56.2 Session Expiry

A diagnostic session expires automatically.

It must not leave verbose logging enabled indefinitely.

---

## 56.3 Sensitive Session

A session permitting Sensitive fields requires an additional warning and remains local unless separately exported.

Secret and Prohibited fields remain forbidden.

---

# 57. Diagnostic Export

Diagnostic export is a separate protected workflow.

---

## 57.1 Export Contents

An export may include selected:

- log segments;
- trace summaries;
- metric summaries;
- process inventory;
- version inventory;
- configuration summary;
- health;
- migration state;
- and crash metadata.

---

## 57.2 Export Exclusions

Default export excludes:

- source code;
- prompts;
- model responses;
- secrets;
- full environment;
- project files;
- database files;
- Vault data;
- raw memory dumps;
- and arbitrary command output.

---

## 57.3 Export Preview

Before creation, show:

- time range;
- processes;
- projects;
- data classes;
- estimated size;
- redactions;
- and destination.

---

## 57.4 Export Format

A diagnostic bundle should contain:

- manifest;
- format version;
- hashes;
- redaction report;
- files;
- and human-readable summary.

---

## 57.5 Export Destination

Creating a local bundle is distinct from uploading it.

External sharing requires:

- Network Gateway;
- policy;
- destination;
- and approval.

---

# 58. External Export

External observability export is optional.

The initial standard is OTLP.

---

## 58.1 Default

All external exporters are disabled by default.

No endpoint is inferred from the environment without explicit Opure configuration.

---

## 58.2 Enabling Export

Enabling export requires:

- project or profile policy;
- endpoint;
- authentication reference;
- signal selection;
- field policy;
- sampling;
- retention and retry policy;
- and preview.

---

## 58.3 Destination Classification

The destination must be classified as:

- local collector;
- organisation collector;
- approved cloud service;
- or custom external endpoint.

---

## 58.4 Export Data Policy

External export should use stricter field rules than local diagnostics.

Project identifiers should be omitted or pseudonymised unless explicitly approved.

---

## 58.5 No Silent Retry Disk

Exporter retry storage must be explicitly configured and Opure managed.

An exporter must not write sensitive retry payloads to an uncontrolled temporary directory.

---

## 58.6 Export Failure

Export failure must not block normal Opure operation.

It should:

- remain visible;
- use bounded queues;
- avoid unbounded disk;
- and drop according to explicit policy.

---

# 59. Product Analytics

Opure should not implement hidden usage analytics through the observability pipeline.

Examples of prohibited default analytics include:

- feature usage;
- project language popularity;
- workflow acceptance rate;
- model preference;
- time in application;
- or developer productivity scoring.

Any future opt-in product analytics require a separate Charter-aligned decision.

---

# 60. Performance

Observability must have defined overhead budgets.

---

## 60.1 Proposed Budget

Subject to prototype evidence, normal production observability should aim for:

- less than 2% CPU overhead during ordinary non-model work;
- bounded allocation overhead;
- less than 150 MiB combined additional working set across Desktop and Runtime for local diagnostics;
- and bounded disk write rate.

These are provisional targets.

---

## 60.2 Hot Paths

Hot paths include:

- token streaming;
- build output;
- file watching;
- project indexing;
- IPC messages;
- and metric collection.

Instrumentation must be sampled, batched or coalesced appropriately.

---

## 60.3 Disabled Cost

When Debug, Trace or a sampled Activity is disabled, code should avoid expensive payload construction.

---

# 61. Backpressure and Failure

## 61.1 Sink Failure

If local logging fails:

- increment in-memory failure counter;
- preserve bounded Error and Critical fallback;
- mark diagnostics degraded;
- avoid crashing the Runtime;
- and show the issue.

---

## 61.2 Disk Full

Under disk full:

- stop verbose capture;
- rotate and delete eligible old data;
- preserve bounded critical fallback where possible;
- and do not compromise authoritative stores for diagnostics.

---

## 61.3 Processor Failure

A failing exporter or processor must be isolated from application work.

Repeated failure may disable the component.

---

## 61.4 Redaction Failure

Fail closed for the unsafe field.

Do not drop a Critical event entirely if safe metadata can still be emitted.

---

# 62. Clock and Time

Every record should contain UTC time.

Where useful, include:

- event timestamp;
- observed timestamp;
- monotonic duration;
- and process start-relative time.

---

## 62.1 Clock Skew

Processes on one machine normally share the system clock, but ordering should also use:

- sequence numbers;
- trace causality;
- and process monotonic clocks.

Wall-clock timestamp alone does not prove order.

---

# 63. Sequence Numbers

Process-local log records should include a monotonically increasing sequence.

Streams should include sequence where gaps matter.

Sequence resets on process instance change.

---

# 64. Resource Identity

OpenTelemetry resource attributes should remain bounded.

Possible attributes:

- service name;
- service version;
- process class;
- process architecture;
- operating-system type;
- deployment environment;
- Opure profile class;
- and Runtime instance.

Do not include:

- user name;
- machine serial;
- raw project path;
- or secret configuration.

---

# 65. Environment Labels

Allowed environment labels may include:

- Development;
- Test;
- Preview;
- Production;
- Recovery;
- SafeMode.

They should not contain arbitrary user text.

---

# 66. Error Codes

Stable Opure error codes belong in:

- logs;
- traces;
- Trust Centre links;
- UI errors;
- and diagnostics.

The same error condition should use the same code across processes.

---

# 67. Exception Policy

Exceptions should be logged once at the boundary that handles or terminates the operation.

Repeated logging of the same exception at every layer should be avoided.

---

## 67.1 Expected Exceptions

Expected validation and policy denials should not be logged as Error merely because exceptions are used internally.

---

## 67.2 Unhandled Exceptions

Unhandled exceptions should produce:

- Critical diagnostic;
- crash metadata;
- process state;
- and recovery signal.

---

# 68. Data Retention Ownership

The Observability service owns ordinary diagnostics retention.

The Trust Centre owns Trust retention.

Workflow services own workflow history.

Build Manager owns build output retention.

No global cleanup job may delete another owner's authoritative data.

---

# 69. Local Storage Security

Diagnostic directories should use restrictive per-user permissions.

They should not be placed:

- in source repositories;
- in cloud-synchronised folders by default;
- or in shared machine-wide folders.

---

# 70. Encryption at Rest

Ordinary diagnostic logs are not application-encrypted by this ADR.

The UI must describe this honestly.

Sensitive diagnostic sessions may require encrypted bundles or future field encryption.

A separate data-at-rest ADR may add encryption.

---

# 71. Multi-Profile Isolation

Each Opure profile has separate:

- log roots;
- trace storage;
- metrics history;
- exporter configuration;
- quotas;
- and diagnostic sessions.

Test profiles must not write to production diagnostics.

---

# 72. Plugin and Worker Files

Plugin Hosts and workers should write only to Runtime-assigned diagnostic locations.

They must not choose arbitrary log paths.

---

# 73. External Library Logging

Third-party libraries may emit through `ILogger`.

Their categories and fields require:

- filtering;
- rate limits;
- and redaction.

---

## 73.1 Library Defaults

Noisy library categories should default to Warning or above unless needed.

---

## 73.2 HTTP Logging

HTTP body logging is prohibited.

HTTP header logging requires allowlists.

Authorisation, cookies and signing headers are prohibited.

---

## 73.3 Database Logging

SQL parameter logging is disabled in production.

---

# 74. Instrumentation Governance

Every service should maintain an instrumentation manifest containing:

- ActivitySource names;
- Meter names;
- log event ranges;
- metrics;
- span names;
- field classifications;
- sampling;
- and owner.

---

## 74.1 Review

New instrumentation should be reviewed for:

- usefulness;
- privacy;
- cardinality;
- performance;
- retention;
- and duplication.

---

# 75. Schema Versioning

Local diagnostic records should include a format version.

Changes should preserve:

- backward reading for supported retention period;
- export compatibility;
- and graceful unknown-field handling.

---

# 76. Semantic Conventions

OpenTelemetry semantic conventions should be used where they fit safely.

Opure-specific fields should use an `opure.` namespace.

Examples:

```text
opure.runtime.instance
opure.process.class
opure.project.scope
opure.operation.id
opure.workflow.stage
opure.patch.state
opure.risk.class
opure.data.class
```

Names are provisional and require a schema registry.

---

# 77. No Arbitrary Tags

Instrumentation must not accept arbitrary dictionaries from:

- plugins;
- providers;
- MCP servers;
- user input;
- or external commands

as telemetry attributes.

External data must be mapped to a bounded schema.

---

# 78. OpenTelemetry Collector

A local or organisation collector may be supported optionally.

Opure should not install or run a collector silently.

---

## 78.1 Collector Configuration

When enabled, show:

- executable or endpoint;
- ownership;
- signals;
- export destination;
- retry storage;
- and data policy.

---

# 79. Sampling Configuration

Sampling should be controlled by Opure configuration and project policy.

Environment variables should not silently override production policy unless explicitly supported and displayed.

---

# 80. Diagnostic Configuration

Configuration should support:

- minimum level by category;
- local storage quota;
- retention;
- trace sampling;
- metric interval;
- diagnostic session;
- and optional exporter.

Secret exporter credentials remain in the Secrets Vault.

---

# 81. Dynamic Configuration

Log levels and sampling may change at runtime.

Changes should be:

- bounded;
- visible;
- persisted as non-secret settings;
- and reset after diagnostic-session expiry where applicable.

---

# 82. Configuration Safety

A configuration requesting unsafe payload capture must be rejected.

No configuration can enable Secret or Prohibited fields.

---

# 83. Safe Mode

Safe Mode should:

- keep Error and Critical local logging;
- disable optional exporters;
- disable third-party instrumentation;
- reduce trace sampling;
- preserve recovery correlation;
- and provide diagnostic export.

---

# 84. Recovery Mode

Recovery Mode should use a minimal observability pipeline that does not depend on:

- optional databases;
- plugins;
- MCP;
- AI providers;
- or external collectors.

---

# 85. Startup Diagnostics

Before normal persistence is ready, the Runtime should maintain a bounded bootstrap diagnostic buffer.

After readiness, safe records may be transferred to the normal local store.

---

## 85.1 Bootstrap Failure

If normal diagnostics cannot start, bootstrap records should remain available through a safe fallback file.

---

# 86. Shutdown Diagnostics

Shutdown should:

- stop new diagnostics;
- drain bounded queue;
- flush Error and Critical;
- close segments;
- write process-end metadata;
- and report dropped records.

Shutdown must not hang indefinitely for diagnostics.

---

# 87. Crash Recovery for Log Segments

At startup, the observability service should:

- discover incomplete segments;
- validate complete records;
- ignore an incomplete final line;
- rebuild manifests;
- and preserve evidence.

---

# 88. Diagnostic Index

The Runtime may maintain a rebuildable local diagnostic index for:

- time;
- severity;
- event ID;
- process;
- service;
- error code;
- and correlation.

The raw segment remains the diagnostic source.

The index is rebuildable.

---

# 89. Search

Diagnostic search must operate over safe fields.

Searching raw payload content is not required because prohibited payloads should not exist.

---

# 90. Exporter Isolation

External exporters should run through bounded processors.

A failing exporter must not:

- block log emission;
- block a workflow;
- block Runtime shutdown indefinitely;
- or create unbounded retry files.

A dedicated exporter worker may be considered if a provider library is risky.

---

# 91. Trust Centre Integration

Significant observability state may create explicit Trust records, including:

- external exporter enabled;
- diagnostic bundle created;
- diagnostic bundle shared;
- sensitive diagnostic session started;
- crash dump created;
- repeated authentication attack detected;
- and logs deleted by explicit user action.

---

# 92. Deletion

Developers should be able to delete ordinary local diagnostics.

Deletion should show:

- scope;
- date range;
- size;
- pinned sessions;
- and exclusions.

Deleting diagnostics does not delete Trust Centre or workflow history.

---

# 93. Secure Deletion Limitation

Opure cannot guarantee forensic erasure from all SSDs, snapshots or backups.

It should claim logical deletion and best-effort local cleanup only.

---

# 94. Support Workflow

A future support workflow may:

1. start bounded diagnostic session;
2. reproduce issue;
3. stop session;
4. scan and redact;
5. preview bundle;
6. create local bundle;
7. optionally share through approved destination;
8. record Trust Centre action;
9. and delete after expiry.

---

# 95. Development Experience

Development builds may additionally use:

- console provider;
- debug provider;
- local OTLP collector;
- and richer traces.

Development defaults must not ship accidentally in production.

---

# 96. Test Observability

Tests should use:

- fake logger;
- in-memory span exporter;
- in-memory metric reader;
- deterministic time;
- and secret canaries.

Test telemetry should be isolated by test run.

---

# 97. Architecture Enforcement

Automated tests should detect:

- direct console writes in first-party services;
- direct file logging outside observability provider;
- arbitrary OTLP exporters;
- unapproved instrumentation packages;
- secret types passed to logs;
- raw prompt fields;
- raw source content fields;
- high-cardinality metric labels;
- Trust records derived from log scraping;
- and Desktop direct access to diagnostic directories.

---

# 98. Security Impact

## 98.1 Trust Boundaries

Observability crosses:

- process boundary;
- service boundary;
- plugin boundary;
- worker boundary;
- provider boundary;
- filesystem boundary;
- export boundary;
- and support boundary.

---

## 98.2 Threats

Relevant threats include:

- secret leakage;
- source-code leakage;
- prompt leakage;
- log injection;
- malicious structured fields;
- disk exhaustion;
- exporter exfiltration;
- unsafe crash dump;
- forged process identity;
- diagnostic tampering;
- high-cardinality denial of service;
- unbounded exception data;
- and local file disclosure.

---

## 98.3 Mitigations

- typed events;
- allowlists;
- redaction;
- bounded queues;
- quotas;
- process identity;
- restrictive permissions;
- exporter disabled by default;
- explicit diagnostic sessions;
- no payload logging;
- secret canary tests;
- and Trust Centre records for external sharing.

---

## 98.4 Log Injection

Newlines and control characters in user or external text must not alter record framing.

Structured serialisation must escape values correctly.

Rendered log viewers must treat content as text.

---

# 99. Privacy Impact

Observability may reveal:

- project existence;
- technology stack;
- operation timing;
- error patterns;
- file names;
- provider choices;
- and plugin use.

The architecture reduces privacy impact through:

- local storage;
- minimisation;
- pseudonymisation;
- retention;
- export consent;
- and no product analytics.

---

# 100. Reliability and Recovery

## 100.1 Diagnostic Failure Is Degraded

Loss of ordinary diagnostics should not normally stop project work.

The Runtime becomes diagnostically degraded.

---

## 100.2 Trust Failure May Block

Failure of required Trust Centre recording may block protected actions according to policy.

---

## 100.3 No False Success

Observability failure must not cause a failed operation to appear successful.

---

# 101. Performance Tests

Measure:

- disabled log call;
- enabled structured log;
- source-generated log;
- Activity not sampled;
- Activity sampled;
- metric recording;
- queue enqueue;
- file batch write;
- log rotation;
- redaction;
- diagnostics query;
- and export.

---

# 102. Reference Hardware

Tests should use:

- Windows 11;
- Ryzen 9 5950X;
- 32 GB RAM;
- RTX 5070 Ti 16 GB;
- local SSD.

Observability should remain subordinate to:

- UI responsiveness;
- local model inference;
- builds;
- tests;
- and patch integrity.

---

# 103. Scale Tests

Prototype at least:

- 100,000 log events per minute burst;
- sustained 10,000 log events per minute;
- 10,000 concurrent trace summaries in synthetic tests;
- 100,000 metric series rejection test;
- 100 Plugin Host streams;
- one million searchable diagnostic records;
- 1 GiB quota rotation;
- and disk-full simulation.

These are stress scenarios.

---

# 104. Fault-Injection Tests

Inject:

- log directory unavailable;
- permission denied;
- disk full;
- queue full;
- redactor exception;
- exporter hang;
- exporter crash;
- corrupt segment;
- incomplete final line;
- Runtime crash;
- Desktop crash;
- worker crash;
- clock adjustment;
- and malformed plugin event.

---

# 105. Prototype Plan

## 105.1 Prototype A — Structured Local Logging

Implement:

- `ILogger`;
- stable EventId;
- JSON Lines provider;
- bounded queue;
- rotation;
- retention;
- and Diagnostics query.

---

## 105.2 Prototype B — Cross-Process Trace

Implement:

- Desktop command;
- IPC trace context;
- Runtime span;
- worker span;
- provider simulation;
- and correlated logs.

---

## 105.3 Prototype C — Metrics

Implement:

- Runtime;
- IPC;
- scheduler;
- persistence;
- and dropped-log metrics.

---

## 105.4 Prototype D — Redaction

Inject:

- secret;
- path;
- URL;
- command;
- exception;
- prompt;
- plugin data;
- and provider response.

Verify approved output.

---

## 105.5 Prototype E — Backpressure

Overload:

- local queue;
- Desktop stream;
- and exporter.

Verify bounded memory and dropped-record accounting.

---

## 105.6 Prototype F — Diagnostic Export

Implement:

- time selection;
- data inventory;
- redaction scan;
- preview;
- manifest;
- hashes;
- and local bundle.

---

## 105.7 Prototype G — Trust Separation

Implement one protected action with:

- diagnostic logs;
- trace;
- metrics;
- and explicit Trust record.

Prove deleting logs does not delete Trust history.

---

# 106. Implementation Plan

## 106.1 Initial Tasks

1. Record founder review.
2. Pin .NET and OpenTelemetry package versions.
3. Define instrumentation registry.
4. Define EventId allocation.
5. Define field-classification model.
6. Define redaction API.
7. Implement local JSON Lines provider.
8. Implement bounded queue and emergency sink.
9. Implement trace propagation through IPC.
10. Implement initial Meters.
11. Implement health model.
12. Implement diagnostic index.
13. Implement retention and quota.
14. Implement diagnostic session.
15. Implement export preview and bundle.
16. Implement optional OTLP configuration.
17. Implement Trust Centre links.
18. Run secret canary tests.
19. Benchmark.
20. Complete security and privacy review.
21. Accept, amend or reject the ADR.

---

## 106.2 Owners

| Area | Owner |
|---|---|
| Product decision | Founder |
| Observability architecture | Observability Architecture Owner |
| Runtime instrumentation | Runtime Owner |
| Desktop diagnostics | Desktop Owner |
| Redaction | Security and Privacy Owners |
| Trust boundary | Trust Centre Owner |
| Local storage | Observability and Persistence Owners |
| Export | Network and Security Owners |
| Performance | Performance Owner |
| Recovery | Recovery Owner |
| Testing | Test Architecture Owner |

---

# 107. Suggested Repository Structure

```text
src/
├── Opure.Observability.Abstractions/
├── Opure.Observability.Logging/
├── Opure.Observability.Tracing/
├── Opure.Observability.Metrics/
├── Opure.Observability.Health/
├── Opure.Observability.Redaction/
├── Opure.Observability.LocalStore/
├── Opure.Observability.Export/
├── Opure.Observability.Diagnostics/
└── Opure.Observability.OpenTelemetry/
```

Test projects may include:

```text
tests/
├── Opure.Observability.UnitTests/
├── Opure.Observability.SecurityTests/
├── Opure.Observability.PerformanceTests/
└── Opure.Observability.EndToEndTests/
```

---

# 108. Suggested Record Envelope

A conceptual local log envelope:

```text
format_version
timestamp_utc
observed_timestamp_utc
sequence
severity
event_id
event_name
category
message_template
process_class
process_instance
runtime_instance
service
trace_id
span_id
correlation_id
operation_id
error_code
attributes
redaction_summary
```

The final schema requires implementation review.

---

# 109. Naming Standards

Use lower-case dotted telemetry names.

Examples:

```text
opure.runtime.start.duration
opure.ipc.request.duration
opure.workflow.active
opure.persistence.write.queue
opure.diagnostics.records.dropped
```

Units must be explicit.

---

# 110. Metric Units

Use standard units such as:

- seconds;
- bytes;
- items;
- requests;
- tokens;
- processes;
- and percentage as ratio where conventions require.

Do not encode unit into a value ambiguously.

---

# 111. Dashboard Principles

Local dashboards should prioritise:

- current health;
- actionable degradation;
- resource pressure;
- recent failures;
- and correlation to developer work.

They should not become vanity dashboards.

---

# 112. Alerting

The initial local product may use in-application alerts for:

- disk pressure;
- repeated crashes;
- exporter failure;
- dropped diagnostics;
- Trust Centre unavailability;
- and recovery required.

Remote alerting is deferred.

---

# 113. Alert Fatigue

Repeated identical alerts should be grouped.

Grouping must not hide escalation or distinct project impact.

---

# 114. Data Export Compatibility

Local diagnostics should map cleanly to OpenTelemetry concepts where practical.

Opure-specific data remains under namespaced attributes.

---

# 115. Package and Supply-Chain Policy

Observability packages must be:

- centrally pinned;
- licence reviewed;
- vulnerability reviewed;
- transitively inspected;
- and updated deliberately.

Exporter packages should not be included unless the feature is supported and tested.

---

# 116. Release Requirements

A release must not ship if:

- secret canary appears in diagnostics;
- external export is enabled by default;
- diagnostic queue is unbounded;
- disk quota is absent;
- Trust Centre depends on log scraping;
- crash artefacts upload automatically;
- high-cardinality metric tests fail;
- or production reflection and debug exporters remain enabled.

---

# 117. Acceptance Criteria

This ADR may move to **Accepted** when:

- [ ] First-party code uses `ILogger`.
- [ ] First-party traces use `ActivitySource`.
- [ ] First-party metrics use `Meter`.
- [ ] Upstream OpenTelemetry packages are pinned.
- [ ] Application libraries do not initialise exporters.
- [ ] Local diagnostics work without OpenTelemetry export.
- [ ] External export is disabled by default.
- [ ] No production TCP telemetry endpoint is opened automatically.
- [ ] Stable EventIds are governed.
- [ ] Structured templates are used.
- [ ] Source-generated logging is used for selected hot paths.
- [ ] Cross-process trace context propagates through IPC.
- [ ] Correlation survives Desktop, Runtime and worker boundaries.
- [ ] Metric names and units are governed.
- [ ] Metric cardinality tests pass.
- [ ] Local log queue is bounded.
- [ ] Queue overflow is recorded.
- [ ] Log segments rotate.
- [ ] Retention and quota work.
- [ ] Disk-full behaviour is safe.
- [ ] Incomplete log segment recovery works.
- [ ] Field classification is implemented.
- [ ] Allowlist redaction is implemented.
- [ ] Secret canary tests pass.
- [ ] Source code and prompts are absent by default.
- [ ] Command output is not duplicated into ordinary logs.
- [ ] Plugin logs are rate limited and validated.
- [ ] Crash upload is disabled.
- [ ] Memory dump creation is explicit.
- [ ] Diagnostic export has preview and manifest.
- [ ] External sharing passes through policy and Network Gateway.
- [ ] Trust Centre records are explicit and separate.
- [ ] Deleting logs does not delete Trust records.
- [ ] Safe Mode diagnostics work.
- [ ] Recovery Mode diagnostics work.
- [ ] Performance overhead is measured.
- [ ] Security and privacy reviews are complete.
- [ ] Founder approval is recorded.

---

# 118. Evidence Required Before Acceptance

- [ ] package and licence inventory;
- [ ] structured-log prototype;
- [ ] cross-process trace report;
- [ ] metric registry;
- [ ] cardinality test;
- [ ] queue-overload test;
- [ ] disk-full test;
- [ ] retention test;
- [ ] segment-recovery test;
- [ ] secret canary report;
- [ ] plugin-log abuse test;
- [ ] crash-diagnostic test;
- [ ] diagnostic-export test;
- [ ] optional OTLP export test;
- [ ] Trust Centre separation test;
- [ ] performance benchmark;
- [ ] security review;
- [ ] privacy review;
- [ ] founder approval.

---

# 119. Known Limitations

- Exact .NET and OpenTelemetry versions are not yet pinned.
- The local log provider is not yet implemented.
- Exact retention defaults are provisional.
- Exact sampling rates are provisional.
- The local trace-store format is not final.
- The local metric-store format is not final.
- Crash-dump policy is not final.
- Database-wide diagnostic encryption is not selected.
- Remote support upload is deferred.
- Enterprise collector discovery is deferred.
- Tail-based sampling is limited initially.
- Same-user malicious code may read local unencrypted diagnostics.
- External library instrumentation may require service-specific restrictions.
- Product analytics remain intentionally absent.
- Secure deletion cannot be guaranteed on all storage hardware.

---

# 120. Open Questions

- Which .NET LTS release should be pinned?
- Which OpenTelemetry .NET version should be pinned?
- Should the local file provider be custom or use a reviewed third-party sink behind `ILogger`?
- Should local log records use JSON Lines or a compact binary format?
- What segment size should be used?
- What total quota should ship?
- What default retention should ship?
- Which successful traces should be sampled?
- How should failed traces be retained without a collector?
- Should local trace summaries use SQLite or content segments?
- Which metric aggregation intervals should be retained?
- Which framework automatic instrumentations are safe?
- Should HttpClient trace propagation be disabled for some external providers?
- Which project identifier, if any, may appear in local diagnostics?
- How should sensitive relative paths be revealed in the UI?
- Which exception fields are safe?
- What crash metadata is sufficient without memory dumps?
- Should ETW or EventSource be enabled as an optional Windows diagnostic provider?
- How should an organisation policy configure OTLP export?
- How should exporter retry storage be protected?
- Should an exporter run in a worker process?
- Which data-at-rest protections are needed for Sensitive diagnostic sessions?
- How should support bundles be encrypted?
- What is the minimum useful diagnostic state when SQLite is unavailable?

---

# 121. Deferred Decisions

This ADR intentionally defers:

- Trust Centre tamper evidence to an audit-integrity ADR;
- data-at-rest encryption to a security ADR;
- crash-dump implementation to a crash-diagnostics ADR;
- remote support upload to a support-service ADR;
- enterprise collector management to a future enterprise ADR;
- product analytics to a separate founder decision;
- exact dashboard design to Desktop implementation;
- and external alerting to post-1.0 planning.

---

# 122. Alternatives Rejected

## 122.1 Direct Serilog APIs Throughout the Codebase

Rejected because Opure should keep application instrumentation on standard .NET abstractions and preserve sink replacement.

---

## 122.2 OpenTelemetry SDK APIs as the Only Application API

Rejected because .NET's native logging, tracing and metrics APIs provide the appropriate instrumentation surface.

---

## 122.3 Custom Observability Framework

Rejected because it would duplicate mature standards and increase security, maintenance and interoperability cost.

---

## 122.4 Windows Event Log as the Primary Store

Rejected because it is Windows specific and does not provide the complete cross-platform local logs, traces and metrics model.

---

## 122.5 Mandatory Collector or Cloud APM

Rejected because Opure's core diagnostics must work locally without an external service.

---

## 122.6 Trust Centre as the Log Store

Rejected because developer accountability records and technical diagnostics have different semantics, retention and volume.

---

## 122.7 Plain Text Logs Only

Rejected because they provide insufficient structure, correlation, redaction and performance analysis.

---

# 123. Official Evidence References

The following official sources informed this ADR:

- [Logging in C# and .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/overview)
- [Logging guidance for .NET library authors](https://learn.microsoft.com/en-gb/dotnet/core/extensions/logging/library-guidance)
- [.NET observability with OpenTelemetry](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-with-otel)
- [.NET distributed tracing](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing)
- [Adding distributed tracing instrumentation](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/dotnet/)
- [OpenTelemetry .NET instrumentation](https://opentelemetry.io/docs/languages/dotnet/instrumentation/)
- [OpenTelemetry .NET logs](https://opentelemetry.io/docs/languages/dotnet/logs/)
- [OpenTelemetry .NET metrics](https://opentelemetry.io/docs/languages/dotnet/metrics/)
- [OpenTelemetry .NET traces](https://opentelemetry.io/docs/languages/dotnet/traces/)
- [OpenTelemetry Logs Data Model](https://opentelemetry.io/docs/specs/otel/logs/data-model/)
- [OpenTelemetry .NET repository](https://github.com/open-telemetry/opentelemetry-dotnet)

Versions, stability and semantic conventions can change.

The implementation team must verify the pinned versions before acceptance.

---

# 124. Review Record

| Date | Reviewer | Decision | Notes |
|---|---|---|---|
| 18 July 2026 | Architecture draft | Proposed | Standard .NET instrumentation with local-first OpenTelemetry-compatible collection recommended |

---

# 125. Approval

## Founder or Product Approval

- **Name:** Christopher Dyer
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Pending founder review

## Architecture Approval

- **Name or role:** Observability Architecture Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Local storage, correlation and export prototypes required

## Security Approval

- **Name or role:** Security Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Secret canary, export, plugin and crash review required

## Privacy Approval

- **Name or role:** Privacy Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Field inventory, retention and external-export review required

## Performance Approval

- **Name or role:** Performance Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Hot-path, queue, storage and trace overhead measurements required

---

# 126. Supersession

This ADR is superseded only when a later ADR:

- names ADR-0006 explicitly;
- explains why the instrumentation or storage model changed;
- identifies affected signals and processes;
- describes schema and retention migration;
- explains export and privacy impact;
- and updates the `Superseded by` field.

Historical ADRs remain in version control.

---

# 127. Change History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | 18 July 2026 | Founder Draft | Initial local-first .NET and OpenTelemetry observability recommendation |

---

# 128. Final Decision Statement

> **Opure will provisionally use Microsoft.Extensions.Logging for structured diagnostic logs, ActivitySource for traces, Meter for metrics and the upstream OpenTelemetry .NET SDK for collection and optional standards-based export, with bounded local storage, strict redaction, no external transmission by default and a permanent architectural separation between technical diagnostics and the developer-facing Trust Centre.**

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**