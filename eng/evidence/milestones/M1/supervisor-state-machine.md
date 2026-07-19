# FND-007 Supervisor State-Machine Report

**Result:** Passed

## Observed transitions

- Normal/Ready -> Recovering/Crashed -> Recovering/Starting -> Normal/Ready
- Normal/Ready -> Recovering/Crashed -> SafeMode/Quarantined

## Policy

- Runtime restart attempts: maximum 3 within 30 seconds.
- Backoff: 100 ms, 200 ms, 400 ms, capped at 2 seconds.
- Each Runtime start has a random supervisor instance ID, PID, start time, executable hash, product version and Runtime boot ID.
- PID equality alone is never treated as process identity.
- Controlled shutdown is classified as policy_stop; a non-zero unexpected exit is classified as crash.
- Safe Mode projects a quarantined Runtime and bounded restart count to Desktop without exposing environment values.

## Recovery scenario

- Runtime starts observed: 2
- Runtime crashes observed: 1
- Successful recoveries observed: 1
- Elapsed milliseconds: 4609

## Crash-loop scenario

- Runtime starts observed: 4
- Safe Mode transitions observed: 1
- Exit code: 60
- Elapsed milliseconds: 8046