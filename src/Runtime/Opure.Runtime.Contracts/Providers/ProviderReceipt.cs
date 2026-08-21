using System;

namespace Opure.Runtime.Contracts.Providers;

public sealed record ProviderReceipt(
    string ProviderId,
    Uri Endpoint,
    long BytesSent,
    long BytesReceived,
    DateTimeOffset Timestamp,
    int StatusCode);
