namespace Application.Posts.Models;

public sealed class MaterialPostResponse : PostResponse
{
    public required string FileName { get; init; }
    public required string StoragePath { get; init; }
    public required long FileSize { get; init; }
}
