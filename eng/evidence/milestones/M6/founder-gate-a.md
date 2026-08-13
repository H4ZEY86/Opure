# Founder Gate A Review

Review date: 13 August 2026

Build identity: `0a25b3425abe325c78ee8e9deaaf37984448a07e`

Release channel: Development

Decision authority: repository founder directive in the active development task.

Directive retained verbatim: **“With Gate A (Foundation/Stability) cleared, your next phase should focus on Integration/Interoperability.”**

## Decision

**Accept with Amendments.**

Founder Gate A is cleared as the stable, non-agent Foundation baseline. Entry to
Phase 7 Controlled Mutation is **explicitly approved**. This approval does not
authorise AI runtimes, agents, plugins, MCP servers, third-party connectors or
network listeners; those remain gated by later phases and ADR evidence.

## Build and demonstration result

- GATE-A-001: 32-step isolated Development-channel demonstration passed.
- GATE-A-002: crash and restart recovery matrix passed.
- GATE-A-003: authenticated IPC security matrix passed with zero Runtime TCP/UDP listeners.
- GATE-A-004: Windows filesystem adversarial matrix passed.
- GATE-A-005: configuration adversarial and last-known-good matrix passed.
- GATE-A-006: Trust Evidence forgery, reconciliation and rebuild matrix passed.
- GATE-A-007: 22-measurement performance baseline passed its regression thresholds.
- GATE-A-008: 12-flow keyboard, UI Automation and high-contrast baseline passed.
- GATE-A-009: 14-ADR evidence matrix passed its commit/test/path verifier.
- Complete Release verification: **765 tests passed, zero warnings, zero errors**.

## Review questions

1. Desktop and Runtime separation: accepted as understandable enough for Controlled Mutation; Desktop remains projection/command only.
2. Project opening performance: accepted for Gate A; small and medium project measurements passed, while startup misses remain visible.
3. Trust Centre usefulness: accepted for the Foundation projection; later usability review must prevent evidence noise as scope grows.
4. Configuration provenance: accepted for requested/effective/source/policy and invalid-source inspection.
5. Error actionability: accepted with visible retry/recovery actions and stable safe text.
6. Recovery confidence: accepted for same-device local rollback only; it is not device-loss protection.
7. Avalonia: retain for Gate A, subject to the recorded packaged accessibility trigger.
8. Process topology: retain Bootstrap → Runtime + Desktop and in-process first-party service grouping.
9. Controlled Mutation base: approved.
10. Accepted limitations: the limitations below are accepted for Phase 7 entry, not silently waived for later release gates.

## Evidence failures and accepted limitations

- Desktop shell visibility measured 5.834 seconds against the provisional target below two seconds.
- Runtime readiness measured 3.673 seconds against the provisional target below three seconds.
- Packaged audible Narrator listening quality remains a release-candidate confirmation; Gate A automated the Windows UI Automation contract.
- ADR-0011 hosted-workflow assumptions do not match the deliberate repository removal of GitHub Actions and require amendment.
- Low-resource Windows 11 reference-hardware and long-duration endurance follow-ups remain open.
- Recovery Points are same-device rollback and do not protect against device loss, disk failure or destructive malware.
- ARM64, non-Windows transports and non-Windows filesystem adapters remain outside Gate A.
- No AI, agent, plugin, MCP, connector, browser-storage or network-listener capability was reviewed or authorised.

No unexplained evidence failure is accepted. The two performance misses are
retained as measured deviations, not converted into passing targets.

## Required amendments

| Amendment | Owner | Due date | Entry impact |
| --- | --- | --- | --- |
| Amend ADR-0011 to distinguish authoritative local verification from optional hosted automation. | Architecture | 20 August 2026 | Non-blocking for Phase 7; blocking before Founder Gate B. |
| Remeasure Desktop visibility and Runtime readiness after startup profiling; do not weaken targets without review. | Desktop and Runtime | 27 August 2026 | Non-blocking for Phase 7; result must enter Gate B evidence. |
| Run packaged Narrator listening review and retain-or-replace trigger assessment. | Desktop and Accessibility | 30 September 2026 | Blocking before Gate D installer acceptance. |
| Run the recorded Windows 11 four-core, 8 GB low-resource follow-up. | Performance | 3 September 2026 | Blocking before Gate B acceptance. |
| Preserve same-device warning and add no disaster-recovery claim until ADR-0028 later subsets are implemented. | Recovery and Product | 20 August 2026 | Continuous invariant. |

## ADR status decisions

- ADR-0001, ADR-0002, ADR-0003, ADR-0004, ADR-0005, ADR-0006, ADR-0008, ADR-0009, ADR-0010 and ADR-0012: retain the reviewed Foundation direction; remain Proposed pending formal ADR status edits.
- ADR-0011: Amend; remain Proposed.
- ADR-0026, ADR-0027 and ADR-0028: retain only the explicitly reviewed Foundation subsets; remain Proposed because wider scope is incomplete.
- No ADR is marked Accepted solely because code exists.

## Phase 7 entry boundary

Approved scope begins with deterministic, reviewable file mutation and then
capability-bound curated commands, following GATE-A-011. Authority remains in
deterministic services. Local intelligence does not enter until Founder Gate B;
remote providers, plugins and MCP remain later work after Founder Gate C.

Founder decision: **Accept with Amendments**

Phase 7 entry: **Approved**
