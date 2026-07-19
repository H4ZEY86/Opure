# FND-009 Named-Pipe Transport Prototype

**Result:** Passed

## Verified

- Desktop Gateway receives the versioned Runtime Health projection through gRPC over the exact Windows named pipe.
- Runtime binds HTTP/2 only through Kestrel's named-pipe transport and owns no TCP or UDP listener.
- Endpoint names are random, channel-qualified and paired with one Runtime boot identity; they are identifiers, not credentials.
- Missing endpoints return `HEALTH_TRANSPORT_UNAVAILABLE` under a bounded connection timeout.
- Live-call deadline expiry returns `HEALTH_TRANSPORT_DEADLINE_EXCEEDED`.
- Caller cancellation closes the call within the one-second test bound.
- Oversized requests are rejected before transport and response limits are configured on client and server.
- Runtime restart rotates both boot and endpoint identity; a new Desktop client reconnects through the latest descriptor.
- Transport implementation remains behind `Opure.Ipc.Abstractions` and the Windows adapter.
- Connection evidence contains no request or response payloads.

## Deliberately deferred

Named-pipe ACLs, mutual session proof, replay protection and stale-session authentication belong to FND-010.

## Evidence files

- `runtime-health-transport-latency.json`
- `runtime-health-network-listeners.json`
- `runtime-health-transport-report.md`