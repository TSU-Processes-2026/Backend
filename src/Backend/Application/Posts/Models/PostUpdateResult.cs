namespace Application.Posts.Models;

public sealed record PostUpdateResult(PostUpdateStatus Status, PostResponse? Post)
{
    public static PostUpdateResult Success(PostResponse post)
    {
        return new PostUpdateResult(PostUpdateStatus.Success, post);
    }

    public static PostUpdateResult Forbidden()
    {
        return new PostUpdateResult(PostUpdateStatus.Forbidden, null);
    }
}

