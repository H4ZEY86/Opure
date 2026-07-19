# BACKLOG-001 — Foundation First 12 Weeks

## Repository-Ready Engineering Backlog

**Status:** Proposed
**Date:** 18 July 2026
**Backlog owner:** Christopher Dyer, Founder and Product Owner
**Programme:** ROADMAP-001 — Foundation Implementation Sequence
**Delivery horizon:** Weeks 1–12
**Milestone target:** M0 through M6 and Founder Gate A
**Repository root:** `C:\Opure`
**Canonical location:** `C:\Opure\specs\BACKLOG-001-foundation-first-12-weeks.md`
**Primary implementation language:** C# on supported LTS .NET
**Target platform:** Windows 11 first
**Working mode:** Small founder-led engineering team
**Backlog type:** Dependency-ordered, evidence-gated and repository ready

---

# 1. Purpose

This backlog converts the first twelve weeks of ROADMAP-001 into implementable epics, stories, acceptance criteria, tests, evidence tasks and founder review points.

It is designed to answer:

* what should be built next;
* why it exists;
* which specification governs it;
* which ADR decisions constrain it;
* what it depends on;
* what counts as complete;
* what evidence is required;
* how failure and recovery must work;
* and which milestone or founder gate it advances.

This backlog contains:

* twelve weekly outcomes;
* ten foundation epics;
* the first sixty numbered engineering tickets from ROADMAP-001;
* Gate A hardening tasks;
* dependency labels;
* milestone assignments;
* specification traceability;
* evidence requirements;
* security requirements;
* recovery requirements;
* accessibility requirements;
* performance requirements;
* and a ready-to-start first-day queue.

---

# 2. Governing Source Set

The backlog is governed by the following source documents.

| Document          | Canonical filename                                  | Role in this backlog                                                  |
| ----------------- | --------------------------------------------------- | --------------------------------------------------------------------- |
| CHARTER-001       | `CHARTER-001.md`                                    | Permanent principles, developer rights and trust test                 |
| SPEC-001          | `SPEC-001.md`                                       | Product direction and normative design principles                     |
| SPEC-002          | `SPEC-002.md`                                       | Runtime Kernel, lifecycle, health and recovery                        |
| SPEC-003          | `SPEC-003.md`                                       | Service ownership, contracts and boundaries                           |
| SPEC-004          | `SPEC-004.md`                                       | Future AI Router constraints; no direct relevance to Gate A execution |
| SPEC-005          | `SPEC-005.md`                                       | Future Knowledge and Memory boundaries                                |
| SPEC-006          | `SPEC-006.md`                                       | Future Workflow and Agent boundaries                                  |
| SPEC-007          | `SPEC-007.md`                                       | Future Plugin SDK boundaries                                          |
| SPEC-008          | `SPEC-008.md`                                       | Trust, security, policy, permissions and evidence                     |
| SPEC-009          | `SPEC-009.md`                                       | Workspace identity, safe reads, snapshots and future patches          |
| SPEC-010          | `SPEC-010.md`                                       | Desktop, Trust Centre, visibility and accessibility                   |
| SPEC-011          | `SPEC-011.md`                                       | Project lifecycle, identity, repository and build foundations         |
| SPEC-012          | `SPEC-012.md`                                       | High-level delivery phases and evidence gates                         |
| ROADMAP-001       | `ROADMAP-001-foundation-implementation-sequence.md` | Detailed implementation order, work packages and twelve-week sequence |
| ADR-0001–ADR-0028 | `C:\Opure\adr\`                                     | Concrete implementation and architecture decisions                    |

---

## 2.1 Filename Normalisation

The uploaded working copies carry `(2)` suffixes because they are duplicate download names.

The repository should use the canonical filenames shown above.

The internal document IDs are already correct.

This backlog uses document IDs rather than uploaded-copy filenames.

---

## 2.2 Specification Hierarchy

The hierarchy is:

```text
CHARTER-001
    ↓
SPEC-001 through SPEC-011
    ↓
SPEC-012
    ↓
ADR-0001 through ADR-0028
    ↓
ROADMAP-001
    ↓
BACKLOG-001
    ↓
Repository issues, branches, commits and evidence
```

Where documents differ:

1. CHARTER-001 controls founding principles.
2. SPEC-001 controls normative product direction.
3. The relevant domain specification controls required behaviour.
4. Accepted ADRs control implementation decisions.
5. Proposed ADRs remain evidence-gated.
6. SPEC-012 controls high-level delivery gates.
7. ROADMAP-001 controls implementation order.
8. BACKLOG-001 controls the initial task sequence.

---

# 3. Foundation Goal

At Founder Gate A, Opure should demonstrate:

```text
Avalonia Desktop
    → Runtime supervision
    → authenticated named-pipe gRPC
    → Project Service
    → Workspace Snapshot
    → Effective Configuration Snapshot
    → Trust Evidence
    → Trust Centre
    → local Recovery Point
```

The first twelve weeks deliberately exclude:

* local AI inference;
* remote AI providers;
* plugins;
* MCP servers;
* arbitrary command execution;
* workflow orchestration;
* patch application;
* cloud backup;
* automatic network activity;
* and external side effects.

---

# 4. Backlog Conventions

## 4.1 Ticket Identifier

Foundation tickets use:

```text
FND-001
```

through:

```text
FND-060
```

Gate A tasks use:

```text
GATE-A-001
```

and later.

---

## 4.2 Epic Identifier

Foundation epics use:

```text
EPIC-FND-01
```

through:

```text
EPIC-FND-10
```

---

## 4.3 Milestone Labels

* `milestone:M0-repository-builds`
* `milestone:M1-runtime-lives`
* `milestone:M2-services-communicate`
* `milestone:M3-state-persists`
* `milestone:M4-project-opens-safely`
* `milestone:M5-configuration-explainable`
* `milestone:M6-foundation-visible`
* `gate:founder-a`

---

## 4.4 Week Labels

* `week:01`
* `week:02`
* `week:03`
* `week:04`
* `week:05`
* `week:06`
* `week:07`
* `week:08`
* `week:09`
* `week:10`
* `week:11`
* `week:12`

---

## 4.5 Capability Labels

* `area:build`
* `area:runtime`
* `area:desktop`
* `area:ipc`
* `area:persistence`
* `area:observability`
* `area:trust`
* `area:filesystem`
* `area:project`
* `area:workspace`
* `area:configuration`
* `area:backup`
* `area:accessibility`
* `area:security`
* `area:performance`
* `area:recovery`
* `area:documentation`

---

## 4.6 Risk Labels

* `risk:security-boundary`
* `risk:data-loss`
* `risk:provisional-technology`
* `risk:performance`
* `risk:accessibility`
* `risk:supply-chain`
* `risk:recovery`
* `risk:windows-specific`

---

## 4.7 Work-Type Labels

* `type:feature`
* `type:architecture`
* `type:infrastructure`
* `type:test`
* `type:security`
* `type:recovery`
* `type:documentation`
* `type:spike`
* `type:founder-review`

---

## 4.8 Statuses

* Not Ready
* Ready
* In Progress
* Review
* Evidence Pending
* Founder Review
* Done
* Blocked
* Rework
* Deferred
* Cancelled

---

# 5. Priority Model

## P0 — Gate Blocking

A missing item prevents the current milestone or Founder Gate A.

---

## P1 — Critical Path

Required by another Gate A story.

---

## P2 — Parallel Foundation

Useful parallel work that does not control the immediate critical path.

---

## P3 — Hardening

May be completed late in the week or during Week 12.

No P3 item may hide a release-gate weakness.

---

# 6. Estimation Model

Use story-point ranges only as planning aids.

| Size | Typical scope                                  |
| ---- | ---------------------------------------------- |
| XS   | hours, one contained change                    |
| S    | one engineering day                            |
| M    | two to three engineering days                  |
| L    | three to five engineering days                 |
| XL   | must be split or treated as a time-boxed spike |

A ticket that becomes XL should be split before implementation unless it is an explicit architecture spike.

---

# 7. Definition of Ready

A ticket is Ready when:

* [ ] User or platform outcome is clear.
* [ ] Owner service or repository area is named.
* [ ] Governing specifications are linked.
* [ ] Applicable ADRs are linked.
* [ ] Dependencies are Done or explicitly mocked behind stable contracts.
* [ ] Inputs and outputs are typed.
* [ ] Persistence ownership is known.
* [ ] Security classification is known.
* [ ] Side effects are identified.
* [ ] Trust Evidence requirement is known.
* [ ] Failure modes are identified.
* [ ] Recovery expectation is identified.
* [ ] Acceptance criteria are testable.
* [ ] Required evidence is named.
* [ ] No unresolved product decision is hidden inside implementation.

---

# 8. Definition of Done

A foundation ticket is Done when:

* [ ] Implementation is complete.
* [ ] Public and internal contracts are versioned.
* [ ] Unit tests pass.
* [ ] Contract or integration tests pass where relevant.
* [ ] Negative tests pass.
* [ ] Security requirements pass.
* [ ] Logs contain no prohibited content.
* [ ] Cancellation and timeout behaviour are tested where relevant.
* [ ] Crash or restart behaviour is tested where relevant.
* [ ] Trust Evidence is implemented where required.
* [ ] Backup or rebuild behaviour is documented.
* [ ] Accessibility is tested for user-facing work.
* [ ] Performance is measured for significant operations.
* [ ] Documentation is updated.
* [ ] Evidence is stored safely under `eng\evidence\`.
* [ ] The clean build remains green.
* [ ] `git status` is clean after the intended commit.
* [ ] No temporary authority bypass remains.

---

# 9. Evidence Gate Mapping

SPEC-012 defines the following evidence progression.

| Gate               | Backlog interpretation                                      |
| ------------------ | ----------------------------------------------------------- |
| G0 — Specification | Required behaviour and non-goals exist in Charter and Specs |
| G1 — Architecture  | Applicable ADRs and contracts are identified                |
| G2 — Prototype     | Ticket produces working behaviour                           |
| G3 — Reliability   | Failure, cancellation, restart and recovery tests pass      |
| G4 — Security      | Identity, permission, path, secret and policy tests pass    |
| G5 — Usability     | Developer can understand and operate the capability         |
| G6 — Performance   | Reference-hardware measurements exist                       |
| G7 — Release       | Documentation, migration and support expectations are ready |

Gate A requires relevant G0–G6 evidence for the first vertical slice.

---

# 10. Epic Catalogue

| Epic        | Name                                            |     Weeks | Milestone     |
| ----------- | ----------------------------------------------- | --------: | ------------- |
| EPIC-FND-01 | Engineering Bootstrap                           |         1 | M0            |
| EPIC-FND-02 | Runtime and Desktop Skeleton                    |         2 | M1            |
| EPIC-FND-03 | Authenticated Local IPC                         |         3 | M2            |
| EPIC-FND-04 | Persistence, Observability and Trust Primitives |   4 and 9 | M3            |
| EPIC-FND-05 | Safe Project Boundary                           |         5 | M4            |
| EPIC-FND-06 | Workspace Snapshot                              |         6 | M4            |
| EPIC-FND-07 | Configuration Definitions and Profiles          |         7 | M5            |
| EPIC-FND-08 | Effective Configuration                         |         8 | M5            |
| EPIC-FND-09 | Trust Centre Foundation                         |  9 and 10 | M6            |
| EPIC-FND-10 | Recovery Point and Gate A Hardening             | 11 and 12 | M6 and Gate A |

---

# 11. Dependency Spine

```text
FND-001 Solution Baseline
    ↓
FND-002 Build Policy
    ↓
FND-003 Version Source
    ↓
FND-004 Runtime
    ↓
FND-005 Desktop
    ↓
FND-006 Bootstrap
    ↓
FND-007 Supervisor
    ↓
FND-008 Health Contract
    ↓
FND-009 Named-Pipe Transport
    ↓
FND-010 Session Authentication
    ↓
FND-014 SQLite
    ↓
FND-015 Migrations
    ↓
FND-016 Outbox
    ↓
FND-021–FND-025 Trust Foundation
    ↓
FND-026–FND-030 Safe Project Open
    ↓
FND-031–FND-038 Workspace Snapshot
    ↓
FND-039–FND-052 Effective Configuration
    ↓
FND-053–FND-057 Trust Centre
    ↓
FND-058–FND-060 Recovery Point
    ↓
Founder Gate A
```

---

# 12. Weekly Outcome Summary

| Week | Outcome                                                     | Core tickets                      |
| ---: | ----------------------------------------------------------- | --------------------------------- |
|    1 | Clean repository restores, builds and tests                 | FND-001–FND-003 plus epic tasks   |
|    2 | Bootstrap starts supervised Runtime and Desktop             | FND-004–FND-007, FND-011–FND-013  |
|    3 | Desktop communicates with Runtime securely                  | FND-008–FND-010                   |
|    4 | Durable SQLite, outbox, logs, traces and redaction          | FND-014–FND-020                   |
|    5 | Project can be opened through a trusted Windows boundary    | FND-026–FND-030                   |
|    6 | Immutable Workspace Snapshot exists                         | FND-031–FND-038                   |
|    7 | Initial typed settings and policies exist                   | FND-039–FND-045                   |
|    8 | Effective Configuration and provenance work                 | FND-046–FND-052                   |
|    9 | Trust Evidence persistence, query and reconciliation work   | FND-021–FND-025, FND-056–FND-057  |
|   10 | Trust Centre Overview, Project and Configuration views work | FND-053–FND-055 plus UI hardening |
|   11 | Local Recovery Point can be created and verified            | FND-058–FND-060                   |
|   12 | End-to-end foundation proof passes Founder Gate A           | GATE-A tasks                      |

---

# 13. Week 1 — Repository and Build

## Outcome

A clean clone restores, builds and tests through one documented command.

## Founder-visible proof

Christopher can clone or reset the repository and run:

```powershell
.\build.ps1 verify
```

with a successful result.

## Primary specifications

* CHARTER-001
* SPEC-001
* SPEC-003
* SPEC-012
* ROADMAP-001

## Primary ADRs

* ADR-0001
* ADR-0008
* ADR-0010
* ADR-0011
* ADR-0012

## Week exit criteria

* [ ] Repository structure exists.
* [ ] `Opure.slnx` exists or the pinned SDK conversion path is proven.
* [ ] .NET SDK is pinned.
* [ ] Central package management exists.
* [ ] Restore is locked.
* [ ] Build script works.
* [ ] Unit test runs.
* [ ] Architecture test runs.
* [ ] CI runs.
* [ ] Version metadata is generated.
* [ ] Repository setup documentation exists.
* [ ] Founder review is recorded.

---

# 14. Week 2 — Bootstrap, Runtime and Desktop Skeleton

## Outcome

Bootstrap starts a supervised Runtime and an Avalonia Desktop shell.

## Founder-visible proof

The Desktop shows Runtime identity and health, and Runtime restarts after a controlled failure.

## Primary specifications

* SPEC-002
* SPEC-003
* SPEC-010
* SPEC-012

## Primary ADRs

* ADR-0002
* ADR-0003
* ADR-0013

## Week exit criteria

* [ ] Bootstrap launches.
* [ ] Runtime launches.
* [ ] Desktop launches.
* [ ] Runtime boot identity exists.
* [ ] Service registry exists.
* [ ] Lifecycle states exist.
* [ ] Job Object supervision works.
* [ ] Runtime restart budget works.
* [ ] Safe Mode exists.
* [ ] Health appears in Desktop.
* [ ] Desktop can close independently.
* [ ] Development data root is isolated.

---

# 15. Week 3 — Authenticated Named-Pipe gRPC

## Outcome

Desktop and Runtime communicate through versioned authenticated local contracts.

## Founder-visible proof

The Desktop receives Runtime health through named-pipe gRPC, while an unrelated process and another Windows user are denied.

## Primary specifications

* SPEC-002
* SPEC-003
* SPEC-008
* SPEC-010

## Primary ADRs

* ADR-0003
* ADR-0004
* ADR-0006
* ADR-0007

## Week exit criteria

* [ ] Health protobuf contract exists.
* [ ] Named-pipe transport works.
* [ ] Pipe ACL is explicit.
* [ ] Session authentication works.
* [ ] Another user is denied.
* [ ] Unrelated same-user process is denied.
* [ ] Contract revision mismatch is actionable.
* [ ] Deadlines work.
* [ ] Cancellation works.
* [ ] Trace context propagates.
* [ ] Malformed messages do not crash Runtime.
* [ ] IPC latency baseline exists.

---

# 16. Week 4 — Persistence, Outbox and Observability

## Outcome

A trusted service owns durable SQLite state, publishes an outbox and emits safe structured diagnostics.

## Founder-visible proof

A test service commits state and evidence, is terminated, restarts and completes outbox delivery without duplicating authority.

## Primary specifications

* SPEC-002
* SPEC-003
* SPEC-008
* SPEC-012

## Primary ADRs

* ADR-0005
* ADR-0006
* ADR-0007
* ADR-0008
* ADR-0027

## Week exit criteria

* [ ] SQLite helper exists.
* [ ] Migration runner exists.
* [ ] WAL and foreign keys are configured.
* [ ] Transactional outbox works.
* [ ] Transactional inbox works.
* [ ] Structured logging exists.
* [ ] Activity tracing exists.
* [ ] Redaction runs before persistence.
* [ ] Secret canaries do not leak.
* [ ] Stable error categories exist.
* [ ] Database health is visible.
* [ ] Crash and replay tests pass.

---

# 17. Week 5 — Project Service and Filesystem Identity

## Outcome

Opure opens and registers one project without trusting a path string.

## Founder-visible proof

A developer selects a repository, Opure validates its root and displays a stable project identity and safe repository summary.

## Primary specifications

* SPEC-003
* SPEC-008
* SPEC-009
* SPEC-010
* SPEC-011
* SPEC-012

## Primary ADRs

* ADR-0005
* ADR-0009
* ADR-0010
* ADR-0027

## Week exit criteria

* [ ] Typed Windows path reference exists.
* [ ] Trusted folder picker exists.
* [ ] Project database exists.
* [ ] Open Project contract exists.
* [ ] Stable project ID exists.
* [ ] Reparse escapes are denied.
* [ ] Traversal is denied.
* [ ] Alternate streams are denied.
* [ ] Repository identity is recorded.
* [ ] Project Open receipt exists.
* [ ] Project list appears in Desktop.
* [ ] Project content remains absent from logs.

---

# 18. Week 6 — Workspace Snapshot

## Outcome

Opure creates an immutable, bounded and hash-verified Workspace Snapshot.

## Founder-visible proof

The Desktop displays a project inventory, repository status and generation; edits create a new generation without mutating the prior snapshot.

## Primary specifications

* SPEC-003
* SPEC-008
* SPEC-009
* SPEC-010
* SPEC-011

## Primary ADRs

* ADR-0005
* ADR-0009
* ADR-0022
* ADR-0027

## Week exit criteria

* [ ] Workspace contract exists.
* [ ] File inventory exists.
* [ ] Logical paths are stable.
* [ ] File hashes are correct.
* [ ] File-size bounds exist.
* [ ] Snapshot generation is immutable.
* [ ] Repository state is represented.
* [ ] Change reconciliation works.
* [ ] Deletion is represented.
* [ ] Watcher loss is recoverable.
* [ ] Snapshot receipt exists.
* [ ] Project summary UI exists.

---

# 19. Week 7 — Configuration Definitions

## Outcome

The first typed Setting and Policy catalogues exist.

## Founder-visible proof

The Desktop can display and edit a small User Base Profile while Product Policy prevents prohibited values.

## Primary specifications

* SPEC-001
* SPEC-002
* SPEC-003
* SPEC-008
* SPEC-010
* SPEC-012

## Primary ADRs

* ADR-0005
* ADR-0007
* ADR-0026

## Week exit criteria

* [ ] Setting Definition schema exists.
* [ ] Policy Definition schema exists.
* [ ] Product Defaults exist.
* [ ] User Base Profile persists.
* [ ] Strict JSON parser exists.
* [ ] Duplicate keys fail.
* [ ] Remote schema references fail.
* [ ] Initial schema registry exists.
* [ ] Product Policy cannot be weakened.
* [ ] Secret values are prohibited.
* [ ] Configuration UI is keyboard accessible.
* [ ] Configuration evidence types are defined.

---

# 20. Week 8 — Effective Configuration

## Outcome

Product, user and project sources produce one immutable Effective Configuration Snapshot with per-key provenance.

## Founder-visible proof

The developer edits `.opure\project.settings.json`; valid changes reconcile, invalid changes remain visible, and the last valid configuration remains active.

## Primary specifications

* SPEC-002
* SPEC-003
* SPEC-008
* SPEC-009
* SPEC-010
* SPEC-011

## Primary ADRs

* ADR-0005
* ADR-0009
* ADR-0026
* ADR-0027

## Week exit criteria

* [ ] Project settings are acquired through Workspace.
* [ ] Setting merge works.
* [ ] Product Policy intersection works.
* [ ] Effective Snapshot is immutable.
* [ ] Per-key provenance exists.
* [ ] Requested and effective values are distinct.
* [ ] Configuration Change Transaction works.
* [ ] Last-known-good state works.
* [ ] Invalid source is visible.
* [ ] Valid external edit reconciles.
* [ ] Stale approval is rejected.
* [ ] Configuration receipt is transactional.

---

# 21. Week 9 — Trust Evidence Service

## Outcome

Owner services publish typed authoritative receipts that Trust Evidence ingests, queries and reconciles.

## Founder-visible proof

Project, Workspace and Configuration operations appear as an ordered causal timeline, including a simulated missed delivery that is detected and repaired.

## Primary specifications

* SPEC-003
* SPEC-008
* SPEC-010
* SPEC-012

## Primary ADRs

* ADR-0005
* ADR-0006
* ADR-0027

## Week exit criteria

* [ ] Evidence Type schema exists.
* [ ] Evidence Record schema exists.
* [ ] Trust database exists.
* [ ] Owner outbox integration works.
* [ ] Trust inbox deduplicates.
* [ ] Authority Classes are represented.
* [ ] Relationships are represented.
* [ ] Completeness is represented.
* [ ] Query API exists.
* [ ] Retention skeleton exists.
* [ ] Owner gap is detected.
* [ ] Reconciliation repairs missing delivery.
* [ ] Projection rebuild works.
* [ ] Model and user assertions cannot impersonate authority.

---

# 22. Week 10 — Trust Centre UI

## Outcome

The developer can inspect Runtime, Project, Workspace and Configuration evidence.

## Founder-visible proof

The Trust Centre explains what happened, which service owned it, which configuration applied and whether the evidence is complete.

## Primary specifications

* SPEC-008
* SPEC-010
* SPEC-012

## Primary ADRs

* ADR-0002
* ADR-0006
* ADR-0027

## Week exit criteria

* [ ] Overview exists.
* [ ] Project view exists.
* [ ] Configuration view exists.
* [ ] Evidence detail exists.
* [ ] Authority labels are visible.
* [ ] Completeness warning is visible.
* [ ] Search and filtering work.
* [ ] Query snapshot time is visible.
* [ ] Owner freshness is visible.
* [ ] Keyboard navigation works.
* [ ] Narrator works.
* [ ] High contrast works.
* [ ] No colour-only meaning exists.
* [ ] Query performance target is measured.

---

# 23. Week 11 — Recovery Point

## Outcome

Foundation state can be snapshotted consistently and restored into a disposable staged root.

## Founder-visible proof

Opure creates a local Recovery Point while services are running, verifies it and restores it without touching the active data root.

## Primary specifications

* SPEC-002
* SPEC-003
* SPEC-008
* SPEC-010
* SPEC-012

## Primary ADRs

* ADR-0005
* ADR-0009
* ADR-0015
* ADR-0027
* ADR-0028

## Week exit criteria

* [ ] Backup Adapter contract exists.
* [ ] Backup Epoch skeleton exists.
* [ ] SQLite Online Backup works.
* [ ] Live WAL database is copied safely.
* [ ] Raw file copy comparison fails as expected.
* [ ] Recovery Point manifest exists.
* [ ] Structural verification works.
* [ ] Disposable staged restore works.
* [ ] Active root remains unchanged.
* [ ] Recovery Point is labelled Same-Device.
* [ ] Backup Health view exists.
* [ ] Crash and cancellation tests pass.

---

# 24. Week 12 — Founder Gate A

## Outcome

The complete foundation slice is demonstrated, measured and reviewed.

## Founder-visible proof

The full ROADMAP-001 demonstration script passes from a clean state, including project open, configuration reconciliation, Trust Centre evidence, Desktop reconnect, Runtime recovery and Recovery Point validation.

## Week exit criteria

* [ ] End-to-end demonstration passes.
* [ ] Runtime restart loses no committed state.
* [ ] Desktop reconnect works.
* [ ] IPC adversarial suite passes.
* [ ] Filesystem adversarial suite passes.
* [ ] Configuration adversarial suite passes.
* [ ] Evidence reconciliation suite passes.
* [ ] Secret-canary suite passes.
* [ ] Accessibility baseline passes.
* [ ] Performance baseline exists.
* [ ] Recovery baseline passes.
* [ ] ADR evidence matrix is updated.
* [ ] Avalonia retain-or-replace decision is recorded.
* [ ] Process topology retain-or-amend decision is recorded.
* [ ] Founder Gate A decision is recorded.
* [ ] Phase 7 backlog is Ready.

---

# 25. Epic Details

## EPIC-FND-01 — Engineering Bootstrap

**Outcome:** A clean repository can be restored, built, tested and versioned predictably.

**Tickets:** FND-001–FND-003 plus Week 1 epic tasks.

**Primary specifications:** SPEC-001, SPEC-003, SPEC-012.

**Primary ADRs:** ADR-0001, ADR-0008, ADR-0010, ADR-0011, ADR-0012.

**Gate:** M0.

---

## EPIC-FND-02 — Runtime and Desktop Skeleton

**Outcome:** Bootstrap starts and supervises a local Runtime and Desktop shell.

**Tickets:** FND-004–FND-007, FND-011–FND-013.

**Primary specifications:** SPEC-002, SPEC-003, SPEC-010.

**Primary ADRs:** ADR-0002, ADR-0003.

**Gate:** M1.

---

## EPIC-FND-03 — Authenticated Local IPC

**Outcome:** Desktop and Runtime communicate through bounded, authenticated and observable named-pipe gRPC.

**Tickets:** FND-008–FND-010.

**Primary specifications:** SPEC-002, SPEC-003, SPEC-008.

**Primary ADRs:** ADR-0004, ADR-0006, ADR-0007.

**Gate:** M2.

---

## EPIC-FND-04 — Persistence, Observability and Trust Primitives

**Outcome:** Services own durable SQLite state, publish receipts and emit safe diagnostics.

**Tickets:** FND-014–FND-025, FND-056–FND-057.

**Primary specifications:** SPEC-002, SPEC-003, SPEC-008.

**Primary ADRs:** ADR-0005, ADR-0006, ADR-0007, ADR-0027.

**Gate:** M3 and M6.

---

## EPIC-FND-05 — Safe Project Boundary

**Outcome:** A project is opened through a trusted Windows filesystem boundary and receives stable identity.

**Tickets:** FND-026–FND-030.

**Primary specifications:** SPEC-003, SPEC-008, SPEC-009, SPEC-011.

**Primary ADRs:** ADR-0009, ADR-0027.

**Gate:** M4.

---

## EPIC-FND-06 — Workspace Snapshot

**Outcome:** Project metadata and selected files are represented by an immutable snapshot.

**Tickets:** FND-031–FND-038.

**Primary specifications:** SPEC-009, SPEC-011.

**Primary ADRs:** ADR-0009, ADR-0022, ADR-0027.

**Gate:** M4.

---

## EPIC-FND-07 — Configuration Definitions and Profiles

**Outcome:** Typed settings, policies, defaults and User Base Profile exist.

**Tickets:** FND-039–FND-045.

**Primary specifications:** SPEC-001, SPEC-002, SPEC-003, SPEC-008, SPEC-010.

**Primary ADRs:** ADR-0026.

**Gate:** M5.

---

## EPIC-FND-08 — Effective Configuration

**Outcome:** One immutable Effective Snapshot is produced with policy and provenance.

**Tickets:** FND-046–FND-052.

**Primary specifications:** SPEC-002, SPEC-003, SPEC-008, SPEC-009, SPEC-010.

**Primary ADRs:** ADR-0009, ADR-0026, ADR-0027.

**Gate:** M5.

---

## EPIC-FND-09 — Trust Centre Foundation

**Outcome:** The developer can inspect authoritative Runtime, Project, Workspace and Configuration evidence.

**Tickets:** FND-021–FND-025, FND-053–FND-057.

**Primary specifications:** SPEC-008, SPEC-010.

**Primary ADRs:** ADR-0006, ADR-0027.

**Gate:** M6.

---

## EPIC-FND-10 — Recovery Point and Gate A Hardening

**Outcome:** Foundation state is recoverable and the complete slice passes Founder Gate A.

**Tickets:** FND-058–FND-060 and GATE-A tasks.

**Primary specifications:** SPEC-002, SPEC-008, SPEC-010, SPEC-012.

**Primary ADRs:** ADR-0005, ADR-0009, ADR-0015, ADR-0027, ADR-0028.

**Gate:** M6 and Founder Gate A.

---

# 26. Ticket Format

Every ticket below contains:

* priority;
* size;
* week;
* milestone;
* owner;
* governing specifications;
* governing ADRs;
* dependencies;
* outcome;
* implementation scope;
* acceptance criteria;
* tests;
* Trust Evidence;
* recovery;
* and evidence output.

# 27. Engineering Tickets FND-001 through FND-030

# FND-001 — Create Solution Baseline

**Epic:** EPIC-FND-01
**Week:** 01
**Milestone:** M0
**Priority:** P0
**Size:** M
**Owner:** Engineering System
**Specifications:** SPEC-001, SPEC-003, SPEC-012
**ADRs:** ADR-0001, ADR-0010, ADR-0011
**Depends on:** None

## Outcome

The repository has a canonical .NET solution and initial project structure that builds from `C:\Opure`.

## Implementation Scope

* Create `Opure.slnx` using the pinned SDK or prove the supported conversion path.
* Create initial `src`, `tests`, `tools`, `build`, `eng`, `packaging`, `schemas` and documentation directories.
* Add Runtime Contracts, Runtime executable, Bootstrap executable and Architecture Tests projects.
* Add projects to the solution with deterministic names and paths.
* Document the canonical repository root and project naming rules.

## Acceptance Criteria

* [ ] A clean checkout opens through the selected solution format.
* [ ] All initial projects restore and compile.
* [ ] Project paths match ADR-0010 boundaries.
* [ ] No generated build artefact is committed.
* [ ] The repository contains no duplicate or temporary solution file.
* [ ] The command used to create or convert `.slnx` is documented.
* [ ] The solution contains no test-to-production or UI-to-persistence dependency violation.

## Required Tests

* Clean-clone restore test.
* Solution project enumeration test.
* Architecture reference smoke test.
* Windows path and casing test.

## Trust Evidence

No runtime Trust Evidence required. CI and commit history provide engineering evidence.

## Recovery

The ticket is reversible by deleting the generated projects before production state exists.

## Evidence Output

* `eng/evidence/milestones/M0/solution-baseline.md`
* clean build log
* solution project inventory

## Suggested Labels

`area:build`, `type:infrastructure`, `week:01`, `milestone:M0`, `priority:p0`

---

# FND-002 — Add Central Build Policy

**Epic:** EPIC-FND-01
**Week:** 01
**Milestone:** M0
**Priority:** P0
**Size:** M
**Owner:** Engineering System
**Specifications:** SPEC-001, SPEC-003, SPEC-012
**ADRs:** ADR-0001, ADR-0008, ADR-0010, ADR-0011
**Depends on:** FND-001

## Outcome

All first-party projects inherit one explicit compile, analysis, package and deterministic-build policy.

## Implementation Scope

* Create `Directory.Build.props` and `Directory.Build.targets`.
* Enable nullable reference types.
* Define warning and analyser policy.
* Enable deterministic and continuous-integration build properties.
* Create `Directory.Packages.props` with exact versions.
* Enable lock files and locked restore.
* Pin repository-local tools.
* Define separate Development, Preview and Stable build constants without weakening security.

## Acceptance Criteria

* [ ] Warnings selected by policy fail the build.
* [ ] Nullable is enabled for first-party projects.
* [ ] Package versions are not declared inside individual project files except approved exceptions.
* [ ] Floating package versions are rejected.
* [ ] Locked restore fails when the lock file is stale.
* [ ] Build outputs are deterministic within documented platform limits.
* [ ] Production assemblies do not inherit test-only packages.
* [ ] The policy can be read from the repository root without hidden machine configuration.

## Required Tests

* Package version duplication test.
* Locked-restore negative test.
* Warning-as-error test.
* Deterministic assembly comparison.
* Architecture test for prohibited package references.

## Trust Evidence

No runtime Trust Evidence required.

## Recovery

Policy changes are source controlled and revertible. A broken policy must not require machine-wide SDK changes.

## Evidence Output

* central build policy file
* dependency graph
* deterministic build comparison
* locked restore report

## Suggested Labels

`area:build`, `type:infrastructure`, `week:01`, `milestone:M0`, `priority:p0`

---

# FND-003 — Add Version Source

**Epic:** EPIC-FND-01
**Week:** 01
**Milestone:** M0
**Priority:** P0
**Size:** S
**Owner:** Release Engineering
**Specifications:** SPEC-001, SPEC-012
**ADRs:** ADR-0012
**Depends on:** FND-001, FND-002

## Outcome

One repository-owned version source generates consistent assembly, package and diagnostic version metadata.

## Implementation Scope

* Create root `version.json`.
* Integrate the selected pinned Nerdbank.GitVersioning release.
* Generate assembly, file, informational and package versions.
* Expose safe build identity to Runtime and Desktop.
* Add CI output showing resolved version.
* Document dirty-tree and uncommitted-build behaviour.

## Acceptance Criteria

* [ ] All production assemblies report the same release-train version.
* [ ] Informational version contains commit identity according to policy.
* [ ] A clean tagged build resolves predictably.
* [ ] An untagged development build is distinguishable.
* [ ] MSIX and update metadata can consume the same source later.
* [ ] No project maintains an independent version literal.
* [ ] Version generation works offline after locked restore.

## Required Tests

* Version resolution unit test.
* Clean versus dirty repository test.
* Tagged versus untagged test.
* Assembly metadata inspection.

## Trust Evidence

Runtime later records product version in startup evidence.

## Recovery

A versioning failure blocks packaging but does not mutate product state.

## Evidence Output

* version-resolution report
* sample Development version
* sample release-tag version

## Suggested Labels

`area:build`, `type:infrastructure`, `week:01`, `milestone:M0`, `priority:p0`

---

# FND-004 — Add Runtime Executable

**Epic:** EPIC-FND-02
**Week:** 02
**Milestone:** M1
**Priority:** P0
**Size:** M
**Owner:** Runtime
**Specifications:** SPEC-002, SPEC-003
**ADRs:** ADR-0001, ADR-0003
**Depends on:** FND-001, FND-002, FND-003

## Outcome

A minimal Runtime process starts, owns a boot identity, exposes lifecycle state and shuts down deterministically.

## Implementation Scope

* Create Runtime host composition root.
* Create Runtime data-root resolution.
* Generate a random opaque Runtime boot identity on every start.
* Implement start, ready, stopping and stopped lifecycle.
* Add controlled shutdown token.
* Add safe console and structured startup diagnostics.
* Do not load project, AI, plugin, MCP or workflow code.

## Acceptance Criteria

* [ ] Runtime starts without network access.
* [ ] Runtime boot identity changes on every process start.
* [ ] Runtime reports its product and contract version.
* [ ] Shutdown completes within the defined timeout.
* [ ] Unhandled startup failure returns a stable exit category.
* [ ] Runtime does not write outside the Development channel data root.
* [ ] Runtime does not require an AI model or provider.
* [ ] Runtime does not create domain databases that it does not own.

## Required Tests

* Start and stop integration test.
* Boot identity uniqueness test.
* Offline launch test.
* Unexpected exception exit test.
* Data-root boundary test.

## Trust Evidence

Later emits `runtime.started` and `runtime.stopped`; for this ticket safe structured logs are sufficient.

## Recovery

A failed Runtime start leaves no partially committed service state.

## Evidence Output

* Runtime startup trace
* process inventory
* offline network capture

## Suggested Labels

`area:runtime`, `type:feature`, `week:02`, `milestone:M1`, `priority:p0`

---

# FND-005 — Add Desktop Executable

**Epic:** EPIC-FND-02
**Week:** 02
**Milestone:** M1
**Priority:** P0
**Size:** M
**Owner:** Desktop
**Specifications:** SPEC-001, SPEC-010
**ADRs:** ADR-0002, ADR-0003
**Depends on:** FND-001, FND-002, FND-003

## Outcome

An Avalonia Desktop shell launches and presents honest disconnected Runtime state.

## Implementation Scope

* Create the Avalonia Desktop project using a pinned template and packages.
* Implement application shell, title, primary navigation placeholder and status area.
* Display Runtime Unavailable before IPC exists.
* Add keyboard focus order and basic Narrator names.
* Keep all authoritative state outside the UI.
* Create an adapter boundary to preserve the WinUI 3 fallback.

## Acceptance Criteria

* [ ] Desktop starts on supported Windows 11.
* [ ] The main window appears without waiting indefinitely for Runtime.
* [ ] Disconnected state is explicit and not shown as success.
* [ ] Closing Desktop exits cleanly.
* [ ] No service database is opened by Desktop.
* [ ] No project file is read directly by Desktop.
* [ ] Primary controls are keyboard reachable.
* [ ] The UI framework is isolated behind Desktop contracts and view models.

## Required Tests

* Avalonia headless launch test.
* Real-window smoke test.
* Keyboard focus smoke test.
* Disconnected-state test.
* Architecture test preventing persistence references.

## Trust Evidence

No authoritative evidence owned by Desktop. UI telemetry remains operational only.

## Recovery

Desktop can be closed and reopened without losing Runtime-owned state.

## Evidence Output

* Desktop launch evidence
* accessibility smoke report
* framework adapter diagram

## Suggested Labels

`area:desktop`, `type:feature`, `week:02`, `milestone:M1`, `priority:p0`

---

# FND-006 — Add Bootstrap Executable

**Epic:** EPIC-FND-02
**Week:** 02
**Milestone:** M1
**Priority:** P0
**Size:** M
**Owner:** Bootstrap
**Specifications:** SPEC-002, SPEC-003, SPEC-010
**ADRs:** ADR-0003, ADR-0013
**Depends on:** FND-004, FND-005

## Outcome

One controlled bootstrap process starts Runtime and Desktop with exact identities and channel isolation.

## Implementation Scope

* Create Bootstrap Windows executable.
* Resolve exact packaged or development executable paths.
* Create channel-specific data-root environment.
* Start Runtime before Desktop.
* Pass only bounded bootstrap session material.
* Record child process IDs and start times safely.
* Implement deterministic shutdown ordering.

## Acceptance Criteria

* [ ] Bootstrap starts the expected Runtime binary.
* [ ] Bootstrap starts the expected Desktop binary.
* [ ] Executable paths are absolute and verified.
* [ ] No current-directory executable search occurs.
* [ ] Stable, Preview and Development roots cannot collide.
* [ ] Session material is random and not persisted.
* [ ] Shutdown does not orphan child processes.
* [ ] Bootstrap failure is actionable.

## Required Tests

* Exact executable path test.
* Wrong binary substitution test.
* Channel isolation test.
* Child start failure test.
* Bootstrap shutdown test.

## Trust Evidence

Bootstrap later publishes verified process-start receipts through Runtime.

## Recovery

A partial start terminates already-started children and leaves the prior durable state untouched.

## Evidence Output

* bootstrap process tree
* channel data-root report
* binary identity report

## Suggested Labels

`area:runtime`, `type:feature`, `week:02`, `milestone:M1`, `priority:p0`

---

# FND-007 — Add Process Supervisor

**Epic:** EPIC-FND-02
**Week:** 02
**Milestone:** M1
**Priority:** P0
**Size:** L
**Owner:** Runtime and Bootstrap
**Specifications:** SPEC-002, SPEC-003, SPEC-008
**ADRs:** ADR-0003, ADR-0006
**Depends on:** FND-004, FND-006

## Outcome

Bootstrap and Runtime supervise owned processes, apply bounded restart policy and enter Safe Mode after repeated failure.

## Implementation Scope

* Implement Windows Job Object ownership.
* Track process ID, start time, executable identity and boot identity.
* Implement restart budget and exponential backoff.
* Distinguish clean exit, crash, policy stop and quarantine.
* Implement Safe Mode transition.
* Expose process health projection to Desktop.
* Prevent child processes from breaking away where selected.

## Acceptance Criteria

* [ ] Unexpected Runtime exit is detected.
* [ ] Runtime restarts within the configured budget.
* [ ] Repeated failure stops restart loops.
* [ ] Safe Mode is entered visibly.
* [ ] Job Object closes orphaned owned workers.
* [ ] PID reuse does not confuse process identity.
* [ ] Clean shutdown is not reported as crash.
* [ ] Supervisor diagnostics exclude command-line secrets and environment values.

## Required Tests

* Runtime kill test.
* Rapid crash-loop test.
* PID reuse simulation.
* Bootstrap crash and orphan cleanup test.
* Clean shutdown classification test.
* Job Object breakaway test.

## Trust Evidence

Publishes service or process state-change receipts once Trust Evidence is available.

## Recovery

Safe Mode starts only the minimum trusted foundation and retains prior durable state.

## Evidence Output

* supervisor state-machine report
* crash-loop trace
* orphan cleanup report

## Suggested Labels

`area:runtime`, `type:feature`, `week:02`, `milestone:M1`, `priority:p0`

---

# FND-008 — Define Runtime Health Contract

**Epic:** EPIC-FND-03
**Week:** 03
**Milestone:** M2
**Priority:** P0
**Size:** S
**Owner:** Runtime Contracts
**Specifications:** SPEC-002, SPEC-003, SPEC-010
**ADRs:** ADR-0004
**Depends on:** FND-004

## Outcome

A versioned protobuf contract exposes Runtime identity, readiness and safe service health.

## Implementation Scope

* Define request and response messages.
* Include product version, Runtime boot ID, mode, readiness and service summaries.
* Define compatibility revision.
* Define deadlines and maximum message size.
* Define stable health and error enums.
* Exclude paths, secrets and raw exception text.

## Acceptance Criteria

* [ ] Contract compiles for client and server.
* [ ] Unknown fields remain compatible according to policy.
* [ ] Boot identity is required.
* [ ] Health state is typed.
* [ ] Service summaries are bounded.
* [ ] Error categories are stable.
* [ ] No implementation class crosses the contract boundary.
* [ ] Golden serialization fixtures exist.

## Required Tests

* Protobuf round-trip test.
* Unknown-field compatibility test.
* Missing-required-semantic validation test.
* Maximum-size test.
* Golden fixture test.

## Trust Evidence

Health responses are projections, not authoritative state changes.

## Recovery

An unsupported contract revision returns an actionable compatibility error without crashing either process.

## Evidence Output

* contract schema
* compatibility matrix
* golden messages

## Suggested Labels

`area:ipc`, `type:architecture`, `week:03`, `milestone:M2`, `priority:p0`

---

# FND-009 — Implement Named-Pipe Transport Prototype

**Epic:** EPIC-FND-03
**Week:** 03
**Milestone:** M2
**Priority:** P0
**Size:** L
**Owner:** Runtime IPC
**Specifications:** SPEC-002, SPEC-003, SPEC-008
**ADRs:** ADR-0004, ADR-0006
**Depends on:** FND-006, FND-008

## Outcome

Desktop calls Runtime Health through gRPC over an exact local named pipe.

## Implementation Scope

* Implement server and client transport adapters.
* Create unpredictable channel-specific pipe naming.
* Bind the pipe to the current Runtime boot session.
* Support unary health call first.
* Add deadline, cancellation and bounded streaming hooks.
* Add connection diagnostics without payload logging.

## Acceptance Criteria

* [ ] Desktop receives Runtime Health through the pipe.
* [ ] The transport does not listen on TCP.
* [ ] Pipe name is not a global stable secret.
* [ ] Connection failure is actionable.
* [ ] Deadline expiry returns a stable error.
* [ ] Cancellation closes the call promptly.
* [ ] Runtime restart causes clean reconnect behaviour.
* [ ] Transport implementation remains behind an adapter.

## Required Tests

* Unary round-trip integration test.
* Runtime restart reconnect test.
* Deadline test.
* Cancellation test.
* Large-message rejection test.
* No-TCP-listener network test.

## Trust Evidence

Connection establishment later creates operational and security evidence, not one record per harmless health poll.

## Recovery

A broken pipe does not corrupt state; Desktop returns to disconnected mode and retries under bounded policy.

## Evidence Output

* transport prototype report
* latency baseline
* network-listener inspection

## Suggested Labels

`area:ipc`, `type:feature`, `week:03`, `milestone:M2`, `priority:p0`

---

# FND-010 — Add Named-Pipe Session Authentication

**Epic:** EPIC-FND-03
**Week:** 03
**Milestone:** M2
**Priority:** P0
**Size:** L
**Owner:** Runtime Security
**Specifications:** SPEC-002, SPEC-003, SPEC-008
**ADRs:** ADR-0004, ADR-0007
**Depends on:** FND-006, FND-009

## Outcome

Only the intended Desktop session can use the Runtime pipe, even among processes running as the same Windows user.

## Implementation Scope

* Apply explicit named-pipe ACL.
* Implement bootstrap-issued ephemeral session material.
* Implement challenge-response or equivalent mutual session proof.
* Bind authentication to Runtime boot identity and client process identity.
* Expire session material.
* Prevent replay.
* Redact all authentication material.

## Acceptance Criteria

* [ ] Expected Desktop authenticates.
* [ ] Another Windows user is denied by ACL.
* [ ] Unrelated same-user process without session material is denied.
* [ ] Captured session proof cannot be replayed.
* [ ] Runtime restart invalidates prior session.
* [ ] Desktop restart receives fresh session material.
* [ ] Authentication failure produces stable security evidence without secret values.
* [ ] Session keys never appear in files, command lines, logs or Trust payloads.

## Required Tests

* Another-user test.
* Same-user unauthorised-process test.
* Replay test.
* Runtime restart invalidation test.
* Desktop restart test.
* Secret-canary scan.

## Trust Evidence

Publishes `ipc.session-established` at a bounded level and `ipc.session-denied` for material denials.

## Recovery

Authentication failure leaves Runtime healthy and allows a valid fresh Bootstrap session.

## Evidence Output

* IPC authentication threat model
* ACL inspection
* replay denial report
* secret leakage report

## Suggested Labels

`area:ipc`, `area:security`, `type:security`, `week:03`, `milestone:M2`, `priority:p0`

---

# FND-011 — Add Service Registry Contract

**Epic:** EPIC-FND-02
**Week:** 02
**Milestone:** M1
**Priority:** P1
**Size:** M
**Owner:** Runtime
**Specifications:** SPEC-002, SPEC-003
**ADRs:** ADR-0003, ADR-0004
**Depends on:** FND-004, FND-008

## Outcome

Runtime exposes a typed registry of logical services without exposing implementation internals.

## Implementation Scope

* Define service ID and revision.
* Define owner, lifecycle state, process placement and dependencies.
* Define capability summary and health reference.
* Implement in-memory registry first.
* Expose bounded query contract.
* Prevent duplicate service ownership.

## Acceptance Criteria

* [ ] Every registered service has a stable ID.
* [ ] Duplicate service ID is rejected.
* [ ] Service owner is explicit.
* [ ] Process placement is explicit.
* [ ] Dependencies are typed.
* [ ] Registry does not expose internal class names or database paths.
* [ ] Query output is deterministic.
* [ ] Registry works before domain services exist.

## Required Tests

* Register and query test.
* Duplicate ID test.
* Unknown dependency test.
* Deterministic ordering test.
* Contract serialization test.

## Trust Evidence

Registry changes later emit service lifecycle receipts.

## Recovery

Registry rebuilds deterministically from trusted service definitions after Runtime restart.

## Evidence Output

* service registry schema
* initial service catalogue

## Suggested Labels

`area:runtime`, `type:feature`, `week:02`, `milestone:M1`, `priority:p1`

---

# FND-012 — Add Service Lifecycle State Machine

**Epic:** EPIC-FND-02
**Week:** 02
**Milestone:** M1
**Priority:** P1
**Size:** M
**Owner:** Runtime
**Specifications:** SPEC-002, SPEC-003
**ADRs:** ADR-0003
**Depends on:** FND-011

## Outcome

Every logical service follows one validated lifecycle with dependency-aware startup and shutdown.

## Implementation Scope

* Implement Registered, Starting, Ready, Degraded, Stopping, Stopped, Failed, Restarting, Quarantined and Disabled states.
* Define allowed transitions.
* Implement dependency ordering.
* Implement startup and shutdown timeouts.
* Capture stable failure categories.
* Expose lifecycle projection.

## Acceptance Criteria

* [ ] Invalid state transition is rejected.
* [ ] Dependencies start before dependants.
* [ ] Dependants do not become Ready when required dependency failed.
* [ ] Shutdown occurs in reverse dependency order.
* [ ] Timeout moves service to explicit failure state.
* [ ] Degraded is distinct from Ready and Failed.
* [ ] Lifecycle events are deterministic.
* [ ] State machine has exhaustive tests.

## Required Tests

* Transition table test.
* Dependency ordering test.
* Startup timeout test.
* Shutdown timeout test.
* Failure propagation test.
* Restart transition test.

## Trust Evidence

Publishes `service.state-changed` once Trust Evidence is available.

## Recovery

Runtime can reconstruct service lifecycle from current process state and service definitions after restart.

## Evidence Output

* state-machine diagram
* transition test report

## Suggested Labels

`area:runtime`, `type:feature`, `week:02`, `milestone:M1`, `priority:p1`

---

# FND-013 — Add Runtime Health UI

**Epic:** EPIC-FND-02
**Week:** 02
**Milestone:** M1
**Priority:** P1
**Size:** M
**Owner:** Desktop
**Specifications:** SPEC-002, SPEC-010
**ADRs:** ADR-0002, ADR-0004
**Depends on:** FND-005, FND-008, FND-009, FND-011, FND-012

## Outcome

Desktop displays Runtime boot identity, mode and service health without pretending projections are authority.

## Implementation Scope

* Create Runtime status view model.
* Show Connected, Disconnected, Starting, Ready, Degraded and Safe Mode.
* Show service list and stable error codes.
* Add refresh and reconnect behaviour.
* Add keyboard and screen-reader semantics.
* Keep detailed diagnostics behind progressive disclosure.

## Acceptance Criteria

* [ ] Disconnected state is immediately understandable.
* [ ] Runtime boot identity can be copied safely.
* [ ] Service state changes update without freezing UI.
* [ ] Degraded state is not shown as healthy.
* [ ] Safe Mode is prominent.
* [ ] No raw exception or secret appears.
* [ ] Keyboard and Narrator access all rows.
* [ ] Closing and reopening Desktop reconnects.

## Required Tests

* Headless view-model tests.
* Real-window reconnect test.
* Keyboard test.
* Narrator label inspection.
* High-contrast test.
* Large service-list performance test.

## Trust Evidence

UI does not create authoritative evidence; user refresh may create bounded operational telemetry only.

## Recovery

After connection loss the UI retains the last snapshot labelled stale and attempts bounded reconnect.

## Evidence Output

* Runtime Health UI screenshots or test artefacts
* accessibility report
* reconnect recording

## Suggested Labels

`area:desktop`, `type:feature`, `week:02`, `milestone:M1`, `priority:p1`

---

# FND-014 — Add SQLite Persistence Library

**Epic:** EPIC-FND-04
**Week:** 04
**Milestone:** M3
**Priority:** P0
**Size:** L
**Owner:** Persistence
**Specifications:** SPEC-002, SPEC-003, SPEC-008
**ADRs:** ADR-0005
**Depends on:** FND-002, FND-010

## Outcome

Trusted services can own independent SQLite databases through one reviewed persistence boundary.

## Implementation Scope

* Create `Opure.Persistence.Sqlite`.
* Pin `Microsoft.Data.Sqlite` and native SQLite dependencies.
* Create connection factory and service database descriptor.
* Enable safe connection strings.
* Implement transaction helper and one-writer policy.
* Implement database open and close health.
* Prevent cross-service database access.

## Acceptance Criteria

* [ ] A sample service creates and opens its own database.
* [ ] Database location is service owned and channel isolated.
* [ ] Foreign database path is rejected by ownership policy.
* [ ] Transactions commit and roll back correctly.
* [ ] Connection strings contain no user-controlled options.
* [ ] One writer is enforced by service design.
* [ ] Library does not expose arbitrary SQL to Desktop.
* [ ] Native dependency versions are recorded.

## Required Tests

* Create/open test.
* Transaction commit and rollback.
* Ownership path test.
* Concurrent writer test.
* Malformed database test.
* Native dependency inventory test.

## Trust Evidence

Database health later produces operational evidence; domain writes remain owner evidence.

## Recovery

Database open failure leaves the service Failed or Recovery Required without creating a replacement silently.

## Evidence Output

* persistence library design
* SQLite dependency manifest
* transaction report

## Suggested Labels

`area:persistence`, `type:infrastructure`, `week:04`, `milestone:M3`, `priority:p0`

---

# FND-015 — Add Migration Runner

**Epic:** EPIC-FND-04
**Week:** 04
**Milestone:** M3
**Priority:** P0
**Size:** M
**Owner:** Persistence
**Specifications:** SPEC-002, SPEC-003, SPEC-008
**ADRs:** ADR-0005, ADR-0028
**Depends on:** FND-014

## Outcome

Every service database applies explicit ordered migrations with validation and recovery metadata.

## Implementation Scope

* Define migration ID and checksum.
* Create migration history table.
* Implement forward-only ordered execution initially.
* Run each migration transactionally where SQLite permits.
* Validate schema after migration.
* Expose migration state to service health.
* Reserve pre-migration Recovery Point hook.

## Acceptance Criteria

* [ ] Fresh database reaches current schema.
* [ ] Existing database applies missing migrations once.
* [ ] Changed migration checksum is rejected.
* [ ] Failed transactional migration rolls back.
* [ ] Unsupported newer schema fails safely.
* [ ] Migration state is visible.
* [ ] No automatic destructive reset occurs.
* [ ] Migration runner can be invoked against a staged restore copy.

## Required Tests

* Fresh schema test.
* Incremental migration test.
* Checksum mutation test.
* Failure rollback test.
* Newer-schema test.
* Interrupted migration recovery test.

## Trust Evidence

Later publishes migration intent and completion receipts.

## Recovery

A failed migration preserves the prior database or pre-migration Recovery Point and prevents normal service readiness.

## Evidence Output

* migration catalogue
* failure and rollback report
* schema validation report

## Suggested Labels

`area:persistence`, `type:infrastructure`, `week:04`, `milestone:M3`, `priority:p0`

---

# FND-016 — Add Transactional Outbox

**Epic:** EPIC-FND-04
**Week:** 04
**Milestone:** M3
**Priority:** P0
**Size:** L
**Owner:** Persistence and Trust
**Specifications:** SPEC-003, SPEC-008
**ADRs:** ADR-0005, ADR-0027
**Depends on:** FND-014, FND-015

## Outcome

A domain service can commit authoritative state and the required publication record in one SQLite transaction.

## Implementation Scope

* Define outbox table and message envelope.
* Write outbox row inside the owner transaction.
* Implement ordered delivery lease.
* Implement retry and backoff.
* Mark delivery without deleting audit-critical identity prematurely.
* Bind owner sequence and payload hash.
* Expose backlog health.

## Acceptance Criteria

* [ ] Domain state and outbox row commit atomically.
* [ ] Rollback removes both.
* [ ] Delivery is at least once.
* [ ] Owner sequence is monotonic within a stream.
* [ ] Crash after commit before delivery is recovered.
* [ ] Crash during delivery may duplicate but not lose.
* [ ] Outbox payload is immutable.
* [ ] Outbox backlog is visible.

## Required Tests

* Commit test.
* Rollback test.
* Crash-before-delivery test.
* Crash-during-delivery test.
* Ordering test.
* Duplicate-delivery test.
* Backlog health test.

## Trust Evidence

The outbox is the publication source for authoritative owner receipts.

## Recovery

Pending outbox rows resume after service restart through idempotent delivery.

## Evidence Output

* transactional outbox report
* crash matrix
* owner sequence test

## Suggested Labels

`area:persistence`, `area:trust`, `type:infrastructure`, `week:04`, `milestone:M3`, `priority:p0`

---

# FND-017 — Add Transactional Inbox

**Epic:** EPIC-FND-04
**Week:** 04
**Milestone:** M3
**Priority:** P1
**Size:** M
**Owner:** Persistence
**Specifications:** SPEC-003, SPEC-008
**ADRs:** ADR-0005, ADR-0027
**Depends on:** FND-014, FND-015

## Outcome

A receiving service processes at-least-once messages idempotently and detects conflicting duplicates.

## Implementation Scope

* Define inbox message identity.
* Persist receipt before or with domain effect.
* Deduplicate matching message and payload hash.
* Quarantine same ID with conflicting payload.
* Record source service and contract revision.
* Expose inbox conflict health.

## Acceptance Criteria

* [ ] First valid message is processed once.
* [ ] Matching duplicate is acknowledged without second effect.
* [ ] Conflicting duplicate is quarantined.
* [ ] Inbox and domain effect commit atomically where applicable.
* [ ] Unsupported source revision fails safely.
* [ ] Source identity is required.
* [ ] Inbox records survive restart.
* [ ] Conflict is visible.

## Required Tests

* First-delivery test.
* Matching duplicate test.
* Conflicting duplicate test.
* Crash-before-commit test.
* Crash-after-commit test.
* Unsupported revision test.

## Trust Evidence

A material conflict creates security or integrity evidence.

## Recovery

Inbox state allows safe replay after receiver restart.

## Evidence Output

* inbox idempotency report
* conflicting duplicate report

## Suggested Labels

`area:persistence`, `type:infrastructure`, `week:04`, `milestone:M3`, `priority:p1`

---

# FND-018 — Add Structured Logging

**Epic:** EPIC-FND-04
**Week:** 04
**Milestone:** M3
**Priority:** P1
**Size:** M
**Owner:** Observability
**Specifications:** SPEC-002, SPEC-008, SPEC-010
**ADRs:** ADR-0006, ADR-0027
**Depends on:** FND-004, FND-010

## Outcome

Runtime and foundation services emit local structured operational logs with stable fields and bounded retention.

## Implementation Scope

* Define log event naming and severity mapping.
* Include service, version, Runtime boot ID, trace and safe operation identity.
* Create JSON Lines local sink.
* Implement rotation and retention.
* Implement bounded attributes and message length.
* Separate operational logs from authoritative Trust Evidence.
* Exclude payload content by schema.

## Acceptance Criteria

* [ ] Logs are structured and parseable.
* [ ] Rotation works.
* [ ] Retention deletes eligible files.
* [ ] Service and boot identity are present.
* [ ] Trace correlation is present where available.
* [ ] No project source or prompt field exists.
* [ ] No secrets or headers are logged.
* [ ] Log write failure does not crash domain service.

## Required Tests

* Schema test.
* Rotation test.
* Retention test.
* Oversized attribute test.
* Control-character injection test.
* Sink failure test.
* Secret-canary test.

## Trust Evidence

Logs remain operational observations and never substitute for owner receipts.

## Recovery

After crash, the next log file opens safely; a partial final line is tolerated and reported.

## Evidence Output

* log schema
* rotation report
* log-injection report

## Suggested Labels

`area:observability`, `type:infrastructure`, `week:04`, `milestone:M3`, `priority:p1`

---

# FND-019 — Add Trace Propagation

**Epic:** EPIC-FND-04
**Week:** 04
**Milestone:** M3
**Priority:** P1
**Size:** M
**Owner:** Observability
**Specifications:** SPEC-002, SPEC-003, SPEC-008
**ADRs:** ADR-0004, ADR-0006, ADR-0027
**Depends on:** FND-009, FND-018

## Outcome

Operation traces cross Desktop, Runtime and service IPC without carrying project payloads.

## Implementation Scope

* Define ActivitySource naming.
* Create root operation span at Desktop Gateway or Runtime.
* Propagate W3C trace context through gRPC metadata.
* Create server and owner-service spans.
* Record safe status, latency and failure class.
* Define sampling for Development.
* Exclude source, paths and request payloads.

## Acceptance Criteria

* [ ] One health request has a connected trace.
* [ ] Trace IDs appear in logs.
* [ ] Cancellation is represented.
* [ ] Error status uses stable class.
* [ ] No full request or response appears in span attributes.
* [ ] No file path appears as high-cardinality attribute.
* [ ] Trace propagation survives asynchronous code.
* [ ] Trace creation can be disabled without changing authority.

## Required Tests

* Cross-process trace test.
* Cancellation trace test.
* Error trace test.
* Payload-canary test.
* High-cardinality rejection test.

## Trust Evidence

Trace IDs may link to Trust Evidence but traces remain non-authoritative.

## Recovery

Trace loss does not prevent domain recovery; dropped traces are counted safely.

## Evidence Output

* trace example
* payload leakage report
* latency overhead measurement

## Suggested Labels

`area:observability`, `type:infrastructure`, `week:04`, `milestone:M3`, `priority:p1`

---

# FND-020 — Add Redaction and Canary Tests

**Epic:** EPIC-FND-04
**Week:** 04
**Milestone:** M3
**Priority:** P0
**Size:** L
**Owner:** Security and Observability
**Specifications:** SPEC-008
**ADRs:** ADR-0006, ADR-0007, ADR-0027
**Depends on:** FND-018, FND-019

## Outcome

Prohibited secret, path and content classes are removed or rejected before diagnostics persist.

## Implementation Scope

* Create central redaction profiles.
* Create exact test canaries for secrets, source and paths.
* Redact structured fields before sink calls.
* Normalise safe path categories.
* Scan persisted logs and traces in tests.
* Define redaction failure behaviour.
* Create stable finding report without storing the secret.

## Acceptance Criteria

* [ ] Known canaries never appear in persisted diagnostics.
* [ ] Fields marked Secret are rejected or replaced.
* [ ] Absolute project root is normalised.
* [ ] Authorization-style headers are prohibited.
* [ ] Exception Data dictionaries are excluded.
* [ ] Redaction occurs before local persistence.
* [ ] A redaction failure drops or safely minimises the event according to policy.
* [ ] Test findings do not reproduce secret values.

## Required Tests

* Exact-canary suite.
* Pattern suite.
* Path normalisation suite.
* Exception leakage suite.
* Encoded-secret suite.
* Redaction failure injection.
* Post-persistence scan.

## Trust Evidence

Material redaction failure creates a security warning without the leaked value.

## Recovery

Unsafe diagnostic staging is quarantined and removed according to policy.

## Evidence Output

* redaction profile
* canary coverage report
* persisted-log scan report

## Suggested Labels

`area:observability`, `area:security`, `type:security`, `week:04`, `milestone:M3`, `priority:p0`

---

# FND-021 — Add Evidence Type Schema

**Epic:** EPIC-FND-04
**Week:** 09
**Milestone:** M3
**Priority:** P0
**Size:** M
**Owner:** Trust Evidence
**Specifications:** SPEC-003, SPEC-008
**ADRs:** ADR-0027
**Depends on:** FND-015, FND-016, FND-020

## Outcome

Trust Evidence types have stable ownership, authority, payload, retention and redaction definitions.

## Implementation Scope

* Define `opure.trust-evidence-type/1`.
* Define owner service and Authority Class.
* Define record schema and safe index fields.
* Define relationship eligibility.
* Define retention class.
* Define support-export eligibility and redaction profile.
* Create initial foundation catalogue.

## Acceptance Criteria

* [ ] Every type has a stable ID and immutable revision.
* [ ] Owner service is required.
* [ ] Authority Class is required.
* [ ] Payload schema is required.
* [ ] Sensitive fields are classified.
* [ ] Default retention is explicit.
* [ ] Unknown type cannot ingest as trusted.
* [ ] A type revision cannot silently change authority.

## Required Tests

* Schema validation.
* Missing owner test.
* Authority-change test.
* Unknown type test.
* Canonical hash test.
* Initial catalogue fixture test.

## Trust Evidence

This ticket defines the trust semantics used by all subsequent owner receipts.

## Recovery

Historical type revisions remain readable after catalogue updates.

## Evidence Output

* Evidence Type schema
* foundation type catalogue
* authority review

## Suggested Labels

`area:trust`, `type:feature`, `week:09`, `milestone:M3`, `priority:p0`

---

# FND-022 — Add Evidence Record Schema

**Epic:** EPIC-FND-04
**Week:** 09
**Milestone:** M3
**Priority:** P0
**Size:** M
**Owner:** Trust Evidence
**Specifications:** SPEC-003, SPEC-008
**ADRs:** ADR-0027
**Depends on:** FND-021

## Outcome

Every Trust Evidence record carries exact owner, authority, subject, time, payload hash, retention and integrity metadata.

## Implementation Scope

* Define `opure.trust-evidence-record/1`.
* Define opaque evidence ID.
* Define owner record and revision.
* Define authority, action and outcome.
* Define project, operation, workflow and trace references.
* Define sequence and hash fields.
* Define payload-reference forms and size limits.

## Acceptance Criteria

* [ ] Required fields validate.
* [ ] Project-scoped records require project identity.
* [ ] Authority is explicit.
* [ ] Occurred and observed time semantics are distinct.
* [ ] Payload hash is required.
* [ ] Inline payload is bounded.
* [ ] Secret-prohibited fields are rejected.
* [ ] Canonical record hash changes when any semantic field changes.

## Required Tests

* Schema fixtures.
* Missing owner test.
* Missing authority test.
* Oversized payload test.
* Secret-field test.
* Canonicalisation test.
* Project-scope test.

## Trust Evidence

The record itself is the common Trust Evidence envelope.

## Recovery

Records are immutable; corrections use a new revision or superseding record.

## Evidence Output

* Evidence Record schema
* canonicalisation vectors
* record examples

## Suggested Labels

`area:trust`, `type:feature`, `week:09`, `milestone:M3`, `priority:p0`

---

# FND-023 — Add Trust Evidence Database

**Epic:** EPIC-FND-04
**Week:** 09
**Milestone:** M3
**Priority:** P0
**Size:** L
**Owner:** Trust Evidence
**Specifications:** SPEC-003, SPEC-008
**ADRs:** ADR-0005, ADR-0027
**Depends on:** FND-014, FND-015, FND-021, FND-022

## Outcome

Trust Evidence has an isolated SQLite store for records, relationships, inbox, projections and retention metadata.

## Implementation Scope

* Create `trust.db`.
* Create migrations for types, records, payload references, relationships, owner sequences, inbox, projections and retention decisions.
* Use one writer.
* Enable foreign keys and WAL.
* Create indexes for safe queries.
* Keep Trust data separate from operational logs.

## Acceptance Criteria

* [ ] Database creates and migrates.
* [ ] Foreign keys pass.
* [ ] Duplicate evidence ID is constrained.
* [ ] Owner sequence is indexed.
* [ ] Project and operation query indexes exist.
* [ ] Sensitive payload is not copied into full-text index.
* [ ] Projection tables are rebuildable.
* [ ] Database health is visible.

## Required Tests

* Fresh database test.
* Migration test.
* Constraint test.
* Index plan test.
* Projection rebuild setup test.
* Corruption health test.

## Trust Evidence

The database stores Trust Evidence but does not become authoritative for the owner domain.

## Recovery

Database can be rebuilt from retained owner records where available; loss is reported as incomplete, not no activity.

## Evidence Output

* Trust database schema
* migration report
* query-plan baseline

## Suggested Labels

`area:trust`, `type:feature`, `week:09`, `milestone:M3`, `priority:p0`

---

# FND-024 — Add Trust Evidence Ingestion

**Epic:** EPIC-FND-04
**Week:** 09
**Milestone:** M3
**Priority:** P0
**Size:** L
**Owner:** Trust Evidence
**Specifications:** SPEC-003, SPEC-008
**ADRs:** ADR-0004, ADR-0005, ADR-0027
**Depends on:** FND-010, FND-017, FND-021, FND-022, FND-023

## Outcome

Authenticated owner services submit records through a validated idempotent ingestion pipeline.

## Implementation Scope

* Define ingestion contract.
* Authenticate owner service session.
* Validate Evidence Type revision.
* Validate owner identity, payload hash, sequence and record hash.
* Deduplicate matching records.
* Quarantine conflicting duplicates.
* Commit record and projection updates transactionally.
* Return stable receipt.

## Acceptance Criteria

* [ ] Correct owner record ingests.
* [ ] Wrong owner is denied.
* [ ] Unknown type quarantines.
* [ ] Matching duplicate is idempotent.
* [ ] Conflicting duplicate quarantines.
* [ ] Sequence gap is recorded.
* [ ] Payload hash mismatch fails.
* [ ] Ingestion transaction rolls back completely on error.

## Required Tests

* Valid ingestion.
* Owner impersonation.
* Unknown type.
* Matching duplicate.
* Conflicting duplicate.
* Gap test.
* Hash mismatch.
* Database failure rollback.

## Trust Evidence

Successful ingestion creates a Verified Service Receipt projection while preserving owner authority.

## Recovery

Owner outbox retries safely after Trust service restart.

## Evidence Output

* ingestion contract
* owner authentication report
* duplicate conflict report

## Suggested Labels

`area:trust`, `type:feature`, `week:09`, `milestone:M3`, `priority:p0`

---

# FND-025 — Add Trust Query Contract

**Epic:** EPIC-FND-04
**Week:** 09
**Milestone:** M3
**Priority:** P1
**Size:** M
**Owner:** Trust Evidence
**Specifications:** SPEC-008, SPEC-010
**ADRs:** ADR-0004, ADR-0027
**Depends on:** FND-023, FND-024

## Outcome

Desktop can query bounded Trust Evidence by project, operation, type, authority, outcome and time.

## Implementation Scope

* Define typed query request.
* Implement project and channel scope.
* Implement filters and cursor pagination.
* Return query snapshot time and projection generation.
* Return completeness and redaction metadata.
* Reject raw SQL and arbitrary expressions.
* Set time and result bounds.

## Acceptance Criteria

* [ ] Project query returns only authorised project records.
* [ ] Cursor pagination is stable.
* [ ] Time range is bounded.
* [ ] Authority filter works.
* [ ] Outcome filter works.
* [ ] Unknown filter value fails safely.
* [ ] Query reports freshness and completeness.
* [ ] Raw SQL or regex injection is impossible through the contract.

## Required Tests

* Filter tests.
* Pagination test.
* Cross-project denial.
* Large-range denial.
* Malformed cursor.
* Concurrent-ingestion snapshot test.
* Query performance smoke.

## Trust Evidence

Query results are projections with explicit freshness and completeness.

## Recovery

Projection rebuild changes generation; clients can refresh without treating stale results as complete.

## Evidence Output

* query schema
* cross-project report
* query-plan and latency report

## Suggested Labels

`area:trust`, `type:feature`, `week:09`, `milestone:M3`, `priority:p1`

---

# FND-026 — Add Windows Path-Reference Library

**Epic:** EPIC-FND-05
**Week:** 05
**Milestone:** M4
**Priority:** P0
**Size:** L
**Owner:** Filesystem
**Specifications:** SPEC-003, SPEC-008, SPEC-009, SPEC-011
**ADRs:** ADR-0009
**Depends on:** FND-002, FND-010, FND-020

## Outcome

Trusted services represent filesystem objects through typed, handle-verified Windows references rather than path-prefix checks.

## Implementation Scope

* Create typed logical and resolved path types.
* Open handles with appropriate sharing and flags.
* Resolve final path.
* Capture volume and file identity.
* Detect reparse points and alternate streams.
* Define local fixed, removable, network and unsupported classes.
* Provide containment verification through handles.

## Acceptance Criteria

* [ ] A valid local directory reference is created.
* [ ] Drive-relative paths are denied.
* [ ] Device paths are denied.
* [ ] Traversal is denied.
* [ ] Reparse escape is detected.
* [ ] Alternate data streams are denied.
* [ ] Same path string with different file identity is detectable.
* [ ] Raw path concatenation is absent from public service contracts.

## Required Tests

* Traversal corpus.
* Symlink and junction corpus.
* Case variation.
* Trailing dot and space.
* Reserved device names.
* ADS.
* File replacement race.
* Volume identity change.

## Trust Evidence

Material path denial later creates `security.path-denied` with a normalised safe category.

## Recovery

A stale or changed reference fails closed and can be reacquired through explicit project selection.

## Evidence Output

* filesystem threat model
* path adversarial report
* typed reference API review

## Suggested Labels

`area:filesystem`, `area:security`, `type:feature`, `week:05`, `milestone:M4`, `priority:p0`

---

# FND-027 — Add Trusted Folder Picker Adapter

**Epic:** EPIC-FND-05
**Week:** 05
**Milestone:** M4
**Priority:** P0
**Size:** M
**Owner:** Desktop and Filesystem
**Specifications:** SPEC-009, SPEC-010, SPEC-011
**ADRs:** ADR-0002, ADR-0009
**Depends on:** FND-005, FND-026

## Outcome

Desktop lets the developer select a project directory and transfers only a verified root reference to the Project Service.

## Implementation Scope

* Integrate trusted Windows-compatible folder picker through Desktop adapter.
* Acquire the selected path once.
* Pass selection to the trusted filesystem boundary.
* Display path classification and warnings.
* Support cancellation.
* Do not persist authority in Desktop.

## Acceptance Criteria

* [ ] Developer can select a local folder.
* [ ] Cancellation makes no state change.
* [ ] Selected folder is handle verified.
* [ ] Unsupported path class is explained.
* [ ] Desktop does not open project files.
* [ ] Project Service receives an opaque verified reference.
* [ ] Keyboard can operate the picker flow.
* [ ] Recent path display does not grant authority.

## Required Tests

* Picker cancellation.
* Local folder.
* Network folder policy.
* Reparse target.
* Deleted folder after selection.
* Keyboard UI test.

## Trust Evidence

Selection itself is a user intent; Project Service later owns registration evidence.

## Recovery

A failed acquisition returns to the project-open screen without persisting a broken project.

## Evidence Output

* folder picker flow
* capability transfer diagram
* accessibility result

## Suggested Labels

`area:desktop`, `area:filesystem`, `type:feature`, `week:05`, `milestone:M4`, `priority:p0`

---

# FND-028 — Add Project Service Database

**Epic:** EPIC-FND-05
**Week:** 05
**Milestone:** M4
**Priority:** P0
**Size:** M
**Owner:** Project Service
**Specifications:** SPEC-003, SPEC-011
**ADRs:** ADR-0005, ADR-0009, ADR-0027
**Depends on:** FND-014, FND-015, FND-016, FND-026

## Outcome

Project Service owns durable project identity, lifecycle and root-reference metadata.

## Implementation Scope

* Create `projects.db`.
* Create project, root reference, repository identity, lifecycle and outbox tables.
* Define stable opaque Project ID.
* Define channel scope.
* Implement project create, read and list repository methods.
* Add migration and health.

## Acceptance Criteria

* [ ] Project ID is random and stable.
* [ ] Project root reference is owner controlled.
* [ ] Duplicate exact project registration is handled idempotently.
* [ ] Different identity at same display path is not silently merged.
* [ ] Lifecycle state persists.
* [ ] Outbox is in the same transaction.
* [ ] Database passes integrity checks.
* [ ] No other service writes `projects.db`.

## Required Tests

* Create project.
* Duplicate exact project.
* Same path different identity.
* Channel isolation.
* Migration.
* Crash after commit.
* Architecture ownership test.

## Trust Evidence

Project lifecycle receipts originate from the Project Service outbox.

## Recovery

Project database restores through its Backup Adapter later; missing roots become Unavailable rather than deleted.

## Evidence Output

* Project database schema
* identity test report
* ownership conformance report

## Suggested Labels

`area:project`, `type:feature`, `week:05`, `milestone:M4`, `priority:p0`

---

# FND-029 — Add Open Project Flow

**Epic:** EPIC-FND-05
**Week:** 05
**Milestone:** M4
**Priority:** P0
**Size:** L
**Owner:** Project Service
**Specifications:** SPEC-009, SPEC-010, SPEC-011
**ADRs:** ADR-0004, ADR-0009, ADR-0027
**Depends on:** FND-010, FND-027, FND-028

## Outcome

Desktop requests project opening; Project Service validates, registers and returns an explicit project state.

## Implementation Scope

* Define Open Project contract.
* Validate selected root reference.
* Check policy and path class.
* Create or resolve project identity.
* Set lifecycle to Opening then Open.
* Request initial Workspace Snapshot through a future-stable contract boundary.
* Return stable project summary and errors.

## Acceptance Criteria

* [ ] Valid project opens.
* [ ] Cancellation before commit creates no project.
* [ ] Root identity is revalidated before commit.
* [ ] Unsupported path is denied.
* [ ] Duplicate exact project reopens existing identity.
* [ ] Same display path with changed identity requires review.
* [ ] Lifecycle transitions are durable.
* [ ] Desktop receives no raw database object.

## Required Tests

* New project open.
* Existing project reopen.
* Cancellation.
* Root deletion race.
* Identity substitution.
* Policy denial.
* Runtime restart after open commit.

## Trust Evidence

Creates `project.registered` when new and `project.opened` when open succeeds.

## Recovery

After crash, Project Service reconciles Opening state and either completes or marks Recovery Required.

## Evidence Output

* Open Project sequence diagram
* race-condition report
* contract fixtures

## Suggested Labels

`area:project`, `type:feature`, `week:05`, `milestone:M4`, `priority:p0`

---

# FND-030 — Add Project Open Trust Receipt

**Epic:** EPIC-FND-05
**Week:** 05
**Milestone:** M4
**Priority:** P0
**Size:** M
**Owner:** Project Service and Trust Evidence
**Specifications:** SPEC-008, SPEC-011
**ADRs:** ADR-0005, ADR-0027
**Depends on:** FND-016, FND-021, FND-022, FND-024, FND-029

## Outcome

A successful project open commits an authoritative owner receipt that appears through Trust Evidence.

## Implementation Scope

* Define project registration and open Evidence Types.
* Create owner payload schema.
* Include Project ID, safe root class, repository state and operation identity.
* Write outbox record transactionally with lifecycle state.
* Ingest and project in Trust service.
* Exclude raw source and unnecessary absolute path.

## Acceptance Criteria

* [ ] Project open state and outbox commit together.
* [ ] Receipt owner is Project Service.
* [ ] Authority Class is Authoritative Domain State Transition.
* [ ] Project ID and operation ID are present.
* [ ] Raw root path is omitted or normalised.
* [ ] Failed open does not emit successful receipt.
* [ ] Duplicate delivery is idempotent.
* [ ] Trust query returns the receipt.

## Required Tests

* Successful receipt.
* Failed-open no-success receipt.
* Transaction rollback.
* Duplicate delivery.
* Path leakage scan.
* Owner impersonation denial.

## Trust Evidence

This is the first user-relevant authoritative project receipt in the vertical slice.

## Recovery

Pending receipt delivery resumes after either Project or Trust service restart.

## Evidence Output

* Project Evidence Type
* sample record
* transaction test

## Suggested Labels

`area:project`, `area:trust`, `type:feature`, `week:05`, `milestone:M4`, `priority:p0`

---

# 28. Engineering Tickets FND-031 through FND-060

# FND-031 — Add Repository Identity Detection

**Epic:** EPIC-FND-06
**Week:** 06
**Milestone:** M4
**Priority:** P1
**Size:** M
**Owner:** Project Service
**Specifications:** SPEC-009, SPEC-011
**ADRs:** ADR-0009, ADR-0027
**Depends on:** FND-026, FND-028, FND-029

## Outcome

Project Service records safe repository identity and current repository state without granting repository-write authority.

## Implementation Scope

* Detect Git repository where available.
* Resolve repository root within the verified project boundary.
* Record safe repository identity, HEAD commit, branch metadata and working-tree state.
* Handle non-Git projects explicitly.
* Avoid executing hooks or arbitrary Git configuration.
* Normalise or hash remote metadata according to policy.

## Acceptance Criteria

* [ ] Git repository is detected without running project scripts.
* [ ] Non-Git project remains supported.
* [ ] Repository root cannot escape project policy.
* [ ] HEAD identity is exact where available.
* [ ] Dirty working tree is represented.
* [ ] Remote credentials are never read or logged.
* [ ] Repository detection failure does not prevent opening a valid non-Git project.
* [ ] Repository identity is stable enough to detect moved and replaced repositories.

## Required Tests

* Git repository test.
* Non-Git directory test.
* Nested repository test.
* Repository escape test.
* Remote URL redaction test.
* Detached HEAD test.
* Corrupt repository metadata test.

## Trust Evidence

Repository detection is a verified service observation linked to the project receipt.

## Recovery

Missing or corrupt repository metadata marks repository state Degraded while preserving project access.

## Evidence Output

* repository identity design
* Git detection report
* remote metadata privacy report

## Suggested Labels

`area:project`, `type:feature`, `week:06`, `milestone:M4`, `priority:p1`

---

# FND-032 — Add Project List UI

**Epic:** EPIC-FND-06
**Week:** 06
**Milestone:** M4
**Priority:** P1
**Size:** M
**Owner:** Desktop
**Specifications:** SPEC-010, SPEC-011
**ADRs:** ADR-0002, ADR-0004
**Depends on:** FND-013, FND-025, FND-028, FND-029

## Outcome

Desktop lists registered projects with honest availability, repository and last-open state.

## Implementation Scope

* Create Project list contract and view model.
* Show display name, safe location summary, repository class, last-open time and availability.
* Support open and remove-registration commands through Project Service.
* Add empty and error states.
* Add keyboard, Narrator and high-contrast behaviour.

## Acceptance Criteria

* [ ] Project list comes from Project Service projection.
* [ ] Unavailable project is not silently removed.
* [ ] Open command calls the service rather than reading files.
* [ ] Remove registration is distinct from deleting files.
* [ ] Stale list is labelled after disconnection.
* [ ] Keyboard can select and open a project.
* [ ] Narrator reads project and availability.
* [ ] Large list remains responsive.

## Required Tests

* Empty state.
* One and many projects.
* Unavailable project.
* Desktop reconnect.
* Keyboard and Narrator.
* Large-list performance.

## Trust Evidence

Desktop creates no project authority; open and removal decisions are recorded by Project Service.

## Recovery

On reconnect the UI refreshes from Project Service and discards stale local ordering only after a successful query.

## Evidence Output

* Project list UI evidence
* accessibility report
* projection contract

## Suggested Labels

`area:desktop`, `type:feature`, `week:06`, `milestone:M4`, `priority:p1`

---

# FND-033 — Add Workspace Service Contract

**Epic:** EPIC-FND-06
**Week:** 06
**Milestone:** M4
**Priority:** P0
**Size:** M
**Owner:** Workspace Service
**Specifications:** SPEC-003, SPEC-009
**ADRs:** ADR-0004, ADR-0009
**Depends on:** FND-008, FND-026, FND-028

## Outcome

Project Service can request a bounded Workspace Snapshot through an explicit owner contract.

## Implementation Scope

* Define create, get and invalidate snapshot operations.
* Bind requests to Project ID and verified root reference.
* Define snapshot generation, file entry and repository summary messages.
* Define size, count and duration limits.
* Define cancellation and partial-result policy.
* Exclude raw file content from the initial contract.

## Acceptance Criteria

* [ ] Contract binds one project.
* [ ] Snapshot generation is required.
* [ ] File entries use logical paths and hashes.
* [ ] Raw absolute paths are absent.
* [ ] Maximum file count and bytes are declared.
* [ ] Cancellation returns no misleading Complete snapshot.
* [ ] Unsupported file type is represented safely.
* [ ] Contract revision is versioned.

## Required Tests

* Schema round trip.
* Cross-project reference denial.
* Limit validation.
* Cancellation fixture.
* Unknown file class fixture.

## Trust Evidence

Snapshot creation later emits an authoritative Workspace receipt.

## Recovery

An interrupted snapshot is discarded or explicitly Partial and never becomes the current generation.

## Evidence Output

* Workspace contract
* limit rationale
* contract fixtures

## Suggested Labels

`area:workspace`, `type:feature`, `week:06`, `milestone:M4`, `priority:p0`

---

# FND-034 — Add File Inventory Generation

**Epic:** EPIC-FND-06
**Week:** 06
**Milestone:** M4
**Priority:** P0
**Size:** L
**Owner:** Workspace Service
**Specifications:** SPEC-009, SPEC-011
**ADRs:** ADR-0009
**Depends on:** FND-026, FND-033

## Outcome

Workspace Service enumerates a project through verified handles and creates a bounded logical file inventory.

## Implementation Scope

* Enumerate from the verified project root.
* Apply initial exclusion rules.
* Classify directories, regular files, reparse points, hidden items and unsupported entries.
* Create logical relative paths.
* Capture size, timestamps as observations and file identity.
* Enforce file-count and traversal budgets.
* Do not read file content yet.

## Acceptance Criteria

* [ ] Inventory remains inside project boundary.
* [ ] Logical paths contain no traversal.
* [ ] Reparse entries follow selected deny policy.
* [ ] Hidden-file policy is explicit.
* [ ] Unsupported entries are recorded or excluded with reason.
* [ ] Enumeration is cancellable.
* [ ] File-count limit is enforced.
* [ ] Absolute paths do not enter normal inventory records.

## Required Tests

* Small tree.
* Large tree.
* Deep tree.
* Symlink and junction.
* Hidden files.
* Case-only names.
* Enumeration cancellation.
* Directory mutation during scan.

## Trust Evidence

Inventory metadata is Workspace-owned state and feeds the snapshot receipt.

## Recovery

A failed scan leaves the previous current snapshot unchanged.

## Evidence Output

* inventory algorithm report
* adversarial tree fixtures
* enumeration benchmark

## Suggested Labels

`area:workspace`, `type:feature`, `week:06`, `milestone:M4`, `priority:p0`

---

# FND-035 — Add Safe File Hashing

**Epic:** EPIC-FND-06
**Week:** 06
**Milestone:** M4
**Priority:** P0
**Size:** L
**Owner:** Workspace Service
**Specifications:** SPEC-008, SPEC-009
**ADRs:** ADR-0009, ADR-0027
**Depends on:** FND-026, FND-034

## Outcome

Eligible files receive SHA-256 hashes from verified stable handles under explicit size and change checks.

## Implementation Scope

* Open file through validated relative reference.
* Capture identity and size before read.
* Stream SHA-256 with bounded buffers.
* Recheck identity, size and modification after read.
* Classify unstable file and retry boundedly.
* Apply maximum file-size policy.
* Avoid logging content.

## Acceptance Criteria

* [ ] Stable file hash is reproducible.
* [ ] Changed-during-read file is not reported as stable.
* [ ] Replaced file identity is detected.
* [ ] Oversized file is excluded with reason.
* [ ] Cancellation stops promptly.
* [ ] Hashing does not follow a changed reparse target.
* [ ] Content never appears in logs or traces.
* [ ] Hash algorithm and version are recorded.

## Required Tests

* Known-answer hash.
* Concurrent modification.
* File replacement race.
* Oversized file.
* Locked file.
* Cancellation.
* Reparse substitution.
* Content-canary leakage.

## Trust Evidence

Hashes support snapshot identity and integrity; they are not proof that content is safe.

## Recovery

Unstable or unreadable files remain explicit exclusions and cannot silently inherit a prior hash.

## Evidence Output

* hashing correctness report
* race-condition report
* throughput benchmark

## Suggested Labels

`area:workspace`, `type:feature`, `week:06`, `milestone:M4`, `priority:p0`

---

# FND-036 — Add Workspace Generation

**Epic:** EPIC-FND-06
**Week:** 06
**Milestone:** M4
**Priority:** P0
**Size:** M
**Owner:** Workspace Service
**Specifications:** SPEC-009
**ADRs:** ADR-0005, ADR-0009, ADR-0027
**Depends on:** FND-014, FND-015, FND-034, FND-035

## Outcome

A complete immutable Workspace Snapshot generation is persisted and becomes current atomically.

## Implementation Scope

* Create Workspace database tables for generations, entries, exclusions and repository summary.
* Build a generation in staging tables.
* Compute canonical generation hash.
* Commit generation and current pointer atomically.
* Retain prior generation according to initial policy.
* Expose get-current and get-by-generation.

## Acceptance Criteria

* [ ] A generation is immutable after commit.
* [ ] Current pointer and generation commit atomically.
* [ ] Failed generation does not replace current.
* [ ] Generation hash is deterministic.
* [ ] Entries bind file identity and content hash.
* [ ] Exclusions are included.
* [ ] Prior generation remains queryable.
* [ ] Only Workspace Service writes the database.

## Required Tests

* First generation.
* Second generation.
* Failure before commit.
* Failure during pointer update.
* Canonical hash.
* Concurrent snapshot request.
* Database restart.

## Trust Evidence

Committed generation is authoritative Workspace state.

## Recovery

After crash, incomplete staging rows are discarded and the last committed current generation remains.

## Evidence Output

* Workspace database schema
* atomic generation report
* canonical hash vectors

## Suggested Labels

`area:workspace`, `type:feature`, `week:06`, `milestone:M4`, `priority:p0`

---

# FND-037 — Add Change Reconciliation

**Epic:** EPIC-FND-06
**Week:** 06
**Milestone:** M4
**Priority:** P1
**Size:** L
**Owner:** Workspace Service
**Specifications:** SPEC-009, SPEC-011
**ADRs:** ADR-0009
**Depends on:** FND-034, FND-035, FND-036

## Outcome

Workspace Service detects changed, added, deleted and renamed files and creates a new verified generation.

## Implementation Scope

* Add filesystem watcher as a hint, not authority.
* Queue bounded reconciliation.
* Rescan affected paths through verified handles.
* Fall back to full scan after watcher overflow or uncertainty.
* Compare generations.
* Represent additions, changes, deletions and likely renames.
* Debounce without losing durable correctness.

## Acceptance Criteria

* [ ] File addition creates a new generation.
* [ ] Modification changes hash.
* [ ] Deletion is represented.
* [ ] Watcher overflow triggers authoritative rescan.
* [ ] A missed watcher event is repaired by reconciliation.
* [ ] Rename detection is labelled as deterministic or heuristic.
* [ ] Rapid edits do not create unbounded queue growth.
* [ ] Current generation remains complete.

## Required Tests

* Add, modify, delete.
* Rename.
* Rapid edit storm.
* Watcher overflow.
* Watcher disabled.
* Runtime restart.
* Directory replacement race.

## Trust Evidence

New generation and invalidation receipts are published by Workspace Service.

## Recovery

On restart, Workspace compares durable current generation with a fresh bounded scan before claiming freshness.

## Evidence Output

* reconciliation state machine
* watcher-loss report
* edit-storm benchmark

## Suggested Labels

`area:workspace`, `type:feature`, `week:06`, `milestone:M4`, `priority:p1`

---

# FND-038 — Add Workspace Snapshot Receipt

**Epic:** EPIC-FND-06
**Week:** 06
**Milestone:** M4
**Priority:** P0
**Size:** M
**Owner:** Workspace Service and Trust Evidence
**Specifications:** SPEC-008, SPEC-009
**ADRs:** ADR-0005, ADR-0027
**Depends on:** FND-016, FND-021, FND-022, FND-024, FND-036

## Outcome

Every committed current Workspace generation publishes an authoritative snapshot receipt.

## Implementation Scope

* Define Workspace snapshot Evidence Type.
* Include project, generation, generation hash, entry count, exclusion count, repository summary hash and operation identity.
* Write outbox transactionally with current pointer.
* Link to Project Open receipt.
* Project content and raw paths remain excluded.

## Acceptance Criteria

* [ ] Receipt commits with generation activation.
* [ ] Owner is Workspace Service.
* [ ] Authority is Authoritative Domain State Transition or Verified Service Receipt according to final catalogue.
* [ ] Generation hash matches database state.
* [ ] Project and operation IDs are present.
* [ ] Receipt links to project-open operation.
* [ ] No file content is included.
* [ ] Duplicate delivery is idempotent.

## Required Tests

* Receipt transaction.
* Hash match.
* Failed generation no receipt.
* Relationship validation.
* Path and content leakage scan.
* Duplicate delivery.

## Trust Evidence

The receipt is the visible proof of which immutable snapshot became current.

## Recovery

Pending delivery resumes after restart; a rebuilt projection retains owner sequence and generation identity.

## Evidence Output

* Workspace Evidence Type
* sample receipt
* relationship test

## Suggested Labels

`area:workspace`, `area:trust`, `type:feature`, `week:06`, `milestone:M4`, `priority:p0`

---

# FND-039 — Add Setting Definition Schema

**Epic:** EPIC-FND-07
**Week:** 07
**Milestone:** M5
**Priority:** P0
**Size:** L
**Owner:** Configuration Service
**Specifications:** SPEC-001, SPEC-002, SPEC-003, SPEC-008
**ADRs:** ADR-0026
**Depends on:** FND-015, FND-021

## Outcome

Every configurable value has a stable typed definition with scope, merge, sensitivity and validation rules.

## Implementation Scope

* Define Setting Definition schema.
* Define key, type, default, source scope, merge strategy, sensitivity, restart impact and UI metadata.
* Define Product Invariant restrictions.
* Create canonical hash and immutable revision.
* Create initial foundation definitions.
* Generate documentation from definitions.

## Acceptance Criteria

* [ ] Every setting has a stable key.
* [ ] Type and default validate.
* [ ] Allowed sources are explicit.
* [ ] Merge strategy is explicit.
* [ ] Sensitivity classification is explicit.
* [ ] Secret type cannot use ordinary configuration storage.
* [ ] Restart impact is explicit.
* [ ] Revision cannot silently change semantics.

## Required Tests

* Schema fixtures.
* Missing type.
* Invalid default.
* Illegal project source.
* Secret-in-config definition.
* Merge strategy validation.
* Canonical hash.

## Trust Evidence

Definition changes are configuration-governance evidence, not ordinary user changes.

## Recovery

Historical snapshots retain the exact definition revision used.

## Evidence Output

* Setting Definition schema
* initial catalogue
* definition review

## Suggested Labels

`area:configuration`, `type:feature`, `week:07`, `milestone:M5`, `priority:p0`

---

# FND-040 — Add Policy Definition Schema

**Epic:** EPIC-FND-07
**Week:** 07
**Milestone:** M5
**Priority:** P0
**Size:** L
**Owner:** Configuration and Security
**Specifications:** SPEC-001, SPEC-008
**ADRs:** ADR-0026
**Depends on:** FND-039

## Outcome

Non-bypassable Product Policies and future enterprise constraints have versioned deterministic definitions.

## Implementation Scope

* Define Policy Definition schema.
* Define policy key, source authority, protected action or setting, decision model, input types, precedence and explanation.
* Define Deny, Allow, Require Approval and Constrain results where applicable.
* Create initial Product Policy catalogue.
* Bind evaluation implementation revision.

## Acceptance Criteria

* [ ] Product Policy is highest non-amendable application authority.
* [ ] Project files cannot define permissions or capabilities.
* [ ] Policy inputs are typed.
* [ ] Policy result is deterministic.
* [ ] Explanation template exists.
* [ ] Unknown policy revision fails safe.
* [ ] Policy definition hash is canonical.
* [ ] AI output cannot become policy input without deterministic classification.

## Required Tests

* Schema validation.
* Project authority-escalation attempt.
* Unknown revision.
* Conflicting policy source.
* Determinism test.
* Canonical hash.

## Trust Evidence

Policy evaluation later emits authoritative decision receipts for material actions.

## Recovery

Snapshots preserve policy revision; current Product Policy is reapplied during restore.

## Evidence Output

* Policy Definition schema
* Product Policy catalogue
* authority review

## Suggested Labels

`area:configuration`, `area:security`, `type:security`, `week:07`, `milestone:M5`, `priority:p0`

---

# FND-041 — Add Product Defaults Catalogue

**Epic:** EPIC-FND-07
**Week:** 07
**Milestone:** M5
**Priority:** P0
**Size:** M
**Owner:** Configuration Service
**Specifications:** SPEC-001, SPEC-002, SPEC-010
**ADRs:** ADR-0026
**Depends on:** FND-039, FND-040

## Outcome

Opure ships one source-controlled, package-bound catalogue of safe default values.

## Implementation Scope

* Create packaged Product Defaults document.
* Include only initial Gate A settings.
* Bind catalogue to Setting Definition revisions.
* Validate at build and Runtime start.
* Expose safe read API.
* Record product version and catalogue hash.
* Keep secrets and machine-specific values out.

## Acceptance Criteria

* [ ] Every default references a known setting.
* [ ] Every required setting has a valid default or explicit required source.
* [ ] Catalogue validation fails the build on error.
* [ ] Runtime refuses an internally inconsistent catalogue.
* [ ] Cloud policy defaults to Local Only.
* [ ] Plugins, MCP and remote providers remain disabled.
* [ ] Defaults are package controlled.
* [ ] User or project source cannot mutate the catalogue.

## Required Tests

* Complete catalogue validation.
* Unknown key.
* Wrong type.
* Missing required default.
* Local-only default test.
* Package tamper simulation.

## Trust Evidence

Runtime startup evidence records the catalogue revision and hash.

## Recovery

A restored user profile is resolved against current packaged defaults.

## Evidence Output

* Product Defaults catalogue
* build validation report
* default-policy review

## Suggested Labels

`area:configuration`, `type:feature`, `week:07`, `milestone:M5`, `priority:p0`

---

# FND-042 — Add User Base Profile

**Epic:** EPIC-FND-07
**Week:** 07
**Milestone:** M5
**Priority:** P0
**Size:** L
**Owner:** Configuration Service
**Specifications:** SPEC-002, SPEC-008, SPEC-010
**ADRs:** ADR-0005, ADR-0026
**Depends on:** FND-014, FND-015, FND-039, FND-041

## Outcome

The developer can persist user-scoped requested values in an immutable revisioned Profile.

## Implementation Scope

* Create configuration database and Profile tables.
* Create default User Base Profile.
* Implement profile revision and canonical hash.
* Validate requested values against Setting Definitions.
* Store no secrets.
* Expose read and proposed-change APIs.
* Create initial Profile editor projection.

## Acceptance Criteria

* [ ] User Base Profile exists on first run.
* [ ] Each change creates a new immutable revision.
* [ ] Unknown setting fails.
* [ ] Wrong type fails.
* [ ] Project-only setting fails in user profile.
* [ ] Secret value fails.
* [ ] Profile hash is deterministic.
* [ ] Prior revision remains inspectable.

## Required Tests

* First-run creation.
* Valid update.
* Unknown key.
* Wrong type.
* Scope denial.
* Secret denial.
* Revision history.
* Crash during update.

## Trust Evidence

Profile commits later create configuration owner receipts.

## Recovery

Last committed Profile revision survives restart and is included by the Configuration Backup Adapter.

## Evidence Output

* configuration database schema
* Profile revision report
* secret-denial report

## Suggested Labels

`area:configuration`, `type:feature`, `week:07`, `milestone:M5`, `priority:p0`

---

# FND-043 — Add Strict Project JSON Parser

**Epic:** EPIC-FND-07
**Week:** 07
**Milestone:** M5
**Priority:** P0
**Size:** L
**Owner:** Configuration Service
**Specifications:** SPEC-008, SPEC-009
**ADRs:** ADR-0009, ADR-0026
**Depends on:** FND-033, FND-035, FND-039

## Outcome

Project configuration is parsed as strict UTF-8 JSON data with bounded structure and no executable or remote behaviour.

## Implementation Scope

* Parse bytes supplied by Workspace Snapshot.
* Require UTF-8 under explicit BOM policy.
* Reject comments and trailing commas.
* Bound file size, depth, property count, string length and numeric forms.
* Preserve source hash and parse diagnostics.
* Do not resolve remote references.
* Do not perform environment substitution.

## Acceptance Criteria

* [ ] Valid strict JSON parses.
* [ ] Comments fail.
* [ ] Trailing commas fail.
* [ ] Invalid UTF-8 fails.
* [ ] Excessive depth fails.
* [ ] Oversized file fails.
* [ ] Duplicate handling is delegated to explicit detector and never silently accepted.
* [ ] Parser never reads another file or network resource.

## Required Tests

* Valid fixtures.
* Comment fixture.
* Trailing comma.
* Invalid UTF-8.
* Deep nesting.
* Large strings.
* Malformed numbers.
* No-network test.

## Trust Evidence

Parse success is a deterministic validation result; invalid source is an owner observation.

## Recovery

Invalid new content does not replace the last-known-good effective configuration.

## Evidence Output

* parser limits
* malformed corpus report
* no-network capture

## Suggested Labels

`area:configuration`, `type:feature`, `week:07`, `milestone:M5`, `priority:p0`

---

# FND-044 — Add Duplicate-Key Detector

**Epic:** EPIC-FND-07
**Week:** 07
**Milestone:** M5
**Priority:** P0
**Size:** M
**Owner:** Configuration Service
**Specifications:** SPEC-008
**ADRs:** ADR-0026
**Depends on:** FND-043

## Outcome

Configuration rejects duplicate object keys at every nesting level before semantic validation.

## Implementation Scope

* Implement token-level duplicate tracking.
* Use exact ordinal key semantics defined by the format.
* Report safe path and key name.
* Bound memory by parser limits.
* Integrate with strict parser and schema validation.
* Create adversarial Unicode and case fixtures.

## Acceptance Criteria

* [ ] Same exact key in one object fails.
* [ ] Duplicate nested key fails.
* [ ] Same key in different objects succeeds.
* [ ] Case-different keys follow documented case-sensitive policy.
* [ ] Escaped equivalent key is detected.
* [ ] Failure location is actionable.
* [ ] No last-key-wins behaviour exists.
* [ ] Detector respects parser limits.

## Required Tests

* Top-level duplicate.
* Nested duplicate.
* Escaped duplicate.
* Case variants.
* Unicode escapes.
* Large object.
* Malformed JSON interaction.

## Trust Evidence

Invalid-source evidence records category and safe location, not full configuration content.

## Recovery

The prior valid source remains active.

## Evidence Output

* duplicate-key algorithm
* fixture suite
* ambiguity review

## Suggested Labels

`area:configuration`, `type:feature`, `week:07`, `milestone:M5`, `priority:p0`

---

# FND-045 — Add Local Schema Registry

**Epic:** EPIC-FND-07
**Week:** 07
**Milestone:** M5
**Priority:** P1
**Size:** L
**Owner:** Configuration Service
**Specifications:** SPEC-003, SPEC-008
**ADRs:** ADR-0026
**Depends on:** FND-039, FND-040, FND-043, FND-044

## Outcome

Configuration validates only against packaged or trusted local versioned schemas.

## Implementation Scope

* Create schema registry contract and storage.
* Register Setting, Policy, Profile and Project Settings schemas.
* Resolve local schema IDs only.
* Reject remote HTTP, HTTPS and file references.
* Pin validator library and supported JSON Schema subset.
* Cache compiled schemas safely.
* Expose schema revision and hash.

## Acceptance Criteria

* [ ] Known local schema resolves.
* [ ] Unknown schema fails.
* [ ] Remote `$ref` fails.
* [ ] File-system `$ref` fails.
* [ ] Schema cycle is bounded and handled.
* [ ] Validator version is recorded.
* [ ] Compiled schema cache cannot mix revisions.
* [ ] Validation errors are stable and actionable.

## Required Tests

* Known schema.
* Unknown schema.
* HTTP reference.
* File reference.
* Cycle.
* Deep schema.
* Cache revision separation.
* Validator compatibility fixtures.

## Trust Evidence

Schema validation is deterministic evidence and cannot grant authority.

## Recovery

Schemas ship with the package; restored records bind historical revisions and migrate through trusted code.

## Evidence Output

* schema registry catalogue
* remote-reference denial report
* validator spike result

## Suggested Labels

`area:configuration`, `type:feature`, `week:07`, `milestone:M5`, `priority:p1`

---

# FND-046 — Add Project Settings Acquisition

**Epic:** EPIC-FND-08
**Week:** 08
**Milestone:** M5
**Priority:** P0
**Size:** L
**Owner:** Configuration and Workspace
**Specifications:** SPEC-008, SPEC-009, SPEC-011
**ADRs:** ADR-0009, ADR-0026
**Depends on:** FND-036, FND-043, FND-045

## Outcome

Configuration Service acquires `.opure/project.settings.json` only from an exact committed Workspace Snapshot.

## Implementation Scope

* Define canonical project settings logical path.
* Request source bytes and metadata from Workspace through bounded contract.
* Bind Project ID, Workspace generation, file identity and content hash.
* Handle missing file as valid absence.
* Parse and validate without direct filesystem access.
* Record source status.

## Acceptance Criteria

* [ ] Configuration Service never opens project files directly.
* [ ] Missing file produces no error.
* [ ] Present file binds exact Workspace generation and hash.
* [ ] Changed file after snapshot does not change the current evaluation.
* [ ] Oversized file is rejected.
* [ ] Invalid file is visible.
* [ ] Cross-project source is denied.
* [ ] Source content remains outside logs.

## Required Tests

* Missing source.
* Valid source.
* Changed after snapshot.
* Cross-project request.
* Oversized file.
* Invalid JSON.
* Content-canary leakage.

## Trust Evidence

Source acquisition and validation results become Configuration owner evidence.

## Recovery

The last valid source revision remains available until a new valid source is committed or the project explicitly removes it.

## Evidence Output

* source acquisition contract
* snapshot binding test
* service-boundary architecture test

## Suggested Labels

`area:configuration`, `area:workspace`, `type:feature`, `week:08`, `milestone:M5`, `priority:p0`

---

# FND-047 — Add Setting Merge

**Epic:** EPIC-FND-08
**Week:** 08
**Milestone:** M5
**Priority:** P0
**Size:** L
**Owner:** Configuration Service
**Specifications:** SPEC-001, SPEC-003, SPEC-008
**ADRs:** ADR-0026
**Depends on:** FND-039, FND-041, FND-042, FND-046

## Outcome

Product Defaults, User Base Profile and Project Settings merge deterministically according to each Setting Definition.

## Implementation Scope

* Implement source ordering only for allowed sources.
* Implement Replace and any Gate A-required merge strategies.
* Preserve requested value and source.
* Reject unsupported strategy.
* Produce per-key merge trace.
* Do not use one global last-writer-wins algorithm.
* Keep result immutable.

## Acceptance Criteria

* [ ] Every initial setting resolves deterministically.
* [ ] Disallowed project source is ignored with policy explanation, not silently accepted.
* [ ] Replace strategy works.
* [ ] Unsupported merge strategy fails configuration activation.
* [ ] Merge order is stable.
* [ ] Requested values remain inspectable.
* [ ] No source can mutate another source.
* [ ] Repeated evaluation produces identical result.

## Required Tests

* Product-only.
* User override.
* Project override allowed.
* Project override denied.
* Missing source.
* Unsupported strategy.
* Determinism property test.
* Large initial catalogue benchmark.

## Trust Evidence

Merge output remains requested configuration until policy evaluation produces an effective result.

## Recovery

A merge failure retains the prior valid Effective Snapshot.

## Evidence Output

* merge algorithm report
* per-key trace examples
* determinism report

## Suggested Labels

`area:configuration`, `type:feature`, `week:08`, `milestone:M5`, `priority:p0`

---

# FND-048 — Add Product Policy Evaluation

**Epic:** EPIC-FND-08
**Week:** 08
**Milestone:** M5
**Priority:** P0
**Size:** L
**Owner:** Configuration and Security
**Specifications:** SPEC-001, SPEC-008
**ADRs:** ADR-0026
**Depends on:** FND-040, FND-047

## Outcome

Non-bypassable Product Policies deterministically constrain requested configuration before activation.

## Implementation Scope

* Implement policy evaluator for initial settings.
* Bind exact Policy Definition revisions.
* Support allow, deny and constrain outcomes needed by Gate A.
* Create safe explanation and provenance.
* Prevent user and project sources from disabling Product Policy.
* Create evaluation receipt payload.

## Acceptance Criteria

* [ ] Remote provider remains disabled during Gate A.
* [ ] Plugins remain disabled.
* [ ] MCP remains disabled.
* [ ] Cloud default remains Local Only.
* [ ] Secret-in-config is denied.
* [ ] Project capability grants are denied.
* [ ] Same inputs produce same decision.
* [ ] Policy failure fails closed and retains prior Effective Snapshot.

## Required Tests

* Every initial Product Policy.
* User bypass attempt.
* Project bypass attempt.
* Unknown policy revision.
* Evaluator exception.
* Determinism.
* Explanation output.

## Trust Evidence

Material policy decisions use Authoritative Domain Decision evidence owned by Configuration or Security according to catalogue.

## Recovery

Current Product Policy is always re-evaluated after restart and restore.

## Evidence Output

* policy evaluator report
* bypass adversarial report
* decision fixtures

## Suggested Labels

`area:configuration`, `area:security`, `type:security`, `week:08`, `milestone:M5`, `priority:p0`

---

# FND-049 — Add Effective Configuration Snapshot

**Epic:** EPIC-FND-08
**Week:** 08
**Milestone:** M5
**Priority:** P0
**Size:** L
**Owner:** Configuration Service
**Specifications:** SPEC-002, SPEC-003, SPEC-008
**ADRs:** ADR-0005, ADR-0026, ADR-0027
**Depends on:** FND-047, FND-048

## Outcome

Configuration Service commits one immutable Effective Snapshot for the Runtime or project scope.

## Implementation Scope

* Define Effective Snapshot schema and database tables.
* Bind Product Defaults, User Profile, Project source, Setting Definitions and Policy Definitions.
* Store requested and effective values separately.
* Store per-key decision state.
* Compute canonical snapshot hash.
* Commit current pointer and outbox atomically.

## Acceptance Criteria

* [ ] Snapshot is immutable.
* [ ] Every key binds definition revision.
* [ ] Every source binds revision or content hash.
* [ ] Every policy binds revision.
* [ ] Requested and effective values are distinct.
* [ ] Current pointer commits atomically.
* [ ] Failed evaluation does not replace current.
* [ ] Snapshot hash is deterministic.

## Required Tests

* First snapshot.
* Changed user profile.
* Changed project source.
* Policy constraint.
* Failure before commit.
* Canonical hash.
* Runtime restart.

## Trust Evidence

Snapshot commit is authoritative Configuration state.

## Recovery

After crash, the last committed snapshot remains current and can be rebuilt from retained sources.

## Evidence Output

* Effective Snapshot schema
* atomic commit report
* canonical examples

## Suggested Labels

`area:configuration`, `type:feature`, `week:08`, `milestone:M5`, `priority:p0`

---

# FND-050 — Add Per-Key Provenance

**Epic:** EPIC-FND-08
**Week:** 08
**Milestone:** M5
**Priority:** P0
**Size:** M
**Owner:** Configuration Service
**Specifications:** SPEC-008, SPEC-010
**ADRs:** ADR-0026
**Depends on:** FND-049

## Outcome

The developer can see exactly where every effective setting came from and why policy changed it.

## Implementation Scope

* Define provenance entry schema.
* Record requested source, revision, merge step, policy decision, effective value and explanation.
* Expose bounded query by snapshot and key.
* Redact sensitive values.
* Create initial UI projection contract.
* Support not-set and defaulted states.

## Acceptance Criteria

* [ ] Every effective key has provenance.
* [ ] Default source is visible.
* [ ] User source is visible.
* [ ] Project source is visible.
* [ ] Denied or constrained request is visible.
* [ ] Sensitive value is masked.
* [ ] Unknown provenance state is not displayed as complete.
* [ ] Provenance binds exact snapshot hash.

## Required Tests

* Default-only provenance.
* User override.
* Project override.
* Policy denial.
* Policy constraint.
* Sensitive masking.
* Missing record integrity test.

## Trust Evidence

Provenance is a derived projection from authoritative configuration records.

## Recovery

Provenance can be rebuilt from the Effective Snapshot and source records.

## Evidence Output

* provenance schema
* UI examples
* sensitive-value review

## Suggested Labels

`area:configuration`, `type:feature`, `week:08`, `milestone:M5`, `priority:p0`

---

# FND-051 — Add Configuration Change Transaction

**Epic:** EPIC-FND-08
**Week:** 08
**Milestone:** M5
**Priority:** P0
**Size:** L
**Owner:** Configuration Service
**Specifications:** SPEC-008, SPEC-010
**ADRs:** ADR-0005, ADR-0026, ADR-0027
**Depends on:** FND-042, FND-048, FND-049, FND-050

## Outcome

A user configuration edit is validated, previewed and committed as one transaction with a new Effective Snapshot.

## Implementation Scope

* Define proposed change contract.
* Validate source scope and type.
* Build preview Effective Snapshot without activation.
* Show requested, effective and policy result.
* Bind approval to proposal hash.
* Commit Profile revision, Effective Snapshot, current pointers and outbox.
* Return activation result.

## Acceptance Criteria

* [ ] Invalid change is rejected before commit.
* [ ] Preview shows policy effect.
* [ ] Changed proposal invalidates approval.
* [ ] Profile and Effective Snapshot commit atomically.
* [ ] Outbox commits atomically.
* [ ] Failure rolls back all changes.
* [ ] No secret can be submitted.
* [ ] Successful change appears through query.

## Required Tests

* Valid change.
* Invalid type.
* Policy denial.
* Stale approval.
* Database failure.
* Crash before commit.
* Crash after commit before delivery.
* Secret attempt.

## Trust Evidence

Creates a Human Decision or configuration request record plus authoritative snapshot commit receipt.

## Recovery

Pending evidence delivery resumes; current configuration is either wholly old or wholly new.

## Evidence Output

* change transaction sequence
* stale-approval report
* atomicity report

## Suggested Labels

`area:configuration`, `type:feature`, `week:08`, `milestone:M5`, `priority:p0`

---

# FND-052 — Add Last-Known-Good Configuration

**Epic:** EPIC-FND-08
**Week:** 08
**Milestone:** M5
**Priority:** P0
**Size:** M
**Owner:** Configuration Service
**Specifications:** SPEC-002, SPEC-008, SPEC-010
**ADRs:** ADR-0026, ADR-0027
**Depends on:** FND-046, FND-049, FND-051

## Outcome

Invalid external project configuration never partially replaces the last valid Effective Snapshot.

## Implementation Scope

* Track latest observed project source and latest valid source separately.
* Retain active Effective Snapshot on parse, schema, merge or policy failure.
* Expose invalid-source diagnostics and stale state.
* Re-evaluate after a new Workspace generation.
* Allow explicit source removal.
* Record reconciliation evidence.

## Acceptance Criteria

* [ ] Invalid JSON leaves prior snapshot active.
* [ ] Duplicate key leaves prior snapshot active.
* [ ] Schema error leaves prior snapshot active.
* [ ] Policy failure leaves prior snapshot active.
* [ ] UI shows source invalid and active snapshot revision.
* [ ] Restored valid file creates a new snapshot.
* [ ] Explicit deletion removes project source according to definition.
* [ ] No invalid requested value leaks into services.

## Required Tests

* Each invalid class.
* Valid-invalid-valid sequence.
* File deletion.
* Runtime restart while invalid.
* Rapid edits.
* Source hash mismatch.

## Trust Evidence

Publishes `configuration.source-invalid` and later a valid snapshot receipt without treating invalid content as authority.

## Recovery

Last committed Effective Snapshot remains usable across restart; invalid observed source remains visible for correction.

## Evidence Output

* last-known-good state machine
* invalid-source test report
* UI state examples

## Suggested Labels

`area:configuration`, `type:feature`, `week:08`, `milestone:M5`, `priority:p0`

---

# FND-053 — Add Trust Centre Overview

**Epic:** EPIC-FND-09
**Week:** 10
**Milestone:** M6
**Priority:** P0
**Size:** L
**Owner:** Desktop and Trust Evidence
**Specifications:** SPEC-008, SPEC-010
**ADRs:** ADR-0002, ADR-0027
**Depends on:** FND-013, FND-025, FND-024

## Outcome

Trust Centre Overview shows Runtime, service, project, configuration, evidence and backup health without raw diagnostic noise.

## Implementation Scope

* Create Trust Centre navigation and Overview view model.
* Show current Runtime boot, service health, open projects, configuration warnings, evidence gaps and Recovery Point status.
* Use typed Trust queries.
* Apply progressive disclosure.
* Show data freshness and completeness.
* Add keyboard and screen-reader behaviour.

## Acceptance Criteria

* [ ] Overview loads without querying raw service databases.
* [ ] Health and authority are distinct.
* [ ] Evidence gaps are visible.
* [ ] Stale data is labelled.
* [ ] No secret or source content is shown.
* [ ] Every card links to a detailed view.
* [ ] Keyboard navigation works.
* [ ] Narrator announces warning and status text.

## Required Tests

* Healthy state.
* Service degraded.
* Evidence gap.
* Configuration invalid.
* Disconnected Runtime.
* Keyboard.
* Narrator.
* High contrast.
* Query latency.

## Trust Evidence

The view is a projection and never authorises or mutates domain state.

## Recovery

After reconnect, stale projections refresh and keep prior data labelled until replacement arrives.

## Evidence Output

* Overview UI report
* accessibility report
* information hierarchy review

## Suggested Labels

`area:desktop`, `area:trust`, `type:feature`, `week:10`, `milestone:M6`, `priority:p0`

---

# FND-054 — Add Trust Centre Project View

**Epic:** EPIC-FND-09
**Week:** 10
**Milestone:** M6
**Priority:** P0
**Size:** L
**Owner:** Desktop and Trust Evidence
**Specifications:** SPEC-008, SPEC-010, SPEC-011
**ADRs:** ADR-0027
**Depends on:** FND-025, FND-030, FND-038, FND-053

## Outcome

The developer can inspect one project's registration, open operation, repository observation and Workspace generations.

## Implementation Scope

* Create project-scoped query.
* Display project identity and safe root class.
* Display open and close timeline.
* Display current and prior Workspace generation receipts.
* Display relationships and completeness.
* Link to Configuration view.
* Provide accessible table alternative to causal graph.

## Acceptance Criteria

* [ ] Only selected project evidence appears.
* [ ] Project authority owner is visible.
* [ ] Workspace generation and hash are visible.
* [ ] Raw source content is absent.
* [ ] Raw absolute root is absent or normalised.
* [ ] Missing owner record is visible.
* [ ] Causal relationships are understandable.
* [ ] Keyboard and Narrator can use the timeline.

## Required Tests

* One project.
* Several projects cross-scope denial.
* Missing receipt.
* Generation change.
* Owner unavailable.
* Accessible table.
* Large timeline performance.

## Trust Evidence

No mutation actions are available in the evidence view.

## Recovery

Historical project evidence remains inspectable even when the root is unavailable.

## Evidence Output

* Project Trust view
* cross-project test
* timeline accessibility report

## Suggested Labels

`area:desktop`, `area:trust`, `type:feature`, `week:10`, `milestone:M6`, `priority:p0`

---

# FND-055 — Add Trust Centre Configuration View

**Epic:** EPIC-FND-09
**Week:** 10
**Milestone:** M6
**Priority:** P0
**Size:** L
**Owner:** Desktop and Configuration
**Specifications:** SPEC-008, SPEC-010
**ADRs:** ADR-0026, ADR-0027
**Depends on:** FND-025, FND-049, FND-050, FND-052, FND-053

## Outcome

The developer can inspect the active Effective Snapshot, source revisions, requested values, policy decisions and invalid-source state.

## Implementation Scope

* Create configuration snapshot query and view model.
* Display snapshot hash and activation time.
* Display per-key requested and effective values.
* Display source and policy provenance.
* Mask sensitive values.
* Display last-known-good and invalid-source warning.
* Link to associated Trust receipts.

## Acceptance Criteria

* [ ] Current snapshot is explicit.
* [ ] Requested and effective values are distinguishable.
* [ ] Default, user and project sources are distinguishable.
* [ ] Policy denials and constraints are explained.
* [ ] Sensitive values are masked.
* [ ] Invalid observed source is visible.
* [ ] Prior snapshot can be inspected.
* [ ] No setting can be changed through the evidence-only view unless routed to Configuration transaction UI.

## Required Tests

* Default.
* User override.
* Project override.
* Policy denial.
* Invalid source.
* Sensitive masking.
* Prior revision.
* Keyboard and Narrator.

## Trust Evidence

The view displays owner records and derived provenance without becoming configuration authority.

## Recovery

After service restart the view resolves the last committed snapshot and marks projection freshness.

## Evidence Output

* Configuration Trust view
* provenance comprehension review
* accessibility report

## Suggested Labels

`area:desktop`, `area:configuration`, `type:feature`, `week:10`, `milestone:M6`, `priority:p0`

---

# FND-056 — Add Evidence Completeness

**Epic:** EPIC-FND-09
**Week:** 09
**Milestone:** M6
**Priority:** P0
**Size:** L
**Owner:** Trust Evidence
**Specifications:** SPEC-008, SPEC-010
**ADRs:** ADR-0027
**Depends on:** FND-016, FND-023, FND-024, FND-025

## Outcome

Every Trust query can state whether the returned evidence is complete for its declared scope.

## Implementation Scope

* Define completeness schema and states.
* Track expected owner sequences.
* Track projection delay and owner freshness.
* Represent gap, owner unavailable, purged, redacted, conflict and unknown.
* Calculate query-scoped completeness.
* Expose explanations.

## Acceptance Criteria

* [ ] Complete is always scoped.
* [ ] Sequence gap changes completeness.
* [ ] Owner unavailable is distinct from no evidence.
* [ ] Projection delay is distinct from owner gap.
* [ ] Purged by policy is represented.
* [ ] Conflict is represented.
* [ ] Unknown never renders as Complete.
* [ ] Query response includes completeness.

## Required Tests

* Complete scope.
* Gap.
* Owner unavailable.
* Projection delay.
* Purged.
* Conflict.
* Redacted.
* Unknown.

## Trust Evidence

Completeness is a derived Trust projection, not a guarantee beyond the declared owners and time range.

## Recovery

After reconciliation, completeness updates without rewriting historical owner evidence.

## Evidence Output

* completeness schema
* state calculation report
* UI wording review

## Suggested Labels

`area:trust`, `type:feature`, `week:09`, `milestone:M6`, `priority:p0`

---

# FND-057 — Add Evidence Owner Reconciliation

**Epic:** EPIC-FND-09
**Week:** 09
**Milestone:** M6
**Priority:** P0
**Size:** L
**Owner:** Trust Evidence and Owner Services
**Specifications:** SPEC-003, SPEC-008
**ADRs:** ADR-0027
**Depends on:** FND-016, FND-024, FND-056

## Outcome

Trust Evidence detects a missing owner sequence and retrieves the exact retained record from the authoritative owner.

## Implementation Scope

* Define owner reconciliation contract.
* Query owner stream head and sequence range.
* Verify returned record identity and hash.
* Ingest missing record idempotently.
* Represent owner record deleted or unavailable.
* Detect conflicting owner hash.
* Expose reconciliation state and receipt.

## Acceptance Criteria

* [ ] Missing one record is detected.
* [ ] Owner returns exact sequence.
* [ ] Returned hash is verified.
* [ ] Record ingests once.
* [ ] Owner unavailable remains visible.
* [ ] Owner-deleted state is represented.
* [ ] Conflicting hash quarantines.
* [ ] Reconciliation cannot ask another project without capability.

## Required Tests

* Missing record repair.
* Several-record range.
* Owner unavailable.
* Owner deleted.
* Hash conflict.
* Duplicate result.
* Cross-project denial.
* Trust restart during reconciliation.

## Trust Evidence

Reconciliation produces a Verified Service Receipt and preserves the original owner authority.

## Recovery

Interrupted reconciliation resumes from durable gap state.

## Evidence Output

* reconciliation contract
* gap-repair demonstration
* hash-conflict report

## Suggested Labels

`area:trust`, `type:recovery`, `week:09`, `milestone:M6`, `priority:p0`

---

# FND-058 — Add Backup Adapter Contract

**Epic:** EPIC-FND-10
**Week:** 11
**Milestone:** M6
**Priority:** P0
**Size:** L
**Owner:** Backup and Owner Services
**Specifications:** SPEC-002, SPEC-003, SPEC-008
**ADRs:** ADR-0005, ADR-0028
**Depends on:** FND-014, FND-015, FND-016, FND-023, FND-028, FND-036, FND-049

## Outcome

Foundation services expose versioned snapshot, validation and restore contracts without surrendering state ownership.

## Implementation Scope

* Define Backup Adapter schema and IPC contract.
* Inventory database, CAS, mutable, rebuildable, secret and prohibited state.
* Define prepare, checkpoint, snapshot, validate and restore operations.
* Define dependencies and schema support.
* Implement adapters for Configuration, Project, Workspace and Trust foundation state.
* Define Backup Epoch skeleton.

## Acceptance Criteria

* [ ] Each foundation owner has one adapter revision.
* [ ] Live database path remains private to owner.
* [ ] Required and rebuildable state are explicit.
* [ ] Secret and prohibited state are explicit.
* [ ] Adapter supports current schema.
* [ ] Unknown state blocks a Complete backup.
* [ ] Prepare can refuse during migration.
* [ ] Restore validator is defined.

## Required Tests

* Adapter schema.
* Missing required state.
* Unknown schema.
* Migration active.
* Owner unavailable.
* Rebuildable exclusion.
* Secret-state denial.

## Trust Evidence

Backup preparation and checkpoint events are owner receipts linked to the Backup Epoch.

## Recovery

Adapter version and schema are recorded so unsupported backups fail safely rather than being guessed.

## Evidence Output

* Backup Adapter schema
* foundation state inventory
* owner dependency graph

## Suggested Labels

`area:backup`, `type:recovery`, `week:11`, `milestone:M6`, `priority:p0`

---

# FND-059 — Add SQLite Online Backup

**Epic:** EPIC-FND-10
**Week:** 11
**Milestone:** M6
**Priority:** P0
**Size:** XL
**Owner:** Persistence and Backup
**Specifications:** SPEC-002, SPEC-003, SPEC-008
**ADRs:** ADR-0005, ADR-0028
**Depends on:** FND-014, FND-015, FND-058

## Outcome

Each foundation owner can create a consistent live SQLite snapshot through the pinned SQLite Online Backup API.

## Implementation Scope

* Complete the SQLite Online Backup architecture spike first.
* Use the exact pinned native SQLite library.
* Create new restrictive staging destination.
* Establish source and destination identity.
* Copy incrementally with bounded page batches.
* Support busy handling, progress and cancellation.
* Flush, reopen and validate destination.
* Do not copy live `.db`, `-wal` or journal files as the normal mechanism.

## Acceptance Criteria

* [ ] Live WAL database snapshot is consistent.
* [ ] Concurrent writes after snapshot establishment do not corrupt destination.
* [ ] Destination is new and restrictive.
* [ ] Cancellation deletes incomplete destination.
* [ ] Disk-full failure is safe.
* [ ] `PRAGMA quick_check` passes.
* [ ] `PRAGMA foreign_key_check` passes.
* [ ] Owner schema validation passes.
* [ ] Raw-copy comparison demonstrates why ordinary copy is prohibited.

## Required Tests

* Empty, small and large database.
* Concurrent writers.
* Busy.
* Cancellation.
* Disk full.
* Source crash.
* Destination I/O failure.
* Wrong WAL raw-copy adversarial case.
* Power-loss staging test.

## Trust Evidence

Owner checkpoint and backup snapshot completion produce authoritative backup receipts.

## Recovery

Incomplete staging is deleted or quarantined; active owner database is never modified.

## Evidence Output

* SQLite Online Backup spike decision
* live backup report
* raw-copy comparison
* performance measurements

## Suggested Labels

`area:persistence`, `area:backup`, `type:recovery`, `week:11`, `milestone:M6`, `priority:p0`, `type:spike`

---

# FND-060 — Add Local Recovery Point View

**Epic:** EPIC-FND-10
**Week:** 11
**Milestone:** M6
**Priority:** P0
**Size:** L
**Owner:** Backup and Desktop
**Specifications:** SPEC-002, SPEC-008, SPEC-010, SPEC-012
**ADRs:** ADR-0009, ADR-0027, ADR-0028
**Depends on:** FND-053, FND-058, FND-059

## Outcome

The developer can create, inspect, structurally verify and test-restore a same-device local Recovery Point.

## Implementation Scope

* Create Recovery Point definition and manifest.
* Coordinate foundation Backup Epoch.
* Create owner snapshots.
* Build complete manifest and commit marker.
* Run cryptographic or hash verification and structural validation.
* Restore into a disposable staged root.
* Add Backup Health and Recovery Point UI.
* Label Same-Device Recovery Only.

## Acceptance Criteria

* [ ] Recovery Point is explicit user or maintenance action.
* [ ] Required owners are complete.
* [ ] Manifest binds product, schemas and checkpoints.
* [ ] Commit marker is written last.
* [ ] Structural verification level is shown.
* [ ] Disposable restore does not touch active root.
* [ ] Same-device limitation is prominent.
* [ ] Failure and cancellation leave prior points intact.
* [ ] Trust Centre shows creation and verification receipts.
* [ ] No cloud or network destination is used.

## Required Tests

* Successful point.
* Required owner unavailable.
* Cancellation.
* Runtime crash during copy.
* Manifest tamper.
* Missing commit marker.
* Disposable restore.
* Active-root unchanged assertion.
* Accessibility.

## Trust Evidence

Creates `backup.recovery-point-created` and `backup.verification-completed` receipts.

## Recovery

The local point may restore foundation state into staging; it is not represented as device-loss protection.

## Evidence Output

* Recovery Point manifest
* structural verification report
* disposable restore report
* Backup Health UI evidence

## Suggested Labels

`area:backup`, `area:desktop`, `type:recovery`, `week:11`, `milestone:M6`, `priority:p0`

---

# 29. Founder Gate A Hardening Tasks

# GATE-A-001 — Run the End-to-End Foundation Demonstration

**Week:** 12
**Priority:** P0
**Size:** L
**Owner:** Foundation Programme
**Depends on:** FND-001 through FND-060
**Specifications:** CHARTER-001, SPEC-001, SPEC-002, SPEC-003, SPEC-008, SPEC-009, SPEC-010, SPEC-011, SPEC-012
**ADRs:** ADR-0001 through ADR-0015 where applicable, ADR-0026, ADR-0027, ADR-0028

## Outcome

The complete first vertical slice is demonstrated from a clean supported Windows 11 environment.

## Demonstration Steps

1. Start from a clean Development-channel data root.
2. Launch Bootstrap.
3. Show Runtime and Desktop process identities.
4. Show authenticated IPC connection.
5. Show Runtime and service health.
6. Select and open a small Git repository.
7. Show verified Project identity.
8. Show repository observation.
9. Show Workspace Snapshot generation and hash.
10. Show Product Defaults.
11. Show User Base Profile.
12. Show project settings.
13. Show Effective Snapshot and per-key provenance.
14. Introduce a valid project-settings change.
15. Show new Workspace and Configuration generations.
16. Introduce invalid JSON.
17. Show invalid source while last-known-good remains active.
18. Restore valid JSON.
19. Show repaired state.
20. Open Trust Centre Overview.
21. Open Project view.
22. Open Configuration view.
23. Close Desktop.
24. Show Runtime remains healthy.
25. Reopen Desktop and reconnect.
26. Terminate Runtime.
27. Show supervised restart and new boot identity.
28. Show durable Project and Configuration state.
29. Create a local Recovery Point.
30. Verify and test-restore it into a disposable root.
31. Show active data root was not modified.
32. Show final Trust Centre timeline and evidence completeness.

## Acceptance Criteria

* [ ] Every step completes without manual database edits.
* [ ] No hidden network connection occurs.
* [ ] No AI runtime starts.
* [ ] No plugin or MCP process starts.
* [ ] No project file is modified.
* [ ] Invalid configuration never becomes effective.
* [ ] Runtime restart loses no committed state.
* [ ] Trust Centre shows owner and authority.
* [ ] Recovery Point is labelled Same-Device.
* [ ] Stable error categories are visible where failures are intentionally injected.
* [ ] Demonstration uses a repeatable script and fixture repository.
* [ ] Build and data-root identity are recorded.

## Evidence Output

* `eng/evidence/milestones/M6/end-to-end-demonstration.md`
* screen recording or automated UI capture where safe
* fixture repository hash
* build identity
* demonstration checklist result

---

# GATE-A-002 — Run Crash and Restart Recovery Suite

**Week:** 12
**Priority:** P0
**Size:** L
**Owner:** Runtime, Persistence and Recovery
**Depends on:** FND-007, FND-012, FND-015, FND-016, FND-017, FND-036, FND-049, FND-059

## Required Scenarios

* Desktop crash;
* Desktop ordinary close;
* Runtime crash;
* Runtime crash loop;
* Project Service crash;
* Workspace Service crash during generation;
* Configuration Service crash before commit;
* Configuration Service crash after commit before outbox delivery;
* Trust Evidence crash during ingestion;
* Backup cancellation;
* Backup worker crash during SQLite copy;
* and process restart during evidence reconciliation.

## Acceptance Criteria

* [ ] No committed owner state is lost.
* [ ] No uncommitted state becomes active.
* [ ] Outbox delivery resumes.
* [ ] Inbox deduplication prevents duplicate effect.
* [ ] Incomplete Workspace generation remains inactive.
* [ ] Incomplete Effective Snapshot remains inactive.
* [ ] Incomplete Recovery Point lacks a commit marker and is ignored.
* [ ] Desktop reconnects or presents an actionable state.
* [ ] Repeated Runtime failure enters Safe Mode.
* [ ] Trust completeness reflects any temporary gap.
* [ ] No automatic database reset occurs.
* [ ] Recovery evidence is retained.

## Evidence Output

* crash matrix
* restart timing
* database integrity results
* outbox and inbox replay results
* Safe Mode report

---

# GATE-A-003 — Run IPC Security Suite

**Week:** 12
**Priority:** P0
**Size:** L
**Owner:** Runtime Security
**Depends on:** FND-008, FND-009, FND-010, FND-019, FND-020

## Required Scenarios

* another Windows user;
* unrelated same-user process;
* stale Bootstrap session;
* captured challenge replay;
* wrong Runtime boot identity;
* malformed protobuf;
* oversized message;
* deadline abuse;
* cancellation abuse;
* rapid connection attempts;
* pipe-name discovery;
* and Runtime restart.

## Acceptance Criteria

* [ ] Unauthorised users are denied by ACL or authentication.
* [ ] Same-user process without session proof is denied.
* [ ] Replay is denied.
* [ ] Session material is absent from logs and files.
* [ ] Runtime remains available after malformed input.
* [ ] Message limits are enforced before excessive allocation.
* [ ] Connection-rate limits or bounded admission protect Runtime.
* [ ] Stable security evidence is created for material denial.
* [ ] Health polling does not flood Trust Evidence.
* [ ] No TCP listener exists.

## Evidence Output

* IPC threat-model closure
* ACL report
* replay report
* malformed-input fuzz report
* no-listener network capture

---

# GATE-A-004 — Run Filesystem Adversarial Suite

**Week:** 12
**Priority:** P0
**Size:** XL
**Owner:** Filesystem, Project and Workspace
**Depends on:** FND-026 through FND-038

## Required Scenarios

* `..` traversal;
* drive-relative path;
* absolute device path;
* UNC path;
* symlink;
* junction;
* mount point;
* reparse substitution;
* alternate data stream;
* reserved device name;
* trailing dot;
* trailing space;
* case-only collision;
* Unicode-normalisation collision;
* file replacement after validation;
* directory replacement during enumeration;
* watcher overflow;
* locked file;
* deleted root;
* and same path on a different volume.

## Acceptance Criteria

* [ ] No escape outside verified Project boundary occurs.
* [ ] Path-string prefix is never the sole containment test.
* [ ] Changed object identity is detected.
* [ ] Reparse policy is applied consistently.
* [ ] Unsupported path class is visible.
* [ ] Workspace generation remains complete or fails explicitly.
* [ ] Watcher loss causes rescan.
* [ ] File contents do not appear in diagnostics.
* [ ] Security denials use safe normalised path categories.
* [ ] No project file is modified.

## Evidence Output

* adversarial fixture catalogue
* containment test report
* race-condition report
* watcher-loss report
* path leakage scan

---

# GATE-A-005 — Run Configuration Adversarial Suite

**Week:** 12
**Priority:** P0
**Size:** L
**Owner:** Configuration and Security
**Depends on:** FND-039 through FND-052

## Required Scenarios

* comments;
* trailing comma;
* duplicate top-level key;
* duplicate nested key;
* escaped equivalent key;
* invalid UTF-8;
* huge string;
* excessive depth;
* remote `$ref`;
* local file `$ref`;
* unknown setting;
* wrong type;
* project attempts to set user-only value;
* project attempts to grant capability;
* secret-like value;
* stale approval;
* changed Workspace generation;
* Product Policy evaluator exception;
* and valid-invalid-valid external edit sequence.

## Acceptance Criteria

* [ ] Strict JSON remains strict.
* [ ] Duplicate keys never use last-key-wins.
* [ ] Remote and file references are denied.
* [ ] Project source cannot grant authority.
* [ ] Secret values are rejected from ordinary configuration.
* [ ] Invalid source does not replace last-known-good state.
* [ ] Policy failure fails closed.
* [ ] Changed proposal invalidates approval.
* [ ] Per-key provenance remains complete.
* [ ] Trust Evidence distinguishes invalid observation from active configuration.

## Evidence Output

* parser adversarial report
* schema-reference denial report
* policy-bypass report
* stale-approval report
* last-known-good report

---

# GATE-A-006 — Run Trust Evidence Forgery and Reconciliation Suite

**Week:** 12
**Priority:** P0
**Size:** L
**Owner:** Trust Evidence
**Depends on:** FND-021 through FND-025, FND-030, FND-038, FND-056, FND-057

## Required Scenarios

* owner impersonation;
* wrong Evidence Type owner;
* authority elevation;
* duplicate matching record;
* duplicate conflicting record;
* sequence gap;
* sequence replay;
* payload-hash substitution;
* record-hash substitution;
* relationship forgery;
* cross-project query;
* owner unavailable;
* owner record deleted;
* projection loss;
* and Trust database rebuild.

## Acceptance Criteria

* [ ] Wrong owner cannot ingest.
* [ ] Model or user assertion cannot become owner authority.
* [ ] Matching duplicate is idempotent.
* [ ] Conflicting duplicate quarantines.
* [ ] Gap is visible.
* [ ] Reconciliation retrieves exact owner record.
* [ ] Hash conflict does not overwrite.
* [ ] Cross-project query is denied.
* [ ] Projection rebuild works.
* [ ] Missing projection never appears as proof that no action occurred.
* [ ] Completeness is scoped and honest.
* [ ] Integrity language avoids tamper-proof claims.

## Evidence Output

* evidence forgery report
* reconciliation demonstration
* projection rebuild report
* authority-label review
* completeness wording review

---

# GATE-A-007 — Establish Performance Baseline

**Week:** 12
**Priority:** P0
**Size:** L
**Owner:** Performance
**Depends on:** FND-001 through FND-060

## Reference Environment

```text
Windows 11
AMD Ryzen 9 5950X
32 GB RAM
RTX 5070 Ti 16 GB
Development channel
```

## Required Measurements

* clean build;
* incremental build;
* Desktop shell visible;
* Runtime ready;
* Desktop reconnect;
* IPC health p50, p95 and p99;
* service registry query;
* SQLite transaction;
* outbox commit;
* evidence ingestion;
* small project open;
* medium project metadata open;
* Workspace inventory;
* file hashing throughput;
* Effective Configuration build;
* Trust query with 10,000 records;
* local Recovery Point consistency barrier;
* SQLite backup throughput;
* disposable restore validation;
* idle CPU;
* working-set memory;
* and disk growth.

## Acceptance Criteria

* [ ] Every measurement includes build, hardware and fixture identity.
* [ ] p95 values are reported where relevant.
* [ ] Cancellation latency is measured for long operations.
* [ ] No benchmark disables security controls.
* [ ] Regressions have thresholds.
* [ ] Results are compared with ROADMAP-001 provisional targets.
* [ ] Failed target is documented rather than hidden.
* [ ] Performance mode is Balanced.
* [ ] Low-resource follow-up environment is identified.

## Evidence Output

* BenchmarkDotNet or equivalent results
* UI launch timings
* IPC latency report
* project fixture description
* memory and disk report
* target decision table

---

# GATE-A-008 — Establish Accessibility Baseline

**Week:** 12
**Priority:** P0
**Size:** L
**Owner:** Desktop and Accessibility
**Depends on:** FND-005, FND-013, FND-027, FND-032, FND-053, FND-054, FND-055, FND-060

## Required Flows

* launch;
* Runtime health;
* open project;
* project list;
* configuration review;
* Trust Centre Overview;
* Project evidence;
* Configuration evidence;
* invalid-source warning;
* Recovery Point creation;
* Recovery Point verification;
* and error handling.

## Acceptance Criteria

* [ ] Every flow is keyboard operable.
* [ ] Focus order is logical.
* [ ] Focus is visible.
* [ ] Narrator announces control name, role, value and state.
* [ ] Warning and health states have text, not colour only.
* [ ] High contrast preserves meaning.
* [ ] Progress and cancellation are announced.
* [ ] Evidence tables are accessible.
* [ ] Causal timeline has a table alternative.
* [ ] Error recovery action is reachable.
* [ ] No timed interaction blocks completion.
* [ ] Avalonia limitations are recorded for the framework decision.

## Evidence Output

* accessibility test matrix
* Narrator review
* keyboard recording or automation
* high-contrast evidence
* Avalonia retain-or-replace input

---

# GATE-A-009 — Update ADR Evidence Matrix

**Week:** 12
**Priority:** P0
**Size:** L
**Owner:** Architecture
**Depends on:** GATE-A-001 through GATE-A-008

## Required ADR Review

At minimum:

* ADR-0001;
* ADR-0002;
* ADR-0003;
* ADR-0004;
* ADR-0005;
* ADR-0006;
* ADR-0008;
* ADR-0009;
* ADR-0010;
* ADR-0011;
* ADR-0012;
* ADR-0026 foundation subset;
* ADR-0027 foundation subset;
* and ADR-0028 local Recovery Point subset.

## Acceptance Criteria

* [ ] Each reviewed ADR links to implementation commit.
* [ ] Each reviewed ADR links to tests.
* [ ] Each reviewed ADR links to performance evidence where applicable.
* [ ] Each reviewed ADR lists remaining limitations.
* [ ] Proposed decisions are marked Retain, Amend, Replace or Evidence Incomplete.
* [ ] Avalonia decision is explicit.
* [ ] Named-pipe gRPC decision is explicit.
* [ ] SQLite Online Backup decision is explicit.
* [ ] Service grouping decision is explicit.
* [ ] No ADR is marked Accepted solely because code exists.
* [ ] Supersession is used where architecture changed.

## Evidence Output

* `eng/evidence/milestones/M6/adr-evidence-matrix.md`
* ADR review notes
* proposed status changes
* architecture follow-up backlog

---

# GATE-A-010 — Founder Gate A Review

**Week:** 12
**Priority:** P0
**Size:** M
**Owner:** Christopher Dyer
**Depends on:** GATE-A-001 through GATE-A-009

## Review Questions

1. Does the Desktop and Runtime separation feel understandable?
2. Does project opening feel fast enough?
3. Is the Trust Centre useful rather than noisy?
4. Is configuration provenance understandable?
5. Are errors actionable?
6. Does recovery behaviour inspire confidence?
7. Should Avalonia be retained?
8. Should the current process topology be retained?
9. Is the first slice acceptable as the base for controlled mutation?
10. Which known limitations are acceptable for the next phase?

## Decision Options

* Accept;
* Accept with Amendments;
* Repeat Phase 6;
* Replace a provisional architectural choice;
* or Stop and Replan.

## Acceptance Criteria

* [ ] Build identity is recorded.
* [ ] Demonstration result is recorded.
* [ ] Evidence failures are listed.
* [ ] Known limitations are listed.
* [ ] Founder decision is explicit.
* [ ] Required amendments have owners and dates.
* [ ] ADR status decisions are recorded.
* [ ] Phase 7 entry is explicitly approved or denied.
* [ ] Review record is committed.

## Evidence Output

* `eng/evidence/milestones/M6/founder-gate-a.md`
* founder decision
* accepted limitations
* Phase 7 entry decision

---

# GATE-A-011 — Prepare Controlled Mutation Backlog

**Week:** 12
**Priority:** P1
**Size:** M
**Owner:** Product and Architecture
**Depends on:** GATE-A-010

## Outcome

Phase 7 begins only with a founder-approved, dependency-ready backlog for Patch Service and Tool Mediation.

## Required Scope

* exact UTF-8 text-file create or replace;
* unified patch validation;
* patch preview;
* approval binding;
* Workspace staged write;
* atomic replacement;
* file identity revalidation;
* patch receipt;
* curated read-only tool templates;
* restricted command worker;
* cancellation;
* bounded output;
* and effect intent.

## Acceptance Criteria

* [ ] Gate A amendments are reflected.
* [ ] No AI-generated patch is introduced before deterministic mutation works.
* [ ] No arbitrary shell is included.
* [ ] Every story has applicable ADR and specification links.
* [ ] Security review points are identified.
* [ ] Recovery and compensation requirements are identified.
* [ ] Founder Gate B scope is explicit.
* [ ] Phase 7 critical path is ready.

---

# 30. Ticket-to-Week Assignment

| Ticket         | Week | Primary outcome                |
| -------------- | ---: | ------------------------------ |
| FND-001        |    1 | Solution baseline              |
| FND-002        |    1 | Central build policy           |
| FND-003        |    1 | Version source                 |
| FND-004        |    2 | Runtime executable             |
| FND-005        |    2 | Desktop executable             |
| FND-006        |    2 | Bootstrap                      |
| FND-007        |    2 | Process supervision            |
| FND-008        |    3 | Runtime Health contract        |
| FND-009        |    3 | Named-pipe transport           |
| FND-010        |    3 | Session authentication         |
| FND-011        |    2 | Service Registry               |
| FND-012        |    2 | Lifecycle state machine        |
| FND-013        |    2 | Runtime Health UI              |
| FND-014        |    4 | SQLite persistence             |
| FND-015        |    4 | Migrations                     |
| FND-016        |    4 | Outbox                         |
| FND-017        |    4 | Inbox                          |
| FND-018        |    4 | Structured logs                |
| FND-019        |    4 | Traces                         |
| FND-020        |    4 | Redaction and canaries         |
| FND-021        |    9 | Evidence Type                  |
| FND-022        |    9 | Evidence Record                |
| FND-023        |    9 | Trust database                 |
| FND-024        |    9 | Trust ingestion                |
| FND-025        |    9 | Trust query                    |
| FND-026        |    5 | Windows path reference         |
| FND-027        |    5 | Folder picker                  |
| FND-028        |    5 | Project database               |
| FND-029        |    5 | Open Project                   |
| FND-030        |    5 | Project receipt                |
| FND-031        |    6 | Repository identity            |
| FND-032        |    6 | Project list UI                |
| FND-033        |    6 | Workspace contract             |
| FND-034        |    6 | File inventory                 |
| FND-035        |    6 | File hashing                   |
| FND-036        |    6 | Workspace generation           |
| FND-037        |    6 | Reconciliation                 |
| FND-038        |    6 | Snapshot receipt               |
| FND-039        |    7 | Setting Definition             |
| FND-040        |    7 | Policy Definition              |
| FND-041        |    7 | Product Defaults               |
| FND-042        |    7 | User Base Profile              |
| FND-043        |    7 | Strict JSON                    |
| FND-044        |    7 | Duplicate keys                 |
| FND-045        |    7 | Schema Registry                |
| FND-046        |    8 | Project settings acquisition   |
| FND-047        |    8 | Setting merge                  |
| FND-048        |    8 | Product Policy                 |
| FND-049        |    8 | Effective Snapshot             |
| FND-050        |    8 | Provenance                     |
| FND-051        |    8 | Change Transaction             |
| FND-052        |    8 | Last-known-good                |
| FND-053        |   10 | Trust Overview                 |
| FND-054        |   10 | Trust Project view             |
| FND-055        |   10 | Trust Configuration view       |
| FND-056        |    9 | Completeness                   |
| FND-057        |    9 | Reconciliation                 |
| FND-058        |   11 | Backup Adapter                 |
| FND-059        |   11 | SQLite Online Backup           |
| FND-060        |   11 | Recovery Point view            |
| GATE-A-001–011 |   12 | Hardening and founder decision |

---

# 31. Specification Traceability Matrix

| Specification | Gate A backlog coverage                                                                                                    |
| ------------- | -------------------------------------------------------------------------------------------------------------------------- |
| CHARTER-001   | Human control, local-first, visibility, reviewability, reversibility, Trust Centre and hardware respect across all tickets |
| SPEC-001      | Foundation product identity, Windows-first, local-first, no AI authority, safe automation and evidence gates               |
| SPEC-002      | FND-004, FND-006–FND-019, FND-039–FND-060                                                                                  |
| SPEC-003      | Service ownership and contract rules across FND-001, FND-004, FND-008–FND-030, FND-033, FND-039–FND-059                    |
| SPEC-004      | Gate A exclusion: AI Router is not started; boundaries are reserved                                                        |
| SPEC-005      | Gate A exclusion: no Project Memory or semantic index                                                                      |
| SPEC-006      | Gate A exclusion: no workflow or agent execution                                                                           |
| SPEC-007      | Gate A exclusion: plugins remain disabled                                                                                  |
| SPEC-008      | IPC security, policy, Trust Evidence, redaction, path security, backup evidence and all Gate A adversarial tests           |
| SPEC-009      | FND-026–FND-038, project configuration acquisition and filesystem adversarial suite                                        |
| SPEC-010      | FND-005, FND-013, FND-027, FND-032, FND-053–FND-055, FND-060 and accessibility gate                                        |
| SPEC-011      | FND-028–FND-038 and project identity/repository observation                                                                |
| SPEC-012      | Week outcomes, milestone progression and G0–G6 evidence gates                                                              |
| ROADMAP-001   | Exact first sixty tickets, weekly order, milestones M0–M6 and Founder Gate A                                               |

---

# 32. ADR Traceability Matrix

| ADR               | Primary tickets                                                                          |
| ----------------- | ---------------------------------------------------------------------------------------- |
| ADR-0001          | FND-001–FND-005                                                                          |
| ADR-0002          | FND-005, FND-013, FND-027, FND-053 and Gate A accessibility decision                     |
| ADR-0003          | FND-004, FND-006, FND-007, FND-011, FND-012                                              |
| ADR-0004          | FND-008–FND-010, FND-019, service contracts                                              |
| ADR-0005          | FND-014–FND-017, FND-023, FND-028, FND-036, FND-042, FND-049, FND-051, FND-058, FND-059  |
| ADR-0006          | FND-018–FND-020, FND-053 and performance/security gates                                  |
| ADR-0007          | FND-010, FND-014, FND-020 and no-secret configuration rules                              |
| ADR-0008          | All test and evidence tasks                                                              |
| ADR-0009          | FND-026–FND-038, FND-043, FND-046, FND-049, FND-058–FND-060                              |
| ADR-0010          | FND-001, FND-002 and architecture conformance                                            |
| ADR-0011          | FND-001, FND-002 and CI tasks                                                            |
| ADR-0012          | FND-003                                                                                  |
| ADR-0013          | FND-006 and Development packaging follow-up                                              |
| ADR-0014          | Not implemented by Gate A; build identity and future signing boundary reserved           |
| ADR-0015          | FND-058–FND-060 pre-update recovery foundation                                           |
| ADR-0016–ADR-0025 | Explicitly excluded from Gate A execution except contract placeholders where unavoidable |
| ADR-0026          | FND-039–FND-055                                                                          |
| ADR-0027          | FND-016, FND-018–FND-025, FND-030, FND-038, FND-048–FND-060 and Gate A Trust tests       |
| ADR-0028          | FND-015, FND-058–FND-060 and Gate A recovery suite                                       |

---

# 33. Suggested GitHub Milestones

## M0 — Repository Builds

Tickets:

* FND-001;
* FND-002;
* FND-003;
* Week 1 CI, documentation and package-lock tasks.

---

## M1 — Runtime Lives

Tickets:

* FND-004;
* FND-005;
* FND-006;
* FND-007;
* FND-011;
* FND-012;
* FND-013.

---

## M2 — Services Communicate

Tickets:

* FND-008;
* FND-009;
* FND-010.

---

## M3 — State Persists

Tickets:

* FND-014 through FND-025.

---

## M4 — Project Opens Safely

Tickets:

* FND-026 through FND-038.

---

## M5 — Configuration Is Explainable

Tickets:

* FND-039 through FND-052.

---

## M6 — Foundation Is Visible

Tickets:

* FND-053 through FND-060;
* GATE-A-001 through GATE-A-009.

---

## Founder Gate A

Tickets:

* GATE-A-010;
* GATE-A-011.

---

# 34. Suggested Issue Creation Order

Create issues in this order:

1. FND-001–FND-003.
2. FND-004–FND-007.
3. FND-011–FND-013.
4. FND-008–FND-010.
5. FND-014–FND-020.
6. FND-026–FND-030.
7. FND-031–FND-038.
8. FND-039–FND-045.
9. FND-046–FND-052.
10. FND-021–FND-025.
11. FND-056–FND-057.
12. FND-053–FND-055.
13. FND-058–FND-060.
14. GATE-A-001–GATE-A-011.

The numeric ticket IDs preserve ROADMAP-001's first-sixty catalogue.

The creation order above reflects actual weekly execution.

---

# 35. First-Day Ready Queue

## Ready Now

* FND-001 — Create Solution Baseline.

## Ready After FND-001

* FND-002 — Add Central Build Policy.

## Ready After FND-002

* FND-003 — Add Version Source.

## First-Day Support Tasks

* confirm canonical file names in `specs`;
* create repository directories;
* confirm `adr` and `specs` remain separate;
* add `.editorconfig`;
* add `.gitattributes`;
* add `.gitignore`;
* create `build.ps1`;
* create initial CI workflow;
* create `eng/evidence/milestones/M0`;
* and record the pinned SDK.

---

# 36. Recommended First Commit Sequence

```text
1. Create Opure solution and repository foundation
2. Add central build package and analysis policy
3. Add version source and deterministic build metadata
4. Add Runtime Desktop and Bootstrap skeletons
5. Add architecture and test foundations
6. Add CI restore build test and verification
```

Every commit should:

* build;
* run relevant tests;
* avoid unrelated formatting;
* and leave `git status` clean.

---

# 37. Branch Naming

Suggested:

```text
foundation/fnd-001-solution-baseline
foundation/fnd-009-named-pipe-transport
foundation/fnd-026-windows-path-reference
foundation/fnd-049-effective-configuration
foundation/fnd-059-sqlite-online-backup
```

Use short-lived branches.

---

# 38. Pull Request Template Fields

```text
Ticket
Outcome
Specifications
ADRs
User-visible behaviour
State ownership
Security impact
Trust Evidence
Recovery impact
Tests
Performance
Accessibility
Evidence paths
Known limitations
```

---

# 39. Architecture Conformance Rules by Gate A

Automated tests should reject:

* Desktop referencing SQLite implementation;
* Desktop reading project files directly;
* Project Service reading Workspace private tables;
* Workspace Service writing Project database;
* Configuration Service opening project files directly;
* Trust Centre mutating owner state;
* Trust Evidence assigning itself owner authority;
* service code using raw path-prefix containment;
* project JSON defining a capability or permission;
* ordinary configuration storing a secret;
* and Backup Service opening live owner databases directly.

---

# 40. Gate A Data Inventory

Expected durable data by Week 12:

| Data                                      | Owner             |
| ----------------------------------------- | ----------------- |
| Runtime service registry state            | Runtime           |
| User Base Profile and revisions           | Configuration     |
| Setting and Policy catalogue references   | Configuration     |
| Effective Configuration Snapshots         | Configuration     |
| Project identity and lifecycle            | Project Service   |
| Workspace generations and file metadata   | Workspace Service |
| Trust Evidence records and projections    | Trust Evidence    |
| Backup metadata and local Recovery Points | Backup Service    |

Expected rebuildable data:

* Desktop projections;
* service health projections;
* Trust Centre view models;
* temporary Workspace scan state;
* compiled local schemas;
* and benchmark outputs outside retained evidence.

Expected prohibited data:

* secrets in configuration;
* project source in logs;
* project source in Trust receipts;
* process environment;
* session authentication material;
* arbitrary command output;
* AI prompts;
* model output;
* plugin data;
* and MCP data.

---

# 41. Gate A Trust Evidence Catalogue

Required types:

```text
runtime.started
runtime.stopped
runtime.recovered
runtime.safe-mode-entered
service.state-changed
ipc.session-established
ipc.session-denied
project.registered
project.opened
project.closed
workspace.snapshot-created
workspace.snapshot-invalidated
configuration.profile-committed
configuration.snapshot-committed
configuration.source-invalid
configuration.policy-denied
trust.owner-gap-detected
trust.owner-gap-reconciled
trust.integrity-conflict
backup.epoch-started
backup.owner-checkpoint-created
backup.recovery-point-created
backup.verification-completed
backup.restore-test-completed
security.path-denied
security.secret-redacted
```

The exact catalogue remains versioned and subject to ADR-0027 review.

---

# 42. Gate A Configuration Catalogue

Initial settings:

```text
desktop.appearance
desktop.show-advanced
runtime.safe-mode
runtime.restart-budget
workspace.maximum-file-size
workspace.include-hidden-files
workspace.network-path-policy
logging.minimum-level
logging.retention
trust.retention
backup.local-recovery.enabled
backup.local-recovery.retention
project.cloud.policy
```

Initial Product Policies:

```text
policy.product.no-secrets-in-configuration
policy.product.no-project-capability-grants
policy.product.no-remote-schema-references
policy.product.local-cloud-default
policy.product.plugins-disabled
policy.product.mcp-disabled
policy.product.remote-provider-disabled
policy.product.no-automatic-external-side-effects
```

Do not expand the catalogue without a Gate A need.

---

# 43. Gate A Service Catalogue

| Service                | Process placement       | Database                                         | Required by |
| ---------------------- | ----------------------- | ------------------------------------------------ | ----------- |
| Runtime Registry       | Runtime                 | `runtime.db` or initial service-owned equivalent | M1          |
| Project Service        | Runtime logical service | `projects.db`                                    | M4          |
| Workspace Service      | Runtime logical service | `workspace.db`                                   | M4          |
| Configuration Service  | Runtime logical service | `configuration.db`                               | M5          |
| Trust Evidence Service | Runtime logical service | `trust.db`                                       | M3–M6       |
| Backup Service         | Runtime logical service | `backup.db`                                      | M6          |

Logical services may share the Runtime process initially.

They must retain:

* distinct ownership;
* distinct migrations;
* distinct contracts;
* and distinct Backup Adapters.

---

# 44. Gate A IPC Catalogue

```text
RuntimeHealth
ServiceRegistry
OpenProject
ListProjects
CreateWorkspaceSnapshot
GetWorkspaceSnapshot
GetEffectiveConfiguration
SubmitConfigurationChange
QueryTrustEvidence
CreateRecoveryPoint
VerifyRecoveryPoint
TestRestoreRecoveryPoint
```

Every operation requires:

* typed request and response;
* contract revision;
* timeout;
* cancellation;
* maximum size;
* error categories;
* authentication;
* and trace propagation.

---

# 45. Gate A Evidence Directory

Suggested:

```text
eng/
└── evidence/
    └── milestones/
        ├── M0/
        ├── M1/
        ├── M2/
        ├── M3/
        ├── M4/
        ├── M5/
        └── M6/
```

Safe evidence may include:

* Markdown summaries;
* test-result XML;
* benchmark JSON;
* dependency inventories;
* schema hashes;
* screenshots without sensitive project content;
* and fixture hashes.

Do not commit:

* session secrets;
* user paths;
* project source;
* raw support bundles;
* database files with personal data;
* or recovery keys.

---

# 46. Weekly Review Template

```text
Week
Target outcome
Completed tickets
Partially completed tickets
Blocked tickets
Demonstrated behaviour
Tests passed
Tests failed
Security findings
Recovery findings
Accessibility findings
Performance measurements
ADR evidence
Founder decisions
Next week's critical path
```

---

# 47. Blocker Escalation

A blocker becomes architecture-level when it affects:

* process topology;
* local IPC;
* state ownership;
* filesystem containment;
* configuration authority;
* Trust Evidence authority;
* backup correctness;
* or a Product Invariant.

Architecture blockers require:

* a time-boxed spike;
* options;
* evidence;
* ADR impact;
* and founder decision.

---

# 48. Scope Protection

Do not add during Weeks 1–12:

* chat completion;
* local model download;
* provider credentials;
* plugin package support;
* MCP transport;
* workflow designer;
* Git writes;
* project file writes;
* shell execution;
* dependency installation;
* remote schema fetch;
* telemetry export;
* support upload;
* or cloud backup.

A feature request in these areas should be labelled:

```text
post-gate-a
```

and placed in the later programme backlog.

---

# 49. Definition of Gate A Ready

Gate A is Ready for founder review when:

* [ ] FND-001 through FND-060 are Done or explicitly waived with founder-visible impact.
* [ ] GATE-A-001 through GATE-A-009 are complete.
* [ ] No critical security finding remains.
* [ ] No critical data-loss finding remains.
* [ ] No secret leakage remains.
* [ ] No unexplained evidence gap remains.
* [ ] No project boundary escape remains.
* [ ] Recovery Point restore has passed.
* [ ] Accessibility baseline has passed or limitations are explicit.
* [ ] Performance baseline exists.
* [ ] ADR evidence matrix exists.
* [ ] Demonstration build is reproducible.
* [ ] Known limitations are written in plain British English.

---

# 50. Definition of Gate A Accepted

Gate A is Accepted when Christopher records:

* the reviewed build;
* the reviewed demonstration;
* accepted limitations;
* required amendments;
* Avalonia decision;
* process topology decision;
* Trust Centre decision;
* recovery confidence;
* and permission to start controlled mutation.

---

# 51. Specifications Folder Inventory Assessment

The supplied `specs` set contains:

```text
CHARTER-001
SPEC-001
SPEC-002
SPEC-003
SPEC-004
SPEC-005
SPEC-006
SPEC-007
SPEC-008
SPEC-009
SPEC-010
SPEC-011
SPEC-012
ROADMAP-001
```

This is sufficient to govern BACKLOG-001.

The set has a clear progression:

```text
Charter
→ product principles
→ Runtime
→ services
→ AI
→ memory
→ workflows
→ plugins
→ trust and security
→ workspace and patches
→ desktop
→ projects and builds
→ delivery roadmap
→ implementation roadmap
→ first twelve-week backlog
```

---

## 51.1 Recommended Canonical Filenames

```text
CHARTER-001.md
SPEC-001.md
SPEC-002.md
SPEC-003.md
SPEC-004.md
SPEC-005.md
SPEC-006.md
SPEC-007.md
SPEC-008.md
SPEC-009.md
SPEC-010.md
SPEC-011.md
SPEC-012.md
ROADMAP-001-foundation-implementation-sequence.md
BACKLOG-001-foundation-first-12-weeks.md
```

---

## 51.2 Recommended Future Specs Index

After committing BACKLOG-001, the next documentation-maintenance artefact should be:

```text
C:\Opure\specs\README.md
```

It should contain:

* document order;
* status;
* title;
* dependencies;
* canonical filename;
* approval state;
* related ADR range;
* and supersession.

This is housekeeping, not a blocker for implementation.

---

# 52. Immediate Repository Actions

1. Copy BACKLOG-001 to `C:\Opure\specs`.
2. Confirm canonical filenames do not include `(2)`.
3. Run a duplicate-file check in `C:\Opure\specs`.
4. Commit BACKLOG-001 separately.
5. Create M0 through M6 milestones in the issue tracker.
6. Create the suggested labels.
7. Create FND-001.
8. Begin only after FND-001 is Ready.
9. Keep FND-059 as an explicit spike plus implementation ticket because its size is XL.
10. Do not create AI, plugin, MCP or workflow implementation issues in the active milestone.

---

# 53. Suggested Commit

```powershell
git add specs/BACKLOG-001-foundation-first-12-weeks.md
git commit -m "Add first twelve-week foundation backlog"
git status
```

---

# 54. Review Record

| Date         | Reviewer           | Decision | Notes                                                                        |
| ------------ | ------------------ | -------- | ---------------------------------------------------------------------------- |
| 18 July 2026 | Architecture draft | Proposed | First sixty tickets, Gate A hardening and specification traceability defined |

---

# 55. Approval

## Founder and Product Approval

* **Name:** Christopher Dyer
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Weekly outcomes, first sixty tickets and Founder Gate A require approval

## Architecture Approval

* **Name or role:** Foundation Architecture Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Dependencies, service boundaries and evidence tasks require review

## Engineering-System Approval

* **Name or role:** Build and CI Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Week 1 baseline and issue conventions require review

## Security Approval

* **Name or role:** Security Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** IPC, filesystem, configuration, evidence and recovery suites require review

## Recovery Approval

* **Name or role:** Recovery Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Recovery Point and staged restore evidence require review

## Accessibility Approval

* **Name or role:** Desktop and Accessibility Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Gate A UI flows and Avalonia evidence require review

---

# 56. Supersession

This backlog is superseded only when a later backlog:

* names BACKLOG-001 explicitly;
* explains why the first twelve-week order changed;
* identifies affected ROADMAP-001 work packages;
* identifies affected specifications and ADRs;
* preserves ticket history or maps replacement tickets;
* records Gate A impact;
* and receives founder approval.

---

# 57. Change History

| Version | Date         | Author        | Summary                                                            |
| ------- | ------------ | ------------- | ------------------------------------------------------------------ |
| 0.1     | 18 July 2026 | Founder Draft | Initial first twelve-week, sixty-ticket and Founder Gate A backlog |

---

# 58. Final Backlog Statement

> **Opure's first implementation backlog will spend twelve evidence-gated weeks building and proving the trustworthy local control plane before any AI, plugin, MCP or autonomous workflow capability is allowed onto the critical path: the work begins with one reproducible repository and build system, then a supervised Runtime and honest Desktop shell, authenticated named-pipe contracts, service-owned SQLite persistence, transactional outboxes, safe observability, a handle-verified Windows project boundary, immutable Workspace generations, typed settings and non-bypassable Product Policy, an explainable Effective Configuration Snapshot, authoritative Trust Evidence and a usable Trust Centre, followed by a structurally verified same-device Recovery Point and a founder-reviewed crash, security, accessibility, performance and recovery demonstration; every ticket names its owner, specification, ADR, dependencies, tests, evidence and recovery behaviour so implementation cannot hide architectural decisions inside code, and Founder Gate A must explicitly confirm that the system is understandable, responsive, secure and recoverable before Opure proceeds to controlled project mutation.**

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**