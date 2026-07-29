# FND-022 Evidence Record Verification

Result: Passed

Trust Evidence owns the provisional `opure.trust-evidence-record/1` envelope.
Each immutable record binds an opaque Evidence ID to an exact Evidence Type
revision and definition hash, owner service and owner-record revision,
Authority Class, release channel, subject, action, outcome, source and
observation times, owner sequence, retention, preservation state, payload hash
and canonical record hash.

Project scope requires an opaque project identity. Operation, workflow, trace,
span and Runtime boot references are optional bounded correlations and do not
grant authority. Source occurrence time and Trust Evidence observation time
remain distinct and are normalised to UTC; sequence remains the preferred
ordering signal.

Inline JSON is canonicalised and limited to 64 KiB. Owner and
content-addressed references carry an explicit payload size and SHA-256 digest
and are limited to 256 MiB by this initial contract. Undeclared, incorrectly
typed, secret-classified and prohibited field names are rejected, and a payload
cannot be labelled below the strongest classification of its fields. The
payload digest, definition digest and every semantic envelope field feed the
framed SHA-256 record identity.

The evidence gate runs the complete Release suite, required and conditional
field fixtures, owner and authority rejection, project-scope validation,
payload bounds and reference forms, prohibited-field tests, JSON order
canonicalisation, a fixed record-hash vector and framework-neutral architecture
checks. Persistence, deduplication, quarantine and ingestion remain assigned to
FND-023 and FND-024.
