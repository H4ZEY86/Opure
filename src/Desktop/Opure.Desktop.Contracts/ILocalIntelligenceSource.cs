using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Opure.Runtime.Contracts.Models;

namespace Opure.Desktop.Contracts;

public interface ILocalIntelligenceSource
{
    IAsyncEnumerable<StreamPayload> GenerateStreamAsync(string prompt, CancellationToken cancellationToken);
}
