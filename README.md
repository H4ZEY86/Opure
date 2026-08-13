# Opure

> Developer Respect. Local Intelligence. Complete Control.

Opure is a local-first, developer-controlled software engineering platform for
Windows 11. It helps developers design, understand, modify, build, test and
operate software without surrendering authority to autonomous behaviour.

The repository contains the trusted Runtime, Avalonia Desktop, Bootstrap,
`opure` CLI, service contracts, persistence libraries, verification tooling and
the specifications that govern their behaviour.

## Current status

Foundation tickets FND-001 through FND-060 are implemented. The active roadmap
work is Founder Gate A, beginning with the repeatable end-to-end foundation
demonstration.

Opure remains pre-release software. The current recovery capability is explicitly
same-device only and is not a substitute for an independent device-loss backup.

## Build and verify

The repository requires the .NET SDK pinned by `global.json`.

```powershell
pwsh ./build.ps1 verify -Configuration Release
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
