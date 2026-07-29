# FND-024 Trust Evidence ingestion verification

Result: Passed

The Trust Evidence ingestion pipeline accepts owner identity only from an
authenticated, time-bounded local transport session context. The submitted
Evidence Record does not authenticate itself, and neither session identifiers
nor session secrets are persisted.

Before admission the pipeline validates:

- the authenticated owner against the registered Evidence Type owner;
- the exact ingestion contract and Evidence Type revisions;
- the Evidence Type definition, owner and Authority Class binding;
- declared payload and record SHA-256 bindings;
- relationship eligibility;
- the supported SQLite owner-sequence range.

The immutable inbox receipt, Evidence Record, payload reference, owner sequence,
relationships, Verified Service Receipt projection, retention decision,
ingestion receipt and any detected owner gap commit in one SQLite transaction.
An injected projection failure rolls back every member, including the inbox
receipt.

Matching message retries are acknowledged without a second domain effect and
retain the same stable receipt identity across a Trust service restart. A
changed record under the same message identity enters the retained inbox
conflict ledger without replacing the accepted record or persisting the
conflicting payload. Unknown types and record or sequence conflicts retain only
bounded safe quarantine metadata.

Sequence gaps are visible and mark the projection incomplete. A missing
projection or gap never proves that no owner activity occurred. The owner
service remains authoritative for its decision or effect.

Evidence files:

- `trust-ingestion-contract.json`;
- `trust-ingestion-owner-authentication.json`;
- `trust-ingestion-duplicate-conflict.json`.

Owner reconciliation and query contracts remain dependency-ordered work for
FND-025 and FND-026.
