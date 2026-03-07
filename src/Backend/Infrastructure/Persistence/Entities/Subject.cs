namespace Infrastructure.Persistence.Entities;

public sealed class Subject
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required ICollection<SubjectParticipant> Participants { get; set; } = new List<SubjectParticipant>();
}
