# GATE-A-006 projection rebuild report

Result: Passed.

Projection reset preserves retained Evidence Records and explicitly reports that
projection absence does not establish an absence of activity. Rebuild clears only
rebuildable rows, projects every retained verified record, recreates per-owner
checkpoints and advances the database-owned projection state to Current.
Retained legacy rows without a matching successful ingestion receipt remain
unprojected and force Incomplete; rebuild cannot elevate their authority label.

A fresh Trust database is also reconstructed by replaying exact owner records
through the authenticated ingestion contract. Payload and authority validation
remain enabled during reconstruction.
