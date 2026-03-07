namespace Infrastructure.Persistence.Entities;

public sealed class Post
{
    public Guid Id { get; set; }
    public Guid SubjectId { get; set; }
    public Guid AuthorId { get; set; }
    public required string PostType { get; set; }
    public required string Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? FileName { get; set; }
    public string? StoragePath { get; set; }
    public long? FileSize { get; set; }
    public required Subject Subject { get; set; }
}
