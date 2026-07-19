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
