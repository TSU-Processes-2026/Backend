namespace Application.Comments.Models;

public sealed class CreateCommentRequest
{
    public string? TargetType { get; init; }
    public Guid TargetId { get; init; }
    public string? Text { get; init; }
}
