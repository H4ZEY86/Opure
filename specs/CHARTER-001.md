# The Opure Charter

## The Founding Principles of the Opure Platform

**Document:** CHARTER-001
**Status:** Founder Draft
**Version:** 0.1
**Language:** British English
**Last updated:** 18 July 2026

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**

---

## 1. Preamble

Software engineering is changing.

Artificial intelligence can generate code, explain systems, identify faults and automate repetitive work. However, many AI-powered products ask developers to surrender privacy, control, understanding or ownership in exchange for convenience.

Opure begins from a different belief.

Technology should strengthen developers rather than replace them. Intelligent automation should support human judgement, not silently override it. Software should remain understandable, inspectable and under the control of the people who build and maintain it.

Opure is a software engineering platform built for developers. It uses artificial intelligence, project knowledge, proven engineering patterns and controlled automation to help developers build, understand, test, maintain and evolve software without taking away their control.

This Charter establishes the founding commitments of Opure.

Technologies, frameworks, models and providers will change. Opure itself will grow and evolve. The principles in this Charter are intended to remain stable and provide the standard against which future features, architectural decisions and engineering practices are measured.

---

## 2. What Opure Is

Opure is a software engineering platform built for developers.

It combines:

* local intelligent automation;
* project knowledge and memory;
* replaceable AI providers;
* controlled engineering workflows;
* reviewable patches;
* validation and testing;
* plugins and open protocols;
* Model Context Protocol integrations;
* command-line and API adapters;
* security and policy enforcement;
* and an inspectable Trust Centre.

Opure coordinates developers, tools, project knowledge, automation and AI within one visible and controlled engineering environment.

AI is an important capability of Opure, but it is not Opure's identity.

> **Opure is a software engineering platform that uses AI — not an AI platform that happens to write code.**

---

## 3. Purpose

Opure exists to help developers engineer better software while protecting their authority, privacy, ownership and understanding.

Its purpose is not merely to generate more code.

Its purpose is to support the complete engineering process, including:

* understanding requirements;
* planning projects and systems;
* designing architecture;
* writing and reviewing code;
* testing and validating changes;
* diagnosing failures;
* maintaining documentation;
* recording engineering decisions;
* managing builds and dependencies;
* integrating engineering tools;
* and improving software safely over time.

Opure must act as an engineering partner rather than an unquestionable authority.

---

## 4. Mission

> **To build a local, developer-first software engineering platform that uses intelligent automation to help developers design, build, understand, test, maintain and evolve software while keeping them in complete control of every important decision.**

Opure must respect the developer's code, time, privacy, hardware, workflow, intelligence, decisions and ownership.

---

## 5. Vision

> **To become the most trusted local software engineering platform, where developer control, privacy, transparency and engineering quality are never sacrificed for automation.**

Opure should become a platform developers choose to use every day because it is dependable, understandable and respectful.

Trust must be earned through behaviour.

It must never be assumed simply because a system is described as intelligent.

---

## 6. The Golden Rule

> **Build software with developers, not instead of them.**

Opure may analyse, plan, recommend, generate, test, review, document and automate.

The developer remains the final authority.

Automation must empower the developer rather than reduce their understanding, weaken their ownership or remove meaningful control.

---

# 7. The Founding Principles

## 7.1 Respect the Developer

Developer Respect is the guiding philosophy of Opure.

Every feature should be judged by whether it respects:

* the developer's time;
* the developer's code;
* the developer's privacy;
* the developer's hardware;
* the developer's intelligence;
* the developer's workflow;
* the developer's decisions;
* and the developer's ownership.

Opure must not pretend to know something it does not know.

It should acknowledge uncertainty, explain relevant trade-offs and provide evidence for important recommendations whenever possible.

It should prefer a focused patch over an unnecessary rewrite, a clear explanation over false confidence and an efficient workflow over wasteful use of time or hardware.

A feature that does not respect the developer does not belong in Opure.

---

## 7.2 Developer-First

Opure is designed around the needs of developers rather than the capabilities of AI models.

Models, providers and automation systems are tools. They are not the platform's authority or identity.

Opure should adapt to the developer's preferred tools, project structure, coding conventions, workflow and chosen level of automation wherever practical.

The developer should not be forced into a single prescribed way of working merely because it is convenient for the platform.

---

## 7.3 Software Engineering First

Generating code is only one part of engineering.

Opure should prioritise:

* correctness;
* architecture;
* maintainability;
* testing;
* security;
* documentation;
* observability;
* performance;
* reproducibility;
* and long-term project health.

AI must be treated as a replaceable engineering capability rather than the centre of the platform.

Engineering outcomes matter more than the quantity of generated code.

---

## 7.4 Local by Design. Cloud Optional. Always.

Opure's core capabilities should be designed to operate locally whenever technically practical.

An internet connection must not be required for core functionality unless the purpose of that functionality inherently depends on an online service.

Cloud services may extend Opure, but they must never define it.

Local execution is the default architecture, not a secondary privacy mode.

Where a local alternative exists, the developer should be able to choose it.

A cloud provider, subscription or remote account must never become a hidden requirement for opening, understanding or maintaining a local project.

---

## 7.5 Human in Control

The developer is the final decision-maker.

Opure may:

* analyse;
* plan;
* recommend;
* generate;
* validate;
* and explain.

Important actions must remain subject to developer authority.

The intended workflow is:

1. Understand the developer's intent.
2. Explain the proposed approach.
3. Present the proposed action or change.
4. Allow review.
5. Request approval where appropriate.
6. Apply the approved action.
7. Validate and record the result.

Controlled autonomous workflows may operate within permissions deliberately granted by the developer. Those permissions must be visible, limited in scope and revocable.

Opure must not assume that autonomy removes the need for accountability.

---

## 7.6 Visible by Design

Nothing important should happen without the developer being able to see, understand and review it.

Every meaningful action should be visible, explainable, inspectable and, where technically practical, reversible.

This includes:

* file modifications;
* generated patches;
* command execution;
* tool usage;
* network requests;
* cloud communication;
* model selection;
* project context supplied to a model;
* permissions;
* approvals;
* configuration changes;
* and important automated decisions.

Opure must not rely on hidden behaviour to create the appearance of intelligence.

Visibility must be designed into the system from the beginning.

---

## 7.7 Inspectable Decisions

Developers have the right to inspect every important decision made by Opure.

Where applicable, Opure should show:

* which model or provider was used;
* the request or prompt constructed by Opure;
* the project context supplied;
* the files examined;
* the tools invoked;
* the commands executed;
* the network services contacted;
* the outputs received;
* the patches generated;
* the tests performed;
* the approvals requested;
* and the actions applied.

Some providers may not expose private internal reasoning. Opure must not pretend that unavailable information is available.

Where internal reasoning cannot be shown, Opure should state the limitation clearly and provide an honest explanation or reasoning summary based on the information available to the platform.

---

## 7.8 Reversible Wherever Technically Practical

Every significant action performed by Opure should be reversible wherever technically practical.

Examples include:

* file changes represented as reviewable patches;
* Git commits, branches or snapshots;
* configuration history;
* database migrations and backups;
* versioned project state;
* reversible plugin operations;
* and restoration points before destructive actions.

When an action cannot be reversed, Opure must clearly warn the developer before it is performed.

Irreversible actions require greater visibility and stronger approval than reversible actions.

---

## 7.9 Learn Through Proven Engineering

Opure should improve through verified engineering outcomes rather than through blind accumulation of generated code.

The platform may maintain structured knowledge about:

* project architecture;
* components and relationships;
* engineering decisions;
* conventions;
* successful fixes;
* failed approaches;
* test results;
* build history;
* reusable patterns;
* documentation;
* and developer-approved solutions.

Reusable code and engineering patterns should progress through evidence-based confidence levels such as:

* **Draft** — generated or captured but not yet validated;
* **Compiled** — successfully built or parsed;
* **Tested** — relevant tests have passed;
* **Reviewed** — examined and approved by a developer;
* **Proven** — used successfully in a real project;
* **Trusted** — reused successfully with no known regression across multiple relevant cases.

Opure must not describe software as entirely bug-free.

Confidence should be based on recorded evidence such as successful compilation, passing tests, developer review, production use and repeated reuse without known regression.

Structured metadata should remain authoritative. Vector search may help retrieve semantically relevant knowledge, but similarity alone must never be treated as proof of correctness.

Project knowledge must support developer judgement, not replace it.

---

## 7.10 Open by Architecture

Opure should be protocol-driven rather than provider-driven.

Developers must not be locked into:

* one AI model;
* one AI provider;
* one tool ecosystem;
* one integration method;
* one development environment;
* one cloud service;
* or one proprietary protocol.

Opure should support replaceable integrations through well-defined mechanisms such as:

* native plugins;
* Model Context Protocol servers;
* command-line adapters;
* local services;
* application programming interfaces;
* and other open or documented protocols.

AI providers must communicate through an AI Router or equivalent abstraction so that the rest of the platform does not depend directly upon a particular provider.

MCP servers and other external tool systems must remain subject to Opure's permissions, visibility, security policies and Trust Centre controls.

---

## 7.11 Loose Coupling by Design

Major Opure components should not depend directly upon the internal implementation of other major components.

Subsystems should communicate through clearly defined:

* interfaces;
* events;
* messages;
* contracts;
* and versioned protocols.

This principle should make components independently:

* replaceable;
* testable;
* maintainable;
* upgradeable;
* and understandable.

Replacing an AI provider, database, user-interface framework or integration system should not require rewriting the whole platform.

Loose coupling must not be used as an excuse for unnecessary complexity. Interfaces should remain clear, practical and proportionate to the system being built.

---

# 8. Developer Rights

Every Opure developer has:

1. The right to know what the platform is doing.
2. The right to understand why an important action was proposed.
3. The right to review important changes before they are applied.
4. The right to work locally and offline wherever technically practical.
5. The right to choose their own AI provider.
6. The right to inspect the data proposed for external sharing.
7. The right to deny network access.
8. The right to disable automation.
9. The right to inspect prompts, supplied context, tool use and generated patches.
10. The right to reverse significant actions wherever technically practical.
11. The right to own and control their project data.
12. The right to understand known uncertainty and provider limitations.
13. The right to revoke previously granted permissions.
14. The right to make the final decision.

These rights are not optional interface features.

They are part of the architecture and philosophy of Opure.

---

# 9. Reviewable Changes

AI-generated code changes should be presented as reviewable patches by default rather than being silently written into project files.

A proposed change should clearly identify:

* the files affected;
* the lines or sections changed;
* the purpose of the change;
* relevant risks;
* tests performed;
* validation results;
* and any unresolved uncertainty.

The developer should be able to approve, reject or modify a proposed patch.

Controlled autonomous workflows may apply pre-approved changes within clearly defined permissions, but their actions must remain visible, logged and reversible wherever technically practical.

Opure should prefer the smallest safe change that fulfils the developer's intent.

---

# 10. Cloud Consent and Data Visibility

Opure must obtain explicit developer approval before sending private project information to an external cloud service or remote AI provider.

Potentially sensitive information includes:

* source code;
* project files;
* private documentation;
* credentials;
* environment variables;
* configuration;
* logs;
* database content;
* project memory;
* and private conversation context.

Before requesting approval, Opure must clearly show:

* what information is proposed for sharing;
* why that information is required;
* which provider or service will receive it;
* which project or account the request relates to;
* whether potentially sensitive information was detected;
* whether information was removed or redacted;
* and any known limitations concerning storage, retention or further processing.

The developer must be able to:

* inspect the proposed data;
* remove unnecessary context;
* redact sensitive content;
* cancel the request;
* select a different provider;
* or choose a local alternative where available.

Approval for one request must not automatically become unlimited approval for future requests.

Persistent permissions must be deliberately enabled, clearly explained, limited in scope and reversible.

Cloud usage must remain consistent with **Visible by Design**.

---

## 10.1 Per-Project Cloud Policies

Each Opure project must have its own visible and configurable cloud-access policy.

Supported policy modes should include:

* **Local Only** — project data must not be sent to cloud services.
* **Ask Every Time** — explicit approval is required for every external request.
* **Approved Providers Only** — requests may be sent only to providers deliberately approved for that project.
* **Custom Policy** — the developer may define permissions for providers, data types, tools and workflows.

The active policy must be clearly displayed within the project and enforced by Opure's Policy Engine and Network Gateway.

A project policy should define:

* which providers may be contacted;
* which types of data may be shared;
* which files or folders are excluded;
* whether automatic redaction is required;
* whether approval is required for each request;
* whether particular workflows may use cloud services;
* and how long any permission remains valid.

Changing a project to a less restrictive policy must require deliberate developer approval.

Plugins, MCP servers, workflows and AI models must not bypass the project's cloud policy.

The developer must be able to review, change or revoke these permissions at any time.

---

## 10.2 Default Cloud Policy

New Opure projects must default to **Ask Every Time**.

Under this policy, every request involving an external AI provider or cloud service requires explicit developer approval before project data leaves the local machine.

During project creation, the developer must also be offered **Local Only** as a clear and equally accessible option.

Opure must never silently select a less restrictive policy based on account status, provider availability, convenience or settings from an unrelated project.

---

# 11. Secrets and Sensitive Information

## 11.1 Secret Detection Before External Sharing

Before sending project data to any external provider or cloud service, Opure must inspect the proposed data for likely secrets and sensitive credentials.

Examples include:

* API keys;
* passwords;
* access tokens;
* authentication cookies;
* private encryption keys;
* connection strings;
* environment variables containing secrets;
* signing credentials;
* and other recognised credential formats.

Detected secrets must be redacted or blocked from external transmission by default.

Opure must clearly show:

* what sensitive information was detected;
* where it was found;
* whether it will be removed or replaced;
* and why sharing it may create a security risk.

A developer may deliberately override a warning when technically necessary, but the override must require explicit confirmation and be recorded in the Trust Centre.

Plugins, MCP servers, workflows and external tools must not bypass secret detection or project cloud policies.

---

## 11.2 Secret Scanning for Generated Changes

Before a generated patch may be applied or committed, Opure must scan the proposed changes for accidentally introduced secrets, credentials and other sensitive information.

This scan should include:

* newly added API keys;
* passwords;
* access tokens;
* private keys;
* connection strings;
* authentication cookies;
* signing credentials;
* sensitive environment values;
* and recognised secret patterns.

When a likely secret is detected, Opure must:

* block the patch from being applied or committed by default;
* identify the affected file and location;
* explain the detected risk;
* recommend a safer alternative, such as an environment variable or secure credential store;
* and record the event in the Trust Centre.

The developer may explicitly override a warning when technically necessary, but the override must be deliberate, clearly confirmed and recorded.

Secret scanning must apply regardless of whether the change was produced by an AI model, workflow, plugin, MCP server, external tool or the developer.

---

## 11.3 Protection Against Committing Sensitive Files

Opure must prevent recognised sensitive files and credential stores from being added to version control by default.

Protected files and patterns should include:

* `.env` files;
* private encryption keys;
* SSH private keys;
* signing keys and certificates;
* credential databases;
* authentication token stores;
* cloud-provider credential files;
* password exports;
* and other recognised secret-bearing files.

Opure should protect projects through measures such as:

* recommended `.gitignore` rules;
* pre-commit secret scanning;
* warnings before staging sensitive files;
* blocking unsafe commits by default;
* and clear guidance for moving secrets into environment variables or secure credential storage.

When a protected file is detected, Opure must identify the file, explain the risk and recommend a safer alternative.

A deliberate override may be permitted when technically necessary, but it must require explicit confirmation and be recorded in the Trust Centre.

Opure must not assume that a file is safe merely because it was created by the developer rather than generated by AI.

---

## 11.4 Encrypted Secrets Vault

Passwords, API keys, tokens, private keys and other credentials must not be stored in Opure's normal project database, project memory, vector index, logs or conversation history.

Secrets required by Opure or its integrations must be stored in a dedicated encrypted secrets vault.

The vault should:

* use strong, established encryption;
* use operating-system-backed credential protection where available;
* encrypt stored secrets at rest;
* minimise the time secrets are held in memory;
* restrict access according to the principle of least privilege;
* reveal values only when explicitly requested and authorised;
* support revocation, replacement and deletion;
* and avoid exposing secret values in logs, prompts, error messages or the Trust Centre.

The Trust Centre may record that a secret was accessed, which component requested it and whether access was approved, but it must never record the secret value itself.

Plugins, MCP servers, providers and workflows must request secrets through the vault's controlled interface. They must not read the vault directly or copy secret values into ordinary storage.

Where a secret must be inserted into a command, environment or network request, Opure should disclose the destination and purpose without unnecessarily displaying the secret itself.

---

# 12. External Capabilities and Tool Mediation

AI models must not receive unrestricted direct access to the developer's system.

External capabilities should be mediated by Opure through appropriate systems such as:

* the Policy Engine;
* the Network Gateway;
* the Plugin Manager;
* the MCP Gateway;
* the Workspace and Patch Engine;
* the Secrets Vault;
* and the Trust Centre.

A typical action should follow this pattern:

1. The AI or workflow expresses an intent.
2. Opure evaluates the request.
3. Permissions, project policies and security rules are checked.
4. Required developer approval is requested.
5. The action is executed through an approved integration.
6. The result is validated, recorded and displayed.
7. Recovery or rollback information is retained where possible.

Every external capability must remain accountable to the same principles regardless of whether it is provided through a native plugin, MCP server, API, local service or command-line tool.

---

# 13. The Trust Centre

Opure should maintain an inspectable record of significant actions.

Where relevant, each record should include:

* the task or developer intent;
* the workflow used;
* the model and provider selected;
* the files or data categories accessed;
* the context sent externally;
* tools and integrations invoked;
* commands executed;
* network destinations contacted;
* permissions and approvals;
* patches and changes produced;
* validation and test results;
* warnings and overrides;
* the final outcome;
* and the time and duration of the action.

Trust Centre records must remain readable and useful rather than becoming meaningless technical noise.

Sensitive values must be redacted. Secret values must never be recorded.

The Trust Centre exists to support understanding, accountability, diagnosis and recovery — not surveillance.

---

# 14. Performance and Hardware Respect

Opure must respect the developer's computer as well as their code.

It should use CPU, memory, storage, network bandwidth and accelerator resources deliberately and proportionately.

The platform should:

* avoid loading unnecessary models;
* avoid wasteful parallelism;
* schedule heavy work intelligently;
* remain responsive during long operations;
* disclose substantial downloads or resource usage;
* provide practical performance controls;
* and prefer efficient engineering workflows over unnecessary computation.

Greater automation must not automatically mean greater resource consumption.

---

# 15. What Opure Is Not

Opure is not:

* merely an AI chatbot;
* merely a code generator;
* a black-box autonomous agent;
* a cloud dependency disguised as a local application;
* a telemetry platform;
* a replacement for developer judgement;
* a system that silently uploads private work;
* a system that hides important actions;
* a system that stores secrets carelessly;
* or a platform controlled by a single AI vendor.

Opure must not pursue convenience at the cost of developer authority, privacy, security or understanding.

---

# 16. The Opure Trust Test

Every significant Opure feature should be evaluated against the following questions:

1. Does it respect the developer?
2. Can the developer see what it is doing?
3. Can the developer understand why it is doing it?
4. Can the developer inspect what data it uses or shares?
5. Can the developer stop it?
6. Can the developer review the result?
7. Can the action be reversed where technically practical?
8. Does it preserve local operation wherever practical?
9. Does it protect secrets and sensitive information?
10. Does it avoid unnecessary provider lock-in?
11. Does it strengthen software engineering rather than merely generate code?
12. Does it keep the human in control?

A feature that fails these questions is not complete.

---

# 17. Charter Governance

This Charter is a living founding document, but it must not change casually.

Changes should be:

* deliberate;
* documented;
* reviewed against the existing principles;
* recorded in version control;
* and approved by the project founder or an explicitly authorised governance process.

Technical specifications may expand upon this Charter, but they must not silently weaken or contradict it.

Where a technical design conflicts with the Charter, the conflict must be made visible and resolved before the design is accepted.

Architecture Decision Records should record major implementation choices and explain how those choices support the Charter.

---

# 18. Looking Ahead

Opure will evolve.

New AI models, protocols, programming languages, operating systems and engineering methods will emerge. The platform should be capable of adopting better technology without abandoning its identity.

Its implementation may change.

Its founding commitments should not.

Opure will remain:

* developer-respecting;
* developer-first;
* software-engineering-first;
* local by design;
* cloud optional;
* visible by design;
* human controlled;
* open by architecture;
* loosely coupled;
* secure with secrets;
* reversible wherever technically practical;
* and committed to learning through proven engineering.

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**
