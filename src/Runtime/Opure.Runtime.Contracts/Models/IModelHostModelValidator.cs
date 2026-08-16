using System.Threading;
using System.Threading.Tasks;

namespace Opure.Runtime.Contracts.Models;

public interface IModelHostModelValidator
{
    Task ValidateAsync(string modelPath, CancellationToken cancellationToken = default);
}
