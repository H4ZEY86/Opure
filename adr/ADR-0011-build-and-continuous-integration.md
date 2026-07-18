# ADR-0011 — Build and Continuous Integration

## Architecture Decision Record

**Status:** Proposed
**Date:** 18 July 2026
**Decision owners:** Founder and Product Owner
**Technical owners:** Build and Continuous Integration Owner
**Reviewers:** Repository Architecture Owner, Test Architecture Owner, Security Owner, Release Engineering Owner, Runtime Architecture Owner, Desktop Owner, Performance Owner
**Supersedes:** None
**Superseded by:** None
**Related ADRs:** ADR-0001 Primary Implementation Language, ADR-0002 Desktop Application Framework, ADR-0003 Runtime Process Topology, ADR-0004 Local IPC, ADR-0005 Persistence, ADR-0006 Logging and Observability, ADR-0007 Secrets Vault, ADR-0008 Testing Strategy, ADR-0009 Windows Path and Filesystem Handling, ADR-0010 Repository and Solution Structure
**Related specifications:** CHARTER-001, SPEC-001, SPEC-002, SPEC-003, SPEC-007, SPEC-008, SPEC-009, SPEC-010, SPEC-011, SPEC-012
**Target milestone:** Phase 0 — Founding Baseline through Phase 1 — Architecture Skeleton, with progressive expansion through Version 1.0

---

## 1. Decision Summary

Opure should use a **provider-neutral local build contract with GitHub Actions as the initial hosted continuous-integration implementation**.

The build and CI architecture should use:

* repository-owned PowerShell entry points under `eng/` as the canonical build behaviour;
* standard `dotnet` commands beneath those entry points;
* `Opure.slnx` as the complete solution;
* the SDK pinned by `global.json`;
* NuGet Central Package Management;
* committed package lock files;
* locked restore;
* Release configuration for CI compilation;
* `ContinuousIntegrationBuild=true` on official CI builds;
* Microsoft Testing Platform for test execution;
* central `artifacts/` output;
* deterministic build settings;
* GitHub-hosted clean virtual machines for all untrusted pull-request work;
* `windows-2025` as the initial explicit Windows hosted-runner label;
* `ubuntu-24.04` for selected platform-neutral compile and test checks;
* self-hosted Windows runners only for trusted, manually or branch-gated workloads that require reference hardware, GPU, real desktop interaction or specialist installation state;
* minimal workflow permissions;
* no workflow secrets for pull-request validation;
* full-length commit-SHA pinning for every external GitHub Action;
* an allowlist of approved actions and reusable workflows;
* concurrency cancellation for superseded pull-request runs;
* explicit build, test, security, nightly and release-candidate workflows;
* package vulnerability auditing across direct and transitive packages;
* security and recovery gates;
* bounded caches and artefact retention;
* release evidence bundles;
* build provenance attestations where the repository plan supports them;
* and exact promotion of tested artefacts rather than rebuilding during publication.

The initial CI provider should be GitHub Actions because it aligns naturally with the Git repository, supports fresh Windows hosted runners, has first-party .NET setup and cache integration, supports minimal token permissions, provides protected environments and can generate build provenance attestations.

Opure's actual build logic must not depend on GitHub-specific behaviour.

A contributor should be able to run substantially the same build and test commands locally.

GitHub workflow YAML should orchestrate repository scripts rather than contain a second implementation of the build.

---

## 2. Status

Current status:

> **Proposed**

This ADR becomes **Accepted** only after the Phase 1 build skeleton demonstrates:

* a clean-clone bootstrap;
* locked restore;
* repository validation;
* deterministic Release build;
* Microsoft Testing Platform execution;
* unit, contract, architecture and smoke integration tests;
* Windows and platform-neutral hosted jobs;
* no-secret pull-request execution;
* minimal `GITHUB_TOKEN` permissions;
* pinned action SHAs;
* safe cache behaviour;
* test and coverage artefact publication;
* cancellation of superseded pull-request runs;
* one scheduled security and dependency run;
* one trusted self-hosted proof where required;
* one release-candidate build;
* release evidence generation;
* and promotion of the exact tested output without rebuild.

---

## 3. Context

Opure is a Windows-first, local software engineering platform with:

* a Desktop host;
* a trusted Runtime;
* Worker processes;
* Plugin Host processes;
* local IPC;
* SQLite persistence;
* an encrypted Secrets Vault;
* Windows filesystem integration;
* AI provider adapters;
* MCP integration;
* build and test execution;
* patch transactions;
* recovery workflows;
* and a public-facing Plugin SDK.

A reliable product requires more than a successful developer-machine build.

The implementation must prove that:

* a clean machine can restore and build the repository;
* package versions are not drifting;
* architecture boundaries remain enforced;
* tests run without real credentials;
* pull requests cannot access release secrets;
* Windows-specific behaviour is tested on Windows;
* platform-neutral code remains portable;
* process and persistence recovery remain intact;
* build output can be traced to source;
* and released binaries are the binaries that passed the gates.

The initial developer has a capable Windows workstation.

That workstation is also a personal development environment and must not automatically become a privileged CI runner.

Hosted CI offers clean isolation for ordinary changes.

Specialist local hardware is still useful for:

* GPU tests;
* local-model performance;
* Appium desktop automation;
* installer tests;
* soak tests;
* and reference-machine benchmarks.

The architecture must support both without allowing untrusted contribution code to run on trusted hardware.

---

## 4. Problem Statement

Opure requires a build and continuous-integration architecture that is reproducible, secure, locally runnable, Windows-capable, cost aware and able to produce trustworthy release evidence without creating CI-only behaviour, exposing secrets to untrusted code or rebuilding artefacts after they have passed validation.

---

## 5. Decision Drivers

The decision is evaluated against:

* alignment with the Opure Charter;
* developer-local reproducibility;
* clean-machine confidence;
* Windows 11 support;
* C# and .NET integration;
* `.slnx` support;
* Microsoft Testing Platform support;
* package-lock support;
* deterministic builds;
* hosted-runner isolation;
* supply-chain security;
* minimal secret exposure;
* pull-request safety;
* self-hosted runner safety;
* process and recovery testing;
* desktop and accessibility testing;
* GPU and performance testing;
* release provenance;
* build artefact promotion;
* branch protection;
* cost control;
* failure diagnosis;
* provider portability;
* small-team maintenance;
* and future public contribution.

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
* **No CI-Only Truth**
* **No Hidden Network Use**
* **No Secret Values in Build Logs**
* **No Untested Release Rebuild**
* **No Silent Test Loss**
* **No False Green Build**
* **Evidence-Based Confidence**
* **Recovery Before Convenience**

Relevant commitments include:

* the same repository scripts run locally and in CI;
* untrusted changes receive no credentials;
* CI does not silently call AI or cloud providers;
* package changes are reviewable;
* test failures remain visible;
* release evidence identifies the exact source and inputs;
* build artefacts are promoted rather than recreated;
* and optional infrastructure failure does not change source semantics.

---

## 7. Scope

This ADR decides:

* the initial CI provider;
* local build authority;
* workflow structure;
* workflow triggers;
* runner strategy;
* hosted versus self-hosted responsibilities;
* pull-request trust boundaries;
* workflow permissions;
* action pinning;
* cache policy;
* artefact policy;
* build configuration;
* restore behaviour;
* package auditing;
* test execution;
* security checks;
* nightly checks;
* release-candidate generation;
* release evidence;
* provenance;
* branch status gates;
* concurrency;
* retention;
* cost controls;
* observability;
* and migration portability.

This ADR does not decide:

* the public source hosting organisation;
* the final installer technology;
* the final updater;
* code-signing certificate provider;
* release publication destination;
* source licence;
* NuGet publication credentials;
* customer deployment;
* remote telemetry;
* enterprise CI mirrors;
* or a final self-hosted runner imaging platform.

---

## 8. Constraints

Known constraints include:

* Windows 11 is the first target platform.
* C# and .NET 10 LTS are the proposed baseline.
* The repository uses `Opure.slnx`.
* Tests use xUnit.net v3 and Microsoft Testing Platform.
* GitHub Actions hosted runner images change over time.
* Hosted Windows runners have finite CPU, memory, disk and execution time.
* Hosted jobs may not provide a visible interactive desktop suitable for all Appium scenarios.
* GPU access is not assumed on standard hosted runners.
* A self-hosted runner is not guaranteed to start clean.
* A personal workstation contains data and credentials that must not be exposed to untrusted workflows.
* Pull requests may originate from forks.
* Workflow files themselves can execute arbitrary code.
* GitHub Actions caches must be treated as untrusted input.
* Build artefacts and logs can contain sensitive paths or fixture data.
* Release credentials must not exist in ordinary validation jobs.
* Environment protection features vary by repository plan and visibility.
* Build provenance features may vary by repository plan.
* A one-person founder workflow cannot require a second human approval before one exists.
* The CI architecture must remain practical for a small team.
* Live AI provider tests may have cost and data-transfer implications.
* Performance tests are noisy on shared hosted hardware.
* Some recovery tests require process termination and real filesystems.
* Some desktop tests require a dedicated Windows session.
* The build must work without mandatory containers.
* Build scripts must not silently install machine-wide software.

---

## 9. Assumptions

This decision assumes:

* The repository will be hosted on GitHub for the initial CI implementation.
* A private repository may be used before public release.
* GitHub Actions remains available on the selected plan.
* Hosted Windows and Ubuntu images remain available.
* `actions/setup-dotnet` supports the pinned SDK.
* Standard pull-request jobs can complete on ordinary hosted hardware.
* Local scripts can isolate provider-specific details.
* The team can add one dedicated self-hosted Windows runner later if specialist tests justify it.
* Package lock files remain committed.
* NuGet Audit remains enabled.
* Public or organisation-owned release credentials can later use OIDC where supported.
* Release signing remains a separate decision.
* Ordinary build and test jobs need no secret.
* Fake local provider servers satisfy normal integration testing.
* Artefacts can be sanitised before upload.
* A release candidate can be generated without publishing it.
* The full release workflow can remain manual until packaging and signing decisions are accepted.
* Platform-neutral projects can run on Ubuntu even while the product remains Windows first.
* The founder can approve temporary security exceptions through reviewed repository changes.

---

## 10. Current Platform Evidence

Official GitHub and Microsoft documentation available on 18 July 2026 establishes that:

* GitHub-hosted jobs run on managed virtual machines and standard hosted jobs normally receive a fresh machine for each job.
* `windows-2025`, `windows-2022` and explicit Ubuntu labels are available hosted-runner choices.
* `-latest` means the latest stable image supplied by GitHub and does not necessarily mean the newest operating-system release.
* GitHub recommends minimum workflow token permissions.
* GitHub recommends full-length commit-SHA pinning as the immutable way to reference an external Action.
* GitHub supports restricting which Actions and reusable workflows can run.
* GitHub warns that untrusted pull-request code can compromise a self-hosted runner.
* GitHub dependency caches can be restored by lower-trust workflow contexts and must not contain sensitive data.
* GitHub workflow artefacts and caches have distinct purposes.
* GitHub supports concurrency groups and cancellation of superseded runs.
* GitHub environments can gate jobs and delay access to environment secrets.
* GitHub OIDC can provide short-lived identity to external systems without a stored long-lived cloud credential.
* GitHub artefact attestations can link released artefacts to source, workflow and commit information.
* .NET uses `ContinuousIntegrationBuild=true` to enable official-build behaviour such as path normalisation.
* .NET 10 can select Microsoft Testing Platform through `global.json`.
* NuGet lock files support locked restore.
* .NET 10 audits direct and transitive NuGet dependencies by default.
* NuGet audit warnings identify low, moderate, high and critical known vulnerabilities.
* package audit can be configured at repository level.
* Source Link guidance recommends deterministic builds and symbol publication for traceability.

These capabilities must be revalidated against the exact selected plan, runner images, Actions and SDK before this ADR moves to Accepted.

---

## 11. Options Considered

The principal options are:

1. **Option A — GitHub Actions with Hosted-First Runners and Local Build Scripts**
2. **Option B — Azure Pipelines with Microsoft-Hosted Agents**
3. **Option C — Self-Hosted CI Only**
4. **Option D — Local Manual Build Only**
5. **Option E — Two Hosted CI Providers**
6. **Option F — GitHub Actions with Self-Hosted Runners for All Jobs**
7. **Option G — Container-Only CI**

---

# 12. Option A — GitHub Actions with Hosted-First Runners

## 12.1 Description

Use GitHub Actions as the initial workflow orchestrator.

Run ordinary pull-request, main-branch and scheduled checks on GitHub-hosted clean virtual machines.

Use repository scripts as the actual build contract.

Reserve self-hosted hardware for trusted specialist jobs.

---

## 12.2 Advantages

* Natural integration with GitHub pull requests.
* Hosted Windows runners.
* Fresh machine for ordinary jobs.
* Simple status checks.
* First-party .NET setup support.
* Dependency caching support.
* Workflow artefacts.
* Minimal token permissions.
* OIDC.
* environment gates;
* concurrency;
* branch protection;
* release provenance attestations;
* public contribution support;
* and straightforward YAML entry point.

Additional advantages include:

* no CI server administration for ordinary jobs;
* no untrusted code on the developer workstation;
* one repository for source and workflows;
* easy cancellation of outdated pull-request work;
* and clear future migration because build logic remains in `eng/`.

---

## 12.3 Disadvantages

* Workflow syntax is GitHub specific.
* Hosted images change.
* Hosted Windows capacity is limited.
* Private repositories consume plan minutes.
* Standard runners do not represent the reference workstation.
* Interactive Appium and GPU tests need another runner.
* Workflow Actions add a supply-chain surface.
* plan capabilities differ;
* artefact retention is external;
* and GitHub outages can delay validation.

---

## 12.4 Risks

* Unpinned Action compromise.
* overly broad `GITHUB_TOKEN`;
* secrets available to untrusted code;
* unsafe `pull_request_target`;
* cache poisoning;
* self-hosted runner compromise;
* CI YAML duplicating build logic;
* hosted-image drift;
* test result loss;
* release rebuilt after tests;
* and provider lock-in.

---

## 12.5 Mitigations

* full SHA pinning;
* workflow permissions default read-only;
* no PR secrets;
* no untrusted checkout in privileged events;
* hosted-only pull-request jobs;
* cache minimisation;
* local build scripts;
* explicit runner labels;
* build manifest capture;
* artefact promotion;
* and provider-neutral evidence formats.

---

## 12.6 Estimated Adoption Cost

* **Initial implementation:** Low to Moderate
* **Operational complexity:** Low to Moderate
* **Migration difficulty:** Low
* **Replacement difficulty:** Low to Moderate

---

# 13. Option B — Azure Pipelines

## 13.1 Description

Use Azure Pipelines with Microsoft-hosted Windows and Linux agents.

---

## 13.2 Advantages

* Mature .NET and Windows integration.
* Hosted Windows agents.
* YAML pipelines.
* environments and approvals;
* enterprise governance;
* strong Microsoft ecosystem alignment;
* self-hosted agent support;
* and extensive release features.

---

## 13.3 Disadvantages

* Separate Azure DevOps organisation and identity surface.
* Pull-request experience is less direct when the repository is hosted on GitHub.
* More service configuration.
* Separate permissions and secrets.
* Build status integration requires another boundary.
* Small-team overhead is higher.
* Provider-specific tasks can spread into build logic.
* No decisive initial advantage for Opure.

---

## 13.4 Decision Relevance

Azure Pipelines remains a viable future enterprise integration.

The provider-neutral local build contract should make migration possible.

---

## 13.5 Estimated Adoption Cost

* **Initial implementation:** Moderate
* **Operational complexity:** Moderate
* **Migration difficulty:** Low to Moderate
* **Replacement difficulty:** Moderate

---

# 14. Option C — Self-Hosted CI Only

## 14.1 Description

Run all CI on Opure-controlled Windows machines.

---

## 14.2 Advantages

* Complete hardware control.
* Reference hardware.
* GPU access.
* Real Windows desktop.
* Large disk.
* Local caches.
* Custom tools.
* Potentially low provider-minute cost.
* Private network access.
* and predictable performance.

---

## 14.3 Disadvantages

* Machine administration.
* Persistent compromise risk.
* Weak clean-machine confidence.
* Exposure to untrusted contribution code.
* runner updates;
* operating-system updates;
* antivirus interference;
* queue availability;
* power and network failures;
* and personal-machine risk.

---

## 14.4 Decision

Rejected as the default.

Self-hosted runners are specialist trusted infrastructure only.

---

# 15. Option D — Local Manual Build Only

## 15.1 Description

Rely on founder-run scripts and manual validation.

---

## 15.2 Advantages

* No hosted service.
* No CI cost.
* Complete local control.
* Simple initial setup.
* No workflow supply chain.

---

## 15.3 Disadvantages

* No independent clean-machine evidence.
* Easy to skip tests.
* No pull-request status.
* No automatic architecture enforcement.
* No scheduled security audit.
* No durable run history.
* Difficult contributor trust.
* Release evidence is weak.
* Environment drift is hidden.

---

## 15.4 Decision

Rejected.

Local builds remain required but not sufficient.

---

# 16. Option E — Two Hosted CI Providers

## 16.1 Description

Run every important build on GitHub Actions and Azure Pipelines.

---

## 16.2 Advantages

* Provider-outage resilience.
* Independent environment comparison.
* Reduced workflow lock-in.
* Additional clean-machine evidence.

---

## 16.3 Disadvantages

* Double configuration.
* Double maintenance.
* Duplicate cost.
* Conflicting status checks.
* Different hosted images.
* Failure triage complexity.
* Small-team burden.
* Risk of one provider becoming ceremonial.

---

## 16.4 Decision Relevance

A second provider may later run periodic portability validation.

It is not required initially.

---

# 17. Option F — GitHub Actions with Self-Hosted Runners for All Jobs

## 17.1 Description

Use GitHub Actions orchestration but execute every job on Opure-controlled machines.

---

## 17.2 Advantages

* GitHub integration.
* Full hardware control.
* GPU and desktop support.
* Persistent caches.
* No hosted Windows minute dependency.

---

## 17.3 Disadvantages

* Retains all self-hosted compromise and cleanliness concerns.
* Fork pull requests become dangerous.
* Release secrets and machine state may be exposed.
* A workstation outage stops CI.
* No clean hosted comparison.
* Maintenance burden remains.

---

## 17.4 Decision

Rejected.

---

# 18. Option G — Container-Only CI

## 18.1 Description

Build every component inside Linux or Windows containers.

---

## 18.2 Advantages

* Image-defined environment.
* Local reproducibility.
* Isolation.
* Portable CI provider.
* Easy parallel workers.
* Explicit dependencies.

---

## 18.3 Disadvantages

* Windows desktop and named-pipe behaviour are not represented adequately.
* Windows containers add complexity.
* GPU and UI support differ.
* Build-image maintenance becomes another product.
* Docker becomes mandatory.
* Conflicts with a lightweight developer setup.
* May hide clean Windows installation problems.

---

## 18.4 Decision

Rejected as the universal build model.

Containers may support selected future tools or server components.

---

# 19. Comparison Matrix

Scores:

* **1** — poor
* **2** — weak
* **3** — acceptable
* **4** — strong
* **5** — excellent

| Criterion                     | Weight | GitHub Hosted-First | Azure Pipelines | Self-Hosted Only | Local Manual | Dual Hosted | GitHub Self-Hosted | Container Only |
| ----------------------------- | -----: | ------------------: | --------------: | ---------------: | -----------: | ----------: | -----------------: | -------------: |
| Charter alignment             |      5 |                   5 |               4 |                4 |            4 |           4 |                  4 |              3 |
| Small-team fit                |      5 |                   5 |               3 |                2 |            4 |           1 |                  2 |              2 |
| Clean-machine evidence        |      5 |                   5 |               5 |                2 |            1 |           5 |                  2 |              4 |
| Windows support               |      5 |                   5 |               5 |                5 |            5 |           5 |                  5 |              3 |
| Pull-request safety           |      5 |                   5 |               4 |                1 |            1 |           5 |                  1 |              4 |
| Local reproducibility         |      5 |                   5 |               5 |                5 |            5 |           5 |                  5 |              3 |
| Supply-chain controls         |      5 |                   5 |               4 |                3 |            2 |           5 |                  3 |              4 |
| GPU and desktop tests         |      3 |                   4 |               4 |                5 |            5 |           5 |                  5 |              2 |
| Release provenance            |      4 |                   5 |               4 |                3 |            2 |           5 |                  4 |              3 |
| Provider portability          |      3 |                   4 |               4 |                5 |            5 |           5 |                  3 |              5 |
| Maintenance burden            |      5 |                   5 |               3 |                2 |            5 |           1 |                  2 |              2 |
| Cost control                  |      4 |                   4 |               3 |                4 |            5 |           2 |                  4 |              3 |
| Contributor onboarding        |      4 |                   5 |               3 |                1 |            2 |           3 |                  1 |              2 |
| Failure diagnosis             |      4 |                   5 |               4 |                4 |            2 |           3 |                  4 |              3 |
| **Indicative weighted total** |        |             **365** |         **298** |          **242** |      **238** |     **294** |            **243** |        **240** |

GitHub Actions with a hosted-first runner strategy provides the strongest initial fit.

---

# 20. Decision

Opure will provisionally adopt:

> **GitHub Actions as the initial CI orchestrator, using clean GitHub-hosted runners for all untrusted and ordinary validation work, repository-owned local scripts as the build authority, and tightly restricted self-hosted Windows runners only for trusted specialist workloads.**

The decision applies to:

* pull-request validation;
* main-branch validation;
* scheduled security and recovery checks;
* release-candidate builds;
* test evidence;
* coverage;
* package audit;
* and future artefact provenance.

This decision does not approve:

* untrusted pull requests on self-hosted runners;
* CI secrets in pull-request jobs;
* long-lived cloud credentials when OIDC is available;
* unpinned external Actions;
* mutable tag-only Action references;
* arbitrary Marketplace Actions;
* `pull_request_target` with untrusted checkout or execution;
* build-output caching as correctness;
* release rebuild after testing;
* automatic publication;
* hidden live-provider tests;
* or CI-only product behaviour.

This decision is:

* [ ] Permanent until superseded
* [x] Provisional pending Phase 1 evidence
* [ ] Experimental only
* [ ] Limited to private repositories
* [ ] Limited to Windows permanently

---

# 21. Build Authority

The canonical build behaviour lives under:

```text
eng/
├── bootstrap.ps1
├── restore.ps1
├── build.ps1
├── test.ps1
├── verify.ps1
├── security.ps1
├── pack.ps1
├── release-candidate.ps1
├── clean.ps1
├── common/
└── README.md
```

Only scripts required by implemented milestones should exist.

---

## 21.1 CI Is an Orchestrator

Workflow YAML should:

* check out;
* set up the pinned SDK;
* restore safe caches;
* invoke an `eng/` command;
* publish results;
* and report status.

It should not independently implement:

* package version selection;
* project discovery;
* test-category definitions;
* migration logic;
* release manifest construction;
* or architecture rules.

---

## 21.2 Local Equivalence

Each required workflow stage should have a documented local command.

Example conceptual mapping:

| CI stage              | Local command                                         |
| --------------------- | ----------------------------------------------------- |
| repository validation | `pwsh ./eng/verify.ps1 -Stage Repository`             |
| locked restore        | `pwsh ./eng/restore.ps1 -Locked`                      |
| build                 | `pwsh ./eng/build.ps1 -Configuration Release`         |
| fast tests            | `pwsh ./eng/test.ps1 -Suite Fast`                     |
| security              | `pwsh ./eng/security.ps1 -Suite PullRequest`          |
| release candidate     | `pwsh ./eng/release-candidate.ps1 -Version <version>` |

---

## 21.3 No Hidden CI Flag Behaviour

`CI=true` and `GITHUB_ACTIONS=true` may enable:

* normalised build paths;
* non-interactive output;
* test-report generation;
* and stricter failure handling.

They must not change product semantics or skip required tests.

---

# 22. Build Phases

The canonical build phases are:

1. environment validation;
2. repository validation;
3. tool restore;
4. package restore;
5. dependency audit;
6. compile;
7. generated-code verification;
8. unit and contract tests;
9. architecture tests;
10. integration and process tests;
11. security and recovery tests;
12. coverage;
13. publish or package;
14. manifest generation;
15. evidence generation;
16. and artefact upload.

Not every workflow runs every phase.

---

# 23. Environment Validation

Before work begins, validate:

* operating system;
* architecture;
* SDK version;
* test platform;
* PowerShell version;
* Git version;
* available disk;
* long-path setting where relevant;
* and expected repository root.

---

## 23.1 Fail Early

A mismatched SDK or unsupported environment should fail before restore or compilation.

---

## 23.2 Environment Report

Every CI job should emit a safe environment report containing:

* runner operating system;
* runner image label;
* runner image version where exposed;
* architecture;
* .NET SDK;
* PowerShell;
* Git;
* locale;
* time zone;
* and source commit.

Do not emit all environment variables.

---

# 24. Repository Validation

Repository validation should check:

* clean solution membership;
* root build files;
* package-version policy;
* lock files;
* architecture metadata;
* generated-file consistency;
* forbidden files;
* secret scan;
* ADR numbering;
* specification naming;
* broken internal links;
* line endings;
* and build-output location.

---

## 24.1 Changed Workflow Review

Workflow changes should trigger:

* workflow syntax validation;
* Action pin validation;
* permission validation;
* and security-owner review when the team supports code ownership.

---

# 25. Restore

CI restore should use:

```powershell
dotnet restore Opure.slnx --locked-mode
```

through the repository script.

---

## 25.1 Separate Restore

Restore occurs as an explicit stage.

Build and test then use:

* `--no-restore`;
* and where appropriate `--no-build`.

---

## 25.2 Lock Files

A lock-file mismatch fails CI.

The workflow does not regenerate and commit lock files automatically.

---

## 25.3 Package Sources

Restore uses only the repository-approved `NuGet.config`.

Inherited user feeds must not alter CI.

---

## 25.4 Interactive Authentication

Ordinary CI restore is non-interactive.

Private feed authentication, when introduced, belongs to trusted workflows and requires a separate supply-chain review.

---

# 26. NuGet Audit

NuGet Audit remains enabled in CI.

Repository policy should explicitly set:

```text
NuGetAudit=true
NuGetAuditMode=all
NuGetAuditLevel=low
```

to make the intended behaviour visible.

---

## 26.1 Severity Policy

Initial policy:

* Critical known vulnerability: blocks every CI stage.
* High known vulnerability: blocks every CI stage.
* Moderate known vulnerability: warning on ordinary pull requests, blocks release candidate unless an active approved exception exists.
* Low known vulnerability: warning and tracked review.
* unavailable audit source: blocks release and security workflow; ordinary pull-request behaviour may retry before failing.

---

## 26.2 Warning Codes

The implementation may configure:

* `NU1903` and `NU1904` as errors;
* `NU1901` and `NU1902` as warnings;
* and `NU1905` as an error in dedicated security and release jobs.

The exact MSBuild policy requires prototype verification with global warnings-as-errors.

---

## 26.3 Audit Exceptions

A `NuGetAuditSuppress` entry requires:

* exact advisory URL;
* affected package;
* rationale;
* exposure analysis;
* owner;
* approval date;
* expiry;
* replacement plan;
* and release impact.

Suppressions are central and reviewed.

---

## 26.4 No Global Audit Disable

CI must not set `NuGetAudit=false`.

A temporary outage response requires a visible security exception.

---

# 27. Dependency Inventory

CI should produce a machine-readable dependency inventory containing:

* direct packages;
* transitive packages;
* resolved versions;
* source mapping;
* native assets;
* licences where available;
* and vulnerability status.

---

## 27.1 Outdated and Deprecated Packages

A scheduled job should report:

* outdated;
* deprecated;
* and vulnerable packages.

It should not update them automatically without review.

---

# 28. Tool Restore

Repository-local tools are restored with:

```powershell
dotnet tool restore
```

---

## 28.1 Tools as Code

Tool-manifest changes receive dependency review.

---

## 28.2 No Tool Auto-Update

CI does not update tool versions automatically during a run.

---

# 29. Build

CI compiles in:

```text
Release
```

configuration.

---

## 29.1 Canonical Command

Conceptually:

```powershell
dotnet build Opure.slnx `
  --configuration Release `
  --no-restore `
  -p:ContinuousIntegrationBuild=true
```

The repository script owns the exact command.

---

## 29.2 Warnings

First-party warnings fail the build.

Suppression must remain scoped and justified.

---

## 29.3 Generated Code

After build, CI verifies that code generation did not leave unexpected source-tree changes.

---

## 29.4 Dirty Tree

A release-candidate build fails if the checkout becomes unexpectedly dirty.

Ordinary pull-request jobs should also report generated-source drift.

---

# 30. Deterministic Build Policy

Build inputs should include:

* exact source commit;
* SDK version;
* package lock files;
* repository tools;
* target framework;
* Runtime Identifier where relevant;
* build properties;
* and environment manifest.

---

## 30.1 Deterministic Compiler Output

Use deterministic compiler settings.

---

## 30.2 Path Normalisation

Use `ContinuousIntegrationBuild=true` for official builds.

---

## 30.3 Source Link

Public packages and release symbols should use Source Link when release engineering is implemented.

---

## 30.4 Rebuild Comparison

A scheduled or release-gate experiment should eventually build the same source twice in clean jobs and compare expected unsigned artefact hashes.

Known nondeterministic inputs must be documented.

---

## 30.5 Signing

Code signing may introduce signed-output differences.

The unsigned payload and manifest should remain traceable to the tested build.

---

# 31. Build Once, Test Many

The preferred CI pattern is:

```text
Restore
    ↓
Build once
    ↓
Test the built outputs
    ↓
Package the same outputs
    ↓
Generate evidence
    ↓
Promote
```

---

## 31.1 No Test Rebuild

Test stages should use `--no-build` and `--no-restore` where the test-platform layout permits.

---

## 31.2 Job Boundary

When tests run in separate jobs, the compiled output should be transferred as an artefact with a manifest and hash.

A later job must not silently rebuild.

---

## 31.3 Release Promotion

Publishing consumes the release-candidate artefact.

It does not check out source and rebuild.

---

# 32. Test Execution

Tests use Microsoft Testing Platform selected through `global.json`.

---

## 32.1 Minimum Expected Tests

CI should set an expected minimum test count per suite or module where supported.

Unexpected disappearance of tests fails.

---

## 32.2 Test Results

Every test job publishes:

* machine-readable results;
* summary;
* failures;
* safe diagnostics;
* and retained artefact manifest.

---

## 32.3 No Hidden Retry

A retry does not erase the original failure.

Release-blocking suites do not use blind automatic retry.

---

# 33. Pull-Request Suite

The required pull-request suite should include:

* repository validation;
* locked restore;
* dependency audit;
* Release build;
* unit tests;
* contract tests;
* architecture tests;
* selected headless Desktop tests;
* selected Windows integration smoke;
* secret-canary smoke;
* generated-code verification;
* and managed coverage.

---

## 33.1 Duration Target

The required pull-request suite should target completion within 15 minutes under ordinary hosted-runner capacity.

This target must not remove required security checks.

---

## 33.2 No External Providers

Pull-request tests use:

* fake local AI providers;
* fake MCP servers;
* fake package endpoints where needed;
* and generated synthetic projects.

No live provider calls.

---

# 34. Main-Branch Suite

Pushes to `main` should run:

* the pull-request suite;
* broader integration tests;
* real-process topology tests;
* persistence migration smoke;
* IPC reconnect tests;
* Vault integration tests;
* filesystem security smoke;
* and release-configuration publish smoke.

---

## 34.1 Main Does Not Publish

A push to `main` does not automatically publish a product release.

---

# 35. Nightly Suite

A scheduled nightly workflow should include rotating groups such as:

* full recovery tests;
* crash injection;
* migration matrix;
* deeper security tests;
* fuzzing time budget;
* soak tests;
* Appium real-window tests;
* installer smoke;
* dependency update report;
* and selected performance trend tests.

---

## 35.1 Nightly Failure

Nightly failure creates visible status and retained evidence.

Automatic issue creation is optional and must avoid duplicate spam.

---

## 35.2 No Silent Ignore

A failing nightly security or recovery test requires triage before release.

---

# 36. Release-Candidate Suite

A release candidate should require:

* exact source commit on an allowed branch or tag;
* clean locked restore;
* clean Release build;
* complete release-blocking tests;
* architecture tests;
* security tests;
* recovery tests;
* accessibility evidence;
* packaging tests when implemented;
* dependency audit;
* licence inventory;
* SBOM;
* hashes;
* build manifest;
* and evidence bundle.

---

## 36.1 Manual Trigger

Initial release-candidate generation should use `workflow_dispatch` with typed inputs such as:

* version;
* commit or tag;
* candidate channel;
* and release notes reference.

---

## 36.2 Tag Trigger

Tag-triggered release candidates may be added after versioning and signing decisions.

---

## 36.3 No Automatic Publication

The workflow initially produces a candidate only.

Publication requires a later release and signing decision.

---

# 37. Workflow Layout

Proposed initial layout:

```text
.github/
├── actions/
│   └── README.md
├── workflows/
│   ├── ci.yml
│   ├── main.yml
│   ├── nightly.yml
│   ├── security.yml
│   └── release-candidate.yml
└── dependabot.yml                 # Only if dependency automation is later approved
```

---

## 37.1 Workflow Count

Start with the fewest workflows that provide distinct trust or schedule boundaries.

---

## 37.2 Reusable Workflows

Repository-local reusable workflows may reduce duplication.

They must not hide permissions or trust boundaries.

---

## 37.3 Composite Actions

Repository-local composite Actions may wrap repeated setup only when ordinary scripts are not sufficient.

Product build logic remains in `eng/`.

---

# 38. Workflow Triggers

## 38.1 Pull Requests

Use the normal `pull_request` event for validation.

---

## 38.2 Push

Use `push` to `main` for post-merge validation.

---

## 38.3 Schedule

Use `schedule` for nightly or weekly work.

Scheduled workflow source comes from the default branch.

---

## 38.4 Manual

Use `workflow_dispatch` for:

* release candidate;
* specialist self-hosted runs;
* performance;
* and recovery investigation.

---

## 38.5 `pull_request_target`

Do not use `pull_request_target` to check out or execute untrusted pull-request code.

A future metadata-only workflow requires security review.

---

# 39. Concurrency

Pull-request workflows should use a concurrency group based on:

* workflow name;
* and pull-request branch or number.

---

## 39.1 Cancellation

Set `cancel-in-progress: true` for superseded pull-request validation.

---

## 39.2 Main Branch

Do not cancel a main-branch release or evidence workflow merely because a later commit arrives.

Main validation may queue or use commit-specific groups.

---

## 39.3 Release

Release-candidate jobs for one version must be serialised.

---

# 40. Workflow Permissions

Every workflow starts with:

```yaml
permissions:
  contents: read
```

or a stricter empty permission set when possible.

---

## 40.1 Job-Level Escalation

Increase permissions only on the job that needs them.

---

## 40.2 Pull Request

Pull-request validation should require only read access.

It should not write:

* repository contents;
* pull requests;
* packages;
* attestations;
* or environments.

---

## 40.3 Release Candidate

A candidate job may require:

* `contents: read`;
* `id-token: write` when using OIDC or attestations;
* `attestations: write` when supported;
* and no more.

Publication permissions remain deferred.

---

## 40.4 Pull-Request Approval

CI must not approve its own pull request.

---

# 41. Action Supply-Chain Policy

Every external Action reference must be pinned to a full-length commit SHA.

Example conceptual form:

```yaml
uses: actions/checkout@<full-commit-sha> # reviewed release
```

---

## 41.1 No Mutable Tags

References such as:

```text
@main
@master
@v4
```

are prohibited in committed production workflows.

---

## 41.2 Version Comment

A comment should record the human-readable release associated with the SHA.

---

## 41.3 Action Allowlist

Initially allow only reviewed Actions required for:

* checkout;
* .NET setup;
* artefact upload and download;
* code scanning where enabled;
* and provenance attestation.

---

## 41.4 Review

Action updates require:

* source repository verification;
* release-note review;
* commit ownership verification;
* permission review;
* transitive runtime review;
* and test run.

---

## 41.5 Repository Policy

When GitHub settings permit, require SHA pinning and selected Actions at repository or organisation level.

---

# 42. Checkout Policy

Checkout should:

* use a pinned Action SHA;
* avoid persisted write credentials;
* fetch only required history;
* avoid submodules by default;
* and avoid Git LFS unless explicitly required.

---

## 42.1 Full History

Fetch full history only for:

* version derivation;
* provenance;
* change analysis;
* or release notes.

---

## 42.2 Untrusted Submodules

Pull-request validation must not initialise unapproved submodules.

---

# 43. Hosted Runner Strategy

## 43.1 Windows

Initial Windows jobs use:

```text
windows-2025
```

rather than `windows-latest`.

---

## 43.2 Why an Explicit Label

An explicit label makes operating-system generation visible.

The underlying image still receives updates.

---

## 43.3 Ubuntu

Platform-neutral compile and test jobs may use:

```text
ubuntu-24.04
```

---

## 43.4 macOS

macOS jobs are deferred until a supported product or SDK path exists.

---

## 43.5 Runner Image Capture

Record runner image metadata in the build manifest.

---

## 43.6 Hosted Runner Privilege

Hosted Windows runners may run with broad machine privileges.

Tests must still use isolated temporary roots and synthetic data.

---

# 44. Hosted Runner Workloads

Hosted runners should execute:

* repository validation;
* restore;
* build;
* unit;
* contract;
* architecture;
* most integration tests;
* process tests that do not need interactive desktop;
* persistence tests;
* secret-canary tests using synthetic secrets;
* package audit;
* coverage;
* and release-candidate compilation.

---

# 45. Self-Hosted Runner Strategy

Self-hosted runners are optional specialist infrastructure.

---

## 45.1 Permitted Workloads

Potential workloads include:

* reference-hardware benchmarks;
* GPU and VRAM tests;
* local-model integration;
* Appium real-window testing;
* multi-monitor and DPI tests;
* installer and updater tests;
* soak tests;
* and selected Windows filesystem fixtures unavailable on hosted images.

---

## 45.2 Prohibited Workloads

Self-hosted runners must not execute:

* fork pull requests;
* arbitrary unreviewed branches;
* untrusted plugin packages;
* untrusted Actions;
* or workflows whose source is not trusted.

---

## 45.3 Dedicated Machine

A self-hosted CI runner should be a dedicated machine or disposable virtual machine.

The founder's normal workstation should not be registered as a general-purpose runner.

---

## 45.4 Ephemeral Preference

Prefer ephemeral or reimaged runner instances.

Persistent runners require:

* cleanup;
* integrity checks;
* patching;
* and periodic rebuild.

---

## 45.5 Runner Account

Use a dedicated low-privilege operating-system account.

Do not run as the developer's interactive account when avoidable.

---

## 45.6 No Personal Secrets

The runner must not have:

* personal Vault access;
* browser sessions;
* SSH agents;
* Git credential helpers with write authority;
* cloud CLI sessions;
* or private project data.

---

## 45.7 Runner Groups

When organisation features support runner groups, restrict the runner to:

* the Opure repository;
* and named trusted workflows.

---

## 45.8 Specialist Labels

Example labels:

```text
self-hosted
Windows
X64
opure-trusted
opure-reference
opure-gpu
opure-desktop
```

---

# 46. Self-Hosted Cleanup

After each job:

* terminate child processes;
* clear temporary profiles;
* clear test Vaults;
* clear test databases;
* clear build output;
* clear provider fixtures;
* revoke temporary credentials;
* and verify no unexpected service remains.

---

## 46.1 Reimage Triggers

Reimage after:

* suspected compromise;
* failed cleanup;
* privileged test;
* untrusted input accident;
* runner software anomaly;
* or periodic maintenance interval.

---

# 47. Secrets in CI

Ordinary build and test workflows should use no repository secrets.

---

## 47.1 Pull Requests

Fork and ordinary pull-request validation receives no secrets.

---

## 47.2 Synthetic Secrets

Security tests generate canary secrets at runtime.

They hold no external authority.

---

## 47.3 Release Credentials

Future signing or publishing credentials are available only to a protected release job.

---

## 47.4 OIDC

Prefer OIDC for short-lived external identity when the destination supports it.

---

## 47.5 Long-Lived Tokens

A long-lived token requires:

* least privilege;
* expiry;
* rotation;
* environment restriction;
* audit;
* and separate approval.

---

## 47.6 Secret Masking

Masking is a defence in depth control.

It is not permission to print a secret.

---

# 48. Environment Gates

A future `release` environment should gate:

* signing;
* publication;
* package upload;
* and public release creation.

---

## 48.1 Required Reviewers

When another authorised reviewer exists and the repository plan supports it, prevent self-review and require approval.

---

## 48.2 Single-Founder Stage

Until a second reviewer exists, use:

* manual candidate generation;
* explicit local release checklist;
* exact commit and version confirmation;
* and no automatic publication.

---

## 48.3 Environment Secrets

Secrets become available only after the job passes environment protection.

---

# 49. Cache Policy

Caches are performance hints.

They are never correctness inputs.

---

## 49.1 NuGet Cache

The initial cache may include only the NuGet global packages folder used by `setup-dotnet`.

---

## 49.2 Cache Key

The cache key should include:

* operating system;
* architecture;
* SDK baseline;
* and hash of all `packages.lock.json` files.

---

## 49.3 No Build-Output Cache

Do not cache:

* `artifacts/bin`;
* `artifacts/obj`;
* published binaries;
* generated executables;
* test databases;
* or release candidates

for required correctness jobs.

---

## 49.4 No Sensitive Cache

Never cache:

* credentials;
* Vault files;
* signing state;
* provider responses;
* private project fixtures;
* or diagnostic bundles.

---

## 49.5 Treat as Untrusted

Restored cache contents are validated by locked restore and package hashes.

---

## 49.6 Cache Miss

Every job must work correctly with an empty cache.

---

## 49.7 Cache Poisoning

Privileged release jobs should either:

* avoid shared pull-request caches;
* use commit-specific trusted caches;
* or perform clean restore.

---

# 50. Artefact Policy

Workflow artefacts preserve outputs and evidence.

They are distinct from caches.

---

## 50.1 Pull-Request Artefacts

May include:

* test results;
* coverage;
* repository-validation report;
* architecture graph;
* and failure diagnostics.

---

## 50.2 Main Artefacts

May additionally include:

* integration reports;
* process logs;
* persistence fixtures;
* and publish smoke output.

---

## 50.3 Nightly Artefacts

May include:

* recovery reports;
* fuzz crashes;
* Appium screenshots;
* performance trend data;
* and installer logs.

---

## 50.4 Release-Candidate Artefacts

Include:

* binaries;
* symbols;
* package outputs;
* hashes;
* SBOM;
* licence inventory;
* dependency inventory;
* build manifest;
* test summary;
* evidence index;
* and provenance data.

---

# 51. Artefact Privacy

All uploaded artefacts must use:

* synthetic test data;
* path redaction;
* secret scanning;
* and bounded content.

---

## 51.1 Prohibited Uploads

Do not upload by default:

* memory dumps;
* full Windows user profiles;
* real developer databases;
* real Vault files;
* local source repositories outside the CI checkout;
* raw provider credentials;
* or unrestricted environment dumps.

---

## 51.2 Crash Dumps

A crash dump requires an explicit trusted diagnostic job and stricter retention.

---

# 52. Artefact Retention

Proposed initial retention:

* pull-request evidence: 14 days;
* main-branch evidence: 30 days;
* nightly diagnostics: 14 days;
* release candidates: 90 days until a durable release store is selected;
* published-release evidence: governed by release policy.

These values are provisional.

---

## 52.1 Failure Evidence

Failure evidence may receive a longer retention than successful pull-request artefacts when needed for triage.

---

# 53. Coverage

Managed coverage is collected through the MTP-compatible coverage decision in ADR-0008.

---

## 53.1 Pull Request

Publish coverage summary and machine-readable report.

---

## 53.2 No Percentage-Only Gate

Coverage is not the sole merge gate.

---

## 53.3 Critical Modules

Security, policy, patch, Vault and recovery code require branch and mutation evidence beyond aggregate coverage.

---

# 54. Static Analysis

CI should run:

* compiler diagnostics;
* .NET analyzers;
* repository analyzers;
* architecture tests;
* package audit;
* secret scan;
* and selected security analysis.

---

## 54.1 CodeQL

GitHub CodeQL may be enabled where available for the repository plan.

It is additive.

The core build must not depend on availability of a paid scanning feature.

---

## 54.2 Analyzer Configuration

Analyzer versions are centrally pinned.

---

# 55. Secret Scanning

A repository-owned secret scan should run before artefact upload and release candidate creation.

GitHub-native secret scanning may be enabled as an additional control when available.

---

## 55.1 Generated Files

Scan:

* source;
* workflow YAML;
* configuration;
* generated release manifests;
* test logs;
* and uploaded artefacts.

---

# 56. Dependency Review

A pull request changing package manifests or lock files should produce a focused dependency report.

---

## 56.1 Required Review

Package updates require:

* direct and transitive diff;
* vulnerability change;
* licence change;
* native asset change;
* Action change where relevant;
* and owner approval.

---

# 57. GitHub Workflow Dependency Review

The repository should treat workflow Actions as build dependencies.

---

## 57.1 Action Inventory

Generate an inventory of:

* owner;
* repository;
* full commit SHA;
* declared release;
* permissions;
* and use location.

---

# 58. Platform-Neutral Linux Job

A selected Ubuntu job should build and test:

* primitives;
* contracts;
* platform-neutral capabilities;
* public SDK;
* and architecture rules.

---

## 58.1 Purpose

This job detects accidental Windows coupling.

It does not claim Linux product support.

---

## 58.2 Windows Exclusions

Windows host, filesystem, DPAPI and desktop integration projects may be excluded intentionally.

---

# 59. Windows Job

Windows remains the authoritative product job for:

* Desktop;
* named-pipe IPC;
* process supervision;
* filesystem behaviour;
* DPAPI;
* packaging;
* and end-to-end Runtime integration.

---

# 60. Matrix Strategy

The initial matrix should remain small.

Proposed:

| Job                 | OS                | Purpose                                 |
| ------------------- | ----------------- | --------------------------------------- |
| verify              | Ubuntu or Windows | repository and dependency validation    |
| build-windows       | Windows 2025      | full product build                      |
| test-fast-windows   | Windows 2025      | required tests                          |
| test-portable       | Ubuntu 24.04      | platform-neutral leakage check          |
| integration-windows | Windows 2025      | selected process and persistence checks |

---

## 60.1 Avoid Combinatorial Explosion

Do not matrix every:

* OS;
* SDK;
* configuration;
* architecture;
* and package version.

The supported baseline is exact.

---

# 61. Architecture Tests

Architecture tests are required status checks.

They enforce ADR-0010 and related boundaries.

---

# 62. Process Tests

Hosted Windows jobs may run:

* Runtime launch;
* worker launch;
* Plugin Host launch;
* named-pipe communication;
* crash containment;
* and cancellation.

Tests must clean process trees.

---

# 63. Persistence Tests

Hosted Windows jobs should use real temporary on-disk SQLite databases.

---

## 63.1 Disk Limits

Large stress fixtures may move to nightly or self-hosted jobs.

---

# 64. Filesystem Tests

Hosted Windows jobs should cover ordinary NTFS fixture behaviour available on the image.

Specialised tests such as:

* alternate streams;
* case-sensitive directories;
* cross-volume junctions;
* and multi-user ACLs

may require trusted dedicated Windows jobs.

---

# 65. Vault Tests

DPAPI tests on hosted Windows runners use generated synthetic root keys and temporary profiles.

No production Vault material.

---

# 66. Desktop Headless Tests

Avalonia headless tests run on hosted Windows and may also run on Ubuntu for platform-neutral controls.

---

# 67. Appium Tests

Real-window Appium tests should run on a trusted dedicated Windows runner with an interactive session.

---

## 67.1 Pull Request Policy

Appium is not a required untrusted fork job.

It may run after merge, nightly or through a trusted dispatch.

---

# 68. Performance Tests

Shared hosted hardware is suitable only for:

* functional performance smoke;
* gross regression detection;
* and allocation checks.

---

## 68.1 Authoritative Benchmarks

Reference performance runs use a controlled self-hosted machine matching the approved hardware profile.

---

## 68.2 No Pull-Request Absolute Gate

Do not block every pull request on noisy absolute hosted timing.

Use relative budgets only where stable.

---

# 69. GPU and Local Model Tests

GPU and local-model tests require trusted specialist infrastructure.

---

## 69.1 Model Files

Model files must not be cached or uploaded through ordinary GitHub caches without an explicit storage policy.

---

## 69.2 No Public PR Execution

Untrusted code never receives access to local model stores or GPU runner state.

---

# 70. Fuzzing

Nightly fuzz jobs should use a bounded time budget.

---

## 70.1 Crash Corpus

A crash input becomes a reviewed regression fixture after:

* sanitisation;
* licence review;
* and size check.

---

# 71. Soak Tests

Soak tests belong on trusted scheduled hardware.

They should not block ordinary pull requests.

A failing required soak blocks release until triaged.

---

# 72. Release Candidate Build

The release candidate should be produced on a clean hosted Windows runner unless packaging requires a trusted specialist image.

---

## 72.1 Source Commit

The manifest records:

* repository;
* commit SHA;
* ref;
* workflow;
* run identifier;
* SDK;
* runner image;
* package lock digest;
* and build properties.

---

## 72.2 Candidate Contents

Candidate output should include only files intended for later signing or publication.

---

## 72.3 Hashes

Generate SHA-256 hashes for every release file and a canonical manifest hash.

---

# 73. Release Evidence Bundle

The evidence bundle should contain:

* release version;
* source commit;
* build manifest;
* environment manifest;
* package lock digest;
* dependency inventory;
* vulnerability report;
* licence report;
* test inventory;
* test results;
* coverage summary;
* architecture report;
* security report;
* recovery report;
* accessibility report;
* performance reference where required;
* binary hashes;
* SBOM;
* provenance reference;
* known exceptions;
* and approval record.

---

# 74. Software Bill of Materials

A release candidate should generate an SBOM in an approved standard such as:

* SPDX;
* or CycloneDX.

The exact generator requires a supply-chain decision.

---

## 74.1 SBOM Source

The SBOM should derive from the resolved locked package graph and packaged output.

---

## 74.2 SBOM Integrity

Hash the SBOM and include it in the release manifest.

---

# 75. Artefact Attestations

Where available, use GitHub artefact attestations for release candidates intended for distribution.

---

## 75.1 What Attestation Proves

An attestation links the artefact to build provenance.

It does not prove the software is defect free or secure.

---

## 75.2 Verification

Release documentation should include a verification path when publication begins.

---

## 75.3 Plan Limitation

If attestations are unavailable for the current private-repository plan, retain the local manifest and hashes and add attestations when the repository or plan supports them.

---

# 76. Signing Boundary

Code signing remains a later ADR.

---

## 76.1 No Rebuild During Signing

The signing workflow consumes the exact candidate files and produces signed derivatives.

---

## 76.2 Unsigned Hash

Retain the unsigned payload hash.

---

## 76.3 Signed Hash

Record the signed result hash separately.

---

# 77. Publication Boundary

Publication remains separate from build.

A publication job should:

* verify candidate manifest;
* verify evidence;
* verify approval;
* sign where required;
* publish;
* and verify remote availability.

---

# 78. Branch Protection

The `main` branch should require:

* pull request;
* required status checks;
* resolved conversations where available;
* no force push;
* and no deletion.

---

## 78.1 Status Check Names

Required job names must be unique across workflows.

---

## 78.2 Strict Versus Loose

Strict up-to-date checks may be enabled when the additional build cost is acceptable.

The initial small team should prefer correctness over avoiding a rebase build.

---

## 78.3 Administrator Bypass

Administrator bypass should be disabled where the repository plan and operating model make that practical.

Emergency bypass requires a recorded reason.

---

## 78.4 Review Count

When only the founder has write authority, CI status checks remain the practical merge gate.

When another maintainer exists, require independent review for security, release and root build changes.

---

# 79. Required Checks

Initial required checks may include:

```text
CI / Repository
CI / Build Windows
CI / Fast Tests Windows
CI / Architecture
CI / Security Smoke
CI / Portable Tests
```

Exact names should remain stable.

---

## 79.1 Aggregate Gate

An aggregate final gate may simplify branch protection, but individual job failures must remain visible.

---

# 80. Path Filters

Do not use path filters to skip a required workflow unless the branch-protection behaviour is proven safe.

A skipped required check can create ambiguity.

---

# 81. Draft Pull Requests

Draft pull requests may run a reduced suite for cost control.

Before marking ready, the full required suite must run.

---

# 82. Fork Pull Requests

Fork pull requests use:

* hosted runners;
* read-only token;
* no secrets;
* no environments;
* no self-hosted labels;
* and no publication permissions.

---

# 83. Workflow Changes from Forks

A pull request can modify workflow files but the base repository's event and permission rules remain the trust boundary.

Maintainers must review workflow changes before any privileged rerun.

---

# 84. Trusted Rerun

A maintainer rerun must not grant secrets to untrusted code unless the exact commit has been reviewed and the workflow is explicitly designed for that trust transition.

---

# 85. Release Workflow Source

A release workflow should execute the workflow definition from a trusted branch or tag.

---

# 86. Build Network Policy

Ordinary build and test jobs may access:

* GitHub;
* NuGet audit source;
* approved package sources;
* and required Actions endpoints.

---

## 86.1 Product External Calls

Tests must not contact production AI providers, MCP servers, package registries or cloud systems unless the workflow is explicitly classified External.

---

## 86.2 Network Evidence

An external integration job should record:

* destination class;
* reason;
* cost limit;
* and secret reference.

---

# 87. External Integration Tests

Live-provider tests are:

* manual or scheduled;
* trusted branch only;
* cost bounded;
* data minimised;
* and not required for every pull request.

---

## 87.1 Synthetic Payloads

Use synthetic source and prompts.

---

# 88. Build Logging

CI logs should include:

* stage;
* command;
* duration;
* exit code;
* and safe summary.

---

## 88.1 No Secret Echo

Use command invocation methods that avoid printing secret values.

---

## 88.2 No Full Environment Dump

Never run unrestricted environment-print commands in normal workflows.

---

## 88.3 Path Redaction

Public artefacts should redact hosted-runner user paths where practical.

---

# 89. Failure Diagnostics

On failure, retain:

* test results;
* safe logs;
* process tree;
* screenshot where relevant;
* fixture seed;
* and reproduction command.

---

## 89.1 Always Steps

Diagnostic upload may use `if: failure()` or equivalent.

A diagnostic upload failure must not hide the original build failure.

---

# 90. Timeouts

Every job requires a timeout.

---

## 90.1 Proposed Defaults

Provisional targets:

* repository validation: 10 minutes;
* build: 20 minutes;
* fast tests: 20 minutes;
* integration: 30 minutes;
* nightly group: 120 minutes;
* release candidate: 120 minutes;
* specialist self-hosted: explicit per suite.

---

## 90.2 No Infinite Processes

Process tests require internal deadlines and cleanup in addition to workflow timeouts.

---

# 91. Cancellation

Cancellation should propagate to:

* test processes;
* Runtime;
* workers;
* Plugin Hosts;
* builds;
* and child process trees.

---

## 91.1 Cleanup on Cancellation

Use final cleanup steps that terminate remaining child processes and remove synthetic secrets.

---

# 92. Retry Policy

Infrastructure retries are bounded.

---

## 92.1 Eligible Retry

Potentially eligible:

* package-source transient failure;
* artefact-upload transient failure;
* runner provisioning failure.

---

## 92.2 Ineligible Automatic Retry

Do not automatically hide:

* test failure;
* architecture failure;
* vulnerability finding;
* secret finding;
* migration failure;
* recovery failure;
* or build warning.

---

# 93. Flaky Tests

ADR-0008 applies.

CI should expose first failure and quarantine metadata.

---

# 94. Job Isolation

Each hosted job receives its own checkout and temporary state.

Cross-job state moves only through declared artefacts.

---

# 95. Artefact Integrity Between Jobs

An artefact passed between jobs should include:

* manifest;
* file hashes;
* producer job;
* source commit;
* and build ID.

Consumer jobs verify it.

---

# 96. Build Outputs

All generated outputs remain under:

```text
artifacts/
```

as defined by ADR-0010.

---

## 96.1 CI Upload Paths

Workflow upload paths must be explicit.

Do not upload the whole repository.

---

# 97. Cost Controls

Cost controls include:

* cancellation of superseded PR runs;
* minimal matrix;
* NuGet cache;
* no unnecessary full history;
* path-aware but safe specialist triggers;
* nightly rotation;
* and artefact retention.

---

## 97.1 No Correctness Trade

Cost does not justify:

* skipping security;
* skipping locked restore;
* reusing unsafe build output;
* or hiding flaky tests.

---

# 98. Scheduled Work Rotation

Nightly expensive suites may rotate by day:

* Monday: migrations and persistence;
* Tuesday: filesystem and recovery;
* Wednesday: plugin and MCP adversarial tests;
* Thursday: Desktop and accessibility;
* Friday: performance and soak;
* weekend: full release simulation.

The exact schedule is implementation policy.

---

# 99. CI Availability

A GitHub Actions outage delays hosted validation.

It does not prevent local build and test.

---

## 99.1 Emergency Local Evidence

An emergency local release is not approved by this ADR.

Any future emergency process requires explicit evidence and founder approval.

---

# 100. CI Provider Portability

Portability requirements include:

* build logic in `eng/`;
* standard dotnet commands;
* Cobertura or another portable coverage format;
* JUnit, TRX or another portable test result;
* JSON dependency inventory;
* SPDX or CycloneDX SBOM;
* SHA-256 manifest;
* and no product code reading GitHub-specific variables.

---

## 100.1 Provider Adapter Layer

Workflow YAML is the CI adapter.

Repository scripts are the provider-neutral build service.

---

# 101. GitHub-Specific Metadata

GitHub run and workflow identifiers may appear in build provenance.

The product runtime must not require them.

---

# 102. Migration to Another Provider

A provider migration should require only:

* new orchestration;
* credentials and environment policy;
* artefact integration;
* and status-check configuration.

It should not require rewriting build semantics.

---

# 103. Workflow Versioning

Workflow files are versioned with source.

A release candidate records the workflow commit.

---

# 104. Reusable Workflow Pinning

External reusable workflows must also be pinned and reviewed.

Repository-local reusable workflows use the current repository commit.

---

# 105. Third-Party Scripts

Do not download and execute remote scripts through:

```text
curl | shell
```

or PowerShell equivalents.

Download, verify and execute only when a reviewed tool-install process requires it.

---

# 106. Machine-Wide Changes

Hosted jobs may install temporary tools only when:

* version pinned;
* source verified;
* command visible;
* and job is disposable.

Self-hosted jobs must avoid machine-wide mutation.

---

# 107. Installer Tests

Installer tests should use disposable Windows virtual machines or a dedicated reimaged runner.

They must not install and uninstall repeatedly on the founder's daily workstation.

---

# 108. Signing Tests

Signing workflow tests use synthetic certificates until the signing ADR is accepted.

---

# 109. Release Channels

Potential channels include:

* Development;
* Technical Preview;
* Beta;
* Stable.

Channel semantics require a versioning and release ADR.

---

# 110. Candidate Naming

A candidate should include:

* version;
* commit short hash;
* and candidate sequence.

The exact format is deferred.

---

# 111. Build Manifest

A build manifest should include:

```text
format_version
product_version
source_repository
source_commit
source_ref
workflow_name
workflow_run
runner_os
runner_image
runner_architecture
dotnet_sdk
target_frameworks
runtime_identifiers
configuration
continuous_integration_build
package_lock_digests
tool_manifest_digest
build_properties_digest
output_files
output_hashes
created_utc
```

---

# 112. Test Manifest

A test manifest should include:

* suite;
* test module;
* expected test count;
* executed;
* passed;
* failed;
* skipped;
* quarantined;
* duration;
* runner;
* and result file.

---

# 113. Evidence Index

One machine-readable index should link all release evidence.

---

# 114. Build Provenance Limitations

Provenance establishes where and how an artefact was built.

It does not prove:

* correct source review;
* no vulnerability;
* no malicious dependency;
* or correct product behaviour.

---

# 115. Build Reproducibility Limitations

Deterministic compiler settings do not automatically make the complete packaged application bit-for-bit reproducible.

Potential differences include:

* signing;
* archives;
* file timestamps;
* installer metadata;
* native tools;
* and external resource generation.

---

# 116. Security Impact

## 116.1 Trust Boundaries

CI crosses:

* repository contributor to workflow;
* workflow to hosted runner;
* workflow to self-hosted runner;
* workflow to cache;
* workflow to artefact storage;
* workflow to package feed;
* workflow to release environment;
* and workflow to publication destination.

---

## 116.2 Threats

Relevant threats include:

* malicious pull request;
* malicious workflow change;
* compromised Action;
* cache poisoning;
* dependency confusion;
* vulnerable package;
* stolen `GITHUB_TOKEN`;
* release-secret exposure;
* self-hosted persistence;
* artefact substitution;
* release rebuild;
* test suppression;
* and forged status check.

---

## 116.3 Mitigations

* hosted untrusted jobs;
* minimum permissions;
* SHA-pinned Actions;
* allowlist;
* locked restore;
* package source mapping;
* package audit;
* no PR secrets;
* environment gates;
* artefact hashes;
* exact promotion;
* branch protection;
* and evidence manifests.

---

# 117. Privacy Impact

CI source, logs and artefacts leave the local machine and are stored by the hosting provider.

The repository must therefore contain only intended source and synthetic fixtures.

---

## 117.1 Private Source

A private repository still shares source with the selected CI provider.

This is an explicit repository-hosting choice, not hidden Opure product telemetry.

---

## 117.2 Test Data

Real user projects are prohibited in hosted CI.

---

# 118. Secrets Impact

CI must not access the Opure application Vault.

CI secrets belong to the CI platform or OIDC trust configuration.

---

# 119. Reliability Impact

Hosted clean runners improve environmental independence.

Hosted image changes can create new failures.

The build manifest and explicit labels provide diagnosis.

---

# 120. Performance Impact

Hosted CI validates correctness but does not replace controlled reference-hardware performance evidence.

---

# 121. Observability

CI metrics should include:

* queue time;
* duration;
* success rate;
* cancellation;
* cache hit;
* restore time;
* build time;
* test time;
* test count;
* flake count;
* artefact size;
* and self-hosted runner availability.

---

## 121.1 No Developer Scoring

CI metrics must not become individual developer productivity scores.

---

# 122. Build Health Dashboard

A future dashboard may show:

* latest main status;
* required checks;
* nightly status;
* vulnerability state;
* release-candidate state;
* and known infrastructure incidents.

---

# 123. Failure Classification

Classify failures as:

* Product;
* Test;
* Dependency;
* Runner;
* Network;
* CI Provider;
* Security Gate;
* Release Evidence;
* or Unknown.

---

# 124. Infrastructure Failure

Infrastructure failure may permit rerun.

Product and security failure requires code or policy change.

---

# 125. Audit Trail

GitHub retains workflow history according to platform policy.

Opure release evidence should additionally persist required release records outside ephemeral workflow logs once publication infrastructure exists.

---

# 126. Branch and Tag Policy

Protected branch and version-tag policy require the versioning ADR.

Until then:

* `main` is the integration branch;
* release candidates are manual;
* and tags do not automatically publish.

---

# 127. Merge Strategy

The merge strategy is not decided here.

Required status checks must run against the commit intended for merge.

---

# 128. Dependabot

GitHub Dependabot may be introduced later for:

* NuGet;
* and GitHub Actions.

Automatic merge is prohibited by default.

A dependency-automation decision is deferred.

---

# 129. Renovation Tools

No third-party dependency-update bot is selected by this ADR.

---

# 130. Code Scanning Autofix

Automated security-fix proposals may open pull requests.

They must pass ordinary review and CI.

---

# 131. Workflow Self-Test

Workflow-related repository tools should validate:

* YAML syntax;
* Action pin format;
* permission declarations;
* event trust;
* self-hosted restrictions;
* and artefact paths.

---

# 132. Policy as Code

Important CI rules should be machine checked rather than relying on reviewer memory.

---

# 133. Self-Hosted Workflow Guard

A self-hosted job should have explicit conditions proving:

* repository identity;
* event type;
* protected branch or manual dispatch;
* and actor authorisation where practical.

---

# 134. Self-Hosted Approval

Specialist self-hosted jobs should require a protected environment or equivalent explicit manual gate when secrets or privileged hardware state are present.

---

# 135. Self-Hosted Network

The runner should have only the network access required by the job.

It must not expose private local services to workflow code.

---

# 136. Reference Hardware Runner

A future reference runner matching:

* Windows 11;
* Ryzen 9 5950X;
* 32 GB RAM;
* RTX 5070 Ti 16 GB;
* local SSD

may generate authoritative Opure performance evidence.

---

## 136.1 Founder Workstation

The existing founder workstation may be used manually for benchmark evidence before a dedicated runner exists.

It must not be registered for untrusted automated jobs.

---

# 137. Performance Baseline Protection

A benchmark baseline update requires:

* source commit;
* machine state;
* tool version;
* comparison;
* and approval.

---

# 138. Build and Test Time Budgets

Proposed initial targets:

| Stage                   |           Target |
| ----------------------- | ---------------: |
| restore with warm cache |  under 2 minutes |
| repository validation   |  under 2 minutes |
| Release build           |  under 5 minutes |
| fast test suite         |  under 5 minutes |
| full PR suite           | under 15 minutes |
| main suite              | under 30 minutes |
| nightly                 |    under 4 hours |
| release candidate       |    under 2 hours |

These are goals, not guarantees.

---

# 139. Storage Budgets

Proposed initial limits:

* NuGet cache governed by GitHub cache limits;
* pull-request artefacts under 500 MiB per run;
* main artefacts under 1 GiB;
* release candidate under 2 GiB before installer and model assets;
* diagnostics uploaded only when needed.

Exact limits require measured output.

---

# 140. Large Artefacts

Local AI models are not release workflow artefacts unless a future distribution decision explicitly includes them.

---

# 141. Workflow Naming

Workflow and job names should be stable and unique.

Changing a required job name requires branch-protection update.

---

# 142. Status Summary

Every workflow should produce a concise job summary containing:

* commit;
* environment;
* stages;
* tests;
* coverage;
* dependency audit;
* artefact links;
* and failure classification.

---

# 143. No Green on Partial Failure

A job cannot mark success while an earlier required command failed.

PowerShell scripts should use strict error handling and exact exit propagation.

---

# 144. PowerShell Rules

Build scripts should use:

* strict mode;
* stop-on-error;
* explicit argument validation;
* repository-root validation;
* and safe process invocation.

---

# 145. Shell Injection

Do not build shell command strings from untrusted branch names, paths or workflow inputs.

Pass arguments as separate values.

---

# 146. Workflow Inputs

Manual workflow inputs require:

* type;
* validation;
* length bound;
* allowlist where applicable;
* and safe display.

---

# 147. Version Input

Release version input must pass the versioning policy before any build.

---

# 148. Commit Input

A release candidate must resolve the requested commit and verify it belongs to an allowed ref.

---

# 149. No Arbitrary Ref Privilege

A manual privileged workflow should not run arbitrary unreviewed commit input with secrets.

---

# 150. Release Candidate Approval Flow

Conceptual flow:

```text
Select trusted commit
    ↓
Run ordinary clean validation
    ↓
Build candidate
    ↓
Run release gates
    ↓
Generate manifest and evidence
    ↓
Approve promotion
    ↓
Sign exact candidate
    ↓
Publish exact signed output
```

Signing and publication remain deferred.

---

# 151. Release Rollback

Release rollback is a release-management decision.

CI must preserve:

* previous candidate;
* manifests;
* and source identity.

---

# 152. Build Toolchain Updates

Updates to:

* SDK;
* Actions;
* runner label;
* test platform;
* package tooling;
* or CI scripts

should be focused pull requests.

---

# 153. Hosted Image Updates

Hosted images receive updates beneath a stable label.

A sudden failure should capture the image version and compare with previous successful runs.

---

# 154. Image Qualification

Before changing from `windows-2025` to a later Windows image:

* run full main suite;
* compare dependencies;
* compare Desktop and filesystem behaviour;
* and update the ADR or implementation baseline.

---

# 155. SDK Qualification

Before changing the SDK feature band:

* run clean restore;
* full build;
* full tests;
* publish;
* deterministic comparison;
* and package audit.

---

# 156. Actions Update Qualification

Before changing an Action SHA:

* verify upstream release;
* inspect diff;
* run workflow branch;
* and confirm permissions and outputs.

---

# 157. Runner Deprecation

Runner-label deprecation requires a planned migration before GitHub removal.

---

# 158. Network Outage

Package and Action download outage should produce infrastructure failure.

The workflow must not substitute an unapproved feed.

---

# 159. Offline Local Build

After packages and tools are cached locally, the fast suite should run without external provider access.

A fully offline clean restore requires a future package-mirror decision.

---

# 160. Clean-Clone Workflow

A scheduled job should periodically disable caches and prove:

* checkout;
* tool restore;
* locked package restore;
* build;
* and fast tests

from a clean hosted machine.

---

# 161. Cacheless Release

Release candidate restore should prefer a clean or trusted cache policy.

A cache may improve performance but must not be required.

---

# 162. Source Archive

A release may include a source archive or source reference.

The source licence decision is deferred.

---

# 163. Test Result Portability

Use portable formats that can be consumed outside GitHub.

---

# 164. Coverage Portability

Use portable formats such as Cobertura.

---

# 165. SBOM Portability

Use SPDX or CycloneDX.

---

# 166. Provenance Portability

Retain a local build manifest even when GitHub attestations are generated.

---

# 167. Retention Outside GitHub

Published release evidence should eventually move to a durable release store.

GitHub workflow artefacts alone are not the permanent release archive.

---

# 168. Disaster Recovery

CI configuration is stored in Git.

The local build remains usable if GitHub Actions is unavailable.

---

# 169. CI Backup

No separate backup of transient workflow state is required.

Release evidence requires durable retention.

---

# 170. Repository Transfer

A GitHub repository transfer may change:

* OIDC claims;
* Action permissions;
* environment configuration;
* branch protection;
* and attestation identity.

A transfer requires CI trust review.

---

# 171. Repository Visibility Change

Changing private to public affects:

* fork workflows;
* cache exposure;
* self-hosted risk;
* hosted runner allocation;
* artefact visibility;
* and attestation transparency.

It requires a security review before the change.

---

# 172. Public Contributions

Before accepting public contributions:

* validate fork PR isolation;
* remove all PR secrets;
* prohibit self-hosted PR jobs;
* document build;
* configure branch protection;
* and verify workflow Action policies.

---

# 173. Private Contributions

Private repository contributors can still submit malicious code.

Self-hosted protections remain necessary.

---

# 174. Build Scripts as Trusted Code

Changes under `eng/`, root build files and workflows are security-sensitive.

---

## 174.1 Required Review Areas

The following should require security or build-owner review when team size permits:

* `.github/workflows`;
* `.github/actions`;
* `eng/`;
* `global.json`;
* `Directory.Build.*`;
* `Directory.Packages.props`;
* `NuGet.config`;
* tool manifests;
* source generators;
* and packaging scripts.

---

# 175. Release Evidence Signature

A future release-evidence index may be signed separately from binaries.

The signing method is deferred.

---

# 176. Security Exceptions

A CI security exception requires:

* exact rule;
* scope;
* risk;
* owner;
* expiry;
* mitigation;
* and founder approval.

---

# 177. Test Exceptions

A skipped release gate requires:

* reason;
* affected release;
* evidence;
* owner;
* and founder approval.

Critical security, secret or data-integrity tests should not be skipped.

---

# 178. Incident Response

If CI credentials or a runner are compromised:

1. disable affected workflow or runner;
2. revoke credentials;
3. rotate tokens;
4. invalidate or quarantine artefacts;
5. inspect workflow history;
6. reimage self-hosted runners;
7. rebuild candidates from trusted source;
8. record incident;
9. and review affected releases.

---

# 179. Artefact Quarantine

An artefact produced by an untrusted or compromised runner is not promotable.

---

# 180. Build Failure Does Not Mutate Source

CI does not push automatic fixes from ordinary validation.

It may upload a proposed patch as an artefact later, but merge remains reviewable.

---

# 181. Automated Formatting

CI checks formatting.

It does not commit formatting changes automatically.

---

# 182. Automated Dependency Updates

A bot may propose package updates later.

CI does not merge them automatically.

---

# 183. Automated Versioning

The release candidate version source remains a later decision.

CI does not infer a public release version from arbitrary branch state.

---

# 184. Test Sharding

Test sharding may be introduced when suite duration warrants it.

---

## 184.1 Deterministic Partition

Sharding should use deterministic module or category boundaries.

---

## 184.2 Result Merge

A missing shard fails the aggregate gate.

---

# 185. Parallelism

Hosted jobs may run in parallel when they do not share mutable state.

Self-hosted specialist jobs should use controlled concurrency.

---

# 186. Exclusive Resources

Jobs requiring:

* one desktop session;
* one GPU;
* installer state;
* or one performance machine

must use exclusive concurrency groups.

---

# 187. Service Containers

No service container is required initially.

Fake provider servers should run as test processes.

---

# 188. Containers

A future service test may use a container only if:

* supported on the runner;
* version pinned;
* image digest pinned;
* and no simpler local process exists.

---

# 189. Native Dependencies

CI should inventory native binaries restored through NuGet.

Windows and Linux jobs should validate supported runtime assets.

---

# 190. Architecture of CI Scripts

Suggested internal script modules:

```text
eng/common/
├── Repository.psm1
├── DotNet.psm1
├── Processes.psm1
├── Results.psm1
├── Security.psm1
├── Artifacts.psm1
└── ReleaseEvidence.psm1
```

Create only when repeated behaviour justifies modules.

---

# 191. Script Testing

Complex build script functions require tests.

Repository validation tools should prefer compiled tested .NET utilities when logic becomes substantial.

---

# 192. Workflow YAML Size

A workflow file should remain readable.

If it becomes large, move product logic to scripts and repeated orchestration to a local reusable workflow.

---

# 193. YAML Anchors

Use YAML features only when supported and clear.

Avoid clever indirection that obscures permissions.

---

# 194. Environment Variables

CI-specific environment variables should use clear names such as:

```text
CI
OPURE_BUILD_CONFIGURATION
OPURE_ARTIFACTS_PATH
OPURE_TEST_SUITE
OPURE_RELEASE_VERSION
```

No secret value belongs in an `OPURE_` non-secret variable.

---

# 195. Working Directory

Every script discovers and validates the repository root.

No script assumes `C:\Opure` in CI.

---

# 196. Long Paths

Windows jobs use the repository's long-path-aware executables.

The checkout path should remain reasonably short.

---

# 197. Source Checkout Path

Self-hosted Windows runners should use a short work root.

Hosted runner paths are recorded but not hardcoded.

---

# 198. Test Profile Isolation

Every CI test run uses a generated Opure profile root under the job temporary directory.

---

# 199. No Production Profile

CI must not resolve the ordinary user's Opure data root.

---

# 200. Process Cleanup

Windows jobs should verify no Opure host, worker or Plugin Host process remains after tests.

---

# 201. File Cleanup

Temporary test workspaces and synthetic Vaults should be removed after results are captured.

---

# 202. Disk Pressure

Before large nightly or release tests, validate free disk.

---

# 203. Artefact Compression

Compression should not hide file identities or hashes.

Hash individual files before archive creation and hash the archive separately.

---

# 204. Archive Reproducibility

Reproducible archive timestamps and ordering are a future release-engineering goal.

---

# 205. Release Candidate Verification

A separate job should:

* download candidate;
* verify manifest;
* verify hashes;
* inspect file inventory;
* run smoke launch where possible;
* and produce verification result.

---

# 206. Desktop Launch Smoke

A release candidate should launch on a clean Windows environment and reach a safe readiness state.

---

# 207. Runtime Launch Smoke

The Runtime should:

* start;
* bind local IPC;
* report readiness;
* and stop cleanly.

---

# 208. No Cloud Requirement

Release smoke must not require a cloud provider credential.

---

# 209. Safe Mode Smoke

Release acceptance should prove Safe Mode startup.

---

# 210. Recovery Mode Smoke

Release acceptance should prove Recovery Mode entry.

---

# 211. Build Compatibility Matrix

The repository should maintain an explicit supported matrix:

* SDK;
* Windows image;
* architecture;
* Desktop framework;
* test platform;
* and package baseline.

---

# 212. Unsupported Matrix

CI should not silently test unsupported combinations and imply support.

---

# 213. Windows ARM64

Windows ARM64 compilation or tests may be added later.

The current product baseline remains x64 until a platform ADR says otherwise.

---

# 214. Linux Build

The Ubuntu job proves selected assemblies remain platform neutral.

It does not produce a supported Linux application release.

---

# 215. macOS Build

Deferred.

---

# 216. CI Documentation

`docs/development/continuous-integration.md` should explain:

* workflows;
* local equivalents;
* trust levels;
* required checks;
* artefacts;
* self-hosted rules;
* and troubleshooting.

---

# 217. Workflow Badge

A README badge may show main build status after the workflow is stable.

It should not imply release quality from one check alone.

---

# 218. Contributor Guidance

`CONTRIBUTING.md` should explain:

* run fast suite;
* do not add secrets;
* package changes require review;
* and self-hosted jobs are maintainer controlled.

---

# 219. Failure Reproduction

CI summaries should include one local reproduction command.

---

# 220. Runner-Specific Failure

If a failure cannot reproduce locally, capture:

* image version;
* installed SDKs;
* environment report;
* and artefacts.

---

# 221. Build Provenance Verification

Before public release, test the documented verification command on a clean machine.

---

# 222. Acceptance Criteria

This ADR may move to **Accepted** when:

* [ ] GitHub Actions is configured for the repository.
* [ ] The build remains runnable without GitHub Actions.
* [ ] `eng/` scripts are the canonical build contract.
* [ ] Workflow YAML invokes repository scripts.
* [ ] `Opure.slnx` is the complete solution.
* [ ] `global.json` pins the SDK and MTP runner.
* [ ] CI uses Release configuration.
* [ ] `ContinuousIntegrationBuild=true` is active.
* [ ] Locked restore succeeds.
* [ ] Lock mismatch fails.
* [ ] NuGet Audit is enabled.
* [ ] Direct and transitive packages are audited.
* [ ] High and Critical vulnerabilities fail CI.
* [ ] Moderate vulnerabilities block release without an approved exception.
* [ ] Audit-source failure blocks the security and release workflow.
* [ ] Package sources use repository policy.
* [ ] Local tools restore from the manifest.
* [ ] No required global .NET tool exists.
* [ ] Build output remains under `artifacts/`.
* [ ] Generated-code drift fails.
* [ ] A dirty release checkout fails.
* [ ] Tests execute through MTP.
* [ ] Missing expected tests fail.
* [ ] Test results are published.
* [ ] Coverage is published.
* [ ] Architecture tests are required.
* [ ] Secret-canary smoke is required.
* [ ] Pull-request jobs use GitHub-hosted runners.
* [ ] Fork pull requests receive no secrets.
* [ ] Pull-request jobs have read-only permissions.
* [ ] `pull_request_target` does not execute untrusted code.
* [ ] Every external Action is pinned to a full commit SHA.
* [ ] Action allowlist validation exists.
* [ ] Checkout does not persist write credentials.
* [ ] Pull-request concurrency cancels superseded runs.
* [ ] Main and release runs are not incorrectly cancelled.
* [ ] `windows-2025` is the explicit Windows hosted image.
* [ ] runner image metadata is recorded.
* [ ] selected platform-neutral tests run on Ubuntu.
* [ ] a cache miss does not affect correctness.
* [ ] build outputs are not used as an untrusted shared cache.
* [ ] sensitive data is absent from caches.
* [ ] uploaded artefacts are bounded and scanned.
* [ ] release candidate uses the exact tested output.
* [ ] release publication does not rebuild.
* [ ] candidate files have SHA-256 hashes.
* [ ] build manifest is generated.
* [ ] dependency inventory is generated.
* [ ] licence inventory is generated.
* [ ] SBOM is generated.
* [ ] release evidence index is generated.
* [ ] provenance attestation is generated where available or an explicit limitation is recorded.
* [ ] `main` has required status checks.
* [ ] force push and deletion are disabled.
* [ ] job names are unique.
* [ ] nightly workflow runs.
* [ ] clean cacheless build runs periodically.
* [ ] self-hosted runner jobs are trusted-only.
* [ ] self-hosted runner contains no personal credentials.
* [ ] process cleanup is verified.
* [ ] workflow logs contain no synthetic canary secret.
* [ ] CI duration and storage are measured.
* [ ] security review is complete.
* [ ] founder approval is recorded.

---

# 223. Evidence Required Before Acceptance

* [ ] local clean-clone build report;
* [ ] GitHub Actions workflow review;
* [ ] hosted Windows build report;
* [ ] hosted Ubuntu portable-test report;
* [ ] locked restore report;
* [ ] NuGet audit report;
* [ ] action pin inventory;
* [ ] permission review;
* [ ] fork pull-request security test;
* [ ] cache-miss test;
* [ ] cache-poisoning review;
* [ ] test-result and coverage report;
* [ ] architecture-test report;
* [ ] secret-canary report;
* [ ] process-cleanup report;
* [ ] nightly workflow report;
* [ ] self-hosted trust proof if introduced;
* [ ] release-candidate report;
* [ ] artefact hash manifest;
* [ ] SBOM;
* [ ] release evidence bundle;
* [ ] provenance result or plan limitation;
* [ ] branch-protection review;
* [ ] build duration report;
* [ ] storage and cost report;
* [ ] security review;
* [ ] founder approval.

---

# 224. Known Limitations

* The GitHub repository and organisation configuration is not yet documented here.
* The exact .NET SDK feature band is not yet pinned.
* Exact Action SHAs are not listed in this ADR.
* The exact GitHub plan and private-repository feature availability are not fixed.
* Hosted runner images change under stable labels.
* Standard hosted runners do not represent the reference workstation.
* GPU tests require specialist infrastructure.
* Appium real-window tests may require self-hosted Windows.
* Self-hosted ephemeral imaging is not yet implemented.
* The founder's workstation is not a dedicated runner.
* CI package caches are externally stored by GitHub.
* Permanent release-evidence storage is not selected.
* Code signing is deferred.
* Product publication is deferred.
* Installer creation is deferred.
* SBOM generator is not selected.
* CodeQL availability may depend on repository plan.
* Artifact attestation availability may depend on repository visibility and plan.
* Required environment reviewers may not be practical for a one-person team.
* Fully offline clean restore is not supported without a package mirror.
* Complete bit-for-bit reproducibility is not yet proven.
* Performance gates on hosted runners remain limited.
* Linux and macOS product support is not implied.
* A second CI provider is not configured.
* Live provider integration tests are deferred.

---

# 225. Open Questions

* Which GitHub account or organisation will own the repository?
* Will the repository initially remain private?
* Which GitHub plan will be used?
* Which exact .NET 10 SDK feature band should be pinned?
* Which full commit SHAs should pin each Action?
* Should `windows-2025` remain the initial Windows image?
* Should a `windows-2022` compatibility job exist?
* Which platform-neutral projects should run on Ubuntu?
* Should an Ubuntu job be required on every pull request?
* Which workflow jobs should be required branch checks?
* Should branch protection require strict up-to-date checks?
* How should founder-only review work before a second maintainer exists?
* Which workflow changes require mandatory security approval?
* Should a repository-level Action allowlist be configured immediately?
* Should all Actions, including GitHub-owned Actions, require SHA pinning through repository policy?
* Should CodeQL be enabled immediately?
* Which local secret-scanning tool should be selected?
* Should GitHub-native dependency review be enabled?
* Should Dependabot update NuGet and Actions?
* How should dependency updates be grouped?
* What should the moderate-vulnerability exception process be?
* Which audit source should be declared explicitly?
* Should `NU1905` fail every pull request or only security and release jobs?
* Which MTP result logger and report formats should be canonical?
* What minimum expected test count should each suite enforce?
* Should compiled outputs pass between jobs or should build and tests remain in one job initially?
* How should artefact manifests be signed?
* Which SBOM generator should be selected?
* Which SBOM format should be canonical?
* What permanent release-evidence store should be used?
* When should artefact attestations become required?
* What code-signing provider should be used?
* Should OIDC authenticate to the signing provider?
* Which release environment protections are available on the selected plan?
* How should release candidate versioning work?
* Should release candidates be tag or manual triggered?
* Should nightly failures open issues automatically?
* Which nightly suites should rotate?
* Should a dedicated self-hosted machine be purchased?
* Should the reference workstation be used only for manual benchmarks?
* How should an ephemeral Windows runner be provisioned?
* Should self-hosted jobs run inside disposable VMs?
* Which GPU and model fixtures are required?
* How should model files be supplied to a trusted runner?
* What Appium runner state is required?
* Which Windows installer tests need elevation?
* Which CI artefacts are safe to upload from security failures?
* What artefact retention should ship?
* What cache-size budget is acceptable?
* What monthly CI cost is acceptable?
* Should draft pull requests receive the full suite?
* Which failure classes are eligible for automatic rerun?
* How should GitHub outage exceptions be handled?
* When should Azure Pipelines run a portability build?
* What evidence would justify migration to another provider?
* Should a public repository visibility change require a new ADR?

---

# 226. Deferred Decisions

This ADR intentionally defers:

* versioning to a Versioning ADR;
* packaging to a Packaging ADR;
* installer technology to an Installer ADR;
* updater technology to an Update ADR;
* code signing to a Signing ADR;
* publication destination to a Release Distribution ADR;
* SBOM generator to a Supply-Chain Tooling ADR;
* permanent release-evidence storage to a Release Records ADR;
* Dependabot or another dependency bot to a Dependency Automation ADR;
* dedicated self-hosted runner provisioning to a Build Infrastructure ADR;
* performance-lab management to a Performance Engineering ADR;
* enterprise Azure Pipelines integration to a future Enterprise Build ADR;
* offline package mirror to an Enterprise Supply-Chain ADR;
* and public contribution governance to a Contribution Governance ADR.

---

# 227. Alternatives Rejected

## 227.1 Azure Pipelines as the Initial Provider

Not selected because GitHub Actions provides the more direct initial repository and pull-request integration without a second identity and configuration surface.

---

## 227.2 Self-Hosted CI for Every Job

Rejected because untrusted contribution code and persistent machine state create unacceptable risk and weak clean-machine evidence.

---

## 227.3 Local Manual Validation Only

Rejected because release and contribution confidence requires independent automated evidence.

---

## 227.4 Two Hosted Providers from the Beginning

Rejected because duplicate orchestration is disproportionate for the founder-led first implementation.

---

## 227.5 GitHub Self-Hosted Only

Rejected because it retains the CI provider but loses the main isolation benefit of hosted clean machines.

---

## 227.6 Container-Only Builds

Rejected because Windows desktop, named-pipe, filesystem, DPAPI and installer behaviour require real Windows validation.

---

## 227.7 Workflow YAML as the Build Implementation

Rejected because it would create CI-only behaviour and make provider migration difficult.

---

## 227.8 Mutable Action Tags

Rejected because a tag can move and does not provide immutable workflow dependency identity.

---

## 227.9 Shared Build-Output Cache

Rejected because stale or poisoned compiled output must not become a correctness input.

---

## 227.10 Automatic Release on Main Push

Rejected because publication requires explicit versioning, signing, evidence and human control.

---

# 228. Official Evidence References

The following official sources informed this ADR:

## GitHub Actions

* [GitHub-hosted runners reference](https://docs.github.com/en/actions/reference/runners/github-hosted-runners)
* [GitHub Actions runners](https://docs.github.com/en/actions/concepts/runners)
* [Building and testing .NET with GitHub Actions](https://docs.github.com/en/actions/tutorials/build-and-test-code/net)
* [Dependency caching](https://docs.github.com/en/actions/concepts/workflows-and-actions/dependency-caching)
* [Dependency caching reference](https://docs.github.com/en/actions/reference/workflows-and-actions/dependency-caching)
* [Workflow artefacts](https://docs.github.com/en/actions/concepts/workflows-and-actions/workflow-artifacts)
* [Secure use reference](https://docs.github.com/en/actions/reference/security/secure-use)
* [Using `GITHUB_TOKEN`](https://docs.github.com/en/actions/tutorials/authenticate-with-github_token)
* [`GITHUB_TOKEN` concepts](https://docs.github.com/en/actions/concepts/security/github_token)
* [GitHub Actions permissions](https://docs.github.com/en/rest/actions/permissions)
* [Self-hosted runners](https://docs.github.com/en/actions/concepts/runners/self-hosted-runners)
* [Adding self-hosted runners](https://docs.github.com/en/actions/how-tos/manage-runners/self-hosted-runners/add-runners)
* [Self-hosted runner reference](https://docs.github.com/en/actions/reference/runners/self-hosted-runners)
* [Managing self-hosted runner groups](https://docs.github.com/en/actions/how-tos/manage-runners/self-hosted-runners/manage-access)
* [Concurrency](https://docs.github.com/en/actions/concepts/workflows-and-actions/concurrency)
* [Controlling workflow concurrency](https://docs.github.com/actions/writing-workflows/choosing-what-your-workflow-does/control-the-concurrency-of-workflows-and-jobs)
* [Deployments and environments](https://docs.github.com/en/actions/reference/workflows-and-actions/deployments-and-environments)
* [Managing environments](https://docs.github.com/en/actions/how-tos/deploy/configure-and-manage-deployments/manage-environments)
* [OpenID Connect](https://docs.github.com/en/actions/reference/security/oidc)
* [Artifact attestations](https://docs.github.com/en/actions/concepts/security/artifact-attestations)
* [Using artifact attestations](https://docs.github.com/en/actions/how-tos/secure-your-work/use-artifact-attestations/use-artifact-attestations)
* [Protected branches](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches)

## .NET and NuGet

* [`dotnet test`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test)
* [Testing with `dotnet test`](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test)
* [Microsoft Testing Platform overview](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-intro)
* [.NET SDK MSBuild properties](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props)
* [NuGet PackageReference lock files](https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files)
* [Auditing NuGet dependencies](https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages)
* [NuGet warnings NU1901–NU1904](https://learn.microsoft.com/en-us/nuget/reference/errors-and-warnings/nu1901-nu1904)
* [.NET 10 transitive NuGet audit change](https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/10.0/nugetaudit-transitive-packages)
* [NuGet configuration](https://learn.microsoft.com/en-us/nuget/reference/nuget-config-file)
* [`dotnet package list`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-package-list)
* [Source Link guidance](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/sourcelink)

## Alternative Provider Evidence

* [Azure Pipelines documentation](https://learn.microsoft.com/en-us/azure/devops/pipelines/)
* [Azure Pipelines agents](https://learn.microsoft.com/en-us/azure/devops/pipelines/agents/agents)
* [Microsoft-hosted Azure Pipelines agents](https://learn.microsoft.com/en-us/azure/devops/pipelines/agents/hosted)
* [Self-hosted Windows Azure Pipelines agent](https://learn.microsoft.com/en-us/azure/devops/pipelines/agents/windows-agent)

Platforms, plans, image labels, Action versions and security features can change.

The implementation team must verify all selected versions, runner labels, repository settings and plan capabilities before acceptance.

---

# 229. Review Record

| Date         | Reviewer           | Decision | Notes                                                                        |
| ------------ | ------------------ | -------- | ---------------------------------------------------------------------------- |
| 18 July 2026 | Architecture draft | Proposed | Provider-neutral local build with GitHub Actions hosted-first CI recommended |

---

# 230. Approval

## Founder or Product Approval

* **Name:** Christopher Dyer
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Pending founder review

## Build and CI Approval

* **Name or role:** Build and Continuous Integration Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Hosted workflows, local equivalence and release-candidate proof required

## Security Approval

* **Name or role:** Security Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Fork isolation, Action pinning, permissions, cache, self-hosted and secret review required

## Test Architecture Approval

* **Name or role:** Test Architecture Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Suite mapping, MTP results, flaky handling and minimum test counts required

## Release Engineering Approval

* **Name or role:** Release Engineering Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** candidate promotion, evidence, SBOM, hashes and provenance required

---

# 231. Supersession

This ADR is superseded only when a later ADR:

* names ADR-0011 explicitly;
* explains why the build contract, CI provider or runner strategy changed;
* identifies affected workflows and trust boundaries;
* describes migration of branch checks, secrets and release evidence;
* explains contributor and release impact;
* and updates the `Superseded by` field.

Historical ADRs remain in version control.

---

# 232. Change History

| Version | Date         | Author        | Summary                                                                                |
| ------- | ------------ | ------------- | -------------------------------------------------------------------------------------- |
| 0.1     | 18 July 2026 | Founder Draft | Initial provider-neutral local build and GitHub Actions hosted-first CI recommendation |

---

# 233. Final Decision Statement

> **Opure will provisionally use repository-owned PowerShell and standard .NET commands as the authoritative local build contract, with GitHub Actions as the initial continuous-integration orchestrator, clean hosted Windows and Ubuntu runners for untrusted and ordinary validation, tightly restricted self-hosted Windows runners only for trusted specialist workloads, locked and audited dependencies, minimum workflow permissions, immutable Action references, bounded caches and artefacts, and promotion of the exact tested release candidate without rebuilding, because trustworthy engineering automation requires reproducible local behaviour, isolated contribution checks, visible supply-chain inputs and release evidence tied to the source that actually passed validation.**

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**
