namespace Opure.Configuration.Contracts;

/// <summary>
/// Policy source authorities, ordered from broadest (most authoritative) to narrowest.
/// A lower source may make a rule stricter but may not broaden a higher rule.
/// </summary>
public enum PolicySourceAuthority
{
    ProductInvariant = 0,
    ReleaseChannel = 1,
    EnterpriseMachine = 2,
    EnterpriseUser = 3,
    ProjectGovernance = 4,
    Workflow = 5,
    OperationCapability = 6
}

/// <summary>
/// Defines the type of constraint a Policy Definition enforces.
/// </summary>
public enum PolicyDecisionModel
{
    ForceValue = 0,
    AllowValues = 1,
    DenyValues = 2,
    RequireBooleanTrue = 3,
    RequireBooleanFalse = 4,
    Minimum = 5,
    Maximum = 6,
    RequireCapability = 7,
    DenyCapability = 8,
    RequireReviewMode = 9,
    MaximumDataClass = 10,
    AllowedProviderProfiles = 11,
    AllowedRegions = 12,
    AllowedPaths = 13,
    DeniedPaths = 14,
    MaximumCost = 15,
    MaximumRetention = 16,
    MinimumRetention = 17,
    RequireLocal = 18,
    RequireOffline = 19,
    LockSetting = 20,
    CustomTrustedConstraint = 21
}

/// <summary>
/// Describes the typed input that a policy evaluator accepts.
/// AI output cannot become policy input without deterministic classification.
/// </summary>
public enum PolicyInputKind
{
    BooleanFlag = 0,
    EnumerationChoice = 1,
    NumericBound = 2,
    DurationBound = 3,
    ByteSizeBound = 4,
    PathSet = 5,
    IdentifierSet = 6,
    DataClassification = 7,
    CostBound = 8,
    RegionSet = 9,
    CapabilityToken = 10,
    ReviewModeToken = 11,
    SettingValueReference = 12,
    None = 13
}

/// <summary>
/// Possible outcomes of policy evaluation.
/// </summary>
public enum PolicyResultKind
{
    Allow = 0,
    Deny = 1,
    Constrain = 2,
    RequireApproval = 3
}

/// <summary>
/// How multiple policy instances from different sources combine.
/// </summary>
public enum PolicyCombination
{
    MostRestrictive = 0,
    Intersection = 1,
    UnionOfDenials = 2,
    HighestAuthorityWins = 3,
    UnionOfRequirements = 4
}

/// <summary>
/// What the policy definition protects.
/// </summary>
public enum PolicyTarget
{
    Setting = 0,
    Capability = 1,
    GeneralConstraint = 2
}
