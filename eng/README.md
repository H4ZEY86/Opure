# Opure Engineering Commands

Repository-owned scripts are the authoritative local build interface.

## Commands

```powershell
pwsh ./build.ps1 restore
pwsh ./build.ps1 build
pwsh ./build.ps1 test
pwsh ./build.ps1 verify
pwsh ./build.ps1 policy
```

`restore`, `build`, `test` and `verify` use the committed package lock files.

`policy` performs the heavier FND-002 evidence checks:

- warning-as-error negative probe;
- stale lock-file negative probe;
- deterministic Release assembly comparison;
- dependency inventory;
- and M0 evidence generation.

## Build channels

```text
Development
Preview
Stable
```

Select one with:

```powershell
pwsh ./build.ps1 verify -BuildChannel Development
```

The channels define compile-time identity only. They do not weaken security, auditing, warning, package or test policy.

## Generated output

All generated build and intermediate output belongs under:

```text
artifacts/
```

The directory is ignored by Git.

## Package policy

Package versions belong only in:

```text
Directory.Packages.props
```

Project files contain versionless `PackageReference` items.

Package lock files are committed beside each project and validation uses locked restore.

## Local tools

Restore exact repository tools with:

```powershell
dotnet tool restore
```

The manifest is security-sensitive because local tools execute with developer privileges.

## Version identity

The authoritative product version is:

```text
version.json
```

The repository pins matching releases of:

```text
Nerdbank.GitVersioning 3.10.70
nbgv 3.10.70
```

Show the current resolved identity with:

```powershell
pwsh ./build.ps1 version
```

Run the FND-003 evidence probes with:

```powershell
pwsh ./build.ps1 version-policy
```

Every first-party project receives the generated internal `ThisAssembly` class. Hosts should use it for diagnostic identity rather than declaring version literals.

Useful generated members include:

```text
ThisAssembly.AssemblyVersion
ThisAssembly.AssemblyFileVersion
ThisAssembly.AssemblyInformationalVersion
ThisAssembly.GitCommitId
ThisAssembly.IsPublicRelease
ThisAssembly.NuGetPackageVersion
```

A development build may be clean or dirty. `eng/version.ps1` reports that state explicitly.

Preview and Stable build-channel commands require a clean working tree.

Public release classification is tag-only. The normal `main` branch is not a public release ref.

The trusted `PublicRelease=true` override is reserved for an exact validated release candidate or tagged commit. It must never be used merely to remove commit identity from ordinary builds.

## Runtime executable

Run the Development Runtime until Ctrl+C:

```powershell
pwsh ./build.ps1 runtime
```

Run it for a bounded smoke-test interval:

```powershell
pwsh ./build.ps1 runtime -RuntimeDurationMilliseconds 1000
```

The minimal FND-004 Runtime:

- owns one random boot identity per process start;
- reports `starting`, `ready`, `stopping` and `stopped`;
- reports product and Runtime contract versions;
- uses the Development channel data-root resolver;
- writes newline-delimited safe JSON to standard output;
- creates no durable state;
- opens no TCP or UDP endpoint;
- starts no child process;
- and loads no project, AI, plugin, MCP or workflow code.

Run the full Runtime evidence gate with:

```powershell
pwsh ./build.ps1 runtime-policy
```

Ctrl+C is intercepted through the controlled shutdown signal so the Runtime can report `stopping` and `stopped` before exit. The current shutdown deadline is five seconds.

## Desktop executable

Run the Development Desktop until its main window is closed:

```powershell
pwsh ./build.ps1 desktop
```

Run a bounded real-window smoke launch:

```powershell
pwsh ./build.ps1 desktop -DesktopDurationMilliseconds 1500
```

Run the complete FND-005 evidence gate:

```powershell
pwsh ./build.ps1 desktop-policy
```

The initial Desktop uses Avalonia 12.1.0 through a framework-specific adapter project. `Opure.Desktop.Contracts` remains framework neutral so the documented WinUI 3 fallback can reuse its shell state and view model.

The shell reports `Runtime unavailable` honestly until authenticated local IPC exists. It does not read project files, open service databases or own authoritative domain state.

## Bootstrap executable

Run the Development Bootstrap until the Desktop is closed:

```powershell
pwsh ./build.ps1 bootstrap
```

Run a bounded process-tree smoke launch:

```powershell
pwsh ./build.ps1 bootstrap -Configuration Release -BootstrapDurationMilliseconds 3000
```

Run the complete FND-006 evidence gate:

```powershell
pwsh ./build.ps1 bootstrap-policy
```

Run the complete FND-007 process-supervisor evidence gate:

```powershell
pwsh ./build.ps1 supervisor-policy
```

Run the complete FND-008 Runtime Health contract evidence gate:

```powershell
pwsh ./build.ps1 health-contract-policy
```

Run the complete FND-009 named-pipe transport evidence gate:

```powershell
pwsh ./build.ps1 health-transport-policy
```

Run the complete FND-010 named-pipe session-authentication evidence gate:

```powershell
pwsh ./build.ps1 health-session-policy
```

Run the complete FND-011 Runtime Service Registry contract evidence gate:

```powershell
pwsh ./build.ps1 service-registry-policy
```

Run the complete FND-012 Service Lifecycle evidence gate:

```powershell
pwsh ./build.ps1 service-lifecycle-policy
```

Run the complete FND-013 Runtime Health UI evidence gate:

```powershell
pwsh ./build.ps1 runtime-health-ui-policy
```

Bootstrap verifies absolute Runtime and Desktop executable paths and companion assembly identities before launch. It starts Runtime first, waits for explicit Runtime readiness, starts Desktop second, and shuts down Desktop before Runtime.

Supervisor verification injects a bounded Runtime crash, a rapid crash loop and an abrupt Bootstrap termination. It verifies restart identity, exponential backoff, visible Safe Mode and Windows Job Object orphan cleanup without recording child environment values.

Runtime Health contract verification compiles the protobuf client and server surfaces, exercises compatibility and semantic validation, enforces message and service-summary bounds, and emits the authoritative schema, compatibility matrix and golden messages under `eng/evidence/milestones/M2`.

Named-pipe transport verification exercises the Desktop gateway round trip, deadline, cancellation, message-size and restart/reconnect paths. It records a bounded unary latency baseline and inspects the live Runtime process for TCP and UDP listeners without logging RPC payloads.

Named-pipe session verification inspects the protected DACL, exercises expected and denied same-user sessions, process binding, replay and expiry paths, and confirms Runtime and Desktop restart rotation. It emits only bounded policy results and scans the evidence and running process command lines for authentication material.

Service Registry verification compiles the protobuf query surface, exercises transactional registration, duplicate and dependency rejection, deterministic cursor ordering, serialization and the authenticated named-pipe endpoint. It emits the authoritative schema and safe initial catalogue under `eng/evidence/milestones/M1`.

Service Lifecycle verification exercises the exhaustive transition policy, dependency-aware start and reverse-order shutdown, required and optional failure propagation, startup and shutdown deadlines, restart transitions, deterministic events and the registry-backed lifecycle projection. It emits the reviewed state-machine diagram and transition report under `eng/evidence/milestones/M1`.

Runtime Health UI verification exercises the live registry-backed projection, authenticated refresh and reconnect path, stale-snapshot recovery, all six visible Runtime states, safe boot-identity copy, keyboard and UI Automation semantics, theme-owned high-contrast colours and a 64-row performance baseline. It observes a native Windows window and emits the UI test artefact, accessibility report and reconnect recording under `eng/evidence/milestones/M1`.

Channel-specific data-root and one-time session material are passed through bounded environment variables. The session secret is not placed on command lines, written to disk or included in diagnostics.
