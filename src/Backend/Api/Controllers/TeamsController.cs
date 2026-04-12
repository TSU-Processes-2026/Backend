using Api.Authentication;
using Application.Teams.Contracts;
using Application.Teams.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class TeamsController : ControllerBase
{
    private readonly ITeamsService _teamsService;

    public TeamsController(ITeamsService teamsService)
    {
        _teamsService = teamsService;
    }

    [HttpGet("subjects/{subjectId:guid}/teams/settings")]
    [ProducesResponseType(typeof(TeamSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSettings(
        [FromRoute] Guid subjectId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _teamsService.GetSettingsAsync(userId.Value, subjectId, cancellationToken);

        return result.Status switch
        {
            TeamSettingsStatus.Success => Ok(result.Settings),
            TeamSettingsStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            TeamSettingsStatus.Invalid => BadRequest(CreateBadRequest(result.Errors)),
            _ => throw new InvalidOperationException("Unsupported team settings status.")
        };
    }

    [HttpPut("subjects/{subjectId:guid}/teams/settings")]
    [ProducesResponseType(typeof(TeamSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateSettings(
        [FromRoute] Guid subjectId,
        [FromBody] TeamSettingsRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _teamsService.UpdateSettingsAsync(userId.Value, subjectId, request ?? new TeamSettingsRequest(), cancellationToken);

        return result.Status switch
        {
            TeamSettingsStatus.Success => Ok(result.Settings),
            TeamSettingsStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            TeamSettingsStatus.Invalid => BadRequest(CreateBadRequest(result.Errors)),
            _ => throw new InvalidOperationException("Unsupported team settings status.")
        };
    }

    [HttpGet("subjects/{subjectId:guid}/teams")]
    [ProducesResponseType(typeof(TeamsListWithConfigResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTeams(
        [FromRoute] Guid subjectId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _teamsService.GetTeamsAsync(userId.Value, subjectId, cancellationToken);

        return result.Status switch
        {
            TeamListStatus.Success => Ok(new TeamsListWithConfigResponse
            {
                Teams = result.Teams,
                DistributionMode = result.DistributionMode,
                IsFinalized = result.IsFinalized
            }),
            TeamListStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            _ => throw new InvalidOperationException("Unsupported team list status.")
        };
    }

    [HttpGet("subjects/{subjectId:guid}/teams/unassigned")]
    [ProducesResponseType(typeof(UnassignedStudentsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUnassignedStudents(
        [FromRoute] Guid subjectId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _teamsService.GetUnassignedStudentsAsync(userId.Value, subjectId, cancellationToken);

        return result.Status switch
        {
            UnassignedStudentsStatus.Success => Ok(result.Response),
            UnassignedStudentsStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            _ => throw new InvalidOperationException("Unsupported unassigned students status.")
        };
    }

    [HttpPost("subjects/{subjectId:guid}/teams/random")]
    [ProducesResponseType(typeof(TeamRandomPreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(TeamRandomPreviewResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> PreviewRandomDistribution(
        [FromRoute] Guid subjectId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _teamsService.PreviewRandomDistributionAsync(userId.Value, subjectId, cancellationToken);

        return result.Status switch
        {
            TeamRandomPreviewStatus.Success => Ok(result.Response),
            TeamRandomPreviewStatus.Invalid => BadRequest(result.Response),
            TeamRandomPreviewStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            _ => throw new InvalidOperationException("Unsupported team random preview status.")
        };
    }

    [HttpPost("subjects/{subjectId:guid}/teams/validate")]
    [ProducesResponseType(typeof(TeamValidationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ValidateManualDistribution(
        [FromRoute] Guid subjectId,
        [FromBody] ManualTeamDistributionRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _teamsService.ValidateManualDistributionAsync(userId.Value, subjectId, request ?? new ManualTeamDistributionRequest(), cancellationToken);

        return result.Status switch
        {
            TeamValidationStatus.Success => Ok(result.Validation),
            TeamValidationStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            _ => throw new InvalidOperationException("Unsupported team validation status.")
        };
    }

    [HttpPost("subjects/{subjectId:guid}/teams")]
    [ProducesResponseType(typeof(TeamDistributionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateTeam(
        [FromRoute] Guid subjectId,
        [FromBody] ManualTeamRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _teamsService.CreateTeamAsync(userId.Value, subjectId, request ?? new ManualTeamRequest(), cancellationToken);

        return result.Status switch
        {
            TeamMutationStatus.Success => StatusCode(StatusCodes.Status201Created, result.Response),
            TeamMutationStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            TeamMutationStatus.Invalid => BadRequest(CreateBadRequest(result.Errors)),
            _ => throw new InvalidOperationException("Unsupported team create status.")
        };
    }

    [HttpPut("subjects/{subjectId:guid}/teams/{teamId:guid}")]
    [ProducesResponseType(typeof(TeamDistributionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateTeam(
        [FromRoute] Guid subjectId,
        [FromRoute] Guid teamId,
        [FromBody] ManualTeamRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _teamsService.UpdateTeamAsync(userId.Value, subjectId, teamId, request ?? new ManualTeamRequest(), cancellationToken);

        return result.Status switch
        {
            TeamMutationStatus.Success => Ok(result.Response),
            TeamMutationStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            TeamMutationStatus.Invalid => BadRequest(CreateBadRequest(result.Errors)),
            _ => throw new InvalidOperationException("Unsupported team update status.")
        };
    }

    [HttpPost("subjects/{subjectId:guid}/teams/manual")]
    [ProducesResponseType(typeof(TeamDistributionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateManualDistribution(
        [FromRoute] Guid subjectId,
        [FromBody] ManualTeamDistributionRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _teamsService.CreateManualDistributionAsync(userId.Value, subjectId, request ?? new ManualTeamDistributionRequest(), cancellationToken);

        return result.Status switch
        {
            TeamCreateStatus.Success => StatusCode(StatusCodes.Status201Created, result.Response),
            TeamCreateStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            TeamCreateStatus.Invalid => BadRequest(CreateBadRequest(result.Errors)),
            _ => throw new InvalidOperationException("Unsupported team create status.")
        };
    }

    [HttpPost("subjects/{subjectId:guid}/teams/finalize")]
    [ProducesResponseType(typeof(TeamFinalizeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(TeamValidationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Finalize(
        [FromRoute] Guid subjectId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _teamsService.FinalizeAsync(userId.Value, subjectId, cancellationToken);

        return result.Status switch
        {
            TeamFinalizeStatus.Success => Ok(result.Response),
            TeamFinalizeStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            TeamFinalizeStatus.Invalid => BadRequest(new TeamValidationResponse
            {
                IsValid = false,
                Errors = result.Errors,
                Warnings = result.Warnings
            }),
            _ => throw new InvalidOperationException("Unsupported team finalize status.")
        };
    }

    [HttpPost("subjects/{subjectId:guid}/teams/draft/start")]
    [ProducesResponseType(typeof(DraftStateResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> StartDraft(
        [FromRoute] Guid subjectId,
        [FromBody] DraftStartRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _teamsService.StartDraftAsync(userId.Value, subjectId, request ?? new DraftStartRequest(), cancellationToken);

        return result.Status switch
        {
            DraftStartStatus.Success => StatusCode(StatusCodes.Status201Created, result.Response),
            DraftStartStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            DraftStartStatus.Invalid => BadRequest(CreateBadRequest(result.Errors)),
            _ => throw new InvalidOperationException("Unsupported draft start status.")
        };
    }

    [HttpGet("subjects/{subjectId:guid}/teams/draft/state")]
    [ProducesResponseType(typeof(DraftStateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDraftState(
        [FromRoute] Guid subjectId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _teamsService.GetDraftStateAsync(userId.Value, subjectId, cancellationToken);

        return result.Status switch
        {
            DraftStateStatus.Success => Ok(result.Response),
            DraftStateStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            DraftStateStatus.NotFound => NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = "No draft found for this subject."
            }),
            _ => throw new InvalidOperationException("Unsupported draft state status.")
        };
    }

    /// <summary>
    /// Pick a student during a draft round.
    /// The current captain (whose turn it is) picks an unassigned student to add to their own team.
    /// Teachers/admins can also make picks on behalf of the current captain.
    /// </summary>
    /// <param name="subjectId">The subject ID.</param>
    /// <param name="request">The pick request containing the student ID to pick.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated draft state.</returns>
    [HttpPost("subjects/{subjectId:guid}/teams/draft/pick")]
    [ProducesResponseType(typeof(DraftStateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DraftPick(
        [FromRoute] Guid subjectId,
        [FromBody] DraftPickRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _teamsService.DraftPickAsync(userId.Value, subjectId, request ?? new DraftPickRequest(), cancellationToken);

        return result.Status switch
        {
            DraftPickStatus.Success => Ok(result.Response),
            DraftPickStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            DraftPickStatus.Invalid => BadRequest(CreateBadRequest(result.Errors)),
            _ => throw new InvalidOperationException("Unsupported draft pick status.")
        };
    }
    [HttpPost("subjects/{subjectId:guid}/teams/student-create")]
    [ProducesResponseType(typeof(TeamDistributionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> StudentCreateTeam(
        [FromRoute] Guid subjectId,
        [FromBody] StudentCreateTeamRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _teamsService.StudentCreateTeamAsync(userId.Value, subjectId, request ?? new StudentCreateTeamRequest(), cancellationToken);

        return result.Status switch
        {
            TeamMutationStatus.Success => StatusCode(StatusCodes.Status201Created, result.Response),
            TeamMutationStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            TeamMutationStatus.Invalid => BadRequest(CreateBadRequest(result.Errors)),
            _ => throw new InvalidOperationException("Unsupported student create team status.")
        };
    }

    [HttpPost("subjects/{subjectId:guid}/teams/{teamId:guid}/join")]
    [ProducesResponseType(typeof(TeamDistributionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> StudentJoinTeam(
        [FromRoute] Guid subjectId,
        [FromRoute] Guid teamId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _teamsService.StudentJoinTeamAsync(userId.Value, subjectId, teamId, cancellationToken);

        return result.Status switch
        {
            StudentJoinTeamStatus.Success => Ok(result.Response),
            StudentJoinTeamStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            StudentJoinTeamStatus.Invalid => BadRequest(CreateBadRequest(result.Errors)),
            _ => throw new InvalidOperationException("Unsupported student join team status.")
        };
    }

    [HttpPost("subjects/{subjectId:guid}/teams/{teamId:guid}/leave")]
    [ProducesResponseType(typeof(TeamDistributionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> StudentLeaveTeam(
        [FromRoute] Guid subjectId,
        [FromRoute] Guid teamId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _teamsService.StudentLeaveTeamAsync(userId.Value, subjectId, teamId, cancellationToken);

        return result.Status switch
        {
            StudentLeaveTeamStatus.Success => Ok(result.Response),
            StudentLeaveTeamStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            StudentLeaveTeamStatus.Invalid => BadRequest(CreateBadRequest(result.Errors)),
            _ => throw new InvalidOperationException("Unsupported student leave team status.")
        };
    }

    private static ProblemDetails CreateUnauthorized()
    {
        return new ProblemDetails
        {
            Title = "Unauthorized",
            Status = StatusCodes.Status401Unauthorized,
            Detail = "Authentication failed."
        };
    }

    private static ProblemDetails CreateForbidden()
    {
        return new ProblemDetails
        {
            Title = "Forbidden",
            Status = StatusCodes.Status403Forbidden,
            Detail = "Access denied."
        };
    }

    private static ProblemDetails CreateBadRequest(IReadOnlyList<string> errors)
    {
        return new ProblemDetails
        {
            Title = "Bad Request",
            Status = StatusCodes.Status400BadRequest,
            Detail = errors.Count == 0 ? "Validation failed." : string.Join("; ", errors)
        };
    }
}
