namespace Opure.TrustEvidence.Contracts;

public static class ApproverIdentity
{
    public static string Agent(string agentName) => $"Agent:{agentName}";
    public static string User(string userName) => $"User:{userName}";
}
