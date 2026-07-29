# FND-019 Trace Propagation Verification

Result: Passed

Observability owns non-authoritative operational trace policy. Desktop Gateway
creates the client operation span, W3C trace context crosses authenticated gRPC
metadata, Runtime creates the server span, and the Runtime Health owner creates
the child service span.

Only stable, low-cardinality attributes are admitted. Project payloads, source,
paths, request and response bodies, authentication material and baggage are
excluded. Runtime completion logs contain the trace identity and bounded span
identity while the trace remains active.

Development sampling is explicitly enabled. Stable and Preview sampling is
disabled until retention and export policy are reviewed. Disabling tracing
does not change request admission, authentication, authorisation, cancellation,
domain state or recovery.

The evidence gate runs the full Release verification, focused trace-policy
tests, authenticated named-pipe propagation tests, cancellation, stable error,
payload-canary, high-cardinality and latency tests. It then performs a bounded
Bootstrap launch and verifies a Desktop-to-Runtime trace identity in the
Runtime operational log.

Trace loss cannot block a domain operation or recovery. Process-local sampled
and dropped activity counters are bounded diagnostic health only. External
export remains disabled.
