namespace Application.Teams.Models;

public sealed class TeamMemberResponse
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public bool IsCaptain { get; init; }
}
