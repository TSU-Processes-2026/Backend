namespace Application.Posts.Models;

public sealed record PostAccessResult(PostAccessStatus Status, PostResponse? Post)
{
    public static PostAccessResult Success(PostResponse post)
    {
        return new PostAccessResult(PostAccessStatus.Success, post);
    }

    public static PostAccessResult NotFound()
    {
        return new PostAccessResult(PostAccessStatus.NotFound, null);
    }
}
