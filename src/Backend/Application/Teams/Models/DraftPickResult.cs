namespace Application.Teams.Models;

public sealed record DraftPickResult(DraftPickStatus Status, DraftStateResponse? Response, IReadOnlyList<string> Errors)
{
    public static DraftPickResult Success(DraftStateResponse response)
    {
        return new DraftPickResult(DraftPickStatus.Success, response, Array.Empty<string>());
    }

    public static DraftPickResult Forbidden()
    {
        return new DraftPickResult(DraftPickStatus.Forbidden, null, Array.Empty<string>());
    }

    public static DraftPickResult Invalid(IReadOnlyList<string> errors)
    {
        return new DraftPickResult(DraftPickStatus.Invalid, null, errors);
    }
}

public enum DraftPickStatus
{
    Success,
    Forbidden,
    Invalid
}
