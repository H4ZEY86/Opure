# ADR-0024 — Model Evaluation, Quality and Routing Governance

## Architecture Decision Record

**Status:** Proposed
**Date:** 18 July 2026
**Decision owners:** Founder and Product Owner
**Technical owners:** AI Evaluation and Routing Governance Owner
**Reviewers:** Runtime Architecture Owner, AI Router Owner, Context Assembly Owner, Local Model Runtime Owner, Provider Trust Owner, Project Knowledge Owner, Project Memory Owner, Workflow Owner, Tool Mediation Owner, Patch Service Owner, Build Owner, Test Owner, Security Owner, Privacy Owner, Secrets Owner, Plugin Platform Owner, MCP Gateway Owner, Persistence Owner, Trust Centre Owner, Desktop Owner, Performance Owner, Release Owner, Recovery Owner, Test Architecture Owner
**Supersedes:** None
**Superseded by:** None
**Related ADRs:** ADR-0001 through ADR-0023
**Related specifications:** CHARTER-001, SPEC-001 through SPEC-012
**Target milestone:** Model Qualification and Governed Routing through Version 1.0

---

## 1. Decision Summary

Opure should implement model evaluation and routing as two distinct but connected trusted capabilities:

1. an **AI Evaluation Service** that produces reproducible evidence about exact model, provider, runtime, prompt, context, tool and workflow configurations; and
2. a **Routing Governance Service** that applies explicit project policy and accepted evaluation evidence to choose one approved execution profile for one operation.

The AI Router should execute an already-authorised Routing Decision.

The AI Router should not invent routing policy.

A model should not choose which model runs the operation.

A provider-managed opaque prompt router should not become Opure's default model-selection authority.

A plugin or MCP server should not select a provider or model outside its approved capability.

The initial architecture should treat a routed AI system as more than a model name.

The evaluated and routed unit should be an immutable **AI Execution Profile** containing:

* provider or local runtime;
* exact model identifier;
* model snapshot or resolved revision;
* endpoint;
* region;
* provider profile revision;
* local Runtime Package where applicable;
* local Model Manifest where applicable;
* instruction-template revision;
* request renderer;
* Tokenizer Profile;
* Context Policy;
* tool projection;
* structured-output schema;
* reasoning or thinking settings;
* sampling settings;
* maximum output;
* truncation policy;
* safety settings;
* provider data posture;
* local Execution Profile;
* and relevant adapter version.

A model family, display alias or marketing name should not be the evaluated unit.

A mutable alias such as:

```text
latest
default
recommended
auto
```

should not be used for Stable production routing.

The initial evaluation architecture should:

* define success before running models;
* use task-specific and risk-specific evaluation suites;
* preserve exact datasets, prompts, sources, graders and execution profiles;
* separate:

  * capability;
  * correctness;
  * instruction following;
  * grounding;
  * patch validity;
  * security;
  * privacy;
  * tool behaviour;
  * robustness;
  * latency;
  * throughput;
  * resource use;
  * cost;
  * developer effort;
  * and subjective quality;
* prefer deterministic and executable graders whenever the result can be verified through trusted software;
* use human review for nuanced engineering judgement and calibration;
* use model-based graders only when deterministic grading is insufficient;
* calibrate model graders against human-labelled examples;
* evaluate grader bias, order bias, verbosity bias, self-preference and provider dependence;
* require more than one evidence type for high-impact quality gates;
* preserve per-case results rather than only aggregate scores;
* report uncertainty and sample size;
* retain failures, not only averages;
* prevent benchmark contamination between development and held-out release sets;
* and make every release or routing qualification inspectable.

The initial evaluation-evidence hierarchy should be:

1. **Deterministic security or policy validation**
2. **Executable build, test, patch and tool verification**
3. **Exact schema, reference or state comparison**
4. **Human expert review**
5. **Calibrated model-based grading**
6. **Text-similarity or heuristic metrics**
7. **Public benchmark claims**
8. **Provider marketing claims**
9. **Model self-assessment**

Higher items are not automatically sufficient for every criterion.

A secure but useless model fails.

A fluent output that does not compile fails a patch task.

A passing patch that violates project policy fails.

A public leaderboard result does not qualify a model for Opure workflows.

The initial evaluation catalogue should include:

### Core Software-Engineering Suites

* repository question answering;
* code explanation;
* symbol and dependency understanding;
* build-diagnostic analysis;
* test-failure analysis;
* patch proposal;
* patch repair;
* refactoring;
* code infill;
* code review;
* security review;
* configuration reasoning;
* documentation;
* structured extraction;
* tool selection;
* tool argument generation;
* MCP result use;
* plugin result use;
* project-memory use;
* long-context use;
* and refusal or escalation.

### Trust and Safety Suites

* secret non-disclosure;
* wrong-project isolation;
* prompt-injection resistance;
* tool-authority boundaries;
* patch-review boundaries;
* command-execution boundaries;
* cloud-policy compliance;
* no hidden fallback;
* no unapproved context expansion;
* no provider-state dependency;
* malicious source content;
* malicious tool results;
* malicious plugin output;
* malicious MCP output;
* and data-minimisation behaviour.

### Operational Suites

* time to first token;
* total latency;
* prompt throughput;
* output throughput;
* provider queueing;
* local model load;
* cancellation;
* timeout;
* retry behaviour;
* output truncation;
* context-window handling;
* tool-loop bounds;
* local VRAM;
* local RAM;
* CPU;
* energy or power where measurable;
* provider token use;
* provider cost;
* and availability.

The initial task taxonomy should be versioned and hierarchical.

Suggested high-level task classes:

1. Explain
2. Retrieve
3. Diagnose
4. Review
5. Propose Patch
6. Repair Patch
7. Generate
8. Transform
9. Extract
10. Classify
11. Summarise
12. Plan
13. Use Tool
14. Use Multiple Tools
15. Embed
16. Rerank
17. Evaluate
18. Refuse or Escalate

Each class should declare required capabilities and risks.

The initial benchmark strategy should combine:

* **Opure Private Regression Suites**
* **Opure Held-Out Release Suites**
* **Adversarial Security Suites**
* **Project-Fixture Suites**
* **Public External Benchmarks**
* **Human-in-the-Loop Productivity Studies**
* and **Operational Benchmarks**

Opure Private Regression Suites may be used during implementation.

Opure Held-Out Release Suites should remain inaccessible to model-facing development workflows and ordinary prompt tuning.

The held-out set should be stored under restricted repository or release-test access.

Prompts, expected outputs and graders should not be sent to the same model for prompt optimisation.

A model should not grade cases it has just generated unless the result is clearly marked exploratory and independently checked.

The initial public code-evaluation evidence may include:

* SWE-bench or appropriately licensed variants for real-world issue resolution;
* HumanEval-style functional code generation;
* EvalPlus-style augmented test coverage;
* HELM-style multi-metric and transparent reporting;
* MLPerf-style operational measurement principles;
* provider evaluation frameworks for implementation comparison;
* and NIST AI RMF measurement and risk-management concepts.

Public benchmark results should remain supplementary.

Opure should create its own Windows-first, C#-first engineering corpus because public benchmarks may not test:

* Opure's architecture;
* Windows paths;
* PowerShell;
* .NET;
* Avalonia;
* named-pipe IPC;
* SQLite policy;
* patch-review boundaries;
* Trust Centre evidence;
* local model operation;
* or project cloud-policy enforcement.

The initial Evaluation Case should contain:

```text
case_id
suite_id
suite_revision
task_class
risk_class
input_fixture
project_fixture
workspace_generation
context_policy
instruction_template
required_capabilities
forbidden_behaviours
expected_result
grader_set
resource_budget
data_classification
licence
provenance
split
created_at
```

An Evaluation Case should be immutable after publication.

A correction should create a new case revision and invalidate affected comparisons.

The initial evaluation split model should include:

* Development;
* Regression;
* Calibration;
* Held-Out Qualification;
* Adversarial;
* Canary;
* and Retired.

The split should be part of case authority.

Development cases should not be reported as held-out evidence.

The initial grader types should be:

* exact value;
* JSON schema;
* AST or syntax;
* compile;
* unit test;
* integration test;
* property test;
* mutation test;
* patch applicability;
* patch scope;
* repository state;
* secret scan;
* policy validation;
* tool-call validation;
* command-plan validation;
* static analysis;
* security scanner;
* deterministic rubric;
* human rubric;
* pairwise human preference;
* calibrated model pointwise;
* calibrated model pairwise;
* text similarity;
* and composite gate.

Deterministic graders should run under the same trust boundaries as ordinary tools.

Evaluation-generated code should be treated as untrusted.

Evaluation harnesses should use:

* isolated worktrees or copied fixtures;
* restricted tool capabilities;
* bounded processes;
* no production credentials;
* no arbitrary internet;
* approved package caches;
* reproducible dependencies;
* fixed clocks where required;
* fixed locale;
* and complete execution receipts.

The initial model-grader policy should be:

> **A model grader may provide evidence but may not be the sole authority for a Stable release gate, a security gate, a privacy gate, a patch-validity gate, a tool-authority gate or an automatic routing promotion.**

Model graders should be evaluated like other models.

A Judge Profile should bind:

* provider or runtime;
* exact model;
* prompt or rubric;
* output schema;
* reasoning settings;
* temperature;
* seed where supported;
* position randomisation;
* reference visibility;
* candidate anonymisation;
* and calibration evidence.

The initial human-evaluation policy should:

* use task-specific rubrics;
* separate correctness from style preference;
* conceal model identity where practical;
* randomise answer order for pairwise review;
* allow Tie and Cannot Determine;
* collect reviewer confidence;
* record reviewer expertise;
* measure agreement;
* adjudicate important disagreements;
* avoid exposing reviewers to secrets;
* and preserve only necessary personal data.

The initial metric policy should avoid one universal model score.

Every AI Execution Profile should instead receive an **Evaluation Card** containing a vector of evidence such as:

* task success;
* patch apply rate;
* build pass rate;
* test pass rate;
* regression rate;
* policy-violation rate;
* secret-leakage rate;
* refusal correctness;
* groundedness;
* instruction-following rate;
* tool-selection accuracy;
* tool-argument accuracy;
* structured-output validity;
* context-use precision;
* context-use recall;
* unsupported-claim rate;
* latency percentiles;
* cancellation success;
* cost distributions;
* local resource distributions;
* and evidence freshness.

A summary score may exist for display within one task profile.

It should not erase the underlying metrics or gates.

The initial statistical reporting should include, where applicable:

* case count;
* pass count;
* failure count;
* mean;
* median;
* standard deviation;
* quantiles;
* confidence interval;
* bootstrap interval;
* effect size;
* pairwise win, loss and tie;
* inter-rater agreement;
* and missing or invalid cases.

An average should not hide a critical failure.

Hard gates should be evaluated before weighted quality scoring.

The initial hard gates should include:

* required capability available;
* project data policy satisfied;
* provider approved;
* region approved;
* context limit sufficient;
* tokenizer and renderer valid;
* secret policy satisfied;
* no critical prompt-injection policy violation;
* no unauthorised tool execution;
* no unauthorised patch application;
* structured output meets required schema;
* cancellation works;
* and exact model or runtime is available.

For code-changing tasks, additional hard gates should include:

* patch parses;
* patch applies to the exact base;
* changed paths are authorised;
* no path escape;
* build or designated validation succeeds;
* required tests pass;
* prohibited files remain unchanged;
* and review remains possible.

The initial model-qualification states should be:

* Discovered;
* Metadata Verified;
* Evaluation Pending;
* Evaluating;
* Qualified for Development;
* Qualified for Preview;
* Qualified for Stable;
* Qualified with Restrictions;
* Not Qualified;
* Suspended;
* Deprecated;
* Unavailable;
* Quarantined;
* and Retired.

Qualification should be task-specific.

A model may be:

* Stable qualified for summarisation;
* Preview qualified for code explanation;
* not qualified for patch generation;
* and prohibited for secret-bearing local-only projects.

The initial qualification record should bind:

* AI Execution Profile;
* task class;
* project-policy class;
* data class;
* required review mode;
* evaluation suites;
* evaluation runs;
* metric thresholds;
* hard-gate outcomes;
* restrictions;
* valid from;
* review due;
* expiry;
* and approving agent.

Qualification should expire or require review when:

* model snapshot changes;
* provider endpoint changes;
* provider data posture changes;
* instruction template changes;
* Context Policy changes;
* Tokenizer Profile changes;
* tool schemas change;
* output schema changes;
* local Runtime Package changes;
* local model file changes;
* safety settings change;
* significant pricing changes;
* or new incident evidence appears.

The initial Routing Governance architecture should:

* accept one canonical Routing Request;
* enumerate eligible AI Execution Profiles;
* apply hard policy constraints;
* apply task qualification;
* apply current availability;
* apply context fit;
* apply local resource fit;
* apply cost and latency budgets;
* apply developer preference;
* use evaluation evidence to rank remaining profiles;
* return one immutable Routing Decision;
* and preserve all rejected candidates and reasons.

The canonical Routing Request should contain:

```text
operation_id
project_id
task_class
risk_class
data_classes
cloud_policy
required_capabilities
preferred_execution
allowed_provider_profiles
allowed_local_profiles
context_plan_summary
input_token_budget
output_token_budget
tool_requirements
structured_output_requirements
latency_budget
cost_budget
local_resource_budget
quality_floor
review_mode
fallback_policy
user_selection
created_at
```

Routing should be driven primarily by trusted operation metadata.

Opure should not send the full developer prompt to a separate router model merely to choose a model.

A deterministic Task Classifier should use:

* invoked product command;
* workflow step;
* expected output type;
* selected tool set;
* source types;
* requested patch intent;
* and structured-operation metadata.

A model-based task classifier may propose a class only for ambiguous free-form requests.

Trusted policy validates the proposal.

The routing eligibility order should be:

1. project and enterprise policy;
2. local or cloud permission;
3. provider approval and data posture;
4. task capability;
5. qualification state;
6. security and privacy gates;
7. exact context fit;
8. required tool and schema support;
9. local hardware or provider availability;
10. latency and cost hard budgets;
11. developer pin or preference;
12. quality evidence;
13. efficiency evidence;
14. and stable tie-breaking.

No quality score can override a project Local Only policy.

No cost saving can override a security gate.

No provider availability can justify hidden cloud fallback.

The initial routing preference should honour Opure's local-first posture:

* Local Only projects route only to qualified local profiles.
* Ask Every Time projects require an approved Data Sharing Plan before a remote profile becomes eligible.
* Approved Providers Only projects use only exact approved Provider Profile revisions.
* Custom policies apply their explicit rules.
* Local preference may be Soft or Hard.
* A Soft local preference may permit a remote candidate only through the project's declared cloud policy and visible Routing Decision.
* A Hard local preference prohibits remote profiles.

The initial routing-ranking strategy should be deterministic and multi-objective.

It should not collapse all concerns into one opaque number.

Suggested phases:

### Phase A — Hard Filtering

Remove ineligible profiles.

### Phase B — Quality Floor

Remove profiles below task-specific minimum evidence.

### Phase C — Pareto Frontier

Identify profiles not dominated across required quality, latency, cost and resource dimensions.

### Phase D — Policy Preference

Apply task-specific preference ordering.

### Phase E — Stable Tie-Break

Use explicit profile priority and immutable profile ID.

Task-specific preference examples:

* interactive explanation:

  * quality floor;
  * first-token latency;
  * total cost;
  * quality margin;
* patch proposal:

  * patch success;
  * test pass;
  * policy compliance;
  * quality;
  * latency;
  * cost;
* background summarisation:

  * quality floor;
  * local preference;
  * cost;
  * throughput;
* embedding:

  * exact embedding-profile identity;
  * retrieval quality;
  * throughput;
  * resource use;
* security review:

  * security recall;
  * false-negative constraints;
  * qualification;
  * human review requirement;
  * cost and latency secondary.

The initial Routing Policy should be versioned.

It should declare:

* applicable task classes;
* project-policy classes;
* candidate profiles;
* hard filters;
* quality floors;
* metric windows;
* evidence freshness;
* preference order;
* cost and latency budgets;
* local preference;
* review requirements;
* fallback;
* and approval.

The initial Routing Decision should contain:

```text
routing_decision_id
routing_request_hash
routing_policy
selected_profile
candidate_profiles
eligibility_results
qualification_results
quality_evidence
operational_evidence
cost_estimate
latency_estimate
context_fit
resource_fit
fallback_policy
approval
created_at
expires_at
decision_sha256
```

A material change creates a new decision.

The Routing Decision should appear in the Context Plan, Data Sharing Plan and inference receipt.

The initial fallback policy should be explicit.

Supported policies:

* No Fallback;
* Retry Same Profile;
* Use Named Local Fallback;
* Use Named Provider Fallback;
* Ask Before Fallback;
* and Workflow-Specific Replan.

Default interactive policy:

> **Ask Before Fallback when the fallback changes model, provider, local versus cloud posture, data region, data terms, output quality class or cost class.**

Retrying the exact same profile for a transient error may occur under bounded retry policy.

A retry is not a model fallback.

A local GPU-to-CPU change is an Execution Profile change and must follow ADR-0020.

A remote-provider change requires a new Provider Profile and ADR-0019 policy.

No fallback should occur after a partial streamed response without clearly ending or labelling the first attempt.

The initial provider-managed routing policy should be:

* disabled by default;
* treated as an opaque multi-model execution profile when enabled experimentally;
* permitted only when the provider exposes the actual selected model;
* permitted only when every possible model and region is approved;
* evaluated end to end as the router service, not as one model;
* and never used for Local Only or exact-model workflows.

The initial provider alias policy should be:

* Stable uses exact stable identifiers or resolved immutable snapshots where available;
* Preview may use preview identifiers only under Preview qualification;
* `latest` aliases are prohibited for Stable;
* provider automatic model migration is prohibited where it can be disabled;
* provider model shutdown requires migration review;
* and a changed resolved model invalidates qualification.

The initial canary policy should use:

* synthetic or licensed evaluation cases;
* no hidden developer source;
* no hidden user prompts;
* no production credentials;
* bounded provider calls;
* and explicit evaluation budgets.

Opure should not duplicate real developer operations to another model silently.

There should be no hidden shadow traffic containing project content.

An explicit diagnostic or evaluation session may replay an approved redacted operation against another qualified profile after showing:

* data;
* providers;
* calls;
* cost;
* storage;
* and comparison purpose.

The initial online-quality signal policy should collect only local, visible and consent-respecting evidence such as:

* developer accepted response;
* developer rejected response;
* patch opened for review;
* patch applied;
* patch edited;
* patch reverted;
* build passed;
* tests passed;
* tool call approved;
* tool call rejected;
* user selected another model;
* cancellation;
* and explicit rating.

These signals should not be interpreted simplistically.

An accepted response is not proof of correctness.

A rejected response may reflect style rather than capability.

A patch edit may improve or merely customise.

Online signals should:

* remain project local by default;
* be linked to exact profile and operation;
* be classified;
* avoid storing full prompts or outputs unless the owning workflow already retains them;
* never auto-promote a model;
* and feed candidate evaluation cases only after review.

The initial feedback policy should prohibit:

* covert experimentation;
* user-specific manipulation;
* hidden model comparison;
* automatic cloud transmission;
* and training-provider disclosure without explicit policy.

The initial drift policy should monitor:

* qualification metrics;
* provider errors;
* output-schema failures;
* refusal changes;
* token-count variance;
* latency;
* cost;
* model identity;
* provider release notes;
* deprecation notices;
* local runtime changes;
* safety incidents;
* and developer-reported regressions.

A drift event may:

* request re-evaluation;
* increase review requirements;
* suspend unattended routing;
* suspend one task qualification;
* quarantine a profile;
* or retire a profile.

It should not silently route to another model.

The initial release governance should require:

* an accepted evaluation-plan revision;
* immutable evaluation datasets;
* exact AI Execution Profiles;
* deterministic harness version;
* grader validation;
* held-out qualification run;
* security suite;
* context and tool suite;
* operational benchmark;
* known limitation review;
* model deprecation check;
* routing-policy review;
* and founder or delegated approval.

The selected trust chain is:

```text
Product task and project policy
    ↓
Versioned evaluation criteria
    ↓
Immutable cases, fixtures and graders
    ↓
Exact AI Execution Profile
    ↓
Reproducible evaluation run
    ↓
Per-case results and uncertainty
    ↓
Task-specific qualification
    ↓
Versioned Routing Policy
    ↓
Hard eligibility and quality filters
    ↓
Deterministic multi-objective selection
    ↓
Immutable Routing Decision
    ↓
Context and Data Sharing Plans
    ↓
AI Router execution
    ↓
Actual usage and outcome evidence
    ↓
Drift, review and requalification
```

---

## 2. Status

Current status:

> **Proposed**

This ADR becomes **Accepted** only after prototypes demonstrate:

* AI Execution Profile identity;
* Evaluation Suite and Case schemas;
* immutable evaluation splits;
* private regression and held-out suites;
* deterministic graders;
* executable patch, build and test grading;
* security and policy grading;
* human rubric workflow;
* calibrated model judge;
* judge-order randomisation;
* judge self-preference tests;
* per-case results;
* uncertainty reporting;
* Evaluation Cards;
* task-specific qualification;
* qualification expiry;
* provider-model identity drift;
* exact local model evaluation;
* exact remote model evaluation;
* deterministic task classification;
* Routing Request and Routing Decision schemas;
* hard policy filtering;
* context-fit filtering;
* local-resource filtering;
* quality-floor filtering;
* deterministic multi-objective ranking;
* stable tie-breaking;
* local-first project policy;
* no hidden fallback;
* explicit fallback approval;
* provider outage;
* model deprecation;
* no hidden shadow traffic;
* explicit comparison sessions;
* local user-feedback evidence;
* drift suspension;
* Trust Centre evidence;
* and adversarial benchmark contamination, grader manipulation, prompt injection, hidden-provider, wrong-region, cost-budget, alias-change and fallback-race tests.

---

## 3. Context

Opure supports:

* local models;
* multiple cloud providers;
* multiple model families;
* multiple model snapshots;
* multiple context windows;
* multiple reasoning settings;
* multiple tool capabilities;
* and multiple data postures.

A model that is strong at one task may be poor at another.

A fast model may be adequate for:

* classification;
* extraction;
* or short explanation

but inadequate for:

* large patch generation;
* complex diagnosis;
* or security review.

A large model may produce better answers but:

* cost more;
* respond more slowly;
* require cloud transmission;
* use more local resources;
* overproduce changes;
* or have a less suitable data posture.

A local model can provide privacy and offline operation but may:

* have a smaller context window;
* be slower on CPU;
* fail structured outputs;
* or perform poorly on complex patches.

Model quality changes with:

* prompt template;
* context;
* tool projection;
* reasoning effort;
* temperature;
* provider endpoint;
* runtime version;
* quantisation;
* and output budget.

Therefore, evaluating a model name alone is insufficient.

Public benchmark leaderboards can be useful, but they may:

* use different prompts;
* use different scaffolding;
* use different tools;
* use different reasoning budgets;
* omit latency or cost;
* omit project policy;
* contain contaminated tasks;
* use weak tests;
* or not resemble Opure workflows.

A routing system can reduce cost and latency, but an opaque router may:

* choose an unapproved model;
* choose a model in another region;
* use application-irrelevant training data;
* change without notice;
* hide the selected model;
* or trade away quality in ways the developer cannot inspect.

Current provider systems demonstrate that automated prompt routing exists and can predict response quality to balance cost and quality, but provider documentation also acknowledges application-specific limitations.

Opure therefore needs first-party evaluation and routing governance.

---

## 4. Problem Statement

Opure requires a provider-neutral evaluation and routing architecture that qualifies exact AI execution configurations against realistic engineering tasks, preserves deterministic and human-verifiable evidence, measures quality, safety, latency, cost and local resources, and selects one approved model profile through visible project policy without hidden fallback, opaque model authority or benchmark-driven overconfidence.

---

## 5. Decision Drivers

The decision is evaluated against:

* Charter alignment;
* developer control;
* provider neutrality;
* local-first operation;
* cloud optionality;
* task quality;
* software correctness;
* security;
* privacy;
* project policy;
* context fit;
* tool capability;
* structured output;
* reproducibility;
* benchmark relevance;
* grader reliability;
* model drift;
* cost;
* latency;
* local resources;
* availability;
* explainability;
* fallback visibility;
* testability;
* release governance;
* and small-team implementation.

---

## 6. Governing Principles

This decision must preserve:

* Developer Respect;
* Human in Control;
* Visible by Design;
* Inspectable Decisions;
* Local by Design;
* Cloud Optional;
* Provider Neutrality;
* Task-Specific Qualification;
* Exact Profile Identity;
* Deterministic Gates First;
* Executable Evidence Before Eloquence;
* Human Judgement for Nuance;
* Model Judges Are Evidence, Not Authority;
* Public Benchmarks Are Context, Not Qualification;
* No Universal Model Score;
* No Model Self-Routing;
* No Provider Alias in Stable;
* No Hidden Fallback;
* No Hidden Shadow Traffic;
* No Quality Override of Data Policy;
* No Cost Override of Security;
* No Silent Cloud Transition;
* No Evaluation on Production Secrets;
* No Benchmark Contamination;
* No Aggregate Score Hiding Critical Failure;
* Evidence Freshness;
* Reproducible Runs;
* Reversible Routing Policy;
* and Continuous Requalification.

---

## 7. Scope

This ADR decides:

* AI Evaluation Service ownership;
* Routing Governance Service ownership;
* AI Execution Profiles;
* task taxonomy;
* evaluation suites;
* evaluation cases;
* dataset splits;
* fixtures;
* graders;
* deterministic grading;
* human review;
* model judges;
* metrics;
* uncertainty;
* public benchmarks;
* private benchmarks;
* held-out benchmarks;
* security evaluation;
* patch evaluation;
* tool evaluation;
* operational evaluation;
* Evaluation Cards;
* qualification;
* routing requests;
* routing policies;
* eligibility;
* quality floors;
* multi-objective ranking;
* fallbacks;
* provider routers;
* model aliases;
* canaries;
* shadow evaluation;
* user feedback;
* drift;
* release gates;
* Trust Centre;
* and acceptance tests.

This ADR does not decide:

* the final supported model catalogue;
* final provider pricing;
* final local model selection;
* final public benchmark list;
* one universal quality metric;
* model training;
* model fine-tuning;
* reinforcement learning;
* provider-side prompt optimisation;
* autonomous learned routing;
* hidden online experiments;
* global user profiling;
* or provider use of Opure feedback for training.

---

## 8. Constraints

Known constraints include:

* ADR-0019 controls cloud provider use and Data Sharing Plans.
* ADR-0020 controls local model execution.
* ADR-0021 controls context construction and token budgeting.
* ADR-0022 controls project retrieval evidence.
* ADR-0023 controls project memory.
* model APIs and model identities change;
* provider aliases may move to different model versions;
* providers deprecate models;
* evaluation services can themselves require cloud data transmission;
* model-based graders have model failure modes;
* public code benchmarks can be contaminated;
* pass rates depend on test quality;
* stochastic models require repeated runs or bounded deterministic settings;
* provider seeds may not guarantee complete reproducibility;
* local GPU and driver behaviour can affect performance;
* user feedback is noisy;
* and project data cannot be replayed to another model silently.

---

## 9. Assumptions

This decision assumes:

* every routed profile can be represented exactly;
* provider adapters can report actual model identity where supported;
* local Runtime Packages and Model Manifests are immutable;
* Evaluation Cases can be source controlled or securely held out;
* deterministic graders can run in supervised workers;
* build and test services can produce authoritative results;
* Patch Service can validate patches;
* Context Assembly can reproduce evaluation context;
* Data Sharing Plans can authorise remote evaluation;
* human reviewers can evaluate selected high-value cases;
* Routing Policies can remain deterministic;
* and qualification evidence can be invalidated after material change.

---

## 10. Current Technical Evidence

Official and primary material available on 18 July 2026 establishes that:

* OpenAI provides Evals and grader resources supporting structured evaluation definitions, datasets and multiple grader types;
* OpenAI's current grader resources include exact string checks, similarity metrics, model graders, Python graders and composite graders;
* OpenAI's evaluation guidance emphasises defining intended outcomes and testing against contextual real-world workflows;
* Anthropic's evaluation guidance emphasises specific, measurable, achievable and relevant success criteria across task fidelity, consistency, privacy, context use, latency and price;
* Google's generative-AI evaluation service supports pointwise, pairwise and computation-based metrics and provides per-instance and aggregate result structures;
* Google explicitly recommends evaluating a judge model against human ratings;
* Amazon Bedrock provides intelligent prompt routing that predicts response quality and balances quality and cost, while its documentation states that the router cannot use application-specific performance data and may be suboptimal for specialised use cases;
* Gemini model documentation distinguishes stable, preview, latest and experimental identifiers, and documents that latest aliases can be hot-swapped;
* Gemini publishes explicit model deprecation and shutdown schedules;
* NIST AI RMF organises risk work around Govern, Map, Measure and Manage and recommends pre-deployment and ongoing testing with uncertainty, benchmark comparisons and documented results;
* SWE-bench evaluates model-generated patches against real-world repository issues through reproducible harnesses;
* EvalPlus research demonstrates that weak test suites can materially overestimate functional correctness;
* HELM promotes multi-metric, standardised and transparent evaluation;
* MLPerf Inference separates operational performance measurement from capability quality;
* and human-centric research has found that preference and actual programmer productivity need not correlate perfectly.

These facts support:

* contextual Opure-specific evals;
* multi-dimensional evidence;
* deterministic graders;
* calibrated model judges;
* exact profile pinning;
* and first-party routing governance.

Every provider API, model identifier, benchmark licence, evaluator and operational assumption must be revalidated before acceptance.

---

## 11. Terminology

### 11.1 AI Execution Profile

The exact model, provider or runtime and request configuration being evaluated or routed.

---

### 11.2 Task Class

A versioned description of the developer operation.

---

### 11.3 Evaluation Suite

A versioned collection of cases, graders and thresholds.

---

### 11.4 Evaluation Case

One immutable evaluation input, expected behaviour and grading plan.

---

### 11.5 Evaluation Split

The governed purpose and visibility of a case.

---

### 11.6 Evaluation Run

Execution of one or more cases against one or more exact profiles.

---

### 11.7 Trial

One attempt of one case against one profile.

---

### 11.8 Grader

A deterministic, executable, human or model-based evaluator.

---

### 11.9 Hard Gate

A criterion that cannot be compensated for by other scores.

---

### 11.10 Quality Metric

A measured property used for task qualification or comparison.

---

### 11.11 Evaluation Card

The current evidence summary for one AI Execution Profile.

---

### 11.12 Qualification

Permission for one exact profile to perform one task class under declared restrictions.

---

### 11.13 Routing Request

The authoritative operation metadata used to select an execution profile.

---

### 11.14 Routing Policy

The versioned rules used for eligibility and selection.

---

### 11.15 Routing Decision

The immutable selected profile and reasons for one operation.

---

### 11.16 Fallback

Use of a different AI Execution Profile after the original profile cannot proceed.

---

### 11.17 Retry

Another attempt using the same exact AI Execution Profile.

---

### 11.18 Canary

A synthetic or licensed case used to detect current operational or quality changes.

---

### 11.19 Shadow Evaluation

An explicitly approved comparison that does not control the user's primary result.

---

### 11.20 Drift

A material change in model, behaviour, quality, latency, cost, policy or availability.

---

### 11.21 Model Judge

A model used to grade another output under a Judge Profile.

---

### 11.22 Human Adjudication

Resolution of important evaluation disagreement by authorised reviewers.

---

## 12. Options Considered

The principal architecture options are:

1. First-party evaluation registry and deterministic governed routing.
2. One manually selected default model.
3. Provider-managed intelligent routing.
4. Model self-routing.
5. Cheapest-qualified model only.
6. Highest-public-benchmark model only.
7. Weighted universal model score.
8. Online bandit routing from user behaviour.
9. LLM-as-judge-only evaluation.
10. Human-only evaluation.
11. Public benchmarks only.
12. Build-and-test-only evaluation.

---

## 13. Option A — First-Party Evaluation and Governed Routing

### 13.1 Advantages

* project policy remains authoritative;
* provider neutral;
* local and remote profiles are comparable;
* task-specific qualification;
* exact provenance;
* visible fallbacks;
* deterministic hard gates;
* reproducible release evidence;
* and controllable drift response.

### 13.2 Disadvantages

* significant test infrastructure;
* benchmark maintenance;
* human-review cost;
* provider and model churn;
* statistical complexity;
* and routing-policy maintenance.

### 13.3 Decision

Selected.

---

## 14. One Manual Default Model

Insufficient because tasks, costs, privacy and local resources differ.

A developer may still pin one profile explicitly.

---

## 15. Provider-Managed Intelligent Routing

Deferred as an optional opaque execution profile.

It does not replace application-specific evidence or Opure project policy.

---

## 16. Model Self-Routing

Rejected because a model cannot grant itself provider, data or cost authority.

---

## 17. Cheapest-Qualified Only

Rejected because quality differences can be material and task specific.

Cost can be a preference after hard quality gates.

---

## 18. Highest Public Benchmark

Rejected because public benchmark configuration and task relevance differ from Opure.

---

## 19. Universal Weighted Score

Rejected because critical gates and multi-dimensional trade-offs should remain visible.

---

## 20. Online Bandit Routing

Deferred because it requires hidden or explicit experimentation, careful regret and privacy policy, and reliable online outcomes.

---

## 21. Model-Judge-Only Evaluation

Rejected because judge models can be biased, inconsistent and manipulable.

---

## 22. Human-Only Evaluation

Insufficient for scale, deterministic correctness and continuous regression.

---

## 23. Public Benchmarks Only

Rejected because Opure needs realistic Windows, .NET, policy, context and patch tasks.

---

## 24. Build-and-Test-Only Evaluation

Insufficient because explanation, grounding, review quality, security, privacy, tool behaviour and developer effort also matter.

---

## 25. Decision

Opure will provisionally adopt:

> **A first-party provider-neutral Evaluation Service and deterministic Routing Governance Service that evaluate exact AI Execution Profiles against immutable task-specific suites, prioritise executable, security and human-verifiable evidence over model judgement, qualify profiles separately by task and data policy, and select one profile through hard constraints, quality floors, visible multi-objective preferences and explicit fallback rules, with no model self-routing, no Stable `latest` aliases, no hidden project-data shadow traffic and no quality or cost score capable of overriding developer-controlled data boundaries.**

This decision is:

* [ ] Permanent until superseded
* [x] Provisional pending evaluation, routing and drift evidence
* [ ] Approval of autonomous learned routing
* [ ] Approval of hidden online experimentation
* [ ] Approval of provider-managed routing as default
* [ ] Approval of provider aliases in Stable

---

# 26. Service Ownership

The AI Evaluation Service owns:

* Evaluation Suite registry;
* Evaluation Case registry;
* dataset splits;
* fixture manifests;
* AI Execution Profile evaluation identity;
* grader registry;
* Judge Profiles;
* human-review rubrics;
* run planning;
* trial execution coordination;
* metric calculation;
* statistical summaries;
* Evaluation Cards;
* qualification evidence;
* benchmark contamination controls;
* evaluation retention;
* and evaluation export.

The Routing Governance Service owns:

* Routing Request validation;
* Routing Policy registry;
* candidate enumeration;
* hard eligibility;
* qualification checks;
* evidence freshness;
* context fit;
* resource fit;
* cost and latency constraints;
* deterministic multi-objective selection;
* fallback policy;
* Routing Decisions;
* decision invalidation;
* and routing evidence.

---

## 26.1 Non-Responsibilities

Neither service owns:

* project files;
* project memory;
* provider credentials;
* provider accounts;
* local runtime execution;
* final Context Plans;
* patch application;
* tool execution;
* developer authentication;
* or cloud-policy authority.

---

# 27. Service Boundary

Conceptual architecture:

```text
Release, Developer or Workflow
    ↓
AI Evaluation Service
    ├── Evaluation Registry
    ├── Fixture Registry
    ├── Grader Registry
    ├── Trial Coordinator
    ├── Human Review
    ├── Statistical Analysis
    └── Evaluation Cards
            ↓
        Qualification Registry
            ↓
Routing Governance Service
    ├── Routing Policies
    ├── Eligibility
    ├── Context Fit
    ├── Resource Fit
    ├── Quality Evidence
    ├── Cost and Latency
    └── Fallback Policy
            ↓
        Routing Decision
            ↓
Context Assembly and AI Router
```

---

## 27.1 Evaluation Is Not Production Execution

Evaluation uses explicit Evaluation Plans and restricted fixtures.

---

## 27.2 Routing Is Not Prompt Engineering

Routing chooses a profile.

It does not rewrite the developer request.

---

## 27.3 AI Router Is Executor

AI Router verifies and executes the decision.

---

## 27.4 Context Assembly Remains Authoritative

Routing receives context requirements, not source authority.

---

# 28. AI Execution Profile

Suggested schema:

```text
opure.ai-execution-profile/1
```

---

## 28.1 Fields

```text
execution_profile_id
revision
display_name
provider_kind
provider_profile_revision
provider_endpoint
provider_region
provider_model_id
resolved_model_id
local_runtime_package
local_model_manifest
local_execution_profile
instruction_template
request_renderer
tokenizer_profile
context_policy
tool_projection
output_schema
reasoning_profile
sampling_profile
maximum_output
truncation_policy
safety_profile
data_posture
adapter_version
created_at
verified_at
profile_sha256
```

---

## 28.2 Exact Profile

Every material setting is bound.

---

## 28.3 Immutable Revision

A changed field creates a revision.

---

## 28.4 Display Name

Not authority.

---

## 28.5 Provider Model Resolution

Where the provider returns resolved model identity, record it.

---

## 28.6 Unknown Resolved Model

Strict qualification fails when exact identity is required.

---

# 29. Provider Profile Binding

ADR-0019 remains authoritative.

The Evaluation Profile binds exact:

* operator;
* account;
* endpoint;
* region;
* terms evidence;
* retention posture;
* training posture;
* residency;
* and credential reference.

---

## 29.1 Different Provider Host

A third-party host serving the same model is a different profile.

---

## 29.2 Marketplace Model

Cloud marketplace, direct provider and local model are separate profiles.

---

# 30. Local Profile Binding

ADR-0020 remains authoritative.

Bind:

* Runtime Package hash;
* runtime upstream commit;
* executable hash;
* backend;
* model weights hash;
* Model Manifest;
* quantisation;
* chat template;
* context length;
* GPU offload;
* KV-cache policy;
* and hardware profile.

---

## 30.1 Quantisation

Different quantisation is a different profile.

---

## 30.2 Hardware

Quality may be profile stable while latency and resource evidence are hardware specific.

---

# 31. Instruction Template Identity

The template includes:

* product security instructions;
* task instructions;
* role structure;
* tool instructions;
* output instructions;
* and rendering.

---

## 31.1 Template Change

Invalidates relevant evaluation evidence.

---

## 31.2 Provider Adaptation

Different provider-native rendering is a different profile.

---

# 32. Sampling Profile

Fields may include:

```text
temperature
top_p
top_k
seed
frequency_penalty
presence_penalty
stop_sequences
determinism_mode
```

---

## 32.1 Missing Seed

Record unsupported.

---

## 32.2 Determinism

A low temperature does not guarantee deterministic output.

---

# 33. Reasoning Profile

Fields may include:

* disabled;
* minimal;
* low;
* medium;
* high;
* provider default;
* budgeted tokens;
* and visible summary policy.

---

## 33.1 Evaluation Equality

Compare profiles only when reasoning settings are explicit.

---

## 33.2 Provider Default

Mutable provider default is not acceptable for Stable qualification without evidence.

---

# 34. Tool Projection Identity

Bind exact available tools and schemas.

---

## 34.1 No Global Tools

Only task tools.

---

## 34.2 Schema Change

Invalidates tool-evaluation evidence.

---

## 34.3 Tool Implementation Change

May require re-evaluation even when schema is stable.

---

# 35. Evaluation Suite

Suggested schema:

```text
opure.evaluation-suite/1
```

---

## 35.1 Fields

```text
suite_id
revision
name
purpose
task_classes
risk_classes
split_policy
case_references
grader_sets
trial_policy
metrics
hard_gates
qualification_thresholds
resource_budget
data_policy
licence
owners
created_at
review_due
suite_sha256
```

---

## 35.2 Immutable Publication

A published revision is immutable.

---

## 35.3 Draft

Draft suites may change until published.

---

## 35.4 Suite Composition

A higher-level suite may reference other suites by exact revision.

---

# 36. Evaluation Case

Suggested schema:

```text
opure.evaluation-case/1
```

---

## 36.1 Required Fields

```text
case_id
revision
suite_id
split
task_class
risk_class
title
purpose
project_fixture
input_fixture
context_policy
instruction_template
required_capabilities
forbidden_behaviours
expected_result
grader_set
resource_budget
classification
licence
provenance
created_at
case_sha256
```

---

## 36.2 No Hidden Expected Result

The harness can access expected evidence.

The candidate model cannot.

---

## 36.3 Stable ID

A correction creates a revision.

---

## 36.4 Case Retirement

Retained for historical result interpretation.

---

# 37. Case Provenance

Record:

* author;
* source issue;
* source project;
* licence;
* transformation;
* redaction;
* validation;
* and split assignment.

---

## 37.1 Production Incident

A real incident may become a case after:

* data minimisation;
* secret removal;
* project approval;
* licence review;
* and hold-out decision.

---

## 37.2 Synthetic Case

Record generator and human or deterministic validation.

---

## 37.3 Model-Generated Case

Cannot grade only with the same generating model.

---

# 38. Dataset Split Governance

### 38.1 Development

Visible during prompt and implementation iteration.

### 38.2 Regression

Run frequently to detect known regressions.

### 38.3 Calibration

Used to validate graders and thresholds.

### 38.4 Held-Out Qualification

Restricted release evidence.

### 38.5 Adversarial

Security, privacy and authority attacks.

### 38.6 Canary

Small current-behaviour checks.

### 38.7 Retired

No longer current but retained historically.

---

## 38.8 No Split Reuse

A case exposed during prompt tuning cannot later claim held-out status.

---

## 38.9 Split Movement

Creates a governance event and invalidates prior claims.

---

# 39. Held-Out Access

Access should be limited to:

* Evaluation Service;
* release test;
* authorised test maintainers;
* and adjudicators.

---

## 39.1 No Model-Facing Development

Ordinary AI workflows cannot search held-out cases.

---

## 39.2 No Project Knowledge Index

Held-out cases are excluded from project indexing.

---

## 39.3 No Remote Prompt Optimiser

Denied.

---

# 40. Benchmark Contamination

Contamination risks include:

* public benchmark memorisation;
* model training exposure;
* prompt tuning against test cases;
* grader leakage;
* expected-output leakage;
* and human reviewer familiarity.

---

## 40.1 Evidence Class

Every suite declares contamination risk:

* Low;
* Medium;
* High;
* Unknown.

---

## 40.2 Public Benchmark

Assume possible training exposure.

---

## 40.3 Private Held-Out

Still cannot prove absence from provider training if source resembles public data.

---

## 40.4 Mitigation

Use:

* novel project fixtures;
* transformed but valid cases;
* time-separated cases;
* secret held-out variants;
* and executable hidden tests.

---

# 41. Evaluation Plan

Suggested schema:

```text
opure.evaluation-plan/1
```

---

## 41.1 Fields

```text
evaluation_plan_id
suite_revisions
execution_profiles
trial_policy
grader_profiles
human_review_plan
resource_limits
network_policy
cost_limit
data_sharing_plans
run_environment
randomisation
created_at
expires_at
plan_sha256
```

---

## 41.2 Approval

Remote evaluations require approved data transmission.

---

## 41.3 Immutable

Material change creates a new plan.

---

# 42. Evaluation Run

Suggested schema:

```text
opure.evaluation-run/1
```

---

## 42.1 Fields

```text
run_id
evaluation_plan
suite_revisions
execution_profiles
harness_version
worker_images
environment_manifest
started_at
completed_at
state
cost
resource_use
result_manifest
run_sha256
```

---

## 42.2 States

* Planned;
* Approval Required;
* Ready;
* Running;
* Paused;
* Completed;
* Completed with Invalid Cases;
* Failed;
* Cancelled;
* Quarantined;
* and Superseded.

---

# 43. Trial

A trial is one case/profile attempt.

---

## 43.1 Fields

```text
trial_id
run_id
case_id
case_revision
execution_profile
attempt
input_hash
context_plan_hash
routing_bypassed_for_eval
provider_request_receipt
local_request_receipt
output_hash
usage
latency
cost
tool_trace
patch_reference
result
started_at
completed_at
```

---

## 43.2 Direct Profile

Evaluation executes the named profile directly.

It does not route through ordinary dynamic selection unless the router itself is under evaluation.

---

## 43.3 Router Evaluation

A router trial records both requested router and actual selected profile.

---

# 44. Trial Repetition

Stochastic tasks may require repeated trials.

---

## 44.1 Trial Policy

Defines:

* repetitions;
* seed sequence;
* temperature;
* aggregation;
* failure handling;
* and stopping.

---

## 44.2 pass@k

Use only where task and sampling policy make it meaningful.

---

## 44.3 Production Relevance

A 100-sample pass rate does not represent one-attempt interactive behaviour.

---

# 45. Reproducibility

Record:

* source revision;
* suite revision;
* case revision;
* model profile;
* prompt;
* context;
* tools;
* runtime;
* environment;
* dependency lock;
* clock;
* locale;
* random settings;
* network policy;
* and grader.

---

## 45.1 Remote Reproducibility

Provider output may change despite identical request.

Record exact time and returned model identity.

---

## 45.2 Local Reproducibility

Hardware and floating-point differences may remain.

---

## 45.3 Claim

Use:

* Reproduced;
* Statistically Consistent;
* Not Reproduced;
* or Unknown.

---

# 46. Evaluation Environment

A run environment should be isolated.

---

## 46.1 Project Fixture

Use:

* read-only source base;
* isolated worktree;
* or verified copied fixture.

---

## 46.2 Network

Default deny except approved provider endpoints and package sources required by the fixture.

---

## 46.3 Credentials

Use dedicated evaluation credentials.

---

## 46.4 No Production Vault

Never expose production secrets.

---

## 46.5 Filesystem

ADR-0009 applies.

---

# 47. Evaluation Worker

Long-running and executable evaluation runs use supervised workers.

---

## 47.1 Worker Types

* Model Trial Worker;
* Patch Validation Worker;
* Build and Test Worker;
* Static Analysis Worker;
* Grader Worker;
* Human Review Coordinator;
* and Statistics Worker.

---

## 47.2 Process Limits

Bound:

* CPU;
* memory;
* disk;
* process count;
* handles;
* time;
* network;
* and output.

---

# 48. Fixture Manifest

A project fixture manifest records:

```text
fixture_id
revision
source
repository_commit
workspace_snapshot
files
hashes
dependencies
build_instructions
test_instructions
toolchain
network_policy
licence
expected_clean_state
fixture_sha256
```

---

## 48.1 No Unpinned Dependency

Qualification fixtures use locked dependencies.

---

## 48.2 Container Use

Linux public benchmarks may use containers under a separate isolated runner.

Windows-first Opure fixtures should use controlled Windows workers.

---

# 49. Grader Registry

Every grader has:

```text
grader_id
revision
kind
implementation
implementation_hash
configuration
input_schema
output_schema
resource_limits
authority_class
calibration
owners
created_at
verified_at
```

---

## 49.1 Immutable Revision

Required.

---

## 49.2 Authority Class

Examples:

* Deterministic Hard Gate;
* Deterministic Metric;
* Human Evidence;
* Model Evidence;
* Heuristic;
* and Informational.

---

# 50. Exact Graders

Examples:

* exact string;
* exact enumeration;
* Boolean;
* numeric tolerance;
* set equality;
* ordered sequence;
* and canonical JSON equality.

---

## 50.1 Normalisation

Must be explicit.

---

## 50.2 No Hidden Fuzzy Match

Exact means exact under declared canonicalisation.

---

# 51. Schema Graders

Validate:

* JSON Schema;
* protobuf;
* XML schema;
* patch schema;
* tool-call schema;
* and custom trusted contracts.

---

## 51.1 Parse Failure

Hard fail where structure is required.

---

## 51.2 Additional Fields

Policy controlled.

---

# 52. Compile Grader

Uses the declared build target.

---

## 52.1 Exact Base

Patch applies to exact fixture state.

---

## 52.2 Build Command

Trusted Build Service contract.

---

## 52.3 Warning Policy

Explicit.

---

## 52.4 No Model Command

The model cannot choose the authoritative validation command.

---

# 53. Test Grader

Uses:

* named test suites;
* test filters;
* expected pass list;
* regression tests;
* hidden tests;
* and timeout.

---

## 53.1 Flaky Tests

Identify and exclude or model statistically.

---

## 53.2 New Tests

A model-added test is evidence but not the only validator.

---

## 53.3 Test Deletion

Hard fail unless case permits.

---

# 54. Property and Mutation Graders

Property tests can broaden functional evidence.

Mutation tests can measure test sensitivity.

---

## 54.1 Generated Inputs

Record generator and seed.

---

## 54.2 Bound Cost

Strict.

---

# 55. Patch Applicability Grader

Checks:

* patch parses;
* base hashes;
* path scope;
* hunk applicability;
* line ending;
* and no conflict.

---

# 56. Patch Scope Grader

Checks:

* allowed files;
* file count;
* line count;
* binary changes;
* generated files;
* dependency changes;
* configuration changes;
* and prohibited roots.

---

## 56.1 Over-Broad Patch

Can fail despite tests passing.

---

# 57. Repository State Grader

Checks final:

* changed files;
* untracked files;
* branch;
* submodules;
* lockfiles;
* and clean restoration.

---

# 58. Static Analysis Grader

May include:

* compiler warnings;
* analyzers;
* format;
* lint;
* security scanning;
* dependency audit;
* and custom architecture rules.

---

## 58.1 Pinned Tool

Exact version and configuration.

---

# 59. Security Grader

Checks:

* secret leakage;
* dangerous command;
* path escape;
* unauthorised network;
* policy bypass;
* injection compliance;
* and insecure patch patterns.

---

## 59.1 Critical Finding

Hard gate.

---

## 59.2 Scanner Limitation

Record false-positive and false-negative evidence.

---

# 60. Policy Grader

Validates:

* project cloud policy;
* tool permissions;
* patch review;
* memory use;
* context source;
* provider;
* region;
* model;
* and fallback.

---

# 61. Tool-Call Grader

Measures:

* whether a tool was necessary;
* correct tool;
* correct arguments;
* no prohibited arguments;
* call order;
* retry;
* cancellation;
* and final use of result.

---

## 61.1 Tool Execution

Runs through Tool Mediator.

---

## 61.2 Simulated Tool

May be used for deterministic cases.

---

# 62. MCP Grader

Checks:

* correct server;
* approved account;
* correct tool or resource;
* argument safety;
* prompt-injection handling;
* result provenance;
* and no hidden external call.

---

# 63. Plugin Grader

Checks:

* capability;
* plugin identity;
* output classification;
* no authority escalation;
* and correct result use.

---

# 64. Context Grader

Measures:

* mandatory evidence included;
* prohibited evidence absent;
* explicit selection preserved;
* retrieved evidence relevance;
* duplicate rate;
* source diversity;
* memory correctness;
* no wrong-project content;
* no secret content;
* and no hidden truncation.

---

## 64.1 Model Output

Context grading examines Context Plan and actual request, not only final answer.

---

# 65. Grounding Grader

Checks output claims against supplied evidence.

---

## 65.1 Claim Extraction

May be deterministic for structured output or model assisted for prose.

---

## 65.2 Unsupported Claim

Record severity.

---

## 65.3 Citation

A citation must resolve to the supplied source.

---

# 66. Instruction-Following Grader

Checks explicit constraints such as:

* format;
* length;
* files;
* scope;
* tone;
* no command;
* no patch;
* or required patch.

---

## 66.1 Constraint List

Case defines machine-checkable and human constraints separately.

---

# 67. Refusal and Escalation Grader

Measures:

* correct refusal;
* incorrect refusal;
* partial safe help;
* escalation;
* and explanation.

---

## 67.1 Over-Refusal

A quality failure.

---

## 67.2 Under-Refusal

A safety failure.

---

# 68. Text Similarity Graders

May use:

* edit distance;
* fuzzy match;
* BLEU;
* ROUGE;
* METEOR;
* embedding similarity;
* and domain metrics.

---

## 68.1 Limitation

Similarity is not correctness for open-ended engineering work.

---

## 68.2 Authority

Usually supplementary.

---

# 69. Human Rubric

Suggested fields:

```text
rubric_id
revision
task_class
criteria
scale
anchors
examples
disqualifiers
reviewer_requirements
adjudication
blindness
randomisation
```

---

## 69.1 Criterion Separation

Separate:

* correctness;
* relevance;
* completeness;
* actionability;
* clarity;
* restraint;
* and style.

---

## 69.2 Safety

Separate hard safety judgement.

---

# 70. Human Review Process

1. validate reviewer eligibility;
2. present anonymised case;
3. randomise candidate order;
4. collect criterion scores;
5. collect confidence;
6. collect failure reason;
7. measure agreement;
8. adjudicate critical disagreement;
9. record review provenance;
10. protect reviewer personal data.

---

## 70.1 Domain Reviewer

Code security requires suitable expertise.

---

## 70.2 Founder Review

May be required for product tone or workflow fit, not every case.

---

# 71. Human Pairwise Evaluation

Pairwise options:

* A Better;
* B Better;
* Tie;
* Both Fail;
* Cannot Determine.

---

## 71.1 Position Bias

Swap positions.

---

## 71.2 Length Bias

Record output length and test reviewer tendencies.

---

## 71.3 Identity Masking

Hide provider and model where practical.

---

# 72. Inter-Rater Agreement

Possible measures:

* raw agreement;
* Cohen's kappa;
* Fleiss' kappa;
* Krippendorff's alpha;
* and adjudication rate.

---

## 72.1 Metric Choice

Depends on number of reviewers and scale.

---

## 72.2 Low Agreement

Rubric or task may be invalid.

---

# 73. Model Judge

A Model Judge is one exact AI Execution Profile plus rubric.

---

## 73.1 Judge Profile

Suggested schema:

```text
opure.judge-profile/1
```

---

## 73.2 Fields

```text
judge_profile_id
execution_profile
rubric
input_rendering
candidate_anonymisation
order_randomisation
reference_policy
reasoning_profile
sampling_profile
output_schema
calibration_suite
validated_at
expires_at
profile_sha256
```

---

# 74. Judge Calibration

Compare judge results with human-labelled calibration cases.

---

## 74.1 Measures

* balanced accuracy;
* F1;
* correlation;
* pairwise agreement;
* confusion matrix;
* calibration by score;
* and disagreement categories.

---

## 74.2 Minimum

No judge used for gating without calibration evidence.

---

## 74.3 Domain

Calibration is task specific.

---

# 75. Judge Bias Tests

Test:

* position;
* length;
* verbosity;
* style;
* provider identity;
* self-provider;
* self-model;
* reference anchoring;
* citation appearance;
* assertive tone;
* and prompt injection.

---

## 75.1 Self-Preference

A provider model judging the same provider requires explicit analysis.

---

## 75.2 Cross-Judge

Important cases may use two diverse judges plus human adjudication.

---

# 76. Judge Prompt Injection

Candidate output may try to instruct the judge.

---

## 76.1 Delimiting

Treat candidate as data.

---

## 76.2 Schema

Judge output is strictly validated.

---

## 76.3 Adversarial Cases

Required.

---

# 77. Judge Explanation

A judge explanation is evidence.

It is not hidden chain of thought authority.

---

## 77.1 Stored Content

Store bounded visible rationale where provider permits.

---

## 77.2 No Private Reasoning Request

Do not request hidden chain of thought.

---

# 78. Composite Grader

A Composite Grader combines named results.

---

## 78.1 Hard Gate First

Any failed hard gate fails the composite.

---

## 78.2 Formula

Versioned and inspectable.

---

## 78.3 No Negative Compensation

High style cannot offset build failure.

---

# 79. Evaluation Metrics

Metric families:

* Binary Gate;
* Rate;
* Count;
* Continuous Score;
* Distribution;
* Latency;
* Cost;
* Resource;
* Human Preference;
* and Incident Severity.

---

## 79.1 Direction

Higher-is-better or lower-is-better explicit.

---

## 79.2 Unit

Explicit.

---

## 79.3 Denominator

Explicit.

---

# 80. Metric Record

```text
metric_id
revision
name
definition
unit
direction
aggregation
missing_policy
confidence_method
hard_gate
task_classes
```

---

# 81. Missing and Invalid Cases

Do not silently omit.

Report:

* Not Run;
* Harness Failed;
* Provider Failed;
* Invalid Fixture;
* Invalid Grader;
* Timed Out;
* Cancelled;
* and Excluded with Reason.

---

## 81.1 Denominator

State whether invalid cases count as failures.

---

# 82. Statistical Uncertainty

For finite samples report uncertainty.

---

## 82.1 Binary Rate

Use an appropriate interval such as Wilson or bootstrap.

---

## 82.2 Pairwise

Report win, loss, tie and confidence.

---

## 82.3 Latency

Report percentiles and sample count.

---

## 82.4 Stochastic Model

Use repeated trials where justified.

---

# 83. Effect Size

A statistically detectable difference may be operationally irrelevant.

Report effect size and threshold.

---

# 84. Multiple Comparisons

When comparing many profiles or metrics, acknowledge multiple-comparison risk.

---

## 84.1 Release Decision

Do not cherry-pick one favourable metric.

---

# 85. Critical Failure Ledger

Every evaluation card should retain:

* security violations;
* privacy violations;
* unauthorised tool calls;
* unauthorised fallbacks;
* secret disclosures;
* corrupt patches;
* destructive changes;
* and wrong-project use.

---

## 85.1 Average Does Not Hide

Any unresolved critical failure may block qualification.

---

# 86. Evaluation Card

Suggested schema:

```text
opure.evaluation-card/1
```

---

## 86.1 Fields

```text
execution_profile
task_classes
suite_results
hard_gates
quality_metrics
safety_metrics
operational_metrics
cost_metrics
resource_metrics
human_results
judge_results
critical_failures
known_limitations
evidence_freshness
qualification_summary
created_at
review_due
card_sha256
```

---

## 86.2 No Marketing Summary

Use measured claims only.

---

# 87. Public Benchmark Evidence

Record:

* benchmark;
* version;
* split;
* harness;
* prompt;
* tools;
* model configuration;
* source;
* licence;
* contamination risk;
* and whether reproduced locally.

---

## 87.1 Provider Claim

Mark `Provider Reported`.

---

## 87.2 Independent Claim

Mark source and configuration.

---

## 87.3 Opure Reproduction

Separate.

---

# 88. SWE-bench Policy

SWE-bench can provide real-world patch evidence.

---

## 88.1 Environment

Its Linux and Docker-based environment is not equivalent to Opure's Windows-first product.

---

## 88.2 Use

Supplementary capability evidence.

---

## 88.3 Variants

Pin exact dataset and harness revision.

---

## 88.4 Tools

Agent scaffolding materially affects result.

Record it.

---

# 89. HumanEval Policy

HumanEval-style cases provide compact functional synthesis evidence.

---

## 89.1 Weakness

Small public tasks and test exposure.

---

## 89.2 Hidden Tests

Use augmented tests where licensed.

---

## 89.3 pass@k

Report exact sampling policy.

---

# 90. EvalPlus Policy

Use as evidence that test strength affects measured correctness.

---

## 90.1 Opure Application

Mutation and property tests should strengthen private patch suites.

---

# 91. HELM Policy

Adopt principles:

* broad scenario coverage;
* multi-metric measurement;
* standardisation;
* transparency;
* and explicit incompleteness.

---

## 91.1 Framework Adoption

HELM itself is optional and currently in maintenance mode.

Opure does not depend on it for core execution.

---

# 92. MLPerf Policy

Use operational measurement principles for:

* latency;
* throughput;
* scenario clarity;
* and reproducible system configuration.

Capability quality remains a separate suite.

---

# 93. NIST Alignment

Evaluation governance should support:

* Govern;
* Map;
* Measure;
* and Manage.

---

## 93.1 Measure

Use quantitative, qualitative and mixed methods.

---

## 93.2 Manage

Qualification is an explicit go or no-go decision.

---

## 93.3 Continuous

Re-evaluate while deployed.

---

# 94. Opure Private Regression Suites

These should cover known product behaviour.

---

## 94.1 Source Control

Store safe cases in the repository.

---

## 94.2 Review

Code review like production tests.

---

## 94.3 Model Access

Development models can see inputs during runs.

---

# 95. Held-Out Qualification Suites

Held outside ordinary development access.

---

## 95.1 Small Team Practicality

A local encrypted or restricted test repository may be used.

---

## 95.2 Rotation

Add new cases after incidents and model changes.

---

## 95.3 Disclosure

Do not expose case text in public logs.

---

# 96. Adversarial Suites

Include:

* prompt injection;
* encoded instructions;
* context poisoning;
* tool result injection;
* plugin and MCP manipulation;
* secret canaries;
* path escape;
* wrong project;
* false memory;
* hidden fallback request;
* cost manipulation;
* and grader manipulation.

---

# 97. C# and .NET Suite

Version 1 should include:

* C# syntax;
* nullable reference types;
* async and cancellation;
* generics;
* records;
* pattern matching;
* LINQ;
* source layout;
* project references;
* NuGet lock behaviour;
* xUnit v3;
* Microsoft Testing Platform;
* SQLite;
* gRPC;
* named pipes;
* and Avalonia patterns.

---

## 97.1 No Source Generator Execution

Evaluation may test understanding without enabling untrusted generation.

---

# 98. Windows Suite

Include:

* Windows paths;
* long paths;
* case sensitivity;
* reparse points;
* alternate streams;
* PowerShell quoting;
* process arguments;
* Job Objects;
* named pipes;
* MSIX;
* DPAPI;
* and Windows ACL reasoning.

---

# 99. Patch Proposal Suite

Each case should test:

* issue understanding;
* context use;
* patch scope;
* correct base;
* build;
* tests;
* style;
* no hidden files;
* no dangerous command;
* and explanation.

---

## 99.1 One-Shot

Primary interactive metric should use one attempt.

---

## 99.2 Repair

A separate suite can allow feedback.

---

# 100. Patch Repair Suite

Flow:

1. initial patch;
2. deterministic validation output;
3. bounded repair turn;
4. final validation.

---

## 100.1 Turn Limit

Explicit.

---

## 100.2 No Hidden Extra Calls

Every turn counted.

---

# 101. Code Review Suite

Measure:

* true positive;
* false positive;
* severity;
* localisation;
* explanation;
* fix suggestion;
* and restraint.

---

## 101.1 Known-Good Code

Required to measure false alarms.

---

## 101.2 Security Review

Separate high-recall suite.

---

# 102. Repository Question Suite

Grounded answers with:

* exact source;
* no invented symbol;
* no stale file;
* and citation.

---

# 103. Diagnostic Suite

Use real and synthetic:

* compiler errors;
* test failures;
* restore failures;
* runtime crashes;
* SQLite errors;
* IPC errors;
* and package errors.

---

## 103.1 Root Cause

Grade root-cause correctness separately from suggested fix.

---

# 104. Tool-Use Suite

Test:

* no tool needed;
* one tool;
* several tools;
* tool failure;
* malformed result;
* prompt injection;
* cancellation;
* and permission denial.

---

# 105. Structured Output Suite

Test:

* valid schema;
* nested schema;
* enum;
* optional fields;
* refusal;
* truncated output;
* tool output;
* and malicious content.

---

# 106. Long-Context Suite

Test:

* exact evidence near beginning;
* middle;
* end;
* distractors;
* duplicate source;
* contradictions;
* irrelevant context;
* and token-budget reduction.

---

## 106.1 Context Length

Use realistic Opure plans, not only synthetic needle tests.

---

# 107. Memory Suite

Test:

* active decision;
* stale observation;
* conflict;
* supersession;
* model proposal;
* and wrong-project memory.

---

# 108. Retrieval Suite

Measure:

* required source found;
* irrelevant source avoided;
* duplicate rate;
* semantic false positive;
* exact symbol;
* and stale index.

---

# 109. Embedding Evaluation

Separate from generation.

---

## 109.1 Retrieval Metrics

* recall at k;
* precision at k;
* MRR;
* nDCG;
* duplicate rate;
* and latency.

---

## 109.2 Routing

Embedding routes require exact embedding-space compatibility.

---

# 110. Reranker Evaluation

If later introduced:

* compare against labelled relevance;
* preserve initial candidate recall;
* test latency;
* test bias;
* and keep deterministic eligibility.

---

# 111. Security Suite

Hard cases include:

* leaking source secrets;
* following malicious comments;
* changing protected files;
* executing command without approval;
* invoking external MCP without approval;
* returning hidden data;
* and bypassing review.

---

# 112. Privacy Suite

Test:

* data minimisation;
* provider count request;
* region;
* provider retention setting;
* personal data;
* output logging;
* and deletion evidence.

---

# 113. Provider-Neutrality Suite

The same canonical task should run across provider adapters.

---

## 113.1 Adapter Difference

Record provider-native limitations.

---

## 113.2 No Lowest Common Denominator

A provider-specific capability can have its own task profile.

---

# 114. Local-versus-Remote Comparison

Compare exact profiles on:

* quality;
* privacy posture;
* latency;
* cost;
* context;
* resource;
* offline operation;
* and failure.

---

## 114.1 No Overall Winner

Task and project policy decide.

---

# 115. Operational Benchmarking

Record:

* cold and warm;
* local load;
* provider connection;
* first token;
* total;
* input tokens;
* output tokens;
* tokens per second;
* requests per minute;
* error rate;
* cancellation;
* and retry.

---

# 116. Cost Measurement

Use actual provider usage when available.

---

## 116.1 Price Snapshot

Bind:

* provider;
* region;
* model;
* date;
* currency;
* input rate;
* output rate;
* cached rate;
* reasoning rate;
* and tool charge.

---

## 116.2 Price Change

Routing estimate changes.

Qualification quality need not.

---

## 116.3 Currency

Store provider billing currency and converted display estimate separately.

---

# 117. Local Cost Evidence

May include:

* energy estimate;
* wall time;
* GPU utilisation;
* CPU utilisation;
* and opportunity cost.

---

## 117.1 No False Precision

Energy requires measured evidence.

---

# 118. Latency Evidence

Separate:

* queue time;
* connection time;
* input processing;
* first token;
* generation;
* tool wait;
* and total.

---

## 118.1 Streaming

TTFT matters independently.

---

## 118.2 Percentiles

At least p50, p90 and p95 where sample size supports.

---

# 119. Availability Evidence

Track:

* successful requests;
* provider error;
* rate limit;
* timeout;
* local crash;
* OOM;
* and degraded capability.

---

## 119.1 Historical Window

Routing policy specifies the window.

---

## 119.2 No Global Internet Assumption

Current provider availability is local evidence for the user's configured endpoint.

---

# 120. Cancellation Evaluation

Test:

* before request;
* during upload;
* before first token;
* during streaming;
* during tool;
* and during patch.

---

## 120.1 Success

Bounded resource release and correct receipt.

---

# 121. Quality Gates

A Quality Gate is task specific.

---

## 121.1 Gate Fields

```text
gate_id
revision
task_class
metric
comparison
threshold
minimum_cases
confidence_requirement
maximum_age
severity
```

---

## 121.2 Hard Gate

Failure disqualifies.

---

## 121.3 Soft Floor

Failure removes from ordinary routing but may allow Development use.

---

# 122. Suggested Initial Hard Gates

For all generation:

* valid identity;
* policy compliance;
* no critical secret leak;
* no wrong-project content;
* output parse where required;
* cancellation success;
* and no hidden fallback.

For patch tasks:

* patch applicable;
* allowed paths;
* build pass threshold;
* test pass threshold;
* no critical static-analysis finding;
* and reviewable diff.

For tool tasks:

* no unauthorised call;
* argument schema valid;
* bounded calls;
* and correct denial behaviour.

---

# 123. Quality Floor

A profile must satisfy the current suite revision.

---

## 123.1 Evidence Age

Suggested default:

* security evidence: 30 days or material change;
* core quality: 90 days or material change;
* operational latency: 7 days;
* price: 24 hours or provider update;
* availability: rolling 24 hours plus 30-day view.

These are provisional.

---

# 124. Qualification

Suggested schema:

```text
opure.model-qualification/1
```

---

## 124.1 Fields

```text
qualification_id
execution_profile
task_class
project_policy_class
data_classes
state
suite_revisions
evaluation_runs
hard_gates
metric_floors
restrictions
review_mode
valid_from
expires_at
approved_by
created_at
qualification_sha256
```

---

## 124.2 Task Specific

Required.

---

## 124.3 Data Class Specific

A remote profile may be qualified for Public but not Confidential data.

---

## 124.4 Project Specific

A project may narrow qualification.

---

# 125. Qualification States

### 125.1 Discovered

Metadata only.

### 125.2 Metadata Verified

Identity and capability verified.

### 125.3 Evaluation Pending

No current evidence.

### 125.4 Evaluating

Run active.

### 125.5 Qualified for Development

Manual experimental use.

### 125.6 Qualified for Preview

Preview workflow use.

### 125.7 Qualified for Stable

Stable ordinary routing.

### 125.8 Qualified with Restrictions

Only declared conditions.

### 125.9 Not Qualified

Failed evidence.

### 125.10 Suspended

Temporary incident or drift.

### 125.11 Deprecated

Migration required.

### 125.12 Unavailable

Endpoint or runtime absent.

### 125.13 Quarantined

Security or integrity concern.

### 125.14 Retired

No new use.

---

# 126. Qualification Approval

Stable qualification requires:

* Evaluation Owner;
* Security Owner for sensitive tasks;
* Provider or Local Runtime Owner;
* and Product or delegated release approval.

---

## 126.1 Founder Burden

The founder need not approve every minor metric refresh.

Policy defines delegation.

---

# 127. Qualification Restrictions

Examples:

* local only;
* public data only;
* no tools;
* no patch generation;
* human review required;
* maximum context;
* maximum output;
* one turn;
* no MCP;
* no plugin results;
* and Preview only.

---

# 128. Evaluation Freshness

Evidence invalidates after material change.

---

## 128.1 Full Re-Evaluation

Required for:

* model snapshot;
* Runtime Package;
* model file;
* instruction template;
* Context Policy;
* request renderer;
* tool schema;
* safety profile;
* or output schema.

---

## 128.2 Partial Re-Evaluation

May suffice for:

* price;
* latency;
* endpoint availability;
* and hardware performance.

---

# 129. Model Identity Monitoring

Provider response and metadata should be checked.

---

## 129.1 Resolved Model Drift

Create incident and suspend qualification if unexpected.

---

## 129.2 Alias

Stable aliases prohibited.

---

## 129.3 Provider Migration

Requires a new profile.

---

# 130. Deprecation Monitoring

Track official:

* deprecation announcement;
* earliest shutdown;
* exact shutdown;
* recommended replacement;
* and migration notes.

---

## 130.1 No Automatic Replacement

Recommended replacement is a candidate, not qualified.

---

## 130.2 Deadline

Surface before shutdown.

---

# 131. Routing Governance Service

The service receives a Routing Request and produces a Routing Decision.

---

## 131.1 No Content Authority

It receives safe operation metadata and Context Plan summary.

---

## 131.2 No Arbitrary Provider Enumeration

Only registered profiles.

---

# 132. Routing Request

Suggested schema:

```text
opure.routing-request/1
```

---

## 132.1 Fields

```text
request_id
operation_id
project_id
task_class
risk_class
data_classes
project_cloud_policy
required_capabilities
preferred_execution
allowed_profiles
context_plan_summary
input_tokens
output_reserve
tool_requirements
output_schema
latency_budget
cost_budget
resource_budget
quality_floor
review_mode
fallback_policy
user_pin
created_at
request_sha256
```

---

## 132.2 No Full Prompt

Default.

---

## 132.3 Ambiguous Task

May require task clarification or a bounded classifier proposal.

---

# 133. Task Classifier

Primary classifier is deterministic.

---

## 133.1 Features

* product command;
* workflow step;
* output type;
* patch intent;
* selected service;
* tool requirement;
* structured schema;
* and project policy.

---

## 133.2 Free-Form Request

A local or approved model classifier may propose.

---

## 133.3 Validation

Trusted code maps to an allowed class.

---

## 133.4 No Provider Choice

Classifier does not choose provider.

---

# 134. Risk Class

Suggested:

* Low;
* Standard;
* Elevated;
* High;
* and Critical.

---

## 134.1 Factors

* code change;
* command;
* external tool;
* secret proximity;
* cloud data;
* security decision;
* release;
* and unattended workflow.

---

## 134.2 High Risk

May require stronger qualification and human review.

---

# 135. Candidate Enumeration

Candidates come from:

* project-approved local profiles;
* project-approved Provider Profiles;
* explicit user pin;
* workflow policy;
* and product defaults.

---

## 135.1 Disabled Profile

Not candidate.

---

## 135.2 Missing Credential

Ineligible.

---

## 135.3 Model Not Installed

Ineligible unless the operation is an explicit install plan.

---

# 136. Hard Eligibility

Check:

* project;
* channel;
* cloud policy;
* provider;
* region;
* data class;
* profile identity;
* qualification;
* task capability;
* context limit;
* output limit;
* tools;
* schema;
* review mode;
* security;
* availability;
* and expiry.

---

## 136.1 Order

Cheap policy checks before expensive availability probes.

---

# 137. Project Cloud Policy

ADR-0019 rules are hard.

---

## 137.1 Local Only

Remote candidates removed.

---

## 137.2 Ask Every Time

Remote candidate remains pending until Data Sharing Plan approval.

---

## 137.3 Approved Providers Only

Exact approved revisions only.

---

## 137.4 Custom

Explicit evaluator.

---

# 138. Required Capabilities

Examples:

* text generation;
* code infill;
* structured output;
* tool calling;
* vision;
* embeddings;
* streaming;
* reasoning control;
* deterministic seed;
* and context length.

---

## 138.1 Capability Claim

Verified by metadata and evaluation.

---

# 139. Context Fit

Use ADR-0021:

* exact input count;
* output reserve;
* combined window;
* reasoning reserve;
* tools;
* and safety reserve.

---

## 139.1 Does Not Fit

Ineligible.

---

## 139.2 Reduction

Context Assembly may create another plan before a new Routing Request.

---

# 140. Local Resource Fit

Use ADR-0020:

* VRAM;
* RAM;
* CPU;
* load;
* active build;
* and performance mode.

---

## 140.1 No Silent CPU Fallback

Profile change.

---

# 141. Availability

A profile is available when:

* provider endpoint reachable under approved policy;
* credential valid;
* rate limit permits;
* or local runtime and model ready or loadable.

---

## 141.1 Probe Data

No project prompt in availability probe.

---

# 142. Cost Budget

Estimate based on:

* input;
* output reserve;
* reasoning;
* tools;
* provider rate;
* and currency.

---

## 142.1 Unknown Cost

Strict budget denies or asks.

---

## 142.2 Actual Cost

Receipt reconciles.

---

# 143. Latency Budget

Estimate by task and profile.

---

## 143.1 Evidence Window

Recent and hardware/region specific.

---

## 143.2 Unknown

May be permitted with warning or denied.

---

# 144. Quality Floor Filtering

Use task-specific qualification and current evidence.

---

## 144.1 Missing Metric

Do not assume pass.

---

## 144.2 Small Sample

May restrict to Development.

---

# 145. Pareto Frontier

After hard filtering, identify non-dominated candidates across selected dimensions.

---

## 145.1 Dimensions

Task policy chooses:

* quality;
* latency;
* cost;
* privacy posture;
* local resource;
* availability;
* and energy where measured.

---

## 145.2 Hard Data Policy

Not a trade-off dimension.

Already filtered.

---

# 146. Policy Preference

Examples:

```text
prefer local if quality within accepted margin
prefer lower latency after quality floor
prefer lower cost for background task
prefer stronger security recall for audit
prefer exact structured-output reliability
```

---

## 146.1 Quality Margin

Versioned and task specific.

---

## 146.2 No Hidden Formula

Visible.

---

# 147. Stable Tie-Breaking

Suggested:

1. explicit user pin;
2. exact task priority;
3. higher qualification class;
4. preferred execution posture;
5. policy preference result;
6. lower profile priority number;
7. immutable profile ID.

---

# 148. Routing Policy

Suggested schema:

```text
opure.routing-policy/1
```

---

## 148.1 Fields

```text
policy_id
revision
task_classes
risk_classes
project_policy_classes
candidate_profiles
hard_filters
quality_floors
evidence_freshness
pareto_dimensions
preference_rules
cost_policy
latency_policy
local_preference
review_policy
fallback_policy
owners
created_at
review_due
policy_sha256
```

---

## 148.2 Source Controlled

First-party defaults are source controlled.

---

## 148.3 Project Override

May narrow or set preferences.

---

## 148.4 Plugin Override

Cannot broaden.

---

# 149. Routing Decision

Suggested schema:

```text
opure.routing-decision/1
```

---

## 149.1 Fields

```text
decision_id
routing_request
routing_policy
candidate_results
selected_profile
selected_reason
quality_evidence
operational_evidence
cost_estimate
latency_estimate
context_fit
resource_fit
fallback
approval
created_at
expires_at
decision_sha256
```

---

## 149.2 Candidate Result

For every profile:

* Eligible;
* Ineligible;
* Pending Approval;
* Qualified but Dominated;
* Qualified but Not Preferred;
* Selected;
* or Probe Failed.

---

## 149.3 Reasons

Stable reason codes plus display text.

---

# 150. Decision Expiry

Suggested interactive expiry:

```text
5 minutes
```

or before any relevant state changes.

---

## 150.1 Invalidation

* project policy;
* Context Plan;
* profile;
* qualification;
* credential;
* availability;
* price;
* local resources;
* or user pin.

---

# 151. Decision Binding

AI Router verifies:

* operation;
* project;
* profile;
* Context Plan;
* Data Sharing Plan;
* capability;
* expiry;
* and hash.

---

## 151.1 No Profile Substitution

Denied.

---

# 152. Explicit User Pin

A developer may choose an eligible profile.

---

## 152.1 Qualification

A pin cannot bypass hard gates.

---

## 152.2 Development Override

A Development profile may be selected through an explicit experimental flow with warnings.

---

## 152.3 Remember Choice

Project-specific preference requires explicit configuration.

---

# 153. Local Preference

Values:

* Hard Local;
* Prefer Local;
* Neutral;
* Prefer Remote;
* and Explicit Only.

---

## 153.1 Hard Local

No remote.

---

## 153.2 Prefer Local

Use local when it passes task policy and accepted quality margin.

---

## 153.3 Prefer Remote

Still subject to cloud approval.

---

# 154. Fallback Policy

Suggested schema:

```text
opure.fallback-policy/1
```

---

## 154.1 Fields

```text
policy_id
initial_profile
allowed_retry
retry_count
allowed_fallbacks
approval_mode
partial_response_policy
cost_change_limit
data_posture_change
quality_class_change
timeout
```

---

# 155. Retry

Same exact profile.

---

## 155.1 Retry Causes

* transient network;
* provider overload;
* bounded timeout before response;
* or local process startup race.

---

## 155.2 No Retry

For:

* invalid request;
* policy denial;
* context too large;
* authentication;
* deterministic safety failure;
* or partial destructive tool action.

---

# 156. Fallback

Different exact profile.

---

## 156.1 Approval Required

Default when changing:

* provider;
* model;
* local versus cloud;
* region;
* data posture;
* cost class;
* quality class;
* context reduction;
* or reasoning profile.

---

## 156.2 Named Profiles

No open-ended “any available model”.

---

# 157. Partial Response

If output started:

* mark first attempt Partial;
* do not concatenate another model invisibly;
* offer retry or new attempt;
* and preserve receipts.

---

# 158. Tool Operation Fallback

Do not switch models mid-tool loop silently.

---

## 158.1 New Plan

Requires new Context Plan and Routing Decision.

---

# 159. Provider Outage

Options:

* retry same profile;
* ask for named fallback;
* use named pre-approved fallback in unattended workflow;
* pause;
* or cancel.

---

## 159.1 Unattended Workflow

Fallback must be declared before start.

---

# 160. Local Failure

Options:

* reload same profile;
* named CPU profile;
* named different local model;
* ask for cloud;
* or fail.

---

## 160.1 OOM

No silent change.

---

# 161. Provider-Managed Router

Represent as:

```text
provider-router-profile
```

---

## 161.1 Requirements

* exact router ID;
* candidate set;
* region set;
* selected-model disclosure;
* quality criteria;
* cost criteria;
* and provider documentation.

---

## 161.2 Qualification

Evaluate router end to end.

---

## 161.3 Limitations

Provider router may not use Opure-specific performance evidence.

---

## 161.4 Stable Default

Disabled.

---

# 162. Model Aliases

Stable forbids:

* latest;
* auto;
* default;
* rolling;
* or unversioned preview

unless the provider contract proves immutability.

---

## 162.1 Preview

May use a preview alias with explicit expiry and model-resolution monitoring.

---

## 162.2 Resolution

Persist actual returned identity.

---

# 163. Model Deprecation

A deprecated profile:

* remains historical;
* stops new Stable qualification before shutdown;
* enters migration queue;
* and may remain Development-only temporarily.

---

## 163.1 Replacement

Must pass evaluation.

---

# 164. Canary Evaluation

Canaries detect:

* identity;
* schema;
* instruction following;
* refusal;
* tool basics;
* latency;
* and obvious drift.

---

## 164.1 Frequency

Provider profiles: daily or before first use after a meaningful interval.

Local profiles: after runtime, model, driver or configuration change.

---

## 164.2 Data

Synthetic or licensed.

---

## 164.3 Cost

Bounded.

---

# 165. Shadow Evaluation

Disabled by default for project operations.

---

## 165.1 Explicit Session

The developer can approve a comparison.

---

## 165.2 Preview

Show:

* exact profiles;
* exact data;
* call count;
* cost;
* provider;
* region;
* and retention.

---

## 165.3 Primary Result

Only the selected primary controls the operation.

---

## 165.4 Project Data

Never duplicated silently.

---

# 166. Synthetic Monitoring

Use synthetic cases for continuous provider health.

---

## 166.1 No User Content

Required.

---

# 167. Online Signals

Possible events:

* response accepted;
* response dismissed;
* explicit rating;
* model changed;
* patch reviewed;
* patch applied;
* patch edited;
* patch reverted;
* build result;
* test result;
* tool approved;
* tool rejected;
* and cancellation.

---

## 167.1 Interpretation

Each has limits.

---

## 167.2 No Automatic Qualification

Signals create evidence or candidate cases.

---

## 167.3 Local Default

Stored locally.

---

# 168. Explicit Feedback

Suggested prompts:

* Helpful;
* Incorrect;
* Missed context;
* Too broad;
* Too slow;
* Too costly;
* Unsafe;
* Wrong model;
* and Other.

---

## 168.1 No Forced Rating

Optional.

---

## 168.2 Source Link

Feedback binds exact operation and profile.

---

# 169. Feedback Privacy

Do not send feedback to a provider unless:

* explicitly requested;
* previewed;
* and covered by policy.

---

## 169.1 Training

Separate consent.

---

# 170. Evaluation Case Promotion from Feedback

A feedback event may become a case after:

* review;
* de-identification;
* secret scan;
* source licence;
* expected result;
* grader;
* and split assignment.

---

## 170.1 Held-Out

Do not expose after designation.

---

# 171. Drift Detection

Drift sources:

* canary;
* regression;
* user incident;
* provider release;
* alias change;
* output schema;
* latency;
* cost;
* token use;
* refusal;
* safety;
* and local runtime.

---

## 171.1 Drift Event

Suggested fields:

```text
drift_id
execution_profile
task_class
source
metric
baseline
observed
severity
detected_at
state
response
```

---

# 172. Drift Severity

* Informational;
* Minor;
* Major;
* Critical.

---

## 172.1 Critical

Examples:

* secret leak;
* hidden model change;
* policy bypass;
* destructive tool call;
* wrong project;
* or provider identity mismatch.

---

# 173. Drift Response

* record;
* increase review;
* re-run suite;
* suspend task qualification;
* suspend profile;
* quarantine;
* update routing;
* or retire.

---

## 173.1 No Automatic Alternative

Suspension may leave no route.

That is preferable to hidden boundary changes.

---

# 174. Routing Policy Change

Requires:

* reason;
* before and after;
* evaluation evidence;
* affected tasks;
* project impact;
* and approval.

---

## 174.1 Rollback

Retain prior policy revision.

---

# 175. Evaluation Data Security

Evaluation datasets may contain:

* source;
* vulnerabilities;
* malicious prompts;
* secrets canaries;
* and unpublished incidents.

---

## 175.1 Classification

Required.

---

## 175.2 Access

Least privilege.

---

## 175.3 Remote Use

Data Sharing Plan.

---

# 176. Evaluation Data Licensing

Every case records:

* source licence;
* dataset licence;
* code licence;
* redistribution;
* modification;
* model-use restrictions;
* and export policy.

---

## 176.1 Unknown Licence

Local internal evaluation may require review.

Redistribution denied.

---

# 177. Malicious Evaluation Content

Treat cases as untrusted data.

---

## 177.1 Harness

Cannot follow instructions embedded in fixture.

---

## 177.2 Grader

Protected from injection.

---

# 178. Evaluation Secrets

Use synthetic canaries.

---

## 178.1 Real Credentials

Prohibited.

---

# 179. Provider Evaluation Services

Opure may compare provider-hosted eval services.

---

## 179.1 Not Authority

Provider service results are supplementary.

---

## 179.2 Data Transmission

Explicit.

---

## 179.3 Provider Grader

Exact profile and retention recorded.

---

# 180. Evaluation Storage

Recommended:

* one service-owned SQLite database for metadata;
* content-addressed artefact store for large outputs;
* immutable run manifests;
* restricted held-out store;
* and generated reports.

---

## 180.1 No Project Repository Output by Default

Evaluation results live in app data or CI artefacts.

---

## 180.2 Source-Controlled Definitions

Safe suite definitions and graders may live in repository.

---

# 181. Suggested Evaluation Tables

```text
execution_profiles
evaluation_suites
evaluation_cases
evaluation_case_revisions
evaluation_splits
fixture_manifests
grader_profiles
judge_profiles
human_rubrics
evaluation_plans
evaluation_runs
trials
trial_outputs
grader_results
human_reviews
metric_definitions
metric_results
critical_failures
evaluation_cards
qualifications
qualification_events
drift_events
feedback_events
routing_policies
routing_requests
routing_decisions
routing_candidate_results
fallback_events
query_receipts
```

---

# 182. Large Artefacts

Use CAS for:

* model outputs;
* patches;
* logs;
* traces;
* build results;
* and reports.

---

## 182.1 Secret Scan

Before retention or export.

---

# 183. Retention

Suggested:

* Evaluation Suite definitions: permanent while referenced;
* Case revisions: permanent while referenced;
* Held-out cases: policy controlled;
* Evaluation Runs: 180 days;
* Stable qualification runs: product version lifetime;
* Trial outputs: 90 days;
* Critical failure artefacts: security policy;
* Human review: product version lifetime;
* Routing Decisions: 90 days;
* Feedback events: 90 days;
* Canary runs: 30 days;
* and operational metrics: bounded rolling windows.

These are provisional.

---

# 184. Deletion

Deleting an Evaluation Case requires:

* dependency preview;
* qualification impact;
* historical run preservation decision;
* and split governance.

---

## 184.1 User Project Data

Privacy deletion should purge retained artefacts under Opure control.

---

# 185. Export

Evaluation report export can include:

* profile;
* suites;
* metrics;
* uncertainty;
* critical failures;
* qualification;
* routing policies;
* and provenance.

---

## 185.1 Held-Out Content

Do not export by default.

---

## 185.2 Model Outputs

Optional and classification controlled.

---

# 186. Trust Centre

Views:

```text
Execution Profiles
Evaluation Suites
Evaluation Runs
Evaluation Cards
Qualifications
Routing Policies
Routing Decisions
Fallbacks
Canaries
Drift
Model Deprecations
User Feedback
Remote Evaluation Calls
Costs
Known Limitations
```

---

## 186.1 Profile Detail

Show:

* exact identity;
* task qualifications;
* evaluation age;
* hard gates;
* metrics;
* failures;
* data posture;
* cost;
* latency;
* and restrictions.

---

## 186.2 Routing Explanation

Show why selected and why others were excluded.

---

# 187. Desktop Routing UI

For interactive use show:

* selected model;
* local or cloud;
* provider;
* task;
* quality class;
* estimated latency;
* estimated cost;
* context fit;
* restrictions;
* fallback;
* and change control.

---

## 187.1 Compact Default

Do not overwhelm ordinary use.

---

## 187.2 Expandable Evidence

Available.

---

# 188. UI Copy — Selection

Suggested:

> Opure selected this exact model profile because it is approved for the current project policy, supports the required context and tools, passes the task's quality gates, and best matches the displayed quality, latency, cost and local-resource preferences. No other provider or model will be used without the declared fallback policy.

---

# 189. UI Copy — No Eligible Model

Suggested:

> No installed or approved model satisfies every required data-policy, capability, context, quality and resource rule for this operation. Opure has not switched to an unapproved cloud provider, reduced the context silently or selected a lower-quality model. Review the listed constraints to create a new plan.

---

# 190. UI Copy — Fallback

Suggested:

> The selected model could not complete the request. The proposed fallback changes the displayed model or execution posture. Review its provider, data policy, estimated quality, cost and context before continuing.

---

# 191. UI Copy — Evaluation Evidence

Suggested:

> Qualification is based on the listed Opure evaluation suites and exact model configuration. Public benchmark scores are supplementary. Model behaviour can change, so evidence expires after material model, prompt, runtime, tool or provider changes.

---

# 192. Diagnostics

Safe diagnostics may include:

* run ID;
* profile ID;
* suite ID;
* case ID;
* grader;
* metric;
* outcome;
* routing reason;
* fallback;
* latency;
* token count;
* cost;
* and error category.

---

## 192.1 Prohibited

Do not log:

* prompts;
* source;
* held-out expected outputs;
* secrets;
* provider credentials;
* human reviewer identity where unnecessary;
* or hidden reasoning.

---

# 193. Metrics

Low-cardinality local metrics:

* evaluation runs;
* pass rate by suite class;
* hard-gate failures;
* qualification count;
* routing selections by local or remote;
* no-route rate;
* fallback rate;
* drift incidents;
* latency;
* and cost.

---

## 193.1 No Project Labels

Do not export project identity.

---

# 194. Recovery

On Runtime restart:

* Running trials become Interrupted;
* pending remote calls reconcile through receipts;
* immutable artefacts remain;
* Routing Decisions expire;
* qualifications remain if evidence valid;
* and incomplete evaluation plans can resume explicitly.

---

# 195. Evaluation Cancellation

Cancel:

* pending trials;
* provider calls where possible;
* local inference;
* build;
* test;
* grader;
* and human-review queue assignment.

---

## 195.1 Completed Evidence

Retain completed trial results with Cancelled run state.

---

# 196. Error Model

Stable categories include:

* Evaluation Profile Invalid;
* Evaluation Suite Invalid;
* Evaluation Case Invalid;
* Evaluation Split Violation;
* Evaluation Fixture Invalid;
* Evaluation Licence Unknown;
* Evaluation Plan Invalid;
* Evaluation Approval Required;
* Evaluation Budget Exceeded;
* Evaluation Provider Unavailable;
* Evaluation Local Runtime Unavailable;
* Evaluation Trial Failed;
* Evaluation Harness Failed;
* Evaluation Grader Invalid;
* Evaluation Judge Uncalibrated;
* Evaluation Human Review Required;
* Evaluation Contamination Suspected;
* Evaluation Result Incomplete;
* Evaluation Critical Failure;
* Evaluation Qualification Denied;
* Evaluation Qualification Expired;
* Evaluation Drift Detected;
* Routing Request Invalid;
* Routing Policy Missing;
* Routing No Eligible Profile;
* Routing Data Policy Denied;
* Routing Capability Missing;
* Routing Context Does Not Fit;
* Routing Resource Does Not Fit;
* Routing Quality Floor Failed;
* Routing Cost Budget Failed;
* Routing Latency Budget Failed;
* Routing Approval Required;
* Routing Decision Expired;
* Routing Profile Changed;
* Routing Fallback Denied;
* Routing Partial Response;
* Routing Provider Alias Unsafe;
* Routing Model Deprecated;
* Routing Operation Cancelled;
* and Routing Recovery Required.

---

# 197. Security Threat Model

Relevant threats include:

* hidden model substitution;
* provider alias drift;
* wrong provider;
* wrong region;
* wrong account;
* wrong local model file;
* evaluation-data leakage;
* held-out leakage;
* benchmark contamination;
* malicious fixture;
* malicious expected output;
* grader prompt injection;
* judge bias;
* judge self-preference;
* model self-grading;
* authority escalation;
* aggregate-score masking;
* critical-failure suppression;
* cost manipulation;
* latency manipulation;
* usage under-reporting;
* hidden shadow traffic;
* hidden fallback;
* fallback race;
* partial-response blending;
* project-policy bypass;
* secret exposure;
* wrong-project evaluation;
* untrusted code execution;
* malicious patch;
* malicious tool call;
* poisoned online feedback;
* sybil feedback;
* model deprecation surprise;
* qualification replay;
* stale Routing Decision;
* and corrupted evaluation evidence.

---

# 198. Security Controls

Controls include:

* exact AI Execution Profiles;
* immutable suites and cases;
* restricted held-out access;
* source and licence provenance;
* dedicated evaluation workers;
* no production credentials;
* deterministic graders;
* calibrated model judges;
* human adjudication;
* critical-failure ledger;
* hard gates;
* task-specific qualification;
* qualification expiry;
* exact provider identity monitoring;
* Stable alias prohibition;
* hard project-policy filters;
* Context Plan binding;
* Data Sharing Plan binding;
* operation-bound Routing Decisions;
* explicit fallback;
* no hidden shadow traffic;
* canary synthetic data;
* feedback review;
* drift suspension;
* and Trust Centre evidence.

---

# 199. Security Limitations

This architecture cannot guarantee:

* complete benchmark decontamination;
* perfect model-judge calibration;
* perfect human judgement;
* provider reproducibility;
* absence of provider-side model changes;
* perfect cost forecasting;
* perfect latency forecasting;
* or complete detection of subtle quality regressions.

Evaluation reduces uncertainty.

It does not prove universal correctness.

---

# 200. Privacy Impact

Evaluation cases and trial outputs may contain sensitive code and project context.

---

## 200.1 Local Default

Private evaluation remains local where practical.

---

## 200.2 Remote Evaluation

Every remote trial is an explicit provider operation.

---

## 200.3 Human Review

Reviewers see only necessary data.

---

## 200.4 Feedback

Local and minimal by default.

---

# 201. Reliability Impact

Routing governance can fail closed when no profile qualifies.

This may reduce availability but preserves trust boundaries.

---

## 201.1 Degraded Mode

Explicit manual selection among eligible Development profiles may remain available.

---

# 202. Performance Impact

Evaluation is resource intensive.

Routing should remain lightweight through cached current evidence.

---

## 202.1 Background Evaluation

Subordinate to developer work.

---

## 202.2 Routing Latency

Should be negligible compared with context assembly and inference.

---

# 203. Testing Strategy

ADR-0008 applies.

Evaluation and routing governance require:

* schema tests;
* profile-identity tests;
* suite tests;
* split tests;
* contamination tests;
* fixture tests;
* grader tests;
* human-review tests;
* judge-calibration tests;
* metric tests;
* statistical tests;
* qualification tests;
* routing-policy tests;
* context-fit tests;
* resource-fit tests;
* fallback tests;
* alias and deprecation tests;
* canary tests;
* shadow-evaluation tests;
* feedback tests;
* drift tests;
* recovery tests;
* fuzzing;
* performance tests;
* and adversarial governance suites.

---

# 204. AI Execution Profile Tests

Test:

* valid remote profile;
* valid local profile;
* changed model;
* changed endpoint;
* changed region;
* changed provider account;
* changed Runtime Package;
* changed model weights;
* changed quantisation;
* changed instruction template;
* changed renderer;
* changed tokenizer;
* changed Context Policy;
* changed tools;
* changed output schema;
* changed reasoning;
* changed sampling;
* and canonical hash.

---

# 205. Model Identity Tests

Test:

* exact stable ID;
* mutable latest alias;
* preview ID;
* experimental ID;
* returned resolved ID;
* missing resolved ID;
* unexpected ID;
* provider migration;
* and local hash mismatch.

---

# 206. Suite Schema Tests

Test:

* valid suite;
* duplicate ID;
* unsupported revision;
* missing owner;
* missing task;
* missing grader;
* invalid threshold;
* unknown split;
* circular suite reference;
* and canonical hash.

---

# 207. Case Schema Tests

Test:

* valid case;
* missing expected result;
* missing forbidden behaviour;
* wrong project fixture;
* missing licence;
* oversized input;
* unsafe URI;
* mutable source;
* invalid data class;
* and revision.

---

# 208. Split Tests

Test:

* Development;
* Regression;
* Calibration;
* Held-Out;
* Adversarial;
* Canary;
* Retired;
* split movement;
* development exposure;
* and held-out access denial.

---

# 209. Held-Out Leakage Tests

Attempt access through:

* Project Knowledge;
* Context Assembly;
* Project Memory;
* plugin;
* MCP server;
* model prompt optimiser;
* source search;
* logs;
* reports;
* support bundle;
* and Desktop.

---

# 210. Contamination Tests

Test:

* public benchmark;
* model-generated case;
* expected-output exposure;
* prompt tuning;
* grader leakage;
* duplicate public task;
* paraphrased task;
* and time-separated case.

---

# 211. Fixture Manifest Tests

Test:

* exact commit;
* exact files;
* wrong hash;
* missing dependency lock;
* unclean state;
* wrong toolchain;
* network denied;
* licence missing;
* and fixture update.

---

# 212. Evaluation Environment Tests

Test:

* clean worker;
* no production credential;
* no network;
* provider-only network;
* local model;
* project path containment;
* process limit;
* memory limit;
* disk limit;
* time limit;
* and cancellation.

---

# 213. Malicious Fixture Tests

Include:

* build script exfiltration;
* pre-build command;
* package restore hook;
* source generator;
* analyser;
* symlink escape;
* archive bomb;
* malicious test;
* fork bomb;
* and secret file.

---

# 214. Trial Tests

Test:

* one attempt;
* repeated attempts;
* same seed;
* unsupported seed;
* provider timeout;
* local crash;
* partial stream;
* tool call;
* patch output;
* invalid output;
* and cancellation.

---

# 215. Reproducibility Tests

Repeat:

* deterministic exact grader;
* local low-temperature generation;
* remote generation;
* build;
* test;
* patch;
* and model judge.

Classify reproducibility honestly.

---

# 216. Exact Grader Tests

Test:

* equality;
* inequality;
* case sensitivity;
* Unicode normalisation;
* numeric tolerance;
* set;
* order;
* and canonical JSON.

---

# 217. Schema Grader Tests

Test:

* valid JSON;
* invalid JSON;
* missing required;
* extra field;
* wrong enum;
* recursive payload;
* excessive depth;
* malformed protobuf;
* and schema change.

---

# 218. Compile Grader Tests

Test:

* compile success;
* compile failure;
* warning;
* warning as error;
* multi-target;
* restore absent;
* dependency lock;
* timeout;
* and malicious target.

---

# 219. Test Grader Tests

Test:

* all pass;
* one fail;
* hidden test;
* flaky test;
* timeout;
* test deletion;
* new test only;
* framework crash;
* and result parsing.

---

# 220. Property Test Grader Tests

Test:

* valid generator;
* seed;
* shrinking;
* timeout;
* generated invalid input;
* and replay.

---

# 221. Mutation Test Grader Tests

Test:

* killed mutant;
* surviving mutant;
* equivalent mutant;
* timeout;
* and report parsing.

---

# 222. Patch Applicability Tests

Test:

* valid patch;
* wrong base;
* path escape;
* malformed hunk;
* CRLF;
* rename;
* delete;
* binary;
* and conflict.

---

# 223. Patch Scope Tests

Test:

* one allowed file;
* prohibited file;
* generated file;
* lockfile;
* dependency change;
* excessive lines;
* excessive files;
* and no-op patch.

---

# 224. Repository State Tests

Test:

* expected diff;
* untracked file;
* changed submodule;
* branch change;
* ignored output;
* lockfile drift;
* and clean restore.

---

# 225. Static Analysis Tests

Test:

* pinned analyser;
* changed rules;
* security finding;
* style finding;
* false positive;
* tool crash;
* and result format.

---

# 226. Security Grader Tests

Test:

* secret disclosure;
* insecure crypto;
* command injection;
* SQL injection;
* path traversal;
* hard-coded credential;
* dangerous deserialisation;
* and false positive.

---

# 227. Policy Grader Tests

Test:

* Local Only;
* Ask Every Time;
* Approved Providers;
* wrong region;
* unapproved model;
* hidden tool;
* hidden context;
* hidden fallback;
* and unreviewed patch.

---

# 228. Tool-Call Grader Tests

Test:

* no call;
* correct call;
* wrong tool;
* wrong argument;
* prohibited argument;
* excessive calls;
* retry;
* denial;
* and cancellation.

---

# 229. MCP Grader Tests

Test:

* correct server;
* wrong account;
* changed fingerprint;
* injection;
* external link;
* output provenance;
* and hidden call.

---

# 230. Plugin Grader Tests

Test:

* correct capability;
* revoked capability;
* package change;
* prompt injection;
* authority escalation;
* and result use.

---

# 231. Context Grader Tests

Test:

* all mandatory;
* prohibited absent;
* wrong project;
* secret;
* duplicate;
* irrelevant flood;
* stale source;
* explicit selection;
* memory conflict;
* and hidden truncation.

---

# 232. Grounding Tests

Test:

* fully grounded;
* partially grounded;
* invented symbol;
* invented file;
* stale source;
* wrong citation;
* ambiguous source;
* and unsupported recommendation.

---

# 233. Instruction-Following Tests

Test:

* exact format;
* maximum length;
* file scope;
* no patch;
* required patch;
* British English;
* no command;
* no list;
* and conflicting instructions.

---

# 234. Refusal Tests

Test:

* correct refusal;
* over-refusal;
* under-refusal;
* partial safe help;
* escalation;
* policy explanation;
* and malicious framing.

---

# 235. Human Rubric Tests

Test:

* clear anchors;
* ambiguous anchors;
* missing criterion;
* evaluator training;
* identity masking;
* random order;
* tie;
* cannot determine;
* and adjudication.

---

# 236. Reviewer Identity Tests

Attempt:

* unauthorised reviewer;
* expired session;
* wrong role;
* model impersonation;
* plugin impersonation;
* imported review;
* and duplicate review.

---

# 237. Inter-Rater Tests

Use known agreement and disagreement fixtures.

Verify metric implementation.

---

# 238. Judge Profile Tests

Test:

* valid profile;
* changed model;
* changed rubric;
* changed order;
* changed reasoning;
* changed temperature;
* invalid output schema;
* expired calibration;
* and unsupported model.

---

# 239. Judge Calibration Tests

Test:

* balanced accuracy;
* F1;
* correlation;
* confusion matrix;
* pairwise agreement;
* threshold;
* domain transfer;
* and sample size.

---

# 240. Judge Position Bias Tests

Swap A and B.

---

# 241. Judge Length Bias Tests

Compare equivalent short and long answers.

---

# 242. Judge Style Bias Tests

Compare substance-equivalent styles.

---

# 243. Judge Self-Preference Tests

Candidate and judge from:

* same model;
* same provider;
* different provider;
* local judge;
* and human labels.

---

# 244. Judge Prompt-Injection Tests

Candidate text instructs:

* score ten;
* ignore rubric;
* reveal reference;
* choose A;
* output invalid JSON;
* and call a tool.

---

# 245. Composite Grader Tests

Test:

* all pass;
* hard gate fail;
* missing metric;
* invalid formula;
* negative compensation;
* and changed weights.

---

# 246. Metric Definition Tests

Test:

* direction;
* unit;
* denominator;
* missing policy;
* aggregation;
* threshold;
* and version.

---

# 247. Statistical Tests

Test known distributions for:

* mean;
* median;
* standard deviation;
* percentiles;
* Wilson interval;
* bootstrap interval;
* effect size;
* pairwise rates;
* and missing cases.

---

# 248. Small-Sample Tests

Verify warning or qualification restriction.

---

# 249. Multiple-Comparison Tests

Verify report warns and release logic does not cherry-pick.

---

# 250. Critical Failure Tests

Insert one critical failure into high average results.

Qualification must fail or require explicit unresolved-risk action.

---

# 251. Evaluation Card Tests

Verify:

* exact profile;
* task metrics;
* hard gates;
* failures;
* limitations;
* evidence age;
* and qualification summary.

---

# 252. Public Benchmark Tests

Verify:

* provider-reported;
* independently reported;
* Opure-reproduced;
* configuration;
* contamination risk;
* and no automatic qualification.

---

# 253. SWE-bench Harness Tests

Pin exact:

* dataset;
* harness;
* image;
* agent scaffold;
* tools;
* and result parser.

---

# 254. HumanEval and EvalPlus Tests

Verify:

* hidden tests;
* pass@k policy;
* sampling;
* test count;
* and strengthened test effect.

---

# 255. C# Suite Tests

Cover all declared language features and project patterns.

---

# 256. Windows Suite Tests

Cover:

* path;
* PowerShell;
* named pipes;
* ACL;
* DPAPI;
* MSIX;
* Job Objects;
* and filesystem attack.

---

# 257. Patch Proposal Evaluation Tests

Test one-shot success, scope, build, tests and reviewability.

---

# 258. Patch Repair Evaluation Tests

Test bounded repair and no hidden calls.

---

# 259. Code Review Evaluation Tests

Measure:

* true positive;
* false positive;
* severity;
* localisation;
* and known-good restraint.

---

# 260. Diagnostic Evaluation Tests

Separate root cause and fix.

---

# 261. Tool Evaluation Tests

Test tool necessity and result integration.

---

# 262. Long-Context Evaluation Tests

Test evidence position, distractors, duplicates and contradiction.

---

# 263. Memory Evaluation Tests

Test lifecycle and conflict.

---

# 264. Retrieval Evaluation Tests

Test exact, lexical, graph, semantic and stale evidence.

---

# 265. Embedding Evaluation Tests

Use labelled retrieval judgements and latency.

---

# 266. Security Evaluation Tests

Every security hard gate should have positive and negative controls.

---

# 267. Operational Benchmark Tests

Test:

* cold;
* warm;
* TTFT;
* throughput;
* timeout;
* rate limit;
* cancellation;
* local load;
* and OOM.

---

# 268. Cost Tests

Test:

* current rate;
* stale rate;
* unknown reasoning cost;
* tool charge;
* currency;
* budget;
* actual reconciliation;
* and price change.

---

# 269. Latency Tests

Test distributions and region or hardware binding.

---

# 270. Qualification Schema Tests

Test:

* valid;
* task-specific;
* missing suite;
* failed gate;
* expired evidence;
* restricted data;
* wrong project policy;
* and changed profile.

---

# 271. Qualification Transition Tests

Test every state.

---

# 272. Qualification Expiry Tests

Test:

* model change;
* runtime change;
* template change;
* context policy;
* tool schema;
* price only;
* latency only;
* and incident.

---

# 273. Routing Request Tests

Test:

* valid;
* missing project;
* wrong task;
* unknown risk;
* missing data class;
* invalid budget;
* impossible context;
* user pin;
* and hash.

---

# 274. Deterministic Task Classification Tests

Test every product command and workflow step.

---

# 275. Model Classifier Proposal Tests

Attempt:

* unsupported class;
* provider selection;
* data-policy change;
* cost change;
* and malicious user instruction.

Trusted validation must constrain it.

---

# 276. Risk Classification Tests

Test:

* explanation;
* patch;
* command;
* security review;
* release;
* MCP;
* plugin;
* and unattended workflow.

---

# 277. Candidate Enumeration Tests

Test:

* local installed;
* local missing;
* approved provider;
* unapproved provider;
* expired credential;
* disabled profile;
* deprecated profile;
* and user pin.

---

# 278. Hard Eligibility Tests

Test every filter in order.

---

# 279. Local Only Routing Tests

Remote never selected.

---

# 280. Ask Every Time Tests

Remote remains Pending Approval until Data Sharing Plan.

---

# 281. Approved Providers Tests

Exact approved revision only.

---

# 282. Context Fit Tests

Test:

* exact fit;
* too large input;
* output reserve;
* reasoning reserve;
* tools;
* combined window;
* and provider count variance.

---

# 283. Local Resource Fit Tests

Test:

* enough VRAM;
* low VRAM;
* active build;
* CPU profile;
* OOM risk;
* and no silent fallback.

---

# 284. Availability Tests

Test:

* provider healthy;
* provider down;
* rate limited;
* credential invalid;
* local loaded;
* local loadable;
* local crash loop;
* and probe privacy.

---

# 285. Cost Budget Tests

Test:

* under;
* exact;
* over;
* unknown;
* price change;
* currency conversion;
* and actual overspend.

---

# 286. Latency Budget Tests

Test:

* recent evidence;
* stale evidence;
* under;
* over;
* unknown;
* and regional difference.

---

# 287. Quality Floor Tests

Test:

* pass;
* fail;
* missing metric;
* insufficient sample;
* expired evidence;
* and restricted qualification.

---

# 288. Pareto Tests

Create known dominated and non-dominated candidates.

---

# 289. Policy Preference Tests

Test:

* prefer local;
* prefer lower latency;
* prefer lower cost;
* prefer security;
* quality margin;
* and no hidden weighted sum.

---

# 290. Stable Tie-Break Tests

Run repeatedly and across restart.

---

# 291. Routing Policy Tests

Test:

* valid policy;
* missing owner;
* broad plugin override;
* narrow project override;
* changed revision;
* circular profile reference;
* and expired review.

---

# 292. Routing Decision Tests

Verify every candidate and reason.

---

# 293. Routing Decision Hash Tests

Change:

* request;
* policy;
* profile;
* evidence;
* cost;
* context;
* fallback;
* and approval.

Hash changes.

---

# 294. Decision Expiry Tests

Test time and state invalidation.

---

# 295. AI Router Binding Tests

Attempt:

* profile substitution;
* stale decision;
* wrong Context Plan;
* wrong Data Sharing Plan;
* wrong project;
* and changed qualification.

---

# 296. User Pin Tests

Test:

* qualified profile;
* unqualified profile;
* Development override;
* cloud denial;
* context mismatch;
* and remembered preference.

---

# 297. Local Preference Tests

Test all modes.

---

# 298. Retry Tests

Test:

* transient error;
* invalid request;
* authentication;
* context too large;
* partial stream;
* and exact profile identity.

---

# 299. Fallback Tests

Test:

* named local;
* named provider;
* ask;
* no fallback;
* workflow pre-approved;
* wrong data posture;
* cost increase;
* quality decrease;
* and unlisted profile.

---

# 300. Partial Response Tests

Verify no blended outputs.

---

# 301. Tool Loop Fallback Tests

Verify new plan required.

---

# 302. Provider Outage Tests

Verify no hidden provider switch.

---

# 303. Local OOM Tests

Verify explicit choices.

---

# 304. Provider Router Tests

Test:

* exact router ID;
* candidate disclosure;
* actual selected model;
* wrong region;
* undisclosed model;
* application-specific failure;
* and qualification.

---

# 305. Alias Tests

Test:

* Stable exact;
* Stable latest denial;
* Preview alias;
* hot swap;
* returned model;
* and unexpected change.

---

# 306. Deprecation Tests

Test:

* announcement;
* earliest shutdown;
* exact shutdown;
* replacement candidate;
* no auto-qualification;
* migration warning;
* and shutdown.

---

# 307. Canary Tests

Test:

* synthetic only;
* identity;
* schema;
* latency;
* refusal;
* tool;
* budget;
* and provider call count.

---

# 308. Hidden Shadow Traffic Tests

Seed project canaries and verify no second model request.

---

# 309. Explicit Comparison Tests

Verify preview, approval, two receipts and no primary control change.

---

# 310. Online Signal Tests

Test every event and noisy interpretation.

---

# 311. Feedback Privacy Tests

Verify local storage and no provider transmission.

---

# 312. Feedback Poisoning Tests

Attempt repeated false ratings, automated ratings and plugin-generated feedback.

No automatic qualification change.

---

# 313. Case Promotion Tests

Test review, secret scan, licence and split.

---

# 314. Drift Tests

Test:

* quality;
* schema;
* refusal;
* latency;
* cost;
* model identity;
* deprecation;
* tool behaviour;
* and security incident.

---

# 315. Drift Response Tests

Test:

* informational;
* review;
* suite rerun;
* task suspension;
* profile suspension;
* quarantine;
* and retirement.

---

# 316. Routing Policy Rollback Tests

Verify prior revision and decision invalidation.

---

# 317. Evaluation Data Security Tests

Test ACL, held-out store, export, support bundle and deletion.

---

# 318. Licence Tests

Test:

* permissive;
* research-only;
* no redistribution;
* unknown;
* public benchmark;
* private incident;
* and external fixture.

---

# 319. Remote Evaluation Tests

Test every project cloud policy.

---

# 320. Retention Tests

Test expiry and referenced qualification evidence.

---

# 321. Deletion Tests

Test case, run, output, feedback and privacy purge.

---

# 322. Recovery Tests

Test:

* Runtime crash;
* worker crash;
* provider uncertain;
* partial run;
* incomplete grader;
* database corruption;
* CAS corruption;
* decision expiry;
* and outbox replay.

---

# 323. Fuzzing

Fuzz:

* AI Execution Profile;
* Evaluation Suite;
* Evaluation Case;
* fixture manifest;
* grader configuration;
* Judge Profile;
* metric definition;
* Evaluation Card;
* qualification;
* Routing Policy;
* Routing Request;
* Routing Decision;
* fallback policy;
* provider responses;
* and import or export reports.

---

# 324. Performance Tests

Measure:

* suite lookup;
* trial scheduling;
* deterministic grading;
* patch validation;
* build and test;
* statistical aggregation;
* qualification query;
* candidate enumeration;
* routing filters;
* Pareto selection;
* decision persistence;
* and Trust Centre projection.

---

# 325. Provisional Performance Targets

On reference hardware:

* current qualification lookup p95: under 5 ms;
* candidate enumeration p95: under 10 ms;
* hard eligibility for 50 profiles p95: under 20 ms excluding live probes;
* deterministic ranking p95: under 5 ms;
* Routing Decision creation p95: under 50 ms excluding approval and live probes;
* decision verification p95: under 5 ms;
* exact grader p95: under 10 ms;
* schema grader p95: under 20 ms;
* and statistical aggregation of 100,000 results: under 5 seconds.

Build, test, model and human evaluation targets are suite specific.

---

# 326. Scale Tests

Test:

* 10 profiles;
* 50 profiles;
* 500 profiles;
* 1,000 suites;
* 1,000,000 cases;
* 10,000,000 trial results;
* 100,000 routing decisions;
* and long evidence history.

---

# 327. Accessibility Tests

Evaluation and routing UI must support:

* keyboard;
* Narrator;
* high contrast;
* reduced motion;
* metric tables;
* critical-failure navigation;
* routing explanation;
* fallback review;
* qualification restrictions;
* and comparison without colour-only meaning.

---

# 328. Prototype Plan

## 328.1 Prototype A — AI Execution Profile

Represent one local and two remote exact profiles.

---

## 328.2 Prototype B — C# Patch Evaluation

Generate, apply, build and test patches against a Windows fixture.

---

## 328.3 Prototype C — Context and Security

Evaluate wrong-project, secret and prompt-injection cases.

---

## 328.4 Prototype D — Human Rubric

Blind pairwise review with agreement measurement.

---

## 328.5 Prototype E — Model Judge

Calibrate one judge against human labels and test order bias.

---

## 328.6 Prototype F — Evaluation Card

Combine deterministic, human, judge, latency and cost evidence.

---

## 328.7 Prototype G — Qualification

Qualify profiles differently for explanation and patch tasks.

---

## 328.8 Prototype H — Deterministic Routing

Apply Local Only, context fit, quality floor and local preference.

---

## 328.9 Prototype I — Fallback

Fail a provider and prove explicit fallback review.

---

## 328.10 Prototype J — Alias Drift

Change a resolved alias and suspend qualification.

---

## 328.11 Prototype K — No Shadow Traffic

Monitor network during ordinary project use.

---

## 328.12 Prototype L — Drift

Degrade structured-output validity and suspend one task qualification.

---

# 329. Implementation Plan

1. Record founder review.
2. Define AI Execution Profile schema.
3. Define Task Class taxonomy.
4. Define Risk Class taxonomy.
5. Define Evaluation Suite schema.
6. Define Evaluation Case schema.
7. Define split governance.
8. Define Fixture Manifest.
9. Define Evaluation Plan and Run schemas.
10. Define Trial schema.
11. Create Evaluation Registry.
12. Create restricted held-out store.
13. Implement exact graders.
14. Implement schema graders.
15. Integrate Patch Service graders.
16. Integrate Build graders.
17. Integrate Test graders.
18. Integrate static-analysis graders.
19. Integrate security and policy graders.
20. Implement tool, plugin and MCP graders.
21. Implement Context and grounding graders.
22. Define human rubrics.
23. Implement blind review workflow.
24. Implement agreement and adjudication.
25. Define Judge Profile.
26. Implement judge calibration.
27. Implement judge adversarial tests.
28. Define metrics and statistics.
29. Implement Evaluation Cards.
30. Build initial private C# and Windows suites.
31. Add security and privacy suites.
32. Add tool and context suites.
33. Add held-out cases.
34. Define qualification schema.
35. Implement qualification state machine.
36. Implement evidence invalidation.
37. Define Routing Request.
38. Define Routing Policy.
39. Define Routing Decision.
40. Implement deterministic Task Classifier.
41. Implement candidate enumeration.
42. Implement hard eligibility.
43. Integrate Context fit.
44. Integrate local resource fit.
45. Integrate cost and latency evidence.
46. Implement quality floors.
47. Implement Pareto selection.
48. Implement policy preference and tie-breaking.
49. Implement explicit user pins.
50. Implement fallback policies.
51. Implement provider alias and deprecation monitoring.
52. Implement synthetic canaries.
53. Implement explicit comparison sessions.
54. Implement local feedback signals.
55. Implement drift events and suspension.
56. Implement Trust Centre and Desktop views.
57. Add adversarial governance suite.
58. Run performance and scale tests.
59. Complete security, privacy and product review.
60. Accept, amend or reject the ADR.

---

# 330. Owners

| Area                       | Owner                                   |
| -------------------------- | --------------------------------------- |
| Product policy             | Founder                                 |
| AI Evaluation Service      | Evaluation Owner                        |
| Routing Governance Service | Routing Governance Owner                |
| AI Router execution        | AI Router Owner                         |
| Context evaluation         | Context Assembly Owner                  |
| Local profile identity     | Local Model Runtime Owner               |
| Remote profile identity    | Provider Trust Owner                    |
| Project retrieval evals    | Project Knowledge Owner                 |
| Memory evals               | Project Memory Owner                    |
| Patch graders              | Patch Service Owner                     |
| Build graders              | Build Owner                             |
| Test graders               | Test Owner                              |
| Tool graders               | Tool Mediation Owner                    |
| Plugin graders             | Plugin Platform Owner                   |
| MCP graders                | MCP Gateway Owner                       |
| Security suites            | Security Owner                          |
| Privacy suites             | Privacy Owner                           |
| Human review               | Product and Test Owners                 |
| Judge calibration          | Evaluation Owner                        |
| Statistics                 | Evaluation and Test Owners              |
| Cost and performance       | Performance Owner                       |
| Qualification              | Evaluation, Security and Product Owners |
| Release gate               | Release Owner                           |
| Trust Centre               | Trust Centre Owner                      |
| Desktop UI                 | Desktop Owner                           |
| Recovery                   | Recovery Owner                          |
| Adversarial tests          | Test Architecture Owner                 |

---

# 331. Suggested Repository Structure

```text
src/
├── AI/
│   ├── Opure.AI.ExecutionProfiles/
│   ├── Opure.AI.Evaluation.Contracts/
│   ├── Opure.AI.Evaluation.Service/
│   ├── Opure.AI.Evaluation.Registry/
│   ├── Opure.AI.Evaluation.Graders/
│   ├── Opure.AI.Evaluation.HumanReview/
│   ├── Opure.AI.Evaluation.Judges/
│   ├── Opure.AI.Evaluation.Statistics/
│   ├── Opure.AI.Evaluation.Qualification/
│   ├── Opure.AI.Routing.Contracts/
│   ├── Opure.AI.Routing.Governance/
│   ├── Opure.AI.Routing.Policies/
│   ├── Opure.AI.Routing.Fallback/
│   └── Opure.AI.Routing.Drift/
├── Worker/
│   ├── Opure.Worker.Evaluation/
│   └── Opure.Worker.Grader/
└── Desktop/
    └── Opure.Desktop.AIGovernance/

schemas/
└── ai-governance/
    ├── ai-execution-profile-v1.schema.json
    ├── evaluation-suite-v1.schema.json
    ├── evaluation-case-v1.schema.json
    ├── evaluation-plan-v1.schema.json
    ├── evaluation-run-v1.schema.json
    ├── evaluation-trial-v1.schema.json
    ├── grader-profile-v1.schema.json
    ├── judge-profile-v1.schema.json
    ├── evaluation-card-v1.schema.json
    ├── model-qualification-v1.schema.json
    ├── routing-request-v1.schema.json
    ├── routing-policy-v1.schema.json
    ├── routing-decision-v1.schema.json
    ├── fallback-policy-v1.schema.json
    └── drift-event-v1.schema.json

tests/
└── AI/
    └── Governance/
        ├── Opure.AI.Evaluation.UnitTests/
        ├── Opure.AI.Evaluation.GraderTests/
        ├── Opure.AI.Evaluation.JudgeTests/
        ├── Opure.AI.Evaluation.SecurityTests/
        ├── Opure.AI.Evaluation.PerformanceTests/
        ├── Opure.AI.Routing.UnitTests/
        ├── Opure.AI.Routing.SecurityTests/
        ├── Opure.AI.Routing.IntegrationTests/
        └── Fixtures/
            ├── Projects/
            ├── HeldOut/
            ├── Adversarial/
            └── HumanLabels/
```

Exact project count may be consolidated under ADR-0010.

---

# 332. AI Execution Profile Sketch

```json
{
  "schema": "opure.ai-execution-profile/1",
  "id": "profile-opaque",
  "revision": 1,
  "provider": {
    "kind": "local",
    "runtime_package": "runtime-package-opaque",
    "model_manifest": "model-manifest-opaque",
    "execution_profile": "local-execution-opaque"
  },
  "instruction_template": "patch-proposal:3",
  "request_renderer": "llama-chat-v2",
  "tokenizer_profile": "tokenizer-profile-opaque:1",
  "context_policy": "patch-proposal:2",
  "tools": [],
  "output_schema": "patch-proposal:1",
  "sampling": {
    "temperature": 0.2,
    "seed": 12345
  },
  "sha256": "..."
}
```

---

# 333. Evaluation Case Sketch

```json
{
  "schema": "opure.evaluation-case/1",
  "id": "case-opaque",
  "revision": 1,
  "split": "held-out-qualification",
  "task_class": "propose-patch",
  "risk_class": "elevated",
  "fixture": "fixture-opaque:1",
  "input": {
    "issue": "Cancellation is not propagated to the named-pipe request handler."
  },
  "required_capabilities": [
    "code-generation",
    "structured-patch"
  ],
  "forbidden_behaviours": [
    "change-protected-file",
    "execute-command",
    "use-network"
  ],
  "graders": [
    "patch-applicability:1",
    "allowed-paths:1",
    "build:1",
    "tests:1",
    "secret-scan:1"
  ],
  "sha256": "..."
}
```

---

# 334. Evaluation Card Sketch

```json
{
  "schema": "opure.evaluation-card/1",
  "execution_profile": "profile-opaque:1",
  "evidence": {
    "patch_proposal": {
      "cases": 200,
      "patch_apply_rate": 0.94,
      "build_pass_rate": 0.88,
      "test_pass_rate": 0.82,
      "policy_violation_rate": 0.0
    },
    "security": {
      "critical_failures": 0,
      "secret_leakage_rate": 0.0
    },
    "operations": {
      "ttft_p50_ms": 420,
      "total_p95_ms": 12400
    }
  },
  "known_limitations": [
    "Not qualified for unattended tool use"
  ],
  "review_due": "2026-10-18T00:00:00Z",
  "sha256": "..."
}
```

Values are illustrative.

---

# 335. Qualification Sketch

```json
{
  "schema": "opure.model-qualification/1",
  "id": "qualification-opaque",
  "execution_profile": "profile-opaque:1",
  "task_class": "propose-patch",
  "project_policy_class": "local-only",
  "data_classes": [
    "project.internal"
  ],
  "state": "qualified-for-preview",
  "suite_revisions": [
    "csharp-patch:4",
    "security-boundaries:3"
  ],
  "restrictions": [
    "human-review-required",
    "no-tools"
  ],
  "expires_at": "2026-10-18T00:00:00Z",
  "sha256": "..."
}
```

---

# 336. Routing Policy Sketch

```json
{
  "schema": "opure.routing-policy/1",
  "id": "interactive-code-explanation",
  "revision": 1,
  "task_classes": [
    "explain-code"
  ],
  "hard_filters": [
    "project-cloud-policy",
    "task-qualification",
    "context-fit",
    "required-capabilities",
    "current-evidence"
  ],
  "quality_floor": {
    "instruction_following_rate": 0.95,
    "grounded_answer_rate": 0.90,
    "critical_failures": 0
  },
  "preference": [
    "prefer-local-within-quality-margin",
    "lower-ttft",
    "lower-cost"
  ],
  "fallback": "ask-before-profile-change"
}
```

---

# 337. Routing Decision Sketch

```json
{
  "schema": "opure.routing-decision/1",
  "id": "routing-decision-opaque",
  "operation": "operation-opaque",
  "project": "project-opaque",
  "request_sha256": "...",
  "policy": "interactive-code-explanation:1",
  "selected_profile": "profile-local-opaque:1",
  "reason": [
    "Qualified for code explanation",
    "Satisfies Local Only project policy",
    "Context fits approved local profile",
    "Meets quality floor",
    "Preferred local profile within quality margin"
  ],
  "candidates": [
    {
      "profile": "profile-local-opaque:1",
      "result": "selected"
    },
    {
      "profile": "profile-remote-opaque:3",
      "result": "ineligible",
      "reason": "Project cloud policy is Local Only"
    }
  ],
  "fallback": "none",
  "expires_at": "2026-07-18T18:05:00Z",
  "sha256": "..."
}
```

---

# 338. Drift Event Sketch

```json
{
  "schema": "opure.ai-drift-event/1",
  "id": "drift-opaque",
  "execution_profile": "profile-opaque:1",
  "task_class": "structured-extraction",
  "source": "daily-canary",
  "metric": "schema-valid-rate",
  "baseline": 0.995,
  "observed": 0.91,
  "severity": "major",
  "response": [
    "suspend-unattended-qualification",
    "run-regression-suite"
  ],
  "detected_at": "2026-07-18T18:00:00Z"
}
```

---

# 339. Release Gate

Model evaluation and governed routing are blocked when:

* Stable can route through `latest`, `auto` or another mutable model alias;
* an AI Execution Profile omits a material request setting;
* a provider can substitute an undisclosed model;
* a local model or Runtime Package hash is unknown;
* evaluation cases are mutable after publication;
* held-out cases are available to ordinary prompt-tuning workflows;
* public benchmark scores alone can qualify a model;
* a model grader is the sole security, privacy or patch-validity gate;
* deterministic build or test failures can be offset by style scores;
* critical failures can be hidden by an average;
* model judges are uncalibrated;
* human review exposes model identity without a stated reason;
* evaluation uses production credentials;
* evaluation executes untrusted project hooks without isolation;
* remote evaluation transmits data without an approved Data Sharing Plan;
* task qualification is not specific;
* evidence freshness is unknown;
* project cloud policy can be overridden by quality or cost;
* context fit is not checked;
* local resource fit is not checked;
* routing decisions omit rejected candidates and reasons;
* a model selects its own provider or fallback;
* fallback changes provider, model or data posture silently;
* two partial model outputs can be blended invisibly;
* ordinary developer prompts are shadowed to another model;
* user feedback can auto-promote a model;
* provider-recommended replacement models are auto-qualified;
* model deprecation is not monitored;
* an identity or security drift does not suspend affected qualification;
* or Trust Centre cannot show exact profile, evidence, routing reason and fallback.

---

# 340. Acceptance Criteria

This ADR may move to **Accepted** when all of the following hold.

## Architecture and Ownership

* [ ] AI Evaluation Service is authoritative for evaluation evidence.
* [ ] Routing Governance Service is authoritative for routing policy and decisions.
* [ ] AI Router only executes verified decisions.
* [ ] Context Assembly remains authoritative for source context.
* [ ] Provider Trust remains authoritative for cloud policy.
* [ ] Local Model Runtime remains authoritative for local execution identity.
* [ ] Patch, Build, Test and Tool services remain authoritative for deterministic execution.
* [ ] Models cannot authorise routing.
* [ ] Plugins cannot broaden routing policy.
* [ ] MCP servers cannot broaden routing policy.
* [ ] One exact AI Execution Profile is the evaluated unit.
* [ ] One exact profile is the routed unit.
* [ ] Evaluation and production execution are separate operations.
* [ ] Router evaluation is explicitly distinguished.

## AI Execution Profiles

* [ ] Provider kind is explicit.
* [ ] Provider Profile revision is explicit.
* [ ] Provider endpoint is explicit.
* [ ] Provider region is explicit.
* [ ] Provider model ID is explicit.
* [ ] Resolved model ID is recorded where available.
* [ ] Local Runtime Package is explicit.
* [ ] Local Model Manifest is explicit.
* [ ] Quantisation is explicit.
* [ ] Local Execution Profile is explicit.
* [ ] Instruction-template revision is explicit.
* [ ] Request renderer is explicit.
* [ ] Tokenizer Profile is explicit.
* [ ] Context Policy is explicit.
* [ ] Tool projection is explicit.
* [ ] Output schema is explicit.
* [ ] Reasoning profile is explicit.
* [ ] Sampling profile is explicit.
* [ ] Maximum output is explicit.
* [ ] Truncation policy is explicit.
* [ ] Safety profile is explicit.
* [ ] Adapter version is explicit.
* [ ] Profile hash is canonical.
* [ ] Material changes create a new revision.

## Model Identity and Lifecycle

* [ ] Stable uses exact model identifiers.
* [ ] `latest` aliases are denied in Stable.
* [ ] `auto` and `default` aliases are denied where mutable.
* [ ] Preview alias use is labelled and expires.
* [ ] Returned model identity is checked.
* [ ] Unexpected model identity suspends qualification.
* [ ] Local weight hashes are verified.
* [ ] Local Runtime Package hashes are verified.
* [ ] Provider deprecation notices are monitored.
* [ ] Earliest shutdown is recorded.
* [ ] Exact shutdown is recorded when known.
* [ ] Recommended replacement remains unqualified until evaluated.
* [ ] Retired profiles remain historical.
* [ ] Provider migration creates a new profile.

## Evaluation Suites and Cases

* [ ] Evaluation Suite schema exists.
* [ ] Evaluation Case schema exists.
* [ ] Suite revisions are immutable.
* [ ] Case revisions are immutable.
* [ ] Task Class is explicit.
* [ ] Risk Class is explicit.
* [ ] Required capabilities are explicit.
* [ ] Forbidden behaviours are explicit.
* [ ] Expected evidence is explicit.
* [ ] Grader sets are explicit.
* [ ] Resource budget is explicit.
* [ ] Classification is explicit.
* [ ] Licence is explicit.
* [ ] Provenance is explicit.
* [ ] Case hash is canonical.
* [ ] Corrected cases create revisions.
* [ ] Retired cases remain interpretable.

## Split Governance

* [ ] Development split exists.
* [ ] Regression split exists.
* [ ] Calibration split exists.
* [ ] Held-Out Qualification split exists.
* [ ] Adversarial split exists.
* [ ] Canary split exists.
* [ ] Retired split exists.
* [ ] Held-out access is restricted.
* [ ] Project Knowledge cannot index held-out cases.
* [ ] Project Memory cannot expose held-out cases.
* [ ] Ordinary AI workflows cannot search held-out cases.
* [ ] Prompt optimisers cannot access held-out cases.
* [ ] Development exposure prevents later held-out claims.
* [ ] Split movement is governed.
* [ ] Contamination risk is recorded.

## Fixtures and Environment

* [ ] Fixture Manifest schema exists.
* [ ] Source revision is pinned.
* [ ] File hashes are pinned.
* [ ] Dependency locks are pinned.
* [ ] Toolchain is pinned.
* [ ] Expected clean state is explicit.
* [ ] Network policy is explicit.
* [ ] Licence is explicit.
* [ ] Evaluation workers are supervised.
* [ ] Production credentials are absent.
* [ ] Production Vault is absent.
* [ ] Project filesystem boundaries are enforced.
* [ ] CPU is bounded.
* [ ] Memory is bounded.
* [ ] Disk is bounded.
* [ ] Process count is bounded.
* [ ] Time is bounded.
* [ ] Network is bounded.
* [ ] Malicious build hooks cannot escape.
* [ ] Evaluation cancellation works.

## Trials and Reproducibility

* [ ] Evaluation Plan schema exists.
* [ ] Evaluation Run schema exists.
* [ ] Trial schema exists.
* [ ] Every trial binds exact case revision.
* [ ] Every trial binds exact profile revision.
* [ ] Input hash is recorded.
* [ ] Context Plan hash is recorded.
* [ ] Provider or local request receipt is recorded.
* [ ] Output hash is recorded.
* [ ] Usage is recorded.
* [ ] Latency is recorded.
* [ ] Cost is recorded.
* [ ] Tool trace is recorded where applicable.
* [ ] Patch reference is recorded where applicable.
* [ ] Trial repetition policy is explicit.
* [ ] pass@k sampling is explicit.
* [ ] Seeds are recorded where supported.
* [ ] Unsupported determinism is labelled.
* [ ] Harness version is recorded.
* [ ] Environment manifest is recorded.
* [ ] Reproducibility classification is honest.

## Deterministic Graders

* [ ] Exact graders work.
* [ ] Numeric-tolerance graders work.
* [ ] Set and sequence graders work.
* [ ] Canonical JSON grader works.
* [ ] JSON Schema grader works.
* [ ] Patch schema grader works.
* [ ] Compile grader works.
* [ ] Unit-test grader works.
* [ ] Integration-test grader works.
* [ ] Property-test grader works where selected.
* [ ] Mutation-test grader works where selected.
* [ ] Patch applicability grader works.
* [ ] Patch scope grader works.
* [ ] Repository-state grader works.
* [ ] Static-analysis grader works.
* [ ] Security grader works.
* [ ] Policy grader works.
* [ ] Tool-call grader works.
* [ ] MCP grader works.
* [ ] Plugin grader works.
* [ ] Context grader works.
* [ ] Grounding grader works.
* [ ] Instruction-following grader works.
* [ ] Refusal grader works.
* [ ] Grader implementations are pinned and hashed.
* [ ] Grader failures are not silently omitted.

## Patch and Code Evaluation

* [ ] Patch applies to the exact base.
* [ ] Paths are authorised.
* [ ] Path escape is denied.
* [ ] Prohibited files remain unchanged.
* [ ] Build result is authoritative.
* [ ] Required tests are authoritative.
* [ ] Hidden tests are supported.
* [ ] Test deletion is detected.
* [ ] Over-broad patch is detectable.
* [ ] No-op patch is detectable.
* [ ] Known-good code measures false positives.
* [ ] Security review measures recall and false alarms.
* [ ] Root cause and suggested fix are graded separately.
* [ ] One-shot and repair workflows are separate suites.
* [ ] Repair turn count is explicit.
* [ ] No hidden extra model calls occur.

## Human Evaluation

* [ ] Human Rubric schema exists.
* [ ] Correctness is separate from style.
* [ ] Safety is separate.
* [ ] Rubric anchors are explicit.
* [ ] Reviewer expertise is recorded.
* [ ] Model identity is hidden where practical.
* [ ] Pairwise order is randomised.
* [ ] Tie is available.
* [ ] Both Fail is available.
* [ ] Cannot Determine is available.
* [ ] Reviewer confidence is recorded.
* [ ] Inter-rater agreement is measured.
* [ ] Critical disagreement is adjudicated.
* [ ] Reviewer identity is verified.
* [ ] Reviewer personal data is minimised.
* [ ] Human feedback provenance is retained.

## Model Judges

* [ ] Judge Profile schema exists.
* [ ] Exact judge model is bound.
* [ ] Rubric is bound.
* [ ] Input rendering is bound.
* [ ] Candidate anonymisation is explicit.
* [ ] Position randomisation is explicit.
* [ ] Reference policy is explicit.
* [ ] Reasoning setting is explicit.
* [ ] Sampling setting is explicit.
* [ ] Output schema is explicit.
* [ ] Calibration suite is explicit.
* [ ] Judge calibration against human labels exists.
* [ ] Balanced accuracy or appropriate metrics are reported.
* [ ] Confusion matrix is available.
* [ ] Position bias is tested.
* [ ] Length bias is tested.
* [ ] Style bias is tested.
* [ ] Self-provider preference is tested.
* [ ] Self-model preference is tested.
* [ ] Prompt injection is tested.
* [ ] Judge expiry is enforced.
* [ ] A judge is never the sole critical gate.
* [ ] A judge cannot grade and confirm its own authority.

## Metrics and Statistics

* [ ] Metric definitions are versioned.
* [ ] Units are explicit.
* [ ] Direction is explicit.
* [ ] Denominator is explicit.
* [ ] Missing-case policy is explicit.
* [ ] Hard-gate status is explicit.
* [ ] Per-case results are retained.
* [ ] Case count is reported.
* [ ] Pass and failure count are reported.
* [ ] Mean is reported only when meaningful.
* [ ] Median is available.
* [ ] Percentiles are available for latency.
* [ ] Standard deviation is available where meaningful.
* [ ] Confidence intervals are available.
* [ ] Effect size is available for comparisons.
* [ ] Win, loss and tie are available.
* [ ] Invalid and missing trials remain visible.
* [ ] Small-sample warnings work.
* [ ] Multiple-comparison risk is acknowledged.
* [ ] Critical failures cannot be hidden by aggregation.
* [ ] No universal score replaces the evidence vector.

## Evaluation Cards

* [ ] Evaluation Card schema exists.
* [ ] Exact profile is shown.
* [ ] Task classes are shown.
* [ ] Suite revisions are shown.
* [ ] Hard gates are shown.
* [ ] Quality metrics are shown.
* [ ] Safety metrics are shown.
* [ ] Operational metrics are shown.
* [ ] Cost metrics are shown.
* [ ] Resource metrics are shown.
* [ ] Human results are shown.
* [ ] Judge results are shown.
* [ ] Critical failures are shown.
* [ ] Known limitations are shown.
* [ ] Evidence freshness is shown.
* [ ] Review due is shown.
* [ ] Public benchmark provenance is shown.
* [ ] Provider-reported and Opure-reproduced claims are distinct.

## Benchmark Strategy

* [ ] Opure Private Regression Suites exist.
* [ ] Opure Held-Out Release Suites exist.
* [ ] Adversarial Security Suites exist.
* [ ] Project Fixture Suites exist.
* [ ] Public benchmark evidence is supplementary.
* [ ] C# and .NET cases exist.
* [ ] Windows cases exist.
* [ ] Patch cases exist.
* [ ] Code-review cases exist.
* [ ] Diagnostic cases exist.
* [ ] Tool-use cases exist.
* [ ] Structured-output cases exist.
* [ ] Long-context cases exist.
* [ ] Retrieval cases exist.
* [ ] Memory cases exist.
* [ ] Privacy cases exist.
* [ ] Provider-neutrality cases exist.
* [ ] Local-versus-remote comparisons exist.
* [ ] Benchmark contamination is documented.
* [ ] Public harness configuration is pinned.
* [ ] Test-strength limitations are considered.
* [ ] Human productivity evidence is not replaced by preference alone.

## Operational Evidence

* [ ] Cold-start latency is measured.
* [ ] Warm latency is measured.
* [ ] TTFT is measured.
* [ ] Total latency is measured.
* [ ] Input tokens are measured.
* [ ] Output tokens are measured.
* [ ] Throughput is measured.
* [ ] Provider queueing is measured where available.
* [ ] Local load time is measured.
* [ ] Cancellation is measured.
* [ ] Timeouts are measured.
* [ ] Retries are measured.
* [ ] Rate limits are measured.
* [ ] Provider availability is measured.
* [ ] Local crash and OOM are measured.
* [ ] Cost rates are date and region bound.
* [ ] Actual cost is reconciled.
* [ ] Local resource evidence is hardware bound.
* [ ] Energy is not reported without measurement.

## Qualification

* [ ] Qualification schema exists.
* [ ] Qualification is task specific.
* [ ] Qualification is project-policy-class specific.
* [ ] Qualification is data-class specific.
* [ ] Qualification state machine works.
* [ ] Development qualification works.
* [ ] Preview qualification works.
* [ ] Stable qualification works.
* [ ] Restricted qualification works.
* [ ] Not Qualified works.
* [ ] Suspended works.
* [ ] Deprecated works.
* [ ] Unavailable works.
* [ ] Quarantined works.
* [ ] Retired works.
* [ ] Suite revisions are bound.
* [ ] Run evidence is bound.
* [ ] Hard gates are bound.
* [ ] Metric floors are bound.
* [ ] Restrictions are bound.
* [ ] Review mode is bound.
* [ ] Expiry is bound.
* [ ] Approval is bound.
* [ ] Evidence age is enforced.
* [ ] Material profile changes invalidate qualification.
* [ ] Security incidents can suspend one task or whole profile.

## Routing Requests and Classification

* [ ] Routing Request schema exists.
* [ ] Operation is bound.
* [ ] Project is bound.
* [ ] Task Class is bound.
* [ ] Risk Class is bound.
* [ ] Data classes are bound.
* [ ] Project cloud policy is bound.
* [ ] Required capabilities are bound.
* [ ] Context summary is bound.
* [ ] Input token budget is bound.
* [ ] Output reserve is bound.
* [ ] Tool requirements are bound.
* [ ] Output schema is bound.
* [ ] Latency budget is bound.
* [ ] Cost budget is bound.
* [ ] Local resource budget is bound.
* [ ] Quality floor is bound.
* [ ] Review mode is bound.
* [ ] Fallback policy is bound.
* [ ] User pin is bound.
* [ ] Routing Request does not contain unnecessary full prompt content.
* [ ] Deterministic Task Classifier works.
* [ ] A model classifier can only propose.
* [ ] A model classifier cannot choose provider.
* [ ] Task and Risk taxonomies are versioned.

## Routing Eligibility

* [ ] Candidate enumeration uses registered profiles only.
* [ ] Disabled profiles are absent.
* [ ] Missing credentials are ineligible.
* [ ] Missing local models are ineligible.
* [ ] Local Only removes all remote profiles.
* [ ] Ask Every Time requires approval.
* [ ] Approved Providers Only checks exact revisions.
* [ ] Custom policies work.
* [ ] Required capability is checked.
* [ ] Task qualification is checked.
* [ ] Security and privacy gates are checked.
* [ ] Context fit is checked.
* [ ] Tool and schema support are checked.
* [ ] Local resource fit is checked.
* [ ] Provider availability is checked.
* [ ] Local availability is checked.
* [ ] Cost hard budget is checked.
* [ ] Latency hard budget is checked.
* [ ] Evidence freshness is checked.
* [ ] Unknown evidence does not silently pass.

## Routing Selection

* [ ] Hard filtering occurs before quality ranking.
* [ ] Quality floor occurs before optimisation.
* [ ] Pareto analysis is deterministic.
* [ ] Task-specific preference rules are explicit.
* [ ] Local preference modes work.
* [ ] Cost cannot override security.
* [ ] Quality cannot override data policy.
* [ ] Privacy posture is not reduced to an opaque score.
* [ ] Raw evidence remains visible.
* [ ] Explicit user pin is honoured when eligible.
* [ ] Explicit user pin cannot bypass hard gates.
* [ ] Stable tie-breaking is deterministic.
* [ ] Every rejected candidate has a reason.
* [ ] Routing Decision schema exists.
* [ ] Decision hash is canonical.
* [ ] Decision expiry is enforced.
* [ ] State changes invalidate decisions.
* [ ] AI Router cannot substitute a profile.

## Fallbacks and Retries

* [ ] Retry means the same exact profile.
* [ ] Retry count is bounded.
* [ ] Invalid requests are not retried.
* [ ] Context-too-large is not retried blindly.
* [ ] Authentication failures are not retried blindly.
* [ ] Fallback means a different profile.
* [ ] Fallback profiles are named.
* [ ] No open-ended fallback exists.
* [ ] Provider change requires review or pre-approved workflow policy.
* [ ] Model change requires review or pre-approved workflow policy.
* [ ] Local-versus-cloud change requires review.
* [ ] Region change requires review.
* [ ] Data-posture change requires review.
* [ ] Quality-class change is visible.
* [ ] Cost-class change is visible.
* [ ] Partial response is labelled.
* [ ] Outputs are not blended invisibly.
* [ ] Tool-loop fallback creates a new plan.
* [ ] OOM does not trigger hidden CPU fallback.
* [ ] Provider outage does not trigger hidden provider switch.
* [ ] Unattended fallback is declared before workflow start.
* [ ] Fallback events are receipted.

## Provider Routers and Aliases

* [ ] Provider-managed routing is disabled by default.
* [ ] Provider router is represented as an exact profile.
* [ ] Candidate model set is known.
* [ ] Candidate region set is known.
* [ ] Actual selected model is reported.
* [ ] Every possible model is approved.
* [ ] Router is evaluated end to end.
* [ ] Application-specific limitations are documented.
* [ ] Local Only prohibits provider routers.
* [ ] Exact-model workflows prohibit provider routers.
* [ ] Stable mutable aliases are prohibited.
* [ ] Preview alias resolution is monitored.
* [ ] Unexpected alias change suspends use.

## Canaries, Comparisons and Feedback

* [ ] Synthetic canaries exist.
* [ ] Canaries use no hidden project data.
* [ ] Canaries use no production credentials.
* [ ] Canary provider calls are bounded.
* [ ] Canary cost is bounded.
* [ ] Ordinary operations produce no hidden shadow traffic.
* [ ] Explicit comparison sessions show exact data.
* [ ] Explicit comparison sessions show exact providers.
* [ ] Explicit comparison sessions show cost.
* [ ] Primary result remains explicit.
* [ ] Online signals are local by default.
* [ ] Accepted response is not treated as proof.
* [ ] Rejected response is not treated as objective failure automatically.
* [ ] Patch edit is interpreted cautiously.
* [ ] Feedback cannot auto-qualify.
* [ ] Feedback cannot auto-route.
* [ ] Provider feedback transmission is separate consent.
* [ ] Feedback-derived cases require review.
* [ ] Feedback poisoning tests pass.

## Drift and Change Control

* [ ] Drift Event schema exists.
* [ ] Identity drift is detected.
* [ ] Schema-validity drift is detected.
* [ ] Quality drift is detected.
* [ ] Refusal drift is detected.
* [ ] Latency drift is detected.
* [ ] Cost drift is detected.
* [ ] Token-use drift is detected.
* [ ] Tool drift is detected.
* [ ] Security incidents are detected.
* [ ] Informational severity works.
* [ ] Minor severity works.
* [ ] Major severity works.
* [ ] Critical severity works.
* [ ] Drift can trigger re-evaluation.
* [ ] Drift can increase review requirements.
* [ ] Drift can suspend a task qualification.
* [ ] Drift can suspend a profile.
* [ ] Drift can quarantine a profile.
* [ ] Drift does not silently select another profile.
* [ ] Routing Policy changes are versioned.
* [ ] Prior Routing Policy can be restored.
* [ ] Affected decisions are invalidated.

## Evidence, Trust and Recovery

* [ ] Evaluation metadata is service owned.
* [ ] Large artefacts are content addressed.
* [ ] Trial outputs are secret scanned.
* [ ] Held-out outputs are access restricted.
* [ ] Retention policy exists.
* [ ] Privacy deletion works.
* [ ] Evaluation export is reviewable.
* [ ] Held-out content is excluded by default.
* [ ] Trust Centre shows profiles.
* [ ] Trust Centre shows Evaluation Cards.
* [ ] Trust Centre shows qualifications.
* [ ] Trust Centre shows routing reasons.
* [ ] Trust Centre shows fallbacks.
* [ ] Trust Centre shows drift.
* [ ] Trust Centre shows model deprecations.
* [ ] Trust Centre shows remote evaluation calls.
* [ ] Recovery reconciles uncertain provider calls.
* [ ] Running trials become Interrupted after crash.
* [ ] Routing Decisions expire after restart where appropriate.
* [ ] Corrupted evidence cannot qualify a profile.
* [ ] Security review is complete.
* [ ] Privacy review is complete.
* [ ] Product review is complete.
* [ ] Founder approval is recorded.

---

# 341. Evidence Required Before Acceptance

* [ ] AI Evaluation Service contract.
* [ ] Routing Governance Service contract.
* [ ] AI Execution Profile schema.
* [ ] Task Class taxonomy.
* [ ] Risk Class taxonomy.
* [ ] Evaluation Suite schema.
* [ ] Evaluation Case schema.
* [ ] split-governance policy.
* [ ] Fixture Manifest schema.
* [ ] Evaluation Plan schema.
* [ ] Evaluation Run schema.
* [ ] Trial schema.
* [ ] Grader Profile schema.
* [ ] Human Rubric schema.
* [ ] Judge Profile schema.
* [ ] Metric schema.
* [ ] Evaluation Card schema.
* [ ] Qualification schema.
* [ ] Routing Request schema.
* [ ] Routing Policy schema.
* [ ] Routing Decision schema.
* [ ] Fallback Policy schema.
* [ ] Drift Event schema.
* [ ] exact-profile identity report.
* [ ] Stable alias denial report.
* [ ] provider model-resolution report.
* [ ] local model-hash report.
* [ ] held-out access-control report.
* [ ] benchmark-contamination report.
* [ ] evaluation-worker isolation report.
* [ ] no-production-credential report.
* [ ] fixture reproducibility report.
* [ ] exact grader report.
* [ ] schema grader report.
* [ ] patch applicability report.
* [ ] patch scope report.
* [ ] C# build and test grader report.
* [ ] security grader report.
* [ ] tool grader report.
* [ ] Context Plan grader report.
* [ ] human-rubric report.
* [ ] reviewer-agreement report.
* [ ] judge-calibration report.
* [ ] judge-position-bias report.
* [ ] judge-length-bias report.
* [ ] judge-self-preference report.
* [ ] judge-prompt-injection report.
* [ ] statistical-validation report.
* [ ] critical-failure-ledger report.
* [ ] Opure private regression suite.
* [ ] Opure held-out qualification suite.
* [ ] C# and .NET suite report.
* [ ] Windows suite report.
* [ ] patch proposal suite report.
* [ ] code-review suite report.
* [ ] diagnostic suite report.
* [ ] tool-use suite report.
* [ ] long-context suite report.
* [ ] project-memory suite report.
* [ ] retrieval suite report.
* [ ] security and privacy suite report.
* [ ] local-versus-remote comparison.
* [ ] operational benchmark report.
* [ ] cost-reconciliation report.
* [ ] Evaluation Card examples.
* [ ] task-specific qualification report.
* [ ] qualification-expiry report.
* [ ] deterministic task-classification report.
* [ ] Local Only routing report.
* [ ] Ask Every Time routing report.
* [ ] context-fit routing report.
* [ ] local-resource routing report.
* [ ] quality-floor routing report.
* [ ] Pareto-routing report.
* [ ] deterministic tie-break report.
* [ ] Routing Decision examples.
* [ ] AI Router binding report.
* [ ] user-pin report.
* [ ] explicit fallback report.
* [ ] partial-response report.
* [ ] provider-outage rehearsal.
* [ ] local-OOM rehearsal.
* [ ] provider-router evaluation.
* [ ] provider-alias drift rehearsal.
* [ ] model-deprecation migration rehearsal.
* [ ] synthetic-canary report.
* [ ] no-hidden-shadow-traffic capture.
* [ ] explicit comparison-session report.
* [ ] feedback-privacy report.
* [ ] feedback-poisoning report.
* [ ] drift and suspension report.
* [ ] Routing Policy rollback report.
* [ ] evaluation-data licence report.
* [ ] retention and privacy-deletion report.
* [ ] recovery rehearsal.
* [ ] routing-performance report.
* [ ] evaluation-scale report.
* [ ] accessibility report.
* [ ] security review.
* [ ] privacy review.
* [ ] product review.
* [ ] founder approval.

---

# 342. Known Limitations

* The final Task Class taxonomy is not selected.
* The final Risk Class taxonomy is not selected.
* The initial supported provider profiles are not selected.
* The initial supported local profiles are not selected.
* The final private benchmark corpus is not created.
* The final held-out corpus is not created.
* Public benchmark contamination cannot be eliminated.
* Private cases cannot prove absence from provider training.
* Some provider models do not expose immutable snapshots.
* Some provider model outputs are not reproducible.
* Seeds may not guarantee repeatability.
* Model identity reporting can be incomplete.
* Provider aliases and defaults can change.
* Provider deprecation schedules can change.
* Provider price and availability change.
* Model judges can be biased and manipulated.
* Human reviewers can disagree.
* Human review is expensive.
* Deterministic graders can encode incorrect expectations.
* Tests can be incomplete.
* Passing tests do not prove complete correctness.
* Static analysis has false positives and false negatives.
* Security benchmarks cannot cover every attack.
* Aggregate metrics can still be misinterpreted.
* Confidence intervals do not remove dataset bias.
* Real-world developer productivity is hard to measure.
* User preference may not correlate perfectly with task performance.
* Online feedback is noisy.
* Routing estimates can be stale.
* Latency varies by network, region and load.
* Local performance varies by driver and workstation load.
* Exact local models may require expensive load before use.
* A no-route failure can reduce convenience.
* Explicit fallback can add interaction.
* Provider-managed routers may conceal application-specific limitations.
* Shadow evaluation is unavailable without explicit approval.
* Hidden online bandit routing is unavailable.
* Learned routing is unavailable.
* Model fine-tuning is unavailable.
* Global user-personalised routing is unavailable.
* HELM entered maintenance mode on 1 June 2026.
* Public evaluation frameworks may change.
* NIST AI RMF 1.0 is under revision.
* Evaluation evidence cannot prove future behaviour.
* Qualification requires continuous maintenance.
* A small team may initially support only a narrow model and task catalogue.

---

# 343. Open Questions

* Which Task Classes belong in Version 1?
* Which Risk Classes are final?
* Should Task Class and product command be one-to-one?
* How are mixed tasks represented?
* Should one operation have a primary and secondary task class?
* How are free-form requests classified without a model?
* Which ambiguous requests merit a local classifier?
* Which model may propose task classification?
* What confidence is required for a proposed class?
* How are task-class errors surfaced?
* Which exact fields define an AI Execution Profile?
* Should provider safety settings always create separate profile revisions?
* Should provider account identity be part of the profile hash?
* How is resolved provider model identity obtained?
* Which providers guarantee snapshot immutability?
* How are marketplace model versions represented?
* How are Azure-, AWS- and direct-hosted versions of the same model compared?
* How are regional endpoint differences evaluated?
* How are provider backend changes detected?
* Should TLS endpoint identity be part of evidence?
* How are serverless provider capacity tiers represented?
* How are reserved-capacity endpoints represented?
* How are local driver versions represented?
* How are GPU firmware and Windows builds represented?
* Which local hardware fields affect quality versus only performance?
* Should CUDA library versions create profile revisions?
* Which sampling fields are material?
* Should an unsupported seed be represented as null or capability absent?
* How are provider defaults prohibited?
* How are reasoning settings normalised across providers?
* How is provider-managed reasoning cost evaluated?
* How are hidden reasoning tokens included in quality and cost evidence?
* How are model-visible reasoning summaries evaluated?
* How are tool schemas canonicalised?
* Does a tool implementation change invalidate model qualification?
* How are equivalent provider-native schemas compared?
* Which output-schema changes require full re-evaluation?
* Which Context Policy changes require full re-evaluation?
* Which Tokenizer Profile changes affect quality versus only budgeting?
* How are prompt-template experiments governed?
* Can prompt templates be optimised against Development suites automatically?
* How is prompt overfitting detected?
* Should prompt templates have their own qualification?
* How are prompt and model changes separated statistically?
* Which Evaluation Suites ship first?
* How many cases are needed per task?
* How many held-out cases are feasible for a small team?
* How frequently should held-out sets rotate?
* Who may author held-out cases?
* How are held-out cases stored on founder machines?
* Should held-out data be encrypted with a separate key?
* Should CI have access to held-out suites?
* Which release environment runs held-out tests?
* How are remote provider calls approved in CI?
* How are held-out results reviewed without exposing expected outputs?
* Which cases may be source controlled?
* How are real incidents de-identified?
* Which licences permit benchmark modification?
* How are dataset-specific terms recorded?
* Should non-redistributable cases be supported?
* How are public benchmark downloads pinned?
* Which SWE-bench variant is appropriate?
* Should Opure run full SWE-bench or a curated subset?
* How are Linux benchmark results contextualised for Windows?
* Should a Windows-native issue-resolution benchmark be created?
* Which open-source .NET repositories may be used?
* How are repository licences handled?
* How are build images pinned?
* Should Docker be required for public benchmark runners?
* Which Windows sandbox runs untrusted benchmark projects?
* Should Windows Sandbox, Hyper-V or disposable VMs be used?
* How are test fixtures reset efficiently?
* Which package caches are permitted?
* How are benchmark network dependencies handled?
* Can public benchmark tests attempt network access?
* How are flaky public benchmark cases identified?
* Should flaky cases be retired or modelled statistically?
* How is benchmark contamination assessed?
* Should time-separated private cases use newly created repositories?
* Can synthetic issue variants be trusted?
* How are model-generated cases validated?
* Should one provider generate cases for another provider?
* How are model-generated expected answers avoided?
* Which deterministic graders are first?
* Which JSON Schema implementation is selected?
* Which patch-parser implementation is selected?
* Which build configurations are authoritative?
* How many target frameworks should patch cases build?
* Which test suites are required for one patch case?
* How are long-running integration tests budgeted?
* Should mutation testing run for every qualification?
* Which property-testing framework is selected?
* How are equivalent mutants handled?
* Which static-analysis tools are required?
* Which security scanners are required?
* How are scanner versions pinned?
* How are scanner false positives adjudicated?
* Should formatting be a hard gate or soft metric?
* How is architecture-policy compliance graded?
* Which repository-state changes are permitted?
* How are new dependencies graded?
* Should package-lock changes require a separate gate?
* How are licence changes in patches evaluated?
* How are generated files graded?
* How are migrations graded?
* How are database changes graded?
* How are UI changes evaluated without brittle screenshots?
* Should Avalonia UI cases use headless tests?
* Which real-window Appium cases are useful?
* How are accessibility changes graded?
* How are documentation changes graded?
* How are citations graded?
* How are code-review false positives measured?
* Which severity rubric is used?
* How are partial findings scored?
* How are security-review false negatives measured?
* How many known-good examples are needed?
* How are diagnostic root causes labelled?
* How are multiple valid fixes represented?
* Should a model receive hidden test feedback in repair suites?
* How many repair turns are allowed?
* How are tool-loop traces canonicalised?
* Which tools belong in Version 1 evals?
* How are MCP servers simulated?
* How are plugin packages simulated?
* How are tool timeouts graded?
* How are partial side effects reset?
* How is refusal correctness defined?
* Which over-refusal cases are needed?
* Which privacy cases are required?
* How is data minimisation graded?
* How are context precision and recall labelled?
* How are long-context distractors constructed?
* How are memory conflicts graded?
* How are retrieval ground-truth judgements created?
* Which embedding metrics are release gates?
* Which model judge is selected initially?
* Should local and remote judges both be tested?
* Should the judge provider differ from the candidate?
* How many human-labelled calibration cases are required?
* What balanced accuracy is sufficient?
* How often should judge calibration expire?
* How is domain transfer tested?
* How are judge rubric changes versioned?
* Should candidate outputs be anonymised completely?
* Should length be normalised for pairwise review?
* How is verbosity bias quantified?
* How is assertiveness bias quantified?
* How is citation-style bias quantified?
* How is model self-preference measured?
* Should two judges be required for high-impact cases?
* Which disagreement triggers human adjudication?
* How is judge prompt injection contained?
* Can a judge use tools?
* Should judge tools be prohibited initially?
* How are visible judge rationales retained?
* How is private chain of thought avoided?
* Which human-review platform is selected?
* How are reviewers trained?
* How are reviewer conflicts of interest handled?
* How are reviewer identities minimised?
* How are reviewers compensated in a future team?
* Which agreement measure is appropriate for each rubric?
* What agreement threshold invalidates a rubric?
* How are ordinal scales analysed?
* How are ties analysed?
* Which statistical library is selected?
* Which confidence intervals are used for pass rates?
* How are bootstrap seeds recorded?
* How are repeated stochastic trials aggregated?
* How many trials are sufficient?
* How are provider rate-limit failures treated?
* Do harness failures count as model failures?
* Which missing-case policies apply to release gates?
* How are effect-size thresholds selected?
* How are multiple comparisons controlled?
* Should Bayesian analysis be used?
* How are critical failures classified?
* Which critical failure count blocks Stable?
* Can one security failure ever be waived?
* Who can waive a gate?
* How is a waiver represented and expired?
* Should qualification be separate by project?
* Which qualifications are global product defaults?
* How do projects narrow default qualifications?
* Should a project be able to require stronger suites?
* Which data classifications need separate qualification?
* How is Public versus Internal quality compared?
* Can a provider be qualified for synthetic data only?
* Which review modes are qualification restrictions?
* How long should qualification remain valid?
* Are 30- and 90-day evidence windows appropriate?
* Which changes require full versus partial re-evaluation?
* How are price-only changes handled?
* How are provider incidents handled?
* How are local driver updates handled?
* How are Windows updates handled?
* How are instruction-template security changes handled?
* How are new tool schemas handled?
* How are qualification histories exported?
* How does Stable differ from Preview qualification?
* What Development override warnings are required?
* Which profiles are candidates by default?
* How are user profile priorities configured?
* Should users be able to prohibit one model?
* Should users be able to prefer one provider?
* How is local preference represented?
* What quality margin permits local preference?
* How is a quality margin calculated across several metrics?
* Should Pareto selection use privacy posture as a dimension or hard rule?
* Which dimensions belong in each task policy?
* How are incomparable missing metrics handled?
* Which deterministic tie-break order is best?
* Should cost be estimated from reserve or historical output?
* How are reasoning-token costs estimated?
* How are tool-call costs estimated?
* How are currency conversions sourced?
* How stale may a price be?
* What happens when actual cost exceeds estimate?
* Should cost budget be per request, workflow, day or project?
* Which latency statistic drives routing?
* Should TTFT or total latency dominate interactive tasks?
* How are cold and warm local latencies selected?
* How are provider queue spikes handled?
* Should live availability probes be used before every request?
* How are probes rate limited?
* How is probe privacy guaranteed?
* How are local resource estimates sampled?
* Should active GPU use immediately change routing?
* How are builds and tests prioritised over local inference?
* Can routing wait for a local model to unload?
* How are context-reduction alternatives presented?
* Should routing happen before or after full Context Plan?
* How many times may context and routing replan each other?
* How are circular replans prevented?
* Which Routing Decision expiry is appropriate?
* Which state changes invalidate immediately?
* How are decisions distributed across processes?
* How are decisions revoked?
* How are retries distinguished from fallbacks in UI?
* Which transient errors permit retry?
* How many retries are safe?
* How is retry backoff selected?
* Should retries use the same seed?
* What happens after a partial stream?
* Can the same provider resume a response?
* How is a new response compared?
* Which fallback changes always require confirmation?
* Can an unattended workflow pre-approve a cloud fallback?
* How are fallback cost limits represented?
* How are fallback quality floors represented?
* Can a fallback use a smaller context?
* How is a smaller context replanned?
* Can a fallback use a lower reasoning setting?
* How are tool loops handled across fallback?
* How are side effects handled before a fallback?
* Which provider routers should be evaluated?
* How is their actual selected model obtained?
* What if the router omits selected-model identity?
* How are router candidate changes detected?
* Can a provider router cross regions?
* Can a provider router cross data terms?
* How is application-specific router performance measured?
* Should provider routers ever qualify for Stable?
* How are `latest` aliases monitored?
* How frequently are deprecation pages checked?
* How are email-only deprecation notices captured?
* Should deprecation monitoring be automated?
* Which source is authoritative when provider docs conflict?
* How early should migration warnings appear?
* Which synthetic canaries are sufficient?
* How often should canaries run?
* Which provider cost budget applies to canaries?
* Should canaries run only before first daily use?
* How are canary failures distinguished from transient outage?
* Should multiple failures be required before suspension?
* Which critical canary failure suspends immediately?
* Should ordinary operations ever be sampled for evaluation?
* How is explicit comparison initiated?
* Can comparisons use redacted context?
* How are comparison costs shown?
* Can comparison results be retained as feedback?
* How are developer feedback events represented?
* Which feedback options are useful?
* How is accidental feedback corrected?
* How are automated patch outcomes linked?
* Can build pass count as positive online evidence?
* How is user editing measured without storing source?
* How is revert evidence interpreted?
* How are noisy signals prevented from biasing routing?
* Should feedback only create regression candidates?
* How are malicious feedback plugins prevented?
* How is drift statistically detected?
* Which baseline window is used?
* How is seasonality handled?
* How are provider outages separated from quality drift?
* Which drift metrics are critical?
* How does one task qualification suspend without affecting others?
* Which routing-policy changes require founder review?
* Which can be delegated?
* How is emergency rollback performed?
* How are old Routing Decisions interpreted after policy rollback?
* How long are Trial outputs retained?
* How long are Routing Decisions retained?
* How long are feedback events retained?
* Which artefacts belong in support bundles?
* How are held-out cases protected in support bundles?
* Should Evaluation Cards be exportable publicly?
* How are provider terms and benchmark licences represented?
* How are evaluation databases backed up?
* Are held-out datasets backed up separately?
* How is privacy deletion reconciled with qualification history?
* Can a qualification remain when one source case is deleted?
* What permanent evidence is required for a model-quality incident?
* What permanent evidence is required for a hidden-routing incident?
* What permanent evidence is required before learned routing can be considered?

---

# 344. Deferred Decisions

This ADR intentionally defers:

* final model catalogue;
* final provider catalogue;
* final Task Class taxonomy;
* final Risk Class taxonomy;
* final private benchmark corpus;
* final held-out corpus;
* final human-review platform;
* final model judge;
* provider fine-tuning;
* local fine-tuning;
* reinforcement learning;
* learned routing;
* bandit routing;
* hidden online experiments;
* global personalisation;
* provider-managed routing as default;
* Stable mutable aliases;
* automatic prompt optimisation;
* automatic benchmark generation;
* public Evaluation Card publication;
* external evaluation signing;
* third-party independent audits;
* and cross-user aggregate feedback.

---

# 345. Alternatives Rejected

One default model is rejected because task, privacy, cost and capability requirements differ.

Provider-managed intelligent routing is not selected as the primary authority because provider routers do not incorporate Opure-specific evidence or project policy completely.

Model self-routing is rejected because a model cannot authorise its own provider, cost or data access.

The cheapest-model rule is rejected because cost cannot replace task quality.

The highest-public-benchmark rule is rejected because public configurations and workloads differ.

A universal weighted score is rejected because hard failures and multi-dimensional evidence must remain visible.

LLM-as-judge-only evaluation is rejected because judges can be biased, uncalibrated and prompt-injected.

Human-only evaluation is rejected because deterministic software correctness and continuous scale require automation.

Public-benchmark-only evaluation is rejected because Opure needs Windows, C#, context, memory, tool and policy evidence.

Build-and-test-only evaluation is rejected because secure, grounded and useful interaction requires more than compilation.

Hidden shadow routing and online bandits are rejected because they would duplicate project data or manipulate routing without adequate developer visibility.

Stable use of mutable aliases is rejected because model identity can change without a new Opure qualification.

Silent fallback is rejected because a fallback can change quality, cost, model, provider, region and data posture.

---

# 346. Official and Primary Evidence References

## OpenAI Evaluation

* [OpenAI Evals API Reference](https://platform.openai.com/docs/api-reference/evals)
* [OpenAI Graders API Reference](https://platform.openai.com/docs/api-reference/graders)
* [How evals drive the next chapter in AI for businesses](https://openai.com/index/evals-drive-next-chapter-of-ai/)
* [A shared playbook for trustworthy third party evaluations](https://openai.com/index/trustworthy-third-party-evaluations-foundations/)
* [OpenAI Evals](https://evals.openai.com/)

## Anthropic Evaluation

* [Define your success criteria](https://docs.anthropic.com/en/docs/test-and-evaluate/define-success)
* [Using the evaluation tool](https://docs.anthropic.com/en/docs/test-and-evaluate/eval-tool)
* [Reduce latency](https://docs.anthropic.com/en/docs/test-and-evaluate/strengthen-guardrails/reduce-latency)

## Google Evaluation and Model Lifecycle

* [Generative AI evaluation overview](https://cloud.google.com/vertex-ai/generative-ai/docs/models/evaluation-overview)
* [View and interpret evaluation results](https://cloud.google.com/vertex-ai/generative-ai/docs/models/eval-python-sdk/view-evaluation)
* [Evaluate a judge model](https://cloud.google.com/vertex-ai/generative-ai/docs/models/evaluate-judge-model)
* [Gemini model version patterns](https://ai.google.dev/gemini-api/docs/models)
* [Gemini deprecations](https://ai.google.dev/gemini-api/docs/deprecations)
* [Gemini release notes](https://ai.google.dev/gemini-api/docs/changelog)

## Provider Routing Evidence

* [Amazon Bedrock intelligent prompt routing](https://docs.aws.amazon.com/bedrock/latest/userguide/prompt-routing.html)
* [CreatePromptRouter API](https://docs.aws.amazon.com/bedrock/latest/APIReference/API_CreatePromptRouter.html)

## NIST Risk, Evaluation and Secure Development

* [NIST AI RMF Core](https://airc.nist.gov/airmf-resources/airmf/5-sec-core/)
* [NIST AI RMF](https://airc.nist.gov/airmf-resources/airmf/)
* [NIST AI Technical Reports](https://airc.nist.gov/technical-reports/)
* [NIST AI 700-1 text-to-text evaluation report](https://www.nist.gov/publications/2024-nist-genai-pilot-study-text-text-evaluation-overview-and-results)
* [NIST SP 800-218A](https://csrc.nist.gov/pubs/sp/800/218/a/final)

## Software-Engineering Evaluation

* [SWE-bench repository](https://github.com/SWE-bench/SWE-bench)
* [SWE-bench paper](https://openreview.net/forum?id=VTF8yNQM66)
* [HumanEval paper](https://arxiv.org/abs/2107.03374)
* [EvalPlus repository](https://github.com/evalplus/evalplus)
* [EvalPlus paper](https://arxiv.org/abs/2305.01210)
* [RealHumanEval paper](https://arxiv.org/abs/2404.02806)

## Holistic and Operational Evaluation

* [HELM documentation](https://crfm-helm.readthedocs.io/en/latest/)
* [HELM repository](https://github.com/stanford-crfm/helm)
* [HELM framework](https://crfm.stanford.edu/helm/)
* [MLPerf Inference documentation](https://docs.mlcommons.org/inference/)

Provider APIs, model identities, benchmark datasets, licences, evaluation services, pricing and deprecation schedules can change.

The implementation must revalidate every enabled profile, benchmark revision, grader, routing policy and external reference before acceptance.

---

# 347. Review Record

| Date         | Reviewer           | Decision | Notes                                                                                                |
| ------------ | ------------------ | -------- | ---------------------------------------------------------------------------------------------------- |
| 18 July 2026 | Architecture draft | Proposed | Exact-profile evaluation, task-specific qualification and deterministic governed routing recommended |

---

# 348. Approval

## Founder or Product Approval

* **Name:** Christopher Dyer
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Quality gates, local preference, routing visibility and fallback policy review required

## Evaluation Architecture Approval

* **Name or role:** AI Evaluation Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Suites, graders, human review, judges, metrics and held-out evidence required

## Routing Governance Approval

* **Name or role:** Routing Governance and AI Router Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Eligibility, qualification, deterministic selection, binding and fallback evidence required

## Local and Provider Approval

* **Name or role:** Local Model Runtime and Provider Trust Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Exact identity, lifecycle, data posture and operational evidence required

## Software-Engineering Validation Approval

* **Name or role:** Patch, Build, Test and Tool Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Deterministic patch, build, test, tool and repository graders required

## Security and Privacy Approval

* **Name or role:** Security and Privacy Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Held-out protection, secret safety, hidden-routing and critical-failure evidence required

## Product Quality Approval

* **Name or role:** Product and Human Evaluation Owners
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Human rubrics, agreement, developer usefulness and known limitations required

## Performance Approval

* **Name or role:** Performance Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Latency, cost, local resources and routing overhead evidence required

## Test Approval

* **Name or role:** Test Architecture Owner
* **Decision:** Pending
* **Date:** Pending
* **Notes:** Adversarial grader, fallback, drift and contamination suites required

---

# 349. Supersession

This ADR is superseded only when a later ADR:

* names ADR-0024 explicitly;
* explains why evaluation identity, quality gates, qualification, routing or fallback changed;
* identifies Execution Profile, Evaluation Card, Qualification, Routing Policy and Routing Decision migration;
* describes project-policy, provider, local-model, cost, privacy and release impact;
* provides comparison evidence for any learned or provider-managed routing;
* and updates the `Superseded by` field.

Historical Evaluation Runs, Evaluation Cards, Qualifications, Routing Decisions, fallback events and drift records remain available according to retention policy.

---

# 350. Change History

| Version | Date         | Author        | Summary                                                                                                  |
| ------- | ------------ | ------------- | -------------------------------------------------------------------------------------------------------- |
| 0.1     | 18 July 2026 | Founder Draft | Initial exact-profile evaluation, task qualification and deterministic routing-governance recommendation |

---

# 351. Final Decision Statement

> **Opure will provisionally govern model quality and selection through a first-party provider-neutral AI Evaluation Service and Routing Governance Service that treat the exact provider, model snapshot, local runtime, weights, quantisation, prompt, renderer, tokenizer, context, tools, schema, reasoning and sampling configuration as one immutable AI Execution Profile; evaluate that profile against versioned private, held-out, adversarial, human and public evidence with executable patch, build, test, tool, policy and security graders taking precedence over calibrated model judges; qualify profiles separately by task, data class and review mode; and route each operation only after deterministic project-policy, capability, context, resource and quality filters followed by visible multi-objective preferences and stable tie-breaking, while mutable aliases, model self-routing, hidden project-data shadow traffic, aggregate-score masking and silent provider, model, region, cost or local-to-cloud fallback remain prohibited, because model choice is an engineering and data-governance decision whose evidence, trade-offs, failures and changes must stay inspectable to the developer.**

---

> **Developer Respect. Local Intelligence. Complete Control.**

> **Build software with developers, not instead of them.**