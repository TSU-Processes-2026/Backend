namespace Application.Teams.Models;

public sealed class TeamSettingsRequest
{
    public TeamDistributionMode? DistributionMode { get; init; }
    public int? FixedTeamsCount { get; init; }
    public int? FixedTeamSize { get; init; }
    public int? MinTeamSize { get; init; }
    public int? MaxTeamSize { get; init; }
}
