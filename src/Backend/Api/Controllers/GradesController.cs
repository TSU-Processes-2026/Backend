using Application.Grades.Contract;
using Application.Grades.Models;
using Application.Submissions.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/submissions/{submissionId}/grade")]
    public class GradesController : ControllerBase
    {
        private readonly IGradesService _gradesService;

        public GradesController(IGradesService gradesService)
        {
            _gradesService = gradesService;
        }

        [HttpGet]
        public async Task<IActionResult> GetGrade(Guid submissionId)
        {
            var result = await _gradesService.GetGradeAsync(submissionId);

            return result.Status switch
            {
                GradesAccessStatus.Success => Ok(result.grade),
                GradesAccessStatus.NotFound => NotFound(),
                GradesAccessStatus.Forbidden => Forbid(),
                _ => StatusCode(500)
            };
        }

        [HttpPost]
        public async Task<IActionResult> CreateGrade(Guid submissionId, [FromBody] GradeRequest request)
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var result = await _gradesService.CreateGradeAsync(submissionId, request.score, request.verdictText, teacherId);

            return result.Status switch
            {
                GradesAccessStatus.Success => Created($"/api/submissions/{submissionId}/grade", result.grade),
                GradesAccessStatus.NotFound => NotFound(),
                GradesAccessStatus.Forbidden => Forbid(),
                _ => StatusCode(500)
            };
        }
    }
}