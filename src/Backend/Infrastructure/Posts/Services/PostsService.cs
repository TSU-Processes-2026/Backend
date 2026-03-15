using Application.Posts.Contracts;
using Application.Posts.Models;
using Infrastructure.Files.Contracts;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Posts.Services;

public sealed class PostsService : IPostsService
{
    private const string AnnouncementPostType = "Announcement";
    private const string MaterialPostType = "Material";
    private const string AssignmentPostType = "Assignment";
    private const string TeacherRole = "Teacher";
    private const string AdminRole = "Admin";

    private readonly LmsDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly IFileStorage _fileStorage;

    public PostsService(LmsDbContext dbContext, TimeProvider timeProvider, IFileStorage fileStorage)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _fileStorage = fileStorage;
    }

    public async Task<PostListResult> GetSubjectPostsAsync(Guid currentUserId, Guid subjectId, string? postType, int limit, int offset, CancellationToken cancellationToken)
    {
        if (!await IsParticipantAsync(currentUserId, subjectId, cancellationToken))
        {
            return PostListResult.Forbidden();
        }

        var safeLimit = limit <= 0 ? 20 : limit;
        var safeOffset = offset < 0 ? 0 : offset;
        var normalizedPostType = NormalizePostTypeForList(postType);

        var query = _dbContext.Posts
            .Where(x => x.SubjectId == subjectId)
            .Include(x => x.Questions)
            .ThenInclude(x => x.Options)
            .AsQueryable();

        if (normalizedPostType is not null)
        {
            query = query.Where(x => x.PostType == normalizedPostType);
        }

        var posts = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Skip(safeOffset)
            .Take(safeLimit)
            .ToListAsync(cancellationToken);

        return PostListResult.Success(posts.Select(MapPost).ToList());
    }

    public async Task<PostAccessResult> GetByIdAsync(Guid currentUserId, Guid postId, CancellationToken cancellationToken)
    {
        var post = await _dbContext.Posts
            .Include(x => x.Questions)
            .ThenInclude(x => x.Options)
            .SingleOrDefaultAsync(x => x.Id == postId, cancellationToken);

        if (post is null)
        {
            return PostAccessResult.NotFound();
        }

        if (!await IsParticipantAsync(currentUserId, post.SubjectId, cancellationToken))
        {
            return PostAccessResult.NotFound();
        }

        return PostAccessResult.Success(MapPost(post));
    }

    public async Task<PostUpdateResult> CreateAsync(Guid currentUserId, Guid subjectId, CreatePostRequest request, CancellationToken cancellationToken)
    {
        if (!await IsParticipantAsync(currentUserId, subjectId, cancellationToken))
        {
            return PostUpdateResult.Forbidden();
        }

        var normalizedPostType = NormalizePostTypeForCreate(request.PostType);

        if (normalizedPostType is null)
        {
            return PostUpdateResult.Forbidden();
        }

        var postId = Guid.NewGuid();
        var fileName = normalizedPostType == MaterialPostType ? request.FileName : null;
        var storagePath = normalizedPostType == MaterialPostType ? request.StoragePath : null;
        var fileSize = normalizedPostType == MaterialPostType ? request.FileSize : null;
        var fileContent = normalizedPostType == MaterialPostType ? request.FileContent : null;
        var needsFile = normalizedPostType == MaterialPostType && !string.IsNullOrWhiteSpace(storagePath) && fileContent is not null;

        var post = new Post
        {
            Id = postId,
            SubjectId = subjectId,
            AuthorId = currentUserId,
            PostType = normalizedPostType,
            Content = request.Content ?? string.Empty,
            CreatedAt = _timeProvider.GetUtcNow(),
            FileName = fileName,
            StoragePath = storagePath,
            FileSize = fileSize,
            Subject = await _dbContext.Subjects.SingleAsync(x => x.Id == subjectId, cancellationToken)
        };

        if (needsFile)
        {
            await using var content = fileContent!;
            await _fileStorage.SaveAsync(storagePath!, content, cancellationToken);
        }

        try
        {
            _dbContext.Posts.Add(post);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            if (needsFile)
            {
                await _fileStorage.DeleteAsync(storagePath!, cancellationToken);
            }

            throw;
        }

        return PostUpdateResult.Success(MapPost(post));
    }

    public async Task<PostUpdateResult> UpdateAsync(Guid currentUserId, Guid postId, UpdatePostRequest request, CancellationToken cancellationToken)
    {
        var post = await _dbContext.Posts
            .Include(x => x.Questions)
            .ThenInclude(x => x.Options)
            .SingleOrDefaultAsync(x => x.Id == postId, cancellationToken);

        if (post is null)
        {
            return PostUpdateResult.Forbidden();
        }

        if (!await CanManagePostAsync(currentUserId, post, cancellationToken))
        {
            return PostUpdateResult.Forbidden();
        }

        post.Content = request.Content ?? post.Content;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return PostUpdateResult.Success(MapPost(post));
    }

    public async Task<PostDeleteResult> DeleteAsync(Guid currentUserId, Guid postId, CancellationToken cancellationToken)
    {
        var post = await _dbContext.Posts
            .SingleOrDefaultAsync(x => x.Id == postId, cancellationToken);

        if (post is null)
        {
            return PostDeleteResult.Forbidden();
        }

        if (!await CanManagePostAsync(currentUserId, post, cancellationToken))
        {
            return PostDeleteResult.Forbidden();
        }

        var storagePath = post.StoragePath;

        _dbContext.Posts.Remove(post);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (post.PostType == MaterialPostType && !string.IsNullOrWhiteSpace(storagePath))
        {
            await _fileStorage.DeleteAsync(storagePath, cancellationToken);
        }

        return PostDeleteResult.Success();
    }

    public async Task<PostFileInfoResult> GetFileInfoAsync(Guid currentUserId, Guid postId, CancellationToken cancellationToken)
    {
        var post = await _dbContext.Posts
            .SingleOrDefaultAsync(x => x.Id == postId, cancellationToken);

        if (post is null)
        {
            return PostFileInfoResult.NotFound();
        }

        if (!await IsParticipantAsync(currentUserId, post.SubjectId, cancellationToken))
        {
            return PostFileInfoResult.NotFound();
        }

        if (post.PostType != MaterialPostType || string.IsNullOrWhiteSpace(post.FileName) || post.FileSize is null || string.IsNullOrWhiteSpace(post.StoragePath))
        {
            return PostFileInfoResult.NotFound();
        }

        var fileInfo = new PostFileInfoResponse
        {
            FileName = post.FileName,
            FileSize = post.FileSize.Value,
            DownloadUrl = $"/api/posts/{post.Id}/file"
        };

        return PostFileInfoResult.Success(fileInfo);
    }

    public async Task<PostFileDownloadResult> DownloadFileAsync(Guid currentUserId, Guid postId, CancellationToken cancellationToken)
    {
        var post = await _dbContext.Posts
            .SingleOrDefaultAsync(x => x.Id == postId, cancellationToken);

        if (post is null)
        {
            return PostFileDownloadResult.NotFound();
        }

        if (!await IsParticipantAsync(currentUserId, post.SubjectId, cancellationToken))
        {
            return PostFileDownloadResult.NotFound();
        }

        if (post.PostType != MaterialPostType || string.IsNullOrWhiteSpace(post.FileName) || string.IsNullOrWhiteSpace(post.StoragePath))
        {
            return PostFileDownloadResult.NotFound();
        }

        var stream = await _fileStorage.OpenReadAsync(post.StoragePath, cancellationToken);
        if (stream is null)
        {
            return PostFileDownloadResult.NotFound();
        }

        var payload = new PostFileDownloadPayload
        {
            Content = stream,
            FileName = post.FileName,
            ContentType = "application/octet-stream"
        };

        return PostFileDownloadResult.Success(payload);
    }

    private async Task<bool> IsParticipantAsync(Guid userId, Guid subjectId, CancellationToken cancellationToken)
    {
        return await _dbContext.SubjectParticipants
            .AnyAsync(x => x.SubjectId == subjectId && x.UserId == userId, cancellationToken);
    }

    private async Task<bool> CanManagePostAsync(Guid currentUserId, Post post, CancellationToken cancellationToken)
    {
        if (post.AuthorId == currentUserId)
        {
            return true;
        }

        return await _dbContext.SubjectParticipants
            .AnyAsync(
                x => x.SubjectId == post.SubjectId
                     && x.UserId == currentUserId
                     && (x.Role == TeacherRole || x.Role == AdminRole),
                cancellationToken);
    }

    private static string? NormalizePostTypeForList(string? postType)
    {
        if (string.Equals(postType, AnnouncementPostType, StringComparison.Ordinal))
        {
            return AnnouncementPostType;
        }

        if (string.Equals(postType, MaterialPostType, StringComparison.Ordinal))
        {
            return MaterialPostType;
        }

        if (string.Equals(postType, AssignmentPostType, StringComparison.Ordinal))
        {
            return AssignmentPostType;
        }

        return null;
    }

    private static string? NormalizePostTypeForCreate(string? postType)
    {
        if (string.Equals(postType, AnnouncementPostType, StringComparison.Ordinal))
        {
            return AnnouncementPostType;
        }

        if (string.Equals(postType, MaterialPostType, StringComparison.Ordinal))
        {
            return MaterialPostType;
        }

        return null;
    }

    private static PostResponse MapPost(Post post)
    {
        if (post.PostType == AnnouncementPostType)
        {
            return new AnnouncementPostResponse
            {
                Id = post.Id,
                AuthorId = post.AuthorId,
                PostType = AnnouncementPostType,
                Content = post.Content,
                CreatedAt = post.CreatedAt
            };
        }

        if (post.PostType == MaterialPostType)
        {
            return new MaterialPostResponse
            {
                Id = post.Id,
                AuthorId = post.AuthorId,
                PostType = MaterialPostType,
                Content = post.Content,
                CreatedAt = post.CreatedAt,
                FileName = post.FileName ?? string.Empty,
                StoragePath = post.StoragePath ?? string.Empty,
                FileSize = post.FileSize ?? 0,
                DownloadUrl = $"/api/posts/{post.Id}/file"
            };
        }

        if (post.PostType == AssignmentPostType)
        {
            return new AssignmentPostResponse
            {
                Id = post.Id,
                SubjectId = post.SubjectId,
                AuthorId = post.AuthorId,
                PostType = AssignmentPostType,
                Content = post.Content,
                CreatedAt = post.CreatedAt,
                AssignmentData = post.AssignmentData ?? string.Empty,
                Questions = post.Questions
                    .OrderBy(x => x.Id)
                    .Select(x => new AssignmentPostQuestionResponse
                    {
                        Id = x.Id,
                        QuestionType = x.QuestionType,
                        QuestionData = x.QuestionData,
                        Options = x.Options
                            .OrderBy(y => y.Id)
                            .Select(y => new AssignmentPostQuestionOptionResponse
                            {
                                Id = y.Id,
                                Text = y.Text
                            })
                            .ToList()
                    })
                    .ToList()
            };
        }

        throw new InvalidOperationException("Unsupported post type.");
    }
}
