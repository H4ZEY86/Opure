# GATE-A-001 End-to-End Foundation Demonstration

Status: **In progress**

The authoritative 32-step checklist is recorded in
`founder-gate-a-001-checklist.json`. The bounded launch prerequisite is run
with:

```powershell
pwsh ./build.ps1 founder-gate-a-launch -Configuration Release
```

The runner builds the product, creates a disposable Git fixture, launches the
Development channel through Bootstrap with an isolated local application-data
root, records Runtime and Desktop identities, proves authenticated CLI health
with server proof and invalid-session denial, opens the fixture through the
authenticated Project gateway, verifies its root identity and Git projection,
composes Workspace and Configuration owners, proves Product Defaults, the User
Base Profile and per-key provenance, then exercises valid, invalid and repaired
project settings while the last-known-good snapshot remains authoritative, and
emits a SHA-256 receipt under the ignored
`artifacts/evidence/founder-gate-a` directory.

The runner also fails if a product process in the Bootstrap-owned process tree
is not Runtime or Desktop, if a child owns a TCP or UDP endpoint, if Bootstrap
reports a failure, if stderr is populated, or if the fixture changes. Windows'
console host is recorded as permitted platform infrastructure, not as a product
capability. The session secret remains in process memory and is neither written
to logs nor included in the receipt. All 32 steps are automated. The runner
proves the three read-only Trust Centre projections, a clean Desktop reconnect
while Runtime remains ready, supervised Runtime recovery with rotated boot and
session identities, durable Project and Configuration state, and a structurally
verified same-device Recovery Point materialised into a disposable restore root
without modifying active owner databases.

No AI runtime, agent loop, skill host, plugin host, MCP server, connector,
browser storage or Linux-style data path is part of this demonstration.

Every checklist item now has passing automated evidence. The founder decision
remains a separate review and sign-off action.
