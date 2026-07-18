# ADR-0025 — Workflow Execution, Checkpointing and Recovery

## Architecture Decision Record

**Status:** Proposed
**Date:** 18 July 2026
**Decision owners:** Founder and Product Owner
**Technical owners:** Workflow Runtime and Recovery Owner
**Reviewers:** Runtime Architecture Owner, Persistence Owner, Scheduler Owner, Tool Mediation Owner, Patch Service Owner, Workspace Owner, Repository Owner, Build Owner, Test Owner, AI Router Owner, Context Assembly Owner, Project Knowledge Owner, Project Memory Owner, Local Model Runtime Owner, Provider Trust Owner, Security Owner, Privacy Owner, Secrets Owner, Plugin Platform Owner, MCP Gateway Owner, Trust Centre Owner, Desktop Owner, Performance Owner, Release Owner, Recovery Owner, Test Architecture Owner
**Supersedes:** None
**Superseded by:** None
**Related ADRs:** ADR-0001 through ADR-0024
**Related specifications:** CHARTER-001, SPEC-001 through SPEC-012
**Target milestone:** Durable Workflow Runtime through Version 1.0

---

## 1. Decision Summary

Opure should implement workflows as **versioned declarative execution plans interpreted by a trusted Workflow Service**, with durable state transitions recorded before work is dispatched.

The Workflow Service should not replay arbitrary C# orchestration methods.

The Workflow Service should not depend on:

* a cloud orchestration service;
* a provider-side agent thread;
* a provider conversation;
* a hidden model loop;
* a plugin-owned scheduler;
* or an MCP server remaining available.

The initial workflow architecture should use:

* immutable Workflow Definitions;
* immutable Compiled Workflow Plans;
* one durable Workflow Instance per execution;
* append-only Workflow Events;
* rebuildable current-state projections;
* durable ready-step queues;
* durable timers;
* durable approval requests;
* operation-bound capabilities;
* step-attempt leases with fencing tokens;
* idempotency keys;
* side-effect intents;
* result and reconciliation records;
* periodic Checkpoint Snapshots;
* transactional outbox records;
* explicit compensation plans;
* and startup recovery.

The selected execution model should be:

```text
Versioned Workflow Definition
    ↓
Deterministic validation and compilation
    ↓
Immutable Compiled Workflow Plan
    ↓
Workflow Instance and initial event
    ↓
Transactional ready-step scheduling
    ↓
Fenced worker lease
    ↓
Step activity execution
    ↓
Result, side-effect or ambiguity record
    ↓
Workflow event and projection update
    ↓
Next ready steps, wait state or terminal state
```

The authoritative workflow history should be an append-only event journal.

The current workflow and step tables should be projections.

Checkpoint Snapshots should accelerate recovery but should not become the sole authority.

A snapshot should contain:

* workflow state;
* active plan revision;
* completed step outputs by safe reference;
* outstanding dependencies;
* active waits;
* timers;
* approval state;
* compensation state;
* and last applied event sequence.

A snapshot should not contain:

* API keys;
* Vault values;
* arbitrary process handles;
* live `CancellationToken` objects;
* open database connections;
* worker memory;
* provider-side hidden state;
* model hidden reasoning;
* unmanaged pointers;
* temporary credentials;
* or an unserialisable object graph.

The initial persistence architecture should use:

* one service-owned Workflow SQLite database per Opure release channel;
* a hard `project_id` or explicit `system` scope on every workflow;
* SQLite WAL mode on a local fixed disk;
* `PRAGMA synchronous=FULL` for durable workflow-control commits;
* explicit transactions;
* one logical writer;
* bounded read transactions;
* content-addressed storage for large workflow artefacts;
* and no network filesystem.

The database should atomically commit:

* the Workflow Event;
* current projection changes;
* ready-step queue changes;
* timers;
* approval waits;
* lease or attempt state;
* side-effect intent metadata;
* and transactional outbox records

when they belong to one Workflow Service transition.

A workflow step should never be dispatched before the scheduling transition is durable.

A worker result should never be accepted without:

* the exact Workflow Instance;
* exact Compiled Plan;
* exact step;
* exact attempt;
* current fencing token;
* output classification;
* output hash;
* and result validation.

The initial delivery semantics should be described honestly:

> **Workflow step activities may execute more than once. Opure provides at-least-once scheduling and uses idempotency, effect reconciliation, fencing and human review to prevent or contain duplicate side effects. Opure does not claim universal exactly-once execution across external systems.**

The initial Side-Effect Classes should be:

1. Pure Deterministic
2. Pure Nondeterministic Read
3. Local Read
4. External Read
5. Transactional Internal Write
6. Idempotent Write
7. Write with External Idempotency Key
8. Reconciliable Write
9. Compensatable Write
10. Irreversible Write
11. Human Action
12. Unknown or Prohibited

Every step that can change state outside the Workflow database should declare one class.

A step whose class is Unknown or Prohibited should not run.

Every logical side effect should have a stable **Effect Identity**.

Suggested derivation:

```text
workflow instance
+ step instance
+ effect slot
+ plan revision
+ logical effect generation
```

A retry of the same logical effect should use the same idempotency key.

A deliberate new effect should use a new effect generation.

Attempt IDs should identify executions.

They should not replace logical effect identity.

The initial side-effect protocol should be:

```text
Validate step capability
    ↓
Persist Side-Effect Intent
    ↓
Commit
    ↓
Invoke trusted service or approved external system
    ↓
Receive result
    ↓
Persist Side-Effect Completion or Failure
    ↓
Commit workflow transition
```

A crash may occur after an external effect succeeds but before Opure records success.

This creates an ambiguity window.

Recovery should resolve the ambiguity through one of:

* external idempotency-key lookup;
* trusted-service operation lookup;
* resource-state reconciliation;
* provider request receipt;
* repository state comparison;
* workspace snapshot comparison;
* package-installation state;
* or explicit developer review.

When a non-idempotent effect cannot be reconciled, the workflow should enter:

```text
Failed — Outcome Unknown
```

or:

```text
Recovery Required — Outcome Unknown
```

according to policy.

Opure should not retry such an effect automatically.

The initial workflow activity policy should require each activity to declare:

* input schema;
* output schema;
* side-effect class;
* idempotency behaviour;
* reconciliation behaviour;
* retry policy;
* timeout;
* cancellation behaviour;
* compensation;
* required capability;
* allowed services;
* data classification;
* resource profile;
* implementation revision;
* and recovery policy.

Activities should be small enough to checkpoint meaningful progress but large enough to represent one coherent unit of work.

A long operation may emit durable progress events.

Progress should not imply success.

The initial workflow states should be:

* Created;
* Validating;
* Ready;
* Running;
* Waiting for Approval;
* Waiting for External Event;
* Waiting for Timer;
* Waiting for Resource;
* Pausing;
* Paused;
* Cancelling;
* Compensating;
* Completed;
* Completed with Warnings;
* Failed;
* Failed — Outcome Unknown;
* Cancelled;
* Terminated;
* Quarantined;
* Migrating;
* Recovery Required;
* Archived;
* and Purged.

The initial step states should be:

* Pending;
* Blocked;
* Ready;
* Leased;
* Running;
* Waiting;
* Succeeded;
* Failed Retryable;
* Failed Terminal;
* Cancel Requested;
* Cancelled;
* Outcome Unknown;
* Skipped;
* Compensation Pending;
* Compensating;
* Compensated;
* Compensation Failed;
* Quarantined;
* and Purged.

The initial activity-attempt states should be:

* Scheduled;
* Leased;
* Started;
* Heartbeating;
* Cancel Requested;
* Completed;
* Failed;
* Timed Out;
* Lease Lost;
* Worker Lost;
* Outcome Unknown;
* and Reconciled.

The initial state-machine policy should:

* validate every transition;
* append one event for every transition;
* reject impossible transitions;
* require expected instance revision;
* use optimistic concurrency;
* use idempotency keys for commands;
* and make duplicate commands return the original authoritative result where safe.

The initial workflow-definition format should be declarative data.

It should support:

* sequence;
* parallel branches;
* join;
* condition;
* bounded loop;
* child workflow;
* activity;
* human approval;
* external signal;
* durable timer;
* retry;
* compensation;
* explicit failure;
* and explicit completion.

It should not support:

* arbitrary script;
* arbitrary reflection;
* arbitrary C# delegate serialisation;
* dynamic assembly loading;
* model-generated executable expressions;
* unbounded recursion;
* unbounded loop;
* or runtime download of workflow code.

Condition expressions should use a small typed deterministic expression language.

Conditions should read only:

* immutable workflow inputs;
* completed step outputs;
* explicit signals;
* approved project-state references;
* and deterministic workflow metadata.

Conditions should not read:

* current clock directly;
* random values;
* process environment;
* ambient filesystem;
* provider state;
* network;
* mutable global variables;
* or model output that has not been recorded as a validated step result.

Time should enter through a durable timer or recorded Clock Activity.

Randomness should enter through a recorded Random Activity with an explicit seed and output.

Identifiers should be generated through a recorded Identifier Activity when the value must survive replay.

The initial Compiled Workflow Plan should bind:

* Workflow Definition revision;
* activity contracts;
* activity implementation revisions;
* expression-language revision;
* input and output schemas;
* project scope;
* required capabilities;
* allowed side effects;
* retry policies;
* timeout policies;
* compensation graph;
* approval policies;
* resource policies;
* Context Policies;
* Routing Policies;
* Plugin and MCP package identities;
* and plan SHA-256.

A running instance should pin one Compiled Plan.

It should not silently adopt a later Workflow Definition, activity contract or retry policy.

The initial workflow-versioning policy should be:

> **Running workflows continue on the exact Compiled Plan they started with. A new product release must either retain compatible execution support for that plan or require an explicit reviewed Workflow Migration Plan.**

A Workflow Migration Plan should contain:

* source plan;
* target plan;
* eligible instance states;
* event-history preconditions;
* step mapping;
* output mapping;
* wait mapping;
* timer mapping;
* approval mapping;
* compensation mapping;
* capability changes;
* provider and model changes;
* source-state revalidation;
* rollback;
* and human approval when authority or side effects change.

The workflow engine should not replay arbitrary orchestration code against old history.

This avoids requiring nondeterministic application methods to behave identically after a software update.

The initial checkpointing policy should be:

* every authoritative transition is committed;
* a compact snapshot is produced after a configurable event count;
* a snapshot is also produced at major wait or terminal boundaries;
* large outputs remain in CAS;
* event sequence and hashes bind the snapshot;
* and replay validates the snapshot before applying later events.

Suggested initial snapshot triggers:

* every 100 workflow events;
* before waiting for approval;
* before waiting for an external event longer than one minute;
* before a workflow is paused;
* after a compensation batch;
* at terminal state;
* and before an explicit version migration.

These values require testing.

The initial Workflow Event should include:

```text
event_id
workflow_instance_id
project_id
instance_sequence
event_type
plan_revision
step_instance_id
attempt_id
actor
operation_id
occurred_at_utc
payload_reference
payload_sha256
previous_event_sha256
event_sha256
```

The event hash chain should provide integrity evidence.

It should not be described as a cryptographically trusted external audit ledger.

A same-user attacker able to rewrite the entire database can also rewrite the chain.

The initial step-attempt lease should include:

* workflow instance;
* step instance;
* attempt;
* worker identity;
* worker process identity;
* lease owner token;
* fencing token;
* acquired time;
* heartbeat time;
* expiry time;
* expected instance revision;
* and state.

Lease acquisition should occur transactionally.

Every new lease should increment a monotonic fencing token for the step.

A stale worker result with an older fencing token should be rejected.

Lease expiry should not prove the side effect did not occur.

It should only allow recovery to start.

The initial worker-recovery policy should distinguish:

* worker definitely not started;
* worker started with no side effect;
* side effect intent committed but invocation not confirmed;
* invocation attempted with receipt;
* result received but not committed;
* and side-effect outcome unknown.

Recovery decisions should depend on side-effect class.

The initial retry policy should support:

* maximum attempts;
* initial delay;
* backoff factor;
* maximum delay;
* deterministic jitter;
* retryable failure classes;
* non-retryable failure classes;
* overall retry budget;
* and deadline.

Deterministic jitter should be derived from:

* Workflow Instance;
* step;
* and attempt

so recovery does not produce a different schedule.

The initial non-retryable failures should include:

* policy denial;
* invalid input;
* schema failure;
* authentication failure requiring user action;
* secret or protected-data violation;
* wrong project;
* stale approval;
* changed source when exact source is required;
* unsupported activity version;
* explicit cancellation;
* irreversible effect with unknown outcome;
* and terminal user rejection.

The initial timeout model should distinguish:

* queue timeout;
* lease-acquisition timeout;
* activity-start timeout;
* execution timeout;
* heartbeat timeout;
* provider timeout;
* tool timeout;
* approval expiry;
* signal expiry;
* workflow deadline;
* and compensation timeout.

Timeout should be a durable event.

A timeout should not be represented only by a cancelled in-memory task.

The initial cancellation model should be cooperative first.

A cancellation request should be persisted.

Pending work should stop becoming Ready.

Active attempts should receive a `CancellationToken`.

Cancellation-aware activities should:

* observe it;
* stop at safe points;
* release resources;
* preserve partial results safely;
* report whether a side effect occurred;
* and acknowledge cancellation.

The design should distinguish:

* cancel the operation;
* cancel the wait;
* and cancel both.

If an isolated worker does not stop within the grace period, the supervisor may terminate the worker process.

Forced process termination does not establish that an external effect did not occur.

The workflow must reconcile the effect.

The initial pause model should pause only at durable safe points by default.

An immediate pause request may behave like cancellation of the current wait while allowing the current atomic activity to finish.

The UI should state the chosen behaviour.

The initial human-approval model should use a durable Approval Request containing:

* exact workflow and step;
* exact plan revision;
* exact preview;
* exact operation;
* exact files, commands, providers or tools;
* data classification;
* risk;
* estimated cost;
* expiry;
* approving role;
* and approval capability.

Approval should bind the exact reviewed operation.

A changed patch, command, Context Plan, Routing Decision, provider, model, tool, source hash or side-effect intent should invalidate the approval.

Approval and rejection should be append-only events.

A human wait may survive:

* Runtime restart;
* Desktop close;
* Windows restart;
* and product update

provided the plan remains supported.

The initial external-signal model should use typed Signal Definitions.

Every signal should include:

* signal ID;
* signal type;
* target workflow;
* target wait;
* sender;
* sender capability;
* payload schema;
* payload hash;
* sequence;
* deduplication key;
* received time;
* expiry;
* and classification.

Signals should have at-least-once handling semantics.

Duplicate signals should be deduplicated by explicit identity.

Signals arriving before the wait should be persisted in a bounded inbox rather than discarded.

Unexpected signals should be rejected or quarantined.

The initial durable-timer model should persist:

* timer ID;
* target workflow;
* target step;
* due instant;
* created instant;
* clock source;
* purpose;
* status;
* and firing event.

Timers should be scheduled from UTC instants.

Live waiting may use a monotonic clock.

After restart, due timers should be discovered from durable state.

A system-clock change should not duplicate a timer firing because the Timer Fired transition is idempotent.

The initial parallel-execution model should:

* permit independent Ready steps;
* cap per-workflow parallelism;
* cap project parallelism;
* cap activity-class parallelism;
* use resource reservations;
* prevent incompatible workspace writes;
* prevent two patch applications to the same base;
* and preserve deterministic join semantics.

A join should define:

* All;
* Any;
* Quorum;
* First Successful;
* or Explicit Condition.

Cancellation of losing branches should be explicit.

Parallel branch completion order should be recorded.

The resulting state transition should not depend on thread scheduling beyond the declared join policy.

The initial repository and workspace write policy should require:

* exact Workspace Snapshot;
* exact Repository State;
* write capability;
* logical resource lock;
* fencing token;
* staged write;
* validation;
* and receipt.

A workflow should not hold a database transaction open while:

* a model runs;
* a tool runs;
* a build runs;
* a test runs;
* an approval waits;
* a network call runs;
* or a child process runs.

The initial compensation model should be explicit and honest.

Compensation is not database rollback.

A Compensation Definition should include:

* triggering failures;
* completed effect prerequisites;
* reverse order or dependency graph;
* compensation activity;
* idempotency;
* timeout;
* retry;
* verification;
* and manual fallback.

Compensation should be recorded as new forward events.

It should not erase the original side effect.

Compensation itself can:

* succeed;
* fail;
* time out;
* be rejected;
* become outcome unknown;
* or require human remediation.

An irreversible activity should have no pretend compensation.

The workflow should show:

```text
Manual Remediation Required
```

where appropriate.

The initial child-workflow model should:

* create an independent child instance;
* bind parent and child;
* define input and output;
* define cancellation propagation;
* define failure propagation;
* define compensation ownership;
* and avoid shared mutable local state.

A parent may wait for a child.

A child history should remain separate.

The initial workflow data policy should store:

* bounded typed inputs;
* bounded typed outputs;
* hashes;
* safe references;
* classifications;
* and receipts.

Large data should use CAS.

Secrets should use Vault references only.

A step should retrieve a secret through an operation-bound capability at execution time.

A secret should not become:

* workflow input;
* event payload;
* checkpoint;
* CAS artefact;
* log;
* output;
* retry payload;
* approval preview;
* or Trust Centre content.

The initial AI-step policy should model each AI call as an ordinary durable activity with explicit:

* Context Plan;
* Routing Request;
* Routing Decision;
* Data Sharing Plan where remote;
* exact AI Execution Profile;
* request receipt;
* response receipt;
* output schema;
* cancellation;
* and retry behaviour.

The workflow should not resume a provider-side hidden conversation.

A retried AI step should be a new provider request attempt under the same logical step.

A stochastic response may differ.

Therefore, retry policy should state whether:

* another response is acceptable;
* exact response reuse is required;
* human review is required;
* or the step must fail.

The initial tool-step policy should route every tool operation through the Tool Mediator.

The initial plugin-step policy should pin:

* package;
* version;
* package hash;
* capability;
* contract revision;
* and result schema.

The initial MCP-step policy should pin:

* server fingerprint;
* account;
* tool or resource;
* request schema;
* data-sharing approval;
* and result classification.

A changed plugin package or MCP server fingerprint should pause recovery or require replanning.

The initial recovery process on Workflow Service start should:

1. open and recover SQLite;
2. run fast integrity checks;
3. validate schema and migration state;
4. verify active event-chain tails;
5. rebuild inconsistent projections;
6. discover non-terminal instances;
7. expire stale in-process leases conservatively;
8. inspect worker processes through the supervisor;
9. reconcile active or ambiguous attempts;
10. requeue safe idempotent steps;
11. restore durable timers;
12. restore approval and signal waits;
13. validate pinned plans and activity implementations;
14. validate project and capability state;
15. publish Recovery Reports;
16. and resume only eligible workflows.

Recovery should be automatic only where the next action is safe.

Otherwise, the workflow should pause with a clear reason.

The initial recovery classifications should be:

* Safe to Resume;
* Safe to Retry;
* Needs Reconciliation;
* Needs Fresh Approval;
* Needs Replanning;
* Implementation Unavailable;
* Source Changed;
* Project Unavailable;
* Credential Required;
* Outcome Unknown;
* Corrupted;
* and Manual Recovery Required.

The selected trust chain is:

```text
Reviewed Workflow Definition
    ↓
Immutable Compiled Plan
    ↓
Durable instance creation
    ↓
Transactional event and ready queue
    ↓
Fenced activity lease
    ↓
Capability and source revalidation
    ↓
Persisted side-effect intent
    ↓
Activity execution
    ↓
Idempotency, receipt or reconciliation
    ↓
Durable result event and projection
    ↓
Checkpoint Snapshot
    ↓
Visible wait, completion, failure or recovery state
```

---

## 2. Status

Current status:

> **Proposed**

This ADR becomes **Accepted** only after prototypes demonstrate:

* declarative Workflow Definitions;
* deterministic plan compilation;
* immutable Compiled Plans;
* one Workflow SQLite database per channel;
* project-scope enforcement;
* WAL with `synchronous=FULL`;
* append-only Workflow Events;
* event hash chains;
* rebuildable projections;
* durable ready queues;
* durable timers;
* durable approval waits;
* typed external signals;
* deduplication;
* checkpoint snapshots;
* content-addressed outputs;
* step-attempt leases;
* fencing tokens;
* stale-worker result rejection;
* activity contracts;
* side-effect classification;
* idempotency keys;
* side-effect intents;
* internal transactional writes;
* external idempotency;
* effect reconciliation;
* explicit Outcome Unknown;
* bounded retries;
* deterministic backoff jitter;
* cooperative cancellation;
* forced worker termination;
* compensation;
* manual remediation;
* parallel branches;
* child workflows;
* version pinning;
* Workflow Migration Plans;
* AI, tool, plugin and MCP steps;
* startup recovery;
* corruption recovery;
* Windows restart recovery;
* project close and reopen;
* product update;
* and adversarial duplicate-effect, crash-window, stale-lease, stale-approval, changed-source, wrong-project, secret, malicious-signal, retry-storm and version-drift tests.

---

## 3. Context

Opure workflows may run for:

* milliseconds;
* minutes;
* hours;
* days;
* or longer while waiting for a human decision.

A workflow may coordinate:

* repository reads;
* project search;
* model inference;
* tool calls;
* patch generation;
* patch review;
* file writes;
* builds;
* tests;
* package operations;
* plugin calls;
* MCP calls;
* and external provider operations.

The Runtime may stop because of:

* application close;
* application crash;
* worker crash;
* Windows restart;
* power loss;
* product update;
* provider outage;
* network loss;
* model-runtime failure;
* disk pressure;
* or user cancellation.

The workflow must not forget whether it:

* sent a remote request;
* applied a patch;
* ran a command;
* created a resource;
* asked for approval;
* received approval;
* or began compensation.

A naïve in-memory `Task` chain cannot survive process termination.

A naïve “save the current JSON object” approach can lose:

* event order;
* side-effect intent;
* retry identity;
* approval provenance;
* and ambiguity.

A code-replay workflow engine can reconstruct local state, but it imposes deterministic-code restrictions and creates versioning hazards when orchestration code changes.

A generic cloud workflow service would weaken:

* local-first operation;
* offline behaviour;
* project control;
* provider neutrality;
* and inspectability.

External activity delivery is inherently vulnerable to a failure window:

1. the activity performs a side effect;
2. the worker crashes;
3. the completion record is not committed;
4. recovery cannot know whether the effect occurred.

Therefore, exactly-once execution cannot be promised universally.

The architecture must make ambiguity visible and manage it through:

* idempotency;
* receipts;
* reconciliation;
* compensation;
* or human review.

---

## 4. Problem Statement

Opure requires a local-first durable workflow architecture that can checkpoint, pause, resume, retry, cancel, compensate and recover after crashes or product updates while preserving project scope, human approvals, source and policy validity, external side-effect safety, version compatibility and complete developer-visible evidence.

---

## 5. Decision Drivers

The decision is evaluated against:

* Charter alignment;
* local-first operation;
* offline recovery;
* human control;
* side-effect safety;
* crash consistency;
* power-loss durability;
* project isolation;
* source validity;
* secret safety;
* provider neutrality;
* workflow longevity;
* cancellation;
* retries;
* approvals;
* external signals;
* timers;
* parallelism;
* compensation;
* versioning;
* product updates;
* testability;
* operational simplicity;
* and small-team implementation.

---

## 6. Governing Principles

This decision must preserve:

* Developer Respect;
* Human in Control;
* Local by Design;
* Visible by Design;
* Inspectable Decisions;
* Workflow Plans, Not Autonomous Minds;
* Durable Before Dispatch;
* Events Before Projections;
* Checkpoints Are Derived;
* Exact Plan Pinning;
* At-Least-Once Honesty;
* Idempotency by Design;
* Reconcile Ambiguity;
* Never Guess External Outcome;
* No Hidden Retry;
* No Hidden Fallback;
* No Hidden Approval Reuse;
* No Secret Checkpoint;
* No Provider-Side Workflow State;
* No Arbitrary Orchestrator Code Replay;
* No Database Transaction Across External Work;
* No Universal Rollback Claim;
* Cooperative Cancellation;
* Fenced Workers;
* Bounded Retry;
* Explicit Compensation;
* Manual Remediation When Required;
* Rebuildable State;
* and Evidence-Based Recovery.

---

## 7. Scope

This ADR decides:

* Workflow Service ownership;
* Workflow Definitions;
* Compiled Plans;
* Workflow Instances;
* steps and attempts;
* event journals;
* projections;
* checkpoints;
* SQLite durability;
* ready queues;
* leases;
* fencing;
* scheduling;
* retries;
* timeouts;
* cancellation;
* pauses;
* approvals;
* external signals;
* timers;
* parallel branches;
* child workflows;
* side-effect classes;
* idempotency;
* reconciliation;
* compensation;
* versioning;
* migration;
* AI steps;
* tool steps;
* plugin steps;
* MCP steps;
* data retention;
* startup recovery;
* Trust Centre;
* and acceptance tests.

This ADR does not decide:

* final visual workflow designer;
* final end-user automation language;
* cloud-hosted workflow execution;
* multi-machine distributed scheduling;
* clustered leader election;
* organisation-wide workflow sharing;
* arbitrary user scripts;
* arbitrary C# orchestration code;
* CRDT workflow state;
* provider-side agents;
* or universal exactly-once external effects.

---

## 8. Constraints

Known constraints include:

* ADR-0003 selected a supervised hybrid process topology.
* ADR-0005 selected service-owned SQLite, explicit transactions and outbox/inbox patterns.
* ADR-0006 selected structured local-first observability.
* ADR-0007 keeps secrets in the Vault.
* ADR-0009 controls paths, file identity and staged writes.
* ADR-0018 and ADR-0019 govern tools, MCP and provider data sharing.
* ADR-0020 governs local inference execution.
* ADR-0021 makes context explicit and immutable.
* ADR-0023 makes project memory explicit and revision bound.
* ADR-0024 makes model routing and fallback explicit.
* SQLite permits one simultaneous writer per database.
* WAL requires all processes to use the same host.
* SQLite `synchronous=NORMAL` in WAL mode may lose the last committed transactions after power loss.
* SQLite `synchronous=FULL` adds a WAL sync after each transaction to improve power-loss durability.
* .NET cancellation is cooperative.
* durable-activity systems commonly provide at-least-once activity execution;
* external events may be duplicated;
* deterministic code replay requires orchestration version discipline;
* and no local database transaction can atomically commit a write in an unrelated external system.

---

## 9. Assumptions

This decision assumes:

* Workflow Definitions can be expressed declaratively;
* activities can have stable typed contracts;
* trusted services can accept operation IDs and idempotency keys;
* external providers expose receipts or resource-state lookup for some operations;
* the Runtime supervisor can identify and terminate workers;
* the Workflow Service can remain the single database writer;
* the Scheduler can reserve CPU, RAM, GPU and service concurrency;
* CAS can store large artefacts;
* approval capabilities can bind exact operations;
* project and source state can be revalidated;
* and old Compiled Plan support can be retained for active instances or migrated explicitly.

---

## 10. Current Technical Evidence

Official and primary documentation available on 18 July 2026 establishes that:

* Microsoft Durable Task persists execution history and replays orchestration state after unloading or restart;
* replay-based orchestrators require deterministic logic;
* changed orchestrator logic can cause nondeterminism unless versioned;
* Durable Task activities are at-least-once and should be idempotent;
* durable external events may be delivered more than once and therefore require deduplication identities;
* external-event waits can survive worker unload;
* .NET cancellation is cooperative and requires the operation to observe the token;
* cancellation of an operation and cancellation of waiting for it are distinct design choices;
* SQLite transactions are atomic within one database;
* SQLite permits multiple readers but one writer;
* WAL operates on one host and supports concurrent readers and a writer;
* WAL mode with `synchronous=FULL` performs an additional sync after each transaction;
* WAL mode with `synchronous=NORMAL` remains consistent but can lose a recent transaction after power loss;
* and SQLite performs crash recovery when a database with journal state is reopened.

These facts support:

* durable event history;
* deterministic declarative orchestration;
* activity idempotency;
* external-event deduplication;
* cooperative cancellation;
* one local SQLite workflow-control database;
* and explicit power-loss durability.

They do not require Opure to adopt Microsoft Durable Task, Azure Functions, Temporal or another external workflow engine.

Every SQLite build, .NET runtime and integrated service contract must be revalidated before acceptance.

---

## 11. Terminology

### 11.1 Workflow Definition

A versioned declarative workflow source.

---

### 11.2 Compiled Workflow Plan

An immutable validated executable representation of one Workflow Definition revision.

---

### 11.3 Workflow Instance

One durable execution of one Compiled Plan.

---

### 11.4 Step Definition

A node in the Workflow Definition.

---

### 11.5 Step Instance

The runtime identity of one step within one workflow execution.

---

### 11.6 Activity Contract

A typed trusted interface for one unit of work.

---

### 11.7 Activity Attempt

One execution attempt of one Step Instance.

---

### 11.8 Workflow Event

An append-only authoritative state transition or evidence record.

---

### 11.9 Projection

A rebuildable current view derived from Workflow Events and authoritative records.

---

### 11.10 Checkpoint Snapshot

A derived compact state used to accelerate recovery.

---

### 11.11 Ready Queue

Durable records for steps whose dependencies and policies permit execution.

---

### 11.12 Lease

A time-bounded assignment of one step attempt to one worker.

---

### 11.13 Fencing Token

A monotonic token that prevents a stale worker from committing a result.

---

### 11.14 Logical Effect

One intended externally visible side effect.

---

### 11.15 Effect Identity

The stable identity of one Logical Effect across retries.

---

### 11.16 Side-Effect Intent

A durable record created before an external effect is attempted.

---

### 11.17 Idempotency Key

A stable key allowing repeated requests to represent one Logical Effect.

---

### 11.18 Reconciliation

A trusted check that determines whether an ambiguous effect occurred and what its result was.

---

### 11.19 Outcome Unknown

A state where Opure cannot safely determine whether an effect occurred.

---

### 11.20 Compensation

A separate forward action intended to mitigate or reverse a completed effect.

---

### 11.21 Approval Request

A durable request for a human decision about one exact operation.

---

### 11.22 External Signal

A typed idempotent input delivered to a waiting workflow.

---

### 11.23 Durable Timer

A persisted future wake-up condition.

---

### 11.24 Workflow Migration Plan

An explicit mapping from one pinned Compiled Plan to another.

---

### 11.25 Recovery Report

Developer-visible evidence describing restart analysis and decisions.

---

## 12. Options Considered

The principal architecture options are:

1. Declarative state-machine interpreter with event journal and checkpoints.
2. Embedded replay-based durable orchestration framework.
3. Azure Durable Task or Durable Functions service.
4. Temporal server.
5. In-memory `Task` chains with periodic JSON save.
6. Database row state machine without event journal.
7. Event journal without projection or snapshots.
8. Arbitrary C# workflow code with serialised locals.
9. Provider-side agent or conversation state.
10. Plugin-owned workflows.
11. MCP-server-owned workflows.
12. Shell-script workflows.

---

## 13. Option A — Declarative Interpreter and Event Journal

### 13.1 Advantages

* local first;
* provider neutral;
* explicit state;
* no arbitrary-code replay;
* exact plan versioning;
* simple project-policy integration;
* explicit side effects;
* inspectable checkpoints;
* recoverable projections;
* and bounded first-party trust surface.

### 13.2 Disadvantages

* substantial implementation;
* custom workflow language;
* custom migration model;
* scheduler and recovery complexity;
* and no mature distributed cluster semantics.

### 13.3 Decision

Selected.

---

## 14. Embedded Replay-Based Framework

Deferred.

Replay systems provide strong durable-execution concepts but require deterministic orchestration code and careful versioning.

Opure prefers a declarative plan whose transitions are interpreted explicitly.

A future framework may implement lower-level scheduling if it can preserve the selected contracts and local-first requirements.

---

## 15. Azure Durable Task or Durable Functions

Rejected as the primary runtime because Opure must work locally and offline without Azure infrastructure.

The concepts remain useful evidence.

---

## 16. Temporal Server

Deferred because it introduces another persistent service, deployment and operational boundary disproportionate to the first local desktop release.

---

## 17. In-Memory Tasks and JSON Save

Rejected because it cannot close side-effect ambiguity windows or provide durable scheduling and event provenance.

---

## 18. Row State Machine Without Events

Rejected because state history, recovery diagnosis and projection rebuild would be incomplete.

---

## 19. Events Without Projection

Rejected because ordinary scheduling and UI need efficient current state.

---

## 20. Arbitrary C# Orchestration Replay

Rejected because product upgrades and nondeterministic APIs create version and replay hazards.

Activities remain implemented in C# behind stable contracts.

---

## 21. Provider-Side Workflow State

Rejected because Opure must remain the system of record.

---

## 22. Plugin- or MCP-Owned Workflow State

Rejected because external components do not own project authority or recovery.

---

## 23. Shell Scripts

Rejected as the workflow model because scripts are difficult to type, constrain, checkpoint, migrate and recover safely.

Trusted activities may invoke explicitly approved commands through Tool Mediation.

---

## 24. Decision

Opure will provisionally adopt:

> **A local declarative durable-workflow interpreter whose immutable Compiled Plans execute through service-owned SQLite event journals, transactional ready queues, fenced worker leases, durable timers and approvals, versioned activity contracts, idempotency and effect reconciliation, periodic derived checkpoints, explicit retries, cooperative cancellation, compensation and human recovery, while running instances remain pinned to their plan, secrets and hidden provider state are excluded, external effects are never described as universally exactly once, and any unreconciled non-idempotent crash window stops as Outcome Unknown rather than being retried or guessed.**

This decision is:

* [ ] Permanent until superseded
* [x] Provisional pending crash, effect, versioning and recovery evidence
* [ ] Approval of arbitrary C# orchestration replay
* [ ] Approval of cloud-hosted workflow authority
* [ ] Approval of multi-machine distributed scheduling
* [ ] Approval of universal exactly-once external effects

---

# 25. Workflow Service Ownership

The Workflow Service owns:

* Workflow Definition registration;
* Definition validation;
* plan compilation;
* plan identity;
* Workflow Instance identity;
* Workflow Events;
* projections;
* checkpoints;
* ready-step scheduling;
* durable timers;
* durable approval waits;
* external signal inboxes;
* activity attempts;
* leases;
* fencing tokens;
* retries;
* deadlines;
* cancellations;
* pauses;
* compensations;
* child-workflow coordination;
* workflow migration;
* recovery;
* retention;
* and workflow-use evidence.

---

## 25.1 Non-Responsibilities

The Workflow Service does not own:

* project files;
* repository authority;
* model routing;
* Context Assembly;
* provider credentials;
* local model processes;
* command execution;
* patch application;
* build execution;
* test execution;
* plugin private state;
* MCP server state;
* or user authentication.

It coordinates the trusted owners through capabilities and receipts.

---

# 26. Service Boundary

Conceptual architecture:

```text
Desktop, API or Product Command
    ↓
Runtime Gateway
    ↓
Workflow Service
    ├── Definition Registry
    ├── Plan Compiler
    ├── Instance State Machine
    ├── Event Journal
    ├── Projection Builder
    ├── Ready Queue
    ├── Timer Scheduler
    ├── Approval Manager
    ├── Signal Inbox
    ├── Lease Manager
    ├── Retry Manager
    ├── Compensation Manager
    ├── Migration Manager
    └── Recovery Manager
        ↓
Scheduler and Supervised Workers
        ↓
Trusted Activity Adapters
        ↓
Workspace, Repository, Patch, Build, Test,
Context, AI Router, Local Runtime, Tool Mediator,
Plugin Gateway, MCP Gateway and Network Gateway
```

---

## 26.1 No Direct Activity Database Access

Activity workers do not read or write Workflow tables directly.

---

## 26.2 No Direct Desktop Mutation

Desktop issues commands through the Runtime Gateway.

---

## 26.3 No Direct Model Mutation

A model may propose a workflow or next action.

Trusted code validates and records it.

---

## 26.4 No Ambient Service Discovery

A Compiled Plan contains exact allowed activity contracts.

---

# 27. Workflow Database

Suggested file:

```text
%LOCALAPPDATA%\Opure\<Channel>\Workflow\workflow.db
```

Associated directories:

```text
Workflow\
├── workflow.db
├── artefacts\
├── definitions\
├── reports\
├── staging\
├── quarantine\
└── recovery\
```

---

## 27.1 Service Ownership

Only Workflow Service writes the database.

---

## 27.2 Release Channel

Stable, Preview and Development use separate databases.

---

## 27.3 Local Fixed Disk

Required.

---

## 27.4 Network Filesystem

Denied because WAL requires same-host shared state and network durability is outside the selected evidence.

---

## 27.5 Cloud-Synchronised Directory

Denied.

---

## 27.6 Repository Directory

Denied.

---

# 28. SQLite Configuration

Suggested initial settings:

```sql
PRAGMA journal_mode = WAL;
PRAGMA synchronous = FULL;
PRAGMA foreign_keys = ON;
PRAGMA busy_timeout = <bounded>;
PRAGMA trusted_schema = OFF;
```

Exact supported pragmas require pinned-build validation.

---

## 28.1 Why `FULL`

Workflow transitions determine whether external work may run.

The last committed scheduling, approval, cancellation or side-effect-intent transaction should survive an ordinary power loss under the selected filesystem assumptions.

---

## 28.2 No `OFF`

Prohibited.

---

## 28.3 `NORMAL`

May be acceptable for rebuildable caches.

Not selected for authoritative workflow control.

---

## 28.4 WAL Checkpointing

Run bounded checkpoints through Workflow Service maintenance.

---

## 28.5 Checkpoint Starvation

Long read transactions are prohibited.

---

## 28.6 Single Writer

Use one logical write coordinator.

---

# 29. Database Transaction Boundary

One state transition transaction may include:

* expected-instance revision check;
* event append;
* projection update;
* step state update;
* ready-queue insert or delete;
* timer insert or delete;
* signal consumption;
* approval update;
* lease update;
* effect-intent update;
* outbox insert;
* and workflow revision increment.

---

## 29.1 No External Call in Transaction

Commit before dispatch.

---

## 29.2 No Long-Running Validation in Transaction

Validate immutable inputs before beginning where possible.

Revalidate critical expected state inside.

---

## 29.3 `BEGIN IMMEDIATE`

The writer may use `BEGIN IMMEDIATE` for predictable write acquisition.

---

## 29.4 Busy Handling

Bounded retry with cancellation and diagnostics.

---

# 30. Suggested Tables

```text
workflow_definitions
workflow_definition_revisions
compiled_workflow_plans
workflow_instances
workflow_events
workflow_current
workflow_inputs
workflow_outputs
workflow_steps
workflow_step_dependencies
workflow_step_current
workflow_attempts
workflow_leases
workflow_ready_queue
workflow_timers
workflow_signal_definitions
workflow_signal_inbox
workflow_signal_consumption
workflow_approval_requests
workflow_approval_decisions
workflow_effects
workflow_effect_intents
workflow_effect_results
workflow_reconciliations
workflow_retry_schedules
workflow_compensations
workflow_child_links
workflow_checkpoints
workflow_migrations
workflow_capabilities
workflow_outbox
workflow_inbox
workflow_recovery_reports
workflow_integrity_results
workflow_archives
workflow_tombstones
```

Large payloads use CAS references.

---

# 31. Workflow Definition

Suggested schema:

```text
opure.workflow-definition/1
```

---

## 31.1 Fields

```text
definition_id
revision
name
description
owner
scope_policy
input_schema
output_schema
steps
dependencies
conditions
loops
joins
approval_policies
retry_policies
timeout_policies
compensation_policies
resource_policies
retention_policy
created_at
definition_sha256
```

---

## 31.2 Source

First-party definitions may be source controlled.

User-created definitions remain in trusted app data until an explicit export.

---

## 31.3 Immutable Published Revision

Required.

---

## 31.4 Draft Revision

May change before publication.

---

## 31.5 No Runtime Code Field

A definition cannot embed:

* C#;
* PowerShell;
* JavaScript;
* Python;
* SQL;
* shell;
* binary;
* or assembly bytes.

---

# 32. Step Definition

Fields:

```text
step_id
step_kind
activity_contract
activity_revision
input_mapping
output_mapping
dependencies
condition
retry_policy
timeout_policy
cancellation_policy
side_effect_policy
compensation_policy
approval_policy
resource_policy
data_policy
failure_policy
```

---

## 32.1 Stable Step ID

Within one Definition lineage.

---

## 32.2 Display Name

Separate from ID.

---

## 32.3 No Duplicate Step ID

Compile failure.

---

# 33. Step Kinds

Initial Step Kinds:

* Activity;
* Condition;
* Parallel Fork;
* Join;
* Bounded Loop;
* Child Workflow;
* Human Approval;
* External Signal Wait;
* Durable Timer;
* Explicit Failure;
* Explicit Completion;
* and No-Op Marker.

---

## 33.1 Future Kinds

Require schema and interpreter revision.

---

## 33.2 Unknown Kind

Compile failure.

---

# 34. Workflow Input

A Workflow Input is:

* typed;
* schema validated;
* classified;
* bounded;
* immutable after start;
* and hashed.

---

## 34.1 Source References

Prefer safe references over full source content.

---

## 34.2 Secret Reference

Opaque Vault reference only.

---

## 34.3 Project

Required unless the workflow is explicitly System scoped.

---

# 35. Workflow Output

The final output should be:

* typed;
* schema validated;
* classification bound;
* source and effect references included;
* and content hashed.

---

## 35.1 Large Output

CAS.

---

## 35.2 Partial Output

Explicit status.

---

## 35.3 No Hidden Model State

Prohibited.

---

# 36. Definition Validation

Validate:

* schema;
* unique IDs;
* graph structure;
* dependency cycles;
* loop bounds;
* join semantics;
* activity existence;
* version support;
* side-effect class;
* retry compatibility;
* compensation compatibility;
* approval policy;
* data classification;
* resource limits;
* output reachability;
* and terminal paths.

---

## 36.1 Unreachable Step

Warning or error according to policy.

---

## 36.2 Unbounded Loop

Error.

---

## 36.3 Recursive Child Workflow

Bounded depth and explicit cycle analysis.

---

## 36.4 Missing Failure Path

Error for high-risk workflows.

---

# 37. Compiled Workflow Plan

Suggested schema:

```text
opure.compiled-workflow-plan/1
```

---

## 37.1 Fields

```text
plan_id
definition_id
definition_revision
compiler_revision
interpreter_revision
activity_contracts
activity_implementations
normalised_graph
condition_bytecode
input_schema
output_schema
capability_requirements
side_effect_summary
retry_summary
timeout_summary
compensation_graph
approval_summary
resource_summary
context_policies
routing_policies
plugin_packages
mcp_servers
created_at
plan_sha256
```

---

## 37.2 Immutable

Required.

---

## 37.3 Deterministic Compilation

Same inputs produce identical canonical plan bytes.

---

## 37.4 Compiler Upgrade

Produces a new plan revision unless equivalence is proven.

---

# 38. Condition Language

A small typed deterministic language.

---

## 38.1 Supported Values

* Boolean;
* integer;
* decimal;
* string;
* enumeration;
* instant from recorded value;
* duration;
* list;
* map;
* and null where schema permits.

---

## 38.2 Supported Operations

* equality;
* inequality;
* comparison;
* Boolean operations;
* membership;
* bounded collection functions;
* null checks;
* and typed path access.

---

## 38.3 Prohibited Operations

* I/O;
* clock;
* random;
* reflection;
* recursion;
* network;
* process;
* dynamic code;
* regex without bounded engine and policy;
* and arbitrary function call.

---

## 38.4 Expression Compilation

Compile to reviewed bytecode or typed AST.

---

## 38.5 Evaluation Limit

Bound instructions, depth and collection size.

---

# 39. Recorded Nondeterminism

Nondeterministic values require explicit activities.

---

## 39.1 Current Time Activity

Returns a recorded UTC instant.

---

## 39.2 Random Activity

Returns recorded bytes or typed value.

---

## 39.3 Identifier Activity

Returns a recorded opaque ID.

---

## 39.4 Environment Observation

Trusted service returns a versioned observation.

---

## 39.5 Reuse

Recovery uses the recorded result.

---

# 40. Workflow Instance

Suggested fields:

```text
workflow_instance_id
project_id
scope_kind
plan_id
plan_sha256
instance_revision
state
input_reference
input_sha256
created_by
created_at
started_at
deadline
priority
retention_policy
last_event_sequence
last_checkpoint_sequence
terminal_at
```

---

## 40.1 Opaque ID

Cryptographically random.

---

## 40.2 User-Supplied Correlation

Separate field.

---

## 40.3 Project Scope

Every project workflow binds exact project.

---

## 40.4 System Scope

Restricted first-party workflows only.

---

# 41. Workflow Instance Creation

Transaction:

1. validate project capability;
2. validate plan availability;
3. validate inputs;
4. create instance;
5. append Instance Created;
6. create initial step states;
7. evaluate deterministic entry conditions;
8. enqueue initial Ready steps;
9. create outbox notifications;
10. commit.

---

## 41.1 No Worker Before Commit

Required.

---

## 41.2 Idempotent Start

Use start command idempotency key.

---

## 41.3 Duplicate Start

Return original instance when request hash matches.

Conflict when it differs.

---

# 42. Workflow Event

Suggested schema:

```text
opure.workflow-event/1
```

---

## 42.1 Event Types

* Instance Created;
* Validation Started;
* Validation Completed;
* Instance Ready;
* Instance Started;
* Step Blocked;
* Step Ready;
* Step Scheduled;
* Lease Acquired;
* Attempt Started;
* Attempt Heartbeat;
* Effect Intent Recorded;
* Effect Invocation Recorded;
* Effect Completed;
* Effect Failed;
* Effect Reconciled;
* Attempt Completed;
* Attempt Failed;
* Retry Scheduled;
* Timer Created;
* Timer Fired;
* Signal Received;
* Signal Consumed;
* Approval Requested;
* Approval Granted;
* Approval Rejected;
* Approval Expired;
* Pause Requested;
* Paused;
* Resume Requested;
* Resumed;
* Cancel Requested;
* Cancellation Acknowledged;
* Worker Terminated;
* Compensation Started;
* Compensation Completed;
* Compensation Failed;
* Step Skipped;
* Step Outcome Unknown;
* Child Started;
* Child Completed;
* Migration Requested;
* Migration Completed;
* Recovery Started;
* Recovery Decision;
* Checkpoint Created;
* Instance Completed;
* Instance Completed with Warnings;
* Instance Failed;
* Instance Cancelled;
* Instance Terminated;
* Instance Quarantined;
* Instance Archived;
* and Instance Purged.

---

## 42.2 Event Payload

Typed schema by event type.

---

## 42.3 Event Sequence

Monotonic per instance.

---

## 42.4 Global Sequence

Optional database sequence for outbox ordering.

---

## 42.5 Event Time

UTC display time.

Sequence remains authoritative for order.

---

# 43. Event Hash Chain

Event hash includes:

* schema;
* instance;
* sequence;
* type;
* plan;
* step;
* attempt;
* actor;
* operation;
* time;
* payload hash;
* previous event hash.

---

## 43.1 Genesis Event

Uses defined zero or instance seed.

---

## 43.2 Tail Verification

At startup and periodically.

---

## 43.3 Full Verification

On demand, recovery or export.

---

## 43.4 Limitation

Integrity evidence, not external non-repudiation.

---

# 44. Current Projection

Suggested fields:

```text
workflow_instance_id
instance_revision
state
active_steps
waiting_reason
progress
current_plan
last_event_sequence
last_checkpoint
cancel_requested
pause_requested
compensation_state
recovery_state
updated_at
```

---

## 44.1 Rebuildable

Required.

---

## 44.2 No Hidden State

All durable state represented in authority records.

---

## 44.3 Projection Update

Same transaction as event.

---

# 45. Step Projection

Fields:

```text
step_instance_id
workflow_instance_id
step_id
iteration
branch
state
attempt_count
current_attempt
last_fencing_token
input_reference
output_reference
failure_reference
ready_at
started_at
completed_at
```

---

## 45.1 Step Instance Identity

Includes loop iteration and branch path.

---

## 45.2 No State from Worker Memory

Required.

---

# 46. Projection Replay

Replay starts from:

* validated checkpoint;
* or genesis.

---

## 46.1 Apply Function

Pure deterministic event application.

---

## 46.2 Unknown Event Version

Pause recovery.

---

## 46.3 Invalid Transition

Quarantine.

---

## 46.4 Replay Side Effects

None.

---

## 46.5 Replay Logging

Do not duplicate ordinary operational logs.

---

# 47. Checkpoint Snapshot

Suggested schema:

```text
opure.workflow-checkpoint/1
```

---

## 47.1 Fields

```text
checkpoint_id
workflow_instance_id
plan_id
event_sequence
instance_revision
workflow_state
step_states
dependency_state
wait_state
timer_state
approval_state
signal_state
effect_state
compensation_state
output_references
created_at
payload_sha256
event_tail_sha256
checkpoint_sha256
```

---

## 47.2 Derived

Not authoritative without matching event tail.

---

## 47.3 Compression

Allowed after bounded validation.

---

## 47.4 Encryption

Sensitive non-secret data may be encrypted under app storage policy.

---

## 47.5 Secret Exclusion

Mandatory.

---

# 48. Checkpoint Creation

1. select committed event sequence;
2. read consistent projection;
3. serialise canonical snapshot;
4. calculate hash;
5. persist CAS or database payload;
6. append Checkpoint Created event;
7. update checkpoint reference;
8. commit.

---

## 48.1 No Blocking Worker

Snapshot may be generated from committed state.

---

## 48.2 Failure

Workflow continues from events.

---

# 49. Checkpoint Validation

Check:

* schema;
* workflow ID;
* plan;
* sequence;
* event tail hash;
* payload hash;
* step identities;
* wait references;
* effect references;
* and no prohibited fields.

---

## 49.1 Invalid Snapshot

Discard and replay earlier history.

---

# 50. Checkpoint Retention

Suggested:

* latest valid;
* previous valid;
* terminal;
* pre-migration;
* and incident-related.

---

## 50.1 Compaction

Older derived snapshots can be deleted.

---

# 51. Event History Retention

Non-terminal workflow history must remain complete.

---

## 51.1 Terminal History

Retain according to workflow policy.

---

## 51.2 Event Compaction

Deferred.

---

## 51.3 Summary Event

Cannot replace required side-effect evidence.

---

# 52. Ready Queue

A durable table of eligible steps.

---

## 52.1 Fields

```text
queue_id
workflow_instance_id
step_instance_id
plan_id
priority
ready_at
deadline
resource_profile
activity_contract
required_worker
created_event
state
```

---

## 52.2 Insert

Same transaction as Step Ready.

---

## 52.3 Claim

Transactional lease acquisition.

---

## 52.4 Duplicate Queue Row

Unique constraint.

---

# 53. Scheduler Integration

Scheduler receives safe queue projections.

---

## 53.1 Workflow Authority

Scheduler cannot alter workflow state directly.

---

## 53.2 Resource Admission

Scheduler grants a resource reservation before attempt start.

---

## 53.3 Priority

Product policy, not model urgency.

---

## 53.4 Starvation

Age-based fairness within policy.

---

# 54. Activity Contract Registry

Suggested schema:

```text
opure.activity-contract/1
```

---

## 54.1 Fields

```text
activity_id
revision
name
owner_service
input_schema
output_schema
side_effect_class
idempotency_contract
reconciliation_contract
retry_contract
timeout_contract
cancellation_contract
compensation_contract
capability_contract
data_policy
resource_profile
implementation_revisions
created_at
contract_sha256
```

---

## 54.2 Immutable Revision

Required.

---

## 54.3 Owner

Trusted first-party service or approved external adapter.

---

## 54.4 Plugin and MCP

Wrapped by first-party gateway activity contracts.

---

# 55. Activity Implementation

Fields:

```text
implementation_id
activity_contract
implementation_revision
service_version
package_hash
worker_kind
supported_platform
minimum_runtime
maximum_input
maximum_output
verified_at
implementation_sha256
```

---

## 55.1 Old Implementation Support

Required while active plans depend on it, or migration required.

---

## 55.2 Missing Implementation

Workflow pauses.

---

# 56. Activity Input Mapping

Map from:

* workflow input;
* completed step output;
* signal payload;
* approval decision;
* project observation;
* or constants in the plan.

---

## 56.1 Schema Validation

Before Ready.

---

## 56.2 Secret Reference

Pass opaque reference only.

---

## 56.3 Mutable Source

Use exact source capability or snapshot.

---

# 57. Activity Output

Every output has:

* schema;
* classification;
* hash;
* size;
* storage reference;
* provenance;
* and validation result.

---

## 57.1 Invalid Output

Attempt fails before workflow transition.

---

## 57.2 Partial Output

Separate schema and state.

---

## 57.3 Large Output

CAS.

---

# 58. Activity Attempt

Suggested fields:

```text
attempt_id
workflow_instance_id
step_instance_id
attempt_number
logical_effect_generation
state
worker_id
lease_id
fencing_token
input_sha256
started_at
heartbeat_at
deadline
cancel_requested
result_reference
failure_reference
completed_at
```

---

## 58.1 Attempt Number

Monotonic per Step Instance.

---

## 58.2 New Attempt

Does not automatically mean a new Logical Effect.

---

# 59. Lease Acquisition

Transaction:

1. select eligible ready row;
2. verify workflow state;
3. verify step state;
4. verify `ready_at`;
5. verify resource reservation;
6. increment fencing token;
7. create attempt;
8. create lease;
9. move step to Leased;
10. remove or mark queue row;
11. append Lease Acquired;
12. commit.

---

## 59.1 Post-Commit Dispatch

Worker receives lease only after commit.

---

## 59.2 Worker Failure Before Start

Recovery can safely requeue if no effect intent exists.

---

# 60. Lease Fields

```text
lease_id
step_instance_id
attempt_id
worker_id
worker_process_id
worker_start_identity
fencing_token
acquired_at
heartbeat_at
expires_at
lease_state
```

---

## 60.1 Process ID Reuse

Use supervisor process identity, not PID alone.

---

## 60.2 Boot Identity

Record Runtime boot ID.

---

## 60.3 UTC

Persist display and conservative expiry time.

---

## 60.4 Monotonic Live Timing

Use for heartbeat intervals within a process.

---

# 61. Lease Heartbeat

A worker sends bounded progress and liveness.

---

## 61.1 Heartbeat Content

* attempt;
* fencing token;
* progress class;
* safe progress value;
* cancellation observation;
* and effect phase.

---

## 61.2 No Source Payload

By default.

---

## 61.3 Heartbeat Failure

Does not instantly prove worker death.

---

## 61.4 Database Load

Bound frequency.

Suggested:

```text
5–15 seconds
```

depending on activity.

---

# 62. Lease Expiry

Lease expiry triggers recovery investigation.

---

## 62.1 No Automatic Duplicate for Unsafe Effect

Required.

---

## 62.2 Supervisor Check

Inspect worker identity.

---

## 62.3 Fencing

New attempt receives higher token.

---

## 62.4 Stale Worker

Completion rejected.

---

# 63. Fencing Token Use

The token should accompany:

* trusted internal write operations;
* workspace locks;
* repository locks;
* patch application;
* build worktree ownership;
* and completion reports

where the target service supports fencing.

---

## 63.1 Unsupported External System

Use idempotency and reconciliation.

---

## 63.2 Token Comparison

Only current or greater accepted according to target contract.

---

# 64. Worker Start

After receiving a lease, the worker:

1. verifies package and activity implementation;
2. verifies plan and step;
3. verifies capability;
4. verifies input hash;
5. verifies lease and fencing;
6. creates isolated execution context;
7. reports Attempt Started;
8. executes.

---

## 64.1 Start Event

Durable before external side effect.

---

## 64.2 Failed Start

Safe retry when no effect intent.

---

# 65. Worker Completion

Worker submits:

* attempt;
* fencing token;
* result;
* result hash;
* effect receipts;
* classification;
* resource use;
* and completion state.

---

## 65.1 Workflow Service Validation

Before commit.

---

## 65.2 Stale Token

Reject.

---

## 65.3 Duplicate Completion

Return original committed result if hash matches.

Quarantine mismatch.

---

# 66. Side-Effect Class Registry

Every Activity Contract declares exactly one primary class and optional sub-effects.

---

## 66.1 Pure Deterministic

Same input and implementation produce same result under declared assumptions.

---

## 66.2 Pure Nondeterministic Read

No external mutation, but result can vary.

---

## 66.3 Local Read

Reads current local authoritative state.

---

## 66.4 External Read

Remote read with no intended mutation.

---

## 66.5 Transactional Internal Write

Target trusted service commits by operation ID.

---

## 66.6 Idempotent Write

Repeated calls converge to same state.

---

## 66.7 External Idempotency Key

Remote system deduplicates.

---

## 66.8 Reconciliable Write

Effect can be looked up reliably.

---

## 66.9 Compensatable Write

Separate reverse action exists.

---

## 66.10 Irreversible Write

Cannot be reliably undone.

---

## 66.11 Human Action

Effect performed or confirmed by person.

---

## 66.12 Unknown or Prohibited

Cannot run.

---

# 67. Effect Identity

Suggested fields:

```text
effect_id
workflow_instance_id
step_instance_id
effect_slot
plan_id
logical_effect_generation
idempotency_key
side_effect_class
target_system
target_resource
state
```

---

## 67.1 Canonical Key

Hash canonical identity and a product namespace.

---

## 67.2 External Key Length

Adapt safely to provider constraints while retaining full local identity.

---

## 67.3 No Secret

Key contains no secret or source text.

---

# 68. Side-Effect Intent

Suggested fields:

```text
effect_intent_id
effect_id
attempt_id
fencing_token
operation_type
target
request_hash
capability_reference
approval_reference
created_at
state
```

---

## 68.1 Persist Before Invocation

Mandatory for writes.

---

## 68.2 Read Effects

Receipt may still be useful.

---

## 68.3 Intent Is Not Success

UI and recovery must distinguish.

---

# 69. Effect Invocation Record

Where possible, record:

* request ID;
* provider correlation;
* trusted-service operation ID;
* start time;
* request hash;
* and transport receipt.

---

## 69.1 Commit Timing

A transport receipt may be committed in a separate transition.

---

## 69.2 Crash Window

Still possible.

---

# 70. Effect Result

Fields:

```text
effect_id
state
result_reference
result_sha256
external_resource_id
external_version
provider_receipt
verified_at
completed_at
```

---

## 70.1 Result Verification

Activity-specific.

---

## 70.2 Duplicate External Result

Map to same Logical Effect.

---

# 71. Transactional Internal Write

Preferred for trusted Opure services.

---

## 71.1 Operation Contract

Target service accepts:

* operation ID;
* effect ID;
* idempotency key;
* expected source revision;
* and fencing token where applicable.

---

## 71.2 Target Inbox

Target service records operation ID transactionally with its change.

---

## 71.3 Duplicate Call

Returns original outcome.

---

## 71.4 Cross-Database Atomicity

Not assumed.

Outbox/inbox and reconciliation remain required.

---

# 72. Idempotent Write

Examples:

* ensure label exists;
* ensure file content equals exact hash through staged write;
* ensure configuration key has value;
* or ensure package version installed.

---

## 72.1 Goal Seeking

Define desired state.

---

## 72.2 Existing Desired State

Success with evidence.

---

## 72.3 Conflicting State

Fail or require review.

---

# 73. External Idempotency Key

Use when provider guarantees deduplication under documented terms.

---

## 73.1 Evidence

Provider profile records:

* key scope;
* retention period;
* duplicate response;
* request equality rules;
* and status lookup.

---

## 73.2 Expired Key Window

Do not retry automatically.

---

## 73.3 Changed Request

New Logical Effect or fail.

---

# 74. Reconciliable Write

Recovery activity can ask:

* did resource exist;
* what request created it;
* what version;
* what state;
* and what response.

---

## 74.1 Strong Reconciliation

Unique operation ID or exact resource state.

---

## 74.2 Weak Reconciliation

Heuristic match.

Not sufficient for automatic retry of irreversible effect.

---

# 75. Irreversible Write

Examples may include:

* sending a non-idempotent external message;
* irreversible publication;
* destructive deletion without recovery;
* or physical user action.

---

## 75.1 Requirements

* explicit human approval;
* exact preview;
* no automatic retry;
* receipt;
* and manual recovery policy.

---

## 75.2 Crash Ambiguity

Outcome Unknown.

---

# 76. External Read

Reads may be repeated if:

* side-effect-free under provider contract;
* cost budget permits;
* data policy permits;
* and workflow semantics accept changed data.

---

## 76.1 Snapshot Required

Some workflows require exact result reuse.

Record and reuse.

---

## 76.2 Fresh Read Required

A new attempt may deliberately read again.

Plan must state.

---

# 77. AI Activity Side Effects

AI generation is usually an external or local nondeterministic read-like operation with cost and provider state, not a source mutation.

---

## 77.1 Retry

May produce a different output.

---

## 77.2 Reuse

If a valid response was received and committed, recovery reuses it.

---

## 77.3 Uncertain Provider Response

If request outcome is uncertain, a repeated call is a new attempt and may incur cost.

Policy decides whether allowed.

---

# 78. Tool Side Effects

Tool activity may contain several effects.

---

## 78.1 Decompose

Prefer one externally visible logical effect per Activity Step.

---

## 78.2 Multi-Effect Tool

Must return effect ledger.

---

## 78.3 Partial Tool Success

Explicit.

---

# 79. Effect Reconciliation

Suggested reconciliation outcome:

* Not Attempted;
* Definitely Not Applied;
* Applied and Verified;
* Applied with Different Result;
* Partially Applied;
* Reversed;
* Inconclusive;
* Target Unavailable;
* and Prohibited.

---

## 79.1 Reconciliation Activity

Versioned trusted contract.

---

## 79.2 No Model Authority

A model may explain evidence.

It cannot declare an effect applied without deterministic or human evidence.

---

# 80. Outcome Unknown

A first-class state.

---

## 80.1 Required UI

Show:

* intended effect;
* last durable state;
* external request evidence;
* reconciliation attempts;
* risk;
* and safe actions.

---

## 80.2 Allowed Actions

* Reconcile Again;
* Mark Applied with Evidence;
* Mark Not Applied with Evidence;
* Compensate;
* Create New Effect;
* Abandon;
* or Escalate.

---

## 80.3 Human Decision

Append event and evidence.

---

# 81. Retry Policy

Suggested schema:

```text
opure.workflow-retry-policy/1
```

---

## 81.1 Fields

```text
policy_id
revision
maximum_attempts
initial_delay
backoff_factor
maximum_delay
jitter
retryable_failures
non_retryable_failures
overall_budget
deadline_policy
effect_policy
created_at
policy_sha256
```

---

## 81.2 Attempt Zero

No ambiguity: initial execution is attempt 1.

---

## 81.3 Maximum

Finite.

---

# 82. Deterministic Backoff

Example:

```text
delay = min(max_delay, initial_delay × factor^(attempt-1))
```

plus bounded deterministic jitter.

---

## 82.1 Jitter Seed

Hash of instance, step and attempt.

---

## 82.2 Recorded Due Time

Persist exact `ready_at`.

---

## 82.3 Restart

Uses recorded value.

---

# 83. Failure Classification

Stable classes:

* Transient Transport;
* Provider Overloaded;
* Rate Limited;
* Worker Crash;
* Local Resource Pressure;
* Target Busy;
* Dependency Unavailable;
* Invalid Input;
* Invalid Output;
* Policy Denied;
* Authentication Required;
* Approval Required;
* Source Changed;
* Project Changed;
* Secret Detected;
* Capability Revoked;
* Timed Out;
* Cancelled;
* Outcome Unknown;
* Implementation Missing;
* and Internal Corruption.

---

## 83.1 Adapter Mapping

Versioned.

---

## 83.2 Unknown Failure

Non-retryable by default for side-effecting activity.

---

# 84. Retry Budget

Limit:

* attempts;
* elapsed time;
* provider calls;
* token use;
* cost;
* CPU;
* GPU;
* and external side effects.

---

## 84.1 Budget Exceeded

Terminal or approval required.

---

# 85. Retry Event Sequence

```text
Attempt Failed
    ↓
Failure Classified
    ↓
Retry Policy Evaluated
    ↓
Retry Scheduled
    ↓
Durable Timer
    ↓
Step Ready
```

---

## 85.1 No In-Memory Delay

Required.

---

# 86. Timeout Policy

Suggested schema:

```text
opure.workflow-timeout-policy/1
```

---

## 86.1 Fields

```text
queue_timeout
start_timeout
execution_timeout
heartbeat_timeout
provider_timeout
tool_timeout
approval_timeout
signal_timeout
workflow_deadline
cancellation_grace
forced_termination_grace
```

---

## 86.2 Null

Explicit unlimited only where approved.

---

## 86.3 Long Human Wait

Permitted with review date.

---

# 87. Durable Timer

Timer states:

* Created;
* Armed;
* Fired;
* Cancelled;
* Superseded;
* and Purged.

---

## 87.1 Unique Fire

A unique transition constraint.

---

## 87.2 Early Wake

Scheduler may wake early but cannot fire before due.

---

## 87.3 Late Wake

Record lateness.

---

## 87.4 Clock Rollback

Due comparison on restart uses persisted UTC.

Duplicate prevented by state.

---

# 88. Timer Scheduler

Workflow Service queries due timers.

---

## 88.1 Sleep Optimisation

An in-memory timer may wake the service.

The database remains authority.

---

## 88.2 Batch

Bound due-timer batch.

---

## 88.3 Overdue Timers

Process in deterministic priority order.

---

# 89. Cancellation Request

Fields:

```text
request_id
workflow_instance_id
scope
requested_by
requested_at
reason
mode
deadline
```

---

## 89.1 Scope

* whole workflow;
* branch;
* step;
* attempt;
* or wait.

---

## 89.2 Modes

* Graceful;
* Stop After Current Activity;
* Cancel Active Activity;
* and Emergency Terminate Worker.

---

# 90. Cancellation State Machine

```text
Running
    ↓ Cancel Requested
Cancelling
    ├── Cancelled
    ├── Compensation
    ├── Outcome Unknown
    ├── Failed
    └── Terminated
```

---

## 90.1 Pending Steps

Do not schedule.

---

## 90.2 New Side Effects

Denied after cancellation boundary unless compensation.

---

## 90.3 Active Attempt

Signal token.

---

# 91. Cooperative Cancellation

Use .NET `CancellationToken` for in-process activity and propagate through:

* gRPC;
* HTTP;
* tool operations;
* build;
* test;
* local inference;
* and trusted services

where supported.

---

## 91.1 Token Is Ephemeral

Not checkpointed.

---

## 91.2 Durable Cancellation State

Persisted separately.

---

## 91.3 Activity Acknowledgement

Required.

---

# 92. Cancel Operation Versus Cancel Wait

For each adapter declare:

* operation cancellation support;
* wait cancellation support;
* both;
* or neither.

---

## 92.1 Cancel Wait Only

Underlying work may continue.

Workflow records it and reconciles.

---

## 92.2 Cancel Operation

May still be best effort.

---

# 93. Forced Worker Termination

Allowed after:

* persisted cancellation;
* grace period;
* worker isolation;
* supervisor verification;
* and security policy.

---

## 93.1 Result

Attempt becomes Worker Terminated.

Effect state may remain unknown.

---

## 93.2 No Thread Abort

Do not attempt unsafe in-process thread termination.

---

# 94. Pause

Pause is a durable request.

---

## 94.1 Safe-Point Pause

Default.

---

## 94.2 Immediate Pause

Cancels waits and requests active activity cancellation according to policy.

---

## 94.3 Paused State

No ordinary steps become Ready.

---

## 94.4 Timers

May continue to mature but not transition until resume, according to plan.

---

# 95. Resume

Resume requires:

* plan support;
* project access;
* source checks;
* capability checks;
* credential checks;
* approval freshness;
* effect reconciliation;
* and resource eligibility.

---

## 95.1 Resume Event

Append before scheduling.

---

## 95.2 Changed Policy

May require replanning or migration.

---

# 96. Human Approval Definition

Suggested schema:

```text
opure.workflow-approval-policy/1
```

---

## 96.1 Fields

```text
approval_policy_id
revision
approver_roles
approval_mode
preview_schema
risk_threshold
expiry
source_revalidation
capability_revalidation
multi_approval
rejection_policy
created_at
policy_sha256
```

---

# 97. Approval Request

Fields:

```text
approval_request_id
workflow_instance_id
step_instance_id
plan_id
operation_id
preview_reference
preview_sha256
risk
data_class
provider
model
tool
command
paths
effects
estimated_cost
requested_at
expires_at
state
```

---

## 97.1 Exact Preview

Required.

---

## 97.2 No Secret

Required.

---

## 97.3 Mutable Operation

Any material change invalidates.

---

# 98. Approval Decision

Fields:

```text
approval_decision_id
approval_request_id
decision
decided_by
decided_at
reason
approval_capability
decision_sha256
```

---

## 98.1 Decisions

* Approve;
* Reject;
* Request Change;
* Defer;
* and Expire.

---

## 98.2 One-Time Capability

Operation bound.

---

## 98.3 Reuse

Denied.

---

# 99. Multi-Approval

Deferred by default but schema may support:

* Any One;
* All Named;
* Quorum;
* and Ordered Roles.

---

## 99.1 Same User

Cannot satisfy distinct-role requirement.

---

# 100. Approval Revalidation

Before execution check:

* request active;
* not expired;
* preview hash;
* plan;
* step;
* operation;
* source hashes;
* Context Plan;
* Routing Decision;
* Data Sharing Plan;
* provider;
* model;
* tool;
* command;
* and effect.

---

# 101. External Signal Definition

Suggested schema:

```text
opure.workflow-signal-definition/1
```

---

## 101.1 Fields

```text
signal_type
revision
payload_schema
allowed_senders
deduplication
ordering
expiry
classification
maximum_size
created_at
schema_sha256
```

---

# 102. Signal Inbox

A received signal is persisted transactionally.

---

## 102.1 Pre-Wait Signal

Retained.

---

## 102.2 Bound Capacity

Per workflow and type.

---

## 102.3 Overflow

Pause or reject.

Never drop silently.

---

# 103. Signal Deduplication

Use:

* sender;
* signal ID;
* target workflow;
* type;
* and deduplication key.

---

## 103.1 Duplicate Payload Match

Return original receipt.

---

## 103.2 Duplicate Key, Different Payload

Quarantine.

---

# 104. Signal Ordering

Options:

* Unordered;
* Sender Sequence;
* Global Workflow Sequence;
* and Correlation Sequence.

---

## 104.1 Gap

Wait or warn according to definition.

---

# 105. Signal Consumption

Same transaction as:

* inbox mark consumed;
* Signal Consumed event;
* wait completion;
* step output;
* next Ready steps.

---

# 106. Signal Security

Sender capability binds:

* workflow;
* signal type;
* payload class;
* expiry;
* and sequence.

---

## 106.1 Model Signal

A model output reaches the workflow only through a validated activity result.

---

## 106.2 Plugin and MCP Signal

Gateway capability required.

---

# 107. Parallel Fork

A fork creates branch step instances transactionally.

---

## 107.1 Branch Identity

Stable.

---

## 107.2 Resource Budget

Reserved or bounded.

---

## 107.3 Failure Policy

* Fail Fast;
* Wait All;
* Continue Successful;
* or Explicit Join Policy.

---

# 108. Join

Join types:

* All;
* Any;
* First Successful;
* Quorum;
* and Expression.

---

## 108.1 Completion Order

Recorded.

---

## 108.2 Deterministic Selection

For simultaneous eligible results use event sequence and declared policy.

---

## 108.3 Losing Branches

Cancellation policy explicit.

---

# 109. Bounded Loop

Fields:

```text
loop_id
maximum_iterations
condition
iteration_input
body
failure_policy
output_aggregation
```

---

## 109.1 Maximum Required

Hard.

---

## 109.2 Iteration Identity

Part of Step Instance ID.

---

## 109.3 Checkpoint

At iteration boundary.

---

# 110. Child Workflow

Parent starts child through durable command.

---

## 110.1 Link Fields

```text
parent_instance
parent_step
child_instance
child_plan
input_hash
cancellation_policy
failure_policy
compensation_owner
```

---

## 110.2 Child Start Idempotency

Effect identity.

---

## 110.3 Child Completion

Signal through outbox/inbox.

---

## 110.4 Parent Crash

Child continues according to policy.

---

# 111. Workflow-Level Concurrency

Suggested defaults:

* Ready attempts per workflow: 4;
* side-effecting writes per workflow: 1 unless explicitly independent;
* repository write branch: 1;
* human approval waits: unlimited within storage limits;
* and child workflows: 4 active.

Values require evidence.

---

# 112. Project-Level Concurrency

Scheduler limits:

* active workflows;
* local model calls;
* remote model calls;
* tools;
* builds;
* tests;
* repository writers;
* and plugin or MCP requests.

---

# 113. Resource Locks

Durable logical locks may protect:

* project workspace;
* repository;
* branch;
* file set;
* build worktree;
* package manager;
* model runtime;
* and release resource.

---

## 113.1 Lock Lease

Fenced and time bounded.

---

## 113.2 OS Mutex

May assist local coordination.

Not durable authority.

---

## 113.3 Deadlock

Acquire in canonical order.

---

# 114. Workspace Write Activity

Requires:

* project capability;
* exact source snapshot;
* destination set;
* staged write;
* path and reparse validation;
* expected file IDs;
* fencing token;
* validation;
* and write receipt.

---

## 114.1 Crash

Workspace Service reconciles staged write.

---

## 114.2 Duplicate

Operation ID returns original receipt.

---

# 115. Patch Activity

Separate:

* create patch;
* review patch;
* approve patch;
* apply patch;
* verify patch;
* and compensate or revert.

---

## 115.1 Patch Generation

No workspace write.

---

## 115.2 Patch Application

Side-effecting internal write with exact base.

---

## 115.3 Retry

Through Patch Service idempotency.

---

# 116. Repository Activity

Examples:

* read status;
* create branch;
* stage;
* commit;
* tag;
* and push.

---

## 116.1 Different Effects

Separate steps.

---

## 116.2 Commit

Use exact tree and operation ID.

---

## 116.3 Push

External write with reconciliation.

---

# 117. Build Activity

Build runs in isolated worker.

---

## 117.1 Side Effect

Build outputs are local artefacts.

---

## 117.2 Retry

Safe with isolated output and operation ID.

---

## 117.3 Source

Exact Workspace Snapshot.

---

# 118. Test Activity

Similar to Build.

---

## 118.1 Cancellation

Cooperative, then worker termination.

---

## 118.2 Partial Results

Explicit.

---

# 119. Tool Activity

Tool Mediator issues:

* Tool Call Plan;
* capability;
* operation ID;
* and result receipt.

---

## 119.1 Command Tool

Command execution is a side effect even when intended as read-only.

---

## 119.2 Shell

No direct shell step.

---

# 120. AI Activity

Inputs:

* Context Plan;
* Routing Decision;
* Data Sharing Plan;
* output schema;
* and budgets.

---

## 120.1 Result

Validated typed output or explicit prose artefact.

---

## 120.2 Retry Policy

Task-specific.

---

## 120.3 Provider Fallback

New Routing Decision and possibly approval.

---

## 120.4 Local Fallback

New Execution Profile and decision.

---

# 121. Plugin Activity

Gateway verifies:

* plugin installed;
* exact package;
* trust;
* capability;
* input;
* and result.

---

## 121.1 Upgrade

Running plan remains pinned.

---

## 121.2 Missing Old Package

Pause or migrate.

---

# 122. MCP Activity

Gateway verifies:

* server identity;
* account;
* capability;
* request;
* data policy;
* and result.

---

## 122.1 Reconnect

Does not imply safe retry of side effect.

---

## 122.2 Changed Fingerprint

Pause.

---

# 123. Network Activity

All network access through approved adapter or Network Gateway.

---

## 123.1 Receipt

Target, method class, request hash, response status and time.

---

## 123.2 Secret Headers

Excluded.

---

# 124. Compensation Definition

Suggested schema:

```text
opure.workflow-compensation-policy/1
```

---

## 124.1 Fields

```text
policy_id
revision
trigger
eligible_effects
ordering
activity_contract
idempotency
reconciliation
retry
timeout
approval
failure_policy
manual_remediation
policy_sha256
```

---

# 125. Compensation Trigger

Examples:

* workflow failure;
* branch failure;
* user cancellation;
* explicit rollback request;
* deadline;
* or policy violation.

---

## 125.1 No Automatic Global Compensation

Plan specific.

---

# 126. Compensation Ordering

Options:

* reverse completion order;
* reverse dependency order;
* explicit graph;
* or parallel independent compensations.

---

## 126.1 Original Event Order

Preserved.

---

# 127. Compensation State

For each effect:

* Not Required;
* Pending;
* Running;
* Compensated;
* Failed;
* Outcome Unknown;
* Manual Required;
* and Waived with Approval.

---

# 128. Compensation Failure

Workflow state:

```text
Failed — Compensation Incomplete
```

or:

```text
Cancelled — Compensation Incomplete
```

should be considered as a warning or failure subtype.

The core state list may use structured terminal reason.

---

# 129. Manual Remediation

A remediation record contains:

* affected effect;
* current evidence;
* proposed action;
* risks;
* owner;
* due date;
* and completion evidence.

---

## 129.1 Not Hidden

Trust Centre and workflow UI.

---

# 130. Workflow Completion

A workflow completes only when:

* all required terminal paths satisfied;
* no unresolved mandatory effects;
* no active attempts;
* no required compensation pending;
* output validates;
* and terminal event commits.

---

## 130.1 Completed with Warnings

May include:

* optional branch failed;
* optional compensation warning;
* late timer;
* or non-critical partial result.

---

## 130.2 Outcome Unknown

Cannot be ordinary Completed.

---

# 131. Workflow Failure

Failure record includes:

* failed step;
* attempt;
* classification;
* effect state;
* retries;
* compensation;
* source validity;
* and recovery options.

---

# 132. Termination

Termination means the workflow is intentionally stopped without normal cancellation guarantees.

---

## 132.1 Use

Administrative or security emergency.

---

## 132.2 Active Effect

Must reconcile.

---

# 133. Quarantine

Triggers:

* event-chain failure;
* impossible transition;
* payload hash mismatch;
* stale worker conflicting result;
* duplicate signal with different payload;
* plan substitution;
* capability forgery;
* secret detection;
* or corrupted checkpoint.

---

## 133.1 Ordinary Resume

Denied.

---

# 134. Archive

Terminal workflows may be archived.

---

## 134.1 State

Read-only.

---

## 134.2 Artefacts

Retention policy.

---

## 134.3 Restore

Creates an explicit restore or rerun operation.

---

# 135. Purge

Purge removes:

* inputs;
* outputs;
* checkpoint payloads;
* artefacts;
* logs under control;
* and ordinary event payloads

according to policy.

---

## 135.1 Minimal Tombstone

May retain:

* instance ID hash;
* project ID hash;
* terminal class;
* purge time;
* and reason.

---

## 135.2 Security Purge

May retain less.

---

# 136. Workflow Retention Policy

Suggested schema:

```text
opure.workflow-retention-policy/1
```

---

## 136.1 Fields

```text
non_terminal_retention
terminal_event_retention
artefact_retention
checkpoint_retention
approval_retention
signal_retention
recovery_report_retention
security_retention
project_deletion_policy
```

---

## 136.2 Non-Terminal

Retain until resolved or explicitly purged after warning.

---

## 136.3 Terminal Default

Suggested 90 days for ordinary workflows.

---

# 137. Project Deletion

Project deletion should:

* cancel or pause active workflows;
* reconcile effects;
* offer export;
* delete project-scoped workflow data after recovery period;
* and preserve only required safe evidence.

---

## 137.1 System Workflows

Unaffected.

---

# 138. Project Close

Closing Desktop does not cancel workflows automatically.

---

## 138.1 Policy

Developer can choose:

* continue;
* pause at safe point;
* cancel;
* or ask.

---

## 138.2 Default

Long side-effecting workflows should ask or follow explicit workflow policy.

---

# 139. Windows Shutdown

Runtime should:

1. stop accepting new work;
2. request checkpoint-safe quiescence;
3. persist shutdown event where possible;
4. cancel or detach workers according to activity policy;
5. close SQLite cleanly;
6. and rely on startup recovery when time expires.

---

## 139.1 No Assumption

Windows may terminate without graceful completion.

---

# 140. Power Loss

Recovery relies on SQLite durability and external reconciliation.

---

## 140.1 Last Commit

With selected `FULL` policy, intended to survive under supported filesystem assumptions.

---

## 140.2 External Effect

Still may be ambiguous.

---

# 141. Workflow Versioning

Running instance pins:

* Definition revision;
* Compiled Plan;
* interpreter revision;
* activity contracts;
* activity implementations;
* condition language;
* plugin packages;
* MCP identities;
* Context Policies;
* Routing Policies;
* and compensation policies.

---

## 141.1 Product Update

Old support remains installed or compatibility layer retained.

---

## 141.2 Removal

Blocked while active instance depends on it unless migrated or terminated.

---

# 142. Workflow Migration Plan

Suggested schema:

```text
opure.workflow-migration-plan/1
```

---

## 142.1 Fields

```text
migration_id
source_plan
target_plan
eligible_states
event_preconditions
step_mapping
output_mapping
wait_mapping
timer_mapping
signal_mapping
approval_mapping
effect_mapping
compensation_mapping
capability_changes
implementation_changes
validation
rollback
approval
created_at
migration_sha256
```

---

## 142.2 Immutable

Required.

---

## 142.3 Dry Run

Required.

---

# 143. Migration Eligibility

Typically:

* Paused;
* Waiting for Approval;
* Waiting for Timer;
* Waiting for External Event;
* Recovery Required;
* or no active attempts.

---

## 143.1 Running Attempt

Wait or cancel safely.

---

## 143.2 Outcome Unknown

Resolve first.

---

# 144. Migration Transaction

1. verify source instance revision;
2. verify source plan;
3. verify no active unsafe attempts;
4. append Migration Requested;
5. create pre-migration checkpoint;
6. apply deterministic mappings;
7. validate target projection;
8. append Migration Completed;
9. update plan reference;
10. commit.

---

## 144.1 External Effects

Never replayed.

---

## 144.2 Changed Approval

Fresh approval when operation meaning changes.

---

# 145. Migration Rollback

If commit fails, source remains.

After successful migration, rollback requires its own reverse Migration Plan.

---

# 146. Activity Version Compatibility

Possible classifications:

* Binary Compatible;
* Contract Compatible;
* Replay Compatible;
* Migration Required;
* and Unsupported.

---

## 146.1 No Silent Compatibility Claim

Evidence required.

---

# 147. Definition Update

New starts use new published revision according to policy.

Existing instances remain pinned.

---

# 148. Event Schema Versioning

Each payload has a schema revision.

---

## 148.1 Reader

Supports declared historical versions.

---

## 148.2 Migration

Authoritative events should not be rewritten casually.

Adapters may project old payloads.

---

# 149. Checkpoint Schema Versioning

Derived checkpoints may be discarded and rebuilt.

---

# 150. Outbox

Workflow outbox records notifications or commands after local transition.

---

## 150.1 Fields

```text
outbox_id
workflow_instance_id
event_sequence
destination
message_type
payload_reference
payload_sha256
idempotency_key
state
attempts
available_at
```

---

## 150.2 Dispatch

After transaction commit.

---

## 150.3 Duplicate Delivery

Destination inbox deduplicates.

---

# 151. Inbox

Workflow inbox handles:

* service result;
* child completion;
* signal;
* approval;
* and recovery response.

---

## 151.1 Idempotency

Unique sender message ID.

---

## 151.2 Different Payload

Quarantine.

---

# 152. No Distributed Transaction

Workflow DB and target service DB are not one atomic transaction.

---

## 152.1 Pattern

* local intent;
* outbox;
* target inbox;
* target transaction;
* result message;
* Workflow inbox;
* reconciliation.

---

# 153. Startup Recovery

Detailed phases:

```text
Database Recovery
    ↓
Schema Validation
    ↓
Integrity and Event-Tail Validation
    ↓
Projection Verification
    ↓
Plan and Implementation Availability
    ↓
Lease and Worker Reconciliation
    ↓
Effect Reconciliation
    ↓
Timer, Signal and Approval Restoration
    ↓
Project and Capability Revalidation
    ↓
Safe Resume or Recovery Required
```

---

# 154. Database Recovery Phase

* open database;
* allow SQLite WAL recovery;
* verify journal mode;
* verify synchronous setting;
* run quick check;
* verify foreign keys;
* inspect incomplete migrations;
* and acquire service ownership.

---

## 154.1 Busy Recovery

Wait boundedly.

---

## 154.2 Corrupt Database

Read-only incident copy then restore or rebuild where possible.

---

# 155. Projection Verification

Compare:

* instance revision;
* last event;
* step states;
* ready queue;
* timers;
* approvals;
* effects;
* and leases.

---

## 155.1 Mismatch

Replay.

---

## 155.2 Replay Failure

Quarantine affected instance.

---

# 156. Lease Recovery

For each non-terminal lease:

1. inspect Runtime boot ID;
2. inspect worker identity;
3. inspect heartbeat;
4. inspect attempt state;
5. inspect effect phase;
6. expire or retain;
7. issue higher fencing token if re-leased;
8. reconcile before retry where required.

---

# 157. Worker Definitely Not Started

Evidence:

* no Attempt Started;
* worker never acknowledged;
* no effect intent;
* and supervisor confirms absent.

Safe to retry.

---

# 158. Worker Started, No Effect Intent

For pure activities, safe to retry.

For side-effecting activities, the contract should require intent before invocation.

If the contract was violated, quarantine.

---

# 159. Effect Intent, No Invocation Evidence

May be safe to retry if adapter guarantees no call occurred.

Otherwise reconcile.

---

# 160. Invocation Evidence, No Result

Reconcile.

---

# 161. Result Received, Not Committed

Worker or target service may return the same result through operation lookup.

---

## 161.1 CAS Artefact

Verify hash.

---

## 161.2 No Lookup

Outcome Unknown for unsafe effects.

---

# 162. Timer Recovery

Load all active timers.

---

## 162.1 Due

Fire idempotently.

---

## 162.2 Future

Arm in-memory wake.

---

## 162.3 Duplicate

Unique transition prevents.

---

# 163. Approval Recovery

Restore waiting request.

---

## 163.1 Expired

Append Approval Expired.

---

## 163.2 Product Update

Re-render preview from bound artefact.

Do not change it.

---

## 163.3 Missing Activity Support

Pause.

---

# 164. Signal Recovery

Pending inbox remains.

---

## 164.1 Expired Signal

Mark and apply policy.

---

## 164.2 Waiting Step

Consume transactionally.

---

# 165. Capability Recovery

Capabilities should be short lived.

On recovery, acquire fresh capability for the exact pending action.

---

## 165.1 Old Capability

Not reused.

---

## 165.2 Changed Policy

Pause or replan.

---

# 166. Credential Recovery

Vault references remain.

Actual secret access requires fresh operation capability and user presence where configured.

---

## 166.1 Missing Credential

Waiting for Credential.

This may be represented as Waiting for Resource or Recovery Required reason.

---

# 167. Source Revalidation

Before resuming a source-sensitive step verify:

* project;
* Workspace Snapshot;
* Repository State;
* file hashes;
* patch base;
* Context Plan sources;
* memory revisions;
* and index generation where required.

---

## 167.1 Changed Source

No silent continuation.

---

# 168. Provider Revalidation

Before remote AI or MCP activity verify:

* Provider Profile;
* terms evidence;
* region;
* model;
* Routing Decision;
* Data Sharing Plan;
* credential;
* and availability.

---

## 168.1 Changed Model

New Routing Decision and potentially approval.

---

# 169. Plugin Revalidation

Verify exact installed package hash.

---

# 170. MCP Revalidation

Verify exact server fingerprint and account.

---

# 171. Recovery Decision

Suggested schema:

```text
opure.workflow-recovery-decision/1
```

---

## 171.1 Fields

```text
decision_id
workflow_instance_id
step_instance_id
attempt_id
observed_state
effect_state
evidence
classification
action
approved_by
created_at
decision_sha256
```

---

## 171.2 Actions

* Resume;
* Retry;
* Reconcile;
* Await Approval;
* Replan;
* Migrate;
* Compensate;
* Mark Outcome;
* Quarantine;
* Terminate;
* or Purge.

---

# 172. Recovery Report

Developer-facing report includes:

* shutdown or failure detected;
* database state;
* instance state;
* active steps;
* worker evidence;
* side-effect evidence;
* timers;
* approvals;
* source changes;
* policy changes;
* automatic actions;
* blocked actions;
* and next choices.

---

# 173. Automatic Resume Policy

Safe automatic resume only when:

* plan supported;
* project available;
* policy unchanged or compatible;
* no stale approval;
* no unsafe ambiguous effect;
* sources valid;
* credentials available or not required;
* and retry policy permits.

---

# 174. Recovery Required Policy

Use when any condition needs human or product decision.

---

## 174.1 No Alert Fatigue

Group related recoveries.

---

# 175. Workflow Corruption

An affected instance is quarantined.

---

## 175.1 Other Instances

May continue if database and service integrity remain safe.

---

## 175.2 Shared Corruption

Stop service.

---

# 176. Database Backup

Workflow DB contains durable non-rebuildable state.

Use SQLite online backup under service coordination.

---

## 176.1 Backup Contents

* database;
* referenced active CAS manifests;
* plan definitions;
* and integrity manifest.

---

## 176.2 Secrets

No secret values.

---

# 177. Restore

Restore validates:

* database integrity;
* event chains;
* plan availability;
* CAS hashes;
* project mapping;
* source freshness;
* effect state;
* and credentials.

---

## 177.1 Another Machine

External effects and local resources require reconciliation.

---

## 177.2 Same Project Missing

Pause.

---

# 178. Workflow Export

A workflow history export may include:

* plan;
* safe inputs;
* events;
* checkpoints;
* outputs;
* receipts;
* approvals;
* recovery decisions;
* and artefact manifest.

---

## 178.1 Secret Values

Never.

---

## 178.2 Project Source

Only explicit selected artefacts.

---

# 179. Workflow Import

Import as:

* historical archive;
* rerun template;
* or migration candidate.

---

## 179.1 No Active Resume by Default

Foreign side-effect state cannot be trusted automatically.

---

# 180. Trust Centre

Views:

```text
Active Workflows
Waiting for Approval
Waiting for Signals
Paused
Cancelling
Compensating
Recovery Required
Outcome Unknown
Completed
Failed
Definitions and Plans
Activities and Implementations
Side Effects
Retries
Timers
Signals
Approvals
Migrations
Recovery Reports
Retention and Purge
```

---

## 180.1 Workflow Detail

Show:

* plan;
* project;
* state;
* timeline;
* steps;
* attempts;
* leases;
* effects;
* approvals;
* retries;
* timers;
* signals;
* compensation;
* recovery;
* and outputs.

---

# 181. Desktop Workflow UI

Suggested actions:

* Start;
* Pause;
* Resume;
* Cancel;
* Review Approval;
* Retry Safe Step;
* Reconcile;
* Compensate;
* Migrate;
* Export;
* Archive;
* Delete;
* and Purge.

---

## 181.1 Unsafe Action

Requires clear preview.

---

## 181.2 No State Colour Alone

Accessible labels.

---

# 182. UI Copy — Durable Workflow

Suggested:

> Opure records each workflow transition before dispatching work. After a crash or restart, it rebuilds the workflow from its local event history, validates the pinned plan and reconciles any activity that may have produced an external effect.

---

# 183. UI Copy — Outcome Unknown

Suggested:

> The worker may have completed an external effect before Opure recorded the result. Retrying could duplicate that effect. Opure has stopped the workflow until the target system, a trusted receipt or you can establish what happened.

---

# 184. UI Copy — Retry

Suggested:

> This retry repeats the same logical step using its existing idempotency identity. It is permitted only because the activity is classified as safe to repeat or its previous outcome has been reconciled.

---

# 185. UI Copy — Compensation

Suggested:

> Compensation is a new action intended to reverse or mitigate an earlier effect. It does not erase the original event and may not restore every external consequence. Review the exact compensation before continuing.

---

# 186. UI Copy — Migration

Suggested:

> This workflow is pinned to an older execution plan. The migration maps its completed steps, waits, approvals and remaining work to a reviewed new plan. External effects will not be replayed.

---

# 187. Diagnostics

Safe diagnostics may include:

* workflow ID;
* plan ID;
* step ID;
* attempt;
* event type;
* state;
* failure class;
* effect class;
* lease age;
* fencing token;
* retry count;
* timer lateness;
* recovery classification;
* and duration.

---

## 187.1 Prohibited Logs

Do not log:

* secrets;
* full workflow input;
* full model prompt;
* source content;
* approval-sensitive content;
* provider credentials;
* or unredacted tool results.

---

# 188. Metrics

Low-cardinality local metrics:

* active workflows;
* ready steps;
* running attempts;
* waiting approvals;
* waiting signals;
* retries;
* cancellation latency;
* lease expiry;
* worker loss;
* Outcome Unknown;
* compensation;
* recovery duration;
* and event-commit latency.

---

## 188.1 No Project Labels

Do not export project identity.

---

# 189. Performance Targets

Provisional reference-hardware targets:

* event and projection commit p95: under 25 ms;
* ready-step claim p95: under 20 ms;
* lease heartbeat commit p95: under 20 ms;
* timer fire transition p95: under 50 ms after scheduler wake;
* approval decision transition p95: under 50 ms;
* projection rebuild for 10,000 events: under 2 seconds;
* startup discovery of 10,000 non-terminal workflows: under 10 seconds;
* and workflow-state query p95: under 20 ms.

External activities are separate.

---

# 190. Scale Targets

Version 1 evidence:

* 10,000 active workflows per channel;
* 100,000 waiting workflows;
* 1,000,000 terminal workflows under retention;
* 100,000,000 events;
* 1,000 Ready steps;
* 100 concurrent workers;
* 1,000,000 timers;
* and 1,000,000 signals under bounded retention.

These are architecture stress targets, not expected normal desktop use.

---

# 191. Backpressure

Bound:

* new workflow starts;
* ready queue;
* active attempts;
* signal inbox;
* approval requests;
* timers due;
* outbox;
* reconciliation;
* and recovery.

---

## 191.1 Overflow

Reject or pause with visible state.

---

## 191.2 Never Drop Durable Input Silently

Required.

---

# 192. Scheduler Fairness

Across:

* projects;
* workflow priority;
* age;
* resource class;
* and foreground versus background.

---

## 192.1 Security and Cancellation

May pre-empt optional work.

---

# 193. Deadlines

Workflow deadline is durable.

---

## 193.1 Reached

Transition according to policy:

* Cancel;
* Compensate;
* Pause;
* Fail;
* or Ask.

---

## 193.2 No New Ordinary Effect

After strict deadline.

---

# 194. Progress

Step may report:

* percentage where meaningful;
* units completed;
* current phase;
* and safe message.

---

## 194.1 Not Authoritative Completion

Required.

---

## 194.2 Persistence

Bounded latest progress plus optional events at milestones.

---

# 195. Error Model

Stable categories:

* Workflow Definition Invalid;
* Workflow Plan Invalid;
* Workflow Plan Unsupported;
* Workflow Project Mismatch;
* Workflow Input Invalid;
* Workflow Output Invalid;
* Workflow State Conflict;
* Workflow Transition Invalid;
* Workflow Event Invalid;
* Workflow Event Chain Invalid;
* Workflow Projection Invalid;
* Workflow Checkpoint Invalid;
* Workflow Queue Full;
* Workflow Resource Unavailable;
* Workflow Lease Conflict;
* Workflow Lease Expired;
* Workflow Fencing Rejected;
* Workflow Worker Lost;
* Workflow Activity Missing;
* Workflow Activity Version Unsupported;
* Workflow Capability Denied;
* Workflow Source Changed;
* Workflow Approval Required;
* Workflow Approval Expired;
* Workflow Signal Invalid;
* Workflow Signal Duplicate Conflict;
* Workflow Timer Invalid;
* Workflow Retry Exhausted;
* Workflow Deadline Reached;
* Workflow Cancelled;
* Workflow Cancellation Unacknowledged;
* Workflow Side Effect Prohibited;
* Workflow Effect Outcome Unknown;
* Workflow Reconciliation Failed;
* Workflow Compensation Failed;
* Workflow Migration Required;
* Workflow Migration Invalid;
* Workflow Credential Required;
* Workflow Plugin Changed;
* Workflow MCP Server Changed;
* Workflow Provider Changed;
* Workflow Corrupted;
* Workflow Recovery Required;
* and Workflow Purged.

---

# 196. Security Threat Model

Relevant threats include:

* workflow-definition substitution;
* Compiled Plan substitution;
* activity-implementation substitution;
* event deletion;
* event insertion;
* event reordering;
* checkpoint substitution;
* projection tampering;
* stale lease;
* stale worker result;
* fencing bypass;
* duplicate step execution;
* duplicate external effect;
* forged idempotency key;
* idempotency-key collision;
* effect-intent omission;
* effect receipt substitution;
* false reconciliation;
* automatic retry of irreversible effect;
* hidden fallback;
* stale approval reuse;
* approval impersonation;
* malicious signal;
* duplicate signal with changed payload;
* signal flooding;
* timer duplication;
* clock manipulation;
* retry storm;
* cancellation ignored;
* forced termination during side effect;
* compensation injection;
* workflow migration replay;
* old-plan removal;
* wrong-project resume;
* source substitution;
* secret checkpointing;
* provider-side hidden state;
* plugin upgrade substitution;
* MCP fingerprint change;
* outbox duplication;
* inbox replay;
* database corruption;
* CAS artefact substitution;
* and recovery-choice manipulation.

---

# 197. Security Controls

Controls include:

* immutable Definition revisions;
* deterministic plan compilation;
* canonical plan hashes;
* pinned activity implementations;
* project capabilities;
* append-only events;
* event hash chains;
* rebuildable projections;
* validated checkpoints;
* SQLite `synchronous=FULL`;
* durable-before-dispatch scheduling;
* unique queue constraints;
* fenced leases;
* operation IDs;
* stable Effect Identities;
* persisted Side-Effect Intents;
* target-service inboxes;
* external idempotency;
* trusted reconciliation;
* explicit Outcome Unknown;
* bounded retries;
* durable timers;
* cooperative cancellation;
* supervised worker termination;
* exact approval capabilities;
* typed signal capabilities;
* bounded inboxes;
* explicit compensation;
* plan pinning;
* migration plans;
* source and policy revalidation;
* secret exclusion;
* CAS hashes;
* Trust Centre evidence;
* and recovery reports.

---

# 198. Security Limitations

This design cannot guarantee:

* universal exactly-once external effects;
* that an external provider honours its idempotency promise;
* that every external effect can be reconciled;
* that compensation restores every consequence;
* that a human approval is wise;
* perfect protection from same-user malware;
* forensic deletion;
* or availability after severe disk corruption without a valid backup.

Fencing protects Opure-controlled commits.

It cannot stop a stale worker from affecting an external system that does not support fencing or idempotency.

---

# 199. Privacy Impact

Workflow state may reveal:

* project activity;
* requested tools;
* providers;
* paths;
* approvals;
* errors;
* and model use.

---

## 199.1 Data Minimisation

Persist only data required for:

* execution;
* recovery;
* evidence;
* and retention policy.

---

## 199.2 Remote Operations

Every remote activity remains covered by its Data Sharing Plan.

---

## 199.3 Workflow Export

Reviewable.

---

# 200. Reliability Impact

The architecture adds:

* event storage;
* recovery logic;
* leases;
* retries;
* and reconciliation

to gain durable, inspectable operation.

---

## 200.1 Fail Closed

Unsafe ambiguity stops.

---

## 200.2 Partial Service Failure

One quarantined workflow should not corrupt others.

---

# 201. Performance Impact

Costs include:

* SQLite commit per transition;
* WAL sync;
* event hashing;
* checkpointing;
* lease heartbeats;
* and recovery checks.

The selected durability intentionally favours correctness over maximum workflow-transition throughput.

---

# 202. Testing Strategy

ADR-0008 applies.

Workflow durability requires:

* schema tests;
* plan-compiler tests;
* state-machine tests;
* event tests;
* projection tests;
* checkpoint tests;
* database durability tests;
* queue tests;
* lease tests;
* fencing tests;
* activity-contract tests;
* effect tests;
* idempotency tests;
* reconciliation tests;
* retry tests;
* timer tests;
* cancellation tests;
* approval tests;
* signal tests;
* parallelism tests;
* compensation tests;
* child-workflow tests;
* migration tests;
* service-integration tests;
* security tests;
* crash tests;
* power-loss simulation;
* recovery tests;
* fuzzing;
* performance tests;
* and adversarial end-to-end workflows.

---

# 203. Database Configuration Tests

Test:

* WAL enabled;
* `synchronous=FULL`;
* foreign keys;
* trusted schema;
* local disk;
* network path;
* cloud-synchronised path;
* read concurrency;
* one writer;
* busy timeout;
* WAL checkpoint;
* and database reopen after process crash.

---

# 204. SQLite Power-Loss Tests

Use a fault-injection or VFS test harness where feasible.

Crash or simulate loss:

* before transaction;
* during event insert;
* during projection update;
* during ready-queue insert;
* during effect-intent insert;
* during commit;
* after commit;
* during WAL checkpoint;
* and after checkpoint.

After reopen, the transition must appear fully or not at all.

---

# 205. `synchronous` Configuration Tests

Verify Workflow database refuses:

* OFF;
* and unauthorised NORMAL.

---

## 205.1 Configuration Drift

Detect at startup.

---

# 206. Database Location Tests

Test:

* expected app-data path;
* repository path;
* network share;
* removable drive;
* OneDrive or equivalent;
* reparse point;
* ACL failure;
* long path;
* and directory replacement.

---

# 207. Definition Schema Tests

Test:

* valid definition;
* duplicate ID;
* duplicate step;
* missing input schema;
* missing output schema;
* unknown step kind;
* unknown activity;
* unbounded loop;
* dependency cycle;
* recursive child cycle;
* missing terminal;
* invalid join;
* and canonical hash.

---

# 208. Arbitrary-Code Denial Tests

Attempt to embed:

* C#;
* PowerShell;
* JavaScript;
* Python;
* shell;
* SQL;
* DLL;
* expression-tree delegate;
* and serialised object graph.

Every attempt fails validation.

---

# 209. Plan Compilation Tests

Test:

* deterministic output;
* canonical ordering;
* same input same hash;
* changed activity revision;
* changed expression language;
* changed retry policy;
* changed approval;
* changed resource policy;
* and compiler upgrade.

---

# 210. Plan Substitution Tests

Attempt:

* different plan same ID;
* changed bytes same hash field;
* wrong Definition;
* wrong project;
* stale cached plan;
* and unsigned package substitution.

Detect and quarantine.

---

# 211. Condition-Language Tests

Test:

* Boolean;
* integer;
* decimal;
* string;
* enum;
* instant;
* duration;
* list;
* map;
* null;
* comparison;
* membership;
* bounded aggregate;
* and path access.

---

## 211.1 Condition Abuse

Attempt:

* I/O;
* network;
* clock;
* random;
* recursion;
* reflection;
* process;
* file;
* dynamic type;
* excessive depth;
* huge collection;
* and catastrophic regex.

---

# 212. Recorded Nondeterminism Tests

Test:

* current time;
* random bytes;
* identifier;
* environment observation;
* replay;
* retry;
* and migration.

The recorded value must be reused.

---

# 213. Instance Creation Tests

Test:

* valid;
* duplicate idempotency key;
* duplicate matching request;
* duplicate differing request;
* invalid project;
* invalid input;
* missing plan;
* unsupported plan;
* and initial Ready steps.

---

# 214. Workflow State-Machine Tests

Test every allowed and prohibited state transition.

---

## 214.1 Examples

* Created to Ready;
* Ready to Running;
* Running to Waiting for Approval;
* Waiting to Running;
* Running to Paused;
* Paused to Running;
* Running to Cancelling;
* Cancelling to Cancelled;
* Running to Compensating;
* Running to Completed;
* Outcome Unknown to Completed without evidence;
* Purged to Running;
* and Quarantined to Running without recovery.

---

# 215. Step State-Machine Tests

Test every step transition.

---

# 216. Attempt State-Machine Tests

Test every attempt transition.

---

# 217. Optimistic-Concurrency Tests

Issue concurrent:

* pause and complete;
* cancel and retry;
* approval and expiry;
* signal and timeout;
* completion and lease expiry;
* migration and resume;
* and purge and recovery.

Only valid expected revision commits.

---

# 218. Command Idempotency Tests

Test duplicate:

* Start;
* Pause;
* Resume;
* Cancel;
* Approve;
* Reject;
* Signal;
* Retry;
* Reconcile;
* Compensate;
* Migrate;
* Archive;
* and Purge.

---

# 219. Event Schema Tests

Test every event type and payload schema.

---

# 220. Event Ordering Tests

Test:

* same UTC timestamp;
* concurrent step completions;
* duplicate event ID;
* missing sequence;
* sequence gap;
* out-of-order inbox;
* and event replay.

---

# 221. Event Hash Tests

Modify:

* event type;
* payload;
* actor;
* time;
* previous hash;
* sequence;
* plan;
* step;
* and attempt.

Detect.

---

# 222. Event Insertion and Deletion Tests

Detect altered chain.

---

# 223. Projection Tests

Build expected projections for:

* sequence;
* parallel;
* join;
* wait;
* retry;
* cancel;
* compensation;
* migration;
* completion;
* and Outcome Unknown.

---

# 224. Projection Corruption Tests

Modify projection only.

Replay should restore.

---

# 225. Replay Side-Effect Tests

Replay must not:

* dispatch activity;
* call provider;
* write file;
* run command;
* send signal;
* request approval;
* or create external effect.

---

# 226. Checkpoint Creation Tests

Test:

* event threshold;
* approval boundary;
* timer wait;
* pause;
* compensation;
* terminal;
* and pre-migration.

---

# 227. Checkpoint Validation Tests

Test:

* valid;
* wrong workflow;
* wrong plan;
* wrong sequence;
* wrong event tail;
* wrong payload hash;
* missing step;
* stale wait;
* unknown schema;
* and prohibited secret field.

---

# 228. Checkpoint Recovery Tests

Test:

* latest valid;
* latest corrupt;
* previous valid;
* no checkpoint;
* and replay from genesis.

---

# 229. Checkpoint Secret Tests

Seed secret canaries in:

* workflow input;
* activity output;
* approval;
* model response;
* tool result;
* signal;
* and failure.

Verify checkpoints exclude values.

---

# 230. Ready Queue Tests

Test:

* initial Ready;
* dependency completion;
* condition false;
* retry due;
* duplicate insertion;
* cancellation;
* pause;
* migration;
* and queue overflow.

---

# 231. Queue Claim Tests

Run concurrent workers.

Only one current lease per Step Instance.

---

# 232. Scheduler Tests

Test:

* priority;
* age fairness;
* resource class;
* foreground;
* background;
* project limit;
* and starvation.

---

# 233. Activity Contract Tests

Test:

* valid contract;
* unknown side-effect class;
* missing idempotency;
* missing reconciliation;
* missing timeout;
* missing cancellation;
* incompatible retry;
* missing capability;
* and changed revision.

---

# 234. Activity Implementation Tests

Test:

* valid package;
* wrong hash;
* wrong platform;
* missing version;
* old supported version;
* removed version;
* crash;
* and quarantine.

---

# 235. Input-Mapping Tests

Test:

* workflow input;
* step output;
* signal;
* approval;
* project observation;
* constants;
* missing field;
* wrong type;
* and secret reference.

---

# 236. Output Validation Tests

Test:

* valid;
* invalid schema;
* wrong classification;
* oversized;
* wrong hash;
* CAS missing;
* partial;
* and duplicate differing result.

---

# 237. Lease Acquisition Tests

Test:

* eligible;
* not Ready;
* paused workflow;
* cancelled workflow;
* future `ready_at`;
* no resource;
* concurrent claim;
* and transaction rollback.

---

# 238. Worker Identity Tests

Test:

* current Runtime boot;
* old boot;
* PID reuse;
* process identity mismatch;
* worker package mismatch;
* and forged worker.

---

# 239. Lease Heartbeat Tests

Test:

* valid;
* stale token;
* wrong attempt;
* late heartbeat;
* cancellation observed;
* effect phase;
* and flood.

---

# 240. Lease Expiry Tests

Test:

* worker alive;
* worker dead;
* process unknown;
* pure step;
* side-effect step before intent;
* after intent;
* after invocation;
* and result not committed.

---

# 241. Fencing Tests

Attempt completion from:

* current worker;
* expired worker;
* old attempt;
* different worker;
* forged higher token;
* and duplicate current completion.

---

# 242. Target-Service Fencing Tests

For supported services, attempt an old token write after a new lease.

Target rejects.

---

# 243. Worker Crash Matrix

Kill worker:

* before acknowledgement;
* after Attempt Started;
* before effect intent;
* after effect intent;
* during transport;
* after external success;
* after result received;
* during output upload;
* and before completion commit.

Verify classification.

---

# 244. Side-Effect Classification Tests

Test every class and prohibited unknown.

---

# 245. Effect Identity Tests

Test:

* retry same logical effect;
* new deliberate generation;
* loop iteration;
* branch;
* child workflow;
* compensation;
* duplicate derivation;
* and collision resistance.

---

# 246. Idempotency-Key Tests

Test:

* exact retry;
* changed request same key;
* same request new key;
* provider length adaptation;
* retention expiry;
* collision;
* and secret absence.

---

# 247. Side-Effect Intent Tests

Verify persist before:

* workspace write;
* repository commit;
* push;
* external API write;
* plugin mutation;
* MCP mutation;
* and irreversible message.

---

# 248. Missing Intent Tests

A side-effecting adapter that invokes before intent is quarantined.

---

# 249. Internal Inbox Tests

Target service:

* first operation;
* duplicate same hash;
* duplicate different hash;
* concurrent duplicate;
* crash before target commit;
* crash after target commit;
* and response loss.

---

# 250. External Idempotency Tests

Simulate:

* first success;
* repeated key;
* changed request;
* key expired;
* provider unavailable;
* provider returns different response;
* and provider violates contract.

---

# 251. Reconciliation Tests

Test outcomes:

* Not Attempted;
* Definitely Not Applied;
* Applied and Verified;
* Applied with Different Result;
* Partially Applied;
* Reversed;
* Inconclusive;
* Target Unavailable;
* and Prohibited.

---

# 252. False Reconciliation Tests

Attempt:

* model-only claim;
* stale provider receipt;
* wrong project resource;
* similar resource name;
* forged operation ID;
* and weak timestamp match.

No automatic applied or not-applied conclusion.

---

# 253. Outcome Unknown Tests

Verify:

* no automatic retry;
* visible evidence;
* allowed actions;
* human decision provenance;
* compensation option;
* and no ordinary completion.

---

# 254. Pure Activity Retry Tests

Kill and retry deterministic and nondeterministic reads.

---

# 255. Irreversible Effect Tests

Test:

* approval;
* intent;
* invocation;
* crash;
* receipt;
* no receipt;
* cancellation;
* and manual remediation.

---

# 256. AI Activity Retry Tests

Test:

* no response;
* full committed response;
* partial stream;
* uncertain provider call;
* retry allowed;
* retry denied;
* changed output;
* changed Routing Decision;
* and cost budget.

---

# 257. Tool Effect Tests

Test:

* read-only command with hidden side effect;
* one effect;
* several effects;
* partial success;
* cancellation;
* and result ledger.

---

# 258. Retry-Policy Tests

Test:

* maximum attempts;
* zero or invalid attempts;
* initial delay;
* factor;
* maximum;
* retryable class;
* terminal class;
* budget;
* and deadline.

---

# 259. Deterministic-Jitter Tests

Same instance and attempt produces same delay across restart.

---

# 260. Retry-Storm Tests

Create many transient failures.

Verify:

* backoff;
* jitter;
* project limits;
* provider rate limits;
* cost budget;
* and cancellation.

---

# 261. Failure-Mapping Tests

Every adapter maps errors to stable classes.

Unknown side-effecting error does not retry.

---

# 262. Timer Tests

Test:

* future;
* due;
* overdue;
* duplicate fire;
* cancellation;
* supersession;
* clock forward;
* clock backward;
* Runtime restart;
* Windows restart;
* and one million timers.

---

# 263. Timer Transaction Tests

Timer firing and workflow transition are atomic.

---

# 264. Cancellation Tests

Test:

* pending;
* Ready;
* Leased;
* Running;
* Waiting;
* Approval;
* Signal;
* Timer;
* Child;
* Compensating;
* and Outcome Unknown.

---

# 265. Cooperative Cancellation Tests

Verify token flows through:

* gRPC;
* HTTP;
* build;
* test;
* local inference;
* tool;
* plugin gateway;
* and MCP gateway.

---

# 266. Cancel-Wait Tests

Underlying activity continues and is reconciled.

---

# 267. Forced-Termination Tests

Use isolated worker.

Verify:

* grace;
* process kill;
* lease state;
* effect ambiguity;
* and recovery.

---

# 268. Pause Tests

Test:

* safe-point;
* immediate;
* active pure step;
* active side effect;
* timer;
* signal;
* approval;
* and resume.

---

# 269. Resume Revalidation Tests

Change:

* project policy;
* source;
* Context Plan;
* Routing Decision;
* provider;
* model;
* plugin;
* MCP server;
* credential;
* and activity implementation.

---

# 270. Approval-Request Tests

Test:

* exact preview;
* patch;
* command;
* provider;
* tool;
* cost;
* expiry;
* classification;
* and no secret.

---

# 271. Approval-Decision Tests

Test:

* approve;
* reject;
* change;
* defer;
* expire;
* duplicate decision;
* concurrent decision;
* and unauthorised user.

---

# 272. Stale-Approval Tests

Change every material bound field.

Execution must stop.

---

# 273. Approval-Impersonation Tests

Attempt from:

* model;
* plugin;
* MCP;
* old session;
* wrong role;
* imported event;
* and forged identity.

---

# 274. Multi-Approval Tests

If enabled, test quorum and distinct roles.

---

# 275. Signal-Schema Tests

Test every payload type and size.

---

# 276. Signal-Deduplication Tests

Test:

* exact duplicate;
* different payload same key;
* same payload new key;
* sender sequence;
* gap;
* expiry;
* and replay.

---

# 277. Pre-Wait Signal Tests

Signal persists and is consumed later.

---

# 278. Signal-Flood Tests

Verify bounded inbox and visible rejection.

---

# 279. Signal-Security Tests

Attempt:

* wrong workflow;
* wrong type;
* wrong sender;
* expired capability;
* secret;
* prompt injection;
* model direct signal;
* and cross-project signal.

---

# 280. Parallel-Fork Tests

Test:

* independent branches;
* resource cap;
* one failure;
* fail fast;
* wait all;
* and cancellation.

---

# 281. Join Tests

Test:

* All;
* Any;
* First Successful;
* Quorum;
* Expression;
* simultaneous completion;
* deterministic tie;
* and losing branch cancellation.

---

# 282. Bounded-Loop Tests

Test:

* zero iteration;
* one;
* maximum;
* over maximum;
* condition;
* retry;
* checkpoint;
* and cancellation.

---

# 283. Child-Workflow Tests

Test:

* start;
* duplicate start;
* complete;
* fail;
* cancel propagation;
* parent crash;
* child crash;
* compensation owner;
* and output.

---

# 284. Resource-Lock Tests

Test:

* acquire;
* release;
* expiry;
* stale token;
* canonical order;
* deadlock;
* repository write;
* and project close.

---

# 285. Workspace-Write Tests

Crash at every staged-write boundary.

Verify idempotency and reconciliation.

---

# 286. Patch-Workflow Tests

Test:

* generate;
* review;
* approve;
* apply;
* verify;
* changed base;
* retry;
* revert;
* and compensation.

---

# 287. Repository-Workflow Tests

Test:

* branch;
* commit;
* tag;
* push;
* push ambiguity;
* and remote reconciliation.

---

# 288. Build-and-Test Workflow Tests

Test isolation, retry, cancellation and partial results.

---

# 289. AI-Workflow Tests

Test:

* exact Context Plan;
* local;
* remote;
* approval;
* fallback;
* retry;
* partial stream;
* provider outage;
* and output schema.

---

# 290. Plugin-Workflow Tests

Test pinned package, upgrade, removal and capability.

---

# 291. MCP-Workflow Tests

Test pinned fingerprint, reconnect, account change and side effect.

---

# 292. Compensation Tests

Test:

* reverse order;
* explicit graph;
* independent parallel;
* idempotency;
* retry;
* timeout;
* failure;
* Outcome Unknown;
* and manual remediation.

---

# 293. Compensation-Honesty Tests

Verify original event remains and UI does not claim database rollback.

---

# 294. Completion Tests

Test all terminal requirements and warnings.

---

# 295. Archive-and-Purge Tests

Test retention, active references, CAS cleanup and tombstones.

---

# 296. Project-Close Tests

Test continue, pause, cancel and ask.

---

# 297. Windows-Shutdown Tests

Simulate:

* graceful shutdown;
* short deadline;
* abrupt termination;
* service kill;
* and machine restart.

---

# 298. Plan-Versioning Tests

Start on old plan, publish new plan and verify old instance remains pinned.

---

# 299. Activity-Removal Tests

Attempt product update that removes required implementation.

Release gate should block or migration required.

---

# 300. Migration-Plan Tests

Test:

* valid;
* wrong source plan;
* wrong target;
* ineligible state;
* step mapping;
* output mapping;
* timer;
* signal;
* approval;
* effect;
* compensation;
* dry run;
* and hash.

---

# 301. Migration-Safety Tests

Verify no external effect replay.

---

# 302. Migration-Approval Tests

Changed provider, command, patch or authority requires fresh approval.

---

# 303. Migration-Crash Tests

Crash:

* before request;
* after pre-checkpoint;
* during mapping;
* before commit;
* after commit;
* and during post-validation.

---

# 304. Outbox Tests

Test:

* first dispatch;
* duplicate;
* failure;
* retry;
* target inbox;
* wrong payload;
* and state recovery.

---

# 305. Inbox Tests

Test duplicate and conflicting messages.

---

# 306. Startup-Recovery Tests

Test:

* clean shutdown;
* Runtime crash;
* worker crash;
* Windows restart;
* power-loss simulation;
* product update;
* incomplete migration;
* and database busy.

---

# 307. Recovery-Classification Tests

Test every classification.

---

# 308. Automatic-Resume Tests

Verify only safe cases resume.

---

# 309. Recovery-Required Tests

Verify unsafe cases remain stopped with actionable explanation.

---

# 310. Source-Revalidation Tests

Change:

* file;
* repository commit;
* patch base;
* memory revision;
* index generation;
* project configuration;
* and workspace root.

---

# 311. Provider-Revalidation Tests

Change:

* terms;
* region;
* model;
* endpoint;
* credential;
* and Routing Decision.

---

# 312. Capability-Revalidation Tests

Revoke capability before restart.

---

# 313. Secret-Recovery Tests

Ensure no secret persists and fresh Vault access is required.

---

# 314. Event-Corruption Recovery Tests

Corrupt one instance history.

Other safe instances continue where policy permits.

---

# 315. Database-Corruption Tests

Corrupt shared pages.

Service stops and restores from backup or incident path.

---

# 316. CAS-Corruption Tests

Modify output artefact.

Affected step or workflow quarantines.

---

# 317. Backup-and-Restore Tests

Test:

* consistent backup;
* active workflows;
* restore same machine;
* restore new machine;
* missing project;
* changed sources;
* external effects;
* and missing plan.

---

# 318. Workflow-Export Tests

Test:

* safe input;
* event history;
* checkpoint;
* outputs;
* approvals;
* effects;
* no secrets;
* and classification.

---

# 319. Workflow-Import Tests

Test historical archive, rerun template and migration candidate.

No foreign active resume.

---

# 320. Trust Centre Tests

Verify:

* timeline;
* steps;
* attempts;
* leases;
* effects;
* retries;
* approvals;
* signals;
* timers;
* compensation;
* migration;
* recovery;
* and purge.

---

# 321. Diagnostics-Leakage Tests

Seed source and secret canaries.

---

# 322. Metrics Tests

Verify low cardinality and no project labels.

---

# 323. Fuzzing

Fuzz:

* Workflow Definition;
* Compiled Plan;
* condition bytecode;
* activity contract;
* Workflow Event;
* checkpoint;
* signal;
* approval;
* retry policy;
* timeout policy;
* effect record;
* reconciliation;
* compensation;
* migration plan;
* Recovery Decision;
* and export bundle.

---

# 324. Crash-Fault Injection

Inject crash after every durable or external boundary in representative workflows.

---

## 324.1 Required Workflows

* pure sequence;
* parallel;
* approval;
* signal;
* timer;
* local AI;
* remote AI;
* patch apply;
* build and test;
* external write;
* compensation;
* child workflow;
* and migration.

---

# 325. Duplicate-Execution Adversarial Suite

Force:

* duplicate queue delivery;
* duplicate worker start;
* lease expiry;
* delayed stale completion;
* outbox duplicate;
* inbox duplicate;
* signal duplicate;
* and provider retry.

---

# 326. Effect-Ambiguity Adversarial Suite

Force success followed by crash before result commit for every side-effect class.

---

# 327. Retry-Storm Adversarial Suite

Thousands of workflows fail transiently.

Verify bounded provider, CPU, disk and queue use.

---

# 328. Stale-Approval Adversarial Suite

Approve then alter:

* patch;
* command;
* model;
* provider;
* source;
* cost;
* tool;
* and effect target.

---

# 329. Wrong-Project Adversarial Suite

Attempt:

* resume;
* signal;
* approval;
* activity result;
* source reference;
* effect reconciliation;
* and export

using another project.

---

# 330. Plan-Drift Adversarial Suite

Replace:

* Definition;
* plan;
* activity implementation;
* plugin;
* MCP server;
* Context Policy;
* and Routing Policy.

---

# 331. Secret Adversarial Suite

Attempt secret entry through every input and event channel.

---

# 332. Cancellation-Race Suite

Race cancellation with:

* lease;
* start;
* intent;
* invocation;
* result;
* completion;
* retry;
* signal;
* timer;
* approval;
* and compensation.

---

# 333. Migration Adversarial Suite

Attempt to use migration to:

* replay effect;
* skip approval;
* change project;
* change provider silently;
* drop compensation;
* alter output;
* and erase history.

---

# 334. Performance Tests

Measure:

* event commit;
* projection update;
* queue claim;
* lease heartbeat;
* timer scan;
* signal ingest;
* approval;
* checkpoint creation;
* replay;
* startup recovery;
* effect lookup;
* outbox;
* retention cleanup;
* and Trust Centre query.

---

# 335. Endurance Tests

Run:

* workflows for seven days;
* long approval waits;
* repeated product restarts;
* repeated machine restarts;
* high event counts;
* timer volume;
* and CAS retention.

---

# 336. Accessibility Tests

Workflow UI must support:

* keyboard;
* Narrator;
* high contrast;
* reduced motion;
* state timeline;
* approval review;
* Outcome Unknown evidence;
* retry explanation;
* compensation;
* migration;
* recovery choices;
* and purge warnings.

---

# 337. Prototype Plan

## 337.1 Prototype A — Durable Sequence

Three pure activities with crash after every transition.

---

## 337.2 Prototype B — Fenced Worker

Expire one lease, issue another and reject stale completion.

---

## 337.3 Prototype C — Idempotent Internal Write

Apply the same operation twice through a target inbox.

---

## 337.4 Prototype D — External Ambiguity

Simulate external success before Workflow result commit and reconcile.

---

## 337.5 Prototype E — Approval Wait

Restart Runtime and Windows while waiting.

---

## 337.6 Prototype F — Signal Deduplication

Deliver a signal twice before and after the wait.

---

## 337.7 Prototype G — Cancellation

Cooperative stop, forced worker termination and effect reconciliation.

---

## 337.8 Prototype H — Compensation

Apply and compensate a staged file change.

---

## 337.9 Prototype I — Product Update

Resume an old pinned plan after installing a new Definition revision.

---

## 337.10 Prototype J — Migration

Map a waiting old plan to a reviewed new plan.

---

## 337.11 Prototype K — AI and Tool Workflow

Use exact Context and Routing Decisions, then restart between steps.

---

## 337.12 Prototype L — Corruption Recovery

Corrupt projection and checkpoint, then replay event history.

---

# 338. Implementation Plan

1. Record founder review.
2. Define Workflow Definition schema.
3. Define Step Definition schema.
4. Define condition language.
5. Define Compiled Plan schema.
6. Implement deterministic Plan Compiler.
7. Define Workflow Event schemas.
8. Create Workflow SQLite schema.
9. Configure WAL and `synchronous=FULL`.
10. Implement event append and projection transaction.
11. Implement event hash chain.
12. Implement projection replay.
13. Implement Checkpoint Snapshot.
14. Implement ready queue.
15. Integrate Scheduler.
16. Define Activity Contract schema.
17. Define Activity Implementation registry.
18. Implement attempt and lease records.
19. Implement fencing tokens.
20. Implement worker protocol.
21. Define Side-Effect Class registry.
22. Implement Effect Identity.
23. Implement Side-Effect Intent.
24. Implement internal inbox and outbox.
25. Implement idempotent trusted-service activity.
26. Implement external-idempotency adapter.
27. Implement reconciliation.
28. Implement Outcome Unknown.
29. Define retry and timeout schemas.
30. Implement durable retry timers.
31. Implement cooperative cancellation.
32. Integrate supervisor termination.
33. Implement pause and resume.
34. Implement durable approval.
35. Implement signal inbox and deduplication.
36. Implement parallel fork and join.
37. Implement bounded loops.
38. Implement child workflows.
39. Implement resource locks.
40. Integrate Workspace and Patch activities.
41. Integrate Repository activities.
42. Integrate Build and Test.
43. Integrate Context and AI Router.
44. Integrate Local Model Runtime.
45. Integrate Tool Mediator.
46. Integrate Plugin Gateway.
47. Integrate MCP Gateway.
48. Define compensation policies.
49. Implement compensation state.
50. Define Workflow Migration Plan.
51. Implement migration dry run and commit.
52. Implement startup recovery.
53. Implement Recovery Decisions and Reports.
54. Implement backup and restore.
55. Implement archive, export, delete and purge.
56. Implement Trust Centre and Desktop UI.
57. Add crash-fault injection harness.
58. Add duplicate-effect adversarial suite.
59. Add stale-approval and wrong-project suites.
60. Run endurance and performance tests.
61. Complete security, privacy and recovery review.
62. Accept, amend or reject the ADR.

---

# 339. Owners

| Area                            | Owner                                 |
| ------------------------------- | ------------------------------------- |
| Product policy                  | Founder                               |
| Workflow Service                | Workflow Runtime Owner                |
| Workflow Definitions            | Workflow Architecture Owner           |
| Plan Compiler                   | Workflow Architecture Owner           |
| SQLite persistence              | Persistence Owner                     |
| Scheduler and resources         | Scheduler Owner                       |
| Workers and supervision         | Runtime Architecture Owner            |
| Activity contracts              | Owning Service and Workflow Owners    |
| Side effects and reconciliation | Workflow and Tool Mediation Owners    |
| Workspace and patch activities  | Workspace and Patch Owners            |
| Repository activities           | Repository Owner                      |
| Build and test activities       | Build and Test Owners                 |
| AI activities                   | Context Assembly and AI Router Owners |
| Local inference activities      | Local Model Runtime Owner             |
| Provider activities             | Provider Trust Owner                  |
| Plugin activities               | Plugin Platform Owner                 |
| MCP activities                  | MCP Gateway Owner                     |
| Approvals                       | Product and Security Owners           |
| Secrets                         | Secrets Owner                         |
| Compensation                    | Workflow and Owning Service Owners    |
| Migration                       | Workflow and Release Owners           |
| Recovery                        | Recovery Owner                        |
| Trust Centre                    | Trust Centre Owner                    |
| Desktop UI                      | Desktop Owner                         |
| Performance                     | Performance Owner                     |
| Adversarial tests               | Test Architecture Owner               |

---

# 340. Suggested Repository Structure

```text
src/
├── Workflow/
│   ├── Opure.Workflow.Contracts/
│   ├── Opure.Workflow.Service/
│   ├── Opure.Workflow.Definitions/
│   ├── Opure.Workflow.Compiler/
│   ├── Opure.Workflow.Interpreter/
│   ├── Opure.Workflow.Events/
│   ├── Opure.Workflow.Projections/
│   ├── Opure.Workflow.Checkpoints/
│   ├── Opure.Workflow.Scheduling/
│   ├── Opure.Workflow.Leases/
│   ├── Opure.Workflow.Activities/
│   ├── Opure.Workflow.Effects/
│   ├── Opure.Workflow.Retry/
│   ├── Opure.Workflow.Timers/
│   ├── Opure.Workflow.Signals/
│   ├── Opure.Workflow.Approvals/
│   ├── Opure.Workflow.Compensation/
│   ├── Opure.Workflow.Migrations/
│   ├── Opure.Workflow.Recovery/
│   ├── Opure.Workflow.Persistence/
│   └── Opure.Workflow.Security/
├── Worker/
│   └── Opure.Worker.Activity/
└── Desktop/
    └── Opure.Desktop.Workflow/

schemas/
└── workflow/
    ├── workflow-definition-v1.schema.json
    ├── compiled-workflow-plan-v1.schema.json
    ├── activity-contract-v1.schema.json
    ├── workflow-event-v1.schema.json
    ├── workflow-checkpoint-v1.schema.json
    ├── workflow-retry-policy-v1.schema.json
    ├── workflow-timeout-policy-v1.schema.json
    ├── workflow-approval-policy-v1.schema.json
    ├── workflow-signal-definition-v1.schema.json
    ├── workflow-compensation-policy-v1.schema.json
    ├── workflow-migration-plan-v1.schema.json
    └── workflow-recovery-decision-v1.schema.json

tests/
└── Workflow/
    ├── Opure.Workflow.UnitTests/
    ├── Opure.Workflow.PersistenceTests/
    ├── Opure.Workflow.CrashTests/
    ├── Opure.Workflow.SecurityTests/
    ├── Opure.Workflow.IntegrationTests/
    ├── Opure.Workflow.PerformanceTests/
    └── Fixtures/
        ├── Definitions/
        ├── Effects/
        ├── Crashes/
        ├── Migrations/
        └── Malicious/
```

Exact project count may be consolidated under ADR-0010.

---

# 341. Workflow Definition Sketch

```json
{
  "schema": "opure.workflow-definition/1",
  "id": "review-and-apply-patch",
  "revision": 1,
  "input_schema": "patch-request:1",
  "output_schema": "patch-workflow-result:1",
  "steps": [
    {
      "id": "assemble-context",
      "kind": "activity",
      "activity": "context.assemble:1"
    },
    {
      "id": "generate-patch",
      "kind": "activity",
      "activity": "ai.generate-patch:1",
      "depends_on": [
        "assemble-context"
      ]
    },
    {
      "id": "review",
      "kind": "human-approval",
      "depends_on": [
        "generate-patch"
      ]
    },
    {
      "id": "apply",
      "kind": "activity",
      "activity": "patch.apply:1",
      "depends_on": [
        "review"
      ]
    },
    {
      "id": "build",
      "kind": "activity",
      "activity": "build.run:1",
      "depends_on": [
        "apply"
      ]
    }
  ]
}
```

---

# 342. Compiled Plan Sketch

```json
{
  "schema": "opure.compiled-workflow-plan/1",
  "id": "plan-opaque",
  "definition": "review-and-apply-patch:1",
  "compiler": "workflow-compiler:1",
  "interpreter": "workflow-interpreter:1",
  "activities": [
    {
      "contract": "context.assemble:1",
      "implementation": "context-service:4"
    },
    {
      "contract": "ai.generate-patch:1",
      "implementation": "ai-router-adapter:3"
    },
    {
      "contract": "patch.apply:1",
      "implementation": "patch-service:2"
    },
    {
      "contract": "build.run:1",
      "implementation": "build-service:3"
    }
  ],
  "sha256": "..."
}
```

---

# 343. Workflow Event Sketch

```json
{
  "schema": "opure.workflow-event/1",
  "event_id": "event-opaque",
  "workflow": "workflow-opaque",
  "project": "project-opaque",
  "sequence": 42,
  "type": "effect-intent-recorded",
  "plan": "plan-opaque",
  "step": "apply",
  "attempt": "attempt-opaque",
  "actor": "workflow-service",
  "operation": "operation-opaque",
  "occurred_at": "2026-07-18T18:00:00Z",
  "payload_sha256": "...",
  "previous_event_sha256": "...",
  "sha256": "..."
}
```

---

# 344. Checkpoint Sketch

```json
{
  "schema": "opure.workflow-checkpoint/1",
  "id": "checkpoint-opaque",
  "workflow": "workflow-opaque",
  "plan": "plan-opaque",
  "event_sequence": 100,
  "state": "waiting-for-approval",
  "steps": {
    "assemble-context": "succeeded",
    "generate-patch": "succeeded",
    "review": "waiting",
    "apply": "blocked",
    "build": "blocked"
  },
  "approval": "approval-request-opaque",
  "event_tail_sha256": "...",
  "sha256": "..."
}
```

---

# 345. Activity Contract Sketch

```json
{
  "schema": "opure.activity-contract/1",
  "id": "patch.apply",
  "revision": 1,
  "owner": "patch-service",
  "input_schema": "approved-patch:1",
  "output_schema": "patch-application-result:1",
  "side_effect_class": "transactional-internal-write",
  "idempotency": "operation-inbox",
  "reconciliation": "patch-operation-lookup",
  "retry": "safe-after-reconciliation",
  "cancellation": "cooperative-before-commit",
  "compensation": "patch.revert:1"
}
```

---

# 346. Effect Intent Sketch

```json
{
  "schema": "opure.workflow-effect-intent/1",
  "id": "effect-intent-opaque",
  "effect": "effect-opaque",
  "workflow": "workflow-opaque",
  "step": "apply",
  "attempt": "attempt-opaque",
  "fencing_token": 3,
  "idempotency_key": "opure-effect-...",
  "operation_type": "patch-apply",
  "request_sha256": "...",
  "approval": "approval-opaque",
  "created_at": "2026-07-18T18:00:00Z"
}
```

---

# 347. Recovery Decision Sketch

```json
{
  "schema": "opure.workflow-recovery-decision/1",
  "id": "recovery-decision-opaque",
  "workflow": "workflow-opaque",
  "step": "push-release",
  "attempt": "attempt-opaque",
  "observed_state": "invocation-recorded-result-missing",
  "effect_state": "inconclusive",
  "classification": "outcome-unknown",
  "action": "await-developer-reconciliation",
  "evidence": [
    "provider-request-receipt-opaque"
  ],
  "created_at": "2026-07-18T18:00:00Z",
  "sha256": "..."
}
```

---

# 348. Migration Plan Sketch

```json
{
  "schema": "opure.workflow-migration-plan/1",
  "id": "migration-opaque",
  "source_plan": "plan-old",
  "target_plan": "plan-new",
  "eligible_states": [
    "paused",
    "waiting-for-approval"
  ],
  "step_mapping": {
    "generate": "generate",
    "review": "review",
    "apply": "apply"
  },
  "effect_policy": "preserve-completed-no-replay",
  "approval_policy": "refresh-if-preview-changed",
  "rollback": "source-plan-before-commit",
  "sha256": "..."
}
```

---

# 349. Release Gate

Durable workflow support is blocked when:

* workflow transitions can dispatch before commit;
* Workflow Definitions can contain arbitrary executable code;
* Compiled Plans are mutable;
* running workflows can silently adopt a new plan;
* an activity implementation can change without revision;
* Workflow Events can be updated in place;
* projections cannot be rebuilt;
* checkpoints are the sole state authority;
* checkpoints can contain secret values;
* the Workflow database uses a network filesystem;
* the authoritative database uses `synchronous=OFF`;
* `synchronous=NORMAL` is used without a reviewed durability decision;
* a worker can commit without a current fencing token;
* an expired worker can overwrite a newer result;
* side-effecting work can execute without a durable intent;
* retries create a new idempotency key for the same Logical Effect;
* unknown side-effect outcomes are retried automatically;
* universal exactly-once execution is claimed;
* an irreversible activity lacks explicit approval and recovery policy;
* cancellation is represented only by abandoning a wait;
* forced termination is treated as proof that no effect occurred;
* approval can survive a changed operation;
* signals have no deduplication identity;
* timers exist only in memory;
* retry delay exists only in memory;
* compensation erases original history;
* product updates remove implementations required by active plans;
* migrations can replay completed effects;
* AI fallback occurs without a new Routing Decision;
* plugin or MCP identity changes silently;
* provider-side hidden state is required to resume;
* startup recovery guesses an external outcome;
* project scope is not checked on every command and result;
* or Trust Centre cannot show the complete timeline, effects, approvals, retries and recovery state.

---

# 350. Acceptance Criteria

This ADR may move to **Accepted** when all of the following hold.

## Architecture and Ownership

* [ ] Workflow Service is authoritative for durable workflow state.
* [ ] Scheduler cannot mutate workflow state directly.
* [ ] Activity workers cannot access Workflow tables directly.
* [ ] Desktop cannot mutate workflow state directly.
* [ ] Models cannot mutate workflow state directly.
* [ ] Plugins cannot own workflow state.
* [ ] MCP servers cannot own workflow state.
* [ ] Provider conversations are not workflow state.
* [ ] Workflow Definitions are declarative.
* [ ] Arbitrary workflow scripts are absent.
* [ ] Arbitrary C# orchestration replay is absent.
* [ ] One service-owned database exists per channel.
* [ ] Every workflow is project scoped or explicitly System scoped.
* [ ] Stable, Preview and Development are isolated.
* [ ] Local offline execution works.

## SQLite Durability

* [ ] Workflow database uses a local fixed disk.
* [ ] Repository storage is denied.
* [ ] Network storage is denied.
* [ ] Cloud-synchronised storage is denied.
* [ ] WAL mode is enabled.
* [ ] `synchronous=FULL` is enabled.
* [ ] Foreign keys are enabled.
* [ ] Trusted schema is disabled where supported.
* [ ] One logical writer exists.
* [ ] Read transactions are bounded.
* [ ] WAL checkpointing is controlled.
* [ ] Checkpoint starvation is detected.
* [ ] Database configuration drift is detected.
* [ ] Event, projection and ready queue commit atomically.
* [ ] Event and effect intent commit atomically.
* [ ] External work never occurs inside a database transaction.
* [ ] Power-loss fault tests pass.

## Workflow Definitions

* [ ] Definition schema is versioned.
* [ ] Published revisions are immutable.
* [ ] Input schema is required.
* [ ] Output schema is required.
* [ ] Step IDs are unique.
* [ ] Step kinds are allowlisted.
* [ ] Dependency cycles are rejected.
* [ ] Unbounded loops are rejected.
* [ ] Child recursion is bounded.
* [ ] Join semantics are explicit.
* [ ] Side-effect policy is explicit.
* [ ] Retry policy is explicit.
* [ ] Timeout policy is explicit.
* [ ] Cancellation policy is explicit.
* [ ] Compensation policy is explicit.
* [ ] Approval policy is explicit.
* [ ] Resource policy is explicit.
* [ ] Terminal paths are validated.
* [ ] No executable code can be embedded.
* [ ] No dynamic assembly can be embedded.

## Plan Compilation

* [ ] Compiled Plan schema is versioned.
* [ ] Plan compilation is deterministic.
* [ ] Plan canonicalisation is deterministic.
* [ ] Plan SHA-256 is calculated.
* [ ] Definition revision is bound.
* [ ] Compiler revision is bound.
* [ ] Interpreter revision is bound.
* [ ] Activity Contract revisions are bound.
* [ ] Activity Implementation revisions are bound.
* [ ] Condition-language revision is bound.
* [ ] Context Policies are bound.
* [ ] Routing Policies are bound.
* [ ] Plugin packages are bound.
* [ ] MCP server identities are bound.
* [ ] Required capabilities are summarised.
* [ ] Side effects are summarised.
* [ ] Compensation graph is bound.
* [ ] Running instances pin the exact plan.
* [ ] Plan substitution is detected.

## Condition Language

* [ ] Condition language is typed.
* [ ] Evaluation is deterministic.
* [ ] Operations are bounded.
* [ ] Collection sizes are bounded.
* [ ] Expression depth is bounded.
* [ ] I/O is unavailable.
* [ ] Clock access is unavailable.
* [ ] Random access is unavailable.
* [ ] Network is unavailable.
* [ ] Process execution is unavailable.
* [ ] Reflection is unavailable.
* [ ] Recursion is unavailable.
* [ ] Recorded Clock Activity exists.
* [ ] Recorded Random Activity exists.
* [ ] Recorded Identifier Activity exists.
* [ ] Recorded values are reused after recovery.

## Workflow Instances

* [ ] Instance IDs are opaque.
* [ ] Project identity is required.
* [ ] Plan ID and hash are required.
* [ ] Inputs are immutable.
* [ ] Inputs are schema validated.
* [ ] Inputs are classified.
* [ ] Inputs are hashed.
* [ ] Start commands are idempotent.
* [ ] Duplicate matching starts return the original instance.
* [ ] Duplicate conflicting starts fail.
* [ ] Initial events and Ready steps commit before dispatch.
* [ ] Instance revision supports optimistic concurrency.
* [ ] Deadline is durable.
* [ ] Retention policy is bound.

## Events and Projections

* [ ] Workflow Events are append only.
* [ ] Every state transition has an event.
* [ ] Event payload schemas are versioned.
* [ ] Event sequence is monotonic.
* [ ] Event actor is recorded.
* [ ] Operation ID is recorded.
* [ ] Event time is UTC.
* [ ] Payload hash is recorded.
* [ ] Previous event hash is recorded.
* [ ] Event hash is recorded.
* [ ] Event insertion is detected.
* [ ] Event deletion is detected.
* [ ] Event reordering is detected.
* [ ] Current projection is derived.
* [ ] Step projection is derived.
* [ ] Projection updates commit with events.
* [ ] Projection corruption is repaired by replay.
* [ ] Event replay has no side effects.
* [ ] Invalid replay transitions quarantine the workflow.

## Checkpoints

* [ ] Checkpoint schema is versioned.
* [ ] Checkpoints are derived.
* [ ] Checkpoint binds workflow.
* [ ] Checkpoint binds plan.
* [ ] Checkpoint binds event sequence.
* [ ] Checkpoint binds event-tail hash.
* [ ] Checkpoint payload is hashed.
* [ ] Checkpoint contains step state.
* [ ] Checkpoint contains wait state.
* [ ] Checkpoint contains effect state.
* [ ] Checkpoint contains compensation state.
* [ ] Large outputs remain referenced.
* [ ] Secrets are excluded.
* [ ] Process handles are excluded.
* [ ] CancellationToken objects are excluded.
* [ ] Provider hidden state is excluded.
* [ ] Invalid latest checkpoint falls back to earlier history.
* [ ] Replay from genesis works.
* [ ] Snapshot retention is bounded.

## Scheduling and Ready Queue

* [ ] Ready queue is durable.
* [ ] Step Ready event and queue row commit together.
* [ ] Duplicate queue rows are prevented.
* [ ] Queue claims are transactional.
* [ ] Paused workflows cannot claim.
* [ ] Cancelling workflows cannot claim ordinary steps.
* [ ] `ready_at` is honoured.
* [ ] Resource admission is checked.
* [ ] Workflow priority is policy controlled.
* [ ] Project fairness is implemented.
* [ ] Queue backpressure is visible.
* [ ] Durable input is never dropped silently.

## Activity Contracts

* [ ] Activity Contract schema is versioned.
* [ ] Owner service is explicit.
* [ ] Input schema is explicit.
* [ ] Output schema is explicit.
* [ ] Side-Effect Class is explicit.
* [ ] Idempotency contract is explicit.
* [ ] Reconciliation contract is explicit.
* [ ] Retry contract is explicit.
* [ ] Timeout contract is explicit.
* [ ] Cancellation contract is explicit.
* [ ] Compensation contract is explicit.
* [ ] Capability contract is explicit.
* [ ] Data policy is explicit.
* [ ] Resource profile is explicit.
* [ ] Unknown Side-Effect Class is denied.
* [ ] Old implementation support is tracked.
* [ ] Missing implementation pauses affected workflow.

## Attempts and Leases

* [ ] Attempt IDs are unique.
* [ ] Attempt numbers are monotonic.
* [ ] Lease acquisition is transactional.
* [ ] Lease binds worker identity.
* [ ] Lease binds Runtime boot identity.
* [ ] Lease binds process identity beyond PID.
* [ ] Lease has expiry.
* [ ] Lease has heartbeat.
* [ ] Lease has fencing token.
* [ ] Fencing token is monotonic per Step Instance.
* [ ] Attempt Started is recorded.
* [ ] Heartbeat content is bounded.
* [ ] Lease expiry triggers investigation.
* [ ] Lease expiry does not prove no effect.
* [ ] New lease fences stale worker.
* [ ] Stale completion is rejected.
* [ ] Duplicate matching completion returns original result.
* [ ] Duplicate differing completion quarantines.
* [ ] Forced worker termination is supervised.
* [ ] Worker crash matrix passes.

## Side-Effect Classification

* [ ] Pure Deterministic is supported.
* [ ] Pure Nondeterministic Read is supported.
* [ ] Local Read is supported.
* [ ] External Read is supported.
* [ ] Transactional Internal Write is supported.
* [ ] Idempotent Write is supported.
* [ ] External Idempotency Key is supported.
* [ ] Reconciliable Write is supported.
* [ ] Compensatable Write is supported.
* [ ] Irreversible Write is supported with restrictions.
* [ ] Human Action is supported.
* [ ] Unknown or Prohibited is denied.
* [ ] Multi-effect activities return an effect ledger.
* [ ] One coherent logical effect per activity is preferred.

## Effect Identity and Intent

* [ ] Every Logical Effect has a stable identity.
* [ ] Effect identity includes workflow.
* [ ] Effect identity includes Step Instance.
* [ ] Effect identity includes effect slot.
* [ ] Effect identity includes plan revision.
* [ ] Effect identity includes logical generation.
* [ ] Retry uses the same identity.
* [ ] Deliberate new effect uses new generation.
* [ ] Idempotency key contains no secret.
* [ ] Idempotency key adaptation is deterministic.
* [ ] Side-Effect Intent is committed before invocation.
* [ ] Intent binds attempt and fencing token.
* [ ] Intent binds request hash.
* [ ] Intent binds capability.
* [ ] Intent binds approval.
* [ ] Intent is not displayed as success.
* [ ] Missing intent is a contract violation.

## Internal and External Effects

* [ ] Trusted services accept operation IDs where selected.
* [ ] Trusted target inbox deduplicates.
* [ ] Duplicate same request returns original result.
* [ ] Duplicate different request fails.
* [ ] Cross-database atomicity is not assumed.
* [ ] Outbox and inbox patterns are used.
* [ ] Goal-seeking idempotent activities exist.
* [ ] External idempotency-key terms are documented.
* [ ] External idempotency retention is documented.
* [ ] Expired key windows block unsafe retry.
* [ ] Reconciliation contracts are versioned.
* [ ] Provider receipts are recorded safely.
* [ ] External-resource identity is recorded.
* [ ] Weak heuristic reconciliation cannot authorise unsafe retry.
* [ ] Irreversible effects require approval.
* [ ] Irreversible effects have no automatic retry.
* [ ] Universal exactly-once claims are absent.

## Outcome Unknown

* [ ] Outcome Unknown is first class.
* [ ] It can result from crash ambiguity.
* [ ] It stops automatic retry.
* [ ] It stops ordinary completion.
* [ ] It shows last durable state.
* [ ] It shows effect intent.
* [ ] It shows invocation evidence.
* [ ] It shows reconciliation evidence.
* [ ] Reconcile Again is available.
* [ ] Mark Applied requires evidence.
* [ ] Mark Not Applied requires evidence.
* [ ] Compensation is available where defined.
* [ ] New Effect requires explicit decision.
* [ ] Abandon is explicit.
* [ ] Human resolution is evented.
* [ ] Model output alone cannot resolve it.

## Retries

* [ ] Retry Policy schema is versioned.
* [ ] Maximum attempts is finite.
* [ ] Initial delay is explicit.
* [ ] Backoff factor is explicit.
* [ ] Maximum delay is explicit.
* [ ] Jitter is deterministic.
* [ ] Retryable classes are explicit.
* [ ] Non-retryable classes are explicit.
* [ ] Overall retry budget is explicit.
* [ ] Cost budget is supported.
* [ ] Provider-call budget is supported.
* [ ] Retry due time is durable.
* [ ] Retry survives restart.
* [ ] Policy denial is not retried.
* [ ] Invalid input is not retried.
* [ ] Authentication requiring user action is not retried blindly.
* [ ] Secret violation is not retried.
* [ ] Wrong project is not retried.
* [ ] Cancellation is not retried.
* [ ] Outcome Unknown is not retried.
* [ ] Unknown side-effect failure is non-retryable by default.
* [ ] Retry-storm controls pass.

## Timers and Timeouts

* [ ] Timeout Policy schema is versioned.
* [ ] Queue timeout exists.
* [ ] Start timeout exists.
* [ ] Execution timeout exists.
* [ ] Heartbeat timeout exists.
* [ ] Provider timeout exists.
* [ ] Tool timeout exists.
* [ ] Approval timeout exists.
* [ ] Signal timeout exists.
* [ ] Workflow deadline exists.
* [ ] Cancellation grace exists.
* [ ] Forced-termination grace exists.
* [ ] Durable Timer schema exists.
* [ ] Timers are persisted.
* [ ] Timer firing is idempotent.
* [ ] Timer transition is transactional.
* [ ] Early wake does not fire early.
* [ ] Late wake is recorded.
* [ ] Clock rollback does not duplicate.
* [ ] Due timers recover after restart.
* [ ] Million-timer scale evidence exists.

## Cancellation and Pause

* [ ] Cancellation request is durable.
* [ ] Cancellation scope is explicit.
* [ ] Cancellation mode is explicit.
* [ ] Pending steps stop scheduling.
* [ ] Active steps receive cooperative cancellation.
* [ ] CancellationToken is propagated where supported.
* [ ] CancellationToken is not durable state.
* [ ] Activity acknowledgement is recorded.
* [ ] Cancel Operation is distinguished.
* [ ] Cancel Wait is distinguished.
* [ ] Cancel Both is distinguished.
* [ ] Wait-only cancellation records continuing work.
* [ ] Forced worker termination is available.
* [ ] Forced termination triggers reconciliation.
* [ ] Unsafe thread abort is absent.
* [ ] Pause request is durable.
* [ ] Safe-point pause is default.
* [ ] Immediate pause behaviour is explicit.
* [ ] Resume revalidates all material state.
* [ ] Changed policy can block resume.

## Approvals

* [ ] Approval Policy schema is versioned.
* [ ] Approval Request is durable.
* [ ] Request binds workflow and step.
* [ ] Request binds plan.
* [ ] Request binds exact preview.
* [ ] Request binds operation.
* [ ] Request binds source hashes.
* [ ] Request binds Context Plan where relevant.
* [ ] Request binds Routing Decision where relevant.
* [ ] Request binds provider and model.
* [ ] Request binds tool and command.
* [ ] Request binds effects.
* [ ] Request binds estimated cost.
* [ ] Request binds expiry.
* [ ] Request contains no secret.
* [ ] Approval identity is verified.
* [ ] Approval capability is one time.
* [ ] Approval decision is append only.
* [ ] Changed operation invalidates approval.
* [ ] Expired approval is denied.
* [ ] Rejection is terminal or follows explicit policy.
* [ ] Approval survives restart without changing reviewed bytes.
* [ ] Approval impersonation tests pass.

## External Signals

* [ ] Signal Definition schema is versioned.
* [ ] Signal type is allowlisted.
* [ ] Sender is capability bound.
* [ ] Payload schema is validated.
* [ ] Payload size is bounded.
* [ ] Payload is classified.
* [ ] Signal ID is required.
* [ ] Deduplication key is required.
* [ ] Sender sequence is supported where needed.
* [ ] Duplicate matching signal is idempotent.
* [ ] Duplicate differing payload is quarantined.
* [ ] Pre-wait signal is persisted.
* [ ] Unexpected signal is rejected or quarantined.
* [ ] Inbox capacity is bounded.
* [ ] Overflow is visible.
* [ ] Signal consumption and workflow transition are atomic.
* [ ] Model cannot send a direct signal.
* [ ] Cross-project signal is denied.

## Parallelism, Loops and Children

* [ ] Parallel Fork is supported.
* [ ] Per-workflow parallelism is bounded.
* [ ] Project parallelism is bounded.
* [ ] Incompatible writes cannot run concurrently.
* [ ] Join All is supported.
* [ ] Join Any is supported.
* [ ] First Successful is supported.
* [ ] Quorum is supported.
* [ ] Join condition is deterministic.
* [ ] Simultaneous completion has deterministic tie-breaking.
* [ ] Losing-branch cancellation is explicit.
* [ ] Bounded Loop requires maximum iterations.
* [ ] Iteration identity is durable.
* [ ] Loop boundary checkpoints work.
* [ ] Child Workflow is independent.
* [ ] Parent-child link is durable.
* [ ] Child start is idempotent.
* [ ] Cancellation propagation is explicit.
* [ ] Failure propagation is explicit.
* [ ] Compensation ownership is explicit.
* [ ] Shared mutable child state is absent.

## Resource and Project Writes

* [ ] Durable logical locks are supported.
* [ ] Lock acquisition is canonical.
* [ ] Lock leases are fenced.
* [ ] OS mutex is not durable authority.
* [ ] Workspace write binds exact source.
* [ ] Workspace write uses staged writes.
* [ ] Workspace write uses fencing.
* [ ] Workspace write returns receipt.
* [ ] Patch generation is separate from application.
* [ ] Patch application binds exact base.
* [ ] Repository commit binds exact tree.
* [ ] Repository push is reconciliable.
* [ ] Build uses isolated output.
* [ ] Test cancellation is supported.
* [ ] Source state is revalidated before resume.

## AI, Tools, Plugins and MCP

* [ ] AI activity binds Context Plan.
* [ ] AI activity binds Routing Decision.
* [ ] AI activity binds Data Sharing Plan where remote.
* [ ] AI activity binds exact Execution Profile.
* [ ] AI activity output is validated.
* [ ] AI retry policy states whether different output is acceptable.
* [ ] AI fallback creates a new Routing Decision.
* [ ] Provider hidden conversation is not required.
* [ ] Tool activity uses Tool Mediator.
* [ ] Command execution is treated as side effect.
* [ ] Plugin activity pins package and hash.
* [ ] Plugin capability is checked.
* [ ] Plugin upgrade pauses pinned workflows where incompatible.
* [ ] MCP activity pins server fingerprint.
* [ ] MCP account is bound.
* [ ] MCP data sharing is checked.
* [ ] MCP reconnect does not imply safe retry.
* [ ] Network access uses approved adapter.

## Compensation

* [ ] Compensation Policy schema is versioned.
* [ ] Compensation triggers are explicit.
* [ ] Eligible effects are explicit.
* [ ] Ordering is explicit.
* [ ] Compensation activity is versioned.
* [ ] Compensation is idempotent or reconciliable.
* [ ] Compensation retry is bounded.
* [ ] Compensation timeout is explicit.
* [ ] Compensation approval is supported.
* [ ] Compensation is recorded as new events.
* [ ] Original effect remains visible.
* [ ] Compensation failure is visible.
* [ ] Compensation Outcome Unknown is supported.
* [ ] Manual Remediation is supported.
* [ ] Irreversible effect has no pretend compensation.
* [ ] Workflow completion waits for mandatory compensation resolution.

## Versioning and Migration

* [ ] Running instance pins Definition revision.
* [ ] Running instance pins Compiled Plan.
* [ ] Running instance pins activity contracts.
* [ ] Running instance pins activity implementations.
* [ ] Running instance pins plugin packages.
* [ ] Running instance pins MCP identities.
* [ ] Running instance pins Context and Routing Policies.
* [ ] Product update retains old support or requires migration.
* [ ] Removal of required implementation is release blocked.
* [ ] Workflow Migration Plan schema exists.
* [ ] Migration source plan is exact.
* [ ] Migration target plan is exact.
* [ ] Eligible states are explicit.
* [ ] Step mapping is explicit.
* [ ] Output mapping is explicit.
* [ ] Timer mapping is explicit.
* [ ] Signal mapping is explicit.
* [ ] Approval mapping is explicit.
* [ ] Effect mapping is explicit.
* [ ] Compensation mapping is explicit.
* [ ] Migration dry run exists.
* [ ] Pre-migration checkpoint exists.
* [ ] Migration transaction is atomic.
* [ ] Completed external effects are not replayed.
* [ ] Material operation changes require fresh approval.
* [ ] Failed migration leaves source plan authoritative.

## Outbox and Inbox

* [ ] Workflow outbox is transactional.
* [ ] Outbox messages have idempotency keys.
* [ ] Destination inbox deduplicates.
* [ ] Result inbox deduplicates.
* [ ] Duplicate same payload returns original result.
* [ ] Duplicate different payload quarantines.
* [ ] Outbox retries are bounded.
* [ ] Cross-service distributed transaction is not claimed.
* [ ] Outbox and inbox recover after restart.
* [ ] Child completion uses durable messaging.

## Startup Recovery

* [ ] SQLite recovery occurs before workflow resume.
* [ ] Database schema is validated.
* [ ] Quick integrity check runs.
* [ ] Foreign keys are checked.
* [ ] Event-chain tails are checked.
* [ ] Projection mismatch is repaired.
* [ ] Non-terminal instances are discovered.
* [ ] Leases are reconciled.
* [ ] Workers are inspected.
* [ ] Side effects are reconciled.
* [ ] Safe retries are requeued.
* [ ] Unsafe effects are not retried.
* [ ] Timers are restored.
* [ ] Approvals are restored.
* [ ] Signals are restored.
* [ ] Plans are validated.
* [ ] Activity implementations are validated.
* [ ] Project state is validated.
* [ ] Capabilities are reacquired.
* [ ] Credentials are reacquired.
* [ ] Recovery Report is created.
* [ ] Automatic resume occurs only when safe.
* [ ] Recovery Required state is actionable.
* [ ] Windows restart recovery passes.
* [ ] Product update recovery passes.

## Recovery Classification

* [ ] Safe to Resume works.
* [ ] Safe to Retry works.
* [ ] Needs Reconciliation works.
* [ ] Needs Fresh Approval works.
* [ ] Needs Replanning works.
* [ ] Implementation Unavailable works.
* [ ] Source Changed works.
* [ ] Project Unavailable works.
* [ ] Credential Required works.
* [ ] Outcome Unknown works.
* [ ] Corrupted works.
* [ ] Manual Recovery Required works.
* [ ] Recovery Decisions are hashed.
* [ ] Human recovery actions are evented.
* [ ] Models cannot authoritatively resolve recovery.

## Retention, Backup and Deletion

* [ ] Retention Policy schema exists.
* [ ] Non-terminal history is retained.
* [ ] Terminal retention is explicit.
* [ ] Checkpoint retention is bounded.
* [ ] Approval retention is explicit.
* [ ] Signal retention is explicit.
* [ ] Recovery retention is explicit.
* [ ] Active CAS references are retained.
* [ ] SQLite online backup works.
* [ ] Backup includes plans.
* [ ] Backup includes active artefact manifests.
* [ ] Backup includes integrity evidence.
* [ ] Restore validates external effects.
* [ ] Restore on another machine requires reconciliation.
* [ ] Project deletion handles active workflows.
* [ ] Project close behaviour is explicit.
* [ ] Archive is read only.
* [ ] Export is reviewable.
* [ ] Import cannot resume foreign active state automatically.
* [ ] Purge removes controlled payloads.
* [ ] Tombstones contain no secret.
* [ ] Forensic-deletion limitations are displayed.

## Trust and User Experience

* [ ] Trust Centre shows active workflows.
* [ ] Trust Centre shows waits.
* [ ] Trust Centre shows attempts.
* [ ] Trust Centre shows leases.
* [ ] Trust Centre shows effects.
* [ ] Trust Centre shows retries.
* [ ] Trust Centre shows timers.
* [ ] Trust Centre shows signals.
* [ ] Trust Centre shows approvals.
* [ ] Trust Centre shows compensation.
* [ ] Trust Centre shows migration.
* [ ] Trust Centre shows recovery.
* [ ] Outcome Unknown explanation is clear.
* [ ] Retry explanation is clear.
* [ ] Compensation limitations are clear.
* [ ] Migration effects are clear.
* [ ] Pause and cancellation semantics are clear.
* [ ] UI is accessible.
* [ ] Diagnostics exclude secrets.
* [ ] Metrics remain low cardinality.
* [ ] Security review is complete.
* [ ] Privacy review is complete.
* [ ] Recovery review is complete.
* [ ] Founder approval is recorded.

---

# 351. Evidence Required Before Acceptance

* [ ] Workflow Service contract.
* [ ] Workflow Definition schema.
* [ ] Step Definition schema.
* [ ] condition-language specification.
* [ ] Compiled Plan schema.
* [ ] Activity Contract schema.
* [ ] Activity Implementation schema.
* [ ] Workflow Event schemas.
* [ ] Checkpoint schema.
* [ ] Retry Policy schema.
* [ ] Timeout Policy schema.
* [ ] Approval Policy schema.
* [ ] Signal Definition schema.
* [ ] Compensation Policy schema.
* [ ] Migration Plan schema.
* [ ] Recovery Decision schema.
* [ ] Retention Policy schema.
* [ ] SQLite configuration report.
* [ ] `synchronous=FULL` durability report.
* [ ] SQLite crash and power-loss report.
* [ ] deterministic-plan compilation report.
* [ ] arbitrary-code denial report.
* [ ] condition-language fuzz report.
* [ ] recorded-nondeterminism report.
* [ ] event-immutability report.
* [ ] event-chain report.
* [ ] projection-replay report.
* [ ] no-side-effect replay report.
* [ ] checkpoint validation report.
* [ ] checkpoint secret-canary report.
* [ ] durable-ready-queue report.
* [ ] concurrent queue-claim report.
* [ ] scheduler fairness report.
* [ ] activity-version report.
* [ ] lease and fencing report.
* [ ] stale-worker rejection report.
* [ ] worker crash matrix.
* [ ] Side-Effect Class report.
* [ ] Effect Identity and idempotency report.
* [ ] Side-Effect Intent report.
* [ ] target inbox deduplication report.
* [ ] external-idempotency report.
* [ ] reconciliation report.
* [ ] false-reconciliation report.
* [ ] Outcome Unknown report.
* [ ] retry-policy report.
* [ ] deterministic-jitter report.
* [ ] retry-storm report.
* [ ] durable-timer report.
* [ ] clock-change report.
* [ ] cooperative-cancellation report.
* [ ] cancel-wait report.
* [ ] forced-worker-termination report.
* [ ] pause and resume report.
* [ ] approval binding report.
* [ ] stale-approval report.
* [ ] approval impersonation report.
* [ ] signal deduplication report.
* [ ] pre-wait signal report.
* [ ] malicious-signal report.
* [ ] parallel-join determinism report.
* [ ] bounded-loop report.
* [ ] child-workflow report.
* [ ] resource-lock fencing report.
* [ ] Workspace write recovery report.
* [ ] patch workflow report.
* [ ] repository push reconciliation report.
* [ ] build and test recovery report.
* [ ] AI workflow report.
* [ ] AI fallback replanning report.
* [ ] plugin pinning report.
* [ ] MCP fingerprint report.
* [ ] compensation report.
* [ ] compensation-failure report.
* [ ] manual-remediation report.
* [ ] old-plan support report.
* [ ] product-update resume report.
* [ ] migration dry-run report.
* [ ] migration crash report.
* [ ] no-effect-replay migration report.
* [ ] outbox and inbox report.
* [ ] startup-recovery report.
* [ ] automatic-resume safety report.
* [ ] source-revalidation report.
* [ ] provider-revalidation report.
* [ ] capability-revalidation report.
* [ ] database-corruption rehearsal.
* [ ] projection-corruption rehearsal.
* [ ] CAS-corruption rehearsal.
* [ ] backup and restore report.
* [ ] Windows shutdown report.
* [ ] power-loss simulation report.
* [ ] project-close report.
* [ ] export and import report.
* [ ] purge report.
* [ ] no-hidden-provider-state report.
* [ ] wrong-project adversarial report.
* [ ] duplicate-effect adversarial report.
* [ ] cancellation-race report.
* [ ] version-drift adversarial report.
* [ ] endurance report.
* [ ] performance report.
* [ ] scale report.
* [ ] accessibility report.
* [ ] security review.
* [ ] privacy review.
* [ ] recovery review.
* [ ] founder approval.

---

# 352. Known Limitations

* Multi-machine distributed scheduling is unavailable.
* Clustered leader election is unavailable.
* Cross-device workflow continuation is unavailable.
* Cloud-hosted workflow authority is unavailable.
* Universal exactly-once external effects are impossible.
* External idempotency depends on provider guarantees.
* Some external effects cannot be reconciled.
* Compensation may not reverse all consequences.
* Compensation can fail.
* Forced worker termination can leave ambiguous external state.
* Cooperative cancellation depends on activity support.
* A non-cancellable external API may continue after the workflow stops waiting.
* SQLite permits one writer.
* `synchronous=FULL` adds commit latency.
* WAL requires local same-host storage.
* Severe disk corruption may require backup restoration.
* Event hash chains do not defeat a full same-user rewrite.
* Same-user malware can access user-owned workflow files.
* Checkpoints can accelerate replay but do not eliminate long histories.
* Event compaction is deferred.
* Old activity implementations may increase release size.
* Workflow migrations require careful product maintenance.
* A missing old plugin may block a workflow.
* A changed MCP server may block a workflow.
* Provider and model changes may block remote steps.
* Human approvals can remain unresolved indefinitely.
* Signal inboxes require capacity limits.
* Large timer sets require efficient indexing.
* Retry policy can still overload a failing dependency if configured poorly.
* Deterministic condition language is less flexible than arbitrary code.
* Visual workflow editing is not selected.
* User-authored custom activities are unavailable.
* User-authored scripts are unavailable as direct workflow steps.
* Dynamic workflow mutation is unavailable.
* Workflow graph changes require migration or a new instance.
* Restoring a workflow on another machine can require extensive reconciliation.
* Forensic secure deletion is not guaranteed.
* The initial retention values are provisional.
* The initial performance and scale targets require evidence.

---

# 353. Open Questions

* Which exact SQLite release should Workflow Service pin?
* Which `Microsoft.Data.Sqlite` package revision should be used?
* Should the Workflow database use a custom SQLite build?
* Which WAL checkpoint mode is selected?
* What maximum WAL size is acceptable?
* How often should manual checkpoints run?
* How are long read transactions detected?
* Should Workflow use one database per channel or one per project?
* Does one database per channel create unacceptable project-isolation risk?
* Would a global control database plus project databases be preferable?
* How would atomic ready-queue claims work across project databases?
* Should System workflows use a separate database?
* What database page size is appropriate?
* Should database-level `secure_delete` be enabled?
* How are backup schedules selected?
* Should active Workflow database backups be encrypted?
* Which backup key policy applies?
* How are CAS artefacts included consistently in backup?
* Should event payloads be inline or always CAS after a threshold?
* What inline payload threshold is appropriate?
* Which canonical JSON implementation is selected?
* How are decimal values canonicalised?
* How are UTC instants canonicalised?
* Which opaque ID format is selected?
* Should ULID ordering be avoided to reduce information leakage?
* How are event IDs generated?
* Should event chains be per instance only or also global?
* Should event-chain roots be periodically signed?
* Should release signing keys ever sign workflow archives?
* How frequently should full event-chain verification run?
* Which event payloads require dedicated schemas?
* Can minor event fields be extended compatibly?
* How are unknown event fields preserved?
* Should events be stored as JSON, protobuf or relational columns plus payload?
* How is event replay tested across product versions?
* How long may event histories grow?
* Should terminal histories be compacted?
* Can a verified terminal summary replace some progress events?
* Which events must always remain?
* Should heartbeats be events or mutable attempt records?
* How many heartbeats are retained?
* Should progress milestones be events?
* What checkpoint event count is optimal?
* Are 100 events appropriate?
* Should checkpoints be synchronous or asynchronous?
* How many prior checkpoints should be retained?
* Should terminal checkpoints be retained permanently with the workflow?
* Should checkpoints be compressed?
* Which compression format is selected?
* Should checkpoint payloads be encrypted?
* How are encrypted checkpoint keys rotated?
* Which fields are forbidden in checkpoints?
* How is the prohibition tested across new activity types?
* Should Workflow Definitions be stored only in SQLite or also as files?
* How are user-created definitions exported?
* Which Definition authoring UI is selected?
* Should a YAML representation be supported?
* Would JSON be safer and easier to canonicalise?
* How are comments preserved in definitions?
* How are Definition diffs shown?
* Which step kinds belong in Version 1?
* Should Map or ForEach be a separate bounded step kind?
* How are dynamic collections bounded?
* Can parallel branch counts depend on a recorded input?
* What maximum branch count is safe?
* What maximum workflow depth is safe?
* What maximum child-workflow depth is safe?
* How are cyclic child references detected across definitions?
* What expression language implementation is selected?
* Should CEL be evaluated?
* Should JSON Logic be evaluated?
* Is a first-party typed AST preferable?
* Which string operations are allowed?
* Is bounded regex required?
* How are numeric overflows handled?
* How are decimal comparisons handled?
* How are missing fields handled?
* How is condition bytecode versioned?
* Can model-generated conditions ever be accepted after review?
* How are recorded clock values named and reused?
* Which randomness APIs are supported?
* Should random output be cryptographic?
* How are deterministic test clocks injected?
* Which values belong in Workflow Input versus a first activity?
* How are mutable project references represented?
* Should Workflow Inputs contain content or only references?
* Which classifications may be inline?
* How is personal data handled?
* Which workflow categories require encryption at rest?
* Which retention policy applies to AI outputs?
* Which retention policy applies to failed patches?
* How are unreviewed outputs deleted?
* Which activity granularity is appropriate?
* How are activities prevented from becoming mini-workflows?
* Should every activity expose a reconciliation operation?
* Which activities may declare no reconciliation?
* How are Activity Contracts source controlled?
* Who approves a new Side-Effect Class?
* Can one activity have several sub-effects?
* When must a multi-effect activity be split?
* How are partial outputs represented?
* How are activity progress units standardised?
* Which worker types are needed?
* Should pure activities run in-process?
* Should all activities run out of process for recovery isolation?
* What activity-risk threshold requires an isolated worker?
* How are worker packages pinned?
* How are old worker implementations distributed after update?
* How long must old implementations remain installed?
* Can active workflows block uninstall of an old version?
* How are worker process identities generated?
* Which supervisor API exposes process-start identity?
* How are PID reuse and Runtime restart handled?
* What lease duration is appropriate?
* What heartbeat frequency is appropriate?
* Should leases extend during known long blocking calls?
* How does a worker heartbeat while blocked in a provider SDK?
* Should the adapter own a separate heartbeat loop?
* What happens if database heartbeats fail but worker is healthy?
* How long before another lease is issued?
* Which activities support target-side fencing?
* How are fencing tokens represented across service contracts?
* Can an external HTTP API accept a fencing token?
* Should fencing be placed in a request header?
* How are fencing tokens prevented from leaking project information?
* How are Effect Identities canonicalised?
* Which namespace protects against collision?
* How are external idempotency-key length limits handled?
* Which providers support idempotency keys?
* How long do providers retain them?
* How is provider evidence monitored?
* What happens when provider idempotency terms change?
* Should the same Logical Effect be retried after the provider key window?
* Which effect requires a new generation?
* How does the UI explain effect generation?
* How are target resource identifiers stored?
* How are resource IDs classified?
* Which receipt fields are safe?
* How are response bodies redacted?
* Should Side-Effect Intent always receive its own event?
* Should invocation evidence receive its own event?
* How is the transport-boundary crash window narrowed?
* Can trusted target services pull commands from Workflow outbox rather than be called?
* Would a single local message bus reduce ambiguity?
* How is outbox dispatch prioritised?
* How many duplicate deliveries are permitted?
* Which inbox retention is required?
* How are target inboxes cleaned without breaking deduplication?
* Which target operations can provide indefinite operation lookup?
* How are operation-lookup indexes stored?
* Which reconciliation outcomes are sufficient for automatic resume?
* Can human evidence mark an effect applied?
* What evidence level is required?
* Should two approvals be required for critical Outcome Unknown resolution?
* How are reconciliation actions themselves retried?
* Can reconciliation have side effects?
* How are weak heuristic matches shown?
* Which effects must always stop as Outcome Unknown?
* How are user-facing recovery choices ordered?
* Should a user be allowed to force retry an irreversible effect?
* What warning and confirmation would be required?
* Is force retry prohibited entirely for some effect classes?
* Which retry-failure taxonomy is final?
* How are HTTP statuses mapped?
* How are gRPC statuses mapped?
* How are provider-specific errors mapped?
* How are local OOM errors mapped?
* How are disk-full errors mapped?
* Which retry policies are product defaults?
* Which activities may override defaults?
* What maximum attempts are acceptable?
* Which backoff factor is selected?
* Which deterministic jitter function is selected?
* How are retry budgets aggregated across child workflows?
* How is workflow-level cost budget represented?
* How is provider token use reconciled?
* How are retry storms prevented during application restart?
* Should recovered retries be rate limited gradually?
* How are due timers prioritised after long downtime?
* Which timer index schema scales to one million?
* Should there be one in-memory timer wheel?
* How are timer clock changes detected?
* Should Windows time-zone changes matter?
* How are daylight-saving changes displayed?
* How is UTC clock rollback handled?
* Should an external trusted time source be supported?
* Which cancellation modes are shown to users?
* What is the default cancellation grace?
* What is the forced-termination grace?
* Which activities may ignore emergency termination?
* Can an in-process activity ever be forcibly stopped safely?
* Should all side-effecting activities be out of process?
* How are non-cancellable provider calls handled?
* How long does Workflow wait after cancel-wait?
* How are detached underlying calls reconciled?
* How are partial provider charges shown?
* Should cancellation trigger compensation automatically?
* Which workflow states permit Pause?
* Should timers continue while Paused?
* Should approval expiry continue while Paused?
* Should workflow deadline continue while Paused?
* How are long maintenance pauses handled?
* Should a pause acquire a checkpoint immediately?
* How is resume policy explained?
* Which source changes require replanning versus retry?
* How are approval previews canonicalised?
* How are patch previews stored?
* How are command arguments displayed safely?
* How are hidden environment variables excluded?
* Which approval changes are material?
* Is an updated cost estimate alone material?
* How much cost drift invalidates approval?
* Does a provider latency change invalidate approval?
* Should approvals be one-time or scoped to a small group of equivalent attempts?
* How are approver roles represented for a single founder?
* Which actions require two approvals in a future team?
* How are approval notifications delivered?
* How are approval requests protected from phishing-like UI?
* How are stale approvals grouped?
* Which signal sources are permitted?
* Can Google Calendar or Gmail events signal workflows in future?
* How are connector signals mapped to typed definitions?
* How are event-triggered webhooks handled when webhook automations are unavailable?
* Should signals be buffered before a workflow exists?
* How are unknown workflow IDs handled?
* How are signal inbox limits selected?
* Which ordering policies are needed?
* How long are signals retained?
* How are expired signals represented?
* How are malicious signal payloads scanned?
* Can signal payloads reference large CAS artefacts?
* How is signal delivery acknowledged?
* Which parallel join policies belong in Version 1?
* How are failures aggregated at joins?
* How is First Successful tie-breaking defined?
* What quorum forms are supported?
* How are losing branches cancelled safely?
* Can losing branches contain side effects?
* Should side-effecting race branches be prohibited?
* How is loop output aggregated?
* How are large iteration sets handled?
* Should a map operation create child workflows?
* How are child-workflow failures shown in the parent?
* Who owns child compensation?
* Can a child outlive a cancelled parent?
* How are orphan children detected?
* How are resource locks modelled?
* Should locks be rows in Workflow database or owned by target services?
* How are cross-workflow repository locks fenced?
* How is deadlock avoidance enforced?
* What maximum lock lease is permitted?
* How are read versus write locks represented?
* Can multiple read workflows share a workspace snapshot?
* How are file-set locks canonicalised?
* Which Workspace operations are idempotent?
* How does staged write reconciliation work after power loss?
* How are patch-revert compensations generated and approved?
* Can repository commits be compensated safely?
* How are pushes reconciled across remotes?
* Should a push ever retry automatically?
* How are release tags handled?
* How are package publications handled?
* Which build outputs are retained?
* How are build worktrees cleaned after worker loss?
* How are test processes terminated?
* Which AI tasks permit retry with a different answer?
* Should patch-generation retry preserve previous response as evidence?
* How are partial AI streams retained?
* Can a provider request be resumed?
* How are duplicate provider charges surfaced?
* When must an AI retry ask the developer?
* How are model-routing drift and workflow plan pinning reconciled?
* Does a running workflow pin a Routing Policy or exact Routing Decision only at each step?
* Can a future step use a newly qualified model?
* When does that require migration?
* How are local model files retained for old plans?
* How are plugin packages retained?
* Can plugin uninstall be blocked by active workflows?
* How are MCP server certificates or fingerprints rotated?
* Can a workflow migrate to a new MCP server?
* Which MCP side effects can be reconciled?
* How are Network Gateway receipts represented?
* Which HTTP methods are assumed idempotent?
* Should the adapter distrust method semantics without provider evidence?
* Which compensation workflows ship first?
* How are compensation dependencies generated?
* Can compensation run in parallel?
* Which compensation failure state is terminal?
* How are manual remediation tasks tracked?
* Should remediation become a child workflow?
* How are waivers represented?
* Who may waive failed compensation?
* How does a Completed with Warnings workflow affect downstream child or parent workflows?
* Which warnings are permitted?
* Can an Outcome Unknown workflow be archived?
* How are terminal reasons normalised?
* How are workflows searched?
* Should workflow events be indexed with FTS?
* How are workflow outputs connected to Project Memory?
* Which workflow outcomes may propose memories?
* How are workflow receipts included in Context Assembly?
* How are tool and AI use receipts linked?
* Which workflow history is visible in Trust Centre by default?
* How is a 100,000-event timeline rendered?
* Which progress events are collapsed?
* How are parallel branches visualised accessibly?
* How are Outcome Unknown choices worded?
* How are compensation limitations explained?
* How are migration mappings shown?
* How does the Desktop reconnect to a running workflow?
* How are Desktop notifications handled?
* Should closing Desktop continue workflows by default?
* Which workflow types ask on close?
* How does Windows shutdown notification interact with worker shutdown?
* How much shutdown time can Opure request?
* How are aborted shutdowns handled?
* Should a Windows service host long workflows in future?
* Is a per-user background process sufficient?
* How are workflow processes started at sign-in?
* How are workflows handled when the user signs out?
* How are laptop sleep and hibernation handled?
* Are timers considered late after sleep?
* How are network transitions handled after resume?
* How are local GPU resources revalidated after sleep?
* Which startup integrity checks run every time?
* How often does a full projection audit run?
* How are 10,000 active workflows discovered efficiently?
* Can recovery proceed incrementally by priority?
* Which workflows resume first?
* How are critical security workflows prioritised?
* How are retry storms suppressed during recovery?
* How is recovery progress shown?
* Which Recovery Decisions can be automatic?
* Which require a developer?
* Can a deterministic reconciliation result automatically resolve Outcome Unknown?
* How is human recovery evidence stored?
* How are mistaken recovery decisions corrected?
* Can a recovery decision be superseded?
* How are restored backups reconciled with external systems?
* How are workflows restored on a different machine?
* Should workflow backups be portable?
* How are machine-local paths mapped?
* How are local model references mapped?
* How are plugin installations mapped?
* How are external provider credentials reacquired?
* How are historical workflows imported without side effects?
* Can an imported workflow be rerun from the start?
* How is rerun identity separated?
* Which workflow retention defaults are appropriate?
* Are 90 days appropriate?
* How long should approvals be retained?
* How long should signals be retained?
* How long should effect receipts be retained?
* Which security events are permanent?
* Which privacy deletion modes are offered?
* How are CAS reference counts maintained?
* How is CAS garbage collection crash safe?
* How are workflow artefacts excluded from support bundles?
* Which safe evidence belongs in a support bundle?
* Should event and checkpoint databases support vacuum?
* How does vacuum affect downtime?
* How is archival storage compressed?
* Should archived workflow history move to JSONL?
* How is archive integrity verified?
* How are performance targets measured under `synchronous=FULL`?
* Is one SQLite writer sufficient at target scale?
* When would sharding be required?
* How are timers and heartbeats batched without weakening durability?
* Can heartbeat writes use lower durability than effect transitions?
* Should heartbeats live in a separate rebuildable database?
* Would that complicate recovery?
* Which event commits may be grouped?
* Can several pure step completions commit together?
* How are latency and durability trade-offs governed?
* How are workflow metrics collected without high-cardinality labels?
* What permanent evidence is required for a duplicate-effect incident?
* What permanent evidence is required for an Outcome Unknown decision?
* What permanent evidence is required before distributed scheduling can be considered?
* What permanent evidence is required before event compaction can be considered?

---

# 354. Deferred Decisions

This ADR intentionally defers:

* multi-machine scheduling;
* clustered Workflow Service;
* distributed leader election;
* cloud workflow authority;
* Windows service hosting;
* cross-device resume;
* organisation-shared workflows;
* visual workflow designer;
* YAML definition format;
* arbitrary user scripts;
* arbitrary C# workflow replay;
* dynamic workflow mutation;
* unbounded loops;
* CRDT workflow state;
* event-history compaction;
* signed event roots;
* trusted timestamps;
* automatic force retry of irreversible effects;
* provider-side workflow state;
* plugin-owned workflows;
* MCP-owned workflows;
* and universal exactly-once external effects.

---

# 355. Alternatives Rejected

In-memory task chains are rejected because they cannot survive a process or machine restart.

Periodic JSON state saves are rejected because they cannot reliably preserve effect intent, event ordering, leases and ambiguity.

A mutable row-only state machine is rejected because history and recovery evidence would be lost.

Event-only execution is rejected because scheduling and UI require efficient projections.

Arbitrary replay-based C# orchestration is rejected because deterministic replay and application versioning would constrain product updates and create nondeterminism risk.

Azure Durable Task is not selected as the primary engine because local offline execution must not depend on Azure infrastructure.

Temporal is deferred because another durable server and deployment boundary are disproportionate to the initial desktop architecture.

Provider-side agent state is rejected because Opure must remain the system of record.

Plugin- and MCP-owned execution are rejected because external components cannot own project authority or recovery.

Shell-script workflows are rejected because arbitrary scripts cannot provide the selected typed state, capability, checkpoint and side-effect contracts.

Universal exactly-once delivery is rejected as an inaccurate claim across unrelated systems.

Automatic retry of unknown irreversible effects is rejected because duplication could be worse than stopping.

Implicit rollback is rejected because compensation is a new effect and may be incomplete.

Silent workflow migration is rejected because completed effects, approvals and policy can change meaning.

---

# 356. Official and Primary Evidence References

## Microsoft Durable Execution Concepts

* [Durable Task documentation](https://learn.microsoft.com/en-us/azure/durable-task/common/)
* [Durable orchestrations](https://learn.microsoft.com/en-us/azure/durable-task/common/durable-task-orchestrations)
* [Durable Task programming model](https://learn.microsoft.com/en-us/azure/azure-functions/durable/programming-model-overview)
* [Orchestration versioning](https://learn.microsoft.com/en-us/azure/durable-task/common/durable-orchestration-versioning)
* [Handling external events](https://learn.microsoft.com/en-us/azure/durable-task/common/durable-task-external-events)
* [What is Durable Task?](https://learn.microsoft.com/en-us/azure/durable-task/common/what-is-durable-task)

## .NET Cancellation

* [Cancellation in managed threads](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads)
* [Cancel non-cancelable asynchronous operations](https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/cancel-non-cancelable-async-operations)
* [Task-based asynchronous programming](https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/task-based-asynchronous-programming)

## SQLite Durability

* [SQLite transactions](https://www.sqlite.org/lang_transaction.html)
* [SQLite Write-Ahead Logging](https://www.sqlite.org/wal.html)
* [SQLite pragma documentation](https://www.sqlite.org/pragma.html)
* [SQLite atomic commit](https://www.sqlite.org/atomiccommit.html)
* [SQLite locking and crash recovery](https://www.sqlite.org/lockingv3.html)

Microsoft Durable Task is used as evidence for durable-execution semantics, not selected as Opure's workflow runtime.

SQLite, .NET, provider idempotency contracts and all integrated activity contracts can change.

The implementation must revalidate every selected runtime, database build, adapter and external guarantee before acceptance.

---

# 357. Review Record

| Date         | Reviewer           | Decision | Notes                                                                                              |
| ------------ | ------------------ | -------- | -------------------------------------------------------------------------------------------------- |
| 18 July 2026 | Architecture draft | Proposed | Declarative event-journal workflow execution with fenced effects and explicit recovery recommended |

---

# 358. Approval

## Founder or Product Approval

* **Name:** Christopher Dyer
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Durable-before-dispatch, Outcome Unknown, approval, compensation and migration review required

## Workflow Architecture Approval

* **Name or role:** Workflow Runtime and Recovery Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Definitions, plans, events, scheduling, attempts, side effects and recovery evidence required

## Persistence Approval

* **Name or role:** Persistence Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** SQLite FULL durability, event journal, projection, checkpoint and backup evidence required

## Runtime and Scheduler Approval

* **Name or role:** Runtime Architecture and Scheduler Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Worker supervision, leases, fencing, resources, cancellation and shutdown evidence required

## Tool and Effect Approval

* **Name or role:** Tool Mediation, Workspace, Repository and Patch Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Intent, idempotency, reconciliation, writes and compensation evidence required

## AI and External Integration Approval

* **Name or role:** Context Assembly, AI Router, Provider Trust, Plugin and MCP Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Exact plans, provider changes, fallback, package and server pinning evidence required

## Security and Privacy Approval

* **Name or role:** Security, Privacy and Secrets Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Secret exclusion, approvals, signals, wrong-project and purge evidence required

## Recovery Approval

* **Name or role:** Recovery Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Crash windows, Outcome Unknown, startup, product update and restore evidence required

## Test Approval

* **Name or role:** Test Architecture Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Fault injection, duplicate execution, race, migration and endurance suites required

---

# 359. Supersession

This ADR is superseded only when a later ADR:

* names ADR-0025 explicitly;
* explains why workflow definitions, execution, events, checkpoints, effects, leases, recovery or migration changed;
* identifies active-instance, plan, event, activity and side-effect migration;
* describes project isolation, approval, cancellation, compensation and data impact;
* provides crash and duplicate-effect comparison evidence;
* and updates the `Superseded by` field.

Historical Workflow Events, effect receipts, approvals, Recovery Decisions and migration records remain available according to retention policy unless explicitly purged.

---

# 360. Change History

| Version | Date         | Author        | Summary                                                                                          |
| ------- | ------------ | ------------- | ------------------------------------------------------------------------------------------------ |
| 0.1     | 18 July 2026 | Founder Draft | Initial declarative durable workflow, fenced side-effect, checkpoint and recovery recommendation |

---

# 361. Final Decision Statement

> **Opure will provisionally execute long-running work through a trusted local Workflow Service that compiles versioned declarative Workflow Definitions into immutable plans, commits every authoritative state transition, ready-step decision, approval, timer, signal, lease and side-effect intent to a service-owned SQLite WAL database using full synchronous durability before dispatching work, and interprets those plans through bounded activity contracts, fenced worker attempts, stable Logical Effect identities, idempotency keys, receipts and reconciliation, periodic derived checkpoints, durable retries and timers, cooperative cancellation, explicit compensation and reviewed migrations, while running instances remain pinned to exact plans and implementations, secrets and provider-side hidden state are excluded, stale workers and approvals are rejected, external-event duplicates are deduplicated, product and Windows restarts rebuild state from the event journal, and any non-idempotent effect whose crash-window outcome cannot be established stops visibly as Outcome Unknown rather than being retried, blended, silently rolled back or guessed, because durable workflow automation is trustworthy only when every transition, side effect, uncertainty and recovery choice remains inspectable and under developer control.**

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**