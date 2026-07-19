# FND-014 SQLite Persistence Library Design

## Ownership boundary

`ServiceDatabaseAuthority` represents exactly one owning service under one absolute channel data root. It derives descriptors at `services/<owner>/databases/<name>.db`; callers cannot supply a database path or append connection-string options. `SqliteServiceDatabaseConnectionFactory` rejects descriptors from another authority before creating directories or opening a file.

The library is infrastructure only. It owns no domain database and the Runtime, Bootstrap and Desktop projects do not reference it. A future service-specific persistence assembly will hold that service's schema and SQL. Desktop continues to receive projections through the Desktop Gateway.

## Reviewed connection profile

- `Microsoft.Data.Sqlite` 10.0.10.
- `SQLitePCLRaw.bundle_e_sqlite3` 2.1.12 with loaded native SQLite 3.53.3.
- Absolute canonical data source, read/write/create mode, private cache and pooling disabled.
- Foreign keys enabled and verified on every connection.
- `trusted_schema` disabled and verified.
- WAL requested and verified; no silent journal fallback.
- `synchronous=FULL`, 2,000 ms busy timeout and memory mapping disabled.
- Positive SQLite `application_id` established for an empty owned file and verified on reopen.
- `quick_check` must return `ok` before the database is reported open.

## Transactions and writer policy

Every state-changing operation enters one bounded in-process writer gate and an immediate SQLite transaction. A process-wide canonical-path lease prevents a second factory from opening another owning writer for the same database. Domain exceptions unwind the transaction and SQLite errors become stable persistence error codes.

## Health and recovery

An opened database reports bounded Open health with the verified profile and loaded SQLite version. Disposal reports Closed health and releases the writer lease. A malformed file, wrong application identifier, unsafe reparse path or configuration failure is not deleted or replaced. The owner receives a stable error and can enter Failed or Recovery Required.

## Deliberately deferred

FND-014 does not create a Runtime database. Ordered migrations, migration history and recovery points belong to FND-015; transactional outbox publication belongs to FND-016; backup, restore, integrity scheduling, ACL hardening and service health publication remain later persistence work. ADR-0005 remains Proposed until its broader acceptance suite passes.