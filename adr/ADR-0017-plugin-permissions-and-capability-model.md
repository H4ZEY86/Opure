# ADR-0017 — Plugin Permissions and Capability Model

## Architecture Decision Record

**Status:** Proposed
**Date:** 18 July 2026
**Decision owners:** Founder and Product Owner
**Technical owners:** Plugin Security and Capability Owner
**Reviewers:** Plugin Platform Owner, Plugin SDK Owner, Runtime Architecture Owner, Security Owner, Workspace Owner, Patch Service Owner, Network Gateway Owner, Secrets Owner, Process Supervisor Owner, Trust Centre Owner, Desktop Owner, Persistence Owner, Test Architecture Owner
**Supersedes:** None
**Superseded by:** None
**Related ADRs:** ADR-0001 through ADR-0016
**Related specifications:** CHARTER-001, SPEC-001 through SPEC-012
**Target milestone:** Plugin SDK Foundation through Version 1.0

---

## 1. Decision Summary

Opure should use a **deny-by-default, brokered object-capability model** for every third-party plugin operation.

A plugin package may **declare** requested permissions.

A developer or higher-precedence policy may **grant** a bounded subset of those permissions.

The Runtime may then mint a short-lived, opaque **capability lease** for one exact plugin instance, operation, project, resource scope and purpose.

Only a trusted Opure service may use that capability to perform the underlying action.

A plugin never receives ambient access to trusted services merely because it is installed or running.

The initial capability model should:

* deny all undeclared and ungranted authority;
* distinguish manifest permission requests, persistent grants, operation approvals and runtime capability leases;
* use stable versioned permission identifiers;
* reject unknown permissions;
* treat semantic broadening as a new permission or catalogue-major change;
* use canonical parameter schemas for file scope, destinations, commands, providers and data classes;
* permit policy to narrow but never broaden a plugin's declared request;
* make Deny win at every policy layer;
* bind persistent grants to plugin ID, publisher trust identity, approved source, channel, permission catalogue and permitted scope;
* bind runtime capability leases additionally to exact plugin version, package hash, Plugin Host process, authenticated IPC session, project, operation and resource generation;
* use opaque random server-side references rather than self-contained JWTs;
* keep capability policy and authority on trusted Runtime services;
* prevent capability delegation, export, persistence or reuse by another plugin;
* issue capabilities just in time;
* make them short lived;
* limit them by use count, bytes, calls, duration, concurrency and data class where relevant;
* revoke them immediately on plugin disable, update, uninstall, permission change, project close, publisher change, source transfer, security event or Plugin Host termination;
* use explicit approval-plan hashes to prevent permission-review time-of-check/time-of-use changes;
* make AI unable to approve permissions, capabilities or high-risk operations;
* pass opaque project, snapshot, file, patch, build-target, network-request, provider-request and secret-use references rather than raw privileged objects;
* keep source-file reads brokered and snapshot bound;
* deny or redact secret-classified content before plugin delivery;
* allow plugins to propose patches but never apply them directly;
* allow plugins to request builds, tests and approved tools but never spawn arbitrary processes directly;
* allow network requests only through the Network Gateway;
* allow provider inference only through the AI Router and project cloud policy;
* allow secret **use** through destination-bound handles but never secret read, reveal, enumerate or export;
* provide only plugin-owned data access through a bounded data capability;
* rate-limit diagnostics and notifications;
* prohibit direct registry, device, clipboard, screen-capture, package-management, process-injection and arbitrary filesystem authority;
* run production third-party Plugin Hosts in a unique Windows AppContainer isolation profile with no network capability;
* prefer Less-Privileged AppContainer hardening when compatibility is proven;
* grant the AppContainer SID read/execute access only to the immutable plugin payload and narrowly required host assets;
* grant write access only to plugin-owned data and temporary scopes;
* grant IPC access only to an explicitly ACL-protected local endpoint;
* apply application-layer session authentication in addition to Windows ACLs;
* place every Plugin Host in a non-breakaway Windows Job Object;
* use kill-on-job-close, active-process, memory, CPU and accounting limits;
* prohibit child-process creation;
* pass no unnecessary inheritable handles;
* apply compatible Windows process mitigations before plugin code loads;
* expose no plugin-created top-level UI process;
* and permit a normal medium-integrity full-trust Plugin Host only in explicit Development Mode with prominent warnings and no production trust claim.

The permission catalogue should define:

1. **Intrinsic host capabilities**
   Narrow functions necessary for plugin lifecycle, package reads, plugin-owned data, configuration, bounded diagnostics and cancellation.

2. **Project read capabilities**
   Project metadata, file enumeration, bounded file snapshots, search, repository status and selected history.

3. **Proposal capabilities**
   Patch proposals, command proposals, build requests, test requests and workflow contributions.

4. **External communication capabilities**
   Network requests, provider inference and MCP-mediated requests, all through trusted gateways.

5. **Sensitive-use capabilities**
   Destination-bound secret use and explicitly classified project-data transmission.

6. **UI contribution capabilities**
   Declarative commands, status and notifications, without arbitrary in-process UI code.

7. **Prohibited ambient authorities**
   Raw filesystem, raw sockets, arbitrary process creation, direct Vault reads, direct repository mutation, direct patch application, package installation, service control, registry mutation, device access and process injection.

The initial production Windows sandbox should use:

> **One unique regular AppContainer profile per plugin identity and publisher trust identity, with no network capabilities, explicit DACL grants, a dedicated authenticated named pipe and Job Object limits.**

A Less-Privileged AppContainer should be the preferred hardening target.

LPAC may become mandatory if the prototype proves .NET, package-host execution, named-pipe IPC and required library access.

A regular AppContainer is acceptable for Version 1.0 only after an access inventory proves that its built-in `ALL APPLICATION PACKAGES` visibility does not expose unacceptable resources.

A same-user full-trust or merely restricted-token process is not an acceptable production boundary for arbitrary third-party native or managed code.

The selected model is intentionally defence in depth:

```text
Package trust
    ↓
Permission declaration
    ↓
Developer and policy grant
    ↓
AppContainer operating-system isolation
    ↓
Authenticated IPC
    ↓
Short-lived object capability
    ↓
Trusted broker validation
    ↓
Service-owned operation
    ↓
Trust Centre evidence
```

No one layer is described as a complete sandbox or proof of plugin safety.

---

## 2. Status

Current status:

> **Proposed**

This ADR becomes **Accepted** only after a prototype demonstrates:

* permission catalogue version 1;
* canonical permission parameters;
* manifest validation;
* install-time permission review;
* operation-time approval;
* immutable approval-plan hashes;
* persistent grant storage;
* opaque capability leases;
* authenticated session binding;
* plugin-process binding;
* exact package-hash binding;
* project and snapshot binding;
* lease expiry;
* use-count and quota enforcement;
* immediate revocation;
* non-delegation;
* no raw service access;
* brokered project reads;
* secret-content denial or redaction;
* patch proposal without direct application;
* brokered build and test requests;
* brokered network request;
* provider inference through cloud policy;
* destination-bound secret use without disclosure;
* plugin-owned data scope;
* a unique AppContainer profile;
* no direct network from Plugin Host;
* explicit package and data DACLs;
* authenticated named-pipe IPC;
* non-breakaway Job Object;
* child-process denial;
* memory and CPU limits;
* compatible process mitigations;
* plugin update permission diff;
* publisher-change revocation;
* Stable and Preview isolation;
* security-event quarantine;
* and an adversarial escape and capability-confusion test suite.

---

## 3. Context

ADR-0016 defines an inspectable plugin package and an out-of-process Plugin Host.

That is necessary but insufficient.

A separate process running as the same Windows user can ordinarily access much of that user's:

* filesystem;
* network;
* environment;
* credentials;
* processes;
* clipboard;
* registry;
* and profile data.

A managed `AssemblyLoadContext` is not a security boundary.

A named-pipe protocol is not a permission model.

A manifest permission list does not itself enforce access.

A user clicking “Install” does not imply consent for every future operation.

A signed package can still contain vulnerable or malicious code.

An AppContainer can restrict direct operating-system access, but it does not decide whether Opure should let a plugin:

* read one project;
* read every project;
* send source code to one host;
* use one provider;
* consume one secret;
* run one build;
* propose one patch;
* or operate forever.

Those product decisions require a capability model above the operating-system sandbox.

Likewise, an in-product capability check cannot stop deliberately malicious native code from calling Windows APIs directly when the process has ambient same-user access.

That requires an operating-system boundary below the broker.

Opure therefore needs two complementary layers:

1. **Windows-enforced isolation** for direct operating-system access.
2. **Opure-enforced object capabilities** for trusted product services.

The model must also distinguish several concepts that products often blur:

* a plugin asks for permission;
* the developer grants a maximum envelope;
* a specific operation may require a fresh approval;
* the Runtime issues a temporary capability;
* and a trusted service performs the action.

This distinction is essential for inspectability and revocation.

---

## 4. Problem Statement

Opure requires a plugin permission and capability architecture that gives plugins only the minimum operation-specific authority needed, keeps sensitive actions visible and revocable, prevents ambient access to trusted services, resists confused-deputy and time-of-check/time-of-use attacks, and combines brokered product authority with Windows-enforced isolation strong enough for untrusted third-party executable code.

---

## 5. Decision Drivers

The decision is evaluated against:

* Charter alignment;
* developer control;
* least privilege;
* deny by default;
* inspectability;
* operation-specific consent;
* project isolation;
* source-code safety;
* secret safety;
* network consent;
* cloud policy;
* process isolation;
* immediate revocation;
* plugin updates;
* publisher continuity;
* auditability;
* compatibility;
* performance;
* Windows 11 support;
* managed and native plugin code;
* small-team implementation;
* future enterprise policy;
* future cross-platform execution;
* and testability.

---

## 6. Governing Principles

This decision must preserve:

* Developer Respect;
* Developer First;
* Human in Control;
* Local by Design;
* Cloud Optional;
* Visible by Design;
* Inspectable Decisions;
* Least Privilege;
* Deny by Default;
* No Ambient Authority;
* No Raw Secret Disclosure;
* No Direct Patch Application;
* No Direct Repository Mutation;
* No Raw Socket;
* No Arbitrary Process Spawn;
* No Hidden Permission Expansion;
* No Permission by Display Name;
* No Trust by Signature Alone;
* No Capability Delegation;
* No Long-Lived Bearer Authority;
* No Policy Broadening;
* No AI Approval;
* No Same-User Full-Trust Production Plugin;
* No False Sandbox Claim;
* Reversible Grants;
* Immediate Revocation;
* and Evidence-Based Confidence.

---

## 7. Scope

This ADR decides:

* permission terminology;
* permission catalogue architecture;
* permission identifiers;
* parameter schemas;
* risk levels;
* manifest declarations;
* policy layering;
* persistent grants;
* operation approvals;
* capability leases;
* capability binding;
* capability expiry;
* capability revocation;
* broker protocols;
* project-file access;
* patch proposals;
* repository access;
* build and test requests;
* tool requests;
* network requests;
* provider inference;
* secret use;
* plugin-owned data;
* diagnostics and notifications;
* inter-plugin authority;
* Windows AppContainer policy;
* Job Object policy;
* process mitigations;
* UI and approval behaviour;
* update permission diffs;
* incident response;
* persistence;
* observability;
* and acceptance tests.

This ADR does not decide:

* every final permission ID;
* every Plugin SDK API;
* rich plugin UI composition;
* Linux sandboxing;
* macOS sandboxing;
* WebAssembly capability bindings;
* external-process plugin protocol;
* enterprise policy transport;
* remote attestation;
* kernel driver isolation;
* antivirus policy;
* public plugin revocation service;
* or a marketplace permission-review programme.

---

## 8. Constraints

Known constraints include:

* Plugins are executable third-party code.
* Plugins may include native libraries.
* Plugins run through `Opure.PluginHost`.
* The first target is Windows 11 x64.
* The main Opure package is full trust at medium integrity.
* A full-trust child process inherits broad same-user authority unless explicitly isolated.
* Windows AppContainer restricts files, registry, network, credentials, process and window interaction.
* An AppContainer uses a unique SID and capability SIDs in Windows access checks.
* An AppContainer runs at low integrity.
* LPAC removes access normally inherited through `ALL APPLICATION PACKAGES` and requires more explicit grants.
* A dynamically launched AppContainer requires profile, SID, process attributes and DACL planning.
* An AppContainer without network capability cannot open ordinary network connections.
* Named pipes are securable Windows objects with ACL-controlled access.
* Windows Job Objects can manage a process tree, enforce limits, account usage and terminate all associated processes.
* process mitigation policies can restrict extension points, child process creation, image loading and other behaviours;
* .NET JIT requires dynamic code, so a blanket dynamic-code prohibition is likely incompatible;
* plugin package files and plugin data live outside the immutable Opure MSIX package;
* same-user malware remains outside the complete protection of this model;
* a plugin can request trusted services only through authenticated IPC;
* ADR-0004 requires application-layer session authentication;
* ADR-0007 prohibits raw secret values in normal plugin contracts;
* ADR-0009 requires Windows-aware path validation;
* ADR-0016 binds package identity to source and publisher trust;
* and production third-party plugins must remain disabled if the required sandbox cannot be established.

---

## 9. Assumptions

This decision assumes:

* the Plugin Host can run inside a dynamically created AppContainer;
* the AppContainer SID can receive read/execute access to the host and immutable plugin package paths;
* the AppContainer SID can receive bounded write access to plugin-owned data and temporary paths;
* a named pipe can be ACL'd for the AppContainer SID and Opure Runtime identity;
* .NET 10 can initialise inside the selected AppContainer profile;
* the Plugin Host requires no direct network;
* the Plugin Host requires no top-level window;
* child process creation can be disabled;
* Job Object limits can be set before untrusted plugin code runs;
* capability state can remain server side;
* IPC sessions can identify one exact Plugin Host process;
* persistent grants can be stored separately from ephemeral leases;
* trusted services can expose narrow broker methods;
* all project mutation can remain service owned;
* and a future non-Windows sandbox can preserve the same logical permission and capability contracts.

---

## 10. Current Platform Evidence

Official Microsoft documentation available on 18 July 2026 establishes that:

* AppContainer is a Windows process and resource isolation boundary;
* AppContainer restricts file, registry, network, credential, device, process and window access;
* access can be granted through AppContainer and capability SIDs on resource DACLs;
* access is the intersection of ordinary user rights and AppContainer rights;
* an AppContainer runs at low integrity;
* no network capability means ordinary network access is unavailable;
* LPAC is more restrictive than a regular AppContainer and does not rely on resources granted to all application packages;
* a dynamically launched AppContainer or LPAC can be created with a profile, SID, capabilities and process-creation attributes;
* full-trust medium-integrity packaged desktop applications do not receive AppContainer restrictions merely because they are MSIX packaged;
* named-pipe access is controlled through Windows security descriptors and DACLs;
* named pipes should not rely on permissive default ACLs;
* Job Objects can group processes, enforce resource limits, prevent breakaway and terminate the associated process tree;
* Job Objects support active-process and memory limits and kill-on-job-close;
* Windows process mitigation policy supports DEP, ASLR, strict-handle checks, extension-point restrictions, image-load restrictions, Control Flow Guard, child-process restrictions and other mitigations;
* and process mitigations are most effective when applied during or before process initialisation.

All exact process-creation flags, AppContainer grants, .NET compatibility and mitigation combinations must be proven on the selected Windows 11 and .NET 10 baseline.

---

## 11. Terminology

### 11.1 Permission Declaration

A manifest statement that a plugin may need a class of authority.

It grants nothing.

---

### 11.2 Permission Catalogue

The versioned Opure definition of:

* permission ID;
* meaning;
* parameters;
* risk;
* persistence;
* approval requirements;
* broker;
* quotas;
* and audit fields.

---

### 11.3 Persistent Grant

A stored developer or policy decision permitting a bounded permission envelope.

It does not itself authorise a live service call.

---

### 11.4 Operation Approval

A decision for one exact dynamic plan, such as:

* send these files to this host;
* run this build target;
* or use this secret for this destination.

---

### 11.5 Capability Lease

An opaque, short-lived server-side authority reference minted for one exact plugin instance and operation.

---

### 11.6 Broker

A trusted Opure service that validates a capability and performs or delegates the underlying action.

---

### 11.7 Resource Reference

An opaque identifier for a trusted resource such as:

* project;
* snapshot;
* file;
* patch proposal;
* build target;
* provider;
* secret binding;
* or network request.

---

### 11.8 Grant Envelope

The maximum authority a plugin may request at runtime under current policy.

---

### 11.9 Hard Deny

Authority that is unavailable to third-party plugins regardless of manifest request or user preference.

---

### 11.10 Sandbox

The Windows-enforced process isolation boundary.

It is distinct from Opure's logical permission model.

---

## 12. Options Considered

The principal logical-authority options are:

1. Brokered object capabilities.
2. Role-based access control.
3. Static install-time permissions only.
4. Direct trusted-service interfaces with runtime checks.
5. Path and URL allowlists passed to the plugin.
6. Signed self-contained capability tokens.
7. User-account operating-system permissions only.
8. Full-trust Plugin Host with audit logging.

The principal Windows execution options are:

1. Unique AppContainer plus Job Object.
2. Less-Privileged AppContainer plus Job Object.
3. Restricted token plus Job Object.
4. Standard medium-integrity process plus Job Object.
5. Windows Sandbox or virtual machine per plugin.
6. WebAssembly sandbox.
7. No operating-system sandbox.

---

# 13. Option A — Brokered Object Capabilities

## 13.1 Advantages

* no ambient service authority;
* fine-grained scope;
* operation binding;
* easy revocation;
* explicit expiry;
* quotas;
* project isolation;
* confused-deputy resistance;
* clean audit;
* works across IPC;
* maps to future non-Windows hosts;
* separates persistent consent from live authority;
* and supports trusted service ownership.

---

## 13.2 Disadvantages

* more contract design;
* more state;
* every broker must validate consistently;
* capability lifecycle bugs can be serious;
* UI must explain dynamic scope;
* and fine granularity can become complex.

---

## 13.3 Decision

Selected.

---

# 14. Role-Based Access Control

RBAC is useful for human and enterprise administration.

It is too coarse as the primary plugin runtime authority because a role such as “project reader” does not express:

* one project;
* one snapshot;
* one file set;
* one purpose;
* one expiry;
* one use count;
* or one destination.

RBAC may restrict grant administration but does not replace capabilities.

---

# 15. Static Install-Time Permissions Only

Rejected because dynamic parameters and project context are unavailable at installation.

An install-time “network” permission cannot safely approve every future destination and payload.

---

# 16. Direct Service Interfaces with Checks

Rejected as the primary model because broad service objects create ambient authority and make accidental methods reachable.

Plugins receive narrow broker contracts and opaque references.

---

# 17. Raw Paths and URL Allowlists

Rejected as the primary authority representation because strings are forgeable, vulnerable to canonicalisation errors and do not bind to resource generation or operation context.

Trusted services may use canonical paths internally.

Plugins use opaque resource references.

---

# 18. Self-Contained Signed Tokens

JWT-like or self-contained signed tokens are not selected for local plugin capabilities.

Disadvantages include:

* bearer-token leakage;
* larger IPC;
* difficult immediate revocation;
* parsing and cryptographic surface;
* accidental logging;
* and unnecessary cross-service trust distribution.

Server-side opaque references are simpler for one local Runtime.

---

# 19. Operating-System User Permissions Only

Rejected because running as the user gives plugins far more authority than the product intends.

---

# 20. Full-Trust Host with Audit Only

Rejected for production because logging a violation after direct filesystem, network or credential access is not prevention.

---

# 21. Windows Sandbox Options

## 21.1 Regular AppContainer

Provides strong Windows-enforced isolation while retaining broader compatibility than LPAC.

Selected as the Version 1.0 production baseline, subject to access-inventory evidence.

---

## 21.2 LPAC

Provides a smaller default resource set and is the preferred hardening target.

Selected for prototype and possible Version 1.0 promotion.

---

## 21.3 Restricted Token

Can remove privileges and add restricting SIDs, but does not inherently provide the same file, network, credential and window isolation as AppContainer.

Development fallback only.

---

## 21.4 Medium-Integrity Job Object

Job Objects limit resources and process trees but do not remove ambient same-user filesystem or network authority.

Development fallback only.

---

## 21.5 Windows Sandbox or VM

Strong but too heavy for ordinary plugin startup, integration and persistent state.

Deferred for exceptionally untrusted tools or manual analysis.

---

## 21.6 WebAssembly

Promising for future plugin models but not a replacement for the initial managed .NET ecosystem.

Deferred.

---

# 22. Decision

Opure will provisionally adopt:

> **A versioned deny-by-default permission catalogue, persistent bounded grants, operation-specific approvals and opaque server-side capability leases enforced by trusted brokers, combined on Windows with one unique no-network AppContainer and non-breakaway resource-limited Job Object per third-party plugin.**

This decision is:

* [ ] Permanent until superseded
* [x] Provisional pending capability and AppContainer evidence
* [ ] Permission catalogue finalisation
* [ ] Cross-platform sandbox approval
* [ ] Rich plugin UI approval

---

# 23. Authority Model

The complete authority chain is:

```text
Package manifest request
    ↓
Catalogue validation
    ↓
Hard product policy
    ↓
Enterprise restriction
    ↓
Channel and profile restriction
    ↓
Project policy
    ↓
Persistent developer grant
    ↓
Operation-specific plan
    ↓
Fresh approval where required
    ↓
Capability lease
    ↓
Trusted broker
    ↓
Service-owned action
```

Every layer may narrow or deny.

No layer may broaden beyond the plugin's declared request or the product hard maximum.

---

# 24. Principal Model

The capability system recognises distinct principals.

## 24.1 Developer Principal

The human operating Opure.

---

## 24.2 Enterprise Policy Principal

A future managed policy source that may restrict or preconfigure approved boundaries.

It cannot grant a hard-denied authority.

---

## 24.3 Plugin Package Principal

Identified by:

```text
plugin_id
plugin_version
package_sha256
publisher_trust_identity
source_id
channel
```

---

## 24.4 Plugin Instance Principal

Identified additionally by:

```text
plugin_host_process_id
plugin_host_start_time
appcontainer_sid
ipc_session_id
runtime_instance_id
instance_nonce
```

Process ID alone is insufficient because Windows may reuse it.

---

## 24.5 Project Principal

Identified by an opaque project ID and workspace generation.

---

## 24.6 Service Principal

A trusted Opure service such as:

* Workspace Service;
* Patch Service;
* Repository Service;
* Build Service;
* Network Gateway;
* AI Router;
* Secrets Vault;
* Memory Service;
* or Plugin Data Service.

---

# 25. Policy Layers

Policy is evaluated in this order.

## 25.1 Hard Product Policy

Defines:

* prohibited authority;
* supported permission IDs;
* maximum persistence;
* maximum quotas;
* required approval;
* and required broker.

It cannot be overridden by user or enterprise policy.

---

## 25.2 Enterprise Policy

May:

* deny permissions;
* constrain destinations;
* constrain package publishers;
* constrain plugin sources;
* constrain persistence;
* require operation approval;
* and set lower quotas.

---

## 25.3 Channel Policy

Stable may be stricter than Preview or Development.

---

## 25.4 Profile Grant

The developer's stored plugin grant envelope.

---

## 25.5 Project Policy

Defines:

* plugin allowed or denied;
* project file scope;
* cloud policy;
* command policy;
* provider policy;
* and project-specific persistence.

---

## 25.6 Operation Policy

Evaluates actual:

* files;
* destinations;
* methods;
* command;
* data classes;
* secret binding;
* provider;
* and side effects.

---

## 25.7 Runtime Conditions

Includes:

* plugin health;
* package trust;
* AppContainer state;
* active project;
* lease expiry;
* quota;
* cancellation;
* and revocation epoch.

---

## 25.8 Deny Precedence

Any deny wins.

Policy resolution must return:

* decision;
* reason;
* policy source;
* required approval;
* and effective narrowed scope.

---

# 26. Permission Catalogue

The permission catalogue is a source-controlled, versioned product artefact.

Suggested location:

```text
schemas/plugins/permission-catalogue-v1.json
```

---

## 26.1 Catalogue Version

The catalogue declares:

```text
opure.plugin-permissions/1
```

---

## 26.2 Permission Entry

Each entry contains:

```text
id
title
description
risk_level
manifest_parameters_schema
runtime_parameters_schema
allowed_grant_scopes
allowed_durations
operation_approval
broker_service
quota_types
data_classes
audit_fields
hard_denies
introduced_in
deprecated_in
replacement
```

---

## 26.3 Stable Meaning

A permission ID's meaning must not broaden silently.

A broader meaning requires:

* a new permission ID;
* or a new catalogue major version;
* migration;
* plugin update review;
* and developer reapproval.

---

## 26.4 Additive Catalogue Change

A new permission ID may be added within the same catalogue major when old IDs retain their meaning.

---

## 26.5 Unknown Permission

A package declaring an unknown permission is incompatible and cannot activate.

---

## 26.6 Deprecated Permission

A deprecated permission remains understood for a bounded compatibility period and maps only to an equal or narrower replacement.

---

# 27. Permission Identifier Rules

Permission identifiers are:

* lower-case;
* dot separated;
* ASCII;
* stable;
* action oriented;
* and product owned.

Examples:

```text
project.metadata.read
project.files.enumerate
project.files.read
project.search
repository.status.read
repository.history.read
project.patch.propose
build.request
test.request
tool.request
network.http.request
provider.inference.request
secret.use
project.memory.read
notification.emit
```

---

## 27.1 No Generic `admin`

The catalogue contains no `admin`, `full_access`, `all` or wildcard permission.

---

## 27.2 No Implicit Hierarchy

Possessing:

```text
project.files.read
```

does not imply:

```text
project.patch.propose
```

unless catalogue policy explicitly defines a relationship.

---

## 27.3 Permission Set Canonicalisation

Permission requests and grants are canonicalised before hashing.

Canonicalisation includes:

* sorted IDs;
* canonical JSON parameters;
* normalised host names;
* normalised relative path patterns;
* explicit defaults;
* and catalogue version.

---

# 28. Risk Levels

The initial risk levels are:

* **R0 — Intrinsic**
* **R1 — Low**
* **R2 — Moderate**
* **R3 — High**
* **R4 — Restricted**
* **R5 — Prohibited**

---

## 28.1 R0 — Intrinsic

Narrow host functions necessary for plugin lifecycle.

No user grant, but still brokered and rate limited.

Examples:

* read immutable own package resources;
* read own non-secret configuration;
* read/write own plugin data within quota;
* emit bounded diagnostics;
* receive cancellation;
* report health.

---

## 28.2 R1 — Low

Read-only non-content project or product information.

May be persistently granted per project.

Examples:

* project display metadata;
* repository branch name;
* repository status summary;
* build-target names.

---

## 28.3 R2 — Moderate

Read project content or invoke a deterministic read-oriented service.

Requires explicit project grant.

Examples:

* enumerate project files;
* read bounded file snapshots;
* search source;
* read repository history;
* read project memory summaries.

---

## 28.4 R3 — High

May transmit data, consume secrets, execute tools or create a mutation proposal.

Usually requires operation-specific review or a narrowly preapproved policy.

Examples:

* HTTP request;
* provider inference with source content;
* secret use;
* build or test execution;
* tool request;
* patch proposal.

---

## 28.5 R4 — Restricted

Available only through a specialised trusted broker and never as direct authority.

Examples:

* a preapproved repository operation proposal;
* a preapproved command template;
* a constrained external service transaction;
* or high-sensitivity data transmission.

---

## 28.6 R5 — Prohibited

Unavailable to third-party plugins.

Examples:

* raw Vault read;
* direct patch application;
* direct Git commit;
* arbitrary process creation;
* raw sockets;
* broad filesystem access;
* process injection;
* package management;
* service control;
* arbitrary registry mutation;
* and device capture.

---

# 29. Manifest Permission Request

A plugin manifest requests permissions in a canonical structure.

Conceptual example:

```json
{
  "permissions": [
    {
      "id": "project.files.read",
      "scope": {
        "include": ["src/**", "tests/**"],
        "exclude": ["**/.env", "**/*.pfx"]
      },
      "reason": "Reads source and tests selected for repository review."
    },
    {
      "id": "network.http.request",
      "scope": {
        "hosts": ["api.example.com"],
        "methods": ["POST"],
        "data_classes": ["plugin.generated", "project.source"]
      },
      "reason": "Sends selected review input to the configured service."
    }
  ]
}
```

---

## 29.1 Reason Text

The plugin-provided reason is displayed as publisher text.

It is not trusted policy.

---

## 29.2 Maximum Request

The manifest declares the plugin's maximum expected authority.

The plugin cannot request an undeclared permission at runtime.

---

## 29.3 Optional Permission

A permission may be marked optional.

Declining it should leave the plugin functional in a declared reduced mode.

---

## 29.4 Required Permission

A required permission may block activation when declined.

The UI must state that choice before approval.

---

## 29.5 Dynamic Parameters

The manifest may declare maximum host or path patterns.

The actual operation must provide exact runtime parameters within those maxima.

---

# 30. Persistent Grants

A persistent grant records a bounded permission envelope.

Conceptual fields:

```text
grant_id
plugin_id
publisher_identity
source_id
channel
permission_catalogue
permission_id
scope
project_id
persistence
created_by
created_at
expires_at
policy_source
grant_hash
revocation_epoch
```

---

## 30.1 Exact Version Binding

Persistent grants may survive a compatible plugin version update only when:

* plugin ID is unchanged;
* publisher identity is unchanged;
* source binding is unchanged;
* permission request is equal or narrower;
* package update passes trust;
* and catalogue meaning is unchanged.

Runtime capability leases always bind the exact version and package hash.

---

## 30.2 Publisher Change

A publisher identity change suspends all persistent grants pending review.

---

## 30.3 Source Transfer

A source transfer suspends grants unless policy explicitly establishes equivalent trust.

---

## 30.4 Package Repack

A different hash for the same ID and version invalidates grants and quarantines the package.

---

# 31. Grant Scopes

Supported grant scopes include:

* Once;
* Until Plugin Host Stops;
* Until Project Closes;
* For This Project;
* For This Profile;
* Until Date;
* Enterprise Managed;
* and Denied.

---

## 31.1 No Global Device Grant Initially

A plugin permission is not granted across every Opure profile and channel by one click.

---

## 31.2 Project-Specific Default

Project content permissions default to one project.

---

## 31.3 High-Risk Persistence

High-risk permissions may prohibit Profile persistence.

---

## 31.4 Secret Persistence

A secret-use grant is bound to:

* one secret reference;
* one plugin;
* one publisher;
* one project or profile;
* one destination or provider;
* and one purpose class.

---

# 32. Operation Approval

A persistent grant may still require a fresh operation approval.

---

## 32.1 Approval Plan

The trusted broker constructs an approval plan containing:

```text
plugin_identity
plugin_version
package_hash
project
permission
exact_resources
destination
method
data_classes
secret_binding
command_or_target
side_effects
quota
expiry
reason
```

---

## 32.2 Plan Hash

The plan is canonicalised and hashed.

The approval UI approves that exact hash.

---

## 32.3 Mutation After Approval

Any material change to:

* files;
* destination;
* method;
* command;
* secret;
* provider;
* data class;
* patch;
* or quota

invalidates the approval and requires a new plan.

---

## 32.4 Approval Token

The UI returns an opaque, short-lived approval reference tied to the plan hash.

It is not a general capability.

---

## 32.5 No AI Approval

AI output cannot satisfy an approval requirement.

Only a deterministic policy or authenticated human action can approve.

---

# 33. Capability Lease

A capability lease is an opaque random reference stored server side.

---

## 33.1 No Self-Contained Token

The lease is not a JWT and contains no readable authority claims.

---

## 33.2 Entropy

Use cryptographically secure random identifiers with at least 128 bits of effective entropy.

The exact representation must avoid accidental logging and copying.

---

## 33.3 Server Record

The authoritative record contains:

```text
capability_id_hash
plugin_instance
plugin_package
ipc_session
runtime_instance
project_id
workspace_generation
permission_id
resource_scope
operation_plan_hash
issued_at
expires_at
remaining_uses
byte_quota
call_quota
concurrency_limit
revocation_epoch
state
```

The plain capability reference should not be persisted after process termination unless required for short crash reconciliation.

---

## 33.4 Session Binding

A capability is valid only on the authenticated IPC session for which it was issued.

---

## 33.5 Process Binding

A capability is valid only for the exact Plugin Host process identity and start time.

---

## 33.6 Package Binding

A capability is valid only for the exact plugin version and package SHA-256.

---

## 33.7 Project Binding

Project capabilities bind to one project ID and workspace generation.

---

## 33.8 Operation Binding

The capability permits only one broker method family or exact operation plan.

---

## 33.9 Expiry

Suggested provisional maximums:

* one-shot sensitive operation: 2 minutes;
* bounded read session: 5 minutes;
* project-session low-risk capability: 30 minutes;
* Plugin Host intrinsic capability: session lifetime;
* and no capability beyond host termination.

Exact values require performance evidence.

---

## 33.10 Use Count

Sensitive capabilities should be one shot.

Streaming reads may use a bounded sequence.

---

## 33.11 Quotas

Capabilities may limit:

* bytes read;
* files read;
* results returned;
* network bytes;
* provider tokens;
* command duration;
* build duration;
* notifications;
* and concurrent operations.

---

# 34. Capability Storage

Capability state belongs to the trusted Runtime capability service.

---

## 34.1 In-Memory First

Live capabilities should normally remain in memory.

---

## 34.2 Crash

A Runtime restart invalidates all live capabilities.

Plugins reconnect and request fresh authority.

---

## 34.3 No Plugin Persistence

A plugin may retain the opaque value in its process memory only.

It cannot persist and reuse it after reconnect.

---

## 34.4 Logging

Logs contain a truncated or hashed diagnostic reference, never the bearer capability.

---

# 35. Capability Validation

Every broker request validates:

1. capability exists;
2. state is Active;
3. Runtime instance matches;
4. IPC session matches;
5. Plugin Host process matches;
6. plugin ID, version and package hash match;
7. permission matches requested broker method;
8. project and workspace generation match;
9. resource scope includes request;
10. operation-plan hash matches where required;
11. lease is not expired;
12. use and byte quotas remain;
13. revocation epoch is current;
14. plugin remains enabled and trusted;
15. project remains open;
16. cancellation has not been requested;
17. and higher-precedence policy still permits the action.

A failure denies without partial execution.

---

# 36. Revocation

Revocation is immediate from the trusted service perspective.

---

## 36.1 Revocation Triggers

Triggers include:

* developer revoke;
* plugin disable;
* plugin update;
* uninstall;
* Plugin Host exit;
* Runtime restart;
* project close;
* workspace generation change;
* publisher change;
* source transfer;
* package quarantine;
* security incident;
* enterprise policy change;
* secret rotation;
* and operation cancellation.

---

## 36.2 Revocation Epoch

Maintain epochs at:

* Runtime;
* plugin;
* project;
* permission grant;
* secret binding;
* and security incident

levels where useful.

A capability with an old epoch fails.

---

## 36.3 In-Flight Operation

A revoked capability causes the broker to:

* cancel where safe;
* stop future chunks;
* avoid committing mutation;
* or complete a non-interruptible atomic boundary and report it honestly.

---

# 37. Non-Delegation

A plugin cannot give a capability to:

* another plugin;
* a child process;
* an MCP server;
* an external command;
* or a provider.

The broker validates the original plugin session.

---

## 37.1 Inter-Plugin Requests

One plugin may request a mediated service exposed by another plugin only through a future explicit inter-plugin contract.

Raw capability forwarding is prohibited.

---

# 38. Confused-Deputy Protection

A trusted broker must not rely only on a plugin-supplied resource ID.

It validates:

* caller identity;
* capability;
* intended resource;
* project;
* operation;
* and policy.

---

## 38.1 No Ambient Current Project

A plugin request must name an opaque project reference.

The broker does not infer “current project” from Desktop UI state.

---

## 38.2 No Ambient Current Secret

A plugin cannot ask for “the current provider key”.

It uses an explicit secret-use binding.

---

## 38.3 No Ambient Current User Path

A plugin cannot pass an arbitrary user path and ask a trusted service to open it.

---

# 39. Resource References

Resource references are opaque and typed.

Examples:

```text
ProjectRef
WorkspaceSnapshotRef
FileSnapshotRef
SearchResultRef
PatchProposalRef
RepositoryRef
BuildTargetRef
ToolTemplateRef
NetworkPlanRef
ProviderPlanRef
SecretUseRef
MemoryQueryRef
PluginDataRef
```

---

## 39.1 No Path as Authority

A relative path may appear as display or operation data.

Authority comes from the capability and trusted resource reference.

---

## 39.2 Generation Binding

References include or resolve against a generation.

Stale references fail safely.

---

## 39.3 Ownership

A reference minted for one plugin cannot be used by another.

---

# 40. Streaming

Large data is streamed through bounded broker protocols.

---

## 40.1 Backpressure

Every stream supports:

* bounded buffers;
* cancellation;
* byte quota;
* timeout;
* and backpressure.

---

## 40.2 Partial Results

A cancelled stream reports partial completion.

---

## 40.3 No Shared Memory Initially

Shared memory with plugins is deferred because handle transfer and lifetime increase the attack surface.

---

# 41. Intrinsic Host Capabilities

The Plugin Host receives only narrow intrinsic functions.

---

## 41.1 Package Resource Read

Read-only access to the plugin's immutable package payload.

This may be direct through AppContainer DACL or brokered for selected resources.

---

## 41.2 Plugin Data

Read and write the plugin's own data scope within quota.

---

## 41.3 Configuration

Read validated non-secret plugin configuration.

Secret fields remain references.

---

## 41.4 Diagnostics

Emit structured bounded diagnostics.

---

## 41.5 Health

Report readiness, health and capability registration.

---

## 41.6 Cancellation

Receive cancellation and shutdown.

---

## 41.7 Clock

Use ordinary process time APIs.

Trusted operation expiry remains server authoritative.

---

# 42. Initial Permission Catalogue

The following catalogue is provisional and must be finalised through SDK design.

| Permission                   | Risk | Primary broker                       | Persistent grant       |
| ---------------------------- | ---: | ------------------------------------ | ---------------------- |
| `project.metadata.read`      |   R1 | Project Service                      | Project                |
| `project.files.enumerate`    |   R2 | Workspace Service                    | Project                |
| `project.files.read`         |   R2 | Workspace Service                    | Project                |
| `project.search`             |   R2 | Search Service                       | Project                |
| `repository.status.read`     |   R1 | Repository Service                   | Project                |
| `repository.history.read`    |   R2 | Repository Service                   | Project                |
| `project.memory.read`        |   R2 | Memory Service                       | Project                |
| `project.patch.propose`      |   R3 | Patch Service                        | Project                |
| `build.request`              |   R3 | Build Service                        | Project or target      |
| `test.request`               |   R3 | Test Service                         | Project or target      |
| `tool.request`               |   R4 | Tool Mediator                        | Template only          |
| `network.http.request`       |   R3 | Network Gateway                      | Host/method/data class |
| `provider.inference.request` |   R3 | AI Router                            | Provider/data policy   |
| `secret.use`                 |   R4 | Secrets Vault and destination broker | Binding only           |
| `notification.emit`          |   R1 | Desktop Notification Service         | Profile                |
| `workflow.contribute`        |   R1 | Workflow Registry                    | Package                |
| `command.contribute`         |   R1 | Desktop Command Registry             | Package                |

---

## 42.1 Catalogue Is Not Final SDK

The table establishes architecture, not final API naming.

---

# 43. Prohibited Authorities

Third-party plugins cannot receive:

```text
filesystem.raw
filesystem.user-profile
network.socket
network.listen
network.loopback.raw
process.spawn
process.debug
process.inject
process.enumerate-sensitive
vault.read
vault.enumerate
secret.reveal
secret.export
patch.apply
repository.write.raw
git.commit.direct
git.push.direct
package.install
package.update
service.control
driver.install
registry.write.raw
clipboard.read
clipboard.write.raw
screen.capture
camera
microphone
device.raw
credential.manager
windows.authentication
desktop.ui.raw
runtime.service-locator
```

---

## 43.1 No User Override

The ordinary user cannot override an R5 hard deny.

---

## 43.2 First-Party Exception

A trusted internal product component requiring one of these authorities is not implemented as a third-party plugin permission.

It receives a separately reviewed service boundary.

---

# 44. Project Metadata

`project.metadata.read` may expose:

* project display name;
* project kind;
* active configuration;
* language summary;
* repository presence;
* and supported build targets.

It should not expose:

* full absolute root path unless required;
* remote credentials;
* environment secrets;
* or unrelated projects.

---

# 45. Project File Enumeration

`project.files.enumerate` returns bounded relative-path metadata from a trusted workspace snapshot.

---

## 45.1 Scope

The capability binds:

* project;
* workspace generation;
* include patterns;
* exclude patterns;
* maximum entries;
* and file classifications.

---

## 45.2 Path Rules

All path matching uses ADR-0009 canonical Windows-aware rules.

---

## 45.3 Hidden and Sensitive Files

Hidden, ignored or sensitive files may be excluded by policy even when a broad manifest pattern requests them.

---

# 46. Project File Read

`project.files.read` returns immutable file snapshots or bounded content streams.

---

## 46.1 No Raw Handle

The plugin does not receive an operating-system file handle to the project file.

---

## 46.2 Snapshot Binding

The returned resource binds:

* relative path;
* content hash;
* size;
* encoding;
* line-ending metadata;
* file classification;
* and workspace generation.

---

## 46.3 Maximum Read

The grant and capability limit:

* file count;
* total bytes;
* individual file bytes;
* binary access;
* and stream duration.

---

## 46.4 Changed File

If the file changes between plan and read:

* fail;
* or mint a new snapshot and require renewed approval when material.

Do not silently read different content under the old approval.

---

## 46.5 Binary Files

Binary access is denied by default.

A future explicit binary-read parameter may permit selected formats.

---

# 47. Secret-Classified Project Content

Secret detection occurs before project content is delivered to a plugin.

---

## 47.1 Known Secret Stores

Files such as:

* Vault stores;
* private keys;
* credential caches;
* package-signing material;
* and protected Opure security state

are hard denied.

---

## 47.2 Likely Secret Content

When likely secret material appears in an otherwise readable source file:

* deny the affected content;
* offer a redacted representation where technically useful;
* identify the classification without displaying the secret;
* and require the developer to remediate the source or use a future explicitly reviewed sensitive-content workflow.

---

## 47.3 No Generic Override in Version 1

Version 1 does not provide a broad “send secret anyway” plugin permission.

---

# 48. Project Search

`project.search` executes through trusted indexing or Workspace services.

---

## 48.1 Query Limits

Limit:

* query complexity;
* result count;
* result bytes;
* file scope;
* and execution time.

---

## 48.2 Search Results

Results contain bounded excerpts and resource references.

They do not automatically grant full-file reads.

---

# 49. Repository Read Capabilities

Repository permissions are split.

---

## 49.1 Status

`repository.status.read` may expose:

* branch;
* clean or dirty status;
* staged count;
* unstaged count;
* untracked count;
* and operation state.

---

## 49.2 History

`repository.history.read` may expose bounded:

* commit metadata;
* changed paths;
* diff summaries;
* and selected commit content

through Repository Service.

---

## 49.3 Credentials

No repository permission exposes:

* credential helper values;
* tokens;
* SSH private keys;
* or remote passwords.

---

## 49.4 Repository Mutation

Version 1 contains no direct repository-write permission.

A plugin may propose an operation for review through a future deterministic repository-action contract.

---

# 50. Patch Proposal

`project.patch.propose` lets a plugin submit a proposed patch to the trusted Patch Service.

---

## 50.1 Proposal Only

The plugin cannot:

* write source files;
* stage Git changes;
* commit;
* or apply the patch.

---

## 50.2 Base Evidence

A proposal includes:

* project;
* base workspace generation;
* base file hashes;
* affected relative paths;
* patch operations;
* reason;
* and plugin identity.

---

## 50.3 Trusted Validation

Patch Service validates:

* path boundaries;
* base hashes;
* encoding;
* line endings;
* binary policy;
* generated-file policy;
* secret introduction;
* and policy.

---

## 50.4 Developer Review

The developer reviews the normal Opure patch plan.

Plugin permission does not replace patch approval.

---

## 50.5 Apply Authority

Only Patch Service applies an approved patch.

---

# 51. Build Request

`build.request` asks Build Service to execute a known project build target.

---

## 51.1 Target Reference

The plugin receives a `BuildTargetRef`, not an arbitrary command line.

---

## 51.2 Scope

The capability binds:

* project;
* target;
* configuration;
* environment profile;
* timeout;
* output class;
* and resource quota.

---

## 51.3 Environment

Build Service constructs the environment.

The plugin cannot inject arbitrary environment variables unless a separate parameter is approved.

---

## 51.4 Output

The plugin receives bounded structured results and selected logs.

Secret redaction applies.

---

# 52. Test Request

`test.request` behaves similarly to build requests.

It may bind:

* test target;
* filter;
* configuration;
* timeout;
* result quota;
* and output visibility.

---

# 53. Tool Request

`tool.request` is restricted.

---

## 53.1 Templates

The plugin may request only a trusted tool template such as:

```text
dotnet.format.check
git.diff.read
compiler.analyse
```

The exact catalogue is owned by Tool Mediator.

---

## 53.2 No Arbitrary Executable

The plugin cannot submit an arbitrary executable path.

---

## 53.3 No Arbitrary Shell

The plugin cannot submit arbitrary shell text.

---

## 53.4 Parameters

Template parameters are typed, validated and shell-free where possible.

---

## 53.5 Approval

A high-impact tool may require operation approval even when the template is persistently granted.

---

# 54. Network HTTP Request

`network.http.request` requests a bounded HTTP operation through Network Gateway.

---

## 54.1 No Socket

The Plugin Host has no direct network capability.

---

## 54.2 Plan

The trusted network plan contains:

* scheme;
* canonical host;
* port;
* path class;
* method;
* headers class;
* body data classes;
* maximum request bytes;
* maximum response bytes;
* redirect policy;
* timeout;
* and credential binding.

---

## 54.3 Scheme

HTTPS is required by default.

HTTP, file, FTP, SMB and custom schemes are denied unless a future specialised policy approves them.

---

## 54.4 Host Scope

Manifest host patterns are maximums.

Actual requests use exact canonical destinations.

---

## 54.5 IP Literals

IP-literal destinations are denied by default.

---

## 54.6 DNS Rebinding

Network Gateway validates resolved destinations according to its policy and prevents a public hostname from silently reaching prohibited local or private ranges.

---

## 54.7 Localhost

Loopback access is denied by default.

A future explicit local-service capability requires a separate risk review.

---

## 54.8 Redirects

Redirects require:

* bounded count;
* HTTPS preservation;
* allowlisted destination;
* and credential stripping across origin changes.

---

## 54.9 Headers

Plugins cannot set restricted or credential headers directly.

---

## 54.10 Request Body

The broker classifies outgoing data.

Sending project source, project memory, diagnostics or personal data may require operation approval.

---

## 54.11 Response

The plugin receives bounded response status, selected headers and content.

No automatic file execution.

---

# 55. Network Data Classes

Initial data classes may include:

* `plugin.generated`;
* `plugin.configuration.public`;
* `project.metadata`;
* `project.source`;
* `project.diff`;
* `project.memory`;
* `diagnostics.redacted`;
* `personal.data`;
* and `secret.material`.

`secret.material` is never delivered as a raw plugin-controlled body.

---

# 56. Provider Inference

`provider.inference.request` routes through AI Router.

---

## 56.1 Project Cloud Policy

Every request is constrained by the project's cloud policy:

* Local Only;
* Ask Every Time;
* Approved Providers Only;
* or Custom.

---

## 56.2 No API Key

The plugin never receives the provider API key.

---

## 56.3 Context Plan

The request plan identifies:

* provider;
* model class;
* context sources;
* data classifications;
* token budget;
* expected output;
* and purpose.

---

## 56.4 Approval

Cloud transmission follows project policy and may require fresh approval.

---

## 56.5 Output

The plugin receives provider output only through a bounded response.

---

## 56.6 No Hidden Chain of Thought

Provider or plugin contracts do not request hidden chain-of-thought material as product data.

---

# 57. MCP Requests

MCP-mediated plugin access is not automatically granted by provider or network permission.

A future permission should identify:

* MCP server;
* tool;
* arguments schema;
* data classes;
* and approval.

Until then, third-party plugins cannot invoke arbitrary MCP tools.

---

# 58. Secret Use

`secret.use` permits use of a secret without disclosing its value to the plugin.

---

## 58.1 SecretUseRef

A trusted service creates a binding containing:

* secret record;
* plugin identity;
* publisher identity;
* source;
* project or profile;
* destination or provider;
* operation class;
* allowed header or protocol placement;
* expiry;
* and revocation epoch.

---

## 58.2 No Read

The plugin cannot:

* read;
* reveal;
* enumerate;
* copy;
* log;
* export;
* or transform

the raw secret.

---

## 58.3 Final Boundary Injection

The trusted destination broker injects the secret at the final network or process boundary.

---

## 58.4 Destination Binding

A secret authorised for:

```text
api.example.com
```

cannot be used for another host.

---

## 58.5 Header Binding

A token authorised for one header or authentication scheme cannot be repurposed as body content.

---

## 58.6 Response Leakage

Network Gateway applies response and diagnostic redaction.

A remote service could still echo a secret; canary and redaction tests are required.

---

## 58.7 User Review

The UI shows:

* secret display label;
* destination;
* plugin;
* purpose;
* persistence;
* and last use.

It never displays the secret value.

---

# 59. Project Memory Read

`project.memory.read` provides bounded, classified memory records.

---

## 59.1 No Global Memory

A plugin cannot search every Opure profile or project.

---

## 59.2 Classification

Secret, credential and prohibited records are excluded.

---

## 59.3 Provenance

Returned memory includes safe provenance and freshness metadata.

---

## 59.4 Output Quota

Limit records, bytes and semantic query cost.

---

# 60. Plugin Data

Plugin-owned data is intrinsic but bounded.

---

## 60.1 Logical API

Preferred operations:

* read value;
* write value;
* enumerate own namespace;
* open bounded file;
* delete own value;
* and transaction.

---

## 60.2 Direct Directory Access

The AppContainer may receive direct DACL access to its own data directory for library compatibility.

Trusted Opure state remains elsewhere.

---

## 60.3 Quota

Enforce:

* total bytes;
* files;
* individual file size;
* transaction duration;
* and write rate.

---

## 60.4 No Cross-Plugin Access

One plugin cannot open another plugin's data.

---

## 60.5 Backup

Plugin data backups remain service owned.

---

# 61. Configuration

The plugin receives validated configuration values.

---

## 61.1 Secret Fields

Secret fields are represented by secret bindings.

---

## 61.2 Environment

Do not expose the full process environment.

Construct a minimal environment containing only required runtime variables and safe plugin metadata.

---

## 61.3 User Paths

Avoid exposing unrelated absolute user paths.

---

# 62. Diagnostics

Plugins may emit bounded structured diagnostics.

---

## 62.1 Allowed Fields

Allowlisted fields include:

* event ID;
* severity;
* message template;
* operation ID;
* safe dimensions;
* and exception category.

---

## 62.2 Prohibited Fields

Reject or redact:

* secret values;
* raw HTTP headers;
* entire source files;
* prompts;
* access tokens;
* full environment;
* capability references;
* and arbitrary serialized objects.

---

## 62.3 Rate Limit

Per-plugin rate and byte limits prevent log flooding.

---

## 62.4 Attribution

Every record is attributed to:

* plugin ID;
* version;
* package hash;
* and host instance.

---

# 63. Notifications

`notification.emit` creates bounded in-product notifications.

---

## 63.1 No Native Toast Initially

Plugins do not create arbitrary Windows notifications directly.

---

## 63.2 Content

Notification content is plain text or safe structured content.

---

## 63.3 Rate Limit

Notification spam triggers throttling or disablement.

---

# 64. Command Contributions

`command.contribute` permits declarative registration of plugin commands.

---

## 64.1 Declaration

Commands are declared in package metadata or activation response.

---

## 64.2 Invocation

User invocation causes the Runtime to mint only the capabilities required for that command under current policy.

---

## 64.3 No Hidden Shortcut

A command contribution does not grant project or network access.

---

# 65. Workflow Contributions

`workflow.contribute` registers declarative workflow definitions.

The Workflow Engine remains authoritative.

A plugin-contributed workflow cannot bypass:

* approvals;
* capability issuance;
* project policy;
* or service ownership.

---

# 66. UI Contributions

Version 1 supports only declarative, constrained UI contributions such as:

* command;
* status item;
* configuration schema;
* results view schema;
* and notification.

---

## 66.1 No Arbitrary In-Process UI

Plugin assemblies do not load into Desktop.

---

## 66.2 No Arbitrary HWND

The Plugin Host does not create normal top-level application windows.

---

## 66.3 Rich UI

Rich remote or embedded plugin UI is deferred to a separate ADR.

---

# 67. Clipboard

Direct clipboard permission is prohibited initially.

A future copy action should pass specific text through a trusted Desktop command with explicit user action.

---

# 68. Process and Command Authority

The Plugin Host cannot create child processes.

---

## 68.1 Job Limit

Set active process limit to one where compatible.

---

## 68.2 Child Process Mitigation

Enable Windows child-process creation denial where compatible.

---

## 68.3 Tool Broker

All executable tool use occurs through Tool Mediator outside the Plugin Host sandbox.

---

# 69. Direct Filesystem Authority

The AppContainer receives filesystem DACLs only for:

* package payload read/execute;
* own data read/write;
* own temporary directory;
* and narrowly required runtime host assets.

No direct DACL is granted to project repositories.

Project access remains brokered.

---

# 70. Direct Network Authority

The AppContainer receives no:

* `internetClient`;
* `internetClientServer`;
* `privateNetworkClientServer`;
* or equivalent network capability.

All network access is brokered.

---

# 71. Direct Registry Authority

No direct writable registry authority is granted.

LPAC should receive no registry capability unless .NET compatibility proves it essential.

Regular AppContainer accessible registry surfaces must be inventoried.

---

# 72. Credentials and Authentication

The Plugin Host must not access:

* Windows Credential Manager;
* user authentication tickets;
* browser credential stores;
* DPAPI-protected Opure Vault keys;
* or enterprise authentication capabilities.

---

# 73. Device Authority

No camera, microphone, location, Bluetooth, USB, printer or raw device capability is granted initially.

---

# 74. AppContainer Identity

Create one stable AppContainer profile per:

```text
Opure channel
plugin canonical ID
publisher trust identity digest
sandbox schema major
```

---

## 74.1 Why Publisher Bound

A publisher transfer creates a new sandbox identity and prevents inherited DACL access from being silently reused.

---

## 74.2 Version Update

Compatible package versions from the same publisher may reuse the profile.

Runtime capabilities still bind exact package hash.

---

## 74.3 Source Change

Source transfer does not necessarily require a new AppContainer SID when publisher identity is unchanged, but grants remain suspended until review.

---

## 74.4 Moniker

The AppContainer moniker is generated from canonical identifiers and a stable digest.

It must not include untrusted raw display text.

---

# 75. Regular AppContainer Baseline

Version 1 production requires a regular AppContainer unless LPAC is proven and promoted.

---

## 75.1 Zero Network Capabilities

No network capability is attached.

---

## 75.2 Explicit DACLs

Grant the AppContainer SID only the required access.

---

## 75.3 Access Inventory

Before acceptance, inventory what the process can still read or invoke through regular AppContainer defaults.

---

## 75.4 Unacceptable Exposure

If the inventory exposes unacceptable user or Opure resources that cannot be removed, regular AppContainer is rejected and LPAC becomes mandatory.

---

# 76. LPAC Hardening

LPAC is the preferred target because it excludes resources broadly granted to all application packages.

---

## 76.1 Prototype Requirements

Prove:

* Plugin Host executable launch;
* .NET runtime initialisation;
* managed assembly loading;
* native dependency loading;
* named-pipe IPC;
* package payload read;
* plugin data write;
* temporary files;
* diagnostics;
* and process shutdown.

---

## 76.2 Required Capabilities

Every LPAC capability must be documented and minimised.

---

## 76.3 No Compatibility Guess

Do not add broad LPAC capabilities merely to make a failing plugin run.

Identify the exact dependency.

---

# 77. AppContainer DACL Plan

The trusted Sandbox Service manages DACL grants.

---

## 77.1 Package Payload

Grant:

```text
Read
ReadAndExecute
Synchronize
```

as required.

No write.

---

## 77.2 Plugin Data

Grant bounded:

```text
Read
Write
CreateFiles
CreateDirectories
Delete own entries
```

according to the data model.

---

## 77.3 Temporary Scope

Grant read/write to a plugin-specific temporary directory.

---

## 77.4 Named Pipe

Grant only the minimum connection rights to the AppContainer SID and trusted Runtime identity.

Do not use a permissive default descriptor.

---

## 77.5 No Broad Profile ACL

Never grant the AppContainer SID access to:

* `%USERPROFILE%`;
* `%LOCALAPPDATA%\Opure`;
* project root;
* Vault root;
* or plugin package parent directory

as a broad tree.

---

## 77.6 ACL Removal

On publisher transfer, uninstall or sandbox reset, remove obsolete explicit ACEs after the host stops.

---

# 78. IPC

The Plugin Host uses a dedicated local IPC endpoint.

---

## 78.1 Endpoint

Endpoint identity includes:

* Runtime instance;
* plugin instance;
* channel;
* and random nonce.

---

## 78.2 Windows ACL

The named pipe DACL permits only:

* expected AppContainer SID;
* trusted Runtime user or logon SID;
* and required system principals.

---

## 78.3 Remote Access

Deny network principals and remote named-pipe access.

---

## 78.4 Application Authentication

ADR-0004 mutual session authentication remains mandatory.

Windows ACL alone is not sufficient.

---

## 78.5 Capability Channel

Capability references are accepted only on the authenticated session.

---

# 79. Handle Inheritance

Create the Plugin Host with an explicit handle list.

No unrelated inheritable handle is passed.

---

## 79.1 Standard Handles

Standard input, output and error are:

* closed;
* redirected to bounded trusted pipes;
* or explicitly controlled.

---

## 79.2 Sensitive Handles

Never inherit:

* Vault handles;
* database handles;
* project file handles;
* signing handles;
* process handles;
* or Runtime job handles.

---

# 80. Job Object

Every Plugin Host belongs to a dedicated Job Object created before plugin activation.

---

## 80.1 Required Limits

Prototype:

* `KILL_ON_JOB_CLOSE`;
* active process limit;
* process memory limit;
* job memory limit;
* CPU-rate or time policy;
* no breakaway;
* and accounting notifications.

---

## 80.2 Active Process Limit

Target:

```text
1
```

The Plugin Host must not spawn child processes.

---

## 80.3 Memory

Initial memory limits should be performance-mode aware and plugin-policy aware.

Exceeding a hard limit terminates the plugin host and records a fault.

---

## 80.4 CPU

Use bounded CPU-rate control or accounting with cancellation and termination thresholds.

---

## 80.5 Job Close

Closing the supervisor's final Job Object handle terminates the Plugin Host.

---

## 80.6 No Breakaway

Do not enable breakaway flags.

---

# 81. Process Mitigations

Apply compatible mitigations before plugin code loads.

Candidates include:

* DEP;
* high-entropy ASLR;
* strict handle checks;
* extension-point disablement;
* child-process denial;
* image-load restrictions;
* font restrictions;
* Control Flow Guard where compatible;
* user shadow stack where compatible;
* and Win32k system-call disablement for a headless host where compatible.

---

## 81.1 Dynamic Code

Do not prohibit dynamic code without evidence because .NET JIT requires it.

---

## 81.2 Signature Policy

A Microsoft-only binary signature policy would block third-party plugin assemblies and is not generally compatible.

Do not enable blindly.

---

## 81.3 Audit Before Enforce

Where Windows supports audit modes, prototype in audit before enforcement.

---

## 81.4 Recorded Profile

The effective mitigation set is recorded in diagnostics and acceptance evidence.

---

# 82. Restricted Token

A restricted token may additionally remove privileges from the AppContainer launch or support Development Mode.

It is not the primary sandbox.

---

## 82.1 Development Fallback

When AppContainer cannot run in Development Mode, a restricted-token Job Object host may be permitted with:

* prominent warning;
* no production package trust;
* no persistent secret grant;
* no direct network;
* and isolated development profile.

---

## 82.2 Production Block

Production third-party activation fails closed if the AppContainer boundary cannot be created.

---

# 83. Environment Block

Construct a minimal environment for Plugin Host.

Permit only required variables such as:

* system runtime paths;
* plugin host mode;
* channel;
* safe culture;
* and temporary path.

Remove:

* provider API keys;
* package signing variables;
* Azure tokens;
* Git credentials;
* CI secrets;
* user custom secret variables;
* and broad Opure internal configuration.

---

# 84. Current Directory

Set current directory to the plugin-specific temporary or data scope.

Do not use:

* project root;
* Opure package root;
* or user profile root.

---

# 85. DLL Search

Use safe DLL search behaviour.

---

## 85.1 No Current-Directory Search

Do not permit current directory to override trusted host dependencies.

---

## 85.2 Package Native Directory

Load plugin-native dependencies only from the verified package RID directory and approved system locations.

---

## 85.3 No PATH Injection

The plugin cannot modify a trusted search path.

---

# 86. Window and Desktop Isolation

The Plugin Host is headless.

---

## 86.1 No UIAccess

No UIAccess capability or elevated window interaction.

---

## 86.2 No Desktop Automation

Plugins cannot send arbitrary input to other applications.

---

## 86.3 Declarative UI Only

Desktop contributions pass through trusted Desktop services.

---

# 87. Resource Budgets

Every plugin has a resource profile.

Potential fields:

```text
max_memory
max_cpu_rate
max_operation_time
max_concurrent_operations
max_network_requests
max_network_bytes
max_file_bytes
max_data_bytes
max_log_rate
max_notifications
```

---

## 87.1 Performance Modes

Eco, Balanced, Performance and Turbo may adjust quotas within security maxima.

---

## 87.2 User Increase

A user may increase resource quotas within hard policy.

Increasing resource quota does not grant a new data or authority permission.

---

# 88. Capability Rate Limits

Rate limits are applied per:

* plugin;
* plugin instance;
* permission;
* project;
* destination;
* and profile.

---

## 88.1 Abuse

Repeated denied or excessive requests may:

* throttle;
* terminate operation;
* disable plugin;
* or quarantine after security review.

---

# 89. Capability Lifecycle

A typical operation is:

1. plugin requests broker operation;
2. Runtime authenticates plugin session;
3. Runtime confirms manifest declaration;
4. policy narrows scope;
5. broker resolves exact resources;
6. trusted service constructs plan;
7. approval is obtained where required;
8. capability lease is minted;
9. plugin invokes typed broker method with capability reference;
10. broker revalidates;
11. service performs bounded action;
12. result is returned;
13. quota is decremented;
14. Trust Centre records completion;
15. capability is consumed or expires.

---

# 90. No Capability Enumeration

A plugin cannot list all live capabilities in the Runtime.

It may receive its own active operation status.

---

# 91. No Service Locator

The Plugin SDK must not expose a generic:

```text
GetService(string name)
```

or equivalent reflection-based service locator.

---

## 91.1 Typed Clients

Expose narrow typed clients such as:

* `IProjectReader`;
* `IPatchProposalClient`;
* `IBuildRequestClient`;
* `INetworkRequestClient`;
* and `ISecretUseClient`.

Even these clients remain capability-backed.

---

# 92. Capability SDK Ergonomics

The SDK should make safe use straightforward.

Conceptual style:

```csharp
await using ProjectReadSession session =
    await context.Projects.OpenReadSessionAsync(projectRef, request, cancellationToken);

FileSnapshot snapshot =
    await session.ReadFileAsync(fileRef, cancellationToken);
```

The SDK must not expose raw Runtime transport or bearer values unnecessarily.

---

# 93. Capability Error Model

Stable errors include:

* Permission Not Declared;
* Permission Denied;
* Approval Required;
* Approval Expired;
* Approval Plan Changed;
* Capability Invalid;
* Capability Expired;
* Capability Revoked;
* Capability Consumed;
* Capability Wrong Session;
* Capability Wrong Plugin;
* Capability Wrong Project;
* Capability Scope Exceeded;
* Capability Quota Exceeded;
* Resource Stale;
* Project Closed;
* Package Quarantined;
* Sandbox Unavailable;
* Operation Cancelled;
* and Policy Changed.

---

# 94. User-Facing Permission Review

The permission UI should group by:

* project data;
* external communication;
* execution;
* secrets;
* plugin data;
* UI contributions;
* and system restrictions.

---

## 94.1 Each Card Shows

* permission name;
* plain-language action;
* scope;
* destination;
* data classifications;
* persistence choices;
* plugin-provided reason;
* Opure risk explanation;
* policy source;
* and whether fresh operation approval remains required.

---

## 94.2 No Single “Allow Everything”

The UI does not offer one global all-permissions button for third-party plugins.

---

## 94.3 Required and Optional

Clearly distinguish required from optional.

---

## 94.4 Effective Scope

Show the narrowed effective scope, not only the plugin's broad request.

---

## 94.5 Accessibility

Trust and risk must not rely on colour alone.

---

# 95. Operation Approval UI

High-risk operation review should show exact:

* plugin;
* project;
* files or data classes;
* destination;
* method;
* provider;
* secret label;
* command or target;
* side effects;
* quota;
* and expiry.

---

## 95.1 Exact File Count

Show count and permit expansion to relative paths.

---

## 95.2 Data Preview

Where safe, show a bounded preview or summary.

Never display secret value.

---

## 95.3 Choices

Offer only policy-supported choices such as:

* Deny;
* Allow Once;
* Allow for This Project;
* Allow This Exact Destination;
* or Manage Plugin Permissions.

---

# 96. Permission Revocation UI

The developer can revoke:

* one permission;
* one project grant;
* one destination;
* one secret binding;
* all plugin permissions;
* or the plugin entirely.

Revocation should take effect immediately for new and in-flight broker operations where safe.

---

# 97. Update Permission Diff

ADR-0016 requires reapproval for expansion.

The capability model computes a semantic diff.

---

## 97.1 Unchanged

Retain compatible persistent grant.

All live capabilities end when the old Plugin Host stops.

---

## 97.2 Narrowed

May retain the narrower grant after visible update review.

---

## 97.3 Added

Requires explicit approval.

---

## 97.4 Broadened Parameter

Requires explicit approval.

Examples:

* new host;
* wider path;
* additional method;
* higher data classification;
* longer persistence;
* or new command template.

---

## 97.5 Removed

Revoke related grants and capabilities.

---

## 97.6 Catalogue Meaning Change

Requires reapproval even when the string ID remains present.

A broadening should normally use a new ID.

---

# 98. Publisher and Source Changes

A publisher change:

* stops Plugin Host;
* revokes capabilities;
* suspends grants;
* creates a new AppContainer identity;
* and requires complete review.

A source change:

* stops automatic update;
* revokes live capabilities during transfer;
* suspends grants pending trust comparison;
* and requires explicit source-transfer approval.

---

# 99. Plugin Disable and Quarantine

Disable:

* prevents new capabilities;
* revokes live capabilities;
* cancels operations;
* stops Plugin Host;
* and preserves grants for later review.

Quarantine additionally:

* marks package untrusted;
* blocks activation;
* suspends persistent grants;
* preserves evidence;
* and removes sensitive bindings.

---

# 100. Secret Binding Revocation

On:

* plugin disable;
* uninstall;
* publisher change;
* package quarantine;
* secret rotation;
* or destination-policy change,

revoke affected `SecretUseRef` records and live capabilities.

---

# 101. Runtime Restart

A Runtime restart invalidates:

* IPC sessions;
* capability leases;
* operation approvals not yet consumed;
* and Plugin Host bootstrap credentials.

Persistent grants remain.

---

# 102. Project Close

Closing a project revokes project-scoped capabilities.

---

# 103. Workspace Change

A workspace generation change invalidates snapshot-bound capabilities where the resource may have changed.

---

# 104. Enterprise Policy Change

A more restrictive policy revokes incompatible live capabilities and narrows future grants.

A less restrictive policy does not automatically grant new authority.

---

# 105. Persistence

Persistent grants belong to the Plugin Permission Service database.

Live capabilities remain in memory.

---

## 105.1 Suggested Tables

```text
permission_catalogue_versions
plugin_permission_requests
plugin_permission_grants
plugin_permission_denials
plugin_secret_bindings
plugin_capability_audit
plugin_policy_exceptions
plugin_sandbox_profiles
```

---

## 105.2 No Capability Secret in Database

Do not persist plain live capability references.

Store audit-safe hashes or operation references.

---

# 106. Trust Centre

Trust Centre should show:

* manifest requests;
* effective grants;
* denials;
* operation approvals;
* destinations;
* data classes;
* secret-use events;
* build and tool requests;
* capability revocations;
* policy changes;
* sandbox identity;
* AppContainer type;
* Job Object limits;
* process mitigations;
* and security faults.

---

## 106.1 Evidence Not Payload

Trust Centre records action metadata, not full source or secret content.

---

## 106.2 Capability Identifier

Use an audit-safe derived identifier.

---

# 107. Diagnostic Logging

Logs should include:

* plugin ID;
* version;
* package hash;
* host instance;
* permission ID;
* policy result;
* capability audit ID;
* broker;
* duration;
* quota;
* and result.

Do not log:

* capability bearer;
* secret;
* source content;
* full request body;
* or unredacted response.

---

# 108. Metrics

Low-cardinality metrics may include:

* active Plugin Hosts;
* permission denials by ID;
* capability issuance count by ID;
* expiry count;
* revocation count;
* quota violations;
* sandbox launch failures;
* host terminations;
* and broker latency.

Do not label metrics with plugin ID in globally exported telemetry without policy.

---

# 109. Security Incident Response

On suspected plugin compromise:

1. disable plugin;
2. revoke all capabilities;
3. terminate Job Object;
4. revoke secret bindings;
5. block package activation;
6. preserve package hash and diagnostics;
7. inspect Trust Centre activity;
8. inspect broker requests;
9. inspect network destinations;
10. inspect project proposals;
11. inspect data scope;
12. assess publisher and source;
13. quarantine affected versions;
14. notify developer;
15. and restore or rotate affected state.

---

## 109.1 Direct Access Attempt

An AppContainer access-denied event may be a bug or malicious attempt.

Repeated attempts trigger escalation.

---

## 109.2 Capability Probing

Repeated invalid capability use is a security signal.

---

# 110. Threat Model

Relevant threats include:

* plugin bypassing broker through Windows APIs;
* plugin reading user files directly;
* raw network exfiltration;
* credential access;
* process injection;
* child process escape;
* named-pipe impersonation;
* capability theft;
* capability replay;
* capability delegation;
* stale snapshot use;
* approval-plan substitution;
* permission smuggling;
* confused deputy;
* quota bypass;
* host process replacement;
* PID reuse;
* publisher transfer;
* source substitution;
* AppContainer DACL overgrant;
* permissive pipe ACL;
* inherited handle leak;
* DLL search hijack;
* mitigation incompatibility;
* malicious native library;
* and same-user malware.

---

# 111. Security Controls

Controls include:

* AppContainer;
* optional LPAC;
* zero network capabilities;
* explicit DACLs;
* low integrity;
* Job Object;
* child-process denial;
* handle allowlist;
* process mitigations;
* minimal environment;
* safe DLL search;
* authenticated IPC;
* opaque capabilities;
* exact process and session binding;
* package-hash binding;
* project-generation binding;
* short expiry;
* one-shot use;
* quotas;
* revocation epochs;
* approval-plan hashes;
* broker validation;
* and Trust Centre evidence.

---

# 112. Security Limitations

This design does not guarantee protection against:

* a Windows kernel vulnerability;
* an AppContainer escape;
* a Runtime broker vulnerability;
* a malicious signed Opure update;
* same-user malware controlling Opure;
* side channels;
* all denial-of-service;
* or a developer explicitly approving harmful data transmission.

The product must state these limitations honestly.

---

# 113. Reliability Impact

The model adds:

* broker calls;
* policy evaluation;
* capability state;
* AppContainer setup;
* and process supervision.

It improves fault isolation and revocation.

A sandbox launch failure disables the plugin rather than weakening the boundary.

---

# 114. Performance Impact

Expected costs include:

* IPC round trips;
* streaming;
* hashing and snapshot creation;
* policy evaluation;
* per-plugin process memory;
* and AppContainer setup.

Mitigations include:

* bounded session capabilities;
* streaming;
* batched read requests;
* cached policy decisions without cached authority;
* and warm Plugin Hosts within quotas.

---

# 115. Privacy Impact

The permission model makes data flow explicit.

Plugin package permissions and actual operations remain local unless a broker performs an approved external request.

No permission grant itself sends data.

---

# 116. Testing Strategy

ADR-0008 applies.

The capability model requires:

* unit tests;
* permission-catalogue tests;
* policy tests;
* approval tests;
* capability lifecycle tests;
* broker tests;
* AppContainer tests;
* Job Object tests;
* process-mitigation tests;
* IPC security tests;
* data-leakage tests;
* update tests;
* recovery tests;
* fuzzing;
* and adversarial plugin fixtures.

---

# 117. Permission Catalogue Tests

Test:

* valid catalogue;
* duplicate ID;
* invalid ID syntax;
* wildcard permission;
* missing broker;
* invalid risk level;
* invalid persistence;
* missing parameter schema;
* incompatible catalogue version;
* semantic broadening;
* deprecation;
* replacement;
* and canonical ordering.

---

# 118. Manifest Permission Tests

Test:

* valid request;
* unknown permission;
* duplicate request;
* conflicting parameters;
* invalid path pattern;
* invalid hostname;
* wildcard host;
* invalid method;
* prohibited data class;
* excessive quota;
* unsupported persistence request;
* optional permission;
* required permission;
* and misleading reason text.

---

# 119. Policy Tests

Test:

* hard deny;
* enterprise deny;
* channel deny;
* project deny;
* profile grant;
* operation approval;
* runtime condition;
* and Deny precedence.

---

## 119.1 No Broadening

Prove:

* enterprise policy cannot broaden;
* user grant cannot exceed manifest;
* Runtime cannot exceed catalogue;
* plugin request cannot exceed persistent grant;
* and capability cannot exceed operation plan.

---

# 120. Persistent Grant Tests

Test:

* Once;
* Host Session;
* Project Session;
* This Project;
* This Profile;
* Until Date;
* Denied;
* expiry;
* project close;
* plugin update;
* publisher change;
* source transfer;
* package hash change;
* catalogue change;
* and enterprise policy change.

---

# 121. Approval Plan Tests

Test:

* exact canonical plan;
* file list change;
* file hash change;
* destination change;
* method change;
* command parameter change;
* secret binding change;
* provider change;
* quota change;
* data-class change;
* expired approval;
* replayed approval;
* and approval from another plugin.

---

# 122. Capability Generation Tests

Test:

* secure random generation;
* collision handling;
* minimum entropy;
* server-side storage;
* plain-reference non-persistence;
* safe diagnostic hash;
* and no bearer in logs.

---

# 123. Capability Binding Tests

Test wrong:

* Runtime instance;
* IPC session;
* process ID;
* process start time;
* AppContainer SID;
* plugin ID;
* plugin version;
* package hash;
* publisher;
* source;
* project;
* workspace generation;
* permission;
* operation plan;
* and resource reference.

Every mismatch denies.

---

# 124. Capability Expiry Tests

Test:

* one-shot use;
* remaining use count;
* byte quota;
* call quota;
* concurrency;
* time expiry;
* session expiry;
* host termination;
* Runtime restart;
* and clock change.

Use monotonic time for live expiry where practical.

---

# 125. Revocation Tests

Test revocation on:

* manual permission revoke;
* project close;
* plugin disable;
* plugin update;
* uninstall;
* host crash;
* Runtime restart;
* package quarantine;
* source compromise;
* publisher transfer;
* secret rotation;
* enterprise policy change;
* and security incident.

---

# 126. Replay and Delegation Tests

Attempt to use a capability:

* twice when one shot;
* after expiry;
* from another plugin;
* from another host instance;
* after reconnect;
* through an MCP server;
* through a child process fixture;
* and after copying it into plugin data.

Every attempt denies.

---

# 127. Confused-Deputy Tests

Test:

* valid capability with different resource;
* valid file reference with another project;
* valid secret reference with another destination;
* valid network capability with another method;
* valid build capability with another target;
* valid patch proposal capability with changed base;
* and inferred current project.

Every broker must use explicit validated scope.

---

# 128. Project File Tests

Test:

* allowed relative file;
* excluded file;
* ignored file;
* hidden file;
* secret store;
* likely secret;
* binary file;
* oversized file;
* changed file;
* symlink;
* junction;
* case collision;
* alternate stream;
* stale snapshot;
* deleted file;
* and project close.

---

# 129. Secret Redaction Tests

Use canary values in:

* `.env`;
* JSON;
* YAML;
* source literals;
* connection strings;
* private keys;
* tokens;
* and command output.

Verify no canary reaches:

* plugin content;
* plugin logs;
* Trust Centre;
* network body;
* provider context;
* diagnostics bundle;
* or capability records.

---

# 130. Patch Proposal Tests

Test:

* valid patch;
* path escape;
* stale base hash;
* secret introduction;
* binary change;
* generated file;
* unsupported encoding;
* line-ending change;
* file delete;
* file rename;
* and plugin attempting direct file write.

Only Patch Service applies an approved plan.

---

# 131. Build and Test Broker Tests

Test:

* approved target;
* unknown target;
* arbitrary command injection;
* environment injection;
* secret environment;
* timeout;
* cancellation;
* output limit;
* process tree;
* and project close.

---

# 132. Tool Broker Tests

Test:

* approved template;
* invalid template;
* executable path injection;
* shell metacharacter;
* argument escape;
* working-directory escape;
* environment secret;
* output secret;
* and template policy change.

---

# 133. Network Tests

Test direct plugin attempts to:

* open Internet socket;
* open private-network socket;
* listen;
* access loopback;
* use DNS;
* use WebSocket;
* use QUIC;
* use raw socket;
* and call a local proxy.

The AppContainer must deny direct access.

---

## 133.1 Brokered Network Tests

Test:

* approved HTTPS host;
* wrong host;
* wildcard expansion;
* IP literal;
* public-to-private DNS resolution;
* redirect;
* cross-origin credentials;
* method mismatch;
* body data-class mismatch;
* request-size quota;
* response-size quota;
* timeout;
* cancellation;
* and secret echo.

---

# 134. Provider Tests

Test:

* Local Only;
* Ask Every Time;
* Approved Providers Only;
* Custom;
* wrong provider;
* context expansion;
* token budget;
* cancelled inference;
* provider error;
* and no API key exposure.

---

# 135. Secret Use Tests

Test:

* approved destination;
* wrong destination;
* wrong header;
* wrong plugin;
* wrong publisher;
* wrong project;
* expired binding;
* rotated secret;
* revoked grant;
* response echo;
* diagnostic error;
* and plugin attempt to enumerate Vault.

No raw secret reaches the Plugin Host.

---

# 136. Plugin Data Tests

Test:

* own data read;
* own data write;
* quota;
* path escape;
* another plugin's data;
* Stable versus Preview;
* package directory write;
* backup;
* restore;
* and uninstall preservation.

---

# 137. Diagnostics Tests

Test:

* allowed event;
* rate limit;
* oversized field;
* arbitrary object;
* secret;
* source content;
* capability reference;
* HTTP header;
* exception;
* and log flood.

---

# 138. Notification Tests

Test:

* allowed notification;
* safe text;
* active content;
* external link;
* spam;
* disabled plugin;
* and quarantined plugin.

---

# 139. AppContainer Launch Tests

Test:

* unique profile creation;
* existing profile;
* SID derivation;
* package payload ACL;
* Plugin Host launch;
* .NET initialisation;
* managed dependency load;
* native dependency load;
* data write;
* temporary directory;
* named-pipe connection;
* and shutdown.

---

# 140. AppContainer Access-Denial Tests

Attempt direct access to:

* project repository;
* another project;
* Opure profile root;
* Vault root;
* another plugin package;
* another plugin data root;
* user Documents;
* Desktop;
* Downloads;
* SSH keys;
* browser profile;
* Credential Manager;
* registry;
* clipboard;
* other process;
* and network.

Expected results must be recorded for regular AppContainer and LPAC.

---

# 141. AppContainer Access Inventory

Use a controlled probe to inventory unexpected readable:

* files;
* registry keys;
* COM objects;
* devices;
* process objects;
* and namespace objects.

A regular AppContainer is accepted only after Security review of this inventory.

---

# 142. LPAC Tests

Repeat all required runtime and denial tests under LPAC.

Record every capability or ACL required.

LPAC becomes mandatory when:

* compatibility is complete;
* performance is acceptable;
* and the access inventory is materially safer.

---

# 143. Full-Trust Failure Test

Force AppContainer creation failure.

Production policy must:

* refuse plugin activation;
* show Sandbox Unavailable;
* and never silently start medium-integrity Plugin Host.

---

# 144. Development Fallback Tests

Explicit Development Mode may start restricted full-trust fallback only after:

* warning;
* isolated development profile;
* no persistent secret use;
* no production package trust;
* and local developer confirmation.

---

# 145. Named-Pipe Security Tests

Test:

* correct AppContainer SID;
* wrong AppContainer SID;
* same user unrelated process;
* another logon session;
* remote network principal;
* anonymous;
* stale endpoint;
* guessed endpoint;
* and application-layer authentication failure.

---

# 146. Handle Inheritance Tests

Place sensitive inheritable handles in the parent process and prove the Plugin Host receives none except the explicit allowlist.

---

# 147. Job Object Tests

Test:

* host assigned before activation;
* active process limit;
* child process creation;
* breakaway attempt;
* memory limit;
* CPU limit;
* accounting;
* kill-on-close;
* Runtime crash;
* and orphan cleanup.

---

# 148. Process Mitigation Tests

Test each proposed mitigation against:

* .NET startup;
* JIT;
* managed plugin;
* native plugin library;
* diagnostics;
* IPC;
* and crash reporting.

Record enforced and audit-only policies.

---

# 149. DLL Search Tests

Attempt:

* current-directory hijack;
* package-directory collision;
* trusted contract substitution;
* PATH injection;
* remote image load;
* low-integrity image load;
* and native dependency collision.

---

# 150. Environment Tests

Seed parent environment with canary secrets and prove the Plugin Host environment excludes them.

---

# 151. UI Isolation Tests

Attempt:

* top-level window;
* SendMessage to Desktop;
* input injection;
* clipboard access;
* screen capture;
* and native toast.

Expected denial or policy behaviour must be recorded.

---

# 152. Resource Limit Tests

Test:

* memory pressure;
* CPU loop;
* excessive threads;
* log flood;
* network flood through broker;
* file-read flood;
* notification flood;
* and concurrent operation flood.

---

# 153. Plugin Update Tests

Test:

* unchanged permissions;
* reduced permissions;
* added permission;
* broader path;
* new host;
* new method;
* higher data class;
* publisher change;
* source transfer;
* package repack;
* and catalogue meaning change.

---

# 154. Incident Tests

Simulate:

* package quarantine;
* signer revocation;
* source compromise;
* publisher transfer;
* malicious network request;
* direct file-access probe;
* capability replay;
* and AppContainer escape alert.

---

# 155. Fuzzing

Fuzz:

* permission catalogue;
* manifest permission parameters;
* canonical JSON;
* capability protocol;
* approval plan;
* resource references;
* path patterns;
* host patterns;
* network plans;
* and broker request messages.

---

# 156. Performance Tests

Measure:

* AppContainer profile creation;
* Plugin Host launch;
* IPC handshake;
* capability issuance;
* policy evaluation;
* file snapshot read;
* search;
* patch proposal;
* build request;
* network request;
* provider request;
* secret use;
* and revocation.

---

# 157. Performance Budgets

Initial provisional targets on reference hardware:

* cached permission evaluation: under 2 ms p95;
* capability issuance: under 5 ms p95;
* broker validation excluding underlying work: under 2 ms p95;
* warm Plugin Host command overhead: under 10 ms p95;
* cold AppContainer Plugin Host readiness: under 2 seconds p95;
* revocation propagation: under 250 ms for new broker calls;
* and process termination after hard revoke: under 2 seconds where safe.

These are targets, not accepted facts.

---

# 158. Accessibility Tests

Permission and approval UI must support:

* keyboard;
* Narrator;
* high contrast;
* reduced motion;
* clear focus;
* risk text;
* source text;
* scope expansion;
* and revocation.

---

# 159. Prototype Plan

## 159.1 Prototype A — Catalogue

Define a minimal permission catalogue and validate an ADR-0016 package.

---

## 159.2 Prototype B — Opaque Capability

Mint a one-shot file-read capability bound to one plugin instance and project snapshot.

---

## 159.3 Prototype C — Replay

Copy and replay the capability from another host and session.

Verify denial.

---

## 159.4 Prototype D — Approval Plan

Approve one exact network request and mutate its host, method and data class.

Verify invalidation.

---

## 159.5 Prototype E — Project Broker

Enumerate and read selected source files without direct project-root access.

---

## 159.6 Prototype F — Patch Proposal

Submit a patch proposal and prove only Patch Service can apply it.

---

## 159.7 Prototype G — Secret Use

Use one API token for one host without exposing the value to the plugin.

---

## 159.8 Prototype H — Regular AppContainer

Launch a managed plugin in a unique no-network AppContainer.

---

## 159.9 Prototype I — LPAC

Repeat under LPAC and inventory compatibility.

---

## 159.10 Prototype J — Job Object

Block child processes, enforce memory, kill on supervisor close and collect accounting.

---

## 159.11 Prototype K — Process Mitigations

Find the strongest compatible mitigation profile.

---

## 159.12 Prototype L — Incident

Quarantine a plugin, revoke every capability, terminate host and preserve evidence.

---

# 160. Implementation Plan

1. Record founder review.
2. Define permission terminology in SDK.
3. Create catalogue schema.
4. Define catalogue version 1.
5. Define canonical parameter schemas.
6. Define permission-risk levels.
7. Implement manifest request validation.
8. Implement policy evaluator.
9. Implement persistent grants.
10. Implement operation approval plans.
11. Implement approval-plan hashing.
12. Implement capability service.
13. Implement session and process binding.
14. Implement expiry and quotas.
15. Implement revocation epochs.
16. Implement project resource references.
17. Implement Workspace read broker.
18. Implement Patch proposal broker.
19. Implement Build and Test brokers.
20. Implement Tool Mediator templates.
21. Implement Network Gateway broker.
22. Implement AI Router broker.
23. Implement secret-use binding.
24. Implement plugin data capability.
25. Implement diagnostics and notification limits.
26. Implement AppContainer profile service.
27. Implement DACL management.
28. Implement named-pipe ACLs.
29. Implement Job Object supervision.
30. Implement process mitigations.
31. Implement minimal environment.
32. Implement permission UI.
33. Implement operation approval UI.
34. Implement revocation UI.
35. Implement Trust Centre projection.
36. Add malicious plugin fixtures.
37. Run regular AppContainer access inventory.
38. Run LPAC compatibility prototype.
39. Complete security review.
40. Accept, amend or reject the ADR.

---

# 161. Owners

| Area                  | Owner                                |
| --------------------- | ------------------------------------ |
| Product policy        | Founder                              |
| Permission catalogue  | Plugin Security and SDK Owners       |
| Capability service    | Runtime Architecture Owner           |
| Project resources     | Workspace Owner                      |
| Patch proposal        | Patch Service Owner                  |
| Build and test        | Build Service Owner                  |
| Tool requests         | Tool Mediator Owner                  |
| Network               | Network Gateway Owner                |
| Provider inference    | AI Router Owner                      |
| Secret use            | Secrets Owner                        |
| Sandbox               | Windows Platform and Security Owners |
| Job Objects           | Process Supervisor Owner             |
| Trust Centre          | Trust Centre Owner                   |
| Desktop permission UI | Desktop Owner                        |
| Persistence           | Persistence Owner                    |
| Adversarial tests     | Test Architecture Owner              |

---

# 162. Suggested Repository Structure

```text
src/
├── Plugins/
│   ├── Opure.Plugins.Permissions/
│   ├── Opure.Plugins.Capabilities/
│   ├── Opure.Plugins.Brokers/
│   ├── Opure.Plugins.Sandboxing.Windows/
│   └── Opure.Plugins.Contracts/
├── Runtime/
│   └── Opure.Runtime.PluginSecurity/
└── Hosts/
    └── Opure.PluginHost/

schemas/
└── plugins/
    ├── permission-catalogue-v1.schema.json
    ├── permission-catalogue-v1.json
    ├── permission-request-v1.schema.json
    └── approval-plan-v1.schema.json

tests/
└── Plugins/
    ├── Opure.Plugins.CapabilityTests/
    ├── Opure.Plugins.SandboxTests/
    ├── Opure.Plugins.SecurityTests/
    └── Fixtures/
        └── MaliciousPlugins/
```

Exact projects may be consolidated under ADR-0010.

---

# 163. Capability Record Sketch

Conceptual internal form:

```json
{
  "audit_id": "cap-audit-opaque",
  "plugin": {
    "id": "Acme.Opure.RepositoryReview",
    "version": "1.2.0",
    "package_sha256": "..."
  },
  "instance": {
    "runtime_instance": "...",
    "ipc_session": "...",
    "process_start": "...",
    "appcontainer_sid": "..."
  },
  "permission": "project.files.read",
  "scope": {
    "project_ref": "...",
    "workspace_generation": 42,
    "file_refs": ["..."],
    "max_bytes": 1048576
  },
  "issued_at": "...",
  "expires_at": "...",
  "remaining_uses": 1,
  "revocation_epoch": 7
}
```

The bearer reference is not serialized into this audit form.

---

# 164. Approval Plan Sketch

```json
{
  "schema": "opure.plugin-approval-plan/1",
  "plugin_id": "Acme.Opure.RepositoryReview",
  "plugin_version": "1.2.0",
  "package_sha256": "...",
  "permission": "network.http.request",
  "project_ref": "...",
  "destination": {
    "scheme": "https",
    "host": "api.example.com",
    "port": 443,
    "method": "POST"
  },
  "data": {
    "classes": ["project.source"],
    "files": 4,
    "bytes": 92841
  },
  "secret_use_ref": "...",
  "expires_at": "..."
}
```

The trusted service canonicalises and hashes this plan before display.

---

# 165. Policy Decision Sketch

```json
{
  "decision": "approval_required",
  "permission": "network.http.request",
  "effective_scope": {
    "hosts": ["api.example.com"],
    "methods": ["POST"],
    "data_classes": ["project.source"]
  },
  "denied_scope": {
    "hosts": ["*"],
    "methods": ["PUT", "DELETE"]
  },
  "policy_sources": [
    "hard_product_policy",
    "project_cloud_policy",
    "plugin_grant"
  ]
}
```

---

# 166. Sandbox Profile Sketch

```text
sandbox_schema: 1
channel: Stable
plugin_id: Acme.Opure.RepositoryReview
publisher_digest: <digest>
container_type: AppContainer
network_capabilities: none
package_access: read_execute
data_access: read_write_own
temp_access: read_write_own
child_processes: denied
job_breakaway: denied
pipe_acl: exact_container_sid
```

---

# 167. Release Gate

Production third-party plugin support is blocked when:

* permission catalogue is absent;
* unknown permission can activate;
* a plugin can call a raw trusted service;
* capability reference appears in logs;
* capability can be replayed across sessions;
* permission expansion avoids reapproval;
* plugin can read project files directly;
* plugin can open direct network;
* plugin can read a raw secret;
* plugin can apply a patch;
* plugin can spawn a child process;
* Plugin Host runs at normal medium integrity;
* AppContainer creation silently falls back;
* pipe ACL is permissive;
* inherited handles are present;
* package/data DACLs are broad;
* Job Object breakaway is enabled;
* Sandbox access inventory is incomplete;
* LPAC decision is unreviewed;
* revocation does not stop broker authority;
* or security incident handling cannot identify affected operations.

---

# 168. Acceptance Criteria

This ADR may move to **Accepted** when all of the following hold.

## Catalogue and Policy

* [ ] Permission catalogue version 1 exists.
* [ ] Permission IDs are stable and canonical.
* [ ] Unknown permissions fail activation.
* [ ] Wildcard permission is prohibited.
* [ ] Permission parameters use schemas.
* [ ] Risk levels are assigned.
* [ ] Hard-denied authorities are defined.
* [ ] Semantic broadening requires a new permission or catalogue major.
* [ ] Deny wins at every policy layer.
* [ ] Policy cannot broaden manifest request.
* [ ] Enterprise policy cannot bypass hard deny.
* [ ] Effective scope and source are visible.

## Grants and Approvals

* [ ] Persistent grants are separate from capabilities.
* [ ] Project content grants are project scoped.
* [ ] High-risk persistence is constrained.
* [ ] Approval plans contain exact resources and destinations.
* [ ] Approval plans are canonicalised and hashed.
* [ ] Material plan changes invalidate approval.
* [ ] AI cannot approve.
* [ ] User can revoke one permission or all permissions.
* [ ] Revocation is visible.
* [ ] Publisher and source changes suspend grants.

## Capabilities

* [ ] Capability references are opaque and random.
* [ ] Capability state is server side.
* [ ] Capability is not a JWT.
* [ ] Capability is session bound.
* [ ] Capability is process and process-start bound.
* [ ] Capability is plugin ID and package-hash bound.
* [ ] Capability is project and workspace-generation bound.
* [ ] Capability is permission and operation bound.
* [ ] Capability expiry is enforced.
* [ ] Use counts are enforced.
* [ ] Byte and call quotas are enforced.
* [ ] Capability cannot be delegated.
* [ ] Capability cannot be replayed after reconnect.
* [ ] Runtime restart invalidates capabilities.
* [ ] Plain bearer references are absent from logs and persistent databases.
* [ ] Revocation epochs are enforced.

## Brokered Operations

* [ ] Plugins use typed narrow broker clients.
* [ ] No generic service locator exists.
* [ ] Project file reads are snapshot bound.
* [ ] Raw project file handles are not exposed.
* [ ] Secret-classified files are denied or redacted.
* [ ] Plugins can propose but not apply patches.
* [ ] Builds and tests use known target references.
* [ ] Tools use typed templates.
* [ ] No arbitrary command or executable path is accepted.
* [ ] Network uses Network Gateway.
* [ ] Provider inference uses AI Router and cloud policy.
* [ ] Secrets are use-only and destination bound.
* [ ] Raw secret values never reach Plugin Host.
* [ ] Project memory is project scoped and classified.
* [ ] Plugin data is isolated and quota limited.
* [ ] Diagnostics and notifications are rate limited.
* [ ] Rich arbitrary UI is unavailable.

## Windows Sandbox

* [ ] Every production third-party plugin has a unique AppContainer identity.
* [ ] AppContainer identity binds plugin and publisher.
* [ ] Plugin Host runs at low integrity.
* [ ] No direct network capability exists.
* [ ] Package payload access is read/execute only.
* [ ] Project repositories have no direct DACL grant.
* [ ] Vault and Opure profile roots have no broad grant.
* [ ] Plugin data and temp grants are narrow.
* [ ] Named pipe uses an explicit DACL.
* [ ] Application-layer IPC authentication remains enabled.
* [ ] No unnecessary handles are inherited.
* [ ] Environment is minimal.
* [ ] Current directory is plugin-owned.
* [ ] Safe DLL search is enabled.
* [ ] Regular AppContainer access inventory is complete.
* [ ] LPAC compatibility is tested.
* [ ] Security review accepts regular AppContainer or promotes LPAC.
* [ ] Sandbox creation failure blocks production activation.
* [ ] Medium-integrity fallback is Development Mode only.

## Process Supervision

* [ ] Plugin Host is assigned to Job Object before activation.
* [ ] Job breakaway is disabled.
* [ ] Kill-on-job-close works.
* [ ] Active process limit prevents child process.
* [ ] Child-process mitigation is enabled where compatible.
* [ ] Memory limit works.
* [ ] CPU policy works.
* [ ] Job accounting is collected.
* [ ] Strongest compatible mitigation profile is recorded.
* [ ] Dynamic-code policy remains compatible with .NET JIT.
* [ ] Plugin Host is headless.
* [ ] Process termination revokes all authority.

## Lifecycle and Security

* [ ] Plugin update computes semantic permission diff.
* [ ] Added or broadened permission requires approval.
* [ ] Removed permission revokes grants.
* [ ] Old host capabilities end during update.
* [ ] Disable revokes capabilities.
* [ ] Uninstall revokes capabilities and secret bindings.
* [ ] Quarantine stops host and blocks activation.
* [ ] Source compromise procedure exists.
* [ ] Publisher compromise procedure exists.
* [ ] Capability probing is detected.
* [ ] Direct-access probes are detected.
* [ ] Trust Centre records permissions and operations safely.
* [ ] Security review is complete.
* [ ] Founder approval is recorded.

---

# 169. Evidence Required Before Acceptance

* [ ] Permission catalogue.
* [ ] Catalogue schema.
* [ ] Hard-deny list.
* [ ] Permission-manifest examples.
* [ ] Policy precedence tests.
* [ ] Persistent-grant report.
* [ ] Approval-plan substitution report.
* [ ] Capability entropy and collision report.
* [ ] Session, process and package binding report.
* [ ] Replay and delegation report.
* [ ] Expiry and quota report.
* [ ] Revocation-latency report.
* [ ] Project read broker report.
* [ ] Secret redaction report.
* [ ] Patch proposal report.
* [ ] Build, test and tool broker report.
* [ ] Direct-network denial report.
* [ ] Brokered-network report.
* [ ] Provider-policy report.
* [ ] Secret-use canary report.
* [ ] Plugin-data isolation report.
* [ ] AppContainer launch report.
* [ ] Regular AppContainer access inventory.
* [ ] LPAC compatibility and access report.
* [ ] DACL report.
* [ ] Named-pipe ACL report.
* [ ] IPC authentication report.
* [ ] Handle inheritance report.
* [ ] Job Object report.
* [ ] Child-process denial report.
* [ ] Process mitigation report.
* [ ] Environment canary report.
* [ ] DLL search report.
* [ ] UI isolation report.
* [ ] Plugin update permission-diff report.
* [ ] Publisher-transfer report.
* [ ] Security incident rehearsal.
* [ ] Performance report.
* [ ] Accessibility report.
* [ ] Security review.
* [ ] Founder approval.

---

# 170. Known Limitations

* The final permission catalogue is not yet defined.
* The final Plugin SDK contracts are not yet defined.
* Regular AppContainer exposes more Windows resources than LPAC.
* LPAC compatibility with the packaged .NET Plugin Host is not yet proven.
* AppContainer does not protect against every kernel or broker vulnerability.
* Same-user malware remains a threat.
* AppContainer profile and DACL lifecycle require native Windows interop.
* Dynamic code cannot be prohibited while using normal .NET JIT.
* Some process mitigations may conflict with managed or native plugin dependencies.
* No direct-network capability means every protocol needs a broker.
* Broker calls add IPC overhead.
* Snapshot-based file reads may increase memory or disk cost.
* Secret detection can produce false positives and false negatives.
* Version 1 has no sensitive-file override for raw secret-like content.
* The initial tool catalogue may be small.
* Direct repository mutation is unavailable to plugins.
* Rich plugin UI is unavailable.
* Inter-plugin capabilities are unavailable.
* Linux and macOS sandbox mappings are deferred.
* WebAssembly capabilities are deferred.
* Enterprise policy transport is deferred.
* Public plugin revocation distribution is deferred.
* A valid capability model cannot make a malicious approved external destination safe.

---

# 171. Open Questions

* Which exact permissions belong in catalogue version 1?
* Should `repository.diff.read` be separate from history?
* Should file enumeration and read be one permission or two?
* Should project memory query have separate summary and record permissions?
* Which path-pattern syntax is canonical?
* How are generated files classified?
* Which sensitive file classes are hard denied?
* Should any explicit sensitive-file read workflow exist after Version 1?
* What are the default file-count and byte quotas?
* Which build targets can be persistently granted?
* Which tool templates belong in Version 1?
* Should command proposals be separate from tool requests?
* How are network host patterns represented?
* Should wildcard subdomains be allowed?
* Should path-level network scopes exist?
* Should loopback ever be brokerable?
* How is DNS rebinding protection implemented?
* Which network headers are plugin controlled?
* Which response headers are exposed?
* Which project-data classes require operation approval every time?
* How is provider context preview represented?
* Can provider permission persist under Ask Every Time?
* How are MCP permissions represented?
* Should secret-use grants ever persist beyond a project session?
* How are OAuth refresh operations brokered?
* How is secret response echo detected?
* Should plugin data be direct AppContainer filesystem access or entirely brokered?
* What plugin-data quota is appropriate?
* Which diagnostics fields are allowed?
* Which notification rate is acceptable?
* What declarative UI schemas are supported?
* What exact opaque capability length should be used?
* Should capability IDs use random bytes or random plus type prefix?
* Which data structure stores live capabilities?
* How is capability lookup protected from timing or denial attacks?
* What revocation-epoch granularity is optimal?
* Which live operations can be cancelled safely?
* What is the maximum capability lifetime?
* Should low-risk session capabilities be renewed automatically?
* How is policy evaluation cached without caching authority?
* Which Windows 11 build is the sandbox baseline?
* Can the packaged Plugin Host executable run under a dynamically created LPAC without copying binaries?
* Which ACLs are needed for .NET runtime files?
* Which AppContainer-readable system files are exposed under regular AppContainer?
* Does regular AppContainer expose unacceptable COM or registry surfaces?
* Which LPAC capabilities are needed by .NET?
* Should one AppContainer profile exist per plugin or per package version?
* Should source identity also affect AppContainer identity?
* How are obsolete AppContainer profiles deleted?
* Which named-pipe rights are minimally required?
* Should Runtime use a logon SID and AppContainer SID intersection?
* Which process mitigations work with .NET 10?
* Can Win32k system calls be disabled safely?
* Can user shadow stack be required?
* Which native plugins fail Control Flow Guard strict mode?
* What memory and CPU defaults suit Balanced mode?
* Should active process limit be exactly one?
* How are crash dump permissions handled in AppContainer?
* How can diagnostics collect a dump without exposing sensitive memory?
* Should Development Mode full-trust fallback prohibit every secret binding?
* Should a plugin developer be able to test AppContainer locally?
* How is permission approval represented in backup and restore?
* Can grants be exported without creating unsafe cross-device authority?
* How do enterprise restrictions distribute?
* How are permission catalogue updates communicated to installed plugins?
* Should first-party downloaded plugins receive identical sandboxing?
* What security signal triggers automatic quarantine?
* How should an offline revocation list work?
* Which metrics remain local only?
* How should capability evidence appear in support bundles?

---

# 172. Deferred Decisions

This ADR intentionally defers:

* final permission catalogue;
* final SDK client names;
* rich plugin UI;
* inter-plugin services;
* arbitrary MCP access;
* sensitive-file override;
* direct local-service networking;
* external-process plugin model;
* WebAssembly plugin model;
* Linux sandbox;
* macOS sandbox;
* enterprise permission-policy transport;
* public revocation feed;
* remote attestation;
* hardware-isolated plugins;
* Windows Sandbox plugin mode;
* and cross-device permission grant portability.

---

# 173. Alternatives Rejected

Install-time permissions alone are rejected because exact resources, destinations and side effects are dynamic.

RBAC alone is rejected because it is too coarse for operation-specific plugin authority.

Raw paths and URLs are rejected as authority because strings are forgeable and vulnerable to stale-resource and canonicalisation errors.

Self-contained signed capability tokens are rejected because local server-side opaque references provide easier revocation and less bearer leakage.

Direct trusted-service interfaces are rejected because they create ambient authority.

A full-trust same-user Plugin Host is rejected for production because malicious plugin code could bypass the broker through Windows APIs.

A restricted token alone is rejected as the production sandbox because it does not provide the complete resource isolation desired.

Job Objects alone are rejected because they limit resource use but do not remove direct file or network authority.

LPAC is not immediately mandated because .NET and packaged-host compatibility require evidence, but it remains the preferred target.

Virtual machines and Windows Sandbox are deferred because they are too heavy for ordinary plugin interaction.

WebAssembly is deferred because the initial plugin ecosystem is managed .NET and requires native engineering integrations.

---

# 174. Official and Primary Evidence References

## Windows AppContainer and Capabilities

* [AppContainer isolation](https://learn.microsoft.com/en-us/windows/win32/secauthz/appcontainer-isolation)
* [Launch an AppContainer or LPAC](https://learn.microsoft.com/en-us/windows/win32/secauthz/implementing-an-appcontainer)
* [Windows 11 application isolation](https://learn.microsoft.com/en-us/windows/security/book/application-security-application-isolation)
* [Application capability declarations](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/app-capability-declarations)
* [MSIX containerisation overview](https://learn.microsoft.com/en-us/windows/msix/msix-containerization-overview)
* [`SECURITY_CAPABILITIES`](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-security_capabilities)
* [`TOKEN_INFORMATION_CLASS` and LPAC](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ne-winnt-token_information_class)
* [Networking capabilities](https://learn.microsoft.com/en-us/windows/apps/develop/networking/networking-basics)

## Windows Process Security and Supervision

* [`CreateRestrictedToken`](https://learn.microsoft.com/en-us/windows/win32/api/securitybaseapi/nf-securitybaseapi-createrestrictedtoken)
* [Restricted tokens](https://learn.microsoft.com/en-us/windows/win32/secauthz/restricted-tokens)
* [Job Objects](https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects)
* [`JOBOBJECT_BASIC_LIMIT_INFORMATION`](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_basic_limit_information)
* [`JOBOBJECT_EXTENDED_LIMIT_INFORMATION`](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_extended_limit_information)
* [Process mitigation policies](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ne-winnt-process_mitigation_policy)
* [`SetProcessMitigationPolicy`](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-setprocessmitigationpolicy)
* [`UpdateProcThreadAttribute`](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute)

## Windows IPC

* [Named pipe security and access rights](https://learn.microsoft.com/en-us/windows/win32/ipc/named-pipe-security-and-access-rights)
* [Named pipes](https://learn.microsoft.com/en-us/windows/win32/ipc/named-pipes)
* [.NET `System.IO.Pipes`](https://learn.microsoft.com/en-us/dotnet/api/system.io.pipes?view=net-10.0)

Windows security APIs, .NET compatibility and mitigation support can change.

The implementation must revalidate all selected APIs and exact sandbox behaviour on the supported Windows 11 baseline before acceptance.

---

# 175. Review Record

| Date         | Reviewer           | Decision | Notes                                                                                           |
| ------------ | ------------------ | -------- | ----------------------------------------------------------------------------------------------- |
| 18 July 2026 | Architecture draft | Proposed | Brokered object capabilities plus no-network per-plugin AppContainer and Job Object recommended |

---

# 176. Approval

## Founder or Product Approval

* **Name:** Christopher Dyer
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Permission, consent and production sandbox policy review required

## Plugin Security Approval

* **Name or role:** Plugin Security and Capability Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Catalogue, capability lifecycle and hard-deny evidence required

## Runtime Approval

* **Name or role:** Runtime Architecture Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Capability service, broker and session binding required

## Windows Security Approval

* **Name or role:** Windows Platform and Security Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** AppContainer, LPAC, DACL, Job Object and mitigation evidence required

## Secrets Approval

* **Name or role:** Secrets Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Use-only binding and canary leakage tests required

## Workspace and Patch Approval

* **Name or role:** Workspace and Patch Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Snapshot reads and proposal-only mutation required

## Test Approval

* **Name or role:** Test Architecture Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Adversarial plugin and sandbox escape suite required

---

# 177. Supersession

This ADR is superseded only when a later ADR:

* names ADR-0017 explicitly;
* explains why permission, capability or sandbox architecture changed;
* identifies installed-plugin grant migration;
* describes AppContainer identity and DACL migration;
* explains security and compatibility impact;
* and updates the `Superseded by` field.

Historical grants, catalogue versions and capability audit records remain available according to retention policy.

---

# 178. Change History

| Version | Date         | Author        | Summary                                                                                              |
| ------- | ------------ | ------------- | ---------------------------------------------------------------------------------------------------- |
| 0.1     | 18 July 2026 | Founder Draft | Initial brokered object-capability, permission catalogue, AppContainer and Job Object recommendation |

---

# 179. Final Decision Statement

> **Opure will provisionally use a deny-by-default, versioned plugin permission catalogue in which package declarations grant no authority, persistent developer and policy grants define only a maximum envelope, operation-specific approval binds exact resources and side effects, and the Runtime mints short-lived opaque non-delegable capability leases for trusted brokers, while every production third-party Plugin Host runs in a unique publisher-bound Windows AppContainer with no direct network, explicit package, data and named-pipe ACLs, a non-breakaway resource-limited Job Object, child-process denial and compatible process mitigations, because plugin trust, user consent, product-service authority and operating-system containment are distinct layers and none should provide ambient access to source repositories, networks, processes or secrets.**

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**