namespace Application.Comments.Models;

public sealed record CommentDeleteResult(CommentDeleteStatus Status)
{
    public static CommentDeleteResult Success()
    {
        return new CommentDeleteResult(CommentDeleteStatus.Success);
    }

    public static CommentDeleteResult Forbidden()
    {
        return new CommentDeleteResult(CommentDeleteStatus.Forbidden);
    }
}
