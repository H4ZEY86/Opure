# FND-021 Evidence Type Verification

Result: Passed

Trust Evidence owns the provisional `opure.trust-evidence-type/1` contract and
foundation catalogue. Each registered type binds one stable identifier and
immutable revision to an owner service, Authority Class, typed payload schema,
safe indexes, relationship eligibility, retention policy, support-export
eligibility and redaction profile.

Exact catalogue resolution is required before later ingestion can regard a
record as trusted. Unknown types and revisions, owner or authority mismatches,
and definition-hash mismatches are rejected. Historical revisions remain
readable. A stable type cannot change owner or authority between revisions; a
different authority requires a new type identifier.

The initial catalogue defines nine foundation contracts. It does not publish
records or claim that every future owner service is implemented. Record
envelopes, persistence, ingestion, querying and reconciliation remain assigned
to FND-022 through FND-025.

The evidence gate runs the complete Release build and test suite, focused schema
and canonical-hash vectors, the reviewed catalogue fixture, authority-drift and
unknown-type adversarial tests, and framework-neutral architecture checks.
Generated reports contain only schema metadata and non-authoritative
engineering evidence.
