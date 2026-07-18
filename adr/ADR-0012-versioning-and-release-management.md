# ADR-0012 — Versioning and Release Management

## Architecture Decision Record

**Status:** Proposed
**Date:** 18 July 2026
**Decision owners:** Founder and Product Owner
**Technical owners:** Release Engineering Owner
**Reviewers:** Repository Architecture Owner, Build and Continuous Integration Owner, Test Architecture Owner, Security Owner, Plugin SDK Owner, Runtime Architecture Owner, Desktop Owner, Documentation Owner
**Supersedes:** None
**Superseded by:** None
**Related ADRs:** ADR-0001 Primary Implementation Language, ADR-0002 Desktop Application Framework, ADR-0003 Runtime Process Topology, ADR-0004 Local IPC, ADR-0005 Persistence, ADR-0006 Logging and Observability, ADR-0007 Secrets Vault, ADR-0008 Testing Strategy, ADR-0009 Windows Path and Filesystem Handling, ADR-0010 Repository and Solution Structure, ADR-0011 Build and Continuous Integration
**Related specifications:** CHARTER-001, SPEC-001, SPEC-002, SPEC-003, SPEC-007, SPEC-008, SPEC-010, SPEC-011, SPEC-012
**Target milestone:** Phase 0 — Founding Baseline through Version 1.0

---

## 1. Decision Summary

Opure should use **Semantic Versioning 2.0.0** for the product and all first-party components shipped on the unified product release train.

The repository should use:

* one root `version.json` as the authoritative product-version source;
* **Nerdbank.GitVersioning 3.10.70** as the initial pinned version-stamping implementation;
* the `Nerdbank.GitVersioning` MSBuild package for assembly and package stamping;
* the matching `nbgv` repository-local .NET tool for version inspection and release preparation;
* one product version for Desktop, Runtime, Worker, Plugin Host, first-party adapters and bundled command-line tools;
* SemVer prerelease labels `preview.N`, `beta.N` and `rc.N`;
* exact release tags in the form `vMAJOR.MINOR.PATCH[-PRERELEASE]`;
* no SemVer build metadata in tag names;
* unique commit-derived versions for all non-release builds;
* product display versions that include the source commit for non-release builds;
* annotated release tags;
* cryptographically signed release tags when the signing identity is available;
* GitHub immutable releases before any public distribution;
* release assets built once, fully tested, then promoted without rebuilding;
* a human-curated root `CHANGELOG.md`;
* release notes derived from the changelog and release evidence rather than raw Git history;
* one release evidence bundle for every public prerelease and stable release;
* no replacement or mutation of a published version;
* and explicit compatibility, migration, support and withdrawal rules.

The initial version domain should be unified:

```text
Opure product
Desktop
Runtime
Worker
Plugin Host
first-party provider adapters
first-party MCP integration
first-party command-line tools
bundled public SDK packages
```

These components use the same product SemVer through Version 1.0.

The following remain independently versioned because they express technical compatibility rather than product marketing:

* local IPC protocol compatibility;
* Plugin Host protocol compatibility;
* service database schemas;
* workflow checkpoint formats;
* Vault record formats;
* diagnostic export formats;
* plugin manifest schema;
* and public file-format versions.

The versioning tool must not infer product-version significance from:

* commit messages;
* branch names;
* pull-request labels;
* number of commits;
* or AI-generated release notes.

Humans choose whether a change is major, minor or patch by evaluating the declared public compatibility surface.

Nerdbank.GitVersioning supplies reproducible source identity and stamping.

It does not decide release meaning.

---

## 2. Status

Current status:

> **Proposed**

This ADR becomes **Accepted** only after a versioning and release prototype demonstrates:

* root `version.json`;
* pinned Nerdbank.GitVersioning package and tool;
* unique versions for separate commits;
* reproducible version calculation from a clean clone;
* non-public commit identity suffixes;
* exact public prerelease versions;
* exact stable versions;
* assembly version mapping;
* file version mapping;
* informational version mapping;
* NuGet package version mapping;
* UI version display;
* tag-to-version validation;
* immutable draft-to-publish release flow;
* exact artefact promotion;
* changelog validation;
* release evidence generation;
* withdrawn-release handling;
* and one complete simulated release cycle.

---

## 3. Context

Opure will ship multiple tightly coordinated executables and libraries:

* `Opure.Desktop`;
* `Opure.Runtime`;
* `Opure.Worker`;
* `Opure.PluginHost`;
* local IPC contracts;
* first-party adapters;
* Plugin SDK packages;
* command-line utilities;
* recovery tooling;
* and future installer or updater components.

The system also contains compatibility versions that must not be confused with product versions:

* transport protocol generations;
* database schema versions;
* persisted workflow formats;
* Vault record formats;
* and plugin manifest versions.

Without a deliberate version model, likely problems include:

* Desktop and Runtime appearing compatible merely because their product versions match;
* assemblies reporting inconsistent versions;
* packages using one version while binaries display another;
* release tags not matching artefacts;
* preview builds colliding in package caches;
* release candidates being renamed to stable without retesting;
* published assets being replaced silently;
* Git tags being moved;
* release notes becoming raw commit dumps;
* accidental public publication from `main`;
* old database formats being treated as product versions;
* and uncertainty about whether a user can downgrade safely.

Opure also needs to distinguish between:

* a development build;
* a public preview;
* a beta;
* a release candidate;
* a stable release;
* a withdrawn release;
* and a supported release.

Those are related concepts but are not identical.

---

## 4. Problem Statement

Opure requires a versioning and release-management architecture that gives every build a unique source identity, communicates compatibility meaning to developers, coordinates multiple shipped components, prevents mutation of published versions, supports prerelease channels and produces release evidence tied to the exact binaries that passed validation.

---

## 5. Decision Drivers

The decision is evaluated against:

* alignment with the Opure Charter;
* semantic clarity;
* developer trust;
* reproducible source identity;
* monorepo simplicity;
* multi-process coordination;
* NuGet compatibility;
* Windows file-version compatibility;
* assembly identity;
* public SDK compatibility;
* release immutability;
* release traceability;
* prerelease ordering;
* Git tag clarity;
* build-once promotion;
* downgrade honesty;
* migration safety;
* hotfix support;
* small-team operation;
* CI integration;
* local build integration;
* provider neutrality;
* package publication;
* changelog quality;
* and future independent component versioning.

---

## 6. Governing Principles

This decision must preserve:

* **Developer Respect**
* **Developer First**
* **Software Engineering First**
* **Local by Design**
* **Cloud Optional**
* **Human in Control**
* **Visible by Design**
* **Inspectable Decisions**
* **Reviewable Changes**
* **Reversible Wherever Technically Practical**
* **Open by Architecture**
* **Loose Coupling**
* **Performance Respect**
* **No Hidden Authority**
* **No Mutable Published Version**
* **No Release Rebuild**
* **No False Compatibility**
* **No Version Inference Theatre**
* **No Commit-Log Changelog**
* **No Silent Downgrade Promise**
* **Evidence-Based Confidence**

Relevant commitments include:

* developers can identify the exact source of a build;
* public compatibility promises are explicit;
* schema compatibility is checked independently from product version;
* release notes describe developer-relevant change;
* security corrections remain visible;
* withdrawn releases remain identifiable;
* and release publication remains a deliberate human-controlled action.

---

## 7. Scope

This ADR decides:

* product-version format;
* version source;
* versioning tool;
* prerelease labels;
* build identity;
* assembly versions;
* Windows file versions;
* informational versions;
* NuGet package versions;
* tag naming;
* release branches;
* release channels;
* changelog format;
* release notes;
* release candidates;
* stable releases;
* hotfixes;
* release immutability;
* release withdrawal;
* package deprecation;
* support statements;
* compatibility declarations;
* release evidence;
* and version validation.

This ADR does not decide:

* installer technology;
* updater technology;
* code-signing provider;
* long-term support policy;
* distribution mirrors;
* package-feed credentials;
* public source licence;
* marketing release cadence;
* automatic-update rollout percentage;
* or enterprise deployment rings.

---

## 8. Constraints

Known constraints include:

* The repository is a modular monorepo.
* `Opure.slnx` is the solution of record.
* .NET 10 LTS is the implementation baseline.
* GitHub Actions is the initial CI provider.
* Release publication is not yet enabled.
* Code signing is deferred.
* The founder is initially the only release authority.
* Public SDK packages may be published later.
* NuGet uses SemVer 2.0.0 with specific normalisation rules.
* .NET assembly versions use four numeric components.
* Windows file versions use four numeric components.
* A Git tag can be moved unless protected by process or platform policy.
* A NuGet package ID and version cannot be overwritten on nuget.org.
* GitHub immutable releases prevent tag and asset changes after publication.
* Preview builds may be consumed internally before public distribution.
* A stable candidate may fail before publication.
* Database migrations may make downgrades unsafe.
* Product components must reject incompatible protocols independently.
* Release tooling runs in full trust.
* Version calculation must work locally without GitHub.
* Shallow clones can affect Git-history-derived version systems.
* The versioning mechanism must remain understandable to a small team.
* The source commit must remain discoverable in every non-release build.

---

## 9. Assumptions

This decision assumes:

* Git history is available for ordinary local and CI builds.
* Release builds use a complete enough clone for version calculation.
* Nerdbank.GitVersioning remains compatible with the selected SDK.
* Version stamping can be applied centrally through MSBuild.
* The root `version.json` can govern all first-party projects.
* All bundled first-party components can release together through Version 1.0.
* Public SDK compatibility remains manageable on the same release train initially.
* The release workflow can create draft GitHub releases.
* GitHub immutable releases can be enabled before public distribution.
* A signing identity can be added later without changing tag naming.
* Release notes can be curated in Markdown.
* Version changes are reviewed like source changes.
* Product builds do not need a date-based version.
* A release can be delayed rather than publishing uncertain binaries.
* The release evidence bundle can disambiguate multiple unpublished stable candidates with the same intended SemVer.
* Only one candidate for a SemVer becomes public.
* If an exact version escapes externally, that version is considered consumed.

---

## 10. Current Standards and Tool Evidence

Primary documentation available on 18 July 2026 establishes that:

* Semantic Versioning 2.0.0 uses `MAJOR.MINOR.PATCH`.
* SemVer increments major for incompatible public API changes, minor for backward-compatible functionality and patch for backward-compatible fixes.
* SemVer prerelease labels follow a hyphen and have lower precedence than the corresponding normal version.
* SemVer build metadata follows `+` and does not affect precedence.
* Once a version is released, its contents must not be modified.
* NuGet supports SemVer 2.0.0 and prerelease identifiers such as `beta.1` and `rc.1`.
* NuGet normalises some version forms and strips SemVer build metadata for version matching.
* NuGet package identity is package ID plus exact version.
* nuget.org does not support ordinary permanent package deletion; unlisted versions remain restorable by exact version.
* .NET distinguishes assembly identity version, file version and informational version.
* Nerdbank.GitVersioning uses one `version.json`, integrates with MSBuild and can include commit identity for non-public builds.
* Nerdbank.GitVersioning does not depend on branch names or tags to generate a unique commit version.
* Git annotated tags are intended for releases.
* Git supports GPG, SSH and X.509 tag signatures.
* GitHub immutable releases lock the associated tag and assets after publication.
* GitHub immutable releases automatically generate a release attestation.
* GitHub recommends creating a release as a draft, attaching all assets and publishing only when complete.
* Keep a Changelog recommends a human-oriented `CHANGELOG.md`, an `Unreleased` section and grouped notable changes rather than a raw Git log.

The implementation team must verify the exact pinned tools and platform settings before this ADR moves to Accepted.

---

## 11. Options Considered

The principal version-source and tooling options are:

1. **Option A — Nerdbank.GitVersioning with Root `version.json`**
2. **Option B — MinVer with Git Tags as the Primary Source**
3. **Option C — GitVersion with Branch and History Inference**
4. **Option D — Handwritten `Version.props` and CI Build Numbers**
5. **Option E — Calendar Versioning**
6. **Option F — Independent Version per Project**
7. **Option G — Date Plus Commit Only**
8. **Option H — Conventional-Commit Automatic Release Versioning**

---

# 12. Option A — Nerdbank.GitVersioning

## 12.1 Description

Use a root `version.json`.

Nerdbank.GitVersioning calculates assembly, package and informational versions from:

* the declared base version;
* Git version height;
* public-release status;
* and source commit.

Use a pinned MSBuild package and matching local CLI tool.

---

## 12.2 Advantages

* One explicit version source.
* Unique version per commit.
* Source commit embedded.
* No branch-name dependency.
* No tag dependency for ordinary build reproducibility.
* Works locally and in CI.
* MSBuild integration.
* NuGet integration.
* Cross-platform.
* Supports public and non-public release distinction.
* Supports SemVer 2 package versions.
* Supports assembly version policy.
* Supports release preparation.
* Supports monorepo inheritance if later required.
* Strong traceability.
* Strong package-cache collision avoidance.
* No need to invent Git traversal logic.
* .NET Foundation-supported project.
* Current stable release available for the selected baseline.
* Easy to backtrack from version to source commit.
* Fits root `version.json` requirement from ADR-0010.

---

## 12.3 Disadvantages

* Adds a trusted build package and tool.
* Version-height behaviour requires understanding.
* Public-release configuration must be correct.
* Shallow clones may need configuration.
* Generated version forms can be surprising.
* Release version changes still require human process.
* A tool upgrade can alter output.
* Public release and SemVer stability are separate concepts.
* Exact assembly/file-version mapping requires testing.
* Release branches can create merge conflicts in `version.json`.

---

## 12.4 Risks

* Main accidentally classified as public.
* non-public suffix omitted;
* version height reset unexpectedly;
* release tag not matching computed version;
* tool and package versions differ;
* package version collision;
* shallow checkout;
* version file modified but uncommitted;
* or automatic release helper used without reviewing branch effects.

---

## 12.5 Mitigations

* tag-only public-release pattern;
* exact tool pin;
* exact package pin;
* version command in CI;
* clean tree requirement;
* full version manifest;
* release validation;
* no automatic public publication;
* and acceptance tests.

---

## 12.6 Estimated Adoption Cost

* **Initial implementation:** Low to Moderate
* **Operational complexity:** Moderate
* **Migration difficulty:** Low
* **Replacement difficulty:** Moderate

---

# 13. Option B — MinVer

## 13.1 Description

Derive SemVer primarily from reachable Git tags and commit distance.

---

## 13.2 Advantages

* Simple conceptual model.
* Tags are release markers.
* Minimal configuration.
* Strong SemVer orientation.
* Good MSBuild integration.
* No manual version file for ordinary releases.
* Source history is visible.

---

## 13.3 Disadvantages

* Build version depends on tag availability.
* Shallow clones and missing tags require care.
* Tags become part of ordinary build calculation.
* Re-tagging or delayed tags can affect historical interpretation.
* One explicit planned product version is less visible.
* Release preparation still needs policy.
* Monorepo package inheritance is less explicit.
* Stable and non-public build distinction requires configuration.
* Source branch independence is weaker than the selected approach.

---

## 13.4 Estimated Adoption Cost

* **Initial implementation:** Low
* **Operational complexity:** Moderate
* **Migration difficulty:** Moderate
* **Replacement difficulty:** Moderate

---

# 14. Option C — GitVersion

## 14.1 Description

Infer versions from Git history, branches, tags, merge messages and a selected development workflow.

---

## 14.2 Advantages

* Powerful.
* Supports GitHub Flow, Git Flow and mainline development.
* Many output variables.
* Strong CI integrations.
* Can infer version increments.
* Flexible branch rules.
* Well established.
* Supports many release models.

---

## 14.3 Disadvantages

* Greater configuration complexity.
* More branch semantics.
* Version meaning can depend on merge history.
* Commit-message increment markers can become release authority.
* Predictive version calculation can surprise contributors.
* More difficult to explain.
* More difficult to reproduce from partial history.
* Flexibility exceeds Opure's need.
* Branch workflow becomes coupled to versioning.
* Human product-version intent becomes less direct.

---

## 14.4 Decision Relevance

GitVersion is capable but its inference model is unnecessary for the founder-led monorepo.

---

## 14.5 Estimated Adoption Cost

* **Initial implementation:** Moderate
* **Operational complexity:** High
* **Migration difficulty:** Moderate
* **Replacement difficulty:** Moderate

---

# 15. Option D — Handwritten MSBuild Versioning

## 15.1 Description

Store an explicit version in `Version.props`.

Use CI run numbers and custom scripts to build:

* assembly version;
* file version;
* package version;
* and informational version.

---

## 15.2 Advantages

* No third-party version package.
* Complete control.
* Easy explicit release version.
* Simple initial file.
* Tool-independent version source.

---

## 15.3 Disadvantages

* Custom version logic.
* Easy precedence mistakes.
* Easy package collisions.
* Git commit discovery must be implemented.
* Dirty-tree handling must be implemented.
* assembly mapping must be implemented;
* public versus non-public logic;
* tests;
* and backtracking

all become Opure maintenance.

---

## 15.4 Estimated Adoption Cost

* **Initial implementation:** Moderate
* **Operational complexity:** Moderate to High
* **Migration difficulty:** Low
* **Replacement difficulty:** Low

---

# 16. Option E — Calendar Versioning

## 16.1 Description

Use a date-oriented version such as:

```text
2026.7.0
```

---

## 16.2 Advantages

* Obvious release date.
* Simple ordering.
* Good for regular cadence.
* No debate about semantic increment.
* Useful for continuously delivered applications.

---

## 16.3 Disadvantages

* Weak compatibility meaning.
* Public SDK consumers cannot infer breakage.
* Product release date and compatibility become conflated.
* Hotfix ordering can become awkward.
* Release timing may be irregular.
* Does not align well with NuGet library guidance.
* Major product milestones become less clear.

---

## 16.4 Decision

Rejected.

Release dates remain release metadata.

---

# 17. Option F — Independent Project Versions

## 17.1 Description

Give Desktop, Runtime, SDK, adapters and tools independent versions from the beginning.

---

## 17.2 Advantages

* Precise component evolution.
* Independent release.
* Smaller compatibility commitments.
* Public SDK can change separately.
* Avoids bumping unchanged components.

---

## 17.3 Disadvantages

* Many version sources.
* Dependency matrix.
* Desktop and Runtime support complexity.
* More release orchestration.
* More package and tag naming.
* More support confusion.
* No current independent teams.
* No current independent delivery cadence.
* Difficult founder workflow.
* Process bundles still need a product version.

---

## 17.4 Decision

Rejected through Version 1.0.

Independent versioning may be reconsidered after component release independence exists.

---

# 18. Option G — Date and Commit Only

## 18.1 Description

Identify builds only by date and Git commit.

---

## 18.2 Advantages

* Unique.
* Traceable.
* Simple.
* No version-bump discussion.
* No release-version file.

---

## 18.3 Disadvantages

* No compatibility meaning.
* Poor NuGet experience.
* Poor user communication.
* Difficult upgrade decisions.
* Difficult prerelease versus stable distinction.
* Difficult support policy.

---

## 18.4 Decision

Rejected.

Commit identity remains additional metadata.

---

# 19. Option H — Conventional-Commit Automatic Versioning

## 19.1 Description

Infer:

* `feat` as minor;
* `fix` as patch;
* `BREAKING CHANGE` as major

and publish automatically.

---

## 19.2 Advantages

* High automation.
* Consistent commit format.
* Low manual release-version work.
* Automatic changelog generation.
* Frequent releases.

---

## 19.3 Disadvantages

* Commit wording becomes release authority.
* One commit may not reflect aggregate compatibility.
* Squash or merge behaviour affects result.
* AI-generated messages could change version meaning.
* Product breakage can occur without an API signature change.
* Commit logs are not user-focused release notes.
* Automatic publication weakens human control.
* Difficult security and migration judgement.
* Inappropriate before product and public API stabilise.

---

## 19.4 Decision

Rejected.

Conventional commits may be adopted later for readability, not as sole version authority.

---

# 20. Comparison Matrix

Scores:

* **1** — poor
* **2** — weak
* **3** — acceptable
* **4** — strong
* **5** — excellent

| Criterion                     | Weight |    NBGV |  MinVer | GitVersion | Handwritten |  CalVer | Independent | Date+Commit | Auto Commits |
| ----------------------------- | -----: | ------: | ------: | ---------: | ----------: | ------: | ----------: | ----------: | -----------: |
| Explicit product intent       |      5 |       5 |       3 |          3 |           5 |       3 |           4 |           1 |            2 |
| Unique commit builds          |      5 |       5 |       5 |          5 |           3 |       4 |           3 |           5 |            5 |
| Reproducibility               |      5 |       5 |       4 |          3 |           4 |       4 |           3 |           5 |            3 |
| Branch independence           |      4 |       5 |       4 |          2 |           5 |       5 |           5 |           5 |            2 |
| Tag independence              |      4 |       5 |       1 |          3 |           5 |       5 |           5 |           5 |            3 |
| SemVer support                |      5 |       5 |       5 |          5 |           4 |       1 |           5 |           1 |            5 |
| NuGet fit                     |      4 |       5 |       5 |          5 |           4 |       2 |           5 |           2 |            5 |
| Monorepo fit                  |      5 |       5 |       4 |          4 |           5 |       4 |           2 |           4 |            4 |
| Human control                 |      5 |       5 |       4 |          3 |           5 |       4 |           4 |           5 |            1 |
| Small-team fit                |      5 |       4 |       5 |          2 |           3 |       5 |           1 |           5 |            3 |
| Tool complexity               |      4 |       4 |       5 |          2 |           3 |       5 |           2 |           5 |            3 |
| Source traceability           |      5 |       5 |       5 |          5 |           4 |       4 |           4 |           5 |            5 |
| Release clarity               |      5 |       5 |       5 |          4 |           5 |       3 |           2 |           1 |            4 |
| Future extraction             |      3 |       4 |       4 |          4 |           4 |       4 |           5 |           3 |            4 |
| **Indicative weighted total** |        | **344** | **287** |    **250** |     **295** | **246** |     **235** |     **245** |      **239** |

Nerdbank.GitVersioning provides the strongest overall fit.

---

# 21. Decision

Opure will provisionally use:

> **Semantic Versioning 2.0.0 with one root `version.json`, Nerdbank.GitVersioning 3.10.70, a unified product release train, explicit prerelease labels, unique commit-derived non-release versions and immutable GitHub releases.**

Version significance is chosen by human review.

Nerdbank.GitVersioning calculates and stamps the version consistently.

This decision does not approve:

* automatic version increments from commits;
* automatic stable release publication;
* independent component versions through Version 1.0;
* mutable release tags;
* overwriting a published NuGet package;
* replacing release assets;
* relabelling RC binaries as stable;
* date-based product versions;
* or treating product version as protocol compatibility.

This decision is:

* [ ] Permanent until superseded
* [x] Provisional pending prototype evidence
* [ ] Experimental only
* [ ] Limited to prerelease
* [ ] Limited to GitHub permanently

---

# 22. Semantic Versioning Policy

The public product version is:

```text
MAJOR.MINOR.PATCH[-PRERELEASE]
```

Examples:

```text
0.1.0-preview.1
0.1.0-beta.1
0.1.0-rc.1
0.1.0
1.0.0
1.1.0
1.1.1
2.0.0
```

---

## 22.1 Major

Increment `MAJOR` when a stable public compatibility promise is broken.

Examples after 1.0 may include:

* incompatible Plugin SDK API;
* incompatible CLI contract;
* incompatible documented configuration without migration;
* removal of a stable integration capability;
* incompatible public export format;
* or a user workflow change that requires deliberate migration and cannot remain backward compatible.

---

## 22.2 Minor

Increment `MINOR` for backward-compatible functionality.

Examples:

* new workflow capability;
* new supported provider;
* new optional SDK API;
* new UI capability;
* new export option;
* or compatible protocol extension.

---

## 22.3 Patch

Increment `PATCH` for backward-compatible fixes.

Examples:

* security correction;
* bug fix;
* reliability improvement;
* performance improvement without compatibility change;
* documentation correction distributed with binaries;
* or compatibility restoration.

---

## 22.4 Public API

SemVer requires a declared public API.

Opure's public compatibility surface includes:

* published Plugin SDK;
* documented command-line interface;
* documented configuration;
* public automation contracts;
* supported plugin manifest schema;
* supported project export formats;
* supported diagnostic bundle formats where promised;
* documented extension points;
* and user data migration guarantees.

---

## 22.5 Product Behaviour

The Desktop application is not merely a library.

A product version also communicates compatibility of:

* stored user state;
* project data;
* workflows;
* and supported upgrade path.

A change can require a major version even when no C# API signature changes.

---

# 23. Pre-1.0 Policy

Versions with major zero indicate that the public compatibility surface is still evolving.

---

## 23.1 Initial Development

The initial development line begins conceptually at:

```text
0.1.0-preview.0
```

The exact first committed version belongs to the Phase 1 implementation.

---

## 23.2 Breaking Changes Before 1.0

Before 1.0, a breaking public change should normally increment the minor version.

Example:

```text
0.3.4 → 0.4.0
```

---

## 23.3 Patch Before 1.0

A backward-compatible fix may increment patch.

---

## 23.4 No Excuse for Surprise

Major zero does not permit undocumented breakage.

Breaking changes require:

* changelog entry;
* migration guidance;
* compatibility note;
* and release evidence.

---

# 24. Version 1.0 Meaning

Version 1.0 means:

* the public compatibility surface is declared;
* the Plugin SDK support level is stated;
* upgrade and migration behaviour is documented;
* recovery behaviour is tested;
* release infrastructure is accepted;
* and stable support expectations are published.

Version 1.0 does not mean:

* no defects;
* every planned feature;
* every operating system;
* or permanent API immutability.

---

# 25. Version Source

The authoritative file is:

```text
C:\Opure\version.json
```

It must use lower-case filename exactly:

```text
version.json
```

---

## 25.1 Ownership

`version.json` is a release-engineering source file.

Changes require:

* intended next release;
* compatibility reason;
* changelog state;
* and review.

---

## 25.2 Root Inheritance

All first-party projects inherit the root version.

Nested `version.json` files are prohibited initially.

---

## 25.3 Future Independent Version

A nested version file requires:

* separately distributed component;
* separate compatibility surface;
* separate owner;
* separate release cadence;
* and a new ADR.

---

# 26. Initial Nerdbank Configuration

A conceptual initial file is:

```json
{
  "$schema": "https://raw.githubusercontent.com/dotnet/Nerdbank.GitVersioning/main/src/NerdBank.GitVersioning/version.schema.json",
  "version": "0.1.0-preview.0",
  "assemblyVersion": {
    "precision": "minor"
  },
  "nugetPackageVersion": {
    "semVer": 2
  },
  "gitCommitIdShortFixedLength": 12,
  "publicReleaseRefSpec": [
    "^refs/tags/v\\d+\\.\\d+\\.\\d+(?:-(?:preview|beta|rc)\\.\\d+)?$"
  ],
  "cloudBuild": {
    "setVersionVariables": true,
    "buildNumber": {
      "enabled": false
    }
  }
}
```

The exact schema and resulting versions must be verified against Nerdbank.GitVersioning 3.10.70 before acceptance.

---

## 26.1 Tag-Only Public Releases

Only a matching release tag is automatically classified as public.

`main` is not classified as a public-release ref.

This prevents ordinary `main` builds from silently producing friendly public package versions.

---

## 26.2 Trusted Override

The trusted release workflow may set:

```text
PublicRelease=true
```

only while validating the exact tagged or soon-to-be-tagged release commit.

The workflow must validate the intended tag and version.

---

## 26.3 No Branch-Derived Label

Feature branch names are not inserted into package versions.

Commit identity provides uniqueness.

---

# 27. Tool Pinning

Pin:

```text
Nerdbank.GitVersioning 3.10.70
```

in `Directory.Packages.props`.

Pin the matching `nbgv` CLI version in:

```text
.config/dotnet-tools.json
```

---

## 27.1 Version Match

The MSBuild package and local CLI should use the same release.

A mismatch fails repository validation.

---

## 27.2 Upgrade

A tool upgrade requires:

* release-note review;
* licence and package review;
* version-output comparison;
* clean-clone build;
* assembly and package inspection;
* and release simulation.

---

# 28. Build Identity Classes

Every build belongs to one class:

* Local Dirty;
* Local Clean;
* Pull Request;
* Main Integration;
* Nightly;
* Public Preview;
* Public Beta;
* Public Release Candidate;
* Stable Candidate;
* Stable Release;
* Hotfix Candidate;
* or Withdrawn Release.

---

# 29. Non-Release Builds

Every non-release build must include source identity.

---

## 29.1 Unique Version

Two distinct commits must not produce an indistinguishable package or informational version.

---

## 29.2 Commit Identity

The informational version should include:

* SemVer basis;
* full commit SHA or sufficient fixed abbreviation;
* and dirty state where available.

---

## 29.3 Dirty Build

A local dirty build must display that it is dirty.

It cannot be a release candidate.

---

## 29.4 Pull Request Build

A pull-request build is non-public and must not produce a package version that can collide with a public release.

---

## 29.5 Main Build

A main-branch integration build remains non-public until deliberately released.

---

# 30. Public Release Builds

A public release build must:

* be clean;
* have a complete source commit;
* have exact intended SemVer;
* match a release tag pattern;
* pass all gates;
* and produce release evidence.

---

## 30.1 No Commit Suffix

A public release's user-facing version should not include the non-public commit suffix.

The release manifest still records the full commit.

---

## 30.2 Prerelease Identity

A public prerelease keeps its SemVer prerelease label.

---

# 31. Prerelease Channels

Opure uses three public prerelease channels.

---

## 31.1 Preview

Format:

```text
X.Y.Z-preview.N
```

Purpose:

* incomplete implementation;
* architecture testing;
* early technical feedback;
* unstable workflows;
* and frequent change.

Compatibility may change.

---

## 31.2 Beta

Format:

```text
X.Y.Z-beta.N
```

Purpose:

* broad feature set present;
* migration path emerging;
* wider testing;
* and known defects or incomplete polish.

Breaking changes remain possible but should be exceptional and explicit.

---

## 31.3 Release Candidate

Format:

```text
X.Y.Z-rc.N
```

Purpose:

* intended final functionality;
* release configuration;
* migration testing;
* packaging testing;
* and final defect correction.

Only release-blocking fixes should normally enter after RC.

---

## 31.4 Stable

Format:

```text
X.Y.Z
```

No prerelease suffix.

---

## 31.5 No Alpha Label

Opure uses `preview` instead of `alpha`.

This keeps public channel terminology aligned with product communication.

---

# 32. Prerelease Numbering

Prerelease numbers begin at `1`.

Examples:

```text
0.2.0-preview.1
0.2.0-preview.2
0.2.0-beta.1
0.2.0-rc.1
0.2.0
```

---

## 32.1 Explicit Number

A public prerelease number is explicit in `version.json`.

It is not inferred from CI run number.

---

## 32.2 No Reuse

A published prerelease version is never reused.

---

## 32.3 Channel Transition

Moving:

```text
preview → beta → rc → stable
```

changes the public version and requires a new build and full appropriate test suite.

---

# 33. Build Metadata

SemVer build metadata may be used in informational versions.

Example conceptual form:

```text
1.2.0-preview.3+g0123456789ab
```

---

## 33.1 NuGet Limitation

NuGet normalises and ignores build metadata for package-version identity and precedence.

Build metadata must not be the only uniqueness mechanism for published NuGet packages.

---

## 33.2 Tags

Release tags do not include `+build.metadata`.

---

## 33.3 File Names

Release asset filenames use the public SemVer without build metadata.

The release manifest provides commit and build identity.

---

# 34. Product Version Domain

The following use one product version through Version 1.0:

* Desktop;
* Runtime;
* Worker;
* Plugin Host;
* Recovery host;
* bundled CLI;
* first-party provider adapters;
* first-party MCP gateway;
* first-party Plugin SDK packages;
* and bundled sample compatibility metadata.

---

## 34.1 Why Unified

Benefits include:

* simple support statement;
* one release tag;
* one release note;
* one evidence bundle;
* one installer version;
* and no bundled-component compatibility matrix.

---

## 34.2 Unchanged Component

An unchanged bundled component still receives the product release version.

Its binary hash may remain identical only if build reproducibility and stamping permit it.

No claim is made that every component changed.

---

# 35. Independent Technical Versions

The following do not copy the product SemVer.

---

## 35.1 IPC Protocol

Use:

* protocol major;
* protocol minor;
* feature capabilities;
* and message schema compatibility.

Product version is diagnostic only.

---

## 35.2 Database Schema

Each service owns monotonically increasing schema or migration identifiers.

---

## 35.3 Vault Format

Vault record and keyring formats use explicit format versions.

---

## 35.4 Workflow Checkpoint

Checkpoint format uses service-owned compatibility version.

---

## 35.5 Plugin Manifest

Plugin manifest schema version remains explicit.

---

## 35.6 Diagnostic Export

Diagnostic bundle format has its own format version.

---

## 35.7 Why Separate

A product patch release may include a schema migration.

A product major release may retain the same protocol.

The values express different truths.

---

# 36. Assembly Version Mapping

.NET assemblies require numeric versions.

The initial policy is:

```text
AssemblyVersion = MAJOR.MINOR.0.0
```

for first-party assemblies.

---

## 36.1 Purpose

This mapping:

* changes at product minor boundaries;
* avoids patch-level assembly-identity churn;
* remains understandable;
* and works before and after 1.0.

---

## 36.2 Public SDK

The public SDK initially uses the same mapping.

Before package publication, compatibility tests must verify whether:

```text
MAJOR.MINOR.0.0
```

or:

```text
MAJOR.0.0.0
```

provides the better consumer experience.

A change requires an ADR amendment before 1.0 package publication.

---

## 36.3 Strong Naming

Strong naming is not selected by this ADR.

Assembly-version significance is greater if strong naming is introduced.

---

# 37. File Version Mapping

Windows file version requires four numeric components.

The versioning tool should produce a deterministic value based on:

```text
MAJOR.MINOR.PATCH.BUILD
```

---

## 37.1 Build Component

The fourth component must:

* fit Windows numeric limits;
* be deterministic;
* distinguish relevant builds where practical;
* and never be relied on as the public SemVer.

Nerdbank.GitVersioning's resulting file-version policy must be verified in the prototype.

---

## 37.2 Public Release

A public release's Windows Product Version should display the full SemVer through informational metadata.

File Version may remain numeric.

---

# 38. Informational Version

`AssemblyInformationalVersion` should contain the most complete human and diagnostic version.

---

## 38.1 Stable Release

Conceptual:

```text
1.0.0
```

The commit remains in the build manifest and may remain appended if the selected tool does so without confusing public display.

---

## 38.2 Prerelease

Conceptual:

```text
1.1.0-rc.2
```

---

## 38.3 Non-Release

Conceptual:

```text
1.1.0-preview.0-g0123456789ab
```

or the exact SemVer-compatible NBGV output.

---

## 38.4 Dirty Build

Conceptual:

```text
1.1.0-preview.0-g0123456789ab-dirty
```

The exact token must be validated.

---

# 39. NuGet Package Version

Public packages use exact SemVer 2.0.0.

Examples:

```text
1.0.0
1.1.0-preview.1
1.1.0-beta.1
1.1.0-rc.1
```

---

## 39.1 No Four-Part Public Package Version

Do not publish product packages as:

```text
1.0.0.123
```

unless a future compatibility requirement proves it necessary.

---

## 39.2 Build Metadata

Do not rely on `+commit` metadata to distinguish published packages.

---

## 39.3 Package Identity

One package ID and version identify one immutable package.

---

# 40. UI Version Display

The About and Diagnostics views should show:

* product version;
* release channel;
* commit SHA;
* build source;
* Runtime version;
* Desktop version;
* protocol compatibility;
* build date if included;
* and dirty state.

---

## 40.1 Normal About View

Stable public display:

```text
Opure 1.0.0
```

Prerelease:

```text
Opure 1.1.0-rc.2
```

---

## 40.2 Diagnostic Details

Diagnostic expansion may show:

```text
Commit: 0123456789abcdef...
Runtime: 1.1.0-rc.2
Desktop: 1.1.0-rc.2
IPC: 1.0
Build: GitHub Actions run ...
```

---

## 40.3 Version Mismatch

If Desktop and Runtime product versions differ, show the mismatch.

Compatibility is decided by protocol negotiation.

---

# 41. Version Validation Tool

Repository validation should verify:

* `version.json` schema;
* approved prerelease labels;
* numeric prerelease suffix;
* package/tool version match;
* tag pattern;
* assembly mapping;
* solution-wide inheritance;
* and no local version override.

---

## 41.1 No Project Override

Ordinary project files may not set:

* `Version`;
* `VersionPrefix`;
* `VersionSuffix`;
* `PackageVersion`;
* `AssemblyVersion`;
* `FileVersion`;
* or `InformationalVersion`

without an approved exception.

---

# 42. Release Tag Format

Release tags use:

```text
vMAJOR.MINOR.PATCH
vMAJOR.MINOR.PATCH-preview.N
vMAJOR.MINOR.PATCH-beta.N
vMAJOR.MINOR.PATCH-rc.N
```

Examples:

```text
v0.1.0-preview.1
v0.1.0-beta.1
v0.1.0-rc.1
v0.1.0
v1.0.0
```

---

## 42.1 Lower Case

Prerelease labels are lower case.

---

## 42.2 No Leading Zero

Numeric identifiers have no leading zero.

---

## 42.3 No Build Metadata

Tag names do not contain `+`.

---

## 42.4 Exact Match

The tag version must equal:

* computed product version;
* package version;
* release title version;
* and manifest version.

---

# 43. Tag Type

Release tags are annotated.

Lightweight release tags are prohibited.

---

## 43.1 Annotation

The tag message should contain:

* product;
* version;
* release channel;
* source commit;
* and release evidence reference.

---

## 43.2 Signature

Public release tags should be cryptographically signed.

Git supports:

* OpenPGP;
* SSH;
* and X.509 signing.

The selected signing backend belongs to the Signing ADR.

---

## 43.3 Interim State

Before tag-signing infrastructure exists:

* use annotated tags;
* immutable GitHub releases;
* provenance attestation where available;
* and document the unsigned-tag limitation.

Stable Version 1.0 should not ship without an accepted signing decision unless the founder explicitly accepts the risk.

---

# 44. Tag Immutability

Release tags are never force moved.

---

## 44.1 Wrong Tag

If a tag points to the wrong commit and has not been distributed:

* delete it only under a recorded pre-publication recovery procedure;
* fix the candidate;
* and create the correct tag.

---

## 44.2 Published Tag

If associated with a published release, the tag is never reused.

Publish a new version.

---

## 44.3 Platform Policy

Enable GitHub immutable releases before public releases.

---

# 45. Branch Strategy

The normal development branch is:

```text
main
```

Feature branches are short lived.

---

## 45.1 Main

`main` is the integration source.

It is not automatically a public release ref.

---

## 45.2 Release Branch

A temporary release branch may use:

```text
release/MAJOR.MINOR
```

Examples:

```text
release/0.8
release/1.0
```

---

## 45.3 When to Create

Create a release branch only when:

* stabilisation must continue while main advances;
* a parallel patch line must be maintained;
* or release-candidate fixes require isolation.

---

## 45.4 No Standing Branch Ceremony

Before parallel development is necessary, release directly from an exact trusted main commit.

---

## 45.5 Support Branch

A longer-lived servicing branch may use:

```text
support/MAJOR.MINOR
```

only after a support policy requires it.

---

# 46. Version Preparation

Preparing a version requires:

1. choose intended SemVer;
2. review compatibility;
3. update `version.json`;
4. update `CHANGELOG.md`;
5. update migration notes;
6. update support notes;
7. run version validation;
8. commit;
9. and run full candidate build.

---

## 46.1 Human Decision

The version choice should be documented in the pull request or release record.

---

## 46.2 AI Assistance

AI may summarise changes.

It cannot decide the release version or compatibility promise.

---

# 47. Nerdbank Release Helper

The `nbgv prepare-release` command may be used only when its branch and version changes match the approved release model.

---

## 47.1 No Blind Use

Before using it:

* review resulting branch;
* review both `version.json` changes;
* review next-development version;
* and ensure no uncommitted changes.

---

## 47.2 Initial Preference

For the first few releases, explicit reviewed version-file changes are preferred over automated branch preparation.

---

# 48. Changelog

The repository should contain:

```text
CHANGELOG.md
```

---

## 48.1 Format

Use a Keep a Changelog-inspired structure:

```markdown
# Changelog

## [Unreleased]

### Added

### Changed

### Deprecated

### Removed

### Fixed

### Security
```

Empty headings may be omitted.

---

## 48.2 Date

Released sections use ISO date:

```text
YYYY-MM-DD
```

---

## 48.3 Audience

The changelog is written for developers and users.

It is not a commit history.

---

## 48.4 Notable Changes

Include:

* user-visible capability;
* public API change;
* migration;
* security correction;
* compatibility change;
* recovery change;
* performance change with impact;
* and removed or deprecated behaviour.

---

## 48.5 Exclusions

Do not include every:

* refactor;
* test rename;
* formatting change;
* dependency patch;
* or internal implementation detail

unless it materially affects developers.

---

# 49. Unreleased Section

Every notable merged change should update `Unreleased` when practical.

---

## 49.1 Merge Gate

A release-affecting change without a changelog entry requires:

* explicit `No changelog required` rationale;
* or release-owner approval.

---

## 49.2 Changelog Conflict

If direct editing becomes a merge-conflict burden, adopt changelog fragments through a later ADR amendment.

---

# 50. Release Notes

Release notes are derived from:

* released changelog section;
* release evidence;
* migration notes;
* known issues;
* support statement;
* and download verification instructions.

---

## 50.1 Not Raw Generated Notes

GitHub-generated release notes may help identify contributors and pull requests.

They are not the sole release notes.

---

## 50.2 Required Sections

A public release note should include:

* summary;
* audience;
* release channel;
* highlights;
* breaking changes;
* migration;
* security;
* known issues;
* system requirements;
* download assets;
* hashes and verification;
* support status;
* and previous-version comparison.

---

## 50.3 Prerelease Warning

Preview, beta and RC notes state:

* instability level;
* data backup guidance;
* support expectation;
* and whether downgrade is supported.

---

# 51. Release Records

Each public release should have a repository record under:

```text
docs/releases/<version>.md
```

or a release evidence index under a durable release store.

---

## 51.1 Purpose

The record preserves:

* version decision;
* source commit;
* release channel;
* approvals;
* evidence;
* known exceptions;
* and withdrawal state.

---

# 52. Release Channels

Product distribution channels are:

* Development;
* Preview;
* Beta;
* Release Candidate;
* Stable.

---

## 52.1 Development

Not a public release channel.

Builds may be shared privately for diagnostics.

---

## 52.2 Preview

For early adopters and architecture validation.

---

## 52.3 Beta

For broader product testing.

---

## 52.4 Release Candidate

For final release validation.

---

## 52.5 Stable

For supported general use.

---

# 53. Channel and Version Agreement

A channel must match the SemVer label.

Examples:

* Preview channel requires `-preview.N`.
* Beta channel requires `-beta.N`.
* RC channel requires `-rc.N`.
* Stable channel has no suffix.

---

# 54. Latest Release

The stable `latest` designation must not point to a prerelease.

GitHub can classify prereleases separately.

---

## 54.1 Mutable Latest Alias

A mutable `latest` web link may be offered for convenience.

It is not an artefact identity.

---

## 54.2 Asset Identity

Every asset filename includes the exact version.

---

# 55. Asset Naming

Conceptual release assets:

```text
Opure-1.0.0-win-x64.zip
Opure-1.0.0-symbols.zip
Opure-1.0.0-sbom.spdx.json
Opure-1.0.0-checksums.txt
Opure-1.0.0-release-evidence.zip
```

Installer names are deferred.

---

## 55.1 No Sole Generic Filename

Do not publish only:

```text
Opure-latest.zip
```

---

# 56. Release Candidate Build

A release-candidate version is a real immutable prerelease.

---

## 56.1 Not a Labelled Development Build

An RC has:

* explicit `rc.N`;
* exact source;
* full release configuration;
* evidence;
* and release notes.

---

## 56.2 RC Promotion

An `rc.N` binary is never renamed to stable.

Stable `X.Y.Z` requires its own version-stamped build and complete stable-candidate validation.

---

# 57. Stable Candidate

A stable candidate is a private build stamped with the intended stable version.

It is not yet published.

---

## 57.1 Multiple Failed Candidates

Multiple unpublished stable candidates may exist internally for one intended SemVer.

They must be isolated by:

* source commit;
* CI run;
* candidate identifier;
* and artefact hash.

Only one may be promoted.

---

## 57.2 Package Feed

Do not publish a stable candidate to a public or shared immutable package feed before final acceptance.

---

## 57.3 Escaped Candidate

If an exact stable candidate version is distributed externally or uploaded to an immutable package feed, the version is consumed.

Use the next patch version.

---

# 58. Build Once and Promote

For one public version:

1. build candidate once;
2. test exact candidate;
3. verify exact candidate;
4. attach exact candidate to draft release;
5. publish exact candidate.

---

## 58.1 No Rebuild

Signing and publication consume the candidate bytes.

They do not compile source.

---

## 58.2 Signed Derivative

Code signing may produce signed derivatives.

The unsigned payload hash and signed payload hash are both recorded.

---

## 58.3 Archive Creation

The final distributable archive should be created before final candidate tests where possible.

If signing requires later archive assembly, the signing workflow must verify every input and test the final signed package separately.

---

# 59. Release Flow

A conceptual release flow is:

```text
Select version
    ↓
Update version.json and changelog
    ↓
Merge trusted release change
    ↓
Build private candidate
    ↓
Run release gates
    ↓
Verify candidate
    ↓
Create annotated tag on exact commit
    ↓
Create draft GitHub release
    ↓
Attach exact assets and evidence
    ↓
Verify hashes and tag
    ↓
Publish immutable release
    ↓
Verify published assets
    ↓
Advance version.json to next development version
```

---

## 59.1 Tag Timing

The tag should be created only after the exact source commit has passed the required candidate gates.

---

## 59.2 Draft Release

Create a draft, attach all assets, then publish.

This avoids needing to mutate an immutable published release.

---

## 59.3 Post-Publish Verification

Verify:

* release is immutable;
* tag points to expected commit;
* every asset matches local hash;
* provenance is available where supported;
* and release notes show the correct version.

---

# 60. GitHub Immutable Releases

Before public distribution, enable release immutability at repository or organisation level.

---

## 60.1 Protected Elements

Immutable release policy protects:

* associated tag movement;
* tag deletion while release exists;
* release asset mutation;
* and release asset deletion.

---

## 60.2 Editable Metadata

GitHub may permit title and release-note edits.

Material compatibility or security corrections should prefer:

* an explicit note;
* advisory;
* or new release

rather than silently changing history.

---

# 61. Release Attestation

GitHub immutable releases may generate a release attestation.

This complements:

* build provenance;
* SHA-256 manifest;
* code signature;
* and release evidence.

It does not replace them.

---

# 62. Signed Tags

When signing is available, use:

```text
git tag -s vX.Y.Z
```

or the approved SSH/X.509 equivalent.

---

## 62.1 Verification

Release procedure verifies:

```text
git tag -v <tag>
```

or the relevant signing backend.

---

## 62.2 Key Identity

The signer identity and key lifecycle belong to the Signing ADR.

---

# 63. Stable Release Gate

A stable release requires:

* version match;
* clean source;
* immutable dependencies;
* complete tests;
* security review;
* migration tests;
* recovery tests;
* packaging tests;
* accessibility evidence;
* release notes;
* SBOM;
* licence inventory;
* checksums;
* evidence bundle;
* and founder approval.

---

# 64. Public Prerelease Gate

A public prerelease requires:

* version match;
* clean source;
* required test subset;
* known-issue list;
* data backup warning;
* security smoke;
* checksums;
* evidence;
* and founder approval.

The depth increases from preview to RC.

---

# 65. Release Approval

The founder is the initial release authority.

---

## 65.1 Approval Record

Record:

* version;
* commit;
* channel;
* evidence result;
* known exceptions;
* approval time;
* and publication decision.

---

## 65.2 Future Independent Review

When another maintainer exists:

* security-sensitive releases;
* stable releases;
* SDK breaking changes;
* and signing changes

should require independent review.

---

# 66. Release Freeze

A release freeze begins when a version enters RC or stable-candidate state.

---

## 66.1 Allowed Changes

Allowed:

* release-blocking defect fix;
* security fix;
* release documentation;
* packaging correction;
* and test correction that does not hide product failure.

---

## 66.2 Prohibited Changes

Normally prohibited:

* new feature;
* broad refactor;
* dependency upgrade without need;
* public API expansion;
* and unrelated cleanup.

---

# 67. Release Branch Changes

Fixes on a release branch must be:

* minimal;
* tested;
* documented;
* and merged or reapplied to main.

---

## 67.1 No Divergent Fix

A release-only fix requires explicit reason.

---

# 68. Hotfixes

A hotfix is a patch release from the relevant stable line.

---

## 68.1 Flow

1. branch from the immutable stable tag;
2. create or use `support/MAJOR.MINOR`;
3. update to `MAJOR.MINOR.PATCH+1`;
4. apply focused fix;
5. update changelog;
6. run release gates;
7. publish immutable patch;
8. merge fix into main;
9. and update support notes.

---

## 68.2 Security Hotfix

A security hotfix may use a private security branch and coordinated advisory.

---

## 68.3 No Hidden Replacement

Do not replace the vulnerable release asset.

Publish a new patch.

---

# 69. Version Withdrawal

A release may be marked:

* Withdrawn;
* Deprecated;
* Superseded;
* or Security Affected.

---

## 69.1 Do Not Reuse

The version remains consumed.

---

## 69.2 GitHub Release

Prefer retaining the immutable release with a prominent warning where platform policy allows.

---

## 69.3 Assets

Do not silently replace assets.

---

## 69.4 Fixed Version

Publish the replacement version.

---

# 70. NuGet Package Withdrawal

nuget.org package versions are not ordinarily permanently deleted.

---

## 70.1 Unlist

Unlist a package to reduce new discovery.

Exact restore remains possible.

---

## 70.2 Deprecate

Deprecate with:

* reason;
* affected status;
* and recommended replacement version.

---

## 70.3 Security Package

For a vulnerable package:

* deprecate;
* unlist where appropriate;
* publish patched version;
* and issue advisory.

---

# 71. Yanking

The changelog may mark a release:

```text
[YANKED]
```

when it should not be adopted.

The exact release and package remain identifiable.

---

# 72. Release Notes Correction

A typo may be corrected in release notes when immutability permits metadata edits.

A compatibility, security or upgrade correction should include:

* timestamped correction;
* reason;
* and affected version.

Do not rewrite history invisibly.

---

# 73. Changelog Release Operation

At release:

1. rename `Unreleased` entries into `[X.Y.Z] - YYYY-MM-DD`;
2. create a new empty `Unreleased`;
3. verify comparison links;
4. verify migration references;
5. and commit before candidate build.

---

## 73.1 No Post-Build Changelog Change

The release changelog should be part of the source commit used to build the candidate.

---

# 74. Release Notes and Changelog Difference

The changelog is the durable chronological record.

Release notes are the curated announcement for one release.

They should agree but need not be identical.

---

# 75. Breaking Change Documentation

A breaking change requires:

* `Changed` or `Removed` changelog entry;
* `BREAKING` marker;
* affected public surface;
* migration steps;
* replacement;
* and version rationale.

---

# 76. Deprecation

Deprecation should precede removal when practical.

---

## 76.1 Changelog

Use `Deprecated`.

---

## 76.2 Runtime Warning

Warnings should be:

* actionable;
* rate limited;
* and not disclose secrets.

---

## 76.3 Removal Version

State the earliest intended removal major version where possible.

---

# 77. Compatibility Policy

Product version and technical compatibility are evaluated separately.

---

## 77.1 Desktop and Runtime

A product-version mismatch may be permitted when protocol compatibility says it is safe.

---

## 77.2 Default Bundle

The installed bundle should use matching product versions.

---

## 77.3 Mixed-Version Support

Mixed-version compatibility exists only when explicitly tested.

---

## 77.4 Unsupported Mix

The UI should state:

* Desktop version;
* Runtime version;
* protocol range;
* and remediation.

---

# 78. Plugin SDK Compatibility

Through 1.0, the Plugin SDK uses the product version.

---

## 78.1 Pre-1.0

Breaking SDK changes normally increment product minor.

---

## 78.2 Post-1.0

Breaking stable SDK changes require product major while the unified train remains in effect.

---

## 78.3 Independent Future

Independent SDK versioning requires:

* external consumer evidence;
* separate package cadence;
* compatibility matrix;
* and a new ADR.

---

# 79. Plugin Package Versions

Third-party plugins own their own versions.

They declare:

* plugin version;
* required SDK range;
* required product capabilities;
* and manifest schema version.

---

# 80. Provider Adapter Versions

Bundled first-party adapters use the product version.

Externally distributed adapters may later version independently.

---

# 81. CLI Compatibility

Documented CLI commands and output formats are part of the public API when declared stable.

---

# 82. Configuration Compatibility

Supported configuration keys and semantics are public compatibility.

---

## 82.1 Unknown Fields

Forward-compatible readers should ignore unknown fields only where the schema declares that behaviour.

---

# 83. Data Migration

Every release that changes authoritative storage must declare:

* migration direction;
* backup requirement;
* rollback behaviour;
* and downgrade support.

---

## 83.1 Product Version Does Not Equal Schema

The migration system uses schema versions.

---

## 83.2 Upgrade

The release notes state whether migration is automatic.

---

## 83.3 Downgrade

Downgrade is supported only when explicitly tested.

---

## 83.4 No False Reversibility

A successful installer rollback does not prove data downgrade safety.

---

# 84. Downgrade Policy

Default policy:

* binary downgrade may be possible;
* data downgrade is not guaranteed;
* pre-migration backup is required for releases with irreversible schema changes;
* and the UI must explain this before upgrade.

---

## 84.1 Supported Downgrade

A release may claim downgrade support only with:

* exact source and target versions;
* migration test;
* backup test;
* and recovery test.

---

# 85. Upgrade Paths

Release notes should state supported direct upgrades.

Examples:

```text
0.8.x → 0.9.0
0.9.x → 1.0.0
1.0.x → 1.1.0
```

---

## 85.1 Skipped Versions

If skipped-version upgrade is supported, test it.

---

## 85.2 Minimum Supported Source

The application should detect an unsupported old state before mutation.

---

# 86. Support Policy

Before 1.0:

* only the latest public prerelease is supported;
* older prereleases may be superseded immediately;
* security fixes may require upgrade to the latest line.

---

## 86.1 After 1.0

Initial stable support policy:

* latest patch in the latest stable minor is supported;
* the previous stable minor may receive critical security or data-integrity fixes at founder discretion;
* no LTS line is promised;
* and support status is stated per release.

---

## 86.2 LTS

Long-term support requires a separate ADR.

---

# 87. Release Cadence

Releases are readiness based.

No calendar promise is created by this ADR.

---

## 87.1 Security

Critical security fixes may use an expedited patch process without skipping essential secret, integrity and release-evidence gates.

---

# 88. Version Bump Timing

The release version should be set before the candidate build.

---

## 88.1 After Stable Release

After publishing `X.Y.Z`, update main to the next intended development line.

Example:

```text
1.0.0 → 1.1.0-preview.0
```

---

## 88.2 Unknown Next Version

If the next minor or major is not known, use the most likely next development version and amend deliberately later.

Do not use an invalid placeholder.

---

# 89. Patch Release Preparation

A support branch for `1.0.x` should use an explicit patch target such as:

```text
1.0.1-preview.0
```

until an RC or stable patch candidate is prepared.

---

# 90. Version Collision Prevention

Validation must ensure:

* no published tag exists for the version;
* no published release exists;
* no package feed contains the package version;
* no release asset name collides;
* and no candidate is accidentally reused.

---

# 91. Release Burn Rule

A version is burned when any of the following occurs:

* public GitHub release published;
* package uploaded to immutable public feed;
* installer distributed externally;
* public checksum published;
* or public announcement identifies downloadable bits.

A burned version is never reused.

---

# 92. Private Candidate Rule

A private candidate version may be rebuilt before publication only when:

* previous candidate is quarantined;
* no external distribution occurred;
* candidate identifiers remain unique;
* and release evidence identifies the final chosen build.

---

# 93. Version Manifest

Every build should generate:

```text
artifacts/version/version.json
```

or another generated manifest distinct from the source file.

---

## 93.1 Generated Manifest Fields

Include:

* product version;
* simple version;
* prerelease;
* public-release state;
* assembly version;
* file version;
* informational version;
* package version;
* commit SHA;
* short commit;
* repository state;
* versioning tool version;
* source `version.json` hash;
* and build identity.

---

# 94. Runtime Version API

The Runtime should expose safe version information through the Desktop Gateway.

---

## 94.1 No Filesystem Scraping

The Desktop should not infer Runtime version by inspecting files directly.

---

# 95. Crash and Diagnostic Version

Every crash and diagnostic bundle includes:

* product version;
* informational version;
* commit;
* process class;
* protocol version;
* and schema state.

---

# 96. Trust Centre Version

Trust records may record the responsible product and service version.

They must not depend on mutable release metadata.

---

# 97. Plugin Diagnostics

Plugin diagnostics include:

* plugin version;
* SDK version;
* product version;
* and protocol version.

---

# 98. Release Evidence Version

Every evidence file should contain the exact public version and candidate identity.

---

# 99. Version in File Properties

Windows file properties should display:

* File Version;
* Product Version;
* Product Name;
* and Company or publisher identity when selected.

---

# 100. Version in Logs

Startup logs include:

* product version;
* informational version;
* commit;
* and public-release state.

---

# 101. Version in User Agent

External provider requests may include a safe Opure product version only when:

* useful;
* permitted by privacy policy;
* and not exposing a dirty commit.

The Network Gateway owns the policy.

---

# 102. Version in Telemetry

No hidden telemetry is enabled.

Optional external observability may include product version according to ADR-0006.

---

# 103. Version in Database

Databases should record:

* application version that created or migrated them;
* and independent schema version.

Product version alone does not govern migration.

---

# 104. Version in Backups

Backup manifests should include:

* product version;
* schema versions;
* Vault format;
* and source platform.

---

# 105. Version in Project Files

Opure internal product version should not be written into developer project files unless the project intentionally adopts an Opure format.

---

# 106. Version Comparison

Use:

* a SemVer-aware parser for product and package versions;
* numeric protocol comparison for protocol versions;
* and service-specific schema comparison for schemas.

Do not compare version strings lexically.

---

# 107. NuGet Version Parsing

When implementing NuGet-compatible logic in .NET, prefer `NuGet.Versioning`.

Do not write an incomplete SemVer parser for package resolution.

---

# 108. Release Version Parser

The repository validation tool should accept only the Opure subset:

```text
MAJOR.MINOR.PATCH
MAJOR.MINOR.PATCH-preview.N
MAJOR.MINOR.PATCH-beta.N
MAJOR.MINOR.PATCH-rc.N
```

---

## 108.1 Rejected Forms

Reject:

```text
1
1.0
01.0.0
1.0.0-alpha
1.0.0-rc01
1.0.0+public
1.0.0.0
v1.0.0 in version.json
```

Tags include `v`; source versions do not.

---

# 109. Release Version Change Review

Reviewers should ask:

* What is the declared public API?
* Is any supported behaviour removed?
* Is migration required?
* Is data downgrade possible?
* Is the SDK compatible?
* Is the CLI compatible?
* Does protocol compatibility change?
* Is patch really backward compatible?
* Are security fixes documented appropriately?
* Is the channel correct?

---

# 110. Version Decision Record

A release pull request should include a short version rationale.

Example:

```text
Minor: adds a backward-compatible Plugin SDK capability.
```

or:

```text
Major: removes the stable v1 plugin permission contract.
```

---

# 111. Commit Messages

Commit messages do not determine product version.

---

## 111.1 Version Commit

A version-preparation commit may use:

```text
Prepare Opure 0.5.0-rc.1
```

---

## 111.2 Post-Release Commit

Example:

```text
Begin Opure 0.6.0 preview development
```

---

# 112. Release Notes Automation

Automation may:

* collect merged pull requests;
* detect contributors;
* validate changelog links;
* assemble evidence;
* and create a draft.

Automation may not silently invent:

* breaking changes;
* migration;
* security impact;
* or support promise.

---

# 113. AI-Generated Release Notes

AI may propose release-note wording from reviewed source.

A human must verify:

* completeness;
* breaking changes;
* security;
* and migrations.

Private vulnerability details must not be sent to unapproved providers.

---

# 114. Security Releases

Security releases should follow coordinated disclosure.

---

## 114.1 Version

Use the appropriate patch, minor or major based on compatibility.

Security does not automatically mean patch if the fix breaks compatibility.

---

## 114.2 Advisory

Publish a repository security advisory where appropriate.

---

## 114.3 Changelog Detail

The changelog may use a safe summary until disclosure timing permits more detail.

---

# 115. Emergency Release

An emergency release may shorten scheduling and manual exploration.

It must not skip:

* exact version;
* locked build;
* secret scan;
* targeted security tests;
* targeted recovery tests;
* hashes;
* evidence;
* and approval.

---

# 116. Release Rollback

Rollback means distributing a previous known release or a new corrective release.

It does not mutate the withdrawn version.

---

## 116.1 Data State

Rollback guidance must account for database and Vault format compatibility.

---

# 117. Release Reproducibility

A release should be reconstructable from:

* source commit;
* `version.json`;
* package lock files;
* SDK;
* build tools;
* workflow;
* and manifest.

---

## 117.1 Reconstructable Does Not Mean Identical

Signing and archive timestamps may prevent immediate bit-for-bit identity.

Differences must be understood.

---

# 118. Release Comparison

Every release note should link or identify the previous release.

---

# 119. First Release

The first public release has no previous public comparison.

It should identify the founding scope and known limitations.

---

# 120. Changelog Comparison Links

`CHANGELOG.md` may use links such as:

```text
[Unreleased]: compare/latest...HEAD
[1.0.0]: compare/v0.9.0...v1.0.0
```

Exact hosting URLs are added when the repository location is final.

---

# 121. Release Title

Use:

```text
Opure X.Y.Z
```

or:

```text
Opure X.Y.Z — Release Candidate N
```

The tag remains exact SemVer.

---

# 122. Release Date

Release date is the UTC publication date represented as:

```text
YYYY-MM-DD
```

---

# 123. Build Date

Build date may appear in the manifest.

It is not part of SemVer precedence.

---

# 124. Support Date

Support-end dates, if introduced, are release metadata.

They are not encoded into the product version.

---

# 125. Release Assets

A draft release must contain all intended assets before immutable publication.

---

## 125.1 Asset Manifest

Every asset appears in the checksum and evidence manifest.

---

## 125.2 Missing Asset

Do not publish an immutable release with a known missing required asset.

Publish a new version if an immutable release is incomplete.

---

# 126. Asset Verification

After publication, verify:

* immutable status;
* release tag;
* release attestation;
* and each local asset match.

---

# 127. Source Archives

GitHub-generated source ZIP and tar archives are convenience assets.

They are not the canonical compiled release.

---

## 127.1 Verified Source

The canonical source identity is the release tag and commit.

---

# 128. Release Checksums

Publish SHA-256 checksums.

---

## 128.1 Format

Use a deterministic text file or JSON manifest.

---

## 128.2 Signed Checksums

A future Signing ADR may sign the checksum manifest.

---

# 129. Release SBOM

The SBOM version and hash belong in the release evidence.

---

# 130. Release Licence Inventory

The licence inventory belongs in the release evidence.

---

# 131. Release Known Issues

Known issues should include:

* severity;
* affected capability;
* workaround;
* data risk;
* and planned fix where known.

---

# 132. Release Support State

Each release is marked:

* Unsupported;
* Current Preview;
* Current Beta;
* Current RC;
* Current Stable;
* Security Maintenance;
* Superseded;
* or Withdrawn.

---

# 133. Version Discovery Command

The repository should support:

```powershell
dotnet tool run nbgv get-version
```

or:

```powershell
nbgv get-version
```

after local tool restore.

---

## 133.1 Repository Wrapper

Provide:

```powershell
pwsh .\eng\version.ps1
```

to:

* show version;
* validate state;
* and produce machine-readable output.

---

# 134. Release Preparation Command

Provide a reviewed command such as:

```powershell
pwsh .\eng\release.ps1 prepare -Version 0.5.0-rc.1
```

The command should:

* validate version;
* validate clean tree;
* update source version;
* validate changelog;
* and print next steps.

It should not publish.

---

# 135. Candidate Command

Conceptually:

```powershell
pwsh .\eng\release-candidate.ps1 -Version 0.5.0-rc.1
```

It should verify version rather than invent it.

---

# 136. Publication Command

Publication remains CI-gated and deferred to release-distribution implementation.

---

# 137. Version Tool Failure

If NBGV cannot calculate a version:

* fail build;
* show safe diagnostic;
* and do not fall back to `0.0.0`.

---

# 138. Git Availability

Release builds require Git metadata.

A source archive without `.git` may build only when an explicit version and source identity are supplied through a supported verified path.

---

## 138.1 Source Archive Build

Source-archive build support is deferred.

---

# 139. Shallow Clone

CI must fetch sufficient history for NBGV.

---

## 139.1 Validation

A release workflow should fail if version calculation reports insufficient history or uncertain height.

---

# 140. Detached HEAD

Detached release builds are permitted when the exact tag or release ref is supplied and validated.

---

# 141. Source Commit Backtracking

The release version should be traceable to source commit using NBGV tooling and the release manifest.

---

# 142. Dirty Tree

A release build fails if:

* tracked files differ;
* generated source differs;
* or untracked release-relevant files exist.

---

# 143. Submodules

First-party submodules are prohibited by ADR-0010.

Version calculation therefore uses one repository history.

---

# 144. Multiple Worktrees

Git worktrees may build the same commit.

The version remains commit-derived.

---

# 145. Rebases

A rebase changes commit IDs.

A rebuilt unpublicised candidate receives different source identity.

Published tags are not rebased.

---

# 146. Force Push

Protected release and support branches should not permit force push after public release points.

---

# 147. Main Rewrite

`main` must not be rewritten across published release ancestry.

---

# 148. Release Branch Merge

After a release, merge or reapply:

* version changes;
* changelog;
* and fixes

without creating duplicate release sections.

---

# 149. Changelog Merge Conflict

Resolve by preserving:

* every notable change;
* release dates;
* and version ordering.

Do not accept one side blindly.

---

# 150. Release Tool Security

Versioning and release tools run in full trust.

---

## 150.1 Package Review

Nerdbank.GitVersioning package and tool require:

* source;
* licence;
* vulnerability;
* transitive dependency;
* and build integration review.

---

## 150.2 Tool Output

The tool must not access network during ordinary version calculation.

Any network operation requires explicit documentation.

---

# 151. Release Credential Separation

Version calculation needs no release credential.

Tag creation, signing and publication are later privileged stages.

---

# 152. Release Environment

A protected CI environment should eventually own:

* tag creation authority;
* signing identity;
* GitHub release publication;
* and package publication.

---

# 153. Release Candidate Without Credentials

Candidate build and tests should run before privileged credentials become available.

---

# 154. Version Security Threats

Relevant threats include:

* tag movement;
* version collision;
* stale candidate promotion;
* release rebuild;
* package overwrite attempt;
* dependency substitution;
* unsigned tag impersonation;
* release-note manipulation;
* and artefact replacement.

---

# 155. Mitigations

* explicit version source;
* unique non-release versions;
* exact tag match;
* signed annotated tags;
* immutable releases;
* locked dependencies;
* build once;
* SHA-256 manifest;
* provenance;
* protected environment;
* and no version reuse.

---

# 156. Privacy Impact

Version metadata may disclose:

* commit identity;
* build provider;
* and build timing.

Public releases already disclose source tag and release date when the repository is public.

Private builds should avoid exposing branch names or private repository paths in public diagnostics.

---

# 157. Performance Impact

NBGV adds Git and MSBuild version calculation.

The overhead should be measured and expected to be small relative to build and test.

---

# 158. Reliability Impact

A central version source reduces drift.

A version-tool failure can block every build.

The tool is therefore pinned and tested.

---

# 159. Testing Strategy

ADR-0008 applies.

Versioning requires dedicated tests.

---

# 160. Unit Tests

Test:

* Opure SemVer subset parser;
* tag parser;
* channel mapping;
* compatibility classification;
* release-state machine;
* and version-burn rules.

---

# 161. Integration Tests

Test:

* NBGV package stamping;
* CLI output;
* root inheritance;
* clean clone;
* shallow clone failure;
* detached tag build;
* and dirty tree.

---

# 162. Assembly Inspection Tests

Inspect built assemblies for:

* AssemblyVersion;
* AssemblyFileVersion;
* AssemblyInformationalVersion;
* product name;
* and source commit metadata.

---

# 163. NuGet Inspection Tests

Inspect packages for:

* package version;
* dependency ranges;
* repository commit;
* symbols;
* and public API baseline.

---

# 164. Version Uniqueness Tests

Build two different commits and prove:

* informational versions differ;
* non-public package versions differ;
* and source commits are recoverable.

---

# 165. Reproducibility Tests

Build one commit twice from clean clones and prove version metadata matches.

---

# 166. Tag Validation Tests

Test:

* exact stable tag;
* exact preview tag;
* exact beta tag;
* exact RC tag;
* wrong case;
* leading zero;
* build metadata;
* mismatched version;
* lightweight tag;
* and moved tag detection.

---

# 167. Release Immutability Test

In a test repository or controlled environment:

* create draft;
* attach assets;
* publish immutable release;
* verify tag and asset lock;
* and verify local asset.

---

# 168. Candidate Promotion Test

Prove:

* candidate hash;
* downloaded candidate hash;
* published asset hash;
* and manifest hash

all agree.

---

# 169. No-Rebuild Test

Release publication job should have no source compilation step.

---

# 170. Changelog Tests

Validate:

* `Unreleased`;
* release ordering;
* date format;
* version uniqueness;
* comparison links;
* allowed categories;
* and no duplicate release section.

---

# 171. Withdrawal Test

Simulate:

* bad package;
* unlisting;
* deprecation;
* release warning;
* and corrective patch.

---

# 172. Migration Release Test

Simulate a release with schema migration and verify:

* backup;
* upgrade;
* downgrade warning;
* and release-note accuracy.

---

# 173. Mixed-Version Test

Test Desktop and Runtime combinations across declared protocol ranges.

---

# 174. Public SDK Test

Verify package and assembly version policy.

---

# 175. Prototype Plan

## 175.1 Prototype A — NBGV Integration

Add:

* package;
* local tool;
* root `version.json`;
* and build stamping.

---

## 175.2 Prototype B — Version Manifest

Generate and inspect version metadata.

---

## 175.3 Prototype C — Commit Uniqueness

Build two commits and compare.

---

## 175.4 Prototype D — Public Prerelease

Create a private test tag:

```text
v0.1.0-preview.1
```

and validate computed version.

---

## 175.5 Prototype E — Stable Candidate

Build a private `0.1.0` candidate, run gates and quarantine it without publication.

---

## 175.6 Prototype F — Immutable Release

Use a disposable repository or approved test repository to validate draft-to-immutable flow.

---

## 175.7 Prototype G — Changelog

Create and validate `CHANGELOG.md`.

---

## 175.8 Prototype H — Package Withdrawal

Publish to a disposable private feed, deprecate or unlist and verify consumer behaviour.

---

# 176. Implementation Plan

## 176.1 Initial Tasks

1. Record founder review.
2. Add NBGV 3.10.70 to central package management.
3. Add matching `nbgv` local tool.
4. Create root `version.json`.
5. Add version validation.
6. Add generated version manifest.
7. Add assembly inspection tests.
8. Add UI version display.
9. Add Runtime version endpoint.
10. Add `CHANGELOG.md`.
11. Add release-note template.
12. Add release-record template.
13. Add release script skeleton.
14. Add tag validation.
15. Add immutable-release setting before public use.
16. Add candidate promotion test.
17. Add release evidence integration.
18. Add withdrawal procedure.
19. Simulate preview release.
20. Simulate stable release.
21. Complete security review.
22. Accept, amend or reject the ADR.

---

## 176.2 Owners

| Area                     | Owner                           |
| ------------------------ | ------------------------------- |
| Product version decision | Founder                         |
| Version tool             | Release Engineering Owner       |
| MSBuild integration      | Build Engineering Owner         |
| CI release flow          | CI Owner                        |
| Public SDK semantics     | Plugin SDK Owner                |
| Protocol compatibility   | Runtime Architecture Owner      |
| Migration compatibility  | Persistence and Recovery Owners |
| Changelog                | Documentation Owner             |
| Release security         | Security Owner                  |
| Release evidence         | Release Engineering Owner       |

---

# 177. Suggested Repository Additions

```text
C:\Opure\
├── version.json
├── CHANGELOG.md
├── eng\
│   ├── version.ps1
│   ├── release.ps1
│   ├── release-candidate.ps1
│   └── common\
│       └── Versioning.psm1
├── docs\
│   └── releases\
│       ├── README.md
│       ├── RELEASE-NOTES-TEMPLATE.md
│       └── RELEASE-RECORD-TEMPLATE.md
└── tests\
    └── Release\
        └── Opure.Versioning.Tests\
```

---

# 178. Suggested `CHANGELOG.md`

```markdown
# Changelog

All notable changes to Opure will be documented in this file.

The format is based on Keep a Changelog, and Opure uses Semantic Versioning 2.0.0.

## [Unreleased]

### Added

- Initial architecture skeleton.
```

The final introductory links should pin the selected Keep a Changelog convention and SemVer specification.

---

# 179. Release Notes Template

Suggested headings:

```markdown
# Opure X.Y.Z

## Release status
## Who should install this
## Highlights
## Added
## Changed
## Fixed
## Security
## Breaking changes
## Migration
## Downgrade and rollback
## Known issues
## System requirements
## Downloads
## Verification
## Support status
```

---

# 180. Release Record Template

Suggested fields:

```text
Version
Channel
Source commit
Tag
Candidate run
Approver
Published time
Previous version
Package lock digest
Version file digest
Asset hashes
SBOM hash
Evidence bundle hash
Signing state
Attestation state
Known exceptions
Withdrawal state
```

---

# 181. Release Checklist

Before candidate:

* [ ] Version chosen.
* [ ] Compatibility reviewed.
* [ ] `version.json` updated.
* [ ] Changelog updated.
* [ ] Migration documented.
* [ ] Known issues updated.
* [ ] Dependencies reviewed.
* [ ] Clean tree.
* [ ] No version collision.

Before publication:

* [ ] Candidate gates pass.
* [ ] Exact candidate verified.
* [ ] Tag matches.
* [ ] Tag annotation exists.
* [ ] Tag signature verified or limitation recorded.
* [ ] Draft release complete.
* [ ] Assets complete.
* [ ] Checksums complete.
* [ ] SBOM complete.
* [ ] Evidence complete.
* [ ] Founder approval recorded.
* [ ] Immutability enabled.

After publication:

* [ ] Release immutable.
* [ ] Tag correct.
* [ ] Assets verify.
* [ ] Package restore verifies.
* [ ] Release notes visible.
* [ ] Support state updated.
* [ ] Next development version committed.

---

# 182. Version Examples

## 182.1 Local Development

Source:

```text
0.4.0-preview.0
```

Diagnostic build:

```text
0.4.0-preview.0-<commit-derived-suffix>
```

---

## 182.2 First Preview

```text
0.4.0-preview.1
```

Tag:

```text
v0.4.0-preview.1
```

---

## 182.3 Beta

```text
0.4.0-beta.1
```

---

## 182.4 Release Candidate

```text
0.4.0-rc.1
```

---

## 182.5 Stable

```text
0.4.0
```

---

## 182.6 Stable Patch

```text
0.4.1
```

---

## 182.7 Backward-Compatible Feature

```text
0.4.1 → 0.5.0
```

---

## 182.8 Breaking Pre-1.0 Change

```text
0.5.2 → 0.6.0
```

---

## 182.9 Breaking Stable Change

```text
1.4.3 → 2.0.0
```

---

# 183. Compatibility Examples

## 183.1 Product Patch with Schema Migration

Possible:

```text
1.2.0 → 1.2.1
```

when migration is backward-compatible to users and downgrade policy is clearly stated.

Schema version may change independently.

---

## 183.2 Product Minor with Protocol Extension

Possible:

```text
1.2.1 → 1.3.0
```

while IPC remains protocol `1.4`.

---

## 183.3 Product Major Without Protocol Major

Possible when a public SDK or user workflow breaks while process transport remains compatible.

---

## 183.4 Protocol Major Without Product Major Before 1.0

Possible only with clear product minor bump and bundle compatibility.

After 1.0, public impact determines product major.

---

# 184. Release Correction Examples

## 184.1 Bad `1.0.0`

Do not replace.

Publish:

```text
1.0.1
```

Mark `1.0.0` withdrawn or deprecated.

---

## 184.2 Bad `1.1.0-rc.1`

Publish:

```text
1.1.0-rc.2
```

---

## 184.3 Missing Stable Asset

If `1.1.0` was published immutable without required asset, do not mutate.

Publish:

```text
1.1.1
```

or a separately versioned packaging correction according to impact.

---

# 185. Release-State Machine

A version may move through:

```text
Planned
    ↓
Prepared
    ↓
Candidate Built
    ↓
Candidate Verified
    ↓
Tagged
    ↓
Draft Release
    ↓
Published
    ↓
Current / Superseded / Withdrawn
```

---

## 185.1 Invalid Transitions

Prohibited:

* Published → Candidate Built;
* Withdrawn → Reused;
* RC Asset → Stable Asset;
* Failed Candidate → Published;
* Tag Mismatch → Draft Release;
* and Dirty Build → Release Candidate.

---

# 186. Release Failure Handling

## 186.1 Before Tag

Fix and build a new private candidate.

---

## 186.2 After Tag but Before Publish

If tag has not escaped and immutability has not been published, use the controlled pre-publication recovery procedure.

Prefer a new prerelease version when uncertainty exists.

---

## 186.3 After Publish

Publish a new version.

---

# 187. Version File Merge Policy

`version.json` conflicts require release-owner review.

Do not accept a version from one branch without considering release line.

---

# 188. Support Branch Version File

A support branch owns its patch line.

Example:

```text
support/1.0 → 1.0.2-preview.0
main → 1.2.0-preview.0
```

---

# 189. Cherry-Pick Policy

A hotfix cherry-picked across lines should not copy the source branch's version-file change.

---

# 190. Release Notes Translation

Release notes are initially British English only.

Translations may be added later.

---

# 191. Time Zones

Release dates use UTC.

The UI may display local time.

---

# 192. Version in Filenames

Use ASCII SemVer characters.

Do not replace dots or hyphens unless required by a packaging format.

---

# 193. Windows Installer Version

Installer formats may impose numeric constraints.

The future packaging ADR must map product SemVer without changing the public version.

---

# 194. Update Feed Version

The updater must parse SemVer and channels.

It must not use lexical string order.

---

# 195. Channel Opt-In

A future updater should let developers choose:

* Stable;
* Release Candidate;
* Beta;
* Preview.

It must not silently move a stable user to prerelease.

---

# 196. Channel Downgrade

Moving from preview to stable may involve a numerically different version and incompatible data.

The updater must evaluate migration compatibility.

---

# 197. Package Dependency Ranges

Public SDK package dependencies should prefer the narrowest compatible SemVer range.

Exact policy requires SDK publication evidence.

---

# 198. Bundled Component Manifest

A release should include a component manifest:

```text
Component
Product version
Assembly version
File version
Informational version
Hash
Protocol versions
Schema versions
```

---

# 199. Version Observability

Metrics may group by product major/minor.

They should not create high-cardinality labels from full commit IDs in externally exported metrics.

Logs and diagnostics may include full build identity.

---

# 200. Security Advisory Version Range

Advisories should identify affected versions with exact ranges.

---

# 201. CVE and Advisory Mapping

A security fix release record should link the advisory when public.

---

# 202. Package Deprecation Message

A deprecated package should name the recommended replacement version.

---

# 203. Old Version Download

Superseded releases may remain downloadable unless withdrawn for security or legal reasons.

Support status must remain clear.

---

# 204. Version Retention

Release records and tags are permanent historical records.

Workflow candidate artefacts follow CI retention.

---

# 205. Permanent Release Evidence

Published release evidence should eventually be stored durably beyond temporary CI artefacts.

ADR-0011 defers the final store.

---

# 206. Repository Visibility Change

Before making the repository public, verify that version history, tags and prerelease records contain no sensitive information.

---

# 207. First Public Tag

The first public tag must pass:

* naming;
* annotation;
* signature or documented limitation;
* source review;
* and immutable release test.

---

# 208. Version Tool Removal

Replacing NBGV requires:

* version-output migration;
* assembly mapping;
* package collision analysis;
* tag compatibility;
* and a new ADR.

---

# 209. Historical Version Recalculation

The version shown by a historical release must remain recoverable even if the tool later changes.

Release manifests therefore store resolved versions.

---

# 210. NBGV Version Height

Version height may influence non-public uniqueness and numeric file versions.

It does not decide public release significance.

---

## 210.1 Reset

Changing the base version can reset version height.

This is expected and must be tested for package uniqueness.

---

## 210.2 Offset

`versionHeightOffset` should not be used unless migrating existing published numbering.

---

# 211. Public Release Ref Pattern

Changes to `publicReleaseRefSpec` are security-sensitive.

A broad pattern could remove commit suffixes from unintended builds.

---

# 212. PublicRelease Override

`PublicRelease=true` may appear only in:

* trusted release validation;
* exact candidate build;
* or explicit test.

Ordinary pull-request and main builds force or retain non-public state.

---

# 213. CI Build Number

GitHub run number is diagnostic.

It is not the product version.

---

# 214. Release Candidate Identifier

An internal candidate identifier may be:

```text
<SemVer> / <commit> / <workflow-run>
```

It is not a public version suffix.

---

# 215. Local Package Feed

Non-public packages may be published to an isolated development feed using their unique commit-derived versions.

They must never use a public stable version.

---

# 216. Package Cache

Unique non-public package versions prevent one branch from poisoning another branch's local package cache with the same package identity.

---

# 217. Release Source of Truth

Before publication, source of truth is:

* `version.json`;
* candidate manifest;
* and source commit.

After publication, source of truth includes:

* immutable tag;
* immutable release;
* release manifest;
* and published hashes.

---

# 218. Latest Development Version

The latest development build is not described as the latest release.

---

# 219. Documentation Version

Documentation may be:

* versioned with the product;
* latest;
* or historical.

The documentation architecture is deferred.

---

# 220. API Documentation Version

Published SDK documentation should state the package version.

---

# 221. Plugin SDK Deprecation Window

A stable SDK API should normally be deprecated for at least one minor release before removal in the next major, unless security makes continued support unsafe.

---

# 222. Support Exception

A compatibility exception requires:

* affected version;
* rationale;
* workaround;
* expiry;
* and release note.

---

# 223. Release Quality Does Not Follow Version Alone

A higher version does not prove higher quality.

Release evidence and channel describe readiness.

---

# 224. Version Honesty

A release candidate is not labelled stable to improve adoption.

A preview is not labelled beta merely because it has been available for time.

---

# 225. Release Metrics

Track:

* release count;
* candidate failure;
* time to correction;
* withdrawn releases;
* update success;
* and support adoption.

Do not use version metrics to score individual developers.

---

# 226. Release Review Questions

Before stable release:

* Does the version match compatibility impact?
* Are all shipped components aligned?
* Is the public API declared?
* Are migrations tested?
* Is downgrade explained?
* Are known security issues addressed?
* Is the changelog human-readable?
* Are assets immutable?
* Can a user verify the download?
* Can support identify the source commit?
* Is a withdrawn release procedure ready?

---

# 227. Release Training

Any future release manager should be able to perform a dry run in a disposable repository.

---

# 228. Dry Run

A dry run creates:

* candidate;
* manifest;
* evidence;
* draft notes;
* and verification report

without creating a public tag or release.

---

# 229. Release Simulation Frequency

Run a full release simulation:

* before first preview;
* before first beta;
* before first stable;
* after major CI changes;
* and after version-tool upgrades.

---

# 230. Release Documentation

Create:

```text
docs/release/versioning.md
docs/release/release-process.md
docs/release/withdrawal.md
docs/release/hotfix.md
```

Only when implementation reaches the relevant milestone.

---

# 231. Architecture Tests

Enforce:

* one root `version.json`;
* NBGV version pin;
* matching CLI tool;
* no project version override;
* approved tag regex;
* and packable-project version inheritance.

---

# 232. Release Workflow Tests

Enforce:

* exact candidate input;
* no compilation after promotion boundary;
* version match;
* and artefact hash verification.

---

# 233. Release Permission Tests

Release publication must not be possible from:

* pull request;
* fork;
* untrusted branch;
* or arbitrary commit input.

---

# 234. Release Immutability Setting

Repository validation cannot fully prove the server setting locally.

Release evidence must capture confirmation.

---

# 235. Acceptance Criteria

This ADR may move to **Accepted** when:

* [ ] Semantic Versioning 2.0.0 is documented as product policy.
* [ ] The public API and compatibility surface are declared.
* [ ] One root lower-case `version.json` exists.
* [ ] No nested version files exist.
* [ ] Nerdbank.GitVersioning 3.10.70 is pinned centrally.
* [ ] Matching `nbgv` local tool is pinned.
* [ ] Tool and package mismatch fails.
* [ ] NBGV runs on the pinned .NET SDK.
* [ ] `version.json` uses SemVer 2 package output.
* [ ] Public release refs are tag only.
* [ ] Ordinary main builds remain non-public.
* [ ] Pull-request builds remain non-public.
* [ ] Distinct commits produce distinct non-public versions.
* [ ] One commit produces the same version from two clean clones.
* [ ] Dirty local builds are marked.
* [ ] Dirty builds cannot become candidates.
* [ ] Product version stamps Desktop, Runtime, Worker and Plugin Host.
* [ ] Bundled adapters inherit the product version.
* [ ] Public SDK packages inherit the product version.
* [ ] Protocol and schema versions remain independent.
* [ ] AssemblyVersion mapping is verified.
* [ ] FileVersion mapping is verified.
* [ ] InformationalVersion contains source identity.
* [ ] NuGet package versions are SemVer 2.
* [ ] No project overrides central version properties.
* [ ] UI shows product and source version.
* [ ] Runtime exposes version through the Gateway.
* [ ] Diagnostic bundles contain exact build identity.
* [ ] `CHANGELOG.md` exists.
* [ ] `Unreleased` validation exists.
* [ ] Released sections use ISO dates.
* [ ] Version tags follow exact pattern.
* [ ] Leading-zero and invalid-label tags fail.
* [ ] Release tags are annotated.
* [ ] Tag signature is verified or limitation recorded.
* [ ] Tag version equals candidate version.
* [ ] Tag commit equals candidate commit.
* [ ] Release asset version equals candidate version.
* [ ] Asset hashes equal manifest hashes.
* [ ] Public prereleases use preview, beta or rc.
* [ ] Public prerelease numbers are never reused.
* [ ] RC binaries are never relabelled stable.
* [ ] Stable candidate uses stable version before build.
* [ ] Failed private candidates are quarantined.
* [ ] Escaped exact versions are burned.
* [ ] Release build occurs once.
* [ ] Publication performs no compilation.
* [ ] Draft release contains all required assets before publish.
* [ ] GitHub immutable releases are enabled before public use.
* [ ] Published release immutability is verified.
* [ ] Release attestation is captured where supported.
* [ ] A release evidence bundle is generated.
* [ ] SBOM and licence inventory are linked.
* [ ] Release notes include migration and downgrade.
* [ ] Support status is explicit.
* [ ] Withdrawal procedure is tested.
* [ ] NuGet unlisting and deprecation procedure is documented.
* [ ] No published version is reused.
* [ ] Hotfix flow is tested.
* [ ] Fixes return to main.
* [ ] Release simulation succeeds.
* [ ] Security review is complete.
* [ ] Founder approval is recorded.

---

# 236. Evidence Required Before Acceptance

* [ ] SemVer policy review;
* [ ] public API declaration;
* [ ] NBGV package and licence review;
* [ ] exact tool pin;
* [ ] root `version.json`;
* [ ] version-output matrix;
* [ ] two-commit uniqueness test;
* [ ] two-clone reproducibility test;
* [ ] shallow-clone test;
* [ ] dirty-tree test;
* [ ] assembly inspection report;
* [ ] Windows file-property report;
* [ ] NuGet package inspection report;
* [ ] UI version screenshot;
* [ ] Runtime version endpoint test;
* [ ] tag validation report;
* [ ] tag signature result or limitation;
* [ ] public prerelease simulation;
* [ ] stable candidate simulation;
* [ ] no-rebuild promotion test;
* [ ] immutable-release test;
* [ ] published-asset verification test;
* [ ] changelog validation report;
* [ ] migration release test;
* [ ] withdrawal test;
* [ ] package deprecation test;
* [ ] hotfix simulation;
* [ ] release evidence bundle;
* [ ] security review;
* [ ] founder approval.

---

# 237. Known Limitations

* The initial committed product version is not fixed by this ADR.
* Exact NBGV output strings require prototype confirmation.
* The AssemblyVersion policy may need adjustment before public SDK publication.
* The exact Windows FileVersion build component is not final.
* Strong naming is not selected.
* Tag-signing backend is not selected.
* Code signing is not selected.
* Installer version mapping is deferred.
* Updater channel behaviour is deferred.
* Permanent release-evidence storage is deferred.
* Public package feed is not selected.
* Release publication is not yet enabled.
* GitHub immutable releases apply only after the setting is enabled.
* GitHub plan or repository visibility may affect attestation features.
* One founder cannot provide independent release approval.
* Bit-for-bit reproducibility is not yet proven.
* A stable candidate can be rebuilt privately before publication, but each candidate must remain uniquely identified.
* Full long-term support is not promised.
* Independent SDK versioning is deferred.
* Changelog fragments are not adopted initially.
* Documentation versioning is deferred.
* Historical source-archive builds without Git metadata are deferred.

---

# 238. Open Questions

* What should the first committed `version.json` version be?
* Should initial implementation builds use `0.1.0-preview.0`?
* Does NBGV 3.10.70 produce the desired exact non-public suffix?
* Should the short commit length remain 12?
* Should public-release ref matching include release branches as well as tags?
* Should release candidates always build from tags or use a trusted override before tag creation?
* Should AssemblyVersion be `MAJOR.MINOR.0.0` or `MAJOR.0.0.0` for the public SDK?
* What deterministic fourth FileVersion component should be used?
* Should stable InformationalVersion include commit metadata?
* Should source commit also use `RepositoryCommit` package metadata?
* Should public SDK packages remain unified after 1.0?
* When should independent SDK versioning be reconsidered?
* Which Git signing backend should be used?
* Should release tags be created locally or through CI?
* How will CI create a verifiable signed tag?
* Which signing key custody model should be used?
* Will GitHub immutable releases be available on the selected repository plan?
* Should all public prereleases be immutable GitHub releases?
* Should preview releases publish NuGet packages?
* Which package feed will host prereleases?
* Should stable candidates be retained for 90 days?
* What exact event burns a version in private partner testing?
* How should internal package feeds isolate repeated stable candidates?
* Should release branches be created before RC or only for parallel work?
* How long should support branches remain?
* Should `nbgv prepare-release` be adopted after the first manual cycles?
* Should the next development version bump patch, minor or be decided explicitly each time?
* Should Keep a Changelog 2.0.0 be pinned as the changelog convention?
* Should direct changelog editing be required for every user-visible pull request?
* When should changelog fragments be adopted?
* Should GitHub-generated notes be included as a contributor appendix?
* Where should permanent release evidence be stored?
* Which SBOM format should be canonical?
* Should release evidence be signed?
* How should a published release be marked withdrawn without deleting it?
* Should old vulnerable binaries remain downloadable?
* How should security advisories link to affected version ranges?
* What is the first stable support policy?
* When should an LTS policy be introduced?
* Which mixed Desktop and Runtime versions should be supported?
* How should the updater compare prerelease channels?
* How should installer numeric versions map from SemVer?
* Which version should appear in Windows uninstall metadata?
* Should public release dates use publication time or tag date?
* Should public preview numbers reset on channel transition?
* Should beta and RC numbers always start at 1?
* Should an emergency release permit a new package dependency?
* What independent reviewer is required before Version 1.0?
* Should release simulations use a dedicated private test repository?
* How should source builds without `.git` obtain verified version metadata?

---

# 239. Deferred Decisions

This ADR intentionally defers:

* Git and binary signing to a Signing ADR;
* installer numeric version mapping to a Packaging ADR;
* update-channel selection to an Updater ADR;
* public distribution destination to a Distribution ADR;
* permanent release records to a Release Evidence ADR;
* public package publication to an SDK Publication ADR;
* long-term support to a Support Lifecycle ADR;
* independent SDK versioning to future ecosystem evidence;
* changelog fragments to future repository-scale evidence;
* documentation version hosting to a Documentation Delivery ADR;
* emergency release disclosure to a Security Response ADR;
* and source-archive reproducibility to a Reproducible Build ADR.

---

# 240. Alternatives Rejected

## 240.1 MinVer

Not selected because release tags should mark releases but should not be required to calculate every ordinary build's source identity.

---

## 240.2 GitVersion

Not selected because branch and merge-history inference creates unnecessary complexity and weakens explicit human version intent.

---

## 240.3 Handwritten Version Logic

Not selected because version uniqueness, Git identity and assembly mapping are mature concerns that should not be reimplemented without need.

---

## 240.4 Calendar Versioning

Rejected because it communicates release timing rather than compatibility significance.

---

## 240.5 Independent Component Versions

Rejected through Version 1.0 because the bundled product releases and is supported as one coordinated system.

---

## 240.6 Commit Hash as Product Version

Rejected because source identity alone does not communicate release compatibility or stability.

---

## 240.7 Conventional-Commit Automatic Versioning

Rejected because release significance and public compatibility require human judgement.

---

## 240.8 Mutable Git Tags

Rejected because published version identity must remain stable.

---

## 240.9 Replacing Published Assets

Rejected because a version identifies immutable contents.

---

## 240.10 Relabelling RC as Stable

Rejected because the stable version must be embedded before the exact binaries are tested.

---

# 241. Official and Primary Evidence References

The following primary sources informed this ADR:

## Semantic Versioning

* [Semantic Versioning 2.0.0](https://semver.org/)

## .NET and NuGet Versioning

* [Versioning and .NET libraries](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/versioning)
* [NuGet package versioning](https://learn.microsoft.com/en-us/nuget/concepts/package-versioning)
* [Assembly versioning](https://learn.microsoft.com/en-us/dotnet/standard/assembly/versioning)
* [Set assembly attributes](https://learn.microsoft.com/en-us/dotnet/standard/assembly/set-attributes)
* [Assembly manifest](https://learn.microsoft.com/en-us/dotnet/standard/assembly/manifest)
* [Deleting or unlisting NuGet packages](https://learn.microsoft.com/en-us/nuget/nuget-org/policies/deleting-packages)
* [Deprecating NuGet packages](https://learn.microsoft.com/en-us/nuget/nuget-org/deprecate-packages)
* [NuGet package publish API](https://learn.microsoft.com/en-us/nuget/api/package-publish-resource)

## Nerdbank.GitVersioning

* [Nerdbank.GitVersioning repository](https://github.com/dotnet/Nerdbank.GitVersioning)
* [Getting started](https://dotnet.github.io/Nerdbank.GitVersioning/docs/getting-started.html)
* [`version.json` reference](https://dotnet.github.io/Nerdbank.GitVersioning/docs/versionJson.html)
* [Public versus stable releases](https://dotnet.github.io/Nerdbank.GitVersioning/docs/public-vs-stable.html)
* [`nbgv` CLI](https://dotnet.github.io/Nerdbank.GitVersioning/docs/nbgv-cli.html)

## Git Tags

* [`git tag` documentation](https://git-scm.com/docs/git-tag)
* [Signing tags on GitHub](https://docs.github.com/en/authentication/managing-commit-signature-verification/signing-tags)

## GitHub Releases

* [About GitHub releases](https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases)
* [Managing releases](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository)
* [Immutable releases](https://docs.github.com/en/code-security/concepts/supply-chain-security/immutable-releases)
* [Preventing release changes](https://docs.github.com/en/code-security/how-tos/secure-your-supply-chain/establish-provenance-and-integrity/prevent-release-changes)
* [Verifying release integrity](https://docs.github.com/en/code-security/how-tos/secure-your-supply-chain/secure-your-dependencies/verify-release-integrity)

## Changelog

* [Keep a Changelog 2.0.0](https://keepachangelog.com/en/2.0.0/)

Versions, platform features and repository-plan capabilities can change.

The implementation team must verify the pinned tool version and server settings before acceptance.

---

# 242. Review Record

| Date         | Reviewer           | Decision | Notes                                                                                |
| ------------ | ------------------ | -------- | ------------------------------------------------------------------------------------ |
| 18 July 2026 | Architecture draft | Proposed | SemVer 2.0, root `version.json`, NBGV 3.10.70 and immutable release flow recommended |

---

# 243. Approval

## Founder or Product Approval

* **Name:** Christopher Dyer
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Pending founder review

## Release Engineering Approval

* **Name or role:** Release Engineering Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Version output, candidate flow and immutable release simulation required

## Build Engineering Approval

* **Name or role:** Build Engineering Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** NBGV, assembly, file and package stamping evidence required

## Security Approval

* **Name or role:** Security Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** tag integrity, version collision, release immutability and withdrawal review required

## SDK Approval

* **Name or role:** Plugin SDK Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** public API and assembly compatibility policy required

## Documentation Approval

* **Name or role:** Documentation Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** changelog and release-note process required

---

# 244. Supersession

This ADR is superseded only when a later ADR:

* names ADR-0012 explicitly;
* explains why the version format, source, tool or release model changed;
* identifies affected products, packages and tags;
* describes version migration;
* explains compatibility and support impact;
* and updates the `Superseded by` field.

Historical ADRs remain in version control.

Published versions remain historical facts.

---

# 245. Change History

| Version | Date         | Author        | Summary                                                              |
| ------- | ------------ | ------------- | -------------------------------------------------------------------- |
| 0.1     | 18 July 2026 | Founder Draft | Initial SemVer, NBGV and immutable release-management recommendation |

---

# 246. Final Decision Statement

> **Opure will provisionally use Semantic Versioning 2.0.0 with one root `version.json`, Nerdbank.GitVersioning 3.10.70 for unique and reproducible source-derived stamping, a unified product release train through Version 1.0, explicit preview, beta and release-candidate channels, human-reviewed compatibility decisions, annotated and ultimately signed exact release tags, immutable GitHub releases, a curated changelog and build-once promotion of the exact binaries that passed validation, because developers must be able to understand what a version promises, identify precisely which source produced it and trust that published contents will never be silently replaced.**

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**