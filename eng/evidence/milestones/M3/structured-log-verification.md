# FND-018 Structured Operational Logging Verification

Status: Passed

Channel: Development

The Runtime composes a framework-neutral operational logging contract through a mandatory bounded process queue and a local JSON Lines sink. Producers synchronously reduce events to fixed reviewed messages and per-event allowlisted attributes before enqueueing, then return without waiting for disk. The sink writes only beneath the established channel data root, pins the owned Windows directory chain, mutates validated file handles for rotation and retention, and reports bounded safe health signals after queue, write or recovery failures.

Each event carries UTC time, stable event and severity values, service identity and version, and the Runtime boot identity. Trace and safe operation identities are included only when supplied. Callers cannot supply message text. Messages, allowlisted attributes, string values, event bytes, queue capacity and cleanup work are bounded before persistence.

Operational logs are non-authoritative observations. They do not authorise service behaviour, create owner receipts, or substitute for Trust Evidence. Queue pressure and sink failure are isolated from domain behaviour, projected through Runtime Health with a fixed safe degradation code, and no external exporter or network listener is enabled.

Verification commands:

```powershell
pwsh ./build.ps1 structured-logging-policy
pwsh ./build.ps1 verify -Configuration Release
```

Evidence reviewed:

- schema and explicit value types;
- one independently parseable JSON event per physical line;
- rotation and retained-segment cleanup;
- path ownership and reparse-point refusal;
- pinned-directory and validated-handle path-swap resistance;
- control-character and line-break minimisation;
- prohibited diagnostic classes absent;
- fixed reviewed messages and exact per-event attribute schemas;
- pre-queue sanitisation, priority overflow and payload-free drop summaries;
- non-blocking producer admission and bounded completion/disposal;
- safe operational-diagnostics degradation in Runtime Health;
- transient sink failure recovery;
- mid-write failure and cancellation quarantine;
- partial final-line crash recovery;
- Runtime service and boot identity;
- architecture separation from authoritative Trust Evidence.

Focused coverage is provided by `JsonLinesOperationalLogSinkTests`, `JsonLinesOperationalLogSinkMidWriteRecoveryTests`, `JsonLinesOperationalLogSinkPathPinningTests`, `OperationalLogAttributeAllowlistTests`, `BoundedOperationalLoggerTests`, `RuntimeHealthRequestHandlerTests` and `ObservabilityBoundaryTests`.
