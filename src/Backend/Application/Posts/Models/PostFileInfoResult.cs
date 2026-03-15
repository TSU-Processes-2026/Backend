namespace Application.Posts.Models;

public sealed record PostFileInfoResult(PostFileInfoStatus Status, PostFileInfoResponse? FileInfo)
{
    public static PostFileInfoResult Success(PostFileInfoResponse fileInfo)
    {
        return new PostFileInfoResult(PostFileInfoStatus.Success, fileInfo);
    }

    public static PostFileInfoResult NotFound()
    {
        return new PostFileInfoResult(PostFileInfoStatus.NotFound, null);
    }
}
