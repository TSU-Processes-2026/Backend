namespace Application.Teams.Models;

public sealed record TeamFinalizeResult(
    TeamFinalizeStatus Status,
    TeamFinalizeResponse? Response,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings)
{
    public static TeamFinalizeResult Success(TeamFinalizeResponse response)
    {
        return new TeamFinalizeResult(TeamFinalizeStatus.Success, response, Array.Empty<string>(), Array.Empty<string>());
    }

    public static TeamFinalizeResult Forbidden()
    {
        return new TeamFinalizeResult(TeamFinalizeStatus.Forbidden, null, Array.Empty<string>(), Array.Empty<string>());
    }

    public static TeamFinalizeResult Invalid(IReadOnlyList<string> errors, IReadOnlyList<string> warnings)
    {
        return new TeamFinalizeResult(TeamFinalizeStatus.Invalid, null, errors, warnings);
    }
}
