using Application.Posts.Models;

namespace Application.Posts.Contracts;

public interface IPostsService
{
    Task<PostListResult> GetSubjectPostsAsync(Guid currentUserId, Guid subjectId, string? postType, int limit, int offset, CancellationToken cancellationToken);
    Task<PostAccessResult> GetByIdAsync(Guid currentUserId, Guid postId, CancellationToken cancellationToken);
    Task<PostUpdateResult> CreateAsync(Guid currentUserId, Guid subjectId, CreatePostRequest request, CancellationToken cancellationToken);
    Task<PostUpdateResult> UpdateAsync(Guid currentUserId, Guid postId, UpdatePostRequest request, CancellationToken cancellationToken);
    Task<PostDeleteResult> DeleteAsync(Guid currentUserId, Guid postId, CancellationToken cancellationToken);
}

