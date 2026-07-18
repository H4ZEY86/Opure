# ADR-0014 — Code Signing and Release Trust

## Architecture Decision Record

**Status:** Proposed  
**Date:** 18 July 2026  
**Decision owners:** Founder and Product Owner  
**Technical owners:** Release Trust and Signing Owner  
**Reviewers:** Release Engineering Owner, Build and Continuous Integration Owner, Security Owner, Packaging Owner, Repository Architecture Owner, Plugin SDK Owner, Incident Response Owner  
**Supersedes:** None  
**Superseded by:** None  
**Related ADRs:** ADR-0001 through ADR-0013  
**Related specifications:** CHARTER-001, SPEC-001 through SPEC-012  
**Target milestone:** First public preview through Version 1.0  

---

## 1. Decision Summary

Opure should use a **layered release-trust model** rather than treating one certificate or one hosting platform as sufficient proof.

The preferred production code-signing path is:

> **Microsoft Artifact Signing Public Trust under a validated Opure legal organisation, using a non-exportable Microsoft-managed HSM key, short-lived signing certificates, GitHub Actions OpenID Connect, least-privilege Azure RBAC and mandatory RFC 3161 timestamping.**

This preference is conditional on Opure releasing under an eligible and successfully validated legal organisation.

Current Microsoft eligibility allows Public Trust for organisations in the United Kingdom, but not for individual developers in the United Kingdom.

Therefore:

- if Opure has an eligible validated legal organisation, Artifact Signing Public Trust is the selected production Authenticode and MSIX signing service;
- if Opure remains an individual UK publisher at the first public release, the approved fallback is a publicly trusted OV code-signing certificate whose private key is non-exportable and held by a compliant provider HSM, cloud signing service or dedicated hardware token;
- a password-protected PFX stored in GitHub, the Opure Vault, a build artefact, a developer profile or a repository directory is prohibited;
- a self-signed certificate is permitted only for development and controlled package testing;
- and direct public distribution is blocked until a production trust path has passed identity, signing, timestamp, verification and incident-response gates.

The release-trust chain will use separate controls for separate claims:

1. **Signed Git release tag** — a human release authority approved this source commit.
2. **GitHub build attestation** — an approved workflow built the artefact from the recorded repository and commit.
3. **Authenticode signatures on first-party executable files** — the binaries were signed by the validated Opure publisher and have not been modified.
4. **Signed MSIX package** — Windows can verify package publisher and package integrity.
5. **RFC 3161 timestamp** — signatures remain verifiable after the short-lived signing certificate expires.
6. **Immutable GitHub release** — published tag and release assets cannot be silently replaced.
7. **SHA-256 release manifest** — every distributed file has an explicit digest and role.
8. **SBOM and component inventory** — the contents and dependencies are reviewable.
9. **Release evidence bundle** — tests, approvals, signer identities, certificate thumbprints and provenance are tied to one version.

The selected human source-release signature is:

> **A dedicated SSH release-signing key held on a hardware security key, separate from repository authentication keys, protected by PIN and physical user presence.**

The release-signing public keys and trust periods should be recorded in a repository-owned allowed-signers file.

The normal release flow should:

- build and test unsigned source outputs;
- verify a release manifest and hashes;
- sign first-party PE files and shipped PowerShell scripts;
- verify every file signature;
- create the MSIX from the exact signed files;
- sign and timestamp the MSIX;
- verify the package on a second clean Windows job;
- run package acceptance tests;
- create a human-signed annotated Git tag for the exact source commit;
- generate GitHub provenance attestations;
- attach exact assets to a draft release;
- verify hashes, tag, signatures and attestations;
- and publish an immutable release.

The release workflow must not:

- expose a private signing key;
- use a long-lived Azure client secret when OIDC is available;
- sign pull-request output;
- sign an arbitrary commit supplied by an untrusted actor;
- compile after the signing boundary;
- repackage after package acceptance;
- overwrite a public version;
- depend on SmartScreen reputation as proof of trust;
- treat a GitHub “Verified” badge as sufficient local verification;
- or confuse publisher identity, source approval, build provenance and package integrity.

---

## 2. Status

Current status:

> **Proposed**

This ADR becomes **Accepted** only after Opure demonstrates:

- a final or test-equivalent publisher identity;
- Artifact Signing organisational eligibility or an accepted OV fallback;
- successful public or production-shaped identity validation;
- least-privilege signing-resource roles;
- GitHub OIDC federation restricted to the release environment;
- no stored Azure client secret;
- no exportable production private key;
- Authenticode signing of representative EXE and DLL files;
- signing of a shipped PowerShell script if any are distributed;
- signed and timestamped MSIX;
- signature verification on a different clean machine;
- a hardware-backed signed Git tag;
- local tag verification from a repository-owned signer policy;
- immutable GitHub release simulation;
- build and SBOM attestations;
- signer and thumbprint capture;
- revocation simulation;
- signer rotation simulation;
- fallback signing-path documentation;
- and one full release-trust rehearsal.

---

## 3. Context

Opure will distribute a Windows application containing:

- Desktop;
- Runtime;
- Worker;
- Plugin Host;
- recovery and diagnostic tools;
- first-party managed assemblies;
- native dependencies;
- public Plugin SDK packages;
- a signed MSIX package;
- and a diagnostic ZIP.

A developer downloading Opure needs to answer several distinct questions:

- Who claims to have published this software?
- Which source commit was approved for release?
- Which workflow built the binaries?
- Were the files modified after the publisher signed them?
- Was the package modified after construction?
- Did the signing certificate remain valid at signing time?
- Can the published assets be replaced later?
- Which dependencies and files are inside the release?
- Has a signer or certificate since been compromised or revoked?
- Is the release still supported?

No single control answers all of these.

An Authenticode signature proves publisher identity and file integrity according to Windows trust.

It does not prove:

- code review;
- correct source;
- test completion;
- safe dependencies;
- correct release notes;
- or absence of malicious logic.

A Git tag signature proves that a signing key approved a Git object.

It does not prove that the distributed binary was built from that object.

A GitHub attestation links an artefact to workflow provenance.

It does not prove that the workflow or source was benign.

An immutable release prevents silent asset replacement.

It does not prove the original artefact was correct.

The architecture must combine these controls and preserve their separate meanings.

---

## 4. Problem Statement

Opure needs a code-signing and release-trust architecture that protects the publisher identity, avoids exportable production private keys, permits secure CI signing, distinguishes source approval from binary signing, provides verifiable provenance and immutable publication, supports incident response and remains practical for a founder-led UK project.

---

## 5. Decision Drivers

The decision is evaluated against:

- Charter alignment;
- Windows trust compatibility;
- MSIX publisher identity;
- direct-download support;
- UK eligibility;
- non-exportable key custody;
- hardware-backed signing;
- CI integration;
- short-lived credentials;
- GitHub OIDC;
- least privilege;
- SmartScreen behaviour;
- timestamping;
- source approval;
- provenance;
- immutable publication;
- revocation;
- rotation;
- recovery;
- public Plugin SDK distribution;
- small-team practicality;
- cost;
- auditability;
- and future organisational growth.

---

## 6. Governing Principles

This decision must preserve:

- Developer Respect;
- Human in Control;
- Visible and Inspectable Decisions;
- Least Privilege;
- No Exportable Production Signing Key;
- No Secret in Source or Build Artefacts;
- No Pull-Request Signing;
- No Arbitrary-Ref Signing;
- No Release Rebuild;
- No Mutable Published Artefact;
- No False “Verified Means Safe” Claim;
- No Single-Point Trust Claim;
- No Silent Signer Rotation;
- No Silent Certificate Revocation;
- No Publisher Identity Drift;
- Evidence-Based Confidence;
- and Recoverable Incident Response.

---

## 7. Scope

This ADR decides:

- the preferred public code-signing service;
- the eligibility fallback;
- development signing;
- publisher identity governance;
- production key custody;
- CI authentication;
- Azure RBAC;
- GitHub environment policy;
- which files are signed;
- signing order;
- digest algorithms;
- timestamping;
- signature verification;
- Git tag signing;
- release signer policy;
- provenance attestations;
- release manifests;
- immutable publication;
- NuGet package trust;
- revocation;
- rotation;
- compromise response;
- release withdrawal;
- and signing evidence.

This ADR does not decide:

- the legal formation of the Opure publisher;
- the final publisher name;
- the final signing vendor if the OV fallback is required;
- Microsoft Store submission;
- automatic update signing;
- update feed format;
- notarisation for macOS;
- Linux package signing;
- container signing;
- enterprise private-trust policy;
- long-term support;
- or public package-feed credentials.

---

## 8. Constraints

Known constraints include:

- The founder is based in the United Kingdom.
- Artifact Signing Public Trust currently supports UK organisations.
- Artifact Signing Public Trust currently does not support UK individual developers.
- The final Opure legal publisher is not yet recorded.
- The MSIX Publisher value must match the signing certificate subject.
- The package family depends on package name and publisher.
- Changing the publisher can break normal MSIX update continuity.
- Public MSIX installation requires a trusted signature.
- Artifact Signing private keys and certificates are service managed.
- Artifact Signing certificates are short lived.
- Timestamping is essential for durable validation.
- GitHub Actions is the initial CI platform.
- Production signing must not run for pull requests.
- The founder is initially the sole release authority.
- Git tags and Authenticode signatures use different trust technologies.
- GitHub release assets can be made immutable.
- NuGet package author signing uses an X.509 certificate and signing toolchain that may not integrate directly with Artifact Signing.
- NuGet.org repository-signs uploaded packages.
- SmartScreen reputation develops over time and is not immediate proof.
- Release evidence must work locally and not depend solely on a web UI.
- The first public release cannot depend on an unaccepted signing service.
- Signing infrastructure changes are release-security changes.

---

## 9. Assumptions

This decision assumes:

- Opure will either establish an eligible legal organisation or procure a suitable OV fallback before direct public distribution.
- GitHub-hosted Windows runners can execute the Artifact Signing action.
- GitHub OIDC can federate to a Microsoft Entra workload identity.
- The release job can use a protected GitHub environment.
- Artifact Signing Public Trust supports the required PE and MSIX file types.
- The package and first-party executables can be signed with SHA-256.
- The Microsoft Artifact Signing timestamp authority remains available.
- A dedicated hardware security key can perform SSH signatures for Git tags.
- GitHub can verify registered SSH signing keys.
- local Git can verify tags using a repository-owned allowed-signers policy;
- GitHub attestations remain available for the selected repository plan;
- release assets can remain immutable;
- and the diagnostic ZIP can rely on signed contents, hashes and attestations rather than an Authenticode signature on the ZIP container.

---

## 10. Current Platform Evidence

Current official documentation establishes that:

- Microsoft Artifact Signing is a managed signing service whose keys are held in FIPS-certified HSM infrastructure.
- Artifact Signing supports Public Trust and Private Trust models.
- Public Trust is intended for publicly distributed Windows software.
- Public Trust currently supports organisations in the United States, Canada, European Union and United Kingdom.
- Public Trust currently supports individual developers only in the United States and Canada.
- the service manages short-lived signing certificates and does not provide the private key to the customer;
- the certificate is available only while the signing operation is performed;
- signing is supported through SignTool, GitHub Actions, Azure DevOps, PowerShell and an SDK;
- the official GitHub Action supports Windows hosted runners and recommends OpenID Connect;
- the signing identity needs only the Certificate Profile Signer role at the certificate-profile scope;
- GitHub OIDC can authenticate to Azure without a long-lived client secret;
- OIDC trust should be constrained to predictable repository, ref or environment claims;
- MSIX packages must be signed by a trusted certificate;
- timestamping preserves validation after certificate expiry;
- Artifact Signing certificates are short lived, making timestamping critical;
- signatures can be verified with SignTool;
- certificate thumbprints can be revoked and revocation is irreversible;
- Git supports cryptographically signed annotated tags using SSH, OpenPGP or X.509;
- GitHub verifies SSH-signed commits and tags when the public signing key is registered;
- GitHub artefact attestations link an artefact to repository, workflow, commit and event provenance;
- GitHub immutable releases protect associated tags and assets from later mutation;
- NuGet packages support author and repository signatures;
- and nuget.org automatically repository-signs uploaded packages.

All exact service names, versions, eligibility rules, roles and plan capabilities must be revalidated before the first public signature.

---

## 11. Trust Claims

Opure will document the claim made by each release control.

| Control | Primary claim | Does not prove |
|---|---|---|
| Authenticode PE signature | File came from validated publisher and is unchanged | Correct source or successful tests |
| MSIX signature | Package publisher and package integrity | Safe application behaviour |
| RFC 3161 timestamp | Signature was created while signing certificate was valid | Release approval |
| Signed Git tag | Release authority approved a source commit | Binary was built from that commit |
| Build attestation | Workflow produced artefact from recorded source | Workflow or source was benign |
| SBOM attestation | Recorded dependency inventory is tied to build | No unknown vulnerability |
| SHA-256 manifest | Exact file bytes are identifiable | Publisher identity |
| Immutable release | Published tag and assets cannot be replaced | Initial release correctness |
| Release evidence | Required gates and exceptions are recorded | Future absence of defects |
| SmartScreen reputation | Windows ecosystem has observed publisher/file history | Cryptographic approval or safety |

No user-facing documentation may collapse these into one statement such as “signed means safe”.

---

## 12. Options Considered

The principal production signing options are:

1. Microsoft Artifact Signing Public Trust under an eligible Opure organisation.
2. Traditional OV code-signing certificate with non-exportable HSM custody.
3. Microsoft Store signing only.
4. EV code-signing certificate.
5. Exportable PFX in CI.
6. Local USB token on a developer workstation.
7. Self-signed production certificate.
8. Azure Key Vault with a separately issued certificate.
9. No code signing.
10. Private Trust only.

---

## 13. Option A — Artifact Signing Public Trust

### Advantages

- Microsoft-managed certificate lifecycle;
- private key never exported;
- HSM-backed custody;
- short-lived certificates;
- Windows public trust;
- CI integration;
- GitHub Actions support;
- OIDC support;
- Azure RBAC;
- scoped signer role;
- no USB token;
- no PFX;
- transaction history;
- revocation;
- timestamp integration;
- identity-based publisher reputation;
- and direct compatibility with Windows Authenticode and MSIX.

### Disadvantages

- requires Azure subscription and Entra tenant;
- requires identity validation;
- current geographic and identity-type restrictions;
- UK individual developers are not eligible;
- service and network dependency;
- recurring cost;
- Azure resource administration;
- action and client-tool dependencies;
- short-lived certificates make timestamping mandatory;
- and public identity validation may take time or fail.

### Decision

Selected as the preferred production service, conditional on eligible organisational identity validation.

---

## 14. Option B — OV Certificate with HSM Custody

### Advantages

- widely available;
- supports UK publishers when Artifact Signing individual eligibility does not;
- Windows public trust;
- established Authenticode compatibility;
- supports MSIX;
- and can provide cloud or hardware-backed private-key custody.

### Disadvantages

- provider selection;
- annual cost;
- certificate renewal;
- possible hardware-token dependency;
- CI integration varies;
- private-key custody may be less operationally simple;
- revocation and audit interfaces vary;
- publisher reputation still builds over time;
- and some providers expose workflows that encourage local PFX use.

### Decision

Approved only as the production fallback when Artifact Signing organisational eligibility is unavailable or validation fails.

The chosen provider must support non-exportable key custody.

---

## 15. Option C — Microsoft Store Signing Only

### Advantages

- Store manages signing;
- globally trusted package;
- no production key custody;
- Store installation and update;
- and strong user recognition.

### Disadvantages

- Store-only distribution;
- certification and account dependency;
- no equivalent direct GitHub package trust;
- reduced private-preview flexibility;
- enterprise Store restrictions;
- and release timing dependency.

### Decision

Not selected as the only public trust path.

It may become an additional distribution path.

---

## 16. Option D — EV Certificate

EV certificates no longer provide a guaranteed immediate SmartScreen bypass.

They cost more and do not remove the need for reputation, timestamping, HSM custody or release controls.

Not selected solely for reputation.

---

## 17. Option E — Exportable PFX in CI

### Advantages

- common tooling;
- simple SignTool integration;
- and broad format support.

### Disadvantages

- long-lived secret;
- theft risk;
- base64-secret anti-pattern;
- accidental logging;
- runner residue;
- hard rotation;
- weak human control;
- and private key can be copied indefinitely.

### Decision

Prohibited for production signing.

---

## 18. Option F — USB Token on Developer Workstation

### Advantages

- non-exportable key;
- physical presence;
- no cloud signing service;
- and broad CA compatibility.

### Disadvantages

- difficult CI integration;
- availability bottleneck;
- founder workstation risk;
- driver and middleware dependency;
- manual upload step;
- physical loss;
- and poor automation.

### Decision

Permitted only as an OV fallback implementation when no approved cloud-HSM path is available.

It must use a dedicated signing workstation or controlled release session, not a general CI runner.

---

## 19. Option G — Self-Signed Production Certificate

Rejected because public Windows devices will not trust it without a manual trust-store change.

Self-signed certificates remain development-only.

---

## 20. Option H — Azure Key Vault with Issued Certificate

This can provide non-exportable cloud key custody when paired with a suitable public certificate and signing integration.

It is more operationally complex than Artifact Signing and requires separate certificate lifecycle management.

It remains a possible OV fallback implementation, not the preferred service.

---

## 21. Option I — No Code Signing

Rejected because MSIX installation requires signing and direct users need publisher and integrity verification.

---

## 22. Option J — Private Trust Only

Rejected for public distribution because the trust root is not broadly trusted by Windows.

Private Trust may later support enterprise application-control scenarios.

---

## 23. Comparison Matrix

Scores:

- 1 — poor
- 2 — weak
- 3 — acceptable
- 4 — strong
- 5 — excellent

| Criterion | Weight | Artifact Signing | OV HSM | Store Only | EV | PFX CI | USB Token | Self-Signed | Key Vault OV | No Signing |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Public Windows trust | 5 | 5 | 5 | 5 | 5 | 5 | 5 | 1 | 5 | 1 |
| UK founder eligibility | 5 | 3 | 5 | 4 | 5 | 5 | 5 | 5 | 5 | 5 |
| Non-exportable key | 5 | 5 | 5 | 5 | 5 | 1 | 5 | 2 | 5 | 1 |
| CI integration | 5 | 5 | 3 | 3 | 3 | 5 | 1 | 4 | 4 | 5 |
| OIDC or short-lived auth | 5 | 5 | 3 | 3 | 3 | 1 | 1 | 1 | 5 | 5 |
| Certificate lifecycle | 4 | 5 | 3 | 5 | 3 | 2 | 3 | 1 | 3 | 5 |
| Revocation | 5 | 5 | 4 | 5 | 4 | 4 | 4 | 1 | 4 | 1 |
| Auditability | 5 | 5 | 4 | 4 | 4 | 2 | 3 | 1 | 4 | 1 |
| Direct distribution | 5 | 5 | 5 | 1 | 5 | 5 | 5 | 1 | 5 | 1 |
| Small-team operation | 4 | 5 | 3 | 4 | 2 | 3 | 2 | 4 | 3 | 5 |
| Publisher continuity | 5 | 5 | 4 | 5 | 4 | 4 | 4 | 1 | 4 | 1 |
| Incident response | 5 | 5 | 4 | 4 | 4 | 2 | 3 | 1 | 4 | 1 |
| **Indicative result** |  | **First choice** | **Fallback** | Additional channel | Not justified | Prohibited | Conditional | Dev only | Fallback option | Rejected |

---

## 24. Decision

Opure will provisionally adopt:

> **Microsoft Artifact Signing Public Trust as the preferred production Authenticode and MSIX signer under an eligible validated Opure legal organisation, with a non-exportable HSM-backed OV certificate as the approved fallback for a UK individual publisher, and a layered source, build, binary, package and immutable-release trust chain.**

No direct public package is released until one production path is accepted.

This decision does not approve:

- exportable production PFX files;
- CI client secrets where OIDC is possible;
- unscoped Azure Contributor access for the signer;
- pull-request signing;
- self-signed public packages;
- automatic signing of arbitrary refs;
- mutable release assets;
- unsiged release tags;
- or reliance on reputation alone.

---

# 25. Publisher Identity Gate

Before the first public package, the founder must record:

- legal publisher name;
- legal entity type;
- country;
- registered address;
- verified domain;
- primary and secondary administrative contacts;
- certificate subject preview;
- Stable MSIX publisher value;
- Preview MSIX publisher value;
- package-family implications;
- and continuity plan.

The certificate subject must be treated as a release compatibility input.

A publisher spelling, legal suffix, country or organisation change can alter the certificate subject and break package update continuity.

---

## 25.1 Eligible Organisation Path

The preferred path requires:

- an eligible UK legal organisation;
- Azure billing identity matching that organisation;
- a Microsoft Entra tenant controlled by that organisation;
- Artifact Signing identity validation;
- and a Public Trust certificate profile whose subject matches the intended MSIX Publisher.

Identity validation is an administrative operation and is not delegated to CI.

---

## 25.2 Individual UK Path

If Opure is still published by an individual UK developer:

- Artifact Signing Public Trust is currently unavailable;
- a publicly trusted OV certificate must be procured through an eligible provider;
- its key must be non-exportable;
- CI or release tooling must use a secure remote-signing or hardware-token interface;
- and the MSIX Publisher must match that certificate subject.

The project must not misrepresent an individual as an organisation to obtain a certificate.

---

## 25.3 Blocked Path

If neither an eligible organisation nor an accepted OV certificate exists:

- direct public MSIX distribution is blocked;
- self-signed packages remain limited to development and controlled testers;
- Microsoft Store signing may be evaluated as a separate distribution path;
- and no documentation should describe an untrusted package as production ready.

---

# 26. Artifact Signing Resource Model

The preferred Azure resource structure should include:

```text
Dedicated Azure subscription or tightly controlled resource group
└── Artifact Signing account
    ├── Public identity validation
    ├── Public Trust certificate profile
    └── Signing transaction history
```

A separate Public Trust Test profile may be used for controlled integration testing, but it is not publicly trusted.

---

## 26.1 Resource Naming

Resource names should identify:

- Opure;
- trust class;
- environment;
- and purpose.

Names must not include secrets.

Conceptual examples:

```text
opure-artifact-signing-prod
opure-public-trust
opure-public-test
```

Exact names remain implementation details.

---

## 26.2 Region

Select an Artifact Signing region supported by the service and appropriate for the publisher.

Record the exact account endpoint in release configuration.

A region change is a signing-infrastructure change.

---

## 26.3 Resource Protection

Production signing resources should use:

- Azure resource locks where supported;
- cost and transaction alerts;
- restricted role assignment;
- MFA for human administrators;
- reviewed administrative groups;
- and Azure activity-log retention.

Deletion must not be part of ordinary cleanup scripts.

---

# 27. Azure Role Separation

Human and workload roles are separated.

## 27.1 Identity Administration

Only an authorised human administrator may manage:

- identity validation;
- account creation;
- certificate-profile creation;
- certificate revocation;
- and role assignment.

This identity requires strong MFA or passkey protection.

---

## 27.2 CI Signer

The GitHub workload identity receives only:

```text
Artifact Signing Certificate Profile Signer
```

at the exact production certificate-profile scope.

It receives no:

- Owner;
- Contributor;
- User Access Administrator;
- Identity Verifier;
- subscription-wide signing;
- or unrelated Azure access.

---

## 27.3 Release Publisher

The job that publishes a GitHub release should be separate from the Azure signer identity.

The ability to sign a file should not automatically grant the ability to:

- push source;
- move a Git tag;
- publish a release;
- modify Azure roles;
- or revoke a certificate.

---

# 28. GitHub OIDC Authentication

The production signing workflow uses GitHub OIDC to obtain short-lived Azure access.

No Azure client secret is stored.

Required workflow permissions should be limited to:

```yaml
permissions:
  contents: read
  id-token: write
```

Additional permissions belong to separate jobs.

`id-token: write` permits requesting an OIDC token; Azure trust policy determines actual resource access.

---

## 28.1 Federated Credential Scope

The Azure federated credential should trust only:

- the exact GitHub owner;
- exact repository;
- protected release-signing environment;
- expected audience;
- and, where available, immutable owner and repository identifiers.

The subject should conceptually bind to:

```text
repo:<owner>/<repository>:environment:release-signing
```

or the newer immutable repository-ID claim model where supported.

---

## 28.2 No Branch Wildcard

Do not create a broad federated credential that trusts every branch, pull request or repository in an organisation.

---

## 28.3 GitHub Environment

Use a protected environment such as:

```text
release-signing
```

The environment should restrict:

- permitted branches or tags;
- workflow source;
- deployment actors;
- and reviewer requirements when a second maintainer exists.

The initial founder-only stage still requires an explicit manual release action and signed tag approval.

---

## 28.4 Repository Transfer and Rename

Repository rename, owner transfer or organisation migration can change OIDC claims.

Before any transfer:

- disable signing federation;
- review new claims;
- update the federated credential;
- run a non-production signing test;
- and re-enable production signing deliberately.

---

## 28.5 OIDC Metadata

The following values are identifiers, not secrets:

- Azure tenant ID;
- subscription ID;
- client ID;
- Artifact Signing endpoint;
- account name;
- certificate-profile name.

They may be stored as protected environment variables or repository configuration.

They must still be change controlled.

---

# 29. Workflow Trigger Policy

Production signing may be triggered only by:

- approved manual `workflow_dispatch`;
- an approved release-candidate workflow;
- or an exact trusted release tag path.

It may not be triggered by:

- `pull_request`;
- fork pull request;
- arbitrary branch push;
- untrusted workflow call;
- issue comment;
- or user-supplied URL.

---

## 29.1 Exact Commit

The signing job receives an artefact manifest tied to:

- repository;
- commit SHA;
- version;
- unsigned file hashes;
- producer workflow;
- and producer attestation.

It does not accept an arbitrary filesystem path without a manifest.

---

## 29.2 Candidate Verification

Before OIDC authentication, verify:

- source commit is trusted;
- version is intended;
- build workflow passed;
- candidate artefact hash matches;
- release evidence is complete enough to sign;
- and no public version collision exists.

---

## 29.3 Signing Permission Timing

The signing job should request OIDC only after local candidate verification.

---

# 30. GitHub Action Policy

The Artifact Signing GitHub Action and Azure login Action must be:

- referenced by full commit SHA;
- recorded with a human-readable release comment;
- included in the workflow dependency inventory;
- reviewed for permissions and changes;
- and updated through a focused pull request.

Mutable references such as `@main` or `@v1` are prohibited.

---

## 30.1 Action Alternatives

If the Action becomes unavailable or unsuitable, use:

- SignTool with the Artifact Signing client integration;
- Artifact Signing SDK;
- or another official supported integration

through a reviewed repository script.

The release trust model must not depend on one Marketplace wrapper.

---

# 31. Signing Runner

Production signing runs on a clean GitHub-hosted Windows runner or another ephemeral approved Windows signing environment.

The runner must not be a general founder workstation.

A self-hosted signer is accepted only when:

- required by the OV fallback;
- dedicated to signing;
- isolated;
- cleaned or reimaged;
- and manually gated.

---

## 31.1 No Source Compilation

The signer downloads a verified candidate.

It does not:

- restore NuGet packages;
- compile source;
- regenerate resources;
- or modify version metadata.

---

## 31.2 Network

The signing job may access only:

- GitHub artefact storage;
- Microsoft Entra;
- Artifact Signing endpoint;
- timestamp authority;
- and required verification endpoints.

External network use is recorded.

---

## 31.3 Temporary State

Signing metadata and temporary files belong under the runner temporary directory.

They are removed at job completion.

No private key material should ever be present.

---

# 32. Signing Targets

The production signing set includes:

- first-party `.exe` files;
- first-party `.dll` files distributed to end users;
- first-party native DLLs produced by Opure;
- first-party `.ps1` scripts shipped to end users;
- the final `.msix`;
- and the future `.msixbundle`.

The signing manifest identifies every intended signing target.

---

## 32.1 Executables

Every first-party user-mode executable in the public package or diagnostic ZIP should be Authenticode signed.

---

## 32.2 Managed DLLs

First-party managed DLLs distributed in the public package should be Authenticode signed unless a measured compatibility or package-size issue is recorded.

This provides integrity and publisher context when files are inspected outside the package.

---

## 32.3 Native DLLs

First-party native binaries must be signed.

Third-party native binaries retain their upstream signatures where present.

---

## 32.4 PowerShell Scripts

A shipped PowerShell script must:

- have a clear product need;
- be Authenticode signed;
- avoid embedded secrets;
- and work under the documented execution policy.

Build-only repository scripts are not public release payloads and do not require production Authenticode signing.

---

## 32.5 Configuration and Documentation

JSON, Markdown, licence and configuration files are covered by:

- package signature;
- release manifest hashes;
- and attestations.

They are not individually Authenticode signed.

---

## 32.6 Diagnostic ZIP

The ZIP container is not treated as an Authenticode target.

Its first-party executable contents are signed.

The ZIP itself is protected by:

- SHA-256;
- release manifest;
- build attestation;
- immutable release;
- and signed release tag.

---

# 33. Third-Party Binary Policy

Do not automatically re-sign a third-party binary.

Re-signing can:

- obscure upstream publisher identity;
- affect support;
- alter signature chains;
- and imply Opure authored the binary.

A third-party binary should be:

- obtained from a locked package or approved source;
- hash verified;
- signature verified where available;
- inventoried;
- licence reviewed;
- and packaged unchanged.

Re-sign only when:

- upstream permits it;
- Windows packaging requires it;
- the original signature cannot survive a legitimate transformation;
- and the release record explains the decision.

---

# 34. Signing Order

The standard order is:

1. verify unsigned build manifest;
2. verify every unsigned input hash;
3. sign first-party PE files;
4. sign shipped PowerShell scripts;
5. verify every individual signature;
6. update the component manifest with signed hashes and certificate thumbprints;
7. compose the MSIX from exact signed files;
8. build the unsigned MSIX;
9. inspect the package layout;
10. sign and timestamp the MSIX;
11. verify package signature and publisher;
12. install on a second clean Windows environment;
13. run package acceptance;
14. generate final release hashes;
15. create or verify the human-signed Git tag;
16. generate attestations;
17. attach exact assets to draft release;
18. verify again;
19. publish immutable release.

No file included in the package may be modified after its individual signature and before package construction unless the signing manifest explicitly requires that transformation.

---

# 35. Algorithms

Use:

```text
File digest: SHA-256
Timestamp protocol: RFC 3161
Timestamp digest: SHA-256
Release file hash: SHA-256
```

SHA-1 is prohibited for new signatures and release manifests.

A future stronger digest may be added while retaining SHA-256 for ecosystem compatibility.

---

# 36. Timestamping

Timestamping is mandatory for every production Authenticode and MSIX signature.

The preferred Artifact Signing timestamp authority is the Microsoft Artifact Signing RFC 3161 service documented for the selected integration.

---

## 36.1 Why Mandatory

Artifact Signing certificates are short lived.

Without a valid timestamp, a signature can fail after certificate expiry.

A valid timestamp allows Windows to evaluate the certificate at signing time.

---

## 36.2 Timestamp Failure

If timestamping fails:

- the signature is not accepted for release;
- the file is quarantined;
- no retry overwrites evidence silently;
- and a new signing attempt produces a new transaction record.

---

## 36.3 Timestamp Verification

Verification must confirm:

- timestamp exists;
- timestamp signature is valid;
- timestamp authority is trusted;
- signing certificate was valid at timestamp;
- and file hash matches.

---

# 37. Certificate Capture

For every signed file, record:

- subject distinguished name;
- issuer;
- serial number;
- SHA-256 certificate fingerprint where available;
- leaf thumbprint used by the signing service;
- validity start and end;
- timestamp time;
- timestamp authority;
- digest algorithm;
- signature verification result;
- signing transaction or correlation identifier;
- and release version.

---

## 37.1 Short-Lived Certificate Rotation

Artifact Signing may issue different short-lived certificates over time.

The release evidence must not assume one permanent leaf thumbprint.

The stable identity is the validated publisher and certificate chain.

The exact leaf thumbprints remain essential for revocation and investigation.

---

## 37.2 One Release Window

Where practical, sign one release's files in one bounded signing window.

This reduces certificate and investigation complexity.

It is not a correctness requirement if the service rotates the certificate.

---

# 38. Verification

Every signature is verified after signing and on a separate clean Windows environment.

Conceptual commands include:

```powershell
signtool verify /pa /all /v <file>
Get-AuthenticodeSignature <file>
Get-FileHash <file> -Algorithm SHA256
```

The exact pinned SignTool version and flags belong in the implementation.

---

## 38.1 Two-Stage Verification

Stage 1 verifies immediately after signing.

Stage 2 verifies after:

- artefact upload;
- artefact download;
- and transfer to the clean package-test job.

---

## 38.2 Package Verification

MSIX verification must confirm:

- signature;
- publisher;
- package identity;
- package version;
- package architecture;
- timestamp;
- block-map integrity;
- and installability.

---

## 38.3 Offline Verification

Where practical, validate that a previously trusted timestamped package remains installable without contacting the signing service.

Revocation checks may still depend on Windows trust behaviour and network availability.

---

# 39. Release Manifest

The final machine-readable release manifest should include:

```text
format_version
product_version
source_commit
signed_tag
build_attestation
sbom_attestation
publisher
package_family
artifact_signing_account
certificate_profile
signing_integration
signing_action_commit
windows_sdk_version
signtool_version
timestamp_authority
files[]
```

Each `files[]` entry includes:

```text
path
role
sha256
size
authenticode_signed
signer_subject
issuer
certificate_fingerprint
timestamp
verification_result
source_component
```

No secret or Azure access token is included.

---

# 40. Human Release Tag Signing

The source release tag is signed by a dedicated SSH release-signing key.

The key must be:

- distinct from repository authentication keys;
- held on a hardware security key;
- protected by PIN;
- require physical user presence;
- registered with GitHub as a signing key;
- and listed in the repository release-signer policy.

---

## 40.1 Why SSH

SSH tag signing is selected because:

- Git supports it;
- GitHub verifies it;
- it is simpler to operate than a full OpenPGP hierarchy;
- hardware-backed OpenSSH keys are available;
- and local verification can use an allowed-signers file.

---

## 40.2 Signing Key Separation

The release signing key must not be reused for:

- SSH repository authentication;
- Windows Authenticode;
- Azure administration;
- or routine commit signing.

---

## 40.3 Backup Signer

Maintain a second hardware-backed release signing key as a sealed backup or alternate authorised signer.

It has a separate public key and recorded activation date.

A backup is not a clone of the same non-exportable key.

---

## 40.4 Allowed Signers

Store public release-signing keys in a file such as:

```text
security/release-signers.allowed
```

The file should record:

- signer identity;
- public key;
- effective date;
- retirement date;
- revocation status;
- and purpose.

Changes require security review and founder approval.

---

## 40.5 Tag Message

The signed annotated tag should include:

- Opure version;
- source commit;
- release channel;
- candidate manifest hash;
- final package hash or evidence reference;
- and release record reference.

---

## 40.6 Verification

Before release:

```powershell
git tag -v v<version>
```

or the configured SSH verification equivalent must succeed locally.

GitHub must also display the tag signature as verified.

The GitHub badge is additional evidence, not the local source of truth.

---

## 40.7 Tag Timing

The human-signed tag is created only after the candidate source and package gates pass.

If the versioning system requires public-release stamping before the tag exists, the trusted release workflow may use the approved `PublicRelease` override from ADR-0012 and then verify the tag points to exactly that candidate commit.

---

# 41. Commit Signing

Routine commits are not required to use the release tag key.

A future branch policy may require verified commit signatures for sensitive paths.

The release tag remains the explicit source-release approval boundary.

---

# 42. GitHub Artefact Attestations

Generate a build-provenance attestation for:

- final MSIX;
- diagnostic ZIP;
- release manifest;
- and public SDK packages where supported.

Generate an SBOM attestation for the release package where supported.

---

## 42.1 Attestation Claim

The attestation should identify:

- repository;
- workflow;
- commit;
- triggering event;
- environment;
- and artefact digest.

---

## 42.2 Verification

Release documentation should provide a GitHub CLI verification path when public distribution begins.

The release evidence records the attestation identifiers.

---

## 42.3 Limitations

An attestation does not replace:

- Authenticode;
- tag signing;
- package signature;
- or code review.

---

# 43. Immutable Release

Before public distribution, enable GitHub immutable releases.

The publication process should:

1. create draft release;
2. attach all exact assets;
3. verify names, hashes and attestations;
4. verify tag signature;
5. verify release notes;
6. publish;
7. confirm immutability;
8. download assets;
9. verify again.

No published asset is replaced under the same version.

---

# 44. NuGet Package Trust

Public Plugin SDK packages require a separate trust treatment.

Initially:

- build from the same signed-tagged source;
- generate package hash;
- generate build attestation;
- publish only through the approved nuget.org organisation;
- rely on nuget.org's repository signature;
- verify the repository signature after publication;
- and record package identity and content hash in release evidence.

---

## 44.1 Author Signing Deferred

Author-signing NuGet packages is not required initially because the standard NuGet author-signing tools expect access to an X.509 private key through a file or certificate store, while the preferred Artifact Signing key is not exported.

Opure will not introduce an exportable PFX solely to author-sign NuGet packages.

A future author-signing path requires:

- a supported non-exportable integration;
- nuget.org certificate registration;
- timestamping;
- restore verification;
- and a focused ADR amendment.

---

## 44.2 NuGet Verification

After publication, verify:

- package ID;
- version;
- package content hash;
- repository signature;
- provenance;
- and release-manifest entry.

---

# 45. Checksum and Evidence Files

Checksums and evidence files are not individually Authenticode signed.

Their trust comes from:

- immutable release;
- build attestation;
- signed release tag;
- and cross-referenced hashes.

A future detached signature format may be added only after a tooling decision.

---

# 46. Development Signing

Development and controlled test packages may use:

- a self-signed development certificate;
- or an Artifact Signing Public Trust Test profile.

Neither is publicly trusted.

---

## 46.1 Development Identity

Development signatures must use a visibly non-production subject and package identity.

Examples should contain terms such as:

```text
Opure Development
Test Only
Not for Distribution
```

---

## 46.2 Development Certificate Storage

A development PFX may exist only:

- on a developer's controlled machine;
- under an ignored protected path;
- or ephemerally inside one CI job.

It must never be:

- committed;
- uploaded as a normal artefact;
- copied into the Opure Vault as the production signing model;
- or shared with public testers.

---

## 46.3 Trust Installation

Installing a development certificate into a Windows trust store is an explicit action.

Documentation must state:

- which store is changed;
- whether elevation is required;
- which subject is trusted;
- and how to remove it.

---

## 46.4 CI Development Signing

A package acceptance job may generate an ephemeral self-signed certificate, trust only its public certificate in the disposable test machine, sign `Opure.Dev`, run tests and destroy the environment.

The private key does not leave the job.

---

# 47. OV Fallback Requirements

If an OV certificate is required, provider selection must satisfy:

- public Windows trust;
- suitable publisher identity;
- code-signing extended key usage;
- RSA 2048-bit or stronger ecosystem support;
- SHA-256;
- RFC 3161 timestamping;
- non-exportable private key;
- HSM or hardware-token custody;
- auditable signing operations;
- revocation support;
- renewal support;
- and documented CI or controlled-release integration.

---

## 47.1 Preferred OV Implementation

Preference order:

1. provider-managed cloud HSM with short-lived workload authentication;
2. Azure Key Vault or equivalent non-exportable HSM-backed key with supported signing integration;
3. dedicated hardware token used in a controlled signing workstation;
4. no release.

---

## 47.2 Prohibited OV Implementation

Do not:

- request an exportable PFX for production;
- place a PFX in GitHub Secrets;
- base64-encode a PFX in workflow YAML;
- store a PFX in a build cache;
- attach a PFX to a workflow artefact;
- place a PFX in a source repository;
- or share a token PIN through a command-line argument.

---

## 47.3 OV CI Authentication

Where the provider supports federation, use OIDC.

Otherwise use the shortest-lived provider credential available, limited to:

- exact signer;
- exact project;
- exact environment;
- and exact operation.

A long-lived API token is a temporary exception requiring rotation and expiry.

---

## 47.4 Hardware-Token Signing

If a hardware token is the only viable fallback:

- use a dedicated signing workstation or clean VM;
- require physical presence;
- verify unsigned candidate hashes before signing;
- sign files through a scripted deterministic manifest;
- verify signatures locally;
- upload only signed artefacts and evidence;
- wipe staging state;
- and never register the token on a general self-hosted pull-request runner.

---

# 48. SmartScreen

SmartScreen reputation is expected to build over time.

Opure must not promise that:

- an OV certificate;
- an EV certificate;
- Artifact Signing;
- or a valid package signature

will eliminate every warning immediately.

---

## 48.1 User Guidance

Release instructions should tell the developer to verify:

- publisher;
- version;
- signature;
- checksum;
- and release source.

They must not advise disabling SmartScreen.

---

## 48.2 False Positive Response

For an unexpected warning or antivirus detection, record:

- release version;
- file hash;
- signer subject;
- certificate thumbprint;
- timestamp;
- detection product;
- and submission reference.

Submit the exact signed file through the vendor's false-positive process.

---

# 49. Signing Transaction Records

For every release, preserve:

- Azure or provider transaction identifier;
- start and completion time;
- signer workload identity;
- certificate profile;
- leaf certificate thumbprint;
- file digest;
- timestamp result;
- Action or client version;
- runner identity;
- and verification result.

---

## 49.1 Retention

Signing transaction evidence for a public release should be retained for the supported life of the release and the security-record retention period.

Temporary CI logs are not the only record.

---

## 49.2 Privacy

Do not expose:

- access tokens;
- internal tenant diagnostics;
- unnecessary personal identity documents;
- billing details;
- or private administrator information

in public release evidence.

---

# 50. Monitoring and Alerts

Production signing infrastructure should alert on:

- signing outside expected release windows;
- unexpected signer identity;
- failed signing bursts;
- unfamiliar certificate profile;
- role assignment change;
- federated credential change;
- identity-validation change;
- profile deletion;
- certificate revocation;
- unusual transaction volume;
- and cost anomaly.

---

## 50.1 Release Window

A signing transaction outside an approved release or rehearsal window requires investigation.

---

## 50.2 Zero-Use Expectation

Production signing should normally have no activity between releases.

This makes unexpected use easier to detect.

---

# 51. Administrative Security

Human Azure administrators should use:

- phishing-resistant MFA or passkey where available;
- dedicated administrative account;
- no shared credentials;
- least privilege;
- protected recovery methods;
- and periodic access review.

---

## 51.1 Founder Stage

The founder may initially hold several human roles.

The CI workload must still remain separately scoped.

---

## 51.2 Team Growth

When a second authorised maintainer exists:

- separate identity administration from release approval;
- require independent review for production signer changes;
- and require two-person approval for revocation and publisher migration where practical.

---

# 52. Infrastructure as Code

Non-secret signing-resource configuration should be represented in reviewed infrastructure code where practical.

It may include:

- account;
- certificate profile;
- role assignment;
- federated credential;
- environment names;
- and alert policy.

Identity validation and sensitive legal verification remain human portal operations.

---

## 52.1 Drift Detection

Regularly compare deployed signing resources with the approved configuration.

Unexpected drift blocks release.

---

# 53. Publisher Continuity

The Opure publisher identity is a long-lived compatibility surface.

---

## 53.1 Certificate Renewal

Renewal or short-lived certificate rotation is acceptable when the validated publisher subject and trust chain remain compatible.

---

## 53.2 Legal Name Change

A legal name change requires:

- certificate subject analysis;
- MSIX package-family impact;
- migration plan;
- public notice;
- update test;
- and ADR amendment.

---

## 53.3 Service Migration

Moving from OV to Artifact Signing or between signing providers requires:

- same intended publisher identity where possible;
- side-by-side signature verification;
- package-update test;
- SmartScreen impact review;
- and release note.

---

# 54. Revocation

Certificate revocation is a high-impact irreversible action.

---

## 54.1 Revocation Triggers

Potential triggers include:

- suspected signing-account compromise;
- unauthorised signing transaction;
- incorrect certificate subject;
- compromised workload identity;
- provider incident;
- maliciously signed release;
- or private-key compromise in the OV fallback.

---

## 54.2 Artifact Signing Revocation

Artifact Signing supports revoking certificate thumbprints.

The responder must identify:

- exact thumbprint;
- issuance interval;
- signing transactions;
- files signed;
- intended revocation time;
- and user impact.

Revocation may invalidate files signed by that certificate from the chosen revocation time.

---

## 54.3 No Casual Revocation

Do not revoke merely because:

- a certificate expired;
- a new certificate was issued;
- or a release was superseded.

Timestamped historical signatures are expected to outlive certificate validity.

---

## 54.4 Revocation Approval

Before revocation, require:

- Security Owner;
- Release Owner;
- founder;
- and provider support where needed.

During an active compromise, the founder may act immediately and document afterward.

---

# 55. Signing Incident Response

On suspected signing compromise:

1. stop release workflows;
2. disable the GitHub release-signing environment;
3. remove or disable OIDC federated credentials;
4. remove CI signer role assignment;
5. disable or isolate the certificate profile;
6. preserve workflow and provider logs;
7. identify all transactions and signed files;
8. compare them with approved release manifests;
9. revoke affected certificate thumbprints when justified;
10. remove compromised Git tag signing keys from active trust;
11. mark affected releases;
12. publish a security notice;
13. create new signing identities or profiles;
14. rebuild from trusted source;
15. sign a new patch version;
16. verify on clean machines;
17. and complete a post-incident review.

---

## 55.1 No Silent Replacement

An affected release asset is not silently replaced.

Publish a new version and mark the old version:

- Withdrawn;
- Security Affected;
- or Superseded.

---

## 55.2 User Guidance

Incident guidance should state:

- affected versions;
- affected hashes;
- affected signer thumbprints;
- safe replacement;
- uninstall or upgrade steps;
- and data impact.

---

# 56. Workload Identity Rotation

Rotating the Entra workload identity should:

- create the new identity;
- configure exact OIDC trust;
- assign signer role at profile scope;
- run a Public Trust Test rehearsal;
- run a production-shaped rehearsal without publication;
- remove the old role;
- remove the old federated credential;
- and record the transition.

No overlap longer than necessary.

---

# 57. Release Tag Key Rotation

A release SSH key is rotated when:

- hardware is lost;
- PIN is suspected compromised;
- key reaches planned retirement;
- signer changes;
- or cryptographic policy changes.

---

## 57.1 Rotation Process

1. create new hardware-backed key;
2. register public key with GitHub;
3. add to allowed-signers policy;
4. sign a rotation record with the old key if available;
5. verify the new key;
6. set activation date;
7. retire old key;
8. retain old public key for historical verification;
9. and document revocation or retirement status.

---

## 57.2 Compromised Tag Key

If compromised:

- remove it from active GitHub signing keys;
- mark it revoked in repository signer policy;
- identify tags signed during the suspect period;
- preserve historical public key;
- and publish a trust advisory.

The Git objects remain immutable historical evidence, but the signer trust decision changes.

---

# 58. Backup and Recovery

Production Authenticode keys are service or provider managed and are not backed up by Opure as raw key material.

Recovery relies on:

- provider continuity;
- identity validation;
- certificate-profile recreation;
- publisher continuity;
- and documented role restoration.

---

## 58.1 Release Tag Backup

Maintain two independent hardware-backed SSH release-signing keys.

Store:

- devices separately;
- PIN recovery procedures securely;
- public keys in repository;
- and activation status in release policy.

---

## 58.2 No Seed in Repository

No hardware-key recovery seed or private material is committed or uploaded.

---

# 59. Release Trust Verification Order

Before public release, verify in this order:

1. source commit exists on trusted branch;
2. candidate version matches ADR-0012;
3. build attestation matches commit;
4. unsigned manifest matches candidate;
5. signed PE file hashes match signed component manifest;
6. PE signatures and timestamps validate;
7. MSIX contents match signed PE files;
8. MSIX signature, publisher and timestamp validate;
9. clean-machine package tests pass;
10. release tag points to source commit;
11. tag signature validates against repository policy;
12. release assets match manifest;
13. SBOM and attestation match package digest;
14. draft release notes match version and hashes;
15. immutable publication succeeds;
16. downloaded public assets verify again.

A failure at any step stops publication.

---

# 60. Release Trust States

A release may be:

- Unsigned Development;
- Test Signed;
- Candidate Unsigned;
- Candidate File Signed;
- Candidate Package Signed;
- Candidate Verified;
- Tag Approved;
- Draft Published Privately;
- Public Immutable;
- Superseded;
- Security Affected;
- or Withdrawn.

---

## 60.1 Invalid Transitions

Prohibited transitions include:

- Test Signed → Public Immutable;
- Candidate Unsigned → Public;
- Package Signed → Recompiled;
- Tag Approved → Different Source Commit;
- Public Immutable → Asset Replaced;
- Revoked Signer → New Release;
- and Failed Verification → Published.

---

# 61. Public Release Verification Documentation

A public release should document:

- exact tag;
- tag signature status;
- publisher subject;
- package family;
- product version;
- MSIX deployment version;
- SHA-256;
- Authenticode verification command;
- Git tag verification command;
- attestation verification command;
- SBOM location;
- and known SmartScreen status.

---

## 61.1 Developer-Friendly Verification

Verification guidance should offer:

- a simple Windows path;
- a detailed security path;
- and machine-readable evidence.

---

## 61.2 No Mandatory Trust in Opure Tool

A user should not need to run an unverified Opure executable to verify the Opure installer.

Use Windows, Git and GitHub tools.

---

# 62. Package Integrity Enforcement

ADR-0013 may enable Windows MSIX package-integrity enforcement after compatibility testing.

Code signing remains required whether or not that additional runtime enforcement is enabled.

---

# 63. Diagnostic ZIP Verification

For the diagnostic ZIP:

1. verify ZIP SHA-256;
2. verify GitHub attestation;
3. extract safely;
4. verify every first-party EXE and DLL signature;
5. verify release component manifest;
6. use isolated diagnostic profile.

---

# 64. Public SDK Verification

For a NuGet package:

1. verify package ID and version;
2. verify package content hash;
3. verify nuget.org repository signature;
4. verify build attestation where supported;
5. compare with release manifest;
6. verify source repository and tag.

---

# 65. No Signing During Pull Requests

Pull-request jobs may:

- validate signing manifests;
- test file-selection logic;
- use ephemeral self-signed certificates;
- verify test signatures;
- and test failure paths.

They may not receive access to:

- production Artifact Signing profile;
- OV signing provider;
- release tag hardware key;
- production GitHub environment;
- or publication permission.

---

# 66. Signing Rehearsals

Run a production-shaped rehearsal:

- before first preview;
- before first beta;
- before first RC;
- before first stable;
- after Action update;
- after Windows SDK update;
- after signing service change;
- after OIDC change;
- after publisher change;
- and after incident recovery.

Use a test identity or disposable version where public signature pollution would be undesirable.

---

# 67. Signing Rate and Cost Controls

Signing tools should use an explicit manifest and avoid broad recursive signing of unknown directories.

Benefits include:

- predictable transaction count;
- cost control;
- accidental file prevention;
- and auditable scope.

Set budget and transaction alerts.

---

# 68. Release File Selection

The signing manifest is generated from the release component manifest.

It must not accept arbitrary wildcards such as:

```text
**\*.exe
```

without an allowlisted root and expected inventory.

Unexpected signable files fail the release.

---

# 69. Signature Append Policy

Do not append multiple publisher signatures by default.

A dual-signing transition requires:

- old and new trust rationale;
- file compatibility test;
- package test;
- and incident or migration record.

SHA-1 dual signing is prohibited.

---

# 70. Strong Naming

.NET strong naming is separate from Authenticode.

This ADR does not adopt strong naming.

A strong-name signature is not a public publisher signature and does not replace Authenticode.

---

# 71. Assembly Version and Signature

Signing must occur after:

- product version;
- assembly version;
- file version;
- informational version;
- Source Link;
- and binary metadata

are final.

Any metadata change after signing requires rebuilding and re-signing as a new private candidate.

---

# 72. Reproducibility and Signing

Unsigned deterministic build outputs should be compared before signing.

Authenticode and timestamps intentionally change file bytes.

Release evidence should retain:

- unsigned hash;
- signed hash;
- and signing metadata.

---

# 73. Signing Toolchain Inventory

Record and pin:

- Windows SDK Build Tools;
- SignTool;
- Artifact Signing client;
- Artifact Signing Action commit;
- Azure login Action commit;
- PowerShell or SDK modules;
- verification tools;
- and Git version.

---

## 73.1 Tool Upgrade

A signing-tool upgrade requires:

- source and release-note review;
- supply-chain review;
- signature output comparison;
- timestamp verification;
- clean-machine install;
- and rehearsal.

---

# 74. Timestamp Authority Change

Changing timestamp authority requires:

- trust-chain review;
- protocol verification;
- signing rehearsal;
- long-term validation test;
- and release record.

---

# 75. Certificate Profile Change

Changing certificate profile requires:

- subject comparison;
- MSIX Publisher comparison;
- package-family test;
- SmartScreen impact review;
- OIDC and RBAC update;
- and release rehearsal.

---

# 76. Multiple Products

The first Artifact Signing account may hold one production Opure profile.

A future separate product should use a separate certificate profile or account boundary according to risk and publisher identity.

---

# 77. Stable and Preview Signer

Stable and Preview may use the same validated publisher and production certificate profile.

Their MSIX package names and package families remain separate.

A separate Preview signing profile is optional and requires cost and incident-response justification.

---

# 78. Development Signer Separation

Development signing never uses the production Public Trust profile.

This prevents accidental trusted publication of development binaries.

---

# 79. Release Trust in the User Interface

The installed application may show:

- publisher;
- package signature state;
- release channel;
- source commit;
- build attestation reference;
- and release support state.

It must not claim it independently reverified every trust property unless it actually did.

---

# 80. Startup Self-Check

A packaged release may perform a bounded startup self-check of:

- package identity;
- product version;
- package version;
- channel;
- and component manifest consistency.

It should rely on Windows for package signature enforcement and avoid expensive full-file hashing on every launch unless recovery requires it.

---

# 81. Trust Centre Records

The Trust Centre may record:

- current package publisher;
- current signer thumbprint;
- package signature verification result;
- product version;
- release channel;
- prior version;
- update transition;
- and release evidence identifier.

No private signing credential enters Trust Centre.

---

# 82. User Data and Signing

Signing infrastructure must not receive:

- user source repositories;
- prompts;
- Vault contents;
- plugin state;
- or diagnostic data.

Only release artefacts and their digests are signed.

---

# 83. Threat Model

Relevant threats include:

- stolen signing key;
- forged publisher;
- malicious pull request;
- broad OIDC trust;
- compromised GitHub Action;
- compromised hosted runner;
- substituted candidate artefact;
- signing wrong version;
- signing unexpected file;
- unsigned DLL replacement;
- timestamp omission;
- tag impersonation;
- attestation omission;
- asset replacement;
- certificate-profile deletion;
- malicious revocation;
- compromised release SSH key;
- and hidden publisher migration.

---

# 84. Security Controls

Controls include:

- non-exportable production keys;
- short-lived workload authentication;
- exact OIDC claim binding;
- profile-scoped signer role;
- protected GitHub environment;
- SHA-pinned Actions;
- clean hosted runner;
- pre-sign hash manifest;
- explicit file allowlist;
- SHA-256 signatures;
- mandatory timestamps;
- second-machine verification;
- hardware-backed signed tags;
- attestation;
- immutable release;
- transaction monitoring;
- and rehearsed revocation.

---

# 85. Reliability Impact

Artifact Signing introduces an external service dependency at release time.

A service outage delays signing but does not prevent:

- local development;
- ordinary CI;
- candidate build;
- tests;
- or unsigned package validation.

Opure must not fall back automatically to a weaker production signer.

---

# 86. Privacy Impact

Artifact Signing processes release artefacts or file digests according to its integration model.

No user data should be present.

Azure identity validation processes legal and representative identity information outside the product repository.

That information must not be copied into public release evidence beyond the certificate subject already visible in signatures.

---

# 87. Performance Impact

Signing adds release time and network operations but not ordinary application runtime overhead.

Signing every first-party DLL increases transaction count.

The prototype should measure:

- transaction duration;
- total files;
- total cost;
- and release critical-path impact.

If per-file signing becomes excessive, the project may narrow the individually signed DLL set only through an evidence-backed amendment.

---

# 88. Testing Strategy

ADR-0008 applies.

Signing and trust require dedicated automated and manual tests.

---

## 88.1 Unit Tests

Test:

- signing manifest generation;
- expected file selection;
- path allowlist;
- release-state transitions;
- certificate metadata parsing;
- tag-message generation;
- signer policy parsing;
- and revocation impact calculation.

---

## 88.2 Integration Tests

Test:

- OIDC token exchange in a non-production profile;
- signer role scope;
- Artifact Signing action;
- SignTool integration;
- PE signing;
- timestamping;
- package signing;
- and transaction capture.

---

## 88.3 Negative Tests

Test:

- wrong repository;
- wrong environment;
- pull-request event;
- wrong ref;
- wrong candidate hash;
- missing attestation;
- unapproved file;
- wrong publisher;
- invalid timestamp;
- modified binary;
- and revoked signing key.

---

## 88.4 Tag Tests

Test:

- hardware-backed signed tag;
- local verification;
- GitHub verification;
- unregistered signing key;
- retired key;
- revoked key;
- wrong commit;
- lightweight tag;
- and unsigned tag.

---

## 88.5 Release Tests

Test the complete sequence from candidate download through immutable publication in a disposable repository or private test channel.

---

# 89. Prototype Plan

## 89.1 Prototype A — Development Signing

Create an ephemeral `Opure.Dev` certificate and sign representative EXE, DLL and MSIX files.

---

## 89.2 Prototype B — Artifact Signing Test Profile

Create an Artifact Signing Public Trust Test profile or equivalent controlled test profile.

Configure GitHub OIDC and sign a harmless test binary.

---

## 89.3 Prototype C — Role Scope

Prove that the CI identity:

- can sign with one test profile;
- cannot create resources;
- cannot change roles;
- cannot revoke;
- and cannot sign with another profile.

---

## 89.4 Prototype D — File Manifest

Generate explicit signing targets and reject unexpected PE files.

---

## 89.5 Prototype E — Timestamp

Verify signature after simulated certificate expiry conditions where practical.

---

## 89.6 Prototype F — Production Eligibility

Complete organisational Artifact Signing validation or document accepted OV fallback procurement and custody.

---

## 89.7 Prototype G — Hardware Tag

Create a dedicated hardware-backed SSH key, sign an annotated test tag and verify locally and on GitHub.

---

## 89.8 Prototype H — Attestation

Generate and verify build and SBOM attestations.

---

## 89.9 Prototype I — Immutable Release

Publish a disposable immutable release and verify that tag and assets cannot be replaced.

---

## 89.10 Prototype J — Incident

Simulate:

- OIDC disable;
- signer role removal;
- certificate thumbprint revocation decision;
- release withdrawal;
- and signer rotation.

---

# 90. Implementation Plan

1. Record founder review.
2. Decide legal publisher path.
3. Confirm Artifact Signing eligibility.
4. Create the Azure or fallback signing account.
5. Complete identity validation.
6. review certificate-subject preview;
7. freeze package Publisher value;
8. create Public Trust certificate profile;
9. create Public Trust Test profile if useful;
10. create Entra workload identity;
11. configure exact GitHub OIDC federation;
12. assign Certificate Profile Signer at profile scope;
13. configure `release-signing` environment;
14. pin signing Actions;
15. implement signing manifest;
16. sign representative PE files;
17. sign a test MSIX;
18. implement two-stage verification;
19. implement signing transaction capture;
20. acquire two hardware security keys;
21. create release SSH signing keys;
22. add allowed-signers policy;
23. sign and verify a test tag;
24. generate attestations;
25. enable immutable releases before public use;
26. write revocation and incident runbooks;
27. rehearse release;
28. complete security review;
29. accept, amend or reject the ADR.

---

# 91. Owners

| Area | Owner |
|---|---|
| Legal publisher decision | Founder |
| Identity validation | Founder or authorised publisher administrator |
| Signing service | Release Trust Owner |
| OIDC and RBAC | Security Owner |
| GitHub environment | CI Owner |
| Signing manifest | Release Engineering Owner |
| Package signature | Packaging Owner |
| Release tag key | Founder or Release Authority |
| Attestations | CI Owner |
| Revocation | Security and Founder |
| Incident response | Incident Response Owner |
| Public verification docs | Documentation Owner |

---

# 92. Suggested Repository Additions

```text
security/
├── release-signers.allowed
├── release-signing-policy.md
├── publisher-identity.md
└── signing-incident-runbook.md

eng/
├── sign.ps1
├── verify-signatures.ps1
├── verify-release-trust.ps1
└── common/
    └── Signing.psm1

src/Packaging/
└── Opure.Packaging.Windows/
    └── signing-manifest.json

tests/
└── Release/
    ├── Opure.Signing.UnitTests/
    └── Opure.Signing.IntegrationTests/
```

Private key material is excluded.

---

# 93. Signing Manifest Shape

Conceptual form:

```json
{
  "formatVersion": 1,
  "productVersion": "0.4.0-preview.1",
  "sourceCommit": "0123456789abcdef",
  "publisher": "<validated subject>",
  "targets": [
    {
      "path": "Opure.Desktop.exe",
      "type": "pe",
      "owner": "Opure.Desktop",
      "unsignedSha256": "<hash>",
      "required": true
    }
  ]
}
```

The final signed manifest adds signed hashes and verification metadata.

---

# 94. Release Signer Policy Shape

Conceptual allowed-signers entry:

```text
christopher.dyer@opure.example namespaces="git" ssh-ed25519-sk AAAA...
```

The exact Git allowed-signers syntax must be validated.

A companion policy records:

- identity;
- key fingerprint;
- hardware key label;
- effective date;
- status;
- and release authority.

---

# 95. Signing Workflow Shape

Conceptual only:

```yaml
permissions:
  contents: read
  id-token: write

jobs:
  sign:
    environment: release-signing
    runs-on: windows-2025
    steps:
      - download verified candidate
      - verify candidate manifest and attestation
      - authenticate to Azure with OIDC
      - sign allowlisted files
      - verify every signature
      - build MSIX from signed files
      - sign and timestamp MSIX
      - verify package
      - upload signed candidate and evidence
```

All external Actions are pinned to full commit SHAs in the real workflow.

---

# 96. Release Trust Checklist

Before signing:

- [ ] Version approved.
- [ ] Source commit trusted.
- [ ] Candidate build passed.
- [ ] Candidate attestation verified.
- [ ] Unsigned hashes verified.
- [ ] File inventory approved.
- [ ] No version collision.
- [ ] Publisher and package family match.
- [ ] Release environment approved.

After file signing:

- [ ] Every expected file signed.
- [ ] No unexpected file signed.
- [ ] SHA-256 used.
- [ ] Timestamp present.
- [ ] Publisher correct.
- [ ] Thumbprints recorded.
- [ ] Signed hashes recorded.

After package signing:

- [ ] MSIX publisher correct.
- [ ] MSIX timestamp valid.
- [ ] Package install succeeds.
- [ ] Package acceptance passes.
- [ ] Final package hash recorded.

Before publication:

- [ ] Hardware-signed tag valid.
- [ ] GitHub tag verification valid.
- [ ] Build attestation valid.
- [ ] SBOM attestation valid.
- [ ] Release assets match manifest.
- [ ] Draft release complete.
- [ ] Immutability enabled.
- [ ] Founder approval recorded.

After publication:

- [ ] Public assets downloaded.
- [ ] Hashes verified.
- [ ] Signatures verified.
- [ ] Attestations verified.
- [ ] Release immutable.
- [ ] Signing transaction archived.
- [ ] Support state updated.

---

# 97. Release Blocking Conditions

A public release is blocked when:

- legal publisher is unresolved;
- production signing path is unavailable;
- Artifact Signing identity validation is incomplete;
- OV fallback key is exportable;
- package Publisher does not match signer subject;
- OIDC trust is broad;
- CI signer has Contributor or Owner;
- a production client secret is required without approved exception;
- Action references are mutable;
- candidate hash mismatch exists;
- unsigned first-party executable is present;
- timestamp is missing;
- signature verification fails;
- package acceptance fails;
- tag is unsigned or points to another commit;
- attestation is missing;
- release is mutable;
- signer activity is unexplained;
- or incident response cannot identify affected artefacts.

---

# 98. Acceptance Criteria

This ADR may move to **Accepted** when all of the following hold.

## Publisher and Eligibility

- [ ] Legal publisher is recorded.
- [ ] Certificate subject preview is reviewed.
- [ ] MSIX Publisher exactly matches the intended certificate subject.
- [ ] Artifact Signing organisational eligibility is confirmed, or an OV fallback is accepted.
- [ ] No identity information is misrepresented.
- [ ] Public release is blocked without a production trust path.

## Key Custody and Authentication

- [ ] Production Authenticode private key is non-exportable.
- [ ] No production PFX exists in the repository, CI secrets, caches or artefacts.
- [ ] GitHub OIDC is used for Artifact Signing.
- [ ] No long-lived Azure client secret is used.
- [ ] Federated trust is limited to exact repository and release environment.
- [ ] CI signer has only Certificate Profile Signer at profile scope.
- [ ] CI signer cannot create, delete, revoke or assign roles.
- [ ] Production signing runs on a clean or dedicated environment.
- [ ] Pull requests cannot access production signing.
- [ ] All external Actions are SHA pinned.

## Signing

- [ ] Every first-party EXE is signed.
- [ ] Every required first-party DLL is signed.
- [ ] Every shipped PowerShell script is signed.
- [ ] Third-party signatures are preserved and verified.
- [ ] Signing target list is explicit.
- [ ] Unexpected PE files fail.
- [ ] SHA-256 is used.
- [ ] Every production signature has an RFC 3161 timestamp.
- [ ] Signed and unsigned hashes are recorded.
- [ ] Certificate subject, issuer and thumbprint are recorded.
- [ ] Signing transaction identifiers are recorded.
- [ ] MSIX is signed after its contents are final.
- [ ] No build or package mutation occurs after acceptance.

## Verification

- [ ] Every signature is verified immediately.
- [ ] Every signature is verified after artefact transfer.
- [ ] Final MSIX is verified on another clean Windows machine.
- [ ] Publisher, package family and timestamp are verified.
- [ ] Tampered files fail verification.
- [ ] Revoked-key test behaviour is understood.
- [ ] Public verification documentation exists.

## Source and Provenance

- [ ] Release tags are annotated and SSH signed.
- [ ] Release tag key is hardware backed.
- [ ] Release tag key is separate from authentication keys.
- [ ] Backup hardware signer exists.
- [ ] Public signer keys are recorded in repository policy.
- [ ] Tags verify locally.
- [ ] Tags verify on GitHub.
- [ ] Build attestation is generated.
- [ ] SBOM attestation is generated where supported.
- [ ] Release manifest links source, tag, build and files.

## Publication

- [ ] GitHub immutable releases are enabled.
- [ ] Draft-to-publish process is tested.
- [ ] Published assets cannot be replaced.
- [ ] Downloaded public assets verify.
- [ ] SmartScreen guidance is honest.
- [ ] No release relies solely on reputation.
- [ ] NuGet packages are repository-signed after publication.
- [ ] No exportable PFX is introduced for NuGet author signing.

## Incident Response

- [ ] Signing monitoring exists.
- [ ] Unexpected transaction alert exists.
- [ ] OIDC disable procedure is tested.
- [ ] Signer role removal is tested.
- [ ] Certificate revocation procedure is documented.
- [ ] Tag key rotation is tested.
- [ ] Compromised signer procedure is documented.
- [ ] Release withdrawal procedure is integrated.
- [ ] Security review is complete.
- [ ] Founder approval is recorded.

---

# 99. Evidence Required Before Acceptance

- [ ] Publisher identity record.
- [ ] Eligibility confirmation.
- [ ] Identity-validation result.
- [ ] Certificate-profile subject.
- [ ] Package-family compatibility report.
- [ ] Azure role-assignment report.
- [ ] OIDC claim and environment report.
- [ ] Negative OIDC test.
- [ ] Action dependency inventory.
- [ ] Signing manifest.
- [ ] PE signing report.
- [ ] Script signing report where applicable.
- [ ] MSIX signing report.
- [ ] Timestamp verification report.
- [ ] Second-machine verification report.
- [ ] Clean installation report.
- [ ] Hardware SSH key record.
- [ ] Signed tag test.
- [ ] Local allowed-signers verification.
- [ ] GitHub tag verification.
- [ ] Build attestation.
- [ ] SBOM attestation.
- [ ] Immutable release test.
- [ ] NuGet repository-signature test.
- [ ] Transaction audit record.
- [ ] Monitoring alert test.
- [ ] Revocation tabletop or test.
- [ ] Signer rotation test.
- [ ] Full release-trust rehearsal.
- [ ] Security review.
- [ ] Founder approval.

---

# 100. Known Limitations

- The final legal publisher is not recorded by this ADR.
- Artifact Signing UK individual eligibility is currently unavailable.
- Organisation identity validation may fail or take time.
- The OV fallback provider is not selected.
- The final production certificate subject is not selected.
- The Artifact Signing region is not selected.
- Exact GitHub Action commits are not listed.
- The exact hardware security key model is not selected.
- SSH hardware-backed tag signing requires prototype confirmation on Windows.
- A sole founder cannot provide independent human release approval.
- SmartScreen reputation cannot be guaranteed immediately.
- Artifact Signing availability is an external release dependency.
- Artifact attestation features can depend on repository plan and visibility.
- NuGet author signing is deferred.
- macOS and Linux signing are outside scope.
- Store signing is outside scope.
- Permanent public release-evidence storage is not selected.
- Full automated certificate-revocation testing is not appropriate against a production profile.
- A publisher identity change may require a new MSIX package family.
- The diagnostic ZIP container has no Authenticode signature.
- A valid signature never proves absence of vulnerabilities.

---

# 101. Open Questions

- What legal entity will publish Opure?
- What exact publisher distinguished name should be used?
- Does the Azure billing identity match that legal entity?
- Can Artifact Signing identity validation complete before first preview?
- Which Artifact Signing region should be used?
- Which subscription and tenant should own signing?
- Should production and test profiles use one account or separate accounts?
- Should Stable and Preview use one production certificate profile?
- Which OV provider is the fallback?
- Does the fallback support cloud HSM signing and OIDC?
- Which hardware token is acceptable if cloud signing is unavailable?
- Which GitHub organisation will own the release repository?
- Which OIDC claim model will be used after the July 2026 immutable-ID change?
- What protection rules are available for the `release-signing` environment?
- When should a second release approver be required?
- Should all first-party managed DLLs be individually Authenticode signed?
- Should individual binaries inside MSIX be signed before Version 1.0?
- Which PowerShell scripts, if any, are shipped?
- Should the diagnostic ZIP include a signed catalogue?
- Should a detached signature format be added for checksum files?
- Should OpenPGP be preferred over SSH for release tags?
- Which two hardware security keys should be procured?
- Where should the backup key be stored?
- Should routine commits be signed?
- Should sensitive-path commits require verified signatures?
- How long should signing transaction records be retained?
- Which Azure alerts should page the founder?
- Should production signing be limited to a release time window automatically?
- How should provider outage be communicated?
- What exact thumbprint and certificate fields belong in public evidence?
- Should public release instructions expose the complete certificate chain?
- Should NuGet author signing be added when a managed non-exportable integration is stable?
- Should Microsoft Store signing replace direct signing for Store assets?
- How should publisher identity migrate if the project incorporates later?
- How should previously signed packages be handled after a publisher name change?
- Should certificate-profile revocation require two people once the team grows?
- What is the emergency release procedure during signing-service compromise?
- Should the release manifest itself receive a detached signature?
- Which permanent evidence store should preserve attestations and signing logs?
- How should enterprise Private Trust signing coexist with Public Trust?
- Should future update metadata be signed by a separate offline key?

---

# 102. Deferred Decisions

This ADR intentionally defers:

- legal formation of the publisher;
- exact OV provider procurement;
- Microsoft Store signing;
- automatic update metadata signing;
- NuGet author signing;
- strong naming;
- Linux package signing;
- macOS Developer ID and notarisation;
- container signing;
- enterprise Private Trust;
- detached checksum signatures;
- long-term evidence storage;
- and multi-person release governance.

---

# 103. Alternatives Rejected

Microsoft Store signing alone is rejected because direct technical-preview and enterprise assets still need a trust path.

EV signing is not selected because it does not guarantee immediate SmartScreen acceptance and adds cost without resolving other trust requirements.

Exportable PFX files are prohibited because private-key copying defeats the desired custody model.

A developer workstation USB token is not the preferred path because it is an operational bottleneck and poor CI boundary.

Self-signed production packages are rejected because Windows users do not trust them by default.

No-signature distribution is rejected because MSIX requires signing and users need publisher verification.

Private Trust is rejected for public release because it requires opt-in root trust.

One signature for every trust claim is rejected because source approval, build provenance, publisher identity and immutability are different claims.

---

# 104. Official and Primary Evidence References

## Microsoft Artifact Signing

- [Artifact Signing documentation](https://learn.microsoft.com/en-us/azure/artifact-signing/)
- [Artifact Signing overview](https://learn.microsoft.com/en-us/azure/artifact-signing/overview)
- [Artifact Signing quickstart](https://learn.microsoft.com/en-us/azure/artifact-signing/quickstart)
- [Artifact Signing trust models](https://learn.microsoft.com/en-us/azure/artifact-signing/concept-trust-models)
- [Signing integrations](https://learn.microsoft.com/en-us/azure/artifact-signing/how-to-signing-integrations)
- [Assign Artifact Signing roles](https://learn.microsoft.com/en-us/azure/artifact-signing/tutorial-assign-roles)
- [Artifact Signing FAQ](https://learn.microsoft.com/en-us/azure/artifact-signing/faq)
- [Revoke an Artifact Signing certificate](https://learn.microsoft.com/en-us/azure/trusted-signing/how-to-cert-revocation)
- [Artifact Signing GitHub Action](https://github.com/Azure/artifact-signing-action)

## Windows Signing

- [Code-signing options for Windows developers](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options)
- [MSIX package-signing overview](https://learn.microsoft.com/en-us/windows/msix/package/signing-package-overview)
- [MSIX signing end-to-end](https://learn.microsoft.com/en-us/windows/msix/package/sign-msix-package-guide)
- [Sign an app package with SignTool](https://learn.microsoft.com/en-us/windows/msix/package/sign-app-package-using-signtool)
- [Create a development package-signing certificate](https://learn.microsoft.com/en-gb/windows/msix/package/create-certificate-package-signing)

## GitHub and Git Trust

- [GitHub OIDC for Azure](https://docs.github.com/en/actions/how-tos/secure-your-work/security-harden-deployments/oidc-in-azure)
- [GitHub OIDC reference](https://docs.github.com/en/actions/reference/security/oidc)
- [GitHub artefact attestations](https://docs.github.com/en/actions/concepts/security/artifact-attestations)
- [GitHub immutable releases](https://docs.github.com/en/code-security/concepts/supply-chain-security/immutable-releases)
- [GitHub commit and tag signature verification](https://docs.github.com/en/authentication/managing-commit-signature-verification/about-commit-signature-verification)
- [Signing Git tags](https://docs.github.com/en/authentication/managing-commit-signature-verification/signing-tags)
- [`git tag` documentation](https://git-scm.com/docs/git-tag.html)

## NuGet

- [NuGet signed packages](https://learn.microsoft.com/en-us/nuget/reference/signed-packages-reference)
- [Sign a NuGet package](https://learn.microsoft.com/en-us/nuget/create-packages/sign-a-package)
- [`dotnet nuget sign`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-nuget-sign)
- [`dotnet nuget verify`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-nuget-verify)
- [NuGet signed-package verification](https://learn.microsoft.com/en-us/dotnet/core/tools/nuget-signed-package-verification)

Eligibility, service names, Action repositories, roles, pricing and trust behaviour can change.

All production selections must be revalidated before identity commitment and before public release.

---

# 105. Review Record

| Date | Reviewer | Decision | Notes |
|---|---|---|---|
| 18 July 2026 | Architecture draft | Proposed | Artifact Signing organisation path with non-exportable OV fallback and layered release trust recommended |

---

# 106. Approval

## Founder or Product Approval

- **Name:** Christopher Dyer
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Legal publisher and release authority decision required

## Release Trust Approval

- **Name or role:** Release Trust and Signing Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Eligibility, signing, timestamp, tag and revocation evidence required

## Security Approval

- **Name or role:** Security Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Key custody, OIDC, RBAC, monitoring and incident response required

## CI Approval

- **Name or role:** Build and Continuous Integration Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Protected environment, pinned Actions and exact candidate transfer required

## Packaging Approval

- **Name or role:** Packaging Owner
- **Decision:** Pending
- **Date:** Pending
- **Notes:** Publisher match, PE signing, MSIX signing and clean-machine verification required

---

# 107. Supersession

This ADR is superseded only when a later ADR:

- names ADR-0014 explicitly;
- explains why publisher identity, signing service, key custody or release trust changed;
- identifies affected package families and releases;
- describes certificate, signer and OIDC migration;
- explains revocation and user impact;
- and updates the `Superseded by` field.

Historical signer public keys, certificate thumbprints and release records remain available for verification.

---

# 108. Change History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | 18 July 2026 | Founder Draft | Initial Artifact Signing, OV fallback, hardware tag signing and layered release-trust recommendation |

---

# 109. Final Decision Statement

> **Opure will provisionally use Microsoft Artifact Signing Public Trust under an eligible validated Opure legal organisation as its preferred production Authenticode and MSIX signer, with a publicly trusted OV certificate held in a non-exportable HSM or hardware token as the approved UK-individual fallback, GitHub OIDC and certificate-profile-scoped RBAC for CI authentication, SHA-256 and mandatory RFC 3161 timestamps, a dedicated hardware-backed SSH key for human-signed release tags, GitHub provenance attestations, immutable releases and complete signing evidence, because publisher identity, source approval, build provenance, file integrity and publication immutability are separate trust claims that must remain verifiable without exposing a reusable production private key.**

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**