# M0 Solution Baseline Evidence

**Ticket:** FND-001  
**Generated:** 2026-07-19T10:26:28+01:00  
**SDK:** 10.0.302  
**Target framework:** net10.0  
**Solution:** Opure.slnx  
**Verification:** restore, build and test succeeded  

## Initial Projects

- src/Runtime/Opure.Runtime.Contracts/Opure.Runtime.Contracts.csproj
- src/Runtime/Opure.Runtime/Opure.Runtime.csproj
- src/Bootstrap/Opure.Bootstrap.Windows/Opure.Bootstrap.Windows.csproj
- 	ests/Architecture/Opure.ArchitectureTests/Opure.ArchitectureTests.csproj

## Boundary Established

- Runtime depends on Runtime.Contracts.
- Bootstrap does not reference Runtime implementation.
- Architecture tests are separate from production projects.
- Desktop is deliberately deferred to FND-005.
- Central package policy is deliberately deferred to FND-002.
- Version generation is deliberately deferred to FND-003.

## Evidence Files

- dotnet-info.txt
- solution-projects.txt

## Result

FND-001 solution baseline verification passed.