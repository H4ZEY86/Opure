# SPEC-004 — AI Router

## Opure Platform Provider-Neutral Intelligence Routing

**Document:** SPEC-004  
**Status:** Founder Draft  
**Version:** 0.1  
**Language:** British English  
**Last updated:** 18 July 2026  
**Depends on:** CHARTER-001, SPEC-001, SPEC-002, SPEC-003  

---

## 1. Purpose

This specification defines the AI Router of the Opure Platform.

The AI Router is the provider-neutral service responsible for:

- registering AI providers;
- discovering models;
- describing model capabilities;
- selecting suitable models for tasks;
- routing requests;
- enforcing local and cloud policy;
- coordinating model loading;
- streaming responses;
- supporting cancellation;
- handling provider health;
- managing bounded retry and fallback;
- recording usage and provenance;
- and exposing a stable Opure intelligence contract.

The AI Router must allow Opure to evolve independently from any individual model, provider or vendor.

The first implementation may support Ollama before other providers.

The architecture must not make Ollama, or any other provider, a permanent dependency of the Opure Platform.

---

## 2. Founding Rule

> **AI is a replaceable engineering capability, not the authority or identity of Opure.**

Every major Opure component must interact with artificial-intelligence providers through the AI Router.

No major platform service may depend directly on:

- an individual provider SDK;
- a provider-specific request format;
- a provider-specific response object;
- a provider-specific model name;
- or provider-specific error behaviour.

Provider-specific behaviour must be translated at the provider-adapter boundary.

---

## 3. Relationship to Other Services

The AI Router operates within the service architecture defined by SPEC-003.

A simplified logical view is:

```text
Workflow Engine
Context Engine
Knowledge Engine
Desktop Gateway
        │
        ▼
     AI Router
        │
        ├── Model Registry
        ├── Provider Registry
        ├── Capability Matcher
        ├── Routing Engine
        ├── Request Normaliser
        ├── Response Normaliser
        ├── Usage and Cost Tracker
        ├── Provider Health Manager
        └── Model Runtime Coordinator
                │
                ▼
       Provider Adapters
        ├── Ollama Adapter
        ├── LM Studio Adapter
        ├── llama.cpp Adapter
        ├── OpenAI-Compatible Adapter
        ├── Commercial Cloud Adapters
        └── Future Provider Adapters
```

The AI Router depends on:

- Policy Engine;
- Network Gateway;
- Secrets Vault;
- Scheduler;
- Trust Centre;
- Configuration Manager;
- Runtime Kernel;
- and provider adapters.

The AI Router does not own:

- project files;
- project knowledge;
- workflow definitions;
- patch application;
- MCP tool execution;
- secret values;
- or developer permissions.

---

## 4. Design Goals

The AI Router must be:

- provider-neutral;
- model-neutral;
- local-first;
- cloud-optional;
- policy-controlled;
- capability-driven;
- observable;
- cancellable;
- resilient;
- performance-aware;
- resource-aware;
- and honest about uncertainty.

It should make model behaviour understandable without pretending all providers expose the same information.

It must preserve developer control when routing, retrying or falling back.

---

## 5. Non-Goals

The AI Router is not responsible for:

- selecting which project files are relevant;
- reading arbitrary project files;
- constructing complete project context independently;
- applying generated code changes;
- executing tools;
- approving network access;
- deciding permissions;
- storing secrets;
- managing project memory;
- or claiming that an AI response is correct.

Those responsibilities belong to other Opure services.

The AI Router may validate response structure and provider metadata.

It must not confuse structural validity with engineering correctness.

---

## 6. Normative Language

The terms **MUST**, **MUST NOT**, **SHOULD**, **SHOULD NOT**, **MAY** and **DEFERRED** have the meanings defined in SPEC-001.

Any intentional violation of a **SHOULD** requirement must be documented in an Architecture Decision Record.

---

# 7. Core Responsibilities

## 7.1 Provider Registration

The AI Router must maintain a registry of available providers.

A provider registration must include:

- provider identifier;
- provider type;
- provider display name;
- adapter version;
- contract version;
- local or remote classification;
- endpoint information where applicable;
- authentication method;
- supported capabilities;
- health-check mechanism;
- privacy classification;
- rate-limit metadata where known;
- usage or cost metadata where known;
- and provider-specific configuration schema.

Provider registration must not expose secret values.

---

## 7.2 Model Discovery

The AI Router must discover or register models available through each provider.

A model record should include:

- stable internal model identifier;
- provider identifier;
- provider-native model identifier;
- display name;
- model family where known;
- model version or digest where known;
- local or remote classification;
- capabilities;
- supported input modalities;
- supported output modalities;
- context limit where known;
- output limit where known;
- tool-use support;
- structured-output support;
- embedding dimensions where relevant;
- estimated resource requirements;
- quantisation or precision where relevant;
- availability state;
- and evidence source for the metadata.

Unknown metadata must remain unknown.

Opure must not invent model capabilities from a model name alone.

---

## 7.3 Capability Matching

The AI Router must select models by declared task requirements and model capabilities.

Selection must not depend solely on model size or marketing category.

Capability matching should consider:

- task type;
- input modality;
- output modality;
- context size;
- structured-output requirement;
- tool-use requirement;
- code-specialisation preference;
- reasoning-depth preference;
- latency target;
- local or cloud restriction;
- available hardware;
- model availability;
- privacy policy;
- developer preference;
- and cost limits where applicable.

---

## 7.4 Request Routing

The AI Router must route each accepted request to one explicitly selected provider and model.

A routed request must identify:

- request identifier;
- initiating service;
- project identifier where applicable;
- task classification;
- selected provider;
- selected model;
- routing reason;
- policy decision reference;
- context summary;
- expected output contract;
- timeout;
- cancellation context;
- and Trust Centre correlation.

---

## 7.5 Response Normalisation

Provider responses must be translated into a stable Opure response contract.

The normalised response should support:

- text content;
- structured content;
- streamed segments;
- finish reason;
- provider usage metadata;
- tool-request proposals;
- safety or refusal metadata where exposed;
- model identifier;
- provider identifier;
- timing;
- and raw-response reference for diagnostics where permitted.

Provider-specific fields must not leak into unrelated services.

---

## 7.6 Provider Health

The AI Router must track provider and model health.

Health should include:

- configured;
- reachable;
- authenticated;
- available;
- busy;
- degraded;
- rate-limited;
- blocked by policy;
- unavailable;
- and disabled.

Local model health should distinguish between:

- provider runtime available;
- model installed;
- model loadable;
- model loaded;
- model busy;
- and model failed.

---

## 7.7 Model Runtime Coordination

For local providers, the AI Router and Scheduler must coordinate:

- model loading;
- model unloading;
- warm retention;
- inference queueing;
- GPU or CPU placement;
- VRAM pressure;
- system-memory pressure;
- model concurrency;
- and process recovery.

The AI Router owns model-routing intent.

The Scheduler owns execution-resource coordination.

---

# 8. Provider Types

## 8.1 Local Providers

A local provider executes inference on the developer's machine or within the developer-controlled local environment.

Examples may include:

- Ollama;
- LM Studio;
- llama.cpp;
- local OpenAI-compatible servers;
- and future local inference runtimes.

Local classification must be based on actual endpoint and execution behaviour, not provider branding.

A provider running on another machine is not automatically local merely because it uses local-model software.

---

## 8.2 Remote or Cloud Providers

A remote provider transmits request data outside the local machine or developer-controlled local boundary.

Remote providers require:

- a project policy that permits their use;
- visible data-sharing information;
- valid provider configuration;
- and approval appropriate to the project's active cloud policy.

---

## 8.3 Developer-Controlled Network Providers

A provider may run on a developer-controlled server or local network.

Such providers should have a distinct classification rather than being forced into only local or public-cloud categories.

Recommended classifications are:

- **Local Machine**;
- **Developer-Controlled Network**;
- **Private Remote Service**;
- **Public Cloud Service**.

Policy may treat these differently.

---

## 8.4 Embedded Providers

Opure may support an inference runtime embedded within an Opure-managed process.

Embedded providers must still use the provider-adapter contract.

Embedding a runtime does not permit bypassing:

- Scheduler control;
- Trust Centre recording;
- model metadata;
- cancellation;
- or project policy.

---

# 9. Provider Adapter Contract

## 9.1 Adapter Responsibility

A provider adapter translates between Opure contracts and provider-native behaviour.

An adapter owns:

- provider connection logic;
- provider-native request translation;
- provider-native response translation;
- provider authentication integration;
- provider health checks;
- model discovery;
- provider error translation;
- streaming translation;
- and provider-specific usage extraction.

---

## 9.2 Adapter Manifest

Each adapter must provide a manifest containing:

- adapter identifier;
- adapter version;
- supported AI Router contract version;
- provider types supported;
- capabilities;
- configuration schema;
- authentication requirements;
- network requirements;
- local-process requirements;
- and health-check behaviour.

---

## 9.3 Adapter Isolation

Third-party provider adapters should run within an appropriate isolation boundary.

A provider adapter failure must not crash the Runtime Kernel.

Adapters must not:

- read project files directly;
- access secrets without an approved reference;
- open undeclared network destinations;
- write to project files;
- or execute arbitrary tools.

---

## 9.4 Adapter Authentication

Adapters must request authentication material through the Secrets Vault.

Secret values must not be written to:

- adapter configuration files;
- logs;
- Trust Centre records;
- model requests unless required;
- or normal storage.

---

## 9.5 Adapter Errors

Provider-native errors must be converted to stable AI Router error categories.

The original safe provider message may be retained for diagnosis.

Secret-bearing headers or request values must be redacted.

---

# 10. Model Capability Model

## 10.1 Core Capabilities

The AI Router should recognise at least these capability categories:

- conversational text;
- code generation;
- code explanation;
- structured output;
- embeddings;
- tool-request generation;
- image understanding;
- audio understanding;
- audio generation;
- image generation;
- long-context processing;
- and reranking.

Only capabilities actually supported by the current provider and model may be marked available.

---

## 10.2 Capability Evidence

Each capability claim should include an evidence source such as:

- provider-declared metadata;
- local model manifest;
- adapter-declared support;
- verified capability test;
- developer override;
- or unknown.

A verified capability test should carry greater confidence than name-based inference.

---

## 10.3 Capability Levels

Capabilities may include levels or attributes.

For example:

```text
code_generation:
    supported: true
    confidence: verified
    languages: unknown
    structured_output: true
```

The capability schema must remain extensible.

---

## 10.4 Task Requirements

Every AI request should declare task requirements.

A task requirement may include:

- required capabilities;
- preferred capabilities;
- forbidden provider classes;
- minimum context size;
- maximum latency preference;
- expected response format;
- privacy classification;
- resource limit;
- cost limit;
- and whether fallback is permitted.

---

# 11. Model Profiles

## 11.1 Purpose

A model profile combines provider metadata, measured behaviour and developer preferences into a routing description.

A profile may include:

- quality tier;
- speed tier;
- context tier;
- code suitability;
- structured-output reliability;
- embedding suitability;
- typical memory use;
- typical VRAM use;
- typical first-token latency;
- tokens per second where measured;
- and stability notes.

---

## 11.2 Evidence-Based Profiles

Measured values should identify:

- hardware;
- provider version;
- model version or digest;
- quantisation;
- date measured;
- and sample size.

Measurements from one machine must not be treated as universal.

---

## 11.3 Developer Overrides

Developers may override routing preferences.

Overrides must be visible and project-scoped or global as selected.

A developer override may mark a model:

- preferred;
- allowed;
- disallowed;
- local-only;
- task-specific;
- experimental;
- or manual-selection-only.

---

## 11.4 Built-In Routing Presets

Opure may provide routing presets such as:

- **Fast Local**;
- **Best Local**;
- **Private Remote**;
- **Balanced**;
- **Lowest Cost**;
- **Manual Selection**.

Presets are convenience configurations.

They must not override project cloud policy.

---

# 12. Request Types

## 12.1 Text Generation

Used for:

- planning;
- explanation;
- documentation;
- review;
- code suggestions;
- and natural-language output.

---

## 12.2 Structured Generation

Used when the caller requires validated output matching a schema.

Examples include:

- patch plans;
- task graphs;
- issue classifications;
- architecture summaries;
- and tool-request proposals.

Structured generation must include a schema or explicit contract version.

---

## 12.3 Embeddings

Used to produce vector representations for retrieval or similarity operations.

Embedding requests must identify:

- input source;
- data classification;
- model;
- output dimension where known;
- normalisation behaviour where known;
- and storage destination.

Embedding output must not be treated as authoritative project knowledge.

---

## 12.4 Reranking

Used to order retrieved candidates by relevance.

Reranking may be local or remote.

Remote reranking remains subject to external data-sharing policy.

---

## 12.5 Multimodal Requests

Multimodal requests may include:

- images;
- audio;
- documents;
- and future media types.

Each modality must have:

- explicit provider capability;
- size limit;
- privacy classification;
- and preview where external sharing is proposed.

---

## 12.6 Tool-Request Generation

A model may propose tool calls.

The AI Router must return proposed tool requests as structured data.

It must not execute them.

Tool execution belongs to the Workflow Engine and the relevant controlled gateway or service.

---

# 13. Request Lifecycle

Every AI request should pass through the following lifecycle.

## 13.1 Stage 1 — Request Validation

The AI Router validates:

- caller identity;
- request contract;
- task requirements;
- expected output;
- timeout;
- cancellation context;
- and project context.

Invalid requests must fail before provider selection.

---

## 13.2 Stage 2 — Policy Evaluation

The AI Router requests a deterministic policy decision.

Policy evaluation must consider:

- project cloud mode;
- provider classification;
- data classification;
- proposed context;
- developer permissions;
- cost policy;
- and network policy.

The AI Router must not interpret an AI model's response as permission.

---

## 13.3 Stage 3 — Candidate Discovery

The AI Router identifies candidate models satisfying mandatory requirements.

Candidates blocked by policy must be excluded.

Candidates missing required capability must be excluded.

---

## 13.4 Stage 4 — Candidate Ranking

Eligible models may be ranked by:

- developer preference;
- local-first preference;
- capability fit;
- expected quality;
- latency;
- availability;
- context fit;
- resource fit;
- cost;
- and recent reliability.

The ranking explanation must be inspectable.

---

## 13.5 Stage 5 — Selection

One provider and model are selected.

The selected model must be visible before external transmission when project policy requires approval.

---

## 13.6 Stage 6 — Context and Prompt Preparation

The AI Router receives a context package from the Context Engine.

It may apply provider-format translation and final size enforcement.

It must not silently add unrelated project data.

---

## 13.7 Stage 7 — Data-Sharing Preview

For a remote provider, the request must include or reference a visible data-sharing preview.

The preview should identify:

- provider;
- model;
- data categories;
- approximate size;
- files or sources represented;
- detected secrets;
- redactions;
- purpose;
- and known retention information where configured.

---

## 13.8 Stage 8 — Approval

Approval must be obtained according to project policy.

A material change after approval invalidates that approval.

---

## 13.9 Stage 9 — Scheduling

The request is submitted to the Scheduler with:

- task priority;
- resource class;
- provider;
- model;
- deadline;
- cancellation;
- and concurrency requirements.

---

## 13.10 Stage 10 — Provider Invocation

The selected adapter invokes the provider.

Remote requests must use the Network Gateway.

Local process invocation must use approved runtime and process controls.

---

## 13.11 Stage 11 — Streaming and Validation

Responses may stream through the AI Router.

Segments must be:

- ordered;
- bounded;
- cancellable;
- attributed;
- and checked for contract violations.

Structured responses must be validated before being marked complete.

---

## 13.12 Stage 12 — Completion

The AI Router records:

- selected provider and model;
- routing reason;
- timings;
- finish reason;
- usage metadata;
- validation status;
- retries;
- fallback;
- errors;
- and Trust Centre correlation.

---

# 14. Local-First Routing

## 14.1 Default Preference

Where two candidates satisfy the task adequately, local execution should be preferred.

Local preference must consider actual capability.

Opure must not select a clearly unsuitable local model merely to avoid presenting a cloud option.

---

## 14.2 Local-Only Projects

Under **Local Only** policy:

- remote providers must not be considered;
- remote embeddings must not be used;
- remote reranking must not be used;
- and no hidden external provider discovery may transmit project data.

Provider-health checks that contact public services must also follow policy.

---

## 14.3 Ask Every Time

Under **Ask Every Time** policy, every remote request involving project data requires explicit approval.

Previous approval for a different request must not be reused automatically.

---

## 14.4 Approved Providers Only

Under **Approved Providers Only**, only explicitly approved provider identities may be considered.

Approval may also restrict:

- models;
- tasks;
- data types;
- projects;
- and time period.

---

## 14.5 Custom Policy

Custom policy may define:

- local-first ordering;
- provider allow-list;
- provider deny-list;
- model allow-list;
- task-specific routing;
- maximum cost;
- maximum data size;
- sensitive-file exclusions;
- and approval requirements.

---

# 15. No Silent Cloud Fallback

The AI Router must never silently switch from a local provider to a remote provider.

If a local request fails and a remote fallback is available, Opure must:

1. explain that the local request failed;
2. identify the proposed remote provider and model;
3. show what data would be shared;
4. explain why fallback may help;
5. request approval under project policy;
6. and record the decision.

A project may define a pre-approved remote fallback policy.

Such a policy must be:

- explicit;
- visible;
- limited;
- revocable;
- and compatible with the project's data-sharing restrictions.

---

# 16. Fallback Rules

## 16.1 Fallback Definition

Fallback means selecting a different model or provider after the preferred route cannot complete successfully.

Fallback may occur because of:

- provider unavailability;
- model unavailability;
- context limit;
- unsupported capability;
- timeout;
- rate limit;
- resource pressure;
- or invalid structured output.

---

## 16.2 Same-Provider Fallback

Fallback to another model within the same provider may be allowed automatically when:

- project policy permits it;
- privacy classification does not change;
- capability requirements remain satisfied;
- and the developer's routing policy permits automatic model fallback.

The fallback must still be recorded.

---

## 16.3 Provider-Class Change

Fallback that changes from:

- local to remote;
- private remote to public cloud;
- approved provider to unapproved provider;
- or lower data-retention class to higher-risk class

requires renewed policy evaluation and approval where applicable.

---

## 16.4 Quality Downgrade

A fallback that materially reduces expected capability should be visible.

The AI Router must not present degraded output as equivalent without qualification.

---

## 16.5 Bounded Attempts

Fallback attempts must be bounded.

The AI Router must prevent provider loops and uncontrolled cost.

---

# 17. Model Selection

## 17.1 Selection Inputs

Model selection should consider:

- task requirements;
- project policy;
- provider health;
- model health;
- context size;
- hardware resources;
- performance mode;
- developer preferences;
- measured model profile;
- cost limits;
- and current queue state.

---

## 17.2 Selection Strategy

The initial strategy may use deterministic weighted scoring.

The exact algorithm is DEFERRED to an ADR.

The selection strategy must be:

- inspectable;
- testable;
- reproducible for the same known inputs where practical;
- and independent from an LLM deciding which model should be trusted.

---

## 17.3 Manual Selection

The developer must be able to select a provider and model manually.

Manual selection remains subject to:

- project policy;
- capability requirements;
- provider availability;
- and resource feasibility.

---

## 17.4 Routing Explanation

The AI Router should provide a concise explanation such as:

```text
Selected: Local Model A
Reason:
- satisfies code-generation and structured-output requirements;
- fits the available context;
- already loaded;
- allowed by Local Only policy;
- expected to complete within Balanced mode limits.
```

The explanation must not claim certainty where only estimates exist.

---

## 17.5 Selection Stability

The AI Router should avoid unnecessary model switching between related workflow stages.

However, a workflow may use different models for:

- planning;
- coding;
- review;
- embeddings;
- and documentation

when there is a justified benefit.

---

# 18. Performance-Aware Routing

## 18.1 Hardware Awareness

The AI Router should receive safe resource information from the Runtime Kernel and Scheduler.

Relevant information may include:

- CPU class;
- available system memory;
- GPU type;
- available VRAM;
- current GPU use;
- current memory pressure;
- model residency;
- and active workload.

---

## 18.2 Reference Hardware

The founder's reference machine is:

- Windows 11;
- AMD Ryzen 9 5950X;
- 32 GB system memory;
- NVIDIA RTX 5070 Ti with 16 GB VRAM.

The initial implementation should be optimised for excellent behaviour on this class of system.

This is not the minimum requirement.

---

## 18.3 Fast Model Residency

Under Balanced mode, Opure should keep one suitable fast local model loaded where hardware and provider behaviour permit.

The model should be unloaded when:

- memory pressure requires it;
- another high-priority task requires the resources;
- the developer requests it;
- the provider becomes unhealthy;
- or the expected benefit no longer justifies residency.

---

## 18.4 Heavy Model Loading

A heavy model should load only when:

- the task requirements justify it;
- resource limits permit it;
- the expected quality benefit is meaningful;
- and the routing explanation records why it was chosen.

---

## 18.5 Concurrency

The default should avoid many simultaneous model instances.

Concurrency may be increased when:

- models fit safely;
- tasks are independent;
- the developer selected Performance or Turbo;
- measured throughput improves;
- and interface responsiveness remains acceptable.

---

## 18.6 Performance Modes

### Eco

Routing should prefer:

- already-loaded models;
- smaller models;
- lower concurrency;
- lower memory use;
- and delayed background embeddings.

### Balanced

Routing should prefer:

- a capable fast default model;
- measured model switching;
- responsive interaction;
- and background work that yields to foreground tasks.

### Performance

Routing may prefer:

- higher-capability local models;
- greater concurrency;
- and faster completion over resource conservation.

### Turbo

Routing may use aggressive local resources within explicit limits.

Turbo must not bypass policy, security or developer visibility.

---

# 19. Context Management

## 19.1 Responsibility Boundary

The Context Engine selects project context.

The AI Router enforces provider and model limits.

The AI Router must not independently scan the project to fill unused context.

---

## 19.2 Context Package

A context package should include:

- package identifier;
- project identifier;
- task identifier;
- context items;
- source references;
- inclusion reasons;
- data classifications;
- redactions;
- estimated token count;
- and package version.

---

## 19.3 Token and Size Budget

The AI Router must calculate a request budget that accounts for:

- model context limit;
- system instructions;
- user request;
- retrieved context;
- tool results;
- expected output;
- safety margin;
- and provider-specific overhead.

---

## 19.4 Context Reduction

When context is too large, reduction should follow declared policy.

Possible strategies include:

- remove lowest-priority items;
- replace large items with trusted summaries;
- retrieve narrower code ranges;
- split the task;
- use a larger-context eligible model;
- or ask the caller to revise requirements.

The AI Router must not silently remove critical context without reporting it.

---

## 19.5 Context Provenance

Every context item must remain traceable to its source.

A model response should be linkable to the context package used.

---

## 19.6 Secret Detection

Context proposed for external transmission must pass secret detection.

Detected secrets must be removed or blocked by default.

Secret values required for provider authentication must be handled separately through the Secrets Vault and Network Gateway.

---

## 19.7 Cross-Project Context

Cross-project context is prohibited by default.

It may be used only when:

- the developer explicitly enables it;
- the source project permits it;
- data classifications allow it;
- and provenance remains visible.

---

# 20. Prompt Construction

## 20.1 Prompt Layers

An Opure request may include:

- platform instructions;
- service-specific instructions;
- workflow-stage instructions;
- project rules;
- developer request;
- context package;
- expected output schema;
- and tool-result context.

These layers must remain logically distinguishable.

---

## 20.2 Prompt Visibility

The developer must be able to inspect the effective prompt or request sent to the model, subject to secret redaction and provider limitations.

The interface may present:

1. concise summary;
2. structured prompt layers;
3. complete serialised request where safe.

---

## 20.3 Hidden Instructions

Opure must not use hidden instructions to override the developer's project policy or stated intent.

Security instructions may remain protected from modification.

Their existence and purpose should still be disclosed at an appropriate level.

---

## 20.4 Prompt Templates

Prompt templates must be:

- versioned;
- testable;
- inspectable;
- associated with a workflow or service;
- and recorded by identifier in Trust Centre activity.

---

## 20.5 Project Instructions

Project-level instructions should be stored as project configuration or knowledge, not silently appended from unrelated conversations.

---

## 20.6 Prompt Injection Defence

Content from project files, external pages, tool results and retrieved documents must be treated as data unless explicitly trusted as instructions.

The AI Router and Context Engine should label untrusted content clearly.

Deterministic policy and permission enforcement must not be delegated to model compliance.

---

# 21. Structured Output

## 21.1 Schema Requirement

When a caller requires structured output, it must supply:

- schema identifier;
- schema version;
- required fields;
- validation rules;
- and repair policy.

---

## 21.2 Provider Support

If a provider supports native structured output, the adapter may use it.

If it does not, Opure may use prompt-based structured generation with validation.

The response must state which method was used.

---

## 21.3 Validation

Structured output must be validated before being accepted.

Validation failure should produce:

- validation errors;
- raw safe output reference;
- repair eligibility;
- and retry or fallback decision.

---

## 21.4 Repair

The AI Router may attempt bounded response repair.

Repair attempts must:

- use the same or approved alternative model;
- preserve the original request correlation;
- be recorded;
- and avoid hiding repeated failure.

---

## 21.5 Partial Output

Partial structured output must not be marked complete.

The caller may choose to inspect or recover partial data.

---

# 22. Streaming

## 22.1 Stream Contract

A stream should include:

- stream identifier;
- request identifier;
- sequence number;
- segment type;
- content;
- cumulative usage where available;
- completion state;
- and error state.

---

## 22.2 Segment Types

Segments may include:

- text delta;
- structured-data delta;
- reasoning summary delta where provided;
- tool-request proposal;
- usage update;
- warning;
- and completion.

Provider-private chain-of-thought must not be fabricated or exposed as if available.

---

## 22.3 Backpressure

Streaming must support backpressure.

A slow desktop consumer must not cause unbounded memory use.

The AI Router may coalesce display deltas while preserving the complete final response.

---

## 22.4 Cancellation

Cancellation must be propagated to the provider adapter.

The final request state must distinguish:

- cancellation requested;
- provider cancellation accepted;
- provider cancellation unsupported;
- response already completed;
- and local stream closed.

---

## 22.5 Stream Recovery

Interrupted streams should not automatically be resumed unless the provider contract safely supports it.

A retry may produce a different response and must be treated as a new attempt.

---

# 23. Tool-Request Proposals

## 23.1 Proposal Only

The AI Router may return tool-request proposals.

It must not execute tools.

A proposal must include:

- requested capability;
- structured arguments;
- model explanation where available;
- provider and model;
- request correlation;
- and validation status.

---

## 23.2 Tool Name Translation

Provider-native tool names must be translated to Opure capability identifiers.

A model must not receive unrestricted access to arbitrary internal service names.

---

## 23.3 Validation

Tool-request proposals must be schema validated.

Invalid or unknown capabilities must be rejected.

---

## 23.4 Execution Boundary

Validated proposals are sent to the Workflow Engine or responsible service.

Execution remains subject to:

- Policy Engine;
- MCP Gateway;
- Plugin Host;
- CLI Adapter Host;
- Network Gateway;
- Workspace Service;
- and Trust Centre recording.

---

## 23.5 Tool Results

Tool results may be returned to the model as context.

Tool-result content must retain:

- source;
- trust classification;
- size;
- data classification;
- and redaction status.

---

# 24. Embeddings

## 24.1 Provider Neutrality

Embedding models must be accessed through the AI Router.

The Knowledge Engine must not depend directly on an embedding provider.

---

## 24.2 Local Preference

Project embeddings should default to local models where practical.

Remote embeddings require cloud policy and data-sharing approval.

---

## 24.3 Embedding Identity

Stored vectors must be associated with:

- model identifier;
- provider identifier;
- model version or digest;
- vector dimension;
- normalisation behaviour;
- creation date;
- source content hash;
- and chunking strategy.

---

## 24.4 Reindexing

A change in embedding model, dimension or chunking strategy may require reindexing.

The Knowledge Engine must not compare incompatible vector spaces as if they were equivalent.

---

## 24.5 Sensitive Content

Secret values must not be embedded.

Sensitive content should follow project policy and storage rules.

---

## 24.6 Embedding Confidence

Vector similarity is a retrieval signal.

It is not evidence that:

- code is correct;
- code is safe;
- code is compatible;
- or two engineering problems are equivalent.

---

# 25. Model Downloads and Local Assets

## 25.1 Visibility

A model download must show:

- model identity;
- provider;
- source;
- expected size where known;
- destination;
- licence information where available;
- integrity metadata where available;
- and expected resource requirements.

---

## 25.2 Approval

Substantial downloads require explicit approval unless covered by a visible developer preference.

Background model downloads must not occur silently.

---

## 25.3 Storage Ownership

Local model assets must be stored in an Opure-managed or provider-managed location with clear ownership.

The developer should be able to inspect:

- installed models;
- storage use;
- last use;
- provider;
- and removal impact.

---

## 25.4 Integrity

Downloaded model assets should be integrity checked where the source provides a trusted digest or signature.

Integrity failure must block use.

---

## 25.5 Licence Awareness

Opure should display known model licence information.

Unknown licence information must be identified as unknown.

Opure must not claim that a model is unrestricted without evidence.

---

## 25.6 Removal

Removing a model must identify:

- current workflows using it;
- fallback effects;
- storage reclaimed;
- and whether provider metadata remains.

---

# 26. Provider Health and Resilience

## 26.1 Health Checks

Provider adapters should support:

- configuration validation;
- reachability;
- authentication;
- model-list query;
- lightweight inference probe where appropriate;
- and version compatibility.

Health checks must avoid sending project data.

---

## 26.2 Circuit Breakers

Repeated provider failure should trigger a circuit breaker.

The circuit breaker should expose:

- provider;
- model if specific;
- state;
- last error;
- failure count;
- next retry time;
- and manual reset option.

---

## 26.3 Rate Limits

Where known, the AI Router should track:

- request limits;
- token limits;
- concurrency limits;
- reset times;
- and provider warnings.

Unknown limits must remain unknown.

---

## 26.4 Local Runtime Failure

If a local provider process crashes, Opure should:

1. preserve request state;
2. record the failure;
3. release resources;
4. apply bounded restart policy;
5. avoid restart loops;
6. and present safe fallback choices.

---

## 26.5 Degraded Providers

A provider may be marked degraded when:

- some models are unavailable;
- streaming fails;
- structured output is unreliable;
- embeddings are unavailable;
- or latency is unusually high.

Capability-specific degradation should be visible.

---

# 27. Retries

## 27.1 Retry Eligibility

Retries may be appropriate for:

- transient connection failure;
- temporary provider overload;
- safe timeout;
- rate-limit delay;
- or invalid structured output.

Retries must not be automatic when they may create duplicate external side effects.

AI inference itself is generally side-effect free, but tool execution is not part of the AI Router.

---

## 27.2 Retry Limits

Retries must be bounded by:

- maximum attempts;
- deadline;
- cost budget;
- provider policy;
- and cancellation.

---

## 27.3 Retry Visibility

The final response must identify the number of attempts.

Repeated attempts must not be hidden.

---

## 27.4 Prompt Stability

A retry should use the same effective request unless a documented repair or reduction strategy is applied.

Changes to context or model must be recorded.

---

# 28. Cost and Usage

## 28.1 Usage Metadata

The AI Router should capture, where available:

- input tokens or units;
- output tokens or units;
- cached input;
- image or audio units;
- request duration;
- estimated or reported cost;
- and provider request identifier.

---

## 28.2 Unknown Cost

If cost cannot be determined, it must be shown as unknown.

Opure must not estimate cost with false precision.

---

## 28.3 Cost Policy

Projects may define:

- per-request limit;
- daily limit;
- monthly limit;
- provider limit;
- task-specific limit;
- and approval threshold.

---

## 28.4 Preflight Estimate

For remote paid providers, Opure should provide a preflight estimate where enough information exists.

The estimate must be labelled as an estimate.

---

## 28.5 Local Cost

Local inference has no provider invoice but still consumes:

- electricity;
- time;
- hardware resources;
- and storage.

Opure may show resource estimates without describing local inference as cost-free.

---

# 29. Privacy and Data Sharing

## 29.1 Data Classification

Every request should carry a data classification.

Recommended classes are defined in more detail by SPEC-008.

---

## 29.2 Data Minimisation

Only context necessary for the task should be included.

Unused context-window capacity is not a reason to send more project data.

---

## 29.3 Remote Request Preview

Before a remote request requiring approval, Opure must show:

- destination provider;
- selected model;
- data categories;
- source files or records represented;
- approximate size;
- detected sensitive content;
- redactions;
- and purpose.

---

## 29.4 Provider Retention Information

Provider retention or training information may be shown when configured from reliable provider metadata.

Unknown or unverified behaviour must be labelled unknown.

The AI Router must not promise provider-side deletion it cannot enforce.

---

## 29.5 No Secret Leakage

Provider authentication secrets must be supplied through secure transport mechanisms.

They must not be included inside prompt content.

---

## 29.6 Provider Isolation

A provider must not receive project context from another project unless explicitly approved.

---

# 30. Trust Centre Integration

## 30.1 Required Records

The AI Router should record:

- request identifier;
- initiating service;
- project;
- task type;
- selected provider;
- selected model;
- routing explanation;
- local or remote classification;
- policy decision;
- context-package reference;
- external data-sharing summary;
- approval reference;
- timings;
- usage;
- retries;
- fallback;
- validation result;
- finish reason;
- cancellation;
- and final status.

---

## 30.2 Prompt Records

The Trust Centre should provide access to the effective prompt or request where policy permits.

Secrets must be redacted.

Large context may be represented through references and summaries rather than duplicated.

---

## 30.3 Provider Reasoning Limitations

If a provider does not expose internal reasoning, the Trust Centre must state this clearly.

Opure may store:

- model output;
- routing explanation;
- workflow reasoning summary;
- and tool evidence.

It must not fabricate hidden chain-of-thought.

---

## 30.4 Raw Provider Data

Raw provider requests and responses may be retained temporarily for diagnosis only when:

- policy allows;
- sensitive content is protected;
- retention is bounded;
- and the developer can inspect or delete it.

Raw retention should be disabled by default for sensitive projects.

---

# 31. Error Model

## 31.1 Error Categories

The AI Router should expose stable categories:

- `AI_REQUEST_INVALID`;
- `AI_POLICY_DENIED`;
- `AI_APPROVAL_REQUIRED`;
- `AI_PROVIDER_UNAVAILABLE`;
- `AI_PROVIDER_AUTHENTICATION_FAILED`;
- `AI_PROVIDER_RATE_LIMITED`;
- `AI_MODEL_UNAVAILABLE`;
- `AI_MODEL_INCOMPATIBLE`;
- `AI_CONTEXT_TOO_LARGE`;
- `AI_RESOURCE_UNAVAILABLE`;
- `AI_TIMEOUT`;
- `AI_CANCELLED`;
- `AI_STREAM_INTERRUPTED`;
- `AI_RESPONSE_INVALID`;
- `AI_STRUCTURED_OUTPUT_INVALID`;
- `AI_COST_LIMIT_EXCEEDED`;
- `AI_NETWORK_BLOCKED`;
- and `AI_INTERNAL_ERROR`.

---

## 31.2 Error Content

Errors should include:

- stable code;
- safe message;
- provider;
- model;
- retryability;
- fallback availability;
- correlation identifier;
- and recovery guidance.

Errors must not expose secrets.

---

## 31.3 Partial Results

An error may include a partial result.

Partial results must be labelled clearly and must not be treated as complete.

---

# 32. Configuration

## 32.1 Global Configuration

Global configuration may define:

- enabled providers;
- default routing preset;
- performance mode;
- model-storage location;
- health-check interval;
- and usage display preferences.

---

## 32.2 Project Configuration

Project configuration may define:

- cloud policy;
- approved providers;
- preferred models;
- forbidden models;
- task-specific routes;
- cost limits;
- context limits;
- fallback policy;
- and embedding model.

Project configuration overrides global defaults where allowed.

---

## 32.3 Session Overrides

A developer may choose a temporary provider or model for one task or session.

The override must be visible and must not silently persist.

---

## 32.4 Sensitive Configuration

Authentication secrets must be stored in the Secrets Vault.

Configuration stores only secret references.

---

## 32.5 Configuration Validation

The AI Router must validate:

- provider endpoint;
- adapter compatibility;
- model identity;
- policy compatibility;
- and required secrets.

---

# 33. API Contract

## 33.1 Core Commands

The AI Router should provide commands such as:

- `RegisterProvider`;
- `UpdateProvider`;
- `RemoveProvider`;
- `EnableProvider`;
- `DisableProvider`;
- `RefreshModels`;
- `StartInference`;
- `CancelInference`;
- `LoadLocalModel`;
- `UnloadLocalModel`;
- and `ResetProviderCircuit`.

---

## 33.2 Core Queries

The AI Router should provide queries such as:

- `ListProviders`;
- `GetProvider`;
- `ListModels`;
- `GetModel`;
- `GetRoutingCandidates`;
- `ExplainRoute`;
- `GetProviderHealth`;
- `GetModelRuntimeState`;
- `EstimateRequest`;
- and `GetUsageSummary`.

---

## 33.3 Core Events

The AI Router should publish events such as:

- `ProviderRegistered`;
- `ProviderHealthChanged`;
- `ModelDiscovered`;
- `ModelAvailabilityChanged`;
- `ModelLoadStarted`;
- `ModelLoaded`;
- `ModelUnloaded`;
- `InferenceStarted`;
- `InferenceCompleted`;
- `InferenceFailed`;
- `InferenceCancelled`;
- `FallbackProposed`;
- and `UsageLimitReached`.

---

# 34. Security Requirements

## 34.1 Deterministic Policy

Routing permission must be decided by the Policy Engine.

The AI Router must not override a policy denial.

---

## 34.2 Network Mediation

Remote provider traffic must use the Network Gateway.

---

## 34.3 Secret Mediation

Provider credentials must use the Secrets Vault.

---

## 34.4 Provider Endpoint Validation

Custom endpoints must be validated.

The interface should warn about:

- insecure transport;
- loopback versus remote address;
- unexpected redirect;
- certificate failure;
- and endpoint classification changes.

---

## 34.5 Request Size Limits

The AI Router must enforce bounded request size.

Provider-advertised limits do not replace local safety limits.

---

## 34.6 Response Size Limits

Streaming and completed responses must have configurable safety limits.

Exceeding a limit should cancel or truncate according to declared policy and report the action.

---

## 34.7 Untrusted Output

Model output must be treated as untrusted data.

It must not directly:

- execute commands;
- modify files;
- grant permission;
- access secrets;
- or make network requests.

---

# 35. Testing Strategy

## 35.1 Unit Tests

Unit tests must cover:

- capability matching;
- candidate filtering;
- route scoring;
- policy handling;
- local-versus-remote classification;
- context budgeting;
- fallback decisions;
- retry decisions;
- usage aggregation;
- and error translation.

---

## 35.2 Contract Tests

Every provider adapter must pass contract tests for:

- registration;
- model discovery;
- health;
- request translation;
- response translation;
- streaming;
- cancellation;
- structured output;
- error translation;
- and secret handling.

---

## 35.3 Simulation Providers

The test suite should include deterministic simulated providers for:

- successful text response;
- streaming response;
- timeout;
- cancellation;
- malformed output;
- unavailable model;
- rate limit;
- context overflow;
- provider crash;
- and partial stream failure.

---

## 35.4 Policy Tests

Tests must prove:

- Local Only excludes remote providers;
- Ask Every Time requires approval;
- approval is invalidated by material request change;
- local failure does not silently trigger cloud fallback;
- disallowed providers never receive requests;
- and revoked approval cannot be reused.

---

## 35.5 Resource Tests

Tests should simulate:

- low VRAM;
- low system memory;
- model already loaded;
- competing GPU task;
- Eco mode;
- Balanced mode;
- Performance mode;
- and Turbo mode.

---

## 35.6 Security Tests

Tests must cover:

- secret redaction;
- endpoint reclassification;
- provider adapter attempting direct file access;
- provider adapter attempting unauthorised network access;
- malicious model tool request;
- oversized response;
- and prompt-injection content attempting to alter policy.

---

## 35.7 Failure Tests

Tests should cover:

- local provider crash;
- remote connection loss;
- repeated invalid structured output;
- fallback loops;
- retry exhaustion;
- stream interruption;
- and Trust Centre unavailability.

---

# 36. Performance Requirements

## 36.1 Router Overhead

AI Router routing overhead should remain small relative to inference latency.

Exact targets are DEFERRED until the prototype exists.

---

## 36.2 Interactive Responsiveness

Model discovery, health checking and background profiling must not block interactive requests unnecessarily.

---

## 36.3 Queue Visibility

When a request is queued, the developer should see:

- queue state;
- blocking resource;
- selected model;
- and cancellation option.

---

## 36.4 Provider Discovery

Provider and model discovery should be cached with explicit refresh and expiry.

Discovery must not repeatedly wake large local runtimes without reason.

---

## 36.5 Streaming Latency

The AI Router should forward safe response segments promptly rather than buffer the entire response unnecessarily.

---

# 37. Initial Ollama Implementation

## 37.1 Scope

The first provider adapter may target Ollama.

The Ollama adapter should support, where available through the chosen integration:

- provider health;
- model discovery;
- text generation;
- conversational generation;
- streaming;
- cancellation or process-level interruption where supported;
- embeddings;
- model metadata;
- local model load behaviour;
- and local error translation.

---

## 37.2 Architectural Constraint

Ollama-specific request objects, model tags and response formats must remain inside the Ollama adapter.

The rest of Opure must use AI Router contracts.

---

## 37.3 Provider Availability

Opure should detect whether Ollama is:

- not installed;
- installed but not running;
- running;
- reachable;
- incompatible;
- or healthy.

Detection must not be represented as certainty when the evidence is incomplete.

---

## 37.4 Model Installation

The initial Ollama integration may direct the developer through provider-managed model installation.

Opure should display the expected operation and storage impact before substantial downloads.

---

## 37.5 Future Providers

The first implementation must include adapter contract tests that can be reused for future providers.

A second simple test adapter should be implemented early to prove provider neutrality.

---

# 38. Initial Implementation Milestone

The first AI Router milestone is successful when it can:

1. register a local provider adapter;
2. discover installed models;
3. normalise model metadata;
4. expose model capabilities;
5. accept a provider-neutral text request;
6. select a local model deterministically;
7. stream a response;
8. cancel a request;
9. record routing and usage information;
10. handle provider failure;
11. avoid silent cloud fallback;
12. operate under Local Only policy;
13. validate structured output;
14. create embeddings through the same router abstraction;
15. expose provider and model health;
16. coordinate model loading with the Scheduler;
17. and pass adapter contract tests.

The milestone does not require a public cloud provider.

---

# 39. Acceptance Criteria

SPEC-004 is implemented when:

- [ ] No major service depends directly on a provider SDK.
- [ ] Provider adapters implement a stable versioned contract.
- [ ] Providers and models are discoverable and health-checked.
- [ ] Model capabilities are explicit and evidence-linked.
- [ ] Requests declare capability and policy requirements.
- [ ] Candidate filtering excludes disallowed providers.
- [ ] Local execution is preferred when suitable.
- [ ] Local Only policy prevents remote routing.
- [ ] Local failure never silently becomes cloud fallback.
- [ ] Routing decisions are inspectable.
- [ ] Remote data sharing is previewed and approved where required.
- [ ] Provider credentials remain in the Secrets Vault.
- [ ] Remote traffic passes through the Network Gateway.
- [ ] Context size is budgeted and reductions are visible.
- [ ] Prompt layers and effective request are inspectable.
- [ ] Structured output is validated.
- [ ] Tool requests are proposals, not executions.
- [ ] Streaming supports ordering, backpressure and cancellation.
- [ ] Embeddings retain model and version identity.
- [ ] Incompatible vector spaces are not mixed.
- [ ] Retries and fallback are bounded.
- [ ] Provider and model health are visible.
- [ ] Cost and usage are shown where known.
- [ ] Unknown cost or capability remains labelled unknown.
- [ ] Model output cannot directly change system state.
- [ ] The first local-provider adapter passes contract tests.
- [ ] A simulated second adapter proves provider neutrality.
- [ ] Trust Centre records contain no secret values.
- [ ] Resource-aware model loading works in Balanced mode.
- [ ] The desktop remains responsive during inference.

---

# 40. Deferred Decisions

The following are intentionally deferred:

- exact AI Router implementation language;
- provider-adapter packaging format;
- exact routing score weights;
- benchmark methodology;
- default local model;
- default embedding model;
- default code model;
- model-download provider;
- local-model file format;
- exact prompt-template format;
- exact schema-validation library;
- commercial provider priority;
- provider pricing refresh mechanism;
- automatic benchmark schedule;
- multimodal implementation order;
- fine-tuning support;
- distributed inference;
- and remote Opure-managed inference services.

These decisions must not weaken the provider-neutral, local-first and visible-by-design requirements.

---

# 41. Required Architecture Decision Records

Implementation should produce ADRs for:

- AI Router implementation language;
- provider-adapter process isolation;
- provider-neutral request and response schema;
- routing algorithm;
- local-provider discovery;
- model capability verification;
- prompt-template versioning;
- structured-output validation;
- embedding-model migration;
- model residency and unload strategy;
- usage and cost accounting;
- and first Ollama integration method.

---

# 42. Relationship to Later Specifications

This specification provides the AI routing foundation for:

- **SPEC-005 — Memory Engine**
- **SPEC-006 — Workflow and Agent System**
- **SPEC-007 — Plugin SDK**
- **SPEC-008 — Trust Centre and Security**
- **SPEC-009 — Workspace and File Patch Engine**
- **SPEC-010 — Desktop User Interface**
- **SPEC-011 — Project and Build Management**

SPEC-005 will define how embeddings and model-generated project summaries are used by project memory.

SPEC-006 will define how workflows request and coordinate AI tasks.

SPEC-008 will define detailed policy, data classification, approval, network and secret requirements.

Later specifications may add stricter requirements.

They must not bypass the AI Router for provider access.

---

# 43. Founder Approval

This document remains a founder draft until explicitly approved.

Approval establishes the following rules:

- AI providers are replaceable.
- Ollama may be first, but it is not permanent architecture.
- Every AI request passes through the AI Router.
- Model selection is capability-driven and policy-controlled.
- Local execution is preferred when suitable.
- Cloud use is optional and visible.
- Local failure must never silently become cloud fallback.
- Provider credentials remain in the encrypted Secrets Vault.
- Model output cannot directly change project or system state.
- Routing, context, provider, model, usage and fallback remain inspectable.
- Performance-aware scheduling must respect the developer's hardware.

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**