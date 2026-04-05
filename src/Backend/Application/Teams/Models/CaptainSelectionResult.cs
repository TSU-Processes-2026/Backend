namespace Application.Teams.Models;

public sealed record CaptainSelectionResult(
    CaptainSelectionStatus Status,
    CaptainInfoResponse? Captain,
    string? ErrorMessage)
{
    public static CaptainSelectionResult Success(CaptainInfoResponse captain)
    {
        return new CaptainSelectionResult(CaptainSelectionStatus.Success, captain, null);
    }

    public static CaptainSelectionResult NotFound(string message)
    {
        return new CaptainSelectionResult(CaptainSelectionStatus.NotFound, null, message);
    }

    public static CaptainSelectionResult Forbidden()
    {
        return new CaptainSelectionResult(CaptainSelectionStatus.Forbidden, null, null);
    }

    public static CaptainSelectionResult InvalidOperation(string message)
    {
        return new CaptainSelectionResult(CaptainSelectionStatus.InvalidOperation, null, message);
    }
}
