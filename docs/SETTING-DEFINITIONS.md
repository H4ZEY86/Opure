# Foundation Setting Definitions

This file is generated from the authoritative packaged Setting Definition catalogue.

Catalogue revision: 1
Catalogue SHA-256: `1426227b555a5ba138131ddf35f16c61b5212dfa55990afd2c827871e28d5cb7`

| Setting | Revision | Type | Default | Scopes | Sources | Merge | Sensitivity | Application | Restart |
| --- | ---: | --- | --- | --- | --- | --- | --- | --- | --- |
| `desktop.appearance.theme` | 1 | Enumeration | `"system"` | User, WorkspaceSession | ProductDefault, UserBaseProfile, NamedUserProfile, WorkspaceSession, SessionOverride | Replace | Public | Immediate | None |
| `logging.level.default` | 1 | Enumeration | `"information"` | Machine, User, Project, WorkspaceSession | ProductDefault, ReleaseChannelDefault, MachinePreference, UserBaseProfile, NamedUserProfile, ProjectSharedSettings, ProjectLocalProfile, WorkspaceSession, SessionOverride | Replace | ProductInternal | Immediate | ReconfigureService |
| `provider.credential.vault-reference` | 1 | VaultReference | `required` | User, Project, Provider | UserBaseProfile, NamedUserProfile, ProjectLocalProfile | Replace | SecretReference | NextOperation | None |
| `runtime.performance.default-mode` | 1 | Enumeration | `"balanced"` | User, Project, WorkspaceSession, Operation | ProductDefault, UserBaseProfile, NamedUserProfile, ProjectSharedSettings, ProjectLocalProfile, WorkspaceSession, SessionOverride, OperationOverride | Replace | ProductInternal | NextOperation | None |
| `security.integrity-validation.enabled` | 1 | Boolean | `true` | Product | ProductDefault | Replace | SecuritySensitive | NextApplicationStart | RestartApplication |
