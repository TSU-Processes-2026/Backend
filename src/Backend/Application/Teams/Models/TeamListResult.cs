namespace Application.Teams.Models;

public sealed record TeamListResult(
    TeamListStatus Status,
    IReadOnlyList<TeamResponse> Teams,
    TeamDistributionMode DistributionMode,
    bool IsFinalized)
{
    public static TeamListResult Success(
        IReadOnlyList<TeamResponse> teams,
        TeamDistributionMode distributionMode,
        bool isFinalized)
    {
        return new TeamListResult(TeamListStatus.Success, teams, distributionMode, isFinalized);
    }

    public static TeamListResult Forbidden()
    {
        return new TeamListResult(TeamListStatus.Forbidden, Array.Empty<TeamResponse>(), TeamDistributionMode.Manual, false);
    }
}
