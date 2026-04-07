namespace Application.Submissions.Models;

public sealed record DecisionVoteResult(
    DecisionVoteStatus Status,
    bool SessionCompleted,
    DecisionResult? FinalResult,
    string? ErrorMessage)
{
    public static DecisionVoteResult Success(bool sessionCompleted = false, DecisionResult? finalResult = null)
    {
        return new DecisionVoteResult(DecisionVoteStatus.Success, sessionCompleted, finalResult, null);
    }

    public static DecisionVoteResult NotFound(string message)
    {
        return new DecisionVoteResult(DecisionVoteStatus.NotFound, false, null, message);
    }

    public static DecisionVoteResult Forbidden()
    {
        return new DecisionVoteResult(DecisionVoteStatus.Forbidden, false, null, null);
    }

    public static DecisionVoteResult InvalidOperation(string message)
    {
        return new DecisionVoteResult(DecisionVoteStatus.InvalidOperation, false, null, message);
    }

    public static DecisionVoteResult AlreadyVoted()
    {
        return new DecisionVoteResult(DecisionVoteStatus.AlreadyVoted, false, null, "You have already submitted a decision in this session.");
    }

    public static DecisionVoteResult SessionClosed()
    {
        return new DecisionVoteResult(DecisionVoteStatus.SessionClosed, false, null, "The decision session has been closed.");
    }

    public static DecisionVoteResult WrongDecisionMode()
    {
        return new DecisionVoteResult(DecisionVoteStatus.WrongDecisionMode, false, null, "Voting is not allowed in captain-decides mode.");
    }
}
