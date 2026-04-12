using Api.Authentication;
using Application.Grades.Contract;
using Application.Grades.Models;
using Application.Submissions.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/teams/{teamId:guid}/assignments/{assignmentId:guid}/grade")]
public sealed class TeamGradesController : ControllerBase
{
    private readonly ITeamGradesService _teamGradesService;

    public TeamGradesController(ITeamGradesService teamGradesService)
    {
        _teamGradesService = teamGradesService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(Guid teamId, Guid assignmentId)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _teamGradesService.GetTeamGradeAsync(userId.Value, teamId, assignmentId);
        return ToActionResult(result, created: false);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid teamId, Guid assignmentId, [FromBody] TeamGradeCreateRequest request)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _teamGradesService.CreateTeamGradeAsync(userId.Value, teamId, assignmentId, request);
        return ToActionResult(result, created: true);
    }

    [HttpPut]
    public async Task<IActionResult> Update(Guid teamId, Guid assignmentId, [FromBody] TeamGradeUpdateRequest request)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _teamGradesService.UpdateTeamGradeAsync(userId.Value, teamId, assignmentId, request);
        return ToActionResult(result, created: false);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(Guid teamId, Guid assignmentId)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _teamGradesService.DeleteTeamGradeAsync(userId.Value, teamId, assignmentId);
        return result.Status switch
        {
            GradesAccessStatus.Success => NoContent(),
            GradesAccessStatus.NotFound => NotFound(),
            GradesAccessStatus.Forbidden => Forbid(),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    private IActionResult ToActionResult(TeamGradeAccessResult result, bool created)
    {
        return result.Status switch
        {
            GradesAccessStatus.Success when created => Created($"/api/teams/{result.Grade!.teamId}/assignments/{result.Grade.assignmentId}/grade", result.Grade),
            GradesAccessStatus.Success => Ok(result.Grade),
            GradesAccessStatus.NotFound => NotFound(),
            GradesAccessStatus.Forbidden => Forbid(),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
