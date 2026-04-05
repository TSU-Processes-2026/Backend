namespace Application.Teams.Models;

public sealed record DraftStateResult(DraftStateStatus Status, DraftStateResponse? Response)
{
    public static DraftStateResult Success(DraftStateResponse response)
    {
        return new DraftStateResult(DraftStateStatus.Success, response);
    }

    public static DraftStateResult Forbidden()
    {
        return new DraftStateResult(DraftStateStatus.Forbidden, null);
    }

    public static DraftStateResult NotFound()
    {
        return new DraftStateResult(DraftStateStatus.NotFound, null);
    }
}

public enum DraftStateStatus
{
    Success,
    Forbidden,
    NotFound
}
