# ADR-0028 — Backup, Restore, Data Portability and Disaster Recovery

## Architecture Decision Record

**Status:** Proposed
**Date:** 18 July 2026
**Decision owners:** Founder and Product Owner
**Technical owners:** Backup, Recovery and Data Portability Architecture Owner
**Reviewers:** Runtime Architecture Owner, Persistence Owner, Security Owner, Privacy Owner, Secrets Owner, Configuration Owner, Project Service Owner, Workspace Owner, Repository Owner, Patch Service Owner, Build Owner, Test Owner, Workflow Owner, Context Assembly Owner, AI Router Owner, Provider Trust Owner, Local Model Runtime Owner, Project Knowledge Owner, Project Memory Owner, Tool Mediation Owner, Plugin Platform Owner, MCP Gateway Owner, Network Gateway Owner, Trust Evidence Owner, Update Owner, Release Owner, Desktop Owner, Enterprise Management Owner, Filesystem Owner, Performance Owner, Test Architecture Owner
**Supersedes:** None
**Superseded by:** None
**Related ADRs:** ADR-0001 through ADR-0027
**Related specifications:** CHARTER-001, SPEC-001 through SPEC-012
**Target milestone:** Recoverable and Portable Local Platform through Version 1.0

---

## 1. Decision Summary

Opure should implement a trusted local **Backup and Recovery Service** that coordinates service-owned snapshots, managed backup repositories, portable backup archives, selective restores, data-portability exports, recovery rehearsals and disaster-recovery runbooks without becoming the authoritative owner of another service's state.

The architecture should make a strict distinction between:

1. **Crash Recovery**, which uses each service's transactional durability and journals;
2. **Local Recovery Points**, which support rollback around application updates, schema migrations and risky maintenance;
3. **Managed Backup Repositories**, which provide user-configured incremental, encrypted and verified backup history;
4. **Portable Backup Archives**, which provide self-contained encrypted recovery sets;
5. **Data Portability Bundles**, which contain stable, structured and machine-readable user data rather than internal recovery state;
6. **Workspace Recovery Packs**, which preserve selected uncommitted project changes without silently copying an entire repository;
7. **Vault Recovery Capsules**, which optionally make selected secrets portable through a separately reviewed encryption boundary;
8. **Restore Plans**, which stage, validate, map and activate recovered state;
9. **Disaster-Recovery Plans**, which describe clean rebuild and recovery after device loss, corruption, ransomware or other major disruption;
10. and **Recovery Exercises**, which prove that backups can actually be restored.

The selected principle is:

> **A backup is not valid merely because bytes were copied. It becomes trustworthy only when the owning services produced consistent snapshots, every object is inventoried and authenticated, restoration is validated in isolation, and the recovery path has been exercised.**

The Backup and Recovery Service should coordinate:

* backup-set identity;
* recovery objectives;
* owner-service backup contracts;
* consistency barriers;
* SQLite Online Backup operations;
* immutable-file and CAS pinning;
* workspace-change capture;
* backup-repository encryption;
* chunk storage;
* manifests;
* retention;
* verification;
* restore planning;
* staged restore;
* path and identity remapping;
* service activation;
* rollback;
* recovery reports;
* portability export;
* portability import;
* Vault Recovery Capsules;
* disaster-recovery mode;
* and exercises.

The service should not own:

* service databases;
* Vault plaintext;
* project source;
* provider credentials;
* model execution;
* workflows;
* plugin trust;
* MCP trust;
* repository history;
* or remote storage accounts.

Each owner service should implement a versioned **Backup Adapter**.

A Backup Adapter should expose:

* owned state inventory;
* backup-critical state;
* rebuildable state;
* owner schema versions;
* consistency checkpoint;
* SQLite database snapshot;
* immutable artefact references;
* mutable-file snapshot;
* excluded state;
* restore validator;
* restore migrator;
* activation requirements;
* deletion and erasure metadata;
* and recovery dependencies.

The initial service-owned database backup mechanism should use SQLite's **Online Backup API** through a reviewed native or managed adapter.

Opure should not copy a live SQLite database file with an ordinary filesystem copy.

Opure should not rely on copying only the main `.db` file while a WAL or hot journal may exist.

`VACUUM INTO` may be used for:

* compact archival copies;
* privacy-oriented exports where deleted free pages should not remain;
* and selected maintenance snapshots

when its I/O, free-space and interruption behaviour is acceptable.

The Online Backup API should remain the normal mechanism because it:

* produces a consistent live-database snapshot;
* can copy incrementally;
* avoids holding the source read lock for the full copy;
* and avoids raw WAL-pairing mistakes.

Every database snapshot should undergo:

* SQLite open;
* schema version validation;
* `PRAGMA quick_check`;
* `PRAGMA foreign_key_check`;
* owner semantic validation;
* manifest hashing;
* and restore test according to verification policy.

A raw VSS shadow copy should not be treated as an application-consistent Opure backup by itself.

Windows Volume Shadow Copy Service may be used later for:

* enterprise backup integration;
* volume-consistent source acquisition;
* or a future Opure VSS writer.

Version 1 should not require an Opure VSS writer.

Opure's own service-aware backup protocol should remain authoritative because the product must know:

* which databases and artefacts form one recovery set;
* which writes were durable;
* which CAS objects are pinned;
* which workflows are unresolved;
* which secrets are portable;
* and which state is rebuildable.

The initial coordinated-backup protocol should use a **Backup Epoch** and a short **Consistency Barrier**.

The flow should be:

```text
Create Backup Epoch
    ↓
Discover participating owner services
    ↓
Ask each service to Prepare Backup
    ↓
Each service flushes durable state and transactional outboxes
    ↓
Each service commits a Backup Checkpoint
    ↓
Each service pins referenced CAS and mutable artefacts
    ↓
Mutating commands enter a short bounded barrier
    ↓
Each SQLite-backed service establishes its Online Backup snapshot
    ↓
Each file-backed service freezes an immutable snapshot reference
    ↓
Barrier releases
    ↓
Snapshots copy incrementally while normal work resumes
    ↓
Owner validators verify each service snapshot
    ↓
Backup Service creates complete manifest
    ↓
Repository or archive commits last
```

The barrier should not remain active for the full database copy.

It should remain only long enough to:

* commit checkpoints;
* establish database snapshot handles or exact source revisions;
* and pin referenced immutable objects.

If an owner service cannot participate within the timeout, the backup set should become:

* Partial;
* Deferred;
* or Failed

according to the selected backup class.

A full-system recovery backup should not be marked Complete when a required owner is missing.

The architecture should not claim a distributed database transaction.

Cross-service consistency should be represented by:

* one Backup Epoch;
* exact owner checkpoints;
* durable outbox and inbox positions;
* service dependency metadata;
* and post-restore reconciliation.

A restored set may contain messages that were:

* committed by the sender but not consumed by the receiver;
* consumed by the receiver but whose sender projection lags;
* or in transit at the backup boundary.

Owner services should use idempotent inbox and outbox recovery to reconcile these states.

The initial backup-state classes should be:

1. **Non-Rebuildable Critical State**
2. **Non-Rebuildable User State**
3. **Recoverable Project State**
4. **Recoverable External State**
5. **Rebuildable Derived State**
6. **Installable Package and Cache State**
7. **Secret State**
8. **Trust and Recovery Evidence**
9. **Ephemeral State**
10. **Prohibited Backup Content**

Examples:

### Non-Rebuildable Critical State

* Configuration Profile revisions;
* policy and activation history;
* workflow state and side-effect evidence;
* project registrations;
* accepted Project Memory;
* user approvals;
* recovery decisions;
* and Trust Evidence required for unresolved effects.

### Non-Rebuildable User State

* UI layouts;
* named Profiles;
* custom workflow definitions;
* custom templates;
* local project preferences;
* and user-authored notes.

### Recoverable Project State

* source-controlled `.opure` files;
* repository history;
* committed source;
* and project configuration

when the repository or another backup remains available.

### Recoverable External State

* provider profile definitions;
* external account references;
* MCP endpoint definitions;
* package sources;
* and repository remotes.

Credentials are separate.

### Rebuildable Derived State

* search indexes;
* embeddings;
* caches;
* projections;
* local metrics;
* and generated previews

when authoritative source and policy are available.

### Installable Package and Cache State

* local model files;
* plugin packages;
* Runtime Packages;
* update packages;
* and tool packages.

The default backup should store manifests, versions, hashes and sources.

The actual package bytes should be optional for offline recovery.

### Secret State

* Vault root key;
* provider keys;
* tokens;
* passwords;
* private keys;
* and destination credentials.

Secret values should be excluded from ordinary portable backups.

### Prohibited Backup Content

* hidden model reasoning;
* transient process memory;
* raw diagnostic dumps unless explicitly selected;
* unrelated user-profile files;
* browser data;
* clipboard;
* process environment;
* and arbitrary operating-system secrets.

The initial backup products should be:

## Local Recovery Point

A channel-local short-retention recovery set used for:

* pre-update;
* pre-schema migration;
* pre-repair;
* pre-compaction;
* and explicit user rollback.

It may include same-user DPAPI-bound Vault files.

It is not a device-loss backup.

## Managed Backup Repository

A user-configured destination containing encrypted content-addressed chunks and synthetic-full manifests.

Each snapshot manifest should independently describe the complete logical recovery set.

A restore should not require replaying a fragile ordered sequence of incremental manifests.

## Portable Backup Archive

A self-contained encrypted archive containing one complete selected recovery set.

It should be suitable for:

* moving to another storage device;
* offline storage;
* and new-machine recovery

subject to key and compatibility rules.

## Data Portability Bundle

A stable, documented, machine-readable export of selected user and project data.

It should not include:

* raw internal databases;
* active service queues;
* executable packages;
* capabilities;
* active approval tokens;
* or automatically resumable workflows.

## Workspace Recovery Pack

A selected project-change artefact containing:

* repository identity;
* base commit;
* tracked diff;
* staged diff;
* selected untracked files;
* file hashes;
* line-ending metadata;
* and conflict warnings.

It should not overwrite a restored repository automatically.

## Vault Recovery Capsule

An optional high-risk encrypted capsule created only by the Secrets Service.

It may contain:

* the Vault database;
* selected secret records;
* Vault schema;
* secret metadata;
* and a rewrapped Vault root key.

It should not contain plaintext secrets.

It should remain separate in:

* policy;
* preview;
* approval;
* retention;
* key recovery;
* and restore.

The initial default behaviour should be:

* no automatic external or cloud backup;
* no managed backup repository until the developer selects and configures a destination;
* mandatory local recovery point before destructive product schema migration;
* mandatory local recovery point before update when persistent-state migration is possible;
* explicit recommendation during onboarding to configure a managed backup repository;
* explicit schedule and retention preview;
* and visible backup-health status.

A first-run wizard should not silently select a cloud-synchronised directory.

A user may configure:

* local fixed disk;
* removable disk;
* network share;
* cloud-synchronised folder;
* or enterprise-managed destination

subject to policy and warnings.

A backup on the same physical device should be labelled:

```text
Same-Device Recovery Only
```

It should not satisfy device-loss or ransomware recovery objectives.

A destination that remains continuously writable by the current user should not be labelled immutable.

The initial ransomware-resilience guidance should recommend:

* at least one offline or otherwise isolated copy;
* encryption;
* regular integrity verification;
* and regular restore tests.

Opure should not claim that it can make an ordinary user-writable folder ransomware proof.

The initial backup repository should use a content-addressed immutable object model.

Suggested structure:

```text
OpureBackupRepository\
├── repository.json
├── keys\
│   └── key-slots.json
├── snapshots\
│   ├── <backup-id>.manifest
│   └── <backup-id>.commit
├── objects\
│   ├── ab\
│   │   └── <object-id>.chunk
│   └── ...
├── indexes\
├── quarantine\
├── verification\
└── locks\
```

The repository should store:

* encrypted object chunks;
* encrypted manifests;
* key-slot metadata;
* non-sensitive repository format metadata;
* verification receipts;
* and commit markers.

The repository should not store plaintext:

* project names;
* absolute paths;
* secret names;
* service payloads;
* or content hashes

in its unauthenticated outer metadata.

The initial chunk size should be:

```text
4 MiB
```

with a separate final partial chunk.

This value is provisional.

Chunking should support:

* streaming;
* bounded memory;
* deduplication within one repository;
* integrity verification;
* cancellation;
* and resumable copying.

The repository should deduplicate by an internal keyed object identity.

A raw plaintext SHA-256 should not be used as a public filename when it would reveal equality across repositories.

Suggested internal object identity:

```text
HMAC-SHA-256(repository-object-id-key, plaintext-chunk-sha256)
```

The plaintext SHA-256 remains inside authenticated encrypted metadata.

The initial repository encryption model should use:

* one random 256-bit Repository Master Key;
* HKDF-SHA-256 to derive purpose-specific keys;
* AES-256-GCM for manifests and objects;
* random 96-bit nonce per encrypted object;
* authenticated associated data containing format, repository, object and version identity;
* and one or more key slots.

Key slots should include:

1. **DPAPI CurrentUser Slot**
2. **Recovery Passphrase Slot**
3. **Random Recovery Key Slot**
4. **Enterprise Recovery Recipient Slot — deferred**

The DPAPI CurrentUser slot should provide convenient same-user access.

It should be labelled:

```text
Bound to this Windows user and normally this computer
```

because Windows DPAPI-protected data usually requires the same user credentials and computer.

The Recovery Passphrase Slot should:

* generate a random salt;
* use PBKDF2-HMAC-SHA-256 through the .NET one-shot `Rfc2898DeriveBytes.Pbkdf2` API;
* derive a 256-bit Key Encryption Key;
* wrap the Repository Master Key with AES-256-GCM;
* use a calibrated work factor;
* and never store the passphrase.

Suggested provisional calibration:

```text
Target derivation time: 750 ms to 2 seconds
Minimum iterations on reference hardware: 600,000
Salt: 128 bits
Derived key: 256 bits
```

The exact work factor requires security and performance evidence.

NIST has announced an intention to revise its password-based key-derivation guidance and to consider an additional memory-hard function.

Therefore, the Backup Encryption Format should version its KDF and allow a later approved memory-hard KDF without rewriting old backups.

The Random Recovery Key Slot should use a cryptographically random 256-bit key stored separately by the developer.

Opure should:

* generate it;
* display a fingerprint;
* export it to a separately selected file or physical medium;
* confirm recovery-key possession;
* and warn that losing every usable key slot makes the backup unrecoverable.

The Recovery Key file should not be stored automatically inside the backup repository.

A managed repository intended for device-loss recovery should require at least one non-DPAPI recovery slot.

The initial portable archive should use the same encrypted object and manifest format inside a strict container.

Suggested extension:

```text
.opure-backup
```

The container may be ZIP64 or another standard streaming container only as a packaging layer.

Security should come from encrypted authenticated Opure objects, not legacy ZIP encryption.

The exact container requires prototype evidence.

The initial Vault Recovery Capsule should use:

* one random 256-bit Capsule Master Key;
* AES-256-GCM;
* HKDF-SHA-256 purpose separation;
* an independent passphrase or random recovery-key slot;
* exact Vault schema and record hashes;
* and a rewrapped Vault root key created inside the Secrets Service.

The Secrets Service should be the only process that:

* unwraps the DPAPI-protected Vault root key;
* rewraps it for the capsule;
* opens a capsule;
* and re-protects the root key under the destination machine's DPAPI context.

The Backup Service should see only:

* encrypted capsule bytes;
* safe inventory counts;
* classification;
* hash;
* and restore requirements.

A Vault Recovery Capsule should be:

* disabled by default;
* explicit;
* separately approved;
* separately retained;
* separately verified;
* excluded from ordinary portability exports;
* and blocked from unattended creation unless enterprise policy and recovery-key custody are configured.

The initial backup manifest should include:

```text
format
format_revision
backup_id
backup_class
repository_id
parent_backup_id
created_at_utc
completed_at_utc
product_version
release_channel
package_identity
backup_epoch
consistency_state
owner_service_checkpoints
database_snapshots
artefact_manifests
workspace_recovery_packs
package_manifests
vault_capsule_reference
retention_policy
encryption_profile
key_slot_fingerprints
data_classifications
exclusions
warnings
verification_state
restore_requirements
manifest_sha256
```

The outer unencrypted header should reveal only:

* format magic;
* format revision;
* repository ID;
* KDF and key-slot metadata;
* encrypted manifest length;
* and integrity fields required to locate and decrypt the manifest.

It should not reveal project names or source paths.

Backup consistency states should be:

* Preparing;
* Barrier Active;
* Snapshot Established;
* Copying;
* Complete;
* Complete with Rebuildable Exclusions;
* Partial;
* Deferred;
* Failed;
* Cancelled;
* Quarantined;
* and Deleted.

Only:

* Complete;
* and Complete with Rebuildable Exclusions

should be eligible for full-system restore.

A Partial backup may support selective restore if every selected owner is complete.

The initial owner-service checkpoint should include:

```text
owner_service
service_version
schema_version
backup_adapter_revision
checkpoint_id
checkpoint_sequence
database_revisions
outbox_position
inbox_position
cas_root
mutable_file_generation
active_migrations
unresolved_operations
created_at_utc
checkpoint_sha256
```

A service should reject `Prepare Backup` when:

* a non-restartable migration is active;
* its database is corrupt;
* a required exclusive operation is active;
* a Vault rotation cannot produce a stable boundary;
* or its backup adapter does not support the current schema.

The Backup Service should show the reason.

The initial backup classes should be:

1. Automatic Pre-Migration Recovery Point
2. Automatic Pre-Update Recovery Point
3. User-Initiated Local Recovery Point
4. Scheduled Managed Backup
5. User-Initiated Managed Backup
6. Portable Full Backup
7. Selective Project Backup
8. Workspace Recovery Pack
9. Vault Recovery Capsule
10. Enterprise Recovery Backup
11. Incident Preservation Backup
12. Test and Exercise Backup

The initial recovery objectives should be expressed by failure mode.

### Process or Service Crash

```text
RPO: zero committed transactions
RTO target: under 2 minutes
Mechanism: service journals, WAL, supervisor and normal startup recovery
```

### Desktop Crash

```text
RPO: zero authoritative Runtime state
RTO target: under 1 minute
Mechanism: reconnect to Runtime
```

### Product Update Failure

```text
RPO: zero pre-migration committed state
RTO target: under 15 minutes
Mechanism: pre-update recovery point and recovery mode
```

### Single Service Database Corruption

```text
RPO: last verified backup for that service
RTO target: under 30 minutes
Mechanism: selective staged service restore
```

### Whole Opure Data-Root Corruption

```text
RPO: last verified Complete backup
RTO target: under 2 hours for core state
Mechanism: full staged restore
```

### Device Loss

```text
RPO: last backup stored off the lost device
RTO target: under 4 hours for core metadata after application reinstall
Mechanism: portable archive or external managed repository
```

### Ransomware or Destructive Malware

```text
RPO: last known-clean offline or isolated backup
RTO target: risk dependent
Mechanism: clean-room recovery, signed reinstall, quarantine and staged restore
```

### Credential Loss

```text
RPO: none for secret values unless a valid Vault Recovery Capsule exists
RTO target: depends on credential reissue
Mechanism: restore metadata then reacquire credentials
```

These are product objectives, not guarantees.

A configured backup schedule should display its expected RPO.

The Backup Health view should state when the current destination cannot meet:

* device-loss;
* offline;
* encryption;
* or ransomware-resilience objectives.

The initial data-portability design should be separate from internal backup.

Suggested extension:

```text
.opure-portable.zip
```

The Portability Bundle should use:

* strict ZIP path rules;
* UTF-8;
* JSON;
* JSON Lines;
* Markdown where human-readable;
* unified diff or Git-compatible patch;
* and documented schemas.

It should include selected:

* user Profiles;
* project registrations;
* source-controlled and project-local settings;
* custom Workflow Definitions;
* Project Memory records and provenance;
* user-authored notes;
* Context and Routing policies;
* Provider Profile metadata without credentials;
* Model Profile metadata and hashes;
* plugin configuration without grants;
* MCP configuration without credentials or trust;
* tool templates;
* project knowledge-source manifests;
* workflow history suitable for archive;
* Trust Evidence exports;
* and workspace change packs.

It should not include:

* raw service databases;
* WAL files;
* active queues;
* active leases;
* active capabilities;
* one-time approvals;
* secret values;
* DPAPI blobs;
* process dumps;
* package executables;
* plugin trust grants;
* MCP trust grants;
* provider bearer tokens;
* or automatically resumable external effects.

A portability import should create:

* candidate Profiles;
* candidate project mappings;
* disabled workflow templates;
* untrusted plugin and MCP references;
* missing-secret requirements;
* and import evidence.

It should not:

* execute code;
* launch plugins;
* start MCP servers;
* resume workflows;
* call providers;
* apply patches;
* overwrite repositories;
* or grant permissions.

The portability format should remain:

* structured;
* commonly processable;
* machine readable;
* documented;
* versioned;
* and free of product-installation requirements where practical.

Opure should not claim that every third-party application can import the format.

The initial Workspace Recovery Pack should support:

* Git repository with tracked changes;
* Git repository with staged changes;
* selected untracked files;
* non-Git project files;
* and deleted-file markers.

It should exclude ignored files by default.

An ignored file may be included only through explicit selection.

The pack should record:

* base commit;
* branch name as metadata;
* remote identity hash;
* working-tree status;
* path classification;
* file mode;
* line-ending class;
* patch hash;
* untracked-file hash;
* and required restore review.

Restoring a Workspace Recovery Pack should:

1. locate or clone the repository separately;
2. verify the base commit where possible;
3. map project root;
4. preview every change;
5. detect conflicts;
6. apply to a new branch or worktree by default;
7. never push;
8. never commit unless explicitly requested;
9. and preserve the pack until verification.

The initial disaster-recovery modes should be:

1. Normal Startup Recovery
2. Safe Mode
3. Service Repair Mode
4. Offline Restore Mode
5. Side-by-Side Recovery Mode
6. Clean-Room Recovery Mode
7. Ransomware-Suspected Mode
8. Data Portability Import Mode
9. Incident Preservation Mode
10. Factory Reset and Rebuild Mode

Offline Restore Mode should start:

* no plugins;
* no MCP servers;
* no provider calls;
* no workflows;
* no automatic update;
* no project writes;
* and no external network

until the Restore Plan permits each boundary.

The initial restore flow should be:

```text
Select backup source
    ↓
Open in dedicated Recovery Host
    ↓
Read minimal outer header
    ↓
Acquire passphrase or recovery key
    ↓
Authenticate and decrypt manifest
    ↓
Validate format, limits and product compatibility
    ↓
Verify every selected object hash and AEAD tag
    ↓
Build Restore Plan
    ↓
Map projects, paths, packages and identities
    ↓
Create new staged data root
    ↓
Restore owner snapshots into staged service roots
    ↓
Run database and semantic validation
    ↓
Run migrations only on staged copies
    ↓
Reconcile outbox, inbox, workflow and deletion ledgers
    ↓
Revalidate plugins, MCP, providers, models and packages
    ↓
Display complete impact and missing requirements
    ↓
Acquire restore approval
    ↓
Stop live Runtime and Desktop
    ↓
Atomically switch to staged data root
    ↓
Start in Recovery Validation Mode
    ↓
Confirm service readiness
    ↓
Keep workflows and external effects paused
    ↓
Commit recovery or roll back to prior data root
```

The Recovery Host should be a dedicated trusted process.

It should:

* use no plugin loading;
* use no MCP;
* use no model execution;
* have network disabled by default;
* parse strict formats;
* enforce archive and path limits;
* never execute files from the backup;
* and write only to a selected staging root.

A backup should be treated as untrusted input even when its encryption authenticates it.

Encryption proves possession of a key and integrity under that key.

It does not prove:

* that the source machine was uncompromised;
* that every record was safe;
* that packages remain trusted;
* or that workflows should resume.

The initial restore should never overwrite the active data root in place.

It should restore to a new same-volume root.

Suggested layout:

```text
%LOCALAPPDATA%\Opure\<Channel>\
├── roots\
│   ├── root-<current>\
│   ├── root-<staged>\
│   └── root-<previous>\
└── current-root.json
```

The exact root-switch mechanism requires ADR-0009 review.

A root switch should occur only when:

* all Opure processes are stopped;
* the staged root is complete;
* hashes and owner validation pass;
* migration passes;
* and an activation marker is durable.

The prior root should remain until:

* post-restore health passes;
* developer confirms;
* and rollback retention expires.

The initial restore activation states should be:

* Planning;
* Source Locked;
* Decrypting;
* Verifying;
* Mapping Required;
* Staging;
* Migrating;
* Validating;
* Approval Required;
* Ready to Activate;
* Activating;
* Recovery Validation;
* Completed;
* Completed with Missing Dependencies;
* Rolled Back;
* Failed;
* Quarantined;
* and Cancelled.

Every restore should produce a **Recovery Report**.

It should include:

* backup identity;
* verification level;
* source class;
* backup age;
* source machine class;
* product version;
* service checkpoints;
* restored owners;
* excluded owners;
* migrations;
* path mappings;
* missing projects;
* missing packages;
* missing models;
* missing plugins;
* missing MCP servers;
* missing credentials;
* paused workflows;
* unresolved external effects;
* deletion-ledger application;
* integrity results;
* activation results;
* rollback root;
* and warnings.

The initial verification levels should be:

1. Created
2. Cryptographically Verified
3. Structurally Verified
4. Semantically Verified
5. Selectively Restore Tested
6. Fully Restore Tested
7. Recovery Exercised

Definitions:

### Created

Manifest and commit marker were written.

### Cryptographically Verified

Every selected object's authenticated encryption and hash passed.

### Structurally Verified

Every database, file and schema opened successfully.

### Semantically Verified

Owner-service validators passed.

### Selectively Restore Tested

Selected owners restored into an isolated root.

### Fully Restore Tested

All included owners restored into an isolated root and reached validation-ready state.

### Recovery Exercised

A planned human-visible recovery exercise met the stated objectives and produced lessons.

The UI should not label a backup merely:

```text
Verified
```

without stating the level.

The initial managed repository should support automatic verification:

* cryptographic verification on every write;
* structural verification on every completed snapshot;
* semantic verification on a configurable schedule;
* selective restore test weekly;
* full restore test monthly where storage permits;
* and recovery exercise quarterly for enterprise policy or manually for individual use.

These defaults are provisional and require resource-budget review.

A restore test should use:

* a disposable isolated root;
* no external network;
* no plugin or MCP execution;
* no workflow resume;
* no project write;
* no provider call;
* and bounded resources.

The initial backup retention recommendation after explicit enablement should be:

```text
7 daily snapshots
4 weekly snapshots
6 monthly snapshots
2 pre-update or pre-migration snapshots
```

The user or enterprise policy may change it.

Retention should be constrained by:

* minimum recovery requirements;
* available storage;
* unresolved incidents;
* Preservation Holds;
* active workflow references;
* erasure requirements;
* and destination policy.

Each managed snapshot should be a synthetic full manifest over shared immutable chunks.

Deleting a snapshot should:

* remove its manifest;
* decrement object references;
* schedule unreferenced chunks;
* respect holds;
* and create a safe deletion receipt.

Backup pruning should not delete:

* the only tested backup;
* a backup required for active rollback;
* a backup under Preservation Hold;
* a backup required by an unresolved recovery incident;
* or a Vault Capsule without explicit secret-retention review.

The initial erasure architecture should include a versioned **Erasure Ledger**.

When controlled data is deleted from live state, the owning service should issue an Erasure Record containing:

* subject;
* owner;
* record selectors;
* data hashes or safe fingerprints;
* deletion time;
* reason;
* backup treatment;
* and expiry.

Backup creation should exclude data already covered by active erasure records.

Restoring an older backup should apply the current Erasure Ledger before the restored root becomes active.

The restore process should:

1. load live or separately preserved erasure records;
2. apply owner-specific purge adapters to staged state;
3. verify deletion or beyond-use state;
4. remove affected CAS references;
5. record restore-time erasure;
6. and block activation on unresolved critical erasure.

Where an immutable backup cannot be edited safely, it may retain deleted data until expiry only under explicit policy.

It should be:

* beyond ordinary use;
* excluded from routine restore;
* subject to the Erasure Ledger on any exceptional restore;
* and deleted according to backup retention.

Opure should not claim immediate forensic erasure from:

* immutable backup media;
* filesystem journals;
* SSD wear levelling;
* cloud-provider versions;
* or external copies.

The initial backup-health model should include:

* Healthy;
* Healthy with Warnings;
* Not Configured;
* Recovery Point Only;
* Same-Device Only;
* No Portable Key;
* Destination Offline;
* Destination Missing;
* Backup Overdue;
* Backup Failed;
* Verification Overdue;
* Restore Test Overdue;
* Retention Conflict;
* Key Recovery Risk;
* Vault Not Portable;
* Partial Backup;
* Corrupt Backup;
* Ransomware Exposure;
* and Recovery Required.

The Trust Centre should expose:

```text
Backup Overview
Recovery Objectives
Destinations
Schedules
Recovery Points
Managed Snapshots
Portable Archives
Workspace Recovery Packs
Vault Recovery Capsules
Verification
Restore Tests
Recovery Exercises
Restore History
Erasure and Backup Retention
Disaster Recovery
Health
```

The selected trust chain is:

```text
Owner service authoritative state
    ↓
Versioned Backup Adapter
    ↓
Backup Epoch and owner checkpoint
    ↓
Short consistency barrier
    ↓
SQLite Online Backup and immutable artefact pinning
    ↓
Encrypted content-addressed objects
    ↓
Complete synthetic-full manifest and commit marker
    ↓
Cryptographic, structural and semantic verification
    ↓
Isolated restore test
    ↓
Staged Restore Plan and path mapping
    ↓
No-side-effect recovery validation
    ↓
Explicit activation and rollback capability
    ↓
Recovery Report and Trust Evidence
```

---

## 2. Status

Current status:

> **Proposed**

This ADR becomes **Accepted** only after prototypes demonstrate:

* owner Backup Adapters;
* Backup Epoch coordination;
* bounded consistency barriers;
* live SQLite Online Backup;
* WAL-safe behaviour;
* owner checkpoint manifests;
* cross-service inbox and outbox reconciliation;
* immutable CAS pinning;
* managed repository format;
* chunking;
* encrypted object storage;
* DPAPI key slot;
* passphrase key slot;
* random recovery-key slot;
* KDF calibration;
* AES-GCM object encryption;
* manifest authentication;
* synthetic-full snapshots;
* snapshot retention;
* workspace recovery packs;
* optional Vault Recovery Capsule;
* strict portable archive parsing;
* data-portability export;
* safe portability import;
* offline Recovery Host;
* staged data-root restore;
* schema migration on copies;
* package and identity revalidation;
* paused-workflow recovery;
* Erasure Ledger application;
* cryptographic verification;
* structural verification;
* semantic verification;
* selective restore test;
* full restore test;
* ransomware-suspected clean-room exercise;
* destination classification;
* same-device warnings;
* offline-copy guidance;
* backup-health UI;
* and adversarial database-copy, WAL mismatch, backup substitution, key-slot, KDF downgrade, nonce reuse, chunk tampering, manifest tampering, archive traversal, decompression, restore-code execution, stale erasure, plugin auto-activation, MCP auto-launch, workflow auto-resume, credential leakage and ransomware tests.

---

## 3. Context

Opure stores critical local state across several service-owned boundaries.

This includes:

* configuration;
* policies;
* profiles;
* project registrations;
* workspace state;
* repository metadata;
* workflow state;
* accepted Project Memory;
* Trust Evidence;
* provider profiles;
* model profiles;
* plugin and MCP configuration;
* indexes;
* caches;
* and Vault secrets.

The platform is intentionally local first.

That improves control but also means the developer is responsible for the durability of data stored on the device unless Opure provides a clear recovery architecture.

A process crash is not the same as a device failure.

SQLite WAL and transactional outboxes can recover from many process and power failures.

They do not protect against:

* disk failure;
* accidental deletion;
* database corruption;
* destructive malware;
* lost device;
* stolen device;
* failed schema migration;
* or user error.

Copying application directories is insufficient because:

* live SQLite files may be inconsistent;
* WAL and journal files may be separated;
* several services may be at different logical points;
* CAS references may be missing;
* secret keys may be machine bound;
* and workflows may have unresolved external effects.

A backup that cannot be restored is not useful.

A restore that automatically resumes external effects can be dangerous.

A portable export designed for interoperability should not expose internal queues, capabilities or machine-bound state.

The product therefore needs separate, explicit contracts for:

* operational recovery;
* backup;
* portability;
* secret recovery;
* and disaster response.

---

## 4. Problem Statement

Opure requires a local-first backup and recovery architecture that produces service-consistent, encrypted and verifiable recovery sets; supports safe same-machine and new-machine restoration; preserves selected uncommitted project work; exports stable machine-readable data without internal authority; keeps secrets machine-bound unless explicitly placed into a separately encrypted recovery capsule; reapplies erasure requirements during restore; and provides exercised clean-room disaster recovery without hidden cloud dependencies, automatic external effects or misleading guarantees.

---

## 5. Decision Drivers

The decision is evaluated against:

* Charter alignment;
* local-first control;
* developer visibility;
* service ownership;
* data durability;
* SQLite correctness;
* cross-service consistency;
* secret safety;
* portability;
* project safety;
* workflow safety;
* ransomware resilience;
* device-loss recovery;
* encryption;
* key recovery;
* backup verification;
* restore testing;
* deletion and erasure;
* privacy;
* performance;
* accessibility;
* testability;
* and small-team implementation.

---

## 6. Governing Principles

This decision must preserve:

* Developer Respect;
* Human in Control;
* Local by Design;
* Owner Services Remain Authoritative;
* Backup Is Not Crash Recovery;
* Portability Is Not Internal Restore;
* Secret Recovery Is Separate;
* No Raw Live Database Copy;
* No Missing WAL Assumption;
* Short Consistency Barriers;
* No Distributed Transaction Claim;
* Synthetic-Full Manifests;
* Encrypted Backup by Default;
* No Recovery Key Stored with Backup;
* DPAPI Binding Is Visible;
* No Secret Portability by Accident;
* No Automatic External Backup;
* No Hidden Cloud Destination;
* Same-Device Is Not Disaster Recovery;
* Offline or Isolated Copies Matter;
* Backup Verification Has Levels;
* Restore Tests Are Required;
* Restore into Staging;
* Never Execute from Backup;
* Never Auto-Resume External Effects;
* Plugins and MCP Revalidate;
* Workflows Restore Paused;
* Current Erasure Ledger Applies;
* Export Is Structured and Machine Readable;
* Import Grants No Authority;
* Old Binaries Are Not Restored as Trusted;
* Recovery Has Rollback;
* and Recovery Exercises Produce Lessons.

---

## 7. Scope

This ADR decides:

* Backup and Recovery Service ownership;
* Backup Adapter contracts;
* state classification;
* Backup Epochs;
* consistency barriers;
* SQLite Online Backup;
* `VACUUM INTO` role;
* CAS and mutable-file snapshots;
* recovery points;
* managed repositories;
* portable archives;
* repository chunking;
* encryption;
* key slots;
* Vault Recovery Capsules;
* manifests;
* destinations;
* schedules;
* retention;
* verification levels;
* restore tests;
* Restore Plans;
* Recovery Host;
* staged data roots;
* path mapping;
* new-machine restore;
* workspace change recovery;
* data portability;
* portability import;
* Erasure Ledger;
* disaster-recovery modes;
* ransomware recovery;
* recovery objectives;
* Trust Centre;
* and acceptance tests.

This ADR does not decide:

* a first-party cloud backup service;
* automatic cloud synchronisation;
* a remote backup account;
* VSS writer implementation;
* enterprise tape integration;
* object-lock vendor integration;
* cloud immutable-storage API;
* cross-device live synchronisation;
* multi-user shared backup repositories;
* external key escrow;
* hardware security module recovery;
* legal records management;
* or guaranteed forensic erasure.

---

## 8. Constraints

Known constraints include:

* ADR-0005 selected service-owned SQLite, explicit migrations and CAS.
* ADR-0007 selected a Vault root key protected with Windows CurrentUser DPAPI.
* ADR-0009 governs path identity, staged writes and archive safety.
* ADR-0013 selected per-user MSIX packaging and durable data outside the package.
* ADR-0014 governs release signing.
* ADR-0015 governs update and recovery hand-off.
* ADR-0016 through ADR-0018 govern plugin and MCP package and trust boundaries.
* ADR-0020 governs local model packages and resources.
* ADR-0022 governs indexes and derived Project Knowledge.
* ADR-0023 governs Project Memory retention and deletion.
* ADR-0025 governs workflow checkpoints, side effects and recovery.
* ADR-0026 governs backup settings and policy.
* ADR-0027 governs Trust Evidence, retention, Preservation Holds and support exports.
* SQLite's Online Backup API can copy a live database into a consistent destination snapshot incrementally;
* SQLite warns that a background raw file copy during a transaction can produce a corrupt backup and that a hot journal or WAL belongs with the main database;
* `VACUUM INTO` produces a consistent compact copy but an interrupted output may be incomplete;
* Windows VSS coordinates snapshot creation for running applications but application-aware consistency requires coordination among requesters, writers and providers;
* Windows DPAPI-protected data normally requires the same user credentials and computer;
* .NET 10 deprecates instance-based `Rfc2898DeriveBytes` construction in favour of one-shot PBKDF2;
* NIST SP 800-132 currently covers password-derived keys for stored data and is scheduled for revision;
* NIST contingency and cyber-event-recovery guidance emphasises planning, prioritisation, tests, exercises and improvement;
* CISA recommends offline, encrypted backups and regular integrity and restore testing for ransomware resilience;
* and current ICO guidance expects structured, commonly used and machine-readable portability formats and requires retention and backup-erasure processes to remain purpose justified.

---

## 9. Assumptions

This decision assumes:

* every first-party owner service can implement a Backup Adapter;
* SQLite backup access can be exposed safely through the pinned native library;
* a short mutating-command barrier is acceptable during snapshot establishment;
* CAS objects are immutable and pinnable;
* backup destinations can be classified;
* users can retain a passphrase or separate recovery key;
* a dedicated Recovery Host can run without normal product integrations;
* services can validate staged restored state;
* workflows can remain paused after restore;
* and a portability bundle can use stable open text schemas.

---

## 10. Current Technical Evidence

Official and primary documentation available on 18 July 2026 establishes that:

* SQLite's Online Backup API can copy a live database incrementally into a consistent snapshot while allowing other database users to continue between copy steps;
* SQLite warns that ordinary file copying during an active transaction can produce a corrupt backup and that hot journal or WAL files must remain paired with their database;
* `VACUUM INTO` produces a consistent compact live-database copy and removes deleted free-page content, but interrupted output can be incomplete;
* VSS coordinates requesters, writers and providers to create point-in-time shadow copies of running application data;
* DPAPI normally binds encrypted data to the same user credentials and machine unless different scope or domain recovery circumstances apply;
* .NET 10 recommends one-shot `Rfc2898DeriveBytes.Pbkdf2` instead of the obsolete streaming constructors;
* NIST SP 800-132 addresses password-based derivation of keys protecting stored data, while NIST has announced that the guidance will be revised;
* NIST SP 800-34 Revision 1 and SP 800-184 treat recovery as a planned, prioritised, tested and continuously improved capability;
* CISA advises maintaining offline encrypted backups and regularly testing their integrity and availability;
* the ICO describes portability as structured, commonly used and machine-readable data transfer;
* and ICO storage-limitation and erasure guidance notes that backup data remains subject to retention and erasure processes, even when immediate physical overwrite is not possible.

These facts support:

* service-aware snapshots;
* incremental SQLite backup;
* explicit consistency metadata;
* portable recovery keys;
* regular restore testing;
* isolated ransomware recovery;
* structured portability;
* and an Erasure Ledger applied during restore.

They do not guarantee:

* that DPAPI data is portable;
* that one offline copy is always clean;
* that a hash proves source trust;
* that a backup can be forensically erased;
* or that one retention schedule satisfies every organisation.

Every selected SQLite, .NET, Windows, cryptographic and privacy assumption must be revalidated before acceptance.

---

## 11. Terminology

### 11.1 Backup Adapter

A versioned owner-service contract for snapshot, validation and restore.

---

### 11.2 Backup Epoch

One cross-service coordination identity.

---

### 11.3 Consistency Barrier

A short period in which mutating commands are held while exact snapshot boundaries are established.

---

### 11.4 Backup Checkpoint

The exact durable owner-service state selected for a backup.

---

### 11.5 Recovery Point

A short-retention local backup used for rollback and repair.

---

### 11.6 Managed Backup Repository

An encrypted content-addressed destination containing many synthetic-full snapshots.

---

### 11.7 Portable Backup Archive

A self-contained encrypted recovery set.

---

### 11.8 Synthetic-Full Snapshot

A complete manifest over shared immutable objects, independent of replaying prior manifests.

---

### 11.9 Backup Object

An immutable encrypted chunk, database snapshot, manifest or artefact.

---

### 11.10 Key Slot

A method of unwrapping a backup master key.

---

### 11.11 Vault Recovery Capsule

A separate encrypted secret-recovery artefact produced and consumed only by the Secrets Service.

---

### 11.12 Workspace Recovery Pack

A selected package of uncommitted project changes.

---

### 11.13 Data Portability Bundle

A stable machine-readable export that does not preserve active runtime authority.

---

### 11.14 Restore Plan

The exact verified mapping and activation plan for one restore.

---

### 11.15 Recovery Host

A restricted process that reads and stages backups without normal product integrations.

---

### 11.16 Staged Data Root

A new isolated data directory built before activation.

---

### 11.17 Recovery Report

The durable explanation of what was restored, excluded, migrated and paused.

---

### 11.18 Verification Level

A precise statement of how thoroughly a backup has been checked.

---

### 11.19 Recovery Exercise

A planned restore test with stated objectives and recorded lessons.

---

### 11.20 Recovery Point Objective

The maximum targeted amount of data loss measured in time or committed state.

---

### 11.21 Recovery Time Objective

The targeted time to restore a capability.

---

### 11.22 Erasure Ledger

Versioned deletion instructions that must be applied to future backups and restored historical backups.

---

### 11.23 Clean-Room Recovery

Restore into a trusted freshly installed environment isolated from suspected compromised state.

---

## 12. Options Considered

The principal architecture options are:

1. Service-aware coordinated backups with staged restore.
2. Raw copy of the complete Opure application-data directory.
3. VSS-only system snapshots.
4. Per-service uncoordinated exports.
5. One monolithic Opure database.
6. Cloud-first backup service.
7. Git as the only backup.
8. SQLite `.backup` or Online Backup per database without a cross-service manifest.
9. `VACUUM INTO` for every database.
10. DPAPI-only encryption for all backups.
11. Unencrypted portable ZIP backups.
12. Windows File History or user backup tooling only.
13. Plain data-portability export as disaster recovery.
14. Full virtual-machine or disk images.
15. Event-log replay as backup.

---

## 13. Option A — Service-Aware Coordinated Backup

### 13.1 Advantages

* correct owner boundaries;
* live consistent databases;
* bounded disruption;
* cross-service checkpoint evidence;
* encrypted incremental storage;
* selective restore;
* staged activation;
* portability separation;
* secret control;
* and recovery testing.

### 13.2 Disadvantages

* many Backup Adapters;
* consistency protocol;
* key management;
* repository format;
* restore migrations;
* and exercise cost.

### 13.3 Decision

Selected.

---

## 14. Raw Application-Data Copy

Rejected because live SQLite, WAL, open files, CAS references and service logical consistency cannot be assumed.

---

## 15. VSS-Only Backup

Deferred as an integration.

VSS can provide a stable volume snapshot, but Opure still needs application-aware checkpoints, manifest semantics, secrets policy and restore validation.

---

## 16. Uncoordinated Service Exports

Insufficient for full recovery because dependencies, outbox positions and shared artefacts would not form one explained set.

---

## 17. Monolithic Database

Rejected by ADR-0005 and bounded-service ownership.

---

## 18. Cloud-First Backup

Rejected because Opure is local first and cloud destinations require explicit policy and user control.

---

## 19. Git Only

Insufficient for configuration, workflows, memory, Trust Evidence, uncommitted files, Vault state and non-repository projects.

---

## 20. Per-Database Online Backup Only

Necessary but insufficient without owner checkpoints, artefact manifests, encryption, retention and staged restore.

---

## 21. `VACUUM INTO` for All Databases

Not selected as the default because the Online Backup API can operate incrementally and normally uses fewer CPU cycles.

`VACUUM INTO` remains a supported special case.

---

## 22. DPAPI-Only Backup Encryption

Rejected for device-loss portability because CurrentUser DPAPI is normally bound to the user and computer.

It remains one convenient key slot.

---

## 23. Unencrypted Portable Archive

Rejected as the normal backup because Opure recovery data may contain personal, confidential and project-sensitive information.

---

## 24. Operating-System Backup Only

Useful as defence in depth but insufficient for Opure-specific consistency, selective restore, workflow safety and key recovery.

---

## 25. Portability Export as Backup

Rejected because a stable export intentionally omits internal recovery queues, service checkpoints and active state.

---

## 26. Full Disk Image

Useful for enterprise recovery but too broad, privacy intensive and platform dependent as the core product format.

---

## 27. Event Replay as Backup

Rejected because not every owner is event sourced and large artefacts, packages, indexes and secrets require separate handling.

---

## 28. Decision

Opure will provisionally adopt:

> **A local trusted Backup and Recovery Service that coordinates versioned owner-service Backup Adapters through a Backup Epoch, short consistency barrier, durable owner checkpoints, SQLite Online Backup snapshots, pinned immutable artefact manifests and transactional outbox positions; stores completed sets as encrypted content-addressed synthetic-full snapshots with explicit DPAPI, passphrase and separate recovery-key slots; distinguishes local Recovery Points, managed repositories, self-contained portable backups, structured Data Portability Bundles, Workspace Recovery Packs and separately approved Vault Recovery Capsules; verifies backups cryptographically, structurally, semantically and through isolated restore tests; restores only into a network-disabled staged data root through a strict Recovery Host, current schema migration, path and identity remapping, package and trust revalidation, Erasure Ledger application and explicit activation with rollback; and leaves workflows, plugins, MCP servers, provider calls and external effects paused until reviewed, while same-device copies are not described as disaster recovery, raw live database copies are prohibited, no cloud destination is selected automatically, and clean offline or isolated recovery remains the required posture for ransomware scenarios.**

This decision is:

* [ ] Permanent until superseded
* [x] Provisional pending consistency, encryption, restore and exercise evidence
* [ ] Approval of a first-party cloud backup service
* [ ] Approval of automatic external backup
* [ ] Approval of automatic workflow resume after restore
* [ ] Approval of DPAPI-only device-loss recovery

---

# 29. Backup and Recovery Service Ownership

The Backup and Recovery Service owns:

* Backup Epoch coordination;
* backup-product definitions;
* destination registration;
* repository format;
* repository keys and key-slot metadata;
* backup schedules;
* snapshot manifests;
* verification orchestration;
* retention;
* restore planning;
* Recovery Host orchestration;
* staged data roots;
* path mapping;
* restore activation;
* rollback;
* portability export;
* portability import;
* recovery exercises;
* and backup health.

---

## 29.1 Non-Responsibilities

The service does not:

* mutate owner state directly;
* read Vault plaintext;
* open arbitrary project files;
* make repository commits;
* resume workflows;
* execute restored packages;
* launch plugins;
* launch MCP servers;
* contact providers;
* or select a cloud destination automatically.

---

# 30. Service Boundary

Conceptual architecture:

```text
Desktop, CLI, Update or Recovery Mode
    ↓
Runtime Gateway
    ↓
Backup and Recovery Service
    ├── Backup Product Catalogue
    ├── Destination Manager
    ├── Epoch Coordinator
    ├── Consistency Barrier
    ├── Repository Manager
    ├── Encryption and Key Slots
    ├── Manifest Builder
    ├── Verification Engine
    ├── Retention Engine
    ├── Restore Planner
    ├── Recovery Host Supervisor
    ├── Portability Export and Import
    ├── Erasure Reconciliation
    └── Exercise Manager
            ↕
Owner-Service Backup Adapters
            ↓
Managed Repository, Portable Archive,
Local Recovery Root or Selected Destination
```

---

## 30.1 No Direct Owner Database Access

The Backup Service should not open another service's live SQLite database directly.

The owner service or a trusted owner-specific helper should create the snapshot.

---

## 30.2 Offline Restore Exception

The Recovery Host may open a staged copy through the owner's restore adapter.

It should not open the live database.

---

## 30.3 No General File Enumeration

Every file is supplied through an owner manifest or explicit Workspace capability.

---

# 31. Backup Service Database

Suggested location:

```text
%LOCALAPPDATA%\Opure\<Channel>\Backup\backup.db
```

Associated directories:

```text
Backup\
├── backup.db
├── recovery-points\
├── staging\
├── imports\
├── exports\
├── quarantine\
├── verification\
├── exercises\
└── recovery\
```

---

## 31.1 Channel Isolation

Stable, Preview and Development use separate metadata and local recovery roots.

---

## 31.2 Cross-Channel Backup

Possible only as an explicit portable archive.

---

## 31.3 Cross-Channel Restore

Creates an import or migration candidate.

No direct activation.

---

# 32. Suggested Tables

```text
backup_product_definitions
backup_product_revisions
backup_adapter_definitions
backup_adapter_revisions
backup_destinations
backup_destination_revisions
backup_schedules
backup_epochs
backup_epoch_participants
backup_barriers
backup_owner_checkpoints
backup_database_snapshots
backup_artefact_manifests
backup_workspace_packs
backup_repository_definitions
backup_repository_key_slots
backup_snapshots
backup_snapshot_objects
backup_object_references
backup_verification_runs
backup_verification_results
backup_restore_tests
backup_retention_policies
backup_retention_decisions
backup_prune_plans
backup_prune_receipts
restore_plans
restore_plan_items
restore_path_mappings
restore_migrations
restore_activation_events
recovery_reports
recovery_exercises
portability_exports
portability_imports
vault_capsule_records
erasure_ledger_references
backup_outbox
backup_inbox
backup_integrity_results
backup_tombstones
```

---

## 32.1 Repository Objects

Stored outside SQLite.

---

## 32.2 Master Keys

Never stored plaintext.

---

## 32.3 Passphrases

Never stored.

---

# 33. Backup Product Definition

Suggested schema:

```text
opure.backup-product-definition/1
```

---

## 33.1 Fields

```text
product_id
revision
display_name
purpose
required_owners
optional_owners
included_state_classes
excluded_state_classes
consistency_requirement
encryption_requirement
key_slot_requirement
verification_requirement
retention_class
restore_modes
approval_policy
created_at
definition_sha256
```

---

## 33.2 Immutable Revision

Required.

---

# 34. Backup Adapter Definition

Suggested schema:

```text
opure.backup-adapter/1
```

---

## 34.1 Fields

```text
adapter_id
revision
owner_service
supported_owner_schemas
backup_state_classes
database_sources
artefact_sources
mutable_file_sources
excluded_sources
prepare_contract
snapshot_contract
validate_contract
restore_contract
migrate_contract
erasure_contract
dependencies
limits
implementation_hash
created_at
adapter_sha256
```

---

## 34.2 First-Party Only

Initially.

---

## 34.3 Plugin Adapter

Deferred.

---

# 35. Owner State Inventory

Every owner classifies each state item as:

* Required;
* Optional;
* Rebuildable;
* External Reference;
* Secret;
* Ephemeral;
* Prohibited;
* or Unsupported.

---

## 35.1 Unknown State

Full backup fails.

---

## 35.2 New Schema Field

Requires adapter revision or explicit safe default.

---

# 36. Required Initial Backup Adapters

* Configuration and Policy;
* Project Registry;
* Workspace Metadata;
* Repository Metadata;
* Patch Service;
* Build and Test Metadata;
* Workflow;
* Context Policy and Plan History;
* AI Routing and Provider Metadata;
* Local Model Profiles;
* Project Knowledge;
* Project Memory;
* Tool Mediation;
* Plugin Platform;
* MCP Gateway;
* Network Gateway Receipts;
* Trust Evidence;
* Update and Release State;
* and Secrets metadata or capsule.

---

# 37. Configuration Adapter

Back up:

* Setting and Policy Definition references;
* user Profile revisions;
* project-local Profiles;
* effective snapshot references;
* change history;
* migration state;
* and activation state.

---

## 37.1 Enterprise Registry Policy

Not copied as authoritative machine policy.

Record safe observed policy provenance.

Restore re-reads current policy.

---

# 38. Project Registry Adapter

Back up:

* project IDs;
* display metadata;
* root references;
* repository identity;
* open-state preferences;
* and project-local lifecycle state.

---

## 38.1 Raw Root

Logical mapping required on restore.

---

# 39. Workspace Adapter

Back up:

* Workspace generations;
* approved staged-write recovery state;
* uncommitted recovery packs where selected;
* and safe file identity metadata.

---

## 39.1 Source Tree

Not copied by default.

---

# 40. Repository Adapter

Back up:

* repository registrations;
* remote metadata with redaction;
* branches and base references;
* local operation receipts;
* and selected Git bundles only under explicit full-project backup.

---

## 40.1 Credentials

Excluded.

---

## 40.2 Git Object Store

External or optional.

---

# 41. Patch Adapter

Back up:

* patch proposals;
* review state;
* approvals;
* application receipts;
* and compensation evidence.

---

## 41.1 Large Patch Content

CAS reference.

---

# 42. Build and Test Adapter

Back up:

* target definitions;
* result summaries;
* important receipts;
* and selected artefacts under policy.

---

## 42.1 Build Cache

Rebuildable.

---

## 42.2 Package Output

Optional.

---

# 43. Workflow Adapter

Back up:

* Definitions;
* Compiled Plans;
* Workflow Events;
* projections;
* checkpoints;
* timers;
* signals;
* approvals;
* effect intents;
* receipts;
* compensation;
* migration plans;
* Recovery Decisions;
* and required CAS objects.

---

## 43.1 Active Workflow

Allowed in backup.

---

## 43.2 Restore

Always paused or Recovery Required.

---

# 44. Context Adapter

Back up:

* user-created Context Policies;
* Context Plan receipts;
* source references;
* and safe context-use history.

---

## 44.1 Source Content

Excluded unless independently selected.

---

# 45. AI Routing Adapter

Back up:

* Routing Policies;
* Provider Profile references;
* model selectors;
* Evaluation Profiles;
* qualification state;
* and receipts.

---

## 45.1 Provider Credentials

Excluded.

---

## 45.2 Current Terms

Revalidated.

---

# 46. Local Model Adapter

Back up:

* Model Profiles;
* Execution Profiles;
* Runtime Package manifests;
* model hashes;
* source locations;
* and local qualification results.

---

## 46.1 Model Bytes

Optional offline package class.

---

## 46.2 Partial Model

Rejected.

---

# 47. Project Knowledge Adapter

Back up:

* index definitions;
* channel configuration;
* generation manifests;
* and source fingerprints.

---

## 47.1 Index Bytes

Rebuildable by default.

Optional for fast restore.

---

## 47.2 Embedding Bytes

Classified and optional.

---

# 48. Project Memory Adapter

Back up:

* accepted memory;
* provenance;
* revisions;
* supersession;
* deletion;
* reminders;
* and context-use receipts.

---

## 48.1 Proposed Memory

Definition specific.

---

## 48.2 Secret-Classified Memory

Prohibited by design.

---

# 49. Tool Mediation Adapter

Back up:

* tool templates;
* approved tool definitions;
* non-secret configuration;
* receipts;
* and pending recovery state.

---

## 49.1 Tool Packages

Manifest by default.

---

# 50. Plugin Platform Adapter

Back up:

* installed package manifests;
* package hashes;
* source;
* trust state;
* permissions requested;
* persistent grants;
* plugin configuration;
* and plugin-owned data according to policy.

---

## 50.1 Plugin Package Bytes

Optional.

---

## 50.2 Grants on Restore

Not automatically active on a new machine.

---

## 50.3 Plugin-Owned Data

Classified and bounded.

---

# 51. MCP Gateway Adapter

Back up:

* server configurations;
* fingerprints;
* transport;
* tool schema hashes;
* account references;
* grants;
* and receipts.

---

## 51.1 Credentials

Excluded.

---

## 51.2 Server Launch on Restore

Denied.

---

# 52. Trust Evidence Adapter

Back up:

* Evidence Type definitions;
* Trust Evidence records;
* relationships;
* retention;
* Preservation Holds;
* incidents;
* support export receipts;
* and recovery evidence.

---

## 52.1 Operational Logs

Separate optional class.

---

# 53. Update and Release Adapter

Back up:

* channel;
* installed product version;
* package identity;
* update state;
* migration state;
* rollback state;
* and post-launch confirmation.

---

## 53.1 Installer Package

Manifest by default.

---

# 54. Secrets Adapter

Ordinary backup:

* Vault schema;
* safe secret-reference inventory;
* credential-required state;
* and DPAPI-bound Vault files for local Recovery Point where policy allows.

Portable secret backup:

* Vault Recovery Capsule only.

---

# 55. Backup Epoch Schema

Suggested:

```text
opure.backup-epoch/1
```

---

## 55.1 Fields

```text
backup_epoch_id
backup_product
destination
requested_by
reason
required_owners
optional_owners
created_at
deadline
barrier_policy
state
epoch_sha256
```

---

# 56. Epoch States

* Created;
* Discovering;
* Preparing;
* Barrier Pending;
* Barrier Active;
* Snapshot Establishing;
* Barrier Released;
* Copying;
* Validating;
* Manifesting;
* Committing;
* Complete;
* Partial;
* Deferred;
* Failed;
* Cancelled;
* Quarantined;
* and Deleted.

---

# 57. Participant Discovery

Use current Runtime service registry.

---

## 57.1 Missing Required Owner

Fail or defer.

---

## 57.2 Missing Optional Owner

Manifest exclusion.

---

## 57.3 Service Starting

Wait boundedly.

---

# 58. Prepare Backup Request

Contains:

```text
epoch
product
required_state_classes
destination_class
deadline
consistency_requirement
verification_requirement
```

---

## 58.1 No Destination Credential

Owner does not need it.

---

# 59. Owner Prepare Response

Contains:

```text
owner
adapter
schema
state_inventory
checkpoint_capability
estimated_database_bytes
estimated_artefact_bytes
estimated_barrier_time
active_exclusions
warnings
ready
```

---

# 60. Prepare Failure

Stable classes:

* Migration Active;
* Database Unhealthy;
* Exclusive Maintenance;
* Unsupported Schema;
* Insufficient Disk;
* Owner Busy;
* Owner Unavailable;
* Secret Rotation Active;
* Policy Denied;
* and Unknown.

---

# 61. Consistency Barrier

The barrier is a trusted Runtime coordination primitive.

---

## 61.1 Scope

Mutating commands for participating owner services.

---

## 61.2 Reads

May continue if they do not prevent snapshot establishment.

---

## 61.3 UI

Show brief state if user-visible delay exceeds threshold.

---

## 61.4 Maximum Duration

Suggested:

```text
5 seconds
```

for ordinary backup.

Pre-migration backup may use:

```text
30 seconds
```

with explicit maintenance UI.

---

# 62. Barrier Acquisition

1. announce epoch;
2. stop admission of new mutating commands;
3. wait for admitted transactions to commit or cancel;
4. flush owner outboxes;
5. create owner checkpoints;
6. establish snapshot handles;
7. pin artefacts;
8. release.

---

## 62.1 Long Operation

Should not remain inside a service database transaction.

---

## 62.2 External Effect

Not paused by the barrier unless its owner contract requires it.

Its durable state is captured.

---

# 63. Barrier Failure

No service should remain permanently blocked.

---

## 63.1 Rollback

Release all acquired participants.

---

## 63.2 Backup State

Failed or Deferred.

---

# 64. SQLite Snapshot Establishment

Owner service should:

1. open destination staging database;
2. call `sqlite3_backup_init`;
3. establish source and destination identity;
4. record start sequence;
5. return established handle or trusted worker identity;
6. release barrier;
7. copy incrementally with `sqlite3_backup_step`;
8. finish;
9. flush destination;
10. close;
11. reopen for validation.

---

## 64.1 Native Access

Use the exact SQLite library pinned by the owner service.

---

## 64.2 Another SQLite Copy

Denied.

---

# 65. Incremental Backup Step

Suggested page batch:

```text
128 pages
```

then yield.

Provisional.

---

## 65.1 Busy Handling

Bounded busy timeout and retry.

---

## 65.2 Progress

Pages copied and total.

---

## 65.3 Cancellation

Stop and discard incomplete destination.

---

# 66. SQLite Destination

Must be:

* new file;
* service staging;
* same owner;
* and restrictive ACL.

---

## 66.1 Existing File

Denied.

---

## 66.2 URI Filenames

Avoid unless required and strictly constructed.

---

# 67. SQLite Snapshot Validation

Required:

```text
open read-only
PRAGMA schema_version
PRAGMA user_version
PRAGMA quick_check
PRAGMA foreign_key_check
owner semantic validator
```

---

## 67.1 Full Integrity Check

Scheduled verification or high-risk backup.

---

## 67.2 WAL File in Snapshot

The finished backup database should be self-contained.

Do not ship the live source WAL.

---

# 68. `VACUUM INTO`

Allowed when:

* owner adapter selects it;
* destination is new;
* enough free space exists;
* interruption cleanup is defined;
* and compact privacy-oriented output is beneficial.

---

## 68.1 Deleted Content

Useful for removing free-page remnants from the generated backup copy.

---

## 68.2 Interruption

Incomplete output is discarded.

---

# 69. VSS Integration

Version 1 status:

```text
Deferred Integration
```

---

## 69.1 System Backup

Enterprise system backup may capture Opure files.

---

## 69.2 Opure Marker

Opure may expose a latest verified backup-health file for external backup tools.

---

## 69.3 No App-Consistency Claim

Without an accepted Opure VSS writer and restore contract.

---

# 70. Immutable Artefact Pinning

Owner returns:

* CAS namespace;
* root manifest;
* object IDs;
* hashes;
* sizes;
* classification;
* and pin lease.

---

## 70.1 Pin Lease

Valid until copy complete or cancelled.

---

## 70.2 GC

Cannot remove pinned objects.

---

# 71. Mutable File Snapshot

Owner should first convert mutable state to:

* immutable staged copy;
* database snapshot;
* or generation manifest.

---

## 71.1 Direct Mutable Copy

Avoid.

---

# 72. Package Manifests

Record:

* package type;
* ID;
* version;
* hash;
* publisher;
* source;
* signature state;
* and reinstall availability.

---

## 72.1 Bytes Included

Explicit object class.

---

# 73. Rebuildable Exclusion

Manifest records:

* owner;
* state class;
* reason;
* rebuild source;
* expected cost;
* network requirement;
* and risk.

---

## 73.1 Complete with Rebuildable Exclusions

Only when every exclusion is proven rebuildable.

---

# 74. Backup Destination Schema

Suggested:

```text
opure.backup-destination/1
```

---

## 74.1 Fields

```text
destination_id
revision
display_name
destination_type
logical_location
filesystem_identity
capacity
free_space
removable
network
cloud_synchronised
continuous_write_access
offline_capability
immutability_claim
encryption_requirement
credentials_reference
health
created_at
destination_sha256
```

---

# 75. Destination Types

* Local Fixed Disk;
* Local Separate Physical Disk;
* Removable Disk;
* Network Share;
* Cloud-Synchronised Folder;
* Enterprise-Managed Path;
* Managed Backup Repository;
* Portable File;
* Test Repository;
* and Unsupported.

---

# 76. Destination Identity

Use ADR-0009 handle-verified identity.

---

## 76.1 Drive Letter

Not sufficient.

---

## 76.2 Removable Reconnect

Volume identity and repository ID.

---

# 77. Same Physical Device Detection

Best effort.

---

## 77.1 Unknown

Show Unknown.

---

## 77.2 Same Device

Label.

---

## 77.3 Separate Device

Does not imply offline or immutable.

---

# 78. Network Share

Allowed only under policy.

---

## 78.1 SQLite Repository Database

Avoid shared mutable SQLite on network.

Repository indexes should be rebuildable files or locally staged.

---

## 78.2 Object Writes

Use staged object and commit marker.

---

## 78.3 Interruption

Resumable.

---

# 79. Cloud-Synchronised Folder

Allowed with warning.

---

## 79.1 Sync State

Opure cannot guarantee upload completion.

---

## 79.2 Versioning

Provider behaviour external.

---

## 79.3 Credential

Opure does not need cloud credential when writing ordinary folder.

---

# 80. Removable Disk

Recommended for offline copy.

---

## 80.1 Safe Eject

Show flush completion.

---

## 80.2 Removal During Backup

Fail current set, retain previous committed sets.

---

# 81. Destination Health

States:

* Healthy;
* Low Space;
* Missing;
* Read Only;
* Permission Denied;
* Repository Locked;
* Repository Corrupt;
* Key Unavailable;
* Network Unavailable;
* Sync Unknown;
* Same-Device Only;
* Not Offline;
* Policy Denied;
* and Unsupported.

---

# 82. Destination Capacity Planning

Estimate:

* new unique chunks;
* manifest;
* verification overhead;
* staging;
* and safety reserve.

---

## 82.1 Safety Reserve

Suggested:

```text
max(5 GiB, 10% of destination capacity)
```

Provisional.

---

# 83. Repository Definition

Suggested schema:

```text
opure.backup-repository/1
```

---

## 83.1 Fields

```text
repository_id
format_revision
created_at
created_by
destination
chunk_size
object_id_scheme
encryption_scheme
kdf_schemes
key_slots
retention_policy
last_verified_at
health
repository_header_sha256
```

---

# 84. Repository Lock

One writer.

---

## 84.1 Read Verification

May run concurrently where safe.

---

## 84.2 Stale Lock

Use process and boot identity plus lease.

---

# 85. Object Chunk Schema

Conceptual:

```text
magic
format_revision
repository_id
object_id
object_type
plaintext_length
ciphertext_length
nonce
associated_data_hash
ciphertext
authentication_tag
```

---

## 85.1 Outer Object ID

Keyed.

---

## 85.2 Plain Hash

Encrypted metadata.

---

# 86. Object Types

* Database Chunk;
* CAS Chunk;
* File Chunk;
* Manifest;
* Workspace Pack;
* Package;
* Vault Capsule;
* Recovery Report;
* Erasure Ledger;
* and Verification Artefact.

---

# 87. Compression

Compress before encryption where useful.

---

## 87.1 Algorithm

A stable reviewed algorithm such as Deflate or Zstandard requires selection.

---

## 87.2 Secret Compression

Potential side-channel is offline and no attacker-controlled oracle should exist.

Still review.

---

## 87.3 Already Compressed

Store.

---

# 88. Encryption Profile

Suggested schema:

```text
opure.backup-encryption-profile/1
```

---

## 88.1 Fields

```text
profile_id
revision
content_cipher
key_size
nonce_size
tag_size
hkdf
object_id_mac
kdf_options
required_key_slots
created_at
profile_sha256
```

---

# 89. Repository Master Key

Random 256-bit.

---

## 89.1 Generation

Secrets Service or trusted cryptography service.

---

## 89.2 Memory

Zero after use where practical.

---

## 89.3 Rotation

New key version for new objects.

---

# 90. Derived Keys

Use HKDF-SHA-256 for:

* object encryption;
* manifest encryption;
* object identity HMAC;
* key-slot authentication;
* and repository metadata authentication.

---

## 90.1 Domain Separation

Explicit labels.

---

# 91. AES-GCM

Use:

* 256-bit key;
* 96-bit random nonce;
* 128-bit tag;
* and authenticated associated data.

---

## 91.1 Nonce Reuse

Catastrophic and prohibited.

---

## 91.2 Object Write

Immutable once committed.

---

## 91.3 Random Source

OS cryptographic RNG.

---

# 92. DPAPI Key Slot

Fields:

```text
slot_id
slot_type
scope
protected_master_key
optional_entropy_version
created_at
fingerprint
```

---

## 92.1 Scope

CurrentUser.

---

## 92.2 LocalMachine

Not selected because any user on that machine may be able to decrypt if they can access the blob and application context.

---

## 92.3 Portability

No guarantee.

---

# 93. Passphrase Key Slot

Fields:

```text
slot_id
slot_type
kdf
hash
salt
iterations
derived_key_size
wrap_cipher
nonce
wrapped_master_key
tag
created_at
fingerprint
```

---

## 93.1 Passphrase Minimum

Prefer strength guidance and confirmation rather than arbitrary composition rules.

---

## 93.2 Empty Passphrase

Denied.

---

## 93.3 Clipboard

Avoid automatic retention.

---

# 94. PBKDF2 Calibration

At repository creation:

1. benchmark one-shot PBKDF2-HMAC-SHA-256;
2. calculate iterations for target time;
3. enforce minimum;
4. cap denial-of-service maximum;
5. store exact iterations;
6. verify.

---

## 94.1 Restore

Use stored exact parameters.

---

## 94.2 Downgrade

New slot cannot use weaker-than-policy parameters.

---

# 95. Random Recovery Key Slot

Fields:

```text
slot_id
slot_type
key_fingerprint
wrap_cipher
nonce
wrapped_master_key
tag
created_at
```

---

## 95.1 Recovery Key File

Contains:

* format;
* random key;
* repository ID;
* key fingerprint;
* creation time;
* and checksum.

---

## 95.2 Encryption

The recovery-key file itself may be stored on an encrypted device.

---

# 96. Key Slot Management

Operations:

* Add;
* Verify;
* Replace;
* Disable;
* Remove;
* Rotate;
* and Recover.

---

## 96.1 Last Usable Slot

Cannot remove without explicit destructive confirmation.

---

## 96.2 Slot Verification

Must unwrap and verify a repository test object.

---

# 97. Key-Loss Warning

Trust Centre should state:

* usable slots;
* last verification;
* portable slot present;
* recovery file confirmed;
* and risk.

---

# 98. Repository Key Rotation

Create new master key version.

---

## 98.1 Existing Objects

May remain under old key.

---

## 98.2 Background Re-encryption

Deferred or explicit maintenance.

---

## 98.3 Restore

Manifest identifies key version per object.

---

# 99. Backup Manifest Schema

Suggested:

```text
opure.backup-manifest/1
```

---

## 99.1 Canonicalisation

Strict canonical JSON or deterministic binary form.

---

## 99.2 Encryption

Full manifest encrypted.

---

## 99.3 Hash

Plain canonical manifest hash stored inside authenticated envelope.

---

# 100. Manifest Owner Entry

Contains:

```text
owner
required
adapter
service_version
schema_version
checkpoint
database_objects
artefact_roots
mutable_file_objects
exclusions
validation
restore_order
```

---

# 101. Commit Marker

Written last.

---

## 101.1 Fields

```text
backup_id
manifest_object_id
manifest_ciphertext_sha256
completed_at
verification_level
commit_sha256
```

---

## 101.2 Missing Commit

Incomplete snapshot ignored.

---

# 102. Synthetic-Full Snapshot

Every committed manifest lists all objects required for its logical restore.

---

## 102.1 Parent ID

For history and optimisation only.

---

## 102.2 Restore Dependency

Objects, not parent manifests.

---

# 103. Object Reference Count

Rebuildable by scanning committed manifests.

---

## 103.1 Index Corruption

Rebuild.

---

## 103.2 Delete

Two-phase prune.

---

# 104. Backup Schedule Schema

Suggested:

```text
opure.backup-schedule/1
```

---

## 104.1 Fields

```text
schedule_id
backup_product
destination
frequency
time_window
idle_requirement
power_requirement
network_requirement
retention
verification
missed_run_policy
notification
created_at
schedule_sha256
```

---

# 105. Schedule Timing

Use local time for user intent and record UTC execution.

---

## 105.1 DST

Show exact next run.

---

## 105.2 Sleep

Missed-run policy.

---

# 106. Power Policy

Options:

* Run on AC only;
* Allow Battery above threshold;
* Always;
* and Ask.

---

## 106.1 Default

AC only for large scheduled backup.

---

# 107. Network Policy

Network destinations may require:

* connected;
* metered-policy check;
* VPN class;
* and enterprise policy.

---

# 108. Missed Run

Options:

* Run at next idle;
* Run at next launch;
* Skip;
* and Ask.

---

## 108.1 Overdue

Visible.

---

# 109. Backup Admission

Scheduler checks:

* CPU;
* memory;
* disk;
* destination;
* power;
* network;
* active high-priority work;
* maintenance;
* and policy.

---

# 110. Backup Priority

Classes:

* Mandatory Migration;
* User Foreground;
* Recovery Critical;
* Scheduled Standard;
* Verification;
* and Exercise.

---

# 111. Backup Cancellation

Persist request.

---

## 111.1 Barrier

Release immediately.

---

## 111.2 SQLite Backup

Finish or abort safely.

---

## 111.3 Objects

Uncommitted objects may remain orphan candidates.

---

## 111.4 Committed Previous Snapshot

Unaffected.

---

# 112. Backup Resume

Copying to a repository may resume object transfer.

---

## 112.1 Database Snapshot

If local staged snapshot is complete.

---

## 112.2 Interrupted Live Snapshot

Recreate from new Backup Epoch.

---

## 112.3 Manifest

New backup identity if source checkpoints changed.

---

# 113. Backup Retry

Retryable:

* destination temporarily unavailable;
* transient network;
* temporary busy;
* and removable reconnected.

Non-retryable without review:

* hash conflict;
* key failure;
* schema unsupported;
* source corruption;
* policy denial;
* and suspected repository tampering.

---

# 114. Local Recovery Points

Suggested location:

```text
%LOCALAPPDATA%\Opure\<Channel>\Backup\recovery-points\
```

---

## 114.1 Encryption

Repository encryption or restrictive local encryption.

---

## 114.2 Same-Device Label

Required.

---

## 114.3 Retention

Short.

---

# 115. Pre-Migration Recovery Point

Created before first irreversible schema change.

---

## 115.1 Migration Code

Cannot run until recovery point reaches Structural Verification.

---

## 115.2 Failure

Update or migration stops.

---

# 116. Pre-Update Recovery Point

Created before update only when persistent-state compatibility requires it.

---

## 116.1 No State Migration

May use current recent verified recovery point.

---

# 117. Recovery Point Cleanup

Retain:

* current pre-update;
* prior successful update;
* current migration;
* and user-pinned point.

---

# 118. Workspace Recovery Pack Schema

Suggested:

```text
opure.workspace-recovery-pack/1
```

---

## 118.1 Fields

```text
pack_id
project
repository
base_commit
branch_metadata
tracked_patch
staged_patch
untracked_files
deleted_files
file_modes
line_endings
classifications
created_at
pack_sha256
```

---

# 119. Git Tracked Patch

Use a deterministic Git-compatible patch representation where possible.

---

## 119.1 Binary Changes

Use binary object inclusion or Git bundle under explicit policy.

---

## 119.2 Renames

Record.

---

# 120. Staged and Unstaged

Separate patches.

---

## 120.1 Restore Order

Base, staged, unstaged.

---

# 121. Untracked Files

Explicit selection or policy.

---

## 121.1 Ignored

Excluded by default.

---

## 121.2 Secret Scan

Required.

---

# 122. Non-Git Project

Selected files and directory manifest.

---

## 122.1 Full Project

Explicit.

---

## 122.2 Default

Changed and important files only where change tracking exists.

---

# 123. Workspace Pack Restore

Default target:

* new branch;
* new worktree;
* or new directory.

---

## 123.1 Existing Working Tree

No silent overwrite.

---

## 123.2 Conflict

Review.

---

# 124. Vault Recovery Capsule Schema

Suggested:

```text
opure.vault-recovery-capsule/1
```

---

## 124.1 Fields

Encrypted:

```text
capsule_id
vault_schema
vault_database
vault_root_key_wrapped
secret_record_inventory
record_hashes
created_at
source_vault_generation
restore_policy
capsule_sha256
```

---

## 124.2 Outer Header

Minimal key-slot metadata.

---

# 125. Vault Capsule Creation

1. explicit request;
2. show secret count and categories;
3. select records or full Vault;
4. choose independent recovery mechanism;
5. verify passphrase or key;
6. Secrets Service creates stable Vault snapshot;
7. unwrap root key inside Secrets Service;
8. rewrap under Capsule Master Key;
9. encrypt capsule;
10. scan metadata;
11. verify restore in isolated Secrets test;
12. export or attach;
13. delete staging.

---

# 126. Vault Capsule Restore

1. isolated Secrets Service;
2. decrypt capsule;
3. validate Vault database;
4. verify record hashes;
5. map secret references;
6. rewrap root key under destination CurrentUser DPAPI;
7. mark credentials requiring destination revalidation;
8. activate only after user review.

---

## 126.1 External Account Token

May be expired or revoked.

---

## 126.2 New Machine

Reauthentication may still be required.

---

# 127. Capsule Loss

No recovery without valid key slot.

---

## 127.1 Forgotten Passphrase

No bypass.

---

# 128. Data Portability Bundle Schema

Suggested:

```text
opure.data-portability-bundle/1
```

---

## 128.1 Manifest

Contains:

```text
bundle_id
format_revision
exported_at
product_version
scope
data_categories
schemas
files
hashes
classifications
redactions
exclusions
import_requirements
manifest_sha256
```

---

# 129. Portability Layout

```text
manifest.json
README.md
profiles/
configuration/
projects/
workflows/
memory/
knowledge/
providers/
models/
tools/
plugins/
mcp/
trust/
workspace/
schemas/
integrity/
```

---

# 130. Portability Format

Prefer:

* JSON;
* JSONL;
* Markdown;
* CSV only where tabular;
* unified diff;
* and binary files only when explicitly selected.

---

## 130.1 Proprietary Internal Database

Excluded.

---

# 131. Portability Export Scope

Options:

* User;
* One Project;
* Several Projects;
* One Workflow;
* Memory;
* Configuration;
* Trust Evidence;
* and Custom.

---

# 132. Portability Export Preview

Show:

* categories;
* files;
* personal data;
* project names;
* source snippets;
* memory content;
* provider metadata;
* plugin and MCP metadata;
* Trust Evidence;
* and exclusions.

---

## 132.1 Secrets

Never in ordinary bundle.

---

# 133. Portability Integrity

SHA-256 file hashes and manifest.

---

## 133.1 Signature

Deferred.

---

## 133.2 Encryption

Optional export encryption or user-selected secure destination requires a separate format decision.

Ordinary portability is a user-readable export.

---

# 134. Portability Import

Run in import mode.

---

## 134.1 Strict Archive Rules

ADR-0027 and ADR-0009.

---

## 134.2 No Execution

Required.

---

# 135. Import Mapping

Map:

* Profile IDs;
* project IDs;
* root paths;
* workflow IDs;
* provider references;
* model references;
* plugin IDs;
* MCP IDs;
* and memory identities.

---

## 135.1 Collision

New local ID or explicit merge.

---

# 136. Import Authority

Imported items become:

* Candidate;
* Disabled;
* Historical;
* or Untrusted Reference.

---

## 136.1 Capability

None.

---

## 136.2 Approval

Fresh.

---

# 137. Import Validation

* format;
* path;
* hash;
* schema;
* classification;
* secret scan;
* size;
* cross-project;
* owner support;
* and future version.

---

# 138. Import History

Preserve:

* source bundle;
* source product version;
* imported time;
* mapping;
* conflicts;
* skipped items;
* and actor.

---

# 139. Restore Plan Schema

Suggested:

```text
opure.restore-plan/1
```

---

## 139.1 Fields

```text
restore_plan_id
source_backup
source_verification
source_product_version
destination_channel
restore_mode
selected_owners
selected_projects
path_mappings
identity_mappings
package_mappings
secret_policy
workflow_policy
network_policy
migration_plan
erasure_plan
activation_plan
rollback_plan
preview_sha256
approval
created_at
expires_at
plan_sha256
```

---

# 140. Restore Modes

* Full In-Place Replacement;
* Side-by-Side Full Restore;
* Selective Service Restore;
* Selective Project Restore;
* Workspace Pack Restore;
* Metadata-Only Restore;
* New Machine Restore;
* Clean-Room Restore;
* Incident Examination;
* and Portability Import.

---

# 141. Restore Source Lock

Prevent source modification during read.

---

## 141.1 Managed Repository

Read snapshot objects immutable.

---

## 141.2 Portable File

Open verified handle.

---

# 142. Recovery Host

Suggested executable:

```text
Opure.RecoveryHost.exe
```

---

## 142.1 Launch

Supervised by a minimal bootstrap.

---

## 142.2 Network

Disabled by default.

---

## 142.3 Plugin and MCP

Unavailable.

---

## 142.4 Environment

Minimal.

---

## 142.5 Working Directory

Recovery staging.

---

# 143. Recovery Host Sandbox

Use:

* dedicated process;
* restrictive token or AppContainer where compatible;
* Job Object;
* no child process;
* no network;
* no project write outside staging;
* and exact source and destination handles.

---

## 143.1 Cryptography

May call trusted Secrets helper.

---

# 144. Restore Parsing

Treat all backup data as untrusted.

---

## 144.1 Limits

* object count;
* manifest size;
* nesting;
* strings;
* paths;
* chunks;
* decompression;
* databases;
* and total output.

---

# 145. Backup Version Compatibility

States:

* Directly Supported;
* Supported with Migration;
* Requires Intermediate Opure Version;
* Newer Unsupported;
* Too Old Unsupported;
* and Corrupt or Unknown.

---

## 145.1 Old Binaries

Never execute from backup.

---

## 145.2 Intermediate Version

Use signed installer obtained separately.

---

# 146. Staged Data Root

Created with restrictive ACLs.

---

## 146.1 Existing Root

Never reused.

---

## 146.2 Low Disk

Fail before destructive action.

---

# 147. Restore Database

For each owner:

1. decrypt objects;
2. reconstruct snapshot file;
3. verify hash;
4. open read-only;
5. quick check;
6. foreign key check;
7. owner schema check;
8. copy into owner staging;
9. run migrations;
10. run semantic validator;
11. close and hash final staged state.

---

# 148. Restore CAS

Reconstruct only referenced objects.

---

## 148.1 Hash

Verify plaintext.

---

## 148.2 Duplicate

Deduplicate in destination.

---

# 149. Restore Package Bytes

Never activate directly.

---

## 149.1 Quarantine

Verify:

* package format;
* hash;
* signature;
* publisher;
* source;
* compatibility;
* and current policy.

---

# 150. Restore Model Bytes

Verify:

* exact hash;
* format;
* model manifest;
* source;
* licence metadata;
* Runtime compatibility;
* and current policy.

---

## 150.1 Load

Only after restore activation and explicit model validation.

---

# 151. Restore Plugin State

Package remains disabled until revalidated.

---

## 151.1 Grants

New-machine restore requires fresh review.

---

## 151.2 Same-machine Rollback

May retain compatible grants only under exact package and policy identity.

---

# 152. Restore MCP State

Configurations restored disabled.

---

## 152.1 Credentials

Reacquire.

---

## 152.2 Server Fingerprint

Revalidate.

---

# 153. Restore Provider State

Provider Profile metadata restored.

---

## 153.1 Credentials

Reacquire unless capsule includes them.

---

## 153.2 Terms and Region

Revalidate.

---

# 154. Restore Workflows

All non-terminal workflows become:

```text
Recovery Required
```

---

## 154.1 Pure Pending Work

May later resume after review.

---

## 154.2 External Effect

Reconcile.

---

## 154.3 Approval

Revalidate.

---

## 154.4 Timer

Recalculate but do not fire before activation review.

---

# 155. Restore Trust Evidence

Preserve authority and source availability.

---

## 155.1 Owner Missing

Mark.

---

## 155.2 Hash Chain

Verify.

---

# 156. Restore Configuration

Apply current Product and Enterprise Policy.

---

## 156.1 Old Requested Value

May be denied.

---

## 156.2 Effective Snapshot

Rebuild.

---

# 157. Restore Project Mapping

Every project root must be:

* found;
* selected;
* cloned separately;
* remapped;
* or marked unavailable.

---

## 157.1 Same Path String

Not sufficient.

---

## 157.2 Project Identity

Verify repository or explicit user mapping.

---

# 158. New-Machine Restore

Requires:

* signed Opure install;
* channel selection;
* recovery key;
* project path mapping;
* package revalidation;
* credential recovery or re-entry;
* and current policy.

---

## 158.1 Machine-Specific Settings

Recalculate.

---

## 158.2 Hardware Profiles

Requalify.

---

# 159. Identity Mapping

Map:

* Windows user;
* machine;
* project;
* repository;
* provider account;
* MCP account;
* and Vault references.

---

## 159.1 Automatic Mapping

Only exact safe identities.

---

## 159.2 Ambiguous

Ask.

---

# 160. Erasure Ledger Schema

Suggested:

```text
opure.erasure-ledger/1
```

---

## 160.1 Fields

```text
erasure_id
owner_service
scope
selectors
safe_fingerprints
deleted_at
reason
backup_policy
restore_policy
expires_at
erasure_sha256
```

---

# 161. Erasure Sources

* user deletion;
* project deletion;
* privacy request;
* retention expiry;
* security purge;
* secret rotation;
* and policy migration.

---

# 162. Backup-Time Erasure

Owner Adapter excludes erased records.

---

## 162.1 Stale Backup Source

Reject or apply purge transform.

---

# 163. Restore-Time Erasure

Current ledger applies after reconstruction and before activation.

---

## 163.1 Missing Ledger

Use preserved deletion records from backup plus current system.

---

## 163.2 Conflict

Fail closed for personal or secret data.

---

# 164. Restore Activation Plan

Contains:

* services;
* dependency order;
* staged root;
* current root;
* rollback root;
* network gates;
* plugin gates;
* MCP gates;
* workflow gates;
* project write gates;
* health checks;
* and confirmation deadline.

---

# 165. Service Restore Order

Suggested:

1. Bootstrap and Configuration
2. Secrets metadata and Vault
3. Project Registry
4. Workspace and Repository Metadata
5. Provider, Model, Plugin and MCP Metadata
6. Context, Knowledge and Memory
7. Workflow
8. Tool, Patch, Build and Test
9. Trust Evidence
10. Desktop State

Exact order may be refined.

---

# 166. Root Activation

1. stop Desktop;
2. stop Runtime services;
3. flush current root;
4. write activation intent;
5. move current pointer to previous;
6. move staged pointer to current;
7. flush root metadata;
8. start minimal Runtime;
9. validate;
10. confirm or roll back.

---

## 166.1 Power Loss

Activation intent enables deterministic recovery.

---

# 167. Recovery Validation Mode

Allows:

* database reads;
* integrity checks;
* configuration resolution;
* project mapping;
* package verification;
* and Trust Centre.

Denies:

* remote network;
* model inference;
* tools;
* workspace writes;
* plugin execution;
* MCP;
* update;
* and workflow dispatch.

---

# 168. Recovery Confirmation

Developer reviews:

* health;
* missing dependencies;
* paused workflows;
* project mappings;
* credentials;
* policy changes;
* and rollback.

---

## 168.1 Timeout

Keep Recovery Validation Mode.

---

# 169. Restore Rollback

Switch back to previous root.

---

## 169.1 New External Effect

None should have occurred before confirmation.

---

## 169.2 New Local Changes

Blocked or separately captured.

---

# 170. Selective Service Restore

Allowed only when dependencies are understood.

---

## 170.1 Example

Configuration database only.

---

## 170.2 Workflow Database

Requires Trust, CAS and owner dependency review.

---

## 170.3 Vault

Secrets Service only.

---

# 171. Selective Project Restore

Restore project-scoped records into a new or existing project mapping.

---

## 171.1 Cross-Project

No silent merge.

---

## 171.2 ID Collision

New project identity or reviewed merge.

---

# 172. Disaster-Recovery Plan Schema

Suggested:

```text
opure.disaster-recovery-plan/1
```

---

## 172.1 Fields

```text
plan_id
revision
failure_scenarios
critical_services
dependencies
rpo
rto
backup_sources
recovery_order
clean_environment_requirements
credential_requirements
communication
validation
exercise_schedule
owners
created_at
plan_sha256
```

---

# 173. Failure Scenarios

* Process Crash;
* Application Data Corruption;
* Failed Update;
* Failed Migration;
* Disk Failure;
* Device Loss;
* Account Loss;
* Credential Loss;
* Malware;
* Ransomware;
* Accidental Deletion;
* Backup Repository Corruption;
* and Regional or Site Loss for enterprise environments.

---

# 174. Business Impact Analysis

Identify:

* critical state;
* acceptable data loss;
* acceptable downtime;
* dependencies;
* manual alternatives;
* and recovery priority.

---

## 174.1 Individual Developer

Simplified BIA.

---

## 174.2 Enterprise

Policy-controlled documented BIA.

---

# 175. Recovery Tiers

## 175.1 Tier 0 — Bootstrap

Install and start repair tools.

---

## 175.2 Tier 1 — Core Control

Configuration, Secrets metadata and Project Registry.

---

## 175.3 Tier 2 — Active Work

Workspace changes, workflows and memory.

---

## 175.4 Tier 3 — Integrations

Providers, plugins, MCP and tools.

---

## 175.5 Tier 4 — Derived State

Indexes, caches and model packages.

---

## 175.6 Tier 5 — Historical Evidence

Logs and long-term history.

---

# 176. Clean-Room Recovery

Requirements:

* trusted clean Windows environment;
* signed current or compatible Opure installer;
* isolated network initially;
* known-clean backup;
* recovery key;
* destination storage;
* and project-source access after validation.

---

## 176.1 Suspect Backup

Quarantine and inspect.

---

## 176.2 Current Device

Do not trust until remediated.

---

# 177. Ransomware-Suspected Mode

Default:

* no writes to existing backup repository;
* no mounting old repository read-write;
* no network;
* no plugin;
* no MCP;
* no package execution;
* no workflow resume;
* and no credential use.

---

## 177.1 Select Backup

Use last known-clean point before incident window.

---

## 177.2 Cleanliness

Cannot be proven only by backup age.

---

# 178. Repository Quarantine

Copy or mount read only.

---

## 178.1 Verification

All objects.

---

## 178.2 Malicious Content

Still possible in source and data.

---

# 179. Signed Reinstall

Use current trusted distribution and code-signing checks.

---

## 179.1 Installer from Backup

Not trusted by location alone.

---

# 180. Recovery Communications

Individual mode:

* local status;
* progress;
* and actions.

Enterprise mode may add:

* incident owner;
* stakeholders;
* and external communication plan.

---

# 181. Recovery Exercise Schema

Suggested:

```text
opure.recovery-exercise/1
```

---

## 181.1 Fields

```text
exercise_id
plan
scenario
backup
objectives
participants
start_time
end_time
rpo_result
rto_result
validation
failures
lessons
actions
state
exercise_sha256
```

---

# 182. Exercise Types

* Tabletop;
* Selective Restore;
* Full Isolated Restore;
* New-Machine Restore;
* Ransomware Clean-Room;
* Credential-Loss;
* Repository-Corruption;
* and Update-Rollback.

---

# 183. Exercise Safety

Use:

* test destination;
* no production write;
* no external effects;
* no provider calls;
* and synthetic or approved data.

---

# 184. Lessons

Create tracked actions.

---

## 184.1 Plan Update

Versioned.

---

## 184.2 Product Bug

Issue reference.

---

# 185. Verification Run Schema

Suggested:

```text
opure.backup-verification-run/1
```

---

## 185.1 Fields

```text
verification_id
backup
level
scope
objects
owners
started_at
completed_at
results
failures
tool_versions
state
verification_sha256
```

---

# 186. Cryptographic Verification

Verify:

* key slot;
* manifest AEAD;
* object AEAD;
* object plaintext hash;
* keyed object identity;
* and commit marker.

---

# 187. Structural Verification

Verify:

* archive;
* paths;
* databases;
* schemas;
* file counts;
* sizes;
* and object graph.

---

# 188. Semantic Verification

Owner validators.

---

## 188.1 No Side Effects

Required.

---

# 189. Restore Test

Creates disposable staged root.

---

## 189.1 Activation

Never becomes current.

---

## 189.2 External Network

Denied.

---

# 190. Full Restore Test

Every included owner reaches Recovery Validation readiness.

---

## 190.1 Missing External Source

Allowed only as documented dependency.

---

# 191. Verification Freshness

Suggested:

* write: immediate crypto;
* structure: every snapshot;
* semantic: every snapshot for critical owners;
* selective restore: weekly;
* full restore: monthly;
* exercise: quarterly enterprise, user scheduled.

Provisional.

---

# 192. Backup Retention Policy Schema

Suggested:

```text
opure.backup-retention-policy/1
```

---

## 192.1 Fields

```text
policy_id
revision
daily_count
weekly_count
monthly_count
pre_update_count
maximum_age
maximum_storage
minimum_verified_count
minimum_restore_tested_count
hold_rules
erasure_rules
created_at
policy_sha256
```

---

# 193. Grandfather-Father-Son Selection

Daily, weekly and monthly points are selected deterministically.

---

## 193.1 Time Zone

Schedule time zone recorded.

---

## 193.2 Duplicate Snapshot

May satisfy several retention buckets.

---

# 194. Retention Decision

For each snapshot:

* Keep;
* Keep as Daily;
* Keep as Weekly;
* Keep as Monthly;
* Keep for Update Rollback;
* Keep for Hold;
* Keep as Last Verified;
* Keep as Last Restore Tested;
* Delete Eligible;
* Delete Blocked;
* and Quarantined.

---

# 195. Prune Plan

Contains:

* manifests;
* objects;
* holds;
* verification dependencies;
* Vault Capsules;
* estimated reclaimed bytes;
* and preview.

---

# 196. Prune Commit

1. mark manifests deletion pending;
2. update references;
3. commit deletion receipts;
4. delete manifests;
5. delete unreferenced objects;
6. verify repository;
7. complete.

---

# 197. Backup Repository Scrub

Periodic full object verification.

---

## 197.1 Rate

Bounded.

---

## 197.2 Removable Offline

Run when connected.

---

# 198. Corrupt Object

Find affected snapshots.

---

## 198.1 Alternative Copy

Repair only from another verified repository or source snapshot.

---

## 198.2 No Repair Source

Mark snapshots unusable.

---

# 199. Backup Replication

Version 1:

* manual repository copy;
* portable archive;
* or user-managed filesystem replication.

---

## 199.1 Direct Cloud API

Deferred.

---

## 199.2 Repository Copy

Must preserve all objects and commit markers.

---

# 200. Trust Centre Backup Views

## 200.1 Overview

Show:

* last successful backup;
* last complete backup;
* last portable backup;
* last off-device backup;
* last verification;
* last restore test;
* last exercise;
* and current RPO estimate.

---

## 200.2 Destination

Show classification and key recovery.

---

## 200.3 Snapshot Detail

Show owners, exclusions, verification and restore requirements.

---

## 200.4 Restore History

Show plan and report.

---

## 200.5 Erasure

Show backup treatment without deleted content.

---

# 201. Desktop Backup UI

Suggested areas:

* Backup Overview;
* Configure Destination;
* Schedule;
* Recovery Points;
* Managed Backups;
* Portable Backup;
* Workspace Recovery;
* Vault Recovery;
* Restore;
* Portability;
* Verification;
* Exercises;
* Disaster Recovery;
* Retention;
* Erasure;
* and Advanced.

---

## 201.1 First-Run Recommendation

Explain:

* local state exists only on this device;
* same-device recovery is not device-loss protection;
* secrets are not portable by default;
* and an external recovery key must be stored separately.

---

# 202. UI Copy — Backup Not Configured

Suggested:

> Opure is protecting active transactions against ordinary process and power failures, but no off-device backup destination is configured. Device loss, disk failure, destructive malware or deletion of this data root could still cause permanent loss.

---

# 203. UI Copy — Same-Device Only

Suggested:

> This recovery point is stored on the same physical device as the active Opure data. It can help with an update or local corruption, but it does not protect against device failure, loss or ransomware affecting the same storage.

---

# 204. UI Copy — Portable Key Missing

Suggested:

> The repository can currently be unlocked only through this Windows user and computer. Add and verify a separately stored recovery passphrase or recovery key before relying on it for device-loss recovery.

---

# 205. UI Copy — Vault Not Portable

Suggested:

> This backup contains secret references and account metadata but not portable secret values. After restore, Opure will ask you to reconnect providers and re-enter credentials. Create a separate Vault Recovery Capsule only when the recovery value justifies the additional secret-backup risk.

---

# 206. UI Copy — Vault Capsule Warning

Suggested:

> A Vault Recovery Capsule contains encrypted copies of selected secrets and a rewrapped Vault key. Anyone with the capsule and its recovery secret may be able to recover those credentials. Store the recovery secret separately, verify it now, and protect both artefacts.

---

# 207. UI Copy — Restore Safety

Suggested:

> Opure will restore into a new isolated data root and validate it before activation. Plugins, MCP servers, workflows, provider calls, project writes and external effects will remain disabled until the recovery review is complete.

---

# 208. UI Copy — Ransomware Recovery

Suggested:

> Do not restore into an environment that may still be compromised. Use a clean Windows installation, a signed Opure package and a known-clean offline or isolated backup. Backup age and successful decryption do not prove that the source data is free from malicious content.

---

# 209. UI Copy — Verification Level

Suggested:

> This backup is cryptographically and structurally verified. It has not yet completed a full isolated restore test. Verification proves the stored objects are intact and readable; it does not prove every external dependency or credential will still be available.

---

# 210. UI Copy — Portability

Suggested:

> A Data Portability Bundle contains documented machine-readable data for reuse and inspection. It is not a full internal backup and cannot automatically resume workflows, restore credentials, grant permissions or reproduce every cache and installed package.

---

# 211. CLI Commands

Conceptual:

```text
opure backup status
opure backup destinations
opure backup create
opure backup verify
opure backup test-restore
opure backup export
opure backup prune
opure restore plan
opure restore validate
opure restore activate
opure restore rollback
opure portability export
opure portability import
opure recovery exercise
```

---

## 211.1 Secret Input

Passphrases through trusted prompt or secure input, not ordinary command-line argument.

---

## 211.2 Output

No recovery keys or secret values.

---

# 212. Diagnostics

Safe diagnostics may include:

* backup product;
* owner count;
* snapshot count;
* object count;
* bytes;
* deduplication ratio;
* destination class;
* barrier duration;
* backup duration;
* verification level;
* restore state;
* migration count;
* missing-dependency count;
* and stable error code.

---

## 212.1 Prohibited Diagnostics

Do not log:

* passphrase;
* recovery key;
* Repository Master Key;
* Vault Capsule contents;
* secret inventory names where sensitive;
* project source;
* absolute destination path;
* raw manifest;
* or decrypted object content.

---

# 213. Metrics

Low-cardinality local metrics:

* backup attempts;
* backup success;
* backup failure class;
* barrier duration;
* copied bytes;
* unique bytes;
* verification result;
* restore test result;
* restore duration class;
* key-slot health;
* overdue schedules;
* and destination health class.

---

## 213.1 No Repository ID Label

Required.

---

## 213.2 No Project Label

Required.

---

# 214. Trust Evidence

Evidence types should include:

* Backup Destination Added;
* Backup Destination Removed;
* Backup Schedule Created;
* Backup Epoch Started;
* Consistency Barrier Entered;
* Owner Checkpoint Created;
* Backup Snapshot Completed;
* Backup Snapshot Failed;
* Backup Verification Completed;
* Restore Test Completed;
* Portable Backup Exported;
* Recovery Key Slot Added;
* Recovery Key Verified;
* Vault Capsule Created;
* Vault Capsule Restored;
* Restore Plan Created;
* Restore Activated;
* Restore Rolled Back;
* Recovery Report Created;
* Erasure Ledger Applied;
* Backup Pruned;
* Disaster Recovery Declared;
* Recovery Exercise Completed;
* and Backup Health Changed.

---

## 214.1 Keys

Never in evidence.

---

# 215. Error Model

Stable categories:

* Backup Product Unsupported;
* Backup Adapter Missing;
* Backup Adapter Unsupported Schema;
* Backup Owner Unavailable;
* Backup Owner Busy;
* Backup Migration Active;
* Backup Source Database Unhealthy;
* Backup Epoch Timeout;
* Backup Barrier Timeout;
* Backup Snapshot Establishment Failed;
* Backup SQLite Busy;
* Backup SQLite Corrupt;
* Backup SQLite Validation Failed;
* Backup Artefact Missing;
* Backup CAS Pin Failed;
* Backup Destination Missing;
* Backup Destination Denied;
* Backup Destination Full;
* Backup Destination Read Only;
* Backup Destination Network Unavailable;
* Backup Destination Sync Unknown;
* Backup Repository Locked;
* Backup Repository Corrupt;
* Backup Repository Version Unsupported;
* Backup Key Unavailable;
* Backup Key Slot Invalid;
* Backup Passphrase Invalid;
* Backup KDF Denied;
* Backup Encryption Failed;
* Backup Authentication Failed;
* Backup Nonce Reuse Detected;
* Backup Object Hash Invalid;
* Backup Manifest Invalid;
* Backup Commit Marker Missing;
* Backup Partial;
* Backup Verification Failed;
* Backup Restore Test Failed;
* Backup Retention Conflict;
* Backup Prune Failed;
* Backup Vault Capsule Denied;
* Backup Vault Capsule Invalid;
* Backup Workspace Pack Conflict;
* Restore Source Unsupported;
* Restore Source Corrupt;
* Restore Source Untrusted;
* Restore Version Unsupported;
* Restore Mapping Required;
* Restore Project Missing;
* Restore Package Missing;
* Restore Credential Required;
* Restore Policy Denied;
* Restore Migration Failed;
* Restore Validation Failed;
* Restore Erasure Failed;
* Restore Activation Failed;
* Restore Rollback Failed;
* Restore External Effect Unresolved;
* Restore Workflow Recovery Required;
* Portability Export Invalid;
* Portability Import Invalid;
* Portability Authority Denied;
* Recovery Exercise Failed;
* Disaster Recovery Clean Environment Required;
* and Backup Recovery Required.

---

# 216. Security Threat Model

Relevant threats include:

* raw live database copy;
* WAL omission;
* hot-journal omission;
* source database substitution;
* destination database substitution;
* backup adapter impersonation;
* owner checkpoint forgery;
* Backup Epoch replay;
* barrier bypass;
* inconsistent cross-service state;
* missing CAS object;
* stale CAS pin;
* object-ID collision;
* plaintext-hash disclosure;
* object substitution;
* chunk truncation;
* chunk reordering;
* manifest substitution;
* commit-marker forgery;
* repository rollback;
* key-slot downgrade;
* KDF downgrade;
* weak passphrase;
* recovery-key stored with backup;
* DPAPI portability misunderstanding;
* Repository Master Key exposure;
* AES-GCM nonce reuse;
* associated-data confusion;
* Vault Capsule leakage;
* Vault root-key exposure;
* secret in ordinary backup;
* secret in portability export;
* destination path substitution;
* removable-volume substitution;
* network-share tampering;
* cloud-sync partial state;
* ransomware deletion;
* ransomware encryption;
* malicious backup content;
* executable package in backup;
* plugin auto-activation;
* MCP auto-launch;
* provider auto-contact;
* workflow auto-resume;
* external effect replay;
* restore path traversal;
* archive bomb;
* decompression bomb;
* case-colliding restore paths;
* reparse-point restore;
* old vulnerable schema parser;
* migration code abuse;
* erasure-ledger omission;
* deleted-data resurrection;
* backup-retention bypass;
* Preservation Hold bypass;
* rollback-root deletion;
* restore approval reuse;
* and clean-room mode network bypass.

---

# 217. Security Controls

Controls include:

* owner-service Backup Adapters;
* authenticated IPC;
* Backup Epochs;
* bounded consistency barriers;
* SQLite Online Backup;
* no raw live copies;
* owner checkpoint hashes;
* exact service versions;
* CAS pin leases;
* encrypted objects;
* keyed object IDs;
* random Repository Master Keys;
* HKDF purpose separation;
* AES-256-GCM;
* random nonces;
* DPAPI and portable key slots;
* PBKDF2 calibration;
* independent recovery keys;
* no passphrase storage;
* separate Vault Capsules;
* strict destination identity;
* synthetic-full manifests;
* commit marker last;
* full object verification;
* strict archive paths;
* untrusted restore parsing;
* network-disabled Recovery Host;
* staged data roots;
* no execution from backup;
* package quarantine;
* plugin and MCP disabled;
* workflows Recovery Required;
* external-effect reconciliation;
* current policy revalidation;
* Erasure Ledger application;
* root-switch rollback;
* clean-room recovery;
* retention and holds;
* and Trust Evidence.

---

# 218. Security Limitations

This architecture cannot guarantee:

* that a backup source was not already compromised;
* that a user-selected passphrase is strong;
* that the recovery key is stored safely;
* that same-user malware cannot access a mounted repository;
* that a network or cloud destination is immutable;
* that every external package remains available;
* that every provider credential can be restored;
* that every external effect can be reconciled;
* or that deleted data is forensically erased from every medium.

---

# 219. Privacy Impact

Backups may contain:

* personal preferences;
* project names;
* memory;
* workflow history;
* provider metadata;
* Trust Evidence;
* and selected source.

---

## 219.1 Data Minimisation

Rebuildable and externally recoverable data excluded by default.

---

## 219.2 Encryption

Required for managed recovery state.

---

## 219.3 Portability

Structured and explicit.

---

## 219.4 Erasure

Ledger applied.

---

# 220. Reliability Impact

Service-aware backups add:

* checkpoint coordination;
* object storage;
* encryption;
* verification;
* and restore logic

to improve recovery confidence.

---

## 220.1 Backup Failure

Does not stop normal work except mandatory migration gates.

---

## 220.2 Restore Failure

Prior active root remains.

---

# 221. Performance Impact

Costs include:

* short barriers;
* SQLite reads;
* hashing;
* compression;
* encryption;
* destination I/O;
* verification;
* and restore tests.

---

## 221.1 Background Budget

Use ADR-0020 resource governance.

---

## 221.2 User Foreground Backup

Higher priority but bounded.

---

# 222. Provisional Performance Targets

On reference hardware:

* ordinary consistency barrier p95: under 2 seconds;
* maximum ordinary barrier: 5 seconds;
* owner checkpoint p95: under 500 ms;
* SQLite snapshot establishment p95: under 500 ms per owner;
* sustained local backup throughput: at least 150 MiB/s where storage permits;
* encryption throughput: at least 500 MiB/s aggregate target where hardware permits;
* repository deduplication lookup p95: under 2 ms per chunk;
* manifest build for one million objects: under 30 seconds;
* cryptographic verification: at least 250 MiB/s;
* core-state staged restore: under 30 minutes for 100 GiB local backup;
* root activation: under 2 minutes;
* and Backup Health query: under 100 ms.

These require evidence.

---

# 223. Scale Targets

Test:

* 100 owner services;
* 1,000 service databases;
* 10 million repository objects;
* 100,000 snapshots;
* 10 TiB repository;
* 10,000 projects;
* 100,000 workflows;
* 1 million memory records;
* 1 million erasure records;
* 1 TiB portable archive;
* and 100 concurrent restore-test candidates as a stress scenario.

---

# 224. Testing Strategy

ADR-0008 applies.

Backup and recovery require:

* schema tests;
* adapter tests;
* epoch tests;
* barrier tests;
* SQLite tests;
* WAL tests;
* CAS tests;
* destination tests;
* repository tests;
* chunk tests;
* cryptographic tests;
* key-slot tests;
* Vault Capsule tests;
* schedule tests;
* retention tests;
* workspace-pack tests;
* portability tests;
* restore tests;
* migration tests;
* erasure tests;
* disaster-recovery tests;
* fuzzing;
* fault injection;
* performance tests;
* endurance tests;
* and adversarial ransomware and malicious-backup suites.

---

# 225. Backup Database Tests

Test:

* create;
* migrate;
* foreign keys;
* outbox;
* backup metadata crash;
* restore metadata;
* channel isolation;
* corruption;
* and recovery.

---

# 226. Backup Product Definition Tests

Test every field and immutable hash.

---

# 227. Backup Adapter Definition Tests

Test:

* supported schema;
* required state;
* rebuildable state;
* secrets;
* prohibited state;
* dependencies;
* limits;
* and implementation hash.

---

# 228. Missing Adapter Tests

Add new owner state without adapter.

Full backup fails.

---

# 229. State Inventory Tests

Test every initial owner and classification.

---

# 230. Configuration Adapter Tests

Verify machine policy is observed but not restored as authority.

---

# 231. Workflow Adapter Tests

Verify complete event, effect and checkpoint dependencies.

---

# 232. Secrets Adapter Tests

Verify ordinary backup contains no plaintext secrets.

---

# 233. Backup Epoch Schema Tests

Test all fields and states.

---

# 234. Participant Discovery Tests

Test:

* all present;
* optional missing;
* required missing;
* service starting;
* service stopping;
* stale registry;
* and other channel.

---

# 235. Prepare Backup Tests

Test every failure class.

---

# 236. Barrier Admission Tests

Race new mutating commands with barrier.

---

# 237. Barrier Drain Tests

Test:

* short transaction;
* long transaction;
* stuck transaction;
* external operation;
* read transaction;
* and service crash.

---

# 238. Barrier Timeout Tests

Verify all services release.

---

# 239. Barrier Crash Tests

Kill coordinator:

* before acquisition;
* during acquisition;
* after owner checkpoint;
* after snapshot establishment;
* and during release.

---

# 240. SQLite Online Backup Tests

Test:

* empty database;
* small;
* large;
* WAL mode;
* concurrent readers;
* concurrent writers after barrier;
* busy;
* cancellation;
* disk full;
* destination I/O error;
* and source service crash.

---

# 241. Raw Copy Denial Tests

Attempt to copy live `.db` only.

---

# 242. WAL Mismatch Tests

Pair:

* wrong WAL;
* missing WAL;
* old WAL;
* another database's WAL;
* and hot journal.

Detect invalid raw source.

---

# 243. Online Snapshot Semantics Tests

Verify destination is consistent at established snapshot boundary.

---

# 244. SQLite Page-Batch Tests

Benchmark 1, 16, 128, 1,024 and all pages.

---

# 245. SQLite Busy-Handling Tests

Test bounded retries and no infinite lock.

---

# 246. SQLite Destination Tests

Test existing file, reparse, network, ACL and collision.

---

# 247. SQLite Validation Tests

Test quick check, foreign keys, schema and owner validator.

---

# 248. `VACUUM INTO` Tests

Test:

* compact copy;
* deleted-page removal;
* insufficient disk;
* interruption;
* concurrent write;
* and cleanup.

---

# 249. VSS Non-Claim Tests

Verify UI does not call raw shadow copy application consistent.

---

# 250. CAS Pin Tests

Test:

* pin;
* copy;
* release;
* cancellation;
* expiry;
* GC race;
* missing object;
* wrong hash;
* and owner crash.

---

# 251. Mutable-File Snapshot Tests

Test staged immutable conversion.

---

# 252. Package Manifest Tests

Test package types, hashes, signatures and optional bytes.

---

# 253. Rebuildable Exclusion Tests

Attempt to exclude non-rebuildable state.

Fail.

---

# 254. Destination Schema Tests

Test every field and state.

---

# 255. Destination Identity Tests

Test:

* same path new volume;
* changed drive letter;
* removable reconnect;
* reparse;
* UNC;
* and cloud folder.

---

# 256. Same-Device Tests

Test known same, known different and unknown.

---

# 257. Network-Share Tests

Test interruption, reconnect, partial object, concurrent client and permissions.

---

# 258. Cloud-Sync Tests

Test placeholder, sync pending, conflict copy, version rollback and unavailable client.

---

# 259. Removable-Disk Tests

Remove:

* before write;
* during object;
* during manifest;
* during commit;
* after commit before flush;
* and during verification.

---

# 260. Destination Capacity Tests

Test estimate, safety reserve and changing free space.

---

# 261. Repository Schema Tests

Test format revision and header hash.

---

# 262. Repository Lock Tests

Test current writer, stale process, stale boot, network delay and forced break.

---

# 263. Chunking Tests

Test:

* zero bytes;
* one byte;
* exactly 4 MiB;
* 4 MiB plus one;
* huge file;
* stream cancellation;
* and deterministic boundaries.

---

# 264. Object-ID Tests

Test HMAC identity, repository separation and collision handling.

---

# 265. Plain-Hash Disclosure Tests

Inspect outer repository names and metadata.

---

# 266. Compression Tests

Test compressible, incompressible, already compressed and secret class.

---

# 267. Repository Master Key Tests

Test generation, memory lifetime, rotation and loss.

---

# 268. HKDF Tests

Test every domain label and known-answer vectors.

---

# 269. AES-GCM Tests

Test known-answer vectors, nonce size, tag size, associated data and tampering.

---

# 270. Nonce-Reuse Tests

Force RNG or state failure.

Backup stops and security evidence created.

---

# 271. DPAPI Slot Tests

Test:

* current user;
* another user;
* same machine;
* new machine;
* profile restore;
* and corrupted blob.

---

# 272. DPAPI UI Tests

Verify portability warning.

---

# 273. Passphrase Slot Tests

Test:

* correct;
* wrong;
* empty;
* Unicode;
* very long;
* changed normalisation;
* copied whitespace;
* and forgotten.

---

# 274. Passphrase Encoding Tests

Define exact UTF-8 and normalisation policy.

---

# 275. PBKDF2 Calibration Tests

Test fast and slow hardware, minimum, maximum and denial-of-service bounds.

---

# 276. PBKDF2 API Tests

Verify one-shot static .NET API is used.

---

# 277. KDF Downgrade Tests

Attempt weak iterations or SHA-1.

---

# 278. Random Recovery Key Tests

Test generation, file export, checksum, fingerprint, wrong repository and corruption.

---

# 279. Recovery Key Co-Location Tests

Warn when selected path is inside repository.

---

# 280. Key Slot Lifecycle Tests

Test add, verify, replace, disable, remove, rotate and last-slot protection.

---

# 281. Key Slot Substitution Tests

Replace metadata, wrapped key, nonce and fingerprint.

---

# 282. Repository Key Rotation Tests

Restore snapshots across key versions.

---

# 283. Manifest Schema Tests

Test every field, canonicalisation and encryption.

---

# 284. Manifest Substitution Tests

Swap between repositories and snapshots.

---

# 285. Owner Entry Tests

Test restore order, schema and exclusions.

---

# 286. Commit Marker Tests

Test missing, altered, duplicate and stale marker.

---

# 287. Synthetic-Full Tests

Delete parent manifest and restore child snapshot.

---

# 288. Reference-Count Tests

Corrupt index and rebuild from manifests.

---

# 289. Schedule Schema Tests

Test every field.

---

# 290. DST Schedule Tests

Test spring forward and autumn rollback in Europe/London.

---

# 291. Sleep and Missed-Run Tests

Test each policy.

---

# 292. Power Policy Tests

Test AC, battery thresholds and power change.

---

# 293. Network Policy Tests

Test metered and VPN classes.

---

# 294. Scheduler Admission Tests

Test active inference, workflow, update, low disk and user foreground.

---

# 295. Backup Cancellation Tests

Cancel at every epoch state.

---

# 296. Backup Resume Tests

Test complete local database snapshot and incomplete live snapshot.

---

# 297. Backup Retry Tests

Test retryable and non-retryable classes.

---

# 298. Local Recovery Point Tests

Test create, list, verify, restore and cleanup.

---

# 299. Pre-Migration Gate Tests

Migration cannot begin without verified point.

---

# 300. Pre-Update Gate Tests

Test update with and without state migration.

---

# 301. Workspace Pack Schema Tests

Test every field and hash.

---

# 302. Tracked Patch Tests

Test modifications, additions, deletions, renames and modes.

---

# 303. Staged and Unstaged Tests

Preserve separation.

---

# 304. Binary Change Tests

Test explicit inclusion.

---

# 305. Untracked File Tests

Test selection, ignore, secret and size.

---

# 306. Non-Git Project Tests

Test selected recovery and full explicit snapshot.

---

# 307. Workspace Pack Restore Tests

Test new branch, new worktree, conflict and no push.

---

# 308. Vault Capsule Schema Tests

Test every encrypted field and outer header.

---

# 309. Vault Capsule Creation Tests

Test selected records, full Vault, passphrase, recovery key and cancellation.

---

# 310. Vault Root-Key Boundary Tests

Backup Service never receives plaintext root key.

---

# 311. Vault Capsule Restore Tests

Test same machine, new machine, wrong key, expired token and remapping.

---

# 312. Vault Capsule Leakage Tests

Inspect logs, Trust Evidence, staging, manifest and crash output.

---

# 313. Portability Bundle Schema Tests

Test all categories and hashes.

---

# 314. Portability Format Tests

Validate JSON, JSONL, Markdown, CSV and patch files.

---

# 315. Portability Exclusion Tests

Verify no databases, queues, leases, capabilities, approvals, secrets, DPAPI or executables.

---

# 316. Portability Preview Tests

Verify all personal and project categories.

---

# 317. Portability Import Tests

Test candidate creation and no execution.

---

# 318. Portability Authority Tests

Attempt grants, active workflows, provider call and patch apply.

---

# 319. Import Collision Tests

Test Profile, project, workflow and memory IDs.

---

# 320. Restore Plan Schema Tests

Test every field, expiry and hash.

---

# 321. Restore Source Lock Tests

Test replacement and modification during read.

---

# 322. Recovery Host Launch Tests

Test exact binary, signature, environment, working directory and policy.

---

# 323. Recovery Host Network Tests

Monitor all outbound and inbound network.

---

# 324. Recovery Host Plugin Tests

Attempt load.

---

# 325. Recovery Host MCP Tests

Attempt launch and connect.

---

# 326. Restore Parsing Fuzz Tests

Test manifests, objects, archives, paths, databases and schemas.

---

# 327. Version Compatibility Tests

Test direct, migration, intermediate, newer, too old and unknown.

---

# 328. Old Binary Execution Tests

Place executable in backup.

Never execute.

---

# 329. Staged Root Tests

Test ACL, path, disk, collision and cleanup.

---

# 330. Restore Database Tests

Test every validation and migration step.

---

# 331. Restore CAS Tests

Test shared object, missing object and corruption.

---

# 332. Package Quarantine Tests

Test valid, invalid signature, revoked publisher and changed source.

---

# 333. Model Restore Tests

Test exact hash, missing Runtime Package, wrong format and licence change.

---

# 334. Plugin Restore Tests

Test disabled state, same-machine grants and new-machine fresh review.

---

# 335. MCP Restore Tests

Test disabled, fingerprint change, account missing and credential missing.

---

# 336. Provider Restore Tests

Test profile, region, terms and reauthentication.

---

# 337. Workflow Restore Tests

Test:

* pure pending;
* timer;
* approval;
* signal;
* side effect;
* Outcome Unknown;
* compensation;
* and child workflow.

All non-terminal workflows remain Recovery Required.

---

# 338. Configuration Restore Tests

Test current Product and Enterprise policy.

---

# 339. Project Mapping Tests

Test same path wrong repository, new path, clone, missing and ambiguous.

---

# 340. New-Machine Restore Tests

Test machine settings, hardware qualification, DPAPI failure and recovery slot.

---

# 341. Identity Mapping Tests

Test every identity and collision.

---

# 342. Erasure Ledger Schema Tests

Test every field and owner.

---

# 343. Backup-Time Erasure Tests

Delete then create new snapshot.

---

# 344. Restore-Time Erasure Tests

Restore backup predating deletion.

Deleted data must not reactivate.

---

# 345. Erasure Conflict Tests

Test missing owner purge adapter and corrupt ledger.

---

# 346. Restore Activation Plan Tests

Test all gates and rollback.

---

# 347. Service Restore Order Tests

Randomise and verify dependency enforcement.

---

# 348. Root Activation Crash Tests

Crash:

* before intent;
* after intent;
* after previous pointer;
* after current pointer;
* during flush;
* before Runtime start;
* and during validation.

---

# 349. Recovery Validation Tests

Attempt network, plugin, MCP, workflow, model, tool and project write.

---

# 350. Recovery Confirmation Tests

Test confirm, timeout and rollback.

---

# 351. Selective Service Restore Tests

Test safe and unsafe dependency combinations.

---

# 352. Selective Project Restore Tests

Test new identity and reviewed merge.

---

# 353. Disaster Plan Schema Tests

Test every field and scenario.

---

# 354. Business Impact Tests

Verify RPO and RTO are present for critical tiers.

---

# 355. Clean-Room Tests

Use clean VM or machine with no prior Opure data.

---

# 356. Ransomware-Suspected Tests

Verify no writes to original repository and no network.

---

# 357. Known-Clean Selection Tests

Test incident window and multiple backup candidates.

---

# 358. Repository Quarantine Tests

Test read-only verification and malicious content.

---

# 359. Signed-Reinstall Tests

Use valid, invalid and revoked packages.

---

# 360. Recovery Exercise Schema Tests

Test every scenario and result.

---

# 361. Tabletop Exercise Tests

Verify actions and lessons.

---

# 362. Full Restore Exercise Tests

Measure RPO and RTO.

---

# 363. New-Machine Exercise Tests

Test secret and path recovery.

---

# 364. Ransomware Exercise Tests

Test isolated destination and no external effects.

---

# 365. Verification Run Schema Tests

Test all levels.

---

# 366. Cryptographic Verification Tests

Tamper every cryptographic field.

---

# 367. Structural Verification Tests

Corrupt archives, databases, paths and object graph.

---

# 368. Semantic Verification Tests

Owner-specific invalid state.

---

# 369. Selective Restore Test Tests

Verify disposable root deletion.

---

# 370. Full Restore Test Tests

Verify every owner readiness state.

---

# 371. Verification Freshness Tests

Test overdue and policy changes.

---

# 372. Retention Policy Schema Tests

Test every field.

---

# 373. Daily-Weekly-Monthly Tests

Test time boundaries and duplicate bucket selection.

---

# 374. Last-Verified Protection Tests

Attempt prune.

---

# 375. Last-Restore-Tested Protection Tests

Attempt prune.

---

# 376. Preservation Hold Tests

Use ADR-0027 hold.

---

# 377. Vault Capsule Retention Tests

Require separate review.

---

# 378. Prune Plan Tests

Test reclaim estimate and object dependencies.

---

# 379. Prune Crash Tests

Crash at every phase.

---

# 380. Repository Scrub Tests

Test full, partial, interrupted and offline destination.

---

# 381. Corrupt Object Impact Tests

Find every affected snapshot.

---

# 382. Repository Copy Tests

Test complete and incomplete copies.

---

# 383. Backup Health Tests

Test every health state and objective warning.

---

# 384. Trust Centre Tests

Verify all backup and recovery views.

---

# 385. Diagnostics Leakage Tests

Seed passphrase, key and project canaries.

---

# 386. Metrics Cardinality Tests

Attempt destination, repository and project IDs.

---

# 387. Fuzzing

Fuzz:

* Backup Product Definition;
* Backup Adapter;
* Backup Epoch;
* owner checkpoint;
* destination;
* repository header;
* key slot;
* encrypted object;
* manifest;
* commit marker;
* schedule;
* Workspace Pack;
* Vault Capsule;
* Portability Bundle;
* Restore Plan;
* Recovery Report;
* Erasure Ledger;
* Disaster Recovery Plan;
* and Recovery Exercise.

---

# 388. Raw-Copy Adversarial Suite

Copy live databases under writes and try restore.

---

# 389. WAL Adversarial Suite

Swap and omit WAL and journal files.

---

# 390. Epoch Adversarial Suite

Forge checkpoints and replay old epochs.

---

# 391. Barrier Adversarial Suite

Flood mutations and crash participants.

---

# 392. Repository-Cryptography Adversarial Suite

Attempt:

* object substitution;
* repository cross-copy;
* nonce reuse;
* KDF downgrade;
* key-slot replacement;
* master-key exposure;
* and rollback.

---

# 393. Backup-Key Adversarial Suite

Store key with backup, use weak passphrase and remove last slot.

---

# 394. Vault Adversarial Suite

Attempt plaintext leakage through every backup path.

---

# 395. Destination Adversarial Suite

Replace removable volume and tamper network objects.

---

# 396. Malicious-Backup Adversarial Suite

Include:

* executable;
* DLL;
* plugin;
* MCP config;
* path traversal;
* symlink;
* device name;
* huge object;
* compression bomb;
* malicious SQLite;
* future schema;
* and forged package.

---

# 397. Auto-Execution Adversarial Suite

Attempt plugin, MCP, workflow, tool, provider and update execution during restore.

---

# 398. Erasure-Resurrection Adversarial Suite

Restore deleted personal data from old backup.

---

# 399. Ransomware Adversarial Suite

Simulate repository deletion, encryption, current-data compromise and suspect packages.

---

# 400. Root-Switch Adversarial Suite

Power loss and process crash at every activation boundary.

---

# 401. Cross-Project Adversarial Suite

Attempt selective restore into another project.

---

# 402. Portability Adversarial Suite

Attempt to import authority, secret and executable content.

---

# 403. Performance Tests

Measure:

* barrier;
* SQLite backup;
* chunking;
* hashing;
* compression;
* encryption;
* destination write;
* manifest;
* verification;
* restore;
* migration;
* root switch;
* and cleanup.

---

# 404. Endurance Tests

Run for 30 days with:

* daily snapshots;
* weekly verification;
* ongoing writes;
* Runtime restarts;
* destination disconnects;
* key rotation;
* pruning;
* and restore tests.

---

# 405. Accessibility Tests

Backup and Recovery UI must support:

* keyboard;
* Narrator;
* high contrast;
* schedule review;
* key warnings;
* destination classification;
* verification levels;
* restore mapping;
* missing dependencies;
* workflow safety;
* erasure warnings;
* and recovery progress.

---

# 406. Prototype Plan

## 406.1 Prototype A — SQLite Live Backup

Three active WAL databases under concurrent writes.

---

## 406.2 Prototype B — Backup Epoch

Coordinate five services and establish snapshots under two seconds.

---

## 406.3 Prototype C — Encrypted Repository

Chunk, deduplicate, encrypt, commit and verify.

---

## 406.4 Prototype D — Portable Key Recovery

Restore repository on a second clean Windows machine using a passphrase or recovery key.

---

## 406.5 Prototype E — Workspace Pack

Recover staged, unstaged and untracked changes into a new worktree.

---

## 406.6 Prototype F — Vault Capsule

Restore selected test credentials to a new DPAPI context.

---

## 406.7 Prototype G — Staged Root Restore

Restore core services and atomically activate with rollback.

---

## 406.8 Prototype H — Workflow Safety

Restore an ambiguous side effect and prove no automatic retry.

---

## 406.9 Prototype I — Erasure Ledger

Delete memory, restore an older backup and prove it remains deleted.

---

## 406.10 Prototype J — Portability

Export and import Profiles, memory and workflows without authority.

---

## 406.11 Prototype K — Ransomware Clean Room

Restore an offline snapshot into a clean isolated environment.

---

## 406.12 Prototype L — Full Recovery Exercise

Measure RPO and RTO and record lessons.

---

# 407. Implementation Plan

1. Record founder review.
2. Define backup state classes.
3. Define Backup Product schema.
4. Define Backup Adapter schema.
5. Inventory every owner service.
6. Define Backup Epoch schema.
7. Implement participant discovery.
8. Implement bounded consistency barrier.
9. Implement owner checkpoint contract.
10. Implement SQLite Online Backup adapter.
11. Implement snapshot validation.
12. Implement CAS pinning.
13. Implement mutable-file snapshot contract.
14. Define Destination schema.
15. Implement destination identity and health.
16. Define Repository format.
17. Implement chunking.
18. Implement keyed object IDs.
19. Implement compression.
20. Define Encryption Profile.
21. Implement Repository Master Key.
22. Implement HKDF key separation.
23. Implement AES-GCM object format.
24. Implement DPAPI key slot.
25. Implement PBKDF2 passphrase slot.
26. Implement random recovery-key slot.
27. Implement key-slot verification.
28. Define Backup Manifest.
29. Implement synthetic-full manifests.
30. Implement commit markers.
31. Implement repository reference rebuild.
32. Define schedules and admission.
33. Implement cancellation and resume.
34. Implement local Recovery Points.
35. Integrate update and migration gates.
36. Define Workspace Recovery Pack.
37. Implement Git and non-Git packs.
38. Define Vault Recovery Capsule.
39. Implement capsule creation in Secrets Service.
40. Implement capsule restore.
41. Define Data Portability Bundle.
42. Implement export and preview.
43. Implement safe import candidates.
44. Define Restore Plan.
45. Implement Recovery Host.
46. Implement strict restore parser.
47. Implement staged data roots.
48. Implement owner database restore.
49. Implement package quarantine.
50. Implement project and identity mapping.
51. Implement workflow Recovery Required state.
52. Define Erasure Ledger.
53. Implement backup-time exclusion.
54. Implement restore-time erasure.
55. Implement root activation and rollback.
56. Define Disaster Recovery Plan.
57. Implement Clean-Room and Ransomware modes.
58. Define Recovery Exercise.
59. Implement verification levels.
60. Implement selective and full restore tests.
61. Define retention policy.
62. Implement prune and scrub.
63. Implement Backup Health.
64. Integrate Trust Centre and Desktop UI.
65. Add cryptography and malicious-backup suites.
66. Add erasure and ransomware suites.
67. Run performance and endurance tests.
68. Complete security, privacy and recovery reviews.
69. Conduct full exercise.
70. Accept, amend or reject the ADR.

---

# 408. Owners

| Area                            | Owner                                 |
| ------------------------------- | ------------------------------------- |
| Product and recovery objectives | Founder                               |
| Backup and Recovery Service     | Backup and Recovery Owner             |
| Backup Adapters                 | Every Domain Service Owner            |
| SQLite snapshots                | Persistence Owner                     |
| Consistency barrier             | Runtime Architecture Owner            |
| Backup repository               | Backup and Filesystem Owners          |
| Encryption and key slots        | Security and Secrets Owners           |
| Vault Recovery Capsule          | Secrets Owner                         |
| Workspace Recovery Pack         | Workspace and Repository Owners       |
| Data portability                | Privacy, Product and Backup Owners    |
| Restore Host                    | Runtime and Recovery Owners           |
| Staged roots and activation     | Filesystem and Recovery Owners        |
| Package revalidation            | Release, Plugin, MCP and Model Owners |
| Workflow recovery               | Workflow Owner                        |
| Erasure Ledger                  | Privacy and Domain Service Owners     |
| Disaster recovery               | Recovery and Security Owners          |
| Trust Centre                    | Trust Evidence Owner                  |
| Desktop UI                      | Desktop Owner                         |
| Enterprise destinations         | Enterprise Management Owner           |
| Exercises                       | Recovery and Test Architecture Owners |
| Adversarial tests               | Test Architecture Owner               |

---

# 409. Suggested Repository Structure

```text
src/
├── Backup/
│   ├── Opure.Backup.Contracts/
│   ├── Opure.Backup.Service/
│   ├── Opure.Backup.Adapters/
│   ├── Opure.Backup.Epochs/
│   ├── Opure.Backup.Sqlite/
│   ├── Opure.Backup.Artefacts/
│   ├── Opure.Backup.Repositories/
│   ├── Opure.Backup.Cryptography/
│   ├── Opure.Backup.Destinations/
│   ├── Opure.Backup.Scheduling/
│   ├── Opure.Backup.Verification/
│   ├── Opure.Backup.Retention/
│   ├── Opure.Backup.Workspace/
│   ├── Opure.Backup.Portability/
│   ├── Opure.Backup.Restore/
│   ├── Opure.Backup.DisasterRecovery/
│   ├── Opure.Backup.Persistence/
│   └── Opure.Backup.Security/
├── Recovery/
│   ├── Opure.RecoveryHost/
│   ├── Opure.Recovery.Validation/
│   ├── Opure.Recovery.Migration/
│   └── Opure.Recovery.Activation/
└── Desktop/
    └── Opure.Desktop.BackupRecovery/

schemas/
└── backup/
    ├── backup-product-definition-v1.schema.json
    ├── backup-adapter-v1.schema.json
    ├── backup-epoch-v1.schema.json
    ├── backup-owner-checkpoint-v1.schema.json
    ├── backup-destination-v1.schema.json
    ├── backup-repository-v1.schema.json
    ├── backup-encryption-profile-v1.schema.json
    ├── backup-manifest-v1.schema.json
    ├── backup-schedule-v1.schema.json
    ├── workspace-recovery-pack-v1.schema.json
    ├── vault-recovery-capsule-v1.schema.json
    ├── data-portability-bundle-v1.schema.json
    ├── restore-plan-v1.schema.json
    ├── recovery-report-v1.schema.json
    ├── erasure-ledger-v1.schema.json
    ├── disaster-recovery-plan-v1.schema.json
    └── recovery-exercise-v1.schema.json

tests/
└── Backup/
    ├── Opure.Backup.UnitTests/
    ├── Opure.Backup.SqliteTests/
    ├── Opure.Backup.CryptographyTests/
    ├── Opure.Backup.RepositoryTests/
    ├── Opure.Backup.RestoreTests/
    ├── Opure.Backup.SecurityTests/
    ├── Opure.Backup.PerformanceTests/
    └── Fixtures/
        ├── Databases/
        ├── Repositories/
        ├── Manifests/
        ├── Workspaces/
        ├── VaultCapsules/
        ├── Portability/
        └── Malicious/
```

Exact project count may be consolidated under ADR-0010.

---

# 410. Backup Epoch Sketch

```json
{
  "schema": "opure.backup-epoch/1",
  "backup_epoch_id": "epoch-opaque",
  "backup_product": "scheduled-managed-backup:1",
  "destination": "destination-opaque",
  "requested_by": "developer-opaque",
  "reason": "Scheduled daily recovery backup",
  "required_owners": [
    "configuration",
    "projects",
    "workflow",
    "memory",
    "trust"
  ],
  "deadline": "2026-07-18T19:05:00Z",
  "barrier_policy": "ordinary-five-second",
  "state": "preparing",
  "sha256": "..."
}
```

---

# 411. Owner Checkpoint Sketch

```json
{
  "schema": "opure.backup-owner-checkpoint/1",
  "owner_service": "workflow-service",
  "service_version": "1.0.0",
  "schema_version": 7,
  "backup_adapter_revision": 2,
  "checkpoint_id": "checkpoint-opaque",
  "checkpoint_sequence": 54321,
  "outbox_position": 3021,
  "inbox_position": 2998,
  "cas_root": "cas-root-opaque",
  "active_migrations": [],
  "unresolved_operations": 3,
  "created_at_utc": "2026-07-18T19:00:00Z",
  "sha256": "..."
}
```

---

# 412. Repository Header Sketch

```json
{
  "format": "opure.backup-repository/1",
  "repository_id": "repository-opaque",
  "chunk_size": 4194304,
  "object_id_scheme": "hmac-sha256-v1",
  "encryption_scheme": "aes-256-gcm-hkdf-sha256-v1",
  "key_slots": [
    {
      "slot_id": "dpapi-slot-opaque",
      "type": "dpapi-current-user",
      "fingerprint": "..."
    },
    {
      "slot_id": "recovery-key-slot-opaque",
      "type": "random-recovery-key",
      "fingerprint": "..."
    }
  ],
  "header_sha256": "..."
}
```

---

# 413. Backup Manifest Sketch

```json
{
  "schema": "opure.backup-manifest/1",
  "backup_id": "backup-opaque",
  "backup_class": "scheduled-managed-backup",
  "repository_id": "repository-opaque",
  "created_at_utc": "2026-07-18T19:00:00Z",
  "completed_at_utc": "2026-07-18T19:03:00Z",
  "product_version": "1.0.0",
  "release_channel": "stable",
  "backup_epoch": "epoch-opaque",
  "consistency_state": "complete-with-rebuildable-exclusions",
  "owner_service_checkpoints": [
    "configuration-checkpoint-opaque",
    "workflow-checkpoint-opaque",
    "memory-checkpoint-opaque"
  ],
  "exclusions": [
    {
      "state_class": "project-knowledge-index",
      "reason": "rebuildable",
      "rebuild_source": "project-source-and-index-policy"
    }
  ],
  "verification_state": "semantically-verified",
  "manifest_sha256": "..."
}
```

---

# 414. Restore Plan Sketch

```json
{
  "schema": "opure.restore-plan/1",
  "restore_plan_id": "restore-opaque",
  "source_backup": "backup-opaque",
  "source_verification": "fully-restore-tested",
  "source_product_version": "1.0.0",
  "destination_channel": "stable",
  "restore_mode": "new-machine-restore",
  "selected_owners": [
    "configuration",
    "projects",
    "memory",
    "workflow",
    "trust"
  ],
  "path_mappings": [
    {
      "project": "project-opaque",
      "destination": "project-root-reference-opaque"
    }
  ],
  "secret_policy": "reacquire-credentials",
  "workflow_policy": "restore-recovery-required",
  "network_policy": "disabled-until-confirmed",
  "preview_sha256": "...",
  "sha256": "..."
}
```

---

# 415. Recovery Report Sketch

```json
{
  "schema": "opure.recovery-report/1",
  "recovery_id": "recovery-opaque",
  "backup": "backup-opaque",
  "verification_level": "fully-restore-tested",
  "restored_owners": 12,
  "excluded_owners": 2,
  "migrations": 4,
  "missing_credentials": 3,
  "missing_packages": 1,
  "paused_workflows": 7,
  "unresolved_external_effects": 1,
  "erasure_records_applied": 5,
  "activation_result": "completed-with-missing-dependencies",
  "rollback_root": "root-previous-opaque",
  "created_at": "2026-07-18T20:00:00Z",
  "sha256": "..."
}
```

---

# 416. Release Gate

Backup, restore, portability and disaster recovery are blocked when:

* a live SQLite database is copied with an ordinary file copy;
* a database is backed up without accounting for WAL or hot journal state;
* the owner service cannot identify its authoritative backup boundary;
* a required owner lacks a Backup Adapter;
* a full backup is marked Complete while required owners are missing;
* the consistency barrier remains active for the full copy;
* a backup claims a cross-service distributed transaction;
* CAS objects can be garbage collected while a snapshot references them;
* backup manifests omit service versions or schema versions;
* a committed snapshot lacks a last-written commit marker;
* a restore requires replaying a fragile sequence of incremental manifests;
* a repository exposes plaintext project names or paths in outer metadata;
* backup content is stored unencrypted without explicit safe classification;
* a Repository Master Key is stored plaintext;
* DPAPI-only access is described as device-loss portability;
* a recovery passphrase is stored;
* a recovery key is placed automatically inside the backup repository;
* PBKDF2 parameters can be downgraded below policy;
* AES-GCM nonces can repeat under one object key;
* object associated data does not bind repository and object identity;
* a Vault root key leaves the Secrets Service plaintext;
* ordinary portable backups silently include secret values;
* a Vault Recovery Capsule is created without separate approval and recovery-key verification;
* a same-device recovery point is described as disaster recovery;
* a continuously user-writable destination is described as immutable;
* a cloud-synchronised folder is selected automatically;
* a backup is called Verified without naming its verification level;
* restore testing is absent;
* a backup is restored directly over the live data root;
* the Recovery Host can use external network by default;
* the Recovery Host can load plugins or MCP servers;
* restored packages execute before current signature and policy validation;
* restored workflows resume automatically;
* external effects retry automatically after restore;
* old approvals remain valid without revalidation;
* provider credentials or terms are assumed current;
* deleted data can be resurrected because the Erasure Ledger was not applied;
* portability import can grant authority or execute code;
* a Workspace Recovery Pack overwrites an existing repository silently;
* a restore can activate without a complete Recovery Report;
* root activation lacks a durable rollback path;
* a ransomware recovery writes into the suspected repository;
* a recovery exercise has never been performed for a claimed objective;
* or Trust Centre cannot explain destination, consistency, encryption, key recovery, verification, retention, exclusions, restore safety and RPO.

---

# 417. Acceptance Criteria

This ADR may move to **Accepted** when all of the following hold.

## Architecture and Ownership

* [ ] Backup and Recovery Service exists.
* [ ] Owner services remain authoritative.
* [ ] Every required owner has a Backup Adapter.
* [ ] Backup Service does not open live owner databases directly.
* [ ] Backup Service does not read Vault plaintext.
* [ ] Backup Service does not enumerate arbitrary project files.
* [ ] Backup Service does not execute restored packages.
* [ ] Backup Service does not resume workflows.
* [ ] Backup Service does not launch plugins.
* [ ] Backup Service does not launch MCP servers.
* [ ] Backup Service does not contact providers during restore validation.
* [ ] Stable, Preview and Development are isolated.
* [ ] Backup metadata works offline.
* [ ] No automatic cloud backup exists.
* [ ] No hidden external destination exists.
* [ ] No distributed transaction claim exists.
* [ ] Cross-service consistency is represented explicitly.
* [ ] Every full backup declares required and optional owners.
* [ ] Partial backups are labelled accurately.
* [ ] Selective restore eligibility is explicit.

## Backup State Classification

* [ ] Non-Rebuildable Critical State is defined.
* [ ] Non-Rebuildable User State is defined.
* [ ] Recoverable Project State is defined.
* [ ] Recoverable External State is defined.
* [ ] Rebuildable Derived State is defined.
* [ ] Installable Package and Cache State is defined.
* [ ] Secret State is defined.
* [ ] Trust and Recovery Evidence is defined.
* [ ] Ephemeral State is defined.
* [ ] Prohibited Backup Content is defined.
* [ ] Every owner inventories its state.
* [ ] Unknown owner state blocks a full backup.
* [ ] Rebuildable exclusions include a proven rebuild source.
* [ ] Non-rebuildable state cannot be marked rebuildable.
* [ ] Secret state cannot enter ordinary backup accidentally.
* [ ] Hidden model reasoning is prohibited.
* [ ] Process memory is prohibited unless explicitly selected as diagnostic evidence.

## Backup Product Definitions

* [ ] Backup Product Definition schema exists.
* [ ] Automatic Pre-Migration Recovery Point is defined.
* [ ] Automatic Pre-Update Recovery Point is defined.
* [ ] User-Initiated Local Recovery Point is defined.
* [ ] Scheduled Managed Backup is defined.
* [ ] User-Initiated Managed Backup is defined.
* [ ] Portable Full Backup is defined.
* [ ] Selective Project Backup is defined.
* [ ] Workspace Recovery Pack is defined.
* [ ] Vault Recovery Capsule is defined.
* [ ] Enterprise Recovery Backup is defined.
* [ ] Incident Preservation Backup is defined.
* [ ] Test and Exercise Backup is defined.
* [ ] Every product declares consistency requirements.
* [ ] Every product declares encryption requirements.
* [ ] Every product declares key-slot requirements.
* [ ] Every product declares verification requirements.
* [ ] Every product declares retention class.
* [ ] Every product declares restore modes.
* [ ] Product revisions are immutable.

## Backup Adapters

* [ ] Backup Adapter schema exists.
* [ ] Adapter ID is stable.
* [ ] Adapter revision is immutable.
* [ ] Owner service is bound.
* [ ] Supported owner schemas are bound.
* [ ] Database sources are explicit.
* [ ] Artefact sources are explicit.
* [ ] Mutable file sources are explicit.
* [ ] Exclusions are explicit.
* [ ] Prepare contract is explicit.
* [ ] Snapshot contract is explicit.
* [ ] Validate contract is explicit.
* [ ] Restore contract is explicit.
* [ ] Migration contract is explicit.
* [ ] Erasure contract is explicit.
* [ ] Dependencies are explicit.
* [ ] Limits are explicit.
* [ ] Implementation hash is bound.
* [ ] First-party adapters are implemented for every initial owner.
* [ ] Plugin adapters are not accepted initially.
* [ ] Unsupported schema blocks backup.
* [ ] Adapter version mismatch is visible.

## Backup Epochs and Participants

* [ ] Backup Epoch schema exists.
* [ ] Epoch ID is opaque.
* [ ] Backup Product revision is bound.
* [ ] Destination revision is bound.
* [ ] Requester is recorded.
* [ ] Reason is recorded.
* [ ] Required owners are recorded.
* [ ] Optional owners are recorded.
* [ ] Deadline is recorded.
* [ ] Barrier policy is recorded.
* [ ] Epoch hash is canonical.
* [ ] Participant discovery uses current service identity.
* [ ] Missing required owner fails or defers.
* [ ] Missing optional owner becomes an exclusion.
* [ ] Another release channel is rejected.
* [ ] Stale service identity is rejected.
* [ ] Epoch replay is rejected.
* [ ] Epoch state machine is durable.
* [ ] Cancellation is durable.
* [ ] Coordinator crash recovery works.

## Prepare Backup

* [ ] Prepare request is typed.
* [ ] Required state classes are bound.
* [ ] Destination class is supplied.
* [ ] Consistency requirement is supplied.
* [ ] Verification requirement is supplied.
* [ ] Owner response includes adapter revision.
* [ ] Owner response includes schema revision.
* [ ] Owner response includes state inventory.
* [ ] Owner response includes estimates.
* [ ] Owner response includes exclusions.
* [ ] Migration Active is handled.
* [ ] Database Unhealthy is handled.
* [ ] Exclusive Maintenance is handled.
* [ ] Unsupported Schema is handled.
* [ ] Insufficient Disk is handled.
* [ ] Owner Busy is handled.
* [ ] Owner Unavailable is handled.
* [ ] Secret Rotation Active is handled.
* [ ] Policy Denied is handled.
* [ ] Unknown prepare failure does not produce a Complete backup.

## Consistency Barrier

* [ ] Consistency Barrier is a trusted Runtime primitive.
* [ ] Barrier scope is explicit.
* [ ] New mutating-command admission stops.
* [ ] Existing short transactions drain.
* [ ] Owner outboxes flush.
* [ ] Owner checkpoints commit.
* [ ] Database snapshots establish.
* [ ] Artefacts pin.
* [ ] Barrier releases before full copy.
* [ ] Reads may continue where safe.
* [ ] Ordinary barrier has a five-second maximum initially.
* [ ] Migration barrier has an explicit maintenance maximum.
* [ ] Barrier timeout releases every participant.
* [ ] Coordinator crash releases every participant.
* [ ] Service crash does not leave other services permanently blocked.
* [ ] External operations are represented by durable state.
* [ ] UI exposes material delay.
* [ ] Mutation-flood tests pass.
* [ ] Barrier never claims atomic external effects.

## Owner Checkpoints

* [ ] Owner Checkpoint schema exists.
* [ ] Owner identity is bound.
* [ ] Service version is bound.
* [ ] Schema version is bound.
* [ ] Adapter revision is bound.
* [ ] Checkpoint ID is opaque.
* [ ] Checkpoint sequence is monotonic.
* [ ] Database revisions are recorded.
* [ ] Outbox position is recorded.
* [ ] Inbox position is recorded.
* [ ] CAS root is recorded.
* [ ] Mutable-file generation is recorded.
* [ ] Active migrations are recorded.
* [ ] Unresolved operations are recorded.
* [ ] Checkpoint time is UTC.
* [ ] Checkpoint SHA-256 is canonical.
* [ ] Checkpoint forgery is detected.
* [ ] Old checkpoint replay is detected.
* [ ] Restore can reconcile outbox and inbox boundaries.

## SQLite Backup

* [ ] SQLite Online Backup API is the normal mechanism.
* [ ] Exact pinned SQLite library is used.
* [ ] Source identity is verified.
* [ ] Destination is a new staging file.
* [ ] `sqlite3_backup_init` is checked.
* [ ] `sqlite3_backup_step` is incremental.
* [ ] `sqlite3_backup_finish` is checked.
* [ ] Busy handling is bounded.
* [ ] Progress is reported.
* [ ] Cancellation discards incomplete destination.
* [ ] Destination is flushed.
* [ ] Destination is reopened read only.
* [ ] Schema version is validated.
* [ ] User version is validated.
* [ ] Quick check runs.
* [ ] Foreign key check runs.
* [ ] Owner semantic validator runs.
* [ ] Finished snapshot is self-contained.
* [ ] Live WAL is not shipped as the normal backup.
* [ ] Ordinary raw file copy is prohibited.
* [ ] Raw copy of `.db` only is detected in tests.
* [ ] Wrong WAL pairing is detected.
* [ ] Missing hot journal is detected in adversarial tests.
* [ ] Existing destination file is denied.
* [ ] Reparse destination is denied.
* [ ] Disk-full behaviour is safe.
* [ ] Source crash behaviour is understood.
* [ ] Power-loss tests pass.

## `VACUUM INTO`

* [ ] Use is adapter controlled.
* [ ] Destination must be new.
* [ ] Free-space requirement is checked.
* [ ] Output is validated.
* [ ] Interrupted output is discarded.
* [ ] Deleted free-page content removal is tested.
* [ ] Privacy benefit and I/O cost are documented.
* [ ] It is not assumed to replace every Online Backup use.
* [ ] URI filenames are denied or strictly controlled.
* [ ] Concurrent-write behaviour is tested.

## VSS

* [ ] VSS is documented as deferred integration.
* [ ] Raw VSS snapshot is not labelled Opure application consistent.
* [ ] Enterprise system backups are recognised as defence in depth.
* [ ] A future VSS writer requires a separate accepted design.
* [ ] Opure can expose safe backup-health metadata for external tools.
* [ ] DiskShadow is not required on Windows client.
* [ ] Restore from external system backup still runs Opure validation.
* [ ] VSS limitations are visible.

## Artefact and CAS Snapshots

* [ ] CAS manifest includes namespace.
* [ ] CAS root is exact.
* [ ] Object IDs are exact.
* [ ] Object hashes are exact.
* [ ] Object sizes are exact.
* [ ] Classification is exact.
* [ ] Pin lease is issued.
* [ ] GC respects pin lease.
* [ ] Cancellation releases pin.
* [ ] Owner crash does not leak permanent pin.
* [ ] Missing object fails affected owner.
* [ ] Wrong object hash fails.
* [ ] Mutable files become immutable staged snapshots.
* [ ] Direct mutable copy is not used where consistency matters.
* [ ] Package manifests include publisher and source.
* [ ] Rebuildable exclusions are fully explained.

## Destinations

* [ ] Backup Destination schema exists.
* [ ] Destination ID is opaque.
* [ ] Destination revision is immutable.
* [ ] Destination type is explicit.
* [ ] Logical location is explicit.
* [ ] Filesystem identity is verified.
* [ ] Capacity is measured.
* [ ] Free space is measured.
* [ ] Removable state is known where possible.
* [ ] Network state is known where possible.
* [ ] Cloud-synchronised state is known where possible.
* [ ] Continuous write access is represented.
* [ ] Offline capability is represented.
* [ ] Immutability is not guessed.
* [ ] Encryption requirement is represented.
* [ ] Credential reference is Vault bound.
* [ ] Health is represented.
* [ ] Same physical device is detected where possible.
* [ ] Unknown physical relationship is shown as Unknown.
* [ ] Same-device destination is labelled.
* [ ] Separate device is not labelled immutable.
* [ ] Network shares require policy.
* [ ] Network interruption is resumable.
* [ ] Shared mutable SQLite repository indexes are avoided.
* [ ] Cloud-sync completion is not claimed.
* [ ] Removable safe-eject flow flushes data.
* [ ] Destination substitution is detected.
* [ ] Safety reserve is enforced provisionally.
* [ ] Low-space state is visible.
* [ ] Destination missing state is visible.
* [ ] Permission failure is visible.

## Backup Repository

* [ ] Repository schema exists.
* [ ] Repository ID is opaque.
* [ ] Format revision is explicit.
* [ ] Chunk size is explicit.
* [ ] Object-ID scheme is explicit.
* [ ] Encryption scheme is explicit.
* [ ] KDF schemes are explicit.
* [ ] Key slots are explicit.
* [ ] Retention policy is explicit.
* [ ] Health is explicit.
* [ ] Header hash is canonical.
* [ ] Outer metadata contains no project names.
* [ ] Outer metadata contains no absolute paths.
* [ ] Outer metadata contains no secret names.
* [ ] One writer is enforced.
* [ ] Lock includes process and boot identity.
* [ ] Stale lock recovery is safe.
* [ ] Repository can be verified read only.
* [ ] Repository index can be rebuilt.
* [ ] Repository corruption quarantines.
* [ ] Previous committed snapshots survive interrupted writes.
* [ ] Commit marker is written last.
* [ ] Missing commit marker means incomplete.
* [ ] Snapshot manifests are synthetic full.
* [ ] Parent manifest is not required for restore.
* [ ] Object reference counts are rebuildable.
* [ ] Repository copy preserves all objects and commit markers.

## Chunking and Object Identity

* [ ] Initial chunk size is 4 MiB.
* [ ] Final partial chunk is supported.
* [ ] Chunking is streaming.
* [ ] Chunking memory is bounded.
* [ ] Cancellation is supported.
* [ ] Object content hash uses SHA-256.
* [ ] Public object filename is keyed.
* [ ] Keyed ID uses HMAC-SHA-256.
* [ ] Repository identity is part of object context.
* [ ] Plaintext equality is not exposed across repositories by filenames.
* [ ] Collision handling is explicit.
* [ ] Zero-byte objects work.
* [ ] Very large objects work.
* [ ] Chunk order is authenticated.
* [ ] Chunk truncation is detected.
* [ ] Chunk substitution is detected.
* [ ] Chunk duplication is handled.
* [ ] Compression is before encryption where selected.
* [ ] Already compressed data uses Store.
* [ ] Compression algorithm is versioned.
* [ ] Compression limits prevent resource exhaustion.

## Encryption

* [ ] Encryption Profile schema exists.
* [ ] Repository Master Key is 256 random bits.
* [ ] OS cryptographic RNG is used.
* [ ] Master Key is never persisted plaintext.
* [ ] Master Key is cleared from memory where practical.
* [ ] HKDF-SHA-256 is used for purpose separation.
* [ ] Domain labels are unique.
* [ ] AES-256-GCM is used.
* [ ] GCM nonce is 96 random bits.
* [ ] GCM tag is 128 bits.
* [ ] Associated data binds format revision.
* [ ] Associated data binds repository ID.
* [ ] Associated data binds object ID.
* [ ] Associated data binds object type.
* [ ] Nonce reuse detection exists.
* [ ] Nonce RNG failure stops backup.
* [ ] Authentication failure stops restore.
* [ ] Known-answer cryptographic tests pass.
* [ ] Master-key rotation is supported.
* [ ] Key version is bound per object.
* [ ] Existing key versions remain restorable.
* [ ] Encryption is not described as source trust.

## DPAPI Key Slot

* [ ] DPAPI CurrentUser slot exists.
* [ ] Slot is produced by trusted cryptography boundary.
* [ ] Slot includes repository ID context.
* [ ] Optional entropy version is controlled.
* [ ] Same-user restore works.
* [ ] Another-user restore fails.
* [ ] New-machine behaviour is tested.
* [ ] DPAPI failure is actionable.
* [ ] LocalMachine scope is not selected.
* [ ] UI states normal machine binding.
* [ ] DPAPI-only repository is labelled No Portable Key.
* [ ] Device-loss objective is not met by DPAPI-only access.
* [ ] DPAPI blob is not a portable secret claim.

## Passphrase Key Slot

* [ ] Passphrase slot exists.
* [ ] PBKDF2-HMAC-SHA-256 is used provisionally.
* [ ] .NET one-shot `Rfc2898DeriveBytes.Pbkdf2` is used.
* [ ] Instance constructors are not used.
* [ ] Salt is 128 random bits.
* [ ] Derived key is 256 bits.
* [ ] Exact iteration count is stored.
* [ ] Calibration target is implemented provisionally.
* [ ] Minimum iteration policy is enforced.
* [ ] Maximum restore-work policy prevents denial of service.
* [ ] Empty passphrase is denied.
* [ ] Passphrase is confirmed.
* [ ] Passphrase is never stored.
* [ ] Passphrase is not accepted as ordinary CLI argument.
* [ ] Unicode encoding is specified.
* [ ] Normalisation policy is specified.
* [ ] Wrong passphrase does not reveal detailed oracle information.
* [ ] KDF revision is versioned.
* [ ] Future memory-hard KDF can be added.
* [ ] Weak KDF downgrade is rejected.
* [ ] NIST revision status is tracked.

## Random Recovery Key Slot

* [ ] Random Recovery Key is 256 bits.
* [ ] Recovery key file has a versioned format.
* [ ] Recovery key file binds repository ID.
* [ ] Recovery key file has a checksum.
* [ ] Fingerprint is displayed.
* [ ] File is saved to a separately selected destination.
* [ ] Co-location inside repository warns or blocks.
* [ ] Possession is verified.
* [ ] Wrong repository fails.
* [ ] Corrupted key fails.
* [ ] Recovery key is never logged.
* [ ] Recovery key is never included in Trust Evidence.
* [ ] Last portable slot cannot be removed accidentally.
* [ ] Losing all slots is explained as unrecoverable.

## Key Slot Lifecycle

* [ ] Add works.
* [ ] Verify works.
* [ ] Replace works.
* [ ] Disable works.
* [ ] Remove works.
* [ ] Rotate works.
* [ ] Recover works.
* [ ] Every slot has a fingerprint.
* [ ] Every slot has a creation time.
* [ ] Slot changes create Trust Evidence.
* [ ] Slot metadata substitution is detected.
* [ ] Wrapped-key substitution is detected.
* [ ] Nonce substitution is detected.
* [ ] Last usable slot protection works.
* [ ] Key recovery health is visible.
* [ ] Portable slot verification is periodically requested.

## Manifests and Commit

* [ ] Backup Manifest schema exists.
* [ ] Manifest canonicalisation is deterministic.
* [ ] Manifest is encrypted.
* [ ] Manifest plaintext hash is authenticated.
* [ ] Backup ID is bound.
* [ ] Backup class is bound.
* [ ] Repository is bound.
* [ ] Product version is bound.
* [ ] Release channel is bound.
* [ ] Package identity is bound.
* [ ] Backup Epoch is bound.
* [ ] Consistency state is bound.
* [ ] Owner checkpoints are bound.
* [ ] Database snapshots are bound.
* [ ] Artefact manifests are bound.
* [ ] Workspace Packs are bound.
* [ ] Package manifests are bound.
* [ ] Vault Capsule reference is bound.
* [ ] Retention policy is bound.
* [ ] Encryption profile is bound.
* [ ] Key-slot fingerprints are bound.
* [ ] Data classifications are bound.
* [ ] Exclusions are bound.
* [ ] Warnings are bound.
* [ ] Verification state is bound.
* [ ] Restore requirements are bound.
* [ ] Commit Marker schema exists.
* [ ] Commit marker is written last.
* [ ] Commit marker binds manifest ciphertext.
* [ ] Incomplete snapshots are ignored.
* [ ] Manifest substitution across repositories fails.
* [ ] Manifest substitution across snapshots fails.
* [ ] Repository rollback is detectable where history is available.

## Backup Consistency States

* [ ] Preparing is implemented.
* [ ] Barrier Active is implemented.
* [ ] Snapshot Established is implemented.
* [ ] Copying is implemented.
* [ ] Complete is implemented.
* [ ] Complete with Rebuildable Exclusions is implemented.
* [ ] Partial is implemented.
* [ ] Deferred is implemented.
* [ ] Failed is implemented.
* [ ] Cancelled is implemented.
* [ ] Quarantined is implemented.
* [ ] Deleted is implemented.
* [ ] Full restore accepts only complete eligible states.
* [ ] Selective restore checks owner completeness.
* [ ] UI never hides partial state.
* [ ] Backup health reflects latest complete state, not latest attempt only.

## Scheduling and Admission

* [ ] Backup Schedule schema exists.
* [ ] Frequency is explicit.
* [ ] Time window is explicit.
* [ ] Idle requirement is explicit.
* [ ] Power requirement is explicit.
* [ ] Network requirement is explicit.
* [ ] Retention is explicit.
* [ ] Verification schedule is explicit.
* [ ] Missed-run policy is explicit.
* [ ] Notification policy is explicit.
* [ ] Next run is shown with exact date and time.
* [ ] DST transition is tested.
* [ ] Sleep and hibernation are tested.
* [ ] AC-only policy works.
* [ ] Battery threshold works.
* [ ] Metered network policy works.
* [ ] Destination health is checked.
* [ ] CPU and memory budgets are checked.
* [ ] Foreground work is respected.
* [ ] Mandatory migration backup has priority.
* [ ] User-initiated backup has foreground visibility.
* [ ] Missed backup becomes overdue visibly.
* [ ] No schedule exists until explicitly configured.
* [ ] No cloud path is chosen automatically.

## Cancellation, Resume and Retry

* [ ] Backup cancellation is durable.
* [ ] Barrier releases on cancellation.
* [ ] Incomplete SQLite destination is deleted.
* [ ] Incomplete object is not committed.
* [ ] Previous committed snapshots remain.
* [ ] Completed staged database snapshot can resume repository copy.
* [ ] Interrupted live snapshot starts a new Epoch.
* [ ] Source checkpoint changes create a new backup identity.
* [ ] Temporary destination failure is retryable.
* [ ] Transient network failure is retryable.
* [ ] Hash conflict is not retried blindly.
* [ ] Key failure is not retried blindly.
* [ ] Source corruption is not retried blindly.
* [ ] Policy denial is not retried.
* [ ] Suspected tampering quarantines.
* [ ] Retry budgets are bounded.
* [ ] Resume state is verified before use.

## Local Recovery Points

* [ ] Local Recovery Point product exists.
* [ ] It is stored outside live owner roots.
* [ ] It is encrypted or equivalently protected.
* [ ] It is labelled Same-Device Recovery Only.
* [ ] It has short retention.
* [ ] Pre-migration point is mandatory before irreversible migration.
* [ ] Structural Verification completes before migration.
* [ ] Pre-update point is mandatory where state migration may occur.
* [ ] Recent equivalent point may be reused only under exact policy.
* [ ] Failed recovery point stops destructive migration.
* [ ] Recovery point cleanup preserves current rollback.
* [ ] User-pinned point is retained.
* [ ] Local point is not counted as off-device RPO.
* [ ] Local point can be restored through the same staged process.

## Workspace Recovery Packs

* [ ] Workspace Recovery Pack schema exists.
* [ ] Project identity is bound.
* [ ] Repository identity is bound.
* [ ] Base commit is bound.
* [ ] Branch metadata is recorded.
* [ ] Tracked patch is deterministic.
* [ ] Staged patch is separate.
* [ ] Unstaged patch is separate.
* [ ] Selected untracked files are hashed.
* [ ] Deleted files are represented.
* [ ] File modes are represented.
* [ ] Line endings are represented.
* [ ] Classifications are represented.
* [ ] Ignored files are excluded by default.
* [ ] Ignored files require explicit inclusion.
* [ ] Secret scan runs on untracked files.
* [ ] Binary changes require explicit handling.
* [ ] Non-Git projects are supported selectively.
* [ ] Full project snapshot is explicit.
* [ ] Restore target defaults to new worktree, branch or directory.
* [ ] Existing working tree is never overwritten silently.
* [ ] Base commit is verified where possible.
* [ ] Conflicts are previewed.
* [ ] Restore never pushes.
* [ ] Restore never commits without explicit request.
* [ ] Pack remains until verification.

## Vault Recovery Capsules

* [ ] Vault Recovery Capsule schema exists.
* [ ] Capsule is disabled by default.
* [ ] Capsule has separate approval.
* [ ] Capsule has separate retention.
* [ ] Capsule has separate key slots.
* [ ] Capsule inventory is safe and bounded.
* [ ] Vault database snapshot is created by Secrets Service.
* [ ] Vault root key is unwrapped only inside Secrets Service.
* [ ] Vault root key is rewrapped under Capsule Master Key.
* [ ] Backup Service never receives plaintext root key.
* [ ] Capsule uses AES-256-GCM.
* [ ] Capsule uses HKDF purpose separation.
* [ ] Capsule contains no plaintext secrets.
* [ ] Capsule staging is short lived.
* [ ] Capsule verification includes isolated Secrets restore.
* [ ] Portable backup does not include capsule unless explicit.
* [ ] Unattended capsule creation is denied by default.
* [ ] New-machine restore re-protects root key with destination CurrentUser DPAPI.
* [ ] Expired or revoked external tokens remain subject to reauthentication.
* [ ] Forgotten passphrase has no bypass.
* [ ] Capsule and recovery key co-location warning exists.
* [ ] Capsule secret-canary tests pass.

## Data Portability Export

* [ ] Data Portability Bundle schema exists.
* [ ] Bundle extension is defined.
* [ ] Manifest is versioned.
* [ ] Export time is recorded.
* [ ] Product version is recorded.
* [ ] Scope is recorded.
* [ ] Data categories are recorded.
* [ ] Schemas are included or referenced.
* [ ] File hashes are included.
* [ ] Classifications are included.
* [ ] Redactions are included.
* [ ] Exclusions are included.
* [ ] Import requirements are included.
* [ ] JSON is used for structured data.
* [ ] JSON Lines is used for record streams.
* [ ] Markdown is used for human-readable material.
* [ ] CSV is limited to genuine tabular data.
* [ ] Unified diff or Git-compatible patch is used for changes.
* [ ] Profiles can be exported.
* [ ] Project metadata can be exported.
* [ ] Custom Workflow Definitions can be exported.
* [ ] Project Memory can be exported with provenance.
* [ ] User notes can be exported.
* [ ] Provider Profile metadata excludes credentials.
* [ ] Model Profile metadata includes hashes.
* [ ] Plugin configuration excludes grants.
* [ ] MCP configuration excludes credentials and trust.
* [ ] Trust Evidence can be exported safely.
* [ ] Workspace Packs can be included.
* [ ] Raw databases are excluded.
* [ ] WAL files are excluded.
* [ ] Active queues are excluded.
* [ ] Leases are excluded.
* [ ] Capabilities are excluded.
* [ ] One-time approvals are excluded.
* [ ] Secrets are excluded.
* [ ] DPAPI blobs are excluded.
* [ ] Executable packages are excluded.
* [ ] Automatically resumable effects are excluded.
* [ ] Export is structured and machine readable.
* [ ] Product does not claim universal third-party interoperability.
* [ ] Export preview is complete.

## Data Portability Import

* [ ] Import runs in dedicated mode.
* [ ] Strict archive-path checks run.
* [ ] Hashes are verified.
* [ ] Schemas are validated.
* [ ] Secret scan runs.
* [ ] Sizes are bounded.
* [ ] Future unsupported schema is rejected.
* [ ] Profile IDs are mapped.
* [ ] Project IDs are mapped.
* [ ] Workflow IDs are mapped.
* [ ] Provider references are mapped.
* [ ] Model references are mapped.
* [ ] Plugin references are mapped.
* [ ] MCP references are mapped.
* [ ] Memory identities are mapped.
* [ ] ID collisions are explicit.
* [ ] Imported Profiles become candidates.
* [ ] Imported workflows remain disabled templates.
* [ ] Plugin references remain untrusted.
* [ ] MCP references remain untrusted.
* [ ] Missing secrets are visible.
* [ ] No code executes.
* [ ] No plugin launches.
* [ ] No MCP server launches.
* [ ] No workflow resumes.
* [ ] No provider call occurs.
* [ ] No patch applies.
* [ ] No repository overwrites.
* [ ] No permission is granted.
* [ ] Import history is retained.
* [ ] Import provenance is retained.

## Restore Plans

* [ ] Restore Plan schema exists.
* [ ] Source backup is exact.
* [ ] Source verification level is bound.
* [ ] Source product version is bound.
* [ ] Destination channel is bound.
* [ ] Restore mode is bound.
* [ ] Selected owners are bound.
* [ ] Selected projects are bound.
* [ ] Path mappings are bound.
* [ ] Identity mappings are bound.
* [ ] Package mappings are bound.
* [ ] Secret policy is bound.
* [ ] Workflow policy is bound.
* [ ] Network policy is bound.
* [ ] Migration plan is bound.
* [ ] Erasure plan is bound.
* [ ] Activation plan is bound.
* [ ] Rollback plan is bound.
* [ ] Preview hash is canonical.
* [ ] Approval is exact.
* [ ] Approval expires.
* [ ] Source changes invalidate approval.
* [ ] Mapping changes invalidate approval.
* [ ] Policy changes invalidate approval.
* [ ] Package-validation changes invalidate approval.
* [ ] Workflow-risk changes invalidate approval.
* [ ] Restore plan cannot broaden current policy.

## Recovery Host

* [ ] Dedicated Recovery Host exists.
* [ ] Host binary is signed and verified.
* [ ] Host process is supervised.
* [ ] Host has minimal environment.
* [ ] Host working directory is recovery staging.
* [ ] Network is disabled by default.
* [ ] Plugins are unavailable.
* [ ] MCP is unavailable.
* [ ] Model execution is unavailable.
* [ ] Tool execution is unavailable.
* [ ] Project write outside staging is unavailable.
* [ ] Child process creation is denied where compatible.
* [ ] Job Object is used.
* [ ] Exact source handle is used.
* [ ] Exact destination handle is used.
* [ ] Backup is treated as untrusted input.
* [ ] Manifest size is bounded.
* [ ] Object count is bounded.
* [ ] Path length is bounded.
* [ ] Decompression ratio is bounded.
* [ ] Database count is bounded.
* [ ] Output size is bounded.
* [ ] Executable files are never run.
* [ ] Diagnostic network capture proves no hidden network.

## Backup Version Compatibility

* [ ] Directly Supported is implemented.
* [ ] Supported with Migration is implemented.
* [ ] Requires Intermediate Opure Version is implemented.
* [ ] Newer Unsupported is implemented.
* [ ] Too Old Unsupported is implemented.
* [ ] Corrupt or Unknown is implemented.
* [ ] Old binary from backup is never executed.
* [ ] Intermediate installer is separately signed and verified.
* [ ] Downgrade is not attempted against newer state.
* [ ] Migrations run only on staged copies.
* [ ] Historical backup remains unchanged.
* [ ] Migration failure preserves source and active root.

## Staged Restore

* [ ] Restore always uses a new data root.
* [ ] Existing live root is never overwritten in place.
* [ ] Staged root uses restrictive ACLs.
* [ ] Staged root is on validated storage.
* [ ] Free-space estimate runs first.
* [ ] Database objects are decrypted and reconstructed.
* [ ] Database hashes are verified.
* [ ] Databases open read only before migration.
* [ ] Quick checks pass.
* [ ] Foreign keys pass.
* [ ] Owner schemas pass.
* [ ] Owner semantic validation passes.
* [ ] CAS objects are reconstructed and hashed.
* [ ] Package bytes remain quarantined.
* [ ] Package signatures are revalidated.
* [ ] Publisher trust is revalidated.
* [ ] Package source is revalidated.
* [ ] Model hashes are revalidated.
* [ ] Runtime compatibility is revalidated.
* [ ] Provider terms and region are revalidated.
* [ ] Project roots are remapped.
* [ ] Identity mapping is explicit.
* [ ] Current Product Policy is applied.
* [ ] Current Enterprise Policy is applied.
* [ ] Effective Configuration is rebuilt.
* [ ] Recovery Report is created before activation.

## Plugin, MCP and Package Restore

* [ ] Plugin package bytes are quarantined.
* [ ] Plugin package format is revalidated.
* [ ] Plugin package signature is revalidated.
* [ ] Plugin publisher is revalidated.
* [ ] Plugin source is revalidated.
* [ ] Plugin current policy is revalidated.
* [ ] Plugin remains disabled on new-machine restore.
* [ ] Fresh permission review is required on new machine.
* [ ] Same-machine compatible grants require exact package and policy identity.
* [ ] MCP configurations restore disabled.
* [ ] MCP fingerprint is revalidated.
* [ ] MCP account is revalidated.
* [ ] MCP credentials are reacquired.
* [ ] MCP server does not launch during validation.
* [ ] Provider Profiles restore as metadata.
* [ ] Provider credentials are reacquired unless valid capsule restored.
* [ ] Provider terms are revalidated.
* [ ] Provider region is revalidated.
* [ ] Model packages do not load during restore validation.
* [ ] Update packages do not execute from backup.
* [ ] Installer location does not imply trust.

## Workflow Restore

* [ ] Workflow Definitions restore.
* [ ] Compiled Plans restore.
* [ ] Workflow Events restore.
* [ ] Checkpoints restore.
* [ ] Timers restore without firing.
* [ ] Signals restore.
* [ ] Approvals restore as historical evidence.
* [ ] Effect intents restore.
* [ ] Effect receipts restore.
* [ ] Compensation state restores.
* [ ] Recovery Decisions restore.
* [ ] Required CAS objects restore.
* [ ] Every non-terminal workflow becomes Recovery Required.
* [ ] Pure pending work does not auto-dispatch.
* [ ] Timers do not fire before review.
* [ ] Signals do not trigger before review.
* [ ] Approvals are revalidated.
* [ ] Provider decisions are revalidated.
* [ ] Plugin and MCP identities are revalidated.
* [ ] External effects are reconciled.
* [ ] Outcome Unknown remains visible.
* [ ] No effect is automatically retried.
* [ ] No workflow completes solely because of restore.
* [ ] Developer explicitly resumes eligible work.

## Project and Workspace Restore

* [ ] Every project requires mapping.
* [ ] Same path string is not sufficient.
* [ ] Repository identity is checked.
* [ ] Missing project is marked unavailable.
* [ ] Clone is a separate operation after validation.
* [ ] Cross-project restore is denied by default.
* [ ] Project ID collision is explicit.
* [ ] Selective restore can create a new project identity.
* [ ] Workspace Pack applies only after preview.
* [ ] Existing changes are protected.
* [ ] New branch or worktree is default.
* [ ] No automatic commit occurs.
* [ ] No automatic push occurs.
* [ ] Source-controlled configuration revalidates.
* [ ] Ignored secret files do not restore silently.

## New-Machine Restore

* [ ] Signed Opure install is required.
* [ ] Correct release channel is selected.
* [ ] Portable key slot is required for encrypted repository.
* [ ] DPAPI-only limitation is shown.
* [ ] Machine-specific settings are recalculated.
* [ ] Hardware profiles are requalified.
* [ ] GPU and model compatibility are rechecked.
* [ ] Project roots are mapped.
* [ ] Packages are revalidated.
* [ ] Credentials are re-entered or recovered.
* [ ] Enterprise policy is read from destination machine.
* [ ] User policy is read from destination user.
* [ ] Vault root key is reprotected under destination DPAPI.
* [ ] Missing credentials do not block safe metadata restore.
* [ ] Missing credentials block dependent operations.
* [ ] Recovery report lists every missing dependency.

## Erasure Ledger

* [ ] Erasure Ledger schema exists.
* [ ] Erasure ID is opaque.
* [ ] Owner service is bound.
* [ ] Scope is bound.
* [ ] Selectors are typed.
* [ ] Safe fingerprints are used.
* [ ] Deleted time is recorded.
* [ ] Reason is recorded.
* [ ] Backup policy is recorded.
* [ ] Restore policy is recorded.
* [ ] Expiry is explicit.
* [ ] Erasure record hash is canonical.
* [ ] User deletion creates erasure evidence.
* [ ] Project deletion creates erasure evidence.
* [ ] Privacy deletion creates erasure evidence.
* [ ] Retention expiry creates erasure evidence where needed.
* [ ] Secret rotation creates appropriate old-secret deletion state.
* [ ] New backups exclude active erasures.
* [ ] Older backup restore loads current erasures.
* [ ] Owner purge adapters run on staged state.
* [ ] CAS references are removed.
* [ ] Deletion is verified where practical.
* [ ] Critical unresolved erasure blocks activation.
* [ ] Immutable backup retention is explained.
* [ ] Beyond-use state is enforced.
* [ ] Old backups cannot routinely resurrect deleted data.
* [ ] Forensic erasure is not promised.

## Root Activation and Rollback

* [ ] Restore Activation Plan exists.
* [ ] Service dependency order is explicit.
* [ ] Network gates are explicit.
* [ ] Plugin gates are explicit.
* [ ] MCP gates are explicit.
* [ ] Workflow gates are explicit.
* [ ] Project-write gates are explicit.
* [ ] Health checks are explicit.
* [ ] Confirmation deadline is explicit.
* [ ] Desktop stops before switch.
* [ ] Runtime stops before switch.
* [ ] Current root flushes.
* [ ] Activation intent is durable.
* [ ] Previous root identity is retained.
* [ ] Staged root becomes current atomically where supported.
* [ ] Root metadata flushes.
* [ ] Minimal Runtime starts.
* [ ] Recovery Validation Mode starts.
* [ ] Validation succeeds before ordinary mode.
* [ ] Power loss at every boundary recovers deterministically.
* [ ] Failed validation triggers rollback option.
* [ ] Prior root remains until confirmation and retention.
* [ ] No external effect occurs before confirmation.
* [ ] Rollback can restore prior root.
* [ ] Rollback failure enters Recovery Required.
* [ ] New local writes are blocked or captured during validation.

## Recovery Validation Mode

* [ ] Database reads work.
* [ ] Integrity checks work.
* [ ] Configuration resolution works.
* [ ] Project mapping works.
* [ ] Package verification works.
* [ ] Trust Centre works.
* [ ] External network is denied.
* [ ] Model inference is denied.
* [ ] Tools are denied.
* [ ] Workspace writes are denied.
* [ ] Repository writes are denied.
* [ ] Plugin execution is denied.
* [ ] MCP execution is denied.
* [ ] Update execution is denied.
* [ ] Workflow dispatch is denied.
* [ ] Provider calls are denied.
* [ ] Developer can review missing dependencies.
* [ ] Developer can confirm.
* [ ] Developer can roll back.
* [ ] Timeout remains in Recovery Validation Mode.

## Restore Modes

* [ ] Full In-Place Replacement is staged safely.
* [ ] Side-by-Side Full Restore works.
* [ ] Selective Service Restore works only with dependencies.
* [ ] Selective Project Restore works.
* [ ] Workspace Pack Restore works.
* [ ] Metadata-Only Restore works.
* [ ] New Machine Restore works.
* [ ] Clean-Room Restore works.
* [ ] Incident Examination works read only.
* [ ] Portability Import remains non-authoritative.
* [ ] Each mode has distinct policy and UI.
* [ ] Selective Workflow restore dependencies are checked.
* [ ] Selective Vault restore uses Secrets Service.
* [ ] Cross-channel restore becomes migration candidate.

## Recovery Objectives

* [ ] Process-crash RPO is documented.
* [ ] Process-crash RTO is documented.
* [ ] Desktop-crash RPO is documented.
* [ ] Desktop-crash RTO is documented.
* [ ] Update-failure RPO is documented.
* [ ] Update-failure RTO is documented.
* [ ] Single-database-corruption RPO is documented.
* [ ] Single-database-corruption RTO is documented.
* [ ] Whole-data-root-corruption RPO is documented.
* [ ] Whole-data-root-corruption RTO is documented.
* [ ] Device-loss RPO is documented.
* [ ] Device-loss RTO is documented.
* [ ] Ransomware RPO is documented.
* [ ] Ransomware RTO is documented as risk dependent.
* [ ] Credential-loss RPO is documented.
* [ ] Credential-loss dependency on capsule is clear.
* [ ] Backup Health estimates current RPO from actual schedule.
* [ ] Same-device backup does not satisfy device-loss RPO.
* [ ] Continuously writable backup does not satisfy offline objective.
* [ ] Objectives are described as targets, not guarantees.

## Disaster Recovery Plans

* [ ] Disaster Recovery Plan schema exists.
* [ ] Failure scenarios are explicit.
* [ ] Critical services are explicit.
* [ ] Dependencies are explicit.
* [ ] RPO is explicit.
* [ ] RTO is explicit.
* [ ] Backup sources are explicit.
* [ ] Recovery order is explicit.
* [ ] Clean-environment requirements are explicit.
* [ ] Credential requirements are explicit.
* [ ] Communication is explicit.
* [ ] Validation is explicit.
* [ ] Exercise schedule is explicit.
* [ ] Owners are explicit.
* [ ] Plan revision is immutable.
* [ ] Individual-developer BIA exists.
* [ ] Enterprise BIA can be configured.
* [ ] Recovery Tier 0 is defined.
* [ ] Recovery Tier 1 is defined.
* [ ] Recovery Tier 2 is defined.
* [ ] Recovery Tier 3 is defined.
* [ ] Recovery Tier 4 is defined.
* [ ] Recovery Tier 5 is defined.
* [ ] Manual alternatives are documented where applicable.
* [ ] Plan is reviewed after exercise and incident.

## Clean-Room and Ransomware Recovery

* [ ] Clean Windows environment is required.
* [ ] Signed Opure installer is required.
* [ ] Network is isolated initially.
* [ ] Backup source is mounted read only where possible.
* [ ] Original repository is not written.
* [ ] Plugins are disabled.
* [ ] MCP is disabled.
* [ ] Package execution is disabled.
* [ ] Workflow dispatch is disabled.
* [ ] Credential use is disabled initially.
* [ ] Backup age is not treated as cleanliness proof.
* [ ] All objects are verified.
* [ ] Packages are revalidated against current trust.
* [ ] Source and data can still be malicious.
* [ ] Current compromised device is not trusted.
* [ ] Known-clean backup selection is documented.
* [ ] Incident window is considered.
* [ ] Recovery proceeds in tiers.
* [ ] External network opens only after review.
* [ ] Ransomware adversarial exercise passes.
* [ ] Offline or isolated copy guidance is visible.
* [ ] Ordinary user-writable folder is not called ransomware proof.

## Verification Levels

* [ ] Created level exists.
* [ ] Cryptographically Verified level exists.
* [ ] Structurally Verified level exists.
* [ ] Semantically Verified level exists.
* [ ] Selectively Restore Tested level exists.
* [ ] Fully Restore Tested level exists.
* [ ] Recovery Exercised level exists.
* [ ] UI always names exact level.
* [ ] Created does not imply readable.
* [ ] Cryptographic verification checks key slot.
* [ ] Cryptographic verification checks manifest AEAD.
* [ ] Cryptographic verification checks object AEAD.
* [ ] Cryptographic verification checks plaintext hashes.
* [ ] Structural verification opens every selected database.
* [ ] Structural verification validates schemas.
* [ ] Structural verification validates object graph.
* [ ] Semantic verification uses owner validators.
* [ ] Selective restore test uses disposable isolated root.
* [ ] Full restore test covers every included owner.
* [ ] Recovery exercise includes human process and objectives.
* [ ] Verification tool versions are recorded.
* [ ] Verification failures mark affected snapshots.
* [ ] Verification does not prove source cleanliness.
* [ ] Verification freshness is visible.

## Restore Testing

* [ ] Cryptographic verification runs on write.
* [ ] Structural verification runs on every snapshot.
* [ ] Critical-owner semantic validation runs on every snapshot.
* [ ] Selective restore test can be scheduled weekly.
* [ ] Full restore test can be scheduled monthly.
* [ ] Enterprise exercise can be scheduled quarterly.
* [ ] Individual user can run exercises manually.
* [ ] Restore test uses no external network.
* [ ] Restore test uses no plugins.
* [ ] Restore test uses no MCP.
* [ ] Restore test uses no workflow resume.
* [ ] Restore test uses no project write.
* [ ] Restore test uses no provider call.
* [ ] Restore test has bounded CPU, memory and disk.
* [ ] Disposable root is deleted after evidence retention.
* [ ] Failed test is visible.
* [ ] Last tested backup is protected from pruning.
* [ ] Recovery Exercise produces lessons and actions.

## Retention and Pruning

* [ ] Backup Retention Policy schema exists.
* [ ] Daily count is explicit.
* [ ] Weekly count is explicit.
* [ ] Monthly count is explicit.
* [ ] Pre-update count is explicit.
* [ ] Maximum age is explicit.
* [ ] Maximum storage is explicit.
* [ ] Minimum verified count is explicit.
* [ ] Minimum restore-tested count is explicit.
* [ ] Hold rules are explicit.
* [ ] Erasure rules are explicit.
* [ ] Initial recommendation is seven daily.
* [ ] Initial recommendation is four weekly.
* [ ] Initial recommendation is six monthly.
* [ ] Initial recommendation is two update or migration points.
* [ ] User can change within policy.
* [ ] Enterprise policy can constrain.
* [ ] Snapshot can satisfy several buckets.
* [ ] Last verified snapshot is protected.
* [ ] Last restore-tested snapshot is protected.
* [ ] Active rollback snapshot is protected.
* [ ] Preservation Hold is respected.
* [ ] Recovery incident dependency is respected.
* [ ] Vault Capsule has separate retention.
* [ ] Prune Plan previews manifests and objects.
* [ ] Reclaimed-byte estimate is shown.
* [ ] Reference changes commit before deletion.
* [ ] Unreferenced object deletion is idempotent.
* [ ] Crash during prune recovers.
* [ ] Reference index can rebuild.
* [ ] Snapshot deletion creates safe receipt.
* [ ] Retention cannot bypass Erasure Ledger.

## Repository Scrub and Repair

* [ ] Full repository scrub exists.
* [ ] Partial bounded scrub exists.
* [ ] Scrub can resume.
* [ ] Offline removable repository verifies when connected.
* [ ] Corrupt object identifies affected snapshots.
* [ ] Snapshot unusability is visible.
* [ ] Repair uses another verified copy or source only.
* [ ] No synthetic repair bytes are invented.
* [ ] Repository header corruption is handled.
* [ ] Key-slot corruption is handled.
* [ ] Manifest corruption is handled.
* [ ] Commit-marker corruption is handled.
* [ ] Object corruption is handled.
* [ ] Repository quarantine is available.
* [ ] Read-only examination is available.
* [ ] Scrub evidence is retained.

## Backup Health

* [ ] Healthy is implemented.
* [ ] Healthy with Warnings is implemented.
* [ ] Not Configured is implemented.
* [ ] Recovery Point Only is implemented.
* [ ] Same-Device Only is implemented.
* [ ] No Portable Key is implemented.
* [ ] Destination Offline is implemented.
* [ ] Destination Missing is implemented.
* [ ] Backup Overdue is implemented.
* [ ] Backup Failed is implemented.
* [ ] Verification Overdue is implemented.
* [ ] Restore Test Overdue is implemented.
* [ ] Retention Conflict is implemented.
* [ ] Key Recovery Risk is implemented.
* [ ] Vault Not Portable is implemented.
* [ ] Partial Backup is implemented.
* [ ] Corrupt Backup is implemented.
* [ ] Ransomware Exposure is implemented.
* [ ] Recovery Required is implemented.
* [ ] Health shows last complete backup.
* [ ] Health shows last off-device backup.
* [ ] Health shows last portable backup.
* [ ] Health shows last verification level.
* [ ] Health shows last restore test.
* [ ] Health shows current RPO estimate.
* [ ] Health shows destination class.
* [ ] Health shows portable key status.
* [ ] Health is visible in Trust Centre.

## Trust and User Experience

* [ ] Backup Overview exists.
* [ ] Destination view exists.
* [ ] Schedule view exists.
* [ ] Recovery Point view exists.
* [ ] Managed Snapshot view exists.
* [ ] Portable Archive view exists.
* [ ] Workspace Recovery view exists.
* [ ] Vault Recovery view exists.
* [ ] Verification view exists.
* [ ] Restore Test view exists.
* [ ] Exercise view exists.
* [ ] Restore History exists.
* [ ] Erasure view exists.
* [ ] Disaster Recovery view exists.
* [ ] Health view exists.
* [ ] Same-device warning is clear.
* [ ] DPAPI machine-binding warning is clear.
* [ ] Vault portability warning is clear.
* [ ] Vault Capsule warning is clear.
* [ ] Restore safety warning is clear.
* [ ] Ransomware warning is clear.
* [ ] Verification levels are understandable.
* [ ] Portability distinction is understandable.
* [ ] Progress is visible.
* [ ] Cancellation is available.
* [ ] Missing dependencies are actionable.
* [ ] Recovery Report is readable.
* [ ] UI is keyboard accessible.
* [ ] UI supports Narrator.
* [ ] UI supports high contrast.
* [ ] Status does not rely on colour alone.
* [ ] Security review is complete.
* [ ] Privacy review is complete.
* [ ] Recovery review is complete.
* [ ] Founder approval is recorded.

---

# 418. Evidence Required Before Acceptance

* [ ] Backup and Recovery Service contract.
* [ ] backup state-classification catalogue.
* [ ] Backup Product Definition schema.
* [ ] Backup Adapter schema.
* [ ] owner-state inventory.
* [ ] Backup Epoch schema.
* [ ] owner checkpoint schema.
* [ ] Destination schema.
* [ ] Repository format specification.
* [ ] object-chunk format specification.
* [ ] Encryption Profile schema.
* [ ] key-slot schemas.
* [ ] Backup Manifest schema.
* [ ] Commit Marker schema.
* [ ] Backup Schedule schema.
* [ ] Workspace Recovery Pack schema.
* [ ] Vault Recovery Capsule schema.
* [ ] Data Portability Bundle schema.
* [ ] Restore Plan schema.
* [ ] Recovery Report schema.
* [ ] Erasure Ledger schema.
* [ ] Disaster Recovery Plan schema.
* [ ] Recovery Exercise schema.
* [ ] initial adapter catalogue.
* [ ] consistency-barrier report.
* [ ] barrier latency report.
* [ ] barrier crash report.
* [ ] SQLite Online Backup report.
* [ ] SQLite WAL and journal adversarial report.
* [ ] SQLite power-loss report.
* [ ] `VACUUM INTO` report.
* [ ] VSS integration decision record.
* [ ] CAS pin and GC report.
* [ ] mutable-file snapshot report.
* [ ] rebuildable-exclusion report.
* [ ] destination identity report.
* [ ] same-device classification report.
* [ ] network-share interruption report.
* [ ] cloud-sync warning report.
* [ ] removable-disk interruption report.
* [ ] repository-lock report.
* [ ] repository-index rebuild report.
* [ ] chunking report.
* [ ] object-ID privacy report.
* [ ] compression report.
* [ ] Repository Master Key report.
* [ ] HKDF known-answer report.
* [ ] AES-GCM known-answer report.
* [ ] nonce-reuse prevention report.
* [ ] DPAPI key-slot report.
* [ ] DPAPI new-machine limitation report.
* [ ] PBKDF2 calibration report.
* [ ] .NET one-shot PBKDF2 API report.
* [ ] KDF downgrade report.
* [ ] random recovery-key report.
* [ ] recovery-key co-location report.
* [ ] key-slot lifecycle report.
* [ ] key-rotation report.
* [ ] manifest canonicalisation report.
* [ ] manifest-substitution report.
* [ ] commit-marker report.
* [ ] synthetic-full restore report.
* [ ] schedule and DST report.
* [ ] power and network admission report.
* [ ] backup cancellation report.
* [ ] backup resume report.
* [ ] pre-migration recovery-point report.
* [ ] pre-update recovery-point report.
* [ ] Workspace Recovery Pack report.
* [ ] Git conflict report.
* [ ] non-Git recovery report.
* [ ] Vault Capsule cryptographic report.
* [ ] Vault root-key boundary report.
* [ ] Vault Capsule new-machine restore report.
* [ ] Vault leakage report.
* [ ] portability-format documentation.
* [ ] portability export sample.
* [ ] portability import report.
* [ ] import authority-denial report.
* [ ] Recovery Host isolation report.
* [ ] Recovery Host no-network capture.
* [ ] malicious-backup parser report.
* [ ] backup version-compatibility report.
* [ ] staged-root restore report.
* [ ] owner database restore report.
* [ ] package quarantine report.
* [ ] model revalidation report.
* [ ] plugin restore report.
* [ ] MCP restore report.
* [ ] provider restore report.
* [ ] workflow Recovery Required report.
* [ ] no-external-effect replay report.
* [ ] project mapping report.
* [ ] new-machine restore report.
* [ ] Erasure Ledger report.
* [ ] deleted-data resurrection report.
* [ ] root activation crash report.
* [ ] root rollback report.
* [ ] Recovery Validation Mode report.
* [ ] clean-room restore report.
* [ ] ransomware-suspected report.
* [ ] signed-reinstall report.
* [ ] recovery-objective report.
* [ ] Business Impact Analysis.
* [ ] cryptographic-verification report.
* [ ] structural-verification report.
* [ ] semantic-verification report.
* [ ] selective restore-test report.
* [ ] full restore-test report.
* [ ] recovery-exercise report.
* [ ] retention selection report.
* [ ] prune crash report.
* [ ] repository scrub report.
* [ ] corrupt-object impact report.
* [ ] Backup Health UI evidence.
* [ ] Trust Centre backup views.
* [ ] diagnostics leakage report.
* [ ] raw-copy adversarial report.
* [ ] epoch and barrier adversarial report.
* [ ] repository cryptography adversarial report.
* [ ] backup-key adversarial report.
* [ ] Vault adversarial report.
* [ ] destination adversarial report.
* [ ] malicious-backup adversarial report.
* [ ] auto-execution adversarial report.
* [ ] erasure-resurrection adversarial report.
* [ ] ransomware adversarial report.
* [ ] root-switch adversarial report.
* [ ] cross-project adversarial report.
* [ ] portability adversarial report.
* [ ] performance report.
* [ ] scale report.
* [ ] 30-day endurance report.
* [ ] accessibility report.
* [ ] security review.
* [ ] privacy review.
* [ ] recovery review.
* [ ] founder approval.

---

# 419. Known Limitations

* No first-party cloud backup service exists.
* No automatic off-device destination exists.
* VSS writer integration is deferred.
* System-image backup remains external.
* Same-device Recovery Points do not protect against device loss.
* User-writable repositories are vulnerable to same-user malware.
* Offline and immutable status cannot always be detected.
* Cloud-sync completion cannot be guaranteed.
* Network-share integrity depends on the remote system.
* DPAPI CurrentUser slots are normally machine and user bound.
* A forgotten passphrase cannot be recovered.
* A lost random recovery key cannot be recreated.
* PBKDF2 is selected provisionally while NIST revision work continues.
* A future memory-hard KDF requires a format revision.
* Application-layer encrypted repository code increases implementation risk.
* AES-GCM nonce management must be exact.
* The Vault Recovery Capsule increases secret-backup risk.
* External tokens can expire or be revoked after backup.
* Some credentials cannot be restored and must be reissued.
* Source-controlled files may be unavailable if the repository is lost.
* Workspace Recovery Packs may conflict with changed repository history.
* Binary and ignored files require explicit selection.
* Full project snapshots can be large and sensitive.
* Package sources may disappear.
* Model files may be too large for routine backup.
* Model licences or terms may change.
* Restored plugins may no longer be trusted.
* Restored MCP server identities may change.
* Provider terms and regions may change.
* Running workflows never auto-resume.
* External effects may remain Outcome Unknown.
* Current policy can prevent restoration of previously valid settings.
* Restore migrations can fail.
* Old backups may require an intermediate signed product version.
* A backup can contain malicious user data even when cryptographically intact.
* Clean-room recovery cannot prove all content is benign.
* Ransomware may affect continuously mounted backups.
* Verification does not prove future dependency availability.
* Restore testing consumes disk, CPU and time.
* Full exercises require human effort.
* One local repository writer limits concurrency.
* Large repositories may require long scrub times.
* Portable archives can be very large.
* Data portability does not preserve all internal runtime state.
* Third-party import compatibility is not guaranteed.
* Erasure from immutable backups may occur only through expiry and beyond-use controls.
* Forensic erasure from SSDs, journals, provider versions and external copies is not guaranteed.
* Backup retention values are provisional.
* RPO and RTO are targets, not contractual guarantees.
* The initial performance targets require evidence.
* Cross-platform restore is deferred.
* Multi-user shared repositories are deferred.
* Enterprise key escrow is deferred.
* Hardware-backed recovery keys are deferred.

---

# 420. Open Questions

* Which exact SQLite release should every Backup Adapter pin?
* Should all services use one SQLite native library build?
* How is a service prevented from accidentally backing up through a different SQLite build?
* Does `Microsoft.Data.Sqlite` expose enough Online Backup control?
* Is a native interop wrapper required?
* How is `sqlite3_backup_init` snapshot establishment timed precisely?
* Does the snapshot become fixed at `backup_init` or first `backup_step` for every supported version?
* How are source writes handled if the snapshot is restarted internally?
* What page batch gives the best latency and throughput?
* Is 128 pages appropriate?
* Which busy timeout is appropriate?
* How many services may copy concurrently?
* Should database backups be parallel or dependency ordered?
* How does barrier coordination work if one service is in a long read transaction?
* Can a long read prevent Online Backup?
* How are WAL checkpoints handled before backup?
* Should owners run passive checkpoint first?
* Could that increase barrier latency?
* Which databases require `synchronous=FULL` before snapshot?
* How are database encryption changes handled if selected later?
* Should `VACUUM INTO` be used for all portable database copies?
* Does deleted-page purging justify its CPU and disk cost?
* How much staging disk is required for `VACUUM INTO`?
* Should privacy-sensitive databases use `VACUUM INTO` by default?
* Which state qualifies as Rebuildable?
* How is rebuild-source availability proven?
* Is a model available from its original source enough?
* What if a licence prohibits redownload?
* Should signed plugin packages be included by default?
* Should local model files be included in an Offline Recovery product?
* How are huge model files chunked and verified efficiently?
* Which model caches are safe to omit?
* Should embedding indexes be backed up by default?
* How expensive is re-embedding?
* Which Trust Evidence is required for unresolved workflows?
* How much historical Trust Evidence belongs in a core backup?
* Should operational logs be included?
* Should support-bundle staging ever be backed up?
* Likely not; how is this enforced?
* Which Configuration history is required?
* Which Effective Snapshots must be preserved?
* Should enterprise registry policy observations be exported?
* How are source-controlled project settings treated when the repository is unavailable?
* Which Workspace metadata are truly non-rebuildable?
* Should every active project receive an automatic Workspace Recovery Pack?
* Would that copy too much source?
* How are ignored build outputs distinguished from important ignored secrets?
* Can users define important ignored files?
* How are Git submodules handled?
* How are nested repositories handled?
* How are Git LFS files handled?
* Can a Workspace Pack include LFS pointers only?
* How are sparse checkouts handled?
* How are case-only renames represented on Windows?
* How are file mode changes represented?
* How are symlinks represented when Windows permissions differ?
* Should symlinks be prohibited in Workspace Packs?
* How are junctions handled?
* How are alternate data streams handled?
* How are very large untracked files handled?
* Which secret scanner runs before Workspace Pack inclusion?
* Should project source ever be included in scheduled backup automatically?
* How is explicit consent retained?
* What is the exact Backup Epoch participant timeout?
* Is five seconds enough for 100 services?
* Should services prepare in parallel?
* How is barrier fairness implemented?
* Which commands are considered mutating?
* Can user edits continue through Workspace while barrier is active?
* How are writes queued?
* What happens when the queue is full?
* How are external provider calls represented at checkpoint?
* How are local model streams represented?
* Do active model responses need backup state?
* Should streaming responses be discarded and retried after restore?
* How are partial patches handled?
* How are open file handles handled?
* How are pending staged writes captured?
* Can one owner be Complete while another is Partial?
* What selective restores remain safe?
* How are cross-service dependencies declared?
* Should Backup Adapters produce a dependency graph?
* How is a cyclic dependency handled?
* Which outbox and inbox positions are sufficient?
* How are messages in external systems handled?
* How are external provider request IDs retained?
* Which pending network operations require Trust Evidence?
* How are consistency-state warnings shown?
* Which backup classes allow Complete with Rebuildable Exclusions?
* Is a missing Project Knowledge index always acceptable?
* Is missing Trust Evidence acceptable?
* Is missing workflow history ever acceptable?
* How is backup destination physical-device identity determined on Windows?
* Which volume and disk identifiers are stable?
* How are Storage Spaces handled?
* How are RAID arrays handled?
* How are virtual disks handled?
* How are mounted VHD files handled?
* How are network-mounted drives classified?
* How is a cloud-synchronised folder detected?
* Can OneDrive Files On-Demand break repository reads?
* Should cloud folders be denied for managed repositories?
* Or allowed only for portable archives?
* How are partial sync conflicts detected?
* How are provider version histories handled?
* Should network repositories use object-store-like immutable writes?
* How are SMB durable handles used?
* How are disconnected writes reconciled?
* Can two machines access one repository?
* Version 1 says one writer; how is another machine rejected?
* Should repository ID be registered to one writer identity?
* How is a writer identity transferred after device loss?
* Can a stale lock prevent recovery?
* How are removable disks safely ejected?
* Which flush APIs are used?
* Can the product verify that USB hardware honoured flush?
* How are fake-capacity USB devices detected?
* Should backup creation test destination capacity before trust?
* How is corruption from unreliable flash handled?
* Which repository chunk size is optimal?
* Is 4 MiB suitable for database and model data?
* Should rolling-content-defined chunking be considered?
* Would it improve deduplication after database page changes?
* Would it create complexity and memory cost?
* Should SQLite database snapshots be page-aware chunked?
* Would 4 KiB or page-aligned chunks improve deduplication?
* Would metadata overhead become excessive?
* Should database backup objects be compressed as one stream then chunked?
* How does that affect deduplication?
* Should each SQLite page be a content object?
* How are privacy and equality leaks handled?
* Which compression algorithm is selected?
* Is Deflate sufficient?
* Should Zstandard be added as a dependency?
* How are dictionary versions handled?
* Should compression be disabled for Vault Capsules?
* Could compression expose secret length?
* What threat model applies to offline encrypted backups?
* How are plaintext chunk hashes protected?
* Is HMAC over SHA-256 sufficient for object identity?
* Should object IDs include object type and compression?
* How are object-ID key rotations handled?
* Can the same plaintext be deduplicated across key versions?
* Should it be?
* How are object-ID collisions detected after decryption?
* Should manifests include Merkle roots?
* Would a Merkle tree improve large verification?
* How are Merkle proofs useful locally?
* Should repository history have a hash chain?
* How is repository rollback detected on removable media?
* Would a local trusted last-seen head help?
* What happens on new-machine restore with no last-seen head?
* Should optional signed snapshot manifests be supported?
* Which signing key would be used?
* Would user-generated signing add complexity?
* Should enterprise signing be deferred?
* How is repository encryption reviewed formally?
* Should backup cryptography receive a separate ADR?
* Is AES-GCM per object sufficient?
* How are random nonces generated and tracked?
* Is a random 96-bit nonce safe at the expected object count?
* Should object keys be unique per object via HKDF to reduce nonce-collision impact?
* Should nonce be derived from object identity under a unique object key?
* Which design is simplest to audit?
* How are AEAD associated-data fields canonicalised?
* How are format upgrades handled?
* Can new software read old encryption profiles?
* How long must old KDFs remain supported?
* When should old KDF slots be upgraded?
* Should passphrase slots be rewrapped automatically after stronger policy?
* How is KDF calibration persisted?
* Is 600,000 iterations appropriate in July 2026?
* How does reference hardware compare with low-power laptops?
* Should target time be the primary policy and iteration floor secondary?
* How is denial of service prevented from malicious iteration counts?
* What maximum iterations are acceptable?
* How is passphrase Unicode normalised?
* Should no normalisation be used to preserve exact input?
* How are leading and trailing spaces handled?
* Should passphrase entry show a fingerprint of derived key?
* How is recovery-key possession confirmed?
* Should the key file be printable?
* Should a mnemonic representation exist?
* Would that be custom and error prone?
* Should QR export exist?
* Could it leak through screenshots?
* How are recovery keys backed up securely?
* Should enterprise policy require two independent recovery slots?
* Should a portable repository require both passphrase and recovery key?
* Or either?
* Should multi-factor key slots be supported?
* How are hardware tokens integrated later?
* How are enterprise public-key recipients integrated?
* Which standard envelope would be appropriate?
* How is key-slot removal audited?
* Can a compromised current machine add an attacker's slot?
* How is slot addition approved?
* Should slot changes require current portable-key proof?
* How is Repository Master Key rotation scheduled?
* Should every backup use a new master key?
* How is old-key retirement handled?
* How is background re-encryption bounded?
* What happens when re-encryption is interrupted?
* How are old key slots backed up?
* Should one repository use separate keys per backup?
* How does that affect deduplication?
* Is one Repository Master Key too broad?
* Should per-snapshot keys be wrapped by repository key?
* How is compromise containment balanced with deduplication?
* How are key materials held in memory?
* Which buffers can be zeroed in .NET?
* Should cryptographic operations use native protected memory?
* Is Windows CNG preferable for some operations?
* How are cryptographic failures tested across power loss?
* Should FIPS-mode compatibility be required?
* How does enterprise FIPS policy affect AES-GCM, HKDF and PBKDF2?
* Which KDF hash is approved under such policy?
* How is the Vault Recovery Capsule related to ADR-0007?
* Does capsule creation require unwrapping the Vault root key?
* Can secret records instead be individually re-encrypted without exposing root key?
* Which approach is easier to audit?
* Should a capsule include all secret versions?
* Should old rotated secrets be excluded?
* How are tombstoned secrets handled?
* How are secret aliases masked in preview?
* How does a user select secret categories safely?
* How are provider refresh tokens handled?
* How are private SSH keys handled?
* How are certificate private keys handled?
* Can non-exportable keys be backed up?
* If not, how are they represented?
* How are Windows certificate-store references handled?
* How is a capsule restore tested without contacting providers?
* How are restored credentials marked Unverified?
* When may they be used?
* Does first use require approval?
* Should capsule creation require a Preservation Hold?
* How are capsules pruned?
* How are capsule recovery keys kept separate?
* Should a capsule be embedded in a portable backup or always separate?
* What is the safest default?
* How are portability schemas documented publicly?
* Which data are user-provided versus inferred?
* Which inferred data should still be exportable for product value?
* How are third-party rights protected?
* How are other people's names in project memory handled?
* Which data categories require redaction?
* Should portability exports be encrypted?
* The user-readable format is useful; how is secure transport handled?
* Should an optional encrypted wrapper reuse backup encryption?
* Would that confuse backup and portability?
* How are large exports streamed?
* How are CSV escaping rules defined?
* How are JSON schema versions published?
* How are Markdown attachments referenced?
* How are binary files represented?
* How are Portability Bundle imports quarantined?
* How are future extensions preserved?
* How are unknown plugin namespaces handled?
* How are imported memory records reviewed?
* How are imported workflows shown as disabled?
* How are project identity collisions resolved?
* Can a portability import merge history?
* How is provenance retained?
* Which restore modes belong in Version 1?
* Is Full In-Place Replacement necessary or should all restores be side by side?
* How is the current-root pointer stored safely?
* Is JSON sufficient?
* Should it be a small SQLite bootstrap database?
* How is root-switch atomicity guaranteed on Windows?
* Can directory rename be used reliably?
* How are antivirus and indexer locks handled?
* How are MSIX process shutdown and data paths handled?
* How does update recovery interact with root switching?
* Which process owns activation?
* What happens if Desktop remains open?
* How are background worker processes stopped?
* How are old roots retained?
* How much disk is reserved for rollback?
* How long is rollback root retained?
* Can the user pin it?
* How is sensitive old-root deletion handled?
* How are backups taken during Recovery Validation Mode?
* Should they be denied?
* How are new local edits captured while validation is pending?
* Should the UI remain read only?
* How are project mappings represented without raw path leakage?
* Can one backup restore to multiple project roots?
* How are moved repositories identified?
* Is Git remote plus commit sufficient?
* What about local repositories without remotes?
* Should Opure store a repository identity file?
* How is clone integrity verified?
* How are non-Git projects mapped?
* How are case-sensitive directories handled?
* How are Windows drive-letter changes handled?
* How are junctioned project roots handled?
* How are unavailable removable project drives handled?
* How are restored workflows mapped to missing projects?
* How long may they remain Recovery Required?
* Can an active workflow be selectively omitted?
* How is its Trust Evidence retained?
* How are side effects reconciled without network?
* Some external reconciliation requires network; when is that enabled?
* Which user approval is required?
* How are provider credentials reacquired before reconciliation?
* Can an external effect remain unresolved indefinitely?
* How is that shown after restore?
* How do current policy changes affect old workflow plans?
* Should some workflows be abandoned automatically?
* Likely not; how is the action presented?
* How are old plugin versions retained for workflow history?
* Can a restored workflow migrate before resume?
* How is migration approved?
* How are timers recalculated after years of downtime?
* Should overdue timers fire immediately?
* No; what review is required?
* How are external signals received while Opure is offline?
* How are stale approvals handled?
* How are current Erasure Ledgers obtained on a new machine?
* If the only current ledger was on the lost device, does the backup contain its latest state?
* How are erasure records protected from deletion?
* Should erasure records have longer retention than deleted data?
* How are safe fingerprints constructed?
* Could fingerprints leak low-entropy personal data?
* Should fingerprints use an owner-held HMAC key?
* How is that key restored?
* How are backup-side erasure records applied to older backups?
* Can an immutable repository be updated with a global erasure overlay?
* Should every restore load the latest repository-level erasure overlay?
* How is overlay rollback detected?
* How are privacy requests handled across several backup repositories?
* Can Opure enumerate disconnected removable repositories?
* How are external copies tracked?
* How are users told to delete old exports?
* What does beyond use mean in the product?
* How are backups prevented from routine restore after erasure?
* Which owner purge adapters are required?
* How are purge failures handled?
* How are CAS chunks containing mixed records handled?
* Does chunk-level deduplication complicate selective erasure?
* Because objects are encrypted and immutable, should record-level data be packed less aggressively?
* How are database backups purged without reconstructing them?
* Restore-time erasure may be the only path; is retention adequate?
* Should privacy-sensitive database backups use shorter retention?
* Should `VACUUM INTO` be used after erasure?
* How are Preservation Holds combined with erasure requests?
* What legal and privacy advice is needed?
* Which RPO and RTO targets are realistic?
* Is two minutes for service crash reasonable?
* Is 15 minutes for update rollback reasonable?
* Is two hours for core full restore reasonable?
* How is RTO measured on slower hardware?
* Which core state must restore first?
* Can Project Knowledge rebuild after the user starts work?
* Can model files download after core recovery?
* How is offline recovery handled without package sources?
* Should an Offline Recovery Backup include signed installers and package bytes?
* How are installer signatures revalidated without network revocation?
* What offline trust evidence is sufficient?
* How are certificate expiry and timestamp signatures handled?
* How are revoked packages handled offline?
* How is a known-clean backup selected in ransomware recovery?
* Should backups have incident-time labels?
* How are anomalous changes detected?
* Should malware scans run on restored source?
* Which antimalware interface is used?
* Does scanning create privacy or provider dependencies?
* How are scanner results treated?
* Can a backup be cryptographically valid but quarantined by malware scan?
* How are false positives handled?
* Which recovery environment requirements are mandatory?
* Should clean-room restore be supported in a Windows Sandbox or VM?
* How are GPU and model tests performed in a VM?
* Should no-network mode use firewall, AppContainer or both?
* How is network denial proven?
* How is a recovery key entered securely in clean-room mode?
* How are recovery operations logged without exposing secrets?
* How are disaster communications handled for an individual user?
* What enterprise communication fields are needed?
* Which exercise types ship in Version 1?
* How much of a full recovery exercise can be automated?
* How are exercise test datasets generated?
* How are external effects guaranteed absent?
* Should an enterprise exercise use a dedicated test tenant?
* How are lessons turned into tasks?
* How are overdue exercises shown?
* Which verification schedule is appropriate for laptops?
* Is weekly selective restore too expensive?
* Should verification run only when destination is connected?
* How are removable repositories verified without keeping them mounted?
* How is a full restore test performed without doubling storage?
* Can sparse or copy-on-write volumes help?
* Should Windows block cloning or VHD be used?
* How is temporary test data deleted?
* How are restore-test results trusted?
* Which owner semantic validators are mandatory?
* Can a service claim semantic validity too easily?
* Should there be cross-service validation?
* How are external references tested without network?
* Which missing dependencies are acceptable?
* How are restore tests scheduled around active work?
* Which resource mode applies?
* How is verification throttled?
* How are backup priorities integrated with ADR-0020?
* Which retention policy is suitable for individual users?
* Are six monthly snapshots enough?
* Should annual snapshots exist?
* How are release evidence and long-lived projects handled?
* How does a repository quota interact with retention buckets?
* Which snapshots prune first?
* How are snapshots with unique large model files treated?
* Can optional package bytes be pruned independently of core snapshot?
* Would that break synthetic-full semantics?
* Should manifests support optional tiers?
* How are Vault Capsules retained separately?
* Should the last Vault Capsule be protected?
* How is key-slot health related to pruning?
* Should no snapshot be pruned until another portable-key backup exists?
* How are object reference counts stored and rebuilt at 10 million objects?
* Is SQLite suitable for local repository index?
* For network repositories, should index remain local and rebuildable?
* How are repository scrubs parallelised?
* How are corrupted chunks repaired?
* Should repositories support parity or erasure coding?
* Deferred; what evidence would justify it?
* Should multiple repository copies be compared?
* How are backup replications tracked?
* How is an external filesystem copy verified?
* Should a portable archive be one file or a directory?
* Which container supports streaming files larger than 4 GiB?
* Should ZIP64 be used?
* How are archive path rules enforced?
* How are encrypted object files named?
* How is random access to large archives handled?
* How does restore avoid extracting the entire archive first?
* Can it stream objects into staged root?
* How are archive truncation and central-directory corruption detected?
* Should the outer archive have a fixed header before ZIP?
* How is format detection safe?
* Should `.opure-backup` be a directory for very large sets?
* How is removable media spanning handled?
* Is multi-volume backup deferred?
* How are very large enterprise repositories supported?
* When would object storage APIs be required?
* Which Trust Evidence from backup operations is retained?
* Can backup evidence reveal destination or key fingerprints?
* How are destination paths redacted?
* How are recovery-key fingerprints displayed safely?
* Should fingerprints be included in Recovery Report?
* How are backup failures surfaced without notification fatigue?
* Should a missed backup create a desktop notification?
* Which health state is critical?
* How is Backup Health integrated into onboarding?
* Should the user be required to acknowledge No Portable Key?
* How often is the warning repeated?
* How are enterprise policies enforced without making the product unusable?
* Can enterprise policy require an offline backup while the drive is absent?
* How is compliance measured?
* How are backup destinations provisioned by enterprise tools?
* How are credentials delivered securely?
* Should network backup credentials be Vault entries?
* How are they restored after device loss?
* Does that create a bootstrap cycle?
* Should network recovery use interactive credentials?
* How are enterprise public-key recovery slots added later?
* What permanent evidence is required before VSS writer integration?
* What permanent evidence is required before first-party cloud backup?
* What permanent evidence is required before cross-device synchronisation?
* What permanent evidence is required before multi-user repositories?
* What permanent evidence is required before hardware-token recovery?
* What permanent evidence is required before memory-hard KDF migration?
* What permanent evidence is required before parity or erasure coding?
* What permanent evidence is required before automatic malware scanning?
* What permanent evidence is required before claiming any ransomware immutability property?

---

# 421. Deferred Decisions

This ADR intentionally defers:

* first-party cloud backup;
* automatic cloud destination selection;
* cross-device live synchronisation;
* shared multi-user repositories;
* multi-writer backup repositories;
* cloud object-lock integration;
* enterprise tape integration;
* VSS writer;
* VSS requester;
* parity and erasure coding;
* content-defined chunking;
* Merkle-tree repository proofs;
* external snapshot signing;
* external timestamping;
* enterprise public-key recovery slots;
* hardware-token recovery;
* key escrow;
* multi-factor key slots;
* memory-hard KDF selection;
* encrypted portability wrapper;
* cross-platform restore;
* multi-volume portable archives;
* automatic malware scanning;
* legal records-management claims;
* and guaranteed forensic erasure.

---

# 422. Alternatives Rejected

Raw copying of the live Opure data directory is rejected because SQLite databases, WAL files, mutable service state and CAS references may not be consistent.

Copying only SQLite main database files is rejected because active WAL or hot-journal state may be required for recovery.

VSS alone is rejected as the primary Opure backup because a volume snapshot does not replace application-aware owner checkpoints, secret policy, cross-service manifests and restore validation.

Uncoordinated per-service exports are rejected for complete disaster recovery because the relationship between outboxes, workflows, CAS roots and dependent state would be unclear.

One monolithic backup database is rejected because Opure's services own separate persistence boundaries and large artefacts do not belong in one SQLite file.

Cloud-first backup is rejected because local-first operation and explicit developer control are product requirements.

Git-only recovery is rejected because Opure holds non-repository state and uncommitted ignored data may still matter.

`VACUUM INTO` for every database is rejected as the default because the Online Backup API can copy incrementally and often consumes fewer CPU cycles.

DPAPI-only repository protection is rejected for device-loss recovery because DPAPI usually binds decryption to the same user and computer.

Unencrypted portable backups are rejected as the normal recovery format because project, memory, workflow and Trust state can be sensitive.

Legacy ZIP encryption is rejected as a misleading security mechanism.

Automatic Vault inclusion is rejected because it would silently make credentials portable and increase backup risk.

Restoring in place is rejected because a failed validation or migration could destroy the current recovery option.

Automatic plugin, MCP or workflow activation is rejected because backup authenticity does not prove present-day trust or side-effect safety.

Data Portability Bundles are rejected as the sole disaster-recovery format because stable exports intentionally omit active internal state.

Full disk images are not selected as the product format because they are broad, platform dependent and privacy intensive, though they remain useful defence in depth.

Event-log replay is rejected because not every owner is event sourced and packages, databases, large files and secrets require other mechanisms.

---

# 423. Official and Primary Evidence References

## SQLite Backup and Integrity

* [SQLite Online Backup API](https://www.sqlite.org/backup.html)
* [SQLite VACUUM and VACUUM INTO](https://sqlite.org/lang_vacuum.html)
* [How To Corrupt An SQLite Database File](https://www.sqlite.org/howtocorrupt.html)
* [SQLite Write-Ahead Logging](https://www.sqlite.org/wal.html)
* [SQLite Atomic Commit](https://www.sqlite.org/atomiccommit.html)
* [SQLite PRAGMA Documentation](https://www.sqlite.org/pragma.html)

## Windows Backup and Data Protection

* [Volume Shadow Copy Service](https://learn.microsoft.com/en-us/windows-server/storage/file-server/volume-shadow-copy-service)
* [Volume Shadow Copy Service for Win32 Applications](https://learn.microsoft.com/en-us/windows/win32/vss/volume-shadow-copy-service-portal)
* [VSS Requesters](https://learn.microsoft.com/en-us/windows/win32/vss/requestors)
* [CryptProtectData](https://learn.microsoft.com/en-us/windows/win32/api/dpapi/nf-dpapi-cryptprotectdata)
* [CryptUnprotectData](https://learn.microsoft.com/en-us/windows/win32/api/dpapi/nf-dpapi-cryptunprotectdata)

## .NET Cryptography

* [Rfc2898DeriveBytes.Pbkdf2](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.rfc2898derivebytes.pbkdf2)
* [SYSLIB0060 — Rfc2898DeriveBytes constructors are obsolete](https://learn.microsoft.com/en-us/dotnet/fundamentals/syslib-diagnostics/syslib0060)
* [AesGcm](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesgcm)

## NIST Recovery and Key Derivation

* [NIST SP 800-34 Revision 1 — Contingency Planning Guide for Federal Information Systems](https://csrc.nist.gov/pubs/sp/800/34/r1/upd1/final)
* [NIST SP 800-184 — Guide for Cybersecurity Event Recovery](https://csrc.nist.gov/pubs/sp/800/184/final)
* [NIST SP 800-84 — Guide to Test, Training, and Exercise Programs for IT Plans and Capabilities](https://csrc.nist.gov/pubs/sp/800/84/final)
* [NIST SP 800-132 — Recommendation for Password-Based Key Derivation](https://csrc.nist.gov/pubs/sp/800/132/final)
* [NIST Decision to Revise SP 800-132](https://csrc.nist.gov/news/2023/decision-to-revise-sp-800-132)
* [NIST IR 8374 Revision 1 — Ransomware Risk Management: CSF 2.0 Community Profile](https://csrc.nist.gov/pubs/ir/8374/r1/final)
* [NIST SP 1339 — OT Backup Quick Start Guide](https://csrc.nist.gov/pubs/sp/1339/final)

## CISA Ransomware Guidance

* [CISA StopRansomware Guide](https://www.cisa.gov/stopransomware/ransomware-guide)

## UK Privacy, Portability and Erasure

* [ICO Right to Data Portability](https://ico.org.uk/for-organisations/uk-gdpr-guidance-and-resources/individual-rights/individual-rights/right-to-data-portability/)
* [ICO Storage Limitation](https://ico.org.uk/for-organisations/uk-gdpr-guidance-and-resources/data-protection-principles/a-guide-to-the-data-protection-principles/storage-limitation/)
* [ICO Right to Erasure](https://ico.org.uk/for-organisations/uk-gdpr-guidance-and-resources/individual-rights/individual-rights/right-to-erasure/)

NIST publications are guidance, not product certification or legal mandates.

The ICO guidance and UK law can change and require current legal and privacy review.

NIST has announced that SP 800-132 will be revised.

Therefore, the selected PBKDF2 profile is provisional and the backup format must support KDF evolution.

The SQLite, Windows, .NET, cryptographic, storage and privacy assumptions can change.

The implementation must revalidate every selected library, format, key parameter, retention policy and recovery control before acceptance.

---

# 424. Review Record

| Date         | Reviewer           | Decision | Notes                                                                                                                |
| ------------ | ------------------ | -------- | -------------------------------------------------------------------------------------------------------------------- |
| 18 July 2026 | Architecture draft | Proposed | Service-aware encrypted backups, staged restores, separate portability and exercised clean-room recovery recommended |

---

# 425. Approval

## Founder or Product Approval

* **Name:** Christopher Dyer
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Backup products, defaults, recovery keys, Vault Capsule, retention, restore safety and recovery objectives require approval

## Backup and Recovery Architecture Approval

* **Name or role:** Backup and Recovery Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Epoch, repository, manifests, verification, restore and exercises require evidence

## Persistence Approval

* **Name or role:** Persistence Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** SQLite Online Backup, checkpoints, CAS pins, validation and corruption recovery required

## Security and Cryptography Approval

* **Name or role:** Security and Secrets Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** AES-GCM, HKDF, PBKDF2, key slots, nonce handling and Vault Capsules require formal review

## Privacy and Portability Approval

* **Name or role:** Privacy and Data Portability Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** export categories, erasure, retention, personal data and backup beyond-use controls required

## Runtime and Filesystem Approval

* **Name or role:** Runtime Architecture and Filesystem Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** consistency barriers, Recovery Host, staged roots, root switching and rollback required

## Workflow and Integration Approval

* **Name or role:** Workflow, Provider, Plugin, MCP, Tool and Project Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** paused restore, revalidation, mapping and no external-effect replay required

## Enterprise and Disaster Recovery Approval

* **Name or role:** Enterprise Management and Recovery Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** destinations, RPO, RTO, offline copies, ransomware mode and exercises required

## Test Approval

* **Name or role:** Test Architecture Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** power loss, cryptography, malicious backup, erasure, root-switch and clean-room suites required

---

# 426. Supersession

This ADR is superseded only when a later ADR:

* names ADR-0028 explicitly;
* explains why backup products, owner checkpoints, repository format, cryptography, portability, restore activation, erasure or disaster recovery changed;
* identifies Backup Adapter, repository, key slot, manifest, Portability Bundle, Restore Plan, Vault Capsule and Erasure Ledger migration;
* describes secret, project, workflow, plugin, MCP, provider, retention, deletion and ransomware impact;
* provides restore-test and recovery-exercise comparison evidence;
* and updates the `Superseded by` field.

Historical backup manifests, verification results, restore plans, Recovery Reports, erasure records, prune receipts and exercise results remain available according to retention policy unless explicitly purged.

---

# 427. Change History

| Version | Date         | Author        | Summary                                                                                                                |
| ------- | ------------ | ------------- | ---------------------------------------------------------------------------------------------------------------------- |
| 0.1     | 18 July 2026 | Founder Draft | Initial service-aware encrypted backup, staged restore, data portability, erasure and disaster-recovery recommendation |

---

# 428. Final Decision Statement

> **Opure will provisionally protect its local state through one trusted Backup and Recovery Service that coordinates versioned owner-service Backup Adapters using a Backup Epoch, bounded mutating-command barrier, durable owner checkpoints, SQLite Online Backup snapshots, transactional inbox and outbox positions and pinned immutable artefact roots; stores complete recovery sets as encrypted content-addressed synthetic-full manifests with authenticated chunks, explicit DPAPI convenience, passphrase and separately retained random recovery-key slots; distinguishes short-lived same-device Recovery Points, user-configured managed repositories, self-contained portable backups, selected Workspace Recovery Packs, structured non-authoritative Data Portability Bundles and separately approved Secrets-Service Vault Recovery Capsules; verifies every backup at named cryptographic, structural, semantic and restore-tested levels; and restores only through a network-disabled Recovery Host into a new staged data root where current schemas, Product and Enterprise Policy, packages, providers, models, plugins, MCP servers, project identities, credentials, workflow side effects and the live Erasure Ledger are revalidated before an atomic activation with the previous root retained for rollback, while no cloud destination is selected automatically, no raw live database or lone WAL-sensitive file is copied, no DPAPI-only repository is described as device-loss recovery, no restored code or integration executes during validation, all non-terminal workflows remain Recovery Required, deleted data is prevented from routine resurrection, and clean signed reinstall plus a known-clean offline or isolated backup and exercised recovery plan remain the required posture for destructive-malware scenarios, because genuine resilience depends not on possessing copied bytes but on proving consistent ownership, recoverable keys, complete manifests, safe restoration and a rehearsed path back to controlled operation.**

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**