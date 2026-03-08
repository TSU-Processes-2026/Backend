namespace Application.Comments.Models;

public sealed class CommentResponse
{
    public required Guid Id { get; init; }
    public required string TargetType { get; init; }
    public required Guid TargetId { get; init; }
    public required Guid AuthorId { get; init; }
    public required string Text { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
