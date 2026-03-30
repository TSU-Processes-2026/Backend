namespace Application.Teams.Models;

public sealed record TeamMutationResult(TeamMutationStatus Status, TeamDistributionResponse? Response, IReadOnlyList<string> Errors)
{
    public static TeamMutationResult Success(TeamDistributionResponse response)
    {
        return new TeamMutationResult(TeamMutationStatus.Success, response, Array.Empty<string>());
    }

    public static TeamMutationResult Forbidden()
    {
        return new TeamMutationResult(TeamMutationStatus.Forbidden, null, Array.Empty<string>());
    }

    public static TeamMutationResult Invalid(IReadOnlyList<string> errors)
    {
        return new TeamMutationResult(TeamMutationStatus.Invalid, null, errors);
    }
}
