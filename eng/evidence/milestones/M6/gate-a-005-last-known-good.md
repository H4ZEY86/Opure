# GATE-A-005 last-known-good report

Result: Passed.

A valid project observation activates a snapshot. A following invalid observation
records its separate generation and safe diagnostic without replacing that active
snapshot. A later valid observation advances the active generation exactly once.

The Trust projection exposes the active project generation alongside the latest
observed generation, latest valid generation, retained snapshot identity and last
error, so an invalid observation cannot be mistaken for active configuration.
