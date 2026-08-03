# FND-039 Setting Definition Review

Result: Passed

- Configuration Service owns definition registration and future effective-value resolution.
- The packaged catalogue is authoritative for definition shape; it does not make mutable values active.
- Every definition binds an exact revision, owner, type, default requirement, scopes, sources, merge strategy, null semantics, sensitivity, secret policy, application class, restart impact and SHA-256.
- Project sources cannot target a definition without Project scope.
- Ordinary secret values are prohibited. The catalogue can carry only opaque Vault references or a declared secret-derived boolean.
- Same-revision semantic replacement is rejected; later catalogues retain exact historical revisions.
- Policy Definition registration, mutable profile values, persistence, source parsing, merge execution and effective snapshots remain deliberately deferred to FND-040 and later tickets.
