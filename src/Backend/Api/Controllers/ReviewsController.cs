using Api.Authentication;
using Application.Reviews.Contracts;
using Application.Reviews.Models;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Infrastructure.Reviews.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api")]
public sealed class ReviewsController : ControllerBase
{
    private readonly IReviewsService _reviewsService;
    private readonly IReviewDistributionService _distributionService;
    private readonly LmsDbContext _dbContext;

    public ReviewsController(IReviewsService reviewsService, IReviewDistributionService distributionService, LmsDbContext dbContext)
    {
        _reviewsService = reviewsService;
        _distributionService = distributionService;
        _dbContext = dbContext;
    }

    [Authorize]
    [HttpGet("reviews/me")]
    [ProducesResponseType(typeof(IReadOnlyList<ReviewAssignmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAssignedReviews(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(CreateUnauthorized());

        var reviews = await _reviewsService.GetAssignedReviewsAsync(userId.Value, cancellationToken);
        return Ok(reviews);
    }

    [Authorize]
    [HttpGet("reviews/{id:guid}")]
    [ProducesResponseType(typeof(ReviewAssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReview([FromRoute] Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(CreateUnauthorized());

        var assignment = await _dbContext.ReviewAssignments
            .Include(x => x.Task)
                .ThenInclude(t => t.Subject)
            .Include(x => x.Submission)
                .ThenInclude(s => s.answers)
            .Include(x => x.Reviews)
                .ThenInclude(r => r.CriterionResults)
            .SingleOrDefaultAsync(x => x.Id == id, ct);

        if (assignment is null)
            return NotFound(CreateNotFound());

        var isTeacherOrAdmin = await _dbContext.SubjectParticipants
            .AnyAsync(x => x.SubjectId == assignment.Task.SubjectId && x.UserId == userId.Value
                && (x.Role == "Teacher" || x.Role == "Admin"), ct);

        if (assignment.ReviewerUserId != userId.Value && !isTeacherOrAdmin)
            return Forbid();

        return Ok(MapToDto(assignment));
    }

    [Authorize]
    [HttpGet("submissions/{id:guid}/reviews")]
    [ProducesResponseType(typeof(SubmissionReviewsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubmissionReviews([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(CreateUnauthorized());

        var reviews = await _reviewsService.GetSubmissionReviewsAsync(id, cancellationToken);
        if (reviews is null)
            return NotFound(CreateNotFound());

        return Ok(reviews);
    }

    [Authorize]
    [HttpGet("submissions/{id:guid}/final-grade")]
    [ProducesResponseType(typeof(FinalGradeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFinalGrade([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(CreateUnauthorized());

        var finalGrade = await _reviewsService.GetFinalGradeAsync(id, cancellationToken);
        if (finalGrade is null)
            return NotFound(CreateNotFound());

        return Ok(finalGrade);
    }

    [Authorize]
    [HttpPost("submissions/{id:guid}/teacher-review")]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateTeacherReview([FromRoute] Guid id, [FromBody] TeacherReviewRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(CreateUnauthorized());

        var review = await _reviewsService.CreateTeacherReviewAsync(userId.Value, id, request, cancellationToken);
        if (review is null)
            return NotFound(CreateNotFound());

        return Ok(review);
    }

    [Authorize]
    [HttpPost("reviews/{id:guid}/start")]
    [ProducesResponseType(typeof(ReviewAssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartReview([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(CreateUnauthorized());

        var result = await _reviewsService.StartReviewAsync(userId.Value, id, cancellationToken);

        return result.Status switch
        {
            ReviewOperationStatus.Success => Ok(result.Data),
            ReviewOperationStatus.NotFound => NotFound(CreateNotFound()),
            ReviewOperationStatus.Forbidden => Forbid(),
            ReviewOperationStatus.Expired => StatusCode(StatusCodes.Status403Forbidden, CreateError("Review deadline has passed")),
            ReviewOperationStatus.InvalidState => StatusCode(StatusCodes.Status409Conflict, CreateError(result.ErrorMessage ?? "Invalid state")),
            _ => throw new InvalidOperationException("Unsupported review operation status.")
        };
    }

    [Authorize]
    [HttpPost("reviews/{id:guid}/save-draft")]
    [ProducesResponseType(typeof(ReviewAssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveDraft([FromRoute] Guid id, [FromBody] SaveDraftRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(CreateUnauthorized());

        var result = await _reviewsService.SaveDraftAsync(userId.Value, id, request, cancellationToken);

        return result.Status switch
        {
            ReviewOperationStatus.Success => Ok(result.Data),
            ReviewOperationStatus.NotFound => NotFound(CreateNotFound()),
            ReviewOperationStatus.Expired => StatusCode(StatusCodes.Status403Forbidden, CreateError("Review deadline has passed")),
            ReviewOperationStatus.Cancelled => StatusCode(StatusCodes.Status403Forbidden, CreateError("Review has been cancelled")),
            _ => throw new InvalidOperationException("Unsupported review operation status.")
        };
    }

    [Authorize]
    [HttpPost("reviews/{id:guid}/submit")]
    [ProducesResponseType(typeof(ReviewAssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitReview([FromRoute] Guid id, [FromBody] SubmitReviewRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(CreateUnauthorized());

        var result = await _reviewsService.SubmitReviewAsync(userId.Value, id, request, cancellationToken);

        return result.Status switch
        {
            ReviewOperationStatus.Success => Ok(result.Data),
            ReviewOperationStatus.NotFound => NotFound(CreateNotFound()),
            ReviewOperationStatus.Forbidden => Forbid(),
            ReviewOperationStatus.Expired => StatusCode(StatusCodes.Status403Forbidden, CreateError("Review deadline has passed")),
            ReviewOperationStatus.Cancelled => StatusCode(StatusCodes.Status403Forbidden, CreateError("Review has been cancelled")),
            ReviewOperationStatus.InvalidState => StatusCode(StatusCodes.Status409Conflict, CreateError(result.ErrorMessage ?? "Invalid state")),
            _ => throw new InvalidOperationException("Unsupported review operation status.")
        };
    }

    [Authorize]
    [HttpPost("tasks/{taskId:guid}/generate-reviews")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateReviews(Guid taskId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(CreateUnauthorized());

        var task = await _dbContext.Posts
            .Include(x => x.Subject)
            .SingleOrDefaultAsync(x => x.Id == taskId, ct);

        if (task is null)
            return NotFound(CreateNotFoundTask());

        var isTeacherOrAdmin = await _dbContext.SubjectParticipants
            .AnyAsync(x => x.SubjectId == task.SubjectId && x.UserId == userId.Value
                && (x.Role == "Teacher" || x.Role == "Admin"), ct);

        if (!isTeacherOrAdmin)
            return Forbid();

        var existingCount = await _dbContext.ReviewAssignments
            .CountAsync(x => x.TaskId == taskId, ct);

        if (existingCount > 0)
            return Ok(new { count = existingCount, message = "Assignments already exist" });

        var mode = task.ReviewMode ?? task.Subject.PeerReviewMode ?? "all_to_all";

        IReadOnlyList<ReviewAssignment> assignments;
        if (mode == "pairs")
            assignments = await _distributionService.GeneratePairsAsync(taskId, task.Subject.PairingStrategy ?? "ordered", ct);
        else
            assignments = await _distributionService.GenerateAllToAllAsync(taskId, ct);

        return Ok(new { count = assignments.Count });
    }

    [Authorize]
    [HttpGet("teams/{teamId:guid}/reviews")]
    [ProducesResponseType(typeof(IReadOnlyList<ReviewAssignmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTeamReviews(Guid teamId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(CreateUnauthorized());

        var team = await _dbContext.Teams
            .Include(x => x.Members)
            .SingleOrDefaultAsync(x => x.Id == teamId, ct);

        if (team is null)
            return NotFound(CreateNotFoundTeam());

        var isMember = team.Members.Any(m => m.UserId == userId.Value);

        var isTeacherOrAdmin = await _dbContext.SubjectParticipants
            .AnyAsync(x => x.SubjectId == team.SubjectId && x.UserId == userId.Value
                && (x.Role == "Teacher" || x.Role == "Admin"), ct);

        if (!isMember && !isTeacherOrAdmin)
            return Forbid();

        var reviews = await _reviewsService.GetTeamReviewsAsync(teamId, ct);
        return Ok(reviews);
    }

    [Authorize]
    [HttpPost("reviews/{reviewId:guid}/reject")]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectReview(Guid reviewId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(CreateUnauthorized());

        var result = await _reviewsService.RejectReviewAsync(userId.Value, reviewId, ct);
        if (result is null)
            return NotFound(CreateNotFound());

        return Ok(result);
    }

    [Authorize]
    [HttpPut("submissions/{submissionId:guid}/final-grade")]
    [ProducesResponseType(typeof(FinalGradeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> OverrideFinalGrade(Guid submissionId, [FromBody] OverrideGradeRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(CreateUnauthorized());

        var result = await _reviewsService.OverrideFinalGradeAsync(userId.Value, submissionId, request.FinalScore, request.Comment, ct);
        if (result is null)
            return NotFound(CreateNotFound());

        return Ok(result);
    }

    [Authorize]
    [HttpPost("courses/{courseId:guid}/recalculate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecalculateCourse(Guid courseId, [FromQuery] Guid? submissionId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(CreateUnauthorized());

        var isTeacherOrAdmin = await _dbContext.SubjectParticipants
            .AnyAsync(x => x.SubjectId == courseId && x.UserId == userId.Value
                && (x.Role == "Teacher" || x.Role == "Admin"), ct);
        if (!isTeacherOrAdmin) return Forbid();

        if (submissionId.HasValue)
        {
            var submission = await _dbContext.Submissions
                .Include(x => x.post)
                .SingleOrDefaultAsync(x => x.id == submissionId.Value && x.post.SubjectId == courseId, ct);
            if (submission is null) return NotFound(CreateNotFound());

            var finalGradeDto = await _reviewsService.GetFinalGradeAsync(submissionId.Value, ct);
            if (finalGradeDto is not null)
            {
                submission.FinalScore = finalGradeDto.FinalScore;
                submission.FinalSource = finalGradeDto.FinalSource;

                var existing = await _dbContext.FinalGrades
                    .SingleOrDefaultAsync(x => x.SubmissionId == submissionId.Value, ct);
                if (existing is not null)
                {
                    existing.FinalScore = finalGradeDto.FinalScore;
                    existing.FinalSource = finalGradeDto.FinalSource;
                    existing.CalculatedAt = DateTimeOffset.UtcNow;
                }
                await _dbContext.SaveChangesAsync(ct);
            }
            return Ok(new { recalculated = 1 });
        }

        var submissions = await _dbContext.Submissions
            .Where(x => x.post.SubjectId == courseId)
            .ToListAsync(ct);

        var count = 0;
        foreach (var sub in submissions)
        {
            var reviews = await _dbContext.Reviews
                .Where(x => x.Assignment.SubmissionId == sub.id && !x.IsRejected && x.IsFinal)
                .ToListAsync(ct);

            if (reviews.Count > 0)
            {
                sub.FinalScore = reviews.Average(r => r.OverallScore ?? 0);
                sub.FinalSource = "peer";
            }

            count++;
        }

        await _dbContext.SaveChangesAsync(ct);
        return Ok(new { recalculated = count });
    }

    private static Microsoft.AspNetCore.Mvc.ProblemDetails CreateUnauthorized()
    {
        return new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Title = "Unauthorized",
            Status = StatusCodes.Status401Unauthorized,
            Detail = "Authentication failed."
        };
    }

    private static Microsoft.AspNetCore.Mvc.ProblemDetails CreateNotFound()
    {
        return new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Title = "Not Found",
            Status = StatusCodes.Status404NotFound,
            Detail = "Review assignment not found."
        };
    }

    private static Microsoft.AspNetCore.Mvc.ProblemDetails CreateNotFoundTeam()
    {
        return new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Title = "Not Found",
            Status = StatusCodes.Status404NotFound,
            Detail = "Team not found."
        };
    }

    private static Microsoft.AspNetCore.Mvc.ProblemDetails CreateNotFoundTask()
    {
        return new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Title = "Not Found",
            Status = StatusCodes.Status404NotFound,
            Detail = "Task not found."
        };
    }

    private static Microsoft.AspNetCore.Mvc.ProblemDetails CreateError(string detail)
    {
        return new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Title = "Error",
            Status = StatusCodes.Status403Forbidden,
            Detail = detail
        };
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
