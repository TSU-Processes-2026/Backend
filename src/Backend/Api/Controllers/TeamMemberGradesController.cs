using Api.Authentication;
using Application.Grades.Contract;
using Application.Grades.Models;
using Application.Submissions.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/teams/{teamId:guid}/assignments/{assignmentId:guid}/students/{studentId:guid}/grade")]
public sealed class TeamMemberGradesController : ControllerBase
{
    private readonly ITeamMemberGradesService _teamMemberGradesService;

    public TeamMemberGradesController(ITeamMemberGradesService teamMemberGradesService)
    {
        _teamMemberGradesService = teamMemberGradesService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(Guid teamId, Guid assignmentId, Guid studentId)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _teamMemberGradesService.GetMemberGradeAsync(userId.Value, teamId, assignmentId, studentId);
        return ToActionResult(result);
    }

    [HttpPut]
    public async Task<IActionResult> Put(Guid teamId, Guid assignmentId, Guid studentId, [FromBody] TeamMemberGradeAdjustmentRequest request)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _teamMemberGradesService.UpsertMemberGradeAsync(userId.Value, teamId, assignmentId, studentId, request);
        return ToActionResult(result);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(Guid teamId, Guid assignmentId, Guid studentId)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _teamMemberGradesService.DeleteMemberGradeAsync(userId.Value, teamId, assignmentId, studentId);

        return result.Status switch
        {
            GradesAccessStatus.Success => NoContent(),
            GradesAccessStatus.NotFound => NotFound(),
            GradesAccessStatus.Forbidden => Forbid(),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    private IActionResult ToActionResult(TeamMemberGradeAccessResult result)
    {
        return result.Status switch
        {
            GradesAccessStatus.Success => Ok(result.Grade),
            GradesAccessStatus.NotFound => NotFound(),
            GradesAccessStatus.Forbidden => Forbid(),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}