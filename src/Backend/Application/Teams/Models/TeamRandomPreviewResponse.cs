namespace Application.Teams.Models;

public sealed class TeamRandomPreviewResponse
{
    public Guid SubjectId { get; init; }
    public bool IsValid { get; init; }
    public IReadOnlyList<RandomTeamPreviewTeamResponse> Teams { get; init; } = Array.Empty<RandomTeamPreviewTeamResponse>();
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    public RandomTeamDistributionSuggestionResponse? SuggestedParameters { get; init; }
}