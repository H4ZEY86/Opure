# ADR-0018 — MCP Server Trust and Permission Model

## Architecture Decision Record

**Status:** Proposed  
**Date:** 18 July 2026  
**Decision owners:** Founder and Product Owner  
**Technical owners:** MCP Gateway and Trust Owner  
**Reviewers:** Runtime Architecture Owner, Security Owner, Network Gateway Owner, Secrets Owner, Plugin Platform Owner, Workspace Owner, AI Router Owner, Trust Centre Owner, Desktop Owner, Persistence Owner, Recovery Owner, Test Architecture Owner  
**Supersedes:** None  
**Superseded by:** None  
**Related ADRs:** ADR-0001 through ADR-0017  
**Related specifications:** CHARTER-001, SPEC-001 through SPEC-012  
**Target milestone:** MCP Gateway Foundation through Version 1.0  

---

## 1. Decision Summary

Opure should implement MCP through a **trusted MCP Gateway that treats every server, capability description, prompt, resource and tool result as untrusted until separately registered, classified, approved and mediated**.

MCP is an interoperability protocol.

It is not:

- a trust system;
- a plugin sandbox;
- a package manager;
- a secrets manager;
- an approval mechanism;
- an authorisation substitute;
- or evidence that a server is safe.

The initial MCP architecture should:

- implement a trusted `Opure.McpGateway` service inside the Runtime boundary;
- expose no raw MCP connection directly to the Desktop, AI Router, plugins or workflow definitions;
- support the stable MCP protocol revision `2025-11-25`;
- support `2025-06-18` only through an explicit compatibility profile;
- reject unsupported and experimental protocol features by default;
- negotiate capabilities explicitly during MCP initialisation;
- use JSON-RPC 2.0 validation with bounded message, nesting, collection and string limits;
- support local `stdio` and remote Streamable HTTP transports;
- prefer `stdio` for local servers;
- use Streamable HTTP only for remote HTTPS endpoints or explicitly approved loopback development endpoints;
- disable legacy HTTP+SSE by default;
- require explicit compatibility approval before connecting to a legacy HTTP+SSE server;
- prohibit custom transports until separately reviewed;
- treat a local MCP server as executable software installed on the developer's machine;
- treat a remote MCP server as an external network service receiving selected data and returning untrusted content;
- require explicit server registration before connection;
- require exact local executable, argument array, working directory, package or file hash, publisher evidence and sandbox profile;
- prohibit shell-string execution;
- prohibit hidden command expansion;
- prohibit automatic execution of `cmd /c`, PowerShell command strings, `npx`, `npm exec`, `uvx`, `pipx`, package-manager runners or mutable Git checkouts in production mode;
- permit those development runners only in an explicit Development or Legacy configuration with the full exact command displayed and no production trust claim;
- require remote servers to use an exact canonical HTTPS endpoint and approved origin;
- route all remote MCP and OAuth discovery traffic through the Network Gateway;
- block SSRF-sensitive destinations and validate every redirect and DNS result;
- require OAuth 2.1 resource-server authorisation for protected remote servers;
- use authorisation-code flow with PKCE for user-delegated access;
- operate Opure as a public OAuth client unless a server-specific confidential-client integration is separately registered;
- prefer OAuth Client ID Metadata Documents where supported;
- support Protected Resource Metadata, Authorization Server Metadata and OpenID Connect Discovery as specified;
- use Dynamic Client Registration only when explicitly allowed;
- store access and refresh tokens only in the Secrets Vault;
- bind tokens to the exact MCP server resource, audience, authorisation server, account, scope set, channel and profile;
- prohibit token passthrough;
- prohibit query-string access tokens;
- request minimum scopes and incremental scope elevation;
- display every new or broadened OAuth scope before browser authorisation;
- never expose MCP OAuth tokens to an AI model, plugin, workflow record, log or Trust Centre payload;
- treat MCP session IDs as opaque routing state rather than authentication;
- keep session IDs in memory where practical;
- never log full session IDs;
- reauthenticate every HTTP request;
- and end remote sessions explicitly when no longer needed.

The initial server trust classes should be:

1. **First-Party Built-In**
2. **Verified Local Package**
3. **Verified Local Executable**
4. **Sandboxed Local Unverified**
5. **Trusted Remote Organisation**
6. **Authenticated Remote**
7. **Registry Discovered**
8. **Local Legacy Full Trust**
9. **Quarantined**
10. **Revoked or Withdrawn**

The official MCP Registry, while useful for namespace and metadata discovery, should be treated as:

> **Discovery metadata only, not installation approval, publisher verification, code review, malware review, tool approval or runtime trust.**

The initial local-server execution policy should:

- use a dedicated MCP Server Host or Process Supervisor launch path;
- create one server instance per registration or project policy;
- run verified third-party local servers in a unique Windows AppContainer where compatible;
- use no direct project-root access by default;
- grant only explicit read or write directories required by the server profile;
- grant no direct network unless separately approved;
- place every local server in a non-breakaway Job Object;
- enforce kill-on-close, child-process, memory, CPU, process-count and lifetime policies;
- pass a minimal environment;
- pass no unrelated inheritable handles;
- use exact executable and argument vectors;
- capture bounded `stderr` as untrusted diagnostics;
- treat `stdout` exclusively as MCP JSON-RPC for `stdio`;
- terminate a server that emits non-protocol data to `stdout`;
- and fail closed rather than silently falling back from sandboxed to full-trust execution.

A local full-trust MCP server should be permitted only in an explicit **Legacy Full Trust** mode that:

- warns that the server can act with the Windows user's authority;
- displays the exact executable, every argument, working directory and environment-secret binding;
- disables automatic startup by default;
- prohibits silent tool approval;
- disables automatic provider sampling;
- disables automatic project-root exposure;
- and creates a high-risk Trust Centre record.

The MCP feature policy should be:

### Tools

- discover tool metadata only after server registration;
- treat tool names, descriptions, annotations and icons as untrusted;
- canonicalise and hash each tool definition and input/output schema;
- classify every tool through Opure policy rather than trusting server annotations;
- require explicit developer approval before a tool invocation by default;
- permit narrowly persistent approval only for low-risk, read-only, exact server/tool/schema/project combinations;
- require per-call approval for mutation, deletion, external transmission, authentication changes, code execution, package management, financial or irreversible actions;
- let an AI propose a tool call but never approve it;
- validate arguments against the declared input schema and an Opure policy schema;
- reject unexpected fields where policy requires;
- validate structured output against the declared output schema;
- bound and classify all output;
- never automatically dereference tool-returned resource links;
- never automatically execute instructions contained in tool output;
- and invalidate saved approval whenever a tool name, schema, description, annotation, execution property or server identity changes materially.

### Resources

- list metadata only within bounds;
- treat resource URIs, names, MIME types, annotations and contents as untrusted;
- read resources only after user selection or an approved deterministic workflow rule;
- keep provenance attached to every resource;
- validate MIME type, size and content;
- render active content safely;
- prohibit automatic remote image or link retrieval;
- treat prompt-like instructions in resources as data rather than authority;
- and disable resource subscriptions initially.

### Prompts

- expose MCP prompts as explicit user-selectable templates;
- never insert an MCP prompt automatically into a system or developer instruction layer;
- show the exact server and prompt provenance;
- show generated messages and embedded resources before use;
- treat prompt content as untrusted;
- prohibit a server prompt from changing Opure security policy;
- and invalidate saved prompt approval when its definition changes.

### Roots

- do not advertise the MCP `roots` client capability initially;
- do not expose raw project root paths or `file://` URIs to remote servers;
- do not treat roots as access control;
- and defer root support until Opure can expose an ephemeral, read-only, server-specific exported workspace rather than a developer repository path.

### Sampling

- do not advertise MCP sampling initially;
- do not permit server-controlled agent loops using Opure provider credentials;
- do not permit sampling-with-tools;
- and defer sampling until per-request prompt, context, provider, model, token, result-sharing and tool-use approval is implemented.

### Elicitation

- support form-mode elicitation only for non-secret, non-payment, reviewable primitive data;
- show which server is requesting the information;
- validate the restricted schema;
- allow edit, decline and cancel;
- prohibit password, API key, access token, payment credential or other secret collection through form mode;
- support URL-mode elicitation only after displaying the exact HTTPS origin, purpose and server;
- open URLs through the trusted Desktop external-navigation service;
- treat URL elicitation as an out-of-band interaction rather than proof of completion;
- and keep MCP client authorisation separate from URL elicitation.

### Tasks

- do not advertise or accept experimental MCP tasks initially;
- reject task-augmented requests cleanly;
- and defer durable MCP tasks until their protocol stabilises and Opure can reconcile them with its own workflow and checkpoint authority.

### Logging, progress, completion and notifications

- accept bounded server logging as untrusted diagnostics;
- rate-limit logs and progress;
- validate progress tokens;
- keep protocol cancellation separate from process termination;
- treat completions as untrusted suggestions;
- and re-enumerate lists after change notifications before permitting use.

The initial permission model should distinguish:

1. **Server connection permission**
2. **Server authentication permission**
3. **Feature discovery permission**
4. **Tool visibility permission**
5. **Tool invocation permission**
6. **Resource-list permission**
7. **Resource-read permission**
8. **Prompt-use permission**
9. **Elicitation permission**
10. **Local server filesystem permission**
11. **Local server network permission**
12. **Local server secret-environment permission**
13. **Data-to-server transmission permission**
14. **Result-to-model permission**
15. **Project and workflow binding**
16. **Persistent approval scope**

A server being connected does not imply that every tool may be invoked.

A tool being visible does not imply that the model may call it.

A model proposing a call does not imply developer approval.

An OAuth grant does not imply Opure product approval.

A local process being sandboxed does not imply that its responses are trustworthy.

The selected model is:

```text
Explicit server registration
    ↓
Transport and identity verification
    ↓
Sandbox or remote-origin policy
    ↓
MCP lifecycle and capability negotiation
    ↓
Inventory discovery
    ↓
Definition fingerprinting and risk classification
    ↓
Developer and project permission
    ↓
Exact operation plan
    ↓
Human or deterministic policy approval
    ↓
MCP request
    ↓
Bounded and classified result
    ↓
Separate decision to expose result to AI, workflow or project
    ↓
Trust Centre evidence
```

---

## 2. Status

Current status:

> **Proposed**

This ADR becomes **Accepted** only after prototypes demonstrate:

- stable MCP 2025-11-25 negotiation;
- explicit compatibility negotiation for 2025-06-18;
- local `stdio`;
- remote Streamable HTTP;
- legacy transport rejection;
- unsupported custom transport rejection;
- exact local command review;
- no shell execution;
- package-runner rejection;
- local executable fingerprinting;
- publisher evidence;
- unique local server identity;
- AppContainer launch;
- Job Object supervision;
- direct-project access denial;
- direct-network denial;
- explicit local directory grant;
- exact environment-secret binding;
- remote HTTPS origin policy;
- SSRF-resistant OAuth metadata discovery;
- OAuth 2.1 PKCE;
- Client ID Metadata Documents or an approved registration fallback;
- Vault token storage;
- audience validation;
- incremental scopes;
- token refresh and revocation;
- no token passthrough;
- MCP session non-authentication;
- tool inventory fingerprinting;
- tool-schema change invalidation;
- per-call high-risk approval;
- low-risk approval scoping;
- resource provenance and safe rendering;
- prompt provenance and explicit selection;
- roots disabled;
- sampling disabled;
- form and URL elicitation controls;
- tasks disabled;
- bounded logs and progress;
- change-notification revalidation;
- server quarantine;
- connection and operation revocation;
- and an adversarial prompt-injection, schema-confusion, SSRF, token-leakage and local-process test suite.

---

## 3. Context

MCP connects an AI host to external servers that may expose:

- tools;
- resources;
- resource templates;
- prompts;
- logs;
- completions;
- elicitation;
- and requests back to the client.

A server may be:

- a local executable launched by the client;
- a locally listening HTTP process;
- a remote SaaS endpoint;
- a proxy to another API;
- a developer-authored script;
- an enterprise service;
- or a package installed from a public registry.

These forms have different risks.

A local server can potentially access the developer's machine.

A remote server can receive developer data and act on external accounts.

A prompt can contain instructions that attempt to override host policy.

A resource can contain malicious prompt injection.

A tool can mutate or delete data.

A tool annotation can falsely claim that a destructive action is read-only.

An OAuth server can direct the client to malicious metadata URLs.

A server can request more scopes after connection.

A session ID can be stolen or confused with authentication.

A server can change its tools after prior approval.

A tool result can contain a resource link that points somewhere unexpected.

A server can request sampling using the developer's AI provider account.

A server can ask the user for sensitive information.

The MCP specification intentionally leaves much trust and consent enforcement to the host.

Official MCP security guidance explicitly treats local servers like installed software, requires clear consent before local commands, recommends sandboxing, forbids token passthrough, warns about SSRF during OAuth discovery and states that MCP sessions must not be used as authentication.

Opure therefore needs a product trust architecture above the wire protocol.

---

## 4. Problem Statement

Opure requires an MCP server trust and permission model that supports useful local and remote interoperability without turning server registration into arbitrary code execution, connection into blanket tool authority, OAuth into product consent, server content into trusted instructions, or protocol capability negotiation into permission to access projects, providers, secrets or external accounts.

---

## 5. Decision Drivers

The decision is evaluated against:

- Charter alignment;
- human control;
- local-first operation;
- remote-service interoperability;
- protocol compliance;
- MCP evolution;
- exact server identity;
- local-code execution safety;
- remote-origin safety;
- OAuth security;
- secret safety;
- prompt-injection resistance;
- tool safety;
- resource privacy;
- project isolation;
- AI authority;
- workflow integration;
- revocation;
- enterprise policy;
- offline operation;
- small-team implementation;
- and testability.

---

## 6. Governing Principles

This decision must preserve:

- Developer Respect;
- Developer First;
- Human in Control;
- Local by Design;
- Cloud Optional;
- Visible by Design;
- Inspectable Decisions;
- Least Privilege;
- No One-Click Arbitrary Command;
- No Shell String;
- No Trust by Registry Presence;
- No Trust by Tool Annotation;
- No Connection-Implied Invocation;
- No OAuth-Implied Product Consent;
- No Token Passthrough;
- No Session-as-Authentication;
- No Hidden Scope Expansion;
- No Automatic Prompt Authority;
- No Automatic Resource-to-Model Flow;
- No Automatic Tool Chain;
- No Raw Secret in Configuration;
- No Project Root Exposure by Default;
- No Silent Sandbox Fallback;
- No AI Approval;
- Immediate Revocation;
- and Evidence-Based Confidence.

---

## 7. Scope

This ADR decides:

- MCP Gateway authority;
- supported protocol versions;
- transports;
- server registration;
- server identity;
- server trust classes;
- local command policy;
- local sandboxing;
- remote endpoint policy;
- OAuth client behaviour;
- token storage;
- server sessions;
- feature discovery;
- tool permissions;
- resource permissions;
- prompt permissions;
- roots;
- sampling;
- elicitation;
- tasks;
- logging;
- progress;
- completion;
- list-change notifications;
- prompt-injection treatment;
- result classification;
- server updates;
- revocation;
- Trust Centre records;
- persistence;
- recovery;
- and acceptance testing.

This ADR does not decide:

- final MCP SDK package;
- final local MCP package format;
- public Opure MCP catalogue;
- marketplace governance;
- server rating;
- server payment;
- final OAuth browser callback implementation;
- enterprise identity provider integration;
- rich remote UI;
- TUF metadata for MCP packages;
- Linux or macOS process sandbox;
- remote server attestation;
- sampling implementation;
- roots implementation;
- experimental tasks;
- or every individual MCP tool risk rule.

---

## 8. Constraints

Known constraints include:

- MCP messages use JSON-RPC 2.0.
- The latest stable protocol revision identified during drafting is `2025-11-25`.
- Standard transports are `stdio` and Streamable HTTP.
- HTTP+SSE from `2024-11-05` is deprecated.
- Streamable HTTP may use POST, GET and SSE.
- Streamable HTTP servers may provide opaque session IDs.
- Sessions are not authentication.
- HTTP authorisation is optional at protocol level but required by Opure for protected remote servers.
- Current MCP authorisation is based on OAuth 2.1 and protected-resource metadata.
- Access tokens must be sent in the Authorization header and not query strings.
- Tokens must be audience restricted to the MCP resource.
- MCP servers must not accept or transit unrelated downstream tokens.
- OAuth metadata discovery can create SSRF risk.
- MCP local server commands can execute arbitrary code.
- `stdio` servers receive credentials through process environment in common implementations.
- MCP server tools, resources, prompts and annotations are server-controlled.
- Tool annotations are explicitly untrusted unless the server is trusted.
- Tool use requires host consent.
- Resources may contain files, database content or arbitrary text and binary data.
- Prompts are intended to be user controlled.
- Roots currently use `file://` URIs.
- Sampling can request use of the client's LLM and, in `2025-11-25`, may include tools.
- Elicitation form mode must not be used for passwords, API keys, access tokens or payment credentials.
- Tasks introduced in `2025-11-25` are experimental.
- The MCP Registry is in preview.
- The first supported operating system is Windows 11 x64.
- The Opure Runtime, Network Gateway, AI Router, Secrets Vault, Workspace and Trust Centre remain authoritative services.
- and a server protocol response can never be assumed safe solely because it conforms to schema.

---

## 9. Assumptions

This decision assumes:

- Opure can implement or consume an MCP client SDK behind an internal adapter;
- the adapter can enforce message limits before materialising large payloads;
- local `stdio` servers can be launched without a shell;
- compatible local servers can run in an AppContainer or another accepted sandbox profile;
- local full-trust compatibility can remain an explicit high-risk mode;
- remote traffic can route through the Network Gateway;
- a system browser can complete OAuth PKCE;
- OAuth tokens can remain in the Vault;
- tool and resource inventories can be fingerprinted;
- every invocation can pass through an Opure approval planner;
- result content can retain provenance and data classification;
- the AI Router can receive only approved result content;
- and optional MCP client capabilities can be withheld until their product controls exist.

---

## 10. Current Protocol Evidence

Official MCP specification and security guidance available on 18 July 2026 establishes that:

- MCP uses JSON-RPC 2.0.
- The standard transports are `stdio` and Streamable HTTP.
- Streamable HTTP replaced the older HTTP+SSE transport.
- Streamable HTTP security guidance requires Origin validation by servers, localhost binding for local servers and authentication.
- MCP initialisation negotiates protocol versions and optional capabilities.
- Server features include tools, resources and prompts.
- Client features include roots, sampling and elicitation.
- Tools can represent arbitrary code execution and require appropriate caution and explicit consent.
- tool annotations are untrusted unless obtained from a trusted server;
- resources are application-driven and can carry arbitrary context;
- prompts are intended to be user controlled;
- sampling should retain a human in the loop;
- form elicitation must not collect secrets or payment credentials;
- URL elicitation is used for sensitive out-of-band interaction;
- tasks are experimental in the `2025-11-25` revision;
- remote authorisation requires audience-restricted access tokens intended for the MCP resource;
- access tokens must not be sent in query strings;
- token passthrough is forbidden;
- OAuth metadata discovery can create SSRF;
- sessions must not be used as authentication;
- local MCP servers should use clear pre-execution consent and sandboxing;
- and the official Registry is a preview metadata service with namespace and installation information rather than a security endorsement.

All exact SDK versions, schemas, OAuth discovery behaviour and Registry formats must be revalidated before acceptance.

---

## 11. Trust Model

MCP itself assumes a client chooses to trust a server.

Opure introduces additional trust layers.

### 11.1 Protocol Trust

The peer speaks a supported MCP revision and follows JSON-RPC framing.

This says nothing about whether its behaviour is safe.

---

### 11.2 Transport Trust

The local process or remote origin is the one Opure intended to connect to.

This says nothing about whether its tools are appropriate.

---

### 11.3 Publisher or Operator Trust

The local package publisher or remote organisation has an established identity.

This says nothing about whether one tool call is appropriate.

---

### 11.4 Feature Trust

A particular tool, resource or prompt definition has been reviewed and fingerprinted.

This says nothing about a future changed definition.

---

### 11.5 Operation Trust

One exact invocation, resource read or elicitation is permitted under current context.

---

### 11.6 Content Trust

Returned content is validated, classified and approved for a destination.

Even a trusted server may return untrusted or compromised content.

---

# 12. Server Trust Classes

## 12.1 First-Party Built-In

A server or adapter shipped, signed and reviewed as part of Opure.

It still uses explicit capability and operation policy.

---

## 12.2 Verified Local Package

A local MCP server installed from an immutable package whose:

- package hash;
- publisher;
- source;
- version;
- executable;
- arguments;
- and sandbox profile

are verified.

---

## 12.3 Verified Local Executable

An explicitly selected local executable with:

- Authenticode or package signature evidence;
- exact SHA-256;
- fixed command vector;
- and reviewed sandbox policy.

---

## 12.4 Sandboxed Local Unverified

An unsigned or untrusted local executable permitted only after explicit review and successful sandbox launch.

No automatic invocation or secret disclosure.

---

## 12.5 Trusted Remote Organisation

A remote server whose:

- organisation;
- domain;
- TLS;
- OAuth issuer;
- protected-resource metadata;
- and operational policy

are explicitly trusted.

Tool calls remain separately approved.

---

## 12.6 Authenticated Remote

A remote server with valid TLS and OAuth but without a separately trusted operator classification.

---

## 12.7 Registry Discovered

A server found through the MCP Registry or another catalogue.

No connection, installation or execution trust is implied.

---

## 12.8 Local Legacy Full Trust

A local server intentionally run without a production sandbox.

High risk.

Disabled by default.

---

## 12.9 Quarantined

Connection and execution denied while evidence is preserved.

---

## 12.10 Revoked or Withdrawn

Known trust has been removed.

No automatic reconnection or invocation.

---

# 13. Options Considered

The principal architecture options are:

1. Trusted MCP Gateway with feature-level mediation.
2. Direct MCP client access from AI Router.
3. Direct MCP client access from Desktop.
4. Treat MCP servers as plugins.
5. Trust server-provided annotations.
6. One approval when connecting a server.
7. Remote-only MCP.
8. Local-only MCP.
9. Full protocol capability support immediately.
10. Registry-driven one-click installation.

---

# 14. Option A — Trusted MCP Gateway

## 14.1 Advantages

- central protocol validation;
- central identity and transport policy;
- Network Gateway integration;
- Vault token handling;
- tool approval;
- content classification;
- prompt-injection treatment;
- cancellation;
- revocation;
- Trust Centre evidence;
- and one policy boundary for AI, workflow and UI clients.

---

## 14.2 Disadvantages

- additional service complexity;
- more IPC;
- protocol-version maintenance;
- and potential throughput bottleneck.

---

## 14.3 Decision

Selected.

---

# 15. Direct AI Router MCP Access

Rejected because model orchestration must not own server credentials, local process launch or tool approval.

---

# 16. Direct Desktop MCP Access

Rejected because UI state is non-authoritative and should not hold OAuth tokens or execute tools.

---

# 17. Treat MCP Servers as Plugins

Rejected as a universal model.

Local MCP servers and Opure plugins may use similar package and sandbox infrastructure, but MCP servers can be remote services and expose a protocol surface different from the Plugin SDK.

---

# 18. Trust Server Annotations

Rejected because the official specification treats tool annotations as untrusted unless the server itself is trusted, and even a trusted server may be compromised or incorrect.

---

# 19. One Connection Approval

Rejected because connection permission cannot safely imply every future tool, resource, prompt, scope or changed server definition.

---

# 20. Remote-Only MCP

Rejected because local developer tools and private integrations are a primary MCP use case.

---

# 21. Local-Only MCP

Rejected because authenticated remote services are a valid interoperability model.

---

# 22. Full Capability Support Immediately

Rejected because roots, sampling-with-tools and experimental tasks require additional authority and user-control design.

---

# 23. Registry One-Click Installation

Rejected because the Registry is discovery metadata and local server installation can execute arbitrary code.

---

# 24. Decision

Opure will provisionally adopt:

> **A trusted MCP Gateway that registers and verifies exact local or remote server identities, negotiates only supported protocol capabilities, mediates every tool, resource, prompt, authentication and data-flow decision through Opure policy and approval services, sandboxes local server execution where supported, and treats all server-provided content as untrusted data rather than authority.**

This decision is:

- [ ] Permanent until superseded
- [x] Provisional pending transport, OAuth, sandbox and consent evidence
- [ ] Approval of roots
- [ ] Approval of sampling
- [ ] Approval of experimental tasks
- [ ] Marketplace or Registry endorsement

---

# 25. MCP Gateway Ownership

The MCP Gateway owns:

- server registration;
- server identity;
- protocol negotiation;
- transport lifecycle;
- OAuth coordination;
- token references;
- capability inventory;
- tool definition fingerprints;
- resource and prompt metadata;
- operation planning;
- request dispatch;
- response validation;
- cancellation;
- session termination;
- revocation;
- and Trust Centre projections.

It does not own:

- user source files;
- patch application;
- provider credentials;
- model routing;
- the Secrets Vault;
- Network Gateway policy;
- local package installation;
- or workflow authorisation.

Those remain service owned.

---

# 26. Internal Architecture

Conceptual components:

```text
Opure.McpGateway
├── Server Registry
├── Trust Classifier
├── Protocol Adapter
├── Transport Manager
│   ├── Stdio Transport
│   └── Streamable HTTP Transport
├── OAuth Coordinator
├── Session Manager
├── Capability Inventory
├── Definition Fingerprinter
├── Tool Policy Engine
├── Resource Policy Engine
├── Prompt Policy Engine
├── Elicitation Controller
├── Result Classifier
├── Local Server Supervisor Adapter
└── Trust Centre Projector
```

---

# 27. Internal Consumers

Trusted internal consumers may include:

- Desktop application services;
- Workflow Engine;
- AI Router;
- Project Service;
- Trust Centre;
- and explicit first-party tools.

No consumer receives a raw MCP transport object.

---

# 28. Server Registration

A server must be registered before discovery or use.

A registration is not an approval to invoke tools.

---

## 28.1 Registration Fields

Conceptual fields:

```text
server_id
display_name
server_kind
transport
protocol_profile
trust_class
source
publisher_identity
operator_identity
endpoint_or_executable
arguments
working_directory
environment_bindings
sandbox_profile
oauth_profile
project_binding
enabled
auto_start
created_by
created_at
last_reviewed_at
```

---

## 28.2 Stable Server ID

Opure assigns an opaque stable server ID.

Display name and MCP `serverInfo.name` are not security identities.

---

## 28.3 Registration Source

Source may be:

- first-party configuration;
- explicit local executable;
- verified local package;
- explicit remote URL;
- enterprise policy;
- Registry metadata import;
- or Development configuration.

---

## 28.4 No Configuration-by-Prompt

AI output, repository text, README content or an MCP server response cannot register another MCP server automatically.

---

## 28.5 No Plugin Registration Authority

An Opure plugin cannot register or enable an MCP server without a separately approved trusted operation.

---

# 29. Server Identity

Server identity combines registration evidence and live protocol evidence.

---

## 29.1 Local Identity

Local identity includes:

```text
canonical executable path
executable SHA-256
file version
Authenticode or package signature
publisher certificate
package identity where applicable
argument vector hash
working-directory policy
environment-binding schema
sandbox identity
source
```

---

## 29.2 Remote Identity

Remote identity includes:

```text
canonical scheme
canonical host
effective port
endpoint path
TLS identity
protected resource identifier
authorisation server
operator identity
OAuth client registration
source
```

---

## 29.3 MCP-Reported Identity

`serverInfo`, instructions and negotiated capabilities are live metadata.

They do not override registered identity.

---

## 29.4 Identity Change

A material identity change:

- ends the session;
- revokes approvals;
- suspends tokens where appropriate;
- invalidates tool and resource fingerprints;
- and requires review.

---

# 30. Registry Discovery

The official MCP Registry may be queried only after the developer explicitly enables Registry discovery.

---

## 30.1 Preview Status

The Registry is currently preview and may change or reset.

Opure must not make it an availability or trust dependency.

---

## 30.2 Registry Metadata

Registry metadata may provide:

- namespace;
- display information;
- package or remote configuration;
- repository;
- version;
- and installation hints.

---

## 30.3 No Trust Elevation

Registry presence does not establish:

- publisher authenticity beyond the Registry's stated namespace mechanism;
- package signature;
- executable safety;
- code review;
- permission safety;
- operator security;
- or Opure endorsement.

---

## 30.4 Imported Configuration

Before importing Registry configuration, show:

- exact source;
- package or endpoint;
- exact local command;
- arguments;
- environment variables;
- requested directories;
- network requirements;
- and trust status.

---

# 31. Protocol Versions

Initial supported profiles:

```text
MCP Stable:          2025-11-25
MCP Compatibility:   2025-06-18
```

---

## 31.1 Stable Default

New registrations request and prefer `2025-11-25`.

---

## 31.2 Compatibility Profile

`2025-06-18` is permitted only when:

- the server does not support the stable revision;
- required features are compatible;
- the developer accepts the compatibility state;
- and acceptance tests cover the server.

---

## 31.3 Older Revisions

Older revisions, including `2024-11-05`, are unsupported initially except for an explicit diagnostic compatibility experiment.

---

## 31.4 Draft Features

Draft protocol features are disabled in Stable.

Development mode may enable one exact draft feature behind a named experiment.

---

## 31.5 Version Mismatch

If the server selects an unsupported revision, disconnect.

Do not guess protocol semantics.

---

# 32. Capability Negotiation

The MCP Gateway declares only implemented and approved client capabilities.

---

## 32.1 Initial Client Capabilities

Initial intended capability advertisement:

```text
elicitation.form
elicitation.url
```

after their implementation and tests.

The Gateway does not initially advertise:

```text
roots
sampling
sampling.tools
sampling.context
tasks
experimental
```

---

## 32.2 Server Capabilities

The Gateway may accept stable server capabilities for:

- tools;
- resources;
- prompts;
- logging;
- completions;
- and list-change notifications.

Resource subscriptions are disabled initially.

Task support is disabled.

---

## 32.3 Unknown Capabilities

Unknown or experimental capabilities are recorded but not enabled.

---

## 32.4 Server Instructions

`instructions` returned during initialisation is untrusted descriptive content.

It is not inserted automatically into:

- system prompts;
- developer prompts;
- workflow policy;
- or security policy.

---

# 33. JSON-RPC Validation

Every MCP message must:

- use JSON-RPC `2.0`;
- use a valid request, response, error or notification shape;
- use a permitted method;
- use a correctly typed ID;
- remain within limits;
- and correspond to the session state.

---

## 33.1 Duplicate Properties

Reject JSON with duplicate critical properties.

---

## 33.2 Unknown Fields

Preserve or ignore unknown optional fields according to protocol compatibility.

Unknown authority-affecting fields are not acted upon.

---

## 33.3 Message Limits

Suggested initial limits:

- request or response JSON: 8 MiB;
- normal metadata message: 1 MiB;
- nesting depth: 64;
- array elements: 100,000 only for approved paged operations, otherwise lower;
- string length: 4 MiB;
- tool definition: 256 KiB;
- one schema: 512 KiB;
- and total inventory: 32 MiB per server.

Exact limits require prototype evidence.

---

## 33.4 Binary Content

Base64 content is decoded only after:

- declared size check;
- encoded-size limit;
- MIME validation;
- and destination policy.

---

# 34. Transport Policy

Supported:

- `stdio`
- Streamable HTTP

Unsupported initially:

- custom transport;
- WebSocket;
- raw TCP;
- arbitrary named-pipe MCP;
- and deprecated HTTP+SSE by default.

---

# 35. Local `stdio` Transport

For `stdio`:

- Opure launches the server;
- client writes only valid MCP messages to `stdin`;
- server writes only valid MCP messages to `stdout`;
- server may write bounded UTF-8 diagnostics to `stderr`;
- process lifetime belongs to Process Supervisor;
- and process exit terminates the MCP session.

---

## 35.1 No Shell

Use an executable path and argument array.

Do not use:

```text
cmd.exe /c
powershell.exe -Command
pwsh -Command
bash -c
wsl.exe <shell command>
```

as a production registration mechanism.

---

## 35.2 Argument Display

Every argument is displayed without truncation during registration.

Sensitive values are shown as Vault references, not plaintext.

---

## 35.3 Protocol Purity

Any non-JSON-RPC output on `stdout` is a protocol violation.

Bounded tolerance for a UTF-8 BOM or implementation-specific startup noise is not allowed by default.

A compatibility exception must be server specific.

---

## 35.4 `stderr`

Treat `stderr` as untrusted diagnostics.

Apply:

- rate limit;
- byte limit;
- redaction;
- line-length limit;
- and retention.

---

# 36. Local Command Policy

Allowed production forms:

1. exact signed executable;
2. exact executable from a verified immutable package;
3. first-party executable;
4. approved runtime host plus immutable verified application entry file.

---

## 36.1 Interpreted Servers

An interpreted server requires exact:

- interpreter executable;
- interpreter hash and publisher;
- script path;
- script hash;
- arguments;
- working directory;
- dependency environment;
- and sandbox.

---

## 36.2 Package Runners

Commands such as:

```text
npx
npm exec
uvx
pipx run
dotnet tool run
cargo install and run
```

are prohibited in production registration when they may:

- resolve a mutable version;
- download code;
- execute install scripts;
- alter caches;
- or change dependencies at launch.

---

## 36.3 Development Exception

Development Mode may allow an exact runner command after displaying:

- every argument;
- expected package and version;
- network behaviour;
- cache behaviour;
- installation scripts;
- filesystem authority;
- and same-user execution warning.

The execution is never classified as verified solely because the command is common.

---

## 36.4 Git Checkout

Do not launch a server directly from a mutable Git checkout in production mode.

---

# 37. Local Server Working Directory

The registration declares a working-directory policy.

Allowed examples:

- server package directory, read only;
- server-specific data directory;
- server-specific temporary directory;
- or explicit approved directory.

Do not default to:

- Opure repository root;
- active project root;
- user profile;
- Desktop;
- Downloads;
- or current process directory.

---

# 38. Local Server Environment

Construct a minimal environment.

---

## 38.1 Safe Variables

May include:

- system runtime paths;
- locale;
- temporary directory;
- server registration ID;
- safe logging level;
- and explicitly declared configuration.

---

## 38.2 Removed Variables

Remove:

- provider keys;
- Git credentials;
- Azure tokens;
- package-signing credentials;
- user secret environment variables;
- Opure Vault keys;
- full parent `PATH` where not required;
- and unrelated product configuration.

---

## 38.3 Environment Secret Binding

A local server may require a secret in one exact environment variable.

This is a high-risk disclosure because the server receives plaintext.

Approval must show:

- server identity;
- executable hash;
- publisher;
- variable name;
- secret label;
- purpose;
- process lifetime;
- and whether child processes are prohibited.

The value is injected at process creation and never stored in registration JSON, logs or command arguments.

---

## 38.4 Preferred Authentication

Prefer:

- OAuth;
- URL elicitation;
- OS-integrated login;
- or destination-bound broker use

over raw environment-secret disclosure.

---

# 39. Local Server Sandboxing

Third-party production local servers should run in a Windows AppContainer when compatible.

---

## 39.1 Unique Sandbox Identity

Bind sandbox identity to:

- channel;
- server registration;
- publisher trust identity;
- executable or package identity;
- and sandbox schema.

---

## 39.2 Filesystem

Grant only explicit server package, data, temporary and approved integration directories.

---

## 39.3 Project Roots

Do not grant project root access by default.

A server requiring direct repository access must request an explicit directory grant and receives a high-risk warning.

Preferred future design is an ephemeral exported workspace.

---

## 39.4 Network

Grant no network capability by default.

A server requiring remote network receives:

- a separately declared network profile;
- exact intended destinations;
- and a warning that AppContainer network capability may be broader than host-level intent.

Where technically practical, use an Opure-controlled egress proxy and block direct network.

---

## 39.5 Job Object

Every local server process belongs to a non-breakaway Job Object with:

- kill-on-close;
- process-count limit;
- memory limits;
- CPU policy;
- lifetime;
- and accounting.

---

## 39.6 Child Processes

Deny child process creation by default.

A server requiring subprocesses needs an explicit server-specific profile and cannot be classified as minimally sandboxed.

---

## 39.7 No Silent Fallback

Sandbox launch failure blocks the server.

---

# 40. Legacy Full Trust Mode

Legacy Full Trust exists for compatibility, not safety.

---

## 40.1 Disabled by Default

The user must enable it in advanced settings.

---

## 40.2 Required Warning

The UI states:

> This local MCP server will run as your Windows account and may access files, network services, credentials and processes outside Opure's control. MCP tool approvals do not prevent the server process from using its operating-system access directly.

---

## 40.3 Restrictions

Legacy Full Trust should:

- require manual start;
- forbid auto-start by default;
- forbid silent high-risk tool invocation;
- forbid persistent environment-secret disclosure by default;
- show every directory and network assumption;
- and isolate data and logs where practical.

---

# 41. Local HTTP Servers

Local Streamable HTTP is disabled by default.

---

## 41.1 Risks

Risks include:

- DNS rebinding;
- unauthorised local processes;
- browser-origin access;
- stale listeners;
- port collision;
- and ambiguous ownership.

---

## 41.2 Development Exception

An explicit development profile may allow:

```text
https://localhost:<port>/mcp
```

or loopback HTTP where protocol and OAuth development rules permit.

It requires:

- exact loopback address;
- server authentication;
- random or fixed reviewed port;
- Origin policy;
- no wildcard bind;
- and lifecycle ownership.

---

## 41.3 Bind Address

A local MCP server must not bind `0.0.0.0` or an external interface under an Opure-managed profile.

---

# 42. Remote Streamable HTTP

Remote servers must use:

```text
https://
```

except explicit loopback development.

---

## 42.1 Canonical Endpoint

Registration stores:

- scheme;
- canonical host;
- effective port;
- exact path;
- and expected protected-resource identifier.

---

## 42.2 Network Gateway

Every request, including:

- MCP POST;
- MCP GET;
- OAuth metadata;
- token exchange;
- OIDC discovery;
- icons;
- and optional remote metadata

uses Network Gateway policy.

---

## 42.3 Redirects

Default:

- no cross-origin redirect for MCP messages;
- OAuth metadata redirects validated hop by hop;
- HTTPS only;
- maximum three;
- no credentials forwarded to another origin;
- and private or reserved destinations blocked.

---

## 42.4 Origin Header

Opure is a native client and does not depend on browser Origin semantics.

If an Origin header is sent, it must be stable and honest.

The server's Origin validation remains its responsibility.

---

# 43. Legacy HTTP+SSE

Legacy HTTP+SSE is disabled.

---

## 43.1 Compatibility Mode

A server-specific compatibility profile may attempt the documented fallback only after:

- explicit developer approval;
- remote HTTPS validation;
- transport downgrade warning;
- security tests;
- and no modern Streamable HTTP support.

---

## 43.2 No Silent Fallback

A failed Streamable HTTP attempt does not automatically downgrade without policy.

---

# 44. Streamable HTTP Sessions

A server may return `Mcp-Session-Id`.

---

## 44.1 Opaque State

Treat it as opaque.

---

## 44.2 Not Authentication

Every HTTP request still includes valid authorisation where required.

---

## 44.3 Storage

Keep session IDs in memory where practical.

Persist only a safe hash for diagnostics.

---

## 44.4 Binding

Bind session state to:

- server registration;
- endpoint;
- authenticated account;
- OAuth token set;
- Runtime instance;
- and protocol revision.

---

## 44.5 Termination

Send HTTP DELETE when the server supports explicit termination.

---

## 44.6 Reconnect

HTTP 404 for a session starts a fresh initialisation only after transport and auth remain valid.

Do not reuse old approvals for changed definitions without revalidation.

---

# 45. Server Lifecycle

States include:

- Registered;
- Disabled;
- Starting;
- Connecting;
- Authenticating;
- Initialising;
- Inventory Pending;
- Ready;
- Approval Required;
- Degraded;
- Disconnected;
- Faulted;
- Quarantined;
- Revoked;
- and Removed.

---

## 45.1 Start

Starting a local server is a separate action from connecting to a remote server.

---

## 45.2 Auto-Start

Auto-start is disabled by default for third-party local servers.

A verified sandboxed server may receive per-profile auto-start approval.

Legacy Full Trust may not auto-start initially.

---

## 45.3 Idle Stop

Local servers may stop after a bounded idle period.

---

## 45.4 Remote Idle

Close remote streams and sessions when unused.

---

# 46. OAuth Applicability

MCP OAuth applies to HTTP-based transports.

It is not applied to `stdio`.

---

## 46.1 Stdio Credentials

A local stdio server may receive explicitly approved configuration or environment secret bindings.

These are not MCP OAuth tokens unless the local server independently runs an OAuth flow.

---

# 47. OAuth Client Model

Opure acts as a public OAuth client by default.

---

## 47.1 No Embedded Client Secret

A desktop application cannot safely keep a universal confidential-client secret.

Do not ship one in code or package assets.

---

## 47.2 Server-Specific Confidential Client

An enterprise may register a confidential-client integration whose secret is stored in Vault and used by a trusted broker.

This requires separate administrative policy.

---

# 48. OAuth Discovery

Follow the supported MCP authorisation specification.

Potential discovery includes:

- Protected Resource Metadata;
- Authorization Server Metadata;
- OpenID Connect Discovery;
- and Client ID Metadata Documents.

---

## 48.1 Discovery Through Network Gateway

Every discovered URL is treated as attacker controlled until validated.

---

## 48.2 SSRF Protection

Block by default:

- private IPv4;
- private IPv6;
- loopback;
- link-local;
- cloud metadata;
- multicast;
- unspecified addresses;
- and non-HTTPS schemes.

Explicit loopback development is separate.

---

## 48.3 DNS

Validate resolved addresses and protect against DNS rebinding.

Where practical:

- pin resolution for the request;
- validate connection destination;
- and use egress policy.

---

## 48.4 Redirect Validation

Validate every redirect target with the same SSRF policy.

---

## 48.5 Error Redaction

Do not reflect internal response bodies or network topology into server-visible errors.

---

# 49. OAuth Authorisation Flow

For user-delegated remote access:

1. discover protected resource and authorisation metadata;
2. validate origins and endpoints;
3. determine client registration;
4. generate PKCE verifier and challenge;
5. generate state and nonce where applicable;
6. show server, operator, scopes and account context;
7. open system browser;
8. receive exact callback;
9. validate state;
10. exchange code through Network Gateway;
11. validate token response;
12. store tokens in Vault;
13. initialise MCP with access token;
14. and record safe trust evidence.

---

## 49.1 Browser

Use the system browser.

Do not embed remote login pages inside privileged Desktop web content initially.

---

## 49.2 Redirect URI

Preferred desktop callback:

- loopback redirect with random high port;
- exact state binding;
- short lifetime;
- and one-shot listener.

A claimed HTTPS redirect may be added when deployment and registration are practical.

---

## 49.3 PKCE

PKCE is mandatory.

---

## 49.4 State

State is:

- cryptographically random;
- server-side bound;
- one shot;
- short lived;
- and deleted after validation.

---

# 50. Client Registration

Preferred order:

1. Client ID Metadata Document;
2. pre-registered public client ID;
3. approved Dynamic Client Registration;
4. explicit administrator-supplied client ID;
5. unsupported.

---

## 50.1 Dynamic Client Registration

DCR is not automatic for every server.

Before DCR:

- validate registration endpoint;
- show operator;
- validate redirect URI rules;
- record issued client ID;
- and limit metadata.

---

## 50.2 Client Secret from DCR

If a public desktop registration returns a secret, do not assume it provides confidentiality.

Store it in Vault if required, but maintain public-client threat assumptions.

---

# 51. OAuth Scopes

Request the minimum scope required.

---

## 51.1 No Omnibus Scope

Do not request every advertised scope by default.

---

## 51.2 Incremental Consent

When a tool requires a new scope:

- show exact scope;
- explain the tool and effect;
- identify the remote account;
- perform incremental authorisation;
- and invalidate the tool plan if scope changes.

---

## 51.3 Scope Mapping

Map OAuth scopes to MCP server tools and resources where evidence permits.

A token scope does not replace Opure tool approval.

---

# 52. Token Requirements

Access tokens:

- use the Authorization header;
- are never placed in URI query strings;
- are sent only to the intended MCP resource;
- and are validated for audience, issuer, expiry and required properties where token format permits.

---

## 52.1 Opaque Tokens

Opaque tokens are handled according to the server's authorisation design.

Opure still binds the stored token to the expected resource and issuer.

---

## 52.2 Token Passthrough

Opure does not give a token intended for another API to an MCP server.

An MCP server must obtain and manage its own downstream authorisation according to the protocol and its operator design.

---

## 52.3 No Model Exposure

Tokens never enter:

- model prompts;
- context;
- tool arguments;
- server-visible diagnostic text;
- or AI memory.

---

# 53. Token Storage

Tokens are Secrets Vault records.

Store safe metadata separately:

- server ID;
- issuer;
- resource;
- account label;
- scope set;
- created time;
- expiry;
- refresh state;
- and last use.

---

## 53.1 Token Handles

MCP Gateway receives a Vault handle.

The Network Gateway injects the token at the final request boundary.

---

## 53.2 Refresh

Refresh tokens remain in Vault.

Refresh is:

- server bound;
- scope bound;
- cancellable;
- redacted;
- and recorded without values.

---

## 53.3 Logout

Disconnect may optionally preserve the account grant.

Explicit Sign Out revokes or deletes the local token set and attempts remote revocation where supported.

---

# 54. Account Binding

A remote server registration may have multiple accounts.

Every session and operation binds one account.

The UI must show which account will be used before a material tool call.

---

# 55. OAuth Scope Challenge

A `WWW-Authenticate` incremental-scope challenge is treated as a request for more remote authority.

It cannot be satisfied silently.

---

## 55.1 Challenge Validation

Validate:

- protected resource;
- authorisation server;
- requested scopes;
- current tool operation;
- and server identity.

---

## 55.2 User Review

Show the exact change.

---

# 56. OAuth Revocation and Failure

On:

- token expiry;
- refresh failure;
- account removal;
- issuer change;
- scope reduction;
- server identity change;
- or user sign-out,

end or degrade the session and require reauthorisation.

---

# 57. OAuth Trust Limitations

Successful OAuth proves that:

- the user or administrator authorised a client to access a resource under scopes;
- and the server accepted the resulting token.

It does not prove:

- tool safety;
- data accuracy;
- operator benevolence;
- server code security;
- or appropriateness of a specific action.

---

# 58. Permission Layers

The MCP permission model separates:

1. server registration;
2. server connection;
3. authentication;
4. feature discovery;
5. feature visibility;
6. operation approval;
7. data transmission;
8. result use;
9. persistence;
10. and policy override.

Each layer may deny or narrow.

---

# 59. Server Connection Permission

A connection permission binds:

- server ID;
- trust class;
- transport;
- endpoint or executable;
- project or profile;
- account;
- protocol profile;
- and duration.

It does not permit tool invocation.

---

# 60. Feature Discovery Permission

Discovery allows the Gateway to request:

- tools list;
- resources list;
- prompts list;
- and supported completions.

It does not expose project data or invoke a tool.

---

# 61. Feature Fingerprints

Every discovered feature receives a canonical fingerprint.

---

## 61.1 Tool Fingerprint

Includes:

- server identity;
- tool name;
- title;
- description;
- input schema;
- output schema;
- annotations;
- execution properties;
- and protocol revision.

---

## 61.2 Resource Fingerprint

Includes:

- server identity;
- resource URI;
- name;
- description;
- MIME type;
- annotations;
- size where known;
- and template definition.

---

## 61.3 Prompt Fingerprint

Includes:

- server identity;
- prompt name;
- title;
- description;
- argument definition;
- icons metadata;
- and returned prompt definition when retrieved.

---

## 61.4 Material Change

A material change invalidates:

- saved approval;
- automation eligibility;
- cached risk classification;
- and model tool projection.

---

# 62. Inventory Changes

When a server sends:

- `notifications/tools/list_changed`;
- `notifications/resources/list_changed`;
- or `notifications/prompts/list_changed`,

the Gateway re-enumerates the relevant list.

---

## 62.1 No Delta Trust

The notification itself grants no new feature.

---

## 62.2 Removed Feature

Remove it from active projections and cancel queued use.

---

## 62.3 Changed Feature

Require reclassification and approval.

---

# 63. Tool Policy

Tools are arbitrary external actions.

---

## 63.1 Tool Visibility

A tool may be:

- hidden;
- visible to the developer only;
- visible to deterministic workflows;
- visible to the AI Router;
- or disabled.

Visibility does not imply invocation approval.

---

## 63.2 Default

New tools are:

```text
Visible to developer
Not visible to AI
Invocation requires approval
```

unless policy says otherwise.

---

## 63.3 Tool Names

Use server ID plus case-sensitive tool name as logical identity.

Display title is not identity.

---

## 63.4 Duplicate Names

Duplicate names within one server inventory are invalid.

---

# 64. Tool Risk Classification

Opure classifies a tool using:

- server trust;
- name;
- description;
- input schema;
- output schema;
- annotations;
- observed behaviour;
- OAuth scopes;
- declared side effects;
- data transmitted;
- and administrator policy.

Server annotations are advisory only.

---

## 64.1 Initial Risk Classes

- **T0 — Metadata Read**
- **T1 — External Read**
- **T2 — Bounded Compute**
- **T3 — External Write**
- **T4 — Destructive or Irreversible**
- **T5 — Code or Command Execution**
- **T6 — Financial, Identity or Security Administration**
- **TX — Unknown**

---

## 64.2 Unknown

Unknown defaults to per-call approval and no AI auto-invocation.

---

## 64.3 Read-Only Claim

A `readOnlyHint` or equivalent annotation does not determine classification alone.

---

## 64.4 Idempotent Claim

An idempotent annotation does not mean harmless.

---

# 65. Tool Approval Policy

Default:

- T0 may receive narrowly persistent approval;
- T1 may receive project or account-specific persistent approval;
- T2 requires operation review when data or cost is material;
- T3 requires per-call approval;
- T4 requires per-call high-risk approval;
- T5 requires per-call approval and may be prohibited;
- T6 requires per-call approval and may be prohibited;
- TX requires per-call approval.

---

## 65.1 Exact Approval Scope

A persistent tool approval binds:

- server identity;
- tool fingerprint;
- account;
- project;
- argument constraints;
- data classes;
- result destination;
- and expiry.

---

## 65.2 No Server-Wide Allow

Initial UI does not offer:

```text
Always allow every tool from this server
```

---

## 65.3 AI Authority

AI may construct candidate arguments.

It cannot approve.

---

## 65.4 Workflow Authority

A deterministic workflow may use a preapproved exact tool policy.

A workflow definition cannot widen it.

---

# 66. Tool Operation Plan

Before invocation, construct a plan containing:

```text
server
account
tool
tool_fingerprint
arguments
argument_summary
project
input_data_classes
external_side_effects
OAuth_scopes
cost
timeout
result_destination
follow_up_permissions
```

---

## 66.1 Plan Hash

Canonicalise and hash the plan.

---

## 66.2 Change

Any material argument, data, account, scope or tool-definition change invalidates approval.

---

# 67. Tool Argument Validation

Validate arguments against:

1. MCP input schema;
2. JSON Schema safety policy;
3. Opure tool policy;
4. project and account policy;
5. and operation-specific constraints.

---

## 67.1 JSON Schema Dialect

Use the protocol's selected JSON Schema dialect, currently defaulting to 2020-12 when unspecified.

---

## 67.2 Schema Limits

Limit:

- nesting;
- references;
- regex complexity;
- number of properties;
- enum count;
- and total schema size.

---

## 67.3 Remote References

Do not fetch remote `$ref` schemas automatically.

---

## 67.4 Additional Properties

Where safety requires exact input, reject undeclared fields even if the server schema allows them broadly.

---

## 67.5 Secrets

A model or plugin cannot insert raw Vault values into tool arguments.

Use a separately approved server authentication or secret-use mechanism.

---

# 68. Tool Invocation

Invocation uses one exact authenticated MCP session.

---

## 68.1 Timeout

Every call has a timeout.

---

## 68.2 Cancellation

Support MCP cancellation where available.

Process or session termination remains the final local boundary.

---

## 68.3 Progress

Progress notifications are:

- correlated;
- bounded;
- rate limited;
- and untrusted display text.

---

## 68.4 Retry

Do not automatically retry a mutating tool unless:

- idempotency is independently established;
- operation identity is stable;
- and policy permits.

---

# 69. Tool Result Validation

Tool output is untrusted.

---

## 69.1 Structured Output

If `outputSchema` exists:

- validate `structuredContent`;
- reject or flag non-conformance;
- and preserve raw bounded evidence for diagnostics.

---

## 69.2 Unstructured Output

Text, image and audio content receive:

- MIME checks;
- size limits;
- data classification;
- provenance;
- and safe rendering.

---

## 69.3 Embedded Resource

An embedded resource is treated as server-supplied content.

It does not gain extra trust.

---

## 69.4 Resource Link

A returned resource link is not dereferenced automatically.

It becomes a separate resource-read proposal.

---

## 69.5 Active Content

Do not execute:

- HTML script;
- SVG script;
- embedded executable;
- macro;
- shell instruction;
- or external resource load.

---

# 70. Tool Result Destination

A tool result may be sent to:

- user display;
- workflow state;
- AI context;
- project memory;
- patch proposal;
- or another explicit service.

Each destination requires separate policy.

---

## 70.1 No Automatic Model Feed

A result is not automatically added to model context merely because a model requested the tool.

The approved tool plan may authorise a bounded result return to that model operation.

---

## 70.2 No Automatic Memory

A result is not automatically persisted to project memory.

---

## 70.3 No Automatic Chaining

Tool output cannot invoke another tool without a new mediated operation.

---

# 71. Tool Prompt Injection

Tool output may contain instructions such as:

- ignore previous rules;
- call another tool;
- reveal secrets;
- upload files;
- or change policy.

Treat them as untrusted content.

---

## 71.1 Instruction Hierarchy

MCP content never becomes an Opure system or developer instruction automatically.

---

## 71.2 Model Labelling

When approved content is provided to a model, include provenance and an untrusted-data boundary.

---

## 71.3 Deterministic Controls

Prompt-injection detection is advisory.

Security does not rely solely on a classifier.

Tool and data permissions remain deterministic.

---

# 72. Resources

Resources are server-hosted data identified by URI.

---

## 72.1 Resource Listing

Listing requires feature discovery permission.

---

## 72.2 Pagination

Enforce pagination and inventory limits.

---

## 72.3 Resource URI

Treat URI as opaque server identity data subject to syntax and scheme policy.

Do not let a `file://` URI from a remote server cause local filesystem access.

---

## 72.4 URI Schemes

A server may use custom URI schemes.

The client sends the URI back only to that server through `resources/read`.

It does not resolve the URI through Windows or Network Gateway unless separately approved.

---

# 73. Resource Read Permission

A read plan binds:

- server;
- resource fingerprint;
- exact URI;
- account;
- project;
- expected MIME;
- maximum bytes;
- destination;
- and subscription state.

---

## 73.1 User Selection

Default resource reads are user selected.

---

## 73.2 Workflow Rule

A deterministic workflow may read a preapproved resource pattern from an exact trusted server.

---

## 73.3 Model Selection

An AI may suggest a resource.

It cannot approve the read or its external transmission.

---

# 74. Resource Content Validation

Validate:

- exact returned URI;
- MIME type;
- text or blob form;
- size;
- encoding;
- binary type;
- and content destination.

---

## 74.1 URI Mismatch

If the server returns a materially different URI, reject or require review.

---

## 74.2 MIME Mismatch

Do not trust declared MIME alone.

Inspect magic bytes where practical.

---

## 74.3 Text

Decode using explicit or safe default encoding.

---

## 74.4 Blob

Base64 decode within limits.

---

# 75. Resource Provenance

Every resource content object retains:

- server ID;
- server trust class;
- account;
- URI;
- resource fingerprint;
- fetch time;
- content hash;
- MIME;
- and validation result.

---

# 76. Resource Rendering

Render:

- plain text safely;
- Markdown with active content disabled;
- images through safe decoders;
- audio through trusted media paths;
- and unknown binary as download/export only after review.

---

## 76.1 Remote Images

Do not fetch image URLs referenced inside text or Markdown automatically.

---

## 76.2 Links

Display destination before opening.

---

# 77. Resource-to-AI Flow

Before a resource enters AI context, show or record:

- server;
- URI;
- data class;
- content size;
- model destination;
- provider;
- and cloud policy.

Remote server data may still contain prompt injection.

---

# 78. Resource Subscriptions

Disabled initially.

Reasons:

- asynchronous data arrival;
- hidden bandwidth;
- changing content;
- stale approval;
- and prompt-injection risk.

A future subscription ADR or amendment must define:

- consent;
- update rate;
- content diff;
- reapproval;
- cancellation;
- and background operation.

---

# 79. Resource Templates

Templates are untrusted parameterised definitions.

---

## 79.1 Argument Completion

Completion suggestions do not grant permission.

---

## 79.2 Template Expansion

Validate exact generated URI and parameters before read.

---

## 79.3 Remote Schema

Do not execute remote template logic outside the MCP request.

---

# 80. Prompts

MCP prompts are user-controlled templates supplied by a server.

---

## 80.1 Visibility

New prompts are visible to the developer but not automatically active.

---

## 80.2 Invocation

Prompt selection must be an explicit user action or a preapproved deterministic command.

---

## 80.3 No Automatic System Prompt

Prompt content never enters the system or developer instruction layer automatically.

---

## 80.4 Role Preservation

Server-supplied role labels are treated as prompt content structure, not product authority.

---

# 81. Prompt Retrieval Plan

A plan binds:

- server;
- prompt fingerprint;
- arguments;
- account;
- project;
- expected data classes;
- embedded resources;
- intended model or display destination;
- and approval.

---

## 81.1 Arguments

Validate and display arguments before retrieval when they include project or personal data.

---

## 81.2 Returned Prompt

Show:

- exact messages;
- roles;
- embedded resources;
- server;
- and content classifications.

---

# 82. Prompt Injection and Policy

An MCP prompt may ask the model to ignore Opure constraints.

It remains lower-trust content.

---

## 82.1 No Policy Mutation

A prompt cannot:

- enable a tool;
- grant a server;
- change cloud policy;
- reveal a secret;
- or alter system instructions.

---

## 82.2 Saved Prompt

Saving a prompt stores its provenance and fingerprint.

A changed prompt requires review.

---

# 83. Roots

The Gateway does not initially advertise `roots`.

---

## 83.1 Why Disabled

Roots currently expose `file://` URIs and may reveal:

- absolute paths;
- project layout;
- usernames;
- drive letters;
- and repository locations.

They do not enforce filesystem access.

---

## 83.2 Remote Servers

Remote servers never receive local filesystem roots initially.

---

## 83.3 Local Servers

A local server needing files uses:

- an explicit sandbox directory grant;
- or a future exported workspace.

It does not receive a roots list as substitute for permission.

---

## 83.4 Future Exported Root

A future design may create:

- server-specific;
- project-specific;
- read-only;
- filtered;
- secret-scanned;
- ephemeral;
- and hash-tracked

workspace exports.

The root URI would reference the export, not the developer repository.

---

# 84. Sampling

The Gateway does not initially advertise sampling.

---

## 84.1 Risks

Sampling allows a server to ask Opure to use:

- developer provider credentials;
- model quota;
- project context;
- prompts;
- and, in the latest stable revision, tools.

---

## 84.2 No Server Agent Loop

Opure does not allow an MCP server to create an autonomous agent loop through client sampling initially.

---

## 84.3 No Context Inclusion

`includeContext` is not supported.

---

## 84.4 No Sampling Tools

Tool-enabled sampling is disabled.

---

## 84.5 Future Gate

Sampling requires:

- exact prompt preview;
- editable prompt;
- provider and model selection;
- context preview;
- token and cost budget;
- result-sharing preview;
- no hidden chain of thought;
- tool-use policy;
- cancellation;
- and per-request approval.

---

# 85. Elicitation

The Gateway may advertise form and URL elicitation only after implementation.

---

## 85.1 Server Identity

Every elicitation UI identifies the requesting server and account.

---

## 85.2 Nested Request

An elicitation nested inside a tool call does not inherit unlimited user-input authority.

---

## 85.3 Decline and Cancel

Always provide both.

---

# 86. Form Elicitation

Form mode is for non-sensitive structured data.

---

## 86.1 Supported Schema

Use only the protocol-supported restricted primitive schema.

---

## 86.2 Secret Detection

Reject fields whose:

- name;
- title;
- description;
- format;
- or behaviour

appears to request:

- password;
- API key;
- access token;
- private key;
- payment credential;
- recovery code;
- session cookie;
- or other secret.

---

## 86.3 Personal Data

Personal data such as name or email may be permitted after showing:

- requested fields;
- purpose;
- server;
- destination;
- and optionality.

---

## 86.4 Default Values

Defaults are server suggestions.

The user may edit or clear them.

---

## 86.5 Response

The exact response is shown before sending.

---

# 87. URL Elicitation

URL mode supports out-of-band sensitive or third-party interactions.

---

## 87.1 URL Validation

Require:

- HTTPS;
- exact canonical host;
- SSRF-safe destination;
- no embedded credentials;
- bounded URL length;
- and safe external navigation.

Loopback development is separate.

---

## 87.2 Display

Show:

- server;
- exact host;
- full destination;
- message;
- and that Opure cannot see or verify the out-of-band data entered.

---

## 87.3 Not MCP Client Auth

Do not use URL elicitation as a substitute for the MCP OAuth connection flow.

---

## 87.4 Completion

User acceptance means only that navigation was approved.

It does not prove the remote interaction succeeded.

---

# 88. Tasks

Experimental MCP tasks are disabled.

---

## 88.1 No Capability Advertisement

Do not advertise task support.

---

## 88.2 Incoming Task Augmentation

Reject task-augmented tool or client requests cleanly.

---

## 88.3 Future Gate

Future support must reconcile:

- MCP task IDs;
- Opure workflow IDs;
- durable state;
- polling;
- cancellation;
- retention;
- authentication;
- and recovery.

---

# 89. Logging

Servers may send structured log messages.

---

## 89.1 Untrusted Diagnostics

Treat all server log fields as untrusted.

---

## 89.2 Limits

Apply:

- severity mapping;
- rate limit;
- byte limit;
- message length;
- field allowlist;
- and redaction.

---

## 89.3 No Raw Serialization

Do not serialize arbitrary log objects directly.

---

## 89.4 Attribution

Attach server ID, session and trust class.

---

# 90. Progress

Progress notifications must correspond to an active request and valid progress token.

---

## 90.1 Unknown Token

Ignore and record bounded diagnostic.

---

## 90.2 Rate

Throttle excessive progress.

---

## 90.3 Display

Progress text is untrusted and cannot become a command or link automatically.

---

# 91. Cancellation

Either side may send cancellation.

---

## 91.1 Opure Cancellation

When the developer cancels:

- send MCP cancellation where supported;
- stop accepting result chunks;
- and terminate the local process or remote session if necessary and safe.

---

## 91.2 Server Cancellation

Treat as operation cancellation, not permission revocation.

---

## 91.3 Mutation Uncertainty

A cancelled remote mutation may have completed.

Result state is:

```text
Cancelled — Remote Outcome Unknown
```

until reconciled.

---

# 92. Completion Suggestions

MCP completions are untrusted suggestions.

---

## 92.1 No Execution

Selecting a completion does not invoke a tool.

---

## 92.2 Bounds

Limit count, string size and request frequency.

---

## 92.3 Sensitive Suggestions

Do not expose Vault values or unrelated project data to completion requests.

---

# 93. Ping and Liveness

Ping is permitted after session initialisation.

---

## 93.1 Timeout

Missing response may degrade or end the session.

---

## 93.2 No Authentication

A successful ping does not reauthenticate a session.

---

# 94. Notifications

Unknown notifications are ignored or cause a protocol error according to compatibility policy.

They never trigger a privileged action automatically.

---

# 95. Server-Requested Client Features

The only initial server-to-client feature requests considered are:

- elicitation;
- cancellation;
- ping;
- and protocol utilities.

Roots, sampling and tasks are unavailable.

---

# 96. Data Classification

Every MCP input and output is classified.

Potential classes:

- public;
- server metadata;
- plugin or server generated;
- project metadata;
- project source;
- project diff;
- project memory;
- diagnostics;
- personal data;
- authentication metadata;
- secret;
- and prohibited.

---

## 96.1 Secret

Raw secrets are not sent as ordinary tool arguments, resources, prompts or elicitation form responses.

---

## 96.2 Prohibited

Prohibited data cannot be transmitted even after ordinary approval.

---

# 97. Data Transmission Review

Before sending Opure-controlled data to a remote server, the plan shows:

- server and operator;
- account;
- tool or feature;
- exact data classes;
- file count and bytes where relevant;
- project;
- retention statement if known;
- and scope.

---

## 97.1 Local Server

A local sandboxed server receiving project data still receives a copy of that data.

The approval record remains relevant.

---

# 98. AI Router Integration

The AI Router sees MCP tools only through a filtered projection.

---

## 98.1 Projection Fields

May include:

- Opure-generated tool ID;
- safe title;
- safe description;
- input schema;
- risk class;
- server provenance;
- approval requirement;
- and availability.

---

## 98.2 Description Sanitisation

Preserve factual description but mark it as server supplied.

Do not let tool description add hidden higher-priority instructions.

---

## 98.3 Model Proposal

The model may propose:

- tool;
- arguments;
- and reason.

---

## 98.4 Deterministic Gate

MCP Gateway and policy services decide whether approval is required and whether invocation is permitted.

---

## 98.5 Result Return

Only approved bounded result content returns to the model operation.

---

# 99. Workflow Integration

A workflow may reference:

- exact server ID;
- exact tool fingerprint;
- exact account class;
- input mapping;
- result mapping;
- and approval policy.

---

## 99.1 Changed Tool

A tool fingerprint change pauses the workflow.

---

## 99.2 No Dynamic Tool Name

A workflow cannot accept an arbitrary server-returned tool name and execute it.

---

## 99.3 Checkpoint

Record remote-operation state and uncertain outcomes.

---

# 100. Plugin Integration

Plugins cannot access MCP transports directly.

A future plugin permission may allow a plugin to propose one exact MCP operation through the Gateway.

Until then, plugins do not invoke arbitrary MCP servers.

---

# 101. Project Binding

A server may be:

- profile scoped;
- project scoped;
- enterprise scoped;
- or session scoped.

---

## 101.1 Project Scoped

A project-scoped server connection does not automatically receive project files.

---

## 101.2 Remote Account

Account grants may be profile scoped, while tool and data approvals remain project scoped.

---

# 102. Server Configuration Secrets

Registration stores Vault references for:

- client IDs where sensitive;
- client secrets;
- API keys;
- environment secret bindings;
- and certificates.

No values in the server registry database.

---

# 103. Server Updates

A local server update changes its executable or package identity.

---

## 103.1 Reverification

Recompute:

- hash;
- publisher;
- version;
- command fingerprint;
- sandbox profile;
- and feature inventory.

---

## 103.2 Approval Invalidation

Invalidate tool approvals when definitions change materially.

---

## 103.3 Remote Server Changes

Remote operator, domain, OAuth issuer or resource metadata changes require review.

---

# 104. Server Quarantine

Quarantine triggers may include:

- executable hash mismatch;
- invalid publisher signature;
- package withdrawal;
- remote TLS identity change;
- OAuth issuer substitution;
- tool-definition conflict;
- malicious output;
- repeated protocol violations;
- secret leakage;
- sandbox escape attempt;
- or incident policy.

---

## 104.1 Effects

- end sessions;
- terminate local process;
- revoke tokens or mark suspended;
- disable tools;
- revoke persistent approvals;
- preserve evidence;
- and show remediation.

---

# 105. Revocation

The developer can revoke:

- one tool approval;
- one resource permission;
- one prompt;
- one account;
- one OAuth scope set;
- one project binding;
- one local directory grant;
- one environment secret binding;
- one server connection;
- or the server registration.

---

## 105.1 Immediate Effect

New operations deny immediately.

In-flight operations cancel where possible.

---

# 106. Removal

Removing a server registration:

- stops local servers;
- closes remote sessions;
- disables inventory;
- removes approvals;
- revokes or deletes local token records according to user choice;
- removes sandbox ACLs where owned;
- and preserves bounded audit evidence.

---

# 107. Recovery

On Runtime restart:

- local processes are terminated by supervision;
- remote sessions are invalid;
- live operation approvals expire;
- OAuth grants remain in Vault;
- persistent feature approvals remain only if fingerprints still match;
- and inventories are revalidated.

---

# 108. Offline Behaviour

Local stdio servers may work offline if their own approved capabilities allow it.

Remote servers are unavailable offline.

Opure local functionality remains available.

---

# 109. Persistence

The MCP Gateway stores authoritative metadata in a service-owned database.

Suggested tables:

```text
mcp_servers
mcp_server_identities
mcp_transports
mcp_protocol_profiles
mcp_oauth_profiles
mcp_accounts
mcp_token_references
mcp_feature_inventories
mcp_feature_fingerprints
mcp_tool_policies
mcp_resource_policies
mcp_prompt_policies
mcp_project_bindings
mcp_directory_grants
mcp_environment_secret_bindings
mcp_operation_approvals
mcp_operation_history
mcp_security_events
```

---

## 109.1 No Raw Tokens

No access or refresh token value enters the MCP database.

---

## 109.2 No Raw Secrets

No environment-secret value enters registration storage.

---

## 109.3 Session State

Live MCP session IDs remain in memory where practical.

---

## 109.4 Inventory History

Retain bounded feature-fingerprint history for:

- change review;
- incident analysis;
- and workflow compatibility.

---

# 110. Trust Centre

The Trust Centre should show:

- registered servers;
- trust class;
- source;
- local executable or remote endpoint;
- publisher or operator;
- transport;
- protocol revision;
- sandbox profile;
- OAuth issuer;
- account label;
- granted scopes;
- tools;
- resources;
- prompts;
- approvals;
- data transmissions;
- elicitation;
- tool results;
- failures;
- revocations;
- and removals.

---

## 110.1 Local Server View

Show:

- executable;
- SHA-256;
- signature;
- arguments;
- working directory;
- environment variable names;
- directory grants;
- network profile;
- AppContainer identity;
- Job Object limits;
- and process state.

Never show secret values.

---

## 110.2 Remote Server View

Show:

- canonical endpoint;
- TLS state;
- protected resource;
- authorisation server;
- account;
- scopes;
- last successful connection;
- and remote data sent.

---

## 110.3 Operation Evidence

Show:

- tool or resource;
- definition fingerprint;
- arguments summary;
- data classes;
- approval;
- time;
- result status;
- and uncertain remote outcome where relevant.

---

# 111. Diagnostics

Structured diagnostics include:

- server ID;
- trust class;
- transport;
- protocol version;
- session audit ID;
- request method;
- request audit ID;
- duration;
- byte counts;
- cancellation;
- and result category.

Do not log:

- OAuth tokens;
- refresh tokens;
- raw session IDs;
- secret environment values;
- full source payloads;
- full tool arguments containing personal data;
- or unredacted server responses.

---

# 112. Metrics

Low-cardinality local metrics may include:

- registered servers;
- connected servers;
- local process starts;
- remote connection attempts;
- OAuth authorisations;
- token refresh outcomes;
- tool calls by risk class;
- resource reads;
- prompt uses;
- elicitation requests;
- denials;
- cancellations;
- protocol violations;
- sandbox failures;
- and quarantine events.

Do not export server IDs or endpoint hosts as metric labels without policy.

---

# 113. Data Retention

Suggested provisional retention:

- current registration and grants: while active;
- feature fingerprints: current plus previous ten changes;
- operation approvals: 90 days;
- security-relevant operations: Trust Centre policy;
- failed OAuth metadata: 30 days, redacted;
- local server stderr: ordinary diagnostic retention;
- and removed server evidence: 180 days or security policy.

Exact policy requires privacy and support review.

---

# 114. User Interface

Suggested MCP settings sections:

```text
Servers
Connections
Accounts
Tools
Resources
Prompts
Permissions
Local Execution
Remote Data Sharing
History
Security
Developer Compatibility
```

---

## 114.1 Server Registration Review

Display:

- source;
- exact identity;
- command or endpoint;
- transport;
- local system authority;
- directory and network needs;
- authentication;
- expected features;
- and trust class.

---

## 114.2 Tool Review

Display:

- server;
- account;
- tool;
- server description;
- Opure risk classification;
- exact arguments;
- data sent;
- external side effects;
- OAuth scopes;
- and approval duration.

---

## 114.3 Resource Review

Display:

- server;
- resource URI;
- description;
- MIME;
- size;
- intended destination;
- and content provenance.

---

## 114.4 Prompt Review

Display:

- server;
- prompt name;
- arguments;
- generated messages;
- embedded resources;
- model destination;
- and untrusted-content notice.

---

## 114.5 OAuth Review

Display:

- operator;
- resource;
- authorisation server;
- account;
- requested scopes;
- redirect host;
- and reason.

---

# 115. Accessibility

MCP registration, OAuth, tool, resource and elicitation UI must support:

- keyboard;
- Narrator;
- high contrast;
- reduced motion;
- safe focus;
- complete command display;
- expandable schemas;
- and text-based risk explanations.

Risk is not communicated by colour alone.

---

# 116. Error Model

Stable categories include:

- MCP Server Not Registered;
- MCP Server Disabled;
- MCP Server Quarantined;
- MCP Transport Unsupported;
- MCP Protocol Version Unsupported;
- MCP Initialisation Failed;
- MCP Capability Unsupported;
- MCP Message Invalid;
- MCP Message Too Large;
- MCP Protocol Violation;
- MCP Local Command Rejected;
- MCP Sandbox Unavailable;
- MCP Process Failed;
- MCP Remote Origin Rejected;
- MCP SSRF Protection Triggered;
- MCP Authentication Required;
- MCP Authorisation Failed;
- MCP Scope Approval Required;
- MCP Token Invalid;
- MCP Token Audience Mismatch;
- MCP Session Invalid;
- MCP Tool Changed;
- MCP Tool Approval Required;
- MCP Tool Denied;
- MCP Tool Result Invalid;
- MCP Resource Approval Required;
- MCP Resource Invalid;
- MCP Prompt Changed;
- MCP Elicitation Denied;
- MCP Sampling Unsupported;
- MCP Roots Unsupported;
- MCP Tasks Unsupported;
- MCP Operation Cancelled;
- MCP Remote Outcome Unknown;
- and MCP Recovery Required.

---

# 117. Security Threat Model

Relevant threats include:

- malicious Registry entry;
- arbitrary local startup command;
- shell injection;
- mutable package runner;
- local server package substitution;
- executable hash change;
- malicious local full-trust server;
- AppContainer escape;
- direct project-root access;
- direct network exfiltration;
- environment-secret theft;
- remote endpoint substitution;
- TLS interception;
- OAuth metadata SSRF;
- malicious redirect;
- dynamic-registration abuse;
- confused deputy;
- token passthrough;
- token audience confusion;
- refresh-token leakage;
- session hijacking;
- session ID used as authentication;
- tool-definition change;
- misleading tool annotation;
- malicious JSON Schema;
- tool argument injection;
- prompt injection;
- resource poisoning;
- active-content output;
- result-driven tool chaining;
- hidden scope expansion;
- malicious elicitation;
- secret collection through forms;
- URL phishing;
- sampling abuse;
- model quota theft;
- server-controlled agent loops;
- experimental task persistence;
- log flooding;
- progress flooding;
- and uncertain remote mutation outcomes.

---

# 118. Security Controls

Controls include:

- explicit registration;
- exact local command vectors;
- no shell;
- immutable hashes;
- publisher verification;
- AppContainer;
- Job Object;
- minimal environment;
- secret references;
- Network Gateway;
- HTTPS;
- SSRF filtering;
- redirect validation;
- OAuth PKCE;
- resource audience validation;
- minimum scopes;
- Vault token storage;
- no token passthrough;
- session non-authentication;
- protocol negotiation;
- message limits;
- feature fingerprints;
- independent risk classification;
- exact operation plans;
- human approval;
- output validation;
- provenance;
- content classification;
- no roots;
- no sampling;
- no tasks;
- safe elicitation;
- revocation;
- and Trust Centre evidence.

---

# 119. Security Limitations

This design does not guarantee protection against:

- a Windows kernel vulnerability;
- a sandbox escape;
- a malicious trusted Opure update;
- a compromised trusted Runtime broker;
- same-user malware;
- a remote server behaving maliciously within approved authority;
- a user approving harmful external transmission;
- a legitimately authorised destructive remote action;
- a compromised OAuth authorisation server;
- a malicious but validly signed local server;
- side channels;
- or all prompt-injection influence.

The product must state these limitations honestly.

---

# 120. Reliability Impact

The Gateway adds:

- protocol adaptation;
- policy checks;
- process supervision;
- OAuth flows;
- inventory fingerprinting;
- and response validation.

This reduces direct coupling and centralises recovery.

A server outage or protocol failure does not prevent local Opure work.

---

# 121. Performance Impact

Expected costs include:

- JSON parsing;
- IPC;
- network mediation;
- schema validation;
- content classification;
- hashing;
- sandbox startup;
- and approval UI.

The Gateway should stream large approved content and avoid retaining duplicate payloads.

---

# 122. Privacy Impact

Remote MCP connections expose:

- source IP;
- ordinary TLS and HTTP metadata;
- account identity;
- requested feature;
- and approved request data

to the remote operator.

Local server execution may expose approved files and secrets to that process.

Every material data flow must remain visible.

---

# 123. Cost Impact

Remote tools and sampling may incur external charges.

The tool plan should show known or estimated cost when available.

Sampling is disabled initially.

---

# 124. Testing Strategy

ADR-0008 applies.

MCP requires:

- protocol conformance tests;
- transport tests;
- local command tests;
- sandbox tests;
- OAuth tests;
- SSRF tests;
- tool-policy tests;
- resource tests;
- prompt-injection tests;
- elicitation tests;
- revocation tests;
- recovery tests;
- fuzzing;
- and malicious server fixtures.

---

# 125. Protocol Tests

Test:

- valid `2025-11-25` initialisation;
- valid `2025-06-18` compatibility;
- unsupported revision;
- server-selected mismatch;
- missing initialisation;
- request before initialisation;
- duplicate request ID;
- invalid JSON-RPC version;
- invalid method;
- invalid response shape;
- duplicate properties;
- oversized message;
- excessive nesting;
- invalid UTF-8;
- and unknown experimental capability.

---

# 126. Capability Negotiation Tests

Test:

- tools;
- resources;
- prompts;
- logging;
- completions;
- elicitation form;
- elicitation URL;
- roots requested;
- sampling requested;
- sampling tools requested;
- tasks requested;
- subscriptions;
- and experimental features.

Unsupported client capabilities must not be advertised.

---

# 127. Stdio Tests

Test:

- exact executable;
- argument array;
- no shell;
- valid protocol output;
- stdout startup noise;
- stderr logging;
- line length;
- log flood;
- process crash;
- cancellation;
- graceful shutdown;
- forced termination;
- and child process attempt.

---

# 128. Local Command Security Tests

Attempt registrations using:

- `cmd /c`;
- PowerShell command string;
- `npx`;
- `npm exec`;
- `uvx`;
- `pipx`;
- mutable Git script;
- relative executable;
- executable from Downloads;
- path with spaces;
- argument injection;
- environment expansion;
- and response-file injection.

Production policy must reject or require the correct explicit mode.

---

# 129. Local Identity Tests

Test:

- valid signed executable;
- unsigned executable;
- changed hash;
- changed signature;
- changed arguments;
- changed working directory;
- changed interpreter;
- changed script;
- package update;
- publisher transfer;
- and source transfer.

---

# 130. Sandbox Tests

Test:

- AppContainer creation;
- package read;
- own data write;
- explicit directory read;
- project-root denial;
- user-profile denial;
- Vault denial;
- other plugin denial;
- direct network denial;
- named-pipe or stdio operation;
- Job Object;
- child-process denial;
- memory limit;
- CPU limit;
- kill-on-close;
- and no silent full-trust fallback.

---

# 131. Legacy Full Trust Tests

Test:

- disabled default;
- exact warning;
- manual start;
- no auto-start;
- no silent tool approval;
- environment-secret warning;
- process access;
- and Trust Centre classification.

---

# 132. Streamable HTTP Tests

Test:

- valid HTTPS endpoint;
- invalid HTTP;
- loopback development;
- private IP;
- link-local;
- cloud metadata address;
- DNS rebinding;
- redirect to private address;
- cross-origin redirect;
- valid POST;
- valid GET SSE;
- server-initiated disconnect;
- resumability;
- HTTP DELETE session;
- and invalid content type.

---

# 133. Legacy Transport Tests

Test:

- modern server;
- legacy-only server;
- automatic fallback denied;
- explicit compatibility approval;
- and unsupported insecure endpoint.

---

# 134. Session Tests

Test:

- secure session ID;
- missing session ID;
- wrong server;
- wrong account;
- wrong token;
- session replay;
- session after logout;
- session after Runtime restart;
- session 404;
- and session termination.

A session ID alone must never authorise a request.

---

# 135. OAuth Discovery Tests

Test:

- Protected Resource Metadata;
- Authorization Server Metadata;
- OIDC Discovery;
- Client ID Metadata Document;
- DCR;
- fallback metadata;
- invalid JSON;
- oversized metadata;
- wrong issuer;
- wrong resource;
- HTTP endpoint;
- private IP;
- loopback;
- link-local;
- IPv4-mapped IPv6;
- encoded IP;
- redirect chain;
- DNS rebinding;
- and response-body reflection.

---

# 136. OAuth Flow Tests

Test:

- PKCE success;
- missing PKCE;
- wrong verifier;
- state mismatch;
- state replay;
- callback timeout;
- callback from wrong browser request;
- wrong redirect URI;
- user denial;
- code replay;
- token endpoint error;
- and cancellation.

---

# 137. Token Tests

Test:

- correct audience;
- wrong audience;
- wrong issuer;
- expired access token;
- refresh success;
- refresh failure;
- reduced scope;
- broader scope challenge;
- opaque token;
- JWT token;
- query-string attempt;
- token in logs;
- token in tool arguments;
- token in model context;
- token passthrough;
- and sign-out.

---

# 138. Scope Tests

Test:

- minimum initial scope;
- incremental scope;
- omnibus scope request;
- removed scope;
- tool requires new scope;
- scope challenge from changed resource;
- and user denial.

---

# 139. Registry Tests

Test:

- valid Registry metadata;
- preview status;
- namespace;
- remote endpoint;
- local command;
- mutable version;
- missing hash;
- package runner;
- deceptive display name;
- and removed Registry entry.

No entry becomes trusted automatically.

---

# 140. Tool Inventory Tests

Test:

- valid tools list;
- pagination;
- duplicate name;
- invalid name;
- oversized description;
- hostile description;
- invalid input schema;
- remote `$ref`;
- regex abuse;
- invalid output schema;
- misleading annotations;
- changed list;
- removed tool;
- and same name changed schema.

---

# 141. Tool Approval Tests

Test:

- new tool;
- low-risk read;
- external write;
- delete;
- code execution;
- financial action;
- unknown;
- persistent exact approval;
- changed project;
- changed account;
- changed arguments;
- changed definition;
- changed OAuth scope;
- and AI attempt to approve.

---

# 142. Tool Invocation Tests

Test:

- valid input;
- missing input;
- extra input;
- schema mismatch;
- timeout;
- cancellation;
- progress;
- retry;
- non-idempotent uncertain outcome;
- malformed result;
- invalid structured output;
- oversized output;
- resource link;
- embedded resource;
- image;
- audio;
- and active content.

---

# 143. Prompt-Injection Tests

Use malicious instructions in:

- server instructions;
- tool description;
- tool annotation;
- resource content;
- resource name;
- prompt template;
- tool result;
- log message;
- progress text;
- completion suggestion;
- and icon metadata.

Verify none changes:

- system policy;
- permission;
- cloud policy;
- secret access;
- tool approval;
- or server registration.

---

# 144. Resource Tests

Test:

- list;
- pagination;
- template;
- read;
- text;
- blob;
- URI mismatch;
- MIME mismatch;
- oversized content;
- base64 bomb;
- custom scheme;
- remote `file://`;
- local path claim;
- active HTML;
- SVG script;
- remote image;
- resource link;
- and subscription request.

---

# 145. Resource-to-Model Tests

Test:

- local model;
- remote provider;
- project cloud policy;
- prompt-injection labelling;
- denied data class;
- oversized resource;
- and no automatic context inclusion.

---

# 146. Prompt Tests

Test:

- list;
- pagination;
- get;
- required arguments;
- hostile prompt;
- role manipulation;
- embedded resource;
- image;
- audio;
- changed prompt;
- removed prompt;
- user selection;
- and no system-prompt elevation.

---

# 147. Roots Tests

Test that:

- roots capability is absent;
- server request is rejected;
- remote server receives no project path;
- local server receives no implicit root;
- and direct directory grants remain separate.

---

# 148. Sampling Tests

Test that:

- sampling capability is absent;
- basic sampling request is rejected;
- context inclusion is rejected;
- sampling tools are rejected;
- server agent loop cannot start;
- and provider credential is not used.

---

# 149. Elicitation Form Tests

Test:

- valid primitive fields;
- defaults;
- edit;
- decline;
- cancel;
- password request;
- API key request;
- token request;
- payment credential;
- deceptive field title;
- hidden required field;
- excessive schema;
- and personal-data review.

---

# 150. URL Elicitation Tests

Test:

- valid HTTPS;
- wrong host;
- HTTP;
- private IP;
- loopback development;
- embedded credentials;
- oversized URL;
- phishing display;
- user decline;
- external navigation;
- and completion notification.

---

# 151. Tasks Tests

Test:

- no task capability;
- task-augmented tool call;
- tasks list;
- tasks cancel;
- deferred result;
- and experimental capability advertisement.

All must fail cleanly or remain ignored.

---

# 152. Logging and Progress Tests

Test:

- normal log;
- oversized log;
- log flood;
- secret canary;
- source canary;
- capability token canary;
- valid progress;
- unknown progress token;
- progress flood;
- and malicious link text.

---

# 153. Completion Tests

Test:

- prompt argument completion;
- resource-template completion;
- oversized suggestions;
- secret leakage;
- project-data leakage;
- hostile suggestion;
- and selection without invocation.

---

# 154. Change Notification Tests

Test:

- tools changed;
- resources changed;
- prompts changed;
- repeated notifications;
- change during active operation;
- changed schema after approval;
- and removed feature.

---

# 155. Data Classification Tests

Use canaries for:

- public data;
- project source;
- diff;
- memory;
- personal data;
- diagnostics;
- OAuth metadata;
- secret;
- and prohibited data.

Verify correct review and destination controls.

---

# 156. AI Integration Tests

Test:

- hidden tool;
- developer-only tool;
- AI-visible tool;
- model proposal;
- invalid arguments;
- approval required;
- approval denied;
- safe result return;
- malicious result;
- result too large;
- and no automatic tool chaining.

---

# 157. Workflow Tests

Test:

- exact tool fingerprint;
- changed tool;
- changed account;
- changed project;
- preapproved low-risk call;
- high-risk call;
- uncertain outcome;
- cancellation;
- and checkpoint recovery.

---

# 158. Revocation Tests

Test revocation of:

- tool approval;
- resource permission;
- prompt;
- OAuth scope;
- account;
- directory grant;
- environment secret;
- project binding;
- connection;
- and registration.

---

# 159. Quarantine Tests

Simulate:

- local hash mismatch;
- invalid signature;
- Registry withdrawal;
- TLS identity change;
- issuer substitution;
- token leakage;
- malicious result;
- repeated protocol violation;
- sandbox escape attempt;
- and server compromise.

---

# 160. Recovery Tests

Test:

- Runtime restart;
- local server orphan;
- remote stream loss;
- session invalidation;
- inventory change;
- token expiry;
- partial mutating tool;
- uncertain result;
- and reauthorisation.

---

# 161. Fuzzing

Fuzz:

- JSON-RPC parser;
- initialisation messages;
- capability negotiation;
- tool schemas;
- resource URIs;
- prompt definitions;
- OAuth metadata;
- session IDs;
- SSE event IDs;
- elicitation schemas;
- and tool results.

---

# 162. Performance Tests

Measure:

- local server cold start;
- sandbox start;
- protocol initialisation;
- remote connection;
- OAuth metadata discovery;
- tools list;
- definition fingerprinting;
- tool-plan creation;
- schema validation;
- resource read;
- prompt retrieval;
- result classification;
- cancellation;
- and shutdown.

---

# 163. Provisional Performance Targets

On reference hardware:

- MCP Gateway local request overhead excluding server work: under 10 ms p95;
- cached tool-policy evaluation: under 3 ms p95;
- definition fingerprinting for 100 tools: under 100 ms;
- local sandboxed server readiness: under 3 seconds p95;
- remote HTTPS initialisation excluding OAuth: under 2 seconds p95 on a healthy connection;
- cancellation dispatch: under 100 ms;
- and revocation of new operations: under 250 ms.

These are targets pending evidence.

---

# 164. Prototype Plan

## 164.1 Prototype A — Stable Protocol

Connect to a controlled `2025-11-25` server over stdio and Streamable HTTP.

---

## 164.2 Prototype B — Local Command

Register a signed local executable without a shell and prove exact command review.

---

## 164.3 Prototype C — Sandboxing

Run the server in an AppContainer and deny project and network access.

---

## 164.4 Prototype D — OAuth

Connect to a protected remote server using PKCE and Vault token storage.

---

## 164.5 Prototype E — SSRF

Use malicious protected-resource and authorisation metadata targeting localhost, private networks and cloud metadata.

---

## 164.6 Prototype F — Tools

Discover tools, fingerprint schemas, classify risk and require exact approval.

---

## 164.7 Prototype G — Tool Change

Change a tool schema after approval and prove invalidation.

---

## 164.8 Prototype H — Resources and Prompts

Read one resource and retrieve one prompt with provenance and safe display.

---

## 164.9 Prototype I — Elicitation

Handle safe form and URL elicitation and reject secret form fields.

---

## 164.10 Prototype J — Disabled Features

Prove roots, sampling and tasks are not advertised or accepted.

---

## 164.11 Prototype K — Prompt Injection

Return hostile instructions through every server-controlled content surface.

---

## 164.12 Prototype L — Incident

Quarantine a local and remote server, revoke tokens and preserve evidence.

---

# 165. Implementation Plan

1. Record founder review.
2. Select or build MCP SDK adapter.
3. Define supported protocol profiles.
4. Define server registry schema.
5. Define server trust classes.
6. Implement local identity fingerprinting.
7. Implement remote endpoint identity.
8. Implement stdio transport.
9. Implement Streamable HTTP through Network Gateway.
10. Add strict JSON-RPC limits.
11. Implement lifecycle negotiation.
12. Implement unsupported-feature policy.
13. Implement local command review.
14. Integrate Process Supervisor.
15. Implement AppContainer server profile.
16. Implement Job Object policy.
17. Implement minimal environment and secret bindings.
18. Implement OAuth metadata discovery.
19. Implement SSRF protections.
20. Implement PKCE and browser callback.
21. Implement Client ID Metadata Documents.
22. Implement approved DCR fallback.
23. Integrate Vault token storage.
24. Implement scope review and refresh.
25. Implement feature inventory and fingerprints.
26. Implement tool classification and approval.
27. Implement resource policy and safe renderer.
28. Implement prompt policy and preview.
29. Implement elicitation form and URL modes.
30. Explicitly disable roots, sampling and tasks.
31. Implement logging, progress and cancellation.
32. Implement AI Router filtered projection.
33. Implement workflow exact-tool binding.
34. Implement Trust Centre views.
35. Add adversarial MCP servers.
36. Run security review.
37. Accept, amend or reject the ADR.

---

# 166. Owners

| Area | Owner |
|---|---|
| Product policy | Founder |
| MCP protocol | MCP Gateway Owner |
| Local process launch | Process Supervisor Owner |
| Local sandbox | Windows Security Owner |
| Remote transport | Network Gateway Owner |
| OAuth | MCP Gateway and Security Owners |
| Token storage | Secrets Owner |
| Tool policy | MCP Gateway and Workflow Owners |
| Resource and prompt content | MCP Gateway and AI Safety Owners |
| AI integration | AI Router Owner |
| Project data | Workspace Owner |
| Trust Centre | Trust Centre Owner |
| Desktop consent UI | Desktop Owner |
| Persistence | Persistence Owner |
| Adversarial tests | Test Architecture Owner |

---

# 167. Suggested Repository Structure

```text
src/
├── MCP/
│   ├── Opure.Mcp.Contracts/
│   ├── Opure.Mcp.Protocol/
│   ├── Opure.Mcp.Gateway/
│   ├── Opure.Mcp.Security/
│   ├── Opure.Mcp.Authorization/
│   ├── Opure.Mcp.Content/
│   └── Opure.Mcp.Sandboxing.Windows/
├── Runtime/
│   └── Opure.Runtime.Mcp/
└── Desktop/
    └── Opure.Desktop.Mcp/

tests/
└── MCP/
    ├── Opure.Mcp.UnitTests/
    ├── Opure.Mcp.ProtocolTests/
    ├── Opure.Mcp.AuthorizationTests/
    ├── Opure.Mcp.SandboxTests/
    ├── Opure.Mcp.SecurityTests/
    └── Fixtures/
        └── MaliciousServers/
```

Exact projects may be consolidated under ADR-0010.

---

# 168. Server Registration Sketch

```json
{
  "server_id": "mcp-server-opaque",
  "display_name": "Example Repository Service",
  "transport": {
    "kind": "streamable-http",
    "endpoint": "https://mcp.example.com/mcp"
  },
  "protocol_profile": "2025-11-25",
  "trust_class": "authenticated-remote",
  "oauth": {
    "resource": "https://mcp.example.com/",
    "registration": "client-id-metadata-document"
  },
  "project_binding": "explicit",
  "auto_start": false
}
```

---

# 169. Local Registration Sketch

```json
{
  "server_id": "mcp-server-opaque",
  "display_name": "Local Repository Inspector",
  "transport": {
    "kind": "stdio",
    "executable": "C:\\Program Files\\Example\\server.exe",
    "arguments": ["--stdio"],
    "working_directory_policy": "server-data"
  },
  "identity": {
    "sha256": "...",
    "publisher": "CN=Example Ltd"
  },
  "sandbox": {
    "kind": "appcontainer",
    "network": "none",
    "project_root": "none",
    "child_processes": false
  }
}
```

---

# 170. Tool Approval Sketch

```json
{
  "schema": "opure.mcp-tool-plan/1",
  "server_id": "mcp-server-opaque",
  "account": "Example account",
  "tool": "issues.create",
  "tool_fingerprint": "...",
  "arguments": {
    "repository": "example/repository",
    "title": "Bug summary"
  },
  "risk": "external-write",
  "data_classes": ["project.metadata"],
  "external_side_effects": ["Creates a remote issue"],
  "approval": "per-call"
}
```

---

# 171. Resource Provenance Sketch

```json
{
  "server_id": "mcp-server-opaque",
  "resource_uri": "docs://example/guide",
  "resource_fingerprint": "...",
  "content_sha256": "...",
  "mime_type": "text/markdown",
  "classification": "external-untrusted",
  "fetched_at": "...",
  "destination": "user-review"
}
```

---

# 172. Release Gate

Public MCP support is blocked when:

- unsupported protocol negotiation succeeds silently;
- local commands use a shell string;
- a package runner downloads mutable code at launch;
- local executable identity is not verified;
- sandbox failure falls back silently;
- local server can access project root without a grant;
- direct network occurs outside policy;
- OAuth discovery can reach private or metadata addresses;
- PKCE is absent;
- tokens enter normal storage or logs;
- token passthrough is possible;
- session ID acts as authentication;
- a connected server can invoke tools without separate approval;
- tool annotations determine trust;
- changed schemas retain approval;
- resource content enters model context automatically;
- MCP prompt becomes a system instruction;
- form elicitation can request secrets;
- roots or sampling are advertised prematurely;
- experimental tasks are accepted;
- a tool result can trigger another tool automatically;
- or Trust Centre cannot reconstruct a material operation.

---

# 173. Acceptance Criteria

This ADR may move to **Accepted** when all of the following hold.

## Gateway and Protocol

- [ ] MCP Gateway is the sole protocol owner.
- [ ] Desktop has no raw MCP transport.
- [ ] AI Router has no raw MCP transport.
- [ ] Plugins have no raw MCP transport.
- [ ] Stable `2025-11-25` is supported.
- [ ] `2025-06-18` requires a compatibility profile.
- [ ] Unsupported versions disconnect.
- [ ] Unknown experimental capabilities are disabled.
- [ ] JSON-RPC validation is strict.
- [ ] Message, schema and inventory limits are enforced.
- [ ] Duplicate critical properties are rejected.
- [ ] Server instructions remain untrusted.

## Registration and Identity

- [ ] Every server is explicitly registered.
- [ ] Registry discovery grants no trust.
- [ ] Local identity includes executable and hash.
- [ ] Local arguments are exact and visible.
- [ ] Remote identity includes exact HTTPS endpoint.
- [ ] MCP display name is not security identity.
- [ ] Identity changes end sessions and invalidate approvals.
- [ ] AI, repository content and plugins cannot auto-register servers.
- [ ] Server removal revokes active authority.

## Local Servers

- [ ] Stdio transport works.
- [ ] No production shell-string execution exists.
- [ ] Package runners are rejected in production.
- [ ] Interpreted servers bind interpreter and script hashes.
- [ ] Working directory is explicit.
- [ ] Environment is minimal.
- [ ] Secret environment bindings are exact and reviewed.
- [ ] Stdout accepts MCP only.
- [ ] Stderr is bounded and redacted.
- [ ] Third-party production servers use the accepted sandbox.
- [ ] Project root is not granted by default.
- [ ] Network is not granted by default.
- [ ] Job Object supervision works.
- [ ] Child processes are denied by default.
- [ ] Sandbox failure blocks activation.
- [ ] Legacy Full Trust is disabled by default and visibly high risk.
- [ ] Local HTTP is disabled outside explicit development policy.

## Remote Transport

- [ ] Streamable HTTP works over HTTPS.
- [ ] Remote traffic uses Network Gateway.
- [ ] Cross-origin redirects are denied or explicitly validated.
- [ ] Private, loopback, link-local and metadata destinations are blocked.
- [ ] Legacy HTTP+SSE does not downgrade silently.
- [ ] Session IDs are opaque.
- [ ] Session IDs are not authentication.
- [ ] Authorisation is included on every protected request.
- [ ] Sessions are terminated when supported.
- [ ] Remote outage does not affect local Opure work.

## OAuth

- [ ] Opure operates as a public client by default.
- [ ] No universal embedded client secret exists.
- [ ] PKCE is mandatory.
- [ ] State is random, one-shot and validated.
- [ ] System browser is used.
- [ ] Discovery uses Protected Resource Metadata.
- [ ] Authorization Server Metadata is supported.
- [ ] OIDC Discovery is supported where applicable.
- [ ] Client ID Metadata Documents are supported.
- [ ] DCR is explicit and bounded.
- [ ] SSRF protections cover discovery and redirects.
- [ ] Tokens are stored only in Vault.
- [ ] Access tokens use Authorization headers.
- [ ] Tokens are resource and audience bound.
- [ ] Token passthrough is impossible.
- [ ] Minimum scopes are requested.
- [ ] Incremental scopes require review.
- [ ] Tokens never reach models, plugins or logs.
- [ ] Sign-out and refresh failure are handled.

## Tools

- [ ] Tool definitions are fingerprinted.
- [ ] Tool annotations are treated as untrusted.
- [ ] New tools are not AI-visible by default.
- [ ] Connection does not approve invocation.
- [ ] Tool risk is independently classified.
- [ ] High-risk tools require per-call approval.
- [ ] AI cannot approve.
- [ ] Arguments are schema validated.
- [ ] Remote schema references are not fetched.
- [ ] Tool plan changes invalidate approval.
- [ ] Structured output is validated.
- [ ] All output is bounded and classified.
- [ ] Resource links are not dereferenced automatically.
- [ ] Tool output cannot trigger another tool automatically.
- [ ] Mutating retries require independent idempotency evidence.
- [ ] Uncertain remote outcomes are represented honestly.
- [ ] Tool-list changes invalidate affected approvals.

## Resources and Prompts

- [ ] Resource metadata is bounded.
- [ ] Resource reads require policy.
- [ ] Remote `file://` does not access local files.
- [ ] Resource MIME and size are validated.
- [ ] Active content is disabled.
- [ ] Provenance remains attached.
- [ ] Resources do not enter AI context automatically.
- [ ] Resource subscriptions are disabled.
- [ ] MCP prompts are user controlled.
- [ ] Prompt definitions are fingerprinted.
- [ ] Prompt content never becomes system policy.
- [ ] Prompt messages are previewed.
- [ ] Changed prompts require review.
- [ ] Embedded resources remain untrusted.

## Client Features

- [ ] Roots are not advertised.
- [ ] Raw project paths are not exposed.
- [ ] Sampling is not advertised.
- [ ] Sampling with tools is disabled.
- [ ] Experimental tasks are disabled.
- [ ] Safe form elicitation works.
- [ ] Secret form elicitation is rejected.
- [ ] URL elicitation shows exact destination.
- [ ] URL elicitation does not replace MCP OAuth.
- [ ] Decline and cancel are always available.
- [ ] Logs and progress are bounded.
- [ ] Completions are suggestions only.

## Integration and Security

- [ ] AI receives filtered tool projections.
- [ ] Workflow references exact tool fingerprints.
- [ ] Changed tools pause workflows.
- [ ] Result-to-model flow is separately authorised.
- [ ] Result-to-memory flow is separately authorised.
- [ ] Prompt injection cannot alter permissions.
- [ ] Data classes are visible before external transmission.
- [ ] Secret values never enter ordinary MCP content.
- [ ] Quarantine ends sessions and local processes.
- [ ] Revocation stops new operations immediately.
- [ ] Trust Centre records material events.
- [ ] Security review is complete.
- [ ] Founder approval is recorded.

---

# 174. Evidence Required Before Acceptance

- [ ] MCP protocol profile report.
- [ ] SDK or adapter dependency review.
- [ ] JSON-RPC limit report.
- [ ] Local stdio report.
- [ ] No-shell command report.
- [ ] Package-runner rejection report.
- [ ] Local identity and update report.
- [ ] AppContainer report.
- [ ] Job Object report.
- [ ] Project-root denial report.
- [ ] Direct-network denial report.
- [ ] Environment-secret canary report.
- [ ] Streamable HTTP report.
- [ ] Legacy transport report.
- [ ] Session non-authentication report.
- [ ] OAuth discovery report.
- [ ] SSRF adversarial report.
- [ ] PKCE and state report.
- [ ] Client registration report.
- [ ] Vault token report.
- [ ] Audience and token-passthrough report.
- [ ] Incremental scope report.
- [ ] Tool inventory and fingerprint report.
- [ ] Tool classification and approval screenshots.
- [ ] Changed-tool invalidation report.
- [ ] Tool-result validation report.
- [ ] Resource provenance and renderer report.
- [ ] Prompt preview and policy report.
- [ ] Roots-disabled report.
- [ ] Sampling-disabled report.
- [ ] Elicitation security report.
- [ ] Tasks-disabled report.
- [ ] Prompt-injection corpus report.
- [ ] AI Router integration report.
- [ ] Workflow binding report.
- [ ] Quarantine and revocation rehearsal.
- [ ] Performance report.
- [ ] Accessibility report.
- [ ] Security review.
- [ ] Founder approval.

---

# 175. Known Limitations

- The exact MCP SDK is not selected.
- MCP protocol and SDKs continue to evolve.
- The official Registry is still preview.
- The final local MCP package format is not selected.
- Many existing local servers expect full user authority.
- AppContainer compatibility with arbitrary Node, Python and native servers is not guaranteed.
- Host-specific network allowlisting inside AppContainer may require an egress proxy or stronger platform controls.
- Legacy Full Trust remains inherently high risk.
- OAuth server implementations vary.
- Dynamic Client Registration may be unavailable.
- Client ID Metadata Documents may not be universally supported.
- Some remote tokens are opaque and offer limited local claim validation.
- The initial design does not implement roots.
- The initial design does not implement sampling.
- The initial design does not implement MCP tasks.
- Resource subscriptions are disabled.
- A model can still be influenced by approved untrusted content.
- Prompt-injection classifiers cannot provide a complete security boundary.
- A valid schema does not prove a tool is safe.
- Tool annotations may be deceptive.
- Remote mutations may have uncertain outcomes after cancellation.
- A trusted remote operator may still be compromised.
- Registry namespace control does not prove code quality.
- Server icons and remote content require privacy controls.
- Full MCP conformance testing may depend on evolving external test suites.
- Linux and macOS sandboxing are deferred.

---

# 176. Open Questions

- Which official or community MCP .NET SDK should Opure use?
- Should Opure implement a minimal protocol core directly?
- Which MCP SDK tier and maintenance status are acceptable?
- Should `2025-06-18` remain supported after Version 1.0?
- When should deprecated HTTP+SSE support be removed entirely?
- Should local custom named-pipe MCP ever be supported?
- What local package type should distribute verified MCP servers?
- Should `OpureMcpServer` reuse ADR-0016 NuGet infrastructure?
- How should Registry `server.json` metadata map into registration?
- Should the Registry be enabled by default?
- Which publisher evidence is required for Registry packages?
- Can Node and Python servers run reliably in AppContainer?
- Should local server environments be prebuilt and immutable?
- How should dependency lockfiles be verified?
- What egress proxy can mediate sandboxed local server networking?
- Can Windows Firewall provide per-AppContainer destination restrictions?
- Which direct directory grants are acceptable?
- Should project data always use exported snapshots rather than direct grants?
- How should exported workspace updates be represented?
- How long may a local server remain running?
- Which server types may auto-start?
- Should Legacy Full Trust be available in Stable?
- Should environment-secret disclosure be prohibited entirely for unverified servers?
- Which OAuth callback method is most reliable in MSIX?
- How should loopback callback firewall prompts be avoided?
- When should claimed HTTPS callbacks be adopted?
- Which Client ID Metadata Document URL should identify Opure?
- How does repository ownership affect client metadata?
- Which DCR metadata fields should Opure send?
- Should a client secret returned to a public client be stored?
- How should multiple OAuth accounts be presented?
- Which OAuth scopes may persist per profile?
- How should incremental scope challenges map to tool operations?
- Should Opure support device authorisation flow for headless enterprise servers?
- How should mTLS-protected MCP servers be supported?
- Should DPoP be supported?
- How should certificate-bound tokens be stored and used?
- Which private network endpoints may enterprise policy allow?
- How should split-horizon DNS be handled safely?
- What DNS pinning mechanism should Network Gateway use?
- Which HTTP redirect policies are compatible with major servers?
- Should remote session IDs ever persist across restart?
- What exact JSON-RPC message limits are appropriate?
- Which JSON Schema implementation and safety controls are selected?
- How should regex denial-of-service be prevented?
- Which tool-risk rules can be deterministic?
- Should low-risk tools ever be auto-approved after repeated use?
- How should server tool changes appear in workflows?
- Should output schemas be mandatory for curated servers?
- How should images and audio from MCP be scanned?
- Which resource MIME types are safe to preview?
- Should resource subscriptions be supported after Version 1.0?
- How should prompt templates interact with user-authored prompt libraries?
- Should MCP prompts be allowed to emit assistant-role messages?
- When should roots be implemented?
- Should roots use exported snapshots or a brokered virtual filesystem?
- When should sampling be implemented?
- Should sampling ever support server-provided tools?
- How are sampling costs displayed?
- Should MCP tasks map onto Opure workflows or remain separate?
- When will MCP tasks leave experimental status?
- How should URL elicitation completion be reconciled?
- Which personal-data fields require additional warning?
- How are remote server retention and privacy claims displayed?
- Should the official Registry be mirrored locally?
- Should Registry metadata be signed or attested?
- Should Opure maintain a local blocklist of known malicious servers?
- How are withdrawn remote services represented offline?
- Should server tool inventories be exported for audit?
- How should an enterprise preapprove server and tool policy?
- How should MCP server trust migrate across Stable and Preview?
- Should first-party MCP adapters use MCP internally or direct service contracts?
- What permanent evidence should support security investigations?

---

# 177. Deferred Decisions

This ADR intentionally defers:

- final MCP SDK;
- local MCP package format;
- Registry integration beyond metadata import;
- public MCP catalogue;
- remote attestation;
- resource subscriptions;
- roots;
- sampling;
- sampling with tools;
- experimental tasks;
- rich server UI;
- inter-plugin MCP access;
- enterprise policy transport;
- mTLS;
- DPoP;
- device authorisation;
- private-network remote servers;
- public server revocation feed;
- Linux sandboxing;
- and macOS sandboxing.

---

# 178. Alternatives Rejected

Direct AI Router or Desktop transport ownership is rejected because protocol, token and approval authority would become fragmented.

Treating connection approval as blanket tool approval is rejected because tools can change and have different effects.

Trusting tool annotations is rejected because the specification explicitly treats them as untrusted unless the server is trusted.

Automatic Registry installation is rejected because local configuration can execute arbitrary code.

Production shell strings and mutable package runners are rejected because they hide executable resolution and install-time behaviour.

Local HTTP is not the default because stdio narrows local exposure and avoids DNS-rebinding listener risks.

Remote HTTP without TLS is rejected outside explicit loopback development.

Token passthrough is prohibited because it breaks audience boundaries and audit.

MCP sessions are not authentication.

Roots are deferred because they reveal paths and do not enforce access.

Sampling is deferred because it spends provider authority and may create server-controlled agent loops.

Experimental tasks are deferred because Opure already owns durable workflows and recovery.

Full-trust local servers remain Legacy mode only because protocol approvals cannot contain operating-system access.

---

# 179. Official and Primary Evidence References

## MCP Specification

- [MCP specification — 2025-11-25](https://modelcontextprotocol.io/specification/2025-11-25)
- [Base protocol overview](https://modelcontextprotocol.io/specification/2025-11-25/basic)
- [Lifecycle and capability negotiation](https://modelcontextprotocol.io/specification/2025-11-25/basic/lifecycle)
- [Transports](https://modelcontextprotocol.io/specification/2025-11-25/basic/transports)
- [Authorization](https://modelcontextprotocol.io/specification/2025-11-25/basic/authorization)
- [Schema reference](https://modelcontextprotocol.io/specification/2025-11-25/schema)
- [Cancellation](https://modelcontextprotocol.io/specification/2025-11-25/basic/utilities/cancellation)
- [Tasks](https://modelcontextprotocol.io/specification/2025-11-25/basic/utilities/tasks)

## Server Features

- [Tools](https://modelcontextprotocol.io/specification/2025-11-25/server/tools)
- [Resources](https://modelcontextprotocol.io/specification/2025-11-25/server/resources)
- [Prompts](https://modelcontextprotocol.io/specification/2025-11-25/server/prompts)
- [Completion](https://modelcontextprotocol.io/specification/2025-11-25/server/utilities/completion)

## Client Features

- [Sampling](https://modelcontextprotocol.io/specification/2025-11-25/client/sampling)
- [Elicitation](https://modelcontextprotocol.io/specification/2025-11-25/client/elicitation)

## Security and Registry

- [MCP Security Best Practices](https://modelcontextprotocol.io/docs/tutorials/security/security_best_practices)
- [MCP project security policy and trust model](https://github.com/modelcontextprotocol/modelcontextprotocol/security)
- [MCP Registry](https://modelcontextprotocol.io/registry/about)
- [MCP specification repository](https://github.com/modelcontextprotocol/modelcontextprotocol)

MCP specification revisions, OAuth guidance, SDK maturity and Registry status can change.

The implementation must revalidate the current stable specification and official security guidance before acceptance.

---

# 180. Review Record

| Date | Reviewer | Decision | Notes |
|---|---|---|---|
| 18 July 2026 | Architecture draft | Proposed | Trusted Gateway, exact server registration, safe OAuth and feature-level approval recommended |

---

# 181. Approval

## Founder or Product Approval

- **Name:** Christopher Dyer
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Local execution, remote data and tool consent policy review required

## MCP Gateway Approval

- **Name or role:** MCP Gateway and Trust Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Protocol, inventory and lifecycle evidence required

## Security Approval

- **Name or role:** Security Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Local command, OAuth, SSRF, prompt-injection and sandbox evidence required

## Network Approval

- **Name or role:** Network Gateway Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** HTTPS, redirects, DNS, private-address and OAuth discovery controls required

## Secrets Approval

- **Name or role:** Secrets Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Token storage and environment-secret canary evidence required

## AI Router Approval

- **Name or role:** AI Router Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Filtered tool projection and result boundary required

## Test Approval

- **Name or role:** Test Architecture Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Malicious MCP server corpus and lifecycle acceptance suite required

---

# 182. Supersession

This ADR is superseded only when a later ADR:

- names ADR-0018 explicitly;
- explains why server identity, transport, OAuth, feature permissions or sandbox policy changed;
- identifies registered-server and approval migration;
- describes token, workflow and project impact;
- explains protocol compatibility;
- and updates the `Superseded by` field.

Historical server identities, tool fingerprints, approvals and security evidence remain available according to retention policy.

---

# 183. Change History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | 18 July 2026 | Founder Draft | Initial trusted MCP Gateway, transport, OAuth, sandbox and feature-permission recommendation |

---

# 184. Final Decision Statement

> **Opure will provisionally implement MCP through a trusted Gateway that supports the stable `2025-11-25` protocol over exact local stdio or remote HTTPS Streamable HTTP registrations, treats Registry records and all server-provided tools, annotations, resources, prompts, instructions and results as untrusted metadata or content, sandboxes third-party local servers where compatible, uses OAuth 2.1 PKCE with SSRF-resistant discovery and Vault-held audience-bound tokens for protected remote servers, fingerprints every feature definition, requires separate data and operation approval rather than connection-wide trust, and initially withholds roots, sampling, resource subscriptions and experimental tasks, because MCP provides interoperability but deliberately leaves executable trust, data consent, tool safety, secret handling and model authority to the host.**

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**