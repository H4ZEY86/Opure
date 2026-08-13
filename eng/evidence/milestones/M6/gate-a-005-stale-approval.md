# GATE-A-005 stale-approval report

Result: Passed.

An approval binds the exact ordered proposal, target, source identifier, base
profile revision and digest, plus the observed Workspace generation and content
hash when present. A changed proposal, intervening profile revision, or changed
Workspace source invalidates the approval before persistence or evidence emission.

The proposal binding uses SHA-256 and comparison of proposal digests uses a
fixed-time operation.
