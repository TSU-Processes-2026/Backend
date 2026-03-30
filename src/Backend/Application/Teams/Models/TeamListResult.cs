namespace Application.Teams.Models;

public sealed record TeamListResult(TeamListStatus Status, IReadOnlyList<TeamResponse> Teams)
{
    public static TeamListResult Success(IReadOnlyList<TeamResponse> teams)
    {
        return new TeamListResult(TeamListStatus.Success, teams);
    }

    public static TeamListResult Forbidden()
    {
        return new TeamListResult(TeamListStatus.Forbidden, Array.Empty<TeamResponse>());
    }
}
