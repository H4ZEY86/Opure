# SPEC-009 — Workspace and File Patch Engine

## Opure Platform Safe Workspace Access, Reviewable Change and Transactional File Operations

**Document:** SPEC-009  
**Status:** Founder Draft  
**Version:** 0.1  
**Language:** British English  
**Last updated:** 18 July 2026  
**Depends on:** CHARTER-001, SPEC-001, SPEC-002, SPEC-003, SPEC-004, SPEC-005, SPEC-006, SPEC-007, SPEC-008  

---

## 1. Purpose

This specification defines the Workspace and File Patch Engine of the Opure Platform.

It establishes how Opure:

- identifies and opens project workspaces;
- defines safe workspace boundaries;
- reads and observes files;
- normalises paths;
- detects symbolic links, junctions and traversal;
- classifies files;
- creates workspace snapshots;
- constructs reviewable patches;
- previews and validates changes;
- detects conflicts;
- scans generated changes for secrets;
- applies file changes transactionally;
- records exact outcomes;
- reverses or compensates changes;
- recovers after interruption;
- and keeps developer-owned files authoritative.

The Workspace and File Patch Engine must allow Opure to propose and apply useful engineering changes without taking hidden ownership of the developer's project.

---

## 2. Founding Rule

> **Opure proposes changes through reviewable patches; the developer's workspace remains authoritative.**

No AI model, workflow, plugin, MCP server, provider adapter or desktop view may write project files directly.

All protected project-file changes must pass through the Workspace Service and Patch Service contracts defined by this specification.

---

## 3. Relationship to Other Services

A simplified logical view is:

```text
Developer / Desktop
        │
        ▼
Workflow Engine
Plugin Host
MCP Gateway
Build Manager
Repository Service
        │
        ▼
Patch Service
        │
        ├── Patch Builder
        ├── Patch Parser
        ├── Diff Engine
        ├── Validation Engine
        ├── Conflict Detector
        ├── Secret Scan Coordinator
        ├── Approval Binder
        ├── Transaction Coordinator
        ├── Snapshot Manager
        ├── Apply Engine
        ├── Reverse Engine
        └── Recovery Journal
                │
                ▼
Workspace Service
        │
        ├── Workspace Registry
        ├── Path Canonicaliser
        ├── File Identity Manager
        ├── Read Service
        ├── File Watcher
        ├── Exclusion Manager
        ├── Classification Manager
        ├── Encoding Detector
        ├── Line Ending Manager
        └── Filesystem Adapter
```

The Workspace Service owns safe access to project files.

The Patch Service owns patch state, validation and application.

The Repository Service owns version-control operations.

The Build Manager owns build and test execution.

The Knowledge Engine owns derived project knowledge.

---

## 4. Design Goals

The Workspace and File Patch Engine must be:

- safe by default;
- reviewable;
- transactional where technically practical;
- conflict-aware;
- reversible where technically practical;
- path-safe;
- encoding-aware;
- line-ending-aware;
- observable;
- cancellable;
- recoverable;
- efficient;
- repository-friendly;
- and independent from AI providers.

It should preserve the developer's existing project conventions rather than normalising files unnecessarily.

---

## 5. Non-Goals

This specification does not make the Patch Service responsible for:

- generating code;
- deciding engineering intent;
- selecting AI providers;
- running builds or tests;
- creating Git commits;
- granting permission;
- storing secrets;
- interpreting every file format semantically;
- or guaranteeing that a valid patch is functionally correct.

Patch validity and engineering correctness are different concerns.

A patch may be structurally valid and still be wrong.

---

## 6. Normative Language

The terms **MUST**, **MUST NOT**, **SHOULD**, **SHOULD NOT**, **MAY** and **DEFERRED** have the meanings defined in SPEC-001.

Any intentional violation of a **SHOULD** requirement must be documented in an Architecture Decision Record.

---

# 7. Core Concepts

## 7.1 Workspace

A Workspace is an approved filesystem boundary associated with an Opure project.

A workspace may contain one or more roots where explicitly supported.

---

## 7.2 Workspace Root

A Workspace Root is a canonical filesystem location within which approved project access may occur.

---

## 7.3 File Identity

A File Identity is a stable Opure representation of a file or directory.

It must not rely only on a display path.

---

## 7.4 File Revision

A File Revision identifies a specific observed state of a file.

It should include:

- file identity;
- content hash;
- size;
- modification metadata;
- encoding;
- line-ending style;
- and observation time.

---

## 7.5 Snapshot

A Snapshot is a recorded set of file revisions used for:

- comparison;
- patch validation;
- conflict detection;
- rollback;
- and recovery.

---

## 7.6 Patch

A Patch is a versioned, reviewable proposal to change one or more workspace resources.

---

## 7.7 Patch Set

A Patch Set groups related patches under one developer intent or workflow.

---

## 7.8 Patch Operation

A Patch Operation is one bounded change such as:

- create file;
- modify file;
- delete file;
- rename file;
- move file;
- create directory;
- delete empty directory;
- or change file metadata where supported.

---

## 7.9 Patch Base

The Patch Base is the exact file revision or snapshot against which a patch was produced.

---

## 7.10 Patch Conflict

A Patch Conflict occurs when current workspace state no longer matches the assumptions required to apply the patch safely.

---

## 7.11 Transaction Journal

A Transaction Journal records the state of an apply, reverse or recovery operation.

---

# 8. Service Responsibilities

## 8.1 Workspace Service

The Workspace Service owns:

- workspace registration;
- workspace roots;
- path canonicalisation;
- file identity;
- file reads;
- directory listing;
- exclusions;
- file metadata;
- file observation;
- file watching;
- snapshot creation;
- and workspace health.

It does not own:

- patch lifecycle;
- patch approval;
- Git state;
- build results;
- or semantic project knowledge.

---

## 8.2 Patch Service

The Patch Service owns:

- patch identifiers;
- patch format;
- patch parsing;
- patch lifecycle;
- diff generation;
- preview;
- conflict detection;
- validation;
- secret-scan coordination;
- application;
- reversal;
- transaction journalling;
- and recovery metadata.

It does not own:

- direct AI generation;
- build execution;
- repository commit;
- or project permissions.

---

# 9. Workspace Registration

## 9.1 Project Association

Every workspace must be associated with a stable Opure project identifier.

---

## 9.2 Root Validation

Before registration, Opure must validate:

- path existence;
- canonical path;
- access permissions;
- filesystem type where relevant;
- symlink or junction behaviour;
- duplicate registration;
- nesting with other workspaces;
- and case-sensitivity behaviour.

---

## 9.3 Multiple Roots

A project may support multiple workspace roots only when each root is explicit and independently policy-scoped.

---

## 9.4 Nested Workspaces

Nested workspaces should be avoided.

Where allowed, ownership and policy precedence must be explicit.

---

## 9.5 Moved Workspace

A project should be able to update its workspace location without losing its internal project identity.

---

# 10. Workspace Boundary

## 10.1 Canonical Boundary

All access decisions must use canonical paths.

---

## 10.2 Boundary Enforcement

A requested path must remain inside an approved root after:

- normalisation;
- symlink resolution;
- junction resolution;
- relative-segment elimination;
- case handling;
- and filesystem canonicalisation.

---

## 10.3 Outside Access

Access outside the workspace requires a separate explicit permission and must not be treated as normal project access.

---

## 10.4 Path Traversal

Traversal attempts must be rejected and recorded as security events where appropriate.

---

## 10.5 Drive and Share Boundaries

Windows drive letters, UNC shares and mounted locations must be handled explicitly.

A path must not silently cross into another drive or network share.

---

# 11. Windows Path Requirements

The initial implementation must correctly handle:

- drive-letter paths;
- UNC paths where supported;
- long paths;
- reserved device names;
- alternate data streams;
- junctions;
- symbolic links;
- hard links;
- case-insensitive comparison;
- trailing spaces and dots;
- invalid characters;
- and path-length behaviour.

---

## 11.1 Alternate Data Streams

Alternate data streams should be blocked by default unless a specific supported use exists.

---

## 11.2 Reserved Names

Reserved Windows names must not be created accidentally.

---

## 11.3 Case Preservation

Display paths should preserve actual case.

Security comparison must use filesystem-appropriate semantics.

---

# 12. File Identity

## 12.1 Identity Requirements

A file identity should include:

- project identifier;
- workspace-root identifier;
- canonical relative path;
- filesystem identity where available;
- resource type;
- and identity version.

---

## 12.2 Rename Detection

Rename detection may use:

- filesystem identity;
- repository information;
- content hash;
- file-watch events;
- and heuristics.

Heuristic rename detection must be labelled as inferred.

---

## 12.3 Hard Links

Hard-linked files require careful handling because multiple paths may reference one underlying file.

The system must not assume one path always equals one independent resource.

---

## 12.4 Deleted Identity

Deleted file identities may be retained as tombstones for patch, recovery and history references.

---

# 13. File Classification

Files should be classified by:

- source;
- generated;
- configuration;
- documentation;
- binary;
- executable;
- archive;
- secret-sensitive;
- dependency;
- build output;
- cache;
- temporary;
- and unknown.

Classification should influence:

- indexing;
- patch preview;
- secret scanning;
- approval;
- and default exclusions.

---

# 14. Exclusions

## 14.1 Sources

Exclusions may come from:

- built-in defaults;
- project configuration;
- `.gitignore`;
- `.opureignore`;
- tool-specific ignore files;
- developer selection;
- security policy;
- and temporary workflow scope.

---

## 14.2 Precedence

Security exclusions must not be weakened silently by lower-priority project rules.

---

## 14.3 Default Exclusions

Common defaults should include:

- `.git`;
- dependency folders;
- build output;
- caches;
- temporary files;
- model storage;
- large binary artefacts;
- and known credential stores.

---

## 14.4 Visibility

The developer must be able to inspect why a file is excluded.

---

## 14.5 Explicit Inclusion

A developer may explicitly include an excluded file where policy permits.

---

# 15. File Reading

## 15.1 Read Contract

A file-read response should include:

- file identity;
- canonical relative path;
- content or stream;
- content hash;
- size;
- encoding;
- byte-order mark;
- line-ending style;
- binary classification;
- and revision metadata.

---

## 15.2 Bounded Reads

Large files should support:

- ranges;
- streaming;
- line slices;
- symbol ranges;
- and maximum-size limits.

---

## 15.3 Binary Files

Binary content must not be decoded as text without evidence.

---

## 15.4 Changed During Read

If a file changes during a read, the response must identify that the revision may be inconsistent or retry the read safely.

---

## 15.5 Locked Files

Locked or inaccessible files should return a clear error without weakening access controls.

---

# 16. Encoding

## 16.1 Preservation

Patch application should preserve the existing file encoding where practical.

---

## 16.2 Detection

Encoding detection must distinguish:

- confirmed encoding;
- BOM-declared encoding;
- configured encoding;
- inferred encoding;
- and unknown.

---

## 16.3 Invalid Text

Invalid byte sequences must not be silently replaced during a protected write unless the developer approves conversion.

---

## 16.4 New Files

New text files should use project convention where known.

Otherwise UTF-8 without BOM may be the default, subject to an ADR.

---

# 17. Line Endings

## 17.1 Preservation

Modifying a file should preserve its existing line-ending style unless the patch explicitly changes it.

---

## 17.2 Mixed Endings

Mixed line endings must be detected.

Opure should avoid normalising the entire file accidentally.

---

## 17.3 Project Convention

New files should follow detected or configured project convention.

---

## 17.4 Diff Noise

Line-ending-only changes must be identified clearly.

---

# 18. File Watching

## 18.1 Purpose

File watching helps Opure detect:

- external edits;
- renames;
- deletes;
- creates;
- and conflicts.

---

## 18.2 Event Reliability

Filesystem watcher events may be incomplete, duplicated or reordered.

They must not be the sole source of truth.

---

## 18.3 Reconciliation

Watcher events should trigger metadata reconciliation.

---

## 18.4 Event Coalescing

Rapid event bursts may be coalesced.

Significant final state must remain accurate.

---

## 18.5 Watcher Overflow

Watcher overflow must trigger bounded rescan or stale-state marking.

---

# 19. Snapshots

## 19.1 Snapshot Purpose

Snapshots support:

- patch bases;
- conflict detection;
- rollback;
- comparison;
- and recovery.

---

## 19.2 Snapshot Types

Supported snapshot types may include:

- metadata snapshot;
- content-hash snapshot;
- selected-content snapshot;
- full workspace snapshot;
- and repository-backed snapshot.

---

## 19.3 Snapshot Scope

A snapshot must declare:

- project;
- roots;
- included files;
- excluded files;
- creation time;
- source revision;
- and completeness.

---

## 19.4 Snapshot Storage

Snapshot storage should minimise duplication through content addressing or repository references where practical.

---

## 19.5 Sensitive Content

Snapshots containing sensitive files require protected storage and bounded retention.

---

## 19.6 Snapshot Validity

A snapshot must identify whether it is:

- complete;
- partial;
- stale;
- corrupted;
- or unavailable.

---

# 20. Patch Format

## 20.1 Requirements

The Opure patch format must be:

- versioned;
- machine-validated;
- human-reviewable;
- encoding-aware;
- path-safe;
- conflict-detectable;
- and capable of representing multi-file change.

---

## 20.2 Patch Header

A patch should include:

- patch identifier;
- patch-set identifier;
- format version;
- project identifier;
- base snapshot;
- creator;
- workflow reference;
- intent summary;
- creation time;
- risk;
- and required validation.

---

## 20.3 File Operation Fields

Each file operation should include:

- operation type;
- file identity where known;
- source path;
- destination path where applicable;
- base revision;
- expected content hash;
- resulting content hash where known;
- encoding;
- line endings;
- mode or attributes where supported;
- and content change representation.

---

## 20.4 Change Representation

A text modification may use:

- unified diff;
- structured edit operations;
- complete replacement;
- syntax-aware operation;
- or another versioned representation.

The chosen representation must support reliable preview and apply.

---

## 20.5 Binary Changes

Binary changes must use a separate explicit representation.

Binary replacement must show size, hash and source.

---

## 20.6 Patch Metadata

Patch metadata must not be trusted as evidence without validation.

---

# 21. Patch Lifecycle

A patch should use states such as:

- Draft;
- Generated;
- Parsed;
- Validating;
- Invalid;
- Ready for Review;
- Approval Required;
- Approved;
- Applying;
- Applied;
- Applied with Warnings;
- Conflicted;
- Rejected;
- Reversing;
- Reversed;
- Failed;
- Recovery Required;
- Superseded;
- and Expired.

---

## 21.1 Immutable Versions

Material edits to a patch should create a new patch version.

---

## 21.2 Superseding

A revised patch should supersede the earlier version without erasing its history.

---

## 21.3 Expiry

A patch may expire when its base is too stale or policy requires renewed review.

---

# 22. Patch Creation

## 22.1 Sources

A patch may be created by:

- developer edit;
- Workflow Engine;
- AI-assisted Coder role;
- plugin;
- MCP capability;
- deterministic refactoring tool;
- formatter;
- migration tool;
- or import.

---

## 22.2 Source Identity

The patch must identify its source and version.

---

## 22.3 No Direct Write

Patch creation must not write to the workspace.

---

## 22.4 Base Revision

Every modify, delete, rename or move operation should identify the base revision.

---

## 22.5 Intent

A patch should include a concise explanation of why each file is changed.

---

# 23. Diff Engine

## 23.1 Text Diff

The Diff Engine should produce:

- line-oriented diff;
- optional word-level highlighting;
- and optional syntax-aware structure.

---

## 23.2 Rename and Move

Renames and moves should be shown as such where evidence supports them.

---

## 23.3 Whitespace

Whitespace-only changes should be distinguishable.

---

## 23.4 Large Changes

Large generated or vendor-file changes should be summarised with the option to inspect full detail.

---

## 23.5 Binary Diff

Binary files should show:

- operation;
- previous size and hash;
- new size and hash;
- and preview where supported.

---

# 24. Patch Preview

## 24.1 Required Information

Patch preview must show:

- files created;
- files modified;
- files deleted;
- files renamed or moved;
- line additions and deletions;
- binary changes;
- secret-scan result;
- conflicts;
- validation status;
- risk;
- reversibility;
- and source.

---

## 24.2 File-Level Review

The developer should be able to accept or reject file operations individually where the patch structure permits.

---

## 24.3 Hunk-Level Review

Text patches should support hunk-level review where technically practical.

---

## 24.4 Exact Content

The developer must be able to inspect exact proposed content before application.

---

## 24.5 Hidden Changes

A patch must not contain hidden file changes not represented in preview.

---

# 25. Patch Validation

## 25.1 Validation Stages

Validation should include:

1. schema validation;
2. project identity validation;
3. path validation;
4. workspace-boundary validation;
5. file-type validation;
6. base revision validation;
7. encoding validation;
8. line-ending validation;
9. operation-order validation;
10. conflict detection;
11. secret scanning;
12. policy evaluation;
13. optional syntax validation;
14. optional repository validation;
15. and approval validation.

---

## 25.2 Validation Result

A validation result should include:

- passed checks;
- warnings;
- failures;
- conflicts;
- secret findings;
- stale assumptions;
- and required action.

---

## 25.3 Validation Freshness

Validation is tied to a workspace state.

A material workspace change may invalidate previous validation.

---

# 26. Path Validation

Patch paths must be rejected when they:

- escape the workspace;
- resolve through unsafe links;
- target reserved names;
- use unsupported alternate streams;
- contain invalid path components;
- conflict by case on case-insensitive filesystems;
- or cross an unapproved root.

---

# 27. Operation Ordering

## 27.1 Dependency Ordering

Patch operations must be ordered safely.

Examples:

- create parent directory before file;
- move source before creating conflicting destination;
- write temporary file before atomic replace;
- delete file before deleting empty directory;
- and update references before final destructive step where required.

---

## 27.2 Cycles

Rename or move cycles must be handled through temporary paths or rejected safely.

---

## 27.3 Duplicate Targets

Multiple operations targeting the same final resource require explicit reconciliation.

---

# 28. Conflict Detection

## 28.1 Conflict Types

Conflicts may include:

- base hash mismatch;
- file missing;
- unexpected file exists;
- destination exists;
- rename collision;
- encoding changed;
- line endings changed materially;
- permission changed;
- file locked;
- repository state changed;
- symlink target changed;
- and patch already applied.

---

## 28.2 Strict Conflict

A strict conflict prevents safe automatic apply.

---

## 28.3 Soft Conflict

A soft conflict may allow apply with warning, such as non-functional metadata change.

---

## 28.4 Three-Way Merge

The Patch Service may support three-way merge when:

- a reliable base exists;
- file type is supported;
- conflicts can be represented;
- and developer review remains possible.

---

## 28.5 AI Merge Assistance

AI may propose a conflict resolution through the Workflow Engine.

The result must be a new patch version and must not be applied silently.

---

# 29. Secret Scanning

## 29.1 Mandatory Scan

Generated or imported patches must be scanned before application.

---

## 29.2 Scan Scope

Scanning should inspect:

- added content;
- modified content;
- new files;
- renamed sensitive files;
- binary metadata where possible;
- and destination paths.

---

## 29.3 Findings

A finding should include:

- rule;
- file;
- range where safe;
- severity;
- confidence;
- and recommended action.

The secret value itself must not be copied into logs or Trust Centre records.

---

## 29.4 Blocking

High-confidence secret findings should block application by default.

---

## 29.5 False Positive Override

A developer may override a false positive through a bounded approval.

---

# 30. Policy and Approval

## 30.1 Protected Action

Patch application is a protected action.

---

## 30.2 Policy Inputs

Policy evaluation should consider:

- project;
- initiating service or plugin;
- patch scope;
- file classifications;
- secret findings;
- risk;
- reversibility;
- repository state;
- and approval.

---

## 30.3 Approval Binding

Approval must bind to the exact patch version and validated file set.

---

## 30.4 Material Change

Any material patch change invalidates approval.

---

## 30.5 Partial Approval

If file-level or hunk-level approval is supported, the resulting approved subset must become a new exact patch version or application plan.

---

# 31. Transactional Apply

## 31.1 Goal

Patch application should be all-or-known.

The system must not leave the developer uncertain about which operations completed.

---

## 31.2 Transaction Phases

A patch apply should use:

1. preflight validation;
2. exclusive or coordinated workspace lock;
3. transaction journal creation;
4. pre-apply snapshot;
5. staging of new content;
6. staged-content verification;
7. ordered filesystem operations;
8. resulting-state verification;
9. journal completion;
10. lock release;
11. event publication;
12. and Trust Centre recording.

---

## 31.3 Staging Area

New content should be written to a managed staging area before final replacement.

---

## 31.4 Atomic Replace

Atomic filesystem replacement should be used where supported and safe.

---

## 31.5 Multi-File Limits

A multi-file patch cannot usually be truly atomic at filesystem level.

Opure must use journalling, snapshots and compensation to achieve known recoverable state.

It must not claim stronger atomicity than provided.

---

## 31.6 Apply Verification

After each operation, the resulting resource should be verified against expected hash or metadata.

---

# 32. Workspace Locking

## 32.1 Purpose

Locking coordinates Opure writes and detects unsafe concurrent change.

---

## 32.2 Lock Scope

Locks may be:

- file;
- directory;
- patch set;
- or workspace transaction.

---

## 32.3 External Editors

Opure must not assume external editors respect Opure locks.

File revisions must still be checked before final replace.

---

## 32.4 Lock Timeout

Locks must have bounded acquisition and recovery behaviour.

---

## 32.5 Stale Locks

Stale locks after crash must be detected safely.

---

# 33. Create File

A create-file operation must validate:

- parent boundary;
- destination absence unless replacement is explicit;
- filename validity;
- encoding;
- line endings;
- content hash;
- and permissions.

Unexpected existing content must cause conflict.

---

# 34. Modify File

A modify-file operation must validate:

- file identity;
- base revision;
- current content;
- encoding;
- line endings;
- write permission;
- and staged output.

---

# 35. Delete File

A delete-file operation must:

- identify the exact base revision;
- show deletion clearly;
- create recovery evidence where practical;
- and require stronger approval for sensitive or broad deletion.

---

# 36. Rename and Move

Rename or move must validate:

- source identity;
- destination boundary;
- destination collision;
- case-only rename behaviour;
- repository implications;
- and references where known.

---

## 36.1 Case-Only Rename

Case-only renames on Windows require explicit safe handling.

---

# 37. Directory Operations

## 37.1 Create Directory

Directory creation should be explicit where required by the patch.

---

## 37.2 Delete Directory

Non-empty directory deletion is a high-risk operation and should not be represented as a simple directory operation without enumerating affected content.

---

## 37.3 Implicit Parents

Parent directory creation may be implicit only when shown in preview.

---

# 38. File Attributes and Permissions

The initial implementation may preserve existing file attributes and basic permissions.

Changes to:

- executable state;
- hidden state;
- read-only state;
- ACL;
- ownership;
- or extended metadata

must be explicit and may require stronger approval.

---

# 39. Binary Files

## 39.1 Explicit Handling

Binary replacement must be explicit.

---

## 39.2 Size Limits

Large binary patches require size and storage controls.

---

## 39.3 Review

The preview should provide:

- type;
- size;
- hashes;
- source;
- and visual preview where supported.

---

## 39.4 Delta Formats

Binary delta formats may be supported later.

---

# 40. Generated and Vendor Files

Changes to generated or vendor-managed files should include a warning.

Opure should prefer changing the source generator or dependency declaration when appropriate.

---

# 41. Repository Integration

## 41.1 Responsibility Boundary

The Patch Service changes workspace files.

The Repository Service manages Git or other version-control state.

---

## 41.2 Repository Status

Patch preview should identify:

- currently modified files;
- untracked files;
- staged files;
- and repository conflicts where available.

---

## 41.3 Existing Developer Changes

Opure must preserve unrelated developer changes.

---

## 41.4 Staging

Patch application must not automatically stage changes unless explicitly requested and approved.

---

## 41.5 Commit

Patch application must not automatically commit unless a separate repository action is approved.

---

## 41.6 Repository Snapshot

A clean Git commit may act as recovery evidence, but Opure must not require a clean repository for all patch operations.

---

# 42. Existing Uncommitted Changes

## 42.1 Detection

Before applying, Opure should detect relevant uncommitted changes.

---

## 42.2 Preservation

Unrelated changes must not be overwritten.

---

## 42.3 Overlap

If a patch overlaps developer changes, it must conflict or use a reviewable merge.

---

## 42.4 Dirty Workspace

A dirty workspace is not automatically unsafe.

The exact affected files determine risk.

---

# 43. Validation After Apply

After application, Opure should perform:

- resulting hash verification;
- syntax check where available;
- repository diff;
- secret rescan;
- and configured build or test validation.

---

## 43.1 Failure

Post-apply validation failure does not automatically mean the patch should be reversed.

The workflow or developer must decide based on policy and recovery options.

---

# 44. Reversal

## 44.1 Reverse Patch

The Patch Service should produce a reverse patch or equivalent recovery plan from the pre-apply snapshot.

---

## 44.2 Reverse Validation

Reversal must validate current state before modifying files.

---

## 44.3 New Changes

If files changed after apply, reversal may conflict.

---

## 44.4 Partial Reverse

A partial reverse must report exact outcome.

---

## 44.5 Repository Revert

Repository revert is a Repository Service operation and is distinct from Patch Service reversal.

---

# 45. Compensation

When exact reversal is unavailable, Opure may offer compensation such as:

- restore selected files;
- recreate deleted file from snapshot;
- remove newly created file;
- or generate a corrective patch.

Compensation must not be labelled full rollback unless it truly restores prior state.

---

# 46. Recovery Journal

## 46.1 Journal Contents

A transaction journal should contain:

- transaction identifier;
- patch identifier and version;
- project;
- initial workspace state;
- operation plan;
- current operation;
- staged paths;
- completed operations;
- verification results;
- recovery instructions;
- and final state.

---

## 46.2 Journal Durability

The journal must be durable before protected writes begin.

---

## 46.3 Secret Safety

The journal must not duplicate secret content unnecessarily.

---

## 46.4 Completion

A completed journal should be marked and retained according to recovery policy.

---

# 47. Crash Recovery

After abnormal interruption, Opure must:

1. detect incomplete transaction journal;
2. enter controlled recovery;
3. stop automatic related writes;
4. inspect staged content;
5. inspect current workspace state;
6. determine completed operations;
7. verify hashes;
8. offer or perform safe completion, compensation or restore according to policy;
9. preserve evidence;
10. and report exact final state.

---

## 47.1 No Blind Replay

An interrupted patch must not be blindly replayed.

---

## 47.2 Developer Changes After Crash

Recovery must detect edits made after the crash.

---

## 47.3 Recovery Outcome

Outcomes may be:

- safely completed;
- safely reversed;
- partially recovered;
- no change required;
- conflict requires developer action;
- or recovery failed.

---

# 48. File Backup and Snapshot Retention

## 48.1 Retention

Pre-apply snapshots should use bounded retention.

---

## 48.2 Storage Visibility

The developer should be able to inspect storage used by snapshots and staged files.

---

## 48.3 Sensitive Files

Sensitive snapshots require protected storage.

---

## 48.4 Cleanup

Cleanup must not remove snapshots required by an incomplete transaction.

---

# 49. Patch Import and Export

## 49.1 Export

A patch may be exported in a documented format.

---

## 49.2 Import

Imported patches are untrusted until fully validated.

---

## 49.3 Signature

A future signed patch format may prove origin and integrity.

It does not prove correctness.

---

## 49.4 Portability

Exported patches should avoid machine-specific absolute paths.

---

# 50. Patch Provenance

Every patch must identify:

- creator;
- creator version;
- workflow;
- AI provider and model where applicable;
- context reference;
- plugin or MCP source where applicable;
- base snapshot;
- developer edits;
- validation;
- approval;
- and application outcome.

---

# 51. Trust Centre Integration

The Trust Centre should record:

- patch creation;
- patch source;
- patch version;
- files affected;
- secret-scan result;
- validation;
- conflicts;
- approval;
- apply start;
- apply outcome;
- reversal;
- recovery;
- and post-apply validation.

Full file contents should not be duplicated unnecessarily.

---

# 52. Knowledge Engine Integration

After file changes, the Patch Service should publish authoritative events so the Knowledge Engine can:

- mark affected records stale;
- update file hashes;
- reparse changed files;
- update relationships;
- associate patch with issue or workflow;
- and record validation evidence.

---

# 53. Build Manager Integration

The Build Manager may validate a patch:

- before apply against a temporary worktree or staged workspace;
- after apply in the active workspace;
- or both.

Exact isolated-validation techniques are DEFERRED.

---

# 54. Plugin and MCP Integration

Plugins and MCP servers may:

- request approved file reads;
- propose patches;
- query patch status;
- and receive validation results.

They may not:

- write files directly;
- bypass path validation;
- bypass secret scanning;
- or apply patches without policy.

---

# 55. Desktop Experience Requirements

The desktop interface should show:

- workspace health;
- current file state;
- patch summary;
- per-file diff;
- conflicts;
- secret findings;
- risk;
- approval;
- application progress;
- cancellation state;
- reverse option;
- and recovery status.

The desktop remains a view and command surface.

The Patch Service remains authoritative.

---

# 56. Patch API

## 56.1 Commands

The Patch Service should provide:

- `CreatePatch`
- `ImportPatch`
- `UpdatePatch`
- `ValidatePatch`
- `ApprovePatch`
- `RejectPatch`
- `ApplyPatch`
- `CancelPatchApply`
- `ReversePatch`
- `ResolvePatchConflict`
- `ExpirePatch`
- and `DeletePatch`

---

## 56.2 Queries

It should provide:

- `GetPatch`
- `ListPatches`
- `GetPatchPreview`
- `GetPatchValidation`
- `GetPatchConflicts`
- `GetPatchSecretFindings`
- `GetPatchApplication`
- `GetPatchReversePlan`
- and `GetPatchRecoveryState`

---

## 56.3 Events

It should publish:

- `PatchCreated`
- `PatchUpdated`
- `PatchValidated`
- `PatchConflictDetected`
- `PatchApprovalRequired`
- `PatchApproved`
- `PatchRejected`
- `PatchApplyStarted`
- `PatchApplied`
- `PatchApplyFailed`
- `PatchReversalStarted`
- `PatchReversed`
- `PatchRecoveryRequired`
- and `WorkspaceFilesChanged`

---

# 57. Workspace API

## 57.1 Commands

The Workspace Service should provide:

- `RegisterWorkspace`
- `UpdateWorkspaceRoot`
- `CloseWorkspace`
- `CreateSnapshot`
- `RefreshWorkspace`
- `UpdateExclusions`
- and `StartFileWatch`

---

## 57.2 Queries

It should provide:

- `GetWorkspace`
- `ListWorkspaceRoots`
- `ResolvePath`
- `GetFileMetadata`
- `ReadFile`
- `ReadFileRange`
- `ListDirectory`
- `GetFileRevision`
- `GetSnapshot`
- `GetExclusionReason`
- and `GetWorkspaceHealth`

---

## 57.3 Events

It should publish:

- `WorkspaceRegistered`
- `WorkspaceRootChanged`
- `FileCreated`
- `FileModified`
- `FileDeleted`
- `FileRenamed`
- `WorkspaceStateStale`
- `WorkspaceRescanStarted`
- and `WorkspaceRescanCompleted`

---

# 58. Error Model

Recommended stable error codes include:

- `WORKSPACE_NOT_FOUND`
- `WORKSPACE_ROOT_INVALID`
- `WORKSPACE_ACCESS_DENIED`
- `WORKSPACE_PATH_OUTSIDE_BOUNDARY`
- `WORKSPACE_PATH_TRAVERSAL`
- `WORKSPACE_LINK_UNSAFE`
- `WORKSPACE_FILE_NOT_FOUND`
- `WORKSPACE_FILE_LOCKED`
- `WORKSPACE_FILE_CHANGED`
- `WORKSPACE_ENCODING_UNKNOWN`
- `WORKSPACE_BINARY_UNSUPPORTED`
- `WORKSPACE_WATCHER_OVERFLOW`
- `PATCH_INVALID`
- `PATCH_PATH_INVALID`
- `PATCH_BASE_MISMATCH`
- `PATCH_CONFLICT`
- `PATCH_SECRET_DETECTED`
- `PATCH_APPROVAL_REQUIRED`
- `PATCH_APPROVAL_INVALID`
- `PATCH_POLICY_DENIED`
- `PATCH_APPLY_FAILED`
- `PATCH_VERIFY_FAILED`
- `PATCH_REVERSE_CONFLICT`
- `PATCH_RECOVERY_REQUIRED`
- `PATCH_CANCELLED`
- and `PATCH_INTERNAL_ERROR`

---

# 59. Security Requirements

## 59.1 Canonical Decisions

All path security decisions must use canonical paths and file identities.

---

## 59.2 Temporary Files

Temporary and staging files must use secure names and managed directories.

---

## 59.3 ACLs

Staged sensitive content should use restrictive permissions.

---

## 59.4 Log Safety

File contents and secrets must not enter logs unnecessarily.

---

## 59.5 Untrusted Patch Content

Patch content must be treated as untrusted input.

---

## 59.6 Decompression

Imported patch archives must protect against:

- path traversal;
- decompression bombs;
- duplicate paths;
- and malicious metadata.

---

# 60. Testing Strategy

## 60.1 Unit Tests

Unit tests must cover:

- path canonicalisation;
- boundary enforcement;
- path traversal;
- case handling;
- encoding;
- line endings;
- patch parsing;
- operation ordering;
- conflict detection;
- approval binding;
- and recovery-state transitions.

---

## 60.2 Filesystem Tests

Tests should cover:

- NTFS;
- long paths;
- UNC where supported;
- symbolic links;
- junctions;
- hard links;
- read-only files;
- locked files;
- case-only rename;
- and alternate data streams.

---

## 60.3 Patch Tests

Tests must cover:

- create;
- modify;
- delete;
- rename;
- move;
- multi-file patch;
- binary replacement;
- mixed line endings;
- stale base;
- duplicate target;
- and invalid path.

---

## 60.4 Transaction Tests

Tests must simulate failure:

- before staging;
- during staging;
- before first replace;
- between file operations;
- after final write but before journal completion;
- during verification;
- and during reversal.

---

## 60.5 Conflict Tests

Tests must cover:

- external edit;
- deleted base file;
- unexpected destination;
- encoding change;
- permission change;
- and repository conflict.

---

## 60.6 Secret Tests

Tests must cover secrets in:

- new file;
- added line;
- renamed file;
- `.env`;
- private key;
- binary metadata where detectable;
- and false positive override.

---

## 60.7 Security Tests

Tests must include:

- traversal;
- unsafe symlink;
- junction escape;
- archive traversal;
- oversized patch;
- malformed encoding;
- alternate stream;
- and plugin bypass attempt.

---

## 60.8 Recovery Tests

Tests must prove:

- incomplete operations are detected;
- journals identify exact progress;
- blind replay is prevented;
- external post-crash edits are preserved;
- and final state is reported accurately.

---

## 60.9 Repository Tests

Tests should cover:

- clean repository;
- dirty unrelated files;
- overlapping changes;
- staged changes;
- untracked destination;
- and line-ending configuration.

---

# 61. Performance Requirements

## 61.1 Large Files

Large files must support streaming and bounded diff behaviour.

---

## 61.2 Large Patch Sets

Large patch sets should support incremental preview and validation.

---

## 61.3 Hashing

Hashing should be incremental or cached using safe revision metadata where practical.

---

## 61.4 File Watching

File watching must not consume excessive CPU when idle.

---

## 61.5 Apply Latency

Small patches should apply promptly after approval.

---

## 61.6 Background Work

Snapshot cleanup and rescanning must use Scheduler priorities.

---

# 62. Initial Implementation Milestone

The first Workspace and Patch Engine milestone is successful when it can:

1. register one Windows workspace root;
2. canonicalise and validate paths;
3. reject traversal outside the root;
4. read text files with encoding and line-ending metadata;
5. create file revisions using content hashes;
6. watch for external file changes;
7. create a selected-file snapshot;
8. parse a versioned text patch;
9. preview create, modify, delete and rename operations;
10. validate base revisions;
11. detect a conflict after external edit;
12. scan proposed changes for common secrets;
13. request policy and approval;
14. stage new content;
15. apply a multi-file patch using a durable journal;
16. verify resulting hashes;
17. generate a reverse plan;
18. recover after simulated interruption;
19. publish workspace-change events;
20. and record the outcome in the Trust Centre.

The first milestone may limit binary editing and advanced merge support.

---

# 63. Acceptance Criteria

SPEC-009 is implemented when:

- [ ] Every workspace has an explicit canonical boundary.
- [ ] Path traversal and unsafe link escape are blocked.
- [ ] Windows path semantics are handled correctly.
- [ ] File identity does not rely only on display path.
- [ ] File reads expose revision, encoding and line endings.
- [ ] Existing encoding and line endings are preserved where practical.
- [ ] Exclusions are visible and explainable.
- [ ] File-watch events are reconciled rather than blindly trusted.
- [ ] Snapshots identify completeness and source state.
- [ ] Patches are versioned and human-reviewable.
- [ ] Patch operations identify exact base revisions.
- [ ] Patch preview shows every affected file.
- [ ] Hidden file changes are impossible.
- [ ] Validation checks schema, path, base, encoding, conflicts, secrets, policy and approval.
- [ ] Validation is invalidated by material workspace change.
- [ ] Secret findings block application by default.
- [ ] Approval binds to the exact patch version.
- [ ] Material patch changes invalidate approval.
- [ ] Existing unrelated developer changes are preserved.
- [ ] Overlapping developer changes produce conflict or reviewable merge.
- [ ] Patch application uses staging, journalling and verification.
- [ ] Multi-file atomicity is described honestly.
- [ ] Interrupted application produces a known recoverable state.
- [ ] Blind replay after crash is prohibited.
- [ ] Reverse operations validate current state.
- [ ] Partial reversal reports exact outcome.
- [ ] Patch application does not automatically stage or commit in Git.
- [ ] Plugins, MCP and AI cannot write workspace files directly.
- [ ] Workspace events invalidate relevant project memory.
- [ ] Sensitive temporary and snapshot content is protected.
- [ ] Trust Centre records contain no secret values.
- [ ] Architecture tests detect direct-write bypass.
- [ ] Recovery tests pass for every interruption phase.
- [ ] The developer remains able to edit the project with ordinary tools outside Opure.

---

# 64. Deferred Decisions

The following are intentionally deferred:

- exact patch serialisation format;
- exact diff library;
- syntax-aware diff implementation;
- three-way merge library;
- binary delta support;
- snapshot storage technology;
- content-addressed storage;
- filesystem transaction abstraction;
- workspace-lock implementation;
- isolated validation worktrees;
- multi-root projects;
- remote workspaces;
- network shares as supported roots;
- file ACL editing;
- and collaborative concurrent editing.

These decisions must not weaken:

- workspace boundaries;
- reviewability;
- conflict detection;
- secret scanning;
- journalling;
- recovery;
- or developer ownership.

---

# 65. Required Architecture Decision Records

Implementation should produce ADRs for:

- workspace path canonicalisation;
- file identity model;
- content hashing;
- patch format;
- diff engine;
- encoding detection;
- line-ending preservation;
- snapshot storage;
- transaction journal format;
- atomic file replacement on Windows;
- workspace locking;
- conflict and merge strategy;
- secret-scan integration;
- and recovery workflow.

---

# 66. Relationship to Later Specifications

This specification provides workspace and change-management foundations for:

- **SPEC-010 — Desktop User Interface**
- **SPEC-011 — Project and Build Management**
- **SPEC-012 — Roadmap**

SPEC-010 will define the review, diff, conflict, approval and recovery experience.

SPEC-011 will define repository, build and test integration around workspace changes.

Later specifications may add stricter requirements.

They must not allow direct protected workspace writes outside the Patch Service.

---

# 67. Founder Approval

This document remains a founder draft until explicitly approved.

Approval establishes the following rules:

- The developer's workspace remains authoritative.
- Every protected file change passes through the Patch Service.
- Patches are reviewable before application.
- Exact base revisions are recorded.
- Existing developer changes are preserved.
- Conflicts are detected rather than overwritten.
- Secrets are scanned before generated changes are applied.
- Approval binds to the exact validated patch.
- Application uses staging, journalling and resulting-state verification.
- Multi-file atomicity is described honestly.
- Interrupted operations enter known recovery state.
- Reversal is attempted only after current-state validation.
- AI, plugins and MCP servers may propose changes but cannot write directly.
- Git staging and commit remain separate explicit operations.
- The developer may continue using ordinary editors and tools outside Opure.
- Opure exists to make project changes safer, clearer and more reversible.

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**