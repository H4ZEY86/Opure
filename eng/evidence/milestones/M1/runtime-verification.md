# FND-004 Runtime Executable Evidence

**Ticket:** FND-004  
**Milestone:** M1  
**Result:** Passed  

## Verified

- The Runtime starts as a separate executable process.
- Every process start receives a distinct opaque boot identity.
- Lifecycle state is emitted as starting, eady, stopping and stopped.
- Product informational version and Runtime contract version 1.0 are reported.
- Controlled automatic shutdown completes within the five-second deadline.
- Unexpected startup failure returns stable exit code 20 and category startup_failure.
- The minimal Runtime writes no files and creates no data-root directory.
- The default data-root resolver is scoped to Local Application Data / Opure / Development.
- No Runtime-owned TCP connection or UDP endpoint exists during the readiness window.
- No AI, plugin, MCP, workflow, project or persistence service is loaded.
- No child process is started by the Runtime.
- Startup diagnostics contain no secrets and omit the absolute data-root path.

## Evidence Files

- untime-startup-trace.jsonl
- untime-process-inventory.txt
- untime-offline-network.txt

## Recovery

The Runtime creates no durable state in this milestone. A startup or shutdown failure leaves no partially committed service state.