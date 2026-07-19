# FND-008 Runtime Health Compatibility Matrix

**Result:** Passed

## Ownership and authority

- Runtime Contracts owns the health contract.
- The checked-in protobuf schema is authoritative.
- Generated C# client and server types are build artefacts and are not checked in.
- Health responses are non-authoritative projections.
- Named-pipe transport and session authentication are deliberately deferred to FND-009 and FND-010.

## Revision policy

| Client range | Runtime revision | Result |
|---|---:|---|
| 1–1 | 1 | Compatible; revision 1 selected |
| 1–2 | 1 | Compatible; revision 1 selected |
| 2–3 | 1 | Rejected with HEALTH_CONTRACT_INCOMPATIBLE |
| unspecified–1 | 1 | Rejected as an invalid implicit range |

- New optional fields may be added without reusing field numbers.
- Unknown fields are preserved by the protobuf runtime.
- Unknown enum values fail semantic validation with a stable safe error.
- A breaking schema change requires a new major contract range.

## Bounded policy

- Default unary deadline: 2 seconds.
- Maximum request: 4096 bytes.
- Maximum response: 65536 bytes.
- Maximum service summaries: 64.
- Boot identity and query identity use 32-character lowercase hexadecimal values.
- Paths, secrets, raw exceptions, arbitrary maps, Any, Struct and unbounded bytes fields are excluded.

## Dependency review

| Package | Version | Purpose | Licence |
|---|---:|---|---|
| Google.Protobuf | 3.35.1 | Protobuf message runtime | BSD-3-Clause |
| Grpc.Core.Api | 2.80.0 | Generated client/server API surface | Apache-2.0 |
| Grpc.Tools | 2.82.0 | Build-time protoc and C# gRPC generation | Apache-2.0 |

Package versions and licences were reviewed against official NuGet metadata on 19 July 2026. Grpc.Tools remains private to the build and is not a runtime dependency.