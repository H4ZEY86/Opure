# FND-034 Workspace Inventory Algorithm

Result: Passed

Workspace Service owns inventory generation. The Windows adapter accepts one
verified-root capability and iteratively enumerates one directory at a time.
Before any entry becomes a normal inventory record, the adapter parses its leaf
as a logical Workspace path, reopens every component without following reparse
points, verifies handle-derived identity and final containment, and confirms the
root has not been replaced.

Final reparse objects are inspected and recorded as denied metadata but are
never queued for traversal. Symbolic links, junctions, mounted folders, cloud
placeholders and unknown tags therefore cannot add target content to the
inventory. A concurrent rename, deletion, replacement or inaccessible entry
produces a hashed, path-safe issue and makes the scan Partial.

Hidden and system entries are included and explicitly labelled. Built-in
directory exclusions initially cover repository metadata, dependency trees,
build outputs, caches, model-owned state and known credential stores. Excluded
directories remain visible as metadata with a stable reason and are not
traversed. Temporary file suffixes are similarly recorded as excluded.

The walk is iterative rather than recursive. Entry count, directory count,
depth and elapsed-time budgets are checked independently. Cancellation throws
before a result can claim completion. Normal entries contain only logical paths,
size, observed modification time, classification, disposition and a one-way
file-identity digest. No file content is opened or read.

Inventory is transient in FND-034. It cannot replace a previous current
snapshot because Workspace generation persistence and atomic current-pointer
updates remain FND-036.
