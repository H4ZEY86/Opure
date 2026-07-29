# FND-020 Redaction and Canary Verification

Result: Passed

Observability owns the versioned local-diagnostics redaction profile. Producers
remain constrained by fixed event definitions and typed attribute allowlists.
The redactor then rejects prohibited field classes, detects direct and encoded
credential or project-text canaries, and replaces an absolute path value with a
safe category before queue admission or persistence.

The profile fails closed. A processor failure discards the candidate event,
increments bounded diagnostics health and emits only a fixed warning definition
with a stable finding code. It never copies the rejected value into the warning,
ordinary logs, trace attributes or evidence.

The evidence gate runs the complete Release verification, focused exact,
pattern, encoded, path, exception-metadata, trace-tag and failure-injection
tests, plus the observability architecture boundary test. It then scans the
generated reports and retained trace evidence for prohibited values and path or
credential patterns.

Operational diagnostics and these reports are local, non-authoritative
engineering evidence. Trust Evidence persistence and query remain deferred to
FND-021 and later tickets.
