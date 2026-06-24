using Application.Reviews.Contracts;
using Application.Reviews.Models;
using Application.Teams.Contracts;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Reviews.Services;

public sealed class ReviewsService : IReviewsService
{
    private readonly LmsDbContext _dbContext;
    private readonly ITeamsService _teamsService;

    public ReviewsService(LmsDbContext dbContext, ITeamsService teamsService)
    {
        _dbContext = dbContext;
        _teamsService = teamsService;
    }

    public async Task<IReadOnlyList<ReviewAssignmentDto>> GetAssignedReviewsAsync(Guid userId, CancellationToken cancellationToken)
    {
        await ExpireOverdueAssignments(cancellationToken);

        var assignments = await _dbContext.ReviewAssignments
            .Include(x => x.Task)
                .ThenInclude(t => t.Subject)
            .Include(x => x.Submission)
                .ThenInclude(s => s.answers)
            .Include(x => x.Reviews)
                .ThenInclude(r => r.CriterionResults)
            .Where(x => x.ReviewerUserId == userId)
            .OrderBy(x => x.AssignedAt)
            .ToListAsync(cancellationToken);

        return assignments.Select(MapToDto).ToList();
    }

    public async Task<ReviewOperationResult<ReviewAssignmentDto>> StartReviewAsync(Guid userId, Guid assignmentId, CancellationToken cancellationToken)
    {
        await ExpireOverdueAssignments(cancellationToken);

        var assignment = await _dbContext.ReviewAssignments
            .Include(x => x.Task)
                .ThenInclude(t => t.Subject)
            .Include(x => x.Submission)
            .Include(x => x.Reviews)
                .ThenInclude(r => r.CriterionResults)
            .FirstOrDefaultAsync(x => x.Id == assignmentId, cancellationToken);

        if (assignment is null)
            return ReviewOperationResult<ReviewAssignmentDto>.NotFound();

        if (!await IsAuthorizedForAssignment(userId, assignment, cancellationToken))
            return ReviewOperationResult<ReviewAssignmentDto>.Forbidden();

        if (assignment.Status == "expired")
            return ReviewOperationResult<ReviewAssignmentDto>.Expired();

        if (assignment.Status != "pending")
            return ReviewOperationResult<ReviewAssignmentDto>.InvalidState("Review already started or completed");

        assignment.Status = "opened";
        assignment.OpenedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ReviewOperationResult<ReviewAssignmentDto>.Success(MapToDto(assignment));
    }

    public async Task<ReviewOperationResult<ReviewAssignmentDto>> SaveDraftAsync(Guid userId, Guid assignmentId, SaveDraftRequest request, CancellationToken cancellationToken)
    {
        await ExpireOverdueAssignments(cancellationToken);

        var assignment = await _dbContext.ReviewAssignments
            .Include(x => x.Task)
                .ThenInclude(t => t.Subject)
            .Include(x => x.Submission)
            .Include(x => x.Reviews)
                .ThenInclude(r => r.CriterionResults)
            .FirstOrDefaultAsync(x => x.Id == assignmentId && x.ReviewerUserId == userId, cancellationToken);

        if (assignment is null)
            return ReviewOperationResult<ReviewAssignmentDto>.NotFound();

        if (assignment.Status == "expired")
            return ReviewOperationResult<ReviewAssignmentDto>.Expired();

        if (assignment.Status == "cancelled")
            return ReviewOperationResult<ReviewAssignmentDto>.Cancelled();

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
                        AssessmentType = "PEER",
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

        return ReviewOperationResult<ReviewAssignmentDto>.Success(MapToDto(assignment));
    }

    public async Task<ReviewOperationResult<ReviewAssignmentDto>> SubmitReviewAsync(Guid userId, Guid assignmentId, SubmitReviewRequest request, CancellationToken cancellationToken)
    {
        await ExpireOverdueAssignments(cancellationToken);

        var assignment = await _dbContext.ReviewAssignments
            .Include(x => x.Task)
                .ThenInclude(t => t.Subject)
            .Include(x => x.Submission)
            .Include(x => x.Reviews)
                .ThenInclude(r => r.CriterionResults)
            .FirstOrDefaultAsync(x => x.Id == assignmentId, cancellationToken);

        if (assignment is null)
            return ReviewOperationResult<ReviewAssignmentDto>.NotFound();

        if (!await IsAuthorizedForAssignment(userId, assignment, cancellationToken))
            return ReviewOperationResult<ReviewAssignmentDto>.Forbidden();

        if (assignment.Status == "expired")
            return ReviewOperationResult<ReviewAssignmentDto>.Expired();

        if (assignment.Status == "cancelled")
            return ReviewOperationResult<ReviewAssignmentDto>.Cancelled();

        if (assignment.Status != "opened")
            return ReviewOperationResult<ReviewAssignmentDto>.InvalidState("Review must be opened first");

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
                        AssessmentType = "PEER",
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

        return ReviewOperationResult<ReviewAssignmentDto>.Success(MapToDto(assignment));
    }

    public async Task<SubmissionReviewsDto?> GetSubmissionReviewsAsync(Guid submissionId, CancellationToken cancellationToken)
    {
        var reviews = await _dbContext.Reviews
            .Include(r => r.CriterionResults)
            .Include(r => r.Assignment)
                .ThenInclude(a => a.Task)
                    .ThenInclude(t => t.Subject)
            .Where(r => r.Assignment.SubmissionId == submissionId)
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync(cancellationToken);

        if (reviews.Count == 0)
            return null;

        var peerReviews = reviews
            .Where(r => r.Source == "peer")
            .Select(MapReviewToDto)
            .ToList();

        var teacherReview = reviews
            .FirstOrDefault(r => r.Source == "teacher");

        return new SubmissionReviewsDto
        {
            SubmissionId = submissionId,
            PeerReviews = peerReviews,
            TeacherReview = teacherReview != null ? MapReviewToDto(teacherReview) : null
        };
    }

    public async Task<FinalGradeDto?> GetFinalGradeAsync(Guid submissionId, CancellationToken cancellationToken)
    {
        var reviews = await _dbContext.Reviews
            .Include(r => r.Assignment)
            .Where(r => r.Assignment.SubmissionId == submissionId && r.IsFinal)
            .ToListAsync(cancellationToken);

        if (reviews.Count == 0)
            return null;

        var teacherReview = reviews.FirstOrDefault(r => r.Source == "teacher");
        var peerReviews = reviews.Where(r => r.Source == "peer").ToList();

        decimal? finalScore;
        string finalSource;

        if (teacherReview != null)
        {
            finalScore = teacherReview.OverallScore;
            finalSource = "teacher";
        }
        else if (peerReviews.Count > 0)
        {
            finalScore = peerReviews.Average(r => r.OverallScore);
            finalSource = "peer";
        }
        else
        {
            return null;
        }

        return new FinalGradeDto
        {
            SubmissionId = submissionId,
            FinalScore = finalScore,
            FinalSource = finalSource,
            CalculatedAt = DateTimeOffset.UtcNow
        };
    }

    public async Task<ReviewDto?> CreateTeacherReviewAsync(Guid teacherId, Guid submissionId, TeacherReviewRequest request, CancellationToken cancellationToken)
    {
        var assignment = await _dbContext.ReviewAssignments
            .FirstOrDefaultAsync(a => a.SubmissionId == submissionId, cancellationToken);

        if (assignment is null)
            return null;

        var existingTeacherReview = await _dbContext.Reviews
            .Include(r => r.CriterionResults)
            .FirstOrDefaultAsync(r => r.Assignment.SubmissionId == submissionId && r.Source == "teacher", cancellationToken);

        if (existingTeacherReview != null)
        {
            existingTeacherReview.OverallScore = request.OverallScore;
            existingTeacherReview.OverallComment = request.OverallComment;
            existingTeacherReview.SubmittedAt = DateTimeOffset.UtcNow;
            existingTeacherReview.IsFinal = true;

            if (request.CriterionResults != null)
            {
                foreach (var criterionResultDto in request.CriterionResults)
                {
                    var existingCriterionResult = existingTeacherReview.CriterionResults
                        .FirstOrDefault(cr => cr.CriterionId == criterionResultDto.CriterionId);

                    if (existingCriterionResult is null)
                    {
                        existingCriterionResult = new CriterionResult
                        {
                            Id = Guid.NewGuid(),
                            ReviewId = existingTeacherReview.Id,
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
        }
        else
        {
            var newReview = new Review
            {
                Id = Guid.NewGuid(),
                AssignmentId = assignment.Id,
                ReviewerUserId = teacherId,
                Source = "teacher",
                OverallScore = request.OverallScore,
                OverallComment = request.OverallComment,
                SubmittedAt = DateTimeOffset.UtcNow,
                IsFinal = true,
                IsRejected = false,
                ReplacedByTeacher = false
            };
            _dbContext.Reviews.Add(newReview);

            if (request.CriterionResults != null)
            {
                foreach (var criterionResultDto in request.CriterionResults)
                {
                    var criterionResult = new CriterionResult
                    {
                        Id = Guid.NewGuid(),
                        ReviewId = newReview.Id,
                        CriterionId = criterionResultDto.CriterionId,
                        Value = criterionResultDto.Value,
                        Comment = criterionResultDto.Comment,
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                    _dbContext.CriterionResults.Add(criterionResult);
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapReviewToDto(newReview);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapReviewToDto(existingTeacherReview);
    }

    public async Task<IReadOnlyList<ReviewAssignmentDto>> GetTeamReviewsAsync(Guid teamId, CancellationToken ct)
    {
        var assignments = await _dbContext.ReviewAssignments
            .Include(x => x.Task)
                .ThenInclude(t => t.Subject)
            .Include(x => x.Submission)
            .Include(x => x.Reviews)
                .ThenInclude(r => r.CriterionResults)
            .Where(x => x.ReviewerTeamId == teamId)
            .OrderByDescending(x => x.AssignedAt)
            .ToListAsync(ct);

        return assignments.Select(MapToDto).ToList();
    }

    public async Task<ReviewDto?> RejectReviewAsync(Guid teacherId, Guid reviewId, CancellationToken ct)
    {
        var review = await _dbContext.Reviews
            .Include(x => x.Assignment)
                .ThenInclude(x => x.Task)
            .SingleOrDefaultAsync(x => x.Id == reviewId, ct);

        if (review is null) return null;

        var subjectId = review.Assignment.Task.SubjectId;
        var isTeacherOrAdmin = await _dbContext.SubjectParticipants
            .AnyAsync(x => x.SubjectId == subjectId && x.UserId == teacherId
                && (x.Role == "Teacher" || x.Role == "Admin"), ct);
        if (!isTeacherOrAdmin) return null;

        if (review.Source == "teacher") return null;

        review.IsRejected = true;
        review.IsFinal = false;
        await _dbContext.SaveChangesAsync(ct);

        return MapReviewToDto(review);
    }

    public async Task<FinalGradeResponse?> OverrideFinalGradeAsync(Guid teacherId, Guid submissionId,
        decimal finalScore, string? comment, CancellationToken ct)
    {
        var submission = await _dbContext.Submissions
            .Include(x => x.post)
            .SingleOrDefaultAsync(x => x.id == submissionId, ct);

        if (submission is null) return null;

        var subjectId = submission.post.SubjectId;
        var isTeacherOrAdmin = await _dbContext.SubjectParticipants
            .AnyAsync(x => x.SubjectId == subjectId && x.UserId == teacherId
                && (x.Role == "Teacher" || x.Role == "Admin"), ct);
        if (!isTeacherOrAdmin) return null;

        submission.FinalScore = finalScore;
        submission.FinalSource = "teacher";

        var finalGrade = await _dbContext.FinalGrades
            .SingleOrDefaultAsync(x => x.SubmissionId == submissionId, ct);

        if (finalGrade is null)
        {
            finalGrade = new FinalGrade
            {
                Id = Guid.NewGuid(),
                SubmissionId = submissionId,
                StudentId = submission.authorId,
                FinalScore = finalScore,
                FinalSource = "teacher",
                TeacherOverrideComment = comment,
                CalculatedAt = DateTimeOffset.UtcNow
            };
            _dbContext.FinalGrades.Add(finalGrade);
        }
        else
        {
            finalGrade.FinalScore = finalScore;
            finalGrade.FinalSource = "teacher";
            finalGrade.TeacherOverrideComment = comment;
            finalGrade.CalculatedAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync(ct);

        return new FinalGradeResponse
        {
            Id = finalGrade.Id,
            SubmissionId = finalGrade.SubmissionId,
            FinalScore = finalGrade.FinalScore,
            FinalSource = finalGrade.FinalSource,
            TeacherOverrideComment = finalGrade.TeacherOverrideComment,
            CalculatedAt = finalGrade.CalculatedAt
        };
    }

    private async Task<bool> IsAuthorizedForAssignment(Guid userId, ReviewAssignment assignment, CancellationToken ct)
    {
        if (assignment.ReviewerTeamId is not null)
        {
            var task = await _dbContext.Posts
                .SingleOrDefaultAsync(x => x.Id == assignment.TaskId, ct);

            var policy = task?.TeamReviewPolicy ?? "all_members";

            return await _teamsService.CanMemberReviewAsync(userId, assignment.ReviewerTeamId.Value, policy, ct);
        }

        return assignment.ReviewerUserId == userId;
    }

    private async Task ExpireOverdueAssignments(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var overdue = await _dbContext.ReviewAssignments
            .Where(x => x.Status != "submitted" && x.Status != "expired" && x.Status != "cancelled" && x.DueAt < now)
            .ToListAsync(ct);
        foreach (var assignment in overdue)
            assignment.Status = "expired";
        if (overdue.Count > 0)
            await _dbContext.SaveChangesAsync(ct);
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
