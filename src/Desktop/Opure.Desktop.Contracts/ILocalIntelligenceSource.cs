using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Opure.Desktop.Contracts;

public interface ILocalIntelligenceSource
{
    IAsyncEnumerable<string> GenerateStreamAsync(string prompt, CancellationToken cancellationToken);
}
