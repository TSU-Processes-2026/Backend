namespace Application.Teams.Models;

public sealed class RandomTeamPreviewTeamResponse
{
    public IReadOnlyList<Guid> MemberIds { get; init; } = Array.Empty<Guid>();
}