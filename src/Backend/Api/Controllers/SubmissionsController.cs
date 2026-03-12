using Microsoft.AspNetCore.Mvc;
using Application.Submissions.Models;
using Infrastructure.Persistence.Entities;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/assignments")]
    public class SubmissionsController : Controller
    {
        [HttpPost("{assignmentId}/submissions")]
        public IActionResult CreateSubmission(
            Guid assignmentId,
            [FromBody] SubmissionCreateRequest request,
            [FromQuery] bool? isStudent)
        {
            if (isStudent == false)
            {
                return Forbid();
            }

            var submission = new Submission
            {
                id = Guid.NewGuid(),
                assignmentId = assignmentId,
                authorId = Guid.NewGuid(),
                answers = request.answers,
                status = SubmissionStatusEnum.Draft,
                submittedAt = DateTime.UtcNow
            };

            return Created($"/api/submissions/{submission.id}", submission);
        }

        [HttpGet("/api/submissions/{submissionId}")]
        public IActionResult GetSubmission(
            Guid submissionId,
            [FromQuery] bool? isAuthor,
            [FromQuery] bool? isTeacher)
        {
            if (isAuthor != true && isTeacher != true)
            {
                return Forbid();
            }

            var submission = new Submission
            {
                id = submissionId,
                assignmentId = Guid.NewGuid(),
                authorId = Guid.NewGuid(),
                answers = new List<AnswerItem>(),
                status = SubmissionStatusEnum.Draft,
                submittedAt = DateTime.UtcNow
            };

            return Ok(submission);
        }
    }
}