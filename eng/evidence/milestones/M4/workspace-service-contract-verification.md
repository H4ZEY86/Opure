# FND-033 Workspace Service Contract Verification

Result: Passed

Workspace Service owns the revisioned Create, Get and Invalidate Snapshot
contract. Project Service now requests its initial snapshot through that
Workspace-owned interface using only an opaque Project ID, opaque verified-root
reference and explicit count, byte and duration limits.

A stored snapshot is bound to one Project, one root reference and a non-zero
generation. File entries use portable logical paths, bounded metadata and
optional hashes. Absolute paths and raw file content are absent from both the
framework-neutral request seam and protobuf schema.

Cancellation, limit exhaustion and invalidation cannot claim a current Complete
generation. Cross-project and cross-root responses fail validation. Unsupported
file objects have an explicit stable representation, while unknown future enum
values return a stable error without throwing or acquiring authority.

This ticket defines and verifies the owner boundary only. FND-034 remains
responsible for filesystem enumeration, generation persistence and real
inventory recovery. No Desktop file access, mutation, network access, plugin
loading or Trust receipt is introduced here.
