namespace Opure.Configuration.Contracts;

public enum SettingValueKind
{
    Boolean = 0,
    Integer = 1,
    Decimal = 2,
    String = 3,
    Duration = 4,
    ByteSize = 5,
    UtcInstant = 6,
    Enumeration = 7,
    Uri = 8,
    LogicalPathReference = 9,
    OpaqueServiceReference = 10,
    VaultReference = 11,
    OrderedList = 12,
    UnorderedSet = 13,
    StringMap = 14,
    TypedObject = 15,
    DiscriminatedUnion = 16,
    BoundedRuleList = 17
}

public enum SettingScope
{
    Product = 0,
    Channel = 1,
    Machine = 2,
    User = 3,
    Project = 4,
    WorkspaceSession = 5,
    Workflow = 6,
    Operation = 7,
    Plugin = 8,
    McpServer = 9,
    Provider = 10,
    LocalModel = 11,
    Tool = 12,
    Test = 13
}

public enum SettingSource
{
    ProductDefault = 0,
    ReleaseChannelDefault = 1,
    MachinePreference = 2,
    UserBaseProfile = 3,
    NamedUserProfile = 4,
    ProjectSharedSettings = 5,
    ProjectLocalProfile = 6,
    WorkspaceSession = 7,
    SessionOverride = 8,
    OperationOverride = 9
}

public enum SettingMergeStrategy
{
    Replace = 0,
    ReplaceIfSet = 1,
    FirstExplicit = 2,
    Append = 3,
    Prepend = 4,
    OrderedUniqueAppend = 5,
    SetUnion = 6,
    SetIntersection = 7,
    MapMergeByKey = 8,
    MapReplace = 9,
    RuleListConcatenation = 10,
    Minimum = 11,
    Maximum = 12,
    CustomTrustedReducer = 13
}

public enum SettingNullSemantics
{
    RejectNull = 0,
    ExplicitNull = 1,
    ResetToDefault = 2,
    RemoveInheritedEntry = 3,
    EmptyValue = 4
}

public enum SettingSensitivity
{
    Public = 0,
    ProductInternal = 1,
    ProjectInternal = 2,
    Personal = 3,
    Confidential = 4,
    SecuritySensitive = 5,
    SecretReference = 6,
    ProhibitedSecretValue = 7
}

public enum SettingSecretPolicy
{
    NoSecret = 0,
    VaultReferenceAllowed = 1,
    VaultReferenceRequired = 2,
    SecretDerivedBooleanOnly = 3,
    Prohibited = 4
}

public enum SettingRuntimeApplication
{
    Immediate = 0,
    NextOperation = 1,
    NextWorkflow = 2,
    NextServiceStart = 3,
    NextRuntimeStart = 4,
    NextApplicationStart = 5
}

public enum SettingRestartImpact
{
    None = 0,
    ReconfigureService = 1,
    RestartService = 2,
    RestartRuntime = 3,
    RestartDesktop = 4,
    RestartApplication = 5,
    WindowsSignOutOrRestart = 6,
    MigrationRequired = 7,
    UnsupportedWhileActive = 8
}

public enum SettingFailureClass
{
    Informational = 0,
    Operational = 1,
    AvailabilityCritical = 2,
    SecurityCritical = 3,
    DataGovernanceCritical = 4
}
