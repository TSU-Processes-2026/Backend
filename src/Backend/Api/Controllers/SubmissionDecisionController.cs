using Api.Authentication;
using Application.Submissions.Contracts;
using Application.Submissions.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/submissions/{submissionId:guid}/decision")]
public sealed class SubmissionDecisionController : ControllerBase
{
    private readonly ISubmissionDecisionService _decisionService;

    public SubmissionDecisionController(ISubmissionDecisionService decisionService)
    {
        _decisionService = decisionService;
    }

    [HttpPost("initiate")]
    [ProducesResponseType(typeof(DecisionSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DecisionSessionResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> InitiateDecision(
        [FromRoute] Guid submissionId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _decisionService.InitiateDecisionAsync(submissionId, userId.Value, cancellationToken);

        return result.Status switch
        {
            DecisionSessionInitiateStatus.Success => Ok(result.Session),
            DecisionSessionInitiateStatus.AlreadyActive => StatusCode(StatusCodes.Status409Conflict, result.Session),
            DecisionSessionInitiateStatus.NotFound => NotFound(CreateNotFound(result.ErrorMessage)),
            DecisionSessionInitiateStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            DecisionSessionInitiateStatus.InvalidOperation => BadRequest(CreateBadRequest(result.ErrorMessage)),
            DecisionSessionInitiateStatus.NoCaptain => BadRequest(CreateBadRequest(result.ErrorMessage)),
            _ => throw new InvalidOperationException("Unsupported decision session initiate status.")
        };
    }

    [HttpPost("vote")]
    [ProducesResponseType(typeof(DecisionVoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CastVote(
        [FromRoute] Guid submissionId,
        [FromBody] DecisionVoteRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _decisionService.CastDecisionVoteAsync(
            submissionId,
            userId.Value,
            request.Decision,
            request.Comment,
            cancellationToken);

        return result.Status switch
        {
            DecisionVoteStatus.Success => Ok(new DecisionVoteResponse(result.SessionCompleted, result.FinalResult)),
            DecisionVoteStatus.NotFound => NotFound(CreateNotFound(result.ErrorMessage)),
            DecisionVoteStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            DecisionVoteStatus.InvalidOperation => BadRequest(CreateBadRequest(result.ErrorMessage)),
            DecisionVoteStatus.AlreadyVoted => BadRequest(CreateBadRequest(result.ErrorMessage)),
            DecisionVoteStatus.SessionClosed => BadRequest(CreateBadRequest(result.ErrorMessage)),
            DecisionVoteStatus.WrongDecisionMode => BadRequest(CreateBadRequest(result.ErrorMessage)),
            _ => throw new InvalidOperationException("Unsupported decision vote status.")
        };
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(DecisionSessionStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(
        [FromRoute] Guid submissionId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _decisionService.GetDecisionStatusAsync(submissionId, userId.Value, cancellationToken);

        return result.Status switch
        {
            DecisionSessionStatusStatus.Success => Ok(result.Response),
            DecisionSessionStatusStatus.NotFound => NotFound(CreateNotFound(result.ErrorMessage)),
            DecisionSessionStatusStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            DecisionSessionStatusStatus.NoActiveSession => NotFound(CreateNotFound(result.ErrorMessage)),
            _ => throw new InvalidOperationException("Unsupported decision session status status.")
        };
    }

    [HttpGet("votes")]
    [ProducesResponseType(typeof(DecisionVoteTallyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVoteTally(
        [FromRoute] Guid submissionId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _decisionService.GetVoteTallyAsync(submissionId, userId.Value, cancellationToken);

        return result.Status switch
        {
            DecisionVoteTallyStatus.Success => Ok(result.Tally),
            DecisionVoteTallyStatus.NotFound => NotFound(CreateNotFound(result.ErrorMessage)),
            DecisionVoteTallyStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            DecisionVoteTallyStatus.NoActiveSession => NotFound(CreateNotFound(result.ErrorMessage)),
            _ => throw new InvalidOperationException("Unsupported decision vote tally status.")
        };
    }

    [HttpPost("captain-approve")]
    [ProducesResponseType(typeof(DecisionSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CaptainApprove(
        [FromRoute] Guid submissionId,
        [FromBody] CaptainDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _decisionService.CaptainApproveAsync(
            submissionId,
            userId.Value,
            request.Comment,
            cancellationToken);

        return result.Status switch
        {
            CaptainDecisionStatus.Success => Ok(result.Session),
            CaptainDecisionStatus.NotFound => NotFound(CreateNotFound(result.ErrorMessage)),
            CaptainDecisionStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            CaptainDecisionStatus.InvalidOperation => BadRequest(CreateBadRequest(result.ErrorMessage)),
            CaptainDecisionStatus.SessionClosed => BadRequest(CreateBadRequest(result.ErrorMessage)),
            CaptainDecisionStatus.WrongDecisionMode => BadRequest(CreateBadRequest(result.ErrorMessage)),
            CaptainDecisionStatus.NotCaptain => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            _ => throw new InvalidOperationException("Unsupported captain decision status.")
        };
    }

    [HttpPost("captain-reject")]
    [ProducesResponseType(typeof(DecisionSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CaptainReject(
        [FromRoute] Guid submissionId,
        [FromBody] CaptainDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _decisionService.CaptainRejectAsync(
            submissionId,
            userId.Value,
            request.Comment,
            cancellationToken);

        return result.Status switch
        {
            CaptainDecisionStatus.Success => Ok(result.Session),
            CaptainDecisionStatus.NotFound => NotFound(CreateNotFound(result.ErrorMessage)),
            CaptainDecisionStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            CaptainDecisionStatus.InvalidOperation => BadRequest(CreateBadRequest(result.ErrorMessage)),
            CaptainDecisionStatus.SessionClosed => BadRequest(CreateBadRequest(result.ErrorMessage)),
            CaptainDecisionStatus.WrongDecisionMode => BadRequest(CreateBadRequest(result.ErrorMessage)),
            CaptainDecisionStatus.NotCaptain => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            _ => throw new InvalidOperationException("Unsupported captain decision status.")
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
