namespace Application.Teams.Models;

public sealed class TeamDistributionResponse
{
    public IReadOnlyList<TeamResponse> Teams { get; init; } = Array.Empty<TeamResponse>();
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}
