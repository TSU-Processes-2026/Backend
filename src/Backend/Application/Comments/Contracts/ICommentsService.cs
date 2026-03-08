using Application.Comments.Models;

namespace Application.Comments.Contracts;

public interface ICommentsService
{
    Task<CommentListResult> GetByTargetAsync(Guid currentUserId, string? targetType, Guid targetId, int limit, int offset, CancellationToken cancellationToken);
    Task<CommentCreateResult> CreateAsync(Guid currentUserId, CreateCommentRequest request, CancellationToken cancellationToken);
    Task<CommentDeleteResult> DeleteAsync(Guid currentUserId, Guid commentId, CancellationToken cancellationToken);
}
