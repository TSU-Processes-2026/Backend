namespace Infrastructure.Persistence.Entities;

public sealed class Team
{
    public Guid Id { get; set; }
    public Guid SubjectId { get; set; }
    public string? Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Subject Subject { get; set; } = null!;
    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
}
