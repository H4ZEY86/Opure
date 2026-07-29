# FND-023 Trust Evidence database verification

Result: Passed

The Trust Evidence service owns the isolated `trust.db` file and its reviewed
forward migration catalogue. The shared SQLite persistence library supplies the
single-writer lease, WAL mode, `synchronous=FULL`, foreign-key enforcement,
bounded busy timeout and closed-state health.

The database stores immutable Evidence Record material and rebuildable Trust
Centre projections. It is not authoritative for the owner service decision or
effect represented by a record. Owner services do not write this database
directly.

The schema contains:

- Evidence Type identities and exact revisions;
- Evidence Records and separately constrained payload references;
- evidence relationships and owner sequences;
- the reusable immutable transactional inbox;
- projection checkpoints and safe projection rows;
- retention decisions;
- reviewed owner-sequence, project and operation indexes.

No FTS table exists. Inline or referenced payload content is not copied into
query indexes or projection rows. Operational logs remain in their separate
diagnostic store.

Projection reset deletes only rebuildable rows and checkpoints. It preserves
Evidence Records and reports the projection as incomplete. Missing projection
data therefore never means that no activity occurred.

Fresh creation, version-one upgrade, duplicate and foreign-key constraints,
reviewed query plans, projection reset, bounded health, missing-schema recovery,
architecture boundaries and the complete Release verification all pass.

Evidence files:

- `trust-database-schema.json`;
- `trust-database-migration-report.json`;
- `trust-database-query-plan.json`.

Record ingestion, duplicate acknowledgement and conflict quarantine are owned
and verified by FND-024. Owner-gap reconciliation remains deliberately deferred
to its later dependency-ordered ticket.
