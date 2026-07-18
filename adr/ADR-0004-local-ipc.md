# ADR-0004 — Local Inter-Process Communication

## Architecture Decision Record

**Status:** Proposed  
**Date:** 18 July 2026  
**Decision owners:** Founder and Product Owner  
**Technical owners:** Runtime Architecture Owner  
**Reviewers:** Security Owner, Desktop Owner, Plugin Owner, Performance Owner, Recovery Owner  
**Supersedes:** None  
**Superseded by:** None  
**Related ADRs:** ADR-0001 Primary Implementation Language, ADR-0002 Desktop Framework, ADR-0003 Runtime Process Topology, ADR-0005 Persistence  
**Related specifications:** CHARTER-001, SPEC-001, SPEC-002, SPEC-003, SPEC-004, SPEC-006, SPEC-007, SPEC-008, SPEC-009, SPEC-010, SPEC-011, SPEC-012  
**Target milestone:** Phase 0 — Founding Baseline and Phase 1 — Architecture Skeleton  

---

## 1. Decision Summary

Opure should use **gRPC over Windows named pipes** as the primary local inter-process communication transport for the Windows-first product.

The first implementation should use:

- Kestrel's named-pipe transport;
- HTTP/2 semantics through gRPC;
- explicit Protocol Buffers service contracts;
- Windows pipe access-control lists restricted to the intended user and authorised process class;
- application-level mutual session authentication;
- unique Runtime instance and endpoint identities;
- version and capability negotiation;
- unary requests for commands and queries;
- server-streaming or bidirectional streaming for subscriptions and long-running operations;
- explicit deadlines and cancellation;
- bounded message sizes;
- flow control and backpressure;
- and file-backed or shared bounded transfer mechanisms for very large payloads.

The named-pipe endpoint name is not a credential.

Opure must not rely on:

- localhost alone;
- a predictable pipe name;
- process name alone;
- or operating-system user identity alone

as the complete application authentication model.

Loopback TCP must be disabled by default for production local communication.

The transport architecture must permit the same gRPC contracts to use **Unix-domain sockets** on future Linux and macOS implementations.

---

## 2. Status

Current status:

> **Proposed**

This ADR becomes **Accepted** only after prototypes prove:

- Desktop-to-Runtime communication;
- Runtime-to-worker communication;
- Runtime-to-Plugin-Host communication;
- named-pipe ACL enforcement;
- session authentication;
- version negotiation;
- streaming;
- cancellation;
- backpressure;
- Runtime restart and client reconnection;
- stale-session rejection;
- malicious-client rejection;
- message-size enforcement;
- and acceptable latency and throughput.

---

## 3. Context

ADR-0003 defines a hybrid supervised process topology with:

- a separate Desktop process;
- one trusted Runtime process;
- trusted worker processes;
- isolated Plugin Host processes;
- external AI providers;
- external MCP servers;
- and controlled command processes.

These processes require local communication.

The communication system must support:

- commands;
- queries;
- events;
- projections;
- health;
- process supervision;
- streaming model output;
- streaming build output;
- workflow updates;
- patch progress;
- cancellation;
- approvals;
- recovery;
- and diagnostics.

The platform requires explicit service contracts and must remain:

- local-first;
- secure;
- provider neutral;
- testable;
- observable;
- recoverable;
- and portable.

The communication system must not accidentally turn the Desktop Gateway into a public network service.

It must not trust any process merely because it runs on the same machine.

It must support future Linux and macOS clients without forcing the trusted domain contracts to change.

---

## 4. Problem Statement

Opure requires a fast, authenticated, versioned and observable local IPC system that supports request-response and streaming communication between the Desktop, Runtime and supervised child processes on Windows 11 while preserving a practical path to Unix-domain sockets on future platforms.

---

## 5. Decision Drivers

The IPC decision is evaluated against:

- alignment with the Opure Charter;
- local-first operation;
- Windows 11 support;
- C# and .NET support;
- Desktop-to-Runtime communication;
- worker supervision;
- plugin isolation;
- request-response calls;
- server streaming;
- bidirectional streaming;
- cancellation;
- deadlines;
- backpressure;
- authentication;
- access control;
- endpoint discovery;
- version negotiation;
- capability negotiation;
- structured errors;
- observability;
- performance;
- memory use;
- message-size control;
- large-payload strategy;
- testability;
- fault injection;
- reconnection;
- recovery;
- cross-platform portability;
- generated clients;
- language-neutral contracts;
- implementation complexity;
- supply-chain risk;
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
- **Open by Architecture**
- **Loose Coupling**
- **Least Privilege**
- **Project Isolation**
- **Performance Respect**
- **No Hidden Authority**
- **No Silent Network Exposure**
- **Honest State**
- **Recoverability**

Relevant architecture requirements include:

- Localhost is not automatically trusted.
- The Desktop communicates primarily through the Desktop Gateway.
- Services communicate through explicit versioned contracts.
- Commands, queries and events remain distinct.
- Third-party Plugin Hosts receive only scoped capabilities.
- Workers do not own authoritative domain state.
- Secret values do not enter ordinary logs or Trust Centre records.
- Cancellation does not imply completion until confirmed.
- Runtime restart requires state reconciliation.
- Cross-platform contracts must not depend on Windows-specific wire types.
- Protected remote traffic remains separate from local IPC.

---

## 7. Scope

This ADR decides local IPC for:

- Desktop to Runtime;
- Runtime to trusted workers;
- Runtime to Plugin Hosts;
- Runtime to Opure-launched local MCP servers where the MCP transport permits;
- Runtime to helper processes;
- future CLI to Runtime;
- local health and supervision channels;
- local command and query channels;
- event and projection streaming;
- and local capability invocation.

This ADR does not decide:

- communication with remote cloud providers;
- ordinary HTTP communication with Ollama;
- the MCP protocol itself;
- build-process standard input and output;
- Git command execution;
- storage schemas;
- desktop framework;
- public remote APIs;
- team collaboration;
- remote Runtime access;
- network tunnelling;
- distributed workers;
- or web-client hosting.

---

## 8. Constraints

Known constraints include:

- Windows 11 is the first supported platform.
- C#/.NET is the proposed primary implementation stack.
- The Desktop and Runtime are separate processes.
- Third-party Plugin Hosts are isolated.
- The first implementation must be practical for a small team.
- Local communication must work offline.
- The IPC endpoint must not listen on a network interface by default.
- The Desktop must reconnect after Runtime restart.
- The Runtime must reject stale clients.
- Long streams must not consume unbounded memory.
- Large files must not be copied repeatedly through ordinary messages.
- Secret values must not appear in endpoint descriptors.
- Endpoint names may be observable.
- A malicious process running as the same operating-system user cannot be treated as fully preventable by local IPC alone.
- Future Linux and macOS support should use equivalent contracts.
- Development tooling may require an explicitly enabled diagnostic transport.
- Production and test endpoints must remain isolated.

---

## 9. Assumptions

This decision assumes:

- Most first-party clients use generated C# gRPC clients.
- Plugin Hosts may also use generated clients from language-neutral contracts.
- Protocol Buffers are acceptable for service contracts.
- Named pipes are available on supported Windows versions.
- Kestrel named-pipe support is available on the selected .NET LTS release.
- The operating system can restrict pipe access through ACLs.
- Desktop and Runtime usually run under the same user account.
- Administrator-level or same-user malware can exceed the protection offered by application IPC.
- Runtime-owned durable state remains authoritative after reconnection.
- Large payloads can be represented by scoped content references rather than copied into one message.
- The Runtime may expose multiple logical services on one physical endpoint.
- Worker and Plugin Host endpoints may be unique per process instance.
- gRPC generated contracts can remain stable across transport changes.
- Public remote access is not an initial requirement.

---

## 10. Official Platform Evidence

Official Microsoft documentation available on 18 July 2026 establishes that:

- .NET supports local IPC using gRPC;
- ASP.NET Core and Kestrel support gRPC over named pipes on Windows;
- built-in Kestrel named-pipe support requires .NET 8 or later;
- named-pipe access can be controlled with `PipeSecurity`;
- named pipes support asynchronous streams in .NET;
- and .NET supports Unix-domain sockets where the operating system provides them.

These capabilities make gRPC over named pipes a viable first-party Windows IPC foundation while preserving a route to Unix-domain sockets.

Framework and platform support must be rechecked before this ADR moves to Accepted.

---

## 11. Options Considered

The principal options are:

1. **Option A — gRPC over Windows Named Pipes**
2. **Option B — Custom Framed Protocol over Named Pipes**
3. **Option C — gRPC over Loopback TCP**
4. **Option D — Local HTTP or WebSocket over Loopback TCP**
5. **Option E — Windows AppService, COM or RPC**
6. **Option F — Memory-Mapped Files with Signalling**
7. **Option G — Message Broker or Embedded Bus**
8. **Option H — Standard Input and Output**

---

# 12. Option A — gRPC over Windows Named Pipes

## 12.1 Description

Use gRPC services over Kestrel's Windows named-pipe transport.

Use Protocol Buffers for explicit contracts and generated clients.

Use:

- unary RPC;
- server streaming;
- client streaming where justified;
- bidirectional streaming where justified;
- deadlines;
- cancellation;
- metadata;
- interceptors;
- and status translation.

---

## 12.2 Advantages

- Official .NET support.
- Strong C# tooling.
- Language-neutral contracts.
- Generated clients and servers.
- Request-response support.
- Streaming support.
- Cancellation support.
- Deadline support.
- HTTP/2 multiplexing.
- Structured status model.
- Mature test tooling.
- Kestrel integration.
- Named-pipe ACL support.
- No production TCP listener.
- Same contracts can use Unix-domain sockets later.
- Good fit for Desktop Gateway.
- Good fit for worker and Plugin Host protocols.
- Avoids writing a custom framing protocol.
- Supports interceptors for authentication, correlation and policy.
- Supports service reflection only when explicitly enabled.
- Good observability options.
- Strong schema evolution discipline.
- Supports multiple logical services on one physical endpoint.
- Efficient enough for control-plane and streaming workloads.
- Makes service boundaries explicit.

---

## 12.3 Disadvantages

- Introduces gRPC and Protocol Buffers as foundational dependencies.
- Requires HTTP/2 semantics even though the transport is local.
- Kestrel adds hosting complexity.
- Default gRPC message-size limits require deliberate configuration.
- Streaming misuse can still cause memory pressure.
- Binary Protocol Buffer payloads are less directly readable than JSON.
- Browser clients cannot use named pipes.
- Authentication still requires an application design.
- Named-pipe transport does not make all same-user processes trustworthy.
- Large file transfer is inappropriate through ordinary unary messages.
- Some debugging tools assume TCP endpoints.
- Error translation must not leak framework-specific status into domain contracts.
- Kestrel and gRPC version upgrades require compatibility tests.

---

## 12.4 Risks

- The team may treat gRPC services as domain ownership rather than transport facades.
- Public reflection could expose service metadata.
- Large payloads may be sent through gRPC without bounds.
- Stream consumers may fail to apply backpressure.
- Deadlines may not propagate to external tasks.
- Pipe ACLs may be configured too broadly.
- Session credentials may be written to logs.
- Endpoint names may become predictable and reused.
- A development TCP endpoint may be left enabled in production.
- Contract evolution may become careless because code generation hides wire impact.
- Framework status codes may replace stable Opure error codes.
- Plugin contracts may receive excessive service surface.
- One multiplexed connection may become a bottleneck if high-volume streams are uncontrolled.

---

## 12.5 Mitigations

- Explicit message-size budgets.
- Content-reference mechanism for large payloads.
- Per-call deadlines.
- Cancellation propagation.
- Stream coalescing.
- Bounded channels.
- Pipe ACL tests.
- Application authentication interceptors.
- Random Runtime endpoint identity.
- Session expiry.
- No production TCP listener.
- Reflection disabled by default.
- Contract review.
- Compatibility tests.
- Stable Opure error envelope.
- Separate service definitions by capability.
- Rate limits.
- connection and stream limits.
- and Trust Centre visibility for significant capability calls.

---

## 12.6 Estimated Adoption Cost

- **Initial implementation:** Moderate
- **Operational complexity:** Moderate
- **Migration difficulty:** Low to Moderate
- **Replacement difficulty:** Moderate

---

# 13. Option B — Custom Framed Protocol over Named Pipes

## 13.1 Description

Use `NamedPipeServerStream` and `NamedPipeClientStream` directly.

Define custom:

- framing;
- message types;
- multiplexing;
- streaming;
- cancellation;
- authentication;
- versioning;
- and error handling.

---

## 13.2 Advantages

- Full control.
- Minimal dependencies.
- Potentially low overhead.
- Simple wire format can be highly efficient.
- Direct access to pipe APIs.
- Exact security configuration.
- No HTTP/2 layer.
- Can be tailored to Opure's needs.

---

## 13.3 Disadvantages

- Opure must design and maintain framing.
- Multiplexing is complex.
- Streaming is complex.
- Flow control is complex.
- Backpressure is complex.
- Cancellation is custom.
- Deadlines are custom.
- Generated clients require additional work.
- Language-neutral tooling requires additional work.
- Compatibility testing burden is high.
- Security review burden is high.
- Parser defects become trusted-core defects.
- Diagnostic tooling is limited.
- Reinvents established infrastructure.
- More difficult to support future transports consistently.
- Error and metadata conventions must be invented.
- Reconnection semantics require custom design.

---

## 13.4 Risks

- Protocol parser vulnerabilities.
- framing desynchronisation;
- memory exhaustion;
- ambiguous partial messages;
- cancellation races;
- version drift;
- and difficult interoperability.

---

## 13.5 Estimated Adoption Cost

- **Initial implementation:** High
- **Operational complexity:** High
- **Migration difficulty:** High
- **Replacement difficulty:** High

---

# 14. Option C — gRPC over Loopback TCP

## 14.1 Description

Host gRPC on `127.0.0.1` or `::1`.

Use TLS or application authentication.

---

## 14.2 Advantages

- Standard gRPC hosting.
- Strong tooling.
- Easy diagnostics.
- Cross-platform.
- Easy remote extension later.
- No Windows-specific transport implementation.
- Supports existing network observability tooling.
- Works with many languages.

---

## 14.3 Disadvantages

- Opens a network socket.
- Requires port discovery.
- Port conflicts are possible.
- Local firewall interactions occur.
- Exposure mistakes are more consequential.
- Binding configuration must be exact.
- Loopback is not an identity boundary.
- Remote access may be enabled accidentally.
- TLS certificate management may be required.
- Endpoint scanning is easier.
- Less aligned with a closed local desktop product.
- Security review must cover network stack exposure.

---

## 14.4 Risks

- Accidental bind to all interfaces.
- insecure development configuration shipping;
- local port hijacking;
- stale endpoint descriptor;
- and remote-access creep.

---

## 14.5 Estimated Adoption Cost

- **Initial implementation:** Moderate
- **Operational complexity:** Moderate
- **Migration difficulty:** Low
- **Replacement difficulty:** Low to Moderate

---

# 15. Option D — Local HTTP or WebSocket over Loopback TCP

## 15.1 Description

Expose a JSON HTTP API and WebSocket or Server-Sent Events stream on loopback.

---

## 15.2 Advantages

- Easy debugging.
- Broad client support.
- Human-readable JSON.
- Web-client compatible.
- Familiar tooling.
- Easy TypeScript client generation.
- Straightforward request-response.
- WebSocket supports streaming.

---

## 15.3 Disadvantages

- Opens a local network endpoint.
- Requires custom event and cancellation semantics.
- JSON has greater payload overhead.
- Schema discipline is weaker without strict tooling.
- Multiple communication patterns must be combined.
- Backpressure needs careful design.
- Port discovery and collision.
- Accidental remote binding risk.
- Browser-origin and CORS complexity.
- Greater attack surface.
- Less direct C# contract generation than Protocol Buffers.
- Harder to model strongly typed bidirectional streams.
- Easy to expose diagnostic endpoints accidentally.

---

## 15.4 Estimated Adoption Cost

- **Initial implementation:** Moderate
- **Operational complexity:** Moderate
- **Migration difficulty:** Moderate
- **Replacement difficulty:** Moderate

---

# 16. Option E — Windows AppService, COM or RPC

## 16.1 Description

Use Windows-specific application-service, COM, WCF named-pipe or Windows RPC technology.

---

## 16.2 Advantages

- Deep Windows integration.
- Mature Windows security models.
- Native identity features.
- Some technologies provide strong local RPC.
- Potentially efficient.
- May integrate with packaged-app lifecycle.

---

## 16.3 Disadvantages

- Windows-specific contracts or hosting.
- Future Linux and macOS path is weaker.
- Some technologies are legacy or specialised.
- Tooling and deployment may be complex.
- Dynamic plugin-language support may be harder.
- Modern streaming patterns may be awkward.
- Developer familiarity may be lower.
- Packaging model may constrain application design.
- Can increase platform lock-in.
- May conflict with a simple .NET service-contract strategy.

---

## 16.4 Estimated Adoption Cost

- **Initial implementation:** Moderate to High
- **Operational complexity:** High
- **Migration difficulty:** High
- **Replacement difficulty:** High

---

# 17. Option F — Memory-Mapped Files with Signalling

## 17.1 Description

Use memory-mapped files for shared data and named events, semaphores or pipes for signalling.

---

## 17.2 Advantages

- Very high bulk-data throughput.
- Avoids repeated copies for large data.
- Low latency.
- Useful for shared read-only buffers.
- May suit large model or index data.

---

## 17.3 Disadvantages

- Poor fit for general command and event RPC.
- Synchronisation is complex.
- Lifetime management is complex.
- Access control is complex.
- Crash recovery is complex.
- Schema evolution is difficult.
- Corruption risk is higher.
- Pointer and offset validation is security sensitive.
- Cross-platform behaviour differs.
- Debugging is difficult.
- Not necessary for most control-plane messages.

---

## 17.4 Decision Relevance

Memory mapping may be used later for a narrowly bounded bulk-transfer mechanism.

It is not selected as the general IPC system.

---

## 17.5 Estimated Adoption Cost

- **Initial implementation:** High
- **Operational complexity:** High
- **Migration difficulty:** High
- **Replacement difficulty:** High

---

# 18. Option G — Message Broker or Embedded Bus

## 18.1 Description

Use a local message broker, embedded queue, ZeroMQ-style library or broker daemon.

---

## 18.2 Advantages

- Decoupled messaging.
- Queueing.
- Publish-subscribe.
- Potential durability.
- Multiple consumers.
- Language-neutral clients.
- Supports future distributed topology.

---

## 18.3 Disadvantages

- Additional dependency or daemon.
- Another operational subsystem.
- Authentication and ACL configuration.
- More complex request-response.
- More difficult cancellation.
- More difficult direct streaming.
- Distributed-system semantics before they are needed.
- Message ordering and delivery guarantees require careful design.
- Can obscure ownership.
- Recovery may duplicate domain durability.
- Increases packaging and update complexity.

---

## 18.4 Estimated Adoption Cost

- **Initial implementation:** High
- **Operational complexity:** High
- **Migration difficulty:** Moderate
- **Replacement difficulty:** Moderate

---

# 19. Option H — Standard Input and Output

## 19.1 Description

Use standard input, standard output and standard error for all child-process communication.

---

## 19.2 Advantages

- Simple process launch.
- Easy for many languages.
- No endpoint discovery.
- Parent-child binding.
- Good for one task per process.
- Useful for small workers.
- Easy to terminate with the process.

---

## 19.3 Disadvantages

- Poor fit for Desktop-to-Runtime.
- Difficult multiplexing.
- Difficult bidirectional streaming with errors and logs.
- Standard error conflicts with protocol diagnostics.
- Framing must be custom.
- Reconnection is impossible.
- Multiple clients are impossible.
- Long-lived services are awkward.
- Backpressure and partial writes require care.
- Protocol corruption can occur if libraries write to standard output.

---

## 19.4 Decision Relevance

Standard input and output may be used for narrowly bounded one-shot tools or bootstrap handshakes.

It is not selected as the primary IPC system.

---

## 19.5 Estimated Adoption Cost

- **Initial implementation:** Low for simple workers
- **Operational complexity:** High for general IPC
- **Migration difficulty:** High
- **Replacement difficulty:** High

---

# 20. Comparison Matrix

Scores:

- **1** — poor
- **2** — weak
- **3** — acceptable
- **4** — strong
- **5** — excellent

| Criterion | Weight | gRPC + Pipes | Custom Pipes | gRPC + TCP | HTTP/WS | Windows RPC | Shared Memory | Broker | StdIO |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Charter alignment | 5 | 5 | 4 | 3 | 3 | 3 | 3 | 3 | 3 |
| Windows-first fit | 5 | 5 | 5 | 4 | 4 | 5 | 5 | 4 | 4 |
| No network listener | 5 | 5 | 5 | 1 | 1 | 5 | 5 | 3 | 5 |
| Security controls | 5 | 5 | 4 | 4 | 3 | 5 | 3 | 4 | 3 |
| Streaming | 5 | 5 | 3 | 5 | 4 | 3 | 2 | 4 | 3 |
| Cancellation | 5 | 5 | 3 | 5 | 3 | 3 | 2 | 3 | 3 |
| Backpressure | 5 | 5 | 2 | 5 | 3 | 3 | 3 | 4 | 3 |
| Contract generation | 4 | 5 | 2 | 5 | 4 | 3 | 1 | 3 | 2 |
| Language neutrality | 4 | 5 | 3 | 5 | 5 | 2 | 3 | 5 | 4 |
| Cross-platform path | 4 | 5 | 3 | 5 | 5 | 1 | 3 | 5 | 5 |
| Testability | 4 | 5 | 3 | 5 | 5 | 3 | 2 | 3 | 3 |
| Observability | 4 | 5 | 3 | 5 | 5 | 3 | 2 | 4 | 3 |
| Performance | 4 | 4 | 5 | 4 | 3 | 4 | 5 | 4 | 4 |
| Large-payload fit | 3 | 3 | 3 | 3 | 2 | 3 | 5 | 3 | 2 |
| Small-team delivery | 5 | 5 | 2 | 4 | 4 | 2 | 1 | 2 | 3 |
| Maintenance | 5 | 5 | 2 | 4 | 4 | 3 | 2 | 3 | 2 |
| Supply-chain control | 3 | 4 | 5 | 4 | 4 | 4 | 5 | 3 | 5 |
| Replacement cost | 3 | 4 | 2 | 5 | 4 | 2 | 2 | 3 | 2 |
| **Indicative weighted total** |  | **436** | **296** | **379** | **343** | **303** | **275** | **326** | **296** |

gRPC over named pipes provides the strongest overall fit.

---

# 21. Decision

Opure will provisionally use:

> **gRPC over Windows named pipes with Protocol Buffer contracts, Windows ACL restrictions and application-level session authentication.**

The first implementation applies this model to:

- Desktop Gateway;
- Runtime worker control;
- Plugin Host control;
- helper-process control;
- and future local CLI access.

The initial production configuration will not expose:

- loopback TCP;
- LAN access;
- remote Runtime control;
- gRPC reflection;
- unauthenticated health endpoints;
- or browser-accessible endpoints.

This decision is:

- [ ] Permanent until superseded
- [x] Provisional pending prototype evidence
- [ ] Experimental only
- [ ] Limited to Desktop Gateway
- [ ] Limited to Windows permanently

---

# 22. Rationale

The selected approach provides:

- native Windows local transport;
- no production network port;
- operating-system ACL support;
- strong .NET implementation support;
- generated language-neutral contracts;
- streaming;
- deadlines;
- cancellation;
- flow control;
- and a direct future route to Unix-domain sockets.

A custom named-pipe protocol would provide control but would force Opure to design and secure framing, multiplexing, cancellation, flow control and compatibility.

Loopback TCP would simplify some tooling but would expose a network socket and create avoidable binding and firewall risks.

Windows-specific RPC technologies would weaken portability.

Shared memory may be useful for special large-data paths but is unnecessarily complex as the general command plane.

---

# 23. Logical Communication Architecture

```text
┌───────────────────────────────────────────────────────────────┐
│ Opure.Desktop                                                 │
│                                                               │
│ Generated Gateway Client                                      │
│ Session Authentication                                        │
│ Reconnect Manager                                             │
└───────────────────────────────┬───────────────────────────────┘
                                │
                                │ gRPC / HTTP2
                                │ Windows Named Pipe
                                ▼
┌───────────────────────────────────────────────────────────────┐
│ Opure.Runtime                                                 │
│                                                               │
│ Named-Pipe Listener                                           │
│ ACL Enforcement                                               │
│ Authentication Interceptor                                    │
│ Version Negotiation                                           │
│ Rate and Size Limits                                          │
│ Desktop Gateway Services                                      │
└───────────────┬───────────────────────────────┬───────────────┘
                │                               │
                │ gRPC / private pipe           │ gRPC / private pipe
                ▼                               ▼
┌───────────────────────────────┐  ┌───────────────────────────────┐
│ Opure.Worker                  │  │ Opure.PluginHost              │
│                               │  │                               │
│ bounded capability contract   │  │ plugin capability contract    │
│ per-process session           │  │ per-plugin session            │
└───────────────────────────────┘  └───────────────────────────────┘
```

---

# 24. Physical Endpoint Model

## 24.1 Runtime Gateway Endpoint

One primary named-pipe endpoint should expose the Desktop Gateway and authorised local client services.

Example conceptual identity:

```text
opure.runtime.<profile>.<runtime-instance>
```

The actual endpoint should include a high-entropy instance component.

The name is not secret.

---

## 24.2 Worker Endpoint

A worker should use a private per-process endpoint or parent-established connection.

Conceptual identity:

```text
opure.worker.<runtime-instance>.<worker-instance>
```

---

## 24.3 Plugin Host Endpoint

A Plugin Host should use a private per-host endpoint.

Conceptual identity:

```text
opure.plugin.<runtime-instance>.<plugin-host-instance>
```

The endpoint name must not reveal sensitive project names.

---

## 24.4 Recovery Endpoint

Recovery Mode should use a distinct endpoint namespace to avoid accidental connection to the ordinary Runtime.

---

## 24.5 Test Endpoint

Automated tests should use isolated random endpoint namespaces.

Tests must never connect accidentally to a developer's active Runtime.

---

# 25. Endpoint Discovery

The Desktop requires a safe way to discover the active Runtime.

The Runtime should create a user-scoped discovery record containing only safe metadata such as:

- installation profile;
- Runtime instance identifier;
- process identifier;
- start time;
- endpoint name;
- protocol-version range;
- application version;
- readiness;
- and descriptor version.

The descriptor must not contain:

- raw session secrets;
- Vault secrets;
- project data;
- or unrestricted bearer tokens.

---

## 25.1 Discovery Record Location

A likely Windows location is a user-scoped Opure Runtime directory under local application data.

The exact path is decided by the persistence and platform-layout ADRs.

The directory and descriptor must have restrictive ACLs.

---

## 25.2 Stale Discovery

The Desktop must validate:

- Runtime process existence;
- instance identity;
- endpoint responsiveness;
- protocol negotiation;
- and authentication.

A descriptor is a hint.

It is not proof that the endpoint is genuine.

Stale descriptors should be cleaned safely.

---

# 26. Pipe Access Control

The Runtime must configure named-pipe access control explicitly.

The production pipe should normally allow:

- the current user;
- the intended Runtime identity;
- and required system identities where necessary.

It should deny broad access such as:

- Everyone;
- Anonymous;
- ordinary remote access;
- and unrelated local users.

Default framework ACL behaviour must not be accepted without review.

---

## 26.1 Per-Endpoint ACL

Different endpoint classes may use different ACLs.

Examples:

- Desktop Gateway allows the current user's authenticated clients.
- Worker endpoint allows only the Runtime and launched worker context.
- Plugin Host endpoint allows only the Runtime and corresponding host.
- Recovery endpoint allows only current-user recovery components.

---

## 26.2 ACL Verification

Tests must verify that:

- another ordinary user cannot connect;
- an anonymous context cannot connect;
- an unrelated process without session authentication is rejected;
- stale worker credentials are rejected;
- and permission changes do not silently broaden access.

---

# 27. Application-Level Authentication

Pipe ACLs are necessary but not sufficient for Opure's application identity.

Every connection must establish an application session.

The session should prove:

- installation or profile identity;
- Runtime instance;
- client process class;
- client instance;
- protocol compatibility;
- and possession of authorised session material.

---

## 27.1 Authentication Goals

Authentication should prevent:

- accidental connection to the wrong Runtime;
- stale client reuse;
- unauthorised Opure process classes;
- worker impersonation;
- Plugin Host impersonation;
- and endpoint spoofing by an unrelated process where practical.

It cannot guarantee protection from fully compromised same-user or administrator-level code.

That limitation must be documented honestly.

---

## 27.2 Root Local Credential

Opure should maintain a per-user, per-installation local credential protected by operating-system-backed storage.

The credential should:

- not appear in ordinary configuration;
- not appear in the discovery descriptor;
- not appear in logs;
- be replaceable;
- be revocable;
- and be used to derive or validate short-lived sessions.

The exact Vault or operating-system storage mechanism is decided by the secrets ADR.

---

## 27.3 Session Establishment

A possible session flow is:

```text
Client connects to ACL-protected pipe
    ↓
Client sends Hello:
    - client class
    - client instance
    - protocol range
    - nonce
    ↓
Runtime sends Challenge:
    - Runtime instance
    - server nonce
    - selected protocol
    - challenge expiry
    ↓
Client sends proof derived from local credential
    ↓
Runtime validates proof and client class
    ↓
Runtime issues short-lived session identifier
    ↓
Subsequent calls include authenticated session metadata
```

The final cryptographic construction requires security review.

A standard established primitive must be used.

Custom cryptography is prohibited.

---

## 27.4 Child-Process Bootstrap

For Runtime-launched workers and Plugin Hosts, the Runtime may pass a one-time bootstrap credential through a safer inherited or restricted mechanism.

Preferred options include:

- inherited pipe or handle;
- one-time secret through protected standard input;
- restricted temporary file deleted after use;
- or operating-system process attribute where available.

Command-line arguments should not carry secrets.

---

## 27.5 Mutual Authentication

The client must authenticate the Runtime as well as the Runtime authenticating the client.

The Runtime proof should bind to:

- Runtime instance;
- endpoint;
- protocol;
- and challenge.

The client must reject a mismatched Runtime.

---

## 27.6 Session Expiry

Sessions should be:

- short lived;
- bound to Runtime instance;
- bound to client class;
- revocable;
- and invalid after Runtime restart.

Long-running streams may continue only while the session remains valid or through an approved renewal protocol.

---

# 28. Authorisation

Authentication answers who the process claims to be.

Authorisation answers what it may do.

The Desktop Gateway, worker and Plugin Host services must use capability and policy checks.

A valid session does not grant unrestricted service access.

---

## 28.1 Client Classes

Initial classes may include:

- Desktop;
- CLI;
- Trusted Worker;
- Plugin Host;
- Recovery Client;
- Test Client;
- Bootstrap Helper;
- and Update Helper.

Each class has a bounded service surface.

---

## 28.2 Service Exposure

The Runtime should expose only the services needed by each client class.

Examples:

### Desktop

- projections;
- commands;
- approvals;
- settings;
- diagnostics;
- and recovery.

### Trusted Worker

- task acknowledgement;
- progress;
- result;
- limited content access;
- cancellation;
- and health.

### Plugin Host

- granted plugin capabilities only;
- no administrative Runtime API;
- no unrestricted Trust Centre search;
- no unrestricted project access.

---

# 29. Contract Model

IPC contracts should be defined in explicit `.proto` files or an equivalent reviewed schema source.

The schema is authoritative.

Generated C# types are implementation artefacts.

---

## 29.1 Contract Categories

Contracts should be separated into:

- common primitives;
- identity;
- authentication;
- health;
- Desktop Gateway;
- projects;
- workflows;
- patches;
- approvals;
- builds;
- repositories;
- providers;
- memory;
- Trust Centre;
- plugins;
- MCP;
- workers;
- recovery;
- and diagnostics.

---

## 29.2 Contract Ownership

Every contract has an owner.

Examples:

- project contracts belong to Project Manager;
- patch contracts belong to Patch Service;
- approval contracts belong to Approval Manager;
- Desktop projections belong to Desktop Gateway;
- worker-control contracts belong to Process Supervisor.

A shared `Common` schema must remain small.

---

# 30. Command, Query and Event Semantics

## 30.1 Commands

Commands request state change.

A command request should include:

- command identifier;
- correlation identifier;
- causation identifier where relevant;
- project;
- expected version where relevant;
- idempotency key where relevant;
- deadline;
- and payload.

---

## 30.2 Queries

Queries request authoritative state.

A query should include:

- query identifier;
- correlation identifier;
- project or scope;
- consistency expectation where relevant;
- deadline;
- and payload.

---

## 30.3 Events

Events report completed state transitions.

An event envelope should include:

- event identifier;
- sequence;
- event type;
- contract version;
- Runtime instance;
- correlation;
- causation;
- project;
- time;
- and payload.

Events must not be hidden commands.

---

## 30.4 Projections

Desktop projections are view-oriented snapshots or deltas.

They are not domain events.

A projection should identify:

- projection type;
- projection version;
- source revision;
- generated time;
- staleness;
- and payload.

---

# 31. Versioning

Contracts must be versioned.

Versioning should distinguish:

- protocol version;
- service version;
- message schema version;
- projection version;
- and capability version.

Application semantic version alone is insufficient.

---

## 31.1 Compatibility Rules

Preferred compatibility rules include:

- new optional fields are allowed;
- unknown fields are ignored and preserved where relevant;
- field numbers are never reused;
- removed fields are reserved;
- enum unknown values are handled safely;
- required behaviour is represented through negotiated capabilities rather than implicit assumptions;
- and breaking changes require a new major protocol range.

---

## 31.2 Negotiation

Connection negotiation should exchange:

- minimum protocol;
- maximum protocol;
- component version;
- process class;
- capabilities;
- required features;
- and optional features.

The Runtime selects an overlapping supported protocol.

No overlap results in a clear incompatible-version error.

---

## 31.3 Feature Negotiation

Features should be identified by stable capability names.

Examples:

- `desktop.patch.partial-review.v1`
- `gateway.trust.streaming.v1`
- `worker.diff.syntax-aware.v1`
- `plugin.capability.patch-proposal.v1`

Capabilities must not be inferred only from version numbers.

---

# 32. Error Model

Transport errors and domain errors must remain distinct.

A response should map:

- gRPC transport status;
- Opure stable error code;
- safe human message;
- technical detail reference;
- retryability;
- recoverability;
- current state;
- and correlation identifier.

---

## 32.1 Domain Error Envelope

A domain error may include:

- `code`;
- `category`;
- `message`;
- `correlation_id`;
- `retryable`;
- `recovery_required`;
- `state_revision`;
- and structured details.

It must not include secret values.

---

## 32.2 Status Mapping

Examples:

- unauthenticated session maps to gRPC `Unauthenticated`;
- insufficient capability maps to `PermissionDenied`;
- missing resource maps to `NotFound`;
- stale expected version may map to `FailedPrecondition` or `Aborted`;
- deadline expiry maps to `DeadlineExceeded`;
- cancellation maps to `Cancelled`;
- invalid schema maps to `InvalidArgument`;
- unavailable Runtime capability maps to `Unavailable`;
- and internal failures map to `Internal` with a safe Opure code.

The Opure code remains the stable application contract.

---

# 33. Deadlines

Every call should have an explicit deadline or use a documented bounded default.

Suggested classes include:

- interactive query;
- interactive command admission;
- ordinary command;
- long-running start request;
- stream inactivity;
- worker heartbeat;
- and shutdown.

The deadline applies to the RPC call.

It does not automatically define the full lifetime of a durable operation.

---

## 33.1 Long-Running Operations

A long-running operation should normally use:

1. unary start command;
2. durable operation identity;
3. streamed or queried progress;
4. explicit cancellation command;
5. and authoritative final state.

The original RPC should not remain open for hours unless there is a specific reason.

---

# 34. Cancellation

Cancellation must propagate through:

- gRPC call context;
- Runtime command handling;
- Scheduler;
- worker task;
- external provider;
- and command process where applicable.

The client must distinguish:

- cancellation requested;
- request cancelled;
- durable operation cancelling;
- operation cancelled;
- and termination failed.

---

## 34.1 Client Disconnect

Client disconnect must not automatically cancel every durable operation.

Each operation declares a disconnect policy:

- cancel on disconnect;
- pause on disconnect;
- continue if pre-authorised;
- or continue independently.

---

# 35. Streaming

Streaming use cases include:

- model output;
- build output;
- test events;
- workflow progress;
- patch application progress;
- Trust Centre timeline;
- resource metrics;
- and notifications.

---

## 35.1 Stream Types

### Server Streaming

Preferred for:

- projections;
- event subscriptions;
- logs;
- progress;
- and results.

### Bidirectional Streaming

Reserved for:

- worker-control sessions;
- Plugin Host sessions;
- and tightly coupled supervised protocols.

### Client Streaming

Reserved for:

- bounded upload workflows;
- chunked diagnostics;
- or other justified cases.

---

## 35.2 Stream Ownership

Every stream should have one clear owner and purpose.

A generic untyped event stream should be avoided.

---

## 35.3 Stream Sequence

Stream items should include sequence numbers where ordering matters.

The client should detect:

- duplicate;
- gap;
- reset;
- and resynchronisation.

---

## 35.4 Resubscription

After reconnect, the client should provide:

- last known projection revision;
- last event sequence where supported;
- and subscription filters.

The Runtime may:

- replay bounded events;
- send a fresh snapshot;
- or declare replay unavailable.

The client must reconcile rather than assume continuity.

---

# 36. Backpressure

Backpressure is mandatory for high-volume streams.

The system should use:

- bounded channels;
- HTTP/2 flow control;
- batch delivery;
- coalescing;
- sampling for metrics;
- and client acknowledgement where required.

---

## 36.1 Coalescing

Coalescing is appropriate for:

- resource metrics;
- token display updates;
- file watcher bursts;
- progress summaries;
- and repeated health updates.

It is not appropriate for:

- final state;
- approval decisions;
- security incidents;
- patch operations;
- or repository changes.

---

## 36.2 Slow Clients

A slow client must not cause unbounded Runtime memory use.

The Runtime may:

- drop non-critical intermediate metrics;
- coalesce updates;
- disconnect an abusive or stalled stream;
- preserve authoritative state;
- and require resubscription.

Any dropped meaningful history must be available from durable service records where required.

---

# 37. Message Size Limits

Every endpoint and message type must have a configured size limit.

Suggested initial policy:

- ordinary command or query: small;
- projection snapshot: moderate;
- streamed item: small;
- diagnostic chunk: bounded;
- large content: external scoped reference.

Exact byte limits require prototype measurements.

---

## 37.1 Oversized Message

An oversized message should be rejected before allocation where possible.

The error should identify:

- message type;
- allowed limit;
- safe size metadata;
- and alternative transfer method.

Payload content must not be echoed in the error.

---

# 38. Large Payload Strategy

Large project files, diffs, logs, embeddings and artefacts should not normally travel as one gRPC message.

Preferred strategies include:

- paged query;
- server stream;
- chunked transfer;
- content-addressed temporary object;
- read-only scoped file handle;
- or service-owned content reference.

---

## 38.1 Content Reference

A content reference should include:

- content identifier;
- owner service;
- content type;
- size;
- hash;
- project;
- classification;
- expiry;
- allowed client class;
- allowed operation;
- and one-time or bounded-use state.

It must not be a raw unrestricted filesystem path.

---

## 38.2 Temporary Content Store

A temporary IPC content store may use:

- restricted directory;
- random identifiers;
- owner-only ACL;
- hash verification;
- expiry;
- and cleanup.

The storage ADR should define its final implementation.

---

## 38.3 Shared Memory

Shared memory may be considered later only for measured high-volume paths.

It requires a separate ADR.

---

# 39. Compression

Compression should not be enabled blindly.

Considerations include:

- CPU use;
- payload sensitivity;
- already compressed content;
- latency;
- and compression side-channel risk.

Local control messages normally do not need compression.

Large textual streams may use bounded compression if evidence supports it.

---

# 40. Metadata

Permitted gRPC metadata may include:

- session identifier;
- correlation identifier;
- client class;
- protocol capability token;
- deadline;
- tracing context;
- and request classification.

Metadata must not include:

- raw secrets;
- project source code;
- full prompts;
- or large payloads.

---

# 41. Correlation

Every meaningful request should carry a correlation identifier.

Causation identifiers should link:

- workflow stage;
- AI request;
- patch;
- approval;
- build;
- test;
- and repository operation.

Correlation must survive process boundaries.

---

# 42. Observability

IPC observability should include:

- connection count;
- authenticated sessions;
- client classes;
- calls by service and method;
- latency;
- errors;
- deadline expiry;
- cancellation;
- active streams;
- queued stream items;
- dropped or coalesced metrics;
- message sizes;
- reconnects;
- authentication failures;
- and compatibility failures.

Payload content should not be logged by default.

---

## 42.1 Logging

Safe IPC logs may include:

- endpoint class;
- Runtime instance;
- client class;
- client instance;
- method;
- status;
- duration;
- request size;
- response size;
- and correlation identifier.

They must not include:

- authentication proof;
- session secret;
- source code;
- secret values;
- or unredacted prompts.

---

## 42.2 Trust Centre

The Trust Centre should record significant IPC-mediated actions at the domain level.

It should not record every transport frame.

Relevant records may include:

- client connected;
- incompatible client rejected;
- plugin capability invoked;
- worker launched;
- protected command accepted;
- approval decision;
- and repeated authentication failure.

Routine successful health checks should not create Trust Centre noise.

---

# 43. Rate Limiting

Rate limits should protect:

- authentication attempts;
- session creation;
- expensive queries;
- diagnostic export;
- large content transfer;
- plugin calls;
- and worker messages.

Limits may be per:

- connection;
- session;
- client class;
- plugin;
- project;
- and method.

Trusted first-party clients still require bounded behaviour.

---

# 44. Connection Limits

The Runtime should define limits for:

- simultaneous Desktop clients;
- CLI clients;
- workers;
- Plugin Hosts;
- streams per client;
- and pending unauthenticated connections.

Exceeding a limit should fail clearly without degrading authoritative work.

---

# 45. Keepalive and Liveness

Long-lived connections should use bounded liveness detection.

The design may use:

- HTTP/2 keepalive;
- application heartbeat;
- or both.

Heartbeat should prove communication health.

It does not prove service readiness.

---

# 46. Health Service

A minimal authenticated health service may expose:

- Runtime instance;
- readiness;
- version;
- protocol range;
- Safe Mode;
- Recovery Required;
- and major capability health.

It must not expose sensitive project state.

Unauthenticated health is disabled by default.

A minimal pre-authentication handshake may reveal only what is required for compatibility and challenge.

---

# 47. Desktop Gateway Services

The Desktop Gateway should expose view-oriented services such as:

- Runtime;
- Home;
- Projects;
- Workflows;
- Patches;
- Approvals;
- Build and Test;
- Repository;
- Providers and Models;
- Memory;
- Trust Centre;
- Plugins;
- MCP;
- Settings;
- Notifications;
- Diagnostics;
- and Recovery.

The Desktop should not call internal worker services.

---

# 48. Worker Service

A worker-control contract should support:

- Hello;
- authenticate;
- receive task;
- acknowledge task;
- report progress;
- request scoped content;
- deliver result;
- report failure;
- heartbeat;
- receive cancellation;
- and shutdown.

A worker must not discover arbitrary Runtime services.

---

# 49. Plugin Host Service

A Plugin Host contract should support:

- package identity;
- host authentication;
- permission manifest;
- capability registration;
- invocation;
- progress;
- result;
- cancellation;
- health;
- storage requests;
- and shutdown.

The Runtime must filter all capability calls through granted permissions.

---

# 50. CLI Service

A future CLI should use a client-class-specific service surface.

The CLI must not receive unrestricted administrative access merely because it is first party.

Interactive approvals may require:

- terminal confirmation;
- desktop handoff;
- or policy denial.

---

# 51. Recovery Client Service

Recovery clients should receive only recovery capabilities.

A damaged ordinary Runtime should not expose the full normal service surface through the Recovery endpoint.

---

# 52. Development Transport

Development builds may optionally expose loopback TCP for:

- protocol inspection;
- test tools;
- and cross-language experiments.

This mode must be:

- disabled by default;
- explicitly configured;
- visibly marked;
- restricted to development profiles;
- authenticated;
- and impossible to enable accidentally in release packaging.

---

# 53. Production Transport Rules

Production defaults:

- named pipes only;
- no TCP listener;
- no gRPC reflection;
- no unauthenticated methods;
- restrictive ACL;
- session authentication;
- bounded message sizes;
- bounded streams;
- and safe logging.

---

# 54. Cross-Platform Transport

Future Linux and macOS implementations should use:

- the same gRPC service contracts;
- Unix-domain sockets;
- filesystem permissions on the socket path;
- per-user runtime directories;
- application-level session authentication;
- and equivalent version negotiation.

Transport-specific details belong behind an abstraction.

---

## 54.1 Transport Abstraction

The codebase should define a transport-neutral endpoint model containing:

- transport kind;
- endpoint identity;
- Runtime instance;
- access policy;
- authentication scheme;
- protocol range;
- and connection options.

Domain services must not depend on named-pipe classes.

---

# 55. Unix-Domain Socket Considerations

Future implementations must account for:

- socket path length limits;
- stale socket files;
- filesystem permissions;
- runtime directory ownership;
- abstract namespace differences;
- process cleanup;
- and container or sandbox behaviour.

The Windows named-pipe design must not assume every future transport has Windows ACL semantics.

---

# 56. Security Impact

## 56.1 Trust Boundaries

The IPC design creates boundaries between:

- Desktop and Runtime;
- Runtime and worker;
- Runtime and Plugin Host;
- Runtime and helper;
- and future CLI and Runtime.

---

## 56.2 Threats

Relevant threats include:

- endpoint spoofing;
- unauthorised connection;
- stale credential reuse;
- replay;
- downgrade attack;
- message tampering;
- oversized message;
- stream exhaustion;
- connection exhaustion;
- authentication brute force;
- method enumeration;
- confused-deputy calls;
- plugin capability escalation;
- worker impersonation;
- endpoint descriptor replacement;
- and diagnostic leakage.

---

## 56.3 Mitigations

- restrictive pipe ACL;
- mutual session authentication;
- nonces;
- short-lived sessions;
- version binding;
- capability-scoped service exposure;
- endpoint descriptor ACL;
- process-class identity;
- rate limits;
- message limits;
- stream limits;
- correlation;
- safe logs;
- no reflection;
- and security tests.

---

## 56.4 Same-User Threat Limitation

A malicious process running with the same user privileges may be able to:

- inspect user-accessible files;
- call user-level credential APIs;
- inject into processes;
- or impersonate ordinary user actions

depending on operating-system protections and attack sophistication.

Opure's IPC controls reduce accidental and opportunistic misuse.

They do not claim to defend fully against a compromised user account or administrator.

---

# 57. Privacy Impact

The selected transport remains local.

It does not require:

- external network communication;
- cloud account;
- telemetry;
- or remote certificates.

IPC diagnostics must not record payload content by default.

Local message contents still require classification and retention controls.

---

# 58. Secret Handling

Raw secret values should not normally travel through general Desktop Gateway calls.

Where a process needs a secret:

- it should request a secret-use capability;
- the Runtime should deliver or apply it through a narrow mechanism;
- the destination and purpose remain visible;
- and the raw value should avoid ordinary protobuf messages where practical.

Any unavoidable secret transfer must use:

- authenticated channel;
- narrow recipient;
- short lifetime;
- no logging;
- and memory minimisation.

---

# 59. TLS Decision

TLS is not selected for the initial named-pipe transport.

The security model uses:

- local named-pipe ACLs;
- application-level mutual authentication;
- and operating-system process and credential controls.

This decision must be reviewed if:

- transport moves to TCP;
- traffic crosses a machine boundary;
- named-pipe security behaviour changes;
- or threat modelling shows an unmet local confidentiality requirement.

---

# 60. Reflection and Discovery

gRPC reflection must be disabled in production.

Service discovery occurs through:

- negotiated capabilities;
- explicit generated clients;
- and bounded metadata.

Development reflection may be enabled only in an explicit development profile.

---

# 61. Interceptors

Server and client interceptors may implement:

- session authentication;
- capability validation;
- correlation;
- deadline defaults;
- request-size checks;
- safe metrics;
- error translation;
- and logging.

Policy decisions remain in the Policy Engine.

An interceptor must not become a hidden replacement for domain authorisation.

---

# 62. Idempotency

State-changing commands should declare idempotency behaviour.

An idempotency key may be used when:

- retries are possible;
- the operation has stable semantics;
- and duplicate execution can be detected.

Idempotency records belong to the owning service, not the transport layer alone.

---

# 63. Retry

Clients may automatically retry only safe transport scenarios and idempotent calls.

Automatic retry should normally be limited to:

- connection establishment;
- safe query;
- read-only projection;
- and explicitly idempotent command admission.

The client must not blindly retry:

- patch apply;
- Git commit;
- package installation;
- external publication;
- approval decision;
- or secret-use operation.

---

# 64. Reconnection

The Desktop reconnect manager should:

1. detect connection loss;
2. mark projections stale;
3. stop reporting command success;
4. read or rediscover Runtime descriptor;
5. authenticate to the active Runtime;
6. negotiate protocol;
7. resubscribe;
8. request snapshots or replay;
9. reconcile active operations;
10. and clear stale indicators only after authoritative state is restored.

---

# 65. Runtime Restart

A Runtime restart invalidates:

- sessions;
- streams;
- worker credentials;
- Plugin Host credentials;
- and ephemeral endpoint references.

Durable operation identities remain valid if the owning service recovers them.

Clients must reconnect to the new Runtime instance.

---

# 66. Client Restart

A Desktop restart should not require Runtime restart.

The new Desktop session should:

- authenticate;
- request current state;
- resume permitted subscriptions;
- and restore only non-sensitive view state.

---

# 67. Worker Reconnect

A trusted worker normally should not reconnect after Runtime restart unless a specific recovery protocol exists.

The safer initial behaviour is:

- worker exits on parent loss;
- Runtime recovers task state;
- and a new worker is launched if safe.

---

# 68. Plugin Host Reconnect

A Plugin Host should normally terminate when the Runtime session is lost.

The Runtime may launch a new host after:

- package validation;
- permission validation;
- project validation;
- and recovery checks.

---

# 69. Endpoint Rotation

The Runtime should rotate the endpoint identity on restart.

A predictable stable endpoint may be retained only as a discovery bootstrap if separately secured.

The active service endpoint should be instance bound.

---

# 70. Credential Rotation

The per-user local credential should support rotation after:

- suspected compromise;
- installation repair;
- profile reset;
- major security migration;
- or explicit developer action.

Rotation invalidates existing sessions.

---

# 71. Audit Failure

If Trust Centre persistence is unavailable:

- routine read-only communication may remain available;
- protected state-changing commands may be denied or constrained according to policy;
- authentication and security failures must remain in a bounded recovery buffer;
- and the desktop must show degraded trust state.

---

# 72. Failure Handling

## 72.1 Pipe Creation Failure

The Runtime should:

- report a stable error;
- avoid falling back silently to TCP;
- attempt a new high-entropy endpoint when safe;
- inspect stale endpoint state;
- and offer Recovery Mode.

---

## 72.2 Access-Control Failure

If the Runtime cannot apply restrictive ACLs:

- the production endpoint must not start;
- the Runtime enters failed or recovery state;
- and no broad-access pipe is accepted as a convenience fallback.

---

## 72.3 Authentication Failure

The Runtime should:

- reject the session;
- apply rate limiting;
- record safe security metadata;
- avoid revealing whether a credential component was correct;
- and show repeated suspicious failure in the Trust Centre.

---

## 72.4 Protocol Mismatch

The connection should fail with:

- client version;
- Runtime version;
- supported protocol ranges;
- safe remediation;
- and no partial service access.

---

## 72.5 Stream Failure

The client should know whether:

- transport failed;
- server cancelled;
- client cancelled;
- deadline expired;
- session expired;
- or operation failed.

---

## 72.6 Backpressure Failure

If a client cannot consume a critical stream:

- the stream may terminate;
- durable state remains available;
- the client must resynchronise;
- and the final operation state must not be lost.

---

# 73. Performance Impact

## 73.1 Expected Performance

Named pipes avoid network-interface exposure and should provide suitable local control-plane performance.

gRPC adds:

- serialisation;
- HTTP/2 framing;
- metadata;
- and dispatch overhead.

This is expected to be acceptable for Opure's service and streaming workloads.

The expectation requires measurement.

---

## 73.2 Reference Hardware

Measurements should use:

- Windows 11;
- Ryzen 9 5950X;
- 32 GB RAM;
- RTX 5070 Ti 16 GB.

IPC overhead should remain small compared with:

- local model inference;
- file indexing;
- builds;
- tests;
- and large diff rendering.

---

## 73.3 Required Benchmarks

Measure:

- connection setup;
- authentication handshake;
- unary round trip;
- concurrent unary calls;
- small server stream;
- sustained log stream;
- model token stream;
- bidirectional worker stream;
- cancellation latency;
- reconnect;
- message-size rejection;
- backpressure;
- 1, 2, 4 and 8 client streams;
- and Runtime CPU and memory overhead.

---

# 74. Benchmark Payloads

Benchmark payload classes should include:

- 128-byte control message;
- 1 KiB request;
- 16 KiB projection;
- 256 KiB snapshot;
- 1 MiB bounded transfer;
- long stream of small items;
- and content-reference lookup for large objects.

Large single messages beyond the intended budget should be tested as rejection cases.

---

# 75. Reliability and Recovery

IPC is not the durable source of truth.

If a connection fails:

- authoritative services retain state;
- durable operations continue according to policy;
- clients reconnect;
- and projections reconcile.

Transport replay must not substitute for domain recovery.

---

# 76. Threading and Concurrency

Service handlers must avoid blocking transport threads.

Handlers should:

- validate quickly;
- admit commands;
- return operation identities;
- use asynchronous IO;
- and delegate long work to the Scheduler or owning service.

Synchronous blocking on async work is prohibited.

---

# 77. Connection Ownership

The Runtime owns server endpoint lifecycle.

Clients own:

- connection;
- session renewal;
- subscriptions;
- and reconnect.

No service should create an independent hidden IPC listener without Process Supervisor registration and approval.

---

# 78. Endpoint Registry

The Runtime should maintain an internal endpoint registry containing:

- endpoint identity;
- process class;
- owning process;
- transport;
- ACL profile;
- authentication profile;
- protocol range;
- readiness;
- and lifecycle.

The registry must not expose secret material.

---

# 79. Testing Strategy

## 79.1 Unit Tests

Test:

- protocol negotiation;
- capability negotiation;
- session expiry;
- idempotency metadata;
- error mapping;
- message-size rules;
- sequence handling;
- and reconnect decisions.

---

## 79.2 Contract Tests

Test:

- schema compatibility;
- unknown fields;
- enum evolution;
- field reservation;
- old client to new Runtime;
- new client to old Runtime;
- and unsupported capability.

---

## 79.3 Integration Tests

Test:

- Desktop-to-Runtime call;
- streaming subscription;
- command admission;
- Runtime-to-worker task;
- Runtime-to-Plugin-Host invocation;
- cancellation;
- reconnect;
- Runtime restart;
- and Safe Mode endpoint.

---

## 79.4 Security Tests

Test:

- wrong user;
- missing ACL;
- broad ACL;
- endpoint spoof;
- stale descriptor;
- invalid challenge proof;
- replayed proof;
- expired session;
- wrong client class;
- worker impersonation;
- plugin impersonation;
- downgrade attempt;
- oversized message;
- stream exhaustion;
- and reflection exposure.

---

## 79.5 Fault-Injection Tests

Inject:

- broken pipe;
- half-open connection;
- Runtime crash;
- Desktop crash;
- worker crash;
- delayed stream;
- dropped sequence;
- authentication-store failure;
- descriptor corruption;
- disk full during discovery update;
- and deadline race.

---

## 79.6 Performance Tests

Test:

- latency;
- throughput;
- CPU;
- allocations;
- memory;
- concurrent streams;
- backpressure;
- cancellation;
- and reconnect.

---

## 79.7 Recovery Tests

Test:

- stale descriptor cleanup;
- endpoint rotation;
- session invalidation;
- projection resynchronisation;
- worker termination on parent loss;
- Plugin Host restart;
- and Recovery Mode connection.

---

# 80. Prototype Plan

## 80.1 Prototype A — Desktop Gateway

Implement:

- named-pipe Kestrel server;
- generated Desktop client;
- Hello and negotiation;
- authenticated session;
- unary health query;
- server-streaming Runtime state;
- disconnect;
- and reconnect.

---

## 80.2 Prototype B — Worker Session

Implement:

- Runtime-launched worker;
- one-time bootstrap;
- private endpoint;
- bidirectional control stream;
- progress;
- result;
- cancellation;
- and parent-loss exit.

---

## 80.3 Prototype C — Plugin Host

Implement:

- test plugin package;
- isolated host;
- package identity;
- capability negotiation;
- granted method surface;
- denied method test;
- crash;
- restart;
- and quarantine signal.

---

## 80.4 Prototype D — Backpressure

Implement a producer that exceeds client rendering speed.

Prove:

- bounded Runtime memory;
- coalescing;
- final-state preservation;
- stream termination where required;
- and resynchronisation.

---

## 80.5 Prototype E — Security

Prove:

- wrong user denied by ACL;
- wrong application credential denied;
- stale session denied;
- replay denied;
- protocol downgrade denied;
- and production TCP absent.

---

## 80.6 Prototype F — Large Content

Implement:

- ordinary size rejection;
- content-reference creation;
- authorised read;
- hash validation;
- expiry;
- and cleanup.

---

# 81. Implementation Plan

## 81.1 Initial Tasks

1. Record founder review.
2. Pin the selected .NET LTS release.
3. Define IPC contract repository layout.
4. Select Protocol Buffer tooling.
5. Define Runtime endpoint descriptor.
6. Define pipe ACL profiles.
7. Define local root credential storage.
8. Define authentication handshake.
9. Implement Desktop Gateway prototype.
10. Implement Runtime-to-worker prototype.
11. Implement Plugin Host prototype.
12. Implement version negotiation.
13. Implement error envelope.
14. Implement correlation interceptors.
15. Implement message-size limits.
16. Implement streaming and cancellation.
17. Implement backpressure tests.
18. Implement reconnect and resynchronisation.
19. Benchmark.
20. Complete security review.
21. Accept, amend or reject the ADR.

---

## 81.2 Owners

| Area | Owner |
|---|---|
| Product decision | Founder |
| IPC architecture | Runtime Architecture Owner |
| Protocol contracts | Service Architecture Owner |
| Desktop client | Desktop Owner |
| Worker protocol | Runtime Owner |
| Plugin Host protocol | Plugin Owner |
| Authentication | Security Owner |
| Performance | Performance Owner |
| Recovery | Recovery Owner |
| Testing | Test Architecture Owner |

---

# 82. Suggested Repository Structure

```text
src/
├── Opure.Contracts/
│   ├── Protos/
│   │   ├── common/
│   │   ├── identity/
│   │   ├── gateway/
│   │   ├── projects/
│   │   ├── workflows/
│   │   ├── patches/
│   │   ├── approvals/
│   │   ├── builds/
│   │   ├── repository/
│   │   ├── providers/
│   │   ├── memory/
│   │   ├── trust/
│   │   ├── plugins/
│   │   ├── workers/
│   │   └── recovery/
│   └── Generated/
├── Opure.Ipc.Abstractions/
├── Opure.Ipc.Grpc/
├── Opure.Ipc.NamedPipes.Windows/
├── Opure.Ipc.Authentication/
├── Opure.Ipc.Diagnostics/
└── Opure.Desktop.GatewayClient/
```

Future projects may include:

```text
├── Opure.Ipc.UnixDomainSockets/
└── Opure.Ipc.Testing/
```

---

# 83. Coding Rules

IPC implementation should:

- use generated contracts;
- use cancellation tokens;
- use deadlines;
- avoid unbounded collections;
- avoid payload logging;
- avoid broad `Any` messages;
- avoid arbitrary maps for core contracts;
- validate strings and byte arrays;
- use stable identifiers;
- and translate framework exceptions at boundaries.

---

# 84. Protocol Buffer Rules

- Field numbers are never reused.
- Removed fields are reserved.
- Message names are domain specific.
- Common messages remain minimal.
- `bytes` fields require explicit maximum size.
- `string` fields require validation where meaningful.
- Times use one standard representation.
- Durations use one standard representation.
- Identifiers use canonical string or byte representation.
- Money or cost values use exact structured representation.
- Paths are never trusted merely because they are typed as paths.
- Secret values use dedicated restricted message types only where unavoidable.
- Generic `Struct` is avoided in trusted contracts.
- `Any` is avoided unless an extensibility boundary requires it and type allowlists exist.

---

# 85. Compatibility Policy

Before version 1.0:

- breaking changes are allowed only with coordinated component updates;
- stored durable operation state must remain migratable;
- and compatibility tests remain mandatory.

At version 1.0:

- public protocol ranges must be documented;
- deprecation requires notice;
- and Plugin Host contracts require a defined compatibility window.

---

# 86. Licensing and Supply Chain

Before acceptance, review:

- gRPC .NET packages;
- Protocol Buffer compiler and runtime;
- Kestrel named-pipe transport package;
- code-generation tools;
- testing tools;
- and any cryptographic dependencies.

Dependencies should use:

- central version management;
- locked restore;
- licence inventory;
- vulnerability review;
- and update policy.

---

# 87. Official Evidence References

The following official Microsoft sources informed this ADR:

- [Inter-process communication with gRPC and named pipes](https://learn.microsoft.com/en-us/aspnet/core/grpc/interprocess-namedpipes)
- [Inter-process communication with gRPC](https://learn.microsoft.com/en-us/aspnet/core/grpc/interprocess)
- [Kestrel named-pipe transport namespace](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.server.kestrel.transport.namedpipes)
- [NamedPipeTransportOptions.PipeSecurity](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.server.kestrel.transport.namedpipes.namedpipetransportoptions.pipesecurity)
- [System.IO.Pipes](https://learn.microsoft.com/en-us/dotnet/api/system.io.pipes)
- [PipeSecurity](https://learn.microsoft.com/en-us/dotnet/api/system.io.pipes.pipesecurity)
- [Socket.OSSupportsUnixDomainSockets](https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.socket.ossupportsunixdomainsockets)

Support details can change.

The implementation team must verify the documentation for the pinned .NET LTS release before acceptance.

---

# 88. Acceptance Criteria

This ADR may move to **Accepted** when:

- [ ] Kestrel serves gRPC over a Windows named pipe.
- [ ] No production TCP listener is open.
- [ ] Pipe ACL is explicitly configured.
- [ ] Another ordinary user cannot connect.
- [ ] A process without valid application authentication is rejected.
- [ ] Desktop and Runtime mutually authenticate.
- [ ] Session credentials are short lived.
- [ ] Sessions are invalid after Runtime restart.
- [ ] Endpoint descriptors contain no secrets.
- [ ] Protocol versions are negotiated.
- [ ] Incompatible clients fail clearly.
- [ ] Capability negotiation works.
- [ ] Unary commands and queries work.
- [ ] Server streaming works.
- [ ] Worker bidirectional streaming works.
- [ ] Cancellation propagates.
- [ ] Deadlines are enforced.
- [ ] Client disconnect policy is explicit.
- [ ] Backpressure keeps Runtime memory bounded.
- [ ] Slow clients can resynchronise.
- [ ] Message-size limits are enforced.
- [ ] Large content uses a scoped reference mechanism.
- [ ] Authentication failures are rate limited.
- [ ] gRPC reflection is disabled in production.
- [ ] Payloads are not logged by default.
- [ ] Stable Opure errors survive transport translation.
- [ ] Correlation survives process boundaries.
- [ ] Desktop reconnect restores authoritative projections.
- [ ] Worker exits safely on parent loss.
- [ ] Plugin Host loses access after session revocation.
- [ ] Safe Mode uses a separate bounded service surface.
- [ ] Latency and throughput are measured.
- [ ] Security review is complete.
- [ ] Founder approval is recorded.

---

# 89. Evidence Required Before Acceptance

- [ ] Desktop Gateway prototype;
- [ ] worker protocol prototype;
- [ ] Plugin Host protocol prototype;
- [ ] ACL test report;
- [ ] authentication test report;
- [ ] replay and stale-session test;
- [ ] compatibility test;
- [ ] streaming test;
- [ ] cancellation test;
- [ ] backpressure test;
- [ ] oversized-message test;
- [ ] large-content reference test;
- [ ] reconnect test;
- [ ] performance benchmark;
- [ ] dependency and licence review;
- [ ] security review;
- [ ] founder approval.

---

# 90. Known Limitations

- The exact .NET LTS release is not yet pinned.
- The authentication cryptographic construction is not yet final.
- The local root credential storage is not yet selected.
- Exact message-size limits are not yet measured.
- Exact rate limits are not yet measured.
- The content-reference storage mechanism is not yet selected.
- Unix-domain socket implementation is deferred.
- Remote Runtime access is not supported.
- Browser clients cannot use the production named-pipe endpoint.
- Same-user malware is not fully prevented by IPC controls.
- Multiple simultaneous interactive Desktop clients are deferred.
- CLI approval behaviour is deferred.
- Worker pooling is deferred.
- Shared-memory optimisation is deferred.
- TCP diagnostics mode is development-only and not yet designed.

---

# 91. Open Questions

- Which .NET LTS release should be pinned?
- Which gRPC and Protocol Buffer package versions should be pinned?
- Should the Runtime expose one physical pipe or separate physical pipes by client class?
- Should Desktop commands and event subscriptions share one HTTP/2 connection?
- Which operating-system-backed mechanism stores the local root credential?
- Should the Desktop be launched with a one-time bootstrap token on first connection?
- How should independent Desktop restart obtain authentication material?
- Which cryptographic challenge protocol should be used?
- How should process identity be verified beyond credential possession?
- What message-size budgets should apply to each service?
- How should content references be stored and revoked?
- Should Plugin Hosts use one bidirectional stream or multiple RPC services?
- Should trusted workers use a parent-established pipe rather than endpoint discovery?
- How long should sessions remain valid?
- How should session renewal work for long streams?
- Which development diagnostics require TCP?
- Should local CLI access be enabled before version 1.0?
- How should API compatibility be communicated in installer upgrades?
- How should recovery clients authenticate if ordinary credential storage is damaged?

---

# 92. Deferred Decisions

This ADR intentionally defers:

- persistence to ADR-0005;
- local credential storage to a Secrets Vault ADR;
- exact cryptographic construction to a security ADR;
- contract repository layout to implementation planning;
- large-content storage to a persistence or content-store ADR;
- Unix-domain socket implementation to a future platform ADR;
- remote Runtime access to a future specification;
- CLI approval model to a CLI ADR;
- updater communication to an update ADR;
- shared-memory optimisation to an evidence-driven ADR;
- and public remote APIs to post-1.0 planning.

---

# 93. Alternatives Rejected

## 93.1 Custom Named-Pipe Protocol

Rejected because Opure would need to build and secure framing, multiplexing, flow control, cancellation, schema evolution and generated clients without evidence that gRPC overhead is unacceptable.

---

## 93.2 Loopback TCP as Production Default

Rejected because it opens a network socket and introduces avoidable binding, firewall and exposure risk for a local desktop platform.

---

## 93.3 JSON HTTP and WebSocket

Rejected because it requires multiple communication patterns and offers weaker contract and streaming discipline for the primary C# service architecture.

---

## 93.4 Windows-Specific RPC as the Contract Foundation

Rejected because it would weaken the future Linux and macOS transport path and increase platform lock-in.

---

## 93.5 Shared Memory as General IPC

Rejected because its synchronisation, validation and recovery complexity are disproportionate for the general control plane.

---

## 93.6 Local Message Broker

Rejected because it introduces an additional operational subsystem and distributed-message semantics before Opure requires them.

---

## 93.7 Standard Input and Output as General IPC

Rejected because it does not support Runtime discovery, reconnect, multiple clients or robust long-lived multiplexed communication.

---

# 94. Review Record

| Date | Reviewer | Decision | Notes |
|---|---|---|---|
| 18 July 2026 | Architecture draft | Proposed | gRPC over named pipes recommended with ACL and application authentication |

---

# 95. Approval

## Founder or Product Approval

- **Name:** Christopher Dyer
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Pending founder review

## Architecture Approval

- **Name or role:** Runtime Architecture Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Transport, streaming and compatibility prototypes required

## Security Approval

- **Name or role:** Security Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** ACL, mutual authentication, replay and same-user threat review required

## Performance Approval

- **Name or role:** Performance Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Latency, throughput, allocation and backpressure evidence required

---

# 96. Supersession

This ADR is superseded only when a later ADR:

- names ADR-0004 explicitly;
- explains why the transport changed;
- identifies affected client classes;
- describes contract compatibility;
- describes credential and endpoint migration;
- explains security impact;
- and updates the `Superseded by` field.

Historical ADRs remain in version control.

---

# 97. Change History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | 18 July 2026 | Founder Draft | Initial gRPC-over-named-pipes recommendation |

---

# 98. Final Decision Statement

> **Opure will provisionally use gRPC over Windows named pipes, with explicit Protocol Buffer contracts, restrictive pipe ACLs and application-level mutual session authentication, because this provides secure local communication, generated versioned contracts, streaming, cancellation and a practical future path to Unix-domain sockets without exposing a production network listener.**

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**