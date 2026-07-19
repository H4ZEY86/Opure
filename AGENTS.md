# Opure — Codex Project Context

## Product

Opure is a local-first, developer-controlled software engineering platform for Windows 11.

Its purpose is to help developers design, understand, modify, build, test and operate software without surrendering authority to autonomous AI behaviour.

The product motto is:

> Developer Respect. Local Intelligence. Complete Control.

The governing rule is:

> Build software with developers, not instead of them.

Opure is a software engineering platform that uses AI. It is not an AI platform that happens to write code.

## Repository

Repository root:

```text
C:\Opure
```

Specifications:

```text
C:\Opure\specs
```

Architecture Decision Records:

```text
C:\Opure\adr
```

Primary solution:

```text
C:\Opure\Opure.slnx
```

Engineering commands are exposed through:

```text
C:\Opure\build.ps1
```

Use British English in documentation, messages and user-facing text.

## Core principles

All implementation work must preserve these principles:

* developer authority remains explicit;
* AI proposes, deterministic services authorise and execute;
* local operation is the default;
* cloud use is optional and policy-controlled;
* decisions and effects must be visible and inspectable;
* patches are reviewable by default;
* actions should be reversible where technically practical;
* services have explicit ownership boundaries;
* no hidden authority or hidden side effects;
* no silent fallback across trust boundaries;
* secrets must not appear in ordinary databases, project memory, embeddings, logs, command lines, checkpoints or Trust Centre evidence;
* Desktop is a command and projection layer, not an authority over domain state;
* agents are controlled workflows, not autonomous minds;
* provider and model implementations must remain replaceable;
* Windows is the first target, not a permanent architectural lock-in.

## Architecture

The initial process topology is:

```text
Opure.Bootstrap.Windows
    ├── Opure.Runtime
    └── Opure.Desktop
```

### Bootstrap

Bootstrap owns controlled product launch.

It must:

* resolve exact absolute executable paths;
* verify expected binary identities before launch;
* never search the current directory for executables;
* create channel-specific process environments;
* start Runtime before Desktop;
* wait for explicit Runtime readiness;
* pass only bounded, random session material;
* keep session secrets out of command lines and diagnostics;
* record safe child process identities;
* stop Desktop before Runtime;
* clean up partial launches;
* avoid persisting bootstrap session material.

Windows Job Objects, restart budgets, crash-loop policy and Safe Mode belong to the Process Supervisor work, not the basic Bootstrap ticket.

### Runtime

Runtime owns authoritative platform state and trusted first-party services.

Runtime must:

* operate offline;
* start without AI, plugins, MCP servers or cloud providers;
* expose explicit lifecycle states;
* produce a unique boot identity;
* expose product and contract versions;
* use time-bounded shutdown;
* keep service ownership explicit;
* avoid creating unowned databases or persistence;
* reject malformed bootstrap environment information;
* support direct engineering and test launches without Bootstrap;
* accept controlled Bootstrap shutdown without blocking startup.

### Desktop

Desktop uses Avalonia behind framework-neutral contracts and view models.

Desktop must:

* remain a command and projection layer;
* show honest disconnected Runtime state;
* never read service databases directly;
* never read project files directly;
* remain independently closable;
* preserve a future WinUI 3 fallback;
* provide keyboard navigation and stable accessibility names;
* keep authoritative state outside the UI.

## Release channels

Supported channels are:

```text
Stable
Preview
Development
```

Their mutable roots must not collide:

```text
%LOCALAPPDATA%\Opure\Stable
%LOCALAPPDATA%\Opure\Preview
%LOCALAPPDATA%\Opure\Development
```

Preview, Stable and Development must remain isolated in data, identity and future IPC namespaces.

## Current implementation state

Completed and committed:

* FND-001 — solution baseline;
* FND-002 — central build policy;
* FND-003 — authoritative version source;
* FND-004 — minimal Runtime executable and lifecycle;
* FND-005 — disconnected Avalonia Desktop shell.

Currently in progress:

* FND-006 — controlled Bootstrap executable.

FND-006 currently includes:

* exact Runtime and Desktop path resolution;
* executable and companion assembly verification;
* SHA-256 binary identities;
* channel-isolated roots;
* random bounded bootstrap session material;
* Runtime-before-Desktop launch;
* explicit Runtime readiness detection;
* Desktop-before-Runtime shutdown;
* partial-start rollback;
* Bootstrap, Runtime and architecture tests;
* process-tree, channel-root and binary-identity evidence.

The latest FND-006 correction addresses an xUnit analyser rule in:

```text
tests\Runtime\Opure.Runtime.Tests\RuntimeBootstrapEnvironmentTests.cs
```

The blocking-reader regression test must use:

```csharp
TestContext.Current.CancellationToken
```

Do not assume FND-006 is complete until the complete Bootstrap verifier passes on Windows and the changes are committed and pushed.

The next planned ticket after FND-006 is:

```text
FND-007 — Add Process Supervisor
```

That ticket will introduce Windows Job Object ownership, process health, restart budgets, exponential backoff, crash classification and Safe Mode.

## Build policy

The repository uses:

* .NET SDK `10.0.302`;
* central package management;
* exact package versions;
* package lock files;
* locked restore;
* nullable reference types;
* warnings and analysers as errors;
* deterministic build output;
* Nerdbank.GitVersioning;
* xUnit v3;
* architecture tests.

Do not suppress analysers merely to make a build pass.

Correct the underlying source unless a suppression is explicitly justified by an existing policy or ADR.

Known strict analyser categories include:

* nullable correctness;
* concrete local types where appropriate;
* xUnit cancellation-token rules;
* xUnit assertion rules;
* allocation and constant-array rules;
* argument exception parameter validation.

## Engineering commands

Run the complete Release verification:

```powershell
pwsh ./build.ps1 verify -Configuration Release
```

Run Runtime:

```powershell
pwsh ./build.ps1 runtime `
    -RuntimeDurationMilliseconds 500
```

Run Runtime policy verification:

```powershell
pwsh ./build.ps1 runtime-policy
```

Run Desktop:

```powershell
pwsh ./build.ps1 desktop `
    -DesktopDurationMilliseconds 1500
```

Run Desktop policy verification:

```powershell
pwsh ./build.ps1 desktop-policy
```

Run a bounded Bootstrap launch:

```powershell
pwsh ./build.ps1 bootstrap `
    -Configuration Release `
    -BootstrapDurationMilliseconds 3000
```

Run Bootstrap evidence verification:

```powershell
pwsh ./build.ps1 bootstrap-policy
```

Generated build output under `artifacts` must not be committed.

## Required working style

Before editing:

1. Read the relevant specification, ADR, ticket and nearby implementation.
2. Inspect the current Git status.
3. Distinguish existing user changes from changes required for the task.
4. Do not discard or rewrite unrelated work.
5. Do not assume an interrupted installer means its earlier changes should be reverted.

While editing:

* make the smallest complete architectural change;
* preserve existing public contracts unless the ticket requires a change;
* keep implementation boundaries explicit;
* prefer deterministic code over hidden convention;
* avoid adding dependencies without strong justification;
* do not introduce AI, network, plugin, MCP or persistence capability outside the relevant ticket;
* keep secrets out of logs, errors, evidence and command lines;
* use absolute verified executable paths;
* use asynchronous process output safely;
* do not invoke PowerShell script blocks from .NET worker-thread callbacks;
* add regression tests for discovered failures;
* update architecture tests when a new boundary needs enforcement.

After editing:

1. Run formatting and static checks.
2. Run locked restore where required.
3. Build Release with zero warnings and zero errors.
4. Run all tests.
5. Run the ticket-specific verifier.
6. Inspect generated evidence.
7. Run `git diff --check`.
8. Show `git diff --stat` and `git status --short`.
9. Do not commit unless explicitly instructed.
10. Never add `artifacts`.

## Failure handling

When a command fails:

* diagnose the first real source failure;
* distinguish product failure from verifier or installer failure;
* preserve already reviewed partial work;
* avoid broad rewrites;
* correct likely follow-on analyser failures in the same narrow area;
* make reruns idempotent and exact-state guarded where scripts are involved;
* do not claim success until the actual Windows build, tests and evidence pass.

Expected negative tests may intentionally emit errors. Judge them by their final asserted result, not by alarming intermediate output.

## Security boundaries

Never:

* persist bootstrap session secrets;
* print environment secrets;
* place secrets in arguments;
* allow current-directory executable resolution;
* trust a child process solely because its filename looks correct;
* silently fall back to another executable, provider or channel;
* let Desktop become authoritative;
* introduce direct Desktop access to service persistence;
* introduce network clients or listeners during foundation tickets unless explicitly scoped;
* create service databases without a declared owner;
* load plugins or external providers into the trusted Runtime;
* weaken tests to accommodate incorrect behaviour.

## Documentation standard

Documentation must state:

* what owns the behaviour;
* what is authoritative;
* what is provisional;
* what is deliberately deferred;
* how the developer can inspect or stop the behaviour;
* what evidence proves the implementation;
* how failure and recovery work.

Do not describe aspirational behaviour as already implemented.

## Definition of done

A foundation ticket is complete only when:

* its acceptance criteria are implemented;
* Release builds with zero warnings and errors;
* all tests pass;
* architecture boundaries pass;
* Windows smoke or integration evidence passes where required;
* required evidence files are generated;
* no secrets or private paths leak into evidence;
* `git diff --check` passes;
* generated artefacts are excluded;
* the implementation is reviewed, committed and pushed;
* the working tree is clean and up to date with `origin/main`.
