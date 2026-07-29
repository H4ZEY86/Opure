# UK Data Protection Engineering Baseline

## Status and scope

This is an engineering control baseline for Opure as at 29 July 2026. It is
not legal advice, a certification, or a claim that a particular deployment is
compliant. Compliance also depends on the operator's purposes, lawful basis,
privacy information, records of processing, contracts, data-subject request
processes, incident response, retention decisions and any required data
protection impact assessment.

The Data (Use and Access) Act 2025 amends, but does not replace, the UK GDPR,
Data Protection Act 2018 or PECR. The ICO states that all of its data-protection
provisions are now in force.

Official references:

- [ICO data protection principles](https://ico.org.uk/for-organisations/uk-gdpr-guidance-and-resources/data-protection-principles/a-guide-to-the-data-protection-principles/)
- [ICO data protection by design and by default](https://ico.org.uk/for-organisations/uk-gdpr-guidance-and-resources/accountability-and-governance/guide-to-accountability-and-governance/data-protection-by-design-and-by-default/)
- [ICO storage limitation](https://ico.org.uk/for-organisations/uk-gdpr-guidance-and-resources/data-protection-principles/a-guide-to-the-data-protection-principles/storage-limitation/)
- [ICO security outcomes](https://ico.org.uk/for-organisations/uk-gdpr-guidance-and-resources/security/a-guide-to-data-security/security-outcomes/)
- [ICO encryption guidance](https://ico.org.uk/for-organisations/uk-gdpr-guidance-and-resources/security/a-guide-to-data-security/encryption/)
- [ICO summary of the Data (Use and Access) Act 2025](https://ico.org.uk/about-the-ico/what-we-do/legislation-we-cover/data-use-and-access-act-2025/the-data-use-and-access-act-2025-what-does-it-mean-for-organisations/)

## Implemented engineering controls

| Area | Current control | Owner |
|---|---|---|
| Data minimisation | FND-018 and FND-019 accept only fixed events and allowlisted trace attributes. Source, payloads, prompts, paths, credentials and authentication material are excluded. | Observability |
| Storage limitation | Operational logs rotate by size and have bounded age and count retention. The defaults are explicit and inspectable. | Observability |
| Local processing | Runtime operates offline and external trace export is disabled. Release-channel data roots do not collide. | Runtime |
| Access boundary | Runtime services own their authoritative state. Desktop does not read service databases or project files directly. | Runtime and service owners |
| IPC security | Named pipes use a current-user DACL, mutual session proof, expiry, replay rejection, bounded messages, deadlines and cancellation. | Runtime IPC |
| Accountability | Deterministic tests and non-authoritative engineering evidence demonstrate the implemented controls. Trust Evidence remains a separate authoritative design. | Security and engineering |

Encryption at rest is not yet claimed for all local service databases. Before
personal data is stored, the service owner must document the risk decision and
implement an appropriate protection and key-management design where required.

## Rate limiting and bounded admission

Message-size bounds, deadlines, cancellation, authentication expiry and replay
rejection are implemented. These controls are not a substitute for admission
rate limiting.

Connection, call and stream admission limits belong to the Runtime IPC security
boundary. They must be added before any remotely reachable endpoint or
untrusted plugin/client class is enabled. The design must:

- use bounded counters or token buckets per authenticated client class;
- cap concurrent connections, calls and streams;
- return a stable retryable refusal without processing the payload;
- avoid client-provided identifiers as unbounded metric or trace dimensions;
- expose only aggregate health and safe diagnostics;
- test burst, sustained-load, cancellation and recovery behaviour; and
- keep limits independent from authentication and domain authorisation.

No current document should describe Opure as rate-limited until that verifier
passes.

## Row and tenant isolation

SQLite does not provide native database-enforced row-level security. Opure must
not label an application predicate as native RLS.

The current foundation instead uses service-owned databases and prevents
Desktop or unrelated services from opening them directly. Before multi-project
or multi-user records are introduced, each owning service must:

- require an authoritative subject and scope at its command/query boundary;
- include the owner or tenant scope in every relevant key and query;
- refuse missing scope rather than falling back to an unrestricted query;
- enforce scope on reads, writes, updates, deletes, outbox and inbox effects;
- test horizontal and vertical access attempts;
- keep administrative bypass explicit, time-bounded and evidenced; and
- perform migration tests that prove existing rows cannot become unscoped.

If a future storage profile uses a database with native RLS, its database
policy must provide defence in depth; service-level authorisation remains
required.

## Required operational and legal decisions before personal-data release

- Identify controller, joint-controller and processor roles.
- Record purposes, data categories, recipients, lawful bases and retention.
- Publish accurate privacy information and data-subject contact routes.
- Implement access, rectification, erasure, restriction, objection and
  portability workflows where applicable.
- Complete a DPIA where the proposed processing is likely to create high risk.
- Define breach detection, assessment and notification procedures.
- Review international transfers before enabling any optional cloud provider.
- Keep consent specific and withdrawable where consent is the lawful basis.
- Review children, employment, biometric and special-category data separately.
- Test backup, export and deletion behaviour against the documented retention
  schedule.

The Product Owner and Data Protection Owner must review this baseline whenever
processing purposes, storage, providers, telemetry or user populations change.
