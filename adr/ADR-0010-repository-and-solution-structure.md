# ADR-0010 — Repository and Solution Structure

## Architecture Decision Record

**Status:** Proposed
**Date:** 18 July 2026
**Decision owners:** Founder and Product Owner
**Technical owners:** Repository Architecture Owner
**Reviewers:** Runtime Architecture Owner, Desktop Owner, Build Engineering Owner, Test Architecture Owner, Security Owner, Plugin SDK Owner, Release Engineering Owner
**Supersedes:** None
**Superseded by:** None
**Related ADRs:** ADR-0001 Primary Implementation Language, ADR-0002 Desktop Application Framework, ADR-0003 Runtime Process Topology, ADR-0004 Local IPC, ADR-0005 Persistence, ADR-0006 Logging and Observability, ADR-0007 Secrets Vault, ADR-0008 Testing Strategy, ADR-0009 Windows Path and Filesystem Handling
**Related specifications:** CHARTER-001, SPEC-001, SPEC-002, SPEC-003, SPEC-004, SPEC-005, SPEC-006, SPEC-007, SPEC-008, SPEC-009, SPEC-010, SPEC-011, SPEC-012
**Target milestone:** Phase 0 — Founding Baseline and Phase 1 — Architecture Skeleton

---

## 1. Decision Summary

Opure should use **one modular monorepo** rooted at the existing Opure repository.

The repository should use:

* `Opure.slnx` as the single committed solution of record;
* the supported .NET 10 LTS SDK feature band selected by the implementation baseline;
* a root `global.json` to pin the SDK and Microsoft Testing Platform runner;
* root `Directory.Build.props` for common early build policy;
* root `Directory.Build.targets` for common validation and late build policy;
* root `Directory.Packages.props` for NuGet Central Package Management;
* root `NuGet.config` for approved feeds and package-source mapping;
* repository-local tools declared in `.config/dotnet-tools.json`;
* root `.editorconfig`, `.gitattributes` and `.gitignore`;
* central build outputs under an ignored `artifacts/` directory;
* first-party application code under `src/`;
* coded tests under `tests/`;
* benchmarks under `benchmarks/`;
* developer and build automation under `eng/`;
* stable product specifications under `specs/`;
* architecture decisions under `adr/`;
* supporting documents under `docs/`;
* examples under `samples/`;
* repository-owned utilities under `tools/`;
* and explicitly reviewed vendored material under `third_party/`.

The codebase should be organised by **bounded capability and executable host**, not by broad horizontal technical layers.

The principal structural rules are:

* executable hosts contain composition and process lifecycle, not domain behaviour;
* every authoritative capability has one primary implementation project;
* cross-capability calls use contracts, commands, queries or events;
* capability contracts are dependency-light and do not reference implementations;
* platform-neutral abstractions do not reference Windows implementations;
* Windows-specific code is isolated in projects ending with `.Windows` or a similarly explicit platform suffix;
* public SDK projects contain only intentionally supported contracts;
* generated code is isolated and never edited manually;
* tests mirror product boundaries;
* benchmark projects never become production dependencies;
* no project named `Opure.Common`, `Opure.Utils`, `Opure.Helpers` or an equivalent dumping ground is permitted;
* one capability should not be divided into Domain, Application, Infrastructure and Persistence projects automatically;
* project extraction occurs only when a compile-time, process, public-contract, platform, security or packaging boundary justifies it;
* and architecture tests enforce the dependency graph.

NuGet package versions should be centralised.

Per-project package version overrides should be disabled by default.

Package lock files should be committed, and continuous integration should restore in locked mode.

Build tools should be repository-local rather than assumed as mutable global machine state.

Build output must remain outside every project source directory through the .NET artifacts output layout.

---

## 2. Status

Current status:

> **Proposed**

This ADR becomes **Accepted** only after the Phase 1 skeleton demonstrates:

* `Opure.slnx`;
* SDK pinning;
* central build policy;
* central package management;
* locked restore;
* package-source mapping;
* local tool restore;
* central artifact output;
* representative host, capability, contract and platform projects;
* architecture dependency tests;
* generated-code isolation;
* test-suite discovery;
* one desktop-to-Runtime vertical slice;
* and successful clean-clone build and test instructions.

---

## 3. Context

Opure already has:

* a Charter;
* architecture specifications;
* roadmap specifications;
* root architecture documentation;
* repository documentation;
* and a growing set of architecture decisions.

The implementation will contain:

* a Desktop process;
* a Runtime process;
* trusted Worker processes;
* isolated Plugin Host processes;
* local IPC;
* service-owned persistence;
* local diagnostics;
* a Secrets Vault;
* Windows filesystem integration;
* AI provider adapters;
* MCP integration;
* workflows;
* project memory;
* patch management;
* builds;
* tests;
* repository operations;
* plugin SDK packages;
* and future platform adapters.

Without a deliberate repository structure, likely failure modes include:

* one enormous Runtime project;
* hundreds of prematurely layered projects;
* circular project references;
* service implementations calling each other directly;
* shared-state utility projects;
* platform code leaking into domain contracts;
* package versions drifting between projects;
* multiple solution files diverging;
* generated files mixed with handwritten code;
* tests coupled to implementation details;
* build outputs appearing throughout the source tree;
* and tooling that works only on one developer machine.

The repository itself must express the architecture.

A contributor should be able to infer:

* what runs as a process;
* which capability owns data;
* which contracts are public;
* which code is Windows-specific;
* which dependencies are allowed;
* and which command builds or tests the product

by inspecting the repository.

---

## 4. Problem Statement

Opure requires a repository and solution structure that supports a multi-process modular platform, preserves clear service ownership, remains practical for a small initial team, centralises build and dependency policy, supports future platforms and prevents the codebase from degenerating into either one monolith project or an unmanageable collection of ceremonial layers.

---

## 5. Decision Drivers

The structure is evaluated against:

* alignment with the Opure Charter;
* architectural clarity;
* small-team delivery;
* fast local development;
* process topology;
* service ownership;
* loose coupling;
* explicit contracts;
* public SDK stability;
* Windows-first development;
* future cross-platform support;
* package and supply-chain governance;
* deterministic builds;
* locked restore;
* testability;
* architecture enforcement;
* generated-code management;
* build performance;
* IDE usability;
* command-line usability;
* release packaging;
* contributor onboarding;
* future extraction;
* and replacement cost.

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
* **Least Privilege**
* **Project Isolation**
* **Performance Respect**
* **No Hidden Authority**
* **No Accidental Shared Ownership**
* **No Ceremonial Architecture**
* **No Tooling Snowflakes**
* **No Hidden Build State**
* **Evidence-Based Confidence**

Relevant architecture commitments include:

* hosts compose rather than own domain state;
* every authoritative resource has one owning service;
* services communicate through explicit contracts;
* third-party plugins depend on an SDK, not internal Runtime assemblies;
* platform-specific implementation stays behind abstractions;
* generated code is treated as generated;
* tests enforce the architecture;
* and a clean clone must be sufficient to restore the declared toolchain.

---

## 7. Scope

This ADR decides:

* monorepo versus multiple repositories;
* primary solution format;
* root repository layout;
* source-project organisation;
* executable host projects;
* capability projects;
* contract projects;
* public SDK projects;
* platform projects;
* adapter projects;
* test projects;
* benchmark projects;
* tooling projects;
* generated code;
* root MSBuild files;
* SDK pinning;
* central package management;
* lock files;
* local tools;
* build-output layout;
* naming;
* project-reference rules;
* package-reference rules;
* friend assemblies;
* documentation placement;
* and architecture enforcement.

This ADR does not decide:

* final continuous-integration provider;
* final release packaging technology;
* final installer layout;
* final code-signing provider;
* branch protection settings;
* contribution licensing;
* public source-code licence;
* issue-tracker workflow;
* NuGet publication credentials;
* or when any source repository becomes public.

---

## 8. Constraints

Known constraints include:

* The repository currently exists at `C:\Opure`.
* Specifications are under `specs/`.
* ADRs are under `adr/`.
* Windows 11 is the first target.
* C# and .NET are the primary implementation stack.
* Avalonia is the selected desktop framework.
* The Runtime begins as a modular trusted process.
* Some capabilities will later move to separate processes.
* Some code is necessarily Windows-specific.
* The first team is small.
* The architecture already defines many logical services.
* Creating four projects per logical service would create excessive ceremony.
* One giant implementation project would erase boundaries.
* Public plugin contracts require stronger compatibility than internal code.
* Generated gRPC code must remain separated from handwritten logic.
* Test projects require selective access to internals.
* Package restore must be repeatable.
* Build tools run in full trust.
* Build outputs and local runtime state must not pollute source directories.
* The codebase must remain usable from PowerShell and IDEs.
* Future Linux and macOS work should reuse platform-neutral projects.
* The solution must remain understandable to a founder-led team.

---

## 9. Assumptions

This decision assumes:

* .NET 10 LTS remains the implementation baseline selected by the earlier language ADR.
* The selected SDK supports `.slnx`.
* Supported Visual Studio and command-line tools can open and build `.slnx`.
* The repository initially contains only first-party product code and controlled examples.
* All .NET projects use SDK-style project files.
* NuGet `PackageReference` is used.
* Most internal implementation projects are not packed.
* Plugin SDK projects may later be packed and published.
* Each executable can reference multiple in-process capability projects.
* Compile-time dependency rules can preserve modularity inside one process.
* Architecture tests can inspect project and assembly references.
* A root package policy is preferable to local version selection.
* A local tool manifest is adequate for repository-owned command-line tools.
* Build outputs can use the .NET central `artifacts/` layout.
* Project files can remain small when common policy is centralised.
* The first implementation can tolerate loading the full solution in modern tooling.
* Solution folders are organisational only and do not define architecture.
* Directory layout and project references are the architectural source of truth.

---

## 10. Current Platform Evidence

Official Microsoft documentation available on 18 July 2026 establishes that:

* .NET 10 is an active LTS release supported until November 2028.
* Starting with .NET 10, `dotnet new sln` creates an `.slnx` solution by default.
* The .NET SDK supports creating, modifying and migrating `.slnx` solutions.
* `.slnx` is supported by major .NET tooling and is intended to be easier to maintain than the legacy `.sln` format.
* `global.json` can pin the .NET SDK, control roll-forward and select Microsoft Testing Platform as the test runner.
* `Directory.Build.props` is imported early for projects under its directory.
* `Directory.Build.targets` is imported later and can enforce common build behaviour.
* NuGet Central Package Management uses a root `Directory.Packages.props`.
* Package version overrides can be disabled.
* NuGet package lock files record the resolved dependency closure and locked mode can fail restore when declared dependencies and the lock disagree.
* .NET supports central output under an `artifacts/` directory through `UseArtifactsOutput`.
* local .NET tools can be declared in `.config/dotnet-tools.json` and restored with one command.
* `.editorconfig` can configure source style and analyzer severity throughout the repository.
* Package Source Mapping can constrain which feeds are used for package families.

These capabilities must be verified against the exact pinned SDK and supported IDE versions before the ADR moves to Accepted.

---

## 11. Options Considered

The principal repository options are:

1. **Option A — Modular Monorepo**
2. **Option B — Repository per Process**
3. **Option C — Repository per Bounded Capability**
4. **Option D — One Large Application Project**
5. **Option E — Layered Solution with Domain/Application/Infrastructure Projects for Every Capability**
6. **Option F — Source Packages as the Primary Internal Integration Mechanism**

The principal solution options are:

1. **One `Opure.slnx`**
2. **One legacy `Opure.sln`**
3. **Multiple solution files by subsystem**
4. **No solution file; project-only CLI commands**

---

# 12. Option A — Modular Monorepo

## 12.1 Description

Keep all first-party source, tests, documentation, specifications and engineering scripts in one Git repository.

Organise code into explicit projects by executable, capability, contract and platform boundary.

---

## 12.2 Advantages

* Atomic architecture changes.
* One dependency policy.
* One SDK policy.
* One test command.
* One architecture-test graph.
* Easier contract updates.
* Easier process-topology changes.
* Easier contributor onboarding.
* Easier full-product search.
* Easier release evidence.
* No first-party package publishing required for every internal change.
* Strong support for end-to-end tests.
* Strong support for refactoring.
* One source of truth for specifications and ADRs.
* Easier licence and vulnerability inventory.
* Easier generated-code management.
* Easier version alignment.
* Simple founder workflow.
* Supports future component extraction.
* Avoids cross-repository coordination overhead.

---

## 12.3 Disadvantages

* The repository can become large.
* The full solution can contain many projects.
* Uncontrolled project references can create coupling.
* Build and test selection require structure.
* Teams may touch unrelated directories.
* Release pipelines need selective packaging.
* Repository permissions cannot isolate one internal component.
* Large Git history may eventually affect clones.
* External SDK consumers require packages despite internal source references.

---

## 12.4 Risks

* Monorepo becomes monolith.
* one root build file accumulates conditional complexity;
* every project references a shared foundation;
* solution load becomes slow;
* build scripts become product-specific shell logic;
* and architecture rules remain documentation only.

---

## 12.5 Mitigations

* architecture tests;
* bounded project references;
* capability ownership;
* central but minimal build policy;
* incremental builds;
* test categories;
* one primary solution;
* no common dumping ground;
* and extraction criteria.

---

## 12.6 Estimated Adoption Cost

* **Initial implementation:** Low to Moderate
* **Operational complexity:** Low
* **Migration difficulty:** Low
* **Replacement difficulty:** Moderate

---

# 13. Option B — Repository per Process

## 13.1 Description

Use separate repositories for Desktop, Runtime, Worker, Plugin Host and related executables.

---

## 13.2 Advantages

* Strong process ownership.
* Smaller repositories.
* Independent releases.
* Independent permissions.
* Clear deployable boundaries.

---

## 13.3 Disadvantages

* Shared contracts require packages.
* Atomic changes are difficult.
* Version coordination is immediate.
* Cross-process test setup is harder.
* More CI configuration.
* More release orchestration.
* More package publishing.
* Architectural iteration slows.
* Workers and hosts may change frequently with the Runtime.
* Small-team overhead is high.
* Specifications and ADRs become fragmented.

---

## 13.4 Decision

Rejected for the first product.

Future independent repositories require organisational or release evidence.

---

# 14. Option C — Repository per Bounded Capability

## 14.1 Description

Give Projects, Patching, Workflows, AI, Trust, Plugins and other capabilities separate repositories.

---

## 14.2 Advantages

* Strong capability ownership.
* Independent versioning.
* Strict boundaries.
* Independent release possible.
* Smaller codebases.

---

## 14.3 Disadvantages

* Very high coordination overhead.
* Internal contracts become package contracts prematurely.
* Full-product refactoring becomes difficult.
* Integration tests require many repositories.
* Dependency updates multiply.
* Security and recovery changes cross many repositories.
* Unsuitable for a small initial team.
* Encourages distributed-system organisation before product evidence.

---

## 14.4 Decision

Rejected.

---

# 15. Option D — One Large Application Project

## 15.1 Description

Place most Runtime behaviour into one `Opure.Runtime` project and most Desktop behaviour into one `Opure.Desktop` project.

---

## 15.2 Advantages

* Few projects.
* Fast initial creation.
* Easy navigation at first.
* Simple build graph.
* Minimal project-file management.

---

## 15.3 Disadvantages

* Boundaries are conventions only.
* Everything can call everything.
* Platform code spreads.
* Persistence ownership weakens.
* Tests require broad internal access.
* Extraction becomes difficult.
* Provider SDKs leak.
* Plugin contracts become coupled to internals.
* Large rebuild surface.
* Circular conceptual dependencies remain hidden.
* Security review becomes harder.

---

## 15.4 Decision

Rejected.

---

# 16. Option E — Four Layers for Every Capability

## 16.1 Description

Create projects such as:

```text
Opure.Patching.Domain
Opure.Patching.Application
Opure.Patching.Infrastructure
Opure.Patching.Persistence
```

for every logical capability.

---

## 16.2 Advantages

* Familiar layered architecture.
* Strong theoretical separation.
* Easy infrastructure replacement.
* Clear project-level dependency direction.
* Small individual assemblies.

---

## 16.3 Disadvantages

* Project explosion.
* Ceremony exceeds behaviour.
* Repeated empty layers.
* Many cross-project changes.
* Slow navigation.
* Large solution.
* Internal abstractions created without alternatives.
* Small team pays enterprise-scale coordination cost.
* Behaviour becomes fragmented across folders and assemblies.
* Every logical service appears separately deployable even when it is not.

---

## 16.4 Decision

Rejected as an automatic template.

Projects are extracted only for a real boundary.

---

# 17. Option F — Internal Source Packages

## 17.1 Description

Package each internal capability as a NuGet package and integrate hosts through packages.

---

## 17.2 Advantages

* Explicit versioning.
* Strong binary boundaries.
* Independent packaging.
* Clear public surface.
* Can simulate external consumers.

---

## 17.3 Disadvantages

* Package publishing overhead.
* Debugging and source navigation friction.
* Atomic refactoring becomes harder.
* Local package feeds.
* Version churn.
* Package restore becomes required for first-party code.
* Too early for internal modules.
* Can hide dependency cycles through package versions.

---

## 17.4 Decision Relevance

Public SDK and test fixture packages may be published later.

Internal product composition uses project references.

---

# 18. Repository Comparison Matrix

Scores:

* **1** — poor
* **2** — weak
* **3** — acceptable
* **4** — strong
* **5** — excellent

| Criterion                     | Weight | Modular Monorepo | Per Process | Per Capability | Large Projects | Four Layers Each | Internal Packages |
| ----------------------------- | -----: | ---------------: | ----------: | -------------: | -------------: | ---------------: | ----------------: |
| Charter alignment             |      5 |                5 |           4 |              4 |              2 |                4 |                 3 |
| Small-team delivery           |      5 |                5 |           2 |              1 |              5 |                2 |                 2 |
| Architectural clarity         |      5 |                5 |           4 |              5 |              1 |                4 |                 4 |
| Atomic refactoring            |      5 |                5 |           2 |              1 |              5 |                5 |                 2 |
| Contract evolution            |      5 |                5 |           2 |              2 |              2 |                4 |                 3 |
| Process testing               |      4 |                5 |           3 |              2 |              3 |                4 |                 3 |
| Dependency governance         |      5 |                5 |           3 |              3 |              2 |                4 |                 4 |
| Build simplicity              |      4 |                4 |           2 |              1 |              5 |                2 |                 2 |
| Contributor onboarding        |      5 |                5 |           2 |              1 |              4 |                2 |                 2 |
| Public SDK support            |      3 |                5 |           4 |              4 |              2 |                4 |                 5 |
| Future extraction             |      4 |                4 |           5 |              5 |              1 |                4 |                 4 |
| Test maintainability          |      5 |                5 |           3 |              2 |              2 |                3 |                 3 |
| Release coordination          |      4 |                5 |           2 |              1 |              4 |                4 |                 2 |
| **Indicative weighted total** |        |          **345** |     **202** |        **178** |        **223** |          **252** |           **220** |

The modular monorepo provides the strongest overall fit.

---

# 19. Solution Format Options

## 19.1 One `Opure.slnx`

Use the modern XML solution format as the single solution of record.

### Advantages

* Human-readable.
* Easier merge review.
* Current .NET default.
* Supported by `dotnet sln`.
* One project inventory.
* No solution divergence.
* Modern tooling direction.
* Easier scripted validation.

### Disadvantages

* Requires current tooling.
* Older IDEs may not support it.
* Some third-party tooling may still assume `.sln`.
* Migration path may be needed for an external tool.

---

## 19.2 One Legacy `Opure.sln`

### Advantages

* Broad historic tooling support.
* Familiar.
* Existing tooling compatibility.

### Disadvantages

* Difficult manual merge review.
* Legacy format.
* No advantage under the selected .NET 10 baseline.
* More opaque project and solution-folder changes.

---

## 19.3 Multiple Solutions

Examples:

* `Opure.Runtime.slnx`
* `Opure.Desktop.slnx`
* `Opure.Security.slnx`

### Advantages

* Smaller IDE views.
* Faster selective load.
* Subsystem focus.

### Disadvantages

* Project lists drift.
* Root build ambiguity.
* New project must be added repeatedly.
* Architecture can differ between views.
* Scripts need solution selection.
* Full test discovery becomes less obvious.

---

## 19.4 No Solution

### Advantages

* Pure project and CLI model.
* No solution maintenance.
* Easy per-project build.

### Disadvantages

* Weaker IDE onboarding.
* No one project inventory.
* Full build command less obvious.
* Solution-level organisation unavailable.
* Some tools expect a solution.

---

# 20. Solution Decision

Opure will use:

> **One committed `Opure.slnx` file as the single solution of record.**

No legacy `.sln` should be committed by default.

No second first-party solution should be created merely for convenience.

---

## 20.1 Compatibility Exception

If a required supported tool cannot consume `.slnx`, the team may:

* generate a temporary `.sln`;
* use direct project commands;
* or maintain a time-bounded compatibility file

only after recording:

* tool;
* limitation;
* owner;
* expiry;
* and divergence test.

---

## 20.2 Solution Folders

Solution folders may group:

* Hosts;
* Capabilities;
* Infrastructure;
* Platform;
* SDK;
* Tests;
* Benchmarks;
* Tools;
* and Documentation.

Solution folders do not grant dependencies or ownership.

---

# 21. Decision

Opure will provisionally adopt:

> **A single modular monorepo using `Opure.slnx`, projects organised by executable host and bounded capability, central .NET and NuGet policy, repository-local tools, central build artifacts and architecture-tested project-reference rules.**

This decision does not approve:

* one repository per capability;
* one giant Runtime implementation project;
* automatic four-layer project templates;
* a shared utility dumping ground;
* package-version declarations in ordinary project files;
* floating package versions;
* global developer tools as required build dependencies;
* multiple divergent solution files;
* generated code mixed with handwritten implementation;
* or platform-specific references from platform-neutral projects.

This decision is:

* [ ] Permanent until superseded
* [x] Provisional pending skeleton evidence
* [ ] Experimental only
* [ ] Limited to Windows
* [ ] Limited to version 1.0

---

# 22. Root Repository Layout

The proposed root layout is:

```text
C:\Opure\
├── .config\
│   └── dotnet-tools.json
├── .github\                         # Only when a GitHub workflow is selected
├── adr\
├── artifacts\                       # Generated and ignored
├── benchmarks\
├── docs\
├── eng\
├── samples\
├── specs\
├── src\
├── tests\
├── third_party\
├── tools\
├── .editorconfig
├── .gitattributes
├── .gitignore
├── Directory.Build.props
├── Directory.Build.targets
├── Directory.Packages.props
├── global.json
├── NuGet.config
├── Opure.slnx
├── ARCHITECTURE.md
├── CHARTER-001.md or specification link
├── CONTRIBUTING.md
├── LICENSE or licence notice
├── README.md
└── SECURITY.md
```

The Charter may remain under `specs/` if that is the established repository location.

There should not be two divergent authoritative copies.

---

# 23. Root File Responsibilities

## 23.1 `Opure.slnx`

Owns:

* complete first-party project inventory;
* solution-folder organisation;
* and the standard full build entry point.

It does not own architectural rules.

---

## 23.2 `global.json`

Owns:

* SDK feature band;
* patch roll-forward policy;
* prerelease policy;
* and preferred test runner.

---

## 23.3 `Directory.Build.props`

Owns early common properties such as:

* target framework baseline;
* language version;
* nullable analysis;
* analyzer policy;
* warning policy;
* central artifacts;
* deterministic-build defaults;
* documentation policy defaults;
* package-lock generation;
* and repository metadata.

---

## 23.4 `Directory.Build.targets`

Owns late enforcement such as:

* forbidden package declarations;
* project naming checks;
* generated-code checks;
* release metadata validation;
* unsupported target checks;
* architecture manifest generation;
* and build-time repository invariants.

It should remain small.

Complex logic belongs in a tested repository tool.

---

## 23.5 `Directory.Packages.props`

Owns:

* package versions;
* global analyzer or build package references;
* package-version policy;
* and approved package families.

---

## 23.6 `NuGet.config`

Owns:

* package sources;
* package-source mapping;
* signature-validation policy where supported;
* local cache behaviour where required;
* and disabled unapproved feeds.

It contains no credentials.

---

## 23.7 `.config/dotnet-tools.json`

Owns pinned repository-local tools.

It is a trusted executable manifest and requires review.

---

## 23.8 `.editorconfig`

Owns:

* whitespace;
* line endings;
* C# style;
* analyzer severities;
* naming;
* generated-code treatment;
* and Markdown or configuration conventions where tooling supports them.

---

## 23.9 `.gitattributes`

Owns repository-level text and binary treatment such as:

* line-ending normalisation;
* diff drivers;
* binary markers;
* and generated fixture handling.

---

## 23.10 `.gitignore`

Ignores:

* `artifacts/`;
* IDE state;
* user-specific files;
* local Runtime state;
* Vault state;
* databases;
* logs;
* temporary test outputs;
* package caches;
* local model files;
* and generated secrets.

---

# 24. Source Layout

The initial `src/` layout should be:

```text
src/
├── Hosts/
├── Desktop/
├── Capabilities/
├── Infrastructure/
├── Platform/
├── SDK/
└── Tools/
```

This is a navigation structure.

Project references remain the real dependency graph.

---

# 25. Executable Host Projects

Initial hosts are expected to include:

```text
src/Hosts/
├── Opure.Desktop/
├── Opure.Runtime/
├── Opure.Worker/
└── Opure.PluginHost/
```

Possible later hosts:

```text
├── Opure.Cli/
├── Opure.Recovery/
├── Opure.Updater/
└── Opure.Diagnostics/
```

---

## 25.1 Host Responsibilities

A host project may contain:

* process entry point;
* dependency composition;
* configuration bootstrapping;
* local IPC hosting;
* process lifecycle;
* operating-system integration;
* top-level error handling;
* and packaging metadata.

---

## 25.2 Host Prohibitions

A host should not contain:

* authoritative domain state machines;
* service-owned SQL;
* AI routing logic;
* patch application logic;
* Vault cryptography;
* or plugin permission policy.

---

## 25.3 Thin Does Not Mean Empty

A host may contain process-specific orchestration that genuinely belongs to that executable.

It should not become a pass-through facade with all composition hidden in an unowned shared project.

---

# 26. Desktop Projects

The Desktop area may begin with:

```text
src/Desktop/
├── Opure.Desktop.Shell/
├── Opure.Desktop.Controls/
└── Opure.Desktop.Presentation/
```

The exact number should remain minimal.

---

## 26.1 `Opure.Desktop`

The executable host owns:

* Avalonia application startup;
* window lifecycle;
* platform integration;
* Desktop Gateway client setup;
* and crash boundary.

---

## 26.2 `Opure.Desktop.Shell`

May own:

* navigation;
* workspace shell;
* view registration;
* layout;
* command routing;
* and shared presentation composition.

---

## 26.3 `Opure.Desktop.Controls`

May own reusable Opure-specific controls such as:

* diff viewer;
* approval panel;
* risk badge;
* process state;
* Trust timeline;
* and project tree.

---

## 26.4 `Opure.Desktop.Presentation`

May own platform-neutral view models and presentation state when extraction materially improves headless testing and prevents Avalonia leakage into capability contracts.

It should not duplicate all domain models.

---

## 26.5 Desktop Project Count

The first vertical slice should not create more Desktop projects than required.

New Desktop projects require a clear boundary such as:

* reusable control package;
* platform-neutral testable presentation;
* packaging;
* or process separation.

---

# 27. Capability Projects

Capability projects represent authoritative services and substantial supporting engines.

Proposed initial capability directory:

```text
src/Capabilities/
├── Opure.Projects/
├── Opure.Workspaces/
├── Opure.Patching/
├── Opure.Workflows/
├── Opure.AI/
├── Opure.Knowledge/
├── Opure.Build/
├── Opure.Repository/
├── Opure.Dependencies/
├── Opure.Environments/
├── Opure.Artefacts/
├── Opure.Trust/
├── Opure.Approvals/
├── Opure.Policies/
├── Opure.Plugins/
├── Opure.Mcp/
└── Opure.Notifications/
```

This is a direction, not a requirement to create every directory immediately.

A project is created when implementation begins.

---

# 28. Capability Granularity

The default capability shape is:

```text
Opure.<Capability>
Opure.<Capability>.Contracts      # Only when a boundary needs it
Opure.<Capability>.Persistence    # Only when persistence deserves isolation
Opure.<Capability>.<Adapter>      # Only for a real adapter boundary
```

---

## 28.1 Primary Implementation Project

One project should usually contain:

* domain behaviour;
* application orchestration;
* internal state machines;
* internal interfaces;
* internal validation;
* and internal handlers

for one bounded capability.

Folders and namespaces can separate concerns inside the project.

---

## 28.2 When to Extract Contracts

Create a contracts project when the types cross:

* process boundary;
* public SDK boundary;
* separately testable service boundary;
* plugin boundary;
* persisted event boundary;
* or generated protocol boundary.

Do not create a contracts project merely because every service template has one.

---

## 28.3 When to Extract Persistence

Create a persistence project when:

* a service-owned database has substantial code;
* the implementation has native or provider dependencies;
* architecture tests should block SQL from the capability core;
* or recovery testing benefits from a distinct assembly.

A small service may keep persistence internal initially.

---

## 28.4 When to Extract an Adapter

Extract an adapter for:

* external provider SDK;
* protocol;
* native dependency;
* operating-system integration;
* or replaceable implementation.

---

## 28.5 When Not to Extract

Do not extract solely for:

* one interface;
* one repository class;
* one folder;
* a fashionable layer name;
* or hypothetical future reuse.

---

# 29. Infrastructure Projects

Cross-cutting infrastructure should remain explicitly named.

Proposed area:

```text
src/Infrastructure/
├── Opure.Ipc.Abstractions/
├── Opure.Ipc.Grpc/
├── Opure.Ipc.NamedPipes.Windows/
├── Opure.Persistence.Abstractions/
├── Opure.Persistence.Sqlite/
├── Opure.Observability.Abstractions/
├── Opure.Observability.OpenTelemetry/
├── Opure.Secrets.Abstractions/
├── Opure.Secrets.Vault/
├── Opure.Filesystem.Abstractions/
├── Opure.Filesystem.Windows/
├── Opure.Messaging/
├── Opure.Scheduling/
└── Opure.ContentStore/
```

Only implemented components should exist.

---

## 29.1 No `Opure.Infrastructure`

One giant `Opure.Infrastructure` project is prohibited.

It would become a dependency sink and obscure ownership.

---

## 29.2 No `Opure.Core`

A universal `Opure.Core` project is prohibited unless a later ADR defines a genuinely small stable kernel.

---

# 30. Foundation Types

A few universally required low-level types may need a small project.

Possible project:

```text
Opure.Primitives
```

It may contain only stable non-domain primitives such as:

* operation identifiers;
* correlation identifiers;
* result envelope primitives;
* version ranges;
* and cancellation or time abstractions.

---

## 30.1 Primitive Project Rules

`Opure.Primitives` must:

* have no project references;
* have minimal package references;
* contain no service logic;
* contain no filesystem implementation;
* contain no persistence;
* contain no logging destination;
* and remain small.

---

## 30.2 Primitive Growth Gate

A size or dependency threshold should trigger review.

The project must not become `Common` under another name.

---

# 31. Platform Projects

Platform-neutral code lives under:

```text
src/Platform/
├── Opure.Platform.Abstractions/
├── Opure.Platform.Windows/
└── future platform projects
```

Specialised platform code may remain beside the relevant infrastructure area when that improves ownership.

---

## 31.1 Dependency Direction

Allowed:

```text
Opure.Platform.Windows
    → Opure.Platform.Abstractions
```

Prohibited:

```text
Opure.Platform.Abstractions
    → Opure.Platform.Windows
```

---

## 31.2 Platform Suffix

Platform projects should use explicit suffixes:

* `.Windows`
* `.Linux`
* `.MacOS`

Avoid generic projects containing extensive conditional compilation.

---

## 31.3 Conditional Compilation

Small platform differences may use compile-time conditions.

Substantial implementations belong in separate platform projects.

---

# 32. SDK Projects

Public or third-party-facing contracts live under:

```text
src/SDK/
├── Opure.PluginSdk/
├── Opure.PluginSdk.Abstractions/
├── Opure.PluginSdk.Testing/
├── Opure.McpSdk/                    # Only if Opure defines an extension surface
└── Opure.Contracts/                 # Only for intentionally public common contracts
```

---

## 32.1 SDK Stability

SDK projects require:

* public API review;
* XML documentation;
* semantic versioning;
* compatibility tests;
* package metadata;
* licence;
* and security review.

---

## 32.2 Internal Types

Internal Runtime implementation types must not leak into SDK packages.

---

## 32.3 SDK Dependencies

Public SDK dependencies should be minimal and stable.

A plugin should not need the full Runtime dependency graph.

---

# 33. Generated Contract Projects

Generated gRPC and Protocol Buffer code should live in explicit projects such as:

```text
Opure.Ipc.Contracts
Opure.PluginProtocol.Contracts
Opure.WorkerProtocol.Contracts
```

---

## 33.1 Schema Location

Each contract project should own its `.proto` files.

Example:

```text
src/Infrastructure/Opure.Ipc.Contracts/
├── Protos/
├── Generated/
└── Opure.Ipc.Contracts.csproj
```

Generated output may instead remain under `obj/` when consumers reference the compiled assembly.

Committed generated source is permitted only when required by external consumers or tooling.

---

## 33.2 Handwritten Extensions

Handwritten partial types or adapters should be separated from generated files.

---

## 33.3 Generated Header

Committed generated files require an unambiguous generated header.

---

## 33.4 No Manual Edits

Build validation should fail if generated files are changed without corresponding source-schema change or generation step.

---

# 34. Adapter Projects

AI provider adapters should follow:

```text
src/Capabilities/Opure.AI/
├── Opure.AI/
├── Opure.AI.Contracts/
└── Adapters/
    ├── Opure.AI.Ollama/
    ├── Opure.AI.OpenAICompatible/
    └── future adapters
```

Names are illustrative.

---

## 34.1 Provider SDK Isolation

A provider-specific SDK may be referenced only by its adapter project.

---

## 34.2 Adapter Conformance

Every adapter project references the shared conformance test package or test project.

---

# 35. Plugin Projects

First-party reference plugins should not be compiled into the Runtime implementation.

Suggested structure:

```text
samples/
└── Plugins/
    ├── Opure.SamplePlugin.Basic/
    └── Opure.SamplePlugin.BuildInspector/
```

First-party product plugins, if any, may live under:

```text
src/Plugins/
```

and still use the public SDK boundary.

---

# 36. Test Layout

Tests should mirror product ownership.

Examples:

```text
tests/
├── Unit/
│   ├── Opure.Patching.UnitTests/
│   ├── Opure.Workflows.UnitTests/
│   └── Opure.Policies.UnitTests/
├── Contract/
│   ├── Opure.Ipc.ContractTests/
│   └── Opure.PluginSdk.ContractTests/
├── Architecture/
│   └── Opure.ArchitectureTests/
├── Integration/
│   ├── Opure.Runtime.IntegrationTests/
│   ├── Opure.Persistence.IntegrationTests/
│   └── Opure.Ipc.IntegrationTests/
├── Process/
│   └── Opure.ProcessTopologyTests/
├── Security/
│   ├── Opure.Secrets.SecurityTests/
│   └── Opure.Filesystem.SecurityTests/
├── Recovery/
│   └── Opure.RecoveryTests/
├── Desktop/
│   ├── Opure.Desktop.HeadlessTests/
│   └── Opure.Desktop.EndToEndTests/
└── Release/
    └── Opure.ReleaseAcceptanceTests/
```

---

## 36.1 Test Project Naming

Use:

```text
<ProductProject>.UnitTests
<ProductProject>.IntegrationTests
```

when a direct ownership mapping exists.

Use scenario names for cross-cutting suites.

---

## 36.2 Production Dependency Direction

Production projects must never reference test projects.

---

## 36.3 Test Infrastructure

Shared test infrastructure should use explicit projects such as:

```text
Opure.Testing.Processes
Opure.Testing.Fakes
Opure.Testing.Security
Opure.Testing.Persistence
```

Avoid `Opure.Testing.Common`.

---

# 37. Benchmark Layout

Benchmarks live under:

```text
benchmarks/
├── Opure.Benchmarks/
├── Opure.Filesystem.Benchmarks/
├── Opure.Persistence.Benchmarks/
└── Opure.Desktop.Benchmarks/
```

Create only measured suites.

---

## 37.1 Benchmark Dependencies

Product projects never reference benchmark projects.

---

## 37.2 Benchmark Results

Generated results go under `artifacts/benchmarks/`.

Only reviewed baseline summaries are committed.

---

# 38. Engineering Scripts

Repository automation lives under:

```text
eng/
├── bootstrap.ps1
├── build.ps1
├── test.ps1
├── format.ps1
├── verify.ps1
├── pack.ps1
├── clean.ps1
├── common/
└── README.md
```

Future shell equivalents may be added.

---

## 38.1 Script Responsibilities

Scripts should orchestrate standard tools.

They should not reimplement:

* MSBuild dependency resolution;
* NuGet restore;
* test discovery;
* or package creation

without a clear need.

---

## 38.2 PowerShell Baseline

Windows-first scripts may use PowerShell 7 where selected.

If Windows PowerShell 5.1 compatibility is required, it must be declared explicitly.

---

## 38.3 Exit Codes

Every script returns the underlying failure accurately.

No script may print success after a failed child command.

---

# 39. Repository Tools

Complex validation should live in tested tools under:

```text
tools/
├── Opure.Tools.Architecture/
├── Opure.Tools.RepositoryValidation/
├── Opure.Tools.SpecValidation/
└── Opure.Tools.ReleaseEvidence/
```

These projects may be invoked through build targets or scripts.

---

## 39.1 Tool Dependency Direction

Repository tools may reference product contracts where necessary.

Product code must not reference repository tools.

---

# 40. Documentation Layout

Recommended documentation structure:

```text
docs/
├── development/
├── architecture/
├── security/
├── testing/
├── operations/
├── release/
└── decisions-index.md
```

Authoritative decisions remain in `adr/`.

Authoritative specifications remain in `specs/`.

---

## 40.1 No Duplicated Authority

A guide may summarise a specification.

It must link to the authoritative source and not silently diverge.

---

# 41. Samples

Samples should demonstrate supported public integration.

They must not become hidden production dependencies.

Examples include:

* Plugin SDK sample;
* MCP integration sample;
* provider adapter sample;
* and project import fixture.

---

# 42. Third-Party Source

Vendored code is discouraged.

When unavoidable, place it under:

```text
third_party/<name>/
```

with:

* source URL;
* exact revision;
* licence;
* modifications;
* update process;
* and security owner.

---

## 42.1 No Untracked Copy-Paste

Third-party source must not be copied into product projects without provenance.

---

# 43. Project Naming

All first-party .NET projects use the `Opure.` prefix.

Examples:

```text
Opure.Runtime
Opure.Patching
Opure.Patching.Contracts
Opure.Filesystem.Windows
Opure.PluginSdk
Opure.Patching.UnitTests
```

---

## 43.1 Assembly Name

Assembly name should match project name unless packaging requires otherwise.

---

## 43.2 Root Namespace

Root namespace should match project name.

---

## 43.3 Folder and Project Name

The project folder should normally match the project name.

---

## 43.4 Acronyms

Use consistent product terminology:

* `AI`, not mixed `Ai`;
* `IPC`, not mixed `Ipc` in human documentation;
* C# namespaces may use `AI` or the selected .NET naming convention consistently;
* `Mcp` may be used in identifiers if standard C# acronym casing is selected.

The final naming guide should resolve acronym casing.

---

# 44. Project Types

Each project should declare one type:

* Executable Host;
* Capability;
* Contract;
* Infrastructure;
* Platform Adapter;
* Public SDK;
* Tool;
* Test;
* Benchmark;
* Generator;
* or Sample.

The type should be inferable from location and name.

---

# 45. Project Creation Checklist

A new project requires:

* owner;
* project type;
* reason for separate assembly;
* allowed references;
* public API policy;
* package policy;
* test project;
* documentation update;
* and solution registration.

---

# 46. Project Extraction Criteria

Create a new assembly when at least one applies:

* process boundary;
* public SDK or package boundary;
* platform-specific implementation;
* security isolation;
* native dependency isolation;
* external provider dependency isolation;
* contract compatibility boundary;
* source generator or analyzer;
* independently packaged executable;
* or architecture enforcement materially benefits.

---

## 46.1 Weak Extraction Reasons

The following are insufficient alone:

* directory has many files;
* interface exists;
* unit tests are desired;
* class names share a suffix;
* or a diagram contains a box.

---

# 47. Project Merge Criteria

Merge projects when:

* they always change together;
* one has no independent boundary;
* references form a one-to-one chain;
* the public API is entirely internal;
* and separation adds ceremony without enforcement value.

---

# 48. Dependency Direction

The high-level allowed graph is:

```text
Hosts
  ↓
Capability Implementations
  ↓
Capability Contracts and Explicit Infrastructure Abstractions
  ↓
Primitives

Platform Implementations
  ↓
Platform Abstractions
  ↓
Primitives

Provider Adapters
  ↓
AI Adapter Contracts
  ↓
AI Capability Contracts
  ↓
Primitives

Desktop
  ↓
Desktop Gateway Contracts
  ↓
Primitives

Plugin Host
  ↓
Plugin Protocol Contracts
  ↓
Plugin SDK
  ↓
Primitives
```

---

# 49. Cross-Capability References

A capability implementation should not reference another capability implementation by default.

Preferred interactions:

* command contract;
* query contract;
* event contract;
* internal Runtime messaging;
* or a narrow service interface owned by the provider capability.

---

## 49.1 Temporary Direct Reference

A direct implementation reference may be allowed temporarily only when:

* both modules run in the Runtime;
* no cycle is created;
* ownership remains clear;
* and an architecture test documents the exception.

The exception requires expiry.

---

# 50. Contract Ownership

The provider capability owns the contract through which others request its behaviour.

A consumer does not define a private mirror of the provider's authoritative contract.

---

# 51. Contract Dependencies

Contracts may reference:

* `Opure.Primitives`;
* Protocol Buffer runtime where generated;
* and carefully selected standard libraries.

They must not reference:

* persistence;
* provider SDKs;
* Avalonia;
* Windows APIs;
* Runtime host;
* or concrete service implementations.

---

# 52. Host References

Hosts may reference:

* implementations;
* adapters;
* platform projects;
* composition libraries;
* and contract projects.

Because hosts are composition roots, broad references are expected.

They must not export host-specific types into contracts.

---

# 53. Desktop References

Desktop projects may reference:

* Desktop Gateway contracts;
* presentation models;
* Avalonia packages;
* and Desktop controls.

They must not reference:

* service persistence;
* Runtime implementations;
* Vault implementation;
* Windows filesystem implementation;
* or provider SDKs.

---

# 54. Worker References

The Worker host may reference:

* worker protocol contracts;
* approved worker task implementations;
* observability;
* and platform process helpers.

It must not reference all Runtime services.

---

# 55. Plugin Host References

The Plugin Host may reference:

* Plugin SDK;
* Plugin protocol contracts;
* isolated loading infrastructure;
* and bounded observability.

It must not reference Runtime capability implementations.

---

# 56. Public SDK References

Public SDK projects must not reference internal first-party projects except approved public primitives.

---

# 57. Platform References

Platform-neutral projects must not reference platform implementations.

---

# 58. Test Friend Assemblies

`InternalsVisibleTo` may be used for the directly corresponding test assembly.

---

## 58.1 Friend Rules

* exact assembly name;
* no wildcard;
* no broad test framework assembly;
* no production friend without ADR;
* and generated declarations centralised where possible.

---

## 58.2 Prefer Behaviour

Friend access should not replace public-behaviour tests.

Use it for deterministic internal state or algorithms where public exposure would be worse.

---

# 59. No Circular References

Project-reference cycles are prohibited.

Architecture validation should fail the build if a cycle appears.

---

# 60. Namespace Rules

Namespaces should mirror:

* product;
* capability;
* and internal concern.

Example:

```text
Opure.Patching.Validation
Opure.Patching.Application
Opure.Patching.Persistence
```

Folders inside one project may use these namespaces without requiring separate projects.

---

# 61. Internal Visibility

Capability implementation types should be `internal` by default.

Only intended composition and contract surfaces are public.

---

# 62. Public API Review

A public type is a compatibility decision.

Public API outside hosts and public SDK projects requires review.

---

# 63. SDK Pinning

The root `global.json` should pin the chosen .NET 10 SDK feature band.

Conceptual form:

```json
{
  "sdk": {
    "version": "<approved .NET 10 SDK feature band>",
    "rollForward": "latestPatch",
    "allowPrerelease": false,
    "errorMessage": "Install the Opure-approved .NET 10 SDK feature band."
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

The exact version belongs in the implementation baseline.

---

## 63.1 No Preview SDK

Preview SDK use is prohibited for ordinary builds.

A preview experiment must use:

* isolated branch;
* separate instructions;
* and no committed baseline change without ADR review.

---

## 63.2 Roll Forward

Patch roll-forward is preferred inside the pinned feature band.

Feature-band or major roll-forward requires deliberate baseline update.

---

# 64. Target Framework

Common product projects should target the approved .NET 10 target framework.

Platform-specific projects may use an explicit Windows target framework when required.

Examples conceptually include:

```text
net10.0
net10.0-windows
```

The exact minimum Windows version belongs in the platform baseline.

---

## 64.1 Multi-Targeting

Do not multi-target internal projects without an actual consumer requirement.

Public SDK projects may multi-target only after compatibility evidence.

---

# 65. Language Version

The C# language version should be pinned centrally to the approved stable version.

Do not use:

* `preview`;
* `latest`;
* or developer-local compiler assumptions.

---

# 66. Root Build Properties

Proposed root defaults include:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>14.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <Deterministic>true</Deterministic>
    <UseArtifactsOutput>true</UseArtifactsOutput>
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
</Project>
```

This is illustrative.

The implementation file should include only verified properties.

---

## 66.1 Target Framework Overrides

Projects override the target framework only for a documented platform or packaging reason.

---

## 66.2 Warnings as Errors

Warnings are errors in first-party projects.

Specific suppressions require:

* rule;
* scope;
* reason;
* owner;
* and review.

---

## 66.3 Nullable

Nullable reference types remain enabled.

Disabling nullable per project requires a migration plan.

---

## 66.4 Unsafe Code

Unsafe code is disabled by default.

A project enabling it requires:

* platform or performance reason;
* security review;
* and focused tests.

---

## 66.5 XML Documentation

XML documentation should be enabled for:

* public SDK;
* public contracts;
* and packages.

Internal implementation may enable it selectively.

---

# 67. CI Build Properties

Official builds should set:

```text
ContinuousIntegrationBuild=true
```

to enable CI-specific deterministic behaviour such as normalised paths.

---

## 67.1 Local Versus CI

Local builds should remain debugger friendly.

CI-specific properties should be conditioned on an explicit environment or script parameter.

---

# 68. Central Artifact Output

Set:

```xml
<UseArtifactsOutput>true</UseArtifactsOutput>
```

or an explicit root `ArtifactsPath`.

Outputs should appear under:

```text
artifacts/
├── bin/
├── obj/
├── publish/
├── package/
├── test-results/
├── coverage/
├── logs/
├── benchmarks/
└── release/
```

The SDK manages its supported categories.

Custom folders should remain under the same root.

---

## 68.1 Source Cleanliness

Project folders should not contain committed or unignored `bin/` and `obj/`.

---

## 68.2 Clean

`eng/clean.ps1` may delete `artifacts/` after confirming it is inside the repository root.

---

# 69. Central Package Management

Use one root:

```text
Directory.Packages.props
```

with:

```xml
<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
```

---

## 69.1 Project Files

Ordinary project files use:

```xml
<PackageReference Include="Package.Name" />
```

without a `Version`.

---

## 69.2 Version Overrides

Set:

```xml
<CentralPackageVersionOverrideEnabled>false</CentralPackageVersionOverrideEnabled>
```

by default.

An exception requires central review and should normally be represented as a conditional central version instead.

---

## 69.3 Floating Versions

Floating versions are prohibited.

---

## 69.4 Duplicate Functionality

Before adding a package, check whether the capability already exists in:

* .NET Base Class Library;
* approved infrastructure;
* or an existing package.

---

# 70. Package Lock Files

Enable:

```text
RestorePackagesWithLockFile=true
```

Commit generated `packages.lock.json` files.

---

## 70.1 CI Locked Mode

Continuous integration and release builds use:

```text
dotnet restore --locked-mode
```

or equivalent MSBuild property.

---

## 70.2 Lock-File Updates

A package update should include:

* central version change;
* affected lock files;
* release notes;
* licence review;
* vulnerability review;
* and test evidence.

---

## 70.3 Transitive Dependencies

Lock files record the resolved closure.

They do not eliminate the need to review transitive packages.

---

# 71. Transitive Pinning

NuGet transitive pinning should remain disabled initially unless a package-management review shows a clear need.

Direct dependencies should be declared deliberately.

---

# 72. Package Sources

`NuGet.config` should clear inherited package sources and declare only approved sources.

---

## 72.1 Package Source Mapping

Package-source mapping should constrain:

* public packages;
* organisation packages;
* and local development packages

to intended feeds.

---

## 72.2 Feed Credentials

Feed credentials must not be stored in `NuGet.config`.

They belong in:

* CI secret store;
* operating-system credential provider;
* or Opure Vault for Opure-managed workflows.

---

## 72.3 Source Ambiguity

The same package identity should not be retrievable from multiple ungoverned feeds.

---

# 73. Package Signatures

NuGet signature verification should use the supported SDK policy.

Additional trusted-signer restrictions may be added through a supply-chain ADR.

---

# 74. Package Approval

Every new runtime dependency requires:

* purpose;
* owner;
* licence;
* maintenance status;
* vulnerability review;
* transitive dependency inventory;
* native assets;
* network behaviour;
* data handling;
* and removal cost.

Test-only packages receive proportional review.

---

# 75. Package Categories

Classify packages as:

* Runtime;
* Build;
* Analyzer;
* Test;
* Tool;
* Source Generator;
* Native;
* Optional Adapter;
* or Public SDK Dependency.

---

# 76. Global Package References

Global package references may be used only for approved:

* analyzers;
* versioning;
* source-link;
* or build validation.

They must use development-only asset settings.

---

# 77. Local Tools

Repository tools should be declared in:

```text
.config/dotnet-tools.json
```

and restored with:

```powershell
dotnet tool restore
```

---

## 77.1 Tool Trust

Local tools run in full trust.

Every tool manifest change requires the same review as a build dependency.

---

## 77.2 No Required Global Tools

The standard build must not require manually installed global .NET tools.

---

## 77.3 Non-.NET Tools

External tools such as Git remain documented prerequisites when they cannot be repository local.

Versions should be validated by bootstrap scripts.

---

# 78. Bootstrapping

`eng/bootstrap.ps1` should:

1. identify repository root;
2. validate supported Windows version;
3. validate the pinned .NET SDK;
4. restore local tools;
5. restore packages;
6. validate Git;
7. validate optional desktop prerequisites;
8. and print exact next commands.

It must not:

* install unapproved software silently;
* change machine-wide policy;
* or collect telemetry.

---

# 79. Standard Build Commands

The canonical commands should remain standard:

```powershell
dotnet restore --locked-mode
dotnet build Opure.slnx --no-restore
dotnet test Opure.slnx --no-build
```

Repository wrappers may provide categories and evidence.

---

## 79.1 Developer First Run

A new developer should be able to run:

```powershell
pwsh .\eng\bootstrap.ps1
pwsh .\eng\build.ps1
pwsh .\eng\test.ps1 -Suite Fast
```

The exact PowerShell executable requirement must be documented.

---

# 80. Build Scripts

Build scripts should invoke:

* `dotnet restore`;
* `dotnet build`;
* `dotnet test`;
* `dotnet pack`;
* and `dotnet publish`

rather than reproducing MSBuild logic.

---

# 81. Project Files

Project files should remain declarative and small.

Common properties belong centrally.

Project-specific files declare:

* SDK;
* output type;
* target override;
* project references;
* package references;
* resources;
* and exceptional settings.

---

# 82. Explicit References

Project references should be explicit in `.csproj`.

Wildcard inclusion of arbitrary sibling projects is prohibited.

---

# 83. Solution Membership

Every first-party production, test and benchmark project should normally appear in `Opure.slnx`.

Experimental projects may remain outside only when marked and time bounded.

---

# 84. Solution Validation

A repository tool should compare:

* discovered project files;
* expected exclusions;
* and `Opure.slnx` membership.

Unexpected omissions fail validation.

---

# 85. Generated Projects

Temporary generated project files under `artifacts/` are excluded from solution membership.

---

# 86. Source Generators

A source generator should use a dedicated project ending with:

```text
.Generators
```

or:

```text
.SourceGeneration
```

---

## 86.1 Generator Dependencies

Generator projects should have minimal dependencies and appropriate target frameworks for analyzer loading.

---

## 86.2 Generated Output

Generated output remains under `obj/` unless committed output is required.

---

# 87. Analyzers

Opure-specific analyzers may live under:

```text
src/Tools/Opure.Analyzers/
```

They should be tested and consumed as development-only assets.

---

# 88. Native Interop

Native interop projects should be explicit.

Examples:

```text
Opure.Filesystem.Windows
Opure.Processes.Windows
Opure.Secrets.Platform.Windows
```

---

## 88.1 Native Assets

Native binary dependencies must be isolated and inventoried.

---

# 89. Resource Files

Desktop assets should live with the Desktop project that owns them.

Shared product branding may use an explicitly named asset project only when multiple packaged executables require it.

---

# 90. Configuration Files

Default application configuration should live with the owning host or capability.

Secrets never live in committed configuration.

---

# 91. Database Migrations

Service-owned migrations live with the service persistence project.

Example:

```text
Opure.Patching.Persistence/
└── Migrations/
```

One global migrations directory is prohibited.

---

# 92. Protocol Schemas

Protocol schemas live with their contract owner.

One undifferentiated repository-wide `protos/` directory is discouraged unless ownership is represented through subdirectories and projects.

---

# 93. SQL Files

SQL belongs to the owning persistence project.

Cross-service SQL is prohibited.

---

# 94. Web and UI Assets

The Desktop framework assets remain under Desktop projects.

A future web interface would use a separate host and project area.

---

# 95. Public Packaging

Only projects explicitly marked:

```xml
<IsPackable>true</IsPackable>
```

may produce NuGet packages.

---

## 95.1 Internal Projects

Internal product projects default to non-packable.

---

## 95.2 Package Metadata

Packable projects require:

* package ID;
* description;
* repository URL;
* licence;
* authorship;
* readme;
* icon where applicable;
* release notes;
* symbols;
* and deterministic package build.

---

# 96. Assembly Versioning

Application binaries should use one product version generated centrally.

Public SDK packages may use semantic versions aligned with their compatibility policy.

---

# 97. Version Source

The repository should have one authoritative version source.

The exact versioning tool requires a separate implementation decision.

---

# 98. Build Metadata

Official builds should include:

* product version;
* source commit;
* build date where reproducibility policy permits;
* target runtime;
* and dependency lock identity.

---

# 99. Reproducibility

The structure should support:

* pinned SDK;
* locked packages;
* deterministic compiler output;
* path normalisation in CI;
* central tools;
* and recorded build inputs.

Bit-for-bit reproducibility is a goal requiring later evidence.

---

# 100. Source Link

Public packages and diagnostic symbols should support Source Link through an approved central build package.

Exact configuration is deferred to release engineering.

---

# 101. Analyzers and Style

Use a root `.editorconfig`.

Analyzer severity is repository policy.

---

## 101.1 Project-Level Suppressions

Project files should not broadly disable analyzer categories.

Use scoped source suppression with justification when required.

---

## 101.2 Generated Code

Generated-code files should be excluded from style checks where appropriate while remaining compiled and security reviewed through their generator source.

---

# 102. Formatting

One formatting command should apply repository conventions.

Example:

```powershell
pwsh .\eng\format.ps1
```

Formatting should not rewrite generated or third-party files unexpectedly.

---

# 103. Line Endings

Use `.gitattributes` to define:

* text normalisation;
* PowerShell conventions;
* shell-script conventions;
* binary files;
* and golden-file exceptions.

Windows working copies must not create meaningless cross-platform diffs.

---

# 104. Repository-Local State

Opure's own development Runtime profile must not be stored in the source tree.

Recommended developer state location remains under the user's local application data.

---

# 105. `.opure` Directory

A future project-local `.opure` directory is not approved by this ADR.

Any project-local metadata requires a separate portability and Git-policy decision.

---

# 106. Secrets

The repository must not contain:

* API keys;
* real tokens;
* private keys;
* Vault files;
* test credentials with real authority;
* or feed passwords.

Secret scanning is release blocking.

---

# 107. Large Files

Large model files, SDK installers, database backups and build artefacts must not be committed.

Git LFS is not adopted by default.

A future need requires a separate decision.

---

# 108. Binary Fixtures

Small security, protocol or UI test fixtures may be committed when:

* necessary;
* licensed;
* size bounded;
* documented;
* and reviewed.

---

# 109. Git Submodules

First-party source must not use Git submodules.

Third-party submodules are discouraged and require an ADR.

---

# 110. Git Subtrees

Vendoring through subtree or source copy requires the third-party provenance policy.

---

# 111. Generated Documentation

Generated API documents belong under `artifacts/` unless the output is intentionally published and reviewed.

---

# 112. Architecture Enforcement

The architecture-test suite should consume:

* project graph;
* assembly references;
* package references;
* source symbols;
* namespaces;
* and repository paths.

---

## 112.1 Required Rules

Initial rules include:

* one primary solution;
* all expected projects in solution;
* no project cycles;
* no product reference to tests;
* no product reference to benchmarks;
* no Desktop reference to Runtime implementation;
* no contract reference to implementation;
* no platform abstraction reference to platform implementation;
* no plugin SDK reference to Runtime;
* no provider SDK outside adapter;
* no SQLite package outside approved persistence projects;
* no Avalonia package outside Desktop projects;
* no gRPC server package outside IPC and hosts;
* no secret implementation outside Vault projects;
* no direct Windows interop outside `.Windows` projects;
* no direct project file writes outside filesystem and patch projects;
* no catch-all project names;
* no package version in ordinary `.csproj`;
* and no floating package version.

---

## 112.2 Exceptions

Architecture exceptions require:

* rule;
* project;
* reason;
* owner;
* expiry;
* and follow-up ADR or issue.

---

# 113. Dependency Manifest

Generate a machine-readable dependency manifest containing:

* project type;
* owner;
* project references;
* package references;
* target framework;
* platform;
* packability;
* and public API status.

---

# 114. Dependency Visualisation

A repository tool may generate a dependency graph under `artifacts/architecture/`.

Generated diagrams are evidence, not the source of truth.

---

# 115. Circular Conceptual Dependencies

A project graph can be acyclic while services depend on each other conceptually.

Architecture review should also inspect:

* command loops;
* event loops;
* and shared-state assumptions.

---

# 116. Build Performance

Project boundaries affect incremental build performance.

---

## 116.1 Measure

Measure:

* clean restore;
* clean build;
* no-change build;
* one-capability edit;
* one-contract edit;
* Desktop-only edit;
* and test discovery.

---

## 116.2 Avoid Project Explosion

A new project must justify its build and navigation cost.

---

# 117. Solution Load Performance

The full solution should remain usable.

If project count becomes burdensome, prefer:

* solution filters generated from the one solution;
* IDE project unloading;
* command filters;
* or repository tools

before creating divergent solutions.

---

# 118. Solution Filters

Solution filters may be generated or committed if they remain derivative of `Opure.slnx`.

They must not become separate project inventories.

The exact `.slnf` compatibility with the chosen tooling must be verified.

---

# 119. Source Ownership

Each capability directory should include an ownership record when the team grows.

The initial founder-led repository may use a central owners document.

---

# 120. Code Owners

A future code-owner mechanism may map:

* security;
* Vault;
* filesystem;
* packaging;
* and public SDK

to required reviewers.

The hosting provider is not selected by this ADR.

---

# 121. Documentation Near Code

Complex capability projects should include a local `README.md` explaining:

* ownership;
* responsibility;
* dependencies;
* persistent state;
* and test commands.

The file does not duplicate specifications.

---

# 122. README Expectations

Every public SDK and executable host requires a local README before public preview.

---

# 123. Development Prerequisites

Root development documentation should list:

* supported Windows version;
* approved .NET SDK;
* Git version;
* PowerShell version;
* Visual Studio or alternative IDE;
* Avalonia prerequisites;
* and optional tools.

---

# 124. Clean-Clone Validation

A test environment should:

1. clone the repository;
2. verify no untracked generated requirements;
3. run bootstrap;
4. restore locked packages;
5. build;
6. test fast suite;
7. and publish one executable.

---

# 125. Offline Restore

A fully clean restore requires package access.

After dependencies are cached, the fast build and test suite should work offline.

A reproducible offline package mirror is deferred.

---

# 126. Dependency Update Workflow

A dependency update should:

1. identify package and owner;
2. review release notes;
3. update central version;
4. restore lock files;
5. inspect transitive changes;
6. run relevant tests;
7. run vulnerability and licence checks;
8. and commit as a focused change.

---

# 127. SDK Update Workflow

An SDK baseline update should:

1. review support lifecycle;
2. update `global.json`;
3. update target framework and language version when intended;
4. validate `.slnx`;
5. restore packages;
6. run full suite;
7. compare build output;
8. review breaking changes;
9. and record an ADR amendment or new ADR.

---

# 128. Dependency Downgrade

A package downgrade requires a documented compatibility or security reason.

---

# 129. Experimental Code

Experimental code should live under:

```text
experiments/
```

only if the repository chooses to add that root.

Experiments are:

* excluded from release;
* excluded from the primary solution by default;
* time bounded;
* and contain no real secrets or private project data.

This directory should not exist until needed.

---

# 130. Prototypes

A prototype intended to graduate should either:

* begin in a real project behind an experimental feature flag;
* or be migrated deliberately into the approved structure.

Production must not depend on an unowned prototype directory.

---

# 131. Feature Flags

Feature-flag code remains in owning projects.

One central feature-flag project should contain only the mechanism, not every feature's behaviour.

---

# 132. Configuration Ownership

Each capability owns its configuration schema.

Hosts compose configuration sources.

---

# 133. Environment Variables

Environment-variable names should use a consistent `OPURE_` prefix when part of supported developer or deployment configuration.

Secrets remain references.

---

# 134. Build-Time Generated Configuration

Generated configuration belongs under `artifacts/` or `obj/`.

It is not committed unless it is an intentional source artefact.

---

# 135. Public Contracts Versus Internal Messages

Public SDK contracts and internal Runtime messages should not share an assembly merely for convenience.

Their compatibility and trust requirements differ.

---

# 136. Event Contracts

Durable event contracts should live with the owning capability and remain versioned.

Not every in-process notification requires a public contract assembly.

---

# 137. Database Ownership in Structure

A service-owned persistence project and migration folder should make database ownership visible.

No root `Data` project should own all databases.

---

# 138. Secrets Ownership in Structure

Only approved Vault projects may reference cryptographic secret-record types.

Secret references may appear in contracts.

Secret material types must remain internal and non-serialisable.

---

# 139. Observability Ownership

Instrumentation abstractions may be shared.

Event identifiers and metrics remain owned by emitting capabilities.

One global log-event class should not own every service event.

---

# 140. Filesystem Ownership

Only filesystem and patch implementation projects may reference the Windows project-filesystem adapter for protected writes.

---

# 141. Network Ownership

Direct external HTTP clients should be isolated to approved adapters and the Network Gateway.

Architecture tests should detect new references.

---

# 142. Process Ownership

Process creation APIs should be isolated to Process Supervisor and approved test harness projects.

---

# 143. Avalonia Ownership

Avalonia references remain in Desktop projects and Desktop-specific test projects.

Domain and capability contracts remain UI neutral.

---

# 144. SQLite Ownership

`Microsoft.Data.Sqlite` references remain in approved persistence and migration projects.

---

# 145. OpenTelemetry Ownership

OpenTelemetry SDK configuration remains in host and observability composition projects.

Capability projects use .NET instrumentation abstractions.

---

# 146. gRPC Ownership

gRPC transport packages remain in IPC implementation and hosts.

Capability contracts should not depend on gRPC unless they are generated transport contracts.

---

# 147. Public API Baselines

Public SDK projects should generate API compatibility baselines before first public preview.

A tool is not selected by this ADR.

---

# 148. Binary Compatibility

Internal project binaries do not promise independent compatibility.

The full product is versioned together until an explicit package contract is published.

---

# 149. Process Protocol Compatibility

Process protocol contract projects do require version compatibility according to ADR-0004.

---

# 150. Friend Boundary for Generated Code

Generated contract projects may expose generated public types.

Handwritten service code should translate those types at the boundary when domain semantics differ.

---

# 151. Domain Models

Do not reuse database row models, gRPC generated messages or UI view models as the authoritative domain model merely to reduce project count.

---

# 152. Mapping

Mapping code belongs near the boundary:

* transport mapping in gateway or adapter;
* persistence mapping in persistence project;
* UI mapping in presentation project.

---

# 153. Shared Files

MSBuild linked source files across production projects are prohibited by default.

Use a project reference or source generator.

---

# 154. Partial Classes Across Projects

A partial class cannot span assemblies.

Do not use generated partial types as a reason to merge unrelated ownership.

---

# 155. Internals and Testing

Testing helpers should not be compiled into release outputs unless intentionally part of a testing SDK.

---

# 156. Conditional Test Code

`#if TEST` in production code is prohibited.

Use dependency injection, internal hooks or separate test assemblies.

---

# 157. Build Configuration

Supported configurations should initially remain:

* Debug;
* Release.

Additional configurations require a concrete packaging or security requirement.

---

# 158. Runtime Identifiers

Runtime identifiers are declared only by publishable executable projects or packaging scripts.

Library projects should not hardcode one runtime identifier.

---

# 159. Self-Contained Publishing

Self-contained or framework-dependent publication is a packaging decision.

The repository structure must support both without changing capability ownership.

---

# 160. Native AOT

Native AOT experiments belong to host publishing and compatibility tests.

Capability projects should avoid unnecessary reflection regardless.

---

# 161. Trimming

Trimming annotations and tests belong to relevant hosts and public libraries.

The full product trimming decision is deferred.

---

# 162. Release Outputs

Release artefacts should be gathered under:

```text
artifacts/release/<version>/
```

with:

* binaries;
* packages;
* symbols;
* manifests;
* hashes;
* licences;
* and evidence references.

---

# 163. Licence Inventory

Generate the dependency licence inventory under `artifacts/release/`.

Third-party notices intended to ship should be generated from reviewed data.

---

# 164. Security Files

Root `SECURITY.md` should explain:

* supported versions;
* vulnerability reporting;
* no-secret issue-report guidance;
* and diagnostic bundle handling.

---

# 165. Contribution Files

Before external contribution, root documentation should include:

* `CONTRIBUTING.md`;
* code of conduct if adopted;
* developer certificate or CLA decision;
* and build/test instructions.

---

# 166. Root Licence

The source licence remains a founder decision.

The repository should not imply an open-source licence before one is selected.

---

# 167. Case Sensitivity

Root filenames should use exact documented casing:

* `Directory.Build.props`;
* `Directory.Build.targets`;
* `Directory.Packages.props`;
* `NuGet.config`;
* `.editorconfig`.

This matters on future case-sensitive systems.

---

# 168. Windows Paths in Build Files

MSBuild files should use portable path construction.

Do not hardcode:

```text
C:\Opure
```

inside build logic.

Use repository-relative MSBuild properties.

---

# 169. User-Specific Build Files

Files such as:

* `.user`;
* `.suo`;
* per-user launch settings with secrets;
* and local signing paths

must not be committed.

---

# 170. Launch Profiles

Development launch profiles may be committed only when:

* safe;
* portable;
* and secret free.

---

# 171. Local Certificates

Development certificates and private keys must not be committed.

---

# 172. Test Certificates

Synthetic public test certificates may be committed when the private key is intentionally non-sensitive and clearly test-only.

---

# 173. Path Length of Repository

Project and folder names should remain descriptive but avoid needless depth.

The root at `C:\Opure` provides a short Windows path.

---

# 174. Project Depth

Prefer project files within four or five directory levels of the root.

Avoid nesting that creates long build paths.

---

# 175. File Naming

C# filenames should normally match the primary type.

Generated, partial and grouped internal files may use clear suffixes.

---

# 176. Large Classes

Repository structure does not justify one file per tiny type when grouping improves understanding.

Project and folder clarity matters more than ceremonial file counts.

---

# 177. Capability README

A capability README may contain:

* responsibility;
* owner;
* public contract;
* persistence;
* events;
* dependencies;
* and tests.

---

# 178. ADR Links

A capability README should link relevant ADRs rather than copying their complete content.

---

# 179. Specification Links

Root documentation should maintain the mapping from specifications to implementation areas.

---

# 180. Implementation Sequence

The first skeleton should create only projects required for the boot-to-health vertical slice.

Suggested first set:

```text
Opure.Desktop
Opure.Runtime
Opure.Primitives
Opure.Ipc.Contracts
Opure.Ipc.Grpc
Opure.Ipc.NamedPipes.Windows
Opure.Observability.Abstractions
Opure.Observability.OpenTelemetry
Opure.Platform.Abstractions
Opure.Platform.Windows
Opure.ArchitectureTests
Opure.Runtime.UnitTests
Opure.Ipc.IntegrationTests
Opure.ProcessTopologyTests
```

The exact set may be smaller if the vertical slice can prove the same boundaries.

---

# 181. Second Skeleton Increment

The safe project foundation may add:

```text
Opure.Projects
Opure.Projects.Contracts
Opure.Workspaces
Opure.Filesystem.Abstractions
Opure.Filesystem.Windows
Opure.Persistence.Sqlite
Opure.Trust
Opure.Secrets.Vault
```

Only when the roadmap reaches those capabilities.

---

# 182. No Empty Future Projects

Do not create every planned service as an empty project.

Empty architecture scaffolding becomes stale.

Create projects with the implementation milestone that needs them.

---

# 183. Project Templates

Repository-local project templates may be added after the first few projects reveal stable conventions.

Do not automate an unproven structure.

---

# 184. Scaffold Validation

A template should include:

* naming;
* central properties;
* ownership metadata;
* references;
* tests;
* and architecture registration.

---

# 185. Solution Organisation

Suggested solution folders:

```text
00 Documentation
10 Hosts
20 Desktop
30 Capabilities
40 Infrastructure
50 Platform
60 SDK
70 Tools
80 Tests
90 Benchmarks
```

Numeric prefixes are optional.

They affect display only.

---

# 186. Solution Build

`dotnet build Opure.slnx` should build all production projects and coded tests unless an explicitly non-buildable project is present.

---

# 187. Benchmarks in Solution Build

Benchmark projects may compile in the full solution.

They must not execute during ordinary tests.

---

# 188. Packaging Projects

Installer or packaging projects may require platform-specific tooling.

They should live under:

```text
src/Packaging/
```

or:

```text
eng/packaging/
```

after the packaging ADR.

---

# 189. IDE Compatibility

The supported IDE list must include versions that understand:

* `.slnx`;
* .NET 10;
* MTP;
* and Avalonia.

Older tooling is not automatically supported.

---

# 190. CLI as Source of Truth

Every required action must be executable from command line even when an IDE offers a convenience UI.

---

# 191. Build Output Verification

A repository validation test should fail if build output appears outside approved generated directories.

---

# 192. Source-Tree Mutation by Builds

Builds must not rewrite committed source files silently.

Code generation that updates committed artefacts requires an explicit command and clean-tree verification.

---

# 193. Dirty-Tree Detection

Release builds should detect unexpected source changes after build and test.

---

# 194. Restore Side Effects

Package restore must not execute unapproved arbitrary repository scripts.

Build-transitive packages require special review.

---

# 195. MSBuild Custom Logic

Custom MSBuild targets run in the trusted build.

They require code-review and security consideration.

---

# 196. Directory.Build Complexity

If root build files become complex:

* move logic into a tested tool;
* keep the target as a thin invocation;
* and document inputs and outputs.

---

# 197. Nested Build Files

Nested `Directory.Build.props` or `Directory.Packages.props` are discouraged.

They create hidden local policy.

A nested file requires a strong subsystem or public SDK reason and explicit parent import behaviour.

---

# 198. Central Package File Count

Use one root `Directory.Packages.props` initially.

---

# 199. Package Family Comments

The central package file should group versions by:

* .NET infrastructure;
* Avalonia;
* gRPC;
* OpenTelemetry;
* SQLite;
* testing;
* analyzers;
* and tools.

Comments must not replace package review records.

---

# 200. Package Version Variables

Use named MSBuild properties for tightly aligned package families only when this improves safe updates.

Avoid indirect version indirection that hides the package's actual version.

---

# 201. Repository Validation Command

Provide:

```powershell
pwsh .\eng\verify.ps1
```

It should check:

* clean project inventory;
* solution membership;
* package policy;
* architecture rules;
* generated code;
* formatting;
* specifications;
* ADR naming;
* and no forbidden files.

---

# 202. ADR Naming

ADRs remain:

```text
ADR-0001-description.md
```

The repository validator should detect duplicate numbers.

---

# 203. Specification Naming

Specifications remain:

```text
SPEC-001.md
```

or the established repository naming pattern.

---

# 204. Documentation Links

Broken internal documentation links should fail a documentation validation stage before release.

---

# 205. Repository Index

A generated index may list:

* specifications;
* ADRs;
* projects;
* test suites;
* and package inventory.

The generated index belongs under `artifacts/` unless reviewed and committed.

---

# 206. Security Impact

## 206.1 Trust Boundaries

The repository structure affects:

* build tools;
* NuGet feeds;
* generated code;
* analyzers;
* source generators;
* local tools;
* test hooks;
* native assets;
* and public SDK boundaries.

---

## 206.2 Threats

Relevant threats include:

* dependency confusion;
* package-source ambiguity;
* malicious build-transitive package;
* compromised local tool;
* hidden preview SDK;
* generated-source tampering;
* secret commit;
* test code entering release;
* platform-specific code bypassing review;
* and architecture erosion.

---

## 206.3 Mitigations

* source mapping;
* locked restore;
* central versions;
* package review;
* local tool pinning;
* stable SDK;
* architecture tests;
* generated-code verification;
* no secrets;
* and release inventory.

---

# 207. Supply-Chain Impact

Every additional package, tool, analyzer, generator and native binary becomes part of the supply chain.

The repository structure makes these dependencies visible and centrally reviewable.

---

# 208. Privacy Impact

The repository must not contain:

* private user paths;
* private project fixtures;
* prompts;
* provider responses;
* real diagnostic bundles;
* or live credentials.

Synthetic data should be obvious and documented.

---

# 209. Reliability Impact

Central policy reduces configuration drift.

A root-file error can affect every project.

Changes to root build files require broad test evidence.

---

# 210. Performance Impact

The modular graph may increase project count.

It also improves:

* incremental compilation;
* selective testing;
* parallel build;
* and clear ownership

when boundaries are chosen carefully.

---

# 211. Testing Strategy

ADR-0008 applies.

The structure itself requires tests.

---

## 211.1 Architecture Tests

Test the project graph and forbidden dependencies.

---

## 211.2 Clean Clone Test

Build from a fresh clone.

---

## 211.3 Locked Restore Test

Delete package caches where practical and restore with locked mode.

---

## 211.4 Tool Restore Test

Restore local tools from the manifest.

---

## 211.5 Solution Membership Test

Compare projects to `Opure.slnx`.

---

## 211.6 Build Output Test

Verify generated files remain under approved roots.

---

## 211.7 Cross-Platform Build Test

Platform-neutral projects should build on future non-Windows CI when introduced.

---

# 212. Prototype Plan

## 212.1 Prototype A — Root Build Files

Create:

* `global.json`;
* `Directory.Build.props`;
* `Directory.Build.targets`;
* `Directory.Packages.props`;
* `NuGet.config`;
* `.editorconfig`;
* `.gitattributes`;
* and `.config/dotnet-tools.json`.

---

## 212.2 Prototype B — `Opure.slnx`

Create one solution and representative solution folders.

---

## 212.3 Prototype C — Vertical Slice Projects

Create:

* Desktop host;
* Runtime host;
* IPC contracts;
* IPC implementation;
* Windows named-pipe transport;
* and observability.

---

## 212.4 Prototype D — Architecture Tests

Enforce project-reference and package rules.

---

## 212.5 Prototype E — Locked Restore

Generate and commit lock files.

Prove locked restore failure on uncommitted dependency change.

---

## 212.6 Prototype F — Central Artifacts

Prove build, test and publish outputs remain under `artifacts/`.

---

## 212.7 Prototype G — Local Tools

Restore and execute one approved repository tool.

---

## 212.8 Prototype H — Clean Clone

Build and test in a new directory with documented prerequisites.

---

# 213. Implementation Plan

## 213.1 Initial Tasks

1. Record founder review.
2. Confirm .NET 10 SDK feature band.
3. Confirm supported Visual Studio version.
4. Create `Opure.slnx`.
5. Create `global.json`.
6. Create root MSBuild files.
7. Create central package file.
8. Create `NuGet.config`.
9. Create local tool manifest.
10. Create root style and Git files.
11. Create `src`, `tests`, `benchmarks`, `eng`, `samples`, `tools` and `third_party` roots as needed.
12. Create the minimum boot-to-health projects.
13. Add architecture tests.
14. Add solution-membership validation.
15. Add package-policy validation.
16. Add central artifacts.
17. Generate package lock files.
18. Add bootstrap, build, test and verify scripts.
19. Run clean-clone validation.
20. Benchmark build and test discovery.
21. Complete security review.
22. Accept, amend or reject the ADR.

---

## 213.2 Owners

| Area                    | Owner                         |
| ----------------------- | ----------------------------- |
| Product decision        | Founder                       |
| Repository architecture | Repository Architecture Owner |
| .NET build policy       | Build Engineering Owner       |
| Runtime project graph   | Runtime Architecture Owner    |
| Desktop project graph   | Desktop Owner                 |
| Package policy          | Supply-Chain Owner            |
| Test layout             | Test Architecture Owner       |
| SDK layout              | Plugin SDK Owner              |
| Release layout          | Release Engineering Owner     |
| Security review         | Security Owner                |
| Documentation layout    | Technical Documentation Owner |

---

# 214. Proposed Initial Tree

A realistic initial tree after Phase 1 may be:

```text
C:\Opure\
├── .config\
│   └── dotnet-tools.json
├── adr\
│   ├── ADR-TEMPLATE.md
│   ├── ADR-0001-primary-implementation-language.md
│   └── ...
├── artifacts\
├── benchmarks\
├── docs\
│   └── development\
├── eng\
│   ├── bootstrap.ps1
│   ├── build.ps1
│   ├── test.ps1
│   ├── verify.ps1
│   └── README.md
├── samples\
├── specs\
│   ├── CHARTER-001.md
│   ├── SPEC-001.md
│   └── ...
├── src\
│   ├── Hosts\
│   │   ├── Opure.Desktop\
│   │   │   └── Opure.Desktop.csproj
│   │   ├── Opure.Runtime\
│   │   │   └── Opure.Runtime.csproj
│   │   ├── Opure.Worker\
│   │   │   └── Opure.Worker.csproj
│   │   └── Opure.PluginHost\
│   │       └── Opure.PluginHost.csproj
│   ├── Infrastructure\
│   │   ├── Opure.Ipc.Contracts\
│   │   ├── Opure.Ipc.Grpc\
│   │   ├── Opure.Ipc.NamedPipes.Windows\
│   │   ├── Opure.Observability.Abstractions\
│   │   └── Opure.Observability.OpenTelemetry\
│   ├── Platform\
│   │   ├── Opure.Platform.Abstractions\
│   │   └── Opure.Platform.Windows\
│   └── Opure.Primitives\
│       └── Opure.Primitives.csproj
├── tests\
│   ├── Architecture\
│   │   └── Opure.ArchitectureTests\
│   ├── Integration\
│   │   └── Opure.Ipc.IntegrationTests\
│   ├── Process\
│   │   └── Opure.ProcessTopologyTests\
│   └── Unit\
│       └── Opure.Runtime.UnitTests\
├── third_party\
├── tools\
│   └── Opure.Tools.RepositoryValidation\
├── .editorconfig
├── .gitattributes
├── .gitignore
├── ARCHITECTURE.md
├── CONTRIBUTING.md
├── Directory.Build.props
├── Directory.Build.targets
├── Directory.Packages.props
├── global.json
├── NuGet.config
├── Opure.slnx
├── README.md
└── SECURITY.md
```

Only required directories should be created.

Empty directories are not preserved by Git and do not need placeholder files without purpose.

---

# 215. Example Capability Tree

When Patching is implemented:

```text
src/Capabilities/Opure.Patching/
├── Opure.Patching/
│   ├── Application/
│   ├── Domain/
│   ├── Validation/
│   └── Opure.Patching.csproj
├── Opure.Patching.Contracts/
│   ├── Commands/
│   ├── Queries/
│   ├── Events/
│   └── Opure.Patching.Contracts.csproj
└── Opure.Patching.Persistence/
    ├── Migrations/
    ├── Queries/
    └── Opure.Patching.Persistence.csproj
```

This three-project form is justified because:

* contracts cross the Desktop Gateway and workflow boundary;
* persistence owns a Critical journal;
* and the main capability remains independent of SQLite details.

A smaller capability may use only one project.

---

# 216. Example Provider Adapter Tree

```text
src/Capabilities/Opure.AI/
├── Opure.AI/
├── Opure.AI.Contracts/
├── Opure.AI.Provider.Abstractions/
└── Adapters/
    ├── Opure.AI.Ollama/
    └── Opure.AI.OpenAICompatible/
```

The exact provider list remains a separate implementation decision.

---

# 217. Example Public SDK Tree

```text
src/SDK/Opure.PluginSdk/
├── Capabilities/
├── Contracts/
├── Manifest/
├── Permissions/
├── README.md
└── Opure.PluginSdk.csproj
```

---

# 218. Example Test Mapping

```text
src/Capabilities/Opure.Patching/Opure.Patching/
    ↕
tests/Unit/Opure.Patching.UnitTests/

src/Capabilities/Opure.Patching/Opure.Patching.Persistence/
    ↕
tests/Integration/Opure.Patching.PersistenceTests/

src/Hosts/Opure.Runtime/
    ↕
tests/Process/Opure.ProcessTopologyTests/
```

---

# 219. Repository Growth Rules

Before adding a root directory, ask:

* Is it a distinct long-lived category?
* Does it have clear ownership?
* Does it overlap an existing root?
* Will a new contributor understand it?
* Does build or release tooling need it?
* Can it remain a subdirectory instead?

---

# 220. Project Growth Rules

Before adding a project, ask:

* What boundary does the assembly enforce?
* Who owns it?
* Who consumes it?
* Is it public?
* Is it platform-specific?
* Does it require different dependencies?
* Does it need independent packaging?
* Can folders inside an existing project enforce the same clarity?
* What architecture test protects the boundary?

---

# 221. Dependency Growth Rules

Before adding a project reference, ask:

* Is this provider's contract being used?
* Is the implementation being bypassed?
* Does this create conceptual shared ownership?
* Could an event or query preserve ownership?
* Does it create a cycle?
* Does the target expose too much?

---

# 222. Package Growth Rules

Before adding a package, ask:

* Does .NET already provide it?
* Is it active and supported?
* What data does it access?
* Does it run build logic?
* Does it include native code?
* Does it open network connections?
* What are its transitive dependencies?
* Can it be isolated to one adapter?

---

# 223. Root Build Change Rules

Changes to:

* `global.json`;
* `Directory.Build.props`;
* `Directory.Build.targets`;
* `Directory.Packages.props`;
* `NuGet.config`;
* and local tool manifest

require broader review than ordinary capability code.

---

# 224. Release Requirements

A release must not ship if:

* more than one divergent primary solution exists;
* the SDK is unpinned;
* preview SDK use is required;
* package versions appear in ordinary project files without exception;
* locked restore fails;
* unapproved feeds are configured;
* architecture project cycles exist;
* Desktop references Runtime implementations;
* public SDK references internal Runtime projects;
* provider SDKs leak outside adapters;
* platform-neutral projects reference Windows implementations;
* build outputs appear in source directories;
* or test-only assemblies enter release packages.

---

# 225. Acceptance Criteria

This ADR may move to **Accepted** when:

* [ ] The repository uses one `Opure.slnx`.
* [ ] No committed legacy `.sln` exists without approved compatibility reason.
* [ ] The chosen IDE opens `Opure.slnx`.
* [ ] `dotnet sln Opure.slnx list` succeeds.
* [ ] `global.json` pins the approved .NET 10 SDK feature band.
* [ ] Prerelease SDK use is disabled.
* [ ] MTP is selected in `global.json`.
* [ ] `Directory.Build.props` exists and is documented.
* [ ] `Directory.Build.targets` remains small and tested.
* [ ] `Directory.Packages.props` enables Central Package Management.
* [ ] Ordinary project files omit package versions.
* [ ] Per-project version overrides are disabled by default.
* [ ] Floating package versions are rejected.
* [ ] `NuGet.config` clears inherited sources.
* [ ] Package-source mapping is configured.
* [ ] Feed credentials are absent from the repository.
* [ ] Package lock files are generated and committed.
* [ ] Locked restore passes.
* [ ] A dependency change without updated lock file fails locked restore.
* [ ] `.config/dotnet-tools.json` exists.
* [ ] Local tools restore through one command.
* [ ] No required global .NET tool exists.
* [ ] `.editorconfig` is active.
* [ ] `.gitattributes` defines line-ending policy.
* [ ] `.gitignore` excludes artifacts, Runtime state, Vault state and secrets.
* [ ] `UseArtifactsOutput` or explicit `ArtifactsPath` centralises output.
* [ ] Build output remains under `artifacts/`.
* [ ] Executable host projects contain composition rather than domain ownership.
* [ ] The Desktop does not reference Runtime implementation projects.
* [ ] Contract projects do not reference implementations.
* [ ] Platform abstractions do not reference Windows implementations.
* [ ] Provider SDKs exist only in adapter projects.
* [ ] SQLite packages exist only in approved persistence projects.
* [ ] Avalonia packages exist only in Desktop and Desktop-test projects.
* [ ] Vault implementation remains isolated.
* [ ] Filesystem Windows implementation remains isolated.
* [ ] Plugin SDK remains free of Runtime implementation references.
* [ ] Public SDK projects are explicitly packable.
* [ ] Internal projects are non-packable by default.
* [ ] No project named Common, Utils or Helpers exists.
* [ ] Project-reference cycles are rejected.
* [ ] Production projects cannot reference tests or benchmarks.
* [ ] Architecture exceptions have owners and expiry.
* [ ] Generated code is separated and verifiable.
* [ ] Every first-party project is in `Opure.slnx` or explicitly excluded.
* [ ] Root bootstrap, build, test and verify scripts exist.
* [ ] A clean clone restores, builds and tests.
* [ ] The fast suite works after dependencies are cached and network is disabled.
* [ ] Build and test duration is measured.
* [ ] Security review is complete.
* [ ] Founder approval is recorded.

---

# 226. Evidence Required Before Acceptance

* [ ] root tree review;
* [ ] `.slnx` tooling report;
* [ ] SDK pinning report;
* [ ] root MSBuild files;
* [ ] central package file;
* [ ] package-source mapping report;
* [ ] locked restore report;
* [ ] local tool restore report;
* [ ] central artifacts report;
* [ ] project inventory;
* [ ] dependency graph;
* [ ] architecture-test report;
* [ ] generated-code verification report;
* [ ] clean-clone build report;
* [ ] offline fast-suite report;
* [ ] build performance report;
* [ ] package and licence inventory;
* [ ] security review;
* [ ] founder approval.

---

# 227. Known Limitations

* The exact .NET 10 SDK feature band is not pinned by this file.
* The supported Visual Studio version is not final.
* Some third-party tools may not support `.slnx`.
* The final project list will grow with roadmap implementation.
* The exact public SDK package split is not final.
* The exact versioning tool is not selected.
* The final code-coverage and API-baseline tools are not selected.
* The CI provider is not selected.
* Package-signature restrictions beyond normal SDK verification are deferred.
* Offline package mirroring is deferred.
* Solution filters are deferred.
* Linux and macOS build agents are deferred.
* Native AOT and trimming structure is provisional.
* Packaging projects are deferred.
* The source licence is not selected.
* Repository ownership files are deferred until the team grows.
* Full bit-for-bit reproducibility is not yet proven.

---

# 228. Open Questions

* Which exact .NET 10 SDK feature band should be pinned?
* Which Visual Studio version is the minimum supported version?
* Should the repository require PowerShell 7?
* Should the first solution use numeric solution-folder prefixes?
* Should `Opure.Primitives` exist immediately or emerge from concrete duplication?
* Which initial capabilities deserve separate contracts projects?
* Should Desktop presentation use one or two non-host projects?
* Should persistence be separate for every Critical store from the beginning?
* Should `Opure.Ipc.Contracts` contain all local protocols or should worker and plugin protocols be separate?
* Should generated gRPC source be committed?
* Which package-source mappings should be used initially?
* Should NuGet signature validation require selected trusted signers?
* Should transitive pinning remain disabled permanently?
* Which local tools are required for Phase 1?
* Should repository validation be a local .NET tool or ordinary project executable?
* Should architecture tests inspect MSBuild project files directly or a generated graph?
* Which architecture exceptions are acceptable during the first vertical slice?
* Should one root test project run repository validation?
* Should public SDK projects multi-target?
* Which project should own common error codes?
* How should acronym casing appear in project names and namespaces?
* Should `src/Infrastructure` and `src/Platform` remain separate?
* Should provider adapters live under the AI capability or a root adapters directory?
* Where should packaging projects live?
* Should first-party plugins live under `src/Plugins` or use `samples/` until proven?
* Should project-local READMEs be required immediately?
* Which generated documentation should be committed?
* Should Source Link be configured in Phase 1?
* Which versioning system should provide product versions?
* Should release builds fail on a dirty tree?
* Should package licence reports be generated on every pull request?
* Should build outputs use the SDK default `artifacts/` pivots or a custom naming convention?
* Should test results be under the SDK artifacts structure or a custom subdirectory?
* How should temporary compatibility `.sln` files be generated?
* When should solution filters be added?
* What project-count threshold triggers structure review?
* What build-duration threshold triggers project graph review?
* When would repository splitting become justified?
* Which repository files require security-owner approval?
* Should a root `CODEOWNERS` file be added before outside contributors?
* Which source licence should Opure use?

---

# 229. Deferred Decisions

This ADR intentionally defers:

* continuous-integration provider to a CI ADR;
* versioning tool to a Versioning ADR;
* packaging structure to a Packaging ADR;
* installer structure to an Installer ADR;
* code-signing infrastructure to a Signing ADR;
* Source Link details to a Release Engineering ADR;
* public package publication to an SDK Release ADR;
* source licence to a founder decision;
* package trusted-signers policy to a Supply-Chain Security ADR;
* offline package mirror to an Enterprise Build ADR;
* code ownership to a Contribution Governance ADR;
* solution filters to implementation evidence;
* and repository splitting to future organisational evidence.

---

# 230. Alternatives Rejected

## 230.1 Repository per Process

Rejected because internal contract changes, process tests and release coordination would become expensive before independent teams or release cadences exist.

---

## 230.2 Repository per Capability

Rejected because it imposes distributed organisational overhead on a small founder-led implementation.

---

## 230.3 One Giant Runtime and Desktop Project

Rejected because compile-time boundaries are required to protect ownership, platform isolation and future extraction.

---

## 230.4 Four Projects per Service Automatically

Rejected because project separation must enforce a real boundary rather than a template.

---

## 230.5 Internal NuGet Packages for All Modules

Rejected because source project references better support atomic product development.

---

## 230.6 Legacy `.sln` as the Default

Rejected because the selected .NET 10 baseline supports and defaults to the more maintainable `.slnx` format.

---

## 230.7 Multiple Primary Solutions

Rejected because project inventories and build expectations would diverge.

---

## 230.8 Required Global Tools

Rejected because a clean clone must declare and restore its own supported tooling.

---

## 230.9 Package Versions in Project Files

Rejected because central dependency governance and review are required.

---

## 230.10 Shared Common or Utilities Project

Rejected because it obscures ownership and becomes a dependency magnet.

---

# 231. Official Evidence References

The following official sources informed this ADR:

* [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy)
* [`dotnet sln` command](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-sln)
* [`.slnx` default in .NET 10](https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/10.0/dotnet-new-sln-slnx-default)
* [`global.json` overview](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json)
* [Customise builds by directory](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory)
* [MSBuild build process](https://learn.microsoft.com/en-us/visualstudio/msbuild/build-process-overview)
* [Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)
* [PackageReference lock files](https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files)
* [.NET upgrade guidance and locked restore](https://learn.microsoft.com/en-us/dotnet/core/install/upgrade)
* [Package Source Mapping](https://learn.microsoft.com/en-us/nuget/consume-packages/package-source-mapping)
* [Artifacts output layout](https://learn.microsoft.com/en-us/dotnet/core/sdk/artifacts-output)
* [.NET SDK MSBuild properties](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props)
* [Code analysis configuration](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files)
* [.NET local tools](https://learn.microsoft.com/en-us/dotnet/core/tools/local-tools-how-to-use)
* [.NET tools overview](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools)
* [NuGet signed-package verification](https://learn.microsoft.com/en-us/dotnet/core/tools/nuget-signed-package-verification)

Platform, SDK and tooling support can change.

The implementation team must verify exact versions and supported tools before acceptance.

---

# 232. Review Record

| Date         | Reviewer           | Decision | Notes                                             |
| ------------ | ------------------ | -------- | ------------------------------------------------- |
| 18 July 2026 | Architecture draft | Proposed | Modular monorepo and one `Opure.slnx` recommended |

---

# 233. Approval

## Founder or Product Approval

* **Name:** Christopher Dyer
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Pending founder review

## Repository Architecture Approval

* **Name or role:** Repository Architecture Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Skeleton, graph and clean-clone evidence required

## Build Engineering Approval

* **Name or role:** Build Engineering Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** SDK, MSBuild, package, tool and artifact policies require evidence

## Test Architecture Approval

* **Name or role:** Test Architecture Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Test discovery, architecture checks and suite layout require evidence

## Security Approval

* **Name or role:** Security Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Package sources, tools, generators, secrets and dependency rules require review

---

# 234. Supersession

This ADR is superseded only when a later ADR:

* names ADR-0010 explicitly;
* explains why the repository, solution or project organisation changed;
* identifies affected projects and build policy;
* describes migration;
* explains developer and release impact;
* and updates the `Superseded by` field.

Historical ADRs remain in version control.

---

# 235. Change History

| Version | Date         | Author        | Summary                                                  |
| ------- | ------------ | ------------- | -------------------------------------------------------- |
| 0.1     | 18 July 2026 | Founder Draft | Initial modular monorepo and `Opure.slnx` recommendation |

---

# 236. Final Decision Statement

> **Opure will provisionally use one modular monorepo with `Opure.slnx` as the single solution of record, source projects organised by executable host and bounded capability, central SDK, build, package and tool policy, repository-local test and engineering infrastructure, explicit contract and platform boundaries, and architecture-tested dependency rules, because the repository must make ownership and trust boundaries visible while remaining practical for a small team and adaptable to future process, platform and public SDK extraction.**

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**