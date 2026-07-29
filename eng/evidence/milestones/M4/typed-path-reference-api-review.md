# FND-026 typed path-reference API review

The public contract distinguishes `UntrustedPathText` from
`LogicalWorkspacePath`; neither converts implicitly to an operating-system
path. Logical paths are bounded, slash-separated and relative to one registered
root.

`WindowsRegisteredWorkspaceRoot` captures a developer-selected root together
with its handle-derived final path, volume and file identity.
`VerifiedWindowsPathReference` owns the live handle and exposes immutable
verification metadata. Its caller must dispose it.

`ResolveExisting` walks components with no-follow handles, denies every reparse
point, checks the handle-derived final path and volume, enumerates alternate
streams and revalidates identity. `Revalidate` compares the current root and
leaf identities against the earlier verified reference.

No public contract accepts raw SQL, shell commands, path concatenation,
filesystem mutation, network access or persistence. Error messages contain
stable failure classes and Win32 codes but do not reproduce supplied paths.
The adapter is Windows-specific; contracts remain framework neutral.
