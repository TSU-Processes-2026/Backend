using Application.Submissions.Contracts;
using Application.Submissions.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api")]
public class SubmissionsController : Controller
{
    private readonly ISubmissionsService _submissionsService;

    public SubmissionsController(ISubmissionsService submissionsService)
    {
        _submissionsService = submissionsService;
    }

    [HttpPost("assignments/{assignmentId}/submissions")]
    public async Task<IActionResult> CreateSubmission(
        Guid assignmentId,
        [FromBody] SubmissionCreateRequest request,
        [FromQuery] bool? isStudent)
    {
        if (isStudent == false)
            return Forbid();

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _submissionsService.CreateSubmission(assignmentId, userId, request);

        if (result.Status == SubmissionAccessStatus.NotFound)
            return NotFound();

        if (result.Status == SubmissionAccessStatus.Forbidden)
            return Forbid();

        return Created($"/api/submissions/{result.Submission!.id}", result.Submission);
    }

    [HttpGet("assignments/{assignmentId}/submissions")]
    public async Task<IActionResult> GetSubmissions(
        Guid assignmentId,
        int limit = 20,
        int offset = 0,
        bool? isTeacher = true)
    {
        if (isTeacher != true)
            return Forbid();

        var submissions = await _submissionsService.GetSubmissions(assignmentId, limit, offset);
        return Ok(submissions);
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
}