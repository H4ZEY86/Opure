# ADR-0021 — Context Assembly and Token Budgeting

## Architecture Decision Record

**Status:** Proposed
**Date:** 18 July 2026
**Decision owners:** Founder and Product Owner
**Technical owners:** Context Assembly and AI Router Owner
**Reviewers:** Runtime Architecture Owner, AI Router Owner, Workspace Owner, Memory Owner, Search and Retrieval Owner, Project Policy Owner, Security Owner, Secrets Owner, Provider Trust Owner, Local Model Runtime Owner, Workflow Owner, Plugin Platform Owner, MCP Gateway Owner, Trust Centre Owner, Desktop Owner, Persistence Owner, Performance Owner, Test Architecture Owner
**Supersedes:** None
**Superseded by:** None
**Related ADRs:** ADR-0001 through ADR-0020
**Related specifications:** CHARTER-001, SPEC-001 through SPEC-012
**Target milestone:** Context and Project Memory through Version 1.0

---

## 1. Decision Summary

Opure should implement a trusted **Context Assembly Service** that creates an immutable, inspectable and model-specific Context Plan before any inference request is sent to a local or remote model.

The Context Assembly Service should be the sole authority for selecting, ordering, reducing and budgeting project context.

The AI Router should remain authoritative for provider and model execution.

The Workspace, Memory, Search, Repository, Build, Test, Plugin and MCP services should supply typed candidate references and content snapshots.

No component should construct a free-form model prompt by independently concatenating whatever data it can access.

The initial context architecture should:

* make context assembly a first-class trusted Runtime capability;
* represent every model input as a canonical Context Plan;
* bind each plan to:

  * one operation;
  * one project;
  * one workspace generation;
  * one model profile;
  * one provider profile revision or local runtime profile;
  * one instruction-template revision;
  * one tokenizer profile;
  * one output budget;
  * and one policy decision;
* preserve source provenance for every included item;
* preserve omission and reduction reasons for every excluded candidate;
* distinguish:

  * trusted product instructions;
  * user instructions;
  * explicit user-selected context;
  * deterministic operation state;
  * project instructions;
  * conversation history;
  * repository context;
  * build and test evidence;
  * project memory;
  * retrieved source chunks;
  * plugin results;
  * MCP results;
  * and model-generated summaries;
* never promote project content, MCP content, plugin content, tool output, repository text or model output into the trusted system-instruction layer;
* treat all externally supplied or project-supplied instructions as untrusted content unless a separately governed project-instruction policy explicitly classifies them;
* require project instructions to be visible, source-bound and lower priority than Opure security and product policy;
* include no ambient workspace context;
* include no hidden memory;
* include no clipboard content;
* include no unrelated open editor state;
* include no shell history;
* include no environment variables;
* include no secrets;
* and include no provider-side conversation state by default.

The initial token-budgeting architecture should:

* use one exact Tokenizer Profile per model and request format;
* never assume a universal characters-per-token ratio;
* never reuse token counts from another model;
* never reuse token counts after a tokenizer, model snapshot, chat template, request schema or tool definition changes;
* count the **fully serialised request shape** rather than merely summing plain-text fragments;
* count:

  * system and developer instructions;
  * user and assistant messages;
  * role and message framing;
  * special tokens;
  * chat templates;
  * tool definitions;
  * tool schemas;
  * structured-output schemas;
  * images;
  * audio;
  * documents;
  * citations metadata;
  * previous reasoning signatures when the provider requires them;
  * and provider-specific request overhead;
* distinguish:

  * model context-window limit;
  * model input-token limit;
  * model output-token limit;
  * combined input-and-output limit;
  * provider endpoint limit;
  * local Execution Profile context limit;
  * reasoning or thinking budget;
  * tool-use budget;
  * and product safety reserve;
* use the narrowest applicable limit;
* reserve output space before admitting optional context;
* reserve explicit space for:

  * the current user request;
  * trusted instructions;
  * expected answer;
  * structured output;
  * tool results where the operation permits them;
  * and a product safety margin;
* prohibit setting output reserve to zero merely to fit more context;
* make output reserve task specific;
* treat an output budget as a maximum, not a promise that every token will be generated;
* disable provider automatic truncation where the API supports that control;
* disable tokenizer-library automatic truncation;
* reject a final request that exceeds the approved budget;
* and never allow a provider to silently drop earlier messages or context items.

The initial token-counting order should be:

1. **Exact local request tokenisation using the exact loaded model and chat template**
2. **Exact provider token-count endpoint using the exact provider request shape**
3. **Pinned provider-specific local tokenizer with verified request-framing rules**
4. **Conservative model-specific estimator with a safety reserve**
5. **Unknown, causing strict-policy denial**

A provider token-count request is itself a data transmission.

It must:

* use the same approved Provider Profile;
* contain no broader data than the inference plan;
* be covered by the same project cloud policy;
* appear in the Data Sharing Plan;
* and produce a safe count receipt.

Provider token counting must not become a hidden preliminary cloud request.

For local `llama.cpp` models, Opure should use the exact model tokenizer and approved chat-template application through the trusted local runtime adapter or an equivalent pinned tokenizer implementation.

For remote providers, Opure should use official token-count endpoints when available and compatible with the configured data posture.

Where a provider documents token counts as estimates, Opure should preserve that status and maintain a conservative reserve.

The budget formula should conceptually be:

```text
effective_input_limit =
    minimum(
        model_input_limit,
        endpoint_input_limit,
        combined_window_adjusted_input_limit,
        local_execution_profile_limit,
        project_policy_limit
    )

available_context_budget =
    effective_input_limit
    - mandatory_instruction_tokens
    - current_request_tokens
    - conversation_structure_tokens
    - tool_definition_tokens
    - structured_output_schema_tokens
    - modality_overhead_tokens
    - reserved_output_interaction_tokens
    - safety_reserve_tokens
```

For models whose documented context window combines input and output:

```text
combined_window_adjusted_input_limit =
    combined_context_window
    - reserved_visible_output
    - reserved_reasoning_or_thinking
    - reserved_tool_result_continuation
```

For models with separate input and output limits, the separate input limit should remain authoritative and output should be constrained independently.

The exact budgeting model must be recorded in the Model Profile.

The initial context-priority order should be:

1. Opure security and product instructions
2. Current authenticated developer request
3. Explicitly selected project context
4. Exact operation state and required deterministic evidence
5. Project-governed instructions
6. Current patch, diff, build or test evidence
7. Recent relevant conversation turns
8. Directly referenced symbols and files
9. High-confidence repository retrieval
10. High-confidence project memory
11. Supporting dependency and call context
12. Optional metadata and examples

This order is a default.

A task-specific Context Policy may refine it without weakening security or adding hidden sources.

The Context Assembly Service should use a two-stage selection model:

### Candidate Generation

Generate bounded candidate references from approved sources.

### Deterministic Admission

Classify, score, deduplicate, budget and admit candidates through trusted code.

An AI model may suggest candidates.

It may not authorise or silently include them.

The initial candidate-ranking model should use inspectable features such as:

* explicit user selection;
* exact path mention;
* exact symbol mention;
* current diff participation;
* current diagnostic reference;
* build or test failure reference;
* dependency relationship;
* call relationship;
* import relationship;
* lexical match;
* semantic similarity;
* repository recency;
* memory confidence;
* memory freshness;
* source trust;
* generated-file penalty;
* duplicate penalty;
* and diversity contribution.

Semantic similarity should be one feature, not the sole authority.

The scoring formula, feature values and final reason should be visible in diagnostic and acceptance evidence.

The initial source-code chunking policy should:

* operate on immutable file snapshots;
* preserve relative path, content hash, encoding and workspace generation;
* prefer syntax-aware or symbol-aware boundaries when a trusted parser exists;
* fall back to line-stable chunks;
* preserve complete line boundaries;
* preserve UTF-8 and original line-ending metadata;
* avoid splitting a Unicode scalar or encoded byte sequence;
* include symbol signatures and relevant headers where practical;
* use bounded overlap;
* avoid repeating large imports or boilerplate in every chunk;
* and assign a stable Chunk ID derived from:

  * file snapshot hash;
  * relative path;
  * range;
  * chunker version;
  * parser version;
  * and normalisation policy.

The initial chunking system should support distinct strategies for:

* source code;
* configuration;
* Markdown;
* JSON;
* YAML;
* XML;
* logs;
* diffs;
* build diagnostics;
* test results;
* repository history;
* conversation;
* and memory.

The service should not treat every file as arbitrary prose.

The initial reduction policy should be deterministic and visible.

When a candidate plan exceeds the budget, Opure should apply, in order:

1. remove exact duplicates;
2. remove semantically redundant lower-priority chunks;
3. remove optional metadata;
4. reduce low-value chunk overlap;
5. reduce per-file supporting chunks;
6. remove the oldest non-essential conversation turns;
7. use an already-approved conversation summary;
8. reduce low-confidence memory;
9. reduce retrieval breadth while preserving source diversity;
10. narrow supporting code to exact symbols and relevant neighbouring ranges;
11. remove optional examples and generated files;
12. offer a smaller output or reasoning reserve only when the user explicitly selects it and the task remains viable;
13. offer another approved model with a larger context budget;
14. offer task decomposition;
15. or fail with an inspectable Context Budget Exceeded result.

Opure should not silently:

* truncate the start of a conversation;
* truncate the end of a file;
* remove the current request;
* remove an explicitly selected file;
* reduce requested output to an unusable size;
* summarise source code through another model;
* switch provider;
* switch model;
* enable provider caching;
* or split the operation into multiple model calls.

Those actions require a new plan and, where relevant, approval.

AI-generated summarisation may be used only as an explicit derived-context operation.

A summary should:

* retain source references;
* retain source hashes;
* record the summarising model;
* record the summary prompt;
* record validation state;
* be classified as model-generated;
* never replace source as authoritative evidence;
* and never be silently reused after source changes.

The initial conversation policy should:

* keep the current user request in full;
* keep the immediately relevant recent turns;
* preserve unresolved developer constraints;
* exclude hidden chain of thought;
* exclude provider-internal reasoning;
* preserve user-visible decisions;
* use local conversation summaries only when visible and provenance bound;
* and avoid blindly carrying every old assistant response forever.

The initial retrieval policy should be hybrid:

* exact path and symbol lookup;
* lexical search;
* repository graph and dependency evidence;
* semantic retrieval;
* project memory;
* and task-specific signals.

It should include diversity and per-source caps to avoid filling the window with near-duplicate chunks from one file.

The initial context cache policy should:

* cache token counts by exact tokenizer and request-framing identity;
* cache chunk boundaries by exact content and chunker identity;
* cache retrieval features by exact index generation;
* never cache authority;
* never reuse a Context Plan after its project, workspace, model, tokenizer, tool schema, provider profile or policy changes;
* keep raw content under the original project access boundary;
* and avoid provider-side context caching under ADR-0019.

Every final plan should produce a Context Receipt containing:

* operation;
* model;
* provider or runtime;
* tokenizer;
* instruction revision;
* included items;
* omitted items;
* reductions;
* token allocation;
* estimated count;
* exact preflight count where available;
* actual provider or runtime usage;
* output reserve;
* safety reserve;
* policy;
* approval;
* and deviations.

The selected trust chain is:

```text
Authenticated developer operation
    ↓
Task-specific Context Policy
    ↓
Approved source capabilities
    ↓
Immutable candidate references
    ↓
Classification and secret scanning
    ↓
Chunking and provenance
    ↓
Inspectable relevance scoring
    ↓
Deduplication and diversity
    ↓
Model-specific exact tokenisation
    ↓
Deterministic budget reduction
    ↓
Canonical Context Plan
    ↓
Policy or human approval
    ↓
Provider or local runtime request
    ↓
Actual usage reconciliation
    ↓
Context Receipt
```

---

## 2. Status

Current status:

> **Proposed**

This ADR becomes **Accepted** only after prototypes demonstrate:

* Context Assembly Service ownership;
* immutable Context Plans;
* explicit source references;
* no ambient workspace access;
* trusted instruction separation;
* project-instruction governance;
* model-specific Tokenizer Profiles;
* exact local model counting;
* one official remote provider count endpoint;
* a second remote provider count endpoint with different accounting semantics;
* no hidden token-count network call;
* fully serialised request counting;
* tool-schema accounting;
* structured-output accounting;
* image accounting;
* separate and combined context-window models;
* output and reasoning reserves;
* provider truncation disabled;
* tokenizer auto-truncation disabled;
* exact snapshot binding;
* syntax-aware and line fallback chunking;
* deterministic ranking;
* semantic retrieval as one feature;
* exact and near-duplicate removal;
* source diversity;
* visible deterministic reduction;
* no silent context loss;
* explicit Context Budget Exceeded handling;
* conversation compaction;
* model-generated summary provenance;
* project memory inclusion rules;
* plugin and MCP result boundaries;
* request-plan hashing;
* actual usage reconciliation;
* and adversarial prompt-injection, token-bomb, secret-leakage, stale-snapshot and hidden-context tests.

---

## 3. Context

AI systems are constrained by model context windows, output limits, provider request limits, latency, cost and local hardware.

A model context window is not a permission to send everything.

A large context window does not make every included item relevant.

More context can:

* increase cost;
* increase latency;
* dilute relevant evidence;
* increase prompt-injection surface;
* repeat stale information;
* create contradictions;
* expose unnecessary data;
* and make model behaviour harder to explain.

Different models tokenise the same text differently.

Different providers account for:

* system messages;
* role wrappers;
* tools;
* images;
* PDFs;
* cached content;
* reasoning;
* and provider-added tokens

differently.

Some providers expose preflight token-count endpoints.

Some describe those counts as estimates.

Some provide separate input and output limits.

Some use a combined context window.

Some APIs can automatically truncate earlier content.

Some tokenizer libraries can automatically truncate sequences.

Those behaviours are unacceptable as invisible product policy.

A coding task may need:

* the developer request;
* a selected file;
* symbol definitions;
* call sites;
* current diff;
* build errors;
* project instructions;
* memory;
* and repository metadata.

It rarely needs every source file.

A retrieval score alone cannot establish authority.

An embedding can find semantically similar but wrong or stale code.

A model can request extra files that contain secrets.

An MCP tool result can contain prompt injection.

A plugin can suggest irrelevant context.

A build log can leak credentials.

A conversation can accumulate obsolete decisions.

A context summary can lose a critical constraint.

Opure therefore needs context assembly as a deliberate software-engineering service rather than prompt concatenation.

---

## 4. Problem Statement

Opure requires a context-assembly and token-budgeting architecture that selects only authorised and relevant evidence, preserves provenance, counts the exact model request, reserves enough output capacity, prevents silent truncation, exposes reduction decisions, protects secrets and remains consistent across local models, cloud providers, workflows, plugins and MCP integrations.

---

## 5. Decision Drivers

The decision is evaluated against:

* Charter alignment;
* developer visibility;
* local-first operation;
* provider neutrality;
* model neutrality;
* exact project boundaries;
* token accuracy;
* output usefulness;
* no hidden data;
* no silent truncation;
* source provenance;
* prompt-injection resistance;
* secret protection;
* context quality;
* retrieval quality;
* conversation longevity;
* reproducibility;
* cost;
* latency;
* local memory use;
* workflow determinism;
* testability;
* and small-team implementation.

---

## 6. Governing Principles

This decision must preserve:

* Developer Respect;
* Human in Control;
* Visible by Design;
* Inspectable Decisions;
* Local by Design;
* Cloud Optional;
* Least Privilege;
* No Ambient Context;
* No Hidden Memory;
* No Secret Context;
* No Provider Auto-Truncation;
* No Tokenizer Auto-Truncation;
* No Universal Token Estimate;
* No Trust by Similarity Score;
* No Model-Selected Authority;
* No Project Content as System Policy;
* No Silent Summarisation;
* No Silent Model Switch;
* No Silent Output Reduction;
* No Whole-Repository Context;
* No Hidden Tool Schema;
* No Hidden Provider Count Request;
* Immutable Source Snapshots;
* Deterministic Reduction;
* Provenance Everywhere;
* and Evidence-Based Confidence.

---

## 7. Scope

This ADR decides:

* Context Assembly Service ownership;
* Context Plan schema;
* source capabilities;
* instruction layers;
* candidate generation;
* source classification;
* source-code chunking;
* non-code chunking;
* retrieval;
* ranking;
* deduplication;
* diversity;
* conversation selection;
* project instructions;
* memory selection;
* plugin and MCP content;
* tokeniser profiles;
* token-count methods;
* context-window models;
* output reserves;
* reasoning reserves;
* modality accounting;
* tool-schema accounting;
* reduction;
* summarisation;
* context preview;
* approval;
* request binding;
* caching;
* usage reconciliation;
* Context Receipts;
* recovery;
* Trust Centre;
* and acceptance tests.

This ADR does not decide:

* final embedding model;
* final search engine;
* final code parser for every language;
* final relevance weights;
* final provider model list;
* provider-side caching;
* autonomous context expansion;
* whole-repository ingestion;
* fine-tuned retrieval models;
* cross-project retrieval;
* global personal memory;
* provider conversation state;
* or hidden model reasoning.

---

## 8. Constraints

Known constraints include:

* Models use different tokenizers.
* Tokenizers may change between model generations.
* Chat templates add model-specific tokens.
* Provider message schemas add framing overhead.
* Tool definitions consume context.
* Structured-output schemas consume context.
* Images, audio and documents consume model-specific token equivalents.
* Some token-count endpoints count exact structured requests.
* Some token-count endpoints return estimates.
* A token-count endpoint may itself transmit the complete request.
* Some providers expose separate input and output limits.
* Some models use a combined window.
* Reasoning or thinking can consume output or combined-window capacity.
* Local `llama.cpp` exposes tokenisation and request-count endpoints.
* Provider APIs may expose automatic truncation.
* General tokenizer libraries may expose automatic truncation.
* Workspace files can change between selection and execution.
* Project memory can become stale.
* retrieval indexes can lag the workspace;
* source files can contain prompt injection and secrets;
* and Opure must remain responsive on the reference workstation.

---

## 9. Assumptions

This decision assumes:

* every Model Profile can describe its context accounting;
* every provider adapter can expose a Tokenizer Profile;
* local runtime adapters can count exact request tokens;
* remote count endpoints can be used under the same cloud policy;
* Workspace Service can create immutable file snapshots;
* Search can return source-bound candidates;
* Memory Service can return provenance and confidence;
* secret scanning can run before final assembly;
* tool and schema definitions are available before token counting;
* provider automatic truncation can be disabled or detected;
* Context Plans can remain server-side authoritative;
* and actual response usage can be reconciled with the preflight plan.

---

## 10. Current Technical Evidence

Official provider and runtime documentation available on 18 July 2026 establishes that:

* OpenAI's Responses API supports an input-token count resource and exposes a truncation policy in which automatic truncation can drop items from the beginning, while disabled truncation fails an oversized request;
* Anthropic provides a token-count endpoint that accepts structured messages, system prompts, tools, images and PDFs;
* Anthropic documents token counts as estimates that may differ slightly from actual input use;
* Anthropic documents tokenizer changes between model generations and advises recounting against the exact target model;
* Gemini provides a `countTokens` operation that runs the selected model's tokenizer over text and multimodal request content;
* Gemini exposes model-specific input and output token limits;
* Gemini usage metadata distinguishes input, output, thought, cache and tool-use tokens in current APIs;
* `llama-server` provides tokenisation, request-input-token counting and chat-template application endpoints for the loaded model;
* Hugging Face tokenizers expose automatic truncation strategies;
* and long-context provider guidance acknowledges that larger windows still carry latency, cost and retrieval-quality trade-offs.

These behaviours support a model-specific counting and explicit-reduction architecture.

They do not support a universal fixed characters-per-token estimate.

Every provider endpoint, model metadata field, tokenizer package and runtime endpoint must be revalidated before acceptance.

---

## 11. Terminology

### 11.1 Context Source

A trusted service capable of proposing bounded source references.

---

### 11.2 Context Candidate

A source reference considered for possible inclusion.

It is not yet admitted.

---

### 11.3 Context Item

An immutable admitted item with content, provenance, classification and token accounting.

---

### 11.4 Context Plan

The canonical ordered set of instructions, messages, schemas and context items proposed for one model request.

---

### 11.5 Context Policy

A versioned rule set for one task or workflow defining sources, priorities, budgets and reduction rules.

---

### 11.6 Tokenizer Profile

A versioned description of how one exact model and request format should be counted.

---

### 11.7 Count Method

The mechanism that produced a token count.

---

### 11.8 Exact Count

A count produced by the target tokenizer over the exact final request representation.

---

### 11.9 Estimated Count

A model-specific approximation that may differ from provider billing or admission.

---

### 11.10 Mandatory Context

Content without which the request must not proceed.

---

### 11.11 Optional Context

Content that may be removed under the declared reduction policy.

---

### 11.12 Output Reserve

Capacity protected for visible model output.

---

### 11.13 Reasoning Reserve

Capacity or provider budget protected for model-internal reasoning or thinking where applicable.

---

### 11.14 Safety Reserve

Unused capacity retained to absorb count uncertainty, framing drift and provider-added overhead.

---

### 11.15 Chunk

A bounded source-derived unit with stable provenance.

---

### 11.16 Retrieval Generation

The exact search-index and workspace generation used to produce candidates.

---

### 11.17 Derived Context

Context generated from other context, such as a summary.

---

### 11.18 Reduction

A visible deterministic change that decreases planned input.

---

### 11.19 Omission

A candidate not admitted to the final plan.

---

### 11.20 Context Receipt

The durable evidence of planned, sent, omitted and actually used context.

---

## 12. Options Considered

The principal architecture options are:

1. Trusted Context Assembly Service.
2. AI Router concatenates context.
3. Model chooses its own context iteratively.
4. Retrieval framework owns prompt construction.
5. Provider SDK conversation state.
6. Whole-repository long context.
7. Fixed percentage budgeting.
8. Character-count estimation.
9. Provider auto-truncation.
10. Conversation FIFO truncation.

---

## 13. Option A — Trusted Context Assembly Service

### 13.1 Advantages

* one authoritative plan;
* exact provenance;
* provider neutrality;
* model-specific budgeting;
* deterministic reduction;
* inspectable retrieval;
* secret filtering;
* policy integration;
* workflow reproducibility;
* and testability.

### 13.2 Disadvantages

* substantial implementation;
* model-specific tokenizer maintenance;
* indexing and parser complexity;
* preflight latency;
* and more product UI.

### 13.3 Decision

Selected.

---

## 14. AI Router Concatenation

Rejected because provider execution and project evidence selection are separate responsibilities.

---

## 15. Model-Directed Context Expansion

Rejected as an authority model.

A model may suggest another file or symbol.

A trusted service and the developer or policy decide whether to include it.

---

## 16. Retrieval Framework Prompt Ownership

Rejected because a third-party retrieval framework should not become authoritative over project access, instructions, secrets or provider transmission.

---

## 17. Provider Conversation State

Rejected initially because it creates hidden context and another system of record.

---

## 18. Whole-Repository Long Context

Rejected because a large context window does not justify unnecessary data transmission, cost or prompt-injection surface.

---

## 19. Fixed Percentage Budgeting

Rejected as the only approach because tasks need different output, tool and context allocations.

Percentages may serve as defaults within a task-specific policy.

---

## 20. Character-Count Estimation

Rejected for final admission because tokenization differs materially by model, language, code, Unicode and request framing.

---

## 21. Provider Auto-Truncation

Rejected because dropping earlier context invisibly violates inspectability.

---

## 22. Conversation FIFO Truncation

Rejected as a universal policy because old constraints can remain essential while recent turns may be incidental.

---

## 23. Decision

Opure will provisionally adopt:

> **A trusted Context Assembly Service that constructs immutable model-specific Context Plans from authorised snapshot-bound sources, ranks and reduces them through inspectable deterministic policy, counts the exact fully serialised request with the target tokenizer or the strongest available model-specific method, reserves output and safety capacity, disables automatic truncation and produces a Context Receipt for every inference operation.**

This decision is:

* [ ] Permanent until superseded
* [x] Provisional pending tokenizer, retrieval and reduction evidence
* [ ] Approval of autonomous context expansion
* [ ] Approval of provider-side context caching
* [ ] Approval of whole-repository long-context plans
* [ ] Approval of cross-project context

---

# 24. Service Ownership

The Context Assembly Service owns:

* Context Policy interpretation;
* source-capability validation;
* candidate registration;
* source classification;
* chunk selection;
* provenance;
* relevance scoring;
* deduplication;
* diversity;
* budget allocation;
* token-count orchestration;
* deterministic reduction;
* Context Plan construction;
* plan hashing;
* context preview;
* Context Receipt creation;
* and actual-usage reconciliation.

It does not own:

* source files;
* repository state;
* project memory;
* provider credentials;
* model execution;
* provider trust;
* patch application;
* tool execution;
* plugin permissions;
* MCP permissions;
* or user authentication.

---

# 25. Service Boundary

Conceptual flow:

```text
Desktop or Workflow
    ↓
Runtime Command
    ↓
Context Assembly Service
    ├── Project Policy
    ├── Workspace
    ├── Repository
    ├── Search
    ├── Memory
    ├── Build and Test
    ├── Plugin Gateway
    ├── MCP Gateway
    ├── Secret Scanner
    └── Tokenizer Registry
    ↓
Context Plan
    ↓
AI Router
    ↓
Provider or Local Runtime
```

---

## 25.1 No Direct Source Read by AI Router

AI Router receives admitted Context Items.

It does not traverse the workspace.

---

## 25.2 No Prompt String as Primary Contract

The internal contract is structured.

A provider adapter may serialise it into a prompt or message body at the final stage.

---

## 25.3 Structured Plan

A plan should preserve:

* instruction layer;
* message role;
* source item;
* ordering;
* trust;
* classification;
* and count.

---

# 26. Context Policy

A Context Policy is versioned and task specific.

Suggested schema:

```text
opure.context-policy/1
```

---

## 26.1 Policy Fields

```text
policy_id
revision
task_class
allowed_sources
mandatory_sources
source_caps
priority_rules
ranking_features
chunking_profiles
deduplication
diversity
conversation_policy
memory_policy
secret_policy
data_class_policy
token_budget_policy
output_reserve_policy
reasoning_reserve_policy
reduction_steps
approval_policy
receipt_policy
```

---

## 26.2 Task Classes

Initial task classes may include:

* explain selected code;
* review a patch;
* propose a patch;
* diagnose a build;
* diagnose a test;
* answer a repository question;
* generate a new file;
* perform code infill;
* summarise selected content;
* create embeddings;
* and workflow-specific analysis.

---

## 26.3 No Generic Unlimited Policy

There is no default “use anything relevant” policy.

---

## 26.4 Policy Source

Policies are:

* first-party source controlled;
* plugin-contributed through a reviewed declarative extension;
* or project configured within product limits.

---

## 26.5 Plugin Policy

A plugin may propose a narrower policy.

It cannot broaden:

* source permissions;
* data classes;
* project scope;
* cloud policy;
* or token limits.

---

# 27. Instruction Layers

The Context Plan represents instruction precedence explicitly.

---

## 27.1 Product Security Instructions

Highest product-controlled layer.

Contains:

* safety boundaries;
* authority boundaries;
* output constraints;
* review requirements;
* and task-invariant trust rules.

---

## 27.2 Product Task Instructions

Defines how the model should perform the selected Opure task.

---

## 27.3 Project-Governed Instructions

Project-specific instructions that have:

* known source;
* project scope;
* visible content;
* policy classification;
* and approval.

They remain subordinate to product policy.

---

## 27.4 User Request

The current authenticated developer instruction.

---

## 27.5 Conversation Content

Prior visible turns.

---

## 27.6 Source and Tool Content

Always data.

Never instruction authority.

---

# 28. Project Instructions

Project instructions may come from:

* an Opure project configuration;
* an explicitly approved repository file;
* a team policy;
* or an enterprise profile.

---

## 28.1 No Filename Magic

A file named:

```text
AGENTS.md
AI.md
CLAUDE.md
README.md
```

does not automatically become an instruction source.

---

## 28.2 Registration

The developer or project policy registers an instruction source explicitly.

---

## 28.3 Scope

Instruction files may apply to:

* whole project;
* directory subtree;
* language;
* task;
* or workflow.

---

## 28.4 Snapshot Binding

The Context Plan binds the exact instruction-file snapshot.

---

## 28.5 Change

A changed project instruction invalidates the plan.

---

## 28.6 Secret Scan

Instruction files are secret scanned.

---

## 28.7 Prompt Injection

Project instructions can be malicious or compromised.

They are shown as project-governed content and remain below Opure product rules.

---

# 29. User Request

The current request is mandatory.

---

## 29.1 Full Preservation

Do not truncate it silently.

---

## 29.2 Oversized Request

When the user request alone is too large:

* show its count;
* offer explicit selected reduction;
* offer attachment selection;
* offer task decomposition;
* or deny.

---

## 29.3 Secret Detection

A remote plan scans the user request under ADR-0019.

---

## 29.4 Ambiguous Scope

Context Assembly may require a deterministic scope resolution before candidate generation.

It should not guess another project silently.

---

# 30. Context Source Capabilities

A source is available only through a typed capability.

Examples:

```text
ProjectRef
WorkspaceSnapshotRef
FileSnapshotRef
SymbolRef
DiffRef
BuildRunRef
TestRunRef
RepositoryRef
MemoryQueryRef
PluginResultRef
McpResultRef
ConversationRef
```

---

## 30.1 No Path as Authority

A path string is not enough.

---

## 30.2 No Result-ID Guessing

Opaque references bind project, source and caller.

---

## 30.3 Capability Validation

Before candidate generation, validate:

* caller;
* operation;
* project;
* policy;
* source;
* expiry;
* and generation.

---

# 31. Candidate Record

A Context Candidate should contain:

```text
candidate_id
source_type
source_reference
project_id
workspace_generation
retrieval_generation
content_hash
display_label
relative_path
range
symbol
data_class
trust_class
freshness
explicitness
mandatory
estimated_bytes
candidate_reason
source_score_features
```

Content need not be materialised until classification or admission.

---

## 31.1 Candidate ID

The candidate ID is opaque.

---

## 31.2 Mandatory Candidate

A mandatory candidate cannot be removed by ordinary reduction.

If it does not fit, the operation fails or is replanned.

---

## 31.3 Explicit Candidate

Explicit user-selected candidates receive high priority but remain subject to:

* project boundary;
* secret policy;
* provider policy;
* and hard size limits.

---

# 32. Candidate Sources

Initial sources include:

1. current request;
2. explicit file selections;
3. explicit text selections;
4. current diff;
5. current patch proposal;
6. build diagnostics;
7. test diagnostics;
8. current repository state;
9. selected conversation;
10. project instructions;
11. exact symbol lookup;
12. lexical search;
13. dependency graph;
14. call and reference graph;
15. semantic search;
16. project memory;
17. approved plugin results;
18. approved MCP results;
19. prior model outputs selected by the developer;
20. and approved generated summaries.

---

# 33. Prohibited Ambient Sources

The initial implementation must not draw automatically from:

* clipboard;
* arbitrary open windows;
* browser history;
* terminal history;
* environment;
* user Documents;
* unrelated repositories;
* other projects;
* email;
* calendar;
* global memory;
* cloud drive;
* provider conversation;
* plugin private data;
* MCP server private state;
* or local model cache.

---

# 34. Source Trust Classes

Suggested classes:

* Product Trusted;
* User Direct;
* Project Governed;
* Project Source;
* Deterministic Tool Evidence;
* Local Model Generated;
* Remote Model Generated;
* Plugin Supplied;
* MCP Supplied;
* External Untrusted;
* and Prohibited.

---

## 34.1 Trust Does Not Equal Relevance

A trusted item may still be irrelevant.

---

## 34.2 Relevance Does Not Equal Trust

A highly similar MCP result remains untrusted.

---

# 35. Data Classification

ADR-0019 data classes apply.

Context Assembly may add source-specific classes such as:

* project instruction;
* source code;
* configuration;
* generated source;
* third-party source;
* build log;
* test log;
* repository history;
* patch;
* memory;
* plugin result;
* MCP resource;
* and external document.

---

## 35.1 Most Restrictive Wins

When classifications conflict, use the most restrictive material class.

---

## 35.2 Classification Evidence

Record:

* classifier;
* ruleset;
* time;
* and confidence.

---

# 36. Secret and Protected Content

Secret scanning occurs before remote admission and may occur earlier for all plans.

---

## 36.1 Hard Exclusion

Never admit:

* Vault values;
* private keys;
* credential files;
* signing material;
* recovery keys;
* browser credential stores;
* protected security evidence;
* and other ADR-0019 prohibited roots.

---

## 36.2 Likely Secret

For remote context, deny initially.

For local context:

* allow only according to explicit local policy;
* preserve classification;
* avoid diagnostics;
* and warn that the local process can see the content.

---

## 36.3 Redaction

A redacted item receives:

* new content hash;
* redaction map stored securely;
* source provenance;
* and explicit `redacted` status.

The model never receives the redaction map.

---

# 37. Workspace Snapshots

Every project file item comes from Workspace Service.

---

## 37.1 Snapshot Fields

```text
project_id
workspace_generation
relative_path
file_identity
content_hash
size
encoding
line_endings
classification
created_at
```

---

## 37.2 No Live File Stream

Do not stream from a mutable file path after approval.

---

## 37.3 Changed File

A changed content hash invalidates the candidate or plan.

---

## 37.4 Deleted File

A deleted source remains available only through an already materialised immutable operation snapshot where policy permits.

---

# 38. Text Selections

An explicit selection binds:

* file snapshot;
* start and end offsets;
* start and end lines;
* selected content hash;
* surrounding-context policy;
* and user action.

---

## 38.1 Selection Priority

The exact selected range is mandatory unless prohibited.

---

## 38.2 Surrounding Context

Neighbouring lines are optional and budgeted separately.

---

# 39. Current File

An active editor file is not ambient context.

It becomes context only when:

* selected by the user;
* required by the invoked command;
* or allowed by a visible task policy.

---

# 40. Diff Context

Diff context includes:

* base revision;
* target revision or workspace generation;
* changed files;
* hunks;
* base hashes;
* and patch provenance.

---

## 40.1 Priority

Changed hunks are higher priority than unrelated full files for patch review.

---

## 40.2 Surrounding Lines

Include bounded surrounding context.

---

## 40.3 Full File

Include full file only when:

* explicit;
* small;
* or required for structural understanding.

---

## 40.4 Binary Diff

Binary changes are metadata only unless a modality-specific policy exists.

---

# 41. Patch Proposal Context

A model-produced patch proposal is untrusted derived context.

---

## 41.1 Review Task

For patch review, include:

* patch operations;
* base hashes;
* changed hunks;
* validator results;
* and relevant source snapshots.

---

## 41.2 No Applied-State Assumption

Do not treat a proposed patch as applied workspace state.

---

# 42. Build Diagnostics

Build Service should provide structured diagnostics.

---

## 42.1 Diagnostic Candidate

Fields may include:

* code;
* severity;
* message;
* project;
* target;
* relative file;
* line and column;
* tool;
* command template;
* run ID;
* and safe output excerpt.

---

## 42.2 Error-Centred Context

Prefer:

* exact diagnostic;
* referenced source range;
* relevant project configuration;
* and bounded neighbouring output.

---

## 42.3 Repeated Errors

Deduplicate identical or cascading diagnostics.

---

## 42.4 Build Log

Do not include the entire build log automatically.

---

# 43. Test Diagnostics

Test Service should provide:

* test identity;
* framework;
* outcome;
* exception;
* stack frames;
* assertion diff;
* source references;
* captured output;
* and run ID.

---

## 43.1 Stack Trace

Prioritise project frames.

---

## 43.2 Repeated Failures

Cluster equivalent failures.

---

## 43.3 Captured Output

Secret scan and bound.

---

# 44. Repository Context

Repository Service may provide:

* branch;
* status;
* staged and unstaged paths;
* selected history;
* selected commit;
* blame metadata;
* and changed-path graph.

---

## 44.1 No Credentials

Never include repository credentials.

---

## 44.2 Remote URL

Repository remote URL is metadata and may reveal organisation or project names.

Classify it.

---

## 44.3 History Limit

Do not include long commit history automatically.

---

# 45. Symbol Context

A Symbol Reference should bind:

* language;
* parser;
* file snapshot;
* symbol kind;
* fully qualified identity;
* declaration range;
* body range;
* and index generation.

---

## 45.1 Exact Mention

A symbol named in the request receives a strong deterministic feature.

---

## 45.2 Ambiguity

Multiple matching symbols remain separate candidates.

Do not silently choose one solely by model suggestion.

---

## 45.3 Declaration and Uses

Policy may include:

* declaration;
* direct callers;
* direct callees;
* implementations;
* tests;
* and configuration references.

Each is separately budgeted.

---

# 46. Dependency Context

Dependency context may include:

* project references;
* package references;
* imports;
* inheritance;
* interface implementation;
* call graph;
* and data flow

where trusted analysis exists.

---

## 46.1 Graph Distance

Closer graph distance increases relevance.

---

## 46.2 Graph Staleness

A stale graph is penalised or rejected.

---

## 46.3 Generated Analysis

An AI-generated dependency claim is not a graph edge until verified.

---

# 47. Search

Search sources include:

* exact path;
* exact phrase;
* symbol;
* lexical;
* regex under limits;
* structural;
* and semantic.

---

## 47.1 Search Query

The query is stored in the plan or candidate evidence.

---

## 47.2 Search Limits

Bound:

* files;
* matches;
* bytes;
* duration;
* regex complexity;
* and result snippets.

---

## 47.3 Search Does Not Grant Read

A search result may expose a bounded excerpt.

Full-file context requires the corresponding Workspace capability.

---

# 48. Semantic Retrieval

Semantic retrieval uses project-scoped embeddings.

---

## 48.1 Embedding Identity

Bind:

* embedding model;
* dimensions;
* normalisation;
* chunker;
* source hash;
* and index generation.

---

## 48.2 Stale Embedding

A changed source hash invalidates the vector.

---

## 48.3 Similarity Is Advisory

Similarity contributes to rank.

It does not:

* grant authority;
* override an explicit file;
* override a secret classification;
* or prove correctness.

---

## 48.4 Thresholds

Thresholds are task and model specific.

---

## 48.5 Local Default

Local embeddings are preferred for local-first indexing.

---

## 48.6 Remote Embeddings

Remote embedding is a separate ADR-0019 Data Sharing Plan.

---

# 49. Hybrid Retrieval

Hybrid retrieval should combine:

* exact matches;
* BM25 or equivalent lexical score;
* semantic similarity;
* symbol graph;
* dependency graph;
* recency;
* diff relevance;
* diagnostic relevance;
* and memory confidence.

---

## 49.1 Fusion

Use an inspectable fusion method such as:

* weighted normalised score;
* reciprocal rank fusion;
* or another deterministic method.

---

## 49.2 Versioning

Ranking formula and weights are versioned.

---

## 49.3 Training

No opaque learned reranker is authoritative initially.

---

# 50. Ranking Feature Record

For each candidate, record applicable features:

```text
explicit_selection
exact_path_match
exact_symbol_match
current_diff
diagnostic_reference
test_reference
graph_distance
lexical_score
semantic_score
recency
memory_confidence
source_trust
generated_penalty
duplicate_penalty
diversity_gain
size_cost
```

---

## 50.1 Feature Normalisation

Normalisation is deterministic and versioned.

---

## 50.2 Missing Feature

Missing evidence contributes no positive score.

---

## 50.3 Negative Features

A candidate may be penalised for:

* generated source;
* stale index;
* external untrusted origin;
* repetition;
* excessive size;
* low confidence;
* or weak relationship.

---

# 51. Priority Classes

Suggested candidate priority:

* **P0 — Mandatory**
* **P1 — Explicit**
* **P2 — Direct Evidence**
* **P3 — Strong Supporting**
* **P4 — Optional Supporting**
* **P5 — Opportunistic**
* **PX — Ineligible**

---

## 51.1 P0

Cannot be removed by ordinary reduction.

---

## 51.2 P1

Explicitly chosen by developer.

Removal requires visible replanning.

---

## 51.3 P2

Directly referenced by operation evidence.

---

## 51.4 P3

High-confidence retrieval or graph context.

---

## 51.5 P4

Useful but replaceable.

---

## 51.6 P5

Examples, broad metadata or weak retrieval.

---

## 51.7 PX

Denied by policy or invalid.

---

# 52. Ranking Formula

A conceptual deterministic formula may be:

```text
base_relevance
+ explicitness
+ direct_evidence
+ graph_relationship
+ lexical_relevance
+ semantic_relevance
+ freshness
+ trust_adjustment
+ diversity_gain
- size_cost
- redundancy
- generated_penalty
- staleness_penalty
```

The exact formula is deferred to implementation evidence.

---

## 52.1 No Hidden Model Ranker

A model may not secretly replace the formula.

---

## 52.2 Optional Learned Reranker

A future local reranker may propose ordering.

The deterministic policy still:

* enforces eligibility;
* preserves mandatory items;
* applies budgets;
* and records the reranker identity.

---

# 53. Diversity

The plan should avoid monopolisation by one:

* file;
* directory;
* source type;
* retrieval method;
* or repeated error.

---

## 53.1 Per-File Cap

Task policies may define a maximum number of optional chunks per file.

---

## 53.2 Per-Source Cap

Memory or MCP results cannot fill the entire optional budget.

---

## 53.3 Mandatory Exception

Mandatory and explicit items are not removed merely to satisfy diversity.

---

# 54. Exact Deduplication

Exact duplicate content is included once unless distinct source provenance is itself relevant.

---

## 54.1 Hash

Use content hash after canonical context rendering.

---

## 54.2 Multiple Provenance

One Context Item may retain multiple source references.

---

## 54.3 Line Endings

Normalised duplicate detection may recognise semantically identical text with different line endings while preserving original provenance.

---

# 55. Near-Duplicate Detection

Near duplicates may arise from:

* generated copies;
* vendored source;
* repeated logs;
* duplicated tests;
* memory summaries;
* and overlapping chunks.

---

## 55.1 Techniques

Use bounded combinations of:

* token shingles;
* MinHash;
* locality-sensitive hashing;
* syntax identity;
* and embedding similarity.

---

## 55.2 False Merge Risk

Near-duplicate removal must not merge code whose small difference is the subject of the task.

---

## 55.3 Diff-Aware Protection

Changed files and comparison tasks disable aggressive near-duplicate collapse across the compared versions.

---

# 56. Chunking Architecture

Chunking is a trusted deterministic service.

---

## 56.1 Chunker Profile

A profile contains:

```text
chunker_id
version
content_kind
parser
parser_version
target_tokens
maximum_tokens
minimum_tokens
overlap_tokens
header_policy
symbol_policy
normalisation
fallback
```

---

## 56.2 Token-Aware Target

Chunk targets use the target Tokenizer Profile where available.

A general structural pass may precede model-specific subdivision.

---

## 56.3 Stable Identity

A Chunk ID is stable only for the same:

* source snapshot;
* range;
* chunker;
* parser;
* normalisation;
* and rendering.

---

## 56.4 No Cross-File Chunk

A source-code chunk does not combine unrelated files.

---

# 57. Source Code Chunking

Preferred hierarchy:

1. complete small file;
2. namespace or module;
3. type;
4. member;
5. logical block;
6. line-stable bounded window.

---

## 57.1 Complete Symbol

Prefer a complete declaration within the maximum.

---

## 57.2 Oversized Symbol

For an oversized symbol:

* include signature;
* include leading documentation;
* split by statement or block where parser supports it;
* preserve line ranges;
* and add bounded overlap.

---

## 57.3 Imports and Using Directives

Include only when:

* required to understand names;
* task policy requires;
* or the chunk is the file header.

Do not repeat a huge import block with every chunk.

---

## 57.4 Type Header

Member chunks may include a small type-header prefix.

---

## 57.5 Namespace

Preserve safe namespace or module identity.

---

## 57.6 Comments

Comments are source content and may contain prompt injection.

They remain data.

---

## 57.7 Generated Files

Generated source receives a penalty and may be excluded by default.

---

# 58. Language Parsers

Version 1 should prioritise parsers for the languages targeted by first vertical slices.

---

## 58.1 Trusted Parser

A parser runs under bounded resource limits.

---

## 58.2 Parse Failure

Fall back to line-stable chunks.

---

## 58.3 No Build Execution

Chunking does not execute project code, macros, source generators or compiler plugins.

---

# 59. Line-Stable Fallback

Fallback chunks use:

* complete lines;
* bounded byte and token sizes;
* deterministic overlap;
* and exact ranges.

---

## 59.1 Long Line

A single extremely long line may be split at safe Unicode boundaries and clearly marked.

---

## 59.2 Minified File

Minified or generated content may be excluded or handled through a specialised policy.

---

# 60. Configuration Chunking

Configuration requires structure-aware boundaries.

---

## 60.1 JSON

Prefer complete objects or arrays under bounded depth.

---

## 60.2 YAML

Prefer document and mapping boundaries.

Reject unsafe tags in parsing.

---

## 60.3 XML

Prefer elements and preserve namespace context.

External entities are disabled.

---

## 60.4 TOML and INI

Prefer table or section boundaries.

---

## 60.5 Secrets

Configuration receives heightened secret scanning.

---

# 61. Markdown Chunking

Prefer:

* heading sections;
* lists;
* code fences;
* tables;
* and paragraphs.

---

## 61.1 Code Fence

Do not split a small code fence.

---

## 61.2 Active Links

Links remain untrusted content.

---

## 61.3 Front Matter

Parse safely and preserve classification.

---

# 62. Log Chunking

Logs are centred around:

* error;
* warning;
* operation boundary;
* timestamp range;
* correlation ID;
* and stack trace.

---

## 62.1 Repetition

Collapse repeated lines with a count.

---

## 62.2 Secrets

Redact before context.

---

## 62.3 Ordering

Preserve chronological order inside a chunk.

---

# 63. Diff Chunking

A diff chunk contains:

* file identity;
* base and target hashes;
* hunk header;
* changed lines;
* and bounded unchanged context.

---

## 63.1 Hunk Priority

Every changed hunk is a direct-evidence candidate.

---

## 63.2 Large Diff

Large diffs are grouped by file and symbol.

---

## 63.3 Rename

Preserve old and new paths.

---

# 64. Conversation Chunking

Conversation is not chunked by arbitrary token windows first.

It is structured by turn and decision.

---

## 64.1 Turn Record

Contains:

* role;
* visible content;
* time;
* operation;
* source;
* and relevance.

---

## 64.2 Current Request

Never split or omit silently.

---

## 64.3 Assistant Response

Only user-visible assistant content may be reused.

---

## 64.4 Hidden Reasoning

Never included.

---

# 65. Conversation Relevance

Features include:

* current topic;
* explicit reference to earlier turn;
* unresolved constraint;
* accepted decision;
* project identity;
* artefact identity;
* and recency.

---

## 65.1 Recency Is Not Enough

An older founder decision may outrank a recent aside.

---

## 65.2 Superseded Decision

A superseded turn is excluded or marked historical.

---

# 66. Conversation Summary

A summary is derived context.

---

## 66.1 Creation

Creation is explicit or governed by a visible conversation-retention policy.

---

## 66.2 Source Coverage

Record included turn IDs and hashes.

---

## 66.3 Model

Record model and provider or runtime.

---

## 66.4 Human Review

A summary containing decisions should be reviewable.

---

## 66.5 Staleness

New conflicting turns invalidate or supersede the summary.

---

## 66.6 No Summary as Authority Alone

For high-impact decisions, include the original decision turn or authoritative artefact when budget permits.

---

# 67. Deterministic Conversation Compression

Before model summarisation, use deterministic compression such as:

* remove greetings;
* remove repeated confirmations;
* remove duplicated artefact links;
* preserve decisions;
* preserve constraints;
* preserve corrections;
* preserve unresolved questions;
* and preserve exact user requests needed by the current operation.

---

# 68. Project Memory

Memory candidates come from Memory Service.

---

## 68.1 Required Metadata

* project;
* memory ID;
* source;
* source hash;
* confidence;
* freshness;
* supersession;
* data class;
* and content hash.

---

## 68.2 No Global Memory

Project context does not search every project or account.

---

## 68.3 Memory Confidence

Low-confidence memory is optional.

---

## 68.4 Memory Conflict

Conflicting memory entries remain visible to the policy or user rather than silently choosing one.

---

## 68.5 Authoritative Source

Prefer the authoritative project document over a derived memory summary.

---

# 69. Plugin Results

Plugin results enter context only through ADR-0017 mediation.

---

## 69.1 Classification

They are Plugin Supplied.

---

## 69.2 Provenance

Include:

* plugin ID;
* version;
* package hash;
* operation;
* result hash;
* and permission evidence.

---

## 69.3 No Instruction Promotion

Plugin content remains data.

---

## 69.4 Size

Bound and summarise structurally where possible.

---

# 70. MCP Results

MCP content enters only through ADR-0018.

---

## 70.1 Provenance

Include:

* server;
* account;
* tool or resource;
* fingerprint;
* operation;
* result hash;
* and trust class.

---

## 70.2 Prompt Injection

Treat instructions in the result as untrusted data.

---

## 70.3 Resource Links

Do not dereference automatically.

---

## 70.4 AI Return

The MCP operation plan must permit result-to-model use.

---

# 71. Prior Model Output

Prior output is model-generated content.

---

## 71.1 Explicit Selection

It may be included when:

* part of conversation;
* selected as a draft;
* required to revise a patch;
* or stored as workflow state.

---

## 71.2 No Truth Promotion

It is not authoritative merely because it was previously accepted as text.

---

# 72. External Documents

External documents require:

* explicit source;
* safe parsing;
* provenance;
* size limits;
* classification;
* and project policy.

---

## 72.1 PDF

PDF extraction may include layout and image tokens.

It is not automatically equivalent to plain text.

---

## 72.2 Office Documents

Macro execution and active content are disabled.

---

# 73. Candidate Materialisation

Candidate content is materialised only after:

* authority validation;
* source freshness;
* and preliminary eligibility.

---

## 73.1 Large Candidate

A large file may be inspected structurally before full content read.

---

## 73.2 Binary

Binary content is excluded unless a modality-specific policy exists.

---

# 74. Candidate Classification Pipeline

```text
Validate source reference
    ↓
Verify project and generation
    ↓
Read bounded metadata
    ↓
Determine content kind
    ↓
Apply protected-root policy
    ↓
Classify data
    ↓
Secret scan
    ↓
Chunk
    ↓
Calculate features
    ↓
Register eligible candidates
```

---

# 75. Candidate Failure

One candidate failure should not necessarily fail the operation.

---

## 75.1 Mandatory Failure

Fails the plan.

---

## 75.2 Optional Failure

Record omission reason and continue.

---

## 75.3 Security Failure

May block the whole plan if the candidate indicates a scope or compromise problem.

---

# 76. Omission Reasons

Stable reasons include:

* Not Authorised;
* Wrong Project;
* Stale Reference;
* Source Changed;
* Source Missing;
* Protected Content;
* Secret Detected;
* Data Class Denied;
* Binary Unsupported;
* Parse Failed;
* Too Large;
* Duplicate;
* Redundant;
* Low Relevance;
* Diversity Limit;
* Source Cap;
* Token Budget;
* Policy Denied;
* User Excluded;
* Superseded;
* and Invalid.

---

# 77. Admission

Admission occurs after ranking and token budgeting.

---

## 77.1 Mandatory First

Admit all mandatory items.

---

## 77.2 Explicit Next

Admit explicit items if policy allows.

---

## 77.3 Direct Evidence

Admit high-value operation evidence.

---

## 77.4 Supporting Items

Admit according to marginal value per token and diversity.

---

## 77.5 Token Cost

Use exact item cost where available.

---

# 78. Marginal Value per Token

A candidate may be considered by:

```text
effective_value / marginal_token_cost
```

subject to priority and diversity.

---

## 78.1 Not for Mandatory Items

Mandatory and explicit items are not reduced to a pure ratio.

---

## 78.2 Tiny Snippet Bias

Prevent the algorithm from selecting many tiny low-value snippets merely because their ratio is high.

---

# 79. Ordering

Final ordering is task and provider aware.

---

## 79.1 Default Logical Order

1. trusted instructions;
2. project instructions;
3. conversation;
4. task statement;
5. primary explicit evidence;
6. supporting evidence;
7. output schema or final response instruction as provider format requires.

---

## 79.2 Query Placement

Some model guidance recommends placing the query after long context.

The Model Profile may specify a tested ordering template.

---

## 79.3 Provider Adapter

Provider-specific role and message mapping is part of Tokenizer Profile and instruction template.

---

# 80. Context Delimiters

Context items use stable visible delimiters and metadata.

---

## 80.1 Not a Security Boundary

Delimiters help interpretation.

They do not prevent prompt injection.

---

## 80.2 Item Header

A rendered item may include:

```text
source type
relative path
line range
content hash prefix
trust class
```

within a bounded format.

---

## 80.3 Path Privacy

Remote plans may omit absolute paths and use project-relative paths.

---

# 81. Content Rendering

Each content kind has a canonical renderer.

---

## 81.1 Source Code

Preserve exact text unless redacted.

---

## 81.2 Logs

Use deterministic repetition compression and redaction.

---

## 81.3 Structured Data

Use canonical or source-preserving form according to task.

---

## 81.4 Diff

Use standard bounded unified diff.

---

## 81.5 Memory

Label as memory and include provenance.

---

## 81.6 Untrusted External Content

Label clearly.

---

# 82. Rendering Version

Rendering affects tokens.

It is versioned and included in:

* Context Plan;
* Tokenizer Profile;
* token-count cache;
* and receipt.

---

# 83. Context Plan Schema

Suggested schema:

```text
opure.context-plan/1
```

---

## 83.1 Plan Fields

```text
plan_id
schema_version
operation_id
project_id
workspace_generation
retrieval_generation
context_policy
instruction_template
provider_profile
model_profile
tokenizer_profile
task_class
messages
context_items
tool_definitions
output_schema
budget
counts
reductions
omissions
data_classes
secret_scan
policy_decision
approval
created_at
expires_at
plan_hash
```

---

## 83.2 Immutable

A material change creates a new plan.

---

## 83.3 Plan Hash

SHA-256 over canonical authoritative fields.

---

## 83.4 Display Metadata

Non-authoritative display text may be excluded from the hash only when it cannot alter the request.

---

# 84. Plan Expiry

Plan expiry depends on:

* workspace volatility;
* provider;
* approval;
* task;
* and source type.

Suggested interactive default:

```text
5 minutes
```

---

## 84.1 Early Invalidation

Invalidate on:

* file change;
* project close;
* policy change;
* model change;
* tokenizer change;
* provider profile change;
* tool schema change;
* instruction change;
* or secret-classification change.

---

# 85. Plan State Machine

States include:

* Draft;
* Gathering;
* Classifying;
* Ranking;
* Counting;
* Reducing;
* Approval Required;
* Ready;
* Executing;
* Completed;
* Expired;
* Invalidated;
* Denied;
* Failed;
* and Cancelled.

---

## 85.1 No Execution from Draft

Only Ready plans execute.

---

# 86. Context Preview

The Desktop should show:

* model;
* provider or local runtime;
* total limit;
* planned input;
* output reserve;
* safety reserve;
* items by source;
* files and ranges;
* conversation turns;
* memory records;
* plugin and MCP results;
* tool-schema cost;
* omitted items;
* reduction steps;
* secret findings;
* cloud data classes;
* and cost estimate.

---

## 86.1 Mandatory and Optional

Clearly distinguish.

---

## 86.2 Exact Counts

Show exact or estimated status.

---

## 86.3 Expand

Allow expansion to item detail.

---

## 86.4 Remove

The user may remove optional items and create a new plan.

---

## 86.5 Add

Adding an item creates a new plan and count.

---

# 87. Tokenizer Profile

A Tokenizer Profile describes exact request accounting.

Suggested schema:

```text
opure.tokenizer-profile/1
```

---

## 87.1 Fields

```text
tokenizer_profile_id
revision
model_profile
provider_profile_revision
tokenizer_kind
tokenizer_identity
tokenizer_version
vocabulary_hash
merge_hash
chat_template_hash
request_renderer
request_schema
count_method
count_endpoint
special_token_policy
modality_policy
tool_policy
reasoning_policy
input_limit
output_limit
combined_limit
count_accuracy
safety_reserve_policy
verified_at
```

---

## 87.2 One Profile per Request Shape

A model may require separate profiles for:

* chat;
* responses;
* completion;
* embedding;
* multimodal;
* tools;
* and structured output.

---

## 87.3 Profile Revision

Changes in any authority-affecting field create a new revision.

---

## 87.4 Provider Alias

A mutable model alias cannot assume a stable tokenizer.

The profile is revalidated when the resolved model changes.

---

# 88. Count Methods

Supported methods:

* Exact Local Runtime;
* Exact Local Tokenizer;
* Provider Structured Count Endpoint;
* Provider Input-Token Endpoint;
* Verified Adapter Count;
* Conservative Estimate;
* and Unknown.

---

## 88.1 Exact Local Runtime

The target loaded model tokenises the exact rendered request.

Preferred for local models.

---

## 88.2 Exact Local Tokenizer

A pinned tokenizer implementation matching the target model.

It must include request framing and special-token rules.

---

## 88.3 Provider Structured Count Endpoint

The provider counts the same structured request fields.

---

## 88.4 Provider Input-Token Endpoint

The provider counts the final provider-native request shape.

---

## 88.5 Verified Adapter Count

Local provider-specific counting whose results have been compared against actual usage.

---

## 88.6 Conservative Estimate

A model-specific estimate with documented error and safety reserve.

---

## 88.7 Unknown

Strict plans fail.

---

# 89. Count Accuracy Classes

Suggested classes:

* Exact;
* Provider Exact;
* Provider Estimate;
* Locally Verified Estimate;
* Conservative Estimate;
* Unknown.

---

## 89.1 Provider Exact

Means the provider describes the endpoint as exact for admission.

It does not mean billing and count can never differ unless documentation and evidence establish that.

---

## 89.2 Reconciliation

Every method is compared with actual usage where the provider returns it.

---

# 90. Fully Serialised Counting

Final admission counts the exact provider or runtime request representation.

---

## 90.1 Why Item Sums Are Insufficient

Independent item counts may omit:

* role wrappers;
* separators;
* template markers;
* JSON structure;
* tool schemas;
* output schema;
* media markers;
* special tokens;
* and provider-added framing.

---

## 90.2 Preliminary Item Counts

Item-level counts remain useful for candidate selection.

---

## 90.3 Final Count

The final request is rendered and counted as one operation.

---

# 91. Token Count Request Privacy

A remote token-count request contains model input.

---

## 91.1 Same Plan

It uses the same Context Plan content.

---

## 91.2 Same Provider

It uses the same Provider Profile revision unless the developer approves a separate counting provider, which is not supported initially.

---

## 91.3 Same Cloud Policy

Project cloud policy applies.

---

## 91.4 Receipt

Record count request:

* endpoint;
* plan hash;
* bytes;
* result;
* and retention posture.

---

## 91.5 No Double Hidden Transmission

The preview states that preflight counting may send the proposed content to the provider before generation.

---

## 91.6 Ask Every Time

The human approves the Data Sharing Plan before remote counting.

The same approval may cover count and inference when content and endpoint are identical and policy permits.

---

# 92. Local Counting

Local token counting does not require cloud consent.

---

## 92.1 Runtime Count

Use the exact loaded model where practical.

---

## 92.2 Pre-Load Count

When the model is not loaded, use:

* pinned local tokenizer;
* a lightweight trusted tokenizer worker;
* or a conservative estimate.

The execution plan records the method.

---

## 92.3 Model Load Solely for Counting

Do not load a very large model solely to count unless the user accepts the resource cost.

---

# 93. `llama.cpp` Counting

For the selected local runtime, the adapter may use:

* `/tokenize`;
* request input-token counting endpoints;
* and approved chat-template application

from the exact pinned runtime.

---

## 93.1 Endpoint Access

Only the trusted adapter calls these endpoints.

---

## 93.2 Template Consistency

Count after the same chat template and request rendering used for inference.

---

## 93.3 BOS and Special Tokens

Record:

* BOS insertion;
* EOS policy;
* special-token parsing;
* and template tokens.

---

## 93.4 Endpoint Drift

Runtime update requires comparison tests.

---

# 94. Provider Count Endpoints

Adapters may support official count endpoints.

---

## 94.1 Anthropic-Like Structured Count

Count:

* system;
* messages;
* tools;
* images;
* documents;
* and provider-specific fields

with the exact target model.

---

## 94.2 Gemini-Like Count

Count:

* text;
* chat;
* inline media;
* tools;
* system instruction;
* and cache-related request content

where applicable.

---

## 94.3 OpenAI-Like Input Count

Count the exact Responses or supported request body.

---

## 94.4 Unsupported Feature

If the count endpoint cannot represent an enabled inference feature, it is not exact for that plan.

---

# 95. Provider Count Differences

The Context Receipt should retain differences between:

* preflight count;
* actual input count;
* billable input count;
* cached input;
* reasoning or thought tokens;
* tool-use tokens;
* and total tokens.

---

## 95.1 No Cross-Provider Comparison Assumption

Fields with the same name can have different semantics.

---

# 96. Tokenizer Change

A tokenizer change invalidates:

* count cache;
* Context Plans;
* workflow estimates;
* pricing estimates;
* and benchmark evidence.

---

## 96.1 Detection

Detect through:

* model snapshot;
* tokenizer files;
* provider documentation;
* count comparison;
* or runtime metadata.

---

# 97. Request Renderer

The renderer converts the structured Context Plan into a provider-native request.

---

## 97.1 Versioned

Renderer version is part of Tokenizer Profile.

---

## 97.2 Deterministic

The same plan and profile produce the same request bytes except explicitly non-authoritative request IDs.

---

## 97.3 No Hidden Text

The renderer cannot add undocumented instructions.

---

## 97.4 Provider Metadata

Provider-required metadata is declared and counted where applicable.

---

# 98. Chat Template

A local chat template is part of Model Profile and Tokenizer Profile.

---

## 98.1 Model-Embedded Template

Treat as model metadata.

---

## 98.2 Opure Override

An override is source controlled and revisioned.

---

## 98.3 Change

Invalidates counts and plans.

---

# 99. Special Tokens

Special-token treatment is explicit.

---

## 99.1 User Content

User and source content cannot inject raw special tokens unless the model and policy explicitly permit parsing.

---

## 99.2 Escaping

The renderer escapes or treats special-token text as ordinary content according to profile.

---

## 99.3 FIM Tokens

Code-infill profiles intentionally use validated special tokens.

---

# 100. Context-Window Models

The Model Profile declares one of:

* Combined Window;
* Separate Input and Output;
* Input Limit with Provider-Managed Reasoning;
* Modality-Specific;
* or Provider Opaque.

---

## 100.1 Combined Window

Input, visible output and possibly reasoning share one maximum.

---

## 100.2 Separate Input and Output

Input and output have independent maxima.

---

## 100.3 Provider-Managed Reasoning

A provider may consume hidden reasoning tokens with provider-specific accounting.

The profile records known constraints.

---

## 100.4 Modality-Specific

Images, audio or documents may have separate count or count-equivalent limits.

---

## 100.5 Provider Opaque

Strict policy requires conservative bounds or denies.

---

# 101. Effective Limits

Compute applicable limits from:

* Model Profile;
* Provider Profile;
* endpoint;
* feature;
* local Execution Profile;
* project policy;
* enterprise policy;
* and task policy.

---

## 101.1 Minimum Wins

Use the minimum compatible limit.

---

## 101.2 Stale Metadata

Stale model-limit metadata blocks strict unattended use.

---

## 101.3 Runtime Verification

Local runtime readiness reports its configured context.

---

# 102. Budget Components

The budget should account for:

```text
trusted instructions
project instructions
current request
conversation
source context
tool definitions
tool-choice framing
output schema
modality overhead
provider framing
output reserve
reasoning reserve
tool-result continuation reserve
safety reserve
```

---

# 103. Mandatory Instruction Budget

Trusted instruction templates have measured token cost per Tokenizer Profile.

---

## 103.1 Fixed Cost Cache

Cache by:

* instruction revision;
* tokenizer profile;
* renderer;
* and task class.

---

## 103.2 Dynamic Instructions

Any dynamic instruction field is counted in the final request.

---

# 104. Current Request Budget

Count the exact user request after rendering.

---

## 104.1 Attachments

Attachment context is separate from user-text cost.

---

## 104.2 Empty Request

A task command may generate a deterministic user instruction.

It is visible in the plan.

---

# 105. Conversation Structure Budget

Message roles and wrappers can add cost.

---

## 105.1 Turn Count

A large number of tiny turns may cost more than an equivalent summary.

---

## 105.2 Provider Differences

Do not estimate turn overhead universally.

---

# 106. Tool Definition Budget

Tool schemas can consume substantial context.

---

## 106.1 Only Available Tools

Include only tools available and approved for the operation.

---

## 106.2 No Global Tool Catalogue

Do not send every Opure, plugin or MCP tool definition.

---

## 106.3 Tool Search

A future tool-search feature must not conceal tool-schema expansion.

---

## 106.4 Changed Schema

Recount and reapprove where required.

---

## 106.5 Tool Result Reserve

A multi-step tool operation may need capacity for:

* assistant tool call;
* tool result;
* and final answer.

Reserve explicitly or prohibit the loop.

---

# 107. Structured Output Schema Budget

The full schema and provider framing are counted.

---

## 107.1 Simplification

A trusted schema optimiser may remove non-semantic descriptions for constrained machine output.

The optimised schema is versioned and visible.

---

## 107.2 No Model-Generated Schema

A model does not choose its own authoritative output schema.

---

# 108. Image Budget

Images use provider and model-specific accounting.

---

## 108.1 Count Endpoint

Use official multimodal counting where available.

---

## 108.2 Local Model

Use the selected vision runtime's exact image-token policy.

---

## 108.3 Detail and Resolution

Image detail or resolution setting is part of the plan.

---

## 108.4 Preprocessing

Resizing, cropping or tiling changes:

* content;
* token cost;
* and quality.

It is explicit and provenance bound.

---

# 109. Audio Budget

Audio is disabled initially under relevant ADRs unless a model feature is approved.

When enabled, count:

* duration;
* encoding;
* sample rate;
* provider token equivalents;
* and output reserve.

---

# 110. Document Budget

PDF or document input may include:

* extracted text;
* page images;
* layout;
* and provider-specific overhead.

The plan identifies the representation.

---

## 110.1 No File API Shortcut

Provider-side file storage remains disabled under ADR-0019.

---

# 111. Reasoning Budget

A reasoning or thinking model may use additional tokens.

---

## 111.1 Explicit Control

The Model Profile defines:

* supported reasoning modes;
* budget semantics;
* count-window semantics;
* billing semantics;
* and visible output relationship.

---

## 111.2 No Hidden Chain-of-Thought Request

Opure does not request private reasoning content.

---

## 111.3 Visible Summary

A provider-visible reasoning summary is output content and counted according to the provider.

---

## 111.4 Combined Window

Reserve reasoning capacity when it counts against the combined limit.

---

# 112. Output Reserve

Output reserve is task specific.

---

## 112.1 Suggested Initial Defaults

Illustrative defaults:

* concise explanation: 1,024 tokens;
* code review: 2,048 tokens;
* patch proposal: 4,096 tokens;
* structured diagnostic: 1,500 tokens;
* long report: 6,000 tokens;
* and embedding: no generation reserve.

These are policy starting points, not universal truths.

---

## 112.2 Model Maximum

Reserve cannot exceed the Model Profile output limit.

---

## 112.3 Minimum Useful Output

Each task defines a minimum.

---

## 112.4 User Change

The developer may choose a smaller or larger reserve within limits.

A changed reserve creates a new plan.

---

## 112.5 No Invisible Shrink

The assembler does not silently lower it to admit context.

---

# 113. Safety Reserve

The safety reserve protects against:

* provider count variance;
* framing drift;
* tokenizer mismatch;
* provider-added tokens;
* modality variance;
* and response continuation.

---

## 113.1 Suggested Policy

For exact local count:

```text
max(64 tokens, 0.5% of effective input limit)
```

For provider exact count:

```text
max(128 tokens, 1% of effective input limit)
```

For provider estimate or verified local estimate:

```text
max(512 tokens, 3% of effective input limit)
```

For conservative estimate:

```text
max(1,024 tokens, 8% of effective input limit)
```

These values require evidence.

---

## 113.2 Hard Maximum

Do not let reserve consume so much that mandatory content and minimum output are impossible.

---

# 114. Budget Formula

Conceptual variables:

```text
I_model = model input limit
O_model = model output limit
C_model = combined context limit
I_endpoint = endpoint input limit
I_local = local execution context
I_policy = project or enterprise input limit
O_requested = requested visible output
R_reasoning = reasoning reserve
R_tool = tool continuation reserve
R_safety = safety reserve
M_fixed = mandatory request and framing cost
```

For separate limits:

```text
I_effective = min(I_model, I_endpoint, I_local, I_policy)
O_effective = min(O_requested, O_model)
ContextBudget = I_effective - M_fixed - R_safety
```

For combined limits:

```text
C_effective = min(C_model, endpoint combined limit, local execution limit, policy limit)
I_effective = C_effective - O_requested - R_reasoning - R_tool
ContextBudget = I_effective - M_fixed - R_safety
```

---

## 114.1 Negative Budget

If negative:

* fail;
* reduce optional requested features through a new plan;
* or select another approved model.

---

# 115. Allocation Buckets

A Context Policy may divide optional budget into buckets.

Example:

```text
explicit project evidence: 40%
conversation: 15%
retrieved code: 25%
memory: 10%
repository and diagnostics: 10%
```

---

## 115.1 Flexible Borrowing

Unused optional budget may be borrowed by higher-priority buckets.

---

## 115.2 Mandatory Outside Percentage

Mandatory content is admitted before optional allocation.

---

## 115.3 Caps

Use maximums, not guaranteed fill.

---

# 116. Budget by Task

### 116.1 Explain Code

Prioritise:

* selected range;
* containing symbol;
* related definitions;
* direct callers;
* and project instructions.

---

### 116.2 Review Patch

Prioritise:

* full changed hunks;
* base context;
* tests;
* invariants;
* and build evidence.

---

### 116.3 Diagnose Build

Prioritise:

* exact diagnostics;
* referenced code;
* project configuration;
* recent related changes;
* and relevant dependency metadata.

---

### 116.4 Generate File

Prioritise:

* explicit specification;
* neighbouring conventions;
* interfaces;
* tests;
* and project instructions.

---

### 116.5 Repository Question

Prioritise:

* exact search matches;
* symbol graph;
* relevant docs;
* and memory.

---

# 117. Preliminary Counting

During candidate selection:

* count item rendering approximately or exactly;
* cache counts;
* and estimate marginal cost.

---

## 117.1 Final Verification

Always count the assembled final request again.

---

## 117.2 Count Drift

If final count is higher:

* reduce deterministically;
* or fail.

Do not rely on provider truncation.

---

# 118. Token Count Cache

Cache key includes:

```text
tokenizer_profile_revision
request_renderer_revision
content_hash
content_kind
role
special_token_policy
tool_or_schema_hash
modality_settings
```

---

## 118.1 No Cross-Model Cache

Counts do not transfer between models unless Tokenizer Profiles explicitly prove identity.

---

## 118.2 Alias Change

Invalidates.

---

## 118.3 Cache Value

Store:

* count;
* method;
* accuracy;
* measured at;
* and variance evidence.

---

# 119. Candidate Item Count

A chunk count includes its canonical item header and delimiters.

---

## 119.1 Marginal Count

When items share one header, calculate marginal cost rather than naïve standalone sum.

---

# 120. Reduction Engine

The Reduction Engine applies only declared steps.

---

## 120.1 Input

* ranked candidates;
* mandatory set;
* exact or estimated counts;
* budget;
* policy;
* and user selections.

---

## 120.2 Output

* admitted items;
* omitted items;
* reduction steps;
* count;
* and unresolved overflow.

---

## 120.3 Determinism

Same inputs and versions produce the same result.

---

# 121. Reduction Step 1 — Exact Duplicates

Merge duplicate content while retaining provenance.

---

# 122. Reduction Step 2 — Redundant Candidates

Remove lower-priority near duplicates where safe.

---

## 122.1 Comparison Task Exception

Do not collapse compared versions.

---

# 123. Reduction Step 3 — Optional Metadata

Remove:

* broad repository summaries;
* optional file lists;
* low-value model-card-style metadata;
* and repeated headers.

---

# 124. Reduction Step 4 — Overlap

Reduce optional chunk overlap to the policy minimum.

---

## 124.1 Exact Selection Protection

Do not shrink the selected range.

---

# 125. Reduction Step 5 — Supporting Chunks

Reduce:

* distant callers;
* distant callees;
* low-score sibling symbols;
* and repeated tests.

---

# 126. Reduction Step 6 — Conversation

Remove oldest non-essential turns.

---

## 126.1 Preserve Constraints

Keep:

* user constraints;
* accepted decisions;
* corrections;
* and unresolved requirements.

---

# 127. Reduction Step 7 — Approved Summary

Use an existing valid summary where policy permits.

---

## 127.1 No On-Demand Hidden Summary

Do not invoke a model silently.

---

# 128. Reduction Step 8 — Memory

Remove:

* low confidence;
* stale;
* redundant;
* and derived memory

before authoritative source.

---

# 129. Reduction Step 9 — Retrieval Breadth

Reduce optional result count.

---

## 129.1 Preserve Diversity

Do not leave only many chunks from one file when another source is directly relevant.

---

# 130. Reduction Step 10 — Structural Narrowing

For supporting source chunks:

* keep signature;
* keep directly referenced lines;
* keep critical surrounding block;
* remove unrelated body sections.

---

## 130.1 New Derived Chunk

The narrowed chunk receives its own rendering and range provenance.

---

# 131. Reduction Step 11 — Generated and External Examples

Remove generated code, examples and external references before direct project evidence unless task specific.

---

# 132. Reduction Step 12 — User Choices

Offer explicit alternatives:

* lower output reserve;
* lower reasoning mode;
* remove selected optional items;
* choose another model;
* use local model;
* decompose task;
* create a reviewed summary;
* or cancel.

---

## 132.1 Cloud Boundary

Changing to a remote model requires ADR-0019 approval.

---

# 133. No Silent Actions

The Reduction Engine must not silently:

* remove mandatory item;
* remove current request;
* remove explicit selection;
* summarise;
* change model;
* change provider;
* change execution profile;
* reduce cloud consent detail;
* enable cache;
* or split the task.

---

# 134. Context Budget Exceeded

When unresolved, return:

```text
Context Budget Exceeded
```

with:

* limit;
* mandatory count;
* optional count;
* output reserve;
* safety reserve;
* largest items;
* attempted reductions;
* and actionable choices.

---

# 135. Task Decomposition

Task decomposition is an explicit workflow operation.

---

## 135.1 Plan

Show:

* subtask count;
* source partition;
* model calls;
* intermediate state;
* cost;
* and synthesis step.

---

## 135.2 No Hidden Multi-Call

A single-call approval does not authorise multiple remote calls.

---

## 135.3 Intermediate Results

Remain model-generated and provenance bound.

---

# 136. Derived Summaries

A summary may reduce context when explicitly created.

---

## 136.1 Types

* conversation summary;
* file summary;
* module summary;
* build summary;
* test-cluster summary;
* and repository summary.

---

## 136.2 Source Binding

Bind every source reference and hash.

---

## 136.3 Summary Model

Record:

* provider or runtime;
* model;
* prompt template;
* Context Plan;
* and receipt.

---

## 136.4 Review State

* Unreviewed;
* Developer Reviewed;
* Deterministically Validated;
* Superseded;
* or Invalid.

---

## 136.5 Data Classification

Summary inherits the most restrictive material source classification.

---

## 136.6 Secret Scan

Scan output.

---

## 136.7 No Authority Upgrade

A summary is not more authoritative than source.

---

# 137. Deterministic Structural Summaries

Prefer non-model summaries where feasible.

Examples:

* symbol inventory;
* public API signatures;
* dependency list;
* error clusters;
* test failure groups;
* changed-file list;
* and configuration key inventory.

---

## 137.1 Trust

They remain derived but deterministic.

---

# 138. Context Quality

A plan should optimise relevance, not only utilisation.

---

## 138.1 Do Not Fill for Its Own Sake

Unused context capacity is acceptable.

---

## 138.2 Low-Relevance Stop

Stop admitting optional candidates below a task-specific threshold.

---

## 138.3 Contradiction Detection

Flag conflicting context such as:

* two versions of a file;
* superseded instructions;
* conflicting memory;
* and inconsistent build states.

---

## 138.4 Model Disclosure

Where contradictions remain, label them in the rendered context.

---

# 139. Stale Context

A Context Item is stale when its source generation no longer matches.

---

## 139.1 Execution Revalidation

Before inference, revalidate:

* project;
* file snapshots;
* diff;
* instructions;
* memory supersession;
* tool schemas;
* provider;
* model;
* and policy.

---

## 139.2 Stale Mandatory Item

Invalidate plan.

---

## 139.3 Stale Optional Item

Rebuild plan.

Do not silently send the old version after source changed.

---

# 140. Retrieval Index Freshness

Index metadata includes:

* workspace generation;
* indexed files;
* embedding model;
* chunker;
* and completion state.

---

## 140.1 Partial Index

A partial index is visible.

---

## 140.2 Exact Search

Workspace exact lookup can supplement a stale index.

---

## 140.3 Semantic Search

Do not return a vector whose source hash is stale.

---

# 141. Cross-Project Isolation

A Context Plan binds one project initially.

---

## 141.1 Shared Library

Another repository requires an explicit multi-project operation in a future ADR or amendment.

---

## 141.2 No Global Similarity Search

Embedding search is project scoped.

---

# 142. Data Sharing Integration

For remote inference, Context Plan feeds ADR-0019 Data Sharing Plan.

---

## 142.1 Same Items

Data Sharing Plan references the exact Context Items.

---

## 142.2 Count Request

Count and inference share the same approved data set.

---

## 142.3 Redaction

Remote rendered content is the approved redacted version.

---

## 142.4 Relative Paths

Use relative safe labels.

---

# 143. Local Inference Integration

For local models, the plan binds ADR-0020:

* Model Profile;
* Runtime Package;
* Execution Profile;
* chat template;
* and context length.

---

## 143.1 Local Does Not Bypass Secret Policy

Task and project policy still apply.

---

# 144. Tool Calling Integration

A request with tools includes only approved tools.

---

## 144.1 Tool Projection

Tool definitions come from trusted mediators.

---

## 144.2 Tool Schema Count

Part of final count.

---

## 144.3 Tool Result

A tool result creates a new Context Plan for the continuation.

---

## 144.4 No Unbounded Loop

Each continuation has a call and token budget.

---

# 145. MCP Integration

MCP tool and resource data is explicitly represented.

---

## 145.1 Definition Cost

MCP tool definitions consume context and are selected narrowly.

---

## 145.2 Result Cost

MCP results are classified and counted.

---

## 145.3 Prompt Content

MCP prompts remain untrusted prompt content and are not inserted into product instruction layers.

---

# 146. Plugin Integration

Plugin-proposed context must include:

* plugin identity;
* source capability;
* project;
* data class;
* and reason.

---

## 146.1 No Raw Prompt Injection API

The Plugin SDK does not expose:

```text
AppendToSystemPrompt(string)
```

or unrestricted prompt concatenation.

---

# 147. Workflow Integration

A workflow step declares a Context Policy.

---

## 147.1 Pinning

Durable workflow state pins:

* Context Policy revision;
* model;
* tokenizer;
* instruction template;
* and source references.

---

## 147.2 Resume

On resume, rebuild or validate the plan.

---

## 147.3 Changed Source

Pause when source changes materially.

---

# 148. Context Plan Approval

Approval may be required by:

* cloud policy;
* sensitive data;
* large context;
* external content;
* plugin context;
* MCP context;
* or task decomposition.

---

## 148.1 Approval Hash

Bind exact Context Plan hash and Data Sharing Plan hash.

---

## 148.2 Human Visibility

Show selected and omitted source categories.

---

# 149. Execution Binding

AI Router accepts only a Ready unexpired plan.

---

## 149.1 One Plan, One Request

Default.

---

## 149.2 Streaming

Streaming remains the same request.

---

## 149.3 Continuation

A tool result or follow-up creates a new plan.

---

# 150. Provider Auto-Truncation

Provider adapters set truncation to disabled where available.

---

## 150.1 No Control Available

The adapter must demonstrate the provider rejects oversized input rather than silently truncating.

---

## 150.2 Unknown Behaviour

Strict use is denied.

---

## 150.3 Error

An over-limit provider error returns to Context Assembly for replanning.

---

# 151. Tokenizer Auto-Truncation

Any tokenizer or SDK truncation option is disabled.

---

## 151.1 Overflow Tokens

Overflow APIs may be used for explicit chunk construction, not invisible final-request truncation.

---

# 152. Provider-Side Compaction

Provider automatic context compaction or conversation editing is disabled under the initial stateless architecture.

---

# 153. Actual Usage Reconciliation

After inference, compare:

* preflight count;
* actual input;
* cached input;
* output;
* reasoning;
* tool use;
* and total.

---

## 153.1 Variance

Store:

```text
actual - preflight
```

and percentage.

---

## 153.2 Threshold

Excessive variance:

* degrades the Tokenizer Profile;
* increases safety reserve;
* pauses unattended use;
* or quarantines adapter behaviour.

---

## 153.3 Provider Added Tokens

Record when documentation indicates provider-added tokens.

---

# 154. Count Calibration

Use synthetic and representative corpora.

---

## 154.1 Corpus

Include:

* English prose;
* British English;
* source code;
* JSON;
* YAML;
* XML;
* Markdown;
* Unicode;
* emoji;
* CJK;
* long identifiers;
* minified text;
* tool schemas;
* images;
* and multi-turn conversations.

---

## 154.2 Models

Calibrate every supported Model Profile.

---

## 154.3 Updates

Repeat after:

* model update;
* tokenizer update;
* adapter update;
* renderer update;
* provider API change;
* or chat-template change.

---

# 155. Count Failure

If exact counting fails:

* do not send an oversized request blindly;
* use an approved conservative method;
* or fail.

---

## 155.1 Remote Count Outage

Ask Every Time approval does not imply permission to skip count safeguards.

---

## 155.2 Local Tokenizer Failure

Offer model load for counting, conservative estimate, another profile or cancel.

---

# 156. Count Cost

Provider count endpoints may have:

* rate limits;
* latency;
* and data-retention implications.

---

## 156.1 Cache

Cache exact count results locally by plan and tokenizer profile.

---

## 156.2 No Count Storm

Repeated UI edits use local item estimates until a bounded final count is needed.

---

# 157. Plan Caching

A Context Plan cache may retain immutable plan metadata.

---

## 157.1 Cache Key

Includes:

* operation;
* project generation;
* policy;
* model;
* tokenizer;
* provider;
* instructions;
* source hashes;
* tool schemas;
* and output reserve.

---

## 157.2 No Authority Cache

A cached plan must revalidate:

* approval;
* policy;
* project state;
* and expiry.

---

# 158. Content Cache

Chunk content may be cached under the Workspace and Search access boundary.

---

## 158.1 Cross-Project Deduplication

Physical deduplication must not imply cross-project authority.

---

## 158.2 Secret Content

Secret or protected content is excluded from ordinary context caches.

---

# 159. Provider-Side Prompt Caching

Disabled initially under ADR-0019.

---

## 159.1 Future Gate

Requires:

* exact cached prefix;
* retention;
* deletion;
* provider;
* project;
* data classes;
* cache key;
* cost;
* and plan visibility.

---

# 160. Context Receipt

Suggested schema:

```text
opure.context-receipt/1
```

---

## 160.1 Fields

```text
receipt_id
operation_id
context_plan_hash
data_sharing_plan_hash
project_id
workspace_generation
context_policy
instruction_template
provider_or_runtime
model_profile
tokenizer_profile
count_method
count_accuracy
items_included
items_omitted
reductions
preflight_input_tokens
actual_input_tokens
cached_input_tokens
output_tokens
reasoning_tokens
tool_tokens
output_reserve
reasoning_reserve
safety_reserve
limit
policy
approval
started_at
completed_at
result
variance
```

---

## 160.2 No Full Content by Default

Receipt uses:

* hashes;
* safe labels;
* ranges;
* and classifications.

---

## 160.3 Local Workflow

The owning workflow may retain content separately.

---

# 161. Trust Centre

Trust Centre should show:

* active Context Policies;
* context sources;
* plan previews;
* included items;
* omitted items;
* reduction decisions;
* tokenizer profile;
* count accuracy;
* provider count transmissions;
* output reserve;
* actual usage;
* variance;
* and failures.

---

## 161.1 Why Included

Every item should have an explanation.

---

## 161.2 Why Omitted

Every explicit or high-score candidate omitted for budget should have a reason.

---

## 161.3 Hidden Context Test

The displayed plan should match the actual provider request under test.

---

# 162. Diagnostic Logging

Logs may include:

* operation ID;
* policy;
* model;
* tokenizer;
* counts;
* number of candidates;
* admitted items;
* omitted reasons;
* reduction steps;
* timing;
* and variance.

---

## 162.1 Prohibited Logs

Do not log:

* source content;
* prompt;
* secret;
* full paths where unnecessary;
* provider credential;
* or hidden reasoning.

---

# 163. Metrics

Low-cardinality local metrics may include:

* plan count;
* plan latency;
* candidate count;
* admitted count;
* token-budget utilisation;
* exact versus estimated counts;
* variance;
* reduction steps;
* budget failures;
* stale-plan failures;
* and source failure categories.

---

## 163.1 No Project Labels

Do not export project or file identity.

---

# 164. Persistence

Suggested service-owned tables:

```text
context_policies
tokenizer_profiles
context_plans
context_plan_items
context_plan_omissions
context_plan_reductions
token_count_cache
chunk_profiles
chunk_metadata
retrieval_feature_versions
context_receipts
count_calibration_runs
```

---

## 164.1 Raw Content

Do not duplicate full source content into the Context database unless an immutable operation snapshot requires it.

Prefer references to Workspace-owned snapshots.

---

## 164.2 Derived Summaries

Store in Memory or a dedicated derived-context store with provenance.

---

# 165. Retention

Suggested provisional retention:

* active plans: until expiry plus short recovery window;
* plan metadata: 30 days;
* receipts: 90 days or Trust Centre policy;
* calibration results: while Tokenizer Profile is supported;
* count cache: bounded LRU and version invalidation;
* chunk metadata: while source generation is current;
* and raw temporary rendered request: memory only unless workflow requires it.

---

# 166. Recovery

On Runtime restart:

* Draft and Ready plans expire;
* live count requests cancel;
* cached item counts remain only if version valid;
* Context Receipts remain;
* and workflows rebuild plans.

---

## 166.1 Provider Request Uncertain

If provider received a request before crash, AI Router receipt governs result uncertainty.

---

# 167. Cancellation

Context assembly is cancellable at:

* source gathering;
* scanning;
* chunking;
* retrieval;
* counting;
* and remote count request.

---

## 167.1 No Partial Ready Plan

Cancellation does not produce a Ready plan.

---

# 168. Resource Limits

Context Assembly limits:

* candidate count;
* bytes materialised;
* files;
* parser time;
* retrieval time;
* embedding queries;
* count calls;
* memory;
* and total duration.

---

## 168.1 Suggested Candidate Limits

Illustrative:

* candidate references: 10,000;
* materialised source candidates: 1,000;
* final context items: 200;
* total pre-reduction bytes: 20 MiB;
* final text bytes: model specific;
* and remote count calls: one per final plan by default.

---

# 169. Performance

The service should prioritise responsiveness.

---

## 169.1 Progressive Preview

Show candidate and budget progress without exposing unverified content.

---

## 169.2 Incremental Work

Reuse valid:

* chunks;
* lexical index;
* embeddings;
* counts;
* and classifications.

---

## 169.3 Final Integrity

Incremental caches never replace final revalidation.

---

# 170. Error Model

Stable error categories include:

* Context Policy Missing;
* Context Policy Invalid;
* Context Source Denied;
* Context Source Unavailable;
* Context Source Stale;
* Context Project Mismatch;
* Context Snapshot Changed;
* Context Instruction Changed;
* Context Protected Content;
* Context Secret Detected;
* Context Classification Denied;
* Context Parse Failed;
* Context Search Failed;
* Context Index Stale;
* Context Candidate Limit Exceeded;
* Context Tokenizer Missing;
* Context Tokenizer Changed;
* Context Count Failed;
* Context Count Estimate Unsafe;
* Context Tool Schema Changed;
* Context Output Schema Changed;
* Context Budget Exceeded;
* Context Mandatory Content Too Large;
* Context Reduction Exhausted;
* Context Approval Required;
* Context Plan Expired;
* Context Plan Invalidated;
* Context Provider Truncation Unsafe;
* Context Usage Variance Excessive;
* Context Operation Cancelled;
* and Context Recovery Required.

---

# 171. User Interface Sections

Suggested views:

```text
Context Preview
Token Budget
Included Sources
Omitted Sources
Conversation
Project Memory
Retrieval
Tools and Schemas
Data Sharing
Reductions
Tokenizer
Usage History
Advanced Policy
```

---

## 171.1 Token Budget Visualisation

Show:

* total input limit;
* mandatory instructions;
* user request;
* explicit context;
* retrieved context;
* tools;
* schemas;
* output reserve;
* reasoning reserve;
* safety reserve;
* and unused capacity.

---

## 171.2 Exact or Estimated

Clearly label.

---

## 171.3 Omitted Item

Show reason and possible action.

---

## 171.4 Accessibility

Provide all information as text, not only charts or colour.

---

# 172. UI Copy — Context Preview

Suggested text:

> Opure has assembled the listed project evidence for this model request. Every item is bound to the displayed project snapshot. Content not shown here will not be added silently. Items may be omitted because they are unauthorised, stale, sensitive, redundant, low relevance or outside the model's token budget.

---

# 173. UI Copy — Estimated Count

Suggested text:

> This provider does not expose an exact local tokeniser for the complete request. Opure is using the displayed model-specific estimate and safety reserve. The request will not use automatic truncation.

---

# 174. UI Copy — Budget Exceeded

Suggested text:

> The mandatory instructions, current request, selected evidence and output reserve do not fit this model's approved limits. Opure has not removed them or switched models automatically. Review the listed choices to create a new plan.

---

# 175. UI Copy — Provider Count

Suggested text:

> To obtain the provider's token count, Opure will send the same proposed request content to the displayed provider before generation. This preflight request is covered by the same project cloud policy and data-sharing approval.

---

# 176. Security Threat Model

Relevant threats include:

* hidden workspace context;
* wrong-project retrieval;
* stale snapshot;
* secret leakage;
* protected-file inclusion;
* prompt injection;
* instruction-layer confusion;
* malicious project instructions;
* malicious plugin context;
* malicious MCP context;
* poisoned memory;
* poisoned embeddings;
* similarity-score manipulation;
* token bomb;
* Unicode expansion;
* oversized tool schema;
* provider count as hidden transmission;
* tokenizer mismatch;
* automatic truncation;
* output starvation;
* model-driven context expansion;
* context-cache authority confusion;
* and receipt mismatch.

---

# 177. Security Controls

Controls include:

* typed source capabilities;
* project binding;
* snapshot hashes;
* instruction separation;
* data classification;
* secret scanning;
* protected-root denial;
* deterministic ranking;
* semantic score limitation;
* model-specific tokenisation;
* final serialised count;
* output reserve;
* safety reserve;
* truncation disabled;
* deterministic reduction;
* plan hashing;
* approval binding;
* revalidation;
* usage reconciliation;
* and Trust Centre evidence.

---

# 178. Security Limitations

This design cannot guarantee:

* perfect relevance;
* perfect secret detection;
* absence of prompt injection influence;
* absence of tokenizer provider bugs;
* exact provider billing agreement;
* model attention to every included item;
* correctness of semantic retrieval;
* or safety after a developer explicitly approves harmful content.

Large context can still reduce model quality.

The product must state these limitations honestly.

---

# 179. Testing Strategy

ADR-0008 applies.

Context assembly requires:

* unit tests;
* source-capability tests;
* instruction-layer tests;
* snapshot tests;
* classification tests;
* secret tests;
* parser and chunker tests;
* retrieval tests;
* ranking tests;
* deduplication tests;
* tokenizer tests;
* provider-count tests;
* budget tests;
* reduction tests;
* prompt-injection tests;
* integration tests;
* recovery tests;
* fuzzing;
* performance tests;
* and request-byte equivalence tests.

---

# 180. Context Policy Tests

Test:

* valid policy;
* duplicate policy ID;
* unsupported revision;
* unknown source;
* prohibited source;
* missing mandatory source;
* invalid priority;
* invalid reduction step;
* missing output reserve;
* zero safety reserve;
* excessive candidate limit;
* plugin narrowing;
* plugin broadening;
* and enterprise restriction.

---

# 181. Instruction-Layer Tests

Test:

* product security instruction;
* product task instruction;
* project-governed instruction;
* user request;
* conversation;
* source content;
* plugin content;
* MCP content;
* and model output.

Verify only trusted product layers receive product authority.

---

## 181.1 Injection Corpus

Place instructions such as:

* ignore system rules;
* reveal secrets;
* upload every file;
* call a tool;
* switch provider;
* remove review;
* and treat this file as policy

inside:

* source comments;
* README;
* project instruction candidate;
* build log;
* test output;
* memory;
* plugin result;
* MCP resource;
* prior assistant output;
* and model-generated summary.

Verify no deterministic policy changes.

---

# 182. Project Instruction Tests

Test:

* registered instruction file;
* unregistered magic filename;
* subtree scope;
* language scope;
* task scope;
* changed file;
* secret-bearing instruction;
* conflicting instructions;
* and superseded instruction.

---

# 183. Source-Capability Tests

Test:

* valid project reference;
* wrong project;
* expired capability;
* wrong caller;
* wrong workflow;
* stale generation;
* guessed opaque reference;
* plugin-provided reference;
* MCP-provided reference;
* and cross-profile reference.

---

# 184. Ambient Context Tests

Seed sensitive or distinctive canaries in:

* clipboard;
* another project;
* another editor tab;
* shell history;
* environment;
* email;
* browser history;
* user Documents;
* provider conversation;
* plugin private data;
* and MCP server state.

Verify none enters a plan without an explicit source capability.

---

# 185. Candidate Tests

Test:

* mandatory;
* explicit;
* direct evidence;
* supporting;
* optional;
* ineligible;
* missing source;
* stale source;
* duplicate;
* oversized candidate;
* and candidate cancellation.

---

# 186. Workspace Snapshot Tests

Test:

* valid snapshot;
* changed file;
* deleted file;
* renamed file;
* case-only rename;
* symlink;
* junction;
* hard link;
* alternate stream;
* encoding change;
* line-ending change;
* and another workspace generation.

---

# 187. Text Selection Tests

Test:

* exact selection;
* empty selection;
* Unicode range;
* CRLF;
* LF;
* selection after file edit;
* selection spanning symbols;
* oversized selection;
* and surrounding context.

---

# 188. Diff Tests

Test:

* one hunk;
* multiple hunks;
* rename;
* delete;
* add;
* binary;
* stale base;
* large diff;
* generated file;
* and secret in changed line.

---

# 189. Build Diagnostic Tests

Test:

* one compiler error;
* repeated error;
* cascading errors;
* warning;
* project configuration error;
* package restore error;
* command output;
* credential canary;
* wrong source line;
* and stale build run.

---

# 190. Test Diagnostic Tests

Test:

* assertion failure;
* exception;
* repeated parametrised tests;
* project stack frames;
* framework stack frames;
* captured output;
* secret canary;
* snapshot mismatch;
* and stale test run.

---

# 191. Repository Tests

Test:

* clean status;
* dirty status;
* selected commit;
* history;
* blame;
* remote URL;
* credentials absent;
* detached head;
* merge conflict;
* and repository generation change.

---

# 192. Symbol Tests

Test:

* exact symbol;
* overloaded symbol;
* partial type;
* interface and implementation;
* generated symbol;
* ambiguous name;
* parser failure;
* stale symbol index;
* and cross-file references.

---

# 193. Graph Tests

Test:

* direct caller;
* indirect caller;
* direct callee;
* inheritance;
* implementation;
* project reference;
* package reference;
* stale graph;
* cyclic graph;
* and malicious model-generated edge.

---

# 194. Search Tests

Test:

* exact path;
* exact phrase;
* lexical;
* regex;
* structural;
* symbol;
* semantic;
* no result;
* excessive result;
* regex timeout;
* secret result;
* and wrong-project result.

---

# 195. Embedding Tests

Test:

* current vector;
* stale source hash;
* changed embedding model;
* changed dimensions;
* changed chunker;
* local embedding;
* remote embedding approval;
* semantic false positive;
* semantic near duplicate;
* and adversarial repeated keywords.

---

# 196. Hybrid Ranking Tests

Test:

* explicit selection versus semantic score;
* exact symbol versus lexical score;
* diagnostic reference versus recency;
* source trust versus similarity;
* generated penalty;
* stale-index penalty;
* size cost;
* diversity;
* and deterministic tie-breaking.

---

## 196.1 Explainability

Every selected candidate must expose the feature contribution or deterministic reason.

---

# 197. Ranking Determinism Tests

Run the same inputs:

* repeatedly;
* on restart;
* across thread schedules;
* and after cache warm-up.

The ordering and plan hash must match.

---

# 198. Diversity Tests

Test:

* many chunks from one file;
* many memory results;
* many MCP results;
* repeated generated files;
* direct evidence exception;
* explicit selection exception;
* and limited budget.

---

# 199. Exact Deduplication Tests

Test:

* same content same source;
* same content different source;
* different line endings;
* different item headers;
* source and memory copy;
* source and model summary;
* and compared versions.

---

# 200. Near-Duplicate Tests

Test:

* copied class;
* generated client;
* repeated stack trace;
* test variants;
* one-line critical difference;
* diff comparison;
* and false merge.

---

# 201. Source-Code Chunker Tests

For each supported language, test:

* small file;
* namespace;
* type;
* method;
* oversized method;
* nested type;
* comments;
* strings;
* interpolated strings;
* preprocessor directives;
* invalid syntax;
* partial syntax;
* long identifier;
* and generated code.

---

# 202. Line Fallback Tests

Test:

* CRLF;
* LF;
* mixed endings;
* invalid encoding;
* Unicode;
* emoji;
* combining characters;
* one huge line;
* minified file;
* empty file;
* and final line without newline.

---

# 203. Configuration Chunker Tests

Test:

* JSON object;
* JSON array;
* duplicate JSON property;
* YAML mapping;
* YAML unsafe tag;
* XML namespace;
* external entity;
* deep nesting;
* TOML table;
* INI section;
* and secret field.

---

# 204. Markdown Chunker Tests

Test:

* headings;
* paragraphs;
* code fences;
* nested lists;
* table;
* HTML;
* front matter;
* remote image;
* malicious link;
* and oversized section.

---

# 205. Log Chunker Tests

Test:

* timestamped lines;
* stack trace;
* repeated line;
* correlation ID;
* multi-line error;
* ANSI escape;
* very long line;
* binary contamination;
* and secret.

---

# 206. Diff Chunker Tests

Test:

* hunk boundaries;
* context lines;
* no-newline marker;
* Unicode;
* rename;
* binary marker;
* huge hunk;
* and stale hashes.

---

# 207. Conversation Tests

Test:

* current request;
* recent relevant turn;
* old accepted decision;
* recent irrelevant aside;
* correction;
* superseded decision;
* greeting;
* repeated confirmation;
* prior model output;
* hidden reasoning absence;
* and cross-project turn.

---

# 208. Conversation Summary Tests

Test:

* valid summary;
* missing source turns;
* changed source turn;
* conflicting new decision;
* remote summariser;
* local summariser;
* secret in source;
* secret in summary;
* unreviewed decision summary;
* and superseded summary.

---

# 209. Deterministic Compression Tests

Test removal of:

* greetings;
* repeated acknowledgements;
* duplicate links;
* duplicate assistant explanations;
* and obsolete status messages

while preserving:

* constraints;
* decisions;
* corrections;
* and unresolved work.

---

# 210. Memory Tests

Test:

* current authoritative memory;
* stale memory;
* low-confidence memory;
* conflicting memories;
* superseded memory;
* derived summary;
* secret memory;
* wrong project;
* and source document available.

---

# 211. Plugin Context Tests

Test:

* approved result;
* unapproved result;
* wrong plugin;
* changed package hash;
* excessive output;
* prompt injection;
* secret;
* another project;
* and revoked capability.

---

# 212. MCP Context Tests

Test:

* approved tool result;
* approved resource;
* changed tool fingerprint;
* resource link;
* hostile instruction;
* account change;
* excessive output;
* secret;
* and result-to-model permission absent.

---

# 213. External Document Tests

Test:

* plain text;
* Markdown;
* PDF text;
* PDF image;
* Office document;
* macro;
* active HTML;
* remote links;
* oversized document;
* and malformed archive.

---

# 214. Classification Tests

Test every data class and trust class.

Verify:

* most restrictive class wins;
* classification provenance;
* stale classifier rules;
* and changed classification invalidates the plan.

---

# 215. Secret Tests

Use canaries in:

* user request;
* source code;
* comments;
* configuration;
* diff;
* build log;
* test output;
* repository metadata;
* memory;
* plugin result;
* MCP result;
* conversation;
* image metadata;
* and model summary.

Verify no denied canary reaches remote count or inference.

---

# 216. Redaction Tests

Test:

* exact credential;
* partial credential;
* structured value;
* URL;
* multiline private key;
* repeated secret;
* false positive;
* and structural leakage after redaction.

---

# 217. Context Plan Schema Tests

Test:

* valid plan;
* duplicate item;
* invalid role;
* missing provenance;
* missing classification;
* missing count;
* unknown source;
* invalid reduction;
* expired plan;
* missing hash;
* and canonical serialisation.

---

# 218. Plan Hash Tests

Change one at a time:

* source content;
* range;
* ordering;
* instruction;
* model;
* tokenizer;
* tool schema;
* output schema;
* reserve;
* provider;
* project;
* and policy.

Every material change alters the hash.

---

# 219. Tokenizer Profile Tests

Test:

* exact local;
* provider exact;
* provider estimate;
* local verified estimate;
* conservative estimate;
* unknown;
* changed vocabulary;
* changed merges;
* changed chat template;
* changed renderer;
* changed model alias;
* and unsupported request shape.

---

# 220. Local Tokenisation Tests

Using the exact local model, test:

* prose;
* source code;
* JSON;
* Unicode;
* emoji;
* special-token text;
* BOS;
* FIM;
* multi-turn chat;
* tool schema;
* structured output;
* and multimodal marker.

---

# 221. Provider Count Tests

For each supported provider, compare:

* plain text;
* messages;
* system instructions;
* tools;
* structured output;
* images;
* documents;
* thinking or reasoning settings;
* and actual usage.

---

## 221.1 Privacy

Capture network traffic and prove the remote count request contains no content outside the approved plan.

---

# 222. Count Endpoint Failure Tests

Test:

* network failure;
* authentication failure;
* rate limit;
* unsupported model;
* unsupported feature;
* provider timeout;
* changed response schema;
* estimate warning;
* and policy denial.

---

# 223. Count Cache Tests

Test:

* hit;
* miss;
* model change;
* tokenizer change;
* renderer change;
* content change;
* role change;
* tool-schema change;
* expiry;
* and corruption.

---

# 224. Fully Serialised Count Tests

Prove final count includes:

* role framing;
* delimiters;
* project headers;
* tool definitions;
* output schema;
* image markers;
* special tokens;
* and template suffix.

---

# 225. Separate-Limit Tests

Test a model with:

* input limit;
* output limit;
* requested output;
* task minimum;
* and optional context.

---

# 226. Combined-Window Tests

Test:

* visible output reserve;
* reasoning reserve;
* tool continuation;
* safety reserve;
* and effective input.

---

# 227. Provider-Opaque Tests

Test:

* strict denial;
* conservative estimate;
* expanded safety reserve;
* and actual usage variance.

---

# 228. Output Reserve Tests

Test:

* default by task;
* user increase;
* user decrease;
* below task minimum;
* above model maximum;
* structured output;
* tool continuation;
* and no silent shrink.

---

# 229. Safety Reserve Tests

Test by count accuracy class.

Verify:

* reserve applied;
* reserve visible;
* reserve not consumed by optional context;
* and actual variance response.

---

# 230. Tool-Schema Budget Tests

Test:

* no tools;
* one small tool;
* many tools;
* huge descriptions;
* duplicate schemas;
* changed schema;
* MCP tool;
* plugin tool;
* and only approved tools projected.

---

# 231. Structured-Output Budget Tests

Test:

* small schema;
* large schema;
* recursive schema;
* remote reference;
* optimised schema;
* changed schema;
* and local post-validation.

---

# 232. Multimodal Count Tests

Test:

* one image;
* multiple images;
* resolution change;
* crop;
* image metadata;
* PDF;
* audio when enabled;
* and provider count mismatch.

---

# 233. Preliminary Versus Final Count Tests

Ensure:

* item estimates guide admission;
* final request recount occurs;
* overhead difference is handled;
* and overflow causes reduction or failure.

---

# 234. Reduction Tests

Test every declared reduction step independently and in sequence.

---

## 234.1 Mandatory Protection

Mandatory items survive all ordinary reduction.

---

## 234.2 Explicit Protection

Explicit items require visible replanning before removal.

---

## 234.3 Determinism

Repeated reduction yields the same plan.

---

# 235. Duplicate Reduction Tests

Verify provenance merge.

---

# 236. Overlap Reduction Tests

Verify exact selected ranges remain.

---

# 237. Conversation Reduction Tests

Verify decisions and constraints remain.

---

# 238. Memory Reduction Tests

Verify authoritative source outranks derived memory.

---

# 239. Retrieval Breadth Tests

Verify diversity and direct evidence survive.

---

# 240. Structural Narrowing Tests

Verify:

* signature;
* exact lines;
* line numbers;
* and content hash

remain correct.

---

# 241. No-Silent-Action Tests

Instrument attempts to:

* truncate;
* summarise;
* switch model;
* switch provider;
* reduce output;
* split calls;
* enable cache;
* remove selection;
* and add context.

Every attempt must create a new plan or fail.

---

# 242. Budget-Exceeded Tests

Test:

* mandatory content too large;
* explicit content too large;
* tools too large;
* schema too large;
* user request too large;
* output reserve too large;
* and combined-window exhaustion.

Verify actionable report.

---

# 243. Provider Truncation Tests

Test:

* explicit disabled flag;
* provider oversized failure;
* provider auto mode accidentally enabled;
* provider silently truncates;
* and unknown behaviour.

Silent truncation is a release blocker.

---

# 244. Tokenizer Truncation Tests

Verify all tokenizer APIs use no-truncate for final request.

---

# 245. Task-Decomposition Tests

Test:

* user-approved split;
* source partition;
* multiple plans;
* intermediate results;
* remote call count;
* cost;
* failure;
* and final synthesis.

---

# 246. Derived-Summary Tests

Test:

* source hashes;
* model provenance;
* policy;
* secret classification;
* human review;
* source change;
* summary conflict;
* and no authority upgrade.

---

# 247. Context Quality Tests

Evaluate:

* precision of selected context;
* recall of required evidence;
* duplicate rate;
* source diversity;
* contradiction detection;
* and unused-capacity behaviour.

---

## 247.1 No Fill Requirement

A plan with no useful optional context should leave capacity unused.

---

# 248. Prompt-Injection Tests

Use adversarial content that tries to:

* change instruction hierarchy;
* request extra context;
* request secrets;
* cause model switch;
* invoke tools;
* hide files;
* alter output schema;
* and claim developer approval.

Verify deterministic boundaries.

---

# 249. Wrong-Project Tests

Create similar symbols and files in two projects.

Verify:

* search;
* memory;
* embeddings;
* conversation;
* plugin;
* and MCP

remain project bound.

---

# 250. Stale-Index Tests

Change files without updating indexes.

Verify stale chunks and embeddings are rejected.

---

# 251. Token-Bomb Tests

Test:

* repetitive Unicode;
* long identifiers;
* minified JSON;
* base64;
* combining characters;
* zero-width characters;
* emoji sequences;
* huge tool enums;
* and very long paths.

Verify bounded work and accurate count.

---

# 252. Unicode Security Tests

Test:

* bidirectional controls;
* homoglyphs;
* invalid UTF-8;
* combining marks;
* normalisation differences;
* and invisible characters.

Preserve source while displaying warnings where useful.

---

# 253. Request-Byte Equivalence Tests

Capture the exact provider or local runtime request.

Compare it against the rendered approved Context Plan.

Verify:

* no extra text;
* no missing text;
* no changed ordering;
* no hidden tool;
* no hidden schema;
* and no secret.

---

# 254. Actual Usage Reconciliation Tests

Test:

* exact match;
* small provider variance;
* large variance;
* cached tokens;
* provider-added tokens;
* reasoning tokens;
* tool tokens;
* and missing usage metadata.

---

# 255. Variance Response Tests

Verify:

* safety reserve update;
* profile degradation;
* unattended-use pause;
* adapter quarantine;
* and diagnostic evidence.

---

# 256. Workflow Tests

Test:

* pinned policy;
* pinned model;
* resume;
* source change;
* tool continuation;
* remote approval;
* task decomposition;
* and Context Receipt linkage.

---

# 257. Plugin Tests

Test a plugin attempts to:

* append system prompt;
* add arbitrary file;
* add hidden data;
* increase budget;
* switch provider;
* and bypass plan approval.

Every attempt denies.

---

# 258. MCP Tests

Test an MCP server attempts to:

* inject instructions;
* add a resource link;
* add another tool;
* expand output;
* request sampling;
* and provide hidden project data.

---

# 259. Recovery Tests

Test:

* Runtime restart;
* count request interrupted;
* plan expired;
* project closed;
* file changed;
* index rebuild;
* provider unavailable;
* tokenizer unavailable;
* and workflow resume.

---

# 260. Cancellation Tests

Cancel during:

* source read;
* secret scan;
* parse;
* search;
* embedding retrieval;
* ranking;
* local count;
* remote count;
* and preview.

No Ready plan should emerge.

---

# 261. Fuzzing

Fuzz:

* Context Policy;
* Context Plan;
* source references;
* chunk metadata;
* parsers;
* renderers;
* tokeniser adapters;
* provider count responses;
* tool schemas;
* output schemas;
* and receipt parsing.

---

# 262. Performance Tests

Measure:

* policy evaluation;
* source validation;
* file snapshot access;
* chunking;
* lexical search;
* semantic search;
* ranking;
* deduplication;
* secret scan;
* item counting;
* final counting;
* reduction;
* plan rendering;
* and receipt persistence.

---

# 263. Provisional Performance Targets

On the reference workstation:

* cached policy lookup: under 2 ms p95;
* Context Plan skeleton: under 10 ms p95;
* candidate ranking for 1,000 candidates: under 100 ms p95;
* exact deduplication for 1,000 candidates: under 50 ms p95;
* chunk-cache lookup: under 5 ms p95;
* final local token count for a 32,000-token request: under 250 ms p95 where tokenizer is resident;
* deterministic reduction: under 100 ms p95 for 1,000 candidates;
* and plan revalidation: under 50 ms p95 excluding source I/O.

Search, embedding and provider-count targets are adapter specific.

These are targets pending evidence.

---

# 264. Scalability Tests

Test:

* 100 files;
* 10,000 files;
* 100,000 files;
* one million indexed chunks;
* large conversation;
* large memory store;
* 10,000 candidates;
* and many tool schemas.

The final operation remains bounded.

---

# 265. Accessibility Tests

Context UI must support:

* keyboard;
* Narrator;
* high contrast;
* reduced motion;
* focus management;
* source expansion;
* omission reasons;
* token-budget text;
* exact or estimated labels;
* and actionable budget choices.

---

# 266. Prototype Plan

## 266.1 Prototype A — Context Plan

Build a plan from one selected C# file, current request and project instruction.

---

## 266.2 Prototype B — Exact Local Count

Apply the selected local model template and count the exact request through ADR-0020 runtime.

---

## 266.3 Prototype C — Remote Count

Use one approved provider count endpoint under the same Data Sharing Plan.

---

## 266.4 Prototype D — Provider Difference

Count the same structured request through a second provider and record different semantics.

---

## 266.5 Prototype E — Source Retrieval

Combine exact symbol, lexical and semantic results with inspectable scoring.

---

## 266.6 Prototype F — Chunking

Chunk C#, JSON, Markdown, logs and diffs with stable provenance.

---

## 266.7 Prototype G — Reduction

Overflow a model window and prove deterministic visible reduction.

---

## 266.8 Prototype H — No Truncation

Attempt provider and tokenizer automatic truncation and prove rejection.

---

## 266.9 Prototype I — Prompt Injection

Insert hostile instructions into every untrusted source.

---

## 266.10 Prototype J — Secret Leakage

Seed secrets through user, source, diagnostics, memory, plugin and MCP content.

---

## 266.11 Prototype K — Stale Context

Change a file and index after plan approval.

---

## 266.12 Prototype L — Usage Variance

Compare preflight and actual counts and degrade an inaccurate Tokenizer Profile.

---

# 267. Implementation Plan

1. Record founder review.
2. Define Context Policy schema.
3. Define Context Plan schema.
4. Define Context Receipt schema.
5. Define source-capability contracts.
6. Implement instruction layers.
7. Implement candidate records.
8. Integrate Workspace snapshots.
9. Integrate Repository evidence.
10. Integrate Build diagnostics.
11. Integrate Test diagnostics.
12. Implement source classifications.
13. Integrate secret scanner.
14. Implement code chunker profiles.
15. Implement configuration chunkers.
16. Implement Markdown, log and diff chunkers.
17. Implement lexical search.
18. Integrate symbol and dependency graphs.
19. Integrate semantic retrieval.
20. Integrate Memory candidates.
21. Integrate Plugin result provenance.
22. Integrate MCP result provenance.
23. Implement ranking features.
24. Implement deterministic fusion.
25. Implement exact and near deduplication.
26. Implement diversity policy.
27. Define Tokenizer Profile schema.
28. Implement local `llama.cpp` counting.
29. Implement one provider count adapter.
30. Implement a second provider count adapter.
31. Implement request renderers.
32. Implement context-window models.
33. Implement output, reasoning and safety reserves.
34. Implement count cache.
35. Implement Reduction Engine.
36. Implement Budget Exceeded result.
37. Implement conversation compression.
38. Implement derived-summary provenance.
39. Implement plan hashing.
40. Implement Context Preview UI.
41. Integrate ADR-0019 Data Sharing Plans.
42. Integrate AI Router execution binding.
43. Implement actual usage reconciliation.
44. Implement Trust Centre views.
45. Add adversarial context corpus.
46. Run performance and scale tests.
47. Complete security and privacy review.
48. Accept, amend or reject the ADR.

---

# 268. Owners

| Area                     | Owner                                 |
| ------------------------ | ------------------------------------- |
| Product policy           | Founder                               |
| Context Assembly Service | Context Assembly Owner                |
| Model execution          | AI Router Owner                       |
| Workspace snapshots      | Workspace Owner                       |
| Repository evidence      | Repository Owner                      |
| Search and retrieval     | Search Owner                          |
| Project memory           | Memory Owner                          |
| Tokenizer profiles       | AI Router and Provider Adapter Owners |
| Local counting           | Local Model Runtime Owner             |
| Remote counting          | Provider Trust Owner                  |
| Secrets                  | Security and Secrets Owners           |
| Plugin context           | Plugin Platform Owner                 |
| MCP context              | MCP Gateway Owner                     |
| Workflow context         | Workflow Owner                        |
| Trust Centre             | Trust Centre Owner                    |
| Desktop preview          | Desktop Owner                         |
| Persistence              | Persistence Owner                     |
| Performance              | Performance Owner                     |
| Adversarial tests        | Test Architecture Owner               |

---

# 269. Suggested Repository Structure

```text
src/
├── Context/
│   ├── Opure.Context.Contracts/
│   ├── Opure.Context.Assembly/
│   ├── Opure.Context.Policies/
│   ├── Opure.Context.Candidates/
│   ├── Opure.Context.Chunking/
│   ├── Opure.Context.Retrieval/
│   ├── Opure.Context.Ranking/
│   ├── Opure.Context.Deduplication/
│   ├── Opure.Context.Tokenization/
│   ├── Opure.Context.Budgeting/
│   ├── Opure.Context.Reduction/
│   └── Opure.Context.Security/
├── AI/
│   └── Opure.AI.ContextAdapter/
└── Desktop/
    └── Opure.Desktop.Context/

schemas/
└── context/
    ├── context-policy-v1.schema.json
    ├── context-plan-v1.schema.json
    ├── context-receipt-v1.schema.json
    ├── tokenizer-profile-v1.schema.json
    └── chunker-profile-v1.schema.json

tests/
└── Context/
    ├── Opure.Context.UnitTests/
    ├── Opure.Context.ChunkerTests/
    ├── Opure.Context.RetrievalTests/
    ├── Opure.Context.TokenizerTests/
    ├── Opure.Context.SecurityTests/
    ├── Opure.Context.IntegrationTests/
    └── Fixtures/
        ├── Repositories/
        ├── Conversations/
        └── Adversarial/
```

Exact project count may be consolidated under ADR-0010.

---

# 270. Context Policy Sketch

```json
{
  "schema": "opure.context-policy/1",
  "id": "code-review",
  "revision": 1,
  "task_class": "review-patch",
  "sources": {
    "mandatory": [
      "current-request",
      "current-diff"
    ],
    "allowed": [
      "project-instructions",
      "selected-files",
      "build-diagnostics",
      "test-diagnostics",
      "symbol-search",
      "semantic-search",
      "project-memory"
    ]
  },
  "budget": {
    "output_reserve": 2048,
    "minimum_output": 768,
    "safety_reserve_policy": "by-count-accuracy"
  },
  "reduction": [
    "exact-deduplicate",
    "remove-redundant",
    "remove-optional-metadata",
    "reduce-overlap",
    "reduce-retrieval-breadth"
  ]
}
```

Values are illustrative.

---

# 271. Context Item Sketch

```json
{
  "id": "context-item-opaque",
  "source": {
    "kind": "file-snapshot",
    "project": "project-opaque",
    "relative_path": "src/Example.cs",
    "workspace_generation": 42,
    "content_sha256": "..."
  },
  "range": {
    "start_line": 20,
    "end_line": 87
  },
  "classification": "project.source",
  "trust": "project-source",
  "priority": "P1",
  "reason": "Explicit developer selection",
  "rendering": {
    "revision": "source-code-v1",
    "sha256": "..."
  },
  "tokens": {
    "profile": "tokenizer-profile-opaque",
    "count": 812,
    "method": "exact-local-tokenizer"
  }
}
```

---

# 272. Tokenizer Profile Sketch

```json
{
  "schema": "opure.tokenizer-profile/1",
  "id": "tokenizer-profile-opaque",
  "revision": 1,
  "model_profile": "model-profile-opaque",
  "request_shape": "chat",
  "tokenizer": {
    "kind": "local-runtime",
    "identity": "llama.cpp-model-tokenizer",
    "chat_template_sha256": "..."
  },
  "limits": {
    "model": "combined-window",
    "combined_tokens": 32768,
    "maximum_output_tokens": 4096
  },
  "count": {
    "method": "exact-local-runtime",
    "accuracy": "exact"
  }
}
```

---

# 273. Budget Sketch

```json
{
  "effective_limit": 32768,
  "allocation": {
    "trusted_instructions": 1200,
    "project_instructions": 500,
    "current_request": 350,
    "conversation": 1600,
    "explicit_context": 9000,
    "retrieved_context": 10500,
    "tool_definitions": 0,
    "output_schema": 0,
    "visible_output_reserve": 4096,
    "reasoning_reserve": 0,
    "safety_reserve": 512,
    "unused": 5010
  },
  "count_accuracy": "exact"
}
```

---

# 274. Context Plan Sketch

```json
{
  "schema": "opure.context-plan/1",
  "id": "context-plan-opaque",
  "operation": "operation-opaque",
  "project": "project-opaque",
  "workspace_generation": 42,
  "policy": "code-review:1",
  "model_profile": "model-profile-opaque",
  "tokenizer_profile": "tokenizer-profile-opaque:1",
  "instruction_template": "patch-review:3",
  "items": [
    "context-item-current-request",
    "context-item-project-instruction",
    "context-item-diff",
    "context-item-selected-file",
    "context-item-test-failure"
  ],
  "reductions": [
    {
      "kind": "exact-deduplicate",
      "items_removed": 2
    }
  ],
  "counts": {
    "preflight_input_tokens": 23150,
    "accuracy": "exact"
  },
  "output_reserve": 4096,
  "safety_reserve": 512,
  "expires_at": "2026-07-18T19:05:00+01:00",
  "sha256": "..."
}
```

---

# 275. Context Receipt Sketch

```json
{
  "schema": "opure.context-receipt/1",
  "operation": "operation-opaque",
  "context_plan_sha256": "...",
  "model_profile": "model-profile-opaque",
  "tokenizer_profile": "tokenizer-profile-opaque:1",
  "count": {
    "preflight_input_tokens": 23150,
    "actual_input_tokens": 23150,
    "output_tokens": 811,
    "variance": 0
  },
  "included": {
    "items": 5,
    "files": 2,
    "memory_records": 0,
    "external_results": 0
  },
  "omitted": {
    "duplicate": 2,
    "low_relevance": 4,
    "budget": 1
  },
  "result": "completed"
}
```

---

# 276. Release Gate

Context-enabled inference is blocked when:

* AI Router can read arbitrary workspace files;
* a model can select context without mediation;
* project content can enter system instructions;
* clipboard or another project appears implicitly;
* source items lack immutable snapshots;
* a secret can enter remote counting;
* a token count from another model is reused;
* a final request is not fully serialised before counting;
* tool or output schemas are omitted from the budget;
* output reserve can be silently reduced;
* provider auto-truncation is enabled;
* tokenizer auto-truncation is enabled;
* an explicitly selected item disappears silently;
* a summary is generated silently;
* a model or provider switches silently;
* stale index content can be sent;
* semantic similarity grants authority;
* plugin or MCP content can change policy;
* the preview differs from actual request content;
* or actual usage cannot be reconciled.

---

# 277. Acceptance Criteria

This ADR may move to **Accepted** when all of the following hold.

## Architecture

* [ ] Context Assembly Service is authoritative.
* [ ] AI Router does not traverse workspace.
* [ ] Context Plan is structured and immutable.
* [ ] Context Policy is versioned.
* [ ] One project binds each initial plan.
* [ ] Every item has provenance.
* [ ] Every omission has a reason.
* [ ] Plan hash covers every material field.
* [ ] Ready plans are revalidated before execution.
* [ ] Plan expiry is enforced.
* [ ] Context Receipts exist.

## Instructions and Sources

* [ ] Product instructions are separate.
* [ ] Project instructions require explicit registration.
* [ ] Magic filenames do not gain authority.
* [ ] Project instructions remain subordinate.
* [ ] Current request is mandatory.
* [ ] No ambient workspace exists.
* [ ] Clipboard is absent.
* [ ] Environment is absent.
* [ ] Cross-project sources are denied.
* [ ] Workspace items are snapshot bound.
* [ ] Build and test evidence is structured.
* [ ] Memory has source, confidence and freshness.
* [ ] Plugin results retain package provenance.
* [ ] MCP results retain server provenance.
* [ ] Prior model output remains model generated.

## Security and Classification

* [ ] Every item is classified.
* [ ] Most restrictive classification wins.
* [ ] Protected roots are denied.
* [ ] Remote plans secret scan all sources.
* [ ] Likely secrets are denied remotely.
* [ ] Redaction creates a new content hash.
* [ ] Untrusted instructions remain data.
* [ ] Semantic similarity never grants authority.
* [ ] Prompt injection cannot change product policy.
* [ ] Stale snapshots invalidate plans.
* [ ] Stale embeddings are rejected.

## Chunking and Retrieval

* [ ] Source-code chunking preserves symbols where possible.
* [ ] Line-stable fallback exists.
* [ ] Configuration formats have safe chunkers.
* [ ] Markdown, logs and diffs have specialised chunkers.
* [ ] Chunk IDs are version and content bound.
* [ ] Exact path search works.
* [ ] Symbol search works.
* [ ] Lexical search works.
* [ ] Semantic search is project scoped.
* [ ] Hybrid ranking is deterministic.
* [ ] Ranking features are inspectable.
* [ ] Exact deduplication works.
* [ ] Near-duplicate reduction protects critical differences.
* [ ] Diversity caps work.
* [ ] Generated content is penalised by default.

## Tokenizer Profiles

* [ ] Every enabled model has a Tokenizer Profile.
* [ ] Request shape is explicit.
* [ ] Tokenizer identity and version are explicit.
* [ ] Chat template is bound.
* [ ] Request renderer is versioned.
* [ ] Model limits are recorded.
* [ ] Count accuracy is recorded.
* [ ] Tokenizer changes invalidate plans.
* [ ] Mutable model aliases trigger revalidation.
* [ ] Counts are not reused across models without proven identity.

## Counting

* [ ] Exact local counting works.
* [ ] One remote count endpoint works.
* [ ] A second provider with different semantics is tested.
* [ ] Remote count is covered by cloud policy.
* [ ] Remote count is visible before transmission.
* [ ] Fully serialised request is counted.
* [ ] Role framing is counted.
* [ ] Tool definitions are counted.
* [ ] Output schemas are counted.
* [ ] Modalities are counted.
* [ ] Preliminary item counts are not final admission.
* [ ] Final recount occurs.
* [ ] Provider estimates remain labelled.
* [ ] Count cache is correctly versioned.
* [ ] Actual usage is reconciled.

## Budgeting

* [ ] Model context accounting type is explicit.
* [ ] Separate input and output limits work.
* [ ] Combined-window budgeting works.
* [ ] Provider-opaque policy works.
* [ ] Mandatory instruction cost is counted.
* [ ] Current request is counted.
* [ ] Output reserve is task specific.
* [ ] Minimum useful output is enforced.
* [ ] Reasoning reserve is represented.
* [ ] Tool continuation reserve is represented.
* [ ] Safety reserve depends on count accuracy.
* [ ] Minimum applicable model, endpoint and policy limit wins.
* [ ] Unused capacity is permitted.
* [ ] Context is not filled with low-value items.

## Reduction

* [ ] Reduction steps are versioned.
* [ ] Exact duplicates are removed first.
* [ ] Mandatory items survive.
* [ ] Explicit selections are not silently removed.
* [ ] Optional metadata is removed before direct evidence.
* [ ] Conversation decisions and constraints survive.
* [ ] Stale and low-confidence memory is reduced first.
* [ ] Diversity remains after breadth reduction.
* [ ] Structural narrowing preserves ranges.
* [ ] No silent summarisation occurs.
* [ ] No silent model switch occurs.
* [ ] No silent provider switch occurs.
* [ ] No silent output shrink occurs.
* [ ] Budget Exceeded is actionable.
* [ ] Task decomposition creates explicit multiple plans.

## Execution and Evidence

* [ ] Provider auto-truncation is disabled.
* [ ] Tokenizer auto-truncation is disabled.
* [ ] Unknown provider truncation behaviour blocks strict use.
* [ ] Actual request matches preview.
* [ ] Tool continuations create new plans.
* [ ] Workflow resume revalidates.
* [ ] Request receipts contain counts and variance.
* [ ] Receipts exclude full content by default.
* [ ] Trust Centre shows included and omitted context.
* [ ] Security review is complete.
* [ ] Privacy review is complete.
* [ ] Founder approval is recorded.

---

# 278. Evidence Required Before Acceptance

* [ ] Context Policy schema.
* [ ] Context Plan schema.
* [ ] Context Receipt schema.
* [ ] Tokenizer Profile schema.
* [ ] Chunker Profile schema.
* [ ] Instruction-layer report.
* [ ] No-ambient-context capture.
* [ ] Project-isolation report.
* [ ] Workspace snapshot report.
* [ ] Source-code chunker report.
* [ ] Configuration chunker report.
* [ ] Log and diff chunker report.
* [ ] Hybrid retrieval report.
* [ ] Ranking explainability report.
* [ ] Deduplication report.
* [ ] Diversity report.
* [ ] Secret-canary report.
* [ ] Prompt-injection corpus report.
* [ ] Local exact-token report.
* [ ] First remote count report.
* [ ] Second remote count report.
* [ ] Count-request privacy capture.
* [ ] Fully serialised counting report.
* [ ] Tool-schema budget report.
* [ ] Multimodal count report.
* [ ] Output-reserve report.
* [ ] Safety-reserve calibration.
* [ ] Deterministic reduction report.
* [ ] No-auto-truncation report.
* [ ] Budget Exceeded screenshots.
* [ ] Summary provenance report.
* [ ] Stale-index report.
* [ ] Request-byte equivalence report.
* [ ] Actual-usage variance report.
* [ ] Workflow resume report.
* [ ] Performance report.
* [ ] Scale report.
* [ ] Accessibility report.
* [ ] Security review.
* [ ] Privacy review.
* [ ] Founder approval.

---

# 279. Known Limitations

* The exact supported language parsers are not selected.
* The final lexical search engine is not selected.
* The final vector engine is not selected.
* The final embedding model is not selected.
* The final ranking weights are not selected.
* Exact tokenisation may require loading a local model.
* Remote count endpoints add latency and data transmission.
* Provider token counts may be estimates.
* Provider count and billing may differ.
* Provider-added tokens may be opaque.
* Model aliases can change tokenizers.
* Chat templates can change.
* Tool schemas can consume substantial context.
* Images and documents have provider-specific accounting.
* Reasoning-token accounting differs.
* Secret scanning has false positives and false negatives.
* Prompt injection cannot be eliminated by labelling.
* Semantic retrieval can return irrelevant content.
* Syntax parsers can fail.
* Source indexes can lag.
* AI-generated summaries can omit important facts.
* Deterministic reduction can still remove useful optional evidence.
* Large context can reduce quality.
* A model may ignore included evidence.
* Cross-project context is unavailable.
* Provider-side prompt caching is unavailable.
* Provider conversation state is unavailable.
* Automatic task decomposition is unavailable.
* Whole-repository context is unavailable.
* Hidden chain of thought is unavailable and not stored.

---

# 280. Open Questions

* Which source-code languages receive syntax-aware chunkers first?
* Should Roslyn be the first C# parser?
* Which parser libraries are acceptable for JavaScript and TypeScript?
* Which parser is used for Python without executing code?
* Should Tree-sitter be introduced?
* How are parser packages pinned and sandboxed?
* What chunk target works best for coding models?
* Should chunk target vary by task?
* What overlap is appropriate?
* How are large generated files handled?
* Should import blocks become separate context items?
* How are partial classes rendered?
* How are source generators represented?
* How are notebooks chunked?
* How are templates and mixed-language files chunked?
* Which configuration formats are included in Version 1?
* Which XML parser hardening is required?
* How are extremely large JSON files handled?
* Should minified files be rejected?
* Which log repetition algorithm is used?
* How much diff context is optimal?
* Which search engine provides lexical ranking?
* Should SQLite FTS5 be used initially?
* Which vector engine is selected?
* How are embedding indexes stored?
* Which local embedding model is selected?
* Should embeddings be generated on demand or continuously?
* What index freshness latency is acceptable?
* How are deleted chunks removed?
* How does rename detection preserve embeddings?
* Which hybrid fusion method is selected?
* What exact ranking feature weights are used?
* How are weights evaluated?
* Should reciprocal rank fusion be preferred?
* Should a local reranker be added later?
* How is diversity calculated?
* Which near-duplicate method is safe for code?
* How is vendored code classified?
* How are generated clients classified?
* Should third-party dependencies be searchable?
* Should external library source ever enter context?
* How are project instructions registered?
* Should directory-scoped instructions be supported in Version 1?
* Which repository instruction filenames should the UI suggest?
* How are conflicting project instructions resolved?
* Should project instructions require signing in enterprise use?
* How long should conversation history remain eligible?
* Which conversation decisions become memory?
* How is a user-visible conversation summary edited?
* When does a summary expire?
* Should summaries be created only by local models?
* What deterministic compression rules are sufficient?
* Which memory confidence threshold is used?
* How are memory conflicts displayed?
* Which source is authoritative when memory differs?
* How are plugin Context Policies reviewed?
* Should plugins be able to contribute chunkers?
* Should MCP prompts ever be eligible as user-selected templates?
* How are MCP resources chunked?
* How are tool results reduced?
* Which model-specific tokenizer packages are selected?
* Should Opure use provider count endpoints for every remote request?
* When may a verified local tokenizer replace remote counting?
* How is a tokenizer profile verified against provider usage?
* What variance threshold degrades a profile?
* How many calibration samples are required?
* Which languages and Unicode corpora are included?
* How is chat-template identity obtained for remote models?
* How are provider-added safety tokens represented?
* How are cached input tokens represented without provider-side caching?
* How are thinking signatures counted when a provider requires them?
* How should adaptive reasoning reserve be estimated?
* Which tasks need tool-result continuation reserve?
* Should output reserve be percentage or fixed by task?
* What minimum useful output applies to patch generation?
* How is structured-output schema cost reduced safely?
* Should tool descriptions be compressed deterministically?
* How many tools may be exposed at once?
* Should tool search be introduced?
* How are provider-native tool definitions compared with local tools?
* How are images resized?
* Should image tiling be deterministic?
* How are PDFs represented?
* When should audio context be enabled?
* Which safety-reserve percentages are appropriate?
* Should the reserve learn from observed variance?
* How is reserve change governed?
* Which budget bucket defaults are best?
* Should explicit items always outrank all supporting evidence?
* What happens when explicit items conflict?
* Should large explicit selections require a second confirmation?
* Which reduction steps may run automatically?
* When does structural narrowing require preview?
* Should the user be able to lock an optional item?
* How are locked optional items represented?
* How should task decomposition be proposed?
* How are subtask results synthesised?
* How are multiple remote call approvals grouped?
* Should derived source summaries ever be persisted in memory?
* Which summaries require human review?
* Can deterministic symbol inventories replace many source chunks?
* How is context quality evaluated without a model judge?
* Which repository tasks form the retrieval evaluation set?
* How is recall of required evidence measured?
* How is prompt-injection resistance evaluated?
* How are long-context quality regressions detected?
* Should the UI show unused capacity?
* Should users see score details or only reasons?
* How are omission reasons prioritised?
* How long are Context Receipts retained?
* Should a receipt export include rendered context?
* How is rendered context protected in support bundles?
* Can content caches deduplicate across projects safely?
* How are secret-bearing local-only contexts cached?
* Should token counts be stored for protected content?
* How does Stable share chunk caches with Preview?
* How are plan records cleaned after project deletion?
* What permanent evidence is required for a data-sharing investigation?

---

# 281. Deferred Decisions

This ADR intentionally defers:

* final parsers;
* final search engine;
* final vector engine;
* final embedding model;
* learned reranking;
* plugin-provided chunkers;
* autonomous context expansion;
* cross-project context;
* global personal memory;
* provider conversation state;
* provider-side prompt caching;
* automatic task decomposition;
* whole-repository context;
* automatic source summarisation;
* remote audio context;
* tool search;
* and hidden model reasoning.

---

# 282. Alternatives Rejected

AI Router prompt concatenation is rejected because source selection and provider execution require separate authority.

Model-controlled retrieval is rejected because a model cannot grant itself project access.

Third-party retrieval frameworks do not own product instruction or data-sharing policy.

Provider conversation state is rejected because it hides context and creates another system of record.

Whole-repository context is rejected because large windows do not eliminate relevance, privacy, cost or injection risks.

Character ratios are rejected for final admission because tokenizers and request framing differ.

Provider and tokenizer auto-truncation are rejected because context loss must be visible.

FIFO conversation truncation is rejected because old decisions may remain mandatory.

Silent AI summarisation is rejected because summaries are lossy derived content requiring provenance.

Filling the entire window is rejected because unused capacity can be safer and more useful than low-relevance context.

---

# 283. Official and Primary Evidence References

## OpenAI

* [Responses input-token counting](https://developers.openai.com/api/reference/resources/responses/subresources/input_tokens)
* [Responses API reference and truncation behaviour](https://platform.openai.com/docs/api-reference/responses)
* [OpenAI API documentation](https://developers.openai.com/api/docs)

## Anthropic

* [Token counting](https://platform.claude.com/docs/en/build-with-claude/token-counting)
* [Count tokens in a Message](https://platform.claude.com/docs/en/api/messages/count_tokens)
* [Context windows](https://platform.claude.com/docs/en/build-with-claude/context-windows)
* [Rate limits and token accounting](https://platform.claude.com/docs/en/api/rate-limits)

## Gemini

* [Counting tokens](https://ai.google.dev/api/tokens)
* [Understand and count tokens](https://ai.google.dev/gemini-api/docs/tokens)
* [Long context](https://ai.google.dev/gemini-api/docs/long-context)
* [Gemini model information](https://ai.google.dev/api/models)

## Local Tokenisation

* [`llama.cpp` server documentation](https://github.com/ggml-org/llama.cpp/blob/master/tools/server/README.md)
* [`llama.cpp` repository](https://github.com/ggml-org/llama.cpp)

## Tokenizer Behaviour

* [Hugging Face Tokenizers API](https://huggingface.co/docs/tokenizers/api/tokenizer)
* [Hugging Face tokenizer padding and truncation](https://huggingface.co/docs/transformers/main/pad_truncation)

Provider token semantics, limits, count endpoints, tokenizer implementations and truncation behaviour can change.

The implementation must revalidate each enabled Model Profile, Tokenizer Profile and provider adapter before acceptance and after every material update.

---

# 284. Review Record

| Date         | Reviewer           | Decision | Notes                                                                                                |
| ------------ | ------------------ | -------- | ---------------------------------------------------------------------------------------------------- |
| 18 July 2026 | Architecture draft | Proposed | Trusted model-specific Context Plans, exact counting and visible deterministic reduction recommended |

---

# 285. Approval

## Founder or Product Approval

* **Name:** Christopher Dyer
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Context visibility, reduction and no-silent-truncation policy review required

## Context Architecture Approval

* **Name or role:** Context Assembly and AI Router Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Context Plans, Tokenizer Profiles, budgeting and receipts required

## Workspace and Search Approval

* **Name or role:** Workspace and Search Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Snapshot, chunking, index freshness and retrieval evidence required

## Memory Approval

* **Name or role:** Memory Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Confidence, supersession and project isolation required

## Security and Privacy Approval

* **Name or role:** Security and Privacy Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Secrets, injection, wrong-project and remote-count evidence required

## Provider and Local Runtime Approval

* **Name or role:** Provider Trust and Local Model Runtime Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Model-specific tokenisation, limits and usage reconciliation required

## Test Approval

* **Name or role:** Test Architecture Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Adversarial context corpus and request-equivalence suite required

---

# 286. Supersession

This ADR is superseded only when a later ADR:

* names ADR-0021 explicitly;
* explains why context source, retrieval, tokenisation, budgeting or reduction changed;
* identifies Context Policy, Context Plan and workflow migration;
* describes provider, model, memory, cache and data-sharing impact;
* explains safety and reproducibility consequences;
* and updates the `Superseded by` field.

Historical Context Receipts, Tokenizer Profile revisions and plan hashes remain available according to retention policy.

---

# 287. Change History

| Version | Date         | Author        | Summary                                                                                                        |
| ------- | ------------ | ------------- | -------------------------------------------------------------------------------------------------------------- |
| 0.1     | 18 July 2026 | Founder Draft | Initial trusted Context Assembly, exact model-specific token budgeting and no-silent-truncation recommendation |

---

# 288. Final Decision Statement

> **Opure will provisionally assemble every AI request through a trusted Context Assembly Service that admits only authorised, project-bound and immutable source snapshots under a versioned task policy, keeps product instructions separate from untrusted project, plugin and MCP content, ranks hybrid retrieval through inspectable deterministic features, chunks and deduplicates with full provenance, counts the exact fully serialised model request through the target tokenizer or strongest approved model-specific method, reserves visible output, reasoning, tool-continuation and safety capacity, disables provider and tokenizer automatic truncation, applies only visible deterministic reduction and produces a Context Receipt reconciling planned and actual use, because context-window capacity is not authority, relevance is not trust and a software-engineering platform must never hide which developer evidence it sent, omitted or altered.**

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**
