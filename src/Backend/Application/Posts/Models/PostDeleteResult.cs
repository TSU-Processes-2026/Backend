namespace Application.Posts.Models;

public sealed record PostDeleteResult(PostDeleteStatus Status)
{
    public static PostDeleteResult Success()
    {
        return new PostDeleteResult(PostDeleteStatus.Success);
    }

    public static PostDeleteResult Forbidden()
    {
        return new PostDeleteResult(PostDeleteStatus.Forbidden);
    }
}
