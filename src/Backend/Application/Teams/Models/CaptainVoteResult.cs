namespace Application.Teams.Models;

public sealed record CaptainVoteResult(
    CaptainVoteStatus Status,
    bool SessionCompleted,
    Guid? SelectedCaptainId,
    string? ErrorMessage)
{
    public static CaptainVoteResult Success(bool sessionCompleted = false, Guid? selectedCaptainId = null)
    {
        return new CaptainVoteResult(CaptainVoteStatus.Success, sessionCompleted, selectedCaptainId, null);
    }

    public static CaptainVoteResult NotFound(string message)
    {
        return new CaptainVoteResult(CaptainVoteStatus.NotFound, false, null, message);
    }

    public static CaptainVoteResult Forbidden()
    {
        return new CaptainVoteResult(CaptainVoteStatus.Forbidden, false, null, null);
    }

    public static CaptainVoteResult InvalidOperation(string message)
    {
        return new CaptainVoteResult(CaptainVoteStatus.InvalidOperation, false, null, message);
    }

    public static CaptainVoteResult AlreadyVoted()
    {
        return new CaptainVoteResult(CaptainVoteStatus.AlreadyVoted, false, null, "You have already voted in this session.");
    }

    public static CaptainVoteResult SessionClosed()
    {
        return new CaptainVoteResult(CaptainVoteStatus.SessionClosed, false, null, "The voting session has been closed.");
    }

    public static CaptainVoteResult InvalidCandidate()
    {
        return new CaptainVoteResult(CaptainVoteStatus.InvalidCandidate, false, null, "The voted-for user is not a member of this team.");
    }
}
