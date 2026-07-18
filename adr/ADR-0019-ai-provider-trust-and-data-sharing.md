# ADR-0019 — AI Provider Trust and Data Sharing

## Architecture Decision Record

**Status:** Proposed  
**Date:** 18 July 2026  
**Decision owners:** Founder and Product Owner  
**Technical owners:** AI Router and Provider Trust Owner  
**Reviewers:** Runtime Architecture Owner, Security Owner, Network Gateway Owner, Secrets Owner, Project Policy Owner, Workspace Owner, Memory Owner, Trust Centre Owner, Desktop Owner, Persistence Owner, Recovery Owner, Test Architecture Owner  
**Supersedes:** None  
**Superseded by:** None  
**Related ADRs:** ADR-0001 through ADR-0018  
**Related specifications:** CHARTER-001, SPEC-001 through SPEC-012  
**Target milestone:** Local AI Router through Version 1.0  

---

## 1. Decision Summary

Opure should use a **provider-neutral, deny-by-default AI data-sharing architecture** in which every local or remote model connection is represented by an explicit, reviewed Provider Profile and every inference request is governed by an exact Data Sharing Plan before any content leaves the trusted Runtime.

The initial AI provider architecture should:

- make local inference the preferred and default path;
- make cloud inference optional;
- make **Local Only** the default project cloud policy;
- support the project policies already established by the Charter:
  - Local Only;
  - Ask Every Time;
  - Approved Providers Only;
  - Custom;
- route all model operations through the trusted AI Router;
- route all remote network traffic through the Network Gateway;
- keep all credentials in the Secrets Vault;
- prohibit direct provider access from Desktop, plugins, MCP servers, workflows or project services;
- permit those components to request an AI operation only through typed Runtime contracts;
- represent every provider connection as an immutable Provider Profile revision;
- distinguish the service operator from the model publisher;
- distinguish direct-provider APIs from managed-cloud model hosting;
- distinguish local loopback inference from remote network inference;
- distinguish wire compatibility from trust;
- treat “OpenAI-compatible” as a protocol-adapter label only;
- require exact endpoint, operator, model source, account, project, region, deployment and authentication binding;
- never infer privacy or retention terms from an API shape;
- require a reviewed Data Handling Record for every remote Provider Profile;
- classify provider claims as:
  - Verified by current official documentation;
  - Verified by contract or enterprise configuration;
  - Declared by operator;
  - Inferred;
  - Unknown;
  - or Contradicted;
- fail closed for unknown training, retention, state storage, data-sharing or processing-location claims when project policy requires a known posture;
- attach the evidence URL, review date, evidence hash or contract reference and expiry to every claim;
- treat provider policy as time-sensitive and require periodic review;
- show stale or expired provider claims before use;
- never silently change a Provider Profile when a provider changes terms, endpoint behaviour or model alias;
- bind project approval to one Provider Profile revision and model policy;
- require fresh review for material changes;
- pin exact model snapshots or immutable deployment IDs where available;
- classify mutable aliases such as `latest` as non-reproducible;
- prohibit automatic migration to a newer model behind the user's back;
- allow explicit alias use only with a visible drift warning;
- create an exact Data Sharing Plan for every remote request;
- identify every selected source item, byte count, token estimate, data classification, provider, model, endpoint, processing region, retention posture, stateful feature, caching behaviour and intended purpose;
- show a human-readable preview before transmission when project policy requires approval;
- obtain deterministic policy approval or authenticated human approval;
- hash the canonical plan;
- bind the request to that plan hash;
- invalidate approval when content, provider, model, region, feature, data class or estimated scope changes materially;
- scan selected content for likely secrets and prohibited files before any remote request;
- hard deny raw Secrets Vault content, private keys, package-signing material, credential stores and other protected Opure state;
- redact or exclude likely credentials;
- provide no generic “send secrets anyway” control in Version 1;
- make project source sharing file and snapshot specific;
- prevent a model or plugin from expanding context after approval;
- apply explicit byte, file, image, audio and token budgets;
- prohibit automatic whole-repository upload;
- prohibit automatic hidden file upload;
- prohibit automatic inclusion of ignored files;
- prohibit automatic provider-side file upload;
- prohibit automatic provider-side conversation storage;
- prohibit automatic provider-side assistants, agents, threads, vector stores, knowledge bases, batches, fine-tuning jobs, computer-use tools, remote MCP tools, web-search tools or code-interpreter environments;
- use only stateless synchronous inference and stateless embeddings initially;
- send provider-specific `store=false` or equivalent controls when supported and verified;
- fail or warn when a provider endpoint cannot meet the selected stateless posture;
- distinguish provider abuse-monitoring retention from application-state retention;
- distinguish data-at-rest location from inference-processing location;
- distinguish service operator access from model publisher access;
- distinguish training use from safety review and operational logging;
- distinguish explicit persistent caching from transient or implicit caching;
- disable explicit remote prompt or context caching initially;
- permit unavoidable transient in-memory provider caching only when disclosed in the Provider Profile and accepted by policy;
- prohibit optional feedback or dataset-sharing controls by default;
- never opt the developer into provider training, feedback sharing or log-dataset contribution;
- keep feedback disabled unless a separate explicit user action identifies exactly what will be shared;
- use provider APIs intended for programmatic commercial use rather than consumer chat interfaces;
- classify free or unpaid API tiers separately from paid or enterprise tiers;
- block confidential project data from a tier whose terms permit product improvement or human review unless a specific project policy explicitly permits it;
- never assume that a provider's consumer product terms apply to its API or vice versa;
- never assume that a managed cloud's terms equal the underlying model publisher's direct API terms;
- record the complete provider chain:
  - Opure;
  - network operator;
  - managed-cloud operator;
  - model publisher;
  - external tool or grounding service;
- require separate approval when an AI endpoint invokes an external provider-side tool;
- treat provider-side web search, remote MCP, browser automation, computer use, code execution and grounding as additional external data-sharing destinations;
- make those features unavailable initially;
- keep API keys, cloud credentials, OAuth tokens and signing credentials out of prompts, logs, traces, memory, provider metadata and error messages;
- inject credentials only at the final Network Gateway boundary;
- use temporary cloud credentials where supported;
- support direct API keys only through Vault-bound secret-use handles;
- support managed-cloud authentication through least-privilege identities or short-lived credentials where available;
- keep provider account and project identities visible;
- never reuse a credential for a different endpoint or provider profile;
- use TLS with normal certificate validation;
- prohibit user-disableable TLS validation;
- apply exact destination allowlists;
- reject redirects by default for inference requests;
- validate DNS and prevent private-address or loopback confusion for remote profiles;
- permit loopback only for explicit local profiles;
- treat local model servers as executable or local-service trust boundaries;
- bind managed local servers to exact executable, package, hash, arguments, model files and process policy;
- bind unmanaged local endpoints to exact loopback origin and a visible Unknown or Declared operator posture;
- never classify a loopback endpoint as safe merely because it is local;
- prohibit automatic LAN model discovery;
- prohibit automatic connection to another machine;
- keep local model files under explicit user control;
- verify model-file hashes where available;
- record model licence and provenance where known;
- isolate Opure-managed local model processes using Process Supervisor and Job Objects;
- set resource limits according to Eco, Balanced, Performance and Turbo modes;
- make local model process logs bounded and redacted;
- make all remote fallback explicit;
- never fall back from local to cloud automatically;
- never fall back from one cloud provider to another automatically;
- never change region automatically;
- never retry a request on another provider without a new plan;
- allow retry only against the same profile and model when policy, idempotency and cost permit;
- stop retrying on authentication, policy, retention, region or validation failures;
- record provider request receipts without storing full prompt or response by default;
- record selected data classes, source hashes, provider, model, region, request feature flags, estimated and actual usage, policy, consent and result;
- store full prompts and outputs locally only when the owning workflow or user explicitly requires it;
- never store hidden chain-of-thought;
- retain only provider-returned user-visible reasoning summaries or structured results;
- classify all model output as untrusted proposed content;
- never apply code, patches, commands or repository changes directly from model output;
- require deterministic validation and the normal Patch or Tool review path;
- attribute every result to provider, model, profile revision, request plan and response ID;
- show that outputs may be inaccurate and non-unique;
- enforce per-project and per-profile cost, token, concurrency and rate budgets;
- provide a preflight cost estimate where pricing metadata is available;
- never exceed a hard budget through automatic retry;
- make pricing metadata date-stamped and non-authoritative until provider billing;
- support manual provider disablement and immediate revocation;
- stop in-flight operations when safe;
- quarantine a profile on endpoint, publisher, retention, credential or trust conflict;
- and keep local work functional when all remote providers are unavailable.

The initial supported Provider Profile classes should be:

1. **Embedded or In-Process Local**
2. **Opure-Managed Local Process**
3. **Verified Local Loopback Service**
4. **Unverified Local Loopback Service**
5. **Direct Cloud API**
6. **Managed Cloud Model Service**
7. **Enterprise Proxy or Gateway**
8. **Custom Remote Compatible Endpoint**
9. **Development or Experimental**
10. **Quarantined**
11. **Revoked or Withdrawn**

The initial Provider Data Posture levels should be:

- **P0 — Local Offline**
- **P1 — Local Network-Isolated**
- **P2 — Remote Stateless, Known Non-Training**
- **P3 — Remote Stateless with Abuse or Operational Retention**
- **P4 — Remote Stateful or Cached**
- **P5 — Remote Data Shared for Improvement or Provider Access**
- **PX — Unknown**
- **PD — Denied**

The posture is descriptive, not a safety guarantee.

The initial remote feature policy should allow only:

- synchronous text or multimodal inference;
- streaming response delivery;
- structured output where locally validated;
- and stateless embeddings.

The following should be disabled until separate approval:

- provider-side stored conversations;
- provider Files APIs;
- provider vector stores;
- provider persistent assistants or agents;
- provider prompt libraries;
- provider knowledge bases;
- provider batch jobs;
- provider fine-tuning;
- provider evaluations;
- provider explicit prompt caching;
- provider background mode;
- provider web search;
- provider remote MCP;
- provider computer use;
- provider code interpreter;
- provider browser automation;
- provider-generated executable actions;
- and provider-side long-running tasks.

The selected trust chain is:

```text
Provider registration
    ↓
Provider identity and endpoint verification
    ↓
Data Handling Record
    ↓
Project cloud policy
    ↓
Model and feature policy
    ↓
Exact context selection
    ↓
Secret and prohibited-content scan
    ↓
Canonical Data Sharing Plan
    ↓
Human or deterministic approval
    ↓
Vault-bound credential use
    ↓
Network Gateway request
    ↓
Provider response validation
    ↓
Untrusted-result classification
    ↓
Reviewable product action
    ↓
Trust Centre receipt
```

---

## 2. Status

Current status:

> **Proposed**

This ADR becomes **Accepted** only after prototypes demonstrate:

- local-only default behaviour;
- an Opure-managed local provider;
- an unmanaged loopback provider;
- one direct-cloud provider;
- one managed-cloud provider;
- exact Provider Profile revisioning;
- operator and model-publisher separation;
- current Data Handling Records;
- stale-claim handling;
- project cloud policy enforcement;
- exact model snapshot or explicit alias warnings;
- canonical Data Sharing Plans;
- file and snapshot binding;
- secret and prohibited-content scanning;
- no context expansion after approval;
- strict byte and token budgets;
- request preview;
- credential injection at the final boundary;
- no credential in logs, memory or provider payload;
- remote destination validation;
- no redirects;
- loopback separation;
- `store=false` or equivalent enforcement where supported;
- provider-stateful feature rejection;
- paid-versus-unpaid tier distinction;
- remote tool and grounding rejection;
- no local-to-cloud fallback;
- no cloud-to-cloud fallback;
- request receipt generation;
- output provenance;
- output non-authority;
- patch and command review;
- cost budgets;
- provider disablement;
- profile quarantine;
- and an adversarial secret-leakage, endpoint-confusion, retention-mismatch, alias-drift and hidden-context test suite.

---

## 3. Context

Opure intends to support:

- local language models;
- direct cloud model APIs;
- managed cloud model services;
- enterprise AI gateways;
- custom OpenAI-compatible endpoints;
- embeddings;
- multimodal inference;
- and future provider capabilities.

These services differ materially.

A local process may:

- read local files;
- open network connections;
- log prompts;
- load unverified model files;
- or expose an unauthenticated loopback endpoint.

A direct cloud provider may:

- retain abuse-monitoring logs;
- retain application state;
- offer optional zero-data-retention controls;
- process data in another region;
- and expose tools that send data to additional third parties.

A managed cloud platform may:

- host a model without sharing prompts with the original model publisher;
- process data globally, within a data zone or regionally;
- apply separate safety monitoring;
- and expose stateful services under the cloud operator's terms.

An “OpenAI-compatible” server may:

- implement only part of the protocol;
- have unknown retention;
- have no stable operator identity;
- ignore `store=false`;
- log full requests;
- proxy to another provider;
- or execute locally.

A free API tier may have different data-use terms from a paid tier.

A provider's current terms may change.

A model alias may move to a new snapshot.

A provider feature may store content even when basic inference is stateless.

An external tool enabled inside the provider API may send data beyond the named provider.

A provider response may contain:

- inaccurate code;
- malicious code;
- prompt injection;
- fabricated citations;
- hidden external instructions;
- or content copied from untrusted context.

The AI Router must therefore make trust and data sharing explicit.

---

## 4. Problem Statement

Opure requires a provider trust and data-sharing architecture that lets developers use local and remote models without hidden cloud transmission, unreviewed retention, secret leakage, silent model drift, provider-side persistence, automatic fallback, ambiguous operator identity or direct execution of untrusted model output.

---

## 5. Decision Drivers

The decision is evaluated against:

- Charter alignment;
- local-first operation;
- explicit cloud consent;
- provider neutrality;
- exact data visibility;
- secret protection;
- project isolation;
- provider policy volatility;
- retention transparency;
- processing-region transparency;
- training-use transparency;
- managed-cloud distinctions;
- reproducibility;
- model drift;
- cost control;
- provider availability;
- enterprise policy;
- offline operation;
- multimodal support;
- output safety;
- small-team implementation;
- and testability.

---

## 6. Governing Principles

This decision must preserve:

- Developer Respect;
- Developer First;
- Local by Design;
- Cloud Optional;
- Human in Control;
- Visible by Design;
- Inspectable Decisions;
- Least Privilege;
- No Hidden Cloud Request;
- No Hidden Context;
- No Secret Transmission;
- No Whole-Repository Upload;
- No Trust by Compatible API;
- No Trust by Model Name;
- No Trust by Provider Marketing Alone;
- No Silent Provider Fallback;
- No Silent Region Fallback;
- No Silent Model Alias Drift;
- No Provider-Side State by Default;
- No Optional Training Opt-In;
- No Automatic Feedback Sharing;
- No Consumer Endpoint for Project Data;
- No Output as Authority;
- No Hidden Chain-of-Thought Storage;
- Reversible Provider Grants;
- Immediate Provider Revocation;
- and Evidence-Based Confidence.

---

## 7. Scope

This ADR decides:

- Provider Profile architecture;
- provider identity;
- operator and model-publisher distinction;
- provider trust classes;
- data-posture levels;
- Data Handling Records;
- evidence and review;
- project cloud policy;
- model selection;
- model aliases and snapshots;
- Data Sharing Plans;
- context selection;
- secret scanning;
- remote feature policy;
- provider credentials;
- Network Gateway use;
- local provider trust;
- cloud fallback;
- retries;
- streaming;
- embeddings;
- multimodal content;
- provider output;
- request receipts;
- cost and quota;
- revocation;
- quarantine;
- recovery;
- Trust Centre;
- and acceptance testing.

This ADR does not decide:

- final provider adapter packages;
- final supported providers;
- final supported models;
- model benchmark policy;
- model quality scoring;
- model-download distribution;
- model licence catalogue;
- GPU scheduling details;
- fine-tuning;
- provider-side agents;
- provider web search;
- provider remote MCP;
- computer-use automation;
- cross-provider ensemble inference;
- speculative decoding;
- remote confidential computing;
- or enterprise contract negotiation.

---

## 8. Constraints

Known constraints include:

- The AI Router is provider neutral.
- Local providers remain the default.
- Project cloud policies are already defined by the Charter.
- Secrets must remain in the Vault.
- Network use must pass through the Network Gateway.
- Workspace and Memory services own context.
- Desktop is non-authoritative.
- Plugins and MCP servers cannot receive provider credentials.
- Remote providers have different retention and training policies.
- Provider policies and features change over time.
- Some APIs store application state by default.
- Some endpoints are incompatible with zero-data-retention controls.
- Some platforms distinguish paid and unpaid data use.
- Some managed clouds do not share prompts with the original model publisher.
- Some managed-cloud deployment types process data globally.
- Some model aliases are mutable.
- Some providers offer explicit caching or background processing.
- Some providers implicitly cache transient state.
- Some providers permit feedback and dataset sharing.
- Stateless inference can still involve operational or abuse-monitoring retention.
- A provider may return usage and response identifiers.
- Generated code is untrusted.
- Local model servers may log data or open network connections.
- and the initial implementation team is small.

---

## 9. Assumptions

This decision assumes:

- provider adapters can expose a common capability model;
- the Network Gateway can inject credentials at the final request boundary;
- the AI Router can create a canonical request plan;
- Workspace and Memory services can provide immutable context references;
- secret scanning can run before remote transmission;
- project cloud policy can evaluate data classes;
- provider terms can be recorded as reviewed evidence;
- model snapshots can be pinned where providers expose them;
- stateful provider features can remain disabled;
- local model processes can be supervised;
- remote output can be streamed through bounded validation;
- cost and token usage can be measured;
- and a provider outage need not affect local product operation.

---

## 10. Current Provider Evidence

Official provider documentation available on 18 July 2026 demonstrates why Opure cannot use one universal privacy assumption.

### 10.1 OpenAI API

Current official OpenAI API documentation states that:

- API data is not used to train or improve models unless the customer explicitly opts in;
- abuse-monitoring logs may contain prompts and responses and are retained for up to 30 days by default;
- approved customers may receive Modified Abuse Monitoring or Zero Data Retention;
- application-state retention varies by endpoint;
- the Responses API may retain response state for at least 30 days by default;
- `store=false` and Zero Data Retention change behaviour for eligible features;
- files, conversations, assistants, vector stores, batches and other stateful objects may persist until deletion or according to endpoint rules;
- some tools and features are not compatible with Zero Data Retention;
- and regional storage and regional processing are separate properties.

### 10.2 Anthropic API

Current Anthropic commercial-product documentation states that:

- Anthropic does not use API data for training unless an agreement states otherwise;
- API inputs and outputs are automatically deleted from backend systems within 30 days by default, subject to exceptions;
- ad hoc deletion of individual paid API requests is not generally supported;
- and approved customers may have Zero Data Retention arrangements for eligible APIs, subject to legal and safety exceptions.

### 10.3 Gemini Developer API

Current Gemini API documentation and terms distinguish paid and unpaid services.

They state that:

- paid-service prompts and responses are not used to improve Google products;
- unpaid-service content may be used to provide, improve and develop products and may be reviewed by humans, subject to regional terms;
- UK use is subject to the current paid-service data-use treatment described in the terms, but product tier and billing state remain material configuration;
- prompt and response logging may occur for policy enforcement;
- Interactions API state storage must be disabled explicitly for a zero-data footprint;
- File API content persists until deletion or expiry;
- explicit cached content persists for its configured lifetime;
- Live API session-resumption state may persist;
- and implicit in-memory caching may occur with a stated transient lifetime.

### 10.4 Microsoft Foundry and Azure-Hosted Models

Current Microsoft documentation states that:

- prompts, completions, embeddings and training data are not made available to the underlying model publishers for models sold or hosted by Azure;
- customer data is not used to train foundation models without permission;
- basic model inference is stateless;
- optional Responses, Threads and other stateful entities persist data;
- content may be reviewed for abuse under documented conditions;
- and processing location depends on whether the deployment is regional, data-zone or global.

### 10.5 Amazon Bedrock

Current AWS documentation states that:

- model providers generally do not have access to Bedrock deployment accounts, logs, prompts or completions;
- Bedrock offers explicit data-retention modes;
- a zero-retention `none` mode can reject incompatible model use rather than silently retain data;
- `default` mode can depend on model-specific policy;
- `provider_data_share` explicitly permits retention and sharing for models that require it;
- and cross-region inference affects where retained inputs and outputs may be stored or processed.

These examples establish that:

- the API provider;
- billing tier;
- endpoint;
- feature;
- region;
- account controls;
- model;
- and contract

all affect the true data posture.

Opure must therefore record actual profile configuration rather than a brand-level assumption.

---

## 11. Terminology

### 11.1 Provider

The service endpoint that accepts an AI operation.

---

### 11.2 Service Operator

The organisation operating the API and processing request data.

Examples may include a direct model company, a cloud provider or an enterprise gateway.

---

### 11.3 Model Publisher

The organisation that developed or published the model.

It may differ from the service operator.

---

### 11.4 Provider Profile

A versioned Opure configuration defining:

- endpoint;
- operator;
- account;
- authentication;
- region;
- deployment;
- supported features;
- models;
- data posture;
- and policy evidence.

---

### 11.5 Model Profile

A Provider Profile child record for one exact model snapshot, deployment or explicitly mutable alias.

---

### 11.6 Data Handling Record

The reviewed evidence describing:

- training use;
- retention;
- application state;
- safety review;
- processing location;
- storage location;
- subprocessors;
- model-publisher access;
- external tools;
- deletion;
- and contractual controls.

---

### 11.7 Data Sharing Plan

The canonical operation-specific record of exactly what Opure proposes to send, where, under which provider and model configuration, for what purpose and with which controls.

---

### 11.8 Context Item

One immutable data source selected for an AI request.

Examples:

- user instruction;
- source-file snapshot;
- diff;
- build error;
- repository metadata;
- project-memory record;
- image;
- or diagnostic excerpt.

---

### 11.9 Data Class

A policy category assigned to content.

---

### 11.10 Provider-Side State

Content persisted or retrievable at the provider beyond immediate inference.

---

### 11.11 Stateless Request

An operation in which Opure sends complete context and does not intentionally create provider-side persistent conversation or file state.

It may still be subject to abuse or operational retention.

---

### 11.12 Local Provider

A model service whose inference endpoint is on the same machine.

Local does not automatically mean trusted or offline.

---

### 11.13 Remote Provider

A service whose inference crosses the local machine boundary.

---

### 11.14 Protocol Compatibility

Compatibility with an API request and response shape.

It is not evidence of operator, security, privacy or model identity.

---

# 12. Options Considered

The principal architecture options are:

1. Provider Profiles plus per-request Data Sharing Plans.
2. One global cloud consent.
3. Trust providers by brand.
4. Trust any OpenAI-compatible endpoint.
5. Provider SDKs with provider-owned state.
6. Direct provider access from workflows and plugins.
7. Automatic local-to-cloud fallback.
8. Local-only product.
9. Cloud-only product.
10. Enterprise cloud gateway only.

---

# 13. Option A — Provider Profiles and Per-Request Plans

## 13.1 Advantages

- exact consent;
- provider neutrality;
- project isolation;
- feature-specific retention;
- evidence freshness;
- secret protection;
- model drift visibility;
- cost control;
- auditable data flow;
- and support for local, direct-cloud and managed-cloud models.

---

## 13.2 Disadvantages

- more metadata;
- more UI;
- provider evidence maintenance;
- additional policy complexity;
- and preflight latency.

---

## 13.3 Decision

Selected.

---

# 14. One Global Cloud Consent

Rejected because consent to one provider, project or data class cannot safely authorise every future provider and feature.

---

# 15. Trust by Brand

Rejected because one brand may expose:

- consumer;
- API;
- paid;
- unpaid;
- enterprise;
- stateful;
- regional;
- and third-party-tool

paths with different data terms.

---

# 16. Trust OpenAI-Compatible Endpoints

Rejected because compatibility does not prove:

- operator identity;
- model identity;
- retention;
- training;
- TLS;
- credential handling;
- or `store` semantics.

---

# 17. Provider SDK State

Provider SDKs may be used behind adapters where justified.

They may not make provider-managed conversation, file or agent state the Opure system of record.

---

# 18. Direct Workflow or Plugin Access

Rejected because credentials, context selection, policy and receipts would be fragmented.

---

# 19. Automatic Fallback

Rejected because a fallback changes:

- operator;
- region;
- cost;
- model;
- data terms;
- and result characteristics.

---

# 20. Local-Only Product

Rejected as a permanent architecture because cloud providers may be valuable when explicitly approved.

Local Only remains the default project policy.

---

# 21. Cloud-Only Product

Rejected because it conflicts with Local by Design and offline operation.

---

# 22. Enterprise Gateway Only

Rejected as the universal model because individuals and local-only developers need direct and local options.

Enterprise gateways remain supported profile types.

---

# 23. Decision

Opure will provisionally adopt:

> **Versioned Provider Profiles with reviewed Data Handling Records and exact per-request Data Sharing Plans, enforced by the AI Router, Project Cloud Policy, Secrets Vault and Network Gateway, with local inference preferred, remote stateful features disabled and no automatic provider, model or region fallback.**

This decision is:

- [ ] Permanent until superseded
- [x] Provisional pending provider, policy and leakage evidence
- [ ] Approval of provider-side state
- [ ] Approval of provider tools
- [ ] Approval of fine-tuning
- [ ] Approval of automatic cloud fallback

---

# 24. Provider Profile Architecture

A Provider Profile is the authoritative description of one configured AI service connection.

It is immutable after approval.

A change creates a new revision.

---

## 24.1 Provider Profile Identity

A Provider Profile is identified by an opaque:

```text
provider_profile_id
```

and a monotonically increasing:

```text
revision
```

The display name is not a security identity.

---

## 24.2 Required Fields

A profile should include:

```text
provider_profile_id
revision
display_name
profile_class
service_operator
model_publishers
endpoint
transport
account_reference
project_or_tenant_reference
region
processing_scope
storage_scope
deployment_type
authentication_method
credential_reference
supported_models
supported_features
data_posture
data_handling_record
network_policy
cost_policy
enabled
managed_by
created_at
reviewed_at
expires_at
```

---

## 24.3 Immutability

After a profile revision is approved:

- endpoint cannot change;
- operator cannot change;
- authentication binding cannot change;
- region cannot change;
- data posture cannot change;
- and model policy cannot broaden

without creating a new revision.

---

## 24.4 Profile Activation

Only one revision of a Provider Profile is active for new operations.

In-flight operations remain bound to the revision with which they started.

---

## 24.5 Historical Profiles

Historical revisions remain available for:

- request-receipt verification;
- incident response;
- data-sharing explanation;
- and workflow reproducibility.

---

# 25. Provider Profile Classes

## 25.1 Embedded or In-Process Local

Inference runs within a trusted first-party process or library.

This offers the strongest local-network posture but increases trusted-process and native-library risk.

---

## 25.2 Opure-Managed Local Process

Opure launches and supervises a local inference server or model process.

The profile binds:

- executable;
- arguments;
- package;
- model files;
- hashes;
- local endpoint;
- process limits;
- and network policy.

---

## 25.3 Verified Local Loopback Service

A separately installed local service with:

- exact loopback origin;
- executable or package evidence;
- publisher;
- hash;
- and reviewed logging and network posture.

---

## 25.4 Unverified Local Loopback Service

A loopback endpoint whose operator or executable cannot be independently verified.

It may be used only after explicit warning.

It is not assumed offline.

---

## 25.5 Direct Cloud API

Opure connects directly to the model provider's API.

---

## 25.6 Managed Cloud Model Service

A cloud operator hosts models published by itself or another organisation.

Examples include cloud model platforms whose operator, model publisher and data terms are distinct.

---

## 25.7 Enterprise Proxy or Gateway

An enterprise-controlled endpoint proxies or routes one or more model providers.

The enterprise operator is part of the provider chain.

---

## 25.8 Custom Remote Compatible Endpoint

A remote endpoint implementing a known wire protocol but lacking a built-in trust adapter.

Default posture is Unknown until reviewed.

---

## 25.9 Development or Experimental

A non-production endpoint, preview feature or mutable local development server.

It cannot inherit production approval.

---

## 25.10 Quarantined

New requests are denied.

Evidence is preserved.

---

## 25.11 Revoked or Withdrawn

The profile is no longer approved for use.

---

# 26. Provider Chain

Every remote profile records the complete processing chain.

Conceptual example:

```text
Opure Desktop
    ↓
Opure Runtime and AI Router
    ↓
Opure Network Gateway
    ↓
Enterprise AI Gateway
    ↓
Managed Cloud Operator
    ↓
Model Publisher Model
    ↓
Optional Grounding or Tool Provider
```

---

## 26.1 Operator Separation

The UI must distinguish:

- who operates the endpoint;
- who publishes the model;
- who bills the request;
- who may receive request content;
- and who may receive usage metadata.

---

## 26.2 Model Publisher Access

The Data Handling Record states whether the model publisher receives:

- prompt content;
- output content;
- usage information;
- contact or transaction metadata;
- or no customer content.

---

## 26.3 External Tool Provider

A provider-side tool introduces an additional operator.

It requires a separate data-flow entry.

---

## 26.4 Unknown Chain

If the full chain is unknown, the profile posture is PX.

---

# 27. Provider Identity

Provider identity is not one string.

It includes:

```text
service operator
canonical endpoint
TLS identity
account or tenant
project or subscription
deployment
region
model publisher
model identifier
data-handling evidence
authentication binding
```

---

## 27.1 Canonical Endpoint

Store:

- scheme;
- canonical hostname;
- effective port;
- base path;
- and API profile.

---

## 27.2 No Endpoint Wildcard

A production profile does not permit arbitrary hosts.

---

## 27.3 Subdomain Changes

A provider region or product subdomain change creates a new profile revision unless the approved endpoint policy explicitly includes it.

---

## 27.4 TLS Identity

Use normal operating-system trust validation.

A private enterprise certificate authority may be approved by enterprise policy.

---

## 27.5 Certificate Pinning

Do not pin a leaf certificate by default.

A future SPKI or enterprise pin requires rotation and recovery policy.

---

# 28. Authentication Methods

Supported profile authentication types may include:

- API key;
- OAuth access token;
- short-lived cloud credential;
- workload identity;
- managed identity;
- signed cloud request;
- mutual TLS;
- local bearer token;
- or no authentication for a verified local endpoint.

---

## 28.1 Authentication Does Not Equal Trust

Successful authentication proves only that the credential was accepted by the endpoint.

---

## 28.2 Credential Binding

A credential reference is bound to:

- Provider Profile revision;
- endpoint;
- service operator;
- account;
- project or tenant;
- region;
- authentication scheme;
- and permitted operation classes.

---

## 28.3 No Credential Reuse

A credential for one profile cannot be reused automatically for another profile.

---

## 28.4 Temporary Credentials

Prefer:

- managed identity;
- workload identity;
- OAuth;
- STS;
- or another short-lived credential

where supported.

---

## 28.5 API Keys

API keys remain supported through Secrets Vault handles.

They are never displayed after entry.

---

# 29. Provider Account Identity

The UI should show a safe account label such as:

- organisation;
- tenant;
- cloud subscription;
- cloud project;
- billing project;
- or enterprise gateway account.

Do not show the secret credential.

---

## 29.1 Multiple Accounts

One endpoint with two accounts requires two Provider Profiles or explicitly separate profile account bindings.

---

## 29.2 Billing Context

The account or project charged for the operation must be visible.

---

# 30. Data Handling Record

Every remote profile requires a Data Handling Record.

---

## 30.1 Required Claims

The record should contain:

```text
training_use
product_improvement_use
human_review
abuse_monitoring
abuse_monitoring_retention
application_state
application_state_retention
deletion_control
prompt_caching
file_storage
batch_storage
fine_tuning_storage
processing_location
storage_location
model_publisher_access
external_tool_data_sharing
encryption
customer_managed_keys
zero_retention_control
contractual_control
subprocessors
policy_url
terms_url
evidence_date
evidence_status
evidence_expiry
reviewer
```

---

## 30.2 Evidence Status

Each claim uses one of:

- Verified Current Official Documentation;
- Verified Contract;
- Verified Technical Configuration;
- Operator Declaration;
- Inferred;
- Unknown;
- or Contradicted.

---

## 30.3 Evidence Scope

Evidence must identify its exact scope.

Examples:

- paid Gemini API;
- unpaid Gemini API;
- OpenAI API project with default monitoring;
- OpenAI API project with approved ZDR;
- Anthropic API default;
- Azure regional deployment;
- Azure global deployment;
- Bedrock retention mode `none`;
- Bedrock retention mode `default`;
- or custom enterprise gateway.

---

## 30.4 Brand-Level Evidence Prohibited

A statement about a provider brand cannot be applied to every:

- account;
- product;
- endpoint;
- tier;
- region;
- model;
- or feature.

---

## 30.5 Evidence Expiry

Suggested default review periods:

- official policy documentation: 90 days;
- contractual record: contract review date or 12 months;
- technical profile verification: 30 days;
- experimental provider: 14 days;
- and operator self-declaration: 30 days.

Exact periods require governance review.

---

## 30.6 Stale Evidence

Stale evidence causes:

- warning;
- blocking under strict project policy;
- or reapproval

according to risk.

---

## 30.7 Changed Evidence

A material change creates a new Provider Profile revision.

---

# 31. Material Provider Changes

Material changes include:

- training-use change;
- retention increase;
- new human review;
- new model-publisher access;
- new external tool;
- new region;
- global processing;
- state storage default;
- caching change;
- new endpoint;
- new operator;
- new authentication scheme;
- new model alias target;
- and deletion-control change.

---

## 31.1 Narrowing Change

A verified improvement such as lower retention may be proposed as a new revision.

It is not applied silently.

---

## 31.2 Provider Terms Unavailable

If evidence becomes unavailable:

- mark Unknown;
- retain historical evidence hash;
- block strict remote use;
- and request review.

---

# 32. Data Posture Levels

## 32.1 P0 — Local Offline

Requirements:

- inference remains on the device;
- no inference-process network access;
- no external telemetry;
- local model and process are verified;
- prompts and outputs remain under local Opure policy.

---

## 32.2 P1 — Local Network-Isolated

Inference is local and the process is intended to be network isolated.

The process may still persist local logs or cache.

---

## 32.3 P2 — Remote Stateless, Known Non-Training

The operator states or contractually commits that:

- request content is not used for model improvement;
- no intentional provider-side application state is created;
- and any operational retention is absent or separately classified.

---

## 32.4 P3 — Remote Stateless with Abuse or Operational Retention

Basic inference is stateless from the application perspective, but the provider may retain content for:

- abuse monitoring;
- reliability;
- legal compliance;
- or operational review.

---

## 32.5 P4 — Remote Stateful or Cached

The operation creates or uses:

- provider conversations;
- files;
- vector stores;
- explicit caches;
- background jobs;
- or other retrievable state.

Disabled initially.

---

## 32.6 P5 — Remote Data Shared for Improvement or Provider Access

The request or output may be:

- used for product or model improvement;
- human reviewed beyond narrow safety controls;
- shared with the model publisher;
- or contributed to a dataset.

Confidential project data is denied by default.

---

## 32.7 PX — Unknown

Material claims are unknown or unverifiable.

Remote confidential data is denied.

---

## 32.8 PD — Denied

Profile is unavailable under current product or enterprise policy.

---

## 32.9 Posture Is Descriptive

A lower number is not a guarantee of model quality or complete security.

---

# 33. Data Posture Calculation

Posture is derived from the most permissive material property.

Example:

- non-training;
- 30-day abuse retention;
- no application state

maps to P3 rather than P2.

---

## 33.1 Feature-Specific Posture

A Provider Profile may have different posture per feature.

Example:

- stateless chat: P3;
- files: P4;
- unpaid feedback sharing: P5.

---

## 33.2 Request Posture

The effective request posture is computed from:

- profile;
- model;
- endpoint;
- feature;
- account controls;
- retention mode;
- and external tools.

---

# 34. Provider Profile Trust States

A profile may be:

- Draft;
- Awaiting Evidence;
- Awaiting Credential;
- Awaiting Approval;
- Approved;
- Approved with Restrictions;
- Evidence Stale;
- Degraded;
- Disabled;
- Quarantined;
- Revoked;
- or Removed.

---

## 34.1 Approved with Restrictions

Restrictions may include:

- public data only;
- no source code;
- no images;
- no personal data;
- no stateful features;
- one region;
- one model;
- one project;
- or one cost budget.

---

# 35. Provider Registration

Registration is explicit.

---

## 35.1 Built-In Adapter

A built-in adapter may prefill official endpoints and known fields.

The user still reviews:

- account;
- profile class;
- data handling;
- region;
- features;
- and credential.

---

## 35.2 Custom Adapter

A custom compatible endpoint requires:

- exact base URI;
- operator name;
- authentication scheme;
- model list;
- data claims;
- and evidence.

Unknown fields default to restrictive values.

---

## 35.3 No Auto-Discovery

Opure does not scan:

- LAN;
- environment variables;
- shell configuration;
- browser sessions;
- cloud accounts;
- or local ports

to register providers automatically.

---

## 35.4 Import

Provider configuration import requires preview and approval.

Secrets remain unresolved references.

---

# 36. Provider Verification

Verification may include:

- DNS;
- TLS;
- endpoint metadata;
- account identity;
- model enumeration;
- region;
- configured retention mode;
- supported store controls;
- and test request.

---

## 36.1 Verification Request

A verification request must not contain project data.

Use a fixed synthetic prompt.

---

## 36.2 Model List

Provider model enumeration is untrusted metadata.

It does not automatically enable new models.

---

## 36.3 Hidden Models

A manually entered model identifier requires an explicit unsupported-model warning.

---

# 37. Model Profile

Every enabled model has a Model Profile.

---

## 37.1 Fields

```text
model_profile_id
provider_profile_revision
provider_model_id
model_publisher
model_family
model_snapshot
alias_type
release_status
input_modalities
output_modalities
context_limit
output_limit
tool_support
structured_output
embedding_dimensions
pricing
data_posture_overrides
approved_use_classes
created_at
reviewed_at
```

---

## 37.2 Exact Snapshot

Prefer immutable identifiers containing:

- date;
- version;
- deployment ID;
- or provider snapshot.

---

## 37.3 Mutable Alias

A mutable alias must be labelled:

```text
Mutable Provider Alias
```

---

## 37.4 Alias Resolution

Record the provider-reported concrete model identifier in each request receipt where available.

---

## 37.5 Alias Change

A detected alias target change:

- pauses reproducible workflows;
- updates quality and policy status;
- and requires review under strict policy.

---

# 38. Model Approval

A model is approved for one or more use classes:

- general assistance;
- code explanation;
- code generation;
- patch proposal;
- embeddings;
- image understanding;
- audio transcription;
- or other explicit categories.

---

## 38.1 Model Capability Is Not Permission

A model supporting tools does not enable tools.

---

## 38.2 Preview Models

Preview or experimental models are disabled in Stable by default.

---

## 38.3 Deprecated Models

A deprecated model remains available only according to support and migration policy.

---

# 39. Model Drift

Model drift may occur through:

- alias change;
- provider-side update;
- safety-system change;
- context-limit change;
- tool behaviour;
- structured-output change;
- or hidden infrastructure change.

---

## 39.1 Reproducible Workflow

A reproducible workflow requires:

- exact Provider Profile revision;
- exact model snapshot;
- exact AI Router prompt template;
- exact context hashes;
- exact inference parameters;
- and output provenance.

Stochastic output remains possible.

---

## 39.2 Mutable Alias Workflow

A workflow using an alias is explicitly non-reproducible.

---

# 40. Project Cloud Policy

The Project Cloud Policy is authoritative.

---

## 40.1 Local Only

Allows only P0 and approved P1 profiles.

No remote request.

---

## 40.2 Ask Every Time

Every remote Data Sharing Plan requires authenticated human approval.

---

## 40.3 Approved Providers Only

A project policy lists approved:

- Provider Profile revisions;
- model profiles;
- data classes;
- features;
- regions;
- and budgets.

A fresh human approval may still be required for high-risk content.

---

## 40.4 Custom

Custom policy may define:

- allowlists;
- denylists;
- data-class rules;
- provider posture maxima;
- model categories;
- region;
- file patterns;
- cost limits;
- persistence;
- and operation-specific approvals.

---

## 40.5 Deny Wins

Any project, enterprise or hard product deny wins.

---

## 40.6 Policy Cannot Broaden Profile

A project cannot enable a feature the Provider Profile prohibits.

---

# 41. Enterprise Policy

Enterprise policy may:

- prohibit direct providers;
- require an enterprise gateway;
- require one region;
- require P2 or better;
- require ZDR or an equivalent control;
- prohibit unpaid services;
- prohibit mutable aliases;
- require managed identity;
- set budgets;
- and require evidence freshness.

---

## 41.1 Enterprise Policy Source

The UI shows the policy source.

---

## 41.2 No User Override

The user cannot override enterprise denial.

---

# 42. Data Classification

The initial AI data classes should include:

- `public`;
- `user.instruction`;
- `project.metadata`;
- `project.source`;
- `project.diff`;
- `project.build-output`;
- `project.test-output`;
- `project.memory`;
- `project.generated`;
- `diagnostics.redacted`;
- `image.project`;
- `audio.project`;
- `personal.data`;
- `authentication.metadata`;
- `secret.likely`;
- `secret.confirmed`;
- `security.protected`;
- and `prohibited`.

---

## 42.1 Public

Content intentionally public.

---

## 42.2 User Instruction

User-authored request text.

It may still contain secrets or personal data and is scanned.

---

## 42.3 Project Metadata

Examples:

- language;
- build target;
- dependency names;
- repository branch;
- and file names.

Paths may reveal personal or project information.

---

## 42.4 Project Source

Source and configuration content not classified as secret.

---

## 42.5 Project Diff

A reviewed or proposed source change.

---

## 42.6 Build and Test Output

May contain:

- source excerpts;
- paths;
- environment values;
- tokens;
- or personal data.

It is scanned and redacted.

---

## 42.7 Project Memory

Local memory summaries and records.

Secret and prohibited entries remain excluded.

---

## 42.8 Personal Data

Names, email addresses, identifiers or other personal information.

---

## 42.9 Likely Secret

Content matching secret heuristics but not yet confirmed.

Remote transmission is denied initially.

---

## 42.10 Confirmed Secret

Known credential or protected value.

Never transmitted as ordinary model content.

---

## 42.11 Security Protected

Examples:

- Vault keyring;
- signing material;
- recovery keys;
- OAuth refresh tokens;
- security incident evidence;
- and privileged configuration.

Hard denied.

---

## 42.12 Prohibited

Content disallowed by product, law, enterprise policy or provider terms.

---

# 43. Context Sources

Permitted context sources may include:

- direct user instruction;
- explicitly selected file snapshots;
- selected code ranges;
- selected diffs;
- selected build or test output;
- selected repository metadata;
- approved project-memory records;
- approved images;
- approved audio;
- and deterministic system instructions.

---

## 43.1 No Ambient Workspace

The AI Router does not read arbitrary workspace files based on model request.

---

## 43.2 No Current Project Inference

Every context source binds an explicit project reference.

---

## 43.3 No Hidden Memory

Memory records included in a request appear in the plan summary.

---

## 43.4 No Secret Store

The Vault is not a context source.

---

# 44. Context Item

Each Context Item includes:

```text
context_item_id
source_type
project_id
resource_reference
workspace_generation
content_hash
byte_count
token_estimate
data_class
display_label
selection_reason
redaction_state
```

---

## 44.1 Immutable Content

Approval binds the content hash.

---

## 44.2 Changed Content

If source content changes:

- invalidate approval;
- create a new Context Item;
- and rebuild the plan.

---

## 44.3 Token Estimate

Token estimate is advisory until provider tokenisation.

---

# 45. Context Selection

Context selection is deterministic and inspectable.

---

## 45.1 User Selection

The developer may select:

- project;
- files;
- ranges;
- diff;
- logs;
- images;
- and memory.

---

## 45.2 Workflow Selection

A workflow may select context only within its approved scope.

---

## 45.3 AI Suggestion

A model may suggest additional context.

It cannot obtain it automatically.

---

## 45.4 Plugin Suggestion

A plugin may propose context through its capabilities.

It cannot bypass project cloud policy.

---

# 46. Context Expansion

After approval, no component may add:

- another file;
- more lines;
- more memory;
- an image;
- a tool result;
- or hidden conversation history

without rebuilding the plan.

---

## 46.1 Provider Conversation History

Provider-side conversation IDs are not used initially.

Opure supplies approved local conversation context explicitly.

---

## 46.2 System Instructions

The exact Opure system instruction template version is part of the plan.

---

# 47. Data Sharing Plan

The Data Sharing Plan is the central operation record.

---

## 47.1 Required Fields

```text
plan_id
schema_version
provider_profile_id
provider_profile_revision
model_profile_id
model_identifier
resolved_model_snapshot
endpoint
operator_chain
account
region
processing_scope
storage_scope
feature
purpose
project
context_items
input_modalities
data_classes
bytes
token_estimate
output_limit
retention_posture
training_posture
stateful_features
cache_features
external_tools
credential_binding
cost_estimate
policy_decision
approval_requirement
created_at
expires_at
```

---

## 47.2 Canonicalisation

Canonicalise:

- profile identifiers;
- model;
- parameters;
- ordered context items;
- content hashes;
- data classes;
- region;
- feature flags;
- and budgets.

---

## 47.3 Plan Hash

Compute a SHA-256 plan hash.

---

## 47.4 Approval Binding

Human or policy approval binds the exact plan hash.

---

## 47.5 Expiry

Suggested provisional expiry:

- interactive remote request: 5 minutes;
- long review screen: 15 minutes;
- local request: session specific;
- and no plan survives a Provider Profile revision change.

---

## 47.6 One Request

A remote plan authorises one request or one explicitly bounded streaming operation.

---

# 48. Plan Preview

The human-readable preview should show:

- provider and service operator;
- model and snapshot or alias;
- account and billing context;
- destination;
- region and processing scope;
- project;
- selected files and ranges;
- other context categories;
- data classes;
- likely secret findings;
- bytes and token estimate;
- retention;
- training or product-improvement posture;
- human review posture;
- provider-side state;
- caching;
- external tools;
- estimated cost;
- and output destination.

---

## 48.1 Expandable Detail

The developer can expand to:

- relative paths;
- line ranges;
- memory entries;
- images;
- system instruction version;
- and redaction details.

---

## 48.2 No Secret Display

The preview identifies a likely secret finding without displaying the secret value.

---

# 49. Approval Modes

Possible approval results:

- Deny;
- Allow Once;
- Allow This Exact Plan;
- Allow Equivalent Plans for This Project;
- Allow This Provider and Data Class;
- Allow Under Project Policy;
- or Enterprise Managed.

---

## 49.1 Default Remote Approval

Under Ask Every Time:

```text
Allow Once
```

only.

---

## 49.2 Equivalent Plan

Equivalent-plan approval is restricted to deterministic bounded workflows.

It binds:

- profile revision;
- model profile;
- project;
- data classes;
- path patterns;
- maximum bytes;
- feature;
- retention posture;
- and expiry.

---

## 49.3 No Global Allow Everything

The UI does not offer one unrestricted “always send everything to this provider” option.

---

# 50. Policy Decision

A plan decision returns:

```text
allow
deny
approval_required
allow_with_redaction
allow_with_narrowed_context
provider_evidence_stale
provider_profile_quarantined
```

---

## 50.1 Reasons

Return stable reason codes and human-readable explanations.

---

## 50.2 Narrowed Context

Policy may remove context items.

It cannot add context.

---

# 51. Secret Scanning

Every remote plan is scanned before approval and again before transmission.

---

## 51.1 Scan Sources

Scan:

- user instruction;
- source files;
- diffs;
- build output;
- test output;
- memory;
- image metadata where practical;
- and structured tool results.

---

## 51.2 Known Secret Registry

Compare against known Vault-derived canary fingerprints without exposing secret values.

---

## 51.3 Heuristics

Detect likely:

- API keys;
- private keys;
- certificates;
- connection strings;
- passwords;
- tokens;
- cookies;
- credentials;
- signed URLs;
- and recovery codes.

---

## 51.4 Confirmed Match

Confirmed secret content is excluded and blocks the plan if required context depends on it.

---

## 51.5 Likely Match

Likely secret content is denied for remote transmission in Version 1.

---

## 51.6 Redaction

Redaction may replace a value with:

```text
[REDACTED SECRET]
```

only when the remaining context is useful and no structural leak remains.

---

## 51.7 No Generic Override

Version 1 provides no generic user override to send detected secret material.

---

# 52. Prohibited File Classes

Hard deny:

- private key files;
- PFX and certificate private-key containers;
- Vault storage;
- password databases;
- credential caches;
- browser profile secrets;
- package-signing keys;
- recovery codes;
- cloud credential files;
- and protected Opure security state.

---

## 52.1 File Extension Is Not Enough

Classification uses:

- path;
- content;
- metadata;
- and known protected roots.

---

# 53. Hidden and Ignored Files

Hidden, ignored and generated files are not included automatically.

---

## 53.1 Explicit Selection

A user may select a non-secret ignored file after a warning.

---

## 53.2 Environment Files

`.env` and equivalent secret-bearing files are denied by default.

---

# 54. Personal Data

Personal data may require:

- project-policy approval;
- purpose;
- provider posture;
- region;
- and user review.

---

## 54.1 Data Minimisation

Prefer:

- placeholders;
- pseudonyms;
- summaries;
- or selected fields.

---

# 55. Context Budgets

Every plan has limits.

Suggested initial defaults:

- source files: 50;
- total source bytes: 2 MiB;
- build or test output: 512 KiB;
- memory records: 50;
- images: 5;
- individual image: 10 MiB;
- audio: disabled initially unless transcription feature is approved;
- request tokens: model and project specific;
- and output tokens: explicit.

Exact limits require evidence.

---

## 55.1 Whole Repository

Whole-repository transmission is unavailable initially.

---

## 55.2 Large Context

A request above defaults requires explicit expanded review.

---

# 56. Multimodal Context

Images and audio can contain sensitive information.

---

## 56.1 Image Preview

Show thumbnails and metadata.

---

## 56.2 Metadata

Strip unnecessary EXIF or location metadata before remote transmission.

---

## 56.3 Screenshots

Screenshots may expose:

- source;
- credentials;
- notifications;
- personal data;
- and unrelated applications.

Require explicit selection.

---

## 56.4 Audio

Remote audio processing is disabled initially unless a profile and feature-specific policy is approved.

---

# 57. System Instructions

Opure system instructions are:

- source controlled;
- versioned;
- provider adapter aware;
- and part of the request plan.

---

## 57.1 No Provider Policy Override

A provider response cannot modify system instructions.

---

## 57.2 No User Secret

System instructions contain no credential or private project content.

---

# 58. Conversation Context

Opure owns conversation state locally.

---

## 58.1 Local Transcript

A local conversation record may be selected as context.

---

## 58.2 Provider State

Provider conversation, thread or response-state IDs are disabled initially.

---

## 58.3 Truncation

Context truncation is deterministic and visible.

---

## 58.4 Summarisation

A local summary may replace older context.

If generated by AI, it retains provenance and review status.

---

# 59. Memory Context

Memory service supplies only approved records.

---

## 59.1 Secret Exclusion

Secret and prohibited memory records never enter a remote plan.

---

## 59.2 Provenance

Every memory item retains source and freshness.

---

## 59.3 Embedding Boundary

Remote embeddings are a separate request class.

---

# 60. Embeddings

Stateless embeddings are supported as a separate feature.

---

## 60.1 Data Sharing

The full embedded content is sent to the provider.

The UI and policy must treat embeddings as data transmission, not harmless hashing.

---

## 60.2 Vector Privacy

An embedding can reveal semantic information.

Local vector storage remains sensitive project data.

---

## 60.3 Provider-Side Vector Stores

Disabled initially.

---

## 60.4 Model Binding

Stored vectors bind:

- embedding provider profile revision;
- exact model;
- dimensions;
- normalisation;
- and content hash.

---

## 60.5 Model Change

Changing the embedding model requires re-embedding.

---

# 61. Data Handling Review UI

Provider settings should include:

```text
Identity
Endpoint
Operator Chain
Authentication
Models
Training Use
Retention
Application State
Caching
Processing Location
Storage Location
Human Review
External Tools
Deletion
Contract and Evidence
Project Approvals
History
```

---

## 61.1 Evidence Link

Display evidence source and last reviewed date.

---

## 61.2 Plain Language

Translate provider-specific terminology into Opure's common fields.

---

## 61.3 Original Wording

Preserve a short source note or exact policy reference for audit without copying excessive source text.

---

# 62. Provider Evidence Update

Evidence updates are an explicit administrative operation.

---

## 62.1 No Hidden Web Fetch

Opure does not silently accept changed terms discovered online.

A future policy-update service may notify, but review remains explicit.

---

## 62.2 Adapter Update

A first-party adapter update may include updated provider evidence templates.

The user reviews material changes before reactivation.

---

# 63. Profile Portability

A Provider Profile may be exported without:

- credential;
- token;
- or private contract.

---

## 63.1 Exported Fields

Include:

- endpoint;
- operator;
- model policy;
- evidence references;
- data posture;
- and restrictions.

---

## 63.2 Import

Imported profiles return to Awaiting Evidence and Awaiting Credential unless enterprise-signed policy establishes trust.

---

# 64. Initial Remote Feature Policy

The initial remote feature set is deliberately small.

Allowed:

- synchronous text inference;
- streaming text inference;
- approved image input;
- approved structured output;
- and stateless embeddings.

Everything else is disabled until separately reviewed.

---

# 65. Provider-Side State

Provider-side state includes:

- conversations;
- threads;
- assistants;
- responses stored for retrieval;
- files;
- vector stores;
- cached content;
- prompt libraries;
- agents;
- knowledge bases;
- batches;
- evaluations;
- and fine-tuned models.

---

## 65.1 Default

Disabled.

---

## 65.2 Why Disabled

Provider-side state creates:

- another system of record;
- deletion obligations;
- retention ambiguity;
- stale context;
- provider lock-in;
- data-residency complexity;
- and hidden context expansion.

---

## 65.3 Future Gate

A stateful feature requires:

- exact purpose;
- retention;
- deletion;
- account;
- region;
- encryption;
- project mapping;
- recovery;
- export;
- and superseding or amending ADR.

---

# 66. `store=false` and Equivalent Controls

When a provider supports a request-level storage control:

- the adapter sends the most restrictive supported value;
- the profile records that behaviour;
- and tests verify the wire request.

---

## 66.1 Not a Universal Guarantee

`store=false` may not disable:

- abuse monitoring;
- safety review;
- transient caching;
- legal retention;
- or provider-side external tools.

---

## 66.2 Unsupported Control

If the selected project requires stateless application behaviour and the endpoint cannot provide it, deny the request.

---

# 67. OpenAI API Profile Policy

A built-in OpenAI API adapter should:

- use an official API endpoint;
- use an API project and account visible in the profile;
- store the API key in Vault;
- send `store=false` for eligible stateless requests;
- avoid Conversations, Assistants, Threads, Vector Stores, Files, Batches and Fine-Tuning initially;
- avoid background mode;
- avoid provider Code Interpreter;
- avoid provider web search;
- avoid provider remote MCP;
- avoid provider computer-use features;
- record default abuse-monitoring retention or approved ZDR/MAM posture;
- record regional endpoint and regional-processing eligibility separately;
- and treat endpoint eligibility as model and feature specific.

---

## 67.1 Zero Data Retention

A ZDR claim requires:

- account approval evidence;
- exact organisation or project setting;
- endpoint eligibility;
- model eligibility;
- feature eligibility;
- and technical verification.

A documentation statement that ZDR exists is not proof that the configured account has it.

---

## 67.2 Responses API

If used for stateless inference:

- set `store=false`;
- prohibit conversation IDs;
- prohibit background mode;
- prohibit remote tools;
- and record the exact endpoint behaviour.

---

# 68. Anthropic API Profile Policy

A built-in Anthropic adapter should:

- use the commercial API rather than consumer Claude interfaces;
- bind one API account;
- keep the key in Vault;
- record default backend deletion posture and exceptions;
- record whether a ZDR agreement applies;
- avoid Files and other persistent features initially;
- avoid provider tool integrations not represented in the plan;
- and treat prompt caching according to feature-specific retention evidence.

---

## 68.1 Ad Hoc Deletion

The profile should state when individual API request deletion is unavailable.

---

# 69. Gemini Developer API Profile Policy

A built-in Gemini adapter should:

- distinguish paid and unpaid service treatment;
- record billing-enabled project state;
- record applicable regional terms;
- keep API credentials in Vault;
- prefer stable `v1` API features;
- set `store=false` where a stateful API supports it;
- avoid Files API;
- avoid explicit cached content;
- avoid Live session resumption;
- avoid optional log-dataset sharing;
- avoid feedback sharing;
- avoid provider grounding and external tools initially;
- and record implicit transient caching where current documentation says it applies.

---

## 69.1 Unpaid Tier

A profile whose current terms permit use of content for product improvement or human review is P5 unless region-specific terms and technical configuration establish otherwise.

Confidential project source is denied by default.

---

## 69.2 Billing Change

A billing-state change is material and creates a new profile revision.

---

# 70. Microsoft Foundry Profile Policy

A Microsoft Foundry or Azure-hosted model profile should:

- identify the Azure tenant, subscription, resource and deployment;
- identify the model publisher separately from Microsoft;
- record whether the deployment is regional, data-zone or global;
- record storage geography separately from processing geography;
- use managed identity or short-lived Azure credentials where practical;
- use API keys only through Vault when necessary;
- disable Responses, Threads and stateful entities initially;
- avoid provider agents and grounding services;
- record abuse-monitoring configuration;
- record whether modified abuse monitoring or an equivalent control applies;
- and record any model-publisher marketplace metadata sharing.

---

## 70.1 Global Deployment

A Global deployment cannot be labelled regional merely because the Azure resource has a region.

---

## 70.2 Data Zone

A data-zone deployment is labelled with its actual processing zone.

---

## 70.3 Regional

A regional deployment records the exact processing region where current service documentation supports it.

---

# 71. Amazon Bedrock Profile Policy

A Bedrock profile should:

- identify AWS account, region and model;
- use IAM role or temporary STS credentials where practical;
- keep static access keys in Vault only when unavoidable;
- record effective data-retention mode;
- prefer `none` when project policy requires zero durable retention;
- reject a model incompatible with required retention mode;
- record whether `provider_data_share` is enabled;
- identify model publisher;
- record cross-region inference behaviour;
- avoid Knowledge Bases, Agents, Prompt Management, Files, Batches and other stateful services initially;
- and record whether the selected model requires provider data sharing.

---

## 71.1 Retention Mode

The adapter should verify the effective retention mode through the supported control plane where permissions permit.

---

## 71.2 Provider Data Share

A request requiring `provider_data_share` is P5 and requires explicit project approval.

---

# 72. Enterprise Gateway Profile Policy

An enterprise gateway profile should record:

- enterprise operator;
- gateway endpoint;
- downstream providers;
- routing policy;
- logging;
- retention;
- model alias policy;
- region;
- authentication;
- and contract.

---

## 72.1 Hidden Routing

If the gateway cannot disclose which downstream provider or region handles a request, posture is Unknown for those properties.

---

## 72.2 Dynamic Routing

Dynamic routing requires the plan to display the set of possible operators, models and regions.

Strict projects may deny it.

---

# 73. Custom Compatible Endpoint Policy

A custom endpoint begins as:

```text
PX — Unknown
```

unless evidence establishes a narrower posture.

---

## 73.1 Protocol Adapter

The user selects a wire adapter such as:

- OpenAI-compatible;
- Anthropic-compatible;
- Gemini-compatible;
- or custom REST.

This is not a trust classification.

---

## 73.2 Required Review

Show:

- exact endpoint;
- TLS;
- operator;
- account;
- credential scheme;
- model;
- logging;
- retention;
- training;
- state;
- and region.

---

## 73.3 Unsupported Semantics

If a custom endpoint ignores or does not implement a safety-relevant field:

- record it;
- adjust posture;
- and prevent strict policy from relying on it.

---

# 74. Local Provider Policy

A local provider is not automatically P0.

---

## 74.1 Local Posture Factors

Evaluate:

- process ownership;
- executable identity;
- model files;
- network access;
- logging;
- prompt cache;
- data directory;
- telemetry;
- remote proxy behaviour;
- and endpoint authentication.

---

## 74.2 Managed Local Process

An Opure-managed local process should:

- launch through Process Supervisor;
- use exact executable and arguments;
- use verified package and model files;
- bind only loopback or private IPC;
- have network denied where practical;
- receive a minimal environment;
- use a dedicated data and cache directory;
- use bounded logs;
- run in a non-breakaway Job Object;
- and stop when Opure releases it.

---

## 74.3 Unmanaged Loopback

An unmanaged local endpoint must show:

- address;
- port;
- owner evidence;
- authentication;
- whether it may proxy remotely;
- and whether it logs prompts.

---

## 74.4 No LAN Discovery

Do not discover or connect to:

- mDNS;
- broadcast;
- peer devices;
- or arbitrary local subnet endpoints.

---

## 74.5 Localhost Confusion

Validate that a local profile resolves only to loopback.

---

## 74.6 Model File Provenance

Record where available:

- model name;
- publisher;
- licence;
- quantisation;
- format;
- size;
- SHA-256;
- source;
- and download date.

---

## 74.7 Model File Change

A changed hash creates a new Model Profile revision.

---

# 75. Local Model Network Isolation

Preferred local process posture:

- no outbound network;
- no inbound non-loopback listener;
- no cloud telemetry;
- no update check;
- and no remote model download during inference.

---

## 75.1 Download Separation

Model download is a separate explicit operation through Network Gateway.

---

## 75.2 Runtime Download

A model process cannot download missing files automatically under a P0 or P1 profile.

---

# 76. Local Process Secrets

A local model process should not require provider secrets.

If it requires a local bearer token for API authentication:

- generate a random session or profile token;
- store it in Vault or process memory;
- bind to loopback;
- never log it;
- and rotate it when the process identity changes.

---

# 77. Local Model Logs and Cache

The profile records:

- prompt logging;
- response logging;
- cache directory;
- cache retention;
- crash dumps;
- and diagnostic export.

---

## 77.1 Opure-Managed Default

No full prompt or response logging.

---

## 77.2 Third-Party Local Server

Unknown logging posture lowers trust.

---

# 78. Model Licences

A local model profile records its licence where known.

---

## 78.1 Unknown Licence

Unknown licence blocks curated distribution but may permit local user-managed use with warning.

---

## 78.2 Use Restrictions

Model-use restrictions are not inferred by Opure.

The user remains responsible for compliance.

---

# 79. Network Gateway Request

Every remote request uses Network Gateway.

---

## 79.1 Exact Destination

The request is allowed only to the Provider Profile endpoint.

---

## 79.2 No Redirect

Inference requests do not follow redirects by default.

---

## 79.3 OAuth Redirect

OAuth and metadata flows are separate and follow their own validated redirect policy.

---

## 79.4 TLS

TLS certificate validation is mandatory.

---

## 79.5 Proxy

Enterprise proxy policy is supported.

Credentials remain mediated.

---

## 79.6 DNS

Validate destination addresses.

A remote profile cannot resolve to:

- loopback;
- private ranges;
- link-local;
- cloud metadata;
- multicast;
- or unspecified addresses

unless an explicit enterprise private-endpoint policy allows it.

---

# 80. Private Cloud Endpoints

Enterprise profiles may use private endpoints.

They require:

- administrator policy;
- exact DNS zone;
- exact address ranges;
- TLS identity;
- and no fallback to public endpoint.

---

# 81. Credential Injection

The AI Router creates a request without the raw credential.

---

## 81.1 Secret Use Handle

The request references a `ProviderCredentialUseRef`.

---

## 81.2 Final Boundary

Network Gateway retrieves or receives a short-lived secret lease and injects:

- header;
- request signature;
- mTLS identity;
- or cloud authentication

at the final boundary.

---

## 81.3 No Adapter Exposure

Where technically practical, provider adapter code does not receive the raw API key.

---

## 81.4 Cloud Signing

AWS-style signed requests may require trusted authentication code to access temporary credentials.

That code remains in the trusted Network Gateway or authentication adapter.

---

# 82. Request Construction

The AI Router constructs:

- deterministic system instructions;
- user instruction;
- approved context;
- model parameters;
- output schema;
- and feature flags.

---

## 82.1 Provider Adapter

The adapter maps the canonical request to provider wire format.

It may not add hidden context.

---

## 82.2 Provider Defaults

Safety-relevant provider defaults must be set explicitly where possible.

---

## 82.3 Unknown Defaults

Unknown defaults are recorded and may block strict use.

---

# 83. Request Parameters

The plan should include:

- temperature or equivalent;
- maximum output;
- seed where supported;
- stop sequences;
- structured-output schema;
- reasoning effort or equivalent visible control;
- and streaming.

---

## 83.1 No Hidden Reasoning Capture

Do not request or persist private chain-of-thought.

---

## 83.2 Reasoning Summaries

A provider's user-visible reasoning summary may be retained as output with provenance.

---

# 84. Structured Output

Structured output is preferred for machine-consumed results.

---

## 84.1 Local Schema

The schema is source controlled or generated by trusted code.

---

## 84.2 Provider Schema Support

Provider schema enforcement is not trusted alone.

Validate locally.

---

## 84.3 Remote `$ref`

Do not fetch remote schema references.

---

## 84.4 Failure

Invalid structured output is not executed or applied.

---

# 85. Streaming

Streaming is supported.

---

## 85.1 Bounds

Enforce:

- maximum bytes;
- maximum tokens;
- timeout;
- cancellation;
- and backpressure.

---

## 85.2 Partial Output

A cancelled or failed stream is marked partial.

---

## 85.3 Trust

Partial content remains untrusted.

---

## 85.4 Receipt

Record actual usage and termination reason.

---

# 86. Remote Tools and Grounding

Provider-side tools are disabled initially.

---

## 86.1 Examples

Includes:

- web search;
- remote MCP;
- code interpreter;
- computer use;
- browser use;
- file search;
- external retrieval;
- connectors;
- and provider-managed functions.

---

## 86.2 Why Disabled

They may send data to:

- another operator;
- another region;
- public websites;
- temporary execution environments;
- or persistent provider stores.

---

## 86.3 Future Gate

Each tool requires:

- operator chain;
- data-flow plan;
- retention;
- credentials;
- output validation;
- cost;
- approval;
- and incident response.

---

# 87. Function and Tool Calling

A model may produce a proposed Opure tool call using local structured output.

---

## 87.1 Proposal Only

The model does not execute the tool.

---

## 87.2 Deterministic Validation

Opure validates:

- tool identity;
- arguments;
- permissions;
- project;
- policy;
- and approval.

---

## 87.3 No Provider Tool Loop

Provider-native automatic tool loops are disabled.

---

# 88. Files API

Provider-side file upload is disabled.

---

## 88.1 Alternatives

Send approved content inline within request limits or use a local provider.

---

## 88.2 Future Gate

Requires:

- file lifecycle;
- retention;
- deletion;
- hash;
- region;
- ownership;
- and recovery.

---

# 89. Batch Processing

Provider batch jobs are disabled initially.

---

## 89.1 Risks

- delayed processing;
- provider state;
- file storage;
- global routing;
- cancellation uncertainty;
- and larger data sets.

---

# 90. Fine-Tuning

Fine-tuning is disabled.

---

## 90.1 Future Gate

Requires:

- training data approval;
- licences;
- personal data;
- retention;
- deletion;
- model ownership;
- model access;
- evaluation;
- and cost.

---

# 91. Explicit Prompt Caching

Remote explicit prompt or context caching is disabled.

---

## 91.1 Why

It creates provider-side state and deletion obligations.

---

## 91.2 Implicit Caching

If a provider performs implicit transient caching:

- record it;
- classify the posture;
- and require project acceptance when material.

---

# 92. Feedback Sharing

Provider feedback, thumbs-up/down sharing, log sharing and dataset contribution are off by default.

---

## 92.1 Explicit Feedback

A future feedback action must show:

- prompt;
- output;
- provider;
- account;
- project;
- data classes;
- and intended use.

---

## 92.2 No Automatic Quality Upload

Opure does not send failed outputs to providers automatically.

---

# 93. Moderation and Safety APIs

A separate remote moderation request is another data transmission.

---

## 93.1 Default

Do not send project content to a separate moderation provider unless the Provider Profile and plan include it.

---

## 93.2 Provider-Inline Safety

Synchronous safety processing performed as part of the named inference service is recorded in the Data Handling Record.

---

# 94. Retry Policy

Retries must not change provider, model, region or data posture.

---

## 94.1 Safe Retry

A limited retry may occur for:

- transient transport failure;
- rate limit with bounded delay;
- and server error

when no mutating provider-side state is involved.

---

## 94.2 No Retry

Do not retry automatically after:

- authentication failure;
- policy denial;
- secret detection;
- model not approved;
- retention mismatch;
- region mismatch;
- invalid certificate;
- invalid response schema;
- or budget exhaustion.

---

## 94.3 Retry Budget

Retries count against:

- request count;
- token estimate;
- cost budget;
- and time budget.

---

# 95. No Automatic Fallback

On failure, Opure offers choices.

It does not:

- switch to cloud;
- switch provider;
- switch model;
- switch region;
- or enable a stateful feature.

---

## 95.1 Suggested Alternative

The UI may suggest another approved Provider Profile.

A new plan and approval are required.

---

# 96. Rate Limits

AI Router enforces:

- per profile;
- per model;
- per project;
- per workflow;
- and global

rate and concurrency limits.

---

## 96.1 Provider Limits

Provider-reported limits are advisory.

---

## 96.2 Backpressure

Queueing is bounded.

---

# 97. Cost Policy

Each Provider Profile may define:

- currency;
- input-token price;
- output-token price;
- image price;
- audio price;
- request price;
- and effective date.

---

## 97.1 Pricing Volatility

Pricing metadata is time sensitive.

---

## 97.2 Estimate

A plan shows an estimate when enough metadata exists.

---

## 97.3 Actual Usage

Record provider-reported usage and estimated cost.

Provider billing remains authoritative.

---

## 97.4 Hard Budget

A hard budget prevents starting a request expected to exceed it.

Streaming stops at output limits where supported.

---

## 97.5 Retry

Automatic retry cannot exceed the remaining budget.

---

# 98. Quotas

Possible quotas:

- tokens per request;
- tokens per day;
- cost per request;
- cost per day;
- requests per minute;
- concurrent streams;
- images per request;
- and embedding bytes.

---

# 99. Output Trust

All model output is untrusted proposed content.

---

## 99.1 Text

May be displayed with provider provenance.

---

## 99.2 Code

Must be reviewed.

---

## 99.3 Patch

Must pass Patch Service and normal approval.

---

## 99.4 Command

Must pass Tool Mediator and approval.

---

## 99.5 Citation

Provider-generated citations are not assumed valid.

---

## 99.6 Security Advice

Output does not change product security policy.

---

# 100. Output Validation

Validate:

- encoding;
- size;
- schema;
- content type;
- forbidden control characters;
- and destination.

---

## 100.1 Active Content

Do not execute:

- HTML script;
- macros;
- shell commands;
- embedded binaries;
- or remote links.

---

## 100.2 Markdown

Render safely.

---

## 100.3 Images

Use safe decoders and bounded dimensions.

---

# 101. Output Provenance

Every output includes:

```text
provider_profile_revision
service_operator
model_publisher
requested_model
resolved_model
request_plan_hash
provider_response_id
created_at
finish_reason
usage
partial
validation
```

---

## 101.1 Provider Response ID

Store only if safe and useful.

It is not authentication.

---

# 102. Conversation and Workflow Storage

Opure decides whether to store output locally.

---

## 102.1 Default Interactive Chat

Store user-visible conversation according to local product settings.

---

## 102.2 Workflow

Store outputs required for deterministic workflow state.

---

## 102.3 Diagnostic Request

May store only a summary and receipt.

---

## 102.4 No Hidden Chain of Thought

Never store provider-hidden reasoning.

---

# 103. Request Receipt

Every remote request produces a receipt.

---

## 103.1 Fields

```text
operation_id
plan_hash
provider_profile_revision
model_profile
requested_model
resolved_model
endpoint_id
operator_chain
account_label
region
feature
project
context_item_hashes
data_classes
bytes_sent
token_estimate
actual_usage
retention_posture
stateful_flags
external_tools
policy
approval
credential_use_audit
started_at
completed_at
result
cost_estimate
actual_cost_estimate
```

---

## 103.2 No Full Content by Default

The receipt does not contain full prompt or output.

---

## 103.3 Local Request Receipt

Local operations may use a lighter receipt but retain model and context provenance.

---

# 104. Trust Centre

Trust Centre should show:

- Provider Profiles;
- active revision;
- endpoint;
- operator chain;
- account;
- model publisher;
- region;
- training posture;
- retention;
- state;
- caching;
- evidence freshness;
- project approvals;
- request plans;
- data classes sent;
- secret-scan result;
- usage;
- cost;
- failures;
- revocation;
- and quarantine.

---

## 104.1 Request Detail

Show:

- file counts;
- relative paths where permitted;
- context classes;
- model;
- provider;
- and approval.

Do not show secret values.

---

## 104.2 Provider Change

Show material profile changes and affected projects.

---

# 105. Diagnostic Logging

Logs may include:

- profile ID;
- revision;
- model ID;
- request audit ID;
- data classes;
- byte counts;
- token counts;
- duration;
- status;
- and provider error category.

---

## 105.1 Prohibited Logs

Do not log:

- API key;
- OAuth token;
- raw prompt;
- raw source;
- raw output;
- full provider headers;
- signed cloud request;
- or hidden reasoning.

---

## 105.2 Provider Errors

Redact provider error bodies before logs and UI.

---

# 106. Metrics

Low-cardinality local metrics may include:

- requests by profile class;
- local versus remote;
- success;
- latency;
- tokens;
- cost estimate;
- secret-scan denial;
- approval denial;
- provider error;
- and fallback suggestions.

Do not export:

- provider account;
- project name;
- model prompt;
- source path;
- or endpoint hostname

without policy.

---

# 107. Local Analytics

No hidden product analytics are enabled by AI usage.

---

# 108. Provider Disablement

Disabling a profile:

- stops new requests;
- cancels queued requests;
- cancels in-flight requests where safe;
- revokes credential-use handles;
- and leaves historical receipts.

---

# 109. Quarantine

Quarantine triggers may include:

- endpoint identity mismatch;
- TLS failure;
- model identity conflict;
- unexpected state storage;
- provider policy contradiction;
- credential leakage;
- unauthorised external tool;
- alias drift;
- response protocol violation;
- or security incident.

---

## 109.1 Effects

- deny new requests;
- cancel in-flight operations where safe;
- revoke secret-use handles;
- suspend project approvals;
- preserve evidence;
- and show remediation.

---

# 110. Credential Compromise

On suspected credential compromise:

1. disable profile;
2. revoke live secret leases;
3. rotate provider credential;
4. inspect provider usage;
5. inspect request receipts;
6. verify endpoint and account;
7. update profile revision;
8. and reapprove projects.

---

# 111. Provider Policy Change Incident

On material adverse policy change:

1. mark evidence contradicted or stale;
2. disable affected remote operations;
3. identify affected profiles and projects;
4. preserve prior policy evidence;
5. show the changed property;
6. offer local or approved alternatives;
7. and require a new profile revision.

---

# 112. Unexpected Data Retention

If a provider stores state contrary to profile:

- quarantine the profile;
- delete state where possible;
- rotate identifiers or credentials if needed;
- notify the user;
- preserve request receipts;
- and update the Data Handling Record.

---

# 113. Model Alias Incident

If a mutable alias changes unexpectedly:

- record old and new resolved model;
- pause reproducible workflows;
- run compatibility tests;
- update Model Profile;
- and require approval where policy demands.

---

# 114. Provider Outage

A remote provider outage:

- does not affect local project access;
- does not trigger automatic cloud fallback;
- preserves queued operation intent only when safe;
- and offers manual retry or alternative-plan creation.

---

# 115. Offline Behaviour

Local providers may remain available.

Remote profiles show Offline.

No remote check is required for ordinary local use.

---

# 116. Request Cancellation

Cancellation:

- stops local context streaming;
- cancels HTTP request;
- closes response stream;
- and records partial state.

A provider may still have processed received content.

The UI must state this limitation.

---

# 117. Deletion and Data Subject Requests

Provider-side deletion obligations depend on the profile and feature.

---

## 117.1 Stateless Request

Some providers do not offer ad hoc deletion of operational logs.

The Data Handling Record states this.

---

## 117.2 Stateful Feature

Disabled initially.

Future support must expose deletion operations and status.

---

# 118. Support Bundles

A support bundle may include:

- provider profile metadata;
- evidence state;
- request receipts;
- redacted errors;
- usage;
- and model identifiers.

It excludes:

- credentials;
- prompts;
- source;
- responses;
- and personal data

unless separately reviewed.

---

# 119. Privacy Impact

Remote inference sends approved data to the provider chain.

The provider receives ordinary network and account metadata in addition to content.

Local inference can still create local logs, caches and model telemetry.

The product must show the actual posture rather than equating “local” with “private” or “cloud” with one universal policy.

---

# 120. Reliability Impact

Central routing and policy add preflight work but prevent fragmented provider behaviour.

Local providers remain available during cloud outage.

Provider-specific adapters remain replaceable behind stable contracts.

---

# 121. Performance Impact

Costs include:

- context hashing;
- secret scanning;
- token estimation;
- policy evaluation;
- request adaptation;
- streaming validation;
- and receipts.

These are accepted for trust and inspectability.

---

# 122. Local Resource Impact

Local models consume:

- GPU memory;
- system RAM;
- CPU;
- disk;
- and power.

Performance modes and scheduling must prevent the AI Router from making the workstation unusable.

---

# 123. Security Threat Model

Relevant threats include:

- hidden cloud request;
- endpoint substitution;
- credential theft;
- TLS bypass;
- DNS rebinding;
- private-address confusion;
- malicious enterprise gateway;
- unknown proxy chain;
- provider policy change;
- unpaid-tier training;
- hidden human review;
- provider application-state storage;
- file upload persistence;
- explicit cache persistence;
- model alias drift;
- provider tool data sharing;
- secret exfiltration;
- whole-repository upload;
- hidden context expansion;
- build-log credential leakage;
- response prompt injection;
- malicious generated code;
- automatic fallback;
- cost exhaustion;
- and compromised local model server.

---

# 124. Security Controls

Controls include:

- local-only default;
- explicit Provider Profiles;
- immutable revisions;
- operator chain;
- Data Handling Records;
- evidence expiry;
- project cloud policy;
- exact context items;
- plan hashing;
- secret scanning;
- hard-denied files;
- byte and token budgets;
- Vault credentials;
- final-boundary injection;
- Network Gateway;
- exact destinations;
- no redirect;
- model snapshot pinning;
- remote state disabled;
- tools disabled;
- output validation;
- no direct execution;
- request receipts;
- budgets;
- revocation;
- and quarantine.

---

# 125. Security Limitations

This architecture cannot guarantee:

- provider policy compliance;
- absence of provider compromise;
- absence of legal retention;
- model correctness;
- non-uniqueness of output;
- absence of prompt injection;
- absence of side channels;
- protection against same-user malware;
- or safety after a developer explicitly approves harmful data sharing.

The product must state these limitations honestly.

---

# 126. Testing Strategy

ADR-0008 applies.

AI provider trust requires:

- unit tests;
- policy tests;
- provider-profile tests;
- evidence tests;
- network tests;
- credential tests;
- context-selection tests;
- secret-leakage tests;
- stateful-feature tests;
- local-provider tests;
- cloud-provider tests;
- cost tests;
- output-safety tests;
- recovery tests;
- fuzzing;
- and adversarial provider fixtures.

---

# 127. Provider Profile Tests

Test:

- valid local profile;
- valid direct-cloud profile;
- valid managed-cloud profile;
- valid enterprise gateway;
- custom compatible endpoint;
- missing operator;
- missing endpoint;
- missing region;
- missing data claims;
- unknown retention;
- unknown training;
- changed authentication;
- changed account;
- changed endpoint;
- changed operator;
- changed processing scope;
- and immutable revision enforcement.

---

# 128. Provider Chain Tests

Test:

- direct operator and publisher same;
- managed cloud with different model publisher;
- enterprise gateway with downstream provider;
- gateway with hidden routing;
- provider-side external tool;
- unknown chain;
- and changed downstream operator.

---

# 129. Evidence Tests

Test:

- current official evidence;
- verified contract;
- verified technical configuration;
- operator declaration;
- inferred claim;
- unknown claim;
- contradicted claim;
- expired evidence;
- missing evidence URL;
- evidence hash change;
- and scope mismatch.

---

## 129.1 Brand-Level Misapplication

Attempt to apply:

- consumer terms to API;
- direct-provider terms to managed cloud;
- paid terms to unpaid tier;
- ZDR documentation to an unapproved account;
- regional storage claim to global processing;
- and stateless inference claim to Files or Agents.

Every attempt must fail.

---

# 130. Data Posture Tests

Test each posture:

- P0;
- P1;
- P2;
- P3;
- P4;
- P5;
- PX;
- and PD.

Verify the most permissive material property determines the effective posture.

---

# 131. Project Cloud Policy Tests

Test:

- Local Only;
- Ask Every Time;
- Approved Providers Only;
- Custom;
- enterprise deny;
- evidence freshness requirement;
- maximum posture;
- region requirement;
- unpaid-tier prohibition;
- alias prohibition;
- feature prohibition;
- and hard budget.

---

# 132. Provider Registration Tests

Test:

- built-in adapter;
- custom endpoint;
- imported profile;
- exported profile;
- no secret export;
- synthetic verification request;
- model enumeration;
- hidden model;
- invalid TLS;
- private address;
- loopback remote profile;
- and unapproved redirect.

---

# 133. Model Profile Tests

Test:

- exact snapshot;
- mutable alias;
- alias resolution;
- alias target change;
- preview model;
- deprecated model;
- changed context limit;
- changed structured output;
- changed publisher;
- changed pricing;
- and changed data posture.

---

# 134. Data Classification Tests

Create representative content for:

- public;
- instruction;
- metadata;
- source;
- diff;
- build output;
- test output;
- memory;
- generated;
- diagnostics;
- image;
- audio;
- personal data;
- likely secret;
- confirmed secret;
- security protected;
- and prohibited.

Verify plan and policy behaviour.

---

# 135. Context Selection Tests

Test:

- explicit file;
- selected line range;
- diff;
- repository metadata;
- build error;
- test output;
- memory record;
- image;
- hidden file;
- ignored file;
- generated file;
- binary file;
- stale snapshot;
- changed file;
- deleted file;
- and another project.

---

# 136. No Hidden Context Tests

Instrument the provider wire request and prove it contains only:

- approved system instruction;
- approved user instruction;
- approved context items;
- approved schemas;
- and approved parameters.

No ambient:

- conversation history;
- project memory;
- current file;
- clipboard;
- environment;
- plugin output;
- MCP output;
- or provider state.

---

# 137. Plan Canonicalisation Tests

Test:

- context ordering;
- parameter ordering;
- Unicode normalisation;
- path display changes;
- equivalent JSON;
- model ID casing where relevant;
- region;
- feature flags;
- and plan-hash stability.

---

# 138. Approval Invalidation Tests

Change:

- file content;
- selected range;
- memory item;
- provider;
- model;
- alias target;
- region;
- feature;
- `store` policy;
- cache policy;
- external tool;
- output destination;
- cost;
- and token budget.

Every material change invalidates approval.

---

# 139. Secret Scanner Tests

Use canaries for:

- API key;
- OAuth token;
- refresh token;
- private key;
- PFX;
- password;
- connection string;
- database URI;
- signed URL;
- session cookie;
- Git credential;
- AWS credential;
- Azure secret;
- Google service-account key;
- SSH key;
- and recovery code.

Verify no canary reaches the wire.

---

# 140. Protected Root Tests

Attempt to include:

- Vault directory;
- signing-key directory;
- browser credential store;
- SSH directory;
- cloud credential directory;
- package-signing configuration;
- Opure recovery keys;
- and security incident data.

Every attempt hard denies.

---

# 141. Redaction Tests

Test:

- one secret in source;
- multiple secrets;
- secret in JSON;
- secret in URL;
- secret split across lines;
- secret in build output;
- secret in image metadata;
- and false positive.

Verify values do not appear in:

- preview;
- logs;
- receipts;
- provider request;
- or error.

---

# 142. Personal Data Tests

Test:

- name;
- email;
- phone number;
- IP address;
- customer identifier;
- and employee record.

Verify minimisation and approval.

---

# 143. Context Budget Tests

Test:

- file count limit;
- total bytes;
- individual file;
- build output;
- memory count;
- token estimate;
- model context limit;
- image count;
- image size;
- and expanded approval.

---

# 144. Whole-Repository Tests

Attempt:

- root directory selection;
- wildcard all;
- model-requested recursive expansion;
- plugin-requested expansion;
- workflow expansion;
- and hidden-file inclusion.

No automatic whole-repository transmission succeeds.

---

# 145. Network Tests

Test:

- approved endpoint;
- wrong host;
- wrong port;
- HTTP;
- invalid TLS;
- expired certificate;
- hostname mismatch;
- redirect;
- private IPv4;
- private IPv6;
- loopback;
- link-local;
- metadata endpoint;
- DNS rebinding;
- proxy;
- enterprise private endpoint;
- cancellation;
- and timeout.

---

# 146. Credential Tests

Test:

- API key;
- OAuth;
- managed identity;
- STS temporary credential;
- cloud-signed request;
- local bearer;
- mTLS;
- credential rotation;
- wrong endpoint;
- wrong account;
- wrong region;
- and expired credential.

---

## 146.1 Leakage Tests

Seed credentials into:

- Vault;
- adapter memory;
- environment;
- error;
- provider response;
- logs;
- traces;
- Trust Centre;
- support bundle;
- crash metadata;
- and model output.

Verify no raw credential is retained or exposed.

---

# 147. Final-Boundary Injection Tests

Prove the raw credential is absent from:

- Desktop;
- AI Router canonical request;
- Workspace;
- Memory;
- workflow state;
- plugin process;
- MCP server;
- and ordinary adapter logs.

---

# 148. OpenAI Adapter Tests

Test:

- default retention profile;
- approved ZDR profile;
- false ZDR claim;
- `store=false` wire value;
- Responses stateless use;
- conversation ID rejection;
- Files rejection;
- vector-store rejection;
- background-mode rejection;
- Code Interpreter rejection;
- web-search rejection;
- remote-MCP rejection;
- model eligibility;
- and regional endpoint policy.

---

# 149. Anthropic Adapter Tests

Test:

- commercial API;
- default retention;
- ZDR agreement;
- no ad hoc deletion representation;
- file feature rejection;
- prompt-cache policy;
- key handling;
- and consumer endpoint rejection.

---

# 150. Gemini Adapter Tests

Test:

- paid profile;
- unpaid profile;
- UK terms scope;
- billing-state change;
- `store=false` where applicable;
- Interactions state rejection;
- Files rejection;
- explicit cache rejection;
- Live session-resumption rejection;
- feedback sharing off;
- dataset sharing off;
- implicit-cache disclosure;
- and grounding rejection.

---

# 151. Microsoft Foundry Tests

Test:

- regional deployment;
- data-zone deployment;
- global deployment;
- storage versus processing display;
- model publisher separation;
- managed identity;
- API key fallback;
- stateful feature rejection;
- abuse-monitoring posture;
- marketplace metadata sharing;
- and resource-region confusion.

---

# 152. Bedrock Tests

Test:

- retention `none`;
- retention `default`;
- `provider_data_share`;
- incompatible model under `none`;
- region;
- cross-region inference;
- model publisher;
- temporary credentials;
- static key;
- Knowledge Base rejection;
- Agent rejection;
- Prompt Management rejection;
- and Responses storage controls.

---

# 153. Enterprise Gateway Tests

Test:

- known downstream provider;
- dynamic routing;
- unknown routing;
- multiple regions;
- hidden model alias;
- enterprise retention;
- gateway logs;
- credential;
- and policy change.

---

# 154. Custom Compatible Endpoint Tests

Test:

- OpenAI wire compatibility;
- ignored `store=false`;
- unknown operator;
- self-signed TLS;
- invalid model list;
- proxy to remote provider;
- loopback server;
- LAN endpoint;
- full request logging;
- and misleading “local” label.

---

# 155. Local Provider Tests

Test:

- embedded local;
- managed process;
- verified loopback;
- unverified loopback;
- network isolation;
- local prompt logs;
- local response logs;
- model cache;
- telemetry;
- model hash;
- model-file change;
- model licence;
- process crash;
- GPU exhaustion;
- and process shutdown.

---

# 156. No Automatic Model Download Test

During inference with a missing model, prove the process cannot download it automatically.

---

# 157. Remote Feature Rejection Tests

Test rejection of:

- conversations;
- threads;
- assistants;
- files;
- vector stores;
- explicit caches;
- prompt libraries;
- agents;
- knowledge bases;
- batch;
- fine-tuning;
- evaluations;
- background mode;
- web search;
- remote MCP;
- computer use;
- code interpreter;
- browser automation;
- and long-running tasks.

---

# 158. Stateless Control Tests

Test:

- supported `store=false`;
- unsupported storage control;
- provider ignores control;
- application state returned;
- conversation ID returned;
- file ID returned;
- and unexpected remote object creation.

---

# 159. Feedback Tests

Test:

- feedback disabled;
- dataset sharing disabled;
- accidental thumbs-up API call;
- automatic error upload;
- and explicit future feedback preview.

---

# 160. Retry Tests

Test:

- transient timeout;
- rate limit;
- server error;
- authentication error;
- policy error;
- region error;
- retention mismatch;
- model error;
- invalid response;
- budget exhausted;
- and cancellation.

Verify no provider or model fallback.

---

# 161. Fallback Tests

Attempt automatic:

- local to cloud;
- cloud A to cloud B;
- regional to global;
- exact model to alias;
- stateless to stateful;
- and paid to unpaid.

Every attempt requires a new plan and approval.

---

# 162. Streaming Tests

Test:

- valid stream;
- partial stream;
- cancellation;
- connection loss;
- oversized stream;
- token limit;
- malformed frame;
- invalid UTF-8;
- late provider error;
- and receipt completion.

---

# 163. Structured Output Tests

Test:

- valid output;
- invalid schema;
- extra field;
- missing field;
- remote `$ref`;
- large schema;
- recursive schema;
- regex abuse;
- truncated JSON;
- and malicious command content.

---

# 164. Output Safety Tests

Use output containing:

- shell command;
- PowerShell;
- patch;
- private key;
- HTML script;
- SVG script;
- executable base64;
- malicious Markdown link;
- prompt injection;
- fake citation;
- and repository deletion instruction.

Verify no automatic execution or application.

---

# 165. Patch Integration Tests

Test:

- valid model patch proposal;
- stale base;
- path escape;
- secret introduction;
- binary modification;
- file deletion;
- large patch;
- and model request to bypass review.

Only Patch Service may apply.

---

# 166. Tool Integration Tests

Test:

- model-proposed tool call;
- invalid tool;
- invalid arguments;
- missing permission;
- approval required;
- user denial;
- and no provider-native tool loop.

---

# 167. Embedding Tests

Test:

- local embedding;
- remote embedding;
- data-sharing preview;
- secret detection;
- dimensions;
- model change;
- vector invalidation;
- provider-side vector-store rejection;
- and semantic-leakage warning.

---

# 168. Cost Tests

Test:

- current price;
- stale price;
- unknown price;
- input estimate;
- output estimate;
- image price;
- actual usage;
- hard per-request budget;
- daily budget;
- retry budget;
- currency;
- and provider-billing mismatch.

---

# 169. Receipt Tests

Verify receipt contains:

- plan hash;
- provider revision;
- operator chain;
- model;
- region;
- data classes;
- context hashes;
- usage;
- approval;
- cost;
- and result.

Verify it excludes:

- full prompt;
- full response;
- secret;
- credential;
- and hidden reasoning.

---

# 170. Revocation Tests

Test:

- user disables profile;
- enterprise denial;
- credential revoke;
- evidence expiry;
- provider-policy change;
- endpoint mismatch;
- alias change;
- model withdrawal;
- project close;
- and security incident.

---

# 171. Quarantine Tests

Simulate:

- TLS substitution;
- unexpected external tool;
- ignored state control;
- credential leakage;
- model identity conflict;
- provider response protocol violation;
- and contradictory policy evidence.

---

# 172. Recovery Tests

Test:

- Runtime restart;
- stream interruption;
- provider outage;
- local model crash;
- stale receipt;
- expired profile;
- rotated credential;
- changed model alias;
- and partial workflow output.

---

# 173. Offline Tests

Disconnect the network and verify:

- projects open;
- source is available;
- local model operates;
- remote profiles show Offline;
- no hidden retry loop;
- no cloud fallback;
- and Trust Centre remains usable.

---

# 174. Fuzzing

Fuzz:

- Provider Profile JSON;
- Data Handling Record;
- Data Sharing Plan;
- provider adapter responses;
- streaming frames;
- usage metadata;
- structured-output schema;
- model identifiers;
- endpoint parsing;
- and provider error bodies.

---

# 175. Performance Tests

Measure:

- profile lookup;
- policy evaluation;
- context hashing;
- secret scanning;
- token estimation;
- plan generation;
- approval rendering;
- credential injection;
- request adaptation;
- streaming validation;
- receipt write;
- local model startup;
- and cancellation.

---

# 176. Provisional Performance Targets

On reference hardware:

- cached profile and policy evaluation: under 3 ms p95;
- plan creation excluding secret scan and tokenisation: under 10 ms p95;
- secret scan for 1 MiB text: under 250 ms p95;
- request adaptation: under 5 ms p95;
- streaming overhead excluding provider latency: under 10 ms p95 per chunked response window;
- cancellation dispatch: under 100 ms;
- local managed model readiness: model dependent and separately budgeted;
- and remote-plan revocation for new calls: under 250 ms.

These are targets pending evidence.

---

# 177. Accessibility Tests

Provider and request UI must support:

- keyboard;
- Narrator;
- high contrast;
- reduced motion;
- focus management;
- provider evidence;
- file-list expansion;
- data-class explanation;
- cost display;
- and approval.

Provider risk is not communicated by colour alone.

---

# 178. Prototype Plan

## 178.1 Prototype A — Local Managed Provider

Launch a local model process under Process Supervisor with no network and verified model hashes.

---

## 178.2 Prototype B — Custom Loopback

Register an unmanaged OpenAI-compatible loopback endpoint and prove that compatibility does not raise its trust class.

---

## 178.3 Prototype C — Direct Cloud

Connect to one direct API with `store=false`, Vault key use and a full Data Sharing Plan.

---

## 178.4 Prototype D — Managed Cloud

Connect to one managed cloud deployment and display operator, publisher, region and retention separately.

---

## 178.5 Prototype E — Secret Leakage

Seed canary secrets through every context source and adapter error path.

---

## 178.6 Prototype F — Alias Drift

Change a mutable alias target and pause a reproducible workflow.

---

## 178.7 Prototype G — State Rejection

Attempt provider files, conversations, cache and background features.

---

## 178.8 Prototype H — Paid and Unpaid Tier

Demonstrate different data posture for two billing or tier configurations.

---

## 178.9 Prototype I — No Fallback

Fail a local request and prove no cloud traffic occurs.

---

## 178.10 Prototype J — Output Review

Generate a patch and command proposal and pass both through normal review boundaries.

---

## 178.11 Prototype K — Provider Policy Change

Change a retention claim and invalidate project approval.

---

## 178.12 Prototype L — Incident

Quarantine a provider, revoke credentials and preserve receipts.

---

# 179. Implementation Plan

1. Record founder review.
2. Define Provider Profile schema.
3. Define Data Handling Record schema.
4. Define data-posture levels.
5. Define Model Profile schema.
6. Define AI data classes.
7. Define Data Sharing Plan schema.
8. Implement profile revisioning.
9. Implement evidence status and expiry.
10. Implement project cloud-policy evaluator.
11. Implement context-item references.
12. Implement context hashing.
13. Implement token estimation.
14. Implement secret scanning.
15. Implement protected-root policy.
16. Implement plan canonicalisation.
17. Implement plan approval.
18. Integrate Secrets Vault credential-use handles.
19. Integrate Network Gateway destination policy.
20. Implement local managed-provider adapter.
21. Implement custom compatible adapter.
22. Implement one direct-cloud adapter.
23. Implement one managed-cloud adapter.
24. Implement model snapshot and alias policy.
25. Implement stateless feature gate.
26. Implement `store=false` or equivalent mappings.
27. Reject provider-side stateful features.
28. Implement streaming validation.
29. Implement structured-output validation.
30. Implement request receipts.
31. Implement cost and token budgets.
32. Implement output provenance.
33. Integrate Patch and Tool proposal boundaries.
34. Implement Trust Centre views.
35. Add provider-policy evidence templates.
36. Add malicious and non-compliant provider fixtures.
37. Run leakage and fallback tests.
38. Complete security and privacy review.
39. Accept, amend or reject the ADR.

---

# 180. Owners

| Area | Owner |
|---|---|
| Product policy | Founder |
| Provider Profiles | AI Router Owner |
| Data Handling Records | Provider Trust and Security Owners |
| Project Cloud Policy | Project Policy Owner |
| Context selection | Workspace and Memory Owners |
| Secret scanning | Security and Secrets Owners |
| Credentials | Secrets Owner |
| Remote network | Network Gateway Owner |
| Local model processes | Process Supervisor and AI Router Owners |
| Cost and quotas | AI Router Owner |
| Output validation | AI Router and Patch Owners |
| Trust Centre | Trust Centre Owner |
| Desktop consent | Desktop Owner |
| Persistence | Persistence Owner |
| Adversarial tests | Test Architecture Owner |

---

# 181. Suggested Repository Structure

```text
src/
├── AI/
│   ├── Opure.AI.Contracts/
│   ├── Opure.AI.Router/
│   ├── Opure.AI.ProviderProfiles/
│   ├── Opure.AI.ProviderTrust/
│   ├── Opure.AI.Context/
│   ├── Opure.AI.DataSharing/
│   ├── Opure.AI.Security/
│   ├── Opure.AI.Costs/
│   └── Opure.AI.Providers/
├── Platform/
│   └── Opure.Platform.LocalModels.Windows/
└── Desktop/
    └── Opure.Desktop.AI/

schemas/
└── ai/
    ├── provider-profile-v1.schema.json
    ├── data-handling-record-v1.schema.json
    ├── model-profile-v1.schema.json
    ├── data-sharing-plan-v1.schema.json
    └── request-receipt-v1.schema.json

tests/
└── AI/
    ├── Opure.AI.UnitTests/
    ├── Opure.AI.ProviderTests/
    ├── Opure.AI.SecurityTests/
    ├── Opure.AI.PrivacyTests/
    └── Fixtures/
        └── MaliciousProviders/
```

Exact projects may be consolidated under ADR-0010.

---

# 182. Provider Profile Sketch

```json
{
  "schema": "opure.ai-provider-profile/1",
  "id": "provider-profile-opaque",
  "revision": 3,
  "display_name": "Example EU Stateless API",
  "class": "direct-cloud-api",
  "operator": {
    "name": "Example AI Ltd"
  },
  "endpoint": {
    "scheme": "https",
    "host": "eu.api.example.com",
    "port": 443,
    "base_path": "/v1"
  },
  "account": {
    "label": "Opure production project"
  },
  "processing": {
    "scope": "europe"
  },
  "authentication": {
    "kind": "api-key",
    "credential_ref": "vault-ref-opaque"
  },
  "data_posture": "P3",
  "reviewed_at": "2026-07-18T00:00:00Z",
  "expires_at": "2026-10-16T00:00:00Z"
}
```

---

# 183. Data Handling Record Sketch

```json
{
  "schema": "opure.ai-data-handling/1",
  "training_use": {
    "value": "not-used-unless-opt-in",
    "status": "verified-current-official-documentation"
  },
  "abuse_monitoring": {
    "value": "content-may-be-retained",
    "maximum_days": 30,
    "status": "verified-current-official-documentation"
  },
  "application_state": {
    "value": "disabled-by-profile"
  },
  "processing_location": {
    "value": "europe",
    "status": "verified-technical-configuration"
  },
  "evidence": [
    {
      "kind": "official-documentation",
      "reviewed_at": "2026-07-18T00:00:00Z",
      "reference": "provider-policy-reference"
    }
  ]
}
```

---

# 184. Data Sharing Plan Sketch

```json
{
  "schema": "opure.ai-data-sharing-plan/1",
  "provider_profile": {
    "id": "provider-profile-opaque",
    "revision": 3
  },
  "model": {
    "profile": "model-profile-opaque",
    "requested": "model-snapshot-id"
  },
  "project": "project-ref-opaque",
  "purpose": "Review selected repository changes",
  "context": [
    {
      "type": "file-snapshot",
      "label": "src/Example.cs",
      "sha256": "...",
      "bytes": 8291,
      "class": "project.source"
    },
    {
      "type": "diff",
      "label": "Current proposed change",
      "sha256": "...",
      "bytes": 2140,
      "class": "project.diff"
    }
  ],
  "controls": {
    "provider_state": false,
    "explicit_cache": false,
    "external_tools": false
  },
  "budget": {
    "input_tokens_estimated": 4200,
    "output_tokens_max": 1500
  }
}
```

---

# 185. Request Receipt Sketch

```json
{
  "schema": "opure.ai-request-receipt/1",
  "operation_id": "operation-opaque",
  "plan_sha256": "...",
  "provider_profile_revision": "provider-profile-opaque:3",
  "requested_model": "model-snapshot-id",
  "resolved_model": "model-snapshot-id",
  "data_classes": ["project.source", "project.diff"],
  "context_hashes": ["...", "..."],
  "bytes_sent": 10431,
  "usage": {
    "input_tokens": 3980,
    "output_tokens": 811
  },
  "result": "completed",
  "partial": false
}
```

---

# 186. Provider Settings UI Copy

Suggested text:

> A Provider Profile records where Opure sends data, who operates the service, which model is used, where processing may occur, what the provider says it retains and whether the configured account has additional controls. API compatibility alone does not establish these facts.

---

# 187. Request Approval UI Copy

Suggested text:

> Opure is ready to send the listed context to the displayed provider and model. Review the selected files, data classes, retention posture, processing location and estimated cost. The provider may still retain data for safety, reliability or legal reasons according to the displayed profile.

---

# 188. Local Provider UI Copy

Suggested text:

> This model runs through a service on your computer. Local does not automatically mean offline or private: the service may log prompts, use caches or make network requests. Opure displays the verified process, model files and network policy when those details are known.

---

# 189. Custom Endpoint UI Copy

Suggested text:

> This endpoint uses a compatible API format, but Opure has not verified that it is operated by the named model provider or that it follows the same retention, training, storage or security practices. Treat compatibility and trust as separate decisions.

---

# 190. Release Gate

Remote AI support is blocked when:

- Local Only makes a remote request;
- a provider has no explicit profile;
- operator identity is unknown under strict policy;
- endpoint can change without revision;
- credential reaches normal application code or logs;
- request context can expand after approval;
- a detected secret reaches the wire;
- a protected file can be selected;
- a provider profile relies on brand-level terms;
- paid and unpaid tiers are indistinguishable;
- ZDR is claimed without account evidence;
- processing and storage location are conflated;
- a mutable alias appears reproducible;
- provider-side state is created;
- remote tools are enabled;
- fallback changes provider or region;
- whole-repository transmission is possible without explicit bounded review;
- output can apply code or commands directly;
- a request receipt cannot explain what was sent;
- or revocation does not stop new requests.

---

# 191. Acceptance Criteria

This ADR may move to **Accepted** when all of the following hold.

## Provider Profiles

- [ ] Every provider has an explicit Provider Profile.
- [ ] Profiles are immutable and revisioned.
- [ ] Endpoint, operator and account are explicit.
- [ ] Service operator and model publisher are separate fields.
- [ ] Direct, managed, enterprise, local and custom classes are distinct.
- [ ] OpenAI-compatible is never a trust class.
- [ ] Authentication is bound to one profile revision.
- [ ] Multiple accounts are distinguishable.
- [ ] Historical revisions remain available.
- [ ] Material changes create a new revision.
- [ ] Profile export excludes credentials.
- [ ] Profile import returns to review.

## Data Handling Records

- [ ] Every remote profile has a Data Handling Record.
- [ ] Training use is recorded.
- [ ] Product-improvement use is recorded.
- [ ] Human review is recorded.
- [ ] Abuse-monitoring retention is recorded.
- [ ] Application-state retention is recorded.
- [ ] Deletion control is recorded.
- [ ] Processing and storage locations are separate.
- [ ] Model-publisher access is recorded.
- [ ] External tools are recorded.
- [ ] Evidence status and scope are recorded.
- [ ] Evidence review and expiry dates exist.
- [ ] Brand-level assumptions are rejected.
- [ ] Stale and contradictory evidence are visible.
- [ ] Strict policy blocks unknown material claims.

## Models

- [ ] Every enabled model has a Model Profile.
- [ ] Exact snapshots are preferred.
- [ ] Mutable aliases are labelled.
- [ ] Resolved model is recorded where available.
- [ ] Alias drift is detected.
- [ ] Reproducible workflows reject silent drift.
- [ ] Preview models require explicit approval.
- [ ] Model capability does not enable tools.
- [ ] Model changes do not broaden data policy.

## Project Policy

- [ ] Local Only is the default.
- [ ] Local Only sends no remote request.
- [ ] Ask Every Time requires human approval.
- [ ] Approved Providers Only uses exact revisions.
- [ ] Custom policy can constrain data, region, posture and cost.
- [ ] Deny wins.
- [ ] Enterprise policy cannot be bypassed.
- [ ] Project policy cannot broaden Provider Profile capabilities.
- [ ] Provider approvals are visible and revocable.

## Context and Plans

- [ ] Every context item is explicit.
- [ ] File context is snapshot and hash bound.
- [ ] Memory context is explicit.
- [ ] No ambient workspace context exists.
- [ ] No hidden conversation context exists.
- [ ] No context expands after approval.
- [ ] Data Sharing Plan is canonical.
- [ ] Plan has a stable hash.
- [ ] Plan identifies operator, model, region and posture.
- [ ] Plan identifies every data class.
- [ ] Plan identifies file and byte counts.
- [ ] Plan identifies state, cache and external tools.
- [ ] Approval binds exact plan hash.
- [ ] Material changes invalidate approval.
- [ ] Whole-repository upload is unavailable.
- [ ] Context budgets are enforced.

## Secrets and Sensitive Data

- [ ] User instructions are secret scanned.
- [ ] Source files are secret scanned.
- [ ] Diffs are secret scanned.
- [ ] Build and test outputs are secret scanned.
- [ ] Memory is secret scanned.
- [ ] Confirmed secrets never reach providers.
- [ ] Likely secrets are denied remotely.
- [ ] Protected roots are hard denied.
- [ ] Secret values do not appear in preview.
- [ ] Secret values do not appear in logs.
- [ ] Secret values do not appear in receipts.
- [ ] No generic send-secret override exists.
- [ ] Personal data is classified and minimised.
- [ ] Image metadata is stripped where appropriate.

## Credentials and Network

- [ ] Credentials remain in Vault.
- [ ] Final-boundary injection is implemented.
- [ ] Desktop never receives a provider credential.
- [ ] Plugins never receive provider credentials.
- [ ] MCP servers never receive provider credentials.
- [ ] Every remote request uses Network Gateway.
- [ ] TLS validation is mandatory.
- [ ] Inference redirects are disabled.
- [ ] Exact destinations are enforced.
- [ ] Remote profiles cannot resolve to private or loopback addresses without enterprise policy.
- [ ] Local loopback profiles are separately classified.
- [ ] Temporary credentials are preferred where supported.
- [ ] Credential rotation and revoke are tested.

## Remote Feature Policy

- [ ] Stateless inference works.
- [ ] Streaming works.
- [ ] Stateless embeddings work.
- [ ] `store=false` or equivalent is sent where supported.
- [ ] Unsupported stateless posture denies under strict policy.
- [ ] Provider conversations are disabled.
- [ ] Provider files are disabled.
- [ ] Provider vector stores are disabled.
- [ ] Provider assistants and agents are disabled.
- [ ] Provider batches are disabled.
- [ ] Provider fine-tuning is disabled.
- [ ] Explicit remote caching is disabled.
- [ ] Background mode is disabled.
- [ ] Web search is disabled.
- [ ] Remote MCP is disabled.
- [ ] Computer use is disabled.
- [ ] Code interpreter is disabled.
- [ ] Feedback and dataset sharing are off.
- [ ] Paid and unpaid provider tiers remain distinct.

## Local Providers

- [ ] Managed local process is supervised.
- [ ] Local executable and model files are identified.
- [ ] Model hashes are recorded where available.
- [ ] Local model licence is recorded where known.
- [ ] Managed local process network is denied where practical.
- [ ] Model download is a separate operation.
- [ ] No automatic model download occurs during inference.
- [ ] Local logs and cache posture are visible.
- [ ] Unmanaged loopback endpoints are not assumed private.
- [ ] LAN discovery is absent.
- [ ] Local and remote profiles cannot be confused.

## Request Execution

- [ ] Provider adapter adds no hidden context.
- [ ] Safety-relevant defaults are explicit.
- [ ] Streaming is bounded.
- [ ] Cancellation works.
- [ ] Retries do not change provider, model, region or posture.
- [ ] Authentication and policy failures are not retried automatically.
- [ ] No automatic fallback occurs.
- [ ] Cost and token budgets are enforced.
- [ ] Retries count against budgets.
- [ ] Actual usage is recorded where available.
- [ ] Provider outage does not impair local work.

## Output

- [ ] Every output has provenance.
- [ ] Output is untrusted.
- [ ] Structured output is validated locally.
- [ ] Active content is not executed.
- [ ] Code is not applied directly.
- [ ] Patch uses Patch Service.
- [ ] Commands use Tool Mediator.
- [ ] Citations are not assumed valid.
- [ ] Hidden chain-of-thought is not requested or stored.
- [ ] Partial outputs are labelled.
- [ ] Output-to-memory requires separate policy.

## Evidence, Audit and Recovery

- [ ] Every remote request creates a receipt.
- [ ] Receipts contain plan hash and context hashes.
- [ ] Receipts contain profile and model revisions.
- [ ] Receipts contain region and data classes.
- [ ] Receipts exclude full content by default.
- [ ] Trust Centre displays provider posture.
- [ ] Trust Centre displays request data classes.
- [ ] Profile disablement stops new requests.
- [ ] Quarantine stops new requests and revokes credential use.
- [ ] Policy-change incident flow works.
- [ ] Credential compromise flow works.
- [ ] Alias-drift flow works.
- [ ] Offline operation works.
- [ ] Security and privacy review is complete.
- [ ] Founder approval is recorded.

---

# 192. Evidence Required Before Acceptance

- [ ] Provider Profile schema.
- [ ] Data Handling Record schema.
- [ ] Model Profile schema.
- [ ] Data Sharing Plan schema.
- [ ] Request Receipt schema.
- [ ] Provider-chain demonstration.
- [ ] Evidence scope and expiry report.
- [ ] Brand-level misapplication tests.
- [ ] Project cloud-policy report.
- [ ] Context inventory screenshots.
- [ ] Secret scanner canary report.
- [ ] Protected-root report.
- [ ] Whole-repository denial report.
- [ ] Plan canonicalisation report.
- [ ] Approval invalidation report.
- [ ] Vault credential-use report.
- [ ] Network destination report.
- [ ] No-redirect report.
- [ ] Direct-cloud adapter report.
- [ ] Managed-cloud adapter report.
- [ ] Local managed-provider report.
- [ ] Unmanaged loopback report.
- [ ] Model alias-drift report.
- [ ] Stateless-control wire report.
- [ ] Stateful-feature rejection report.
- [ ] Paid-versus-unpaid posture report.
- [ ] No-feedback-sharing report.
- [ ] No-fallback network capture.
- [ ] Cost-budget report.
- [ ] Output-safety report.
- [ ] Patch and command review report.
- [ ] Request receipt examples.
- [ ] Profile quarantine rehearsal.
- [ ] Credential compromise rehearsal.
- [ ] Provider-policy change rehearsal.
- [ ] Offline acceptance report.
- [ ] Performance report.
- [ ] Accessibility report.
- [ ] Security review.
- [ ] Privacy review.
- [ ] Founder approval.

---

# 193. Known Limitations

- The final provider adapters are not selected.
- The final supported provider list is not selected.
- Provider terms and documentation change.
- Evidence expiry creates ongoing maintenance.
- Technical verification cannot prove every contractual claim.
- Some providers do not expose retention configuration through an API.
- Some model aliases do not reveal a concrete snapshot.
- A provider may change model behaviour without changing an identifier.
- Token estimates differ from provider billing.
- Cost estimates can be stale.
- Secret detection has false positives and false negatives.
- Version 1 has no broad secret-transmission override.
- Whole-repository upload is unavailable.
- Provider-side files and agents are unavailable.
- Provider web search and remote MCP are unavailable.
- Fine-tuning and batch are unavailable.
- Explicit remote prompt caching is unavailable.
- Automatic cloud fallback is unavailable.
- Managed local processes may require native components.
- Not every local server can be network isolated completely.
- Same-user malware remains a risk.
- A local endpoint may secretly proxy remotely.
- Provider responses may remain inaccurate or malicious.
- Model outputs may not be unique.
- Cancellation cannot retract content already received by a provider.
- Stateless inference can still involve abuse or legal retention.
- No universal provider posture score can replace project judgement.
- Enterprise contracts are outside Opure's verification authority.

---

# 194. Open Questions

- Which providers belong in the first-party adapter set?
- Should Version 1 support only local plus one cloud provider?
- Which direct-cloud provider should be the first vertical slice?
- Which managed-cloud provider should be the first vertical slice?
- Which provider SDKs are acceptable?
- Should adapters use raw HTTP for greater inspection?
- How should provider adapter packages be signed and updated?
- What exact evidence-review period is appropriate?
- Should evidence templates ship with Opure releases?
- Should Opure offer a provider-policy notification service?
- How should offline copies of provider terms be retained?
- What contract evidence can enterprise users attach?
- How should evidence hashes be calculated?
- Which data-handling claims are mandatory for remote use?
- Should P3 be the maximum default for source code?
- Should personal data require Ask Every Time regardless of project policy?
- Should public data be permitted under PX?
- How are regional legal requirements represented?
- How should data-controller and processor roles be displayed?
- Should subprocessor lists be tracked?
- How should provider policy translations be reviewed?
- Which exact data classes belong in Version 1?
- Which secret scanner is authoritative?
- How should secret canary fingerprints work without exposing values?
- Should likely-secret redaction ever permit remote use?
- Which file extensions are hard denied?
- How are notebooks and generated artefacts classified?
- What maximum context budgets suit common coding workflows?
- Should an explicit large-context approval exist?
- How should images be scanned for secrets?
- Should OCR be used for image secret detection?
- When should remote audio be supported?
- How is source-code licence or copyright policy displayed?
- Should project policy forbid code under specific licences from remote sharing?
- How are Git submodules and generated dependencies classified?
- Should dependency source be included in plans?
- How should prompt templates be versioned?
- Which tokenisers are used for estimation?
- Should provider token-count APIs be called before inference?
- How is mutable alias resolution detected reliably?
- What happens when a provider does not return the resolved model?
- Which workflows require exact snapshots?
- How should model deprecation migration work?
- Should quality benchmarks be tied to model profiles?
- Should safety settings be part of the model profile?
- Which reasoning controls are acceptable?
- How should provider-visible reasoning summaries be stored?
- Should streaming chunks be content scanned incrementally?
- Which structured-output library is selected?
- How are provider-specific schema limitations represented?
- Should embeddings ever use a different provider from generation?
- Which embedding data classes may be remote?
- How are old vectors deleted after provider change?
- Which local model formats are supported?
- How are model files downloaded and verified?
- Should model download use a TUF-style repository?
- Which model licences are acceptable for bundled use?
- Can local model processes run in AppContainer or another sandbox?
- Which GPU runtime dependencies are required?
- How should local model telemetry be blocked?
- How should unmanaged local server process identity be discovered safely?
- Should LAN enterprise inference be supported?
- How are private endpoints and split DNS configured?
- Which managed identity flows work in a desktop application?
- Should a local broker exchange user credentials for short-lived cloud tokens?
- How are AWS STS and Azure identity flows surfaced?
- Should Google service-account keys be prohibited in favour of workload identity?
- How is mTLS configured?
- Which provider errors may be shown to users?
- Should provider response IDs be retained?
- How long should request receipts be retained?
- Should users be able to export full prompt and response with a receipt?
- How are remote deletion requests represented?
- Should provider account usage be reconciled with local receipts?
- How should hard budgets handle provider-side rounding?
- Should price changes block use under strict policy?
- What retry count and backoff are appropriate?
- Should streaming cost be displayed live?
- Which provider tool should be approved first after Version 1?
- Should provider web search ever coexist with Local Only?
- How should provider remote MCP interact with ADR-0018?
- Should remote code interpreters ever receive project files?
- How should batch and fine-tuning governance work?
- Should a provider-side conversation ever become an Opure feature?
- How should enterprise gateways disclose dynamic routing?
- Should an enterprise-signed Provider Profile bypass ordinary review?
- How does Stable share Provider Profiles with Preview?
- Can provider credentials be imported across profiles safely?
- How should a provider profile migrate after company acquisition or endpoint rename?
- What event automatically quarantines a profile?
- How can Opure detect ignored `store=false` behaviour?
- Should synthetic canary requests detect unexpected state retrieval?
- Which permanent evidence is needed for privacy investigations?

---

# 195. Deferred Decisions

This ADR intentionally defers:

- final provider adapter list;
- provider benchmark selection;
- provider-side agents;
- provider conversations;
- provider files;
- provider vector stores;
- provider prompt caching;
- provider web search;
- provider remote MCP;
- provider code interpreter;
- provider computer use;
- provider batch;
- provider fine-tuning;
- provider evaluations;
- remote audio;
- cross-provider ensembles;
- automatic fallback;
- model-download trust architecture;
- enterprise contract ingestion;
- legal compliance automation;
- and provider-policy notification service.

---

# 196. Alternatives Rejected

One global cloud consent is rejected because provider, feature, data class and project matter.

Brand-level trust is rejected because product tiers and endpoints have different terms.

OpenAI-compatible trust is rejected because protocol compatibility says nothing about operator or data handling.

Provider-side state is disabled because Opure should remain the system of record.

Direct provider access from plugins, MCP servers and workflows is rejected because credentials and policy would fragment.

Automatic local-to-cloud fallback is rejected because it changes the data boundary.

Automatic cloud-to-cloud fallback is rejected because it changes operator, model, region, cost and terms.

Consumer chat interfaces are rejected for project automation because they are not provider API profiles.

Whole-repository upload is rejected because data minimisation and inspectability require bounded context.

Provider output authority is rejected because generated content must remain reviewable and deterministic services must own mutation.

---

# 197. Official and Primary Evidence References

## OpenAI API

- [Data controls in the OpenAI platform](https://platform.openai.com/docs/models/default-usage-policies-by-endpoint)
- [OpenAI API documentation](https://platform.openai.com/docs/)
- [OpenAI API authentication and quickstart](https://platform.openai.com/docs/quickstart)

## Anthropic Commercial API

- [Anthropic commercial data retention](https://privacy.claude.com/en/articles/7996866-how-long-do-you-store-my-organization-s-data)
- [Anthropic API data deletion](https://privacy.claude.com/en/articles/7996875-can-you-delete-data-that-i-sent-via-api)
- [Anthropic Zero Data Retention coverage](https://privacy.claude.com/en/articles/8956058-i-have-a-zero-data-retention-agreement-with-anthropic-what-products-does-it-apply-to)
- [Anthropic API documentation](https://docs.anthropic.com/)

## Gemini Developer API

- [Gemini API Additional Terms of Service](https://ai.google.dev/gemini-api/terms)
- [Gemini zero data retention](https://ai.google.dev/gemini-api/docs/zdr)
- [Gemini data logging and sharing](https://ai.google.dev/gemini-api/docs/logs-policy)
- [Gemini context caching](https://ai.google.dev/gemini-api/docs/caching)
- [Gemini API versions](https://ai.google.dev/gemini-api/docs/api-versions)

## Microsoft Foundry and Azure Models

- [Data, privacy and security for models sold by Azure](https://learn.microsoft.com/en-us/azure/foundry/responsible-ai/openai/data-privacy)
- [Region availability for models sold by Azure](https://learn.microsoft.com/en-us/azure/foundry/foundry-models/concepts/models-sold-directly-by-azure-region-availability)
- [Microsoft Foundry model deployment types](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/deployment-types)

## Amazon Bedrock

- [Amazon Bedrock data protection](https://docs.aws.amazon.com/bedrock/latest/userguide/data-protection.html)
- [Amazon Bedrock data retention](https://docs.aws.amazon.com/bedrock/latest/userguide/data-retention.html)
- [Amazon Bedrock infrastructure security](https://docs.aws.amazon.com/bedrock/latest/userguide/infrastructure-security.html)
- [Amazon Bedrock security](https://docs.aws.amazon.com/bedrock/latest/userguide/security-overview.html)

Provider policies, APIs, retention controls, model identifiers, prices and regional availability can change.

All production profiles must revalidate their exact account, endpoint, feature and model configuration before approval.

---

# 198. Review Record

| Date | Reviewer | Decision | Notes |
|---|---|---|---|
| 18 July 2026 | Architecture draft | Proposed | Versioned Provider Profiles, reviewed data handling and exact per-request sharing plans recommended |

---

# 199. Approval

## Founder or Product Approval

- **Name:** Christopher Dyer
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Local default, remote consent, provider state and fallback policy review required

## AI Router Approval

- **Name or role:** AI Router and Provider Trust Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Provider profiles, plans, model binding and receipts required

## Security Approval

- **Name or role:** Security Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Secret scanning, credential injection, endpoint security and output boundaries required

## Privacy Approval

- **Name or role:** Privacy or Data Governance Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Provider evidence, data classes, retention, region and personal-data policy required

## Network Approval

- **Name or role:** Network Gateway Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Exact destination, TLS, DNS, redirect and private-endpoint controls required

## Workspace and Memory Approval

- **Name or role:** Workspace and Memory Owners
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Explicit snapshot and memory context selection required

## Test Approval

- **Name or role:** Test Architecture Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Provider fixtures, leakage, drift, fallback and stateful-feature tests required

---

# 200. Supersession

This ADR is superseded only when a later ADR:

- names ADR-0019 explicitly;
- explains why provider trust, data-sharing or fallback architecture changed;
- identifies Provider Profile and project-policy migration;
- describes credentials, receipts and retained state;
- explains privacy, region, cost and output-safety impact;
- and updates the `Superseded by` field.

Historical profiles, evidence and request receipts remain available according to retention policy.

---

# 201. Change History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | 18 July 2026 | Founder Draft | Initial provider-neutral trust, data handling, request planning and local-first recommendation |

---

# 202. Final Decision Statement

> **Opure will provisionally make local inference the default and permit remote inference only through immutable reviewed Provider Profile revisions whose service operator, model publisher, account, endpoint, region, retention, training, state, caching and external-tool posture are explicitly recorded, with every remote operation bound to a canonical secret-scanned Data Sharing Plan that identifies the exact project context, data classes, model, cost and provider chain, credentials injected only at the final Network Gateway boundary, provider-side state and tools disabled, no automatic provider, region or model fallback, and every output retained as untrusted proposed content with provenance and a Trust Centre receipt, because API compatibility, authentication and model branding do not establish consent, privacy, reproducibility or authority over developer source code.**

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**