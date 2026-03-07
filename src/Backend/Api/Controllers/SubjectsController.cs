using Api.Authentication;
using Application.Subjects.Contracts;
using Application.Subjects.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/subjects")]
public sealed class SubjectsController : ControllerBase
{
    private readonly ISubjectsService _subjectsService;

    public SubjectsController(ISubjectsService subjectsService)
    {
        _subjectsService = subjectsService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(SubjectResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateSubjectRequest? request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var created = await _subjectsService.CreateAsync(userId.Value, request ?? new CreateSubjectRequest(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SubjectResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetList([FromQuery] int limit = 20, [FromQuery] int offset = 0, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var subjects = await _subjectsService.GetListAsync(userId.Value, limit, offset, cancellationToken);
        return Ok(subjects);
    }

    [HttpGet("{subjectId:guid}")]
    [ProducesResponseType(typeof(SubjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid subjectId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _subjectsService.GetByIdAsync(userId.Value, subjectId, cancellationToken);

        return result.Status switch
        {
            SubjectAccessStatus.Success => Ok(result.Subject),
            SubjectAccessStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            SubjectAccessStatus.NotFound => NotFound(CreateNotFound()),
            _ => throw new InvalidOperationException("Unsupported subject get status.")
        };
    }

    [HttpPut("{subjectId:guid}")]
    [ProducesResponseType(typeof(SubjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] Guid subjectId, [FromBody] UpdateSubjectRequest? request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _subjectsService.UpdateAsync(userId.Value, subjectId, request ?? new UpdateSubjectRequest(), cancellationToken);

        return result.Status switch
        {
            SubjectAccessStatus.Success => Ok(result.Subject),
            SubjectAccessStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            SubjectAccessStatus.NotFound => NotFound(CreateNotFound()),
            _ => throw new InvalidOperationException("Unsupported subject update status.")
        };
    }

    [HttpDelete("{subjectId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid subjectId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _subjectsService.DeleteAsync(userId.Value, subjectId, cancellationToken);

        return result.Status switch
        {
            SubjectDeleteStatus.Success => NoContent(),
            SubjectDeleteStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            SubjectDeleteStatus.NotFound => NotFound(CreateNotFound()),
            _ => throw new InvalidOperationException("Unsupported subject delete status.")
        };
    }

    [HttpPost("{subjectId:guid}/join")]
    [ProducesResponseType(typeof(ParticipantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Join([FromRoute] Guid subjectId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _subjectsService.JoinAsync(userId.Value, subjectId, cancellationToken);

        return result.Status switch
        {
            JoinSubjectStatus.Success => Ok(result.Participant),
            JoinSubjectStatus.NotFound => NotFound(CreateNotFound()),
            _ => throw new InvalidOperationException("Unsupported join status.")
        };
    }

    [HttpPost("{subjectId:guid}/participants")]
    [ProducesResponseType(typeof(ParticipantResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddParticipant([FromRoute] Guid subjectId, [FromBody] AddParticipantRequest? request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _subjectsService.AddParticipantAsync(userId.Value, subjectId, request ?? new AddParticipantRequest(), cancellationToken);

        return result.Status switch
        {
            ParticipantMutationStatus.Success => StatusCode(StatusCodes.Status201Created, result.Participant),
            ParticipantMutationStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            _ => throw new InvalidOperationException("Unsupported add participant status.")
        };
    }

    [HttpGet("{subjectId:guid}/participants")]
    [ProducesResponseType(typeof(IReadOnlyList<ParticipantResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetParticipants([FromRoute] Guid subjectId, [FromQuery] int limit = 50, [FromQuery] int offset = 0, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _subjectsService.GetParticipantsAsync(userId.Value, subjectId, limit, offset, cancellationToken);

        return result.Status switch
        {
            ParticipantMutationStatus.Success => Ok(result.Participants),
            ParticipantMutationStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            _ => throw new InvalidOperationException("Unsupported get participants status.")
        };
    }

    [HttpPatch("{subjectId:guid}/participants/{userId:guid}")]
    [ProducesResponseType(typeof(ParticipantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateParticipantRole([FromRoute] Guid subjectId, [FromRoute] Guid userId, [FromBody] UpdateParticipantRoleRequest? request, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();

        if (currentUserId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _subjectsService.UpdateParticipantRoleAsync(currentUserId.Value, subjectId, userId, request ?? new UpdateParticipantRoleRequest(), cancellationToken);

        return result.Status switch
        {
            ParticipantMutationStatus.Success => Ok(result.Participant),
            ParticipantMutationStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            _ => throw new InvalidOperationException("Unsupported update participant status.")
        };
    }

    [HttpDelete("{subjectId:guid}/participants/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteParticipant([FromRoute] Guid subjectId, [FromRoute] Guid userId, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();

        if (currentUserId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _subjectsService.DeleteParticipantAsync(currentUserId.Value, subjectId, userId, cancellationToken);

        return result.Status switch
        {
            ParticipantDeleteStatus.Success => NoContent(),
            ParticipantDeleteStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            _ => throw new InvalidOperationException("Unsupported delete participant status.")
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

    private static ProblemDetails CreateNotFound()
    {
        return new ProblemDetails
        {
            Title = "Not Found",
            Status = StatusCodes.Status404NotFound,
            Detail = "Resource not found."
        };
    }
}
