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
