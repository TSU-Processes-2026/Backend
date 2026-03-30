namespace Application.Teams.Models;

public sealed class RandomTeamDistributionSuggestionResponse
{
    public int SuggestedTeamsCount { get; init; }
    public int SuggestedMinTeamSize { get; init; }
    public int SuggestedMaxTeamSize { get; init; }
    public int? SuggestedFixedTeamSize { get; init; }
    public IReadOnlyList<int> SuggestedTeamSizes { get; init; } = Array.Empty<int>();
}