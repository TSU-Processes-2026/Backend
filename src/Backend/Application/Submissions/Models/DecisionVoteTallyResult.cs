namespace Application.Submissions.Models;

public sealed record DecisionVoteTallyResult(
    DecisionVoteTallyStatus Status,
    DecisionVoteTallyResponse? Tally,
    string? ErrorMessage)
{
    public static DecisionVoteTallyResult Success(DecisionVoteTallyResponse tally)
    {
        return new DecisionVoteTallyResult(DecisionVoteTallyStatus.Success, tally, null);
    }

    public static DecisionVoteTallyResult NotFound(string message)
    {
        return new DecisionVoteTallyResult(DecisionVoteTallyStatus.NotFound, null, message);
    }

    public static DecisionVoteTallyResult Forbidden()
    {
        return new DecisionVoteTallyResult(DecisionVoteTallyStatus.Forbidden, null, null);
    }

    public static DecisionVoteTallyResult NoActiveSession()
    {
        return new DecisionVoteTallyResult(DecisionVoteTallyStatus.NoActiveSession, null, "No active decision session for this submission.");
    }
}
