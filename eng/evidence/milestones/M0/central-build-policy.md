# FND-002 Central Build Policy Evidence

**Ticket:** FND-002  
**SDK:** 10.0.302  
**Build channel tested:** Development  
**Configuration tested:** Release  
**Result:** Passed  

## Verified

- Central Package Management is enabled.
- Project-local package versions are prohibited.
- Package version overrides are disabled.
- Package lock files are generated and committed.
- Locked restore succeeds for the committed graph.
- Locked restore fails for a deliberately stale graph.
- Nullable analysis is enabled.
- A nullable compiler warning fails the build.
- .NET analyzers and build code-style enforcement are enabled.
- Production projects do not inherit test-only packages.
- Release assemblies reproduce identical SHA-256 hashes across two clean artifact builds.
- Build output is centralised under rtifacts/.
- The local dotnet-coverage tool is pinned by the repository manifest.
- Development, Preview and Stable build constants are explicit.

## Evidence Files

- dependency-graph.txt
- deterministic-build-hashes.txt
- project-level packages.lock.json files

## Recovery

Every policy file and lock file is source controlled. Reverting the FND-002 commit restores the FND-001 build policy without machine-wide changes.