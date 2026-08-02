# FND-032 Project List UI Verification

Result: Passed

Project Service owns the registered-project projection and all Open or Remove
decisions. Desktop receives bounded display rows over the authenticated Runtime
pipe and never reads the Project database or project files. A registered Open
reuses the existing verified-root pipeline after re-acquiring and comparing the
stored Windows filesystem identity.

Unavailable projects remain visible. On disconnection, Desktop retains the last
successful ordering only as an explicitly stale projection and replaces it only
after a successful query. Remove archives the registration and records the
lifecycle decision; it does not delete or modify developer-owned project files.

The list uses framework virtualisation, exposes keyboard selection and commands,
and provides stable automation metadata. Each row narrates project name,
availability, repository class and a path-free storage summary. Availability is
also text, never colour alone. A 10,000-row projection is covered by the bounded
performance acceptance test without per-item Desktop I/O.
