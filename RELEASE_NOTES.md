# Opure Release Notes

## v0.4.0 (Gate D)

Gate D is formally secured and released.

### Architecture Update: Scorched-Earth MSIX Migration
The MSIX virtualization layer was found to be structurally incompatible with Opure's strict named-pipe IPC and process hierarchy boundaries, masking standard streams and interfering with deterministic process launches. 

We have completely purged MSIX from the repository and pivoted to a deterministic **WiX v4 `.msi`** installer using **.NET 10 Single-File deployment**. This guarantees absolute IPC integrity and preserves the airtight, local-first, default-deny architecture.

Gate D is secured. The repository is hygienically clean. Opure is now ready for the V1 Public Release Sprint.
