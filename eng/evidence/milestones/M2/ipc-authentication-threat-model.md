# FND-010 IPC Authentication Threat Model

**Result:** Passed

## Protected assets

- Runtime Health named-pipe access.
- Bootstrap-issued ephemeral session material.
- Runtime boot and intended Desktop process identity.
- Authentication evidence integrity and confidentiality.

## Threats and controls

| Threat | Control | Verified outcome |
| --- | --- | --- |
| Another Windows user opens the pipe | Protected explicit DACL grants only the current user and LocalSystem | No World, Anonymous or inherited access rule |
| Unrelated same-user process calls Runtime | Per-call HMAC proof using Bootstrap-issued material | Missing material is denied with a stable public code |
| Client claims another PID | Signed PID is compared with the Windows-reported pipe client PID | Mismatch is denied |
| Captured proof is reused | Issued-time bound plus bounded nonce replay cache | Second use is denied |
| Stale material survives Runtime restart | Proof binds Runtime boot identity and fresh restart material | Prior session is denied; fresh session recovers |
| Runtime is impersonated to Desktop | Runtime returns a separately labelled server proof bound to the client nonce and proof | Desktop rejects an invalid server proof |
| Authentication material reaches evidence | Logging providers are cleared and Trust events contain bounded classifications only | Canary, credential-shape and command-line scans pass |

## Residual boundaries

- LocalSystem remains an operating-system administrative trust boundary.
- Compromise of the intended Desktop process remains outside IPC peer-authentication scope.
- Session material is held in managed process memory for the bounded session lifetime; temporary cryptographic byte buffers are zeroed.

No SID, PID, pipe name, session identifier, nonce, proof or key is recorded in this report.