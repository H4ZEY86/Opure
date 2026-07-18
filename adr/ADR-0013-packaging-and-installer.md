# ADR-0013 — Packaging and Installer

## Architecture Decision Record

**Status:** Proposed
**Date:** 18 July 2026
**Decision owners:** Founder and Product Owner
**Technical owners:** Packaging and Installation Owner
**Reviewers:** Release Engineering Owner, Build and Continuous Integration Owner, Security Owner, Runtime Architecture Owner, Desktop Owner, Persistence Owner, Secrets Owner, Recovery Owner, Test Architecture Owner
**Supersedes:** None
**Superseded by:** None
**Related ADRs:** ADR-0001 through ADR-0012
**Related specifications:** CHARTER-001, SPEC-001 through SPEC-012
**Target milestone:** Phase 1 — Architecture Skeleton through Version 1.0

---

## 1. Decision Summary

Opure will provisionally use a **signed, per-user MSIX package as the primary Windows installation format**.

The first supported package will:

* target Windows 11 x64;
* use a full-trust packaged desktop application model;
* run at normal user integrity;
* require no administrator elevation for ordinary installation;
* contain the Desktop, Runtime, Worker, Plugin Host and required first-party support binaries;
* use a self-contained, multi-file .NET deployment;
* package the exact release-candidate files that passed validation;
* keep installed application files immutable;
* keep all mutable Opure state outside the package installation directory;
* preserve source repositories, project metadata, Vault state, plugins and local models during ordinary repair, update and uninstall;
* use separate stable, preview and development package identities;
* use separate profile roots and IPC namespaces for each channel;
* bind package identity to a final publisher certificate subject before the first public release;
* map product SemVer to a separate monotonic four-part MSIX deployment version;
* build directly from source-published files rather than converting another installer;
* create and test an unsigned candidate before the privileged signing boundary;
* sign, timestamp and verify the exact tested package;
* publish checksums, an SBOM, a component manifest and release evidence;
* provide an unpackaged diagnostic ZIP as a secondary troubleshooting artefact;
* and defer automatic updating, Store publication and enterprise MSI packaging.

The package will not initially:

* install a Windows service;
* install a driver;
* create scheduled tasks;
* register automatic startup;
* modify `PATH`;
* install a shell extension;
* use custom install or uninstall actions;
* write into the installation directory;
* use Package Support Framework fixups;
* perform background update checks;
* force an update before launch;
* or remove durable user data on uninstall.

---

## 2. Status

Current status:

> **Proposed**

This ADR becomes **Accepted** only after a packaging prototype demonstrates:

* source-based MSIX construction;
* a valid source-controlled manifest;
* per-user installation without elevation;
* self-contained x64 publication;
* full-trust Desktop launch;
* Runtime, Worker and Plugin Host launch;
* authenticated named-pipe IPC;
* package identity detection;
* package-directory immutability;
* external persistent state;
* repair, update and uninstall;
* preservation of repositories and durable profiles;
* explicit safe cleanup;
* stable and preview side-by-side installation;
* deterministic SemVer-to-MSIX mapping;
* signature and timestamp verification;
* clean-machine installation;
* offline launch;
* Safe Mode;
* Recovery Mode;
* and a complete release-candidate simulation.

---

## 3. Context

Opure is a local software engineering platform composed of several cooperating executables:

* `Opure.Desktop`;
* `Opure.Runtime`;
* `Opure.Worker`;
* `Opure.PluginHost`;
* future recovery and diagnostic hosts;
* first-party provider adapters;
* local IPC contracts;
* native Windows integration;
* and public SDK assets.

The installed product interacts with user-selected source repositories, SQLite stores, project memory, workflow checkpoints, diagnostics, plugins, local models and an encrypted Secrets Vault.

Packaging therefore determines more than file copying. It establishes:

* application identity;
* publisher trust;
* installation and repair behaviour;
* update continuity;
* process activation;
* writable-state boundaries;
* Windows integration;
* uninstall consequences;
* and release verification.

A poor choice could require unnecessary elevation, delete durable data, break plugin loading, redirect files unexpectedly, expose signing credentials, create stale mixed-version installations or make a release impossible to verify.

The package must follow the Charter:

* installation is explicit;
* application files and developer-owned data are separate;
* user repositories are never installer-owned;
* the Vault is never installer-owned;
* repair affects package files rather than durable state;
* uninstall removes the application but does not silently destroy the developer's data;
* and publication promotes exactly what was tested.

---

## 4. Problem Statement

Opure needs a Windows installer that provides trusted per-user installation and clean application-file removal while preserving its multi-process architecture, plugin model, local data, security boundaries and release evidence.

---

## 5. Decision Drivers

The decision is evaluated against:

* Charter alignment;
* Windows 11 compatibility;
* per-user installation;
* no-elevation operation;
* package identity;
* signature verification;
* multi-process support;
* Avalonia compatibility;
* .NET deployment;
* local IPC;
* plugin compatibility;
* source-repository safety;
* Vault and database preservation;
* clean repair and uninstall;
* update continuity;
* Microsoft Store eligibility;
* enterprise deployment;
* future ARM64 support;
* deterministic packaging;
* release immutability;
* small-team maintenance;
* and testability.

---

## 6. Governing Principles

This decision must preserve:

* Developer Respect;
* Developer First;
* Local by Design;
* Human in Control;
* Visible and Inspectable Decisions;
* Least Privilege;
* Project Isolation;
* No Unnecessary Elevation;
* No Hidden Persistence;
* No Secret in an Installer;
* No Custom Uninstall Surprise;
* No Automatic Network Activity Without Policy;
* No Release Rebuild;
* No Data-Loss Uninstall;
* No False Downgrade Promise;
* and Evidence-Based Confidence.

---

## 7. Scope

This ADR decides:

* the primary Windows package format;
* installation scope;
* runtime trust level;
* supported architecture;
* .NET deployment mode;
* package identity and channel identities;
* product-version mapping;
* package layout;
* mutable-state locations;
* plugin and model locations;
* package build tooling;
* signing boundary;
* package verification;
* install, repair and uninstall behaviour;
* explicit cleanup;
* diagnostic ZIP behaviour;
* package testing;
* and enterprise fallback criteria.

It does not decide:

* the updater;
* update-check frequency;
* update hosting;
* Microsoft Store submission;
* final code-signing provider;
* key custody;
* public download site;
* enterprise deployment platform;
* Windows ARM64 release date;
* Linux or macOS packaging;
* local-model distribution;
* or plugin marketplace distribution.

---

## 8. Constraints

Known constraints include:

* Windows 11 is the first supported desktop platform.
* x64 is the first architecture.
* Opure uses C# and .NET 10 LTS.
* Avalonia is the selected UI framework.
* Runtime, Worker and Plugin Host are separate processes.
* Local named-pipe IPC must continue working.
* MSIX packages with executable code require identity and signing.
* MSIX uses a four-part numeric version whose major component cannot be zero.
* Opure product SemVer begins before `1.0.0`.
* Package family identity depends on package name and publisher.
* The manifest publisher must match the signing certificate subject.
* Public package identity is expensive to change.
* Package files are protected and not writable at runtime.
* Plugins and durable user state are mutable.
* Uninstall cannot safely run arbitrary application cleanup.
* The founder may not yet have a final production signing identity.
* The package must operate offline after installation.
* Release publication cannot rebuild source.
* The first implementation must remain practical for a small team.

---

## 9. Assumptions

This decision assumes:

* Windows 11 provides full MSIX support.
* Opure can run as a full-trust packaged desktop application.
* Packaged processes can launch the Runtime, Worker and Plugin Host.
* Named pipes work between those processes.
* No machine-wide service or driver is required.
* Mutable state can live under user-owned local application data.
* Package identity can be detected explicitly.
* `dotnet publish` can produce the application payload.
* Self-contained deployment provides the best first-run reliability.
* Package size is acceptable for initial previews.
* A signing decision will be accepted before public release.
* GitHub Releases can host direct package artefacts.
* Microsoft Store distribution can be added later.
* Separate package identities can isolate stable and preview channels.
* Source repositories remain outside all package-owned state.

---

## 10. Current Platform Evidence

Current official platform documentation establishes that:

* MSIX is Microsoft's modern Windows application package format.
* MSIX provides package identity, installation, repair, uninstall and update support.
* Full-trust packaged desktop applications can run at medium integrity.
* Packaged applications should not write into their installation directory.
* Windows 11 supports core MSIX functionality.
* Package identity uses `Major.Minor.Build.Revision`.
* The package major version cannot be zero.
* Updates occur within one package family based on package name and publisher.
* Ordinary updates require a higher package version.
* MSIX can use block-map differential updates.
* An installed MSIX can later move to an MSIX bundle, but an installed bundle should remain a bundle.
* App Installer can install signed MSIX files.
* `.appinstaller` files can configure update prompts, background checks and forced activation blocking.
* MSIX packages must be signed by a certificate trusted on the target device.
* self-signed certificates are suitable for development and controlled testing only;
* `dotnet publish` is the supported .NET deployment preparation command;
* self-contained deployment bundles the .NET runtime;
* and self-contained runtime security servicing requires a new application release.

All exact APIs, manifest schemas, signing choices and Windows SDK versions must be revalidated before acceptance.

---

## 11. Options Considered

The principal options were:

1. Full MSIX package.
2. MSI built with WiX.
3. Third-party EXE installer and updater framework.
4. Packaging with external location.
5. Portable ZIP only.
6. Microsoft Store only.
7. ClickOnce.
8. Custom installer.
9. Multiple equally supported formats from the first release.

---

## 12. Option A — Full MSIX Package

### Advantages

* modern Windows package identity;
* per-user installation;
* clean application-file install and uninstall;
* mandatory signing;
* Windows-managed registration;
* differential update capability;
* Store eligibility;
* enterprise management support;
* no custom installer executable;
* strong separation of package files and writable state;
* full-trust Win32 support;
* multiple executable support;
* source-based reproducible construction;
* and lower installer-maintenance burden.

### Disadvantages

* mandatory signing;
* early publisher-identity commitment;
* immutable installation directory;
* numeric package-version constraints;
* no custom install or uninstall actions;
* no ordinary custom installation-directory chooser;
* packaging-specific diagnostics;
* and potential SmartScreen reputation friction for a new publisher.

### Decision

Selected provisionally.

---

## 13. Option B — MSI with WiX

### Advantages

* mature enterprise format;
* flexible per-user and per-machine scope;
* repair and upgrade support;
* configurable install directory;
* and broad Windows Installer tooling.

### Disadvantages

* significant authoring complexity;
* upgrade-component rules;
* custom-action risk;
* no package identity without an additional sparse package;
* separate updater requirement;
* more signing and rollback surface;
* and no existing installer legacy that Opure needs to preserve.

### Decision

Not selected as primary. Reserved as a future enterprise fallback.

---

## 14. Option C — EXE Installer and Updater Framework

### Advantages

* integrated installation and update;
* per-user operation;
* cross-platform potential;
* and flexible lifecycle hooks.

### Disadvantages

* substantial third-party supply-chain dependency;
* packaging and updating become coupled;
* product must own update security and rollback;
* package identity may be absent;
* and privileged lifecycle hooks increase the trust surface.

### Decision

Deferred to the Updater ADR rather than selected as the installer foundation.

---

## 15. Other Options

Packaging with external location is rejected because Opure has no legacy installer.

ZIP-only distribution is retained only for diagnostics.

Store-only distribution is rejected because previews, offline testing and enterprise deployment need a direct artefact.

ClickOnce offers no decisive advantage.

A custom installer is rejected because it would reinvent privileged infrastructure.

Multiple primary formats are rejected because they would create multiple support, update and migration models.

---

## 16. Decision

Opure will provisionally adopt:

> **A signed, per-user, full-trust x64 MSIX package containing a self-contained multi-file .NET deployment as the primary Windows installer, with all persistent user-owned state outside package ownership and an unpackaged ZIP retained only as a diagnostic artefact.**

This decision does not approve:

* machine-wide installation by default;
* administrator elevation;
* services or drivers;
* custom install actions;
* custom uninstall actions;
* automatic startup;
* background update checks;
* forced updates;
* Package Support Framework fixups;
* MSI as the primary format;
* a Store-only release;
* or several equally supported installer formats.

---

# 17. Primary Distribution Artefact

The initial primary artefact is:

```text
Opure-<SemVer>-win-x64.msix
```

Example:

```text
Opure-0.4.0-preview.1-win-x64.msix
```

When ARM64 is supported, the primary artefact may become an `.msixbundle`. The transition must be tested before release.

---

# 18. Installation Scope

The package installs per user.

Ordinary installation must:

* require no UAC elevation;
* make no machine-wide configuration change;
* install no service or driver;
* and affect only the current Windows user's package registration.

Enterprise administrators may provision the same package for managed devices through Windows management tooling. That is an enterprise deployment action, not Opure's default installer behaviour.

---

# 19. Runtime Model

The package uses the appropriate full-trust desktop declaration, conceptually:

```xml
uap10:RuntimeBehavior="packagedClassicApp"
uap10:TrustLevel="mediumIL"
```

The exact namespaces and schema version must be validated against the selected Windows SDK.

MSIX is not treated as Opure's internal security sandbox. Plugin, secret, process and project boundaries remain enforced by Opure services.

---

# 20. Application Entries

The package exposes one ordinary Start application:

```text
Opure
```

or:

```text
Opure Preview
```

The Runtime, Worker and Plugin Host are internal process hosts and do not receive normal Start entries.

Internal hosts must validate their authenticated bootstrap contract and must not start unrestricted operation when launched directly.

A separate Recovery entry may be added only when the recovery journey is implemented and tested.

---

# 21. Package Process Layout

Conceptually:

```text
MSIX package
├── Opure.Desktop.exe
├── Opure.Runtime.exe
├── Opure.Worker.exe
├── Opure.PluginHost.exe
├── first-party managed assemblies
├── native Windows dependencies
├── package assets
├── package manifest
└── resource metadata
```

All mutable state remains outside this layout.

---

# 22. .NET Deployment Mode

The primary package uses:

```text
Release
win-x64
self-contained
multi-file
```

## 22.1 Self-Contained Rationale

A self-contained deployment:

* requires no separately installed .NET runtime;
* uses the exact runtime tested with the product;
* supports offline operation;
* and provides a predictable first-run experience.

The cost is a larger package and the requirement to release an Opure update when the bundled .NET runtime needs a security fix.

## 22.2 Runtime Security

The release process must inventory the bundled runtime and track its servicing status. A high-severity .NET runtime vulnerability may require an Opure patch even if application source is unchanged.

---

# 23. Single-File, Trimming and ReadyToRun

Single-file publication is disabled initially because Opure has several hosts, native dependencies, plugins and a need for transparent component inventory.

Trimming is disabled initially because Avalonia, serializers, dependency injection, generated protocols, reflection and plugins require broad compatibility evidence.

ReadyToRun is disabled initially because its package-size and differential-update cost has not been justified by measured startup benefit.

Native AOT is not selected for product hosts.

Any change requires:

* package-size measurement;
* startup measurement;
* plugin tests;
* serialization tests;
* crash diagnostics;
* and release acceptance.

---

# 24. Publish and Package Composition

Every executable host is prepared with the supported `dotnet publish` process.

A packaging stage then creates one package layout from the host outputs.

The composition stage must:

* use an explicit file inventory;
* verify architecture;
* verify version;
* reject unexpected files;
* compare hashes for path collisions;
* and fail when two different files target the same package path.

The first implementation may keep host outputs in separate subdirectories. Correct operation is more important than premature runtime-file deduplication.

A later optimisation may deduplicate identical files only after verifying the same hash, version, runtime contract and native-load expectation.

---

# 25. Source-Based Package Construction

The package is built from:

* source-controlled manifest templates;
* generated version values;
* published host files;
* approved assets;
* approved licence notices;
* and generated package and component manifests.

Opure will not:

* build an MSI or EXE and convert it;
* use installation-capture packaging;
* scrape a developer machine;
* or include a whole output directory blindly.

---

# 26. Package Build Tooling

The initial source-based toolchain should use approved Windows SDK tooling, expected to include:

* `MakeAppx.exe`;
* `MakePri.exe` when required;
* and `SignTool.exe`.

The release process must record:

* Windows SDK version;
* tool file versions;
* tool path identity;
* and tool hashes where practical.

The build may not choose an arbitrary newest Windows SDK installed on the runner.

The preview Windows App Development CLI is not required while it remains preview.

The package implementation may live under:

```text
src/Packaging/Opure.Packaging.Windows/
```

with substantial validation in a tested repository tool.

---

# 27. Manifest Ownership

The source template should live under the packaging project.

Conceptual files:

```text
AppxManifest.template.xml
Assets/
PackageMapping.template.txt
README.md
```

The final generated manifest belongs under `artifacts/`.

Manifest validation must fail on:

* invalid schema;
* identity mismatch;
* missing asset;
* version mismatch;
* architecture mismatch;
* unsupported capability;
* or unexpected extension.

---

# 28. Package Identity

Before the first public preview, freeze:

* package name;
* publisher distinguished name;
* display name;
* preview name;
* and expected package family names.

The manifest publisher must exactly match the signing certificate subject.

Changing package name or publisher breaks ordinary update continuity and requires a migration plan and a new decision.

---

# 29. Channel Identities

Use separate package identities:

```text
Stable:       Opure
Preview:      Opure.Preview
Development:  Opure.Dev
```

Exact package names remain subject to reservation and publisher validation.

The preview identity covers:

* preview;
* beta;
* and release-candidate builds.

This lets testers update through the prerelease sequence while keeping stable installation and data separate.

The development identity is used only for local and CI packaging tests and is never distributed as a public channel.

---

# 30. Side-by-Side Channels

Stable, Preview and Development may be installed together.

Each channel has its own:

* package identity;
* Start entry;
* default profile;
* IPC endpoint namespace;
* Runtime identity;
* logs;
* Vault;
* plugins;
* and package-update line.

A Preview installation does not silently open or migrate the Stable profile.

A future explicit export/import workflow may move selected data.

---

# 31. Product SemVer and MSIX Version

Product SemVer remains the user-facing product version.

MSIX uses a separate four-part numeric deployment version because:

* it cannot express SemVer labels;
* it requires a non-zero major;
* and package update ordering is numeric.

The initial mapping is:

```text
MSIX Major    = Product SemVer Major + 1
MSIX Minor    = Product SemVer Minor
MSIX Build    = Product SemVer Patch
MSIX Revision = Channel code
```

Channel codes are:

```text
preview.N = 10000 + N
beta.N    = 20000 + N
rc.N      = 30000 + N
stable    = 60000
```

Public prerelease `N` must be between `1` and `9999`.

Examples:

| Product SemVer    | MSIX version  |
| ----------------- | ------------- |
| `0.1.0-preview.1` | `1.1.0.10001` |
| `0.1.0-beta.1`    | `1.1.0.20001` |
| `0.1.0-rc.1`      | `1.1.0.30001` |
| `0.1.0`           | `1.1.0.60000` |
| `0.1.1-preview.1` | `1.1.1.10001` |
| `1.0.0-rc.2`      | `2.0.0.30002` |
| `1.0.0`           | `2.0.0.60000` |

This preserves:

```text
preview < beta < rc < stable < next patch preview
```

The application displays product SemVer prominently. The numeric MSIX version is diagnostic deployment metadata.

Microsoft Store submission may impose additional version constraints. A Store distribution decision must validate or replace this mapping without changing product SemVer.

---

# 32. Development Package Version

Development packages use the `Opure.Dev` identity and do not participate in public update ordering.

Their numeric version may use a CI or local monotonic sequence, but it must:

* remain within MSIX limits;
* avoid collision on one test machine;
* and never be published under Stable or Preview identity.

Pull-request jobs must not create a production-identity package.

---

# 33. Architecture and Minimum Windows Version

The first package declares x64.

It must not declare a neutral architecture while containing x64 executable code.

The minimum target is the lowest Windows 11 build accepted by the product support policy.

The manifest must not imply Windows 10 support when it is not tested.

The release manifest records the maximum Windows build tested.

ARM64 requires a separate test matrix and a package-to-bundle transition.

x86 is not planned.

---

# 34. Installation Directory

Windows selects the protected package installation path.

Opure does not offer a custom installation-directory chooser.

The product must never write:

* settings;
* logs;
* databases;
* plugins;
* models;
* temporary files;
* update files;
* or user content

into that directory.

Processes receive explicit working directories and must not depend on shortcut current-directory behaviour.

Package resources are accessed through application-base, embedded-resource or package APIs.

---

# 35. Persistent State Model

Mutable state is classified as:

* Durable User State;
* Durable Security State;
* Recoverable Application State;
* Cache;
* Diagnostics;
* Temporary;
* User Project;
* Plugin;
* Model;
* and Package Registration.

Proposed default profile roots:

```text
%LOCALAPPDATA%\Opure\Stable\
%LOCALAPPDATA%\Opure\Preview\
%LOCALAPPDATA%\Opure\Development\
```

The exact names require filesystem and migration tests.

These roots are outside package-owned application data so ordinary uninstall does not destroy durable state.

The prototype must verify that full-trust packaged processes reach these paths without unintended redirection.

---

# 36. Durable User and Security State

Durable state may include:

* project registrations;
* settings;
* workflow state;
* project memory;
* Trust Centre records;
* plugin registration;
* local provider configuration;
* SQLite databases;
* recovery records;
* Vault keyring;
* encrypted secret records;
* and security policy.

None belongs in package files.

Package update and repair must not replace it.

---

# 37. Source Repositories

Developer source repositories remain in developer-selected locations.

The package and installer own no project source file.

Install, repair, update, uninstall and cleanup must not modify or remove repositories unless a separate reviewed project operation explicitly does so.

---

# 38. Plugins

Third-party plugins live under the channel profile, conceptually:

```text
%LOCALAPPDATA%\Opure\<Channel>\Plugins\
```

They are never installed into the immutable package directory.

A bundled first-party plugin may be package-owned and product-versioned.

Plugin update behaviour remains separate from application package update behaviour.

---

# 39. Local Models

Local AI models are not embedded in the primary package because of:

* package size;
* licensing;
* hardware differences;
* update cadence;
* and developer choice.

Models live in a user-selected or Opure-managed external location.

Uninstall preserves them by default.

---

# 40. Package-Managed Application Data

Use package-managed application data only for state that is safe to lose on uninstall, such as disposable shell or activation cache.

Durable profile, Vault, project memory and plugins must not rely on it.

---

# 41. Profile Manifest

Each profile should contain a non-secret manifest recording:

* channel;
* product version last used;
* package family;
* schema versions;
* creation time;
* last successful startup;
* and recovery state.

A profile cannot be opened through another channel without an explicit supported migration or diagnostic override.

---

# 42. First Launch

First launch should:

1. detect package identity and channel;
2. resolve the correct profile root;
3. validate profile ownership and version;
4. create required directories;
5. initialise safe local configuration;
6. start the Runtime;
7. and enter normal, Safe or Recovery state honestly.

First launch must not:

* require a network connection;
* download a model;
* authenticate to a cloud provider;
* or enable background updates automatically.

---

# 43. Windows Registration

Initially the package registers only what the product implements and tests.

It may register:

* one Start application;
* product display name;
* publisher display name;
* icons;
* and package identity.

It does not initially register:

* Desktop shortcut;
* forced taskbar pin;
* startup task;
* service;
* scheduled task;
* driver;
* PATH entry;
* file association;
* URI protocol;
* Explorer extension;
* or shell context menu.

A future CLI may use an application execution alias after collision and activation policy is defined.

---

# 44. App Installer and Automatic Updates

The first distribution does not use an `.appinstaller` file as the default.

Reason:

* update checks are network activity;
* App Installer can check or apply updates outside Opure's normal consent and Trust Centre path;
* update prompting, blocking and rollback have not yet been accepted.

The first public package can be installed directly by opening the signed `.msix`.

A future Updater ADR may adopt `.appinstaller` with:

* explicit consent;
* visible source;
* no background task by default;
* prompt before apply;
* no activation block by default;
* and post-update Trust Centre reconciliation.

---

# 45. Update and Downgrade

A newer package in one family may replace the installed package when package name and publisher match and the numeric version increases.

On first launch after update, Opure performs service-owned migrations.

If migration fails, the application enters Recovery Mode and does not claim success.

Technical MSIX downgrade does not reverse:

* databases;
* Vault format;
* workflow state;
* plugin state;
* or project memory.

Ordinary downgrade is therefore disabled by policy.

A supported downgrade requires exact version-pair tests, backup and schema compatibility.

---

# 46. Repair

Windows package repair restores package files.

Repair must not replace or erase external profile state.

Package corruption and application-state corruption are separate conditions.

---

# 47. Uninstall

Ordinary uninstall removes:

* package files;
* Start registration;
* package identity registration;
* package manifest integrations;
* and package-owned disposable state.

It preserves by default:

* source repositories;
* durable Opure profiles;
* Vault;
* databases;
* recovery records;
* plugins;
* local models;
* and user exports.

The product and documentation must state clearly:

> Uninstalling Opure removes the application but preserves your Opure profile, Vault, project metadata, plugins and source repositories unless you remove them separately.

This is safer than attempting destructive custom uninstall behaviour that MSIX does not support.

---

# 48. Explicit Cleanup

Opure should provide separately scoped cleanup actions:

* clear cache;
* clear diagnostics;
* reset UI settings;
* remove one project registration;
* remove project memory;
* remove plugins;
* revoke secrets;
* remove a Vault;
* remove one channel profile;
* or remove all Opure-owned external state.

Cleanup must:

* preview paths and categories;
* identify the owning profile;
* exclude source repositories;
* require confirmation for durable or security data;
* use ADR-0009 path validation;
* and create a local receipt without secret values.

Reset and uninstall are distinct operations.

---

# 49. Reinstall

Reinstallation detects preserved profile state.

If the profile was last opened by a newer incompatible product version, the application must enter Recovery Mode or request a supported migration.

It must not open newer state blindly.

---

# 50. Package Support Framework

The Package Support Framework is not selected.

Opure owns its source and should correct incompatible current-directory or install-directory assumptions directly.

A future PSF fixup requires a separate decision explaining why source correction is impractical.

---

# 51. Package Capabilities

The manifest requests only implemented capabilities.

Full-trust desktop execution is expected.

Every additional capability requires:

* an owning feature;
* security rationale;
* privacy impact;
* developer-visible behaviour;
* and acceptance tests.

Package capability does not replace Opure policy. Network access remains governed by the Network Gateway, filesystem access by Workspace and Patch services, and process creation by the Process Supervisor.

---

# 52. Runtime Bootstrap and Child Processes

The Desktop locates internal hosts through the package installation base or a generated component manifest.

It does not search the registry for Runtime binaries.

Before launch, Opure validates:

* expected relative path;
* file existence;
* architecture;
* component version;
* package context;
* and optional file hash or signature.

Each child process receives a controlled profile or temporary working directory.

Internal hosts must not use the package directory as writable working state.

---

# 53. Native Dependencies

All native dependencies needed for offline operation must be packaged or represented by an explicit approved Windows dependency.

The first launch must not download a missing runtime.

The release inventory records:

* native file;
* architecture;
* version;
* source package;
* licence;
* and signature where available.

Any Visual C++ runtime requirement must use a supported app-local or framework dependency model rather than an undisclosed prerequisite installer.

---

# 54. Assets and Localisation

The package includes required:

* logos;
* Start icons;
* target-size assets;
* display names;
* publisher display name;
* description;
* and British English resources.

CI validates asset dimensions, format, transparency and manifest references.

Stable, Preview and Development should be visually distinguishable while preserving the Opure brand.

The description must remain factual and avoid unsupported marketing claims.

---

# 55. Signing Boundary

A public MSIX requires a trusted signing path.

The Signing ADR must choose among:

* Microsoft Store signing;
* Microsoft-managed signing where eligible;
* an organisation-validated certificate;
* a hardware-backed certificate;
* or another approved trusted provider.

Before public release:

* the publisher distinguished name is final;
* the manifest publisher matches the certificate subject;
* the private key is absent from the repository;
* signing secrets are absent from command lines and logs;
* and certificate renewal preserves package-family continuity.

The unsigned candidate passes ordinary tests before signing credentials become available.

---

# 56. Timestamping and Verification

Public package signatures should be timestamped.

Release verification records:

* package signature result;
* certificate-chain result;
* timestamp result;
* publisher;
* package family;
* digest algorithm;
* certificate or key identifier;
* unsigned package hash;
* and signed package hash.

Any mutation after signing invalidates the candidate.

A trusted signature proves publisher identity and package integrity; it does not prove absence of defects.

---

# 57. Individual Binary Signing

The Signing ADR decides whether individual executables and DLLs are signed in addition to the outer MSIX.

If binaries are signed:

1. build;
2. sign binaries;
3. verify binaries;
4. compose package;
5. sign package;
6. verify package;
7. test final package.

No later binary change is permitted.

The diagnostic ZIP should use the same verified binaries where possible.

---

# 58. Development Certificates

Development packages use an obviously non-production certificate subject.

The setup procedure must:

* explain that it is test-only;
* make the trust-store change explicit;
* describe machine-wide trust impact;
* provide removal instructions;
* and never instruct public users to trust a development certificate.

A production package must not contain or install a root certificate.

---

# 59. Publisher Reputation

A valid certificate may not provide immediate SmartScreen reputation for a new publisher.

Opure documentation must not advise users to disable SmartScreen or antivirus.

It should provide:

* publisher identity;
* signature-verification instructions;
* checksums;
* and a support route for false positives.

---

# 60. Build and Release Pipeline

The packaging flow is:

```text
Trusted source commit
    ↓
Locked restore
    ↓
Release build
    ↓
Release tests
    ↓
Publish host outputs
    ↓
Compose deterministic package layout
    ↓
Generate manifest and file mapping
    ↓
Build unsigned MSIX
    ↓
Inspect and test unsigned layout
    ↓
Sign exact MSIX
    ↓
Verify signature and timestamp
    ↓
Install final signed MSIX on clean Windows
    ↓
Run package acceptance tests
    ↓
Promote exact package
```

The signing job must not restore or compile source.

The publication job must not rebuild or repackage.

---

# 61. Package Reproducibility

The unsigned package should be reproducible as far as the Windows SDK and compression format permit.

Control:

* input file inventory;
* file ordering;
* source manifest;
* version mapping;
* packaging tools;
* resource generation;
* and timestamps where safe.

Record both unsigned and signed hashes.

A signature timestamp intentionally changes the final package.

---

# 62. Package Content Allowlist

The composition step includes only declared files.

It rejects:

* PFX and private key files;
* developer settings;
* test databases;
* coverage;
* test results;
* logs;
* crash dumps;
* source-control metadata;
* local tool caches;
* real credentials;
* and Debug outputs.

Production package contents must be Release configuration and free from test bypasses, fault-injection endpoints and fake credentials.

---

# 63. Privacy and Secrets Scan

Before package creation and before upload, scan for:

* secrets;
* local user paths;
* test credentials;
* private fixtures;
* debug artefacts;
* environment dumps;
* and unapproved network configuration.

No secret value may appear in:

* package;
* manifest;
* component inventory;
* build logs;
* signing logs;
* or release evidence.

---

# 64. Release Assets

Proposed initial release files:

```text
Opure-<SemVer>-win-x64.msix
Opure-<SemVer>-win-x64-diagnostic.zip
Opure-<SemVer>-checksums.txt
Opure-<SemVer>-sbom.spdx.json
Opure-<SemVer>-components.json
Opure-<SemVer>-release-evidence.zip
```

The primary file name uses product SemVer, not the internal numeric MSIX deployment version.

The release note may show both versions in a diagnostic section.

---

# 65. Diagnostic ZIP

The secondary ZIP supports:

* packaging diagnosis;
* emergency recovery;
* CI smoke testing;
* and controlled technical support.

It is not the recommended installation.

The diagnostic build:

* has no package identity;
* creates no Start entry;
* creates no file association;
* creates no service or scheduled task;
* creates no updater registration;
* uses an isolated diagnostic profile by default;
* and must not silently open Stable or Preview data.

Deleting the extracted directory removes its application files. Profile cleanup remains explicit.

A true portable mode, especially one storing Vault state beside the executable, is not approved.

---

# 66. Direct Distribution

Initial direct distribution may use immutable GitHub Releases.

Public instructions should identify:

* exact version;
* x64 architecture;
* Windows 11 requirement;
* package publisher;
* signature state;
* checksum;
* profile-preservation behaviour;
* uninstall behaviour;
* and the absence of automatic updating in the initial release.

No unsigned package should appear beside the signed production package under a confusingly similar name.

---

# 67. Microsoft Store

Store distribution remains a future additional channel.

Potential advantages include:

* Store signing;
* user trust;
* Store-managed update;
* discovery;
* and enterprise acquisition.

Store adoption requires:

* publisher account;
* product identity reservation;
* Store policy review;
* privacy and support information;
* certification;
* update-channel reconciliation;
* and validation of the MSIX numeric version mapping.

The same tested application payload should be reused where possible, while Store-specific manifest or signing differences remain documented.

---

# 68. Enterprise Distribution

MSIX is the initial enterprise artefact.

Enterprise documentation may later cover tested deployment through tools such as Intune or Configuration Manager.

A separate MSI becomes justified only when a real requirement cannot be met by:

* direct signed MSIX;
* Store distribution;
* or enterprise MSIX management.

Adding MSI requires a separate ADR covering WiX version and licence, component rules, upgrade codes, scope, repair, signing, uninstall and identity migration.

---

# 69. Install, Update, Repair and Uninstall Tests

The acceptance suite must cover:

## 69.1 Installation

* direct double-click;
* scripted install;
* no UAC;
* current-user scope;
* correct publisher;
* correct product and package versions;
* Start entry;
* package identity;
* and offline launch.

## 69.2 Processes

* Desktop;
* Runtime;
* Worker;
* Plugin Host;
* authenticated IPC;
* cancellation;
* shutdown;
* and orphan cleanup.

## 69.3 State

* profile creation;
* databases;
* Vault;
* plugins;
* diagnostics;
* no package-directory write;
* and channel isolation.

## 69.4 Repair

* missing or damaged package file;
* package repair;
* durable profile unchanged;
* and successful relaunch.

## 69.5 Update

* preview to preview;
* preview to beta;
* beta to RC;
* stable patch;
* running-process shutdown;
* interrupted workflow;
* service migration;
* and failed migration recovery.

## 69.6 Uninstall

* application files removed;
* registration removed;
* profile preserved;
* Vault preserved;
* plugins preserved;
* models preserved;
* source repositories preserved;
* and reinstall finds existing state.

## 69.7 Cleanup

* cache only;
* diagnostics only;
* one project memory store;
* plugins;
* Vault;
* one profile;
* and all Opure-owned state without touching decoy repositories.

---

# 70. Version Ordering Tests

The packaging tests must prove numeric MSIX ordering for:

```text
preview.1
preview.2
beta.1
rc.1
stable
next patch preview.1
```

They must also reject:

* prerelease number zero;
* prerelease number over 9999;
* unsupported labels;
* product major over the mapping limit;
* manually supplied inconsistent MSIX version;
* and a package version lower than the installed channel version.

---

# 71. Package Identity Tests

Verify:

* Stable package name;
* Preview package name;
* Development package name;
* publisher;
* package family;
* architecture;
* channel;
* manifest version;
* and product-version metadata.

A public package must not use `Opure.Dev`.

---

# 72. Signing Tests

Test:

* valid signature;
* wrong publisher;
* invalid chain;
* expired or revoked certificate conditions where testable;
* invalid timestamp;
* modified package;
* unsigned package;
* untrusted development certificate;
* and post-sign hash mismatch.

---

# 73. Security Tests

Test:

* package tamper;
* DLL substitution;
* executable substitution;
* package-path write;
* package identity spoof;
* unsafe activation;
* plugin-path confusion;
* channel-profile confusion;
* cleanup traversal;
* source-repository deletion attempt;
* and signing-secret leakage.

---

# 74. Fault Injection and Recovery

Inject failure:

* during package layout generation;
* before package creation;
* after unsigned package creation;
* during signing;
* after signing;
* during install;
* during update shutdown;
* during profile migration;
* and during explicit cleanup.

A failed package operation must not be reported as success.

A failed profile migration enters Recovery Mode.

---

# 75. Package Performance

Measure:

* total package size;
* compressed size;
* .NET runtime contribution;
* duplicate file contribution;
* install duration;
* first launch;
* warm launch;
* Runtime readiness;
* repair duration;
* uninstall duration;
* and update delta when updates are introduced.

Package-size optimisation must not weaken correctness or diagnostics.

---

# 76. Runtime Deduplication Prototype

Evaluate whether published hosts can share identical runtime files safely in one package.

A file may be deduplicated only when:

* target path is intentional;
* hashes match;
* version metadata matches;
* architecture matches;
* runtime configuration remains valid;
* and all host launch tests pass.

Until then, separate subdirectories are acceptable.

---

# 77. Clean-Machine Environment

Public package acceptance runs on Windows 11 without requiring:

* .NET SDK;
* Visual Studio;
* Git;
* PowerShell 7;
* developer certificate;
* or internet connection after installation.

The final signed package must be tested on a machine that did not build or sign it.

---

# 78. Safe Mode and Recovery Mode

Safe Mode must launch from the installed package with:

* third-party plugins disabled;
* optional providers disabled;
* diagnostics available;
* and no package-directory mutation.

Recovery Mode must work when:

* database migration fails;
* Vault is unavailable;
* workflow state is incomplete;
* or the Runtime cannot complete normal startup.

---

# 79. Package Identity Abstraction

Provide one Windows platform service exposing:

* packaged or unpackaged state;
* package name;
* package family;
* package version;
* product version;
* channel;
* installation base;
* and architecture.

Domain services must not call package APIs directly.

Package identity may be an additional process-authentication signal, but it is not the sole IPC credential.

---

# 80. Version and Diagnostics

The About and Diagnostics views should show:

```text
Product version
Release channel
Commit SHA
Package identity
MSIX deployment version
Desktop version
Runtime version
IPC compatibility
Architecture
```

Windows Settings may display the numeric MSIX version. Opure displays product SemVer prominently.

---

# 81. Error Model

Stable package-related errors include:

* Package Build Failed;
* Manifest Invalid;
* Package Version Invalid;
* Package Identity Mismatch;
* Architecture Mismatch;
* Package Unsigned;
* Signature Invalid;
* Publisher Mismatch;
* Timestamp Invalid;
* Package Install Failed;
* Package Update Failed;
* Package Repair Failed;
* Profile Migration Failed;
* Package Context Missing;
* Diagnostic Mode Isolated;
* Cleanup Required;
* and Recovery Required.

Errors include safe version and identity detail but no secrets.

---

# 82. Trust Centre and Observability

After launch, the Trust Centre may record:

* package family;
* installed product version;
* previous version;
* signature verification result;
* channel;
* profile migration;
* and recovery outcome.

Events that occur entirely before Opure starts may exist only in Windows deployment logs. The product must not claim complete visibility into installer internals.

Metrics and logs must not use private paths or full file inventories as high-cardinality labels.

---

# 83. Repository Structure

Suggested additions:

```text
src/
└── Packaging/
    └── Opure.Packaging.Windows/
        ├── AppxManifest.template.xml
        ├── Assets/
        ├── Packaging.targets
        ├── README.md
        └── Opure.Packaging.Windows.csproj

tools/
└── Opure.Tools.Packaging/

tests/
└── Packaging/
    ├── Opure.Packaging.UnitTests/
    ├── Opure.Packaging.IntegrationTests/
    └── Opure.Packaging.AcceptanceTests/
```

Exact placement follows ADR-0010.

---

# 84. Conceptual Manifest

The final schema must be validated, but the shape is expected to resemble:

```xml
<Package
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
  xmlns:uap10="http://schemas.microsoft.com/appx/manifest/uap/windows10/10"
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  IgnorableNamespaces="uap uap10 rescap">

  <Identity
    Name="$(PackageName)"
    Publisher="$(Publisher)"
    Version="$(MsixVersion)"
    ProcessorArchitecture="x64" />

  <Properties>
    <DisplayName>$(DisplayName)</DisplayName>
    <PublisherDisplayName>$(PublisherDisplayName)</PublisherDisplayName>
    <Description>$(Description)</Description>
    <Logo>Assets\StoreLogo.png</Logo>
  </Properties>

  <Dependencies>
    <TargetDeviceFamily
      Name="Windows.Desktop"
      MinVersion="$(MinWindowsVersion)"
      MaxVersionTested="$(MaxWindowsVersionTested)" />
  </Dependencies>

  <Applications>
    <Application
      Id="Opure"
      Executable="Opure.Desktop.exe"
      uap10:RuntimeBehavior="packagedClassicApp"
      uap10:TrustLevel="mediumIL">
      ...
    </Application>
  </Applications>

  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>
</Package>
```

The packaging tool must reject unimplemented manifest extensions.

---

# 85. Developer Commands

Conceptual commands:

```powershell
pwsh .\eng\publish.ps1 -RuntimeIdentifier win-x64
pwsh .\eng\package.ps1 -Format Msix -Channel Development
pwsh .\eng\verify-package.ps1 -Input <package>
pwsh .\eng\test-package.ps1 -Input <package> -Mode CleanMachine
```

Signing commands remain controlled by the Signing ADR.

Every script:

* validates repository root;
* validates tool version;
* uses `artifacts/`;
* propagates exit codes;
* and never prints a signing secret.

---

# 86. Release Gate

A public package must not ship when:

* package name or publisher is provisional;
* the package is unsigned;
* the signature or timestamp is invalid;
* package version is inconsistent or non-monotonic;
* product version does not match;
* Debug or test files are present;
* package-directory writes occur;
* persistent state is package-owned and lost on uninstall;
* source repositories are at risk;
* any internal host fails;
* state migration is untested;
* cleanup can escape its profile;
* or the package was rebuilt after acceptance tests.

---

# 87. Acceptance Criteria

This ADR may move to **Accepted** when all of the following hold.

## Package and Install

* [ ] MSIX is the primary Windows installer.
* [ ] The package targets Windows 11 x64.
* [ ] Installation is per user.
* [ ] Installation requires no elevation.
* [ ] Full-trust medium-integrity execution is verified.
* [ ] Desktop launches from Start.
* [ ] Runtime, Worker and Plugin Host launch.
* [ ] Named-pipe IPC works.
* [ ] Internal hosts are not Start entries.
* [ ] The package is self-contained.
* [ ] No separate .NET runtime is required.
* [ ] Single-file and trimming are disabled.
* [ ] Package layout is source based.
* [ ] No installer conversion or capture is used.
* [ ] Windows SDK tooling is recorded.
* [ ] Package contents follow an allowlist.
* [ ] Forbidden files fail the build.

## Identity and Version

* [ ] Stable, Preview and Development identities are defined.
* [ ] Publisher is final before public release.
* [ ] Package family names are recorded.
* [ ] Stable and Preview install side by side.
* [ ] Profiles and IPC are isolated by channel.
* [ ] Product SemVer and MSIX version are separate.
* [ ] The mapping handles product major zero.
* [ ] Preview, Beta, RC, Stable and next-patch ordering pass.
* [ ] UI shows product SemVer.
* [ ] Diagnostics show the MSIX version.
* [ ] Pull requests cannot create public identity packages.

## State and Uninstall

* [ ] Application never writes to the package directory.
* [ ] Durable profiles are external.
* [ ] Vault, databases, plugins and models are external.
* [ ] Source repositories are outside package ownership.
* [ ] Repair preserves durable state.
* [ ] Update preserves durable state.
* [ ] Uninstall removes application files.
* [ ] Uninstall preserves durable profiles.
* [ ] Uninstall preserves Vault and source repositories.
* [ ] Cleanup is explicit, scoped and previewed.
* [ ] Cleanup cannot delete source repositories.
* [ ] Reinstall detects preserved state safely.

## Signing and Release

* [ ] Public package is signed.
* [ ] Manifest publisher matches certificate subject.
* [ ] Signature chain validates.
* [ ] Timestamp validates.
* [ ] Modified package fails validation.
* [ ] No signing key exists in the repository.
* [ ] No signing secret appears in logs.
* [ ] Signed package is tested on a clean machine.
* [ ] Signed hash, component manifest, SBOM and licence inventory exist.
* [ ] Publication promotes the exact tested package.
* [ ] No compile or package step occurs during publication.

## Product Behaviour

* [ ] First launch works offline.
* [ ] No model downloads automatically.
* [ ] No cloud authentication occurs automatically.
* [ ] No automatic update association is enabled initially.
* [ ] No background or forced update is configured.
* [ ] No service, driver, scheduled task or startup entry exists.
* [ ] No PATH mutation or shell extension exists.
* [ ] Package Support Framework is not required.
* [ ] Safe Mode works.
* [ ] Recovery Mode works.
* [ ] Diagnostic ZIP is isolated and creates no registration.
* [ ] Package size and startup are measured.
* [ ] Security review is complete.
* [ ] Founder approval is recorded.

---

# 88. Evidence Required Before Acceptance

* [ ] Package-format comparison.
* [ ] Windows support baseline.
* [ ] Source manifest.
* [ ] Package identity record.
* [ ] Publisher decision.
* [ ] Version-mapping tests.
* [ ] Self-contained publish report.
* [ ] Package layout and collision report.
* [ ] Unsigned package hash.
* [ ] Development-signing report.
* [ ] Production-signing plan.
* [ ] Signature and timestamp report.
* [ ] Clean-machine install report.
* [ ] No-elevation report.
* [ ] Process-topology and IPC report.
* [ ] Package-directory write-monitor report.
* [ ] External-profile report.
* [ ] Vault, database, plugin and repository preservation reports.
* [ ] Repair, update, uninstall and cleanup reports.
* [ ] Stable/Preview side-by-side report.
* [ ] Offline launch report.
* [ ] Safe Mode and Recovery Mode reports.
* [ ] Diagnostic ZIP report.
* [ ] Package-size and startup report.
* [ ] SBOM, licence inventory and component manifest.
* [ ] Complete release-candidate simulation.
* [ ] Security review.
* [ ] Founder approval.

---

# 89. Known Limitations

* Final package names are not selected.
* The final publisher distinguished name is not selected.
* The signing provider is not selected.
* The founder's signing-service eligibility is not verified.
* The minimum Windows 11 build is not selected.
* The Windows SDK version is not selected.
* Runtime-file deduplication is not final.
* The first package may be large.
* Automatic updates are not provided initially.
* `.appinstaller` and Store distribution are deferred.
* Enterprise MSI is deferred.
* ARM64 and MSIX bundles are deferred.
* Individual binary signing is deferred.
* SmartScreen reputation cannot be guaranteed immediately.
* External profile preservation means uninstall is not a full data purge.
* True portable mode is not supported.
* Store-specific package-version mapping is deferred.
* Package-family migration is not implemented.
* Permanent release-evidence storage is deferred.

---

# 90. Open Questions

* What legal publisher identity will be used?
* Is the publisher an individual or organisation?
* Which signing provider is available in the United Kingdom?
* Should Microsoft Store signing be the first public trust path?
* What exact Stable, Preview and Development package names should be reserved?
* Should Beta and RC share Preview identity permanently?
* What are the final external profile paths?
* Can package-managed application data be avoided entirely?
* Which Windows 11 build is the minimum?
* Which Windows SDK is pinned?
* Should MakeAppx remain the package builder?
* Is MakePri required?
* Should the first artefact be an MSIX or bundle?
* When should ARM64 be added?
* Should individual binaries be signed?
* Should the diagnostic ZIP be public for every release?
* What package-size budget is acceptable?
* Can self-contained runtime files be safely shared?
* Should ReadyToRun be enabled after measurement?
* When should an execution alias, URI protocol or file association be added?
* What update mechanism best preserves consent and Trust Centre visibility?
* Should App Installer ever perform background checks?
* How should Preview users move to Stable?
* How should a supported downgrade be expressed?
* What enterprise requirement would justify MSI?
* How should users remove preserved data after uninstall?
* Should cleanup be a separate signed utility?
* Should plugins or models ever be shared between channels?
* What Store numeric version mapping will be accepted?
* How should package-to-bundle migration be tested?
* Which deployment logs should support bundles include?

---

# 91. Deferred Decisions

This ADR intentionally defers:

* signing provider and key custody to `ADR-0014-code-signing-and-release-trust.md`;
* automatic updating to an Updater ADR;
* Store submission to a Distribution ADR;
* enterprise MSI to an Enterprise Packaging ADR;
* Windows ARM64 to a Platform Expansion ADR;
* true portable mode to a Portable Deployment ADR;
* local model distribution to a Model Management ADR;
* plugin marketplace distribution to a Plugin Distribution ADR;
* and package-family migration to a future Migration ADR.

---

# 92. Alternatives Rejected

MSI is not primary because Opure has no installer legacy requiring Windows Installer complexity.

A third-party installer/updater framework is not primary because updater consent and rollback remain undecided.

Packaging with external location is not selected because full MSIX is feasible for a new application.

ZIP-only distribution lacks package identity, repair and clean registration removal.

Store-only distribution is too restrictive for early technical previews and offline use.

ClickOnce offers no decisive advantage.

A custom installer would create an unnecessary privileged security surface.

Multiple primary formats would exceed the initial support capacity.

Package Support Framework is rejected because Opure can correct its source directly.

Framework-dependent deployment is not selected because it introduces a runtime prerequisite.

Single-file and trimming are not selected because compatibility evidence is incomplete.

---

# 93. Official and Primary Evidence References

## Windows Packaging

* [Packaging overview](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/packaging/)
* [Choose a distribution path](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/choose-distribution-path)
* [What is MSIX?](https://learn.microsoft.com/en-us/windows/msix/overview)
* [MSIX documentation](https://learn.microsoft.com/en-us/windows/msix/)
* [MSIX on Windows 10 and Windows 11](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/msix-windows10-windows11)
* [MSIX supported platforms](https://learn.microsoft.com/en-us/windows/msix/supported-platforms)
* [Packaged desktop application runtime](https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-behind-the-scenes)
* [Detect package identity](https://learn.microsoft.com/en-us/windows/msix/detect-package-identity)
* [Package Identity manifest element](https://learn.microsoft.com/en-us/uwp/schemas/appxpackage/uapmanifestschema/element-f-identity)
* [App package updates](https://learn.microsoft.com/en-us/windows/msix/app-package-updates)
* [Plan MSIX deployment](https://learn.microsoft.com/en-us/windows/msix/desktop/managing-your-msix-deployment-targetdevices)
* [Package Support Framework](https://learn.microsoft.com/en-us/windows/msix/psf/package-support-framework-overview)

## App Installer

* [App Installer overview](https://learn.microsoft.com/en-us/windows/msix/app-installer/app-installer-root)
* [App Installer file overview](https://learn.microsoft.com/en-us/windows/msix/app-installer/app-installer-file-overview)
* [Update settings](https://learn.microsoft.com/en-us/windows/msix/app-installer/update-settings)
* [Auto-update and repair](https://learn.microsoft.com/en-us/windows/msix/app-installer/auto-update-and-repair--overview)

## Signing

* [MSIX signing overview](https://learn.microsoft.com/en-us/windows/msix/package/signing-package-overview)
* [Sign with SignTool](https://learn.microsoft.com/en-us/windows/msix/package/sign-app-package-using-signtool)
* [Create a package-signing certificate](https://learn.microsoft.com/en-gb/windows/msix/package/create-certificate-package-signing)
* [MSIX signing end-to-end](https://learn.microsoft.com/en-us/windows/msix/package/sign-msix-package-guide)
* [Windows code-signing options](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options)

## .NET Deployment

* [`dotnet publish`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-publish)
* [.NET publishing overview](https://learn.microsoft.com/en-us/dotnet/core/deploying/)
* [Single-file deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview)
* [Trim self-contained applications](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trim-self-contained)
* [Self-contained runtime servicing](https://learn.microsoft.com/en-us/dotnet/core/deploying/runtime-patch-selection)

## Alternative Installer Evidence

* [WiX Package element](https://docs.firegiant.com/wix/schema/wxs/package/)
* [WiX installation scope](https://docs.firegiant.com/wix/schema/wxs/packagescopetype/)
* [Velopack](https://github.com/velopack/velopack)

All exact versions, eligibility rules, schemas and platform features must be revalidated before public release.

---

# 94. Review Record

| Date         | Reviewer           | Decision | Notes                                                             |
| ------------ | ------------------ | -------- | ----------------------------------------------------------------- |
| 18 July 2026 | Architecture draft | Proposed | Signed per-user full MSIX with external durable state recommended |

---

# 95. Approval

## Founder or Product Approval

* **Name:** Christopher Dyer
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Pending founder review

## Packaging Approval

* **Name or role:** Packaging and Installation Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Manifest, identity, install, repair and uninstall evidence required

## Release Engineering Approval

* **Name or role:** Release Engineering Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Exact artefact promotion and release evidence required

## Security Approval

* **Name or role:** Security Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Signing, identity, package tamper, profile and cleanup review required

## Runtime Approval

* **Name or role:** Runtime Architecture Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Child process and IPC operation in package required

## Persistence and Recovery Approval

* **Name or role:** Persistence and Recovery Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** External data, migration, uninstall and Recovery Mode evidence required

## Test Approval

* **Name or role:** Test Architecture Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Clean-machine package acceptance suite required

---

# 96. Supersession

This ADR is superseded only when a later ADR:

* names ADR-0013 explicitly;
* explains why package format, identity, installation scope or deployment mode changed;
* identifies affected channels and user data;
* describes package-family and profile migration;
* explains signing, update and uninstall impact;
* and updates the `Superseded by` field.

Published package identities remain historical facts.

---

# 97. Change History

| Version | Date         | Author        | Summary                                                             |
| ------- | ------------ | ------------- | ------------------------------------------------------------------- |
| 0.1     | 18 July 2026 | Founder Draft | Initial signed per-user MSIX packaging and installer recommendation |

---

# 98. Final Decision Statement

> **Opure will provisionally use a signed, per-user, full-trust x64 MSIX package containing a self-contained multi-file .NET deployment as its primary Windows installer, with stable, preview and development identities, deterministic product-SemVer-to-MSIX version mapping, immutable package files, all durable profiles, Vaults, plugins, models and source repositories outside package ownership, explicit cleanup instead of uninstall-time deletion, and an unpackaged ZIP retained only for diagnostics, because installation must provide Windows-native identity, integrity, repair and application-file removal without unnecessary elevation, hidden update behaviour or risk to developer-owned data.**

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**