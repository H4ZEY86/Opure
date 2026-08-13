# GATE-A-011 Controlled Mutation Backlog

Status: Dependency-ready after Founder Gate A Accept with Amendments

Authority: `eng/evidence/milestones/M6/founder-gate-a.md`

## Governing sequence

1. Prove deterministic single-file UTF-8 mutation.
2. Prove unified patch validation against exact source identities.
3. Prove review, approval, staged execution, verification and recovery.
4. Only then introduce curated read-only command templates.
5. Pass Founder Gate B before any AI-generated patch or local intelligence.

Timeout and cancellation, bounded output, effect intent and authoritative receipts
are mandatory for every mediated command.

There is no arbitrary shell, shell string, AI inference, agent loop, plugin, MCP
server, connector, dependency installation, Git write, network listener or direct
Desktop write in this backlog.

No AI-generated patch is introduced before deterministic mutation works and
Founder Gate B passes.

## Gate A amendment carry-over

| Amendment | Phase 7 treatment | Blocking point |
| --- | --- | --- |
| ADR-0011 local-versus-hosted verification amendment | CM-001 defines `build.ps1` policy as authoritative; no hosted workflow is assumed. | Founder Gate B |
| Desktop/Runtime startup target misses | CM-016 reruns GATE-A-007 and records unchanged or improved results. | Founder Gate B |
| Packaged Narrator listening review | Retained as a Gate D installer acceptance item; Phase 7 UI keeps UI Automation metadata. | Gate D |
| Four-core/8 GB Windows follow-up | CM-016 runs mutation and command cases on the recorded low-resource class. | Founder Gate B |
| Same-device recovery wording | CM-007 and CM-009 preserve the warning; patch snapshots are compensation, not disaster recovery. | Continuous invariant |

## Critical path

`CM-001 → CM-002 → CM-003 → CM-004 → CM-005 → CM-006 → CM-007 → CM-008 → CM-009 → CM-010 → CM-011 → CM-012 → CM-013 → CM-014 → CM-015 → CM-016`

Command-worker implementation cannot begin before CM-011 proves deterministic
file mutation and unified-patch safety. Parallel test design is permitted; no
execution authority may bypass the dependency chain.

## CM-001 — Version Patch Contracts and Exact UTF-8 Operation

- Outcome: versioned framework-neutral Patch Service contracts represent create or replace of exactly one UTF-8 text file inside one open project, with base Workspace generation, path reference, expected existence, source hash, line-ending intent and exact content hash.
- Depends on: GATE-A-010, FND-033 through FND-038.
- ADR links: ADR-0001, ADR-0008, ADR-0009, ADR-0010, ADR-0027.
- Specification links: SPEC-003, SPEC-008, SPEC-009, SPEC-012, ROADMAP-001 §19.4.
- Security review: reject NUL, invalid UTF-8, BOM ambiguity, absolute paths, traversal, alternate streams and undeclared encodings; content never enters ordinary logs.
- Recovery and compensation: contract is proposal-only and has no write authority; cancellation leaves no state.
- Acceptance: schema/version tests; canonical hash tests; create/replace distinction; unsupported binary denial; architecture boundary test.

## CM-002 — Add Patch State Store and Transition Machine

- Outcome: service-owned SQLite state records Draft through Cancelled transitions, immutable proposal identity and idempotent command identities.
- Depends on: CM-001, FND-014 through FND-017.
- ADR links: ADR-0005, ADR-0008, ADR-0010, ADR-0025, ADR-0027.
- Specification links: SPEC-003, SPEC-008, SPEC-009 §39 and §46, SPEC-012.
- Security review: no raw secret-bearing file content in the state database, outbox or errors; illegal/stale transitions fail closed.
- Recovery and compensation: incomplete transitions resume from journalled state without blind replay; state reconciliation is idempotent.
- Acceptance: transition matrix, restart, duplicate command, crash-point, bounded-conflict and migration tests.

## CM-003 — Validate Workspace Boundary and Source Preconditions

- Outcome: validation resolves the path through Workspace authority and binds project identity, Workspace generation, canonical path, file identity, expected source length/hash and observed repository state.
- Depends on: CM-001, CM-002, FND-026 through FND-037.
- ADR links: ADR-0008, ADR-0009, ADR-0027.
- Specification links: SPEC-008, SPEC-009 §14–§22 and §59, SPEC-011.
- Security review: deny symlink/junction/reparse escape, hard-link substitution, case/Unicode collision, device paths, reserved names and time-of-check/time-of-use mismatch.
- Recovery and compensation: validation is read-only; source drift invalidates the proposal and any later approval.
- Acceptance: adversarial NTFS matrix plus changed-source, changed-generation and cross-project denial tests.

## CM-004 — Produce Exact Preview and Bind Developer Approval

- Outcome: deterministic preview shows path, create/replace intent, exact before/after hashes, line-ending/encoding changes, bounded diff and effect intent; approval binds the complete validated proposal and preview digest.
- Depends on: CM-003, FND-051.
- ADR links: ADR-0002, ADR-0008, ADR-0009, ADR-0026, ADR-0027.
- Specification links: SPEC-008 §21–§23, SPEC-009 §30–§33 and §55, SPEC-010.
- Security review: hidden/bidi controls, secret findings, truncation and omitted diff regions are explicit; approval cannot bind a partial or stale preview.
- Recovery and compensation: rejection/expiry changes no file; any proposal/source change revokes approval.
- Acceptance: exact-preview golden tests, accessibility tests, stale/tampered approval denial and no-timed-interaction test.

## CM-005 — Stage Same-Volume Write and Atomically Replace

- Outcome: Workspace Service owns restrictive staging under the same volume and performs one atomic create-or-replace only after a valid apply command.
- Depends on: CM-004.
- ADR links: ADR-0005, ADR-0008, ADR-0009, ADR-0027, ADR-0028.
- Specification links: SPEC-003, SPEC-008, SPEC-009 §35–§38 and §59.
- Security review: random managed staging names, restrictive ACL, no current-directory resolution, no overwrite outside the capability root, no partial visible write.
- Recovery and compensation: retain pre-apply snapshot and staged/result hashes until the operation reaches a terminal verified state.
- Acceptance: create, replace, locked/read-only file, disk-full simulation, cancellation-before-commit and atomic-visibility tests.

## CM-006 — Revalidate File Identity Immediately Before Commit

- Outcome: Patch Service rechecks project, Workspace generation, canonical path, file identity, reparse state, length and source hash immediately before atomic replacement and verifies resulting identity/hash afterwards.
- Depends on: CM-005.
- ADR links: ADR-0008, ADR-0009, ADR-0027.
- Specification links: SPEC-008, SPEC-009 §19, §36, §40 and §59.
- Security review: adversarial substitution between preview, approval, staging and commit must fail closed without touching the attacker-controlled target.
- Recovery and compensation: a pre-commit mismatch deletes/quarantines staging and preserves the active file; post-commit mismatch enters Recovery Required.
- Acceptance: reparse substitution, hard-link swap, same-length hash change, file-ID change and postcondition-failure tests.

## CM-007 — Journal, Cancel, Reverse and Recover Patch Application

- Outcome: durable apply journal distinguishes not-started, staged, committed, verified, reversed, compensated and recovery-required states; cancellation is honoured at safe boundaries.
- Depends on: CM-006, FND-058 through FND-060.
- ADR links: ADR-0005, ADR-0008, ADR-0009, ADR-0025, ADR-0027, ADR-0028.
- Specification links: SPEC-008 §31, SPEC-009 §41–§48, SPEC-012.
- Security review: no blind replay, no reversal over developer changes, bounded snapshot retention, same-device warning remains visible.
- Recovery and compensation: restore only after current-state validation; otherwise preserve both states and require developer action with an exact recovery report.
- Acceptance: crash at every journal boundary, cancel, developer-edit-after-crash, reverse conflict, compensation and cleanup-retention tests.

## CM-008 — Emit Authoritative Patch Receipts

- Outcome: owner receipts cover proposal, validation, approval, apply start/outcome, verification, cancellation, reversal and recovery without duplicating file content.
- Depends on: CM-007, FND-021 through FND-025, FND-056 and FND-057.
- ADR links: ADR-0005, ADR-0006, ADR-0008, ADR-0027.
- Specification links: SPEC-003, SPEC-008, SPEC-009 §50–§51, SPEC-012.
- Security review: receipt hashes and safe path references are authoritative; logs are not; secrets and full diffs are excluded.
- Recovery and compensation: transactional outbox/reconciliation repairs missing projections without rewriting owner history.
- Acceptance: forgery, duplicate, gap, owner-unavailable, restart and Trust projection tests.

## CM-009 — Add Accessible Patch Review and Recovery UI

- Outcome: Desktop projects patch state, exact preview, validation, approval, progress/cancellation, verification, reverse/recovery options and Trust links without direct file/database access.
- Depends on: CM-004, CM-007, CM-008.
- ADR links: ADR-0002, ADR-0008, ADR-0009, ADR-0027.
- Specification links: SPEC-008, SPEC-009 §55, SPEC-010.
- Security review: no hidden apply gesture, colour-only state, secret-bearing automation label or authority in the view model.
- Recovery and compensation: retry/recovery actions remain keyboard reachable; closing cancels pending UI requests but not an already committed domain transaction.
- Acceptance: keyboard, Narrator/UIA, high contrast, stale preview, invalid approval, progress and error-recovery tests.

## CM-010 — Parse and Validate One Unified Patch

- Outcome: strict parser accepts a bounded documented unified-diff subset and resolves each hunk to exact source hashes and Workspace path references.
- Depends on: CM-009.
- ADR links: ADR-0008, ADR-0009, ADR-0027.
- Specification links: SPEC-008, SPEC-009 §24–§29 and §49.
- Security review: reject traversal, duplicate/case-colliding targets, malformed headers, oversized hunks, binary payloads, ambiguous encodings and patch archive expansion.
- Recovery and compensation: parse/validation is proposal-only; any failed hunk blocks the whole patch before staging.
- Acceptance: parser fuzzing, malformed corpus, path collision, line-ending, context mismatch and exact-preview tests.

## CM-011 — Apply Validated Unified Patch as a Workspace Transaction

- Outcome: ordered multi-file transaction stages every result, revalidates every source, journals commit order and reports atomicity honestly; no target is touched until all validation/staging succeeds.
- Depends on: CM-010.
- ADR links: ADR-0005, ADR-0008, ADR-0009, ADR-0025, ADR-0027, ADR-0028.
- Specification links: SPEC-003, SPEC-008, SPEC-009 §34–§48.
- Security review: capability scope is the exact target set; added/removed/renamed targets cannot expand after approval.
- Recovery and compensation: compensate committed subset in reverse order only after identity validation; otherwise enter explicit partial-recovery state.
- Acceptance: two-file success, Nth-file failure, crash at each commit, concurrent developer edit, compensation conflict and receipt ordering tests.

## CM-012 — Define Typed Read-Only Tool Templates and Effect Intent

- Outcome: versioned catalogue represents executable identity, literal argument array, working-directory capability, environment allowlist, timeout, input/output policy, resource budget and declared effect class.
- Depends on: CM-011.
- ADR links: ADR-0003, ADR-0006, ADR-0008, ADR-0009, ADR-0027.
- Specification links: SPEC-003, SPEC-008 §18–§23, ROADMAP-001 §19.6–§19.7.
- Security review: initial allowlist is `git status`, `git diff --stat`, `dotnet --info` and explicitly configured read-only verification templates; no shell executable, command string, interpolation or inherited environment.
- Recovery and compensation: catalogue validation is read-only; invalid/unknown templates are denied.
- Acceptance: schema, signature/hash, argument injection, working-directory escape, environment and effect-class mismatch tests.

## CM-013 — Add Restricted Command Worker

- Outcome: supervised worker resolves an exact verified executable, starts without a shell, receives only capability-bound directory/environment, and is contained by Windows Job Object and resource limits.
- Depends on: CM-012.
- ADR links: ADR-0003, ADR-0004, ADR-0006, ADR-0008, ADR-0009, ADR-0027.
- Specification links: SPEC-002, SPEC-003, SPEC-008 §18–§20, ROADMAP-001 §19.
- Security review: deny PATH/current-directory resolution, child escape, network-capable templates, interactive stdin, response-file smuggling and unauthorised descendants.
- Recovery and compensation: timeout/cancel terminates the bounded process tree; orphan scan and cleanup are mandatory.
- Acceptance: identity swap, child escape, cancel, timeout, crash, orphan, zero-network-listener and minimal-environment tests.

## CM-014 — Bound and Redact Command Output

- Outcome: stdout/stderr are asynchronously drained into bounded local buffers with truncation metadata, cancellation-safe completion and redaction before presentation.
- Depends on: CM-013, FND-018 through FND-020.
- ADR links: ADR-0006, ADR-0008, ADR-0027.
- Specification links: SPEC-008 §24–§29, ROADMAP-001 §19.7–§19.8.
- Security review: output is excluded from ordinary logs by default; secret canaries, terminal-control sequences, encoding faults and output floods are contained.
- Recovery and compensation: cancellation preserves safe metadata and exit state; buffers are disposed under retention policy.
- Acceptance: stdout/stderr deadlock, flood, invalid UTF-8, canary, truncation, cancellation latency and memory-bound tests.

## CM-015 — Bind Command Approval and Emit Exit Receipt

- Outcome: execution approval binds template revision/hash, executable identity, literal arguments, capability-bound directory, environment, budget and effect intent; authoritative receipt records start/exit/cancel/timeout and verification result.
- Depends on: CM-014, CM-008.
- ADR links: ADR-0004, ADR-0006, ADR-0008, ADR-0027.
- Specification links: SPEC-003, SPEC-008 §21–§23, SPEC-012, ROADMAP-001 §19.8.
- Security review: changed catalogue, executable, arguments, directory, environment or effect invalidates approval; output content is referenced, not copied into Trust Evidence.
- Recovery and compensation: read-only initial commands need no domain compensation; failed verification is explicit and cannot be relabelled successful.
- Acceptance: stale approval, template substitution, effect mismatch, timeout, cancel, non-zero exit, receipt forgery and reconciliation tests.

## CM-016 — Controlled Mutation Adversarial Demonstration and Founder Gate B

- Outcome: repeatable Development-channel demonstration covers exact file create/replace, unified patch, review/approval, source drift, crash/recovery, read-only tool execution, cancellation, bounded output, Trust inspection and all negative capability assertions.
- Depends on: CM-015 and every Gate A amendment due before Founder Gate B.
- ADR links: ADR-0002, ADR-0003, ADR-0004, ADR-0005, ADR-0006, ADR-0008, ADR-0009, ADR-0011, ADR-0025, ADR-0027, ADR-0028.
- Specification links: SPEC-001, SPEC-003, SPEC-008, SPEC-009, SPEC-010, SPEC-012, ROADMAP-001 §20.
- Security review: prove no AI, agent, plugin, MCP, connector, arbitrary shell, direct Desktop write, unexpected listener, child escape, secret leak or unapproved effect.
- Recovery and compensation: exercise interruption at every commit/worker boundary and prove exact final state, bounded cleanup and inspectable receipts.
- Acceptance: full Release verification; low-resource follow-up; GATE-A-007 remeasurement; founder review of clarity, approval friction, file safety, command visibility, error recovery, Trust usefulness and developer control.

## Founder Gate B scope

Founder Gate B may choose Accept, Accept with Amendments, Repeat Phase 7, Replace
a provisional decision, or Stop and Replan. Acceptance must record the reviewed
build, demonstration, passed/failed evidence, limitations, amendments, ADR
decisions and explicit permission or denial for Phase 8 Local Intelligence.

Gate B asks whether patch clarity, approval friction, file safety, command
visibility, error recovery and Trust evidence preserve developer control. **No
AI-generated patch, AI runtime, model provider, agent loop or autonomous mutation
may be introduced before Gate B passes.**

## Explicit non-goals

- Arbitrary shell or command string.
- Write-capable command templates in the initial tool slice.
- Package installation, Git stage/commit/push or repository hooks.
- AI-generated patches, model inference, agents or workflows.
- Plugins, MCP servers, third-party connectors or browser storage.
- Network clients/listeners or cloud telemetry.
- Direct Desktop/project-file/database access.
- Silent conflict resolution, blind crash replay or overwrite of developer edits.
- Claiming patch compensation is device-loss backup.
