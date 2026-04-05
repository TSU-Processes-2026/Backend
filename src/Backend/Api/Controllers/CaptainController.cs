using Api.Authentication;
using Application.Teams.Contracts;
using Application.Teams.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/subjects/{subjectId:guid}/teams/{teamId:guid}/captain")]
public sealed class CaptainController : ControllerBase
{
    private readonly ICaptainSelectionService _captainService;

    public CaptainController(ICaptainSelectionService captainService)
    {
        _captainService = captainService;
    }

    [HttpPost("initiate-voting")]
    [ProducesResponseType(typeof(CaptainVotingSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CaptainVotingSessionResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> InitiateVoting(
        [FromRoute] Guid subjectId,
        [FromRoute] Guid teamId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _captainService.InitiateVotingAsync(subjectId, teamId, userId.Value, cancellationToken);

        return result.Status switch
        {
            CaptainVotingInitiateStatus.Success => Ok(result.Session),
            CaptainVotingInitiateStatus.AlreadyActive => StatusCode(StatusCodes.Status409Conflict, result.Session),
            CaptainVotingInitiateStatus.NotFound => NotFound(CreateNotFound(result.ErrorMessage)),
            CaptainVotingInitiateStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            CaptainVotingInitiateStatus.InvalidOperation => BadRequest(CreateBadRequest(result.ErrorMessage)),
            _ => throw new InvalidOperationException("Unsupported captain voting initiate status.")
        };
    }

    [HttpPost("vote")]
    [ProducesResponseType(typeof(CaptainVoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CastVote(
        [FromRoute] Guid subjectId,
        [FromRoute] Guid teamId,
        [FromBody] CaptainVoteRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _captainService.CastVoteAsync(teamId, userId.Value, request.VotedForUserId, cancellationToken);

        return result.Status switch
        {
            CaptainVoteStatus.Success => Ok(new CaptainVoteResponse(result.SessionCompleted, result.SelectedCaptainId)),
            CaptainVoteStatus.NotFound => NotFound(CreateNotFound(result.ErrorMessage)),
            CaptainVoteStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            CaptainVoteStatus.InvalidOperation => BadRequest(CreateBadRequest(result.ErrorMessage)),
            CaptainVoteStatus.AlreadyVoted => BadRequest(CreateBadRequest(result.ErrorMessage)),
            CaptainVoteStatus.SessionClosed => BadRequest(CreateBadRequest(result.ErrorMessage)),
            CaptainVoteStatus.InvalidCandidate => BadRequest(CreateBadRequest(result.ErrorMessage)),
            _ => throw new InvalidOperationException("Unsupported captain vote status.")
        };
    }

    [HttpGet("voting-status")]
    [ProducesResponseType(typeof(CaptainVotingStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVotingStatus(
        [FromRoute] Guid subjectId,
        [FromRoute] Guid teamId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _captainService.GetVotingStatusAsync(teamId, userId.Value, cancellationToken);

        return result.Status switch
        {
            CaptainVotingStatusStatus.Success => Ok(result.Response),
            CaptainVotingStatusStatus.NotFound => NotFound(CreateNotFound(result.ErrorMessage)),
            CaptainVotingStatusStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            CaptainVotingStatusStatus.NoActiveSession => NotFound(CreateNotFound(result.ErrorMessage)),
            _ => throw new InvalidOperationException("Unsupported captain voting status status.")
        };
    }

    [HttpPost("select-random")]
    [ProducesResponseType(typeof(CaptainInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SelectRandom(
        [FromRoute] Guid subjectId,
        [FromRoute] Guid teamId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _captainService.SelectRandomCaptainAsync(subjectId, teamId, userId.Value, cancellationToken);

        return result.Status switch
        {
            CaptainSelectionStatus.Success => Ok(result.Captain),
            CaptainSelectionStatus.NotFound => NotFound(CreateNotFound(result.ErrorMessage)),
            CaptainSelectionStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            CaptainSelectionStatus.InvalidOperation => BadRequest(CreateBadRequest(result.ErrorMessage)),
            _ => throw new InvalidOperationException("Unsupported captain selection status.")
        };
    }

    [HttpPost("assign")]
    [ProducesResponseType(typeof(CaptainInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignManually(
        [FromRoute] Guid subjectId,
        [FromRoute] Guid teamId,
        [FromBody] AssignCaptainRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _captainService.AssignCaptainManuallyAsync(subjectId, teamId, request.CaptainUserId, userId.Value, cancellationToken);

        return result.Status switch
        {
            CaptainSelectionStatus.Success => Ok(result.Captain),
            CaptainSelectionStatus.NotFound => NotFound(CreateNotFound(result.ErrorMessage)),
            CaptainSelectionStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            CaptainSelectionStatus.InvalidOperation => BadRequest(CreateBadRequest(result.ErrorMessage)),
            _ => throw new InvalidOperationException("Unsupported captain assignment status.")
        };
    }

    [HttpGet]
    [ProducesResponseType(typeof(CaptainInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCaptainInfo(
        [FromRoute] Guid subjectId,
        [FromRoute] Guid teamId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _captainService.GetCaptainInfoAsync(teamId, cancellationToken);

        return result.Status switch
        {
            CaptainInfoStatus.Success => Ok(result.Captain),
            CaptainInfoStatus.NotFound => NotFound(CreateNotFound(result.ErrorMessage)),
            CaptainInfoStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            CaptainInfoStatus.NoCaptain => NotFound(CreateNotFound(result.ErrorMessage)),
            _ => throw new InvalidOperationException("Unsupported captain info status.")
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

    private static ProblemDetails CreateNotFound(string? message)
    {
        return new ProblemDetails
        {
            Title = "Not Found",
            Status = StatusCodes.Status404NotFound,
            Detail = message ?? "Resource not found."
        };
    }

    private static ProblemDetails CreateBadRequest(string? message)
    {
        return new ProblemDetails
        {
            Title = "Bad Request",
            Status = StatusCodes.Status400BadRequest,
            Detail = message ?? "Validation failed."
        };
    }
}
