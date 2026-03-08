using Application.Comments.Contracts;
using Application.Comments.Models;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Comments.Services;

public sealed class CommentsService : ICommentsService
{
    private const string PostTargetType = "Post";
    private const string TeacherRole = "Teacher";
    private const string AdminRole = "Admin";

    private readonly LmsDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public CommentsService(LmsDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<CommentListResult> GetByTargetAsync(Guid currentUserId, string? targetType, Guid targetId, int limit, int offset, CancellationToken cancellationToken)
    {
        if (!string.Equals(targetType, PostTargetType, StringComparison.Ordinal))
        {
            return CommentListResult.Forbidden();
        }

        var post = await _dbContext.Posts
            .SingleOrDefaultAsync(x => x.Id == targetId, cancellationToken);

        if (post is null)
        {
            return CommentListResult.Forbidden();
        }

        if (!await IsParticipantAsync(currentUserId, post.SubjectId, cancellationToken))
        {
            return CommentListResult.Forbidden();
        }

        var safeLimit = limit <= 0 ? 20 : limit;
        var safeOffset = offset < 0 ? 0 : offset;

        var comments = await _dbContext.Comments
            .Where(x => x.TargetType == PostTargetType && x.TargetId == targetId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Skip(safeOffset)
            .Take(safeLimit)
            .ToListAsync(cancellationToken);

        return CommentListResult.Success(comments.Select(MapComment).ToList());
    }

    public async Task<CommentCreateResult> CreateAsync(Guid currentUserId, CreateCommentRequest request, CancellationToken cancellationToken)
    {
        if (!string.Equals(request.TargetType, PostTargetType, StringComparison.Ordinal))
        {
            return CommentCreateResult.Forbidden();
        }

        var post = await _dbContext.Posts
            .SingleOrDefaultAsync(x => x.Id == request.TargetId, cancellationToken);

        if (post is null)
        {
            return CommentCreateResult.Forbidden();
        }

        if (!await IsParticipantAsync(currentUserId, post.SubjectId, cancellationToken))
        {
            return CommentCreateResult.Forbidden();
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            TargetType = PostTargetType,
            TargetId = request.TargetId,
            AuthorId = currentUserId,
            Text = request.Text ?? string.Empty,
            CreatedAt = _timeProvider.GetUtcNow()
        };

        _dbContext.Comments.Add(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CommentCreateResult.Success(MapComment(comment));
    }

    public async Task<CommentDeleteResult> DeleteAsync(Guid currentUserId, Guid commentId, CancellationToken cancellationToken)
    {
        var comment = await _dbContext.Comments
            .SingleOrDefaultAsync(x => x.Id == commentId, cancellationToken);

        if (comment is null)
        {
            return CommentDeleteResult.Forbidden();
        }

        if (comment.AuthorId == currentUserId)
        {
            _dbContext.Comments.Remove(comment);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return CommentDeleteResult.Success();
        }

        if (!string.Equals(comment.TargetType, PostTargetType, StringComparison.Ordinal))
        {
            return CommentDeleteResult.Forbidden();
        }

        var post = await _dbContext.Posts
            .SingleOrDefaultAsync(x => x.Id == comment.TargetId, cancellationToken);

        if (post is null)
        {
            return CommentDeleteResult.Forbidden();
        }

        if (!await IsTeacherOrAdminAsync(currentUserId, post.SubjectId, cancellationToken))
        {
            return CommentDeleteResult.Forbidden();
        }

        _dbContext.Comments.Remove(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CommentDeleteResult.Success();
    }

    private async Task<bool> IsParticipantAsync(Guid userId, Guid subjectId, CancellationToken cancellationToken)
    {
        return await _dbContext.SubjectParticipants
            .AnyAsync(x => x.SubjectId == subjectId && x.UserId == userId, cancellationToken);
    }

    private async Task<bool> IsTeacherOrAdminAsync(Guid userId, Guid subjectId, CancellationToken cancellationToken)
    {
        return await _dbContext.SubjectParticipants
            .AnyAsync(
                x => x.SubjectId == subjectId
                     && x.UserId == userId
                     && (x.Role == TeacherRole || x.Role == AdminRole),
                cancellationToken);
    }

    private static CommentResponse MapComment(Comment comment)
    {
        return new CommentResponse
        {
            Id = comment.Id,
            TargetType = comment.TargetType,
            TargetId = comment.TargetId,
            AuthorId = comment.AuthorId,
            Text = comment.Text,
            CreatedAt = comment.CreatedAt
        };
    }
}
