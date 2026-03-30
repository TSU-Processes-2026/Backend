namespace Application.Teams.Models;

public sealed record TeamCreateResult(TeamCreateStatus Status, TeamDistributionResponse? Response, IReadOnlyList<string> Errors)
{
    public static TeamCreateResult Success(TeamDistributionResponse response)
    {
        return new TeamCreateResult(TeamCreateStatus.Success, response, Array.Empty<string>());
    }

    public static TeamCreateResult Forbidden()
    {
        return new TeamCreateResult(TeamCreateStatus.Forbidden, null, Array.Empty<string>());
    }

    public static TeamCreateResult Invalid(IReadOnlyList<string> errors)
    {
        return new TeamCreateResult(TeamCreateStatus.Invalid, null, errors);
    }
}
