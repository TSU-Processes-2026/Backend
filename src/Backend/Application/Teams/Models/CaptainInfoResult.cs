namespace Application.Teams.Models;

public sealed record CaptainInfoResult(
    CaptainInfoStatus Status,
    CaptainInfoResponse? Captain,
    string? ErrorMessage)
{
    public static CaptainInfoResult Success(CaptainInfoResponse captain)
    {
        return new CaptainInfoResult(CaptainInfoStatus.Success, captain, null);
    }

    public static CaptainInfoResult NotFound(string message)
    {
        return new CaptainInfoResult(CaptainInfoStatus.NotFound, null, message);
    }

    public static CaptainInfoResult Forbidden()
    {
        return new CaptainInfoResult(CaptainInfoStatus.Forbidden, null, null);
    }

    public static CaptainInfoResult NoCaptain()
    {
        return new CaptainInfoResult(CaptainInfoStatus.NoCaptain, null, "This team has no captain selected.");
    }
}
