using Api.Authentication;
using Application.Posts.Contracts;
using Application.Posts.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class PostsController : ControllerBase
{
    private const string AnnouncementPostType = "Announcement";
    private const string MaterialPostType = "Material";

    private readonly IPostsService _postsService;

    public PostsController(IPostsService postsService)
    {
        _postsService = postsService;
    }

    [HttpGet("subjects/{subjectId:guid}/posts")]
    [ProducesResponseType(typeof(IReadOnlyList<PostResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSubjectPosts(
        [FromRoute] Guid subjectId,
        [FromQuery] string? postType,
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _postsService.GetSubjectPostsAsync(userId.Value, subjectId, postType, limit, offset, cancellationToken);

        return result.Status switch
        {
            PostListStatus.Success => Ok(result.Posts),
            PostListStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            _ => throw new InvalidOperationException("Unsupported post list status.")
        };
    }

    [HttpPost("subjects/{subjectId:guid}/posts")]
    [ProducesResponseType(typeof(PostResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromRoute] Guid subjectId,
        [FromForm] CreatePostFormRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var postType = request?.PostType;

        if (!string.Equals(postType, AnnouncementPostType, StringComparison.Ordinal)
            && !string.Equals(postType, MaterialPostType, StringComparison.Ordinal))
        {
            return BadRequest(CreateBadRequest("Unsupported postType."));
        }

        if (string.Equals(postType, MaterialPostType, StringComparison.Ordinal) && request?.File is null)
        {
            return BadRequest(CreateBadRequest("File is required for Material posts."));
        }

        var fileName = request?.File?.FileName;
        var storagePath = request?.File is null
            ? null
            : $"subjects/{subjectId}/materials/{Guid.NewGuid():N}/{request.File.FileName}";

        var createRequest = new CreatePostRequest
        {
            PostType = postType,
            Content = request?.Content,
            FileName = fileName,
            StoragePath = storagePath,
            FileSize = request?.File?.Length,
            FileContent = request?.File?.OpenReadStream()
        };

        var result = await _postsService.CreateAsync(userId.Value, subjectId, createRequest, cancellationToken);

        return result.Status switch
        {
            PostUpdateStatus.Success => StatusCode(StatusCodes.Status201Created, result.Post),
            PostUpdateStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            _ => throw new InvalidOperationException("Unsupported post create status.")
        };
    }

    [HttpGet("posts/{postId:guid}")]
    [ProducesResponseType(typeof(PostResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid postId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _postsService.GetByIdAsync(userId.Value, postId, cancellationToken);

        return result.Status switch
        {
            PostAccessStatus.Success => Ok(result.Post),
            PostAccessStatus.NotFound => NotFound(CreateNotFound()),
            _ => throw new InvalidOperationException("Unsupported post access status.")
        };
    }

    [HttpGet("posts/{postId:guid}/file-info")]
    [ProducesResponseType(typeof(PostFileInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFileInfo([FromRoute] Guid postId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _postsService.GetFileInfoAsync(userId.Value, postId, cancellationToken);

        return result.Status switch
        {
            PostFileInfoStatus.Success => Ok(result.FileInfo),
            PostFileInfoStatus.NotFound => NotFound(CreateNotFound()),
            _ => throw new InvalidOperationException("Unsupported post file info status.")
        };
    }

    [HttpGet("posts/{postId:guid}/file")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadFile([FromRoute] Guid postId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _postsService.DownloadFileAsync(userId.Value, postId, cancellationToken);

        return result.Status switch
        {
            PostFileDownloadStatus.Success => File(result.File!.Content, result.File.ContentType, result.File.FileName),
            PostFileDownloadStatus.NotFound => NotFound(CreateNotFound()),
            _ => throw new InvalidOperationException("Unsupported post file download status.")
        };
    }

    [HttpPut("posts/{postId:guid}")]
    [ProducesResponseType(typeof(PostResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update([FromRoute] Guid postId, [FromBody] UpdatePostRequest? request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _postsService.UpdateAsync(userId.Value, postId, request ?? new UpdatePostRequest(), cancellationToken);

        return result.Status switch
        {
            PostUpdateStatus.Success => Ok(result.Post),
            PostUpdateStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            _ => throw new InvalidOperationException("Unsupported post update status.")
        };
    }

    [HttpDelete("posts/{postId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete([FromRoute] Guid postId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(CreateUnauthorized());
        }

        var result = await _postsService.DeleteAsync(userId.Value, postId, cancellationToken);

        return result.Status switch
        {
            PostDeleteStatus.Success => NoContent(),
            PostDeleteStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateForbidden()),
            _ => throw new InvalidOperationException("Unsupported post delete status.")
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

    private static ProblemDetails CreateBadRequest(string detail)
    {
        return new ProblemDetails
        {
            Title = "Bad Request",
            Status = StatusCodes.Status400BadRequest,
            Detail = detail
        };
    }
}

public sealed class CreatePostFormRequest
{
    public string? PostType { get; init; }
    public string? Content { get; init; }
    public IFormFile? File { get; init; }
}

