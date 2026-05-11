using Application.Criteria.Models;

namespace Application.Criteria.Contracts;

public interface ICriteriaService
{
    Task<CriterionUpdateResult> UpdateAsync(Guid currentUserId, Guid criterionId, UpdateCriterionRequest request, CancellationToken cancellationToken);
    Task<CriterionDeleteResult> DeleteAsync(Guid currentUserId, Guid criterionId, CancellationToken cancellationToken);
}
