namespace Application.Submissions.Models;

public sealed record CaptainDecisionResult(
    CaptainDecisionStatus Status,
    DecisionSessionResponse? Session,
    string? ErrorMessage)
{
    public static CaptainDecisionResult Success(DecisionSessionResponse session)
    {
        return new CaptainDecisionResult(CaptainDecisionStatus.Success, session, null);
    }

    public static CaptainDecisionResult NotFound(string message)
    {
        return new CaptainDecisionResult(CaptainDecisionStatus.NotFound, null, message);
    }

    public static CaptainDecisionResult Forbidden()
    {
        return new CaptainDecisionResult(CaptainDecisionStatus.Forbidden, null, null);
    }

    public static CaptainDecisionResult InvalidOperation(string message)
    {
        return new CaptainDecisionResult(CaptainDecisionStatus.InvalidOperation, null, message);
    }

    public static CaptainDecisionResult SessionClosed()
    {
        return new CaptainDecisionResult(CaptainDecisionStatus.SessionClosed, null, "The decision session has been closed.");
    }

    public static CaptainDecisionResult WrongDecisionMode()
    {
        return new CaptainDecisionResult(CaptainDecisionStatus.WrongDecisionMode, null, "Captain decision is not allowed in voting mode.");
    }

    public static CaptainDecisionResult NotCaptain()
    {
        return new CaptainDecisionResult(CaptainDecisionStatus.NotCaptain, null, "Only the team captain can approve or reject in captain-decides mode.");
    }
}
