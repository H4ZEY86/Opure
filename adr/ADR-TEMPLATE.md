# ADR-[NNNN] — [Decision Title]

## Architecture Decision Record

**Status:** Proposed  
**Date:** YYYY-MM-DD  
**Decision owners:** [Name or role]  
**Technical owners:** [Name or role]  
**Reviewers:** [Names or roles]  
**Supersedes:** None  
**Superseded by:** None  
**Related ADRs:** None  
**Related specifications:** [CHARTER-001, SPEC-XXX]  
**Target milestone:** [Phase or milestone]  

---

## 1. Decision Summary

<!--
State the decision in one short paragraph.

A reader should understand the selected direction without reading the full ADR.
Do not describe an unresolved proposal as a completed decision.
-->

[Concise decision summary.]

---

## 2. Status

Use one of the following states:

- **Proposed** — drafted and awaiting review;
- **Accepted** — approved for implementation;
- **Rejected** — considered but not approved;
- **Deprecated** — retained for historical context but no longer recommended;
- **Superseded** — replaced by a later ADR;
- **Withdrawn** — removed before a final decision;
- **Experimental** — approved only for a bounded prototype;
- **Under Review** — accepted decision being reconsidered.

Current status:

> **Proposed**

---

## 3. Context

<!--
Explain the problem that requires a decision.

Include:
- why the decision is needed now;
- which system or milestone depends on it;
- what is currently unknown or blocked;
- and what happens if no decision is made.
-->

[Describe the architectural context.]

---

## 4. Problem Statement

<!--
Write one precise problem statement.

Example:
"Opure requires a primary implementation language that supports Windows 11,
service contracts, process supervision, desktop integration and long-term
cross-platform portability."
-->

[State the problem to solve.]

---

## 5. Decision Drivers

The decision should be evaluated against the following drivers.

<!--
Remove irrelevant entries and add decision-specific drivers.
Do not weight a criterion merely to force a preferred result.
-->

- alignment with the Opure Charter;
- developer control;
- local-first operation;
- Windows 11 support;
- future cross-platform viability;
- security;
- recoverability;
- performance;
- maintainability;
- testability;
- observability;
- ecosystem maturity;
- implementation complexity;
- operational complexity;
- migration risk;
- team capability;
- licensing;
- and long-term replacement cost.

---

## 6. Governing Principles

This decision must remain consistent with:

- **Developer Respect**
- **Developer First**
- **Software Engineering First**
- **Local by Design**
- **Cloud Optional**
- **Human in Control**
- **Visible by Design**
- **Inspectable Decisions**
- **Reviewable Changes**
- **Reversible Wherever Technically Practical**
- **Open by Architecture**
- **Loose Coupling**
- **Least Privilege**
- **Project Isolation**
- **Performance Respect**

Relevant Charter or specification requirements:

- [Requirement and document reference]
- [Requirement and document reference]
- [Requirement and document reference]

---

## 7. Scope

This ADR decides:

- [In-scope decision]
- [In-scope decision]
- [In-scope decision]

This ADR does not decide:

- [Out-of-scope item]
- [Deferred item]
- [Decision owned by another ADR]

---

## 8. Constraints

Known constraints include:

- Windows 11 is the first supported operating system;
- local operation must not require a cloud account;
- protected file changes must pass through the Patch Service;
- AI providers must remain replaceable;
- services must communicate through defined contracts;
- third-party code must not gain hidden authority;
- secret values must remain in the Secrets Vault;
- and the first implementation must remain practical for a small team.

Decision-specific constraints:

- [Constraint]
- [Constraint]
- [Constraint]

---

## 9. Assumptions

<!--
List assumptions that materially influence the decision.

An assumption is not evidence.
Assumptions should be verified where practical.
-->

- [Assumption]
- [Assumption]
- [Assumption]

---

## 10. Options Considered

The following options were considered:

1. **Option A — [Name]**
2. **Option B — [Name]**
3. **Option C — [Name]**
4. **Option D — [Name, if applicable]**

---

## 11. Option A — [Name]

### 11.1 Description

[Describe the option.]

### 11.2 Advantages

- [Advantage]
- [Advantage]
- [Advantage]

### 11.3 Disadvantages

- [Disadvantage]
- [Disadvantage]
- [Disadvantage]

### 11.4 Risks

- [Risk]
- [Risk]

### 11.5 Evidence

- [Prototype, benchmark, documentation or experiment]
- [Prototype, benchmark, documentation or experiment]

### 11.6 Estimated Adoption Cost

- **Initial implementation:** [Low / Moderate / High]
- **Operational complexity:** [Low / Moderate / High]
- **Migration difficulty:** [Low / Moderate / High]
- **Replacement difficulty:** [Low / Moderate / High]

---

## 12. Option B — [Name]

### 12.1 Description

[Describe the option.]

### 12.2 Advantages

- [Advantage]
- [Advantage]
- [Advantage]

### 12.3 Disadvantages

- [Disadvantage]
- [Disadvantage]
- [Disadvantage]

### 12.4 Risks

- [Risk]
- [Risk]

### 12.5 Evidence

- [Prototype, benchmark, documentation or experiment]
- [Prototype, benchmark, documentation or experiment]

### 12.6 Estimated Adoption Cost

- **Initial implementation:** [Low / Moderate / High]
- **Operational complexity:** [Low / Moderate / High]
- **Migration difficulty:** [Low / Moderate / High]
- **Replacement difficulty:** [Low / Moderate / High]

---

## 13. Option C — [Name]

### 13.1 Description

[Describe the option.]

### 13.2 Advantages

- [Advantage]
- [Advantage]
- [Advantage]

### 13.3 Disadvantages

- [Disadvantage]
- [Disadvantage]
- [Disadvantage]

### 13.4 Risks

- [Risk]
- [Risk]

### 13.5 Evidence

- [Prototype, benchmark, documentation or experiment]
- [Prototype, benchmark, documentation or experiment]

### 13.6 Estimated Adoption Cost

- **Initial implementation:** [Low / Moderate / High]
- **Operational complexity:** [Low / Moderate / High]
- **Migration difficulty:** [Low / Moderate / High]
- **Replacement difficulty:** [Low / Moderate / High]

---

## 14. Comparison Matrix

<!--
Use a simple scale such as:
1 = poor
2 = weak
3 = acceptable
4 = strong
5 = excellent

Scores should be supported by evidence or explicit judgement.
Do not present subjective scores as objective fact.
-->

| Criterion | Weight | Option A | Option B | Option C | Notes |
|---|---:|---:|---:|---:|---|
| Charter alignment | [1–5] | [1–5] | [1–5] | [1–5] | [Notes] |
| Windows 11 support | [1–5] | [1–5] | [1–5] | [1–5] | [Notes] |
| Cross-platform viability | [1–5] | [1–5] | [1–5] | [1–5] | [Notes] |
| Security | [1–5] | [1–5] | [1–5] | [1–5] | [Notes] |
| Recoverability | [1–5] | [1–5] | [1–5] | [1–5] | [Notes] |
| Performance | [1–5] | [1–5] | [1–5] | [1–5] | [Notes] |
| Maintainability | [1–5] | [1–5] | [1–5] | [1–5] | [Notes] |
| Testability | [1–5] | [1–5] | [1–5] | [1–5] | [Notes] |
| Ecosystem maturity | [1–5] | [1–5] | [1–5] | [1–5] | [Notes] |
| Team capability | [1–5] | [1–5] | [1–5] | [1–5] | [Notes] |
| Replacement cost | [1–5] | [1–5] | [1–5] | [1–5] | [Notes] |
| **Weighted total** |  | [Total] | [Total] | [Total] |  |

---

## 15. Decision

<!--
State exactly what is approved.

Include:
- selected option;
- boundaries;
- exceptions;
- implementation scope;
- and whether the decision is permanent, provisional or experimental.
-->

We will adopt **[Selected Option]** for [specific purpose].

The decision applies to:

- [Scope]
- [Scope]
- [Scope]

The decision does not grant:

- [Excluded authority]
- [Excluded behaviour]
- [Excluded coupling]

This decision is:

- [ ] Permanent until superseded
- [ ] Provisional
- [ ] Experimental
- [ ] Limited to one milestone
- [ ] Limited to one subsystem

---

## 16. Rationale

The selected option is preferred because:

- [Reason linked to a decision driver]
- [Reason linked to a decision driver]
- [Reason linked to a decision driver]
- [Reason linked to evidence]

The selected option is not perfect.

The most important accepted weaknesses are:

- [Weakness]
- [Weakness]

---

## 17. Consequences

### 17.1 Positive Consequences

- [Positive consequence]
- [Positive consequence]
- [Positive consequence]

### 17.2 Negative Consequences

- [Negative consequence]
- [Negative consequence]
- [Negative consequence]

### 17.3 Neutral Consequences

- [Neutral consequence]
- [Neutral consequence]

### 17.4 New Responsibilities

This decision creates responsibility for:

- [Owner and responsibility]
- [Owner and responsibility]
- [Owner and responsibility]

---

## 18. Security Impact

<!--
Describe the security impact even when the answer is "no material change".
-->

### 18.1 Trust Boundaries

Affected trust boundaries:

- [Boundary]
- [Boundary]

### 18.2 Permissions

Required permissions:

- [Permission]
- [Permission]

### 18.3 Secrets

Secret-handling impact:

- [Impact]
- [Impact]

### 18.4 Network

Network impact:

- [Destination or none]
- [Data classification]
- [Approval requirement]

### 18.5 Threats

Relevant threats include:

- [Threat]
- [Threat]
- [Threat]

### 18.6 Mitigations

- [Mitigation]
- [Mitigation]
- [Mitigation]

---

## 19. Privacy Impact

This decision affects privacy as follows:

- [Local data impact]
- [External sharing impact]
- [Retention impact]
- [Deletion impact]
- [Telemetry impact]

Required privacy controls:

- [Control]
- [Control]

---

## 20. Developer-Control Impact

Explain how the decision affects:

- developer authority;
- approval;
- visibility;
- manual alternatives;
- cancellation;
- reversibility;
- provider choice;
- and project ownership.

Assessment:

[Developer-control impact.]

---

## 21. Performance Impact

### 21.1 Expected Resource Use

- **CPU:** [Impact]
- **Memory:** [Impact]
- **GPU:** [Impact]
- **VRAM:** [Impact]
- **Disk:** [Impact]
- **Network:** [Impact]
- **Startup:** [Impact]
- **Idle use:** [Impact]

### 21.2 Reference Hardware

Expected behaviour on the reference machine:

- Windows 11;
- AMD Ryzen 9 5950X;
- 32 GB RAM;
- NVIDIA RTX 5070 Ti with 16 GB VRAM.

[Expected behaviour.]

### 21.3 Required Measurements

- [Benchmark]
- [Benchmark]
- [Benchmark]

---

## 22. Reliability and Recovery

### 22.1 Failure Modes

- [Failure mode]
- [Failure mode]
- [Failure mode]

### 22.2 Cancellation

Cancellation behaviour:

[Describe cancellation.]

### 22.3 Retry

Retry behaviour:

[Describe bounded and idempotency-aware retry.]

### 22.4 Recovery

Recovery behaviour:

[Describe restart, rollback or repair.]

### 22.5 Data Integrity

Data-integrity protections:

- [Protection]
- [Protection]

---

## 23. Observability and Trust Centre

The implementation must expose:

- [Health state]
- [Metrics]
- [Logs]
- [Trust Centre records]
- [Correlation]
- [Diagnostics]

Secret values must not appear in logs or Trust Centre records.

---

## 24. Compatibility

### 24.1 Contract Compatibility

Affected contracts:

- [Contract]
- [Contract]

### 24.2 Data Compatibility

Affected data formats or stores:

- [Store or format]
- [Store or format]

### 24.3 Platform Compatibility

- **Windows 11:** [Supported / Limited / Unsupported]
- **Linux future:** [Expected impact]
- **macOS future:** [Expected impact]

### 24.4 Plugin and MCP Compatibility

[Compatibility impact.]

---

## 25. Migration

### 25.1 Existing State

[Describe existing implementation or state.]

### 25.2 Migration Steps

1. [Step]
2. [Step]
3. [Step]

### 25.3 Migration Validation

- [Check]
- [Check]

### 25.4 Migration Failure

[Describe recovery from failed migration.]

---

## 26. Rollback or Replacement

This decision may be reversed or replaced by:

- [Rollback approach]
- [Adapter boundary]
- [Migration path]

Rollback limitations:

- [Limitation]
- [Limitation]

A replacement should not require rewriting:

- [Protected subsystem]
- [Protected subsystem]

---

## 27. Implementation Plan

### 27.1 Initial Tasks

1. [Task]
2. [Task]
3. [Task]
4. [Task]

### 27.2 Owners

| Area | Owner |
|---|---|
| Architecture | [Owner] |
| Implementation | [Owner] |
| Security review | [Owner] |
| Testing | [Owner] |
| Documentation | [Owner] |

### 27.3 Milestones

- [Milestone]
- [Milestone]
- [Milestone]

---

## 28. Testing Strategy

Required tests include:

### 28.1 Unit Tests

- [Test]
- [Test]

### 28.2 Contract Tests

- [Test]
- [Test]

### 28.3 Integration Tests

- [Test]
- [Test]

### 28.4 Architecture Tests

- [Boundary or dependency rule]
- [Boundary or dependency rule]

### 28.5 Security Tests

- [Test]
- [Test]

### 28.6 Fault-Injection Tests

- [Failure]
- [Failure]

### 28.7 Recovery Tests

- [Test]
- [Test]

### 28.8 Performance Tests

- [Benchmark]
- [Benchmark]

### 28.9 Accessibility or Usability Tests

- [Test]
- [Test]

---

## 29. Acceptance Criteria

This decision is successfully implemented when:

- [ ] [Criterion]
- [ ] [Criterion]
- [ ] [Criterion]
- [ ] Security review is complete.
- [ ] Recovery behaviour is tested.
- [ ] Trust Centre behaviour is implemented.
- [ ] Documentation is updated.
- [ ] Known limitations are recorded.
- [ ] Relevant specifications remain satisfied.

---

## 30. Evidence Required Before Acceptance

The ADR should not move to **Accepted** until the following evidence is available:

- [ ] architecture review;
- [ ] prototype or proof of concept;
- [ ] security review;
- [ ] performance measurement;
- [ ] failure and recovery test;
- [ ] compatibility assessment;
- [ ] licensing review where relevant;
- [ ] founder or authorised approval.

Decision-specific evidence:

- [ ] [Evidence]
- [ ] [Evidence]

---

## 31. Known Limitations

- [Limitation]
- [Limitation]
- [Limitation]

These limitations must be visible in implementation and documentation.

---

## 32. Open Questions

- [Question]
- [Question]
- [Question]

Open questions that block acceptance must be resolved or explicitly deferred.

---

## 33. Deferred Decisions

This ADR intentionally defers:

- [Decision]
- [Decision]
- [Decision]

Each deferred decision should identify its expected ADR or milestone.

---

## 34. Alternatives Rejected

### [Rejected Alternative]

Rejected because:

- [Reason]
- [Reason]

### [Rejected Alternative]

Rejected because:

- [Reason]
- [Reason]

---

## 35. Review Record

| Date | Reviewer | Decision | Notes |
|---|---|---|---|
| YYYY-MM-DD | [Name or role] | [Approve / Reject / Changes Requested] | [Notes] |

---

## 36. Approval

### Founder or Product Approval

- **Name:** [Name]
- **Decision:** [Approved / Rejected / Changes Requested]
- **Date:** YYYY-MM-DD
- **Notes:** [Notes]

### Architecture Approval

- **Name or role:** [Name or role]
- **Decision:** [Approved / Rejected / Changes Requested]
- **Date:** YYYY-MM-DD
- **Notes:** [Notes]

### Security Approval

- **Name or role:** [Name or role]
- **Decision:** [Approved / Rejected / Changes Requested / Not Required]
- **Date:** YYYY-MM-DD
- **Notes:** [Notes]

---

## 37. Supersession

This ADR is superseded only when a later ADR:

- names this ADR explicitly;
- explains why the decision changed;
- describes migration or compatibility impact;
- and updates the `Superseded by` field above.

Historical ADRs must remain in version control.

---

## 38. Change History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | YYYY-MM-DD | [Author] | Initial proposal |

---

## 39. Final Decision Statement

<!--
Repeat the final decision in one precise sentence after approval.
-->

> **Opure will [approved decision] because [primary reason], subject to [important boundary or limitation].**

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**