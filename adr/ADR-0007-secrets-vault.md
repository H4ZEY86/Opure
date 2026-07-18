# ADR-0007 — Secrets Vault

## Architecture Decision Record

**Status:** Proposed  
**Date:** 18 July 2026  
**Decision owners:** Founder and Product Owner  
**Technical owners:** Security Architecture Owner  
**Reviewers:** Runtime Architecture Owner, Persistence Owner, Plugin Owner, AI Router Owner, MCP Owner, Network Owner, Recovery Owner, Privacy Owner  
**Supersedes:** None  
**Superseded by:** None  
**Related ADRs:** ADR-0001 Primary Implementation Language, ADR-0003 Runtime Process Topology, ADR-0004 Local IPC, ADR-0005 Persistence, ADR-0006 Logging and Observability  
**Related specifications:** CHARTER-001, SPEC-001, SPEC-002, SPEC-003, SPEC-004, SPEC-006, SPEC-007, SPEC-008, SPEC-009, SPEC-010, SPEC-011, SPEC-012  
**Target milestone:** Phase 0 — Founding Baseline through Phase 8 — Trust Centre and Security Completion  

---

## 1. Decision Summary

Opure should implement a dedicated **application-managed encrypted Secrets Vault**.

The Windows-first implementation should use:

- a cryptographically random 256-bit Vault Root Key;
- Windows Data Protection API through `ProtectedData` with `DataProtectionScope.CurrentUser` to protect the Vault Root Key at rest;
- an explicit platform key-protector abstraction so future Linux and macOS implementations can use native platform protection;
- HKDF-SHA-256 to derive a distinct 256-bit record-encryption key for each secret version;
- AES-256-GCM with a required 16-byte authentication tag for each encrypted secret record;
- a unique random record salt;
- a unique random 12-byte nonce;
- authenticated additional data that binds ciphertext to immutable record identity, scope, version, format and key version;
- encrypted secret records stored outside ordinary SQLite databases;
- non-secret metadata stored separately in a service-owned metadata database;
- atomic record replacement and a durable mutation journal;
- versioned key rotation;
- integrity checks;
- scoped secret-use leases;
- and brokered delivery to approved adapters and external processes.

Secret consumers should normally request **use of a secret for a declared purpose**, not unrestricted retrieval of its plaintext.

The Desktop, project memory, embeddings, workflow checkpoints, logs, traces, Trust Centre, plugin storage and ordinary service databases must never contain raw secret values.

Windows Credential Manager is not selected as the primary vault store.

It may be used later for narrowly defined interoperability, but Opure's authoritative secret model remains provider-neutral and application-owned.

The Windows implementation should use Current User DPAPI, not Local Machine DPAPI, because machine-scoped protection would permit other users on the same computer to decrypt protected data if they can access the blob.

Opure must state honestly that:

- DPAPI Current User protection is tied to the Windows user profile and usually the computer;
- a malicious process already running as the same user may be able to access user-protected secrets;
- plaintext must exist briefly in process memory when a secret is used;
- memory clearing reduces exposure but does not guarantee that no copy ever existed;
- and vault encryption does not replace full-disk encryption, operating-system account security or endpoint protection.

---

## 2. Status

Current status:

> **Proposed**

This ADR becomes **Accepted** only after prototypes demonstrate:

- Vault creation;
- DPAPI root-key protection;
- record encryption and authenticated decryption;
- record tamper detection;
- record substitution detection;
- nonce and salt generation;
- secret-use leases;
- cancellation and expiry;
- adapter use without Desktop retrieval;
- Plugin Host mediation;
- external command injection;
- key rotation;
- interrupted mutation recovery;
- backup and restore behaviour;
- Windows profile and machine-bound failure handling;
- secret-canary tests across every prohibited subsystem;
- bounded memory handling;
- and acceptable performance.

---

## 3. Context

Opure must store and use secrets such as:

- local and remote AI-provider API keys;
- MCP credentials;
- Git hosting tokens;
- package-registry tokens;
- signing credentials;
- repository credentials;
- database passwords;
- proxy credentials;
- webhook credentials;
- external service passwords;
- private keys;
- certificate passphrases;
- and plugin-specific credentials approved by the developer.

The Charter requires that secret values never enter:

- normal databases;
- project memory;
- embeddings;
- logs;
- workflow checkpoints;
- plugin storage;
- conversations;
- or the Trust Centre.

The architecture also requires:

- project-level cloud policies;
- visible external destinations;
- least privilege;
- plugin isolation;
- MCP mediation;
- explicit command plans;
- and recoverable state changes.

A secret therefore cannot be treated as an ordinary configuration string.

The Vault must protect secrets at rest and reduce exposure during use.

It must remain local and usable without a cloud account.

The first implementation targets Windows 11.

Future Linux and macOS support must not require changing every secret record or consumer contract.

---

## 4. Problem Statement

Opure requires a local Secrets Vault that encrypts secrets at rest, minimises plaintext exposure, binds every use to an authorised purpose, supports isolated plugins and external tools, survives crashes and key rotation, and remains portable across future operating systems without placing secret values in ordinary storage or developer-facing history.

---

## 5. Decision Drivers

The decision is evaluated against:

- alignment with the Opure Charter;
- local-first operation;
- no cloud dependency;
- Windows 11 support;
- future Linux and macOS support;
- strong authenticated encryption;
- operating-system-backed key protection;
- secret-value isolation;
- project isolation;
- provider neutrality;
- plugin mediation;
- MCP mediation;
- external process injection;
- least privilege;
- developer visibility;
- consent;
- memory minimisation;
- rotation;
- revocation;
- backup;
- recovery;
- corruption detection;
- testability;
- auditability without value disclosure;
- performance;
- package maturity;
- small-team implementation;
- and replacement cost.

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
- **Performance Respect**
- **No Hidden Authority**
- **No Secret Values in Ordinary Storage**
- **No Secret Values in Logs**
- **No Secret Values in Trust Records**
- **No Silent External Sharing**
- **Honest Security Claims**
- **Recoverability**

Specific requirements include:

- Secret detection occurs before external sharing.
- Generated changes are scanned for secrets.
- Sensitive files are protected from accidental commit.
- Secrets remain in an encrypted Vault.
- Plugins do not access the Vault directly.
- MCP servers do not access the Vault directly.
- AI providers receive only secrets required for the selected destination and purpose.
- The Desktop does not become a secret-distribution channel.
- A valid secret does not grant permission to use it.
- Secret use requires policy and capability evaluation.
- Revocation must affect future use immediately.
- Trust records identify purpose and destination without exposing values.
- Recovery must not export or reveal secret plaintext.

---

## 7. Scope

This ADR decides:

- Vault ownership;
- Windows root-key protection;
- key hierarchy;
- record encryption;
- record format;
- Vault storage layout;
- non-secret metadata storage;
- secret identity;
- secret scopes;
- secret types;
- create, read, update and delete semantics;
- secret-use leases;
- consumer APIs;
- plugin and MCP access;
- external process injection;
- memory handling;
- rotation;
- revocation;
- backup;
- restore;
- integrity;
- recovery;
- logging and Trust Centre boundaries;
- and cross-platform abstraction.

This ADR does not decide:

- hardware security module integration;
- enterprise key escrow;
- organisation-managed secret servers;
- cloud secret-manager integration;
- team sharing;
- password generation UI;
- automatic browser credential import;
- certificate-authority management;
- code-signing workflow;
- Windows Hello user-presence enforcement;
- database-wide encryption;
- full-disk encryption;
- or secure remote collaboration.

---

## 8. Constraints

Known constraints include:

- Windows 11 is the first supported platform.
- C#/.NET is the proposed implementation stack.
- The Runtime initially hosts the trusted Vault service.
- The Desktop communicates through the Desktop Gateway.
- Plugin Hosts are separate processes.
- MCP servers are external trust boundaries.
- AI providers are external processes or endpoints.
- The platform must work offline.
- Secret values must remain outside ordinary SQLite databases.
- The same-user threat cannot be completely defeated by a user-mode Vault.
- The Vault must not require administrator rights.
- Normal Vault use should not require an interactive operating-system prompt.
- The Vault must recover from interrupted updates.
- External command lines may be observable to other processes.
- Environment variables may be inherited.
- Temporary files may persist after crashes.
- Managed strings cannot be reliably zeroed.
- Cross-platform consumers need transport-neutral secret references.
- Backups must not silently make secrets portable to another machine.
- Test secrets must never use real credentials.
- Development convenience must not disable production safeguards.

---

## 9. Assumptions

This decision assumes:

- The primary user has a protected Windows account.
- DPAPI is available under the selected Windows and .NET versions.
- The Windows user profile is loaded.
- Opure runs as the interactive user for ordinary operation.
- A randomly generated root key can remain in Runtime memory while the Vault is unlocked.
- Most secrets are small.
- Very large private-key or certificate data can still be represented as bounded encrypted records.
- The Runtime can mediate secret use for providers, plugins, MCP and commands.
- The Desktop does not need raw secret values after initial entry.
- Most secret use can be implemented through a callback, lease or destination-specific operation.
- Provider adapters can accept secret material through narrow interfaces.
- Secret metadata is not itself a secret, but may be Sensitive.
- Secret record ciphertext can be stored in files outside SQLite.
- Future platform key protectors can wrap the same Vault Root Key format.
- Developer-selected secret export can be treated as a separate high-risk workflow.
- Full recovery across another machine requires an explicit future portable recovery mechanism.

---

## 10. Official Platform Evidence

Official Microsoft documentation available on 18 July 2026 establishes that:

- `ProtectedData` provides .NET access to Windows DPAPI.
- DPAPI can protect data using current-user or local-machine scope.
- Current-user DPAPI normally requires the same user credentials and the same computer for decryption.
- Local-machine scope permits any user on that computer to decrypt the data if they can access the protected blob.
- DPAPI includes integrity protection for protected blobs.
- DPAPI depends on the user's Windows profile.
- `AesGcm` provides authenticated AES-GCM encryption and supports explicit required tag sizes.
- .NET provides HKDF based on RFC 5869.
- `RandomNumberGenerator.Fill` produces cryptographically strong random bytes.
- `CryptographicOperations.ZeroMemory` provides an explicit zeroing operation for mutable byte buffers.
- Windows Credential Manager generic credential blobs are bounded and are application-readable under the user's context.

These facts support using DPAPI to protect a small Vault Root Key rather than using Credential Manager or DPAPI independently as Opure's complete cross-platform secret data model.

The implementation team must verify current framework behaviour before moving this ADR to Accepted.

---

## 11. Threat Model

The Vault is designed to reduce exposure against:

- offline theft of Opure Vault files;
- accidental source-control commit;
- accidental inclusion in normal backups;
- ordinary database inspection;
- log and diagnostic leakage;
- plugin access;
- MCP access;
- provider misrouting;
- command-line leakage;
- stale secret use;
- record tampering;
- record substitution;
- interrupted writes;
- and accidental cross-project use.

---

## 11.1 Partially Addressed Threats

The Vault partially addresses:

- another ordinary local user;
- malware without the user's security context;
- opportunistic same-user process access;
- memory scraping;
- process injection;
- and local administrator access.

Protection depends on operating-system boundaries and implementation details.

---

## 11.2 Out-of-Scope Threats

The first Vault does not claim complete protection against:

- compromised Windows account;
- administrator or kernel compromise;
- malicious code executing as the same user with equivalent access;
- physical memory acquisition;
- hostile debugger attached to the Runtime;
- hardware compromise;
- compromised provider destination;
- credential already disclosed before entry;
- or a developer explicitly revealing a secret.

---

## 11.3 Honest Security Statement

Opure protects secrets against many common storage and integration failures.

It does not make a compromised machine safe.

---

## 12. Options Considered

The principal options are:

1. **Option A — Application Vault with DPAPI-Protected Root Key**
2. **Option B — Windows Credential Manager as the Complete Vault**
3. **Option C — DPAPI Protect Every Secret Independently**
4. **Option D — ASP.NET Core Data Protection as the Complete Vault**
5. **Option E — Password-Derived Encrypted Vault**
6. **Option F — Local Secret-Manager Server**
7. **Option G — Cloud Secret Manager**
8. **Option H — Plain Configuration with File ACLs**
9. **Option I — Dedicated Vault Process from the Beginning**

---

# 13. Option A — Application Vault with DPAPI-Protected Root Key

## 13.1 Description

Generate one or more random Vault Root Keys.

Protect each root key through the operating-system key protector.

On Windows, use DPAPI Current User.

Derive one record key per secret version with HKDF-SHA-256.

Encrypt each record through AES-256-GCM.

Store encrypted records in a dedicated Vault directory and non-secret metadata in a separate metadata store.

---

## 13.2 Advantages

- Strong authenticated encryption.
- One portable application record format.
- Windows key protection uses the operating system.
- Future Linux and macOS key protectors can wrap the same root key.
- Secret records can rotate independently.
- Root-key rotation is manageable.
- Secret records remain outside ordinary SQLite.
- Per-record integrity.
- Record identity can be authenticated.
- Supports project, provider, plugin and global scopes.
- Supports destination-specific use.
- Supports metadata search without decrypting values.
- Supports encrypted backup.
- Avoids Credential Manager size limitations as the main data model.
- Avoids DPAPI format coupling for every secret record.
- Minimises OS-specific code.
- Supports future optional portable recovery.
- Can isolate cryptographic code.
- Allows explicit versioning and migration.
- Supports atomic per-record update.
- Supports key hierarchy and revocation.

---

## 13.3 Disadvantages

- Opure must implement a secure record format.
- Opure must manage root keys and rotation.
- Opure must implement atomic storage and recovery.
- Plaintext exists in Runtime memory during use.
- Root key is present while the Vault is unlocked.
- Metadata and encrypted records require consistency.
- Cross-machine restore is not automatic with DPAPI.
- Same-user malware remains a risk.
- More implementation than direct Credential Manager calls.
- Cryptographic format mistakes could be serious.
- Backup and migration require careful design.
- Secure deletion cannot be guaranteed.
- A root-key compromise exposes all records protected by that key version.

---

## 13.4 Risks

- Nonce reuse.
- incorrect associated data;
- incorrect HKDF context;
- root key written to disk;
- raw secret converted to immutable string;
- secret logged by exception;
- stale record accepted;
- metadata and ciphertext mismatch;
- rotation interrupted;
- unprotected backup;
- DPAPI scope changed accidentally;
- optional entropy treated as a secret;
- custom cryptography added;
- or secret-use API becomes broad retrieval.

---

## 13.5 Mitigations

- standard .NET cryptography;
- fixed format;
- test vectors;
- independent security review;
- random nonce and salt;
- per-record derived key;
- immutable authenticated header;
- no secret strings in API;
- secret-canary tests;
- mutation journal;
- redundant manifests;
- key-version registry;
- fail-closed parsing;
- and no custom primitive.

---

## 13.6 Estimated Adoption Cost

- **Initial implementation:** Moderate to High
- **Operational complexity:** Moderate
- **Migration difficulty:** Low to Moderate
- **Replacement difficulty:** Moderate

---

# 14. Option B — Windows Credential Manager as the Complete Vault

## 14.1 Description

Store every secret directly as a Windows generic credential.

---

## 14.2 Advantages

- Operating-system-managed storage.
- Existing Windows UI.
- No application encryption format.
- Simple API.
- Per-user storage.
- Credential roaming may be available in some environments.
- Familiar security boundary.
- No root key managed by Opure.

---

## 14.3 Disadvantages

- Windows only.
- Generic credential values are readable by user processes with suitable access.
- Credential blob size is bounded.
- Metadata model is limited.
- Project and capability scoping must be encoded into names.
- Atomic multi-record mutation is difficult.
- Export and recovery semantics are constrained.
- Key rotation is not an Opure-controlled concept.
- Rich secret types require custom encoding.
- Testing and isolated profiles are harder.
- Future Linux and macOS migration requires a new store.
- Credential Manager UI may expose confusing internal records.
- Plugin and provider metadata remain elsewhere.
- Backup and restore behaviour depends on Windows profile behaviour.

---

## 14.4 Decision Relevance

Credential Manager may later store:

- a small bootstrap secret;
- interoperability credential;
- or organisation-managed login

when explicitly justified.

It is not selected as Opure's authoritative Vault.

---

## 14.5 Estimated Adoption Cost

- **Initial implementation:** Low
- **Operational complexity:** Moderate
- **Migration difficulty:** High
- **Replacement difficulty:** High

---

# 15. Option C — DPAPI Protect Every Secret Independently

## 15.1 Description

Protect each secret value directly with DPAPI and store the resulting blob.

---

## 15.2 Advantages

- Simple cryptographic design.
- Operating-system protection for every record.
- Built-in integrity.
- No application root key.
- No AES-GCM record encryption code.
- Per-record independent protection.

---

## 15.3 Disadvantages

- Every record is Windows-specific.
- Future platform migration requires decrypt-and-reencrypt.
- Cross-platform backup format is weak.
- Rotation and algorithm migration are OS-coupled.
- Metadata binding is limited unless optional entropy is managed carefully.
- High record counts require many DPAPI operations.
- Recovery remains tied to Windows profile.
- Secret storage format cannot be inspected independently.
- Application-level record versioning is still required.
- Consumer APIs remain unsolved.
- A later hardware-backed protector cannot wrap the existing portable root model.

---

## 15.4 Estimated Adoption Cost

- **Initial implementation:** Low to Moderate
- **Operational complexity:** Moderate
- **Migration difficulty:** High
- **Replacement difficulty:** High

---

# 16. Option D — ASP.NET Core Data Protection as the Complete Vault

## 16.1 Description

Use ASP.NET Core Data Protection to protect secret payloads.

Persist its key ring with DPAPI protection.

---

## 16.2 Advantages

- Mature .NET framework.
- Purpose strings.
- Key rotation.
- Windows DPAPI integration.
- Authenticated payloads.
- Well-tested framework.
- Less custom key management.

---

## 16.3 Disadvantages

- Designed primarily for application data-protection scenarios rather than a complete secret broker.
- Key-ring semantics may not match per-secret lifecycle.
- Storage and revocation remain custom.
- Secret metadata remains custom.
- Cross-process secret-use capability remains custom.
- Payload format is framework controlled.
- Long-term secret archive and export requirements may not align.
- Dependency surface is broader.
- Cross-platform protector behaviour still needs design.
- Per-record independent rotation and version controls are less explicit.
- Developer-facing secret inventory remains custom.

---

## 16.4 Decision Relevance

ASP.NET Core Data Protection may be evaluated for selected local tokens or transient application state.

It is not selected as the complete Vault architecture.

---

# 17. Option E — Password-Derived Encrypted Vault

## 17.1 Description

Require the developer to create a Vault password.

Derive a master key from the password through a password-based KDF.

---

## 17.2 Advantages

- Portable across machines.
- Independent from Windows account.
- Developer can back up and restore.
- Strong protection when the password is strong.
- Can require deliberate unlock.
- No operating-system-specific key protector.

---

## 17.3 Disadvantages

- Adds password burden.
- Password loss can destroy access.
- Weak passwords reduce security.
- Requires expensive KDF tuning.
- Unlock interrupts workflows.
- Password entry creates phishing and UI risk.
- Password must exist in memory.
- Automated background use becomes harder.
- Recovery and reset are complex.
- Conflicts with low-friction local use.
- May encourage password reuse.
- Does not leverage the operating system by default.

---

## 17.4 Decision Relevance

A password-protected portable recovery export may be added later.

It is not the default runtime Vault protector.

---

# 18. Option F — Local Secret-Manager Server

## 18.1 Description

Install or bundle a dedicated local Vault server process.

---

## 18.2 Advantages

- Strong process isolation.
- Mature secret APIs may exist.
- Policy and leases may be built in.
- Separate memory boundary.
- Potential future team integration.
- Can support dynamic secrets.

---

## 18.3 Disadvantages

- Additional daemon.
- Installation and update complexity.
- Authentication complexity.
- High idle resource use.
- More support burden.
- May require network listener.
- May become a hidden infrastructure dependency.
- Disproportionate for a single-user desktop product.
- External server storage and backup become product responsibilities.
- Cross-platform packaging is more complex.

---

## 18.4 Estimated Adoption Cost

- **Initial implementation:** Very High
- **Operational complexity:** Very High
- **Migration difficulty:** Moderate
- **Replacement difficulty:** High

---

# 19. Option G — Cloud Secret Manager

## 19.1 Description

Store all secrets in a remote cloud provider.

---

## 19.2 Advantages

- Managed security controls.
- Rotation.
- central policy;
- team sharing;
- audit;
- and recovery.

---

## 19.3 Disadvantages

- Violates local-first default.
- Requires account and network.
- Creates provider dependency.
- Exposes secret metadata externally.
- Blocks offline use.
- Adds ongoing cost.
- Conflicts with personal local projects.
- Recovery depends on remote service.
- Multiple cloud providers complicate selection.

---

## 19.4 Decision

Rejected for core operation.

Optional enterprise integrations require a separate ADR.

---

# 20. Option H — Plain Configuration with File ACLs

## 20.1 Description

Store secrets in JSON, environment files or configuration protected only by filesystem permissions.

---

## 20.2 Advantages

- Very simple.
- Easy integration.
- Easy debugging.
- Easy backup.
- No cryptographic implementation.

---

## 20.3 Disadvantages

- Plaintext at rest.
- Easy accidental commit.
- Easy diagnostic leakage.
- Easy backup leakage.
- No record integrity.
- No scoped use.
- No rotation model.
- No trustworthy audit.
- Weak separation.
- Violates the Charter.

---

## 20.4 Decision

Rejected.

---

# 21. Option I — Dedicated Vault Process from the Beginning

## 21.1 Description

Run Vault cryptography and plaintext handling in a dedicated restricted process.

---

## 21.2 Advantages

- Separate memory boundary.
- Smaller trusted process.
- Potential privilege reduction.
- Runtime compromise may not immediately reveal the root key.
- Stronger secret-use broker.
- Easier process-level auditing.

---

## 21.3 Disadvantages

- Additional IPC.
- Additional process authentication.
- More failure modes.
- More deployment complexity.
- Same-user process compromise remains relevant.
- External secret delivery still crosses process boundaries.
- Small-team implementation cost is higher.
- The first Runtime prototype already requires many boundaries.
- Secure process hardening must be designed.

---

## 21.4 Decision Relevance

A dedicated Vault process is a planned extraction candidate.

The initial implementation may host the Vault in the trusted Runtime, as established by ADR-0003.

Extraction requires evidence and a separate ADR.

---

# 22. Comparison Matrix

Scores:

- **1** — poor
- **2** — weak
- **3** — acceptable
- **4** — strong
- **5** — excellent

| Criterion | Weight | App Vault + DPAPI | Credential Manager | DPAPI per Secret | Data Protection | Password Vault | Local Server | Cloud | Plain ACL | Vault Process |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Charter alignment | 5 | 5 | 4 | 4 | 4 | 4 | 4 | 1 | 1 | 5 |
| Local-first | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 1 | 5 | 5 |
| Windows integration | 5 | 5 | 5 | 5 | 5 | 3 | 4 | 3 | 4 | 5 |
| Cross-platform record model | 4 | 5 | 1 | 2 | 4 | 5 | 4 | 5 | 5 | 5 |
| Authenticated encryption | 5 | 5 | 4 | 5 | 5 | 5 | 5 | 5 | 1 | 5 |
| Secret-use mediation | 5 | 5 | 2 | 3 | 3 | 4 | 5 | 5 | 1 | 5 |
| Project scoping | 5 | 5 | 3 | 4 | 4 | 5 | 5 | 5 | 2 | 5 |
| Rotation | 5 | 5 | 3 | 3 | 5 | 4 | 5 | 5 | 1 | 5 |
| Recovery control | 4 | 4 | 2 | 2 | 4 | 4 | 4 | 5 | 2 | 4 |
| Offline operation | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 1 | 5 | 5 |
| Small-team fit | 5 | 4 | 5 | 5 | 4 | 3 | 1 | 3 | 5 | 2 |
| Metadata flexibility | 4 | 5 | 2 | 4 | 4 | 5 | 5 | 5 | 4 | 5 |
| Backup control | 4 | 4 | 2 | 2 | 4 | 5 | 4 | 5 | 5 | 4 |
| Supply-chain control | 3 | 5 | 5 | 5 | 4 | 5 | 2 | 2 | 5 | 5 |
| Replacement cost | 3 | 4 | 2 | 2 | 3 | 4 | 2 | 2 | 3 | 4 |
| **Indicative weighted total** |  | **421** | **296** | **326** | **358** | **363** | **347** | **303** | **232** | **399** |

The application-managed Vault with a DPAPI-protected root key provides the strongest overall fit for the first implementation.

---

# 23. Decision

Opure will provisionally implement:

> **A dedicated application-managed Secrets Vault using a random 256-bit Vault Root Key protected by Windows DPAPI Current User, HKDF-SHA-256 per-record key derivation and AES-256-GCM per-secret authenticated encryption.**

The Vault will:

- store encrypted record files outside ordinary SQLite databases;
- store only non-secret metadata separately;
- expose secret-use operations through the Runtime;
- bind use to policy, purpose and destination;
- minimise plaintext lifetime;
- support rotation and revocation;
- and remain transport and platform neutral at the consumer contract.

This decision does not approve:

- raw secret retrieval by the Desktop;
- secret values in SQLite;
- Local Machine DPAPI scope;
- secret values in command-line arguments;
- secret values in logs or Trust Centre;
- Plugin Host direct Vault access;
- arbitrary secret export;
- cloud backup;
- or a claim of protection against a compromised Windows user account.

This decision is:

- [ ] Permanent until superseded
- [x] Provisional pending prototype evidence
- [ ] Experimental only
- [ ] Limited to API keys
- [ ] Limited to Windows permanently

---

# 24. Rationale

The selected architecture separates:

- the portable secret-record format;
- the application cryptographic key hierarchy;
- and the operating-system root-key protector.

This allows:

- Windows DPAPI today;
- future native key protection on Linux and macOS;
- record-level rotation;
- application-level scope and metadata;
- and brokered use.

Using Credential Manager as the whole Vault would make the model Windows specific and constrain record size and lifecycle.

Protecting every record directly through DPAPI would also couple every stored record to Windows.

A password-derived Vault would improve portability but impose avoidable password burden for normal local use.

A dedicated secret server or cloud manager would add disproportionate operational complexity.

---

# 25. Vault Ownership

The Secrets Vault is one authoritative Runtime service.

It owns:

- Vault state;
- keyring;
- encrypted record store;
- secret metadata;
- secret-use leases;
- secret-use history references;
- rotation;
- revocation;
- backup;
- restore;
- integrity;
- and recovery.

No other service opens the Vault's files directly.

---

# 26. Initial Process Placement

The initial Vault service may run inside `Opure.Runtime`.

It must be isolated as a strict internal module.

It should have:

- a narrow public contract;
- no public repository access;
- no broad serialization;
- no Desktop-returning plaintext method;
- and architecture tests.

---

## 26.1 Future Extraction

The Vault should move to a dedicated process when evidence demonstrates material benefit from:

- separate memory boundary;
- reduced privileges;
- independent restart;
- hardware-backed key use;
- or enterprise policy.

Extraction requires a separate ADR.

---

# 27. Key Hierarchy

The initial key hierarchy is:

```text
Windows User Credentials
        ↓
DPAPI Current User
        ↓
Protected Vault Root Key Envelope
        ↓
Vault Root Key Version
        ↓
HKDF-SHA-256
        ↓
Per-Secret-Version Record Key
        ↓
AES-256-GCM
        ↓
Encrypted Secret Record
```

---

# 28. Vault Root Key

A Vault Root Key is:

- 32 random bytes;
- generated through a cryptographically strong random generator;
- assigned a stable key-version identifier;
- protected at rest through the platform key protector;
- loaded only into trusted Vault memory;
- and zeroed when no longer required where practical.

---

## 28.1 Root-Key Generation

Use `RandomNumberGenerator.Fill` or an equivalent approved system cryptographic random generator.

No:

- timestamp;
- GUID alone;
- password;
- project identifier;
- device identifier;
- or pseudo-random application generator

may be used as the root key.

---

## 28.2 Root-Key Storage

The plaintext root key must never be written to disk.

Only the platform-protected envelope is stored.

---

## 28.3 Root-Key Scope

The Windows envelope uses:

```text
DataProtectionScope.CurrentUser
```

Local Machine scope is prohibited for ordinary Vault keys.

---

## 28.4 DPAPI Optional Entropy

A non-secret, deterministic purpose binding may be used as DPAPI optional entropy.

It may include:

- Opure application identity;
- profile identifier;
- Vault format version;
- and key-version identity.

The entropy is additional binding.

It is not the encryption key and must not be treated as a secret.

Changing it without migration makes the envelope undecryptable.

---

## 28.5 DPAPI Description

If a DPAPI description is used, it must not contain:

- project name;
- provider name;
- user name;
- secret name;
- or other sensitive data.

---

# 29. Keyring

The Vault maintains a keyring containing:

- current root-key version;
- previous root-key versions required for migration;
- platform-protected envelopes;
- creation time;
- activation time;
- retirement state;
- algorithm suite;
- protector kind;
- and integrity metadata.

The keyring contains no plaintext root key at rest.

---

## 29.1 Keyring Format

The keyring should use a versioned, canonical binary or JSON envelope.

Sensitive contents remain DPAPI protected.

The outer format is not secret.

It must be integrity checked through protected envelope validation and application-level manifest checks.

---

## 29.2 Keyring Backup

Backing up the keyring alone does not guarantee restore on another computer.

The backup manifest must state the protector dependency.

---

# 30. Per-Record Key Derivation

Every secret version receives a distinct encryption key.

Proposed derivation:

```text
record_key = HKDF-SHA-256(
    input_key_material = vault_root_key,
    salt = random_record_salt,
    info = canonical_record_context,
    output_length = 32
)
```

---

## 30.1 Record Salt

The salt should be:

- random;
- non-secret;
- 32 bytes;
- stored in the encrypted-record header;
- and unique per secret version.

---

## 30.2 Record Context

The HKDF context should include stable canonical values such as:

- application identifier;
- Vault format version;
- root-key version;
- secret identifier;
- secret version;
- project scope;
- secret type;
- and `opure-vault-record-key`.

---

## 30.3 Canonical Encoding

Context encoding must be:

- unambiguous;
- length delimited;
- versioned;
- and independently tested.

Simple string concatenation is prohibited.

---

# 31. Record Encryption

Each secret record uses:

- AES-256-GCM;
- 32-byte derived key;
- 12-byte random nonce;
- 16-byte authentication tag;
- plaintext secret bytes;
- and canonical associated data.

---

## 31.1 Required Tag Size

The implementation must construct `AesGcm` with an explicit 16-byte tag requirement.

Truncated tags are prohibited.

---

## 31.2 Nonce

Each encryption under one derived record key must use one unique nonce.

Because every secret version has a separately derived key, a fresh random 12-byte nonce remains required.

Nonce reuse is a Critical defect.

---

## 31.3 Associated Data

Authenticated additional data should bind:

- magic value;
- record format version;
- algorithm suite;
- root-key version;
- secret identifier;
- secret version;
- scope kind;
- scope identifier;
- secret type;
- creation generation;
- record salt;
- nonce;
- ciphertext length;
- and immutable policy flags.

---

## 31.4 Mutable Metadata

Mutable display metadata such as:

- label;
- description;
- last-used time;
- and tags

should remain outside the authenticated secret record or require a new record version.

---

## 31.5 Authentication Failure

If AES-GCM authentication fails:

- no plaintext is returned;
- the record is marked corrupt or tampered;
- secret use is denied;
- the original record is preserved;
- and Recovery Required may be raised.

---

# 32. Cryptographic Suite

The initial suite is:

```text
OPURE-VAULT-1
Root key: 256-bit random
Root protector: Windows DPAPI Current User
KDF: HKDF-SHA-256
Record cipher: AES-256-GCM
Nonce: 12 bytes
Tag: 16 bytes
Record salt: 32 bytes
```

Every record stores a suite identifier.

---

## 32.1 Algorithm Agility

The format must permit future:

- key protector;
- KDF;
- cipher;
- nonce size;
- tag size;
- and record format

without ambiguous interpretation.

---

## 32.2 No Automatic Downgrade

A record using an unknown or retired algorithm must fail closed.

The Vault must not silently decrypt and rewrite through a weaker suite.

---

# 33. Secret Record Store

Encrypted secret records should live in a dedicated directory.

Conceptual layout:

```text
vault/
├── keyring/
│   ├── keyring.manifest
│   └── keys/
├── records/
│   ├── 00/
│   ├── 01/
│   └── ...
├── journal/
├── recovery/
├── backups/
└── metadata/
```

The final layout is an implementation detail.

The separation is architectural.

---

## 33.1 Record Filename

A record filename should use:

- random or stable secret identifier;
- version;
- and format suffix.

It must not include:

- provider name;
- project name;
- account name;
- secret label;
- or external destination.

---

## 33.2 Directory Permissions

Vault directories must be restricted to the current Windows user and required system identities.

Unexpected broad permissions are a Critical security failure.

---

## 33.3 Junctions and Links

Vault paths must reject:

- symbolic links;
- junction substitution;
- hard-link attacks;
- alternate stream misuse;
- and path escape.

---

## 33.4 Record Atomicity

A record update should use:

1. restricted temporary file;
2. complete encryption;
3. flush according to Critical durability;
4. integrity validation;
5. atomic replacement where supported;
6. metadata commit;
7. and journal completion.

---

# 34. Metadata Store

Non-secret Vault metadata may be stored in a service-owned SQLite database.

Metadata may include:

- secret identifier;
- display label;
- type;
- project scope;
- provider scope;
- plugin scope;
- destination class;
- purpose list;
- created time;
- updated time;
- last-used time;
- expiration;
- current record version;
- root-key version;
- state;
- and policy references.

---

## 34.1 Metadata Prohibition

Metadata must not contain:

- secret value;
- ciphertext;
- private key material;
- password hint that reveals value;
- token prefix unless explicitly safe;
- authentication proof;
- or recovery password.

---

## 34.2 Sensitive Metadata

Account names, usernames and service hosts may be Sensitive.

They require:

- minimisation;
- redaction;
- and display policy.

---

# 35. Secret Identity

Every secret has a stable opaque identifier.

The identifier must not encode:

- value;
- provider;
- project;
- user;
- or account.

---

## 35.1 Secret Version

Every value change creates a new immutable secret version.

The current metadata record points to the active version.

---

## 35.2 No In-Place Ciphertext Mutation

An encrypted secret record is immutable after activation.

Rotation or update writes a new record.

---

# 36. Secret Types

Initial types may include:

- API Token;
- Password;
- Username and Password;
- Private Key;
- Certificate Password;
- OAuth Refresh Token;
- OAuth Access Token;
- Connection String;
- Signing Key;
- SSH Key;
- Proxy Credential;
- Package Registry Token;
- Git Credential;
- Generic Binary Secret;
- and Generic Text Secret.

---

## 36.1 Type-Specific Validation

Types may define:

- maximum size;
- encoding;
- expected fields;
- allowed consumers;
- exportability;
- rotation guidance;
- and masking behaviour.

---

## 36.2 No Secret Guessing

Opure should not infer a stored secret's type solely from its value after entry.

The developer or integration declares it.

---

# 37. Secret Scope

A secret may be scoped to:

- User Profile;
- Opure Profile;
- Project;
- Provider;
- Provider Account;
- Plugin;
- MCP Server;
- Repository;
- Package Registry;
- Signing Identity;
- or explicit custom capability.

---

## 37.1 Scope Narrowing

Prefer the narrowest useful scope.

A project secret should not automatically be usable by every project.

---

## 37.2 Cross-Project Use

Cross-project use requires:

- an explicitly broader secret scope;
- or an approval that references the secret and destination.

---

## 37.3 Scope Change

Broadening scope is a protected action.

Narrowing scope may invalidate leases and integrations.

---

# 38. Secret State

Secret states include:

- Active;
- Expired;
- Revoked;
- Disabled;
- Rotation Required;
- Corrupt;
- Unavailable;
- Recovery Required;
- Pending Deletion;
- Deleted;
- and Imported Unverified.

---

# 39. Vault State

Vault states include:

- Not Initialised;
- Initialising;
- Locked;
- Unlocking;
- Available;
- Available with Warnings;
- Rotation in Progress;
- Recovery Required;
- Unavailable;
- and Shutting Down.

---

## 39.1 Available

Available means:

- root key unprotected;
- keyring valid;
- metadata available;
- record store available;
- and core integrity checks passed.

---

## 39.2 Locked

Locked means plaintext root keys are not retained in the Vault service's ordinary working memory.

Because DPAPI Current User may unlock again without a user-presence prompt, Locked is a memory-reduction and operation-control state.

It is not equivalent to a hardware-backed user-presence lock.

---

# 40. Vault Initialisation

Vault creation should:

1. create restricted directories;
2. create metadata store;
3. generate root key;
4. create key version;
5. protect root key with DPAPI Current User;
6. write keyring atomically;
7. verify unprotect;
8. zero test plaintext buffers;
9. create Vault identity;
10. create first integrity manifest;
11. and record Trust Centre event without values.

---

# 41. Vault Unlock

Unlock should:

1. validate Vault paths and permissions;
2. read keyring;
3. validate format;
4. unprotect current root key;
5. verify key check record;
6. load only required key versions;
7. zero intermediate buffers;
8. enter Available;
9. and record safe outcome.

---

## 41.1 Unlock Failure

Possible causes include:

- Windows profile unavailable;
- wrong user;
- different machine;
- corrupt DPAPI blob;
- keyring tamper;
- permission error;
- or unsupported format.

The Vault must not recreate a new Vault silently.

---

# 42. Vault Lock

Lock should:

- deny new secret uses;
- expire leases;
- cancel safe pending secret operations;
- zero root-key buffers where practical;
- release cryptographic objects;
- clear caches;
- and enter Locked.

---

## 42.1 Manual Lock

The developer may request manual lock.

The UI must explain its limitation under DPAPI Current User.

---

## 42.2 Automatic Lock

Automatic lock after inactivity is optional and deferred to product policy.

It should not create repeated model or build failures without clear visibility.

---

# 43. Create Secret

Secret creation should:

1. authenticate caller;
2. evaluate policy;
3. validate scope and type;
4. receive secret through protected input path;
5. scan for accidental surrounding whitespace where type appropriate;
6. create secret identifier;
7. create version;
8. derive record key;
9. encrypt;
10. validate decrypt in memory;
11. atomically store record;
12. commit metadata;
13. zero plaintext and derived key;
14. and record Trust Centre metadata without value.

---

## 43.1 Secret Entry UI

The Desktop may collect a secret in a masked input.

The value should:

- avoid clipboard by default;
- avoid normal view-model persistence;
- avoid session restore;
- avoid analytics;
- avoid logs;
- and be transferred immediately through a dedicated secret-entry call.

---

## 43.2 Desktop Value Lifetime

The Desktop should clear the input after the Runtime confirms secure storage.

It should not retain the value for later display.

---

# 44. Read Secret

General unrestricted plaintext read is prohibited for ordinary consumers.

The Vault may expose an internal privileged diagnostic or export operation only through a high-risk protected workflow.

---

## 44.1 Reveal to Developer

A future Reveal action may be supported.

It requires:

- explicit developer action;
- re-authentication or user presence where available;
- short display timeout;
- no screenshots where technically enforceable only as best effort;
- clipboard warning;
- Trust Centre record;
- and no automatic persistence.

Reveal is not required for version 1.0.

---

# 45. Update Secret

Updating a secret creates a new version.

The old version remains encrypted until:

- active leases expire;
- rollback window passes;
- and retention policy permits deletion.

---

## 45.1 Version Activation

Activation should be atomic at metadata level.

A consumer sees either:

- old active version;
- or new active version.

It must not see a partially written record.

---

# 46. Delete Secret

Deletion should:

1. deny new leases;
2. revoke active leases where possible;
3. mark Pending Deletion;
4. remove active metadata reference;
5. delete encrypted records after policy window;
6. remove orphaned files;
7. and record completion.

---

## 46.1 Secure Deletion Limitation

Opure cannot guarantee forensic erasure from:

- SSD wear levelling;
- filesystem snapshots;
- backups;
- or storage-controller caches.

It provides logical deletion and best-effort cleanup.

---

# 47. Secret-Use Model

The primary API is:

> **Use this secret for this approved purpose and destination.**

It is not:

> **Give this caller the plaintext forever.**

---

## 47.1 Secret-Use Request

A request should include:

- secret identifier;
- caller identity;
- caller class;
- project;
- purpose;
- destination;
- operation;
- expected consumer;
- requested lifetime;
- and correlation.

---

## 47.2 Evaluation

The Vault and Policy Engine evaluate:

- secret state;
- secret scope;
- caller capability;
- project policy;
- destination;
- approval;
- purpose allowlist;
- expiry;
- and risk.

---

## 47.3 Secret-Use Result

Possible outcomes:

- Denied;
- Approval Required;
- Lease Issued;
- Secret Applied Internally;
- Destination Operation Started;
- Expired;
- Revoked;
- Unavailable;
- or Recovery Required.

---

# 48. Secret Lease

A secret lease is a short-lived authority to use one secret for one bounded purpose.

A lease includes:

- lease identifier;
- secret identifier;
- secret version;
- caller;
- purpose;
- destination;
- issued time;
- expiry;
- maximum uses;
- operation identity;
- and revocation state.

It does not include the secret value in ordinary persistence.

---

## 48.1 Lease Duration

Leases should be as short as practical.

Examples:

- one provider request;
- one MCP session;
- one command process;
- one Git operation;
- or one plugin invocation.

---

## 48.2 Lease Renewal

Renewal requires re-evaluation.

A long-running operation must not retain a perpetual secret lease.

---

## 48.3 Lease Revocation

Revocation prevents future use.

It may not erase a value already delivered to an external process.

The UI must explain this limitation.

---

# 49. Secret Consumer API

Preferred internal patterns include:

- callback with scoped plaintext bytes;
- adapter operation executed inside the Vault trust boundary;
- secure pipe delivery;
- destination-specific credential object;
- and one-time injection token.

---

## 49.1 Callback Pattern

Conceptual pattern:

```text
UseSecretAsync(
    secret_reference,
    purpose,
    destination,
    callback(secret_material)
)
```

The callback:

- executes in a narrow trusted adapter;
- cannot retain the reference legally;
- receives bounded material;
- and returns before zeroing.

This is an architectural contract, not a guarantee that malicious trusted code cannot copy the value.

---

## 49.2 Secret Material Type

Use a dedicated non-serialisable secret-material type.

It should:

- avoid `ToString`;
- avoid equality output;
- avoid JSON serialization;
- avoid logging;
- expose bytes or characters only through a bounded callback;
- and support disposal.

---

## 49.3 No Generic Object

Secret material must not be placed in:

- generic dictionaries;
- dependency-injection options objects;
- configuration trees;
- or ordinary command contracts.

---

# 50. Provider Adapter Use

An AI-provider adapter may request:

- provider secret;
- for one provider endpoint;
- for one request or bounded client session.

The AI Router and Network Gateway remain responsible for:

- provider selection;
- destination;
- cloud policy;
- and request visibility.

---

## 50.1 HTTP Authentication

Preferred delivery:

- construct the authentication header within the trusted adapter immediately before sending;
- avoid logging headers;
- dispose request objects;
- and avoid retaining default headers on long-lived global clients when broader reuse could occur.

---

## 50.2 Provider Failure

Provider error bodies must not cause request headers or secrets to be logged.

---

# 51. MCP Secret Use

An MCP server may request a declared credential capability.

The MCP Gateway should:

- identify server;
- identify capability;
- identify destination;
- evaluate policy;
- request secret lease;
- and deliver through the approved server mechanism.

The server does not receive Vault query access.

---

# 52. Plugin Secret Use

A Plugin Host may request use of a developer-approved secret capability.

The Plugin Host must declare:

- secret purpose;
- destination;
- required type;
- project scope;
- and whether plaintext delivery is unavoidable.

---

## 52.1 Plugin Default

Default plugin behaviour is secret use through a brokered operation.

Raw plaintext delivery to a Plugin Host is denied by default.

---

## 52.2 Plugin Plaintext Exception

If a plugin cannot operate without plaintext:

- the permission must say so clearly;
- the secret scope must include that plugin;
- approval must identify the plugin and destination;
- the host receives a one-use short-lived delivery;
- and the Trust Centre records the disclosure without value.

---

# 53. Git Credential Use

Preferred approaches include:

- Git credential helper integration;
- standard input;
- secure askpass helper;
- or bounded environment delivery.

Do not place tokens directly in:

- repository URLs;
- command-line arguments;
- or logs.

---

# 54. Package Manager Credential Use

Use provider-specific secure mechanisms where available, such as:

- credential helpers;
- temporary authenticated configuration;
- standard input;
- or restricted environment.

Any temporary configuration file must be:

- restrictive;
- scoped;
- short lived;
- secret scanned after use;
- and deleted best effort.

---

# 55. External Command Secret Injection

Preferred order:

1. destination-specific credential helper;
2. inherited secure pipe or handle;
3. standard input;
4. restricted temporary file;
5. environment variable;
6. command-line argument only when no safer supported mechanism exists and the developer explicitly approves.

---

## 55.1 Command-Line Arguments

Command-line secret injection is denied by default.

It may expose secrets through:

- process inspection;
- crash reports;
- history;
- diagnostics;
- and child-process inheritance.

---

## 55.2 Environment Variables

Environment variables are allowed only when:

- the tool requires them;
- the child environment is constructed deliberately;
- unrelated variables are removed;
- child inheritance is controlled;
- value is not logged;
- and process tree is supervised.

---

## 55.3 Standard Input

Standard input is preferred when the tool supports it and does not echo or log the secret.

---

## 55.4 Temporary File

A secret temporary file requires:

- dedicated restricted directory;
- random filename;
- restrictive ACL;
- no project-tree placement;
- no sync folder;
- short lifetime;
- supervised consumer;
- deletion;
- and recovery cleanup.

---

# 56. Secret Injection to Child Trees

A secret must not be inherited by arbitrary descendant processes.

The command plan should know whether:

- only the direct child;
- selected descendants;
- or a credential helper

requires access.

If the operating system cannot enforce the desired scope, the risk must be shown.

---

# 57. Memory Handling

Plaintext secrets inevitably exist in memory during use.

The implementation should minimise:

- lifetime;
- copies;
- scope;
- allocation;
- and process count.

---

## 57.1 Mutable Buffers

Prefer:

- `byte[]`;
- `char[]`;
- stack or pooled buffers where safe;
- `Span<byte>`;
- and dedicated secret types.

Use `CryptographicOperations.ZeroMemory` for mutable byte buffers after use.

---

## 57.2 Strings

Avoid converting secret values to `string`.

Strings are immutable and cannot be reliably zeroed.

Where an external API requires a string:

- create it as late as possible;
- keep it in the narrowest scope;
- do not intern it;
- do not cache it;
- and release references promptly.

Opure must not claim that garbage collection erases it immediately.

---

## 57.3 Buffer Pools

General shared buffer pools must not receive plaintext secrets unless:

- buffers are zeroed before return;
- pool behaviour is tested;
- and no snapshot or diagnostic capture includes them.

Dedicated non-pooled buffers may be safer initially.

---

## 57.4 Pinning

Pinning may reduce movement but can increase memory pressure.

It is not a complete defence.

Use only when required by native APIs.

---

## 57.5 SecureString

`SecureString` is not selected as the Vault's primary secret representation.

The architecture relies on bounded mutable buffers and narrow use instead.

---

# 58. Root Key in Memory

The root key may remain in the trusted Vault module while Available.

Controls include:

- dedicated buffer;
- no serialization;
- no logging;
- no diagnostics;
- no ordinary heap copies where avoidable;
- zero on lock and shutdown;
- and restricted code path.

---

## 58.1 Root-Key Cache

The Vault should not cache derived record keys.

Derive them per operation and zero them after use.

---

## 58.2 Multiple Root Keys

During rotation, only required current and previous key versions should be loaded.

---

# 59. DPAPI Protection

Windows DPAPI protects the root key envelope.

---

## 59.1 Current User

Use Current User scope.

This binds normal decryption to the user's logon credentials and usually the same computer.

---

## 59.2 Local Machine

Local Machine scope is prohibited for ordinary Vault keys because it broadens decryption to other users on the same machine.

---

## 59.3 User Profile Dependency

If the user profile is unavailable or damaged, DPAPI decryption may fail.

The Vault must surface:

- exact safe error;
- recovery options;
- and backup limitations.

---

## 59.4 Impersonation

The Vault must not perform DPAPI operations under arbitrary impersonation.

If future service accounts are introduced, the protector model requires a separate ADR.

---

# 60. Windows Credential Manager

Credential Manager is not the primary Vault.

Potential limited uses require a separate decision.

The Vault must not silently duplicate secrets into Credential Manager.

---

# 61. Windows Hello and User Presence

Windows Hello or another user-presence control may later protect:

- secret reveal;
- portable export;
- signing operation;
- or high-risk use.

It is not required for ordinary version 1.0 Vault unlock.

A separate ADR is required.

---

# 62. Vault Metadata Visibility

The Desktop may display:

- label;
- type;
- scope;
- provider;
- destination class;
- state;
- expiry;
- last use;
- and rotation status.

It must not display:

- value;
- ciphertext;
- tag;
- nonce;
- key material;
- or authentication proof.

---

# 63. Secret Masking

Masked displays should not reveal exact secret length by default.

Possible display:

```text
••••••••
```

not a number of characters equal to the real value.

---

# 64. Clipboard

Copying a secret is denied by default.

A future copy action requires:

- explicit reveal permission;
- warning;
- short clipboard clear attempt;
- no history guarantee claim;
- and Trust Centre record.

Clipboard clearing cannot guarantee removal from all clipboard-history or synchronisation services.

---

# 65. Secret Import

Secret import should support:

- direct entry;
- selected environment variable;
- selected credential helper;
- selected file;
- and provider-specific flow.

---

## 65.1 Import Preview

Before import, show:

- source type;
- destination Vault scope;
- whether the source remains;
- and cleanup guidance.

Do not display the value.

---

## 65.2 Environment Import

Importing an environment variable does not remove it from:

- parent process;
- shell;
- profile;
- or operating-system configuration.

Opure should explain this.

---

## 65.3 File Import

Private-key import should read the file through a bounded secure path.

The source file remains unless the developer explicitly requests deletion.

---

# 66. Secret Export

Secret export is not required for ordinary operation.

A future export requires a high-risk workflow.

---

## 66.1 Portable Export

A portable export should use:

- explicit developer password or recipient public key;
- modern password KDF or public-key encryption;
- authenticated encryption;
- manifest;
- expiry;
- and warnings.

It must not rely on the Windows DPAPI envelope alone.

---

## 66.2 Plaintext Export

Plaintext export should be denied by default.

Any future support requires explicit risk acceptance.

---

# 67. Backup

Vault backups should include only:

- encrypted record files;
- protected keyring;
- non-secret metadata backup;
- journal state;
- and manifest.

---

## 67.1 Same-Machine Backup

A normal backup may restore under the same Windows user profile and machine when DPAPI remains valid.

This is an expectation that must be tested, not guaranteed universally.

---

## 67.2 Cross-Machine Backup

A normal backup is not considered portable.

The UI must state this before the backup is treated as complete recovery protection.

---

## 67.3 Backup Manifest

The manifest should include:

- Vault identity;
- format version;
- key versions;
- protector type;
- machine-bound warning;
- record count;
- metadata version;
- hashes;
- creation time;
- and verification result.

---

## 67.4 Backup Security

An encrypted Vault backup still contains sensitive ciphertext and metadata.

It must use restrictive permissions and clear classification.

---

# 68. Restore

Restore should occur through Recovery Mode.

The process should:

1. validate manifest;
2. verify file hashes;
3. stage files;
4. verify metadata;
5. test DPAPI unprotect without replacing the current Vault;
6. verify key check;
7. verify sample or all record authentication;
8. preserve current Vault;
9. atomically activate;
10. and record outcome.

---

## 68.1 Wrong Machine or User

If DPAPI unprotect fails because the backup is not usable on the current user or machine:

- do not overwrite current Vault;
- state that normal backup is non-portable;
- offer portable recovery import if available;
- and preserve the backup.

---

# 69. Key Rotation

Root-key rotation creates a new root-key version.

New secret versions use the new key.

Existing records are re-encrypted through a controlled migration.

---

## 69.1 Rotation Flow

1. create new root key;
2. protect new root key;
3. verify envelope;
4. add keyring version as Pending;
5. create rotation journal;
6. re-encrypt records to new versions;
7. validate each new record;
8. atomically update metadata;
9. mark new key Active;
10. retain old key during rollback window;
11. complete integrity scan;
12. retire old key;
13. delete old protected envelope when safe;
14. and record Trust Centre event.

---

## 69.2 Lazy Rotation

Lazy rotation may reduce startup cost but retains old keys longer.

It is not the initial preferred strategy for security-triggered rotation.

---

## 69.3 Interrupted Rotation

The journal must identify:

- old key;
- new key;
- completed records;
- pending records;
- metadata state;
- and rollback status.

The Vault must remain able to decrypt both active versions during recovery.

---

# 70. Secret Rotation

A secret-value rotation creates a new secret version.

The integration may test the new value before activation.

---

## 70.1 Validation

Validation may contact an external destination only under policy and approval.

A valid external result does not reveal the value.

---

## 70.2 Automatic Rotation

Automatic provider-managed rotation is deferred.

It requires provider-specific capability and rollback design.

---

# 71. Revocation

Revocation immediately denies new leases.

The Vault should notify:

- AI Router;
- MCP Gateway;
- Plugin Manager;
- Repository Service;
- Build Manager;
- and other registered consumers

that cached or session-bound use must stop.

---

## 71.1 External Sessions

Revocation cannot always invalidate a token already accepted by an external provider.

Provider-side revocation may be required.

---

# 72. Expiry

Secrets may have:

- no known expiry;
- fixed expiry;
- provider-reported expiry;
- or rotation due date.

Expired secrets are denied unless a specific recovery exception is approved.

---

# 73. Secret Validation

A provider integration may validate:

- format locally;
- expiry locally;
- and access remotely.

Remote validation requires:

- destination visibility;
- policy;
- and no hidden network request.

---

# 74. Secret Detection

Secret detection complements the Vault.

It should identify likely secrets in:

- project files;
- generated patches;
- command output;
- prompts;
- context;
- diagnostic exports;
- and outbound payloads.

---

## 74.1 Detection Does Not Store Value

Detection systems should store:

- detector type;
- location;
- fingerprint;
- confidence;
- and remediation

without persisting the full value.

---

## 74.2 Fingerprint

A secret fingerprint may use a keyed or one-way construction.

It must not permit practical recovery of the value.

The exact design requires security review.

---

# 75. Generated Patch Scanning

Before patch approval and application, scan for:

- newly introduced secrets;
- credentials;
- private keys;
- tokens;
- and sensitive configuration.

A finding should block or require explicit remediation.

---

# 76. Git Protection

Before staging or commit preparation, scan:

- changed files;
- untracked sensitive files;
- and high-risk file names.

Vault records never enter the project tree.

---

# 77. Secret References

Configuration should store a reference such as:

```text
secret://<opaque-secret-id>
```

The exact syntax is an implementation decision.

The reference:

- contains no value;
- grants no authority;
- and resolves only through the Vault.

---

## 77.1 Reference in Project Files

Project files should not contain Opure secret references unless the project intentionally uses them and the developer accepts portability implications.

Opure internal integration configuration should live outside the source repository.

---

# 78. Integration Configuration

An integration record may contain:

- provider;
- endpoint;
- account label;
- secret reference;
- project scope;
- cloud policy;
- and capability.

The secret value remains in the Vault.

---

# 79. Cache Policy

Plaintext secret caching is prohibited by default.

Approved caches must be:

- bounded;
- in memory only;
- short lived;
- tied to lease;
- zeroed;
- and invalidated on revocation.

---

# 80. OAuth Tokens

OAuth access and refresh tokens should be separate secret records or a structured encrypted secret type.

---

## 80.1 Access Token

Access tokens should use short leases and expiry.

---

## 80.2 Refresh Token

Refresh tokens require stricter scope.

Token refresh is an external state-changing operation and must be visible.

---

## 80.3 Token Rotation

If a provider rotates the refresh token, the Vault update must be atomic and recoverable.

---

# 81. Private Keys

Private keys may be:

- raw key bytes;
- encrypted PEM;
- provider-managed handle;
- hardware-backed reference;
- or certificate-store reference.

---

## 81.1 Hardware-Backed Key

Where a key can remain non-exportable in Windows CNG or hardware, Opure should prefer a handle or provider operation rather than importing plaintext.

This requires a separate signing or hardware-key ADR.

---

# 82. Certificates

Certificate metadata may remain outside the Vault.

Private key material and passphrases remain protected.

Windows Certificate Store integration is deferred.

---

# 83. Logging

Vault logs may include:

- operation kind;
- caller class;
- secret identifier;
- secret type;
- scope class;
- purpose;
- destination class;
- result;
- duration;
- and stable error.

---

## 83.1 Logging Prohibitions

Never log:

- secret value;
- ciphertext;
- derived key;
- root key;
- nonce and tag combination with full record path where unnecessary;
- DPAPI blob;
- authentication header;
- raw private key;
- or secret-bearing exception.

---

## 83.2 Secret Identifier in Logs

Secret identifiers are pseudonymous and should be truncated or tokenised where full identity is unnecessary.

---

# 84. Tracing

Vault traces may include:

- `vault.create`;
- `vault.use`;
- `vault.rotate`;
- `vault.revoke`;
- `vault.backup`;
- `vault.restore`;
- and `vault.integrity`.

No secret material appears in spans.

---

# 85. Metrics

Vault metrics may include:

- active secrets;
- expired secrets;
- rotation due;
- lease count;
- denied use;
- decryption failures;
- integrity failures;
- operation latency;
- and Vault state.

Metric labels must not include secret identifier or project identifier by default.

---

# 86. Trust Centre

Trust records should include significant actions such as:

- secret created;
- secret scope broadened;
- secret used for external destination;
- plugin received plaintext exception;
- secret revealed;
- secret exported;
- secret revoked;
- key rotation;
- backup;
- restore;
- and integrity failure.

---

## 86.1 Trust Prohibition

Trust records must not contain:

- secret value;
- ciphertext;
- token prefix;
- password hint;
- private key;
- or DPAPI envelope.

---

# 87. Diagnostics Export

Diagnostic export must exclude Vault files and secret records by default.

It may include:

- Vault state;
- format version;
- record counts;
- root-key version count;
- safe error codes;
- permission state;
- and integrity summary.

---

# 88. Crash Diagnostics

Crash dumps may contain root keys or plaintext secrets.

Vault-related crash policy should:

- disable automatic dumps;
- warn before dump creation;
- use restrictive permissions;
- shorten retention;
- and never upload automatically.

---

# 89. Serialization

Secret material types must not be serialisable by:

- JSON;
- Protocol Buffers;
- ordinary IPC;
- logging;
- or diagnostics.

Only explicit secret-entry or secret-delivery protocols may carry plaintext.

---

# 90. IPC

ADR-0004 local IPC should define separate protected methods for:

- secret entry;
- secret-use request;
- secret metadata query;
- and secret-state notifications.

---

## 90.1 Desktop IPC

The Desktop may send secret value only during:

- create;
- update;
- import;
- or approved reveal re-entry.

The Runtime must not send the value back in normal responses.

---

## 90.2 Plugin IPC

Plugin IPC uses lease and capability contracts.

Raw secret delivery is exceptional and clearly typed.

---

## 90.3 Worker IPC

Trusted workers should not receive secrets unless the worker task explicitly requires them.

Secret-bearing workers should use:

- one task per process where practical;
- no logs;
- no dumps;
- short lifetime;
- and strict cleanup.

---

# 91. Policy Integration

The Policy Engine determines whether secret use is allowed.

Policy dimensions include:

- project cloud mode;
- secret scope;
- caller;
- destination;
- provider;
- plugin;
- MCP server;
- command risk;
- and data classification.

---

# 92. Approval Integration

Approval may be required for:

- first use at a destination;
- cloud transmission;
- scope broadening;
- plugin plaintext delivery;
- reveal;
- export;
- deletion;
- rotation failure override;
- and recovery action.

---

# 93. Network Integration

The Network Gateway should receive:

- secret-use authorisation result;
- destination;
- provider;
- and operation identity.

It should not receive a reusable unrestricted Vault credential.

---

# 94. Secret-Use History

Use history should store:

- secret identifier;
- secret version;
- caller;
- purpose;
- destination class;
- operation;
- decision;
- time;
- and result.

It stores no value.

---

# 95. Record Format

A conceptual encrypted record contains:

```text
magic
format_version
suite_id
vault_id
root_key_version
secret_id
secret_version
scope_kind
scope_id
secret_type
record_salt
nonce
ciphertext_length
immutable_flags
ciphertext
authentication_tag
```

The final binary layout must be canonical and independently reviewed.

---

## 95.1 Bounds

Every variable-length field has a strict maximum.

A malformed record must fail before unbounded allocation.

---

## 95.2 Endianness

The format must define endianness explicitly.

---

## 95.3 Parsing

The parser must:

- validate magic;
- validate version;
- validate suite;
- validate lengths;
- reject duplicate fields;
- reject trailing data unless specified;
- verify metadata match;
- derive key;
- authenticate;
- and only then release plaintext.

---

# 96. Key Check Record

The Vault should maintain an encrypted key-check record.

It verifies that an unprotected root key corresponds to the Vault without exposing a secret.

---

## 96.1 Key Check Content

The plaintext may contain:

- random Vault verification bytes;
- Vault identity;
- format version;
- and checksum.

It is not a user secret.

---

# 97. Integrity Manifest

A Vault manifest may record:

- keyring hash;
- metadata generation;
- active secret count;
- record inventory hash;
- journal state;
- and clean shutdown.

It must not be treated as a substitute for per-record authentication.

---

# 98. Mutation Journal

Vault mutations require a durable journal.

Mutation types include:

- create;
- update;
- delete;
- rotation;
- metadata migration;
- backup;
- restore;
- and record migration.

---

## 98.1 Journal Contents

Journal entries contain:

- mutation identifier;
- secret identifier;
- source version;
- target version;
- temporary file;
- expected hashes;
- metadata generation;
- state;
- and recovery action.

No plaintext is stored.

---

# 99. Crash Recovery

After a crash, the Vault should:

1. validate paths;
2. load keyring;
3. examine journal;
4. identify temporary records;
5. validate encrypted records;
6. reconcile metadata;
7. complete or roll back atomic mutations;
8. quarantine ambiguous files;
9. and enter Available or Recovery Required.

---

# 100. Corruption Handling

If one encrypted secret record is corrupt:

- deny that secret;
- preserve record;
- mark Corrupt;
- allow unrelated secrets to remain available;
- and offer replacement or restore.

---

## 100.1 Keyring Corruption

Keyring corruption may make the entire Vault unavailable.

Recovery requires:

- verified backup;
- repair from redundant envelope where designed;
- or portable recovery material.

The Vault must not generate a replacement root key and orphan existing records silently.

---

# 101. Redundancy

The keyring may keep:

- primary protected envelope;
- verified redundant protected copy;
- and manifest.

Redundancy does not protect against wrong user or wrong machine.

---

# 102. Migration

Vault format migrations must be:

- versioned;
- journalled;
- reversible through backup;
- and tested against corrupt records.

---

## 102.1 Cryptographic Migration

A cryptographic suite migration writes new record versions.

It never reinterprets old ciphertext under new parameters.

---

# 103. Algorithm Retirement

An algorithm suite may be:

- Active;
- Decrypt Only;
- Migration Required;
- Retired;
- or Rejected.

Rejected suites cannot be decrypted in normal operation without explicit recovery tooling.

---

# 104. Vault Backup Set

A Vault backup set should be independent from ordinary SQLite backup sets but may be included in a coordinated Opure profile backup.

The backup manifest must identify:

- secrets included;
- protector dependency;
- portability;
- and restoration requirements.

It never lists secret values.

---

# 105. Project Backup

Project backup should not automatically include project-scoped secrets unless the developer selects them.

Reason:

- project data may be shared;
- secret backup may be machine bound;
- and credentials have different retention.

---

# 106. Project Export

Project export excludes Vault secrets by default.

It may include unresolved secret-reference metadata only when useful and clearly marked.

---

# 107. Project Deletion

Deleting Opure data for a project should offer separate choices for project-scoped secrets.

Secrets shared across projects must not be deleted accidentally.

---

# 108. Secret Naming

Labels should help the developer distinguish secrets without revealing values.

Good labels:

- `GitHub – Opure development`
- `Local test registry`
- `Provider account – personal`

Avoid labels containing:

- token fragments;
- passwords;
- full account identifiers when unnecessary;
- or private repository paths.

---

# 109. Secret Search

Search operates on metadata only.

Search never decrypts values.

---

# 110. Duplicate Detection

Exact duplicate-secret detection would require comparing plaintext or stable fingerprints.

The initial Vault should not attempt global duplicate detection by default.

A future safe fingerprint design requires a separate security review.

---

# 111. Validation and Test Secret

The Vault UI may allow a developer to test one integration.

The test:

- obtains a scoped lease;
- uses the declared destination;
- reports result;
- and never reveals the value.

---

# 112. Rate Limiting

Rate-limit:

- unlock failures;
- secret-use requests;
- reveal requests;
- export;
- and plugin plaintext exceptions.

---

# 113. Denial Behaviour

Denial should reveal enough to remediate without revealing secret existence to unauthorised callers.

A Plugin Host may receive:

- capability denied;
- approval required;
- or unavailable

without a complete Vault inventory.

---

# 114. Enumeration

Only authorised first-party UI and management services may enumerate secret metadata.

Plugins and MCP servers cannot list all secrets.

---

# 115. Least Disclosure

A consumer should learn only:

- whether its required secret capability is configured;
- whether use is approved;
- and operation result.

It should not learn unrelated secret metadata.

---

# 116. Configuration Files

Configuration files may contain:

- secret reference;
- provider endpoint;
- account label;
- and policy.

They must not contain the value.

---

# 117. Environment Variables

Opure must scan its own process environment for known provider credentials only when the developer requests import or an integration explicitly declares environment use.

It must not silently ingest every secret-like environment variable.

---

# 118. Legacy Secret Migration

If Opure detects a plaintext secret in old configuration:

1. warn;
2. offer Vault import;
3. write encrypted record;
4. update configuration to reference;
5. verify integration;
6. securely delete plaintext best effort;
7. and record migration.

---

# 119. First-Run Behaviour

The Vault should initialise only when the first secret is stored or a protected integration requires it.

Empty Opure use should not create unnecessary secret state.

---

# 120. Safe Mode

Safe Mode should:

- keep the Vault locked initially where practical;
- permit metadata inspection;
- permit integrity checks;
- permit backup and restore;
- deny third-party secret use;
- disable plugin plaintext delivery;
- and require explicit unlock for recovery actions.

---

# 121. Recovery Mode

Recovery Mode should support:

- keyring validation;
- DPAPI test;
- record inventory;
- record authentication;
- metadata reconciliation;
- backup restore;
- rotation recovery;
- and encrypted export where supported.

It must not display plaintext values.

---

# 122. Performance

Vault operations are expected to be low volume.

Performance priorities are:

1. correctness;
2. minimal plaintext lifetime;
3. integrity;
4. recovery;
5. then speed.

---

## 122.1 Caching

Do not weaken security to reduce milliseconds of secret decryption.

---

## 122.2 Benchmarks

Measure:

- Vault initialisation;
- unlock;
- secret create;
- secret use;
- 1,000 secret inventory;
- 10,000 metadata records;
- rotation;
- backup;
- restore;
- and plugin-mediated use.

---

# 123. Reference Hardware

Tests should use:

- Windows 11;
- Ryzen 9 5950X;
- 32 GB RAM;
- RTX 5070 Ti 16 GB;
- local SSD.

The Vault should consume negligible resources compared with local models and builds.

---

# 124. Cryptographic Test Vectors

The implementation should maintain deterministic test vectors for:

- HKDF context;
- record format;
- associated data;
- AES-GCM output;
- parser;
- and migration.

Production nonces, salts and keys remain random.

---

# 125. Fuzz Testing

Fuzz:

- record parser;
- keyring parser;
- manifest parser;
- journal parser;
- metadata mismatch;
- length fields;
- unknown suite;
- truncated record;
- duplicate record;
- and corrupted tag.

---

# 126. Security Tests

Test:

- wrong DPAPI user;
- machine mismatch where practical;
- Local Machine scope rejection;
- broad ACL;
- junction substitution;
- record swap;
- record rename;
- version rollback;
- metadata substitution;
- tag corruption;
- nonce corruption;
- salt corruption;
- ciphertext truncation;
- keyring tamper;
- stale lease;
- revoked secret;
- plugin enumeration;
- secret in logs;
- secret in crash metadata;
- and command-line leak.

---

# 127. Secret Canary Tests

Inject unique canaries through:

- Desktop entry;
- provider adapter;
- plugin request;
- MCP request;
- command environment;
- command failure;
- exception;
- backup;
- diagnostic export;
- workflow cancellation;
- and Runtime crash.

Verify the canary does not appear in:

- SQLite;
- logs;
- traces;
- metrics;
- Trust Centre;
- project memory;
- embeddings;
- workflow checkpoints;
- plugin storage;
- command plan display;
- crash metadata;
- diagnostic bundle;
- or project files.

---

# 128. Memory Tests

Memory tests should inspect:

- allocations;
- string creation;
- buffer copies;
- cache lifetime;
- dump exposure;
- and zeroing.

The test can demonstrate minimisation.

It cannot prove no inaccessible copy ever existed.

---

# 129. Fault-Injection Tests

Inject:

- crash before record write;
- crash after record write before metadata;
- crash after metadata before journal completion;
- disk full;
- access denied;
- keyring unavailable;
- DPAPI failure;
- rotation interruption;
- backup interruption;
- restore interruption;
- and clock change.

---

# 130. Architecture Tests

Enforce:

- no secret value type serialization;
- no Vault file access outside Vault module;
- no Desktop raw secret response;
- no Plugin Host direct Vault reference;
- no secret in general configuration;
- no secret-bearing logging;
- no secret in workflow checkpoint types;
- no secret in memory-index contracts;
- and no command-line secret field in process plans.

---

# 131. Package and Supply-Chain Policy

The initial cryptographic implementation should prefer the .NET Base Class Library.

Avoid third-party cryptographic packages unless required.

Any package touching secret material requires:

- licence review;
- vulnerability review;
- source review where practical;
- version pinning;
- and security approval.

---

# 132. No Custom Cryptography

Opure may design:

- record framing;
- key hierarchy;
- and purpose binding.

It must not invent:

- cipher;
- MAC;
- KDF;
- random generator;
- password KDF;
- or signature algorithm.

---

# 133. Compliance Position

The first Vault is a product security control.

It does not claim certification under:

- FIPS;
- Common Criteria;
- PCI DSS;
- SOC 2;
- ISO 27001;
- or another standard.

Future compliance requirements need separate assessment.

---

# 134. FIPS Considerations

If an organisation requires FIPS-validated cryptography:

- algorithm provider;
- operating-system mode;
- .NET implementation;
- and deployment

must be reviewed.

This ADR does not claim FIPS validation.

---

# 135. Cross-Platform Architecture

Define:

```text
IPlatformKeyProtector
```

Conceptual operations:

- ProtectRootKey;
- UnprotectRootKey;
- DescribeProtection;
- ValidateAvailability;
- RotateProtection;
- and Diagnose.

---

## 135.1 Windows

Initial protector:

```text
WindowsDpapiCurrentUserKeyProtector
```

---

## 135.2 Linux

Future options may include:

- Secret Service;
- kernel keyring;
- desktop keychain;
- TPM-backed key;
- or password-protected local key.

A separate ADR is required.

---

## 135.3 macOS

Future options may include:

- Keychain;
- Secure Enclave-backed key;
- or user-presence-protected item.

A separate ADR is required.

---

## 135.4 Record Portability

The encrypted record format remains the same.

Only the root-key envelope protector changes.

---

# 136. Platform Migration

A platform migration requires access to the plaintext root key in a trusted migration workflow.

The workflow:

1. unlocks source protector;
2. verifies Vault;
3. protects root key with destination protector;
4. writes new envelope;
5. validates;
6. and records migration.

Secret records do not need re-encryption solely because the OS protector changes.

---

# 137. User-Account Migration

Windows account migration may invalidate DPAPI access.

The normal Vault should warn that user-profile migration needs verified backup and supported Windows credential migration.

A portable recovery option is deferred.

---

# 138. Machine Replacement

Normal DPAPI backups should not be presented as sufficient for machine replacement.

The UI must distinguish:

- same-machine recovery;
- profile recovery;
- and portable recovery.

---

# 139. Recovery Key

An optional recovery key or password-protected root-key envelope is deferred.

It requires:

- strong KDF;
- recovery UX;
- storage guidance;
- brute-force resistance;
- and threat review.

---

# 140. Organisation Escrow

Organisation-managed recovery or escrow is deferred.

It must never be enabled silently.

---

# 141. Multi-Profile Isolation

Every Opure profile has a separate:

- Vault identity;
- root key;
- keyring;
- metadata store;
- record root;
- and backup set.

One profile cannot resolve another profile's secret references.

---

# 142. Test Profiles

Test profiles use generated fake secrets and isolated Vault roots.

Production Vault files must never be copied into automated test fixtures.

---

# 143. Development Mode

Development mode may provide:

- fake Vault;
- in-memory Vault;
- deterministic test vectors;
- and secret canary injection.

The production Vault must not permit a plaintext development backend in release builds.

---

# 144. Build and CI

CI should use the CI platform's secret mechanism.

CI secrets must not be committed into Opure's test repository.

End-to-end Vault tests use generated ephemeral values.

---

# 145. Error Model

Stable error categories include:

- Vault Not Initialised;
- Vault Locked;
- Vault Unavailable;
- Root Key Unprotect Failed;
- Wrong User or Machine;
- Keyring Corrupt;
- Record Missing;
- Record Corrupt;
- Authentication Failed;
- Scope Denied;
- Approval Required;
- Secret Expired;
- Secret Revoked;
- Lease Expired;
- Delivery Failed;
- Rotation Required;
- Backup Not Portable;
- Restore Incompatible;
- Disk Full;
- Permission Denied;
- and Recovery Required.

---

# 146. Error Messages

Errors must not reveal:

- value;
- token prefix;
- key material;
- ciphertext;
- full secret metadata to unauthorised callers;
- or cryptographic oracle detail.

---

# 147. Oracles

Authentication and decryption failures should avoid exposing distinctions that help an attacker.

Detailed internal error may remain in safe diagnostics without secret data.

---

# 148. Observability

The Vault should expose:

- state;
- secret count;
- expired count;
- rotation status;
- key versions;
- integrity status;
- backup age;
- lease count;
- denied-use count;
- and safe error codes.

---

# 149. Health

Vault health checks should verify:

- paths;
- permissions;
- metadata;
- keyring format;
- root-key availability where unlocked;
- key check;
- journal state;
- and sample or scheduled record integrity.

Health checks should not decrypt every secret on every request.

---

# 150. Scheduled Integrity

A low-priority integrity job may authenticate encrypted records without retaining plaintext.

Records can be decrypted into a temporary buffer, validated and immediately zeroed.

The job should be bounded and cancellable.

---

# 151. Retention

Old secret versions should be retained only for:

- active lease;
- rollback window;
- audit reference;
- or recovery.

Retention must not become indefinite by default.

---

# 152. Last-Used Time

Last-used time is metadata.

It may be disabled for privacy-sensitive environments.

Trust Centre actions may still record required external use separately.

---

# 153. Usage Count

Usage count is not required.

It can become behavioural monitoring.

Use only where needed for security or rotation.

---

# 154. Secret Discovery UI

The UI should organise secrets by:

- project;
- provider;
- plugin;
- MCP server;
- repository;
- state;
- and rotation status.

It should not display a global unfiltered inventory to plugins.

---

# 155. Risk Levels

Secret actions have risk classes.

Examples:

- create local-only provider key: Moderate;
- broaden project scope: High;
- plaintext plugin disclosure: High;
- reveal: High;
- portable export: Critical;
- delete last recovery envelope: Critical.

---

# 156. Approval Binding

Approval should bind to:

- secret identifier;
- secret version where relevant;
- caller;
- purpose;
- destination;
- duration;
- operation;
- and risk.

Changing any material field invalidates approval.

---

# 157. Developer Notifications

Notify the developer for:

- expiry approaching;
- rotation required;
- repeated denied use;
- corrupt record;
- backup non-portability;
- and recovery issue.

Do not include secret value in notifications.

---

# 158. Automatic Provider Discovery

Opure must not scan arbitrary files or system credential stores and import secrets silently.

Discovery may identify that a provider might already be configured.

Import requires deliberate action.

---

# 159. Secret Files in Projects

When Opure detects:

- `.env`;
- private-key files;
- cloud credential files;
- or token files

it should warn and offer:

- ignore;
- protect;
- move to Vault;
- add to ignore;
- and scan Git history guidance.

It must not delete automatically.

---

# 160. Secret Removal from Source

Moving a secret to the Vault does not remove it from:

- Git history;
- remote repositories;
- backups;
- build caches;
- or provider logs.

Opure should recommend rotation.

---

# 161. Provider Revocation Guidance

When a leaked secret is detected, the UI should provide:

- provider destination;
- rotation guidance;
- affected integrations;
- and verification steps.

It should not pretend local deletion revokes the provider credential.

---

# 162. Release Requirements

A release must not ship if:

- Local Machine DPAPI is used for ordinary root keys;
- root key is written plaintext;
- nonce reuse test fails;
- truncated AES-GCM tags are accepted;
- secret values appear in SQLite;
- Desktop receives plaintext in normal queries;
- Plugin Host has direct Vault access;
- command-line secret injection is the default;
- log canary test fails;
- backup is described as portable when it is not;
- or interrupted rotation can orphan all records.

---

# 163. Prototype Plan

## 163.1 Prototype A — Vault Initialisation

Implement:

- restricted directories;
- random root key;
- DPAPI Current User envelope;
- key check;
- metadata store;
- and clean shutdown.

---

## 163.2 Prototype B — Secret Record

Implement:

- HKDF;
- AES-GCM;
- canonical AAD;
- record parser;
- atomic write;
- read;
- update;
- and delete.

---

## 163.3 Prototype C — Tamper Tests

Test:

- header change;
- record swap;
- version change;
- ciphertext change;
- nonce change;
- tag change;
- truncation;
- and trailing data.

---

## 163.4 Prototype D — Secret Lease

Implement:

- policy request;
- lease;
- trusted adapter callback;
- expiry;
- revocation;
- and safe result.

---

## 163.5 Prototype E — Plugin Use

Implement:

- test Plugin Host;
- brokered secret operation;
- denied enumeration;
- raw plaintext exception;
- cancellation;
- and Trust record.

---

## 163.6 Prototype F — External Command

Implement:

- secure pipe or standard-input injection;
- environment fallback;
- process tree;
- crash;
- logs;
- and cleanup.

---

## 163.7 Prototype G — Rotation

Implement:

- new root key;
- re-encryption;
- crash at each stage;
- recovery;
- rollback;
- and old-key retirement.

---

## 163.8 Prototype H — Backup and Restore

Implement:

- same-machine backup;
- restore;
- wrong-user failure;
- wrong-machine test where practical;
- manifest;
- and non-portability warning.

---

## 163.9 Prototype I — Memory and Canary

Measure:

- string allocations;
- plaintext lifetime;
- zeroing;
- dump exposure;
- logs;
- SQLite;
- Trust Centre;
- and diagnostic export.

---

# 164. Implementation Plan

## 164.1 Initial Tasks

1. Record founder review.
2. Pin .NET LTS release.
3. Define cryptographic suite.
4. Define canonical record format.
5. Define platform key-protector interface.
6. Implement Windows DPAPI Current User protector.
7. Implement root-key keyring.
8. Implement record key derivation.
9. Implement AES-GCM record cipher.
10. Implement metadata store.
11. Implement atomic record store.
12. Implement mutation journal.
13. Implement Vault service contract.
14. Implement secret-material type.
15. Implement secret-use leases.
16. Implement provider adapter path.
17. Implement Plugin Host mediation.
18. Implement external command delivery.
19. Implement rotation.
20. Implement backup and restore.
21. Implement integrity and Recovery Mode.
22. Implement secret-canary suite.
23. Complete independent security review.
24. Benchmark.
25. Accept, amend or reject the ADR.

---

## 164.2 Owners

| Area | Owner |
|---|---|
| Product decision | Founder |
| Security architecture | Security Architecture Owner |
| Vault service | Secrets Vault Owner |
| Platform protector | Windows Platform Owner |
| Cryptographic review | Security Owner |
| Persistence metadata | Persistence Owner |
| Runtime integration | Runtime Architecture Owner |
| IPC | IPC Owner |
| AI provider use | AI Router Owner |
| Plugin use | Plugin Owner |
| MCP use | MCP Owner |
| Command injection | Build and Repository Owners |
| Backup and recovery | Recovery Owner |
| Privacy | Privacy Owner |
| Testing | Security Test Owner |

---

# 165. Suggested Repository Structure

```text
src/
├── Opure.Secrets.Abstractions/
├── Opure.Secrets.Contracts/
├── Opure.Secrets.Vault/
├── Opure.Secrets.Cryptography/
├── Opure.Secrets.Storage/
├── Opure.Secrets.Metadata/
├── Opure.Secrets.Leases/
├── Opure.Secrets.Injection/
├── Opure.Secrets.Recovery/
├── Opure.Secrets.Platform.Abstractions/
└── Opure.Secrets.Platform.Windows/
```

Test projects:

```text
tests/
├── Opure.Secrets.UnitTests/
├── Opure.Secrets.CryptoTests/
├── Opure.Secrets.SecurityTests/
├── Opure.Secrets.FuzzTests/
├── Opure.Secrets.RecoveryTests/
└── Opure.Secrets.EndToEndTests/
```

---

# 166. Coding Rules

Vault code should:

- enable nullable reference analysis;
- treat warnings as errors;
- avoid unsafe code;
- avoid reflection;
- avoid dynamic serialization;
- use checked lengths;
- use spans carefully;
- zero mutable key buffers;
- use constant-time comparison where applicable;
- and minimise dependencies.

---

# 167. Code Review

Every change to:

- cryptographic format;
- key management;
- secret use;
- injection;
- backup;
- restore;
- or redaction

requires security-owner review.

---

# 168. Test Coverage

High statement coverage is insufficient.

Required behavioural tests include:

- cryptographic misuse;
- interruption;
- tamper;
- access control;
- memory;
- logging;
- scope;
- and recovery.

---

# 169. Independent Review

Before Public Technical Preview, the Vault should receive independent security review or penetration testing.

---

# 170. Acceptance Criteria

This ADR may move to **Accepted** when:

- [ ] A 256-bit root key is generated cryptographically.
- [ ] Root key is protected with DPAPI Current User.
- [ ] Local Machine DPAPI is rejected for ordinary Vaults.
- [ ] Root key never appears in plaintext files.
- [ ] Keyring is versioned and atomic.
- [ ] HKDF-SHA-256 derives a distinct record key per secret version.
- [ ] Every record has a unique random salt.
- [ ] Every record has a fresh 12-byte nonce.
- [ ] AES-256-GCM uses an explicit 16-byte tag.
- [ ] Associated data binds immutable identity and scope.
- [ ] Record substitution is detected.
- [ ] Record rollback is detected or controlled through version metadata.
- [ ] Record corruption fails closed.
- [ ] Encrypted values remain outside SQLite.
- [ ] Metadata contains no secret value or ciphertext.
- [ ] Vault paths have restrictive ACLs.
- [ ] Junction and link substitution tests pass.
- [ ] Record update is atomic.
- [ ] Mutation journal recovers interrupted operations.
- [ ] Desktop cannot query raw secret value.
- [ ] Secret material is non-serialisable.
- [ ] Secret use is purpose and destination bound.
- [ ] Lease expiry works.
- [ ] Lease revocation works.
- [ ] Plugin enumeration is denied.
- [ ] Plugin plaintext delivery is exceptional and approved.
- [ ] MCP secret use is mediated.
- [ ] Provider secret use is mediated.
- [ ] Command-line injection is denied by default.
- [ ] Environment injection uses a constructed environment.
- [ ] Temporary secret files are restrictive and cleaned.
- [ ] Mutable plaintext buffers are zeroed where practical.
- [ ] Secret strings are avoided and measured.
- [ ] Logs contain no secret values.
- [ ] Traces contain no secret values.
- [ ] Trust Centre contains no secret values.
- [ ] Workflow checkpoints contain no secret values.
- [ ] Project memory and embeddings contain no secret values.
- [ ] Diagnostic exports exclude Vault records.
- [ ] Crash upload is disabled.
- [ ] Root-key rotation succeeds.
- [ ] Interrupted rotation recovers.
- [ ] Old root keys retire safely.
- [ ] Secret update creates a new version.
- [ ] Revocation denies new use.
- [ ] Same-machine backup and restore are tested.
- [ ] Cross-machine non-portability is shown honestly.
- [ ] Wrong-user restore fails safely.
- [ ] Keyring corruption does not recreate the Vault silently.
- [ ] Secret-canary tests pass across all prohibited systems.
- [ ] Fuzz tests pass.
- [ ] Performance is measured.
- [ ] Independent security review is complete.
- [ ] Founder approval is recorded.

---

# 171. Evidence Required Before Acceptance

- [ ] DPAPI protector test report;
- [ ] cryptographic design review;
- [ ] record-format test vectors;
- [ ] nonce and salt test;
- [ ] tamper and substitution test;
- [ ] atomic mutation test;
- [ ] rotation recovery test;
- [ ] backup and restore report;
- [ ] wrong-user and wrong-machine behaviour report;
- [ ] secret-use lease prototype;
- [ ] plugin mediation test;
- [ ] MCP mediation test;
- [ ] provider adapter test;
- [ ] command injection test;
- [ ] memory allocation and zeroing report;
- [ ] secret-canary report;
- [ ] logging and diagnostics report;
- [ ] fuzzing report;
- [ ] package and licence review;
- [ ] independent security review;
- [ ] founder approval.

---

# 172. Known Limitations

- The exact .NET LTS release is not yet pinned.
- The final binary record format is not yet implemented.
- The exact platform storage path is not final.
- The Vault initially shares the trusted Runtime process.
- DPAPI Current User does not defend fully against same-user malware.
- Normal Vault backups are usually machine and profile bound.
- Portable recovery is deferred.
- Windows Hello user presence is deferred.
- Hardware-backed keys are deferred.
- Secure deletion cannot be guaranteed.
- Managed-memory copies cannot be proven absent.
- Full-disk encryption remains the developer's or organisation's responsibility.
- Plugin plaintext exceptions may expose values inside the Plugin Host.
- External tools may retain credentials after use.
- Provider-side revocation remains provider specific.
- Organisation secret servers are deferred.
- Team secret sharing is deferred.
- Certificate-store integration is deferred.
- Database-wide encryption is separate.
- Crash dumps remain inherently sensitive.

---

# 173. Open Questions

- Which .NET LTS release should be pinned?
- What exact canonical binary record format should be used?
- Should the keyring use binary or canonical JSON outer framing?
- Should record salt be 32 bytes or another reviewed size?
- Should the Vault derive one record key or separate encryption and metadata keys?
- Should the Root Key remain loaded for the full Runtime session?
- Should the Vault auto-lock after inactivity?
- Should a key check record be duplicated?
- Should the metadata database use FULL synchronous?
- What record-size limits should apply to each secret type?
- Should very large private keys use streaming encryption?
- Which secret-use operations can avoid plaintext delivery entirely?
- Which providers require string-only credentials?
- How should secret-bearing HTTP clients be pooled safely?
- Should Plugin Hosts ever receive plaintext in version 1.0?
- What is the safest Git credential-helper integration on Windows?
- How should package-manager temporary configuration be handled?
- How should OAuth refresh-token replacement be made atomic?
- Should Vault backup be included in default profile backup?
- How should same-machine restore be verified before backup completion?
- Which portable recovery design should be adopted?
- Should recovery use a password, recovery key or recipient public key?
- Should Windows Hello protect reveal and export?
- When should the Vault move to a dedicated process?
- Can Windows process mitigations meaningfully reduce same-user extraction?
- Should root keys be protected additionally by TPM-backed keys?
- How should organisation-managed protectors integrate?
- Which metadata fields are Sensitive?
- How long should old secret versions remain?
- How should secure clipboard clearing be described?
- What independent security review is required before alpha?
- Which record corruption should trigger full Vault Recovery Required?
- Should record inventory hashes use Merkle trees?
- How should rollback of metadata files be detected?
- Should the Vault maintain an append-only mutation sequence?
- How should Vault state participate in Opure profile migration?

---

# 174. Deferred Decisions

This ADR intentionally defers:

- portable recovery format to a Vault Recovery ADR;
- Windows Hello user presence to a User Presence ADR;
- dedicated Vault process to a Vault Isolation ADR;
- TPM or hardware-backed protection to a Hardware Key ADR;
- organisation secret-server integration to an Enterprise Secrets ADR;
- cloud secret managers to optional provider ADRs;
- team sharing to post-1.0 collaboration planning;
- certificate store and signing keys to a Signing Architecture ADR;
- database-wide encryption to a Data-at-Rest ADR;
- secure clipboard feature to a Desktop Security ADR;
- and provider-specific credential rotation to adapter ADRs.

---

# 175. Alternatives Rejected

## 175.1 Windows Credential Manager as the Full Vault

Rejected because it is Windows specific, bounded in record size, limited in application metadata and not sufficient as Opure's portable secret lifecycle and broker model.

---

## 175.2 DPAPI Per Secret

Rejected because it would couple every encrypted record to Windows and make future platform migration and application-level key rotation more difficult.

---

## 175.3 ASP.NET Core Data Protection as the Full Vault

Rejected because it solves protected application payloads but does not by itself define Opure's secret inventory, scopes, leases, consumer mediation, backup and recovery model.

---

## 175.4 Password-Protected Vault by Default

Rejected because it imposes password creation, entry and loss risk on ordinary local operation.

It remains relevant for portable recovery.

---

## 175.5 Local Vault Server

Rejected because an additional daemon and authentication system are disproportionate for the first local single-user product.

---

## 175.6 Cloud Secret Manager

Rejected because core Opure operation must remain local, offline and provider independent.

---

## 175.7 Plain Configuration

Rejected because plaintext files and ACLs alone violate the Charter's encrypted Vault requirement.

---

## 175.8 Dedicated Vault Process Immediately

Not selected initially because the hybrid Runtime topology already provides a trusted core and the added IPC and recovery complexity should be justified through evidence.

The design preserves later extraction.

---

# 176. Official Evidence References

The following official Microsoft sources informed this ADR:

- [ProtectedData class](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.protecteddata)
- [CryptProtectData](https://learn.microsoft.com/en-us/windows/win32/api/dpapi/nf-dpapi-cryptprotectdata)
- [CryptUnprotectData](https://learn.microsoft.com/en-us/windows/win32/api/dpapi/nf-dpapi-cryptunprotectdata)
- [DPAPI header and functions](https://learn.microsoft.com/en-us/windows/win32/api/dpapi/)
- [CNG DPAPI and DPAPI-NG](https://learn.microsoft.com/en-us/windows/win32/seccng/cng-dpapi)
- [AesGcm class](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesgcm)
- [AesGcm required tag size guidance](https://learn.microsoft.com/en-us/dotnet/fundamentals/syslib-diagnostics/syslib0053)
- [HKDF class](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.hkdf)
- [HKDF.DeriveKey](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.hkdf.derivekey)
- [RandomNumberGenerator.Fill](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.randomnumbergenerator.fill)
- [CryptographicOperations.ZeroMemory](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.cryptographicoperations.zeromemory)
- [CryptProtectMemory](https://learn.microsoft.com/en-us/windows/win32/api/dpapi/nf-dpapi-cryptprotectmemory)
- [Windows Credential Manager overview](https://learn.microsoft.com/en-us/windows-server/security/windows-authentication/credentials-processes-in-windows-authentication)
- [Credential structure and blob size](https://learn.microsoft.com/en-us/windows/win32/api/wincred/ns-wincred-credentialw)
- [Kinds of Windows credentials](https://learn.microsoft.com/en-us/windows/win32/secauthn/kinds-of-credentials)
- [ASP.NET Core key encryption at rest](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-encryption-at-rest)

Versions and platform behaviour can change.

The implementation team must verify all selected APIs against the pinned .NET and Windows support baseline before acceptance.

---

# 177. Review Record

| Date | Reviewer | Decision | Notes |
|---|---|---|---|
| 18 July 2026 | Architecture draft | Proposed | Application-managed Vault with DPAPI-protected root key, HKDF and AES-GCM recommended |

---

# 178. Approval

## Founder or Product Approval

- **Name:** Christopher Dyer
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Pending founder review

## Architecture Approval

- **Name or role:** Security Architecture Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Key hierarchy, storage and consumer prototypes required

## Cryptographic Approval

- **Name or role:** Independent Security Reviewer
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Record format, KDF context, nonce discipline and rotation require review

## Recovery Approval

- **Name or role:** Recovery Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Keyring, mutation, backup, restore and rotation recovery tests required

## Privacy Approval

- **Name or role:** Privacy Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Metadata, use history, export and retention review required

---

# 179. Supersession

This ADR is superseded only when a later ADR:

- names ADR-0007 explicitly;
- explains why the Vault, protector or cryptographic suite changed;
- identifies affected records and consumers;
- describes migration and recovery;
- explains backup portability;
- explains security impact;
- and updates the `Superseded by` field.

Historical ADRs remain in version control.

---

# 180. Change History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | 18 July 2026 | Founder Draft | Initial DPAPI-protected application Vault recommendation |

---

# 181. Final Decision Statement

> **Opure will provisionally use an application-managed encrypted Secrets Vault with a cryptographically random 256-bit Vault Root Key protected on Windows by Current User DPAPI, per-secret-version keys derived through HKDF-SHA-256 and records encrypted through AES-256-GCM with authenticated identity and scope, because this provides strong local at-rest protection, brokered least-privilege use, future cross-platform key-protector portability and clear separation from ordinary databases, logs, memory systems, plugins and developer-facing history, while honestly acknowledging the limits of same-user and in-memory protection.**

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**