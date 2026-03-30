namespace Application.Teams.Models;

public sealed class TeamSettingsResponse
{
    public Guid SubjectId { get; init; }
    public TeamDistributionMode DistributionMode { get; init; }
    public int? FixedTeamsCount { get; init; }
    public int? FixedTeamSize { get; init; }
    public int? MinTeamSize { get; init; }
    public int? MaxTeamSize { get; init; }
    public bool IsFinalized { get; init; }
    public DateTimeOffset? FinalizedAt { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}
