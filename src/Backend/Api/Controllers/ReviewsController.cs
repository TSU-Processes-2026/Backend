using Api.Authentication;
using Application.Reviews.Contracts;
using Application.Reviews.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api")]
public sealed class ReviewsController : ControllerBase
{
    private readonly IReviewsService _reviewsService;

    public ReviewsController(IReviewsService reviewsService)
    {
        _reviewsService = reviewsService;
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
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartReview([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(CreateUnauthorized());

        var result = await _reviewsService.StartReviewAsync(userId.Value, id, cancellationToken);
        if (result is null)
            return NotFound(CreateNotFound());

        return Ok(result);
    }

    [Authorize]
    [HttpPost("reviews/{id:guid}/save-draft")]
    [ProducesResponseType(typeof(ReviewAssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveDraft([FromRoute] Guid id, [FromBody] SaveDraftRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(CreateUnauthorized());

        var result = await _reviewsService.SaveDraftAsync(userId.Value, id, request, cancellationToken);
        if (result is null)
            return NotFound(CreateNotFound());

        return Ok(result);
    }

    [Authorize]
    [HttpPost("reviews/{id:guid}/submit")]
    [ProducesResponseType(typeof(ReviewAssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitReview([FromRoute] Guid id, [FromBody] SubmitReviewRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(CreateUnauthorized());

        var result = await _reviewsService.SubmitReviewAsync(userId.Value, id, request, cancellationToken);
        if (result is null)
            return NotFound(CreateNotFound());

        return Ok(result);
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
}
