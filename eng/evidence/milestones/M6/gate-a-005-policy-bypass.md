# GATE-A-005 policy-bypass report

Result: Passed.

Project settings cannot override a setting whose allowed sources exclude the
project source, and project policy metadata cannot grant product authority or a
capability. Raw secret-like credential values fail closed. An evaluator exception
is converted to a failed policy receipt and no configuration revision is activated.

Per-key provenance is generated for every effective setting and remains available
for deterministic inspection.
