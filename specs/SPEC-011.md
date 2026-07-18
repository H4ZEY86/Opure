# SPEC-011 — Project and Build Management

## Opure Platform Project Lifecycle, Build, Test, Repository and Dependency Operations

**Document:** SPEC-011  
**Status:** Founder Draft  
**Version:** 0.1  
**Language:** British English  
**Last updated:** 18 July 2026  
**Depends on:** CHARTER-001, SPEC-001, SPEC-002, SPEC-003, SPEC-004, SPEC-005, SPEC-006, SPEC-007, SPEC-008, SPEC-009, SPEC-010  

---

## 1. Purpose

This specification defines Project and Build Management for the Opure Platform.

It establishes how Opure:

- creates, imports, opens, closes and removes projects;
- preserves stable project identity;
- detects languages, frameworks and build systems;
- manages project profiles;
- discovers and executes builds;
- discovers and executes tests;
- handles package managers and dependencies;
- coordinates repository operations;
- manages project environments;
- records artefacts and validation evidence;
- prepares commits and releases;
- handles failures safely;
- and presents all significant engineering operations through the Trust Centre and desktop interface.

This specification brings together the Project Manager, Build Manager, Repository Service and related adapters defined in SPEC-003.

---

## 2. Founding Rule

> **Project automation must remain explicit, inspectable and subordinate to the developer's intent.**

Opure may:

- detect;
- propose;
- prepare;
- validate;
- and automate within approved limits.

Opure must not silently:

- run arbitrary project scripts;
- install dependencies;
- modify repository history;
- publish packages;
- push commits;
- change branches;
- alter environment secrets;
- or treat a detected command as trusted merely because it is conventional.

---

## 3. Relationship to Other Services

A simplified logical view is:

```text
Developer / Desktop
        │
        ▼
Desktop Gateway
        │
        ├── Project Manager
        ├── Build Manager
        ├── Repository Service
        ├── Dependency Manager
        ├── Environment Manager
        ├── Artefact Manager
        └── Release Coordinator
                │
                ├── Workspace Service
                ├── Patch Service
                ├── Policy Engine
                ├── Secrets Vault
                ├── Network Gateway
                ├── Scheduler
                ├── CLI Adapter Host
                ├── Workflow Engine
                ├── Knowledge Engine
                ├── Trust Centre
                └── Plugin and MCP Gateways
```

The Project Manager owns project identity and lifecycle.

The Build Manager owns build and test execution records.

The Repository Service owns version-control operations.

The Dependency Manager owns dependency discovery, proposed changes and package-manager coordination.

The Environment Manager owns non-secret environment metadata and controlled execution environments.

The Artefact Manager owns build artefact metadata and retention.

The Release Coordinator owns release preparation workflows, not external publication authority.

---

## 4. Design Goals

Project and Build Management must be:

- project-scoped;
- deterministic where possible;
- explicit;
- inspectable;
- cancellable;
- reproducible;
- environment-aware;
- repository-aware;
- safe around scripts;
- secure around credentials;
- recoverable;
- and independent from any one language or build system.

It should support multiple ecosystems through adapters rather than embedding every tool into the core.

---

## 5. Non-Goals

This specification does not require Opure to:

- replace every IDE;
- replace all package managers;
- host source repositories;
- provide a public CI service;
- publish releases automatically;
- create cloud infrastructure;
- guarantee reproducible builds where the project itself is not reproducible;
- or understand every build system in the first release.

The first implementation may support a limited set of common ecosystems.

---

## 6. Normative Language

The terms **MUST**, **MUST NOT**, **SHOULD**, **SHOULD NOT**, **MAY** and **DEFERRED** have the meanings defined in SPEC-001.

Any intentional violation of a **SHOULD** requirement must be documented in an Architecture Decision Record.

---

# 7. Core Concepts

## 7.1 Project

A Project is an Opure-managed engineering workspace with stable identity, settings, policy, memory and lifecycle.

---

## 7.2 Project Profile

A Project Profile describes detected and configured project characteristics.

It may include:

- languages;
- frameworks;
- runtimes;
- build systems;
- test systems;
- package managers;
- repository type;
- deployment targets;
- and project conventions.

---

## 7.3 Build Profile

A Build Profile defines one controlled build operation.

---

## 7.4 Test Profile

A Test Profile defines one controlled test operation.

---

## 7.5 Command Plan

A Command Plan is a structured, reviewable description of an external process invocation.

---

## 7.6 Execution Environment

An Execution Environment defines:

- working directory;
- environment variables;
- secret references;
- runtime paths;
- tool versions;
- resource limits;
- and isolation rules.

---

## 7.7 Dependency Record

A Dependency Record identifies one declared, resolved or observed dependency.

---

## 7.8 Artefact

An Artefact is an output produced by a build, test, packaging or release-preparation process.

---

## 7.9 Release Plan

A Release Plan is a reviewable workflow describing versioning, validation, packaging, notes and publication steps.

---

# 8. Project Lifecycle

## 8.1 Project States

A project should use states such as:

- Discovered;
- Registering;
- Registered;
- Opening;
- Open;
- Degraded;
- Closing;
- Closed;
- Missing;
- Recovery Required;
- Removing;
- and Removed.

---

## 8.2 Project Creation

Project creation may include:

- creating a new empty workspace;
- creating from a template;
- cloning an existing repository;
- importing an existing folder;
- or creating from a pattern or workflow.

---

## 8.3 Project Import

Import must inspect:

- workspace path;
- repository state;
- detected languages;
- build systems;
- package managers;
- ignore rules;
- likely sensitive files;
- and existing Opure metadata.

---

## 8.4 Project Open

Opening a project must:

1. validate project identity;
2. validate workspace roots;
3. load project settings;
4. load project policy;
5. restore safe project state;
6. initialise file watching;
7. inspect repository state;
8. inspect build profile state;
9. initialise memory namespace;
10. and report readiness or degradation.

---

## 8.5 Project Close

Closing a project should:

- stop new project tasks;
- pause or complete active workflows safely;
- flush project state;
- release file watchers;
- close project-scoped plugin sessions;
- and preserve resumable state.

---

## 8.6 Project Removal

Removing a project from Opure must not delete the workspace by default.

It should offer separate choices for:

- unregister project only;
- remove project memory;
- remove cached artefacts;
- remove local Opure metadata;
- and delete the workspace, requiring stronger confirmation.

---

# 9. Stable Project Identity

## 9.1 Identifier

Every project must have a stable internal identifier independent from path.

---

## 9.2 Metadata Location

Project identity metadata may be stored:

- inside the project;
- in user-level Opure storage;
- or in both through a reconciled design.

The final approach is DEFERRED to an ADR.

---

## 9.3 Moved Projects

A moved project should retain identity where evidence supports it.

---

## 9.4 Duplicate Projects

If the same project is registered from multiple paths, Opure must identify possible duplication rather than merge silently.

---

## 9.5 Cloned Projects

A cloned project should normally receive a distinct Opure project identity even when repository history is shared.

---

# 10. Project Profiles

## 10.1 Detection

Project Profile detection may inspect:

- file extensions;
- manifests;
- lock files;
- build scripts;
- repository files;
- configuration;
- framework markers;
- and developer-supplied metadata.

---

## 10.2 Evidence

Every detected characteristic should identify its evidence.

---

## 10.3 Confidence

Detection should distinguish:

- confirmed;
- strongly indicated;
- inferred;
- developer-declared;
- and unknown.

---

## 10.4 Manual Override

The developer must be able to correct or override detected profile information.

---

## 10.5 Profile Refresh

Profiles should refresh when relevant files change.

---

# 11. Supported Ecosystem Model

The architecture should support adapters for ecosystems such as:

- Python;
- Node.js and JavaScript;
- TypeScript;
- .NET;
- Java;
- Rust;
- Go;
- C and C++;
- PHP;
- Ruby;
- and others.

The initial implementation may support fewer.

Adapters must remain optional and versioned.

---

# 12. Build System Discovery

## 12.1 Discovery Sources

Build discovery may use:

- project manifests;
- solution or workspace files;
- build configuration;
- scripts;
- repository documentation;
- plugin adapters;
- and developer configuration.

---

## 12.2 Detection Is Not Trust

Detecting a build command does not authorise execution.

---

## 12.3 Multiple Build Systems

A project may contain multiple build systems.

Each must have a separate profile or clearly defined scope.

---

## 12.4 Ambiguous Discovery

Ambiguous or conflicting build detection must be visible.

---

# 13. Build Profiles

## 13.1 Required Fields

A Build Profile should include:

- profile identifier;
- project;
- title;
- build system;
- working directory;
- command plan;
- environment profile;
- input scope;
- output locations;
- expected artefacts;
- timeout;
- resource class;
- isolation requirements;
- and validation rules.

---

## 13.2 Profile Sources

A profile may be:

- detected;
- developer-created;
- plugin-provided;
- imported;
- or workflow-proposed.

---

## 13.3 Versioning

Build Profiles must be versioned.

---

## 13.4 Activation

One or more profiles may be active.

The default profile must be explicit.

---

## 13.5 Profile Changes

Material changes require validation and may require renewed approval.

---

# 14. Test Profiles

## 14.1 Required Fields

A Test Profile should include:

- profile identifier;
- test framework;
- command plan;
- test scope;
- filters;
- environment;
- timeout;
- retry policy;
- parallelism;
- expected result format;
- and coverage configuration.

---

## 14.2 Test Scope

Scopes may include:

- one test;
- one file;
- one suite;
- affected tests;
- smoke tests;
- integration tests;
- full suite;
- and custom selection.

---

## 14.3 Flaky Tests

Flaky tests should be identified separately from deterministic failures.

---

## 14.4 Retrying Tests

Automatic test retry must not hide flakiness.

The final result must show all attempts.

---

# 15. Command Plans

## 15.1 Required Structure

A Command Plan must define:

- executable identity;
- arguments;
- working directory;
- environment summary;
- secret references;
- input files;
- expected outputs;
- timeout;
- cancellation method;
- shell use;
- elevation requirement;
- network expectation;
- and risk.

---

## 15.2 Structured Arguments

Executable and arguments must remain separate.

---

## 15.3 Shell Use

Shell execution should be avoided where direct process invocation is possible.

---

## 15.4 Command Preview

Risky command plans must be visible before execution.

---

## 15.5 Command Identity

Known tools should use adapter identity rather than unstructured command text where practical.

---

# 16. External Process Execution

## 16.1 Controlled Host

External processes must run through the CLI Adapter Host, Build Manager or another approved execution service.

---

## 16.2 Working Directory

The working directory must be explicit and inside an approved scope unless separately authorised.

---

## 16.3 Environment

The environment must be constructed deliberately.

---

## 16.4 Output

Standard output and standard error must be captured separately where possible.

---

## 16.5 Cancellation

Processes must support cancellation and verified termination.

---

## 16.6 Child Processes

Child-process behaviour must be controlled.

---

## 16.7 Exit State

The Build Manager must distinguish:

- successful exit;
- failed exit;
- timeout;
- cancellation;
- process crash;
- and termination failure.

---

# 17. Build Execution Lifecycle

A build should use states such as:

- Created;
- Validating;
- Approval Required;
- Queued;
- Starting;
- Running;
- Cancelling;
- Cancelled;
- Succeeded;
- Succeeded with Warnings;
- Failed;
- Timed Out;
- Recovery Required;
- and Superseded.

---

## 17.1 Preflight

Build preflight should validate:

- profile;
- project state;
- workspace access;
- required tools;
- required secrets;
- policy;
- output paths;
- and resource availability.

---

## 17.2 Execution

Build execution should:

1. create execution record;
2. prepare environment;
3. acquire required resources;
4. start process;
5. stream output;
6. monitor resource use;
7. parse diagnostics;
8. capture artefacts;
9. record exit state;
10. and publish outcome.

---

## 17.3 Post-Execution

Post-execution should:

- verify expected artefacts;
- parse diagnostics;
- record hashes where useful;
- update project memory;
- and link Trust Centre records.

---

# 18. Test Execution Lifecycle

Test execution should follow a similar lifecycle to build execution.

It must additionally capture:

- discovered test cases where available;
- test result;
- duration;
- failure output;
- source locations;
- retries;
- flaky classification;
- and coverage artefacts.

---

# 19. Build Output Parsing

## 19.1 Structured Diagnostics

Adapters should translate tool output into structured diagnostics.

---

## 19.2 Diagnostic Fields

A diagnostic should include:

- severity;
- code;
- message;
- file;
- line and column;
- tool;
- profile;
- and raw output reference.

---

## 19.3 Safe Parsing

Malformed output must not crash the Build Manager.

---

## 19.4 Unparsed Output

Unparsed output should remain available.

---

# 20. Build Artefacts

## 20.1 Artefact Metadata

An artefact record should include:

- artefact identifier;
- build record;
- path;
- type;
- size;
- hash;
- creation time;
- retention;
- and publication state.

---

## 20.2 Artefact Classification

Artefacts may include:

- executable;
- library;
- package;
- installer;
- archive;
- report;
- coverage data;
- logs;
- documentation;
- symbol files;
- and generated source.

---

## 20.3 Retention

Artefact retention must be configurable.

---

## 20.4 Sensitive Artefacts

Artefacts containing secrets or restricted content must be protected.

---

## 20.5 Publication

Publication is a separate protected action.

---

# 21. Build Reproducibility

## 21.1 Evidence

A build record should capture enough information to support reproducibility where possible:

- source revision;
- profile version;
- tool versions;
- dependency lock state;
- environment summary;
- command plan;
- and relevant configuration.

---

## 21.2 Honest Claims

Opure must not claim a build is reproducible merely because it succeeded twice.

---

## 21.3 Environment Drift

Environment drift should be visible.

---

# 22. Toolchain Discovery

Opure may detect:

- installed runtimes;
- compilers;
- SDKs;
- package managers;
- build tools;
- test runners;
- and repository tools.

Detection should identify:

- path;
- version;
- source;
- trust level;
- and project compatibility.

---

# 23. Toolchain Selection

The developer must be able to select among compatible installed toolchains.

Automatic selection must be explainable.

---

# 24. Environment Management

## 24.1 Environment Profile

An Environment Profile should include:

- runtime paths;
- non-secret variables;
- secret references;
- architecture;
- platform;
- toolchain;
- package environment;
- and isolation method.

---

## 24.2 Secrets

Secret values must remain in the Secrets Vault.

---

## 24.3 Inheritance

Environment inheritance must be controlled.

---

## 24.4 Project Environments

Examples include:

- Python virtual environment;
- Node.js version environment;
- .NET SDK selection;
- Java runtime;
- Rust toolchain;
- container environment;
- or custom local toolchain.

---

## 24.5 Activation

Environment activation must not mutate the developer's global shell unexpectedly.

---

# 25. Dependency Management

## 25.1 Responsibility

The Dependency Manager coordinates dependency discovery and proposed changes.

It does not replace package managers.

---

## 25.2 Dependency Sources

Dependencies may come from:

- manifests;
- lock files;
- solution files;
- module files;
- generated metadata;
- and package-manager queries.

---

## 25.3 Dependency Record

A Dependency Record should include:

- name;
- version constraint;
- resolved version;
- source;
- direct or transitive classification;
- scope;
- licence where known;
- integrity metadata where known;
- and security metadata where available.

---

## 25.4 Package Manager Execution

Package-manager operations must run through controlled command plans.

---

## 25.5 Scripts

Package-manager scripts are executable code and must be treated as a trust boundary.

---

## 25.6 Install Preview

Before dependency installation, Opure should show:

- packages;
- versions;
- source registries;
- scripts likely to run;
- lock-file changes;
- manifest changes;
- network destinations;
- expected size;
- and risk.

---

# 26. Dependency Change Lifecycle

A dependency change may use states such as:

- Proposed;
- Analysing;
- Approval Required;
- Applying;
- Installed;
- Validating;
- Validated;
- Failed;
- Reverted;
- and Recovery Required.

---

## 26.1 Patch First

Manifest and lock-file changes should be represented as patches where practical.

---

## 26.2 Installation

Package installation may occur only after policy and approval requirements are met.

---

## 26.3 Validation

After installation, Opure should run configured validation.

---

## 26.4 Reversal

Reversal may require:

- restoring manifests;
- restoring lock files;
- reinstalling prior dependency state;
- or developer intervention.

The interface must describe limits honestly.

---

# 27. Dependency Security

## 27.1 Registry Trust

Package registries must be treated as external trust boundaries.

---

## 27.2 Credentials

Private registry credentials must use the Secrets Vault.

---

## 27.3 Integrity

Lock-file or package integrity metadata should be verified where supported.

---

## 27.4 Vulnerability Information

Vulnerability information may be displayed when available from reliable sources or local tooling.

Unknown status must remain unknown.

---

## 27.5 Licence Information

Licence data may be displayed where known.

Opure must not make legal claims beyond available evidence.

---

# 28. Repository Service

## 28.1 Responsibility

The Repository Service provides version-control operations through a provider-neutral contract.

---

## 28.2 Initial Git Support

The first implementation may support Git only.

---

## 28.3 Repository States

A repository may be:

- Not Detected;
- Ready;
- Dirty;
- Conflicted;
- Detached;
- Operation In Progress;
- Unavailable;
- and Recovery Required.

---

# 29. Repository Discovery

Repository discovery must identify:

- repository root;
- type;
- working tree;
- metadata path;
- current branch or detached state;
- remotes;
- and nested repository behaviour.

---

# 30. Repository Status

Status should expose:

- modified;
- staged;
- untracked;
- deleted;
- renamed;
- conflicted;
- ignored;
- and submodule state where supported.

---

# 31. Branch Operations

Branch operations may include:

- list;
- create;
- switch;
- rename;
- delete;
- and compare.

Changing branch is a protected state-changing action.

---

## 31.1 Dirty Workspace

Branch switching with local changes must show conflict risk.

---

## 31.2 Automatic Stash

Automatic stash must not occur silently.

---

# 32. Staging

## 32.1 Explicit Action

Staging is separate from patch application.

---

## 32.2 Scope

Staging should support:

- file;
- hunk;
- selected patch;
- and exact path set.

---

## 32.3 Secret Scan

Staged content should be secret scanned.

---

## 32.4 Existing Staged Changes

Opure must preserve unrelated staged changes.

---

# 33. Commit Preparation

## 33.1 Commit Plan

A Commit Plan should include:

- files and hunks;
- validation evidence;
- commit message;
- author identity;
- signing option;
- and repository state.

---

## 33.2 AI Assistance

AI may propose a commit message.

It must not create the commit without approval.

---

## 33.3 Commit Validation

Before commit, Opure should validate:

- exact staged set;
- secret scan;
- unresolved conflict;
- identity;
- hooks policy;
- and approval.

---

## 33.4 Hooks

Repository hooks must be visible as executable risk.

---

# 34. Commit Execution

Commit execution must:

- use the Repository Service;
- preserve exact staged state;
- record resulting commit identity;
- and report hook behaviour.

---

# 35. Remote Repository Operations

Remote operations may include:

- fetch;
- pull;
- push;
- clone;
- and remote inspection.

All remote traffic must pass through network and credential controls.

---

## 35.1 Push

Push is a protected external action.

---

## 35.2 Force Push

Force push is Critical risk by default.

---

## 35.3 Pull and Merge

Pull behaviour must be explicit about merge, rebase or fast-forward strategy.

---

## 35.4 Credentials

Remote credentials must use the Secrets Vault.

---

# 36. Repository Recovery

Interrupted repository operations must be detected.

Examples include:

- merge;
- rebase;
- cherry-pick;
- revert;
- and bisect.

Opure should expose the repository's actual recovery options rather than inventing generic ones.

---

# 37. Source Revision Identity

Build, test, patch and workflow records should identify:

- repository commit where available;
- workspace dirty state;
- staged state;
- and relevant file hashes.

---

# 38. Validation Strategy

Project validation may combine:

- syntax checks;
- formatting checks;
- lint;
- type checking;
- compile;
- unit tests;
- integration tests;
- security checks;
- package checks;
- and artefact verification.

---

## 38.1 Validation Profiles

Validation Profiles may group build and test profiles.

---

## 38.2 Required Versus Optional

Each check must be marked:

- required;
- recommended;
- optional;
- or unavailable.

---

## 38.3 Unperformed Checks

Unperformed checks must remain visible.

---

# 39. Affected-Test Selection

The Build Manager and Knowledge Engine may identify tests likely affected by a patch.

This is a prioritisation aid.

It must not imply unaffected tests are unnecessary.

---

# 40. Test Coverage

Coverage data may be captured where available.

Coverage must not be represented as proof of correctness.

---

# 41. Build Caching

## 41.1 Tool-Owned Caches

Opure may use existing build-tool caches.

---

## 41.2 Opure Cache Metadata

Opure may record cache state and size.

---

## 41.3 Cache Invalidation

Cache invalidation must remain tool-appropriate.

---

## 41.4 Cleanup

Cleanup must identify what will be removed.

---

# 42. Temporary Worktrees and Sandboxes

Opure may use temporary worktrees or isolated copies to validate patches before application.

Such environments must:

- preserve project identity references;
- protect secrets;
- use bounded storage;
- clean up safely;
- and report differences from the active workspace.

---

# 43. Build Isolation

Isolation may use:

- process boundaries;
- job objects;
- containers;
- virtual environments;
- temporary worktrees;
- or restricted execution users.

Exact mechanisms are DEFERRED.

---

# 44. Resource Management

Build and test work must use the Scheduler.

Profiles should declare:

- CPU;
- memory;
- GPU;
- disk;
- network;
- and expected duration.

---

# 45. Performance Modes

## 45.1 Eco

- limit parallel builds and tests;
- defer background validation;
- and reduce CPU use.

## 45.2 Balanced

- preserve interactive responsiveness;
- allow normal local builds;
- and bound background work.

## 45.3 Performance

- increase parallelism where supported;
- and prioritise completion speed.

## 45.4 Turbo

- maximise throughput within user-approved resource limits.

Performance mode must not bypass security or approval.

---

# 46. Cancellation

Builds, tests, package operations and repository operations should support cancellation where technically possible.

The final state must distinguish:

- cancellation requested;
- child process still active;
- safely terminated;
- partially completed;
- and recovery required.

---

# 47. Timeouts

Timeout policy must be explicit.

A timed-out external process must not be assumed terminated until verified.

---

# 48. Retries

Retries must be bounded and idempotency-aware.

Automatic retry of package installation or repository mutation must be conservative.

---

# 49. Failure Handling

## 49.1 Build Failure

A build failure should preserve:

- command plan;
- output;
- diagnostics;
- environment summary;
- source revision;
- and artefacts produced.

---

## 49.2 Test Failure

A test failure should preserve:

- exact test identity;
- output;
- retries;
- source revision;
- and related patch or workflow.

---

## 49.3 Package Failure

A package operation failure must identify whether:

- manifests changed;
- lock files changed;
- packages were partially installed;
- scripts ran;
- and recovery is required.

---

## 49.4 Repository Failure

Repository failure must preserve actual repository state and native recovery information.

---

# 50. Project Memory Integration

The Knowledge Engine should receive records for:

- project profile;
- build profiles;
- test profiles;
- toolchains;
- dependencies;
- build results;
- test results;
- errors;
- fixes;
- repository state;
- commits;
- and release outcomes.

---

# 51. Workflow Integration

Built-in workflows may coordinate:

- Diagnose Build Failure;
- Add Dependency;
- Update Dependency;
- Generate Tests;
- Prepare Commit;
- Prepare Release;
- and Validate Patch.

---

# 52. Patch Integration

Project configuration, dependency manifests, build scripts and source changes must use the Patch Service for protected file modification.

---

# 53. Security Integration

Protected actions require Policy Engine evaluation.

Secret use must pass through Secrets Vault.

Remote operations must pass through Network Gateway.

Significant actions must be recorded in Trust Centre.

---

# 54. Plugin Integration

Plugins may provide:

- build adapters;
- test adapters;
- repository adapters;
- package-manager adapters;
- artefact processors;
- and release workflows.

They must not bypass service boundaries.

---

# 55. MCP Integration

MCP capabilities may assist:

- remote CI inspection;
- issue tracker integration;
- release-note gathering;
- and external build systems.

All access remains mediated by the MCP Gateway and policy.

---

# 56. Desktop Experience Requirements

The desktop should show:

- project profile;
- detected tools;
- active build and test profiles;
- command plans;
- environment summary;
- repository state;
- dependencies;
- artefacts;
- release plans;
- and recovery state.

---

# 57. Project Dashboard

The Project Dashboard should summarise:

- project identity;
- workspace;
- repository;
- profile;
- build status;
- test status;
- dependencies;
- active environment;
- active workflows;
- and recent Trust Centre activity.

---

# 58. Build and Test History

History should support filtering by:

- profile;
- result;
- branch;
- commit;
- patch;
- workflow;
- and date.

---

# 59. Dependency View

The desktop should show:

- direct dependencies;
- resolved versions;
- lock state;
- source;
- available update where known;
- licence where known;
- vulnerability information where known;
- and project scope.

---

# 60. Environment View

The desktop should show:

- active toolchain;
- runtime version;
- virtual environment;
- non-secret variables;
- secret references;
- isolation method;
- and drift warnings.

---

# 61. Artefact View

The desktop should show:

- build source;
- artefact type;
- size;
- hash;
- location;
- retention;
- and publication state.

---

# 62. Release Preparation

## 62.1 Release Plan

A Release Plan should include:

- target version;
- source revision;
- changelog;
- validation;
- artefacts;
- signing;
- publication destinations;
- and rollback guidance.

---

## 62.2 Version Changes

Version-file updates must be patches.

---

## 62.3 Release Notes

AI may propose release notes using project evidence.

They remain reviewable text.

---

## 62.4 Publication

External publication is a separate High or Critical risk action.

---

## 62.5 No Silent Release

Opure must never publish a release silently.

---

# 63. Packaging

Packaging must use controlled build profiles and produce recorded artefacts.

---

# 64. Signing

Code signing or package signing must use Secrets Vault-controlled keys or external signing systems.

Private signing keys must not be exposed to workflows or plugins unnecessarily.

---

# 65. Publication Destinations

Destinations may include:

- source repository;
- package registry;
- release server;
- object storage;
- application store;
- or private distribution system.

Each destination requires explicit configuration and policy.

---

# 66. Release Validation

A release should identify:

- checks performed;
- checks skipped;
- artefact hashes;
- source revision;
- dependency state;
- and signing state.

---

# 67. Rollback Planning

Release preparation should include rollback or recovery guidance where possible.

It must not claim rollback is guaranteed for external systems.

---

# 68. Project Templates

Opure may support project templates.

Templates must include:

- source;
- version;
- licence;
- expected files;
- dependency operations;
- and initial workflow.

Applying a template creates reviewable project changes.

---

# 69. Multi-Module and Monorepo Projects

The architecture should support:

- multiple modules;
- multiple build systems;
- multiple package managers;
- and scoped profiles.

The first implementation may provide limited monorepo support.

---

# 70. Nested Repositories

Nested repositories and submodules must be detected and represented explicitly.

---

# 71. Generated Files

Build-generated files should be classified and excluded from normal source patching where appropriate.

---

# 72. Clean Operations

Clean commands may delete build outputs.

They must show scope and risk.

Broad deletion outside known output directories requires stronger approval.

---

# 73. Project Backups

Opure may support project metadata and configuration backup.

It must not imply source backup exists unless the workspace is actually backed up.

---

# 74. Project Export

Project export may include:

- Opure metadata;
- settings;
- profiles;
- memory export;
- workflow definitions;
- and selected Trust Centre records.

Secrets must not be exported by default.

---

# 75. Import Portability

Imported project metadata must validate:

- paths;
- toolchains;
- providers;
- plugins;
- and policy compatibility.

Machine-specific settings should not be applied blindly.

---

# 76. API Contract — Project Manager

## 76.1 Commands

The Project Manager should provide:

- `CreateProject`
- `ImportProject`
- `OpenProject`
- `CloseProject`
- `RemoveProject`
- `UpdateProjectMetadata`
- `UpdateProjectProfile`
- `SetProjectPolicy`
- and `MoveProjectWorkspace`

---

## 76.2 Queries

It should provide:

- `ListProjects`
- `GetProject`
- `GetProjectProfile`
- `GetProjectHealth`
- `GetProjectSettings`
- `GetProjectCapabilities`
- and `GetProjectOpenState`

---

## 76.3 Events

It should publish:

- `ProjectCreated`
- `ProjectImported`
- `ProjectOpened`
- `ProjectClosed`
- `ProjectMoved`
- `ProjectProfileChanged`
- `ProjectHealthChanged`
- and `ProjectRemoved`

---

# 77. API Contract — Build Manager

## 77.1 Commands

The Build Manager should provide:

- `CreateBuildProfile`
- `UpdateBuildProfile`
- `DeleteBuildProfile`
- `RunBuild`
- `CancelBuild`
- `CreateTestProfile`
- `UpdateTestProfile`
- `DeleteTestProfile`
- `RunTests`
- `CancelTests`
- `CleanBuildOutputs`
- and `ValidateEnvironment`

---

## 77.2 Queries

It should provide:

- `ListBuildProfiles`
- `GetBuildProfile`
- `ListTestProfiles`
- `GetTestProfile`
- `GetBuild`
- `ListBuilds`
- `GetTestRun`
- `ListTestRuns`
- `GetDiagnostics`
- `GetArtefacts`
- and `GetToolchains`

---

## 77.3 Events

It should publish:

- `BuildProfileCreated`
- `BuildStarted`
- `BuildOutputReceived`
- `BuildCompleted`
- `BuildFailed`
- `BuildCancelled`
- `TestRunStarted`
- `TestCaseCompleted`
- `TestRunCompleted`
- `TestRunFailed`
- and `ArtefactRecorded`

---

# 78. API Contract — Repository Service

## 78.1 Commands

The Repository Service should provide:

- `RefreshRepositoryStatus`
- `CreateBranch`
- `SwitchBranch`
- `DeleteBranch`
- `StageFiles`
- `UnstageFiles`
- `CreateCommit`
- `RestoreFiles`
- `FetchRepository`
- `PullRepository`
- `PushRepository`
- `StartMerge`
- `ContinueMerge`
- `AbortMerge`
- and `ResolveConflict`

---

## 78.2 Queries

It should provide:

- `GetRepository`
- `GetRepositoryStatus`
- `GetBranch`
- `ListBranches`
- `GetDiff`
- `GetHistory`
- `GetCommit`
- `GetRemotes`
- and `GetRecoveryState`

---

## 78.3 Events

It should publish:

- `RepositoryDiscovered`
- `RepositoryStatusChanged`
- `BranchChanged`
- `FilesStaged`
- `CommitCreated`
- `RemoteOperationStarted`
- `RemoteOperationCompleted`
- `RepositoryConflictDetected`
- and `RepositoryRecoveryRequired`

---

# 79. API Contract — Dependency Manager

## 79.1 Commands

The Dependency Manager should provide:

- `RefreshDependencies`
- `ProposeDependencyAdd`
- `ProposeDependencyUpdate`
- `ProposeDependencyRemove`
- `ApplyDependencyPlan`
- `CancelDependencyOperation`
- and `RestoreDependencyState`

---

## 79.2 Queries

It should provide:

- `ListDependencies`
- `GetDependency`
- `GetDependencyPlan`
- `GetDependencyOperation`
- `GetPackageManager`
- `GetRegistryConfiguration`
- and `GetDependencyHealth`

---

# 80. Error Model

Recommended error codes include:

- `PROJECT_NOT_FOUND`
- `PROJECT_ALREADY_REGISTERED`
- `PROJECT_WORKSPACE_MISSING`
- `PROJECT_PROFILE_AMBIGUOUS`
- `PROJECT_OPEN_FAILED`
- `PROJECT_RECOVERY_REQUIRED`
- `BUILD_PROFILE_INVALID`
- `BUILD_TOOL_NOT_FOUND`
- `BUILD_ENVIRONMENT_INVALID`
- `BUILD_APPROVAL_REQUIRED`
- `BUILD_POLICY_DENIED`
- `BUILD_START_FAILED`
- `BUILD_FAILED`
- `BUILD_TIMEOUT`
- `BUILD_CANCELLED`
- `BUILD_TERMINATION_FAILED`
- `TEST_PROFILE_INVALID`
- `TEST_FAILED`
- `TEST_CANCELLED`
- `DEPENDENCY_PLAN_INVALID`
- `DEPENDENCY_SCRIPT_RISK`
- `DEPENDENCY_INSTALL_FAILED`
- `DEPENDENCY_RECOVERY_REQUIRED`
- `REPOSITORY_NOT_FOUND`
- `REPOSITORY_DIRTY`
- `REPOSITORY_CONFLICT`
- `REPOSITORY_OPERATION_FAILED`
- `REPOSITORY_APPROVAL_REQUIRED`
- `REPOSITORY_REMOTE_BLOCKED`
- `RELEASE_VALIDATION_FAILED`
- `RELEASE_PUBLICATION_DENIED`
- and `PROJECT_BUILD_INTERNAL_ERROR`

---

# 81. Trust Centre Integration

The Trust Centre should record:

- project creation and import;
- profile detection;
- build and test profile changes;
- command plans;
- build and test execution;
- dependency changes;
- repository changes;
- remote operations;
- artefact creation;
- release preparation;
- signing;
- publication;
- and recovery.

---

# 82. Security Requirements

## 82.1 Scripts

Project, package and repository scripts are untrusted executable content.

---

## 82.2 Secrets

Credentials and signing keys must use Secrets Vault controls.

---

## 82.3 Network

Remote registries and repositories must use Network Gateway controls.

---

## 82.4 Publication

External publication requires explicit approval.

---

## 82.5 File Changes

Configuration and manifest changes use Patch Service.

---

## 82.6 Privilege

Build and package operations must not elevate privileges silently.

---

# 83. Testing Strategy

## 83.1 Unit Tests

Unit tests must cover:

- project identity;
- project lifecycle;
- profile detection;
- profile versioning;
- command-plan validation;
- environment construction;
- build state transitions;
- test-result parsing;
- dependency-plan validation;
- repository state transitions;
- and release-plan validation.

---

## 83.2 Adapter Contract Tests

Every build, test, package and repository adapter must pass contract tests.

---

## 83.3 Integration Tests

Tests should cover:

- import project;
- detect build system;
- create profile;
- run build;
- run tests;
- parse diagnostics;
- stage exact patch files;
- create commit;
- add dependency;
- cancel operation;
- and recover after failure.

---

## 83.4 Security Tests

Tests must cover:

- script execution warning;
- secret redaction;
- registry credential use;
- malicious package output;
- unsafe working directory;
- command injection;
- repository hook execution;
- and force-push approval.

---

## 83.5 Failure Tests

Tests should simulate:

- missing toolchain;
- process crash;
- timeout;
- child-process leak;
- disk full;
- lock-file conflict;
- partial dependency install;
- interrupted merge;
- and artefact capture failure.

---

## 83.6 Recovery Tests

Tests must prove:

- project open can recover;
- interrupted build state is known;
- partial package operations are visible;
- repository-native recovery state is preserved;
- and unsafe automatic continuation is blocked.

---

## 83.7 Performance Tests

Tests should cover:

- large build output;
- many test cases;
- large dependency graph;
- large repository history;
- and parallel build scheduling.

---

# 84. Performance Requirements

## 84.1 Responsiveness

Build and test streaming must not block the desktop.

---

## 84.2 Large Output

Large output must use bounded buffering and virtualised display.

---

## 84.3 Parallelism

Parallel execution must respect Scheduler and project tool limits.

---

## 84.4 Idle

Project services should consume minimal CPU when no work is active.

---

# 85. Initial Implementation Milestone

The first Project and Build Management milestone is successful when Opure can:

1. register and open an existing Windows project;
2. preserve stable project identity;
3. detect at least one supported language and build system;
4. create and edit a Build Profile;
5. create and edit a Test Profile;
6. show a structured command plan;
7. run a build through the CLI Adapter Host;
8. stream output;
9. parse file-linked diagnostics;
10. cancel a running build;
11. run tests and record results;
12. record build artefacts;
13. detect a Git repository;
14. show repository status;
15. stage exact selected files;
16. prepare and create a commit with approval;
17. detect a supported package manager;
18. propose a dependency change;
19. show scripts and network impact;
20. apply the dependency plan with approval;
21. record project, build, test and repository activity in the Trust Centre;
22. update project memory;
23. recover safely from a simulated interrupted operation;
24. and remain fully usable without cloud services.

---

# 86. Acceptance Criteria

SPEC-011 is implemented when:

- [ ] Every project has stable identity independent from path.
- [ ] Project import shows detected profile evidence.
- [ ] Detected commands are not treated as automatically trusted.
- [ ] Build and Test Profiles are versioned and inspectable.
- [ ] Command Plans separate executable and arguments.
- [ ] Working directory and environment are explicit.
- [ ] Build and test execution is cancellable.
- [ ] Process termination is verified.
- [ ] Output is streamed and bounded.
- [ ] Diagnostics link to files where evidence exists.
- [ ] Test retries do not hide flaky behaviour.
- [ ] Build records retain source revision and tool versions.
- [ ] Artefacts retain hashes and provenance.
- [ ] Dependency changes are previewed.
- [ ] Package scripts are treated as executable risk.
- [ ] Manifest and lock-file changes use Patch Service.
- [ ] Registry credentials remain in Secrets Vault.
- [ ] Repository status preserves unrelated developer changes.
- [ ] Patch application, staging and commit remain separate.
- [ ] Staging can be limited to exact files or hunks.
- [ ] Secret scanning occurs before commit.
- [ ] Repository hooks are visible.
- [ ] Remote repository operations use Network Gateway controls.
- [ ] Force push requires Critical-risk approval.
- [ ] Interrupted repository operations preserve native recovery state.
- [ ] Environment activation does not mutate the developer's shell unexpectedly.
- [ ] Toolchain selection is visible and configurable.
- [ ] Validation states what was and was not run.
- [ ] Release preparation is separate from publication.
- [ ] External publication never occurs silently.
- [ ] Signing keys are not exposed unnecessarily.
- [ ] Project memory receives build, test and repository evidence.
- [ ] Plugin adapters remain bounded by service contracts.
- [ ] Trust Centre records contain no secret values.
- [ ] Recovery tests cover build, package and repository interruption.
- [ ] The platform remains useful without supported build tooling.
- [ ] The developer can still use ordinary command-line and repository tools outside Opure.

---

# 87. Deferred Decisions

The following are intentionally deferred:

- initial supported languages;
- initial supported build systems;
- initial package managers;
- initial test frameworks;
- exact command-runner implementation;
- container support;
- remote build agents;
- public CI integration;
- dependency vulnerability data source;
- licence analysis;
- monorepo depth;
- submodule support depth;
- release publishing adapters;
- code-signing providers;
- package registry integrations;
- and team project sharing.

These decisions must not weaken:

- explicit execution;
- script risk visibility;
- developer control;
- secret handling;
- repository safety;
- or reproducibility evidence.

---

# 88. Required Architecture Decision Records

Implementation should produce ADRs for:

- project identity storage;
- project metadata layout;
- Project Profile detection;
- Build Profile schema;
- Test Profile schema;
- command-plan format;
- process execution on Windows;
- child-process control;
- environment-profile model;
- package-manager adapter model;
- Git integration method;
- repository recovery handling;
- artefact storage;
- build output parsing;
- and release-plan format.

---

# 89. Relationship to SPEC-012

SPEC-012 will define the delivery roadmap, sequencing and milestone boundaries for all approved specifications.

It must identify:

- which project systems are required for the first usable product;
- which build and repository adapters arrive first;
- which capabilities remain deferred;
- and what evidence is required before each milestone advances.

---

# 90. Founder Approval

This document remains a founder draft until explicitly approved.

Approval establishes the following rules:

- Projects have stable identity independent from folder path.
- Detection does not equal trust.
- Build, test, package and repository operations remain explicit.
- Commands are structured, inspectable and cancellable.
- Project and package scripts are treated as executable risk.
- Dependency changes are reviewable and validated.
- Git staging and commit remain separate from patch application.
- Remote repository and registry operations remain policy controlled.
- Secrets and signing keys remain in the Secrets Vault.
- Builds, tests, dependencies, commits and artefacts retain evidence and provenance.
- Release preparation does not equal publication.
- External publication never occurs silently.
- Developer tools outside Opure remain valid and supported.
- Opure coordinates engineering work without taking ownership away from the developer.

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**