namespace Application.Teams.Models;

public sealed record DraftStartResult(DraftStartStatus Status, DraftStateResponse? Response, IReadOnlyList<string> Errors)
{
    public static DraftStartResult Success(DraftStateResponse response)
    {
        return new DraftStartResult(DraftStartStatus.Success, response, Array.Empty<string>());
    }

    public static DraftStartResult Forbidden()
    {
        return new DraftStartResult(DraftStartStatus.Forbidden, null, Array.Empty<string>());
    }

    public static DraftStartResult Invalid(IReadOnlyList<string> errors)
    {
        return new DraftStartResult(DraftStartStatus.Invalid, null, errors);
    }
}

public enum DraftStartStatus
{
    Success,
    Forbidden,
    Invalid
}
