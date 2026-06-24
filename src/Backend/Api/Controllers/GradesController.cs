using Application.Grades.Contract;
using Application.Grades.Models;
using Application.Submissions.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Authentication;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Api.Controllers
{
    [ApiController]
    public class GradesController : ControllerBase
    {
        private readonly IGradesService _gradesService;
        private readonly IGradeCalculationService _gradeCalculationService;
        private readonly LmsDbContext _dbContext;

        public GradesController(IGradesService gradesService, IGradeCalculationService gradeCalculationService, LmsDbContext dbContext)
        {
            _gradesService = gradesService;
            _gradeCalculationService = gradeCalculationService;
            _dbContext = dbContext;
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

            await _gradeCalculationService.CalculateCourseGradesAsync(id, cancellationToken);

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
        [HttpGet("api/courses/{courseId:guid}/analytics")]
        [ProducesResponseType(typeof(CourseAnalyticsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCourseAnalytics([FromRoute] Guid courseId, CancellationToken ct)
        {
            var userId = User.GetUserId();

            if (userId is null)
            {
                return Unauthorized(CreateUnauthorized());
            }

            var isTeacherOrAdmin = await _dbContext.SubjectParticipants
                .AnyAsync(x => x.SubjectId == courseId && x.UserId == userId.Value
                    && (x.Role == "Teacher" || x.Role == "Admin"), ct);

            if (!isTeacherOrAdmin)
                return Forbid();

            var courseExists = await _dbContext.Subjects.AnyAsync(x => x.Id == courseId, ct);
            if (!courseExists)
                return NotFound(CreateNotFound());

            var analytics = await BuildAnalyticsAsync(courseId, ct);
            return Ok(analytics);
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

            var isTeacherOrAdmin = await _dbContext.SubjectParticipants
                .AnyAsync(x => x.SubjectId == id && x.UserId == userId.Value
                    && (x.Role == "Teacher" || x.Role == "Admin"), cancellationToken);

            if (!isTeacherOrAdmin)
                return Forbid();

            var analytics = await BuildAnalyticsAsync(id, cancellationToken);

            var sb = new StringBuilder();
            sb.Append("Student");
            foreach (var title in analytics.TaskTitles)
            {
                sb.Append($",{EscapeCsv(title)} (Score),{EscapeCsv(title)} (Source),{EscapeCsv(title)} (Reviewers)");
            }
            sb.AppendLine(",Final Course Grade");

            foreach (var row in analytics.Rows)
            {
                sb.Append(EscapeCsv(row.StudentName));
                foreach (var cell in row.TaskGrades)
                {
                    sb.Append($",{cell.Score},{cell.Source ?? "-"},{cell.ReviewerCount}");
                }
                sb.AppendLine($",{row.FinalCourseGrade}");
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"grades-{id}.csv");
        }

        private async Task<CourseAnalyticsResponse> BuildAnalyticsAsync(Guid courseId, CancellationToken ct)
        {
            var tasks = await _dbContext.Posts
                .Where(x => x.SubjectId == courseId && x.PostType == "Assignment")
                .OrderBy(x => x.CreatedAt)
                .ToListAsync(ct);

            var submissions = await _dbContext.Submissions
                .Include(x => x.post)
                .Where(x => x.post.SubjectId == courseId)
                .ToListAsync(ct);

            var studentUserIds = submissions.Select(x => x.authorId).Distinct().ToList();
            var users = await _dbContext.Users
                .Where(u => studentUserIds.Contains(u.Id))
                .ToListAsync(ct);
            var userNames = users.ToDictionary(u => u.Id, u => u.UserName ?? u.Email ?? u.Id.ToString());

            var submissionsByAuthor = submissions.GroupBy(x => x.authorId).ToList();

            var courseGrades = await _dbContext.CourseGrades
                .Where(x => x.CourseId == courseId)
                .ToListAsync(ct);

            var result = new CourseAnalyticsResponse
            {
                CourseId = courseId,
                TaskTitles = tasks.Select(t => t.Content).ToList()
            };

            foreach (var group in submissionsByAuthor)
            {
                var studentId = group.Key;
                var userName = userNames.GetValueOrDefault(studentId, studentId.ToString());

                var row = new StudentAnalyticsRow
                {
                    StudentId = studentId,
                    StudentName = userName
                };

                foreach (var task in tasks)
                {
                    var sub = group.FirstOrDefault(x => x.assignmentId == task.Id);
                    var reviewCount = sub is not null
                        ? await _dbContext.ReviewAssignments
                            .CountAsync(x => x.SubmissionId == sub.id && x.Status == "submitted", ct)
                        : 0;

                    row.TaskGrades.Add(new TaskGradeCell
                    {
                        TaskId = task.Id,
                        TaskTitle = task.Content,
                        Score = sub?.FinalScore,
                        Source = sub?.FinalSource,
                        ReviewerCount = reviewCount
                    });
                }

                var courseGrade = courseGrades.FirstOrDefault(x => x.StudentId == studentId);
                row.FinalCourseGrade = courseGrade?.FinalScore;

                result.Rows.Add(row);
            }

            return result;
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
 
