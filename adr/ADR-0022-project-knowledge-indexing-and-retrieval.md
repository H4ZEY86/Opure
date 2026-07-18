# ADR-0022 — Project Knowledge Indexing and Retrieval

## Architecture Decision Record

**Status:** Proposed
**Date:** 18 July 2026
**Decision owners:** Founder and Product Owner
**Technical owners:** Project Knowledge and Retrieval Owner
**Reviewers:** Runtime Architecture Owner, Context Assembly Owner, Workspace Owner, Repository Owner, Memory Owner, AI Router Owner, Local Model Runtime Owner, Provider Trust Owner, Security Owner, Secrets Owner, Plugin Platform Owner, MCP Gateway Owner, Persistence Owner, Trust Centre Owner, Desktop Owner, Performance Owner, Recovery Owner, Test Architecture Owner
**Supersedes:** None
**Superseded by:** None
**Related ADRs:** ADR-0001 through ADR-0021
**Related specifications:** CHARTER-001, SPEC-001 through SPEC-012
**Target milestone:** Context and Project Memory through Version 1.0

---

## 1. Decision Summary

Opure should implement project knowledge as a **derived, project-scoped, rebuildable local index** owned by a trusted Project Knowledge Service.

The index should accelerate:

* exact path lookup;
* exact symbol lookup;
* lexical code and documentation search;
* structural and semantic source discovery;
* dependency and reference traversal;
* retrieval for Context Assembly;
* project-memory grounding;
* and developer-facing repository exploration.

The project knowledge index should never become:

* the source of truth for project files;
* a replacement for the Workspace Service;
* a hidden memory system;
* an authority for code changes;
* a cross-project data pool;
* a secrets store;
* an autonomous prompt generator;
* or a reason to send project data to a cloud provider.

The Workspace Service remains authoritative for file content and current workspace state.

The Repository Service remains authoritative for repository state.

The Memory Service remains authoritative for explicit project-memory records.

The Context Assembly Service remains authoritative for final model context.

The Project Knowledge Service should return **source-bound candidates and evidence**, not prompts.

The initial storage architecture should use:

* one service-owned index directory per project and channel;
* one service-owned SQLite database for:

  * index metadata;
  * document metadata;
  * chunk metadata;
  * lexical indexes;
  * symbols;
  * references;
  * graph edges;
  * embedding metadata;
  * indexing operations;
  * and query evidence;
* SQLite FTS5 contentless-delete virtual tables for lexical indexes;
* normal SQLite STRICT tables for authoritative index metadata;
* immutable vector segment files for embedding values;
* immutable generation manifests;
* and a small atomic active-generation pointer.

The index should live under Opure application data.

It should not live:

* inside the project repository;
* in `.git`;
* in `.opure`;
* on a network share;
* in a cloud-synchronised folder;
* or in a plugin-controlled directory.

The initial lexical search decision should be:

> **Use SQLite FTS5 with contentless-delete indexes, built-in tokenizers and deterministic code-aware search-term expansion, while retaining source text only through Workspace-owned immutable snapshots.**

The lexical index should:

* use FTS5 compiled into the approved SQLite package;
* prohibit arbitrary runtime extension loading;
* use contentless-delete tables so the FTS index does not keep a second ordinary copy of source content;
* map FTS row IDs to trusted chunk records;
* enable FTS5 secure-delete where supported by the pinned SQLite build;
* run FTS5 integrity checks;
* use bounded idle-time merge or optimisation;
* rebuild from chunk metadata and Workspace snapshots when inconsistent;
* use `bm25()` as one inspectable lexical feature;
* use separate weighted columns for:

  * relative-path terms;
  * symbol terms;
  * identifier-expanded terms;
  * source content;
  * documentation content;
  * and optional project metadata;
* use exact B-tree tables for path and symbol equality;
* avoid relying on one tokenizer to understand all programming-language identifier conventions;
* generate deterministic identifier expansions such as:

  * raw identifier;
  * snake-case components;
  * camel-case components;
  * acronym boundaries;
  * digit boundaries;
  * and normalised Unicode form;
* preserve the original identifier;
* prohibit user-controlled raw FTS `MATCH` syntax;
* construct FTS expressions through trusted query code;
* escape phrases, prefixes, NEAR and column filters safely;
* bound query length, term count, prefix expansion and result count;
* and record every lexical score and match reason.

The initial vector-search decision should be:

> **Store normalised float32 embeddings in immutable checksummed segment files and perform exact cosine or dot-product search in a dedicated trusted Search Worker using hardware-accelerated managed SIMD, rather than adopting an approximate nearest-neighbour index in Version 1.**

The vector architecture should:

* keep embedding metadata in SQLite;
* keep embedding values outside SQLite in immutable segment files;
* bind every vector segment to:

  * project;
  * source generation;
  * embedding model;
  * embedding provider or local runtime;
  * dimensions;
  * normalisation;
  * chunker;
  * embedding-input template;
  * data classification;
  * and segment checksum;
* use normalised vectors so cosine ranking can use a dot product where the model profile permits;
* use float32 initially;
* defer float16 and int8 vector storage until retrieval-quality evidence exists;
* reject NaN and infinity;
* validate dimensions and norm;
* memory-map verified local segments read-only where safe;
* scan vectors through bounded, cancellable exact search;
* use hardware acceleration through stable .NET vector primitives where available;
* produce deterministic top-k ordering with stable tie-breaking;
* keep semantic retrieval optional when the project has no approved embedding model;
* keep lexical, path, symbol and graph retrieval fully useful without embeddings;
* use local embeddings by default;
* treat remote embedding as a separate ADR-0019 Data Sharing Plan;
* never embed detected secrets or protected files;
* never send an entire project for remote embedding automatically;
* and never hide embedding work behind ordinary project opening.

Version 1 should not use an approximate nearest-neighbour index.

This deferral is deliberate because:

* exact search is easier to verify;
* exact search has no recall loss from ANN approximation;
* exact search has a simpler corruption and deletion model;
* project retrieval can combine lexical and graph candidate generation;
* project-scale performance can be measured before introducing another native index;
* `sqlite-vec` remains pre-v1;
* and its newer DiskANN and IVF work remains alpha or experimental.

`sqlite-vec` may be evaluated later as:

* a pinned trusted extension;
* statically linked into a dedicated Search Worker;
* with runtime extension loading still disabled;
* after API stability, deletion, corruption, fuzzing, Windows packaging and retrieval-quality evidence.

No arbitrary SQLite extension should be loadable from:

* project;
* plugin;
* MCP server;
* user configuration;
* or model repository.

The initial source-understanding decision should be:

> **Use Roslyn compiler APIs for C# semantic indexing from a Project Service-provided safe compilation snapshot, use deterministic parse-only adapters for other formats, and introduce pinned Tree-sitter grammars incrementally for languages that need robust syntax structure without executing project code.**

C# indexing should:

* use source text, syntax trees, semantic models and compilations;
* operate in a dedicated Analysis Worker;
* receive project references, parse options, compilation options and metadata references from the trusted Project Service;
* not open an untrusted solution directly through an unrestricted design-time build;
* not restore packages;
* not run custom MSBuild targets;
* not run source generators;
* not run analyzers;
* not execute compiler plugins;
* not load arbitrary project assemblies;
* bound semantic-model lifetime and memory;
* and fall back to syntax-only indexing when safe semantic construction is unavailable.

Tree-sitter support should:

* use pinned source revisions;
* package generated parser code as first-party reviewed runtime artefacts;
* never run grammar generation on the user's project during indexing;
* never download grammars dynamically;
* run parsing under CPU, memory and time limits;
* exploit incremental parsing only for in-memory or staged updates where safe;
* and fall back to line-stable chunks after parse failure.

The durable index should cover **saved Workspace state**.

Unsaved editor buffers should use a separate ephemeral overlay:

* in memory;
* session bound;
* project bound;
* not embedded by default;
* not persisted;
* not included in background indexing;
* and visible as Unsaved Overlay results.

The initial Index Inclusion Policy should be explicit and ordered.

Suggested precedence:

1. Product hard deny
2. Security and protected-root deny
3. Project explicit deny
4. Enterprise deny
5. Explicit user include
6. Tracked repository source
7. Project explicit include patterns
8. Git ignore semantics for untracked files
9. Opure generated, dependency and large-file heuristics
10. Default text-file policy
11. Default deny for unknown or binary content

Git ignore files should be treated as repository intent for intentionally untracked files, not as a security boundary and not as a universal source-indexing policy.

The policy should recognise that tracked files are not ignored by `.gitignore`.

The initial hard-denied or default-excluded locations should include:

* `.git`;
* Opure application state;
* Opure Vault;
* credential stores;
* private-key locations;
* package caches;
* build artefacts;
* test artefacts;
* dependency trees;
* virtual environments;
* generated code;
* minified bundles;
* binary outputs;
* database files;
* archives;
* media without a modality-specific indexer;
* model files;
* and other protected or high-volume content.

Typical directory names such as:

* `bin`;
* `obj`;
* `node_modules`;
* `.venv`;
* `packages`;
* `artifacts`;
* `dist`;
* `coverage`;
* and generated output directories

should be defaults, not irreversible name-based security rules.

Tracked source inside a conventionally excluded directory may be indexed after policy evaluation.

Project explicit inclusion should never override:

* protected roots;
* confirmed secret files;
* prohibited file classes;
* workspace containment;
* or enterprise denial.

The initial file policy should:

* classify by path, file identity, content type, encoding, size and project role;
* not trust extension alone;
* detect binary content;
* detect generated content where evidence exists;
* detect minified or pathological text;
* cap individual files and total indexed bytes;
* expose every exclusion reason;
* allow explicit review for large non-secret text files;
* and preserve project-relative paths only.

Suggested Version 1 default limits:

* maximum candidate files during inventory: 500,000;
* maximum indexed text files per project: 200,000;
* maximum ordinary text file: 2 MiB;
* maximum explicitly approved large text file: 20 MiB;
* maximum individual line: 256 KiB before pathological-text handling;
* maximum durable source bytes: 5 GiB per project;
* maximum chunks: 1,000,000;
* maximum symbols: 5,000,000;
* maximum graph edges: 20,000,000;
* maximum embedding chunks by default: 250,000;
* maximum exact-vector scan candidates: policy and benchmark dependent;
* and maximum query results before fusion: 1,000 per retrieval channel.

These limits are provisional and must be tested.

The initial indexing pipeline should be:

```text
Project registration
    ↓
Workspace and repository inventory
    ↓
Inclusion and exclusion policy
    ↓
File identity and content-hash verification
    ↓
Data classification and secret scanning
    ↓
Content-kind detection
    ↓
Parsing and chunking
    ↓
Symbol and reference extraction
    ↓
Lexical term generation
    ↓
FTS5 indexing
    ↓
Optional local or approved remote embedding
    ↓
Immutable vector segment commit
    ↓
Integrity checks
    ↓
Generation commit
    ↓
Active generation publication
```

The index should be incrementally updated from trusted Workspace events.

File watchers should remain an optimisation and change hint, not the sole authority.

The incremental pipeline should:

* debounce related file activity;
* coalesce save, rename and delete operations;
* read immutable Workspace snapshots;
* calculate content hashes;
* skip unchanged content;
* invalidate old chunks, symbols, edges and vectors;
* update SQLite and generation metadata in one authoritative transaction;
* append new vector values to a staging segment;
* publish a new active generation only after all mandatory indexes are consistent;
* allow semantic embeddings to lag while clearly reporting Partial Semantic Coverage;
* and periodically reconcile the full Workspace inventory.

Lexical and structural indexing should be considered mandatory for an Index Ready state.

Embeddings should be optional and separately ready.

The initial states should distinguish:

* No Index;
* Inventory;
* Lexical Building;
* Structural Building;
* Lexical Ready;
* Structural Ready;
* Semantic Queued;
* Semantic Building;
* Ready;
* Ready with Partial Semantic Coverage;
* Stale;
* Degraded;
* Rebuilding;
* Faulted;
* Corrupted;
* Quarantined;
* and Disabled.

Search results should carry:

* project;
* active index generation;
* workspace generation;
* source snapshot hash;
* chunk ID;
* path;
* range;
* symbol;
* source class;
* retrieval channel;
* raw channel score;
* normalised feature score;
* fusion score;
* staleness;
* data classification;
* and an inspectable reason.

The Context Assembly Service must revalidate every selected result against current Workspace state before using it.

Search result presence should never authorise reading a changed file.

The initial retrieval channels should be:

1. exact path;
2. path prefix and filename;
3. exact symbol;
4. symbol prefix;
5. lexical FTS5;
6. structural relation;
7. dependency and reference graph;
8. semantic vector;
9. project documentation;
10. build and test evidence;
11. repository metadata;
12. and project memory through the Memory Service.

Project Memory should not be copied silently into the knowledge index as authoritative source.

The retrieval service may query Memory and fuse results while retaining separate provenance.

The initial hybrid fusion should use a deterministic, versioned method such as reciprocal rank fusion, followed by inspectable task-specific feature adjustments.

The final result score should not pretend that:

* BM25;
* cosine similarity;
* graph distance;
* exact match;
* and memory confidence

share one natural scale.

Each channel's raw score should remain available.

Exact path, explicit symbol and direct diagnostic evidence should receive deterministic priority before generic semantic similarity.

No opaque model reranker should be authoritative in Version 1.

A future local reranker may propose an ordering after:

* source eligibility;
* project isolation;
* secret policy;
* exact matches;
* and hard caps

have already been applied.

The index should be disposable and rebuildable.

Corruption recovery should:

* stop serving affected generations;
* preserve safe diagnostics;
* run SQLite and FTS5 integrity checks;
* verify vector-segment checksums;
* verify manifest consistency;
* rebuild into a staging directory;
* atomically publish the new generation;
* and delete the old generation only after safe recovery.

The product should never silently repair an index while continuing to present stale results as current.

The selected architecture is:

```text
Authoritative Workspace
    ↓
Project-scoped Index Policy
    ↓
Immutable file snapshots
    ↓
Trusted parsers and chunkers
    ↓
SQLite metadata, symbols, graph and FTS5
    +
Immutable exact-vector segments
    ↓
Versioned retrieval channels
    ↓
Deterministic hybrid fusion
    ↓
Source-bound search candidates
    ↓
Context Assembly revalidation
    ↓
Inspectable model context
```

---

## 2. Status

Current status:

> **Proposed**

This ADR becomes **Accepted** only after prototypes demonstrate:

* project-scoped index directories;
* one service-owned SQLite index database;
* FTS5 compiled into the approved SQLite package;
* runtime extension loading disabled;
* contentless-delete lexical indexes;
* FTS5 secure-delete behaviour;
* FTS5 integrity checks;
* exact path and symbol tables;
* deterministic code-identifier expansion;
* safe FTS query construction;
* C# syntax and semantic indexing without project-code execution;
* parse-only fallback;
* one pinned Tree-sitter grammar;
* file inventory policy;
* `.gitignore` handling;
* tracked-file handling;
* protected-root denial;
* binary, large, generated and minified exclusions;
* secret scanning;
* stable chunk IDs;
* symbols and graph edges;
* incremental save, rename and delete updates;
* full reconciliation;
* crash-safe generation publication;
* immutable float32 vector segments;
* exact SIMD vector scanning;
* local embedding generation;
* remote embedding approval;
* vector deletion through generation replacement;
* lexical-only operation;
* partial semantic coverage;
* hybrid fusion;
* inspectable result reasons;
* Context Assembly revalidation;
* index corruption recovery;
* index deletion;
* offline operation;
* and adversarial query, source-poisoning, malformed-parser, wrong-project, secret-leakage and vector-corruption tests.

---

## 3. Context

ADR-0021 establishes that Context Assembly needs source-bound retrieval candidates.

A repository may contain:

* source files;
* configuration;
* project files;
* documentation;
* generated code;
* dependencies;
* build output;
* test output;
* vendored code;
* archives;
* binaries;
* secrets;
* model artefacts;
* and ignored local files.

Not all of it should be indexed.

A repository can contain hundreds of thousands of files.

A monorepo can contain millions of chunks and symbols.

A simple text grep can find exact strings but cannot reliably answer:

* where a symbol is defined;
* which implementation satisfies an interface;
* which tests reference a component;
* which files call a method;
* which configuration controls a feature;
* or which semantically related source does not share query words.

A semantic vector search can find related text but can also:

* return stale chunks;
* return generated copies;
* return another project's source;
* prefer a repeated keyword attack;
* obscure why a result ranked;
* miss exact identifiers;
* and expose source to a remote embedding provider.

An approximate vector index can improve scale but introduces:

* native code;
* format stability;
* tuning;
* approximate recall;
* deletion behaviour;
* rebuild complexity;
* and another corruption domain.

An indexing service can accidentally:

* read outside the project;
* follow a junction;
* index `.env`;
* embed secrets;
* execute project build logic;
* load source generators;
* load parser plugins;
* download language grammars;
* index a cloud placeholder;
* or write data inside the repository.

An index can become stale after:

* a save;
* rename;
* delete;
* branch checkout;
* merge;
* rebase;
* generator run;
* package restore;
* or external editor change.

File-system notifications can overflow or be missed.

The index must therefore remain derived evidence that is always revalidated against authoritative Workspace state.

---

## 4. Problem Statement

Opure requires a project knowledge indexing and retrieval architecture that supports fast exact, lexical, structural and semantic discovery while preserving project isolation, source provenance, secret safety, deterministic ranking, incremental correctness, offline operation, bounded resource use and complete rebuildability.

---

## 5. Decision Drivers

The decision is evaluated against:

* Charter alignment;
* local-first operation;
* project isolation;
* source authority;
* retrieval relevance;
* exact identifier search;
* code structure;
* semantic discovery;
* explainability;
* secret protection;
* no project-code execution;
* incremental updates;
* crash safety;
* corruption recovery;
* large repositories;
* local resource use;
* embedding model changes;
* remote embedding consent;
* persistence simplicity;
* Windows 11 support;
* small-team implementation;
* and future cross-platform support.

---

## 6. Governing Principles

This decision must preserve:

* Developer Respect;
* Local by Design;
* Human in Control;
* Visible by Design;
* Inspectable Decisions;
* Least Privilege;
* Workspace Is Authoritative;
* Index Is Derived;
* One Project, One Authority Boundary;
* No Cross-Project Search;
* No Protected-File Indexing;
* No Secret Embeddings;
* No Project-Code Execution;
* No Dynamic Grammar Download;
* No Arbitrary SQLite Extension;
* No Trust by Filename;
* No Trust by Similarity;
* No Hidden Cloud Embedding;
* No Hidden Whole-Repository Index;
* No Opaque Reranker Authority;
* No Stale Result as Current;
* Rebuildable by Design;
* Reversible Index Policy;
* and Evidence-Based Confidence.

---

## 7. Scope

This ADR decides:

* Project Knowledge Service ownership;
* project index location;
* data model;
* SQLite and FTS5 use;
* lexical tokenisation;
* exact path and symbol indexes;
* source inventory;
* inclusion and exclusion;
* Git ignore handling;
* content-kind detection;
* generated and vendor handling;
* secret scanning;
* parsing;
* C# semantic indexing;
* Tree-sitter policy;
* chunks;
* symbols;
* references;
* graph edges;
* embeddings;
* vector storage;
* exact vector search;
* incremental updates;
* index generations;
* hybrid retrieval;
* query syntax;
* result provenance;
* index health;
* recovery;
* deletion;
* Trust Centre;
* and acceptance tests.

This ADR does not decide:

* final local embedding model;
* final remote embedding provider;
* final parser list;
* final language support list;
* final relevance weights;
* approximate nearest-neighbour indexing;
* learned reranking;
* cross-project indexing;
* global personal search;
* public code search;
* cloud-hosted project indexes;
* code intelligence server protocol;
* autonomous repository understanding;
* or whole-repository provider upload.

---

## 8. Constraints

Known constraints include:

* ADR-0005 selected SQLite through `Microsoft.Data.Sqlite`.
* ADR-0005 permits FTS5 and defers vector-engine selection.
* ADR-0005 disables arbitrary SQLite extensions.
* ADR-0009 governs paths, reparse points, file identity and cloud placeholders.
* ADR-0021 requires immutable source-bound candidates.
* FTS5 provides built-in full-text indexing, tokenizers, BM25, contentless-delete tables, integrity checks, rebuild and optimisation commands.
* SQLite runtime extension loading is disabled by default for security reasons but can be enabled explicitly; Opure should keep it disabled.
* `sqlite-vec` is pre-v1.
* `sqlite-vec` exact vector support is more mature than its new ANN work.
* `sqlite-vec` DiskANN and IVF features are alpha or experimental in current releases.
* Roslyn Workspaces, syntax trees, semantic models and compilations can support C# code analysis.
* Semantic models retain caches and can consume significant memory.
* Tree-sitter provides incremental parsing and useful syntax trees even with syntax errors.
* Git ignore patterns have layered precedence and do not affect already tracked files.
* Windows file notifications can miss changes.
* indexes can contain content-derived sensitive information even without full source text;
* and embeddings are sensitive project derivatives.

---

## 9. Assumptions

This decision assumes:

* FTS5 is available in the approved SQLite package;
* a contentless-delete FTS5 table can be maintained transactionally;
* source chunks can be retrieved from Workspace snapshots by trusted references;
* C# source can be indexed with Roslyn in an isolated worker;
* project configuration can be represented safely without executing custom build logic;
* pinned Tree-sitter grammars can be packaged for selected languages;
* exact vector scanning is adequate for the first supported repository sizes;
* local embedding generation can run under ADR-0020;
* remote embeddings can remain optional;
* generation manifests can make updates crash safe;
* query results can be revalidated before Context Assembly;
* and a complete index can always be rebuilt from authoritative project state.

---

## 10. Current Technical Evidence

Official and primary documentation available on 18 July 2026 establishes that:

* SQLite FTS5 supports full-text query syntax, built-in tokenizers, weighted `bm25()` ranking, contentless-delete tables, integrity checks, merge or optimise operations and rebuild operations;
* FTS5 external-content tables require applications to keep the content table and index consistent, with documented pitfalls when they diverge;
* FTS5 contentless-delete tables support ordinary deletion and full-column updates without retaining ordinary source columns;
* FTS5 secure-delete can remove old full-text entries instead of leaving delete keys until later merges;
* SQLite runtime extension loading is disabled by default for security reasons;
* Git ignore rules have multiple precedence levels and do not apply to files already tracked by Git;
* Roslyn Workspaces expose projects, documents, syntax trees, semantic models and compilations;
* a Roslyn SemanticModel caches semantic information and should not be retained indefinitely;
* Tree-sitter is an incremental parser library designed to remain useful in the presence of syntax errors;
* .NET `Vector<T>` provides hardware-accelerated SIMD operations on supported systems;
* `sqlite-vec` describes itself as pre-v1 and therefore subject to breaking changes;
* and `sqlite-vec` introduced new DiskANN and IVF work through alpha or experimental releases in 2026.

These facts support:

* FTS5 for lexical search;
* Roslyn for the first semantic language;
* Tree-sitter as a future parse-only framework;
* managed exact vector search for Version 1;
* and continued deferral of production ANN.

Every SQLite build, FTS5 option, Roslyn package, grammar package and embedding implementation must be revalidated before acceptance.

---

## 11. Terminology

### 11.1 Project Index

The complete derived knowledge state for one project.

---

### 11.2 Index Generation

A consistent version of index metadata and derived artefacts.

---

### 11.3 Active Generation

The generation currently served for queries.

---

### 11.4 Staging Generation

A generation under construction or rebuild.

---

### 11.5 Inventory

The policy-evaluated list of project files and their identities.

---

### 11.6 Document

One indexed project file or other approved project-owned source.

---

### 11.7 Chunk

A stable source-derived range defined by ADR-0021 chunking policy.

---

### 11.8 Symbol

A language-aware declaration or named structural element.

---

### 11.9 Reference

A source occurrence linked to a symbol or unresolved identity.

---

### 11.10 Graph Edge

A typed relation between documents, symbols, projects, dependencies or tests.

---

### 11.11 Lexical Index

The FTS5-derived term index.

---

### 11.12 Embedding

A model-generated numeric representation of one approved chunk.

---

### 11.13 Vector Segment

An immutable file containing vectors and their chunk mapping.

---

### 11.14 Semantic Coverage

The proportion of eligible chunks with valid embeddings for the active embedding profile.

---

### 11.15 Query Plan

The deterministic set of retrieval channels and limits used for one query.

---

### 11.16 Retrieval Candidate

A source-bound result from one channel before fusion.

---

### 11.17 Hybrid Result

A fused result retaining every channel's provenance.

---

### 11.18 Overlay Index

A session-only index of unsaved editor buffers.

---

### 11.19 Index Policy

The versioned inclusion, exclusion, parser, embedding and resource policy.

---

### 11.20 Reconciliation

A complete comparison between authoritative Workspace inventory and index state.

---

## 12. Options Considered

The principal architecture options are:

1. SQLite FTS5 plus graph tables and exact vector segments.
2. SQLite FTS5 plus `sqlite-vec`.
3. Lucene.NET plus a vector engine.
4. Embedded external vector database.
5. Local Qdrant process.
6. Cloud search service.
7. Pure grep and symbol lookup.
8. Whole-repository embeddings only.
9. Language-server-owned index.
10. One global index across projects.

---

## 13. Option A — FTS5, Graph Tables and Exact Vector Segments

### 13.1 Advantages

* aligns with existing SQLite decision;
* project-local;
* no new daemon;
* no arbitrary extension;
* exact semantic results;
* deterministic corruption model;
* simple deletion through generation replacement;
* inspectable lexical ranking;
* easy backup and rebuild;
* provider neutral;
* and a manageable Version 1 surface.

### 13.2 Disadvantages

* exact vector scan is linear;
* custom segment format;
* custom SIMD search code;
* semantic scale is bounded;
* and ANN may be required later.

### 13.3 Decision

Selected.

---

## 14. FTS5 plus `sqlite-vec`

Attractive because it keeps lexical and vector data near SQLite.

Deferred for production because:

* the project remains pre-v1;
* breaking changes are expected;
* ANN features are currently alpha or experimental;
* native extension packaging adds supply-chain and crash risk;
* arbitrary extension loading conflicts with Opure policy;
* and deletion, corruption and Windows packaging require more evidence.

A pinned static integration may be reconsidered.

---

## 15. Lucene.NET

Deferred because it introduces:

* another persistence engine;
* another query language;
* another corruption and upgrade path;
* and no immediate need beyond FTS5 for Version 1.

---

## 16. External Vector Database

Rejected for Version 1 because it adds:

* a daemon or embedded native engine;
* service lifecycle;
* network or IPC;
* additional persistence;
* and more complex repair.

---

## 17. Local Qdrant Process

Deferred for similar reasons.

It may be appropriate for very large future workspaces but is disproportionate initially.

---

## 18. Cloud Search Service

Rejected because project knowledge should remain local by default.

---

## 19. Pure Grep

Insufficient because it lacks:

* ranking;
* semantic retrieval;
* stable chunks;
* symbol graph;
* and incremental query structures.

Exact grep remains a useful retrieval channel or fallback.

---

## 20. Embeddings Only

Rejected because semantic similarity cannot replace exact identifiers, lexical evidence or structure.

---

## 21. Language Server Ownership

Rejected because language servers vary, may execute project logic, may be absent and should not own Opure project data policy.

Language-server evidence may become an optional source later.

---

## 22. Global Cross-Project Index

Rejected because it increases privacy, authority and wrong-project risk.

---

## 23. Decision

Opure will provisionally adopt:

> **A derived project-scoped knowledge index using service-owned SQLite STRICT tables, FTS5 contentless-delete lexical indexes, Roslyn-first symbols and graph evidence, immutable exact-search float32 vector segments and deterministic hybrid retrieval, with no arbitrary extensions, no approximate nearest-neighbour engine, no cross-project index and mandatory Workspace revalidation before context use.**

This decision is:

* [ ] Permanent until superseded
* [x] Provisional pending indexing, retrieval and scale evidence
* [ ] Approval of `sqlite-vec`
* [ ] Approval of approximate nearest-neighbour search
* [ ] Approval of learned reranking
* [ ] Approval of cross-project knowledge

---

# 24. Project Knowledge Service Ownership

The Project Knowledge Service owns:

* index registration;
* index policy;
* inventory coordination;
* file eligibility;
* parser selection;
* chunk metadata;
* lexical indexing;
* symbol indexing;
* reference extraction;
* graph construction;
* embedding orchestration;
* vector-segment lifecycle;
* generation publication;
* query planning;
* retrieval-channel execution;
* score normalisation;
* hybrid fusion;
* result provenance;
* index health;
* reconciliation;
* rebuild;
* corruption recovery;
* and Trust Centre projections.

It does not own:

* source files;
* repository mutation;
* project build;
* project memory;
* provider credentials;
* model execution;
* final context admission;
* patch application;
* plugin authority;
* MCP authority;
* or user authentication.

---

# 25. Service Boundary

Conceptual architecture:

```text
Desktop and Context Assembly
    ↓
Runtime Gateway
    ↓
Project Knowledge Service
    ├── Index Registry
    ├── Inventory Coordinator
    ├── Inclusion Policy
    ├── Classification and Secret Filter
    ├── Parser Coordinator
    ├── Chunk Metadata
    ├── Symbol and Reference Index
    ├── Graph Index
    ├── FTS5 Lexical Index
    ├── Embedding Coordinator
    ├── Vector Segment Manager
    ├── Query Planner
    ├── Hybrid Fusion
    └── Health and Recovery
        ↓
Workspace, Repository, Build, Test, Memory,
Local Model Runtime, AI Router and Network Gateway
```

---

## 25.1 No Direct Project Access by Desktop

Desktop queries through trusted contracts.

---

## 25.2 No Direct Database Access by Context Assembly

Context Assembly receives typed retrieval results.

---

## 25.3 No Direct Index Access by Plugins

Plugins may request approved search operations through capabilities.

---

## 25.4 No Direct Index Access by MCP Servers

MCP servers do not receive database paths or raw index handles.

---

# 26. Index Registration

A project must be registered before indexing.

---

## 26.1 Registration Fields

Suggested fields:

```text
project_id
project_profile
workspace_root_ref
repository_ref
channel
index_policy
storage_profile
language_profiles
embedding_profile
enabled
created_at
last_reviewed_at
```

---

## 26.2 One Index per Project and Channel

Stable, Preview and Development maintain separate logical indexes.

---

## 26.3 Project ID

Use the Project Service opaque identity.

Do not derive authority from:

* directory name;
* repository remote;
* solution name;
* or display name.

---

## 26.4 Root Change

A workspace-root identity change invalidates the index.

---

# 27. Index Directory

Suggested root:

```text
%LOCALAPPDATA%\Opure\<Channel>\Projects\<project-id>\Knowledge\
```

Suggested shape:

```text
Knowledge\
├── active.json
├── registry.json
├── generations\
│   ├── <generation-id>\
│   │   ├── knowledge.db
│   │   ├── manifest.json
│   │   ├── vectors\
│   │   ├── reports\
│   │   └── recovery\
│   └── ...
├── staging\
├── overlay\
└── quarantine\
```

---

## 27.1 Opaque Paths

Use opaque project and generation IDs.

---

## 27.2 ACL

Only the trusted user and Opure services should access the index.

---

## 27.3 No Repository State

The index directory must not be under the repository root.

---

## 27.4 Local Fixed Disk

Use a local fixed disk by default.

---

## 27.5 Secondary Storage

A future approved secondary local disk may host indexes after filesystem capability review.

---

# 28. Index Generations

An Index Generation is a consistent published view.

---

## 28.1 Generation Identity

Suggested identity fields:

```text
generation_id
project_id
index_policy_revision
workspace_generation
repository_generation
chunker_profiles
parser_profiles
lexical_schema
embedding_profiles
created_at
published_at
state
manifest_sha256
```

---

## 28.2 Active Pointer

`active.json` or an equivalent authoritative record identifies the published generation.

---

## 28.3 Atomic Publication

Publish only after:

* SQLite transaction commit;
* SQLite integrity check;
* FTS5 integrity check;
* vector checksum verification;
* manifest generation;
* and directory durability.

---

## 28.4 Staging

Incomplete work remains outside the active path.

---

## 28.5 Historical Generations

Retain a bounded previous generation for rollback and incident analysis.

---

## 28.6 Generation Reuse

Do not reuse a generation ID for different bytes.

---

# 29. Incremental Generation Model

Version 1 may use one mutable active SQLite database inside a generation directory for ordinary incremental updates, provided:

* each update is transactional;
* the generation revision increases;
* query readers use SQLite snapshot isolation;
* vector changes publish immutable segment revisions;
* and the generation manifest records every committed revision.

A full rebuild creates a new generation directory.

---

## 29.1 Database Revision

Suggested field:

```text
index_revision
```

increments on every committed update batch.

---

## 29.2 Query Binding

Every query binds:

* generation ID;
* database revision;
* workspace generation;
* and semantic coverage revision.

---

## 29.3 Vector Manifest

A database revision references a complete immutable set of vector segments.

---

## 29.4 Old Vector Segments

Delete only after no active query or retained generation references them.

---

# 30. Index Manifest

Suggested schema:

```text
opure.project-knowledge-manifest/1
```

---

## 30.1 Fields

```text
project_id
generation_id
index_revision
workspace_generation
repository_generation
index_policy
database_sha256_or_integrity_record
lexical_schema
parsers
chunkers
symbols
graphs
embedding_profiles
vector_segments
coverage
counts
storage
created_at
published_at
```

---

## 30.2 Manifest Hash

Canonical SHA-256.

---

## 30.3 Authority

The manifest describes derived state.

It does not authorise project access by itself.

---

# 31. SQLite Database

Suggested filename:

```text
knowledge.db
```

---

## 31.1 Connection Policy

ADR-0005 applies:

* one service owner;
* WAL on local filesystem;
* foreign keys enabled;
* short write transactions;
* explicit migrations;
* no cross-service direct access;
* and bounded connections.

---

## 31.2 STRICT Tables

Use STRICT tables where practical.

---

## 31.3 Extension Loading

Keep runtime extension loading disabled.

---

## 31.4 Trusted Built-Ins

Use only features compiled into the approved SQLite build.

---

## 31.5 Query-Only Readers

Read-only query connections should use query-only mode where practical.

---

# 32. Suggested Database Tables

```text
index_metadata
index_revisions
documents
document_versions
document_classifications
document_exclusions
chunks
chunk_renderings
chunk_terms
languages
parser_profiles
parse_results
symbols
symbol_names
references
graph_nodes
graph_edges
dependency_records
lexical_query_history
embedding_profiles
embedding_jobs
embedding_records
vector_segments
vector_segment_entries
semantic_coverage
index_operations
index_failures
integrity_results
query_receipts
```

FTS5 virtual tables:

```text
chunks_fts
symbols_fts
documents_fts
```

Exact schemas remain implementation work.

---

# 33. Document Table

A document record should contain:

```text
document_id
project_id
relative_path
normalised_path_key
file_identity
workspace_generation
content_sha256
size_bytes
encoding
line_endings
content_kind
language
project_role
tracked_state
ignore_state
generated_state
classification
eligibility
exclusion_reason
created_at
indexed_at
```

---

## 33.1 Relative Path

Use ADR-0009 logical project-relative path.

---

## 33.2 Normalised Path Key

Normalisation supports exact lookup.

It must not erase case-sensitive distinctions.

---

## 33.3 File Identity

Retain volume and file identity where supported.

---

## 33.4 Content Hash

Content hash is the primary change detector.

---

# 34. Document Versions

A changed file creates a new indexed document version.

---

## 34.1 Stable Document ID

A rename may retain logical document continuity if Repository and Workspace evidence establish it.

---

## 34.2 Version ID

Every content version is immutable.

---

## 34.3 Historical Content

The knowledge index does not retain complete historical file content by default.

Repository history remains the authority.

---

# 35. Index Inclusion Policy

Suggested schema:

```text
opure.index-policy/1
```

---

## 35.1 Fields

```text
policy_id
revision
hard_denies
protected_roots
tracked_file_policy
untracked_file_policy
git_ignore_policy
explicit_includes
explicit_excludes
generated_policy
dependency_policy
vendor_policy
binary_policy
large_file_policy
minified_policy
language_profiles
secret_policy
embedding_policy
resource_limits
reconciliation_policy
```

---

## 35.2 Immutable Revision

A changed policy creates a new revision.

---

## 35.3 Reindex Scope

A policy change computes affected files and may trigger full rebuild.

---

# 36. Inclusion Precedence

Recommended order:

1. Workspace containment and verified root
2. Product protected-root deny
3. Product prohibited-file deny
4. Enterprise deny
5. Project explicit deny
6. Security classification deny
7. Explicit user include
8. Tracked source policy
9. Project include patterns
10. Git ignore handling for untracked files
11. Generated and dependency policy
12. Content-kind and size policy
13. Default text eligibility
14. Default deny

---

## 36.1 Deny Wins

Hard product, security and enterprise denies cannot be overridden.

---

## 36.2 User Include

A user include can override ordinary convenience exclusions, not security exclusions.

---

## 36.3 Explainability

Record the first decisive rule and relevant competing rules.

---

# 37. Git Integration

Repository Service should provide tracked and ignored state.

---

## 37.1 Tracked Files

Tracked files are considered repository source even when a matching ignore pattern exists.

---

## 37.2 Untracked Files

Use current Git ignore semantics to decide default eligibility.

---

## 37.3 Ignore Sources

Record which rule source applied:

* command or Opure policy;
* repository `.gitignore`;
* nested `.gitignore`;
* repository info exclude;
* or user global excludes.

---

## 37.4 Global Excludes

User global Git excludes should not automatically become shared project policy.

They may influence local default indexing and remain visible.

---

## 37.5 Repository Unavailable

For a non-Git project, use explicit Index Policy and default file rules.

---

## 37.6 No Shell Parsing

Use Repository Service, Git library or safe command invocation.

Do not parse arbitrary shell output.

---

# 38. Default Excluded Roots

Product hard or ordinary defaults may include:

```text
.git/
.opure/
bin/
obj/
node_modules/
packages/
artifacts/
dist/
coverage/
.venv/
venv/
__pycache__/
.gradle/
.idea/
.vs/
TestResults/
```

These names require context.

---

## 38.1 Tracked Exception

A tracked source file inside an ordinary excluded directory may be included after review or policy.

---

## 38.2 Protected Root

`.git` and Opure security state remain denied regardless of tracking.

---

## 38.3 Nested Repository

Nested repositories or submodules require explicit project or dependency policy.

---

# 39. Dependency and Vendor Policy

Dependencies and vendored source can dominate retrieval.

---

## 39.1 Default

Exclude:

* package cache;
* installed dependency tree;
* generated client;
* vendored third-party source;
* and SDK source

from ordinary semantic indexing.

---

## 39.2 Metadata

Index dependency name, version and project relation through Project Service.

---

## 39.3 Explicit Source Review

A developer may enable selected third-party source for one project.

---

## 39.4 Separate Source Class

Third-party content remains distinct and receives a retrieval penalty.

---

## 39.5 Licence

Do not copy or export dependency source beyond permitted use.

---

# 40. Generated Content

Generated content may be identified through:

* project metadata;
* generator manifest;
* file header;
* output directory;
* build artefact classification;
* repository attributes;
* or explicit policy.

---

## 40.1 Default

Exclude generated implementation bodies.

---

## 40.2 Signatures

A deterministic signature-only structural record may be retained when useful.

---

## 40.3 Source Generator Output

Do not run source generators to obtain output.

Already materialised generated files may be indexed only under policy.

---

## 40.4 Generated Marker Is Untrusted

A comment alone is evidence, not absolute authority.

---

# 41. Content-Kind Detection

Determine content kind using:

* path;
* extension;
* magic bytes;
* encoding;
* byte distribution;
* parser probe;
* project role;
* and user policy.

---

## 41.1 Binary Detection

Reject binary content from text indexes.

---

## 41.2 Text Encoding

Support a bounded approved set.

Prefer UTF-8.

---

## 41.3 Unknown Encoding

Exclude or require explicit review.

---

## 41.4 Invalid Text

Do not silently replace large numbers of invalid bytes.

---

# 42. Large Files

Ordinary text files above the default limit are excluded.

---

## 42.1 Metadata Only

Index safe metadata:

* path;
* size;
* type;
* tracking;
* and exclusion reason.

---

## 42.2 Explicit Approval

A large file may be indexed under:

* exact path;
* exact size;
* exact hash;
* special chunker;
* and resource budget.

---

## 42.3 Change

Any content change requires renewed eligibility evaluation.

---

# 43. Pathological and Minified Text

Indicators may include:

* extremely long line;
* very low whitespace;
* high entropy;
* repeated encoded data;
* dense minification;
* or generated bundle headers.

---

## 43.1 Default

Exclude bodies.

---

## 43.2 Exact Search

A bounded raw exact search may still be available outside the durable index when explicitly requested.

---

## 43.3 No Embedding

Do not embed minified or encoded blobs by default.

---

# 44. Archives and Containers

Do not recursively index:

* ZIP;
* TAR;
* NuGet packages;
* JAR;
* installers;
* Office archives;
* or model containers

as ordinary project source.

---

## 44.1 Explicit Import

A future archive-inspection operation is separate, bounded and non-persistent by default.

---

# 45. Databases

Do not index SQLite, SQL Server, database dumps or other databases as ordinary binary files.

---

## 45.1 SQL Scripts

Plain-text schema and migration scripts are eligible.

---

## 45.2 Database Content

Requires an explicit connector or MCP operation and separate data policy.

---

# 46. Media

Images, audio and video are excluded from text indexing initially.

---

## 46.1 Metadata

Safe filename and project-role metadata may be indexed.

---

## 46.2 Future Multimodal Index

Requires:

* modality model;
* privacy;
* storage;
* cost;
* and retrieval policy.

---

# 47. Secret Scanning

Every eligible text file is scanned before lexical or embedding indexing.

---

## 47.1 Lexical Secret Risk

An FTS index can preserve secret-derived terms.

Confirmed or likely secret content should not enter FTS5.

---

## 47.2 Embedding Secret Risk

Embeddings can encode semantic information.

Secret content must not be embedded.

---

## 47.3 Result

Possible states:

* Clear;
* Redacted Index Variant;
* Likely Secret;
* Confirmed Secret;
* Protected;
* and Scan Failed.

---

## 47.4 Default

Likely, confirmed, protected and scan-failed content is excluded from lexical and semantic indexes.

---

## 47.5 Metadata

Store only safe exclusion metadata.

---

## 47.6 Redacted Variant

A redacted index variant may be created only when:

* redaction is structurally safe;
* developer policy permits;
* new hash is recorded;
* and retrieval result identifies redaction.

---

# 48. Secret Rescan

Rescan when:

* file content changes;
* secret rules change;
* known Vault canary fingerprints change;
* or a security incident occurs.

---

## 48.1 Rule Change

A stricter scanner may invalidate existing indexed content and trigger purge or rebuild.

---

# 49. File Inventory

Inventory uses trusted Workspace and Repository services.

---

## 49.1 No Recursive Raw Walk as Authority

A raw filesystem walk may assist reconciliation but must use ADR-0009 containment and no-follow rules.

---

## 49.2 Cloud Placeholder

Do not hydrate a cloud placeholder silently.

---

## 49.3 Reparse Point

Do not follow an unapproved reparse target.

---

## 49.4 Mounted Folder

Treat a cross-volume mounted target as a separate root requiring policy.

---

## 49.5 File Change During Inventory

Use immutable snapshots or retry.

---

# 50. Initial Inventory

The initial index operation is explicit and cancellable.

---

## 50.1 Preview

Show:

* candidate files;
* excluded files;
* tracked and untracked;
* estimated bytes;
* languages;
* secret exclusions;
* embedding eligibility;
* estimated storage;
* and estimated local compute.

---

## 50.2 Background Work

After approval, indexing may continue in bounded background mode.

---

## 50.3 No Hidden Cloud Work

Remote embeddings require separate approval before any data transmission.

---

# 51. Index Operation

Suggested fields:

```text
operation_id
project_id
operation_kind
policy_revision
workspace_generation
started_at
state
files_seen
files_eligible
files_indexed
bytes_read
chunks
symbols
edges
embeddings
failures
cancelled
completed_at
```

---

# 52. Index Operation Types

* Initial Build;
* Incremental Update;
* Full Reconciliation;
* Lexical Rebuild;
* Structural Rebuild;
* Semantic Build;
* Semantic Rebuild;
* Integrity Check;
* Optimise;
* Recovery;
* Purge;
* and Delete.

---

# 53. File Processing Pipeline

```text
Workspace candidate
    ↓
Containment and identity
    ↓
Inclusion policy
    ↓
Content-kind probe
    ↓
Size and pathological-text policy
    ↓
Immutable snapshot
    ↓
Content hash
    ↓
Classification and secret scan
    ↓
Parser selection
    ↓
Chunking
    ↓
Lexical terms
    ↓
Symbols and references
    ↓
Optional embedding
    ↓
Transactional commit
```

---

# 54. Parser Coordinator

The Parser Coordinator selects a reviewed Parser Profile.

---

## 54.1 Parser Profile

Fields:

```text
parser_profile_id
revision
language
parser_kind
package
package_hash
grammar_or_compiler_version
supported_features
resource_limits
fallback
verified_at
```

---

## 54.2 Parser Types

* Roslyn Semantic;
* Roslyn Syntax;
* Tree-sitter;
* Structured Configuration;
* Markdown;
* Diff;
* Log;
* Plain Text;
* and Unsupported.

---

## 54.3 No Project Plugin Parser

Project files cannot register executable parsers.

---

## 54.4 Plugin Parser

A future plugin-contributed parser requires a narrow declarative or sandboxed capability and cannot become trusted automatically.

---

# 55. Analysis Worker

Parsing and semantic analysis run in a dedicated supervised worker.

---

## 55.1 Why

Parsers process untrusted and potentially pathological source.

---

## 55.2 Limits

Enforce:

* memory;
* CPU;
* file bytes;
* syntax-node count;
* recursion;
* duration;
* and output size.

---

## 55.3 No Network

Analysis workers need no network.

---

## 55.4 Minimal Environment

No project secrets or provider credentials.

---

## 55.5 Crash

A parser crash excludes the affected file or parser profile and preserves the last valid active index.

---

# 56. C# Indexing

C# is the first semantic language because Opure's trusted core is C#.

---

## 56.1 Inputs

Project Service supplies:

* source snapshots;
* project references;
* target framework identity;
* parse options;
* compilation options;
* preprocessor symbols;
* approved metadata references;
* and generated-file policy.

---

## 56.2 No Direct Solution Open

The Analysis Worker should not directly open an arbitrary solution and execute unrestricted design-time build logic.

---

## 56.3 Safe Compilation Snapshot

Create an AdhocWorkspace or equivalent compiler model from trusted inputs.

---

## 56.4 Metadata References

References must be:

* exact;
* verified;
* local;
* and supplied by Project Service.

---

## 56.5 No Restore

Indexing does not restore packages.

---

## 56.6 No Source Generators

Do not run source generators.

---

## 56.7 No Analyzers

Do not load project analyzers.

---

## 56.8 No Compiler Plugins

Do not load arbitrary compiler server extensions.

---

# 57. Roslyn Syntax Index

Extract:

* namespaces;
* types;
* members;
* parameters;
* local functions;
* attributes;
* using directives;
* declarations;
* documentation comments;
* and syntax ranges.

---

## 57.1 Partial Syntax

Roslyn may still produce syntax trees for incomplete source.

---

## 57.2 Diagnostics

Parser diagnostics are index evidence, not build diagnostics.

---

# 58. Roslyn Semantic Index

When safe compilation is available, extract:

* fully qualified symbol identity;
* symbol kind;
* containing symbol;
* declaration;
* implementations;
* inheritance;
* interface relations;
* type references;
* method calls;
* property access;
* field access;
* object creation;
* extension-method relation;
* and project-reference relation.

---

## 58.1 Semantic Model Lifetime

Use and release semantic models promptly.

---

## 58.2 Memory Bound

Process large solutions in bounded batches.

---

## 58.3 Compilation Failure

Partial semantic evidence may be retained with explicit confidence.

---

## 58.4 Unresolved References

Keep unresolved textual identity separately.

---

# 59. Symbol Identity

A symbol record includes:

```text
symbol_id
language
symbol_key
display_name
qualified_name
kind
accessibility
project_id
document_id
declaration_range
body_range
signature_hash
semantic_confidence
parser_profile
workspace_generation
```

---

## 59.1 Roslyn SymbolKey

A Roslyn-specific key may assist identity.

Opure still records an implementation-neutral symbol identity.

---

## 59.2 Local Symbols

Local variables and parameters may be indexed only for file-local search, not global graph expansion.

---

## 59.3 Generated Symbol

Mark source and generated status.

---

# 60. References

A reference includes:

```text
reference_id
source_symbol
target_symbol
unresolved_target
reference_kind
document_id
range
confidence
parser_profile
workspace_generation
```

---

## 60.1 Reference Kinds

* Declaration;
* Call;
* Read;
* Write;
* Type Use;
* Inheritance;
* Implementation;
* Import;
* Construction;
* Attribute;
* Test Relation;
* Configuration Relation;
* and Documentation Link.

---

## 60.2 Confidence

Semantic references outrank lexical inference.

---

# 61. Graph Nodes and Edges

Graph nodes may represent:

* project;
* document;
* symbol;
* package;
* target framework;
* test;
* build target;
* configuration key;
* and repository commit reference.

---

## 61.1 Edge Types

* Contains;
* Declares;
* References;
* Calls;
* Called By;
* Implements;
* Inherits;
* Imports;
* Depends On;
* Tests;
* Configures;
* Generates;
* Changes;
* and Documents.

---

## 61.2 Directed

Record direction explicitly.

---

## 61.3 Weight

Graph weights are evidence-specific and versioned.

---

## 61.4 No Model-Generated Edge Authority

A model suggestion may create a candidate, not an authoritative edge.

---

# 62. C# Project Graph

Project Service supplies:

* project references;
* package references;
* target frameworks;
* compile item membership;
* and configuration identity.

---

## 62.1 Package Source

Do not index package binaries.

---

## 62.2 Package Metadata

Safe name and version may be searchable.

---

## 62.3 Framework References

Represent as metadata, not source chunks.

---

# 63. Tree-sitter Policy

Tree-sitter is a parse-only future and incremental-language framework.

---

## 63.1 Pinned Grammar

Every grammar has:

* repository;
* full commit;
* generated parser hash;
* licence;
* supported language version;
* and test corpus.

---

## 63.2 Build Time

Generate parser code in Opure's controlled build pipeline.

---

## 63.3 Runtime

Load only first-party packaged grammar code.

---

## 63.4 No Dynamic Grammar

No project download, package-manager install or runtime generation.

---

## 63.5 Query Files

Tree-sitter queries for symbols are source controlled and hashed.

---

## 63.6 Incremental Parsing

An old tree may accelerate a changed buffer.

The authoritative indexed output still binds the new file hash.

---

## 63.7 Error Trees

A parse with errors can still yield bounded structural evidence.

Mark confidence.

---

# 64. Structured Configuration Parsers

Use safe dedicated parsers for:

* JSON;
* YAML;
* XML;
* TOML;
* INI;
* project files;
* and package manifests.

---

## 64.1 YAML

Disable unsafe tags and object creation.

---

## 64.2 XML

Disable external entities and DTD retrieval.

---

## 64.3 JSON

Reject duplicate critical properties where interpretation matters.

---

## 64.4 Project Files

Project Service remains authoritative for evaluated meaning.

The knowledge index may index text and safe declared items.

---

# 65. Plain-Text Parser

Plain text produces:

* document metadata;
* ADR-0021 chunks;
* lexical terms;
* headings where detected;
* and no semantic symbols beyond explicit safe heuristics.

---

# 66. Chunk Records

The index uses ADR-0021 chunk identities and policies.

---

## 66.1 Fields

```text
chunk_id
document_version_id
relative_path
start_byte
end_byte
start_line
end_line
content_sha256
rendering_sha256
chunker_profile
parser_profile
symbol_id
content_kind
data_class
generated_state
token_estimates
eligible_for_embedding
created_at
```

---

## 66.2 Raw Content

Do not store a second ordinary full chunk copy in FTS5.

A bounded rendering cache may be owned by Workspace or Context services.

---

## 66.3 Retrieval

Search result reads current immutable snapshot content through Workspace.

---

## 66.4 Index Deletion

Deleting a chunk removes:

* FTS row;
* symbols and references;
* graph edges;
* embedding record;
* and vector manifest reference.

---

# 67. Stable Chunk Identity

Chunk identity includes:

* project;
* document content hash;
* range;
* chunker;
* parser;
* rendering;
* and normalisation.

---

## 67.1 Same Content, New Path

A rename can produce a new path-bound chunk identity while retaining a continuity link.

---

## 67.2 Same Range, Changed Content

New ID.

---

# 68. Lexical Term Generation

Generate several term streams.

---

## 68.1 Path Terms

From:

* relative path;
* filename;
* extension;
* directory components;
* and path-component expansions.

---

## 68.2 Symbol Terms

From:

* raw name;
* qualified name;
* signature;
* type name;
* member name;
* and namespace.

---

## 68.3 Identifier Terms

For each identifier:

* raw;
* Unicode normalised;
* snake segments;
* kebab segments;
* camel segments;
* acronym segments;
* numeric segments;
* and joined variants.

---

## 68.4 Documentation Terms

From:

* comments;
* headings;
* XML docs;
* Markdown;
* and safe configuration descriptions.

---

## 68.5 Content Terms

Original source text or a deterministic searchable representation.

---

## 68.6 Keyword Injection

Do not add model-generated keywords.

---

# 69. Identifier Expansion Example

Input:

```text
HTTPRequest2XXHandler
```

Possible deterministic terms:

```text
HTTPRequest2XXHandler
HTTP
Request
2
XX
Handler
http request 2 xx handler
```

The original remains searchable.

---

## 69.1 Language Rules

Language profiles may refine:

* valid identifier characters;
* namespace separators;
* operator names;
* and case sensitivity.

---

# 70. FTS5 Schema

Conceptual chunk FTS table:

```sql
CREATE VIRTUAL TABLE chunks_fts USING fts5(
    path_terms,
    symbol_terms,
    identifier_terms,
    documentation_terms,
    content_terms,
    content='',
    contentless_delete=1,
    tokenize='unicode61 remove_diacritics 2'
);
```

Exact tokenizer and options require tests.

---

## 70.1 Row ID

Map FTS row ID to `chunks.chunk_rowid`.

---

## 70.2 Unindexed Metadata

Keep authority metadata in normal tables.

---

## 70.3 Column Weights

Example BM25 weighting concept:

```text
path_terms:          8
symbol_terms:       12
identifier_terms:    6
documentation_terms: 2
content_terms:       1
```

Exact values require evaluation.

---

## 70.4 Lower BM25 Is Better

The adapter converts raw BM25 to a normalised feature without discarding the raw score.

---

# 71. Symbol FTS

A dedicated symbol FTS table may index:

* display name;
* qualified name;
* signature;
* container;
* and documentation.

Exact symbol equality remains in normal tables.

---

# 72. Document FTS

A document-level FTS table may index:

* path;
* file name;
* project role;
* headings;
* and safe summary metadata.

---

## 72.1 No AI Summary

Version 1 document FTS does not depend on model-generated summaries.

---

# 73. FTS Query Construction

The user query never becomes raw SQL or raw MATCH syntax.

---

## 73.1 Query Tokens

Trusted code converts query text into:

* exact phrase;
* safe prefix;
* identifier alternatives;
* path terms;
* and optional NEAR expressions.

---

## 73.2 Operator Escaping

Escape:

* quotes;
* parentheses;
* column syntax;
* boolean operators;
* prefix markers;
* and NEAR syntax.

---

## 73.3 Advanced Search

An expert advanced syntax may be introduced only through a separately parsed bounded grammar.

---

## 73.4 Empty Query

No FTS scan.

---

# 74. FTS Query Limits

Suggested limits:

* query UTF-8 bytes: 16 KiB;
* lexical terms: 64;
* phrases: 32;
* prefixes: 16;
* NEAR groups: 8;
* result candidates: 1,000;
* and execution timeout: 2 seconds interactive.

---

## 74.1 Pathological Query

Reject excessive or recursive syntax.

---

## 74.2 Cancellation

Use SQLite interruption on cancellation.

---

# 75. FTS5 Integrity

Run:

* ordinary SQLite integrity check;
* FTS5 integrity-check;
* row mapping check;
* chunk count comparison;
* and sample source-hash verification.

---

## 75.1 Schedule

* after initial build;
* after recovery;
* after migration;
* periodically during idle;
* and after suspected corruption.

---

## 75.2 Failure

Stop serving affected lexical generation.

---

# 76. FTS5 Secure Delete

Enable FTS5 secure-delete in the pinned compatible build.

---

## 76.1 Purpose

Reduce recoverable old term entries after updates and deletes.

---

## 76.2 Limitation

It does not promise forensic deletion from storage media, WAL, backups or old generations.

---

## 76.3 Old Generations

Purge old generation files according to retention.

---

# 77. FTS5 Merge and Optimise

Incremental updates create FTS segments.

---

## 77.1 Ordinary Maintenance

Use bounded incremental merges.

---

## 77.2 Full Optimise

Run only:

* during idle;
* with cancellation;
* with storage budget;
* and with visible maintenance state.

---

## 77.3 No Foreground Stall

Do not block an interactive query for a full optimise.

---

# 78. FTS Rebuild

Because contentless FTS cannot rebuild itself from an external content table, Opure rebuilds lexical indexes from trusted chunk records and Workspace snapshots.

---

## 78.1 Staging

Build a new staging generation.

---

## 78.2 No In-Place Guess

Do not repair by manipulating FTS shadow tables.

---

# 79. Lexical Index Staleness

A lexical result is stale when:

* active workspace generation advanced;
* source hash changed;
* chunk removed;
* policy changed;
* or index revision is behind mandatory updates.

---

## 79.1 Serve Stale?

Default:

* exact metadata may be shown as stale;
* Context Assembly cannot use stale content without revalidation;
* and strict queries may wait or fall back to direct Workspace search.

---

# 80. Exact Path Index

Normal SQLite table indexed by:

* exact logical relative path;
* case-aware path key;
* filename;
* extension;
* and directory.

---

## 80.1 Case Sensitivity

Respect root and directory case sensitivity under ADR-0009.

---

## 80.2 Path Prefix

Use bounded prefix search over normalised path components.

---

## 80.3 Fuzzy Path

A deterministic fuzzy filename scorer may be added after exact and prefix results.

---

# 81. Exact Symbol Index

Normal tables support:

* exact name;
* exact qualified name;
* signature hash;
* kind;
* container;
* and project.

---

## 81.1 Case

Use language-specific comparison.

---

## 81.2 Overloads

Return each overload with signature.

---

## 81.3 Ambiguity

Do not collapse distinct symbols.

---

# 82. Graph Storage

Graph edges live in normal SQLite tables.

---

## 82.1 Edge Indexes

Index:

* source node;
* target node;
* edge type;
* project;
* and confidence.

---

## 82.2 Reverse Lookup

Support reverse edges without duplicating authority if practical.

---

## 82.3 Traversal Depth

Bound interactive traversal depth and node count.

---

## 82.4 Cycle Handling

Track visited nodes.

---

# 83. Graph Query Channels

Initial channels:

* declaration;
* references;
* callers;
* callees;
* implementations;
* base types;
* derived types;
* tests;
* imports;
* project dependencies;
* and configuration relations.

---

## 83.1 No Arbitrary Recursive SQL

Use bounded trusted traversal.

---

# 84. Structural Confidence

Possible confidence:

* Compiler Semantic;
* Parser Structural;
* Project Metadata;
* Heuristic;
* and Unknown.

---

## 84.1 Fusion Weight

Higher-confidence direct evidence receives stronger priority.

---

# 85. Embedding Profile

Suggested schema:

```text
opure.embedding-profile/1
```

---

## 85.1 Fields

```text
embedding_profile_id
revision
provider_profile
model_profile
dimensions
input_limit
normalisation
distance_metric
input_template
chunker_profiles
data_classes
local_or_remote
storage_type
verified_at
quality_evidence
```

---

## 85.2 One Profile per Vector Space

Vectors from different profiles are never mixed.

---

## 85.3 Model Change

Requires new embeddings.

---

## 85.4 Dimension Change

Requires new segment generation.

---

# 86. Embedding Input

Embedding input is deterministic and versioned.

---

## 86.1 Possible Fields

* project-relative path;
* language;
* symbol signature;
* source chunk;
* documentation;
* and bounded structural labels.

---

## 86.2 No Hidden Metadata

Every included field is documented.

---

## 86.3 Prompt Injection

Embedding models do not grant instruction authority, but malicious content can influence similarity.

---

## 86.4 Truncation

Do not silently truncate embedding input.

Split or mark ineligible through chunk policy.

---

# 87. Local Embeddings

Preferred initial semantic path.

---

## 87.1 Model Profile

Use ADR-0020 local embedding model.

---

## 87.2 Background Scheduling

Embedding work runs below interactive generation and build priorities.

---

## 87.3 Resource Budget

Bound:

* batch;
* RAM;
* GPU;
* duration;
* queue;
* and daily work.

---

## 87.4 No Inference Log

Do not log source chunks.

---

# 88. Remote Embeddings

Remote embedding requires:

* approved Provider Profile;
* exact Data Sharing Plan;
* project cloud policy;
* secret scan;
* explicit data classes;
* bounded batch;
* cost;
* retention posture;
* and request receipts.

---

## 88.1 No Automatic Project Open Upload

Opening a project never starts remote embedding.

---

## 88.2 Incremental Approval

Approved Providers Only may permit bounded ongoing embedding under an exact policy.

---

## 88.3 Provider State

Provider-side vector stores remain disabled.

---

# 89. Embedding Records

A record includes:

```text
embedding_id
chunk_id
embedding_profile
input_sha256
vector_segment
vector_offset
dimensions
norm
created_at
provider_receipt
valid
```

---

## 89.1 Input Hash

Binds the exact embedding rendering.

---

## 89.2 Invalid Vector

Exclude on:

* wrong dimensions;
* NaN;
* infinity;
* zero norm where not permitted;
* or segment mismatch.

---

# 90. Vector Segment Format

Suggested schema:

```text
opure.vector-segment/1
```

---

## 90.1 Header

Contains:

```text
magic
format_version
project_id_hash
embedding_profile_hash
dimensions
element_type
normalisation
distance_metric
entry_count
record_stride
data_offset
created_at
header_sha256
data_sha256
```

---

## 90.2 Entry Mapping

Use a separate checksummed mapping from vector ordinal to:

* chunk ID;
* embedding ID;
* and input hash.

---

## 90.3 Endianness

Specify explicitly.

---

## 90.4 Alignment

Specify and validate.

---

## 90.5 Float32

Initial element type.

---

## 90.6 No Executable Content

Segment is data only.

---

# 91. Vector Segment Lifecycle

New embeddings write to a staging segment.

---

## 91.1 Seal

A segment is immutable after sealing.

---

## 91.2 Verify

Verify:

* header;
* length;
* hashes;
* dimensions;
* finite values;
* norms;
* and mapping.

---

## 91.3 Publish

Commit manifest and SQLite references transactionally.

---

## 91.4 Delete

Remove only after no active generation references it.

---

# 92. Vector Search Worker

Exact search runs in a dedicated trusted worker.

---

## 92.1 Inputs

* query vector;
* embedding profile;
* segment references;
* candidate filters;
* top-k;
* score threshold;
* and cancellation.

---

## 92.2 Validation

Validate all segments before mapping.

---

## 92.3 Read-Only

Map read-only.

---

## 92.4 No Network

No network required.

---

## 92.5 No Source Content

The worker receives vectors and identifiers, not source text.

---

# 93. Exact Similarity

For normalised float32 vectors:

```text
cosine_similarity = dot(query, candidate)
```

when both norms are verified to be approximately one.

---

## 93.1 Non-Normalised

Use explicit cosine calculation only if the profile declares it.

---

## 93.2 Distance Conversion

Retain raw similarity.

Do not hide conversion to a rank feature.

---

## 93.3 Floating-Point Determinism

Results may differ slightly by architecture or SIMD path.

Stable tie-breaking uses chunk ID after a configured epsilon.

---

# 94. SIMD Implementation

Use stable .NET hardware-accelerated vector primitives or reviewed intrinsics.

---

## 94.1 Managed Boundary

Prefer managed code for the first exact scanner.

---

## 94.2 Fallback

Provide a scalar reference implementation.

---

## 94.3 Verification

Compare SIMD and scalar results within tolerance.

---

## 94.4 CPU Features

Record available vector width and hardware acceleration.

---

# 95. Exact Search Algorithm

Conceptual:

1. validate query vector;
2. choose compatible segments;
3. apply project and generation filter;
4. apply eligibility bitmap;
5. scan each vector;
6. calculate similarity;
7. maintain bounded top-k heap;
8. apply stable tie-breaking;
9. return identifiers and raw scores;
10. revalidate metadata in SQLite.

---

## 95.1 Complexity

Linear in eligible vectors.

---

## 95.2 Cancellation

Check at bounded intervals.

---

## 95.3 Parallelism

Use bounded parallel segment scanning only after evidence.

---

## 95.4 Memory

Do not copy entire segments unnecessarily.

---

# 96. Vector Search Limits

Suggested initial limits:

* dimensions: 4,096;
* query vectors: 1;
* top-k: 200;
* candidate vectors per query: 250,000 default;
* hard scan vectors: 1,000,000 after explicit performance profile;
* segment size: 1 GiB;
* timeout: 2 seconds interactive;
* and concurrent semantic queries: 2.

Values require benchmarking.

---

## 96.1 Larger Projects

For projects beyond tested limits:

* lexical and graph retrieval remain available;
* semantic coverage may be limited;
* semantic search may require explicit longer operation;
* or future ANN support.

---

# 97. Semantic Coverage

Coverage record includes:

```text
eligible_chunks
embedded_chunks
valid_vectors
pending_chunks
failed_chunks
excluded_chunks
coverage_percentage
embedding_profile
workspace_generation
```

---

## 97.1 Ready Threshold

No universal threshold.

The UI reports exact coverage.

---

## 97.2 Partial Coverage

Semantic results must state partial coverage.

---

## 97.3 Query Against Partial Coverage

Allowed when visible.

---

# 98. Vector Deletion

Immutable segments are not edited in place.

---

## 98.1 Tombstones

SQLite metadata marks obsolete entries invalid.

---

## 98.2 Query Filter

Search uses an active-entry bitmap or mapping.

---

## 98.3 Compaction

Build new segments during idle or threshold-based maintenance.

---

## 98.4 Sensitive Deletion

A secret-classification incident triggers prompt compaction or generation rebuild.

Do not rely solely on tombstones.

---

# 99. Vector Compaction

Compaction:

* selects active entries;
* writes new segments;
* verifies;
* publishes new manifest;
* and retires old segments.

---

## 99.1 Cancellation

Old generation remains valid.

---

## 99.2 No In-Place Rewrite

Never partially rewrite a sealed segment.

---

# 100. ANN Deferral

Version 1 does not use:

* HNSW;
* IVF;
* DiskANN;
* product quantisation;
* or other approximate indexes.

---

## 100.1 Future Acceptance Requirements

A future ANN decision must cover:

* recall;
* exact baseline;
* build time;
* update and delete;
* corruption;
* persistence;
* memory;
* native safety;
* Windows packaging;
* version migration;
* filters;
* reproducibility;
* and fallback.

---

# 101. `sqlite-vec` Evaluation

A future experiment may use a pinned release.

---

## 101.1 Static or Allowlisted Integration

Prefer static linking or one first-party signed component.

---

## 101.2 No General `.load`

Do not expose SQLite `.load` or arbitrary path loading.

---

## 101.3 Exact Mode First

Compare exact KNN against the managed scalar baseline.

---

## 101.4 ANN Separate

Do not treat alpha DiskANN or IVF as accepted because exact mode passes.

---

# 102. Incremental Update Sources

Trusted triggers include:

* Workspace save;
* Workspace file create;
* Workspace delete;
* Workspace rename;
* repository checkout;
* repository merge;
* project-file change;
* Index Policy change;
* parser update;
* chunker update;
* embedding-profile update;
* secret-rule update;
* and reconciliation.

---

## 102.1 File Watcher

Watcher events are hints.

---

## 102.2 External Editors

Workspace reconciliation detects changes.

---

## 102.3 Unsaved Buffer

Handled by overlay, not durable index.

---

# 103. Update Debounce

Coalesce bursts.

Suggested default:

```text
250–750 ms
```

for ordinary saves, with immediate handling for explicit query revalidation.

---

## 103.1 Repository Checkout

Use a wider batch and suspend ordinary per-file churn until repository state stabilises.

---

## 103.2 Maximum Delay

Do not leave mandatory lexical state indefinitely stale.

---

# 104. Incremental Update Transaction

Conceptual transaction:

1. verify source snapshots;
2. calculate deletions;
3. delete old FTS rows;
4. delete old symbols, references and edges;
5. insert document version;
6. insert chunks;
7. insert FTS rows;
8. insert symbols and graph;
9. mark old embeddings invalid;
10. queue new embeddings;
11. update revision and coverage;
12. commit.

---

## 104.1 Vector Publication

New vector segments may publish in a later semantic revision.

---

## 104.2 Failure

Rollback transaction.

---

# 105. Rename Handling

A rename may preserve continuity.

---

## 105.1 Evidence

Use Repository and Workspace identity.

---

## 105.2 Lexical Update

Path terms change even when content does not.

---

## 105.3 Embedding

If embedding input includes path, re-embed.

If not, vector may be reusable under the exact profile.

---

## 105.4 Chunk ID

Path-bound chunk identity may change.

---

# 106. Delete Handling

Delete:

* document eligibility;
* FTS rows;
* symbols;
* references;
* graph edges;
* and embedding active mappings.

---

## 106.1 Historical Receipt

Query receipts may preserve old safe labels and hashes.

---

# 107. Repository Checkout

A branch checkout can change many files.

---

## 107.1 Batch Mode

Create one reconciliation operation.

---

## 107.2 Query Behaviour

Serve old results as Stale or suspend strict queries until mandatory indexing catches up.

---

## 107.3 Context Assembly

Must revalidate and therefore will not use wrong-branch content.

---

# 108. Project Configuration Change

A project-file or target-framework change may alter semantic meaning.

---

## 108.1 Structural Invalidation

Rebuild affected C# semantic indexes.

---

## 108.2 Lexical Reuse

Unchanged file lexical chunks may remain valid.

---

## 108.3 Graph Rebuild

Recompute project and reference edges.

---

# 109. Parser Update

A Parser Profile change invalidates:

* parse results;
* symbols;
* references;
* graph;
* and parser-dependent chunks.

---

## 109.1 Lexical Raw Content

May remain valid if chunk identity and rendering do not change.

---

# 110. Chunker Update

Invalidates chunks and all dependent indexes.

---

# 111. Embedding Update

An Embedding Profile change invalidates semantic vectors only.

Lexical and structural retrieval remain available.

---

# 112. Secret Rule Update

Re-evaluate eligible documents.

---

## 112.1 New Secret Finding

Immediately:

* exclude affected chunks from query;
* invalidate vectors;
* remove FTS rows in a transaction;
* queue secure rebuild or compaction;
* and record a security event.

---

# 113. Full Reconciliation

Reconciliation compares:

* project inventory;
* file identities;
* hashes;
* eligibility;
* index records;
* FTS row mapping;
* symbols;
* edges;
* and embedding coverage.

---

## 113.1 Schedule

Suggested:

* project open after unclean shutdown;
* repository checkout;
* daily while active;
* after watcher overflow;
* after policy change;
* and on demand.

---

## 113.2 No Unnecessary Read

Use metadata and hashes intelligently while preserving correctness.

---

# 114. Unsaved Overlay

The overlay indexes current unsaved buffers.

---

## 114.1 Scope

* current session;
* current project;
* Desktop or editor capability;
* and current buffer revision.

---

## 114.2 Storage

Memory only.

---

## 114.3 Features

Initially:

* exact path;
* current buffer content;
* syntax symbols where available;
* lexical search;
* and direct range lookup.

---

## 114.4 Embeddings

Disabled by default for unsaved buffers.

---

## 114.5 Fusion

Overlay results outrank durable results for the same file.

---

## 114.6 Close

Discard on save, buffer close or session end.

---

# 115. Query API

Initial query types:

* Find Path;
* Find Symbol;
* Search Lexical;
* Search Semantic;
* Find References;
* Find Callers;
* Find Implementations;
* Find Tests;
* Find Related Files;
* Hybrid Search;
* and Explain Result.

---

## 115.1 Structured Query

Use typed fields.

---

## 115.2 No SQL

Callers cannot submit SQL.

---

## 115.3 No Raw Vector File Access

Callers provide query text or an approved query vector reference.

---

# 116. Query Plan

Suggested schema:

```text
opure.knowledge-query-plan/1
```

---

## 116.1 Fields

```text
query_id
project_id
query_kind
query_text
exact_path
symbol
filters
channels
channel_limits
graph_depth
embedding_profile
fusion_profile
result_limit
index_generation
overlay_revision
created_at
expires_at
```

---

## 116.2 Model Suggestion

A model may propose a query.

Trusted code validates and executes it.

---

# 117. Query Filters

Possible filters:

* relative path prefix;
* extension;
* language;
* symbol kind;
* project role;
* tracked state;
* generated state;
* source class;
* data class;
* test source;
* documentation;
* and current diff.

---

## 117.1 Filter Authority

Filters narrow.

They do not broaden index eligibility.

---

# 118. Query Normalisation

Normalise query text for:

* Unicode;
* whitespace;
* path separators;
* identifier splitting;
* and language profile.

Preserve original query.

---

## 118.1 Case

Use channel-specific case policy.

---

## 118.2 Empty and Stop Terms

Avoid broad accidental scans.

---

# 119. Exact Retrieval

Exact path and exact symbol channels run first.

---

## 119.1 Direct Hit

A direct exact hit receives a deterministic reason and priority.

---

## 119.2 Multiple Symbols

Return all distinct overloads or scopes.

---

# 120. Lexical Retrieval

FTS query returns bounded candidate rows and raw BM25.

---

## 120.1 Snippets

Because FTS is contentless, retrieve source text through Workspace after candidate selection.

---

## 120.2 Highlight

Generate highlights deterministically from current snapshot or matched token offsets when available.

---

## 120.3 No Stale Snippet

Do not display an old stored snippet as current source.

---

# 121. Structural Retrieval

Graph queries produce candidates with:

* edge type;
* graph distance;
* confidence;
* and source symbol.

---

## 121.1 Depth

Default one or two edges.

---

## 121.2 Node Cap

Bound.

---

# 122. Semantic Retrieval

Query text is embedded using the exact Embedding Profile.

---

## 122.1 Local Default

Local embedding where installed.

---

## 122.2 Remote Query Embedding

Remote query text transmission is a separate approved provider operation.

---

## 122.3 No Semantic Profile

Return Semantic Unavailable, not a hidden provider fallback.

---

## 122.4 Candidate Filter

Search only active eligible entries.

---

# 123. Query Embedding Cache

Cache by:

* exact normalised query;
* embedding profile;
* input template;
* project data classification where relevant;
* and expiry.

---

## 123.1 User Request Sensitivity

A query may contain sensitive text.

Do not persist query embeddings indefinitely by default.

---

# 124. Hybrid Retrieval

Hybrid retrieval combines eligible channel results.

---

## 124.1 Default Channel Order

1. exact path;
2. exact symbol;
3. direct diagnostic or diff relation;
4. lexical;
5. graph;
6. semantic;
7. documentation;
8. memory.

---

## 124.2 Candidate Union

Keep channel provenance.

---

## 124.3 Deduplication

Merge the same chunk or symbol while retaining channel scores.

---

# 125. Reciprocal Rank Fusion

A candidate initial fusion:

```text
RRF(d) = Σ 1 / (k + rank_channel(d))
```

where `k` and channel weights are versioned.

---

## 125.1 Exact Boost

Exact path and symbol hits receive deterministic priority outside ordinary RRF.

---

## 125.2 Channel Weight

Task-specific policy may weight:

* lexical;
* semantic;
* graph;
* docs;
* and memory.

---

## 125.3 Raw Scores

Retain BM25, cosine and graph distance.

---

# 126. Feature Adjustment

After fusion, apply inspectable features:

* explicit path mention;
* explicit symbol mention;
* current diff;
* diagnostic link;
* source trust;
* generated penalty;
* third-party penalty;
* stale penalty;
* diversity gain;
* and size cost.

---

## 126.1 No Opaque Final Score

The UI and receipt should explain final ordering.

---

# 127. Deterministic Tie-Breaking

Suggested order:

1. exactness;
2. fused score;
3. direct evidence;
4. path;
5. range;
6. chunk ID.

---

# 128. Result Diversity

Apply caps by:

* file;
* directory;
* symbol;
* channel;
* generated source;
* external source;
* and memory.

---

## 128.1 Direct Evidence Exception

Do not suppress mandatory direct evidence.

---

# 129. Result Record

Suggested fields:

```text
result_id
query_id
project_id
generation_id
index_revision
workspace_generation
document_id
chunk_id
symbol_id
relative_path
range
source_hash
data_class
source_class
channels
raw_scores
fusion_score
feature_adjustments
rank
reason
stale
overlay
```

---

## 129.1 No Authority Token

A result ID is not sufficient to read source later.

Context Assembly requests revalidation.

---

# 130. Result Explanation

Examples:

* Exact symbol match;
* Exact path match;
* Contains all lexical terms in a symbol declaration;
* Direct caller of selected method;
* Test referencing changed class;
* Semantically similar source with 0.82 cosine;
* Current diff file;
* Project memory referring to this path;
* and Third-party generated result penalised.

---

# 131. Query Receipt

Suggested schema:

```text
opure.knowledge-query-receipt/1
```

---

## 131.1 Fields

```text
query_id
project_id
query_plan
generation_id
index_revision
workspace_generation
overlay_revision
channels
candidate_counts
latencies
embedding_profile
semantic_coverage
fusion_profile
results
omissions
stale_state
created_at
completed_at
```

---

## 131.2 No Full Source

Safe labels and hashes by default.

---

# 132. Memory Integration

Memory remains a separate service and channel.

---

## 132.1 Query

Project Knowledge Service may request project-scoped memory candidates.

---

## 132.2 Fusion

Retain:

* memory ID;
* confidence;
* source;
* freshness;
* and supersession.

---

## 132.3 No Copy as Source

Do not present memory text as repository source.

---

# 133. Build and Test Integration

Current structured diagnostics may act as direct query seeds.

---

## 133.1 Not Durable Source Index

Build and test records remain owned by their services.

---

## 133.2 Short-Lived Channel

Fuse current run evidence with durable project index.

---

# 134. Repository History Integration

Repository history remains a separate explicit query.

---

## 134.1 Current Index

The durable knowledge index represents current saved workspace state.

---

## 134.2 Historical Search

A future historical index or on-demand commit search requires separate policy.

---

# 135. Context Assembly Integration

Context Assembly requests:

* query plan;
* result cap;
* project;
* source classes;
* and task.

---

## 135.1 Revalidation

For selected results, Workspace confirms:

* current project;
* current source hash;
* current range;
* classification;
* and access.

---

## 135.2 Stale Result

Discard or re-query.

---

## 135.3 Token Budget

Project Knowledge Service does not decide final token admission.

---

# 136. AI Router Integration

AI Router has no direct index access.

---

## 136.1 Model Query Proposal

A model may propose:

* exact symbol;
* path;
* lexical query;
* or related-context request.

Context Assembly and Project Knowledge mediate it.

---

# 137. Plugin Integration

A plugin permission may allow:

* exact lookup;
* bounded lexical search;
* or approved symbol search.

---

## 137.1 Result Limit

Capability binds project, query type and result cap.

---

## 137.2 No Embedding Access

Plugins do not receive vector files.

---

## 137.3 No Raw Database

Denied.

---

# 138. MCP Integration

An MCP server cannot query the local project index directly by default.

---

## 138.1 Future Tool

A trusted Opure MCP client tool may expose a bounded search operation after explicit project and data approval.

---

## 138.2 Remote Data

Returning source snippets to a remote MCP server is external transmission and requires ADR-0018 approval.

---

# 139. Search UI

Developer-facing search may use:

* exact path;
* symbols;
* text;
* related code;
* callers;
* implementations;
* tests;
* and semantic search.

---

## 139.1 Channel Visibility

Show which channel produced each result.

---

## 139.2 Semantic Toggle

Semantic retrieval can be disabled.

---

## 139.3 Coverage

Show embedding coverage and model.

---

# 140. Index Status UI

Display:

* active generation;
* indexed files;
* excluded files;
* lexical readiness;
* structural readiness;
* semantic coverage;
* embedding model;
* storage;
* last reconciliation;
* pending work;
* failures;
* and health.

---

# 141. Inclusion Review UI

Allow the developer to inspect:

* included tracked files;
* included untracked files;
* ignored files;
* generated exclusions;
* large files;
* secret exclusions;
* binary exclusions;
* and explicit overrides.

---

## 141.1 No Secret Preview

Do not show secret values.

---

# 142. Exclusion Reason UI

Suggested text categories:

* Protected by Opure;
* Outside Project;
* Git Ignored;
* Dependency;
* Generated;
* Binary;
* Too Large;
* Pathological Text;
* Secret Detected;
* Unsupported Encoding;
* Unsupported Content;
* Policy Excluded;
* Enterprise Excluded;
* and User Excluded.

---

# 143. Trust Centre

Trust Centre should show:

* project indexes;
* Index Policy;
* active generation;
* storage location;
* FTS5 schema;
* parser packages;
* grammar packages;
* embedding profile;
* remote embedding transmissions;
* semantic coverage;
* file exclusion counts;
* secret exclusions;
* index maintenance;
* integrity checks;
* corruption;
* rebuilds;
* query receipts;
* and deletion.

---

## 143.1 Source Visibility

Show file paths and classifications where project policy permits.

---

## 143.2 Embedding Evidence

Show:

* local or remote;
* model;
* provider;
* input template;
* dimensions;
* and vector count.

---

# 144. Diagnostics

Structured diagnostics may include:

* project ID;
* operation ID;
* generation;
* index revision;
* file counts;
* chunk counts;
* parser profile;
* FTS timing;
* vector timing;
* memory;
* storage;
* and failure category.

---

## 144.1 Prohibited Logs

Do not log:

* source content;
* secrets;
* embedding values;
* raw query text when sensitive;
* provider credentials;
* or absolute project paths without need.

---

# 145. Metrics

Local low-cardinality metrics may include:

* projects indexed;
* files indexed;
* chunks;
* lexical query latency;
* vector query latency;
* graph query latency;
* semantic coverage;
* update queue;
* reconciliation duration;
* parser failures;
* and corruption events.

---

## 145.1 No Project Name Labels

Do not export project identity as a metric label.

---

# 146. Storage Budget

Suggested default:

* metadata and FTS: up to 2 GiB per project;
* vector segments: up to 4 GiB per project;
* temporary staging: up to active index size plus 25%;
* retained old generation: one;
* and global index budget: user configurable.

---

## 146.1 Warning

Warn before exceeding budget.

---

## 146.2 Hard Limit

Pause optional semantic work before deleting useful lexical state.

---

## 146.3 Eviction

Do not evict an active project index silently.

---

# 147. Storage Estimation

Estimate from:

* source bytes;
* chunk count;
* FTS term count;
* symbol count;
* edge count;
* embedding dimensions;
* element type;
* and retained generations.

---

# 148. Maintenance Priority

Suggested order:

1. security purge;
2. corruption recovery;
3. mandatory incremental lexical;
4. mandatory structural;
5. query;
6. reconciliation;
7. embeddings;
8. vector compaction;
9. FTS optimise;
10. benchmark.

---

# 149. Background Resource Policy

Use ADR-0020 performance modes.

---

## 149.1 Eco

* no optional semantic indexing while busy;
* low CPU;
* small batches;
* and aggressive pause.

---

## 149.2 Balanced

* bounded background lexical and structural;
* embedding only when resources allow;
* and preserve build responsiveness.

---

## 149.3 Performance

* larger batches;
* more embedding concurrency;
* and faster reconciliation.

---

## 149.4 Turbo

Explicit maintenance or benchmark session.

---

# 150. Query Responsiveness

Interactive search should pre-empt background indexing within bounded cancellation points.

---

## 150.1 Writer Contention

Use WAL and short transactions.

---

## 150.2 Vector Build

Do not lock active query segments.

---

# 151. Index Deletion

Deleting project knowledge:

* stops indexing;
* cancels queries;
* closes database;
* retires generations;
* removes vector segments;
* removes overlays;
* preserves only policy-required query or security evidence;
* and leaves source files untouched.

---

## 151.1 Preview

Show storage reclaimed and active workflows affected.

---

## 151.2 Rebuild

The project can operate without an index and rebuild later.

---

## 151.3 Secure Deletion

Do not promise forensic secure deletion.

---

# 152. Project Removal

Project removal should offer:

* retain index;
* delete index;
* or archive metadata

according to product policy.

Default should delete derived index after a reviewable retention period.

---

# 153. Backup

Knowledge indexes are derived and excluded from ordinary backup by default.

---

## 153.1 Backup Metadata

May retain:

* Index Policy;
* parser profiles;
* embedding profile;
* and index preferences.

---

## 153.2 Rebuild

Restore rebuilds from source.

---

# 154. Channel Isolation

Stable, Preview and Development indexes remain logically separate.

---

## 154.1 Physical Sharing

No shared mutable database.

---

## 154.2 Embedding Segment Deduplication

Deferred until authority and deletion can remain separate.

---

# 155. Offline Behaviour

All lexical, structural and local semantic queries work offline.

---

## 155.1 Remote Embedding Profile

Pending remote embeddings pause offline.

---

## 155.2 No Source Contact

The index does not contact repository hosts or model providers during ordinary offline queries.

---

# 156. Index Health

Health dimensions:

* Database;
* FTS;
* Metadata Mapping;
* Source Freshness;
* Parser;
* Graph;
* Vector Segments;
* Embedding Coverage;
* Storage;
* and Reconciliation.

---

## 156.1 Overall State

The most severe mandatory dimension determines overall readiness.

---

## 156.2 Semantic Degradation

Vector corruption need not disable lexical search.

---

# 157. Integrity Checks

### 157.1 SQLite

* quick check;
* full integrity check when needed;
* foreign-key check;
* schema version.

### 157.2 FTS5

* integrity-check;
* row mapping;
* index count;
* safe sample query.

### 157.3 Graph

* foreign references;
* invalid node;
* edge count;
* cycle sanity where relevant.

### 157.4 Vectors

* header;
* file length;
* hashes;
* dimensions;
* finite values;
* mapping;
* active bitmap.

### 157.5 Manifest

* complete file set;
* checksums;
* revision;
* policy;
* and coverage.

---

# 158. Corruption Response

On mandatory database or FTS corruption:

1. stop strict query serving;
2. mark generation Corrupted;
3. preserve safe evidence;
4. build staging generation;
5. verify against Workspace;
6. publish atomically;
7. retain old generation for bounded investigation;
8. and clean after approval or policy.

---

## 158.1 Vector-Only Corruption

Disable semantic channel and continue lexical and structural if healthy.

---

## 158.2 Parser Corruption

Quarantine parser profile and rebuild affected language with fallback.

---

# 159. Unclean Shutdown

On next open:

* inspect operation journal;
* verify active pointer;
* verify SQLite;
* verify vector manifest;
* discard incomplete staging;
* and reconcile pending file events.

---

# 160. Migration

Index schema migration should prefer rebuild.

---

## 160.1 Small Metadata Migration

May be transactional.

---

## 160.2 FTS Schema Change

Build a new generation.

---

## 160.3 Vector Format Change

Build new segments.

---

# 161. Query Failure Model

Stable categories:

* Knowledge Index Disabled;
* Knowledge Index Missing;
* Knowledge Index Building;
* Knowledge Index Stale;
* Knowledge Index Corrupted;
* Knowledge Project Mismatch;
* Knowledge Policy Denied;
* Knowledge Query Invalid;
* Knowledge Query Too Large;
* Knowledge Query Timed Out;
* Knowledge Path Not Found;
* Knowledge Symbol Ambiguous;
* Knowledge Parser Unavailable;
* Knowledge Structural Unavailable;
* Knowledge Semantic Unavailable;
* Knowledge Semantic Partial;
* Knowledge Embedding Profile Changed;
* Knowledge Vector Segment Invalid;
* Knowledge FTS Invalid;
* Knowledge Result Stale;
* Knowledge Source Revalidation Failed;
* Knowledge Operation Cancelled;
* and Knowledge Recovery Required.

---

# 162. Security Threat Model

Relevant threats include:

* path escape;
* reparse-point escape;
* wrong-project indexing;
* cross-project search;
* protected-root indexing;
* secret lexical indexing;
* secret embedding;
* source-content duplication;
* malicious file encoding;
* parser exploit;
* Roslyn memory exhaustion;
* Tree-sitter grammar compromise;
* dynamic grammar download;
* project-code execution;
* source-generator execution;
* analyzer execution;
* MSBuild target execution;
* malicious FTS query;
* FTS shadow-table manipulation;
* arbitrary SQLite extension loading;
* malformed vector segment;
* NaN vector;
* dimension confusion;
* vector mapping substitution;
* semantic poisoning;
* keyword stuffing;
* malicious repository instruction;
* generated-code domination;
* stale index;
* stale embedding;
* watcher overflow;
* active-generation pointer substitution;
* index database corruption;
* index rollback;
* query receipt mismatch;
* and denial of service through repository size or query volume.

---

# 163. Security Controls

Controls include:

* verified project roots;
* ADR-0009 filesystem boundary;
* project-scoped index directories;
* opaque project IDs;
* explicit Index Policy;
* protected-root deny;
* secret scanning before indexing;
* no arbitrary extensions;
* FTS query construction;
* isolated Analysis Worker;
* no project-code execution;
* pinned parsers;
* bounded parsing;
* immutable source hashes;
* contentless FTS5;
* FTS secure-delete;
* immutable vector segments;
* checksums;
* exact dimension binding;
* local embeddings by default;
* remote Data Sharing Plans;
* deterministic retrieval;
* generation manifests;
* active-pointer validation;
* Workspace revalidation;
* corruption rebuild;
* and Trust Centre evidence.

---

# 164. Security Limitations

This design cannot guarantee:

* perfect secret detection;
* absence of parser vulnerabilities;
* absence of SQLite vulnerabilities;
* absence of semantic poisoning;
* perfect search relevance;
* complete forensic deletion;
* same-user file protection;
* exact cross-hardware floating-point identity;
* or safety after a developer explicitly indexes sensitive content locally.

An embedding can retain semantic information even when source text is not stored beside it.

The product must state these limitations honestly.

---

# 165. Privacy Impact

The local index contains sensitive project derivatives including:

* terms;
* paths;
* symbols;
* relationships;
* classifications;
* and embeddings.

It must be protected like project source.

---

## 165.1 Local Default

Indexing and query remain local.

---

## 165.2 Remote Embeddings

Remote embedding discloses the exact approved chunks to the provider.

---

## 165.3 Query Text

A remote embedding of a semantic query also discloses the query.

---

## 165.4 No Telemetry Content

Do not transmit paths, terms, vectors or source through product telemetry.

---

# 166. Reliability Impact

A derived index can fail without corrupting source.

The architecture supports:

* lexical-only operation;
* structural-only degradation;
* semantic-only degradation;
* full rebuild;
* and offline recovery.

---

# 167. Performance Impact

Costs include:

* source hashing;
* parsing;
* FTS writes;
* symbol extraction;
* graph construction;
* embedding generation;
* vector scan;
* storage;
* and periodic reconciliation.

These costs should remain subordinate to foreground development work.

---

# 168. Exact Vector Search Trade-Off

Exact scan offers:

* complete recall within indexed vectors;
* simple deletion filtering;
* deterministic baseline;
* no ANN build;
* and easier validation.

It costs linear query work.

Version 1 accepts this trade-off under explicit project-size limits.

---

## 168.1 Future Trigger

Revisit ANN when measured projects exceed one or more of:

* semantic query p95 target;
* vector storage target;
* background compaction target;
* or tested chunk count.

---

# 169. Resource Scheduling

Project indexing uses the shared Scheduler and performance modes.

---

## 169.1 CPU

Parsing, hashing, FTS and exact vector search are CPU bounded.

---

## 169.2 GPU

Local embeddings may use GPU.

---

## 169.3 RAM

Roslyn and vector mapping require bounded memory.

---

## 169.4 Disk

Initial build and rebuild require staging space.

---

## 169.5 I/O Priority

Use background I/O priority where supported and proven.

---

# 170. Indexing Concurrency

Suggested Version 1 defaults:

* file classification workers: 2–4;
* parser workers: 1–2;
* SQLite writer: 1;
* embedding batch: model specific;
* vector compaction: 1;
* project initial builds concurrently: 1;
* and interactive queries: bounded separately.

---

## 170.1 Dynamic Adjustment

Scheduler may reduce concurrency under build, test or memory pressure.

---

# 171. Memory Limits

Suggested provisional limits:

* Analysis Worker: 2 GiB;
* Search Worker: 1 GiB plus mapped segments;
* Project Knowledge Service managed heap: 1 GiB;
* individual parse tree: 256 MiB;
* Roslyn compilation batch: 1 GiB;
* and query candidate materialisation: 256 MiB.

Exact values require evidence.

---

# 172. CPU Limits

Background index work should not consume all logical processors.

Balanced mode should reserve capacity for:

* Desktop;
* Runtime;
* build;
* tests;
* and developer applications.

---

# 173. Disk I/O Limits

Use bounded read queues.

---

## 173.1 Source Reads

Avoid rereading unchanged content.

---

## 173.2 Hash Cache

A file identity, size, timestamp and journal hint may optimise hash decisions.

Content hash remains authoritative when required.

---

## 173.3 Cloud Files

Do not trigger hidden hydration.

---

# 174. Query Performance Targets

Provisional reference-hardware targets:

* exact path p95: under 10 ms;
* exact symbol p95: under 25 ms;
* lexical top-100 p95: under 100 ms for 1,000,000 chunks;
* one-hop graph p95: under 50 ms;
* exact semantic top-100 p95: under 250 ms for 250,000 vectors at 768 dimensions;
* hybrid fusion p95: under 20 ms after channel completion;
* result explanation p95: under 10 ms;
* and source revalidation for 20 results p95: under 100 ms excluding cold disk.

These are targets pending evidence.

---

# 175. Initial Build Targets

Provisional:

* inventory 100,000 files: under 60 seconds excluding inaccessible files;
* lexical and chunk build for 1 GiB eligible source: under 10 minutes;
* C# structural indexing for a medium solution: under 5 minutes;
* local embedding throughput: model and hardware specific;
* cancellation acknowledgement: under 250 ms at safe boundaries;
* and active-generation publication: under 1 second after validation.

---

# 176. Incremental Targets

Provisional:

* ordinary saved file lexical update visible: under 1 second p95;
* structural update: under 2 seconds p95;
* embedding update: under 30 seconds p95 when resources allow;
* delete visibility: under 1 second;
* rename path update: under 1 second;
* and watcher-overflow reconciliation start: under 5 seconds.

---

# 177. Scaling Levels

Suggested evidence levels:

* **S1 Small:** 1,000 files, 10,000 chunks
* **S2 Medium:** 10,000 files, 100,000 chunks
* **S3 Large:** 100,000 files, 1,000,000 chunks
* **S4 Extreme:** 500,000 files, several million chunks

Version 1 acceptance should cover S1 through S3 for lexical retrieval.

Semantic acceptance may initially cover up to 250,000 vectors.

---

# 178. Backpressure

Bound:

* file event queue;
* parser queue;
* embedding queue;
* SQLite write queue;
* query queue;
* and maintenance queue.

---

## 178.1 Overflow

On update-queue overflow:

* mark index Stale;
* coalesce to full reconciliation;
* and preserve active generation.

---

## 178.2 Query Queue

Reject or cancel low-priority searches rather than exhausting memory.

---

# 179. Cancellation

All long operations are cancellable:

* inventory;
* hashing;
* parse;
* semantic analysis;
* FTS build;
* embedding;
* vector scan;
* compaction;
* optimise;
* reconciliation;
* and rebuild.

---

## 179.1 Transaction Boundary

Cancellation rolls back an incomplete database transaction.

---

## 179.2 Sealed Segment

A completed sealed segment may remain unreferenced and be cleaned later.

---

# 180. Observability

Index health is available through trusted local diagnostics.

---

## 180.1 Operation Progress

Show:

* phase;
* files;
* bytes;
* chunks;
* symbols;
* vectors;
* elapsed;
* queue;
* and estimated remaining work where grounded.

---

## 180.2 No False Precision

Do not show a precise remaining time without sufficient evidence.

---

# 181. Testing Strategy

ADR-0008 applies.

Project knowledge requires:

* policy tests;
* filesystem tests;
* Git tests;
* content detection tests;
* secret tests;
* parser tests;
* Roslyn tests;
* Tree-sitter tests;
* FTS5 tests;
* symbol and graph tests;
* embedding tests;
* vector-format tests;
* exact-search tests;
* hybrid-ranking tests;
* incremental-update tests;
* generation tests;
* corruption tests;
* recovery tests;
* scale tests;
* performance tests;
* and adversarial repository fixtures.

---

# 182. Index Registration Tests

Test:

* valid project;
* duplicate project ID;
* wrong root;
* changed root;
* Stable and Preview;
* project removed;
* index disabled;
* and policy missing.

---

# 183. Directory Tests

Test:

* correct app-data location;
* repository-root rejection;
* network share;
* cloud-synchronised folder;
* removable disk;
* long path;
* ACL failure;
* reparse point;
* alternate stream;
* and stale active pointer.

---

# 184. Generation Tests

Test:

* initial generation;
* incremental revision;
* staging generation;
* atomic publish;
* active pointer;
* retained old generation;
* cancelled build;
* crash before database commit;
* crash after database commit;
* crash before manifest;
* crash before active pointer;
* and duplicate generation ID.

---

# 185. Manifest Tests

Test:

* valid manifest;
* missing file;
* changed database;
* changed vector segment;
* wrong project;
* wrong policy;
* wrong workspace generation;
* duplicate segment;
* invalid hash;
* and canonical serialisation.

---

# 186. SQLite Tests

Test:

* WAL;
* foreign keys;
* STRICT tables;
* migration;
* query-only reader;
* writer contention;
* locked database;
* disk full;
* I/O error;
* corruption;
* and extension loading disabled.

---

## 186.1 Extension Security

Attempt:

* SQL `load_extension`;
* C API extension enabling through untrusted configuration;
* plugin DLL;
* project DLL;
* and `sqlite-vec` arbitrary path.

Every attempt denies.

---

# 187. FTS5 Schema Tests

Test:

* contentless-delete creation;
* insert;
* update;
* delete;
* row mapping;
* column weights;
* secure-delete;
* integrity-check;
* merge;
* optimise;
* and rebuild through Opure staging.

---

# 188. FTS5 Query Tests

Test:

* simple term;
* phrase;
* prefix;
* NEAR;
* column filter;
* identifier;
* path;
* Unicode;
* diacritics;
* underscore;
* dollar sign;
* punctuation;
* code operators;
* and no result.

---

# 189. FTS Injection Tests

Use:

* unmatched quotes;
* parentheses;
* `OR`;
* `NOT`;
* column syntax;
* `NEAR`;
* prefix flood;
* nested expression;
* SQL syntax;
* comment syntax;
* and null bytes.

Trusted query construction must remain safe and bounded.

---

# 190. FTS Query Limit Tests

Test:

* 64 terms;
* 65 terms;
* long query;
* many prefixes;
* many phrases;
* pathological Unicode;
* cancellation;
* and timeout.

---

# 191. FTS Ranking Tests

Test:

* exact symbol term;
* path term;
* identifier term;
* documentation term;
* body term;
* repeated keyword;
* long file;
* short file;
* and stable tie-breaking.

Retain raw BM25.

---

# 192. FTS Contentless Privacy Tests

Verify:

* ordinary source columns are absent;
* source retrieval uses Workspace;
* deleted terms follow secure-delete behaviour;
* WAL and old generation retention are documented;
* and no full snippet is stored accidentally.

---

# 193. FTS Integrity Tests

Corrupt:

* shadow data;
* row mapping;
* metadata count;
* chunk record;
* and active manifest.

Verify detection and rebuild.

---

# 194. Git Ignore Tests

Use:

* root `.gitignore`;
* nested `.gitignore`;
* negation;
* directory pattern;
* escaped pattern;
* info exclude;
* global exclude;
* tracked ignored-looking file;
* untracked ignored file;
* and non-Git project.

---

## 194.1 Precedence

Verify the current Git precedence and last-match behaviour.

---

## 194.2 Tracked File

Verify tracked files remain eligible unless Opure policy excludes them.

---

# 195. Default Exclusion Tests

Test conventional:

* `bin`;
* `obj`;
* `node_modules`;
* `.venv`;
* `dist`;
* `coverage`;
* `packages`;
* `.git`;
* and `.opure`.

Verify protected roots differ from convenience defaults.

---

# 196. Explicit Override Tests

Test:

* include tracked generated file;
* include ignored documentation;
* include large source;
* exclude tracked file;
* attempt to include Vault;
* attempt to include private key;
* enterprise deny;
* and changed override.

---

# 197. Content Detection Tests

Test:

* UTF-8;
* UTF-16;
* ASCII;
* invalid UTF-8;
* binary zero bytes;
* high entropy;
* executable;
* image;
* archive;
* database;
* source with unusual extension;
* binary with source extension;
* and text with no extension.

---

# 198. Large-File Tests

Test:

* below limit;
* exact limit;
* above limit;
* explicit approval;
* changed hash;
* one huge line;
* many small lines;
* and cancellation.

---

# 199. Minified and Pathological Tests

Test:

* minified JavaScript;
* base64 line;
* source map;
* generated bundle;
* huge JSON;
* compressed-looking text;
* repeated character;
* and legitimate long SQL migration.

---

# 200. Secret Scanner Tests

Seed:

* API key;
* private key;
* OAuth token;
* connection string;
* password;
* cookie;
* signed URL;
* cloud credential;
* Vault canary;
* and package-signing key.

Verify absence from:

* FTS;
* symbol docs;
* embeddings;
* query results;
* logs;
* reports;
* and support bundles.

---

# 201. Secret Rule Update Tests

Index a canary under an old rule, update the scanner, then verify:

* immediate query exclusion;
* FTS purge;
* vector invalidation;
* generation rebuild;
* and security record.

---

# 202. Inventory Tests

Test:

* 1,000 files;
* 100,000 files;
* 500,000 files;
* file added during inventory;
* file deleted during inventory;
* branch checkout;
* inaccessible file;
* cloud placeholder;
* junction;
* symlink;
* mounted folder;
* and watcher overflow.

---

# 203. Workspace Containment Tests

Attempt:

* `..`;
* absolute path;
* device path;
* alternate stream;
* symlink outside root;
* junction outside root;
* hard-link alias;
* and case-confused path.

ADR-0009 controls must hold.

---

# 204. Parser Profile Tests

Test:

* valid profile;
* wrong language;
* changed package hash;
* missing licence;
* unsupported version;
* timeout;
* memory limit;
* crash;
* fallback;
* and quarantined parser.

---

# 205. Analysis Worker Tests

Test:

* no network;
* minimal environment;
* CPU limit;
* memory limit;
* process crash;
* child-process attempt;
* malformed source;
* cancellation;
* and orphan cleanup.

---

# 206. C# Syntax Tests

Test:

* namespace;
* file-scoped namespace;
* class;
* record;
* struct;
* interface;
* enum;
* delegate;
* method;
* property;
* event;
* field;
* local function;
* top-level statement;
* generic;
* partial type;
* attributes;
* XML docs;
* preprocessor;
* invalid source;
* and incomplete edit.

---

# 207. C# Semantic Tests

Test:

* fully qualified symbol;
* overload;
* interface implementation;
* inheritance;
* extension method;
* generic construction;
* property read;
* property write;
* method call;
* object creation;
* project reference;
* unresolved reference;
* missing metadata;
* multi-target project;
* and conditional symbol.

---

# 208. No Project Execution Tests

Seed a project with:

* custom MSBuild target;
* pre-build command;
* source generator;
* analyzer;
* compiler plugin;
* package restore hook;
* project SDK resolver;
* and malicious task assembly.

Indexing must not execute them.

---

# 209. Roslyn Memory Tests

Test:

* large solution;
* many semantic queries;
* semantic-model disposal;
* compilation batch release;
* cancellation;
* and repeated indexing.

Verify bounded memory and no long-lived unintended references.

---

# 210. Tree-sitter Tests

For one pinned grammar, test:

* valid source;
* invalid source;
* incremental edit;
* timeout;
* parser crash;
* large tree;
* query file;
* changed grammar hash;
* and line fallback.

---

# 211. Dynamic Grammar Denial Tests

Attempt:

* npm grammar install;
* project grammar;
* remote grammar download;
* user DLL;
* plugin grammar;
* and runtime generation.

Every attempt denies.

---

# 212. Configuration Parser Tests

Test:

* JSON;
* duplicate critical property;
* YAML safe mapping;
* YAML unsafe tag;
* XML external entity;
* TOML;
* INI;
* project file;
* package manifest;
* and secret field.

---

# 213. Chunk Tests

Use ADR-0021 chunk suites.

Additionally verify:

* stable chunk ID;
* parser version change;
* path rename;
* same content;
* changed content;
* generated state;
* embedding eligibility;
* and line-range accuracy.

---

# 214. Lexical-Term Tests

Test:

* snake case;
* camel case;
* Pascal case;
* acronym;
* digits;
* Unicode identifier;
* namespace;
* dotted path;
* kebab filename;
* operator name;
* and language keyword.

---

# 215. Identifier Expansion Tests

Verify deterministic expansion and no uncontrolled combinatorial explosion.

---

# 216. Symbol Identity Tests

Test:

* same name different namespace;
* overloads;
* partial declarations;
* nested type;
* local symbol;
* generated symbol;
* rename;
* signature change;
* and parser-only symbol.

---

# 217. Reference Tests

Test every reference kind.

Verify confidence and source range.

---

# 218. Graph Tests

Test:

* contains;
* calls;
* implementation;
* inheritance;
* imports;
* depends on;
* tests;
* configures;
* cycle;
* missing node;
* stale edge;
* traversal depth;
* and node cap.

---

# 219. Project Graph Tests

Test:

* project reference;
* package reference;
* target framework;
* missing project;
* changed target;
* cyclic project;
* and multi-target project.

---

# 220. Embedding Profile Tests

Test:

* valid local profile;
* valid remote profile;
* changed model;
* changed dimensions;
* changed input template;
* changed normalisation;
* changed distance;
* changed chunker;
* unsupported data class;
* and stale quality evidence.

---

# 221. Embedding Input Tests

Verify inclusion of:

* path;
* symbol;
* source;
* documentation;
* language;
* and structural labels

according to exact template.

Verify no hidden field.

---

# 222. Embedding Secret Tests

Seed secrets in:

* content;
* path;
* symbol name;
* comments;
* documentation;
* generated metadata;
* and query.

No denied secret reaches local embedding logs or remote provider.

---

# 223. Local Embedding Tests

Test:

* generation;
* batch;
* cancellation;
* GPU pressure;
* CPU profile;
* output dimensions;
* normalisation;
* zero vector;
* NaN;
* infinity;
* model update;
* and receipt.

---

# 224. Remote Embedding Tests

Test:

* Local Only;
* Ask Every Time;
* Approved Providers Only;
* secret denial;
* data classes;
* batching;
* provider outage;
* retention posture;
* cost;
* and no provider vector store.

---

# 225. Vector Segment Format Tests

Test:

* valid header;
* wrong magic;
* unsupported version;
* wrong dimensions;
* wrong element type;
* wrong endianness;
* invalid stride;
* misalignment;
* truncated file;
* extra bytes;
* header hash mismatch;
* data hash mismatch;
* mapping mismatch;
* duplicate chunk;
* and wrong project.

---

# 226. Vector Value Tests

Test:

* normal vector;
* zero vector;
* NaN;
* positive infinity;
* negative infinity;
* denormal;
* wrong norm;
* extreme values;
* and dimension mismatch.

---

# 227. Segment Lifecycle Tests

Test:

* staging write;
* seal;
* verify;
* publish;
* cancel before seal;
* crash after seal;
* unreferenced cleanup;
* active query;
* retire;
* and delete.

---

# 228. Exact Search Correctness Tests

Compare SIMD and scalar for:

* random vectors;
* identical vectors;
* orthogonal vectors;
* negative similarity;
* ties;
* top-k;
* filters;
* and multiple segments.

---

# 229. Exact Search Determinism Tests

Run across:

* repeated calls;
* different thread schedules;
* warm and cold cache;
* SIMD and scalar;
* and process restart.

Tie order must be stable within defined floating-point tolerance.

---

# 230. Exact Search Performance Tests

Measure:

* 10,000 vectors;
* 100,000 vectors;
* 250,000 vectors;
* 1,000,000 vectors;
* dimensions 384;
* 768;
* 1,024;
* and 4,096.

---

# 231. Vector Search Security Tests

Attempt:

* mapping substitution;
* segment path escape;
* mutable segment;
* writable mapping;
* wrong project;
* wrong embedding profile;
* crafted NaN;
* oversized top-k;
* and cancellation denial.

---

# 232. Semantic Coverage Tests

Test:

* 0%;
* partial;
* 100%;
* pending;
* failed chunks;
* changed model;
* changed source;
* secret exclusion;
* and offline remote profile.

---

# 233. Vector Tombstone Tests

Test:

* deleted chunk;
* renamed chunk;
* changed content;
* policy exclusion;
* secret incident;
* active bitmap corruption;
* and compaction.

---

# 234. Vector Compaction Tests

Test:

* normal compaction;
* cancellation;
* crash;
* active query;
* disk full;
* hash failure;
* manifest failure;
* and old segment retention.

---

# 235. `sqlite-vec` Experimental Tests

If evaluated:

* exact result equivalence;
* extension loading path;
* static integration;
* insert;
* delete;
* update;
* metadata filters;
* corruption;
* version migration;
* Windows packaging;
* and ANN recall.

No experiment changes production decision automatically.

---

# 236. Incremental Save Tests

Test:

* one saved file;
* rapid saves;
* unchanged save;
* changed content;
* parse failure;
* secret introduced;
* and query during update.

---

# 237. Rename Tests

Test:

* file rename;
* directory rename;
* case-only rename;
* Git rename;
* content change with rename;
* path-in-embedding profile;
* and overlay rename.

---

# 238. Delete Tests

Test:

* source delete;
* generated delete;
* active query;
* FTS removal;
* graph removal;
* vector invalidation;
* and receipt preservation.

---

# 239. Branch Checkout Tests

Test:

* small checkout;
* massive checkout;
* same files different content;
* rename;
* deleted branch files;
* untracked files;
* query during transition;
* and Context Assembly revalidation.

---

# 240. Project Configuration Tests

Test:

* project reference change;
* target framework change;
* parse option change;
* preprocessor symbol change;
* metadata reference change;
* package version change;
* and source membership change.

---

# 241. Parser Update Tests

Verify affected structural indexes rebuild while unrelated lexical state remains available.

---

# 242. Chunker Update Tests

Verify all dependent lexical and semantic state is rebuilt.

---

# 243. Secret Rule Update Tests

Verify immediate security purge.

---

# 244. Watcher Overflow Tests

Cause overflow and verify:

* stale state;
* full reconciliation;
* no silent current claim;
* and active source revalidation.

---

# 245. Reconciliation Tests

Test:

* missing indexed file;
* unindexed eligible file;
* wrong hash;
* stale FTS row;
* stale symbol;
* stale edge;
* stale vector;
* wrong classification;
* and no difference.

---

# 246. Overlay Tests

Test:

* unsaved file;
* unsaved new file;
* unsaved delete;
* lexical overlay;
* syntax overlay;
* overlay outranks durable;
* save replaces overlay;
* close discards;
* wrong project;
* and no embedding.

---

# 247. Query Plan Tests

Test:

* exact path;
* exact symbol;
* lexical;
* semantic;
* graph;
* hybrid;
* invalid filter;
* wrong project;
* excessive result limit;
* unsupported channel;
* expired plan;
* and model-suggested query.

---

# 248. Exact Path Tests

Test:

* exact case;
* case-insensitive directory;
* case-sensitive directory;
* prefix;
* filename;
* extension;
* duplicate filename;
* and rename.

---

# 249. Exact Symbol Tests

Test:

* full name;
* short name;
* overload;
* generic;
* local;
* interface;
* partial;
* ambiguous;
* and wrong language case.

---

# 250. Lexical Result Tests

Verify current source retrieval, range, highlight and no stored stale snippet.

---

# 251. Structural Query Tests

Test:

* callers;
* callees;
* implementations;
* derived types;
* tests;
* imports;
* dependencies;
* depth;
* node cap;
* and cycle.

---

# 252. Semantic Query Tests

Test:

* local query embedding;
* remote query embedding;
* semantic unavailable;
* partial coverage;
* threshold;
* top-k;
* source filter;
* stale vector;
* and wrong profile.

---

# 253. Query Poisoning Tests

Create files with:

* repeated query keywords;
* misleading symbol names;
* prompt injection;
* generated copies;
* huge documentation repetition;
* and adversarial embeddings.

Verify exact and direct evidence remain prioritised and reasons remain visible.

---

# 254. Hybrid Fusion Tests

Test:

* exact plus lexical;
* lexical plus semantic;
* graph plus semantic;
* direct diagnostic;
* current diff;
* memory;
* generated penalty;
* third-party penalty;
* diversity;
* and stable tie.

---

# 255. Raw Score Preservation Tests

Verify:

* BM25;
* cosine;
* graph distance;
* exact flag;
* memory confidence;
* and final fusion

are all retained.

---

# 256. RRF Tests

Test:

* channel absence;
* unequal channel lengths;
* duplicate result;
* exact boost;
* weight change;
* `k` change;
* and deterministic ranking.

---

# 257. Diversity Tests

Test per:

* file;
* directory;
* source class;
* channel;
* generated state;
* and memory.

---

# 258. Result Provenance Tests

Every result must identify:

* project;
* generation;
* revision;
* source hash;
* range;
* channel;
* scores;
* and reason.

---

# 259. Context Assembly Tests

Test:

* current result;
* stale result;
* changed file;
* deleted file;
* wrong project;
* secret reclassification;
* overlay result;
* and token-budget selection.

Only revalidated source is admitted.

---

# 260. Plugin Query Tests

Attempt:

* allowed exact lookup;
* excessive result count;
* semantic access;
* raw database;
* wrong project;
* source read;
* and revoked capability.

---

# 261. MCP Query Tests

Attempt direct local index access, vector access and hidden source export.

All deny unless a future explicit tool permits it.

---

# 262. Trust Centre Tests

Verify:

* index status;
* policy;
* exclusions;
* parser versions;
* embedding profile;
* remote transmissions;
* integrity;
* rebuild;
* and deletion.

---

# 263. Diagnostics Leakage Tests

Seed source, path and secret canaries.

Verify safe diagnostics.

---

# 264. Storage-Budget Tests

Test:

* under budget;
* warning;
* vector pause;
* staging space shortage;
* old generation;
* cleanup;
* and no silent active-index eviction.

---

# 265. Query Concurrency Tests

Test:

* multiple lexical queries;
* lexical during write;
* vector during compaction;
* query cancellation;
* project close;
* and service shutdown.

---

# 266. Indexing Concurrency Tests

Test:

* one project;
* two projects;
* initial build plus query;
* build plus test;
* memory pressure;
* scheduler pre-emption;
* and queue overflow.

---

# 267. Corruption Tests

Corrupt:

* SQLite page;
* FTS shadow data;
* graph edge;
* manifest;
* active pointer;
* vector header;
* vector data;
* vector mapping;
* and staging journal.

Verify correct channel degradation or full rebuild.

---

# 268. Recovery Tests

Test:

* unclean Runtime shutdown;
* killed Analysis Worker;
* killed Search Worker;
* disk full;
* locked file;
* antivirus interference;
* stale staging;
* incomplete compaction;
* and failed active-pointer replacement.

---

# 269. Migration Tests

Test:

* metadata-only migration;
* FTS schema change;
* parser change;
* chunker change;
* vector format change;
* rollback;
* and unsupported old generation.

---

# 270. Deletion Tests

Test:

* delete one project index;
* active query;
* active build;
* retained receipt;
* vector segments;
* old generations;
* overlay;
* and source untouched.

---

# 271. Offline Tests

Disconnect network and verify:

* lexical search;
* symbols;
* graph;
* local semantic;
* index update;
* rebuild;
* no remote embedding retry;
* and no source-host contact.

---

# 272. Fuzzing

Fuzz:

* Index Policy;
* manifests;
* file metadata;
* FTS query parser;
* identifier expansion;
* source parsers;
* chunk records;
* graph records;
* embedding metadata;
* vector headers;
* vector mappings;
* query plans;
* and query receipts.

---

# 273. Performance Tests

Measure:

* inventory;
* content detection;
* hashing;
* secret scanning;
* parsing;
* Roslyn syntax;
* Roslyn semantics;
* FTS insert;
* FTS query;
* graph query;
* embedding;
* vector scan;
* fusion;
* incremental update;
* reconciliation;
* optimise;
* rebuild;
* and deletion.

---

# 274. Quality Evaluation

Retrieval evaluation should use a source-controlled corpus.

---

## 274.1 Query Types

* exact file;
* exact symbol;
* code concept;
* implementation;
* caller;
* test;
* configuration;
* documentation;
* and bug diagnostic.

---

## 274.2 Required Evidence

For each query, label:

* mandatory result;
* relevant results;
* irrelevant traps;
* generated duplicates;
* stale variants;
* and secret files.

---

## 274.3 Metrics

Measure:

* recall at k;
* precision at k;
* mean reciprocal rank;
* nDCG;
* exact-hit rate;
* stale-result rate;
* duplicate rate;
* source diversity;
* and explanation correctness.

---

## 274.4 No Model Judge Alone

Human- or fixture-labelled relevance remains authoritative.

---

# 275. Semantic Evaluation

Compare exact vector results with:

* lexical;
* graph;
* hybrid;
* and future ANN.

---

## 275.1 Embedding Changes

Every model or template change reruns evaluation.

---

## 275.2 Quantised Vectors

Float16 or int8 requires quality comparison against float32.

---

# 276. ANN Evaluation Gate

A future ANN candidate must demonstrate:

* recall at least the accepted threshold against exact results;
* stable filtering;
* correct deletion;
* bounded rebuild;
* safe persistence;
* and measurable latency improvement.

---

# 277. Accessibility Tests

Index and search UI must support:

* keyboard;
* Narrator;
* high contrast;
* reduced motion;
* focus;
* result reasons;
* channel labels;
* inclusion review;
* progress;
* errors;
* and rebuild actions.

---

# 278. Prototype Plan

## 278.1 Prototype A — Project Index

Index a medium C# repository into one project-scoped SQLite database.

---

## 278.2 Prototype B — FTS5

Implement contentless-delete FTS5 with path, symbol and source columns.

---

## 278.3 Prototype C — Safe C# Semantics

Build Roslyn semantic evidence from trusted Project Service inputs without executing project logic.

---

## 278.4 Prototype D — Tree-sitter

Package one pinned grammar and index syntax with error recovery.

---

## 278.5 Prototype E — Index Policy

Demonstrate tracked, ignored, generated, dependency, binary, large and secret decisions.

---

## 278.6 Prototype F — Incremental Updates

Save, rename, delete and branch-switch while querying.

---

## 278.7 Prototype G — Local Embeddings

Embed eligible chunks with ADR-0020.

---

## 278.8 Prototype H — Vector Segments

Write, verify, map, scan, compact and delete float32 segments.

---

## 278.9 Prototype I — Hybrid Search

Fuse exact, lexical, graph and semantic results with explanations.

---

## 278.10 Prototype J — Stale Result

Change source after query and prove Context Assembly revalidation.

---

## 278.11 Prototype K — Security

Seed secrets, parser attacks, FTS injection and wrong-project paths.

---

## 278.12 Prototype L — Recovery

Corrupt FTS and vector data and rebuild from Workspace.

---

# 279. Implementation Plan

1. Record founder review.
2. Define Index Policy schema.
3. Define Index Manifest schema.
4. Define Query Plan and Query Receipt schemas.
5. Create project-scoped index registry.
6. Create index directory and generation manager.
7. Define SQLite schema and migrations.
8. Build FTS5 contentless-delete prototype.
9. Implement safe FTS query builder.
10. Implement exact path and symbol tables.
11. Integrate Workspace inventory.
12. Integrate Repository tracked and ignored state.
13. Implement content-kind detection.
14. Integrate secret scanning.
15. Implement document and chunk records.
16. Implement Analysis Worker.
17. Implement C# syntax indexing.
18. Implement safe C# semantic compilation snapshot.
19. Implement symbol and reference tables.
20. Implement graph tables and traversal.
21. Package one Tree-sitter grammar.
22. Implement lexical term expansion.
23. Implement transactional incremental updates.
24. Implement reconciliation.
25. Define Embedding Profile schema.
26. Integrate local embeddings.
27. Integrate remote embedding policy.
28. Define vector-segment format.
29. Implement segment writer and verifier.
30. Implement scalar exact search.
31. Implement SIMD exact search.
32. Implement semantic coverage.
33. Implement vector tombstones and compaction.
34. Implement Query Planner.
35. Implement deterministic hybrid fusion.
36. Implement result explanation.
37. Implement unsaved overlay.
38. Integrate Context Assembly revalidation.
39. Implement health and integrity checks.
40. Implement staging rebuild and atomic publication.
41. Implement index deletion.
42. Implement Trust Centre and Desktop views.
43. Add adversarial repositories.
44. Run S1, S2 and S3 scale tests.
45. Complete security, privacy and performance review.
46. Accept, amend or reject the ADR.

---

# 280. Owners

| Area                      | Owner                                    |
| ------------------------- | ---------------------------------------- |
| Product policy            | Founder                                  |
| Project Knowledge Service | Project Knowledge Owner                  |
| Workspace inventory       | Workspace Owner                          |
| Git and repository state  | Repository Owner                         |
| Index policy              | Project Knowledge and Security Owners    |
| Secret scanning           | Security and Secrets Owners              |
| SQLite and FTS5           | Persistence Owner                        |
| C# semantic indexing      | C# Analysis Owner                        |
| Tree-sitter parsers       | Language Analysis Owner                  |
| Embeddings                | AI Router and Local Model Runtime Owners |
| Remote embeddings         | Provider Trust Owner                     |
| Vector search             | Search and Performance Owners            |
| Context integration       | Context Assembly Owner                   |
| Plugin integration        | Plugin Platform Owner                    |
| MCP integration           | MCP Gateway Owner                        |
| Trust Centre              | Trust Centre Owner                       |
| Desktop search UI         | Desktop Owner                            |
| Recovery                  | Recovery Owner                           |
| Adversarial tests         | Test Architecture Owner                  |

---

# 281. Suggested Repository Structure

```text
src/
├── Knowledge/
│   ├── Opure.Knowledge.Contracts/
│   ├── Opure.Knowledge.Service/
│   ├── Opure.Knowledge.Policy/
│   ├── Opure.Knowledge.Inventory/
│   ├── Opure.Knowledge.Content/
│   ├── Opure.Knowledge.Parsing/
│   ├── Opure.Knowledge.CSharp/
│   ├── Opure.Knowledge.TreeSitter/
│   ├── Opure.Knowledge.Lexical/
│   ├── Opure.Knowledge.Symbols/
│   ├── Opure.Knowledge.Graph/
│   ├── Opure.Knowledge.Embeddings/
│   ├── Opure.Knowledge.Vectors/
│   ├── Opure.Knowledge.Retrieval/
│   ├── Opure.Knowledge.Persistence/
│   └── Opure.Knowledge.Security/
├── Worker/
│   ├── Opure.Worker.Analysis/
│   └── Opure.Worker.VectorSearch/
└── Desktop/
    └── Opure.Desktop.Knowledge/

schemas/
└── knowledge/
    ├── index-policy-v1.schema.json
    ├── index-manifest-v1.schema.json
    ├── parser-profile-v1.schema.json
    ├── embedding-profile-v1.schema.json
    ├── vector-segment-v1.schema.json
    ├── knowledge-query-plan-v1.schema.json
    └── knowledge-query-receipt-v1.schema.json

tests/
└── Knowledge/
    ├── Opure.Knowledge.UnitTests/
    ├── Opure.Knowledge.FtsTests/
    ├── Opure.Knowledge.CSharpTests/
    ├── Opure.Knowledge.TreeSitterTests/
    ├── Opure.Knowledge.VectorTests/
    ├── Opure.Knowledge.SecurityTests/
    ├── Opure.Knowledge.IntegrationTests/
    ├── Opure.Knowledge.PerformanceTests/
    └── Fixtures/
        ├── Repositories/
        ├── Languages/
        ├── Malicious/
        └── RetrievalJudgements/
```

Exact project count may be consolidated under ADR-0010.

---

# 282. Index Policy Sketch

```json
{
  "schema": "opure.index-policy/1",
  "id": "default-project",
  "revision": 1,
  "tracked_files": "include-source-and-documentation",
  "untracked_files": "respect-git-ignore",
  "hard_denies": [
    "opure-security-state",
    "credentials",
    "private-keys",
    "outside-project"
  ],
  "defaults": {
    "generated": "exclude",
    "dependencies": "metadata-only",
    "vendor": "exclude",
    "binary": "exclude",
    "maximum_text_bytes": 2097152,
    "maximum_large_text_bytes": 20971520
  },
  "semantic": {
    "enabled": true,
    "maximum_chunks": 250000,
    "local_preferred": true
  }
}
```

---

# 283. Index Manifest Sketch

```json
{
  "schema": "opure.project-knowledge-manifest/1",
  "project": "project-opaque",
  "generation": "generation-opaque",
  "revision": 87,
  "workspace_generation": 412,
  "policy": "default-project:1",
  "database": {
    "schema": 1,
    "integrity": "passed"
  },
  "counts": {
    "documents": 12842,
    "chunks": 161204,
    "symbols": 498312,
    "edges": 1408261
  },
  "semantic": {
    "profile": "embedding-profile-opaque:1",
    "eligible_chunks": 145002,
    "embedded_chunks": 140991,
    "coverage": 0.9723
  },
  "vectors": [
    {
      "segment": "segment-opaque",
      "sha256": "...",
      "entries": 100000
    }
  ],
  "sha256": "..."
}
```

---

# 284. Parser Profile Sketch

```json
{
  "schema": "opure.parser-profile/1",
  "id": "csharp-roslyn-semantic",
  "revision": 1,
  "language": "csharp",
  "kind": "roslyn-semantic",
  "package_versions": {
    "Microsoft.CodeAnalysis.CSharp": "<pinned-version>"
  },
  "features": [
    "syntax",
    "symbols",
    "references",
    "calls",
    "inheritance",
    "implementations"
  ],
  "execution": {
    "worker": "analysis",
    "network": false,
    "source_generators": false,
    "analyzers": false,
    "msbuild_targets": false
  }
}
```

---

# 285. Embedding Profile Sketch

```json
{
  "schema": "opure.embedding-profile/1",
  "id": "local-code-embedding",
  "revision": 1,
  "provider": {
    "kind": "local",
    "model_profile": "model-profile-opaque"
  },
  "dimensions": 768,
  "normalisation": "l2",
  "distance": "cosine",
  "element_type": "float32",
  "input_template": {
    "revision": "code-chunk-v1",
    "fields": [
      "relative-path",
      "language",
      "symbol-signature",
      "source-content"
    ]
  },
  "eligible_data_classes": [
    "project.source",
    "project.documentation"
  ]
}
```

---

# 286. Vector Segment Sketch

```json
{
  "schema": "opure.vector-segment/1",
  "id": "segment-opaque",
  "project": "project-opaque",
  "embedding_profile": "local-code-embedding:1",
  "dimensions": 768,
  "element_type": "float32",
  "normalisation": "l2",
  "distance": "cosine",
  "entries": 100000,
  "data_sha256": "...",
  "mapping_sha256": "...",
  "sealed_at": "2026-07-18T18:00:00Z"
}
```

---

# 287. Query Plan Sketch

```json
{
  "schema": "opure.knowledge-query-plan/1",
  "id": "query-opaque",
  "project": "project-opaque",
  "kind": "hybrid",
  "query": "Where is plugin capability revocation enforced?",
  "channels": {
    "exact_symbol": 50,
    "lexical": 200,
    "graph": 200,
    "semantic": 200
  },
  "filters": {
    "generated": false,
    "third_party": false
  },
  "fusion": "rrf-v1",
  "result_limit": 30,
  "generation": "generation-opaque"
}
```

---

# 288. Result Sketch

```json
{
  "result_id": "result-opaque",
  "project": "project-opaque",
  "generation": "generation-opaque",
  "workspace_generation": 412,
  "path": "src/Plugins/CapabilityLeaseService.cs",
  "range": {
    "start_line": 117,
    "end_line": 189
  },
  "source_sha256": "...",
  "symbol": "CapabilityLeaseService.RevokeAsync",
  "channels": {
    "lexical_bm25": -8.42,
    "semantic_cosine": 0.846,
    "graph_distance": 1
  },
  "fusion_score": 0.073,
  "reason": [
    "Exact symbol component match",
    "Contains all lexical query concepts",
    "Directly referenced by capability revocation workflow"
  ],
  "stale": false
}
```

---

# 289. Query Receipt Sketch

```json
{
  "schema": "opure.knowledge-query-receipt/1",
  "query": "query-opaque",
  "project": "project-opaque",
  "generation": "generation-opaque",
  "revision": 87,
  "channels": {
    "exact_symbol": {
      "candidates": 2,
      "duration_ms": 4
    },
    "lexical": {
      "candidates": 200,
      "duration_ms": 31
    },
    "graph": {
      "candidates": 63,
      "duration_ms": 12
    },
    "semantic": {
      "candidates": 200,
      "duration_ms": 141,
      "coverage": 0.9723
    }
  },
  "fusion": {
    "profile": "rrf-v1",
    "duration_ms": 2
  },
  "results": 30
}
```

---

# 290. Release Gate

Project knowledge support is blocked when:

* index files are written inside a repository;
* one index can serve multiple projects without explicit isolation;
* protected roots can be indexed;
* likely secrets can enter FTS or embeddings;
* a project file can register executable parser code;
* indexing can run source generators, analyzers or custom MSBuild targets;
* arbitrary SQLite extension loading is enabled;
* user search text can become raw SQL or raw FTS syntax;
* source content is duplicated in an uncontrolled index table;
* stale results are presented as current;
* Workspace revalidation is absent;
* remote embeddings can begin on project open;
* embeddings from different models or dimensions are mixed;
* malformed vectors can enter a segment;
* vector mappings are not checksummed;
* semantic search silently falls back to another provider;
* an approximate vector index is enabled without a separate accepted decision;
* generated or dependency content can monopolise results;
* hybrid scoring cannot be explained;
* query results cannot identify source hash and generation;
* FTS corruption is repaired by direct shadow-table manipulation;
* index deletion can affect source;
* or local project work depends on a cloud index.

---

# 291. Acceptance Criteria

This ADR may move to **Accepted** when all of the following hold.

## Architecture and Ownership

* [ ] Project Knowledge Service is authoritative for derived indexes.
* [ ] Workspace remains authoritative for file content.
* [ ] Repository Service remains authoritative for repository state.
* [ ] Memory remains separate.
* [ ] Context Assembly remains authoritative for final context.
* [ ] One logical index exists per project and channel.
* [ ] Index storage is outside the repository.
* [ ] Index storage uses a local fixed disk by default.
* [ ] Desktop has no direct database access.
* [ ] Plugins have no raw index access.
* [ ] MCP servers have no raw index access.
* [ ] Index can be deleted and rebuilt without source loss.

## Index Generations

* [ ] Generation IDs are never reused.
* [ ] Active generation is explicit.
* [ ] Staging is isolated.
* [ ] Publication is atomic.
* [ ] Manifest is canonical and hashed.
* [ ] Database revision is recorded.
* [ ] Workspace generation is recorded.
* [ ] Query results bind a generation and revision.
* [ ] At least one previous generation can support bounded recovery.
* [ ] Unclean shutdown recovery is tested.
* [ ] Schema migration prefers rebuild where appropriate.

## SQLite and FTS5

* [ ] SQLite schema uses explicit migrations.
* [ ] STRICT tables are used where practical.
* [ ] WAL is local only.
* [ ] Foreign keys are enabled.
* [ ] Arbitrary extension loading is disabled.
* [ ] FTS5 is built into the approved SQLite package.
* [ ] Contentless-delete FTS5 tables are used.
* [ ] Source content is not duplicated as ordinary FTS content.
* [ ] FTS rows map to trusted chunks.
* [ ] FTS secure-delete is enabled where supported.
* [ ] FTS integrity-check is implemented.
* [ ] Row mapping is verified.
* [ ] Bounded merge or maintenance exists.
* [ ] Full optimise does not block foreground queries.
* [ ] FTS rebuild uses a staging generation.
* [ ] Shadow tables are never manipulated directly.
* [ ] Query cancellation interrupts SQLite work.

## Query Safety

* [ ] User input cannot submit SQL.
* [ ] User input cannot submit raw MATCH syntax.
* [ ] Phrases are escaped.
* [ ] Prefixes are bounded.
* [ ] NEAR is bounded.
* [ ] Column filters are controlled.
* [ ] Term count is bounded.
* [ ] Query bytes are bounded.
* [ ] Result count is bounded.
* [ ] Timeouts are enforced.
* [ ] Empty queries do not scan broadly.
* [ ] Query receipts exist.

## Inclusion and Exclusion

* [ ] Index Policy is versioned.
* [ ] Precedence is deterministic.
* [ ] Product hard denies cannot be overridden.
* [ ] Enterprise denies cannot be overridden.
* [ ] Project explicit denies work.
* [ ] User includes cannot override security denies.
* [ ] Tracked files are distinguished from ignored untracked files.
* [ ] Git ignore precedence is respected.
* [ ] Global excludes remain local policy evidence.
* [ ] Non-Git projects work.
* [ ] `.git` is denied.
* [ ] Opure security state is denied.
* [ ] Dependencies are excluded or metadata-only by default.
* [ ] Vendored source is separately classified.
* [ ] Generated source is excluded by default.
* [ ] Binary files are excluded.
* [ ] Large files require policy.
* [ ] Minified and pathological text is handled.
* [ ] Every exclusion has a visible reason.

## Filesystem and Inventory

* [ ] ADR-0009 containment applies.
* [ ] Reparse points do not escape.
* [ ] Mounted folders require separate roots.
* [ ] Alternate streams are rejected.
* [ ] Cloud placeholders are not hydrated silently.
* [ ] Network-share index storage is denied.
* [ ] File identity is recorded where supported.
* [ ] Content hash detects changes.
* [ ] Initial inventory is cancellable.
* [ ] Inventory preview is available.
* [ ] File and byte limits are enforced.
* [ ] Full reconciliation exists.
* [ ] Watcher overflow triggers reconciliation.
* [ ] File watchers are not the sole authority.

## Classification and Secrets

* [ ] Eligible files are classified.
* [ ] Secret scan runs before FTS indexing.
* [ ] Secret scan runs before embeddings.
* [ ] Confirmed secrets are excluded.
* [ ] Likely secrets are excluded by default.
* [ ] Protected files retain only safe metadata.
* [ ] Redacted variants have new hashes.
* [ ] Scanner-rule changes invalidate affected index entries.
* [ ] Newly detected secrets are purged promptly.
* [ ] Secret canaries do not appear in FTS.
* [ ] Secret canaries do not appear in vectors.
* [ ] Secret canaries do not appear in diagnostics.
* [ ] Secret canaries do not appear in support bundles.

## Parsing and C# Semantics

* [ ] Parser Profiles are pinned and versioned.
* [ ] Analysis runs in a supervised worker.
* [ ] Analysis worker has no network.
* [ ] Analysis worker has bounded CPU and memory.
* [ ] C# syntax indexing works.
* [ ] Safe C# semantic indexing works.
* [ ] Project Service supplies safe compilation inputs.
* [ ] Indexing does not restore packages.
* [ ] Indexing does not run custom MSBuild targets.
* [ ] Indexing does not run source generators.
* [ ] Indexing does not run analyzers.
* [ ] Indexing does not load project compiler plugins.
* [ ] Metadata references are exact and verified.
* [ ] Semantic model lifetime is bounded.
* [ ] Syntax fallback works.
* [ ] Parser crashes do not corrupt active index.

## Tree-sitter and Other Parsers

* [ ] At least one grammar is source pinned.
* [ ] Generated parser code is packaged first party.
* [ ] Runtime grammar download is absent.
* [ ] Runtime grammar generation is absent.
* [ ] Project grammars cannot load.
* [ ] Tree-sitter parsing is bounded.
* [ ] Error-tree indexing is marked lower confidence.
* [ ] Line-stable fallback works.
* [ ] Safe JSON parser exists.
* [ ] Safe YAML parser exists.
* [ ] Safe XML parser exists.
* [ ] External XML entities are disabled.
* [ ] YAML object construction is disabled.

## Chunks, Symbols and Graph

* [ ] Chunk IDs bind content and parser versions.
* [ ] Chunk ranges are accurate.
* [ ] Chunk content comes from Workspace.
* [ ] Exact path table exists.
* [ ] Exact symbol table exists.
* [ ] Symbol overloads remain distinct.
* [ ] Symbol confidence is recorded.
* [ ] References retain ranges and kinds.
* [ ] Graph edges are typed.
* [ ] Graph confidence is recorded.
* [ ] Graph traversal is bounded.
* [ ] Cycles are safe.
* [ ] Model-generated edges are not authoritative.
* [ ] Project and package relations are represented safely.

## Lexical Search

* [ ] Path terms are indexed.
* [ ] Symbol terms are indexed.
* [ ] Raw identifiers are indexed.
* [ ] Identifier components are generated deterministically.
* [ ] Documentation terms are indexed.
* [ ] Source terms are indexed only after security policy.
* [ ] Column weights are versioned.
* [ ] Raw BM25 remains available.
* [ ] Exact path outranks generic lexical results.
* [ ] Exact symbol outranks generic lexical results.
* [ ] Current Workspace content supplies result snippets.
* [ ] Stale stored snippets are impossible.
* [ ] Unicode and identifier tests pass.

## Embeddings

* [ ] Embedding Profiles are versioned.
* [ ] Model, dimensions and input template are bound.
* [ ] Embedding input is deterministic.
* [ ] Local embeddings work.
* [ ] Remote embeddings require ADR-0019 approval.
* [ ] Project open does not start remote embedding.
* [ ] Secret content is not embedded.
* [ ] Protected content is not embedded.
* [ ] Different vector spaces are never mixed.
* [ ] Model changes invalidate vectors.
* [ ] Dimension changes invalidate vectors.
* [ ] Input-template changes invalidate vectors.
* [ ] Semantic coverage is explicit.
* [ ] Lexical operation works without embeddings.
* [ ] Partial semantic coverage is visible.
* [ ] Provider-side vector stores remain disabled.

## Vector Storage and Exact Search

* [ ] Vector Segment format is versioned.
* [ ] Segment headers are validated.
* [ ] Segment data is checksummed.
* [ ] Mapping is checksummed.
* [ ] Segments are immutable after sealing.
* [ ] Vectors are float32 initially.
* [ ] Dimensions are validated.
* [ ] NaN and infinity are rejected.
* [ ] Norm is validated.
* [ ] Segments are mapped read only.
* [ ] Search worker has no network.
* [ ] Search worker receives no source content.
* [ ] Scalar reference search exists.
* [ ] SIMD search matches scalar within tolerance.
* [ ] Stable tie-breaking exists.
* [ ] Cancellation works.
* [ ] top-k is bounded.
* [ ] Project and profile filters are enforced.
* [ ] Tombstoned vectors do not return.
* [ ] Compaction publishes new immutable segments.
* [ ] Corrupt vector channel can degrade independently.
* [ ] Approximate search is absent from Version 1.

## Incremental Correctness

* [ ] Save updates lexical state.
* [ ] Save updates structural state.
* [ ] Semantic work can lag separately.
* [ ] Rename updates paths.
* [ ] Delete removes results.
* [ ] Branch checkout uses batch reconciliation.
* [ ] Project configuration changes invalidate semantics.
* [ ] Parser changes invalidate structure.
* [ ] Chunker changes invalidate dependent indexes.
* [ ] Embedding changes affect semantic state only.
* [ ] Secret-rule changes trigger security purge.
* [ ] Update queues are bounded.
* [ ] Queue overflow marks stale and reconciles.
* [ ] Incremental database writes are transactional.
* [ ] Active queries see consistent revisions.
* [ ] Stale results are labelled.

## Unsaved Overlay

* [ ] Unsaved buffers use a separate overlay.
* [ ] Overlay is project scoped.
* [ ] Overlay is session scoped.
* [ ] Overlay is memory only.
* [ ] Overlay results outrank old durable content for the same file.
* [ ] Save replaces overlay.
* [ ] Close discards overlay.
* [ ] Overlay embeddings are disabled by default.
* [ ] Overlay cannot leak to another project.

## Retrieval and Fusion

* [ ] Exact path channel works.
* [ ] Exact symbol channel works.
* [ ] Lexical channel works.
* [ ] Graph channel works.
* [ ] Semantic channel works when available.
* [ ] Build and test direct evidence can seed retrieval.
* [ ] Memory remains separate and provenance bound.
* [ ] Candidate union retains channels.
* [ ] Duplicate results merge without losing provenance.
* [ ] Fusion profile is versioned.
* [ ] RRF or selected fusion is deterministic.
* [ ] Exact hits receive deterministic priority.
* [ ] Raw BM25 remains visible.
* [ ] Raw cosine remains visible.
* [ ] Raw graph distance remains visible.
* [ ] Generated and third-party penalties are visible.
* [ ] Result diversity works.
* [ ] No opaque learned reranker is authoritative.
* [ ] Result reasons are correct.
* [ ] Tie-breaking is stable.

## Context and Trust

* [ ] Every result has project identity.
* [ ] Every result has generation and revision.
* [ ] Every result has source hash.
* [ ] Every result has path and range where applicable.
* [ ] Every result has data class.
* [ ] Every result has retrieval reason.
* [ ] A result ID does not authorise source access.
* [ ] Context Assembly revalidates selected results.
* [ ] Changed source invalidates retrieval use.
* [ ] Wrong-project results are impossible.
* [ ] Plugin queries are capability bound.
* [ ] MCP direct index access is denied.
* [ ] Trust Centre exposes index state and remote embedding.
* [ ] Query receipts exist.

## Health and Recovery

* [ ] SQLite integrity checks work.
* [ ] FTS integrity checks work.
* [ ] Graph consistency checks work.
* [ ] Vector checks work.
* [ ] Manifest checks work.
* [ ] Mandatory corruption stops strict query serving.
* [ ] Vector corruption can disable semantic only.
* [ ] Rebuild occurs in staging.
* [ ] New generation publishes atomically.
* [ ] Old generation remains boundedly available for investigation.
* [ ] Index deletion leaves source untouched.
* [ ] Offline operation works.
* [ ] Security review is complete.
* [ ] Privacy review is complete.
* [ ] Performance review is complete.
* [ ] Founder approval is recorded.

---

# 292. Evidence Required Before Acceptance

* [ ] Project Knowledge Service contract.
* [ ] Index Policy schema.
* [ ] Index Manifest schema.
* [ ] Query Plan schema.
* [ ] Query Receipt schema.
* [ ] project-scoped directory report.
* [ ] active-generation publication report.
* [ ] unclean-shutdown report.
* [ ] SQLite schema and migration report.
* [ ] runtime-extension denial report.
* [ ] FTS5 contentless-delete report.
* [ ] FTS5 secure-delete report.
* [ ] FTS5 integrity and rebuild report.
* [ ] safe query-builder report.
* [ ] Git ignore precedence report.
* [ ] tracked-file report.
* [ ] protected-root report.
* [ ] binary and content-kind report.
* [ ] generated and dependency report.
* [ ] large-file and minified-text report.
* [ ] secret-canary report.
* [ ] secret-rule purge report.
* [ ] Analysis Worker sandbox report.
* [ ] no-project-code-execution report.
* [ ] C# syntax report.
* [ ] C# semantic report.
* [ ] Roslyn memory report.
* [ ] Tree-sitter pinned-grammar report.
* [ ] dynamic-grammar denial report.
* [ ] safe configuration-parser report.
* [ ] chunk identity report.
* [ ] exact path report.
* [ ] exact symbol report.
* [ ] graph report.
* [ ] local embedding report.
* [ ] remote embedding approval report.
* [ ] vector-segment specification.
* [ ] vector-segment fuzz report.
* [ ] scalar exact-search report.
* [ ] SIMD equivalence report.
* [ ] exact vector performance report.
* [ ] semantic-coverage report.
* [ ] vector compaction report.
* [ ] incremental save report.
* [ ] rename and delete report.
* [ ] branch-checkout report.
* [ ] watcher-overflow report.
* [ ] reconciliation report.
* [ ] unsaved-overlay report.
* [ ] hybrid-fusion report.
* [ ] ranking explainability report.
* [ ] wrong-project adversarial report.
* [ ] Context Assembly revalidation report.
* [ ] corruption and rebuild rehearsal.
* [ ] storage-budget report.
* [ ] S1 scale report.
* [ ] S2 scale report.
* [ ] S3 scale report.
* [ ] accessibility report.
* [ ] security review.
* [ ] privacy review.
* [ ] performance review.
* [ ] founder approval.

---

# 293. Known Limitations

* The exact SQLite version and build are not selected.
* The exact FTS5 tokenizer settings are not final.
* Code identifier tokenisation will not understand every language convention.
* FTS5 BM25 is lexical and not semantic.
* Contentless FTS still stores recoverable term structures subject to deletion and storage limitations.
* FTS secure-delete does not guarantee forensic storage erasure.
* The final C# compilation-snapshot implementation is not selected.
* Safe semantic indexing may be incomplete when references are missing.
* Source generators are not run.
* Analyzer-derived symbols are unavailable.
* Generated source is excluded by default.
* The initial Tree-sitter language is not selected.
* Tree-sitter grammar quality varies.
* Parse-only indexes cannot provide full semantic resolution.
* The final local embedding model is not selected.
* The final embedding dimensions are not selected.
* Exact vector search is linear.
* The initial semantic chunk cap is bounded.
* Very large monorepos may require ANN later.
* Float32 vector storage is larger than quantised alternatives.
* SIMD floating-point results may differ slightly by platform.
* Vector search quality depends on embedding model and input template.
* Semantic poisoning cannot be eliminated.
* Secret scanning has false positives and false negatives.
* An embedding can retain sensitive semantic information.
* Remote embeddings can have provider retention.
* Unsaved buffers are not embedded by default.
* Cross-project retrieval is unavailable.
* Historical repository search is unavailable.
* Learned reranking is unavailable.
* Approximate nearest-neighbour search is unavailable.
* Cloud-hosted indexes are unavailable.
* Language-server indexes are not consumed initially.
* Search quality may vary by language.
* Full reconciliation can be expensive.
* Same-user malware can read user-owned index files.
* Secure deletion is not guaranteed.
* The index can be temporarily stale during large repository changes.
* Context Assembly revalidation adds latency.
* Storage estimates are approximate.

---

# 294. Open Questions

* Which exact SQLite release should Opure pin?
* Should Opure use the SQLitePCLRaw bundle supplied with `Microsoft.Data.Sqlite` or a custom approved native build?
* How is FTS5 availability verified at startup?
* Should the SQLite build enable any custom compile options?
* Should FTS5 `secure-delete` be enabled for every project?
* What compatibility implications does FTS5 secure-delete create for old SQLite readers?
* Should the index database use database-level secure deletion?
* Which FTS5 automerge and crisismerge values are appropriate?
* How often should FTS optimise run?
* Should each language use a separate FTS table?
* Should documentation and source use separate indexes?
* Which `unicode61` token characters are appropriate?
* Should `_`, `$`, `#`, `@` or `.` be token characters?
* How should C++ operators and template syntax be searched?
* How should Rust lifetimes be tokenised?
* How should dotted namespaces and paths be expanded?
* Should a trigram FTS index be added for path substrings?
* What storage cost would trigram indexing add?
* Should path fuzzy search use a dedicated algorithm instead?
* How should raw BM25 be normalised?
* What initial column weights are best?
* Which lexical evaluation corpus should tune them?
* Should exact phrase and prefix query modes be user visible?
* Should expert FTS syntax ever be supported?
* Which query parser library is selected?
* How are query Unicode controls displayed?
* How is source highlight calculated for contentless FTS?
* Can FTS token offsets be retained safely without source duplication?
* Which index metadata belongs in SQLite versus the manifest?
* Should incremental updates mutate one active database or publish immutable database generations more frequently?
* How many old generations should be retained?
* How long should query readers pin a generation?
* How should Windows file replacement handle active SQLite readers?
* Should the active pointer live in SQLite rather than JSON?
* Which crash journal is required?
* How is generation storage reclaimed?
* How is antivirus interference handled?
* Should index directories be compressed?
* Should NTFS compression be prohibited for vector segments?
* Should index storage support another local drive?
* What minimum free-space reserve is required?
* Which source files are included by default?
* Should all tracked text files be indexed?
* Should tracked vendored source remain excluded?
* How are submodules handled?
* Should nested repositories become separate projects?
* How are Git worktrees handled?
* How are sparse checkouts handled?
* How are Git LFS pointer files handled?
* Should LFS content be hydrated?
* How are repository info excludes treated in shared policy?
* Should global Git excludes influence local indexing?
* How are non-Git generated directories detected?
* Which generated-code headers are recognised?
* Should generated declarations be indexed while bodies are excluded?
* Which dependency metadata should be searchable?
* Should package README files be indexed?
* How are SDK reference docs integrated?
* Which binary detector is selected?
* Which encodings are supported?
* Should UTF-16 source be converted for FTS?
* How are invalid bytes reported?
* What file-size limits are appropriate?
* Which large files merit specialised indexers?
* How are lockfiles indexed?
* How are minified files detected without false positives?
* Should source maps be excluded?
* How are notebooks handled?
* How are templating languages handled?
* How are mixed-language files handled?
* Which C# Roslyn package version is selected?
* How are safe compilation inputs produced?
* Can Project Service evaluate projects without executing arbitrary targets?
* Should a restricted MSBuild evaluation worker be designed?
* How are custom SDKs represented?
* How are multi-target projects indexed?
* How are conditional compilation variants represented?
* Should the index pick one target framework or several?
* How are unresolved references displayed?
* How are metadata references verified?
* How are framework reference packs located?
* How are NuGet package references resolved without restore?
* Should local source generators ever be indexed from materialised output?
* How are analyzer-generated diagnostics represented?
* Which C# symbol kinds are global?
* How are partial types merged?
* How are explicit interface implementations represented?
* How are extension methods represented?
* How are records and primary constructors represented?
* How are local functions and lambdas indexed?
* Should local variables be searchable globally?
* How is `SymbolKey` persistence handled across Roslyn versions?
* Which implementation-neutral symbol identity format is used?
* How are method overloads ranked?
* How are references stored compactly?
* What graph-edge confidence values are used?
* Which graph traversals belong in Version 1?
* How are tests linked to source?
* How are configuration keys linked to code?
* Should call graphs include virtual dispatch candidates?
* How are reflection-based calls represented?
* How are dependency-injection registrations linked?
* Should model-generated relation candidates ever be stored?
* Which Tree-sitter language should be first?
* Which grammar repositories are acceptable?
* How are grammar licences reviewed?
* How are parser query files tested?
* Should grammars be statically linked or separate signed DLLs?
* Can Tree-sitter C# binding be used safely in the Analysis Worker?
* How are incremental parse trees cached?
* How are parse-tree caches invalidated?
* What parser timeout is appropriate?
* What syntax-node limit is appropriate?
* How are parser crashes quarantined?
* Which structured configuration parsers are selected?
* Should YAML anchors and aliases be expanded?
* How is YAML alias explosion prevented?
* How are XML namespaces indexed?
* Should project XML conditions be indexed lexically?
* How are JSON duplicate keys handled for search?
* Should SQL receive a dedicated parser?
* Which chunker profiles are reused from ADR-0021?
* Should chunk content be cached by Knowledge Service?
* Which service owns rendered chunk caches?
* How are chunk IDs preserved across renames?
* Should content-identical chunks deduplicate physically?
* How is source provenance retained after deduplication?
* Which local embedding model is selected?
* Should one embedding model cover code and documentation?
* Should separate code and text embedding spaces exist?
* What embedding-input template performs best?
* Should path and symbol labels be embedded?
* How much path information risks repository-name overfitting?
* Which data classes are eligible for embeddings?
* Should generated declarations be embedded?
* Should third-party source be embedded?
* Should tests and production code share one embedding space?
* Which dimension is optimal?
* Should embeddings be normalised by model or Opure?
* What norm tolerance is allowed?
* How are zero vectors handled?
* How are embedding failures retried?
* Should remote embeddings ever be the project default?
* How are remote embedding batches approved?
* How are query embeddings retained?
* Should query embeddings be stored?
* How are personal or secret queries protected?
* Should vectors use float32 permanently?
* When should float16 be evaluated?
* When should int8 be evaluated?
* Which vector-segment endianness and alignment are selected?
* Should segment mappings be embedded in the same file?
* Should vector segments be memory mapped?
* How does Windows memory mapping behave during deletion?
* How are active segment leases tracked?
* What segment size is optimal?
* How many segments can be scanned efficiently?
* How often should compaction run?
* What tombstone percentage triggers compaction?
* Should semantic search prefilter by language or path?
* Should lexical candidates be used to narrow exact vector scan?
* Would narrowing harm semantic recall?
* Which SIMD API should be used?
* Should AVX2 and AVX-512 paths be explicit?
* Is `System.Numerics.Vector<T>` sufficient?
* Should a source-pinned native exact scanner be introduced?
* How are scalar and SIMD tolerances selected?
* How are floating-point tie thresholds selected?
* How is query cancellation checked efficiently?
* How many vector scans may run concurrently?
* How is mapped-vector memory accounted?
* What exact vector p95 target triggers ANN reconsideration?
* Should `sqlite-vec` exact mode be evaluated before custom segments ship?
* Can `sqlite-vec` be statically linked into an isolated worker?
* How are `sqlite-vec` breaking changes migrated?
* Which release is sufficiently stable?
* How is `sqlite-vec` fuzzed on Windows?
* Which ANN algorithm should be evaluated first?
* What minimum recall is acceptable?
* How are filters applied in an ANN index?
* How are deletes and updates reconciled?
* Should exact search remain the fallback after ANN?
* Which hybrid fusion method is final?
* What RRF `k` is used?
* Which channel weights are task specific?
* How are exact boosts represented?
* Should semantic scores have thresholds?
* How is lexical keyword stuffing penalised?
* How is semantic poisoning detected?
* How are generated duplicates reduced?
* How is source diversity calculated?
* Should a local reranker be introduced later?
* Which reranker model and features are acceptable?
* How is reranker output explained?
* How are build and test diagnostic channels fused?
* How is Memory queried without creating index copies?
* Should repository documentation receive a dedicated channel?
* How are current-diff results boosted?
* How are result explanations tested?
* Should users see raw BM25 and cosine?
* How many result channels should the UI show?
* How are unsaved overlays indexed?
* Should overlays include semantic embeddings for very small selections?
* How are overlays shared across Desktop windows?
* How are editor crashes handled?
* How does an overlay interact with repository checkout?
* How frequently should full reconciliation run?
* Can NTFS USN Journal improve reconciliation?
* How is USN loss or wrap handled?
* What watcher debounce works best?
* How are massive generated-output bursts coalesced?
* How quickly must secret-rule purge complete?
* Should queries stop during a security purge?
* How are old FTS terms removed from WAL and generations?
* How are remote embedding deletions represented?
* How are index receipts retained after project removal?
* Should project indexes be exportable?
* Should an export contain vectors?
* How are licences and third-party source handled in export?
* Should indexes be included in support bundles?
* Which safe metadata may be exported?
* How is index health communicated without jargon?
* Which permanent evidence is required for an indexing security investigation?

---

# 295. Deferred Decisions

This ADR intentionally defers:

* exact SQLite version;
* exact FTS5 tokenizer options;
* custom FTS tokenizers;
* trigram path index;
* final language-parser list;
* unrestricted MSBuildWorkspace;
* source-generator execution;
* analyzer execution;
* language-server integration;
* final embedding model;
* final remote embedding provider;
* float16 vectors;
* int8 vectors;
* `sqlite-vec` production adoption;
* HNSW;
* IVF;
* DiskANN;
* learned reranking;
* cross-project retrieval;
* historical repository indexing;
* cloud-hosted indexes;
* multimodal project indexing;
* model-generated repository summaries;
* autonomous context expansion;
* and public project-index export.

---

# 296. Alternatives Rejected

A global project index is rejected because project isolation is a core authority boundary.

Cloud search is rejected because local project knowledge should not require remote storage.

Embeddings-only retrieval is rejected because exact names, paths and code structure are indispensable.

Pure grep is retained as a fallback but rejected as the complete architecture.

Language-server ownership is rejected because language servers are optional, inconsistent and may execute project-specific logic.

External vector databases are deferred because they add service and persistence complexity.

`sqlite-vec` production use is deferred because it remains pre-v1 and its new ANN features are alpha or experimental.

Arbitrary SQLite extension loading is rejected because project or plugin DLL paths must never become database code authority.

Approximate nearest-neighbour search is deferred because exact search provides the correctness baseline and simpler Version 1 lifecycle.

Opaque learned reranking is rejected because retrieval reasons must remain inspectable.

Storing a second ordinary copy of source in FTS is rejected because Workspace remains authoritative and the index should minimise duplicated content.

---

# 297. Official and Primary Evidence References

## SQLite

* [SQLite FTS5 Extension](https://www.sqlite.org/fts5.html)
* [SQLite run-time loadable extensions](https://www.sqlite.org/loadext.html)
* [SQLite documentation](https://www.sqlite.org/docs.html)

## Git

* [Git `gitignore` documentation](https://git-scm.com/docs/gitignore)

## Roslyn

* [Work with the .NET Compiler Platform SDK workspace model](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/work-with-workspace)
* [Roslyn SemanticModel API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.semanticmodel)

## Tree-sitter

* [Tree-sitter introduction](https://tree-sitter.github.io/tree-sitter/)
* [Using parsers](https://tree-sitter.github.io/tree-sitter/using-parsers/)
* [Advanced parsing and incremental edits](https://tree-sitter.github.io/tree-sitter/using-parsers/3-advanced-parsing.html)
* [Tree-sitter query documentation](https://tree-sitter.github.io/tree-sitter/cli/query.html)

## Managed SIMD

* [.NET `Vector<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.numerics.vector-1)
* [.NET hardware acceleration indicator](https://learn.microsoft.com/en-us/dotnet/api/system.numerics.vector.ishardwareaccelerated)

## Deferred Vector Extension Evidence

* [`sqlite-vec` repository](https://github.com/asg017/sqlite-vec)
* [`sqlite-vec` releases](https://github.com/asg017/sqlite-vec/releases)

SQLite features, Git behaviour, Roslyn packages, Tree-sitter grammars and vector libraries can change.

The implementation must revalidate every selected version, build option and parser artefact before acceptance.

---

# 298. Review Record

| Date         | Reviewer           | Decision | Notes                                                             |
| ------------ | ------------------ | -------- | ----------------------------------------------------------------- |
| 18 July 2026 | Architecture draft | Proposed | Project-scoped FTS5, graph and exact-vector retrieval recommended |

---

# 299. Approval

## Founder or Product Approval

* **Name:** Christopher Dyer
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Inclusion, local indexing, semantic coverage and ANN deferral review required

## Project Knowledge Approval

* **Name or role:** Project Knowledge and Retrieval Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Index generation, FTS, graph, vectors and retrieval evidence required

## Workspace and Repository Approval

* **Name or role:** Workspace and Repository Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Inventory, snapshots, Git state and revalidation required

## Security and Privacy Approval

* **Name or role:** Security and Privacy Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Protected roots, secrets, parser isolation, embedding and deletion evidence required

## Persistence Approval

* **Name or role:** Persistence Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** SQLite, FTS5, generation, integrity and recovery evidence required

## AI and Context Approval

* **Name or role:** AI Router, Local Model Runtime and Context Assembly Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Embedding profiles, semantic coverage and source revalidation required

## Performance Approval

* **Name or role:** Performance Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** S1–S3 indexing, FTS and exact-vector evidence required

## Test Approval

* **Name or role:** Test Architecture Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Adversarial repositories, corruption and quality evaluation required

---

# 300. Supersession

This ADR is superseded only when a later ADR:

* names ADR-0022 explicitly;
* explains why index storage, lexical search, parser strategy, vectors or retrieval changed;
* identifies project-index and workflow migration;
* describes privacy, embedding, corruption and resource impact;
* provides exact-search comparisons for any ANN adoption;
* and updates the `Superseded by` field.

Historical manifests, parser profiles, embedding profiles and query receipts remain available according to retention policy.

---

# 301. Change History

| Version | Date         | Author        | Summary                                                                                   |
| ------- | ------------ | ------------- | ----------------------------------------------------------------------------------------- |
| 0.1     | 18 July 2026 | Founder Draft | Initial project-scoped FTS5, Roslyn-first graph and exact-vector retrieval recommendation |

---

# 302. Final Decision Statement

> **Opure will provisionally maintain one derived and rebuildable knowledge index per project and channel outside the repository, using service-owned SQLite STRICT metadata tables, FTS5 contentless-delete lexical indexes, exact path and symbol lookup, Roslyn-first structural and semantic evidence, pinned parse-only grammars for additional languages, and immutable checksummed float32 embedding segments searched exactly through a bounded hardware-accelerated worker, while every file is admitted through explicit tracked, ignored, generated, dependency, size, classification and secret policy, remote embeddings require a separate data-sharing approval, approximate nearest-neighbour engines and arbitrary SQLite extensions remain disabled, and every fused search result retains project, generation, source hash, channel score and explanation and must be revalidated by Workspace before Context Assembly can use it, because an index may accelerate developer understanding but must never become hidden source authority, a cross-project memory pool or an unreviewed cloud copy of the repository.**

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**