using Application.Grades.Models;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/submissions/{submissionId}/grade")]
    public class GradesController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetGrade(Guid submissionId)
        {
            GradeDto gradeDto = new GradeDto
            {
                id = submissionId,
                score = 4,
                submissionId = submissionId,
                verdictedAt = DateTime.Now,
                verdictText = "string"
            };

            return Ok(gradeDto);
        }

        [HttpPost]
        public IActionResult CreateGrade(Guid submissionId, [FromBody] object request)
        {
            GradeDto gradeDto = new GradeDto
            {
                id = submissionId,
                score = 4,
                submissionId = submissionId,
                verdictedAt = DateTime.Now,
                verdictText = "string"
            };

            return Ok(gradeDto);
        }
    }
}
