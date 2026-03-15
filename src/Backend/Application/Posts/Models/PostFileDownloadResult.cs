namespace Application.Posts.Models;

public sealed record PostFileDownloadResult(PostFileDownloadStatus Status, PostFileDownloadPayload? File)
{
    public static PostFileDownloadResult Success(PostFileDownloadPayload file)
    {
        return new PostFileDownloadResult(PostFileDownloadStatus.Success, file);
    }

    public static PostFileDownloadResult NotFound()
    {
        return new PostFileDownloadResult(PostFileDownloadStatus.NotFound, null);
    }
}
