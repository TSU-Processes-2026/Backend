namespace Application.Posts.Models;

public sealed class CreatePostRequest
{
    public string? PostType { get; init; }
    public string? Content { get; init; }
    public string? FileName { get; init; }
    public string? StoragePath { get; init; }
    public long? FileSize { get; init; }
}
