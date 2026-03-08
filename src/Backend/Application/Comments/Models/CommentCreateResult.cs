namespace Application.Comments.Models;

public sealed record CommentCreateResult(CommentCreateStatus Status, CommentResponse? Comment)
{
    public static CommentCreateResult Success(CommentResponse comment)
    {
        return new CommentCreateResult(CommentCreateStatus.Success, comment);
    }

    public static CommentCreateResult Forbidden()
    {
        return new CommentCreateResult(CommentCreateStatus.Forbidden, null);
    }
}
