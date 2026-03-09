using Microsoft.AspNetCore.Mvc;
using Application.Submissions.Models;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/assignments")]
    public class SubmissionsController : Controller
    {
        private Application.Submissions.Models.ProblemDetails _problemDetails;

        public SubmissionsController()
        {
        }
        [HttpPost("{assignmentId}/submissions")]
        public IActionResult CreateSubmission(Guid assignmentId, SubmissionCreateRequest submissionCreateRequest, bool? isStudent)
        {

            if (isStudent != null)
            {
                if ((bool)isStudent)
                {
                    var submissionTest = new Submission
                    {
                        id = Guid.NewGuid(),
                        assignmentId = assignmentId,
                        authorId = Guid.NewGuid(),
                        answers = submissionCreateRequest.answers,
                        status = SubmissionStatusEnum.Draft,
                        submittedAt = DateTime.UtcNow
                    };

                    return Created("", submissionTest);
                }
                else
                {
                    return Forbid();
                }
            }

            var submission = new Submission
            {
                id = Guid.NewGuid(),
                assignmentId = assignmentId,
                authorId = Guid.NewGuid(),
                answers = submissionCreateRequest.answers,
                status = SubmissionStatusEnum.Draft,
                submittedAt = DateTime.UtcNow
            };

            return Created("", submission);
        }
    }
}
