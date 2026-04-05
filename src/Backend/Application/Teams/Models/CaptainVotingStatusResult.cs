namespace Application.Teams.Models;

public sealed record CaptainVotingStatusResult(
    CaptainVotingStatusStatus Status,
    CaptainVotingStatusResponse? Response,
    string? ErrorMessage)
{
    public static CaptainVotingStatusResult Success(CaptainVotingStatusResponse response)
    {
        return new CaptainVotingStatusResult(CaptainVotingStatusStatus.Success, response, null);
    }

    public static CaptainVotingStatusResult NotFound(string message)
    {
        return new CaptainVotingStatusResult(CaptainVotingStatusStatus.NotFound, null, message);
    }

    public static CaptainVotingStatusResult Forbidden()
    {
        return new CaptainVotingStatusResult(CaptainVotingStatusStatus.Forbidden, null, null);
    }

    public static CaptainVotingStatusResult NoActiveSession()
    {
        return new CaptainVotingStatusResult(CaptainVotingStatusStatus.NoActiveSession, null, "No active voting session for this team.");
    }
}
