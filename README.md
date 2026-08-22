# Opure

> Developer Respect. Local Intelligence. Complete Control.

Opure is a local-first, developer-controlled software engineering platform for
Windows 11. It helps developers design, understand, modify, build, test and
operate software without surrendering authority to autonomous behaviour.

The repository contains the trusted Runtime, Avalonia Desktop, Bootstrap,
`opure` CLI, service contracts, persistence libraries, verification tooling and
the specifications that govern their behaviour.

## Current status — v0.4.0-gate-d (Gate D Sandbox Release)

Gate D is formally secured and released. Phase 15 (MCP Gateway) is fully verified.

### Deployment Architecture

Opure is deployed as a deterministic WiX v4 `.msi` package containing two isolated .NET 10 Single-File executables. 

This guarantees absolute IPC integrity and preserves the airtight, local-first, default-deny architecture. We have explicitly abandoned MSIX/UWP virtualization as it is structurally incompatible with our strict named-pipe process hierarchy boundaries.

### Production baseline (v0.4.0-gate-d)

- **1,062 passing tests**, zero warnings, zero errors in Release configuration
- **Zero AI inference**, zero network listeners, zero arbitrary shell authority
  — proven by the architecture test suite.
- **Provider Trust, Plugins, and MCP Gateway** are fully integrated.

### Gate D performance baseline (RTX-HAZE, 32 processors, Windows 11)

| Measurement | Value |
|---|---|
| Bootstrap to IPC session readiness | ~3 161 ms |

## Build and verify

The repository requires the .NET SDK pinned by `global.json`.

```powershell
pwsh ./build.ps1 verify -Configuration Release
```

To build the full WiX installer:

```powershell
pwsh ./build-msi.ps1
```

Useful bounded launch commands:

```powershell
pwsh ./build.ps1 runtime -RuntimeDurationMilliseconds 500
pwsh ./build.ps1 desktop -DesktopDurationMilliseconds 1500
pwsh ./build.ps1 bootstrap -Configuration Release -BootstrapDurationMilliseconds 3000
```

Run the Local Recovery Point acceptance verifier with:

```powershell
pwsh ./build.ps1 local-recovery-point-policy
```

## Documentation

- [Product documentation](docs/README.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Foundation roadmap](specs/ROADMAP-001-foundation-implementation-sequence.md)
- [Foundation backlog](specs/BACKLOG-001-foundation-first-12-weeks.md)
- [Architecture decisions](adr/)
- [Engineering commands](eng/README.md)

The public website has deliberately been removed from this repository. It will
be rebuilt as a complete web application closer to public launch.

## Maintainers and attribution

Opure is maintained and attributed exclusively to:

- **H4ZEY86**
- **DevMediaDesign**

Automated tools are not recognised as contributors or authors. See
[CONTRIBUTORS.md](CONTRIBUTORS.md) for the repository attribution policy.

## Repository

[github.com/H4ZEY86/Opure](https://github.com/H4ZEY86/Opure)
