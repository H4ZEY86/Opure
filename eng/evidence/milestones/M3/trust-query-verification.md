# FND-025 Trust Evidence query verification

Result: Passed

The Trust Evidence query surface is the framework-neutral
`opure.trust-query/1` contract. It accepts only typed release-channel,
project, operation, Evidence Type, Authority Class, outcome and UTC time-range
fields. There is no raw SQL, regular-expression or arbitrary-expression field.

The authenticated local transport supplies a bounded session context containing
the client identity, one release channel, an explicit set of authorised opaque
project identifiers and a maximum fifteen-minute lifetime. The query service
checks authentication, lifetime, channel and project authority before touching
the database. The context contains no authentication material and is not
persisted.

Queries are limited to 31 days and 100 results per page. Pagination uses a
bounded base64url cursor bound to:

- the exact typed filter hash;
- the database-owned projection generation;
- the first-page calculation time;
- the first-page maximum projection row identifier;
- and the final ordered `(occurred_at_utc, evidence_id)` key.

The row bound excludes concurrent later ingestion even when clocks have the
same timestamp. A projection reset changes the database-owned generation and
requires the client to refresh rather than treating a stale cursor as complete.
Cursor integrity is local consistency evidence, not authentication; project and
channel authorisation is independently enforced for every page.

Only Verified Service Receipt projection metadata is returned. Payload content
and payload references are always omitted. Each snapshot reports calculation
time, projection generation, last projection update, owner availability,
completeness, effective-filter hash, result count and redaction metadata. Open
owner gaps, unavailable owners and rebuild-required state are reported rather
than silently described as complete.

The SQLite query is fixed and fully parameterised. Its reviewed
project/channel/time index and a bounded local latency smoke are recorded in the
query-plan evidence. Cancellation remains available to the caller. Network
transport, gateway admission-rate policy, free-text search, global queries and
live owner-health integration are deliberately outside FND-025.

Evidence files:

- `trust-query-schema.json`;
- `trust-query-cross-project.json`;
- `trust-query-plan-latency.json`.
