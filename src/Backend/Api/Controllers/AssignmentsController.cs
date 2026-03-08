using Api.Authentication;
using Application.Assignments.Contracts;
using Application.Assignments.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class AssignmentsController : ControllerBase
{
    private readonly IAssignmentsService _assignmentsService;

    public AssignmentsController(IAssignmentsService assignmentsService)
    {
        _assignmentsService = assignmentsService;
    }

    [HttpGet("subjects/{subjectId:guid}/assignments")]
    [ProducesResponseType(typeof(IReadOnlyList<AssignmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSubjectAssignments([FromRoute] Guid subjectId, [FromQuery] int limit = 20, [FromQuery] int offset = 0, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _assignmentsService.GetSubjectAssignmentsAsync(userId.Value, subjectId, limit, offset, cancellationToken);

        return result.Status switch
        {
            AssignmentListStatus.Success => Ok(result.Assignments),
            AssignmentListStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            _ => throw new InvalidOperationException("Unsupported assignments list status.")
        };
    }

    [HttpPost("subjects/{subjectId:guid}/assignments")]
    [ProducesResponseType(typeof(AssignmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromRoute] Guid subjectId, [FromBody] UpsertAssignmentRequest? request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _assignmentsService.CreateAsync(userId.Value, subjectId, request ?? new UpsertAssignmentRequest(), cancellationToken);

        return result.Status switch
        {
            AssignmentUpdateStatus.Success => StatusCode(StatusCodes.Status201Created, result.Assignment),
            AssignmentUpdateStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            _ => throw new InvalidOperationException("Unsupported assignments create status.")
        };
    }

    [HttpGet("assignments/{assignmentId:guid}")]
    [ProducesResponseType(typeof(AssignmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById([FromRoute] Guid assignmentId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _assignmentsService.GetByIdAsync(userId.Value, assignmentId, cancellationToken);

        return result.Status switch
        {
            AssignmentAccessStatus.Success => Ok(result.Assignment),
            AssignmentAccessStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            _ => throw new InvalidOperationException("Unsupported assignments get status.")
        };
    }

    [HttpPut("assignments/{assignmentId:guid}")]
    [ProducesResponseType(typeof(AssignmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update([FromRoute] Guid assignmentId, [FromBody] UpsertAssignmentRequest? request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _assignmentsService.UpdateAsync(userId.Value, assignmentId, request ?? new UpsertAssignmentRequest(), cancellationToken);

        return result.Status switch
        {
            AssignmentUpdateStatus.Success => Ok(result.Assignment),
            AssignmentUpdateStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            _ => throw new InvalidOperationException("Unsupported assignments update status.")
        };
    }

    [HttpDelete("assignments/{assignmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete([FromRoute] Guid assignmentId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _assignmentsService.DeleteAsync(userId.Value, assignmentId, cancellationToken);

        return result.Status switch
        {
            AssignmentDeleteStatus.Success => NoContent(),
            AssignmentDeleteStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            _ => throw new InvalidOperationException("Unsupported assignments delete status.")
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
}
