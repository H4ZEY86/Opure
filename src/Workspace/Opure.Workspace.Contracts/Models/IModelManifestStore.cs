using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Opure.Workspace.Contracts.Models;

public interface IModelManifestStore
{
    Task<ModelHostManifest?> GetManifestAsync(string modelPath, CancellationToken cancellationToken = default);
    Task<ModelHostManifest?> GetManifestForHashAsync(byte[] sha256Hash, CancellationToken cancellationToken = default);
    Task StoreManifestAsync(ModelHostManifest manifest, CancellationToken cancellationToken = default);
    Task RecordValidationAsync(string modelPath, byte[] computedHash, CancellationToken cancellationToken = default);
    Task RecordImportAsync(ModelHostManifest manifest, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ModelHostManifest>> ListManifestsAsync(CancellationToken cancellationToken = default);
}
