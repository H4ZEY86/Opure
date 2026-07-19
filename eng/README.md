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
