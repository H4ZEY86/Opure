# SPEC-010 — Desktop User Interface

## Opure Platform Desktop Experience, Review Surfaces and Developer Control

**Document:** SPEC-010  
**Status:** Founder Draft  
**Version:** 0.1  
**Language:** British English  
**Last updated:** 18 July 2026  
**Depends on:** CHARTER-001, SPEC-001, SPEC-002, SPEC-003, SPEC-004, SPEC-005, SPEC-006, SPEC-007, SPEC-008, SPEC-009  

---

## 1. Purpose

This specification defines the desktop user interface of the Opure Platform.

The desktop application is the primary surface through which a developer:

- opens and manages projects;
- inspects project state;
- interacts with workflows;
- reviews plans and generated changes;
- grants or denies approvals;
- chooses models and providers;
- observes resource use;
- manages plugins and MCP servers;
- inspects project memory;
- navigates the Trust Centre;
- resolves conflicts;
- recovers interrupted work;
- and controls local or cloud behaviour.

The desktop interface must make Opure's behaviour visible, understandable and controllable without exposing unnecessary implementation complexity.

The interface must support expert use without requiring the developer to surrender control to automation.

---

## 2. Founding Rule

> **The interface must show what Opure is doing, why it is doing it, what it will affect, and how the developer can stop or reverse it.**

The desktop application must not:

- hide high-impact work behind generic loading indicators;
- imply that AI output is authoritative;
- present a patch as applied before it is actually applied;
- conceal provider or network use;
- conceal permissions;
- conceal failures;
- conceal degraded capability;
- or create domain state that exists only inside the interface.

The desktop interface is a command and projection layer.

Authoritative state remains with the relevant platform services.

---

## 3. Relationship to Other Services

A simplified logical view is:

```text
Developer
    │
    ▼
Opure Desktop Application
    │
    ├── Shell and Navigation
    ├── Project Workspace
    ├── Workflow Centre
    ├── Patch Review
    ├── Approval Centre
    ├── Trust Centre
    ├── Model and Provider Controls
    ├── Memory Explorer
    ├── Build and Test Views
    ├── Plugin and MCP Management
    ├── Settings
    ├── Notifications
    └── Recovery Experience
            │
            ▼
       Desktop Gateway
            │
            ├── Runtime Kernel
            ├── Project Manager
            ├── Workflow Engine
            ├── Patch Service
            ├── Workspace Service
            ├── AI Router
            ├── Knowledge Engine
            ├── Build Manager
            ├── Repository Service
            ├── Policy Engine
            ├── Trust Centre
            ├── Plugin Manager
            ├── MCP Gateway
            └── Notification Service
```

The desktop interface must communicate primarily through the Desktop Gateway.

It should not become tightly coupled to every internal service.

---

## 4. Design Goals

The desktop interface must be:

- developer-first;
- transparent;
- responsive;
- keyboard-accessible;
- screen-reader accessible;
- efficient;
- calm;
- information-rich without becoming noisy;
- consistent;
- recoverable;
- and suitable for long engineering sessions.

It should support both:

- guided workflows for less experienced users;
- and direct, precise control for experienced developers.

---

## 5. Non-Goals

The desktop interface is not responsible for:

- owning project files;
- owning workflow state;
- applying patches directly;
- granting permissions outside the Policy Engine;
- storing secrets;
- invoking AI providers directly;
- executing commands directly;
- replacing the operating system's file manager;
- replacing a full code editor in the first release;
- or imitating a chat application as the entire product experience.

Chat may be one interaction surface.

It must not define the complete product.

---

## 6. Normative Language

The terms **MUST**, **MUST NOT**, **SHOULD**, **SHOULD NOT**, **MAY** and **DEFERRED** have the meanings defined in SPEC-001.

Any intentional violation of a **SHOULD** requirement must be documented in an Architecture Decision Record.

---

# 7. Experience Principles

## 7.1 Developer Respect

The interface must treat the developer as the owner of:

- project intent;
- source code;
- time;
- hardware;
- data;
- providers;
- and final decisions.

---

## 7.2 Visibility by Default

Significant actions must be visible.

Hidden automation must not be the default.

---

## 7.3 Progressive Disclosure

The interface should present:

1. a concise summary;
2. an actionable middle level;
3. full technical detail.

This prevents both oversimplification and overload.

---

## 7.4 Honest State

The interface must distinguish:

- planned;
- proposed;
- queued;
- running;
- waiting;
- approved;
- applied;
- validated;
- failed;
- cancelled;
- and recovered.

---

## 7.5 No Artificial Confidence

The interface must not use confident language when evidence is weak.

---

## 7.6 Reversibility

Where an action is reversible, the interface should make reversal discoverable.

Where it is not reversible, the interface must say so clearly.

---

## 7.7 Local-First Clarity

The interface must always make clear whether work is:

- local;
- developer-controlled remote;
- private remote;
- or public cloud.

---

## 7.8 Performance Respect

The interface must make heavy local resource use visible and controllable.

---

# 8. Application Shell

## 8.1 Main Regions

The initial desktop shell should include:

```text
┌──────────────────────────────────────────────────────────┐
│ Title Bar / Global Status / Project Switcher             │
├──────────────┬──────────────────────────────┬─────────────┤
│ Primary Nav  │ Main Work Area               │ Context Pane│
│              │                              │             │
│              │                              │             │
├──────────────┴──────────────────────────────┴─────────────┤
│ Task / Runtime / Resource Status Bar                      │
└──────────────────────────────────────────────────────────┘
```

---

## 8.2 Primary Navigation

Primary navigation should include:

- Home;
- Project;
- Workflows;
- Patches;
- Build and Test;
- Memory;
- Trust Centre;
- Plugins;
- MCP;
- Providers;
- and Settings.

The exact order may evolve through usability testing.

---

## 8.3 Context Pane

The Context Pane may show:

- selected file details;
- workflow stage detail;
- model route;
- permissions;
- validation evidence;
- patch statistics;
- and Trust Centre links.

It should be collapsible.

---

## 8.4 Status Bar

The status bar should show:

- active project;
- Runtime health;
- current performance mode;
- active model or provider;
- queued tasks;
- network state;
- and security warnings.

---

## 8.5 Global Command Palette

The desktop should provide a global command palette.

It may expose:

- project actions;
- workflows;
- settings;
- navigation;
- plugin commands;
- and recovery actions.

Commands must indicate when they come from plugins.

---

# 9. Window and Session Behaviour

## 9.1 Single Main Window

The initial release should use one primary application window.

Additional windows may be supported later.

---

## 9.2 Session Restore

The desktop should restore:

- previously open project;
- selected view;
- panel layout;
- filters;
- and non-sensitive UI preferences.

It must not automatically resume unsafe workflows without Runtime validation.

---

## 9.3 Multiple Projects

The interface may support multiple registered projects while keeping one active project context at a time initially.

---

## 9.4 Project Switching

Project switching must clearly update:

- cloud policy;
- provider restrictions;
- active workflows;
- memory scope;
- plugin permissions;
- and Trust Centre scope.

---

## 9.5 Unsaved UI State

Unsaved interface state must not be confused with unapplied project changes.

---

# 10. Home View

## 10.1 Purpose

The Home view provides a calm summary of Opure's current state.

---

## 10.2 Content

It should include:

- recent projects;
- active or interrupted workflows;
- pending approvals;
- Runtime health;
- plugin or MCP warnings;
- provider availability;
- and recent significant Trust Centre activity.

---

## 10.3 Empty State

A first-run Home view should explain:

- how to open a project;
- local-first behaviour;
- provider setup;
- and that Opure can operate without cloud services.

---

## 10.4 No Forced Onboarding

The developer should be able to skip guided onboarding.

---

# 11. Project Opening

## 11.1 Open Existing Project

The interface should support selecting a folder and registering it as a project.

---

## 11.2 Import Detection

Opure should detect:

- repository type;
- languages;
- build systems;
- package managers;
- project files;
- ignore files;
- and likely sensitive files.

---

## 11.3 Opening Summary

Before initial deep analysis, the interface should show:

- selected root;
- detected project type;
- exclusions;
- cloud policy choice;
- provider options;
- and expected indexing work.

---

## 11.4 Initial Cloud Policy

New projects should offer:

- Local Only;
- Ask Every Time;
- Approved Providers Only;
- Custom.

Ask Every Time may be the initial selected default, with Local Only presented equally and clearly.

---

## 11.5 Initial Indexing

The developer should be able to:

- begin indexing;
- defer indexing;
- use structural indexing only;
- or cancel.

---

# 12. Project View

## 12.1 Purpose

The Project view provides an engineering overview, not merely a file tree.

---

## 12.2 Project Summary

It should show:

- project identity;
- root;
- repository state;
- languages;
- build systems;
- active branch;
- cloud policy;
- indexed state;
- open issues or warnings;
- and active workflows.

---

## 12.3 File Explorer

The initial desktop may provide a project file explorer for:

- navigation;
- selection;
- context building;
- patch review;
- and metadata inspection.

It does not need to replace a full IDE editor initially.

---

## 12.4 File Status

Files should show states such as:

- modified externally;
- modified by applied patch;
- untracked;
- staged;
- conflicted;
- excluded;
- sensitive;
- generated;
- stale in memory;
- and currently involved in workflow.

---

## 12.5 File Preview

Text preview should support:

- syntax highlighting;
- line numbers;
- search;
- selectable ranges;
- encoding display;
- line-ending display;
- and source revision.

---

## 12.6 External Editor

The interface should support opening a file or project in the developer's preferred external editor.

---

# 13. Project Health

## 13.1 Health Summary

Project Health should summarise:

- workspace accessibility;
- repository state;
- indexing state;
- build profile;
- last build;
- last test;
- dependency warnings;
- policy issues;
- provider availability;
- and incomplete workflows.

---

## 13.2 Evidence

Every health warning should link to evidence.

---

## 13.3 No False Score

A single project-health score should not be used unless its meaning is defensible.

Categorised status is preferred.

---

# 14. Interaction Entry Points

Developers should be able to start work through:

- natural-language request;
- built-in workflow template;
- command palette;
- file context action;
- patch action;
- project-health action;
- Trust Centre event;
- build failure;
- and plugin contribution.

---

## 14.1 Natural-Language Requests

Natural-language input should capture:

- developer request;
- selected files or symbols;
- active project;
- chosen workflow mode;
- provider preference;
- and optional constraints.

---

## 14.2 Request Preview

Before a complex workflow starts, Opure should show the interpreted goal and likely stages.

---

# 15. Chat and Conversation Surface

## 15.1 Role

Chat may support:

- questions;
- explanations;
- planning;
- workflow initiation;
- result discussion;
- and revision requests.

---

## 15.2 Not the Whole Product

Project state, patches, approvals, workflows and Trust Centre records must have dedicated structured views.

---

## 15.3 Message Provenance

AI responses should identify:

- provider;
- model;
- local or remote classification;
- context package;
- workflow or task;
- and whether tools were used.

---

## 15.4 Code Suggestions

Code changes shown in chat must link to a patch proposal rather than pretending chat text is applied code.

---

## 15.5 Conversation Retention

The interface must show the project's conversation-retention mode.

---

# 16. Workflow Centre

## 16.1 Purpose

The Workflow Centre is the primary view for active, completed, paused and failed engineering workflows.

---

## 16.2 Workflow List

The list should support:

- active;
- waiting for approval;
- waiting for input;
- paused;
- completed;
- failed;
- cancelled;
- and recovery required.

---

## 16.3 Workflow Card

A workflow card should show:

- title;
- project;
- status;
- current stage;
- initiator;
- start time;
- model or tool currently active;
- risk;
- and cancellation control.

---

## 16.4 Workflow Detail

Workflow detail should include:

- original intent;
- interpreted goal;
- plan;
- stage graph;
- current stage;
- outputs;
- approvals;
- model requests;
- tool calls;
- patches;
- validation;
- retries;
- warnings;
- and outcome.

---

## 16.5 Stage Timeline

The interface must present a stage timeline.

---

## 16.6 Current Activity

For active stages, show:

- what is happening;
- which service is responsible;
- which model or tool is in use;
- why the stage exists;
- resource use;
- elapsed time;
- and cancel option.

---

## 16.7 Progress

Progress should be based on known stages.

Uncertain completion time must not be shown as precise.

---

## 16.8 Pause and Resume

Pause and resume controls must show any validation that will occur before continuation.

---

# 17. Workflow Plan Review

## 17.1 Plan Summary

Before high-impact execution, the interface should show:

- goals;
- assumptions;
- affected areas;
- stages;
- expected commands;
- providers;
- data sharing;
- file scope;
- validation;
- and risk.

---

## 17.2 Editable Constraints

The developer should be able to adjust:

- file scope;
- provider restrictions;
- validation level;
- workflow stages;
- and automation level.

---

## 17.3 Material Changes

If the workflow plan changes materially after approval, the interface must request renewed approval.

---

# 18. Role Presentation

## 18.1 Functional Labels

Planner, Architect, Coder, Reviewer, Tester, Documentation and Git should be presented as functional workflow roles.

---

## 18.2 No Artificial Personas

The interface should avoid implying that each role is an independent conscious entity.

---

## 18.3 Shared Model Visibility

Where multiple roles use the same model, this should be visible in technical detail.

---

## 18.4 Role Outputs

Each role output should use structured sections appropriate to the role.

---

# 19. Approval Centre

## 19.1 Purpose

The Approval Centre gives one place to inspect and decide pending protected actions.

---

## 19.2 Approval Queue

Approvals should be grouped by:

- project;
- workflow;
- risk;
- action type;
- and urgency.

---

## 19.3 Approval Detail

Approval detail must show:

- what will happen;
- why;
- who or what requested it;
- files;
- commands;
- provider;
- model;
- network destination;
- data categories;
- secret references;
- reversibility;
- risk;
- and alternatives.

---

## 19.4 Decision Controls

Controls should include:

- Approve Once;
- Approve Bounded Scope;
- Modify;
- Deny;
- Cancel Workflow;
- and View Technical Details.

---

## 19.5 No Dark Patterns

Approve must not be visually favoured over deny for high-risk actions.

---

## 19.6 Approval Expiry

Expiry and single-use state must be visible.

---

## 19.7 Changed Request

If the action changes, the interface must show exactly what changed and invalidate the earlier approval.

---

# 20. External Data-Sharing Preview

## 20.1 Trigger

A preview is required before external project-data sharing when policy requires approval.

---

## 20.2 Required Information

The view must show:

- provider or destination;
- local or remote classification;
- model where relevant;
- purpose;
- source files or memory records represented;
- approximate size;
- data classification;
- secret-scan result;
- redactions;
- known retention information;
- and approval scope.

---

## 20.3 Exact Content

The developer should be able to inspect the exact outgoing content where practical.

---

## 20.4 Unknown Provider Behaviour

Unknown retention or training behaviour must be labelled unknown.

---

# 21. Patch Centre

## 21.1 Purpose

The Patch Centre provides one place to inspect all proposed, approved, applied, conflicted and reversed patches.

---

## 21.2 Patch List

The list should show:

- title;
- source;
- workflow;
- files affected;
- status;
- risk;
- validation;
- secret findings;
- and age.

---

## 21.3 Patch Detail

Patch detail must show:

- intent;
- patch version;
- base snapshot;
- source;
- affected files;
- additions;
- deletions;
- renames;
- binary changes;
- validation;
- approvals;
- and Trust Centre links.

---

# 22. Diff Review

## 22.1 Diff Modes

The interface should support:

- side-by-side;
- unified;
- file summary;
- word-level highlighting;
- and optional syntax-aware view.

---

## 22.2 Whitespace

The developer should be able to show or hide whitespace-only changes.

---

## 22.3 Line Endings

Line-ending-only changes must be clearly identified.

---

## 22.4 Encoding

Encoding changes must be shown prominently.

---

## 22.5 Hunk Review

Text patches should support hunk-level acceptance where technically practical.

---

## 22.6 File Review

File-level acceptance and rejection should be supported.

---

## 22.7 Resulting Patch

Partial acceptance must produce an exact new patch version.

---

# 23. Patch Apply Experience

## 23.1 Preflight

Before apply, show:

- current validation state;
- conflicts;
- secret scan;
- approval;
- affected files;
- snapshot plan;
- and expected reversibility.

---

## 23.2 Apply Progress

During application, show:

- staging;
- operation count;
- current file;
- verification;
- cancellation state;
- and journal status.

---

## 23.3 Cancellation

Cancellation must explain whether the transaction can stop immediately or must reach a safe point.

---

## 23.4 Outcome

After apply, show:

- applied files;
- failed files;
- warnings;
- resulting hashes;
- post-apply validation;
- reverse option;
- and Git options.

---

# 24. Conflict Resolution

## 24.1 Conflict View

Conflict detail should show:

- base version;
- current developer version;
- proposed patch version;
- conflict type;
- and safe resolution choices.

---

## 24.2 Resolution Options

Options may include:

- keep current;
- accept proposed;
- manually merge;
- generate revised patch;
- skip file;
- cancel patch;
- and open in external editor.

---

## 24.3 AI Assistance

AI-assisted conflict resolution must produce a new reviewable patch.

---

## 24.4 No Silent Merge

The interface must never hide unresolved conflicts.

---

# 25. Reverse and Recovery UX

## 25.1 Reverse

Where reversal is available, show:

- exact files;
- current-state validation;
- conflicts;
- and expected outcome.

---

## 25.2 Partial Reverse

The interface must state when only partial reversal is possible.

---

## 25.3 Recovery Mode

After interrupted application, the recovery view must show:

- interrupted transaction;
- completed operations;
- staged files;
- current workspace state;
- conflicts;
- safe options;
- and evidence.

---

## 25.4 Recovery Actions

Actions may include:

- Complete Safely;
- Reverse Completed Operations;
- Restore Selected Files;
- Inspect Manually;
- Abandon Staged Data;
- and Export Diagnostics.

---

# 26. Build and Test View

## 26.1 Build Profiles

The interface should show detected and configured build profiles.

---

## 26.2 Command Preview

Before risky execution, show the structured command plan.

---

## 26.3 Build Output

Build output should support:

- streaming;
- severity highlighting;
- error grouping;
- file navigation;
- cancellation;
- and export.

---

## 26.4 Test Results

Test results should show:

- passed;
- failed;
- skipped;
- flaky;
- duration;
- source revision;
- and evidence.

---

## 26.5 Validation Linkage

Build and test results should link to:

- workflow;
- patch;
- source revision;
- and Trust Centre record.

---

## 26.6 No Fabrication

The UI must never display a test as run when it was only proposed.

---

# 27. Repository View

## 27.1 Repository Status

Show:

- branch;
- upstream;
- modified files;
- staged files;
- untracked files;
- conflicts;
- and last commit.

---

## 27.2 Git Separation

Patch application, staging and commit must remain distinct actions.

---

## 27.3 Commit Preparation

The interface may propose:

- staging groups;
- commit message;
- branch name;
- and validation summary.

---

## 27.4 History Rewrite

High-risk repository actions must show stronger warnings.

---

# 28. Memory Explorer

## 28.1 Purpose

The Memory Explorer lets developers inspect what Opure knows about a project.

---

## 28.2 Views

It should support:

- project summary;
- files and symbols;
- relationships;
- decisions;
- errors;
- fixes;
- builds;
- tests;
- conversations;
- workflows;
- patterns;
- and stale records.

---

## 28.3 Record Detail

A record should show:

- content;
- provenance;
- confidence;
- freshness;
- evidence;
- related records;
- and correction or deletion controls.

---

## 28.4 Conflict Visibility

Conflicting records must be visible.

---

## 28.5 Correction

The developer should be able to correct inferred or stale knowledge.

---

## 28.6 Deletion

Selective deletion controls must be available.

---

## 28.7 Reindex

The developer should be able to reindex:

- one file;
- one category;
- or the whole project.

---

# 29. Pattern Library View

## 29.1 Pattern List

Show:

- title;
- category;
- language;
- framework;
- lifecycle state;
- evidence;
- reuse count;
- known limitations;
- and compatibility.

---

## 29.2 Lifecycle

The interface must distinguish:

- Draft;
- Compiled;
- Tested;
- Reviewed;
- Proven;
- Trusted;
- Deprecated;
- Rejected.

---

## 29.3 Promotion

Promotion controls must show required missing evidence.

---

## 29.4 Reuse

Using a pattern should create a proposed workflow or patch.

It must not silently copy code.

---

## 29.5 No Bug-Free Label

The interface must never label a pattern bug-free.

---

# 30. Trust Centre Experience

## 30.1 Overview

The Trust Centre should summarise:

- recent significant activity;
- approvals;
- external data sharing;
- secret access;
- plugins;
- MCP;
- security events;
- quarantines;
- incidents;
- and audit integrity.

---

## 30.2 Timeline

A timeline should support filtering by:

- project;
- workflow;
- action;
- risk;
- provider;
- plugin;
- MCP server;
- file;
- and outcome.

---

## 30.3 Correlation Navigation

The developer should be able to navigate from one record to related:

- workflow;
- AI request;
- patch;
- command;
- build;
- test;
- network request;
- approval;
- and repository action.

---

## 30.4 Technical Detail

Technical detail should show:

- correlation identifiers;
- service;
- contract;
- policy result;
- model;
- provider;
- command;
- destination;
- and safe metadata.

---

## 30.5 Audit Integrity

Audit-integrity status should be visible.

---

## 30.6 Export

Trust exports must be previewed and redacted.

---

# 31. Security Event Experience

## 31.1 Event Presentation

Security events should show:

- severity;
- factual summary;
- affected component;
- action taken;
- evidence;
- and recommended next step.

---

## 31.2 No Alarmism

Uncertain detection must be labelled as uncertain.

---

## 31.3 Quarantine Controls

The developer should be able to inspect:

- why a component was quarantined;
- what it attempted;
- dependent capabilities;
- and restoration requirements.

---

# 32. Provider and Model Management

## 32.1 Provider List

Show:

- provider;
- local or remote class;
- endpoint;
- authentication status;
- health;
- available models;
- and policy availability.

---

## 32.2 Model List

Show:

- model;
- provider;
- capabilities;
- context limit;
- structured-output support;
- embedding support;
- installed state;
- loaded state;
- estimated resource use;
- and measured profile.

---

## 32.3 Capability Evidence

Capability claims should identify evidence where useful.

---

## 32.4 Routing Presets

The interface may expose:

- Fast Local;
- Best Local;
- Balanced;
- Private Remote;
- Lowest Cost;
- Manual Selection.

---

## 32.5 Manual Selection

Developers must be able to manually choose a provider and model where policy allows.

---

## 32.6 Routing Explanation

The interface should explain why a model was selected.

---

## 32.7 Cloud Fallback

A local failure must never visually imply that cloud fallback happened automatically.

A remote fallback proposal requires a visible decision.

---

# 33. Local Model Management

## 33.1 Installed Models

Show:

- provider-native name;
- internal identity;
- size;
- digest where known;
- quantisation;
- last use;
- loaded state;
- and storage location.

---

## 33.2 Download

Before a substantial download, show:

- model;
- source;
- licence;
- size;
- storage impact;
- and expected resource use.

---

## 33.3 Remove

Before removal, show:

- dependent workflows;
- configured defaults;
- storage reclaimed;
- and fallback effect.

---

## 33.4 Load and Unload

Manual load and unload controls may be provided.

They must respect active tasks.

---

# 34. Performance and Resource Controls

## 34.1 Performance Modes

The interface must expose:

- Eco;
- Balanced;
- Performance;
- Turbo.

Balanced should be the default.

---

## 34.2 Mode Explanation

Each mode should explain:

- CPU behaviour;
- GPU behaviour;
- model residency;
- concurrency;
- background work;
- and power impact.

---

## 34.3 Resource View

Show:

- CPU;
- memory;
- GPU;
- VRAM;
- disk;
- network;
- active models;
- task queues;
- and plugin resource use.

---

## 34.4 Heavy Task Warning

Before a heavy task, the interface should show expected resource impact where known.

---

## 34.5 Throttling

The developer should be able to pause or reduce background work.

---

# 35. Plugin Management

## 35.1 Plugin List

Show:

- name;
- publisher;
- version;
- trust class;
- signature status;
- health;
- enabled state;
- permissions;
- contributions;
- and update state.

---

## 35.2 Installation Review

Before installation, show:

- package source;
- digest;
- signature;
- licence;
- compatibility;
- requested permissions;
- network destinations;
- secret requirements;
- and storage impact.

---

## 35.3 Permission Management

Developers must be able to:

- grant;
- narrow;
- deny;
- revoke;
- and inspect plugin permissions.

---

## 35.4 Update Review

New permissions or broadened scope must be highlighted.

---

## 35.5 Quarantine

Quarantined plugins must be visibly separated from disabled plugins.

---

## 35.6 Removal

Removal should explain dependent workflows, data and settings.

---

# 36. MCP Management

## 36.1 Server List

Show:

- server identity;
- transport;
- location;
- health;
- capabilities;
- permissions;
- project access;
- and current sessions.

---

## 36.2 Connection Review

Before connection, show:

- executable or endpoint;
- network classification;
- requested secrets;
- capabilities;
- and trust status.

---

## 36.3 Capability Review

Capability discovery must not look like permission grant.

---

## 36.4 Invocation History

The developer should be able to inspect significant MCP invocations.

---

# 37. Settings Architecture

## 37.1 Settings Scopes

Settings should be grouped into:

- Application;
- User;
- Project;
- Providers;
- Performance;
- Security;
- Privacy;
- Plugins;
- MCP;
- Memory;
- Workflows;
- Build and Git;
- Appearance;
- Accessibility;
- and Advanced.

---

## 37.2 Scope Visibility

Each setting must show whether it is:

- global;
- user;
- project;
- or session scoped.

---

## 37.3 Restart Requirements

Settings requiring restart must say so before application.

---

## 37.4 Security Settings

Security-sensitive settings must not be hidden inside vague advanced menus.

---

## 37.5 Search

Settings should be searchable.

---

# 38. Appearance

## 38.1 Themes

The desktop should support:

- system;
- light;
- dark.

Additional themes may be supported later.

---

## 38.2 Accent Colour

Accent colour may be configurable.

---

## 38.3 Density

The interface may support:

- comfortable;
- compact.

---

## 38.4 Code Font

Code font should be configurable.

---

## 38.5 Scaling

The interface must support operating-system scaling and application zoom.

---

# 39. Accessibility

## 39.1 Keyboard Navigation

Every primary action must be keyboard accessible.

---

## 39.2 Focus

Visible keyboard focus must be clear.

---

## 39.3 Screen Readers

Controls must expose meaningful accessible names, roles and states.

---

## 39.4 Contrast

Text and essential controls must meet recognised contrast standards.

---

## 39.5 Colour Independence

State must not be communicated by colour alone.

---

## 39.6 Motion

Non-essential motion should be reducible.

---

## 39.7 Reduced Motion

The desktop should respect the operating-system reduced-motion preference.

---

## 39.8 Error Identification

Errors must be associated with the relevant control and announced accessibly.

---

## 39.9 Diff Accessibility

Diff views must provide textual addition, deletion and context labels.

---

# 40. Notifications

## 40.1 Notification Types

Notifications may include:

- approval required;
- workflow completed;
- workflow failed;
- patch conflicted;
- build failed;
- provider unavailable;
- plugin quarantined;
- security event;
- and recovery required.

---

## 40.2 Severity

Notifications should use clear severity and actionability.

---

## 40.3 Grouping

Repeated related notifications should be grouped.

---

## 40.4 Persistence

Important notifications must remain accessible after dismissal.

---

## 40.5 Operating-System Notifications

OS notifications should avoid sensitive project content by default.

---

# 41. Error Experience

## 41.1 Error Content

An error should show:

- what failed;
- what did not happen;
- current state;
- likely cause;
- safe next action;
- technical reference;
- and Trust Centre link.

---

## 41.2 No Generic Failure

Generic messages such as “Something went wrong” must not be the only explanation.

---

## 41.3 Retry

Retry controls must explain whether retry is safe.

---

## 41.4 Partial Success

Partial success must be described exactly.

---

# 42. Degraded Mode

## 42.1 Visibility

When a service is degraded, the interface must show:

- affected capability;
- unaffected capability;
- cause;
- and available recovery.

---

## 42.2 Examples

Examples include:

- AI unavailable but project browsing works;
- Memory rebuilding but file operations work;
- Git unavailable but patches work;
- plugin unavailable but core workflow works.

---

## 42.3 No Full-Screen Blocking

Optional service failure should not block the entire application.

---

# 43. Offline Experience

## 43.1 Offline Availability

The desktop must remain useful offline.

---

## 43.2 Offline Features

Offline operation should include:

- project browsing;
- local providers;
- patches;
- builds;
- tests;
- memory;
- Trust Centre;
- plugins that do not require network;
- and settings.

---

## 43.3 Network State

Network-dependent capabilities must show their unavailable reason.

---

# 44. First-Run Experience

## 44.1 Goals

First run should establish:

- local-first philosophy;
- project opening;
- provider setup;
- performance mode;
- and privacy choices.

---

## 44.2 Optional Provider Setup

A cloud provider must not be required.

---

## 44.3 Ollama Detection

The first release may detect Ollama and show:

- not installed;
- installed but stopped;
- available;
- incompatible;
- or unknown.

---

## 44.4 Skip

The developer must be able to skip provider setup.

---

# 45. Safe Mode Experience

## 45.1 Banner

Safe Mode must be clearly visible.

---

## 45.2 Disabled Components

The interface should list disabled or restricted components.

---

## 45.3 Recovery Actions

Safe Mode should offer:

- inspect crash;
- disable plugin;
- reset provider;
- restore configuration;
- export diagnostics;
- and restart normally.

---

# 46. Recovery Mode Experience

## 46.1 Recovery Dashboard

Recovery Mode should summarise:

- abnormal shutdown;
- incomplete workflows;
- incomplete patches;
- storage issues;
- quarantined components;
- and policy integrity.

---

## 46.2 Guided Recovery

Recovery actions should be ordered by safety.

---

## 46.3 Evidence

The developer must be able to inspect technical evidence before repair.

---

# 47. Responsive Performance

## 47.1 Main Thread

Long-running work must not block the desktop interface thread.

---

## 47.2 Streaming

Streaming output should render incrementally without excessive layout work.

---

## 47.3 Virtualisation

Large lists, logs and diffs should use virtualised rendering.

---

## 47.4 Background Refresh

Background refresh must be bounded and cancellable.

---

## 47.5 Slow Services

A slow service must not freeze unrelated views.

---

# 48. State Synchronisation

## 48.1 Gateway Projections

The desktop should receive view projections through the Desktop Gateway.

---

## 48.2 Reconnection

After Desktop Gateway reconnection, the UI must reconcile authoritative state.

---

## 48.3 Optimistic Updates

Optimistic UI updates may be used only when failure can be shown and corrected clearly.

---

## 48.4 Stale View

A stale view must be marked and refreshed.

---

# 49. Local API Security

The desktop must authenticate to the local Runtime API.

The interface must not assume localhost equals trusted access.

---

# 50. Plugin UI Security

## 50.1 Identification

Plugin-provided views and commands must be visibly identified.

---

## 50.2 Sandboxing

Rich plugin views should run in a sandboxed host.

---

## 50.3 Permissions

A UI view must not expand the plugin's granted permissions.

---

## 50.4 Navigation

Plugin views must not obscure the ability to reach core security and Trust Centre controls.

---

# 51. Data Retention UX

The desktop should show retention controls for:

- conversations;
- AI requests;
- raw provider responses;
- project memory;
- logs;
- Trust Centre records;
- snapshots;
- diagnostics;
- and plugin data.

Retention explanations must be honest about backups and provider-side data.

---

# 52. Privacy UX

The Privacy area should show:

- telemetry status;
- cloud policy;
- approved providers;
- external data-sharing history;
- conversation retention;
- raw request retention;
- and deletion tools.

External telemetry should be shown as off by default.

---

# 53. Search

## 53.1 Global Search

Global search may include:

- projects;
- files;
- symbols;
- workflows;
- patches;
- memory;
- Trust Centre;
- settings;
- plugins;
- and commands.

---

## 53.2 Scope

Search scope must be visible.

---

## 53.3 Sensitive Results

Sensitive records must respect access and redaction.

---

# 54. Deep Links

The desktop should support internal links to:

- project;
- file and line;
- workflow;
- stage;
- patch;
- diff;
- build;
- test;
- model;
- provider;
- plugin;
- MCP server;
- memory record;
- and Trust Centre event.

---

# 55. Copy and Export

## 55.1 Safe Copy

Copy actions should avoid including hidden metadata or secret values.

---

## 55.2 Export Preview

Exports should show content and redaction before creation.

---

## 55.3 Formats

Views may export:

- Markdown;
- JSON;
- patch format;
- diagnostic archive;
- and plain text.

---

# 56. Undo and History

## 56.1 UI Undo

The desktop may support undo for local UI changes.

---

## 56.2 Domain Undo

Domain reversals must use the responsible service.

Examples:

- reverse patch;
- restore setting;
- revoke approval;
- remove plugin;
- and correct memory.

---

## 56.3 History

History views must distinguish interface history from project and Trust Centre history.

---

# 57. Command Safety

## 57.1 Command Preview

Risky commands must show:

- executable;
- arguments;
- working directory;
- environment summary;
- secret references;
- risk;
- and expected effect.

---

## 57.2 Copyable Form

The developer may copy the command for manual execution.

---

## 57.3 Execution Result

The result should show:

- exit code;
- duration;
- output;
- cancellation;
- and Trust Centre link.

---

# 58. Status Language

The interface should use precise state language.

Examples:

Use:

- “Patch is ready for review.”
- “Patch was applied to 4 files.”
- “Tests were not run.”
- “Provider is unavailable.”
- “Approval expired.”

Avoid:

- “All done” when validation is incomplete.
- “Safe” without evidence.
- “Fixed” when only a patch was generated.
- “Successful” when a command returned partial failure.
- “Private” when data leaves the machine.

---

# 59. Visual Design Direction

## 59.1 Character

The visual design should feel:

- professional;
- calm;
- technical;
- modern;
- trustworthy;
- and durable.

---

## 59.2 Avoid

It should avoid:

- excessive gradients;
- distracting animation;
- gamified AI language;
- cartoon agent avatars as primary navigation;
- hidden hover-only controls;
- and visually dominant marketing content.

---

## 59.3 Information Hierarchy

High-impact state, risk and current action should be visually stronger than decorative elements.

---

# 60. Keyboard Shortcuts

The desktop should support shortcuts for:

- command palette;
- project switcher;
- workflow centre;
- patch review;
- approve or deny with confirmation;
- global search;
- open Trust Centre;
- cancel active task;
- toggle context pane;
- and open settings.

Destructive actions must not be triggered by ambiguous single-key shortcuts.

---

# 61. Layout Persistence

The desktop may persist:

- pane widths;
- selected tabs;
- collapsed sections;
- density;
- and preferred diff mode.

Sensitive content must not be stored merely to restore layout.

---

# 62. Multi-Monitor and High-DPI

The desktop should support:

- multiple monitors;
- different scaling factors;
- window restoration;
- and high-DPI rendering.

Window restoration must avoid placing the application off-screen.

---

# 63. Internationalisation

The first release may use English only.

The interface architecture should support later localisation.

Technical identifiers should remain stable and untranslated where appropriate.

---

# 64. Date, Time and Number Formatting

User-facing dates, times and numbers should follow locale settings.

Technical logs may expose ISO timestamps in detailed views.

---

# 65. Help and Documentation

## 65.1 Contextual Help

High-risk and unfamiliar controls should offer concise contextual help.

---

## 65.2 Technical Documentation

Technical detail should link to relevant specifications or local documentation where available.

---

## 65.3 Offline Help

Core help should remain available offline.

---

# 66. Feedback and Diagnostics

## 66.1 Local Feedback

The desktop may allow developers to record local notes about:

- bad routing;
- incorrect memory;
- poor patch;
- plugin issue;
- or confusing UI.

---

## 66.2 External Submission

External feedback submission must be opt-in and previewed.

---

## 66.3 Diagnostic Attachment

Diagnostic bundles must be redacted and inspected before sharing.

---

# 67. Desktop API Requirements

The Desktop Gateway should provide view-oriented contracts for:

- Home summary;
- project summary;
- workflow list and detail;
- patch list and detail;
- approval queue;
- Trust Centre timeline;
- provider and model state;
- memory records;
- build and test state;
- repository state;
- plugin state;
- MCP state;
- notifications;
- settings;
- and recovery.

---

# 68. Desktop Commands

The desktop should route commands such as:

- `OpenProject`
- `CloseProject`
- `StartWorkflow`
- `PauseWorkflow`
- `ResumeWorkflow`
- `CancelWorkflow`
- `ApproveAction`
- `DenyAction`
- `ApplyPatch`
- `ReversePatch`
- `RunBuild`
- `RunTests`
- `StageFiles`
- `CreateCommit`
- `EnablePlugin`
- `DisablePlugin`
- `ConnectMcpServer`
- `ChangePerformanceMode`
- and `EnterSafeMode`

through the Desktop Gateway.

---

# 69. Desktop Events

The desktop should subscribe to events such as:

- `RuntimeHealthChanged`
- `ProjectOpened`
- `WorkflowStateChanged`
- `StageStateChanged`
- `ApprovalRequested`
- `PatchStateChanged`
- `BuildStateChanged`
- `ProviderHealthChanged`
- `ModelRuntimeChanged`
- `PluginHealthChanged`
- `McpHealthChanged`
- `SecurityEventRaised`
- `NotificationPublished`
- and `RecoveryStateChanged`

---

# 70. Error Model

Recommended desktop-facing error categories include:

- `UI_GATEWAY_UNAVAILABLE`
- `UI_PROJECT_UNAVAILABLE`
- `UI_VIEW_STALE`
- `UI_COMMAND_REJECTED`
- `UI_APPROVAL_EXPIRED`
- `UI_PATCH_CHANGED`
- `UI_WORKFLOW_STATE_CHANGED`
- `UI_PROVIDER_UNAVAILABLE`
- `UI_PLUGIN_UNAVAILABLE`
- `UI_MCP_UNAVAILABLE`
- `UI_RECOVERY_REQUIRED`
- and `UI_INTERNAL_ERROR`

The interface should translate service errors without hiding their stable code.

---

# 71. Testing Strategy

## 71.1 Unit Tests

Unit tests should cover:

- view models;
- command validation;
- state rendering;
- accessibility labels;
- risk display;
- and stale-state handling.

---

## 71.2 Component Tests

Component tests should cover:

- workflow timeline;
- approval detail;
- patch diff;
- conflict view;
- Trust Centre timeline;
- provider selector;
- plugin permission review;
- and recovery dashboard.

---

## 71.3 Accessibility Tests

Tests must cover:

- keyboard navigation;
- focus order;
- screen-reader labels;
- contrast;
- scaling;
- reduced motion;
- and diff semantics.

---

## 71.4 Integration Tests

Integration tests should cover:

- project open;
- start workflow;
- approval;
- patch review;
- apply;
- build;
- Trust Centre navigation;
- plugin install;
- provider failure;
- and recovery.

---

## 71.5 Failure Tests

Tests should simulate:

- Desktop Gateway disconnect;
- stale workflow state;
- stale patch;
- Runtime restart;
- provider loss;
- plugin crash;
- audit unavailability;
- and file conflict.

---

## 71.6 Performance Tests

Tests should cover:

- large diff;
- large workflow timeline;
- long build output;
- thousands of Trust Centre records;
- large project tree;
- and continuous streaming.

---

## 71.7 Visual Regression

Core views should use visual-regression testing.

---

## 71.8 Usability Testing

Usability testing should verify that developers can:

- understand current state;
- distinguish local from cloud;
- review a patch;
- deny approval;
- stop a workflow;
- recover an interrupted apply;
- and inspect what data left the machine.

---

# 72. Security Testing

Tests must prove:

- plugin UI cannot elevate permission;
- stale approval cannot be accepted;
- hidden patch changes cannot appear;
- secret values are redacted;
- external-sharing preview matches outgoing data;
- localhost is not shown as automatically trusted;
- and Safe Mode restrictions are visible.

---

# 73. Performance Requirements

## 73.1 Startup

The desktop should open promptly to a usable shell while optional services continue starting.

---

## 73.2 Interaction

Common navigation and selection actions should feel immediate.

Exact targets are DEFERRED until prototype measurement.

---

## 73.3 Streaming

Streaming updates should remain smooth without excessive CPU use.

---

## 73.4 Large Data

Large logs, diffs and timelines must use incremental loading or virtualisation.

---

## 73.5 Idle

The desktop should consume minimal CPU when idle.

---

# 74. Initial Implementation Milestone

The first Desktop UI milestone is successful when the application can:

1. start on Windows 11;
2. connect securely to the Desktop Gateway;
3. display Runtime health;
4. open and switch projects;
5. show project summary and file tree;
6. start a built-in workflow;
7. show workflow stages and current activity;
8. display an approval request;
9. approve or deny a bounded action;
10. show local versus remote provider classification;
11. preview external data sharing;
12. display a generated patch;
13. review a multi-file diff;
14. show secret-scan results;
15. apply a patch through the Patch Service;
16. show apply progress and outcome;
17. run a build or test;
18. display Trust Centre records;
19. show provider and model health;
20. switch performance modes;
21. list installed plugins;
22. list configured MCP servers;
23. display notifications;
24. enter Safe Mode;
25. display Recovery Mode;
26. support keyboard navigation;
27. support light, dark and system appearance;
28. handle Desktop Gateway reconnection;
29. remain responsive during inference and build streaming;
30. and pass accessibility and security tests.

---

# 75. Acceptance Criteria

SPEC-010 is implemented when:

- [ ] The desktop communicates primarily through the Desktop Gateway.
- [ ] The UI does not own authoritative domain state.
- [ ] Significant actions show what, why, impact and control.
- [ ] Planned, running, applied and validated states remain distinct.
- [ ] AI output identifies provider and model.
- [ ] Local and remote execution are visually distinguishable.
- [ ] Cloud fallback never appears automatic.
- [ ] Workflows show stages, current activity and cancellation.
- [ ] High-impact plans are reviewable before execution.
- [ ] Agents are presented as functional roles, not authorities.
- [ ] Approval detail includes scope, risk, destination and reversibility.
- [ ] Approval and denial have balanced presentation.
- [ ] Changed actions invalidate previous approval visibly.
- [ ] External sharing previews outgoing data.
- [ ] Patch preview shows every affected file.
- [ ] Diff review supports per-file inspection.
- [ ] Partial acceptance creates a new exact patch version.
- [ ] Conflicts remain visible until resolved.
- [ ] AI conflict assistance creates a new patch.
- [ ] Patch apply progress reflects actual service state.
- [ ] Partial apply or reverse is described exactly.
- [ ] Tests are never shown as run when only proposed.
- [ ] Patch application, Git staging and commit remain separate.
- [ ] Memory records expose provenance, confidence and freshness.
- [ ] Pattern states remain evidence-based.
- [ ] Trust Centre correlation is navigable.
- [ ] Security events are factual and non-alarmist.
- [ ] Provider capabilities and health are inspectable.
- [ ] Performance modes are understandable.
- [ ] Plugin permissions are visible and revocable.
- [ ] MCP capability discovery is not shown as permission.
- [ ] Safe Mode and Recovery Mode are clearly visible.
- [ ] Optional-service failure does not block the full application.
- [ ] The desktop remains useful offline.
- [ ] Telemetry is shown as disabled by default.
- [ ] Keyboard navigation covers all primary actions.
- [ ] Screen-reader semantics exist for primary views.
- [ ] Colour is not the only state indicator.
- [ ] Reduced-motion settings are respected.
- [ ] Large lists and diffs remain responsive.
- [ ] Errors explain current state and safe next action.
- [ ] Secret values do not appear in notifications, logs or exports.
- [ ] The desktop remains responsive during long-running local work.

---

# 76. Deferred Decisions

The following are intentionally deferred:

- desktop framework;
- implementation language;
- exact visual design system;
- exact navigation order;
- embedded editor depth;
- multi-window support;
- detachable panels;
- remote desktop client;
- mobile companion;
- collaboration;
- team approval interface;
- marketplace UI;
- advanced visual workflow editor;
- voice input;
- and public theming API.

These decisions must not weaken:

- transparency;
- developer control;
- accessibility;
- honest state;
- patch review;
- approval clarity;
- or local-first visibility.

---

# 77. Required Architecture Decision Records

Implementation should produce ADRs for:

- desktop framework;
- Desktop Gateway transport;
- application state management;
- view-projection model;
- code and diff editor component;
- plugin UI sandbox;
- design system;
- accessibility baseline;
- notification strategy;
- session restoration;
- large-list virtualisation;
- and application update UX.

---

# 78. Relationship to Later Specifications

This specification provides the primary user experience for:

- **SPEC-011 — Project and Build Management**
- **SPEC-012 — Roadmap**

SPEC-011 will define deeper project, build, test, repository and dependency behaviour represented in the desktop.

SPEC-012 will define delivery phases and which interface capabilities appear in each milestone.

Later specifications may add stricter requirements.

They must not hide protected actions, data sharing, permissions or recovery state.

---

# 79. Founder Approval

This document remains a founder draft until explicitly approved.

Approval establishes the following rules:

- The desktop is a command and projection surface, not the owner of project truth.
- Significant work remains visible.
- The interface must show what Opure is doing, why, impact and control.
- Chat is useful but does not replace structured engineering views.
- Workflows, patches, approvals, memory and Trust Centre have dedicated surfaces.
- Local and cloud behaviour are always distinguishable.
- High-impact approval is informed and bounded.
- Patch review exposes exact proposed changes.
- Failure, partial success and uncertainty are described honestly.
- Safe Mode and Recovery Mode are first-class experiences.
- Accessibility is a core product requirement.
- Performance controls respect developer hardware.
- Plugins and MCP contributions are visible as external capabilities.
- The interface supports expert control without requiring hidden automation.
- Developer Respect is visible in every interaction.

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**