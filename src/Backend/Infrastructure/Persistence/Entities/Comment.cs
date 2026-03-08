namespace Infrastructure.Persistence.Entities;

public sealed class Comment
{
    public Guid Id { get; set; }
    public required string TargetType { get; set; }
    public Guid TargetId { get; set; }
    public Guid AuthorId { get; set; }
    public required string Text { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
