using Infrastructure.Persistence.Entities;

namespace Infrastructure.Reviews.Contracts;

public interface IReviewDistributionService
{
    Task<IReadOnlyList<ReviewAssignment>> GenerateAllToAllAsync(Guid taskId, CancellationToken ct);
    Task<IReadOnlyList<ReviewAssignment>> GeneratePairsAsync(Guid taskId, string pairingStrategy, CancellationToken ct);
}
