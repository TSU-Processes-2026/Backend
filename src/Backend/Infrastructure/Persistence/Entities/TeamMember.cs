namespace Infrastructure.Persistence.Entities;

public sealed class TeamMember
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public Guid UserId { get; set; }
    public bool IsCaptain { get; set; }
    public Team Team { get; set; } = null!;
}
