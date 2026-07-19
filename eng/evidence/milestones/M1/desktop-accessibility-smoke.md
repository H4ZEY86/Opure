# FND-005 Accessibility Smoke

**Result:** Passed by automated headless checks; manual Narrator confirmation remains part of the broader ADR-0002 prototype.

## Automated checks

- Home, Projects, Workflows and Trust Centre controls are tab stops.
- Tab order is explicit and stable from 1 through 4.
- Each primary navigation control exposes a stable automation name.
- Each primary navigation control exposes a stable automation identifier.
- Runtime unavailable status exposes explicit text and automation metadata.
- Disconnected state is conveyed by words, not colour alone.
- The real Windows window launches and closes cleanly.

## Manual follow-up retained by ADR-0002

- Narrator announcement quality.
- High-contrast usability.
- 100%, 150% and 200% scaling.
- Mixed-DPI and multi-monitor movement.