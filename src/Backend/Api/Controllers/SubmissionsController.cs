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

            var answers = request.answers.Select(a => new AnswerItem
            {
                id = a.id,
                assignmentQuestionId = a.assignmentQuestionId,
                answerType = a.answerType,
                selectedOptionId = a.selectedOptionId,
                selectedOptionsId = a.selectedOptionIds,
                text = a.text
            }).ToList();

            var submission = new Submission
            {
                id = Guid.NewGuid(),
                assignmentId = assignmentId,
                authorId = Guid.NewGuid(),
                answers = answers,
                status = SubmissionStatusEnum.Draft,
                submittedAt = DateTime.UtcNow
            };

            return Created($"/api/submissions/{submission.id}", submission);
        }

        [HttpGet("{assignmentId}/submissions")]
        public IActionResult GetSubmissions(
            Guid assignmentId,
            [FromQuery] int limit = 20,
            [FromQuery] int offset = 0,
            [FromQuery] bool? isTeacher = true)
        {
            if (isTeacher != true)
            {
                return Forbid();
            }

            var submissions = new List<Submission>
            {
                new Submission
                {
                    id = Guid.NewGuid(),
                    assignmentId = assignmentId,
                    authorId = Guid.NewGuid(),
                    answers = new List<AnswerItem>(),
                    status = SubmissionStatusEnum.Draft,
                    submittedAt = DateTime.UtcNow
                }
            };

            var result = submissions
                .Skip(offset)
                .Take(limit)
                .ToList();

            return Ok(result);
        }

        [HttpGet("/api/submissions/{submissionId}")]
        public IActionResult GetSubmission(Guid submissionId)
        {
            var submission = new Submission
            {
                id = submissionId,
                assignmentId = Guid.NewGuid(),
                authorId = Guid.NewGuid(),
                status = SubmissionStatusEnum.Draft,
                submittedAt = DateTime.UtcNow,
                answers = new List<AnswerItem>
                {
                    new AnswerItem
                    {
                        assignmentQuestionId = Guid.NewGuid(),
                        answerType = AnswerTypeEnum.SingleChoiceAnswer,
                        selectedOptionId = Guid.NewGuid()
                    },
                    new AnswerItem
                    {
                        assignmentQuestionId = Guid.NewGuid(),
                        answerType = AnswerTypeEnum.MultipleChoiceAnswer,
                        selectedOptionsId = new List<Guid> { Guid.NewGuid() }
                    },
                    new AnswerItem
                    {
                        assignmentQuestionId = Guid.NewGuid(),
                        answerType = AnswerTypeEnum.TextAnswer,
                        text = "string"
                    }
                }
            };

            return Ok(submission);
        }

        [HttpPatch("/api/submissions/{submissionId}")]
        public IActionResult PatchSubmission(Guid submissionId, [FromBody] SubmissionCreateRequest request)
        {
            var submission = new Submission
            {
                id = submissionId,
                assignmentId = Guid.NewGuid(),
                authorId = Guid.NewGuid(),
                status = SubmissionStatusEnum.Draft,
                submittedAt = DateTime.UtcNow,
                answers = new List<AnswerItem>
                {
                    new AnswerItem
                    {
                        assignmentQuestionId = Guid.NewGuid(),
                        answerType = AnswerTypeEnum.TextAnswer,
                        text = "Updated answer"
                    }
                }
            };

            return Ok(submission);
        }
    }
}