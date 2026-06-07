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

    private static Microsoft.AspNetCore.Mvc.ProblemDetails CreateUnauthorized()
    {
        return new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Title = "Unauthorized",
            Status = StatusCodes.Status401Unauthorized,
            Detail = "Authentication failed."
        };
    }
}
