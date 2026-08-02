# FND-030 Project Open Trust Receipt Verification

Result: Passed

Project Service owns Project registration and Open lifecycle authority. A
successful state transition commits its typed Evidence Record to the Project
transactional outbox in the same SQLite transaction. Trust Evidence remains a
separate query projection and cannot authorise, reverse or replace Project
state.

Runtime binds the ingestion port to `opure.project`; an ordinary publisher
cannot choose a different authenticated owner. Delivery is bounded,
at-least-once and idempotent. If Trust Evidence is unavailable, the Project
operation remains honestly successful, backlog health reports the pending
receipt, and a later Project or Trust restart resumes delivery. A failed Open
does not emit `project.opened`.

The receipt payload contains pseudonymous Project and operation identifiers,
safe root class, repository state and lifecycle state. It does not contain the
raw selected path, project content, authentication material or secrets. Trust
queries omit the inline payload while returning the verified owner receipt and
its Authority Class.

The evidence gate runs the complete Release verification, focused transaction,
failure, duplicate, owner-binding, path-minimisation and restart tests,
type-filtered outbox regression coverage, Evidence Type contract checks and
architecture boundaries. Generated JSON is scanned for absolute paths and
credential-like material.
