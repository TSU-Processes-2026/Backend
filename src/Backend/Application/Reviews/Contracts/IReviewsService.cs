using Application.Reviews.Models;

namespace Application.Reviews.Contracts;

public interface IReviewsService
{
    Task<IReadOnlyList<ReviewAssignmentDto>> GetAssignedReviewsAsync(Guid userId, CancellationToken cancellationToken);
    Task<ReviewOperationResult<ReviewAssignmentDto>> StartReviewAsync(Guid userId, Guid assignmentId, CancellationToken cancellationToken);
    Task<ReviewOperationResult<ReviewAssignmentDto>> SaveDraftAsync(Guid userId, Guid assignmentId, SaveDraftRequest request, CancellationToken cancellationToken);
    Task<ReviewOperationResult<ReviewAssignmentDto>> SubmitReviewAsync(Guid userId, Guid assignmentId, SubmitReviewRequest request, CancellationToken cancellationToken);
    Task<SubmissionReviewsDto?> GetSubmissionReviewsAsync(Guid submissionId, CancellationToken cancellationToken);
    Task<FinalGradeDto?> GetFinalGradeAsync(Guid submissionId, CancellationToken cancellationToken);
    Task<ReviewDto?> CreateTeacherReviewAsync(Guid teacherId, Guid submissionId, TeacherReviewRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReviewAssignmentDto>> GetTeamReviewsAsync(Guid teamId, CancellationToken ct);
    Task<ReviewDto?> RejectReviewAsync(Guid teacherId, Guid reviewId, CancellationToken ct);
    Task<FinalGradeResponse?> OverrideFinalGradeAsync(Guid teacherId, Guid submissionId, decimal finalScore, string? comment, CancellationToken ct);
}
