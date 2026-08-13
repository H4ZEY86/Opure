# GATE-A-005 schema-reference denial report

Result: Passed.

The local schema registry resolves only explicitly registered in-process schemas.
Remote URI and local-file `$ref` inputs fail resolution. The configuration path
does not open a network client or read an arbitrary file to resolve a schema.

Proof is executable in `LocalSchemaRegistryTests`.
