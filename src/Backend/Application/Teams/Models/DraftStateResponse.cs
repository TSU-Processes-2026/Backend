namespace Application.Teams.Models;

public sealed class DraftStateResponse
{
    public Guid SubjectId { get; init; }
    public bool IsActive { get; init; }
    public bool IsCompleted { get; init; }
    public Guid? CurrentCaptainId { get; init; }
    public int CurrentRound { get; init; }
    public IReadOnlyList<TeamResponse> Teams { get; init; } = Array.Empty<TeamResponse>();
    public IReadOnlyList<TeamMemberResponse> AvailableStudents { get; init; } = Array.Empty<TeamMemberResponse>();
}
