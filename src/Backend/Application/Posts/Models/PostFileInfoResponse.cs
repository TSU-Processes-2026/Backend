namespace Application.Posts.Models;

public sealed class PostFileInfoResponse
{
    public required string FileName { get; init; }
    public required long FileSize { get; init; }
    public required string DownloadUrl { get; init; }
}
