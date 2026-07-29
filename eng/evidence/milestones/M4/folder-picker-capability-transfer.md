# FND-027 capability transfer

```text
Developer
  -> Avalonia folder picker
  -> one untrusted local-path value
  -> Windows filesystem boundary
  -> handle-verified opaque root reference
  -> Project Service receiver port
```

The picker is a Desktop platform adapter. It acquires one selected path and
does not inspect project contents. Cancellation returns before filesystem
registration.

The Windows filesystem boundary validates namespace and components, opens the
selected directory without following a reparse point, and creates an opaque
reference containing identity and volume classification. The coordinator
immediately transfers that reference through `IVerifiedWorkspaceRootReceiver`.
The view model receives only display text and classification; it cannot obtain
the reference.

The Project Service implementation is deliberately deferred to FND-028. Until
that service is available, the shipped receiver refuses transfer, Desktop
reports the unavailable state, and the verified reference is not retained.
Selection is user intent, not durable registration evidence.
