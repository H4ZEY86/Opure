using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Opure.Runtime.Contracts.Models;

public interface IModelHostJsonContractSerializer
{
    byte[] SerializeModelRequest(ModelRequest request);
    ModelHostResponse? DeserializeResponse(byte[] responseBytes);
    Task<ModelHostResponse?> DeserializeResponseStreamAsync(Stream stream, CancellationToken cancellationToken = default);
}
