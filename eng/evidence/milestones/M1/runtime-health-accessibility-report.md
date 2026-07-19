# FND-013 Runtime Health Accessibility Report

**Result:** Passed by automated Avalonia headless, contract and native-window checks.

## Verified checks

- Refresh, copy, service-list and diagnostic-disclosure controls have explicit tab order and stable automation identifiers.
- Runtime status, projection freshness, Safe Mode and degraded state use text and screen-reader names rather than colour alone.
- Each service row exposes its service identifier, lifecycle state, readiness requirement, safe detail and stable failure code in one bounded accessibility label.
- The full boot identity is copied unchanged while only its shortened form is shown in the summary.
- The Runtime Health surface declares no fixed foreground, background or border colours, so platform high-contrast resources remain authoritative.
- A 64-row service projection materialises within the tested performance bound and remains keyboard inspectable.
- The native Windows Desktop window launches, refreshes asynchronously and closes within a bounded deadline.

## Scope

The automated Narrator-label test validates the UI Automation names consumed by Narrator. Listening-quality review with Windows Narrator remains a release usability activity; it is not represented as an automated assistive-technology certification.