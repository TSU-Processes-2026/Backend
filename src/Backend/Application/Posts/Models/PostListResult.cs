namespace Application.Posts.Models;

public sealed record PostListResult(PostListStatus Status, IReadOnlyList<PostResponse> Posts)
{
    public static PostListResult Success(IReadOnlyList<PostResponse> posts)
    {
        return new PostListResult(PostListStatus.Success, posts);
    }

    public static PostListResult Forbidden()
    {
        return new PostListResult(PostListStatus.Forbidden, Array.Empty<PostResponse>());
    }
}
