using Application.Comments.Contracts;
using Application.Comments.Models;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Comments.Services;

public sealed class CommentsService : ICommentsService
{
    private const string PostTargetType = "Post";
    private const string SubmissionTargetType = "Submission";
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
        var subjectId = await GetSubjectIdForTargetAsync(targetType, targetId, cancellationToken);

        if (subjectId is null)
        {
            return CommentListResult.Forbidden();
        }

        if (!await IsParticipantAsync(currentUserId, subjectId.Value, cancellationToken))
        {
            return CommentListResult.Forbidden();
        }

        var safeLimit = limit <= 0 ? 20 : limit;
        var safeOffset = offset < 0 ? 0 : offset;

        var comments = await _dbContext.Comments
            .Where(x => x.TargetType == targetType && x.TargetId == targetId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Skip(safeOffset)
            .Take(safeLimit)
            .ToListAsync(cancellationToken);

        return CommentListResult.Success(comments.Select(MapComment).ToList());
    }

    public async Task<CommentCreateResult> CreateAsync(Guid currentUserId, CreateCommentRequest request, CancellationToken cancellationToken)
    {
        var subjectId = await GetSubjectIdForTargetAsync(request.TargetType, request.TargetId, cancellationToken);

        if (subjectId is null)
        {
            return CommentCreateResult.Forbidden();
        }

        if (!await IsParticipantAsync(currentUserId, subjectId.Value, cancellationToken))
        {
            return CommentCreateResult.Forbidden();
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            TargetType = request.TargetType!,
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

        if (!string.Equals(comment.TargetType, PostTargetType, StringComparison.Ordinal)
            && !string.Equals(comment.TargetType, SubmissionTargetType, StringComparison.Ordinal))
        {
            return CommentDeleteResult.Forbidden();
        }

        var subjectId = await GetSubjectIdForTargetAsync(comment.TargetType, comment.TargetId, cancellationToken);

        if (subjectId is null)
        {
            return CommentDeleteResult.Forbidden();
        }

        if (!await IsTeacherOrAdminAsync(currentUserId, subjectId.Value, cancellationToken))
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

    private async Task<Guid?> GetSubjectIdForTargetAsync(string? targetType, Guid targetId, CancellationToken cancellationToken)
    {
        if (string.Equals(targetType, PostTargetType, StringComparison.Ordinal))
        {
            var post = await _dbContext.Posts
                .SingleOrDefaultAsync(x => x.Id == targetId, cancellationToken);

            return post?.SubjectId;
        }

        if (string.Equals(targetType, SubmissionTargetType, StringComparison.Ordinal))
        {
            var submission = await _dbContext.Submissions
                .SingleOrDefaultAsync(x => x.id == targetId, cancellationToken);

            if (submission is null)
            {
                return null;
            }

            var post = await _dbContext.Posts
                .SingleOrDefaultAsync(x => x.Id == submission.assignmentId, cancellationToken);

            return post?.SubjectId;
        }

        return null;
    }
}