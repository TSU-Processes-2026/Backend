namespace Application.Posts.Models;

public sealed class PostFileDownloadPayload
{
    public required Stream Content { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
}
