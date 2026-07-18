# ADR-0027 — Trust Centre, Evidence Retention and Support Bundles

## Architecture Decision Record

**Status:** Proposed
**Date:** 18 July 2026
**Decision owners:** Founder and Product Owner
**Technical owners:** Trust Evidence, Diagnostics and Supportability Owner
**Reviewers:** Runtime Architecture Owner, Persistence Owner, Observability Owner, Security Owner, Privacy Owner, Secrets Owner, Configuration Owner, Workflow Owner, Project Service Owner, Workspace Owner, Repository Owner, Patch Service Owner, Build Owner, Test Owner, Context Assembly Owner, AI Router Owner, Provider Trust Owner, Local Model Runtime Owner, Project Knowledge Owner, Project Memory Owner, Tool Mediation Owner, Plugin Platform Owner, MCP Gateway Owner, Network Gateway Owner, Update Owner, Release Owner, Recovery Owner, Desktop Owner, Enterprise Management Owner, Performance Owner, Test Architecture Owner
**Supersedes:** None
**Superseded by:** None
**Related ADRs:** ADR-0001 through ADR-0026
**Related specifications:** CHARTER-001, SPEC-001 through SPEC-012
**Target milestone:** Trust Centre and Supportability through Version 1.0

---

## 1. Decision Summary

Opure should implement a trusted local **Trust Evidence Service** and a developer-facing **Trust Centre** that make important product decisions, external interactions, policy applications, side effects, failures and recovery actions inspectable without turning operational logs into an unbounded surveillance store.

The architecture should make a strict distinction between:

1. **Authoritative Domain Records**, which remain owned by the service that made or enforced a decision;
2. **Trust Evidence Records**, which provide a typed, immutable receipt or reference to an authoritative domain event;
3. **Trust Centre Projections**, which are rebuildable read models for developer inspection;
4. **Operational Telemetry**, which consists of logs, traces and metrics used for diagnostics and performance;
5. **Diagnostic Artefacts**, such as traces, GC dumps and process dumps;
6. **Preserved Incident Evidence**, which is a reviewed copy held for a specific incident or investigation;
7. and **Support Bundles**, which are newly generated, redacted, user-reviewed local exports.

The selected principle is:

> **Trust records explain what Opure decided and did. Telemetry helps diagnose how it behaved. A support bundle is a reviewed export of selected evidence. These are related, but they are not interchangeable.**

The Trust Centre should be a **read-only federated projection**.

It should not become a second authority for:

* workflow state;
* configuration;
* provider approval;
* model routing;
* project memory;
* patch application;
* tool execution;
* plugin capability;
* MCP calls;
* update state;
* or secret access.

Each owning service should persist its authoritative record transactionally with the action or decision it owns.

Each owning service should publish a typed Trust Evidence Record through its transactional outbox.

The Trust Evidence Service should:

* validate the record;
* verify its schema and hashes;
* deduplicate it;
* retain a projection or safe reference;
* reconcile missing or conflicting records with the owner;
* and expose it through the Trust Centre.

A missing Trust Centre projection should not imply that the authoritative action did not occur.

A Trust Centre projection should not authorise a new action.

The initial Trust Evidence Service should own:

* Trust Evidence schema registration;
* evidence-type registration;
* evidence ingestion;
* evidence deduplication;
* evidence indexes;
* evidence relationships;
* cross-service correlation;
* evidence integrity checks;
* retention-policy calculation;
* retention execution for evidence it owns;
* Preservation Holds;
* support-bundle definitions;
* support-bundle planning;
* diagnostic collection orchestration;
* redaction;
* secret scanning;
* bundle preview;
* bundle manifest generation;
* local export;
* bundle deletion;
* and Trust Centre read models.

The service should not own:

* raw domain databases;
* Vault values;
* project source;
* provider credentials;
* tool execution;
* model execution;
* operating-system event logs;
* Windows Error Reporting;
* or external support systems.

The initial evidence architecture should use:

* one service-owned Trust Evidence SQLite database per release channel;
* one content-addressed evidence artefact store per channel;
* append-only evidence-ingestion records;
* immutable Evidence Record revisions;
* rebuildable projections;
* transactional inbox and outbox;
* typed owner-service query adapters;
* and bounded local indexes.

The Trust Evidence database should store:

* record envelopes;
* hashes;
* safe typed attributes;
* source references;
* relationship indexes;
* retention state;
* preservation state;
* redaction state;
* support-bundle plans;
* support-bundle manifests;
* and bundle-generation receipts.

It should not duplicate every full owner-service payload by default.

Large or sensitive payloads should remain:

* in the owner service;
* in an owner-controlled CAS;
* or in a restricted Trust Evidence CAS

according to an explicit evidence contract.

The initial Trust Evidence Record envelope should include:

```text
evidence_id
schema
evidence_type
evidence_revision
owner_service
owner_record_id
owner_record_revision
release_channel
service_version
project_id
workflow_instance_id
operation_id
trace_id
span_id
actor
subject
action
outcome
authority_class
data_classification
occurred_at_utc
observed_at_utc
monotonic_sequence
runtime_boot_id
payload_reference
payload_sha256
previous_stream_sha256
record_sha256
retention_class
preservation_state
```

Not every field is required for every type.

Every Evidence Type should define:

* required fields;
* optional fields;
* safe indexed fields;
* sensitive fields;
* payload location;
* authority class;
* default retention;
* relationship types;
* redaction rules;
* support-bundle eligibility;
* and owner reconciliation contract.

The initial Authority Classes should be:

1. Authoritative Domain Decision
2. Authoritative Domain Effect
3. Authoritative Domain State Transition
4. Verified Service Receipt
5. Verified External Receipt
6. Deterministic Validation Result
7. Human Decision
8. Derived Trust Projection
9. Operational Observation
10. Diagnostic Observation
11. Model-Generated Proposal
12. User-Provided Assertion
13. Imported Historical Evidence
14. Unknown or Unverified

A Model-Generated Proposal must never be displayed as an authoritative decision merely because it appears in the Trust Centre.

A User-Provided Assertion should retain:

* actor;
* time;
* scope;
* reason;
* and supporting evidence

without becoming equivalent to a deterministic receipt.

The initial Trust Evidence Types should cover:

* application start and shutdown;
* service readiness;
* project open and close;
* configuration and policy resolution;
* policy conflict;
* capability grant, denial and revocation;
* Vault secret-use receipt without secret value;
* Context Plan creation;
* Data Sharing Plan approval;
* Routing Decision;
* AI provider request and response receipt;
* local-model request and response receipt;
* model identity resolution;
* model fallback;
* project indexing;
* Project Knowledge query;
* Project Memory proposal, acceptance, supersession and deletion;
* workflow creation, transition, side effect, approval, compensation and recovery;
* tool-call plan, approval, execution and result;
* command execution;
* patch proposal, approval, application and verification;
* workspace and repository writes;
* build and test result;
* plugin package, trust, permission and execution;
* MCP server identity, capability and call;
* network request through Network Gateway;
* update check, package selection and installer hand-off;
* package, plugin and model installation;
* security incident;
* retention deletion;
* preservation action;
* support-bundle generation;
* support-bundle export;
* and support-bundle deletion.

The Trust Centre should correlate evidence using:

* project ID;
* operation ID;
* workflow instance;
* step and attempt;
* effect ID;
* request ID;
* approval ID;
* Context Plan;
* Routing Decision;
* Data Sharing Plan;
* Provider Profile;
* AI Execution Profile;
* tool-call ID;
* patch ID;
* configuration snapshot;
* and trace context.

Correlation IDs should be opaque.

They should not contain:

* project names;
* source paths;
* user names;
* email addresses;
* provider secrets;
* or source text.

The initial Trust Evidence integrity model should use:

* SHA-256 payload hashes;
* SHA-256 record hashes;
* per-owner-stream hash chaining where practical;
* immutable content-addressed artefacts;
* database foreign keys;
* schema validation;
* source-revision binding;
* periodic tail verification;
* and cross-service reconciliation.

The system should not claim:

* external non-repudiation;
* legal chain of custody;
* tamper-proof audit;
* trusted timestamping;
* or protection against a fully compromised same-user account.

A same-user attacker able to rewrite:

* the database;
* owner-service records;
* application files;
* and app-managed keys

may be able to rewrite local evidence.

Hash chains should be described as:

> **Local integrity and corruption evidence, not externally anchored proof.**

The initial clock model should record both:

* `occurred_at_utc`, when the source says the event occurred;
* and `observed_at_utc`, when the Trust Evidence Service observed it.

Ordering should primarily use:

* owner sequence;
* workflow sequence;
* operation sequence;
* and ingestion sequence

rather than wall-clock time alone.

A Runtime boot ID and process identity should help explain events across restarts.

The system should not claim that the local Windows clock is externally trusted.

The initial evidence relationship model should support:

* Causes;
* Caused By;
* Authorises;
* Authorised By;
* Implements;
* Uses;
* Produces;
* Supersedes;
* Retries;
* Reconciles;
* Compensates;
* Violates;
* Resolves;
* Derives From;
* Belongs To;
* and Correlates With.

Relationships should be typed and directional.

A model should not create an authoritative relationship directly.

The initial Trust Centre views should include:

```text
Overview
Projects
Operations
AI and Models
Data Sharing
Context
Knowledge
Memory
Workflows
Approvals
Tools and Commands
Patches and Writes
Builds and Tests
Plugins
MCP
Network
Configuration and Policy
Secrets Use
Updates and Installation
Security and Privacy
Retention and Deletion
Preservation
Support Bundles
System Health
```

The initial operational telemetry architecture should remain aligned with ADR-0006:

* `ILogger` for logs;
* `ActivitySource` for traces;
* `Meter` for metrics;
* OpenTelemetry-compatible data models;
* local storage by default;
* no telemetry export by default;
* strict cardinality;
* strict redaction;
* and explicit bounded collection.

Operational telemetry should not be used as the sole evidence for:

* a patch application;
* a secret access;
* a provider approval;
* a workflow side effect;
* a configuration commit;
* or a capability decision.

Those events need authoritative receipts.

The initial Operational Log Record should use a stable structured envelope aligned conceptually with the stable OpenTelemetry Logs Data Model:

```text
timestamp
observed_timestamp
trace_id
span_id
severity_text
severity_number
event_name
body
resource
instrumentation_scope
attributes
```

Opure should use:

* `event_name` for a stable event class;
* bounded structured attributes;
* trace and span correlation;
* stable severity mapping;
* and explicit service resource identity.

Free-form bodies should be secondary.

High-value diagnostic events should be structured.

The initial logs should exclude by default:

* source code;
* prompts;
* model responses;
* file contents;
* secret values;
* access tokens;
* credentials;
* raw request headers;
* raw response headers;
* full command lines;
* process environment;
* clipboard;
* keystrokes;
* full user names;
* email addresses;
* absolute home paths;
* arbitrary MCP payloads;
* plugin private data;
* and full exception object graphs containing user data.

Exception logs should include:

* exception type;
* stable error code;
* safe message;
* stack where safe;
* service;
* operation;
* and trace context.

Exception `Data` dictionaries should not be serialised automatically.

The initial trace policy should record:

* operation boundaries;
* service calls;
* queue time;
* execution time;
* result class;
* and safe identifiers.

It should not record full payloads.

The initial metrics policy should use low-cardinality dimensions.

Project IDs, workflow IDs, operation IDs, file paths, provider request IDs and user names should not become metric labels.

The initial retention architecture should use versioned **Retention Policies** by:

* evidence class;
* purpose;
* scope;
* data classification;
* unresolved state;
* preservation state;
* and owner-service dependency.

Retention should not be one global number.

The initial retention-state model should include:

* Active;
* Expiring;
* Preserved;
* Deletion Scheduled;
* Deleted;
* Purged;
* Owner Retained;
* Owner Missing;
* Legal or Administrative Hold;
* Security Hold;
* User Hold;
* and Retention Conflict.

A hold name does not create legal authority by itself.

Opure should record the hold's:

* creator;
* purpose;
* scope;
* time;
* review date;
* and release authority.

The initial default retention recommendations should be:

### Authoritative Trust Evidence

```text
180 days after terminal operation
or while referenced by an active workflow, unresolved incident,
retained configuration snapshot, release record or preservation hold
```

### Security-Critical Trust Evidence

```text
365 days
```

### Ordinary Application Logs

```text
14 days
```

### Debug Logs

```text
24 hours by default
7 days maximum without a new explicit diagnostic session
```

### Traces

```text
7 days
```

### High-Volume Detailed Traces

```text
24 hours
```

### Aggregated Local Metrics

```text
30 days
```

### Raw Metric Samples

```text
7 days
```

### Crash Triage Dumps

```text
disabled by default
7 days after explicit collection
```

### Heap or Full Dumps

```text
disabled by default
24 hours after explicit collection unless preserved
```

### GC Dumps

```text
7 days
```

### Support-Bundle Staging

```text
24 hours
```

### Exported Support Bundle

User-owned after export.

Opure should offer a local deletion action but should not claim control over copies moved elsewhere.

### Invalid or Rejected Support-Bundle Candidates

```text
24 hours
```

### Preservation Hold Copies

Until explicit reviewed release or configured administrative expiry.

These values are provisional and must be configurable within Product, Enterprise and privacy policy.

The retention engine should calculate one **Retention Decision** for each record or artefact.

The decision should include:

```text
retention_decision_id
record_or_artefact
policy_revisions
purpose
classification
base_expiry
dependency_extensions
hold_extensions
effective_expiry
deletion_method
reason
created_at
decision_sha256
```

A more permissive user preference should not override:

* Product minimum evidence needed for unresolved effects;
* enterprise incident policy;
* active Preservation Hold;
* or owner-service referential requirements.

A longer retention period should not be selected merely because storage is available.

The initial privacy principle should be:

> **Collect the minimum evidence necessary for the stated operational, security, recovery or support purpose, and retain it only while that purpose remains valid.**

The architecture should support:

* evidence inventory;
* purpose;
* classification;
* data subject relevance where known;
* owner;
* retention;
* deletion;
* export;
* and review.

The initial deletion model should distinguish:

1. Logical Removal from Trust Centre
2. Projection Deletion
3. Payload Deletion
4. Owner-Record Deletion
5. CAS Garbage Collection
6. Bundle Staging Deletion
7. Exported File Deletion
8. Secure-Erase Request
9. Tombstone Retention

Deleting a projection should not silently delete an authoritative owner record.

Deleting an owner record should be governed by that owner service.

The Trust Centre should show:

* what was deleted;
* what remains;
* which owner retains a record;
* what is held;
* and what is outside Opure's control.

The initial deletion workflow should:

1. resolve dependencies;
2. check holds;
3. check owner authority;
4. preview records and artefacts;
5. calculate policy;
6. acquire approval where required;
7. delete owner and Trust records in defined order;
8. garbage collect unreferenced artefacts;
9. write safe deletion receipts;
10. and verify absence where practical.

Deletion receipts should not retain the deleted personal or source content.

The initial forensic-deletion statement should be:

> **Opure can delete controlled database records and files and can release references to content-addressed artefacts. It cannot guarantee forensic erasure from SSD wear-levelling, filesystem journals, backups, external copies or previously exported bundles.**

The initial Preservation Hold model should be an application feature for operational, security or administrative preservation.

It should not be marketed as legal e-discovery or court-ready evidence management.

A Preservation Hold should include:

```text
hold_id
hold_type
scope
purpose
created_by
created_at
review_due
release_authority
record_selectors
artefact_selectors
retention_override
state
hold_sha256
```

The initial hold types should be:

* Security Incident;
* Recovery Investigation;
* Support Investigation;
* Release Investigation;
* User Requested;
* Enterprise Administrative;
* and External Legal Instruction Recorded by Administrator.

The final type does not mean Opure has verified the legal instruction.

The initial preserved-evidence process should:

1. identify records;
2. acquire immutable owner references;
3. copy selected payloads where necessary;
4. calculate hashes;
5. record source and time;
6. record collection tool and version;
7. record redaction state;
8. restrict access;
9. record every access and export;
10. verify periodically;
11. and release only through explicit review.

Preserved copies should remain separate from ordinary support bundles.

The initial incident record should include:

* incident ID;
* title;
* category;
* severity;
* affected projects and services;
* detected time;
* reported time;
* status;
* owners;
* evidence references;
* containment;
* recovery;
* lessons;
* and closure.

Incident response should be integrated with normal product risk management rather than existing only after an event.

The initial support-bundle principle should be:

> **A support bundle is created locally, from an explicit plan, after bounded collection, classification, redaction and preview. Opure does not upload support data automatically.**

The initial support-bundle presets should be:

1. Minimal
2. Standard
3. Performance
4. Crash Triage
5. Workflow Recovery
6. Provider and AI
7. Plugin and MCP
8. Security Incident Export
9. Custom

A preset should be a versioned Bundle Definition.

It should not be a hidden list embedded only in UI code.

The initial Minimal bundle should include:

* Opure product version;
* release channel;
* package identity;
* OS version;
* .NET runtime version;
* architecture;
* relevant service versions;
* configuration snapshot hashes and safe Resultant Configuration;
* health status;
* recent error and fatal log records;
* recent authoritative failure receipts;
* relevant migration state;
* and bundle manifest.

It should exclude:

* source code;
* prompts;
* model outputs;
* file contents;
* process dumps;
* EventPipe traces;
* GC dumps;
* raw metric series;
* project names by default;
* user names;
* absolute paths;
* and secrets.

The initial Standard bundle may add:

* selected project-scoped evidence;
* bounded recent logs;
* bounded recent traces;
* workflow and tool receipts;
* policy conflicts;
* retention state;
* and service diagnostics.

It should still exclude memory dumps and source content by default.

The initial Performance bundle may add:

* a bounded EventPipe trace;
* GC counters;
* selected runtime counters;
* CPU and allocation profiles;
* service timing;
* and scheduler state.

The collection duration should be explicit and bounded.

Suggested default:

```text
30 seconds
```

Suggested maximum without Development policy:

```text
5 minutes
```

The initial Crash Triage bundle may add:

* crash metadata;
* exception receipt;
* module inventory;
* managed thread stacks;
* and a triage or mini dump

after explicit high-risk approval.

A triage dump should still be treated as potentially sensitive.

Microsoft documentation currently describes .NET's `Triage` dump type as removing personal user information such as paths and passwords.

Opure should not interpret that description as a guarantee that a triage dump contains no secrets or personal data.

The initial Workflow Recovery bundle may add:

* Workflow Definition and Compiled Plan hashes;
* safe event timeline;
* step and attempt state;
* lease evidence;
* side-effect intent and receipts;
* reconciliation results;
* approval hashes;
* and Recovery Report.

It should not include secret values, full source or unredacted tool payloads.

The initial Provider and AI bundle may add:

* Provider Profile identity;
* AI Execution Profile identity;
* Routing Decision;
* Data Sharing Plan;
* request and response metadata;
* token counts;
* latency;
* model-resolved identity;
* and error receipts.

It should exclude prompt and model output by default.

The developer may explicitly add selected redacted prompt or output artefacts after preview.

The initial Plugin and MCP bundle may add:

* plugin package identity and hash;
* permission grants;
* Plugin Host health;
* MCP server fingerprint;
* account reference;
* tool schema hashes;
* call receipts;
* and gateway failures.

It should exclude:

* plugin private files;
* MCP credentials;
* arbitrary MCP results;
* and third-party process memory

unless explicitly selected and authorised.

The initial Security Incident Export should operate from Preserved Incident Evidence.

It should not be an ordinary one-click support preset.

It should require:

* incident scope;
* export purpose;
* access authorisation;
* complete manifest;
* no automatic redaction assumption;
* and explicit destination handling.

The initial support bundle container should be:

```text
.opure-support.zip
```

using a standard ZIP container with strict archive rules.

The archive should not use legacy ZIP encryption.

Version 1 should produce an unencrypted local file after explicit warning.

Secure transport or external encryption remains the user's or enterprise process responsibility until Opure adopts a separately reviewed modern encrypted-bundle format.

The initial bundle layout should be:

```text
manifest.json
README.txt
inventory.json
consent/
    approval.json
configuration/
    resultant-configuration.json
evidence/
    records.jsonl
    relationships.jsonl
telemetry/
    logs.jsonl
    traces.jsonl
    metrics.json
diagnostics/
    <explicit diagnostic artefacts>
workflows/
    <selected recovery reports>
incidents/
    <selected preserved evidence>
redaction/
    report.json
    findings.json
integrity/
    hashes.json
    omissions.json
```

Not every bundle contains every directory.

The initial Bundle Manifest should include:

```text
bundle_id
bundle_definition
product_version
channel
created_at
created_by
scope
time_range
projects
operations
included_items
excluded_items
redaction_policy
secret_scan
diagnostic_collection
retention
export_path_hash
manifest_sha256
```

The manifest should not contain a raw absolute export path.

The initial bundle-generation pipeline should be:

```text
Select problem and preset
    ↓
Resolve explicit scope and time range
    ↓
Build Support Bundle Plan
    ↓
Preview requested sources and risk
    ↓
Acquire collection capabilities
    ↓
Freeze owner-service snapshots
    ↓
Collect bounded records and artefacts
    ↓
Canonicalise and classify
    ↓
Apply field redaction
    ↓
Run pattern and canary secret scans
    ↓
Run structural validation
    ↓
Create inventory, omissions and hashes
    ↓
Display final file-by-file preview
    ↓
Acquire export approval
    ↓
Write staged archive
    ↓
Reopen and verify archive
    ↓
Atomically move to selected destination
    ↓
Record export receipt
    ↓
Delete staging on schedule
```

The initial Support Bundle Plan should bind:

* exact Bundle Definition revision;
* project scope;
* operation scope;
* time range;
* evidence types;
* telemetry types;
* diagnostic tools;
* diagnostic duration;
* data classifications;
* source-content policy;
* prompt and output policy;
* path-redaction policy;
* user-identity policy;
* dump policy;
* maximum size;
* staging retention;
* and export destination class.

A changed plan should invalidate approval.

The initial support-bundle collection should use owner-service query contracts.

It should not:

* copy entire SQLite databases;
* copy entire log directories;
* copy the Vault;
* copy project roots;
* copy `%USERPROFILE%`;
* enumerate browser history;
* enumerate unrelated processes;
* or collect full machine inventory.

The initial diagnostic collector should be a trusted supervised process.

It may use:

* `Microsoft.Diagnostics.NETCore.Client`;
* EventPipe;
* selected .NET diagnostic tools;
* and documented Windows diagnostic APIs

through bounded adapters.

Diagnostic collection should require:

* target process identity;
* Runtime boot identity;
* target service;
* collection type;
* duration or trigger;
* output limit;
* classification;
* approval;
* cancellation;
* and retention.

The .NET diagnostic port can request:

* memory dumps;
* EventPipe traces;
* and process command-line information.

Opure should not expose general diagnostic-port access to plugins or MCP servers.

The initial Support Collector should not request process command lines because command lines may contain sensitive values and are not required for ordinary support.

The initial dump policy should be:

* crash dumps disabled by default;
* no automatic full dump;
* no automatic heap dump;
* triage or mini dump preferred where a dump is explicitly approved;
* full or heap dump only under Development or explicit advanced diagnostic policy;
* no automatic inclusion in a support bundle;
* no automatic upload;
* separate high-risk warning;
* short retention;
* and post-collection secret scan of metadata where possible.

The contents of a process dump cannot be reliably redacted without potentially destroying diagnostic value.

Therefore, dump preview should show metadata and risk, not pretend to render every memory page safely.

A user should be told:

> **A process dump may contain source text, file paths, prompts, model output, credentials or other memory-resident data even when the selected dump type is intended to minimise personal information.**

The initial EventPipe trace policy should:

* use an allowlisted provider set;
* use bounded duration;
* use bounded buffer;
* avoid payload-heavy custom events;
* exclude source content;
* and record provider names and keywords in the bundle manifest.

The initial GC dump policy should note that collection triggers a garbage collection and may affect the target process.

Collection should be explicit for performance investigation.

The initial Windows Event Log policy should be:

* no broad automatic collection;
* no Security log collection;
* no Application log collection outside Opure sources by default;
* and explicit event-source and time-range selection where supported.

The initial system inventory should include only support-relevant values such as:

* Windows edition and build;
* architecture;
* CPU model;
* logical processor count;
* physical memory;
* GPU model and driver version where relevant;
* disk free space for Opure-owned volumes;
* .NET runtime version;
* Opure package identity;
* and locale/time-zone identifiers where relevant.

It should exclude:

* device serial numbers;
* full machine name by default;
* domain name by default;
* account name;
* installed application inventory;
* network adapter addresses;
* Wi-Fi profiles;
* browser information;
* and unrelated environment variables.

The initial identity-redaction policy should replace:

* user profile path;
* user name;
* machine name;
* project display name;
* repository remote URL;
* email;
* and selected external account display names

with stable bundle-local pseudonyms where the identity is not required.

Pseudonyms should be scoped to one bundle.

They should not permit correlation across unrelated support bundles by default.

The initial path-redaction policy should:

* preserve project-relative paths where approved;
* replace project root with `<PROJECT_ROOT>`;
* replace user profile with `<USER_PROFILE>`;
* replace Opure app data with `<OPURE_DATA>`;
* replace temp with `<TEMP>`;
* remove unrelated absolute paths;
* and retain only file extensions or safe path segments where required.

A path may still reveal sensitive names.

The final preview should expose every included path string category.

The initial source-content policy should be:

* no source content by default;
* no whole-file source export;
* explicit file or snippet selection;
* exact line range;
* classification;
* secret scan;
* and preview.

Generated patches may be included as selected artefacts because they are already review objects, but their contents still require preview.

The initial prompt and model-output policy should be:

* metadata only by default;
* exact selected content only;
* redact secrets and paths;
* preserve the fact that content was omitted;
* and never include hidden model reasoning.

The initial tool-result policy should be:

* receipt and schema metadata by default;
* selected result fields only;
* no raw command stdout or stderr by default;
* no environment;
* no secret-bearing headers;
* and explicit source-content preview.

The initial redaction architecture should use ordered layers:

1. Schema-Level Exclusion
2. Field-Level Classification Redaction
3. Owner-Service Redaction
4. Path and Identity Normalisation
5. Known Secret Reference Removal
6. Known Canary Scan
7. Pattern-Based Secret Scan
8. Entropy and Key-Shape Heuristics
9. Structural Validation
10. Final User Preview

No single scan should be treated as sufficient.

The initial redaction action types should be:

* Remove Field;
* Replace with Fixed Token;
* Replace with Bundle-Local Pseudonym;
* Replace with Length;
* Replace with Hash where safe;
* Truncate;
* Keep Metadata Only;
* Keep Selected Range;
* and Exclude Artefact.

Hashing should not be used for low-entropy sensitive values where it would enable guessing.

The initial Secret Scan Result should include:

```text
scanner_id
scanner_revision
artefact
finding_type
location
confidence
action
review_state
created_at
```

It should not store the secret itself.

The initial scan should recognise:

* Opure Vault canaries;
* configured provider key shapes;
* private-key headers;
* bearer tokens;
* common connection-string credentials;
* cloud credential formats;
* password assignment patterns;
* and high-entropy suspicious values.

A finding should block export until:

* removed;
* redacted;
* explicitly excluded;
* or reviewed under an advanced incident-export path.

The initial final preview should show:

* bundle size;
* every file;
* every data classification;
* source-content presence;
* prompt or model-output presence;
* dump presence;
* path categories;
* identity categories;
* secret findings;
* redaction count;
* omitted items;
* diagnostic side effects;
* staging retention;
* and destination.

The preview should support opening safe text files before export.

Binary dumps should show:

* type;
* size;
* target process;
* collection time;
* and risk warning.

The initial export should require an explicit file destination selected through the trusted file picker.

ADR-0009 applies.

Opure should:

* write to a staged file;
* flush;
* verify archive paths and hashes;
* atomically replace where safe;
* and record only a redacted destination reference or hash in evidence.

The initial support-bundle archive limits should be:

* 2 GiB maximum total bundle;
* 100,000 entries maximum;
* 512 MiB maximum ordinary file;
* larger dumps only under explicit advanced policy;
* path length limit;
* decompression-ratio checks for imported verification;
* no absolute paths;
* no drive paths;
* no UNC paths;
* no traversal;
* no alternate streams;
* no reserved device names;
* no duplicate archive paths;
* no case-colliding paths;
* no symlinks;
* and no reparse entries.

These values are provisional.

A generated bundle should be reopened and verified before success.

The initial support-bundle upload policy should be:

* no automatic upload;
* no background upload;
* no hidden retry;
* no support endpoint embedded as universal destination;
* and no provider access to the bundle.

A future explicit upload should require:

* Network Gateway;
* exact destination;
* TLS policy;
* destination identity;
* data-sharing preview;
* user approval;
* upload receipt;
* retry policy;
* and deletion policy.

That future upload requires a separate accepted decision or amendment.

The initial bundle staging location should be:

```text
%LOCALAPPDATA%\Opure\<Channel>\Support\Staging\<bundle-id>\
```

The exported bundle belongs at the user-selected location.

The initial staging policy should:

* use restrictive user ACLs;
* deny plugins and MCP servers;
* exclude ordinary indexing;
* exclude Project Knowledge;
* exclude Project Memory;
* exclude cloud sync where Opure can detect it;
* delete after export or 24 hours;
* and preserve only under explicit hold.

The initial support-bundle generation state machine should be:

* Draft;
* Planning;
* Approval Required;
* Collecting;
* Redacting;
* Scanning;
* Preview Ready;
* Export Approved;
* Writing;
* Verifying;
* Exported;
* Failed;
* Cancelled;
* Quarantined;
* Expired;
* and Deleted.

Cancellation should:

* stop new collection;
* cancel traces;
* stop diagnostic tools;
* close staging files;
* delete incomplete outputs where safe;
* and record a safe receipt.

The initial evidence-access model should use capabilities.

Views and exports should be scoped by:

* current user;
* project;
* evidence class;
* sensitivity;
* incident;
* operation;
* and purpose.

Plugins should not receive general Trust Centre query access.

A plugin may submit bounded self-diagnostic events through Plugin Host under a specific capability.

Those events should be:

* namespaced;
* schema validated;
* size limited;
* rate limited;
* classified;
* and displayed as plugin-provided observations rather than first-party authority.

MCP servers should not submit Trust Evidence directly.

The MCP Gateway should generate receipts for MCP interactions.

The initial Trust Centre search should use:

* typed filters;
* indexed safe attributes;
* time range;
* project;
* operation;
* category;
* outcome;
* authority;
* and text over approved safe message fields.

No raw SQL or arbitrary expression should be accepted.

The initial Trust Centre query result should bind:

* snapshot time;
* query filters;
* source freshness;
* owner availability;
* omitted sensitive fields;
* and result count.

A Trust Centre view should show when an owner service is unavailable or evidence is incomplete.

The initial evidence-reconciliation process should:

1. inspect expected owner sequences;
2. identify gaps;
3. request owner records by safe ID;
4. verify owner record hash;
5. ingest missing evidence;
6. record irreconcilable gap;
7. and expose evidence completeness.

A record conflict should quarantine the conflicting projection.

The Trust Centre should not silently replace one hash with another.

The initial evidence-completeness states should be:

* Complete;
* Complete for Requested Scope;
* Projection Delayed;
* Owner Unavailable;
* Gap Detected;
* Owner Record Missing;
* Conflict;
* Purged by Policy;
* Redacted;
* and Unknown.

The selected trust chain is:

```text
Authoritative owner-service transaction
    ↓
Owner record and transactional outbox
    ↓
Typed Trust Evidence Record
    ↓
Schema, authority and hash validation
    ↓
Trust Evidence inbox and immutable projection
    ↓
Retention and preservation policy
    ↓
Developer-visible Trust Centre
    ↓
Explicit Support Bundle Plan
    ↓
Bounded collection, redaction and secret scanning
    ↓
File-by-file preview and approval
    ↓
Verified local export
```

---

## 2. Status

Current status:

> **Proposed**

This ADR becomes **Accepted** only after prototypes demonstrate:

* owner-service authoritative records;
* transactional evidence outboxes;
* Trust Evidence ingestion;
* schema validation;
* evidence deduplication;
* per-stream hash chains;
* projection rebuild;
* cross-service correlation;
* evidence relationships;
* evidence completeness;
* source reconciliation;
* Trust Centre views;
* operational-log separation;
* OpenTelemetry-compatible records;
* low-cardinality telemetry;
* data classification;
* default retention;
* retention decisions;
* deletion dependency checks;
* Preservation Holds;
* incident evidence copies;
* access records;
* Minimal and Standard bundles;
* EventPipe performance collection;
* crash triage collection;
* dump-risk warnings;
* strict bundle staging;
* redaction;
* secret canaries;
* pattern scanning;
* path and identity pseudonymisation;
* source-content opt-in;
* prompt and output opt-in;
* file-by-file preview;
* ZIP generation and verification;
* no automatic upload;
* cancellation;
* staging expiry;
* project and channel isolation;
* and adversarial forged-evidence, owner-gap, log-injection, secret-leak, dump, archive-path, symlink, case-collision, oversized-bundle, stale-approval, plugin-evidence, MCP-evidence, hold-bypass and deletion-race tests.

---

## 3. Context

Opure deliberately exposes important decisions through:

* Trust Centre;
* operation receipts;
* workflow history;
* Data Sharing Plans;
* Routing Decisions;
* tool approvals;
* patch reviews;
* and configuration provenance.

Without a common evidence architecture, each service may:

* use different record schemas;
* store different time fields;
* retain data indefinitely;
* duplicate source content;
* confuse logs with decisions;
* hide missing evidence;
* produce unsafe support bundles;
* or upload diagnostics without sufficient review.

Operational logs are valuable for:

* diagnosing failures;
* identifying timing;
* measuring performance;
* and correlating services.

Operational logs are not sufficient proof of every important action because logs can be:

* sampled;
* disabled;
* rotated;
* malformed;
* delayed;
* duplicated;
* or emitted before a transaction fails.

Authoritative service records should therefore exist independently.

Support bundles create another risk.

A naïve support bundle may include:

* source code;
* project names;
* user paths;
* machine identity;
* command lines;
* environment variables;
* model prompts;
* model outputs;
* provider credentials;
* private keys;
* process memory;
* and unrelated operating-system logs.

A process dump may contain nearly any memory-resident information.

A trace can contain payloads if event providers are configured carelessly.

A support export must therefore be:

* explicit;
* scoped;
* bounded;
* classified;
* redacted;
* scanned;
* previewed;
* and locally controlled.

Retention also creates tension.

Too little evidence can make:

* recovery;
* security investigation;
* billing reconciliation;
* workflow ambiguity;
* and product support

impossible.

Too much evidence can increase:

* privacy risk;
* secret exposure;
* storage use;
* attack surface;
* and user distrust.

The architecture must retain the minimum justified evidence for each purpose and extend retention only through explicit dependencies or holds.

---

## 4. Problem Statement

Opure requires a local-first trust-evidence and supportability architecture that preserves authoritative provenance, separates trust records from telemetry, correlates cross-service actions, enforces purpose-specific retention and deletion, supports incident preservation, and produces bounded redacted support bundles only after complete developer review without claiming impossible tamper-proof, secret-free or forensic-erasure guarantees.

---

## 5. Decision Drivers

The decision is evaluated against:

* Charter alignment;
* developer visibility;
* human control;
* local-first operation;
* authoritative provenance;
* service ownership;
* privacy;
* secret safety;
* security investigation;
* workflow recovery;
* diagnostic usefulness;
* retention discipline;
* deletion transparency;
* supportability;
* dump risk;
* export control;
* archive safety;
* incident response;
* performance;
* accessibility;
* testability;
* and small-team implementation.

---

## 6. Governing Principles

This decision must preserve:

* Developer Respect;
* Human in Control;
* Local by Design;
* Visible by Design;
* Owner Services Remain Authoritative;
* Trust Centre Is a Projection;
* Evidence Is Not Telemetry;
* Telemetry Is Not Authority;
* Support Bundles Are Derived Exports;
* No Automatic Upload;
* No Secret by Default;
* No Source by Default;
* No Prompt or Model Output by Default;
* No Dump by Default;
* No Hidden Collection;
* No Hidden Retention;
* Purpose Before Collection;
* Minimum Necessary Evidence;
* Retention by Class and Purpose;
* Holds Are Explicit;
* Deletion Is Explainable;
* Hash Chains Are Not External Proof;
* Clock Time Is Not Trusted Ordering;
* Redaction Is Layered;
* Secret Scanning Is Not a Guarantee;
* Binary Dumps Are High Risk;
* Preview Before Export;
* Staging Is Short Lived;
* Exported Copies Leave Opure Control;
* and Fail Closed for Sensitive Export.

---

## 7. Scope

This ADR decides:

* Trust Evidence Service ownership;
* owner-service evidence contracts;
* Evidence Record envelopes;
* Authority Classes;
* evidence types;
* evidence relationships;
* evidence correlation;
* Trust Centre projections;
* evidence completeness;
* integrity evidence;
* clock and ordering;
* operational telemetry separation;
* logs, traces and metrics;
* retention;
* deletion;
* Preservation Holds;
* incident evidence;
* support-bundle presets;
* support-bundle format;
* diagnostic collection;
* dumps;
* EventPipe;
* GC dumps;
* system inventory;
* redaction;
* secret scanning;
* preview;
* export;
* staging;
* bundle retention;
* access control;
* Trust Centre UI;
* and acceptance tests.

This ADR does not decide:

* a public cloud telemetry backend;
* automatic support upload;
* encrypted support-bundle format;
* external trusted timestamping;
* external audit signing;
* SIEM integration;
* court-ready evidence management;
* legal discovery;
* full enterprise incident platform;
* remote support access;
* remote desktop support;
* Windows Error Reporting upload;
* or general operating-system log collection.

---

## 8. Constraints

Known constraints include:

* ADR-0005 selected service-owned SQLite and transactional outbox/inbox.
* ADR-0006 selected local-first structured logs, traces and metrics with strict redaction and no export by default.
* ADR-0007 keeps secret values in the Vault.
* ADR-0009 governs paths, staged writes and archive safety.
* ADR-0018 and ADR-0019 govern tools, MCP, network and provider data sharing.
* ADR-0021 requires immutable Context Plans.
* ADR-0023 governs Project Memory provenance and retention.
* ADR-0024 governs AI evaluation and Routing Decisions.
* ADR-0025 governs durable workflow events, side effects and recovery.
* ADR-0026 governs effective configuration, policy and retention settings.
* OpenTelemetry's Logs Data Model distinguishes occurrence and observed timestamps and supports trace correlation;
* .NET diagnostic tooling can collect traces, logs, metrics and dumps;
* .NET diagnostic ports can expose dumps, EventPipe traces and process command lines;
* .NET crash dump types have materially different privacy and size characteristics;
* process dumps can contain memory-resident data;
* NIST guidance treats log management as an ongoing infrastructure and process concern;
* NIST incident-response guidance integrates response into cybersecurity risk management;
* NIST digital-evidence guidance emphasises preservation and integrity;
* and current UK ICO guidance emphasises data minimisation and retention only for as long as needed for the purpose.

---

## 9. Assumptions

This decision assumes:

* owner services can produce typed receipts;
* owner services can support evidence lookup by stable ID;
* Trust Evidence can ingest through local IPC;
* most support questions can be diagnosed without source or process memory;
* EventPipe providers can be allowlisted;
* secret canaries can be seeded in test fixtures;
* bundle generation can run in a supervised process;
* users can select a local export destination;
* and enterprise administrators can define stricter retention and export policy through ADR-0026.

---

## 10. Current Technical Evidence

Official and primary documentation available on 18 July 2026 establishes that:

* OpenTelemetry's stable Logs Data Model defines occurrence time, observed time, trace and span context, severity, body, resource, instrumentation scope, attributes and event name;
* OpenTelemetry uses the logs data model to represent first-party, third-party and system logs in a common structured form;
* .NET's diagnostics client can request EventPipe traces and process dumps;
* the .NET diagnostic port can also expose process command-line information;
* `dotnet-monitor` can collect dumps, traces, logs and metrics on demand or through collection rules;
* .NET crash-dump configuration includes Mini, Heap, Triage and Full dump types;
* current Microsoft documentation describes Triage as a Mini-like dump intended to remove personal information such as paths and passwords;
* NIST SP 800-92 provides enterprise log-management guidance;
* NIST SP 800-61 Revision 3 integrates incident response throughout cybersecurity risk management;
* NIST digital-evidence guidance discusses preservation, integrity and disposition challenges;
* NIST Privacy Framework is a voluntary risk-management framework, not a legal mandate;
* and current ICO guidance states that personal data should be adequate, relevant and limited to what is necessary, and should not be retained longer than needed.

These facts support:

* structured local telemetry;
* separate authoritative receipts;
* explicit diagnostic collection;
* high-risk dump treatment;
* purpose-specific retention;
* incident preservation;
* and data-minimised support bundles.

They do not prove that a dump is secret free, that a hash chain is tamper proof or that one retention period satisfies every legal or organisational requirement.

All external guidance, .NET tools, OpenTelemetry specifications and privacy requirements must be revalidated before acceptance.

---

## 11. Terminology

### 11.1 Authoritative Domain Record

A record owned by the service that made a decision or performed an effect.

---

### 11.2 Trust Evidence Record

A typed receipt or safe reference used to explain an authoritative or observed event.

---

### 11.3 Trust Centre Projection

A rebuildable read model over Trust Evidence.

---

### 11.4 Authority Class

The strength and source of an evidence claim.

---

### 11.5 Operational Telemetry

Logs, traces and metrics used for diagnosis and performance.

---

### 11.6 Diagnostic Artefact

A bounded collected file such as EventPipe trace, GC dump or process dump.

---

### 11.7 Evidence Relationship

A typed link between evidence records.

---

### 11.8 Evidence Completeness

The known coverage and gaps for a requested scope.

---

### 11.9 Retention Policy

A versioned rule that determines how long evidence remains.

---

### 11.10 Retention Decision

The exact calculated expiry and reason for one record or artefact.

---

### 11.11 Preservation Hold

An explicit instruction that suspends ordinary deletion for selected evidence.

---

### 11.12 Preserved Incident Evidence

A restricted immutable copy or owner-bound reference retained for an incident.

---

### 11.13 Support Bundle Definition

A versioned preset describing eligible support evidence and diagnostics.

---

### 11.14 Support Bundle Plan

The exact approved collection and export plan for one bundle.

---

### 11.15 Support Bundle

A locally generated redacted ZIP archive.

---

### 11.16 Redaction

A recorded transformation that removes or substitutes sensitive content.

---

### 11.17 Secret Scan

A best-effort set of deterministic and heuristic checks for sensitive material.

---

### 11.18 Bundle-Local Pseudonym

A replacement identifier stable only within one bundle.

---

### 11.19 Owner Reconciliation

A process that verifies or repairs a Trust Centre projection from the authoritative owner.

---

### 11.20 Local Integrity Evidence

Hashes and chains useful for detecting corruption or inconsistency without external non-repudiation.

---

## 12. Options Considered

The principal architecture options are:

1. Federated owner authority with Trust Evidence projections.
2. One central audit database as universal authority.
3. Operational logs as the sole Trust Centre source.
4. Full duplication of every owner database.
5. Windows Event Log as the universal evidence store.
6. Automatic cloud telemetry and support upload.
7. On-demand local support bundles with no Trust Centre.
8. Unredacted process dumps for every crash.
9. General SIEM integration as the initial solution.
10. External cryptographic ledger.
11. Plain directory support exports.
12. Standard ZIP support bundles with explicit review.

---

## 13. Option A — Federated Owner Authority

### 13.1 Advantages

* correct service ownership;
* no central authority duplication;
* transactional domain records;
* rebuildable Trust Centre;
* local operation;
* typed evidence;
* retention by purpose;
* and bounded support exports.

### 13.2 Disadvantages

* cross-service schemas;
* reconciliation;
* projection lag;
* retention dependencies;
* and more contracts.

### 13.3 Decision

Selected.

---

## 14. Central Universal Audit Database

Rejected because it would duplicate authority and create cross-database transaction claims that Opure cannot guarantee.

A central projection remains selected.

---

## 15. Operational Logs as Authority

Rejected because logs may be sampled, rotated, disabled or emitted outside the authoritative transaction.

---

## 16. Full Database Duplication

Rejected because it increases sensitive-data duplication, coupling and retention complexity.

---

## 17. Windows Event Log

Useful for selected product health events in future, but rejected as the universal authority because structured project evidence, local retention and bundle redaction require product-owned contracts.

---

## 18. Automatic Cloud Upload

Rejected because Opure is local first and support data may contain highly sensitive material.

---

## 19. Bundles Without Trust Centre

Insufficient because developers need routine visibility before a support event.

---

## 20. Automatic Crash Dumps

Rejected because dumps are high-risk, potentially large and can contain memory-resident sensitive data.

---

## 21. SIEM Integration

Deferred.

Enterprise export can be considered later through explicit schemas and policy.

---

## 22. External Cryptographic Ledger

Deferred because it adds infrastructure and still does not solve source truth, privacy or correct data minimisation.

---

## 23. Plain Directory Export

Useful for Development but not selected as the ordinary user artefact because a verified single file is easier to move and inventory.

---

## 24. ZIP Bundle

Selected as a standard unencrypted local container with strict archive rules, complete manifest, short staging retention and explicit warning.

---

## 25. Decision

Opure will provisionally adopt:

> **A local federated Trust Evidence architecture in which every domain service remains authoritative for its transactional decisions and side effects, publishes typed immutable receipts through an outbox, and can reconcile a read-only Trust Centre projection that distinguishes authoritative records, derived evidence, operational telemetry, diagnostic artefacts and user assertions; purpose- and classification-specific retention, explicit Preservation Holds and transparent deletion govern those records; and support bundles are generated only from versioned local plans through bounded owner-service collection, layered redaction, secret scanning, file-by-file preview, explicit approval, strict verified ZIP creation and short-lived staging, while source, prompts, model output, command output and process dumps remain excluded by default, no bundle is uploaded automatically, exported copies leave Opure's control, and hashes are described honestly as local integrity evidence rather than tamper-proof or legal proof.**

This decision is:

* [ ] Permanent until superseded
* [x] Provisional pending evidence, retention, dump, redaction and bundle-security prototypes
* [ ] Approval of automatic telemetry export
* [ ] Approval of automatic support upload
* [ ] Approval of court-ready evidence claims
* [ ] Approval of full crash dumps by default

---

# 26. Trust Evidence Service Ownership

The Trust Evidence Service owns:

* Evidence Type registration;
* schema validation;
* evidence ingestion;
* evidence deduplication;
* safe evidence indexing;
* relationship indexing;
* evidence completeness;
* owner reconciliation;
* retention decisions;
* Preservation Holds;
* support-bundle definitions;
* support-bundle plans;
* diagnostic collection coordination;
* redaction;
* secret scanning;
* bundle staging;
* manifest creation;
* archive verification;
* export receipts;
* and Trust Centre projections.

---

## 26.1 Non-Responsibilities

The Trust Evidence Service does not:

* authorise domain actions;
* execute commands;
* apply patches;
* access Vault values;
* select models;
* approve providers;
* mutate workflows;
* mutate project source;
* grant plugin permissions;
* grant MCP permissions;
* or upload support bundles automatically.

---

# 27. Service Boundary

Conceptual architecture:

```text
Authoritative Owner Service
    ├── Domain Transaction
    ├── Domain Record
    └── Transactional Evidence Outbox
            ↓
Trust Evidence Service
    ├── Schema Registry
    ├── Evidence Inbox
    ├── Integrity Validation
    ├── Correlation
    ├── Relationships
    ├── Projection Builder
    ├── Retention Engine
    ├── Preservation Manager
    ├── Support Bundle Planner
    ├── Diagnostic Collector
    ├── Redaction and Secret Scan
    └── Bundle Writer
            ↓
Trust Centre and Explicit Local Export
```

---

## 27.1 Owner Service Authority

The owner record remains authoritative.

---

## 27.2 Projection Lag

Trust Centre may lag without changing domain truth.

---

## 27.3 No Reverse Mutation

Trust Centre commands route to the owning service.

---

## 27.4 No Shared Write Database

Owner services do not write the Trust Evidence database directly.

---

# 28. Trust Evidence Database

Suggested location:

```text
%LOCALAPPDATA%\Opure\<Channel>\Trust\Evidence\evidence.db
```

Associated directories:

```text
Trust\
├── Evidence\
│   ├── evidence.db
│   ├── artefacts\
│   ├── quarantine\
│   ├── preservation\
│   └── recovery\
└── Support\
    ├── staging\
    ├── reports\
    └── quarantine\
```

---

## 28.1 Channel Isolation

Stable, Preview and Development use separate stores.

---

## 28.2 Local Fixed Disk

Required.

---

## 28.3 Repository Storage

Denied.

---

## 28.4 Network Share

Denied initially.

---

# 29. Suggested Tables

```text
evidence_type_definitions
evidence_type_revisions
evidence_records
evidence_record_attributes
evidence_payload_references
evidence_relationships
evidence_ingestion_events
evidence_owner_streams
evidence_owner_gaps
evidence_conflicts
evidence_completeness
evidence_retention_policies
evidence_retention_decisions
evidence_deletion_plans
evidence_deletion_receipts
preservation_holds
preservation_hold_selectors
preserved_evidence
preservation_access_events
incidents
incident_evidence_links
support_bundle_definitions
support_bundle_plans
support_bundle_plan_items
support_bundle_collection_items
support_bundle_redactions
support_bundle_scan_findings
support_bundle_manifests
support_bundle_exports
support_bundle_deletions
trust_projection_checkpoints
trust_outbox
trust_inbox
trust_integrity_results
trust_tombstones
```

---

## 29.1 Full Owner Payload

Not copied by default.

---

## 29.2 CAS

Used for selected immutable artefacts.

---

## 29.3 Secret Values

Prohibited.

---

# 30. Evidence Type Definition

Suggested schema:

```text
opure.trust-evidence-type/1
```

---

## 30.1 Fields

```text
evidence_type
revision
owner_service
description
authority_class
record_schema
payload_schema
safe_index_fields
sensitive_fields
relationship_types
default_retention_class
support_bundle_eligibility
redaction_profile
owner_lookup_contract
owner_reconciliation_contract
created_at
definition_sha256
```

---

## 30.2 Immutable Revision

Required.

---

## 30.3 Stable Evidence Type

Use dotted lowercase IDs.

Examples:

```text
ai.routing.decision
ai.provider.request-receipt
workflow.effect.intent
workflow.effect.outcome-unknown
tool.execution.receipt
patch.application.receipt
configuration.snapshot.committed
security.secret-use.receipt
support.bundle.exported
```

---

## 30.4 Dynamic Type

Plugins may not create first-party authority types.

---

# 31. Evidence Record Schema

Suggested schema:

```text
opure.trust-evidence-record/1
```

---

## 31.1 Required Core Fields

```text
evidence_id
schema
evidence_type
evidence_type_revision
owner_service
owner_record_id
owner_record_revision
authority_class
occurred_at_utc
observed_at_utc
record_sha256
```

---

## 31.2 Conditional Fields

Project and operation fields required where the evidence belongs to them.

---

## 31.3 Payload

Inline only below a bounded threshold and only when approved by the Evidence Type.

---

## 31.4 Maximum Inline Payload

Suggested:

```text
64 KiB
```

---

# 32. Evidence Identity

Evidence IDs should be opaque random identifiers.

---

## 32.1 Owner Record ID

Opaque owner identifier.

---

## 32.2 Deduplication

Use:

* owner service;
* owner record ID;
* owner record revision;
* evidence type;
* and payload hash.

---

## 32.3 Duplicate Same Record

Idempotent.

---

## 32.4 Duplicate Different Payload

Conflict and quarantine.

---

# 33. Authority Class Semantics

## 33.1 Authoritative Domain Decision

Example:

* Routing Decision;
* configuration commit;
* capability decision.

---

## 33.2 Authoritative Domain Effect

Example:

* patch applied;
* file written;
* command launched;
* provider call completed.

---

## 33.3 Authoritative State Transition

Example:

* workflow step completed;
* plugin quarantined;
* update hand-off started.

---

## 33.4 Verified Service Receipt

A trusted service attests to its own completed operation.

---

## 33.5 Verified External Receipt

A trusted adapter has validated an external receipt.

---

## 33.6 Deterministic Validation Result

Build, test, hash, schema or policy evidence.

---

## 33.7 Human Decision

Approval, rejection, recovery decision or manual reconciliation.

---

## 33.8 Derived Trust Projection

Summary or aggregation.

---

## 33.9 Operational Observation

Log, trace or metric observation.

---

## 33.10 Diagnostic Observation

Dump, trace or diagnostic-tool result.

---

## 33.11 Model Proposal

Never authoritative by itself.

---

## 33.12 User Assertion

Visible as an assertion.

---

## 33.13 Imported Evidence

Historical and source labelled.

---

## 33.14 Unknown

Cannot support a critical decision.

---

# 34. Evidence Outcome

Suggested values:

* Requested;
* Proposed;
* Approved;
* Denied;
* Started;
* Succeeded;
* Succeeded with Warnings;
* Failed;
* Cancelled;
* Timed Out;
* Outcome Unknown;
* Reconciled;
* Compensated;
* Revoked;
* Superseded;
* Deleted;
* Preserved;
* and Unknown.

---

## 34.1 Domain Vocabulary

An Evidence Type may provide more specific sub-outcomes.

---

# 35. Actor

Actor kinds:

* Developer;
* Enterprise Administrator;
* Runtime;
* Trusted Service;
* Worker;
* Plugin;
* MCP Server;
* Provider;
* Local Model;
* Imported Source;
* and Unknown.

---

## 35.1 Actor Identity

Use opaque local identity and role.

---

## 35.2 No Email by Default

Required.

---

# 36. Subject

A bounded typed resource reference.

Examples:

* project;
* workflow;
* provider profile;
* model profile;
* plugin package;
* MCP server;
* patch;
* tool;
* configuration snapshot;
* and support bundle.

---

# 37. Occurred and Observed Time

Both UTC.

---

## 37.1 Missing Occurred Time

Use observed time and mark source time unavailable.

---

## 37.2 Clock Skew

Record observed difference.

---

## 37.3 Ordering

Sequence beats timestamp.

---

# 38. Owner Sequence

Each owner stream should include a monotonic sequence where practical.

---

## 38.1 Stream Identity

Examples:

* service;
* project;
* workflow;
* operation;
* or evidence category.

---

## 38.2 Gap Detection

Expected sequence comparison.

---

## 38.3 Wraparound

Use 64-bit monotonic value and explicit rollover policy.

---

# 39. Runtime Boot Identity

One opaque ID per Runtime start.

---

## 39.1 Process Identity

Use supervisor identity beyond PID.

---

## 39.2 Reuse

Never across boot.

---

# 40. Evidence Payload Reference

Possible locations:

* Owner Inline;
* Owner Database;
* Owner CAS;
* Trust Inline;
* Trust CAS;
* Preserved Copy;
* External Receipt Metadata;
* and Deleted.

---

## 40.1 Location Change

Create a payload-reference revision without rewriting domain meaning.

---

## 40.2 Owner Deletion

Trust Centre shows unavailable.

---

# 41. Record Hash

Canonical hash includes:

* schema;
* evidence type;
* owner;
* owner record;
* authority;
* scope;
* actor;
* subject;
* action;
* outcome;
* occurrence time;
* sequence;
* payload hash;
* and previous stream hash.

---

## 41.1 Non-Semantic Display Fields

Excluded.

---

# 42. Owner Stream Hash Chain

Optional per Evidence Type.

---

## 42.1 Genesis

Defined stream seed.

---

## 42.2 Verification

Tail on ingestion.

---

## 42.3 Gap

Hash chain cannot be advanced without missing record.

---

## 42.4 Reset

Requires explicit stream-reset evidence.

---

# 43. Hash-Chain Limitations

Hash chains can detect:

* accidental modification;
* missing local records;
* reordering;
* and inconsistent owner projections.

They cannot guarantee:

* who originally created the record;
* that a fully compromised owner did not forge it;
* that local time is accurate;
* or that an external recipient saw the same chain.

---

# 44. Evidence Ingestion

Flow:

1. receive owner outbox message;
2. authenticate service session;
3. validate owner identity;
4. validate Evidence Type revision;
5. validate envelope;
6. verify payload hash;
7. verify owner sequence;
8. verify previous stream hash where used;
9. apply data classification;
10. deduplicate;
11. store record;
12. update relationships and indexes;
13. update completeness;
14. create retention decision;
15. acknowledge inbox;
16. commit.

---

## 44.1 Message Too Large

Reject and require owner payload reference.

---

## 44.2 Unknown Type

Quarantine.

---

## 44.3 Owner Mismatch

Security incident.

---

# 45. Owner Outbox Contract

Outbox record contains:

```text
message_id
owner_service
evidence_type
owner_record_id
owner_record_revision
payload_reference
payload_sha256
available_at
attempt_count
```

---

## 45.1 Owner Transaction

Domain record and outbox commit together.

---

## 45.2 Delivery

At least once.

---

## 45.3 Trust Inbox

Deduplicates.

---

# 46. Trust Inbox

Stores:

* message ID;
* owner;
* hash;
* processing state;
* and result.

---

## 46.1 Duplicate Same Hash

Return original result.

---

## 46.2 Duplicate Different Hash

Quarantine.

---

# 47. Evidence Relationships

Suggested schema:

```text
opure.trust-evidence-relationship/1
```

---

## 47.1 Fields

```text
relationship_id
source_evidence
relationship_type
target_evidence_or_subject
owner_service
authority_class
created_at
relationship_sha256
```

---

## 47.2 Relationship Authority

Owner of the relationship declares it.

---

## 47.3 Derived Relationship

Marked derived.

---

# 48. Relationship Validation

Check:

* source exists;
* target exists or is a valid subject;
* relationship type allowed;
* owner authorised;
* no cross-project leak;
* and no impossible authority escalation.

---

# 49. Causal Graph

Trust Centre may display a graph such as:

```text
Developer Request
    ↓
Context Plan
    ↓
Routing Decision
    ↓
Data Sharing Approval
    ↓
Provider Request
    ↓
Model Response
    ↓
Patch Proposal
    ↓
Developer Approval
    ↓
Patch Application
    ↓
Build and Test
```

---

## 49.1 Graph Is a Projection

Required.

---

## 49.2 Missing Edge

Visible.

---

# 50. Evidence Completeness

Suggested schema:

```text
opure.trust-evidence-completeness/1
```

---

## 50.1 Fields

```text
scope
requested_types
owner_services
expected_sequences
observed_sequences
gaps
redactions
purged_records
unavailable_owners
state
calculated_at
```

---

## 50.2 Complete

Only for defined scope.

---

## 50.3 Never “All Evidence”

Unless scope is explicitly bounded.

---

# 51. Owner Reconciliation Contract

An owner service should support:

* lookup by owner record ID;
* lookup by sequence range;
* lookup by operation;
* record hash;
* payload hash;
* retention state;
* and deletion state.

---

## 51.1 Query Capability

Trust Evidence Service only.

---

## 51.2 Bounded Results

Required.

---

# 52. Reconciliation Flow

1. detect gap or conflict;
2. create reconciliation request;
3. query owner;
4. verify service identity;
5. compare hashes;
6. ingest missing records;
7. repair projection;
8. or preserve conflict evidence;
9. update completeness.

---

## 52.1 Owner Missing Record

Do not invent.

---

## 52.2 Owner Says Deleted

Record safe deletion state.

---

# 53. Projection Checkpoint

Trust projections may use checkpoints.

---

## 53.1 Derived

Discardable.

---

## 53.2 Authority

None.

---

## 53.3 Validation

Bind ingestion sequence and database state.

---

# 54. Trust Centre Query

Suggested schema:

```text
opure.trust-query/1
```

---

## 54.1 Filters

* channel;
* project;
* operation;
* workflow;
* evidence type;
* category;
* authority;
* outcome;
* time range;
* service;
* preservation state;
* and free text over safe fields.

---

## 54.2 Maximum Range

Bounded.

---

## 54.3 Pagination

Cursor based.

---

## 54.4 Raw SQL

Denied.

---

# 55. Query Snapshot

Results bind:

* query;
* calculated time;
* projection generation;
* owner freshness;
* result count;
* redactions;
* and completeness.

---

# 56. Trust Centre Overview

Show:

* active operations;
* recent external data sharing;
* recent writes and commands;
* unresolved workflow effects;
* policy conflicts;
* revoked capabilities;
* active Preservation Holds;
* pending retention deletion;
* recent support exports;
* and evidence-health warnings.

---

## 56.1 No Fear Dashboard

Use factual language.

---

# 57. Project View

Show:

* project policy;
* providers used;
* models used;
* tools;
* commands;
* patches;
* workflows;
* memory changes;
* plugin and MCP interactions;
* and retention.

---

## 57.1 Cross-Project Isolation

Hard.

---

# 58. Operation View

Show causal timeline.

---

## 58.1 Exact Plans

Link:

* Context Plan;
* Routing Decision;
* Data Sharing Plan;
* Tool Call Plan;
* approval;
* and result.

---

# 59. AI and Model View

Show:

* local or remote;
* Provider Profile;
* model identity;
* resolved model;
* context size;
* output size;
* token use;
* cost estimate and actual;
* fallback;
* and errors.

---

## 59.1 Prompt

Excluded unless selected.

---

## 59.2 Response

Excluded unless selected.

---

# 60. Data Sharing View

Show:

* destination;
* provider;
* region;
* data classes;
* size;
* purpose;
* approval;
* and receipt.

---

# 61. Secret Use View

Show:

* Vault reference alias where safe;
* consuming service;
* operation;
* destination class;
* time;
* outcome;
* and policy.

---

## 61.1 Secret Value

Never.

---

## 61.2 Secret Name

May be masked.

---

# 62. Workflow View

Show:

* plan;
* timeline;
* steps;
* attempts;
* approvals;
* side effects;
* compensation;
* recovery;
* and Outcome Unknown.

---

# 63. Tool and Command View

Show:

* tool;
* command family;
* working directory class;
* arguments with redaction;
* capability;
* approval;
* exit class;
* and effects.

---

## 63.1 Full Command Line

Not by default.

---

# 64. Patch and Write View

Show:

* patch ID;
* source base;
* files;
* line count;
* approval;
* application receipt;
* validation;
* and rollback or compensation.

---

# 65. Plugin View

Show:

* package ID;
* version;
* hash;
* trust;
* permissions;
* host;
* calls;
* denials;
* and crashes.

---

# 66. MCP View

Show:

* server fingerprint;
* account reference;
* tool or resource;
* data class;
* approval;
* result class;
* and errors.

---

# 67. Configuration View

Show:

* Effective Snapshot;
* requested versus effective;
* policies;
* conflicts;
* activation;
* and change history.

---

# 68. Update View

Show:

* check;
* metadata source;
* selected package;
* signature state;
* installer hand-off;
* result;
* and recovery.

---

# 69. Operational Log Store

Suggested location:

```text
%LOCALAPPDATA%\Opure\<Channel>\Diagnostics\Logs\
```

---

## 69.1 Rotation

By size and time.

---

## 69.2 Format

Structured JSON Lines or binary local representation with deterministic export.

---

## 69.3 Access

Current user and trusted diagnostics service.

---

## 69.4 No Trust Authority

Required.

---

# 70. Log Record Schema

Suggested schema:

```text
opure.operational-log-record/1
```

---

## 70.1 Fields

```text
timestamp
observed_timestamp
trace_id
span_id
severity_number
severity_text
event_name
service
service_version
runtime_boot_id
body
attributes
classification
redaction_state
```

---

## 70.2 Event Name

Stable dotted identifier.

---

## 70.3 Body

Short safe description.

---

# 71. Severity Mapping

Use OpenTelemetry-compatible severity ranges.

---

## 71.1 Trace

Fine detail.

---

## 71.2 Debug

Diagnostic detail.

---

## 71.3 Information

Expected significant event.

---

## 71.4 Warning

Unexpected condition with continued operation.

---

## 71.5 Error

Operation failed.

---

## 71.6 Fatal

Service or application cannot safely continue.

---

# 72. Log Attributes

Allowed:

* stable error code;
* component;
* operation class;
* result class;
* duration;
* retry count;
* and safe opaque IDs.

---

## 72.1 High Cardinality

Bounded.

---

## 72.2 Project ID

May appear in local logs only under hashed or opaque form and should not become exported by default.

---

# 73. Log Injection

Structured serialization prevents line injection.

---

## 73.1 Untrusted Text

Store as value, not event-name or attribute key.

---

## 73.2 Control Characters

Escape.

---

## 73.3 Multiline

Bound and mark.

---

# 74. Log Redaction

Occurs before persistence.

---

## 74.1 Post-Persistence Scan

Periodic canary scan.

---

## 74.2 Redaction Failure

Drop sensitive field or event according to severity and policy.

---

# 75. Trace Store

Suggested:

```text
%LOCALAPPDATA%\Opure\<Channel>\Diagnostics\Traces\
```

---

## 75.1 Ordinary Traces

Bounded sampled local traces.

---

## 75.2 Diagnostic Traces

Explicit sessions.

---

## 75.3 Payload

No full payload.

---

# 76. Trace Attributes

Allowed:

* service;
* operation;
* activity kind;
* status;
* duration;
* queue duration;
* result class;
* model local or remote;
* and safe resource class.

---

## 76.1 File Path

Excluded or templated.

---

## 76.2 Provider Request ID

Owner receipt only unless safe.

---

# 77. Metrics Store

Suggested:

```text
%LOCALAPPDATA%\Opure\<Channel>\Diagnostics\Metrics\
```

---

## 77.1 Local Aggregation

Default.

---

## 77.2 Export

Disabled.

---

## 77.3 Cardinality Budget

Enforced.

---

# 78. Metric Dimensions

Examples:

* service;
* operation class;
* result class;
* local or remote;
* performance mode;
* and channel.

---

## 78.1 Prohibited Dimensions

* project ID;
* user ID;
* file path;
* workflow ID;
* request ID;
* provider request ID;
* and plugin call ID.

---

# 79. Telemetry Session

Suggested schema:

```text
opure.diagnostic-session/1
```

---

## 79.1 Fields

```text
session_id
purpose
targets
providers
metrics
duration
buffer_limit
output_limit
approval
started_at
completed_at
state
artefacts
```

---

# 80. Diagnostic Session States

* Draft;
* Approval Required;
* Ready;
* Running;
* Cancelling;
* Completed;
* Failed;
* Expired;
* and Deleted.

---

# 81. Diagnostic Collector Process

A supervised trusted process.

---

## 81.1 Permissions

Only requested target and artefact type.

---

## 81.2 Diagnostic Port

Exact process and boot identity.

---

## 81.3 Plugins

No access.

---

## 81.4 MCP

No access.

---

# 82. EventPipe Collection

Allowlisted providers.

---

## 82.1 Suggested Default Providers

* .NET runtime counters;
* Opure ActivitySource and Meter;
* selected TPL events where needed;
* and selected GC events.

Exact provider keywords require evidence.

---

## 82.2 Buffer

Bounded.

---

## 82.3 Duration

Bounded.

---

## 82.4 Cancellation

Required.

---

# 83. Trace Side Effects

Collection may:

* consume CPU;
* allocate buffers;
* alter timing;
* and increase disk use.

The preview should state this.

---

# 84. GC Dump

Collection triggers diagnostic GC behaviour.

---

## 84.1 Approval

Explicit.

---

## 84.2 Target

Exact process.

---

## 84.3 Retention

Short.

---

# 85. Process Dump Types

## 85.1 Mini

Small metadata and selected process information.

---

## 85.2 Triage

Preferred first diagnostic dump where supported.

---

## 85.3 Heap

Contains extensive managed memory.

High risk.

---

## 85.4 Full

Contains all memory and images.

Highest risk.

---

# 86. Dump Approval

Show:

* process;
* dump type;
* estimated size;
* possible data;
* retention;
* inclusion status;
* and destination.

---

## 86.1 Separate Approval

A dump approval is separate from ordinary Standard bundle approval.

---

# 87. Dump Collection

1. verify target;
2. verify user approval;
3. reserve disk;
4. create restricted staging file;
5. request dump;
6. validate completion;
7. hash;
8. classify;
9. record metadata;
10. schedule retention.

---

## 87.1 Failure

Delete incomplete file where safe.

---

# 88. Dump Redaction

Binary dump redaction is not promised.

---

## 88.1 Metadata Scan

Possible.

---

## 88.2 Full Content Scan

Can be incomplete and expensive.

---

## 88.3 Safe Default

Exclude.

---

# 89. Crash Collection

Crash receipt should be written without requiring a dump.

---

## 89.1 Fields

* process;
* service;
* exception class;
* exit code;
* module;
* Runtime boot;
* operation;
* and last safe evidence references.

---

## 89.2 Automatic Triage Dump

Disabled.

---

# 90. Windows Error Reporting

Opure may integrate later.

---

## 90.1 Automatic Upload

Disabled.

---

## 90.2 WER Queue

Not assumed to be Opure's evidence store.

---

# 91. System Inventory Schema

Suggested:

```text
opure.support-system-inventory/1
```

---

## 91.1 Included Values

Only allowlisted support values.

---

## 91.2 Stable Local Machine ID

Not exported by default.

---

## 91.3 Host Name

Pseudonymised.

---

# 92. Software Inventory

Include:

* Opure package;
* Opure services;
* .NET runtime;
* local model runtime package;
* selected plugin packages;
* selected MCP client adapter;
* and diagnostic tools.

---

## 92.1 Installed Applications

Excluded.

---

# 93. Hardware Inventory

Include when relevant:

* CPU model;
* core counts;
* RAM;
* GPU;
* driver;
* storage free space;
* and battery state class.

---

## 93.1 Serial Numbers

Excluded.

---

# 94. Network Inventory

Default:

* online or offline;
* approved provider reachability class;
* proxy configured Boolean;
* and network-policy state.

---

## 94.1 Addresses

Excluded.

---

## 94.2 Wi-Fi

Excluded.

---

# 95. Retention Policy Definition

Suggested schema:

```text
opure.trust-retention-policy/1
```

---

## 95.1 Fields

```text
policy_id
revision
record_classes
scope
purpose
classification
base_duration
minimum_duration
maximum_duration
dependency_rules
hold_rules
deletion_method
review_period
owner
created_at
policy_sha256
```

---

# 96. Retention Classes

* Ephemeral;
* Operational Debug;
* Operational Standard;
* Performance Diagnostic;
* Authoritative Operation;
* Security Critical;
* Recovery Critical;
* Release Evidence;
* Incident Evidence;
* Support Staging;
* User Export;
* and Preservation.

---

# 97. Retention Decision Calculation

Inputs:

* Evidence Type;
* authority;
* classification;
* project;
* state;
* terminal time;
* owner dependency;
* active workflow;
* unresolved outcome;
* incident;
* hold;
* enterprise policy;
* user policy;
* and storage pressure.

---

## 97.1 Storage Pressure

May shorten only where policy permits.

---

## 97.2 Critical Dependency

Extends.

---

# 98. Retention Conflict

Examples:

* privacy maximum below required unresolved-effect minimum;
* administrative hold with requested deletion;
* project deletion while incident preserved;
* and owner record deleted before Trust projection.

---

## 98.1 Resolution

Visible and policy controlled.

---

# 99. Retention Review

Periodic:

* expired records;
* open holds;
* unresolved incidents;
* owner dependencies;
* storage use;
* and failed deletions.

---

## 99.1 Frequency

Suggested daily maintenance.

---

# 100. Evidence Inventory

Trust Centre should provide:

* category;
* record count;
* artefact count;
* storage;
* oldest;
* newest;
* retention class;
* active holds;
* and next deletion.

---

# 101. Deletion Plan

Suggested schema:

```text
opure.trust-deletion-plan/1
```

---

## 101.1 Fields

```text
deletion_plan_id
scope
selectors
owner_records
trust_records
artefacts
dependencies
holds
retention_decisions
method
preview
approval
created_at
plan_sha256
```

---

# 102. Deletion Ordering

Typical:

1. remove active projection access;
2. request owner deletion;
3. verify owner result;
4. delete Trust payload;
5. delete Trust projection;
6. decrement CAS references;
7. garbage collect;
8. create safe deletion receipt.

---

## 102.1 Owner Retains

Show.

---

## 102.2 External Copy

Outside control.

---

# 103. CAS Garbage Collection

Only unreferenced artefacts.

---

## 103.1 Transaction

Reference removal before deletion queue.

---

## 103.2 Crash

Idempotent.

---

## 103.3 Corrupt Reference Count

Rebuild from manifests.

---

# 104. Deletion Receipt

Contains:

* deletion plan;
* scope;
* record counts;
* artefact counts;
* owner outcomes;
* hold exceptions;
* completion time;
* and verification.

---

## 104.1 Deleted Values

Not retained.

---

# 105. Preservation Hold Definition

Suggested schema:

```text
opure.trust-preservation-hold/1
```

---

## 105.1 States

* Draft;
* Active;
* Review Due;
* Release Requested;
* Released;
* Expired;
* Superseded;
* and Quarantined.

---

# 106. Hold Scope

May select:

* incident;
* project;
* operation;
* workflow;
* evidence types;
* time range;
* actor class;
* and artefact types.

---

## 106.1 Broad Machine Hold

Requires advanced enterprise authority.

---

# 107. Hold Creation

1. define purpose;
2. define scope;
3. preview included evidence;
4. check storage estimate;
5. acquire approval;
6. create hold;
7. mark matching evidence;
8. copy payloads where owner retention cannot be guaranteed;
9. hash;
10. schedule review.

---

# 108. Hold Access

Every access to preserved payload should record:

* actor;
* purpose;
* time;
* records;
* and result.

---

## 108.1 Trust Centre Viewing

Counts as access for sensitive payload.

---

# 109. Hold Release

Requires:

* current scope;
* reason;
* review of unresolved dependencies;
* approval;
* and new retention decisions.

---

## 109.1 Immediate Deletion

Not assumed.

Ordinary retention recalculates.

---

# 110. Incident Record Schema

Suggested:

```text
opure.security-incident/1
```

---

## 110.1 States

* Reported;
* Triaging;
* Investigating;
* Containing;
* Recovering;
* Monitoring;
* Closed;
* Reopened;
* and Archived.

---

## 110.2 Severity

* Informational;
* Low;
* Moderate;
* High;
* and Critical.

---

# 111. Incident Evidence

Evidence links include:

* relevance;
* collector;
* source;
* hash;
* preservation status;
* access;
* and notes.

---

## 111.1 Model Summary

May explain evidence but not alter it.

---

# 112. Incident Export

Separate from normal support.

---

## 112.1 Redaction

Purpose specific.

---

## 112.2 Full Evidence

May remain sensitive and require enterprise process.

---

## 112.3 Legal Advice

Opure does not provide it.

---

# 113. Support Bundle Definition

Suggested schema:

```text
opure.support-bundle-definition/1
```

---

## 113.1 Fields

```text
bundle_definition_id
revision
name
purpose
eligible_evidence_types
telemetry_policy
diagnostic_policy
system_inventory_policy
source_content_policy
prompt_policy
tool_result_policy
dump_policy
redaction_profile
secret_scan_profile
maximum_time_range
maximum_size
staging_retention
created_at
definition_sha256
```

---

# 114. Minimal Bundle Definition

No process diagnostics.

---

## 114.1 Time Range

Suggested:

```text
last 30 minutes
```

for errors plus exact selected operation evidence.

---

## 114.2 Maximum Size

Suggested:

```text
25 MiB
```

---

# 115. Standard Bundle Definition

Suggested time range:

```text
last 2 hours
```

Maximum:

```text
250 MiB
```

---

## 115.1 Explicit Project

Optional.

---

# 116. Performance Bundle Definition

Includes an explicit diagnostic session.

---

## 116.1 Maximum

Suggested:

```text
1 GiB
```

without dump.

---

# 117. Crash Triage Bundle Definition

Includes one explicitly approved dump.

---

## 117.1 Maximum

Based on dump estimate and free disk.

---

# 118. Custom Bundle Definition

Created per operation.

---

## 118.1 Persisted Template

Only after explicit save.

---

## 118.2 Policy

Cannot broaden prohibited data classes.

---

# 119. Support Bundle Plan Schema

Suggested:

```text
opure.support-bundle-plan/1
```

---

## 119.1 Fields

```text
bundle_id
definition_revision
purpose
scope
time_range
projects
operations
evidence_selectors
telemetry_selectors
diagnostic_sessions
system_inventory
content_inclusions
redaction_profile
secret_scan_profile
size_limit
staging_retention
destination_class
preview_sha256
approval
created_at
expires_at
plan_sha256
```

---

# 120. Plan Expiry

Suggested:

```text
30 minutes
```

before collection or export approval refresh.

---

## 120.1 State Change

Material source or policy change invalidates.

---

# 121. Bundle Item

Fields:

```text
item_id
source_service
source_record
source_revision
source_type
bundle_path
classification
original_size
collected_size
payload_sha256
redaction_state
scan_state
inclusion_reason
```

---

# 122. Collection Snapshot

Owner service returns:

* exact query;
* snapshot time;
* record IDs;
* hashes;
* completeness;
* and expiry.

---

## 122.1 Mutable Owner Payload

Copy exact revision.

---

# 123. Collection Capability

Binds:

* bundle;
* owner service;
* item types;
* time range;
* project;
* maximum bytes;
* and expiry.

---

## 123.1 Reuse

Denied.

---

# 124. Collection Failure

Options:

* Fail Bundle;
* Continue with Omission;
* Ask;
* or Retry Same Source.

Definition specific.

---

## 124.1 Omission

Manifested.

---

# 125. Bundle Staging

Directory ACL:

* current user;
* Opure trusted services;
* and no broad inherited write where practical.

---

## 125.1 Random Directory Name

Bundle ID.

---

## 125.2 No Project Indexing

Required.

---

## 125.3 No Memory Indexing

Required.

---

# 126. Canonical Text Export

Use:

* UTF-8;
* LF or declared newline;
* stable property order;
* strict JSON;
* and JSON Lines for records.

---

## 126.1 Source Newline

Not preserved unless source artefact explicitly selected.

---

# 127. Bundle README

Human-readable:

* purpose;
* collection time;
* important exclusions;
* dump warning;
* sharing warning;
* and verification instructions.

---

## 127.1 No Sensitive Values

Required.

---

# 128. Bundle Inventory

List every item with:

* path;
* type;
* classification;
* size;
* hash;
* source;
* redaction;
* and scan result.

---

# 129. Integrity Hashes

Use SHA-256.

---

## 129.1 Manifest Hash

Manifest excludes its own hash field or uses defined canonical procedure.

---

## 129.2 Archive Hash

Calculated after final file creation.

Stored in export receipt, not inside itself.

---

# 130. Bundle Signature

Deferred.

---

## 130.1 Local HMAC

Not selected because it would not give an external recipient independent trust.

---

## 130.2 Enterprise Signing

Future.

---

# 131. ZIP Entry Rules

Every entry path:

* relative;
* forward slash separated;
* normalised;
* no empty segment where prohibited;
* no `.` or `..`;
* no drive;
* no UNC;
* no device name;
* no colon;
* no trailing space or dot;
* no alternate stream;
* no case collision;
* and unique.

---

## 131.1 Directory Entries

Optional.

---

## 131.2 Symlink

Never generated.

---

# 132. ZIP Compression

Use standard Deflate or Store.

---

## 132.1 Already Compressed Binary

Store.

---

## 132.2 Compression Ratio

Record.

---

## 132.3 Zip Bomb

Generation limits prevent.

---

# 133. ZIP Encryption

Not used.

---

## 133.1 Warning

Bundle may contain sensitive information.

---

## 133.2 Future Format

Separate ADR or amendment.

---

# 134. Archive Verification

After writing:

1. reopen;
2. enumerate entries;
3. validate paths;
4. validate count;
5. validate sizes;
6. recalculate entry hashes;
7. validate manifest;
8. validate inventory;
9. validate omissions;
10. calculate archive hash.

---

## 134.1 Verification Failure

Quarantine and do not export success.

---

# 135. Export Destination

Selected through trusted file picker.

---

## 135.1 Existing File

Explicit replace.

---

## 135.2 Network Destination

Allowed only under policy and warning.

---

## 135.3 Cloud-Synchronised Folder

Warn.

---

## 135.4 Removable Drive

Warn and record destination class.

---

# 136. Export Receipt

Suggested schema:

```text
opure.support-bundle-export-receipt/1
```

---

## 136.1 Fields

```text
bundle_id
definition
manifest_sha256
archive_sha256
archive_size
destination_class
exported_at
exported_by
staging_deletion_due
state
receipt_sha256
```

---

## 136.2 Raw Path

Not stored.

---

# 137. Support Bundle Deletion

Delete:

* staging directory;
* incomplete archive;
* internal manifest cache;
* and unreferenced diagnostic artefacts.

---

## 137.1 Exported Archive

User-controlled.

---

## 137.2 Delete Export Action

May delete exact selected path after verification.

---

# 138. Redaction Profile

Suggested schema:

```text
opure.redaction-profile/1
```

---

## 138.1 Fields

```text
profile_id
revision
field_rules
path_rules
identity_rules
content_rules
hash_rules
truncation_rules
binary_policy
created_at
profile_sha256
```

---

# 139. Schema-Level Exclusion

Strongest control.

---

## 139.1 Example

Vault secret field does not exist in export schema.

---

# 140. Field-Level Redaction

Evidence Type marks:

* Safe;
* Mask;
* Remove;
* Pseudonymise;
* Hash;
* or Explicit Approval.

---

# 141. Owner-Service Redaction

Owner understands payload semantics.

---

## 141.1 Trust Redaction

Second layer.

---

## 141.2 Conflict

More restrictive result wins.

---

# 142. Bundle-Local Pseudonymisation

Generate mapping:

```text
project-1
user-1
machine-1
provider-account-1
external-account-1
```

---

## 142.1 Mapping File

Not included unless necessary.

---

## 142.2 Stable Across Bundle

Yes.

---

## 142.3 Stable Across Bundles

No by default.

---

# 143. Path Normalisation

Order replacements longest and most specific first.

---

## 143.1 Canonical Tokens

* `<PROJECT_ROOT>`;
* `<USER_PROFILE>`;
* `<OPURE_DATA>`;
* `<TEMP>`;
* `<WORKTREE>`;
* `<MODEL_STORE>`;
* and `<PLUGIN_STORE>`.

---

# 144. Hash Redaction

Use only when:

* value has sufficient entropy;
* correlation is needed;
* and disclosure risk is acceptable.

---

## 144.1 Boolean and Enumeration

Do not hash.

---

## 144.2 Email

Prefer pseudonym.

---

# 145. Truncation

Record:

* original length;
* retained prefix or suffix policy;
* and truncated marker.

---

## 145.1 Secret

Remove, do not merely truncate.

---

# 146. Source Snippet

Fields:

```text
project_relative_path
base_hash
line_start
line_end
content
classification
redactions
```

---

## 146.1 Exact Selection

Required.

---

## 146.2 Generated File

Label.

---

# 147. Prompt Snippet

Fields:

* operation;
* role;
* selected segment;
* original hash;
* redaction;
* and reason.

---

## 147.1 Hidden Reasoning

Never.

---

# 148. Model Output Snippet

Similar explicit selection.

---

## 148.1 Tool Call

Separate metadata.

---

# 149. Command Output

Default metadata:

* exit class;
* duration;
* byte counts;
* truncation;
* and error category.

---

## 149.1 Raw Output

Explicit selection.

---

# 150. Secret Scanner Registry

Suggested schema:

```text
opure.secret-scanner/1
```

---

## 150.1 Scanner Types

* Exact Canary;
* Vault Reference;
* Pattern;
* Structured Credential;
* Private Key;
* Entropy;
* and Custom First-Party.

---

## 150.2 Plugin Scanner

Deferred.

---

# 151. Exact Canary Scan

Use test and runtime canaries where configured.

---

## 151.1 Production Secret

Do not load from Vault for comparison.

---

## 151.2 Derived Fingerprint

May use a safe keyed fingerprint under Secrets Service where supported.

---

# 152. Pattern Scan

Patterns for:

* provider tokens;
* bearer tokens;
* private keys;
* passwords;
* connection strings;
* and cloud credentials.

---

## 152.1 False Positive

Reviewable.

---

# 153. Entropy Scan

Supplementary.

---

## 153.1 Not Sole Gate

Required.

---

## 153.2 Low-Entropy Secret

May be missed.

---

# 154. Scan Finding State

* New;
* Confirmed Sensitive;
* False Positive;
* Redacted;
* Excluded;
* Accepted for Incident Export;
* and Unresolved.

---

## 154.1 Ordinary Support

Unresolved blocks export.

---

# 155. Structural Validation

After redaction:

* JSON parse;
* schema validate;
* no secret-prohibited fields;
* no path violations;
* no broken references;
* and no invalid UTF-8.

---

# 156. Final Preview

Must be generated from final staged bytes.

---

## 156.1 Preview Staleness

Any file change invalidates approval.

---

## 156.2 Binary Files

Metadata preview.

---

# 157. Export Approval

Binds:

* bundle ID;
* plan hash;
* manifest hash;
* file inventory hash;
* classifications;
* dump presence;
* source presence;
* secret-scan state;
* destination class;
* and expiry.

---

## 157.1 Approval Reuse

Denied.

---

# 158. Bundle State Machine

```text
Draft
    ↓
Planning
    ↓
Approval Required
    ↓
Collecting
    ↓
Redacting
    ↓
Scanning
    ↓
Preview Ready
    ↓
Export Approved
    ↓
Writing
    ↓
Verifying
    ↓
Exported
```

Failure branches to:

* Failed;
* Cancelled;
* Quarantined;
* Expired;
* or Deleted.

---

# 159. Collection Cancellation

Persist request.

---

## 159.1 EventPipe

Stop session.

---

## 159.2 Dump

May not be cancellable safely once requested.

Record state.

---

## 159.3 Partial File

Delete or quarantine.

---

# 160. Bundle Generation Recovery

After Runtime restart:

* discover non-terminal plans;
* expire stale approvals;
* inspect diagnostic collector;
* validate staging;
* delete invalid partial files;
* resume redaction or scanning where deterministic;
* require fresh export approval;
* and never upload.

---

# 161. Bundle Quarantine

Triggers:

* hash mismatch;
* path violation;
* secret unresolved;
* dump without approval;
* stale plan;
* owner conflict;
* archive verification failure;
* or unexpected executable.

---

# 162. Bundle Retention

Staging expires.

---

## 162.1 Preservation

Only explicit.

---

## 162.2 Export Receipt

Retained according to Trust Evidence policy.

---

# 163. Support Interaction

Version 1 has no integrated support account.

---

## 163.1 Bundle ID

Can be quoted manually.

---

## 163.2 External Ticket

User may record a ticket reference as an assertion.

---

## 163.3 Credentials

Never stored in bundle metadata.

---

# 164. Enterprise Support Policy

May:

* prohibit dumps;
* prohibit source snippets;
* prohibit external destinations;
* require a support destination class;
* shorten staging;
* require Preservation Hold;
* and require administrator approval.

It may not:

* bypass Product secret controls;
* enable automatic hidden upload;
* or remove preview for sensitive content.

---

# 165. Plugin Diagnostic Contribution

Plugin Host may emit:

```text
plugin.<publisher>.<id>.diagnostic
```

under a registered schema.

---

## 165.1 Authority

Plugin-Provided Observation.

---

## 165.2 Limits

* 16 KiB per event;
* bounded rate;
* no binary;
* no secret;
* and no arbitrary path.

Values provisional.

---

# 166. Plugin Support Artefact

No direct file inclusion.

Plugin requests a first-party collection adapter.

---

## 166.1 User Approval

Required for private plugin data.

---

# 167. MCP Evidence

Generated by MCP Gateway.

---

## 167.1 Server Claim

Displayed as external server data.

---

## 167.2 Raw Result

Excluded.

---

# 168. Provider Evidence

Provider adapter records:

* endpoint class;
* region;
* model;
* request ID;
* status;
* usage;
* latency;
* and cost.

---

## 168.1 Provider Receipt Verification

Adapter specific.

---

## 168.2 Prompt

Separate artefact.

---

# 169. Local Model Evidence

Record:

* Runtime Package;
* model hash;
* Execution Profile;
* GPU or CPU profile;
* input and output counts;
* latency;
* cancellation;
* and failure.

---

# 170. Project Knowledge Evidence

Record:

* query plan;
* channels;
* index generations;
* result count;
* source references;
* and truncation.

---

## 170.1 Result Content

Excluded by default.

---

# 171. Project Memory Evidence

Record lifecycle and context-use receipts.

---

## 171.1 Memory Content

Sensitive and excluded by default.

---

# 172. Trust Centre Access Record

Sensitive views may create access evidence.

---

## 172.1 Ordinary Summary

No access event required for every row.

---

## 172.2 Preserved Payload

Access event required.

---

# 173. Trust Centre Export

A general evidence export should use the support-bundle pipeline.

---

## 173.1 CSV

May be available for safe tables.

---

## 173.2 Sensitive Evidence

Bundle only.

---

# 174. Trust Centre Deletion UI

Show:

* selected scope;
* estimated records;
* owners;
* holds;
* dependencies;
* exported copies warning;
* and forensic-deletion limitation.

---

# 175. Trust Centre Retention UI

Allow:

* view policy;
* shorten preference within policy;
* extend within policy;
* create user hold;
* release user hold;
* and run cleanup.

---

## 175.1 Security Hold

Restricted.

---

# 176. Trust Centre Health

Indicators:

* ingestion lag;
* owner gaps;
* conflicts;
* integrity failures;
* retention backlog;
* failed deletion;
* failed scan;
* bundle staging age;
* and database health.

---

# 177. Accessibility

Trust Centre should support:

* keyboard;
* Narrator;
* high contrast;
* text alternatives for graphs;
* chronological table view;
* expandable evidence;
* and no colour-only authority or severity.

---

# 178. UI Copy — Trust Evidence

Suggested:

> The Trust Centre shows verified receipts and projections from the Opure services that own each decision or effect. A missing projection may indicate delayed or unavailable evidence; it does not prove that an action did not occur.

---

# 179. UI Copy — Logs

Suggested:

> Logs and traces help diagnose behaviour but may be sampled, rotated or omitted. Opure does not treat an operational log line as the sole authoritative record of a policy decision, approval, write or external side effect.

---

# 180. UI Copy — Support Bundle

Suggested:

> This bundle will be created locally from the displayed evidence and diagnostics. Opure will redact and scan the staged files, then show the final inventory before export. It will not upload the bundle automatically.

---

# 181. UI Copy — Dump Warning

Suggested:

> A process dump can contain source text, file paths, prompts, model output, credentials or other data held in memory. Opure cannot reliably redact every memory page without damaging the diagnostic. Include this dump only when its support value justifies the risk.

---

# 182. UI Copy — Retention

Suggested:

> Evidence is retained for a stated operational, recovery, security or support purpose. The effective deletion date includes current policy, unresolved dependencies and active preservation holds. Exported copies and external systems are outside Opure's deletion control.

---

# 183. UI Copy — Integrity

Suggested:

> Hashes and record chains help Opure detect local corruption, gaps and inconsistent projections. They are not externally anchored timestamps, legal chain-of-custody proof or protection against a fully compromised user account.

---

# 184. Diagnostics

Safe Trust Service diagnostics may include:

* evidence type;
* owner service;
* ingestion result;
* gap count;
* conflict class;
* retention class;
* bundle state;
* redaction count;
* finding count;
* and duration.

---

## 184.1 Prohibited

Do not log:

* evidence payload;
* support-bundle contents;
* dump data;
* source snippets;
* prompts;
* model output;
* secret findings;
* or export path.

---

# 185. Metrics

Low-cardinality metrics:

* evidence ingested;
* evidence rejected;
* owner gaps;
* conflicts;
* retention deletion count;
* preservation holds;
* bundle generation count;
* bundle failure class;
* scan findings count;
* and staging bytes.

---

## 185.1 No Project Label

Required.

---

# 186. Error Model

Stable categories:

* Trust Evidence Type Unknown;
* Trust Evidence Schema Invalid;
* Trust Evidence Owner Mismatch;
* Trust Evidence Duplicate Conflict;
* Trust Evidence Sequence Gap;
* Trust Evidence Hash Invalid;
* Trust Evidence Relationship Invalid;
* Trust Evidence Owner Unavailable;
* Trust Evidence Owner Record Missing;
* Trust Evidence Projection Delayed;
* Trust Evidence Projection Corrupt;
* Trust Evidence Retention Conflict;
* Trust Evidence Hold Required;
* Trust Evidence Deletion Denied;
* Trust Evidence Deletion Failed;
* Trust Evidence Preservation Failed;
* Trust Evidence Incident Invalid;
* Support Bundle Definition Invalid;
* Support Bundle Plan Invalid;
* Support Bundle Approval Required;
* Support Bundle Approval Expired;
* Support Bundle Source Unavailable;
* Support Bundle Collection Failed;
* Support Bundle Size Limit;
* Support Bundle Diagnostic Denied;
* Support Bundle Dump Denied;
* Support Bundle Redaction Failed;
* Support Bundle Secret Found;
* Support Bundle Preview Changed;
* Support Bundle Archive Invalid;
* Support Bundle Export Failed;
* Support Bundle Destination Denied;
* Support Bundle Cancelled;
* Support Bundle Quarantined;
* Support Bundle Expired;
* and Trust Recovery Required.

---

# 187. Security Threat Model

Relevant threats include:

* forged Evidence Record;
* owner-service impersonation;
* evidence-type substitution;
* payload substitution;
* owner-sequence deletion;
* owner-sequence insertion;
* owner-sequence replay;
* hash-chain reset;
* projection tampering;
* evidence relationship forgery;
* authority-class elevation;
* model proposal displayed as decision;
* user assertion displayed as deterministic result;
* cross-project evidence disclosure;
* Trust Centre query injection;
* log injection;
* trace-payload leakage;
* metric-cardinality exhaustion;
* secret in log;
* secret in trace;
* secret in metric attribute;
* secret in crash dump;
* support-bundle source overcollection;
* hidden prompt collection;
* hidden model-output collection;
* process command-line collection;
* environment-variable collection;
* machine identity leakage;
* path leakage;
* bundle-local pseudonym collision;
* weak hashing of low-entropy identifiers;
* secret-scanner evasion;
* secret-scanner false negative;
* secret-scanner false positive;
* redaction-rule bypass;
* stale preview approval;
* staged-file substitution;
* archive path traversal;
* archive case collision;
* archive duplicate path;
* archive device name;
* archive symlink;
* archive bomb;
* oversized dump;
* diagnostic-port abuse;
* plugin diagnostic flooding;
* MCP evidence forgery;
* Preservation Hold bypass;
* deletion while held;
* hold created too broadly;
* hold access without evidence;
* exported bundle left indefinitely;
* automatic upload;
* malicious support destination;
* and local evidence history rewrite.

---

# 188. Security Controls

Controls include:

* service-authenticated evidence IPC;
* owner-service authority;
* transactional owner outboxes;
* typed Evidence Types;
* immutable record revisions;
* payload hashes;
* per-stream sequence;
* per-stream hash chains;
* evidence deduplication;
* relationship validation;
* Authority Classes;
* model non-authority labels;
* user-assertion labels;
* project-scoped capabilities;
* safe indexed fields;
* strict query schemas;
* structured logs;
* early redaction;
* low-cardinality metrics;
* diagnostic-session capabilities;
* exact process identity;
* dump opt-in;
* dump retention;
* source-content opt-in;
* prompt and output opt-in;
* schema-level exclusion;
* owner redaction;
* Trust redaction;
* path and identity pseudonymisation;
* layered secret scanning;
* unresolved-finding export block;
* final-byte preview;
* preview-hash approval;
* strict ZIP paths;
* staged atomic writes;
* archive reopen and verification;
* no ZIP encryption claim;
* no automatic upload;
* short staging retention;
* Preservation Holds;
* access evidence;
* deletion plans;
* and Trust Centre health.

---

# 189. Security Limitations

This architecture cannot guarantee:

* that every owner service emits every correct receipt;
* that every secret pattern is detected;
* that a dump is safe;
* that a user reviews every included file correctly;
* that an exported bundle is shared securely;
* that external copies are deleted;
* that the Windows clock is trustworthy;
* that local hash chains resist a fully compromised account;
* or that an application-level Preservation Hold satisfies legal requirements.

---

# 190. Privacy Impact

Trust evidence can contain:

* project activity;
* provider use;
* tool use;
* model use;
* plugin use;
* file names;
* account references;
* and incident data.

---

## 190.1 Data Minimisation

Every Evidence Type defines minimum fields.

---

## 190.2 Purpose

Every retention and export action records purpose.

---

## 190.3 Retention

Bounded and reviewable.

---

## 190.4 Export

Explicit and local.

---

# 191. Reliability Impact

Federated authority avoids central transaction claims.

Projection lag and owner unavailability become visible states.

---

## 191.1 Trust Database Loss

Rebuild from available owners where retention permits.

---

## 191.2 Owner Loss

Projection remains evidence but authority availability is marked.

---

# 192. Performance Impact

Costs include:

* owner outbox records;
* evidence ingestion;
* hashing;
* projection indexes;
* retention scans;
* reconciliation;
* redaction;
* and bundle verification.

These costs should remain subordinate to developer work.

---

# 193. Provisional Performance Targets

On reference hardware:

* Evidence Record validation p95: under 5 ms excluding payload transfer;
* evidence ingestion commit p95: under 20 ms;
* Trust Centre operation query p95: under 100 ms for 10,000 records;
* owner gap detection for 1,000 streams: under 1 second;
* retention calculation for 100,000 records: under 5 seconds;
* deletion-plan calculation for 100,000 records: under 10 seconds;
* Minimal bundle generation p95: under 10 seconds;
* Standard bundle generation without diagnostics p95: under 30 seconds;
* redaction of 100 MiB structured text: under 30 seconds;
* secret scan of 100 MiB structured text: under 60 seconds;
* and archive verification of 1 GiB: under 2 minutes.

Diagnostic collection duration is separate.

---

# 194. Scale Targets

Test:

* 10,000 Evidence Types;
* 100 owner services or streams;
* 100 million Evidence Records;
* 1 billion relationships;
* 1 million retained operations;
* 100,000 active Preservation Holds as a stress case;
* 100,000 bundle manifests;
* and a 2 GiB bundle.

These are architecture stress targets.

---

# 195. Testing Strategy

ADR-0008 applies.

Trust evidence and support bundles require:

* schema tests;
* owner-contract tests;
* ingestion tests;
* sequence tests;
* hash tests;
* authority tests;
* relationship tests;
* projection tests;
* completeness tests;
* query tests;
* telemetry tests;
* redaction tests;
* retention tests;
* deletion tests;
* Preservation Hold tests;
* incident tests;
* diagnostic tests;
* dump tests;
* bundle-plan tests;
* archive tests;
* export tests;
* cancellation tests;
* recovery tests;
* fuzzing;
* performance tests;
* and adversarial secret and evidence-forgery suites.

---

# 196. Database Tests

Test:

* create;
* migrate;
* foreign keys;
* transaction rollback;
* concurrent reads;
* one writer;
* corruption;
* backup;
* restore;
* channel isolation;
* and CAS consistency.

---

# 197. Evidence Type Tests

Test:

* valid;
* unknown owner;
* duplicate ID;
* changed authority;
* missing redaction profile;
* missing retention class;
* invalid support eligibility;
* and canonical hash.

---

# 198. Evidence Record Schema Tests

Test every required and conditional field.

---

## 198.1 Missing Owner

Reject.

---

## 198.2 Missing Authority

Reject.

---

## 198.3 Oversized Inline Payload

Reject.

---

## 198.4 Secret-Prohibited Field

Reject.

---

# 199. Evidence Identity Tests

Test:

* same owner record;
* same revision;
* new revision;
* duplicate hash;
* conflicting hash;
* and random-ID collision handling.

---

# 200. Authority-Class Tests

Verify each class renders distinctly.

---

## 200.1 Model Proposal

Cannot become Authoritative Domain Decision.

---

## 200.2 User Assertion

Cannot become Deterministic Validation Result.

---

## 200.3 Imported Record

Cannot become live authority without owner verification.

---

# 201. Outcome Tests

Test every outcome and subtype.

---

# 202. Actor Tests

Test:

* Developer;
* Enterprise Administrator;
* Runtime;
* service;
* worker;
* plugin;
* MCP;
* provider;
* model;
* imported;
* and unknown.

---

# 203. Actor-Impersonation Tests

Attempt plugin, model and MCP assertions as developer.

---

# 204. Subject Tests

Test every reference type and cross-project denial.

---

# 205. Time Tests

Test:

* occurred and observed equal;
* source time absent;
* source time future;
* source time old;
* clock rollback;
* Windows sleep;
* and boot change.

---

# 206. Sequence Tests

Test:

* first;
* next;
* duplicate;
* gap;
* out of order;
* reset;
* rollover boundary;
* and concurrent owner streams.

---

# 207. Runtime-Boot Tests

Test PID reuse and prior Runtime boot.

---

# 208. Payload-Reference Tests

Test:

* Owner Inline;
* Owner Database;
* Owner CAS;
* Trust Inline;
* Trust CAS;
* Preserved Copy;
* External Receipt;
* Deleted;
* missing;
* wrong hash;
* and wrong owner.

---

# 209. Record-Hash Tests

Modify every semantic field.

---

# 210. Stream-Hash Tests

Test:

* valid chain;
* altered payload;
* deletion;
* insertion;
* reordering;
* wrong genesis;
* reset without event;
* and concurrent stream.

---

# 211. Integrity-Limitation Tests

UI and exports must not say:

* tamper proof;
* legal proof;
* trusted timestamp;
* or non-repudiation.

---

# 212. Owner-Outbox Tests

Test:

* domain commit and outbox success;
* domain rollback;
* outbox delivery retry;
* duplicate;
* owner crash;
* and Trust unavailable.

---

# 213. Trust-Inbox Tests

Test duplicate same and conflicting payload.

---

# 214. Service-Authentication Tests

Attempt evidence submission from:

* wrong service;
* plugin host;
* MCP process;
* worker without capability;
* stale session;
* and another channel.

---

# 215. Ingestion Tests

Test every pipeline stage and transaction rollback.

---

# 216. Unknown-Evidence-Type Tests

Quarantine.

---

# 217. Owner-Mismatch Tests

Create security incident evidence.

---

# 218. Relationship-Schema Tests

Test every relationship type.

---

# 219. Relationship-Authority Tests

Attempt a model-created Authorises relationship.

Reject.

---

# 220. Relationship-Cycle Tests

Cycles may be valid for Correlates With but invalid for Supersedes under declared rules.

---

# 221. Causal-Graph Tests

Verify ordering, missing nodes and accessible text alternative.

---

# 222. Completeness Tests

Test:

* complete;
* scoped complete;
* delayed projection;
* owner unavailable;
* gap;
* owner missing;
* conflict;
* purged;
* redacted;
* and unknown.

---

# 223. Reconciliation Tests

Test:

* missing one record;
* sequence range;
* owner hash match;
* owner hash conflict;
* owner deleted;
* owner unavailable;
* and owner database restored.

---

# 224. Projection-Rebuild Tests

Delete projection and rebuild from Trust records.

---

# 225. Trust-Database-Rebuild Tests

Rebuild from owners under retention.

---

# 226. Owner-Loss Tests

Mark authority unavailable.

---

# 227. Query-Schema Tests

Test every filter, pagination and bounds.

---

# 228. Query-Injection Tests

Attempt:

* SQL;
* regex denial-of-service;
* path traversal;
* wildcard explosion;
* huge time range;
* and cross-project query.

---

# 229. Query-Snapshot Tests

Verify freshness and completeness.

---

# 230. Trust-Centre-Overview Tests

Verify all selected warning categories.

---

# 231. Project-View Tests

Verify strict isolation.

---

# 232. Operation-View Tests

Build causal chain across Context, Routing, Provider, Patch and Build.

---

# 233. AI-View Tests

Test local, remote, fallback, model drift and no prompt.

---

# 234. Data-Sharing-View Tests

Verify destination, classes, approval and no content.

---

# 235. Secret-Use-View Tests

Verify value absence.

---

# 236. Workflow-View Tests

Verify Outcome Unknown and compensation.

---

# 237. Tool-View Tests

Verify command redaction and effect receipt.

---

# 238. Plugin-and-MCP-View Tests

Verify authority labels.

---

# 239. Operational-Log-Schema Tests

Test all fields and OpenTelemetry mapping.

---

# 240. Log-Severity Tests

Map all .NET levels to selected ranges.

---

# 241. Log-Attribute Tests

Attempt high-cardinality and prohibited fields.

---

# 242. Log-Injection Tests

Use:

* newline;
* control characters;
* JSON fragments;
* terminal escape;
* huge text;
* and malicious attribute keys.

---

# 243. Early-Redaction Tests

Seed secrets before logger call.

---

# 244. Post-Persistence-Canary Tests

Verify periodic scan detects injected test canary.

---

# 245. Exception-Logging Tests

Test:

* exception type;
* safe message;
* stack;
* Data dictionary exclusion;
* inner exception;
* aggregate;
* and file paths.

---

# 246. Trace-Schema Tests

Test safe attributes and no payload.

---

# 247. Trace-Propagation Tests

Across:

* named pipes;
* gRPC;
* worker;
* provider adapter;
* plugin host;
* and MCP Gateway.

---

# 248. Trace-Payload-Leakage Tests

Seed source and secret canaries.

---

# 249. Metric-Cardinality Tests

Attempt:

* project IDs;
* paths;
* request IDs;
* workflow IDs;
* model IDs;
* and random labels.

---

# 250. Metric-Export Tests

Disabled by default.

---

# 251. Diagnostic-Session-Schema Tests

Test every field and state.

---

# 252. Diagnostic-Collector-Identity Tests

Test exact process, PID reuse, boot mismatch and stale capability.

---

# 253. Diagnostic-Port-Abuse Tests

Attempt from plugin, MCP and untrusted process.

---

# 254. EventPipe-Provider Tests

Test allowlist and prohibited provider.

---

# 255. EventPipe-Duration Tests

Test zero, default, maximum, cancellation and timeout.

---

# 256. EventPipe-Buffer Tests

Test bounded buffer and dropped-event reporting.

---

# 257. EventPipe-Side-Effect Tests

Measure overhead.

---

# 258. GC-Dump Tests

Test explicit approval, target, induced GC and retention.

---

# 259. Dump-Type Tests

Test Mini, Triage, Heap and Full.

---

# 260. Dump-Approval Tests

Change:

* process;
* type;
* size estimate;
* destination;
* and bundle inclusion.

Approval invalidates.

---

# 261. Dump-Secret Tests

Place canaries in process memory.

Verify warning and default exclusion.

---

# 262. Dump-Cancellation Tests

Test before request, during request and after file creation.

---

# 263. Automatic-Dump-Denial Tests

Crash without explicit policy.

No dump.

---

# 264. Windows-Event-Log Tests

Verify no broad Security or Application log collection.

---

# 265. System-Inventory Tests

Test included and excluded fields.

---

# 266. Hardware-Inventory Tests

Verify serial numbers absent.

---

# 267. Network-Inventory Tests

Verify addresses and Wi-Fi absent.

---

# 268. Retention-Policy-Schema Tests

Test every field and hash.

---

# 269. Default-Retention Tests

Test every selected default.

---

# 270. Retention-Decision Tests

Test:

* terminal operation;
* active workflow;
* unresolved effect;
* security critical;
* incident;
* hold;
* enterprise policy;
* user shortening;
* and storage pressure.

---

# 271. Retention-Conflict Tests

Test all conflict examples.

---

# 272. Daily-Retention-Maintenance Tests

Test backlog, cancellation, crash and restart.

---

# 273. Evidence-Inventory Tests

Verify counts, storage and next deletion.

---

# 274. Deletion-Plan-Schema Tests

Test scope, dependencies and hash.

---

# 275. Projection-Deletion Tests

Verify owner record remains.

---

# 276. Owner-Deletion Tests

Verify Trust state updates.

---

# 277. Deletion-Ordering Tests

Crash after every stage.

---

# 278. CAS-Garbage-Collection Tests

Test:

* referenced;
* unreferenced;
* corrupt count;
* crash;
* shared artefact;
* and hold.

---

# 279. Deletion-Receipt Tests

Verify no deleted content remains.

---

# 280. Forensic-Deletion-Copy Tests

Verify UI states limitations.

---

# 281. Preservation-Hold-Schema Tests

Test every type and state.

---

# 282. Hold-Scope Tests

Test incident, project, operation, workflow, type and time.

---

# 283. Broad-Hold-Authority Tests

Ordinary user cannot create machine-wide enterprise hold without policy.

---

# 284. Hold-Creation Tests

Test preview, storage estimate, owner copy and hash.

---

# 285. Hold-Bypass Tests

Attempt deletion, retention expiry, CAS GC and bundle staging cleanup.

---

# 286. Hold-Access Tests

Verify access evidence.

---

# 287. Hold-Release Tests

Test review and retention recalculation.

---

# 288. Incident-Schema Tests

Test every state and severity.

---

# 289. Incident-Evidence-Link Tests

Verify source, collector, hash and relevance.

---

# 290. Incident-Model-Summary Tests

Summary cannot mutate evidence.

---

# 291. Incident-Export-Tests

Verify separate advanced path.

---

# 292. Bundle-Definition-Schema Tests

Test every preset and limit.

---

# 293. Minimal-Bundle Tests

Verify inclusions and all default exclusions.

---

# 294. Standard-Bundle Tests

Verify bounded logs, traces and evidence.

---

# 295. Performance-Bundle Tests

Verify explicit EventPipe session.

---

# 296. Crash-Triage-Bundle Tests

Verify separate dump approval.

---

# 297. Workflow-Recovery-Bundle Tests

Verify no source or secret.

---

# 298. Provider-AI-Bundle Tests

Verify no prompt or output by default.

---

# 299. Plugin-MCP-Bundle Tests

Verify no private files or raw results.

---

# 300. Custom-Bundle Tests

Verify policy cannot be broadened.

---

# 301. Bundle-Plan-Schema Tests

Test every field, hash and expiry.

---

# 302. Plan-Staleness Tests

Change source, policy, time range, dump, project or destination.

---

# 303. Bundle-Item Tests

Test source revision, path, classification, size, hash and inclusion reason.

---

# 304. Owner-Collection-Snapshot Tests

Test exact snapshot and completeness.

---

# 305. Collection-Capability Tests

Test wrong bundle, owner, time, bytes, project and expiry.

---

# 306. Collection-Failure-Policy Tests

Test fail, omit, ask and retry.

---

# 307. Staging-ACL Tests

Verify plugin, MCP and unrelated process access denial where OS permits.

---

# 308. Staging-Indexing Tests

Verify Project Knowledge and Memory exclusion.

---

# 309. Staging-Expiry Tests

Test export, no export, crash and hold.

---

# 310. Canonical-Text Tests

Test UTF-8, property order, JSONL and newline.

---

# 311. README Tests

Verify warnings and no sensitive values.

---

# 312. Inventory Tests

Verify every file and omission.

---

# 313. Manifest Tests

Test exact source, classification, redaction and hash.

---

# 314. Manifest-Self-Hash Tests

Verify canonical procedure.

---

# 315. Archive-Hash Tests

Verify export receipt.

---

# 316. ZIP-Path Tests

Attempt:

* absolute;
* drive;
* UNC;
* device;
* traversal;
* dot;
* empty;
* colon;
* trailing dot;
* trailing space;
* ADS;
* reserved name;
* duplicate;
* case collision;
* and excessive length.

---

# 317. ZIP-Symlink Tests

Attempt symlink and reparse metadata.

---

# 318. ZIP-Entry-Count Tests

Test limit.

---

# 319. ZIP-Size Tests

Test ordinary file, dump exception and total.

---

# 320. ZIP-Compression Tests

Test Deflate, Store and compression ratio.

---

# 321. ZIP-Encryption Tests

Verify no legacy encryption and clear warning.

---

# 322. Archive-Reopen Tests

Test corrupted central directory, altered entry and missing manifest.

---

# 323. Export-Destination Tests

Test:

* local fixed disk;
* existing file;
* removable;
* network;
* cloud-synchronised;
* denied path;
* reparse;
* and low disk.

---

# 324. Export-Receipt Tests

Verify no raw path.

---

# 325. Bundle-Deletion Tests

Test staging and exported file action.

---

# 326. Redaction-Profile-Schema Tests

Test every rule type.

---

# 327. Schema-Exclusion Tests

Attempt forbidden fields.

---

# 328. Owner-Redaction Tests

Verify Trust cannot reintroduce removed values.

---

# 329. Pseudonymisation Tests

Test stable within bundle, different across bundles and collision.

---

# 330. Path-Normalisation Tests

Test nested paths and replacement order.

---

# 331. Hash-Redaction Tests

Test low-entropy denial.

---

# 332. Truncation Tests

Test original length and marker.

---

# 333. Source-Snippet Tests

Test exact file, hash, lines, secret and changed base.

---

# 334. Prompt-Snippet Tests

Test selected segment and no hidden reasoning.

---

# 335. Model-Output-Snippet Tests

Test explicit approval.

---

# 336. Command-Output Tests

Verify metadata default and raw opt-in.

---

# 337. Secret-Scanner-Schema Tests

Test every scanner kind.

---

# 338. Canary-Scan Tests

Seed canaries in:

* log;
* trace;
* evidence;
* source;
* prompt;
* output;
* command result;
* plugin event;
* MCP receipt;
* and manifest.

---

# 339. Pattern-Scan Tests

Test supported credential formats.

---

# 340. Private-Key Tests

Test PEM variants.

---

# 341. Entropy-Scan Tests

Test true and false positives.

---

# 342. Scanner-Evasion Tests

Use:

* splitting;
* Unicode;
* encoding;
* escaping;
* JSON arrays;
* base64;
* and compression.

---

# 343. Finding-State Tests

Test every state and ordinary export block.

---

# 344. Structural-Validation Tests

Test broken JSON, bad reference, path and prohibited field.

---

# 345. Final-Preview Tests

Verify final bytes and classifications.

---

# 346. Preview-Staleness Tests

Modify staged file after preview.

---

# 347. Binary-Preview Tests

Verify metadata and risk.

---

# 348. Export-Approval Tests

Test every bound field and expiry.

---

# 349. Bundle-State-Machine Tests

Test every allowed and prohibited transition.

---

# 350. Collection-Cancellation Tests

Test logs, trace, GC dump, process dump and owner collection.

---

# 351. Bundle-Recovery Tests

Crash in every state.

---

# 352. Bundle-Quarantine Tests

Test every trigger.

---

# 353. No-Automatic-Upload Tests

Monitor network during bundle generation and export.

---

# 354. Future-Upload-Guard Tests

Attempt hidden destination or retry.

---

# 355. Enterprise-Support-Policy Tests

Test dump denial, source denial, destination restriction and admin approval.

---

# 356. Plugin-Diagnostic-Schema Tests

Test namespace, size, rate and classification.

---

# 357. Plugin-Diagnostic-Flood Tests

Verify quota.

---

# 358. Plugin-Authority-Escalation Tests

Attempt authoritative first-party evidence.

---

# 359. Plugin-File-Inclusion Tests

Attempt direct path.

---

# 360. MCP-Evidence-Forgery Tests

Server cannot submit Trust Evidence.

---

# 361. Provider-Receipt Tests

Test identity, usage, status and no prompt.

---

# 362. Local-Model-Receipt Tests

Test model hash and profile.

---

# 363. Knowledge-and-Memory-Evidence Tests

Verify content exclusion.

---

# 364. Trust-Access-Record Tests

Test preserved payload viewing and export.

---

# 365. Trust-Centre-Export Tests

Verify support-bundle pipeline.

---

# 366. Trust-Centre-Deletion-UI Tests

Verify dependencies and warnings.

---

# 367. Trust-Centre-Health Tests

Test every health state.

---

# 368. Diagnostics-Leakage Tests

Seed canaries in Trust Service diagnostics.

---

# 369. Metrics-Cardinality Tests

Verify no project labels.

---

# 370. Fuzzing

Fuzz:

* Evidence Type;
* Evidence Record;
* relationships;
* owner outbox;
* Trust inbox;
* query;
* log record;
* retention policy;
* retention decision;
* deletion plan;
* Preservation Hold;
* incident;
* Bundle Definition;
* Bundle Plan;
* manifest;
* inventory;
* redaction profile;
* scanner finding;
* archive paths;
* and import verification.

---

# 371. Evidence-Forgery Adversarial Suite

Attempt:

* owner impersonation;
* authority elevation;
* record replacement;
* sequence reset;
* relationship forgery;
* and model decision forgery.

---

# 372. Evidence-Gap Adversarial Suite

Drop and reorder outbox deliveries.

---

# 373. Log-Injection Adversarial Suite

Inject untrusted model, tool, plugin and MCP text.

---

# 374. Secret-Leak Adversarial Suite

Seed each known secret class in every exportable source.

---

# 375. Dump Adversarial Suite

Place secrets, paths, prompts and source in process memory.

---

# 376. Archive Adversarial Suite

Test every path and size attack.

---

# 377. Hold-Bypass Adversarial Suite

Race deletion, export and retention.

---

# 378. Stale-Approval Adversarial Suite

Change final bytes after preview.

---

# 379. Cross-Project Adversarial Suite

Attempt query, bundle, deletion and hold across projects.

---

# 380. Plugin-and-MCP Adversarial Suite

Attempt evidence flooding, authority spoofing and raw payload export.

---

# 381. Performance Tests

Measure:

* ingestion;
* query;
* relationship traversal;
* completeness;
* retention;
* deletion planning;
* redaction;
* scanning;
* archive;
* verification;
* and recovery.

---

# 382. Endurance Tests

Run for seven days with:

* continuous owner evidence;
* log rotation;
* trace sampling;
* retention deletion;
* bundle generation;
* Runtime restarts;
* owner restarts;
* and Trust projection rebuilds.

---

# 383. Accessibility Tests

Trust Centre and Bundle UI must support:

* keyboard;
* Narrator;
* high contrast;
* reduced motion;
* timeline table;
* causal graph alternative;
* authority labels;
* completeness warnings;
* retention explanations;
* file inventory;
* dump warnings;
* and scan findings.

---

# 384. Prototype Plan

## 384.1 Prototype A — Owner Receipt

Configuration Service emits a transactional snapshot receipt.

---

## 384.2 Prototype B — Federated Projection

Trust Centre rebuilds from three owner services.

---

## 384.3 Prototype C — Gap Reconciliation

Drop one owner sequence and recover it.

---

## 384.4 Prototype D — Authority Labels

Display deterministic receipt, model proposal and user assertion distinctly.

---

## 384.5 Prototype E — Retention

Expire logs while retaining unresolved workflow evidence.

---

## 384.6 Prototype F — Preservation Hold

Hold one operation and prevent deletion.

---

## 384.7 Prototype G — Minimal Bundle

Generate with no source, prompt or dump.

---

## 384.8 Prototype H — Performance Bundle

Collect a 30-second EventPipe trace.

---

## 384.9 Prototype I — Crash Triage

Collect an explicitly approved triage dump and display risk.

---

## 384.10 Prototype J — Redaction and Canary

Redact paths and block a seeded provider key.

---

## 384.11 Prototype K — Archive Verification

Reject traversal, duplicate and case-colliding paths.

---

## 384.12 Prototype L — No Upload

Capture network activity during the complete support flow.

---

# 385. Implementation Plan

1. Record founder review.
2. Define Authority Classes.
3. Define Evidence Type schema.
4. Define Evidence Record schema.
5. Define relationship schema.
6. Define completeness schema.
7. Create Trust Evidence SQLite schema.
8. Create Trust Evidence CAS.
9. Implement owner evidence contract.
10. Implement transactional owner outbox pattern.
11. Implement Trust inbox.
12. Implement record validation.
13. Implement deduplication.
14. Implement owner sequences.
15. Implement record hashes.
16. Implement optional stream hash chains.
17. Implement relationship validation.
18. Implement projection builder.
19. Implement owner reconciliation.
20. Implement completeness reporting.
21. Implement Trust query contracts.
22. Build Overview and Operation views.
23. Build AI, Workflow, Tool and Patch views.
24. Build Configuration, Plugin, MCP and Update views.
25. Define structured operational log schema.
26. Integrate OpenTelemetry-compatible fields.
27. Implement telemetry redaction.
28. Implement trace and metric cardinality rules.
29. Define retention-policy schema.
30. Implement retention calculation.
31. Implement evidence inventory.
32. Implement deletion plans.
33. Implement CAS garbage collection.
34. Define Preservation Hold schema.
35. Implement hold creation, access and release.
36. Define incident schema.
37. Implement incident evidence links.
38. Define Bundle Definition schema.
39. Define Bundle Plan schema.
40. Implement Minimal bundle.
41. Implement Standard bundle.
42. Implement supervised Diagnostic Collector.
43. Integrate EventPipe.
44. Integrate GC dump collection.
45. Integrate triage dump collection.
46. Implement system inventory.
47. Define redaction profiles.
48. Implement path and identity pseudonymisation.
49. Implement source and prompt opt-in.
50. Implement Secret Scanner registry.
51. Implement canary and pattern scanners.
52. Implement final-byte preview.
53. Implement export approval.
54. Implement strict ZIP writer.
55. Implement archive reopen and verification.
56. Implement staged atomic export.
57. Implement staging expiry.
58. Implement bundle recovery.
59. Implement Trust Centre retention and bundle UI.
60. Add evidence-forgery adversarial suite.
61. Add secret and dump adversarial suites.
62. Add archive and hold-bypass suites.
63. Run performance and endurance tests.
64. Complete security, privacy, supportability and product review.
65. Accept, amend or reject the ADR.

---

# 386. Owners

| Area                       | Owner                                |
| -------------------------- | ------------------------------------ |
| Product policy             | Founder                              |
| Trust Evidence Service     | Trust Evidence Owner                 |
| Owner evidence contracts   | Every Domain Service Owner           |
| Operational logs           | Observability Owner                  |
| Traces and metrics         | Observability and Performance Owners |
| Trust Evidence persistence | Persistence Owner                    |
| Retention                  | Privacy, Security and Trust Owners   |
| Preservation Holds         | Security and Enterprise Owners       |
| Incident records           | Security and Recovery Owners         |
| Diagnostic collector       | Runtime and Observability Owners     |
| Process dumps              | Runtime, Security and Privacy Owners |
| Bundle definitions         | Supportability and Product Owners    |
| Redaction                  | Privacy and Security Owners          |
| Secret scanning            | Secrets and Security Owners          |
| Archive format             | Supportability and Filesystem Owners |
| Export                     | Desktop and Filesystem Owners        |
| Plugin evidence            | Plugin Platform Owner                |
| MCP evidence               | MCP Gateway Owner                    |
| Provider evidence          | Provider Trust Owner                 |
| Local model evidence       | Local Model Runtime Owner            |
| Workflow evidence          | Workflow Owner                       |
| Trust Centre UI            | Trust Centre and Desktop Owners      |
| Enterprise policy          | Enterprise Management Owner          |
| Recovery                   | Recovery Owner                       |
| Adversarial tests          | Test Architecture Owner              |

---

# 387. Suggested Repository Structure

```text
src/
├── Trust/
│   ├── Opure.Trust.Contracts/
│   ├── Opure.Trust.Evidence.Service/
│   ├── Opure.Trust.Evidence.Types/
│   ├── Opure.Trust.Evidence.Ingestion/
│   ├── Opure.Trust.Evidence.Relationships/
│   ├── Opure.Trust.Evidence.Projections/
│   ├── Opure.Trust.Evidence.Reconciliation/
│   ├── Opure.Trust.Evidence.Retention/
│   ├── Opure.Trust.Evidence.Preservation/
│   ├── Opure.Trust.Evidence.Incidents/
│   ├── Opure.Trust.Support.Contracts/
│   ├── Opure.Trust.Support.Service/
│   ├── Opure.Trust.Support.Collection/
│   ├── Opure.Trust.Support.Diagnostics/
│   ├── Opure.Trust.Support.Redaction/
│   ├── Opure.Trust.Support.SecretScanning/
│   ├── Opure.Trust.Support.Archives/
│   ├── Opure.Trust.Support.Export/
│   ├── Opure.Trust.Persistence/
│   └── Opure.Trust.Security/
├── Worker/
│   └── Opure.Worker.Diagnostics/
└── Desktop/
    └── Opure.Desktop.TrustCentre/

schemas/
└── trust/
    ├── evidence-type-v1.schema.json
    ├── evidence-record-v1.schema.json
    ├── evidence-relationship-v1.schema.json
    ├── evidence-completeness-v1.schema.json
    ├── retention-policy-v1.schema.json
    ├── retention-decision-v1.schema.json
    ├── deletion-plan-v1.schema.json
    ├── preservation-hold-v1.schema.json
    ├── security-incident-v1.schema.json
    ├── support-bundle-definition-v1.schema.json
    ├── support-bundle-plan-v1.schema.json
    ├── support-bundle-manifest-v1.schema.json
    ├── redaction-profile-v1.schema.json
    └── support-bundle-export-receipt-v1.schema.json

tests/
└── Trust/
    ├── Opure.Trust.UnitTests/
    ├── Opure.Trust.PersistenceTests/
    ├── Opure.Trust.SecurityTests/
    ├── Opure.Trust.SupportBundleTests/
    ├── Opure.Trust.DiagnosticsTests/
    ├── Opure.Trust.PerformanceTests/
    └── Fixtures/
        ├── Evidence/
        ├── Logs/
        ├── Dumps/
        ├── Bundles/
        ├── Secrets/
        └── MaliciousArchives/
```

Exact project count may be consolidated under ADR-0010.

---

# 388. Evidence Record Sketch

```json
{
  "schema": "opure.trust-evidence-record/1",
  "evidence_id": "evidence-opaque",
  "evidence_type": "ai.routing.decision",
  "evidence_type_revision": 1,
  "owner_service": "routing-governance",
  "owner_record_id": "routing-decision-opaque",
  "owner_record_revision": 1,
  "release_channel": "stable",
  "project_id": "project-opaque",
  "operation_id": "operation-opaque",
  "authority_class": "authoritative-domain-decision",
  "action": "select-ai-execution-profile",
  "outcome": "approved",
  "occurred_at_utc": "2026-07-18T18:00:00Z",
  "observed_at_utc": "2026-07-18T18:00:00.050Z",
  "payload_sha256": "...",
  "previous_stream_sha256": "...",
  "record_sha256": "...",
  "retention_class": "authoritative-operation"
}
```

---

# 389. Relationship Sketch

```json
{
  "schema": "opure.trust-evidence-relationship/1",
  "relationship_id": "relationship-opaque",
  "source_evidence": "provider-request-receipt-opaque",
  "relationship_type": "authorised-by",
  "target_evidence_or_subject": "data-sharing-approval-opaque",
  "owner_service": "provider-trust",
  "authority_class": "verified-service-receipt",
  "created_at": "2026-07-18T18:00:00Z",
  "sha256": "..."
}
```

---

# 390. Retention Decision Sketch

```json
{
  "schema": "opure.trust-retention-decision/1",
  "retention_decision_id": "retention-opaque",
  "record": "evidence-opaque",
  "policy_revisions": [
    "authoritative-operation:2",
    "enterprise-retention:4"
  ],
  "purpose": [
    "workflow-recovery",
    "developer-inspection"
  ],
  "base_expiry": "2027-01-14T18:00:00Z",
  "dependency_extensions": [
    "workflow-active"
  ],
  "hold_extensions": [],
  "effective_expiry": null,
  "reason": "Retained while the linked workflow remains non-terminal.",
  "sha256": "..."
}
```

---

# 391. Preservation Hold Sketch

```json
{
  "schema": "opure.trust-preservation-hold/1",
  "hold_id": "hold-opaque",
  "hold_type": "security-incident",
  "scope": {
    "incident": "incident-opaque",
    "projects": [
      "project-opaque"
    ],
    "from": "2026-07-18T17:00:00Z",
    "to": "2026-07-18T19:00:00Z"
  },
  "purpose": "Preserve evidence for investigation of an unexpected provider request.",
  "created_by": "developer-opaque",
  "review_due": "2026-08-18T00:00:00Z",
  "state": "active",
  "sha256": "..."
}
```

---

# 392. Support Bundle Plan Sketch

```json
{
  "schema": "opure.support-bundle-plan/1",
  "bundle_id": "bundle-opaque",
  "definition_revision": "standard:2",
  "purpose": "Investigate a workflow that stopped after an ambiguous repository push.",
  "scope": {
    "project": "project-opaque",
    "workflow": "workflow-opaque"
  },
  "time_range": {
    "from": "2026-07-18T17:30:00Z",
    "to": "2026-07-18T18:30:00Z"
  },
  "content_inclusions": {
    "source": false,
    "prompts": false,
    "model_outputs": false,
    "command_output": false,
    "process_dump": false
  },
  "redaction_profile": "standard-support:3",
  "secret_scan_profile": "support-export:4",
  "size_limit": 262144000,
  "staging_retention": "PT24H",
  "destination_class": "user-selected-local-file",
  "sha256": "..."
}
```

---

# 393. Bundle Manifest Sketch

```json
{
  "schema": "opure.support-bundle-manifest/1",
  "bundle_id": "bundle-opaque",
  "bundle_definition": "standard:2",
  "product_version": "1.0.0",
  "channel": "stable",
  "created_at": "2026-07-18T18:30:00Z",
  "scope": {
    "project": "project-1",
    "workflow": "workflow-opaque"
  },
  "included_items": 143,
  "excluded_items": 27,
  "redaction_policy": "standard-support:3",
  "secret_scan": {
    "profile": "support-export:4",
    "unresolved_findings": 0
  },
  "diagnostic_collection": [],
  "manifest_sha256": "..."
}
```

---

# 394. Redaction Report Sketch

```json
{
  "schema": "opure.support-redaction-report/1",
  "bundle_id": "bundle-opaque",
  "profile": "standard-support:3",
  "actions": {
    "removed_fields": 84,
    "pseudonymised_values": 12,
    "normalised_paths": 33,
    "excluded_artefacts": 4,
    "truncated_values": 9
  },
  "secret_findings": {
    "confirmed": 2,
    "redacted": 2,
    "unresolved": 0
  },
  "sha256": "..."
}
```

---

# 395. Release Gate

Trust evidence and supportability are blocked when:

* the Trust Centre becomes authoritative for another service's decision;
* operational logs are the sole record of a critical decision or effect;
* an owner action can commit without its required authoritative record;
* owner evidence can be submitted without authenticated service identity;
* a model proposal can be displayed as an authoritative decision;
* a user assertion can be displayed as deterministic validation;
* evidence can cross project boundaries;
* evidence types can change authority without revision;
* conflicting duplicate records silently overwrite;
* owner sequence gaps are hidden;
* local hash chains are described as tamper proof or legal proof;
* Trust Centre completeness is not shown;
* logs can contain secrets by default;
* traces can contain source or prompt payloads by default;
* metrics accept unbounded project or request labels;
* diagnostic ports are exposed to plugins or MCP servers;
* process command lines are collected for ordinary support;
* crash dumps are enabled automatically;
* full or heap dumps are included without separate approval;
* a dump is described as secret free;
* support bundles include source, prompts, model outputs or command output by default;
* a support bundle copies whole service databases, the Vault or a project root;
* bundle redaction relies on one pattern scan;
* unresolved secret findings can be exported through the ordinary support flow;
* a final preview is generated from bytes different from those exported;
* an approval can survive a changed inventory, manifest or destination class;
* archive paths permit traversal, absolute names, case collisions, duplicates, symlinks or alternate streams;
* the generated archive is not reopened and verified;
* legacy ZIP encryption is presented as secure;
* a bundle uploads automatically;
* a support endpoint receives data without a separate approved design;
* staging persists indefinitely;
* a Preservation Hold can be bypassed by ordinary cleanup;
* deletion hides retained owner records or external copies;
* forensic deletion is promised;
* or the Trust Centre cannot explain authority, provenance, retention, redaction, omissions and evidence gaps.

---

# 396. Acceptance Criteria

This ADR may move to **Accepted** when all of the following hold.

## Architecture and Ownership

* [ ] Trust Evidence Service exists.
* [ ] Trust Centre is a read-only projection.
* [ ] Domain owner services remain authoritative.
* [ ] Owner records commit transactionally with owned actions.
* [ ] Required evidence outbox records commit with owner records.
* [ ] Trust Evidence ingestion uses authenticated service IPC.
* [ ] Trust Evidence does not authorise domain actions.
* [ ] Trust Evidence does not execute tools.
* [ ] Trust Evidence does not access Vault values.
* [ ] Trust Evidence does not select providers or models.
* [ ] Trust Evidence does not mutate workflows.
* [ ] Plugins cannot query general Trust evidence.
* [ ] MCP servers cannot submit direct Trust evidence.
* [ ] Stable, Preview and Development are isolated.
* [ ] Trust Evidence works offline.
* [ ] No automatic support upload exists.

## Evidence Type Definitions

* [ ] Evidence Type schema exists.
* [ ] Every type has a stable ID.
* [ ] Every type has an owner service.
* [ ] Every type has an Authority Class.
* [ ] Every type has a record schema.
* [ ] Every payload has a schema or explicit opaque policy.
* [ ] Safe index fields are explicit.
* [ ] Sensitive fields are explicit.
* [ ] Relationship types are explicit.
* [ ] Default retention class is explicit.
* [ ] Support-bundle eligibility is explicit.
* [ ] Redaction profile is explicit.
* [ ] Owner lookup contract is explicit.
* [ ] Owner reconciliation contract is explicit.
* [ ] Revisions are immutable.
* [ ] Type authority cannot change silently.
* [ ] Plugin diagnostic types remain plugin-provided observations.
* [ ] Unknown types quarantine.

## Evidence Records

* [ ] Evidence Record schema exists.
* [ ] Evidence IDs are opaque.
* [ ] Owner service is required.
* [ ] Owner record ID is required.
* [ ] Owner record revision is required.
* [ ] Authority Class is required.
* [ ] Occurred or observed time is required.
* [ ] Record hash is required.
* [ ] Project ID is required for project-scoped records.
* [ ] Operation ID is required where applicable.
* [ ] Workflow ID is required where applicable.
* [ ] Trace and span fields are supported.
* [ ] Runtime boot identity is supported.
* [ ] Payload classification is required.
* [ ] Retention class is required.
* [ ] Preservation state is represented.
* [ ] Inline payload size is bounded.
* [ ] Large payloads use references.
* [ ] Secret-prohibited fields are rejected.
* [ ] Record canonicalisation is deterministic.
* [ ] Duplicate matching records are idempotent.
* [ ] Duplicate conflicting records quarantine.

## Authority Classes

* [ ] Authoritative Domain Decision is implemented.
* [ ] Authoritative Domain Effect is implemented.
* [ ] Authoritative Domain State Transition is implemented.
* [ ] Verified Service Receipt is implemented.
* [ ] Verified External Receipt is implemented.
* [ ] Deterministic Validation Result is implemented.
* [ ] Human Decision is implemented.
* [ ] Derived Trust Projection is implemented.
* [ ] Operational Observation is implemented.
* [ ] Diagnostic Observation is implemented.
* [ ] Model-Generated Proposal is implemented.
* [ ] User-Provided Assertion is implemented.
* [ ] Imported Historical Evidence is implemented.
* [ ] Unknown or Unverified is implemented.
* [ ] Authority labels are visible.
* [ ] Models cannot elevate authority.
* [ ] Users cannot relabel assertions as deterministic.
* [ ] Imported evidence cannot become live authority without owner verification.

## Identity, Time and Ordering

* [ ] Actor kinds are typed.
* [ ] Actor identity is opaque.
* [ ] Email is excluded by default.
* [ ] Subject references are typed.
* [ ] Cross-project subjects are denied.
* [ ] `occurred_at_utc` is supported.
* [ ] `observed_at_utc` is supported.
* [ ] Missing source time is labelled.
* [ ] Owner sequence is supported.
* [ ] Ingestion sequence is supported.
* [ ] Runtime boot identity is supported.
* [ ] Supervisor process identity is supported.
* [ ] Sequence is preferred over timestamp for ordering.
* [ ] Clock rollback is visible.
* [ ] Clock time is not described as trusted.
* [ ] PID reuse does not merge identities.

## Integrity Evidence

* [ ] Payload SHA-256 is implemented.
* [ ] Record SHA-256 is implemented.
* [ ] Owner-stream hash chaining is available where selected.
* [ ] Genesis rules are explicit.
* [ ] Stream reset requires evidence.
* [ ] Tail verification runs.
* [ ] Full verification is available.
* [ ] Missing stream records are detected.
* [ ] Reordered records are detected.
* [ ] Altered payloads are detected.
* [ ] Chain gaps stop false completeness.
* [ ] Integrity failure creates a Trust warning.
* [ ] Hashes are described as local integrity evidence.
* [ ] No external non-repudiation claim exists.
* [ ] No trusted-timestamp claim exists.
* [ ] No legal chain-of-custody claim exists.
* [ ] Same-user limitations are documented.

## Ingestion and Deduplication

* [ ] Owner outbox schema exists.
* [ ] Domain record and outbox commit atomically.
* [ ] At-least-once delivery works.
* [ ] Trust inbox schema exists.
* [ ] Message IDs deduplicate.
* [ ] Payload hashes deduplicate.
* [ ] Service sessions authenticate.
* [ ] Owner identity is checked.
* [ ] Evidence Type revision is checked.
* [ ] Payload hash is checked.
* [ ] Sequence is checked.
* [ ] Hash chain is checked where enabled.
* [ ] Classification is applied.
* [ ] Relationships and indexes update transactionally.
* [ ] Retention Decision is created.
* [ ] Oversized messages require payload references.
* [ ] Owner mismatch creates security evidence.
* [ ] Another release channel is rejected.
* [ ] Plugin Host cannot impersonate owner service.
* [ ] MCP process cannot impersonate owner service.

## Evidence Relationships

* [ ] Relationship schema exists.
* [ ] Causes works.
* [ ] Caused By works.
* [ ] Authorises works.
* [ ] Authorised By works.
* [ ] Implements works.
* [ ] Uses works.
* [ ] Produces works.
* [ ] Supersedes works.
* [ ] Retries works.
* [ ] Reconciles works.
* [ ] Compensates works.
* [ ] Violates works.
* [ ] Resolves works.
* [ ] Derives From works.
* [ ] Belongs To works.
* [ ] Correlates With works.
* [ ] Relationship source is validated.
* [ ] Relationship target is validated.
* [ ] Owner is authorised.
* [ ] Cross-project links are denied.
* [ ] Authority escalation through links is denied.
* [ ] Derived relationships are labelled.
* [ ] Causal graph has an accessible table alternative.

## Evidence Completeness and Reconciliation

* [ ] Completeness schema exists.
* [ ] Complete is scoped.
* [ ] Complete for Requested Scope is supported.
* [ ] Projection Delayed is supported.
* [ ] Owner Unavailable is supported.
* [ ] Gap Detected is supported.
* [ ] Owner Record Missing is supported.
* [ ] Conflict is supported.
* [ ] Purged by Policy is supported.
* [ ] Redacted is supported.
* [ ] Unknown is supported.
* [ ] Expected owner sequences are tracked.
* [ ] Gaps are visible.
* [ ] Owner reconciliation can query sequence ranges.
* [ ] Owner hashes are verified.
* [ ] Missing records can be ingested.
* [ ] Deleted owner records are represented.
* [ ] Conflicting owner hashes do not overwrite.
* [ ] Projection rebuild works.
* [ ] Trust database rebuild is possible from available owners.
* [ ] Owner loss is visible.
* [ ] Missing projection does not imply no action.

## Trust Centre Queries

* [ ] Typed query schema exists.
* [ ] Channel filtering works.
* [ ] Project filtering works.
* [ ] Operation filtering works.
* [ ] Workflow filtering works.
* [ ] Evidence Type filtering works.
* [ ] Authority filtering works.
* [ ] Outcome filtering works.
* [ ] Time filtering works.
* [ ] Preservation filtering works.
* [ ] Safe text search works.
* [ ] Cursor pagination works.
* [ ] Time range is bounded.
* [ ] Raw SQL is denied.
* [ ] Arbitrary expressions are denied.
* [ ] Cross-project search is denied.
* [ ] Query snapshot time is shown.
* [ ] Projection generation is shown.
* [ ] Owner freshness is shown.
* [ ] Redaction count is shown.
* [ ] Evidence completeness is shown.

## Trust Centre Views

* [ ] Overview exists.
* [ ] Projects view exists.
* [ ] Operations view exists.
* [ ] AI and Models view exists.
* [ ] Data Sharing view exists.
* [ ] Context view exists.
* [ ] Knowledge view exists.
* [ ] Memory view exists.
* [ ] Workflows view exists.
* [ ] Approvals view exists.
* [ ] Tools and Commands view exists.
* [ ] Patches and Writes view exists.
* [ ] Builds and Tests view exists.
* [ ] Plugins view exists.
* [ ] MCP view exists.
* [ ] Network view exists.
* [ ] Configuration and Policy view exists.
* [ ] Secret Use view exists.
* [ ] Updates and Installation view exists.
* [ ] Security and Privacy view exists.
* [ ] Retention and Deletion view exists.
* [ ] Preservation view exists.
* [ ] Support Bundles view exists.
* [ ] System Health view exists.
* [ ] Owner-unavailable state is visible.
* [ ] Evidence-gap state is visible.
* [ ] Authority is not represented by colour alone.

## Authoritative Coverage

* [ ] Application start and shutdown receipts exist.
* [ ] Service readiness receipts exist.
* [ ] Project open and close receipts exist.
* [ ] Configuration snapshot receipts exist.
* [ ] Policy conflict receipts exist.
* [ ] Capability decisions exist.
* [ ] Secret-use receipts exist without values.
* [ ] Context Plan receipts exist.
* [ ] Data Sharing Plan receipts exist.
* [ ] Routing Decision receipts exist.
* [ ] Remote provider request receipts exist.
* [ ] Local model request receipts exist.
* [ ] Model identity evidence exists.
* [ ] Model fallback evidence exists.
* [ ] Indexing receipts exist.
* [ ] Knowledge query receipts exist.
* [ ] Memory lifecycle receipts exist.
* [ ] Workflow transition receipts exist.
* [ ] Workflow effect receipts exist.
* [ ] Workflow recovery receipts exist.
* [ ] Tool call receipts exist.
* [ ] Command execution receipts exist.
* [ ] Patch application receipts exist.
* [ ] Workspace write receipts exist.
* [ ] Repository write receipts exist.
* [ ] Build and test receipts exist.
* [ ] Plugin trust and execution receipts exist.
* [ ] MCP call receipts exist.
* [ ] Network Gateway receipts exist.
* [ ] Update and installer receipts exist.
* [ ] Package and model installation receipts exist.
* [ ] Retention and deletion receipts exist.
* [ ] Support-bundle receipts exist.

## Operational Logs

* [ ] Operational logs are separate from authoritative evidence.
* [ ] Log schema maps to OpenTelemetry concepts.
* [ ] Occurred timestamp is supported.
* [ ] Observed timestamp is supported.
* [ ] Trace ID is supported.
* [ ] Span ID is supported.
* [ ] Severity number is supported.
* [ ] Severity text is supported.
* [ ] Event name is stable.
* [ ] Resource identity is safe.
* [ ] Instrumentation scope is represented.
* [ ] Attributes are bounded.
* [ ] Free-form body is secondary.
* [ ] Structured high-value events exist.
* [ ] Log rotation works.
* [ ] Logs are retained locally.
* [ ] Telemetry export is disabled by default.
* [ ] Source code is excluded.
* [ ] Prompts are excluded.
* [ ] Model responses are excluded.
* [ ] File contents are excluded.
* [ ] Secrets are excluded.
* [ ] Headers are excluded.
* [ ] Full command lines are excluded.
* [ ] Environment variables are excluded.
* [ ] Clipboard and keystrokes are excluded.
* [ ] Exception Data dictionaries are excluded.
* [ ] Log injection tests pass.
* [ ] Redaction occurs before persistence.
* [ ] Canary scans run after persistence.

## Traces and Metrics

* [ ] Traces record operation boundaries.
* [ ] Traces record service calls.
* [ ] Traces record queue and execution time.
* [ ] Traces record safe result class.
* [ ] Traces do not record full payloads.
* [ ] Trace propagation works across IPC.
* [ ] Project paths are excluded or templated.
* [ ] Metrics use low-cardinality labels.
* [ ] Project ID is not a metric label.
* [ ] User ID is not a metric label.
* [ ] Workflow ID is not a metric label.
* [ ] Request ID is not a metric label.
* [ ] File path is not a metric label.
* [ ] Provider request ID is not a metric label.
* [ ] Cardinality budgets are enforced.
* [ ] Raw metric sample retention is bounded.
* [ ] Aggregated metric retention is bounded.

## Diagnostic Sessions

* [ ] Diagnostic Session schema exists.
* [ ] Purpose is required.
* [ ] Target process is exact.
* [ ] Runtime boot identity is exact.
* [ ] Provider allowlist is explicit.
* [ ] Duration is explicit.
* [ ] Buffer limit is explicit.
* [ ] Output limit is explicit.
* [ ] Approval is explicit.
* [ ] Cancellation is supported.
* [ ] Session state is durable.
* [ ] Diagnostic collector is supervised.
* [ ] Plugins cannot access diagnostic ports.
* [ ] MCP servers cannot access diagnostic ports.
* [ ] Process command-line collection is disabled.
* [ ] Session overhead is measured.
* [ ] Diagnostic artefact retention is applied.

## EventPipe and GC Diagnostics

* [ ] EventPipe providers are allowlisted.
* [ ] EventPipe keywords are recorded.
* [ ] EventPipe duration is bounded.
* [ ] EventPipe buffers are bounded.
* [ ] Dropped events are reported.
* [ ] EventPipe cancellation works.
* [ ] EventPipe artefacts are hashed.
* [ ] GC dump collection is explicit.
* [ ] GC dump target is exact.
* [ ] GC collection side effect is disclosed.
* [ ] GC dump retention is short.
* [ ] GC dump is not included by default.

## Process Dumps

* [ ] Dump collection is disabled by default.
* [ ] Mini dump is supported where selected.
* [ ] Triage dump is supported where selected.
* [ ] Heap dump requires advanced policy.
* [ ] Full dump requires advanced policy.
* [ ] Dump target is exact.
* [ ] Dump type is displayed.
* [ ] Estimated size is displayed.
* [ ] Possible sensitive content is displayed.
* [ ] Retention is displayed.
* [ ] Bundle inclusion is displayed.
* [ ] Dump approval is separate.
* [ ] Changed dump plan invalidates approval.
* [ ] Incomplete dump is deleted or quarantined.
* [ ] Dump is hashed.
* [ ] Dump is classified high risk.
* [ ] Binary dump redaction is not promised.
* [ ] Triage is not described as secret free.
* [ ] Automatic crash dump is absent.
* [ ] Automatic full dump is absent.
* [ ] Automatic heap dump is absent.
* [ ] Automatic dump upload is absent.
* [ ] Dump secret-canary tests pass.

## System Inventory

* [ ] Windows edition and build are included.
* [ ] Architecture is included.
* [ ] CPU model and counts are included where relevant.
* [ ] RAM is included.
* [ ] GPU and driver are included where relevant.
* [ ] Opure-volume free space is included.
* [ ] .NET runtime version is included.
* [ ] Opure package identity is included.
* [ ] Selected service versions are included.
* [ ] Device serial numbers are excluded.
* [ ] Full machine name is excluded by default.
* [ ] Domain name is excluded by default.
* [ ] Account name is excluded.
* [ ] Installed application inventory is excluded.
* [ ] Network addresses are excluded.
* [ ] Wi-Fi profiles are excluded.
* [ ] Browser information is excluded.
* [ ] Unrelated environment variables are excluded.

## Retention Policies

* [ ] Retention Policy schema exists.
* [ ] Evidence class is considered.
* [ ] Purpose is considered.
* [ ] Scope is considered.
* [ ] Classification is considered.
* [ ] Unresolved state is considered.
* [ ] Preservation state is considered.
* [ ] Owner dependency is considered.
* [ ] Enterprise policy is considered.
* [ ] User preference is considered within policy.
* [ ] Storage pressure cannot shorten protected records.
* [ ] Ephemeral class exists.
* [ ] Operational Debug class exists.
* [ ] Operational Standard class exists.
* [ ] Performance Diagnostic class exists.
* [ ] Authoritative Operation class exists.
* [ ] Security Critical class exists.
* [ ] Recovery Critical class exists.
* [ ] Release Evidence class exists.
* [ ] Incident Evidence class exists.
* [ ] Support Staging class exists.
* [ ] User Export class exists.
* [ ] Preservation class exists.
* [ ] Default periods are implemented provisionally.
* [ ] Daily retention review runs.
* [ ] Failed retention deletion is visible.
* [ ] Retention settings follow ADR-0026 policy.

## Retention Decisions

* [ ] Retention Decision schema exists.
* [ ] Policy revisions are bound.
* [ ] Purpose is recorded.
* [ ] Classification is recorded.
* [ ] Base expiry is recorded.
* [ ] Dependency extension is recorded.
* [ ] Hold extension is recorded.
* [ ] Effective expiry is recorded.
* [ ] Deletion method is recorded.
* [ ] Reason is recorded.
* [ ] Decision hash is canonical.
* [ ] Active workflow extends necessary evidence.
* [ ] Unresolved Outcome Unknown extends evidence.
* [ ] Incident extends evidence.
* [ ] Hold overrides ordinary expiry.
* [ ] More permissive user preference cannot bypass required evidence.
* [ ] Longer retention is not automatic merely due to storage capacity.
* [ ] Retention conflicts are explicit.

## Deletion

* [ ] Deletion Plan schema exists.
* [ ] Owner records are identified.
* [ ] Trust records are identified.
* [ ] Artefacts are identified.
* [ ] Dependencies are identified.
* [ ] Holds are identified.
* [ ] Retention Decisions are bound.
* [ ] Method is explicit.
* [ ] Preview is explicit.
* [ ] Approval is explicit where required.
* [ ] Projection deletion is distinct.
* [ ] Payload deletion is distinct.
* [ ] Owner-record deletion is distinct.
* [ ] CAS garbage collection is distinct.
* [ ] Exported-file deletion is distinct.
* [ ] Deletion ordering is defined.
* [ ] Owner-retained state is visible.
* [ ] External copies warning is visible.
* [ ] Deletion receipts contain no deleted content.
* [ ] CAS shared references are protected.
* [ ] CAS GC recovers after crash.
* [ ] Forensic erasure is not promised.
* [ ] SSD and backup limitations are displayed.

## Preservation Holds

* [ ] Preservation Hold schema exists.
* [ ] Security Incident hold exists.
* [ ] Recovery Investigation hold exists.
* [ ] Support Investigation hold exists.
* [ ] Release Investigation hold exists.
* [ ] User Requested hold exists.
* [ ] Enterprise Administrative hold exists.
* [ ] Recorded External Legal Instruction type is labelled cautiously.
* [ ] Hold purpose is required.
* [ ] Hold scope is required.
* [ ] Hold creator is recorded.
* [ ] Hold review date is recorded.
* [ ] Release authority is recorded.
* [ ] Hold hash is canonical.
* [ ] Hold preview exists.
* [ ] Storage estimate exists.
* [ ] Owner references are frozen.
* [ ] Payload copies are made only where needed.
* [ ] Preserved copies are hashed.
* [ ] Access is restricted.
* [ ] Preserved payload access is recorded.
* [ ] Ordinary deletion cannot bypass a hold.
* [ ] CAS GC cannot bypass a hold.
* [ ] Hold release is explicit.
* [ ] Retention recalculates after release.
* [ ] Application hold is not marketed as legal compliance.

## Incident Evidence

* [ ] Incident schema exists.
* [ ] Incident state machine exists.
* [ ] Severity exists.
* [ ] Affected projects are represented.
* [ ] Affected services are represented.
* [ ] Detection and reporting times are represented.
* [ ] Owners are represented.
* [ ] Evidence links are typed.
* [ ] Containment is represented.
* [ ] Recovery is represented.
* [ ] Lessons are represented.
* [ ] Incident evidence hash is verified.
* [ ] Model summaries remain derived.
* [ ] Incident export is separate from ordinary support.
* [ ] Incident response aligns with product risk management.
* [ ] Opure does not provide legal advice.

## Bundle Definitions and Presets

* [ ] Support Bundle Definition schema exists.
* [ ] Minimal preset exists.
* [ ] Standard preset exists.
* [ ] Performance preset exists.
* [ ] Crash Triage preset exists.
* [ ] Workflow Recovery preset exists.
* [ ] Provider and AI preset exists.
* [ ] Plugin and MCP preset exists.
* [ ] Security Incident Export exists as a separate advanced flow.
* [ ] Custom preset exists.
* [ ] Every preset is versioned.
* [ ] Eligible evidence types are explicit.
* [ ] Telemetry policy is explicit.
* [ ] Diagnostic policy is explicit.
* [ ] System inventory policy is explicit.
* [ ] Source-content policy is explicit.
* [ ] Prompt policy is explicit.
* [ ] Tool-result policy is explicit.
* [ ] Dump policy is explicit.
* [ ] Redaction profile is explicit.
* [ ] Secret scan profile is explicit.
* [ ] Time range is bounded.
* [ ] Maximum size is bounded.
* [ ] Staging retention is explicit.
* [ ] Custom bundle cannot broaden policy.

## Minimal and Standard Bundles

* [ ] Minimal includes product version.
* [ ] Minimal includes channel.
* [ ] Minimal includes package identity.
* [ ] Minimal includes OS and .NET version.
* [ ] Minimal includes service versions.
* [ ] Minimal includes safe configuration provenance.
* [ ] Minimal includes health.
* [ ] Minimal includes bounded recent errors.
* [ ] Minimal includes relevant failure receipts.
* [ ] Minimal includes manifest.
* [ ] Minimal excludes source.
* [ ] Minimal excludes prompts.
* [ ] Minimal excludes model output.
* [ ] Minimal excludes command output.
* [ ] Minimal excludes dumps.
* [ ] Minimal excludes raw traces.
* [ ] Minimal excludes project names by default.
* [ ] Standard adds selected evidence.
* [ ] Standard adds bounded logs.
* [ ] Standard adds bounded traces.
* [ ] Standard still excludes dumps by default.
* [ ] Standard still excludes source by default.

## Bundle Plans and Collection

* [ ] Support Bundle Plan schema exists.
* [ ] Bundle Definition revision is bound.
* [ ] Purpose is required.
* [ ] Scope is exact.
* [ ] Time range is exact.
* [ ] Project selection is exact.
* [ ] Operation selection is exact.
* [ ] Evidence selectors are exact.
* [ ] Telemetry selectors are exact.
* [ ] Diagnostic sessions are exact.
* [ ] Content inclusions are explicit.
* [ ] Redaction profile is exact.
* [ ] Secret scan profile is exact.
* [ ] Size limit is exact.
* [ ] Staging retention is exact.
* [ ] Destination class is exact.
* [ ] Plan hash is canonical.
* [ ] Plan expiry is enforced.
* [ ] Material changes invalidate approval.
* [ ] Owner-service collection contracts are used.
* [ ] Whole databases are not copied.
* [ ] Whole log directories are not copied.
* [ ] Vault is not copied.
* [ ] Project root is not copied.
* [ ] User profile is not copied.
* [ ] Unrelated process inventory is not collected.
* [ ] Collection capability is one time and bounded.
* [ ] Collection omissions are manifested.

## Staging

* [ ] Staging location is service owned.
* [ ] Staging ACLs are restrictive.
* [ ] Plugin access is denied.
* [ ] MCP access is denied.
* [ ] Ordinary indexing is denied.
* [ ] Project Knowledge indexing is denied.
* [ ] Project Memory indexing is denied.
* [ ] Cloud-sync warning is supported.
* [ ] Random bundle directory is used.
* [ ] Incomplete files are marked.
* [ ] Staging expires after 24 hours by default.
* [ ] Export can trigger earlier deletion.
* [ ] Preservation is explicit.
* [ ] Crash recovery removes invalid partials.

## Redaction

* [ ] Redaction Profile schema exists.
* [ ] Schema-level exclusion works.
* [ ] Field classification redaction works.
* [ ] Owner-service redaction works.
* [ ] Trust redaction works.
* [ ] Path normalisation works.
* [ ] Identity pseudonymisation works.
* [ ] Vault references are handled safely.
* [ ] Known canary removal works.
* [ ] Pattern scan works.
* [ ] Entropy scan is supplementary.
* [ ] Structural validation follows redaction.
* [ ] Final user preview is required.
* [ ] Remove Field works.
* [ ] Fixed Token works.
* [ ] Bundle-Local Pseudonym works.
* [ ] Length replacement works.
* [ ] Hash where safe works.
* [ ] Truncation works.
* [ ] Metadata Only works.
* [ ] Selected Range works.
* [ ] Exclude Artefact works.
* [ ] Low-entropy sensitive values are not hashed.
* [ ] Owner and Trust redaction choose the stricter result.
* [ ] Redaction actions are reported.
* [ ] Redacted data is not recoverable from the bundle.

## Path and Identity Protection

* [ ] Project root becomes `<PROJECT_ROOT>`.
* [ ] User profile becomes `<USER_PROFILE>`.
* [ ] Opure data becomes `<OPURE_DATA>`.
* [ ] Temp becomes `<TEMP>`.
* [ ] Worktree becomes `<WORKTREE>`.
* [ ] Model store becomes `<MODEL_STORE>`.
* [ ] Plugin store becomes `<PLUGIN_STORE>`.
* [ ] Replacement ordering is deterministic.
* [ ] Project-relative paths are included only when approved.
* [ ] Unrelated absolute paths are removed.
* [ ] User name is pseudonymised.
* [ ] Machine name is pseudonymised.
* [ ] Project display name is pseudonymised.
* [ ] Repository remote is removed or pseudonymised.
* [ ] Email is pseudonymised.
* [ ] External account display names are pseudonymised.
* [ ] Pseudonyms are stable within one bundle.
* [ ] Pseudonyms differ across bundles by default.
* [ ] Mapping is not exported by default.
* [ ] Identity categories are visible in preview.

## Source, Prompts and Tool Content

* [ ] Source content is excluded by default.
* [ ] Whole source files are not exported by default.
* [ ] Selected source snippet requires exact file and lines.
* [ ] Selected source snippet binds base hash.
* [ ] Selected source snippet is scanned.
* [ ] Generated patch inclusion is explicit.
* [ ] Prompts are metadata only by default.
* [ ] Prompt snippet requires explicit selection.
* [ ] Model output is metadata only by default.
* [ ] Model output snippet requires explicit selection.
* [ ] Hidden model reasoning is never included.
* [ ] Tool result is metadata only by default.
* [ ] Raw stdout is excluded by default.
* [ ] Raw stderr is excluded by default.
* [ ] Process environment is excluded.
* [ ] Secret-bearing headers are excluded.
* [ ] Changed content invalidates preview approval.

## Secret Scanning

* [ ] Secret Scanner registry exists.
* [ ] Exact Canary scanner exists.
* [ ] Vault Reference scanner exists.
* [ ] Pattern scanner exists.
* [ ] Structured Credential scanner exists.
* [ ] Private Key scanner exists.
* [ ] Entropy scanner exists.
* [ ] Scanners are versioned.
* [ ] Scanner outputs do not store the secret.
* [ ] Opure canaries are recognised.
* [ ] Provider token shapes are recognised.
* [ ] Private-key headers are recognised.
* [ ] Bearer tokens are recognised.
* [ ] Credential connection strings are recognised.
* [ ] Cloud credential shapes are recognised.
* [ ] Password assignment patterns are recognised.
* [ ] High-entropy findings are reviewed.
* [ ] False positives can be recorded.
* [ ] Ordinary export blocks unresolved findings.
* [ ] Incident export has a separately approved exception path.
* [ ] Scanner evasion tests pass.
* [ ] Scanning is not described as a guarantee.

## Final Preview and Approval

* [ ] Preview is generated from final staged bytes.
* [ ] Bundle size is shown.
* [ ] Every file is shown.
* [ ] Every classification is shown.
* [ ] Source-content presence is shown.
* [ ] Prompt presence is shown.
* [ ] Model-output presence is shown.
* [ ] Dump presence is shown.
* [ ] Path categories are shown.
* [ ] Identity categories are shown.
* [ ] Secret findings are shown.
* [ ] Redaction count is shown.
* [ ] Omitted items are shown.
* [ ] Diagnostic side effects are shown.
* [ ] Staging retention is shown.
* [ ] Destination class is shown.
* [ ] Safe text files can be inspected.
* [ ] Binary dump metadata is shown.
* [ ] Approval binds plan hash.
* [ ] Approval binds manifest hash.
* [ ] Approval binds inventory hash.
* [ ] Approval binds classifications.
* [ ] Approval binds dump presence.
* [ ] Approval binds source presence.
* [ ] Approval binds secret-scan state.
* [ ] Approval binds destination class.
* [ ] Approval expires.
* [ ] Approval cannot be reused.
* [ ] Any final-byte change invalidates approval.

## ZIP Archive

* [ ] Container is standard ZIP.
* [ ] Extension is `.opure-support.zip`.
* [ ] Manifest exists.
* [ ] README exists.
* [ ] Inventory exists.
* [ ] Consent record exists.
* [ ] Redaction report exists.
* [ ] Hash inventory exists.
* [ ] Omission report exists.
* [ ] Entry paths are relative.
* [ ] Absolute paths are denied.
* [ ] Drive paths are denied.
* [ ] UNC paths are denied.
* [ ] Device paths are denied.
* [ ] Traversal is denied.
* [ ] Dot segments are denied.
* [ ] Alternate streams are denied.
* [ ] Reserved device names are denied.
* [ ] Duplicate paths are denied.
* [ ] Case collisions are denied.
* [ ] Symlinks are denied.
* [ ] Reparse entries are denied.
* [ ] Entry count is bounded.
* [ ] File size is bounded.
* [ ] Total size is bounded.
* [ ] Compression ratio is checked.
* [ ] Deflate and Store are supported.
* [ ] Legacy ZIP encryption is not used.
* [ ] Unencrypted-bundle warning is explicit.
* [ ] Archive is reopened.
* [ ] Every entry hash is verified.
* [ ] Manifest is verified.
* [ ] Inventory is verified.
* [ ] Archive SHA-256 is calculated.
* [ ] Verification failure quarantines.

## Export

* [ ] Trusted file picker is used.
* [ ] ADR-0009 path validation applies.
* [ ] Staged file is flushed.
* [ ] Atomic replacement is used where safe.
* [ ] Existing-file replacement is explicit.
* [ ] Removable-drive warning exists.
* [ ] Network-destination policy exists.
* [ ] Cloud-synchronised-folder warning exists.
* [ ] Destination class is recorded.
* [ ] Raw export path is not recorded.
* [ ] Export Receipt schema exists.
* [ ] Archive hash is in the receipt.
* [ ] Archive size is in the receipt.
* [ ] Export actor is in the receipt.
* [ ] Staging deletion due time is in the receipt.
* [ ] Exported archive becomes user owned.
* [ ] Opure deletion action can target the exact selected file.
* [ ] Copies outside Opure control are disclosed.
* [ ] No network request occurs during ordinary export.
* [ ] No automatic upload occurs.
* [ ] No hidden upload retry occurs.
* [ ] Future upload requires a separate accepted design.

## Bundle Lifecycle and Recovery

* [ ] Draft state works.
* [ ] Planning state works.
* [ ] Approval Required works.
* [ ] Collecting works.
* [ ] Redacting works.
* [ ] Scanning works.
* [ ] Preview Ready works.
* [ ] Export Approved works.
* [ ] Writing works.
* [ ] Verifying works.
* [ ] Exported works.
* [ ] Failed works.
* [ ] Cancelled works.
* [ ] Quarantined works.
* [ ] Expired works.
* [ ] Deleted works.
* [ ] Cancellation stops new collection.
* [ ] EventPipe cancellation works.
* [ ] Non-cancellable dump state is visible.
* [ ] Incomplete outputs are deleted or quarantined.
* [ ] Runtime restart discovers non-terminal bundles.
* [ ] Stale approvals expire after restart.
* [ ] Staged bytes are revalidated.
* [ ] Deterministic redaction can resume.
* [ ] Secret scanning can resume.
* [ ] Fresh export approval is required.
* [ ] Recovery never uploads.

## Plugin and MCP Evidence

* [ ] Plugin diagnostic namespace is enforced.
* [ ] Plugin event schema is registered.
* [ ] Plugin event size is bounded.
* [ ] Plugin event rate is bounded.
* [ ] Plugin events are classified.
* [ ] Plugin observations are not first-party authority.
* [ ] Plugin cannot include a direct file.
* [ ] Private plugin data requires first-party adapter and approval.
* [ ] MCP evidence is generated by MCP Gateway.
* [ ] MCP server cannot submit Trust records.
* [ ] MCP raw results are excluded by default.
* [ ] MCP credentials are excluded.
* [ ] Plugin and MCP adversarial tests pass.

## Provider, Model, Knowledge and Memory Evidence

* [ ] Provider receipt records endpoint class.
* [ ] Provider receipt records region.
* [ ] Provider receipt records model identity.
* [ ] Provider receipt records request ID safely.
* [ ] Provider receipt records status.
* [ ] Provider receipt records usage.
* [ ] Provider receipt records latency.
* [ ] Provider receipt records cost.
* [ ] Provider prompt is separate.
* [ ] Local model receipt records Runtime Package.
* [ ] Local model receipt records model hash.
* [ ] Local model receipt records Execution Profile.
* [ ] Local model receipt records resource profile.
* [ ] Knowledge evidence records query plan and generations.
* [ ] Knowledge result content is excluded.
* [ ] Memory evidence records lifecycle.
* [ ] Memory content is excluded.
* [ ] Model hidden reasoning is never evidence.

## Trust Centre Access and Export

* [ ] Sensitive preserved-payload access is recorded.
* [ ] Ordinary summary viewing avoids excessive access logging.
* [ ] General evidence export uses the support-bundle pipeline.
* [ ] Safe table CSV is bounded.
* [ ] Sensitive evidence uses bundles.
* [ ] Deletion UI shows dependencies.
* [ ] Deletion UI shows holds.
* [ ] Deletion UI shows retained owners.
* [ ] Deletion UI shows exported-copy warning.
* [ ] Retention UI shows policy and next deletion.
* [ ] User hold actions are available.
* [ ] Security and enterprise holds are restricted.
* [ ] Trust Centre health shows ingestion lag.
* [ ] Trust Centre health shows owner gaps.
* [ ] Trust Centre health shows conflicts.
* [ ] Trust Centre health shows integrity failures.
* [ ] Trust Centre health shows retention backlog.
* [ ] Trust Centre health shows failed deletion.
* [ ] Trust Centre health shows failed scans.
* [ ] Trust Centre health shows stale staging.

## Privacy, Security and Accessibility

* [ ] Evidence inventory is available.
* [ ] Purpose is recorded.
* [ ] Classification is recorded.
* [ ] Owner is recorded.
* [ ] Retention is recorded.
* [ ] Deletion is recorded.
* [ ] Export is recorded.
* [ ] Personal data is minimised.
* [ ] Support bundles remain local by default.
* [ ] Dumps are high risk.
* [ ] Same-user limitations are documented.
* [ ] UI is keyboard accessible.
* [ ] UI supports Narrator.
* [ ] UI supports high contrast.
* [ ] Causal graphs have text alternatives.
* [ ] Authority is not colour only.
* [ ] Severity is not colour only.
* [ ] Dump warnings are understandable.
* [ ] Retention explanations are understandable.
* [ ] Integrity limitations are understandable.
* [ ] Security review is complete.
* [ ] Privacy review is complete.
* [ ] Supportability review is complete.
* [ ] Founder approval is recorded.

---

# 397. Evidence Required Before Acceptance

* [ ] Trust Evidence Service contract.
* [ ] owner evidence contract.
* [ ] Evidence Type schema.
* [ ] Evidence Record schema.
* [ ] Authority Class catalogue.
* [ ] relationship schema.
* [ ] completeness schema.
* [ ] Trust query schema.
* [ ] operational log schema.
* [ ] diagnostic-session schema.
* [ ] retention-policy schema.
* [ ] retention-decision schema.
* [ ] deletion-plan schema.
* [ ] Preservation Hold schema.
* [ ] incident schema.
* [ ] Support Bundle Definition schema.
* [ ] Support Bundle Plan schema.
* [ ] Bundle Manifest schema.
* [ ] Redaction Profile schema.
* [ ] Secret Scanner schema.
* [ ] Export Receipt schema.
* [ ] initial Evidence Type catalogue.
* [ ] owner transactional-outbox report.
* [ ] service-authentication report.
* [ ] evidence-ingestion report.
* [ ] duplicate-conflict report.
* [ ] owner-sequence report.
* [ ] stream-hash report.
* [ ] integrity-claims review.
* [ ] evidence-relationship report.
* [ ] causal-graph accessibility report.
* [ ] evidence-completeness report.
* [ ] owner-gap reconciliation report.
* [ ] projection-rebuild report.
* [ ] Trust-database rebuild report.
* [ ] Trust Centre overview.
* [ ] operation causal timeline.
* [ ] AI and Data Sharing view.
* [ ] Workflow Outcome Unknown view.
* [ ] Tool and Patch view.
* [ ] Configuration and Policy view.
* [ ] Plugin and MCP view.
* [ ] structured-log report.
* [ ] log-injection report.
* [ ] early-redaction report.
* [ ] post-persistence canary report.
* [ ] trace-payload leakage report.
* [ ] metric-cardinality report.
* [ ] diagnostic-collector isolation report.
* [ ] EventPipe provider allowlist.
* [ ] EventPipe overhead report.
* [ ] EventPipe cancellation report.
* [ ] GC dump side-effect report.
* [ ] dump-type report.
* [ ] triage-dump privacy review.
* [ ] dump secret-canary report.
* [ ] no-automatic-dump report.
* [ ] no-process-command-line report.
* [ ] system-inventory privacy report.
* [ ] default-retention report.
* [ ] Retention Decision report.
* [ ] retention-conflict report.
* [ ] evidence-inventory report.
* [ ] deletion dependency report.
* [ ] CAS garbage-collection report.
* [ ] forensic-deletion wording review.
* [ ] Preservation Hold report.
* [ ] hold access report.
* [ ] hold-bypass report.
* [ ] incident evidence report.
* [ ] Minimal bundle.
* [ ] Standard bundle.
* [ ] Performance bundle.
* [ ] Crash Triage bundle.
* [ ] Workflow Recovery bundle.
* [ ] Provider and AI bundle.
* [ ] Plugin and MCP bundle.
* [ ] custom-bundle policy report.
* [ ] owner collection-snapshot report.
* [ ] staging ACL report.
* [ ] staging indexing-exclusion report.
* [ ] staging expiry report.
* [ ] path pseudonymisation report.
* [ ] identity pseudonymisation report.
* [ ] source-content opt-in report.
* [ ] prompt and output opt-in report.
* [ ] command-output opt-in report.
* [ ] secret scanner report.
* [ ] canary coverage report.
* [ ] scanner-evasion report.
* [ ] false-positive review report.
* [ ] final-byte preview report.
* [ ] stale-preview approval report.
* [ ] ZIP path-security report.
* [ ] ZIP case-collision report.
* [ ] ZIP symlink report.
* [ ] ZIP size and bomb report.
* [ ] unencrypted-bundle warning review.
* [ ] archive reopen and verification report.
* [ ] staged atomic export report.
* [ ] export destination report.
* [ ] no-automatic-upload network capture.
* [ ] bundle cancellation report.
* [ ] bundle crash-recovery report.
* [ ] bundle quarantine report.
* [ ] plugin diagnostic quota report.
* [ ] plugin authority-escalation report.
* [ ] MCP evidence-forgery report.
* [ ] provider receipt report.
* [ ] local model receipt report.
* [ ] Knowledge and Memory exclusion report.
* [ ] Trust access-record report.
* [ ] retention and deletion UI.
* [ ] support-bundle UI.
* [ ] diagnostics leakage report.
* [ ] evidence-forgery adversarial report.
* [ ] cross-project adversarial report.
* [ ] secret-leak adversarial report.
* [ ] dump adversarial report.
* [ ] archive adversarial report.
* [ ] hold-bypass adversarial report.
* [ ] plugin and MCP adversarial report.
* [ ] performance report.
* [ ] scale report.
* [ ] endurance report.
* [ ] accessibility report.
* [ ] security review.
* [ ] privacy review.
* [ ] supportability review.
* [ ] founder approval.

---

# 398. Known Limitations

* The initial Evidence Type catalogue is not final.
* Not every domain service contract is implemented.
* Trust Centre projections can lag.
* Owner services can be unavailable.
* Owner data may be purged before a late projection repair if policy is incorrect.
* Local hash chains are not externally anchored.
* Local timestamps are not trusted timestamps.
* Same-user malware can access user-owned evidence.
* A fully compromised service can emit false records.
* Evidence completeness is always scoped.
* Operational logs can still omit useful detail.
* Strict redaction can reduce diagnostic value.
* Secret scans have false positives.
* Secret scans have false negatives.
* Encoded or fragmented secrets may evade detection.
* A process dump may contain any memory-resident content.
* Triage dumps are not guaranteed secret free.
* Binary dump redaction is not generally available.
* EventPipe collection changes performance.
* GC dump collection triggers GC-related work.
* Windows Event Log integration is narrow.
* Windows Error Reporting integration is deferred.
* Support bundles are unencrypted in Version 1.
* Secure sharing is outside the initial product.
* Automatic support upload is unavailable.
* Remote support is unavailable.
* A user can move or copy an exported bundle.
* Opure cannot delete external copies.
* Opure cannot guarantee forensic erasure.
* SSD wear levelling and filesystem journals are outside Opure control.
* Backups may retain prior records.
* Preservation Holds are not legal advice.
* Application holds may not satisfy legal discovery requirements.
* Incident exports require organisational judgement.
* Broad support collection can still reveal contextual metadata.
* Bundle-local pseudonyms may still be re-identifiable from context.
* Low-entropy hashes are unsafe for pseudonymisation.
* Enterprise retention requirements can conflict with privacy preferences.
* The default retention periods are provisional.
* One Trust Evidence SQLite writer may eventually require sharding.
* One billion relationships is only a stress target.
* Large Trust Centre histories require aggregation.
* Full-text search is restricted to safe fields.
* External SIEM integration is deferred.
* External evidence signing is deferred.
* Encrypted bundle format is deferred.
* Legal chain-of-custody features are deferred.
* The initial performance targets require evidence.

---

# 399. Open Questions

* Which exact SQLite release should Trust Evidence pin?
* Should Trust Evidence use one database per channel or per project?
* Would project databases simplify deletion?
* How would cross-project system evidence be represented?
* Should incidents have a separate database?
* Should preserved evidence have a separate encrypted store?
* Which data require encryption at rest beyond user ACLs?
* Should evidence CAS use the Vault root or a separate evidence key?
* How are evidence encryption keys rotated?
* How are preserved copies restored?
* Which owner records must always emit Trust evidence?
* Which owner records are too noisy for Trust Centre?
* What is the threshold between authoritative record and telemetry?
* Should every denied capability produce a record?
* How are repeated identical denials aggregated?
* Which Evidence Types belong in Version 1?
* Which Authority Classes are final?
* Should Verified External Receipt be split by verification strength?
* How are provider receipts graded?
* How are human decisions authenticated initially?
* How are future enterprise identities represented?
* Which user assertions are useful?
* Can a user attach explanatory notes?
* How are notes classified?
* Can notes contain source?
* How are notes redacted in bundles?
* Which opaque ID format is selected?
* Which canonical JSON implementation is selected?
* Which inline payload threshold is appropriate?
* Is 64 KiB too large?
* How are owner payload references resolved after migration?
* Can an owner move a payload from database to CAS?
* How are reference revisions represented?
* How are owner deletions reconciled?
* Should Trust retain a safe copy of every critical receipt?
* Which receipts need payload duplication for recovery?
* How are owner-service schema changes coordinated?
* Can owner services emit old Evidence Type revisions after update?
* How long must old schema readers remain?
* How are unknown future records displayed?
* Which owner streams use hash chaining?
* Is one stream per service sufficient?
* Should workflows have their own stream?
* Should operations have their own stream?
* How are stream sequences generated transactionally?
* How are stream resets governed?
* Should stream roots be periodically exported?
* Should stream roots be signed with release signing infrastructure?
* Would signing create misleading non-repudiation?
* Should a future enterprise server anchor roots?
* What evidence is needed before that?
* How frequently should tail verification run?
* How frequently should full verification run?
* How are verification costs bounded?
* Which relationship types are final?
* How are relationship cycles handled?
* Can a relationship target a deleted record?
* How are relationship tombstones represented?
* How are cross-service causal links validated?
* Should operation IDs be the primary correlation?
* How are nested operations represented?
* How are child workflows linked?
* How are retries linked?
* How are compensations linked?
* How are model fallback attempts linked?
* How are parallel tool calls displayed?
* How are partial effects displayed?
* How are user actions correlated across Desktop restart?
* How is evidence completeness calculated when an owner lacks sequence support?
* What owner freshness interval is acceptable?
* Should reconciliation run continuously or on demand?
* How are owner gap queries rate limited?
* What happens when the owner says a record never existed?
* How are conflicting owner backups handled?
* Can a user suppress a Trust Centre warning?
* How is suppression recorded?
* How are Trust Centre views paged at large scale?
* Which indexes are required?
* Should safe full-text search use SQLite FTS?
* How are FTS copies retained and deleted?
* Could FTS tokenisation leak sensitive fragments?
* Should full-text search be disabled for sensitive evidence?
* How are causal graphs virtualised?
* What default time range should Overview use?
* How are old records summarised?
* Can aggregate summaries replace records?
* Which records must remain individually visible?
* How are authority and confidence explained without jargon?
* How is a model proposal visually distinct?
* How is a user assertion visually distinct?
* How are imported historical records marked?
* Which evidence appears in notifications?
* Should every external provider request create a notification?
* How are high-volume Local Model receipts grouped?
* How are repeated read-only tool calls grouped?
* Which logs belong in Trust Centre?
* Should logs be queried through the Trust service or a separate Diagnostics service?
* How are log files rotated?
* What maximum file size is selected?
* Is JSON Lines sufficient for high volume?
* Should a binary log format be used internally?
* How are binary logs exported canonically?
* Which OpenTelemetry SDK revision is selected?
* Which semantic conventions are stable enough to adopt?
* How are custom event names governed?
* Which exception fields are safe?
* Should stack traces be stored by default?
* How are source file paths removed from stack traces?
* Should line numbers be retained?
* How are PDB paths handled?
* How are native exceptions handled?
* How are log redaction failures measured?
* Should redaction failure drop the event?
* How are dropped events counted?
* How are log canaries seeded without exposing secrets?
* Which trace sampling policy is selected?
* Are all local traces kept for seven days?
* Should only errors and selected operations be sampled?
* How are traces linked to authoritative records?
* How are dropped spans reported?
* Which metric dimensions are allowed?
* How is the cardinality budget enforced?
* Should local model IDs appear as dimensions?
* Could model IDs create excessive cardinality?
* Which metric retention periods are appropriate?
* Should metrics be persisted in SQLite or a specialised local store?
* Which diagnostic providers belong in the Performance bundle?
* Which EventPipe keywords are safe?
* How much EventPipe overhead is acceptable?
* What default buffer size is appropriate?
* How are dropped EventPipe events shown?
* Should traces be collected in-process or out-of-process?
* Which version of Microsoft.Diagnostics.NETCore.Client is pinned?
* Should `dotnet-monitor` be embedded, invoked or only used as design evidence?
* Does invoking global tools create supply-chain risk?
* Should Opure ship its own diagnostic worker?
* Which diagnostic binaries require code signing?
* How is diagnostic-port access restricted on Windows?
* Can another same-user process access the port?
* Should the runtime suspend diagnostic ports until needed?
* Is that supported by the selected .NET runtime?
* Which process dumps are supported by self-contained deployment?
* How do Native AOT or single-file deployment affect dump types?
* Is Triage available and useful for all Opure processes?
* How accurate are dump-size estimates?
* How much free disk is required?
* Should dump collection pause other high-memory work?
* Should heap and full dumps be impossible in Stable?
* Or available under advanced enterprise policy?
* How are dump files named without leaking process identity?
* How are incomplete dumps detected?
* How are dumps deleted after 24 hours?
* Should dumps be stored outside ordinary Support staging?
* How are dump accesses recorded?
* Should a dump require a Preservation Hold before export?
* How are dumps shared securely before encrypted bundles exist?
* Should Opure recommend an external encryption tool?
* Would that be platform-specific and confusing?
* What evidence is required before encrypted bundle design?
* Should the future encrypted bundle use recipient public keys?
* Should it use a passphrase KDF?
* Which cryptographic format avoids custom cryptography?
* Would CMS, age or another standard be appropriate?
* How are enterprise support public keys distributed?
* How are support destinations authenticated?
* Should direct upload ever be supported?
* How would Network Gateway preview the entire bundle classification?
* How are upload retries made idempotent?
* How is partial upload deleted?
* Which support system would receive it?
* How are terms and retention shown?
* How are external ticket references stored?
* Which system inventory fields are truly required?
* Should locale be included?
* Should time zone be included?
* Could time zone identify the user?
* Should machine name always be pseudonymised?
* Should domain membership be represented only as Boolean?
* Is proxy presence useful?
* How are proxy URLs excluded?
* Should GPU driver details be included by default?
* Should disk free space include exact values or ranges?
* How are battery and power mode represented?
* Which retention defaults are correct?
* Is 180 days appropriate for authoritative evidence?
* Is 365 days appropriate for security-critical evidence?
* Are 14 days of logs sufficient?
* Are seven days of traces sufficient?
* Should debug logs last only 24 hours?
* Which release records must persist for the release lifetime?
* How are active-workflow dependencies tracked?
* How are unresolved external effects tracked?
* Should a deleted project retain security evidence?
* How are user-requested shorter periods applied?
* How are enterprise longer periods applied?
* What happens when enterprise retention violates a user privacy expectation?
* How is that explained?
* Which retention constraints are Product Invariants?
* Which can be configured?
* Which records require a minimum period?
* Which records allow immediate deletion?
* Should harmless UI telemetry be ephemeral?
* How is storage pressure handled?
* What minimum free disk triggers cleanup?
* Which records delete first?
* How is cleanup fairness applied across projects?
* How are failed deletions retried?
* How are locked files handled?
* How are active diagnostic sessions handled during cleanup?
* How are CAS reference counts rebuilt?
* Should deletion receipts themselves have retention?
* How much detail belongs in a deletion receipt?
* Can a user delete the deletion receipt?
* How are privacy deletion requests represented?
* How are backups included in deletion scope?
* Can Opure enumerate all backups?
* How are exported copies recorded?
* Should the user be able to register an external copy?
* How are external copies marked deleted?
* How are Preservation Holds named?
* Who may create a Security Hold?
* Who may create an Enterprise Hold?
* Can a user create a hold for one operation?
* How long can a user hold last without review?
* What review cadence is appropriate?
* What storage estimate should be shown?
* Can a hold capture future matching evidence?
* Or only existing evidence?
* Should holds support both?
* How are prospective holds represented?
* How are hold selectors tested?
* Can a malicious broad selector retain all data?
* Which authority is required for machine-wide holds?
* How are hold releases reviewed?
* Can hold scope be narrowed?
* Can preserved evidence be redacted?
* Does redaction create a derived copy while original remains?
* How are accesses logged without creating an infinite access-log loop?
* Should access records themselves be preserved?
* How are incident IDs created?
* Which incident categories are final?
* How are security and privacy incidents separated?
* Can a support issue become an incident?
* How are incident severity changes recorded?
* How are containment and recovery actions linked?
* Which incident evidence is visible to the ordinary user?
* How are enterprise incidents isolated?
* Should an incident export always require a hold?
* Which Bundle presets belong in Version 1?
* Are nine presets too many?
* Should Plugin and MCP be separate presets?
* Should Provider and AI be separate?
* What default time range is best for Minimal?
* Is 30 minutes sufficient?
* What default time range is best for Standard?
* Is two hours sufficient?
* How are exact operation records included outside the time range?
* Which logs are included at each severity?
* Should Warning logs be included in Minimal?
* Which health snapshots are included?
* Which configuration fields are safe?
* Should full Resultant Configuration be included?
* Or only relevant service sections?
* How are enterprise policies masked?
* Should policy value hashes be included?
* How are project names pseudonymised?
* Can a user choose to preserve project names?
* Which evidence types are excluded from all ordinary bundles?
* Should secret-use receipts be included?
* Could even secret aliases reveal sensitive information?
* Should Vault references be omitted entirely?
* Which source snippets are useful for support?
* Should the user be able to include a complete patch?
* How are binary project files handled?
* Should generated build logs be raw or structured?
* How are command outputs truncated?
* How are ANSI escape sequences removed?
* How are model prompts segmented for selection?
* How are conversation roles represented?
* How is hidden reasoning excluded across providers?
* How are tool-call payloads separated from prompt content?
* How are plugin-private diagnostic files collected?
* Should plugins declare a support schema?
* How are malicious plugin schemas reviewed?
* Should plugin diagnostics be disabled in Stable initially?
* Which MCP call metadata are safe?
* Could server fingerprints identify an organisation?
* Should fingerprints be hashed in bundles?
* How are external account names pseudonymised?
* Which archive format implementation is selected?
* Is .NET `ZipArchive` sufficient?
* How are archive timestamps normalised?
* Should entry timestamps be zeroed or set to collection time?
* Could timestamps leak source times?
* Which ZIP compression level is selected?
* How are huge dumps streamed without loading into memory?
* How are archive hashes calculated while streaming?
* How are final bytes previewed before the archive is written?
* Should preview operate on staged directory bytes?
* How are staged files protected from mutation after approval?
* Should staged files be made read only?
* Should their handles remain open?
* How is TOCTOU prevented between preview and archive writing?
* Should archive writing use already hashed immutable CAS objects?
* How are bundle paths assigned deterministically?
* How are case collisions checked on Windows?
* How are Unicode-normalisation collisions checked?
* Should bundle paths be ASCII only?
* How are long file names shortened?
* How are collisions after shortening avoided?
* Is 2 GiB a suitable maximum?
* Should ZIP64 be permitted?
* If ZIP64 is permitted, how are readers tested?
* Is 100,000 entries excessive?
* Which ordinary file size limit is appropriate?
* How are dump exceptions represented?
* How are bundle generation disk quotas enforced?
* How is low disk handled gracefully?
* How are partial archives deleted?
* How are exported archives verified after atomic move?
* How are network or removable destinations flushed?
* How is a destination class determined?
* How is a cloud-synchronised folder detected?
* Can detection be reliable?
* How is a false warning handled?
* Should export to a network path be denied by default?
* How does enterprise policy allow it?
* How are encrypted external drives treated?
* Can Opure know they are encrypted?
* Should it avoid claims?
* Which redaction profiles ship?
* Which fields are always excluded?
* Which fields are pseudonymised?
* Which fields may be hashed?
* How is low entropy detected?
* Which pseudonym generator is selected?
* How are pseudonym collisions prevented?
* Should pseudonyms preserve resource class?
* How are related paths mapped consistently?
* How are repository URLs redacted?
* Should host names be removed or pseudonymised?
* How are issue URLs handled?
* How are email addresses detected?
* How are user-entered notes redacted?
* Which secret patterns are supported?
* How frequently are patterns updated?
* Do pattern updates invalidate prior bundle approval?
* How are provider-specific patterns distributed?
* Can pattern lists themselves reveal key formats?
* How are test canaries generated?
* How are Vault-derived fingerprints calculated safely?
* Should the Secrets Service scan candidate bundles directly?
* How is bundle content passed without exposing secrets to another service?
* Can scanning be streamed?
* How are base64-encoded secrets detected?
* How are compressed nested files handled?
* Should nested archives be denied?
* How are binary strings scanned?
* Should dump files be scanned at all?
* How are scanner false positives reviewed?
* Can a user override a finding for ordinary support?
* Which findings are never overridable?
* How is an incident-export exception approved?
* How are scanner results retained without secret content?
* What final preview UI is practical for thousands of files?
* How are files grouped by classification?
* Which files should be openable?
* How are large logs sampled for preview?
* How are binary files represented?
* Should the user be able to deselect one item after scanning?
* Does deselection create a new plan and require rescan?
* How are omissions documented?
* How are support-bundle definitions versioned?
* Should user-created custom presets be allowed?
* How are custom presets protected from accidental broad scope?
* How are Bundle Plan expiries handled during long diagnostics?
* Does collection approval differ from export approval?
* Should dumps require both?
* Should source snippets require both?
* How are changed policies applied during collection?
* Does a new denial cancel collection?
* How are active EventPipe sessions stopped?
* How are bundle generation retries handled?
* Which steps are idempotent?
* How does bundle recovery know a diagnostic session ended?
* How are stale diagnostic-worker processes terminated?
* How are bundle staging directories discovered after crash?
* How are quarantine files retained?
* How are quarantined secrets protected?
* How long are quarantine artefacts retained?
* Who can inspect a quarantine?
* How are Trust Centre access events bounded?
* Does viewing a sensitive record create a new record recursively?
* How are access records aggregated?
* Should every bundle preview create evidence?
* Which support actions should be authoritative?
* How are UI actions correlated?
* How are bundle IDs communicated to external support?
* Should a QR code or copy action exist?
* Could it leak?
* How are support instructions localised?
* How are warnings tested for comprehension?
* How are accessibility tests performed for large timelines?
* What permanent evidence is required for an evidence-forgery incident?
* What permanent evidence is required for a secret-leak support bundle?
* What permanent evidence is required before automatic upload can be considered?
* What permanent evidence is required before encrypted bundles can be considered?
* What permanent evidence is required before external hash anchoring can be considered?
* What permanent evidence is required before SIEM integration can be considered?
* What permanent evidence is required before legal-discovery features can be considered?

---

# 400. Deferred Decisions

This ADR intentionally defers:

* automatic telemetry export;
* automatic support upload;
* direct support integration;
* cloud-hosted Trust Centre;
* encrypted support-bundle format;
* recipient public-key encryption;
* passphrase-encrypted bundles;
* external evidence signing;
* externally anchored hash roots;
* trusted timestamp service;
* SIEM integration;
* Windows Event Log broad collection;
* Windows Error Reporting upload;
* remote diagnostic access;
* remote desktop support;
* court-ready chain of custody;
* legal e-discovery;
* enterprise incident-response platform;
* cross-user evidence aggregation;
* and public evidence transparency reports.

---

# 401. Alternatives Rejected

A central universal audit database is rejected because owner services must remain authoritative and Opure cannot atomically commit unrelated service databases as one transaction.

Operational logs as the only evidence source are rejected because logs may be sampled, rotated, delayed or emitted outside the domain transaction.

Full owner-database duplication is rejected because it increases sensitive-data copies and couples retention to implementation details.

Windows Event Log as the universal evidence store is rejected because project-scoped typed evidence and local redaction require product-owned contracts.

Automatic cloud telemetry is rejected because local-first operation and explicit data sharing are core product requirements.

Automatic support upload is rejected because support data may contain source, identity, prompts, external metadata or process memory.

Automatic crash dumps are rejected because memory dumps are high risk and can be very large.

A claim that triage dumps are secret free is rejected because a data-minimising dump format cannot guarantee absence of all memory-resident sensitive values.

One secret-pattern scan is rejected because layered exclusion, classification, redaction, canaries and human preview are required.

Legacy ZIP encryption is rejected because it would create a misleading security posture.

A custom encrypted bundle is deferred to avoid unreviewed cryptographic design.

Plain support directories are not selected as the normal export because a verified single archive is easier to inventory and transfer.

External cryptographic ledgers are deferred because they add infrastructure without solving source authority, data minimisation or correct evidence semantics.

Legal chain-of-custody claims are rejected because Opure is a developer product running under a user account, not a forensic evidence-management system.

---

# 402. Official and Primary Evidence References

## OpenTelemetry Logs and Telemetry

* [OpenTelemetry Logs Data Model](https://opentelemetry.io/docs/specs/otel/logs/data-model/)
* [OpenTelemetry Logging](https://opentelemetry.io/docs/specs/otel/logs/)
* [OpenTelemetry Logs API](https://opentelemetry.io/docs/specs/otel/logs/api/)
* [OpenTelemetry Specification Overview](https://opentelemetry.io/docs/specs/otel/overview/)

## .NET Diagnostics

* [.NET Diagnostic Tools Overview](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/tools-overview)
* [Diagnostics Client Library](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/diagnostics-client-library)
* [.NET Diagnostic Port](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/diagnostic-port)
* [Collect Dumps on Crash](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/collect-dumps-crash)
* [dotnet-gcdump](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-gcdump)
* [dotnet-monitor](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-monitor)
* [Windows Error Reporting WerReportAddDump](https://learn.microsoft.com/en-us/windows/win32/api/werapi/nf-werapi-werreportadddump)

## NIST Logging, Incident Response and Evidence

* [NIST SP 800-92 — Guide to Computer Security Log Management](https://csrc.nist.gov/pubs/sp/800/92/final)
* [NIST Log Management Project](https://csrc.nist.gov/Projects/log-management/publications)
* [NIST SP 800-61 Revision 3 — Incident Response Recommendations and Considerations](https://csrc.nist.gov/pubs/sp/800/61/r3/final)
* [NISTIR 8387 — Digital Evidence Preservation](https://www.nist.gov/publications/digital-evidence-preservation-considerations-evidence-handlers)
* [NIST SP 1500-33A — Evidence Management Opportunities](https://www.nist.gov/publications/evidence-management-steering-committee-report-opportunities-strengthen-evidence)

## Privacy and Retention

* [NIST Privacy Framework](https://www.nist.gov/privacy-framework)
* [Using NIST Privacy Framework 1.1](https://www.nist.gov/privacy-framework/using-privacy-framework-11)
* [ICO Data Minimisation Guidance](https://ico.org.uk/for-organisations/uk-gdpr-guidance-and-resources/data-protection-principles/a-guide-to-the-data-protection-principles/data-minimisation/)
* [ICO Storage Limitation Guidance](https://ico.org.uk/for-organisations/uk-gdpr-guidance-and-resources/data-protection-principles/a-guide-to-the-data-protection-principles/storage-limitation/)

The NIST Privacy Framework is voluntary and does not itself create legal obligations.

The ICO guidance and UK law can change and must be reviewed with appropriate legal and privacy expertise before product release.

The OpenTelemetry specifications, .NET diagnostic APIs, dump behaviour, operating-system security boundaries and privacy guidance can change.

The implementation must revalidate every selected API, tool, retention rule, evidence contract and export control before acceptance.

---

# 403. Review Record

| Date         | Reviewer           | Decision | Notes                                                                                         |
| ------------ | ------------------ | -------- | --------------------------------------------------------------------------------------------- |
| 18 July 2026 | Architecture draft | Proposed | Federated owner authority, bounded retention and locally reviewed support bundles recommended |

---

# 404. Approval

## Founder or Product Approval

* **Name:** Christopher Dyer
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Trust Centre scope, retention defaults, dump risk, support-bundle review and no-upload policy require approval

## Trust Evidence Architecture Approval

* **Name or role:** Trust Evidence Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Evidence Types, authority, ingestion, reconciliation, projections and integrity evidence required

## Observability Approval

* **Name or role:** Observability Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Logs, traces, metrics, OpenTelemetry alignment, cardinality and retention evidence required

## Persistence Approval

* **Name or role:** Persistence Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Trust database, CAS, backup, deletion and rebuild evidence required

## Security and Privacy Approval

* **Name or role:** Security, Privacy and Secrets Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Authority, redaction, secret scans, retention, holds, dump and deletion evidence required

## Runtime and Diagnostics Approval

* **Name or role:** Runtime Architecture and Performance Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Diagnostic process isolation, EventPipe, GC dump, process dump and cancellation evidence required

## Supportability Approval

* **Name or role:** Supportability Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Bundle presets, final preview, archive verification and user guidance required

## Domain Service Approval

* **Name or role:** Configuration, Workflow, AI, Tool, Patch, Plugin, MCP, Update and Project Service Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Authoritative owner records, outboxes and reconciliation contracts required

## Enterprise Approval

* **Name or role:** Enterprise Management Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Retention, Preservation Hold, dump and destination policies required

## Test Approval

* **Name or role:** Test Architecture Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Evidence-forgery, secret, dump, archive, hold-bypass and cross-project suites required

---

# 405. Supersession

This ADR is superseded only when a later ADR:

* names ADR-0027 explicitly;
* explains why evidence ownership, Trust Centre, retention, preservation, diagnostics or support bundles changed;
* identifies Evidence Type, Evidence Record, Retention Decision, Preservation Hold, Bundle Definition and manifest migration;
* describes privacy, secret, project, incident, dump, archive and external-sharing impact;
* provides comparison evidence for any automatic upload, encryption, external ledger or central authority;
* and updates the `Superseded by` field.

Historical Evidence Records, retention and deletion receipts, Preservation Holds, incident links and support-bundle export receipts remain available according to retention policy unless explicitly purged.

---

# 406. Change History

| Version | Date         | Author        | Summary                                                                                                                     |
| ------- | ------------ | ------------- | --------------------------------------------------------------------------------------------------------------------------- |
| 0.1     | 18 July 2026 | Founder Draft | Initial federated Trust Evidence, purpose-specific retention, preservation and reviewed local support-bundle recommendation |

---

# 407. Final Decision Statement

> **Opure will provisionally provide one local federated Trust Evidence Service and developer-facing Trust Centre in which the service that owns a decision, approval, state transition or side effect remains authoritative and publishes a typed immutable receipt through its transactional outbox, while Trust Evidence validates, correlates, retains, reconciles and projects those records alongside separately classified operational logs, traces, metrics and diagnostic artefacts; every record carries explicit authority, provenance, classification, retention and completeness, local hashes and stream chains are described only as corruption and consistency evidence, purpose-specific retention and transparent deletion remain subject to unresolved dependencies and reviewed Preservation Holds, and support bundles are produced solely from versioned local plans through bounded owner-service snapshots, layered schema exclusion and redaction, bundle-local path and identity pseudonymisation, canary and pattern secret scans, final-byte file inventory, expiring approval, strict reopened-and-verified unencrypted ZIP creation and short-lived staging, while source, prompts, model output, command output and process dumps remain excluded by default, dumps carry explicit high-risk approval and no secret-free claim, no support data uploads automatically, and exported copies leave Opure's control, because developer trust depends on being able to inspect what actually authorised and happened without confusing logs with authority or turning diagnostics into hidden, indefinite or unsafe data collection.**

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**