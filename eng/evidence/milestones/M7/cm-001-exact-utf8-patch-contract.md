# CM-001 Exact UTF-8 Patch Contract

Status: Passed

The framework-neutral `Opure.Patch.Contracts` assembly defines version 1 of a
single-file create-or-replace proposal. It is a proposal boundary only and owns
no filesystem, apply, persistence, Desktop, IPC, process or network authority.

## Exact binding

- opaque patch, project, root and target path-reference identities;
- exact base Workspace generation number and SHA-256;
- create-target-absent or replace-source-hash-and-size precondition;
- explicit UTF-8 bytes without BOM;
- explicit preserve/project/LF/CRLF line-ending intent;
- developer or deterministic-service creator only;
- bounded printable intent summary and UTC creation time;
- and computed resulting content SHA-256.

## Denied representations

- invalid UTF-8, BOM, NUL and content above 4 MiB;
- binary or unknown encoding;
- absolute, traversal, stream or slash-bearing raw paths in place of a path reference;
- create proposals carrying source state;
- replace proposals without exact source state;
- unknown contract revisions or creator kinds;
- AI, plugin, MCP or external creator authority;
- and any apply/write method in the contract assembly.

## Evidence

- 11 Patch contract tests;
- Patch architecture boundary test;
- deterministic SHA-256 known vector;
- package lock file and central build policy;
- and complete Release verification.

Next dependency: CM-002 Patch state store and transition machine.
