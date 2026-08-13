# GATE-A-003 IPC Security Suite

Status: **Ready**

Run the complete verifier with:

```powershell
pwsh ./build.ps1 founder-gate-a-ipc-security -Configuration Release
```

The suite covers all twelve backlog scenarios through the exact Windows
named-pipe adapter. It inspects the protected DACL, exercises missing, stale,
replayed and boot-mismatched session proofs, sends malformed bytes, verifies
message-size limits, deadlines and cancellation, exceeds the 32-connection
admission ceiling, proves endpoint names rotate, and reconnects after Runtime
restart.

The live Runtime listener check uses owning-process inspection for TCP and UDP.
Session material, pipe names, SIDs, PIDs, nonces and payloads are excluded from
committed evidence and the ignored SHA-256 execution receipt.
