# ADR-0009 — Windows Path and Filesystem Handling

## Architecture Decision Record

**Status:** Proposed  
**Date:** 18 July 2026  
**Decision owners:** Founder and Product Owner  
**Technical owners:** Workspace and Filesystem Architecture Owner  
**Reviewers:** Security Owner, Patch Engine Owner, Runtime Architecture Owner, Repository Owner, Build Owner, Recovery Owner, Desktop Owner, Test Architecture Owner  
**Supersedes:** None  
**Superseded by:** None  
**Related ADRs:** ADR-0001 Primary Implementation Language, ADR-0003 Runtime Process Topology, ADR-0004 Local IPC, ADR-0005 Persistence, ADR-0006 Logging and Observability, ADR-0007 Secrets Vault, ADR-0008 Testing Strategy  
**Related specifications:** CHARTER-001, SPEC-001, SPEC-002, SPEC-003, SPEC-007, SPEC-008, SPEC-009, SPEC-010, SPEC-011, SPEC-012  
**Target milestone:** Phase 0 — Founding Baseline through Phase 3 — Reviewable Patch Vertical Slice  

---

## 1. Decision Summary

Opure should implement a dedicated, Windows-aware filesystem boundary for all project and internal file operations.

The first implementation should:

- treat every path received from a developer, plugin, model, MCP server, configuration file, repository, archive or external tool as untrusted input;
- represent path states through distinct strongly typed values rather than passing ordinary strings through trusted services;
- resolve relative paths against an explicit immutable base with `Path.GetFullPath(path, basePath)`;
- reject drive-relative paths, root-relative paths, device paths, NT object paths, named-pipe paths and alternate-data-stream syntax from ordinary workspace operations;
- opt all shipped Windows executables into long-path support through `longPathAware`;
- keep extended-path namespace details such as `\\?\` inside the Windows platform adapter;
- capture workspace-root volume identity, file identity, filesystem capabilities and case-sensitivity state at registration;
- inspect every existing path component for reparse-point behaviour;
- use handle-based identity and final-path verification for protected reads and writes;
- compare volume serial number and file ID when determining whether two existing paths refer to the same object;
- never use string prefix checks as the sole workspace-containment or authorisation test;
- treat symbolic links, junctions, mounted folders, cloud placeholders and unknown reparse tags according to explicit policy;
- default to denying protected traversal through unknown or out-of-workspace name-surrogate reparse points;
- identify case-sensitive directories and preserve exact case;
- reject ambiguous case collisions;
- detect hard-linked files before patch application;
- reject ordinary access to named alternate data streams;
- stage replacement files in the destination directory or same volume;
- use Windows `ReplaceFileW` or the verified .NET equivalent for replacement of existing ordinary files when its preconditions hold;
- use same-directory or same-volume atomic rename for new files;
- use durable patch journals and recovery rather than claiming multi-file filesystem atomicity;
- revalidate path, file identity and expected revision immediately before mutation;
- and expose filesystem capability and risk information to the developer.

All trusted file reads and writes should pass through the Workspace Service, Patch Service or a narrowly approved platform storage service.

Plugins, AI providers, MCP servers, the Desktop and ordinary workers must not perform unrestricted project filesystem access.

---

## 2. Status

Current status:

> **Proposed**

This ADR becomes **Accepted** only after prototypes demonstrate:

- deterministic path normalisation;
- long-path support;
- reserved-name rejection;
- device-path rejection;
- workspace containment;
- symbolic-link and junction handling;
- mounted-folder handling;
- cloud-placeholder handling;
- unknown reparse-tag denial;
- case-sensitive directory handling;
- case-collision detection;
- hard-link detection;
- alternate-data-stream detection;
- file-identity comparison;
- time-of-check to time-of-use attack resistance;
- same-volume staged replacement;
- multi-file interruption recovery;
- FileSystemWatcher overflow reconciliation;
- network and removable-volume capability handling;
- and successful Windows filesystem security testing.

---

## 3. Context

Opure reads and changes developer-controlled repositories.

Project trees may contain:

- ordinary files;
- directories;
- symbolic links;
- junctions;
- hard links;
- mounted folders;
- cloud placeholders;
- sparse files;
- encrypted files;
- compressed files;
- alternate data streams;
- files with unusual Unicode names;
- paths longer than legacy `MAX_PATH`;
- case-sensitive directories;
- generated build trees;
- package caches;
- Git internals;
- and files concurrently changed by editors or tools.

Path strings on Windows can represent:

- drive-rooted paths;
- drive-relative paths;
- root-relative paths;
- UNC paths;
- device paths;
- Win32 extended paths;
- DOS device names;
- NT object-manager paths;
- named streams;
- and filesystem-filter-controlled objects.

A path string is therefore not a reliable object identity.

A lexically normalised path can still:

- traverse a junction;
- resolve through a symbolic link;
- reach another volume;
- target a cloud placeholder;
- be swapped after validation;
- identify a file through a hard link;
- or be interpreted differently by an external tool.

The Patch Service requires particularly strong guarantees.

Before applying a patch it must know:

- the intended workspace;
- the intended relative path;
- the current object;
- the expected revision;
- the reparse behaviour;
- the volume;
- the stream;
- and whether the object changed after review.

This ADR defines the filesystem foundation required to make those guarantees.

---

## 4. Problem Statement

Opure requires a Windows filesystem architecture that supports modern long paths and legitimate developer repositories while preventing path escape, reparse-point redirection, alternate-stream access, case ambiguity, hard-link surprises, stale-object mutation and unrecoverable partial writes.

---

## 5. Decision Drivers

The decision is evaluated against:

- alignment with the Opure Charter;
- protection of developer files;
- reviewable patch application;
- Windows 11 support;
- C# and .NET integration;
- long-path support;
- Unicode support;
- project isolation;
- path traversal defence;
- reparse-point defence;
- symbolic-link usability;
- Git repository compatibility;
- WSL interoperability;
- cloud-placeholder awareness;
- case sensitivity;
- hard-link behaviour;
- alternate data streams;
- atomic replacement;
- crash recovery;
- concurrent editor changes;
- external-tool compatibility;
- filesystem capability detection;
- observability;
- security testability;
- performance;
- cross-platform abstraction;
- and future replacement cost.

---

## 6. Governing Principles

This decision must preserve:

- **Developer Respect**
- **Developer First**
- **Software Engineering First**
- **Local by Design**
- **Human in Control**
- **Visible by Design**
- **Inspectable Decisions**
- **Reviewable Changes**
- **Reversible Wherever Technically Practical**
- **Open by Architecture**
- **Loose Coupling**
- **Least Privilege**
- **Project Isolation**
- **Performance Respect**
- **No Hidden Authority**
- **No Silent Path Escape**
- **No False Atomicity**
- **No False Success**
- **Honest Filesystem Capability**
- **Recovery Before Convenience**

Relevant requirements include:

- The Workspace Service owns project filesystem access.
- The Patch Service owns protected project mutation.
- Plugins propose changes rather than writing directly.
- AI output is untrusted.
- Every protected mutation is reviewable.
- Planned and applied state remain distinct.
- Current file state is revalidated before application.
- Secret-bearing files receive additional protection.
- External command plans display the working directory and affected roots.
- Recovery records survive process crashes.
- Windows-first behaviour must not leak into domain contracts unnecessarily.

---

## 7. Scope

This ADR decides:

- path value types;
- path parsing;
- deterministic lexical normalisation;
- fully qualified path requirements;
- long-path handling;
- path namespace policy;
- reserved-name policy;
- Unicode and case policy;
- workspace registration;
- filesystem capability discovery;
- file identity;
- volume identity;
- containment;
- reparse-point inspection;
- symbolic links;
- junctions;
- mounted folders;
- cloud placeholders;
- hard links;
- alternate data streams;
- enumeration;
- file watching;
- staging;
- replacement;
- creation;
- deletion;
- rename;
- metadata preservation;
- time-of-check to time-of-use protection;
- concurrent modification;
- error handling;
- diagnostics;
- testing;
- and cross-platform abstraction.

This ADR does not decide:

- complete patch-diff semantics;
- Git merge algorithms;
- repository ignore semantics;
- build sandboxing;
- Windows application installation paths;
- internal database layout;
- content-store layout;
- archive extraction format;
- Windows file-encryption policy;
- Windows Defender integration;
- remote workspace synchronisation;
- WSL-native Runtime execution;
- or distributed filesystems.

---

## 8. Constraints

Known constraints include:

- Windows 11 is the first supported platform.
- C#/.NET is the proposed implementation stack.
- The first release must support ordinary Git repositories on local Windows filesystems.
- Some repositories contain symbolic links.
- Some repositories are created through WSL.
- Some directories can be case sensitive.
- Some workspaces may reside in OneDrive or another cloud-synchronised tree.
- Some external tools still have legacy path limitations.
- Some filesystem operations cannot be made atomic across multiple files.
- Some file metadata is filesystem specific.
- The same path can refer to different objects over time.
- Multiple paths can refer to one hard-linked file.
- A path can be redirected by reparse points.
- `FileSystemWatcher` can lose events when its internal buffer overflows.
- Plugins and model output are untrusted.
- A malicious same-user process can race filesystem operations.
- The Patch Service must remain recoverable after process termination.
- The UI must not expose raw device namespaces as ordinary project paths.
- Cross-platform domain contracts should use workspace-relative paths.
- The first implementation must remain practical for a small team.

---

## 9. Assumptions

This decision assumes:

- Most version 1.0 workspaces reside on local NTFS or ReFS volumes.
- A workspace can be represented by one registered root and optional explicitly approved additional roots.
- Protected mutation can be denied when filesystem guarantees are insufficient.
- File identity can be queried from an open Windows handle.
- Volume serial number plus file ID provides a stronger existing-object identity than a path string.
- The Windows adapter can use narrowly reviewed P/Invoke where .NET APIs do not expose required controls.
- Long-path-aware manifests can be applied to all shipped executables.
- Patch staging can occur beside the destination or on the same volume.
- The Patch Service can keep a durable journal outside the source repository.
- External command tools may receive ordinary display paths after the Workspace Service validates them.
- The product can show unsupported or degraded workspace capability rather than pretending full safety.
- Cross-platform workspace paths can use forward-slash logical separators in contracts.
- The Windows adapter converts logical paths at the boundary.
- The source repository itself remains authoritative for source files.
- Opure internal state remains outside the repository.

---

## 10. Official Platform Evidence

Official Microsoft documentation available on 18 July 2026 establishes that:

- Windows supports several path namespaces and path forms with materially different interpretation.
- Drive-relative paths such as `C:folder\file` are not equivalent to `C:\folder\file`.
- Windows reserves device names including `CON`, `PRN`, `AUX`, `NUL`, `COM1` and `LPT1`, including names followed by extensions.
- Windows shells generally do not support names ending in a space or period.
- Windows 10 version 1607 and later support long-path opt-in through the `longPathAware` application manifest setting.
- `Path.GetFullPath(path, basePath)` can resolve a relative path deterministically against a specified base.
- reparse points can change ordinary filesystem behaviour and include symbolic links, junctions, mounted folders, cloud placeholders and other filter-controlled objects;
- `FILE_FLAG_OPEN_REPARSE_POINT` allows an application to open the reparse point itself instead of following normal reparse processing;
- .NET exposes symbolic-link and junction resolution through `ResolveLinkTarget`;
- Windows can retrieve the final path associated with an open file handle;
- volume serial number and file ID can identify whether two handles refer to the same file on one computer;
- NTFS hard links allow multiple paths to reference one file;
- Windows directories can be configured as case sensitive;
- NTFS supports named alternate data streams;
- `ReplaceFileW` replaces a file while preserving selected metadata, ACLs and named streams, and requires the replacement and replaced files to be on the same volume;
- and `FileSystemWatcher` may lose detailed events if its internal buffer overflows.

These capabilities and limitations must be verified against the pinned Windows and .NET support baseline before this ADR moves to Accepted.

---

## 11. Options Considered

The principal approaches are:

1. **Option A — String-Based Normalised Paths**
2. **Option B — Handle-Verified Workspace Filesystem Boundary**
3. **Option C — Copy Every Workspace into an Opure Sandbox**
4. **Option D — Perform All File Operations Through Git**
5. **Option E — Windows Broker Process for Every File Operation**
6. **Option F — Permit Direct Service and Plugin File Access**

---

# 12. Option A — String-Based Normalised Paths

## 12.1 Description

Use `Path.GetFullPath`, remove `.` and `..`, compare the resulting string with the workspace-root string and then use ordinary .NET file APIs.

---

## 12.2 Advantages

- Simple.
- Fast.
- Easy to understand.
- Minimal native interop.
- Good for ordinary non-adversarial paths.
- Cross-platform-looking implementation.
- Low initial development cost.

---

## 12.3 Disadvantages

- String containment does not prove object containment.
- Symbolic links and junctions can redirect traversal.
- Mounted folders can change volumes.
- A validated object can be replaced before use.
- Hard links allow different paths to one object.
- Case-sensitive directories complicate comparison.
- Device namespace paths may bypass ordinary assumptions.
- Alternate streams can hide behind colon syntax.
- Existing and non-existing paths need different validation.
- Final filesystem identity remains unknown.
- File replacement races remain possible.
- Unknown reparse tags remain unclassified.
- External tools may interpret strings differently.

---

## 12.4 Risks

- Workspace escape.
- protected file overwrite outside the project;
- secret-file access;
- junction race;
- symlink race;
- case-collision overwrite;
- alternate-stream mutation;
- and false security confidence.

---

## 12.5 Estimated Adoption Cost

- **Initial implementation:** Low
- **Operational complexity:** Low
- **Security risk:** High
- **Migration difficulty:** High

---

# 13. Option B — Handle-Verified Workspace Filesystem Boundary

## 13.1 Description

Use explicit path types, deterministic lexical normalisation, filesystem capability discovery, component-by-component reparse inspection and open-handle identity verification.

Protected mutations are staged and revalidated immediately before execution.

---

## 13.2 Advantages

- Strong workspace containment.
- Strong existing-object identity.
- Detects symlink and junction behaviour.
- Supports legitimate internal links under policy.
- Detects mounted-folder changes.
- Supports case-sensitive directories.
- Detects hard links.
- Blocks alternate-stream access.
- Provides TOCTOU resistance.
- Enables accurate developer warnings.
- Supports long paths.
- Supports atomic single-file replacement.
- Preserves service ownership.
- Supports future platform adapters.
- Testable through adversarial fixtures.
- Keeps path policy centralised.
- Enables precise recovery evidence.

---

## 13.3 Disadvantages

- More implementation complexity.
- Requires Windows-specific adapter code.
- Requires safe-handle management.
- Requires more filesystem calls.
- Non-existing leaf paths require careful parent validation.
- Reparse tags require classification.
- Cloud placeholders require special handling.
- External tools may still behave differently.
- Some filesystems provide weaker identity guarantees.
- Same-user attackers can still race or interfere.
- Multi-file operations still require journalling.

---

## 13.4 Risks

- Native interop defects.
- leaked handles;
- incorrect final-path conversion;
- incomplete component walking;
- case policy mismatch;
- false denial of legitimate repositories;
- unknown reparse tag misclassification;
- and overconfidence in file IDs across unsupported filesystems.

---

## 13.5 Mitigations

- small Windows adapter;
- SafeHandle use;
- extensive security tests;
- capability-driven behaviour;
- fail-closed protected mutations;
- explicit degraded workspace states;
- and independent review.

---

## 13.6 Estimated Adoption Cost

- **Initial implementation:** Moderate to High
- **Operational complexity:** Moderate
- **Security risk:** Low to Moderate
- **Migration difficulty:** Low

---

# 14. Option C — Copy Every Workspace into an Opure Sandbox

## 14.1 Description

Copy or clone the source repository into an Opure-owned directory and perform all operations there.

---

## 14.2 Advantages

- Strong root ownership.
- Easier permissions.
- Easier staging.
- Easier cleanup.
- Reduced interaction with external editors.
- Easier recovery.
- Predictable filesystem.

---

## 14.3 Disadvantages

- Duplicates repositories.
- Breaks developer's normal working directory.
- Large disk cost.
- File synchronisation complexity.
- Git worktree and submodule complications.
- External editor changes require merging.
- Violates developer-first expectations.
- Slow for large repositories.
- May not preserve all filesystem metadata.
- Does not eliminate malicious repository content.

---

## 14.4 Decision

Rejected as the ordinary workspace model.

Temporary isolated workspaces may be used for builds or experiments through separate policy.

---

# 15. Option D — Perform All File Operations Through Git

## 15.1 Description

Use Git plumbing and worktree operations as the sole project filesystem mutation mechanism.

---

## 15.2 Advantages

- Git-aware.
- Strong revision model.
- Existing status and diff semantics.
- Useful rollback.
- Familiar engineering tool.

---

## 15.3 Disadvantages

- Not every project is Git.
- Untracked files still matter.
- Git does not own arbitrary build or configuration files.
- Working-tree filesystem attacks remain.
- Git itself follows filesystem semantics.
- Protected mutation still requires path safety.
- External hooks may execute.
- Binary and permission behaviour varies.
- Git cannot replace the Workspace Service.

---

## 15.4 Decision

Rejected as the complete filesystem architecture.

Repository Service remains a consumer of the Workspace boundary.

---

# 16. Option E — Windows Broker Process for Every File Operation

## 16.1 Description

Place all filesystem access in a dedicated restricted process.

---

## 16.2 Advantages

- Separate memory and privilege boundary.
- Central handle ownership.
- Potential sandboxing.
- Stronger process-level policy.
- Easier audit of operations.

---

## 16.3 Disadvantages

- Additional IPC for every operation.
- More process and failure complexity.
- High-volume enumeration cost.
- Harder cancellation.
- More packaging.
- Same-user threats remain relevant.
- Premature for the first vertical slice.

---

## 16.4 Decision Relevance

A dedicated filesystem broker may be considered later for privilege separation.

The first implementation keeps the Workspace and Patch services in the trusted Runtime.

---

# 17. Option F — Direct Service and Plugin File Access

## 17.1 Description

Allow services, plugins and workers to use `System.IO` directly against project paths.

---

## 17.2 Advantages

- Fast development.
- Minimal abstractions.
- Maximum plugin flexibility.

---

## 17.3 Disadvantages

- No central policy.
- No consistent path validation.
- No patch review guarantee.
- No containment.
- No reliable recovery.
- Plugins gain broad authority.
- Difficult auditing.
- Difficult testing.
- Violates the Charter.

---

## 17.4 Decision

Rejected.

---

# 18. Comparison Matrix

Scores:

- **1** — poor
- **2** — weak
- **3** — acceptable
- **4** — strong
- **5** — excellent

| Criterion | Weight | String Paths | Handle Verified | Sandbox Copy | Git Only | Broker Process | Direct Access |
|---|---:|---:|---:|---:|---:|---:|---:|
| Charter alignment | 5 | 2 | 5 | 3 | 3 | 5 | 1 |
| Workspace containment | 5 | 2 | 5 | 5 | 2 | 5 | 1 |
| Reparse defence | 5 | 1 | 5 | 4 | 2 | 5 | 1 |
| TOCTOU resistance | 5 | 1 | 5 | 4 | 2 | 5 | 1 |
| Developer workflow | 5 | 5 | 5 | 2 | 4 | 4 | 5 |
| Long paths | 4 | 4 | 5 | 4 | 4 | 5 | 3 |
| Case sensitivity | 4 | 2 | 5 | 4 | 3 | 5 | 2 |
| Hard-link handling | 4 | 1 | 5 | 3 | 2 | 5 | 1 |
| ADS handling | 4 | 2 | 5 | 3 | 2 | 5 | 1 |
| Atomic replacement | 5 | 3 | 5 | 5 | 3 | 5 | 2 |
| Recovery | 5 | 3 | 5 | 5 | 4 | 5 | 1 |
| Small-team fit | 5 | 5 | 4 | 2 | 3 | 2 | 5 |
| Performance | 4 | 5 | 4 | 2 | 3 | 3 | 5 |
| Cross-platform abstraction | 3 | 5 | 4 | 4 | 4 | 3 | 4 |
| Testability | 5 | 3 | 5 | 4 | 3 | 5 | 2 |
| Replacement cost | 3 | 2 | 4 | 2 | 2 | 3 | 1 |
| **Indicative weighted total** |  | **251** | **417** | **313** | **268** | **384** | **193** |

The handle-verified Workspace boundary provides the strongest overall fit.

---

# 19. Decision

Opure will provisionally adopt:

> **A central handle-verified Workspace filesystem boundary using strongly typed paths, deterministic lexical normalisation, Windows filesystem capability detection, reparse inspection, file and volume identities, staged same-volume mutation and durable recovery journals.**

All protected project mutation will pass through the Patch Service.

All ordinary project access will pass through the Workspace Service or a narrowly approved read-only indexing interface.

This decision does not approve:

- direct plugin writes;
- string-prefix containment;
- arbitrary device paths;
- arbitrary alternate streams;
- automatic traversal of unknown reparse points;
- cross-volume atomicity claims;
- direct source-repository database storage;
- or silent operation on unsupported filesystem capabilities.

This decision is:

- [ ] Permanent until superseded
- [x] Provisional pending prototype evidence
- [ ] Experimental only
- [ ] Limited to Patch Service
- [ ] Limited to NTFS permanently

---

# 20. Rationale

The selected approach recognises that Windows paths are names, not durable object identities.

A secure operation therefore requires two distinct phases:

1. lexical path validation;
2. filesystem object validation.

Lexical validation protects against:

- malformed paths;
- ambiguous path forms;
- `.` and `..`;
- reserved names;
- device namespaces;
- alternate streams;
- and unsupported path classes.

Filesystem validation protects against:

- symbolic links;
- junctions;
- mounted folders;
- cloud placeholders;
- hard links;
- case-sensitive directories;
- object replacement;
- and stale review state.

This approach is more complex than string normalisation, but it directly protects the developer's repository and supports the Charter's review and recovery commitments.

---

# 21. Architectural Boundary

The intended dependency direction is:

```text
Desktop / Workflow / AI / Plugins
                ↓
Workspace and Patch Contracts
                ↓
Workspace Service / Patch Service
                ↓
Filesystem Policy and Identity
                ↓
Platform Filesystem Abstraction
                ↓
Windows Filesystem Adapter
                ↓
System.IO and narrowly reviewed Win32 APIs
```

The following direction is prohibited:

```text
Plugin or AI Adapter
        ↓
System.IO against project paths
```

---

# 22. Core Path Types

Opure should define separate path types.

Conceptual types include:

- `UntrustedPathText`
- `LogicalWorkspacePath`
- `AnchoredAbsolutePath`
- `RegisteredWorkspaceRoot`
- `ResolvedExistingPath`
- `VerifiedTargetPath`
- `FileObjectIdentity`
- `VolumeIdentity`
- `ContentRevision`
- `FilesystemCapabilitySet`

These names are provisional.

The separation is architectural.

---

# 23. Untrusted Path Text

`UntrustedPathText` represents raw input from:

- developer;
- model;
- plugin;
- MCP server;
- repository;
- archive;
- build output;
- configuration;
- or external service.

It carries no authority.

It must not expose implicit conversion to a trusted path.

---

# 24. Logical Workspace Path

A logical workspace path is:

- relative to one registered workspace root;
- segmented;
- normalised;
- free of `.` and `..`;
- free of empty ordinary segments;
- free of drive or UNC prefixes;
- free of stream syntax;
- and independent of Windows path separators at the contract layer.

Suggested contract form:

```text
src/Opure.Runtime/Program.cs
```

---

## 24.1 Logical Separator

Use `/` as the logical separator in service and IPC contracts.

The Windows adapter converts to native path syntax.

---

## 24.2 Root Path

An empty logical path may represent the workspace root only in APIs that explicitly permit the root.

`/` should not be used ambiguously as a Windows or logical root.

---

# 25. Anchored Absolute Path

An anchored path is produced by resolving untrusted or logical text against one explicit immutable base.

It is a lexical result.

It is not proof of containment.

---

# 26. Resolved Existing Path

A resolved existing path contains:

- anchored display path;
- final handle-derived path;
- volume identity;
- file identity;
- object type;
- attributes;
- reparse information;
- case information;
- size;
- revision metadata;
- and verification time.

---

# 27. Verified Target Path

A target path for a not-yet-existing leaf contains:

- verified existing parent handle;
- parent identity;
- validated leaf name;
- expected non-existence or expected identity;
- case-collision check;
- stream prohibition;
- and mutation token.

---

# 28. File Object Identity

On Windows, an existing file identity should prefer:

- volume serial number;
- and 128-bit file ID where supported.

Fallback identity may use:

- volume serial number;
- legacy file index;
- and verified final path.

The capability level must be recorded.

---

## 28.1 Identity Limitations

File identity may:

- change on some filesystems;
- become invalid after deletion;
- identify a replacement file rather than the old file after replacement;
- and be unavailable or unreliable on some remote filesystems.

Opure must not overstate the guarantee.

---

# 29. Volume Identity

Volume identity may contain:

- volume serial number;
- volume GUID path where available;
- filesystem name;
- root;
- drive type;
- maximum component length;
- filesystem flags;
- and capability assessment.

---

# 30. Content Revision

A content revision should use:

- cryptographic content hash;
- byte length;
- encoding state where relevant;
- line-ending state where relevant;
- and file identity.

Modification time alone is insufficient.

---

# 31. Path Parsing

Path parsing occurs before filesystem access.

The parser must classify the path as:

- logical relative;
- ordinary drive-rooted;
- UNC;
- drive relative;
- root relative;
- extended Win32;
- device path;
- NT object path;
- named pipe;
- alternate stream;
- URI;
- or invalid.

---

## 31.1 Ordinary Workspace Input

Ordinary workspace APIs accept only:

- logical workspace-relative paths;
- or a developer-selected root during registration.

All other path forms require a specialised operation.

---

# 32. Deterministic Full-Path Resolution

When a trusted internal operation must convert a relative native path, it should use:

```text
Path.GetFullPath(path, explicitBasePath)
```

It must not rely on the process current directory.

---

## 32.1 Process Current Directory

The Runtime should not change the process current directory for workspace operations.

External commands receive an explicit working directory.

---

## 32.2 Drive Current Directory

Drive-relative forms such as:

```text
C:folder\file.txt
```

are rejected.

Their meaning depends on drive-specific current-directory state.

---

## 32.3 Root-Relative Paths

Forms such as:

```text
\folder\file.txt
```

are rejected in ordinary workspace APIs.

They depend on the current drive.

---

# 33. Namespace Policy

The ordinary Workspace boundary rejects input beginning with or resolving to:

- `\\?\`
- `\\.\`
- `\??\`
- `\Device\`
- `GLOBALROOT`
- named-pipe namespaces;
- mailslot namespaces;
- volume device paths;
- and arbitrary NT object-manager paths.

---

## 33.1 Internal Extended Paths

The Windows adapter may use extended paths internally to support long paths and exact filesystem access.

These forms must not be accepted as ordinary user or plugin input.

---

## 33.2 No Namespace Downgrade

The adapter must not remove a device or extended prefix and then assume the result is an ordinary safe path without revalidation.

---

# 34. Long Paths

All shipped Windows executables should declare:

```xml
<longPathAware>true</longPathAware>
```

through the supported application manifest namespace.

---

## 34.1 .NET Path APIs

Use current .NET Unicode path APIs.

Avoid legacy APIs with fixed `MAX_PATH` buffers.

---

## 34.2 External Tool Compatibility

Before invoking an external tool, Opure should assess:

- tool version;
- declared long-path support;
- path length;
- and working-directory length.

---

## 34.3 Long-Path Warning

If a tool may fail, show:

- affected path;
- current length class;
- tool;
- and alternative.

Do not truncate or alias silently.

---

## 34.4 8.3 Names

Opure must not depend on 8.3 short-name generation.

It may be disabled on a volume.

---

# 35. Name Validation

For create and rename operations, validate each leaf component.

Reject:

- embedded NUL;
- path separators;
- invalid namespace syntax;
- alternate-stream colon;
- reserved device name;
- final space;
- final period;
- and empty name.

---

## 35.1 Reserved DOS Device Names

Reject device names case-insensitively, including extension forms such as:

```text
NUL
NUL.txt
CON
PRN
AUX
COM1
LPT1
```

The implementation should include the full documented device-name family, including superscript digit variants recognised by Windows.

---

## 35.2 Existing Unusual Names

An existing repository may contain a name created through specialised APIs that the Windows shell handles poorly.

Opure may:

- read it through a specialised safe path;
- show a compatibility warning;
- refuse rename or creation of an equivalent name;
- and require explicit recovery tooling.

---

# 36. Unicode

Path values should remain Unicode.

The platform adapter should use Unicode Windows APIs.

---

## 36.1 Preserve Original Name

Do not normalise the actual filesystem name before opening it.

Preserve the exact name returned by the filesystem.

---

## 36.2 Display Normalisation

The UI may apply safe visual normalisation for display only if it also preserves access to the exact stored name.

---

## 36.3 Confusable Names

Opure may warn about visually confusable filenames.

It must not rename them automatically.

---

## 36.4 Normalisation Collisions

If two names normalise to the same display form but remain distinct on disk, the UI must present an unambiguous escaped representation.

---

# 37. Case Sensitivity

Windows is commonly case insensitive but can enable case sensitivity per directory.

Opure must not assume one comparison rule for the entire workspace.

---

## 37.1 Registration Check

Workspace registration should inspect:

- volume case capability;
- root-directory case-sensitive state;
- and known nested case-sensitive directories during enumeration.

---

## 37.2 Per-Directory Semantics

Path resolution should respect the case rules of each directory component.

---

## 37.3 Exact Case

Store the exact filesystem-returned case for existing paths.

---

## 37.4 Case-Insensitive Comparison

Where a directory is case insensitive, use ordinal case-insensitive comparison.

Culture-sensitive comparison is prohibited.

---

## 37.5 Case-Sensitive Comparison

Where a directory is case sensitive, use ordinal comparison.

---

## 37.6 Case Collision

Before create or rename, enumerate the parent and detect:

- exact collision;
- case-insensitive collision;
- Unicode-display collision;
- and Git portability risk.

---

## 37.7 Git Case Behaviour

Repository Service should compare filesystem case behaviour with Git configuration.

Conflicting assumptions should become a project warning.

---

# 38. Workspace Registration

Registering a workspace should:

1. accept developer-selected root;
2. classify the path;
3. deterministically fully qualify it;
4. open the root directory;
5. inspect reparse state without following unexpectedly;
6. resolve the intended final target;
7. obtain volume and file identity;
8. inspect filesystem capabilities;
9. inspect case sensitivity;
10. classify drive type;
11. detect cloud or virtualised state;
12. assess write safety;
13. create workspace identity;
14. and present any limitations.

---

## 38.1 Root Reparse Point

If the selected root itself is a symbolic link or junction, Opure must show:

- selected path;
- final target;
- volume;
- and policy.

The developer may approve registration of the final target as the root.

The alias alone is not the workspace security boundary.

---

## 38.2 Root Identity

The registered security root is based on the opened final directory identity.

---

## 38.3 Root Movement

If the root path later resolves to a different identity, the workspace enters:

```text
Root Changed
```

Protected mutation is denied until re-registration or recovery.

---

# 39. Workspace Capability States

A workspace may be:

- Fully Supported;
- Supported with Warnings;
- Read-Only Supported;
- Protected Writes Disabled;
- External Hydration Required;
- Unsupported Filesystem;
- Root Changed;
- Recovery Required;
- or Unavailable.

---

# 40. Filesystem Capability Discovery

The Windows adapter should discover:

- filesystem name;
- maximum component length;
- case support;
- case preservation;
- persistent ACL support;
- reparse-point support;
- named-stream support;
- sparse-file support;
- remote-storage support;
- USN-journal support;
- integrity-stream support;
- read-only state;
- drive type;
- and volume identity.

---

## 40.1 Capability, Not Filesystem Name Alone

Do not infer every behaviour only from the string `NTFS` or `ReFS`.

Use reported capabilities and tested behaviour.

---

# 41. Drive Classes

Classify roots as:

- Fixed Local;
- Removable;
- Network;
- RAM;
- Optical;
- Virtual;
- Unknown.

---

## 41.1 Fixed Local

Preferred for full protected writes.

---

## 41.2 Removable

May be supported with warnings for:

- disconnection;
- write caching;
- filesystem limits;
- and identity change.

---

## 41.3 Network

Network workspaces require a separate capability assessment.

Protected multi-file writes may be disabled initially.

---

## 41.4 Unknown

Fail closed for protected mutation.

Read-only inspection may remain possible.

---

# 42. Workspace Containment

Containment is a security decision.

It must not rely only on:

```text
candidate.StartsWith(root)
```

---

## 42.1 Lexical Containment

First require the logical path to contain no parent traversal.

Then anchor it under the registered root.

---

## 42.2 Component Verification

For every existing component:

1. open or inspect the component;
2. identify reparse state;
3. resolve according to policy;
4. obtain final identity;
5. verify that traversal remains inside an allowed root;
6. and continue.

---

## 42.3 Handle Containment

For protected operations, compare the resolved object and parent chain to registered allowed root identities where practical.

---

## 42.4 Relative-Path Display

A path can be displayed as workspace relative only after containment is verified.

---

# 43. Reparse Points

A reparse point is not automatically a symbolic link.

It may represent:

- symbolic link;
- junction;
- mounted folder;
- cloud placeholder;
- projected filesystem;
- storage filter;
- application execution link;
- deduplication;
- or another filesystem-filter feature.

---

## 43.1 Reparse Classification

The Windows adapter should obtain:

- reparse attribute;
- reparse tag;
- object type;
- and final target behaviour.

---

## 43.2 Unknown Reparse Tag

An unknown reparse tag is denied for protected mutation by default.

Read access may be allowed only through an explicit restricted policy after behaviour is understood.

---

## 43.3 No Blanket Follow

Ordinary recursive enumeration must not blindly follow every reparse point.

---

# 44. Symbolic Links

Symbolic links may be legitimate repository content.

The policy should distinguish:

- link object;
- link target;
- internal target;
- external target;
- broken link;
- relative link;
- absolute link;
- and remote target.

---

## 44.1 Internal Symbolic Link

An internal link resolves to an object inside an allowed workspace root.

Read traversal may be allowed.

Protected mutation through the link requires explicit operation semantics and revalidation.

---

## 44.2 External Symbolic Link

An external link resolves outside allowed workspace roots.

Default behaviour:

- display the link;
- do not index target contents;
- do not send target contents to AI;
- do not patch target;
- and require explicit additional-root approval for access.

---

## 44.3 Broken Symbolic Link

Preserve and display the link.

Do not create its target implicitly.

---

## 44.4 Link Mutation

A patch must state whether it intends to:

- replace link object;
- change link target text;
- or modify the target file.

These are different operations.

---

## 44.5 Remote Symbolic Link

A link to a UNC path is external network access.

It is denied by default and subject to Network Gateway and project policy.

---

# 45. Junctions

Junctions are directory reparse points and may target another local volume.

---

## 45.1 Internal Junction

An internal same-root junction may be traversed for reads under policy.

---

## 45.2 Cross-Volume Junction

A junction that reaches another volume changes:

- volume identity;
- replacement guarantees;
- filesystem capabilities;
- and recovery assumptions.

It is treated as an additional root, not ordinary containment.

---

## 45.3 Junction Mutation

Deleting a junction should delete the junction object, not recursively delete its target.

The operation must use no-follow semantics.

---

# 46. Mounted Folders

A mounted folder can introduce another volume beneath an ordinary path.

Workspace enumeration must detect this.

---

## 46.1 Mounted Volume Policy

A mounted volume is a separate root.

Protected mutation requires:

- explicit registration;
- separate capability assessment;
- and separate journal assumptions.

---

# 47. Cloud Placeholders

Cloud-managed files can appear as reparse-point-backed placeholders.

---

## 47.1 Detection

Use file attributes and reparse tags to identify cloud-placeholder state.

---

## 47.2 No Silent Hydration

Opening a placeholder may trigger download.

Opure should not hydrate large or sensitive project trees silently.

---

## 47.3 Hydration Plan

Before hydration, show:

- file or subtree;
- provider class;
- estimated size where available;
- network requirement;
- and purpose.

---

## 47.4 Local-Only Project Policy

Hydration from a cloud synchronisation provider is still network activity.

Local-only project policy should block it unless the developer approves the provider behaviour.

---

## 47.5 Patch Application

Protected mutation of cloud placeholders requires a tested policy for:

- hydration;
- sync conflicts;
- offline state;
- and post-write synchronisation.

Until proven, such workspaces may be Supported with Warnings or Protected Writes Disabled.

---

# 48. Projected and Virtual Filesystems

Tags such as projected filesystem or container isolation may present virtualised content.

Default protected-write behaviour is deny until the adapter has explicit support.

---

# 49. Reparse Walk Algorithm

A conceptual path walk is:

```text
start with registered root handle
for each component:
    validate component text
    inspect child without following reparse processing
    if child does not exist:
        only final leaf may be absent for create
        validate parent handle and stop
    obtain attributes and reparse tag
    apply tag policy
    if traversal is allowed:
        open resolved target
        obtain final path, volume and file identity
        verify allowed-root membership
        continue from verified handle
```

The exact Windows implementation requires prototype review.

---

# 50. No-Follow Handles

The Windows adapter should use `CreateFileW` with:

- `FILE_FLAG_OPEN_REPARSE_POINT` when inspecting link objects;
- `FILE_FLAG_BACKUP_SEMANTICS` when opening directories where required;
- explicit sharing flags;
- and `SafeFileHandle`.

---

## 50.1 Handle Rights

Request the minimum rights needed for:

- attributes;
- identity;
- read;
- write;
- delete;
- or rename.

---

## 50.2 Handle Lifetime

Keep handles only as long as required.

Long-lived directory handles may interfere with external tools or updates.

---

# 51. Final Path by Handle

For an opened existing object, the Windows adapter may retrieve the final path using the handle.

The final path is diagnostic and useful for verification.

It is still not the sole identity.

---

## 51.1 Final Path Namespace

Convert final device or volume paths into a stable internal representation.

Do not expose raw NT device paths to ordinary UI.

---

# 52. Existing Object Identity

For protected reads:

1. resolve path;
2. open object;
3. retrieve identity;
4. read through the verified handle where possible;
5. hash content;
6. and return the identity and revision.

---

# 53. Non-Existing Target

For create:

1. resolve and verify every existing parent;
2. keep the final parent identity;
3. validate leaf name;
4. check exact and case collisions;
5. check stream syntax;
6. record expected absence;
7. stage beside the target;
8. revalidate parent;
9. create with no overwrite;
10. and verify the created identity.

---

# 54. Time-of-Check to Time-of-Use

No path validation can prevent all same-user races without using handles and carefully selected native operations.

---

## 54.1 Review-Time State

A patch review should record:

- logical path;
- resolved identity;
- content hash;
- size;
- encoding;
- line ending;
- attributes;
- reparse state;
- hard-link count;
- and timestamp metadata.

---

## 54.2 Apply-Time Revalidation

Immediately before mutation, revalidate:

- workspace root identity;
- parent identity;
- target identity;
- content hash;
- reparse state;
- case;
- and policy.

---

## 54.3 Changed Target

If any material field changes, the patch becomes stale.

No automatic overwrite.

---

## 54.4 Handle-Based Mutation

Where possible, use handles or operations bound to the verified parent and object.

A Windows-specific rename-by-handle implementation may be considered after prototype evidence.

---

# 55. Hard Links

A hard-linked file has more than one directory entry pointing to one file object.

Changing the content through one link affects all links.

Replacing one directory entry may break that shared relationship.

---

## 55.1 Detection

For protected mutation, inspect the file's link count.

A link count greater than one is a special condition.

---

## 55.2 Default Patch Policy

Default behaviour for a hard-linked file:

- show warning;
- identify that multiple names reference the object;
- deny silent replacement;
- and require a specific strategy.

---

## 55.3 Strategies

Possible strategies include:

- deny mutation;
- modify in place with reduced atomicity and explicit risk;
- locate and update all intended links;
- or replace only one name and acknowledge link separation.

The first implementation should prefer denial or explicit high-risk approval.

---

## 55.4 Hard-Link Enumeration

Enumerating every hard-link path may require Windows-specific APIs or tooling.

This capability is optional for the first vertical slice.

The known link count is still sufficient to avoid silent assumptions.

---

# 56. Alternate Data Streams

NTFS named streams use colon syntax such as:

```text
file.txt:stream
```

---

## 56.1 Ordinary Workspace Policy

Ordinary logical workspace paths must not address a named stream.

A colon is prohibited in ordinary path components.

---

## 56.2 Stream Detection

For protected mutation of existing files on a filesystem supporting named streams, Opure should detect whether named streams exist.

---

## 56.3 Patch Policy

A normal patch modifies the unnamed primary data stream only.

Named streams are preserved only when the selected replacement operation is proven to preserve them.

Windows `ReplaceFileW` is expected to preserve named streams from the replaced file that do not already exist in the replacement, but this behaviour must be covered by acceptance tests.

---

## 56.4 Security Streams

A secret or executable payload may be hidden in a named stream.

Secret and security scanning should report unexpected streams.

---

## 56.5 Stream Creation

Creating or modifying named streams is not supported in version 1.0 ordinary workflows.

---

# 57. File Types

The Workspace Service should classify:

- regular file;
- directory;
- symbolic link;
- junction;
- mounted folder;
- placeholder;
- device-like object;
- socket-like object where applicable;
- and unknown reparse object.

---

# 58. File Attributes

Relevant Windows attributes include:

- ReadOnly;
- Hidden;
- System;
- Directory;
- Archive;
- ReparsePoint;
- SparseFile;
- Offline;
- NotContentIndexed;
- Encrypted;
- IntegrityStream;
- and NoScrubData.

---

## 58.1 Attribute Visibility

Patch review should show material attributes that affect mutation.

---

## 58.2 Read-Only

Read-only is not silently cleared.

A patch may request an explicit attribute change.

---

## 58.3 Hidden and System

Protected system or hidden files require higher-risk review.

---

## 58.4 Offline

Offline or placeholder files may require hydration.

---

## 58.5 Encrypted

Encrypted files require a tested replacement path that preserves intended encryption state.

---

## 58.6 Compressed

Compression state should be preserved where the replacement API supports it.

---

# 59. Access Control Lists

File replacement must not silently broaden permissions.

---

## 59.1 Existing File

Use a replacement operation that preserves destination ACLs where supported.

---

## 59.2 New File

A new file inherits parent-directory policy unless explicit approved metadata is required.

---

## 59.3 ACL Merge Failure

Do not ignore ACL merge errors by default.

A failure should prevent success and enter exact recovery state.

---

# 60. Staging

Protected content should be staged before activation.

---

## 60.1 Staging Location

For file replacement, prefer a hidden Opure-controlled temporary name in the destination directory.

Benefits:

- same volume;
- same parent ACL;
- rename compatibility;
- and reduced cross-volume risk.

---

## 60.2 Name

Use a random non-sensitive staging name.

Do not include:

- secret;
- prompt;
- developer name;
- or full original content.

---

## 60.3 Creation Mode

Create staging files with create-new semantics.

Do not overwrite an existing staging name.

---

## 60.4 Staging Permissions

Use restrictive access and expected inherited ACLs.

---

## 60.5 Write and Flush

The staging flow should:

1. stream new content;
2. verify byte count;
3. compute hash;
4. flush managed buffers;
5. flush the file handle according to durability policy;
6. close or retain handle as required;
7. reopen and verify where justified;
8. and record staged state.

---

# 61. Existing File Replacement

For an ordinary existing file on a supported local volume:

1. journal original identity and revision;
2. create same-directory staging file;
3. validate staged content;
4. revalidate destination;
5. call `ReplaceFileW` or verified `File.Replace`;
6. preserve or create backup according to patch journal;
7. verify resulting identity and content;
8. verify metadata;
9. record applied state;
10. and retain recovery evidence.

---

## 61.1 Same-Volume Requirement

Destination, replacement and backup must reside on the same volume.

Cross-volume replacement is not treated as atomic.

---

## 61.2 Replacement Identity

The resulting file takes the replacement file's identity.

Consumers tracking the previous file ID must reconcile.

---

## 61.3 Metadata Preservation

Acceptance tests must cover preservation of:

- ACL;
- creation time;
- encryption;
- compression;
- object identifier where supported;
- short name where present;
- and named streams.

---

## 61.4 Unsupported Merge

Do not use flags that ignore merge or ACL errors in ordinary protected mutation.

---

# 62. New File Creation

For a new file:

1. verify parent;
2. journal expected absence;
3. stage in parent;
4. revalidate parent and absence;
5. rename or move within the same directory;
6. fail if target now exists;
7. verify created identity;
8. and record outcome.

---

# 63. File Deletion

Deletion should be a patch operation.

---

## 63.1 Existing Identity

Revalidate the target identity and hash before deletion.

---

## 63.2 Reparse Object

Deleting a symbolic link or junction should delete the link object, not target contents.

---

## 63.3 Recovery

Where practical, move the file to an Opure recovery location on the same volume before final deletion.

If a recoverable move is impossible, risk must be visible.

---

## 63.4 Hard Links

Deleting one hard-link name does not delete the underlying content while other links remain.

Show the known link count.

---

# 64. Directory Creation

Directory creation should:

- verify parent;
- validate name;
- check case collisions;
- use create-new semantics;
- and verify resulting identity.

---

# 65. Directory Deletion

Directory deletion is high risk.

---

## 65.1 Recursive Delete

Recursive deletion must never blindly follow reparse points.

---

## 65.2 Enumeration

Build an explicit deletion plan.

Each item identifies:

- ordinary object;
- link object;
- external target exclusion;
- and current identity.

---

## 65.3 Non-Empty Directory

A directory must match the reviewed plan at execution.

Unexpected new children make the plan stale.

---

# 66. Rename

Rename should:

- verify source identity;
- verify destination parent;
- validate destination name;
- check collisions;
- detect case-only rename;
- remain on the same volume for atomic rename assumptions;
- and verify result.

---

## 66.1 Case-Only Rename

On a case-insensitive directory, a case-only rename may require a temporary intermediate name.

It must be journalled and recoverable.

---

## 66.2 Directory Rename

Directory rename must account for:

- active child handles;
- current working directories of external processes;
- reparse points;
- and project watchers.

---

# 67. Cross-Volume Move

A cross-volume move is a copy-and-delete style operation.

It is not treated as an atomic rename.

---

## 67.1 Protected Policy

Cross-volume project moves require:

- explicit plan;
- copy verification;
- content hashes;
- metadata policy;
- destination capability check;
- and source deletion only after verification.

---

# 68. Multi-File Patch Transactions

Windows does not provide a general supported atomic transaction for arbitrary multi-file project changes in this architecture.

---

## 68.1 No False Atomicity

Opure must not describe a multi-file patch as filesystem atomic.

---

## 68.2 Transaction Model

Use:

- durable patch journal;
- per-file staged content;
- reviewed order;
- backup or reverse data;
- apply records;
- verification;
- compensation;
- and Recovery Mode.

---

## 68.3 Apply Order

Apply order should minimise risk.

Possible order:

1. create directories;
2. create new files;
3. replace existing files;
4. rename;
5. delete files;
6. delete directories.

The exact order depends on dependencies.

---

## 68.4 Interruption

After interruption, the journal identifies:

- not started;
- staged;
- applied;
- verified;
- reversed;
- and ambiguous items.

---

# 69. Revision Validation

Every protected write should compare the reviewed revision with the current revision.

---

## 69.1 Text File Revision

May include:

- file identity;
- SHA-256;
- byte length;
- encoding;
- byte-order mark;
- line endings;
- and final newline state.

---

## 69.2 Binary Revision

Use:

- file identity;
- SHA-256;
- and byte length.

---

## 69.3 Metadata Revision

Material metadata such as reparse state and hard-link count should be included.

---

# 70. Concurrent Modification

External editors, Git operations, builds and synchronisation tools may change files.

---

## 70.1 Stale Patch

Any unexpected material change marks the patch stale.

---

## 70.2 Automatic Rebase

Automatic patch rebase is a separate deterministic operation.

It must produce a new reviewable patch version.

---

## 70.3 No Last-Writer-Wins

The Patch Service must not silently overwrite newer developer changes.

---

# 71. File Sharing Modes

Windows permits files to be opened with different sharing flags.

The adapter should use explicit sharing modes.

---

## 71.1 Read

Normal reads may allow:

- shared read;
- shared write where safe;
- and shared delete

to coexist with editors.

The exact profile requires testing.

---

## 71.2 Protected Mutation

Replacement and deletion must handle sharing violations honestly.

---

## 71.3 Locked File

A locked target should produce:

- owning operation where discoverable;
- retry guidance;
- cancellation option;
- and no false success.

---

# 72. External Processes

Builds, tests, Git and tools receive:

- verified working directory;
- explicit input paths;
- and capability scope.

---

## 72.1 Tool Path Interpretation

The tool may interpret:

- symlinks;
- wildcards;
- response files;
- environment;
- and device names

differently from Opure.

Command plans must avoid passing untrusted paths into shell parsing.

---

## 72.2 Shell

Prefer direct executable invocation with argument arrays.

Avoid `cmd.exe /c` or PowerShell command strings unless the command requires a shell and risk is reviewed.

---

## 72.3 Wildcards

Do not let a shell expand untrusted wildcard text.

Enumerate through the Workspace Service where practical.

---

# 73. Plugin Access

Plugins should receive capability methods such as:

- read approved file;
- enumerate approved subtree;
- propose patch;
- and request content reference.

They do not receive unrestricted native paths by default.

---

# 74. MCP Access

MCP filesystem capabilities must be mediated.

An MCP server cannot treat an Opure workspace path as permission to access the local filesystem.

---

# 75. AI Context Access

AI context selection uses verified workspace content.

External link targets and unknown reparse content are excluded by default.

---

# 76. Secret-Sensitive Paths

The Workspace Service should classify likely sensitive paths such as:

- `.env`;
- private keys;
- credentials;
- package authentication;
- cloud configuration;
- and secret-store files.

Path safety and secret scanning are separate controls.

---

# 77. Git Metadata

The `.git` directory and equivalent repository control files are protected.

---

## 77.1 Direct Patch

A normal AI patch must not modify `.git` internals.

Repository Service owns Git operations.

---

## 77.2 Worktrees and Submodules

Git worktrees and submodules may introduce paths outside the apparent repository root.

They require explicit repository-aware root registration.

---

# 78. Submodules

A submodule working tree is a separate repository root.

It may be registered as:

- child workspace;
- approved additional root;
- or read-only external dependency.

---

# 79. Git Worktrees

A worktree may store Git administrative data outside the visible root.

Repository Service may access that data through a specific capability.

General Patch Service mutation remains limited to the working tree.

---

# 80. Build Outputs

Build output directories may contain:

- symlinks;
- generated files;
- executable files;
- and enormous trees.

They should be classified separately from source.

---

## 80.1 Mutation

Generated-output cleanup is a Build Manager operation, not an ordinary source patch.

---

# 81. Package Caches

Package caches outside the workspace are not project roots.

They require Dependency Manager access.

---

# 82. Internal Opure State

Opure databases, Vault, logs and content store remain outside project roots.

The Workspace Service must never mistake internal state for project content.

---

# 83. Enumeration

Enumeration should use explicit `EnumerationOptions`.

---

## 83.1 Options

Set deliberately:

- recurse;
- maximum depth;
- inaccessible behaviour;
- casing;
- attributes to skip;
- and special-directory behaviour.

---

## 83.2 Reparse Recursion

Do not rely only on default enumeration behaviour.

Explicitly inspect and apply reparse policy.

---

## 83.3 Inaccessible Items

Do not silently ignore access-denied items in security-sensitive enumeration.

Return partial state with exact skipped paths.

---

## 83.4 Bounds

Enumeration should be bounded by:

- item count;
- depth;
- time;
- cancellation;
- and memory.

---

## 83.5 Ordering

Do not rely on filesystem enumeration order.

Sort where deterministic order is required.

---

# 84. File Watching

`FileSystemWatcher` is an optimisation signal.

It is not the authoritative filesystem history.

---

## 84.1 Event Coalescing

Coalesce duplicate and burst events.

---

## 84.2 Overflow

On watcher error or internal-buffer overflow:

- mark view stale;
- stop claiming exact incremental state;
- perform bounded rescan;
- rebuild revision index;
- and resume watching.

---

## 84.3 Rename Pairing

Rename events may be incomplete or reordered.

Verify filesystem state.

---

## 84.4 Watcher Scope

Avoid one giant recursive watcher where multiple scoped watchers or index scans are safer.

---

## 84.5 Network Watchers

Remote filesystem watcher behaviour may be weaker.

Capability must be assessed.

---

# 85. USN Journal

A future Windows indexer may use the NTFS USN Journal for efficient change detection.

It is not required for the first implementation.

It requires a separate ADR.

---

# 86. Cloud Synchronisation Conflicts

A cloud synchronisation engine may change files after Opure writes them.

The Workspace Service should detect revision changes and report conflicts.

It must not claim that a locally successful write is globally synchronised.

---

# 87. Network Filesystems

Version 1.0 should not promise full protected-write guarantees on arbitrary network filesystems.

---

## 87.1 Read-Only Use

Read-only inspection may be supported with warnings.

---

## 87.2 Protected Writes

Protected writes require tests for:

- locking;
- identity;
- rename;
- ACL;
- disconnect;
- and recovery.

Until then, disable or classify as experimental.

---

# 88. Removable Volumes

Removable volumes can disappear between validation and use.

---

## 88.1 Volume Removal

A removed volume causes:

- operation cancellation;
- exact uncertain state;
- and recovery on reconnection.

---

## 88.2 Volume Serial Reuse

Volume serial number alone is not sufficient identity.

Use the registered root and file identity together.

---

# 89. ReFS

ReFS may support many required capabilities but differs from NTFS.

Support should be capability driven and tested.

---

# 90. FAT and exFAT

FAT-family filesystems may lack:

- persistent ACLs;
- reparse points;
- stable file identities;
- and other expected semantics.

Protected writes may be restricted.

---

# 91. WSL Interoperability

Repositories created or modified through WSL can contain:

- case-sensitive names;
- symbolic links;
- permission expectations;
- and names problematic for Windows tools.

---

## 91.1 NTFS WSL Directory

Detect per-directory case sensitivity.

---

## 91.2 Linux Filesystem Through `\\wsl$`

A WSL filesystem path is a network-style provider boundary.

Full support is deferred.

Read-only or experimental use may be offered after testing.

---

## 91.3 Tool Split

Running a Windows Runtime against files simultaneously modified by Linux tools increases race and compatibility risk.

Show the current capability state.

---

# 92. Paths in Contracts

Public and IPC contracts should carry:

- workspace identifier;
- logical workspace path;
- optional expected revision;
- and operation intent.

They should not carry arbitrary native absolute paths for ordinary project operations.

---

# 93. Paths in Persistence

Persist:

- workspace identity;
- registered display root;
- final root identity;
- logical relative paths;
- and file identities where useful.

Do not persist only one absolute path and assume it remains valid.

---

# 94. Paths in Logs

Logs should prefer:

- workspace-relative path;
- safe file identifier;
- or redacted path class.

Absolute paths are Sensitive.

---

# 95. Paths in Trust Centre

Trust records should show enough path information for developer understanding.

They should avoid leaking unrelated user-profile paths.

---

# 96. Paths in Diagnostic Export

Diagnostic export should redact:

- user-profile root;
- organisation names;
- unrelated absolute paths;
- and external target details

according to policy.

---

# 97. Paths in UI

The UI should display:

- logical workspace-relative path prominently;
- root alias or project name;
- reparse marker;
- external-target marker;
- case-sensitive marker;
- cloud-placeholder marker;
- and warning state.

---

# 98. Escaping Display

Control characters and bidirectional text controls should be rendered safely.

The UI may show an escaped representation.

---

# 99. Copy Path

Copying a path should offer:

- workspace-relative;
- Windows absolute;
- and external-editor format.

Copying raw device paths is a diagnostic-only action.

---

# 100. Error Model

Stable error categories include:

- Invalid Path;
- Unsupported Path Form;
- Drive-Relative Path;
- Root-Relative Path;
- Device Path Denied;
- Reserved Name;
- Alternate Stream Denied;
- Path Too Long for Tool;
- Workspace Escape;
- Root Changed;
- Reparse Point Denied;
- Unknown Reparse Tag;
- External Link Target;
- Cloud Hydration Required;
- Case Collision;
- Hard Link Detected;
- Identity Changed;
- Revision Changed;
- Sharing Violation;
- Access Denied;
- Read-Only Volume;
- Cross-Volume Operation;
- Filesystem Unsupported;
- Watcher Overflow;
- Disk Full;
- Partial Apply;
- and Recovery Required.

---

# 101. Error Detail

An error may include:

- logical path;
- safe root identity;
- operation;
- expected state;
- current state;
- reparse class;
- volume class;
- and remediation.

It must not include secret file contents.

---

# 102. Observability

Filesystem observability should include:

- workspace count;
- capability state;
- path-validation failures;
- reparse denials;
- external links;
- cloud hydration requests;
- case collisions;
- hard-link warnings;
- watcher overflow;
- stale patches;
- replacement latency;
- recovery count;
- and filesystem error codes.

---

# 103. Metrics

Metrics should remain low cardinality.

Do not use full paths as metric labels.

---

# 104. Trust Centre Records

Significant records include:

- external root approved;
- cloud hydration approved;
- protected patch applied;
- patch denied due to path escape;
- hard-link mutation approved;
- cross-volume operation approved;
- directory deletion;
- and recovery action.

---

# 105. Security Impact

## 105.1 Trust Boundaries

Filesystem boundaries include:

- Desktop to Runtime;
- model output to Patch Service;
- plugin to Workspace Service;
- MCP server to Workspace Service;
- Runtime to Windows filesystem;
- Runtime to external tool;
- workspace root to external target;
- and local volume to network or cloud provider.

---

## 105.2 Threats

Relevant threats include:

- `..` traversal;
- drive-relative ambiguity;
- device-path access;
- DOS device names;
- alternate-stream access;
- symlink escape;
- junction escape;
- mounted-volume escape;
- unknown reparse filters;
- root replacement;
- target replacement;
- hard-link alias;
- case collision;
- Unicode confusion;
- watcher-event loss;
- shell injection;
- wildcard injection;
- cross-volume false atomicity;
- and cloud-sync race.

---

## 105.3 Mitigations

- explicit path types;
- deterministic base;
- namespace rejection;
- handle verification;
- reparse inspection;
- final-path and identity checks;
- revision hashes;
- same-volume staging;
- direct process invocation;
- journalling;
- and adversarial tests.

---

# 106. TOCTOU Limitation

A malicious same-user process can still interfere with files between operations.

Handle-based verification and same-directory replacement reduce risk.

They do not create a kernel-enforced sandbox.

Opure must state this honestly.

---

# 107. Reliability and Recovery

## 107.1 Failure Modes

Potential failures include:

- process crash;
- power loss;
- locked file;
- disk full;
- removed drive;
- reparse target change;
- cloud conflict;
- permission change;
- watcher overflow;
- and external editor race.

---

## 107.2 Recovery Principle

Recovery uses:

- patch journal;
- staged files;
- backups;
- identity records;
- content hashes;
- and exact operation states.

---

## 107.3 Ambiguous State

If Opure cannot prove whether a file changed, it marks the item:

```text
Recovery Required
```

It does not guess.

---

# 108. Performance Impact

Handle verification adds filesystem operations.

The overhead is expected to be small compared with:

- hashing;
- diffing;
- builds;
- tests;
- and model inference.

It requires measurement.

---

## 108.1 Fast Reads

Low-risk read-only navigation may use cached verified metadata.

Before sensitive use or mutation, revalidate.

---

## 108.2 Caching

Cache:

- volume capabilities;
- root identity;
- directory case state;
- and known reparse classification

with invalidation.

Do not cache final mutation authority indefinitely.

---

# 109. Required Benchmarks

Measure:

- lexical normalisation;
- 100,000-path validation;
- component handle walk;
- reparse classification;
- file identity query;
- hashing;
- large-tree enumeration;
- watcher reconciliation;
- staging;
- replacement;
- and recovery.

---

# 110. Reference Hardware

Tests should use:

- Windows 11;
- AMD Ryzen 9 5950X;
- 32 GB RAM;
- NVIDIA RTX 5070 Ti with 16 GB VRAM;
- and local SSD storage.

---

# 111. Testing Strategy

ADR-0008 applies.

Filesystem tests require actual Windows filesystem fixtures.

---

# 112. Unit Tests

Test:

- path classification;
- logical-path parser;
- reserved-name detection;
- stream syntax;
- comparison selection;
- error mapping;
- and operation-state machines.

---

# 113. Integration Tests

Use real temporary directories and volumes where practical.

Test:

- long paths;
- Unicode;
- ACLs;
- replacement;
- streams;
- hard links;
- symlinks;
- junctions;
- and case-sensitive directories.

---

# 114. Multi-User Tests

Test another ordinary Windows user attempting to access or redirect Opure-managed staging and journal locations.

---

# 115. Reparse Tests

Create fixtures for:

- symbolic link to internal file;
- symbolic link to external file;
- symbolic link to UNC;
- relative symbolic link;
- broken symbolic link;
- junction to internal directory;
- junction to another volume;
- mounted folder;
- cloud placeholder simulation where available;
- unknown test reparse point where practical;
- and reparse-chain depth.

---

# 116. TOCTOU Tests

Use an adversarial helper process that repeatedly:

- replaces a directory with a junction;
- swaps a file;
- changes a symlink target;
- renames a parent;
- and creates the target after absence validation.

Protected operations must deny or affect only the intended verified object.

---

# 117. Hard-Link Tests

Test:

- two paths to one file;
- content mutation;
- replacement;
- deletion of one link;
- link-count warning;
- and approval behaviour.

---

# 118. Alternate-Stream Tests

Test:

- stream syntax rejection;
- named-stream detection;
- `ReplaceFileW` stream preservation;
- secret scanner;
- and export behaviour.

---

# 119. Case Tests

Test:

- ordinary case-insensitive directory;
- case-sensitive root;
- case-sensitive nested directory;
- case-only rename;
- two files differing only by case;
- Git configuration mismatch;
- and display ambiguity.

---

# 120. Long-Path Tests

Test paths:

- below legacy `MAX_PATH`;
- above legacy `MAX_PATH`;
- near filesystem component limit;
- above external tool limit;
- and using Unicode.

---

# 121. Reserved-Name Tests

Test:

- `NUL`;
- `NUL.txt`;
- `CON`;
- `COM1`;
- `LPT9`;
- superscript variants;
- trailing dot;
- trailing space;
- and embedded colon.

---

# 122. Namespace Tests

Test rejection of:

- extended path input;
- device namespace;
- named pipe;
- NT object path;
- volume GUID input;
- drive-relative;
- and root-relative path.

Internal adapter tests may use approved extended forms.

---

# 123. Replacement Tests

Test:

- ordinary replace;
- ACL preservation;
- named-stream preservation;
- encryption and compression where available;
- replacement failure codes;
- sharing violation;
- backup;
- and same-volume requirement.

---

# 124. Crash Tests

Terminate the Runtime:

- after staging;
- before replacement;
- during replacement call boundary;
- after replacement before journal update;
- during multi-file apply;
- and during reversal.

---

# 125. Watcher Tests

Generate enough changes to cause:

- coalescing;
- rename ambiguity;
- and buffer overflow.

Verify stale state and full rescan.

---

# 126. Cloud Tests

Where a supported cloud provider is available, test:

- placeholder detection;
- hydration;
- offline state;
- sync conflict;
- and local-only policy.

These tests are environment-specific and not part of the default suite.

---

# 127. Network Tests

Use controlled SMB fixtures for:

- disconnect;
- identity;
- rename;
- locking;
- and watcher behaviour.

Protected-write support remains provisional.

---

# 128. Fuzz Testing

Fuzz:

- path parser;
- namespace classifier;
- component validator;
- reserved-name detector;
- logical-path serialiser;
- archive-entry-to-path conversion;
- and external tool argument builder.

---

# 129. Architecture Tests

Enforce:

- no direct project `File.*` writes outside approved filesystem projects;
- no Plugin Host filesystem implementation reference;
- no Desktop project filesystem access;
- no path-string authorisation;
- no native absolute path in ordinary service contracts;
- no shell command construction from untrusted paths;
- and no alternate-stream operation in ordinary APIs.

---

# 130. Prototype Plan

## 130.1 Prototype A — Path Types

Implement:

- logical parser;
- anchored path;
- namespace classification;
- reserved names;
- and long-path manifest.

---

## 130.2 Prototype B — Workspace Registration

Implement:

- root handle;
- final path;
- volume identity;
- file ID;
- capabilities;
- case state;
- and root-change detection.

---

## 130.3 Prototype C — Reparse Walk

Implement:

- no-follow inspection;
- symlink;
- junction;
- mounted folder;
- external target denial;
- and unknown tag denial.

---

## 130.4 Prototype D — Verified Read

Implement:

- path walk;
- handle open;
- identity;
- content hash;
- and revision record.

---

## 130.5 Prototype E — Protected Replace

Implement:

- same-directory staging;
- journal;
- apply-time revalidation;
- `ReplaceFileW`;
- verification;
- and recovery.

---

## 130.6 Prototype F — Hard Links and Streams

Implement:

- link-count detection;
- named-stream detection;
- replacement preservation test;
- and developer warning.

---

## 130.7 Prototype G — Watch Reconciliation

Implement:

- watcher;
- overflow;
- stale marker;
- and full rescan.

---

## 130.8 Prototype H — Adversarial Race

Implement an attack helper that swaps links and targets during validation.

---

# 131. Implementation Plan

## 131.1 Initial Tasks

1. Record founder review.
2. Pin the .NET LTS release.
3. Define path contracts.
4. Define logical-path parser.
5. Define Windows namespace classifier.
6. Add long-path-aware manifests.
7. Implement volume capability discovery.
8. Implement file and directory handle abstractions.
9. Implement file identity.
10. Implement root registration.
11. Implement reparse classification.
12. Implement component walk.
13. Implement case-sensitive directory detection.
14. Implement hard-link detection.
15. Implement named-stream detection.
16. Implement verified reads.
17. Implement staged replacement.
18. Implement create, delete and rename.
19. Implement watcher reconciliation.
20. Implement diagnostics.
21. Implement adversarial test suite.
22. Benchmark.
23. Complete security review.
24. Accept, amend or reject the ADR.

---

## 131.2 Owners

| Area | Owner |
|---|---|
| Product decision | Founder |
| Workspace architecture | Workspace and Filesystem Architecture Owner |
| Windows adapter | Windows Platform Owner |
| Protected mutation | Patch Engine Owner |
| Repository integration | Repository Owner |
| Build integration | Build Owner |
| Security | Security Owner |
| Recovery | Recovery Owner |
| Desktop presentation | Desktop Owner |
| Testing | Test Architecture Owner |

---

# 132. Suggested Repository Structure

```text
src/
├── Opure.Filesystem.Abstractions/
├── Opure.Filesystem.Contracts/
├── Opure.Filesystem.Paths/
├── Opure.Filesystem.Policy/
├── Opure.Filesystem.Identity/
├── Opure.Filesystem.Enumeration/
├── Opure.Filesystem.Watching/
├── Opure.Filesystem.Mutation/
├── Opure.Filesystem.Recovery/
├── Opure.Filesystem.Platform.Abstractions/
└── Opure.Filesystem.Platform.Windows/
```

Service projects:

```text
├── Opure.Workspace/
├── Opure.Workspace.Persistence/
├── Opure.Patching/
└── Opure.Patching.Persistence/
```

Test projects:

```text
tests/
├── Opure.Filesystem.UnitTests/
├── Opure.Filesystem.WindowsTests/
├── Opure.Filesystem.SecurityTests/
├── Opure.Filesystem.RecoveryTests/
├── Opure.Filesystem.FuzzTests/
└── Opure.Filesystem.PerformanceTests/
```

---

# 133. Windows Adapter APIs

The Windows adapter may use narrowly reviewed access to:

- `CreateFileW`;
- `GetFileInformationByHandle`;
- `GetFileInformationByHandleEx`;
- `GetFinalPathNameByHandleW`;
- `GetVolumeInformationByHandleW`;
- `DeviceIoControl` for approved reparse queries;
- `ReplaceFileW`;
- `SetFileInformationByHandle` where justified;
- and safe file-handle APIs.

---

## 133.1 P/Invoke Rules

P/Invoke declarations should:

- use Unicode APIs;
- use source-generated interop where appropriate;
- use SafeHandle;
- specify exact error handling;
- avoid raw pointer ownership where possible;
- and remain internal to the Windows adapter.

---

## 133.2 No Native API Spread

Domain and service projects must not call Win32 filesystem APIs directly.

---

# 134. .NET APIs

Use ordinary .NET APIs where they provide required semantics, including:

- `Path.GetFullPath`;
- `Path.IsPathFullyQualified`;
- `FileSystemInfo.LinkTarget`;
- `File.ResolveLinkTarget`;
- `Directory.ResolveLinkTarget`;
- `EnumerationOptions`;
- `FileStream`;
- and `File.Replace`.

Native APIs remain necessary for identity, tags and no-follow handling.

---

# 135. Coding Rules

Filesystem code should:

- avoid raw string paths after validation;
- use nullable analysis;
- use cancellation;
- bound allocations;
- avoid recursive call-stack traversal;
- use explicit comparisons;
- and translate platform errors into stable Opure errors.

---

# 136. String Prefix Prohibition

The following is prohibited as a security decision:

```csharp
candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase)
```

A prefix can match:

```text
C:\Project
C:\Project-Escape
```

and does not resolve reparse behaviour.

---

# 137. Relative-Path Calculation

`Path.GetRelativePath` may be used for display after both paths are verified under compatible roots.

It is not a containment proof by itself.

---

# 138. Separator Handling

Logical paths use `/`.

Windows accepts `\` at the platform boundary.

Untrusted input containing mixed separators should be parsed consistently and never passed directly to a shell.

---

# 139. Trailing Separators

Directory display may preserve a conceptual directory marker.

Internal canonical paths should have one defined root and trailing-separator policy.

---

# 140. Empty and Whitespace Paths

Reject empty or whitespace-only untrusted paths except where an API explicitly permits the logical root.

Whitespace is meaningful inside ordinary names.

Do not trim arbitrary internal whitespace.

---

# 141. Wildcards

Ordinary path values do not permit wildcard semantics.

Glob patterns are separate types.

---

# 142. Glob Types

A glob is:

- relative;
- bounded;
- explicitly parsed;
- and evaluated by the Workspace Service.

It is never passed unquoted to a shell.

---

# 143. Archive Extraction

Archive entry names require the same path parser.

Reject:

- absolute paths;
- parent traversal;
- device names;
- streams;
- and links escaping the extraction root.

Full archive handling requires a separate implementation decision.

---

# 144. Temporary Files

Project-adjacent staging files should be recognisable as Opure-owned without exposing data.

They should be ignored by ordinary UI and cleaned through recovery.

---

## 144.1 Git Visibility

Staging files should not remain long enough to appear as normal project changes.

If they do, the Workspace Service should classify them clearly.

---

# 145. Backup Files

Patch backups should live in the Opure recovery area where possible.

If `ReplaceFileW` requires a same-volume backup path, use a controlled same-volume recovery location or temporary backup and then move after completion.

The design must preserve recoverability.

---

# 146. Recovery Area

A per-workspace recovery area may require:

- same volume;
- restricted access;
- hidden state;
- and exclusion from Git.

Its final location requires prototype evidence.

---

# 147. Disk Space

Before staging, estimate:

- new content;
- backup;
- journal;
- and filesystem overhead.

---

## 147.1 Low Space

Deny the patch before mutation when safe completion and recovery space are insufficient.

---

# 148. File Size Limits

Service APIs should define bounds for:

- preview;
- hash;
- diff;
- patch;
- stream;
- and in-memory content.

Large files use streaming.

---

# 149. Sparse Files

Reading a sparse file may produce a large logical size.

Use size limits and streaming.

Replacing it may remove sparsity unless explicitly preserved.

Show the risk.

---

# 150. Compressed Files

Replacement should preserve compression where supported and tested.

---

# 151. Encrypted Files

Replacement should preserve EFS state where supported and tested.

If the Runtime cannot access the encryption context, deny mutation.

---

# 152. Executable Files

Executable and script mutations receive higher risk classification.

---

# 153. Read-Only Workspace

A workspace may be registered for read-only inspection.

Patch proposals remain possible but cannot be applied.

---

# 154. Policy Integration

Filesystem policy considers:

- workspace capability;
- object type;
- reparse state;
- destination;
- path sensitivity;
- hard-link state;
- file attributes;
- operation;
- and approval.

---

# 155. Approval Integration

High-risk approvals may be required for:

- external link target;
- additional root;
- hard-linked file;
- executable file;
- hidden or system file;
- cross-volume move;
- recursive deletion;
- cloud hydration;
- and unsupported filesystem.

---

# 156. Approval Binding

Approval binds to:

- workspace identity;
- logical path;
- resolved identity;
- expected revision;
- operation;
- additional root;
- and risk.

Any material change invalidates approval.

---

# 157. Desktop Presentation

Patch review should show badges for:

- Link;
- External;
- Junction;
- Mounted Volume;
- Cloud;
- Case Sensitive;
- Hard Linked;
- Alternate Streams;
- Read Only;
- Encrypted;
- Compressed;
- and Unsupported.

---

# 158. Developer Override

An override does not disable all future path validation.

It authorises one bounded operation under exact reviewed conditions.

---

# 159. Safe Mode

Safe Mode should:

- disable third-party write capabilities;
- allow root and path diagnostics;
- allow recovery;
- avoid cloud hydration;
- and deny unknown reparse traversal.

---

# 160. Recovery Mode

Recovery Mode should support:

- workspace-root verification;
- journal inspection;
- staged-file inspection;
- backup restore;
- identity comparison;
- and orphan cleanup.

---

# 161. Release Requirements

A release must not ship if:

- path authorisation uses string prefix alone;
- long-path manifest is absent;
- device paths are accepted by ordinary APIs;
- alternate streams are writable through ordinary paths;
- reparse points are blindly followed;
- hard-linked files are silently replaced;
- apply-time revision is not checked;
- cross-volume replacement is called atomic;
- watcher overflow is ignored;
- or multi-file recovery is untested.

---

# 162. Acceptance Criteria

This ADR may move to **Accepted** when:

- [ ] Path contracts distinguish untrusted, logical, anchored and verified paths.
- [ ] Ordinary service contracts use workspace-relative logical paths.
- [ ] `Path.GetFullPath(path, basePath)` is used for deterministic native anchoring.
- [ ] Process current directory is not used for authorisation.
- [ ] Drive-relative paths are rejected.
- [ ] Root-relative paths are rejected.
- [ ] Device and NT namespace paths are rejected in ordinary APIs.
- [ ] Named-pipe and device names are rejected.
- [ ] Alternate-stream syntax is rejected.
- [ ] Reserved DOS names are rejected.
- [ ] Trailing period and space creation is rejected.
- [ ] Unicode names are preserved exactly.
- [ ] Long-path-aware manifests are present in all shipped executables.
- [ ] Paths above legacy `MAX_PATH` work in supported operations.
- [ ] External tool long-path limitations are visible.
- [ ] Workspace registration captures final root identity.
- [ ] Root replacement is detected.
- [ ] Volume capabilities are recorded.
- [ ] Drive class is recorded.
- [ ] Per-directory case sensitivity is detected.
- [ ] Case collisions are detected.
- [ ] String prefix is not used as the sole containment test.
- [ ] Every existing protected path component is inspected.
- [ ] Reparse tags are classified.
- [ ] Unknown reparse tags deny protected mutation.
- [ ] Internal symlink policy works.
- [ ] External symlink targets are denied by default.
- [ ] Junction cross-volume behaviour is visible.
- [ ] Mounted folders become separate roots.
- [ ] Broken links are preserved without target creation.
- [ ] Link-object deletion does not delete target content.
- [ ] Cloud placeholders are detected.
- [ ] Hydration is not silent.
- [ ] Local-only policy applies to hydration.
- [ ] File identity uses volume and file ID where supported.
- [ ] Same-object comparison works across hard links.
- [ ] Hard-link count is detected.
- [ ] Hard-linked replacement is denied or explicitly approved.
- [ ] Named streams are detected.
- [ ] `ReplaceFileW` named-stream preservation is tested.
- [ ] Protected reads return identity and revision.
- [ ] Protected writes revalidate identity and revision.
- [ ] Stale patches do not overwrite new developer changes.
- [ ] Non-existing targets verify their parent and absence.
- [ ] Same-directory staging is implemented.
- [ ] Staging content is hashed and verified.
- [ ] Existing-file replacement remains same-volume.
- [ ] ACL preservation is tested.
- [ ] encryption and compression behaviour is tested where available.
- [ ] Cross-volume moves are not described as atomic.
- [ ] Multi-file patches use durable journals.
- [ ] Interrupted multi-file apply recovers.
- [ ] Recursive delete does not follow reparse targets.
- [ ] Case-only rename is recoverable.
- [ ] Sharing violations produce exact state.
- [ ] FileSystemWatcher overflow triggers rescan.
- [ ] Network workspaces do not receive unsupported guarantees.
- [ ] WSL case-sensitive fixtures are tested.
- [ ] Plugin direct project filesystem access is blocked.
- [ ] MCP direct filesystem authority is blocked.
- [ ] AI context excludes unapproved external link targets.
- [ ] Architecture tests pass.
- [ ] Adversarial TOCTOU tests pass.
- [ ] Performance is measured.
- [ ] Security review is complete.
- [ ] Founder approval is recorded.

---

# 163. Evidence Required Before Acceptance

- [ ] path-type implementation;
- [ ] namespace classification report;
- [ ] reserved-name test report;
- [ ] long-path test report;
- [ ] workspace-registration prototype;
- [ ] volume-capability report;
- [ ] case-sensitivity report;
- [ ] reparse-walk prototype;
- [ ] symlink and junction security report;
- [ ] cloud-placeholder report;
- [ ] file-identity report;
- [ ] hard-link report;
- [ ] alternate-stream report;
- [ ] staged-replacement report;
- [ ] metadata-preservation report;
- [ ] crash-recovery report;
- [ ] watcher-overflow report;
- [ ] TOCTOU adversarial report;
- [ ] architecture-test report;
- [ ] performance benchmark;
- [ ] security review;
- [ ] founder approval.

---

# 164. Known Limitations

- The exact .NET LTS release is not yet pinned.
- The final path-type names are not final.
- The Windows P/Invoke surface is not yet implemented.
- Full network-workspace protected writes are deferred.
- Full `\\wsl$` support is deferred.
- USN-journal indexing is deferred.
- Hard-link path enumeration may be deferred.
- Cloud-provider-specific hydration UX is not final.
- Unknown reparse tags are denied rather than supported.
- A same-user malicious process can still race some operations.
- Windows does not provide general multi-file atomicity for this design.
- Exact directory-handle mutation strategy is not final.
- External tools can still have weaker path behaviour.
- Secure deletion cannot be guaranteed.
- ReFS, FAT and exFAT support levels require testing.
- Unicode confusable detection is deferred.
- Archive extraction is not fully specified.
- Filesystem broker process is deferred.
- Platform-equivalent Linux and macOS identity semantics are deferred.

---

# 165. Open Questions

- Which .NET LTS release should be pinned?
- What should the final strongly typed path names be?
- Should workspace roots permit selected aliases or always display the final target?
- How should handle-based containment be implemented efficiently?
- Should the Windows adapter use source-generated P/Invoke?
- Should every protected component remain open through the mutation?
- Should rename-by-handle be used for stronger race resistance?
- Which reparse tags are allowed for read-only use?
- Which cloud-placeholder providers should be recognised?
- How should hydration size be estimated?
- Should cloud-synchronised workspaces permit protected writes in version 1.0?
- Should network workspaces be read-only in version 1.0?
- What is the ReFS support level?
- What is the exFAT support level?
- Should case-sensitive nested directories be cached?
- How should Git `core.ignorecase` mismatches be displayed?
- Should hard-linked file mutation always be denied in version 1.0?
- Should all named streams block replacement, or only unknown and sensitive streams?
- Where should same-volume patch backups be placed?
- Should the Patch Service use `File.Replace` or direct `ReplaceFileW`?
- How should unsupported ACL merge behaviour be handled?
- Should a directory handle remain open while creating the leaf?
- How should directory recursive deletion plans scale?
- Which watcher strategy is appropriate for very large repositories?
- Should USN Journal support be added before version 1.0?
- How should WSL repositories be registered?
- How should submodules outside the main root be represented?
- Should worktree administrative paths receive a separate repository capability?
- Which file attributes should increase risk?
- Should executable replacement require explicit approval every time?
- How should sparse files be preserved?
- How should EFS-encrypted files be tested in CI?
- What exact content revision fields should patch approval bind to?
- How should file IDs be stored across Runtime restart?
- How should removable-volume identity changes be detected?
- What errors should permit a retry?
- How should external tools receive paths above their supported limit?
- Should Opure offer a short temporary working-path alias for a tool?
- How should such an alias preserve containment and cleanup?
- Which path details belong in Trust Centre?
- Which absolute path segments should diagnostics redact?
- When should the filesystem boundary move to a broker process?

---

# 166. Deferred Decisions

This ADR intentionally defers:

- complete Patch Service algorithm to implementation under SPEC-009;
- USN Journal use to a Filesystem Indexing ADR;
- dedicated filesystem broker to a Filesystem Isolation ADR;
- WSL-native execution to a WSL Integration ADR;
- remote and network workspaces to a Remote Workspace ADR;
- archive extraction to an Archive Security ADR;
- build sandboxing to a Build Isolation ADR;
- Unicode-confusable policy to a User Interface Security ADR;
- secure deletion to a Data Disposal ADR;
- and Linux and macOS filesystem identity to future platform ADRs.

---

# 167. Alternatives Rejected

## 167.1 String Normalisation as Complete Security

Rejected because lexical normalisation cannot prove object containment across reparse points, hard links, case-sensitive directories or object replacement.

---

## 167.2 Copying Every Repository into an Opure Sandbox

Rejected because it disrupts the developer's normal workspace and creates expensive synchronisation and duplication.

---

## 167.3 Git as the Entire Filesystem Layer

Rejected because not all project files or projects are Git controlled, and Git still relies on filesystem semantics.

---

## 167.4 Dedicated Broker Immediately

Not selected initially because the trusted Runtime can host the Workspace boundary while the first vertical slice proves semantics.

The design preserves later extraction.

---

## 167.5 Direct Plugin and Service Access

Rejected because it would bypass policy, patch review, recovery and developer visibility.

---

# 168. Official Evidence References

The following official Microsoft sources informed this ADR:

- [Naming Files, Paths, and Namespaces](https://learn.microsoft.com/en-us/windows/win32/fileio/naming-a-file)
- [Application manifests and `longPathAware`](https://learn.microsoft.com/en-us/windows/win32/sbscs/application-manifests)
- [`Path.GetFullPath`](https://learn.microsoft.com/en-us/dotnet/api/system.io.path.getfullpath)
- [`FileSystemInfo.LinkTarget`](https://learn.microsoft.com/en-us/dotnet/api/system.io.filesysteminfo.linktarget)
- [`File.ResolveLinkTarget`](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.resolvelinktarget)
- [`Directory.ResolveLinkTarget`](https://learn.microsoft.com/en-us/dotnet/api/system.io.directory.resolvelinktarget)
- [Reparse points](https://learn.microsoft.com/en-us/windows/win32/fileio/reparse-points)
- [Reparse point tags](https://learn.microsoft.com/en-us/windows/win32/fileio/reparse-point-tags)
- [Reparse points and file operations](https://learn.microsoft.com/en-us/windows/win32/fileio/reparse-points-and-file-operations)
- [`CreateFile` and `FILE_FLAG_OPEN_REPARSE_POINT`](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilew)
- [`GetFinalPathNameByHandle`](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getfinalpathnamebyhandlew)
- [`GetFileInformationByHandle`](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getfileinformationbyhandle)
- [`GetFileInformationByHandleEx`](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-getfileinformationbyhandleex)
- [`FILE_ID_INFO`](https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_id_info)
- [Hard Links and Junctions](https://learn.microsoft.com/en-us/windows/win32/fileio/hard-links-and-junctions)
- [Case sensitivity](https://learn.microsoft.com/en-us/windows/wsl/case-sensitivity)
- [`GetVolumeInformation`](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getvolumeinformationw)
- [`ReplaceFileW`](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-replacefilew)
- [Moving and Replacing Files](https://learn.microsoft.com/en-us/windows/win32/fileio/moving-and-replacing-files)
- [NTFS alternate data streams and Streams](https://learn.microsoft.com/en-us/sysinternals/downloads/streams)
- [`EnumerationOptions`](https://learn.microsoft.com/en-us/dotnet/api/system.io.enumerationoptions)
- [`FileSystemWatcher`](https://learn.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher)
- [`FileSystemWatcher.InternalBufferSize`](https://learn.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher.internalbuffersize)
- [Cloud Files placeholder state](https://learn.microsoft.com/en-us/windows/win32/api/cfapi/nf-cfapi-cfgetplaceholderstatefromattributetag)
- [File-system placeholders](https://learn.microsoft.com/en-us/windows-hardware/drivers/ifs/placeholders)

Versions and filesystem behaviour can change.

The implementation team must verify all selected APIs against the pinned Windows 11 and .NET support baseline before acceptance.

---

# 169. Review Record

| Date | Reviewer | Decision | Notes |
|---|---|---|---|
| 18 July 2026 | Architecture draft | Proposed | Handle-verified Workspace filesystem boundary recommended |

---

# 170. Approval

## Founder or Product Approval

- **Name:** Christopher Dyer
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Pending founder review

## Architecture Approval

- **Name or role:** Workspace and Filesystem Architecture Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Path types, handles, reparse policy and replacement prototype required

## Security Approval

- **Name or role:** Security Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Path escape, TOCTOU, links, streams and device namespace tests required

## Recovery Approval

- **Name or role:** Recovery Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Staging, replacement and interrupted multi-file recovery evidence required

## Test Approval

- **Name or role:** Test Architecture Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Windows adversarial filesystem suite required

---

# 171. Supersession

This ADR is superseded only when a later ADR:

- names ADR-0009 explicitly;
- explains why path identity, containment or mutation changed;
- identifies affected services and platforms;
- describes migration of stored path and workspace identity;
- explains security and recovery impact;
- and updates the `Superseded by` field.

Historical ADRs remain in version control.

---

# 172. Change History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | 18 July 2026 | Founder Draft | Initial handle-verified Windows path and filesystem recommendation |

---

# 173. Final Decision Statement

> **Opure will provisionally use a central handle-verified Windows filesystem boundary with strongly typed logical paths, deterministic lexical anchoring, long-path-aware executables, component-level reparse inspection, volume and file identity, case-aware comparison, hard-link and alternate-stream detection, same-volume staged replacement and durable recovery journals, because a Windows path string is a name rather than a trustworthy object identity and developer-controlled repositories require protection against redirection, ambiguity, races and partial mutation.**

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**