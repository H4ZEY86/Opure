# FND-026 filesystem threat model

## Ownership and authority

The Windows filesystem adapter owns path validation and handle-derived identity.
The framework-neutral contract owns only untrusted text, portable logical paths
and identity value types. Neither layer grants mutation authority. Callers
remain responsible for developer consent and operation policy.

## Threats and controls

Untrusted absolute, drive-relative, root-relative, UNC, device, NT and named-pipe
names are rejected before root registration. Logical paths reject traversal,
backslashes, alternate-stream syntax, control characters, trailing dots or
spaces and reserved device names.

The adapter opens the registered root and every path component with
open-reparse-point semantics. Reparse points are classified and denied. The
final handle supplies the canonical path, 128-bit file identity, volume serial,
object type and link count. Containment is checked only after those handle
observations. Alternate streams are enumerated and the named object is reopened
to detect replacement during inspection.

Root and leaf identities are revalidated before later use. A replacement is
reported rather than silently accepted. Handles share deletion so the
original object remains inspectable during a rename race.

## Deliberate limits

This ticket does not authorise create, write, rename or delete operations.
Reparse traversal is denied even when its target would remain inside the root.
Network and removable volumes are classified but mutation policy is deferred.
Recovery, journalling, developer approval and rollback belong to later
filesystem-operation tickets. A developer can stop all behaviour by disposing
the verified reference; no background worker or watcher is created.
