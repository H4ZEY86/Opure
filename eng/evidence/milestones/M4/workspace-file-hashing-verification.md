# FND-035 Safe Workspace File Hashing Verification

Workspace Service hashes an included regular-file inventory entry only through a
no-follow, identity-verified Windows handle. SHA-256 is streamed with a bounded,
cleared buffer. Size, last-write state, handle identity and current path identity
are checked before a stable result is returned.

The acceptance suite covers a standard known-answer vector, reproducibility,
concurrent modification, object replacement, maximum size, sharing-lock denial,
cancellation, reparse substitution, content-canary exclusion and bounded
throughput. Unstable or unreadable entries return explicit reason codes and an
empty content hash; no prior hash is inherited.

Hash evidence proves observed content identity only. It does not state that file
content is safe, trusted or suitable for execution.
