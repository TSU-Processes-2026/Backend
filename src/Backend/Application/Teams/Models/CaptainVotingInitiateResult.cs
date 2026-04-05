namespace Application.Teams.Models;

public sealed record CaptainVotingInitiateResult(
    CaptainVotingInitiateStatus Status,
    CaptainVotingSessionResponse? Session,
    string? ErrorMessage)
{
    public static CaptainVotingInitiateResult Success(CaptainVotingSessionResponse session)
    {
        return new CaptainVotingInitiateResult(CaptainVotingInitiateStatus.Success, session, null);
    }

    public static CaptainVotingInitiateResult NotFound(string message)
    {
        return new CaptainVotingInitiateResult(CaptainVotingInitiateStatus.NotFound, null, message);
    }

    public static CaptainVotingInitiateResult Forbidden()
    {
        return new CaptainVotingInitiateResult(CaptainVotingInitiateStatus.Forbidden, null, null);
    }

    public static CaptainVotingInitiateResult InvalidOperation(string message)
    {
        return new CaptainVotingInitiateResult(CaptainVotingInitiateStatus.InvalidOperation, null, message);
    }

    public static CaptainVotingInitiateResult AlreadyActive(CaptainVotingSessionResponse session)
    {
        return new CaptainVotingInitiateResult(CaptainVotingInitiateStatus.AlreadyActive, session, "A voting session is already active for this team.");
    }
}
