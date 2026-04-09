namespace Application.Teams.Models;

/// <summary>
/// Response for the teams list that includes essential config info for students
/// to understand the distribution mode and finalization state for self-assembly.
/// </summary>
public sealed class TeamsListWithConfigResponse
{
    public IReadOnlyList<TeamResponse> Teams { get; init; } = Array.Empty<TeamResponse>();
    public TeamDistributionMode DistributionMode { get; init; }
    public bool IsFinalized { get; init; }
}
