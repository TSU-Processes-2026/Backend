namespace Application.Teams.Models;

public sealed class UnassignedStudentsResponse
{
    public Guid SubjectId { get; init; }
    public IReadOnlyList<Guid> StudentIds { get; init; } = Array.Empty<Guid>();
    public IReadOnlyList<TeamMemberResponse> Students { get; init; } = Array.Empty<TeamMemberResponse>();
}
