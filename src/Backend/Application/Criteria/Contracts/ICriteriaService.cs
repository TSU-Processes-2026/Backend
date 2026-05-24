using Application.Criteria.Models;

namespace Application.Criteria.Contracts;

public interface ICriteriaService
{
    Task<CriterionListResult> GetTaskCriteriaAsync(Guid currentUserId, Guid taskId, CancellationToken cancellationToken);
    Task<CriterionUpdateResult> CreateAsync(Guid currentUserId, Guid taskId, CreateCriterionRequest request, CancellationToken cancellationToken);
    Task<CriterionUpdateResult> UpdateAsync(Guid currentUserId, Guid criterionId, UpdateCriterionRequest request, CancellationToken cancellationToken);
    Task<CriterionDeleteResult> DeleteAsync(Guid currentUserId, Guid criterionId, CancellationToken cancellationToken);
    Task<CriterionResultsUpdateResult> UpsertInstructorResultsAsync(Guid currentUserId, Guid submissionId, UpsertCriterionResultsRequest request, CancellationToken cancellationToken);
}
