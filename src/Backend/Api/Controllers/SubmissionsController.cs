using Api.Authentication;
using Application.Submissions.Contracts;
using Application.Submissions.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api")]
public sealed class SubmissionsController : ControllerBase
{
    private readonly ISubmissionsService _submissionsService;

    public SubmissionsController(ISubmissionsService submissionsService)
    {
        _submissionsService = submissionsService;
    }
    [Authorize]

    [HttpPost("assignments/{assignmentId}/submissions")]
    public async Task<IActionResult> CreateSubmission(
        Guid assignmentId,
        [FromBody] SubmissionCreateRequest request,
        [FromQuery] bool? isStudent)
    {
        if (isStudent == false)
            return Forbid();

        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _submissionsService.CreateSubmission(assignmentId, userId.Value, request);

        if (result.Status == SubmissionAccessStatus.NotFound)
            return NotFound();

        if (result.Status == SubmissionAccessStatus.Forbidden)
            return Forbid();

        return Created($"/api/submissions/{result.Submission!.id}", result.Submission);
    }

    [Authorize]
    [HttpPost("tasks/{taskId}/submissions")]
    [ProducesResponseType(typeof(SubmissionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateSubmissionWithSelfAssessment(
        [FromRoute] Guid taskId,
        [FromBody] SubmissionWithSelfAssessmentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(CreateUnauthorized());

        var result = await _submissionsService.CreateSubmissionWithSelfAssessment(
            taskId,
            userId.Value,
            request,
            cancellationToken);

        return result.Status switch
        {
            SubmissionAccessStatus.Success => Created($"/api/submissions/{result.Submission!.id}", result.Submission),
            SubmissionAccessStatus.NotFound => NotFound(CreateNotFound()),
            SubmissionAccessStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            _ => throw new InvalidOperationException("Unsupported submission create status.")
        };
    }

    [HttpGet("assignments/{assignmentId}/submissions")]
    public async Task<IActionResult> GetSubmissions(
        Guid assignmentId,
        int limit = 20,
        int offset = 0,
        bool? isTeacher = true)
    {
        if (isTeacher == true)
        {
            var submissions = await _submissionsService.GetSubmissions(assignmentId, limit, offset);
            return Ok(submissions);
        }

        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized();

        var studentSubmissions = await _submissionsService.GetUserSubmissions(assignmentId, userId.Value, limit, offset);
        return Ok(studentSubmissions);
    }

    [HttpGet("submissions/{submissionId}")]
    public async Task<IActionResult> GetSubmission(Guid submissionId)
    {
        var result = await _submissionsService.GetSubmission(submissionId);

        if (result.Status == SubmissionAccessStatus.NotFound)
            return NotFound();

        if (result.Status == SubmissionAccessStatus.Forbidden)
            return Forbid();

        return Ok(result.Submission);
    }

    [HttpPatch("submissions/{submissionId}")]
    public async Task<IActionResult> PatchSubmission(
        Guid submissionId,
        [FromBody] SubmissionCreateRequest request)
    {
        var result = await _submissionsService.PatchSubmission(submissionId, request);

        if (result.Status == SubmissionAccessStatus.NotFound)
            return NotFound();

        if (result.Status == SubmissionAccessStatus.Forbidden)
            return Forbid();

        return Ok(result.Submission);
    }

    [HttpPost("submissions/{submissionId}/submit")]
    public async Task<IActionResult> SubmitSubmission(Guid submissionId)
    {
        var result = await _submissionsService.SubmitSubmission(submissionId);

        if (result.Status == SubmissionAccessStatus.NotFound)
            return NotFound();

        if (result.Status == SubmissionAccessStatus.Forbidden)
            return Forbid();

        return Ok(result.Submission);
    }

    [HttpPost("submissions/{submissionId}/withdraw")]
    public async Task<IActionResult> WithdrawSubmission(Guid submissionId)
    {
        var result = await _submissionsService.WithdrawSubmission(submissionId);

        if (result.Status == SubmissionAccessStatus.NotFound)
            return NotFound();

        if (result.Status == SubmissionAccessStatus.Forbidden)
            return Forbid();

        return Ok(result.Submission);
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

    private static Microsoft.AspNetCore.Mvc.ProblemDetails CreateForbidden()
    {
        return new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Title = "Forbidden",
            Status = StatusCodes.Status403Forbidden,
            Detail = "Access denied."
        };
    }

    private static Microsoft.AspNetCore.Mvc.ProblemDetails CreateNotFound()
    {
        return new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Title = "Not Found",
            Status = StatusCodes.Status404NotFound,
            Detail = "Resource not found."
        };
    }
}
