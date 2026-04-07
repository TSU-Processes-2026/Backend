namespace Application.Teams.Models;

public sealed class TeamResponse
{
    public Guid Id { get; init; }
    public Guid SubjectId { get; init; }
    public string? Name { get; init; }
    public Guid? CaptainId { get; init; }
    public IReadOnlyList<Guid> MemberIds { get; init; } = Array.Empty<Guid>();
    public IReadOnlyList<TeamMemberResponse> Members { get; init; } = Array.Empty<TeamMemberResponse>();
}
