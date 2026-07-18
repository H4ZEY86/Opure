# ADR-0005 — Persistence Architecture

## Architecture Decision Record

**Status:** Proposed  
**Date:** 18 July 2026  
**Decision owners:** Founder and Product Owner  
**Technical owners:** Persistence Architecture Owner  
**Reviewers:** Runtime Architecture Owner, Security Owner, Recovery Owner, Knowledge Owner, Trust Centre Owner, Performance Owner  
**Supersedes:** None  
**Superseded by:** None  
**Related ADRs:** ADR-0001 Primary Implementation Language, ADR-0003 Runtime Process Topology, ADR-0004 Local IPC  
**Related specifications:** CHARTER-001, SPEC-001, SPEC-002, SPEC-003, SPEC-004, SPEC-005, SPEC-006, SPEC-007, SPEC-008, SPEC-009, SPEC-010, SPEC-011, SPEC-012  
**Target milestone:** Phase 0 — Founding Baseline through Phase 5 — Context and Project Memory  

---

## 1. Decision Summary

Opure should use **SQLite through `Microsoft.Data.Sqlite` as its default embedded relational persistence engine** for local authoritative service state.

The persistence architecture should use:

- one database per service-owned bounded persistence domain;
- project-scoped databases where independent isolation, deletion, backup and rebuild are beneficial;
- no direct cross-service database access;
- no cross-service database transactions;
- explicit SQL schemas and reviewed forward migrations;
- `STRICT` tables where practical;
- foreign-key enforcement on every connection;
- Write-Ahead Logging on supported local filesystems;
- durability classes selected by data criticality;
- short transactions;
- one coordinated writer path per database;
- bounded lock waits and deliberate retries;
- transactional outboxes for reliable service events;
- integrity and foreign-key checks;
- consistent online backup APIs;
- pre-migration recovery points;
- and a separate content-addressed file store for large immutable payloads.

Authoritative SQLite databases should be stored in an Opure-managed local data root, not:

- inside the developer's source repository;
- on a network filesystem;
- in a project workspace;
- or in a cloud-synchronised directory by default.

Secret values remain outside SQLite in the dedicated Secrets Vault.

Full-text search may use SQLite FTS5 as a rebuildable index.

Vector-search implementation remains a separate decision. Vector metadata may be stored in SQLite, but this ADR does not approve arbitrary dynamically loaded SQLite vector extensions inside the trusted Runtime.

---

## 2. Status

Current status:

> **Proposed**

This ADR becomes **Accepted** only after prototypes demonstrate:

- service-owned database boundaries;
- WAL configuration;
- writer coordination;
- transaction retry behaviour;
- crash recovery;
- migration recovery;
- online backup and restore;
- corruption detection;
- project deletion;
- content-addressed blob storage;
- transactional outbox delivery;
- full-text indexing;
- and acceptable performance on the reference machine.

---

## 3. Context

Opure requires persistent local state for:

- Runtime configuration;
- registered projects;
- project settings;
- project cloud policy;
- workspace metadata;
- patch records;
- patch journals;
- workflow definitions;
- workflow instances;
- workflow checkpoints;
- approvals;
- build and test records;
- repository-operation state;
- provider and model metadata;
- project knowledge;
- symbols and relationships;
- conversations;
- plugin installation and permissions;
- MCP registrations;
- Trust Centre records;
- diagnostics;
- artefact metadata;
- and recovery state.

The platform is local by design.

It should not require:

- PostgreSQL;
- a database server;
- a container runtime;
- a cloud account;
- or a remote control plane

for ordinary local operation.

The persistence architecture must preserve explicit service ownership.

A convenient shared database must not become a hidden coupling mechanism.

Opure also stores data with different durability and rebuildability requirements.

For example:

- an approval is authoritative;
- a patch transaction journal is recovery critical;
- a full-text index is rebuildable;
- a UI projection cache is disposable;
- and a secret value must not enter ordinary persistence at all.

One storage policy is therefore insufficient.

---

## 4. Problem Statement

Opure requires a local persistence architecture that supports reliable authoritative state, project isolation, service ownership, recovery, migrations, search, backups and large engineering payloads without introducing a database server, hidden cloud dependency or tightly coupled shared schema.

---

## 5. Decision Drivers

The persistence decision is evaluated against:

- alignment with the Opure Charter;
- local-first operation;
- offline operation;
- Windows 11 support;
- future Linux and macOS support;
- C# and .NET integration;
- zero-server deployment;
- ACID transactions;
- crash recovery;
- migration safety;
- service ownership;
- project isolation;
- backup and restore;
- corruption detection;
- full-text search;
- vector-search extensibility;
- large-payload handling;
- audit history;
- data deletion;
- privacy;
- secrets separation;
- testability;
- fault injection;
- observability;
- performance;
- memory use;
- dependency maturity;
- package security;
- small-team delivery;
- future replacement;
- and exportability.

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
- **Proven Engineering**
- **Performance Respect**
- **No Hidden Authority**
- **No Secret Values in Ordinary Storage**
- **Honest Recovery**
- **Evidence-Based Confidence**

Specific architecture requirements include:

- Every authoritative resource has one owning service.
- One service must not mutate another service's private data.
- Cross-service coordination occurs through contracts.
- Project memory is isolated by project.
- Durable knowledge retains provenance.
- Secret values remain in the Secrets Vault.
- Trust Centre records remain readable and useful.
- Patch application requires durable journals and recovery.
- Workflow checkpoints must survive Runtime restart.
- Storage migrations must not silently discard developer data.
- Rebuildable indexes must not be confused with authoritative state.
- Recovery must prefer integrity over convenience.

---

## 7. Scope

This ADR decides:

- the default embedded relational engine;
- the .NET data provider;
- database ownership boundaries;
- database file placement;
- project-scoped persistence;
- transaction strategy;
- write concurrency;
- journalling mode;
- durability classes;
- schema rules;
- migration rules;
- backup and restore;
- integrity checks;
- transactional outbox behaviour;
- full-text search;
- large-payload storage;
- retention and deletion;
- and persistence observability.

This ADR does not decide:

- the encrypted Secrets Vault implementation;
- the vector-search engine;
- the embedding model;
- database-wide encryption;
- cloud backup;
- team collaboration storage;
- remote project storage;
- telemetry storage;
- the final audit hash-chain algorithm;
- the final updater;
- or distributed transactions.

---

## 8. Constraints

Known constraints include:

- Windows 11 is the first supported platform.
- C#/.NET is the proposed implementation stack.
- The first implementation must remain practical for a small team.
- The platform must work without a database server.
- The Runtime owns authoritative state.
- The desktop must not access databases directly.
- Plugin Hosts must not access service database files.
- Workers must not own authoritative service stores.
- Project source repositories must remain separate from Opure state.
- Database operations must not block the desktop UI thread.
- Database operations must not block IPC transport threads for long periods.
- Service state must survive process crashes.
- Migration failure must lead to recovery, not silent data loss.
- Secret values must not be stored in normal databases.
- Large source files and artefacts should not be duplicated unnecessarily.
- Network filesystems may not provide reliable SQLite locking.
- Cross-platform future support should not require replacing all persistent data.
- Multiple Runtime profiles must use isolated state roots.
- Test runs must not touch production user state.

---

## 9. Assumptions

This decision assumes:

- Most Opure workloads are local and single-user.
- There are many concurrent readers but relatively few concurrent writers per bounded domain.
- One writer at a time per database is acceptable when transactions remain short.
- Service boundaries will reduce write contention.
- AI inference, builds and indexing dominate resource use more than ordinary metadata transactions.
- Large immutable content can live outside relational tables.
- Full-text indexes can be rebuilt.
- Vector indexes can be rebuilt from authoritative metadata and source evidence.
- Database files can be kept in a local application-data directory.
- Project-specific databases can be deleted independently.
- The Runtime can coordinate database maintenance.
- Application-level recovery is required even when SQLite guarantees database transaction integrity.
- A pinned supported SQLite build will be distributed consistently.
- Database files are private implementation stores, not public user-editable document formats.

---

## 10. Official Platform Evidence

Official SQLite and Microsoft documentation available on 18 July 2026 establishes that:

- SQLite provides transactional commit and rollback.
- Write-Ahead Logging supports concurrent readers and a writer, with checkpointing.
- Microsoft.Data.Sqlite is a lightweight ADO.NET provider for SQLite.
- Microsoft.Data.Sqlite supports transactions, savepoints and online backup.
- SQLite foreign-key enforcement must be deliberately enabled on connections.
- SQLite provides `quick_check`, `integrity_check` and `foreign_key_check`.
- SQLite supports `STRICT` tables.
- SQLite FTS5 provides full-text search.
- SQLite databases in WAL mode have associated `-wal` and `-shm` state that must be treated as part of the live database.
- Raw copying a live database file can produce an invalid backup; SQLite's backup API or `VACUUM INTO` provides consistent alternatives.
- SQLite depends on correct filesystem locking and should not be treated as safe on unreliable network filesystems.
- Microsoft.Data.Sqlite asynchronous ADO.NET methods do not provide true asynchronous SQLite IO.

These platform details must be rechecked against the pinned SQLite and .NET versions before this ADR moves to Accepted.

---

## 11. Options Considered

The principal storage options are:

1. **Option A — SQLite with Service-Owned Databases**
2. **Option B — One Shared SQLite Database**
3. **Option C — Embedded Document Database**
4. **Option D — PostgreSQL or Another Local Database Server**
5. **Option E — File-Based JSON and Binary Records**
6. **Option F — Key-Value or LSM Store**
7. **Option G — Multiple Storage Engines from the Beginning**
8. **Option H — Cloud-Hosted Persistence**

---

# 12. Option A — SQLite with Service-Owned Databases

## 12.1 Description

Use SQLite as the default relational engine.

Use separate database files for bounded service-owned domains and project-scoped state where appropriate.

Use explicit contracts for cross-service coordination.

---

## 12.2 Advantages

- No server process.
- Local and offline.
- Mature transactional engine.
- Portable database format.
- Strong .NET integration.
- Suitable for structured metadata.
- Supports indexes and constraints.
- Supports full-text search.
- Supports online backup.
- Supports integrity checks.
- Supports WAL concurrency.
- Supports savepoints.
- Simple packaging.
- Low idle resource use.
- Easy per-project isolation.
- Easy project deletion.
- Easy service-specific migration.
- Good test isolation.
- No hidden cloud dependency.
- Allows service ownership to be reflected physically.
- Supports local diagnostic tooling.
- Provides a clear route for export and recovery.
- Can be replaced behind service contracts later.
- Avoids distributed-system complexity.

---

## 12.3 Disadvantages

- Only one writer can modify a database at a time.
- Write transactions must remain short.
- Database files require filesystem locking.
- WAL creates auxiliary files.
- SQLite has flexible typing unless stricter schema practices are used.
- Full database encryption is not included by default.
- Large BLOBs can inflate database files and backups.
- Cross-database transactions are unsuitable for service coordination.
- Schema migration requires discipline.
- Microsoft.Data.Sqlite does not provide true asynchronous SQLite IO.
- High-volume vector search may require another engine.
- Advanced server-database features are unavailable.
- Long-running analytical queries can affect maintenance.
- Operational code must handle `SQLITE_BUSY` deliberately.
- Multiple databases require a backup-set coordinator.

---

## 12.4 Risks

- Developers may bypass service contracts and open another service's database.
- One giant shared database may emerge despite the decision.
- Long transactions may block writes.
- Database files may be placed in a synced or network folder.
- Raw file copying may omit WAL state.
- Migrations may become destructive.
- Rebuildable indexes may be treated as authoritative.
- Large content may be duplicated inside BLOB columns.
- An ORM may hide inefficient query patterns.
- SQLite extensions may expand the trusted computing base.
- `synchronous=OFF` may be enabled for benchmarks and accidentally ship.
- Unbounded event tables may grow indefinitely.
- Per-project database count may become excessive without lifecycle rules.
- Backup sets may represent different moments unless coordinated.
- Same database may accidentally be opened through multiple paths.

---

## 12.5 Mitigations

- Architecture tests.
- File access controls.
- service-specific database access assemblies;
- local managed storage root;
- canonical database paths;
- one writer coordinator per database;
- transaction-duration metrics;
- migration checksum;
- pre-migration backup;
- integrity checks;
- online backup API;
- content-addressed file store;
- rebuild markers;
- retention jobs;
- database manifests;
- connection configuration validation;
- and explicit durability classes.

---

## 12.6 Estimated Adoption Cost

- **Initial implementation:** Moderate
- **Operational complexity:** Low to Moderate
- **Migration difficulty:** Low to Moderate
- **Replacement difficulty:** Moderate

---

# 13. Option B — One Shared SQLite Database

## 13.1 Description

Use one SQLite database file containing all Opure tables.

Services use separate schemas by naming convention.

---

## 13.2 Advantages

- Simple backup.
- Simple connection configuration.
- Atomic transactions across all tables.
- Easy reporting joins.
- Fewer files.
- Easy initial implementation.
- One migration sequence.
- One full-text catalogue.
- One integrity check.

---

## 13.3 Disadvantages

- Weak service ownership.
- Direct cross-service joins become tempting.
- One migration can block the entire platform.
- One corruption event affects all domains.
- Project deletion is harder.
- Backup and retention cannot vary by domain.
- Database growth is concentrated.
- Write contention is concentrated.
- Test isolation is harder.
- Service extraction becomes harder.
- Trust Centre growth affects unrelated domains.
- Rebuildable indexes become mixed with authoritative state.
- Project-scoped privacy boundaries are weaker.
- One database becomes a hidden internal API.

---

## 13.4 Risks

- The shared schema becomes the true architecture.
- Services bypass contracts.
- Cross-service transactions become unmaintainable.
- Schema migrations require global coordination.
- A large Trust Centre or memory index degrades project operations.
- A future process extraction requires major database surgery.

---

## 13.5 Estimated Adoption Cost

- **Initial implementation:** Low
- **Operational complexity:** Moderate
- **Migration difficulty:** High
- **Replacement difficulty:** High

---

# 14. Option C — Embedded Document Database

## 14.1 Description

Use an embedded document database such as a BSON or JSON-oriented local engine.

---

## 14.2 Advantages

- Flexible documents.
- Convenient object persistence.
- Easy schema evolution for some records.
- Natural fit for workflow and provider payloads.
- Embedded deployment.
- May provide indexing and transactions.
- Rapid prototyping.
- Fewer relational mappings.

---

## 14.3 Disadvantages

- Weaker relational integrity.
- Ecosystem maturity varies.
- Query tooling may be less standard.
- Full-text behaviour varies.
- Cross-platform file-format stability may vary.
- Migration discipline can become informal.
- Nested documents may hide duplication.
- Provider-specific lock-in may increase.
- Recovery tooling may be weaker.
- Long-term package maintenance is less certain.
- Evidence and relationship-heavy project knowledge fits relational and graph-like queries better.
- Licence and commercial status may change.

---

## 14.4 Estimated Adoption Cost

- **Initial implementation:** Low to Moderate
- **Operational complexity:** Moderate
- **Migration difficulty:** Moderate to High
- **Replacement difficulty:** High

---

# 15. Option D — PostgreSQL or Another Local Database Server

## 15.1 Description

Install or bundle a local relational database server.

---

## 15.2 Advantages

- Strong concurrency.
- Rich SQL.
- Strong migrations and tooling.
- Full-text search.
- Extensions.
- Mature observability.
- Server-grade backup.
- Potential future team and remote deployment.
- Better high-volume analytical capabilities.
- Strong multi-process access.

---

## 15.3 Disadvantages

- Requires a server process.
- Installation complexity.
- Port and authentication management.
- Higher idle resource use.
- Update and repair complexity.
- User-account and service management.
- Difficult clean uninstall.
- Larger attack surface.
- Greater support burden.
- Disproportionate for a single-user local desktop application.
- May become a hidden infrastructure dependency.
- Portable project state becomes harder.
- Cross-platform packaging is more complex.

---

## 15.4 Risks

- Opure becomes dependent on a database server.
- Local-first installation becomes difficult.
- Background services remain after uninstall.
- Database administration becomes a product responsibility.
- Team-scale architecture is built before the local product exists.

---

## 15.5 Estimated Adoption Cost

- **Initial implementation:** High
- **Operational complexity:** Very High
- **Migration difficulty:** Moderate
- **Replacement difficulty:** High

---

# 16. Option E — File-Based JSON and Binary Records

## 16.1 Description

Store state as JSON, JSON Lines, YAML, MessagePack or custom files.

---

## 16.2 Advantages

- Human-readable formats are possible.
- Easy inspection.
- No database dependency.
- Simple export.
- Easy version control for selected configuration.
- Per-record files can isolate corruption.
- Portable.
- Natural for append-only logs.

---

## 16.3 Disadvantages

- Transactions require custom implementation.
- Concurrency is difficult.
- Indexing is custom.
- Query performance is weak.
- Referential integrity is custom.
- Schema migrations are custom.
- Partial writes require journals.
- Atomic multi-file updates are difficult.
- Large directories become slow.
- Deletion and retention become complex.
- Rebuilding indexes becomes frequent.
- Developers may edit internal state manually.
- Locking semantics vary.
- Backups can capture inconsistent sets.

---

## 16.4 Estimated Adoption Cost

- **Initial implementation:** Low
- **Operational complexity:** High
- **Migration difficulty:** High
- **Replacement difficulty:** Moderate

---

# 17. Option F — Key-Value or LSM Store

## 17.1 Description

Use RocksDB, LevelDB or another embedded key-value engine.

---

## 17.2 Advantages

- High write throughput.
- Efficient key lookup.
- Suitable for large indexes.
- Good append-heavy performance.
- Compact binary values.
- Useful for caches and vector metadata.
- Mature engines exist.

---

## 17.3 Disadvantages

- Relational constraints are absent.
- Queries and joins are custom.
- Schema and migration logic are custom.
- Native dependencies may be required.
- Crash and compaction behaviour require specialist knowledge.
- Windows packaging can be more complex.
- Debugging data is harder.
- Full-text search is separate.
- Service metadata and approvals require substantial custom structure.
- Replacement cost is high.
- Native libraries expand the trusted computing base.

---

## 17.4 Decision Relevance

A key-value engine may later support a measured indexing workload.

It is not selected as the general authoritative persistence engine.

---

## 17.5 Estimated Adoption Cost

- **Initial implementation:** High
- **Operational complexity:** High
- **Migration difficulty:** High
- **Replacement difficulty:** High

---

# 18. Option G — Multiple Storage Engines from the Beginning

## 18.1 Description

Use SQLite for metadata, a document store for workflows, a graph database for relationships, a vector database for embeddings and a log store for audit.

---

## 18.2 Advantages

- Each workload receives a specialised engine.
- Strong future scalability.
- Native graph or vector search.
- Independent tuning.
- Potential high performance.

---

## 18.3 Disadvantages

- Multiple dependencies.
- Multiple file formats.
- Multiple migration systems.
- Multiple backup systems.
- Multiple recovery paths.
- Multiple security reviews.
- Multiple package and licence risks.
- More process and memory overhead.
- More complex project deletion.
- More complex export.
- Greater cross-store consistency risk.
- Disproportionate for a small initial team.
- Harder fault injection.
- Harder user support.

---

## 18.4 Risks

- The persistence layer becomes the product.
- Service integration dominates development.
- Data truth becomes ambiguous.
- Backups are inconsistent.
- Security and retention behaviour diverge.
- Replacement becomes expensive.

---

## 18.5 Estimated Adoption Cost

- **Initial implementation:** Very High
- **Operational complexity:** Very High
- **Migration difficulty:** High
- **Replacement difficulty:** High

---

# 19. Option H — Cloud-Hosted Persistence

## 19.1 Description

Store Opure state in a remote managed database.

---

## 19.2 Advantages

- Managed backups.
- Remote access.
- Team collaboration.
- Centralised upgrades.
- High availability.
- Server-grade query performance.

---

## 19.3 Disadvantages

- Violates local-first default.
- Requires an account and network.
- Sends private project state remotely.
- Adds latency.
- Adds ongoing cost.
- Creates provider dependency.
- Complicates offline operation.
- Complicates deletion and ownership.
- Expands compliance scope.
- Makes local recovery dependent on remote service.

---

## 19.4 Decision

Rejected for core local operation.

Optional future collaboration services require separate specifications and consent.

---

# 20. Comparison Matrix

Scores:

- **1** — poor
- **2** — weak
- **3** — acceptable
- **4** — strong
- **5** — excellent

| Criterion | Weight | Service SQLite | Shared SQLite | Document DB | Local Server | Files | Key-Value | Multi-Engine | Cloud |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Charter alignment | 5 | 5 | 4 | 4 | 3 | 5 | 4 | 3 | 1 |
| Local-first | 5 | 5 | 5 | 5 | 4 | 5 | 5 | 5 | 1 |
| Zero-server install | 5 | 5 | 5 | 5 | 1 | 5 | 5 | 3 | 5 |
| Service ownership | 5 | 5 | 2 | 4 | 4 | 4 | 4 | 5 | 3 |
| Project isolation | 5 | 5 | 2 | 4 | 3 | 5 | 4 | 4 | 2 |
| Transactions | 5 | 5 | 5 | 3 | 5 | 1 | 3 | 2 | 5 |
| Recovery | 5 | 5 | 4 | 3 | 5 | 2 | 3 | 2 | 4 |
| Backup simplicity | 4 | 4 | 5 | 3 | 3 | 2 | 3 | 2 | 4 |
| Migration safety | 5 | 5 | 3 | 3 | 5 | 2 | 2 | 2 | 4 |
| Full-text support | 4 | 5 | 5 | 3 | 5 | 1 | 1 | 5 | 5 |
| Vector extensibility | 3 | 3 | 3 | 3 | 5 | 1 | 4 | 5 | 5 |
| C# ecosystem | 4 | 5 | 5 | 4 | 5 | 5 | 3 | 3 | 5 |
| Small-team fit | 5 | 5 | 5 | 4 | 1 | 3 | 2 | 1 | 3 |
| Observability | 4 | 4 | 4 | 3 | 5 | 2 | 2 | 2 | 4 |
| Exportability | 3 | 5 | 5 | 4 | 4 | 5 | 2 | 2 | 3 |
| Supply-chain control | 4 | 5 | 5 | 3 | 3 | 5 | 3 | 2 | 2 |
| Cross-platform | 4 | 5 | 5 | 4 | 4 | 5 | 4 | 3 | 5 |
| Replacement cost | 3 | 4 | 2 | 3 | 3 | 4 | 2 | 1 | 2 |
| **Indicative weighted total** |  | **450** | **358** | **344** | **339** | **327** | **308** | **288** | **282** |

Service-owned SQLite databases provide the strongest overall fit.

---

# 21. Decision

Opure will provisionally adopt:

> **SQLite through Microsoft.Data.Sqlite as the default local relational persistence engine, using service-owned database files, explicit migrations, WAL on supported local filesystems, durability classes and a separate content-addressed store for large immutable payloads.**

The decision applies to:

- Runtime state;
- project registry;
- project settings;
- workspace metadata;
- patches and patch journals;
- workflows and checkpoints;
- approvals;
- builds and tests;
- repository-operation state;
- provider and model metadata;
- project knowledge metadata;
- plugin and MCP metadata;
- Trust Centre records;
- diagnostics metadata;
- artefact metadata;
- transactional outboxes;
- and recovery records.

This decision does not approve:

- one shared database for all services;
- direct database access from Desktop or plugins;
- network-hosted SQLite files;
- raw live-file backup;
- secret storage in SQLite;
- database-server installation;
- arbitrary SQLite extensions;
- or a final vector-search implementation.

This decision is:

- [ ] Permanent until superseded
- [x] Provisional pending prototype evidence
- [ ] Experimental only
- [ ] Limited to Phase 1
- [ ] Limited to Windows permanently

---

# 22. Rationale

SQLite provides the strongest overall balance of:

- local operation;
- transactional integrity;
- low operational burden;
- cross-platform support;
- mature tooling;
- structured queries;
- full-text search;
- backup;
- and small-team productivity.

Service-owned database files physically reinforce the architectural rule that each service owns its authoritative state.

A shared database would be simpler initially, but it would create hidden coupling and make future service extraction more difficult.

A local database server would provide higher concurrency but would introduce unnecessary installation, security and maintenance complexity.

Specialised graph, document and vector stores may become useful later, but introducing them before measured need would create disproportionate recovery and migration burden.

---

# 23. Persistence Domains

A persistence domain is a database or data store owned by one service or one tightly bounded service capability.

A domain should have:

- one owner;
- one migration stream;
- one durability class;
- one backup policy;
- one retention policy;
- one access assembly;
- and one recovery procedure.

---

# 24. Suggested Database Inventory

The exact inventory may evolve.

A likely initial layout includes:

## 24.1 User and Runtime Scope

- `runtime.db`
- `projects.db`
- `settings.db`
- `providers.db`
- `plugins.db`
- `mcp.db`
- `trust.db`

## 24.2 Per-Project Scope

- `workspace.db`
- `patches.db`
- `workflows.db`
- `knowledge.db`
- `builds.db`
- `repository.db`
- `artefacts.db`

## 24.3 Rebuildable Project Indexes

- `search.db`
- `symbol-index.db`
- vector index files or databases selected later

The exact file names are implementation details.

Ownership is not.

---

# 25. Storage Root

Opure-managed persistence should live under a per-user local application-data root.

Conceptual Windows layout:

```text
%LOCALAPPDATA%\Opure\
├── profiles\
│   └── default\
│       ├── runtime\
│       ├── services\
│       ├── projects\
│       │   └── <project-id>\
│       │       ├── databases\
│       │       ├── content\
│       │       ├── indexes\
│       │       ├── journals\
│       │       └── recovery\
│       ├── backups\
│       ├── temporary\
│       └── diagnostics\
```

The final path layout requires a platform-storage ADR or implementation plan.

---

## 25.1 Repository Separation

Opure internal databases must not be placed inside:

- `.git`;
- the project source tree;
- build output;
- package cache;
- or developer documentation folders.

Internal state must not appear in project Git status.

---

## 25.2 Local Filesystem Requirement

Authoritative databases should reside on a supported local filesystem.

Opure must not intentionally create active authoritative SQLite databases on:

- SMB shares;
- NFS;
- removable media;
- cloud-mounted network drives;
- or unknown remote filesystem providers.

---

## 25.3 Cloud-Synchronised Folder

Opure should avoid placing active databases inside folders managed by:

- OneDrive;
- Dropbox;
- Google Drive;
- or equivalent synchronisation tools.

Synchronisation may copy incomplete combinations of database, WAL and shared-memory state.

Backups intended for synchronisation should be created as consistent closed backup artefacts.

---

# 26. Database Ownership

Only the owning service may:

- open a write connection;
- execute migrations;
- change schema;
- perform maintenance;
- restore backup;
- or delete the database.

Other services use commands and queries.

---

## 26.1 Read Access

Direct read access by another service is prohibited by default.

A service needing data should use:

- owner query;
- projection;
- exported snapshot;
- event-fed local read model;
- or approved public content reference.

---

## 26.2 Desktop Access

The Desktop must never open service database files.

It receives projections through the Desktop Gateway.

---

## 26.3 Plugin Access

A Plugin Host must never open Opure service database files.

Plugin private storage is mediated by the Plugin Manager.

---

## 26.4 Worker Access

A trusted worker may receive a scoped read-only content reference or task-specific temporary database.

It must not open authoritative service databases directly unless a later ADR approves a specialised worker ownership model.

---

# 27. Access Assemblies

Each persistence domain should expose one internal data-access assembly.

Example:

```text
Opure.Patching.Persistence
Opure.Workflows.Persistence
Opure.Trust.Persistence
Opure.Knowledge.Persistence
```

Architecture tests should prohibit references from unrelated services.

---

# 28. Database Manifest

Every database should have a manifest record containing:

- database kind;
- owner service;
- project scope;
- application identifier;
- schema version;
- migration history;
- durability class;
- creation time;
- last clean close;
- last backup;
- last integrity check;
- SQLite version;
- and feature flags.

Some values may be stored in both:

- database header pragmas;
- and an internal metadata table.

---

# 29. Application Identifier

Opure should assign a documented SQLite `application_id` value or bounded family of values for its internal database files.

The identifier helps distinguish Opure stores from arbitrary SQLite files.

It is not a security control.

---

# 30. Schema Version

The database should use:

- an explicit migration-history table as the authoritative migration record;
- and `user_version` as a coarse compatibility marker where useful.

The internal SQLite `schema_version` must not be modified by Opure.

---

# 31. Schema Rules

Schemas should use:

- explicit primary keys;
- explicit foreign keys;
- `NOT NULL`;
- `CHECK`;
- `UNIQUE`;
- explicit indexes;
- and `STRICT` tables where practical.

Schema should avoid relying on SQLite's permissive typing.

---

## 31.1 STRICT Tables

New authoritative tables should use `STRICT` unless a documented compatibility or data-shape reason prevents it.

Use of `ANY` requires justification.

---

## 31.2 Boolean Values

Booleans should be stored as integer values constrained to:

```sql
CHECK (value IN (0, 1))
```

---

## 31.3 Time Values

Authoritative timestamps should use one canonical UTC representation.

Proposed initial representation:

- signed 64-bit integer;
- microseconds since Unix epoch;
- UTC only.

Durations should use explicit integer units.

Local time-zone display is a presentation concern.

---

## 31.4 Identifiers

Identifiers should use stable application-generated values.

The exact identifier format is deferred.

Database row identifiers must not become public resource identifiers by accident.

---

## 31.5 Enumerations

Enums should be stored as stable text or stable numeric codes with constraints.

Application enum ordinal values must not be written implicitly.

---

## 31.6 JSON

JSON may be stored for:

- versioned opaque provider payload;
- optional extension metadata;
- and archived untrusted input.

Core authoritative fields should remain queryable columns.

JSON schemas must be versioned where behaviour depends on them.

---

## 31.7 BLOB Columns

BLOB columns should be limited to small bounded values such as:

- hashes;
- compact serialized evidence;
- or small immutable payloads.

Large source content and artefacts belong in the content store.

---

# 32. Connection Configuration

Every connection should apply and verify required configuration.

A likely authoritative-store profile includes:

```sql
PRAGMA foreign_keys = ON;
PRAGMA trusted_schema = OFF;
PRAGMA journal_mode = WAL;
PRAGMA synchronous = FULL;
PRAGMA busy_timeout = <bounded value>;
```

Additional settings require benchmark and security review.

---

## 32.1 Foreign Keys

Foreign-key enforcement must be enabled and verified on every connection.

The application must not assume the default.

---

## 32.2 Trusted Schema

`trusted_schema` should be disabled where supported and compatible.

Opure controls its schemas.

No untrusted database should be opened as an authoritative Opure service store.

---

## 32.3 Writable Schema

`writable_schema` is prohibited in normal operation.

It may be used only by a dedicated reviewed recovery tool where no safer alternative exists.

---

## 32.4 Memory Mapping

SQLite memory-mapped IO should remain disabled or conservatively bounded until measured evidence and crash-risk review justify it.

---

## 32.5 Shared Cache

Shared-cache mode should not be used as the default.

Connections should use ordinary private caches and WAL concurrency.

---

# 33. Journal Mode

Authoritative databases should use Write-Ahead Logging on supported local filesystems.

Benefits include:

- readers do not ordinarily block the writer;
- writer does not ordinarily block readers;
- committed transactions are represented in the WAL;
- and crash recovery is built into SQLite.

---

## 33.1 WAL State

A live WAL database may include:

- main database file;
- `-wal` file;
- and `-shm` file.

These files must not be separated casually.

---

## 33.2 WAL Activation

Database creation or migration should:

1. request WAL;
2. verify the returned journal mode;
3. fail or use an explicitly approved fallback if WAL cannot be enabled;
4. record the active mode;
5. and test recovery.

---

## 33.3 WAL Fallback

Opure must not silently use a weaker or incompatible configuration.

A fallback to rollback journal requires:

- explicit capability result;
- documented reason;
- adjusted concurrency;
- and test coverage.

---

# 34. Durability Classes

Persistence domains should declare one durability class.

## 34.1 Critical

Examples:

- patch transaction journal;
- approval decisions;
- workflow recovery checkpoints;
- repository-operation recovery;
- project registry;
- migration state;
- Trust Centre security records.

Recommended default:

- WAL;
- `synchronous=FULL`;
- short transactions;
- and frequent verified backup.

---

## 34.2 Authoritative

Examples:

- project settings;
- knowledge records;
- build history;
- plugin permissions;
- provider configuration metadata.

Recommended default:

- WAL;
- `synchronous=FULL` initially;
- may move to `NORMAL` only after explicit risk and performance evidence.

---

## 34.3 Rebuildable

Examples:

- full-text indexes;
- symbol indexes;
- derived summaries;
- projection caches;
- embedding indexes.

Recommended default:

- WAL or another suitable mode;
- `synchronous=NORMAL`;
- rebuild marker;
- no claim of irreplaceable durability.

---

## 34.4 Ephemeral

Examples:

- temporary import staging;
- disposable test cache;
- one-run computation scratch.

May use reduced durability or memory storage.

It must never contain the only copy of authoritative state.

---

# 35. Synchronous Policy

`synchronous=OFF` is prohibited for authoritative or rebuildable production stores.

Critical and authoritative stores should begin with `FULL`.

Rebuildable stores may use `NORMAL`.

Any change from `FULL` to `NORMAL` for authoritative stores requires:

- benchmark evidence;
- explicit acknowledgement that recent commits may be lost after power or operating-system failure;
- recovery analysis;
- and an ADR amendment.

---

# 36. Checkpointing

WAL checkpointing should be managed deliberately.

The implementation should monitor:

- WAL size;
- active readers;
- checkpoint latency;
- checkpoint completion;
- and disk pressure.

---

## 36.1 Automatic Checkpoint

SQLite's automatic checkpoint may be used initially with a reviewed threshold.

The threshold should be measured.

---

## 36.2 Application Checkpoint

The Runtime may request passive checkpoints:

- during idle periods;
- before backup;
- before controlled shutdown;
- and under disk pressure.

Aggressive checkpoints should not block interactive work unnecessarily.

---

## 36.3 Checkpoint Failure

Checkpoint failure should not automatically mean data loss.

The system should:

- report WAL growth;
- identify blocking readers;
- retry within policy;
- and enter maintenance or recovery if disk safety is threatened.

---

# 37. Writer Coordination

Each database should have one owning writer coordinator.

All state-changing persistence operations pass through it.

The coordinator should:

- serialise writes where appropriate;
- assign operation context;
- open short transactions;
- apply timeout;
- record duration;
- handle `SQLITE_BUSY`;
- and publish committed outbox events.

---

## 37.1 One Writer Does Not Mean One Thread Forever

The architecture requires one logical coordinated writer path.

Implementation may use:

- one dedicated queue;
- one connection per operation;
- or a controlled pool

as long as concurrent write behaviour remains bounded and tested.

---

## 37.2 Write Queue

A write queue may include:

- operation identifier;
- service;
- project;
- priority;
- deadline;
- cancellation;
- idempotency;
- and transaction body.

The queue must be bounded.

---

## 37.3 Interactive Priority

Interactive approvals, patch state and recovery operations should not wait behind low-priority index maintenance indefinitely.

---

# 38. Read Coordination

Read operations may use multiple connections.

Reads should:

- remain short;
- avoid holding transactions while awaiting external work;
- use pagination;
- use bounded result sets;
- and expose cancellation at the service layer.

---

## 38.1 Long Reads

Long analytical reads should:

- run at low priority;
- avoid blocking checkpoint indefinitely;
- support pagination or snapshots;
- and be cancelled under maintenance pressure.

---

# 39. Transaction Rules

Transactions must be:

- explicit for multi-statement writes;
- short;
- owned by one service;
- and free of network, model or user interaction.

A transaction must not wait for:

- AI response;
- approval;
- file dialog;
- plugin call;
- MCP call;
- build process;
- or network service.

---

## 39.1 Write Transaction Mode

Write operations may use immediate transactions where this reduces upgrade races and provides predictable lock acquisition.

The exact default requires prototype measurement.

---

## 39.2 Deferred Transactions

Deferred transactions may be used for read-mostly operations.

An upgrade from read to write can fail under contention.

The entire transaction must be safely retryable before automatic retry is allowed.

---

## 39.3 Savepoints

Savepoints may be used for bounded internal recovery inside a single database transaction.

They must not be presented as distributed or nested cross-service transactions.

---

## 39.4 No Cross-Service Transaction

Opure must not use attached service databases to simulate a global transaction.

Cross-service consistency uses:

- operation coordinator;
- committed events;
- outbox;
- idempotent handlers;
- compensation;
- and recovery state.

---

# 40. Busy Handling

SQLite write contention may return busy or locked results.

The owning service should use:

- bounded `busy_timeout`;
- short retry;
- jitter where appropriate;
- and clear final error.

---

## 40.1 Retry Eligibility

Automatic transaction retry is allowed only when:

- the entire transaction is idempotent;
- no external side effect occurred;
- input state is revalidated;
- deadline remains;
- and retry count is bounded.

---

## 40.2 No Infinite Retry

Persistence must never wait indefinitely.

A persistent busy state should become:

- degraded service;
- maintenance warning;
- or recovery issue.

---

# 41. Microsoft.Data.Sqlite Execution Model

SQLite IO through Microsoft.Data.Sqlite should be treated as synchronous.

Database calls must not run on:

- Desktop UI thread;
- IPC transport dispatch thread for long operations;
- or a latency-critical model-stream thread.

---

## 41.1 Persistence Scheduler

Services should execute storage work through a bounded persistence scheduler or owning worker queue.

The scheduler should expose:

- queue depth;
- wait time;
- execution time;
- cancellation before admission;
- and overload state.

---

## 41.2 Async API Naming

Application service methods may remain asynchronous because they coordinate scheduling and IPC.

The underlying SQLite call may execute synchronously on a controlled worker.

---

# 42. Data Access Style

Opure should use:

- `Microsoft.Data.Sqlite`;
- parameterised SQL;
- explicit SQL migrations;
- small typed row mappers;
- and repository classes owned by each service.

---

## 42.1 ORM Policy

No ORM is selected as the schema authority by this ADR.

A service may use a lightweight mapping library only when:

- SQL remains inspectable;
- migrations remain explicit;
- transaction ownership remains clear;
- query plans remain measurable;
- and architecture review approves the dependency.

---

## 42.2 Entity Framework Core

Entity Framework Core is not prohibited.

It is not assumed.

Use in a service requires evidence that it does not:

- obscure migrations;
- create unintended tracking;
- generate unbounded queries;
- or couple domain models to persistence.

---

# 43. Query Rules

All SQL should be:

- parameterised;
- reviewed;
- bounded;
- indexed where required;
- and covered by tests.

Dynamic identifiers require allowlists.

User text must never be concatenated into SQL.

---

## 43.1 Query Plans

Performance-sensitive queries should capture and review:

- query plan;
- index use;
- row counts;
- and expected bounds.

---

## 43.2 Pagination

Large result sets require:

- cursor or keyset pagination where practical;
- stable ordering;
- bounded page size;
- and continuation metadata.

Offset pagination should be avoided for very large mutable histories.

---

# 44. Indexes

Indexes should exist only for measured or clear query needs.

Every index has:

- owner;
- purpose;
- query;
- maintenance cost;
- and removal criteria.

Unused indexes should be detected during maintenance.

---

# 45. Full-Text Search

SQLite FTS5 may be used for local full-text indexing.

Suitable content includes:

- documentation;
- code symbol text;
- decisions;
- error summaries;
- build diagnostics;
- and Trust Centre searchable summaries.

---

## 45.1 FTS Is Rebuildable

FTS tables are derived indexes.

The authoritative source remains:

- structured service records;
- source files;
- or content store objects.

---

## 45.2 FTS Isolation

Full-text indexes should normally live:

- in the owning service database;
- or in a dedicated rebuildable project search database.

A global shared FTS database should not weaken project isolation.

---

## 45.3 Tokenisers

Custom native tokenisers require security and packaging review.

Built-in tokenisers should be preferred initially.

---

## 45.4 Search Provenance

Search results should retain:

- source identity;
- source revision;
- project;
- index version;
- and freshness.

---

# 46. Vector Persistence

This ADR does not select a vector database or SQLite vector extension.

The persistence architecture requires:

- vector metadata;
- embedding model identity;
- dimensions;
- source hash;
- project;
- freshness;
- and rebuild state

to remain authoritative in a service-owned store.

---

## 46.1 Vector Files

Vector data may later live in:

- a dedicated SQLite representation;
- a specialised local index;
- or a provider-managed local engine.

The final choice requires benchmarks and a separate ADR.

---

## 46.2 Extension Loading

Runtime loading of arbitrary SQLite extensions is disabled by default.

Any approved extension requires:

- pinned binary;
- hash verification;
- licence review;
- security review;
- platform packaging;
- and failure isolation.

---

# 47. Content-Addressed Store

Large immutable content should use an Opure-managed content-addressed store.

Examples:

- large source snapshots;
- patch base snapshots;
- diff payloads;
- build logs;
- test attachments;
- artefacts;
- diagnostic bundles;
- model outputs;
- and imported archives.

---

## 47.1 Content Identity

Proposed initial identity:

- SHA-256 hash;
- byte length;
- content type;
- encoding where applicable;
- and storage version.

The hash algorithm may be superseded through a versioned identifier.

---

## 47.2 Write Flow

A content write should:

1. create a restricted temporary file;
2. stream bytes;
3. calculate hash;
4. flush according to durability class;
5. verify size and hash;
6. atomically move into content-addressed location where supported;
7. commit metadata reference;
8. and schedule orphan cleanup for failed operations.

---

## 47.3 Reference Counting

Content references should be tracked by owner records.

Reference counting alone may not be sufficient after crashes.

A periodic mark-and-sweep or reconciliation job should verify reachability.

---

## 47.4 Content Immutability

A content object must never be modified in place.

Changed content creates a new identity.

---

## 47.5 Content Access

Clients receive scoped content references through services.

They do not receive unrestricted content-store root paths.

---

## 47.6 Content Classification

Content metadata should retain:

- project;
- data classification;
- retention;
- source;
- and export policy.

---

# 48. Small Versus Large Payload Threshold

The exact threshold between database BLOB and content store is deferred to benchmarks.

The threshold may vary by data type.

The decision must consider:

- query locality;
- backup size;
- memory;
- streaming;
- retention;
- and deduplication.

---

# 49. Transactional Outbox

Each service that publishes durable events should use a transactional outbox.

Within one database transaction, the service commits:

- authoritative state change;
- event envelope;
- outbox sequence;
- and idempotency metadata.

---

## 49.1 Dispatch

After commit, the outbox dispatcher:

- reads pending events;
- publishes through Runtime Messaging;
- marks dispatch progress;
- and retries safely.

---

## 49.2 Delivery Semantics

The initial target is:

- at-least-once delivery;
- ordered per owning service or aggregate where required;
- and idempotent consumers.

Exactly-once delivery is not claimed.

---

## 49.3 Inbox

Consumers with durable side effects should maintain an inbox or deduplication record.

---

## 49.4 Event Retention

Outbox records may be compacted only after:

- all required consumers progressed;
- recovery window passed;
- and audit needs are satisfied.

---

# 50. Cross-Service Workflow

A cross-service operation should use:

```text
Coordinator starts durable operation
    ↓
Owning Service A commits local state and outbox
    ↓
Event is dispatched
    ↓
Owning Service B validates and commits local state
    ↓
Coordinator observes outcome
    ↓
Compensation or recovery occurs if required
```

No hidden shared database transaction is permitted.

---

# 51. Trust Centre Persistence

The Trust Centre database should be append oriented.

A Trust record should normally be corrected by:

- superseding record;
- annotation;
- or correction event

rather than destructive update.

---

## 51.1 Sensitive Data

Trust Centre persistence must not include:

- raw secret values;
- unrestricted prompt payloads;
- full project contents by default;
- or authentication proofs.

---

## 51.2 Audit Integrity

Hash chaining, signed checkpoints or immutable segments require a dedicated audit-integrity decision.

The persistence schema should preserve the ability to add them.

---

## 51.3 Retention

Trust retention should be configurable by:

- project;
- record class;
- security requirement;
- and developer choice.

Security-critical records may require a minimum retention period.

---

# 52. Patch Persistence

Patch persistence should retain:

- patch identity;
- version;
- source;
- base revisions;
- operation list;
- review state;
- approval;
- journal;
- apply result;
- reverse data;
- validation;
- and recovery state.

Large patch payloads may use content references.

---

## 52.1 Patch Journal Durability

Patch transaction journals are Critical durability data.

A patch may not begin protected application until the required journal state is durable.

---

# 53. Workflow Persistence

Workflow persistence should retain:

- definition version;
- instance;
- stages;
- current state;
- checkpoints;
- approvals;
- retries;
- compensation;
- and final outcome.

---

## 53.1 Checkpoint Content

Large or sensitive checkpoint payloads should use:

- structured metadata;
- content references;
- and redacted state.

Secret values must not enter workflow checkpoints.

---

# 54. Knowledge Persistence

The Knowledge Engine may use relational tables for:

- files;
- revisions;
- symbols;
- relationships;
- decisions;
- errors;
- fixes;
- builds;
- tests;
- conversations;
- provenance;
- confidence;
- and pattern evidence.

---

## 54.1 Project Isolation

Knowledge databases should normally be project scoped.

Cross-project knowledge reuse requires explicit export and import.

---

## 54.2 Source of Truth

Knowledge records are evidence and structured memory.

They do not replace:

- current project files;
- build results;
- or developer decisions.

---

## 54.3 Staleness

Knowledge records should track:

- source revision;
- indexed revision;
- stale state;
- invalidation reason;
- and rebuild requirement.

---

# 55. Plugin Persistence

Plugin package metadata, enablement and permissions belong to the Plugin Manager.

A plugin may receive private storage through a mediated API.

---

## 55.1 Plugin Private Store

A plugin private store should be scoped by:

- plugin identifier;
- version;
- project where relevant;
- and data-class policy.

The plugin must not receive a database filesystem path.

---

## 55.2 Plugin Removal

Plugin uninstall should offer:

- remove package only;
- remove cached data;
- remove project-specific private data;
- and retain export where appropriate.

---

# 56. Database Migrations

Every service owns its migration sequence.

A migration should contain:

- stable identifier;
- source version;
- target version;
- checksum;
- description;
- reversible status;
- data-loss risk;
- required free space;
- and estimated class.

---

## 56.1 Migration Principles

Migrations must be:

- ordered;
- deterministic;
- tested;
- idempotency-aware;
- observable;
- and recovery designed.

---

## 56.2 Forward-Only Default

Production schema migrations are forward only by default.

Application rollback should normally restore a compatible pre-migration backup rather than run an unsafe down migration.

---

## 56.3 Transactional Migration

A migration should run inside a transaction when SQLite supports the complete operation transactionally.

Operations requiring file rebuild or external content migration need a durable migration journal.

---

## 56.4 Pre-Migration Backup

Before a destructive or material migration:

- create a consistent backup;
- verify backup integrity;
- verify free disk space;
- record manifest;
- and retain recovery instructions.

---

## 56.5 Migration Lock

Only the owning service migration coordinator may migrate a database.

The service should remain unavailable or read-only until migration completes.

---

## 56.6 Migration Failure

On failure:

- rollback transaction where possible;
- preserve backup;
- preserve migration journal;
- mark database Recovery Required;
- stop normal writes;
- and show clear recovery actions.

---

## 56.7 Migration Validation

After migration, run:

- schema validation;
- migration checksum validation;
- foreign-key check;
- targeted query checks;
- and quick integrity check where appropriate.

---

# 57. Database Creation

Database creation should:

1. create parent directory with restrictive permissions;
2. create database at canonical path;
3. set application identifier;
4. set encoding;
5. apply base pragmas;
6. create schema;
7. record migration zero;
8. verify foreign keys;
9. run quick check;
10. create first backup if required;
11. and mark clean creation.

---

# 58. Backup Architecture

Backups should be consistent database snapshots created through:

- SQLite online backup API;
- or `VACUUM INTO` when compaction and purging are specifically desired.

Raw copying of a live database is prohibited.

---

## 58.1 Backup Set

A backup set should contain:

- set identifier;
- Runtime version;
- profile;
- included services;
- included projects;
- per-database backup;
- per-database schema version;
- content-store manifest;
- creation time;
- consistency scope;
- hashes;
- and completion state.

---

## 58.2 Consistency Scope

A backup set may be:

- database consistent;
- project coordinated;
- or profile quiesced.

The manifest must state the achieved scope.

Opure must not claim one atomic moment across independent databases unless the Runtime actually quiesced relevant writers.

---

## 58.3 Coordinated Project Backup

A project backup may:

1. pause new project writes;
2. wait for current transactions;
3. establish operation boundary;
4. back up project databases;
5. snapshot referenced content manifest;
6. resume writes;
7. verify backups;
8. and record completion.

---

## 58.4 Backup Destination

Backups may be placed in:

- local Opure backup root;
- developer-selected location;
- or later approved external target.

External backup may contain private project information and requires visibility.

---

## 58.5 Backup Encryption

Backup encryption is a separate security decision.

Unencrypted backups must be clearly identified.

Secret values should not be present because the Secrets Vault is separate.

---

## 58.6 Backup Retention

Retention should support:

- count;
- age;
- milestone;
- pre-migration;
- and user-pinned backups.

Deletion should never remove the only known recovery point during an active migration.

---

# 59. Restore

Restore should occur through Recovery Mode.

A restore should:

1. validate backup manifest;
2. verify hashes;
3. verify database identifiers;
4. verify schema compatibility;
5. stop owning services;
6. preserve current damaged state;
7. restore into a staging location;
8. run integrity and foreign-key checks;
9. atomically activate where practical;
10. reconcile content references;
11. restart services;
12. and record recovery outcome.

---

## 59.1 No In-Place Blind Overwrite

Restore must not overwrite active database files without:

- service shutdown;
- staging;
- validation;
- and rollback path.

---

## 59.2 Partial Restore

Partial restore may be supported for:

- one project;
- one service;
- or one rebuildable index.

Dependency implications must be visible.

---

# 60. Integrity Checks

Opure should use layered integrity checks.

---

## 60.1 Startup Checks

Normal startup should verify:

- database header and application identifier;
- expected schema version;
- migration completion;
- last clean close;
- and required tables.

---

## 60.2 Unclean Shutdown

After unclean shutdown, run:

- WAL recovery through normal SQLite open;
- targeted `quick_check`;
- foreign-key check for affected stores;
- outbox consistency;
- and operation journal validation.

---

## 60.3 Scheduled Quick Check

Low-priority maintenance may run `quick_check` periodically.

The schedule should avoid disrupting interactive work.

---

## 60.4 Full Integrity Check

`integrity_check` should run:

- in Recovery Mode;
- before high-risk migration where appropriate;
- after suspected corruption;
- or during explicit developer maintenance.

---

## 60.5 Foreign-Key Check

`foreign_key_check` should run:

- after migration;
- after restore;
- after corruption recovery;
- and during targeted diagnostics.

---

# 61. Corruption Handling

On suspected corruption:

- stop writes;
- preserve original files including WAL state;
- create diagnostic copy;
- attempt safe integrity analysis;
- locate latest verified backup;
- identify rebuildable domains;
- and enter Recovery Required.

---

## 61.1 No Automatic Destructive Repair

Opure must not silently:

- delete tables;
- drop indexes;
- rewrite schema;
- or discard records

to make a corruption warning disappear.

---

## 61.2 Rebuildable Store Recovery

A rebuildable index may be:

- quarantined;
- deleted;
- recreated from authoritative data;
- and marked with a new index version.

---

## 61.3 Authoritative Store Recovery

Authoritative corruption requires:

- backup restore;
- safe SQLite recovery tooling where justified;
- or developer-guided export.

The result must record possible data loss honestly.

---

# 62. Clean Shutdown

At controlled shutdown, each owning service should:

- stop accepting writes;
- drain bounded queue;
- commit or roll back current transaction;
- flush outbox state;
- checkpoint according to policy;
- close connections;
- record clean close;
- and release locks.

---

# 63. Crash Recovery

SQLite handles transaction recovery.

Opure must additionally recover domain operations.

After a crash, services should reconcile:

- transaction journal;
- outbox;
- workflow checkpoint;
- patch journal;
- repository state;
- content-store temporary files;
- and migration state.

---

# 64. Content-Store Recovery

On startup or maintenance, inspect:

- incomplete temporary files;
- metadata without content;
- content without metadata;
- hash mismatch;
- expired references;
- and orphan candidates.

Deletion should use a grace period.

---

# 65. Retention

Every persistence domain should declare retention for:

- active state;
- history;
- audit;
- logs;
- content objects;
- backups;
- and rebuildable indexes.

---

## 65.1 Default Retention

Default retention should preserve developer understanding without unbounded growth.

Exact values are product settings and require measured use.

---

## 65.2 User Control

Developers should be able to:

- inspect storage use;
- delete project memory;
- remove build history;
- clear rebuildable indexes;
- remove backups;
- and delete a project registration without deleting source code.

---

# 66. Project Deletion

Removing a project from Opure must distinguish:

- unregister project;
- delete Opure project memory;
- delete Opure build and workflow history;
- delete Opure content references;
- delete Opure backups;
- and delete source workspace.

Source workspace deletion must never be the default.

---

## 66.1 Deletion Workflow

A project-data deletion should:

1. show affected domains;
2. stop project writes;
3. revoke project sessions;
4. remove service records;
5. mark content references unreachable;
6. delete project databases;
7. schedule content cleanup;
8. retain or delete backups according to choice;
9. and record completion without retaining deleted content.

---

## 66.2 Secure Deletion Limitation

Opure must not claim guaranteed forensic erasure from:

- SSDs;
- filesystem snapshots;
- backups;
- cloud synchronisation;
- or storage-controller caches.

Logical deletion and best-effort local cleanup must be described honestly.

---

# 67. Vacuum and Space Reclamation

Automatic full vacuum on every deletion is prohibited.

Space reclamation should be:

- scheduled;
- low priority;
- cancellable where possible;
- free-space aware;
- and visible.

---

## 67.1 Auto Vacuum

Auto-vacuum mode should not be enabled globally without benchmarks.

It may increase fragmentation and write cost.

---

## 67.2 VACUUM

Full `VACUUM` may be used during explicit maintenance.

It requires additional free disk space and exclusive write activity.

---

## 67.3 VACUUM INTO

`VACUUM INTO` may be used to create compact backup copies when its CPU and disk cost are acceptable.

---

# 68. Database Optimisation

Maintenance may use:

- `PRAGMA optimize`;
- targeted index analysis;
- stale-index detection;
- checkpoint management;
- and controlled vacuum.

Maintenance must not run blindly after every startup.

---

# 69. Data Encryption

## 69.1 Secrets

Secret values must never enter ordinary SQLite databases.

They remain in the encrypted Secrets Vault.

---

## 69.2 Database-Wide Encryption

Database-wide encryption is not selected by this ADR.

It requires a separate decision covering:

- threat model;
- key management;
- search and indexing;
- performance;
- native dependencies;
- licensing;
- backup;
- and recovery.

---

## 69.3 Operating-System Protection

Initial stores should rely on:

- per-user directory permissions;
- operating-system account security;
- and optional full-disk encryption managed by the developer or organisation.

Opure must not misrepresent ACL-protected plaintext databases as application-encrypted.

---

## 69.4 Sensitive Fields

Field-level encryption may be introduced for narrowly scoped values that are sensitive but not secrets.

It requires:

- key management;
- query limitations;
- rotation;
- and separate security review.

---

# 70. Privacy Impact

Persistence may contain private project information such as:

- file paths;
- source-derived summaries;
- errors;
- workflow history;
- prompts;
- model outputs;
- and Trust Centre metadata.

The platform should:

- minimise duplicated source content;
- classify records;
- provide retention controls;
- support project deletion;
- avoid external backup by default;
- and display storage location.

---

# 71. Data Minimisation

Services should not persist data merely because it was available.

A record should identify:

- purpose;
- owner;
- retention;
- classification;
- source;
- and deletion behaviour.

---

# 72. Prompt and Conversation Persistence

Prompt and conversation persistence should be:

- project scoped;
- configurable;
- redacted;
- and retention controlled.

Raw provider requests should not be retained automatically when a structured summary and evidence are sufficient.

Secret values must be removed before persistence.

---

# 73. Source Content Persistence

Opure should prefer:

- file identity;
- revision hash;
- selected snippets;
- and content references

over duplicating every project file indefinitely.

A complete snapshot is stored only when required for:

- patch recovery;
- reproducibility;
- explicit checkpoint;
- or developer-approved archive.

---

# 74. Observability

Persistence observability should include:

- database size;
- WAL size;
- free pages;
- open connection count;
- write queue depth;
- write wait time;
- transaction duration;
- busy count;
- retry count;
- checkpoint duration;
- last backup;
- last integrity check;
- migration state;
- and content-store size.

---

## 74.1 Query Logging

SQL text may be logged in development with parameter values redacted.

Production logs should prefer:

- query identifier;
- duration;
- rows;
- database domain;
- and error code.

Project content and secret values must not appear.

---

## 74.2 Slow Query

A slow-query record should include:

- query identifier;
- owner;
- duration;
- result count;
- plan reference;
- and correlation.

---

## 74.3 Storage Dashboard

The desktop may expose:

- total Opure storage;
- storage by project;
- storage by service;
- database health;
- backup status;
- rebuildable index size;
- and cleanup actions.

---

# 75. Trust Centre Records

Significant persistence events include:

- migration;
- backup;
- restore;
- corruption detection;
- project deletion;
- retention cleanup;
- integrity failure;
- database recovery;
- and permission failure.

Routine transactions should not flood the Trust Centre.

---

# 76. Error Model

Persistence errors should include stable categories such as:

- database unavailable;
- busy;
- timeout;
- constraint violation;
- incompatible schema;
- migration failed;
- corruption suspected;
- disk full;
- permission denied;
- backup failed;
- restore failed;
- content missing;
- content hash mismatch;
- and recovery required.

---

# 77. Disk-Full Behaviour

Disk-full conditions must be tested.

On disk full:

- stop non-essential writes;
- preserve Critical writes where possible;
- stop content ingestion;
- warn clearly;
- avoid repeated retry;
- retain transaction integrity;
- and provide cleanup guidance.

---

# 78. Low-Disk Policy

The Runtime should monitor free space.

Thresholds may trigger:

- warning;
- pause rebuildable indexing;
- pause large model downloads;
- force checkpoint;
- prevent backup;
- or deny high-risk migration.

---

# 79. File Permissions

Database directories should use restrictive per-user permissions.

The Runtime should validate permissions at creation and during diagnostics.

A database with unexpectedly broad permissions should produce a security warning or fail for Critical stores.

---

# 80. Canonical Paths

Database files must be opened through canonical Opure-managed paths.

Opure should avoid:

- symbolic links;
- junctions;
- hard links;
- multiple aliases;
- and user-selected arbitrary live database locations.

---

# 81. File Handles

Only the owning Runtime service should keep database connections open.

Backup, diagnostic and recovery tools must coordinate before accessing stores.

---

# 82. Multiple SQLite Libraries

The application should distribute and use one reviewed SQLite library build per process where practical.

Uncontrolled native packages that bundle separate SQLite versions require review.

---

# 83. Package Selection

The proposed provider is:

```text
Microsoft.Data.Sqlite
```

The exact package variant and native bundle require a package ADR or implementation decision.

---

## 83.1 Package Requirements

The selected package configuration must provide:

- supported SQLite version;
- FTS5 support;
- required security pragmas;
- deterministic deployment;
- x64 support;
- future ARM64 path;
- Linux and macOS path;
- and licence inventory.

---

## 83.2 Version Pinning

SQLite and provider versions must be centrally pinned.

Updates require:

- release-note review;
- corruption and WAL regression review;
- migration tests;
- backup tests;
- and performance tests.

---

# 84. Security Impact

## 84.1 Trust Boundaries

Persistence boundaries include:

- Runtime to filesystem;
- owning service to database;
- service to content store;
- migration tool;
- backup destination;
- restore source;
- plugin private storage;
- and developer-selected export.

---

## 84.2 Threats

Relevant threats include:

- path substitution;
- database replacement;
- schema tampering;
- malicious SQLite file;
- broad file permissions;
- SQL injection;
- unsafe extension loading;
- backup theft;
- restore from untrusted file;
- content hash collision or mismatch;
- disk exhaustion;
- rollback to vulnerable schema;
- and direct cross-service access.

---

## 84.3 Mitigations

- canonical managed paths;
- application identifier;
- schema checksum;
- restrictive ACL;
- parameterised SQL;
- `trusted_schema=OFF`;
- extension loading disabled;
- backup validation;
- restore staging;
- content hashing;
- size limits;
- migration signature or checksum;
- and architecture tests.

---

## 84.4 Untrusted SQLite Files

Opure must not attach or open arbitrary project SQLite databases as trusted service stores.

Project database analysis should occur through:

- read-only bounded tooling;
- worker isolation;
- and untrusted-data policy.

---

# 85. Reliability and Recovery

## 85.1 Failure Modes

Potential failure modes include:

- process crash during transaction;
- operating-system crash;
- power failure;
- disk full;
- permission change;
- WAL growth;
- long reader;
- busy timeout;
- migration interruption;
- backup interruption;
- content write interruption;
- database corruption;
- and mismatched backup set.

---

## 85.2 Recovery Principle

SQLite restores database transaction consistency.

Opure restores domain-operation consistency.

Both layers are required.

---

## 85.3 Recovery Evidence

Every recovery should record:

- original condition;
- database identity;
- backup used;
- actions;
- integrity results;
- data loss possibility;
- and final state.

---

# 86. Performance Impact

## 86.1 Expected Performance

SQLite should be suitable for Opure's local metadata and history workloads.

Potential bottlenecks include:

- long write transactions;
- unbounded Trust Centre tables;
- full-text rebuild;
- large knowledge graphs;
- and content-store scanning.

These require measurement.

---

## 86.2 Reference Hardware

Tests should use:

- Windows 11;
- Ryzen 9 5950X;
- 32 GB RAM;
- RTX 5070 Ti 16 GB;
- and local SSD storage.

Database performance should remain a small part of normal interactive latency.

---

## 86.3 Required Benchmarks

Measure:

- database creation;
- migration;
- project registration;
- 1,000 concurrent read requests;
- write queue latency;
- workflow checkpoint rate;
- Trust Centre append rate;
- patch journal writes;
- build-result insertion;
- knowledge ingestion;
- FTS query;
- backup;
- restore;
- quick check;
- full integrity check;
- checkpoint;
- project deletion;
- and content-store deduplication.

---

# 87. Scale Test Data

Prototype at least:

- 1,000 registered projects in registry tests;
- 1,000,000 Trust Centre records;
- 100,000 workflow instances;
- 10,000,000 searchable text records in stress tests where practical;
- 10,000,000 symbol relationships in a synthetic knowledge test;
- 100 GB content store synthetic manifest;
- and large per-project build histories.

These are stress scenarios, not ordinary expectations.

---

# 88. Testing Strategy

## 88.1 Unit Tests

Test:

- schema mapping;
- constraints;
- timestamp conversion;
- identifier conversion;
- transaction retry eligibility;
- migration ordering;
- outbox state;
- content hashing;
- retention;
- and error translation.

---

## 88.2 Migration Tests

For every migration:

- empty database;
- previous version;
- representative large database;
- interrupted migration;
- insufficient disk;
- duplicate execution;
- and restore after failure.

---

## 88.3 Integration Tests

Test:

- Runtime startup;
- database creation;
- service write;
- concurrent read;
- outbox dispatch;
- backup;
- restore;
- project deletion;
- and index rebuild.

---

## 88.4 Architecture Tests

Enforce:

- one service owns each database;
- no Desktop persistence reference;
- no Plugin Host direct SQLite access;
- no worker authoritative database access;
- no cross-service repository reference;
- no arbitrary extension loading;
- and no shared global database assembly.

---

## 88.5 Security Tests

Test:

- SQL injection;
- path replacement;
- broad ACL;
- malicious schema;
- wrong application identifier;
- untrusted restore;
- content hash mismatch;
- symlink or junction database path;
- extension-load attempt;
- and backup disclosure.

---

## 88.6 Fault-Injection Tests

Inject:

- process termination before commit;
- process termination after commit;
- Runtime termination during checkpoint;
- disk full;
- access denied;
- WAL file retained;
- backup interruption;
- restore interruption;
- content temp-file interruption;
- migration interruption;
- and corrupted page.

---

## 88.7 Recovery Tests

Test:

- unclean close;
- hot WAL recovery;
- incomplete outbox;
- incomplete migration;
- corrupt rebuildable index;
- corrupt authoritative store;
- missing content object;
- orphan content;
- and incompatible backup.

---

## 88.8 Performance Tests

Measure:

- latency;
- throughput;
- lock wait;
- allocations;
- WAL growth;
- checkpoint;
- FTS;
- backup;
- and integrity checks.

---

# 89. Prototype Plan

## 89.1 Prototype A — Service-Owned Stores

Implement:

- Project Manager database;
- Workflow database;
- Trust Centre database;
- architecture tests;
- and separate migrations.

---

## 89.2 Prototype B — WAL and Writer Coordination

Implement:

- WAL;
- FULL synchronous;
- writer queue;
- concurrent reads;
- busy timeout;
- bounded retry;
- and metrics.

---

## 89.3 Prototype C — Crash Recovery

Simulate:

- termination during write;
- termination after commit;
- retained WAL;
- and Runtime restart.

Verify:

- transaction consistency;
- operation recovery;
- and no blind replay.

---

## 89.4 Prototype D — Migration

Implement:

- version zero;
- additive migration;
- table rebuild migration;
- pre-migration backup;
- interrupted migration;
- and Recovery Mode.

---

## 89.5 Prototype E — Backup and Restore

Implement:

- online backup;
- coordinated project backup set;
- hash manifest;
- staged restore;
- integrity check;
- and rollback.

---

## 89.6 Prototype F — Content Store

Implement:

- streamed write;
- SHA-256;
- atomic activation;
- metadata transaction;
- duplicate content;
- missing content;
- orphan collection;
- and project deletion.

---

## 89.7 Prototype G — Outbox

Implement:

- state and event commit;
- dispatcher crash;
- duplicate delivery;
- consumer inbox;
- ordered replay;
- and compaction.

---

## 89.8 Prototype H — Full-Text Search

Implement:

- FTS5 index;
- source provenance;
- stale record;
- rebuild;
- search latency;
- and project isolation.

---

# 90. Implementation Plan

## 90.1 Initial Tasks

1. Record founder review.
2. Select and pin the .NET LTS release.
3. Select and pin Microsoft.Data.Sqlite package.
4. Verify bundled SQLite features.
5. Define persistence abstractions.
6. Define database manifest.
7. Define durability classes.
8. Define storage-root layout.
9. Define service database access rules.
10. Implement connection configuration.
11. Implement migration framework.
12. Implement writer coordinator.
13. Implement transactional outbox.
14. Implement online backup.
15. Implement integrity service.
16. Implement content-addressed store.
17. Implement project deletion.
18. Implement FTS prototype.
19. Complete security review.
20. Benchmark.
21. Accept, amend or reject the ADR.

---

## 90.2 Owners

| Area | Owner |
|---|---|
| Product decision | Founder |
| Persistence architecture | Persistence Architecture Owner |
| Runtime integration | Runtime Architecture Owner |
| Service ownership | Service Architecture Owner |
| Migrations | Persistence Owner |
| Backup and recovery | Recovery Owner |
| Content store | Workspace and Artefact Owners |
| Full-text search | Knowledge Owner |
| Trust Centre | Trust Centre Owner |
| Security | Security Owner |
| Performance | Performance Owner |
| Testing | Test Architecture Owner |

---

# 91. Suggested Repository Structure

```text
src/
├── Opure.Persistence.Abstractions/
├── Opure.Persistence.Sqlite/
├── Opure.Persistence.Migrations/
├── Opure.Persistence.Backup/
├── Opure.Persistence.Integrity/
├── Opure.Persistence.Outbox/
├── Opure.ContentStore/
├── Opure.ContentStore.Abstractions/
└── Opure.Platform.Storage.Windows/
```

Service-owned projects may include:

```text
├── Opure.Projects.Persistence/
├── Opure.Workflows.Persistence/
├── Opure.Patching.Persistence/
├── Opure.Knowledge.Persistence/
├── Opure.Build.Persistence/
├── Opure.Repository.Persistence/
├── Opure.Trust.Persistence/
├── Opure.Plugins.Persistence/
└── Opure.Mcp.Persistence/
```

---

# 92. Suggested Database Metadata Tables

Each database may contain:

```text
opure_database
opure_migrations
opure_outbox
opure_inbox
opure_maintenance
opure_recovery
```

Not every domain requires every table.

Names are implementation details.

Semantics are architectural.

---

# 93. Naming Rules

Database objects should use:

- consistent lower-case naming;
- explicit singular or plural convention;
- stable migration identifiers;
- no reserved ambiguous terms;
- and no provider-specific hidden tables outside approved features.

---

# 94. Data Export

Services should support structured export without requiring users to manipulate SQLite files.

Possible export formats include:

- JSON;
- JSON Lines;
- CSV for tabular data;
- Markdown for readable records;
- and content bundles.

Export must preserve:

- schema version;
- project;
- provenance;
- classification;
- and hashes.

---

# 95. Data Import

Import must treat all data as untrusted.

Import should:

- validate schema;
- validate size;
- validate identity;
- scan secrets where relevant;
- stage;
- preview;
- and create service-owned records through commands.

Direct database replacement is a Recovery action, not ordinary import.

---

# 96. Cross-Version Portability

A newer Opure version may migrate an older database.

An older Opure version must not open and write a newer incompatible database.

It should fail clearly and preserve the data.

---

# 97. Downgrade

Application downgrade is not guaranteed after schema migration.

The update process should preserve a compatible backup before migration.

Downgrade uses:

- application rollback;
- database restore;
- and version compatibility validation.

---

# 98. Multi-Profile Isolation

Each Runtime profile must use separate:

- database roots;
- content stores;
- backups;
- endpoints;
- and credentials.

A test profile must not share production stores.

---

# 99. Development Databases

Development should support:

- disposable profile;
- deterministic fixtures;
- seeded databases;
- migration from historical fixtures;
- and corruption samples.

Real developer projects should not be used as required test fixtures.

---

# 100. Test Database Strategy

Tests may use:

- temporary on-disk SQLite;
- controlled in-memory SQLite for limited unit cases;
- and full filesystem stores for integration and recovery.

In-memory tests do not replace WAL and crash tests.

---

# 101. Database Inspection Tools

Opure may provide a developer diagnostics command to inspect:

- schema;
- migrations;
- sizes;
- integrity;
- and safe metadata.

It should not expose raw secret values because none should exist in these stores.

Direct editing is unsupported.

---

# 102. Maintenance Mode

A service may enter Maintenance Mode for:

- migration;
- vacuum;
- restore;
- full integrity check;
- or large rebuild.

The desktop should show:

- affected capability;
- progress;
- cancellation limits;
- and recovery state.

---

# 103. Safe Mode

Safe Mode should open authoritative databases conservatively.

It may:

- disable optional writes;
- skip rebuildable indexes;
- defer non-essential migrations;
- and provide backup and integrity tools.

---

# 104. Recovery Mode

Recovery Mode should support:

- database inventory;
- manifest validation;
- integrity checks;
- backup restore;
- content reconciliation;
- index rebuild;
- and export.

It must minimise third-party dependencies.

---

# 105. Service Degradation

If one service database fails:

- unrelated services should remain available where safe;
- the Runtime should become Ready with Degraded Capabilities;
- dependent workflows should pause;
- and the exact affected capability should be visible.

Critical database failure may require Runtime Recovery Mode.

---

# 106. Rebuildability Declaration

Every derived store should declare:

- authoritative sources;
- rebuild command;
- expected cost;
- required disk space;
- and validation.

A store without a tested rebuild path is not rebuildable merely because it was labelled as such.

---

# 107. Backup Verification

A backup is not complete until:

- copy operation succeeded;
- hash manifest exists;
- database opens;
- quick check passes;
- foreign-key check passes where relevant;
- and schema compatibility is recorded.

---

# 108. Restore Verification

A restore is not complete until:

- databases open;
- integrity checks pass;
- services start;
- outbox state is reconciled;
- content references resolve;
- and the developer sees the result.

---

# 109. Release Requirements

A release must not ship if:

- migrations are untested;
- backup restore fails;
- Critical stores use `synchronous=OFF`;
- foreign keys are disabled;
- architecture tests permit cross-service database access;
- content-store recovery is incomplete;
- or known corruption can be hidden.

---

# 110. Acceptance Criteria

This ADR may move to **Accepted** when:

- [ ] Microsoft.Data.Sqlite is pinned.
- [ ] The bundled SQLite version and features are verified.
- [ ] Service-owned database boundaries are implemented.
- [ ] The Desktop cannot reference persistence projects.
- [ ] Plugin Hosts cannot open Opure databases.
- [ ] Workers cannot own authoritative stores.
- [ ] Authoritative databases use local managed paths.
- [ ] Network and synced database locations are rejected or clearly unsupported.
- [ ] Application identifiers are verified.
- [ ] Explicit migrations are implemented.
- [ ] STRICT tables are used where practical.
- [ ] Foreign-key enforcement is verified per connection.
- [ ] Trusted schema is disabled where supported.
- [ ] WAL is enabled and verified.
- [ ] Critical stores use FULL synchronous.
- [ ] Rebuildable stores declare durability.
- [ ] Writer coordination is bounded.
- [ ] Long transactions are detected.
- [ ] Busy handling is bounded.
- [ ] Database work does not run on the UI thread.
- [ ] Cross-service transactions are prohibited.
- [ ] Transactional outbox delivery works.
- [ ] Duplicate event handling works.
- [ ] Online backup works.
- [ ] Raw live-file backup is prohibited.
- [ ] Staged restore works.
- [ ] Pre-migration backup works.
- [ ] Interrupted migration enters Recovery Mode.
- [ ] Quick and full integrity checks are implemented.
- [ ] Foreign-key checks are implemented.
- [ ] Corrupt rebuildable indexes can be rebuilt.
- [ ] Authoritative corruption recovery is documented.
- [ ] Content-addressed storage works.
- [ ] Content hash validation works.
- [ ] Orphan cleanup is crash safe.
- [ ] Project deletion leaves source code untouched.
- [ ] Secret values are absent from ordinary databases.
- [ ] FTS5 project isolation works.
- [ ] Arbitrary SQLite extension loading is disabled.
- [ ] Storage metrics are exposed.
- [ ] Disk-full behaviour is tested.
- [ ] Performance is measured.
- [ ] Security review is complete.
- [ ] Founder approval is recorded.

---

# 111. Evidence Required Before Acceptance

- [ ] package and licence review;
- [ ] SQLite feature report;
- [ ] service-boundary architecture tests;
- [ ] WAL recovery test;
- [ ] writer-contention test;
- [ ] transaction retry test;
- [ ] migration test suite;
- [ ] interrupted migration test;
- [ ] backup and restore report;
- [ ] corruption and recovery report;
- [ ] content-store test;
- [ ] outbox and inbox test;
- [ ] full-text search benchmark;
- [ ] disk-full test;
- [ ] project deletion test;
- [ ] security review;
- [ ] performance benchmark;
- [ ] founder approval.

---

# 112. Known Limitations

- The exact .NET LTS release is not yet pinned.
- The exact Microsoft.Data.Sqlite package variant is not yet selected.
- The exact SQLite version is not yet pinned.
- The final vector engine is not selected.
- Database-wide encryption is deferred.
- The exact local storage path is not final.
- The exact content-store BLOB threshold is not measured.
- The exact WAL checkpoint policy is not measured.
- Busy timeout values are not measured.
- Backup encryption is deferred.
- Audit hash chaining is deferred.
- Cloud backup is deferred.
- Team and remote persistence are deferred.
- Cross-database point-in-time backup is coordinated rather than inherently atomic.
- Same-user malicious code may access plaintext local state under the user's permissions.
- Secure deletion cannot be guaranteed on all storage hardware.
- SQLite synchronous IO requires controlled scheduling.

---

# 113. Open Questions

- Which .NET LTS release should be pinned?
- Which Microsoft.Data.Sqlite package variant should be used?
- Which SQLite version and native bundle should be pinned?
- Should all authoritative stores begin with FULL synchronous?
- Which stores may safely use NORMAL?
- What busy timeout should each durability class use?
- Should writer coordination use one dedicated thread or a bounded worker scheduler?
- What WAL checkpoint threshold is appropriate?
- What is the maximum acceptable WAL size?
- Which database domains should be global versus project scoped?
- Should Trust Centre use one global database or one project database plus a global index?
- What content size should move out of SQLite?
- Which identifier format should records use?
- Which UTC timestamp precision should be standard?
- Should application identifiers differ by database kind?
- Should project backup quiesce all project services?
- How should backup sets be encrypted?
- Which retention defaults should ship?
- How should project memory export work?
- Which vector engine should be selected?
- Should a vector worker use a separate process?
- Should FTS use one project search database or service-local indexes?
- Which SQLite extensions, if any, are acceptable?
- Should field-level encryption be used for selected conversation records?
- How should storage paths behave when the Windows profile itself is redirected?
- Which corruption recovery tools may ship with Opure?

---

# 114. Deferred Decisions

This ADR intentionally defers:

- Secrets Vault implementation to a dedicated security ADR;
- vector search to a vector persistence ADR;
- database-wide encryption to a data-at-rest ADR;
- identifier format to a contract and identity ADR;
- exact platform storage layout to a platform storage ADR;
- backup encryption to a security ADR;
- audit hash-chain implementation to an audit-integrity ADR;
- cloud backup to a future optional-service specification;
- remote collaboration storage to post-1.0 planning;
- telemetry storage because telemetry is not required;
- and distributed persistence because the first product is local.

---

# 115. Alternatives Rejected

## 115.1 One Shared SQLite Database

Rejected because it would make one schema the hidden integration layer and weaken service and project isolation.

---

## 115.2 Embedded Document Database as the Foundation

Rejected because Opure requires strong relationships, constraints, migrations and standard recovery tooling across many engineering domains.

---

## 115.3 Local PostgreSQL or Equivalent Server

Rejected because the installation, background service, authentication, update and support burden is disproportionate for a local single-user product.

---

## 115.4 JSON Files as Authoritative State

Rejected because Opure would need to reimplement transactions, indexes, concurrency, migration and integrity.

---

## 115.5 Key-Value Store as the General Engine

Rejected because most authoritative Opure state benefits from relational constraints and queryability.

---

## 115.6 Multiple Specialised Stores Immediately

Rejected because the backup, migration, security and recovery burden would delay the first usable product.

---

## 115.7 Cloud Database

Rejected because core Opure operation must remain local and offline without mandatory external data transfer.

---

# 116. Official Evidence References

The following official sources informed this ADR:

- [SQLite documentation](https://sqlite.org/docs.html)
- [SQLite Write-Ahead Logging](https://sqlite.org/wal.html)
- [SQLite database file format](https://sqlite.org/fileformat.html)
- [SQLite transactions](https://sqlite.org/lang_transaction.html)
- [SQLite pragma reference](https://sqlite.org/pragma.html)
- [SQLite STRICT tables](https://sqlite.org/stricttables.html)
- [SQLite FTS5](https://sqlite.org/fts5.html)
- [SQLite VACUUM and VACUUM INTO](https://sqlite.org/lang_vacuum.html)
- [SQLite corruption guidance](https://sqlite.org/howtocorrupt.html)
- [Microsoft.Data.Sqlite overview](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/)
- [Microsoft.Data.Sqlite transactions](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions)
- [Microsoft.Data.Sqlite online backup](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/backup)
- [Microsoft.Data.Sqlite async limitations](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async)
- [Microsoft.Data.Sqlite ADO.NET limitations](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/adonet-limitations)

Versions and behaviour can change.

The implementation team must verify these sources against the pinned package and SQLite library before acceptance.

---

# 117. Review Record

| Date | Reviewer | Decision | Notes |
|---|---|---|---|
| 18 July 2026 | Architecture draft | Proposed | Service-owned SQLite stores recommended with WAL, durability classes and content store |

---

# 118. Approval

## Founder or Product Approval

- **Name:** Christopher Dyer
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Pending founder review

## Architecture Approval

- **Name or role:** Persistence Architecture Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Service ownership, migration and backup prototypes required

## Security Approval

- **Name or role:** Security Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Path, ACL, schema, backup and extension review required

## Recovery Approval

- **Name or role:** Recovery Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Crash, migration, restore and corruption tests required

## Performance Approval

- **Name or role:** Performance Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** WAL, writer queue, FTS, backup and integrity benchmarks required

---

# 119. Supersession

This ADR is superseded only when a later ADR:

- names ADR-0005 explicitly;
- explains why the persistence engine or ownership model changed;
- identifies affected services and databases;
- describes schema and content migration;
- describes backup and restore compatibility;
- explains security impact;
- and updates the `Superseded by` field.

Historical ADRs remain in version control.

---

# 120. Change History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | 18 July 2026 | Founder Draft | Initial service-owned SQLite persistence recommendation |

---

# 121. Final Decision Statement

> **Opure will provisionally use SQLite through Microsoft.Data.Sqlite for local authoritative service state, with one database per bounded service-owned domain, WAL on supported local filesystems, explicit durability classes, reviewed migrations, transactional outboxes, verified backup and recovery, and a separate content-addressed store for large immutable payloads, because this provides the strongest balance of local control, reliability, isolation, portability and small-team delivery without requiring a database server or hidden cloud dependency.**

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**