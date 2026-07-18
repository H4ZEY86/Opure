# ADR-0016 — Plugin Packaging and Distribution

## Architecture Decision Record

**Status:** Proposed
**Date:** 18 July 2026
**Decision owners:** Founder and Product Owner
**Technical owners:** Plugin Platform Owner
**Reviewers:** Plugin SDK Owner, Runtime Architecture Owner, Security Owner, Package and Supply-Chain Owner, Network Gateway Owner, Trust Centre Owner, Persistence Owner, Recovery Owner, Desktop Owner, Test Architecture Owner
**Supersedes:** None
**Superseded by:** None
**Related ADRs:** ADR-0001 through ADR-0015
**Related specifications:** CHARTER-001, SPEC-001 through SPEC-012
**Target milestone:** Plugin SDK Foundation through Version 1.0

---

## 1. Decision Summary

Opure should package first-party and third-party plugins as **standard NuGet `.nupkg` files** with the declared package type:

```xml
<packageType name="OpurePlugin" version="1.0" />
```

Opure will use the NuGet package format as a transport and identity container.

Opure will **not** treat an installed plugin as:

* a `PackageReference`;
* a project dependency;
* a build dependency;
* an MSBuild extension;
* a transitive package graph;
* or an instruction to modify a developer repository.

The initial plugin package model should:

* use standard NuGet package identity and SemVer 2.0 package versions;
* require the NuGet package type `OpurePlugin` version `1.0`;
* include an Opure-owned manifest at `opure/plugin.json`;
* use an independently versioned plugin-manifest schema;
* use one canonical plugin ID matching the NuGet package ID case-insensitively;
* require the manifest version to match the NuGet package version exactly;
* use a closed payload with no runtime dependency download;
* prohibit NuGet dependency groups in the plugin package;
* prohibit `build`, `buildTransitive`, `contentFiles`, `analyzers`, `tools` install scripts and project mutation assets;
* execute no plugin code during discovery, download, verification, installation or update staging;
* support only the initial `managed-dotnet-v1` execution model;
* load managed plugin code only inside a dedicated per-plugin `Opure.PluginHost` process;
* use a collectible `AssemblyLoadContext` inside that disposable process for dependency isolation and diagnostics;
* never load third-party plugin assemblies into `Opure.Runtime` or `Opure.Desktop`;
* package all private runtime dependencies inside the plugin payload;
* share only the versioned Opure Plugin Contract assemblies supplied by the host;
* support Windows 11 x64 initially;
* require explicit architecture and runtime compatibility declarations;
* define requested permissions in the manifest;
* show permissions, publisher identity, source, signatures, package hash, compatibility and data access before activation;
* bind approval to plugin ID, publisher trust identity, package source and permission set;
* require new approval when an update adds or broadens a permission;
* download packages through the Network Gateway;
* support explicit local-file installation and explicitly configured HTTPS NuGet V3 sources;
* perform no package-source auto-discovery;
* bind each installed plugin identity to one approved source;
* reject ambiguous automatic switching between sources that publish the same package ID;
* validate NuGet author and repository signatures where present;
* support repository-trusted, author-trusted and local-unverified trust classes;
* permit unsigned local packages only through Developer Mode and an explicit high-risk confirmation;
* prohibit automatic update for unsigned or locally unverified packages;
* quarantine downloads before extraction;
* validate paths, package structure, counts, compressed and expanded size, duplicate names, case collisions, reserved names, alternate data streams, symlinks and reparse-point attempts;
* calculate and store SHA-256 and the available NuGet package content hash;
* extract into a versioned content-addressed package store;
* keep plugin package files immutable after verification;
* keep writable plugin data in a separate plugin data scope;
* activate a version through authoritative database state rather than a mutable filesystem link;
* stage a new version beside the active version;
* stop the old Plugin Host before activation;
* snapshot plugin-owned data before an update that changes plugin data schema;
* roll back the active package pointer only when data compatibility is proven;
* preserve older verified versions for bounded recovery;
* remove package files only after no active, recovery or audit reference remains;
* keep Stable, Preview and Development plugin stores separate;
* and defer a public Opure marketplace, ratings, payments, automated review and cross-platform plugin payloads.

The initial public distribution sources should be:

1. **Local file or local folder** — explicit side-loading.
2. **Explicit HTTPS NuGet V3 feed** — generic provider-neutral package source.
3. **nuget.org** — optional source that the developer may enable, not an implicit universal plugin registry.
4. **Future Opure-curated feed** — deferred until governance, moderation and publisher-verification policy exist.

The initial trust policy should be:

* an author-signed package from a trusted certificate may be trusted across approved sources;
* a repository-signed package may be trusted only from the configured repository and, where available, approved repository owners;
* an unsigned package from a trusted repository is not automatically equivalent to an author-signed package;
* a package whose signature is invalid or whose signed content has changed is rejected;
* an untrusted or unsupported signature is treated according to explicit Opure policy rather than silently accepted as verified;
* and local unsigned development packages remain visibly unverified.

NuGet's signature system and source metadata are inputs to Opure trust decisions.

They do not replace:

* plugin permission review;
* malware and content scanning;
* package compatibility checks;
* runtime isolation;
* source provenance;
* publisher transfer review;
* or human judgement.

---

## 2. Status

Current status:

> **Proposed**

This ADR becomes **Accepted** only after a complete plugin-package prototype demonstrates:

* a valid `OpurePlugin` package type;
* a valid `opure/plugin.json`;
* exact package and manifest identity matching;
* exact package and manifest version matching;
* no NuGet dependencies;
* no project mutation assets;
* deterministic package creation;
* local-file installation;
* HTTPS NuGet V3 discovery;
* package content download through the Network Gateway;
* author-signature verification;
* repository-signature verification;
* invalid-signature rejection;
* unsigned Developer Mode installation;
* quarantine;
* archive path validation;
* size and count limits;
* content-addressed extraction;
* immutable package files;
* separate writable data;
* permission review;
* dedicated Plugin Host execution;
* managed dependency isolation;
* plugin cancellation and termination;
* staged update;
* expanded-permission reapproval;
* plugin-data snapshot;
* activation rollback where compatible;
* uninstall preservation policy;
* package-source identity binding;
* Stable and Preview isolation;
* and a simulated compromised-package withdrawal.

---

## 3. Context

Opure is an extensible software engineering platform.

Plugins may eventually provide:

* engineering workflows;
* source-control integrations;
* project analysers;
* build-system adapters;
* test adapters;
* provider adapters;
* language services;
* review tools;
* repository insights;
* and constrained UI contributions.

A plugin is executable third-party code.

Its package may contain:

* managed assemblies;
* private dependencies;
* native libraries;
* configuration defaults;
* icons;
* licence notices;
* an SBOM;
* documentation;
* and migration metadata.

The package must communicate:

* identity;
* version;
* publisher;
* source;
* compatibility;
* entry point;
* execution model;
* permissions;
* data scopes;
* architecture;
* dependencies;
* and support information.

The distribution mechanism must answer:

* where the package came from;
* whether it changed in transit;
* who signed it;
* whether the source repository is trusted;
* whether the package is compatible;
* what it can access;
* where it will write;
* whether it can update;
* and whether an old version can be recovered.

The package format must not introduce a second arbitrary installer system.

It must not run code during installation.

It must not modify a developer's project.

It must not allow a package to smuggle MSBuild targets or install scripts into source repositories.

It must not turn a plugin into a hidden dependency graph fetched at activation time.

The package format should be open, inspectable, toolable and provider neutral.

NuGet already provides:

* a ZIP-based package format;
* package identity and SemVer;
* package metadata;
* custom package types;
* HTTPS V3 package sources;
* package-content retrieval;
* author and repository signatures;
* signature verification;
* content hashes;
* local folder sources;
* and broad .NET tooling.

Opure can use those mature transport capabilities without adopting NuGet project-install semantics.

---

## 4. Problem Statement

Opure requires an executable plugin package and distribution architecture that is inspectable, deterministic, provider neutral, compatible with offline side-loading, resistant to package confusion and archive attacks, explicit about permissions and publisher trust, isolated at runtime and reversible without allowing package code to execute during installation or mutate developer projects.

---

## 5. Decision Drivers

The decision is evaluated against:

* Charter alignment;
* developer control;
* package openness;
* deterministic identity;
* SemVer support;
* offline installation;
* generic feed support;
* package signing;
* repository signing;
* publisher trust;
* source mapping;
* dependency confusion resistance;
* runtime isolation;
* no install-time code;
* no project mutation;
* Windows x64 compatibility;
* update safety;
* permission review;
* data migration;
* recovery;
* small-team implementation;
* public ecosystem potential;
* and future language-neutral plugins.

---

## 6. Governing Principles

This decision must preserve:

* Developer Respect;
* Developer First;
* Human in Control;
* Visible and Inspectable Decisions;
* Local by Design;
* Cloud Optional;
* Open by Architecture;
* Loose Coupling;
* Least Privilege;
* No Install-Time Plugin Code;
* No Project Mutation;
* No Hidden Dependency Restore;
* No Source Auto-Discovery;
* No Silent Permission Expansion;
* No Publisher Identity Substitution;
* No Runtime Loading into Trusted Core;
* No Package Path Escape;
* No Mutable Verified Payload;
* No Automatic Update for Unverified Code;
* No Marketplace Authority Without Governance;
* No Signature Claim Beyond Its Meaning;
* Reversible Activation Where Data Allows;
* and Evidence-Based Confidence.

---

## 7. Scope

This ADR decides:

* plugin package container;
* package type;
* plugin manifest;
* plugin identity;
* plugin versioning;
* payload layout;
* initial execution model;
* runtime dependency policy;
* source types;
* source configuration;
* package trust classes;
* NuGet signature use;
* local side-loading;
* discovery;
* download;
* quarantine;
* archive validation;
* package store;
* activation;
* permissions;
* updates;
* rollback;
* uninstall;
* data ownership;
* package retention;
* security withdrawal;
* package diagnostics;
* and ecosystem deferrals.

This ADR does not decide:

* final Plugin SDK APIs;
* every permission name;
* rich plugin UI;
* plugin marketplace;
* ratings;
* payments;
* publisher commercial agreements;
* source-code review service;
* malware vendor;
* public Opure feed implementation;
* external-process language-neutral plugin protocol;
* Linux or macOS plugin payloads;
* plugin telemetry;
* plugin monetisation;
* or enterprise plugin policy distribution.

---

## 8. Constraints

Known constraints include:

* Third-party plugins run outside the trusted Runtime.
* ADR-0003 selects a dedicated Plugin Host process.
* ADR-0004 requires authenticated local IPC.
* ADR-0007 protects secrets through mediated capability access.
* ADR-0008 requires deterministic and adversarial tests.
* ADR-0009 governs path handling.
* ADR-0014 does not introduce an exportable production PFX solely for NuGet author signing.
* NuGet packages are ZIP-based archives with metadata conventions.
* NuGet package IDs are case-insensitive for practical consumption.
* NuGet supports custom package types.
* NuGet author signatures and repository signatures express different trust claims.
* nuget.org automatically repository-signs uploaded packages.
* unsigned packages can exist.
* an untrusted signature is not automatically a trusted publisher identity;
* repository signatures are source-specific;
* generic NuGet V3 sources expose capabilities through a service index;
* multiple sources can publish the same package ID;
* source ambiguity can create dependency-confusion risk;
* NuGet project restore semantics are not appropriate for plugin installation;
* managed plugin unload through `AssemblyLoadContext` is cooperative and can be blocked by remaining references;
* process termination is the final isolation and unload boundary;
* Windows 11 x64 is the first platform;
* and the initial team is small.

---

## 9. Assumptions

This decision assumes:

* a standard `.nupkg` can carry the required executable payload;
* generic feeds accept the `OpurePlugin` package type;
* Opure can use approved NuGet client libraries for metadata and package parsing;
* network operations can remain mediated by the Network Gateway;
* a custom package layout inside the NuGet archive is acceptable;
* plugin packages can be built without dependency groups;
* plugin authors can bundle private runtime dependencies;
* the host can supply only shared Opure contract assemblies;
* a dedicated Plugin Host can load and run the managed entry assembly;
* process termination is acceptable for unload and update;
* permission grants can be bound to plugin identity and publisher trust;
* the plugin data scope can be backed up independently;
* older versions can be retained for bounded recovery;
* and a public marketplace can be added later without changing the package container.

---

## 10. Current Technical Evidence

Current official NuGet and .NET documentation establishes that:

* a NuGet package is a ZIP-based `.nupkg` file following NuGet conventions;
* a `.nuspec` manifest is included and contains package metadata;
* NuGet supports custom package types through `packageTypes`;
* package versions use NuGet's SemVer support;
* a NuGet V3 service index is the entry point for source capabilities;
* the Package Base Address resource supports downloading package content and enumerating versions;
* package-source mapping exists to constrain package IDs to sources and reduce ambiguous source resolution;
* package signatures use X.509 certificates;
* author signatures protect package content independently of transport source;
* repository signatures protect packages associated with a repository;
* repository signature metadata can include repository owners;
* nuget.org repository-signs uploaded packages;
* clients can require and configure trusted author or repository signer certificates;
* signed packages can be verified with NuGet tooling;
* package signing should use RFC 3161 timestamps;
* and .NET supports dynamically loading plugin assemblies through `AssemblyLoadContext`, while unloadability remains cooperative and requires release of references.

All exact NuGet client versions, package-signature behaviour, feed capabilities and .NET loading behaviour must be revalidated before acceptance.

---

## 11. Options Considered

The principal package-container options are:

1. Standard NuGet package with `OpurePlugin` package type.
2. Custom `.opureplugin` ZIP format.
3. OCI artefact.
4. MSIX optional or modification package.
5. Plain directory or ZIP.
6. Git repository installation.
7. NuGet `PackageReference`.
8. Executable installer.
9. WebAssembly component package.
10. Language-specific package formats.

---

# 12. Option A — NuGet Package with `OpurePlugin` Type

## 12.1 Advantages

* standard open package format;
* standard identity;
* SemVer;
* standard metadata;
* custom package type;
* local folder sources;
* generic HTTPS V3 feeds;
* package-content API;
* author signatures;
* repository signatures;
* signature tooling;
* package hashes;
* broad .NET libraries;
* nuget.org compatibility;
* package provenance fields;
* no need to invent archive framing;
* and potential future publisher ecosystem.

---

## 12.2 Disadvantages

* users may confuse plugin packages with project dependencies;
* NuGet dependency semantics are inappropriate for runtime plugins;
* NuGet permits assets that Opure must reject;
* repository signatures do not prove author identity in every scenario;
* package-source ambiguity must be controlled;
* NuGet signing requires X.509 infrastructure;
* and generic feeds may display plugin packages poorly.

---

## 12.3 Mitigations

* custom package type;
* no dependency groups;
* custom payload root;
* explicit manifest;
* Opure-owned installer;
* source binding;
* permission review;
* signature policy;
* and no project restore.

---

## 12.4 Decision

Selected.

---

# 13. Option B — Custom `.opureplugin` ZIP

## 13.1 Advantages

* exact semantics;
* clear file extension;
* no NuGet confusion;
* full manifest control;
* and no unused metadata.

---

## 13.2 Disadvantages

* custom signing;
* custom feed protocol;
* custom hash and index format;
* custom tooling;
* custom package publishing;
* custom repository trust;
* custom ecosystem;
* and long-term maintenance.

---

## 13.3 Decision

Rejected.

A future friendly file extension must not hide a non-standard package format without strong reason.

---

# 14. Option C — OCI Artefact

## 14.1 Advantages

* registry ecosystem;
* content-addressed manifests;
* signatures and attestations;
* multi-platform manifests;
* immutable digests;
* and enterprise registry support.

---

## 14.2 Disadvantages

* container-registry terminology;
* desktop UX burden;
* authentication complexity;
* local side-loading complexity;
* larger client stack;
* and poor initial fit for ordinary plugin developers.

---

## 14.3 Decision

Deferred for future enterprise or cross-platform distribution.

---

# 15. Option D — MSIX Optional or Modification Package

Rejected because the plugin system is cross-application product extension, not Windows package modification.

It would couple plugins to Windows packaging and package-family rules.

---

# 16. Option E — Plain ZIP or Directory

Rejected as the primary format because identity, version, signatures, repository metadata and deterministic distribution would need custom solutions.

A developer directory may remain a local development input.

---

# 17. Option F — Git Repository Installation

Rejected because installing from mutable source requires build tools, arbitrary build execution, dependency resolution and trust in repository state.

Opure may support plugin development from source separately.

---

# 18. Option G — `PackageReference`

Rejected because it would modify a developer project, resolve transitive dependencies and load package assets through build semantics.

A runtime plugin is not a project dependency.

---

# 19. Option H — Executable Installer

Rejected because plugin installation must not execute arbitrary setup code or require elevation.

---

# 20. Option I — WebAssembly Component

Potentially valuable for future portable and capability-oriented plugins.

Deferred because:

* Plugin SDK contracts;
* runtime maturity;
* host bindings;
* performance;
* debugging;
* and native engineering integrations

need evidence.

---

# 21. Option J — Language-Specific Packages

Rejected as the universal distribution model because Opure should not require a separate installer for every plugin language.

---

# 22. Decision

Opure will provisionally adopt:

> **Standard NuGet `.nupkg` files carrying package type `OpurePlugin` version `1.0`, an Opure manifest and a closed executable payload, installed through an Opure-owned permissioned pipeline and executed only inside a dedicated Plugin Host process.**

This decision is:

* [ ] Permanent until superseded
* [x] Provisional pending package, trust and runtime evidence
* [ ] Marketplace approval
* [ ] Cross-platform plugin approval
* [ ] External-process plugin approval

---

# 23. Package Identity

The NuGet package ID is the canonical plugin identity.

Example:

```text
Acme.Opure.RepositoryReview
```

Canonical comparison is case-insensitive.

The UI preserves publisher-selected display casing.

---

## 23.1 Manifest Identity

`opure/plugin.json` must contain:

```json
{
  "id": "Acme.Opure.RepositoryReview",
  "version": "1.2.0"
}
```

The manifest ID must equal the NuGet package ID under canonical case-insensitive comparison.

The manifest version must equal the NuGet package version exactly after NuGet normalisation rules are considered.

---

## 23.2 Immutable Identity

A package ID identifies one plugin product line.

A publisher must not reuse an ID for unrelated functionality.

---

## 23.3 Display Name

Display name is user-facing metadata.

It is not a security identity.

---

## 23.4 Publisher Display Name

Publisher display name is descriptive.

Trust comes from:

* author signature;
* repository signature;
* trusted repository owner;
* configured source;
* or explicit local-unverified approval.

---

## 23.5 Identity Collision

If two sources publish the same package ID:

* Opure does not merge them;
* the installed source binding remains authoritative;
* automatic source switching is blocked;
* and the developer must review a source-transfer operation.

---

# 24. Package Version

Plugin package versions use SemVer 2.0 as supported by NuGet.

Examples:

```text
1.0.0
1.1.0-preview.1
1.1.0-beta.1
2.0.0
```

---

## 24.1 Version Meaning

Plugin authors should use:

* major for incompatible plugin-owned public behaviour or data;
* minor for backward-compatible functionality;
* patch for backward-compatible fixes.

Opure cannot enforce the author's semantic judgement, but it can enforce syntax and compatibility declarations.

---

## 24.2 Version Reuse

An installed or published plugin package version is immutable.

A different package hash with the same ID and version is a supply-chain conflict.

---

## 24.3 Repack Detection

If the same package ID and version appears with a different content hash:

* quarantine it;
* mark the source inconsistent;
* block automatic installation;
* and require security review.

---

# 25. Package Type

The `.nuspec` must declare:

```xml
<packageTypes>
  <packageType name="OpurePlugin" version="1.0" />
</packageTypes>
```

---

## 25.1 Exact Type

The package is rejected when:

* package type is absent;
* package type name differs;
* multiple conflicting package types exist;
* or package-type version is unsupported.

---

## 25.2 Type Version

Package-type version describes Opure's package-layout contract.

It is separate from:

* plugin version;
* manifest schema;
* Plugin SDK;
* Plugin Host protocol;
* and product version.

---

# 26. NuGet Metadata

Required or expected NuGet metadata includes:

* package ID;
* version;
* authors;
* description;
* licence expression or licence file;
* project URL;
* repository URL and commit where public;
* readme;
* icon;
* tags;
* package type;
* and release notes where practical.

---

## 26.1 Repository Metadata

A public plugin package should include its source repository and commit when source is public.

This is provenance metadata, not a guarantee that the package was built from that commit.

---

## 26.2 Licence

Every package requires:

* SPDX-compatible licence expression;
* or included licence file.

Unknown or absent licensing blocks public curated distribution.

Local Developer Mode may permit installation with a prominent warning.

---

## 26.3 Readme

A package readme may describe:

* purpose;
* setup;
* permissions;
* data handling;
* support;
* and source.

It cannot override the machine-readable manifest.

---

# 27. Opure Manifest

The required manifest path is:

```text
opure/plugin.json
```

The path is lower-case and exact.

---

## 27.1 Encoding

The manifest uses:

* UTF-8 without reliance on BOM;
* JSON;
* no comments;
* bounded depth;
* and duplicate-property rejection.

---

## 27.2 Schema

The manifest declares:

```json
{
  "schema": "opure.plugin-manifest/1"
}
```

The exact URI or schema identifier should be stable and resolvable in repository documentation.

---

## 27.3 Manifest Fields

The initial manifest should include:

```text
schema
id
version
display_name
description
publisher
execution
entry_point
plugin_host_protocol
opure_api
supported_platforms
supported_architectures
permissions
data_schema
update_policy
homepage
support
source
licence
```

Optional fields may include:

```text
icon
capabilities
commands
workflows
configuration_schema
localisation
sbom
notices
deprecation
```

---

## 27.4 Unknown Fields

Unknown optional fields are preserved or ignored according to schema compatibility.

Unknown required features cause rejection.

---

## 27.5 Duplicate Properties

Duplicate JSON properties are rejected.

---

# 28. Initial Execution Model

The only initial execution model is:

```text
managed-dotnet-v1
```

---

## 28.1 Host

The plugin runs inside:

```text
Opure.PluginHost
```

with one Plugin Host process per plugin instance where practical.

---

## 28.2 Entry Point

The manifest identifies:

* managed assembly;
* entry type;
* and contract version.

Example:

```json
{
  "execution": {
    "model": "managed-dotnet-v1",
    "assembly": "opure/payload/managed/Acme.RepositoryReview.dll",
    "type": "Acme.RepositoryReview.Plugin"
  }
}
```

---

## 28.3 No Trusted-Core Load

Third-party assemblies are never loaded into:

* Desktop;
* Runtime;
* Gateway;
* Secrets Vault;
* or another trusted first-party service process.

---

## 28.4 Assembly Load Context

The Plugin Host uses a dedicated collectible `AssemblyLoadContext` to:

* isolate private managed dependencies;
* resolve payload-local assemblies;
* prevent arbitrary fallback to trusted host implementation assemblies;
* and support diagnostics.

Process exit remains the authoritative unload mechanism.

---

## 28.5 Shared Assemblies

Only approved host-provided contract assemblies are shared.

A plugin package must not include or override:

* Opure Plugin Contract assemblies;
* Runtime internals;
* Gateway internals;
* or trusted service implementations.

Duplicate shared contract assemblies are rejected or ignored according to a strict host policy.

---

# 29. Future Execution Models

Potential future models include:

* `external-process-v1`;
* `wasm-component-v1`;
* and constrained scripting.

Each requires a separate ADR or amendment covering:

* protocol;
* lifecycle;
* sandbox;
* packaging;
* debugging;
* performance;
* and permission enforcement.

---

# 30. Package Layout

The initial layout is:

```text
<package>.nuspec
opure/
├── plugin.json
├── payload/
│   └── managed/
│       ├── Plugin.Entry.dll
│       ├── Plugin.Dependency.dll
│       └── ...
├── runtimes/
│   └── win-x64/
│       └── native/
│           └── ...
├── resources/
│   ├── icon.png
│   └── ...
├── schemas/
│   └── configuration.schema.json
├── notices/
│   └── THIRD-PARTY-NOTICES.txt
├── licences/
│   └── ...
└── sbom/
    └── plugin.spdx.json
```

Not every optional directory is required.

---

## 30.1 Payload Root

Executable payload must remain below:

```text
opure/payload/
```

and:

```text
opure/runtimes/
```

---

## 30.2 Native Files

Native libraries use an explicit RID path.

The initial supported RID is:

```text
win-x64
```

---

## 30.3 Resources

Resources are non-executable package assets.

They remain subject to size and path validation.

---

## 30.4 Documentation

Package readme and notices may use NuGet-standard metadata locations, but Opure executable assets remain under `opure/`.

---

# 31. Prohibited Package Paths

Reject packages containing project-install assets such as:

```text
build/
buildMultiTargeting/
buildTransitive/
content/
contentFiles/
analyzers/
lib/
ref/
```

unless a future package-layout version explicitly permits a narrowly defined use.

---

## 31.1 Tools Directory

Reject:

```text
tools/install.ps1
tools/uninstall.ps1
init.ps1
```

and any equivalent install-time script.

The initial plugin package should not use `tools/` at all.

---

## 31.2 MSBuild Assets

Any `.props` or `.targets` file in a NuGet auto-import location is rejected.

---

## 31.3 Project Mutation

A plugin package may not contain instructions that cause NuGet to modify a developer project.

---

# 32. Dependency Policy

The initial plugin package must contain no NuGet dependency groups.

---

## 32.1 Closed Payload

All private runtime dependencies must be included in the package payload.

---

## 32.2 No Install-Time Restore

Opure performs no dependency restore when installing or launching the plugin.

---

## 32.3 No Transitive Graph

A plugin cannot bring an unreviewed transitive package graph into runtime after permission approval.

---

## 32.4 Shared Contract Dependency

The plugin is compiled against an Opure Plugin SDK package during plugin development.

The runtime package does not declare it as a NuGet installation dependency.

Compatibility is expressed in `plugin.json`.

---

## 32.5 Future Shared Dependencies

A shared plugin dependency system requires a new ADR.

---

# 33. Compatibility Fields

The manifest declares compatibility independently.

Example:

```json
{
  "plugin_host_protocol": "1.x",
  "opure_api": ">=1.0.0 <2.0.0",
  "supported_platforms": ["windows"],
  "supported_architectures": ["x64"]
}
```

---

## 33.1 Product Version

A product-version range may be shown for user guidance.

It is not the sole compatibility test.

---

## 33.2 Protocol

Plugin Host protocol compatibility is authoritative for host communication.

---

## 33.3 API Contract

The Plugin SDK API range governs compiled contract compatibility.

---

## 33.4 Architecture

A package with native files must declare matching architecture.

---

## 33.5 Operating System

The initial package must declare Windows support.

---

# 34. Plugin Permissions

Permissions are machine-readable manifest entries.

Examples may include:

* read project files;
* propose file patches;
* execute approved build command;
* read repository metadata;
* access network through Network Gateway;
* request provider inference;
* read plugin configuration;
* write plugin data;
* contribute command;
* contribute workflow;
* and emit diagnostics.

The definitive permission catalogue belongs to the Plugin SDK and Security specifications.

---

## 34.1 No Wildcard Permission

An unrestricted wildcard permission is prohibited.

---

## 34.2 Permission Parameters

A permission may include bounded parameters such as:

* host allowlist;
* command family;
* file scope;
* project scope;
* provider class;
* or data category.

---

## 34.3 Permission Review

Before first activation, show:

* requested permissions;
* why the plugin says it needs them;
* source;
* publisher trust;
* package version;
* and affected data.

---

## 34.4 Permission Expansion

An update adding or broadening permission is staged but not activated until approved.

---

## 34.5 Permission Reduction

A reduction may be accepted automatically after package verification, but the change remains visible.

---

## 34.6 Grant Binding

A grant is bound to:

* plugin ID;
* publisher trust identity;
* source ID;
* permission set;
* and profile channel.

---

# 35. Publisher Trust Identity

Trust identity is derived from one of:

* author-signing certificate;
* repository-signing certificate plus source and approved owner;
* curated source publisher record;
* or explicit local-unverified user approval.

---

## 35.1 Display Name Is Not Identity

A package cannot become trusted by changing the `authors` or publisher text.

---

## 35.2 Certificate Rotation

A new author certificate requires:

* valid chain;
* package signature;
* continuity evidence;
* and user-visible signer change unless the source has an approved rotation policy.

---

## 35.3 Ownership Transfer

Repository owner or publisher transfer is a high-risk update.

Automatic update pauses pending review.

---

# 36. Trust Classes

The initial trust classes are:

* **Author Verified**
* **Repository Verified**
* **Curated Publisher Verified**
* **Local Verified by Hash**
* **Local Unverified**
* **Invalid**
* **Revoked**
* **Withdrawn**

---

## 36.1 Author Verified

Requirements:

* valid NuGet author signature;
* valid timestamp;
* trusted certificate chain or configured author trust;
* and no revocation or policy failure.

---

## 36.2 Repository Verified

Requirements:

* valid repository signature;
* explicitly trusted repository;
* expected repository signing certificate;
* and optional expected owner.

Trust does not automatically extend to a copy obtained from another unrelated source unless signature policy permits it.

---

## 36.3 Curated Publisher Verified

Future Opure-curated feed trust may combine:

* repository signature;
* verified publisher account;
* review status;
* and package policy.

Deferred until marketplace governance exists.

---

## 36.4 Local Verified by Hash

A package may be explicitly approved by exact SHA-256 in enterprise or development policy.

This verifies bytes, not publisher identity.

---

## 36.5 Local Unverified

Unsigned or untrusted side-loaded package.

Requires Developer Mode and high-risk consent.

---

## 36.6 Invalid

Invalid or tampered signature, malformed package or identity mismatch.

Never install.

---

# 37. Signature Policy

NuGet package signatures are verified through approved NuGet libraries or tooling.

---

## 37.1 Author Signature

An author signature can protect package integrity independently of transport source and identify the signing certificate.

---

## 37.2 Repository Signature

A repository signature identifies repository provenance and can include owner metadata.

---

## 37.3 Timestamp

A signed plugin package should have a valid RFC 3161 timestamp.

---

## 37.4 Invalid Signature

An invalid signature is not treated as unsigned.

It is rejected.

---

## 37.5 Untrusted Signature

An intact signature whose certificate is not trusted is displayed as untrusted and follows explicit policy.

It is not silently labelled verified.

---

## 37.6 Multiple Signatures

The NuGet signature model and supported primary/countersignature rules are followed exactly.

Opure does not invent a parallel signature inside the package initially.

---

# 38. nuget.org Policy

nuget.org is not automatically enabled as the universal Opure plugin registry.

The developer may enable it explicitly.

---

## 38.1 Repository Signature

nuget.org repository-signs packages.

Opure may trust the nuget.org repository signer according to current repository-signature metadata and policy.

---

## 38.2 Owners

Where available, Opure may bind trust to approved package owners.

---

## 38.3 Package Type

Only packages declaring the supported `OpurePlugin` type are shown.

---

## 38.4 No General Package Search

Opure must not present arbitrary NuGet libraries as installable plugins.

---

# 39. Generic NuGet V3 Source

An HTTPS NuGet V3 service index is the initial remote-source protocol.

---

## 39.1 Required Capabilities

At minimum, the source must support:

* service index;
* package metadata or registration;
* package content download;
* version enumeration;
* and HTTPS.

Repository signature capability is preferred.

---

## 39.2 Unsupported Source

A V2-only feed is not supported initially.

---

## 39.3 Provider Neutrality

The implementation must not depend on nuget.org-specific URLs for generic sources.

---

# 40. Local Sources

Supported local sources:

* explicit `.nupkg` file;
* explicit local folder;
* and enterprise-managed offline folder.

---

## 40.1 No Recursive Drive Scan

Opure does not scan disks automatically for plugin packages.

---

## 40.2 Folder Source

A folder source is explicitly added and bounded.

---

## 40.3 Removable Media

A removable-media package is copied into quarantine and verified before installation.

---

# 41. Source Configuration

A source record contains:

```text
source_id
display_name
source_type
service_index_or_path
enabled
trust_mode
trusted_repository_certificates
trusted_owners
package_id_patterns
credentials_reference
managed_by
created_at
```

Credentials are Vault references, not values.

---

## 41.1 Source Consent

Adding a remote source displays:

* URL;
* trust mode;
* data sent;
* authentication need;
* and package namespace mapping.

---

## 41.2 No Source Auto-Discovery

A plugin or package cannot add another source.

---

## 41.3 Source Removal

Removing a source does not delete installed plugins.

It disables discovery and update from that source.

---

# 42. Source Mapping

Each installed plugin ID is bound to one source.

---

## 42.1 Package ID Patterns

Source configuration may permit patterns such as:

```text
Acme.Opure.*
```

Exact IDs are preferred.

---

## 42.2 Ambiguity

If one ID matches multiple enabled sources:

* discovery shows a conflict;
* automatic resolution is blocked;
* and the developer selects and records the source.

---

## 42.3 Source Transfer

Changing the bound source requires:

* exact plugin ID;
* current and new source;
* publisher identity comparison;
* package hash comparison where version overlaps;
* permission review;
* and user approval.

---

# 43. Authentication

Authenticated feeds use the Secrets Vault.

---

## 43.1 Credential Types

Potential types include:

* personal access token;
* feed token;
* credential provider;
* or enterprise integrated authentication.

---

## 43.2 No Plaintext Configuration

Credentials do not enter:

* source database;
* logs;
* package metadata;
* Trust Centre details;
* or command-line arguments.

---

## 43.3 Credential Provider

A feed-specific credential provider requires separate supply-chain and process review.

---

# 44. Network Mediation

All remote package-source operations pass through the Network Gateway.

This includes:

* service index;
* search;
* registration;
* package metadata;
* package content;
* repository signature metadata;
* and icons or readmes.

---

## 44.1 NuGet Client Integration

The implementation should use maintained NuGet client libraries for:

* protocol semantics;
* package parsing;
* SemVer;
* and signatures.

It must still route HTTP through an Opure-controlled, policy-aware adapter.

---

## 44.2 Prototype Gate

If the selected NuGet client stack cannot cleanly use Network Gateway mediation:

* do not bypass the Gateway;
* isolate a bounded Package Source Worker with an authenticated mediated network capability;
* or implement only the minimal documented V3 read subset in a reviewed service.

The final choice requires prototype evidence.

---

## 44.3 No Direct Plugin Network

Package discovery is performed by trusted Opure services, not plugin code.

---

# 45. Remote Request Privacy

Package-source requests may reveal:

* requested plugin ID;
* requested version;
* source IP;
* and ordinary HTTP metadata.

The source-setting UI must explain this.

Requests must not include:

* project names;
* repository paths;
* prompts;
* secrets;
* model inventory;
* or persistent device IDs.

---

# 46. Discovery

Discovery returns metadata only.

It does not download or execute package payload automatically.

---

## 46.1 Search

Search is limited to supported plugin package type where the feed exposes package-type metadata.

Packages without compatible type are filtered after metadata retrieval.

---

## 46.2 Metadata Display

Display:

* ID;
* version;
* title;
* description;
* authors;
* source;
* package type;
* licence;
* repository;
* trust class;
* download size;
* compatibility;
* permissions;
* and deprecation or withdrawal.

---

## 46.3 Remote Content

Remote icons and readmes are treated as untrusted content.

No active HTML.

Remote images may remain disabled by default.

---

# 47. Download

Download occurs only after explicit install or update preparation.

---

## 47.1 Quarantine

The package is written to:

```text
<Profile>\Plugins\Quarantine\<operation-id>\
```

or another short controlled path.

---

## 47.2 Temporary Name

Use a generated filename, not a server-supplied path.

---

## 47.3 Streaming Hash

Calculate hashes while downloading.

---

## 47.4 Limits

Enforce:

* content-length policy;
* streamed byte limit;
* timeout;
* cancellation;
* and disk-space check.

---

## 47.5 Partial Download

A partial package is never parsed as a complete candidate.

---

# 48. Provisional Package Limits

Initial limits should be conservative and configurable by policy.

Suggested defaults:

* compressed package: 256 MiB;
* total extracted content: 1 GiB;
* files: 10,000;
* individual file: 512 MiB;
* path depth: 32;
* manifest: 1 MiB;
* readme: 2 MiB;
* icon: 5 MiB;
* total metadata before payload: 10 MiB;
* and compression-ratio alert or rejection above 100:1 for suspicious entries.

Exact limits require representative plugin evidence.

---

# 49. Package Parsing

The package parser must:

* use NuGet package APIs where appropriate;
* identify the `.nuspec`;
* reject multiple manifests;
* validate package type;
* validate identity;
* validate version;
* inspect dependencies;
* inspect every path;
* inspect package signatures;
* and build a complete content inventory before extraction.

---

# 50. Archive Path Security

Reject entries with:

* absolute paths;
* drive-qualified paths;
* UNC paths;
* `..` traversal;
* empty segments;
* trailing dots or spaces;
* Windows reserved device names;
* alternate data stream syntax;
* invalid Unicode;
* invalid surrogate sequences;
* overly long paths;
* duplicate canonical paths;
* case-insensitive collisions;
* file-directory collisions;
* symlink metadata;
* reparse-point intent;
* or paths outside approved roots.

---

## 50.1 Canonicalisation

Canonicalise using Windows-aware rules before any extraction.

---

## 50.2 No Follow Links

Extraction creates regular files and directories only.

It never follows links from quarantine or target roots.

---

## 50.3 Existing Target

Extraction occurs into a new empty staging directory.

No archive entry overwrites an existing file.

---

# 51. File-Type Inspection

The inventory classifies:

* managed PE;
* native PE;
* script;
* executable text;
* configuration;
* schema;
* image;
* licence;
* documentation;
* archive;
* and unknown.

---

## 51.1 Nested Archives

Nested archives are rejected initially unless they are declared non-executable resources and remain unopened.

Executable payload may not be hidden in a nested archive.

---

## 51.2 Scripts

Runtime scripts are not supported by `managed-dotnet-v1`.

Unexpected scripts in executable paths are rejected.

---

## 51.3 PE Architecture

Native PE files must match declared architecture.

---

# 52. Malware and Reputation Scanning

The trusted installer should support Windows Defender or an approved local malware scan where available.

---

## 52.1 Scan Result

A positive detection blocks activation.

---

## 52.2 Scan Limitation

A clean scan does not prove safety.

---

## 52.3 Cloud Submission

Opure must not upload a private plugin package to a cloud scanner without explicit consent and data visibility.

---

# 53. Secret and Sensitive Content Scan

Scan package content for likely:

* private keys;
* access tokens;
* connection strings;
* credentials;
* and developer-local secrets.

A likely secret blocks curated publication and prompts local installation review.

Do not expose the secret value in logs or UI.

---

# 54. SBOM

A public plugin package should include an SBOM.

The initial recommended path is:

```text
opure/sbom/plugin.spdx.json
```

---

## 54.1 Missing SBOM

Missing SBOM is:

* warning for ordinary third-party source;
* rejection for future curated source after the policy date;
* and allowed for local Developer Mode.

---

## 54.2 Verification

The SBOM is compared with packaged managed and native files where practical.

---

# 55. Licence and Notices

Installation displays:

* plugin licence;
* third-party notice status;
* and source licence metadata.

Unknown licensing requires explicit local approval and blocks curated distribution.

---

# 56. Package Hashes

Persist:

* SHA-256 of the complete `.nupkg`;
* available NuGet package content hash;
* SHA-256 of `.nuspec`;
* SHA-256 of `opure/plugin.json`;
* and SHA-256 for every extracted file.

---

## 56.1 Hash Identity

The package store identity includes the complete package SHA-256.

---

## 56.2 Hash Mismatch

Any later file mismatch marks the installed version corrupted and prevents launch.

---

# 57. Installation Pipeline

The installation pipeline is:

```text
Select package
    ↓
Resolve explicit source
    ↓
Download or copy to quarantine
    ↓
Calculate package hashes
    ↓
Verify NuGet signatures
    ↓
Parse NuGet metadata
    ↓
Validate package type
    ↓
Parse and validate Opure manifest
    ↓
Validate identity and version
    ↓
Validate package layout
    ↓
Validate paths and limits
    ↓
Inspect files and architecture
    ↓
Scan for malware and likely secrets
    ↓
Evaluate compatibility
    ↓
Build permission plan
    ↓
Show publisher, source, trust and permissions
    ↓
Developer approval
    ↓
Extract to new staging root
    ↓
Verify every extracted hash
    ↓
Commit immutable package version
    ↓
Create or update plugin registration
    ↓
Launch dedicated Plugin Host
    ↓
Perform bounded activation handshake
    ↓
Mark active or roll back
```

No plugin code runs before the dedicated Plugin Host activation step.

---

# 58. Installation Authority

The trusted Plugin Package Service owns:

* package acquisition;
* parsing;
* signature verification;
* quarantine;
* extraction;
* package-store commit;
* registration;
* activation;
* update;
* rollback;
* and removal.

The plugin cannot install itself.

---

# 59. Package Store

Proposed package root:

```text
%LOCALAPPDATA%\Opure\<Channel>\Plugins\Packages\
```

Versioned content path:

```text
Packages\<canonical-id>\<version>\<package-sha256>\
```

Example:

```text
Packages\acme.opure.repositoryreview\1.2.0\a1b2c3...\
```

---

## 59.1 Immutable Payload

After commit, plugin payload files are treated as immutable.

The Plugin Host receives read-only logical access.

---

## 59.2 ACL

Where practical, set ACLs to prevent plugin child processes from writing to package files.

The same Windows user remains able to alter their own files outside the plugin sandbox, so hashes are revalidated before activation when policy requires.

---

## 59.3 No Active Symlink

The active version is recorded in the service database.

Do not rely on a user-mutable symlink or junction named `current`.

---

## 59.4 Package Copy

Retain the original verified `.nupkg` or a content-addressed copy for:

* re-verification;
* recovery;
* export;
* and evidence.

Retention is bounded.

---

# 60. Plugin Data Store

Writable plugin data lives separately:

```text
%LOCALAPPDATA%\Opure\<Channel>\Plugins\Data\<canonical-id>\
```

---

## 60.1 Package Versus Data

Package update may replace executable payload.

It must not replace or delete plugin data without a migration plan.

---

## 60.2 Data Scope

A plugin receives a logical data capability for its own ID.

It does not receive the raw data-root path unless the execution contract requires it.

---

## 60.3 Data Quota

Plugin data is subject to configurable:

* byte quota;
* file count;
* path rules;
* and retention policy.

---

## 60.4 Shared Data

Plugins do not share writable data directories directly.

Cross-plugin exchange uses mediated contracts.

---

# 61. Plugin Cache

Download and metadata caches are separate from:

* immutable packages;
* plugin data;
* and recovery backups.

Cache deletion must not uninstall a plugin.

---

# 62. Plugin Registration

The authoritative plugin registration records:

```text
plugin_id
active_version
active_package_hash
source_id
trust_class
publisher_identity
permissions
data_schema
state
installed_at
activated_at
last_started_at
last_result
```

---

## 62.1 States

Plugin states include:

* Discovered;
* Downloaded;
* Quarantined;
* Verified;
* Awaiting Approval;
* Staged;
* Activating;
* Active;
* Disabled;
* Incompatible;
* Update Available;
* Permission Review Required;
* Faulted;
* Revoked;
* Withdrawn;
* Corrupted;
* Rollback Available;
* Uninstall Pending;
* and Removed.

---

# 63. Permission Approval

The approval screen must show:

* plugin ID;
* display name;
* version;
* publisher display name;
* trust class;
* author signer;
* repository signer;
* source;
* package hash;
* licence;
* compatibility;
* requested permissions;
* network destinations if declared;
* project data categories;
* executable/native content;
* data migration;
* and update policy.

---

## 63.1 No Bundled Consent

Installing one plugin does not approve another plugin.

---

## 63.2 No Hidden Default

High-impact permissions are not preselected silently.

---

## 63.3 Decline

Declining permissions leaves the package unactivated or removes the staged package according to user choice.

---

# 64. Activation

Activation occurs only after:

* package verification;
* permission approval;
* compatibility approval;
* package-store commit;
* and Plugin Host readiness.

---

## 64.1 Bootstrap

The Runtime launches a dedicated Plugin Host with:

* plugin ID;
* package version;
* package-store reference;
* permission grant reference;
* short-lived bootstrap credential;
* protocol version;
* and resource profile.

No secret value appears in command-line arguments.

---

## 64.2 Handshake

The Plugin Host must:

* authenticate to Runtime;
* verify package identity;
* load only approved entry assembly;
* initialise within timeout;
* report capabilities;
* accept permission projection;
* and return a health result.

---

## 64.3 Failure

If activation fails:

* stop Plugin Host;
* preserve diagnostics;
* keep previous active version if updating;
* mark candidate faulted;
* and do not broaden permissions.

---

# 65. Managed Dependency Loading

The Plugin Host uses a plugin-specific resolver.

---

## 65.1 Private Dependencies

Private assemblies resolve only from approved payload paths.

---

## 65.2 Host Contracts

Approved Plugin Contract assemblies resolve from the host.

---

## 65.3 Trusted Internals

A plugin cannot bind to non-public trusted Runtime implementation assemblies merely because they are present on disk.

---

## 65.4 Native Dependencies

Native library resolution is restricted to:

* plugin-specific RID native directory;
* approved operating-system libraries;
* and host-approved runtime libraries.

---

## 65.5 Assembly Name Collision

A private assembly with the same identity as a host contract cannot replace the host contract.

---

# 66. Runtime Isolation

A plugin runs out of process from the trusted Runtime.

---

## 66.1 Process Boundary

The process boundary is the primary:

* crash;
* memory;
* unload;
* and termination boundary.

---

## 66.2 AssemblyLoadContext Limitation

Collectible `AssemblyLoadContext` unload is cooperative.

The host must not rely on it for security.

---

## 66.3 Restart

Plugin update and reliable unload normally restart the per-plugin Plugin Host.

---

## 66.4 Resource Limits

Process Supervisor may enforce:

* memory budget;
* CPU budget;
* process count;
* child-process policy;
* and timeouts.

---

# 67. Plugin Child Processes

A managed plugin cannot launch arbitrary child processes directly.

It requests a mediated command capability.

Future external-process plugin models require separate policy.

---

# 68. Network Access

Plugin network access is mediated by the Network Gateway.

---

## 68.1 Declared Destinations

The manifest should declare intended host patterns or service categories.

---

## 68.2 Runtime Approval

Actual requests remain subject to:

* user permission;
* project cloud policy;
* secret policy;
* destination policy;
* and Trust Centre visibility.

---

## 68.3 Package Source Network

Package download authority does not grant the plugin runtime network access.

---

# 69. Filesystem Access

Plugin filesystem access is mediated.

---

## 69.1 Package Files

Plugin may read its package files.

It cannot write them.

---

## 69.2 Plugin Data

Plugin may use its own mediated data scope.

---

## 69.3 Project Files

Project access requires explicit permission and service mediation.

---

## 69.4 Arbitrary Paths

No permission means no arbitrary user-profile access.

---

# 70. Secrets

Plugins never receive raw Vault database access.

A plugin may request a secret capability according to ADR-0007.

---

## 70.1 Secret Binding

A secret grant is bound to:

* plugin identity;
* publisher trust identity;
* operation;
* destination;
* and scope.

---

## 70.2 Publisher Change

Publisher trust change invalidates or suspends secret grants.

---

# 71. Plugin Configuration

Configuration schema may be included at:

```text
opure/schemas/configuration.schema.json
```

---

## 71.1 Validation

Opure validates configuration before sending it to the plugin.

---

## 71.2 Secret Fields

Secret-valued configuration is represented by Vault references.

---

## 71.3 Configuration Migration

A plugin update changing configuration schema must declare migration behaviour.

No plugin code runs during package installation to migrate configuration.

Migration runs only in the isolated Plugin Host after backup and approval.

---

# 72. Plugin Data Schema

The manifest declares a plugin-owned data schema version.

Example:

```json
{
  "data_schema": {
    "version": 3,
    "migration": "forward-only",
    "rollback": false
  }
}
```

---

## 72.1 Independent Version

Plugin data schema is independent from plugin SemVer.

---

## 72.2 Migration Authority

Migration code executes inside the Plugin Host and receives access only to the plugin's data scope.

---

## 72.3 Backup

Before a schema-changing activation, snapshot plugin data.

---

## 72.4 Failure

A failed migration:

* stops the candidate host;
* preserves backup;
* keeps previous package registration;
* and marks recovery required.

---

# 73. Plugin Installation Does Not Run Code

The following stages are code-free with respect to plugin payload:

* discovery;
* package metadata display;
* signature verification;
* manifest parsing;
* permission review;
* extraction;
* package-store commit;
* and uninstall planning.

Only activation runs plugin code, in the Plugin Host.

---

# 74. Update Discovery

Plugin update discovery is separate from Opure application updating.

---

## 74.1 Default

Manual Only by default.

---

## 74.2 Source Opt-In

A user may opt into bounded plugin metadata checks per source.

No plugin may enable its own updater.

---

## 74.3 Package Source Binding

Updates are discovered only from the installed source unless a source transfer is approved.

---

## 74.4 No Package Auto-Install

An available plugin update is not installed silently.

---

# 75. Update Validation

A candidate update must pass the full package pipeline again.

---

## 75.1 Identity

Plugin ID must match.

---

## 75.2 Source

Source binding must match or source transfer must be approved.

---

## 75.3 Publisher

Publisher trust must remain compatible.

---

## 75.4 Version

Version must be higher unless an explicit recovery install is being performed.

---

## 75.5 Hash

Same ID and version with different hash is rejected.

---

## 75.6 Permissions

Permission changes are compared.

---

## 75.7 Data Schema

Migration and rollback are evaluated.

---

# 76. Update Permission Diff

Classify permission changes as:

* unchanged;
* reduced;
* added;
* broadened;
* narrowed;
* removed;
* or semantically changed.

---

## 76.1 Added or Broadened

Requires explicit approval.

---

## 76.2 Semantically Changed

Requires review even if the permission identifier is unchanged.

This requires permission-catalogue versioning.

---

## 76.3 Rejected Permission

The old plugin version remains active.

---

# 77. Staged Update

A new version is installed beside the active version.

---

## 77.1 No In-Place Overwrite

Never overwrite active package files.

---

## 77.2 Host Stop

Stop the old Plugin Host before candidate activation.

---

## 77.3 Active Pointer

Update authoritative registration only after:

* candidate handshake;
* permission acceptance;
* migration success;
* and health check.

---

## 77.4 Atomic Commit

The active version change and relevant service state commit atomically within the plugin package domain.

Cross-service effects use outbox events.

---

# 78. Rollback

Package rollback means reactivating a previously verified package version.

---

## 78.1 Conditions

Rollback is allowed only when:

* old package remains verified;
* old publisher trust remains acceptable;
* old permissions are available;
* plugin data remains compatible;
* and plugin data migration declares rollback or backup restoration succeeds.

---

## 78.2 No False Rollback

Changing the active package pointer does not reverse plugin data automatically.

---

## 78.3 Forward-Only Data

If data migration is forward only:

* package rollback is blocked;
* restore from compatible backup may be offered;
* or the newer package remains disabled pending repair.

---

## 78.4 Security Withdrawal

Do not roll back to a withdrawn or vulnerable version merely because activation failed.

---

# 79. Recovery Versions

Retain a bounded number of verified older versions.

Suggested default:

* current;
* one previous compatible;
* and one last-known-good recovery version

subject to storage policy.

---

## 79.1 Retention Exception

Security-withdrawn versions may be retained quarantined for evidence but cannot activate.

---

# 80. Uninstall

Uninstall is explicit.

---

## 80.1 Disable First

Stop and disable the plugin before removal.

---

## 80.2 Package Removal

Remove:

* active registration;
* package versions not retained for evidence;
* package cache;
* and source update state.

---

## 80.3 Plugin Data

By default, uninstall preserves plugin data temporarily and asks whether to remove it.

---

## 80.4 Secrets

Secret grants are revoked on uninstall.

Underlying Vault secrets are not automatically deleted if they may be shared or user-owned.

---

## 80.5 Project Files

Uninstall never deletes or reverts project changes automatically.

Plugin-proposed changes are ordinary reviewed repository history.

---

## 80.6 Receipt

Produce an uninstall receipt describing:

* package versions removed;
* data retained or removed;
* permissions revoked;
* secrets unbound;
* and remaining recovery evidence.

---

# 81. Remove Plugin Data

Plugin data deletion is a separate operation.

---

## 81.1 Preview

Show paths, categories, size and backup status.

---

## 81.2 No Plugin Code

Data deletion is performed by trusted Opure services, not plugin uninstall code.

---

## 81.3 Secure Deletion

Do not promise forensic secure deletion on modern filesystems.

---

# 82. Disable

Disabling a plugin:

* stops its host;
* prevents activation;
* preserves package;
* preserves data;
* preserves permissions for later review;
* and stops update checks if policy says so.

---

# 83. Quarantine

A plugin is quarantined when:

* signature becomes invalid or revoked;
* package hash changes;
* malware detection occurs;
* source is compromised;
* publisher identity conflicts;
* package is withdrawn;
* or runtime behaviour triggers security policy.

---

## 83.1 Quarantine Effects

* stop host;
* revoke active capabilities;
* block activation;
* preserve evidence;
* and show remediation.

---

# 84. Package Withdrawal

A source may mark a plugin version deprecated, unlisted or withdrawn.

---

## 84.1 Source Metadata

Source metadata is advisory unless backed by trusted repository policy.

---

## 84.2 Opure Security Record

Opure may maintain a signed or curated blocklist later.

Not selected here.

---

## 84.3 Installed Version

An installed withdrawn version is disabled only according to explicit security policy or user decision.

A future critical block mechanism requires separate governance.

---

# 85. Publisher Transfer

Publisher transfer requires:

* old publisher identity;
* new publisher identity;
* source ownership evidence;
* package ID continuity;
* compatibility;
* permission review;
* and explicit developer approval.

No silent transfer through package metadata.

---

# 86. Source Compromise

If a configured source is compromised:

1. disable source;
2. stop automatic metadata checks;
3. compare installed hashes;
4. verify author signatures independently;
5. identify affected packages;
6. quarantine conflicting versions;
7. preserve evidence;
8. notify users;
9. rotate source credentials;
10. and require source reapproval.

---

# 87. Author Key Compromise

If an author certificate is revoked or compromised:

* suspend trust for that signer;
* identify package versions;
* evaluate timestamp and revocation time;
* quarantine affected active plugins when policy requires;
* and require publisher recovery evidence.

---

# 88. Repository Signer Rotation

Repository signing certificates can rotate.

The repository source must announce expected certificates through supported metadata.

Opure should:

* compare announced certificates;
* validate trusted rotation;
* update policy through review;
* and reject unexpected signer removal or substitution.

---

# 89. Package ID Squatting

A package ID appearing on a public source does not prove ownership.

Trust depends on:

* source binding;
* author signature;
* repository owner;
* and prior installation continuity.

---

# 90. Dependency Confusion

The no-dependency policy eliminates runtime transitive plugin dependency resolution initially.

Source ambiguity for the plugin package itself is mitigated by explicit source binding and source mappings.

---

# 91. Package Mutation

A package version is immutable.

If a source serves changed bytes for the same ID and version:

* block;
* alert;
* preserve both hashes for investigation;
* and do not replace installed content.

---

# 92. Package Export

The developer may export the exact installed `.nupkg` and verification receipt.

---

## 92.1 No Data Export by Default

Package export excludes plugin data and secrets.

---

# 93. Offline Backup

A verified plugin package may be backed up with:

* package file;
* source record;
* hashes;
* signatures;
* manifest;
* permission grant;
* and compatibility record.

Credentials are excluded.

---

# 94. Restore

Restoring a plugin package from backup repeats:

* hash verification;
* signature verification;
* compatibility;
* permission review;
* and activation.

A prior approval may be reused only when identity, publisher, source class and permissions match policy.

---

# 95. Stable and Preview Isolation

Stable, Preview and Development profiles have separate:

* package stores;
* data stores;
* source configuration;
* permissions;
* update state;
* and Plugin Hosts.

---

## 95.1 Package Reuse

Physical package-file deduplication across channels is deferred.

Logical trust and activation remain separate.

---

## 95.2 Import

A package can be imported from another channel only through explicit installation.

---

# 96. First-Party Plugins

First-party plugins use the same package format and installation pipeline where practical.

---

## 96.1 Bundled First-Party Plugin

A plugin bundled inside the Opure MSIX may be product-versioned and package-owned.

It still requires:

* manifest;
* permissions;
* Plugin Host isolation;
* and compatibility tests.

---

## 96.2 Downloaded First-Party Plugin

A separately downloaded first-party plugin is a normal signed `OpurePlugin` package.

---

# 97. Public Plugin SDK Package

The development SDK remains a normal NuGet dependency package.

It is not an `OpurePlugin` runtime package.

---

## 97.1 Distinct Package Types

Example:

```text
Opure.Plugin.Sdk
```

is a development package.

A runtime plugin package declares:

```text
OpurePlugin
```

---

## 97.2 Packaging Tool

The SDK may include a build-time pack tool that creates the runtime `.nupkg`.

It must not execute when a runtime plugin is installed into Opure.

---

# 98. Plugin Packaging CLI

A future command may be:

```powershell
dotnet opure-plugin pack
```

or:

```powershell
opure plugin pack
```

It should:

* validate manifest;
* collect closed payload;
* reject prohibited assets;
* generate `.nuspec`;
* generate SBOM;
* calculate hashes;
* create deterministic `.nupkg`;
* and run package tests.

---

## 98.1 No Signing Secret

Packaging and signing remain separate.

---

# 99. Deterministic Package Build

Package creation should control:

* file ordering;
* manifest generation;
* timestamps where supported;
* line endings;
* encoding;
* and file inventory.

---

## 99.1 Signature Difference

A NuGet signature changes package bytes.

Record unsigned and signed package hashes where author signing is used.

---

# 100. Package Validation CLI

A future command:

```powershell
opure plugin verify <package.nupkg>
```

should output:

* identity;
* version;
* package type;
* manifest schema;
* source;
* signatures;
* hashes;
* permissions;
* compatibility;
* paths;
* limits;
* SBOM status;
* licence;
* and result.

---

# 101. Developer Directory Mode

Plugin developers may run an unpacked development plugin from an explicit project output directory.

---

## 101.1 Developer Mode Only

Requires Developer Mode.

---

## 101.2 No Production Trust

Directory mode is unverified and cannot be enabled in Stable by default.

---

## 101.3 Change Detection

The host may restart on development rebuild.

---

## 101.4 Isolated Profile

Use Development channel or dedicated test profile.

---

# 102. Marketplace

A public Opure marketplace is deferred.

---

## 102.1 Why Deferred

It requires:

* publisher identity;
* moderation;
* content policy;
* malware review;
* security response;
* privacy;
* legal terms;
* takedown;
* ratings integrity;
* payments;
* and support.

---

## 102.2 No Implicit Endorsement

A plugin appearing on nuget.org or another feed is not endorsed by Opure.

---

# 103. Curated Feed Future Gate

A curated feed requires:

* repository signature;
* verified publisher records;
* package review;
* automated security analysis;
* source and licence policy;
* incident response;
* withdrawal;
* ownership transfer;
* and transparency log or equivalent audit.

---

# 104. Ratings and Reviews

Deferred.

No plugin installation decision should rely on unverified popularity metrics initially.

---

# 105. Paid Plugins

Deferred.

Payment and licence enforcement must not compromise local control or offline project access.

---

# 106. Enterprise Plugin Sources

An enterprise may configure:

* private NuGet V3 source;
* trusted repository certificate;
* owner restrictions;
* package ID mapping;
* and allowed permissions.

---

## 106.1 Policy Visibility

Managed source and restrictions are visible.

---

## 106.2 No Credential Exposure

Feed credentials remain in Vault or enterprise credential provider.

---

# 107. Plugin Lockfile

Each profile should record a plugin lock manifest containing:

```text
plugin_id
version
package_hash
source_id
publisher_identity
permissions_hash
manifest_hash
data_schema
installed_at
```

---

## 107.1 Purpose

Supports:

* backup;
* restore;
* diagnostics;
* reproducibility;
* and incident investigation.

---

## 107.2 Not Project Lockfile

The profile plugin lock does not modify developer repositories by default.

A project may later declare required plugins through a separate reviewed project format.

---

# 108. Project-Required Plugins

Deferred.

A project file must not silently install executable plugins when a repository is opened.

---

## 108.1 Future Behaviour

A project may declare recommended plugin IDs and versions.

Opure must show the request and require installation approval.

---

# 109. Plugin Update Policy

Update settings may include:

* Manual Only;
* Check from Approved Source;
* Notify Only;
* Pin Exact Version;
* and Enterprise Managed.

No initial setting silently installs.

---

## 109.1 Unverified Plugin

Local Unverified plugins are always pinned and manual.

---

## 109.2 Version Range

Automatic discovery may identify versions within an approved major range.

Installation remains explicit.

---

# 110. Version Pinning

A developer may pin:

* exact version;
* major line;
* prerelease inclusion;
* or no updates.

---

## 110.1 Security Warning

A pinned vulnerable version may show a warning without silently overriding the pin.

---

# 111. Prerelease Plugins

Prerelease versions are hidden by default in Stable.

---

## 111.1 Opt-In

The developer may enable prerelease for one plugin or source.

---

## 111.2 Stable to Prerelease

Requires explicit approval.

---

# 112. Deprecation

A deprecated plugin remains installed but displays:

* reason;
* replacement;
* support state;
* and last compatible Opure version.

---

# 113. Compatibility Loss After Opure Update

After an Opure application update:

* evaluate all plugins;
* disable incompatible versions;
* preserve package and data;
* show remediation;
* and check approved sources only according to plugin update policy.

---

## 113.1 No Silent Replacement

Do not install a different plugin version merely to restore compatibility without approval.

---

# 114. Plugin Crash Policy

Repeated crashes may automatically disable the plugin after a deterministic threshold.

---

## 114.1 Package Trust Unchanged

A crash does not prove package tampering.

---

## 114.2 Diagnostics

Record:

* plugin identity;
* version;
* package hash;
* host exit;
* operation;
* and safe exception summary.

---

# 115. Resource Abuse

Resource-limit violations may:

* cancel operation;
* terminate Plugin Host;
* disable plugin;
* and request review.

---

# 116. Permission Abuse

A plugin request outside granted permissions is denied and recorded.

Repeated or malicious attempts may quarantine the plugin.

---

# 117. Plugin Trust Centre

The Trust Centre should show:

* installed plugins;
* active versions;
* package hashes;
* sources;
* publisher identities;
* trust classes;
* permissions;
* network destinations;
* secret grants;
* last execution;
* faults;
* updates;
* revocations;
* and removals.

---

# 118. Plugin Diagnostics Bundle

A diagnostic bundle may include:

* manifest;
* package metadata;
* hashes;
* signature summary;
* source;
* compatibility;
* permissions;
* host logs;
* and crash records.

It excludes:

* plugin data by default;
* secrets;
* project contents;
* and authenticated feed credentials.

---

# 119. Privacy Impact

Remote plugin search and download reveal requested package metadata to the configured source.

No plugin source receives project data merely because the package is discovered or installed.

Runtime plugin data sharing follows granted permissions.

---

# 120. Performance Impact

Package verification adds:

* signature verification;
* archive scanning;
* hashing;
* extraction;
* and malware scanning.

These occur outside the UI thread and before activation.

Plugin Host process isolation adds memory overhead.

This is accepted for fault and trust separation.

---

# 121. Reliability Impact

Closed payloads reduce dependency drift.

Content-addressed versioned storage supports rollback and investigation.

External sources remain availability dependencies for discovery and download, not for running an installed plugin.

---

# 122. Offline Behaviour

Installed plugins run without package-source access unless their runtime permission requires network.

Offline installation from a local verified package is supported.

---

# 123. Security Threat Model

Relevant threats include:

* malicious package source;
* dependency confusion;
* package ID squatting;
* source substitution;
* publisher impersonation;
* package repacking;
* invalid or revoked signature;
* archive traversal;
* case-collision overwrite;
* reserved device name;
* alternate data stream;
* decompression bomb;
* nested executable archive;
* malicious native library;
* plugin assembly binding to trusted internals;
* permission smuggling;
* permission expansion during update;
* install-time code execution;
* mutable active package;
* plugin data destruction;
* source credential leakage;
* malicious ownership transfer;
* and compromised curated repository.

---

# 124. Security Controls

Controls include:

* explicit sources;
* source binding;
* package type;
* no dependency graph;
* signature verification;
* package hashes;
* immutable version identity;
* quarantine;
* bounded download;
* strict archive paths;
* file inventory;
* malware scan;
* likely-secret scan;
* no install scripts;
* no project assets;
* explicit permissions;
* dedicated Plugin Host;
* mediated filesystem, network and secret access;
* versioned content-addressed store;
* staged updates;
* permission diff;
* data backup;
* rollback rules;
* Trust Centre records;
* and incident procedures.

---

# 125. Testing Strategy

ADR-0008 applies.

Plugin packaging requires:

* unit tests;
* package-format tests;
* NuGet protocol tests;
* signature tests;
* archive security tests;
* permission tests;
* Plugin Host tests;
* update tests;
* recovery tests;
* and adversarial package corpora.

---

# 126. Unit Tests

Test:

* canonical plugin ID;
* ID equality;
* version equality;
* package-type parsing;
* manifest schema;
* compatibility ranges;
* RID selection;
* permission diff;
* source mapping;
* trust classification;
* package state machine;
* retention;
* and rollback eligibility.

---

# 127. Package Format Tests

Test:

* valid minimal package;
* valid full package;
* missing `.nuspec`;
* multiple `.nuspec`;
* missing package type;
* wrong package type;
* unsupported type version;
* missing Opure manifest;
* duplicate Opure manifest;
* mismatched ID;
* mismatched version;
* dependencies present;
* build assets;
* content assets;
* analyser assets;
* install scripts;
* unexpected tools;
* and malformed metadata.

---

# 128. Manifest Tests

Test:

* duplicate JSON property;
* invalid UTF-8;
* overlong manifest;
* unsupported schema;
* unknown required feature;
* invalid execution model;
* invalid entry path;
* invalid type name;
* invalid permission;
* wildcard permission;
* invalid compatibility range;
* invalid data schema;
* and malformed support metadata.

---

# 129. Archive Security Tests

Test:

* `../` traversal;
* absolute path;
* drive path;
* UNC;
* alternate data stream;
* trailing dot;
* trailing space;
* reserved device name;
* duplicate path;
* case collision;
* file-directory collision;
* excessive depth;
* excessive length;
* symlink;
* junction;
* reparse metadata;
* sparse or unusual file;
* decompression bomb;
* too many files;
* oversized file;
* nested archive;
* and overwrite attempt.

---

# 130. Signature Tests

Test:

* valid author signature;
* valid repository signature;
* both signature forms where supported;
* valid timestamp;
* missing timestamp;
* expired signing certificate with valid timestamp;
* invalid chain;
* untrusted chain;
* revoked certificate;
* changed signed content;
* unexpected repository certificate;
* unexpected repository owner;
* signer rotation;
* and unsigned package.

---

# 131. Source Tests

Test:

* local file;
* local folder;
* removable media;
* HTTPS V3 service index;
* package registration;
* package content;
* unlisted package;
* disabled source;
* removed source;
* authenticated feed;
* wrong credentials;
* source timeout;
* TLS failure;
* redirect;
* duplicate ID across sources;
* and source transfer.

---

# 132. Network Mediation Tests

Prove every remote request passes through:

* Network Gateway policy;
* destination allowlist;
* timeout;
* cancellation;
* proxy;
* and Trust Centre eventing.

No direct NuGet client network bypass is permitted.

---

# 133. Privacy Tests

Inspect source requests and confirm no:

* project content;
* project path;
* repository remote;
* prompt;
* secret;
* plugin data;
* machine ID;
* account name;
* or persistent tracking identifier.

---

# 134. Download Tests

Test:

* known content length;
* unknown content length;
* partial response;
* interrupted connection;
* disk full;
* cancellation;
* hash mismatch;
* cache collision;
* and quarantine cleanup.

---

# 135. Extraction Tests

Test:

* fresh staging directory;
* no overwrite;
* file hashes;
* package root;
* payload root;
* native RID;
* read-only policy;
* interrupted extraction;
* crash before commit;
* crash after commit;
* and orphaned staging cleanup.

---

# 136. Permission Tests

Test:

* first approval;
* decline;
* unchanged update;
* added permission;
* broadened permission;
* reduced permission;
* semantically changed catalogue;
* publisher change;
* source change;
* secret permission;
* network destination;
* and project scope.

---

# 137. Runtime Tests

Test:

* dedicated Plugin Host;
* authenticated bootstrap;
* correct package version;
* managed assembly load;
* private dependency load;
* host contract sharing;
* trusted-internal binding denial;
* native library resolution;
* initialisation timeout;
* cancellation;
* crash;
* memory limit;
* CPU limit;
* and process termination.

---

# 138. Assembly Isolation Tests

Create plugins with:

* conflicting private dependency versions;
* host contract duplicate;
* trusted assembly name collision;
* unmanaged dependency collision;
* and leaked references preventing ALC unload.

Verify process restart restores a clean boundary.

---

# 139. Install-Time Code Test

Instrument package installation and prove no plugin:

* assembly;
* native library;
* script;
* target;
* or tool

is executed before activation.

---

# 140. Update Tests

Test:

* patch update;
* minor update;
* major update;
* prerelease update;
* source mismatch;
* publisher mismatch;
* same version different hash;
* permission expansion;
* data migration;
* activation failure;
* old host still running;
* and update cancellation.

---

# 141. Rollback Tests

Test:

* compatible rollback;
* forward-only data;
* restored data backup;
* withdrawn old version;
* corrupted old version;
* revoked old signer;
* and missing old package.

---

# 142. Uninstall Tests

Test:

* disable;
* active host stop;
* package removal;
* data preservation;
* data deletion;
* secret grant revocation;
* project repository preservation;
* source removal;
* and receipt.

---

# 143. Stable and Preview Tests

Test:

* same plugin different channel;
* separate package stores;
* separate data;
* separate permissions;
* separate source binding;
* and no cross-channel host connection.

---

# 144. Incident Tests

Simulate:

* source compromise;
* author-key compromise;
* repository-signer rotation;
* package withdrawal;
* package repack;
* malware discovery;
* and malicious ownership transfer.

---

# 145. Performance Tests

Measure:

* source search;
* metadata retrieval;
* package download;
* signature verification;
* hashing;
* archive scan;
* malware scan;
* extraction;
* activation;
* Plugin Host memory;
* update;
* and rollback.

---

# 146. Accessibility Tests

Plugin discovery and permission UI must support:

* keyboard;
* Narrator;
* high contrast;
* reduced motion;
* focus management;
* and clear trust labels.

Trust must not be communicated by colour alone.

---

# 147. Fuzzing

Fuzz:

* `.nuspec`;
* plugin manifest;
* ZIP central directory;
* archive paths;
* version ranges;
* signature metadata;
* and permission structures.

---

# 148. Prototype Plan

## 148.1 Prototype A — Minimal Package

Create a minimal `OpurePlugin` package with one managed assembly and manifest.

---

## 148.2 Prototype B — NuGet V3 Source

Publish to a disposable local or private V3 source.

Discover and download through the Network Gateway.

---

## 148.3 Prototype C — Signature

Create:

* author-signed package;
* repository-signed package;
* unsigned package;
* and tampered package.

Verify trust classes.

---

## 148.4 Prototype D — Archive Corpus

Build malicious packages covering path, collision, link, size and nested-archive attacks.

---

## 148.5 Prototype E — Plugin Host

Load the plugin in a dedicated Plugin Host and verify dependency isolation.

---

## 148.6 Prototype F — Permissions

Install one package requesting project-read and network permissions.

Approve, deny and update with expanded permission.

---

## 148.7 Prototype G — Update

Stage a new version beside the old version, migrate plugin data and activate atomically.

---

## 148.8 Prototype H — Rollback

Fail candidate activation and return to last-known-good version.

---

## 148.9 Prototype I — Source Conflict

Publish the same ID and version with different bytes on two sources.

Verify conflict and no automatic switching.

---

## 148.10 Prototype J — Incident

Revoke or distrust a signer and quarantine an installed plugin.

---

# 149. Implementation Plan

1. Record founder review.
2. Define `OpurePlugin` package type.
3. Define plugin manifest schema version 1.
4. Define package layout.
5. Define permission catalogue version 1.
6. Build package validator.
7. Build archive security validator.
8. Build signing verifier.
9. Build source registry.
10. Prototype NuGet V3 client mediation.
11. Add local file and folder sources.
12. Add quarantine.
13. Add content-addressed package store.
14. Add plugin registration database.
15. Add permission approval UI.
16. Add Plugin Host package bootstrap.
17. Add managed dependency resolver.
18. Add package and data separation.
19. Add staged update.
20. Add data backup and migration.
21. Add rollback.
22. Add uninstall.
23. Add Trust Centre views.
24. Add security incident flows.
25. Add package CLI.
26. Add adversarial test corpus.
27. Complete security review.
28. Accept, amend or reject the ADR.

---

# 150. Owners

| Area                   | Owner                               |
| ---------------------- | ----------------------------------- |
| Product policy         | Founder                             |
| Package format         | Plugin Platform Owner               |
| Plugin manifest        | Plugin SDK Owner                    |
| NuGet protocol         | Package and Supply-Chain Owner      |
| Network mediation      | Network Gateway Owner               |
| Signature verification | Security Owner                      |
| Package store          | Persistence Owner                   |
| Plugin Host            | Runtime Architecture Owner          |
| Permissions            | Security and Plugin SDK Owners      |
| Data migration         | Plugin Platform and Recovery Owners |
| Desktop experience     | Desktop Owner                       |
| Trust Centre           | Trust Centre Owner                  |
| Test corpus            | Test Architecture Owner             |

---

# 151. Suggested Repository Structure

```text
src/
├── Plugins/
│   ├── Opure.Plugins.Contracts/
│   ├── Opure.Plugins.Packaging/
│   ├── Opure.Plugins.Sources/
│   ├── Opure.Plugins.Security/
│   ├── Opure.Plugins.Storage/
│   └── Opure.Plugins.Runtime/
├── Hosts/
│   └── Opure.PluginHost/
└── Tools/
    └── Opure.Plugin.Tools/

schemas/
└── plugins/
    ├── plugin-manifest-v1.schema.json
    └── permission-catalogue-v1.json

tests/
└── Plugins/
    ├── Opure.Plugins.UnitTests/
    ├── Opure.Plugins.PackageTests/
    ├── Opure.Plugins.SecurityTests/
    ├── Opure.Plugins.ProtocolTests/
    └── Fixtures/
        ├── Valid/
        └── Malicious/
```

Exact project count should remain consistent with ADR-0010 and may consolidate modules.

---

# 152. Example `.nuspec`

Conceptual only:

```xml
<?xml version="1.0"?>
<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
  <metadata>
    <id>Acme.Opure.RepositoryReview</id>
    <version>1.2.0</version>
    <authors>Acme Engineering</authors>
    <description>Repository review workflow for Opure.</description>
    <license type="expression">MIT</license>
    <projectUrl>https://example.invalid/plugin</projectUrl>
    <repository
      type="git"
      url="https://example.invalid/repository.git"
      commit="0123456789abcdef" />
    <packageTypes>
      <packageType name="OpurePlugin" version="1.0" />
    </packageTypes>
    <readme>README.md</readme>
  </metadata>
  <files>
    <file src="README.md" target="README.md" />
    <file src="opure\**" target="opure" />
  </files>
</package>
```

It intentionally contains no dependencies.

---

# 153. Example Plugin Manifest

Conceptual only:

```json
{
  "schema": "opure.plugin-manifest/1",
  "id": "Acme.Opure.RepositoryReview",
  "version": "1.2.0",
  "display_name": "Acme Repository Review",
  "description": "Adds a review workflow for local repositories.",
  "publisher": {
    "display_name": "Acme Engineering"
  },
  "execution": {
    "model": "managed-dotnet-v1",
    "assembly": "opure/payload/managed/Acme.RepositoryReview.dll",
    "type": "Acme.RepositoryReview.Plugin"
  },
  "plugin_host_protocol": "1.x",
  "opure_api": ">=1.0.0 <2.0.0",
  "supported_platforms": ["windows"],
  "supported_architectures": ["x64"],
  "permissions": [
    {
      "id": "project.files.read",
      "reason": "Reads source files selected for review."
    },
    {
      "id": "project.patch.propose",
      "reason": "Produces reviewable patch proposals."
    }
  ],
  "data_schema": {
    "version": 1,
    "migration": "compatible",
    "rollback": true
  },
  "licence": {
    "expression": "MIT"
  }
}
```

The final schema may use structured version ranges rather than free-form strings.

---

# 154. Package Verification Receipt

A receipt should contain:

```text
operation_id
plugin_id
version
package_type
manifest_schema
source_id
package_sha256
nuget_content_hash
nuspec_sha256
manifest_sha256
author_signature
repository_signature
trusted_owner
trust_class
publisher_identity
permissions_hash
compatibility_result
scan_result
installed_path_reference
installed_at
approved_by
```

No feed credential.

---

# 155. Error Model

Stable error categories include:

* Package Source Unavailable;
* Package Download Failed;
* Package Too Large;
* Package Hash Mismatch;
* Package Signature Invalid;
* Package Signer Untrusted;
* Package Type Missing;
* Package Type Unsupported;
* Plugin Manifest Missing;
* Plugin Manifest Invalid;
* Plugin Identity Mismatch;
* Plugin Version Mismatch;
* Dependencies Prohibited;
* Project Asset Prohibited;
* Archive Path Invalid;
* Archive Collision;
* Archive Limit Exceeded;
* Native Architecture Mismatch;
* Compatibility Failed;
* Permission Denied;
* Permission Review Required;
* Package Store Failed;
* Activation Failed;
* Data Migration Failed;
* Rollback Unsupported;
* Source Conflict;
* Publisher Changed;
* Package Withdrawn;
* Package Corrupted;
* and Recovery Required.

---

# 156. User-Facing Trust Labels

Use clear labels:

* Verified author;
* Verified repository;
* Verified curated publisher;
* Exact local hash approved;
* Unverified local package;
* Signature invalid;
* Signer revoked;
* Source conflict;
* and Package withdrawn.

Do not use only:

* Safe;
* Trusted;
* or Secure

without the precise basis.

---

# 157. Acceptance Criteria

This ADR may move to **Accepted** when all of the following hold.

## Package Format

* [ ] Package is standard `.nupkg`.
* [ ] Package type is `OpurePlugin` version `1.0`.
* [ ] `opure/plugin.json` is required.
* [ ] Manifest schema is versioned independently.
* [ ] Package and manifest IDs match.
* [ ] Package and manifest versions match.
* [ ] NuGet dependencies are prohibited.
* [ ] Project mutation assets are prohibited.
* [ ] Install scripts are prohibited.
* [ ] Package layout is documented.
* [ ] Managed entry point is explicit.
* [ ] Windows x64 compatibility is explicit.
* [ ] Private dependencies are closed in payload.
* [ ] Shared contract assemblies are host owned.

## Sources and Network

* [ ] Local file source works.
* [ ] Local folder source works.
* [ ] HTTPS NuGet V3 source works.
* [ ] V2-only source is rejected.
* [ ] No source is auto-discovered.
* [ ] Every remote request is mediated.
* [ ] Source credentials remain in Vault.
* [ ] Package ID is bound to a source.
* [ ] Ambiguous sources block automatic resolution.
* [ ] Source transfer is reviewed.
* [ ] Remote requests contain no project or secret data.

## Trust

* [ ] Author signature verification works.
* [ ] Repository signature verification works.
* [ ] Timestamp validation works.
* [ ] Invalid signature is rejected.
* [ ] Untrusted signature is not labelled verified.
* [ ] Unsigned local installation requires Developer Mode.
* [ ] Unsigned local packages cannot auto-update.
* [ ] Same ID/version with different hash is rejected.
* [ ] Publisher display text is not treated as identity.
* [ ] Signer rotation and ownership transfer require review.
* [ ] Hashes and verification receipt are persisted.

## Package Security

* [ ] Download occurs in quarantine.
* [ ] Compressed size is bounded.
* [ ] Expanded size is bounded.
* [ ] File count is bounded.
* [ ] Individual file size is bounded.
* [ ] Absolute paths are rejected.
* [ ] Traversal is rejected.
* [ ] UNC and drive paths are rejected.
* [ ] Reserved names are rejected.
* [ ] ADS syntax is rejected.
* [ ] Case collisions are rejected.
* [ ] Duplicate paths are rejected.
* [ ] Symlinks and reparse attempts are rejected.
* [ ] Nested executable archives are rejected.
* [ ] Native architecture is verified.
* [ ] Malware scan integration is tested.
* [ ] Likely-secret scan is tested.
* [ ] No plugin code runs during install.

## Store and Runtime

* [ ] Package store is versioned and content addressed.
* [ ] Package payload is immutable after commit.
* [ ] Writable plugin data is separate.
* [ ] Active version is database authoritative.
* [ ] No mutable active symlink is required.
* [ ] Dedicated Plugin Host runs each plugin.
* [ ] Third-party code never loads into Runtime or Desktop.
* [ ] Private dependency isolation works.
* [ ] Host contract substitution is blocked.
* [ ] Native resolution is bounded.
* [ ] Process termination provides final unload.
* [ ] Resource limits are enforceable.
* [ ] Network, filesystem and secrets remain mediated.

## Permissions

* [ ] Permissions are machine readable.
* [ ] Wildcard permission is prohibited.
* [ ] First activation requires review.
* [ ] Approval shows source, publisher and trust.
* [ ] Approval is bound to identity and source.
* [ ] Added permissions require reapproval.
* [ ] Broadened permissions require reapproval.
* [ ] Publisher change suspends sensitive grants.
* [ ] Secret grants are revoked on uninstall.

## Updates and Recovery

* [ ] Plugin updates are separate from application update.
* [ ] Manual install is default.
* [ ] No plugin can self-update.
* [ ] Candidate passes full verification.
* [ ] Candidate stages beside active version.
* [ ] Active files are never overwritten.
* [ ] Old host stops before activation.
* [ ] Plugin data is backed up before schema migration.
* [ ] Activation failure preserves old version.
* [ ] Permission rejection preserves old version.
* [ ] Rollback checks data compatibility.
* [ ] Forward-only migration blocks false rollback.
* [ ] Last-known-good retention is bounded.
* [ ] Withdrawn or revoked versions cannot reactivate silently.

## Uninstall and Channels

* [ ] Uninstall stops the Plugin Host.
* [ ] Package removal is explicit.
* [ ] Data preservation choice is explicit.
* [ ] Data deletion uses trusted services.
* [ ] Project repositories are never deleted.
* [ ] Stable and Preview stores are isolated.
* [ ] Stable and Preview permissions are isolated.
* [ ] Diagnostic export excludes plugin data and secrets.
* [ ] Security review is complete.
* [ ] Founder approval is recorded.

---

# 158. Evidence Required Before Acceptance

* [ ] Package-format comparison.
* [ ] Package-type test.
* [ ] Manifest schema.
* [ ] Valid package corpus.
* [ ] Malicious archive corpus.
* [ ] NuGet V3 protocol report.
* [ ] Network mediation report.
* [ ] Privacy request capture.
* [ ] Author-signature report.
* [ ] Repository-signature report.
* [ ] Invalid-signature report.
* [ ] Local unverified flow.
* [ ] Package hash and repack report.
* [ ] Package source conflict report.
* [ ] Quarantine and extraction report.
* [ ] Malware and secret-scan report.
* [ ] Package-store report.
* [ ] Permission review screenshots.
* [ ] Plugin Host isolation report.
* [ ] Assembly dependency conflict report.
* [ ] Native dependency report.
* [ ] Update and permission-diff report.
* [ ] Data migration report.
* [ ] Rollback report.
* [ ] Uninstall and data-preservation report.
* [ ] Stable and Preview isolation report.
* [ ] Source compromise simulation.
* [ ] Signer compromise simulation.
* [ ] Performance report.
* [ ] Accessibility report.
* [ ] Security review.
* [ ] Founder approval.

---

# 159. Known Limitations

* The final manifest schema is not defined.
* The final permission catalogue is not defined.
* The exact NuGet client package versions are not selected.
* Network Gateway integration with NuGet client libraries requires prototype evidence.
* The final public plugin source is not selected.
* nuget.org is not an Opure-specific marketplace.
* NuGet package search may not expose all desired plugin metadata efficiently.
* Generic feeds may not support repository signatures.
* Author signing requires X.509 infrastructure.
* ADR-0014 defers NuGet author signing for first-party packages.
* Malware scanning cannot prove safety.
* Same-user malware can modify user-owned plugin files.
* ACLs do not create a full same-user sandbox.
* `AssemblyLoadContext` is not a security boundary.
* Process isolation has memory overhead.
* Initial plugins are managed .NET and Windows x64 only.
* External-process and WebAssembly models are deferred.
* NuGet dependency groups are prohibited, increasing package size.
* Public marketplace governance is deferred.
* Plugin source-code review is deferred.
* Ratings, payments and licensing services are deferred.
* Project-required plugin declarations are deferred.
* Public security blocklist is deferred.
* Strong metadata transparency log is deferred.
* Secure deletion of plugin data is not guaranteed.

---

# 160. Open Questions

* What exact package-type version string should be used?
* Should the package ID require an `Opure.Plugin.` prefix?
* Should publisher namespace ownership be enforced by a curated source?
* Should canonical plugin IDs be lower-case in `plugin.json`?
* Should the package use `opure/` or another payload root?
* Should a package contain multiple architecture payloads?
* Should the first package support managed-only `any` payloads?
* Which target framework should plugins compile against?
* Should the host expose `netstandard` contracts or a .NET 10 contract?
* Which assemblies are shared by the Plugin Host?
* How is host contract binding enforced?
* Should native libraries require upstream Authenticode signatures?
* Which package-size limits are appropriate?
* Which compression-ratio policy is appropriate?
* Which Windows Defender API should be used?
* Should private plugin packages ever be submitted to cloud malware analysis?
* What likely-secret scanner should be reused?
* Is SPDX the canonical plugin SBOM?
* Should CycloneDX also be accepted?
* Should SBOM become mandatory for all remote sources?
* How should licence policy work for proprietary plugins?
* Which NuGet client libraries will be pinned?
* Can NuGet Protocol use a fully mediated HTTP pipeline?
* Should a Package Source Worker be isolated out of process?
* Which NuGet V3 search and registration resources are mandatory?
* Should unlisted packages remain installable by exact version?
* Should nuget.org be enabled by default?
* Should nuget.org package owners be part of trust binding?
* How are repository-signer certificate rotations approved?
* What author certificate authorities are acceptable?
* Should author-signed packages be trusted across sources?
* Should unsigned packages from a trusted repository be allowed outside Developer Mode?
* What curated-source review is required?
* How should publisher transfer be proven?
* Should package source mappings be exact-ID only initially?
* How are authenticated feed credential providers sandboxed?
* Should source configuration be exportable?
* How are enterprise sources distributed?
* How often may plugin sources be checked for updates?
* Should a plugin be able to pin a compatible Opure API range?
* How is permission semantic-versioning represented?
* Which permissions are considered high impact?
* Should permission reductions activate automatically?
* How are plugin configuration migrations specified?
* Which data backup format is used?
* How many previous plugin versions are retained?
* What storage quota applies to plugin packages and data?
* Should package files be rehashed on every launch or sampled?
* Can Stable and Preview physically deduplicate identical packages?
* Should an installed package be exportable as exact original bytes?
* Should project files declare recommended plugins?
* When should external-process plugins be introduced?
* When should WebAssembly plugins be introduced?
* Should a public transparency log record curated packages?
* Should TUF protect a future curated plugin feed?
* How should critical plugin revocation work offline?
* Should plugin signing use a future Sigstore identity in addition to NuGet signatures?
* How should paid plugin licences remain local and privacy preserving?

---

# 161. Deferred Decisions

This ADR intentionally defers:

* final Plugin SDK contracts;
* final permission catalogue;
* public Opure plugin feed;
* marketplace governance;
* publisher verification service;
* plugin review service;
* automated malware cloud scanning;
* public revocation blocklist;
* TUF metadata;
* Sigstore integration;
* external-process plugins;
* WebAssembly plugins;
* scripting plugins;
* cross-platform payloads;
* paid plugins;
* project-required plugins;
* plugin ratings;
* plugin telemetry;
* and plugin UI composition.

---

# 162. Alternatives Rejected

A custom `.opureplugin` archive is rejected because it would require custom signing, indexing and repository infrastructure.

OCI is deferred because its registry and client model is heavier than the initial desktop plugin ecosystem.

MSIX extension packages are rejected because they bind plugins to Windows package-family semantics.

Plain ZIP and directory are not the production package because they lack standard identity and signature infrastructure.

Git repository installation is rejected because it executes builds and resolves dependencies from mutable source.

`PackageReference` is rejected because it mutates developer projects and restores dependencies.

Executable installers are rejected because installation must not run plugin code.

Language-specific package formats are rejected as a universal model.

NuGet transitive dependencies are prohibited because a reviewed plugin should have a closed payload and predictable runtime.

---

# 163. Official and Primary Evidence References

## NuGet Package Format and Metadata

* [Create a NuGet package](https://learn.microsoft.com/en-us/nuget/create-packages/creating-a-package)
* [.nuspec file reference](https://learn.microsoft.com/en-us/nuget/reference/nuspec)
* [Set a NuGet package type](https://learn.microsoft.com/en-us/nuget/create-packages/set-package-type)
* [Create a NuGet package with MSBuild](https://learn.microsoft.com/en-us/nuget/create-packages/creating-a-package-msbuild)

## NuGet V3 Protocol

* [NuGet Server API overview](https://learn.microsoft.com/en-us/nuget/api/overview)
* [Service index](https://learn.microsoft.com/en-us/nuget/api/service-index)
* [Package content resource](https://learn.microsoft.com/en-us/nuget/api/package-base-address-resource)
* [Repository signatures resource](https://learn.microsoft.com/en-us/nuget/api/repository-signatures-resource)
* [Catalog resource](https://learn.microsoft.com/en-us/nuget/api/catalog-resource)

## NuGet Trust

* [Signed packages reference](https://learn.microsoft.com/en-us/nuget/reference/signed-packages-reference)
* [Sign a NuGet package](https://learn.microsoft.com/en-us/nuget/create-packages/sign-a-package)
* [`dotnet nuget sign`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-nuget-sign)
* [NuGet verify command](https://learn.microsoft.com/en-us/nuget/reference/cli-reference/cli-ref-verify)
* [Manage package trust boundaries](https://learn.microsoft.com/en-us/nuget/consume-packages/installing-signed-packages)
* [NuGet signed-package verification](https://learn.microsoft.com/en-us/dotnet/core/tools/nuget-signed-package-verification)
* [Package Source Mapping](https://learn.microsoft.com/en-us/nuget/consume-packages/package-source-mapping)
* [`nuget.config` reference](https://learn.microsoft.com/en-us/nuget/reference/nuget-config-file)
* [Authenticated package feeds](https://learn.microsoft.com/en-us/nuget/consume-packages/consuming-packages-authenticated-feeds)

## .NET Plugin Loading

* [Create a .NET application with plugins](https://learn.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support)
* [Load and unload assemblies](https://learn.microsoft.com/en-us/dotnet/standard/assembly/load-unload)
* [Assembly unloadability](https://learn.microsoft.com/en-us/dotnet/standard/assembly/unloadability)
* [`AssemblyLoadContext`](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.loader.assemblyloadcontext)

NuGet protocol, signature, package and .NET loading behaviour can change.

The implementation must revalidate the selected client versions and source capabilities before acceptance.

---

# 164. Review Record

| Date         | Reviewer           | Decision | Notes                                                                                                 |
| ------------ | ------------------ | -------- | ----------------------------------------------------------------------------------------------------- |
| 18 July 2026 | Architecture draft | Proposed | NuGet `OpurePlugin` package with closed payload, explicit trust and dedicated Plugin Host recommended |

---

# 165. Approval

## Founder or Product Approval

* **Name:** Christopher Dyer
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Package, source and Developer Mode policy review required

## Plugin Platform Approval

* **Name or role:** Plugin Platform Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Package pipeline, store, update and recovery evidence required

## Plugin SDK Approval

* **Name or role:** Plugin SDK Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Manifest, compatibility and permission contracts required

## Security Approval

* **Name or role:** Security Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Signatures, archive validation, permissions and incident response required

## Runtime Approval

* **Name or role:** Runtime Architecture Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Plugin Host, assembly isolation and process limits required

## Package and Network Approval

* **Name or role:** Package and Network Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** NuGet V3 and Network Gateway mediation required

## Test Approval

* **Name or role:** Test Architecture Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Malicious package corpus and lifecycle acceptance suite required

---

# 166. Supersession

This ADR is superseded only when a later ADR:

* names ADR-0016 explicitly;
* explains why package format, source protocol, trust model or execution model changed;
* identifies installed-plugin migration;
* describes package-store, permission and data impact;
* explains ecosystem compatibility;
* and updates the `Superseded by` field.

Historical package hashes, source records and signer identities remain available for investigation.

---

# 167. Change History

| Version | Date         | Author        | Summary                                                                                |
| ------- | ------------ | ------------- | -------------------------------------------------------------------------------------- |
| 0.1     | 18 July 2026 | Founder Draft | Initial NuGet `OpurePlugin`, source, trust, package-store and lifecycle recommendation |

---

# 168. Final Decision Statement

> **Opure will provisionally package plugins as standard NuGet `.nupkg` files declaring package type `OpurePlugin` version `1.0`, containing an independently versioned Opure manifest and a closed Windows x64 managed payload with no NuGet dependencies, project assets or install-time code, acquired only from explicit local or HTTPS NuGet V3 sources through the Network Gateway, verified through package hashes and distinct author, repository or local trust classes, installed into an immutable content-addressed package store, granted explicit permissions and executed only inside a dedicated Plugin Host process, because an executable extension must remain inspectable, source-bound, permissioned, isolated and recoverable without modifying developer projects or inventing a proprietary package ecosystem.**

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**