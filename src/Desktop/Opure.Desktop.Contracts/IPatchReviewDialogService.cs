using System.Threading;
using System.Threading.Tasks;

namespace Opure.Desktop.Contracts;

public interface IPatchReviewDialogService
{
    Task<PatchReviewResult?> ShowReviewAsync(PatchReviewViewModel viewModel, CancellationToken cancellationToken);
}
