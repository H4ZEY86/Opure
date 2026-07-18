# SPEC-006 — Workflow and Agent System

## Opure Platform Controlled Engineering Automation

**Document:** SPEC-006  
**Status:** Founder Draft  
**Version:** 0.1  
**Language:** British English  
**Last updated:** 18 July 2026  
**Depends on:** CHARTER-001, SPEC-001, SPEC-002, SPEC-003, SPEC-004, SPEC-005  

---

## 1. Purpose

This specification defines the Workflow and Agent System of the Opure Platform.

The Workflow and Agent System is responsible for coordinating multi-stage engineering work such as:

- understanding developer intent;
- planning;
- architecture analysis;
- context retrieval;
- code generation;
- patch production;
- validation;
- testing;
- review;
- documentation;
- Git preparation;
- approval;
- application;
- recovery;
- and final reporting.

In Opure, agents are not permanently independent artificial intelligences.

They are named engineering roles implemented through:

- workflows;
- prompts;
- contracts;
- policies;
- tools;
- model requests;
- and deterministic platform services.

The system must preserve human authority, visible execution, project safety, provider neutrality and controlled automation at every stage.

---

## 2. Founding Rule

> **Agents are workflows, not authorities.**

A Planner, Architect, Coder, Reviewer, Tester, Documentation or Git agent:

- does not own project files;
- does not grant itself permission;
- does not execute arbitrary tools directly;
- does not bypass project policy;
- does not silently apply code;
- and does not become a separate source of truth.

Each role is a bounded workflow participant operating through Opure service contracts.

---

## 3. Relationship to Other Services

A simplified logical view is:

```text
Developer / Desktop
        │
        ▼
Desktop Gateway
        │
        ▼
Workflow Engine
        │
        ├── Workflow Registry
        ├── Workflow Planner
        ├── Stage Coordinator
        ├── State Machine
        ├── Checkpoint Manager
        ├── Approval Coordinator
        ├── Retry and Compensation Manager
        ├── Role Template Library
        ├── Workflow Policy Binder
        ├── Progress Projector
        └── Outcome Recorder
                │
                ├── Scheduler
                ├── Policy Engine
                ├── AI Router
                ├── Context Engine
                ├── Knowledge Engine
                ├── Patch Service
                ├── Build Manager
                ├── Repository Service
                ├── Plugin Manager
                ├── MCP Gateway
                ├── CLI Adapter Host
                ├── Network Gateway
                ├── Secrets Vault
                └── Trust Centre
```

The Workflow Engine owns workflow state and coordination.

It does not own the implementation of the services it invokes.

---

## 4. Design Goals

The Workflow and Agent System must be:

- human-controlled;
- inspectable;
- resumable;
- cancellable;
- policy-bound;
- provider-neutral;
- deterministic where safety requires it;
- adaptable;
- testable;
- recoverable;
- resource-aware;
- and resistant to hidden authority escalation.

It should support simple one-step tasks and complex multi-stage engineering workflows through the same core model.

---

## 5. Non-Goals

The Workflow and Agent System is not responsible for:

- directly reading arbitrary files;
- directly applying patches;
- directly executing commands;
- deciding permissions;
- storing secrets;
- directly invoking AI providers;
- replacing source control;
- guaranteeing that generated code is correct;
- or simulating fictional autonomous personalities.

The Workflow Engine coordinates services that perform these actions.

It must not absorb their responsibilities.

---

## 6. Normative Language

The terms **MUST**, **MUST NOT**, **SHOULD**, **SHOULD NOT**, **MAY** and **DEFERRED** have the meanings defined in SPEC-001.

Any intentional violation of a **SHOULD** requirement must be documented in an Architecture Decision Record.

---

# 7. Core Concepts

## 7.1 Workflow Definition

A Workflow Definition is a versioned description of an engineering process.

It should define:

- workflow identifier;
- version;
- title;
- purpose;
- input schema;
- output schema;
- stages;
- transitions;
- permissions;
- approval points;
- retry rules;
- compensation rules;
- checkpoint policy;
- cancellation policy;
- resource expectations;
- and Trust Centre requirements.

---

## 7.2 Workflow Instance

A Workflow Instance is one execution of a Workflow Definition.

It should include:

- instance identifier;
- workflow definition identifier and version;
- project identifier;
- initiating user;
- intent;
- inputs;
- current state;
- current stage;
- stage history;
- permissions;
- approvals;
- checkpoints;
- outputs;
- errors;
- timing;
- and Trust Centre correlation.

---

## 7.3 Stage

A Stage is one bounded unit of workflow work.

A stage may:

- call a service;
- request AI inference;
- request approval;
- wait for an event;
- transform structured data;
- validate an output;
- or coordinate multiple sub-stages.

A stage must have one clear purpose.

---

## 7.4 Role

A Role is a named engineering responsibility associated with one or more stages.

Examples include:

- Planner;
- Architect;
- Coder;
- Reviewer;
- Tester;
- Documentation;
- Git;
- Security Reviewer;
- Performance Reviewer;
- and Release Coordinator.

A role is not a separate permanently running model.

---

## 7.5 Task

A Task is a unit of scheduled execution created by a workflow stage.

The Scheduler owns execution capacity.

The Workflow Engine owns task meaning and stage progression.

---

## 7.6 Checkpoint

A Checkpoint is a durable recovery point containing enough state to resume or safely conclude a workflow.

---

## 7.7 Approval Point

An Approval Point pauses progression until a developer or authorised policy grants a bounded approval.

---

## 7.8 Compensation

Compensation is a defined action that attempts to undo or mitigate an earlier completed action after a later failure.

Compensation is not assumed to be perfect rollback.

---

## 7.9 Workflow Outcome

A Workflow Outcome is the final result of a workflow.

Recommended outcomes include:

- completed;
- completed with warnings;
- partially completed;
- cancelled;
- failed;
- blocked by policy;
- blocked awaiting approval;
- rolled back;
- recovery required;
- and superseded.

---

# 8. Workflow Types

## 8.1 Interactive Workflow

Started directly by a developer and expected to provide visible progress.

Examples:

- explain this project;
- fix this bug;
- add a feature;
- review this patch;
- create tests;
- update documentation.

---

## 8.2 Background Workflow

Runs in the background within approved limits.

Examples:

- incremental indexing;
- pattern validation;
- dependency analysis;
- documentation freshness checks;
- and low-priority test discovery.

Background workflows must yield to interactive work.

---

## 8.3 Scheduled Workflow

Runs at a configured time or cadence.

Examples:

- nightly test run;
- dependency audit;
- project health report;
- and periodic pattern revalidation.

Scheduled execution must remain visible and configurable.

---

## 8.4 Event-Triggered Workflow

Begins in response to an Opure event.

Examples:

- file changed;
- patch applied;
- build failed;
- test failed;
- dependency updated;
- or plugin installed.

Event triggers must not silently create high-impact actions.

---

## 8.5 Manual Workflow

Requires developer initiation for every run.

This should be the default for high-impact workflows.

---

## 8.6 Controlled Autonomous Workflow

May execute without approval at every stage, but only within explicit permissions and limits.

It must remain:

- visible;
- logged;
- cancellable;
- scoped;
- and reversible where technically practical.

---

## 8.7 Recovery Workflow

Designed to diagnose and recover from interrupted or failed operations.

Recovery workflows must prioritise integrity over completion.

---

# 9. Built-In Engineering Roles

## 9.1 Planner

The Planner role translates developer intent into a structured engineering plan.

It may produce:

- goals;
- assumptions;
- constraints;
- tasks;
- dependencies;
- risks;
- validation strategy;
- and completion criteria.

The Planner does not apply changes.

---

## 9.2 Architect

The Architect role evaluates:

- system boundaries;
- interfaces;
- dependencies;
- data flow;
- design alternatives;
- and compatibility with Opure specifications and project conventions.

It may propose architecture changes.

It does not grant approval or directly modify files.

---

## 9.3 Coder

The Coder role produces structured change proposals or patch content.

It must work through:

- Context Engine;
- AI Router where used;
- Patch Service;
- and project policy.

It must not write directly to project files.

---

## 9.4 Reviewer

The Reviewer role evaluates:

- correctness;
- maintainability;
- project consistency;
- security concerns;
- testing;
- documentation;
- and unresolved uncertainty.

Review output must distinguish evidence from opinion.

---

## 9.5 Tester

The Tester role plans or invokes validation through the Build Manager.

It may:

- select relevant tests;
- propose new tests;
- interpret results;
- and identify gaps.

It does not falsify or invent test results.

---

## 9.6 Documentation

The Documentation role updates or proposes:

- README content;
- API documentation;
- comments;
- changelog entries;
- decision records;
- and user-facing guidance.

Documentation changes must remain reviewable patches.

---

## 9.7 Git

The Git role coordinates repository operations through the Repository Service.

It may propose:

- branch names;
- commit groupings;
- commit messages;
- and restore actions.

It must not commit secrets or bypass repository policy.

---

## 9.8 Security Reviewer

The Security Reviewer role evaluates:

- permission use;
- secret exposure;
- unsafe command execution;
- network activity;
- insecure dependencies;
- and trust-boundary changes.

Deterministic security checks remain authoritative.

---

## 9.9 Performance Reviewer

The Performance Reviewer role evaluates:

- resource use;
- latency;
- memory;
- concurrency;
- and likely regressions.

Measured evidence must be distinguished from speculation.

---

## 9.10 Release Coordinator

The Release Coordinator role may prepare:

- release checklist;
- version changes;
- changelog;
- build verification;
- packaging steps;
- and release notes.

It must not publish externally without explicit permission.

---

# 10. Role Implementation

## 10.1 Role Template

A Role Template should define:

- role identifier;
- version;
- purpose;
- permitted stage types;
- default prompt template;
- required context categories;
- allowed tools;
- expected output schema;
- validation rules;
- and fallback behaviour.

---

## 10.2 Shared Models

Multiple roles may use the same model.

Role identity must come from:

- workflow intent;
- context;
- prompt template;
- expected output;
- and validation.

It must not require one dedicated model process per role.

---

## 10.3 Role Composition

A workflow may use one role multiple times.

Example:

```text
Planner
    ↓
Coder
    ↓
Reviewer
    ↓
Coder Revision
    ↓
Tester
```

The second Coder stage must receive the review evidence and updated context.

---

## 10.4 No Persona Authority

Role names are functional labels.

The interface must not imply that a role has independent authority, consciousness or ownership of the project.

---

# 11. Workflow Definition Model

## 11.1 Required Fields

A Workflow Definition must include:

- unique identifier;
- semantic version;
- title;
- description;
- owner;
- source;
- trust classification;
- input schema;
- output schema;
- stage graph;
- default policy requirements;
- and compatibility metadata.

---

## 11.2 Source

A workflow source may be:

- built-in;
- developer-authored;
- plugin-provided;
- imported;
- or AI-proposed.

AI-proposed workflows must not become trusted automatically.

---

## 11.3 Versioning

Workflow definitions must be versioned.

A running workflow instance must remain tied to the exact definition version that started it.

Updating a definition must not silently change active instances.

---

## 11.4 Compatibility

A workflow should declare compatible:

- Opure contract versions;
- required services;
- required capabilities;
- project types;
- languages;
- tools;
- and providers where applicable.

---

## 11.5 Stage Graph

The stage graph may support:

- sequence;
- conditional branch;
- parallel branch;
- join;
- loop;
- approval wait;
- event wait;
- sub-workflow;
- compensation path;
- and terminal state.

Unbounded loops are prohibited.

---

## 11.6 Human-Readable Form

Every workflow definition must have a human-readable representation.

The developer should be able to understand:

- what stages exist;
- what each stage does;
- what permissions it requires;
- and where approvals occur.

---

# 12. Stage Model

## 12.1 Stage Types

Supported stage types should include:

- service command;
- service query;
- AI request;
- context retrieval;
- deterministic transform;
- validation;
- approval;
- event wait;
- delay;
- sub-workflow;
- compensation;
- and manual input.

---

## 12.2 Stage Inputs

A stage must define:

- required inputs;
- optional inputs;
- input sources;
- schema;
- sensitivity;
- and maximum size where relevant.

---

## 12.3 Stage Outputs

A stage must define:

- output schema;
- validation;
- lifecycle;
- visibility;
- and whether output may be persisted.

---

## 12.4 Stage State

A stage should use states such as:

- pending;
- ready;
- scheduled;
- running;
- waiting for approval;
- waiting for input;
- waiting for dependency;
- completed;
- completed with warnings;
- failed;
- cancelled;
- skipped;
- compensated;
- and recovery required.

---

## 12.5 Stage Timeouts

Every non-trivial stage should define a timeout or deadline policy.

Timeout must not automatically mean that an external action stopped.

---

## 12.6 Stage Cancellation

A running stage must support cancellation where technically practical.

Cancellation behaviour must be declared.

---

## 12.7 Stage Idempotency

A stage should declare whether it is:

- idempotent;
- conditionally idempotent;
- or non-idempotent.

Retries and resume behaviour must respect this classification.

---

# 13. Workflow State Machine

## 13.1 Workflow States

A workflow instance should use:

- created;
- validating;
- ready;
- running;
- paused;
- waiting for approval;
- waiting for input;
- cancelling;
- cancelled;
- completed;
- completed with warnings;
- failed;
- compensating;
- rolled back;
- recovery required;
- and superseded.

---

## 13.2 Valid Transitions

Transitions must be explicit and tested.

Invalid transitions must be rejected.

---

## 13.3 Durable State

Workflow state must be persisted after significant transitions.

---

## 13.4 State History

Every transition should retain:

- previous state;
- new state;
- reason;
- actor;
- timestamp;
- stage;
- and correlation identifier.

---

# 14. Workflow Planning

## 14.1 Static Workflows

A static workflow has a predefined stage graph.

Examples include:

- review patch;
- run tests;
- prepare commit;
- and reindex project.

---

## 14.2 Dynamic Workflows

A dynamic workflow may generate or adapt stages based on developer intent and project evidence.

Dynamic planning must still produce a concrete inspectable workflow before high-impact execution.

---

## 14.3 AI-Proposed Plans

AI may propose:

- stages;
- ordering;
- tools;
- checks;
- and expected outputs.

The proposal must be validated against:

- allowed stage types;
- available capabilities;
- project policy;
- resource limits;
- and workflow safety rules.

---

## 14.4 Plan Approval

A plan may require approval before execution.

Approval strength should depend on:

- risk;
- scope;
- external sharing;
- file impact;
- command execution;
- irreversible actions;
- and autonomy level.

---

## 14.5 Plan Mutation

A running workflow may adapt its plan only within declared rules.

Material plan changes require renewed approval.

Examples include:

- new file scope;
- new provider;
- new command;
- broader secret access;
- new network destination;
- destructive action;
- and expanded project scope.

---

# 15. Developer Intent

## 15.1 Intent Record

Every workflow must retain a developer-intent record.

It should include:

- original request;
- interpreted goal;
- constraints;
- accepted assumptions;
- exclusions;
- desired outcome;
- and success criteria.

---

## 15.2 Ambiguity

When intent is ambiguous, Opure should:

- use available project context;
- make safe assumptions;
- expose those assumptions;
- and ask only when the ambiguity materially affects safety or outcome.

---

## 15.3 Intent Drift

A workflow must not silently drift away from the original goal.

If the workflow discovers a materially different problem, it should pause or propose a revised plan.

---

# 16. Context Use

## 16.1 Context Requests

Workflow stages request context through the Context Engine.

They must not read arbitrary project data directly.

---

## 16.2 Context Scope

Each stage should declare:

- project;
- file scope;
- symbol scope;
- memory categories;
- history requirements;
- and maximum context size.

---

## 16.3 Context Provenance

Stage outputs should retain a reference to the context package used.

---

## 16.4 External Context

Context sent to remote providers must follow:

- project cloud policy;
- secret detection;
- data-sharing preview;
- and approval.

---

## 16.5 Context Refresh

A long workflow should refresh context before applying changes if the project has changed.

---

# 17. AI Requests

## 17.1 AI Router Requirement

All AI requests must pass through the AI Router.

---

## 17.2 Request Contract

An AI stage must define:

- role;
- task type;
- required capabilities;
- expected output schema;
- context package;
- privacy classification;
- provider restrictions;
- timeout;
- retry policy;
- and fallback policy.

---

## 17.3 Structured Output

Planning, patch plans, reviews and tool proposals should use structured output where practical.

---

## 17.4 AI Output Validation

AI output must be validated before stage completion.

Validation may include:

- schema validation;
- reference validation;
- capability validation;
- path validation;
- and policy validation.

---

## 17.5 AI Failure

An AI failure must not corrupt workflow state.

The workflow may:

- retry;
- use an approved fallback;
- request developer input;
- skip an optional stage;
- or fail safely.

---

## 17.6 No Hidden Model Authority

A model may recommend that a workflow continue.

It cannot authorise the continuation.

---

# 18. Tool and Service Calls

## 18.1 Controlled Invocation

Workflow stages may invoke only registered service capabilities.

---

## 18.2 Tool Proposal Boundary

Model-generated tool requests are proposals.

The Workflow Engine validates and routes them.

---

## 18.3 Policy Evaluation

Protected calls require Policy Engine evaluation.

---

## 18.4 Approval Binding

An approval must bind to the exact or bounded action shown to the developer.

---

## 18.5 Tool Results

Tool results must retain:

- source;
- status;
- timing;
- output classification;
- and trust metadata.

---

# 19. Permissions

## 19.1 Workflow Permission Set

A workflow must have an explicit permission set.

Permissions may include:

- project read;
- project write through Patch Service;
- build execution;
- test execution;
- repository read;
- repository write;
- network access;
- provider access;
- secret access;
- plugin access;
- MCP capability;
- and external publication.

---

## 19.2 Least Privilege

A workflow must receive only the permissions required for its current stages.

---

## 19.3 Stage-Level Permissions

Permissions should be bound to stages where practical.

---

## 19.4 Permission Escalation

A workflow must not escalate its own permissions.

Escalation requires Policy Engine evaluation and developer approval where applicable.

---

## 19.5 Permission Expiry

Temporary permissions should expire when:

- the stage completes;
- the workflow completes;
- the timeout is reached;
- or the developer revokes them.

---

# 20. Approval Model

## 20.1 Approval Types

Approval types should include:

- approve once;
- approve stage;
- approve bounded action set;
- approve workflow instance;
- approve project policy exception;
- deny;
- modify;
- and cancel workflow.

---

## 20.2 Approval Presentation

An approval request should show:

- what will happen;
- why;
- affected files;
- commands;
- provider;
- data shared;
- network destination;
- secrets requested;
- reversibility;
- risks;
- and alternatives.

---

## 20.3 Approval Reuse

Approval may be reused only within its explicit scope.

---

## 20.4 Material Change

A material change invalidates approval.

---

## 20.5 Approval Logging

Approval and denial must be recorded without storing secret values.

---

# 21. Automation Levels

## 21.1 Manual

Opure proposes only.

No state-changing action occurs without direct approval.

---

## 21.2 Assisted

Opure may execute low-risk read-only or validation stages automatically.

State-changing stages require approval.

---

## 21.3 Controlled Automation

Opure may execute pre-approved state-changing actions within explicit scope.

---

## 21.4 Workflow Automation

A complete workflow may execute within:

- project policy;
- workflow permission set;
- risk limits;
- and approval rules.

---

## 21.5 Autonomy Boundaries

No automation level may bypass:

- secrets protection;
- project cloud policy;
- irreversible-action warnings;
- network policy;
- Trust Centre recording;
- or deterministic safety checks.

---

# 22. Risk Classification

## 22.1 Risk Levels

Workflow stages should be classified as:

- **Low**
- **Moderate**
- **High**
- **Critical**

---

## 22.2 Low Risk

Examples:

- read project metadata;
- query memory;
- inspect repository status;
- generate explanation;
- run non-modifying static analysis.

---

## 22.3 Moderate Risk

Examples:

- generate patch;
- run tests;
- run known build command;
- create local snapshot;
- update project memory.

---

## 22.4 High Risk

Examples:

- apply broad patch;
- install dependency;
- execute package scripts;
- modify database schema;
- access secrets;
- send project code to cloud;
- commit changes.

---

## 22.5 Critical Risk

Examples:

- publish release;
- delete project data;
- irreversible migration;
- destructive external API action;
- privilege elevation;
- rotate or expose secrets;
- and force-push repository history.

---

## 22.6 Risk Determination

Risk must be determined by deterministic rules.

AI may provide additional warning context but must not lower risk classification.

---

# 23. Checkpointing

## 23.1 Checkpoint Triggers

Checkpoints should occur:

- before state-changing stages;
- after successful high-cost stages;
- before approval waits;
- before external publication;
- before irreversible actions;
- and periodically during long workflows.

---

## 23.2 Checkpoint Contents

A checkpoint should include:

- workflow state;
- completed stage outputs;
- current plan;
- permissions;
- approvals;
- context references;
- patch references;
- task handles;
- and compensation state.

---

## 23.3 Sensitive Data

Checkpoints must not store raw secret values.

---

## 23.4 Resume Validation

Before resume, Opure must validate:

- project identity;
- workflow definition version;
- file state;
- policy;
- permissions;
- provider availability;
- and checkpoint integrity.

---

## 23.5 Stale Resume

If project state changed materially, the workflow should:

- refresh context;
- revalidate patches;
- recompute affected stages;
- or require developer review.

---

# 24. Cancellation

## 24.1 Developer Cancellation

The developer must be able to cancel a running workflow.

---

## 24.2 Cancellation Propagation

Cancellation must propagate to:

- Scheduler tasks;
- AI requests;
- builds;
- tests;
- plugin calls;
- MCP calls;
- CLI processes;
- and waiting stages.

---

## 24.3 Safe Completion

Cancellation should allow active transactional stages to:

- finish safely;
- roll back;
- or enter recovery-required state.

---

## 24.4 Cancellation Outcome

The workflow must report:

- what stopped;
- what completed;
- what remains applied;
- what was reversed;
- and whether recovery is required.

---

# 25. Pause and Resume

## 25.1 Pause

A workflow may pause because of:

- developer request;
- approval wait;
- missing input;
- resource pressure;
- provider failure;
- external dependency;
- or policy change.

---

## 25.2 Resume

Resume must restore from a valid checkpoint.

---

## 25.3 Long Pauses

A workflow paused for a long period must revalidate:

- project state;
- provider state;
- permissions;
- approvals;
- and external assumptions.

---

# 26. Retry

## 26.1 Retry Eligibility

A stage may retry when:

- failure is transient;
- operation is idempotent;
- duplicate side effects are prevented;
- deadline permits;
- and policy allows.

---

## 26.2 Retry Limits

Retries must be bounded.

---

## 26.3 Retry Visibility

The workflow must record retry attempts and reasons.

---

## 26.4 Human Review

Repeated failure should trigger:

- stage failure;
- alternate route proposal;
- or developer review.

---

# 27. Compensation and Rollback

## 27.1 Compensation Definition

A workflow should define compensation for state-changing stages where technically practical.

---

## 27.2 Examples

Examples include:

- reverse applied patch;
- restore snapshot;
- unstage files;
- remove installed plugin;
- restore configuration;
- and revert temporary project-memory changes.

---

## 27.3 Partial Compensation

Compensation may fail or be incomplete.

The workflow must report the exact outcome.

---

## 27.4 Irreversible Actions

Irreversible stages require:

- explicit warning;
- stronger approval;
- precondition validation;
- and recovery guidance.

---

# 28. Validation

## 28.1 Validation Stages

A workflow should include validation appropriate to the change.

Examples include:

- parse;
- format;
- lint;
- type-check;
- compile;
- unit test;
- integration test;
- security scan;
- performance check;
- and documentation check.

---

## 28.2 Validation Evidence

Validation results must identify:

- tool;
- version;
- command or profile;
- source revision;
- outcome;
- warnings;
- and timing.

---

## 28.3 Unperformed Validation

The workflow must clearly state which validation was not performed.

---

## 28.4 Validation Failure

Failure may:

- return to Coder stage;
- request developer review;
- run a narrower retry;
- or fail the workflow.

---

# 29. Review

## 29.1 Review Inputs

Review stages should receive:

- developer intent;
- plan;
- patch;
- validation evidence;
- project conventions;
- and known uncertainty.

---

## 29.2 Review Categories

Review may cover:

- correctness;
- architecture;
- maintainability;
- security;
- performance;
- tests;
- documentation;
- and scope.

---

## 29.3 Independent Review

A workflow may use a different model or prompt for review.

This must not be presented as proof of independence if both stages use the same provider and context.

---

## 29.4 Review Outcome

A review should produce:

- approve recommendation;
- request changes;
- block;
- warnings;
- and unresolved questions.

The developer remains final authority.

---

# 30. Human Input

## 30.1 Input Requests

A workflow may request human input when:

- intent is materially ambiguous;
- a trade-off requires developer choice;
- permission is required;
- external data sharing is proposed;
- an irreversible action is required;
- or evidence is conflicting.

---

## 30.2 Minimal Questions

The workflow should ask the fewest questions needed.

---

## 30.3 Defaults

Safe defaults may be offered.

They must not hide consequences.

---

## 30.4 Input Expiry

Waiting input may have an optional expiry policy.

No destructive default action should occur when input expires.

---

# 31. Dynamic Branching

## 31.1 Branch Conditions

Branch conditions must be explicit and inspectable.

---

## 31.2 Deterministic Branching

Safety-critical branching must use deterministic conditions.

---

## 31.3 AI-Assisted Branching

AI may propose a branch based on analysis.

The proposal must be validated against allowed transitions.

---

## 31.4 Loop Limits

Loops must define:

- maximum iterations;
- stopping condition;
- resource budget;
- and failure behaviour.

---

# 32. Parallel Execution

## 32.1 Appropriate Parallelism

Parallel stages may be used for:

- independent tests;
- independent static analysis;
- independent documentation retrieval;
- and safe provider comparisons.

---

## 32.2 Shared Resource Conflict

Parallel stages must not concurrently modify the same authoritative state without coordination.

---

## 32.3 Join

A join must define:

- required branches;
- optional branches;
- timeout;
- partial result behaviour;
- and failure policy.

---

## 32.4 Hardware Respect

Parallelism must be justified by measurable benefit.

It must not overload the developer's machine merely to simulate multiple agents.

---

# 33. Sub-Workflows

## 33.1 Purpose

Reusable workflow groups may be invoked as sub-workflows.

Examples include:

- validate patch;
- run security review;
- prepare commit;
- and update documentation.

---

## 33.2 Permission Inheritance

Sub-workflows inherit only permissions explicitly passed to them.

---

## 33.3 Version Binding

A parent workflow must record the exact sub-workflow version used.

---

## 33.4 Failure Propagation

The parent must define how sub-workflow outcomes affect progression.

---

# 34. Workflow Templates

## 34.1 Built-In Templates

Opure should provide built-in templates such as:

- Understand Project;
- Fix Bug;
- Add Feature;
- Review Patch;
- Generate Tests;
- Update Documentation;
- Refactor Safely;
- Prepare Commit;
- Diagnose Build Failure;
- and Release Preparation.

---

## 34.2 Template Inspection

Templates must be inspectable.

---

## 34.3 Template Cloning

The developer should be able to clone and customise a template.

---

## 34.4 Template Safety

A customised template must still pass workflow validation and policy checks.

---

# 35. Example Workflow — Fix Bug

A default bug-fix workflow may be:

```text
1. Capture Intent
2. Retrieve Error Evidence
3. Inspect Relevant Project Memory
4. Build Context Package
5. Planner Produces Fix Plan
6. Developer Reviews Plan if Required
7. Coder Produces Patch
8. Patch Service Validates Patch
9. Secret Scan
10. Build and Test
11. Reviewer Evaluates Result
12. Coder Revises if Needed
13. Developer Reviews Final Patch
14. Apply Patch
15. Re-run Validation
16. Update Project Memory
17. Prepare Optional Git Commit
18. Record Outcome
```

No stage may bypass service boundaries.

---

# 36. Example Workflow — Add Feature

A feature workflow may include:

```text
1. Capture Requirements
2. Inspect Architecture
3. Retrieve Existing Patterns
4. Planner Produces Task Plan
5. Architect Produces Design
6. Developer Approves Design
7. Coder Produces Patch Set
8. Validation
9. Tests
10. Documentation
11. Reviewer
12. Developer Approval
13. Apply
14. Commit Preparation
15. Memory Update
```

Large feature work may use multiple patch sets rather than one broad change.

---

# 37. Workflow Persistence

## 37.1 Storage Ownership

The Workflow Engine owns workflow definitions and instance state.

---

## 37.2 Storage Model

Persistent state should include:

- definitions;
- versions;
- instances;
- stages;
- transitions;
- checkpoints;
- approvals;
- outputs;
- and recovery metadata.

---

## 37.3 Output References

Large outputs should be referenced through owning services rather than duplicated.

---

## 37.4 Migration

Workflow schema migrations must be versioned and recoverable.

---

# 38. Workflow Memory Integration

## 38.1 Read

Workflows may query project memory through the Knowledge Engine.

---

## 38.2 Write

Workflows may propose new memory records.

The Knowledge Engine validates provenance and lifecycle.

---

## 38.3 Task Summaries

Completed workflows should produce a durable engineering summary where useful.

---

## 38.4 Failed Attempts

Failed approaches should be recorded when they may prevent repeated waste.

---

## 38.5 Pattern Candidates

Successful workflows may propose pattern candidates.

Promotion remains evidence-based.

---

# 39. Trust Centre Integration

## 39.1 Workflow Record

Every significant workflow should have a Trust Centre record.

---

## 39.2 Required Details

The record should include:

- developer intent;
- workflow definition and version;
- stages;
- model requests;
- context references;
- tools;
- commands;
- network activity;
- data sharing;
- approvals;
- patches;
- tests;
- retries;
- failures;
- compensations;
- and outcome.

---

## 39.3 Progressive Detail

The Trust Centre should provide:

1. concise workflow summary;
2. stage timeline;
3. detailed technical records.

---

## 39.4 Secret Safety

Secret values must never be recorded.

---

# 40. Workflow Security

## 40.1 Untrusted Definitions

Imported and plugin-provided workflows must be treated as untrusted until validated.

---

## 40.2 Capability Declaration

A workflow definition must declare all required capabilities.

---

## 40.3 Hidden Stages

A workflow must not contain hidden executable stages.

---

## 40.4 Definition Integrity

Built-in and installed workflow definitions should support integrity verification.

---

## 40.5 Prompt Injection

Project files and external content may attempt to alter workflow behaviour.

They must be treated as data.

Policy enforcement remains deterministic.

---

## 40.6 Secret Access

Secret references must be requested only for the stage requiring them.

---

# 41. Plugin and MCP Workflows

## 41.1 Plugin-Provided Workflows

Plugins may provide workflow templates.

They must declare:

- publisher;
- version;
- capabilities;
- stages;
- permissions;
- and compatibility.

---

## 41.2 MCP Stages

A workflow may invoke MCP capabilities through the MCP Gateway.

---

## 41.3 No Bypass

Plugin or MCP stages must not bypass:

- Policy Engine;
- Network Gateway;
- Secrets Vault;
- Trust Centre;
- or Scheduler.

---

## 41.4 Removal

Disabling a plugin or MCP server must make dependent workflows visibly unavailable or degraded.

---

# 42. Workflow API

## 42.1 Commands

The Workflow Engine should provide:

- `RegisterWorkflowDefinition`
- `UpdateWorkflowDefinition`
- `RemoveWorkflowDefinition`
- `StartWorkflow`
- `PauseWorkflow`
- `ResumeWorkflow`
- `CancelWorkflow`
- `ApproveWorkflowAction`
- `DenyWorkflowAction`
- `ProvideWorkflowInput`
- `RetryWorkflowStage`
- `SkipOptionalStage`
- `RunCompensation`
- and `CloneWorkflowDefinition`

---

## 42.2 Queries

The Workflow Engine should provide:

- `ListWorkflowDefinitions`
- `GetWorkflowDefinition`
- `ValidateWorkflowDefinition`
- `GetWorkflowInstance`
- `ListWorkflowInstances`
- `GetWorkflowTimeline`
- `GetWorkflowStage`
- `GetPendingApprovals`
- `GetWorkflowPermissions`
- `GetWorkflowCheckpoint`
- and `GetWorkflowOutcome`

---

## 42.3 Events

The Workflow Engine should publish:

- `WorkflowCreated`
- `WorkflowStarted`
- `WorkflowPaused`
- `WorkflowResumed`
- `WorkflowCancelled`
- `WorkflowCompleted`
- `WorkflowFailed`
- `WorkflowRecoveryRequired`
- `StageReady`
- `StageStarted`
- `StageCompleted`
- `StageFailed`
- `ApprovalRequested`
- `ApprovalGranted`
- `ApprovalDenied`
- `CheckpointCreated`
- `CompensationStarted`
- and `CompensationCompleted`

---

# 43. Error Model

Recommended error categories include:

- `WORKFLOW_DEFINITION_INVALID`
- `WORKFLOW_DEFINITION_INCOMPATIBLE`
- `WORKFLOW_NOT_FOUND`
- `WORKFLOW_ALREADY_RUNNING`
- `WORKFLOW_STATE_INVALID`
- `WORKFLOW_STAGE_FAILED`
- `WORKFLOW_STAGE_TIMEOUT`
- `WORKFLOW_STAGE_CANCELLED`
- `WORKFLOW_POLICY_DENIED`
- `WORKFLOW_APPROVAL_REQUIRED`
- `WORKFLOW_APPROVAL_INVALID`
- `WORKFLOW_PERMISSION_EXPIRED`
- `WORKFLOW_CONTEXT_STALE`
- `WORKFLOW_PATCH_CONFLICT`
- `WORKFLOW_RESOURCE_UNAVAILABLE`
- `WORKFLOW_PROVIDER_UNAVAILABLE`
- `WORKFLOW_COMPENSATION_FAILED`
- `WORKFLOW_CHECKPOINT_INVALID`
- `WORKFLOW_RECOVERY_REQUIRED`
- and `WORKFLOW_INTERNAL_ERROR`

Errors must include safe recovery guidance where possible.

---

# 44. Observability

## 44.1 Timeline

Every workflow should expose a timeline.

---

## 44.2 Progress

Progress should be stage-based.

Artificial percentage estimates must not be presented as precise when unknown.

---

## 44.3 Current Activity

The developer should see:

- current stage;
- active task;
- model or tool;
- resource status;
- blocking reason;
- and cancellation option.

---

## 44.4 Resource Use

Long workflows should expose relevant:

- CPU;
- memory;
- GPU;
- VRAM;
- network;
- and cost information.

---

# 45. Testing Strategy

## 45.1 Unit Tests

Unit tests must cover:

- workflow validation;
- state transitions;
- stage dependency resolution;
- permission binding;
- approval scope;
- retry rules;
- checkpointing;
- compensation;
- cancellation;
- and dynamic branching.

---

## 45.2 Definition Tests

Each built-in workflow must pass definition validation.

---

## 45.3 Integration Tests

Integration tests should cover:

- simple one-stage workflow;
- multi-stage patch workflow;
- approval wait;
- cancellation;
- resume;
- provider failure;
- build failure;
- patch conflict;
- compensation;
- and Trust Centre recording.

---

## 45.4 Policy Tests

Tests must prove:

- a workflow cannot escalate permission;
- local-only policy prevents cloud stages;
- stale approval is rejected;
- revoked permission stops progression;
- and hidden tool execution is impossible.

---

## 45.5 Fault Injection

Tests should simulate:

- Runtime restart;
- Workflow Engine crash;
- Scheduler loss;
- AI Router failure;
- Build Manager failure;
- Trust Centre temporary unavailability;
- disk full;
- and corrupted checkpoint.

---

## 45.6 Recovery Tests

Recovery tests must prove:

- active workflow state survives restart;
- stale project state is detected;
- unsafe automatic resume is blocked;
- completed stages are not repeated unnecessarily;
- and compensation state remains visible.

---

## 45.7 Architecture Tests

Architecture tests must enforce:

- no direct provider SDK usage;
- no direct workspace writes;
- no direct secret access;
- no direct MCP execution;
- and no policy bypass.

---

# 46. Performance Requirements

## 46.1 Orchestration Overhead

Workflow coordination overhead should remain small relative to the work being coordinated.

---

## 46.2 Large Workflows

The engine should support large workflows without loading all stage output into memory.

---

## 46.3 Event Volume

Progress events should be coalesced where appropriate.

Significant state transitions must not be dropped.

---

## 46.4 Parallelism

Parallel stages must respect Scheduler resource limits.

---

# 47. Initial Implementation Milestone

The first Workflow Engine milestone is successful when it can:

1. register a versioned workflow definition;
2. validate a stage graph;
3. create a workflow instance;
4. run sequential stages;
5. call mock service commands and queries;
6. request AI through the AI Router;
7. request context through the Context Engine;
8. pause for developer approval;
9. persist workflow state;
10. create checkpoints;
11. cancel a running workflow;
12. resume after restart;
13. retry an idempotent failed stage;
14. reject unsafe retry of non-idempotent work;
15. run a compensation stage;
16. expose a stage timeline;
17. record activity in the Trust Centre;
18. enforce stage permissions;
19. detect stale project context;
20. and execute a basic reviewable patch workflow.

---

# 48. Acceptance Criteria

SPEC-006 is implemented when:

- [ ] Agents are implemented as roles and workflows, not separate authorities.
- [ ] Workflow definitions are versioned and inspectable.
- [ ] Running instances remain tied to the definition version that started them.
- [ ] Stage graphs reject invalid cycles and unbounded loops.
- [ ] Every stage has clear inputs, outputs and ownership.
- [ ] Workflow state is durable.
- [ ] Long workflows support pause, resume and cancellation.
- [ ] Checkpoints support safe recovery.
- [ ] Project changes invalidate stale workflow assumptions.
- [ ] All AI requests pass through the AI Router.
- [ ] All file changes pass through the Patch Service.
- [ ] All commands pass through approved services.
- [ ] Protected actions invoke Policy Engine evaluation.
- [ ] Workflow permissions follow least privilege.
- [ ] Workflows cannot escalate their own permissions.
- [ ] Approval is bound to the action shown.
- [ ] Material action changes invalidate approval.
- [ ] Local Only policy blocks remote workflow stages.
- [ ] Secrets are accessed only through the Secrets Vault.
- [ ] Tool proposals do not execute automatically.
- [ ] Retries are bounded and idempotency-aware.
- [ ] Compensation outcomes remain explicit.
- [ ] Irreversible actions require stronger approval.
- [ ] Validation results identify what was and was not tested.
- [ ] Reviewer output distinguishes evidence from opinion.
- [ ] Failed approaches may be retained as engineering memory.
- [ ] Pattern candidates require later evidence-based promotion.
- [ ] Plugin and MCP workflow stages remain mediated.
- [ ] Parallel stages respect resource limits.
- [ ] Workflow progress is understandable.
- [ ] Trust Centre records contain no secret values.
- [ ] A workflow can survive Runtime restart.
- [ ] A cancelled workflow leaves project state known.
- [ ] The platform remains usable without autonomous workflows enabled.

---

# 49. Deferred Decisions

The following are intentionally deferred:

- workflow definition language;
- visual workflow editor;
- exact persistence engine;
- exact checkpoint format;
- default autonomy level;
- workflow marketplace;
- remote workflow execution;
- team approval chains;
- signed workflow packages;
- organisation policy inheritance;
- workflow scheduling interface;
- event-trigger configuration;
- and public automation APIs.

These decisions must not weaken:

- human control;
- project policy;
- visibility;
- cancellation;
- recoverability;
- and service boundaries.

---

# 50. Required Architecture Decision Records

Implementation should produce ADRs for:

- workflow definition format;
- workflow persistence;
- stage execution model;
- checkpoint format;
- approval-token integration;
- compensation model;
- dynamic workflow planning;
- role-template storage;
- workflow event transport;
- workflow scheduling;
- and plugin-provided workflow validation.

---

# 51. Relationship to Later Specifications

This specification provides the workflow foundation for:

- **SPEC-007 — Plugin SDK**
- **SPEC-008 — Trust Centre and Security**
- **SPEC-009 — Workspace and File Patch Engine**
- **SPEC-010 — Desktop User Interface**
- **SPEC-011 — Project and Build Management**
- **SPEC-012 — Roadmap**

SPEC-007 will define how plugins provide capabilities and workflow templates.

SPEC-008 will define detailed permissions, approvals, risk, secrets and audit rules.

SPEC-009 will define patch lifecycle and workspace transactions in greater detail.

SPEC-010 will define how workflows, roles, approvals and progress appear in the desktop interface.

Later specifications may add stricter requirements.

They must not turn agents into hidden authorities or permit workflow bypass of Opure services.

---

# 52. Founder Approval

This document remains a founder draft until explicitly approved.

Approval establishes the following rules:

- Agents are workflows, not authorities.
- Planner, Architect, Coder, Reviewer, Tester, Documentation and Git are bounded engineering roles.
- Roles may share one model and inference engine.
- Workflows are versioned, inspectable, cancellable and resumable.
- Every stage remains subject to project policy and least privilege.
- AI may propose plans and tools but cannot grant permission or execute protected actions directly.
- Generated changes remain reviewable patches.
- Approvals are bounded and invalidated by material change.
- Retries are bounded and compensation remains explicit.
- Workflows must survive interruption without leaving project state unknown.
- Controlled automation is permitted only within visible, revocable limits.
- The developer remains the final authority.

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**