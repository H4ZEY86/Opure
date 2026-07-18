# SPEC-005 — Memory Engine

## Opure Platform Project Knowledge and Proven Engineering Memory

**Document:** SPEC-005  
**Status:** Founder Draft  
**Version:** 0.1  
**Language:** British English  
**Last updated:** 18 July 2026  
**Depends on:** CHARTER-001, SPEC-001, SPEC-002, SPEC-003, SPEC-004  

---

## 1. Purpose

This specification defines the Memory Engine of the Opure Platform.

Within the service architecture, this capability is exposed primarily through the **Knowledge Engine** service.

The Memory Engine is responsible for creating, maintaining, retrieving, correcting and deleting durable engineering knowledge about a project.

It must help Opure understand:

- what a project contains;
- how its components relate;
- why important engineering decisions were made;
- which failures occurred;
- which fixes worked;
- which tests passed;
- which patterns have been proven;
- and what information is relevant to a current engineering task.

The Memory Engine must not be reduced to chat history or vector similarity.

It must combine structured records, direct project evidence, semantic retrieval, relationship data, validation evidence and developer-approved corrections.

---

## 2. Founding Rule

> **Project memory must strengthen developer understanding, not replace it.**

The Memory Engine must:

- preserve provenance;
- distinguish fact from inference;
- expose confidence;
- remain correctable;
- remain deletable;
- avoid hidden cross-project leakage;
- and never claim that semantic similarity proves correctness.

---

## 3. Service Naming

The roadmap document uses the title **Memory Engine**.

The service catalogue in SPEC-003 uses the title **Knowledge Engine**.

For this specification:

- **Memory Engine** refers to the complete subsystem and its internal architecture.
- **Knowledge Engine** refers to the primary service interface exposed to the rest of Opure.
- **Project Memory** refers to the knowledge namespace belonging to one project.
- **Pattern Library** refers to reusable, evidence-backed engineering patterns.
- **Context Engine** refers to the separate service that selects relevant memory and project evidence for AI or workflow tasks.

This distinction must remain clear.

---

## 4. Relationship to Other Services

A simplified logical view is:

```text
Workspace Service
Repository Service
Build Manager
Workflow Engine
Trust Centre
AI Router
        │
        ▼
   Memory Engine
        │
        ├── Knowledge Engine API
        ├── Project Memory Store
        ├── Code Structure Index
        ├── Relationship Graph
        ├── Vector Index
        ├── Decision Memory
        ├── Error and Fix Memory
        ├── Build and Test Memory
        ├── Documentation Memory
        ├── Conversation Memory
        ├── Proven Pattern Library
        ├── Confidence Engine
        ├── Provenance Registry
        ├── Reindexing Coordinator
        └── Retention and Deletion Manager
```

The Memory Engine depends on:

- Project Manager;
- Workspace Service;
- Repository Service where available;
- Storage Service;
- Scheduler;
- Policy Engine;
- Trust Centre;
- AI Router for optional embeddings and summaries;
- Build Manager for validation evidence;
- and Configuration Manager.

The Memory Engine does not own:

- project files;
- Git state;
- build execution;
- patch application;
- model selection;
- secrets;
- project policy;
- or workflow authority.

---

## 5. Design Goals

The Memory Engine must be:

- project-scoped;
- evidence-based;
- structured;
- inspectable;
- correctable;
- deletable;
- reindexable;
- model-independent;
- provider-independent;
- local-first;
- privacy-preserving;
- resilient to stale data;
- and suitable for long-term project evolution.

It should make Opure more useful over time without silently changing the meaning of the developer's project.

---

## 6. Non-Goals

The Memory Engine is not responsible for:

- training or fine-tuning foundation models;
- replacing source control;
- replacing project documentation;
- deciding whether a patch is safe to apply;
- executing builds or tests;
- granting permissions;
- storing secrets;
- proving that code is bug-free;
- or treating every conversation as permanent truth.

The Memory Engine may retain summaries or references to conversations where policy permits.

It must not assume that conversational statements are correct merely because they were said.

---

## 7. Normative Language

The terms **MUST**, **MUST NOT**, **SHOULD**, **SHOULD NOT**, **MAY** and **DEFERRED** have the meanings defined in SPEC-001.

Any intentional violation of a **SHOULD** requirement must be documented in an Architecture Decision Record.

---

# 8. Core Concepts

## 8.1 Knowledge Record

A Knowledge Record is a versioned unit of project knowledge.

A record should contain:

- record identifier;
- project identifier;
- record type;
- subject identifier;
- content;
- structured fields;
- provenance;
- confidence;
- validation evidence;
- lifecycle state;
- source version;
- creation time;
- update time;
- and deletion state.

---

## 8.2 Evidence

Evidence is the observable basis supporting a knowledge claim.

Evidence may include:

- source file content;
- source file hash;
- parser output;
- compiler output;
- test result;
- repository commit;
- build record;
- developer approval;
- documentation;
- Trust Centre record;
- tool output;
- or model-generated inference.

Evidence strength must vary by source.

---

## 8.3 Provenance

Provenance explains where a record came from and how it was produced.

Every durable record must identify:

- source type;
- source identifier;
- source location;
- source version or hash;
- producer service;
- production method;
- and timestamp.

Inferred knowledge must identify the model and prompt or workflow reference where applicable.

---

## 8.4 Confidence

Confidence describes how strongly available evidence supports a record.

Confidence must not be presented as certainty.

Confidence should account for:

- evidence type;
- evidence freshness;
- parser certainty;
- validation results;
- developer confirmation;
- conflicting evidence;
- and known limitations.

---

## 8.5 Freshness

Freshness describes whether a record still reflects the current project.

A record may be:

- current;
- possibly stale;
- stale;
- superseded;
- invalidated;
- or unknown.

Freshness must be updated when relevant source evidence changes.

---

## 8.6 Relationship

A Relationship links two or more knowledge subjects.

Examples include:

- file contains symbol;
- class implements interface;
- function calls function;
- service depends on service;
- test covers component;
- error caused by change;
- decision affects module;
- pattern derived from project;
- and patch resolved issue.

---

## 8.7 Memory Namespace

A Memory Namespace defines an isolation boundary.

Namespaces may include:

- project memory;
- user-approved shared memory;
- global Opure patterns;
- temporary workflow memory;
- and imported knowledge packs.

Project memory is isolated by default.

---

# 9. Knowledge Categories

The Memory Engine should support the following categories.

## 9.1 Project Identity

Stores:

- project name;
- stable project identifier;
- workspace roots;
- repository references;
- detected languages;
- build systems;
- package managers;
- and project-level metadata.

The Project Manager remains authoritative for project identity.

The Memory Engine stores knowledge projections and references.

---

## 9.2 File Knowledge

Stores:

- file identity;
- path;
- language;
- content hash;
- size;
- generated or source classification;
- exclusion state;
- parse state;
- and last indexed version.

The Workspace Service remains authoritative for file contents.

---

## 9.3 Symbol Knowledge

Stores:

- modules;
- namespaces;
- packages;
- classes;
- interfaces;
- functions;
- methods;
- fields;
- constants;
- routes;
- commands;
- events;
- database entities;
- and other language-specific symbols.

Each symbol should retain a source location and parser provenance.

---

## 9.4 Architecture Knowledge

Stores:

- components;
- services;
- boundaries;
- dependencies;
- protocols;
- data flows;
- deployment units;
- and architecture summaries.

Architecture knowledge may be:

- parsed;
- documented;
- developer-declared;
- inferred;
- or mixed.

Its origin must remain visible.

---

## 9.5 Decision Memory

Stores engineering decisions and their context.

A decision record should include:

- decision identifier;
- title;
- status;
- date;
- decision;
- alternatives;
- reasons;
- consequences;
- affected components;
- ADR reference where available;
- and superseding decision where applicable.

---

## 9.6 Error Memory

Stores:

- error identity;
- error signature;
- stack trace hash;
- affected component;
- environment;
- triggering operation;
- first occurrence;
- latest occurrence;
- frequency;
- and status.

Sensitive values must be redacted.

---

## 9.7 Fix Memory

Stores:

- associated error or issue;
- patch reference;
- reasoning summary;
- validation performed;
- result;
- regressions;
- developer review;
- and reuse evidence.

A fix must not be described as proven until evidence supports it.

---

## 9.8 Build Memory

Stores:

- build profile;
- command plan reference;
- environment summary;
- dependency state;
- result;
- warnings;
- errors;
- duration;
- artefacts;
- and source revision.

The Build Manager remains authoritative for build execution results.

---

## 9.9 Test Memory

Stores:

- test identity;
- test suite;
- coverage relationship;
- result;
- duration;
- source revision;
- environment;
- flaky status;
- and failure history.

Passing tests are evidence, not proof of complete correctness.

---

## 9.10 Documentation Memory

Stores indexed knowledge from:

- README files;
- architecture documents;
- API documentation;
- comments;
- ADRs;
- issue descriptions;
- changelogs;
- and developer-authored notes.

Documentation may conflict with source code.

Conflicts must remain visible.

---

## 9.11 Conversation Memory

Stores only deliberately retained conversational knowledge.

Conversation memory may include:

- approved project facts;
- developer preferences;
- decisions;
- unresolved questions;
- and task summaries.

Raw conversation retention should be limited by policy.

A conversation statement must not automatically become authoritative project fact.

---

## 9.12 Workflow Memory

Stores:

- workflow definition;
- workflow instance;
- stage outcomes;
- model and tool references;
- approvals;
- generated artefacts;
- validation;
- and final result.

The Workflow Engine remains authoritative for live workflow state.

---

## 9.13 Pattern Memory

Stores reusable patterns with evidence, compatibility and limitations.

Pattern Memory is defined in greater detail later in this specification.

---

# 10. Evidence Classes

## 10.1 Direct Source Evidence

Examples:

- current file content;
- parsed syntax tree;
- current configuration;
- repository state;
- current test output.

Direct source evidence generally has high authority within its scope.

---

## 10.2 Tool-Verified Evidence

Examples:

- compiler success;
- linter result;
- type-check result;
- test result;
- package-manager resolution;
- runtime probe.

Tool verification must identify the exact tool and version where possible.

---

## 10.3 Developer-Confirmed Evidence

Examples:

- approved architecture decision;
- manually confirmed project convention;
- reviewed fix;
- accepted pattern promotion.

Developer confirmation is strong evidence but may later become stale.

---

## 10.4 Documentation Evidence

Examples:

- README;
- ADR;
- design document;
- code comment.

Documentation authority depends on freshness and consistency with source evidence.

---

## 10.5 Model-Inferred Evidence

Examples:

- inferred component purpose;
- inferred relationship;
- generated summary;
- likely error cause.

Model inference must be labelled clearly.

It must not override contradictory direct evidence.

---

## 10.6 External Evidence

Examples:

- package documentation;
- public API documentation;
- issue tracker;
- remote repository metadata.

External evidence must retain source and retrieval date.

Remote retrieval remains subject to cloud and network policy.

---

# 11. Confidence Model

## 11.1 Confidence Dimensions

Confidence should be represented across dimensions rather than one opaque number.

Recommended dimensions include:

- source authority;
- validation strength;
- freshness;
- consistency;
- developer confirmation;
- and inference risk.

---

## 11.2 Confidence States

A simplified user-facing state may be:

- **Unverified**
- **Inferred**
- **Supported**
- **Verified**
- **Developer Confirmed**
- **Conflicted**
- **Stale**
- **Invalidated**

These states must be derived from evidence.

---

## 11.3 Conflicting Evidence

When evidence conflicts, the Memory Engine must:

1. preserve both claims where relevant;
2. identify the conflict;
3. avoid silently choosing the more convenient claim;
4. prefer stronger and fresher evidence for retrieval ranking;
5. and allow developer resolution.

---

## 11.4 Confidence Decay

Some records should lose confidence as their evidence ages.

Examples include:

- dependency versions;
- build commands;
- runtime behaviour;
- file summaries;
- and inferred relationships.

Historical decisions should not decay merely because they are old, but may become superseded.

---

## 11.5 No False Precision

The interface should avoid presenting unsupported confidence such as `97.3% certain`.

Numeric confidence may be used internally if justified.

User-facing explanations should identify the evidence instead.

---

# 12. Lifecycle States

A knowledge record should use one primary lifecycle state:

- **Observed**
- **Inferred**
- **Validated**
- **Confirmed**
- **Superseded**
- **Stale**
- **Invalidated**
- **Deleted**

Lifecycle and confidence are related but distinct.

A validated record may later become stale.

A confirmed decision may later be superseded.

---

# 13. Provenance Registry

## 13.1 Purpose

The Provenance Registry tracks the origin and transformation history of knowledge.

---

## 13.2 Required Provenance Fields

Every durable record must include:

- producer;
- production method;
- source reference;
- source version or hash;
- transformation steps;
- model reference where applicable;
- tool reference where applicable;
- workflow reference where applicable;
- and creation timestamp.

---

## 13.3 Transformation Chain

If a summary is derived from parsed files, its provenance should link:

```text
Source File
    ↓
Parser Output
    ↓
Structured Symbols
    ↓
Architecture Summary
```

The developer must be able to trace the summary back to evidence.

---

## 13.4 Provenance Integrity

Provenance records should be append-only where practical.

Corrections should create a new version rather than silently rewriting history.

---

# 14. Project Isolation

## 14.1 Default Isolation

Each project must have an isolated memory namespace.

A query in one project must not retrieve another project's private records by default.

---

## 14.2 Shared Memory

Cross-project sharing may be enabled for:

- developer-authored conventions;
- approved reusable patterns;
- shared libraries;
- organisation-wide standards;
- or explicitly linked projects.

Sharing must be visible and revocable.

---

## 14.3 Data Classification

Shared records must retain their data classification.

A shared index must not make restricted project content available to unrelated projects.

---

## 14.4 Copy Versus Reference

Cross-project knowledge should prefer references when possible.

Copying private source content into another project's memory requires explicit approval.

---

## 14.5 Project Deletion

Deleting a project from Opure must offer to remove:

- structured project memory;
- vector indexes;
- graph data;
- cached summaries;
- temporary embeddings;
- and project-specific pattern candidates.

Shared patterns derived from the project must identify their origin and follow separate deletion rules.

---

# 15. Workspace Ingestion

## 15.1 Ingestion Trigger

Ingestion may occur when:

- a project is opened;
- a file changes;
- a repository revision changes;
- a build completes;
- a workflow requests deeper analysis;
- the developer requests reindexing;
- or a parser is upgraded.

---

## 15.2 Staged Ingestion

Ingestion should proceed in stages:

1. workspace discovery;
2. exclusion application;
3. file classification;
4. hashing;
5. parser selection;
6. structural extraction;
7. relationship extraction;
8. summary generation where useful;
9. embedding generation where useful;
10. validation;
11. persistence;
12. and Trust Centre recording.

---

## 15.3 Exclusions

The Memory Engine must respect Workspace Service exclusions.

Default exclusions should include common:

- dependency directories;
- build outputs;
- caches;
- binaries;
- generated artefacts;
- model files;
- secrets;
- and temporary directories.

The developer must be able to inspect exclusions.

---

## 15.4 Incremental Ingestion

Unchanged files should not be reparsed unnecessarily.

Incremental ingestion should use:

- content hashes;
- parser version;
- configuration version;
- and dependency change information.

---

## 15.5 Large Projects

For large projects, ingestion must support:

- prioritisation;
- bounded concurrency;
- progress reporting;
- cancellation;
- resumability;
- and selective indexing.

The interface should identify which parts are indexed and which are not.

---

## 15.6 Generated Files

Generated files should be classified separately.

Generated content may be indexed when useful but should not dominate retrieval or pattern creation.

---

# 16. Code Parsing

## 16.1 Parser Interface

Language parsers must implement a versioned contract.

A parser should provide:

- language identifier;
- parser version;
- file support;
- syntax diagnostics;
- symbols;
- source ranges;
- imports;
- exports;
- references;
- calls;
- inheritance;
- interfaces;
- annotations;
- and parser confidence.

---

## 16.2 Parser Sources

Parsers may use:

- compiler APIs;
- language servers;
- tree-sitter;
- native parsers;
- static-analysis tools;
- or custom adapters.

Exact parser choices are DEFERRED to ADRs.

---

## 16.3 Parse Failure

A parse failure must not discard the file.

The record should retain:

- parse error;
- partial extraction where safe;
- parser version;
- and retry eligibility.

---

## 16.4 Multi-Language Files

Files containing multiple languages should support nested language regions where practical.

Examples include:

- HTML with JavaScript;
- Markdown with code blocks;
- templates;
- notebooks;
- and embedded SQL.

---

## 16.5 Source Positions

Symbols and relationships must retain precise source positions where available.

Positions should include:

- file identifier;
- line and column;
- byte or character range;
- and source hash.

---

# 17. Relationship Graph

## 17.1 Purpose

The Relationship Graph represents how project entities connect.

---

## 17.2 Node Types

Nodes may include:

- project;
- file;
- module;
- symbol;
- service;
- dependency;
- test;
- error;
- fix;
- decision;
- workflow;
- patch;
- build;
- pattern;
- and external system.

---

## 17.3 Edge Types

Edges may include:

- contains;
- imports;
- exports;
- calls;
- implements;
- extends;
- depends on;
- reads;
- writes;
- produces;
- validates;
- tests;
- documents;
- caused by;
- resolved by;
- supersedes;
- derived from;
- similar to;
- and approved by.

---

## 17.4 Evidence on Edges

Every edge should retain:

- source evidence;
- extraction method;
- confidence;
- freshness;
- and source version.

---

## 17.5 Graph Queries

The Knowledge Engine should support queries such as:

- what depends on this component;
- which tests cover this function;
- which decisions affect this service;
- which fixes resolved similar errors;
- what may be impacted by changing this interface;
- and which workflows touched this file.

---

## 17.6 Graph Storage

The logical graph contract must remain storage-neutral.

The initial implementation may store relationships in relational tables.

A dedicated graph engine is not required unless evidence justifies it.

---

# 18. Vector Index

## 18.1 Purpose

The Vector Index supports semantic retrieval.

It may help locate:

- similar code;
- relevant documentation;
- related errors;
- prior fixes;
- architectural concepts;
- and reusable patterns.

---

## 18.2 Authority Rule

> **Vector similarity is a retrieval hint, not proof.**

A high similarity score must not be treated as evidence that:

- code is correct;
- code is compatible;
- a fix will work;
- an architecture is appropriate;
- or two issues share the same cause.

---

## 18.3 Embedding Provenance

Every vector must retain:

- source record;
- source content hash;
- embedding model;
- provider;
- model version or digest;
- vector dimension;
- normalisation method;
- chunking method;
- and creation time.

---

## 18.4 Compatible Vector Spaces

Vectors from incompatible models or dimensions must not be compared directly.

The Memory Engine must maintain separate indexes or migrate them deliberately.

---

## 18.5 Local Preference

Project embeddings should be generated locally where practical.

Remote embeddings require project policy and external-sharing approval.

---

## 18.6 Secret Exclusion

Secrets and credential values must not be embedded.

Secret detection must run before embedding content.

---

## 18.7 Chunking

Chunking should respect semantic boundaries where possible.

Examples include:

- function;
- class;
- section;
- document heading;
- test case;
- error record;
- and decision record.

Arbitrary fixed-size chunking may be used as fallback but must retain source boundaries.

---

## 18.8 Reindexing

A reindex may be required when:

- embedding model changes;
- dimension changes;
- chunking strategy changes;
- parser output changes materially;
- source content changes;
- or the index is corrupted.

Reindexing must be resumable and cancellable.

---

# 19. Search and Retrieval

## 19.1 Retrieval Types

The Knowledge Engine should support:

- exact lookup;
- structured filtering;
- graph traversal;
- full-text search;
- vector similarity;
- hybrid retrieval;
- and evidence-ranked retrieval.

---

## 19.2 Hybrid Retrieval

Hybrid retrieval may combine:

- lexical relevance;
- vector similarity;
- graph proximity;
- freshness;
- confidence;
- project scope;
- developer confirmation;
- and task-specific relevance.

---

## 19.3 Retrieval Explanation

A result should explain why it was returned.

Examples:

- exact symbol match;
- imported by current file;
- semantically similar to error;
- developer-confirmed project convention;
- recent validated fix;
- or linked through architecture relationship.

---

## 19.4 Result Provenance

Every retrieved item must retain its provenance and current lifecycle state.

Stale or conflicted items must be labelled.

---

## 19.5 Query Scope

A query must declare scope such as:

- current file;
- current module;
- current project;
- selected projects;
- shared patterns;
- or global public knowledge.

Broader scope must not be assumed silently.

---

## 19.6 Retrieval Limits

Retrieval must use bounded result counts and size budgets.

The Context Engine decides which retrieved records enter an AI context package.

---

# 20. Summaries

## 20.1 Purpose

Summaries may improve retrieval and understanding.

Examples include:

- file summary;
- module summary;
- service summary;
- architecture summary;
- build summary;
- and decision summary.

---

## 20.2 Summary Provenance

A generated summary must identify:

- source records;
- source versions;
- model;
- prompt template;
- date;
- and validation state.

---

## 20.3 Summary Freshness

A summary becomes stale when its source records change materially.

Stale summaries must not be presented as current.

---

## 20.4 Deterministic Summaries

Where structured data is sufficient, Opure should prefer deterministic summaries.

Model-generated summaries should be used when they add real value.

---

## 20.5 Developer Editing

The developer may edit or replace a summary.

Developer-authored summaries should be distinguished from model-generated ones.

---

# 21. Conversation Memory

## 21.1 Default Behaviour

Raw conversations must not automatically become permanent project truth.

---

## 21.2 Retention Modes

A project may support:

- **Do Not Retain**
- **Retain Task Summaries**
- **Retain Approved Decisions**
- **Retain Full Conversation Locally**
- **Custom Retention**

The default is DEFERRED to SPEC-008 or SPEC-010.

---

## 21.3 Promotion

A conversational statement may be promoted into project memory when:

- the developer explicitly approves it;
- a workflow records an accepted decision;
- or direct evidence confirms it.

---

## 21.4 Sensitive Conversations

Conversation memory must respect:

- project isolation;
- secret redaction;
- cloud policy;
- and deletion requests.

---

## 21.5 Correction

The developer must be able to correct retained conversational knowledge without deleting the entire conversation.

---

# 22. Decision Memory

## 22.1 ADR Integration

Architecture Decision Records should be indexed as authoritative decision evidence.

---

## 22.2 Decision Status

Decision status may include:

- proposed;
- accepted;
- rejected;
- deprecated;
- superseded;
- and withdrawn.

---

## 22.3 Decision Impact

A decision should link to:

- affected services;
- affected specifications;
- affected files;
- affected patterns;
- and related Trust Centre records.

---

## 22.4 Conflicts

When implementation conflicts with an accepted decision, the Memory Engine should surface the conflict.

It must not enforce architecture directly.

---

# 23. Error and Fix Memory

## 23.1 Error Fingerprinting

Errors should be grouped using safe fingerprints based on:

- error code;
- exception type;
- stack structure;
- message pattern;
- affected component;
- and environment.

Fingerprints must redact secrets and unstable values.

---

## 23.2 Similar Error Retrieval

Semantic and structural similarity may identify related errors.

Similarity must not imply identical cause.

---

## 23.3 Fix Evidence

A fix record should include:

- issue or error reference;
- patch;
- source revision;
- tests;
- build result;
- runtime result where available;
- developer review;
- and regression history.

---

## 23.4 Failed Fixes

Failed fixes should be retained where useful.

They must be labelled clearly and should prevent repeated wasteful attempts.

---

## 23.5 Regression

A fix that later causes regression must have its confidence reduced and lifecycle updated.

---

# 24. Proven Pattern Library

## 24.1 Purpose

The Pattern Library stores reusable engineering solutions supported by evidence.

It is not a warehouse of arbitrary generated code.

---

## 24.2 Pattern Types

Patterns may include:

- implementation pattern;
- architecture pattern;
- configuration pattern;
- testing pattern;
- error-handling pattern;
- migration pattern;
- integration pattern;
- deployment pattern;
- and workflow pattern.

---

## 24.3 Pattern Record

A pattern record should include:

- pattern identifier;
- title;
- purpose;
- category;
- language;
- framework;
- dependencies;
- assumptions;
- source;
- content;
- adaptation guidance;
- validation evidence;
- known limitations;
- compatibility range;
- confidence level;
- reuse history;
- and deprecation status.

---

## 24.4 Pattern Lifecycle

A pattern may progress through:

- **Draft**
- **Compiled**
- **Tested**
- **Reviewed**
- **Proven**
- **Trusted**
- **Deprecated**
- **Rejected**

---

## 24.5 Promotion Rules

Promotion requires evidence.

### Draft to Compiled

Requires successful parse or build where applicable.

### Compiled to Tested

Requires relevant tests to pass.

### Tested to Reviewed

Requires developer review.

### Reviewed to Proven

Requires successful use in a real project context.

### Proven to Trusted

Requires repeated successful reuse with no known regression across relevant cases.

---

## 24.6 No Bug-Free Claim

No pattern may be labelled bug-free.

Pattern confidence must describe known evidence and limitations.

---

## 24.7 Pattern Extraction

A pattern may be proposed from:

- a successful fix;
- a reusable component;
- an accepted architecture decision;
- a validated workflow;
- or developer-authored content.

Automatic extraction creates a candidate only.

It does not create a Trusted pattern.

---

## 24.8 Private Project Code

Private project code must not enter a global or cross-project Pattern Library without explicit approval.

The developer must be shown:

- source content proposed;
- redactions;
- abstraction performed;
- destination namespace;
- and reuse scope.

---

## 24.9 Generalisation

A project-specific implementation may be generalised before promotion.

Generalisation must remove:

- secrets;
- project-specific identifiers;
- private business logic;
- private endpoints;
- and unnecessary proprietary content.

---

## 24.10 Compatibility

A pattern must identify compatibility assumptions.

Examples include:

- language version;
- framework version;
- operating system;
- database;
- runtime;
- provider;
- and package versions.

---

## 24.11 Pattern Retrieval

Pattern search should consider:

- task requirements;
- project compatibility;
- validation strength;
- recency;
- known limitations;
- developer preference;
- and reuse success.

---

## 24.12 Pattern Adaptation

Using a pattern should create a new proposed patch or workflow step.

A stored pattern must not be copied into the project without review and adaptation.

---

## 24.13 Deprecation

Patterns should be deprecated when:

- dependencies are insecure;
- APIs are obsolete;
- regressions are found;
- better patterns replace them;
- or assumptions are no longer valid.

Deprecated patterns may remain visible for historical understanding.

---

# 25. Knowledge Updates

## 25.1 Update Triggers

Knowledge may update when:

- a file changes;
- a patch is applied;
- a build completes;
- tests complete;
- a commit is created;
- a decision is approved;
- a workflow completes;
- a pattern is reused;
- or a developer edits memory.

---

## 25.2 Event-Driven Updates

The Memory Engine should subscribe to relevant platform events.

Examples include:

- `ProjectOpened`
- `FileChanged`
- `PatchApplied`
- `BuildCompleted`
- `TestCompleted`
- `CommitCreated`
- `DecisionAccepted`
- `WorkflowCompleted`

---

## 25.3 Reconciliation

Event-driven updates may be missed during failure.

The Memory Engine must support reconciliation against authoritative services.

---

## 25.4 Update Idempotency

Repeated events must not create duplicate records.

Event handling should use stable identifiers and source versions.

---

## 25.5 Stale Marking

When a source changes, dependent records should be marked stale before replacement is complete.

---

# 26. Correction

## 26.1 Developer Correction

The developer must be able to correct:

- project facts;
- summaries;
- relationships;
- conventions;
- decisions;
- and pattern metadata.

---

## 26.2 Correction Record

A correction should include:

- original record;
- corrected value;
- reason;
- developer identity;
- date;
- and affected dependent records.

---

## 26.3 Correction Precedence

Developer correction may override inferred knowledge.

It must not silently override contradictory current source evidence.

Instead, the conflict should remain visible.

---

## 26.4 Learning from Correction

Repeated corrections may inform:

- parser improvements;
- prompt-template changes;
- retrieval ranking;
- and model-selection preferences.

Corrections must not be used for external model training without explicit approval.

---

# 27. Deletion and Forgetting

## 27.1 Right to Delete

The developer must be able to delete project memory.

Deletion must include:

- structured records;
- vector indexes;
- graph edges;
- summaries;
- caches;
- and temporary artefacts.

---

## 27.2 Selective Deletion

The developer should be able to delete:

- one record;
- one conversation;
- one file's memory;
- one category;
- one project's memory;
- or all user memory.

---

## 27.3 Tombstones

Deletion may use tombstones temporarily for synchronisation or recovery.

Tombstones must not retain deleted private content.

---

## 27.4 Shared Pattern Impact

If deleted project knowledge contributed to a shared pattern, Opure must identify:

- whether source content remains embedded;
- whether the pattern can be retained safely;
- whether revalidation is required;
- and whether deletion must propagate.

---

## 27.5 Backups

Deletion behaviour must explain backup retention.

A developer must not be told data is fully erased if backups still contain it.

---

## 27.6 Rebuild

After deletion, indexes must rebuild without restoring deleted content from stale caches.

---

# 28. Retention

## 28.1 Retention Policies

Retention may vary by category.

Examples:

- source-derived structure retained while project exists;
- temporary AI context discarded after task;
- raw provider responses retained briefly or not at all;
- Trust Centre references retained by Trust Centre policy;
- obsolete embeddings removed after successful migration;
- and failed pattern candidates expired after review.

---

## 28.2 Local Default

Memory should remain local by default.

Remote synchronisation is not required for the initial implementation.

---

## 28.3 Retention Visibility

The developer should be able to inspect:

- what is retained;
- why;
- where;
- size;
- last use;
- and deletion controls.

---

# 29. Storage Architecture

## 29.1 Logical Stores

The Memory Engine may use separate logical stores for:

- structured metadata;
- relationships;
- full-text search;
- vectors;
- blobs;
- and journals.

---

## 29.2 Storage Neutrality

Service contracts must not expose storage-engine-specific behaviour.

---

## 29.3 Initial Storage

The initial implementation may use:

- SQLite for structured records and relationships;
- an embedded vector index;
- filesystem-backed blobs;
- and transactional journals.

Exact choices are DEFERRED to ADRs.

---

## 29.4 Storage Isolation

Each project should have an isolated logical namespace.

Global patterns must use a separate namespace.

---

## 29.5 Migrations

Schema migrations must be:

- versioned;
- transactional where practical;
- recoverable;
- tested;
- and recorded.

---

## 29.6 Backup

Memory backups must not include secret values.

Backup scope and restoration must remain visible.

---

# 30. Indexing Scheduler

## 30.1 Task Priority

Interactive retrieval should take priority over background indexing.

---

## 30.2 Resource Awareness

Indexing should account for:

- CPU;
- memory;
- GPU or VRAM for embeddings;
- disk activity;
- battery or power mode where available;
- and active developer work.

---

## 30.3 Performance Modes

### Eco

- delay background embedding;
- reduce concurrency;
- prefer structural indexing;
- and avoid loading heavy embedding models.

### Balanced

- index incrementally;
- use a suitable local embedding model where configured;
- and yield to foreground tasks.

### Performance

- increase indexing concurrency;
- process more pending files;
- and allow heavier local models.

### Turbo

- maximise throughput within approved resource limits.

---

## 30.4 Cancellation

Indexing must be cancellable.

Cancellation should preserve completed valid records and journals.

---

## 30.5 Resumability

Large reindex operations should resume from checkpoints.

---

# 31. AI Use Within Memory

## 31.1 Optional Capability

The Memory Engine may use AI for:

- summaries;
- relationship suggestions;
- classification;
- error clustering;
- and pattern generalisation.

AI use must be optional.

---

## 31.2 Local Preference

Memory-related AI tasks should prefer local providers where suitable.

---

## 31.3 Provider Routing

All AI use must pass through the AI Router.

---

## 31.4 Inference Labels

AI-derived records must be labelled as inferred until validated or confirmed.

---

## 31.5 No Autonomous Truth

A model-generated statement must not become authoritative solely because it sounds plausible.

---

## 31.6 Prompt Visibility

Memory-generation prompts should be inspectable through Trust Centre links where relevant.

---

# 32. Context Engine Integration

## 32.1 Responsibility Boundary

The Memory Engine retrieves knowledge.

The Context Engine chooses what enters a task context.

---

## 32.2 Retrieval Package

A retrieval package should include:

- query;
- scope;
- filters;
- result records;
- relevance explanation;
- confidence;
- freshness;
- provenance;
- and size estimate.

---

## 32.3 Context Budget

The Context Engine may request narrower or summarised results.

The Memory Engine should not over-return large content merely because it exists.

---

## 32.4 External Sharing

Before knowledge enters a remote AI request, the Context Engine and Policy Engine must enforce:

- project cloud policy;
- data classification;
- exclusions;
- secret detection;
- and approval.

---

# 33. Trust Centre Integration

## 33.1 Significant Records

The Memory Engine should record:

- initial project indexing;
- deep project analysis;
- full reindex;
- embedding model change;
- cross-project sharing;
- pattern promotion;
- pattern deprecation;
- developer correction;
- bulk deletion;
- and memory migration.

---

## 33.2 Routine Activity

Routine low-risk file indexing may be summarised rather than recorded file by file.

---

## 33.3 Visibility

The developer should be able to inspect:

- what was indexed;
- what was excluded;
- which model was used for embeddings;
- which summaries were AI-generated;
- which records are stale;
- and which patterns are shared.

---

## 33.4 Secret Safety

Trust records must not contain secret values or private content unnecessarily.

---

# 34. Privacy and Security

## 34.1 Secret Exclusion

Secrets must not enter:

- structured memory;
- vector indexes;
- summaries;
- pattern content;
- logs;
- or Trust Centre records.

---

## 34.2 Sensitive Files

Sensitive files may be excluded entirely or indexed only as safe metadata.

---

## 34.3 Access Control

Memory queries must respect:

- project scope;
- user identity;
- workflow permission;
- plugin permission;
- and data classification.

---

## 34.4 Plugin Access

Plugins must declare memory capabilities such as:

- read project structure;
- search documentation;
- write proposed knowledge;
- or access shared patterns.

Plugins must not receive unrestricted memory access by default.

---

## 34.5 MCP Access

MCP servers may access memory only through controlled capabilities.

MCP access must be policy checked and auditable.

---

## 34.6 External Export

Exporting memory outside the machine requires visible scope and approval.

---

# 35. Import and Export

## 35.1 Export

The developer should be able to export:

- project summaries;
- decisions;
- structured symbol data;
- relationship data;
- pattern metadata;
- and selected memory categories.

---

## 35.2 Export Format

Exports should use documented formats.

Possible formats include:

- JSON;
- JSON Lines;
- Markdown;
- CSV for tabular records;
- and GraphML or equivalent for relationships.

Exact formats are DEFERRED.

---

## 35.3 Import

Imported memory must be treated as untrusted until validated.

Import must retain:

- source;
- import date;
- scope;
- and validation state.

---

## 35.4 Knowledge Packs

Opure may support portable knowledge packs.

A knowledge pack should declare:

- publisher;
- contents;
- licence;
- compatibility;
- signatures where available;
- and requested project scope.

---

# 36. API Contract

## 36.1 Core Commands

The Knowledge Engine should provide commands such as:

- `IndexProject`
- `IndexFile`
- `ReindexProject`
- `RecordKnowledge`
- `CorrectKnowledge`
- `DeleteKnowledge`
- `PromotePattern`
- `DeprecatePattern`
- `ShareKnowledge`
- `RevokeSharedKnowledge`
- and `MigrateIndex`

---

## 36.2 Core Queries

The Knowledge Engine should provide queries such as:

- `GetProjectSummary`
- `GetFileKnowledge`
- `GetSymbol`
- `SearchKnowledge`
- `TraverseRelationships`
- `FindSimilarErrors`
- `FindRelevantFixes`
- `SearchPatterns`
- `GetKnowledgeProvenance`
- `GetKnowledgeConfidence`
- `GetIndexStatus`
- and `GetMemoryUsage`

---

## 36.3 Core Events

The Knowledge Engine should publish events such as:

- `ProjectIndexingStarted`
- `ProjectIndexingCompleted`
- `KnowledgeRecorded`
- `KnowledgeCorrected`
- `KnowledgeInvalidated`
- `KnowledgeDeleted`
- `IndexBecameStale`
- `ReindexStarted`
- `ReindexCompleted`
- `PatternProposed`
- `PatternPromoted`
- `PatternDeprecated`
- and `SharedKnowledgeRevoked`

---

# 37. Error Model

Recommended stable error categories include:

- `MEMORY_PROJECT_NOT_FOUND`
- `MEMORY_RECORD_NOT_FOUND`
- `MEMORY_SCOPE_DENIED`
- `MEMORY_SOURCE_UNAVAILABLE`
- `MEMORY_PARSE_FAILED`
- `MEMORY_INDEX_CORRUPT`
- `MEMORY_VECTOR_INCOMPATIBLE`
- `MEMORY_EMBEDDING_FAILED`
- `MEMORY_REINDEX_REQUIRED`
- `MEMORY_CONFLICT`
- `MEMORY_STALE`
- `MEMORY_PATTERN_NOT_ELIGIBLE`
- `MEMORY_PATTERN_VALIDATION_FAILED`
- `MEMORY_DELETE_FAILED`
- `MEMORY_MIGRATION_FAILED`
- `MEMORY_CANCELLED`
- and `MEMORY_INTERNAL_ERROR`

Errors must include safe recovery guidance where possible.

---

# 38. Testing Strategy

## 38.1 Unit Tests

Unit tests must cover:

- provenance;
- confidence derivation;
- lifecycle transitions;
- freshness invalidation;
- namespace isolation;
- vector compatibility;
- pattern promotion;
- deletion;
- correction;
- and retrieval ranking.

---

## 38.2 Parser Contract Tests

Every parser adapter must pass tests for:

- valid file;
- invalid file;
- partial parse;
- symbol extraction;
- source positions;
- relationships;
- and parser version changes.

---

## 38.3 Index Tests

Tests must cover:

- initial index;
- incremental update;
- deleted file;
- renamed file;
- parser upgrade;
- embedding-model change;
- interrupted reindex;
- and corrupted index.

---

## 38.4 Isolation Tests

Tests must prove:

- one project cannot retrieve another project's private memory;
- shared patterns require approval;
- revoked sharing stops retrieval;
- and deleted project memory does not reappear.

---

## 38.5 Security Tests

Tests must cover:

- secret exclusion;
- path-boundary enforcement;
- malicious imported knowledge;
- plugin overreach;
- MCP overreach;
- and external export approval.

---

## 38.6 Pattern Tests

Tests must prove:

- Draft cannot skip required promotion evidence;
- failed tests prevent Tested status;
- developer review is required for Reviewed;
- real use is required for Proven;
- repeated successful reuse is required for Trusted;
- and regression reduces confidence.

---

## 38.7 Retrieval Tests

Tests should measure:

- exact lookup;
- lexical retrieval;
- vector retrieval;
- hybrid ranking;
- stale-result labelling;
- conflict visibility;
- and provenance completeness.

---

## 38.8 Recovery Tests

Tests must cover:

- process crash during indexing;
- disk full;
- interrupted migration;
- incomplete deletion;
- vector-index corruption;
- and stale journal recovery.

---

# 39. Performance Requirements

## 39.1 Interactive Retrieval

Common project-memory queries should return promptly for ordinary projects.

Exact targets are DEFERRED until measurable prototypes exist.

---

## 39.2 Background Indexing

Background indexing must not make the desktop unresponsive.

---

## 39.3 Incremental Updates

A small file change should not trigger full project reindexing unless required by parser or dependency semantics.

---

## 39.4 Storage Visibility

The developer should be able to inspect memory storage by category:

- structured records;
- vectors;
- graph;
- summaries;
- patterns;
- and temporary data.

---

## 39.5 Large Repositories

The system should support selective and staged indexing for large repositories.

---

# 40. Initial Implementation Milestone

The first Memory Engine milestone is successful when it can:

1. create an isolated memory namespace for a project;
2. index project files incrementally;
3. store file and symbol records;
4. retain source hashes and provenance;
5. build basic relationships;
6. perform exact and full-text search;
7. generate local embeddings through the AI Router;
8. perform vector and hybrid retrieval;
9. prevent incompatible vector comparison;
10. mark records stale when files change;
11. correct a record;
12. delete a record;
13. rebuild an index;
14. retain build and test evidence;
15. record an error and associated fix;
16. create a Draft pattern candidate;
17. promote a pattern only with evidence;
18. keep project memory isolated;
19. exclude secrets;
20. and expose progress, cancellation and health.

The milestone may initially support a limited set of programming languages.

---

# 41. Acceptance Criteria

SPEC-005 is implemented when:

- [ ] Every durable record has provenance.
- [ ] Project memory is isolated by default.
- [ ] Workspace files remain authoritative for file content.
- [ ] Structured evidence outranks unsupported inference.
- [ ] Model-generated records are labelled as inferred.
- [ ] Records expose freshness and confidence.
- [ ] Conflicting evidence remains visible.
- [ ] File changes invalidate dependent records.
- [ ] Incremental indexing avoids unnecessary full reindexing.
- [ ] Parser versions are recorded.
- [ ] Relationships retain evidence.
- [ ] Vector records retain exact embedding identity.
- [ ] Incompatible vector spaces are never compared.
- [ ] Secrets are excluded from memory and embeddings.
- [ ] Cross-project retrieval requires explicit sharing.
- [ ] Conversation statements do not automatically become project truth.
- [ ] Developers can correct memory.
- [ ] Developers can selectively delete memory.
- [ ] Deleted records do not reappear from stale caches.
- [ ] Reindexing is cancellable and resumable.
- [ ] Failed indexing does not corrupt valid existing memory.
- [ ] Pattern promotion requires evidence.
- [ ] No pattern is labelled bug-free.
- [ ] Private code does not enter shared patterns without approval.
- [ ] Deprecated and regressed patterns remain identifiable.
- [ ] Retrieval results explain why they were returned.
- [ ] Stale and conflicted results are labelled.
- [ ] Memory export is inspectable.
- [ ] Plugins and MCP servers receive scoped memory access only.
- [ ] Trust Centre records contain no secret values.
- [ ] The Memory Engine remains functional without cloud providers.
- [ ] The project remains usable if all memory data is deleted.

---

# 42. Deferred Decisions

The following are intentionally deferred:

- exact relational database;
- vector-index technology;
- graph-storage technology;
- full-text search engine;
- parser technologies per language;
- default embedding model;
- chunking algorithm;
- ranking weights;
- confidence formula;
- default conversation retention;
- shared pattern packaging;
- organisation-level knowledge;
- remote memory synchronisation;
- collaborative editing;
- and public knowledge marketplace.

These decisions must not weaken:

- project isolation;
- provenance;
- correction;
- deletion;
- local-first operation;
- and evidence-based confidence.

---

# 43. Required Architecture Decision Records

Implementation should produce ADRs for:

- structured-memory storage;
- vector-index technology;
- relationship-graph storage;
- full-text search;
- parser framework;
- embedding model and migration;
- chunking strategy;
- confidence representation;
- provenance storage;
- project namespace isolation;
- pattern-library storage;
- deletion and backup behaviour;
- and conversation-retention defaults.

---

# 44. Relationship to Later Specifications

This specification provides the memory foundation for:

- **SPEC-006 — Workflow and Agent System**
- **SPEC-007 — Plugin SDK**
- **SPEC-008 — Trust Centre and Security**
- **SPEC-009 — Workspace and File Patch Engine**
- **SPEC-010 — Desktop User Interface**
- **SPEC-011 — Project and Build Management**

SPEC-006 will define how workflows retrieve and update memory.

SPEC-008 will define detailed data classification, retention, access control and deletion security.

SPEC-009 will define file identities, patch relationships and source-change events in greater detail.

Later specifications may add stricter requirements.

They must not treat vector similarity as proof or bypass project isolation.

---

# 45. Founder Approval

This document remains a founder draft until explicitly approved.

Approval establishes the following rules:

- Opure memory is structured engineering knowledge, not merely chat history.
- Project memory is isolated by default.
- Every durable record has provenance.
- Facts, inference, validation and developer confirmation remain distinguishable.
- Vector similarity assists retrieval but never proves correctness.
- Knowledge can be corrected, invalidated, reindexed and deleted.
- Secrets do not enter project memory or vector indexes.
- Reusable patterns progress through evidence-based lifecycle states.
- Private code cannot become shared pattern content without approval.
- Failed fixes and regressions remain part of engineering memory.
- The Memory Engine improves project understanding without replacing developer judgement.

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**