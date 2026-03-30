namespace Application.Teams.Models;

public sealed class ManualTeamRequest
{
    public IReadOnlyList<Guid> MemberIds { get; init; } = Array.Empty<Guid>();
}
