# GATE-A-002 Crash and Restart Recovery Suite

Status: **Ready**

The authoritative scenario mapping is recorded in
`gate-a-002-crash-recovery-matrix.json`. Run the complete bounded verifier with:

```powershell
pwsh ./build.ps1 founder-gate-a-crash-recovery -Configuration Release
```

The verifier runs the complete Release build and test suite, then repeats the
Bootstrap and end-to-end crash classes that exercise Desktop failure, ordinary
close, Runtime termination, crash-loop Safe Mode, Project, Workspace,
Configuration and Trust Evidence termination, backup cancellation and backup
worker failure. It also validates the existing transactional outbox, inbox,
database-integrity, Workspace recovery and Recovery Point evidence.

Workspace and Configuration termination hooks are armed through a disposable
file and require the exact Bootstrap test-mode marker. They cannot be activated
by an ordinary product launch. Each Runtime recovery must rotate both the
process identity and Runtime boot identity before durable Project state is
queried.

The execution receipt is written under ignored
`artifacts/evidence/founder-gate-a` and binds the committed matrix plus its
durability evidence using SHA-256. It contains no session material, absolute
project path or database content.
