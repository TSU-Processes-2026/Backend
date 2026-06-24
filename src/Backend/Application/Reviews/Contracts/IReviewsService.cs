using Application.Reviews.Models;

namespace Application.Reviews.Contracts;

public interface IReviewsService
{
    Task<IReadOnlyList<ReviewAssignmentDto>> GetAssignedReviewsAsync(Guid userId, CancellationToken cancellationToken);
    Task<ReviewAssignmentDto?> StartReviewAsync(Guid userId, Guid assignmentId, CancellationToken cancellationToken);
    Task<ReviewAssignmentDto?> SaveDraftAsync(Guid userId, Guid assignmentId, SaveDraftRequest request, CancellationToken cancellationToken);
    Task<ReviewAssignmentDto?> SubmitReviewAsync(Guid userId, Guid assignmentId, SubmitReviewRequest request, CancellationToken cancellationToken);
    Task<SubmissionReviewsDto?> GetSubmissionReviewsAsync(Guid submissionId, CancellationToken cancellationToken);
    Task<FinalGradeDto?> GetFinalGradeAsync(Guid submissionId, CancellationToken cancellationToken);
    Task<ReviewDto?> CreateTeacherReviewAsync(Guid teacherId, Guid submissionId, TeacherReviewRequest request, CancellationToken cancellationToken);
}
