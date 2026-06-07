using Application.Reviews.Models;

namespace Application.Reviews.Contracts;

public interface IReviewsService
{
    Task<IReadOnlyList<ReviewAssignmentDto>> GetAssignedReviewsAsync(Guid userId, CancellationToken cancellationToken);
}
