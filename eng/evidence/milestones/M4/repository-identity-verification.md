# FND-031 Repository Identity Verification

Result: Passed

Project Service owns the authoritative repository observation. The Git adapter
is local and read-only: it revalidates the verified Windows project root, does
not start Git or another process, does not fetch or push, and does not invoke a
credential provider. Repository writes remain deferred to Repository Service
operations in later tickets.

The observation records exact local HEAD, bounded branch metadata, aggregate
working-tree state and a move-stable, replacement-sensitive digest of the Git
administrative directory's Windows file identity. A non-Git root is explicit.
Parent repositories, external worktree metadata and corrupt metadata produce a
safe degraded observation without preventing the valid Project from opening.

Remote configuration is minimised before persistence. User information, query
and fragment are discarded; only a count and SHA-256 fingerprint are retained.
Tests use an embedded credential canary and verify it is absent from returned
observations and the authoritative Project database. Trust receives the typed
`repository.observed` Verified Service receipt with Project and operation
correlation and no path, remote URL, file name or credential material.
