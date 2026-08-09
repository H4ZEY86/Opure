namespace Opure.Recovery.Contracts;

public enum RecoveryPointVerificationState
{
    Unverified,
    HashVerified,
    StructurallyValidated,
    Failed
}
