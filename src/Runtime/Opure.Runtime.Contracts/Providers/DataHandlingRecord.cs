using System;

namespace Opure.Runtime.Contracts.Providers;

/// <summary>
/// A read-only record capturing the provider's privacy and data usage terms.
/// </summary>
public sealed record DataHandlingRecord(
    string ProviderId,
    Uri TermsUrl,
    TimeSpan? RetentionDuration,
    bool UsesDataForTraining);
