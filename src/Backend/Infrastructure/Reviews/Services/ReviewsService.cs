using Application.Reviews.Contracts;
using Application.Reviews.Models;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Reviews.Services;

public sealed class ReviewsService : IReviewsService
{
    private readonly LmsDbContext _dbContext;

    public ReviewsService(LmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ReviewAssignmentDto>> GetAssignedReviewsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var assignments = await _dbContext.ReviewAssignments
            .Include(x => x.Task)
                .ThenInclude(t => t.Subject)
            .Include(x => x.Submission)
            .Include(x => x.Reviews)
                .ThenInclude(r => r.CriterionResults)
            .Where(x => x.ReviewerUserId == userId)
            .OrderBy(x => x.AssignedAt)
            .ToListAsync(cancellationToken);

        return assignments.Select(MapToDto).ToList();
    }

    public async Task<ReviewAssignmentDto?> StartReviewAsync(Guid userId, Guid assignmentId, CancellationToken cancellationToken)
    {
        var assignment = await _dbContext.ReviewAssignments
            .Include(x => x.Task)
                .ThenInclude(t => t.Subject)
            .Include(x => x.Submission)
            .Include(x => x.Reviews)
                .ThenInclude(r => r.CriterionResults)
            .FirstOrDefaultAsync(x => x.Id == assignmentId && x.ReviewerUserId == userId, cancellationToken);

        if (assignment is null)
            return null;

        if (assignment.Status == "pending")
        {
            assignment.Status = "opened";
            assignment.OpenedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return MapToDto(assignment);
    }

    public async Task<ReviewAssignmentDto?> SaveDraftAsync(Guid userId, Guid assignmentId, SaveDraftRequest request, CancellationToken cancellationToken)
    {
        var assignment = await _dbContext.ReviewAssignments
            .Include(x => x.Task)
                .ThenInclude(t => t.Subject)
            .Include(x => x.Submission)
            .Include(x => x.Reviews)
                .ThenInclude(r => r.CriterionResults)
            .FirstOrDefaultAsync(x => x.Id == assignmentId && x.ReviewerUserId == userId, cancellationToken);

        if (assignment is null)
            return null;

        if (assignment.Status != "opened" && assignment.Status != "submitted")
            return null;

        var existingReview = assignment.Reviews
            .FirstOrDefault(r => r.Source == "peer" && !r.IsFinal);

        if (existingReview is null)
        {
            existingReview = new Review
            {
                Id = Guid.NewGuid(),
                AssignmentId = assignmentId,
                ReviewerUserId = userId,
                Source = "peer",
                OverallScore = request.OverallScore,
                OverallComment = request.OverallComment,
                SubmittedAt = DateTimeOffset.UtcNow,
                IsFinal = false,
                IsRejected = false,
                ReplacedByTeacher = false
            };
            _dbContext.Reviews.Add(existingReview);
        }
        else
        {
            existingReview.OverallScore = request.OverallScore;
            existingReview.OverallComment = request.OverallComment;
            existingReview.SubmittedAt = DateTimeOffset.UtcNow;
        }

        if (request.CriterionResults != null)
        {
            foreach (var criterionResultDto in request.CriterionResults)
            {
                var existingCriterionResult = existingReview.CriterionResults
                    .FirstOrDefault(cr => cr.CriterionId == criterionResultDto.CriterionId);

                if (existingCriterionResult is null)
                {
                    existingCriterionResult = new CriterionResult
                    {
                        Id = Guid.NewGuid(),
                        ReviewId = existingReview.Id,
                        CriterionId = criterionResultDto.CriterionId,
                        Value = criterionResultDto.Value,
                        Comment = criterionResultDto.Comment,
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                    _dbContext.CriterionResults.Add(existingCriterionResult);
                }
                else
                {
                    existingCriterionResult.Value = criterionResultDto.Value;
                    existingCriterionResult.Comment = criterionResultDto.Comment;
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(assignment);
    }

    public async Task<ReviewAssignmentDto?> SubmitReviewAsync(Guid userId, Guid assignmentId, SubmitReviewRequest request, CancellationToken cancellationToken)
    {
        var assignment = await _dbContext.ReviewAssignments
            .Include(x => x.Task)
                .ThenInclude(t => t.Subject)
            .Include(x => x.Submission)
            .Include(x => x.Reviews)
                .ThenInclude(r => r.CriterionResults)
            .FirstOrDefaultAsync(x => x.Id == assignmentId && x.ReviewerUserId == userId, cancellationToken);

        if (assignment is null)
            return null;

        if (assignment.Status != "opened" && assignment.Status != "submitted")
            return null;

        var existingReview = assignment.Reviews
            .FirstOrDefault(r => r.Source == "peer" && !r.IsFinal);

        if (existingReview is null)
        {
            existingReview = new Review
            {
                Id = Guid.NewGuid(),
                AssignmentId = assignmentId,
                ReviewerUserId = userId,
                Source = "peer",
                OverallScore = request.OverallScore,
                OverallComment = request.OverallComment,
                SubmittedAt = DateTimeOffset.UtcNow,
                IsFinal = true,
                IsRejected = false,
                ReplacedByTeacher = false
            };
            _dbContext.Reviews.Add(existingReview);
        }
        else
        {
            existingReview.OverallScore = request.OverallScore;
            existingReview.OverallComment = request.OverallComment;
            existingReview.SubmittedAt = DateTimeOffset.UtcNow;
            existingReview.IsFinal = true;
        }

        if (request.CriterionResults != null)
        {
            foreach (var criterionResultDto in request.CriterionResults)
            {
                var existingCriterionResult = existingReview.CriterionResults
                    .FirstOrDefault(cr => cr.CriterionId == criterionResultDto.CriterionId);

                if (existingCriterionResult is null)
                {
                    existingCriterionResult = new CriterionResult
                    {
                        Id = Guid.NewGuid(),
                        ReviewId = existingReview.Id,
                        CriterionId = criterionResultDto.CriterionId,
                        Value = criterionResultDto.Value,
                        Comment = criterionResultDto.Comment,
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                    _dbContext.CriterionResults.Add(existingCriterionResult);
                }
                else
                {
                    existingCriterionResult.Value = criterionResultDto.Value;
                    existingCriterionResult.Comment = criterionResultDto.Comment;
                }
            }
        }

        assignment.Status = "submitted";
        assignment.SubmittedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(assignment);
    }

    private static ReviewAssignmentDto MapToDto(ReviewAssignment assignment)
    {
        var latestReview = assignment.Reviews
            .OrderByDescending(r => r.SubmittedAt)
            .FirstOrDefault();

        return new ReviewAssignmentDto
        {
            Id = assignment.Id,
            TaskId = assignment.TaskId,
            TaskTitle = assignment.Task.Subject.Title,
            SubmissionId = assignment.SubmissionId,
            ReviewTargetType = assignment.ReviewTargetType,
            Status = assignment.Status,
            AssignedAt = assignment.AssignedAt,
            StartsAt = assignment.StartsAt,
            DueAt = assignment.DueAt,
            OpenedAt = assignment.OpenedAt,
            SubmittedAt = assignment.SubmittedAt,
            LatestReview = latestReview != null ? MapReviewToDto(latestReview) : null
        };
    }

    private static ReviewDto MapReviewToDto(Review review)
    {
        return new ReviewDto
        {
            Id = review.Id,
            OverallScore = review.OverallScore,
            OverallComment = review.OverallComment,
            Source = review.Source,
            SubmittedAt = review.SubmittedAt,
            IsFinal = review.IsFinal
        };
    }
}
