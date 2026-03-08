namespace Application.Comments.Models;

public sealed record CommentListResult(CommentListStatus Status, IReadOnlyList<CommentResponse> Comments)
{
    public static CommentListResult Success(IReadOnlyList<CommentResponse> comments)
    {
        return new CommentListResult(CommentListStatus.Success, comments);
    }

    public static CommentListResult Forbidden()
    {
        return new CommentListResult(CommentListStatus.Forbidden, Array.Empty<CommentResponse>());
    }
}
