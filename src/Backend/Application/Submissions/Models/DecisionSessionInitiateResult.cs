namespace Application.Submissions.Models;

public sealed record DecisionSessionInitiateResult(
    DecisionSessionInitiateStatus Status,
    DecisionSessionResponse? Session,
    string? ErrorMessage)
{
    public static DecisionSessionInitiateResult Success(DecisionSessionResponse session)
    {
        return new DecisionSessionInitiateResult(DecisionSessionInitiateStatus.Success, session, null);
    }

    public static DecisionSessionInitiateResult NotFound(string message)
    {
        return new DecisionSessionInitiateResult(DecisionSessionInitiateStatus.NotFound, null, message);
    }

    public static DecisionSessionInitiateResult Forbidden()
    {
        return new DecisionSessionInitiateResult(DecisionSessionInitiateStatus.Forbidden, null, null);
    }

    public static DecisionSessionInitiateResult InvalidOperation(string message)
    {
        return new DecisionSessionInitiateResult(DecisionSessionInitiateStatus.InvalidOperation, null, message);
    }

    public static DecisionSessionInitiateResult AlreadyActive(DecisionSessionResponse session)
    {
        return new DecisionSessionInitiateResult(DecisionSessionInitiateStatus.AlreadyActive, session, "A decision session is already active for this submission.");
    }

    public static DecisionSessionInitiateResult NoCaptain()
    {
        return new DecisionSessionInitiateResult(DecisionSessionInitiateStatus.NoCaptain, null, "The team has no captain. A captain is required for captain-decides mode.");
    }
}
