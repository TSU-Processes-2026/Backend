namespace Application.Teams.Models;

public sealed class ManualTeamDistributionRequest
{
    public IReadOnlyList<ManualTeamRequest> Teams { get; init; } = Array.Empty<ManualTeamRequest>();
}
