# FND-034 Workspace File Inventory Verification

Result: Passed

The Windows Workspace adapter generates a bounded logical inventory from a
verified project root. Every emitted entry has passed component-level no-follow
inspection, handle-derived identity capture and final containment validation.
Absolute paths remain adapter-private and do not enter inventory records,
issues or evidence.

The adversarial suite covers small, entry-limited and depth-limited trees;
symbolic links and junctions; hidden entries; case-preserving logical names;
cancellation; and deterministic mutation between enumeration and handle
inspection. Reparse targets are not traversed. Mutation yields a safe Partial
result with a hashed entry-name reference.

The initial exclusion policy is visible through stable per-entry reasons.
Inventory captures size, observed last-write time and an opaque SHA-256 digest
of platform file identity only. It never opens file content; content hashing is
owned by FND-035.

No inventory is persisted or made current in this ticket. A failure therefore
cannot alter a previous current Workspace Snapshot. Atomic generation and
current-pointer ownership remain explicitly deferred to FND-036.
