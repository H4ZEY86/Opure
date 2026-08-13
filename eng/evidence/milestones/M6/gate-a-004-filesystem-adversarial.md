# GATE-A-004 Filesystem Adversarial Suite

Status: **Ready**

Run the complete verifier with:

```powershell
pwsh ./build.ps1 founder-gate-a-filesystem -Configuration Release
```

The matrix binds all twenty backlog scenarios to executable Windows path,
inventory, hashing, reconciliation and end-to-end tests. Filesystem authority
comes from held Windows handles and file identities, never string-prefix checks.
Reparse targets are not traversed. Replacement, deletion, locking and watcher
loss produce explicit partial or stale results without replacing the current
Workspace generation.

Case-only and Unicode-normalisation collisions are compared using NFC plus
case-insensitive portable semantics. The inventory preserves exact names but
cannot claim completeness when a collision exists; evidence contains only
SHA-256 name hashes and a stable category.
