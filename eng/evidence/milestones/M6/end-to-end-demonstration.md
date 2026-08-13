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
root, records Runtime and Desktop identities, and emits a SHA-256 receipt under
the ignored `artifacts/evidence/founder-gate-a` directory.

The runner also fails if a product process in the Bootstrap-owned process tree
is not Runtime or Desktop, if a child owns a TCP or UDP endpoint, if Bootstrap
reports a failure, if stderr is populated, or if the fixture changes. Windows'
console host is recorded as permitted platform infrastructure, not as a product
capability. These assertions
prove only the bounded launch prerequisite. They do not complete the remaining
UI, configuration mutation, restart, durability, recovery or Trust Centre
steps.

No AI runtime, agent loop, skill host, plugin host, MCP server, connector,
browser storage or Linux-style data path is part of this demonstration.

Gate A remains open until every checklist item has passing evidence and the
founder decision is reviewed and recorded.
