# FND-033 Workspace Snapshot Limit Rationale

Result: Passed

Workspace Service owns snapshot construction. Project Service supplies one
opaque Project ID and one opaque verified-root reference; neither grants the
wire caller a path or direct filesystem handle.

The contract caps a request at 4 KiB and a response at 32 MiB. A generation may
observe at most 100,000 entries and 4 GiB of aggregate file size within 30
seconds. These limits admit large local repositories while bounding allocation,
enumeration work and IPC framing. Callers may request smaller limits but cannot
increase the owner policy.

Logical paths use forward slashes and reject roots, drive syntax, backslashes,
empty components and traversal components. Entries contain metadata and hashes,
never raw file content. Hashes support change comparison without converting the
snapshot into a content store.

Complete is reserved for a non-zero generation that is current and reached no
limit. A limit-bounded result is Partial and never current. Cancellation has no
generation, entries or aggregates and cannot claim Complete. Interrupted or
invalidated generations therefore cannot be mistaken for the current snapshot.

The 100,000-entry and 4 GiB bounds are foundation defaults, not an assertion
that enumeration is implemented. FND-034 owns inventory generation and must
measure these limits against real Windows fixtures before changing them.
