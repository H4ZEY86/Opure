# ADR-0015 — Updater and Update Policy

## Architecture Decision Record

**Status:** Proposed
**Date:** 18 July 2026
**Decision owners:** Founder and Product Owner
**Technical owners:** Update and Lifecycle Owner
**Reviewers:** Packaging Owner, Release Engineering Owner, Security Owner, Runtime Architecture Owner, Persistence Owner, Recovery Owner, Trust Centre Owner, Network Gateway Owner, Desktop Owner, Test Architecture Owner
**Supersedes:** None
**Superseded by:** None
**Related ADRs:** ADR-0001 through ADR-0014
**Related specifications:** CHARTER-001, SPEC-001 through SPEC-012
**Target milestone:** First public preview through Version 1.0

---

## 1. Decision Summary

Opure should use an **application-controlled, user-initiated update policy with Windows App Installer as the package-deployment authority**.

The update architecture should:

* use the trusted Opure Runtime as the Update Coordinator;
* route every network update check through the Network Gateway;
* expose every update check and decision through the Trust Centre;
* use one HTTPS `.appinstaller` source per public channel;
* use no `UpdateSettings` element in the initial `.appinstaller` files;
* therefore configure no Windows automatic launch-time checks;
* configure no Windows background update task;
* configure no silent application of updates;
* configure no activation-blocking update;
* configure no downgrade through `ForceUpdateFromAnyVersion`;
* make **Manual Only** the default update-check policy;
* offer an explicit opt-in **Check on Launch** policy;
* rate-limit opt-in launch checks to no more than once every 24 hours by default;
* run no hidden updater process, service, scheduled task or background task;
* use no device identifier, account identifier, project identifier or telemetry identifier in an update request;
* validate update-origin, package identity, publisher, channel, architecture and numeric MSIX version before offering an update;
* reject any package version that is not strictly newer than the installed package version;
* remember the highest valid package version observed for the channel to detect simple metadata rollback;
* treat remote metadata as update discovery rather than the final package-trust authority;
* rely on Windows package signature, package identity, publisher continuity, package version and release evidence for final payload trust;
* show version, channel, download size, release notes, migration impact, restart requirement, support state and known issues before hand-off;
* require a deliberate **Install Update** action;
* create required backups and recovery checkpoints before package hand-off;
* quiesce the Runtime, Workers, Plugin Hosts, builds, tests and patch transactions before exit;
* open the exact trusted `.appinstaller` file with Windows App Installer;
* let Windows display publisher and installation confirmation;
* close Opure only after the developer explicitly confirms the hand-off;
* reconcile the installed version and perform service-owned migrations on the next launch;
* enter Recovery Mode rather than claim success if post-update migration fails;
* preserve source repositories, Vaults, databases, plugins, local models and other durable profile state;
* keep Stable and Preview on separate package identities, update sources, profiles and IPC namespaces;
* disable the direct updater for Microsoft Store installations and defer to Store policy;
* disable production updating in unpackaged diagnostic builds;
* and treat enterprise-managed policy as visible higher-precedence configuration rather than hidden product behaviour.

The initial direct-distribution updater should not:

* download and install a package silently;
* use `AutomaticBackgroundTask`;
* use App Installer `OnLaunch`;
* use `UpdateBlocksActivation`;
* use `ForceUpdateFromAnyVersion`;
* use the `ms-appinstaller:` browser protocol;
* request the restricted `packageManagement` capability;
* call `PackageManager` to replace the running package;
* run as a Windows service;
* run at elevated integrity;
* use a custom privileged bootstrapper;
* replace application files itself;
* write into the MSIX installation directory;
* switch release channels automatically;
* force Stable users onto Preview;
* downgrade the package;
* roll back migrated data automatically;
* update plugins or local models as part of the application package update;
* or describe HTTPS discovery metadata alone as cryptographic proof of a release.

The initial update trust model is intentionally layered:

1. HTTPS and a pinned origin protect ordinary discovery transport.
2. Strict `.appinstaller` parsing constrains the update target.
3. Local monotonic state rejects older package versions.
4. Windows App Installer verifies the signed MSIX publisher and package identity.
5. ADR-0014 signatures, timestamps, hashes and attestations verify the release.
6. ADR-0012 immutable releases prevent silent replacement.
7. Opure's post-update reconciliation verifies the installed product, package and profile transition.

This initial model does not claim full resilience to a compromised update-origin server that withholds an update.

A future TUF-conformant metadata service may be introduced when Opure needs:

* multiple mirrors;
* delegated release roles;
* offline root keys;
* threshold metadata signatures;
* explicit freeze-attack resistance;
* or independent metadata-key compromise recovery.

Opure should not implement an improvised partial TUF protocol and market it as TUF.

---

## 2. Status

Current status:

> **Proposed**

This ADR becomes **Accepted** only after the updater prototype demonstrates:

* an associated Stable development `.appinstaller`;
* a separate Preview development `.appinstaller`;
* no automatic `UpdateSettings`;
* manual update discovery through the Network Gateway;
* explicit Check on Launch consent;
* update-check rate limiting;
* no check when the preference is Manual Only;
* no check while offline;
* no unique client identifier;
* origin allowlisting;
* redirect restriction;
* size and time limits;
* strict XML parsing;
* package name, publisher, architecture and channel validation;
* numeric version comparison;
* rollback rejection;
* highest-observed-version persistence;
* release-note presentation;
* migration-impact presentation;
* user cancellation;
* package hand-off to Windows App Installer;
* Safe Shutdown preparation;
* post-update startup reconciliation;
* successful schema migration;
* failed-migration Recovery Mode;
* preserved Vault and project state;
* side-by-side Stable and Preview updates;
* Store and diagnostic modes;
* enterprise-policy visibility;
* and a full simulated security withdrawal.

---

## 3. Context

ADR-0013 selected a signed, per-user, full-trust MSIX package.

ADR-0014 selected a layered code-signing and release-trust model.

ADR-0012 selected SemVer, immutable releases and exact artefact promotion.

Those decisions establish how a package is built and trusted.

They do not decide:

* whether Opure contacts an update server;
* when it contacts the server;
* which data it sends;
* whether Windows checks independently;
* whether an update can install silently;
* whether the application can block launch;
* whether a package can downgrade;
* how active workflows are stopped;
* how project and Vault data are protected;
* how failed migrations recover;
* or how channel changes behave.

Windows provides several update mechanisms:

* Microsoft Store;
* App Installer files;
* App Installer automatic checks;
* PackageManager APIs;
* enterprise management;
* manual MSIX installation;
* and third-party updater frameworks.

Some mechanisms can check independently of the application.

Some can install without application UI.

Some can block activation.

Some require a restricted package-management capability.

Some can technically install lower package versions.

Those capabilities do not automatically align with the Opure Charter.

Opure is local by design.

An update check is network activity.

A background update is a state mutation.

A forced update can remove developer control.

A downgrade can damage migrated data.

An updater can become one of the most privileged components in the product.

The safest first design is therefore deliberately conservative:

* Opure owns consent and discovery;
* Windows owns package installation;
* the application owns data migration and recovery;
* and the developer owns the decision to apply.

---

## 4. Problem Statement

Opure requires an update architecture that can notify developers of trusted releases and hand them safely to Windows package deployment without hidden network activity, silent installation, forced activation blocking, unsafe downgrade, data loss, custom privileged updater code or ambiguity about which component authorised the change.

---

## 5. Decision Drivers

The decision is evaluated against:

* Charter alignment;
* developer consent;
* visible network activity;
* Trust Centre integration;
* signed MSIX compatibility;
* package-family continuity;
* no-elevation operation;
* no custom privileged updater;
* active-workflow safety;
* migration safety;
* recovery;
* Stable and Preview isolation;
* offline operation;
* privacy;
* release trust;
* rollback honesty;
* enterprise control;
* Store compatibility;
* small-team implementation;
* testability;
* update bandwidth;
* and future TUF or mirror migration.

---

## 6. Governing Principles

This decision must preserve:

* Developer Respect;
* Developer First;
* Local by Design;
* Cloud Optional;
* Human in Control;
* Visible by Design;
* Inspectable Decisions;
* Reversible Wherever Technically Practical;
* Least Privilege;
* No Hidden Network Activity;
* No Silent Update;
* No Forced Channel Change;
* No Unauthorised Downgrade;
* No Custom Privileged File Replacement;
* No Package Rebuild;
* No Data Migration Without Recovery;
* No False Rollback Promise;
* No User Project Deletion;
* No Vault Deletion;
* No “Latest” Without a Defined Channel;
* No Metadata Claim Beyond Its Trust;
* and Evidence-Based Confidence.

---

## 7. Scope

This ADR decides:

* update discovery;
* update-check consent;
* check frequency;
* network routing;
* update source;
* `.appinstaller` configuration;
* version and channel validation;
* release-note display;
* update availability state;
* update hand-off;
* Runtime quiescence;
* restart and resume;
* data backup;
* migration;
* failure recovery;
* Stable and Preview policy;
* Store policy;
* diagnostic-build policy;
* enterprise precedence;
* critical update policy;
* withdrawal policy;
* update logging;
* update privacy;
* and future metadata-hardening criteria.

This ADR does not decide:

* the final public update domain;
* CDN provider;
* Microsoft Store submission;
* enterprise deployment platform;
* plugin updating;
* local-model updating;
* operating-system updating;
* .NET SDK updating;
* project dependency updating;
* GitHub Action updating;
* TUF implementation;
* update metadata signing keys;
* delta format beyond native MSIX behaviour;
* Linux or macOS updates;
* or a future cross-platform updater.

---

## 8. Constraints

Known constraints include:

* The primary package is MSIX.
* Stable and Preview use different package families.
* Package updates require package-family continuity.
* MSIX uses numeric package-version ordering.
* Product SemVer and MSIX deployment version are separate.
* App Installer files can configure automatic checks and silent application.
* Automatic background checks can occur every eight hours independently of application launch.
* App Installer `OnLaunch` checks occur outside Opure's own network path.
* `UpdateBlocksActivation` can prevent application launch.
* `ForceUpdateFromAnyVersion` can permit package downgrade.
* `CheckUpdateAvailabilityAsync` currently works only for packages associated with an App Installer file.
* Microsoft's current documentation records a known issue when calling that API directly on `Package.Current`.
* code-driven PackageManager installation requires the restricted `packageManagement` capability;
* `RequestAddPackageByAppInstallerFileAsync` invokes SmartScreen and user verification but still requires that capability;
* the `ms-appinstaller:` browser protocol is disabled by default on most devices;
* direct `.appinstaller` download and double-click remains supported;
* Windows owns final MSIX signature and package-family enforcement;
* Opure must not update while authoritative state is mid-transaction;
* migrations may be irreversible;
* uninstall and package update preserve external durable state;
* Store-installed packages may use Store-managed updates;
* enterprise policy may override application preferences;
* and the first implementation team is small.

---

## 9. Assumptions

This decision assumes:

* the direct package is installed through an `.appinstaller` file with no automatic update settings;
* the associated App Installer URI remains discoverable through package metadata;
* Opure can also store an approved channel update source in product configuration;
* the Network Gateway can fetch bounded HTTPS metadata;
* the `.appinstaller` file contains the package URI and numeric version;
* Windows App Installer will verify package signing and package identity at hand-off;
* Opure can launch the downloaded `.appinstaller` file through normal Windows shell activation;
* no restricted package-management capability is needed for that hand-off;
* the developer can cancel in Windows App Installer;
* package update may require Opure processes to exit;
* the next package launch can identify the previous product and schema versions;
* service-owned migrations remain the authority;
* GitHub Releases or another immutable store can host versioned package assets;
* a separate stable channel pointer can reference the current immutable asset;
* and offline operation remains fully functional without an update check.

---

## 10. Current Platform Evidence

Official Microsoft and GitHub documentation available on 18 July 2026 establishes that:

* an App Installer file can install or update an MSIX from HTTPS, HTTP, SMB or local paths;
* the `ms-appinstaller:` URI protocol is disabled by default on most devices;
* direct download and opening of the `.appinstaller` file is the broad consumer path;
* App Installer `OnLaunch` can check when the application launches;
* `AutomaticBackgroundTask` checks every eight hours independently of launch and cannot show UI;
* `ShowPrompt` and `UpdateBlocksActivation` can control prompting and launch blocking;
* when a prompted update does not block activation, Windows can apply the update silently later;
* `ForceUpdateFromAnyVersion` allows both higher and lower package versions;
* without it, package movement is higher-version only;
* App Installer settings can be changed by Windows Settings, PowerShell or enterprise CSP, with higher-precedence policy capable of overriding developer defaults;
* `Package.GetAppInstallerInfo` can return the associated App Installer URI and policy;
* `Package.CheckUpdateAvailabilityAsync` works only for packages installed through App Installer association;
* Microsoft's documentation records a current access issue when it is called directly on `Package.Current`;
* `RequestAddPackageByAppInstallerFileAsync` invokes SmartScreen and user verification;
* that API requires the restricted `packageManagement` capability;
* native MSIX update uses block-map metadata to reduce transfer to changed blocks;
* an MSIX can update to an MSIX bundle, but a bundle should not later update back to a single MSIX;
* GitHub immutable releases prevent tag movement and release-asset modification;
* and GitHub release attestations bind release tags, commits and assets.

The implementation must revalidate APIs, App Installer schema and current Windows behaviour before acceptance.

---

## 11. Options Considered

The principal options are:

1. **Application-controlled discovery plus user hand-off to Windows App Installer**
2. **Automatic App Installer `OnLaunch` updates**
3. **Automatic App Installer background updates**
4. **In-process PackageManager updater**
5. **Third-party updater framework**
6. **Microsoft Store only**
7. **Manual browser download only**
8. **WinGet as product updater**
9. **Custom elevated updater service**
10. **TUF-conformant custom update repository from the first release**

---

# 12. Option A — Application-Controlled Discovery and App Installer Hand-Off

## 12.1 Description

Opure fetches update discovery metadata through its Network Gateway.

It displays the update.

The developer chooses to open the `.appinstaller` file.

Windows App Installer verifies and applies the signed MSIX.

---

## 12.2 Advantages

* developer controls network policy;
* Trust Centre can record the check;
* no hidden OS background check;
* no custom updater service;
* no restricted package-management capability;
* Windows verifies package signature and publisher;
* Windows owns package replacement;
* app can prepare workflows and backups;
* Stable and Preview remain isolated;
* works with direct distribution;
* supports manual or opt-in launch checks;
* update logic remains small;
* no application-file write logic;
* no privileged custom bootstrapper;
* future App Installer and Store paths remain open;
* and native differential package transfer remains possible through Windows deployment.

---

## 12.3 Disadvantages

* the developer sees a Windows hand-off rather than a fully embedded update UI;
* App Installer progress is not fully controlled by Opure;
* the application may need to exit before completion;
* post-update restart may require the developer to launch again;
* availability discovery and package installation use different components;
* an update-origin outage can prevent discovery;
* the initial metadata model does not fully resist a compromised origin withholding updates;
* and the update cannot be entirely automated.

---

## 12.4 Risks

* stale discovery metadata;
* malicious redirect;
* wrong channel;
* wrong package family;
* replayed older metadata;
* package hand-off without safe quiescence;
* migration failure;
* user confusion after cancellation;
* and policy drift in the Windows App Installer association.

---

## 12.5 Mitigations

* origin allowlist;
* strict parsing;
* no redirects by default;
* package identity checks;
* monotonic version checks;
* highest-observed version;
* explicit review screen;
* safe-shutdown gate;
* recovery checkpoints;
* policy inspection;
* post-update reconciliation;
* and final Windows signature verification.

---

## 12.6 Decision

Selected.

---

# 13. Option B — App Installer `OnLaunch`

## 13.1 Description

Configure Windows deployment service to check the `.appinstaller` source whenever Opure launches, subject to an interval.

---

## 13.2 Advantages

* native;
* little application code;
* update association managed by Windows;
* prompt support;
* and package-differential support.

---

## 13.3 Disadvantages

* network check occurs before or outside Opure Trust Centre;
* developer cannot use Opure Network Gateway policy;
* launch experience is controlled by Windows;
* prompted non-blocking updates can later apply silently;
* blocking mode removes normal launch choice;
* and enterprise or Windows settings can alter behaviour.

---

## 13.4 Decision

Not enabled by Opure's initial `.appinstaller`.

An enterprise may configure it externally and Opure must display the effective policy.

---

# 14. Option C — Automatic Background Task

## 14.1 Description

Configure App Installer to check every eight hours independently of application use.

---

## 14.2 Advantages

* timely updates;
* no application launch required;
* native Windows deployment;
* and no custom scheduler.

---

## 14.3 Disadvantages

* hidden background network activity;
* no Opure prompt during the check;
* no Trust Centre mediation;
* no project-aware quiescence;
* update can arrive while the developer is not using Opure;
* and conflicts with Local by Design and Human in Control.

---

## 14.4 Decision

Rejected as an Opure default.

---

# 15. Option D — In-Process PackageManager Updater

## 15.1 Description

Request or apply the App Installer package through Windows PackageManager APIs from Opure.

---

## 15.2 Advantages

* integrated progress;
* better application-controlled sequence;
* SmartScreen-capable request API;
* and native package deployment.

---

## 15.3 Disadvantages

* requires restricted `packageManagement` capability;
* increases manifest authority;
* updater code becomes more privileged;
* Store certification and security review burden;
* package replacement is invoked by the application itself;
* and current App Installer APIs have documented quirks.

---

## 15.4 Decision

Deferred.

It may be reconsidered only if the external App Installer hand-off creates unacceptable usability or reliability problems and the restricted capability is justified.

---

# 16. Option E — Third-Party Updater Framework

## 16.1 Description

Use a framework such as Velopack or another installer/updater system.

---

## 16.2 Advantages

* integrated download;
* progress;
* restart;
* delta update;
* rollback support;
* and cross-platform potential.

---

## 16.3 Disadvantages

* duplicates or replaces MSIX lifecycle;
* adds a trusted privileged dependency;
* introduces feed and metadata security;
* application-file replacement logic;
* update framework key management;
* migration between package formats;
* and premature cross-platform abstraction.

---

## 16.4 Decision

Rejected for the initial Windows product.

---

# 17. Option F — Microsoft Store Only

## 17.1 Description

Use Store-managed update exclusively.

---

## 17.2 Advantages

* Windows-native;
* trusted distribution;
* Store signing;
* automatic update infrastructure;
* and no direct update host.

---

## 17.3 Disadvantages

* Store dependency;
* reduced private-preview flexibility;
* Store policy controls update behaviour;
* enterprise Store restrictions;
* and direct technical-preview users remain unsupported.

---

## 17.4 Decision

A Store installation will defer to Store updating, but Store is not the only distribution or update path.

---

# 18. Option G — Manual Browser Download Only

## 18.1 Description

Show a release page and require the developer to download and install manually.

---

## 18.2 Advantages

* simplest;
* no update check;
* no updater code;
* full user control;
* and no metadata service.

---

## 18.3 Disadvantages

* poor discovery;
* easy to miss security updates;
* no channel-specific status;
* no migration preparation;
* and weak recovery integration.

---

## 18.4 Decision

Retained as a fallback, not the only update experience.

---

# 19. Option H — WinGet

## 19.1 Description

Publish Opure to Windows Package Manager and direct users to `winget upgrade`.

---

## 19.2 Advantages

* familiar technical-user workflow;
* package repository;
* command-line automation;
* enterprise use;
* and no custom updater.

---

## 19.3 Disadvantages

* separate manifest publication and review;
* no in-product migration coordination;
* no guaranteed channel behaviour;
* no direct Trust Centre control;
* no immediate release availability;
* and package-manager policy is outside Opure.

---

## 19.4 Decision

May be added as an optional distribution channel later.

It is not the primary in-product update path.

---

# 20. Option I — Custom Elevated Updater

Rejected because it would create a privileged long-lived component that downloads and replaces executable code.

The per-user MSIX package does not require it.

---

# 21. Option J — TUF from the First Release

## 21.1 Advantages

A conformant TUF implementation can provide:

* root-key rotation;
* threshold signatures;
* rollback resistance;
* freeze detection;
* mix-and-match protection;
* delegated targets;
* and mirror resilience.

---

## 21.2 Disadvantages

* no selected maintained conformant .NET client;
* substantial repository and key-management operations;
* conformance testing;
* metadata expiry operations;
* offline-root ceremonies;
* and complexity disproportionate to one signed Windows channel.

---

## 21.3 Decision

Deferred.

TUF principles inform threat analysis, but Opure will not claim TUF compliance without a conformant implementation and conformance evidence.

---

# 22. Comparison Matrix

Scores:

* **1** — poor
* **2** — weak
* **3** — acceptable
* **4** — strong
* **5** — excellent

| Criterion               | Weight | App-Controlled + App Installer | App Installer OnLaunch | Background App Installer | PackageManager | Third Party | Store Only | Browser Only |   WinGet | Custom Service | TUF First |
| ----------------------- | -----: | -----------------------------: | ---------------------: | -----------------------: | -------------: | ----------: | ---------: | -----------: | -------: | -------------: | --------: |
| Human control           |      5 |                              5 |                      3 |                        1 |              4 |           3 |          2 |            5 |        4 |              2 |         5 |
| Trust Centre visibility |      5 |                              5 |                      1 |                        1 |              5 |           4 |          1 |            3 |        1 |              5 |         5 |
| No privilege expansion  |      5 |                              5 |                      5 |                        5 |              2 |           3 |          5 |            5 |        5 |              1 |         4 |
| MSIX compatibility      |      5 |                              5 |                      5 |                        5 |              5 |           2 |          5 |            5 |        4 |              3 |         5 |
| Safe migration          |      5 |                              5 |                      3 |                        2 |              5 |           4 |          3 |            2 |        2 |              5 |         5 |
| Small-team fit          |      5 |                              5 |                      5 |                        5 |              3 |           3 |          4 |            5 |        4 |              1 |         1 |
| Direct distribution     |      5 |                              5 |                      5 |                        5 |              5 |           5 |          1 |            5 |        3 |              5 |         5 |
| Offline product use     |      5 |                              5 |                      5 |                        5 |              5 |           5 |          5 |            5 |        5 |              5 |         5 |
| Rollback resistance     |      4 |                              4 |                      4 |                        4 |              4 |           4 |          5 |            3 |        4 |              4 |         5 |
| Freeze detection        |      3 |                              2 |                      2 |                        3 |              2 |           3 |          4 |            1 |        2 |              3 |         5 |
| User experience         |      4 |                              4 |                      4 |                        5 |              5 |           5 |          5 |            2 |        3 |              5 |         4 |
| Provider portability    |      3 |                              5 |                      4 |                        4 |              4 |           4 |          1 |            5 |        2 |              4 |         5 |
| Security surface        |      5 |                              5 |                      5 |                        5 |              3 |           3 |          5 |            5 |        4 |              1 |         3 |
| **Indicative result**   |        |                   **Selected** |           Not selected |                 Rejected |       Deferred |    Rejected | Additional |     Fallback | Optional |       Rejected |  Deferred |

---

# 23. Decision

Opure will provisionally adopt:

> **Application-controlled update discovery through the Network Gateway, with explicit developer consent and Windows App Installer hand-off for signed MSIX deployment, while keeping all Windows automatic update settings disabled in the initial App Installer files.**

This decision is:

* [ ] Permanent until superseded
* [x] Provisional pending update and recovery evidence
* [ ] Automatic by default
* [ ] Store-only
* [ ] Cross-platform updater policy

---

# 24. Update Policy Modes

Opure supports the following update-check policies.

## 24.1 Manual Only

Default.

Behaviour:

* no automatic update request;
* no startup request;
* no scheduled request;
* no Windows App Installer automatic check configured by Opure;
* and no update traffic until the developer selects **Check for Updates**.

---

## 24.2 Check on Launch

Optional.

Behaviour:

* explicitly enabled by the developer;
* one check after the Desktop and Runtime are ready;
* no check more frequently than the configured minimum interval;
* default minimum interval of 24 hours;
* no blocking of application launch;
* no silent installation;
* and no check when network policy, offline mode or enterprise policy prohibits it.

The first enablement screen must state:

* update origin;
* data sent;
* check frequency;
* that no project content or identifier is sent;
* that an available update is not installed automatically;
* and how to disable the preference.

---

## 24.3 Enterprise Managed

An enterprise policy may:

* disable checks;
* require a managed update source;
* require a minimum supported version;
* use Store or management tooling;
* or configure Windows App Installer policy.

Opure must show the effective policy and its source.

The product must not pretend a managed policy is a user preference.

---

## 24.4 Store Managed

When installed through the Microsoft Store:

* the Store owns package updating;
* Opure's direct App Installer hand-off is disabled;
* the UI may show Store-managed status;
* and a manual action may open the Store product page when approved.

---

## 24.5 Diagnostic Build

An unpackaged diagnostic ZIP:

* performs no production update check by default;
* cannot replace a packaged Stable or Preview installation;
* reports that update management is unavailable;
* and directs the user to the official release page.

---

## 24.6 Development Package

`Opure.Dev` uses only development update sources.

It cannot contact Stable or Preview production update endpoints without an explicit test configuration.

---

# 25. Policy Precedence

Effective update policy is resolved in this order:

1. operating-system or enterprise management policy;
2. Opure administrator policy, when introduced;
3. package-distribution mode;
4. channel policy;
5. developer preference;
6. default.

Higher-precedence policy cannot be silently overridden.

---

## 25.1 Effective Policy Display

The Updates settings view should show:

* effective mode;
* configured preference;
* policy source;
* update channel;
* update origin;
* last attempted check;
* last successful check;
* last result;
* and next eligible check time.

---

## 25.2 Policy Change Receipt

Changing update policy creates a local Trust Centre record.

---

# 26. Update Architecture

The update architecture contains:

* **Update Coordinator**
* **Update Source Registry**
* **Update Discovery Client**
* **App Installer Metadata Parser**
* **Release Verification Service**
* **Update Policy Service**
* **Update Readiness Service**
* **Update Handoff Service**
* **Post-Update Reconciliation Service**
* **Migration and Recovery Services**
* **Desktop Update Projection**
* **Trust Centre**
* **Network Gateway**

Conceptual flow:

```text
Desktop command
    ↓
Update Coordinator
    ↓
Update Policy Service
    ↓
Network Gateway
    ↓
Approved channel .appinstaller source
    ↓
Strict metadata parsing
    ↓
Identity, channel and version validation
    ↓
Release information projection
    ↓
Developer decision
    ↓
Readiness and recovery checkpoint
    ↓
Windows App Installer hand-off
    ↓
Package update
    ↓
Next-launch reconciliation and migrations
```

---

# 27. Service Ownership

## 27.1 Update Coordinator

Owns:

* update-check orchestration;
* state machine;
* cancellation;
* policy evaluation;
* version comparison;
* result persistence;
* readiness request;
* hand-off preparation;
* and post-update reconciliation.

It does not:

* replace package files;
* validate source repository content;
* sign releases;
* modify schemas directly;
* or own Windows package deployment.

---

## 27.2 Network Gateway

Owns:

* DNS and HTTPS request;
* proxy policy;
* certificate validation;
* redirect policy;
* request limits;
* response limits;
* timeout;
* cancellation;
* and destination logging.

The Update Coordinator must not create a raw independent HTTP client that bypasses it.

---

## 27.3 Update Source Registry

Owns approved sources by:

* package family;
* channel;
* architecture;
* distribution mode;
* and policy source.

---

## 27.4 Release Verification Service

Owns verification of:

* expected package name;
* expected publisher;
* expected package family;
* channel;
* product SemVer;
* MSIX deployment version;
* architecture;
* release state;
* SHA-256 where available;
* and ADR-0014 evidence references.

It does not replace Windows signature verification.

---

## 27.5 Update Readiness Service

Queries:

* active builds;
* active tests;
* active patch transactions;
* active model inference;
* active plugin calls;
* active MCP calls;
* pending approvals;
* uncheckpointed workflows;
* open database migrations;
* and restart blockers.

---

## 27.6 Handoff Service

Owns:

* bounded local `.appinstaller` staging;
* final source and hash checks;
* shell activation;
* hand-off receipt;
* and controlled shutdown request.

It does not request the restricted package-management capability initially.

---

## 27.7 Post-Update Reconciliation

Owns:

* previous and current package identity;
* previous and current product version;
* migration plan;
* profile compatibility;
* backup state;
* interrupted operation recovery;
* and Trust Centre completion record.

---

# 28. Authority Boundaries

The authorities are:

| Decision                                   | Authority                                   |
| ------------------------------------------ | ------------------------------------------- |
| Whether network check is allowed           | Update Policy Service and Network Gateway   |
| Which source may be contacted              | Update Source Registry                      |
| Whether metadata appears valid             | Update Coordinator and Verification Service |
| Whether user wants to apply                | Developer through Desktop                   |
| Whether workflows are safe to stop         | Readiness and Process Supervisors           |
| Whether package is trusted and installable | Windows App Installer and Windows trust     |
| Whether data migration is safe             | Service-owned migration planners            |
| Whether startup completed                  | Runtime readiness and Recovery services     |
| Whether release is supported               | Release policy metadata and product policy  |

No single UI view becomes authoritative.

---

# 29. Update Source Registry

A source record should contain:

```text
source_id
distribution_mode
channel
package_name
publisher
package_family
architecture
appinstaller_uri
release_notes_base_uri
allowed_hosts
redirect_hosts
minimum_tls_policy
enabled
managed_by
```

No credentials.

---

## 29.1 Stable Source

Stable source accepts stable product SemVer only.

---

## 29.2 Preview Source

Preview source accepts:

* preview;
* beta;
* and RC

according to the Preview package family's monotonic MSIX mapping.

---

## 29.3 Development Source

Development source accepts test packages only.

---

## 29.4 No Arbitrary URL

The ordinary UI does not allow an arbitrary update URL.

A custom enterprise source requires an administrative policy and explicit trust configuration.

---

# 30. Update Origin

The final public origin is not selected by this ADR.

Requirements include:

* HTTPS;
* stable host name;
* no authentication for public metadata;
* immutable versioned package URLs;
* bounded redirects;
* correct content type;
* high availability;
* and release-process integration.

---

## 30.1 GitHub Releases

GitHub Releases may host immutable versioned package assets.

A separate update-origin endpoint may host the channel `.appinstaller` pointer.

---

## 30.2 Mutable Channel Pointer

A channel pointer is inherently mutable.

It is discovery metadata, not final release proof.

The final package must still pass Windows signature and identity verification.

---

## 30.3 Origin Compromise Limitation

A compromised origin might:

* withhold updates;
* replay an older pointer;
* redirect to an unavailable package;
* or display misleading advisory text.

It should not be able to install a package signed by an unrelated publisher because Windows verifies package signature and family.

This limitation must remain documented.

---

# 31. App Installer File Policy

Initial `.appinstaller` files must omit:

```xml
<UpdateSettings>
```

entirely.

They must not include:

* `OnLaunch`;
* `AutomaticBackgroundTask`;
* `ShowPrompt`;
* `UpdateBlocksActivation`;
* or `ForceUpdateFromAnyVersion`.

---

## 31.1 Why Omit

Omission means Opure does not configure Windows deployment service to check or apply updates independently.

Opure checks only through its own policy and Network Gateway.

---

## 31.2 Effective External Policy

Windows Settings, PowerShell or enterprise CSP may still configure an associated application's update behaviour.

Opure should inspect `AppInstallerInfo` and display:

* OnLaunch;
* AutomaticBackgroundTask;
* ShowPrompt;
* UpdateBlocksActivation;
* ForceUpdateFromAnyVersion;
* policy source;
* last checked;
* and pause state

where APIs permit.

---

## 31.3 Unexpected Automatic Policy

If Opure detects automatic or downgrade policy it did not configure:

* show a Trust Centre notice;
* identify the policy source;
* do not claim that Opure controls it;
* and provide appropriate Windows settings guidance.

Do not attempt to override enterprise policy silently.

---

# 32. App Installer Association

The package should be installed initially through the approved `.appinstaller` file so Windows records the association.

---

## 32.1 Direct MSIX Installation

A package installed directly from `.msix` may lack App Installer association.

In that case:

* manual discovery may still identify a release;
* update UI states that association is absent;
* hand-off opens the approved `.appinstaller`;
* Windows may establish the association during the update;
* and package identity remains the final authority.

---

## 32.2 Source Discovery

The application may use:

```text
Package.GetAppInstallerInfo()
```

to inspect the associated source.

If no association exists, use the approved source registry.

---

## 32.3 API Compatibility

The current package API surface must be wrapped behind a Windows platform adapter and guarded by runtime API availability checks.

---

# 33. Check Availability API

Opure does not rely solely on `Package.CheckUpdateAvailabilityAsync` initially because:

* it works only with App Installer association;
* current Microsoft documentation records a known direct-current-package access issue;
* and it evaluates App Installer policy rather than Opure's full release and migration policy.

A prototype may use it as an additional signal through the documented workaround.

---

# 34. Discovery Request

The discovery request should fetch only the bounded `.appinstaller` document and optional bounded release summary.

The request should not include:

* project names;
* project paths;
* Git remotes;
* user account;
* email;
* machine identifier;
* Windows user name;
* hardware inventory;
* installed plugins;
* models;
* provider settings;
* Vault metadata;
* or persistent tracking identifier.

---

## 34.1 User Agent

Use a generic safe user agent containing at most:

* Opure product;
* major/minor version;
* operating-system family;
* architecture;
* and update channel.

The user agent must not include:

* full commit;
* device ID;
* project ID;
* or Windows account.

A more private constant user agent is acceptable if the host does not need version routing.

---

## 34.2 Query String

Do not place installed version or identifiers in a query string unless a future source requires it and privacy review approves it.

A static channel URI is preferred.

---

## 34.3 HTTP Headers

Do not add unique correlation identifiers that persist across update checks.

Local correlation IDs remain local.

---

# 35. Network Policy

Update checks are classified as:

```text
Opure Product Infrastructure
```

rather than project provider traffic.

They still require product-level consent.

---

## 35.1 Default

Manual Only permits a request only after the user selects **Check for Updates**.

---

## 35.2 Check on Launch

The stored opt-in authorises a bounded request according to the displayed frequency and source.

---

## 35.3 Offline Mode

Offline Mode disables update checks.

The UI may show the last cached status.

---

## 35.4 Metered Network

A future metered-network policy may suppress automatic launch checks.

Manual checks remain available with warning.

---

## 35.5 Proxy

Use the approved system or enterprise proxy policy.

Do not implement a hidden proxy bypass.

---

# 36. HTTPS Requirements

The initial source requires HTTPS.

Reject:

* HTTP;
* FTP;
* file URI from remote policy;
* SMB public source;
* and unsupported schemes.

Enterprise local sources may use an explicitly managed file or SMB source after separate policy.

---

## 36.1 Certificate Validation

Use normal Windows and .NET certificate validation through the Network Gateway.

Do not disable TLS validation.

---

## 36.2 Certificate Pinning

Do not pin a leaf TLS certificate.

The release payload is protected by Windows code signing.

A future origin-public-key pin requires an operational rotation plan.

---

# 37. Redirect Policy

Default:

* no cross-host redirects;
* maximum three redirects;
* HTTPS only;
* no downgrade;
* no credentials forwarded;
* and each destination must be allowlisted.

A versioned GitHub release-asset flow may require known GitHub asset hosts.

Those hosts must be explicit.

---

# 38. Request Limits

Suggested initial limits:

* metadata response: 256 KiB;
* release summary: 512 KiB;
* connect timeout: 10 seconds;
* total metadata timeout: 30 seconds;
* maximum redirects: 3;
* decompressed-size enforcement;
* and one active check per channel.

Exact values require testing.

---

# 39. XML Security

The `.appinstaller` parser must:

* disable DTDs;
* disable external entities;
* prohibit external schema retrieval;
* limit depth;
* limit attributes;
* limit text length;
* reject duplicate critical elements;
* reject unknown identity-changing elements;
* and use the supported schema version.

---

## 39.1 No Trust in XML Display Text

Publisher and package identity are validated against local policy, not merely displayed from the downloaded XML.

---

# 40. Metadata Validation

An update candidate must match:

* expected channel;
* expected package name;
* expected publisher;
* expected package family derivation;
* expected architecture or approved bundle;
* allowed minimum Windows version;
* supported product SemVer format;
* valid MSIX numeric mapping;
* package URI scheme;
* package URI host;
* release asset naming;
* and higher installed package version.

---

## 40.1 SemVer and MSIX Agreement

Recompute MSIX version from product SemVer using ADR-0013 where the product version is available.

A disagreement fails discovery.

---

## 40.2 Missing Product Version

If the `.appinstaller` file provides only numeric package version, the UI may show limited information until immutable release metadata is retrieved.

Do not guess a product SemVer.

---

# 41. Release Summary

A release summary may be fetched from an immutable release asset or API.

It should contain:

* product version;
* release channel;
* release date;
* release notes URI;
* package SHA-256;
* package size;
* minimum supported source version;
* migration classification;
* restart classification;
* security classification;
* known issues;
* and withdrawal state.

---

## 41.1 Advisory Trust

Until a cryptographically verified metadata format is selected, the summary is advisory discovery content.

The package's signed identity and immutable release evidence remain final trust controls.

---

## 41.2 UI Language

The UI should distinguish:

* **Verified package information**
* **Release publisher information**
* **Remote release notes**

where their trust differs.

---

# 42. Version Comparison

Compare:

* installed product SemVer;
* installed MSIX version;
* candidate product SemVer;
* and candidate MSIX version.

The package version must be strictly greater.

---

## 42.1 Higher Product but Lower Package

Reject.

---

## 42.2 Higher Package but Invalid Product Transition

Reject or mark unsupported.

---

## 42.3 Same Package Version

No update.

---

## 42.4 Lower Package Version

Reject as rollback.

---

# 43. Highest-Observed Version

Persist per channel:

* highest valid MSIX version observed;
* associated product version;
* source;
* observation time;
* release hash where available;
* and trust level.

A later discovery result below this value is treated as:

* stale;
* replayed;
* rolled back;
* or channel misconfiguration.

---

## 43.1 Local State Loss

Deleting the local updater cache removes this defence.

Current installed package version remains the minimum boundary.

---

## 43.2 Legitimate Withdrawal

Withdrawal does not cause automatic downgrade.

A replacement must have a higher package version.

---

# 44. Expiry and Freeze Detection

The initial App Installer discovery model does not provide a strong independently signed expiry mechanism.

Therefore:

* Opure may show how long since the last successful check;
* security policy may warn when a supported release-status check is stale;
* but the product must not claim cryptographic freeze-attack detection.

A TUF-conformant future design is required for a stronger claim.

---

# 45. Cached Discovery

Cache only:

* validated metadata;
* release summary;
* timestamps;
* and status.

Do not cache:

* credentials;
* unbounded HTML;
* arbitrary scripts;
* or executable packages

inside the metadata cache.

---

## 45.1 Cache Display

Cached status is labelled with:

* fetched time;
* source;
* and offline or stale state.

---

# 46. Update Availability States

Stable states include:

* Not Checked;
* Checking;
* Current;
* Update Available;
* Update Recommended;
* Security Update Available;
* Update Required by Enterprise Policy;
* Source Unavailable;
* Offline;
* Metadata Invalid;
* Identity Mismatch;
* Publisher Mismatch;
* Channel Mismatch;
* Rollback Rejected;
* Release Withdrawn;
* Unsupported Upgrade Path;
* Readiness Blocked;
* Awaiting User;
* Handed to Windows;
* Installation Cancelled or Unknown;
* Updated Pending Migration;
* Updated;
* Recovery Required;
* and Store Managed.

---

# 47. Criticality

Release metadata may classify an update as:

* Optional;
* Recommended;
* Security;
* Critical Security;
* Compatibility Required;
* or Enterprise Required.

This classification affects messaging, not hidden installation authority.

---

## 47.1 No Product Forced Update

Opure does not force a package update before local launch initially.

---

## 47.2 Feature Restriction

A remotely exploitable security issue may justify disabling one affected external capability through a separately reviewed signed security policy or local release rule.

That is not approved by this ADR.

---

## 47.3 Enterprise Required

Enterprise policy may require a minimum version.

Opure must show the policy and remediation.

---

# 48. Update User Experience

The Updates view should show:

* current version;
* current channel;
* current package family;
* update policy;
* source;
* last check;
* candidate version;
* package size;
* publisher;
* signature expectation;
* release date;
* highlights;
* security classification;
* migration classification;
* restart requirement;
* known issues;
* and links to full release notes and verification.

---

## 48.1 Actions

Possible actions:

* Check for Updates;
* Review Release;
* Install Update;
* Remind Me Later;
* Skip This Optional Version;
* Open Release Page;
* Copy Verification Details;
* View Trust Centre;
* and Cancel Check.

---

## 48.2 Skip Version

Skipping is allowed only for optional updates.

Persist:

* exact product version;
* exact package version;
* channel;
* and user decision.

A security or required update may continue to warn but cannot install silently.

---

## 48.3 Remind Later

A reminder is a UI preference.

It does not create a background scheduled task.

The next reminder is evaluated when Opure is next running.

---

# 49. Release Notes Safety

Render release notes as safe Markdown or plain text.

Do not execute:

* HTML script;
* embedded active content;
* custom URI without validation;
* or remote images by default.

External links open only after user action and destination display.

---

# 50. Download Behaviour

The initial updater does not download the MSIX payload itself before user confirmation.

It fetches metadata only.

Windows App Installer retrieves and installs the package after hand-off.

This:

* avoids storing an executable payload in Opure cache;
* avoids duplicating Windows deployment;
* keeps publisher UI visible;
* and reduces updater code.

---

## 50.1 Future Pre-Download

A future pre-download feature requires:

* explicit consent;
* storage limits;
* metered-network policy;
* package hash verification;
* signature verification;
* secure staging;
* and cleanup.

---

# 51. Handoff Mechanism

When the developer selects **Install Update**, Opure should:

1. revalidate candidate and source;
2. compute update readiness;
3. explain blockers;
4. create required backups and checkpoints;
5. persist resumable UI state;
6. stage the bounded `.appinstaller` document in an Opure-owned temporary directory;
7. verify it again;
8. invoke normal Windows shell activation for that local file;
9. confirm hand-off was accepted by the shell;
10. request graceful Opure shutdown;
11. and leave package installation to Windows App Installer.

---

## 51.1 No `ms-appinstaller:` Protocol

Do not depend on the disabled browser protocol.

Open the downloaded `.appinstaller` file directly.

---

## 51.2 No Restricted Capability

Do not request `packageManagement` solely to create an embedded updater experience.

---

## 51.3 Handoff Receipt

Record:

* candidate version;
* package version;
* source;
* local metadata hash;
* time;
* user approval;
* readiness result;
* and shell hand-off result.

Do not record a successful package installation until next-launch reconciliation proves it.

---

# 52. Update Readiness

Before hand-off, classify active work.

## 52.1 Blocking

Examples:

* patch transaction applying;
* database migration;
* Vault rotation;
* repository mutation;
* package installation;
* critical file write;
* unresolved commit operation;
* or non-checkpointable workflow step.

---

## 52.2 Deferrable

Examples:

* long build;
* test run;
* model generation;
* log stream;
* or plugin task with cancellation.

The user may choose to cancel or wait.

---

## 52.3 Safe

Examples:

* idle UI;
* completed workflow;
* read-only inspection;
* or checkpointed operation.

---

# 53. Safe Shutdown Plan

The plan should list:

* processes to stop;
* tasks to cancel;
* tasks to checkpoint;
* unsaved UI state;
* database checkpoints;
* pending Trust Centre records;
* and expected restart behaviour.

The developer must see material interruption.

---

## 53.1 No Kill-First Update

Do not terminate processes before attempting bounded graceful shutdown.

---

## 53.2 Timeout

After a bounded timeout, offer:

* cancel update;
* continue waiting;
* or force stop with explicit recovery warning.

Force stop is not the default.

---

# 54. Process Shutdown Order

Conceptual order:

1. stop accepting new high-risk commands;
2. finish or reject new patch operations;
3. cancel external provider calls;
4. cancel MCP calls;
5. stop plugin work;
6. checkpoint workflows;
7. stop workers;
8. flush outboxes and logs;
9. close service databases;
10. stop Runtime;
11. close Desktop;
12. allow Windows App Installer to continue.

---

# 55. Backup and Recovery Checkpoint

Before a release with a migration classification above `None`, create:

* service-owned database backup;
* Vault backup or keyring recovery checkpoint according to ADR-0007;
* profile manifest snapshot;
* workflow checkpoint;
* and update receipt.

---

## 55.1 Backup Scope

The update UI shows:

* what is backed up;
* where;
* retention;
* and limitations.

---

## 55.2 Source Repository

Package update does not back up source repositories automatically because it does not mutate them as part of package installation.

A migration that will mutate project-owned Opure files requires a separate project plan.

---

# 56. Migration Classification

Each release declares:

* None;
* Compatible;
* Forward Only;
* Backup Required;
* Manual Action Required;
* or Unsupported Direct Upgrade.

---

## 56.1 None

No persistent schema change.

---

## 56.2 Compatible

Migration occurs automatically and older binary compatibility is retained where documented.

---

## 56.3 Forward Only

Migration is automatic but downgrade is unsupported.

---

## 56.4 Backup Required

Update cannot proceed until required backups succeed.

---

## 56.5 Manual Action Required

The user must complete a specific step.

---

## 56.6 Unsupported Direct Upgrade

The installed version cannot upgrade directly.

The UI provides the supported intermediate path.

---

# 57. Post-Update Reconciliation

On every startup, compare:

* current package family;
* current MSIX version;
* current product version;
* previous profile-recorded version;
* schema versions;
* Vault format;
* plugin state;
* and interrupted update receipt.

---

## 57.1 No Update Receipt

A package may have been updated outside Opure.

Reconciliation must still detect it.

---

## 57.2 Success

An update is considered complete only when:

* package identity is correct;
* Runtime starts;
* migrations complete;
* critical services are healthy;
* Safe Mode remains available;
* and profile manifest is committed.

---

## 57.3 Failure

Enter Recovery Mode.

Do not repeatedly rerun a failed destructive migration without a recorded recovery plan.

---

# 58. Update Recovery

Recovery may offer:

* retry idempotent migration;
* restore backup;
* open Safe Mode;
* export diagnostics;
* open release notes;
* reinstall current package;
* or contact support.

---

## 58.1 Binary Downgrade Warning

Restoring data does not install old binaries.

Installing old binaries does not restore data.

The UI must explain both.

---

# 59. Restart Behaviour

The initial App Installer hand-off may not guarantee automatic restart.

Therefore:

* persist UI and workflow state;
* tell the developer that Opure will close;
* show how to relaunch;
* and reconcile on next launch.

A future restart hand-off may use supported activation facilities after testing.

---

# 60. User Cancellation

The developer may cancel:

* metadata check;
* update review;
* readiness preparation before irreversible backup finalisation;
* or Windows App Installer.

Cancellation before package installation leaves current binaries unchanged.

---

## 60.1 Unknown External Result

Opure may not know whether the user completed the Windows dialog after Opure exited.

Next launch determines the actual result.

---

# 61. Update Failure Categories

Stable categories include:

* Policy Denied;
* Offline;
* Source Unavailable;
* TLS Failure;
* Redirect Rejected;
* Metadata Too Large;
* Metadata Invalid;
* Unsupported Schema;
* Identity Mismatch;
* Publisher Mismatch;
* Architecture Mismatch;
* Channel Mismatch;
* Version Mapping Invalid;
* Rollback Detected;
* Release Withdrawn;
* Unsupported Upgrade;
* Backup Failed;
* Readiness Blocked;
* Handoff Failed;
* Installation Cancelled or Unknown;
* Package Unchanged;
* Package Updated;
* Migration Failed;
* Recovery Required;
* and Enterprise Managed.

---

# 62. Retry Policy

Metadata network failure may be retried:

* manually;
* or once with bounded backoff during an explicit launch check.

Do not retry indefinitely.

No background retry loop.

---

# 63. Cancellation

Network, parsing and readiness operations accept cancellation.

After Windows hand-off, Opure cannot treat its own cancellation token as authority over the external installer.

---

# 64. Stable Channel Policy

Stable channel offers only stable SemVer.

It never offers:

* preview;
* beta;
* RC;
* Development;
* or another package family.

---

## 64.1 Stable Skipping

Optional Stable releases may be skipped.

The next supported release must still validate direct upgrade compatibility.

---

# 65. Preview Channel Policy

Preview may offer:

```text
preview → later preview → beta → RC
```

within one intended release line.

After stable publication, Preview does not automatically switch to the Stable package family.

---

## 65.1 Next Preview Line

Preview may continue to the next planned development version after stable release.

---

## 65.2 Data Warning

Preview update UI emphasises:

* compatibility risk;
* backup;
* profile isolation;
* and no automatic Stable migration.

---

# 66. Channel Switching

Switching channel is not an update.

It is a separate installation and data migration operation.

---

## 66.1 Stable to Preview

May:

* install Preview side by side;
* optionally import selected stable configuration;
* and leave Stable intact.

---

## 66.2 Preview to Stable

Requires explicit Stable installation and supported export/import.

Do not replace Preview with Stable by package downgrade or identity substitution.

---

# 67. Store Policy

For Store installation:

* package-family and distribution-mode detection identifies Store management;
* direct `.appinstaller` source is not used;
* no duplicate update prompt is shown;
* and Store availability is displayed as external policy.

A future Store API integration requires a distribution ADR amendment.

---

# 68. Enterprise Policy

Enterprise policy may:

* disable product checks;
* point to a managed source;
* mandate Store or device-management updates;
* enforce minimum version;
* pause updates;
* or configure App Installer auto-update.

Opure should expose the effective policy and not fight it.

---

## 68.1 Managed Source

A managed source requires:

* explicit package identity;
* publisher trust;
* HTTPS or approved enterprise transport;
* architecture;
* and administrator signature or policy provenance.

---

# 69. Security Update Policy

A security release should state:

* affected versions;
* severity;
* exploit conditions;
* fixed version;
* package hash;
* migration impact;
* and support state.

---

## 69.1 Notification

Manual Only users learn of the update only when they check or through external advisory channels.

This is an explicit trade-off.

---

## 69.2 Opt-In Launch Check

Users who opt into launch checks receive faster notices without background service.

---

## 69.3 Critical Advisory

Opure may display a cached or newly fetched critical advisory.

It does not silently install the fix.

---

# 70. Withdrawn Release

If a release is withdrawn:

* the channel source moves to a higher replacement version;
* the withdrawn version is never reused;
* existing installed users see a warning when update status is checked;
* and the release record remains immutable.

---

## 70.1 Installed Withdrawn Version

Offer:

* replacement update;
* known-risk summary;
* backup guidance;
* and support link.

---

## 70.2 No Automatic Downgrade

A withdrawn higher version is not automatically downgraded to an older safe version.

Publish a newer corrective package.

---

# 71. Package Signature and Publisher Verification

Before offering hand-off, Opure validates expected publisher metadata.

Windows App Installer performs final package signature verification.

After update, Opure checks that the installed package:

* has the expected family;
* has a higher expected version;
* maps to the expected product release;
* and is not Development identity.

---

## 71.1 No Application Bypass

The application cannot override Windows signature failure.

---

# 72. Release Evidence

The update UI should expose a verification summary linking:

* release tag;
* package SHA-256;
* publisher;
* package family;
* build attestation;
* SBOM;
* and immutable release.

It may not require the user to verify every item before ordinary installation, but the evidence must remain available.

---

# 73. Trust Centre Events

Record:

* update policy changed;
* update check requested;
* network destination;
* check completed;
* candidate discovered;
* validation rejected;
* update reviewed;
* update skipped;
* readiness blocked;
* backup created;
* hand-off approved;
* App Installer launched;
* shutdown initiated;
* package change detected;
* migration started;
* migration completed;
* migration failed;
* recovery entered;
* and update completed.

---

## 73.1 Sensitive Data

Events exclude:

* project contents;
* source paths where unnecessary;
* secrets;
* full HTTP headers;
* and unique external tracking data.

---

# 74. Update Logs

Structured update logs should include:

* operation ID;
* channel;
* source ID;
* package family;
* current and candidate versions;
* policy result;
* validation result;
* duration;
* and outcome.

Do not log arbitrary remote XML or release-note bodies at normal levels.

---

# 75. Privacy Statement

The update setting should state:

> When Opure checks for updates, it requests a small channel metadata file from the displayed update source. The request does not include project content, project names, source paths, prompts, secrets, account identifiers or a persistent device identifier. The server will still receive ordinary network information such as the source IP address and standard connection metadata.

---

# 76. No Telemetry Coupling

An update check is not permission for product analytics.

Update infrastructure must not add:

* tracking pixel;
* third-party analytics;
* advertising identifier;
* or cross-request device token.

---

# 77. Bandwidth

Manual metadata checks are small.

Package transfer is performed by Windows only after user approval.

MSIX block-map update machinery may reduce changed package transfer when the hosting path supports it.

Opure must not promise a specific delta size without measurement.

---

# 78. Metered and Battery Policy

Initial policy:

* no background checks;
* manual check allowed;
* opt-in launch check may be suppressed when Windows reports metered connection or battery saver;
* and the UI explains suppression.

Exact Windows APIs require prototype validation.

---

# 79. Proxy Authentication

If an enterprise proxy requires authentication:

* use approved system mechanisms;
* do not capture proxy credentials in Opure normal storage;
* and show a safe source-unavailable result.

---

# 80. Offline Behaviour

No update source is required for:

* launch;
* project access;
* build;
* test;
* local AI;
* plugin use;
* recovery;
* or uninstall.

An unavailable update service cannot degrade ordinary local functionality.

---

# 81. Update Source Outage

On outage:

* preserve current version;
* show last successful status;
* permit local work;
* permit manual retry;
* and never switch to an unapproved mirror automatically.

---

# 82. Mirror Policy

No mirror is selected initially.

A future mirror requires:

* package hash;
* publisher verification;
* origin policy;
* and preferably TUF-style signed metadata.

---

# 83. TUF Adoption Gate

Consider TUF when at least one is true:

* two or more public mirrors;
* update repository operated separately from release repository;
* delegated enterprise channels;
* independent metadata signers;
* offline root key requirement;
* high-value targeted attack evidence;
* need for cryptographic freeze detection;
* or cross-platform updater convergence.

---

## 83.1 Conformance Requirement

A TUF implementation requires:

* accepted TUF specification version;
* maintained implementation or isolated helper;
* conformance test suite;
* root key ceremony;
* threshold policy;
* expiration policy;
* key rotation;
* recovery rehearsal;
* and separate ADR.

---

# 84. No Custom Crypto Claim

The initial updater may validate standard:

* TLS;
* SHA-256;
* Authenticode;
* MSIX identity;
* and GitHub attestations through approved tooling.

It must not invent and advertise a custom secure-update signature protocol.

---

# 85. Package Manager Capability

The initial package does not request:

```text
packageManagement
```

for self-update.

---

## 85.1 Reconsideration Gate

Reconsider only when:

* App Installer hand-off is unreliable;
* restart experience is unacceptable;
* restricted-capability approval is feasible;
* security review accepts it;
* and direct PackageManager use materially improves developer control.

---

# 86. App Installer Auto-Repair

Windows may support App Installer auto-repair policy.

Opure does not configure it initially.

Package repair remains a user or enterprise action.

---

# 87. Windows Settings Overrides

Windows Settings can change App Installer automatic update and repair behaviour.

Opure should inspect and report the effective state when possible.

It should not attempt to conceal or bypass the user's Windows setting.

---

# 88. Package-to-Bundle Transition

When ARM64 introduces an MSIX bundle:

* update discovery recognises bundle URI;
* package identity remains;
* transition is tested from x64 MSIX;
* and no later update returns to single MSIX.

---

# 89. Architecture Transition

An update may change installed architecture where Windows supports it.

Opure should not do so without:

* hardware compatibility;
* native dependency validation;
* package test;
* and explicit release note.

---

# 90. Runtime Compatibility

The package contains matching Desktop and Runtime versions.

Post-update mixed-version operation is not expected.

If the update is interrupted and a mismatch appears, protocol negotiation and Recovery Mode prevent unsafe operation.

---

# 91. Plugin Compatibility After Update

After package update:

* scan plugin manifests;
* check SDK range;
* check permissions;
* disable incompatible plugins;
* and show review.

Application package update does not silently update third-party plugins.

---

# 92. Local Model Compatibility

Package update may change model-runtime requirements.

The application checks local model compatibility on launch.

It does not delete or redownload models automatically.

---

# 93. Provider Compatibility

First-party provider adapters update with the package.

Third-party external provider configuration remains.

A changed permission or data-sharing behaviour requires release-note and Trust Centre review.

---

# 94. Project Compatibility

Package update must not automatically rewrite source repositories merely because the application version changed.

A project-format migration is:

* explicit;
* reviewable;
* separately backed up;
* and owned by Project or Workspace services.

---

# 95. Update State Persistence

Persist updater state in an authoritative local service database, including:

* policy;
* source;
* last attempt;
* last success;
* candidate;
* highest observed;
* skip decisions;
* pending hand-off;
* previous package version;
* backup reference;
* and completion result.

No package payload.

---

# 96. Update Operation Identity

Every check and apply preparation receives:

* operation ID;
* correlation ID;
* causation ID;
* policy source;
* and initiating actor.

---

# 97. Idempotency

Repeated metadata checks must be safe.

Post-update reconciliation must be idempotent.

A repeated hand-off should not create duplicate backup or migration state without detection.

---

# 98. Concurrency

Only one update check per channel may run.

Only one update hand-off or reconciliation may exist per profile.

Package update and profile cleanup cannot run together.

---

# 99. Locking

Use service-level leases for:

* readiness;
* profile migration;
* package transition record;
* and recovery.

Do not hold a database transaction while waiting for user input or Windows App Installer.

---

# 100. Timeout

Suggested defaults:

* metadata check: 30 seconds;
* release summary: 30 seconds;
* readiness analysis: 30 seconds;
* graceful shutdown: 60 seconds;
* individual task cancellation: service specific;
* and external install result: determined on next launch rather than an infinite wait.

---

# 101. UI Responsiveness

Update checks run asynchronously.

The Desktop remains usable unless the user enters the final shutdown preparation.

---

# 102. Accessibility

Update UI must support:

* keyboard;
* Narrator;
* high contrast;
* reduced motion;
* focus management;
* and clear progress and error text.

No countdown that forces an update.

---

# 103. Notifications

An available update may appear as:

* unobtrusive in-app badge;
* Updates settings status;
* or Trust Centre notice.

No Windows toast is required initially.

No notification occurs when Manual Only has not checked.

---

# 104. Release Notes Cache

Cache safe text for offline review.

Remote images and tracking content are excluded.

---

# 105. Support Lifecycle

The updater may warn when the installed version is:

* current;
* superseded;
* security affected;
* unsupported;
* or withdrawn.

Support state comes from release policy and should not be inferred solely from version age.

---

# 106. Minimum Supported Version

A release may specify the oldest version it can upgrade directly.

If the installed version is older:

* show supported intermediate release;
* provide manual instructions;
* and do not attempt an untested leap.

---

# 107. Future Mandatory Minimum

A cloud provider integration might require a minimum secure client version.

That provider capability may be disabled with explanation.

The local product still launches unless a separate safety decision says otherwise.

---

# 108. Update Check on Startup Timing

When opt-in is enabled, check only after:

* Desktop shell appears;
* Runtime is healthy;
* project loading critical path completes;
* and startup performance budget permits.

The check must not delay local readiness.

---

# 109. Check Frequency

Default opt-in frequency:

```text
24 hours
```

Allowed user choices may include:

* Every Launch, rate limited to 1 hour;
* Daily;
* Weekly;
* Manual Only.

The exact UI choices require product review.

No frequency creates a background task.

---

# 110. Random Jitter

An opt-in launch check may add bounded random jitter after startup to avoid simultaneous server load.

Jitter is local and does not delay user work.

---

# 111. Clock Changes

Use UTC wall clock for last-check display and monotonic elapsed time within one session.

A backward system-clock change must not create an update request loop.

---

# 112. First Run

The first packaged launch should ask no update question before the developer can use the product.

The Updates settings may later offer opt-in.

A security preview programme may explain the recommendation during onboarding without preselecting consent.

---

# 113. Preference Migration

Update preferences migrate within one channel profile.

They do not copy automatically between Stable and Preview.

---

# 114. Preference Reset

Resetting settings returns to Manual Only.

---

# 115. Enterprise Audit

An enterprise-managed minimum version should be visible in diagnostics and support bundles without exposing private policy secrets.

---

# 116. Security Boundary Limitations

The initial update design does not protect against every threat.

It does not fully prevent:

* update-origin withholding;
* stale release-note text;
* same-user malware changing local preferences;
* compromise of the production signing identity;
* or malicious code in a legitimately signed release.

Controls remain:

* code-signing revocation;
* immutable releases;
* provenance;
* release review;
* and local monotonic checks.

---

# 117. Threat Model

Relevant threats include:

* malicious metadata server;
* DNS or TLS interception;
* redirect to malicious host;
* XML entity attack;
* oversized metadata;
* stale metadata replay;
* downgrade;
* channel confusion;
* package-family substitution;
* publisher substitution;
* malicious signed release;
* compromised signing key;
* hand-off race;
* active-workflow corruption;
* migration failure;
* and silent enterprise policy.

---

# 118. Security Controls

Controls include:

* explicit consent;
* Network Gateway;
* HTTPS;
* origin allowlist;
* redirect limits;
* XML hardening;
* size and time limits;
* package identity validation;
* publisher validation;
* monotonic version;
* highest observed version;
* Windows signature verification;
* signed release evidence;
* immutable publication;
* readiness gate;
* backups;
* Recovery Mode;
* and Trust Centre records.

---

# 119. Privacy Impact

Manual Only creates no update traffic until requested.

Check on Launch creates a small HTTPS request after explicit opt-in.

The update server receives ordinary network metadata such as source IP.

Opure sends no user, project, prompt, secret or persistent device identifier.

---

# 120. Reliability Impact

Windows owns package replacement, reducing custom updater failure modes.

The external hand-off creates less integrated progress and restart behaviour.

Post-update reconciliation must handle unknown external outcomes honestly.

---

# 121. Performance Impact

Metadata checks should not affect startup readiness.

Package update time is primarily Windows deployment and package transfer.

MSIX block-map differential behaviour may reduce bandwidth but must be measured.

---

# 122. Cost Impact

Initial public update infrastructure requires:

* HTTPS hosting for channel pointers;
* immutable package hosting;
* modest bandwidth;
* monitoring;
* and release automation.

No always-running update service is required.

---

# 123. Testing Strategy

ADR-0008 applies.

The updater requires unit, integration, package, recovery, security, policy and user-experience testing.

---

# 124. Unit Tests

Test:

* update policy precedence;
* Manual Only behaviour;
* Check on Launch opt-in;
* rate limiting;
* clock rollback;
* channel validation;
* package-name validation;
* publisher validation;
* package-family validation;
* SemVer parsing;
* MSIX version parsing;
* SemVer-to-MSIX mapping;
* monotonic comparison;
* highest-observed version;
* skip-version state;
* migration classification;
* readiness classification;
* release-state transitions;
* and Trust Centre event generation.

---

# 125. XML Parser Tests

Test:

* valid App Installer schemas;
* unsupported schema;
* DTD;
* external entity;
* external schema;
* duplicate identity;
* duplicate main package;
* missing package;
* oversized element;
* excessive depth;
* invalid encoding;
* unknown namespace;
* unexpected optional package;
* unexpected dependency;
* package bundle;
* and malformed URI.

---

# 126. Network Tests

Test:

* approved HTTPS source;
* HTTP rejection;
* DNS failure;
* TLS failure;
* expired TLS certificate;
* wrong host;
* same-host redirect;
* cross-host redirect;
* redirect loop;
* too many redirects;
* downgrade redirect;
* oversized compressed response;
* decompression bomb;
* timeout;
* cancellation;
* proxy;
* offline mode;
* and metered-network suppression.

---

# 127. Privacy Tests

Inspect requests and prove they contain no:

* project name;
* project path;
* repository remote;
* user name;
* email;
* device ID;
* hardware identifier;
* plugin list;
* model list;
* Vault metadata;
* prompt;
* source code;
* or persistent tracking token.

---

# 128. Policy Tests

Test:

* default Manual Only;
* explicit opt-in;
* explicit opt-out;
* user preference;
* enterprise disable;
* enterprise source;
* enterprise minimum;
* Windows App Installer automatic policy;
* Store managed;
* diagnostic build;
* Development package;
* and profile reset.

---

# 129. No-Background Test

After installation and without launching Opure:

* monitor network;
* monitor scheduled tasks;
* monitor background tasks;
* monitor services;
* and verify no Opure-configured update check occurs.

---

# 130. Startup Test

With Manual Only:

* launch;
* observe no update request;
* and reach local readiness.

With Check on Launch:

* launch;
* reach local readiness first;
* perform one bounded request;
* and keep UI responsive.

---

# 131. App Installer Association Tests

Test:

* initial installation through `.appinstaller`;
* `GetAppInstallerInfo`;
* source URI;
* no UpdateSettings;
* direct `.msix` installation without association;
* association establishment on later hand-off;
* and enterprise-modified settings.

---

# 132. App Installer Policy Inspection Tests

Where APIs support it, inspect and display:

* OnLaunch;
* AutomaticBackgroundTask;
* ShowPrompt;
* UpdateBlocksActivation;
* ForceUpdateFromAnyVersion;
* LastChecked;
* PausedUntil;
* IsAutoRepairEnabled;
* and PolicySource.

---

# 133. Availability Discovery Tests

Test:

* no update;
* valid update;
* optional update;
* security update;
* unsupported direct upgrade;
* withdrawn release;
* invalid product version;
* invalid package version;
* wrong channel;
* wrong publisher;
* wrong package name;
* wrong architecture;
* wrong host;
* and missing release summary.

---

# 134. Rollback Tests

Test:

* candidate lower than installed;
* candidate lower than highest observed;
* candidate equal installed;
* stale channel pointer;
* withdrawn old version;
* and local updater-state deletion.

No scenario installs a lower package automatically.

---

# 135. Channel Tests

Test:

* Stable sees only stable;
* Preview sees preview, beta and RC;
* Stable rejects Preview;
* Preview rejects Stable package family;
* Development rejects production;
* side-by-side Stable and Preview;
* isolated profiles;
* isolated IPC;
* and no shared skip state.

---

# 136. Release Notes Tests

Test:

* safe Markdown;
* plain text fallback;
* hostile HTML;
* script;
* external image;
* oversized notes;
* invalid link;
* deceptive display URL;
* offline cache;
* and advisory trust label.

---

# 137. Update Readiness Tests

Create active:

* patch transaction;
* database migration;
* build;
* test;
* AI inference;
* MCP call;
* plugin call;
* pending approval;
* and checkpointed workflow.

Verify correct blocking or deferrable classification.

---

# 138. Shutdown Tests

Test:

* graceful shutdown;
* cancellation;
* timeout;
* user cancels force;
* user accepts force;
* orphan Worker;
* orphan Plugin Host;
* stuck external command;
* database flush;
* Trust Centre flush;
* and process-tree cleanup.

---

# 139. Backup Tests

Test:

* no migration;
* backup required;
* database backup;
* Vault recovery checkpoint;
* profile manifest;
* backup failure;
* disk full;
* permission denied;
* and cleanup retention.

A backup failure blocks the update when required.

---

# 140. Handoff Tests

Test:

* valid local `.appinstaller`;
* shell activation;
* App Installer opens;
* shell activation failure;
* metadata file removed too early;
* user cancels;
* user installs;
* package unchanged;
* package changed;
* and application shutdown race.

---

# 141. No Restricted Capability Test

Inspect the package manifest and verify that the initial release does not declare:

```text
packageManagement
```

---

# 142. Package Update Tests

Test:

* Preview to Preview;
* Preview to Beta;
* Beta to RC;
* Stable patch;
* Stable minor;
* MSIX to bundle transition;
* package running;
* package closed;
* network interruption;
* disk full;
* signature failure;
* and publisher mismatch.

---

# 143. Post-Update Tests

On next launch, test:

* expected package installed;
* no package change;
* different expected version;
* unexpected higher version;
* package family changed;
* product-version mismatch;
* successful migration;
* failed migration;
* Runtime crash;
* Safe Mode;
* Recovery Mode;
* and interrupted update receipt.

---

# 144. Migration Tests

Test:

* None;
* Compatible;
* Forward Only;
* Backup Required;
* Manual Action Required;
* Unsupported Direct Upgrade;
* retry;
* idempotency;
* restoration;
* and skipped intermediate version.

---

# 145. Data Preservation Tests

Verify update never deletes or overwrites unexpectedly:

* source repositories;
* uncommitted source changes;
* Vault;
* project databases;
* project memory;
* workflow checkpoints;
* plugins;
* local models;
* logs;
* and exports.

---

# 146. Plugin Compatibility Tests

After update:

* compatible plugin remains available;
* incompatible plugin is disabled;
* plugin code is not deleted;
* permissions are not broadened;
* and review is visible.

---

# 147. Model Compatibility Tests

After update:

* compatible local model remains;
* incompatible model is marked;
* no automatic download occurs;
* no automatic deletion occurs;
* and model path remains unchanged.

---

# 148. Store Tests

For Store-shaped installation:

* direct update is disabled;
* Store-managed status is shown;
* no `.appinstaller` request occurs;
* and no duplicate prompt occurs.

---

# 149. Diagnostic Tests

For unpackaged ZIP:

* no production check;
* no package update;
* release-page link only;
* isolated profile;
* and no Stable package mutation.

---

# 150. Enterprise Tests

Test:

* updates disabled;
* managed source;
* minimum version;
* App Installer OnLaunch policy;
* AutomaticBackgroundTask policy;
* ForceUpdateFromAnyVersion policy;
* policy source display;
* and inability of user preference to override.

---

# 151. Security Tests

Test:

* malicious origin;
* compromised DNS simulation;
* wrong TLS certificate;
* redirect to attacker;
* XML entity;
* metadata replay;
* package-family substitution;
* publisher substitution;
* malicious unsigned package;
* signed wrong publisher;
* signed older package;
* tampered package;
* and compromised local updater cache.

---

# 152. Withdrawal Tests

Simulate:

* release published;
* release withdrawn;
* channel advanced;
* installed withdrawn release;
* update warning;
* replacement;
* and no asset replacement.

---

# 153. Critical Security Tests

Simulate:

* Critical Security classification;
* Manual Only;
* launch opt-in;
* offline user;
* source unavailable;
* and enterprise minimum.

Verify no silent installation while messaging remains prominent.

---

# 154. Fault Injection

Inject failure:

* before metadata commit;
* after metadata commit;
* during readiness;
* during backup;
* after backup;
* before hand-off;
* after hand-off;
* during shutdown;
* after package replacement;
* during migration;
* after migration but before profile commit;
* and during Trust Centre completion.

---

# 155. Performance Tests

Measure:

* metadata request;
* parse;
* validation;
* update UI render;
* readiness analysis;
* backup;
* graceful shutdown;
* App Installer launch;
* package update;
* cold start after update;
* and migration.

---

# 156. Bandwidth Tests

Measure:

* `.appinstaller` file;
* release summary;
* release notes;
* full package;
* and changed package transfer

under representative releases.

---

# 157. Accessibility Tests

Verify:

* keyboard check;
* keyboard install;
* Narrator announcements;
* focus after result;
* progress status;
* error status;
* high contrast;
* reduced motion;
* and no forced countdown.

---

# 158. Prototype Plan

## 158.1 Prototype A — Manual Discovery

Fetch a development `.appinstaller` through the Network Gateway.

Parse and display a newer Development package.

---

## 158.2 Prototype B — No Automatic Settings

Install through an `.appinstaller` containing no `UpdateSettings`.

Monitor for background and launch-time Windows update traffic.

---

## 158.3 Prototype C — App Installer Information

Read associated URI and effective policy.

Verify direct `.msix` fallback.

---

## 158.4 Prototype D — Handoff

Open a local downloaded `.appinstaller` through Windows shell activation.

Install a higher Development package.

---

## 158.5 Prototype E — Readiness

Run builds, tests, workers and plugins.

Prepare and cancel the update safely.

---

## 158.6 Prototype F — Migration

Install a package with a database schema migration.

Verify backup, success and failed-migration Recovery Mode.

---

## 158.7 Prototype G — Side by Side

Update Stable-test and Preview-test package families independently.

---

## 158.8 Prototype H — Policy

Enable opt-in launch check, enterprise disable and App Installer external policy.

---

## 158.9 Prototype I — Withdrawal

Publish a disposable signed package, mark it withdrawn and advance to a corrective package.

---

## 158.10 Prototype J — Store Shape

Simulate Store-managed distribution and verify direct updater suppression.

---

# 159. Implementation Plan

1. Record founder review.
2. Implement Update Policy contracts.
3. Implement Update Source Registry.
4. Define Development channel source.
5. Create `.appinstaller` templates without UpdateSettings.
6. Add Network Gateway update request classification.
7. Add bounded HTTP client behaviour.
8. Add strict App Installer XML parser.
9. Add identity and version validation.
10. Add highest-observed state.
11. Add update availability state machine.
12. Add Update settings UI.
13. Add manual check.
14. Add opt-in launch check.
15. Add effective Windows policy display.
16. Add release summary projection.
17. Add safe release-note rendering.
18. Implement readiness aggregation.
19. Implement backup planner.
20. Implement hand-off staging.
21. Implement Windows shell activation.
22. Implement controlled shutdown.
23. Implement post-update reconciliation.
24. Implement migration Recovery Mode.
25. Add channel isolation tests.
26. Add Store and diagnostic modes.
27. Add Trust Centre events.
28. Add security and privacy tests.
29. Run package update rehearsal.
30. Complete security review.
31. Accept, amend or reject the ADR.

---

# 160. Owners

| Area                   | Owner                      |
| ---------------------- | -------------------------- |
| Product policy         | Founder                    |
| Update Coordinator     | Runtime Architecture Owner |
| Network requests       | Network Gateway Owner      |
| Package metadata       | Packaging Owner            |
| Release information    | Release Engineering Owner  |
| Readiness and shutdown | Runtime and Process Owners |
| Database migration     | Persistence Owner          |
| Vault recovery         | Secrets Owner              |
| Recovery Mode          | Recovery Owner             |
| Desktop experience     | Desktop Owner              |
| Trust Centre           | Trust Centre Owner         |
| Tests                  | Test Architecture Owner    |
| Enterprise policy      | Future Enterprise Owner    |

---

# 161. Suggested Repository Structure

```text
src/
├── Runtime/
│   └── Opure.Runtime.Updates/
├── Platform/
│   └── Opure.Platform.Windows.Updates/
├── Desktop/
│   └── Opure.Desktop.Updates/
└── Packaging/
    └── Opure.Packaging.Windows/
        └── AppInstaller/

tests/
└── Updates/
    ├── Opure.Updates.UnitTests/
    ├── Opure.Updates.IntegrationTests/
    ├── Opure.Updates.SecurityTests/
    └── Opure.Updates.AcceptanceTests/

eng/
├── create-appinstaller.ps1
├── verify-appinstaller.ps1
└── publish-update-source.ps1
```

Exact project count follows ADR-0010 and should remain minimal.

---

# 162. Contract Sketch

Conceptual command:

```text
CheckForUpdates
```

Fields:

```text
channel
reason
user_initiated
correlation_id
```

---

## 162.1 Result

```text
status
current_product_version
current_package_version
candidate_product_version
candidate_package_version
release_classification
migration_classification
package_size
release_notes
source
trust_summary
checked_at
```

---

## 162.2 Prepare Command

```text
PrepareUpdate
```

Returns:

```text
readiness
blockers
cancellable_operations
backup_plan
restart_plan
warnings
```

---

## 162.3 Handoff Command

```text
ApproveUpdateHandoff
```

Requires:

```text
candidate_identity
review_token
readiness_token
user_approval
```

Tokens are short lived and local.

---

# 163. Update State Machine

```text
NotChecked
    ↓
Checking
    ├── Current
    ├── Available
    ├── Rejected
    └── Failed

Available
    ↓
Reviewing
    ├── Deferred
    ├── Skipped
    └── Preparing

Preparing
    ├── Blocked
    ├── Cancelled
    └── Ready

Ready
    ↓
HandedOff
    ├── UnknownOrCancelled
    └── PackageChanged

PackageChanged
    ↓
Reconciling
    ├── Updated
    └── RecoveryRequired
```

---

# 164. Invalid State Transitions

Prohibited:

* NotChecked → Installed;
* Rejected → HandedOff;
* Blocked → HandedOff;
* HandedOff → Updated without next-launch verification;
* MigrationFailed → Updated;
* Stable → Preview through update;
* Higher Version → Lower Version;
* StoreManaged → Direct PackageManager Update;
* and Diagnostic → Production Update.

---

# 165. Idempotent Recovery

If the application crashes during reconciliation:

* retain update receipt;
* identify completed migration steps;
* continue only idempotent steps;
* and require recovery for uncertain destructive steps.

---

# 166. Database Schema

A conceptual updater store may include:

```text
update_policy
update_sources
update_checks
update_candidates
update_decisions
update_high_watermarks
update_handoffs
update_reconciliations
update_exceptions
```

Service ownership and migrations follow ADR-0005.

---

# 167. Retention

Suggested retention:

* successful checks: latest 50 per channel;
* failed checks: 90 days or bounded count;
* user decisions: current version plus relevant history;
* hand-off receipts: until successful reconciliation plus 90 days;
* release-security events: Trust Centre retention policy;
* and cached release notes: bounded by count and bytes.

---

# 168. Data Deletion

Clearing update history must not:

* lower installed package version;
* permit downgrade;
* remove Trust Centre security records contrary to policy;
* or remove migration backups prematurely.

---

# 169. Update Settings UI

Suggested sections:

```text
Current Installation
Update Policy
Channel
Update Source
Windows Update Policy
Last Check
Available Update
Release Verification
History
Enterprise Policy
```

---

# 170. Default UI Copy

Suggested policy text:

> Opure checks for updates only when you ask. You can opt in to a check when Opure starts. Update checks contact the displayed Opure update source but do not send project content, prompts, secrets or a persistent device identifier. Updates are never installed automatically by Opure.

---

# 171. Handoff UI Copy

Suggested text:

> Opure will save recoverable state, stop active services and open Windows App Installer. Windows will display the package publisher and ask you to install the update. Opure records success only after the updated application starts and completes its migrations.

---

# 172. Downgrade UI Copy

Suggested text:

> Opure does not automatically install an older package. Installing older binaries does not reverse database, Vault or workflow migrations. Use a documented recovery path and verified backup when a downgrade is explicitly supported.

---

# 173. Unavailable UI Copy

Suggested text:

> Opure could not check the approved update source. Your installed application continues to work locally. No alternate server was contacted.

---

# 174. App Installer File Shape

Conceptual Stable file:

```xml
<?xml version="1.0" encoding="utf-8"?>
<AppInstaller
    xmlns="http://schemas.microsoft.com/appx/appinstaller/2021"
    Version="$(AppInstallerVersion)"
    Uri="$(StableAppInstallerUri)">

  <MainPackage
      Name="$(StablePackageName)"
      Publisher="$(Publisher)"
      Version="$(MsixVersion)"
      ProcessorArchitecture="x64"
      Uri="$(ImmutablePackageUri)" />
</AppInstaller>
```

The initial file intentionally contains no `UpdateSettings`.

The exact schema namespace and package or bundle element require validation.

---

# 175. Preview App Installer File

Preview uses:

* Preview package name;
* Preview URI;
* Preview package family;
* and Preview channel package.

It cannot reference the Stable package identity.

---

# 176. App Installer File Version

The App Installer document's own version must be monotonic.

The initial mapping may use the target MSIX package version.

The implementation must validate Windows schema semantics.

---

# 177. Package URI

The package URI should be:

* HTTPS;
* immutable;
* versioned;
* allowlisted;
* and point to the exact signed release asset.

Avoid a mutable `latest.msix` URL as the package target.

---

# 178. Release Notes URI

Release notes should point to an immutable versioned release page or asset.

---

# 179. Update Source Publishing

Publishing one release should:

1. publish immutable package and evidence;
2. verify public package;
3. generate channel `.appinstaller`;
4. validate identity and version;
5. publish channel pointer atomically;
6. fetch from public origin;
7. validate again;
8. and record update-source publication.

The channel pointer is updated only after release immutability and verification.

---

# 180. Atomic Channel Pointer

The host should replace the small channel pointer atomically.

A partial file must not be served.

---

# 181. Publication Failure

If channel-pointer publication fails:

* release remains available manually;
* current installed versions remain functional;
* no package asset is changed;
* and the release is not falsely described as available through automatic discovery.

---

# 182. Release Withdrawal Publication

A withdrawal should:

* stop the pointer from offering the withdrawn package;
* point to a higher corrective version when available;
* retain the immutable release record;
* and publish advisory content.

---

# 183. Source Monitoring

Monitor:

* HTTPS availability;
* certificate validity;
* content type;
* metadata parse;
* package URI;
* package availability;
* package hash;
* and expected version.

Monitoring does not make product background requests.

---

# 184. Server Logging

Update hosting should retain only ordinary operational logs according to privacy policy.

Do not add unique device tokens to correlate clients.

---

# 185. CDN

A CDN may serve update metadata and package files.

Requirements:

* HTTPS;
* origin control;
* immutable versioned assets;
* cache invalidation for channel pointer;
* no content transformation;
* and security logging.

---

# 186. Cache-Control

Versioned package assets may use long immutable caching.

The channel pointer should use short bounded caching and validation.

Exact headers require hosting design.

---

# 187. Content Types

Serve expected content types for:

* `.appinstaller`;
* `.msix`;
* `.msixbundle`;
* JSON release summary;
* and Markdown or text notes.

The client should not rely solely on content type.

---

# 188. Release Host Compromise

If the update host is compromised:

1. suspend channel pointer publication;
2. notify users through independent channels;
3. verify package signer activity;
4. verify immutable releases;
5. rotate hosting credentials;
6. audit served metadata;
7. republish known-good pointer;
8. and consider TUF adoption.

If the signing identity is not compromised, arbitrary malicious MSIX should fail Windows publisher validation.

---

# 189. Signing Compromise

ADR-0014 applies.

Disable updater promotion of affected packages.

Publish a higher corrective version under the recovered trust path.

---

# 190. Package Family Compromise

A package signed with the expected publisher and family but malicious due to signing compromise is a critical incident.

The updater alone cannot solve it.

Use:

* certificate revocation;
* release withdrawal;
* security advisory;
* new package;
* and possibly publisher or package-family migration.

---

# 191. Enterprise Source Compromise

An enterprise administrator owns its source and policy.

Opure should still verify:

* expected package family;
* publisher;
* version;
* and Windows signature.

---

# 192. Store Incident

Store-distributed incident response follows Store mechanisms plus Opure's release advisory.

---

# 193. Update Rollback versus Recovery

Use precise terms:

* **Package rollback** — install older application package.
* **Data restore** — restore database, Vault or profile backup.
* **Workflow recovery** — reconcile interrupted operations.
* **Release withdrawal** — mark a release unsafe.
* **Corrective update** — install a newer fixed package.

Do not call one operation all four.

---

# 194. No Automatic Package Rollback

Opure does not automatically install an older package after failed migration.

Reasons:

* migrated data may be incompatible;
* package version normally must increase;
* `ForceUpdateFromAnyVersion` is disabled;
* and automatic downgrade can create a second failure.

---

# 195. Corrective Release

Preferred recovery from a bad package is a higher patch release.

---

# 196. Backup Restore

A backup restore requires:

* exact backup;
* current and target schema;
* Vault compatibility;
* user confirmation;
* and recovery record.

---

# 197. Safe Mode Update Check

Safe Mode may permit a manual metadata check if:

* Network Gateway is healthy;
* user requests it;
* and the update system itself is not the suspected failure.

No automatic Safe Mode check.

---

# 198. Recovery Mode Update Check

Recovery Mode may offer:

* open release page;
* manual check;
* or reinstall current package.

It must not update automatically while state is uncertain.

---

# 199. Plugin Host Failure

A broken third-party plugin should not block package update if it can be disabled safely.

---

# 200. Active Build

A build may be cancelled or allowed to finish.

The update does not kill it silently.

---

# 201. Active Patch

An applying patch is a hard blocker until commit or rollback completes.

---

# 202. Active Git Operation

A repository mutation is a blocker when Opure owns the process.

External Git processes are detected where practical and warned about.

---

# 203. Active Model Inference

Inference may be cancellable.

The user is told that generated output may be incomplete.

---

# 204. Active Provider Call

External provider calls are cancelled through adapters and recorded.

No update hand-off waits indefinitely for a provider.

---

# 205. Pending Approval

Pending approval state is persisted before shutdown.

---

# 206. Unsaved Editor State

Desktop state is saved to the UI session store.

Source file buffers are saved only through normal explicit file semantics.

The updater does not silently save source modifications.

---

# 207. Accessibility State

Focus and open views may be restored after update where safe.

---

# 208. Update Completion Notification

After successful reconciliation, show:

* previous version;
* new version;
* migration result;
* restored session state;
* and release-note link.

---

# 209. Update History

Display:

* check time;
* candidate;
* decision;
* hand-off;
* detected package transition;
* migration;
* and result.

No remote tracking ID.

---

# 210. CLI

A future CLI may support:

```text
opure update status
opure update check
opure update review
```

It should not support unattended production update initially.

---

# 211. Automation API

Plugins and MCP servers cannot initiate package update.

They may query non-sensitive current-version status if permission allows.

---

# 212. AI Authority

AI may:

* summarise release notes;
* explain migration;
* or compare versions.

AI may not:

* enable checks;
* approve an update;
* bypass blockers;
* choose a channel;
* or force package installation.

---

# 213. Update Recommendations

Recommendations are deterministic policy plus release metadata.

Do not invent urgency through AI-generated wording.

---

# 214. Security Exception

A temporary update-policy exception requires:

* exact rule;
* reason;
* scope;
* owner;
* expiry;
* and founder approval.

---

# 215. PackageManagement Future Gate

If Opure later requests `packageManagement`, a superseding or amending ADR must cover:

* restricted capability justification;
* Store implications;
* attack surface;
* progress UI;
* cancellation;
* elevation;
* package authority;
* and direct comparison with external App Installer hand-off.

---

# 216. App Installer Automatic Future Gate

If Opure later enables `OnLaunch` or background checking, the decision must cover:

* consent;
* Trust Centre visibility gap;
* silent application behaviour;
* activation blocking;
* enterprise interaction;
* and network privacy.

---

# 217. Forced Update Future Gate

A forced update requires:

* threat model;
* legal and support need;
* offline behaviour;
* local-only functionality impact;
* migration safety;
* recovery;
* and explicit Charter amendment if human control is materially reduced.

---

# 218. TUF Future Gate

A future TUF ADR should specify:

* root role;
* targets role;
* snapshot role;
* timestamp role;
* threshold;
* key custody;
* expiry;
* consistent snapshots;
* delegation;
* mirrors;
* conformance suite;
* and client implementation.

---

# 219. Acceptance Criteria

This ADR may move to **Accepted** when all of the following hold.

## Policy and Consent

* [ ] Manual Only is the default.
* [ ] No request occurs before user action under Manual Only.
* [ ] Check on Launch requires explicit opt-in.
* [ ] Opt-in text identifies source, frequency and data sent.
* [ ] Check on Launch is rate limited.
* [ ] Launch check does not delay local readiness.
* [ ] Offline Mode disables checks.
* [ ] Preference reset returns to Manual Only.
* [ ] Enterprise policy precedence is implemented.
* [ ] Effective policy and source are visible.
* [ ] Store and diagnostic modes are distinct.

## App Installer Configuration

* [ ] Stable `.appinstaller` exists.
* [ ] Preview `.appinstaller` exists.
* [ ] Development `.appinstaller` exists.
* [ ] No initial file contains `UpdateSettings`.
* [ ] No `OnLaunch` is configured by Opure.
* [ ] No `AutomaticBackgroundTask` is configured.
* [ ] No `ShowPrompt` policy is configured.
* [ ] No `UpdateBlocksActivation` is configured.
* [ ] No `ForceUpdateFromAnyVersion` is configured.
* [ ] No dependency on `ms-appinstaller:` exists.
* [ ] Package manifest does not request `packageManagement`.
* [ ] Effective externally configured App Installer policy is visible where possible.

## Network and Privacy

* [ ] Every check uses Network Gateway.
* [ ] HTTPS is required.
* [ ] Source host is allowlisted.
* [ ] Redirects are bounded and allowlisted.
* [ ] TLS validation cannot be disabled.
* [ ] Response size is bounded.
* [ ] Timeout is bounded.
* [ ] XML DTDs and external entities are disabled.
* [ ] Update requests contain no project or secret data.
* [ ] No persistent device identifier is sent.
* [ ] No hidden analytics is coupled to update checks.
* [ ] Update source outage does not affect local functionality.

## Validation

* [ ] Package name is validated.
* [ ] Publisher is validated.
* [ ] Package family is validated.
* [ ] Channel is validated.
* [ ] Architecture is validated.
* [ ] Product SemVer is validated where available.
* [ ] MSIX deployment version is validated.
* [ ] SemVer-to-MSIX mapping is checked.
* [ ] Candidate package version must be higher.
* [ ] Highest observed version is persisted.
* [ ] Stale or rolled-back metadata is rejected.
* [ ] Versioned package URI is immutable.
* [ ] Windows performs final package-signature verification.
* [ ] Release evidence is available.
* [ ] Metadata trust limitations are stated honestly.

## User Experience

* [ ] Update view shows current and candidate versions.
* [ ] Update view shows channel and publisher.
* [ ] Update view shows size, release notes and known issues.
* [ ] Update view shows migration and restart impact.
* [ ] Remote release notes are rendered safely.
* [ ] User can cancel the check.
* [ ] User can defer an optional update.
* [ ] User can skip an optional exact version.
* [ ] No countdown forces installation.
* [ ] Critical update remains user controlled.
* [ ] Accessibility tests pass.

## Readiness and Handoff

* [ ] Active operations are classified.
* [ ] Applying patches and migrations block update.
* [ ] Cancellable work is shown.
* [ ] Required backup completes before hand-off.
* [ ] Backup failure blocks update.
* [ ] `.appinstaller` is staged safely.
* [ ] Metadata is revalidated before hand-off.
* [ ] Windows App Installer opens.
* [ ] Opure shuts down gracefully.
* [ ] Processes are cleaned up.
* [ ] Handoff is not recorded as installation success.
* [ ] User cancellation is handled honestly.

## Post-Update

* [ ] Next launch detects package transition.
* [ ] Product and package versions reconcile.
* [ ] Migrations are service owned.
* [ ] Migrations are idempotent or recoverable.
* [ ] Failed migration enters Recovery Mode.
* [ ] Safe Mode remains available.
* [ ] Source repositories are preserved.
* [ ] Vault and databases are preserved.
* [ ] Plugins and models are preserved.
* [ ] Incompatible plugins are disabled rather than deleted.
* [ ] Completion is recorded only after health checks.
* [ ] Update history is visible.

## Channels and Distribution

* [ ] Stable cannot receive Preview.
* [ ] Preview cannot replace Stable family.
* [ ] Development cannot contact production source by default.
* [ ] Stable and Preview update independently.
* [ ] Channel switching is a separate operation.
* [ ] Store installation disables direct updater.
* [ ] Diagnostic ZIP cannot self-update a package.
* [ ] Enterprise managed source remains visible.

## Security and Recovery

* [ ] Tampered metadata fails.
* [ ] Wrong publisher fails.
* [ ] Wrong package family fails.
* [ ] Wrong channel fails.
* [ ] Older package fails.
* [ ] Unsigned or invalid package fails in Windows.
* [ ] Withdrawal simulation succeeds.
* [ ] Signing-compromise runbook integrates.
* [ ] Update-origin compromise runbook exists.
* [ ] Security review is complete.
* [ ] Founder approval is recorded.

---

# 220. Evidence Required Before Acceptance

* [ ] App Installer schema review.
* [ ] No-UpdateSettings manifest evidence.
* [ ] Network capture under Manual Only.
* [ ] Network capture under opt-in launch check.
* [ ] Privacy request report.
* [ ] Source and redirect validation report.
* [ ] XML parser security report.
* [ ] Package identity validation report.
* [ ] Version mapping and rollback report.
* [ ] Highest-observed state report.
* [ ] App Installer association report.
* [ ] Effective Windows policy display.
* [ ] Manual discovery screenshot.
* [ ] Update review screenshot.
* [ ] Accessibility report.
* [ ] Readiness report.
* [ ] Backup report.
* [ ] Handoff report.
* [ ] Process-shutdown report.
* [ ] Package update report.
* [ ] Post-update reconciliation report.
* [ ] Migration success report.
* [ ] Migration failure and Recovery Mode report.
* [ ] Source repository preservation report.
* [ ] Vault and database preservation report.
* [ ] Stable and Preview isolation report.
* [ ] Store-mode report.
* [ ] Diagnostic-mode report.
* [ ] Enterprise-policy report.
* [ ] Withdrawal simulation.
* [ ] Update-origin incident tabletop.
* [ ] Full release update rehearsal.
* [ ] Security review.
* [ ] Founder approval.

---

# 221. Known Limitations

* The public update domain is not selected.
* The CDN or hosting provider is not selected.
* The release-summary format is not final.
* Release-summary content is initially advisory rather than independently signed.
* The initial model does not provide cryptographic freeze-attack detection.
* A compromised update origin may withhold or replay discovery metadata.
* Check on Launch is opt-in and may delay awareness for Manual Only users.
* App Installer hand-off is less integrated than an in-process updater.
* Opure may not know whether the external installer completed until next launch.
* Automatic restart is not guaranteed.
* The `CheckUpdateAvailabilityAsync` API has documented limitations and is not the sole mechanism.
* The initial package does not request `packageManagement`.
* Windows Settings or enterprise policy may change App Installer behaviour.
* Store update integration is deferred.
* WinGet distribution is deferred.
* TUF is deferred.
* Mirrors are deferred.
* Pre-download is deferred.
* Forced updates are not supported.
* Automatic package rollback is not supported.
* Plugin and model updates remain separate.
* Metered-network and battery APIs require prototype confirmation.
* A sole founder cannot provide independent approval for critical update policy changes.

---

# 222. Open Questions

* What public domain should host Stable update metadata?
* What public domain should host Preview metadata?
* Should package assets remain on GitHub Releases?
* Which known redirect hosts are required for GitHub assets?
* Should the channel pointer be served through GitHub Pages, object storage or a dedicated CDN?
* What cache headers should the channel pointer use?
* What is the final release-summary format?
* Should release summaries become independently signed?
* Should a TUF client be adopted before Version 1.0?
* Is there a maintained conformant .NET TUF client acceptable to Opure?
* Should a small isolated helper based on an official TUF implementation be considered?
* Should the updater verify GitHub attestations in-process?
* Which Sigstore or attestation verification library is acceptable?
* Should the updater fetch only `.appinstaller` or also a summary document?
* How should product SemVer be obtained securely before installation?
* Should package SHA-256 be displayed before App Installer hand-off?
* Should the `.appinstaller` file itself be included in the immutable release?
* How should a mutable channel pointer reference an immutable `.appinstaller`?
* Should Stable and Preview use separate hosts?
* Should Check on Launch offer Daily and Weekly choices?
* Should Check on Launch be recommended during Preview onboarding?
* Should metered networks suppress launch checks automatically?
* Should battery saver suppress checks?
* Should update checks use a constant user agent?
* Should Windows App Installer policy inspection be required on every launch?
* How should the known `CheckUpdateAvailabilityAsync` issue be wrapped?
* Can Windows shell activation reliably open a local `.appinstaller` after Runtime shutdown?
* Should Opure remain running until App Installer displays?
* How should hand-off failure be detected?
* Can the app request restart after an external App Installer update without `packageManagement`?
* Should a small non-privileged restart helper be introduced?
* Which operations are hard blockers?
* What graceful-shutdown timeout is appropriate?
* Which backups are mandatory for each migration class?
* How long should pre-update backups be retained?
* Should the user be able to delete a backup immediately?
* What direct upgrade matrix should each release provide?
* How should a Stable user be notified of a withdrawn release under Manual Only?
* Should an independent security advisory feed exist?
* How can a security advisory feed remain privacy preserving?
* Should a critical update disable one vulnerable external capability?
* What governance would permit forced update in the future?
* How should Store-managed status be detected reliably?
* Should WinGet be offered as an optional alternative?
* What enterprise policy format should be supported?
* Should enterprise sources support SMB or local files?
* Should enterprise-managed `.appinstaller` automatic checks be shown as a warning or neutral status?
* Should Opure provide a command to open Windows App Installer settings?
* When should `packageManagement` be reconsidered?
* How should MSIX-to-bundle transition appear?
* How should ARM64 update selection work?
* Should package transfer size be measured through Windows deployment logs?
* How should plugin compatibility be presented after update?
* Should update history be exportable?
* What permanent retention applies to update security events?
* How should origin compromise trigger TUF adoption?
* What release volume justifies mirrors?
* How should a publisher or package-family migration be handled?

---

# 223. Deferred Decisions

This ADR intentionally defers:

* public update hosting;
* CDN selection;
* signed metadata;
* TUF;
* PackageManager restricted capability;
* automatic App Installer checks;
* background updates;
* forced updates;
* update pre-download;
* automatic restart;
* Store integration;
* WinGet distribution;
* enterprise deployment tooling;
* plugin updating;
* model updating;
* security-advisory feed;
* cross-platform updating;
* and package-family migration.

---

# 224. Alternatives Rejected

Automatic App Installer launch checks are not enabled because they bypass Opure's Network Gateway and can later apply a deferred update outside the in-app decision.

Automatic background tasks are rejected because they create hidden periodic network activity and package mutation.

PackageManager self-update is deferred because it requires the restricted `packageManagement` capability and expands application authority.

A third-party updater is rejected because it duplicates MSIX package lifecycle and introduces another privileged supply-chain dependency.

Store-only updating is rejected because direct preview and enterprise users need another path.

Browser-only manual download is retained as fallback but does not provide enough migration and recovery integration.

WinGet is optional distribution, not the primary in-product lifecycle.

A custom updater service is rejected because per-user MSIX does not justify elevated file-replacement code.

TUF is deferred because Opure should not create an incomplete custom secure-update protocol without a maintained implementation and conformance evidence.

---

# 225. Official and Primary Evidence References

## Microsoft App Installer and MSIX

* [App Installer file overview](https://learn.microsoft.com/en-us/windows/msix/app-installer/app-installer-file-overview)
* [Configure App Installer update settings](https://learn.microsoft.com/en-us/windows/msix/app-installer/update-settings)
* [Auto-update and repair apps](https://learn.microsoft.com/en-us/windows/msix/app-installer/auto-update-and-repair--overview)
* [Create an App Installer file manually](https://learn.microsoft.com/en-us/windows/msix/app-installer/how-to-create-appinstaller-file)
* [UpdateSettings schema](https://learn.microsoft.com/en-us/uwp/schemas/appinstallerschema/element-update-settings)
* [AutomaticBackgroundTask schema](https://learn.microsoft.com/en-us/uwp/schemas/appinstallerschema/element-s2-automaticbackgroundtask)
* [ForceUpdateFromAnyVersion schema](https://learn.microsoft.com/en-us/uwp/schemas/appinstallerschema/element-s4-forceupdatefromanyversion)
* [App package updates](https://learn.microsoft.com/en-us/windows/msix/app-package-updates)
* [Update non-Store apps from code](https://learn.microsoft.com/en-us/windows/msix/non-store-developer-updates)
* [`Package.GetAppInstallerInfo`](https://learn.microsoft.com/en-us/uwp/api/windows.applicationmodel.package.getappinstallerinfo)
* [`AppInstallerInfo`](https://learn.microsoft.com/en-us/uwp/api/windows.applicationmodel.appinstallerinfo)
* [`Package.CheckUpdateAvailabilityAsync`](https://learn.microsoft.com/en-us/uwp/api/windows.applicationmodel.package.checkupdateavailabilityasync)
* [`PackageManager.RequestAddPackageByAppInstallerFileAsync`](https://learn.microsoft.com/en-us/uwp/api/windows.management.deployment.packagemanager.requestaddpackagebyappinstallerfileasync)
* [`PackageUpdateAvailability`](https://learn.microsoft.com/en-us/uwp/api/windows.applicationmodel.packageupdateavailability)

## GitHub Release Integrity

* [GitHub immutable releases](https://docs.github.com/en/code-security/concepts/supply-chain-security/immutable-releases)
* [GitHub artefact attestations](https://docs.github.com/en/actions/concepts/security/artifact-attestations)

## Future Metadata Architecture

* [The Update Framework specification](https://theupdateframework.github.io/specification/)
* [TUF roles and metadata](https://updateframework.com/docs/metadata/)
* [The Update Framework organisation](https://github.com/theupdateframework)

Windows APIs, App Installer policy, GitHub capabilities and TUF implementations can change.

All implementation choices must be revalidated before acceptance and before enabling any automatic behaviour.

---

# 226. Review Record

| Date         | Reviewer           | Decision | Notes                                                                                       |
| ------------ | ------------------ | -------- | ------------------------------------------------------------------------------------------- |
| 18 July 2026 | Architecture draft | Proposed | Manual-by-default, app-controlled discovery with Windows App Installer hand-off recommended |

---

# 227. Approval

## Founder or Product Approval

* **Name:** Christopher Dyer
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Update consent, channel and forced-update policy review required

## Update Architecture Approval

* **Name or role:** Update and Lifecycle Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Discovery, hand-off and reconciliation evidence required

## Security Approval

* **Name or role:** Security Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Origin, rollback, XML, package identity and incident review required

## Packaging Approval

* **Name or role:** Packaging Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** App Installer association, version ordering and package transition required

## Persistence and Recovery Approval

* **Name or role:** Persistence and Recovery Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Backups, migrations, idempotency and Recovery Mode required

## Trust Centre Approval

* **Name or role:** Trust Centre Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Network, consent, decision and completion records required

## Test Approval

* **Name or role:** Test Architecture Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Security, package, migration and policy acceptance suite required

---

# 228. Supersession

This ADR is superseded only when a later ADR:

* names ADR-0015 explicitly;
* explains why discovery, consent, installation authority or update policy changed;
* identifies affected package families and channels;
* describes migration of App Installer associations and local state;
* explains network, security, recovery and user-control impact;
* and updates the `Superseded by` field.

Historical update policies remain in version control.

---

# 229. Change History

| Version | Date         | Author        | Summary                                                                                      |
| ------- | ------------ | ------------- | -------------------------------------------------------------------------------------------- |
| 0.1     | 18 July 2026 | Founder Draft | Initial manual-by-default update discovery and Windows App Installer hand-off recommendation |

---

# 230. Final Decision Statement

> **Opure will provisionally use application-controlled update discovery through its Network Gateway, with Manual Only as the default, an explicit opt-in launch check, no Windows automatic launch or background update settings, no forced activation block, no downgrade permission and no restricted package-management capability, while handing the exact approved `.appinstaller` file to Windows App Installer only after the developer reviews the release and Opure safely checkpoints and stops its work, because update discovery is network activity, installation is a high-authority state change, and package replacement, data migration and recovery must remain visible, separately authorised and honest about what each layer can verify.**

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**