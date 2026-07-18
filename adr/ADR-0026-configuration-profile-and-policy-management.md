# ADR-0026 — Configuration, Profile and Policy Management

## Architecture Decision Record

**Status:** Proposed
**Date:** 18 July 2026
**Decision owners:** Founder and Product Owner
**Technical owners:** Configuration and Policy Architecture Owner
**Reviewers:** Runtime Architecture Owner, Persistence Owner, Security Owner, Privacy Owner, Secrets Owner, Project Service Owner, Workspace Owner, Repository Owner, Workflow Owner, AI Router Owner, Context Assembly Owner, Project Knowledge Owner, Project Memory Owner, Local Model Runtime Owner, Provider Trust Owner, Tool Mediation Owner, Plugin Platform Owner, MCP Gateway Owner, Trust Centre Owner, Desktop Owner, Enterprise Management Owner, Release Owner, Recovery Owner, Test Architecture Owner
**Supersedes:** None
**Superseded by:** None
**Related ADRs:** ADR-0001 through ADR-0025
**Related specifications:** CHARTER-001, SPEC-001 through SPEC-012
**Target milestone:** Governed Configuration and Profiles through Version 1.0

---

## 1. Decision Summary

Opure should implement configuration through a trusted **Configuration and Policy Service** that maintains:

* versioned Setting Definitions;
* versioned Policy Definitions;
* immutable Profile revisions;
* immutable Policy Source revisions;
* validated source documents;
* effective configuration snapshots;
* per-key provenance;
* configuration-impact plans;
* atomic change transactions;
* last-known-good state;
* and developer-visible Resultant Configuration reports.

The architecture should make a strict distinction between:

1. **Settings**, which express a desired value;
2. **Policies**, which constrain, require, prohibit, lock or bound values and capabilities;
3. **Profiles**, which group reusable setting choices;
4. **Scopes**, which determine where a setting or policy applies;
5. **Effective Configuration**, which is the exact resolved and validated result;
6. **Runtime Application**, which determines when a changed value becomes active;
7. **Secrets**, which remain Vault values and are referenced rather than configured directly;
8. and **Operation Plans**, which may apply bounded one-operation overrides through capabilities.

The selected principle is:

> **Settings choose within the permitted space. Policies define the permitted space. A setting source can never weaken a policy source.**

Opure should not implement all governance through ordinary “last provider wins” key precedence.

A later user or project value must not override:

* built-in safety constraints;
* enterprise machine policy;
* enterprise user policy;
* project governance policy;
* secret-handling rules;
* data-classification rules;
* capability boundaries;
* or a product release-channel boundary.

The initial Configuration and Policy Service should own:

* Setting Definition registration;
* Policy Definition registration;
* configuration schema versions;
* profile identity;
* profile inheritance;
* profile revisions;
* setting-source identity;
* policy-source identity;
* source parsing;
* source validation;
* source classification;
* source precedence;
* setting merge;
* policy intersection;
* effective-value calculation;
* cross-setting validation;
* restart and reconfiguration impact;
* atomic changes;
* rollback;
* source reconciliation;
* effective snapshot publication;
* change receipts;
* export and import;
* Trust Centre projections;
* and configuration recovery.

The Configuration and Policy Service should not own:

* secrets;
* source files;
* repository state;
* model execution;
* provider credentials;
* workflow execution;
* tool execution;
* plugin private data;
* MCP server state;
* operating-system Group Policy processing;
* or enterprise identity.

Those owners should expose typed configuration adapters and capability checks.

The initial configuration hierarchy should include the following **setting sources**, from broadest default to most specific requested choice:

1. Product Built-In Defaults
2. Release-Channel Defaults
3. Machine Preferences
4. User Base Profile
5. Named User Profile
6. Project Shared Settings
7. Project Local Profile
8. Workspace Session Settings
9. Explicit Session Override
10. Explicit Operation Override

This ordering applies only where a Setting Definition declares ordinary replacement semantics.

Arrays, maps, sets, ordered rules and structured objects should use schema-defined merge semantics rather than generic replacement.

The initial **policy sources** should be separate:

1. Product Invariant Policy
2. Release-Channel Policy
3. Enterprise Machine Policy
4. Enterprise User Policy
5. Project Governance Policy
6. Workflow Policy
7. Operation Capability Constraints

Policy sources should combine through deterministic restriction.

A lower source may make a rule stricter.

It may not broaden a higher rule.

Where several policy constraints apply, the effective permitted space should normally be their intersection.

Examples:

* allowed providers become the intersection of provider allowlists;
* maximum cost becomes the lowest maximum;
* minimum review level becomes the highest minimum;
* allowed paths become the intersection of authorised roots;
* denied capabilities become the union of denials;
* required controls become the union of requirements;
* permitted performance modes become the intersection of allowed modes;
* data-retention limits use the most restrictive applicable rule unless a legal or security policy explicitly requires longer retention;
* and a forced value must satisfy every higher policy.

If applicable policy constraints produce an empty or contradictory permitted space, the affected configuration should become:

```text
Invalid — Policy Conflict
```

Opure should not select an arbitrary winner.

Opure should not silently ignore either policy.

The initial built-in safety policy should be immutable within a released package.

Examples include:

* secrets cannot be stored in ordinary configuration;
* Local Only projects cannot route to remote providers;
* untrusted project files cannot grant capabilities;
* plugins cannot broaden enterprise or product policy;
* MCP servers cannot broaden enterprise or product policy;
* a command-line argument cannot disable review boundaries silently;
* and a configuration document cannot enable arbitrary code execution.

Enterprise policy should be allowed to make Opure stricter.

Enterprise policy should not be able to:

* inject executable code;
* inject secret values;
* bypass Opure's built-in security invariants;
* change the signed application publisher;
* disable mandatory integrity validation;
* make a project file trusted as machine policy;
* or elevate a plugin or MCP server beyond the product capability model.

The initial machine and user enterprise-policy transport on Windows should use registry-based policy under:

```text
HKEY_LOCAL_MACHINE\Software\Policies\Opure
HKEY_CURRENT_USER\Software\Policies\Opure
```

Opure should publish versioned ADMX and ADML templates for supported enterprise settings after the policy catalogue stabilises.

ADMX files should describe registry-based policy settings.

They should not become runtime executable code.

Machine policy should be treated as machine-administrator authority.

User policy should be treated as managed-user authority.

Where both apply, constraints should intersect.

For an exact forced-value conflict, machine policy should win only when the Policy Definition explicitly declares machine precedence and the result remains compatible with Product Invariant Policy.

Otherwise, the result should be a visible policy conflict.

The initial enterprise implementation should not require Opure to implement a custom Group Policy client-side extension.

Registry-based Administrative Template policy is preferred because it is a comparatively simple Windows management boundary and can participate in ordinary Windows Group Policy administration.

A future MDM or ADMX-backed Policy CSP integration may write the same supported registry policy values.

Opure should not parse arbitrary SyncML directly in the desktop process.

The initial profile scopes should be:

* Product;
* Channel;
* Machine;
* User;
* Project;
* Workspace Session;
* Workflow;
* Operation;
* Plugin;
* MCP Server;
* Provider;
* Local Model;
* Tool;
* and Test.

Not every Setting Definition should be valid at every scope.

A definition should declare its allowed scopes.

A project file should not be able to set a Machine-only option.

A plugin profile should not be able to set an AI Provider policy outside that plugin's capability.

The initial Profile model should support:

* one stable Profile ID;
* immutable revisions;
* display name;
* profile kind;
* owner scope;
* parent Profile revision;
* setting values;
* source references;
* schema version;
* classification;
* created time;
* and canonical SHA-256.

Profile inheritance should be:

* explicit;
* immutable by revision;
* acyclic;
* bounded in depth;
* and visible.

Suggested maximum inheritance depth:

```text
8
```

A child profile should inherit settings from one parent in Version 1.

Multiple profile inheritance should be deferred because it complicates ordering, conflict explanation and deletion.

Composition should occur through the explicit source hierarchy rather than arbitrary multiple inheritance.

A running operation should bind an exact **Effective Configuration Snapshot**.

The snapshot should include:

* snapshot ID;
* project or system scope;
* active profile revisions;
* setting-source revisions;
* policy-source revisions;
* schema catalogue revision;
* effective values;
* effective constraints;
* per-key provenance;
* validation results;
* restart status;
* generated time;
* and canonical SHA-256.

A service should not query mutable configuration keys repeatedly during one operation unless the activity contract explicitly allows dynamic observation.

A workflow should bind:

* the relevant Effective Configuration Snapshot;
* or exact configuration sub-snapshots

according to ADR-0025.

A model request should bind exact:

* Context Policy;
* Routing Policy;
* Provider Profile;
* Local Execution Profile;
* and project cloud policy revisions

rather than reading mutable values halfway through inference.

The initial Setting Definition should include:

```text
setting_id
revision
type
description
default
allowed_scopes
allowed_sources
merge_strategy
null_semantics
validation
semantic_validators
sensitivity
secret_policy
policy_binding
runtime_application
restart_impact
deprecation
replacement
owner_service
schema_sha256
```

The initial supported setting types should include:

* Boolean;
* integer;
* decimal;
* string;
* duration;
* byte size;
* UTC instant where necessary;
* enumeration;
* URI under policy;
* logical path reference;
* opaque service reference;
* Vault reference;
* ordered list;
* unordered set;
* string map;
* typed object;
* discriminated union;
* and bounded rule list.

No setting value should contain:

* executable code;
* arbitrary assembly names for loading;
* arbitrary command text unless the owning Tool Policy explicitly permits a reviewed command template;
* arbitrary SQL;
* provider secret;
* private key;
* access token;
* password;
* or hidden model instruction.

The initial merge strategies should include:

* Replace;
* Replace If Set;
* First Explicit;
* Append;
* Prepend;
* Ordered Unique Append;
* Set Union;
* Set Intersection;
* Map Merge by Key;
* Map Replace;
* Rule List Concatenation;
* Minimum;
* Maximum;
* and Custom Trusted Reducer.

The merge strategy should be defined by the Setting Definition.

Configuration files should not choose their own merge strategy.

A Custom Trusted Reducer should be:

* first-party;
* versioned;
* deterministic;
* pure;
* bounded;
* and covered by tests.

The initial null semantics should distinguish:

* Not Specified;
* Explicit Null;
* Reset to Default;
* Remove Inherited Entry;
* and Empty Value.

A JSON `null` should not have one universal meaning.

The schema should define it.

The initial Policy Definition should support:

* Force Value;
* Allow Values;
* Deny Values;
* Minimum;
* Maximum;
* Require Boolean True;
* Require Boolean False;
* Require Capability;
* Deny Capability;
* Require Review Mode;
* Maximum Data Class;
* Allowed Provider Profiles;
* Allowed Regions;
* Allowed Paths;
* Denied Paths;
* Maximum Cost;
* Maximum Retention;
* Minimum Retention where legally or operationally required;
* Require Local;
* Require Offline;
* Lock Setting;
* and Custom Trusted Constraint.

Policy constraints should be typed.

A stringly typed generic policy bag should not be authoritative.

The initial source formats should be:

### Service-Owned Profiles

Stored in a service-owned SQLite database and exported as strict canonical JSON.

### Project Shared Settings

Optional source-controlled file:

```text
<project-root>\.opure\project.settings.json
```

### Project Governance Policy

Optional source-controlled file:

```text
<project-root>\.opure\project.policy.json
```

### Project Local Profile

Stored under Opure application data, outside the repository.

### Enterprise Policy

Registry-based under the `Software\Policies\Opure` roots.

### Explicit Import and Export

Canonical Opure Configuration Bundle.

The `.opure` project documents should be treated as untrusted project content.

They may:

* choose project settings;
* declare project conventions;
* request stricter controls;
* select approved project profile references;
* and provide source-controlled defaults.

They may not:

* weaken Product Invariant Policy;
* weaken Enterprise Policy;
* grant a plugin capability;
* grant an MCP capability;
* expose a secret;
* enable provider use contrary to cloud policy;
* change machine settings;
* or create an unreviewed external side effect.

The initial file syntax should be **strict UTF-8 JSON**.

Configuration documents should:

* reject duplicate object property names;
* reject trailing non-whitespace;
* reject invalid UTF-8;
* reject excessive depth;
* reject excessive size;
* reject non-finite numbers;
* reject unsupported comments;
* reject trailing commas;
* reject unknown critical schema identifiers;
* and validate against a pinned schema.

Opure should not rely on generic JSON parser defaults where those defaults permit ambiguous duplicate names.

As of current .NET documentation, duplicate-property rejection controls exist in newer System.Text.Json APIs but are associated with .NET 11 preview documentation.

Opure's .NET 10 implementation should therefore include its own tested duplicate-property detection during token parsing unless the pinned runtime provides an accepted stable equivalent.

The initial schema language should be a reviewed subset of **JSON Schema Draft 2020-12**.

Opure should pin:

* supported vocabularies;
* supported keywords;
* format behaviour;
* reference resolution;
* maximum depth;
* maximum schema size;
* and remote-reference policy.

Remote `$ref` retrieval should be disabled.

Schemas should be packaged or explicitly registered locally.

Unknown required vocabulary should fail validation.

Formats should be assertions only when Opure implements and tests them as assertions.

The initial document envelope should include:

```text
$schema
schema
document_id
revision
scope
profile
settings or policies
created_at
```

A source-controlled document revision field should be informational unless combined with content hash and repository identity.

The source file's authoritative revision should include:

* Workspace Snapshot;
* repository commit or blob where applicable;
* canonical document hash;
* and project identity.

The initial parsing pipeline should be:

```text
Acquire source through owning boundary
    ↓
Verify path, registry or database identity
    ↓
Read bounded bytes
    ↓
Validate UTF-8 and JSON token stream
    ↓
Reject duplicate properties
    ↓
Validate document envelope
    ↓
Validate pinned JSON Schema subset
    ↓
Bind typed values
    ↓
Run setting or policy source authorisation
    ↓
Run semantic validation
    ↓
Classify and secret scan
    ↓
Create immutable Source Revision
```

The initial effective-configuration pipeline should be:

```text
Select applicable Product and Channel defaults
    ↓
Select applicable setting profiles
    ↓
Validate profile inheritance
    ↓
Merge settings using definition-specific reducers
    ↓
Select applicable policy sources
    ↓
Intersect typed policy constraints
    ↓
Detect policy conflicts
    ↓
Validate each effective setting against permitted space
    ↓
Run cross-setting semantic validation
    ↓
Calculate runtime and restart impact
    ↓
Create immutable Effective Configuration Snapshot
    ↓
Publish only after atomic persistence
```

The Configuration and Policy Service should preserve a complete **per-key provenance chain**.

For every effective setting, the service should be able to answer:

* what is the effective value;
* which default applied;
* which sources supplied values;
* which source won or participated in merge;
* which policies constrained it;
* which values were rejected;
* which source was ignored and why;
* which validator ran;
* whether the value is currently active;
* whether restart or reconfiguration is pending;
* and which operations bind the snapshot.

The initial Resultant Configuration report should be Opure's application-level equivalent of a Resultant Set of Policy view.

It should show:

* effective values;
* setting-source precedence;
* policy constraints;
* winning source;
* losing sources;
* conflicts;
* validation;
* restart status;
* active revision;
* pending revision;
* and source provenance.

It should not claim to be Windows RSoP.

Where registry-based Group Policy is used, the Trust Centre may link to or identify Windows policy provenance separately.

The initial configuration-change model should use an explicit **Configuration Change Transaction**.

A transaction should include:

* transaction ID;
* target scope;
* target Profile revision;
* expected base revision;
* requested changes;
* source;
* actor;
* reason;
* schema catalogue revision;
* policy snapshot;
* validation results;
* impact plan;
* preview hash;
* approval where required;
* and idempotency key.

The change pipeline should be:

```text
Begin against exact base revision
    ↓
Apply typed staged changes
    ↓
Validate setting definitions
    ↓
Validate scope and source authority
    ↓
Resolve candidate effective settings
    ↓
Apply policy constraints
    ↓
Run cross-setting validators
    ↓
Calculate impact
    ↓
Display diff and provenance
    ↓
Acquire approval where required
    ↓
Persist new immutable Profile revision
    ↓
Persist new Effective Configuration Snapshot
    ↓
Persist change receipt and outbox events
    ↓
Commit
    ↓
Apply dynamic changes through owner services
```

A change should not be partially committed across profile, snapshot and receipt.

Service-level application after commit may fail.

Such a failure should produce:

```text
Committed — Activation Failed
```

or:

```text
Committed — Restart Required
```

rather than rewriting history.

The initial runtime application classes should be:

1. Immediate Hot Apply
2. Next Read
3. Next Operation
4. New Workflow Instances Only
5. Reopen Project
6. Reindex Required
7. Reload Local Model
8. Restart Owning Service
9. Restart Runtime
10. Restart Desktop
11. Restart Application
12. Windows Sign-Out or Restart
13. Migration Required
14. Unsupported While Active

A Setting Definition should declare its class.

A combined change should use the strongest required impact.

The UI should not claim a change is active until the owning service acknowledges the exact snapshot revision.

The initial service application protocol should include:

* Configuration Snapshot notification;
* affected setting IDs;
* previous and new values by safe hash;
* impact class;
* activation capability;
* acknowledgement;
* activated snapshot;
* activation time;
* failure;
* and rollback recommendation.

Owner services should receive only their typed section.

They should not receive unrelated settings.

The process-local .NET Options pattern may be used behind trusted adapters.

`IConfiguration`, `IOptions<T>`, `IOptionsSnapshot<T>` and `IOptionsMonitor<T>` should not become the cross-service authority.

The Configuration and Policy Service remains authoritative.

A service adapter may:

* bind one validated snapshot section to a typed options object;
* use generated or explicit validation;
* expose immutable current options;
* and subscribe to Configuration Snapshot changes.

Critical options should be validated before service readiness.

The .NET source-generated Options validation and configuration-binding facilities may be used where stable and compatible with the selected .NET release.

The initial use of `Microsoft.Extensions.Configuration` providers should be limited.

Opure should not simply add arbitrary:

* JSON files;
* environment variables;
* command-line arguments;
* and memory providers

then trust generic last-provider-wins behaviour for product policy.

Microsoft's provider ordering remains useful as an implementation fact, but Opure's typed Configuration and Policy Service should explicitly define and test ordering.

The initial environment-variable policy should allow only an approved `OPURE_` bootstrap catalogue.

Suggested bootstrap categories:

* diagnostics mode;
* test fixture root;
* development channel;
* safe log level;
* service endpoint override for local development;
* and non-secret CI controls.

Environment variables should not be a general production settings layer.

Environment variables should not:

* contain Opure secrets;
* override enterprise policy;
* override project cloud policy;
* disable signature validation;
* enable arbitrary plugin loading;
* or enable unreviewed remote data sharing.

The initial command-line policy should allow:

* explicit product commands;
* typed operation parameters;
* safe diagnostic flags;
* explicit profile selection;
* and approved development-only bootstrap values.

Command-line arguments should not contain:

* passwords;
* access tokens;
* API keys;
* Vault values;
* or private keys

because process command lines can be exposed through operating-system tooling, logs or diagnostics.

A command-line operation override should be:

* typed;
* visible;
* bounded to one operation or process;
* policy checked;
* included in the Effective Configuration Snapshot;
* and receipted.

The initial source-change policy should not treat a raw file watcher notification as authoritative.

File watcher events may be:

* duplicated;
* coalesced;
* delayed;
* reordered;
* or missed.

For project configuration files, the service should:

1. receive watcher hints;
2. debounce;
3. reacquire a verified Workspace Snapshot;
4. reread the full bounded document;
5. calculate hash;
6. validate;
7. compare with the active source revision;
8. publish a candidate source revision;
9. and update effective configuration transactionally.

A periodic or project-focus reconciliation should detect missed changes.

The initial external-edit behaviour should be:

* valid external edit: import as a new source revision;
* invalid edit: retain last-known-good effective configuration and display the error;
* deleted optional source: remove that source through a new revision;
* deleted required source: affected configuration invalid;
* changed project identity: do not silently transfer;
* and conflicting source edit: show a merge or revision conflict.

The service should never replace a valid active snapshot with a partially written or invalid document.

All Opure-authored configuration-file writes should use ADR-0009 staged atomic replacement.

The initial Registry Policy reader should:

* read only schema-defined values;
* read both 64-bit and selected Windows view according to the product's x64 architecture;
* bind exact hive, key, value name and registry type;
* reject type mismatches;
* reject unknown critical values;
* record source and read time;
* monitor change notifications where practical;
* periodically reconcile;
* and create immutable policy-source revisions.

Registry policy should contain no secret values.

A registry policy value naming scheme should be stable and documented.

Nested complex policy should avoid opaque arbitrary JSON registry blobs where practical.

Where structured policy is necessary, use:

* a versioned bounded canonical JSON value;
* strict duplicate detection;
* local schema;
* and size limits.

The initial policy-refresh behaviour should include:

* application start;
* Runtime start;
* project open;
* user session unlock where supported;
* registry change notification;
* explicit Refresh Policy command;
* and bounded periodic reconciliation.

A policy change should immediately invalidate:

* pending configuration approvals;
* stale operation capabilities;
* stale Routing Decisions;
* stale Data Sharing Plans;
* and other affected plans

where their policy binding no longer matches.

The initial profile-selection model should include:

* one active User Base Profile;
* zero or one active Named User Profile;
* zero or one Project Shared Settings source;
* zero or one Project Local Profile;
* one Session Override set;
* and zero or more operation-bound override sets.

A user may create several named profiles, such as:

* Balanced Local;
* Maximum Privacy;
* Cloud Assisted;
* Low Power;
* Release Engineering;
* and Experimental.

A profile name should not imply permission.

For example, a profile named `Cloud Assisted` remains unable to use cloud providers in a Local Only project.

The initial Project Shared Settings source should be optional.

Absence should not create an error.

The Project Governance Policy source should also be optional unless a project template or enterprise policy requires it.

Source-controlled project configuration should support a reviewable diff.

Changes in a repository branch should apply only after:

* project-source validation;
* effective configuration recalculation;
* and impact review.

A branch checkout that changes configuration may:

* require project reopen;
* reindex;
* alter workflow eligibility;
* invalidate pending Context Plans;
* or require a model reload.

These impacts should be visible before affected operations start.

The initial configuration rollback model should create a new Profile revision based on an earlier revision.

Rollback should not delete the intervening history.

Rollback should re-evaluate current policy.

A value that was previously valid may now be prohibited.

Rollback should include a new impact plan and approval.

The initial configuration history should be append-only.

It should record:

* source revisions;
* Profile revisions;
* effective snapshots;
* change transactions;
* validation results;
* policy conflicts;
* activation receipts;
* rollback;
* import;
* export;
* migration;
* and deletion tombstones.

A hash chain may be used for change-history integrity evidence.

It should not be described as protection against a full same-user rewrite.

The initial persistence architecture should use:

* one service-owned SQLite database per release channel;
* immutable Setting and Policy Definition revisions;
* immutable Profile revisions;
* immutable Source revisions;
* immutable Effective Configuration Snapshots;
* append-only change events;
* current projections;
* transactional outbox;
* and content-addressed storage for large imported or exported documents.

The selected database should not contain secrets.

The initial Configuration database should use the general ADR-0005 durability policy.

Configuration changes affecting security, provider use, workflow execution or release behaviour should use durable commits before publication.

The initial last-known-good policy should preserve:

* last valid source revision;
* last valid Effective Configuration Snapshot;
* last activated snapshot per service;
* and failure evidence.

When a user or project setting becomes invalid, Opure should continue using the last activated safe snapshot where doing so does not violate current policy.

When current enterprise or Product Invariant Policy newly prohibits the last-known-good value, Opure should disable the affected capability rather than continue in violation.

Examples:

* a provider becomes prohibited: remote provider operations stop;
* a plugin becomes prohibited: plugin execution stops;
* a retention maximum becomes stricter: new retention uses the stricter rule and existing data enters a governed cleanup plan;
* a logging value becomes invalid: safe built-in logging defaults apply;
* and a UI preference becomes invalid: safe UI default applies.

The initial failure policy should classify settings as:

* Optional Preference;
* Operational;
* Security Critical;
* Privacy Critical;
* Data Governance Critical;
* Persistence Critical;
* Release Critical;
* and Bootstrap Critical.

Failure behaviour should be definition specific.

A malformed accent preference should not stop Runtime.

A malformed provider policy should stop remote provider use.

A malformed Vault configuration should stop secret-dependent operations.

A malformed Workflow durability setting should stop new workflows if safe operation cannot be guaranteed.

The initial configuration migration model should support:

* schema version migration;
* renamed setting;
* moved setting;
* split setting;
* combined setting;
* changed enumeration;
* changed unit;
* changed default;
* removed setting;
* and policy conversion.

Migrations should be:

* versioned;
* deterministic;
* pure;
* idempotent;
* tested;
* and provenance preserving.

A migration should create a new Source or Profile revision.

It should not rewrite historical revisions.

An unknown future schema should not be loaded.

The initial deprecated-setting model should include:

* deprecation version;
* replacement setting;
* migration availability;
* warning;
* removal release;
* and conflict rules.

A removed setting in a user profile should be ignored only through an explicit migration result.

An unknown setting in a project document should be:

* warning and preserved if the schema marks extension points;
* or validation failure

according to the document schema.

The initial export format should be an **Opure Configuration Bundle** containing:

* manifest;
* schema catalogue references;
* Profile revisions;
* safe setting values;
* policy documents selected for export;
* source provenance;
* validation;
* and hashes.

The bundle should not contain:

* Vault values;
* provider credentials;
* private keys;
* user access tokens;
* hidden machine policy not selected for export;
* unrelated project data;
* or hidden model reasoning.

Import should:

* validate bundle structure;
* verify hashes;
* classify data;
* secret scan;
* map scopes;
* detect unsupported settings;
* resolve Profile identity;
* preview changes;
* apply current policy;
* and create new local revisions.

An imported Profile should not become active automatically.

An imported policy should not become enterprise authority merely because the bundle labels it as such.

The initial Trust Centre should show:

```text
Effective Configuration
Profiles
Setting Sources
Policy Sources
Enterprise Policy
Project Settings
Project Policy
Session Overrides
Operation Overrides
Pending Activation
Restart Requirements
Conflicts
Validation Failures
History
Imports and Exports
Schema and Migration
```

The selected trust chain is:

```text
Versioned Setting and Policy Definitions
    ↓
Authorised immutable source revisions
    ↓
Typed profile merge
    ↓
Typed policy intersection
    ↓
Conflict detection
    ↓
Cross-setting validation
    ↓
Effective Configuration Snapshot
    ↓
Impact and approval
    ↓
Atomic change commit
    ↓
Owner-service activation
    ↓
Activation receipt and Trust Centre provenance
```

---

## 2. Status

Current status:

> **Proposed**

This ADR becomes **Accepted** only after prototypes demonstrate:

* Setting Definition and Policy Definition catalogues;
* strict JSON parsing;
* duplicate-property rejection on the pinned .NET runtime;
* JSON Schema Draft 2020-12 subset validation;
* source-controlled project settings;
* source-controlled project policy;
* project local profiles;
* immutable Profile revisions;
* bounded single inheritance;
* setting-source precedence;
* schema-defined merge strategies;
* policy intersection;
* product safety invariants;
* registry-based machine policy;
* registry-based user policy;
* ADMX and ADML prototypes;
* policy refresh;
* policy conflict;
* effective snapshot generation;
* per-key provenance;
* Resultant Configuration;
* Configuration Change Transactions;
* atomic commit;
* owner-service activation;
* activation failure;
* restart impact;
* last-known-good state;
* source watcher reconciliation;
* branch-change impact;
* rollback;
* schema migration;
* deprecation;
* environment and command-line restrictions;
* Vault references;
* import and export;
* Trust Centre;
* project and channel isolation;
* and adversarial policy bypass, duplicate-key, malformed-file, symlink, registry-type, environment override, command-line secret, stale approval, plugin escalation, MCP escalation and configuration rollback tests.

---

## 3. Context

Opure has configuration across:

* Desktop;
* Runtime;
* local services;
* projects;
* workflows;
* models;
* providers;
* tools;
* plugins;
* MCP servers;
* logging;
* updates;
* indexing;
* memory;
* and performance.

Without one governed architecture, each service may:

* choose a different file format;
* choose a different precedence order;
* accept different environment overrides;
* store secrets improperly;
* reload values at unsafe times;
* ignore enterprise policy;
* interpret `null` differently;
* merge arrays differently;
* or hide why an effective value was selected.

The ordinary .NET configuration system is useful but intentionally general.

Its providers are ordered, and later providers override earlier values for duplicate keys.

That behaviour is appropriate for many application settings.

It is insufficient as the complete Opure policy model because:

* a policy is not merely another value source;
* security constraints should intersect rather than lose to a later source;
* lists and objects require typed merge semantics;
* service boundaries need immutable snapshots;
* cross-setting validation is required;
* user-visible provenance is required;
* and operation plans must bind exact revisions.

Windows enterprise management commonly uses registry-based policy described by ADMX and ADML Administrative Templates.

Opure should integrate with that environment without making raw registry values the only internal representation.

Project teams may also need source-controlled configuration.

A repository-controlled file is useful for shared defaults and stricter project rules.

It is not equivalent to trusted machine policy because repository content may come from:

* an untrusted clone;
* a pull request;
* a changed branch;
* a malicious dependency;
* or a compromised remote.

Configuration changes can also have different activation requirements.

A theme can apply immediately.

A local model context length may require a reload.

A database setting may require service restart.

A project indexing policy may require a rebuild.

A provider change may invalidate approvals and Data Sharing Plans.

A workflow policy should normally affect new workflow instances rather than mutate a running plan.

Therefore, configuration needs explicit lifecycle and impact.

---

## 4. Problem Statement

Opure requires a local-first configuration architecture that resolves defaults, profiles, project settings, enterprise policy, session choices and operation overrides into immutable effective snapshots while preserving typed merge semantics, non-bypassable safety constraints, per-key provenance, secret exclusion, atomic updates, activation impact, rollback, migration and developer-visible policy conflicts.

---

## 5. Decision Drivers

The decision is evaluated against:

* Charter alignment;
* developer control;
* enterprise management;
* local-first operation;
* project isolation;
* release-channel isolation;
* secret safety;
* data governance;
* policy non-bypass;
* deterministic precedence;
* typed validation;
* cross-setting validation;
* source-controlled project configuration;
* profile reuse;
* runtime consistency;
* atomic change;
* restart visibility;
* rollback;
* migration;
* inspectability;
* testability;
* and small-team implementation.

---

## 6. Governing Principles

This decision must preserve:

* Developer Respect;
* Human in Control;
* Local by Design;
* Visible by Design;
* Inspectable Decisions;
* Settings Are Not Policy;
* Policy Defines Permitted Space;
* Later Settings Cannot Weaken Policy;
* Product Invariants Cannot Be Disabled by Data;
* Enterprise Policy May Restrict, Not Elevate;
* Project Files Are Untrusted Content;
* Profiles Are Choices, Not Capabilities;
* Immutable Revisions;
* Exact Effective Snapshots;
* Per-Key Provenance;
* Strict Parsing;
* Duplicate Keys Are Errors;
* No Remote Schema Fetch;
* No Secret Configuration Values;
* No Command-Line Secrets;
* No Generic Environment Override;
* No Hidden Reload;
* Last Known Good, Subject to Current Policy;
* Atomic Change;
* Explicit Activation Impact;
* Running Work Pins Revisions;
* Reversible Rollback through New Revisions;
* and Fail Closed for Critical Governance.

---

## 7. Scope

This ADR decides:

* Configuration and Policy Service ownership;
* Setting Definitions;
* Policy Definitions;
* source hierarchy;
* policy hierarchy;
* Profiles;
* profile inheritance;
* scopes;
* merge semantics;
* null semantics;
* strict JSON;
* JSON Schema subset;
* project files;
* registry policy;
* ADMX and ADML;
* environment variables;
* command-line arguments;
* Effective Configuration Snapshots;
* provenance;
* Resultant Configuration;
* atomic changes;
* runtime activation;
* restart requirements;
* source reconciliation;
* last-known-good state;
* rollback;
* migration;
* deprecation;
* import and export;
* persistence;
* Trust Centre;
* and acceptance tests.

This ADR does not decide:

* final setting catalogue;
* final enterprise-policy catalogue;
* cloud-hosted configuration;
* organisation configuration synchronisation;
* web administration portal;
* arbitrary policy scripting;
* Rego or OPA adoption;
* arbitrary expression-based policy;
* custom Group Policy client-side extension;
* final MDM product integration;
* global roaming profiles;
* or provider-managed configuration.

---

## 8. Constraints

Known constraints include:

* ADR-0005 selected service-owned SQLite and explicit migrations.
* ADR-0007 keeps secret values in the Vault.
* ADR-0009 controls paths and atomic staged writes.
* ADR-0018 and ADR-0019 govern tools, MCP and remote data.
* ADR-0020 controls local execution profiles.
* ADR-0021 requires immutable Context Plans.
* ADR-0023 requires memory revision binding.
* ADR-0024 requires exact Routing Policies and Decisions.
* ADR-0025 requires workflow plan pinning.
* .NET configuration providers use ordered source precedence;
* .NET JSON configuration providers report duplicate keys within one provider as format errors;
* file providers may support reload on change;
* Options validation can run at service startup;
* JSON Schema Draft 2020-12 is a stable published specification;
* Windows Administrative Templates describe registry-based policy;
* Windows Group Policy policy settings and preferences have different governance behaviour;
* and Windows RSoP exists to explain applied Group Policy precedence, but Opure still needs its own application-level effective configuration report.

---

## 9. Assumptions

This decision assumes:

* all first-party settings can be registered in typed catalogues;
* services can consume immutable snapshot sections;
* project files can be read through Workspace;
* enterprise registry values can be read without administrative write access;
* ADMX and ADML files can be distributed separately from runtime policy authority;
* profile revisions can be stored in SQLite;
* source-controlled project configuration is optional;
* owner services can acknowledge activation;
* and a small first-party policy expression set can cover Version 1 needs.

---

## 10. Current Technical Evidence

Official and primary documentation available on 18 July 2026 establishes that:

* .NET configuration uses configuration providers and later-added providers override earlier providers for the same key;
* .NET file configuration providers report duplicate keys in one provider as format errors;
* JSON configuration files may be configured for change reload;
* .NET Options can validate settings during service startup;
* source-generated Options validation and configuration binding can reduce reflection and support AOT-compatible code;
* JSON Schema Draft 2020-12 defines versioned vocabularies, array semantics, dynamic references, unevaluated items and format vocabularies;
* current preview System.Text.Json documentation includes an `AllowDuplicateProperties` control, but that API is documented against .NET 11 preview rather than the selected .NET 10 release;
* Windows Administrative Templates use language-neutral ADMX files and language-specific ADML resources to describe registry-based policy;
* registry-based policy is recommended by Microsoft as a comparatively straightforward application-policy mechanism;
* and Windows Group Policy Resultant Set of Policy reporting identifies applied policy and precedence.

These facts support:

* a typed first-party configuration layer above generic provider order;
* strict duplicate detection;
* startup validation;
* pinned local schemas;
* registry-based enterprise policy;
* and an application-level Resultant Configuration view.

Every .NET API, JSON implementation, Windows policy transport and schema library must be revalidated before acceptance.

---

## 11. Terminology

### 11.1 Setting Definition

The versioned schema and behaviour for one configurable value.

---

### 11.2 Policy Definition

The versioned schema and combination rules for one enforceable constraint.

---

### 11.3 Setting Source

A source that proposes values.

---

### 11.4 Policy Source

A source that constrains or forces permitted values and capabilities.

---

### 11.5 Profile

A named immutable collection of setting choices.

---

### 11.6 Profile Revision

One immutable version of a Profile.

---

### 11.7 Source Revision

One immutable validated reading of a file, registry, database or built-in source.

---

### 11.8 Scope

The product, machine, user, project, workflow, operation or component boundary where a value applies.

---

### 11.9 Merge Strategy

The typed deterministic rule for combining setting values.

---

### 11.10 Constraint Intersection

The typed deterministic combination of applicable policies into one permitted space.

---

### 11.11 Effective Configuration Snapshot

The immutable resolved values and constraints used by an operation or service.

---

### 11.12 Per-Key Provenance

Evidence showing every source and policy that influenced one effective setting.

---

### 11.13 Resultant Configuration

The developer-facing explanation of effective settings and policies.

---

### 11.14 Configuration Change Transaction

An atomic reviewed change from one Profile and Effective Snapshot revision to another.

---

### 11.15 Activation

The owning service's acknowledgement that one snapshot revision is in use.

---

### 11.16 Pending Restart

A committed value that cannot become active until the declared restart boundary.

---

### 11.17 Last Known Good

The last validated and activated safe snapshot.

---

### 11.18 Product Invariant Policy

A signed built-in safety rule that no data source may weaken.

---

### 11.19 Enterprise Machine Policy

Administrator-managed machine-wide policy.

---

### 11.20 Enterprise User Policy

Administrator-managed policy for the current user.

---

### 11.21 Project Governance Policy

Source-controlled project constraints that may make project behaviour stricter but cannot grant higher authority.

---

### 11.22 Operation Override

A one-operation typed setting choice that remains subject to all policy.

---

## 12. Options Considered

The principal architecture options are:

1. First-party typed Configuration and Policy Service.
2. Plain `Microsoft.Extensions.Configuration` provider stack.
3. One mutable JSON settings file.
4. Windows Registry only.
5. Group Policy only.
6. Environment variables and command-line arguments.
7. Repository `.editorconfig`-style configuration only.
8. OPA or Rego policy engine.
9. JSON Logic or arbitrary expression policy.
10. Cloud-hosted configuration service.
11. Database-only opaque key-value settings.
12. Per-service independent configuration.

---

## 13. Option A — Typed Configuration and Policy Service

### 13.1 Advantages

* policy cannot be confused with value precedence;
* immutable snapshots;
* per-key provenance;
* project and enterprise support;
* atomic change;
* typed merge;
* restart impact;
* last-known-good recovery;
* and provider-neutral local operation.

### 13.2 Disadvantages

* larger schema catalogue;
* custom policy reducers;
* profile UI;
* migration maintenance;
* and enterprise template maintenance.

### 13.3 Decision

Selected.

---

## 14. Plain .NET Provider Stack

Useful inside adapters but rejected as the complete architecture because ordinary last-provider-wins precedence cannot represent non-bypassable policy intersection, exact cross-service snapshots or complete provenance by itself.

---

## 15. One Mutable JSON File

Rejected because machine, user, project, session and operation scopes differ, and history, policy and activation would be unclear.

---

## 16. Windows Registry Only

Rejected as the complete store because project configuration, profile history, structured objects and cross-platform evolution require a richer internal model.

Registry policy remains selected for enterprise transport.

---

## 17. Group Policy Only

Insufficient for ordinary developer preferences and project profiles.

---

## 18. Environment and Command Line

Rejected as general production layers because they are difficult to inspect, can expose values and can bypass user expectations.

Restricted typed bootstrap and operation parameters remain allowed.

---

## 19. Repository-Only Configuration

Insufficient for machine, user, local, secret and enterprise state.

Project files remain optional sources.

---

## 20. OPA or Rego

Deferred because Version 1 policy needs are typed and bounded, while embedding a general policy language adds another runtime, package and security surface.

---

## 21. Arbitrary Expression Policy

Rejected because policy should be inspectable, typed and bounded.

---

## 22. Cloud Configuration Service

Rejected as the initial authority because Opure must work offline and remain local first.

---

## 23. Opaque Key-Value Database

Rejected because setting types, scopes, validation, merge and policy semantics would remain implicit.

---

## 24. Per-Service Independent Configuration

Rejected because precedence, secrets, enterprise policy and cross-service operation snapshots would diverge.

---

## 25. Decision

Opure will provisionally adopt:

> **A local trusted Configuration and Policy Service that resolves immutable typed Setting Source and Profile revisions through definition-specific merge semantics, intersects separate Product, Enterprise, Project, Workflow and Operation Policy constraints into a non-bypassable permitted space, validates strict locally schemed JSON and registry policy, publishes exact Effective Configuration Snapshots with per-key provenance and activation impact, and applies changes through atomic reviewed transactions and owner-service acknowledgements, while project files, environment variables, command-line parameters, plugins, MCP servers and imported bundles remain unable to weaken built-in or enterprise policy, secret values remain in the Vault, running operations pin revisions, and invalid or conflicting critical policy fails closed rather than silently selecting a later value.**

This decision is:

* [ ] Permanent until superseded
* [x] Provisional pending schema, policy, enterprise and activation evidence
* [ ] Approval of arbitrary policy scripting
* [ ] Approval of cloud-hosted configuration authority
* [ ] Approval of general environment-variable precedence
* [ ] Approval of multiple Profile inheritance

---

# 26. Configuration and Policy Service Ownership

The Configuration and Policy Service owns:

* Setting Definition registration;
* Policy Definition registration;
* schema catalogue identity;
* source registration;
* source revisions;
* Profile registration;
* Profile revisions;
* Profile inheritance;
* setting merge;
* policy intersection;
* semantic validation;
* Effective Configuration Snapshots;
* per-key provenance;
* Resultant Configuration;
* change transactions;
* activation plans;
* activation receipts;
* pending-restart state;
* last-known-good state;
* rollback;
* migration;
* import;
* export;
* retention;
* and recovery.

---

## 26.1 Non-Responsibilities

The service does not:

* store Vault values;
* read arbitrary project files;
* write project source without Workspace authority;
* decide provider terms;
* execute models;
* execute tools;
* grant plugin capabilities;
* grant MCP capabilities;
* process Windows Group Policy itself;
* or apply operating-system settings outside Opure.

---

# 27. Service Boundary

Conceptual architecture:

```text
Desktop, CLI, Project or Enterprise Source
    ↓
Runtime Gateway
    ↓
Configuration and Policy Service
    ├── Definition Catalogue
    ├── Schema Registry
    ├── Source Registry
    ├── Profile Manager
    ├── Setting Merger
    ├── Policy Evaluator
    ├── Semantic Validator
    ├── Effective Snapshot Builder
    ├── Impact Planner
    ├── Change Transaction Manager
    ├── Migration Manager
    ├── Import and Export
    └── Recovery Manager
        ↓
Typed Configuration Adapters
        ↓
Desktop, Runtime, Workflow, Context, AI,
Knowledge, Memory, Tool, Plugin, MCP,
Provider, Local Model and Update Services
```

---

## 27.1 No Direct Database Access

Consumers do not query Configuration tables directly.

---

## 27.2 No Raw Registry Access by Consumers

Enterprise policy enters through the service.

---

## 27.3 No Raw Project JSON Access by Consumers

Project settings enter through the service.

---

## 27.4 No Model Authority

A model may propose settings or a Profile.

It cannot commit, activate, lock or waive policy.

---

# 28. Configuration Database

Suggested location:

```text
%LOCALAPPDATA%\Opure\<Channel>\Configuration\configuration.db
```

Associated directories:

```text
Configuration\
├── configuration.db
├── schemas\
├── exports\
├── imports\
├── staging\
├── quarantine\
└── recovery\
```

---

## 28.1 Channel Isolation

Stable, Preview and Development use separate databases.

---

## 28.2 Local Fixed Disk

Required.

---

## 28.3 Repository Storage

Denied for the service database.

---

## 28.4 Network Storage

Denied initially.

---

# 29. Suggested Tables

```text
setting_definitions
setting_definition_revisions
policy_definitions
policy_definition_revisions
schema_catalogues
schema_documents
configuration_sources
configuration_source_revisions
policy_sources
policy_source_revisions
profiles
profile_revisions
profile_parents
profile_values
profile_value_elements
effective_snapshots
effective_snapshot_values
effective_snapshot_constraints
effective_snapshot_provenance
configuration_change_transactions
configuration_change_items
configuration_change_events
configuration_activation_plans
configuration_activation_receipts
configuration_conflicts
configuration_validation_results
configuration_migrations
configuration_imports
configuration_exports
configuration_outbox
configuration_inbox
configuration_integrity_results
configuration_tombstones
```

---

## 29.1 Large Documents

Use CAS when an import, export or source exceeds the inline threshold.

---

## 29.2 Secret Values

Absent.

---

# 30. Setting Definition Catalogue

The catalogue is versioned as a whole.

---

## 30.1 Setting Identity

Use stable dotted identifiers.

Examples:

```text
runtime.performance.default-mode
project.cloud.policy
ai.routing.local-preference
knowledge.indexing.embeddings.enabled
workflow.maximum-parallel-steps
desktop.appearance.theme
logging.level.default
plugins.development-mode.enabled
mcp.external-server.enabled
updates.check-on-launch.enabled
```

---

## 30.2 Naming Rules

Identifiers should be:

* lowercase ASCII;
* dot separated;
* stable;
* product owned;
* and independent of UI wording.

---

## 30.3 Renaming

Use migration and alias metadata.

---

## 30.4 No Dynamic User Setting IDs

User-defined metadata belongs under explicit extension containers.

---

# 31. Setting Definition Schema

Suggested schema:

```text
opure.setting-definition/1
```

---

## 31.1 Fields

```text
setting_id
revision
owner_service
display_name
description
value_type
default_value
allowed_scopes
allowed_sources
merge_strategy
null_semantics
validation
semantic_validator_ids
sensitivity
secret_policy
policy_definition_ids
runtime_application
failure_class
deprecation
created_at
definition_sha256
```

---

## 31.2 Immutable Revision

Required.

---

## 31.3 Default Value

Must validate under the definition and Product Invariant Policy.

---

## 31.4 No Undefined Default

Allowed only when the setting is explicitly required from another source.

---

# 32. Value Types

## 32.1 Boolean

Canonical `true` or `false`.

---

## 32.2 Integer

Bounded signed or unsigned range.

---

## 32.3 Decimal

Use decimal semantics when money or exact decimal matters.

---

## 32.4 String

Length, Unicode and pattern bounded.

---

## 32.5 Duration

Canonical ISO 8601 or product-defined representation.

Internal type should be explicit.

---

## 32.6 Byte Size

Integer bytes internally.

Display in human units.

---

## 32.7 Instant

UTC only.

Not for ambient “current time”.

---

## 32.8 Enumeration

Closed versioned values.

---

## 32.9 URI

Scheme and host policy.

---

## 32.10 Logical Path Reference

Project-relative or service-owned typed path.

No raw arbitrary path where a safer reference exists.

---

## 32.11 Opaque Service Reference

Provider Profile, Model Profile, Plugin Package, MCP Server, Vault entry or other service identity.

---

## 32.12 Ordered List

Order meaningful.

---

## 32.13 Set

Order not meaningful.

---

## 32.14 Map

Key and value schemas.

---

## 32.15 Typed Object

Closed object schema.

---

## 32.16 Discriminated Union

Explicit discriminator.

---

## 32.17 Rule List

Ordered bounded typed rules.

---

# 33. Value Limits

Every type declares:

* maximum encoded size;
* maximum collection count;
* maximum object depth;
* maximum string length;
* and maximum map-key length.

---

## 33.1 Suggested General Limits

* document: 4 MiB;
* one string: 64 KiB;
* one list: 10,000 elements;
* one map: 10,000 entries;
* object depth: 32;
* setting count per Profile: 10,000;
* policy constraints per source: 10,000;
* and inheritance depth: 8.

These require evidence.

---

# 34. Setting Scope

Initial scopes:

```text
product
channel
machine
user
project
workspace-session
workflow
operation
plugin
mcp-server
provider
local-model
tool
test
```

---

## 34.1 Scope Key

A scope has:

* kind;
* owning identity;
* parent scope where relevant;
* and channel.

---

## 34.2 Invalid Scope

Rejected before merge.

---

## 34.3 Scope Narrowing

A narrower source applies only to its descendants according to the definition.

---

## 34.4 Cross-Project

Denied.

---

# 35. Allowed Sources

A Setting Definition declares which sources may supply it.

Example:

```text
desktop.appearance.theme:
    user-profile
    named-profile
    session-override
```

Example:

```text
project.cloud.policy:
    project-local-profile
    project-shared-settings
    enterprise-policy-force
```

Example:

```text
runtime.security.integrity-validation:
    product-default
    product-policy
    enterprise-policy-stricter-only
```

---

## 35.1 Disallowed Source

Ignored only with a visible validation failure.

It does not participate in the snapshot.

---

# 36. Sensitivity

Classes:

* Public;
* Product Internal;
* Project Internal;
* Personal;
* Confidential;
* Security Sensitive;
* Secret Reference;
* and Prohibited Secret Value.

---

## 36.1 Inheritance

Effective sensitivity is at least the highest contributing source and definition class.

---

## 36.2 Display

Sensitive values may be masked.

---

# 37. Secret Policy

Possible values:

* No Secret;
* Vault Reference Allowed;
* Vault Reference Required;
* Secret-Derived Boolean Only;
* and Prohibited.

---

## 37.1 Vault Reference

Stores opaque ID, not secret.

---

## 37.2 Existence Validation

May verify that the reference exists without reading the value.

---

## 37.3 Export

Vault reference may be excluded or mapped.

---

# 38. Failure Class

Classes:

* Optional Preference;
* Operational;
* Security Critical;
* Privacy Critical;
* Data Governance Critical;
* Persistence Critical;
* Release Critical;
* and Bootstrap Critical.

---

## 38.1 Behaviour

Definition-specific safe fallback.

---

# 39. Policy Definition Catalogue

The catalogue is separate from settings.

---

## 39.1 Policy Identity

Examples:

```text
policy.cloud.allowed-providers
policy.cloud.require-local
policy.tools.allowed-capabilities
policy.plugins.allow-development-mode
policy.mcp.allowed-servers
policy.data.maximum-class-for-provider
policy.workflow.minimum-review-mode
policy.updates.allowed-channels
policy.logging.maximum-retention
policy.memory.remote-embedding
```

---

# 40. Policy Definition Schema

Suggested schema:

```text
opure.policy-definition/1
```

---

## 40.1 Fields

```text
policy_id
revision
owner_service
constraint_type
target_settings
target_capabilities
allowed_sources
scope_rules
combination_rule
conflict_rule
validation
display
failure_class
created_at
policy_sha256
```

---

## 40.2 Immutable Revision

Required.

---

# 41. Constraint Types

## 41.1 Force Value

One required typed value.

---

## 41.2 Allow Values

Set of permitted values.

---

## 41.3 Deny Values

Set of prohibited values.

---

## 41.4 Minimum

Lower bound.

---

## 41.5 Maximum

Upper bound.

---

## 41.6 Require True

Boolean must be true.

---

## 41.7 Require False

Boolean must be false.

---

## 41.8 Require Capability

Capability must be present.

---

## 41.9 Deny Capability

Capability prohibited.

---

## 41.10 Require Review Mode

Minimum review class.

---

## 41.11 Maximum Data Class

Maximum data class for one destination.

---

## 41.12 Allowed Providers

Exact Provider Profile revisions or governed selectors.

---

## 41.13 Allowed Regions

Exact region identifiers.

---

## 41.14 Allowed Paths

Verified logical path roots.

---

## 41.15 Denied Paths

Explicit exclusions.

---

## 41.16 Maximum Cost

Typed currency or budget policy.

---

## 41.17 Retention Bound

Maximum or minimum under explicit purpose.

---

## 41.18 Require Local

Remote candidates denied.

---

## 41.19 Require Offline

No network dependency.

---

## 41.20 Lock Setting

Lower setting sources cannot change the effective value.

---

## 41.21 Custom Trusted Constraint

Versioned reducer.

---

# 42. Policy Combination Rules

## 42.1 Set Intersection

For allowed sets.

---

## 42.2 Set Union

For denied sets and required controls.

---

## 42.3 Lowest Maximum

For maximum limits.

---

## 42.4 Highest Minimum

For minimum requirements.

---

## 42.5 Boolean Conjunction

For required true controls.

---

## 42.6 Boolean Disjunction of Denials

Any applicable denial denies.

---

## 42.7 Exact Force Agreement

All force values must agree unless explicit machine precedence applies.

---

## 42.8 Path Intersection

Allowed roots narrowed by all policies.

---

## 42.9 Custom Reducer

Bounded trusted implementation.

---

# 43. Policy Conflict

A conflict record contains:

```text
conflict_id
scope
policy_definition
source_revisions
constraints
empty_or_conflicting_space
affected_settings
severity
created_at
resolution_state
```

---

## 43.1 Conflict Outcomes

* Disable Affected Capability;
* Use Product Safe Default;
* Require Administrator Resolution;
* Require Project Resolution;
* Ignore Invalid Lower Authority Source;
* or Application Start Blocked.

---

## 43.2 No Arbitrary Winner

Required.

---

# 44. Product Invariant Policy

Packaged with the application.

---

## 44.1 Integrity

Bound to signed package and product version.

---

## 44.2 Examples

* secret values prohibited in ordinary configuration;
* remote use requires project cloud policy;
* untrusted project content cannot grant capability;
* arbitrary code policy denied;
* signature validation cannot be disabled by settings;
* and cross-project scope denied.

---

## 44.3 Change

Requires product release and ADR or reviewed product change.

---

# 45. Release-Channel Policy

Examples:

* Preview features disabled in Stable;
* Development plugin loading disabled in Stable by default;
* Development provider endpoints prohibited in Stable;
* and Stable update channel cannot point to Preview packages.

---

## 45.1 Cross-Channel Profile Import

Creates candidates and revalidates.

---

# 46. Enterprise Machine Policy

Windows source:

```text
HKLM\Software\Policies\Opure
```

---

## 46.1 Authority

Machine administrator.

---

## 46.2 Scope

Machine and all users unless definition says otherwise.

---

## 46.3 Read Only

Opure does not write this key during ordinary use.

---

## 46.4 Unknown Value

Recorded as unknown policy metadata but not applied unless schema recognises it.

---

# 47. Enterprise User Policy

Windows source:

```text
HKCU\Software\Policies\Opure
```

---

## 47.1 Authority

Administrator-managed user policy.

---

## 47.2 User Preference Difference

A user preference lives outside `Software\Policies`.

---

## 47.3 Intersection

Cannot broaden machine policy.

---

# 48. Machine Preference

Ordinary machine-local Opure setting.

Suggested storage:

* Configuration database;
* or an Opure-owned non-policy registry location if required for bootstrap.

It is not enterprise authority.

---

# 49. Project Governance Policy

Suggested file:

```text
.opure\project.policy.json
```

---

## 49.1 Authority

Project governance within Product and Enterprise limits.

---

## 49.2 Source Trust

Untrusted repository content until validated.

---

## 49.3 Allowed Effect

Can make project rules stricter.

---

## 49.4 Prohibited Effect

Cannot elevate capabilities.

---

# 50. Workflow Policy

Bound to:

* Workflow Definition;
* Compiled Plan;
* and Workflow Instance.

---

## 50.1 Narrowing

Can restrict execution further.

---

## 50.2 Running Instance

Pinned according to ADR-0025.

---

# 51. Operation Capability Constraints

The final constraint layer.

---

## 51.1 Examples

* exact paths;
* exact provider;
* exact tool;
* maximum cost;
* no remote;
* no writes;
* and expiry.

---

## 51.2 One Operation

Not reusable.

---

# 52. Setting Sources

Every source record contains:

```text
source_id
source_kind
scope
location_or_reference
owner
priority_class
schema
classification
optional
active
created_at
```

---

## 52.1 Source Revision

Contains:

```text
source_revision_id
source_id
native_revision
content_sha256
canonical_sha256
parsed_document_reference
validation
observed_at
source_state
```

---

## 52.2 Source States

* Current;
* Missing Optional;
* Missing Required;
* Invalid;
* Inaccessible;
* Superseded;
* Quarantined;
* and Deleted.

---

# 53. Product Defaults

Compiled or packaged typed definitions.

---

## 53.1 No Mutable File Requirement

Defaults remain available if app-data files are damaged.

---

## 53.2 Validation

Test every default against current policy.

---

# 54. Channel Defaults

Packaged with channel.

---

## 54.1 Stable

Conservative.

---

## 54.2 Preview

May enable preview UI but not bypass security.

---

## 54.3 Development

May expose development options with visible posture.

---

# 55. User Base Profile

One active Profile per channel.

---

## 55.1 Created at First Run

From Product and Channel defaults.

---

## 55.2 User Ownership

Current user.

---

# 56. Named User Profile

Reusable choices.

---

## 56.1 Active Selection

Zero or one.

---

## 56.2 Project Compatibility

Re-evaluated per project.

---

## 56.3 Profile Name

No authority.

---

# 57. Project Shared Settings

Suggested file:

```text
.opure\project.settings.json
```

---

## 57.1 Source Control

Optional.

---

## 57.2 Review

Normal repository diff.

---

## 57.3 Branch Change

New source revision.

---

## 57.4 Secrets

Prohibited.

---

# 58. Project Local Profile

Stored outside repository:

```text
%LOCALAPPDATA%\Opure\<Channel>\Projects\<project-id>\Configuration\
```

---

## 58.1 Purpose

Developer-specific project settings.

---

## 58.2 Export

Optional.

---

## 58.3 Project Deletion

Handled with project lifecycle.

---

# 59. Workspace Session Settings

Ephemeral but durable enough for current session recovery.

Examples:

* selected project view;
* temporary performance preference;
* and current diagnostic verbosity.

---

## 59.1 Expiry

Session end or explicit time.

---

## 59.2 Workflow

Not inherited by running workflow unless pinned.

---

# 60. Session Overrides

Explicit user action.

---

## 60.1 Visibility

Banner or profile indicator.

---

## 60.2 Persistence

May survive Runtime restart within the session if declared.

---

## 60.3 Policy

Fully constrained.

---

# 61. Operation Overrides

Bound to:

* operation ID;
* Effective Snapshot;
* actor;
* expiry;
* and capability.

---

## 61.1 Example

Use Turbo mode for one local inference.

---

## 61.2 No Profile Mutation

Required.

---

## 61.3 Receipt

Required.

---

# 62. Profile Model

Suggested schema:

```text
opure.configuration-profile/1
```

---

## 62.1 Fields

```text
profile_id
revision
profile_kind
display_name
scope
parent_profile_revision
values
classification
created_by
created_at
profile_sha256
```

---

## 62.2 Profile Kinds

* User Base;
* Named User;
* Project Local;
* Machine Preference;
* Session;
* Test;
* and Imported Candidate.

---

# 63. Profile Inheritance

One parent in Version 1.

---

## 63.1 Parent Revision

Exact.

---

## 63.2 Cycle

Rejected.

---

## 63.3 Depth

Maximum 8 initially.

---

## 63.4 Parent Deletion

Blocked while referenced or child migration required.

---

## 63.5 Parent Update

Does not mutate a child revision.

A new active Effective Snapshot may select a newer parent only through an explicit Profile revision or selection transaction.

---

# 64. Profile Selection

Selection record contains:

```text
scope
user_base_profile
named_profile
project_local_profile
session_profile
selected_at
selected_by
selection_revision
```

---

## 64.1 Atomic

Profile selection and effective snapshot publication commit together.

---

# 65. Setting Merge

Merge is performed by Setting Definition.

---

## 65.1 Replace

Highest applicable explicit source wins.

---

## 65.2 Replace If Set

Absent values do not override.

---

## 65.3 First Explicit

Broad source wins by definition.

Rare and explicit.

---

## 65.4 Append

Source-order append.

---

## 65.5 Prepend

Source-order prepend.

---

## 65.6 Ordered Unique Append

Preserve first occurrence under declared equality.

---

## 65.7 Set Union

All values.

---

## 65.8 Set Intersection

Common values.

---

## 65.9 Map Merge

Per-key merge.

---

## 65.10 Map Replace

Whole map.

---

## 65.11 Rule List

Concatenate with explicit evaluation order.

---

## 65.12 Minimum

Lowest requested value.

---

## 65.13 Maximum

Highest requested value.

---

# 66. Merge Provenance

For every resulting collection element record:

* source;
* source revision;
* operation;
* position;
* replaced element;
* and reason.

---

## 66.1 Map Key Provenance

Per key.

---

## 66.2 Removed Entry

Visible.

---

# 67. Null Semantics

## 67.1 Not Specified

No effect.

---

## 67.2 Explicit Null

Only where the value type permits null.

---

## 67.3 Reset to Default

Special typed operation, not ambiguous raw null.

---

## 67.4 Remove Inherited Entry

Allowed for maps and sets according to definition.

---

## 67.5 Empty

Distinct from absent.

---

# 68. Deletion Markers

Use explicit typed document operations rather than magic strings.

Example:

```json
{
  "$remove": true
}
```

Only where the schema permits.

---

## 68.1 Ambiguous User Object

Reserved property names documented.

---

# 69. Custom Reducer

A custom reducer contract contains:

```text
reducer_id
revision
input_type
source_order
determinism
limits
implementation_hash
tests
owner
```

---

## 69.1 No Plugin Reducer

Initially prohibited.

---

## 69.2 No Model Reducer

Prohibited.

---

# 70. Strict JSON Format

Documents use:

* UTF-8;
* no BOM preferred but accepted only if explicitly tested;
* standard JSON strings;
* finite numbers;
* no comments;
* no trailing commas;
* no duplicate property names;
* one top-level object;
* and no trailing content.

---

## 70.1 Why No Comments

Avoid parser variation and canonicalisation ambiguity.

Descriptions belong in schemas and UI.

---

## 70.2 Human Editing

Provide UI, validation command and schema-aware editor support.

---

# 71. Duplicate Property Detection

Duplicate names are errors at every object depth.

---

## 71.1 Comparison

Use exact Unicode code-point property-name equality after JSON unescaping.

Do not apply case folding.

---

## 71.2 Setting IDs

Setting IDs themselves are lowercase canonical identifiers.

---

## 71.3 Parser Implementation

Use a streaming token reader and per-object bounded name set on .NET 10 unless a stable accepted API provides equivalent rejection.

---

## 71.4 Memory Bound

Enforce object property count limit.

---

# 72. JSON Number Handling

No:

* `NaN`;
* positive infinity;
* negative infinity;
* hexadecimal;
* leading plus;
* or implementation-specific extension.

---

## 72.1 Integer Overflow

Validation error.

---

## 72.2 Decimal Precision

Definition specific.

---

# 73. Unicode

Validate UTF-8 strictly.

---

## 73.1 Invalid Surrogate

Reject.

---

## 73.2 Normalisation

Setting identifiers use ASCII.

Human strings preserve input, with security checks where identity comparison occurs.

---

## 73.3 Confusable Identity

Opaque IDs and canonical enum values reduce risk.

---

# 74. JSON Schema Registry

Suggested schema catalogue:

```text
opure.configuration-schema-catalogue/1
```

---

## 74.1 Fields

```text
catalogue_id
revision
supported_draft
supported_vocabularies
schema_documents
format_assertions
remote_reference_policy
limits
created_at
catalogue_sha256
```

---

## 74.2 Draft

JSON Schema Draft 2020-12 subset.

---

## 74.3 Pinned Metaschema

Packaged locally.

---

# 75. Supported JSON Schema Keywords

Initial candidate set:

* `$schema`;
* `$id`;
* `$defs`;
* `$ref`;
* `type`;
* `const`;
* `enum`;
* `multipleOf`;
* `maximum`;
* `exclusiveMaximum`;
* `minimum`;
* `exclusiveMinimum`;
* `maxLength`;
* `minLength`;
* `pattern`;
* `maxItems`;
* `minItems`;
* `uniqueItems`;
* `prefixItems`;
* `items`;
* `contains`;
* `maxContains`;
* `minContains`;
* `maxProperties`;
* `minProperties`;
* `required`;
* `properties`;
* `patternProperties` where bounded;
* `additionalProperties`;
* `dependentRequired`;
* `propertyNames`;
* `allOf`;
* `anyOf`;
* `oneOf`;
* `not`;
* `if`;
* `then`;
* `else`;
* `unevaluatedProperties`;
* and metadata annotations.

Exact subset requires implementation evidence.

---

## 75.1 Dynamic References

Deferred initially unless the selected validator proves bounded predictable behaviour.

---

## 75.2 Remote Reference

Denied.

---

## 75.3 Regex

Use a bounded non-backtracking or timeout-protected implementation.

---

# 76. Format Assertions

Candidate formats:

* date-time;
* duration;
* URI;
* UUID;
* IPv4;
* IPv6;
* hostname;
* email where actually needed;
* and Opure-specific opaque references.

---

## 76.1 Annotation Versus Assertion

Explicit.

---

## 76.2 Unknown Format

Annotation or error according to schema vocabulary.

---

# 77. Schema Validation Result

Contains:

```text
document
schema
valid
errors
warnings
evaluated_properties
unknown_extensions
duration
validator_revision
```

---

## 77.1 Error Location

JSON Pointer and source byte range where practical.

---

## 77.2 No Source Content in Logs

Required.

---

# 78. Semantic Validation

Runs after schema binding.

---

## 78.1 Examples

* output token reserve less than context window;
* Local Only cannot have remote default provider;
* maximum parallel steps consistent with resource policy;
* retention minimum not greater than maximum;
* profile parent scope compatible;
* project path within project root;
* model profile installed;
* Provider Profile approved;
* plugin package exists;
* and workflow setting valid for new instances.

---

## 78.2 Validator Contract

```text
validator_id
revision
input_settings
input_policies
owner_service
determinism
timeout
implementation_hash
```

---

## 78.3 No Network

Default.

A service-reference existence check uses a trusted local service contract.

---

# 79. Cross-Setting Validation

May produce:

* Valid;
* Warning;
* Invalid Preference;
* Invalid Capability;
* Restart Required;
* Reindex Required;
* Migration Required;
* and Policy Conflict.

---

# 80. Project Settings Document

Suggested envelope:

```json
{
  "$schema": "https://schemas.opure.local/configuration/project-settings-v1.schema.json",
  "schema": "opure.project-settings/1",
  "document_id": "project-settings",
  "settings": {
    "project.cloud.policy": "local-only",
    "runtime.performance.default-mode": "balanced"
  }
}
```

---

## 80.1 Schema URI

Logical local identifier.

No network retrieval.

---

## 80.2 Revision

Source identity comes from hash and Workspace or repository revision.

---

# 81. Project Policy Document

Example:

```json
{
  "$schema": "https://schemas.opure.local/configuration/project-policy-v1.schema.json",
  "schema": "opure.project-policy/1",
  "document_id": "project-policy",
  "policies": {
    "policy.cloud.require-local": true,
    "policy.plugins.allow-development-mode": false
  }
}
```

---

## 81.1 Stricter Only

Policy evaluator enforces.

---

# 82. Project File Location

The `.opure` directory should be a verified direct child of the project root.

---

## 82.1 Reparse Points

ADR-0009 applies.

---

## 82.2 Case

Windows path comparison uses handle-verified identity.

---

## 82.3 Alternate Data Streams

Denied.

---

## 82.4 File Size

Bounded.

---

# 83. Project Source Acquisition

Workspace Service issues:

* project identity;
* logical relative path;
* file snapshot;
* bytes hash;
* file identity;
* workspace generation;
* and repository evidence.

---

## 83.1 Raw Path Read

Configuration Service does not open arbitrary paths supplied by the document.

---

# 84. File Watcher Hint

Watcher event contains:

* project;
* relative path;
* event kind;
* observed time;
* and watcher generation.

---

## 84.1 Not Authority

Required.

---

## 84.2 Debounce

Bounded.

---

## 84.3 Reconcile

Full read and hash.

---

# 85. Periodic Source Reconciliation

Suggested triggers:

* project focus;
* branch checkout;
* repository state event;
* every bounded interval while project open;
* and before critical operation if source freshness is uncertain.

---

## 85.1 No Constant Polling Storm

Use state and event hints.

---

# 86. Partial File Write

If parsing fails immediately after a change hint:

* wait boundedly;
* retry full read;
* and retain last valid revision.

---

## 86.1 Timeout

Show invalid source.

---

## 86.2 No Partial Apply

Required.

---

# 87. External Edit Conflict

If the active Profile source changed since an editor began:

* show base;
* current;
* proposed;
* and per-key conflict.

---

## 87.1 Automatic Merge

Only for non-conflicting typed keys and merge-safe collections.

---

## 87.2 Same Key

User review.

---

# 88. Opure File Write

Use:

* staged same-volume file;
* flush;
* schema revalidation;
* hash;
* handle verification;
* atomic replacement;
* directory flush where supported;
* and Workspace event.

---

## 88.1 Backup Copy

Optional bounded previous file.

---

# 89. Windows Registry Policy Schema

Policy values should use explicit names.

Example structure:

```text
HKLM\Software\Policies\Opure
    Cloud\RequireLocal
    Cloud\AllowedProviders
    Plugins\DevelopmentMode
    Updates\AllowedChannels
    Logging\MaximumRetentionDays
```

Exact value design requires ADMX review.

---

## 89.1 Registry Types

Use:

* `REG_DWORD`;
* `REG_SZ`;
* and bounded `REG_MULTI_SZ`

where practical.

---

## 89.2 Binary

Avoid.

---

## 89.3 JSON Value

Only for approved complex policy with strict schema.

---

# 90. Registry View

Opure is x64.

Read the native 64-bit policy view.

---

## 90.1 32-Bit Compatibility

Only if an explicit enterprise migration requires it.

---

## 90.2 Duplicate Views

Do not merge silently.

---

# 91. Registry Type Validation

A DWORD policy stored as a string is invalid.

---

## 91.1 Coercion

No implicit production coercion.

---

## 91.2 Diagnostic

Show expected and actual type without sensitive data.

---

# 92. Registry Change Detection

Use Windows registry change notification where practical.

---

## 92.1 Notification Loss

Periodic reconciliation.

---

## 92.2 Source Revision

Full reread and hash of recognised policy values.

---

# 93. Policy Refresh

Triggers:

* Runtime start;
* application start;
* session unlock;
* project open;
* registry change;
* explicit command;
* and periodic reconciliation.

---

## 93.1 Refresh Failure

Retain last observed policy only when doing so remains safe and record source uncertainty.

---

## 93.2 Policy Removal

Creates a new source revision.

---

# 94. ADMX and ADML

Opure should publish:

```text
Opure.admx
en-US\Opure.adml
```

Additional languages later.

---

## 94.1 ADMX Purpose

Describe policy UI and registry mapping.

---

## 94.2 Runtime

Opure reads registry values, not ADMX files.

---

## 94.3 Versioning

Template version aligned with Policy Definition catalogue.

---

## 94.4 Central Store

Enterprise administrators may distribute through Windows Group Policy Central Store.

---

# 95. ADMX Categories

Suggested:

* Cloud and Data Sharing;
* Local Models;
* AI Routing;
* Tools and Commands;
* Plugins;
* MCP;
* Updates;
* Logging and Diagnostics;
* Project Policy;
* Retention;
* and User Interface Restrictions.

---

# 96. ADMX Policy State

Prefer:

* Not Configured;
* Enabled with typed values;
* Disabled

where semantics are clear.

---

## 96.1 Not Configured

No constraint from that policy source.

---

## 96.2 Disabled

Policy-definition specific.

It should not always mean Boolean false.

---

# 97. Group Policy Provenance

Record:

* hive;
* key;
* value;
* registry type;
* data hash;
* observed time;
* and machine or user scope.

---

## 97.1 GPO Identity

Ordinary registry reading may not reveal the winning GPO.

Opure should not invent it.

---

## 97.2 RSoP Link

Trust Centre can instruct administrators to use Windows RSoP or Group Policy Results for exact GPO provenance.

---

# 98. MDM

Deferred.

---

## 98.1 Future Approach

Use ADMX-backed or native Policy CSP to produce supported policy values.

---

## 98.2 Runtime Contract

Same Policy Definitions.

---

# 99. Environment Variables

Approved prefix:

```text
OPURE_
```

---

## 99.1 Catalogue

Every variable maps to one bootstrap Setting Definition.

---

## 99.2 Unknown Variable

Ignored with Development diagnostic only.

---

## 99.3 Production Override

Cannot override policy.

---

## 99.4 Secret

Prohibited.

---

# 100. Suggested Bootstrap Variables

Examples subject to review:

```text
OPURE_CHANNEL
OPURE_DIAGNOSTICS
OPURE_LOG_LEVEL
OPURE_TEST_MODE
OPURE_TEST_FIXTURE_ROOT
OPURE_DEVELOPMENT_ENDPOINT
```

---

## 100.1 Stable Restrictions

Development endpoint variables ignored in Stable unless signed development policy permits.

---

# 101. Command-Line Configuration

Commands should use explicit typed options.

---

## 101.1 Profile Selection

Example conceptual:

```text
opure --profile <profile-id> <command>
```

---

## 101.2 Operation Override

Allowlisted per command.

---

## 101.3 Secret Arguments

Denied.

---

## 101.4 Process-Wide Override

Visible and included in snapshot.

---

# 102. Command-Line Precedence

It is not a universal “highest layer”.

A command parameter becomes an Operation Override only if:

* the command schema allows it;
* the Setting Definition allows operation scope;
* and policy permits it.

---

# 103. Configuration Source Precedence

For ordinary Replace settings:

```text
Product Default
< Channel Default
< Machine Preference
< User Base Profile
< Named User Profile
< Project Shared Settings
< Project Local Profile
< Workspace Session
< Explicit Session Override
< Explicit Operation Override
```

---

## 103.1 Policy Is Separate

No policy source appears in this value chain.

---

## 103.2 Definition Override

A Setting Definition may declare a different source order only through reviewed product code.

---

# 104. Policy Source Order

Order represents authority and diagnostics, but combination is usually intersection:

```text
Product Invariant
Release Channel
Enterprise Machine
Enterprise User
Project Governance
Workflow
Operation Capability
```

---

## 104.1 More Restrictive Lower Source

Allowed.

---

## 104.2 Broader Lower Source

No effect and visible.

---

# 105. Effective Value Calculation

For one setting:

1. load definition;
2. select applicable value sources;
3. validate each source value;
4. merge by strategy;
5. select applicable policies;
6. combine constraints;
7. detect conflict;
8. validate merged value against constraints;
9. apply safe fallback or invalid state;
10. record provenance.

---

# 106. Forced Value

A policy may force a value.

---

## 106.1 User Display

Show requested value and forced effective value.

---

## 106.2 Locked UI

Disable ordinary edit but permit source inspection.

---

## 106.3 Conflict

Two incompatible force values follow conflict rule.

---

# 107. Allowed and Denied Values

Effective allowed set:

```text
intersection(all allows) minus union(all denies)
```

subject to Product Invariant semantics.

---

## 107.1 Empty Set

Policy conflict.

---

# 108. Numeric Bounds

Effective range:

```text
max(all minimums) <= value <= min(all maximums)
```

---

## 108.1 Invalid Range

Policy conflict.

---

# 109. Path Constraints

Use verified logical paths, not string prefixes.

---

## 109.1 Allowed Roots

Intersect.

---

## 109.2 Denied Paths

Union.

---

## 109.3 Reparse

Resolved at use.

---

# 110. Provider Constraints

Provider selection uses:

* exact Provider Profile;
* operator;
* region;
* data class;
* project cloud policy;
* and operation capability.

---

## 110.1 Wildcards

Avoid broad wildcards.

---

## 110.2 Profile Revision

Exact or governed compatible selector.

---

# 111. Capability Constraints

Capabilities include:

* remote inference;
* network access;
* command execution;
* workspace write;
* repository write;
* plugin execution;
* MCP call;
* update installation;
* and diagnostic export.

---

## 111.1 Deny Wins

Any applicable deny denies unless Product Invariant defines a stricter specialised rule.

---

# 112. Cross-Setting Policy

Examples:

* remote default provider prohibited when cloud policy Local Only;
* automatic update checks prohibited when enterprise Manual Only;
* plugin development mode prohibited in Stable;
* external MCP prohibited for Confidential data;
* Turbo mode prohibited on battery under machine policy;
* and remote embeddings prohibited for secret-adjacent indexes.

---

# 113. Effective Configuration Snapshot

Suggested schema:

```text
opure.effective-configuration-snapshot/1
```

---

## 113.1 Fields

```text
snapshot_id
scope
schema_catalogue
setting_definition_catalogue
policy_definition_catalogue
profile_revisions
setting_source_revisions
policy_source_revisions
effective_values
effective_constraints
provenance
validation
impact_state
created_at
snapshot_sha256
```

---

## 113.2 Immutable

Required.

---

## 113.3 Secret

Only Vault references.

---

# 114. Snapshot Granularity

One root snapshot may reference component sub-snapshots.

---

## 114.1 Service Section

Each owner service receives exact relevant section and root hash.

---

## 114.2 Project Snapshot

Project-specific.

---

## 114.3 Operation Snapshot

Adds session and operation overrides.

---

# 115. Snapshot Identity

Canonical hash includes:

* schema versions;
* source revisions;
* effective typed values;
* effective constraints;
* and scope.

---

## 115.1 Display Metadata

Excluded where non-semantic.

---

# 116. Per-Key Provenance

Suggested fields:

```text
setting_id
effective_value_hash
default_source
contributing_sources
winning_source
merge_steps
policy_constraints
rejected_values
validation
activation
```

---

## 116.1 Sensitive Value

Hash or masked rendering.

---

## 116.2 Collection

Per element or map key.

---

# 117. Resultant Configuration

Views:

* Summary;
* Effective Values;
* Requested versus Effective;
* Policies;
* Source Order;
* Conflicts;
* Pending Activation;
* and Historical Snapshot.

---

## 117.1 Winning Source

For Replace.

---

## 117.2 Contributing Sources

For merge.

---

## 117.3 Policy Reason

Explicit.

---

# 118. Resultant Configuration Export

Safe report may include:

* identifiers;
* masked values;
* source types;
* policy sources;
* validation;
* and activation.

---

## 118.1 Registry Details

Optional administrator report.

---

## 118.2 Secrets

No values.

---

# 119. Configuration Change Transaction

Suggested schema:

```text
opure.configuration-change-transaction/1
```

---

## 119.1 Fields

```text
transaction_id
target_scope
target_profile
base_revision
requested_changes
actor
source
reason
catalogue_revisions
policy_snapshot
candidate_snapshot
validation
impact_plan
preview_sha256
approval
idempotency_key
state
created_at
committed_at
```

---

# 120. Change States

* Draft;
* Validating;
* Invalid;
* Policy Conflict;
* Approval Required;
* Ready;
* Committing;
* Committed;
* Committed — Pending Activation;
* Committed — Restart Required;
* Committed — Activation Failed;
* Cancelled;
* Superseded;
* and Quarantined.

---

# 121. Staged Change

Each item contains:

```text
setting_id
operation
old_value_hash
new_value
source_scope
expected_definition_revision
```

---

## 121.1 Operations

* Set;
* Reset;
* Remove;
* Add Element;
* Remove Element;
* Replace Map Key;
* Remove Map Key;
* Select Profile;
* and Clear Override.

---

# 122. Change Validation

Validate:

* actor;
* scope;
* Profile;
* base revision;
* definition;
* type;
* source authority;
* policy;
* semantic constraints;
* impact;
* and approval.

---

# 123. Optimistic Concurrency

Commit requires expected base Profile and source revisions.

---

## 123.1 Conflict

Return current and proposed diff.

---

## 123.2 Retry

User or caller rebases explicitly.

---

# 124. Change Preview

Show:

* requested changes;
* effective changes;
* policy-forced differences;
* dependent setting changes;
* services affected;
* restart requirements;
* workflow impact;
* provider impact;
* indexing impact;
* data-retention impact;
* and rollback availability.

---

## 124.1 Preview Hash

Approval binds exact preview.

---

# 125. Approval Requirements

Examples:

* enabling remote provider;
* enabling external MCP;
* enabling plugin development mode;
* changing update channel;
* reducing review;
* changing retention;
* enabling diagnostic export;
* and applying enterprise-policy exception where such exception is product permitted.

---

## 125.1 Policy Waiver

Not a normal setting change.

Requires an explicit separately governed waiver model where allowed.

---

# 126. Atomic Commit

One transaction commits:

* Profile revision;
* source revision if service owned;
* Effective Snapshot;
* change event;
* activation plan;
* history;
* and outbox.

---

## 126.1 External Project File

Workspace write occurs as a separate reviewed effect before or after service commit under a two-phase application plan.

---

## 126.2 Source-Controlled Edit

The repository file remains source authority.

A local Profile transaction should not pretend to commit repository history.

---

# 127. Project File Edit Flow

1. prepare typed change;
2. calculate candidate document;
3. validate;
4. preview file diff and effective impact;
5. acquire Workspace write capability;
6. atomically write file;
7. receive Workspace snapshot;
8. import source revision;
9. create Effective Snapshot;
10. publish activation.

---

## 127.1 Crash

Reconcile file hash.

---

# 128. Activation Plan

Suggested schema:

```text
opure.configuration-activation-plan/1
```

---

## 128.1 Fields

```text
activation_plan_id
from_snapshot
to_snapshot
affected_services
setting_changes
impact_classes
dependency_order
approvals
deadline
rollback_plan
created_at
plan_sha256
```

---

# 129. Runtime Application Classes

## 129.1 Immediate Hot Apply

Owner can apply atomically without interrupting work.

---

## 129.2 Next Read

New service read sees it.

---

## 129.3 Next Operation

Current operations remain pinned.

---

## 129.4 New Workflow Only

ADR-0025.

---

## 129.5 Reopen Project

Project state rebuilt.

---

## 129.6 Reindex Required

ADR-0022.

---

## 129.7 Reload Local Model

ADR-0020.

---

## 129.8 Restart Owning Service

One service.

---

## 129.9 Restart Runtime

All trusted local services.

---

## 129.10 Restart Desktop

UI only.

---

## 129.11 Restart Application

Desktop and Runtime.

---

## 129.12 Windows Restart

Rare enterprise or OS integration.

---

## 129.13 Migration Required

Explicit migration.

---

## 129.14 Unsupported While Active

Block until dependent work ends.

---

# 130. Impact Aggregation

The strongest applicable impact becomes the overall minimum action.

---

## 130.1 Independent Services

May activate separately.

---

## 130.2 Mixed State

Resultant Configuration shows each service's activated snapshot.

---

# 131. Activation Receipt

Fields:

```text
receipt_id
service
snapshot_id
section_hash
activation_result
activated_at
restart_required
failure
service_version
```

---

## 131.1 Exact Section

Required.

---

## 131.2 Failure

Does not erase committed change.

---

# 132. Activation Dependency Order

Examples:

* policy service before AI Router;
* Provider Trust before Routing;
* Workspace before Project Knowledge reindex;
* Local Runtime unload before model-profile activation;
* and Workflow defaults before new instance creation.

---

# 133. Partial Activation

Possible.

---

## 133.1 User Display

Clearly show:

* committed;
* active in service A;
* pending in service B;
* restart needed in service C;
* and failed in service D.

---

## 133.2 Critical Inconsistency

Stop affected capability.

---

# 134. Dynamic Configuration Safety

A service should not mutate a shared options object in place.

---

## 134.1 Immutable Options

Build new instance.

---

## 134.2 Atomic Swap

Owner service chooses safe boundary.

---

## 134.3 Existing Operation

Keeps old bound revision.

---

# 135. .NET Configuration Adapter

An adapter may implement:

* typed binding;
* generated validation;
* immutable options;
* and change subscription.

---

## 135.1 `IConfiguration`

Internal convenience only.

---

## 135.2 `IOptions<T>`

Static process lifetime options where appropriate.

---

## 135.3 `IOptionsMonitor<T>`

May expose latest activated snapshot.

---

## 135.4 Authority

Always the Configuration Service snapshot, not raw providers.

---

# 136. Startup Validation

Critical services validate their configuration section before Ready.

---

## 136.1 `ValidateOnStart`

May be used.

---

## 136.2 Failure

Service enters Not Ready with structured reason.

---

## 136.3 Optional Preference

Safe default.

---

# 137. Binding Source Generation

Use stable supported .NET source-generated binding and validation where beneficial.

---

## 137.1 Reflection

Not prohibited generally, but generated code improves explicitness and AOT readiness.

---

## 137.2 Generated Validator

Still requires semantic validators.

---

# 138. Last Known Good

Track per:

* root scope;
* service section;
* project;
* and owner service.

---

## 138.1 Candidate Invalid

Retain current active.

---

## 138.2 Current Policy Prohibits Active

Disable affected capability.

---

## 138.3 Source Missing

Definition-specific.

---

# 139. Safe Default

A safe default is packaged and validated.

---

## 139.1 Use

Only when the setting's Failure Class allows it.

---

## 139.2 Security Critical

Fail closed rather than guess.

---

# 140. Invalid User Preference

Example:

* invalid theme;
* invalid editor font size;
* invalid panel width.

Use safe default and show issue.

---

# 141. Invalid Operational Setting

Example:

* impossible concurrency;
* invalid model context;
* invalid index size.

Disable or preserve previous active according to policy.

---

# 142. Invalid Security Policy

Disable affected capability.

---

## 142.1 Application Startup

Only block whole application when no safe diagnostic or repair mode exists.

---

# 143. Policy Conflict Handling

## 143.1 Non-Critical UI Setting

Use Product safe default and show conflict.

---

## 143.2 Provider Policy

Disable remote use.

---

## 143.3 Tool Policy

Deny affected tool capability.

---

## 143.4 Update Policy

Manual update only.

---

## 143.5 Persistence Policy

Stop new writes if durability cannot be established.

---

# 144. Policy Uncertainty

Registry inaccessible or source unreadable.

---

## 144.1 Previously Known Policy

May remain temporarily with warning when safe.

---

## 144.2 Cannot Establish Required Policy

Fail closed for controlled capability.

---

# 145. Source Reconciliation State

Contains:

```text
source_id
last_observed_revision
last_valid_revision
last_error
watcher_state
next_reconcile
freshness
```

---

# 146. Branch Checkout

Repository event triggers project-source reread.

---

## 146.1 Changed Effective Snapshot

Calculate impact before new operation.

---

## 146.2 Running Workflow

Remains pinned unless its policy requires live invalidation.

---

## 146.3 Critical New Denial

Can revoke capability according to policy.

---

# 147. Project Configuration Freshness

States:

* Current;
* Watcher Hint Pending;
* Reconciliation Pending;
* Source Changed;
* Invalid Source;
* Missing;
* Inaccessible;
* and Unknown.

---

## 147.1 Critical Operation

Requires Current or explicit last-known-good policy.

---

# 148. Rollback

Rollback creates a new revision.

---

## 148.1 Input

Prior Profile revision or change transaction.

---

## 148.2 Revalidation

Current definitions and policies.

---

## 148.3 Impact

New.

---

## 148.4 No History Erasure

Required.

---

# 149. Configuration History Event

Types:

* Definition Registered;
* Policy Definition Registered;
* Source Added;
* Source Revised;
* Source Invalid;
* Source Removed;
* Profile Created;
* Profile Revised;
* Profile Selected;
* Change Proposed;
* Change Validated;
* Change Approved;
* Change Committed;
* Activation Planned;
* Activation Succeeded;
* Activation Failed;
* Restart Required;
* Policy Conflict Detected;
* Rollback Committed;
* Migration Applied;
* Import Completed;
* Export Completed;
* Deleted;
* and Purged.

---

# 150. History Hash Chain

Optional integrity evidence.

---

## 150.1 Scope

Per Configuration database or Profile.

---

## 150.2 Limitation

Not same-user tamper proof.

---

# 151. Schema Migration

Suggested schema:

```text
opure.configuration-migration/1
```

---

## 151.1 Fields

```text
migration_id
source_schema
target_schema
applicable_documents
operations
validator
rollback
implementation_hash
created_at
migration_sha256
```

---

# 152. Migration Operations

* Rename Setting;
* Move Setting;
* Split Setting;
* Combine Settings;
* Convert Type;
* Convert Unit;
* Map Enumeration;
* Insert Default;
* Remove Deprecated;
* Convert Policy;
* and Custom Trusted Transform.

---

## 152.1 Pure

No I/O.

---

## 152.2 Deterministic

Required.

---

## 152.3 Idempotent

Required.

---

# 153. Migration Flow

1. read immutable source revision;
2. select migration path;
3. transform in memory;
4. validate target schema;
5. compare semantics;
6. preview;
7. create new revision;
8. create new Effective Snapshot;
9. preserve source relation;
10. activate according to impact.

---

# 154. Unknown Future Schema

Do not load.

---

## 154.1 UI

Show required newer Opure version.

---

# 155. Deprecated Setting

Metadata:

```text
deprecated_in
replacement
automatic_migration
removal_in
warning
```

---

## 155.1 Use

Accepted until removal under warning.

---

## 155.2 Policy Definition

Deprecation may be stricter for security settings.

---

# 156. Removed Setting

Requires migration or validation error.

---

## 156.1 Silent Ignore

Prohibited for critical settings.

---

# 157. Unknown Extension Settings

A document may contain a defined `extensions` object.

---

## 157.1 Namespace

Plugin package or future product namespace.

---

## 157.2 Activation

Unknown extension is preserved as data but not activated.

---

## 157.3 Plugin Installed

Plugin Configuration Definition must validate it.

---

# 158. Plugin Configuration

Plugin may register Setting Definitions under:

```text
plugin.<publisher>.<plugin-id>.*
```

---

## 158.1 Registration

During package validation.

---

## 158.2 Scope

Plugin own scope or project within capability.

---

## 158.3 Policy

Cannot broaden product or enterprise policy.

---

## 158.4 Uninstall

Retain or remove inactive configuration according to user choice.

---

# 159. Plugin Policy

A plugin may expose its own settings.

It cannot register a Product Invariant or Enterprise Policy Definition.

---

## 159.1 Host Policy

First-party Plugin Platform definitions govern plugin execution.

---

# 160. MCP Configuration

MCP server profiles remain owned by MCP Gateway.

Configuration Service stores references and non-secret choices.

---

## 160.1 Credential

Vault reference.

---

## 160.2 Server Arguments

Typed and capability controlled.

---

## 160.3 Project File

Cannot grant server trust.

---

# 161. Provider Configuration

Provider Profile remains owned by Provider Trust.

---

## 161.1 Configuration Value

Exact Provider Profile reference.

---

## 161.2 Credentials

Vault.

---

## 161.3 Terms

Not copied into generic settings.

---

# 162. Local Model Configuration

Model Profile and Execution Profile remain owned by Local Model Runtime.

---

## 162.1 Configuration Value

Opaque exact references.

---

## 162.2 File Paths

Model service owns.

---

# 163. Workflow Configuration

Workflow Definitions may reference setting IDs and snapshot policies.

---

## 163.1 New Instances

Use selected snapshot.

---

## 163.2 Running Instances

Pinned.

---

## 163.3 Live Policy Revocation

Explicit policy only.

---

# 164. AI Routing Configuration

Routing Policy references are exact.

---

## 164.1 Change

Next operation by default.

---

## 164.2 Running Inference

Unaffected.

---

# 165. Context Configuration

Context Policy references are exact.

---

## 165.1 Reindex

Only when source indexing semantics change.

---

# 166. Knowledge Configuration

Settings include:

* enabled channels;
* size limits;
* embedding profile;
* and excluded paths.

---

## 166.1 Impact

May require reindex.

---

# 167. Memory Configuration

Settings include:

* capture defaults;
* retention;
* review reminders;
* local embedding profile;
* and context-use policy.

---

## 167.1 Policy

No automatic model authority.

---

# 168. Logging Configuration

Separate:

* log level;
* retention;
* diagnostic detail;
* export;
* and remote export.

---

## 168.1 Secret Redaction

Cannot be disabled by user setting.

---

# 169. Update Configuration

Settings include:

* Manual Only;
* Check on Launch;
* channel;
* source;
* and notification.

---

## 169.1 Policy

ADR-0015 remains authoritative.

---

# 170. Desktop Configuration

Examples:

* theme;
* accent;
* layout;
* density;
* and accessibility preferences.

---

## 170.1 User Scope

Usually.

---

## 170.2 Policy

Enterprise may restrict selected features, not accessibility without strong reason.

---

# 171. Import Bundle

Suggested schema:

```text
opure.configuration-bundle/1
```

---

## 171.1 Files

```text
manifest.json
profiles.jsonl
settings.jsonl
policies.jsonl
sources.jsonl
hashes.json
```

---

## 171.2 No Executable Content

Required.

---

# 172. Import Pipeline

```text
Select bundle
    ↓
Validate archive paths and limits
    ↓
Verify hashes
    ↓
Parse strict JSON
    ↓
Validate schemas
    ↓
Secret scan
    ↓
Map scopes and references
    ↓
Detect unknown definitions
    ↓
Apply current policies
    ↓
Preview effective changes
    ↓
Create candidate Profiles
    ↓
Explicit activation
```

---

## 172.1 Imported Enterprise Policy

Cannot become Enterprise authority.

---

## 172.2 Imported Product Policy

Cannot become Product Invariant.

---

# 173. Export Preview

Show:

* Profiles;
* values;
* masked sensitive values;
* Vault references;
* policy sources;
* project references;
* personal data;
* unknown extensions;
* and exclusions.

---

## 173.1 Machine Policy

Excluded by default.

---

## 173.2 Project Shared Files

May be included by reference or content with approval.

---

# 174. Profile Sharing

Export and import only initially.

---

## 174.1 Cloud Sync

Deferred.

---

## 174.2 Repository Sharing

Use explicit project settings instead of copying a user Profile into source control blindly.

---

# 175. Configuration Deletion

Delete:

* inactive Profile;
* imported candidate;
* session override;
* project local Profile;
* history payload;
* or project configuration state.

---

## 175.1 Active Profile

Requires selection change first.

---

## 175.2 Referenced Revision

Retain according to operation and workflow history.

---

# 176. Purge

Removes:

* inactive values;
* import payloads;
* export staging;
* old activation payloads;
* and eligible history content.

---

## 176.1 Tombstone

Safe IDs and reason.

---

## 176.2 Forensic Limit

Displayed.

---

# 177. Retention

Suggested:

* definitions: product lifetime;
* Profile revisions: channel lifetime while referenced;
* effective snapshots: 180 days or while referenced;
* change transactions: 180 days;
* activation receipts: 180 days;
* invalid source payloads: 30 days;
* imports: 90 days;
* exports: 30 days;
* policy conflicts: 180 days;
* and session overrides: session plus 7 days.

Provisional.

---

# 178. Backup

Configuration database contains non-rebuildable user choices.

Include in profile backup.

---

## 178.1 Contents

* database;
* selected schemas;
* Profile exports;
* integrity manifest;
* and safe project-local references.

---

## 178.2 Secrets

No values.

---

# 179. Restore

Validate:

* database;
* definitions;
* schemas;
* policy sources;
* project mapping;
* service references;
* and current product version.

---

## 179.1 Another Machine

Machine policy re-evaluated.

---

## 179.2 Missing Provider or Model

Affected settings invalid or inactive.

---

# 180. Startup Recovery

1. open Configuration database;
2. validate schema and migrations;
3. run integrity checks;
4. load Product and Channel definitions;
5. load last valid Profiles;
6. read Enterprise Policy;
7. reconcile project sources for open projects;
8. build Effective Snapshots;
9. compare last activated snapshots;
10. publish safe service sections;
11. record conflicts and pending activation;
12. enter repair mode where required.

---

## 180.1 Product Definition Mismatch

Migrate or stop affected capability.

---

## 180.2 Database Corruption

Restore backup or reconstruct from exports and source files.

---

# 181. Integrity Checks

Check:

* SQLite integrity;
* foreign keys;
* definition hashes;
* schema hashes;
* source hashes;
* Profile hashes;
* inheritance cycles;
* effective snapshot hashes;
* provenance completeness;
* current projection;
* activation receipts;
* and history chain if enabled.

---

# 182. Configuration Health

States:

* Healthy;
* Healthy with Pending Activation;
* Healthy with Warnings;
* Policy Conflict;
* Invalid Preference;
* Critical Setting Invalid;
* Source Stale;
* Migration Required;
* Recovery Required;
* and Quarantined.

---

# 183. Trust Centre

Views:

```text
Effective
Requested versus Effective
Profiles
Project Settings
Project Policy
Enterprise Machine Policy
Enterprise User Policy
Session and Operation Overrides
Pending Activation
Restart Requirements
Validation
Conflicts
History
Schema and Migration
Imports and Exports
Health
```

---

## 183.1 Setting Detail

Show:

* definition;
* effective value;
* requested values;
* merge;
* policies;
* source provenance;
* validation;
* active service revision;
* impact;
* history;
* and actions.

---

# 184. Desktop Configuration UI

Suggested areas:

* General;
* Appearance;
* Projects;
* Local Models;
* Cloud Providers;
* AI Routing;
* Context;
* Knowledge;
* Memory;
* Workflows;
* Tools;
* Plugins;
* MCP;
* Updates;
* Logging;
* Privacy;
* Enterprise Policy;
* and Advanced.

---

## 184.1 Search

By setting name, description and ID.

---

## 184.2 Policy Lock

Explain source.

---

## 184.3 Source-Controlled Setting

Offer open-file or edit-through-Opure action.

---

# 185. UI Copy — Policy

Suggested:

> This value is constrained by policy. Your Profile can request a different setting, but Opure will use only values permitted by Product, Enterprise, Project and operation-specific rules. Expand the policy evidence to see each contributing constraint.

---

# 186. UI Copy — Pending Restart

Suggested:

> The change is committed but is not active in every service. Existing operations continue with their pinned configuration. Complete the listed restart, reload or reindex action to activate the new snapshot.

---

# 187. UI Copy — Invalid External Edit

Suggested:

> Opure detected a change to the project configuration file, but the complete document does not currently pass strict JSON, schema or policy validation. The last valid configuration remains active. No partial values were applied.

---

# 188. UI Copy — Policy Conflict

Suggested:

> Applicable policies define incompatible requirements, so Opure cannot calculate a permitted value. The affected capability is disabled or using the displayed safe product default until an administrator or project owner resolves the conflict. Opure has not selected a silent winner.

---

# 189. UI Copy — Last Known Good

Suggested:

> The latest requested configuration is invalid or could not be activated. Opure is continuing with the last validated and activated snapshot only where that snapshot still satisfies current policy. Affected critical capabilities are disabled when safe compliance cannot be established.

---

# 190. Diagnostics

Safe diagnostics may include:

* snapshot ID;
* Profile ID;
* definition ID;
* source type;
* policy type;
* validation code;
* impact class;
* activation result;
* conflict class;
* and duration.

---

## 190.1 Prohibited Diagnostics

Do not log:

* raw sensitive values;
* Vault references where unnecessary;
* provider credentials;
* full project documents;
* personal data;
* command-line secrets;
* or registry values containing sensitive text.

---

# 191. Metrics

Low-cardinality local metrics:

* valid snapshots;
* invalid sources;
* policy conflicts;
* pending restarts;
* activation failures;
* migration count;
* source-reconciliation latency;
* and change-transaction latency.

---

## 191.1 No Project Labels

Do not export project identity.

---

# 192. Error Model

Stable categories:

* Configuration Definition Missing;
* Configuration Definition Invalid;
* Configuration Policy Definition Missing;
* Configuration Policy Definition Invalid;
* Configuration Schema Unsupported;
* Configuration Schema Invalid;
* Configuration Source Missing;
* Configuration Source Inaccessible;
* Configuration Source Invalid;
* Configuration Duplicate Property;
* Configuration Invalid UTF-8;
* Configuration Size Limit;
* Configuration Scope Denied;
* Configuration Source Denied;
* Configuration Value Invalid;
* Configuration Merge Failed;
* Configuration Policy Conflict;
* Configuration Policy Denied;
* Configuration Secret Detected;
* Configuration Profile Missing;
* Configuration Profile Invalid;
* Configuration Profile Cycle;
* Configuration Profile Depth;
* Configuration Revision Conflict;
* Configuration Approval Required;
* Configuration Preview Changed;
* Configuration Commit Failed;
* Configuration Activation Failed;
* Configuration Restart Required;
* Configuration Reindex Required;
* Configuration Migration Required;
* Configuration Import Invalid;
* Configuration Export Denied;
* Configuration Registry Type Invalid;
* Configuration Enterprise Policy Unavailable;
* Configuration Project Changed;
* Configuration Channel Mismatch;
* Configuration Operation Expired;
* Configuration Recovery Required;
* and Configuration Quarantined.

---

# 193. Security Threat Model

Relevant threats include:

* policy bypass through later settings;
* policy bypass through environment variables;
* policy bypass through command-line arguments;
* policy bypass through project files;
* policy bypass through imported Profiles;
* policy bypass through plugins;
* policy bypass through MCP servers;
* Product Invariant substitution;
* Setting Definition substitution;
* Policy Definition substitution;
* schema substitution;
* remote `$ref` retrieval;
* duplicate JSON properties;
* Unicode-confusable setting identity;
* parser differential;
* partial file application;
* watcher-event loss;
* watcher-event race;
* source-path substitution;
* reparse-point substitution;
* registry-view confusion;
* registry-type coercion;
* machine-policy impersonation;
* user-preference impersonation as policy;
* stale policy;
* empty permitted-space concealment;
* forced-value conflict concealment;
* secret in JSON;
* secret in registry;
* secret in environment variable;
* secret in command line;
* malicious URI;
* malicious path;
* custom reducer abuse;
* Profile inheritance cycle;
* Profile parent substitution;
* stale approval reuse;
* configuration rollback to insecure value;
* activation split brain;
* service using stale snapshot;
* operation reading mutable values midway;
* migration data loss;
* deprecated-setting resurrection;
* imported authority escalation;
* and configuration-history tampering.

---

# 194. Security Controls

Controls include:

* separate setting and policy models;
* immutable Product Invariant Policy;
* typed Policy Definitions;
* constraint intersection;
* deny-wins capability policy;
* source-authority validation;
* strict JSON;
* duplicate-property rejection;
* local pinned schemas;
* no remote `$ref`;
* bounded parsing;
* schema-defined merge;
* trusted reducers only;
* one-parent bounded Profile inheritance;
* exact source revisions;
* Workspace file capabilities;
* registry hive and type validation;
* machine and user policy separation;
* no secrets in ordinary sources;
* Vault references;
* typed environment-variable catalogue;
* typed command-line catalogue;
* immutable Effective Snapshots;
* per-key provenance;
* exact operation binding;
* atomic changes;
* approval preview hashes;
* owner-service activation receipts;
* last-known-good subject to current policy;
* rollback revalidation;
* migration provenance;
* and Trust Centre evidence.

---

# 195. Security Limitations

This design cannot guarantee:

* that an enterprise administrator chooses safe policy;
* that a project owner chooses useful settings;
* perfect secret detection;
* perfect JSON-schema implementation;
* perfect detection of same-user database tampering;
* that registry change notification is never lost;
* or that every third-party plugin setting is semantically safe.

Product invariants and capability boundaries reduce the impact of unsafe configuration.

They do not replace operating-system and account security.

---

# 196. Privacy Impact

Configuration can reveal:

* provider choices;
* project preferences;
* local model use;
* plugin and MCP selections;
* personal UI preferences;
* and enterprise restrictions.

---

## 196.1 Data Minimisation

Persist only required values and provenance.

---

## 196.2 Sensitive Display

Mask.

---

## 196.3 Remote Processing

Configuration validation is local.

---

## 196.4 Export

Review personal and enterprise data.

---

# 197. Reliability Impact

Immutable snapshots and last-known-good state improve reliability.

Policy conflicts and activation failures may disable capabilities rather than silently continuing.

---

# 198. Performance Impact

Costs include:

* strict parsing;
* schema validation;
* policy evaluation;
* semantic validation;
* source reconciliation;
* snapshot hashing;
* and service activation.

These occur on changes and operation planning, not on every property access.

---

# 199. Provisional Performance Targets

On reference hardware:

* definition lookup p95: under 5 ms;
* Profile revision lookup p95: under 10 ms;
* effective snapshot build for 5,000 settings p95: under 100 ms;
* policy intersection for 5,000 constraints p95: under 100 ms;
* strict parse of a 4 MiB document p95: under 250 ms;
* project-source reconciliation p95: under 500 ms excluding Workspace delay;
* configuration commit p95: under 50 ms;
* Resultant Configuration key lookup p95: under 10 ms;
* and startup load for 10,000 definitions and 100 Profiles: under 2 seconds.

These require evidence.

---

# 200. Scale Targets

Test:

* 10,000 Setting Definitions;
* 10,000 Policy Definitions;
* 100,000 Profile revisions;
* 10,000 values in one Profile;
* 10,000 constraints in one source;
* 10,000 open-project snapshots;
* 1,000 simultaneous pending changes;
* and 1,000,000 history events.

These are architecture stress targets.

---

# 201. Testing Strategy

ADR-0008 applies.

Configuration and policy require:

* schema tests;
* parser tests;
* duplicate-key tests;
* Setting Definition tests;
* Policy Definition tests;
* scope tests;
* source tests;
* Profile tests;
* merge tests;
* null tests;
* policy-intersection tests;
* conflict tests;
* registry tests;
* ADMX tests;
* project-file tests;
* environment tests;
* command-line tests;
* snapshot tests;
* provenance tests;
* transaction tests;
* activation tests;
* last-known-good tests;
* migration tests;
* import and export tests;
* security tests;
* recovery tests;
* fuzzing;
* performance tests;
* and adversarial policy-bypass suites.

---

# 202. Database Tests

Test:

* create;
* migrate;
* integrity;
* foreign keys;
* concurrent readers;
* one writer;
* transaction rollback;
* outbox;
* backup;
* restore;
* corruption;
* and channel isolation.

---

# 203. Definition Catalogue Tests

Test:

* valid catalogue;
* duplicate setting ID;
* duplicate policy ID;
* unsupported revision;
* missing owner;
* invalid default;
* default violating Product Policy;
* and canonical hash.

---

# 204. Setting ID Tests

Test:

* lowercase;
* uppercase;
* whitespace;
* Unicode;
* empty segment;
* double dot;
* leading dot;
* trailing dot;
* excessive length;
* and renamed alias.

---

# 205. Setting Definition Tests

Test every field.

---

## 205.1 Missing Merge Strategy

Failure.

---

## 205.2 Missing Null Semantics

Failure where null can appear.

---

## 205.3 Invalid Scope

Failure.

---

## 205.4 Invalid Source

Failure.

---

## 205.5 Secret Default

Failure.

---

# 206. Policy Definition Tests

Test:

* every constraint type;
* source authority;
* scope rule;
* combination rule;
* conflict rule;
* failure class;
* and hash.

---

# 207. Product Invariant Tests

Attempt to change through:

* User Profile;
* Named Profile;
* Project Settings;
* Project Policy;
* Project Local Profile;
* Session Override;
* Operation Override;
* environment;
* command line;
* plugin;
* MCP;
* import;
* and registry user preference.

Every attempt fails or is ignored visibly.

---

# 208. Value-Type Tests

Test:

* Boolean;
* signed integer;
* unsigned integer;
* decimal;
* string;
* duration;
* byte size;
* instant;
* enumeration;
* URI;
* logical path;
* opaque reference;
* ordered list;
* set;
* map;
* object;
* union;
* and rule list.

---

# 209. Numeric Tests

Test:

* minimum;
* maximum;
* overflow;
* underflow;
* exponent;
* excessive precision;
* negative zero;
* and invalid non-finite values.

---

# 210. Duration Tests

Test:

* zero;
* positive;
* negative when denied;
* maximum;
* fractional;
* canonical form;
* and overflow.

---

# 211. Byte-Size Tests

Test:

* bytes;
* display conversion;
* zero;
* maximum;
* negative;
* and ambiguous unit string.

---

# 212. URI Tests

Test:

* HTTPS;
* HTTP denied;
* file URI;
* custom scheme;
* username and password;
* fragment;
* international host;
* localhost;
* and allowlisted host.

---

# 213. Logical-Path Tests

Test:

* project relative;
* absolute;
* drive relative;
* UNC;
* device path;
* traversal;
* reparse point;
* alternate stream;
* case difference;
* and path outside scope.

---

# 214. Opaque-Reference Tests

Test:

* valid Provider Profile;
* missing Provider Profile;
* valid Model Profile;
* removed plugin;
* changed MCP fingerprint;
* valid Vault reference;
* and another project.

---

# 215. Collection-Limit Tests

Test maximum:

* list;
* set;
* map;
* rule list;
* nesting;
* and encoded size.

---

# 216. Scope Tests

Test every scope and parent relationship.

---

## 216.1 Scope Abuse

Attempt:

* Project setting at Machine;
* Machine setting in project file;
* Provider setting in plugin scope;
* other project;
* other channel;
* and System setting in user Profile.

---

# 217. Allowed-Source Tests

For every critical setting, attempt every disallowed source.

---

# 218. Sensitivity Tests

Verify inheritance, masking, export and diagnostics.

---

# 219. Secret-Policy Tests

Test:

* plain key;
* token;
* password;
* private key;
* connection string;
* Vault reference;
* missing Vault reference;
* and secret-derived Boolean.

---

# 220. Failure-Class Tests

Create invalid settings in every class and verify safe behaviour.

---

# 221. Constraint-Type Tests

Test:

* Force Value;
* Allow Values;
* Deny Values;
* Minimum;
* Maximum;
* Require True;
* Require False;
* Require Capability;
* Deny Capability;
* Require Review;
* Maximum Data Class;
* Allowed Providers;
* Allowed Regions;
* Allowed Paths;
* Denied Paths;
* Maximum Cost;
* Retention Bound;
* Require Local;
* Require Offline;
* Lock;
* and Custom Constraint.

---

# 222. Policy Combination Tests

Test each combination rule with:

* one source;
* two compatible sources;
* two conflicting sources;
* absent source;
* lower stricter source;
* lower broader source;
* and Product Invariant.

---

# 223. Empty Permitted Space Tests

Examples:

* provider allowlist intersection empty;
* numeric minimum above maximum;
* allowed path fully denied;
* required and denied capability;
* and conflicting force value.

Verify explicit conflict.

---

# 224. Force-Value Tests

Test:

* one force;
* same force from two sources;
* conflicting user and machine force;
* conflicting machine and Product Invariant;
* requested different value;
* and rollback.

---

# 225. Deny-Wins Tests

Capabilities denied by any applicable source remain denied.

---

# 226. Machine-Precedence Tests

Only definitions explicitly allowing machine precedence use it.

---

# 227. Source-Registration Tests

Test every source kind, optionality and scope.

---

# 228. Source-Revision Tests

Test:

* same bytes;
* changed bytes;
* same native revision different bytes;
* missing;
* inaccessible;
* invalid;
* superseded;
* quarantined;
* and deleted.

---

# 229. Product-Default Tests

Remove app-data database and verify packaged defaults remain.

---

# 230. Channel-Default Tests

Verify Stable, Preview and Development separation.

---

# 231. User-Base-Profile Tests

Test creation, selection, revision and recovery.

---

# 232. Named-Profile Tests

Test:

* create;
* rename;
* revise;
* select;
* unselect;
* delete;
* export;
* import;
* and incompatible project policy.

---

# 233. Project-Shared-Settings Tests

Test:

* absent;
* valid;
* invalid;
* changed branch;
* source-control conflict;
* secret;
* unsupported setting;
* and scope denial.

---

# 234. Project-Policy Tests

Attempt stricter and broader rules.

Broader rules have no elevating effect.

---

# 235. Project-Local-Profile Tests

Test project identity, deletion and clone.

---

# 236. Session-Override Tests

Test creation, visibility, expiry, Runtime restart and clear.

---

# 237. Operation-Override Tests

Test:

* eligible setting;
* disallowed setting;
* policy denial;
* expiry;
* wrong operation;
* replay;
* and receipt.

---

# 238. Profile-Schema Tests

Test every field and canonical hash.

---

# 239. Profile-Inheritance Tests

Test:

* no parent;
* one parent;
* depth 8;
* depth 9;
* self cycle;
* indirect cycle;
* wrong scope;
* missing revision;
* parent deletion;
* and parent update.

---

# 240. Multiple-Inheritance-Denial Tests

Attempt two parents.

---

# 241. Profile-Selection Tests

Test concurrent selection and effective snapshot commit.

---

# 242. Replace-Merge Tests

Test every source level.

---

# 243. Replace-If-Set Tests

Test absent, null, empty and explicit.

---

# 244. First-Explicit Tests

Verify broad source behaviour is definition controlled.

---

# 245. Append-and-Prepend Tests

Test ordering and duplicates.

---

# 246. Ordered-Unique Tests

Test equality, case, order and removal.

---

# 247. Set Tests

Test union, intersection, empty, duplicate and canonical ordering.

---

# 248. Map-Merge Tests

Test:

* unique keys;
* same key;
* remove key;
* nested value;
* null;
* and provenance.

---

# 249. Rule-List Tests

Test evaluation order and maximum count.

---

# 250. Minimum-and-Maximum-Merge Tests

Test requested numeric profile behaviour separately from policy bounds.

---

# 251. Custom-Reducer Tests

Test:

* deterministic output;
* timeout;
* excessive memory;
* exception;
* changed implementation;
* plugin attempt;
* model attempt;
* and hash.

---

# 252. Null-Semantics Tests

Test every declared semantic.

---

# 253. Removal-Marker Tests

Test reserved property, invalid context and ambiguity.

---

# 254. Strict-JSON Tests

Test:

* valid UTF-8;
* UTF-8 BOM;
* invalid UTF-8;
* comment;
* trailing comma;
* duplicate key;
* duplicate nested key;
* trailing content;
* empty file;
* top-level array;
* excessive depth;
* huge number;
* and invalid escape.

---

# 255. Duplicate-Key Tests

Test duplicates expressed through:

* identical text;
* escaped equivalent property name;
* nested object;
* case difference;
* Unicode difference;
* and large object.

---

## 255.1 Expected

Identical decoded property names fail.

Case-different names remain distinct at JSON level but Setting ID schema may reject uppercase or unknown ID.

---

# 256. Parser-Differential Tests

Run documents through:

* streaming duplicate detector;
* System.Text.Json binding;
* JSON Schema validator;
* and canonical writer.

All must agree on accepted structure.

---

# 257. JSON-Number Tests

Test every invalid extension.

---

# 258. Unicode Tests

Test:

* invalid surrogate;
* overlong UTF-8;
* normalised and non-normalised strings;
* confusable display name;
* and ASCII setting identity.

---

# 259. Schema-Catalogue Tests

Test supported draft, vocabulary and hash.

---

# 260. Remote-Reference Tests

Attempt:

* HTTPS `$ref`;
* HTTP `$ref`;
* file `$ref`;
* UNC `$ref`;
* package `$ref`;
* and unknown local `$id`.

No remote fetch.

---

# 261. Schema-Keyword Tests

Test every supported keyword and rejected unsupported vocabulary.

---

# 262. Regex-Schema Tests

Test bounded matching and malicious catastrophic pattern.

---

# 263. Format Tests

Test each asserted and annotated format.

---

# 264. Schema-Error-Location Tests

Verify JSON Pointer and safe source location.

---

# 265. Semantic-Validator Tests

Test:

* deterministic;
* timeout;
* service reference;
* changed owner service;
* missing dependency;
* and exception.

---

# 266. Cross-Setting Tests

Use all declared examples and contradictory pairs.

---

# 267. Project-Document-Envelope Tests

Test:

* schema;
* document ID;
* scope;
* settings or policies;
* extra critical property;
* and version.

---

# 268. Project-Path Tests

Attempt `.opure` as:

* real directory;
* reparse point;
* junction;
* symlink;
* file;
* alternate stream;
* and path outside root.

---

# 269. Workspace-Snapshot Tests

Test exact project, file identity, hash and generation.

---

# 270. File-Watcher Tests

Generate:

* duplicate;
* coalesced;
* missed;
* rename;
* replace;
* delete;
* create;
* rapid writes;
* and branch checkout.

Final state must come from full reconciliation.

---

# 271. Partial-Write Tests

Write document in chunks and verify no partial apply.

---

# 272. Invalid-External-Edit Tests

Verify last valid snapshot remains and error visible.

---

# 273. File-Write-Atomicity Tests

Crash:

* before staging;
* during staging;
* before flush;
* after flush;
* before replace;
* after replace;
* and before source import.

Reconcile by hash.

---

# 274. Registry-Path Tests

Test exact HKLM and HKCU policy roots.

---

# 275. Registry-View Tests

Test 64-bit, 32-bit, conflicting views and migration.

---

# 276. Registry-Type Tests

Test:

* DWORD;
* SZ;
* MULTI_SZ;
* BINARY denied;
* missing;
* wrong type;
* and oversized string.

---

# 277. Registry-Policy-Value Tests

Test every initial enterprise policy mapping.

---

# 278. Registry-JSON Tests

Test strict schema and size if complex value is used.

---

# 279. Registry-Notification Tests

Test change, rapid change, lost notification, service restart and periodic reconciliation.

---

# 280. Policy-Refresh Tests

Test every trigger.

---

# 281. Enterprise-Policy-Unavailable Tests

Test permission denied, registry error and corrupt value.

---

# 282. ADMX-Schema Tests

Validate ADMX and ADML against Microsoft schema where tools permit.

---

# 283. ADMX-Mapping Tests

For every policy verify:

* category;
* name;
* hive;
* key;
* value;
* type;
* Not Configured;
* Enabled;
* Disabled;
* and UI text.

---

# 284. ADMX-Version Tests

Test old and new templates against runtime policy catalogue.

---

# 285. Central-Store Tests

Document installation and version mismatch behaviour.

---

# 286. RSoP-Provenance Tests

Verify Opure does not invent winning GPO identity.

---

# 287. Environment-Variable Tests

Test:

* known;
* unknown;
* case;
* prefix;
* empty;
* invalid type;
* secret;
* Stable development endpoint;
* and policy denial.

---

# 288. Command-Line Tests

Test:

* typed command;
* Profile selection;
* operation override;
* invalid setting;
* secret argument;
* policy denial;
* repeated option;
* quoting;
* and response file if supported.

---

# 289. Generic-Precedence-Denial Tests

Attempt to use raw .NET environment or command-line provider to override governed values.

---

# 290. Effective-Calculation Tests

For every merge and policy combination, verify exact result and provenance.

---

# 291. Provider-Policy Tests

Test Local Only, Ask Every Time, Approved Providers and Custom.

---

# 292. Capability-Policy Tests

Test tools, plugins, MCP, updates, export and workspace writes.

---

# 293. Effective-Snapshot Tests

Test:

* schema;
* sources;
* policy sources;
* values;
* constraints;
* provenance;
* hash;
* immutability;
* and secret absence.

---

# 294. Snapshot-Section Tests

Verify owner services receive only relevant settings.

---

# 295. Snapshot-Pinning Tests

Start:

* AI operation;
* workflow;
* index build;
* model load;
* and plugin activity

then change configuration.

Existing operation keeps bound revision.

---

# 296. Per-Key-Provenance Tests

Test Replace, merge, map key, set element, forced value, rejected value and policy conflict.

---

# 297. Resultant-Configuration Tests

Verify requested versus effective, policy reason and source order.

---

# 298. Change-Transaction-Schema Tests

Test every field and state.

---

# 299. Change-Validation Tests

Test actor, scope, base revision, policy, semantic validation and impact.

---

# 300. Revision-Conflict Tests

Two editors change same and different keys.

---

# 301. Change-Preview Tests

Verify all affected services and impacts.

---

# 302. Approval-Binding Tests

Change preview after approval.

Commit denied.

---

# 303. Atomic-Commit Tests

Crash:

* before transaction;
* after Profile revision;
* after Snapshot;
* after history;
* after outbox;
* during commit;
* and after commit.

All-or-nothing.

---

# 304. Project-File-Edit-Flow Tests

Test Workspace write and Configuration import reconciliation.

---

# 305. Activation-Plan Tests

Test dependencies, hash, rollback and deadline.

---

# 306. Runtime-Application-Class Tests

Test every class.

---

# 307. Impact-Aggregation Tests

Combine Immediate, Service Restart, Reindex and Application Restart.

---

# 308. Activation-Receipt Tests

Test success, failure, delayed, wrong section, stale snapshot and duplicate.

---

# 309. Partial-Activation Tests

Verify mixed state is clear and critical inconsistency disables capability.

---

# 310. Immutable-Options Tests

Verify no in-place mutation.

---

# 311. .NET-Adapter Tests

Test typed binding, generated validation, monitor subscription and authority isolation.

---

# 312. Startup-Validation Tests

Test valid, optional invalid and critical invalid.

---

# 313. Last-Known-Good Tests

Test:

* invalid new preference;
* invalid project file;
* policy changed to prohibit old value;
* missing source;
* source inaccessible;
* and service activation failure.

---

# 314. Safe-Default Tests

Verify only definition-approved safe defaults.

---

# 315. Policy-Uncertainty Tests

Test last observed policy and fail-closed capability.

---

# 316. Branch-Checkout Tests

Change settings and policy across branches.

---

# 317. Running-Workflow Tests

Verify pinning and critical live revocation behaviour.

---

# 318. Rollback Tests

Test:

* recent revision;
* old revision;
* current policy denial;
* impact change;
* approval;
* and history.

---

# 319. History Tests

Test every event type and append-only behaviour.

---

# 320. History-Hash Tests

Modify, delete, insert and reorder.

---

# 321. Migration-Schema Tests

Test every migration operation.

---

# 322. Migration-Purity Tests

Attempt file, network, clock and random use.

---

# 323. Migration-Idempotency Tests

Apply twice.

---

# 324. Migration-History Tests

Verify old revisions remain.

---

# 325. Future-Schema Tests

Reject with actionable version message.

---

# 326. Deprecation Tests

Test warning, automatic migration, removal and replacement conflict.

---

# 327. Unknown-Extension Tests

Test preserve, inactive, plugin installed and plugin removed.

---

# 328. Plugin-Definition Tests

Test namespace, package pin, scope, policy and uninstall.

---

# 329. Plugin-Escalation Tests

Attempt to register:

* Product Invariant;
* Enterprise Policy;
* remote-provider grant;
* command grant;
* and secret setting.

---

# 330. MCP-Configuration Tests

Test profile references, Vault references, project file trust and server change.

---

# 331. Provider-Configuration Tests

Test exact references and credential separation.

---

# 332. Local-Model-Configuration Tests

Test exact profile and file ownership.

---

# 333. Workflow-Configuration Tests

Test new instance and running instance.

---

# 334. Routing-Configuration Tests

Test next-operation activation and stale Routing Decision invalidation.

---

# 335. Context-and-Knowledge Tests

Test reindex and Context Plan invalidation.

---

# 336. Memory-Configuration Tests

Test retention, capture and remote embedding policy.

---

# 337. Logging-Configuration Tests

Attempt to disable redaction through every source.

---

# 338. Update-Configuration Tests

Verify ADR-0015 constraints.

---

# 339. Desktop-Configuration Tests

Test hot apply and policy lock.

---

# 340. Import-Archive Tests

Test:

* valid;
* path traversal;
* absolute path;
* duplicate archive path;
* huge file;
* compression bomb;
* malformed JSON;
* secret;
* unsupported schema;
* and executable content.

---

# 341. Import-Authority Tests

Attempt to import Enterprise, Product and capability authority.

---

# 342. Import-Reference Tests

Test missing Provider, Model, Plugin, MCP and Vault references.

---

# 343. Export-Preview Tests

Verify personal data, machine policy, references and exclusions.

---

# 344. Profile-Sharing Tests

Test export and import between channels and projects.

---

# 345. Deletion Tests

Test inactive, active, referenced, project local, session and imported Profiles.

---

# 346. Purge Tests

Verify controlled payload removal and safe tombstone.

---

# 347. Retention Tests

Test every default and user-shortened permitted policy.

---

# 348. Backup-and-Restore Tests

Test same machine, another machine, changed enterprise policy, missing service references and database corruption.

---

# 349. Startup-Recovery Tests

Test:

* clean;
* invalid latest source;
* schema migration;
* definition mismatch;
* enterprise conflict;
* missing project;
* activation mismatch;
* and repair mode.

---

# 350. Integrity Tests

Test every listed integrity check.

---

# 351. Trust Centre Tests

Verify all views and per-key explanation.

---

# 352. Diagnostics-Leakage Tests

Seed canaries in every sensitive source.

---

# 353. Metrics Tests

Verify low cardinality.

---

# 354. Fuzzing

Fuzz:

* strict JSON parser;
* duplicate detector;
* JSON Schema validator;
* Setting Definition;
* Policy Definition;
* Profile;
* project settings;
* project policy;
* registry JSON;
* merge reducer;
* policy reducer;
* semantic validator;
* Effective Snapshot;
* change transaction;
* migration;
* import bundle;
* and export.

---

# 355. Policy-Bypass Adversarial Suite

Attempt to enable prohibited capability through every source.

---

# 356. Duplicate-Key Adversarial Suite

Use escaped and deeply nested duplicates.

---

# 357. Partial-Write Adversarial Suite

Continuously rewrite project documents while operations start.

No partial snapshot.

---

# 358. Stale-Policy Adversarial Suite

Change registry policy during:

* approval;
* Routing Decision;
* Data Sharing Plan;
* workflow start;
* plugin call;
* and MCP call.

---

# 359. Configuration-Split-Brain Suite

Cause one service to reject activation while others accept.

Verify affected capability safety.

---

# 360. Rollback Adversarial Suite

Attempt rollback to:

* prohibited provider;
* removed plugin;
* old insecure default;
* deprecated policy;
* and another project Profile.

---

# 361. Profile-Cycle Adversarial Suite

Create cycles through import and concurrent edits.

---

# 362. Source-Substitution Adversarial Suite

Replace:

* project file;
* `.opure` directory;
* registry type;
* schema file;
* definition package;
* and CAS payload.

---

# 363. Command-Line-Secret Adversarial Suite

Attempt secrets in every argument form.

---

# 364. Environment-Override Adversarial Suite

Attempt arbitrary `OPURE_` and .NET-style environment keys.

---

# 365. Plugin-and-MCP Escalation Suite

Attempt setting and policy registration outside namespace and capability.

---

# 366. Performance Tests

Measure:

* parsing;
* schema;
* merge;
* policy;
* semantic validation;
* snapshot;
* commit;
* activation;
* provenance;
* registry reconciliation;
* project reconciliation;
* migration;
* import;
* export;
* and Trust Centre query.

---

# 367. Endurance Tests

Run for seven days with:

* rapid Profile changes;
* project branch changes;
* registry policy changes;
* Runtime restarts;
* Desktop restarts;
* model reloads;
* reindex;
* and workflow starts.

---

# 368. Accessibility Tests

Configuration UI must support:

* keyboard;
* Narrator;
* high contrast;
* reduced motion;
* policy lock explanation;
* per-key provenance;
* merge display;
* restart impact;
* conflict comparison;
* import preview;
* and rollback.

---

# 369. Prototype Plan

## 369.1 Prototype A — Typed Setting Catalogue

Register 100 representative settings.

---

## 369.2 Prototype B — Strict Project JSON

Reject duplicate keys and partial writes on .NET 10.

---

## 369.3 Prototype C — Profiles

User Base, Named and Project Local with one-parent inheritance.

---

## 369.4 Prototype D — Policy Intersection

Product, machine, user and project constraints.

---

## 369.5 Prototype E — Registry Policy and ADMX

Manage provider, plugin and update restrictions.

---

## 369.6 Prototype F — Effective Snapshot

Produce per-key provenance and hash.

---

## 369.7 Prototype G — Atomic Change

Commit Profile, snapshot, history and outbox.

---

## 369.8 Prototype H — Activation

Hot apply one setting and require Runtime restart for another.

---

## 369.9 Prototype I — Last Known Good

Break a project file and retain safe active state.

---

## 369.10 Prototype J — Branch Change

Switch configuration and show reindex and workflow impact.

---

## 369.11 Prototype K — Migration

Rename, split and convert settings without rewriting history.

---

## 369.12 Prototype L — Policy Bypass

Attempt environment, command-line, plugin, MCP and import escalation.

---

# 370. Implementation Plan

1. Record founder review.
2. Define Setting Definition schema.
3. Define Policy Definition schema.
4. Define scope taxonomy.
5. Define source taxonomy.
6. Define merge strategies.
7. Define null semantics.
8. Define constraint types.
9. Define policy combination rules.
10. Define Effective Snapshot schema.
11. Define provenance schema.
12. Create Configuration SQLite schema.
13. Implement Definition Catalogue.
14. Implement strict JSON token parser.
15. Implement duplicate-property detection.
16. Select JSON Schema 2020-12 validator or implement bounded subset.
17. Package local schemas.
18. Implement source registry.
19. Implement Profile revisions.
20. Implement one-parent inheritance.
21. Implement setting merge.
22. Implement policy intersection.
23. Implement policy conflict.
24. Implement semantic validators.
25. Implement source-controlled project settings.
26. Implement project policy.
27. Implement project local Profiles.
28. Implement Workspace reconciliation.
29. Implement atomic project-file writes.
30. Implement HKLM policy reader.
31. Implement HKCU policy reader.
32. Implement registry notification and reconciliation.
33. Create ADMX and ADML prototype.
34. Implement approved environment catalogue.
35. Implement typed command-line overrides.
36. Implement Effective Snapshot builder.
37. Implement per-key provenance.
38. Implement Resultant Configuration.
39. Define Change Transaction schema.
40. Implement preview and optimistic concurrency.
41. Implement atomic commit.
42. Define activation classes.
43. Implement service activation protocol.
44. Integrate .NET typed options adapters.
45. Implement last-known-good state.
46. Implement critical fail-closed behaviour.
47. Implement branch-change impact.
48. Implement rollback.
49. Define migration schema.
50. Implement deterministic migrations.
51. Implement deprecation.
52. Implement plugin configuration namespace.
53. Integrate MCP, Provider and Local Model references.
54. Integrate Workflow, Context, Knowledge and Memory snapshots.
55. Implement import and export.
56. Implement retention, deletion and purge.
57. Implement backup and restore.
58. Implement Trust Centre and Desktop UI.
59. Add policy-bypass adversarial suite.
60. Add parser and source-substitution fuzzing.
61. Run performance and endurance tests.
62. Complete enterprise, security and privacy review.
63. Accept, amend or reject the ADR.

---

# 371. Owners

| Area                       | Owner                                          |
| -------------------------- | ---------------------------------------------- |
| Product policy             | Founder                                        |
| Configuration Service      | Configuration Architecture Owner               |
| Setting Definitions        | Owning Service and Configuration Owners        |
| Policy Definitions         | Security, Product and Configuration Owners     |
| JSON and schemas           | Configuration Architecture Owner               |
| Profiles                   | Configuration and Desktop Owners               |
| Project settings           | Project Service and Workspace Owners           |
| Project policy             | Product Governance and Project Owners          |
| Enterprise registry policy | Enterprise Management Owner                    |
| ADMX and ADML              | Enterprise Management and Release Owners       |
| Persistence                | Persistence Owner                              |
| Secrets                    | Secrets Owner                                  |
| Provider references        | Provider Trust Owner                           |
| Local model references     | Local Model Runtime Owner                      |
| Routing and Context        | AI Router and Context Assembly Owners          |
| Knowledge and Memory       | Project Knowledge and Memory Owners            |
| Workflow integration       | Workflow Owner                                 |
| Tool configuration         | Tool Mediation Owner                           |
| Plugin configuration       | Plugin Platform Owner                          |
| MCP configuration          | MCP Gateway Owner                              |
| Activation                 | Runtime Architecture and Owning Service Owners |
| Trust Centre               | Trust Centre Owner                             |
| Desktop UI                 | Desktop Owner                                  |
| Migration and recovery     | Recovery Owner                                 |
| Adversarial tests          | Test Architecture Owner                        |

---

# 372. Suggested Repository Structure

```text
src/
├── Configuration/
│   ├── Opure.Configuration.Contracts/
│   ├── Opure.Configuration.Service/
│   ├── Opure.Configuration.Definitions/
│   ├── Opure.Configuration.Policies/
│   ├── Opure.Configuration.Profiles/
│   ├── Opure.Configuration.Json/
│   ├── Opure.Configuration.Schema/
│   ├── Opure.Configuration.Merge/
│   ├── Opure.Configuration.Validation/
│   ├── Opure.Configuration.Snapshots/
│   ├── Opure.Configuration.Activation/
│   ├── Opure.Configuration.Registry/
│   ├── Opure.Configuration.Project/
│   ├── Opure.Configuration.Migrations/
│   ├── Opure.Configuration.ImportExport/
│   ├── Opure.Configuration.Persistence/
│   └── Opure.Configuration.Security/
├── Desktop/
│   └── Opure.Desktop.Configuration/
└── Enterprise/
    └── Opure.Enterprise.PolicyTemplates/

schemas/
└── configuration/
    ├── setting-definition-v1.schema.json
    ├── policy-definition-v1.schema.json
    ├── configuration-profile-v1.schema.json
    ├── project-settings-v1.schema.json
    ├── project-policy-v1.schema.json
    ├── effective-configuration-snapshot-v1.schema.json
    ├── configuration-change-transaction-v1.schema.json
    ├── configuration-activation-plan-v1.schema.json
    ├── configuration-migration-v1.schema.json
    └── configuration-bundle-v1.schema.json

enterprise/
├── Opure.admx
└── en-US/
    └── Opure.adml

tests/
└── Configuration/
    ├── Opure.Configuration.UnitTests/
    ├── Opure.Configuration.JsonTests/
    ├── Opure.Configuration.PolicyTests/
    ├── Opure.Configuration.RegistryTests/
    ├── Opure.Configuration.SecurityTests/
    ├── Opure.Configuration.IntegrationTests/
    ├── Opure.Configuration.PerformanceTests/
    └── Fixtures/
        ├── Profiles/
        ├── ProjectDocuments/
        ├── Registry/
        ├── Migrations/
        └── Malicious/
```

Exact project count may be consolidated under ADR-0010.

---

# 373. Setting Definition Sketch

```json
{
  "schema": "opure.setting-definition/1",
  "setting_id": "runtime.performance.default-mode",
  "revision": 1,
  "owner_service": "local-model-runtime",
  "value_type": {
    "kind": "enum",
    "values": [
      "eco",
      "balanced",
      "performance",
      "turbo"
    ]
  },
  "default_value": "balanced",
  "allowed_scopes": [
    "user",
    "project",
    "workspace-session",
    "operation"
  ],
  "allowed_sources": [
    "product-default",
    "user-base-profile",
    "named-user-profile",
    "project-shared-settings",
    "project-local-profile",
    "session-override",
    "operation-override"
  ],
  "merge_strategy": "replace",
  "runtime_application": "next-operation",
  "failure_class": "operational",
  "sha256": "..."
}
```

---

# 374. Policy Definition Sketch

```json
{
  "schema": "opure.policy-definition/1",
  "policy_id": "policy.cloud.allowed-providers",
  "revision": 1,
  "owner_service": "provider-trust",
  "constraint_type": "allowed-set",
  "target_settings": [
    "ai.routing.allowed-provider-profiles"
  ],
  "allowed_sources": [
    "product-invariant",
    "enterprise-machine-policy",
    "enterprise-user-policy",
    "project-governance-policy",
    "workflow-policy",
    "operation-capability"
  ],
  "combination_rule": "set-intersection",
  "failure_class": "data-governance-critical",
  "sha256": "..."
}
```

---

# 375. Profile Sketch

```json
{
  "schema": "opure.configuration-profile/1",
  "profile_id": "profile-opaque",
  "revision": 3,
  "profile_kind": "named-user",
  "display_name": "Maximum Privacy",
  "scope": {
    "kind": "user",
    "user": "user-opaque"
  },
  "parent_profile_revision": "profile-base:7",
  "values": {
    "ai.routing.local-preference": "hard-local",
    "memory.remote-embedding.enabled": false,
    "updates.check-on-launch.enabled": false
  },
  "created_at": "2026-07-18T18:00:00Z",
  "sha256": "..."
}
```

---

# 376. Effective Snapshot Sketch

```json
{
  "schema": "opure.effective-configuration-snapshot/1",
  "snapshot_id": "snapshot-opaque",
  "scope": {
    "channel": "stable",
    "project": "project-opaque",
    "user": "user-opaque"
  },
  "catalogues": {
    "settings": "setting-catalogue:12",
    "policies": "policy-catalogue:8",
    "schemas": "schema-catalogue:5"
  },
  "profiles": [
    "profile-base:7",
    "profile-maximum-privacy:3",
    "project-local-profile:4"
  ],
  "sources": [
    "project-settings-source:9",
    "enterprise-machine-policy:14"
  ],
  "effective_values": {
    "project.cloud.policy": "local-only",
    "runtime.performance.default-mode": "balanced"
  },
  "effective_constraints": {
    "policy.cloud.require-local": true
  },
  "created_at": "2026-07-18T18:00:00Z",
  "sha256": "..."
}
```

---

# 377. Change Transaction Sketch

```json
{
  "schema": "opure.configuration-change-transaction/1",
  "transaction_id": "change-opaque",
  "target_scope": {
    "kind": "project",
    "project": "project-opaque"
  },
  "target_profile": "project-local-profile",
  "base_revision": 4,
  "requested_changes": [
    {
      "setting_id": "runtime.performance.default-mode",
      "operation": "set",
      "value": "performance"
    }
  ],
  "candidate_snapshot": "snapshot-candidate-opaque",
  "impact_plan": "activation-plan-opaque",
  "preview_sha256": "...",
  "state": "ready"
}
```

---

# 378. Provenance Sketch

```json
{
  "setting_id": "runtime.performance.default-mode",
  "effective_value": "balanced",
  "requested_values": [
    {
      "source": "named-profile:3",
      "value": "performance",
      "result": "contributed-winning-request"
    }
  ],
  "policies": [
    {
      "source": "enterprise-machine-policy:14",
      "constraint": {
        "allowed_values": [
          "eco",
          "balanced"
        ]
      }
    }
  ],
  "effective_reason": "Requested Performance is prohibited by Enterprise Machine Policy; Balanced is the selected permitted value according to the setting fallback rule."
}
```

---

# 379. Registry Policy Sketch

Conceptual:

```text
HKEY_LOCAL_MACHINE\Software\Policies\Opure\Cloud
    RequireLocal          REG_DWORD  1
    AllowedProviders      REG_MULTI_SZ
```

The exact registry catalogue must be generated from accepted Policy Definitions.

---

# 380. Migration Sketch

```json
{
  "schema": "opure.configuration-migration/1",
  "migration_id": "routing-local-preference-v1-to-v2",
  "source_schema": "opure.project-settings/1",
  "target_schema": "opure.project-settings/2",
  "operations": [
    {
      "kind": "rename",
      "from": "ai.prefer-local",
      "to": "ai.routing.local-preference"
    },
    {
      "kind": "map-enum",
      "setting": "ai.routing.local-preference",
      "mapping": {
        "true": "prefer-local",
        "false": "neutral"
      }
    }
  ],
  "sha256": "..."
}
```

---

# 381. Release Gate

Configuration and policy management are blocked when:

* settings and policies are represented as one undifferentiated precedence stack;
* a later setting source can weaken Product or Enterprise Policy;
* Product Invariant Policy is loaded from mutable user data;
* project files can grant capabilities;
* imported Profiles can become enterprise authority;
* plugins or MCP servers can register Product or Enterprise Policy;
* environment variables can override arbitrary production settings;
* command-line values can disable policy or review;
* command-line secrets are accepted;
* registry values are coerced from the wrong type;
* HKCU preferences are treated as HKLM policy;
* policy conflicts are resolved through silent last-writer-wins;
* an empty permitted space is hidden;
* setting merge semantics are selected by the source document;
* Profile inheritance can cycle or become unbounded;
* multiple Profile inheritance is enabled without a separate accepted decision;
* JSON duplicate properties are accepted;
* remote JSON Schema references can be fetched;
* comments or trailing commas produce parser-dependent meaning;
* a file watcher event applies a partial document;
* an invalid project edit replaces the last valid snapshot;
* Effective Configuration is mutable;
* per-key provenance is unavailable;
* a running operation can observe mixed revisions;
* a Configuration Change Transaction can partially commit;
* an approval can survive a changed preview;
* a service can claim activation without binding the exact snapshot;
* a critical policy change leaves a prohibited capability running;
* rollback can restore a value prohibited by current policy;
* migrations rewrite historical Profile revisions;
* secret values can enter configuration, history, export or diagnostics;
* or Trust Centre cannot explain the effective value, every policy and the activation state.

---

# 382. Acceptance Criteria

This ADR may move to **Accepted** when all of the following hold.

## Architecture and Ownership

* [ ] Configuration and Policy Service is authoritative.
* [ ] Setting Definitions are separate from Policy Definitions.
* [ ] Settings express desired values.
* [ ] Policies express constraints.
* [ ] Policies cannot be weakened by later setting sources.
* [ ] Product Invariant Policy is packaged and immutable.
* [ ] Enterprise Machine Policy is distinct.
* [ ] Enterprise User Policy is distinct.
* [ ] Project Governance Policy is distinct.
* [ ] Workflow and Operation constraints are distinct.
* [ ] Consumer services do not read Configuration database directly.
* [ ] Consumer services do not read raw project JSON directly.
* [ ] Consumer services do not read raw enterprise policy directly.
* [ ] Models cannot commit configuration.
* [ ] Plugins cannot broaden policy.
* [ ] MCP servers cannot broaden policy.
* [ ] Provider-side configuration is not authoritative.
* [ ] Stable, Preview and Development are isolated.
* [ ] Local offline configuration works.

## Persistence

* [ ] One service-owned SQLite database exists per channel.
* [ ] Database is outside the repository.
* [ ] Database is on local fixed storage.
* [ ] Network storage is denied.
* [ ] Foreign keys are enabled.
* [ ] Schema migrations are explicit.
* [ ] Profile revisions are immutable.
* [ ] Source revisions are immutable.
* [ ] Effective Snapshots are immutable.
* [ ] Change history is append only.
* [ ] Transactional outbox exists.
* [ ] Backup works.
* [ ] Restore works.
* [ ] Database corruption has a recovery path.
* [ ] Secret values are absent from the database.

## Setting Definitions

* [ ] Setting Definition schema exists.
* [ ] Every setting has a stable ID.
* [ ] Every setting has an owner service.
* [ ] Every setting has a type.
* [ ] Every setting has a valid default or required-source rule.
* [ ] Every setting declares allowed scopes.
* [ ] Every setting declares allowed sources.
* [ ] Every setting declares merge semantics.
* [ ] Every setting declares null semantics.
* [ ] Every setting declares validation.
* [ ] Every setting declares sensitivity.
* [ ] Every setting declares secret policy.
* [ ] Every setting declares runtime application.
* [ ] Every setting declares failure class.
* [ ] Every setting declares deprecation metadata where applicable.
* [ ] Setting Definition revisions are immutable.
* [ ] Default values satisfy Product Invariant Policy.
* [ ] Dynamic user-defined core setting IDs are denied.
* [ ] Renames use migration metadata.

## Value Types and Limits

* [ ] Boolean type works.
* [ ] Integer type works.
* [ ] Decimal type works.
* [ ] String type works.
* [ ] Duration type works.
* [ ] Byte-size type works.
* [ ] UTC-instant type works.
* [ ] Enumeration type works.
* [ ] URI type works.
* [ ] Logical-path reference works.
* [ ] Opaque service reference works.
* [ ] Vault reference works.
* [ ] Ordered list works.
* [ ] Set works.
* [ ] Map works.
* [ ] Typed object works.
* [ ] Discriminated union works.
* [ ] Rule list works.
* [ ] Document size is bounded.
* [ ] String size is bounded.
* [ ] Collection sizes are bounded.
* [ ] Object depth is bounded.
* [ ] Numeric overflow is rejected.
* [ ] Non-finite numbers are rejected.
* [ ] Executable values are rejected.

## Scopes and Sources

* [ ] Product scope works.
* [ ] Channel scope works.
* [ ] Machine scope works.
* [ ] User scope works.
* [ ] Project scope works.
* [ ] Workspace Session scope works.
* [ ] Workflow scope works.
* [ ] Operation scope works.
* [ ] Plugin scope works.
* [ ] MCP Server scope works.
* [ ] Provider scope works.
* [ ] Local Model scope works.
* [ ] Tool scope works.
* [ ] Test scope works.
* [ ] Disallowed source-setting combinations fail visibly.
* [ ] Cross-project use is denied.
* [ ] Cross-channel use is denied.
* [ ] Project files cannot set Machine-only values.
* [ ] Plugins cannot set settings outside their namespace and capability.
* [ ] Operation overrides bind one operation.

## Sensitivity and Secrets

* [ ] Sensitivity classes are implemented.
* [ ] Effective sensitivity inherits restrictive sources.
* [ ] Sensitive UI values are masked.
* [ ] No Secret policy works.
* [ ] Vault Reference Allowed works.
* [ ] Vault Reference Required works.
* [ ] Secret-Derived Boolean works where approved.
* [ ] Prohibited Secret Value works.
* [ ] Plain API keys are rejected.
* [ ] Tokens are rejected.
* [ ] Passwords are rejected.
* [ ] Private keys are rejected.
* [ ] Connection strings containing credentials are rejected.
* [ ] Vault values are never exported.
* [ ] Vault references are handled safely.
* [ ] Secret canaries are absent from diagnostics.

## Policy Definitions

* [ ] Policy Definition schema exists.
* [ ] Every policy has a stable ID.
* [ ] Every policy has an owner.
* [ ] Every policy has a constraint type.
* [ ] Every policy declares targets.
* [ ] Every policy declares allowed sources.
* [ ] Every policy declares scope rules.
* [ ] Every policy declares a combination rule.
* [ ] Every policy declares a conflict rule.
* [ ] Every policy declares a failure class.
* [ ] Policy Definition revisions are immutable.
* [ ] A setting source cannot create a Policy Definition.
* [ ] A project source cannot create Product Invariant authority.
* [ ] A Profile cannot create Enterprise authority.

## Constraint Types

* [ ] Force Value works.
* [ ] Allow Values works.
* [ ] Deny Values works.
* [ ] Minimum works.
* [ ] Maximum works.
* [ ] Require True works.
* [ ] Require False works.
* [ ] Require Capability works.
* [ ] Deny Capability works.
* [ ] Require Review Mode works.
* [ ] Maximum Data Class works.
* [ ] Allowed Provider Profiles works.
* [ ] Allowed Regions works.
* [ ] Allowed Paths works.
* [ ] Denied Paths works.
* [ ] Maximum Cost works.
* [ ] Maximum Retention works.
* [ ] Minimum Retention works where justified.
* [ ] Require Local works.
* [ ] Require Offline works.
* [ ] Lock Setting works.
* [ ] Custom Trusted Constraint is bounded and first party.

## Policy Combination

* [ ] Allowed sets intersect.
* [ ] Denied sets unite.
* [ ] Required controls unite.
* [ ] Maximum limits select the lowest maximum.
* [ ] Minimum requirements select the highest minimum.
* [ ] Required-true controls combine safely.
* [ ] Denials win for capabilities.
* [ ] Force values require agreement or explicit precedence.
* [ ] Path permissions intersect.
* [ ] Lower policy may restrict further.
* [ ] Lower policy cannot broaden higher policy.
* [ ] Empty permitted space is detected.
* [ ] Invalid numeric range is detected.
* [ ] Conflicting force values are detected.
* [ ] Policy conflict is first class.
* [ ] No silent winner is selected.
* [ ] Critical conflict disables affected capability.
* [ ] Conflict evidence is visible.

## Product and Channel Policy

* [ ] Product Invariant Policy is signed-package bound.
* [ ] Product Invariant cannot be modified by app data.
* [ ] Product Invariant prohibits ordinary secret values.
* [ ] Product Invariant enforces cloud-policy boundaries.
* [ ] Product Invariant prevents project capability grants.
* [ ] Product Invariant prevents arbitrary policy code.
* [ ] Release-Channel Policy works.
* [ ] Stable preview-feature restrictions work.
* [ ] Stable development-plugin restrictions work.
* [ ] Cross-channel imports revalidate.
* [ ] Channel defaults cannot weaken Product Invariant.

## Enterprise Policy

* [ ] HKLM policy root is supported.
* [ ] HKCU policy root is supported.
* [ ] HKLM and HKCU are distinct sources.
* [ ] Machine policy cannot be modified by ordinary user changes.
* [ ] User policy cannot broaden Machine policy.
* [ ] Registry values are schema defined.
* [ ] Registry value types are validated exactly.
* [ ] Native x64 registry view is used.
* [ ] Conflicting 32-bit values do not merge silently.
* [ ] Unknown critical registry values are visible.
* [ ] Registry policy contains no secrets.
* [ ] Registry change notification works.
* [ ] Periodic registry reconciliation works.
* [ ] Runtime-start policy refresh works.
* [ ] Session-unlock refresh works where supported.
* [ ] Explicit Refresh Policy works.
* [ ] Policy removal creates a revision.
* [ ] Policy uncertainty fails closed where required.
* [ ] ADMX prototype exists.
* [ ] ADML prototype exists.
* [ ] ADMX mappings match Policy Definitions.
* [ ] Not Configured semantics are correct.
* [ ] Enabled semantics are correct.
* [ ] Disabled semantics are definition specific.
* [ ] Template version mismatch is documented.
* [ ] Opure does not invent the winning GPO.
* [ ] Windows RSoP guidance is available.

## Project Settings and Policy

* [ ] `.opure\project.settings.json` is supported.
* [ ] `.opure\project.policy.json` is supported.
* [ ] Both are optional by default.
* [ ] Both are acquired through Workspace.
* [ ] Project identity is checked.
* [ ] File identity is checked.
* [ ] Reparse points are checked.
* [ ] Alternate data streams are denied.
* [ ] File size is bounded.
* [ ] Source hash is recorded.
* [ ] Workspace generation is recorded.
* [ ] Repository evidence is recorded where available.
* [ ] Project settings can express only allowed setting scopes.
* [ ] Project policy can make rules stricter.
* [ ] Project policy cannot grant capability.
* [ ] Project files cannot contain secrets.
* [ ] Branch changes create new source revisions.
* [ ] Branch-change impact is calculated.
* [ ] Project clone does not silently inherit local Profile authority.
* [ ] Project deletion handles local configuration.

## Strict JSON

* [ ] Documents require UTF-8.
* [ ] Invalid UTF-8 is rejected.
* [ ] One top-level object is required.
* [ ] Comments are rejected.
* [ ] Trailing commas are rejected.
* [ ] Trailing content is rejected.
* [ ] Duplicate property names are rejected at every depth.
* [ ] Escaped-equivalent duplicate names are rejected.
* [ ] Excessive depth is rejected.
* [ ] Excessive object property count is rejected.
* [ ] Excessive document size is rejected.
* [ ] Non-finite numbers are rejected.
* [ ] Invalid escapes are rejected.
* [ ] Parser differential tests pass.
* [ ] .NET 10 duplicate detection has an explicit implementation.
* [ ] Parser behaviour does not depend on .NET 11 preview APIs.
* [ ] Canonical writer produces stable output.

## JSON Schema

* [ ] JSON Schema Draft 2020-12 subset is documented.
* [ ] Metaschemas are packaged locally.
* [ ] Supported vocabularies are pinned.
* [ ] Supported keywords are pinned.
* [ ] Remote `$ref` is disabled.
* [ ] File `$ref` is disabled unless registry controlled.
* [ ] Unknown required vocabulary fails.
* [ ] Format annotation and assertion behaviour is explicit.
* [ ] Regex evaluation is bounded.
* [ ] Schema size is bounded.
* [ ] Schema depth is bounded.
* [ ] Error locations are usable.
* [ ] Unknown future schema fails with an upgrade message.
* [ ] Schema hash is included in source validation.

## Profiles

* [ ] User Base Profile exists.
* [ ] Named User Profiles exist.
* [ ] Project Local Profiles exist.
* [ ] Session Profiles exist.
* [ ] Test Profiles exist.
* [ ] Imported Candidate Profiles exist.
* [ ] Profile schema is versioned.
* [ ] Profile IDs are stable.
* [ ] Profile revisions are immutable.
* [ ] Profile scope is explicit.
* [ ] Parent Profile revision is exact.
* [ ] One-parent inheritance works.
* [ ] Inheritance cycles are rejected.
* [ ] Inheritance depth is bounded at eight initially.
* [ ] Multiple inheritance is rejected.
* [ ] Parent deletion is protected.
* [ ] Parent updates do not mutate child revisions.
* [ ] Profile selection is transactional.
* [ ] Profile names do not grant authority.
* [ ] Profile compatibility is evaluated per project.

## Setting Merge

* [ ] Replace works.
* [ ] Replace If Set works.
* [ ] First Explicit works only where declared.
* [ ] Append works.
* [ ] Prepend works.
* [ ] Ordered Unique Append works.
* [ ] Set Union works.
* [ ] Set Intersection works.
* [ ] Map Merge by Key works.
* [ ] Map Replace works.
* [ ] Rule List Concatenation works.
* [ ] Minimum works.
* [ ] Maximum works.
* [ ] Custom Trusted Reducer works.
* [ ] Source documents cannot choose merge strategy.
* [ ] Custom reducers are pure.
* [ ] Custom reducers are deterministic.
* [ ] Custom reducers are bounded.
* [ ] Plugins cannot register reducers initially.
* [ ] Models cannot implement reducers.
* [ ] Collection-element provenance is retained.
* [ ] Map-key provenance is retained.

## Null and Removal

* [ ] Not Specified is distinct.
* [ ] Explicit Null is distinct.
* [ ] Reset to Default is distinct.
* [ ] Remove Inherited Entry is distinct.
* [ ] Empty Value is distinct.
* [ ] Raw JSON null has definition-specific meaning.
* [ ] Reserved removal markers are schema controlled.
* [ ] Ambiguous removal fails.

## Environment and Command Line

* [ ] Only approved `OPURE_` variables are recognised.
* [ ] Unknown variables do not enter production configuration.
* [ ] Environment variables cannot override policy.
* [ ] Environment variables cannot contain secrets.
* [ ] Environment variables cannot disable signature checks.
* [ ] Environment variables cannot grant plugin capability.
* [ ] Environment variables cannot grant MCP capability.
* [ ] Stable rejects development endpoint bootstrap without explicit development policy.
* [ ] Command-line settings are typed.
* [ ] Command-line Profile selection is explicit.
* [ ] Command-line overrides are operation or process scoped.
* [ ] Command-line overrides are policy checked.
* [ ] Command-line overrides are visible.
* [ ] Command-line overrides are receipted.
* [ ] Command-line secrets are rejected.
* [ ] Raw .NET provider order cannot bypass governance.
* [ ] Command-line precedence is not universal.

## Source Reconciliation

* [ ] Watcher events are hints only.
* [ ] Duplicate watcher events are safe.
* [ ] Coalesced watcher events are safe.
* [ ] Missed watcher events are found by reconciliation.
* [ ] Rename and atomic replace are handled.
* [ ] Full bounded documents are reread.
* [ ] Source hashes are compared.
* [ ] Partial writes do not apply.
* [ ] Invalid edits retain the last valid snapshot.
* [ ] Deleted optional source removes its contribution.
* [ ] Deleted required source becomes invalid.
* [ ] Changed project identity does not transfer source authority.
* [ ] Periodic reconciliation is bounded.
* [ ] Project-focus reconciliation works.
* [ ] Critical operation freshness checks work.
* [ ] Opure-authored writes use staged atomic replacement.
* [ ] Write crash reconciliation uses exact hash.

## Effective Configuration

* [ ] Effective Snapshot schema exists.
* [ ] Snapshot binds scope.
* [ ] Snapshot binds Setting Definition catalogue.
* [ ] Snapshot binds Policy Definition catalogue.
* [ ] Snapshot binds schema catalogue.
* [ ] Snapshot binds Profile revisions.
* [ ] Snapshot binds Setting Source revisions.
* [ ] Snapshot binds Policy Source revisions.
* [ ] Snapshot contains typed effective values.
* [ ] Snapshot contains typed effective constraints.
* [ ] Snapshot contains per-key provenance.
* [ ] Snapshot contains validation results.
* [ ] Snapshot contains impact state.
* [ ] Snapshot hash is canonical.
* [ ] Snapshot is immutable.
* [ ] Snapshot contains no secret values.
* [ ] Service sub-snapshots are exact.
* [ ] Owner services receive only relevant sections.
* [ ] Operation Snapshot includes explicit overrides.
* [ ] Running operations pin a snapshot.
* [ ] Running workflows pin relevant revisions.
* [ ] Mid-operation mixed configuration is prevented.

## Provenance and Resultant Configuration

* [ ] Effective value is explainable.
* [ ] Default source is shown.
* [ ] Every contributing source is shown.
* [ ] Winning source is shown for Replace.
* [ ] Merge steps are shown.
* [ ] Every policy constraint is shown.
* [ ] Rejected values are shown safely.
* [ ] Validation is shown.
* [ ] Activation state is shown.
* [ ] Collection elements have provenance.
* [ ] Map keys have provenance.
* [ ] Requested versus Effective view exists.
* [ ] Policy conflicts are visible.
* [ ] Losing sources are visible.
* [ ] Resultant Configuration export is safe.
* [ ] Sensitive values are masked.
* [ ] Windows GPO provenance is not fabricated.

## Change Transactions

* [ ] Change Transaction schema exists.
* [ ] Transaction binds target scope.
* [ ] Transaction binds target Profile.
* [ ] Transaction binds exact base revision.
* [ ] Changes are typed.
* [ ] Actor is recorded.
* [ ] Reason is recorded.
* [ ] Catalogue revisions are bound.
* [ ] Policy snapshot is bound.
* [ ] Candidate Effective Snapshot is bound.
* [ ] Validation is bound.
* [ ] Impact Plan is bound.
* [ ] Preview hash is bound.
* [ ] Approval is bound where required.
* [ ] Idempotency key exists.
* [ ] Optimistic concurrency works.
* [ ] Conflicting editor changes are shown.
* [ ] Non-conflicting typed changes may rebase safely.
* [ ] Profile revision and Effective Snapshot commit atomically.
* [ ] Change history and outbox commit atomically.
* [ ] Partial transaction commit is impossible.
* [ ] Source-controlled file edits use Workspace.
* [ ] External file and internal snapshot are reconciled.

## Preview and Approval

* [ ] Requested changes are shown.
* [ ] Effective changes are shown.
* [ ] Policy-forced differences are shown.
* [ ] Dependent setting changes are shown.
* [ ] Affected services are shown.
* [ ] Restart impact is shown.
* [ ] Reindex impact is shown.
* [ ] Model reload impact is shown.
* [ ] Workflow impact is shown.
* [ ] Provider impact is shown.
* [ ] Retention impact is shown.
* [ ] Rollback availability is shown.
* [ ] Approval binds preview hash.
* [ ] Changed preview invalidates approval.
* [ ] Remote enablement can require approval.
* [ ] External MCP enablement can require approval.
* [ ] Plugin development mode can require approval.
* [ ] Update-channel change can require approval.
* [ ] Policy waiver is not a normal setting edit.

## Activation

* [ ] Activation Plan schema exists.
* [ ] Affected services are listed.
* [ ] Dependency order is explicit.
* [ ] Runtime Application Class is known per setting.
* [ ] Immediate Hot Apply works.
* [ ] Next Read works.
* [ ] Next Operation works.
* [ ] New Workflow Only works.
* [ ] Reopen Project works.
* [ ] Reindex Required works.
* [ ] Reload Local Model works.
* [ ] Restart Owning Service works.
* [ ] Restart Runtime works.
* [ ] Restart Desktop works.
* [ ] Restart Application works.
* [ ] Windows Restart works where explicitly needed.
* [ ] Migration Required works.
* [ ] Unsupported While Active works.
* [ ] Combined impact uses the strongest requirement.
* [ ] Owner-service acknowledgement binds exact snapshot and section.
* [ ] Activation receipts are persisted.
* [ ] Committed Pending Activation is visible.
* [ ] Committed Restart Required is visible.
* [ ] Committed Activation Failed is visible.
* [ ] Partial activation is visible.
* [ ] Critical split brain disables affected capability.
* [ ] Existing operations retain pinned revisions.
* [ ] Shared mutable options objects are absent.

## .NET Adapters

* [ ] `IConfiguration` is internal convenience only.
* [ ] Raw provider order is not cross-service authority.
* [ ] Typed binding consumes validated snapshot sections.
* [ ] Immutable typed options are produced.
* [ ] Startup validation works.
* [ ] Generated validation is evaluated for stable .NET compatibility.
* [ ] Generated binding is evaluated for stable .NET compatibility.
* [ ] Semantic validation remains available.
* [ ] `IOptionsMonitor<T>` exposes activated snapshots only.
* [ ] Critical service readiness depends on valid configuration.

## Last Known Good and Failure

* [ ] Last valid Source revision is tracked.
* [ ] Last valid Effective Snapshot is tracked.
* [ ] Last activated service snapshot is tracked.
* [ ] Invalid new preference does not corrupt active state.
* [ ] Invalid project file does not partially apply.
* [ ] Current policy is rechecked before retaining old state.
* [ ] Newly prohibited old value stops.
* [ ] Provider policy failure disables remote use.
* [ ] Plugin policy failure disables plugin use.
* [ ] Tool policy failure denies affected tools.
* [ ] Logging preference failure uses safe default.
* [ ] Security-critical failure fails closed.
* [ ] Whole application blocks only when no safe repair mode exists.
* [ ] Policy uncertainty is visible.
* [ ] Safe fallback is definition controlled.

## Rollback and History

* [ ] Rollback creates a new Profile revision.
* [ ] Rollback does not erase history.
* [ ] Rollback uses current definitions.
* [ ] Rollback uses current policy.
* [ ] Prohibited prior values do not reactivate.
* [ ] Rollback has a new impact plan.
* [ ] Rollback approval works.
* [ ] Source revisions remain historical.
* [ ] Effective Snapshots remain historical while retained.
* [ ] Activation receipts remain historical.
* [ ] Policy conflicts remain historical.
* [ ] History events are append only.
* [ ] Optional hash-chain integrity works and limitations are documented.

## Migration and Deprecation

* [ ] Migration schema exists.
* [ ] Rename Setting works.
* [ ] Move Setting works.
* [ ] Split Setting works.
* [ ] Combine Settings works.
* [ ] Convert Type works.
* [ ] Convert Unit works.
* [ ] Map Enumeration works.
* [ ] Insert Default works.
* [ ] Remove Deprecated works.
* [ ] Convert Policy works.
* [ ] Custom transform is first party.
* [ ] Migrations are deterministic.
* [ ] Migrations are pure.
* [ ] Migrations are idempotent.
* [ ] Target schema validation runs.
* [ ] Migration preview exists.
* [ ] Migration creates new revisions.
* [ ] Historical revisions are not rewritten.
* [ ] Unknown future schema is rejected.
* [ ] Deprecation warnings exist.
* [ ] Replacement settings are identified.
* [ ] Removal release is identified.
* [ ] Removed critical setting is not silently ignored.
* [ ] Unknown extensions are preserved only in explicit extension containers.

## Plugin, MCP, Provider and Model Integration

* [ ] Plugin setting namespace is enforced.
* [ ] Plugin package revision binds definitions.
* [ ] Plugin cannot register Product Policy.
* [ ] Plugin cannot register Enterprise Policy.
* [ ] Plugin cannot broaden capability.
* [ ] Plugin uninstall handles inactive configuration.
* [ ] MCP server configuration uses gateway-owned references.
* [ ] MCP credentials use Vault references.
* [ ] Project files cannot grant MCP trust.
* [ ] Provider configuration uses exact Provider Profile references.
* [ ] Provider credentials remain in Vault.
* [ ] Provider terms remain Provider Trust evidence.
* [ ] Local model configuration uses exact Model and Execution references.
* [ ] Local model file paths remain owner-service state.
* [ ] Workflow settings pin new instances correctly.
* [ ] Routing settings invalidate stale future decisions.
* [ ] Context settings bind exact policies.
* [ ] Knowledge changes trigger reindex where required.
* [ ] Memory policy retains model non-authority.
* [ ] Logging redaction cannot be disabled.
* [ ] Update settings remain within ADR-0015.

## Import and Export

* [ ] Configuration Bundle schema exists.
* [ ] Bundle manifest is hashed.
* [ ] Archive paths are validated.
* [ ] Archive sizes are bounded.
* [ ] Strict JSON parsing is used.
* [ ] Schemas are validated.
* [ ] Secret scanning is used.
* [ ] Scopes are mapped explicitly.
* [ ] Unknown settings are visible.
* [ ] Service references are resolved.
* [ ] Effective-change preview exists.
* [ ] Imported Profiles are candidates.
* [ ] Imported Profiles do not auto-activate.
* [ ] Imported policy does not gain Enterprise authority.
* [ ] Imported Product Policy does not gain invariant authority.
* [ ] Machine policy is excluded by default from export.
* [ ] Sensitive values are masked.
* [ ] Vault values are excluded.
* [ ] Executable content is rejected.
* [ ] Import provenance is retained.
* [ ] Export provenance is retained.

## Retention, Deletion and Recovery

* [ ] Retention policy exists.
* [ ] Definitions remain while referenced.
* [ ] Profile revisions remain while referenced.
* [ ] Operation-bound snapshots remain while referenced.
* [ ] Invalid source payload retention is bounded.
* [ ] Import payload retention is bounded.
* [ ] Export staging retention is bounded.
* [ ] Session overrides expire.
* [ ] Active Profile cannot be deleted without selection change.
* [ ] Referenced revision deletion is protected.
* [ ] Purge removes controlled payloads.
* [ ] Tombstones contain no secrets.
* [ ] Forensic deletion limitations are displayed.
* [ ] Startup integrity checks work.
* [ ] Definition mismatch triggers migration or safe failure.
* [ ] Project-source reconciliation works after restart.
* [ ] Enterprise Policy is reread after restore.
* [ ] Another-machine restore re-evaluates machine policy.
* [ ] Missing provider or model references become inactive or invalid.
* [ ] Repair Mode exists.
* [ ] Trust Centre is available during repair where safe.

## Trust and User Experience

* [ ] Effective Configuration view exists.
* [ ] Requested versus Effective view exists.
* [ ] Profile view exists.
* [ ] Setting Source view exists.
* [ ] Policy Source view exists.
* [ ] Enterprise Machine Policy view exists.
* [ ] Enterprise User Policy view exists.
* [ ] Project Settings view exists.
* [ ] Project Policy view exists.
* [ ] Session Override view exists.
* [ ] Operation Override view exists.
* [ ] Pending Activation view exists.
* [ ] Restart Requirement view exists.
* [ ] Conflict view exists.
* [ ] Validation view exists.
* [ ] History view exists.
* [ ] Migration view exists.
* [ ] Import and Export view exists.
* [ ] Per-setting search works.
* [ ] Policy locks explain source.
* [ ] Source-controlled settings expose safe edit actions.
* [ ] Invalid external edit copy is clear.
* [ ] Last-known-good copy is clear.
* [ ] Policy-conflict copy is clear.
* [ ] UI is keyboard accessible.
* [ ] UI supports Narrator.
* [ ] UI supports high contrast.
* [ ] Status does not rely on colour alone.
* [ ] Security review is complete.
* [ ] Privacy review is complete.
* [ ] Enterprise review is complete.
* [ ] Founder approval is recorded.

---

# 383. Evidence Required Before Acceptance

* [ ] Configuration and Policy Service contract.
* [ ] Setting Definition schema.
* [ ] Policy Definition schema.
* [ ] scope taxonomy.
* [ ] source taxonomy.
* [ ] merge-strategy specification.
* [ ] null-semantics specification.
* [ ] constraint-combination specification.
* [ ] Configuration Profile schema.
* [ ] project-settings schema.
* [ ] project-policy schema.
* [ ] schema-catalogue specification.
* [ ] Effective Configuration Snapshot schema.
* [ ] per-key provenance schema.
* [ ] Configuration Change Transaction schema.
* [ ] Activation Plan schema.
* [ ] Activation Receipt schema.
* [ ] migration schema.
* [ ] Configuration Bundle schema.
* [ ] Product Invariant Policy catalogue.
* [ ] Release-Channel Policy catalogue.
* [ ] initial enterprise-policy catalogue.
* [ ] default Setting Definition catalogue.
* [ ] strict JSON parser report.
* [ ] duplicate-property rejection report on .NET 10.
* [ ] parser-differential report.
* [ ] UTF-8 validation report.
* [ ] JSON Schema 2020-12 subset report.
* [ ] no-remote-reference report.
* [ ] regex-bounds report.
* [ ] Setting Definition validation report.
* [ ] Policy Definition validation report.
* [ ] Profile inheritance report.
* [ ] multiple-inheritance denial report.
* [ ] setting-merge test report.
* [ ] per-element provenance report.
* [ ] policy-intersection report.
* [ ] empty-permitted-space report.
* [ ] Product Invariant bypass report.
* [ ] Enterprise Machine Policy report.
* [ ] Enterprise User Policy report.
* [ ] HKLM and HKCU intersection report.
* [ ] registry-type validation report.
* [ ] registry-view report.
* [ ] registry-notification and reconciliation report.
* [ ] Opure.admx prototype.
* [ ] Opure.adml prototype.
* [ ] ADMX mapping report.
* [ ] RSoP documentation review.
* [ ] project-settings Workspace report.
* [ ] project-policy restriction report.
* [ ] project-file path-security report.
* [ ] source watcher reconciliation report.
* [ ] partial-file-write report.
* [ ] atomic project-file write report.
* [ ] branch-checkout impact report.
* [ ] approved environment-variable catalogue.
* [ ] environment policy-bypass report.
* [ ] typed command-line catalogue.
* [ ] command-line secret denial report.
* [ ] Effective Snapshot reproducibility report.
* [ ] per-key Resultant Configuration examples.
* [ ] snapshot-pinning report.
* [ ] atomic Configuration Change report.
* [ ] optimistic-concurrency report.
* [ ] preview and approval binding report.
* [ ] runtime-application-class report.
* [ ] owner-service activation report.
* [ ] partial-activation safety report.
* [ ] .NET typed-options adapter report.
* [ ] startup-validation report.
* [ ] last-known-good report.
* [ ] critical fail-closed report.
* [ ] policy uncertainty report.
* [ ] rollback revalidation report.
* [ ] configuration-history report.
* [ ] migration purity and idempotency report.
* [ ] deprecation and removal report.
* [ ] plugin namespace report.
* [ ] plugin policy-escalation report.
* [ ] MCP configuration and trust report.
* [ ] Provider Profile reference report.
* [ ] Local Model Profile reference report.
* [ ] Workflow snapshot-pinning report.
* [ ] Routing Decision invalidation report.
* [ ] Knowledge reindex-impact report.
* [ ] Memory policy report.
* [ ] logging-redaction invariant report.
* [ ] update-policy integration report.
* [ ] import archive-security report.
* [ ] import authority-escalation report.
* [ ] export privacy report.
* [ ] retention report.
* [ ] deletion and purge report.
* [ ] backup and restore report.
* [ ] another-machine policy re-evaluation report.
* [ ] startup recovery report.
* [ ] database-corruption rehearsal.
* [ ] Trust Centre screenshots or UI test evidence.
* [ ] diagnostics leakage report.
* [ ] policy-bypass adversarial suite report.
* [ ] source-substitution adversarial report.
* [ ] configuration split-brain report.
* [ ] rollback adversarial report.
* [ ] performance report.
* [ ] scale report.
* [ ] endurance report.
* [ ] accessibility report.
* [ ] security review.
* [ ] privacy review.
* [ ] enterprise management review.
* [ ] founder approval.

---

# 384. Known Limitations

* The final Setting Definition catalogue is not selected.
* The final Policy Definition catalogue is not selected.
* The initial ADMX catalogue is not final.
* MDM integration is deferred.
* Cloud configuration sync is unavailable.
* Organisation-wide Profile sync is unavailable.
* Multi-parent Profile inheritance is unavailable.
* Arbitrary policy scripting is unavailable.
* OPA and Rego are not integrated.
* Dynamic user-defined core settings are unavailable.
* Project configuration comments are unavailable.
* Trailing commas are unavailable.
* Strict JSON is less forgiving for manual editing.
* JSON Schema Draft 2020-12 support will be a reviewed subset initially.
* Remote schema references are unavailable.
* Some schema keywords may be deferred.
* System.Text.Json stable .NET 10 does not expose the preview duplicate-property API documented for .NET 11; a first-party duplicate detector is therefore required.
* File watchers can miss events, so reconciliation adds latency.
* Registry notification can fail, so periodic reconciliation remains necessary.
* Registry policy does not by itself identify the winning GPO.
* Opure's Resultant Configuration is not Windows RSoP.
* ADMX and ADML files require enterprise template maintenance.
* A malformed enterprise policy can disable capabilities.
* A conflict between legitimate administrators can require external resolution.
* Product Invariant Policy cannot protect against a fully compromised signed application.
* Same-user malware can modify user-owned configuration storage.
* Hash chains do not prevent a complete same-user rewrite.
* Secret scanning has false positives and false negatives.
* Vault references can become stale.
* Service references can become unavailable.
* Partial activation can require repair.
* Some settings require service or application restart.
* Some settings require project reindex.
* Some settings affect only new workflows.
* Running work may continue on earlier permitted snapshots.
* Critical policy changes can stop running or future capabilities.
* Rollback is not guaranteed to restore prior behaviour under changed policy.
* Historical revisions consume storage.
* Configuration imports can require significant reference mapping.
* Forensic secure deletion is not guaranteed.
* The initial retention values are provisional.
* The initial performance targets require evidence.
* Cross-platform enterprise-policy transport is deferred.

---

# 385. Open Questions

* Which exact SQLite version should Configuration Service pin?
* Should Configuration use one database per channel or one database per project?
* Would project databases improve deletion and isolation?
* How are global Profile selection and project snapshots committed atomically across databases?
* Should System and User configuration use separate databases?
* Which database durability setting is required for configuration commits?
* Should security-critical configuration use `synchronous=FULL`?
* Which change events require full synchronous durability?
* How are CAS reference counts maintained?
* Which history payloads may be compacted?
* Should per-key provenance be fully relational or JSON payload based?
* How many historical Effective Snapshots should be retained?
* Should snapshots referenced by AI receipts remain longer?
* Should snapshots referenced by workflows remain until workflow purge?
* How are project snapshots deleted with a project?
* Which opaque ID format is selected?
* Which canonical JSON writer is selected?
* How are decimal values canonicalised?
* How are durations encoded?
* Should durations use ISO 8601 strings or integer milliseconds?
* How are byte sizes displayed and parsed?
* Should human-friendly strings ever be accepted in UI and normalised before storage?
* How are time values represented without accidental local time?
* Which Setting Definition fields are mandatory?
* Which failure classes are final?
* Should `Operational` be split further?
* Which settings require a safe product fallback?
* Which settings fail the owning service?
* Which settings fail the whole Runtime?
* How are definition catalogues versioned across product updates?
* Should every service own a source-controlled definitions file?
* How are cross-service setting IDs reviewed?
* How are naming conflicts prevented?
* Can settings move between owner services?
* How are aliases retained after rename?
* How long are deprecated aliases accepted?
* Which value types belong in Version 1?
* Are discriminated unions necessary initially?
* Are rule lists necessary initially?
* Which URI schemes are permitted?
* How are internationalised hostnames handled?
* How are path-reference identities represented?
* Can a Profile contain a raw absolute path?
* Which machine-local paths may be configured?
* How are removable drives treated?
* How are network paths treated?
* Can enterprise policy allow a network path?
* How are UNC allowlists represented?
* How are reparse points revalidated at use?
* Which collection limits are appropriate?
* Is 4 MiB an appropriate document limit?
* Is 10,000 settings per Profile excessive?
* Which JSON depth limit is appropriate?
* Should a UTF-8 BOM be accepted?
* Which duplicate-property detector implementation is selected?
* Should duplicate detection occur before all binding?
* How is escaped-equivalent property identity implemented?
* Are property names compared ordinally?
* Should Unicode normalisation apply to human-defined map keys?
* How are confusable map keys shown?
* Should strict JSON permit comments in Development only?
* Would a separate JSONC authoring format be useful?
* How would JSONC canonicalise into strict JSON?
* Should source-controlled files remain strict for reproducibility?
* Which JSON Schema library is selected?
* Does it fully support Draft 2020-12?
* Which vocabularies are required?
* Are dynamic references needed?
* Is `unevaluatedProperties` required?
* How are schema-bundling references resolved?
* How are schema IDs protected from collision?
* Should plugin schemas be bundled inside plugin packages?
* How are plugin-schema updates pinned?
* Which `format` values are assertions?
* Which regex engine is selected?
* Should regex be prohibited from configuration schemas initially?
* How are schema-validation errors localised?
* How are schema errors shown in source files?
* Should Opure emit editor diagnostics through LSP?
* Should schemas be published for Visual Studio and VS Code?
* What logical schema URI is selected?
* Should `https://schemas.opure.local/` be used?
* Could a `.local` URI be misleading?
* Should `urn:opure:schema:` be preferred?
* How are source document IDs defined?
* Are document revision fields required?
* Which repository evidence is recorded?
* Should Git blob and Workspace hashes both be kept?
* How are line-ending conversions handled?
* How are Git LFS configuration files handled?
* What happens when `.opure` is ignored?
* Should project templates create `.opure` automatically?
* Should project settings be enabled by default?
* Should project policy be enabled by default?
* Can enterprise policy require project policy?
* Can enterprise policy prohibit repository configuration?
* Can project policy require Local Only?
* Can project policy restrict plugins?
* Can project policy restrict MCP?
* Can project policy restrict update behaviour?
* Should project policy affect UI preferences?
* Which policy constraints may a project source declare?
* How are project-policy changes reviewed on branch checkout?
* Should a pull request configuration change require an explicit confirmation?
* Can trusted repository status affect confirmation?
* How is a newly cloned repository handled?
* Should project settings activate immediately on first open?
* Should project policy activate before indexing?
* How is invalid project configuration shown before project readiness?
* How are source watcher events debounced?
* What retry delay handles partial writes?
* How frequently should periodic reconciliation run?
* Should reconciliation run at every operation start?
* How is configuration freshness cached?
* Which critical operations require synchronous source validation?
* How are large project files reread efficiently?
* Can source hashes be obtained through Workspace without reading again?
* How are file deletion and recreation distinguished?
* How are file renames handled?
* How does an Opure edit interact with an editor's unsaved buffer?
* Should Opure offer a source-control-ready file editor?
* How is formatting preserved after Opure edits?
* Should Opure use canonical sorted JSON or preserve user order?
* How are comments handled if future JSONC exists?
* How are project-file conflicts merged?
* Which settings can merge automatically?
* Should same-key conflicts always require review?
* How are list conflicts represented?
* How are map-key conflicts represented?
* How is policy conflict represented in repository diffs?
* Which source precedence is final?
* Does Machine Preference belong before or after User Base?
* Should Project Shared Settings override Named User Profile?
* Should developer preferences override project shared style choices?
* Should each Setting Definition be allowed to customise source order?
* Could customised source order become too difficult to explain?
* Should source-order variants be a small allowlisted set?
* Which merge strategies belong in Version 1?
* Is `First Explicit` necessary?
* Are Minimum and Maximum setting merges confusing beside policy bounds?
* Should map removal use JSON Patch semantics?
* Should an RFC 6902-inspired format be used for staged changes?
* How are reserved `$remove` keys protected in user maps?
* Which equality comparer applies to Ordered Unique?
* How are list entries identified?
* How are typed rule lists evaluated?
* Can rule lists grant capability accidentally?
* Which Custom Reducers are initially necessary?
* Should custom reducers be prohibited until evidence appears?
* How are reducer implementations pinned across old Profile revisions?
* Which Policy Definition types belong in Version 1?
* Should Force Value be used sparingly?
* How are machine and user force conflicts handled?
* Which definitions permit machine precedence?
* Should Product Invariant always beat machine force?
* How are legally required minimum-retention policies represented?
* How are privacy maximum-retention policies combined with legal minimums?
* What happens when legal minimum exceeds privacy maximum?
* Should that be a policy conflict requiring administrator resolution?
* How are currency-denominated maximum costs represented?
* Which exchange rate source is used?
* Should policy use provider billing currency only?
* How are aggregate workflow budgets represented?
* How are provider allowlists represented across profile revisions?
* Can a policy allow an operator but not an exact Provider Profile?
* How is provider-region identity normalised?
* How are capability identifiers versioned?
* Which denials are immediate revocations?
* Which policy changes affect running workflows?
* Which policy changes affect current model inference?
* Which policy changes invalidate Context Plans?
* Which policy changes invalidate approvals?
* Which policy changes invalidate Data Sharing Plans?
* Which policy changes invalidate Routing Decisions?
* How are revocation messages distributed?
* Should Configuration Service issue policy capabilities directly?
* Or should each owning service issue capabilities from snapshots?
* Which service evaluates final path policy?
* How are operation constraints represented?
* Can an Operation Override request a stricter value?
* Can it request a broader value that is then rejected?
* Should rejected operation overrides remain in provenance?
* How long are operation snapshots retained?
* How are session overrides displayed persistently?
* What defines one session?
* Does Runtime restart end a session?
* Does Windows sign-out end it?
* Should Session Overrides survive Desktop close?
* Should Workspace Session settings be stored per window?
* How are multiple Desktop windows handled?
* Which named Profiles ship as templates?
* Should Maximum Privacy be a built-in template?
* Should Balanced Local be the default?
* How is a template distinguished from a user's Profile?
* How are template updates handled?
* Does selecting a template create a new Profile revision?
* Can Profiles be locked?
* Can enterprise policy force one Profile?
* Should enterprise policy force settings instead of a Profile identity?
* How are Profile names localised?
* Are Profile names personal data?
* How are Profile exports de-identified?
* Should Profile inheritance depth be lower than eight?
* Is one parent sufficient?
* Would component Profile composition be better than inheritance?
* Should a Profile be able to include named component Profiles?
* How would composition precedence work?
* What evidence is needed before multiple inheritance?
* How are Profile parent revisions retained?
* Can Profile selection be project-specific?
* Can one project pin a named Profile revision?
* Should project settings refer to a Profile by name or ID?
* Is cross-machine Profile identity portable?
* How are imported Profile IDs mapped?
* How are duplicate imported Profiles detected?
* Should imported Profiles receive new IDs?
* How are imported references to Providers or Models mapped?
* How are missing Vault references handled?
* Can export include placeholder secret-reference names?
* How are enterprise machine policies enumerated?
* Which registry value naming scheme is best?
* Should policy values be flat or nested subkeys?
* How are long Provider Profile allowlists represented?
* Is `REG_MULTI_SZ` sufficient?
* When is bounded JSON in registry justified?
* How are registry JSON documents versioned?
* How are registry value size limits enforced?
* Should invalid unknown registry values fail a policy source?
* Should known values remain active when one unrelated value is invalid?
* How is per-value policy validation represented?
* Does one invalid critical value invalidate the whole source?
* How are HKLM and HKCU permissions tested?
* How are policy values refreshed on domain Group Policy updates?
* Is registry notification sufficient?
* What periodic interval is appropriate?
* Should session unlock trigger every time?
* Should network reconnect trigger Group Policy reconciliation?
* Should Opure invoke `gpupdate`?
* Likely not; how is that documented?
* How are ADMX categories organised?
* Which policies are binary versus enumerated?
* How are ADML languages added?
* Which version metadata belongs in ADMX?
* How are old Central Store templates handled?
* How are extra registry settings diagnosed?
* Should Opure offer a policy health export for administrators?
* Can Opure call Windows RSoP APIs?
* Is that worthwhile or too complex?
* Should Opure only provide registry provenance and external RSoP guidance initially?
* How does MDM ADMX ingestion map to the same registry values?
* Which Windows editions support the selected enterprise path?
* How are workgroup machines handled?
* Which environment variables are truly needed?
* Should `OPURE_CHANNEL` be available in installed Stable builds?
* Could it create channel confusion?
* Should channel be determined only by package identity?
* Which diagnostics flags are safe?
* How is Test Mode protected from production activation?
* Should environment overrides be compiled out of Stable?
* How are CI overrides authorised?
* Should response files be supported for command-line options?
* Could response files contain secrets?
* Should they be prohibited?
* How are duplicate command-line options handled?
* Does last argument win or fail?
* How are command-line values included in support evidence?
* How is process command-line exposure disclosed?
* Which configuration changes require approval?
* Who can approve in the single-founder initial product?
* How are future enterprise roles represented?
* Can an administrator pre-approve classes of changes?
* How are policy waivers modelled?
* Are waivers allowed at all for Product Invariants?
* Which Product Invariants can never be waived?
* How are temporary enterprise exceptions represented?
* Should exceptions be separate signed policy sources?
* How do exception expiries work?
* How are stale approvals invalidated?
* Which changes are material to a preview?
* Does a source-provenance change with the same effective value invalidate approval?
* Does policy-source revision with same constraint invalidate?
* Does estimated cost change invalidate?
* How are activation dependencies modelled?
* Can services activate in parallel?
* How are activation timeouts handled?
* How are service activation retries handled?
* Can an activation have side effects requiring workflow semantics?
* Should complex activation use ADR-0025 workflows?
* How is a model reload activation represented?
* How is reindex activation represented?
* Can Configuration commit before a required migration?
* Should some changes remain staged until migration completes?
* How are failed migrations rolled back?
* How is partial activation repaired?
* Should the system auto-rollback a failed optional preference?
* Should security-critical activation failure restore a prior permitted snapshot?
* What if prior snapshot is now prohibited?
* How are service snapshot acknowledgements secured?
* How are stale services detected?
* Should every service expose active snapshot in health?
* How is process restart reconciled with pending snapshots?
* Which `.NET` Options APIs are selected?
* Should source-generated validation be mandatory?
* Does .NET 10 provide every required stable generator feature?
* How are nested immutable records bound?
* Should services avoid `IOptionsMonitor` entirely?
* How are configuration adapters tested for snapshot equivalence?
* How are startup-critical settings loaded before Configuration Service is fully ready?
* What minimal bootstrap configuration is compiled?
* How is Configuration database path selected safely?
* How is release channel selected before configuration loads?
* How are logging and diagnostics initialised before full settings?
* Which bootstrap values are trusted?
* How is Last Known Good represented?
* Is it per service or root snapshot?
* How many previous activated snapshots are retained?
* Which invalid preference falls back automatically?
* Which invalid operational setting preserves old active value?
* Which critical setting disables capability immediately?
* How are current-policy checks made against old snapshots?
* How are policy-source uncertainty and last observation bounded in time?
* Can enterprise policy have an expiry?
* How are temporary policies represented?
* How are source freshness and policy freshness shown?
* Should stale enterprise policy ever continue indefinitely?
* What safe behaviour applies when registry is inaccessible?
* How are recovery and Repair Mode configured without reading invalid settings?
* Which settings are available in Repair Mode?
* How is a corrupted Configuration database restored?
* Can Profiles be rebuilt from exports?
* Can project configuration rebuild project snapshots?
* How are User Profile values recovered without backup?
* Which settings are non-rebuildable and require backup?
* How are backups encrypted?
* How are backups versioned?
* How are backups restored across channels?
* How are machine-specific paths and references mapped?
* How are profile exports signed?
* Is signing necessary?
* How are malicious imported bundles quarantined?
* Which archive format is selected?
* How are bundle paths canonicalised?
* How are duplicate archive entries rejected?
* Can configuration export be a directory rather than archive?
* How are unknown extension settings preserved?
* Can a plugin import its old settings after reinstall?
* How are plugin settings migrated between package versions?
* Can plugin uninstall request deletion?
* How are plugin configuration definitions inspected?
* How does a plugin declare restart impact?
* Can a plugin request application restart?
* Should plugin settings always apply only to its host process?
* How are MCP server launch arguments configured safely?
* Which arguments may reference Vault entries?
* How are MCP environment variables handled?
* Can MCP settings be source controlled?
* Probably not for credentials; which non-secret parts can?
* How are Provider Profile references exported?
* How are Provider Profile references imported on another machine?
* How are local model Profiles mapped across machines?
* How are missing models offered for installation without auto-install?
* How are workflow Profile references migrated?
* How are running workflows affected by Profile deletion?
* How are active Context Plans affected by source changes?
* Which Knowledge settings trigger full versus partial reindex?
* Which Memory settings trigger re-embedding?
* Which logging settings hot apply?
* Which update settings require Runtime restart?
* Which Desktop settings hot apply?
* How is Resultant Configuration rendered for 10,000 settings?
* Which settings are hidden as advanced?
* How is per-key provenance made understandable?
* How are merged collections visualised?
* How are denied values shown without clutter?
* How are policy conflicts grouped?
* How are restart requirements grouped?
* How are project-file source locations linked safely?
* Can a user copy a setting ID?
* Can an administrator export a policy report?
* How are sensitive values masked consistently?
* Should masked values show a hash?
* Could hashes leak low-entropy settings?
* When should even hashes be hidden?
* How are diagnostics support bundles filtered?
* How are configuration metrics useful without high-cardinality IDs?
* Which performance targets matter most?
* Is 100 ms snapshot build realistic?
* How are snapshot builds cached?
* How are caches invalidated?
* Can unaffected service sections reuse hashes?
* How are policy reductions incremental?
* How are 10,000 open projects handled?
* Is that scale necessary on a desktop?
* How are project snapshots unloaded?
* How are source watchers capped?
* How are registry watchers shared?
* Which endurance failures are acceptable?
* What permanent evidence is required for a policy-bypass incident?
* What permanent evidence is required for a secret-in-configuration incident?
* What permanent evidence is required before cloud profile synchronisation can be considered?
* What permanent evidence is required before multiple Profile inheritance can be considered?
* What permanent evidence is required before a general policy language can be considered?

---

# 386. Deferred Decisions

This ADR intentionally defers:

* final Setting Definition catalogue;
* final Policy Definition catalogue;
* final ADMX policy catalogue;
* MDM administration;
* cloud configuration sync;
* organisation-wide profile sync;
* web administration;
* roaming profiles;
* multiple Profile inheritance;
* Profile composition beyond the source hierarchy;
* arbitrary policy scripting;
* OPA or Rego;
* JSON Logic;
* remote schema references;
* JSONC project files;
* dynamic user core-setting definitions;
* custom Group Policy client-side extension;
* cryptographically signed configuration history;
* signed Profile exports;
* and cross-platform enterprise-policy transport.

---

# 387. Alternatives Rejected

A plain `Microsoft.Extensions.Configuration` stack is rejected as the full authority because later-provider-wins semantics cannot represent non-bypassable typed policy intersection and immutable cross-service operation snapshots.

One mutable JSON file is rejected because user, machine, project, enterprise, session and operation scopes require separate authority and history.

Registry-only configuration is rejected because project files, structured Profiles, migration and provenance need a richer store.

Group Policy-only configuration is rejected because ordinary developer preferences and local project settings are not enterprise policy.

General environment-variable and command-line precedence is rejected because it is difficult to inspect and can expose or unexpectedly override values.

Repository-only configuration is rejected because machine preferences, user Profiles, enterprise policy, Vault references and local overrides do not belong entirely in source control.

OPA and Rego are deferred because Version 1 policies can be expressed as bounded typed constraints without introducing another general execution runtime.

Arbitrary policy expressions are rejected because they weaken predictability, static validation and provenance.

Cloud-hosted configuration authority is rejected because Opure must operate offline and remain local first.

Opaque key-value storage is rejected because types, merge, scope, activation and policy would remain implicit.

Independent per-service configuration is rejected because policy and revision consistency would diverge.

Multiple Profile inheritance is deferred because precedence and conflict explanation would become substantially more complex.

Comments and trailing commas in authoritative project files are rejected initially because strict JSON simplifies canonicalisation, parser agreement and security review.

---

# 388. Official and Primary Evidence References

## .NET Configuration and Options

* [Configuration providers in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-providers)
* [Options pattern guidance for .NET library authors](https://learn.microsoft.com/en-us/dotnet/core/extensions/options-library-authors)
* [Compile-time options validation source generation](https://learn.microsoft.com/en-us/dotnet/core/extensions/options-validation-generator)
* [FileConfigurationSource.ReloadOnChange](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.fileconfigurationsource.reloadonchange)
* [OptionsBuilderExtensions.ValidateOnStart](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.optionsbuilderextensions.validateonstart)

## System.Text.Json

* [JsonDocumentOptions](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsondocumentoptions)
* [JsonDocumentOptions.AllowDuplicateProperties](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsondocumentoptions.allowduplicateproperties)
* [JsonSerializerOptions.AllowDuplicateProperties](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonserializeroptions.allowduplicateproperties)

The duplicate-property APIs above are currently documented for .NET 11 preview builds.

Opure targets .NET 10 LTS and should not assume those preview APIs are available.

## JSON Schema

* [JSON Schema Draft 2020-12](https://json-schema.org/draft/2020-12)
* [JSON Schema Core, Draft 2020-12](https://json-schema.org/draft/2020-12/json-schema-core.html)
* [JSON Schema Validation, Draft 2020-12](https://json-schema.org/draft/2020-12/json-schema-validation.html)

## Windows Group Policy and Administrative Templates

* [Administrative Template File ADMX Format](https://learn.microsoft.com/en-us/previous-versions/windows/desktop/policy/admx-schema)
* [Extending Registry-based Policy](https://learn.microsoft.com/en-us/previous-versions/windows/desktop/policy/extending-registry-based-policy)
* [Working with Administrative Template policy settings](https://learn.microsoft.com/en-us/previous-versions/windows/it-pro/windows-server-2012-r2-and-2012/dn789184%28v%3Dws.11%29)
* [Create and manage the Central Store for Group Policy Administrative Templates](https://learn.microsoft.com/en-us/troubleshoot/windows-client/group-policy/create-and-manage-central-store)
* [Group Policy preferences](https://learn.microsoft.com/en-us/windows-server/identity/ad-ds/manage/group-policy/group-policy-preferences)
* [Group Policy Modeling and Results](https://learn.microsoft.com/en-us/windows-server/identity/ad-ds/manage/group-policy/group-policy-modeling-results)
* [Get-GPResultantSetOfPolicy](https://learn.microsoft.com/en-us/powershell/module/grouppolicy/get-gpresultantsetofpolicy)
* [Policy CSP ADMX GroupPolicy](https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-grouppolicy)

The .NET configuration provider stack, JSON APIs, JSON Schema implementations, Windows policy behaviour and ADMX distribution details can change.

The implementation must revalidate all selected APIs, registry mappings, schema support and enterprise-management assumptions before acceptance.

---

# 389. Review Record

| Date         | Reviewer           | Decision | Notes                                                                                                   |
| ------------ | ------------------ | -------- | ------------------------------------------------------------------------------------------------------- |
| 18 July 2026 | Architecture draft | Proposed | Typed immutable settings and separately intersected policy with exact activation provenance recommended |

---

# 390. Approval

## Founder or Product Approval

* **Name:** Christopher Dyer
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Source hierarchy, policy constraints, strict JSON, Profiles and activation-impact review required

## Configuration Architecture Approval

* **Name or role:** Configuration and Policy Architecture Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Definitions, merge, snapshots, transactions, migration and recovery evidence required

## Enterprise Management Approval

* **Name or role:** Enterprise Management Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Registry mappings, machine/user policy, ADMX, ADML and administrative diagnostics required

## Runtime and Service Approval

* **Name or role:** Runtime Architecture and Service Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Typed snapshot adapters, activation receipts, restart impact and split-brain safety required

## Project and Workspace Approval

* **Name or role:** Project Service, Workspace and Repository Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Project files, branch changes, atomic writes and source provenance required

## AI and Workflow Approval

* **Name or role:** Workflow, AI Router, Context, Knowledge and Memory Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Snapshot pinning, policy invalidation, reindex and running-operation behaviour required

## Plugin and MCP Approval

* **Name or role:** Plugin Platform and MCP Gateway Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Namespaces, references, secrets and authority non-escalation required

## Security, Privacy and Secrets Approval

* **Name or role:** Security, Privacy and Secrets Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Product invariants, secret exclusion, policy bypass, retention and export evidence required

## Recovery Approval

* **Name or role:** Recovery Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Last known good, rollback, migration, backup and repair-mode evidence required

## Test Approval

* **Name or role:** Test Architecture Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Parser differential, registry, policy bypass, partial-write and split-brain suites required

---

# 391. Supersession

This ADR is superseded only when a later ADR:

* names ADR-0026 explicitly;
* explains why settings, policy, Profiles, source precedence, merge, snapshots or activation changed;
* identifies Setting Definition, Policy Definition, Profile, Source Revision and Effective Snapshot migration;
* describes enterprise registry, project file, secret, workflow and operation impact;
* provides policy-bypass and activation-consistency comparison evidence;
* and updates the `Superseded by` field.

Historical Profile revisions, source revisions, Effective Snapshots, policy conflicts, change transactions and activation receipts remain available according to retention policy unless explicitly purged.

---

# 392. Change History

| Version | Date         | Author        | Summary                                                                                                      |
| ------- | ------------ | ------------- | ------------------------------------------------------------------------------------------------------------ |
| 0.1     | 18 July 2026 | Founder Draft | Initial typed configuration, immutable Profile, policy-intersection and activation-governance recommendation |

---

# 393. Final Decision Statement

> **Opure will provisionally manage configuration through one trusted local Configuration and Policy Service that registers versioned typed Setting and Policy Definitions, treats settings as requested choices and Product, Channel, Enterprise, Project, Workflow and Operation policy as separately intersected constraints, reads immutable Profile revisions, strict locally schemed JSON project sources and validated Windows registry policy into exact source revisions, applies definition-owned merge and null semantics, rejects duplicate keys, remote schemas, secret values and authority escalation, and publishes immutable Effective Configuration Snapshots with per-key provenance, policy conflicts and activation impact through atomic reviewed change transactions and owner-service receipts, while environment variables and command-line options remain allowlisted and bounded, file watchers remain hints followed by complete source reconciliation, invalid edits retain only a still-policy-compliant last-known-good snapshot, rollback and migration create new revisions, running operations and workflows pin exact configuration, and no project file, imported bundle, plugin, MCP server, profile name or later value can weaken built-in safety or enterprise policy, because developer control requires every effective value, restriction, restart, rejection and historical change to be visible, explainable and reversible without turning ordinary configuration precedence into a security boundary.**

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**