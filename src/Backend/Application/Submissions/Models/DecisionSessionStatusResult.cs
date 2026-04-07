namespace Application.Submissions.Models;

public sealed record DecisionSessionStatusResult(
    DecisionSessionStatusStatus Status,
    DecisionSessionStatusResponse? Response,
    string? ErrorMessage)
{
    public static DecisionSessionStatusResult Success(DecisionSessionStatusResponse response)
    {
        return new DecisionSessionStatusResult(DecisionSessionStatusStatus.Success, response, null);
    }

    public static DecisionSessionStatusResult NotFound(string message)
    {
        return new DecisionSessionStatusResult(DecisionSessionStatusStatus.NotFound, null, message);
    }

    public static DecisionSessionStatusResult Forbidden()
    {
        return new DecisionSessionStatusResult(DecisionSessionStatusStatus.Forbidden, null, null);
    }

    public static DecisionSessionStatusResult NoActiveSession()
    {
        return new DecisionSessionStatusResult(DecisionSessionStatusStatus.NoActiveSession, null, "No active decision session for this submission.");
    }
}
