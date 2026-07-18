# ADR-0002 — Desktop Framework

## Architecture Decision Record

**Status:** Proposed
**Date:** 18 July 2026
**Decision owners:** Founder and Product Owner
**Technical owners:** Desktop Architecture Owner
**Reviewers:** Runtime Architecture Owner, Accessibility Owner, Security Owner, Performance Owner
**Supersedes:** None
**Superseded by:** None
**Related ADRs:** ADR-0001 Primary Implementation Language, ADR-0003 Runtime Process Topology, ADR-0004 Local IPC
**Related specifications:** CHARTER-001, SPEC-001, SPEC-003, SPEC-008, SPEC-009, SPEC-010, SPEC-012
**Target milestone:** Phase 0 — Founding Baseline and Phase 1 — Architecture Skeleton

---

## 1. Decision Summary

Opure should adopt **Avalonia UI** as the primary desktop user-interface framework, using C#, AXAML and a strict presentation architecture built around the Desktop Gateway.

The decision is conditional.

Avalonia must pass a bounded desktop-framework prototype covering:

* Windows 11 application startup;
* keyboard and screen-reader accessibility;
* high-DPI and multi-monitor behaviour;
* large project-tree rendering;
* large diff rendering;
* virtualised logs and timelines;
* native window and clipboard integration;
* secure Desktop Gateway communication;
* installer and update packaging;
* and an acceptable open-source or commercially approved control strategy.

If Avalonia fails the prototype evidence gate, Opure should use **WinUI 3** for the Windows-first desktop while preserving the Desktop Gateway and framework-neutral view-model contracts so that a later cross-platform client remains possible.

---

## 2. Status

Current status:

> **Proposed**

This ADR becomes **Accepted** only after the framework prototype and evidence gates in this document are complete.

The framework recommendation must not be treated as final merely because Avalonia has been selected on paper.

---

## 3. Context

Opure requires a desktop application that presents:

* project navigation;
* workflow state;
* patch review;
* large diffs;
* build and test output;
* repository state;
* model and provider controls;
* project memory;
* plugin and MCP management;
* approvals;
* the Trust Centre;
* settings;
* Safe Mode;
* and Recovery Mode.

The desktop must support long engineering sessions and large volumes of structured information.

It must remain:

* responsive;
* accessible;
* keyboard efficient;
* high-DPI aware;
* secure;
* testable;
* and visually calm.

Windows 11 is the first supported platform.

The architecture should preserve a realistic route to Linux and macOS without delaying the Windows product indefinitely.

ADR-0001 recommends C# and .NET for the primary trusted platform implementation.

The desktop framework should fit that decision unless another technology offers a clearly superior product and architecture outcome.

---

## 4. Problem Statement

Opure requires a desktop framework that can deliver a professional Windows 11 application in C# while supporting strict separation from domain services, strong accessibility, efficient rendering of engineering data, reliable packaging and a credible future route to Linux and macOS.

---

## 5. Decision Drivers

The framework is evaluated against:

* alignment with the Opure Charter;
* Windows 11 quality;
* future Linux support;
* future macOS support;
* C# and .NET alignment;
* desktop maturity;
* accessibility;
* keyboard navigation;
* screen-reader integration;
* high-DPI support;
* multi-monitor support;
* custom-control support;
* data binding;
* virtualisation;
* large-tree performance;
* large-diff performance;
* log-streaming performance;
* styling and theming;
* light, dark and system appearance;
* native window integration;
* file-dialog integration;
* clipboard integration;
* drag-and-drop;
* notification integration;
* automated UI testing;
* visual-regression testing;
* packaging;
* installer support;
* update support;
* licensing;
* supply-chain risk;
* ecosystem maturity;
* small-team productivity;
* contributor accessibility;
* maintainability;
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
* **Open by Architecture**
* **Loose Coupling**
* **Least Privilege**
* **Project Isolation**
* **Accessibility**
* **Performance Respect**
* **Honest State**

Relevant architectural requirements include:

* The desktop is a command and projection surface.
* The desktop must not own authoritative domain state.
* The desktop communicates primarily through the Desktop Gateway.
* Optional-service failure must not block the whole interface.
* Long-running work must not block the UI thread.
* Planned, proposed, applied and validated states remain distinct.
* All primary actions must be keyboard accessible.
* Colour must not be the only state signal.
* Reduced-motion settings must be respected.
* Large lists, logs and diffs require virtualisation or incremental rendering.
* Safe Mode and Recovery Mode are first-class experiences.
* Windows 11 is the first target, not a permanent architectural lock.

---

## 7. Scope

This ADR decides the primary framework for:

* the Opure desktop shell;
* project and navigation views;
* workflow views;
* patch and diff review;
* approval views;
* Trust Centre views;
* build and test views;
* model and provider views;
* memory views;
* plugin and MCP views;
* settings;
* notifications within the application;
* Safe Mode;
* Recovery Mode;
* and desktop-specific accessibility.

This ADR does not decide:

* the Runtime implementation language;
* the Desktop Gateway transport;
* the final dependency-injection approach;
* the complete design system;
* the code-editor component;
* the diff algorithm;
* the charting component;
* the updater technology;
* the installer technology;
* the public plugin-view format;
* mobile clients;
* web clients;
* or whether Linux and macOS ship in version 1.0.

---

## 8. Constraints

Known constraints include:

* Windows 11 is the first supported platform.
* C#/.NET is the proposed primary implementation stack.
* The first implementation must be practical for a small team.
* The desktop must not access service databases directly.
* The desktop must remain usable while models load or builds run.
* The desktop must reconnect to the Runtime safely.
* The desktop must support large project trees and engineering output.
* The desktop must not store secrets merely to restore UI state.
* Third-party plugin views must not escape their permission boundary.
* The application must support local-only operation.
* The framework must not require a cloud account.
* Accessibility cannot be deferred until after version 1.0.
* A framework-specific UI must not leak into domain contracts.
* The source tree should avoid unnecessary second-language complexity.

---

## 9. Assumptions

This decision assumes:

* Opure initially ships as one primary desktop window.
* The desktop communicates with an independently testable Runtime.
* The main desktop logic can be represented through framework-light view models and projections.
* Large code editing is not required in the first implementation.
* File preview and diff review are required.
* A full IDE replacement is not required.
* Platform-native integrations can be placed behind desktop platform adapters.
* Linux and macOS are future targets rather than initial release requirements.
* The initial design language can be Opure-specific rather than a perfect clone of Windows Fluent.
* Automated accessibility testing will be supplemented by manual assistive-technology testing.
* Commercial controls may be considered only through an explicit founder-approved licence decision.
* The project should prefer framework controls that remain available under acceptable open-source terms.

---

## 10. Current Framework Evidence

The framework assessment reviewed official documentation available on 18 July 2026.

### Avalonia

Official Avalonia documentation describes support for Windows, macOS and Linux in addition to mobile and WebAssembly targets.

Its accessibility documentation describes automation peers and platform accessibility integration, including Windows UI Automation, macOS accessibility APIs and Linux AT-SPI.

Its architecture uses a retained-mode rendering system and platform backends.

### WinUI 3

Official Microsoft documentation describes WinUI 3 as the modern native Windows desktop UI framework delivered through the Windows App SDK.

WinUI 3 is focused on Windows.

### WPF

Official Microsoft documentation describes WPF as a mature .NET desktop framework with XAML, controls, data binding and vector rendering.

WPF runs only on Windows.

### .NET MAUI

Official Microsoft documentation lists Windows, macOS through Mac Catalyst, Android and iOS as supported targets.

Desktop Linux is not listed as a supported target.

### Uno Platform

Official Uno Platform documentation describes a C# and XAML framework based on WinUI-compatible APIs, with targets including Windows, Linux, macOS, mobile and WebAssembly.

Uno carries a broad compatibility surface and must account for APIs that may not be implemented on every target.

### Web Shells

Tauri documentation describes a desktop architecture combining Rust tooling with HTML rendered in an operating-system WebView.

Electron provides a Chromium and Node-based desktop model for Windows, macOS and Linux.

Both approaches introduce a web presentation stack in addition to the C# Runtime.

---

## 11. Options Considered

The principal options are:

1. **Option A — Avalonia UI**
2. **Option B — WinUI 3**
3. **Option C — WPF**
4. **Option D — Uno Platform**
5. **Option E — .NET MAUI**
6. **Option F — Web Desktop Shell**
7. **Option G — Separate Native Client Per Platform**

---

# 12. Option A — Avalonia UI

## 12.1 Description

Use Avalonia UI as the desktop framework.

Implement the desktop in:

* C#;
* AXAML;
* framework-light view models;
* reusable presentation models;
* and platform adapters.

The Windows application would be the first shipping target.

Linux and macOS clients would be enabled only after the core desktop and Runtime contracts are stable.

---

## 12.2 Advantages

* Strong alignment with C#/.NET.
* One primary language for Runtime and desktop.
* Supports Windows, Linux and macOS.
* XAML-style declarative UI.
* Retained-mode rendering.
* Mature styling and templating model.
* Supports custom controls.
* Supports MVVM.
* Supports platform-native windowing backends.
* Supports keyboard navigation.
* Provides accessibility automation concepts.
* Allows an Opure-specific design system.
* Can share desktop UI code across future operating systems.
* Avoids a required browser or JavaScript runtime.
* Keeps desktop memory use independent from a bundled Chromium engine.
* Supports direct integration with .NET contracts and generated clients.
* Allows source-level architecture tests in one ecosystem.
* Reduces language fragmentation.
* Can preserve platform-specific adapters for Windows integration.
* Fits a service-backed desktop architecture.
* Provides a realistic route to future Linux and macOS support.

---

## 12.3 Disadvantages

* Windows appearance is not automatically identical to native WinUI.
* Some controls may require additional packages.
* Complex engineering controls may need custom implementation.
* Rich tree-grid or advanced data-grid choices may have licensing implications.
* Cross-platform behaviour still requires platform-specific testing.
* Accessibility quality must be validated with real assistive technologies.
* Some Windows platform features require native interop or Windows App SDK calls.
* Packaging and updating require separate design work.
* Framework version upgrades may involve breaking changes.
* Tooling may be less integrated than Microsoft's Windows-only frameworks.
* Third-party control availability may be narrower than WPF or web ecosystems.
* Cross-platform promise could encourage premature multi-platform scope.

---

## 12.4 Risks

* Large project trees may not perform adequately without custom virtualisation.
* Large diffs may require a specialised text-rendering control.
* Screen-reader behaviour may differ across platforms.
* A required advanced control may be commercially licensed.
* The team may attempt to solve Linux and macOS too early.
* Styling may become a custom-framework project of its own.
* Platform-specific bugs may increase test burden.
* A future major framework release may change control or package strategy.
* Built-in data controls may not meet Opure's engineering-data requirements.
* Native Windows integrations may be less direct than WinUI 3.
* Plugin-provided UI may be difficult to sandbox inside the same visual tree.

---

## 12.5 Mitigations

* Ship Windows first.
* Use the Desktop Gateway to keep the Runtime independent.
* Build a bounded framework prototype.
* Prototype large tree and diff views before acceptance.
* Avoid dependence on one advanced commercial control unless approved.
* Keep platform integrations behind `Opure.Desktop.Platform.*` adapters.
* Create an Opure design system with controlled components.
* Enforce UI-thread and allocation budgets.
* Test with Narrator and keyboard-only navigation.
* Use visual-regression tests.
* Use automation identifiers from the beginning.
* Keep plugin UI isolated through constrained contribution models.
* Defer full embedded editor capability.
* Maintain a WinUI 3 fallback plan.

---

## 12.6 Evidence Required

* Windows shell prototype.
* navigation and command palette;
* project tree with at least 100,000 logical items using lazy loading;
* diff view for a large multi-file patch;
* continuously streaming build log;
* large Trust Centre timeline;
* keyboard-only workflow;
* Narrator test;
* 100%, 150%, 200% and mixed-DPI testing;
* multi-monitor restore testing;
* theme switching;
* reduced-motion testing;
* Runtime disconnect and reconnect;
* crash recovery;
* installer prototype;
* cold and warm startup measurements;
* idle CPU and memory measurements;
* package and licence inventory.

---

## 12.7 Estimated Adoption Cost

* **Initial implementation:** Moderate
* **Operational complexity:** Moderate
* **Migration difficulty:** Moderate
* **Replacement difficulty:** Moderate

---

# 13. Option B — WinUI 3

## 13.1 Description

Use WinUI 3 and the Windows App SDK for the Windows desktop.

The Runtime and Desktop Gateway remain framework independent.

A later Linux or macOS client would require another desktop framework or separate implementation.

---

## 13.2 Advantages

* Microsoft's modern Windows desktop UI framework.
* Strong Windows 11 visual integration.
* C# and XAML support.
* Direct access to modern Windows APIs.
* Native Windows input and windowing model.
* Strong Fluent-design alignment.
* Good Windows accessibility integration.
* Strong high-DPI support.
* Strong Microsoft documentation and samples.
* Windows App SDK packaging options.
* Good fit for a Windows-only first release.
* Fewer visual differences from other modern Windows applications.
* Direct platform feature access.
* No third-party cross-platform rendering layer.

---

## 13.3 Disadvantages

* Windows only.
* A future Linux or macOS client requires another UI implementation.
* Cross-platform UI reuse is limited.
* Some Windows App SDK deployment scenarios add complexity.
* The application becomes more dependent on Microsoft desktop APIs.
* UI tests are Windows-only.
* A future client may diverge in behaviour.
* Framework-specific types may leak into view models without discipline.
* Native platform integration does not remove the need for service boundaries.
* A later framework replacement may be expensive if presentation logic is not isolated.

---

## 13.4 Risks

* Windows-first could become permanent Windows lock-in.
* Cross-platform work could require a partial UI rewrite.
* Developers may couple domain logic to WinUI types.
* Packaging and Windows App SDK versioning may create deployment issues.
* The team may overuse platform APIs in shared presentation logic.
* Future desktop clients may produce inconsistent experiences.

---

## 13.5 Mitigations

* Keep all authoritative state in Runtime services.
* Use framework-light view models.
* Define generated Desktop Gateway clients.
* Keep navigation and presentation contracts framework neutral.
* Isolate Windows APIs.
* Maintain UI behaviour specifications.
* Treat WinUI as a client, not the platform.

---

## 13.6 Evidence Required

* Windows shell prototype.
* packaging and installer proof.
* large tree and diff performance.
* accessibility test.
* Runtime reconnect.
* framework-neutral view-model proof.
* future-client replacement analysis.

---

## 13.7 Estimated Adoption Cost

* **Initial implementation:** Low to Moderate
* **Operational complexity:** Low to Moderate
* **Migration difficulty:** High for cross-platform UI
* **Replacement difficulty:** Moderate to High

---

# 14. Option C — WPF

## 14.1 Description

Use WPF on modern .NET for the Windows desktop.

---

## 14.2 Advantages

* Mature framework.
* Strong C# and XAML alignment.
* Mature data binding.
* Large control ecosystem.
* Mature MVVM patterns.
* Strong tooling.
* Good Windows accessibility support.
* Proven for information-dense desktop applications.
* Good custom-control and document support.
* Good compatibility with established .NET libraries.
* Stable engineering knowledge base.
* Direct Windows desktop model.
* Suitable for large enterprise applications.

---

## 14.3 Disadvantages

* Windows only.
* Visual model is older than WinUI 3.
* Modern Windows styling requires custom work or additional libraries.
* Cross-platform UI would require replacement.
* Some framework patterns encourage heavy code-behind or tightly coupled view models.
* Advanced rendering may require custom controls.
* High-performance text and diff views still require careful design.
* A long-term product may inherit legacy assumptions.
* Platform expansion would not reuse the UI layer.

---

## 14.4 Risks

* The application may appear dated without substantial design work.
* Windows-only implementation may become entrenched.
* Third-party theme and control dependencies may grow.
* Framework convenience may encourage UI ownership of domain state.
* Future migration cost may be high.

---

## 14.5 Estimated Adoption Cost

* **Initial implementation:** Low
* **Operational complexity:** Low
* **Migration difficulty:** High
* **Replacement difficulty:** High

---

# 15. Option D — Uno Platform

## 15.1 Description

Use Uno Platform with C# and WinUI-compatible XAML.

Target Windows first and retain paths to Linux, macOS, mobile and WebAssembly.

---

## 15.2 Advantages

* C# and XAML.
* Broad platform target set.
* WinUI-compatible API model.
* On Windows, can align closely with WinUI.
* Supports desktop Linux and macOS targets.
* Supports future web and mobile targets.
* Provides platform-specific integration.
* Strong route for a Windows-first design that expands later.
* Allows reuse of presentation logic across targets.
* Uses familiar Microsoft-style APIs.

---

## 15.3 Disadvantages

* Broad platform abstraction introduces complexity.
* WinUI compatibility surface is large.
* Not every API behaves identically across targets.
* The project may carry mobile and web concerns that Opure does not initially need.
* Cross-platform target structure can complicate builds.
* More framework concepts than a desktop-only need requires.
* Debugging platform differences may be complex.
* Dependency and package graph may be larger.
* The team must understand both WinUI concepts and Uno-specific behaviour.
* Desktop Linux and macOS need separate validation.
* The broad target promise may encourage scope expansion.

---

## 15.4 Risks

* The desktop architecture may become shaped by future mobile and web targets.
* Unsupported or partially implemented APIs may appear late.
* Windows and non-Windows rendering may diverge.
* Build and packaging complexity may exceed the small-team budget.
* The platform may become more framework-dependent than intended.

---

## 15.5 Estimated Adoption Cost

* **Initial implementation:** Moderate to High
* **Operational complexity:** High
* **Migration difficulty:** Moderate
* **Replacement difficulty:** Moderate to High

---

# 16. Option E — .NET MAUI

## 16.1 Description

Use .NET MAUI for a shared C# application targeting Windows, macOS and mobile.

---

## 16.2 Advantages

* Microsoft-supported .NET application model.
* C# and XAML.
* Windows and macOS support.
* Mobile expansion possible.
* Shared project model.
* Familiar .NET tooling.
* Uses WinUI 3 on Windows.
* Strong integration with .NET application services.
* Suitable for forms and conventional application navigation.

---

## 16.3 Disadvantages

* Desktop Linux is not an official target.
* Mobile-first abstractions may not fit an information-dense engineering application.
* Advanced desktop windowing may require platform-specific work.
* Desktop control depth may be insufficient for complex engineering views.
* Multi-window and advanced desktop interaction can require additional complexity.
* macOS uses Mac Catalyst rather than a pure desktop AppKit model.
* Large tree, diff and log views require custom work.
* Opure does not initially need mobile targets.
* Framework choices could be driven by irrelevant mobile constraints.

---

## 16.4 Risks

* The desktop experience may be compromised by cross-device abstractions.
* The project may inherit mobile workload and tooling complexity.
* Linux remains unresolved.
* Advanced desktop controls may depend on third-party commercial libraries.
* Windows-specific behaviour may still require direct platform code.

---

## 16.5 Estimated Adoption Cost

* **Initial implementation:** Moderate
* **Operational complexity:** Moderate to High
* **Migration difficulty:** Moderate to High
* **Replacement difficulty:** Moderate

---

# 17. Option F — Web Desktop Shell

## 17.1 Description

Use a web presentation layer with:

* HTML;
* CSS;
* TypeScript;
* a frontend framework;
* and either Tauri or Electron as the desktop shell.

The C# Runtime remains a separate process behind the Desktop Gateway.

---

## 17.2 Advantages

* Large UI ecosystem.
* Strong styling flexibility.
* Strong virtualised-list ecosystem.
* Mature code-editor and diff components.
* Easy creation of complex layouts.
* Broad contributor familiarity.
* Cross-platform desktop support.
* Rapid UI iteration.
* Strong automated browser-style testing.
* Potential reuse for a future web client.
* Tauri can use the operating system WebView rather than bundling Chromium.
* Electron provides highly consistent rendering across platforms.

---

## 17.3 Disadvantages

* Introduces TypeScript and web build tooling.
* Adds a second major application ecosystem.
* Requires a web security model.
* Requires a WebView or bundled browser engine.
* Native accessibility requires careful validation.
* Native desktop behaviour may feel less integrated.
* Tauri introduces Rust tooling.
* Electron introduces Chromium and Node runtime cost.
* Package-supply-chain exposure can be substantial.
* UI-to-Runtime IPC must cross more layers.
* Browser security concerns become product concerns.
* Plugin-view isolation becomes complex.
* Memory use may be higher, especially with Electron.
* Frontend dependencies can grow rapidly.
* A future embedded editor may over-influence the entire framework decision.

---

## 17.4 Risks

* The web layer may become the true application platform rather than a replaceable client.
* Domain logic may drift into TypeScript.
* Browser and Node dependencies may broaden the attack surface.
* WebView version differences may affect rendering.
* The team may optimise for visual speed over desktop reliability.
* Framework churn may increase maintenance.
* Accessibility may be treated as browser compliance rather than desktop usability.
* Tauri adds Rust despite C# being the primary language.
* Electron resource use may conflict with local-model workloads.

---

## 17.5 Mitigations

* Keep all domain state in the Runtime.
* Treat web code as presentation only.
* Use generated Gateway clients.
* Apply a strict content-security policy.
* Disable unnecessary Node integration.
* Minimise frontend dependencies.
* Test with assistive technologies.
* Set memory budgets.
* Keep the option as a fallback for a specialised editor surface rather than the whole desktop.

---

## 17.6 Estimated Adoption Cost

* **Initial implementation:** Moderate
* **Operational complexity:** High
* **Migration difficulty:** Moderate
* **Replacement difficulty:** Moderate

---

# 18. Option G — Separate Native Client Per Platform

## 18.1 Description

Build a Windows client with WinUI 3, a macOS client with AppKit or SwiftUI and a Linux client with a separate native toolkit.

All clients use the Desktop Gateway.

---

## 18.2 Advantages

* Best platform-specific experience.
* Direct native accessibility.
* Direct platform integration.
* No cross-platform abstraction limitations.
* Each client can follow platform conventions.
* Runtime remains portable.
* Clients can release independently.

---

## 18.3 Disadvantages

* Multiple languages.
* Multiple codebases.
* Multiple test suites.
* Duplicate presentation logic.
* High design-consistency cost.
* High maintenance cost.
* High contributor cost.
* Slow feature parity.
* Disproportionate burden for a small team.
* Complex release coordination.

---

## 18.4 Risks

* Non-Windows clients may remain permanently incomplete.
* Security and approval presentation may diverge.
* Accessibility fixes may need repeated implementation.
* Product behaviour may become inconsistent.
* Most development effort may go into client parity.

---

## 18.5 Estimated Adoption Cost

* **Initial implementation:** Very High
* **Operational complexity:** Very High
* **Migration difficulty:** High
* **Replacement difficulty:** High

---

# 19. Comparison Matrix

Scores:

* **1** — poor
* **2** — weak
* **3** — acceptable
* **4** — strong
* **5** — excellent

Scores are architectural judgements that require prototype validation.

| Criterion                     | Weight | Avalonia | WinUI 3 |     WPF |     Uno |    MAUI | Web Shell | Separate Native |
| ----------------------------- | -----: | -------: | ------: | ------: | ------: | ------: | --------: | --------------: |
| Charter alignment             |      5 |        5 |       4 |       4 |       4 |       3 |         3 |               4 |
| Windows 11 quality            |      5 |        4 |       5 |       4 |       5 |       4 |         4 |               5 |
| Linux route                   |      4 |        5 |       1 |       1 |       5 |       1 |         5 |               5 |
| macOS route                   |      4 |        5 |       1 |       1 |       5 |       4 |         5 |               5 |
| C# alignment                  |      5 |        5 |       5 |       5 |       5 |       5 |         2 |               2 |
| Small-team suitability        |      5 |        4 |       4 |       5 |       3 |       3 |         3 |               1 |
| Accessibility potential       |      5 |        4 |       5 |       5 |       4 |       4 |         3 |               5 |
| Keyboard-heavy UX             |      5 |        5 |       5 |       5 |       4 |       4 |         5 |               5 |
| Custom controls               |      4 |        5 |       4 |       5 |       4 |       3 |         5 |               5 |
| Large data views              |      5 |        4 |       4 |       4 |       3 |       3 |         5 |               5 |
| Native integration            |      4 |        4 |       5 |       5 |       4 |       4 |         3 |               5 |
| Cross-platform reuse          |      4 |        5 |       1 |       1 |       5 |       4 |         5 |               1 |
| Packaging simplicity          |      4 |        4 |       4 |       4 |       3 |       3 |         3 |               2 |
| Supply-chain control          |      4 |        4 |       4 |       4 |       3 |       3 |         2 |               3 |
| Framework maturity            |      4 |        4 |       4 |       5 |       4 |       4 |         5 |               5 |
| UI testing                    |      4 |        4 |       4 |       4 |       4 |       4 |         5 |               4 |
| Styling flexibility           |      3 |        5 |       4 |       4 |       4 |       4 |         5 |               5 |
| Runtime memory fit            |      4 |        4 |       4 |       4 |       4 |       4 |    2 to 4 |               4 |
| Replacement cost              |      4 |        4 |       3 |       2 |       3 |       3 |         3 |               2 |
| **Indicative weighted total** |        |  **419** | **344** | **342** | **391** | **337** |   **365** |         **350** |

Avalonia provides the best overall fit for:

* C# alignment;
* Windows-first delivery;
* cross-platform desktop reuse;
* custom engineering UI;
* and a small-team architecture.

WinUI 3 provides the strongest documented fallback if Windows-native quality or Avalonia prototype results are unacceptable.

---

# 20. Decision

Opure will adopt **Avalonia UI** as its proposed primary desktop framework.

The implementation will use:

* C#;
* AXAML;
* compiled bindings where practical;
* framework-light view models;
* explicit navigation;
* Desktop Gateway clients;
* an Opure design system;
* platform adapters;
* and automated accessibility identifiers.

The initial shipping target is Windows 11.

Linux and macOS packaging are deferred.

This decision does not:

* place domain state inside the UI;
* permit direct service-database access;
* permit direct protected file writes;
* permit direct provider calls;
* permit direct secret access;
* require third-party plugin UI to run in-process;
* require a full embedded code editor;
* or guarantee Linux and macOS release dates.

This decision is:

* [ ] Permanent until superseded
* [x] Provisional pending prototype evidence
* [ ] Experimental only
* [ ] Limited to one view
* [ ] Limited to one milestone

---

# 21. Rationale

Avalonia is preferred because it offers the strongest balance of:

* C# and .NET alignment;
* Windows desktop capability;
* future Linux and macOS support;
* custom-control support;
* MVVM compatibility;
* styling flexibility;
* retained-mode rendering;
* and one primary desktop codebase.

WinUI 3 would provide stronger Windows-native alignment, but it would make the primary desktop Windows-only.

WPF is mature and productive, but it would create the same platform limitation with an older presentation model.

Uno Platform offers broad reach, but its large WinUI-compatible abstraction and multi-target scope create more complexity than Opure presently needs.

.NET MAUI includes mobile-oriented constraints and does not provide an official Linux desktop target.

A web shell provides strong component availability but adds a substantial second technology stack and may compete with local AI workloads for memory.

Avalonia is not accepted unconditionally.

Opure's UI is unusually demanding because it requires:

* large trees;
* large diffs;
* long streaming logs;
* accessible technical data;
* exact keyboard workflows;
* and recovery interfaces.

The prototype must prove these requirements rather than relying on framework marketing or simple samples.

---

# 22. Consequences

## 22.1 Positive Consequences

* Runtime and desktop can use C#.
* Domain contracts and generated clients remain easy to share.
* Windows, Linux and macOS can use one desktop presentation codebase.
* The desktop does not require a bundled browser engine.
* The UI can use an Opure-specific visual system.
* Custom engineering controls are possible.
* View models can remain independently testable.
* Platform-specific integrations can be isolated.
* Cross-platform intent is preserved without requiring immediate non-Windows releases.
* The project avoids an unnecessary TypeScript frontend.
* Architecture tests can cover desktop dependency rules.
* The desktop can remain a separate client of the Runtime.

---

## 22.2 Negative Consequences

* Avalonia becomes an important third-party dependency.
* Windows styling requires deliberate design work.
* Advanced controls may require custom development.
* Accessibility must be proven manually.
* Platform-specific bugs require additional testing.
* Some Windows features require platform adapters.
* Framework upgrades may require migration.
* A specialised diff renderer may be needed.
* A rich tree-grid may require a custom or licensed control.
* Packaging and updating remain separate work.
* The team must learn Avalonia-specific control and styling behaviour.

---

## 22.3 Neutral Consequences

* The Runtime process model remains undecided.
* Local IPC remains undecided.
* The design system remains undecided.
* The updater remains undecided.
* The code-editor component remains undecided.
* Plugin UI sandboxing remains undecided.
* Mobile support remains out of scope.
* The Desktop Gateway remains authoritative for desktop communication.

---

## 22.4 New Responsibilities

This decision creates responsibility for:

* Avalonia version policy;
* package and licence review;
* accessibility test matrix;
* platform adapter design;
* UI performance budgets;
* design-system maintenance;
* custom-control ownership;
* visual-regression testing;
* Windows installer validation;
* and fallback criteria.

---

# 23. Desktop Architecture

The desktop should use the following dependency direction:

```text
Views and Controls
        ↓
View Models
        ↓
Desktop Application Services
        ↓
Generated Desktop Gateway Client
        ↓
Desktop Gateway Contracts
```

The following dependency is prohibited:

```text
Domain Services
        ↓
Avalonia Types
```

Runtime contracts must not contain:

* Avalonia controls;
* Avalonia brushes;
* Avalonia geometry;
* window handles;
* view models;
* or AXAML-specific types.

---

# 24. Project Structure

A likely desktop structure is:

```text
src/
├── Opure.Desktop/
├── Opure.Desktop.Application/
├── Opure.Desktop.Contracts/
├── Opure.Desktop.DesignSystem/
├── Opure.Desktop.Controls/
├── Opure.Desktop.Platform.Abstractions/
├── Opure.Desktop.Platform.Windows/
├── Opure.Desktop.Testing/
└── Opure.Desktop.GatewayClient/
```

Possible future projects include:

```text
├── Opure.Desktop.Platform.Linux/
└── Opure.Desktop.Platform.MacOS/
```

Platform projects must remain thin.

---

# 25. Presentation Model

The desktop should use framework-light view models.

View models may depend on:

* presentation contracts;
* commands;
* immutable projections;
* navigation abstractions;
* notification abstractions;
* and time or scheduling abstractions.

View models should not depend directly on:

* project databases;
* Runtime internals;
* filesystems;
* AI providers;
* Git libraries;
* plugin processes;
* MCP servers;
* or secret stores.

---

# 26. View State and Domain State

## 26.1 View State

The desktop may own:

* selected tab;
* expanded tree nodes;
* panel widths;
* search text;
* sort order;
* filters;
* scroll position;
* window geometry;
* and non-sensitive presentation preferences.

---

## 26.2 Domain State

The desktop must not own:

* project identity;
* workflow state;
* patch status;
* approval validity;
* build state;
* repository truth;
* provider health;
* model state;
* plugin permissions;
* MCP permissions;
* Trust Centre history;
* or recovery authority.

---

# 27. Binding Strategy

Compiled bindings should be preferred where practical.

Binding failures must be visible during development and testing.

The project should:

* avoid dynamic binding where it weakens correctness;
* treat binding warnings as defects;
* use typed view models;
* avoid business logic in converters;
* and avoid state-changing operations in property getters.

---

# 28. Navigation

Navigation should be explicit and testable.

A navigation service may manage:

* primary routes;
* project routes;
* detail routes;
* modal routes;
* recovery routes;
* and deep links.

Navigation must not become a global service locator.

A route should identify:

* destination;
* project scope;
* resource identity;
* and optional view state.

---

# 29. Window Model

The initial implementation should use:

* one primary window;
* modal or modeless dialogs only where justified;
* a command palette;
* collapsible context panes;
* and in-place detail navigation.

Additional windows are deferred until:

* multi-monitor workflows justify them;
* state ownership is clear;
* and recovery behaviour is tested.

---

# 30. Platform Adapter Boundary

Platform-specific capabilities should use interfaces for:

* file and folder pickers;
* clipboard;
* drag and drop;
* operating-system notifications;
* taskbar integration;
* window placement;
* high-DPI information;
* theme detection;
* reduced-motion detection;
* credential prompts;
* application activation;
* URI handling;
* external editor launch;
* installer state;
* and update integration.

Windows implementations belong in `Opure.Desktop.Platform.Windows`.

---

# 31. Accessibility Requirements

The prototype and implementation must support:

* logical tab order;
* visible focus;
* keyboard invocation;
* accessible names;
* accessible descriptions;
* roles;
* states;
* selected-state reporting;
* expanded-state reporting;
* live-region announcements where appropriate;
* error announcements;
* high contrast;
* text scaling;
* reduced motion;
* and colour-independent status.

---

## 31.1 Screen Reader Tests

Windows testing must include:

* Narrator;
* keyboard-only navigation;
* navigation landmarks;
* tree state;
* list state;
* table headers;
* patch additions and deletions;
* approval risk;
* workflow progress;
* errors;
* and recovery actions.

A view is not accepted merely because controls expose default automation peers.

---

## 31.2 Custom Controls

Every custom interactive control must define:

* keyboard behaviour;
* focus behaviour;
* accessible role;
* accessible name;
* accessible state;
* high-contrast behaviour;
* and automation identifier.

---

## 31.3 Diff Accessibility

The diff view must expose:

* file name;
* old and new line numbers;
* added line;
* removed line;
* unchanged context;
* conflict;
* and selected review state

through non-colour semantics.

---

# 32. Keyboard Model

The desktop should define a stable keyboard system for:

* command palette;
* global search;
* navigation;
* project switcher;
* workflow centre;
* patch review;
* next and previous file;
* next and previous hunk;
* approve;
* deny;
* cancel;
* open Trust Centre;
* toggle context pane;
* and settings.

Destructive or high-risk actions must not execute through ambiguous single-key commands.

---

# 33. Design System

Opure should create a small internal design system containing:

* typography;
* spacing;
* elevation;
* borders;
* focus indicators;
* status icons;
* risk indicators;
* buttons;
* inputs;
* navigation;
* panels;
* cards;
* tables;
* trees;
* tabs;
* banners;
* dialogs;
* notifications;
* progress states;
* empty states;
* and error states.

The design system must prioritise technical clarity over visual novelty.

---

# 34. Theme Strategy

The desktop should support:

* System;
* Light;
* Dark.

Theme switching must not restart the Runtime.

Theme resources should use semantic tokens such as:

* surface;
* elevated surface;
* primary text;
* secondary text;
* border;
* focus;
* selection;
* addition;
* deletion;
* warning;
* error;
* success;
* and risk.

Colour must never be the only semantic channel.

---

# 35. Density

The desktop may support:

* Comfortable;
* Compact.

Density changes should preserve:

* accessible hit targets;
* readable text;
* visible focus;
* and keyboard navigation.

---

# 36. Large Project Tree

The project tree must support:

* lazy loading;
* node recycling or virtualisation;
* incremental updates;
* file status;
* filtering;
* keyboard navigation;
* accessible hierarchy;
* preserved expansion state;
* and cancellation of expensive loading.

The desktop must not load every file node eagerly.

---

# 37. Diff View

The diff view must support:

* unified view;
* side-by-side view;
* large files;
* virtualised lines;
* syntax colouring where practical;
* word-level emphasis;
* whitespace controls;
* encoding warnings;
* line-ending warnings;
* hunk selection;
* file selection;
* conflict state;
* keyboard navigation;
* and accessible text semantics.

The diff algorithm remains a service or shared-engine responsibility.

The UI renders structured diff data.

---

# 38. Streaming Logs

Build, test, model and workflow streams must:

* render incrementally;
* coalesce updates;
* avoid one UI dispatch per token or line;
* use bounded in-memory windows;
* retain service-owned full records;
* allow pause;
* allow search;
* and remain responsive.

The desktop should display that output continues even when rendering is throttled.

---

# 39. Workflow Timeline

The workflow timeline must support:

* many stages;
* nested stages;
* current activity;
* completed state;
* warnings;
* approvals;
* retries;
* cancellations;
* and recovery.

It must avoid fake percentage completion where work is not measurable.

---

# 40. Trust Centre Timeline

The Trust Centre must support:

* large histories;
* filters;
* search;
* correlation navigation;
* expandable technical detail;
* and redacted export preview.

The UI should load records incrementally.

---

# 41. Charts and Resource Views

Charts are secondary to textual state.

Resource views should remain understandable without charts.

Any charting dependency requires:

* licence review;
* accessibility review;
* performance review;
* export behaviour;
* and fallback text representation.

---

# 42. Embedded Editor

A full embedded code editor is deferred.

The initial desktop may provide:

* text preview;
* search;
* line selection;
* diff review;
* and open-in-external-editor.

A future editor component requires a separate ADR covering:

* accessibility;
* memory;
* language services;
* security;
* WebView use;
* licensing;
* and replacement cost.

---

# 43. Plugin UI Contributions

Third-party plugins must not receive unrestricted access to the main visual tree.

Initial plugin contributions should be limited to:

* labelled commands;
* structured settings;
* structured forms;
* status cards;
* read-only details;
* and declared navigation entries.

Arbitrary rich plugin views are deferred until a sandboxed UI model is designed.

---

# 44. Desktop Security

The desktop must:

* authenticate to the Desktop Gateway;
* avoid direct secret storage;
* redact clipboard actions where required;
* avoid secret content in OS notifications;
* validate deep links;
* validate plugin navigation contributions;
* avoid executing content from rendered Markdown;
* constrain external URI launching;
* and treat provider output as untrusted content.

---

# 45. Markdown and Rich Content

Rendered Markdown must:

* disable arbitrary script;
* sanitise links;
* identify external destinations;
* avoid automatic remote image loading;
* avoid embedded active content;
* and preserve selectable plain text.

Code blocks should never execute from a click without a separate command plan and approval.

---

# 46. Clipboard

Clipboard operations should:

* be deliberate;
* avoid hidden metadata;
* avoid secret copying where possible;
* show warnings for sensitive content;
* and support plain-text copy.

The desktop must not monitor the clipboard continuously without explicit user action and policy.

---

# 47. Drag and Drop

Drag and drop must validate:

* source;
* target;
* project;
* path;
* resource type;
* and intended operation.

Dropping a file must not silently copy it into a project.

It should create an explicit import or patch proposal.

---

# 48. Notifications

In-application notifications should distinguish:

* information;
* action required;
* warning;
* security;
* failure;
* and recovery.

Operating-system notifications must avoid sensitive project details by default.

---

# 49. Session Restore

The desktop may restore:

* window placement;
* active project;
* selected route;
* layout;
* filters;
* and non-sensitive view state.

It must not restore:

* stale approval;
* secret values;
* assumed workflow authority;
* or unsafe modal state.

After reconnect, authoritative state is reloaded from the Runtime.

---

# 50. Runtime Disconnect

When the Desktop Gateway disconnects, the UI must:

* remain responsive;
* mark projections stale;
* stop presenting commands as successful;
* attempt bounded reconnection;
* show current connection state;
* and reconcile authoritative state after reconnection.

The desktop must not invent final workflow or patch state.

---

# 51. Safe Mode UI

Safe Mode must be visually unmistakable.

It should expose:

* disabled plugins;
* disabled MCP servers;
* restricted providers;
* failed migrations;
* recovery actions;
* diagnostic export;
* and restart options.

Safe Mode must not depend on third-party controls.

---

# 52. Recovery Mode UI

Recovery Mode must operate with a minimal control set.

It must support:

* interrupted patch inspection;
* incomplete workflow inspection;
* repository recovery state;
* plugin quarantine;
* configuration repair;
* and safe restart.

Recovery UI dependencies should be minimised.

---

# 53. Performance Budgets

Exact figures require prototype measurements.

The desktop should define budgets for:

* cold shell readiness;
* warm shell readiness;
* idle CPU;
* idle memory;
* navigation response;
* tree expansion;
* diff scrolling;
* log streaming;
* theme switch;
* Runtime reconnect;
* and shutdown.

Performance budgets should be measured on the reference machine.

---

# 54. UI Thread Rules

The UI thread must not perform:

* file enumeration;
* hashing;
* diff computation;
* database queries;
* model calls;
* Git operations;
* build parsing;
* network calls;
* or large serialisation.

Views receive prepared projections or incremental batches.

---

# 55. Update Coalescing

High-frequency events should be coalesced.

Examples include:

* model tokens;
* build lines;
* resource metrics;
* filesystem changes;
* test-case results;
* and workflow progress.

Coalescing must not lose authoritative final state.

---

# 56. Memory Management

The desktop should:

* release closed project views;
* bound log buffers;
* avoid retaining old patch versions unnecessarily;
* avoid caching complete repository histories;
* unload large previews;
* and avoid duplicate copies of large text.

Memory profiling must be part of the prototype.

---

# 57. Packaging

The framework prototype must prove:

* self-contained Windows publishing;
* installer creation;
* per-user installation where practical;
* application identity;
* clean uninstall;
* project-data preservation;
* repair;
* upgrade;
* downgrade or rollback expectations;
* and code-signing compatibility.

Packaging technology is decided separately.

---

# 58. Updates

The update mechanism must remain independent from Avalonia where practical.

The UI may present:

* update availability;
* release notes;
* download progress;
* migration preview;
* restart;
* rollback information;
* and failure recovery.

Automatic update behaviour must respect developer policy.

---

# 59. Testing Strategy

## 59.1 Unit Tests

Unit-test:

* view models;
* commands;
* navigation;
* projection transformation;
* state formatting;
* risk presentation;
* stale-state handling;
* and recovery choices.

---

## 59.2 Component Tests

Test:

* navigation rail;
* project tree;
* workflow timeline;
* patch summary;
* diff view;
* approval detail;
* Trust Centre record;
* build output;
* settings;
* Safe Mode;
* and Recovery Mode.

---

## 59.3 Accessibility Tests

Test:

* automation names;
* roles;
* states;
* tab order;
* focus;
* keyboard invocation;
* screen-reader output;
* high contrast;
* text scaling;
* reduced motion;
* and diff semantics.

---

## 59.4 Integration Tests

Test:

* startup;
* Gateway authentication;
* project open;
* workflow updates;
* approval;
* patch apply;
* build streaming;
* Runtime restart;
* reconnect;
* and recovery.

---

## 59.5 Visual Regression

Use visual-regression tests for:

* shell;
* light theme;
* dark theme;
* compact density;
* high contrast;
* patch states;
* approval states;
* warnings;
* errors;
* and Recovery Mode.

---

## 59.6 Performance Tests

Measure:

* project tree;
* diff scrolling;
* log streaming;
* timeline updates;
* search;
* memory retention;
* startup;
* theme switching;
* and reconnect.

---

## 59.7 Platform Tests

Windows tests must cover:

* Windows 11 supported versions;
* x64;
* ARM64 where practical;
* mixed-DPI monitors;
* high contrast;
* reduced motion;
* Narrator;
* multiple monitors;
* sleep and resume;
* lock and unlock;
* and installer lifecycle.

---

# 60. Framework Prototype

The prototype should contain real Opure-shaped views rather than generic samples.

Required prototype views:

1. application shell;
2. Home;
3. project tree;
4. workflow timeline;
5. multi-file diff;
6. approval detail;
7. streaming build output;
8. Trust Centre timeline;
9. settings;
10. Safe Mode;
11. Recovery Mode.

---

## 60.1 Prototype Data Volume

Use at least:

* 100,000 logical project-tree entries;
* 2,000-file status set;
* 250-file patch summary;
* one diff with 100,000 rendered logical lines through virtualisation;
* one million-line simulated build source with a bounded visible window;
* 100,000 Trust Centre records loaded incrementally;
* 10,000 workflow events;
* and sustained model-token streaming.

These are stress scenarios, not routine expected project sizes.

---

## 60.2 Prototype Interaction Tests

Prove:

* keyboard-only navigation;
* command palette;
* route switching;
* tree search;
* next and previous patch hunk;
* approve and deny;
* cancellation;
* Runtime disconnect;
* Runtime reconnect;
* theme switching;
* scaling;
* and screen-reader navigation.

---

# 61. Fallback Gate

Avalonia should be rejected or placed Under Review if the prototype demonstrates any unresolved blocker in:

* Windows accessibility;
* large tree performance;
* large diff performance;
* text rendering correctness;
* mixed-DPI behaviour;
* installer reliability;
* application stability;
* licensing;
* or required native integration.

Minor defects do not automatically trigger rejection.

The gate should consider:

* severity;
* workaround quality;
* maintenance cost;
* user impact;
* and framework-roadmap confidence.

---

# 62. WinUI 3 Fallback

If Avalonia fails the gate:

1. preserve Desktop Gateway contracts;
2. preserve framework-light view models;
3. preserve navigation contracts;
4. preserve design tokens;
5. implement the Windows desktop in WinUI 3;
6. keep platform-specific code isolated;
7. defer Linux and macOS clients;
8. document the cross-platform UI migration path.

The fallback must not rewrite Runtime services.

---

# 63. Licensing

Before acceptance, review:

* Avalonia core licence;
* all required control-package licences;
* design-system dependencies;
* icon licences;
* font licences;
* charting licences;
* syntax-highlighting licences;
* diff-view dependencies;
* and test-tool licences.

A control must not enter the foundation because it appears free during prototype work.

Commercial dependencies require:

* cost;
* redistribution terms;
* offline build support;
* licence-key handling;
* source-availability considerations;
* and replacement plan.

---

# 64. Supply Chain

Desktop packages should use:

* central package management;
* locked restore;
* explicit versions;
* dependency inventory;
* vulnerability review;
* licence review;
* update policy;
* and reproducible-build evidence where practical.

The desktop should minimise third-party theme and control packages.

---

# 65. Privacy Impact

Avalonia does not require cloud use for Opure's desktop architecture.

The desktop must still ensure:

* no automatic remote assets;
* no web font loading;
* no remote image loading from rendered content;
* no automatic crash upload;
* no external analytics;
* and no hidden framework telemetry.

Any framework or dependency telemetry must be disabled or documented.

---

# 66. Security Impact

## 66.1 Trust Boundaries

Affected boundaries include:

* desktop to Desktop Gateway;
* desktop to operating system;
* desktop to clipboard;
* desktop to external editor;
* desktop to browser or URI handlers;
* plugin contributions;
* rendered AI content;
* and updater integration.

---

## 66.2 Threats

Relevant threats include:

* forged local Gateway;
* malicious Markdown;
* unsafe link activation;
* secret clipboard leakage;
* plugin view spoofing;
* deceptive approval UI;
* stale authoritative state;
* untrusted file preview;
* image or font parsing;
* and update-package tampering.

---

## 66.3 Mitigations

* authenticated Gateway session;
* strict content sanitisation;
* external-link confirmation where appropriate;
* clear plugin labelling;
* stable approval layouts;
* stale-state indicators;
* secret-redaction controls;
* signed update packages;
* and minimal third-party rendering dependencies.

---

# 67. Reliability and Recovery

## 67.1 Failure Modes

Potential failure modes include:

* UI-thread deadlock;
* unhandled binding exception;
* rendering failure;
* graphics-device failure;
* window-state corruption;
* inaccessible modal dialog;
* Runtime disconnect;
* corrupted saved layout;
* plugin contribution failure;
* and framework upgrade regression.

---

## 67.2 Recovery

The desktop should:

* restore default layout if saved state fails;
* start Safe Mode without third-party UI;
* restart after graphics failure where possible;
* reconnect to Runtime;
* preserve authoritative operations;
* and export diagnostics.

---

## 67.3 Crash Integrity

Desktop crash must not corrupt:

* workflows;
* patches;
* builds;
* repository state;
* approvals;
* or Trust Centre records.

Those remain Runtime-owned.

---

# 68. Observability

The desktop should expose safe diagnostics for:

* framework version;
* renderer;
* graphics backend;
* display scale;
* window state;
* Gateway state;
* event queue depth;
* UI-dispatch latency;
* rendering latency;
* memory;
* and active view.

Secret or project content must not be included by default.

---

# 69. Compatibility

## 69.1 Windows

Windows 11 is the first production target.

The prototype should test supported Windows 11 releases and current security configurations.

---

## 69.2 Linux

Linux packaging and platform integration are deferred.

Desktop contracts and platform adapters should keep Linux viable.

---

## 69.3 macOS

macOS packaging, signing and notarisation are deferred.

Desktop contracts and platform adapters should keep macOS viable.

---

## 69.4 ARM64

Windows ARM64 should be considered after the x64 prototype.

Dependencies must not assume x64 without documentation.

---

# 70. Migration and Replacement

There is no existing application UI to migrate.

Replacement cost is limited by:

* Desktop Gateway boundary;
* presentation contracts;
* framework-light view models;
* design tokens;
* platform adapters;
* and UI behaviour tests.

AXAML views and Avalonia custom controls would require replacement.

Runtime and domain services should not.

---

# 71. Implementation Plan

## 71.1 Initial Tasks

1. Record founder review of this ADR.
2. Select an Avalonia version for the prototype.
3. Create desktop solution projects.
4. Create Gateway client abstraction.
5. Create navigation abstraction.
6. Create design-token foundation.
7. Create accessible shell.
8. Create project-tree stress prototype.
9. Create diff stress prototype.
10. Create log-streaming stress prototype.
11. Create Trust Centre timeline prototype.
12. Test Narrator and keyboard navigation.
13. Test mixed DPI and multi-monitor behaviour.
14. Create Windows package prototype.
15. Measure startup, memory and rendering.
16. Review package licences.
17. Compare blockers against the WinUI 3 fallback.
18. Accept, amend or reject the ADR.

---

## 71.2 Owners

| Area                 | Owner                      |
| -------------------- | -------------------------- |
| Product decision     | Founder                    |
| Desktop architecture | Desktop Architecture Owner |
| Prototype            | Desktop Owner              |
| Accessibility        | Accessibility Owner        |
| Security             | Security Owner             |
| Performance          | Performance Owner          |
| Packaging            | Release Engineering Owner  |
| Design system        | Product Design Owner       |
| Testing              | Test Architecture Owner    |

---

# 72. Acceptance Criteria

This ADR may move to **Accepted** when:

* [ ] Avalonia shell starts successfully on Windows 11.
* [ ] Desktop Gateway authentication works.
* [ ] Runtime disconnect and reconnect work.
* [ ] View models remain framework-light.
* [ ] Domain services have no Avalonia dependency.
* [ ] Project-tree stress test remains responsive.
* [ ] Diff stress test remains responsive.
* [ ] Streaming log test remains responsive.
* [ ] Trust Centre timeline loads incrementally.
* [ ] Keyboard-only navigation covers every prototype view.
* [ ] Narrator can identify primary controls and state.
* [ ] Diff additions, deletions and conflicts are accessible without colour.
* [ ] High-contrast mode is usable.
* [ ] 100%, 150% and 200% scale tests pass.
* [ ] Mixed-DPI multi-monitor movement works.
* [ ] System, light and dark themes work.
* [ ] Reduced-motion behaviour works.
* [ ] Safe Mode starts without optional controls.
* [ ] Recovery Mode remains operable.
* [ ] Windows package installs and uninstalls safely.
* [ ] Uninstall preserves project data.
* [ ] Cold and warm startup are measured.
* [ ] Idle CPU and memory are measured.
* [ ] Required controls have acceptable licences.
* [ ] No unresolved Critical framework blocker remains.
* [ ] WinUI 3 fallback cost is documented.
* [ ] Security review is complete.
* [ ] Founder approval is recorded.

---

# 73. Evidence Required Before Acceptance

* [ ] shell prototype;
* [ ] project-tree benchmark;
* [ ] diff benchmark;
* [ ] log-streaming benchmark;
* [ ] timeline benchmark;
* [ ] keyboard test report;
* [ ] Narrator test report;
* [ ] high-contrast test;
* [ ] scaling test;
* [ ] multi-monitor test;
* [ ] package and installer test;
* [ ] dependency and licence inventory;
* [ ] memory profile;
* [ ] UI-thread latency profile;
* [ ] security review;
* [ ] WinUI 3 fallback assessment;
* [ ] founder approval.

---

# 74. Known Limitations

* The framework prototype does not yet exist.
* The exact Avalonia version is not selected.
* The design system is not defined.
* The diff-rendering component is not selected.
* The advanced tree or grid strategy is not selected.
* Plugin rich-view sandboxing is deferred.
* The embedded editor is deferred.
* Linux packaging is deferred.
* macOS packaging is deferred.
* Windows update technology is deferred.
* Installer technology is deferred.
* Commercial-control policy is not yet approved.
* Performance targets have not yet been measured.

---

# 75. Open Questions

* Which Avalonia release should be pinned?
* Are all required controls available under acceptable terms?
* Should the initial project tree use TreeView plus columns or a custom virtualised control?
* Should the diff view use a custom text renderer?
* Which MVVM helper library, if any, should be adopted?
* Which dependency-injection pattern should the desktop use?
* Which visual-regression tool should be used?
* Which accessibility automation framework should be used?
* How should plugin settings forms be described?
* Should any specialised view use a sandboxed WebView?
* Which packaging technology best fits per-user installation?
* How should the application update process integrate with Recovery Mode?
* What is the minimum supported Windows 11 release?

---

# 76. Deferred Decisions

This ADR intentionally defers:

* Runtime process topology to ADR-0003;
* local IPC to ADR-0004;
* persistence to ADR-0005;
* desktop design system to a later ADR;
* MVVM helper library to a tooling decision;
* diff-view implementation to a dedicated ADR;
* editor implementation to a dedicated ADR;
* plugin rich-view sandbox to a Plugin UI ADR;
* packaging to a packaging ADR;
* updater to an update ADR;
* Linux packaging to a future platform ADR;
* and macOS packaging to a future platform ADR.

---

# 77. Alternatives Rejected

## 77.1 WinUI 3 as the Immediate Primary Recommendation

Not selected as the first recommendation because its Windows-only scope would make future Linux and macOS desktop delivery depend on a second UI implementation.

It remains the fallback because it offers strong Windows-native alignment.

---

## 77.2 WPF

Not selected because it is Windows-only and would create a larger future UI replacement obligation.

Its maturity makes it a credible emergency fallback, but WinUI 3 is preferred for a new modern Windows application.

---

## 77.3 Uno Platform

Not selected because its broad WinUI compatibility surface and multi-target model create more complexity than the current desktop-only requirement justifies.

It remains a possible future reconsideration if Windows-native WinUI compatibility becomes more important than Avalonia's model.

---

## 77.4 .NET MAUI

Not selected because Opure needs a desktop-first engineering interface, official Linux desktop support is absent, and mobile targets do not justify their architectural cost.

---

## 77.5 Electron

Not selected because bundling Chromium and Node would add memory and supply-chain cost alongside local AI workloads.

Electron remains technically capable but is not the preferred resource profile.

---

## 77.6 Tauri

Not selected because it introduces Rust and a web frontend despite C# being the chosen primary language.

A Tauri-style shell may be reconsidered for a specialised future client, but it is not preferred for the foundational desktop.

---

## 77.7 Separate Native Clients

Not selected because the maintenance and parity cost is disproportionate for a small team.

---

# 78. Official Evidence References

The following official sources informed the comparison:

* [Avalonia supported platforms](https://docs.avaloniaui.net/docs/supported-platforms)
* [Avalonia accessibility](https://docs.avaloniaui.net/docs/app-development/accessibility)
* [Avalonia architecture](https://docs.avaloniaui.net/docs/fundamentals/architecture)
* [Avalonia MVVM pattern](https://docs.avaloniaui.net/docs/fundamentals/the-mvvm-pattern)
* [Microsoft WinUI 3 documentation](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/)
* [Microsoft Windows App SDK documentation](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/)
* [Microsoft WPF overview](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/)
* [Microsoft .NET MAUI supported platforms](https://learn.microsoft.com/en-us/dotnet/maui/supported-platforms)
* [Uno Platform introduction](https://platform.uno/docs/articles/intro.html)
* [Uno Platform supported platforms](https://platform.uno/docs/articles/getting-started/requirements.html)
* [Tauri architecture](https://v2.tauri.app/concept/architecture/)
* [Electron installation and platform documentation](https://www.electronjs.org/docs/latest/tutorial/installation)

Framework support and licensing can change.

The implementation team must re-check official documentation before moving this ADR to Accepted.

---

# 79. Review Record

| Date         | Reviewer           | Decision | Notes                                                                   |
| ------------ | ------------------ | -------- | ----------------------------------------------------------------------- |
| 18 July 2026 | Architecture draft | Proposed | Avalonia recommended subject to prototype; WinUI 3 retained as fallback |

---

# 80. Approval

## Founder or Product Approval

* **Name:** Christopher Dyer
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Pending founder review

## Architecture Approval

* **Name or role:** Desktop Architecture Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Prototype evidence required

## Accessibility Approval

* **Name or role:** Accessibility Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Narrator, keyboard, contrast and scaling evidence required

## Security Approval

* **Name or role:** Security Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Gateway, content-rendering and package review required

---

# 81. Supersession

This ADR is superseded only when a later ADR:

* names ADR-0002 explicitly;
* explains why the framework changed;
* identifies affected views and controls;
* describes migration of view models and design tokens;
* explains packaging impact;
* and updates the `Superseded by` field.

Historical ADRs remain in version control.

---

# 82. Change History

| Version | Date         | Author        | Summary                                                              |
| ------- | ------------ | ------------- | -------------------------------------------------------------------- |
| 0.1     | 18 July 2026 | Founder Draft | Initial framework comparison and conditional Avalonia recommendation |

---

# 83. Final Decision Statement

> **Opure will provisionally use Avalonia UI as its primary C# desktop framework because it provides the strongest balance of Windows-first delivery, cross-platform desktop reuse, custom engineering UI capability and alignment with the service architecture, subject to successful accessibility, performance, packaging and licensing prototypes, with WinUI 3 retained as the documented fallback.**

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**
