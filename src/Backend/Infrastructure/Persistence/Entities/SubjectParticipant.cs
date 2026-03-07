namespace Infrastructure.Persistence.Entities;

public sealed class SubjectParticipant
{
    public Guid SubjectId { get; set; }
    public Guid UserId { get; set; }
    public required string Role { get; set; }
    public required Subject Subject { get; set; }
}
