using Application.Grades.Contract;
using Application.Grades.Models;
using Application.Submissions.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Authentication;
using Microsoft.AspNetCore.Authorization;

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
        
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateGrade(Guid submissionId, [FromBody] GradeRequest request)
        {
            var teacherId = User.GetUserId();

            if (!teacherId.HasValue)
            {
                return Unauthorized("In grade controller you are not authorized");
            }

            var result = await _gradesService.CreateGradeAsync(submissionId, request.score, request.verdictText, teacherId.Value.ToString());

            return result.Status switch
            {
                GradesAccessStatus.Success => Created($"/api/submissions/{submissionId}/grade", result.grade),
                GradesAccessStatus.NotFound => NotFound(),
                GradesAccessStatus.Forbidden => Forbid(),
                _ => StatusCode(500)
            };
        }

        [Authorize]
        [HttpPut]
        public async Task<IActionResult> UpdateGrade(Guid submissionId, [FromBody] GradeRequest request)
        {
            var teacherId = User.GetUserId();

            if (!teacherId.HasValue)
            {
                return Unauthorized("In grade controller you are not authorized");
            }

            var result = await _gradesService.UpdateGradeAsync(submissionId, request.score, request.verdictText, teacherId.Value.ToString());

            return result.Status switch
            {
                GradesAccessStatus.Success => Ok(result.grade),
                GradesAccessStatus.NotFound => NotFound(),
                GradesAccessStatus.Forbidden => Forbid(),
                _ => StatusCode(500)
            };
        }

        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> DeleteGrade(Guid submissionId)
        {
            var teacherId = User.GetUserId();

            if (!teacherId.HasValue)
            {
                return Unauthorized("In grade controller you are not authorized");
            }

            var result = await _gradesService.DeleteGradeAsync(submissionId, teacherId.Value.ToString());

            return result.Status switch
            {
                GradesAccessStatus.Success => NoContent(),
                GradesAccessStatus.NotFound => NotFound(),
                GradesAccessStatus.Forbidden => Forbid(),
                _ => StatusCode(500)
            };
        }
    }
}
 