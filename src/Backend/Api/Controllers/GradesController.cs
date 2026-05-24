using Application.Grades.Contract;
using Application.Grades.Models;
using Application.Submissions.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Text;

namespace Api.Controllers
{
    [ApiController]
    public class GradesController : ControllerBase
    {
        private readonly IGradesService _gradesService;

        public GradesController(IGradesService gradesService)
        {
            _gradesService = gradesService;
        }

        [HttpGet("api/submissions/{submissionId}/grade")]
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
        [HttpPost("api/submissions/{submissionId}/grade")]
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
        [HttpPut("api/submissions/{submissionId}/grade")]
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
        [HttpDelete("api/submissions/{submissionId}/grade")]
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

        [Authorize]
        [HttpGet("api/courses/{id:guid}/grades")]
        [ProducesResponseType(typeof(IReadOnlyList<CourseGradeDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCourseGrades([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            if (userId is null)
            {
                return Unauthorized(CreateUnauthorized());
            }

            var result = await _gradesService.GetCourseGradesAsync(userId.Value, id, cancellationToken);

            return result.Status switch
            {
                CourseGradesListStatus.Success => Ok(result.Grades),
                CourseGradesListStatus.NotFound => NotFound(CreateNotFound()),
                CourseGradesListStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
                _ => throw new InvalidOperationException("Unsupported course grades list status.")
            };
        }

        [Authorize]
        [HttpPost("api/courses/{id:guid}/calculate-grades")]
        [ProducesResponseType(typeof(IReadOnlyList<CourseGradeDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CalculateCourseGrades([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            if (userId is null)
            {
                return Unauthorized(CreateUnauthorized());
            }

            var result = await _gradesService.CalculateCourseGradesAsync(userId.Value, id, cancellationToken);

            return result.Status switch
            {
                CourseGradesCalculateStatus.Success => Ok(result.Grades),
                CourseGradesCalculateStatus.NotFound => NotFound(CreateNotFound()),
                CourseGradesCalculateStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
                _ => throw new InvalidOperationException("Unsupported course grades calculate status.")
            };
        }

        [Authorize]
        [HttpGet("api/courses/{id:guid}/grade-scale")]
        [ProducesResponseType(typeof(IReadOnlyList<GradeScaleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetGradeScale([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            if (userId is null)
            {
                return Unauthorized(CreateUnauthorized());
            }

            var result = await _gradesService.GetGradeScaleAsync(userId.Value, id, cancellationToken);

            return result.Status switch
            {
                GradeScaleAccessStatus.Success => Ok(result.Scale),
                GradeScaleAccessStatus.NotFound => NotFound(CreateNotFound()),
                GradeScaleAccessStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
                _ => throw new InvalidOperationException("Unsupported grade scale get status.")
            };
        }

        [Authorize]
        [HttpPut("api/courses/{id:guid}/grade-scale")]
        [ProducesResponseType(typeof(IReadOnlyList<GradeScaleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpsertGradeScale([FromRoute] Guid id, [FromBody] UpsertGradeScaleRequest? request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            if (userId is null)
            {
                return Unauthorized(CreateUnauthorized());
            }

            var result = await _gradesService.UpsertGradeScaleAsync(userId.Value, id, request ?? new UpsertGradeScaleRequest(), cancellationToken);

            return result.Status switch
            {
                GradeScaleAccessStatus.Success => Ok(result.Scale),
                GradeScaleAccessStatus.NotFound => NotFound(CreateNotFound()),
                GradeScaleAccessStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
                _ => throw new InvalidOperationException("Unsupported grade scale update status.")
            };
        }

        [Authorize]
        [HttpGet("api/courses/{id:guid}/grades/export")]
        [Produces("text/csv")]
        public async Task<IActionResult> ExportCourseGrades([FromRoute] Guid id, [FromQuery] string? format, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            if (userId is null)
            {
                return Unauthorized(CreateUnauthorized());
            }

            var result = await _gradesService.GetCourseGradesAsync(userId.Value, id, cancellationToken);

            if (result.Status == CourseGradesListStatus.NotFound)
            {
                return NotFound(CreateNotFound());
            }

            if (result.Status == CourseGradesListStatus.Forbidden)
            {
                return StatusCode(StatusCodes.Status403Forbidden, CreateForbidden());
            }

            var csv = new StringBuilder();
            csv.AppendLine("StudentId,FinalScore,FinalGrade,CalculatedAt");

            foreach (var grade in result.Grades)
            {
                csv.AppendLine($"{grade.StudentId},{grade.FinalScore},{EscapeCsv(grade.FinalGrade)},{grade.CalculatedAt:O}");
            }

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"course-{id}-grades.csv");
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

        private static string EscapeCsv(string value)
        {
            if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
            {
                return value;
            }

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}
 
